using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    public static class RuntimeLifecycle
    {
        private static Action load, root;
        private static Action<Game, IGameStarter> start;
        private static Action<float> tick;
        private static bool loaded;

        public static void Install(Harmony harmony, Assembly original, Assembly runtime)
        {
            Type adapted = runtime.GetType("MyLittleWarband.SubModule", true);
            object instance = Activator.CreateInstance(adapted);
            load = Bind<Action>(adapted, instance, "OnSubModuleLoad");
            root = Bind<Action>(adapted, instance, "OnBeforeInitialModuleScreenSetAsRoot");
            start = Bind<Action<Game, IGameStarter>>(adapted, instance, "OnGameStart");
            tick = Bind<Action<float>>(adapted, instance, "OnApplicationTick");
            loaded = false;
            Type legacy = original.GetType("MyLittleWarband.SubModule", true);
            Patch(harmony, legacy, "OnSubModuleLoad", nameof(LoadPrefix));
            Patch(harmony, legacy, "OnBeforeInitialModuleScreenSetAsRoot", nameof(RootPrefix));
            Patch(harmony, legacy, "OnGameStart", nameof(StartPrefix));
            Patch(harmony, legacy, "OnApplicationTick", nameof(TickPrefix));
        }

        private static T Bind<T>(Type type, object instance, string name) where T : Delegate =>
            (T)Delegate.CreateDelegate(typeof(T), instance, AccessTools.DeclaredMethod(type, name));

        private static void Patch(Harmony harmony, Type type, string name, string prefix) =>
            harmony.Patch(AccessTools.DeclaredMethod(type, name), prefix: new HarmonyMethod(typeof(RuntimeLifecycle), prefix));

        private static bool LoadPrefix()
        {
            if (!loaded) { load(); loaded = true; }
            return false;
        }
        private static bool RootPrefix() { root(); return false; }
        private static bool StartPrefix(Game __0, IGameStarter __1) { start(__0, __1); return false; }
        private static bool TickPrefix(float __0) { tick(__0); return false; }
    }
}
