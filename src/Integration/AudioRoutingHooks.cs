using UnityEngine;

namespace AudioManager
{
    internal static class AudioRoutingHooks
    {
        public static void WriteMusic(AudioSource source, float nativeVolume)
        {
            if (AudioManagerApi.Router == null) source.volume = nativeVolume;
            else AudioManagerApi.Router.WriteNative(source, nativeVolume, AudioChannel.Music);
        }

        public static float ReadMusic(AudioSource source)
            => AudioManagerApi.Router == null ? source.volume : AudioManagerApi.Router.NativeVolume(source);

        public static void WriteEffect(AudioSource source, float nativeVolume)
        {
            if (AudioManagerApi.Router == null) source.volume = nativeVolume;
            else AudioManagerApi.Router.WriteNative(source, nativeVolume, PlaybackContext.Current);
        }
    }
}
