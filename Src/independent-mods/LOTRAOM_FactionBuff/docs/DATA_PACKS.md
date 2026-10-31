# Data Pack Guide

LOTRAOM Faction Buff can clone or override culture profiles from XML without rebuilding the DLL.

## Load Locations

The loader checks:

```text
ModuleData/FactionBuff_DataPacks.xml
ModuleData/FactionBuff/*.xml
```

Files under `ModuleData/FactionBuff/` are read only from that top-level directory.

## XML Format

```xml
<?xml version="1.0" encoding="utf-8"?>
<FactionBuffDataPacks>
  <Culture id="new_culture"
           cloneFrom="gondor"
           alignment="Good"
           foodItem="grain"
           foodBonus="6"
           peakSeason="Autumn">
    <Keys bucket="foodStocked">
      custom_food_stocked_new_culture_0 custom_food_stocked_new_culture_1
    </Keys>
    <Keys bucket="prisonerLow">
      custom_prisoner_low_new_culture_0
    </Keys>
  </Culture>
</FactionBuffDataPacks>
```

`id` and `cultureId` are both accepted.

`cloneFrom` and `base` are both accepted. If omitted, the loader tries to clone from the same ID.

`extrasCloneFrom` and `extrasBase` are both accepted for victory, political, renown, and wounded extra profile data.

## Culture Attributes

| Attribute | Meaning |
| --- | --- |
| `id` or `cultureId` | Culture ID to create or override |
| `cloneFrom` or `base` | Existing profile to copy defaults from |
| `extrasCloneFrom` or `extrasBase` | Existing extra profile to copy from |
| `alignment` | `Good`, `Neutral`, or `Evil` |
| `foodItem` | Bannerlord item string ID |
| `foodBonus` | Base morale value before tier and doctrine multipliers |
| `peakSeason` | `Spring`, `Summer`, `Autumn`, or `Winter` |

## Key Buckets

The loader accepts `<Keys bucket="...">` or `<Keys name="...">` nodes. Values can be separated by spaces, commas, semicolons, tabs, or new lines.

Supported buckets:

```text
foodScarce
foodStocked
foodSurplus
prisonerHigh
prisonerNormal
prisonerLow
combinedPeak
combinedDire
seasonSpring
seasonSummer
seasonAutumn
seasonWinter
streakPeak
streakDire
```

The key strings must exist in loaded GameText XML, normally `ModuleData/FactionBuff_GameTexts.xml` or another XML registered by the consuming module.

## Runtime Behavior

- Data packs load once during `OnSubModuleLoad`.
- Bad XML files are skipped and logged through `Debug.Print`.
- A culture override applies only if its clone source exists.
- Missing attributes or buckets fall back to the cloned profile.
- Data packs can override built-in culture IDs.

An example pack is included at:

```text
docs/examples/FactionBuff_CustomCulture.xml
```
