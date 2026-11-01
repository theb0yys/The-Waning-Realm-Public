using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace TAOM_CheatPanel.UI.Tabs
{
    public sealed class TimeWeatherTabVM : ViewModel
    {
        private string _currentDate   = "";
        private string _currentSeason = "";
        private string _currentSpeed  = "";
        private string _statusText    = "";

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        public TimeWeatherTabVM()
        {
            RefreshCurrentInfo();
        }

        // ── Bound properties ──────────────────────────────────────────────

        [DataSourceProperty]
        public string CurrentDate
        {
            get { return _currentDate; }
            set { if (_currentDate == value) return; _currentDate = value; OnPropertyChangedWithValue(value, nameof(CurrentDate)); }
        }

        [DataSourceProperty]
        public string CurrentSeason
        {
            get { return _currentSeason; }
            set { if (_currentSeason == value) return; _currentSeason = value; OnPropertyChangedWithValue(value, nameof(CurrentSeason)); }
        }

        [DataSourceProperty]
        public string CurrentSpeed
        {
            get { return _currentSpeed; }
            set { if (_currentSpeed == value) return; _currentSpeed = value; OnPropertyChangedWithValue(value, nameof(CurrentSpeed)); }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
        }

        // ── Commands — speed control ───────────────────────────────────────

        public void ExecuteSetSpeedStopped()    => SetSpeed(CampaignTimeControlMode.Stop,                   "Stopped");
        public void ExecuteSetSpeedNormal()     => SetSpeed(CampaignTimeControlMode.StoppablePlay,          "Normal");
        public void ExecuteSetSpeedFast()       => SetSpeed(CampaignTimeControlMode.FastForwardStop,        "Fast");
        public void ExecuteSetSpeedVeryFast()   => SetSpeed(CampaignTimeControlMode.UnstoppableFastForward, "Very Fast");

        // ── Commands — time advance ────────────────────────────────────────

        /// <summary>Advance campaign time by N days using CampaignTime arithmetic.</summary>
        public void ExecuteAdvance1Day()  => AdvanceDays(1);
        public void ExecuteAdvance7Days() => AdvanceDays(7);
        public void ExecuteAdvance30Days()=> AdvanceDays(30);

        public void ExecuteRefresh() => RefreshCurrentInfo();

        // ── Helpers ───────────────────────────────────────────────────────

        private void SetSpeed(CampaignTimeControlMode mode, string label)
        {
            try
            {
                Campaign.Current.TimeControlMode = mode;
                var msg = $"Campaign speed set to: {label}";
                StatusText = msg;
                CurrentSpeed = label;
                TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg);
                TAOM_CheatPanel.Runtime.Log.Info("[TimeWeather] " + msg);
            }
            catch (Exception ex)
            {
                StatusText = "Error setting speed: " + ex.Message;
                TAOM_CheatPanel.Runtime.Log.ErrorOnce("time.speed." + mode, ex);
            }
        }

        private void AdvanceDays(int days)
        {
            TAOM_CheatPanel.UI.CheatPanelHost.RequestConfirmation(
                $"Advance campaign time by {days} day(s)? This cannot be undone.",
                () =>
                {
                    try
                    {
                        // CampaignTime.Days returns a CampaignTime duration.
                        // We add it to the campaign's current time via the internal tracker.
                        // Bannerlord doesn't expose a public "advance time" API, so we use
                        // the campaign's MapTimeTracker field via reflection.
                        var trackerField = typeof(Campaign).GetField(
                            "_mapTimeTracker",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                        );

                        if (trackerField != null)
                        {
                            var tracker = trackerField.GetValue(Campaign.Current);
                            var tickMethod = tracker?.GetType().GetMethod(
                                "Tick",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                            );
                            // Tick uses float seconds; 1 day = 24 in-game hours = CampaignTime.TicksPerDay ticks
                            // We call Campaign tick in a loop for N days if direct field unavailable.
                            // Fallback: inform player to use very-fast mode and wait.
                            if (tickMethod != null)
                            {
                                float secondsPerDay = (float)CampaignTime.Days(1).ToSeconds;
                                for (int i = 0; i < days; i++)
                                    tickMethod.Invoke(tracker, new object[] { secondsPerDay });

                                var msg = $"Advanced time by {days} day(s).";
                                StatusText = msg;
                                TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg);
                                RefreshCurrentInfo();
                                return;
                            }
                        }

                        // Reflection path unavailable — graceful fallback
                        StatusText = "Time advance requires internal API access. Use Very Fast speed to advance manually.";
                    }
                    catch (Exception ex)
                    {
                        StatusText = "Error advancing time: " + ex.Message;
                        TAOM_CheatPanel.Runtime.Log.ErrorOnce("time.advance." + days, ex);
                    }
                }
            );
        }

        private void RefreshCurrentInfo()
        {
            try
            {
                var now = CampaignTime.Now;
                CurrentDate   = $"Day {(int)now.ToDays} of Year {now.GetYear}";
                CurrentSeason = SeasonNames[(int)now.GetSeasonOfYear];

                var mode = Campaign.Current.TimeControlMode;
                CurrentSpeed = mode == CampaignTimeControlMode.Stop                   ? "Stopped"
                             : mode == CampaignTimeControlMode.StoppablePlay          ? "Normal"
                             : mode == CampaignTimeControlMode.UnstoppablePlay        ? "Normal (Unstoppable)"
                             : mode == CampaignTimeControlMode.FastForwardStop        ? "Fast"
                             : mode == CampaignTimeControlMode.UnstoppableFastForward ? "Very Fast"
                             : mode.ToString();
            }
            catch (Exception ex)
            {
                CurrentDate = CurrentSeason = CurrentSpeed = "N/A";
                TAOM_CheatPanel.Runtime.Log.ErrorOnce("time.refresh", ex);
            }
        }
    }
}
