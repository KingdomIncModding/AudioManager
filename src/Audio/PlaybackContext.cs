using System;

namespace AudioManager
{
    internal static class PlaybackContext
    {
        [ThreadStatic] static AudioChannel? current;
        public static AudioChannel Current
        {
            get => current ?? AudioChannel.UI;
            set => current = value;
        }
    }
}
