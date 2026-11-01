using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    public static class EquipmentCategoryCompatibility
    {
        private static readonly ItemObject.ItemTypeEnum[] Weapons =
        {
            ItemObject.ItemTypeEnum.Arrows, ItemObject.ItemTypeEnum.Bolts,
            ItemObject.ItemTypeEnum.Bow, ItemObject.ItemTypeEnum.Bullets,
            ItemObject.ItemTypeEnum.Crossbow, ItemObject.ItemTypeEnum.Musket,
            ItemObject.ItemTypeEnum.OneHandedWeapon, ItemObject.ItemTypeEnum.Pistol,
            ItemObject.ItemTypeEnum.Polearm, ItemObject.ItemTypeEnum.Shield,
            ItemObject.ItemTypeEnum.Thrown, ItemObject.ItemTypeEnum.TwoHandedWeapon
        };

        public static List<ItemObject.ItemTypeEnum> RequiredItemTypes(EquipmentIndex slot)
        {
            if (slot == EquipmentIndex.Weapon0 || slot == EquipmentIndex.Weapon1 ||
                slot == EquipmentIndex.Weapon2 || slot == EquipmentIndex.Weapon3)
                return new List<ItemObject.ItemTypeEnum>(Weapons);
            var result = new List<ItemObject.ItemTypeEnum>(1);
            switch (slot)
            {
                case EquipmentIndex.Head: result.Add(ItemObject.ItemTypeEnum.HeadArmor); break;
                case EquipmentIndex.Body: result.Add(ItemObject.ItemTypeEnum.BodyArmor); break;
                case EquipmentIndex.Leg: result.Add(ItemObject.ItemTypeEnum.LegArmor); break;
                case EquipmentIndex.Gloves: result.Add(ItemObject.ItemTypeEnum.HandArmor); break;
                case EquipmentIndex.Cape: result.Add(ItemObject.ItemTypeEnum.Cape); break;
                case EquipmentIndex.Horse: result.Add(ItemObject.ItemTypeEnum.Horse); break;
                case EquipmentIndex.HorseHarness: result.Add(ItemObject.ItemTypeEnum.HorseHarness); break;
            }
            return result;
        }

        public static ItemObject.ItemTypeEnum WeaponType(string name)
        {
            switch (name)
            {
                case "Arrows": return ItemObject.ItemTypeEnum.Arrows;
                case "Bolts": return ItemObject.ItemTypeEnum.Bolts;
                case "Bow": return ItemObject.ItemTypeEnum.Bow;
                case "Bullets": return ItemObject.ItemTypeEnum.Bullets;
                case "Crossbow": return ItemObject.ItemTypeEnum.Crossbow;
                case "Musket": return ItemObject.ItemTypeEnum.Musket;
                case "One Handed Weapon": return ItemObject.ItemTypeEnum.OneHandedWeapon;
                case "Pistol": return ItemObject.ItemTypeEnum.Pistol;
                case "Polearm": return ItemObject.ItemTypeEnum.Polearm;
                case "Shield": return ItemObject.ItemTypeEnum.Shield;
                case "Thrown": return ItemObject.ItemTypeEnum.Thrown;
                case "Two Handed Weapon": return ItemObject.ItemTypeEnum.TwoHandedWeapon;
                default: return ItemObject.ItemTypeEnum.Invalid;
            }
        }

        public static bool IsWeaponType(ItemObject.ItemTypeEnum type)
        {
            switch (type)
            {
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Bullets:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Musket:
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Shield:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon: return true;
                default: return false;
            }
        }

        // Startup-only, after BinaryRuntime verifies the exact supported source hash/MVID.
        internal static int Rewrite(object module)
        {
            object owner = CecilMetadata.Call(module, "GetType", "MyLittleWarband.CustomUnitsBehavior");
            if (owner == null) throw new MissingMemberException("MLW equipment category owner missing.");
            var names = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "getRequiredItemType", nameof(RequiredItemTypes) },
                { "stringToWeaponType", nameof(WeaponType) },
                { "isWeaponType", nameof(IsWeaponType) }
            };
            var replacements = new Dictionary<object, object>();
            foreach (object method in CecilMetadata.List(owner, "Methods"))
            {
                string replacementName;
                if (!names.TryGetValue((string)CecilMetadata.Get(method, "Name"), out replacementName)) continue;
                MethodInfo bridge = typeof(EquipmentCategoryCompatibility).GetMethod(replacementName);
                object imported = CecilMetadata.Call(module, "ImportReference", bridge);
                IList parameters = CecilMetadata.List(method, "Parameters");
                IList expected = CecilMetadata.List(imported, "Parameters");
                if (!(bool)CecilMetadata.Get(method, "IsStatic") || !(bool)CecilMetadata.Get(method, "HasBody") ||
                    parameters.Count != 1 || expected.Count != 1 ||
                    (string)CecilMetadata.Get(CecilMetadata.Get(parameters[0], "ParameterType"), "FullName") !=
                        (string)CecilMetadata.Get(CecilMetadata.Get(expected[0], "ParameterType"), "FullName") ||
                    (string)CecilMetadata.Get(CecilMetadata.Get(method, "ReturnType"), "FullName") !=
                        (string)CecilMetadata.Get(CecilMetadata.Get(imported, "ReturnType"), "FullName"))
                    throw new NotSupportedException("MLW equipment category signature mismatch: " + replacementName);
                replacements.Add(method, imported);
            }
            if (replacements.Count != 3) throw new NotSupportedException("Expected exactly three MLW equipment category helpers.");
            Type opcodes = CecilMetadata.Type("Cil.OpCodes");
            foreach (var replacement in replacements)
            {
                object body = CecilMetadata.Get(replacement.Key, "Body");
                IList instructions = CecilMetadata.List(body, "Instructions");
                instructions.Clear();
                CecilMetadata.List(body, "Variables").Clear();
                CecilMetadata.List(body, "ExceptionHandlers").Clear();
                CecilMetadata.Set(body, "InitLocals", false);
                CecilMetadata.Set(body, "MaxStackSize", 1);
                object il = CecilMetadata.Call(body, "GetILProcessor");
                instructions.Add(CecilMetadata.Call(il, "Create", opcodes.GetField("Ldarg_0").GetValue(null)));
                instructions.Add(CecilMetadata.Call(il, "Create", opcodes.GetField("Call").GetValue(null), replacement.Value));
                instructions.Add(CecilMetadata.Call(il, "Create", opcodes.GetField("Ret").GetValue(null)));
            }
            return replacements.Count;
        }
    }
}
