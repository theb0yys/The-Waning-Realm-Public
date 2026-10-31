# User Guide

## Opening the Signal Board

Press `F9` on the campaign map to open the signal board. Press `Esc` to close it.

If the Gauntlet screen cannot open, the module falls back to campaign feed messages.

## What Messages Cover

The module listens to campaign events and ambient campaign state. It can show messages for:

- Battles, raids, sieges, war, peace, ownership changes, and kingdom destruction.
- Army creation and dispersal.
- Hero deaths, prisoners, fugitives, clans, companions, marriages, children, and wounded state.
- Tournaments, hideouts, quests, caravan activity, settlement entry, route arrival, supplies, morale, travel, season changes, and weekly realm pulse.
- Optional TAOM bridge signals when supported TAOM systems are present.

Messages are paced by cooldowns and hourly budgets so the campaign feed does not flood.

## Stance And Focus

Settlement, camp, and encounter menus add controls for signal stance and focus. These settings are saved with the campaign.

Stance changes how actively the module reports signals. Focus changes which signal categories are emphasized.

## Data Packs

Extra message lines can be installed under:

```text
ModuleData/ContextualMessages_DataPacks.xml
ModuleData/ContextualMessages/*.xml
```

See `docs/DATA_PACKS.md` for the accepted XML format and pool naming rules.

## Troubleshooting

If `F9` does not open the board, confirm the module is loaded after TAOM and that the installed folder is:

```text
<Bannerlord install>/Modules/Vanilla_ContextualMessages/
```

For detailed checks, see `docs/TROUBLESHOOTING.md`.
