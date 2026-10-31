using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TroopLimitExpander.UI
{
    public sealed class TroopLimitBattleOverlayVM : ViewModel
    {
        private string _titleText = "TROOP LIMIT";
        private string _playerTotalText = "-";
        private string _playerActiveText = "-";
        private string _playerDeadText = "-";
        private string _playerReserveText = "-";
        private string _playerLeftText = "-";
        private string _playerWaveText = "-";
        private string _enemyTotalText = "-";
        private string _enemyActiveText = "-";
        private string _enemyDeadText = "-";
        private string _enemyReserveText = "-";
        private string _enemyLeftText = "-";
        private string _enemyWaveText = "-";
        private string _totalUnitsText = "-";
        private string _missionDeathsText = "-";
        private string _playerKillsText = "-";
        private string _settingsText = "-";
        private string _waveRulesText = "-";
        private string _activeCapText = "-";
        private bool _isPanelVisible;

        [DataSourceProperty]
        public string TitleText
        {
            get { return _titleText; }
            set { SetField(ref _titleText, value, nameof(TitleText)); }
        }

        [DataSourceProperty]
        public string PlayerTotalText
        {
            get { return _playerTotalText; }
            set { SetField(ref _playerTotalText, value, nameof(PlayerTotalText)); }
        }

        [DataSourceProperty]
        public string PlayerActiveText
        {
            get { return _playerActiveText; }
            set { SetField(ref _playerActiveText, value, nameof(PlayerActiveText)); }
        }

        [DataSourceProperty]
        public string PlayerDeadText
        {
            get { return _playerDeadText; }
            set { SetField(ref _playerDeadText, value, nameof(PlayerDeadText)); }
        }

        [DataSourceProperty]
        public string PlayerReserveText
        {
            get { return _playerReserveText; }
            set { SetField(ref _playerReserveText, value, nameof(PlayerReserveText)); }
        }

        [DataSourceProperty]
        public string PlayerLeftText
        {
            get { return _playerLeftText; }
            set { SetField(ref _playerLeftText, value, nameof(PlayerLeftText)); }
        }

        [DataSourceProperty]
        public string PlayerWaveText
        {
            get { return _playerWaveText; }
            set { SetField(ref _playerWaveText, value, nameof(PlayerWaveText)); }
        }

        [DataSourceProperty]
        public string EnemyTotalText
        {
            get { return _enemyTotalText; }
            set { SetField(ref _enemyTotalText, value, nameof(EnemyTotalText)); }
        }

        [DataSourceProperty]
        public string EnemyActiveText
        {
            get { return _enemyActiveText; }
            set { SetField(ref _enemyActiveText, value, nameof(EnemyActiveText)); }
        }

        [DataSourceProperty]
        public string EnemyDeadText
        {
            get { return _enemyDeadText; }
            set { SetField(ref _enemyDeadText, value, nameof(EnemyDeadText)); }
        }

        [DataSourceProperty]
        public string EnemyReserveText
        {
            get { return _enemyReserveText; }
            set { SetField(ref _enemyReserveText, value, nameof(EnemyReserveText)); }
        }

        [DataSourceProperty]
        public string EnemyLeftText
        {
            get { return _enemyLeftText; }
            set { SetField(ref _enemyLeftText, value, nameof(EnemyLeftText)); }
        }

        [DataSourceProperty]
        public string EnemyWaveText
        {
            get { return _enemyWaveText; }
            set { SetField(ref _enemyWaveText, value, nameof(EnemyWaveText)); }
        }

        [DataSourceProperty]
        public string TotalUnitsText
        {
            get { return _totalUnitsText; }
            set { SetField(ref _totalUnitsText, value, nameof(TotalUnitsText)); }
        }

        [DataSourceProperty]
        public string MissionDeathsText
        {
            get { return _missionDeathsText; }
            set { SetField(ref _missionDeathsText, value, nameof(MissionDeathsText)); }
        }

        [DataSourceProperty]
        public string PlayerKillsText
        {
            get { return _playerKillsText; }
            set { SetField(ref _playerKillsText, value, nameof(PlayerKillsText)); }
        }

        [DataSourceProperty]
        public string SettingsText
        {
            get { return _settingsText; }
            set { SetField(ref _settingsText, value, nameof(SettingsText)); }
        }

        [DataSourceProperty]
        public string WaveRulesText
        {
            get { return _waveRulesText; }
            set { SetField(ref _waveRulesText, value, nameof(WaveRulesText)); }
        }

        [DataSourceProperty]
        public string ActiveCapText
        {
            get { return _activeCapText; }
            set { SetField(ref _activeCapText, value, nameof(ActiveCapText)); }
        }

        [DataSourceProperty]
        public bool IsPanelVisible
        {
            get { return _isPanelVisible; }
            set { SetField(ref _isPanelVisible, value, nameof(IsPanelVisible)); }
        }

        public void Refresh()
        {
            try
            {
                RefreshCore();
            }
            catch (Exception ex)
            {
                TitleText = "TROOP LIMIT - UI ERROR";
                TotalUnitsText = "overlay refresh failed: " + ex.GetType().Name;
                MissionDeathsText = SafeMessage(ex.Message);
                PlayerKillsText = "-";
                SettingsText = "-";
                WaveRulesText = "-";
                ActiveCapText = "-";
                IsPanelVisible = true;
            }
        }

        private void RefreshCore()
        {
            Mission mission = Mission.Current;
            if (mission == null)
            {
                Clear();
                return;
            }

            BattleSideEnum playerSide = ResolvePlayerSide(mission);
            BattleSideEnum enemySide = playerSide == BattleSideEnum.Attacker
                ? BattleSideEnum.Defender
                : BattleSideEnum.Attacker;

            DefaultBattleMissionAgentSpawnLogic? spawnLogic =
                mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            SideSnapshot player = spawnLogic != null
                ? ReadSide(spawnLogic, playerSide)
                : ReadFallbackSide(mission, playerSide);
            SideSnapshot enemy = spawnLogic != null
                ? ReadSide(spawnLogic, enemySide)
                : ReadFallbackSide(mission, enemySide);

            TitleText = mission.IsSiegeBattle || mission.IsSallyOutBattle ? "TROOP LIMIT - SIEGE" : "TROOP LIMIT - BATTLE";
            PlayerTotalText = Format(player.Total);
            PlayerActiveText = Format(player.Active);
            PlayerDeadText = Format(player.Dead);
            PlayerReserveText = Format(player.Reserve);
            PlayerLeftText = Format(player.Left);
            PlayerWaveText = Format(player.WaveSize);
            EnemyTotalText = Format(enemy.Total);
            EnemyActiveText = Format(enemy.Active);
            EnemyDeadText = Format(enemy.Dead);
            EnemyReserveText = Format(enemy.Reserve);
            EnemyLeftText = Format(enemy.Left);
            EnemyWaveText = Format(enemy.WaveSize);
            TotalUnitsText = Format(SumKnown(player.Total, enemy.Total));
            MissionDeathsText = Format(SumKnown(player.Dead, enemy.Dead)) +
                                " (P " + Format(player.Dead) +
                                " | E " + Format(enemy.Dead) + ")";
            PlayerKillsText = Format(SafePlayerKills(mission));
            SettingsText = "waves " + TroopLimitSettings.EffectiveLandReinforcementWaves +
                           " | batch " + TroopLimitSettings.EffectiveLandWaveBatchPercent + "%" +
                           " | interval " + TroopLimitSettings.EffectiveLandReinforcementIntervalSeconds + "s" +
                           " | active cap " + TroopLimitSettings.EffectiveLandActiveAgentSafetyCap;
            WaveRulesText = "waves " + TroopLimitSettings.EffectiveLandReinforcementWaves +
                            " | batch " + TroopLimitSettings.EffectiveLandWaveBatchPercent + "%" +
                            " | interval " + TroopLimitSettings.EffectiveLandReinforcementIntervalSeconds + "s";
            ActiveCapText = Format(TroopLimitSettings.EffectiveLandActiveAgentSafetyCap);
            IsPanelVisible = true;
        }

        private static BattleSideEnum ResolvePlayerSide(Mission mission)
        {
            try
            {
                if (mission.PlayerTeam != null)
                    return mission.PlayerTeam.Side;
            }
            catch
            {
            }

            return BattleSideEnum.Attacker;
        }

        private static SideSnapshot ReadSide(DefaultBattleMissionAgentSpawnLogic spawnLogic, BattleSideEnum side)
        {
            int active = side == BattleSideEnum.Attacker
                ? spawnLogic.NumberOfActiveAttackerTroops
                : spawnLogic.NumberOfActiveDefenderTroops;

            int reserve = side == BattleSideEnum.Attacker
                ? spawnLogic.NumberOfRemainingAttackerTroops
                : spawnLogic.NumberOfRemainingDefenderTroops;

            int total = SafeTotal(spawnLogic, side);
            int left = Math.Max(0, active) + Math.Max(0, reserve);
            int dead = total > 0 ? Math.Max(0, total - left) : -1;
            int wave = EstimateWaveSize(spawnLogic, side, total);

            return new SideSnapshot(total, active, reserve, left, dead, wave);
        }

        private static SideSnapshot ReadFallbackSide(Mission mission, BattleSideEnum side)
        {
            int active = 0;
            int dead = 0;

            try
            {
                foreach (Agent agent in mission.Agents)
                {
                    if (agent == null || agent.Team == null || agent.Team.Side != side || IsMountAgent(agent))
                        continue;

                    if (IsActiveAgent(agent))
                        active++;
                    else
                        dead++;
                }
            }
            catch
            {
            }

            int total = active + dead;
            return new SideSnapshot(
                total > 0 ? total : -1,
                active,
                -1,
                active,
                total > 0 ? dead : -1,
                -1);
        }

        private static int SafeTotal(DefaultBattleMissionAgentSpawnLogic spawnLogic, BattleSideEnum side)
        {
            try
            {
                return spawnLogic.GetTotalNumberOfTroopsForSide(side);
            }
            catch
            {
                return -1;
            }
        }

        private static int EstimateWaveSize(DefaultBattleMissionAgentSpawnLogic spawnLogic, BattleSideEnum side, int total)
        {
            try
            {
                if (total <= 0)
                    total = spawnLogic.GetTotalNumberOfTroopsForSide(side);
                if (total <= 0)
                    return -1;

                MissionSpawnSettings settings = spawnLogic.SpawnSettings;
                float batch = side == BattleSideEnum.Attacker
                    ? settings.AttackerReinforcementBatchPercentage
                    : settings.DefenderReinforcementBatchPercentage;

                if (batch <= 0f)
                    batch = settings.ReinforcementBatchPercentage;
                if (batch <= 0f)
                    batch = settings.ReinforcementWavePercentage;
                if (batch <= 0f)
                    return -1;

                float ratio = batch > 1f ? batch / 100f : batch;
                return Math.Max(1, (int)Math.Ceiling(total * ratio));
            }
            catch
            {
                return -1;
            }
        }

        private static int SafePlayerKills(Mission mission)
        {
            try
            {
                Agent mainAgent = mission.MainAgent;
                if (mainAgent != null)
                    return Math.Max(0, mainAgent.KillCount);
            }
            catch
            {
            }

            return -1;
        }

        private static bool IsMountAgent(Agent agent)
        {
            try
            {
                return agent.IsMount;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsActiveAgent(Agent agent)
        {
            try
            {
                return agent.IsActive();
            }
            catch
            {
                return true;
            }
        }

        private static int SumKnown(int first, int second)
        {
            bool hasValue = false;
            int total = 0;

            if (first >= 0)
            {
                total += first;
                hasValue = true;
            }

            if (second >= 0)
            {
                total += second;
                hasValue = true;
            }

            return hasValue ? total : -1;
        }

        private void Clear()
        {
            IsPanelVisible = false;
            PlayerTotalText = "-";
            PlayerActiveText = "-";
            PlayerDeadText = "-";
            PlayerReserveText = "-";
            PlayerLeftText = "-";
            PlayerWaveText = "-";
            EnemyTotalText = "-";
            EnemyActiveText = "-";
            EnemyDeadText = "-";
            EnemyReserveText = "-";
            EnemyLeftText = "-";
            EnemyWaveText = "-";
            TotalUnitsText = "-";
            MissionDeathsText = "-";
            PlayerKillsText = "-";
            SettingsText = "-";
            WaveRulesText = "-";
            ActiveCapText = "-";
        }

        private static string Format(int value)
        {
            return value >= 0 ? value.ToString("N0") : "-";
        }

        private static string SafeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "-";

            return message.Length <= 72 ? message : message.Substring(0, 72);
        }

        private readonly struct SideSnapshot
        {
            public SideSnapshot(int total, int active, int reserve, int left, int dead, int waveSize)
            {
                Total = total;
                Active = active;
                Reserve = reserve;
                Left = left;
                Dead = dead;
                WaveSize = waveSize;
            }

            public int Total { get; }
            public int Active { get; }
            public int Reserve { get; }
            public int Left { get; }
            public int Dead { get; }
            public int WaveSize { get; }
        }
    }
}
