using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace Fourberie_CompatPatch
{
    [HarmonyPatch]
    public static class FourberieDamageSafetyPatch
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Logs",
            "fourberie_compatpatch.log");

        private static readonly List<MethodBase> CachedTargets = new List<MethodBase>();
        private static bool _prepared;

        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (_prepared)
                return CachedTargets.Count > 0;

            _prepared = true;
            CachedTargets.Clear();

            try
            {
                SafeLog("Prepare() starting Fourberie target resolution...");

                foreach (var m in ResolveTargetMethods())
                {
                    if (m != null)
                        CachedTargets.Add(m);
                }

                if (CachedTargets.Count == 0)
                {
                    SafeLog("Prepare(): no compatible Fourberie damage methods found. Patch will be skipped cleanly.");
                    return false;
                }

                SafeLog("Prepare(): total target methods selected = " + CachedTargets.Count);
                return true;
            }
            catch (Exception ex)
            {
                SafeLog("Prepare() failed: " + ex);
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
                // Exact Fourberie v1.4.7.6 target from the installed DLL.
                string[] knownTypeNames =
                {
                    "Fourberie.FModelDamage"
                };

                for (int i = 0; i < knownTypeNames.Length; i++)
                {
                    var t = AccessTools.TypeByName(knownTypeNames[i]);
                    if (t == null)
                    {
                        SafeLog("Known type not found: " + knownTypeNames[i]);
                        continue;
                    }

                    SafeLog("Found known type: " + t.FullName);
                    AddTargetsFromType(t, results, seen, "known-type scan");
                }

                // Fallback: scan all loaded assemblies containing "Fourberie"
                if (results.Count == 0)
                {
                    SafeLog("No targets from known-type scan. Attempting assembly-wide Fourberie scan...");

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

                        SafeLog("Scanning assembly: " + asmName);

                        Type[] types;
                        try
                        {
                            types = asm.GetTypes();
                        }
                        catch (ReflectionTypeLoadException rtle)
                        {
                            types = rtle.Types;
                            SafeLog("ReflectionTypeLoadException while scanning " + asmName);

                            if (rtle.LoaderExceptions != null)
                            {
                                foreach (var loaderEx in rtle.LoaderExceptions)
                                {
                                    if (loaderEx != null)
                                        SafeLog("LoaderException: " + loaderEx.GetType().Name + ": " + loaderEx.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SafeLog("Failed to enumerate types from " + asmName + ": " + ex);
                            continue;
                        }

                        if (types == null)
                            continue;

                        foreach (var t in types)
                        {
                            if (t == null)
                                continue;

                            AddTargetsFromType(t, results, seen, "assembly scan");
                        }
                    }
                }

                if (results.Count == 0)
                {
                    SafeLog("ResolveTargetMethods(): no compatible Fourberie damage methods found.");
                }
                else
                {
                    SafeLog("ResolveTargetMethods(): final target count = " + results.Count);
                }
            }
            catch (Exception ex)
            {
                SafeLog("ResolveTargetMethods() failed: " + ex);
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
                SafeLog("Failed to inspect methods on " + t.FullName + " during " + source + ": " + ex.Message);
                return;
            }

            foreach (var m in methods)
            {
                try
                {
                    if (!IsLikelyFourberieDamageMethod(m))
                        continue;

                    string key = GetMethodKey(m);
                    if (!seen.Add(key))
                        continue;

                    results.Add(m);
                    SafeLog("Target candidate added (" + source + "): " + m.DeclaringType.FullName + "::" + m.Name);
                }
                catch (Exception ex)
                {
                    SafeLog("Error inspecting method " + t.FullName + "::" + (m == null ? "<null>" : m.Name) + " => " + ex);
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

        private static bool IsLikelyFourberieDamageMethod(MethodInfo m)
        {
            if (m == null)
                return false;

            if (!string.Equals(m.Name, "ApplyGeneralDamageModifiers", StringComparison.Ordinal))
                return false;

            var ps = m.GetParameters();
            if (ps == null || ps.Length != 3)
                return false;

            if (m.ReturnType != typeof(float))
                return false;

            return IsParameterType(ps[0].ParameterType, typeof(AttackInformation)) &&
                   IsParameterType(ps[1].ParameterType, typeof(AttackCollisionData)) &&
                   ps[2].ParameterType == typeof(float);
        }

        static bool Prefix(
            MethodBase __originalMethod,
            ref AttackInformation attackInformation,
            ref AttackCollisionData collisionData,
            float baseDamage,
            ref float __result)
        {
            __result = baseDamage;

            try
            {
                if (baseDamage <= 0f)
                {
                    CompatLog.Once("damage-base-non-positive", "Skipped Fourberie damage method: baseDamage <= 0.");
                    return false;
                }

                MissionWeapon attackerWeapon = attackInformation.AttackerWeapon;
                bool attackerWeaponIsEmpty;
                if (!TryGetMissionWeaponIsEmpty(ref attackerWeapon, out attackerWeaponIsEmpty))
                {
                    CompatLog.Once("damage-unsafe-attacker-weapon", "Skipped Fourberie damage method: unsafe AttackInformation.AttackerWeapon.");
                    return false;
                }

                if (!attackerWeaponIsEmpty)
                    return true;

                if (attackInformation.IsAttackerAgentHuman && attackInformation.AttackerAgentCharacter == null)
                {
                    CompatLog.Once("damage-missing-attacker-character", "Skipped Fourberie hand-to-hand damage method: missing AttackerAgentCharacter.");
                    return false;
                }

                if (IsVictimHitBodyPartZero(ref collisionData) && attackInformation.VictimAgent == null)
                {
                    CompatLog.Once("damage-missing-victim-agent", "Skipped Fourberie hand-to-hand damage method: missing VictimAgent for body-part branch.");
                    return false;
                }

                if (attackInformation.IsAttackerPlayer && Hero.MainHero == null)
                {
                    CompatLog.Once("damage-missing-main-hero", "Skipped Fourberie hand-to-hand damage method: MainHero unavailable for skill XP branch.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                __result = baseDamage;
                CompatLog.Once("damage-prefix-guard-exception", "Prefix guard exception; returning baseDamage. Exception=" + ex);
                return false;
            }
        }

        static Exception Finalizer(MethodBase __originalMethod, float baseDamage, Exception __exception, ref float __result)
        {
            if (__exception != null)
            {
                __result = baseDamage;
                CompatLog.Once("damage-finalizer-exception", "Suppressed exception in " +
                        (__originalMethod == null ? "<unknown>" : __originalMethod.ToString()) +
                        "; returning baseDamage. Exception=" + __exception);
                return null;
            }

            return null;
        }

        private static bool IsParameterType(Type actual, Type expected)
        {
            if (actual == null)
                return false;

            if (actual.IsByRef)
                actual = actual.GetElementType();

            return actual == expected;
        }

        private static bool TryGetMissionWeaponIsEmpty(ref MissionWeapon weapon, out bool isEmpty)
        {
            try
            {
                isEmpty = weapon.IsEmpty;
                return true;
            }
            catch (Exception ex)
            {
                isEmpty = true;
                CompatLog.Once("damage-mission-weapon-is-empty", "MissionWeapon.IsEmpty access failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static bool IsVictimHitBodyPartZero(ref AttackCollisionData collisionData)
        {
            try
            {
                return (int)collisionData.VictimHitBodyPart == 0;
            }
            catch (Exception ex)
            {
                CompatLog.Once("damage-victim-hit-body-part", "AttackCollisionData.VictimHitBodyPart access failed: " + ex.GetType().Name + ": " + ex.Message);
                return true;
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
