using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace CustomBattleReforged
{
    internal static class Program
    {
        private static int _checks;
        private static string _game;
        private static int Main(string[] args)
        {
            try
            {
                Check(args.Length == 2, "Provide game root and built owner DLL.");
                _game = Path.GetFullPath(args[0]);
                AppDomain.CurrentDomain.AssemblyResolve += Resolve;
                TestPolicy();
                TestInvalidManifests();
                var timer = Stopwatch.StartNew();
                CatalogDefinition live = CatalogDefinition.Load(Path.Combine(_game, "Modules", "TAOM"));
                timer.Stop();
                Check(live.CultureNames.ContainsKey("vlandia") && live.CultureNames["vlandia"].Contains("Rohirrim"), "Rohan's reused ID is retained and renamed.");
                Check(live.CultureNames.ContainsKey("empire") && live.CultureNames["empire"].Contains("Dunlendings"), "Dunland's reused ID is retained and renamed.");
                Check(live.Characters.ContainsKey("gondor_loss_lumberman"), "Live Gondor soldier found.");
                Check(!live.Characters.ContainsKey("imperial_infantryman"), "Vanilla troop excluded from authored roster.");
                Check(!live.Characters.ContainsKey("commander_1"), "Vanilla commander excluded from authored roster.");
                Check(!live.CultureNames.ContainsKey("nord"), "Unconverted vanilla Nord is excluded.");
                Check(!live.CultureNames.ContainsKey("battania"), "Variag is excluded from Custom Battle factions.");
                int excludedCharacters = 0;
                bool allVariagCharactersRejected = true;
                foreach (var entry in live.Characters)
                {
                    if (entry.Value.Culture != "battania") continue;
                    excludedCharacters++;
                    allVariagCharactersRejected &= !live.Accepts(entry.Key, "battania", false, true);
                }
                Check(excludedCharacters > 0, "Live exclusion regression exercises existing Variag characters.");
                Check(allVariagCharactersRejected, "Variag soldiers and commanders are ineligible even with valid battle equipment.");
                var soldierCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
                foreach (var entry in live.Characters)
                {
                    if (!entry.Value.Soldier || !live.CultureNames.ContainsKey(entry.Value.Culture)) continue;
                    if (!soldierCounts.ContainsKey(entry.Value.Culture)) soldierCounts[entry.Value.Culture] = 0;
                    soldierCounts[entry.Value.Culture]++;
                }
                Check(soldierCounts.ContainsKey("gondor") && soldierCounts.ContainsKey("vlandia"), "Authored Gondor and Rohan troop pools exist.");
                // Some authored factions have one soldier type but several heroes.
                // Runtime availability depends on their resolved, equipped characters.
                foreach (var entry in soldierCounts) Check(entry.Value > 0, "Nonempty authored soldier pool for " + entry.Key);
                Check(timer.ElapsedMilliseconds < 5000, "Metadata load within 5-second startup budget.");
                Console.WriteLine($"LIVE METADATA: {live.CultureNames.Count} cultures, {live.Characters.Count} character definitions, {live.FilesRead} files, {timer.ElapsedMilliseconds} ms.");
                Console.WriteLine("SOLDIER COUNTS: " + string.Join(", ", soldierCounts));
                VerifyHarmony(Path.GetFullPath(args[1]));
                TestFullRosterSelection(Path.GetFullPath(args[1]));
                Console.WriteLine($"PASS {_checks} checks; managed metadata/policy/Harmony only; no native game or rendering executed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void TestPolicy()
        {
            var catalog = new CatalogDefinition();
            catalog.AddDocument(Xml("<SPCultures><Culture id='gondor' name='Gondor' is_main_culture='true'/><Culture id='raiders' name='Raiders' is_main_culture='true' is_bandit='true'/></SPCultures>"));
            catalog.AddCultureTransform(Xml("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'><xsl:template match=\"Culture[@id='vlandia']\"><xsl:copy><xsl:attribute name='name'>Rohirrim</xsl:attribute></xsl:copy></xsl:template></xsl:stylesheet>"));
            catalog.AddDocument(Xml("<NPCCharacters><NPCCharacter id='taom_soldier' culture='Culture.vlandia' occupation='Soldier'/><NPCCharacter id='taom_hero' culture='Culture.gondor' is_hero='true'/><NPCCharacter id='townsperson' culture='Culture.gondor' occupation='Villager'/></NPCCharacters>"));
            Check(catalog.Accepts("taom_soldier", "vlandia", false, true), "Remapped Rohan soldier accepted.");
            Check(catalog.Accepts("taom_hero", "gondor", false, true), "Authored hero accepted.");
            Check(!catalog.Accepts("vlandian_knight", "vlandia", false, true), "Vanilla soldier sharing a remapped culture rejected.");
            Check(!catalog.Accepts("taom_soldier", "gondor", false, true), "Wrong-culture collision rejected.");
            Check(!catalog.Accepts("taom_soldier", "vlandia", true, true), "Obsolete character rejected.");
            Check(!catalog.Accepts("taom_soldier", "vlandia", false, false), "Missing battle equipment rejected.");
            Check(!catalog.Accepts(null, null, false, true), "Missing identity rejected.");
            Check(!catalog.Characters.ContainsKey("townsperson"), "Civilian omitted.");
            Check(!catalog.CultureNames.ContainsKey("raiders"), "Bandit faction omitted.");
            Check(CatalogDefinition.ArmyLimit == 9999, "Four-digit army limit.");
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
                if (!catalog.Accepts("taom_soldier", "vlandia", false, true)) throw new Exception("Unstable cached lookup.");
            watch.Stop();
            Check(watch.ElapsedMilliseconds < 1000, "100k dictionary membership checks within 1 second.");
            Console.WriteLine($"POLICY PERFORMANCE: 100000 lookups, {watch.ElapsedMilliseconds} ms, Release x64 .NET {Environment.Version}.");
        }

        private static void TestInvalidManifests()
        {
            string temp = Path.Combine(Path.GetTempPath(), "CustomBattleReforged-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                File.WriteAllText(Path.Combine(temp, "SubModule.xml"), "<Module><Id value='TAOM'/><Version value='v99.0.0'/></Module>");
                Reject(() => CatalogDefinition.Load(temp), "Unknown TAOM version rejected.");
                File.WriteAllText(Path.Combine(temp, "SubModule.xml"), "<Module><Id value='TAOM'/><Version value='v2.0.29.5'/><Xmls><XmlNode><XmlName id='NPCCharacters' path='../../outside'/></XmlNode></Xmls></Module>");
                Reject(() => CatalogDefinition.Load(temp), "Path traversal rejected before opening external data.");
                File.WriteAllText(Path.Combine(temp, "SubModule.xml"), "<!DOCTYPE Module [<!ENTITY probe SYSTEM 'file:///missing'>]><Module>&probe;</Module>");
                Reject(() => CatalogDefinition.Load(temp), "External XML entity rejected.");
            }
            finally
            {
                // This directory is the exact newly-created test fixture, never a caller path.
                File.Delete(Path.Combine(temp, "SubModule.xml"));
                Directory.Delete(temp);
            }
        }

        private static void VerifyHarmony(string ownerDll)
        {
            Assembly owner = Assembly.LoadFrom(ownerDll);
            Assembly.LoadFrom(Path.Combine(_game, "Modules", "TAOM", "bin", "Win64_Shipping_Client", "TAOM.dll"));
            Assembly harmonyAssembly = Assembly.LoadFrom(Path.Combine(_game, "Modules", "TAOM.Dependencies", "bin", "Win64_Shipping_Client", "0Harmony.dll"));
            Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
            object harmony = Activator.CreateInstance(harmonyType, new object[] { "taom.custombattlereforged.headless" });
            try
            {
                harmonyType.GetMethod("PatchAll", new[] { typeof(Assembly) }).Invoke(harmony, new object[] { owner });
                _checks++;
                owner.GetType("CustomBattleReforged.SubModule", true).GetMethod("InstallTaomServicePatches", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { harmony });
                _checks++;
                Console.WriteLine("HARMONY: all native targets, private fields, setter arguments and TAOM service overloads installed in a headless process.");
            }
            finally
            {
                harmonyType.GetMethod("UnpatchAll", new[] { typeof(string) }).Invoke(harmony, new object[] { "taom.custombattlereforged.headless" });
            }
        }

        private static void TestFullRosterSelection(string ownerDll)
        {
            Assembly owner = Assembly.LoadFrom(ownerDll);
            Assembly native = Assembly.LoadFrom(Path.Combine(_game, "Modules/CustomBattle/bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.CustomBattle.dll"));
            Assembly core = Assembly.LoadFrom(Path.Combine(_game, "bin/Win64_Shipping_Client/TaleWorlds.Core.dll"));
            Assembly library = Assembly.LoadFrom(Path.Combine(_game, "bin/Win64_Shipping_Client/TaleWorlds.Library.dll"));
            Type troopType = native.GetType("TaleWorlds.MountAndBlade.CustomBattle.CustomBattleTroopTypeVM", true);
            Type itemType = native.GetType("TaleWorlds.MountAndBlade.CustomBattle.ArmyCompositionItemVM", true);
            Type groupType = native.GetType("TaleWorlds.MountAndBlade.CustomBattle.ArmyCompositionGroupVM", true);
            Type characterType = core.GetType("TaleWorlds.Core.BasicCharacterObject", true);
            Type listType = library.GetType("TaleWorlds.Library.MBBindingList`1", true).MakeGenericType(troopType);
            MethodInfo selectAll = owner.GetType("CustomBattleReforged.TroopVariety", true).GetMethod("SelectAll", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo selections = native.GetType("TaleWorlds.MountAndBlade.CustomBattle.CustomBattleVM", true).GetMethod("GetTroopSelections", BindingFlags.Static | BindingFlags.NonPublic);
            // Real managed VM types without their renderer/portrait constructors.
            object group = FormatterServices.GetUninitializedObject(groupType);
            string[] fields = { "_meleeInfantryComposition", "_rangedInfantryComposition", "_meleeCavalryComposition", "_rangedCavalryComposition" };
            int[] sizes = { 12, 7, 3, 0 };
            var lists = new IList[4];
            var characters = new object[4][];
            for (int formation = 0; formation < 4; formation++)
            {
                lists[formation] = (IList)Activator.CreateInstance(listType);
                characters[formation] = new object[sizes[formation]];
                for (int i = 0; i < sizes[formation]; i++)
                {
                    object troop = FormatterServices.GetUninitializedObject(troopType);
                    object character = FormatterServices.GetUninitializedObject(characterType);
                    characters[formation][i] = character;
                    troopType.GetField("<Character>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(troop, character);
                    lists[formation].Add(troop);
                }
                object item = FormatterServices.GetUninitializedObject(itemType);
                itemType.GetField("_troopTypes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, lists[formation]);
                groupType.GetField(fields[formation], BindingFlags.Instance | BindingFlags.NonPublic).SetValue(group, item);
                selectAll.Invoke(null, new object[] { lists[formation] });
            }
            var selected = (Array)selections.Invoke(null, new[] { group });
            for (int formation = 0; formation < 4; formation++)
            {
                var selection = (IList)selected.GetValue(formation);
                Check(selection.Count == sizes[formation], "All branch/tier entries forwarded for formation " + formation);
                foreach (object character in characters[formation])
                    Check(selection.Contains(character), "Selected character identity preserved through native battle boundary.");
                foreach (object troop in lists[formation])
                    Check((bool)troopType.GetField("IsDefault").GetValue(troop), "Full roster is marked as default.");
            }
            troopType.GetProperty("IsSelected").SetValue(lists[0][5], false);
            selected = (Array)selections.Invoke(null, new[] { group });
            Check(((IList)selected.GetValue(0)).Count == sizes[0] - 1, "Manual deselection survives battle boundary.");
            Check(!((IList)selected.GetValue(0)).Contains(characters[0][5]), "Manual deselected identity stays excluded.");
            selectAll.Invoke(null, new object[] { lists[0] });
            selected = (Array)selections.Invoke(null, new[] { group });
            Check(((IList)selected.GetValue(0)).Count == sizes[0], "Randomize/faction reset restores full variety.");
            Console.WriteLine("VARIETY: real managed selection boundary retains all 22 candidates across 3 formations, an empty formation, and manual deselections.");
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            foreach (string relative in new[] { "bin/Win64_Shipping_Client", "Modules/CustomBattle/bin/Win64_Shipping_Client", "Modules/TAOM.Dependencies/bin/Win64_Shipping_Client", "Modules/TAOM/bin/Win64_Shipping_Client", "Modules/Native/bin/Win64_Shipping_Client", "Modules/SandBox/bin/Win64_Shipping_Client" })
            {
                string path = Path.Combine(_game, relative, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        }

        private static XmlDocument Xml(string text) { var doc = new XmlDocument { XmlResolver = null }; doc.LoadXml(text); return doc; }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); _checks++; }
        private static void Reject(Action action, string label)
        {
            try { action(); }
            catch (InvalidDataException) { _checks++; return; }
            catch (XmlException) { _checks++; return; }
            throw new Exception(label);
        }
    }
}
