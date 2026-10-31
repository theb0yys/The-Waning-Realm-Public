# No Fall Damage

- State: implementing; offline validation passed, native acceptance pending.
- Owner: `Src/systems/Non-suite-suite-systems/NoFallDamage`.
- Authorization: user requested another mod that eliminates all fall damage, 2026-10-10.
- Research path: root budget-first rules -> protected policy / research-first / existing compatibility and performance gates -> installed fall callback -> owner tests and package -> user-started native fall/ordinary-damage checks.

## Scope and ownership

A separate singleplayer module, always active when enabled in the launcher. Blocks standard fall damage for every agent, including the player, NPCs, horses and riders. No MCM setting requested or needed. No dependency on PlayerJumpHeight; works alongside it. Native gravity, landing movement and non-fall combat damage remain game-owned.

Only new owner files change. Reuse the neighboring PlayerJumpHeight build/package convention and installed TAOM.Dependencies Harmony provider; do not copy or deploy provider DLLs. No campaign objects, configuration files, public APIs or save schema changes. Disable/remove the module and restart the game to restore fall damage. Native save/reload compatibility remains untested.

## Verified local authority

Installed Bannerlord 1.4.8 was established in the preceding owner work. SHA256 of `A:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.dll` was freshly checked: `19387F31557FF840D14F378F6BBDF1D58FCFF406AB9FF2294DFBB3E49A50B87E`, matching the retained ILSpy 10.1.0.8386 decompilation in `Artifacts/player-jump-height-20261010/MountAndBlade.decompiled.cs`.

- `Mission.FallDamageCallback(ref AttackCollisionData, Blow, Agent, Agent)` is an engine callback. It calculates collision damage, registers a blow when positive, then recursively calls itself for a mount's rider (retained decompilation lines 58317-58341).
- `ComputeBlowMagnitudeFromFall` calculates native fall damage from vertical speed, mass, encumbrance and fall parameters (77501-77529).
- `Agent.HandleBlow` subtracts positive inflicted damage from health and marks fall deaths as Gravity. Avoid changing this shared combat path.
- AttackCollisionData exposes writable BaseMagnitude, InflictedDamage and SelfInflictedDamage. Zero those outputs and skip the dedicated fall callback, preventing both the first blow and rider recursion.
- `GameNetwork.IsMultiplayerOrReplay` reads managed game-type state. Bypass our suppression for multiplayer and playback so recordings/network authority are not rewritten.

This is an exact damage-callback interception, not a guessed physics change. No shared creature data, gravity parameters or general health/damage methods are patched. Another mod that independently subtracts health or kills an agent without this standard callback is outside this hook's coverage.

## Lifecycle and failure

Apply one Harmony patch at module load; roll back owner patches if binding fails. Remove only owner patches on unload. No per-agent state, mission behaviors, ticks, reflection/IO in the callback or global scans. Preserve non-damage collision fields. No protected external data or pre-existing dirty files may be edited. Unknown future API signatures fail setup; compatibility beyond 1.4.8 is not qualified.

## Acceptance

Run `powershell -ExecutionPolicy Bypass -File ./Build-Package.ps1` in this owner directory.

- Offline: invoke the real patched installed fall callback with missing agents, proving it returns before native/agent access; verify damage outputs zero for small, lethal and extreme values; preserve every other public field; multiplayer/replay bypass; exactly one patch target; unpatch cleanup.
- Performance: helper classifies Harmony as high risk. Expected O(1) managed callback; temporary ceiling 1,000 ms per one million patched calls (1 ms per 1,000). Record Release x64 runtime/machine and elapsed result. This does not measure native physics or scene performance.
- Static package: correct manifest, dependency, singleplayer flag, exact three-file ZIP, matching staged DLL and input hashes. Missing central owner test catalog entry is explicit; the owner executable supplies a direct local lane.
- Native pending: user starts the game; enable No Fall Damage after TAOM.Dependencies. Compare health before/after short and lethal falls, player/NPC/horse/rider and a 5x jump. Confirm melee, arrows, fire and drowning still deal damage, landing/collision work, and save/reload and module disable behave normally.

No game is started by agents. Package and offline proof do not close native gameplay, live deployment or release acceptance. Next researched gate is that user-started native checklist.

## Current results

Release build passed with 0 warnings/errors; 52 offline checks passed, including invocation of the installed patched callback, multiplayer/replay routing, preservation of unrelated collision fields and unpatch cleanup. One million patched callbacks took 16.99 ms against the 1,000 ms ceiling. Package layout, staged DLL and final input hashes passed. See `VALIDATION.json` and `Release/build-receipt.json`. No protected files changed. Live installation and native gameplay checks have not run.
