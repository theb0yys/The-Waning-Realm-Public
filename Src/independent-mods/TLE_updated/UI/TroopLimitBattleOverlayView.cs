using System;
using System.IO;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TroopLimitExpander.UI
{
    public sealed class TroopLimitBattleOverlayView : MissionView
    {
        private const float RefreshInterval = 0.25f;
        private float _timeSinceLastRefresh;
        private GauntletLayer? _layer;
        private GauntletMovieIdentifier? _movie;
        private MissionScreen? _attachedScreen;
        private TroopLimitBattleOverlayVM? _viewModel;
        private bool _initializationFailed;
        private bool _screenMissingLogged;

        public override void AfterStart()
        {
            base.AfterStart();
            Log("AfterStart");
            TryInitializeLayer();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (!TroopLimitSettings.IsBattleOverlayEnabled)
            {
                if (_layer != null)
                    CleanupLayer();
                return;
            }

            if (_layer == null && !TryInitializeLayer())
                return;

            if (_viewModel == null)
                return;

            _timeSinceLastRefresh += dt;
            if (_timeSinceLastRefresh < RefreshInterval)
                return;

            _timeSinceLastRefresh = 0f;
            _viewModel.Refresh();
        }

        protected override void OnEndMission()
        {
            CleanupLayer();
            base.OnEndMission();
        }

        private bool TryInitializeLayer()
        {
            if (_initializationFailed)
                return false;

            if (!TroopLimitSettings.IsBattleOverlayEnabled)
                return false;

            if (_layer != null)
                return true;

            try
            {
                MissionScreen? missionScreen = ResolveMissionScreen();
                if (missionScreen == null)
                {
                    if (!_screenMissingLogged)
                    {
                        _screenMissingLogged = true;
                        Log("mission screen unavailable; topScreen=" + SafeTopScreenName());
                    }

                    return false;
                }

                _viewModel = new TroopLimitBattleOverlayVM();
                _layer = new GauntletLayer("TroopLimitExpanderBattleOverlay", 201, false);
                _movie = _layer.LoadMovie("TroopLimitBattleOverlay", _viewModel);
                missionScreen.AddLayer(_layer);
                _attachedScreen = missionScreen;
                _viewModel.Refresh();
                Log("attached movie TroopLimitBattleOverlay to " + missionScreen.GetType().FullName);
                return true;
            }
            catch (System.Exception ex)
            {
                _initializationFailed = true;
                CleanupLayer();
                Log("attach failed: " + ex.GetType().Name + ": " + ex.Message);
                InformationManager.DisplayMessage(new InformationMessage(
                    "[TroopLimitExpander] Battle overlay failed to load: " + ex.Message,
                    Color.FromUint(0xFF4500FF)));
                return false;
            }
        }

        private void CleanupLayer()
        {
            try
            {
                if (_layer != null && _movie != null)
                    _layer.ReleaseMovie(_movie);

                MissionScreen? missionScreen = _attachedScreen ?? ResolveMissionScreen();
                if (missionScreen != null && _layer != null)
                    missionScreen.RemoveLayer(_layer);
            }
            catch
            {
            }
            finally
            {
                _movie = null;
                _layer = null;
                _attachedScreen = null;
                _viewModel = null;
            }
        }

        private MissionScreen? ResolveMissionScreen()
        {
            if (MissionScreen != null)
                return MissionScreen;

            try
            {
                return ScreenManager.TopScreen as MissionScreen;
            }
            catch
            {
                return null;
            }
        }

        private static string SafeTopScreenName()
        {
            try
            {
                return ScreenManager.TopScreen?.GetType().FullName ?? "<null>";
            }
            catch (Exception ex)
            {
                return "<error " + ex.GetType().Name + ">";
            }
        }

        private static void Log(string message)
        {
            try
            {
                string logs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logs);
                File.AppendAllText(
                    Path.Combine(logs, "tle_overlay.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
