using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TAOM.XorberaxCompatibility;

internal static class Bootstrap
{
    internal static string Root;
    private static int Main(string[] args)
    {
        Root = args.Length > 0 ? args[0] : @"A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string name = new AssemblyName(e.Name).Name;
            string primary = name == "TAOM" ? "Modules/TAOM/bin/Win64_Shipping_Client" :
                name == "SandBox" ? "Modules/SandBox/bin/Win64_Shipping_Client" :
                name == "XorberaxLegacy" ? "Modules/XorberaxLegacy/bin/Win64_Shipping_Client" :
                name == "0Harmony" ? "Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client" : "bin/Win64_Shipping_Client";
            foreach (string dir in new[] { primary, "Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client",
                "Modules/TAOM.Dependencies/bin/Win64_Shipping_Client", "Modules/TAOM/bin/Win64_Shipping_Client" }) {
                string path = Path.Combine(Root, dir, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { return (int)Assembly.GetExecutingAssembly().GetType("Regression").GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

internal static class Regression
{
    private static int passed;
    private static bool qualifies = true, fail;
    private static readonly Model model = new Model();

    internal static int Run()
    {
        Assembly taom = Load("Modules/TAOM/bin/Win64_Shipping_Client/TAOM.dll");
        Assembly game = Load("bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.dll");
        Assembly sandbox = Load("Modules/SandBox/bin/Win64_Shipping_Client/SandBox.dll");
        Assembly xorberax = Load("Modules/XorberaxLegacy/bin/Win64_Shipping_Client/XorberaxLegacy.dll");
        SubModule.RequireBuild(taom.ManifestModule, SubModule.SupportedTaomBuild, "TAOM");
        Check(true, "current TAOM accepted");
        SubModule.RequireBuild(game.ManifestModule, "4267abec-e4ba-4746-94b7-47cbe3ba79f8", "Bannerlord");
        SubModule.RequireBuild(sandbox.ManifestModule, "7c957d2a-5cbc-4e29-b5b4-8f4fc6f82313", "Sandbox");
        SubModule.RequireBuild(xorberax.ManifestModule, "1226f98a-2a9d-44c7-b1c4-c00f46405cb9", "Xorberax");
        Check(true, "other three build guards retained and matched");
        Throws(() => SubModule.RequireBuild(taom.ManifestModule, "bd1af67e-d002-47d1-bf58-83e8967fd143", "TAOM"), "old TAOM guard reproduces failure");
        Throws(() => SubModule.RequireBuild(null, SubModule.SupportedTaomBuild, "TAOM"), "missing dependency rejected");
        Throws(() => SubModule.RequireBuild(game.ManifestModule, SubModule.SupportedTaomBuild, "TAOM"), "unverified module rejected");
        var hit = game.GetType("TaleWorlds.MountAndBlade.Mission").GetMethod("MeleeHitCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var momentum = taom.GetType("TAOM.Features.CombatMechanics.Models.TaomCombatMechanicsModel").GetMethod("CalculateRemainingMomentum");
        var service = taom.GetType("TAOM.Features.CombatMechanics.CreatureCombatService").GetMethod("CalculateCleaveMomentum");
        Check(hit.GetParameters().Length == 11 && momentum.GetParameters().Length == 7 && service.GetParameters().Length == 3 && service.ReturnType == typeof(float?), "installed cleave signatures match");
        Check(xorberax.GetType("XorberaxLegacy.Patches.CutThroughEveryonePatchMeleeHit").GetMethod("MeleeHitCallbackPostfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null, "Xorberax postfix exists");
        Check(typeof(Harmony).Assembly.GetName().Version == new Version(2,4,2,0), "Harmony guard matches");

        var legacy = new Harmony("XorberaxLegacy");
        var compat = new Harmony("TAOM.XorberaxCompatibility.Cleave");
        var hitFixture = typeof(Regression).GetMethod(nameof(Hit), BindingFlags.Static | BindingFlags.NonPublic);
        try
        {
            legacy.Patch(hitFixture, postfix: new HarmonyMethod(typeof(Regression).GetMethod(nameof(LegacyPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
            PatchInstaller.ActiveModel = () => model;
            CleaveContext.PrepareThread();
            PatchInstaller.Install(compat, hitFixture, typeof(Model).GetMethod(nameof(Model.Calculate)),
                typeof(Regression).GetMethod(nameof(Service), BindingFlags.Static | BindingFlags.NonPublic));
            float result = -1;
            Hit(new object(), ref result);
            Check(result == 37f, "qualified TAOM momentum restored after Xorberax postfix");
            qualifies = false; Hit(new object(), ref result);
            Check(result == 0f, "ordinary hit retains Xorberax behaviour");
            qualifies = true; PatchInstaller.ActiveModel = () => null;
            Hit(new object(), ref result);
            Check(result == 0f, "inactive model is not overridden");
            PatchInstaller.ActiveModel = () => model;
            fail = true;
            Throws(() => Hit(new object(), ref result), "original exception preserved");
            fail = false; Hit(new object(), ref result);
            Check(result == 37f, "exception cleanup permits next hit");
            compat.UnpatchAll(compat.Id);
            Hit(new object(), ref result);
            Check(result == 0f, "unloading restores unbridged behaviour");
        }
        finally { compat.UnpatchAll(compat.Id); legacy.UnpatchAll(legacy.Id); PatchInstaller.ActiveModel = null; }
        Console.WriteLine("PASS: " + passed + " checks. Native combat NOT_RUN.");
        return 0;
    }
    private static Assembly Load(string path) => Assembly.LoadFrom(Path.Combine(Bootstrap.Root, path));
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Hit(object attacker, ref float inOutMomentumRemaining) { inOutMomentumRemaining = model.Calculate(attacker); }
    private static void LegacyPostfix(ref float inOutMomentumRemaining) { inOutMomentumRemaining = 0; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float? Service() { return qualifies ? (float?)37 : null; }
    private sealed class Model
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public float Calculate(object attacker) { if (fail) throw new InvalidOperationException("fixture"); return Service() ?? 12; }
    }
    private static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
    private static void Throws(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true, name); return; } throw new Exception("FAIL: " + name); }
}
