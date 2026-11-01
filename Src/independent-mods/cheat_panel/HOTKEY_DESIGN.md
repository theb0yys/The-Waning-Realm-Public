# Cheat Panel configurable opening shortcut

Historical design record: references to Utilities Hub extraction checks and local evidence describe the original workspace. That cross-project test and its local evidence reports are not included in the public source repository.

- State: implementing; revision 1, 2026-10-08. Local validation and approved
  panel-only installation passed; native acceptance pending. Owner: `core/TAOM_CheatPanel`.
- User direction: add an MCM configurable hotkey to open the Cheat Panel.
- Path: root AGENTS/process stack -> existing PROGRESSION_MCM_DESIGN and installed
  input/menu/MCM decompilation -> panel settings/input adapter -> owner checks,
  Release build/deploy -> user-started native campaign acceptance.

## Scope, ownership and compatibility

Add OpenPanelKey (default F10) and OpenPanelRequireControl (default true) to the
existing XML settings owner. MCM General offers Disabled, function keys, letters,
digits and navigation keys plus the Ctrl toggle. Existing XML retains defaults;
unknown keys disable the shortcut. No campaign save data or cheat effects change.
The shortcut only opens; it does not toggle or repeat while held. Existing manual
settlement entries remain available, including when cheats are disabled.

Reuse registered panel menus. Open with ActivateGameMenu on the free map and
SwitchToMenu from town/castle/castle_outside/village. Reject other menus, combat,
captivity, conversations, loading and overlays. Preserve the originating menu on
Close, including castle_outside; a free-map opening exits menu mode. The existing
cheat master continues to govern actions, not access to settings.

## Research and integration

ILSpy 10.1.0.8386 inspected installed Native v1.4.8 GameMenu.ActivateGameMenu,
SwitchToMenu, GameMenuManager.ExitToLast, MapState, MapScreen, InputContext,
InputKey, CampaignUIHelper.GetMapScreenActionIsEnabledWithReason and MCM 5.12.1
ISettingsPropertyGroupBuilder, Dropdown, SettingsPropertyVM/SetSelectedIndexAction.
Local source and assembly identities: `Artifacts/outputs/cheat-panel-hotkey-20261008/`.
MCM dropdown undo mutates SelectedIndex on the referenced object, so maintain one
dropdown and sync its change event to the sole XML owner; Apply alone saves.
Use the map scene input context so native modal input restrictions remain active.
SandBox.View is a native Sandbox assembly, referenced without bundling or a new
module dependency. No unresolved API research question requires a brief.

## Acceptance and risks

1. Managed checks: XML old/default/roundtrip/unknown key; key selection and modifier
   rejection; accepted and rejected menu routes. Command: build SettingsChecks with
   VS18 MSBuild Release and run its exe with the fresh panel DLL.
2. Static owner checks: `python Src/systems/support/cheat_panel/tests/validate_extraction.py`.
   Preserve and report existing unrelated Hub extraction failures.
3. Build panel Release/x64. Full-suite gate remains required; helper catalog does
   not map cheat_panel and current whole-suite inventory is unresolved. Report
   gaps honestly; do not deploy stale or unidentified DLLs.
4. User-started campaign: default/rebound/disabled key, Ctrl option, held-key
   behavior, MCM Apply/Cancel/restart, free-map and each settlement return, panel
   submenus, inventory/encyclopedia/dialog/inquiry/mission/captivity guards.
5. Medium performance risk: O(1) cached key lookup and bounded state checks each
   application tick, no world scan, IO, reflection or allocation in idle polling.
   Native timing and UI behavior remain pending until observed.

Keep protected game/overhaul/dependency data read-only and unrelated dirty work
untouched. Back up live panel files before any permitted deployment; verify hashes
and timestamps. Rollback is the prior DLL and settings backup. No game startup,
GitHub operation, asset change or unrelated module repair is included.
Evidence: HOTKEY_EVIDENCE.json. Next gate: local validation, deployment eligibility,
then native acceptance above. Never label local simulation native proof.

## Current evidence

Release/x64 module build and 148 managed checks pass. Static checks: 8 pass and
4 pre-existing Utilities Hub extraction failures remain. The managed runner now
targets x64 to match the installed InputSystem assembly; its initial architecture
failure is resolved. Hotkey lookup guard: one million calls in 7 ms, below 5000 ms.
The user explicitly approved "Install Cheat Panel only", superseding the full-suite
build/deploy rule for this task. The one DLL was installed with matching SHA256
and UTC timestamp; the old DLL backup hash was verified. See HOTKEY_EVIDENCE.json
and the local deployment-receipt.json for exact paths. No manifest, dependencies,
other module, game process or protected files were changed. Next: user-started
campaign acceptance in step 4; local tests do not establish native UI behavior.
