# Audio Manager

**Current release: 0.1.3 — for Kingdom Inc game version 1.0.14.**

Adjust Kingdom Inc's audio from a dedicated **Audio** tab in Foundation's Options menu.

## Volume controls

- **Master:** overall volume for native game audio and registered mod sounds.
- **Music:** background and contextual music.
- **UI:** buttons, windows, tabs, menus, and interface confirmations.
- **Crafting:** handcrafting steps and hand-assisted completion.
- **Machines:** automatic production and machine operations.
- **Economy:** coins, collections, sales, and exchange confirmations.
- **Combat:** existing arena and dungeon launch confirmations.
- **Mods:** sounds explicitly registered by participating mods.
- **Base Music / Base Effects:** the game's original music and sound effect master preferences, exposed right in the Audio tab so previously muted saves can always be raised or restored.

Each slider ranges from 0 to 100. All new channel multipliers start at 100%, preserving your existing music and sound levels. Native music selection, transitions, sound pitch, and repeated-sound suppression remain in use. Victory music stays on the Music channel; the mod does not add attack sounds.

Slider changes preview immediately. **Apply & Close** saves them; **Cancel** or closing Options restores your previous committed mix. Switching tabs keeps pending changes. Display settings, Autosave and Key Bindings remain under **Game**; its old **Audio & System** heading is hidden.

## Requirements

- Kingdom Inc.
- BepInEx 5.
- [Foundation](https://www.nexusmods.com/kingdominc/mods/18) 0.14.4 or later.

## Installation and use

1. Install BepInEx 5 and Foundation.
2. Place `AudioManager.dll` and `mod.json` together in `BepInEx/plugins/AudioManager/` inside your Kingdom Inc folder.
3. Open **Options → Audio** from the main menu or in-game menu.

Channel settings are stored separately in `BepInEx/config/urakka.kingdominc.audiomanager.cfg`. Existing native music and sound preferences are preserved. The Mods slider affects participating mods; it does not automatically take over other mods' AudioSources.

To uninstall, close the game and remove `BepInEx/plugins/AudioManager/`. The game's original volume controls return. You may also remove Audio Manager's configuration file if you no longer want the saved channel levels.

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| MusicManager._updateVolume | Transpiler | Applies Master and Music to native volume updates. |
| MusicManager.PreviewVolume | Transpiler | Preserves native previews while applying channel levels. |
| MusicManager._playTrack | Coroutine transpiler | Applies channel levels when native tracks start; retains playback and quiet intervals. |
| MusicManager._fadeOut | Coroutine transpiler | Preserves the original fade calculations using unscaled volume. |
| UISounds.PlaySound(AudioClip, float, float) | Transpiler | Routes native pooled effects without replacing pitch or cooldown logic. |
| UISounds.Coins | Prefix / Finalizer | Classifies native coin effects as Economy unless explicitly scoped by a participating mod. |
| RecipeElement.DoManualCraft | Prefix / Finalizer | Classifies handcrafting steps. |
| ResourceElement.DoManualCraft | Prefix / Finalizer | Classifies manual resource actions. |
| Forest.DoManualCraft | Prefix / Finalizer | Classifies manual harvesting actions. |
| Inn.DoManualCraft | Prefix / Finalizer | Classifies manual work effects. |
| TrainingGrounds.DoManualCraft | Prefix / Finalizer | Classifies manual training effects. |
| Disenchanter.DoManualCraft | Prefix / Finalizer | Classifies manual disenchanting effects; coins retain Economy routing. |
| RecipeElement.Update | Prefix / Finalizer | Separates hand-assisted completion from automatic production. |
| ResourceElement.Update | Prefix / Finalizer | Uses manual queue context to classify production effects. |
| Forest.Update | Prefix / Finalizer | Uses manual queue context to classify harvesting effects. |
| Refiner.Update | Prefix / Finalizer | Classifies automatic refining effects as Machines. |
| Disenchanter.Update | Prefix / Finalizer | Classifies automatic disenchanting effects as Machines. |
| Inn.Update | Prefix / Finalizer | Classifies automatic operation effects as Machines. |
| TrainingGrounds.Update | Prefix / Finalizer | Classifies automatic training operation effects as Machines. |
| CurrencyExchangePopup.ButtonExchange | Prefix / Finalizer | Classifies currency-exchange confirmations and failures as Economy. |
| Shop.DoCollect | Prefix / Finalizer | Classifies collection effects as Economy. |
| Shop.DoManualCraft | Prefix / Finalizer | Classifies manual collection effects as Economy. |
| Restaurant.DoManualCraft | Prefix / Finalizer | Classifies manual collection effects as Economy. |
| RequisitionOffice.DoManualCraft | Prefix / Finalizer | Classifies manual collection effects as Economy. |
| Restaurant.Update | Prefix / Finalizer | Classifies automatic income effects as Economy. |
| LuxuryShop.Update | Prefix / Finalizer | Classifies automatic income effects as Economy. |
| MercenaryGuild.Update | Prefix / Finalizer | Classifies automatic income effects as Economy. |
| RequisitionOffice.Update | Prefix / Finalizer | Classifies automatic income effects as Economy. |
| WarEffort.Update | Prefix / Finalizer | Classifies native income effects as Economy. |
| Arena.DoManualCraft | Prefix / Finalizer | Classifies arena action confirmations as Combat. |
| Arena.Update | Prefix / Finalizer | Classifies arena completion effects as Combat. |
| Dungeon.CheckForParty | Prefix / Finalizer | Classifies dungeon launch confirmations as Combat. |
