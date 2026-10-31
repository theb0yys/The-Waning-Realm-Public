using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace PlayerSpeedControl.Tests
{
    internal static class Program
    {
        private static int checks;
        private static int Main(string[] args)
        {
            string root = args.Length == 0 ? @"A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" : args[0];
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string name = new AssemblyName(e.Name).Name + ".dll";
                foreach (string directory in new[] { "bin/Win64_Shipping_Client", "Modules/TAOM.Dependencies/bin/Win64_Shipping_Client" })
                {
                    string path = Path.Combine(root, directory, name);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            try { Run(); Console.WriteLine("PASS: " + checks + " owner checks (offline; no native game started)."); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
            // Full boolean truth tables protect every non-player/walking/mounted case.
            for (int bits = 0; bits < 64; bits++)
            {
                bool player = (bits & 1) != 0, active = (bits & 2) != 0, human = (bits & 4) != 0;
                bool mounted = (bits & 8) != 0, walking = (bits & 16) != 0, land = (bits & 32) != 0;
                Check(SpeedPolicy.CanRun(player, active, human, mounted, walking, land) == (bits == 39), "running eligibility " + bits);
            }
            for (int bits = 0; bits < 8; bits++)
                Check(SpeedPolicy.CanTravel((bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0) == (bits == 1), "travel eligibility " + bits);

            Near(SpeedPolicy.Clamp(float.NaN), 1f, "NaN");
            Near(SpeedPolicy.Clamp(float.PositiveInfinity), 1f, "infinite");
            Near(SpeedPolicy.Clamp(float.NegativeInfinity), 1f, "negative infinite");
            Near(SpeedPolicy.Clamp(-1f), 0.5f, "lower bound");
            Near(SpeedPolicy.Clamp(100f), 3f, "upper bound");

            var adjustment = new RunningAdjustment();
            float speed = 0.8f;
            for (int i = 0; i < 100; i++) speed = adjustment.Apply(speed, 3f);
            Near(speed, 2.4f, "repeated application does not compound");
            Near(adjustment.Restore(speed), 0.8f, "walking restores baseline");
            speed = adjustment.Apply(0.9f, 2f);
            Near(adjustment.Apply(speed, 0.5f), 0.45f, "live setting change");
            Near(adjustment.Restore(1.2f), 1.2f, "intervening external value preserved");
            speed = adjustment.Apply(0.6f, 3f);
            Near(adjustment.Apply(speed, 1f), 0.6f, "disable restores");
            Near(adjustment.Restore(0.7f), 0.7f, "restoration idempotent");
            speed = adjustment.Apply(0.7f, 2f);
            adjustment.Restore(speed);
            Near(adjustment.Apply(0.5f, 2f), 1f, "equipment recalculation becomes new baseline");

            foreach (bool describe in new[] { false, true })
            {
                foreach (float multiplier in new[] { 0.5f, 1f, 1.5f, 3f })
                {
                    var normal = new ExplainedNumber(5f, describe, new TextObject("Base speed"));
                    normal.Add(1f, new TextObject("Native bonus"));
                    normal.AddFactor(-0.25f, new TextObject("Night"));
                    normal.LimitMin(1f);
                    var result = SpeedPolicy.ApplyTravel(normal, multiplier);
                    Near(result.ResultNumber, 4.5f * multiplier, "native factors multiplied " + multiplier);
                    Near(normal.ResultNumber, 4.5f, "native value preserved");
                    Check(result.IncludeDescriptions == describe, "description flag");
                    if (describe)
                    {
                        string lines = result.GetExplanations();
                        Check(lines.Contains("Native bonus") && lines.Contains("Night"), "native tooltip retained");
                        Check(lines.Contains("Player Speed Control") == (multiplier != 1f), "named delta only when enabled");
                    }
                }
                var limited = new ExplainedNumber(0.2f, describe);
                limited.LimitMin(1f);
                Near(SpeedPolicy.ApplyTravel(limited, 0.5f).ResultNumber, 0.5f, "multiply native floor");
                var capped = new ExplainedNumber(10f, describe);
                capped.LimitMax(4f);
                Near(SpeedPolicy.ApplyTravel(capped, 2f).ResultNumber, 8f, "multiply native cap");
            }

            var settings = new SpeedSettings();
            Near(settings.RunningMultiplier, 1f, "default running");
            Near(settings.TravelMultiplier, 1f, "default travel");
            Check(settings.RunningEnabled && settings.TravelEnabled, "default toggles");
            Check(settings.Id == "PlayerSpeedControl" && settings.FormatType == "json2", "isolated MCM config");

            // Bind the real installed methods with Harmony without invoking native code.
            var harmony = new Harmony("PlayerSpeedControl.offline-tests");
            try
            {
                harmony.PatchAll(typeof(SubModule).Assembly);
                Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(AgentDrivenProperties), "UpdateDrivenProperties"))
                    .Owners.Contains(harmony.Id), "running patch binds installed game");
                Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(DefaultPartySpeedCalculatingModel), "CalculateFinalSpeed"))
                    .Owners.Contains(harmony.Id), "travel patch binds installed game");
            }
            finally { harmony.UnpatchAll(harmony.Id); }
        }

        private static void Near(float actual, float expected, string name)
        {
            Check(!float.IsNaN(actual) && Math.Abs(actual - expected) < 0.0001f, name + ": " + actual + " expected " + expected);
        }

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks++;
        }
    }
}
