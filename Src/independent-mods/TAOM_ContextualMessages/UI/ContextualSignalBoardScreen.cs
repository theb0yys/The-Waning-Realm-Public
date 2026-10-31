using System;
using ContextualMessages.UI.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace ContextualMessages.UI
{
    public sealed class ContextualSignalBoardScreen : ScreenBase
    {
        private static ContextualSignalBoardScreen _activeScreen;

        private readonly ContextualMessageBehavior _behavior;
        private GauntletLayer _layer;
        private GauntletMovieIdentifier _movie;
        private ContextualSignalBoardVM _vm;

        private ContextualSignalBoardScreen(ContextualMessageBehavior behavior)
        {
            _behavior = behavior;
        }

        public static bool TryOpen(ContextualMessageBehavior behavior)
        {
            if (behavior == null)
                return false;

            if (_activeScreen != null)
            {
                if (_activeScreen._vm != null)
                    _activeScreen._vm.Refresh();
                return true;
            }

            try
            {
                ScreenManager.PushScreen(new ContextualSignalBoardScreen(behavior));
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print("ContextualMessages: failed to push signal board: " + ex.Message,
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
                _vm = new ContextualSignalBoardVM(_behavior, Close);
                _layer = new GauntletLayer("Vanilla_ContextualMessages", 207, false);
                _layer.IsFocusLayer = true;
                _layer.InputRestrictions.SetInputRestrictions();
                _movie = _layer.LoadMovie("Vanilla_ContextualMessages", _vm);

                AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch (Exception ex)
            {
                Debug.Print("ContextualMessages: signal board initialize failed: " + ex.Message,
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
                    if (_layer != null)
                        _layer.ReleaseMovie(_movie);
                    _movie = null;
                }

                if (_vm != null)
                    _vm.OnFinalize();
            }
            catch (Exception ex)
            {
                Debug.Print("ContextualMessages: signal board finalize error: " + ex.Message,
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
