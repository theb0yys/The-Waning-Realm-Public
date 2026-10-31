# User Guide

## Daily Faction Signals

The module evaluates the player party once per in-game day. It selects one major faction signal for that day based on the current party state.

Priority order is:

1. Season transition.
2. Combined peak or dire state, including sustained streaks.
3. Food state.
4. Prisoner state.
5. Hero alignment conflict.
6. Terrain change.
7. Allied army culture change.
8. Battle victory.
9. Political status change.
10. Renown tier increase.
11. Wounded commander state.

## Food And Prisoners

Carry the culture profile's food item to gain a morale line. More food per soldier improves the tier from scarce to stocked to surplus.

Prisoners help or hurt morale depending on alignment:

- Good cultures prefer few prisoners.
- Evil cultures benefit from a strong prisoner column.
- Neutral cultures ignore prisoner morale.

## Doctrine Board

Use settlement or camp menu options to open the faction doctrine board. The board shows the latest strategic snapshot, estimated morale delta, supply state, prisoner state, readiness, and doctrine directive.

The board controls:

- Cycle doctrine mode: `Story`, `Balanced`, `Strict`.
- Toggle doctrine morale effects.
- Toggle strategic reports.
- Print a strategic summary.
- Refresh the board.

`Esc` closes the board.

## Doctrine Modes

| Mode | Morale multiplier | Use |
| --- | --- | --- |
| Story | x0.75 | Softer morale impact |
| Balanced | x1.00 | Default tuning |
| Strict | x1.25 | Stronger morale impact |

Doctrine settings are persisted per save.

## Data Packs

Modders can override or clone culture profiles through XML files under `ModuleData`. See `docs/DATA_PACKS.md`.
