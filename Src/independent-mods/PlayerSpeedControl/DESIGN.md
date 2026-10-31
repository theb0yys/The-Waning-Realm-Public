# Player Speed Control

State: implementing (offline validated and deployed; native acceptance pending). User authorized implementation on 2026-10-10 after correcting
the scope to RUNNING ONLY plus campaign map travel. This owner contains all code,
MCM settings, manifest, tests and packaging. No suite-owned runtime is changed.

## Contract and acceptance

- Two independent enabled toggles and 0.5x-3x sliders, default 1x, saved by MCM v5.
- Only the active player human on land and on foot receives the running multiplier.
  Walking, mounts, swimming and NPCs retain normal properties. Combat running uses
  the same native maximum-speed property; animation/acceleration remain native.
- Changes refresh within 0.2 seconds plus a mission tick or normal map speed refresh.
- Land travel multiplies the main party's completed default speed calculation,
  retaining existing explanations and adding a named modifier. Native floor effects
  are included before multiplication: a normal minimum speed of 1 can become 0.5.
- Attached army followers obey their leader; naval movement is outside this release.
- No campaign behaviors, SaveableTypes, SyncData, troop/XML or save-schema changes.
  Global MCM configuration is the sole persisted data, isolated under PlayerSpeedControl.
- Defaults/disable restore normal calculation. Invalid nonfinite settings become 1x;
  finite values are clamped. Removal leaves no mod-owned campaign objects.

## Research and integration

Path: user scope -> root budget-first/protected policy -> native decompilation ->
this owner -> targeted Release tests/package -> owner-only deployment -> user-started
native acceptance. Skills applied: context-budget, research-sentinel,
contract-save-compatibility-gates, test-gap-enforcer. No protected data is edited.

Installed Native manifest: v1.4.8. ILSpy 10.1.0.8386 inspected installed game
assemblies, whose FileVersion is 1.0.0.0 (not the game release version).
Evidence: Artifacts/player-speed-control-20261010/{Agent,AgentDrivenProperties,
SandboxAgentStatCalculateModel,DefaultPartySpeedCalculatingModel,ExplainedNumber}.cs.
MountAndBlade SHA256: 19387F31557FF840D14F378F6BBDF1D58FCFF406AB9FF2294DFBB3E49A50B87E.
CampaignSystem SHA256: 1F8E33E2ED73E6EC653D7629180AFB70649DDC6E5BD1657A802A264EFDA1C3AE.

- Agent.WalkMode queries native state. Agent.UpdateAgentProperties invokes
  AgentDrivenProperties.UpdateDrivenProperties, then sends its backing array native.
- SandboxAgentStatCalculateModel recalculates MaxSpeedMultiplier from normal
  environment/equipment/perk state. Our prefix removes its own previous adjustment;
  postfix modifies the freshly calculated property before native submission.
- Mission tick observes one player only and recalculates on a factor/control change.
- DefaultPartySpeedCalculatingModel.CalculateFinalSpeed includes terrain, weather,
  perks and minimum-speed clamp. ExplainedNumber factors add together; flattening
  the completed value prevents applying the wrong multiplier to existing factors.
- MCM attributes/project/manifest follow the adjacent CampaignMapVisibility owner.
  Dependencies come from installed TAOM.Dependencies; this package ships none of them.

## Compatibility, performance and failure boundaries

New module/config IDs; no old consumer/schema to migrate. Target is the inspected
1.4.8 installation. Custom party-speed models that replace the default method may
bypass this patch; no compatibility with arbitrary replacement models is claimed.
Other postfixes can change results after ours. Native script-imposed speed caps
may still limit movement. These require runtime observation, not invented hooks.
Harmony target failure blocks module initialization rather than silently claiming
working controls. No filesystem IO, reflection, logging, scans or LINQ in hot paths.
Running work is O(1) per tick and per native stat update; no new per-tick allocations.
Map work is O(1) without descriptions; tooltip flattening is O(native explanation
line count). No mission-wide or party-wide iteration. Medium performance risk;
native frame timing is unmeasured.

## Validation and release

Run `powershell -NoProfile -ExecutionPolicy Bypass -File ./Build-Package.ps1`.
Targeted tests cover every running eligibility combination, exclusions, malformed
settings, repeated scaling/restoration and competing value changes, actual installed
ExplainedNumber factors/floors/tooltip retention, defaults and patch binding.
Preflight has no registered system-catalog/runtime console lane for this new owner;
local owner tests do not close that gap or constitute native sign-off.

Native acceptance pending (user alone starts Bannerlord): load module and MCM;
run/walk a fixed distance at 0.5/1/3x in a town and battle; toggle walking repeatedly;
mount/dismount, swim, change controlled hero, die/restart/leave/re-enter mission;
verify an NPC remains unchanged; alter equipment; change settings during play and
disable both controls; compare map tooltip/movement at 0.5/1/3x, army attachment,
night/terrain, reload campaign and restart application to check MCM persistence.

Only PlayerSpeedControl.dll and its SubModule.xml are live deployment targets.
Back up any existing targets and compare fresh source/build/live SHA256/timestamps.
No game or launcher start is authorized. Rollback: restore backed-up owner files or
disable this new module. Preserve all unrelated dirty work in this checkout.

Acceptance: source, Release build (zero warnings/errors), 126 offline owner checks,
manifest assertions and package passed. Owner-only live deployment passed with
matching SHA256 and timestamps for DLL and manifest. This was a new installation;
no existing owner files required backup. Receipt:
Artifacts/player-speed-control-20261010/deploy-20261010-022526-011/receipt.json.
MCM persistence, loader, native movement and save/reload acceptance remain pending.
See VALIDATION.json. Next researched stop is the user-started native checklist above.
