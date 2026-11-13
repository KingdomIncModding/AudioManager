using System;
using KingdomFoundation.Api;

namespace AudioManager
{
    internal static class FoundationAudioHooks
    {
        internal static IDisposable Register() => FoundationApi.AudioInterception.Subscribe(AudioManagerPlugin.Id,
            writeEffect: (source, volume) => { AudioRoutingHooks.WriteEffect(source, volume); return source.volume; },
            writeMusic: (source, volume) => { AudioRoutingHooks.WriteMusic(source, volume); return source.volume; },
            readMusic: (source, volume) => AudioRoutingHooks.ReadMusic(source),
            activityScope: BeginActivity);

        private static IDisposable BeginActivity(GameAudioActivity activity)
        {
            switch (activity.Kind)
            {
                case GameAudioActivityKind.ManualCraft: return new PlaybackScope(AudioChannel.Crafting);
                case GameAudioActivityKind.MachineUpdate: return new PlaybackScope(activity.ManualQueued ? AudioChannel.Crafting : AudioChannel.Machines);
                case GameAudioActivityKind.EconomyAction: return new PlaybackScope(AudioChannel.Economy);
                case GameAudioActivityKind.CombatAction: return new PlaybackScope(AudioChannel.Combat);
                default: return null;
            }
        }
    }
}
