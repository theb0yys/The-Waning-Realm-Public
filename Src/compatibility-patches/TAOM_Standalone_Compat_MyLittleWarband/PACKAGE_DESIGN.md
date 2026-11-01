# TAOM My Little Warband compatibility design

Status: local managed validation passes; native loader/UI/mission/save-load sign-off
pending. Scope: this independent TAOM patch only. The user explicitly authorized
binary compatibility work here and prohibited changing either original mod.
The user subsequently authorized scoped deployment of this patch. Git ref changes,
original-mod edits and suite changes remain outside scope. Game startup is forbidden.

## Research path and authority

Request -> root budget/protected-file rules -> installed game and supplied DLL
inspection -> this owner's runtime adapter -> Release/owner managed tests ->
user-started campaign acceptance. Context, sentinel, impact, compatibility,
test-gap and performance skills govern the work. No subagents were used.

Inspected MLW v1.2.7 SHA256:
B241034E4EC4B5E9389078B8CEF50B560638A85BDDC40EDBBC6BF83D49931AF1.
Its MVID is ecf22d06-2f8c-411c-80ab-6704956dd347; length 98816 bytes.
Installed and supplied DLLs match. The ZIP supplies compiled code/XML, not C#.
Native manifest is v1.4.8; TAOM manifest is v2.0.29.5.
Inspected CampaignSystem SHA256:
1F8E33E2ED73E6EC653D7629180AFB70649DDC6E5BD1657A802A264EFDA1C3AE.
ILSpy type output and earlier diagnostic evidence remain local under
Artifacts/compatibility/MyLittleWarband; no third-party binary is packaged.

Observed culture bug: CustomUnitsBehavior.SetCulture calls updateAppearance, which
changes body range without Race. It checks Townswoman/Townsman but dereferences
FemaleDancer/BasicTroop. CustomUnit.Update also restores a body range without Race.
Installed BasicCharacterObject.GetBodyProperties uses both Race and BodyPropertyRange
and its FillFrom copies both together. MLW's prefab already binds Edit beside
Culture to ChangeCulture; it lists distinct kingdom cultures.

Observed binary breaks: MBCharacterSkills.Skills now returns PropertyOwner<SkillObject>;
image/VM types moved; GauntletLayer reordered constructor arguments and now uses
GauntletMovieIdentifier; tooltip constructors gained parameters; FillFrom gained
parameters; HiddenInEncylopedia was corrected to HiddenInEncyclopedia; direct
AllEquipments/Culture accesses are nonpublic. Merely adding Harmony postfixes fails
while Harmony reads unresolved operands. Missing field/signature types also block
the old editor, so a skills-only method detour is insufficient.

## Owner implementation and boundaries

BinaryRuntime checks the exact source hash/MVID/length, then uses Cecil already
embedded in the installed Harmony to retarget 105 calls in 27 MLW methods. It emits
a differently named assembly into memory and never writes a replacement MLW file.
Moved type references and generic VM types are retargeted together. LegacyApi's
explicit factories preserve the old evaluation stack and supply current defaults.
No new external dependency is distributed.

The adapter runs before MLW. RuntimeLifecycle forwards the original submodule's
load/root/game-start/tick entry points through cached delegates to the adapted
submodule. Only the adapted MLW native hooks initialize, under a patch-owned Harmony
ID; repeated load forwarding is guarded. Original XML/GUI module resources remain
under their original IDs and ownership.

The original internal MCM settings type stays the sole settings owner. The adapted
runtime references an object plus cached access delegates, avoiding duplicate MCM
registration. Its duplicate SaveDefiner is removed; the original definer's type
operands register the adapted CustomUnit and Dictionary<string, CustomUnit>.
Native SaveableTypeDefiner source confirms IDs are its existing base plus local ID:
base 1436500005 and class ID 1 are retained. SaveableProperty names/types/IDs match
the original. Actual save-file round trips remain a native acceptance requirement.

The obsolete PartyUnitUpgradeLengthPatch is omitted: inspected 1.4.8
PartyCharacterVM.Character creates an entry for every UpgradeTarget and
InitializeUpgrades updates those entries. HasMoreThanTwoUpgrades no longer exists.
No native party UI method is replaced for that obsolete workaround.

