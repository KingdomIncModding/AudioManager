using System;
using UnityEngine;

namespace AudioManager
{
    public static class AudioManagerApi
    {
        internal static AudioRouter Router;
        public static bool IsAvailable => Router != null;

        /// <summary>Register a mod-owned source without changing its clip, pitch, mixer, or playback.</summary>
        public static ModAudioRegistration RegisterSource(AudioSource source, AudioChannel channel = AudioChannel.Mods)
            => RequireRouter().Register(source, channel);

        /// <summary>Classifies native UISounds calls inside this scope as sounds from a participating mod.</summary>
        public static PlaybackScope BeginModPlayback()
        {
            RequireRouter().RequireMainThread();
            return new PlaybackScope(AudioChannel.Mods);
        }

        static AudioRouter RequireRouter()
            => Router ?? throw new InvalidOperationException("Audio Manager has not initialized or has shut down.");
    }
}
