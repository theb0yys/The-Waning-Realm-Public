# Troubleshooting

## Module Does Not Appear In Launcher

Confirm the installed folder is:

```text
<Bannerlord install>/Modules/TAOM_LordsLedger/
```

and contains `SubModule.xml` at the module root.

## Build Fails With BannerlordDir Error

Set `BannerlordDir` with one of:

- `BannerlordDir.props` next to the project.
- Environment variable `BannerlordDir`.
- MSBuild property `-p:BannerlordDir="..."`.

The value must point to the Bannerlord install root, not the `bin` folder.

## Pressing L Does Nothing

Check:

- You are on the campaign map.
- No campaign menu is currently open.
- The module is enabled and loaded.
- Another module is not intercepting the `L` key.

The module does not open the board while a campaign menu context is active.

## Board Fails To Open

The module should fall back to the ledger menu if the Gauntlet board fails.

Check that this file exists in the installed module:

```text
GUI/Prefabs/TAOM_LordsLedger.xml
```

## No Fiefs Are Listed

Check:

- Your clan owns at least one town, castle, or village.
- The active filter is not hiding the owned fief type.
- Warnings-only mode is off if no fiefs currently have warnings.
- Scope is set to `Kingdom` if you expect vassal-owned settlements.

## Export Fails

The export writes to:

```text
Documents\Mount and Blade II Bannerlord\LordsLedger_export.txt
```

Confirm Windows allows the game to create or overwrite files in that folder.