CultureCompatibility hooks only the adapted MLW's appearance/copy/restore paths.
Custom IDs must be _basic_root or _elite_root followed by binary upgrade suffixes;
heroes and other troops are excluded. Use a matching-culture/gender townswoman,
then female dancer; otherwise BasicTroop and Townsman with the same checks. Copy
Race and BodyPropertyRange from the same valid template. Preserve appearance if
none is valid. No hardcoded race values, Empire fallback or additional save fields.
Mixed-race choices within a culture and equipment fitting are outside acceptance.

Original MLW public type identities are not replaced for other mods calling directly
into its assembly. Such third-party integrations are not qualified by this patch.
The existing MLW culture choice remains in its original save field. Keep the patch
enabled when using its runtime; removal restores the old incompatible implementation.

## Validation, performance and remaining gates

Test-Compatibility.ps1 builds Release, validates module/dependency/order/assembly
identity, runs the owner regression suite and confirms original MLW/TAOM DLL and manifest
hashes are unchanged. All 367 closed adapted methods JIT-prepare without invoking
gameplay. Tests execute the original save definer through the transpilers and
capture class/container registrations, verify the old property schema, bind native
Harmony targets, exercise culture/gender/fallback/isolation, and test startup twice.

Cast regression, 19 October 2026: Eye session events at 22:03:34 UTC identified
GauntletMovieIdentifier-to-GauntletMovie failure when opening the troop editor.
The previous adapter retargeted IGauntletMovie but missed the concrete GauntletMovie
reference used by the cast and field. The added test executes the actual adapted
cast operand and field assignment with an uninitialized instance of the real
GauntletMovieIdentifier class, and reproduces the exact InvalidCastException on
the previous code. Both old movie type references now resolve to the current handle.
The regression covers troop/equipment editor flows, object identity and field type;
it does not execute native rendering or claim a full editor/mission acceptance pass.

Culture/preview boundary regression: a subsequent user screenshot showed the
actual Appearance culture set to Gundabad Orcs with a human preview. The initial
bridge delegated SetCulture to the native setter alone and FillCharacter directly
to FillFrom; its tests exercised Synchronize separately rather than these entry
points. New actual CharacterObject tests reproduce a human-to-orc culture write
that leaves Race unchanged on that bridge. SetCulture now synchronizes after the
culture write; FillCharacter synchronizes before the native preview reads the troop.
The preview regression captures that native method entry without running rendering.
Both boundaries remain restricted to MLW custom trees and preserve native troops
and missing-template behavior. No game data, new save fields or culture defaults
are changed. Native rendered appearance still requires user-started validation.

Menu follow-up, 20 October 2026: user screenshots show an orc in the encyclopedia,
a stale human party-row portrait and human editor previews for nonhuman cultures.
Read-only inspection of MLW GUI/Prefabs/CustomUnits.xml confirms its UnitCharacter
CharacterTableauWidget has no Race binding. Native CharacterTableauWidget forwards
its separate Race property to the renderer; changing only body properties cannot
select the race. Native WidgetPrefab.LoadFrom reads XML before constructing the
widget templates. The patch adds Race="@Race" to only that missing binding in
the in-memory document for this exact MLW prefab path. Existing bindings, other
widgets and files stay unchanged; no copied prefab or replacement art is shipped.

Native PartyCharacterVM builds Code only when its Character reference changes;
RefreshValues does not rebuild Code. MLW edits the existing CharacterObject and
DeleteVMLayer does not refresh the party rows. The patch weakly observes the
current PartyVM lifecycle and regenerates only custom nonhero row image codes in
its four current troop/prisoner lists when the MLW editor closes, using the native
GetCharacterCode method so faction colors, equipment and race remain native-owned.
Finalization clears the weak observation. Release regression checks pass for
the actual native prefab loader and adapted MLW editor-close hook, with widget
construction and native image generation excluded from the offline host.
This adds no campaign scan, per-frame
work, extra save state or race mutation from the portrait refresh. Acceptance:
actual source-prefab binding regression, native row-code/cache regression, foreign
path/widget/troop isolation, lifecycle cleanup, Release checks and hash-verified
owner-only deployment. Native rendered editor/party acceptance remains pending.

