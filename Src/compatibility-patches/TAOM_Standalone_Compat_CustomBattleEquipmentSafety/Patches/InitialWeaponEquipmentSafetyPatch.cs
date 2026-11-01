using HarmonyLib;
using System;
using System.Reflection;
using TAOM.Compat.Shared;
using TaleWorlds.Core;

namespace TAOM_Standalone_Compat_CustomBattleEquipmentSafety
{
    [HarmonyPatch]
    internal static class InitialWeaponEquipmentSafetyPatch
    {
        private static readonly EquipmentIndex[] InitialWeaponSlots =
        {
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
            EquipmentIndex.ExtraWeaponSlot
        };

        [HarmonyTargetMethod]
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Equipment),
                "GetInitialWeaponIndicesToEquip",
                new[]
                {
                    typeof(EquipmentIndex).MakeByRefType(),
                    typeof(EquipmentIndex).MakeByRefType(),
                    typeof(bool).MakeByRefType(),
                    typeof(Equipment.InitialWeaponEquipPreference)
                });
        }

        internal static bool Prefix(
            Equipment __instance,
            ref EquipmentIndex mainHandWeaponIndex,
            ref EquipmentIndex offHandWeaponIndex,
            ref bool isMainHandNotUsableWithOneHand,
            Equipment.InitialWeaponEquipPreference initialWeaponEquipPreference)
        {
            try
            {
                GetInitialWeaponIndicesToEquipSafely(
                    __instance,
                    ref mainHandWeaponIndex,
                    ref offHandWeaponIndex,
                    ref isMainHandNotUsableWithOneHand,
                    initialWeaponEquipPreference);
            }
            catch (Exception ex)
            {
                mainHandWeaponIndex = EquipmentIndex.None;
                offHandWeaponIndex = EquipmentIndex.None;
                isMainHandNotUsableWithOneHand = false;
                CompatLog.Info("Safe GetInitialWeaponIndicesToEquip failed and original was skipped: " + ex);
            }

            return false;
        }

        internal static Exception Finalizer(
            Exception __exception,
            ref EquipmentIndex mainHandWeaponIndex,
            ref EquipmentIndex offHandWeaponIndex,
            ref bool isMainHandNotUsableWithOneHand)
        {
            if (__exception == null)
                return null;

            mainHandWeaponIndex = EquipmentIndex.None;
            offHandWeaponIndex = EquipmentIndex.None;
            isMainHandNotUsableWithOneHand = false;
            CompatLog.Info("Suppressed GetInitialWeaponIndicesToEquip exception: " + __exception);
            return null;
        }

        private static void GetInitialWeaponIndicesToEquipSafely(
            Equipment equipment,
            ref EquipmentIndex mainHandWeaponIndex,
            ref EquipmentIndex offHandWeaponIndex,
            ref bool isMainHandNotUsableWithOneHand,
            Equipment.InitialWeaponEquipPreference initialWeaponEquipPreference)
        {
            mainHandWeaponIndex = EquipmentIndex.None;
            offHandWeaponIndex = EquipmentIndex.None;
            isMainHandNotUsableWithOneHand = false;

            if (equipment == null)
            {
                CompatLog.Info("Skipped initial weapon selection: equipment is null.");
                return;
            }

            var hasOneHanded = false;
            var hasTwoHanded = false;

            foreach (var slot in InitialWeaponSlots)
            {
                EquipmentElement element;
                try
                {
                    element = equipment[slot];
                }
                catch (Exception ex)
                {
                    CompatLog.Info("Skipped initial weapon slot " + slot + ": equipment indexer failed: " + ex.GetType().Name);
                    continue;
                }

                if (element.IsEmpty)
                    continue;

                var item = element.Item;
                if (item == null)
                {
                    CompatLog.Info("Skipped initial weapon slot " + slot + ": item is null.");
                    continue;
                }

                var heldInOffHand = HasItemFlag(item, ItemFlags.HeldInOffHand);
                var shouldSetMain =
                    mainHandWeaponIndex == EquipmentIndex.None ||
                    (initialWeaponEquipPreference == Equipment.InitialWeaponEquipPreference.MeleeForMainHand && !hasOneHanded) ||
                    (initialWeaponEquipPreference == Equipment.InitialWeaponEquipPreference.RangedForMainHand && !hasTwoHanded);

                if (shouldSetMain && !heldInOffHand)
                {
                    var primaryWeapon = item.PrimaryWeapon;
                    if (primaryWeapon == null)
                    {
                        CompatLog.Info("Skipped initial main-hand slot " + slot + ": item has no primary weapon: " + DescribeItem(item) + ".");
                        continue;
                    }

                    mainHandWeaponIndex = slot;
                    isMainHandNotUsableWithOneHand = HasWeaponFlag(primaryWeapon, WeaponFlags.NotUsableWithOneHand);
                    hasOneHanded = HasWeaponFlag(primaryWeapon, WeaponFlags.MeleeWeapon);
                    hasTwoHanded = HasWeaponFlag(primaryWeapon, WeaponFlags.RangedWeapon);
                }
                else if (offHandWeaponIndex == EquipmentIndex.None && heldInOffHand)
                {
                    offHandWeaponIndex = slot;
                }
            }
        }

        private static bool HasItemFlag(ItemObject item, ItemFlags flag)
        {
            try { return (item.ItemFlags & flag) != 0; }
            catch { return false; }
        }

        private static bool HasWeaponFlag(WeaponComponentData weapon, WeaponFlags flag)
        {
            try { return (weapon.WeaponFlags & flag) != 0; }
            catch { return false; }
        }

        private static string DescribeItem(ItemObject item)
        {
            try
            {
                if (item == null)
                    return "<null item>";
                if (!string.IsNullOrEmpty(item.StringId))
                    return item.StringId;
                return item.Name != null ? item.Name.ToString() : "<unnamed item>";
            }
            catch
            {
                return "<item description failed>";
            }
        }
    }
}
