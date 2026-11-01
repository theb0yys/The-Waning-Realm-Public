using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XorberaxTFTAOMPackControl
{
    internal static class ModulePatchManager
    {
        private static readonly string HarmonyPrefix = "taom.xorberax.experience.module.";
        private static bool _campaignInstalled;
        private static bool _missionInstalled;

        internal static void InstallCampaign(PackSettings settings)
        {
            if (_campaignInstalled)
                return;

            settings = settings ?? PackSettings.Current;
            PatchAssembly("TrainingField", settings.TrainingFieldEnabled);
            _campaignInstalled = true;
            PackLog.Info("Installed delayed Xorberax campaign patches.");
        }

        internal static void InstallMission(PackSettings settings)
        {
            if (_missionInstalled)
                return;

            settings = settings ?? PackSettings.Current;
            PatchAssembly("CutThroughEveryone", settings.CutThroughEveryoneEnabled);
            PatchAssembly("DeadlyCombat", settings.DeadlyCombatEnabled);
            PatchAssembly("DeadlyHorseCharges", settings.DeadlyHorseChargesEnabled);
            PatchAssembly("KillFeedDisabler", settings.KillFeedDisablerEnabled);
            PatchAssembly("ShoulderCam", settings.ShoulderCamEnabled);
            PatchAssembly("TextToSpeech", settings.TextToSpeechEnabled);
            _missionInstalled = true;
            PackLog.Info("Installed delayed Xorberax mission patches.");
        }

        internal static void Uninstall()
        {
            if (!_campaignInstalled && !_missionInstalled)
                return;

            foreach (var id in new[]
            {
                "CutThroughEveryone",
                "DeadlyCombat",
                "DeadlyHorseCharges",
                "KillFeedDisabler",
                "ShoulderCam",
                "TextToSpeech",
                "TrainingField"
            })
            {
                try { new Harmony(HarmonyPrefix + id.ToLowerInvariant()).UnpatchAll(HarmonyPrefix + id.ToLowerInvariant()); }
                catch { }
            }

            _campaignInstalled = false;
            _missionInstalled = false;
        }

        private static void PatchAssembly(string assemblyName, bool enabled)
        {
            if (!enabled)
                return;

            try
            {
                var assembly = FindOrLoadAssembly(assemblyName);
                if (assembly == null)
                    return;

                new Harmony(HarmonyPrefix + assemblyName.ToLowerInvariant()).PatchAll(assembly);
            }
            catch (Exception ex)
            {
                PackLog.Error("Could not install delayed patches for " + assemblyName, ex);
            }
        }

        private static Assembly FindOrLoadAssembly(string assemblyName)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(asm => string.Equals(asm.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
            if (loaded != null)
                return loaded;

            var path = GetLocalAssemblyPath(assemblyName + ".dll");
            return path != null && File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        private static string GetLocalAssemblyPath(string fileName)
        {
            try
            {
                var assemblyDir = Path.GetDirectoryName(typeof(ModulePatchManager).Assembly.Location);
                return assemblyDir == null ? null : Path.Combine(assemblyDir, fileName);
            }
            catch
            {
                return null;
            }
        }
    }
}
