using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TAOM_PalantirNetwork.UI.ViewModels;

namespace TAOM_PalantirNetwork.UI
{
    public sealed class PalantirScreen : ScreenBase
    {
        private static PalantirScreen _activeScreen;

        private readonly PalantirCampaignBehavior _behavior;
        private GauntletLayer _layer;
        private GauntletMovieIdentifier _movie;
        private PalantirVM _vm;

        private PalantirScreen(PalantirCampaignBehavior behavior)
        {
            _behavior = behavior;
        }

        public static bool TryOpen(PalantirCampaignBehavior behavior)
        {
            if (behavior == null)
                return false;

            if (_activeScreen != null)
            {
                _activeScreen._vm?.Refresh();
                return true;
            }

            try
            {
                ScreenManager.PushScreen(new PalantirScreen(behavior));
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print("TAOM_PalantirNetwork: failed to push board: " + ex.Message,
                    0, Debug.DebugColor.Red);
                return false;
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            try
            {
                _activeScreen = this;
                _vm = new PalantirVM(_behavior, Close);
                _layer = new GauntletLayer("TAOM_PalantirNetwork", 210, false);
                _layer.IsFocusLayer = true;
                _layer.InputRestrictions.SetInputRestrictions();
                _movie = _layer.LoadMovie("TAOM_PalantirNetwork", _vm);

                AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch (Exception ex)
            {
                Debug.Print("TAOM_PalantirNetwork: board initialize failed: " + ex.Message,
                    0, Debug.DebugColor.Red);
                try { Close(); } catch { }
            }
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);

            if (_layer != null && _layer.Input.IsKeyPressed(InputKey.Escape))
                Close();
        }

        protected override void OnFinalize()
        {
            try
            {
                if (_layer != null)
                {
                    _layer.InputRestrictions.ResetInputRestrictions();
                    RemoveLayer(_layer);
                }

                if (_movie != null)
                {
                    _layer?.ReleaseMovie(_movie);
                    _movie = null;
                }

                _vm?.OnFinalize();
            }
            catch (Exception ex)
            {
                Debug.Print("TAOM_PalantirNetwork: board finalize error: " + ex.Message,
                    0, Debug.DebugColor.Yellow);
            }
            finally
            {
                if (ReferenceEquals(_activeScreen, this))
                    _activeScreen = null;
                _layer = null;
                _vm = null;
            }

            base.OnFinalize();
        }

        private void Close()
        {
            ScreenManager.PopScreen();
        }
    }
}
