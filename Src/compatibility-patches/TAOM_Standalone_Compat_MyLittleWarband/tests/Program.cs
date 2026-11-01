using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections;
using System.Linq;
using System.Xml;
using HarmonyLib;
using TAOM_Standalone_Compat_MyLittleWarband;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;

internal static class Program
{
    private static readonly List<string> SearchPaths = new List<string>();
    private static int assertions;
    private static Type registeredSaveType, registeredContainer;
    private static int registeredSaveId;

    private static int Main(string[] args)
    {
        string game = args[0];
        SearchPaths.Add(Path.Combine(game, "bin", "Win64_Shipping_Client"));
        foreach (string module in Directory.GetDirectories(Path.Combine(game, "Modules")))
            SearchPaths.Add(Path.Combine(module, "bin", "Win64_Shipping_Client"));
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { return args.Length > 1 && args[1] == "--audit" ? Audit(game) : Run(game); }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        foreach (string folder in SearchPaths)
        {
            string path = Path.Combine(folder, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Audit(string game)
    {
        string path = Path.Combine(game, "Modules", "MyLittleWarband", "bin", "Win64_Shipping_Client", "MyLittleWarband.dll");
        Assembly assembly = Assembly.Load(File.ReadAllBytes(path));
        Type moduleType = typeof(Harmony).Assembly.GetType("Mono.Cecil.ModuleDefinition", true);
        Type referenceType = typeof(Harmony).Assembly.GetType("Mono.Cecil.MethodReference", true);
        object metadata = moduleType.GetMethod("ReadModule", new[] { typeof(string) }).Invoke(null, new object[] { path });
        using ((IDisposable)metadata)
        {
            Console.WriteLine("MVID " + Get(metadata, "Mvid"));
            foreach (object type in (IEnumerable)moduleType.GetMethod("GetTypes").Invoke(metadata, null))
            foreach (object method in (IEnumerable)Get(type, "Methods"))
            {
                if (!(bool)Get(method, "HasBody")) continue;
                var seen = new HashSet<int>();
                foreach (object instruction in (IEnumerable)Get(Get(method, "Body"), "Instructions"))
                {
                    object reference = Get(instruction, "Operand");
                    if (!referenceType.IsInstanceOfType(reference)) continue;
                    int token = Token(reference);
                    if (!seen.Add(token)) continue;
                    try
                    {
                        MethodBase resolved = assembly.ManifestModule.ResolveMethod(token);
                        if (!resolved.IsPublic && resolved.DeclaringType.Assembly != assembly)
                            Console.WriteLine("ACCESS " + Token(method).ToString("X8") + " " + Get(method, "FullName") + " -> " + token.ToString("X8") + " " + Get(reference, "FullName"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("MISSING " + Token(method).ToString("X8") + " " + Get(method, "FullName") + " -> " + token.ToString("X8") + " " + Get(reference, "FullName") + " [" + ex.GetType().Name + "]");
                    }
                }
            }
        }
        return 0;
    }

    private static object Get(object instance, string property) => instance.GetType().GetProperty(property).GetValue(instance, null);
    private static int Token(object instance)
    {
        object token = Get(instance, "MetadataToken");
        return (int)token.GetType().GetMethod("ToInt32").Invoke(token, null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string game)
    {
        foreach (string id in new[] { "_basic_root", "_elite_root", "_basic_root0101", "_elite_root1" })
            Check(CultureCompatibility.IsCustomTree(id), "custom ID " + id);
        foreach (string id in new[] { null, "", "imperial_recruit", "hero_basic_root", "_basic_root2", "_elite_root_human" })
            Check(!CultureCompatibility.IsCustomTree(id), "foreign ID " + id);

        CultureObject culture = Bare<CultureObject>();
        CultureObject otherCulture = Bare<CultureObject>();
        CharacterObject male = Troop("template_male", culture, false, 7);
        CharacterObject female = Troop("template_female", culture, true, 8);
        CharacterObject target = Troop("_basic_root01", culture, false, 0);
        Set(culture, "BasicTroop", male);
        Set(culture, "Townswoman", female);
        Check(CultureCompatibility.Synchronize(target), "male applied");
        Check(target.Race == 7 && ReferenceEquals(target.BodyPropertyRange, male.BodyPropertyRange), "paired male race/range");
        Check(CultureCompatibility.Synchronize(target) && target.Race == 7, "idempotent restore");

        target.IsFemale = true;
        Check(CultureCompatibility.Synchronize(target), "female applied");
        Check(target.Race == 8 && ReferenceEquals(target.BodyPropertyRange, female.BodyPropertyRange), "paired female race/range");
        Set(culture, "Townswoman", null);
        Set(culture, "FemaleDancer", null);
        MBBodyProperty previous = target.BodyPropertyRange;
        Check(!CultureCompatibility.Synchronize(target) && target.Race == 8 && ReferenceEquals(previous, target.BodyPropertyRange), "no opposite-gender fallback");

        target.IsFemale = false;
        Set(male, "Culture", otherCulture);
        Check(!CultureCompatibility.Synchronize(target), "foreign-culture template rejected");
        Set(male, "Culture", culture);
        Set(male, "BodyPropertyRange", null);
        Check(!CultureCompatibility.Synchronize(target), "missing body range rejected");
        Set(culture, "Townsman", Troop("townsman", culture, false, 9));
        Check(CultureCompatibility.Synchronize(target) && target.Race == 9, "townsman fallback");
        Check(!CultureCompatibility.Synchronize(Troop("imperial_recruit", culture, false, 3)), "normal troop excluded");
        Check(!CultureCompatibility.Synchronize(Troop("_elite_root", null, false, 3)), "missing culture preserved");
        Check(!CultureCompatibility.Synchronize(null), "null preserved");

        // Inspect the supplied assembly without clearing its download marker or changing live files.
        Console.WriteLine("PASS: " + assertions + " managed culture policy assertions. Real MLW binding check follows.");
        string originalPath = Path.Combine(game, "Modules", "MyLittleWarband", "bin", "Win64_Shipping_Client", "MyLittleWarband.dll");
        Assembly mlw = Assembly.LoadFrom(originalPath);
        var harmony = new Harmony("taom.compat.mylittlewarband.tests");
        try
        {
            CheckCultureBoundaries(harmony);
            CheckOriginalPrefab(harmony, game);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Assembly runtime = BinaryRuntime.Build(mlw, Path.Combine(game, "Modules", "MyLittleWarband", "bin", "Win64_Shipping_Client", "MyLittleWarband.dll"));
            assertions += EquipmentEnumTests.Run(mlw, runtime);
            CheckEquipmentCategories(runtime);
            CheckEditorMovieHandles(runtime);
            Check(runtime.GetName().Name == BinaryRuntime.RuntimeName, "patch-owned runtime identity");
            Check(runtime.GetType("MyLittleWarband.SaveDefiner") == null, "no duplicate save definer");
            Check(runtime.GetType("MyLittleWarband.MyLittleWarbandSetting") == null, "no duplicate MCM settings");
            Check(ReferenceEquals(mlw, Assembly.LoadFrom(originalPath)), "launcher load-from identity stable");
            int prepared = 0;
            foreach (Type type in runtime.GetTypes())
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    Check(field.FieldType != null, "runtime field type resolves");
                var methods = new List<MethodBase>(type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                methods.AddRange(type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
                foreach (MethodBase method in methods)
                {
                    if (method.ContainsGenericParameters || method.IsAbstract || method.GetMethodBody() == null) continue;
                    try { RuntimeHelpers.PrepareMethod(method.MethodHandle); }
                    catch (Exception ex) { throw new InvalidOperationException("Adapted method failed JIT: " + type.FullName + "." + method.Name, ex); }
                    prepared++;
                }
            }
            Check(prepared > 0, "closed methods prepared without executing game code");
            Check(SaveSchema(mlw.GetType("MyLittleWarband.CustomUnit", true)) == SaveSchema(runtime.GetType("MyLittleWarband.CustomUnit", true)), "custom troop save property names, types and IDs unchanged");
            Console.WriteLine("PASS: " + prepared + " adapted method bodies JIT-prepared; save property schema matches.");
            bool rejected = false;
            try { BinaryRuntime.Build(mlw, typeof(Harmony).Assembly.Location); }
            catch (NotSupportedException) { rejected = true; }
            Check(rejected, "unsupported binary rejected without rewrite");
            Console.WriteLine("Retargeted " + BinaryRuntime.RewrittenCalls + " calls in " + BinaryRuntime.RewrittenMethods + " methods.");
            BinaryRuntime.BindSaveTypes(harmony, mlw, runtime);
            Type definerType = mlw.GetType("MyLittleWarband.SaveDefiner", true);
            harmony.Patch(AccessTools.DeclaredMethod(definerType.BaseType, "AddClassDefinition"), prefix: new HarmonyMethod(typeof(Program), nameof(CaptureSaveClass)));
            harmony.Patch(AccessTools.DeclaredMethod(definerType.BaseType, "ConstructContainerDefinition"), prefix: new HarmonyMethod(typeof(Program), nameof(CaptureSaveContainer)));
            object definer = Activator.CreateInstance(definerType);
            AccessTools.DeclaredMethod(definerType, "DefineClassTypes").Invoke(definer, null);
            AccessTools.DeclaredMethod(definerType, "DefineContainerDefinitions").Invoke(definer, null);
            Check(registeredSaveType == runtime.GetType("MyLittleWarband.CustomUnit") && registeredSaveId == 1, "original definer registers adapted troop with original local save ID");
            Check((int)AccessTools.Field(definerType.BaseType, "_saveBaseId").GetValue(definer) == 1436500005, "original save base ID retained");
            Check(registeredContainer == typeof(Dictionary<,>).MakeGenericType(typeof(string), registeredSaveType), "original definer registers matching adapted dictionary");
            Type originalSettings = mlw.GetType("MyLittleWarband.MyLittleWarbandSetting", true);
            object settingsObject = FormatterServices.GetUninitializedObject(originalSettings);
            foreach (string property in new[] { "SpawnAtPlayerSettlement", "SpawnAtPlayerKingdom", "FullUnitEditor", "ClanRecruitCustomTroop" })
            {
                Set(settingsObject, property, true);
                Check((bool)typeof(LegacyApi).GetMethod(property).Invoke(null, new[] { settingsObject }), "shared original settings delegate " + property);
            }
            CultureCompatibility.Install(harmony, runtime);
            RuntimeLifecycle.Install(harmony, mlw, runtime);
            CheckPortraitBoundaries(harmony, runtime, game);
            Type behavior = runtime.GetType("MyLittleWarband.CustomUnitsBehavior", true);
            MethodInfo appearance = AccessTools.DeclaredMethod(behavior, "updateAppearance");
            Check(Harmony.GetPatchInfo(appearance).Owners.Contains(harmony.Id), "real MLW appearance hook bound");
            MethodInfo copy = AccessTools.DeclaredMethod(behavior, "CopyCharacter");
            Check(Harmony.GetPatchInfo(copy).Owners.Contains(harmony.Id), "real MLW copy hook bound");
            MethodInfo restore = AccessTools.DeclaredMethod(runtime.GetType("MyLittleWarband.CustomUnit", true), "Update");
            Check(Harmony.GetPatchInfo(restore).Owners.Contains(harmony.Id), "real MLW restore hook and private field bound");
            target.Race = 0;
            appearance.Invoke(null, new object[] { target });
            Check(target.Race == 9, "patched original appearance method executes");
            Type originalEntry = mlw.GetType("MyLittleWarband.SubModule", true);
            MethodInfo originalLoad = AccessTools.DeclaredMethod(originalEntry, "OnSubModuleLoad");
            object originalInstance = Activator.CreateInstance(originalEntry);
            originalLoad.Invoke(originalInstance, null);
            int hooks = Harmony.GetAllPatchedMethods().Count(method => Harmony.GetPatchInfo(method).Owners.Contains(BinaryRuntime.RuntimeHarmonyId));
            Check(hooks > 0, "adapted MLW native hooks installed");
            originalLoad.Invoke(originalInstance, null);
            Check(hooks == Harmony.GetAllPatchedMethods().Count(method => Harmony.GetPatchInfo(method).Owners.Contains(BinaryRuntime.RuntimeHarmonyId)), "MLW startup forwarding is idempotent");
            Check(watch.ElapsedMilliseconds < 10000, "startup preparation under temporary 10s guard");
            Console.WriteLine("Managed adapter startup and native Harmony bindings: " + watch.ElapsedMilliseconds + " ms.");
            var hot = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++) CultureCompatibility.Synchronize(target);
            hot.Stop();
            Check(hot.ElapsedMilliseconds < 2000, "100k culture updates under temporary 2s guard");
            Console.WriteLine("100000 culture synchronization calls: " + hot.ElapsedMilliseconds + " ms; guard 2000 ms. CLR " + Environment.Version + ", x64=" + Environment.Is64BitProcess + ".");
        }
        finally
        {
            new Harmony(BinaryRuntime.RuntimeHarmonyId).UnpatchAll(BinaryRuntime.RuntimeHarmonyId);
            harmony.UnpatchAll(harmony.Id);
        }
        Console.WriteLine("PASS: " + assertions + " owner-scoped managed assertions. No game or native rendering started.");
        return 0;
    }

    private static void CheckEquipmentCategories(Assembly runtime)
    {
        Type behavior = runtime.GetType("MyLittleWarband.CustomUnitsBehavior", true);
        MethodInfo required = AccessTools.DeclaredMethod(behavior, "getRequiredItemType");
        MethodInfo weaponName = AccessTools.DeclaredMethod(behavior, "stringToWeaponType");
        MethodInfo isWeapon = AccessTools.DeclaredMethod(behavior, "isWeaponType");
        var categories = new Dictionary<EquipmentIndex, ItemObject.ItemTypeEnum>
        {
            { EquipmentIndex.Leg, ItemObject.ItemTypeEnum.LegArmor },
            { EquipmentIndex.Body, ItemObject.ItemTypeEnum.BodyArmor },
            { EquipmentIndex.Head, ItemObject.ItemTypeEnum.HeadArmor },
            { EquipmentIndex.Gloves, ItemObject.ItemTypeEnum.HandArmor },
            { EquipmentIndex.Cape, ItemObject.ItemTypeEnum.Cape },
            { EquipmentIndex.Horse, ItemObject.ItemTypeEnum.Horse },
            { EquipmentIndex.HorseHarness, ItemObject.ItemTypeEnum.HorseHarness }
        };
        foreach (var category in categories)
        {
            var actual = (List<ItemObject.ItemTypeEnum>)required.Invoke(null, new object[] { category.Key });
            Check(actual.Count == 1 && actual[0] == category.Value,
                "actual adapted " + category.Key + " category selects " + category.Value + "; actual=" + string.Join(",", actual));
            actual.Clear();
            Check(((List<ItemObject.ItemTypeEnum>)required.Invoke(null, new object[] { category.Key })).Count == 1,
                "category result mutation cannot alter later selections " + category.Key);
        }
        Check(((List<ItemObject.ItemTypeEnum>)required.Invoke(null, new object[] { EquipmentIndex.None })).Count == 0,
            "unknown equipment slot stays empty");
        var weapons = new Dictionary<string, ItemObject.ItemTypeEnum>
        {
            { "Arrows", ItemObject.ItemTypeEnum.Arrows }, { "Bolts", ItemObject.ItemTypeEnum.Bolts },
            { "Bow", ItemObject.ItemTypeEnum.Bow }, { "Bullets", ItemObject.ItemTypeEnum.Bullets },
            { "Crossbow", ItemObject.ItemTypeEnum.Crossbow }, { "Musket", ItemObject.ItemTypeEnum.Musket },
            { "One Handed Weapon", ItemObject.ItemTypeEnum.OneHandedWeapon }, { "Pistol", ItemObject.ItemTypeEnum.Pistol },
            { "Polearm", ItemObject.ItemTypeEnum.Polearm }, { "Shield", ItemObject.ItemTypeEnum.Shield },
            { "Thrown", ItemObject.ItemTypeEnum.Thrown }, { "Two Handed Weapon", ItemObject.ItemTypeEnum.TwoHandedWeapon }
        };
        foreach (var weapon in weapons)
            Check((ItemObject.ItemTypeEnum)weaponName.Invoke(null, new object[] { weapon.Key }) == weapon.Value,
                "actual adapted weapon label " + weapon.Key);
        foreach (string unknown in new string[] { null, "", "Unknown", "bow" })
            Check((ItemObject.ItemTypeEnum)weaponName.Invoke(null, new object[] { unknown }) == ItemObject.ItemTypeEnum.Invalid,
                "unknown weapon label remains invalid");
        foreach (var slot in new[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3 })
        {
            var actual = (List<ItemObject.ItemTypeEnum>)required.Invoke(null, new object[] { slot });
            Check(actual.Count == weapons.Count && new HashSet<ItemObject.ItemTypeEnum>(actual).SetEquals(weapons.Values),
                "actual adapted weapon slot retains the twelve supported categories " + slot);
        }
        foreach (ItemObject.ItemTypeEnum type in Enum.GetValues(typeof(ItemObject.ItemTypeEnum)))
            Check((bool)isWeapon.Invoke(null, new object[] { type }) == weapons.Values.Contains(type),
                "actual adapted weapon classification " + type);
        var selector = (Func<EquipmentIndex, List<ItemObject.ItemTypeEnum>>)Delegate.CreateDelegate(
            typeof(Func<EquipmentIndex, List<ItemObject.ItemTypeEnum>>), required);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++) selector(EquipmentIndex.Leg);
        watch.Stop();
        Check(watch.ElapsedMilliseconds < 2000, "100k equipment selections under temporary 2s guard");
        Console.WriteLine("100000 actual adapted equipment selections: " + watch.ElapsedMilliseconds + " ms; guard 2000 ms.");
    }
    private static int previewRace;
    private static MBBodyProperty previewRange;

    private static XmlDocument parsedPrefab;
    private static int portraitCalls, portraitRace;
    private static CharacterObject portraitCharacter;

    private static string EditorPrefab(string game) => Path.Combine(game, "Modules", "MyLittleWarband", "GUI", "Prefabs", "CustomUnits.xml");

    private static void CheckOriginalPrefab(Harmony harmony, string game)
    {
        harmony.Patch(AccessTools.DeclaredMethod(typeof(WidgetTemplate), "LoadFrom"),
            prefix: new HarmonyMethod(typeof(Program), nameof(CapturePrefabTemplate)));
        try
        {
            WidgetPrefab.LoadFrom(new PrefabExtensionContext(), null, EditorPrefab(game));
            XmlElement preview = (XmlElement)parsedPrefab.SelectSingleNode("//CharacterTableauWidget[@DataSource='{UnitCharacter}']");
            Check(preview != null && !preview.HasAttribute("Race"), "original MLW prefab reproduces the missing race binding");
        }
        finally { harmony.Unpatch(AccessTools.DeclaredMethod(typeof(WidgetTemplate), "LoadFrom"), HarmonyPatchType.Prefix, harmony.Id); }
    }

    private static bool CapturePrefabTemplate(XmlNode __2, ref WidgetTemplate __result)
    {
        parsedPrefab = __2.OwnerDocument;
        // Stop before native widget creation/rendering, after the real loader's
        // XML read and compatibility binding transformation have executed.
        __result = new WidgetTemplate(__2.Name);
        return false;
    }

    private static void CheckPortraitBoundaries(Harmony harmony, Assembly runtime, string game)
    {
        string path = EditorPrefab(game);
        byte[] originalBytes = File.ReadAllBytes(path);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(WidgetTemplate), "LoadFrom"),
            prefix: new HarmonyMethod(typeof(Program), nameof(CapturePrefabTemplate)));
        try
        {
            WidgetPrefab.LoadFrom(new PrefabExtensionContext(), null, path);
            XmlElement preview = (XmlElement)parsedPrefab.SelectSingleNode("//CharacterTableauWidget[@DataSource='{UnitCharacter}']");
            Check(preview.GetAttribute("Race") == "@Race", "real native prefab loader supplies Race binding before template parsing");
            Check(File.ReadAllBytes(path).SequenceEqual(originalBytes), "original MLW prefab remains byte-identical");
        }
        finally { harmony.Unpatch(AccessTools.DeclaredMethod(typeof(WidgetTemplate), "LoadFrom"), HarmonyPatchType.Prefix, harmony.Id); }

        const string fixture = "<Prefab><Window><Widget><Children><CharacterTableauWidget DataSource='{UnitCharacter}'/><CharacterTableauWidget DataSource='{OtherCharacter}'/></Children></Widget></Window></Prefab>";
        XmlDocument repaired = LoadFixture(fixture, path);
        Check(((XmlElement)repaired.SelectSingleNode("//CharacterTableauWidget[@DataSource='{UnitCharacter}']")).GetAttribute("Race") == "@Race", "editor race binding added");
        Check(!((XmlElement)repaired.SelectSingleNode("//CharacterTableauWidget[@DataSource='{OtherCharacter}']")).HasAttribute("Race"), "unrelated widget preserved");
        Check(LoadFixture(fixture, path.Replace("MyLittleWarband", "OtherMod")).OuterXml == LoadFixture(fixture, path.Replace("CustomUnits.xml", "Other.xml")).OuterXml,
            "foreign module and prefab documents receive no transformation");
        Check(!((XmlElement)LoadFixture(fixture, path.Replace("MyLittleWarband", "OtherMod")).SelectSingleNode("//CharacterTableauWidget")).HasAttribute("Race"), "foreign module remains untouched");
        string fixedXml = repaired.OuterXml;
        Check(LoadFixture(fixedXml, path).OuterXml == fixedXml, "race binding repair is idempotent");
        Check(((XmlElement)LoadFixture(fixture.Replace("DataSource='{UnitCharacter}'", "DataSource='{UnitCharacter}' Race='@CustomRace'"), path)
            .SelectSingleNode("//CharacterTableauWidget")).GetAttribute("Race") == "@CustomRace", "existing explicit binding preserved");

        CultureObject orc = Bare<CultureObject>();
        Set(orc, "BasicTroop", Troop("gundabad_template", orc, false, 7));
        CharacterObject recruit = Troop("_basic_root", null, false, 0);
        PartyCharacterVM row = Bare<PartyCharacterVM>();
        AccessTools.Field(typeof(PartyCharacterVM), "_character").SetValue(row, recruit);
        row.Code = new CharacterImageIdentifierVM(Code("human-before-edit", 0));
        LegacyApi.SetCulture(recruit, orc);
        Check(recruit.Race == 7 && row.Code.Id == "human-before-edit", "native party row reproduces stale human image after in-place culture edit");
        CharacterImageIdentifierVM staleCode = row.Code;
        PartyCharacterVM nativeRow = Bare<PartyCharacterVM>();
        AccessTools.Field(typeof(PartyCharacterVM), "_character").SetValue(nativeRow, Troop("native_recruit", orc, false, 0));
        nativeRow.Code = new CharacterImageIdentifierVM(Code("native-unchanged", 0));
        PartyVM party = Bare<PartyVM>();
        party.MainPartyTroops = new MBBindingList<PartyCharacterVM> { row, nativeRow };
        party.OtherPartyTroops = new MBBindingList<PartyCharacterVM> { row };
        party.MainPartyPrisoners = new MBBindingList<PartyCharacterVM> { row };
        party.OtherPartyPrisoners = new MBBindingList<PartyCharacterVM> { row };
        MethodInfo getCode = AccessTools.DeclaredMethod(typeof(PartyCharacterVM), "GetCharacterCode");
        harmony.Patch(getCode, prefix: new HarmonyMethod(typeof(Program), nameof(CapturePortraitCode)));
        try
        {
            PortraitCompatibility.ObserveParty(party);
            MethodInfo close = AccessTools.DeclaredMethod(runtime.GetType("MyLittleWarband.CustomUnitsBehavior", true), "DeleteVMLayer");
            Check(Harmony.GetPatchInfo(close).Owners.Contains(harmony.Id), "adapted editor close refresh hook installed");
            Check(typeof(PartyVM).GetConstructors().All(ctor => Harmony.GetPatchInfo(ctor).Owners.Contains(harmony.Id)), "native party constructor observation installed");
            Check(Harmony.GetPatchInfo(AccessTools.DeclaredMethod(typeof(PartyVM), "OnFinalize")).Owners.Contains(harmony.Id), "native party lifecycle cleanup installed");
            portraitCalls = 0;
            close.Invoke(null, null);
            Check(portraitCalls == 4, "actual editor close refreshes all four party lists");
            Check(row.Code.Id == "current-race-7" && !ReferenceEquals(row.Code, staleCode), "party row gets a fresh culture-correct image code");
            Check(portraitRace == 7 && ReferenceEquals(portraitCharacter, recruit), "native image factory receives the exact edited troop and race");
            Check(nativeRow.Code.Id == "native-unchanged" && nativeRow.Character.Race == 0, "native troops and portraits remain unchanged");
            Check(recruit.Race == 7, "portrait refresh does not mutate race");
            PortraitCompatibility.ReleaseParty(Bare<PartyVM>());
            portraitCalls = 0;
            PortraitCompatibility.RefreshPartyPortraits();
            Check(portraitCalls == 4, "unrelated party finalization does not discard active observation");
            PortraitCompatibility.ReleaseParty(party);
            portraitCalls = 0;
            PortraitCompatibility.RefreshPartyPortraits();
            Check(portraitCalls == 0, "finalized party is no longer refreshed");

            party.OtherPartyTroops = null;
            party.MainPartyPrisoners = null;
            party.OtherPartyPrisoners = null;
            party.MainPartyTroops = new MBBindingList<PartyCharacterVM>();
            for (int i = 0; i < 1000; i++) party.MainPartyTroops.Add(row);
            PortraitCompatibility.ObserveParty(party);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            PortraitCompatibility.RefreshPartyPortraits();
            watch.Stop();
            Check(portraitCalls == 1000 && watch.ElapsedMilliseconds < 2000, "1000 visible custom rows refresh under temporary 2s guard");
            Console.WriteLine("1000 custom party row refreshes: " + watch.ElapsedMilliseconds + " ms; guard 2000 ms; native image generation stubbed.");
        }
        finally
        {
            PortraitCompatibility.ReleaseParty(party);
            harmony.Unpatch(getCode, HarmonyPatchType.Prefix, harmony.Id);
        }
        Console.WriteLine("PASS: real prefab race binding and editor-close party portrait regressions; native rendering pending.");
    }

    private static XmlDocument LoadFixture(string xml, string path)
    {
        var document = new XmlDocument();
        using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(xml)))
            PortraitCompatibility.LoadPrefabDocument(document, reader, path);
        return document;
    }

    private static CharacterCode Code(string id, int race)
    {
        CharacterCode code = Bare<CharacterCode>();
        Set(code, "Code", id);
        Set(code, "Race", race);
        return code;
    }

    private static bool CapturePortraitCode(CharacterObject __0, ref CharacterCode __result)
    {
        portraitCalls++;
        portraitRace = __0.Race;
        portraitCharacter = __0;
        __result = Code("current-race-" + __0.Race, __0.Race);
        return false;
    }

    private static void CheckCultureBoundaries(Harmony harmony)
    {
        CultureObject human = Bare<CultureObject>();
        CultureObject orc = Bare<CultureObject>();
        CharacterObject template = Troop("gundabad_template", orc, false, 7);
        Set(orc, "BasicTroop", template);
        CharacterObject recruit = Troop("_basic_root", human, false, 0);
        LegacyApi.SetCulture(recruit, orc);
        Check(recruit.Culture == orc, "retargeted culture setter assigns culture");
        Check(recruit.Race == template.Race && ReferenceEquals(recruit.BodyPropertyRange, template.BodyPropertyRange),
            "retargeted culture setter synchronizes human recruit race and body range");

        MethodInfo bridge = typeof(LegacyApi).GetMethod(nameof(LegacyApi.FillCharacter));
        Type viewType = bridge.GetParameters()[0].ParameterType;
        MethodInfo fill = AccessTools.DeclaredMethod(viewType, "FillFrom", new[] { typeof(BasicCharacterObject), typeof(int), typeof(string) });
        harmony.Patch(fill, prefix: new HarmonyMethod(typeof(Program), nameof(CapturePreview)));
        recruit.Race = 0;
        MBBodyProperty stale = Bare<MBBodyProperty>();
        Set(recruit, "BodyPropertyRange", stale);
        bridge.Invoke(null, new[] { FormatterServices.GetUninitializedObject(viewType), recruit, (object)-1 });
        Check(previewRace == template.Race && ReferenceEquals(previewRange, template.BodyPropertyRange),
            "native preview entry receives the selected culture race and body range");
        CharacterObject normal = Troop("native_recruit", human, false, 0);
        LegacyApi.SetCulture(normal, orc);
        Check(normal.Race == 0, "culture bridge leaves native troop race unchanged");
        Set(orc, "BasicTroop", null);
        recruit.Race = 3;
        LegacyApi.SetCulture(recruit, orc);
        Check(recruit.Race == 3, "culture bridge preserves race when no compatible template exists");
        harmony.Unpatch(fill, HarmonyPatchType.Prefix, harmony.Id);
        Console.WriteLine("PASS: culture setter and native preview boundaries synchronize custom troop race/range.");
    }

    private static bool CapturePreview(BasicCharacterObject __0)
    {
        previewRace = __0.Race;
        previewRange = __0.BodyPropertyRange;
        return false;
    }

    private static void CheckEditorMovieHandles(Assembly runtime)
    {
        Type handleType = typeof(LegacyApi).GetMethod(nameof(LegacyApi.Layer)).ReturnType.Assembly
            .GetType("TaleWorlds.Engine.GauntletUI.GauntletMovieIdentifier", true);
        object nativeHandle = FormatterServices.GetUninitializedObject(handleType);
        foreach (string name in new[] { "CustomUnitsBehavior", "EquipmentSelectorBehavior" })
        {
            Type behavior = runtime.GetType("MyLittleWarband." + name, true);
            MethodInfo open = AccessTools.DeclaredMethod(behavior, "CreateVMLayer");
            FieldInfo storage = behavior.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Single(field => field.FieldType.Name.Contains("GauntletMovie"));
            Type[] casts = PatchProcessor.GetOriginalInstructions(open)
                .Where(instruction => instruction.opcode == OpCodes.Castclass && instruction.operand is Type type && type.Name.Contains("GauntletMovie"))
                .Select(instruction => (Type)instruction.operand).ToArray();
            if (name == "CustomUnitsBehavior") Check(casts.Length > 0, "actual troop editor movie cast is covered");
            foreach (Type cast in casts.Concat(new[] { storage.FieldType }))
            {
                var probe = new DynamicMethod("EditorMovieHandle", typeof(object), new[] { typeof(object) }, typeof(Program).Module, true);
                ILGenerator il = probe.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, cast);
                il.Emit(OpCodes.Stsfld, storage);
                il.Emit(OpCodes.Ldsfld, storage);
                il.Emit(OpCodes.Ret);
                try
                {
                    // Execute the real adapted cast and field assignment with a real API
                    // handle type, without calling the native renderer or opening a game.
                    var invoke = (Func<object, object>)probe.CreateDelegate(typeof(Func<object, object>));
                    Check(ReferenceEquals(invoke(nativeHandle), nativeHandle), name + " preserves native movie handle identity");
                }
                finally { storage.SetValue(null, null); }
            }
            Check(storage.FieldType == handleType, name + " stores the current movie handle type");
        }
        Console.WriteLine("PASS: both editor movie casts and storage accept actual GauntletMovieIdentifier handles.");
    }

