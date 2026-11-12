using System;
using UnityEngine;

namespace AudioManager
{
    /// <summary>Routes a caller-owned source. The caller retains playback and object ownership.</summary>
    public sealed class ModAudioRegistration : IDisposable
    {
        AudioRouter router;
        readonly AudioSource source;
        internal int SourceId { get; }

        internal ModAudioRegistration(AudioRouter router, AudioSource source)
        {
            this.router = router;
            this.source = source;
            SourceId = source.GetInstanceID();
        }

        /// <summary>Use this instead of assigning source.volume while the source is registered.</summary>
        public void SetVolume(float nativeVolume)
        {
            if (float.IsNaN(nativeVolume) || float.IsInfinity(nativeVolume) || nativeVolume < 0 || nativeVolume > 1)
                throw new ArgumentOutOfRangeException(nameof(nativeVolume));
            if (router == null) throw new ObjectDisposedException(nameof(ModAudioRegistration));
            router.SetRegisteredVolume(this, source, nativeVolume);
        }

        public void Dispose()
        {
            if (router == null) return;
            router.Unregister(this, source);
            router = null;
        }
    }
}
