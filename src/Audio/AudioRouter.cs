using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AudioManager
{
    internal sealed class AudioRouter : IDisposable
    {
        readonly Dictionary<int, SourceRoute> routes = new Dictionary<int, SourceRoute>();
        readonly List<int> expired = new List<int>();
        readonly float[] levels = new float[ChannelRegistry.Count];
        readonly int mainThread = Thread.CurrentThread.ManagedThreadId;
        bool disposed;

        public AudioRouter()
        {
            for (int i = 0; i < levels.Length; i++) levels[i] = 1f;
        }

        public void RequireMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThread)
                throw new InvalidOperationException("Audio Manager's Unity API must be called on the main thread.");
            if (disposed) throw new ObjectDisposedException(nameof(AudioRouter));
        }

        public void SetLevels(int[] percentages)
        {
            RequireMainThread();
            for (int i = 0; i < levels.Length; i++) levels[i] = Mathf.Clamp(percentages[i], 0, 100) / 100f;
            Refresh();
        }

        public float NativeVolume(AudioSource source)
        {
            return source && routes.TryGetValue(source.GetInstanceID(), out var route) ? route.NativeVolume : source.volume;
        }

        public void WriteNative(AudioSource source, float volume, AudioChannel channel)
        {
            if (!source) return;
            int id = source.GetInstanceID();
            if (!routes.TryGetValue(id, out var route))
            {
                route = new SourceRoute { Source = source };
                routes.Add(id, route);
            }
            // A native pool source can be reused by a different gameplay category.
            route.Channel = channel;
            route.NativeVolume = volume;
            Apply(route);
        }

        public ModAudioRegistration Register(AudioSource source, AudioChannel channel)
        {
            RequireMainThread();
            if (!source) throw new ArgumentNullException(nameof(source));
            if (channel != AudioChannel.Mods && channel != AudioChannel.Ambience)
                throw new ArgumentException("External sources may register with Mods or Ambience only.", nameof(channel));
            int id = source.GetInstanceID();
            if (routes.ContainsKey(id)) throw new InvalidOperationException("This AudioSource is already routed.");
            var registration = new ModAudioRegistration(this, source);
            var route = new SourceRoute { Source = source, Channel = channel, NativeVolume = source.volume, Owner = registration };
            routes.Add(id, route);
            Apply(route);
            return registration;
        }

        internal void SetRegisteredVolume(ModAudioRegistration owner, AudioSource source, float volume)
        {
            RequireMainThread();
            if (!source) return;
            if (!routes.TryGetValue(source.GetInstanceID(), out var route) || route.Owner != owner)
                throw new ObjectDisposedException(nameof(ModAudioRegistration));
            route.NativeVolume = volume;
            Apply(route);
        }

        internal void Unregister(ModAudioRegistration owner, AudioSource source)
        {
            if (disposed) return;
            RequireMainThread();
            int id = owner.SourceId;
            if (!routes.TryGetValue(id, out var route) || route.Owner != owner) return;
            if (source) source.volume = route.NativeVolume;
            routes.Remove(id);
        }

        void Apply(SourceRoute route)
        {
            route.Source.volume = route.NativeVolume * levels[(int)AudioChannel.Master] * levels[(int)route.Channel];
        }

        void Refresh()
        {
            expired.Clear();
            foreach (var pair in routes)
            {
                if (!pair.Value.Source) expired.Add(pair.Key);
                else Apply(pair.Value);
            }
            foreach (int id in expired) routes.Remove(id);
        }

        public void Dispose()
        {
            if (disposed) return;
            RequireMainThread();
            foreach (var route in routes.Values)
                if (route.Source) route.Source.volume = route.NativeVolume;
            routes.Clear();
            disposed = true;
        }
    }
}
