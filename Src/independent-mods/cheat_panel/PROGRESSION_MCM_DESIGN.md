# Cheat Panel: MCM and player progression

Historical design record: references to Utilities Hub extraction checks and local evidence describe the original workspace. That cross-project test and its local evidence reports are not included in the public source repository.

- State: implementing; implementation, local checks and approved panel-only
  deployment complete. Native acceptance pending.
- Owner: `core/TAOM_CheatPanel`; reviewed revision 1, 2026-10-07.
- Authorization: add Take both perks as an MCM option, plus both existing upkeep
  cheats and XP/attribute/focus progression options.
- Path: root AGENTS/process stack -> standalone panel README and installed
  native/MCM decompilation -> Cheat Panel settings and campaign behavior -> local
  tests, Release build/deploy -> disposable campaign and save/reload acceptance.

## Authority and ownership

ILSpy 10.1.0.8386 inspected installed Native v1.4.8, CampaignSystem and MCM 5.12.1.
Assembly paths, SHA256 and decompiled types are in
`Artifacts/outputs/cheat-panel-progression-20261007/` (local evidence).
`HeroDeveloper.AddPerk` sets native hero state; `Hero.SetPerkValueInternal`
publishes PerkOpened only on a change. `SelectPerks` checks RequiredSkillValue
and AlternativePerk. `AddSkillXp` applies raw XP to level XP before skill/focus
conversion. `OnGainLevel` publishes HeroLevelledUp after native point awards.
The native HeroDeveloper remains the only progression-state owner.

MCM BaseSettingsContainer registers fluent settings with an unmatched format
without loading/saving a second file, then emits SAVE_TRIGGERED on Apply.
Use a unique owner format and ProxyRef properties; save through the existing
panel XML store only on Apply. The installed MCM SettingsVM edits live refs
through its undo stack; ModOptionsVM.ExecuteCancelInternal calls UndoAll.
Cancelling therefore restores prior values without saving. Runtime verification
of that UI remains required; copied refs apply to presets, not the live editor.
No remaining API research question requires a brief; native validation is separate.

The user's explicit MCM request supersedes the README's native-only dependency
choice. Require Bannerlord.MBOptionScreen and Bannerlord.Harmony in the manifest;
reference their APIs without packaging copies. On this machine MCM is supplied
by TAOM.Dependencies through the installed alias. Do not edit that external pack.

## Behavior and compatibility

- One XML settings owner; existing settings/defaults and native Settings menu
  remain valid. Add TakeBothPerks=false, PlayerXpMultiplier=1 (1..10),
  ExtraAttributePointsPerLevel=0 and ExtraFocusPointsPerLevel=0 (0..10).
- Master Enabled gates progression. Daily upkeep retains its existing separate
  passive master. MCM exposes both masters and the existing daily upkeep values.
- Take both perks affects the main hero only. Grant both paired perks whose
  individual skill requirements are met; never unlock unearned tiers or NPC perks.
  Reconcile on session launch and the first campaign hour after enabling; react
  to skill gains and perk selection. No repeated additions or per-tick scans.
- A narrow Harmony prefix multiplies positive normal main-hero skill XP only
  (isAffectedByFocusFactor=true); explicit panel XP commands remain exact.
  Disabled/default/invalid XP passes through. Bonus points apply once per native
  level event, without retrospective awards. Native save data persists all gains.
- Disabling/removing cheats does not undo earned/granted perks, XP or points.
  No custom SyncData, save IDs or migration; old XML gets inert new defaults.
  Old saves and removal still need a disposable native save/reload check.
- MCM UI only edits configuration. No suite UI assets, shared contracts or
  gameplay changes in other modules. No new game startup is permitted.

## Tests and acceptance

1. Extend owner managed checks: defaults/clamps, XML compatibility, main-hero
   gating, paired/under-level/singleton perks, idempotence, reentrant perk events,
   level awards and XP arithmetic. Test event lifecycle through explicit doubles;
   label it managed simulation, never native proof.
2. Static package and UI checks: exact dependencies, compile entries, MCM rows,
   single config owner, Apply-only persistence, native menu entries.
3. Release build with VS MSBuild; run SettingsChecks with built DLL argument and
   `python Src/independent-mods/cheat_panel/tests/validate_extraction.py`.
4. Performance: O(1) XP/level/perk-selection handlers; one O(P) paired-perk scan
   on skill gain/session/hourly reconciliation, no IO/reflection in progression handlers. Check
   bounded operation counts with a large synthetic perk set and measure XP path.
5. Full-suite build/deploy gate still applies. Unmapped helper owner/catalog and
   unresolved whole-suite project inventory must be reported as partial/blocked;
   targeted checks do not substitute for suite/runtime evidence.
6. User-started disposable campaign: MCM Apply/Cancel and restart persistence;
   both earned perks; NPC isolation; future level/XP awards; disable/re-enable;
   save/reload and panel menu/upkeep regression. Native acceptance pending.

## Preservation and rollback

Preserve all unrelated dirty files and existing panel actions. External overhaul
and dependency data is read-only. Back up any replaced live panel files before
deployment and verify SHA256/timestamps. Roll back settings/DLL/manifest together;
use a pre-cheat save to undo campaign mutations. No GitHub/release action requested.
Evidence pack and next stop: `PROGRESSION_MCM_EVIDENCE.json`, then the native
acceptance sequence above once build/deployment gates permit it.

## Current handoff

Release build and 54 managed checks pass. Static pack: 7 pass, 4 existing
Utilities Hub extraction assertions fail. The candidate ZIP, hashes, command
logs and reviewed installer are under the local evidence directory above.
The user approved panel-only deployment on 2026-10-07. The DLL and manifest are
installed with matching SHA256 and timestamps; backups are verified under the
live module's `.codex-backups/progression-20261007-161805/`. The original Vortex
manifest symbolic link was moved into that backup; its external target remains
unchanged. See `deployment-receipt.json` and `deployment-verification.json` in
the local evidence directory. Other modules were not deployed. Next is the
user-started disposable campaign acceptance above; no game was started by Codex.
