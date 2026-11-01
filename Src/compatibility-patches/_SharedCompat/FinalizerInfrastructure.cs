using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TAOM.Compat.Shared
{
    internal static class CompatLog
    {
        private static string _fileName = "taom_compatpatch.log";

        internal static void Configure(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
                _fileName = fileName;
        }

        internal static void Info(string message)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord",
                    "Logs",
                    _fileName);

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(
                    path,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message + Environment.NewLine);
            }
            catch
            {
            }
        }

        internal static void Suppressed(string group, MethodBase method, Exception exception)
        {
            Info("Suppressed exception in " + group + " target " + Describe(method) + ": " + exception);
        }

        internal static string Describe(MethodBase method)
        {
            if (method == null)
                return "<unknown>";

            var typeName = method.DeclaringType == null ? "<no type>" : method.DeclaringType.FullName;
            return typeName + "::" + method.Name;
        }
    }

    internal static class CompatBootstrap
    {
        internal static void ApplyAnnotatedPatchClasses(Harmony harmony, Assembly assembly)
        {
            foreach (var type in GetTypes(assembly))
            {
                if (!HasHarmonyPatch(type))
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    CompatLog.Info("Applied patch class: " + type.FullName);
                }
                catch (Exception ex)
                {
                    CompatLog.Info("Skipped patch class " + type.FullName + ": " + ex);
                }
            }
        }

        private static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        private static bool HasHarmonyPatch(Type type)
        {
            try
            {
                return type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class CompatResolver
    {
        internal static IEnumerable<MethodBase> ResolveMethods(
            string[] typeNames,
            Func<MethodInfo, bool> filter,
            string group,
            bool requireMcm = false)
        {
            var results = new List<MethodBase>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (requireMcm && !IsMcmReady())
            {
                CompatLog.Info("Skipping " + group + " targets because MCMv5 5.10.2+ is not loaded.");
                return results;
            }

            foreach (var typeName in typeNames)
            {
                var type = AccessTools.TypeByName(typeName);
                if (type == null)
                    continue;

                foreach (var method in SafeMethods(type))
                {
                    if (method == null ||
                        method.IsSpecialName ||
                        method.DeclaringType != type ||
                        !filter(method))
                    {
                        continue;
                    }

                    Add(results, seen, method, group);
                }
            }

            CompatLog.Info("Resolved " + group + " targets: " + results.Count);
            return results;
        }

        internal static IEnumerable<MethodInfo> SafeMethods(Type type)
        {
            try
            {
                return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }
            catch (Exception ex)
            {
                CompatLog.Info("Failed to inspect " + type.FullName + ": " + ex);
                return Enumerable.Empty<MethodInfo>();
            }
        }

        internal static void Add(List<MethodBase> results, HashSet<string> seen, MethodBase method, string group)
        {
            var key = CompatLog.Describe(method);
            if (seen.Add(key))
                results.Add(method);
        }

        internal static bool IsPatchBool(MethodInfo method)
        {
            return method.ReturnType == typeof(bool) && IsPrefixOrPostfix(method);
        }

        internal static bool IsPatchVoid(MethodInfo method)
        {
            return method.ReturnType == typeof(void) && IsPrefixOrPostfix(method);
        }

        internal static bool IsPrefixOrPostfix(MethodInfo method)
        {
            return method.Name.StartsWith("Prefix", StringComparison.Ordinal) ||
                   method.Name.StartsWith("Postfix", StringComparison.Ordinal);
        }

        internal static bool IsMcmReady()
        {
            var asm = FindOrLoadAssembly("MCMv5");
            if (asm == null)
                return false;

            var version = asm.GetName().Version;
            return version != null && version >= new Version(5, 10, 2, 0);
        }

        internal static Assembly FindOrLoadAssembly(string name)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                        return asm;
                }

                return Assembly.Load(name);
            }
            catch
            {
                return null;
            }
        }
    }
}
