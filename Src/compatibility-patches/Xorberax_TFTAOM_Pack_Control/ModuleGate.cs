using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace XorberaxTFTAOMPackControl
{
    internal static class ModuleGate
    {
        private static readonly string[] TargetMethods =
        {
            "Banks.SubModule:OnGameStart",
            "DeadlyCombat.SubModule:OnMissionBehaviorInitialize",
            "ScholarsOfCalradia.SubModule:OnGameStart",
            "ShoulderCam.SubModule:OnMissionBehaviorInitialize",
            "TrainingField.SubModule:OnGameStart",
            "Work.SubModule:OnGameStart",
            "Yell.SubModule:OnApplicationTick"
        };

        private static readonly HashSet<string> Patched = new HashSet<string>(StringComparer.Ordinal);
        private static Harmony _harmony;
        private static bool _patching;

        internal static void Install(Harmony harmony)
        {
            _harmony = harmony;
            PatchAvailableTargets();
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        internal static void Uninstall()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            PatchAvailableTargets();
        }

        private static void PatchAvailableTargets()
        {
            if (_harmony == null)
                return;

            if (_patching)
                return;

            _patching = true;
            try
            {
                var prefix = new HarmonyMethod(typeof(ModuleGate).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
                foreach (var targetName in TargetMethods)
                {
                    if (Patched.Contains(targetName))
                        continue;

                    var method = AccessTools.Method(targetName);
                    if (method == null)
                        continue;

                    try
                    {
                        _harmony.Patch(method, prefix);
                        Patched.Add(targetName);
                    }
                    catch (Exception ex)
                    {
                        PackLog.Error("Could not patch gate target " + targetName, ex);
                    }
                }
            }
            finally
            {
                _patching = false;
            }
        }

        private static bool Prefix(MethodBase __originalMethod)
        {
            var declaringType = __originalMethod?.DeclaringType?.FullName;
            if (string.IsNullOrEmpty(declaringType))
                return true;

            var enabled = IsEnabled(declaringType);
            if (!enabled)
                PackLog.Info("Skipped disabled module hook " + declaringType + "." + __originalMethod.Name + ".");
            return enabled;
        }

        internal static bool IsEnabled(string declaringType)
        {
            var settings = PackSettings.Current;
            if (declaringType.StartsWith("Banks.", StringComparison.Ordinal)) return settings.BanksUnsafeEnabled;
            if (declaringType.StartsWith("CutThroughEveryone.", StringComparison.Ordinal)) return settings.CutThroughEveryoneEnabled;
            if (declaringType.StartsWith("DeadlyCombat.", StringComparison.Ordinal)) return settings.DeadlyCombatEnabled;
            if (declaringType.StartsWith("DeadlyHorseCharges.", StringComparison.Ordinal)) return settings.DeadlyHorseChargesEnabled;
            if (declaringType.StartsWith("KillFeedDisabler.", StringComparison.Ordinal)) return settings.KillFeedDisablerEnabled;
            if (declaringType.StartsWith("ScholarsOfCalradia.", StringComparison.Ordinal)) return settings.ScholarsEnabled;
            if (declaringType.StartsWith("ShoulderCam.", StringComparison.Ordinal)) return settings.ShoulderCamEnabled;
            if (declaringType.StartsWith("TextToSpeech.", StringComparison.Ordinal)) return settings.TextToSpeechEnabled;
            if (declaringType.StartsWith("TrainingField.", StringComparison.Ordinal)) return settings.TrainingFieldEnabled;
            if (declaringType.StartsWith("Work.", StringComparison.Ordinal)) return settings.WorkEnabled;
            if (declaringType.StartsWith("Yell.", StringComparison.Ordinal)) return settings.YellEnabled;
            return true;
        }
    }
}
