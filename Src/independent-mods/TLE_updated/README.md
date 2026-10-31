# Waning Troop Limit Expander

## v2.2.13 for Bannerlord v1.4.7

Troop Limit Expander raises the Custom Battle per-side roster ceiling to 25,000
and stages very large battles through controlled active-agent caps and
reinforcement waves.

Module ID: `WANINGTROOPLIMITEXPANDER`

## Current Defaults

| Setting | Default |
|---|---:|
| Custom battle troops per side cap | 25,000 |
| Active field cap | 1,600 |
| Active siege cap | 1,200 |
| Active-agent safety cap | 1,800 |
| Reinforcement waves | 50 |
| Wave batch percent | 4% |
| Reinforcement interval | 6 seconds |
| Active battle-size expansion | Off |
| Crash guard | On |
| Battle telemetry overlay | On |

The 25,000 cap is for reserve roster scale. Active-agent expansion is disabled
by default so Bannerlord does not try to place the full roster on the field at
once.

## MCM Settings

This package registers with Mod Configuration Menu v5 through the MCM runtime
integrated into `TAOM.Dependencies`. The same researched defaults are preserved,
but they can now be changed from MCM.

Important runtime toggle:

- `Show in-battle overlay`: adds or removes the battle telemetry overlay during
  a mission.

## UI Notes

The Custom Battle army-size input is patched with Harmony at runtime. This mod
does not override SandBoxCore's `ArmyComposition.xml` and does not use
UIExtenderEx for the Custom Battle screen.

The Custom Battle army-size field is widened independently of the active troop
cap, so conservative first-battle caps do not clip the displayed number.

The battle overlay initializes after the mission screen is available and fails
closed if Gauntlet rejects the layer during mission bring-up.

## Dependencies

Load after:

- Native
- SandBoxCore
- Sandbox
- CustomBattle
- TAOM.Dependencies
- TAOM

Harmony and MCMv5 references are provided by `TAOM.Dependencies` for the runtime
patches and settings registration.

## Fixes in v2.2.13

- Restored MCM settings for the TLE runtime profile.
- Added a live MCM toggle for the in-battle telemetry overlay.
- The overlay mission behavior now stays available and attaches/detaches the
  Gauntlet layer based on the MCM setting.

## Fixes in v2.2.12

- Kept the battle overlay visible when the Custom Battle mission does not expose
  `DefaultBattleMissionAgentSpawnLogic`, falling back to live mission agent
  counts for total, active, left, deaths, and player kills.

## Fixes in v2.2.11

- Restored the Custom Battle per-side roster ceiling to `25,000`.
- Removed the custom battle cap clamp to the active field cap while keeping the
  researched first-battle active-agent staging profile.

## Fixes in v2.2.10

- Widened the Custom Battle army-size input by `CompositionGroup` location
  instead of depending on the `MaxInt` binding order.

## Fixes in v2.2.9

- Widened the Custom Battle army-size input even when the crash guard keeps the
  roster cap below five digits.
- Re-enabled the battle telemetry overlay with guarded mission-screen
  initialization and added total units, deaths, and player kills.

## Fixes in v2.2.8

- Disabled the battle overlay by default and capped custom battle rosters to
  the active field cap to avoid the TftAoM mission-init loading loop.

## Fixes in v2.2.7

- Removed external settings registration and dependency for the vanilla TftAoM package.

- Added required `TAOM.Dependencies` and `TAOM` module dependencies plus
  load-order metadata for the TftAoM package.

## Ship Checks

- Builds against Bannerlord v1.4.7 references.
- No Custom Battle XML prefab override is shipped.
- MCM settings are registered through the runtime integrated into `TAOM.Dependencies`.
- The battle overlay is guarded so it can fail closed without blocking battle load.
