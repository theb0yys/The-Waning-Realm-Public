using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TAOM.EquipmentTestShop
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter starter)
                starter.AddBehavior(new EquipmentShopBehavior());
        }
    }

    internal sealed class EquipmentShopBehavior : CampaignBehaviorBase
    {
        private readonly Dictionary<string, ItemObject> _items = new Dictionary<string, ItemObject>(75);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Native settlement ItemRoster persists the stock; no custom save keys/types.
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _items.Clear();
            foreach (string id in EquipmentIds.All)
            {
                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (item != null) _items.Add(id, item);
            }
            Stock(Settlement.Find(TestStock.SettlementId));
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != null && party == MobileParty.MainParty)
                Stock(settlement);
        }

        private void Stock(Settlement settlement)
        {
            if (settlement == null || !settlement.IsTown || settlement.StringId != TestStock.SettlementId)
                return;
            var roster = settlement.ItemRoster;
            TestStock.Apply(settlement.StringId, settlement.IsTown,
                id =>
                {
                    if (!_items.TryGetValue(id, out ItemObject item)) return -1;
                    int index = roster.FindIndexOfElement(new EquipmentElement(item));
                    return index < 0 ? 0 : roster.GetElementNumber(index);
                },
                (id, amount) => roster.AddToCounts(_items[id], amount));
        }
    }
}
