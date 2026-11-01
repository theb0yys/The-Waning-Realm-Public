# TAOM Lord's Ledger

TAOM Lord's Ledger is a single-hotkey settlement dashboard for Mount & Blade II: Bannerlord. It shows fief risk, governance readiness, warnings, reinforcement needs, and exportable settlement reports.

This package is prepared for Bannerlord `v1.4.15`.

## Current Identity

| Item | Value |
| --- | --- |
| Launcher display name | `TAOM Lord's Ledger` |
| Module ID | `TAOM_LordsLedger` |
| Module folder | `TAOM_LordsLedger` |
| DLL | `TAOM_LordsLedger.dll` |
| Module version | `v2.1.0` |
| Assembly version | `2.1.0.0` |
| Target framework | `.NET Framework 4.7.2` |
| Platform | `x64` |

## What It Does

- Opens a fief dashboard with `L` on the campaign map.
- Adds a fallback game-menu ledger with sort, filter, threshold, view, scope, export, refresh, and close controls.
- Shows towns, castles, villages, warning state, governance score, risk score, reinforcement need, and action hints.
- Tracks loyalty trend from daily snapshots.
- Sends daily warning messages only when a fief newly crosses a warning threshold.
- Sends a periodic governance pulse summarizing readiness, warnings, reinforcement needs, and the current priority fief.
- Exports the current report to `Documents\Mount and Blade II Bannerlord\LordsLedger_export.txt`.

## Dashboard Data

Towns and castles show:

- Loyalty, security, prosperity, loyalty trend, food change, garrison, militia, governor, wall level, bound villages, active construction, notable prisoners, risk, readiness, primary issue, and recommended action.

Villages show:

- Hearth value, raided/looted state, readiness, and protection advice.

The summary bar shows:

- Visible fief count, warning count, readiness, town/castle/village counts, sieges, raids, field danger, critical count, total garrison, militia, and reinforcement need.

## Risk And Readiness

Risk is a 0-100 urgency score. Siege sets risk to 100. Thin garrison with a nearby hostile army sets risk to 90. Other risk comes from low loyalty, low security, food deficit, and weak garrison.

Readiness is the inverse governance score with extra adjustments for siege, field danger, loyalty trend, and active construction.

## Threshold Presets

| Preset | Loyalty | Security | Food | Garrison | Danger radius |
| --- | --- | --- | --- | --- | --- |
| Normal | below 30 | below 20 | below 0/day | below 40 | 20 map units |
| Strict | below 45 | below 35 | below 5/day | below 60 | 30 map units |
| Relaxed | below 15 | below 10 | below -10/day | below 20 | 15 map units |

## Requirements

- Mount & Blade II: Bannerlord `v1.4.15`.
- `TAOM.Dependencies` installed and loaded before this module.
- Base modules listed in `SubModule.xml`: `Native`, `SandBoxCore`, `Sandbox`.

The data reader includes TAOM culture labels and vanilla culture labels. The module does not use Harmony.

## Folder Structure

```text
TAOM_LordsLedger/
  UI/                 Ledger board screen and view models
  GUI/                Gauntlet prefab used by the board
  Source/             Entry point, behavior, settings, models, and services
  docs/               Public release, user, build, and modder documentation
  SubModule.xml
  TAOM_LordsLedger.csproj
```

Generated folders are not part of the public source package:

```text
.vs/
bin/
obj/
release/
```

## Build

From this folder:

```powershell
dotnet build ".\TAOM_LordsLedger.csproj" -c Release -p:Platform=x64
```

The legacy project requires `BannerlordDir` through `BannerlordDir.props`, an environment variable, or a command-line property.

See `docs/BUILD.md` for details.

## Install

Copy the player-ready module folder:

```text
TAOM_LordsLedger/
```

to:

```text
<Bannerlord install>/Modules/TAOM_LordsLedger/
```

See `docs/INSTALL.md`.

## Documentation

- `docs/INSTALL.md` - Player and developer install notes.
- `docs/USER_GUIDE.md` - Player-facing usage notes.
- `docs/BUILD.md` - Build requirements and commands.
- `docs/ARCHITECTURE.md` - Runtime structure and source boundaries.
- `docs/MODDER_NOTES.md` - Source-level extension notes.
- `docs/TROUBLESHOOTING.md` - Common failure cases.
- `docs/PUBLIC_RELEASE_CHECKLIST.md` - Release packaging checklist.

## Public Release Notes

This is a source package. It should not include compiled binaries, debug symbols, Visual Studio cache files, local Bannerlord path files, or generated build output.

For a player-ready release, package the built `Modules/TAOM_LordsLedger/` folder separately.
