# Minas Tirith equipment test stock

User request: add the 75 registered Modular Knights items to Gondor's Minas
Tirith shop for testing. This non-suite owner supplies one small DLL loaded by
the existing TAOM_EquipmentAssets module. No additional selectable module,
Harmony patches, external dependencies, asset import or suite deployment.

Research path: owner request -> protected-files and compatibility/test gates ->
installed 1.4.8 CampaignEvents, Helpers.InventoryScreenHelper and ItemRoster ->
this handler -> Release build, deterministic policy/package tests and scoped
hash deployment -> user-started campaign load and Minas Tirith Trade inspection.
TaomWorldBindings maps Minas Tirith exactly to town_EW1. InventoryScreenHelper
OpenTradeWithCurrentSettlement passes Settlement.CurrentSettlement.ItemRoster
to OpenScreenAsTrade. ItemRoster exposes FindIndexOfElement, GetElementNumber and
AddToCounts. GetItemNumber returns only the first item match and can select a
modified stack, so count the exact unmodified EquipmentElement that AddToCounts
will increase. This avoids repeated additions when a modified stack appears first.
CampaignEvents supplies OnSessionLaunchedEvent and SettlementEntered with
(MobileParty, Settlement, Hero). Decompilation is in the existing Modular Knights
mesh evidence directory; native code/files are read-only.

On session launch resolve the exact 75 registered IDs once and top up Minas
Tirith's native town ItemRoster to three of each. On main-party entry top up
again. Skip missing items safely; never replace existing stock, remove surplus,
restock another town or invent IDs. Maintain three unmodified copies; existing
modified stacks remain untouched and do not participate in the top-up count.
All items keep is_merchandise=false: the handler explicitly supplies this test
shop, without enabling global merchant generation. Troop assignments unchanged.

Budget: at most 75 catalogue lookups per session and 75 roster count checks per
matching entry; no daily/frame ticks, world scans, IO, reflection or logs. The
native roster's FindIndexOfElement scan makes matching-entry cost O(75 * town stock);
other settlements and NPC entries return before any roster work. No new runtime
allocation proportional to the world. Test against a large stock dictionary and
verify exactly 75 count operations; native roster timing remains unmeasured.

Compatibility: no custom save fields or serialized types. Native shop inventory
persists added items; keep TAOM_EquipmentAssets enabled and retain its item IDs.
Handler can be removed to stop replenishment without deleting purchased/stocked
items. Existing saves are intended to work on next campaign load, but native
save/load is not independently proven. Do not remove the asset module from a save
that contains its items. No existing content IDs change.

Deploy only TAOM_EquipmentTestShop.dll and the required TAOM_EquipmentAssets
SubModule.xml entry, after backing up the manifest/any existing DLL. Preserve
published packages, source assets and all other modules. Native inventory,
icon, weapon grip/collision and save/reload proof require the user's game session;
the agent never starts Bannerlord. Four weapon collision limitations recorded
in the equipment registration design remain unchanged.
