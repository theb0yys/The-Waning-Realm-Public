using System;
using System.IO;
using System.Reflection;

namespace CustomBattleReturn.Tests
{
    internal static class NativeApiProbe
    {
        internal static int Run(string gameRoot)
        {
            var directories = new[]
            {
                Path.Combine(gameRoot, "bin/Win64_Shipping_Client"),
                Path.Combine(gameRoot, "Modules/Native/bin/Win64_Shipping_Client"),
                Path.Combine(gameRoot, "Modules/CustomBattle/bin/Win64_Shipping_Client")
            };
            ResolveEventHandler resolver = (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name + ".dll";
                foreach (var directory in directories)
                {
                    var path = Path.Combine(directory, name);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            try
            {
                var assembly = Assembly.LoadFrom(Path.Combine(directories[1], "TaleWorlds.MountAndBlade.GauntletUI.dll"));
                var menu = assembly.GetType("TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer.MissionGauntletSingleplayerEscapeMenu", true);
                var baseMenu = assembly.GetType("TaleWorlds.MountAndBlade.GauntletUI.Mission.MissionGauntletEscapeMenuBase", true);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                var target = menu.GetMethod("GetEscapeMenuItems", flags);
                if (target == null || target.GetParameters().Length != 0 || !target.ReturnType.IsGenericType ||
                    target.ReturnType.GetGenericTypeDefinition() != typeof(System.Collections.Generic.List<>) ||
                    target.ReturnType.GetGenericArguments()[0].FullName != "TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu.EscapeMenuItemVM")
                    throw new NotSupportedException("Installed native pause menu signature does not match.");
                var toggle = baseMenu.GetMethod("OnEscapeMenuToggled", flags, null, new[] { typeof(bool) }, null);
                if (toggle == null || toggle.ReturnType != typeof(bool))
                    throw new NotSupportedException("Installed close-menu signature does not match.");
                var delegateType = typeof(Func<,,>).MakeGenericType(baseMenu, typeof(bool), typeof(bool));
                var closeMenu = Delegate.CreateDelegate(delegateType, toggle);
                if (closeMenu == null) throw new InvalidOperationException("Native protected delegate binding failed.");
                Console.WriteLine("PASS: installed native pause-menu signatures and protected close-menu delegate binding; no engine calls.");
                return 0;
            }
            finally { AppDomain.CurrentDomain.AssemblyResolve -= resolver; }
        }
    }
}
