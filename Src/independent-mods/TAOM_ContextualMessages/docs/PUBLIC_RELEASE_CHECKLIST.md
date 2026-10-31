# Public Release Checklist

Use this checklist before publishing the public source package.

## Version

- `Content/SubModule.xml` version is `v1.4.15`.
- `Content/SubModule.xml` supported version is `v1.4.15`.
- Base module dependency versions are `v1.4.15`.
- `Properties/AssemblyInfo.cs` assembly version is `1.4.15.0`.
- README and changelog say `v1.4.15`.

## Identity

- Launcher display name is `TAOM Contextual Messages`.
- Internal module ID remains `Vanilla_ContextualMessages`.
- DLL remains `Vanilla_ContextualMessages.dll`.
- Gauntlet movie/prefab remains `Vanilla_ContextualMessages`.

## Source Package Contents

Include:

```text
Behaviors/
Configuration/
Content/
Entry/
GUI/
Integration/
ModuleData/
Properties/
UI/
docs/
.gitignore
CHANGELOG.md
CONTRIBUTING.md
LICENSE.md
README.md
Directory.Build.props
Vanilla_ContextualMessages.csproj
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

## Validation

Run:

```powershell
dotnet build ".\Vanilla_ContextualMessages.csproj" -c Release -p:Platform=x64
```

Expected:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Search source and manifest files for stale pre-`v1.4.15` version strings before publishing.

Search for generated files:

```powershell
rg --files | rg "(\.dll$|\.pdb$|\.cache$|^bin/|^obj/|^build/|^release/|^\.vs/)"
```

Expected for the clean source package:

```text
no matches
```
