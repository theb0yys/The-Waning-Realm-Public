# The Waning Realm Public

Public source for 26 Mount & Blade II: Bannerlord mod and compatibility projects. The 11 other mod projects live under `Src/independent-mods/`; 14 compatibility patches and Xorberax Pack Control are grouped under [`Src/compatibility-patches/`](Src/compatibility-patches/). Each project keeps its own implementation and internal layout. The patch folder also includes the shared source helper used by seven patches.

This is a source collection. Compiled modules, game assets and external dependencies are not bundled. Choose the projects you need; the collection is not a single combined mod or an install-ready release.

## Included projects

| Project | Purpose | Module ID |
| --- | --- | --- |
| [Campaign Map Visibility](Src/independent-mods/CampaignMapVisibility/) | Keeps campaign map labels and markers visible when zoomed out, with an optional map-reveal setting. | `CampaignMapVisibility` |
| [Custom Battle Reforged](Src/independent-mods/CustomBattleReforged/) | TAOM Custom Battle faction, troop-selection and army-size adjustments. | `CustomBattleReforged` |
| [Custom Battle Return](Src/independent-mods/CustomBattleReturn/) | Custom Battle return-menu patch. | `CustomBattleReturn` |
| [Injuries And Diseases Compat Patch](Src/compatibility-patches/IAD_CompatPatch/) | Compatibility patches for Injuries and Diseases. | `IAD_CompatPatch` |
| [LOTRAOM Faction Food & Prisoner Buffs](Src/independent-mods/LOTRAOM_FactionBuff/) | Faction food and prisoner buffs for TAOM. | `LOTRAOM_FactionBuff` |
| [No Fall Damage](Src/independent-mods/NoFallDamage/) | Disables standard singleplayer fall damage for players, NPCs, mounts and riders. | `NoFallDamage` |
| [Player Jump Height](Src/independent-mods/PlayerJumpHeight/) | Configurable player-character jump height through Mod Options. | `PlayerJumpHeight` |
| [Player Speed Control](Src/independent-mods/PlayerSpeedControl/) | Separate controls for player running speed and campaign land travel. | `PlayerSpeedControl` |
| [TAOM Less Menus More Immersion Patch](Src/compatibility-patches/TAOM-LESS-MENUS_MORE-IMMERSION_PATCH/) | Less Menus More Immersion compatibility patch for game-menu activation. | `TAOM-LESS-MENUS_MORE-IMMERSION_PATCH` |
| [TAOM Contextual Messages](Src/independent-mods/TAOM_ContextualMessages/) | Contextual campaign messages for TAOM. | `Vanilla_ContextualMessages` |
| [TAOM Equipment Test Shop](Src/independent-mods/TAOM_EquipmentTestShop/) | Equipment test-shop utility source; module packaging is incomplete in this repository. | No published module manifest |
| [TAOM Palantir Network](Src/independent-mods/TAOM_PalantirNetwork/) | Palantir network campaign feature for TAOM. | `TAOM_PalantirNetwork` |
| [TAOM Fourberie Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_Fourberie/) | Compatibility patches for Fourberie. | `TAOM_Standalone_Compat_Fourberie` |
| [Waning Troop Limit Expander](Src/independent-mods/TLE_updated/) | Custom Battle troop limits, active-agent caps and reinforcement-wave controls. | `WANINGTROOPLIMITEXPANDER` |
| [AIInfluence-TAOM-2.0.29.5](Src/compatibility-patches/TAOM_Standalone_Compat_AIInfluence/) | Diplomacy compatibility bridge between TAOM and AI Influence. | `TAOM_Standalone_Compat_AIInfluence` |
| [TAOM Agriculture Estate Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_AgricultureEstate/) | Compatibility finalizers for Agriculture Estate. | `TAOM_Standalone_Compat_AgricultureEstate` |
| [TAOM BE Companion Expanded Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_BECompanionExpanded/) | Compatibility finalizers for Bannerlord Expanded Companion Expanded. | `TAOM_Standalone_Compat_BECompanionExpanded` |
| [TAOM BE Settlement Interactions Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_BESettlementInteractions/) | Compatibility finalizers for Bannerlord Expanded Settlement Interactions. | `TAOM_Standalone_Compat_BESettlementInteractions` |
| [TAOM Custom Battle Equipment Safety Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_CustomBattleEquipmentSafety/) | Initial weapon-equipment safety patch for Custom Battle. | `TAOM_Standalone_Compat_CustomBattleEquipmentSafety` |
| [TAOM Distinguished Service Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_DistinguishedService/) | Compatibility finalizers for Distinguished Service. | `TAOM_Standalone_Compat_DistinguishedService` |
| [TAOM Governors Handle Issues Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_GovernorsHandleIssues/) | Compatibility finalizers for Governors Handle Issues. | `TAOM_Standalone_Compat_GovernorsHandleIssues` |
| [TAOM My Little Warband Compatibility](Src/compatibility-patches/TAOM_Standalone_Compat_MyLittleWarband/) | My Little Warband runtime API, culture and equipment compatibility. | `TAOM_Standalone_Compat_MyLittleWarband` |
| [TAOM RBM Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_RBM/) | Compatibility finalizers for Realistic Battle Mod. | `TAOM_Standalone_Compat_RBM` |
| [Retinues Clan Colours - TAOM 2.0.29.5](Src/compatibility-patches/TAOM_Standalone_Compat_Retinues/) | Retinues customised-troop clan-colour compatibility. | `TAOM_Standalone_Compat_Retinues` |
| [Xorberax-TAOM-2.0.29.5](Src/compatibility-patches/TAOM_XorberaxCompatibility/) | TAOM creature-cleave compatibility with Xorberax Legacy. | `TAOM_XorberaxCompatibility` |
| [Xorberax TFTAOM Experience](Src/compatibility-patches/Xorberax_TFTAOM_Pack_Control/) | Supporting module host and configuration control for the Xorberax pack. | `Xorberax_TFTAOM_Experience` |

