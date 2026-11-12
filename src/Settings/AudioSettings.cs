using BepInEx.Configuration;

namespace AudioManager
{
    internal sealed class AudioSettings
    {
        readonly ConfigFile config;
        readonly ConfigEntry<int>[] entries = new ConfigEntry<int>[ChannelRegistry.Count];

        public AudioSettings(ConfigFile config)
        {
            this.config = config;
            config.SaveOnConfigSet = false;
            for (int i = 0; i < entries.Length; i++)
            {
                var channel = (AudioChannel)i;
                entries[i] = config.Bind("Channels", channel.ToString(), 100,
                    new ConfigDescription(ChannelRegistry.Description(channel) + " (percent).", new AcceptableValueRange<int>(0, 100)));
            }
        }

        public void Read(int[] values)
        {
            for (int i = 0; i < entries.Length; i++) values[i] = entries[i].Value;
        }

        public void Save(int[] values)
        {
            for (int i = 0; i < entries.Length; i++) entries[i].Value = values[i];
            config.Save();
        }
    }
}