    private static bool CaptureSaveClass(Type __0, int __1)
    {
        registeredSaveType = __0;
        registeredSaveId = __1;
        return false;
    }
    private static bool CaptureSaveContainer(Type __0) { registeredContainer = __0; return false; }

    private static string SaveSchema(Type type) => string.Join("\n", type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .SelectMany(property => property.GetCustomAttributesData().Where(attribute => attribute.AttributeType.Name == "SaveablePropertyAttribute")
            .Select(attribute => property.Name + ":" + property.PropertyType.FullName + ":" + string.Join(",", attribute.ConstructorArguments.Select(value => value.ToString()))))
        .OrderBy(value => value));

    private static T Bare<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

    private static CharacterObject Troop(string id, CultureObject culture, bool female, int race)
    {
        CharacterObject troop = Bare<CharacterObject>();
        Set(troop, "StringId", id);
        Set(troop, "Culture", culture);
        troop.IsFemale = female;
        troop.Race = race;
        Set(troop, "BodyPropertyRange", Bare<MBBodyProperty>());
        return troop;
    }

    private static void Set(object instance, string name, object value)
    {
        MethodInfo setter = null;
        for (Type type = instance.GetType(); type != null && setter == null; type = type.BaseType)
            setter = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.GetSetMethod(true);
        if (setter == null) throw new InvalidOperationException("Test setup has no setter: " + name);
        setter.Invoke(instance, new[] { value });
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        assertions++;
    }
}
