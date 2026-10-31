# Architecture

## Entry Point

`Entry/SubModule.cs` extends `MBSubModuleBase`.

On campaign start it creates `ContextualMessageBehavior` and registers it with the `CampaignGameStarter`.

On application tick it checks `F9` and opens the signal board when the campaign behavior is active.

## Campaign Behavior

`Behaviors/ContextualMessageBehavior.cs` owns:

- Bannerlord campaign event registration.
- Contextual message handlers.
- Global message pacing.
- Per-category cooldowns.
- Campaign mood tracking.
- Signal-board snapshot building.
- Settlement/camp/encounter menu controls.
- Optional TAOM bridge pulse checks.

The behavior persists cooldown and signal state through `SyncData`.

## Settings

`Configuration/MessageSettings.cs` contains static settings:

- Category toggles.
- Global pacing.
- Per-category cooldowns.
- Random chance gates.
- Food and wounded thresholds.

These settings are compile-time defaults. There is no external settings UI in this package.

## Message Libraries

Primary line pools live in:

```text
Content/MessageLibrary.cs
```

Legacy/simple pools live in:

```text
Integration/MessageLibrary.cs
```

The campaign behavior aliases the content library as `ContentMessageLibrary` where expanded pools and data-pack support are required.

## Signal Board UI

The signal board is implemented by:

```text
UI/ContextualSignalBoardScreen.cs
UI/ContextualSignalBoardSnapshot.cs
UI/ViewModels/ContextualSignalBoardVM.cs
UI/ViewModels/ContextualSignalItemVM.cs
GUI/Prefabs/Vanilla_ContextualMessages.xml
```

The Gauntlet movie name remains `Vanilla_ContextualMessages`, matching the prefab and existing project identity.

## TAOM Bridges

`Integration/ModBridge.cs` discovers optional TAOM campaign behaviors by reflection.

Bridge support is fail-safe:

- No optional TAOM companion assembly is hard-referenced.
- Missing companion systems return default values.
- Reflection failures are caught.
- Loaded bridges are reported by the signal board.

Known bridge names shown by the behavior include:

```text
Shadow & Dread
Roads of the King
Weight of Rule
Watch of the Marches
Bonds of the Holding
Long Mending
Burdened Marches
Founding of Holds
Hosts in the Field
Oaths and Houses
Legacy Traits
Court/Council
Roads of Middle-earth
Army Composition
Ranger's Satchel
Palantir
```

## Manifest

The source manifest is:

```text
Content/SubModule.xml
```

The build copies it to:

```text
release/Modules/Vanilla_ContextualMessages/SubModule.xml
```

The manifest launcher name is `TAOM Contextual Messages`. The internal ID and DLL name remain `Vanilla_ContextualMessages`.
