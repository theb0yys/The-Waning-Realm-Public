using System;
using System.Reflection;
using Helpers;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Fourberie_CompatPatch
{
    [HarmonyPatch]
    public static class MapPartyVisualWeaponSafetyPatch
    {
        private const string TargetTypeName = "SandBox.View.Map.Visuals.MobilePartyVisual";
        private static MethodBase _cachedTarget;
        private static bool _prepared;

        public static int ResolvedTargetCount
        {
            get { return _cachedTarget == null ? 0 : 1; }
        }

        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (_prepared)
                return _cachedTarget != null;

            _prepared = true;
            Type targetType = AccessTools.TypeByName(TargetTypeName);
            if (targetType != null)
            {
                _cachedTarget = AccessTools.Method(
                    targetType,
                    "GetMeleeWeaponToWield",
                    new[] { typeof(PartyBase), typeof(int).MakeByRefType() });
            }

            if (_cachedTarget == null)
            {
                CompatLog.Info("Map weapon patch skipped: exact Bannerlord v1.4.7 target was not found.");
                return false;
            }

            CompatLog.Info("Map weapon patch target resolved: " + _cachedTarget);
            return true;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return _cachedTarget;
        }

        public static bool Prefix(PartyBase party, ref int wieldedItemIndex)
        {
            try
            {
                wieldedItemIndex = -1;

                CharacterObject visualLeader = GetVisualPartyLeaderSafely(party);
                if (visualLeader == null || visualLeader.Equipment == null)
                {
                    CompatLog.Once("map-missing-visual-leader", "Map icon weapon fallback: missing visual leader/equipment.");
                    return false;
                }

                for (int i = 0; i < 5; i++)
                {
                    EquipmentElement element;
                    try
                    {
                        element = visualLeader.Equipment[i];
                    }
                    catch (Exception ex)
                    {
                        CompatLog.Once("map-equipment-slot", "Map icon weapon fallback: equipment slot access failed: " + ex.GetType().Name + ".");
                        continue;
                    }

                    ItemObject item = element.Item;
                    if (item == null)
                        continue;

                    WeaponComponentData primaryWeapon = item.PrimaryWeapon;
                    if (primaryWeapon == null)
                    {
                        CompatLog.Once("map-item-without-primary-weapon", "Map icon weapon fallback: an equipped item has no primary weapon.");
                        continue;
                    }

                    if (primaryWeapon.IsMeleeWeapon)
                    {
                        wieldedItemIndex = i;
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                wieldedItemIndex = -1;
                CompatLog.Once("map-prefix-exception", "Map icon weapon fallback: prefix failed. Exception=" + ex);
                return false;
            }
        }

        public static Exception Finalizer(Exception __exception, ref int wieldedItemIndex)
        {
            if (__exception == null)
                return null;

            wieldedItemIndex = -1;
            CompatLog.Once("map-finalizer-exception", "Map icon weapon fallback: suppressed GetMeleeWeaponToWield exception. Exception=" + __exception);
            return null;
        }

        private static CharacterObject GetVisualPartyLeaderSafely(PartyBase party)
        {
            if (party == null)
                return null;

            try
            {
                return PartyBaseHelper.GetVisualPartyLeader(party);
            }
            catch (Exception ex)
            {
                CompatLog.Once("map-party-helper-exception", "Map icon weapon fallback: PartyBaseHelper.GetVisualPartyLeader failed: " + ex.GetType().Name + ".");
            }

            try
            {
                if (party.LeaderHero != null && party.LeaderHero.CharacterObject != null)
                    return party.LeaderHero.CharacterObject;
            }
            catch
            {
            }

            try
            {
                if (party.MobileParty != null && party.MobileParty.LeaderHero != null)
                    return party.MobileParty.LeaderHero.CharacterObject;
            }
            catch
            {
            }

            return null;
        }

    }
}
