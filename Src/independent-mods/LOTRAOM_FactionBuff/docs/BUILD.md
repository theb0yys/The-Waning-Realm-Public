# Build Guide

## Prerequisites

- Windows x64.
- Mount & Blade II: Bannerlord `v1.4.15` installed.
- Bannerlord reference assemblies available under:

```text
<Bannerlord install>/bin/Win64_Shipping_Client/
```

- .NET SDK capable of building `net472` projects.

## Default Local Paths

The project file defaults `BannerlordGamePath` to:

```text
A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord
```

The default reference directory is:

```text
$(BannerlordGamePath)\bin\Win64_Shipping_Client
```

## Build Command

From the project root:

```powershell
dotnet build ".\LOTRAOM_FactionBuff.csproj" -c Release -p:Platform=x64
```

Expected compile output:

```text
build/bin/Win64_Shipping_Client/LOTRAOM_FactionBuff.dll
build/bin/Win64_Shipping_Client/LOTRAOM_FactionBuff.pdb
```

## Override Bannerlord Path

Use this when Bannerlord is installed somewhere else:

```powershell
dotnet build ".\LOTRAOM_FactionBuff.csproj" -c Release -p:Platform=x64 -p:BannerlordGamePath="A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

## Stage A Player Module

Stage to a temporary module folder:

```powershell
dotnet build ".\LOTRAOM_FactionBuff.csproj" -c Release -p:Platform=x64 -p:ModuleDeployRoot="D:\Temp\LOTRAOM_FactionBuff\"
```

The staged payload should contain:

```text
LOTRAOM_FactionBuff/
  SubModule.xml
  bin/Win64_Shipping_Client/LOTRAOM_FactionBuff.dll
  GUI/Prefabs/LOTRAOM_FactionBuff.xml
  ModuleData/FactionBuff_GameTexts.xml
  ModuleData/FactionBuff_DataPacks.xml
```

## Build Outputs

Do not include these generated folders in the public source release:

```text
.vs/
bin/
obj/
build/
release/
```

For a player release, package the staged `LOTRAOM_FactionBuff/` module folder separately.
