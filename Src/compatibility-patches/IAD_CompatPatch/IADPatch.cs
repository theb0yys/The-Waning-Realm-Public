using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;

namespace IAD_CompatPatch
{
    [HarmonyPatch]
    public static class IADPatch
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Logs",
            "iad_compatpatch.log");

        static IEnumerable<MethodBase> TargetMethods()
        {
            var results = new List<MethodBase>();

            try
            {
                // Find the exact type shown in your log: InjuriesAndDiseases.Patches.HeroPatches
                var heroPatchesType = AccessTools.TypeByName("InjuriesAndDiseases.Patches.HeroPatches");
                if (heroPatchesType != null)
                {
                    var methods = heroPatchesType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var m in methods)
                    {
                        if (m == null) continue;

                        // This catches: Postfix_CalculateDamage, Postfix_CalculateDamage_Patch2, etc.
                        if (m.Name.StartsWith("Postfix_CalculateDamage", StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(m);
                            SafeLog("Targeting IAD damage hook: " + heroPatchesType.FullName + "::" + m.Name);
                        }
                    }
                }
                else
                {
                    SafeLog("Type not found: InjuriesAndDiseases.Patches.HeroPatches");
                }

                SafeLog("Total IAD damage-hook targets: " + results.Count);
            }
            catch (Exception ex)
            {
                SafeLog("TargetMethods() failed: " + ex);
            }

            return results;
        }

        // SAFEST OPTION: block IAD damage hooks entirely in TAOM context
        static bool Prefix(MethodBase __originalMethod)
        {
            SafeLog("Blocked IAD damage hook: " + (__originalMethod == null ? "<unknown>" : __originalMethod.ToString()));
            return false;
        }

        static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
            {
                SafeLog("Suppressed exception in blocked IAD hook: " +
                        (__originalMethod == null ? "<unknown>" : __originalMethod.ToString()) +
                        " => " + __exception);
                return null;
            }

            return null;
        }

        private static void SafeLog(string msg)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(
                    LogPath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + msg + Environment.NewLine
                );
            }
            catch
            {
            }
        }
    }
}