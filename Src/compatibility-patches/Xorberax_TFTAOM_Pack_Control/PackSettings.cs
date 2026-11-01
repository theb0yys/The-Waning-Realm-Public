using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace XorberaxTFTAOMPackControl
{
    public sealed class PackSettings : AttributeGlobalSettings<PackSettings>
    {
        private static readonly PackSettings Default = new PackSettings();

        public static PackSettings Current => Instance ?? Default;

        public override string Id => "Xorberax_TFTAOM_Experience";
        public override string DisplayName => "Xorberax TFTAOM Experience";
        public override string FolderName => "Xorberax_TFTAOM_Experience";
        public override string FormatType => "json2";
        public override int UIVersion => 2;

        private bool _banksUnsafeEnabled;
        private bool _cutThroughEnabled = true;
        private bool _deadlyCombatEnabled = true;
        private bool _deadlyHorseChargesEnabled = true;
        private bool _killFeedDisablerEnabled = true;
        private bool _scholarsEnabled = true;
        private bool _shoulderCamEnabled = true;
        private bool _textToSpeechEnabled = true;
        private bool _trainingFieldEnabled = true;
        private bool _workEnabled = true;
        private bool _yellEnabled = true;

        private float _cutDamageRetained = 0.85f;
        private float _cutArmorThreshold = 0.8f;
        private bool _cutOnlyKilledUnits;
        private bool _cutFriendlyUnitsBlock;
        private bool _cutOnlyPlayer;
        private float _horseChargeMultiplier = 5f;
        private float _deadlyCombatKillThreshold = 0.3f;
        private float _deadlyCombatBleedThreshold = 0.75f;
        private float _deadlyCombatSpeedReduction = 0.05f;
        private bool _deadlyCombatAutoReload;
        private bool _enableGeneralKillFeed;
        private bool _enablePersonalKillFeed;

        private int _bankMinimumRenown = 5;
        private int _bankLoanAmountPerRenown = 200;
        private int _bankRepaymentDays = 30;
        private int _bankOpeningCost = 3000;
        private int _bankMaxBalance = 100000;
        private int _trainingFieldXpPerHour = 10;
        private int _trainingArenaXpPerHour = 50;
        private int _trainingArenaCostPerUnit = 15;
        private int _trainingMaxHours = 24;
        private int _trainingCooldownHours = 72;
        private bool _trainingWounding = true;
        private float _trainingFieldWoundChance = 0.2f;
        private float _trainingArenaWoundChance = 0.02f;
        private int _trainingMaxSkill = 275;
        private bool _trainingCombatSkillScaling = true;
        private int _workShiftHours = 8;
        private int _workCooldownHours = 24;
        private int _workPaymentLimit = 500;
        private int _workAthleticsXp = 50;
        private int _workRelationGain = 1;
        private float _workGiftChance = 0.1f;
        private int _workMaxGiftQuantity = 15;
        private float _scholarAppearanceChance = 0.25f;
        private int _noviceScholarXp = 100;
        private int _intermediateScholarXp = 250;
        private int _advancedScholarXp = 500;
        private int _expertScholarXp = 1000;

        private float _shoulderFov = 65f;
        private float _shoulderOnFootX = 0.35f;
        private float _shoulderOnFootZ = -0.5f;
        private float _shoulderMountedX = 0.35f;
        private float _shoulderMountedZ = -0.5f;
        private bool _shoulderLiveUpdates;
        private float _shoulderMaxShake = 1f;
        private float _ttsSpeechRate = 1f;
        private bool _ttsDeveloperMode;

        [SettingPropertyGroup("Module Toggles", GroupOrder = 0)]
        [SettingPropertyBool("Banks", RequireRestart = true, Order = 0, HintText = "Disabled by default for TAOM on Bannerlord 1.4.5. The original Banks behavior creates synthetic heroes and clans and can freeze campaign startup.")]
        public bool BanksUnsafeEnabled { get => _banksUnsafeEnabled; set => Set(ref _banksUnsafeEnabled, value, nameof(BanksUnsafeEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Cut Through Everyone", RequireRestart = true, Order = 1, HintText = "Skips Cut Through Everyone when disabled. Restart required.")]
        public bool CutThroughEveryoneEnabled { get => _cutThroughEnabled; set => Set(ref _cutThroughEnabled, value, nameof(CutThroughEveryoneEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Deadly Combat", RequireRestart = true, Order = 2)]
        public bool DeadlyCombatEnabled { get => _deadlyCombatEnabled; set => Set(ref _deadlyCombatEnabled, value, nameof(DeadlyCombatEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Deadly Horse Charges", RequireRestart = true, Order = 3)]
        public bool DeadlyHorseChargesEnabled { get => _deadlyHorseChargesEnabled; set => Set(ref _deadlyHorseChargesEnabled, value, nameof(DeadlyHorseChargesEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Kill Feed Disabler", RequireRestart = true, Order = 4)]
        public bool KillFeedDisablerEnabled { get => _killFeedDisablerEnabled; set => Set(ref _killFeedDisablerEnabled, value, nameof(KillFeedDisablerEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Scholars", RequireRestart = true, Order = 5)]
        public bool ScholarsEnabled { get => _scholarsEnabled; set => Set(ref _scholarsEnabled, value, nameof(ScholarsEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Shoulder Cam", RequireRestart = true, Order = 6)]
        public bool ShoulderCamEnabled { get => _shoulderCamEnabled; set => Set(ref _shoulderCamEnabled, value, nameof(ShoulderCamEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Text To Speech", RequireRestart = true, Order = 7)]
        public bool TextToSpeechEnabled { get => _textToSpeechEnabled; set => Set(ref _textToSpeechEnabled, value, nameof(TextToSpeechEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Training Field", RequireRestart = true, Order = 8)]
        public bool TrainingFieldEnabled { get => _trainingFieldEnabled; set => Set(ref _trainingFieldEnabled, value, nameof(TrainingFieldEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Work", RequireRestart = true, Order = 9)]
        public bool WorkEnabled { get => _workEnabled; set => Set(ref _workEnabled, value, nameof(WorkEnabled)); }

        [SettingPropertyGroup("Module Toggles")]
        [SettingPropertyBool("Yell", RequireRestart = true, Order = 10)]
        public bool YellEnabled { get => _yellEnabled; set => Set(ref _yellEnabled, value, nameof(YellEnabled)); }

        [SettingPropertyGroup("Combat", GroupOrder = 1)]
        [SettingPropertyFloatingInteger("Cut damage retained per target", 0f, 1f, "0.00", Order = 0)]
        public float CutDamageRetained { get => _cutDamageRetained; set => SetConfig(ref _cutDamageRetained, value, nameof(CutDamageRetained)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyFloatingInteger("Cut armor threshold", 0f, 2f, "0.00", Order = 1)]
        public float CutArmorThreshold { get => _cutArmorThreshold; set => SetConfig(ref _cutArmorThreshold, value, nameof(CutArmorThreshold)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyBool("Cut only killed units", Order = 2)]
        public bool CutOnlyKilledUnits { get => _cutOnlyKilledUnits; set => SetConfig(ref _cutOnlyKilledUnits, value, nameof(CutOnlyKilledUnits)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyBool("Friendly units block cuts", Order = 3)]
        public bool CutFriendlyUnitsBlock { get => _cutFriendlyUnitsBlock; set => SetConfig(ref _cutFriendlyUnitsBlock, value, nameof(CutFriendlyUnitsBlock)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyBool("Only player can cut through", Order = 4)]
        public bool CutOnlyPlayer { get => _cutOnlyPlayer; set => SetConfig(ref _cutOnlyPlayer, value, nameof(CutOnlyPlayer)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyFloatingInteger("Horse charge damage multiplier", 0f, 20f, "0.0", Order = 5)]
        public float HorseChargeMultiplier { get => _horseChargeMultiplier; set => SetConfig(ref _horseChargeMultiplier, value, nameof(HorseChargeMultiplier)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyFloatingInteger("Deadly Combat kill threshold", 0f, 1f, "0.00", Order = 6)]
        public float DeadlyCombatKillThreshold { get => _deadlyCombatKillThreshold; set => SetConfig(ref _deadlyCombatKillThreshold, value, nameof(DeadlyCombatKillThreshold)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyFloatingInteger("Bleedout health threshold", 0f, 1f, "0.00", Order = 7)]
        public float DeadlyCombatBleedThreshold { get => _deadlyCombatBleedThreshold; set => SetConfig(ref _deadlyCombatBleedThreshold, value, nameof(DeadlyCombatBleedThreshold)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyFloatingInteger("Bleedout speed reduction", 0f, 1f, "0.00", Order = 8)]
        public float DeadlyCombatSpeedReduction { get => _deadlyCombatSpeedReduction; set => SetConfig(ref _deadlyCombatSpeedReduction, value, nameof(DeadlyCombatSpeedReduction)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyBool("Deadly Combat auto reload config", Order = 9)]
        public bool DeadlyCombatAutoReload { get => _deadlyCombatAutoReload; set => SetConfig(ref _deadlyCombatAutoReload, value, nameof(DeadlyCombatAutoReload)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyBool("Enable general kill feed", Order = 10)]
        public bool EnableGeneralKillFeed { get => _enableGeneralKillFeed; set => SetConfig(ref _enableGeneralKillFeed, value, nameof(EnableGeneralKillFeed)); }

        [SettingPropertyGroup("Combat")]
        [SettingPropertyBool("Enable personal kill feed", Order = 11)]
        public bool EnablePersonalKillFeed { get => _enablePersonalKillFeed; set => SetConfig(ref _enablePersonalKillFeed, value, nameof(EnablePersonalKillFeed)); }

        [SettingPropertyGroup("Campaign Economy", GroupOrder = 2)]
        [SettingPropertyInteger("Minimum renown for bank loan", 0, 1000, "0", Order = 0)]
        public int BankMinimumRenown { get => _bankMinimumRenown; set => SetConfig(ref _bankMinimumRenown, value, nameof(BankMinimumRenown)); }

        [SettingPropertyGroup("Campaign Economy")]
        [SettingPropertyInteger("Loan amount per renown", 0, 10000, "0", Order = 1)]
        public int BankLoanAmountPerRenown { get => _bankLoanAmountPerRenown; set => SetConfig(ref _bankLoanAmountPerRenown, value, nameof(BankLoanAmountPerRenown)); }

        [SettingPropertyGroup("Campaign Economy")]
        [SettingPropertyInteger("Loan repayment days", 1, 365, "0", Order = 2)]
        public int BankRepaymentDays { get => _bankRepaymentDays; set => SetConfig(ref _bankRepaymentDays, value, nameof(BankRepaymentDays)); }

        [SettingPropertyGroup("Campaign Economy")]
        [SettingPropertyInteger("Bank account opening base cost", 0, 100000, "0", Order = 3)]
        public int BankOpeningCost { get => _bankOpeningCost; set => SetConfig(ref _bankOpeningCost, value, nameof(BankOpeningCost)); }

        [SettingPropertyGroup("Campaign Economy")]
        [SettingPropertyInteger("Maximum bank balance", 0, 5000000, "0", Order = 4)]
        public int BankMaxBalance { get => _bankMaxBalance; set => SetConfig(ref _bankMaxBalance, value, nameof(BankMaxBalance)); }

        [SettingPropertyGroup("Training and Work", GroupOrder = 3)]
        [SettingPropertyInteger("Training field XP per hour", 0, 5000, "0", Order = 0)]
        public int TrainingFieldXpPerHour { get => _trainingFieldXpPerHour; set => SetConfig(ref _trainingFieldXpPerHour, value, nameof(TrainingFieldXpPerHour)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Arena XP per hour", 0, 5000, "0", Order = 1)]
        public int TrainingArenaXpPerHour { get => _trainingArenaXpPerHour; set => SetConfig(ref _trainingArenaXpPerHour, value, nameof(TrainingArenaXpPerHour)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Arena cost per unit", 0, 10000, "0", Order = 2)]
        public int TrainingArenaCostPerUnit { get => _trainingArenaCostPerUnit; set => SetConfig(ref _trainingArenaCostPerUnit, value, nameof(TrainingArenaCostPerUnit)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Maximum training hours", 1, 240, "0", Order = 3)]
        public int TrainingMaxHours { get => _trainingMaxHours; set => SetConfig(ref _trainingMaxHours, value, nameof(TrainingMaxHours)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Training cooldown hours", 0, 720, "0", Order = 4)]
        public int TrainingCooldownHours { get => _trainingCooldownHours; set => SetConfig(ref _trainingCooldownHours, value, nameof(TrainingCooldownHours)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyBool("Enable training wounds", Order = 5)]
        public bool TrainingWounding { get => _trainingWounding; set => SetConfig(ref _trainingWounding, value, nameof(TrainingWounding)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyFloatingInteger("Training field wound chance", 0f, 1f, "0.00", Order = 6)]
        public float TrainingFieldWoundChance { get => _trainingFieldWoundChance; set => SetConfig(ref _trainingFieldWoundChance, value, nameof(TrainingFieldWoundChance)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyFloatingInteger("Arena wound chance", 0f, 1f, "0.00", Order = 7)]
        public float TrainingArenaWoundChance { get => _trainingArenaWoundChance; set => SetConfig(ref _trainingArenaWoundChance, value, nameof(TrainingArenaWoundChance)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Training max skill", 1, 500, "0", Order = 8)]
        public int TrainingMaxSkill { get => _trainingMaxSkill; set => SetConfig(ref _trainingMaxSkill, value, nameof(TrainingMaxSkill)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyBool("Combat skills affect training XP", Order = 9)]
        public bool TrainingCombatSkillScaling { get => _trainingCombatSkillScaling; set => SetConfig(ref _trainingCombatSkillScaling, value, nameof(TrainingCombatSkillScaling)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Work shift hours", 1, 24, "0", Order = 10)]
        public int WorkShiftHours { get => _workShiftHours; set => SetConfig(ref _workShiftHours, value, nameof(WorkShiftHours)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Work cooldown hours", 0, 720, "0", Order = 11)]
        public int WorkCooldownHours { get => _workCooldownHours; set => SetConfig(ref _workCooldownHours, value, nameof(WorkCooldownHours)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Work payment limit", 0, 50000, "0", Order = 12)]
        public int WorkPaymentLimit { get => _workPaymentLimit; set => SetConfig(ref _workPaymentLimit, value, nameof(WorkPaymentLimit)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Work athletics XP", 0, 5000, "0", Order = 13)]
        public int WorkAthleticsXp { get => _workAthleticsXp; set => SetConfig(ref _workAthleticsXp, value, nameof(WorkAthleticsXp)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Work notable relation gain", -20, 20, "0", Order = 14)]
        public int WorkRelationGain { get => _workRelationGain; set => SetConfig(ref _workRelationGain, value, nameof(WorkRelationGain)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyFloatingInteger("Work gift chance", 0f, 1f, "0.00", Order = 15)]
        public float WorkGiftChance { get => _workGiftChance; set => SetConfig(ref _workGiftChance, value, nameof(WorkGiftChance)); }

        [SettingPropertyGroup("Training and Work")]
        [SettingPropertyInteger("Work max gift quantity", 0, 1000, "0", Order = 16)]
        public int WorkMaxGiftQuantity { get => _workMaxGiftQuantity; set => SetConfig(ref _workMaxGiftQuantity, value, nameof(WorkMaxGiftQuantity)); }

        [SettingPropertyGroup("Scholars", GroupOrder = 4)]
        [SettingPropertyFloatingInteger("Scholar appearance chance", 0f, 1f, "0.00", Order = 0)]
        public float ScholarAppearanceChance { get => _scholarAppearanceChance; set => SetConfig(ref _scholarAppearanceChance, value, nameof(ScholarAppearanceChance)); }

        [SettingPropertyGroup("Scholars")]
        [SettingPropertyInteger("Novice lecture XP", 0, 10000, "0", Order = 1)]
        public int NoviceScholarXp { get => _noviceScholarXp; set => SetConfig(ref _noviceScholarXp, value, nameof(NoviceScholarXp)); }

        [SettingPropertyGroup("Scholars")]
        [SettingPropertyInteger("Intermediate lecture XP", 0, 10000, "0", Order = 2)]
        public int IntermediateScholarXp { get => _intermediateScholarXp; set => SetConfig(ref _intermediateScholarXp, value, nameof(IntermediateScholarXp)); }

        [SettingPropertyGroup("Scholars")]
        [SettingPropertyInteger("Advanced lecture XP", 0, 10000, "0", Order = 3)]
        public int AdvancedScholarXp { get => _advancedScholarXp; set => SetConfig(ref _advancedScholarXp, value, nameof(AdvancedScholarXp)); }

        [SettingPropertyGroup("Scholars")]
        [SettingPropertyInteger("Expert lecture XP", 0, 20000, "0", Order = 4)]
        public int ExpertScholarXp { get => _expertScholarXp; set => SetConfig(ref _expertScholarXp, value, nameof(ExpertScholarXp)); }

        [SettingPropertyGroup("Camera and Utility", GroupOrder = 5)]
        [SettingPropertyFloatingInteger("Shoulder cam FOV", 30f, 120f, "0", Order = 0)]
        public float ShoulderFov { get => _shoulderFov; set => SetConfig(ref _shoulderFov, value, nameof(ShoulderFov)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyFloatingInteger("On-foot X offset", -2f, 2f, "0.00", Order = 1)]
        public float ShoulderOnFootX { get => _shoulderOnFootX; set => SetConfig(ref _shoulderOnFootX, value, nameof(ShoulderOnFootX)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyFloatingInteger("On-foot Z offset", -2f, 2f, "0.00", Order = 2)]
        public float ShoulderOnFootZ { get => _shoulderOnFootZ; set => SetConfig(ref _shoulderOnFootZ, value, nameof(ShoulderOnFootZ)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyFloatingInteger("Mounted X offset", -2f, 2f, "0.00", Order = 3)]
        public float ShoulderMountedX { get => _shoulderMountedX; set => SetConfig(ref _shoulderMountedX, value, nameof(ShoulderMountedX)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyFloatingInteger("Mounted Z offset", -2f, 2f, "0.00", Order = 4)]
        public float ShoulderMountedZ { get => _shoulderMountedZ; set => SetConfig(ref _shoulderMountedZ, value, nameof(ShoulderMountedZ)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyBool("Shoulder cam live config updates", Order = 5)]
        public bool ShoulderLiveUpdates { get => _shoulderLiveUpdates; set => SetConfig(ref _shoulderLiveUpdates, value, nameof(ShoulderLiveUpdates)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyFloatingInteger("Maximum camera shake", 0f, 5f, "0.00", Order = 6)]
        public float ShoulderMaxShake { get => _shoulderMaxShake; set => SetConfig(ref _shoulderMaxShake, value, nameof(ShoulderMaxShake)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyInteger("Text-to-speech rate", -10, 10, "0", Order = 7)]
        public int TextToSpeechRate { get => (int)_ttsSpeechRate; set => SetConfig(ref _ttsSpeechRate, value, nameof(TextToSpeechRate)); }

        [SettingPropertyGroup("Camera and Utility")]
        [SettingPropertyBool("Text-to-speech developer mode", Order = 8)]
        public bool TextToSpeechDeveloperMode { get => _ttsDeveloperMode; set => SetConfig(ref _ttsDeveloperMode, value, nameof(TextToSpeechDeveloperMode)); }

        private bool Set<T>(ref T field, T value, string name)
        {
            if (Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void SetConfig<T>(ref T field, T value, string name)
        {
            if (Set(ref field, value, name))
                ConfigBridge.ApplyAll(this);
        }
    }
}
