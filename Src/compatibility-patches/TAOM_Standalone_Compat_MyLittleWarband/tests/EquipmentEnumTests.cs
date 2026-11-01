using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

internal static class EquipmentEnumTests
{
    private static int assertions;
    public static int Run(Assembly original, Assembly runtime)
    {
        assertions = 0;
        MethodInfo oldRanged = Ranged(original), ranged = Ranged(runtime);
        Check((bool)oldRanged.Invoke(null, new object[] { Troop(ItemObject.ItemTypeEnum.Shield, 0) }), "original shield defect reproduced");
        Check(!(bool)oldRanged.Invoke(null, new object[] { Troop(ItemObject.ItemTypeEnum.Crossbow, 0) }), "original crossbow defect reproduced");
        var classify = (Func<CharacterObject, bool>)Delegate.CreateDelegate(typeof(Func<CharacterObject, bool>), ranged);
        foreach (ItemObject.ItemTypeEnum type in Enum.GetValues(typeof(ItemObject.ItemTypeEnum)))
        for (int slot = 0; slot < 4; slot++)
            Check(classify(Troop(type, slot)) == (type == ItemObject.ItemTypeEnum.Bow || type == ItemObject.ItemTypeEnum.Crossbow),
                "ranged decision for " + type + " in slot " + slot);
        CharacterObject troop = Troop(ItemObject.ItemTypeEnum.Shield, 0);
        troop.Equipment[1] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.OneHandedWeapon));
        troop.Equipment[2] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.Thrown));
        Check(!classify(troop), "sword/shield/javelin remains infantry policy");
        troop.Equipment[2] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.Bow));
        Check(classify(troop), "mixed shield and bow is ranged");
        troop.Equipment[2] = default(EquipmentElement);
        Check(!classify(troop), "removing bow clears ranged predicate");
        troop.Equipment[10] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.Horse));
        Check(!classify(troop), "mounted shield is not ranged");
        troop.Equipment[3] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.Crossbow));
        Check(classify(troop), "mounted crossbow is ranged");
        var empty = Troop(ItemObject.ItemTypeEnum.Invalid, 0);
        empty.Equipment[0] = default(EquipmentElement);
        Check(!classify(empty), "empty equipment");
        CheckFormationAssignments(runtime);
        // Preserve the original default-roster policy; do not silently switch to
        // scanning all equipment variants as part of an enum correction.
        var roster = (MBEquipmentRoster)AccessTools.Field(typeof(BasicCharacterObject), "_equipmentRoster").GetValue(empty);
        var variants = (MBList<Equipment>)AccessTools.Field(typeof(MBEquipmentRoster), "_equipments").GetValue(roster);
        var second = new Equipment(); second[0] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.Bow));
        variants.Add(second);
        Check(!classify(empty), "nondefault ranged variant does not change MLW policy");

        Type card = runtime.GetType("MyLittleWarband.EquipmentCardVM", true);
        MethodInfo map = AccessTools.DeclaredMethod(card, "GetItemTypeWithItemObject");
        object instance = FormatterServices.GetUninitializedObject(card);
        Type oldCard = original.GetType(card.FullName, true);
        object oldInstance = FormatterServices.GetUninitializedObject(oldCard);
        Check((EquipmentIndex)AccessTools.DeclaredMethod(oldCard, map.Name).Invoke(oldInstance,
            new object[] { Item(ItemObject.ItemTypeEnum.HeadArmor) }) == EquipmentIndex.Leg, "original armor-card defect reproduced");
        var slots = new Dictionary<ItemObject.ItemTypeEnum, EquipmentIndex>
        {
            { ItemObject.ItemTypeEnum.Horse, EquipmentIndex.Horse },
            { ItemObject.ItemTypeEnum.Arrows, EquipmentIndex.Weapon0 },
            { ItemObject.ItemTypeEnum.Bolts, EquipmentIndex.Weapon0 },
            { ItemObject.ItemTypeEnum.Shield, EquipmentIndex.Weapon0 },
            { ItemObject.ItemTypeEnum.HeadArmor, EquipmentIndex.Head },
            { ItemObject.ItemTypeEnum.BodyArmor, EquipmentIndex.Body },
            { ItemObject.ItemTypeEnum.LegArmor, EquipmentIndex.Leg },
            { ItemObject.ItemTypeEnum.HandArmor, EquipmentIndex.Gloves },
            { ItemObject.ItemTypeEnum.Cape, EquipmentIndex.Cape },
            { ItemObject.ItemTypeEnum.HorseHarness, EquipmentIndex.HorseHarness },
            { ItemObject.ItemTypeEnum.Banner, EquipmentIndex.ExtraWeaponSlot }
        };
        foreach (ItemObject.ItemTypeEnum type in Enum.GetValues(typeof(ItemObject.ItemTypeEnum)))
        {
            EquipmentIndex expected;
            if (!slots.TryGetValue(type, out expected)) expected = EquipmentIndex.None;
            Check((EquipmentIndex)map.Invoke(instance, new object[] { Item(type) }) == expected, "card slot " + type);
        }
        Check((EquipmentIndex)map.Invoke(instance, new object[] { null }) == EquipmentIndex.None, "null card item");
        ItemObject weapon = Item(ItemObject.ItemTypeEnum.Bow);
        AccessTools.Property(typeof(ItemObject), "ItemComponent").GetSetMethod(true).Invoke(weapon,
            new[] { FormatterServices.GetUninitializedObject(typeof(WeaponComponent)) });
        Check((EquipmentIndex)map.Invoke(instance, new object[] { weapon }) == EquipmentIndex.Weapon0, "weapon-component fallback retained");

        ConstructorInfo ctor = card.GetConstructors().Single(c => c.GetParameters().Length == 3);
        // Execute the actual adapted constructor's enum decision instructions in
        // a small IL probe. Renderer/UI/string/native calls are deliberately absent.
        var instructions = PatchProcessor.GetOriginalInstructions(ctor, (ILGenerator)null);
        var expectedConstants = new[] { 2, 3, 4, (int)ItemObject.ItemTypeEnum.Thrown, (int)ItemObject.ItemTypeEnum.Shield,
            (int)ItemObject.ItemTypeEnum.Bow, (int)ItemObject.ItemTypeEnum.Crossbow, (int)ItemObject.ItemTypeEnum.Crossbow, 5, 6 };
        var actual = new List<CodeInstruction>();
        for (int i = 0; i + 1 < instructions.Count; i++)
            if (instructions[i].opcode == OpCodes.Ldloc_S && instructions[i].operand is LocalVariableInfo local && local.LocalIndex == 14)
                actual.Add(instructions[i + 1]);
        Check(actual.Count == expectedConstants.Length, "all ten weapon tooltip comparisons accounted for");
        for (int i = 0; i < actual.Count; i++)
        {
            var probe = new DynamicMethod("TooltipEnumOperand", typeof(bool), new[] { typeof(int) });
            ILGenerator il = probe.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (actual[i].operand == null) il.Emit(actual[i].opcode);
            else if (actual[i].operand is sbyte small) il.Emit(actual[i].opcode, small);
            else il.Emit(actual[i].opcode, (int)actual[i].operand);
            il.Emit(OpCodes.Ceq); il.Emit(OpCodes.Ret);
            var check = (Func<int, bool>)probe.CreateDelegate(typeof(Func<int, bool>));
            Check(check(expectedConstants[i]) && !check(-1), "actual tooltip comparison " + i);
        }
        Check(instructions.All(i => i.opcode.OperandType != OperandType.ShortInlineBrTarget), "expanded tooltip operands cannot overflow short branches");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++) classify(troop);
        timer.Stop();
        Check(timer.ElapsedMilliseconds < 2000, "100k four-slot classifications under 2s guard");
        Console.WriteLine("100000 actual adapted classifications: " + timer.ElapsedMilliseconds + " ms; guard 2000 ms.");
        Console.WriteLine("PASS: " + assertions + " equipment enum regression assertions (no rendering/campaign).");
        return assertions;
    }

    private static CharacterObject formationTroop;
    private static CharacterObject ResolveTroop() => formationTroop;
    private static IEnumerable<CodeInstruction> FormationLookup(IEnumerable<CodeInstruction> source)
    {
        var code = source.ToList();
        // Stub only Game.Current.ObjectManager.GetObject<CharacterObject>(id).
        // Execute the original formation branches, isRanged calls and setters.
        if (code.Count < 6 || !(code[4].operand is MethodInfo getObject) || getObject.Name != "GetObject" ||
            getObject.ReturnType != typeof(CharacterObject)) throw new Exception("Unexpected formation lookup shape");
        code.RemoveRange(1, 4);
        code.Insert(1, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EquipmentEnumTests), nameof(ResolveTroop))));
        return code;
    }
    private static void CheckFormationAssignments(Assembly runtime)
    {
        var owner = new Harmony("taom.compat.mylittlewarband.tests.formation");
        MethodInfo assign = AccessTools.DeclaredMethod(runtime.GetType("MyLittleWarband.CustomUnitsBehavior", true), "SetDefaultGroup");
        try
        {
            owner.Patch(assign, transpiler: new HarmonyMethod(typeof(EquipmentEnumTests), nameof(FormationLookup)));
            foreach (bool mounted in new[] { false, true })
            foreach (bool ranged in new[] { false, true })
            {
                formationTroop = Troop(ItemObject.ItemTypeEnum.Shield, 0);
                formationTroop.Equipment[1] = new EquipmentElement(Item(ranged ? ItemObject.ItemTypeEnum.Crossbow : ItemObject.ItemTypeEnum.OneHandedWeapon));
                if (mounted) formationTroop.Equipment[10] = new EquipmentElement(Item(ItemObject.ItemTypeEnum.Horse));
                assign.Invoke(null, new object[] { "_basic_root" });
                FormationClass expected = mounted ? (ranged ? FormationClass.HorseArcher : FormationClass.Cavalry) :
                    (ranged ? FormationClass.Ranged : FormationClass.Infantry);
                Check(formationTroop.DefaultFormationClass == expected && formationTroop.DefaultFormationGroup == (int)expected,
                    "actual MLW formation assignment " + expected);
            }
            formationTroop.Equipment[1] = default(EquipmentElement);
            assign.Invoke(null, new object[] { "_basic_root" });
            Check(formationTroop.DefaultFormationClass == FormationClass.Cavalry &&
                formationTroop.DefaultFormationGroup == (int)FormationClass.Cavalry, "actual assignment updates after crossbow removal");
        }
        finally { owner.UnpatchAll(owner.Id); formationTroop = null; }
    }
    private static MethodInfo Ranged(Assembly a) => AccessTools.DeclaredMethod(a.GetType("MyLittleWarband.CustomUnitsBehavior", true), "isRanged");
    private static ItemObject Item(ItemObject.ItemTypeEnum type)
    {
        var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
        AccessTools.Field(typeof(ItemObject), "Type").SetValue(item, type);
        return item;
    }
    private static CharacterObject Troop(ItemObject.ItemTypeEnum type, int slot)
    {
        var equipment = new Equipment(); equipment[slot] = new EquipmentElement(Item(type));
        var roster = (MBEquipmentRoster)FormatterServices.GetUninitializedObject(typeof(MBEquipmentRoster));
        AccessTools.Field(typeof(MBEquipmentRoster), "_equipments").SetValue(roster, new MBList<Equipment> { equipment });
        var troop = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
        AccessTools.Field(typeof(BasicCharacterObject), "_equipmentRoster").SetValue(troop, roster);
        return troop;
    }
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("FAIL equipment enum: " + name);
        assertions++;
    }
}
