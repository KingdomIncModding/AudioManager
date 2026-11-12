using System;

namespace AudioManager
{
    internal static class ChannelRegistry
    {
        public const int Count = 9;
        public static readonly AudioChannel[] Visible =
        {
            AudioChannel.Master, AudioChannel.Music, AudioChannel.UI, AudioChannel.Crafting,
            AudioChannel.Machines, AudioChannel.Economy, AudioChannel.Combat, AudioChannel.Mods
        };

        public static void Validate(AudioChannel channel)
        {
            if ((uint)channel >= Count) throw new ArgumentOutOfRangeException(nameof(channel));
        }

        public static string Description(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master: return "Overall Game Volume";
                case AudioChannel.Music: return "Background and contextual music";
                case AudioChannel.UI: return "Buttons, windows, tabs, menus";
                case AudioChannel.Crafting: return "Crafting progress and completion";
                case AudioChannel.Machines: return "Production and machine operations";
                case AudioChannel.Economy: return "Coins, sales, purchases";
                case AudioChannel.Combat: return "Battles, attacks, victory effects";
                case AudioChannel.Ambience: return "Environmental and background sounds";
                case AudioChannel.Mods: return "Sounds registered by other mods";
                default: throw new ArgumentOutOfRangeException(nameof(channel));
            }
        }
    }
}
