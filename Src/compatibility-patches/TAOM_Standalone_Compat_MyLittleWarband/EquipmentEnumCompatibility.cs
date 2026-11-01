using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    // Only called while adapting the hash/MVID-pinned MLW 1.2.7 input. Offsets
    // identify researched enum operands, never a global integer substitution.
    public static class EquipmentEnumCompatibility
    {
        internal static void Rewrite(object module)
        {
            object behavior = CecilMetadata.Call(module, "GetType", "MyLittleWarband.CustomUnitsBehavior");
            object ranged = Method(behavior, "isRanged", 1);
            ReplaceConstants(ranged, new[] { 0x24, 0x2d }, new[] { 8, 9 }, new[] { "Bow", "Crossbow" });

            object card = CecilMetadata.Call(module, "GetType", "MyLittleWarband.EquipmentCardVM");
            object constructor = Method(card, ".ctor", 3);
            ReplaceConstants(constructor, new[] { 0x568, 0x632, 0x689, 0x68e, 0x741 },
                new[] { 10, 7, 8, 9, 9 }, new[] { "Thrown", "Shield", "Bow", "Crossbow", "Crossbow" });

            object slot = Method(card, "GetItemTypeWithItemObject", 1);
            object bridge = CecilMetadata.Call(module, "ImportReference",
                typeof(EquipmentEnumCompatibility).GetMethod(nameof(ItemSlot)));
            IList parameters = CecilMetadata.List(slot, "Parameters");
            if ((bool)CecilMetadata.Get(slot, "IsStatic") ||
                (string)CecilMetadata.Get(CecilMetadata.Get(parameters[0], "ParameterType"), "FullName") != typeof(ItemObject).FullName ||
                (string)CecilMetadata.Get(CecilMetadata.Get(slot, "ReturnType"), "FullName") != typeof(EquipmentIndex).FullName)
                throw new NotSupportedException("Unexpected MLW equipment-card slot signature.");
            object body = CecilMetadata.Get(slot, "Body");
            IList instructions = CecilMetadata.List(body, "Instructions");
            instructions.Clear();
            CecilMetadata.List(body, "Variables").Clear();
            CecilMetadata.List(body, "ExceptionHandlers").Clear();
            CecilMetadata.Set(body, "InitLocals", false);
            CecilMetadata.Set(body, "MaxStackSize", 1);
            object il = CecilMetadata.Call(body, "GetILProcessor");
            Type opcodes = CecilMetadata.Type("Cil.OpCodes");
            // The original is an instance method; the item is argument 1.
            instructions.Add(CecilMetadata.Call(il, "Create", opcodes.GetField("Ldarg_1").GetValue(null)));
            instructions.Add(CecilMetadata.Call(il, "Create", opcodes.GetField("Call").GetValue(null), bridge));
            instructions.Add(CecilMetadata.Call(il, "Create", opcodes.GetField("Ret").GetValue(null)));
        }

        private static object Method(object owner, string name, int parameterCount)
        {
            if (owner == null) throw new MissingMemberException("MLW equipment enum owner missing.");
            object found = null;
            foreach (object method in CecilMetadata.List(owner, "Methods"))
                if ((string)CecilMetadata.Get(method, "Name") == name &&
                    CecilMetadata.List(method, "Parameters").Count == parameterCount)
                {
                    if (found != null || !(bool)CecilMetadata.Get(method, "HasBody"))
                        throw new NotSupportedException("Ambiguous or missing MLW equipment method body: " + name);
                    found = method;
                }
            return found ?? throw new MissingMethodException("Missing MLW equipment method: " + name);
        }

        private static void ReplaceConstants(object method, int[] offsets, int[] expected, string[] names)
        {
            IList instructions = CecilMetadata.List(CecilMetadata.Get(method, "Body"), "Instructions");
            var targets = new object[offsets.Length];
            var values = new int[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                foreach (object instruction in instructions)
                    if ((int)CecilMetadata.Get(instruction, "Offset") == offsets[i]) targets[i] = instruction;
                if (targets[i] == null) throw new NotSupportedException("Missing MLW equipment enum operand.");
                string opcode = CecilMetadata.Get(CecilMetadata.Get(targets[i], "OpCode"), "Name").ToString();
                bool matches = expected[i] <= 8 ? opcode == "ldc.i4." + expected[i] :
                    opcode == "ldc.i4.s" && Convert.ToInt32(CecilMetadata.Get(targets[i], "Operand")) == expected[i];
                if (!matches) throw new NotSupportedException("Unexpected MLW equipment enum operand at " + offsets[i].ToString("X"));
                values[i] = Convert.ToInt32(Enum.Parse(typeof(ItemObject.ItemTypeEnum), names[i]));
            }
            Type opcodes = CecilMetadata.Type("Cil.OpCodes");
            for (int i = 0; i < targets.Length; i++)
            {
                CecilMetadata.Set(targets[i], "OpCode", opcodes.GetField("Ldc_I4").GetValue(null));
                CecilMetadata.Set(targets[i], "Operand", values[i]);
            }
            // Constants can grow. Widen short branches so no old byte displacement
            // can overflow; retain the same instruction targets and handler objects.
            foreach (object instruction in instructions)
            {
                object opcode = CecilMetadata.Get(instruction, "OpCode");
                if (CecilMetadata.Get(opcode, "OperandType").ToString() != "ShortInlineBrTarget") continue;
                string name = CecilMetadata.Get(opcode, "Name").ToString();
                string longName = name.Substring(0, name.Length - 2).Replace('.', '_');
                FieldInfo field = Array.Find(opcodes.GetFields(), candidate =>
                    candidate.Name.Equals(longName, StringComparison.OrdinalIgnoreCase));
                if (field == null) throw new NotSupportedException("Missing long branch opcode: " + name);
                CecilMetadata.Set(instruction, "OpCode", field.GetValue(null));
            }
        }

        // Preserve MLW's old policy, including its WeaponComponent fallback.
        // This public bridge is called from the separate in-memory assembly.
        public static EquipmentIndex ItemSlot(ItemObject item)
        {
            if (item == null) return EquipmentIndex.None;
            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.Horse: return EquipmentIndex.Horse;
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                case ItemObject.ItemTypeEnum.Shield: return EquipmentIndex.Weapon0;
                case ItemObject.ItemTypeEnum.HeadArmor: return EquipmentIndex.Head;
                case ItemObject.ItemTypeEnum.BodyArmor: return EquipmentIndex.Body;
                case ItemObject.ItemTypeEnum.LegArmor: return EquipmentIndex.Leg;
                case ItemObject.ItemTypeEnum.HandArmor: return EquipmentIndex.Gloves;
                case ItemObject.ItemTypeEnum.Cape: return EquipmentIndex.Cape;
                case ItemObject.ItemTypeEnum.HorseHarness: return EquipmentIndex.HorseHarness;
                case ItemObject.ItemTypeEnum.Banner: return EquipmentIndex.ExtraWeaponSlot;
                default: return item.WeaponComponent != null ? EquipmentIndex.Weapon0 : EquipmentIndex.None;
            }
        }
    }
}
