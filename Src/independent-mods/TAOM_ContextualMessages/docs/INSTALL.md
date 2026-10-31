# Install Guide

## Player Install From Binary Release

Use the player-ready module folder:

```text
Vanilla_ContextualMessages/
```

Copy it to:

```text
<Bannerlord install>/Modules/Vanilla_ContextualMessages/
```

The final installed structure should include:

```text
Vanilla_ContextualMessages/
  SubModule.xml
  bin/Win64_Shipping_Client/Vanilla_ContextualMessages.dll
  GUI/Prefabs/Vanilla_ContextualMessages.xml
  ModuleData/ContextualMessages_DataPacks.xml
```

## Developer Install From Source

Build the project first:

```powershell
dotnet build ".\Vanilla_ContextualMessages.csproj" -c Release -p:Platform=x64
```

Then copy:

```text
release/Modules/Vanilla_ContextualMessages/
```

to:

```text
<Bannerlord install>/Modules/Vanilla_ContextualMessages/
```

## Launcher

The launcher should show:

```text
TAOM Contextual Messages
```

The internal module ID remains:

```text
Vanilla_ContextualMessages
```

That internal name is expected and is not the launcher display name.

## Load Order

Load after Bannerlord base modules and after TAOM:

```text
Native
SandBoxCore
Sandbox
StoryMode
CustomBattle
TAOM
TAOM Contextual Messages
```

`StoryMode` and `CustomBattle` are marked optional in the manifest. `TAOM` is required.

## Keybind

Press `F9` in campaign to open the signal board.
