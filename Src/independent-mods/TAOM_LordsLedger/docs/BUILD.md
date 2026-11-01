# Build Guide

## Prerequisites

- Windows x64.
- Mount & Blade II: Bannerlord `v1.4.15` installed.
- Bannerlord reference assemblies available under:

```text
<Bannerlord install>/bin/Win64_Shipping_Client/
```

- .NET SDK or MSBuild capable of building .NET Framework 4.7.2 projects.

## BannerlordDir

This legacy project requires `BannerlordDir`.

Recommended local file, not for public source packages:

```xml
<Project>
  <PropertyGroup>
    <BannerlordDir>A:\SteamLibrary\steamapps\common\Mount &amp; Blade II Bannerlord</BannerlordDir>
  </PropertyGroup>
</Project>
```

Save that as:

```text
BannerlordDir.props
```

next to `TAOM_LordsLedger.csproj`.

Alternative command-line override:

```powershell
dotnet build ".\TAOM_LordsLedger.csproj" -c Release -p:Platform=x64 -p:BannerlordDir="A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

## Build Command

From the project root:

```powershell
dotnet build ".\TAOM_LordsLedger.csproj" -c Release -p:Platform=x64
```

Expected compile output:

```text
bin/x64/Release/TAOM_LordsLedger.dll
bin/x64/Release/TAOM_LordsLedger.pdb
```

## Stage A Player Module

Stage to a temporary module folder:

```powershell
dotnet build ".\TAOM_LordsLedger.csproj" -c Release -p:Platform=x64 -p:ModuleDeployRoot="D:\Temp\TAOM_LordsLedger\"
```

The staged payload should contain:

```text
TAOM_LordsLedger/
  SubModule.xml
  bin/Win64_Shipping_Client/TAOM_LordsLedger.dll
  GUI/Prefabs/TAOM_LordsLedger.xml
```

## Build Outputs

Do not include these generated or local-only files in the public source release:

```text
.vs/
bin/
obj/
release/
BannerlordDir.props
*.dll
*.pdb
```
