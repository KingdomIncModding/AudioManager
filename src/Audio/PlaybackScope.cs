using System;

namespace AudioManager
{
    /// <summary>An allocation-free, nestable playback category; dispose in LIFO order on the Unity thread.</summary>
    public readonly struct PlaybackScope : IDisposable
    {
        readonly AudioChannel previous;
        readonly bool initialized;

        internal PlaybackScope(AudioChannel channel)
        {
            previous = PlaybackContext.Current;
            initialized = true;
            PlaybackContext.Current = previous == AudioChannel.Mods ? previous : channel;
        }

        public void Dispose()
        {
            // Another Harmony prefix can skip ours; a default __state must not enter Master context.
            if (initialized) PlaybackContext.Current = previous;
        }
    }
}
