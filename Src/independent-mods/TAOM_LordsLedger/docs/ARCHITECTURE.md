# Architecture

## Entry Point

`Source/Entry/SubModule.cs` registers `LedgerCampaignBehavior` for campaign games.

## Campaign Behavior

`Source/Behaviors/LedgerCampaignBehavior.cs` owns:

- Hotkey `L`.
- Gauntlet board open/fallback menu flow.
- Game-menu controls.
- Daily warning scan.
- Governance pulse.
- Loyalty history snapshots.
- Save persistence through `SyncData`.

Persisted save keys are prefixed with `ledger_`.

## Data Reader

`Source/Services/LedgerDataReader.cs` reads Bannerlord settlement state and builds `FiefInfo`.

It computes:

- Type labels.
- Loyalty trend.
- Garrison, militia, wall level, governor, construction, bound villages, and hero prisoners.
- Nearby hostile-army danger.
- Warning state.
- Risk score.
- Governance score.
- Primary issue and action hint.

The reader is defensive and returns safe defaults when TaleWorlds APIs are unavailable for a settlement.

## Renderer

`Source/Services/LedgerRenderer.cs` builds:

- Text report for the fallback menu.
- `LedgerBoardSnapshot` for the Gauntlet UI.
- Sorted and filtered warned/clean groups.
- Export file contents.

## Settings

`Source/Configuration/LedgerSettings.cs` owns sort/filter/view/scope state and threshold presets.

## UI

`UI/LedgerBoardScreen.cs` opens the Gauntlet screen.

`UI/ViewModels/LedgerBoardVM.cs` and `LedgerFiefItemVM.cs` expose board commands and fief cards to:

```text
GUI/Prefabs/TAOM_LordsLedger.xml
```

The Gauntlet movie name remains `TAOM_LordsLedger`.
