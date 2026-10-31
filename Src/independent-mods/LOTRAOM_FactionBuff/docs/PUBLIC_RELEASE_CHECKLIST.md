# Public Release Checklist

Use this checklist before publishing the public source package and binary module package.

## Version

- `SubModule.xml` module version is `v1.3.0`.
- `SubModule.xml` supported version is `v1.4.15`.
- `LOTRAOM_FactionBuff.csproj` assembly version is `1.3.0.0`.
- README and changelog say Bannerlord `v1.4.15`.

## Identity

- Launcher display name is `LOTRAOM Faction Food & Prisoner Buffs`.
- Internal module ID remains `LOTRAOM_FactionBuff`.
- DLL remains `LOTRAOM_FactionBuff.dll`.
- Gauntlet movie/prefab remains `LOTRAOM_FactionBuff`.

## Source Package Contents

Include:

```text
src/
GUI/
ModuleData/
docs/
.gitignore
CHANGELOG.md
CONTRIBUTING.md
LICENSE.md
README.md
Directory.Build.props
LOTRAOM_FactionBuff.csproj
SubModule.xml
```

Exclude:

```text
.vs/
bin/
obj/
build/
release/
*.dll
*.pdb
*.cache
*.zip
*.7z
```

## Binary Package Contents

Include:

```text
LOTRAOM_FactionBuff/
  SubModule.xml
  bin/Win64_Shipping_Client/LOTRAOM_FactionBuff.dll
  GUI/Prefabs/LOTRAOM_FactionBuff.xml
  ModuleData/FactionBuff_GameTexts.xml
  ModuleData/FactionBuff_DataPacks.xml
```

## Validation

Run:

```powershell
dotnet build ".\LOTRAOM_FactionBuff.csproj" -c Release -p:Platform=x64
```

Expected:

```text
Build succeeded.
0 Error(s)
```

Search source and manifest files for stale pre-`v1.4.15` Bannerlord version strings before publishing.

Search the clean source package for generated files:

```powershell
rg --files | rg "(\.dll$|\.pdb$|\.cache$|^bin/|^obj/|^build/|^release/|^\.vs/)"
```

Expected for the clean source package:

```text
no matches
```
