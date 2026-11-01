# Compatibility patches

All **14 compatibility patch projects** identified in the local project inventory are included here, together with **Xorberax Pack Control** and the **shared source helper**. Each project retains its own module identity and internal layout.

## Patches

| Patch | Module ID | Required modules declared by manifest | Project files |
| --- | --- | --- | ---: |
| [AIInfluence-TAOM-2.0.29.5](TAOM_Standalone_Compat_AIInfluence/) | `TAOM_Standalone_Compat_AIInfluence` | `Native`, `SandBoxCore`, `Sandbox`, `TAOM.Dependencies`, `TAOM`, `AIInfluence` | 7 |
| [Injuries And Diseases Compat Patch](IAD_CompatPatch/) | `IAD_CompatPatch` | `InjuriesAndDiseases`, `TAOM.Dependencies` | 8 |
| [Retinues Clan Colours - TAOM 2.0.29.5](TAOM_Standalone_Compat_Retinues/) | `TAOM_Standalone_Compat_Retinues` | `Native`, `SandBoxCore`, `Sandbox`, `Bannerlord.Harmony`, `TAOM.Dependencies`, `TAOM`, `Retinues` | 10 |
| [TAOM Agriculture Estate Compat Patch](TAOM_Standalone_Compat_AgricultureEstate/) | `TAOM_Standalone_Compat_AgricultureEstate` | `AgricultureEstate`, `TAOM.Dependencies` | 9 |
| [TAOM BE Companion Expanded Compat Patch](TAOM_Standalone_Compat_BECompanionExpanded/) | `TAOM_Standalone_Compat_BECompanionExpanded` | `BannerlordExpanded.CompanionExpanded`, `TAOM.Dependencies` | 9 |
| [TAOM BE Settlement Interactions Compat Patch](TAOM_Standalone_Compat_BESettlementInteractions/) | `TAOM_Standalone_Compat_BESettlementInteractions` | `BannerlordExpanded.SettlementInteractions`, `TAOM.Dependencies` | 9 |
| [TAOM Custom Battle Equipment Safety Compat Patch](TAOM_Standalone_Compat_CustomBattleEquipmentSafety/) | `TAOM_Standalone_Compat_CustomBattleEquipmentSafety` | `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `TAOM.Dependencies` | 9 |
| [TAOM Distinguished Service Compat Patch](TAOM_Standalone_Compat_DistinguishedService/) | `TAOM_Standalone_Compat_DistinguishedService` | `DistinguishedService110`, `TAOM.Dependencies` | 9 |
| [TAOM Fourberie Compat Patch](TAOM_Standalone_Compat_Fourberie/) | `TAOM_Standalone_Compat_Fourberie` | `Fourberie`, `TAOM.Dependencies` | 16 |
| [TAOM Governors Handle Issues Compat Patch](TAOM_Standalone_Compat_GovernorsHandleIssues/) | `TAOM_Standalone_Compat_GovernorsHandleIssues` | `GovernorsHandleIssues`, `TAOM.Dependencies` | 9 |
| [TAOM Less Menus More Immersion Patch](TAOM-LESS-MENUS_MORE-IMMERSION_PATCH/) | `TAOM-LESS-MENUS_MORE-IMMERSION_PATCH` | `Native`, `SandBoxCore`, `Sandbox`, `StoryMode`, `TAOM.Dependencies` | 9 |
| [TAOM My Little Warband Compatibility](TAOM_Standalone_Compat_MyLittleWarband/) | `TAOM_Standalone_Compat_MyLittleWarband` | `Native`, `TAOM.Dependencies`, `TAOM` | 18 |
| [TAOM RBM Compat Patch](TAOM_Standalone_Compat_RBM/) | `TAOM_Standalone_Compat_RBM` | `RBM`, `TAOM.Dependencies` | 9 |
| [Xorberax-TAOM-2.0.29.5](TAOM_XorberaxCompatibility/) | `TAOM_XorberaxCompatibility` | `Native`, `SandBoxCore`, `Sandbox`, `TAOM`, `XorberaxLegacy` | 11 |

The table uses exact manifest module IDs. No optional dependencies are declared by these 14 patch manifests. Follow the dependency metadata in each manifest and the project instructions for load order.

## Supporting projects

- [Xorberax Pack Control](Xorberax_TFTAOM_Pack_Control/): 13 source files; module ID `Xorberax_TFTAOM_Experience`. Its manifest requires `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `StoryMode`, `TAOM.Dependencies`, `TAOM`. It also expects separately supplied managed-module/dependency DLL payloads.
- [_SharedCompat](_SharedCompat/): 2 files, including `FinalizerInfrastructure.cs`. This is linked source, not a launcher module. Keep it beside Agriculture Estate, BE Companion Expanded, BE Settlement Interactions, Custom Battle Equipment Safety, Distinguished Service, Governors Handle Issues and RBM; all seven projects reference it through `../_SharedCompat/`.

The folder contains **157 project/support files**, plus this index: 33 previously published files and 124 additional reviewed files.

## Build and runtime requirements

- See the [repository README](../../README.md#runtime-dependencies) for the complete dependency table and project build links. The main projects target .NET Framework 4.7.2; game and third-party libraries are external.
- [My Little Warband](TAOM_Standalone_Compat_MyLittleWarband/README.md) requires the original My Little Warband installation and the exact supported DLL. Its compatibility module loads before My Little Warband and adapts the runtime in memory. The original mod DLL, XML and GUI files are not bundled.
- AIInfluence, Retinues and Xorberax Compatibility retain their dependency identity guards. Their source was recovered from the existing compatibility-bridge binaries; this publication contains the bridge projects rather than the external mod implementations.
- Xorberax Pack Control and the My Little Warband adapter are included as explicitly authorized supporting/runtime source. No compiled or third-party binary payloads are included.
- Included build/package scripts may refer to local game paths and generate files under `Artifacts` or `Releases`. Review their parameters before running them. Existing validation claims in project documents describe their recorded runs; this publication did not rebuild or rerun gameplay tests.
