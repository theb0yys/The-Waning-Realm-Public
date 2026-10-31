using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Fourberie_CompatPatch
{
    [HarmonyPatch]
    public static class FourberieFoodSafetyPatch
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Logs",
            "fourberie_compatpatch.log");

        private static readonly List<MethodBase> CachedTargets = new List<MethodBase>();
        private static readonly HashSet<string> PassThroughLogged = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object Sync = new object();
        private static bool _prepared;
        private static string _lastStatus = "Food patch not prepared.";

        public static int ResolvedTargetCount
        {
            get { return CachedTargets.Count; }
        }

        public static bool IsPrepared
        {
            get { return _prepared; }
        }

        public static string GetDiagnosticStatus()
        {
            try
            {
                return _lastStatus + " Targets=" + CachedTargets.Count + ".";
            }
            catch
            {
                return "Food patch diagnostic status unavailable.";
            }
        }

        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (_prepared)
            {
                _lastStatus = CachedTargets.Count > 0
                    ? "Food patch already prepared with resolved targets."
                    : "Food patch already prepared but no targets were resolved.";
                return CachedTargets.Count > 0;
            }

            _prepared = true;
            CachedTargets.Clear();
            lock (Sync)
            {
                PassThroughLogged.Clear();
            }

            try
            {
                SafeLog("Food Prepare() starting Fourberie target resolution...");

                foreach (var m in ResolveTargetMethods())
                {
                    if (m != null)
                        CachedTargets.Add(m);
                }

                if (CachedTargets.Count == 0)
                {
                    _lastStatus = "Food patch prepared but no compatible Fourberie food methods were found.";
                    SafeLog("Food Prepare(): no compatible Fourberie food methods found. Patch will be skipped cleanly.");
                    return false;
                }

                _lastStatus = "Food patch prepared successfully.";
                SafeLog("Food Prepare(): total target methods selected = " + CachedTargets.Count);
                return true;
            }
            catch (Exception ex)
            {
                _lastStatus = "Food patch prepare failed: " + ex.GetType().Name;
                SafeLog("Food Prepare() failed: " + ex);
                return false;
            }
        }

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return CachedTargets;
        }

        private static IEnumerable<MethodBase> ResolveTargetMethods()
        {
            var results = new List<MethodBase>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                string[] knownTypeNames =
                {
                    "Fourberie.FModelMobileFood",
                    "Fourberie.Models.FModelMobileFood"
                };

                for (int i = 0; i < knownTypeNames.Length; i++)
                {
                    var t = AccessTools.TypeByName(knownTypeNames[i]);
                    if (t == null)
                    {
                        SafeLog("Food known type not found: " + knownTypeNames[i]);
                        continue;
                    }

                    SafeLog("Food found known type: " + t.FullName);
                    AddTargetsFromType(t, results, seen, "food known-type scan");
                }

                if (results.Count == 0)
                {
                    SafeLog("Food no targets from known-type scan. Attempting assembly-wide Fourberie scan...");

                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        string asmName;
                        try
                        {
                            asmName = asm.GetName().Name;
                        }
                        catch
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(asmName))
                            continue;

                        if (asmName.IndexOf("Fourberie", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        SafeLog("Food scanning assembly: " + asmName);

                        Type[] types;
                        try
                        {
                            types = asm.GetTypes();
                        }
                        catch (ReflectionTypeLoadException rtle)
                        {
                            types = rtle.Types;
                            SafeLog("Food ReflectionTypeLoadException while scanning " + asmName);

                            if (rtle.LoaderExceptions != null)
                            {
                                foreach (var loaderEx in rtle.LoaderExceptions)
                                {
                                    if (loaderEx != null)
                                        SafeLog("Food LoaderException: " + loaderEx.GetType().Name + ": " + loaderEx.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SafeLog("Food failed to enumerate types from " + asmName + ": " + ex);
                            continue;
                        }

                        if (types == null)
                            continue;

                        foreach (var t in types)
                        {
                            if (t == null)
                                continue;

                            var fullName = t.FullName ?? string.Empty;
                            if (fullName.IndexOf("FModelMobileFood", StringComparison.OrdinalIgnoreCase) < 0 &&
                                fullName.IndexOf("MobileFood", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;

                            AddTargetsFromType(t, results, seen, "food assembly scan");
                        }
                    }
                }

                if (results.Count == 0)
                {
                    SafeLog("Food ResolveTargetMethods(): no compatible Fourberie food methods found.");
                }
                else
                {
                    SafeLog("Food ResolveTargetMethods(): final target count = " + results.Count);
                }
            }
            catch (Exception ex)
            {
                SafeLog("Food ResolveTargetMethods() failed: " + ex);
            }

            return results;
        }

        private static void AddTargetsFromType(Type t, List<MethodBase> results, HashSet<string> seen, string source)
        {
            MethodInfo[] methods;

            try
            {
                methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                SafeLog("Food failed to inspect methods on " + t.FullName + " during " + source + ": " + ex.Message);
                return;
            }

            foreach (var m in methods)
            {
                try
                {
                    if (!IsLikelyFourberieFoodMethod(m))
                        continue;

                    string key = GetMethodKey(m);
                    if (!seen.Add(key))
                        continue;

                    results.Add(m);
                    SafeLog("Food target candidate added (" + source + "): " + m.DeclaringType.FullName + "::" + m.Name);
                }
                catch (Exception ex)
                {
                    SafeLog("Food error inspecting method " + t.FullName + "::" + (m == null ? "<null>" : m.Name) + " => " + ex);
                }
            }
        }

        private static string GetMethodKey(MethodBase method)
        {
            try
            {
                return method.Module.ModuleVersionId + ":" + method.MetadataToken;
            }
            catch
            {
                return method.DeclaringType.FullName + "::" + method.Name;
            }
        }

        private static bool IsLikelyFourberieFoodMethod(MethodInfo m)
        {
            if (m == null)
                return false;

            if (!string.Equals(m.Name, "CalculateDailyFoodConsumptionf", StringComparison.Ordinal))
                return false;

            var ps = m.GetParameters();
            if (ps == null || ps.Length != 2)
                return false;

            Type p0 = ps[0].ParameterType;
            Type p1 = ps[1].ParameterType;

            if (p0 != typeof(MobileParty))
                return false;

            if (p1 != typeof(ExplainedNumber))
                return false;

            return m.ReturnType == typeof(ExplainedNumber);
        }

        static bool Prefix(MethodBase __originalMethod, MobileParty party, ExplainedNumber baseConsumption, ref ExplainedNumber __result)
        {
            try
            {
                if (party == null)
                {
                    __result = baseConsumption;
                    CompatLog.Once("food-null-party", "Food Prefix guard fallback: null MobileParty. Returning conservative fallback.");
                    return false;
                }

                LogPassThroughOnce(__originalMethod, party);
            }
            catch (Exception ex)
            {
                __result = baseConsumption;
                CompatLog.Once("food-prefix-exception", "Food Prefix guard failed; returning conservative fallback. Exception=" + ex);
                return false;
            }

            return true;
        }

        static Exception Finalizer(MethodBase __originalMethod, MobileParty party, ExplainedNumber baseConsumption, Exception __exception, ref ExplainedNumber __result)
        {
            if (__exception == null)
                return null;

            try
            {
                __result = baseConsumption;
                CompatLog.Once("food-finalizer-exception", "Food Finalizer suppressed exception in " + DescribeMethod(__originalMethod) +
                        " for party='" + DescribeParty(party) + "'. Returning conservative fallback. Exception=" + __exception);
            }
            catch (Exception ex)
            {
                CompatLog.Once("food-finalizer-reporting-exception", "Food Finalizer reporting failed: " + ex);
            }

            return null;
        }

        private static void LogPassThroughOnce(MethodBase method, MobileParty party)
        {
            string key = GetMethodKey(method);
            lock (Sync)
            {
                if (!PassThroughLogged.Add(key))
                    return;
            }

            SafeLog("Food Prefix pass-through active for " + DescribeMethod(method) + " with party='" + DescribeParty(party) + "'.");
        }

        private static string DescribeMethod(MethodBase method)
        {
            try
            {
                if (method == null)
                    return "<unknown method>";

                return (method.DeclaringType == null ? "<no type>" : method.DeclaringType.FullName) + "::" + method.Name;
            }
            catch
            {
                return "<method description failed>";
            }
        }

        private static string DescribeParty(MobileParty party)
        {
            try
            {
                if (party == null)
                    return "<null>";

                string name = string.Empty;
                try
                {
                    if (party.Name != null)
                        name = party.Name.ToString();
                }
                catch
                {
                    name = string.Empty;
                }

                string stringId = string.Empty;
                try
                {
                    stringId = party.StringId ?? string.Empty;
                }
                catch
                {
                    stringId = string.Empty;
                }

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(stringId))
                    return name + " (" + stringId + ")";

                if (!string.IsNullOrEmpty(name))
                    return name;

                if (!string.IsNullOrEmpty(stringId))
                    return stringId;

                return party.GetHashCode().ToString();
            }
            catch
            {
                return "<party description failed>";
            }
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
