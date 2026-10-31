# Player Jump Height

- State: implementing; offline validation passed, native qualification pending.
- Owner: `Src/systems/Non-suite-suite-systems/PlayerJumpHeight`.
- Authorization: player jump height default 5x, configurable in MCM; expanded research approved 2026-10-10 (30 sources / 80 calls, no agents).
- Controlling path: root budget rules -> protected policy / research-first / compatibility gate -> installed 1.4.8 spawn flow -> owner tests/package -> user-started native height and isolation checks.
- Revision: initial 0.1.0.

## Scope and integration

Standalone singleplayer module using the existing TAOM.Dependencies MCMv5 and Harmony provider, matching PlayerSpeedControl's project and settings conventions. No suite changes. MCM owns JSON settings; this owner modifies only the temporary AgentSpawnData passed to native creation. Native movement, collisions, animation and fall damage remain game-owned.

MCM: enabled by default, height multiplier 5, range 1-10. Read settings at spawn. Apply to the campaign player character, identified by non-null identity with Game.Current.PlayerTroop, and humanoid flags only. Mounts and other characters remain unchanged. Changes take effect on the next scene entry/spawn, including disable and 1x. This does not dynamically follow possession of a different troop. Existing spawned agents retain their initial value.

## Local research and decision

ILSpy 10.1.0.8386 inspected installed assemblies under `A:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client`; Native/SubModule.xml reports v1.4.8 (assembly file versions are only 1.0.0.0).

- TaleWorlds.MountAndBlade.dll SHA256: 19387F31557FF840D14F378F6BBDF1D58FCFF406AB9FF2294DFBB3E49A50B87E.
- TaleWorlds.Core.dll SHA256: 3E5CECE8CA42FAF3FDDE27632519ED2A6F86D1EC6D1A742867729F00001689BF.
- MCMv5.dll SHA256: BE8FDE024957C97F0CF08ED813D2EF0A069F1D3E3F4C54F4557DA4083CBA3B15.
- `Mission.SpawnAgent` -> private `CreateAgent(Monster,...,BasicCharacterObject)` -> `monster.FillSpawnData(null)` -> private `CreateAgentInternal(...,ref AgentSpawnData,...)` -> IMBMission.CreateAgent.
- `FillSpawnData` copies Monster.JumpAcceleration into a value-type spawn record; no shared Monster mutation is necessary. The player identity convention is also used in Agent.IsPlayerTroop and Mission.SpawnAgent.
- Exact decompilation: `ilspycmd -t TaleWorlds.MountAndBlade.Mission <game-bin>/TaleWorlds.MountAndBlade.dll`; also Agent, IMBAgent, AgentSpawnData and TaleWorlds.Core.Monster. Local generated evidence under `Artifacts/player-jump-height-20261010`.

Harmony brackets CreateAgent with thread-local scope and restores it in a finalizer, including exceptions and nested spawns. CreateAgentInternal changes only JumpAcceleration, once per scope, after checking humanoid flags. No per-frame code, agent collection scans, IO or reflection in the spawn adjustment. Patch binding occurs at module load and is rolled back if incomplete.

The height setting scales launch strength by sqrt(height multiplier), using ballistic h proportional to v squared. This is a physics-based conversion, not native measurement. The managed trace proves the per-agent field reaches native creation; the native engine's precise conversion and resulting apex remain an execution acceptance gate. We do not use the ambiguous AddAcceleration impulse API. A native mismatch blocks a release claim and requires calibration/research before changing the conversion.

## Compatibility and preservation

New module/settings IDs PlayerJumpHeight; JSON2 global settings, no campaign save objects or XML patches. No existing schema changes or migrations. Installation/removal should leave campaign data unchanged, but native save/reload acceptance is pending. Dependency files are references only and are not packaged or deployed. Unknown future game APIs fail patch setup instead of silently skipping. Exact game compatibility beyond 1.4.8 is unqualified.

No protected external data or existing dirty work is changed. New owner source only. Package rollback: remove this module; enter a fresh scene to restore native spawn parameters. Never start the game automatically. Current authorized stopping point is package and offline validation; no live deployment in this turn.

## Acceptance

1. Offline: eligibility truth table, sanitization, sqrt conversion, nested scope/exception cleanup, one application per spawn, unrelated fields unchanged, MCM defaults/schema, installed Harmony signatures. Run `powershell -ExecutionPolicy Bypass -File ./Build-Package.ps1` from owner directory.
2. Static: manifest identity/dependencies, packaged DLL hash and ZIP entries; no third-party DLL payload.
3. Native (pending): user enables module after TAOM.Dependencies, sees MCM controls, saves/reopens settings, enters fresh scenes at 1x and 5x with same character/equipment on level ground. Measure apex minus takeoff Z; target ratio approximately 5, tolerance 10% for observation/frame sampling. Check NPC and horse jumps unchanged, mounted player dismounts into adjusted foot jump, combat/town scenes, disable then re-enter, landing/collision and fall damage, save/reload. No new character/campaign required by implementation.
4. Catalog: no registered owner lane existed in Get-AgentTestPlan; explicit owner command above supplies offline checks but does not claim catalog registration or native coverage.
5. Performance: preflight flags Harmony as high risk. The inspected adjustment is O(1) per spawn, with thread-local value state and arithmetic; no per-frame path. Temporary engineering ceiling is 1 ms per 1,000 managed spawn adjustments (one million repetitions must finish below 1,000 ms). The owner test records elapsed time, Release configuration, CLR, OS and CPU. This isolates the managed adjustment; native spawning, Harmony dispatch and MCM provider cost are not included and require runtime acceptance. No claim of improvement over an earlier version.

Next researched gate: user-started native comparison. Offline pass is not native runtime sign-off.

## Current evidence

Release build passed with no warnings/errors; 59 offline checks passed. One million managed adjustments took 10.42 ms against the provisional 1,000 ms ceiling. Manifest identity, three-file owner ZIP and SHA256 checks passed. See `VALIDATION.json` and `Release/build-receipt.json`. Deployment and native gameplay were not run; no protected files were touched. The package is a native-test candidate, not a verified 5x gameplay release.
