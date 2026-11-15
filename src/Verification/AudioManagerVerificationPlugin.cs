using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.MainMenu;
using Assets.Behaviour.Util;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AudioManager.Verification
{
    [BepInPlugin("urakka.kingdominc.audiomanager.verification", "Audio Manager Verification", "0.1.2")]
    [BepInDependency(AudioManagerPlugin.Id)]
    public sealed class AudioManagerVerificationPlugin : BaseUnityPlugin
    {
        readonly List<string> checks = new List<string>();
        readonly List<string> errors = new List<string>();
        VerificationPreferences preferences;
        string directory;
        OptionsUI options;
        FoundationConfigMenu menu;
        AudioSettingsSession session;
        float musicPreference, soundPreference;
        bool restart;

        void Awake()
        {
            if (!Environment.GetCommandLineArgs().Contains("-audio-manager-verify")) { enabled = false; return; }
            directory = Path.Combine(Paths.GameRootPath, "Verification");
            Directory.CreateDirectory(directory);
            File.Delete(Path.Combine(directory, "audio-tab.png"));
            File.Delete(Path.Combine(directory, "audio-tab-ingame.png"));
            restart = Environment.GetCommandLineArgs().Contains("-audio-manager-verify-restart");
            preferences = new VerificationPreferences();
            musicPreference = MusicManager.Volume;
            soundPreference = UISounds.Volume;
            var safety = new Harmony("urakka.kingdominc.audiomanager.verification.safety");
            var skip = new HarmonyMethod(AccessTools.Method(GetType(), nameof(Skip)));
            safety.Patch(AccessTools.Method(typeof(SaveGame), "Store"), prefix: skip);
            safety.Patch(AccessTools.Method(typeof(SaveGame), "StoreAutosaveState"), prefix: skip);
            safety.Patch(AccessTools.Method(typeof(SteamManager), "Awake"), prefix: skip);
            safety.Patch(AccessTools.PropertyGetter(typeof(SteamManager), "Initialized"), prefix: skip);
            string saves = Path.Combine(directory, "Saves");
            Directory.CreateDirectory(saves);
            SaveGame.SavesPath = saves;
            AccessTools.Field(typeof(SaveGame), "SavesDir").SetValue(null, new DirectoryInfo(saves));
            AccessTools.Field(typeof(SaveGame), "_saves").SetValue(null, null);
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>().MakeCurrent();
            if (Mouse.current == null) InputSystem.AddDevice<Mouse>().MakeCurrent();
            Application.logMessageReceived += WatchErrors;
        }

        static bool Skip() => false;
        void WatchErrors(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message + "\n" + stack);
        }

        IEnumerator Start()
        {
            if (!enabled) yield break;
            string failure = null;
            float deadline = Time.realtimeSinceStartup + 30;
            while ((!MainMenuUI.Instance || !UISounds.Instance) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(1);
            try { VerifyStartup(); }
            catch (Exception error) { failure = error.ToString(); }
            if (failure == null)
            {
                var work = VerifyAudioAndMenu();
                while (true)
                {
                    object next = null;
                    bool hasNext = false;
                    try { hasNext = work.MoveNext(); if (hasNext) next = work.Current; }
                    catch (Exception error) { failure = error.ToString(); }
                    if (!hasNext || failure != null) break;
                    yield return next;
                }
            }
            if (failure == null)
            {
                try { GamePlayer.CreateNewPlayer(); SceneManager.LoadScene("Game"); }
                catch (Exception error) { failure = error.ToString(); }
                yield return new WaitForSecondsRealtime(3);
            }
            if (failure == null)
            {
                var work = VerifyGameplay();
                while (true)
                {
                    object next = null;
                    bool hasNext = false;
                    try { hasNext = work.MoveNext(); if (hasNext) next = work.Current; }
                    catch (Exception error) { failure = error.ToString(); }
                    if (!hasNext || failure != null) break;
                    yield return next;
                }
            }
            try
            {
                Check(Mathf.Approximately(MusicManager.Volume, musicPreference) && Mathf.Approximately(UISounds.Volume, soundPreference), "Native music and sound preferences are unchanged");
                Check(errors.Count == 0, "No Unity errors or exceptions during the checks");
            }
            catch (Exception error) { if (failure == null) failure = error.ToString(); }
            preferences.Restore();
            File.WriteAllText(Path.Combine(directory, restart ? "restart-results.txt" : "results.txt"),
                (failure == null ? "PASS" : "FAIL") + "\nGame " + Application.version + " / Unity " + Application.unityVersion +
                "\n" + string.Join("\n", checks) + "\n" + failure + "\n" + string.Join("\n", errors));
            Logger.LogInfo("Audio Manager verification: " + (failure ?? (checks.Count + " checks passed")));
            Application.logMessageReceived -= WatchErrors;
            Application.Quit();
        }

        void VerifyStartup()
        {
            Check(AudioManagerApi.IsAvailable && AudioManagerPlugin.Instance, "Audio Manager loads alongside Foundation");
            session = (AudioSettingsSession)AccessTools.Field(typeof(AudioManagerPlugin), "session").GetValue(AudioManagerPlugin.Instance);
            Check(session.Value(AudioChannel.Master) == (restart ? 67 : 100), restart ? "Committed values survive a process restart" : "New channels default to 100 percent");
            Check(Harmony.GetAllPatchedMethods().Count(method => Harmony.GetPatchInfo(method).Owners.Contains("urakka.kingdominc.foundation.audio-hooks")) >= 30 &&
                !Harmony.GetAllPatchedMethods().Any(method => Harmony.GetPatchInfo(method).Owners.Contains(AudioManagerPlugin.Id)),
                "Foundation owns native playback and activity hooks; Audio Manager supplies no direct game patches");
            MainMenuUI.Instance.ShowOptions();
            options = MainMenuUI.Instance.GetComponentInChildren<OptionsUI>(true);
            menu = options.GetComponent<FoundationConfigMenu>();
            Check(menu && FoundationApi.Options.TryGet(AudioManagerPlugin.Id, "audio", out _), "Audio tab is registered with Foundation's public Options API");
            VerifyGameHeading("game-tab.png");
            ShowAudio();
            Check(options.transform.Find("OptionsTabs/LeftTabsViewport/LeftTabs").GetComponentsInChildren<Button>().Length == 2 &&
                options.transform.Find("OptionsTabs/RightTabs").GetComponentsInChildren<Button>().Length == 2,
                "Foundation owns Game and Audio on the left and Mods and Installed on the right");
            var audio = options.GetComponentInChildren<AudioSettingsView>();
            Check(!audio.transform.Find("AmbienceVolume"), "Ambience remains hidden");
            Check(audio.GetComponentsInChildren<Slider>().Length == 10, "Eight visible channel sliders plus two native base sliders use native templates");
            Check(menu.TryGetVanillaSetting(VanillaSettingIds.MusicVolume, out var nativeMusic) && !nativeMusic.IsVisible &&
                menu.TryGetVanillaSetting(VanillaSettingIds.SoundVolume, out var nativeSound) && !nativeSound.IsVisible,
                "Native volume rows are hidden through Foundation's public view API");
            Check(!Harmony.GetAllPatchedMethods().Any(method => method.DeclaringType.Assembly == typeof(FoundationConfigMenu).Assembly &&
                Harmony.GetPatchInfo(method).Owners.Contains(AudioManagerPlugin.Id)), "Audio Manager does not Harmony-patch Foundation's menu");
        }

        IEnumerator VerifyAudioAndMenu()
        {
            VerificationScreenshot.Capture(Path.Combine(directory, "audio-tab.png"));
            yield return new WaitForSecondsRealtime(.3f);
            SetSlider(AudioChannel.Master, 50);
            SetSlider(AudioChannel.Music, 50);
            SetSlider(AudioChannel.UI, 40);
            FoundationApi.Audio.PreviewMusicVolume(.8f);
            UISounds.PreviewVolume(.8f);
            var music = FindAnyObjectByType<MusicManager>();
            var musicSource = (AudioSource)AccessTools.Field(typeof(MusicManager), "_source").GetValue(music);
            Check(Near(musicSource.volume, .2f), "Music live preview combines native, Master, and Music exactly once");
            UISounds.Button();
            var button = Clip("_button");
            var route = FindRoute(button, AudioChannel.UI);
            Check(route != null && Near(route.Source.volume, .08f), "UI slider scales native pooled playback independently");
            Check(route.Source.pitch >= 1f / 1.02f && route.Source.pitch <= 1.02f, "Native sound pitch variation stays in its original range");
            int count = Pool().Count;
            float pitch = route.Source.pitch;
            UISounds.Button();
            Check(Pool().Count == count && Near(route.Source.pitch, pitch), "Immediate duplicate sounds remain suppressed");
            SetSlider(AudioChannel.UI, 0);
            yield return new WaitForSecondsRealtime(.15f);
            UISounds.Button();
            route = FindRoute(button, AudioChannel.UI);
            Check(route != null && route.Source.volume == 0, "Muted effects still pass through native pooled playback");
            pitch = route.Source.pitch;
            count = Pool().Count;
            UISounds.Button();
            Check(Pool().Count == count && Near(route.Source.pitch, pitch), "Muted effects retain native duplicate suppression");
            SetSlider(AudioChannel.UI, 40);
            SetSlider(AudioChannel.Master, 0);
            Check(musicSource.volume == 0, "Master mutes music");
            SetSlider(AudioChannel.Master, 50);
            Check(Near(musicSource.volume, .2f), "Unmuting restores the unscaled native music level");
            VerifyModSource();
            default(PlaybackScope).Dispose();
            Check(PlaybackContext.Current == AudioChannel.UI, "A skipped context prefix cannot leave Master selected or scale twice");
            yield return new WaitForSecondsRealtime(.15f);
            using (AudioManagerApi.BeginModPlayback()) UISounds.Coins();
            Check(FindRoute(Clip("_coins"), AudioChannel.Mods) != null, "Explicit mod scopes retain ownership through native sound helpers");
            var fade = (IEnumerator)AccessTools.Method(typeof(MusicManager), "_fadeOut").Invoke(music, new object[] { .35f });
            float previous = musicSource.volume;
            bool monotonic = true;
            while (fade.MoveNext())
            {
                monotonic &= musicSource.volume <= previous + .0001f;
                previous = musicSource.volume;
                yield return null;
            }
            Check(monotonic, "Native fade remains monotonic");
            FoundationApi.Audio.PlayMusic("Rise and Shine LOOP BPM120", 0);
            yield return new WaitForSecondsRealtime(.15f);
            Check(musicSource.isPlaying && Near(musicSource.volume, musicPreference * .25f), "Native music restarts at its saved level without repeated scaling");
            options.CancelOptions();
            MainMenuUI.Instance.ShowOptions();
            VerifyGameHeading("game-tab-reopened.png");
            ShowAudio();
            Check(GetSlider(AudioChannel.Master).value == (restart ? 67 : 100), "Cancel discards previews and reopening restores committed values");
            int[] levels = { 67, 73, 37, 49, 51, 53, 59, 100, 61 };
            foreach (var channel in ChannelRegistry.Visible) SetSlider(channel, levels[(int)channel]);
            // Exercise Foundation's existing transaction rather than directly committing the session.
            options.ApplyAndClose();
            Check(File.Exists(Path.Combine(Paths.ConfigPath, AudioManagerPlugin.Id + ".cfg")), "Apply persists the complete channel configuration");
            MainMenuUI.Instance.ShowOptions();
            ShowAudio();
            Check(GetSlider(AudioChannel.Crafting).value == 49 && GetSlider(AudioChannel.Machines).value == 51, "Apply preserves independent Crafting and Machines values when reopened");
            menu.ShowGameTab();
            Check(menu.TryGetVanillaSetting(VanillaSettingIds.Resolution, out var resolution) && resolution.IsVisible &&
                menu.TryGetVanillaSetting(VanillaSettingIds.UIScale, out var scale) && scale.IsVisible,
                "Native display controls remain under Game");
            menu.ShowModsTab();
            Check(options.transform.Find("ModSettingsPage").gameObject.activeSelf, "Foundation Mods page remains functional");
            ShowAudio();
            options.CancelOptions();
        }

        void VerifyModSource()
        {
            var obj = new GameObject("AudioManagerVerificationSource");
            var source = obj.AddComponent<AudioSource>();
            source.volume = .6f;
            SetSlider(AudioChannel.Mods, 50);
            Check(Near(source.volume, .6f), "Unregistered mod sources are left untouched");
            using (var registration = AudioManagerApi.RegisterSource(source))
            {
                Check(Near(source.volume, .15f), "Participating mod sources receive Master and Mods multipliers");
                registration.SetVolume(.8f);
                Check(Near(source.volume, .2f), "Registered sources can change their original volume without compounding");
            }
            Check(Near(source.volume, .8f), "Disposing registration restores the mod-owned source volume");
            Destroy(obj);
            var disposable = new GameObject("AudioManagerDestroyedSourceProbe");
            var destroyedRegistration = AudioManagerApi.RegisterSource(disposable.AddComponent<AudioSource>());
            DestroyImmediate(disposable);
            destroyedRegistration.Dispose();
            var routes = (Dictionary<int, SourceRoute>)AccessTools.Field(typeof(AudioRouter), "routes").GetValue(AudioManagerApi.Router);
            Check(!routes.ContainsKey(destroyedRegistration.SourceId), "Disposal also removes registrations whose Unity source was already destroyed");
        }

        IEnumerator VerifyGameplay()
        {
            Check(GameUI.Instance && GamePlayer.Current != null, "Disposable gameplay scene loads with Foundation and Audio Manager");
            var forge = GamePlayer.Current.Flows[0].Elements.OfType<Forge>().First();
            forge.Update(0);
            Check(forge.Crafting && forge.World, "Native forge has a recipe, input, and world representation");
            forge.DoManualCraft();
            Check(FindRoute(Clip("_craftStep"), AudioChannel.Crafting) != null, "Actual handcrafting step routes to Crafting");
            yield return new WaitForSecondsRealtime(.15f);
            forge.Progress = 1;
            forge.Update(0);
            Check(FindRoute(Clip("_craftFinished"), AudioChannel.Crafting) != null, "Hand-assisted completion retains Crafting context");
            yield return new WaitForSecondsRealtime(.15f);
            forge.MouseProximity = 1;
            forge.Progress = 1;
            forge.Update(0);
            Check(FindRoute(Clip("_craftFinished"), AudioChannel.Machines) != null, "Automatic completion of the same forge and clip routes to Machines");
            yield return new WaitForSecondsRealtime(.15f);
            UISounds.Coins();
            Check(FindRoute(Clip("_coins"), AudioChannel.Economy) != null, "Native coin playback routes to Economy");
            yield return new WaitForSecondsRealtime(.15f);
            new Arena().DoManualCraft();
            Check(FindRoute(Clip("_craftFinished"), AudioChannel.Combat) != null, "Native arena action routes its shared completion clip to Combat");
            Check(PlaybackContext.Current == AudioChannel.UI, "Gameplay context is restored after nested native calls");
            var ingame = Resources.FindObjectsOfTypeAll<InGameMenuUI>().First(item => item.gameObject.scene.IsValid());
            ingame.gameObject.SetActive(true);
            ingame.ShowOptions();
            options = ingame.GetComponentInChildren<OptionsUI>(true);
            menu = options.GetComponent<FoundationConfigMenu>();
            Check(menu, "In-game Foundation menu is available");
            VerifyGameHeading("game-tab-ingame.png");
            ShowAudio();
            Check(options.GetComponentInChildren<AudioSettingsView>(), "Audio settings render inside the in-game Foundation tab too");
            VerificationScreenshot.Capture(Path.Combine(directory, "audio-tab-ingame.png"));
            yield return new WaitForSecondsRealtime(.5f);
            VerifyCleanup();
        }

        void VerifyCleanup()
        {
            var obj = new GameObject("AudioManagerCleanupProbe");
            var source = obj.AddComponent<AudioSource>();
            source.volume = .7f;
            var registration = AudioManagerApi.RegisterSource(source);
            AccessTools.Method(typeof(AudioManagerPlugin), "Cleanup").Invoke(AudioManagerPlugin.Instance, null);
            Check(!AudioManagerApi.IsAvailable && Near(source.volume, .7f), "Shutdown restores source volumes and withdraws the public API");
            Check(!FoundationApi.Options.TryGet(AudioManagerPlugin.Id, "audio", out _), "Foundation-owned Audio tab registration is removed during cleanup");
            FoundationApi.Audio.PreviewMusicVolume(.8f);
            var music = FindAnyObjectByType<MusicManager>();
            var musicSource = (AudioSource)AccessTools.Field(typeof(MusicManager), "_source").GetValue(music);
            Check(Near(musicSource.volume,.8f), "Removing the mod subscription restores unscaled native music writes");
            FoundationApi.Audio.PreviewMusicVolume(musicPreference);
            Check(menu.TryGetVanillaSetting(VanillaSettingIds.MusicVolume, out var nativeMusic) && nativeMusic.IsVisible &&
                menu.TryGetVanillaSetting(VanillaSettingIds.SoundVolume, out var nativeSound) && nativeSound.IsVisible,
                "Cleanup restores the native volume controls through Foundation's public API");
            Check(((IOptionsView)menu).TryGetVanillaSection(VanillaSectionIds.AudioAndSystem, out var heading) && heading.IsVisible,
                "Cleanup restores the Game Audio & System heading through Foundation's public API");
            registration.Dispose();
            Destroy(obj);
        }

        void VerifyGameHeading(string screenshot)
        {
            menu.ShowGameTab();
            IOptionsView view = menu;
            Check(view.TryGetVanillaSection(VanillaSectionIds.AudioAndSystem, out var heading) && !heading.IsVisible,
                "Game Audio & System heading is hidden: " + screenshot);
            bool hasAutosave = view.TryGetVanillaSetting(VanillaSettingIds.AutosaveFreq, out var autosave);
            Check(view.TryGetVanillaSection(VanillaSectionIds.DisplayAndInterface, out var display) && display.IsVisible &&
                hasAutosave && autosave.IsVisible &&
                view.TryGetVanillaSetting(VanillaSettingIds.KeyBindings, out var bindings) && bindings.IsVisible,
                "Display heading, Autosave and Key Bindings remain visible: " + screenshot);
            // Foundation must retain the explicit heading choice after another native-row layout update.
            autosave.IsVisible = true;
            Check(!heading.IsVisible, "Native row layout updates do not restore the hidden audio heading: " + screenshot);
            VerificationScreenshot.Capture(Path.Combine(directory, screenshot));
        }

        void SetSlider(AudioChannel channel, int value) => GetSlider(channel).value = value;
        Slider GetSlider(AudioChannel channel) => options.GetComponentInChildren<AudioSettingsView>().transform.Find(channel + "Volume/ConfigSlider").GetComponent<Slider>();
        void ShowAudio() => menu.ShowCustomTab(AudioManagerPlugin.Instance.AudioTab);
        AudioClip Clip(string field) => (AudioClip)AccessTools.Field(typeof(UISounds), field).GetValue(UISounds.Instance);
        List<AudioSource> Pool() => (List<AudioSource>)AccessTools.Field(typeof(UISounds), "_sourceCache").GetValue(UISounds.Instance);

        SourceRoute FindRoute(AudioClip clip, AudioChannel channel)
        {
            var routes = (Dictionary<int, SourceRoute>)AccessTools.Field(typeof(AudioRouter), "routes").GetValue(AudioManagerApi.Router);
            return routes.Values.LastOrDefault(route => route.Source && route.Source.clip == clip && route.Channel == channel);
        }

        static bool Near(float left, float right) => Mathf.Abs(left - right) < .0001f;
        void Check(bool result, string description)
        {
            if (!result) throw new InvalidOperationException(description);
            checks.Add(description);
        }
    }
}
