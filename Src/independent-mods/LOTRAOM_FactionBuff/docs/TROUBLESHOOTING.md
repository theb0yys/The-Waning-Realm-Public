# Troubleshooting

## Module Does Not Appear In Launcher

Confirm the installed folder is:

```text
<Bannerlord install>/Modules/LOTRAOM_FactionBuff/
```

and contains `SubModule.xml` at the module root.

## Crash Or Load Failure

Check load order:

```text
Native
SandBoxCore
Sandbox
TAOM
TAOM.Dependencies
LOTRAOM_FactionBuff
```

Confirm the module was built against Bannerlord `v1.4.15` assemblies.

## No Morale Line Appears

Check:

- The player party culture ID exists in `src/Data/FactionData.cs` or a data pack.
- The player party carries the profile food item.
- Doctrine effects are not suspended.
- The party is the main player party. AI parties are intentionally ignored.

## No Daily Messages Appear

Daily messages are paced to one major faction signal per in-game day. Some signals require state changes, such as terrain changes, political changes, renown tier increases, or a new victory.

## Doctrine Board Does Not Open

Use the camp, town, castle, or village menu option `Open faction doctrine board`.

If the Gauntlet screen fails, the module falls back to an inquiry panel or campaign feed status.

## Data Pack Does Nothing

Check:

- XML root is `FactionBuffDataPacks`.
- Culture nodes are under `/FactionBuffDataPacks/Culture`.
- The `cloneFrom` profile exists.
- The profile food item exists in loaded item data.
- Key buckets point to valid GameText IDs.
- Files are installed under `ModuleData/FactionBuff_DataPacks.xml` or `ModuleData/FactionBuff/*.xml`.
