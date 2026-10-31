# Campaign Map Visibility

Status: eye-overlay correction built and deployed; Release build and 70 offline
checks pass. Native visual sign-off for the correction remains pending.

## Request and scope

Keep ordinary settlements, moving party labels and battle markers visible when
zooming out. Preserve normal scouting by default. MCM offers an opt-in
"Reveal entire map" setting, default false. The zoom setting defaults true.
The reveal setting displays parties and battles outside scouting range; it does
not discover hidden hideouts/retreats or change fog/scouting/gameplay state.
This is an owner-only standalone feature module. TAOM.Dependencies supplies
Harmony and MCM; the mod declares that provider directly.

## Research authority and integration

Request -> root budget/protected-file policy -> installed 1.4.8 managed map UI
decompilation -> this owner -> Release build, patch integration and policy tests
-> user-started native campaign qualification.

ILSpy 10.1.0.8386 was used against the installed game. Assembly identities and
targeted decompilations are in `Artifacts/tmp/campaign-map-visibility-20261009`.
SandBox.ViewModelCollection SHA256:
`8C4B2CA448A3BC8789A6F8D7B4427E7A9832A31C45DD79E9034173535EBAEBF4`.
Native/SubModule.xml identifies v1.4.8. Unknown UI assembly builds fail closed.

- PartyNameplateVM.DetermineIsVisibleOnMap: depth < 100 and camera Z < 200.
- SettlementNameplateVM.IsVisible: Z > 200 limits labels to fortifications;
  Z > 400 limits labels to towns. Keep tracking and viewport/projection rules.
- MapEventVisualItemVM.DetermineIsVisibleOnMap: camera Z < 200 and native
  event visibility. Preserve native visibility unless the reveal option is on.
- PartyNameplatesVM owns dictionary, pool and create/remove lifecycle.
  Use cached delegates to its create/remove methods. Reconcile once per toggle;
  maintain changes using native dirty-visibility, creation, settlement-exit,
  settlement-entry and destruction paths. Clear only this owner's listeners.
  When reveal turns off, rebuild the existing bound list once with its retained
  objects and call native Clear for discarded labels, avoiding repeated linear
  List.Remove searches and retaining thousands of unused objects in the pool.
- Existing MCM attribute usage was checked against PalantirMcmSettings.cs.

## Ownership, compatibility and performance

Only map view-model display state is changed. No global IsSpotted/IsVisible
getter patch, no scouting radius override, campaign behavior, save type,
SyncData, AI, world geometry, protected overhaul data or suite change.
Party labels represent moving parties at distant zoom; this mod does not enlarge
or force-render otherwise unseen 3D troop models or expose battle-scene troops.
Native army aggregation, inside-settlement suppression and raft rules remain.
MCM owns JSON settings. Removing the mod adds no save-schema migration; native
save/load qualification is still required before claiming compatibility proven.

Hot display patches are constant-time, allocation-free policy checks. Cached
delegates and field accessors are constructed outside hot loops. One O(N)
reconciliation occurs on reveal toggles; creation/visibility events use O(1)
dictionary membership. Individual native removal retains its existing O(P)
bound-list removal cost, where P is the displayed party count; bulk toggle
cleanup uses one linear pass. Native label rendering/sorting still scales with the
number of visible parties; full-map reveal can increase UI cost significantly.
No per-frame world scan, IO, metadata lookup or logging is added. Native frame
time, label overlap and large-campaign reveal cost remain qualification gates.

## Acceptance and next stop

### Scouting-eye correction

User screenshots 6925 and 6926 show isolated white scouting eyes at far zoom and
eyes above settlement nameplates nearby. The inspected native
SettlementNameplateWidget.UpdateNameplateTransparencyAndBrightness restores the
InspectedIconWidget alpha while IsInRange is true. A widget-local postfix zeros
only that child's alpha after the native update when KeepMarkersVisible is on.
No IsInRange, IsInspected or gameplay visibility state is changed. The native
widget refresh restores its ordinary behavior when the option is off. This is
constant-time, allocation-free, and edits no native or overhaul prefab files.
The tenth Harmony target is validated against the installed widget assembly.
Native acceptance: verify eyes remain absent through zoom/pan and time advance;
toggle KeepMarkersVisible off/on and verify scouting and other markers persist.

Offline: Release compile, actual Harmony patch installation on copied native
assemblies, supported-build identity, settings defaults, policy edge cases,
toggle lifecycle/deduplication and package dependency isolation.
The repository test catalog does not register this new owner; local tests are
owner-specific proof, not suite coverage or native sign-off.

Native: in a user-started campaign, verify town/castle/village labels, moving
parties and field/naval battle markers at near/mid/full zoom; check screen
edges and armies; toggle reveal on/off repeatedly; enter/leave settlements,
spawn/destroy parties, resolve battles, open MCM, save/reload, exit and load a
second campaign. Confirm unseen parties vanish when reveal is off and no
duplicate/stale labels or permanent discoveries remain. Record frame times
with reveal off/on and dense label readability. Do not start the game for this.

Deploy only this owner's DLL and manifest after dependency preflight. The owner
identified TAOM.Dependencies as the installed provider. Its manifest registers
MCM and its UI loader, and its client bin contains MCMv5 5.12.1.0 (SHA256
BE8FDE024957C97F0CF08ED813D2EF0A069F1D3E3F4C54F4557DA4083CBA3B15).
Build and test against that provider, without copying or changing dependencies.
The earlier missing-dependency finding concerned the unused standalone MCM path
and is superseded by this verified provider. No protected provider files changed.
The provider-based Release rebuild and 69 offline checks passed. The two owner
files were deployed with matching source/package/live SHA256 hashes; see
Release/deployment.json. No previous files existed, so no backups were needed.
The user still performs game startup and native qualification.

## Offline result

`Build-Package.ps1` builds the owner and its test executable in Release (zero
warnings/errors), validates 70 checks, and emits a two-file owner-only ZIP with
SHA256 payload receipt. All ten real Harmony UI patches install and unpatch in
the test process. One million varied policy checks completed within the 1000 ms
guard on .NET Framework 4.8 / Windows x64; see Release/offline-tests.txt for the
measured result. This does not measure native rendering or validate native
settings transitions, event lifecycle, MCM interaction, save/load or visuals.
