using System;
using UnityEngine;

namespace AudioManager
{
    internal sealed class AudioSettingsSession
    {
        readonly AudioSettings settings;
        readonly AudioRouter router;
        readonly int[] committed = new int[ChannelRegistry.Count];
        readonly int[] pending = new int[ChannelRegistry.Count];
        bool editing;
        public event Action Changed;

        public AudioSettingsSession(AudioSettings settings, AudioRouter router)
        {
            this.settings = settings;
            this.router = router;
            settings.Read(committed);
            Array.Copy(committed, pending, pending.Length);
            router.SetLevels(committed);
        }

        public int Value(AudioChannel channel) => pending[(int)channel];

        public void Begin()
        {
            if (editing) return;
            settings.Read(committed);
            Array.Copy(committed, pending, pending.Length);
            editing = true;
            Changed?.Invoke();
        }

        public void Preview(AudioChannel channel, int value)
        {
            ChannelRegistry.Validate(channel);
            Begin();
            pending[(int)channel] = Mathf.Clamp(value, 0, 100);
            router.SetLevels(pending);
            Changed?.Invoke();
        }

        public void Commit()
        {
            if (!editing) return;
            bool changed = false;
            for (int i = 0; i < pending.Length; i++) changed |= pending[i] != committed[i];
            // Persist once for the complete edit transaction, never once per slider.
            if (changed) settings.Save(pending);
            Array.Copy(pending, committed, committed.Length);
            editing = false;
        }

        public void Cancel()
        {
            if (!editing) return;
            Array.Copy(committed, pending, pending.Length);
            router.SetLevels(committed);
            editing = false;
            Changed?.Invoke();
        }

    }
}
