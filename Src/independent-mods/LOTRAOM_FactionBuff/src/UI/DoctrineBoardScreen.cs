using System;
using LOTRAOM.FactionBuff.Behaviors;
using LOTRAOM.FactionBuff.UI.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace LOTRAOM.FactionBuff.UI
{
    public sealed class DoctrineBoardScreen : ScreenBase
    {
        private static DoctrineBoardScreen? _activeScreen;

        private readonly FactionBuffMessageBehavior _behavior;
        private GauntletLayer? _layer;
        private GauntletMovieIdentifier? _movie;
        private DoctrineBoardVM? _vm;

        private DoctrineBoardScreen(FactionBuffMessageBehavior behavior)
        {
            _behavior = behavior;
        }

        public static bool TryOpen(FactionBuffMessageBehavior behavior)
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
                ScreenManager.PushScreen(new DoctrineBoardScreen(behavior));
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print("LOTRAOM_FactionBuff: failed to push doctrine board: " + ex.Message,
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
                _vm = new DoctrineBoardVM(_behavior, Close);
                _layer = new GauntletLayer("LOTRAOM_FactionBuff", 207, false);
                _layer.IsFocusLayer = true;
                _layer.InputRestrictions.SetInputRestrictions();
                _movie = _layer.LoadMovie("LOTRAOM_FactionBuff", _vm);

                AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch (Exception ex)
            {
                Debug.Print("LOTRAOM_FactionBuff: doctrine board initialize failed: " + ex.Message,
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
                Debug.Print("LOTRAOM_FactionBuff: doctrine board finalize error: " + ex.Message,
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
