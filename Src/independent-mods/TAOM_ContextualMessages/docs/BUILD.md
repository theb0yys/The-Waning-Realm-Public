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

The project file defaults `BannerlordPath` to:

```text
A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord
```

The default reference directory is:

```text
$(BannerlordPath)\bin\Win64_Shipping_Client
```

## Build Command

From the project root:

```powershell
dotnet build ".\Vanilla_ContextualMessages.csproj" -c Release -p:Platform=x64
```

Expected compile output:

```text
build/bin/Win64_Shipping_Client/Vanilla_ContextualMessages.dll
build/bin/Win64_Shipping_Client/Vanilla_ContextualMessages.pdb
```

The build target also stages the Bannerlord module payload to:

```text
release/Modules/Vanilla_ContextualMessages/
```

## Override Bannerlord Path

Use this when Bannerlord is installed somewhere else:

```powershell
dotnet build ".\Vanilla_ContextualMessages.csproj" -c Release -p:Platform=x64 -p:BannerlordPath="A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

## Override Module Deploy Root

Use this when staging to a temporary folder:

```powershell
dotnet build ".\Vanilla_ContextualMessages.csproj" -c Release -p:Platform=x64 -p:ModuleDeployRoot="D:\Temp\Vanilla_ContextualMessages"
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

For a source release, package only the source folders, project files, docs, and metadata.

For a player release, package:

```text
release/Modules/Vanilla_ContextualMessages/
```
