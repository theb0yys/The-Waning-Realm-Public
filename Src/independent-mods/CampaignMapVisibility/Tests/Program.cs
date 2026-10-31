using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

namespace CampaignMapVisibility.Tests
{
    internal static class Program
    {
        private static int _checks;

        private static int Main(string[] args)
        {
            if (args.Length != 2) { Console.Error.WriteLine("Expected game root and MCM DLL path."); return 2; }
            string[] directories = {
                Path.GetDirectoryName(args[1]),
                Path.Combine(args[0], "bin", "Win64_Shipping_Client"),
                Path.Combine(args[0], "Modules", "SandBox", "bin", "Win64_Shipping_Client")
            };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, request) => {
                string file = new AssemblyName(request.Name).Name + ".dll";
                foreach (string directory in directories)
                {
                    string path = Path.Combine(directory, file);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            try { Run(); Console.WriteLine("PASS " + _checks + " offline checks; native campaign not run."); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }

        private static void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            _checks++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
            // Policy truth table covers all scouting/reveal and lifecycle combinations.
            for (int mask = 0; mask < 32; mask++)
            {
                bool active = (mask & 1) != 0, stationary = (mask & 2) != 0;
                bool inside = (mask & 4) != 0, spotted = (mask & 8) != 0, reveal = (mask & 16) != 0;
                bool visible = VisibilityPolicy.ShowParty(active, stationary, inside, spotted, reveal);
                if (!active || stationary || inside) Check(!visible, "Lifecycle suppression " + mask);
                else if (spotted) Check(visible, "Scouted party " + mask);
                else Check(visible == reveal, "Unscouted party " + mask);
            }
            Check(VisibilityPolicy.IsOnScreen(5000, 960, 540, 1920, 1080), "Distant projected party survives zoom");
            Check(!VisibilityPolicy.IsOnScreen(-1, 960, 540, 1920, 1080), "Behind camera");
            Check(!VisibilityPolicy.IsOnScreen(0, 960, 540, 1920, 1080), "Projection plane");
            Check(!VisibilityPolicy.IsOnScreen(float.NaN, 960, 540, 1920, 1080), "Invalid depth");
            Check(!VisibilityPolicy.IsOnScreen(float.PositiveInfinity, 960, 540, 1920, 1080), "Infinite depth");
            Check(!VisibilityPolicy.IsOnScreen(5000, float.NaN, 540, 1920, 1080), "Invalid position");
            Check(!VisibilityPolicy.IsOnScreen(5000, -101, 540, 1920, 1080), "Outside left margin");
            Check(!VisibilityPolicy.IsOnScreen(5000, 2021, 540, 1920, 1080), "Outside right margin");
            Check(!VisibilityPolicy.IsOnScreen(5000, 960, -31, 1920, 1080), "Outside top margin");
            Check(!VisibilityPolicy.IsOnScreen(5000, 960, 1111, 1920, 1080), "Outside bottom margin");
            Check(!VisibilityPolicy.IsOnScreen(5000, 960, 540, 0, 1080), "Uninitialized viewport");
            for (int mask = 0; mask < 16; mask++)
            {
                bool onScreen = (mask & 1) != 0, spotted = (mask & 2) != 0;
                bool reveal = (mask & 4) != 0, ended = (mask & 8) != 0;
                bool visible = VisibilityPolicy.ShowBattle(onScreen, spotted, reveal, ended);
                if (!onScreen || ended) Check(!visible, "Battle lifecycle " + mask);
                else Check(visible == (spotted || reveal), "Battle scouting " + mask);
            }

            var settings = new MapSettings();
            Check(settings.KeepMarkersVisible, "Zoom option defaults on");
            Check(!settings.RevealEntireMap, "Reveal defaults off");
            Check(settings.Id == "CampaignMapVisibility" && settings.FormatType == "json2", "Stable settings identity");

            var owner = typeof(SubModule).Assembly;
            using (var input = File.OpenRead(typeof(PartyNameplateVM).Assembly.Location))
            using (var sha = SHA256.Create())
                Check(BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "") == SubModule.SupportedViewModelHash,
                    "Exact supported native view-model identity");
            PartyNameplateSession.ValidateBindings();
            Check(true, "Native dictionary/create/remove delegate bindings");

            var harmony = new Harmony(SubModule.HarmonyId);
            try
            {
                harmony.PatchAll(owner);
                var methods = harmony.GetPatchedMethods().ToArray();
                Check(methods.Length == 10, "All ten native UI targets patched");
                Check(methods.All(m => m.DeclaringType.Assembly == typeof(PartyNameplatesVM).Assembly ||
                    m.DeclaringType == typeof(SettlementNameplateWidget)),
                    "No campaign gameplay methods patched");
                Check(methods.Count(m => m.DeclaringType == typeof(SettlementNameplateWidget) &&
                    m.Name == "UpdateNameplateTransparencyAndBrightness") == 1,
                    "Eye suppression runs after the native alpha refresh");
                Check(methods.All(m => Harmony.GetPatchInfo(m).Owners.Contains(SubModule.HarmonyId)), "Patch ownership");
            }
            finally { harmony.UnpatchAll(SubModule.HarmonyId); }
            Check(!harmony.GetPatchedMethods().Any(), "Owner patches fully removed");

            // Guard the added constant-time policy; this is not a native frame-time benchmark.
            const int iterations = 1000000;
            var depths = new float[1024];
            for (int i = 0; i < depths.Length; i++) depths[i] = (i & 1) == 0 ? 5000f + i : -1f;
            int shown = 0;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                if (VisibilityPolicy.IsOnScreen(depths[i & 1023], i % 1920, i % 1080, 1920, 1080)) shown++;
            watch.Stop();
            Check(shown == iterations / 2 && watch.ElapsedMilliseconds < 1000, "One million varied policy calls below 1000 ms");
            Console.WriteLine("Policy guard: " + iterations + " calls in " + watch.ElapsedMilliseconds + " ms; " +
                Environment.Version + "; " + Environment.OSVersion + "; 64-bit=" + Environment.Is64BitProcess);
        }
    }
}
