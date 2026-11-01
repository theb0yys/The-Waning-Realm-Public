using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TAOM_CheatPanel.Core
{
    internal enum TroopRolePreference
    {
        Any = 0,
        Infantry = 1,
        Ranged = 2,
        Mounted = 3
    }

    internal static class TroopEquipmentSafety
    {
        internal static bool IsSpawnSafe(CharacterObject troop)
        {
            if (troop == null || troop.IsHero || troop.Tier <= 0 || troop.IsTemplate || troop.IsChildTemplate)
            {
                return false;
            }

            Equipment battleEquipment = GetBestBattleEquipment(troop);
            Equipment civilianEquipment = GetBestCivilianEquipment(troop);
            bool hasCivilianEquipment = HasAnyCivilianEquipment(troop);

            return HasBodyGear(battleEquipment)
                && HasUsableBattleWeapon(battleEquipment)
                && (!hasCivilianEquipment || HasBodyGear(civilianEquipment));
        }

        internal static CharacterObject ResolveBestArmoredTroop(int tier, string cultureId, TroopRolePreference preference)
        {
            bool anyCulture = string.IsNullOrEmpty(cultureId);
            IEnumerable<CharacterObject> query = CharacterObject.All
                .Where(c => c != null
                         && !c.IsHero
                         && c.Tier > 0
                         && (anyCulture || string.Equals(c.Culture != null ? c.Culture.StringId : string.Empty, cultureId, StringComparison.OrdinalIgnoreCase)))
                .Where(IsSpawnSafe);

            return query
                .OrderBy(c => c.Tier == tier ? 0 : (c.Tier < tier ? 1 : 2))
                .ThenBy(c => Math.Abs(c.Tier - tier))
                .ThenBy(c => PreferenceScore(c, preference))
                .ThenBy(c => c.Name != null ? c.Name.ToString() : c.StringId ?? string.Empty)
                .FirstOrDefault();
        }

        internal static CharacterObject ResolveSafeUpgradeTarget(CharacterObject troop)
        {
            if (troop == null || troop.IsHero)
            {
                return troop;
            }

            CharacterObject best = IsSpawnSafe(troop) ? troop : null;
            List<CharacterObject> seen = new List<CharacterObject>();
            Queue<CharacterObject> pending = new Queue<CharacterObject>();
            EnqueueUpgradeTargets(troop, pending);

            int safety = 0;
            while (pending.Count > 0 && safety++ < 48)
            {
                CharacterObject candidate = pending.Dequeue();
                if (candidate == null || candidate.IsHero || seen.Contains(candidate))
                {
                    continue;
                }

                seen.Add(candidate);
                if (IsSpawnSafe(candidate) && (best == null || candidate.Tier > best.Tier))
                {
                    best = candidate;
                }

                EnqueueUpgradeTargets(candidate, pending);
            }

            if (best != null)
            {
                return best;
            }

            string cultureId = troop.Culture != null ? troop.Culture.StringId : null;
            return ResolveBestArmoredTroop(Math.Max(1, troop.Tier), cultureId, PreferenceFromTroop(troop))
                ?? ResolveBestArmoredTroop(Math.Max(1, troop.Tier), null, PreferenceFromTroop(troop));
        }

        internal static int RepairHeroVisualGear(Hero hero)
        {
            if (hero == null)
            {
                return 0;
            }

            try
            {
                string cultureId = hero.Culture != null ? hero.Culture.StringId : null;
                CharacterObject donor = ResolveBestArmoredTroop(3, cultureId, TroopRolePreference.Any)
                    ?? ResolveBestArmoredTroop(3, null, TroopRolePreference.Any);
                if (donor == null)
                {
                    return 0;
                }

                Equipment donorBattle = GetBestBattleEquipment(donor);
                Equipment donorCivilian = GetBestCivilianEquipment(donor) ?? donorBattle;
                int repaired = 0;
                repaired += RepairEmptyArmorSlots(hero.BattleEquipment, donorBattle);
                repaired += RepairEmptyArmorSlots(hero.CivilianEquipment, donorCivilian);

                if (repaired > 0)
                {
                    hero.CheckInvalidEquipmentsAndReplaceIfNeeded();
                }

                return repaired;
            }
            catch
            {
                return 0;
            }
        }

        internal static TroopRolePreference PreferenceFromTroop(CharacterObject troop)
        {
            if (troop == null)
            {
                return TroopRolePreference.Any;
            }

            if (troop.IsMounted)
            {
                return TroopRolePreference.Mounted;
            }

            if (troop.IsRanged)
            {
                return TroopRolePreference.Ranged;
            }

            if (troop.IsInfantry)
            {
                return TroopRolePreference.Infantry;
            }

            return TroopRolePreference.Any;
        }

        internal static string DescribeSafety(CharacterObject troop)
        {
            if (troop == null)
            {
                return "missing troop";
            }

            Equipment battleEquipment = GetBestBattleEquipment(troop);
            if (battleEquipment == null)
            {
                return troop.StringId + " has no battle equipment";
            }

            if (!HasBodyGear(battleEquipment))
            {
                return troop.StringId + " has no battle body gear";
            }

            if (!HasUsableBattleWeapon(battleEquipment))
            {
                return troop.StringId + " has no battle weapon";
            }

            if (HasAnyCivilianEquipment(troop) && !HasBodyGear(GetBestCivilianEquipment(troop)))
            {
                return troop.StringId + " has no civilian/map body gear";
            }

return troop.StringId + " has battle gear";
        }

        private static Equipment GetBestBattleEquipment(CharacterObject troop)
        {
            if (troop == null)
            {
                return null;
            }

            try
            {
                if (troop.BattleEquipments != null)
                {
                    foreach (Equipment equipment in troop.BattleEquipments)
                    {
                        if (IsUsableEquipment(equipment, false))
                        {
                            return equipment;
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (IsUsableEquipment(troop.FirstBattleEquipment, false))
                {
                    return troop.FirstBattleEquipment;
                }
            }
            catch
            {
            }

            try
            {
                if (IsUsableEquipment(troop.Equipment, false))
                {
                    return troop.Equipment;
                }
            }
            catch
            {
            }

            return null;
        }

        private static Equipment GetBestCivilianEquipment(CharacterObject troop)
        {
            if (troop == null)
            {
                return null;
            }

            try
            {
                if (troop.CivilianEquipments != null)
                {
                    Equipment firstUsable = null;
                    foreach (Equipment equipment in troop.CivilianEquipments)
                    {
                        if (!IsUsableEquipment(equipment, true))
                        {
                            continue;
                        }

                        if (HasBodyGear(equipment))
                        {
                            return equipment;
                        }

                        if (firstUsable == null)
                        {
                            firstUsable = equipment;
                        }
                    }

                    if (firstUsable != null)
                    {
                        return firstUsable;
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (IsUsableEquipment(troop.FirstCivilianEquipment, true))
                {
                    return troop.FirstCivilianEquipment;
                }
            }
            catch
            {
            }

            try
            {
                if (IsUsableEquipment(troop.RandomCivilianEquipment, true))
                {
                    return troop.RandomCivilianEquipment;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool HasAnyCivilianEquipment(CharacterObject troop)
        {
            return GetBestCivilianEquipment(troop) != null;
        }

        private static int RepairEmptyArmorSlots(Equipment target, Equipment donor)
        {
            if (target == null || donor == null)
            {
                return 0;
            }

            int repaired = 0;
            repaired += RepairEmptyArmorSlot(target, donor, EquipmentIndex.Body);
            repaired += RepairEmptyArmorSlot(target, donor, EquipmentIndex.Leg);
            repaired += RepairEmptyArmorSlot(target, donor, EquipmentIndex.Gloves);
            repaired += RepairEmptyArmorSlot(target, donor, EquipmentIndex.Cape);
            repaired += RepairEmptyArmorSlot(target, donor, EquipmentIndex.Head);
            return repaired;
        }

        private static int RepairEmptyArmorSlot(Equipment target, Equipment donor, EquipmentIndex slot)
        {
            try
            {
                EquipmentElement current = target[slot];
                EquipmentElement replacement = donor[slot];
                if (current.Item == null && replacement.Item != null)
                {
                    target[slot] = new EquipmentElement(replacement);
                    return 1;
                }
            }
            catch
            {
            }

            return 0;
        }

        private static bool IsUsableEquipment(Equipment equipment, bool allowCivilian)
        {
            if (equipment == null)
            {
                return false;
            }

            try
            {
                return (allowCivilian || !equipment.IsCivilian) && !equipment.IsEmpty();
            }
            catch
            {
                return false;
            }
        }

        private static bool HasBodyGear(Equipment equipment)
        {
            try
            {
                return equipment != null && equipment[EquipmentIndex.Body].Item != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasUsableBattleWeapon(Equipment equipment)
        {
            if (equipment == null)
            {
                return false;
            }

            try
            {
                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    if (equipment[i].Item != null)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static void EnqueueUpgradeTargets(CharacterObject troop, Queue<CharacterObject> pending)
        {
            if (troop == null || troop.UpgradeTargets == null || pending == null)
            {
                return;
            }

            for (int i = 0; i < troop.UpgradeTargets.Length; i++)
            {
                CharacterObject target = troop.UpgradeTargets[i];
                if (target != null)
                {
                    pending.Enqueue(target);
                }
            }
        }

        private static int PreferenceScore(CharacterObject troop, TroopRolePreference preference)
        {
            if (troop == null)
            {
                return 9;
            }

            switch (preference)
            {
                case TroopRolePreference.Infantry:
                    return troop.IsInfantry ? 0 : troop.IsRanged ? 1 : troop.IsMounted ? 2 : 3;
                case TroopRolePreference.Ranged:
                    return troop.IsRanged && !troop.IsMounted ? 0 : troop.IsRanged ? 1 : troop.IsInfantry ? 2 : 3;
                case TroopRolePreference.Mounted:
                    return troop.IsMounted ? 0 : troop.IsInfantry ? 1 : troop.IsRanged ? 2 : 3;
                default:
                    return troop.IsInfantry ? 0 : troop.IsRanged ? 1 : troop.IsMounted ? 2 : 3;
            }
        }
    }
}
