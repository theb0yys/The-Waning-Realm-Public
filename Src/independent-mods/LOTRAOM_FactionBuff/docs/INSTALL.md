# Install Guide

## Player Install From Binary Release

Use the player-ready module folder:

```text
LOTRAOM_FactionBuff/
```

Copy it to:

```text
<Bannerlord install>/Modules/LOTRAOM_FactionBuff/
```

The final installed structure should include:

```text
LOTRAOM_FactionBuff/
  SubModule.xml
  bin/Win64_Shipping_Client/LOTRAOM_FactionBuff.dll
  GUI/Prefabs/LOTRAOM_FactionBuff.xml
  ModuleData/FactionBuff_GameTexts.xml
  ModuleData/FactionBuff_DataPacks.xml
```

## Load Order

Load after Bannerlord base modules, TAOM, and TAOM dependencies:

```text
Native
SandBoxCore
Sandbox
StoryMode
CustomBattle
TAOM
TAOM.Dependencies
LOTRAOM Faction Food & Prisoner Buffs
```

`StoryMode` and `CustomBattle` are optional in the manifest. `TAOM` and `TAOM.Dependencies` are required.

## Developer Install From Source

Build and stage the module:

```powershell
dotnet build ".\LOTRAOM_FactionBuff.csproj" -c Release -p:Platform=x64 -p:ModuleDeployRoot="D:\Temp\LOTRAOM_FactionBuff\"
```

Then copy the staged `LOTRAOM_FactionBuff/` folder into Bannerlord `Modules`.

## Save Safety

The module stores doctrine state and message pacing in the save. It does not define custom save classes.

Removing it will remove the added morale patch, messages, and doctrine controls from future sessions.
