using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace CustomBattleReturn
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private MethodInfo _menuMethod;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var menuMethod = AccessTools.DeclaredMethod(typeof(MissionGauntletSingleplayerEscapeMenu), "GetEscapeMenuItems");
            var toggleMethod = AccessTools.DeclaredMethod(typeof(MissionGauntletEscapeMenuBase), "OnEscapeMenuToggled", new[] { typeof(bool) });
            if (menuMethod == null || menuMethod.ReturnType != typeof(List<EscapeMenuItemVM>) ||
                toggleMethod == null || toggleMethod.ReturnType != typeof(bool))
            {
                throw new NotSupportedException("Custom Battle Return: this Bannerlord version has an unsupported pause menu API.");
            }

            // Resolve the protected native action once, never while building menu rows.
            ReturnMenuPatch.CloseMenu = (Func<MissionGauntletEscapeMenuBase, bool, bool>)Delegate.CreateDelegate(
                typeof(Func<MissionGauntletEscapeMenuBase, bool, bool>), toggleMethod);
            _harmony = new Harmony("custombattlereturn.pause-menu");
            _menuMethod = menuMethod;
            _harmony.Patch(menuMethod, postfix: new HarmonyMethod(typeof(ReturnMenuPatch), nameof(ReturnMenuPatch.Postfix)));
        }

        protected override void OnSubModuleUnloaded()
        {
            _harmony?.Unpatch(_menuMethod, HarmonyPatchType.All, _harmony.Id);
            _harmony = null;
            _menuMethod = null;
            ReturnMenuPatch.CloseMenu = null;
            base.OnSubModuleUnloaded();
        }
    }
}
