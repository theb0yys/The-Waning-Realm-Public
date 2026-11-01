using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XorberaxTFTAOMPackControl
{
    internal static class DependencyResolver
    {
        private static bool _installed;
        private static Assembly _newtonsoft;
        private static Assembly _harmony;

        internal static void Install()
        {
            if (_installed)
                return;

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            _installed = true;
        }

        internal static void Uninstall()
        {
            if (!_installed)
                return;

            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _installed = false;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requested;
            try { requested = new AssemblyName(args.Name); }
            catch { return null; }

            if (string.Equals(requested.Name, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
            {
                var loaded = FindLoadedAssembly("Newtonsoft.Json", requested.Version);
                if (loaded != null)
                    return loaded;

                return _newtonsoft ?? (_newtonsoft = LoadLocalNewtonsoft(requested.Version));
            }

            if (string.Equals(requested.Name, "0Harmony", StringComparison.OrdinalIgnoreCase))
            {
                var loaded = FindLoadedAssembly("0Harmony", null);
                if (loaded != null)
                    return loaded;

                return _harmony ?? (_harmony = LoadTaomDependency("0Harmony.dll"));
            }

            return null;
        }

        private static Assembly FindLoadedAssembly(string name, Version version)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(asm =>
                {
                    var asmName = asm.GetName();
                    if (!string.Equals(asmName.Name, name, StringComparison.OrdinalIgnoreCase))
                        return false;
                    return version == null || asmName.Version == version;
                });
        }

        private static Assembly LoadLocalNewtonsoft(Version requestedVersion)
        {
            var path = GetLocalDependencyPath("Newtonsoft.Json.dll");
            if (path == null || !File.Exists(path))
                return null;

            try
            {
                var name = AssemblyName.GetAssemblyName(path);
                if (requestedVersion != null && name.Version != requestedVersion)
                {
                    PackLog.Error("Rejected Newtonsoft.Json dependency version " + name.Version + " for requested " + requestedVersion, new InvalidOperationException(path));
                    return null;
                }

                PackLog.Info("Resolving Newtonsoft.Json through exact local pack copy: " + path);
                return Assembly.LoadFrom(path);
            }
            catch (Exception ex)
            {
                PackLog.Error("Could not resolve Newtonsoft.Json through local pack copy", ex);
                return null;
            }
        }

        private static string GetLocalDependencyPath(string fileName)
        {
            try
            {
                var assemblyDir = new DirectoryInfo(Path.GetDirectoryName(typeof(DependencyResolver).Assembly.Location));
                return Path.Combine(assemblyDir.FullName, fileName);
            }
            catch
            {
                return null;
            }
        }

        private static Assembly LoadTaomDependency(string fileName)
        {
            var path = GetTaomDependencyPath(fileName);
            if (path == null || !File.Exists(path))
                return null;

            try
            {
                PackLog.Info("Resolving " + fileName + " through TAOM.Dependencies runtime copy: " + path);
                return Assembly.LoadFrom(path);
            }
            catch (Exception ex)
            {
                PackLog.Error("Could not resolve " + fileName + " through TAOM.Dependencies", ex);
                return null;
            }
        }

        private static string GetTaomDependencyPath(string fileName)
        {
            try
            {
                var assemblyDir = new DirectoryInfo(Path.GetDirectoryName(typeof(DependencyResolver).Assembly.Location));
                var moduleDir = assemblyDir.Parent?.Parent;
                var modulesDir = moduleDir?.Parent;
                return modulesDir == null ? null : Path.Combine(modulesDir.FullName, "TAOM.Dependencies", "bin", "Win64_Shipping_Client", fileName);
            }
            catch
            {
                return null;
            }
        }
    }
}
