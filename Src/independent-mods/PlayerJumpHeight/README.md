# Player Jump Height 0.1.0

Singleplayer player-character jump adjustment for Bannerlord 1.4.8. Default **5x height**, configurable from **1x to 10x** through **Mod Options -> Player Jump Height**, with an enable switch.

Settings apply when your character next spawns: leave and re-enter the scene or battle after changing them. NPC and mount spawn values are unchanged. This targets your player character, not troops you temporarily possess through another mod. Fall damage, obstacles and ceilings still apply.

Requires the existing **TAOM.Dependencies** module supplying Harmony and MCMv5. This package does not bundle those dependencies. Extract the ZIP's PlayerJumpHeight folder into Modules, then enable Player Jump Height after its dependencies in the launcher. Start the game yourself.

The 5x setting uses a square-root launch-strength conversion. Offline checks validate selection, settings and patch binding; **the actual height ratio and in-game behavior still need native testing**. Compare standing jumps at 1x and 5x on flat ground in fresh scenes with the same equipment. Also verify MCM persistence, NPC/horse jumps, landing, disable/re-entry and save/reload.

Build and run offline checks: `powershell -ExecutionPolicy Bypass -File ./Build-Package.ps1`.

No campaign save data is added. To uninstall, disable/remove this module and enter a fresh scene. Source compatibility beyond 1.4.8 and native save/reload have not been qualified.