## Runtime dependencies

The entries below reproduce the published `SubModule.xml` declarations. Names are exact module IDs; dependencies marked optional remain optional. “None declared” describes the manifest, not a guarantee that a mod can run without the base game or other requirements imposed by its dependencies.

| Project / manifest | Required modules | Optional modules |
| --- | --- | --- |
| [Campaign Map Visibility](Src/independent-mods/CampaignMapVisibility/SubModule.xml) | `TAOM.Dependencies`, `Native` (`v1.4.8`), `SandBoxCore`, `Sandbox` | None declared |
| [Custom Battle Reforged](Src/independent-mods/CustomBattleReforged/SubModule.xml) | `Native`, `SandBoxCore`, `CustomBattle`, `TAOM.Dependencies`, `TAOM` | None declared |
| [Custom Battle Return](Src/independent-mods/CustomBattleReturn/module/CustomBattleReturn/SubModule.xml) | `Bannerlord.Harmony`, `Native`, `CustomBattle` | None declared |
| [Injuries And Diseases Compat Patch](Src/compatibility-patches/IAD_CompatPatch/SubModule.xml) | `InjuriesAndDiseases`, `TAOM.Dependencies` | None declared |
| [LOTRAOM Faction Food & Prisoner Buffs](Src/independent-mods/LOTRAOM_FactionBuff/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `TAOM`, `TAOM.Dependencies` | `StoryMode`, `CustomBattle` |
| [No Fall Damage](Src/independent-mods/NoFallDamage/SubModule.xml) | `TAOM.Dependencies`, `Native` (`v1.4.8`), `SandBoxCore`, `Sandbox` | None declared |
| [Player Jump Height](Src/independent-mods/PlayerJumpHeight/SubModule.xml) | `TAOM.Dependencies`, `Native` (`v1.4.8`), `SandBoxCore`, `Sandbox` | None declared |
| [Player Speed Control](Src/independent-mods/PlayerSpeedControl/SubModule.xml) | `TAOM.Dependencies`, `Native` (`v1.4.8`), `SandBoxCore`, `Sandbox` | None declared |
| [TAOM Less Menus More Immersion Patch](Src/compatibility-patches/TAOM-LESS-MENUS_MORE-IMMERSION_PATCH/TAOM-LESS-MENUS_MORE-IMMERSION_PATCH/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `StoryMode`, `TAOM.Dependencies` | None declared |
| [TAOM Contextual Messages](Src/independent-mods/TAOM_ContextualMessages/Content/SubModule.xml) | `Native` (`v1.4.15`), `SandBoxCore` (`v1.4.15`), `Sandbox` (`v1.4.15`), `TAOM` | `StoryMode` (`v1.4.15`), `CustomBattle` (`v1.4.15`) |
| [TAOM Equipment Test Shop](Src/independent-mods/TAOM_EquipmentTestShop/TAOM_EquipmentTestShop.csproj) | Not recorded: no published `SubModule.xml` | Not recorded |
| [TAOM Palantir Network](Src/independent-mods/TAOM_PalantirNetwork/TAOM_PalantirNetwork/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `Bannerlord.Harmony`, `Bannerlord.ButterLib`, `Bannerlord.UIExtenderEx`, `Bannerlord.MBOptionScreen` | `TAOM` |
| [TAOM Fourberie Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_Fourberie/SubModule.xml) | `Fourberie`, `TAOM.Dependencies` | None declared |
| [Waning Troop Limit Expander](Src/independent-mods/TLE_updated/SubModule.xml) | `Native` (`v1.4.7`), `SandBoxCore` (`v1.4.7`), `Sandbox` (`v1.4.7`), `CustomBattle` (`v1.4.7`), `TAOM.Dependencies`, `TAOM` | None declared |
| [AIInfluence-TAOM-2.0.29.5](Src/compatibility-patches/TAOM_Standalone_Compat_AIInfluence/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `TAOM.Dependencies`, `TAOM`, `AIInfluence` | None declared |
| [TAOM Agriculture Estate Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_AgricultureEstate/SubModule.xml) | `AgricultureEstate`, `TAOM.Dependencies` | None declared |
| [TAOM BE Companion Expanded Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_BECompanionExpanded/SubModule.xml) | `BannerlordExpanded.CompanionExpanded`, `TAOM.Dependencies` | None declared |
| [TAOM BE Settlement Interactions Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_BESettlementInteractions/SubModule.xml) | `BannerlordExpanded.SettlementInteractions`, `TAOM.Dependencies` | None declared |
| [TAOM Custom Battle Equipment Safety Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_CustomBattleEquipmentSafety/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `TAOM.Dependencies` | None declared |
| [TAOM Distinguished Service Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_DistinguishedService/SubModule.xml) | `DistinguishedService110`, `TAOM.Dependencies` | None declared |
| [TAOM Governors Handle Issues Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_GovernorsHandleIssues/SubModule.xml) | `GovernorsHandleIssues`, `TAOM.Dependencies` | None declared |
| [TAOM My Little Warband Compatibility](Src/compatibility-patches/TAOM_Standalone_Compat_MyLittleWarband/SubModule.xml) | `Native`, `TAOM.Dependencies`, `TAOM` | None declared |
| [TAOM RBM Compat Patch](Src/compatibility-patches/TAOM_Standalone_Compat_RBM/SubModule.xml) | `RBM`, `TAOM.Dependencies` | None declared |
| [Retinues Clan Colours - TAOM 2.0.29.5](Src/compatibility-patches/TAOM_Standalone_Compat_Retinues/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `Bannerlord.Harmony`, `TAOM.Dependencies`, `TAOM`, `Retinues` | None declared |
| [Xorberax-TAOM-2.0.29.5](Src/compatibility-patches/TAOM_XorberaxCompatibility/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `TAOM`, `XorberaxLegacy` | None declared |
| [Xorberax TFTAOM Experience](Src/compatibility-patches/Xorberax_TFTAOM_Pack_Control/SubModule.xml) | `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `StoryMode`, `TAOM.Dependencies`, `TAOM` | None declared |

### Dependency and version notes

- `Native`, `SandBoxCore`, `Sandbox`, `StoryMode` and `CustomBattle` are Bannerlord modules.
- `TAOM`, `TAOM.Dependencies`, `InjuriesAndDiseases`, `Fourberie`, `Bannerlord.Harmony`, `Bannerlord.ButterLib`, `Bannerlord.UIExtenderEx` and `Bannerlord.MBOptionScreen` are external requirements where listed; their implementations are not included here. `Bannerlord.MBOptionScreen` is the Mod Configuration Menu module ID.
- Make required dependencies available and follow each module manifest's load-order declarations, including `LoadBeforeThis` metadata. Optional integrations may require their own dependencies when enabled. There is no validated collection-wide load order.
- Campaign Map Visibility, No Fall Damage, Player Jump Height and Player Speed Control declare `Native v1.4.8`. Contextual Messages declares `v1.4.15` for its listed native modules. Troop Limit Expander declares `v1.4.7` for its listed native modules.
- The `SupportedVersions` fields additionally declare `v1.4.15` for Faction Food & Prisoner Buffs and Contextual Messages, `v1.4.7` for Troop Limit Expander, and `e1.3.15` for Palantir Network. These are existing manifest values, not a claim that all projects target the same game version.
- Less Menus More Immersion is the patch's named compatibility target, but its published manifest does not declare a separate Less Menus module dependency. Equipment Test Shop has no published launcher manifest; its runtime requirements and install layout remain unrecorded.

- Additional external module IDs used by the patches include `AIInfluence`, `AgricultureEstate`, `BannerlordExpanded.CompanionExpanded`, `BannerlordExpanded.SettlementInteractions`, `DistinguishedService110`, `GovernorsHandleIssues`, `RBM`, `Retinues`, `XorberaxLegacy`. They remain separately installed dependencies.
- My Little Warband also requires the original `MyLittleWarband` installation and its supported DLL, XML and GUI files, even though it is not listed in the required-module list. Follow its [README](Src/compatibility-patches/TAOM_Standalone_Compat_MyLittleWarband/README.md): the compatibility module must load before My Little Warband.
- Some bridges validate exact dependency hashes or module identities. A matching module name alone does not establish compatibility. Xorberax Pack Control also expects its managed-module and dependency DLL payloads; those binaries are not included in this source repository.

## Building from source

The 26 main mod projects target **.NET Framework 4.7.2** (`net472` or `v4.7.2`). Use a Windows C# / MSBuild toolchain with the corresponding targeting pack; SDK-style projects also require a compatible .NET SDK. Test projects may have different framework requirements.

1. Open the individual project below. There is no root solution for the entire collection.
2. Inspect its `.csproj` and any included build script. Set the game and dependency paths for your machine; some references use local paths or MSBuild properties such as `GameRoot` and `GameBin`. Match the game assemblies to that project's declared version and APIs.
3. Supply the referenced game and external libraries. Most Harmony consumers reference `0Harmony.dll` from an installed module or a local dependency path. Faction Food & Prisoner Buffs instead declares the NuGet package `Lib.Harmony` version `2.3.3`.
4. Campaign Map Visibility, Player Jump Height, Player Speed Control and Troop Limit Expander also reference MCM. Palantir Network references Harmony, ButterLib, UIExtenderEx and MCM. These build references do not replace the runtime module requirements above.
5. Review output paths and post-build targets before building: some projects or scripts copy output into a local game installation. Use any included project-specific validation instructions.

| Project folder | Build entry point |
| --- | --- |
| `CampaignMapVisibility` | [CampaignMapVisibility.csproj](Src/independent-mods/CampaignMapVisibility/CampaignMapVisibility.csproj) |
| `CustomBattleReforged` | [CustomBattleReforged.csproj](Src/independent-mods/CustomBattleReforged/CustomBattleReforged.csproj) |
| `CustomBattleReturn` | [CustomBattleReturn.csproj](Src/independent-mods/CustomBattleReturn/CustomBattleReturn.csproj) |
| `IAD_CompatPatch` | [IAD_CompatPatch.csproj](Src/compatibility-patches/IAD_CompatPatch/IAD_CompatPatch.csproj) |
| `LOTRAOM_FactionBuff` | [LOTRAOM_FactionBuff.csproj](Src/independent-mods/LOTRAOM_FactionBuff/LOTRAOM_FactionBuff.csproj) |
| `NoFallDamage` | [NoFallDamage.csproj](Src/independent-mods/NoFallDamage/NoFallDamage.csproj) |
| `PlayerJumpHeight` | [PlayerJumpHeight.csproj](Src/independent-mods/PlayerJumpHeight/PlayerJumpHeight.csproj) |
| `PlayerSpeedControl` | [PlayerSpeedControl.csproj](Src/independent-mods/PlayerSpeedControl/PlayerSpeedControl.csproj) |
| `TAOM-LESS-MENUS_MORE-IMMERSION_PATCH` | [TAOM-LESS-MENUS_MORE-IMMERSION_PATCH.csproj](Src/compatibility-patches/TAOM-LESS-MENUS_MORE-IMMERSION_PATCH/TAOM-LESS-MENUS_MORE-IMMERSION_PATCH/TAOM-LESS-MENUS_MORE-IMMERSION_PATCH.csproj) |
| `TAOM_ContextualMessages` | [Vanilla_ContextualMessages.csproj](Src/independent-mods/TAOM_ContextualMessages/Vanilla_ContextualMessages.csproj) |
| `TAOM_EquipmentTestShop` | [TAOM_EquipmentTestShop.csproj](Src/independent-mods/TAOM_EquipmentTestShop/TAOM_EquipmentTestShop.csproj) |
| `TAOM_PalantirNetwork` | [TAOM_PalantirNetwork.csproj](Src/independent-mods/TAOM_PalantirNetwork/TAOM_PalantirNetwork/TAOM_PalantirNetwork.csproj) |
| `TAOM_Standalone_Compat_Fourberie` | [TAOM_Standalone_Compat_Fourberie.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_Fourberie/TAOM_Standalone_Compat_Fourberie.csproj) |
| `TLE_updated` | [TroopLimitExpander.csproj](Src/independent-mods/TLE_updated/TroopLimitExpander.csproj) |
| `TAOM_Standalone_Compat_AIInfluence` | [TAOM_Standalone_Compat_AIInfluence.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_AIInfluence/TAOM_Standalone_Compat_AIInfluence.csproj) |
| `TAOM_Standalone_Compat_AgricultureEstate` | [TAOM_Standalone_Compat_AgricultureEstate.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_AgricultureEstate/TAOM_Standalone_Compat_AgricultureEstate.csproj) |
| `TAOM_Standalone_Compat_BECompanionExpanded` | [TAOM_Standalone_Compat_BECompanionExpanded.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_BECompanionExpanded/TAOM_Standalone_Compat_BECompanionExpanded.csproj) |
| `TAOM_Standalone_Compat_BESettlementInteractions` | [TAOM_Standalone_Compat_BESettlementInteractions.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_BESettlementInteractions/TAOM_Standalone_Compat_BESettlementInteractions.csproj) |
| `TAOM_Standalone_Compat_CustomBattleEquipmentSafety` | [TAOM_Standalone_Compat_CustomBattleEquipmentSafety.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_CustomBattleEquipmentSafety/TAOM_Standalone_Compat_CustomBattleEquipmentSafety.csproj) |
| `TAOM_Standalone_Compat_DistinguishedService` | [TAOM_Standalone_Compat_DistinguishedService.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_DistinguishedService/TAOM_Standalone_Compat_DistinguishedService.csproj) |
| `TAOM_Standalone_Compat_GovernorsHandleIssues` | [TAOM_Standalone_Compat_GovernorsHandleIssues.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_GovernorsHandleIssues/TAOM_Standalone_Compat_GovernorsHandleIssues.csproj) |
| `TAOM_Standalone_Compat_MyLittleWarband` | [TAOM_Standalone_Compat_MyLittleWarband.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_MyLittleWarband/TAOM_Standalone_Compat_MyLittleWarband.csproj) |
| `TAOM_Standalone_Compat_RBM` | [TAOM_Standalone_Compat_RBM.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_RBM/TAOM_Standalone_Compat_RBM.csproj) |
| `TAOM_Standalone_Compat_Retinues` | [TAOM_Standalone_Compat_Retinues.csproj](Src/compatibility-patches/TAOM_Standalone_Compat_Retinues/TAOM_Standalone_Compat_Retinues.csproj) |
| `TAOM_XorberaxCompatibility` | [TAOM_XorberaxCompatibility.csproj](Src/compatibility-patches/TAOM_XorberaxCompatibility/TAOM_XorberaxCompatibility.csproj) |
| `Xorberax_TFTAOM_Pack_Control` | [Xorberax_TFTAOM_Pack_Control.csproj](Src/compatibility-patches/Xorberax_TFTAOM_Pack_Control/Xorberax_TFTAOM_Pack_Control.csproj) |

Seven patch projects link `../_SharedCompat/FinalizerInfrastructure.cs`. Keep the [_SharedCompat](Src/compatibility-patches/_SharedCompat/) folder alongside those projects when building. AIInfluence, Retinues and Xorberax Compatibility reference Harmony from the installed `Bannerlord.Harmony` directory in their project files; Pack Control additionally references MCMv5. These build paths are separate from the manifest declarations.

## Repository layout

```text
Src/
  independent-mods/
    <project>/           # 11 independent mod projects
  compatibility-patches/
    <patch>/             # 14 patches, Pack Control and _SharedCompat
      ...                # Source, text definitions and available docs/tests
LICENSE
README.md
```

Project folders and module IDs are not always identical. In particular, Contextual Messages uses `Vanilla_ContextualMessages`, and Troop Limit Expander uses `WANINGTROOPLIMITEXPANDER`. Preserve the IDs and manifest layout when preparing an installable module.

## Status and validation

This README documents the published source and dependency declarations; it is not a build, gameplay, save-compatibility or cross-mod compatibility certification. Consult the documentation and tests included with each project for its evidence and remaining limitations. Some existing project instructions refer to local build outputs or resources that are not part of this source repository.

## License

See [LICENSE](LICENSE) for the repository's GNU General Public License v3.0. Game libraries and external mods retain their own licenses and distribution terms.
