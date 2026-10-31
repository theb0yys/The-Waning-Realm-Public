using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace NoFallDamage.Tests
{
    internal static class Program
    {
        private static int checks;
        private delegate void FallCallback(ref AttackCollisionData data, Blow blow, Agent attacker, Agent victim);

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
            try { Run(); Console.WriteLine("PASS: " + checks + " offline checks; no game started."); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
            var gameType = AccessTools.Field(typeof(MBCommon), "_currentGameType");
            object previousType = gameType.GetValue(null);
            var harmony = new Harmony("NoFallDamage.offline-tests");
            var target = AccessTools.Method(typeof(Mission), "FallDamageCallback");
            try
            {
                harmony.PatchAll(typeof(SubModule).Assembly);
                Check(Harmony.GetPatchInfo(target).Owners.Contains(harmony.Id), "installed callback binding");
                int targets = 0;
                foreach (MethodBase method in Harmony.GetAllPatchedMethods())
                    if (Harmony.GetPatchInfo(method).Owners.Contains(harmony.Id))
                    {
                        Check(method == target, "only dedicated fall callback patched");
                        targets++;
                    }
                Check(targets == 1, "exactly one patch target");

                // Never initialize the native engine: prefix must exit before victim access.
                var mission = (Mission)FormatterServices.GetUninitializedObject(typeof(Mission));
                var callback = (FallCallback)Delegate.CreateDelegate(typeof(FallCallback), mission, target);
                gameType.SetValue(null, MBCommon.GameType.Single);
                foreach (int amount in new[] { 0, 1, 25, 100, 500, int.MaxValue })
                {
                    var data = Damage(amount);
                    var before = data;
                    callback(ref data, default(Blow), null, null);
                    Check(data.InflictedDamage == 0 && data.SelfInflictedDamage == 0 && data.BaseMagnitude == 0f,
                        "actual patched callback clears damage " + amount);
                    foreach (var field in typeof(AttackCollisionData).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (field.Name == "InflictedDamage" || field.Name == "SelfInflictedDamage" || field.Name == "BaseMagnitude") continue;
                        Check(Equals(field.GetValue(before), field.GetValue(data)), "preserved collision field " + field.Name);
                    }
                }

                foreach (MBCommon.GameType mode in Enum.GetValues(typeof(MBCommon.GameType)))
                {
                    // The public setter calls native; offline tests change only the managed backing field.
                    gameType.SetValue(null, mode);
                    var data = Damage(100);
                    bool runOriginal = FallDamagePatch.Prefix(ref data);
                    bool excluded = mode == MBCommon.GameType.MultiClient || mode == MBCommon.GameType.MultiServer
                        || mode == MBCommon.GameType.MultiClientServer || mode == MBCommon.GameType.SingleReplay;
                    Check(runOriginal == excluded, "mode routing " + mode);
                    Check(data.InflictedDamage == (excluded ? 100 : 0), "mode damage " + mode);
                    if (excluded)
                    {
                        bool reachedOriginal = false;
                        data = Damage(100);
                        try { callback(ref data, default(Blow), null, null); }
                        catch (NullReferenceException) { reachedOriginal = true; }
                        Check(reachedOriginal && data.InflictedDamage == 100, "original callback retained " + mode);
                    }
                }

                gameType.SetValue(null, MBCommon.GameType.Single);
                var sample = Damage(100);
                for (int i = 0; i < 1000; i++) callback(ref sample, default(Blow), null, null);
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < 1000000; i++)
                {
                    sample.InflictedDamage = 100;
                    callback(ref sample, default(Blow), null, null);
                }
                timer.Stop();
                Check(sample.InflictedDamage == 0, "measured callback suppresses damage");
                Check(timer.Elapsed.TotalMilliseconds < 1000, "million patched callbacks under 1000 ms");
                Console.WriteLine("PERF: 1000000 patched callbacks in " + timer.Elapsed.TotalMilliseconds.ToString("F2")
                    + " ms; limit 1000 ms; Release x64; CLR " + Environment.Version + "; OS " + Environment.OSVersion
                    + "; CPUs " + Environment.ProcessorCount + "; " + Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"));
            }
            finally
            {
                harmony.UnpatchAll(harmony.Id);
                gameType.SetValue(null, previousType);
            }
            var remaining = Harmony.GetPatchInfo(target);
            Check(remaining == null || !remaining.Owners.Contains(harmony.Id), "owner unpatch cleanup");
        }

        private static AttackCollisionData Damage(int amount) => new AttackCollisionData
        {
            InflictedDamage = amount, SelfInflictedDamage = amount, BaseMagnitude = amount,
            MovementSpeedDamageModifier = 1.25f, AbsorbedByArmor = 3, IsShieldBroken = true, IsSneakAttack = true
        };

        private static void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            checks++;
        }
    }
}
