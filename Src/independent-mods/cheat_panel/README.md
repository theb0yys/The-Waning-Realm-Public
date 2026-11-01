# TAOM Cheat Panel

Public source note: this copy includes the standalone runtime and settings-check project. The Utilities Hub extraction test and two local machine evidence reports are omitted. Validation and installation statements below describe historical local runs, not a build or deployment performed for this publication.

TAOM Cheat Panel runs without Utilities Hub or other gameplay suite modules.
The user-requested MCM/progression integration now requires **Mod Configuration
Menu v5** and **Bannerlord Harmony**, loaded before the panel. This machine's
MCM alias is supplied by TAOM.Dependencies; the panel does not package or alter it.
Native, SandBoxCore and Sandbox remain required; StoryMode is optional.

Enable **TAOM Cheat Panel** in the launcher. Open **[TAOM] Cheat Panel** from a
town, castle-outside or village menu. Open **Settings** inside the panel to
configure cheats, confirmations and passive daily adjustments, or open
**Mod Options -> TAOM Cheat Panel**. Both menus share the same settings file.
Passive cheats and the new progression cheats start disabled (XP starts at x1).

The configurable opening shortcut defaults to **Ctrl+F10**. In **Mod Options ->
TAOM Cheat Panel -> General**, choose **Open panel key** and **Hold Ctrl for panel
shortcut**; select **Disabled** to turn it off. Apply saves; Cancel restores the
previous choice. It opens from the campaign map and normal settlement menus.
Combat, conversations and other active screens block the shortcut. Close returns
to the originating settlement menu or the map. Pick a key unused by other mods
and native controls. This candidate DLL is installed; native acceptance is pending. See
[hotkey design](HOTKEY_DESIGN.md). The local evidence report is not included in this public source copy.

## Progression and MCM options

- **Take both perks**: automatically grant both choices at earned skill tiers for
  the main hero. Existing tiers catch up on campaign load or by the next campaign
  hour after enabling. Higher skill tiers stay locked; NPCs are unaffected.
- **Player XP multiplier**: x1–x10 for normal main-hero skill and level XP.
  Explicit panel XP commands keep their requested amount. Other XP mods may stack.
- **Extra attribute/focus points per level**: 0–10 additional unspent points of
  each kind on future main-hero level-ups; no retrospective awards.
- **Daily upkeep**: main hero/companion healing, wounded troop recovery, morale,
  food, smithing stamina, and gold/influence/renown stipends. Enable both the
  general master and daily upkeep master. These are daily actions, not continuous.

Use MCM Done/Apply to save, or Cancel to undo its pending setting edits. Turning
cheats off does not remove perks, XP or points already written to the campaign.
Use a pre-cheat save to undo them. Native UI/save acceptance remains pending.
See [design and native acceptance](PROGRESSION_MCM_DESIGN.md) and
current evidence (local evidence report, not published).

Settings save automatically to:
`Documents/Mount and Blade II Bannerlord/Configs/TAOM_CheatPanel/settings.xml`.
The prior settings file is preserved as `settings.xml.bak` on replacement.
No prior MCM values are imported automatically. Removing the mod does not undo
campaign changes already made by cheats.

The module owns its settings, bounded session history, diagnostics, native API
helpers and troop-equipment checks. Troop selection supports native and modded
cultures; it does not require overhaul-specific IDs. Hub reports remain in Hub.
No shared UI art or suite libraries are packaged.

## Validation and testing

2026-10-07 candidate: Release build and 54 managed checks pass; 7 static checks
pass and 4 pre-existing Utilities Hub extraction checks fail. The user-approved
panel-only update is installed with verified backups, hashes and timestamps.
MCM/native campaign acceptance remains pending. Detailed scope and results are in
`PROGRESSION_MCM_EVIDENCE.json`.

Historical 2026-09-27 baseline: standalone Release/x64 rebuild passed. 24 executable checks covered
settings defaults, master gating, numerical clamps, XML round-trip, bounded
history and the actual compiled DLL's native-only assembly references. Nine
static checks cover module dependencies, ownership, settings separation,
project sources and native-menu destinations. No MCM test doubles are needed.

The private-repository extraction test is not included because it reads Utilities Hub source.
Build `tests/CheatPanel.SettingsChecks/CheatPanel.SettingsChecks.csproj` with
Visual Studio MSBuild, then run the resulting executable with the built
`TAOM_CheatPanel.dll` path as its sole argument.

The historical standalone DLL and manifest were installed under the live
`Modules/TAOM_CheatPanel` directory. SHA256 and UTC timestamp verification pass.
The replaced files are backed up under
`outputs/taom-cheat-panel-20260927/backups/20260927-010136/`.
Sources, destinations, hashes and verification are recorded in
`outputs/taom-cheat-panel-20260927/standalone-deployment.json`.
This supersedes the earlier Hub-dependent candidate and installation.

Next: launch a disposable native campaign with Cheat Panel enabled and the TAOM
suite disabled. Check the panel, settings persistence, cancellation, one daily
passive adjustment, and save/reload. Runtime sign-off has not been performed.
Earlier full-suite build/deploy failures are separate from this standalone build.
