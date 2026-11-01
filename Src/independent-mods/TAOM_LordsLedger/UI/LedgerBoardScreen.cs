using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TAOM_LordsLedger.UI.ViewModels;

namespace TAOM_LordsLedger.UI
{
    public sealed class LedgerBoardScreen : ScreenBase
    {
        private static LedgerBoardScreen _activeScreen;

        private readonly LedgerCampaignBehavior _behavior;
        private GauntletLayer _layer;
        private GauntletMovieIdentifier _movie;
        private LedgerBoardVM _vm;

        private LedgerBoardScreen(LedgerCampaignBehavior behavior)
        {
            _behavior = behavior;
        }

        public static bool TryOpen(LedgerCampaignBehavior behavior)
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
                ScreenManager.PushScreen(new LedgerBoardScreen(behavior));
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print("TAOM_LordsLedger: failed to push board: " + ex.Message,
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
                _vm = new LedgerBoardVM(_behavior, Close);
                _layer = new GauntletLayer("TAOM_LordsLedger", 205, false);
                _layer.IsFocusLayer = true;
                _layer.InputRestrictions.SetInputRestrictions();
                _movie = _layer.LoadMovie("TAOM_LordsLedger", _vm);

                AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch (Exception ex)
            {
                Debug.Print("TAOM_LordsLedger: board initialize failed: " + ex.Message,
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
                Debug.Print("TAOM_LordsLedger: board finalize error: " + ex.Message,
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
