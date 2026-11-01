# User Guide

## Opening The Board

Press `L` on the campaign map to open Lord's Ledger. Press `Esc` to close the board.

The board tracks fiefs owned by your clan by default. Toggle scope to include kingdom vassal fiefs.

## Controls

| Control | Effect |
| --- | --- |
| Sort | Cycles name, loyalty, prosperity, food, garrison, and risk sorting |
| Filter | Cycles all, towns, castles, and villages |
| Warnings | Shows all fiefs or warnings only |
| Thresholds | Cycles normal, strict, and relaxed warning presets |
| View | Cycles detailed and compact report views |
| Scope | Cycles my clan and kingdom |
| Export | Writes the current report to a text file |
| Refresh | Re-reads settlement data |

## Warning States

The Ledger flags:

- Active sieges.
- Thin garrisons with a nearby hostile army.
- Raided or looted villages.
- Loyalty below the active threshold.
- Security below the active threshold.
- Food change below the active threshold.
- Garrison below the active threshold.

## Governance Pulse

The governance pulse runs periodically and reports:

- Number of tracked fiefs.
- Average readiness.
- Warning count.
- Total reinforcement need.
- Highest-priority fief and recommended action.

The default pulse interval is 7 in-game days.

## Export

Use `Export` to write:

```text
Documents\Mount and Blade II Bannerlord\LordsLedger_export.txt
```

The export uses the active sort, filter, warnings-only, threshold, view, and scope settings.
