using System;
using System.Collections.Generic;
using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AudioManager
{
    [BepInPlugin(Id, "Audio Manager", "0.1.2")]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.0")]
    public sealed class AudioManagerPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.audiomanager";
        internal static AudioManagerPlugin Instance;
        readonly Dictionary<IVanillaSettingView, bool> hiddenRows = new Dictionary<IVanillaSettingView, bool>();
        readonly List<IVanillaSettingView> deadRows = new List<IVanillaSettingView>();
        readonly Dictionary<IVanillaSectionView, bool> hiddenHeadings = new Dictionary<IVanillaSectionView, bool>();
        readonly List<IVanillaSectionView> deadHeadings = new List<IVanillaSectionView>();
        FoundationModContext context;
        IDisposable viewObserver;
        internal IOptionsTabRegistration AudioTab;
        AudioSettingsSession session;
        AudioRouter router;
        IDisposable audioHooks;

        void Awake()
        {
            try
            {
                if (Instance != null) throw new InvalidOperationException("Audio Manager is already initialized.");
                if (FoundationApi.Options == null || FoundationApi.Audio == null)
                    throw new InvalidOperationException("Required Foundation settings/audio services are unavailable.");
                Instance = this;
                context = FoundationApi.ForMod(Id);
                router = new AudioRouter();
                session = new AudioSettingsSession(new AudioSettings(Config), router);
                RegisterOptions();
                AudioManagerApi.Router = router;
                audioHooks = FoundationAudioHooks.Register();
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                Logger.LogInfo("Audio Manager 0.1.2 initialized through Foundation 0.14.0; Ambience is registered but hidden.");
            }
            catch (Exception error)
            {
                Logger.LogError("Audio Manager could not initialize; restoring native audio and settings. " + error);
                Cleanup();
                enabled = false;
            }
        }

        void RegisterOptions()
        {
            var tab = new OptionsTab(Id, "audio", "Audio", order: 20);
            tab.Settings.AddElement(new AudioSettingsElement(session));
            AudioTab = context.Register(tab);
            viewObserver = FoundationApi.Options.ObserveViews(OnOptionsOpened);
        }

        void OnOptionsOpened(IOptionsView view)
        {
            session.Begin();
            PruneViews();
            HideVanillaRow(view, VanillaSettingIds.MusicVolume);
            HideVanillaRow(view, VanillaSettingIds.SoundVolume);
            HideAudioHeading(view);
        }

        void HideVanillaRow(IOptionsView view, string id)
        {
            if (!view.TryGetVanillaSetting(id, out var row) || !row.IsAlive) return;
            if (!hiddenRows.ContainsKey(row)) hiddenRows.Add(row, row.IsVisible);
            row.IsVisible = false;
        }

        void HideAudioHeading(IOptionsView view)
        {
            if (!view.TryGetVanillaSection(VanillaSectionIds.AudioAndSystem, out var heading) || !heading.IsAlive) return;
            if (!hiddenHeadings.ContainsKey(heading)) hiddenHeadings.Add(heading, heading.IsVisible);
            heading.IsVisible = false;
        }

        void PruneViews()
        {
            deadRows.Clear();
            foreach (var row in hiddenRows.Keys) if (!row.IsAlive) deadRows.Add(row);
            foreach (var row in deadRows) hiddenRows.Remove(row);
            deadHeadings.Clear();
            foreach (var heading in hiddenHeadings.Keys) if (!heading.IsAlive) deadHeadings.Add(heading);
            foreach (var heading in deadHeadings) hiddenHeadings.Remove(heading);
        }

        void OnSceneUnloaded(Scene scene)
        {
            // Scene teardown is event-driven; prune destroyed pooled sources without per-frame polling.
            if (router != null && session != null)
            {
                var levels = new int[ChannelRegistry.Count];
                for (int i = 0; i < levels.Length; i++) levels[i] = session.Value((AudioChannel)i);
                router.SetLevels(levels);
            }
            PruneViews();
        }

        void Cleanup()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            session?.Cancel();
            viewObserver?.Dispose();
            viewObserver = null;
            foreach (var pair in hiddenRows) if (pair.Key.IsAlive) pair.Key.IsVisible = pair.Value;
            hiddenRows.Clear();
            foreach (var pair in hiddenHeadings) if (pair.Key.IsAlive) pair.Key.IsVisible = pair.Value;
            hiddenHeadings.Clear();
            context?.Dispose();
            context = null;
            AudioTab = null;
            audioHooks?.Dispose();
            audioHooks = null;
            AudioManagerApi.Router = null;
            router?.Dispose();
            router = null;
            session = null;
            if (Instance == this) Instance = null;
        }

        void OnDestroy() => Cleanup();
    }
}