Startup is bounded to the pinned 98816-byte input, <=128 types, <=512 methods and
<=30000 instructions. Temporary local installation guard: <10000 ms. Measured final
result is recorded in validation.json (about 0.7 seconds on this host). Hot culture
work checks at most three templates and scans only the troop ID; no per-tick scan,
reflection, IO or logging was added. Legacy callback forwarding uses cached delegates.
Temporary local guard: 100000 culture synchronizations <2000 ms; measured results
are in validation.json (1-2 ms on this Windows x64 CLR 4.0.30319.42000 host, Release).
These microchecks use the researched bounded cost, not a comparative optimization
claim or native frame-time/soak certification.

The central helper has no catalog/runtime lane for this independent owner. Local
owner tests were added here; no unrelated catalog or suite module was changed.
Build/deploy preflight resolves the independent owner only. Packaging checks the
tested DLL's SHA256 and ZIP layout. No deployment or game startup is performed.

Next researched gate: user-started Native 1.4.8 campaign with TAOM 2.0.29.5 and this
exact MLW DLL, adapter before MLW. Verify module startup/MCM/editor UI, human and
Gundabad culture choices, supported genders/upgrades, equipment and mission
appearance, save/reload and unaffected normal troops. Missing template data,
other MLW features, actual save serialization, native performance and other-mod
integrations remain unqualified until their corresponding native cases run.

## Equipment category enum regression, 30 October 2026

Current request: boots display helmets and body armor is empty in MLW 1.2.7.
Inspected current TaleWorlds.Core.dll SHA256
3E5CECE8CA42FAF3FDDE27632519ED2A6F86D1EC6D1A742867729F00001689BF.
Its ItemTypeEnum contains SlingStones and Sling, shifting the old armor and
weapon constants. The pinned MLW getRequiredItemType returns current HeadArmor
for Leg and Goods for Body. Its stringToWeaponType and isWeaponType also retain
old constants. Source/type inspection is local under equipment-categories.

Authorized correction: replace only those three helpers in the patch-owned
in-memory runtime with named current enum mappings. Preserve their signatures,
slot categories, twelve supported weapon categories, ordering, fresh-list behavior
and unknown-input defaults. No original mod files, XML, saves, equipment contents,
other MLW behavior, new weapon support or dependencies are changed. Do not rewrite
all numeric constants or change native enum values. Existing source hash/MVID pin
and adaptation bounds remain required; incompatible helper signatures must fail.

Tests execute the actual adapted helpers, reproduce the boot regression before
the fix, cover all original slots/weapon labels and classify all current enum
values. Existing Release/JIT/schema/loader/portrait/culture/isolation tests must
still pass. Existing startup guard is 10 seconds; the equipment selector adds a
temporary measured 100000-call/2000ms guard. Adapter changes run at startup only;
selector work remains bounded with the original fresh list allocation. No tick
scan, reflection lookup, IO or new cache is added to equipment filtering.

Native equipment editor and campaign/save acceptance remain NOT_RUN. Deployment
of this owner is separate from the prior Fourberie-only deployment permission.

Validation result: the regression failed on the pre-fix adapted Leg selector
(actual HeadArmor), then passed after the scoped helper replacement. The full
owner command Test-Compatibility.ps1 passed: Release build 0 warnings/errors,
301 assertions, 367 adapted method bodies JIT-prepared, save schema retained,
original mod/prefab hashes unchanged. Adapter startup/native Harmony binding was
480ms against the existing 10000ms guard; 100000 actual adapted equipment
selections took 2ms against the 2000ms guard (Release, Windows x64 CLR
4.0.30319.42000). These are managed timings, not native UI/frame-time proof.
The adapter now reports 108 redirected calls across 30 methods, including these
three category helpers. Evidence: Artifacts/compatibility/MyLittleWarband/validation.json.
Deployment and native editor/save acceptance for this change remain NOT_RUN.
Next authorized gate: owner-only MLW patch deployment, then user-started checks
of Head/Body/Leg/Gloves/Cape/Horse/Harness and weapon-filter choices. Confirm
visible results and equipment selection, then save/reload in the campaign.


## Complete researched ItemType boundaries, 31 October 2026

User authorized integration of MLWShieldFix into this existing owner and checking
other equipment for the same enum mismatch. No standalone module, native enum
change, original-MLW modification, save migration or live deployment is included.
The supplied standalone source was reviewed, but is not added as a second runtime
patch layer. EquipmentEnumCompatibility participates in BinaryRuntime.Build after
the existing three category bridges and before Assembly.Load of the adapted bytes.
The original MLW SHA256/MVID gate and size/type/method/instruction bounds remain.

