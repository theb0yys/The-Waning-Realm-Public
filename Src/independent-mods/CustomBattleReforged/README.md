# Custom Battle Reforged

A standalone Custom Battle fix for TAOM 2.0.29.5. Version 0.1.2.

- Enter army sizes from **1 to 9,999 per side** without the three-digit text restriction.
- Select TAOM factions, including factions that reuse vanilla internal IDs.
- Variag (the `battania` culture) is excluded from Custom Battle, along with its troops and commanders.
- Select TAOM-authored soldiers and commanders with their own battle equipment.
- Full troop-tree variety by default: all eligible units across the faction's
  branches and tiers are selected in each formation. The native roster builder
  divides that formation's soldiers evenly across the selected types.
- Keeps TAOM's equipment choices, including any shared base-game items its author deliberately uses.

Changing faction and pressing Randomize restore the full eligible troop mix.
You can still deselect individual types manually. Formation percentages remain
under your control; a formation needs enough soldiers to include every selected
type at least once. Only authored, loaded, equipped troops can participate.

Enable **Custom Battle Reforged** after **CustomBattle**, **TAOM.Dependencies**
and **TAOM** in your launcher. The install folder is `CustomBattleReforged`.
Only this mod's DLL and manifest are required; no suite modules are deployed.
The ZIP contains that single module folder.

The army number is the total participating force, not a new simultaneous-agent
limit. Normal engine spawning/reinforcements still apply. This module has no
campaign save data; removing it restores the underlying custom-battle behavior.

Compatibility is pinned to the locally inspected TAOM, CustomBattle and Core
assembly hashes in `SubModule.cs`. Other builds show an activation error and
leave Reforged disabled. Metadata is read from the installed TAOM module;
TAOM XML, equipment and assets are neither copied nor edited by this mod.

## Validation

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File Src/systems/Non-suite-suite-systems/CustomBattleReforged/Validate.ps1
```

This builds Release, checks metadata/policy behavior against synthetic cases
and the installed TAOM data, checks parsing/lookup budgets, and installs then
removes the Harmony patches in a separate **headless** test process. It also
checks the owner manifest and creates a portable package with SHA256 evidence.
It does not start Bannerlord or its launcher and does not validate rendering,
native object loading, campaign transitions or actual battles.

After validation, owner-only deployment:

```powershell
powershell -ExecutionPolicy Bypass -File Src/systems/Non-suite-suite-systems/CustomBattleReforged/Deploy-OwnerOnly.ps1
```

Existing owner files are backed up before replacement; only the new DLL and
SubModule.xml are copied. The deployment receipt records both SHA256 hashes.
The user starts the game and follows the native acceptance checks in DESIGN.md:
type 1,000/9,999 on both sides, change factions, inspect troop/commander gear,
randomize, test field/village/siege battles and leave/reopen Custom Battle.
Native runtime sign-off remains pending until those checks are recorded.
