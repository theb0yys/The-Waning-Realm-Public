using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    public static class PortraitCompatibility
    {
        private static WeakReference<PartyVM> activeParty;
        private static readonly Func<PartyCharacterVM, CharacterObject, PartyScreenLogic.TroopType,
            PartyScreenLogic.PartyRosterSide, CharacterCode> GetCode =
            (Func<PartyCharacterVM, CharacterObject, PartyScreenLogic.TroopType,
                PartyScreenLogic.PartyRosterSide, CharacterCode>)Delegate.CreateDelegate(
                    typeof(Func<PartyCharacterVM, CharacterObject, PartyScreenLogic.TroopType,
                        PartyScreenLogic.PartyRosterSide, CharacterCode>),
                    AccessTools.DeclaredMethod(typeof(PartyCharacterVM), "GetCharacterCode"));

        public static void Install(Harmony harmony, Assembly runtime)
        {
            harmony.Patch(AccessTools.DeclaredMethod(typeof(WidgetPrefab), "LoadFrom"),
                transpiler: new HarmonyMethod(typeof(PortraitCompatibility), nameof(BindEditorRace)));
            foreach (ConstructorInfo constructor in typeof(PartyVM).GetConstructors())
                harmony.Patch(constructor, postfix: new HarmonyMethod(typeof(PortraitCompatibility), nameof(ObserveParty)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(PartyVM), "OnFinalize"),
                postfix: new HarmonyMethod(typeof(PortraitCompatibility), nameof(ReleaseParty)));
            harmony.Patch(AccessTools.DeclaredMethod(runtime.GetType("MyLittleWarband.CustomUnitsBehavior", true), "DeleteVMLayer"),
                postfix: new HarmonyMethod(typeof(PortraitCompatibility), nameof(RefreshPartyPortraits)));
        }

        // The original XML is read normally. Only its in-memory document is changed,
        // before the native prefab parser compiles the bindings.
        public static void LoadPrefabDocument(XmlDocument document, XmlReader reader, string path)
        {
            document.Load(reader);
            string normalized = Path.GetFullPath(path).Replace('\\', '/');
            if (!normalized.EndsWith("/Modules/MyLittleWarband/GUI/Prefabs/CustomUnits.xml", StringComparison.OrdinalIgnoreCase)) return;
            XmlElement preview = document.SelectSingleNode("//CharacterTableauWidget[@DataSource='{UnitCharacter}']") as XmlElement;
            if (preview != null && !preview.HasAttribute("Race")) preview.SetAttribute("Race", "@Race");
        }

        private static IEnumerable<CodeInstruction> BindEditorRace(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo load = typeof(XmlDocument).GetMethod("Load", new[] { typeof(XmlReader) });
            MethodInfo replacement = typeof(PortraitCompatibility).GetMethod(nameof(LoadPrefabDocument));
            int replaced = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(load))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2).MoveLabelsFrom(instruction);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1) throw new NotSupportedException("Unsupported native prefab loader; expected one XML reader load.");
        }

        public static void ObserveParty(PartyVM __instance) => activeParty = new WeakReference<PartyVM>(__instance);

        public static void ReleaseParty(PartyVM __instance)
        {
            if (activeParty != null && activeParty.TryGetTarget(out PartyVM current) && ReferenceEquals(current, __instance))
                activeParty = null;
        }

        public static void RefreshPartyPortraits()
        {
            if (activeParty == null || !activeParty.TryGetTarget(out PartyVM party)) return;
            RefreshRows(party.MainPartyTroops);
            RefreshRows(party.OtherPartyTroops);
            RefreshRows(party.MainPartyPrisoners);
            RefreshRows(party.OtherPartyPrisoners);
        }

        private static void RefreshRows(IEnumerable<PartyCharacterVM> rows)
        {
            if (rows == null) return;
            foreach (PartyCharacterVM row in rows)
            {
                CharacterObject character = row.Character;
                if (character == null || character.IsHero || !CultureCompatibility.IsCustomTree(character.StringId)) continue;
                row.Code = new CharacterImageIdentifierVM(GetCode(row, character, row.Type, row.Side));
            }
        }
    }
}
