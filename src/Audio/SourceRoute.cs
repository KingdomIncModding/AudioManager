using UnityEngine;

namespace AudioManager
{
    internal sealed class SourceRoute
    {
        public AudioSource Source;
        public AudioChannel Channel;
        public float NativeVolume;
        public object Owner;
    }
}
