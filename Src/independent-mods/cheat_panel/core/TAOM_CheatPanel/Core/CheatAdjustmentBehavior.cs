using TAOM_CheatPanel.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TAOM_CheatPanel.Core;

namespace TAOM_CheatPanel.Runtime
{
    public sealed class CheatAdjustmentBehavior : CampaignBehaviorBase
    {
        private static readonly string[] FoodItemIds =
        {
            "grain",
            "fish",
            "meat",
            "butter",
            "cheese",
            "date_fruit",
            "olives",
            "grapes",
            "beer",
            "wine"
        };

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static void OnDailyTick()
        {
            try
            {
                if (!CheatPanelSettingsAccess.EnablePassiveCheatAdjustments)
                {
                    return;
                }

                Hero hero = Hero.MainHero;
                MobileParty party = MobileParty.MainParty;
                if (hero == null || party == null)
                {
                    return;
                }

                List<string> changes = new List<string>();
                ApplyDailyStipends(hero, changes);
                ApplyPartyUpkeep(hero, party, changes);
                ApplyOwnedSettlementStabiliser(hero, changes);

                if (changes.Count > 0)
                {
                    string summary = "Passive cheat adjustments: " + string.Join("; ", changes.ToArray()) + ".";
                    RecentActionsLog.Record(summary);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("cheat.adjustments.daily", ex);
            }
        }

        private static void ApplyDailyStipends(Hero hero, List<string> changes)
        {
            int gold = CheatPanelSettingsAccess.DailyGoldStipend;
            if (gold > 0)
            {
                hero.ChangeHeroGold(gold);
                changes.Add("+" + gold.ToString("N0") + " gold");
            }

            Clan clan = hero.Clan;
            if (clan == null)
            {
                return;
            }

            int influence = CheatPanelSettingsAccess.DailyInfluenceStipend;
            if (influence > 0)
            {
                clan.Influence += influence;
                changes.Add("+" + influence.ToString("N0") + " influence");
            }

            int renown = CheatPanelSettingsAccess.DailyRenownStipend;
            if (renown > 0)
            {
                clan.AddRenown(renown, shouldNotify: false);
                changes.Add("+" + renown.ToString("N0") + " renown");
            }
        }

        private static void ApplyPartyUpkeep(Hero hero, MobileParty party, List<string> changes)
        {
            if (CheatPanelSettingsAccess.KeepMainHeroHealed && hero.HitPoints < hero.MaxHitPoints)
            {
                hero.HitPoints = hero.MaxHitPoints;
                changes.Add("main hero healed");
            }

            if (CheatPanelSettingsAccess.KeepCompanionsHealed)
            {
                int healed = HealCompanions(hero);
                if (healed > 0)
                {
                    changes.Add(healed + " companion(s) healed");
                }
            }

            if (CheatPanelSettingsAccess.RecoverWoundedTroopsDaily)
            {
                int recovered = RecoverWoundedTroops(party);
                if (recovered > 0)
                {
                    changes.Add(recovered + " wounded troop(s) recovered");
                }
            }

            if (CheatPanelSettingsAccess.KeepPartyMoraleAtMax && party.RecentEventsMorale < 100f)
            {
                party.RecentEventsMorale = 100f;
                changes.Add("morale set to 100");
            }

            if (CheatPanelSettingsAccess.AutoRestockPartyFood)
            {
                int added = AddFoodBundle(party, 25);
                if (added > 0)
                {
                    changes.Add(added + " food item(s) restocked");
                }
            }

            if (CheatPanelSettingsAccess.AutoRestoreSmithingStamina)
            {
                int restored = RestoreSmithingStamina(party);
                if (restored > 0)
                {
                    changes.Add("smithing stamina restored for " + restored + " hero(es)");
                }
            }
        }

        private static void ApplyOwnedSettlementStabiliser(Hero hero, List<string> changes)
        {
            if (!CheatPanelSettingsAccess.StabiliseOwnedSettlements)
            {
                return;
            }

            Clan clan = hero.Clan;
            if (clan == null || clan.Fiefs == null)
            {
                return;
            }

            int touched = 0;
            int garrisonAdded = 0;
            int loyaltyFloor = CheatPanelSettingsAccess.OwnedSettlementLoyaltyFloor;
            int securityFloor = CheatPanelSettingsAccess.OwnedSettlementSecurityFloor;
            int prosperityFloor = CheatPanelSettingsAccess.OwnedSettlementProsperityFloor;
            int garrisonDaily = CheatPanelSettingsAccess.DailyOwnedGarrisonReinforcement;

            foreach (Town town in clan.Fiefs)
            {
                try
                {
                    if (town == null || town.Settlement == null)
                    {
                        continue;
                    }

                    bool changed = false;
                    if (town.Loyalty < loyaltyFloor)
                    {
                        town.Loyalty = loyaltyFloor;
                        changed = true;
                    }

                    if (town.Security < securityFloor)
                    {
                        town.Security = securityFloor;
                        changed = true;
                    }

                    if (prosperityFloor > 0 && town.Prosperity < prosperityFloor)
                    {
                        town.Prosperity = prosperityFloor;
                        changed = true;
                    }

                    if (CheatPanelSettingsAccess.FillOwnedSettlementFood)
                    {
                        float cap = town.FoodStocksUpperLimit();
                        if (town.FoodStocks < cap)
                        {
                            town.FoodStocks = cap;
                            changed = true;
                        }
                    }

                    if (garrisonDaily > 0 && town.GarrisonParty != null)
                    {
                        CultureObject culture = town.Settlement != null ? town.Settlement.Culture : null;
                        CharacterObject elite = culture != null ? culture.EliteBasicTroop : null;
                        CharacterObject basic = culture != null ? culture.BasicTroop : null;
                        string cultureId = culture != null ? culture.StringId : null;
                        CharacterObject troop = TroopEquipmentSafety.IsSpawnSafe(elite)
                            ? elite
                            : TroopEquipmentSafety.IsSpawnSafe(basic)
                                ? basic
                                : TroopEquipmentSafety.ResolveBestArmoredTroop(4, cultureId, TroopRolePreference.Infantry)
                                    ?? TroopEquipmentSafety.ResolveBestArmoredTroop(4, null, TroopRolePreference.Infantry);
                        if (troop != null)
                        {
                            town.GarrisonParty.AddElementToMemberRoster(troop, garrisonDaily);
                            garrisonAdded += garrisonDaily;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        touched++;
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("cheat.adjustments.fief." + (town?.Settlement?.StringId ?? "unknown"), ex);
                }
            }

            if (touched > 0)
            {
                string line = "stabilised " + touched + " owned fief(s)";
                if (garrisonAdded > 0)
                {
                    line += ", +" + garrisonAdded + " garrison troop(s)";
                }

                changes.Add(line);
            }
        }

        private static int HealCompanions(Hero hero)
        {
            int healed = 0;
            foreach (Hero companion in hero.CompanionsInParty)
            {
                if (companion != null && companion.HitPoints < companion.MaxHitPoints)
                {
                    companion.HitPoints = companion.MaxHitPoints;
                    healed++;
                }
            }

            return healed;
        }

        private static int RecoverWoundedTroops(MobileParty party)
        {
            TroopRoster roster = party.MemberRoster;
            int recovered = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                TroopRosterElement element = roster.GetElementCopyAtIndex(i);
                if (element.Character != null && element.WoundedNumber > 0)
                {
                    roster.AddToCounts(element.Character, 0, woundedCount: -element.WoundedNumber);
                    recovered += element.WoundedNumber;
                }
            }

            return recovered;
        }

        private static int AddFoodBundle(MobileParty party, int each)
        {
            if (party.ItemRoster == null || Game.Current == null || Game.Current.ObjectManager == null)
            {
                return 0;
            }

            int added = 0;
            for (int i = 0; i < FoodItemIds.Length; i++)
            {
                ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>(FoodItemIds[i]);
                if (item != null)
                {
                    party.ItemRoster.AddToCounts(item, each);
                    added += each;
                }
            }

            return added;
        }

        private static int RestoreSmithingStamina(MobileParty party)
        {
            try
            {
                Type craftType = ResolveLoadedType("TaleWorlds.CampaignSystem.CampaignBehaviors.CraftingCampaignBehavior, TaleWorlds.CampaignSystem");
                if (craftType == null)
                {
                    return 0;
                }

                MethodInfo getCampaignBehavior = typeof(Campaign).GetMethod("GetCampaignBehavior");
                if (getCampaignBehavior == null)
                {
                    return 0;
                }

                object behavior = getCampaignBehavior.MakeGenericMethod(craftType).Invoke(Campaign.Current, null);
                if (behavior == null)
                {
                    return 0;
                }

                FieldInfo field = craftType.GetField("_heroCraftingStamina", BindingFlags.NonPublic | BindingFlags.Instance);
                Dictionary<Hero, int> stamina = field != null ? field.GetValue(behavior) as Dictionary<Hero, int> : null;
                if (stamina == null)
                {
                    return 0;
                }

                int restored = 0;
                List<TroopRosterElement> heroes = party.MemberRoster.GetTroopRoster()
                    .Where(e => e.Character != null && e.Character.IsHero && e.Character.HeroObject != null)
                    .ToList();
                for (int i = 0; i < heroes.Count; i++)
                {
                    stamina[heroes[i].Character.HeroObject] = 100;
                    restored++;
                }

                return restored;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("cheat.adjustments.smithing", ex);
                return 0;
            }
        }

        private static Type ResolveLoadedType(string assemblyQualifiedTypeName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedTypeName))
                return null;

            string typeName = assemblyQualifiedTypeName;
            string assemblyName = null;
            int comma = typeName.IndexOf(',');
            if (comma >= 0)
            {
                assemblyName = typeName.Substring(comma + 1).Trim();
                typeName = typeName.Substring(0, comma).Trim();
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null)
                    continue;

                try
                {
                    if (!string.IsNullOrWhiteSpace(assemblyName))
                    {
                        AssemblyName name = assembly.GetName();
                        if (name == null || !string.Equals(name.Name, assemblyName, StringComparison.Ordinal))
                            continue;
                    }

                    Type type = assembly.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
