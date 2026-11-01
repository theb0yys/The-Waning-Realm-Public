# TAOM My Little Warband compatibility

Local managed validation passes. Native campaign/UI/save-load validation is pending.
Targets: supplied My Little Warband v1.2.7, Bannerlord v1.4.8, TAOM v2.0.29.5.

## Install and use

Extract the ZIP into the game's Modules folder. It contains one folder named
TAOM_Standalone_Compat_MyLittleWarband, with SubModule.xml directly inside it.
Keep the original My Little Warband and TAOM installations enabled and unchanged.

Load in this order:

1. TAOM.Dependencies and TAOM, in their existing valid order.
2. TAOM My Little Warband Compatibility.
3. My Little Warband.

The patch must run BEFORE My Little Warband initializes its old hooks. Its manifest
declares that ordering. No original manifest or DLL needs replacing.

In MLW's custom troop editor, use **Edit beside Culture**. Choose a human culture
for human troops, or the desired orc culture for orc troops. MLW already provides
the button. The patch synchronizes race and body range with that culture's valid
template when editing, copying and restoring custom troops. Change each existing
troop that needs another culture; this does not automatically convert its upgrades.
The original list of kingdom cultures is retained.

## What the binary adapter does

At startup the TAOM patch checks the original MLW DLL's exact hash, reads it without
writing it, and adapts its API references into a separately named, memory-only
runtime. No modified MLW DLL is installed or bundled. MLW remains required for its
original code, troop XML and GUI files.

The adapter handles the changed skills getter, image classes, tooltips, character
preview, Gauntlet movies, encyclopedia property and private culture/equipment
access. It forwards MLW's startup/campaign/tick callbacks to the adapted runtime.
It keeps the original MCM settings owner and save definer; save type IDs and troop
property IDs remain unchanged. The obsolete upgrade-list patch is omitted because
1.4.8 already creates entries for every upgrade and removed its legacy flag.

Race is reconstructed from MLW's saved Culture. The culture correction excludes
heroes and non-custom troops. A missing same-culture/gender template is preserved
by the correction instead of guessing a race. MLW's original save restoration
still needs its data to be valid. Distinct races within one mixed-race culture and
equipment fitting are outside this patch's acceptance scope.

## Validation and limits

Release builds, the owner managed assertions, JIT preparation of all 367 closed adapted
method bodies, native-method Harmony binding, save schema/registration checks,
startup idempotence and original-file hash checks pass. These are local managed
checks; no game was started. No deployment was performed.

The editor movie-handle regression reproduces the reported InvalidCastException
before the correction. Both concrete GauntletMovie casts/storage and IGauntletMovie
references are now retargeted to GauntletMovieIdentifier. The test executes the
adapted cast and field assignment for troop and equipment editors using an actual
handle type, without running the renderer. Full native UI validation remains pending.

Culture changes now also synchronize custom troop race/body range directly at the
compatibility setter and before the native character preview reads it. The added
boundary regression covers changing a human custom recruit to an orc culture and
repairing stale preview input, while leaving ordinary troop races unchanged.

The next gate is a user-started campaign: open the editor, change between a human
culture and Gundabad, inspect supported genders/upgrades/equipment, enter a mission,
then save/reload and confirm persistence. Native rendering and actual serialized
save compatibility are not certified. Keep the patch enabled for saves using its
adapted runtime; disabling it returns to the incompatible original MLW code.

Validation details and hashes are in validation.json and PACKAGE_DESIGN.md.
The adapter fails closed for a different MLW DLL. Changes to Harmony's privately
bundled Cecil or further game API changes require requalification.

Run Test-Compatibility.ps1 for the owner checks or Build-Package.ps1 for a checked
local ZIP. Neither script deploys files or starts the game.

## Integrated equipment enum correction (31 October 2026)

This build includes the shield/crossbow classification fix. Do not install the
separate MLWShieldFix module alongside it; no extra module is required. Keep the
existing load order above. The correction is applied before the adapted runtime
is loaded, rather than waiting for a Harmony scan of newly loaded assemblies.

The equipment audit also corrected the equipment-card armor/cape/harness/banner
slot mapping and shield/bow/crossbow/thrown tooltip comparisons. Current item types
are used only at the researched enum boundaries; unrelated numeric values are not
shifted. The original four weapon slots, default equipment variant, thrown-weapon
classification policy and weapon-component fallback are preserved. This does not
add sling or sling-ammunition support to MLW's legacy editor.

Managed validation passes 466 assertions, including 165 equipment regressions,
all 367 adapted method bodies preparing successfully, and the actual MLW formation
assignment branches with campaign object lookup stubbed. The original shield and
armor-card failures are reproduced in the unadapted DLL. The new regression also
failed against the previous adapter before the correction. No renderer or campaign
was started. Native equipment cards, troop updates and save/reload remain pending.

Existing troops are corrected when MLW runs its normal formation recalculation;
the patch does not sweep campaigns or rewrite saves. In a user-started campaign,
edit/apply equipment for a previously affected troop, verify infantry/cavalry versus
bow/crossbow classes, check armor and weapon cards, then save/reload and repeat.
