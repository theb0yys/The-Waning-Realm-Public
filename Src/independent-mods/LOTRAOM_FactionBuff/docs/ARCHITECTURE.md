# Architecture

## Entry Point

`src/SubModule.cs` loads XML culture data packs, applies Harmony patches, and registers `FactionBuffMessageBehavior` for campaign games.

## Morale Patch

`src/Patches/PartyMoralePatch.cs` patches `DefaultPartyMoraleModel.GetEffectivePartyMorale`.

The postfix is player-party only. It adds:

- Tiered food morale.
- Peak-season morale.
- Alignment-based prisoner morale.

Food tier results are cached per campaign day to avoid repeated item-roster scans during UI morale recalculation.

## Daily Evaluation

`src/Evaluation/DailyStateEvaluator.cs` creates the party state used by both messages and doctrine scoring.

It evaluates food tier, prisoner state, season, party scale, terrain, hero context, allied army cultures, political status, renown tier, and wounded state.

## Messages And Doctrine

`src/Behaviors/FactionBuffMessageBehavior.cs` owns campaign events, daily pacing, message selection, doctrine menu options, doctrine state persistence, and doctrine-board snapshots.

Persisted save keys are prefixed with `FactionBuff_`.

## Static Data

`src/Data/FactionData.cs` defines culture profiles, message key pools, colors, thresholds, and morale constants.

`src/Data/FactionBuffSettings.cs` owns doctrine mode and multiplier settings.

`src/Data/FactionBuffDataPackLoader.cs` applies XML profile overrides from `ModuleData`.

## UI

`src/UI/DoctrineBoardScreen.cs` opens the Gauntlet screen.

`src/UI/ViewModels/DoctrineBoardVM.cs` and `DoctrineSignalItemVM.cs` expose board state to the prefab:

```text
GUI/Prefabs/LOTRAOM_FactionBuff.xml
```

The Gauntlet movie name remains `LOTRAOM_FactionBuff`.
