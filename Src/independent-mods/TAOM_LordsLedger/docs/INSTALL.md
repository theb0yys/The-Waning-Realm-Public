# Install Guide

## Player Install From Binary Release

Use the player-ready module folder:

```text
TAOM_LordsLedger/
```

Copy it to:

```text
<Bannerlord install>/Modules/TAOM_LordsLedger/
```

The final installed structure should include:

```text
TAOM_LordsLedger/
  SubModule.xml
  bin/Win64_Shipping_Client/TAOM_LordsLedger.dll
  GUI/Prefabs/TAOM_LordsLedger.xml
```

## Load Order

Load after Bannerlord base modules and `TAOM.Dependencies`:

```text
Native
SandBoxCore
Sandbox
TAOM.Dependencies
TAOM Lord's Ledger
```

## Opening The Ledger

Press `L` on the campaign map.

If the Gauntlet board cannot open, the module falls back to the in-game ledger menu.

## Developer Install From Source

Build and stage the module:

```powershell
dotnet build ".\TAOM_LordsLedger.csproj" -c Release -p:Platform=x64 -p:ModuleDeployRoot="D:\Temp\TAOM_LordsLedger\"
```

Then copy the staged `TAOM_LordsLedger/` folder into Bannerlord `Modules`.

## Save Safety

The module persists UI settings, governance pulse settings, the last pulse day, and loyalty history through `SyncData`.

It does not define custom save classes.
