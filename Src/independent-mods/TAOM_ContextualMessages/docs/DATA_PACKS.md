# Data Pack Guide

TAOM Contextual Messages can load extra message lines from XML without rebuilding the DLL.

## Load Locations

The loader checks:

```text
ModuleData/ContextualMessages_DataPacks.xml
ModuleData/ContextualMessages/*.xml
```

Files under `ModuleData/ContextualMessages/` are read only from that top-level directory.

## XML Format

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContextualMessageDataPacks>
  <Pool name="BattleWin">
    <Line>The line held, and the road remains open.</Line>
  </Pool>
  <Pool name="QuestCompleted">
    <Message>The record is closed, but the consequences are still walking.</Message>
  </Pool>
</ContextualMessageDataPacks>
```

`Line` and `Message` child nodes are both accepted.

## Pool Names

Pool names must match public `string[]` field names in:

```text
Content/MessageLibrary.cs
```

Examples include:

```text
BattleWin
BattleLoss
RaidCompleted
SiegeStarted
TournamentVictory
CompanionRecruited
CaravanArrived
QuestCompleted
DreadFearful
RoadsMEDangerous
PalantirHealthGood
```

Use `rg "public static readonly string\\[\\]" Content\\MessageLibrary.cs` to list available built-in pools.

An example pack is included at:

```text
docs/examples/ContextualMessages_CustomLines.xml
```

## Runtime Behavior

- Data packs are loaded on first message-pool use.
- Extra lines are appended to built-in pool choices.
- Selection uses random choice with last-used avoidance.
- Bad XML files are skipped and logged through `Debug.Print`.
- Empty text nodes are ignored.
- Unknown pool names are accepted into the extra-line dictionary, but they only have an effect if code later asks for a matching built-in pool name.
