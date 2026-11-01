using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;
using TAOM.Adapters;
using TAOM.Features.BannerColorPersistence;
using TAOM.Features.BannerColorPersistence.Hooks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaomRetinuesCompat;

internal static class Bootstrap
{
    private static string _gameRoot;
    private static int Main(string[] args)
    {
        _gameRoot = args.Length == 0 ? @"A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" : args[0];
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { return (int)Assembly.GetExecutingAssembly().GetType("Program").GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs e)
    {
        string name = new AssemblyName(e.Name).Name + ".dll";
        string preferred = name == "TAOM.dll" ? "Modules/TAOM/bin/Win64_Shipping_Client" :
            name == "Retinues.dll" ? "Modules/Retinues/bin/Win64_Shipping_Client" :
            name == "0Harmony.dll" ? "Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client" : "bin/Win64_Shipping_Client";
        string preferredPath = Path.Combine(_gameRoot, preferred, name);
        if (File.Exists(preferredPath)) return Assembly.LoadFrom(preferredPath);
        string[] dirs = { "Modules/TAOM/bin/Win64_Shipping_Client",
            "Modules/Retinues/bin/Win64_Shipping_Client",
            "Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client",
            "bin/Win64_Shipping_Client",
            "Modules/TAOM.Dependencies/bin/Win64_Shipping_Client",
            "Modules/Bannerlord.UIExtenderEx/bin/Win64_Shipping_Client" };
        foreach (string dir in dirs)
        {
            string path = Path.Combine(_gameRoot, dir, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

}

internal static class Program
{
    private static bool _eligible = true, _classifierThrows;
    private static ClanColorInfo? _colors = new ClanColorInfo("player", 0xFF225599, 0xFFEECC44);
    private static uint _seen1, _seen2;
    private static int _passed;
    private static readonly ConfigProvider Config = new ConfigProvider();

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int Run()
    {
        Console.WriteLine("Harmony: " + typeof(Harmony).Assembly.Location);
        Console.WriteLine("Game: " + typeof(AgentBuildData).Assembly.Location);
        MethodInfo prefix = typeof(Mission_SpawnAgent_Patch).GetMethod("Prefix");
        var fixtures = new Harmony("retinues.regression.fixtures");
        var bridge = new Harmony(SubModule.HarmonyId);
        try
        {
            SubModule.RequireHash(typeof(Mission_SpawnAgent_Patch).Assembly, SubModule.TaomHash);
            SubModule.RequireHash(typeof(Retinues.SubModule).Assembly, SubModule.RetinuesHash);
            Check(true, "supported dependency hashes");
            Throws<InvalidOperationException>(() => SubModule.RequireHash(typeof(Retinues.SubModule).Assembly,
                "B3BFFDBD0358BF2545972E8E8F6586848DCF20588778A65F14AAB0B22C40264A"), "legacy hash rejects current Retinues");
            Check(!RetinuesEligibility.IsManagedCombatTroop(null), "null classification safe");
            Mission_SpawnAgent_Patch.Initialize(new BannerColorService(Config), null, null);
            var colorMethod = typeof(Mission_SpawnAgent_Patch).GetMethod("ResolveColors", BindingFlags.Static | BindingFlags.NonPublic);
            var fixtureMethod = typeof(Program).GetMethod(nameof(ResolveTestClan), BindingFlags.Static | BindingFlags.NonPublic);
            Console.WriteLine("Colour method: " + colorMethod + "; fixture: " + fixtureMethod);
            var fixturePatch = new HarmonyMethod(fixtureMethod);
            Console.WriteLine("Fixture patch constructed.");
            fixtures.Patch(colorMethod, prefix: fixturePatch);
            Console.WriteLine("Colour fixture installed.");
            // Register a caller BEFORE installing the bridge, as TAOM does during startup.
            fixtures.Patch(typeof(Program).GetMethod(nameof(SpawnProbe), BindingFlags.Static | BindingFlags.NonPublic), prefix: new HarmonyMethod(prefix));
            Equipment equipment = (Equipment)FormatterServices.GetUninitializedObject(typeof(Equipment));
            AgentBuildData data = (AgentBuildData)FormatterServices.GetUninitializedObject(typeof(AgentBuildData));
            typeof(AgentBuildData).GetProperty("AgentData").SetValue(data, FormatterServices.GetUninitializedObject(typeof(AgentData)), null);
            data.Equipment(equipment);
            Reset(data); SpawnProbe(data);
            Check(_seen1 == 0xFF808080, "baseline reproduces overridden-equipment colour skip");

            RetinuesColorBridge.Install(bridge, prefix, Classify);
            Reset(data); SpawnProbe(data);
            Check(_seen1 == _colors.Value.Color1 && _seen2 == _colors.Value.Color2,
                "registered spawn caller receives both clan colours");
            Check(ReferenceEquals(equipment, data.AgentOverridenSpawnEquipment), "custom equipment object preserved");
            _colors = new ClanColorInfo("player-after-kingdom-change", 0xFF117733, 0xFFCC1133);
            Reset(data); SpawnProbe(data);
            Check(_seen1 == _colors.Value.Color1 && _seen2 == _colors.Value.Color2, "later spawn uses fresh clan colours");
            _eligible = false;
            Reset(data); SpawnProbe(data);
            Check(_seen1 == 0xFF808080, "unmanaged troop keeps original equipment guard");
            _eligible = true; _classifierThrows = true;
            Reset(data); SpawnProbe(data);
            Check(_seen1 == 0xFF808080 && ReferenceEquals(equipment, data.AgentOverridenSpawnEquipment), "classifier failure preserves equipment and behaviour");
            _classifierThrows = false;
            Config.Value.EnableColorPersistence = false;
            Reset(data); SpawnProbe(data);
            Check(_seen1 == 0xFF808080, "disabled colour persistence respected");
            Config.Value.EnableColorPersistence = true;
            _colors = null;
            Reset(data); SpawnProbe(data);
            Check(_seen1 == 0xFF808080, "missing clan colours remain unchanged");
            _colors = new ClanColorInfo("player", 0xFF123456, 0xFF654321);
            data.Equipment(null);
            Reset(data); SpawnProbe(data);
            Check(_seen1 == _colors.Value.Color1 && data.AgentOverridenSpawnEquipment == null, "normal equipment route unchanged");
            data.Equipment(equipment);
            Throws<InvalidOperationException>(() => RetinuesColorBridge.Install(bridge, prefix, Classify), "duplicate install rejected");
            Throws<InvalidOperationException>(() => RetinuesColorBridge.Transpile(new[] { new CodeInstruction(OpCodes.Ret) }), "missing guard rejected");
            var getter = typeof(AgentBuildData).GetProperty("AgentOverridenSpawnEquipment").GetGetMethod();
            Throws<InvalidOperationException>(() => RetinuesColorBridge.Transpile(new[] {
                new CodeInstruction(OpCodes.Call, getter), new CodeInstruction(OpCodes.Callvirt, getter) }), "ambiguous guards rejected");
            RetinuesColorBridge.Uninstall(bridge);
            Reset(data); SpawnProbe(data);
            Check(_seen1 == 0xFF808080, "unload restores original guard");
            Console.WriteLine("PASS: " + _passed + " managed regression checks. Native rendering NOT_RUN.");
            return 0;
        }
        finally
        {
            RetinuesColorBridge.Uninstall(bridge);
            fixtures.UnpatchAll(fixtures.Id);
        }
    }

    private static bool Classify(AgentBuildData data)
    {
        if (_classifierThrows) throw new InvalidOperationException("fixture");
        return _eligible;
    }
    private static bool ResolveTestClan(ref ClanColorInfo? __result) { __result = _colors; return false; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SpawnProbe(AgentBuildData agentBuildData)
    {
        _seen1 = agentBuildData.AgentClothingColor1;
        _seen2 = agentBuildData.AgentClothingColor2;
    }
    private static void Reset(AgentBuildData data) { data.ClothingColor1(0xFF808080).ClothingColor2(0xFF808080); }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        _passed++; Console.WriteLine("PASS: " + name);
    }
    private static void Throws<T>(Action action, string name) where T : Exception
    {
        try { action(); } catch (T) { Check(true, name); return; }
        throw new Exception("FAIL: expected " + typeof(T).Name + ": " + name);
    }
    private sealed class ConfigProvider : IBannerColorConfigProvider
    {
        internal readonly BannerColorConfig Value = new BannerColorConfig();
        public BannerColorConfig GetConfig() => Value;
    }
}