Read-only Cecil inspection of the installed MLW 1.2.7 and Core binaries found:

| Surface | Evidence and disposition |
| --- | --- |
| CustomUnitsBehavior.isRanged | Old 8/9 comparisons at IL_0024/002d classify Shield as ranged and miss Crossbow. Resolve current Bow/Crossbow by enum name during adaptation. Preserve the original loop, null-item handling and default Equipment selection. |
| EquipmentCardVM.GetItemTypeWithItemObject | Old ItemType switch maps current HeadArmor/BodyArmor into Leg/Gloves; other armor and Cape/HorseHarness/Banner are displaced. Bridge only this instance method to a named current mapping; preserve null and WeaponComponent fallback. |
| EquipmentCardVM constructor | Weapon-component ItemType stored in local 14: old Thrown=10 at 0568, Shield=7 at 0632, Bow=8 at 0689, Crossbow=9 at 068e and 0741. Resolve those five operands by current enum name. Other comparisons 2/3/4 and 5/6 already match current melee/ammunition categories. |
| Existing getRequiredItemType/stringToWeaponType/isWeaponType | Already adapted by EquipmentCategoryCompatibility; retained and tested across slots, labels and every current enum member. |
| UpgradeGear2 and MatchesFilter | Consume actual current item types through the corrected category/weapon helpers and typed lists; no additional stale literal correction identified at those boundaries. |
| Equipment-card type/weapon labels | Read current enum values for native localization; no old literal mapping substituted. |

Every direct ItemType getter/field and GetItemTypeFromWeaponClass use in the
original assembly was inventoried for this audit, including nested types. It is
an equipment-enum audit, not certification of all MLW features or future game APIs.
EquipmentIndex slots retain the original policy; new Sling/SlingStones support
would be a feature change and is not inferred from this compatibility correction.

The seven enum operands are selected by exact offsets in the hash-pinned input
and checked against their expected original opcodes/values before each method's
mutation. This is not a global integer remapping. Enlarged constants are followed
by widening the affected methods' short branches, retaining target objects and
exception regions. The slot bridge validates parameter/return identity and uses
argument 1 because the original helper is an instance method. JIT checks include
the modified constructor and all other closed adapted method bodies.

Regression tests execute the adapted predicate for every current item type in
all four weapon slots; mixed equipment, removing weapons, mounts, empty equipment,
and default-versus-secondary variants; every current item type through the card
slot helper, null and WeaponComponent fallback; and the constructor's actual ten
enum comparison operands through isolated IL probes. Native tooltip rendering is
not simulated or claimed. MLW's actual SetDefaultGroup branches and both formation
properties are tested for all four classes and crossbow removal; only the initial
Game.Current.ObjectManager troop lookup is stubbed. The tests do not prove all
campaign edit/load paths invoke that operation.

Validation: the new suite first failed on the old adapter with Shield in slot 0.
After integration, Release build passed with zero warnings/errors, 466 assertions
(165 equipment regressions), 367 JIT-prepared method bodies, unchanged save schema,
startup idempotence and unchanged original game/mod/prefab hashes. On Windows x64
CLR 4.0.30319.42000, 100000 adapted four-slot classifications took 6ms (guard 2000ms)
and total adapter startup/binding took 412ms (guard 10000ms). Latest packaging run
measurements and exact DLL hash are recorded in validation.json. Classification
keeps the original constant-size four-slot loop; the card helper is a bounded
switch. Reflection, metadata edits and enum-name resolution are startup-only.
No tick scans, new state, logging or IO were added to equipment operations.

Owner managed and static package gates are available via Test-Compatibility.ps1
and Build-Package.ps1. The central test helper has no mapped native lane for this
independent owner. Native UI/rendering, full campaign edits, serialized save/load
and mod combinations remain NOT_RUN. Next gate: use the updated owner package
without MLWShieldFix, apply shield-only/bow/crossbow/mounted troop edits, inspect
armor and weapon cards, enter a mission, save/reload and verify the same results.
Deployment is a separate user-authorized operation; no game is started by tooling.
