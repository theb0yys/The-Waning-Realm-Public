# Troubleshooting

## Launcher Shows The Wrong Name

Expected launcher display name:

```text
TAOM Contextual Messages
```

If the launcher still shows `Vanilla Contextual Messages`, close and reopen the launcher so it rereads `SubModule.xml`.

Verify the installed manifest:

```text
<Bannerlord install>/Modules/Vanilla_ContextualMessages/SubModule.xml
```

The top-level module name should be:

```xml
<Name value="TAOM Contextual Messages"/>
```

## Build Cannot Find TaleWorlds Assemblies

The project expects Bannerlord assemblies under:

```text
$(BannerlordPath)/bin/Win64_Shipping_Client
```

Pass the path explicitly:

```powershell
dotnet build ".\Vanilla_ContextualMessages.csproj" -c Release -p:Platform=x64 -p:BannerlordPath="<Bannerlord install>"
```

## Signal Board Does Not Open

Check:

- You are in campaign, not main menu.
- The module loaded after TAOM.
- `F9` is not blocked by another overlay or keybind.
- `GUI/Prefabs/Vanilla_ContextualMessages.xml` exists in the installed module.

## Messages Are Too Quiet Or Too Frequent

Adjust defaults in:

```text
Configuration/MessageSettings.cs
```

Relevant settings include:

```text
Enabled
MaxMessagesPerHour
GlobalMessageGapHours
MaxBonusLinesPerEvent
ShowTAOMBridgeMessages
TAOMBridgeCooldownHours
TAOMBridgeMessageChance
```

## Data-Pack Lines Do Not Appear

Check:

- XML root is `ContextualMessageDataPacks`.
- Pool names match public `string[]` fields in `Content/MessageLibrary.cs`.
- Lines use `Line` or `Message` child nodes.
- Files are in `ModuleData/ContextualMessages_DataPacks.xml` or `ModuleData/ContextualMessages/*.xml`.

## TAOM Bridge Lines Do Not Appear

Bridge lines require the matching optional TAOM system to be loaded and expose the expected campaign behavior or metric accessors. Missing systems are intentionally silent.

Check the signal board bridge line to see which bridges linked at runtime.

## Public Source Package Contains Binaries

Remove generated output before packaging:

```text
.vs/
bin/
obj/
build/
release/
```

The public source package should not include `.dll`, `.pdb`, `.cache`, `.zip`, or NuGet generated files.
