using System;
using KingdomFoundation.Api;
using UnityEngine;

namespace AudioManager
{
    internal sealed class AudioSettingsSession
    {
        readonly AudioSettings settings;
        readonly AudioRouter router;
        readonly int[] committed = new int[ChannelRegistry.Count];
        readonly int[] pending = new int[ChannelRegistry.Count];
        int committedNativeMusic;
        int committedNativeSound;
        int pendingNativeMusic;
        int pendingNativeSound;
        bool editing;
        public event Action Changed;

        public AudioSettingsSession(AudioSettings settings, AudioRouter router)
        {
            this.settings = settings;
            this.router = router;
            settings.Read(committed);
            Array.Copy(committed, pending, pending.Length);
            ReadNativeBase();
            router.SetLevels(committed);
        }

        public int Value(AudioChannel channel) => pending[(int)channel];
        public int NativeMusicValue => pendingNativeMusic;
        public int NativeSoundValue => pendingNativeSound;

        public void Begin()
        {
            if (editing) return;
            settings.Read(committed);
            Array.Copy(committed, pending, pending.Length);
            ReadNativeBase();
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

        public void PreviewNativeMusic(int value)
        {
            Begin();
            pendingNativeMusic = Mathf.Clamp(value, 0, 100);
            FoundationApi.Audio?.PreviewMusicVolume(pendingNativeMusic / 100f);
            Changed?.Invoke();
        }

        public void PreviewNativeSound(int value)
        {
            Begin();
            pendingNativeSound = Mathf.Clamp(value, 0, 100);
            FoundationApi.Audio?.PreviewSoundVolume(pendingNativeSound / 100f);
            Changed?.Invoke();
        }

        public void Commit()
        {
            if (!editing) return;
            bool changed = false;
            for (int i = 0; i < pending.Length; i++) changed |= pending[i] != committed[i];
            // Persist once for the complete edit transaction, never once per slider.
            if (changed) settings.Save(pending);
            if (FoundationApi.Audio != null)
            {
                if (pendingNativeMusic != committedNativeMusic)
                    FoundationApi.Audio.MusicVolume = pendingNativeMusic / 100f;
                if (pendingNativeSound != committedNativeSound)
                    FoundationApi.Audio.SoundVolume = pendingNativeSound / 100f;
            }
            Array.Copy(pending, committed, committed.Length);
            committedNativeMusic = pendingNativeMusic;
            committedNativeSound = pendingNativeSound;
            editing = false;
        }

        public void Cancel()
        {
            if (!editing) return;
            Array.Copy(committed, pending, pending.Length);
            pendingNativeMusic = committedNativeMusic;
            pendingNativeSound = committedNativeSound;
            router.SetLevels(committed);
            if (FoundationApi.Audio != null)
            {
                FoundationApi.Audio.PreviewMusicVolume(FoundationApi.Audio.MusicVolume);
                FoundationApi.Audio.PreviewSoundVolume(FoundationApi.Audio.SoundVolume);
            }
            editing = false;
            Changed?.Invoke();
        }

        void ReadNativeBase()
        {
            committedNativeMusic = pendingNativeMusic = ToPercent(FoundationApi.Audio?.MusicVolume);
            committedNativeSound = pendingNativeSound = ToPercent(FoundationApi.Audio?.SoundVolume);
        }

        static int ToPercent(float? level) => level.HasValue ? Mathf.Clamp(Mathf.RoundToInt(level.Value * 100f), 0, 100) : 100;
    }
}
