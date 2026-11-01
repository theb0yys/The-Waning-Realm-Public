using System;
using System.IO;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace XorberaxTFTAOMPackControl
{
    internal sealed class ManagedModuleHost
    {
        [Flags]
        private enum ModuleHook
        {
            None = 0,
            GameStart = 1,
            Mission = 2,
            Tick = 4
        }

        private sealed class ModuleEntry
        {
            internal readonly string Name;
            internal readonly string AssemblyName;
            internal readonly string TypeName;
            internal readonly Func<PackSettings, bool> IsEnabled;
            internal readonly ModuleHook Hooks;
            internal Assembly Assembly;
            internal Type Type;
            internal object Instance;
            internal bool LoadInvoked;
            internal bool Failed;
            internal MethodInfo GameStartMethod;
            internal MethodInfo MissionBehaviorInitializeMethod;
            internal MethodInfo ApplicationTickMethod;
            internal MethodInfo SubModuleUnloadedMethod;
            internal MethodInfo SubModuleLoadMethod;
            internal Action<float> ApplicationTickInvoker;
            internal bool GameStartMethodResolved;
            internal bool MissionBehaviorInitializeMethodResolved;
            internal bool ApplicationTickMethodResolved;
            internal bool SubModuleUnloadedMethodResolved;
            internal bool SubModuleLoadMethodResolved;
            internal bool ApplicationTickInvokerResolved;

            internal ModuleEntry(string name, string assemblyName, string typeName, Func<PackSettings, bool> isEnabled, ModuleHook hooks)
            {
                Name = name;
                AssemblyName = assemblyName;
                TypeName = typeName;
                IsEnabled = isEnabled;
                Hooks = hooks;
            }
        }

        private static readonly Type[] GameStartParameterTypes = { typeof(Game), typeof(IGameStarter) };
        private static readonly Type[] MissionParameterTypes = { typeof(Mission) };
        private static readonly Type[] TickParameterTypes = { typeof(float) };

        private readonly ModuleEntry[] _modules =
        {
            new ModuleEntry("Banks", "Banks", "Banks.SubModule", settings => settings.BanksUnsafeEnabled, ModuleHook.GameStart),
            new ModuleEntry("Cut Through Everyone", "CutThroughEveryone", "CutThroughEveryone.SubModule", settings => settings.CutThroughEveryoneEnabled, ModuleHook.Mission),
            new ModuleEntry("Deadly Combat", "DeadlyCombat", "DeadlyCombat.SubModule", settings => settings.DeadlyCombatEnabled, ModuleHook.Mission),
            new ModuleEntry("Deadly Horse Charges", "DeadlyHorseCharges", "DeadlyHorseCharges.SubModule", settings => settings.DeadlyHorseChargesEnabled, ModuleHook.Mission),
            new ModuleEntry("Kill Feed Disabler", "KillFeedDisabler", "KillFeedDisabler.SubModule", settings => settings.KillFeedDisablerEnabled, ModuleHook.None),
            new ModuleEntry("Scholars", "ScholarsOfCalradia", "ScholarsOfCalradia.SubModule", settings => settings.ScholarsEnabled, ModuleHook.GameStart),
            new ModuleEntry("Shoulder Cam", "ShoulderCam", "ShoulderCam.SubModule", settings => settings.ShoulderCamEnabled, ModuleHook.Mission),
            new ModuleEntry("Text To Speech", "TextToSpeech", "TextToSpeech.SubModule", settings => settings.TextToSpeechEnabled, ModuleHook.None),
            new ModuleEntry("Training Field", "TrainingField", "TrainingField.SubModule", settings => settings.TrainingFieldEnabled, ModuleHook.GameStart),
            new ModuleEntry("Work", "Work", "Work.SubModule", settings => settings.WorkEnabled, ModuleHook.GameStart),
            new ModuleEntry("Yell", "Yell", "Yell.SubModule", settings => settings.YellEnabled, ModuleHook.Tick)
        };

        internal void OnGameStart(PackSettings settings, Game game, IGameStarter gameStarter)
        {
            settings = settings ?? PackSettings.Current;
            for (int i = 0; i < _modules.Length; i++)
            {
                ModuleEntry module = _modules[i];
                if (IsEnabledForHook(module, settings, ModuleHook.GameStart))
                {
                    Invoke(module, "OnGameStart", GameStartParameterTypes, game, gameStarter);
                }
            }
        }

        internal void OnMissionBehaviorInitialize(PackSettings settings, Mission mission)
        {
            settings = settings ?? PackSettings.Current;
            for (int i = 0; i < _modules.Length; i++)
            {
                ModuleEntry module = _modules[i];
                if (IsEnabledForHook(module, settings, ModuleHook.Mission))
                {
                    Invoke(module, "OnMissionBehaviorInitialize", MissionParameterTypes, mission);
                }
            }
        }

        internal void OnApplicationTick(PackSettings settings, float dt)
        {
            settings = settings ?? PackSettings.Current;
            for (int i = 0; i < _modules.Length; i++)
            {
                ModuleEntry module = _modules[i];
                if (IsEnabledForHook(module, settings, ModuleHook.Tick))
                {
                    InvokeApplicationTick(module, dt);
                }
            }
        }

        internal void OnSubModuleUnloaded()
        {
            for (int i = 0; i < _modules.Length; i++)
            {
                ModuleEntry module = _modules[i];
                if (module.Instance != null)
                {
                    Invoke(module, "OnSubModuleUnloaded", Type.EmptyTypes);
                }
            }
        }

        private static bool IsEnabledForHook(ModuleEntry module, PackSettings settings, ModuleHook hook)
        {
            if (module == null)
            {
                return false;
            }

            if (hook != ModuleHook.None && (module.Hooks & hook) != hook)
            {
                return false;
            }

            try
            {
                return module.IsEnabled(settings);
            }
            catch
            {
                return false;
            }
        }

        private void Invoke(ModuleEntry module, string methodName, Type[] parameterTypes, params object[] args)
        {
            if (!EnsureLoaded(module, true))
                return;

            MethodInfo method = GetCachedMethod(module, methodName, parameterTypes);
            if (method == null)
                return;

            try
            {
                method.Invoke(module.Instance, args);
            }
            catch (TargetInvocationException ex)
            {
                PackLog.Error("Managed " + module.Name + " hook failed: " + methodName, ex.InnerException ?? ex);
            }
            catch (Exception ex)
            {
                PackLog.Error("Managed " + module.Name + " hook failed: " + methodName, ex);
            }
        }

        private void InvokeApplicationTick(ModuleEntry module, float dt)
        {
            if (!EnsureLoaded(module, true))
                return;

            Action<float> tick = GetApplicationTickInvoker(module);
            if (tick == null)
                return;

            try
            {
                tick(dt);
            }
            catch (Exception ex)
            {
                PackLog.Error("Managed " + module.Name + " hook failed: OnApplicationTick", ex);
            }
        }

        private bool EnsureLoaded(ModuleEntry module, bool invokeLoad)
        {
            if (module.Failed)
                return false;

            try
            {
                if (module.Assembly == null)
                    module.Assembly = FindOrLoadAssembly(module.AssemblyName);

                if (module.Assembly == null)
                    return false;

                if (module.Type == null)
                    module.Type = module.Assembly.GetType(module.TypeName, true, false);

                if (module.Instance == null)
                    module.Instance = Activator.CreateInstance(module.Type);

                if (invokeLoad && !module.LoadInvoked)
                {
                    InvokeLoad(module);
                    module.LoadInvoked = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                module.Failed = true;
                PackLog.Error("Could not load managed Xorberax module " + module.Name, ex);
                return false;
            }
        }

        private void InvokeLoad(ModuleEntry module)
        {
            MethodInfo method = GetCachedMethod(module, "OnSubModuleLoad", Type.EmptyTypes);
            if (method == null)
                return;

            try
            {
                method.Invoke(module.Instance, null);
                PackLog.Info("Loaded managed Xorberax module " + module.Name + ".");
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        private static MethodInfo GetCachedMethod(ModuleEntry module, string methodName, Type[] parameterTypes)
        {
            if (string.Equals(methodName, "OnGameStart", StringComparison.Ordinal))
            {
                if (!module.GameStartMethodResolved)
                {
                    module.GameStartMethod = ResolveMethod(module, methodName, parameterTypes);
                    module.GameStartMethodResolved = true;
                }

                return module.GameStartMethod;
            }

            if (string.Equals(methodName, "OnMissionBehaviorInitialize", StringComparison.Ordinal))
            {
                if (!module.MissionBehaviorInitializeMethodResolved)
                {
                    module.MissionBehaviorInitializeMethod = ResolveMethod(module, methodName, parameterTypes);
                    module.MissionBehaviorInitializeMethodResolved = true;
                }

                return module.MissionBehaviorInitializeMethod;
            }

            if (string.Equals(methodName, "OnApplicationTick", StringComparison.Ordinal))
            {
                if (!module.ApplicationTickMethodResolved)
                {
                    module.ApplicationTickMethod = ResolveMethod(module, methodName, parameterTypes);
                    module.ApplicationTickMethodResolved = true;
                }

                return module.ApplicationTickMethod;
            }

            if (string.Equals(methodName, "OnSubModuleUnloaded", StringComparison.Ordinal))
            {
                if (!module.SubModuleUnloadedMethodResolved)
                {
                    module.SubModuleUnloadedMethod = ResolveMethod(module, methodName, parameterTypes);
                    module.SubModuleUnloadedMethodResolved = true;
                }

                return module.SubModuleUnloadedMethod;
            }

            if (string.Equals(methodName, "OnSubModuleLoad", StringComparison.Ordinal))
            {
                if (!module.SubModuleLoadMethodResolved)
                {
                    module.SubModuleLoadMethod = ResolveMethod(module, methodName, parameterTypes);
                    module.SubModuleLoadMethodResolved = true;
                }

                return module.SubModuleLoadMethod;
            }

            return ResolveMethod(module, methodName, parameterTypes);
        }

        private static MethodInfo ResolveMethod(ModuleEntry module, string methodName, Type[] parameterTypes)
        {
            MethodInfo method = module.Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
            return method == null || method.DeclaringType == typeof(MBSubModuleBase)
                ? null
                : method;
        }

        private static Action<float> GetApplicationTickInvoker(ModuleEntry module)
        {
            if (!module.ApplicationTickInvokerResolved)
            {
                MethodInfo method = GetCachedMethod(module, "OnApplicationTick", TickParameterTypes);
                if (method != null)
                {
                    try
                    {
                        module.ApplicationTickInvoker = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), module.Instance, method, false);
                    }
                    catch (Exception ex)
                    {
                        module.Failed = true;
                        PackLog.Error("Could not bind managed Xorberax module " + module.Name + " OnApplicationTick delegate", ex);
                    }
                }

                module.ApplicationTickInvokerResolved = true;
            }

            return module.ApplicationTickInvoker;
        }

        private static Assembly FindOrLoadAssembly(string assemblyName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }

            var path = GetLocalAssemblyPath(assemblyName + ".dll");
            return path != null && File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        private static string GetLocalAssemblyPath(string fileName)
        {
            try
            {
                var assemblyDir = Path.GetDirectoryName(typeof(ManagedModuleHost).Assembly.Location);
                return assemblyDir == null ? null : Path.Combine(assemblyDir, fileName);
            }
            catch
            {
                return null;
            }
        }
    }
}
