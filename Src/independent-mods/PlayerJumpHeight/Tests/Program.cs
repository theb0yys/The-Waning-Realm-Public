using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PlayerJumpHeight.Tests
{
    internal static class Program
    {
        private static int checks;
        private static int Main(string[] args)
        {
            string root = args[0];
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
            try { Run(); Console.WriteLine("PASS: " + checks + " offline checks; native game not started."); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
            for (int bits = 0; bits < 8; bits++)
                Check(JumpPolicy.Eligible((bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0) == (bits == 6), "eligibility " + bits);
            Near(JumpPolicy.StrengthFactor(true, 1f), 1f, "native height");
            Near(JumpPolicy.StrengthFactor(true, 4f), 2f, "4x ballistic height requires 2x launch strength");
            float defaultFactor = JumpPolicy.StrengthFactor(true, 5f);
            Near(defaultFactor * defaultFactor, 5f, "5x ballistic conversion");
            Near(JumpPolicy.StrengthFactor(false, 5f), 1f, "disabled");
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -100f, 0f })
                Near(JumpPolicy.StrengthFactor(true, value), 1f, "invalid or below range " + value);
            Near(JumpPolicy.StrengthFactor(true, 100f), (float)Math.Sqrt(10), "upper clamp");

            var data = new AgentSpawnData { JumpAcceleration = 4f, JumpSpeedLimit = 7f, Weight = 80, HitPoints = 100, StandingEyeHeight = 1.7f };
            var original = data;
            var outside = JumpSpawnContext.Enter(defaultFactor);
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f * defaultFactor, "player spawn adjusted");
            foreach (FieldInfo field in typeof(AgentSpawnData).GetFields())
                if (field.Name != nameof(AgentSpawnData.JumpAcceleration))
                    Check(Equals(field.GetValue(original), field.GetValue(data)), "untouched spawn field " + field.Name);
            for (int i = 0; i < 50; i++) JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f * defaultFactor, "one application only");
            JumpSpawnContext.Restore(outside);
            data = original;
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f, "outside scope unchanged");

            outside = JumpSpawnContext.Enter(defaultFactor);
            var outer = JumpSpawnContext.Enter(1f);
            data = original;
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f, "nested NPC is not boosted");
            JumpSpawnContext.Restore(outer);
            data = original;
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f * defaultFactor, "nested scope restores pending player boost");
            JumpSpawnContext.Restore(outside);

            outside = JumpSpawnContext.Enter(defaultFactor);
            data = original;
            JumpSpawnContext.Apply((AgentFlag)0, ref data);
            Near(data.JumpAcceleration, 4f, "mount or nonhuman rejected");
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f, "rejected creation consumes scope");
            JumpSpawnContext.Restore(outside);

            foreach (float acceleration in new[] { 0f, -3f, float.MaxValue, float.PositiveInfinity, float.NaN })
            {
                outside = JumpSpawnContext.Enter(defaultFactor);
                data = new AgentSpawnData { JumpAcceleration = acceleration };
                JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
                Check(data.JumpAcceleration.Equals(acceleration), "invalid native input preserved " + acceleration);
                JumpSpawnContext.Restore(outside);
            }

            outside = JumpSpawnContext.Enter(defaultFactor);
            Exception threadError = null;
            var worker = new Thread(() =>
            {
                try
                {
                    var other = original;
                    JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref other);
                    if (other.JumpAcceleration != 4f) throw new Exception("scope leaked across threads");
                }
                catch (Exception error) { threadError = error; }
            });
            worker.Start(); worker.Join();
            Check(threadError == null, "thread scope isolated");
            var nested = JumpSpawnContext.Enter(1f);
            var expectedError = new InvalidOperationException("test spawn failure");
            var result = AccessTools.Method(typeof(PlayerSpawnPatch), "Finalizer").Invoke(null, new object[] { expectedError, nested });
            Check(ReferenceEquals(result, expectedError), "finalizer preserves exception");
            data = original;
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            Near(data.JumpAcceleration, 4f * defaultFactor, "exception cleanup restores outer context");
            JumpSpawnContext.Restore(outside);

            var settings = new JumpSettings();
            Check(settings.Enabled, "enabled default");
            Near(settings.HeightMultiplier, 5f, "MCM default 5x");
            Check(settings.Id == "PlayerJumpHeight" && settings.FolderName == settings.Id && settings.FormatType == "json2", "isolated settings schema");
            Check(typeof(JumpSettings).GetProperty("HeightMultiplier").GetCustomAttributes(false).Length == 2, "MCM slider/group attributes present");

            // Bind patches to the real installed methods without invoking the native engine.
            var harmony = new Harmony("PlayerJumpHeight.offline-tests");
            try
            {
                harmony.PatchAll(typeof(SubModule).Assembly);
                foreach (string methodName in new[] { "CreateAgent", "CreateAgentInternal" })
                    Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(Mission), methodName)).Owners.Contains(harmony.Id), "installed signature binds " + methodName);
            }
            finally { harmony.UnpatchAll(harmony.Id); }

            // Temporary budget: <= 1 ms per 1,000 managed spawn adjustments.
            // The trace has constant work per spawn and no tick/agent scans.
            // This excludes the engine, Harmony dispatch and MCM provider.
            const int iterations = 1000000;
            for (int i = 0; i < 1000; i++) ExerciseSpawn(original, defaultFactor);
            var timer = Stopwatch.StartNew();
            float last = 0f;
            for (int i = 0; i < iterations; i++) last = ExerciseSpawn(original, defaultFactor);
            timer.Stop();
            Near(last, 4f * defaultFactor, "measured adjustment result");
            Check(timer.Elapsed.TotalMilliseconds < 1000, "managed spawn budget <= 1000 ms / million");
            Console.WriteLine("PERF: " + iterations + " managed spawn adjustments in " + timer.Elapsed.TotalMilliseconds.ToString("F2")
                + " ms; limit 1000 ms; Release x64; CLR " + Environment.Version + "; OS " + Environment.OSVersion
                + "; logical CPUs " + Environment.ProcessorCount + "; " + Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"));
        }

        private static float ExerciseSpawn(AgentSpawnData data, float factor)
        {
            var previous = JumpSpawnContext.Enter(factor);
            JumpSpawnContext.Apply(AgentFlag.IsHumanoid, ref data);
            JumpSpawnContext.Restore(previous);
            return data.JumpAcceleration;
        }

        private static void Near(float actual, float expected, string name)
            => Check(!float.IsNaN(actual) && Math.Abs(actual - expected) < 0.0001f, name + ": " + actual + " expected " + expected);

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks++;
        }
    }
}
