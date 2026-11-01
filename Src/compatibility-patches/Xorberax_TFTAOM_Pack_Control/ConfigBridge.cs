using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace XorberaxTFTAOMPackControl
{
    internal static class ConfigBridge
    {
        internal static void ApplyAll(PackSettings settings)
        {
            if (settings == null)
                return;

            try
            {
                Apply("XorberaxCutThroughEveryone", "onlyCutThroughWhenUnitIsKilled", settings.CutOnlyKilledUnits, "CutThroughEveryone.SubModule", "ShouldOnlyCutThroughKilledUnits");
                Apply("XorberaxCutThroughEveryone", "damageRetainedPerCut", settings.CutDamageRetained, "CutThroughEveryone.SubModule", "DamageRetainedPerCut");
                Apply("XorberaxCutThroughEveryone", "percentageOfInflictedDamageRequiredToCutThroughArmor", settings.CutArmorThreshold, "CutThroughEveryone.SubModule", "PercentageOfInflictedDamageRequiredToCutThroughArmor");
                Apply("XorberaxCutThroughEveryone", "doFriendlyUnitsBlockCutThroughs", settings.CutFriendlyUnitsBlock, "CutThroughEveryone.SubModule", "DoFriendlyUnitsBlockCutThroughs");
                Apply("XorberaxCutThroughEveryone", "onlyPlayerCanCutThrough", settings.CutOnlyPlayer, "CutThroughEveryone.SubModule", "OnlyPlayerCanCutThrough");

                Apply("XorberaxDeadlyHorseCharges", "chargeDamageMultiplier", settings.HorseChargeMultiplier, "DeadlyHorseCharges.SubModule", "ChargeDamageMultiplier");

                Apply("XorberaxDeadlyCombat", "percentageOfDamageRequiredToKillUnit", settings.DeadlyCombatKillThreshold, "DeadlyCombat.SubModule", "PercentageOfDamageRequiredToKillUnit");
                Apply("XorberaxDeadlyCombat", "unitHealthPercentageToCauseBleedout", settings.DeadlyCombatBleedThreshold, "DeadlyCombat.SubModule", "UnitHealthPercentageToCauseBleedout");
                Apply("XorberaxDeadlyCombat", "unitSpeedReductionRateDuringBleedout", settings.DeadlyCombatSpeedReduction, "DeadlyCombat.SubModule", "UnitSpeedReductionRateDuringBleedout");
                Apply("XorberaxDeadlyCombat", "shouldAutoReloadConfig", settings.DeadlyCombatAutoReload, "DeadlyCombat.SubModule", "ShouldAutoReloadConfig");

                Apply("XorberaxKillFeedDisabler", "enableGeneralKillFeed", settings.EnableGeneralKillFeed, "KillFeedDisabler.SubModule", "EnableGeneralKillFeed");
                Apply("XorberaxKillFeedDisabler", "enablePersonalKillFeed", settings.EnablePersonalKillFeed, "KillFeedDisabler.SubModule", "EnablePersonalKillFeed");

                Apply("XorberaxBanks", "minimumRenownRequiredToTakeOutLoan", settings.BankMinimumRenown, "Banks.SubModule", "MinimumRenownRequiredToTakeOutLoan");
                Apply("XorberaxBanks", "availableLoanAmountPerRenown", settings.BankLoanAmountPerRenown, "Banks.SubModule", "AvailableLoanAmountPerRenown");
                Apply("XorberaxBanks", "daysToRepayLoan", settings.BankRepaymentDays, "Banks.SubModule", "DaysToRepayLoan");
                Apply("XorberaxBanks", "bankAccountOpeningCostBase", settings.BankOpeningCost, "Banks.SubModule", "BankAccountOpeningCostBase");
                Apply("XorberaxBanks", "maxBankBalance", settings.BankMaxBalance, "Banks.SubModule", "MaxBankBalance");

                Apply("XorberaxTrainingField", "experiencePerHour", settings.TrainingFieldXpPerHour, "TrainingField.SubModule", "BaseExperiencePerHour");
                Apply("XorberaxTrainingField", "experiencePerHourAtArenas", settings.TrainingArenaXpPerHour, "TrainingField.SubModule", "BaseExperiencePerHourAtArenas");
                Apply("XorberaxTrainingField", "perUnitTrainingCostAtArenas", settings.TrainingArenaCostPerUnit, "TrainingField.SubModule", "PerUnitTrainingCostAtArenas");
                Apply("XorberaxTrainingField", "maximumHoursToTrain", settings.TrainingMaxHours, "TrainingField.SubModule", "MaximumHoursToTrain");
                Apply("XorberaxTrainingField", "trainingCooldownHours", settings.TrainingCooldownHours, "TrainingField.SubModule", "TrainingCooldownHours");
                Apply("XorberaxTrainingField", "enableWoundingDuringTraining", settings.TrainingWounding, "TrainingField.SubModule", "ShouldWoundDuringTraining");
                Apply("XorberaxTrainingField", "woundProbabilityAtTrainingField", settings.TrainingFieldWoundChance, "TrainingField.SubModule", "WoundProbabilityAtTrainingField");
                Apply("XorberaxTrainingField", "woundProbabilityAtArena", settings.TrainingArenaWoundChance, "TrainingField.SubModule", "WoundProbabilityAtArena");
                Apply("XorberaxTrainingField", "maxSkillValue", settings.TrainingMaxSkill, "TrainingField.SubModule", "MaxSkillValue");
                Apply("XorberaxTrainingField", "combatSkillsAffectExperienceGain", settings.TrainingCombatSkillScaling, "TrainingField.SubModule", "ShouldCombatSkillsAffectExperienceGain");

                Apply("XorberaxWork", "hoursInShift", settings.WorkShiftHours, "Work.SubModule", "HoursInShift");
                Apply("XorberaxWork", "workCooldownInHours", settings.WorkCooldownHours, "Work.SubModule", "WorkCooldownInHours");
                Apply("XorberaxWork", "paymentLimit", settings.WorkPaymentLimit, "Work.SubModule", "PaymentLimit");
                Apply("XorberaxWork", "athleticsExperienceGainedFromFullShift", settings.WorkAthleticsXp, "Work.SubModule", "AthleticsExperienceGainedFromFullShift");
                Apply("XorberaxWork", "relationChangeWithVillageNotablesFromFullShift", settings.WorkRelationGain, "Work.SubModule", "RelationChangeWithVillageNotablesFromFullShift");
                Apply("XorberaxWork", "probabilityOfReceivingGift", settings.WorkGiftChance, "Work.SubModule", "ProbabilityOfReceivingGift");
                Apply("XorberaxWork", "maxGiftQuantity", settings.WorkMaxGiftQuantity, "Work.SubModule", "MaxGiftQuantity");

                Apply("XorberaxScholars", "scholarAppearanceProbability", settings.ScholarAppearanceChance, "ScholarsOfCalradia.SubModule", "ScholarAppearanceProbability");
                Apply("XorberaxScholars", "noviceScholarExperienceGain", settings.NoviceScholarXp, "ScholarsOfCalradia.SubModule", "NoviceScholarExperienceGain");
                Apply("XorberaxScholars", "intermediateScholarExperienceGain", settings.IntermediateScholarXp, "ScholarsOfCalradia.SubModule", "IntermediateScholarExperienceGain");
                Apply("XorberaxScholars", "advancedScholarExperienceGain", settings.AdvancedScholarXp, "ScholarsOfCalradia.SubModule", "AdvancedScholarExperienceGain");
                Apply("XorberaxScholars", "expertScholarExperienceGain", settings.ExpertScholarXp, "ScholarsOfCalradia.SubModule", "ExpertScholarExperienceGain");

                Apply("XorberaxShoulderCam", "thirdPersonFieldOfView", settings.ShoulderFov, "ShoulderCam.SubModule", "ThirdPersonFieldOfView");
                Apply("XorberaxShoulderCam", "onFootPositionXOffset", settings.ShoulderOnFootX, "ShoulderCam.SubModule", "OnFootPositionXOffset");
                Apply("XorberaxShoulderCam", "onFootPositionZOffset", settings.ShoulderOnFootZ, "ShoulderCam.SubModule", "OnFootPositionZOffset");
                Apply("XorberaxShoulderCam", "mountedPositionXOffset", settings.ShoulderMountedX, "ShoulderCam.SubModule", "MountedPositionXOffset");
                Apply("XorberaxShoulderCam", "mountedPositionZOffset", settings.ShoulderMountedZ, "ShoulderCam.SubModule", "MountedPositionZOffset");
                Apply("XorberaxShoulderCam", "enableLiveConfigUpdates", settings.ShoulderLiveUpdates, "ShoulderCam.SubModule", "AreLiveConfigUpdatesEnabled");
                Apply("XorberaxShoulderCam", "maxCamShakeAmount", settings.ShoulderMaxShake, "ShoulderCam.SubModule", "MaxCamShakeAmount");

                Apply("XorberaxTextToSpeech", "SpeechRate", settings.TextToSpeechRate, "TextToSpeech.SubModule", "SpeechRate");
                Apply("XorberaxTextToSpeech", "IsDeveloperMode", settings.TextToSpeechDeveloperMode, "TextToSpeech.SubModule", "IsDeveloperMode");
            }
            catch (Exception ex)
            {
                PackLog.Error("Could not apply MCM config values", ex);
            }
        }

        private static void Apply(string moduleId, string jsonKey, object value, string subModuleTypeName, string configPropertyName)
        {
            WriteJsonValue(moduleId, jsonKey, value);
            SetRuntimeConfigValue(subModuleTypeName, configPropertyName, value);
        }

        private static void WriteJsonValue(string moduleId, string key, object value)
        {
            var path = GetConfigPath(moduleId);
            if (path == null || !File.Exists(path))
                return;

            try
            {
                var text = File.ReadAllText(path);
                var updated = ReplaceTopLevelJsonValue(text, key, value);
                if (!ReferenceEquals(text, updated))
                    File.WriteAllText(path, updated);
            }
            catch (Exception ex)
            {
                PackLog.Error("Could not write " + moduleId + " config key " + key, ex);
            }
        }

        private static string ReplaceTopLevelJsonValue(string text, string key, object value)
        {
            var escapedKey = Regex.Escape(key);
            var pattern = "(\""+ escapedKey + "\"\\s*:\\s*)(\"(?:\\\\.|[^\"\\\\])*\"|true|false|null|-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?)";
            var replacementValue = SerializeScalar(value);
            var replaced = false;
            return Regex.Replace(text, pattern, match =>
            {
                if (replaced)
                    return match.Value;
                replaced = true;
                return match.Groups[1].Value + replacementValue;
            });
        }

        private static string SerializeScalar(object value)
        {
            if (value == null)
                return "null";
            if (value is bool b)
                return b ? "true" : "false";
            if (value is string s)
                return "\"" + EscapeJsonString(s) + "\"";
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return "\"" + EscapeJsonString(value.ToString()) + "\"";
        }

        private static string EscapeJsonString(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(ch); break;
                }
            }
            return builder.ToString();
        }

        private static void SetRuntimeConfigValue(string subModuleTypeName, string propertyName, object value)
        {
            try
            {
                var subModuleType = FindType(subModuleTypeName);
                var configProperty = subModuleType?.GetProperty("Config", BindingFlags.Public | BindingFlags.Static);
                var config = configProperty?.GetValue(null);
                if (config == null)
                    return;

                var property = config.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                    return;

                var converted = ConvertFor(value, property.PropertyType);
                property.SetValue(config, converted);
            }
            catch
            {
                // Some values are applied before the target module assembly/config exists. The file write is enough for next load.
            }
        }

        private static object ConvertFor(object value, Type targetType)
        {
            if (value == null)
                return null;
            if (targetType.IsInstanceOfType(value))
                return value;
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value.ToString(), true);
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(asm =>
                {
                    try { return asm.GetType(fullName, false, false); }
                    catch { return null; }
                })
                .FirstOrDefault(type => type != null);
        }

        private static string GetConfigPath(string moduleId)
        {
            var integratedPath = GetIntegratedConfigPath();
            if (integratedPath != null && File.Exists(integratedPath))
                return integratedPath;

            var modulesRoot = GetModulesRoot();
            if (modulesRoot == null)
                return null;
            return Path.Combine(modulesRoot, moduleId, "bin", "Win64_Shipping_Client", "config.json");
        }

        private static string GetIntegratedConfigPath()
        {
            try
            {
                var location = typeof(ConfigBridge).Assembly.Location;
                var assemblyDir = Path.GetDirectoryName(location);
                return assemblyDir == null ? null : Path.Combine(assemblyDir, "config.json");
            }
            catch
            {
                return null;
            }
        }

        private static string GetModulesRoot()
        {
            try
            {
                var location = typeof(ConfigBridge).Assembly.Location;
                var binWin64 = Directory.GetParent(Path.GetDirectoryName(location));
                var moduleDir = binWin64?.Parent;
                return moduleDir?.Parent?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }
}
