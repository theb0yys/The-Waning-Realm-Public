using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;

namespace TAOM_ContextualMessages
{
    /// <summary>
    /// Reflection-based bridge to optional companion mods.
    /// All methods fail gracefully, returning defaults if a mod is not loaded.
    ///
    /// FIX: Shadow and Dread bridge was completely broken — GetDread searched for a
    /// public GetDread(Settlement) method on DreadCampaignBehavior, which never
    /// existed, so every cross-post always returned 0f.
    ///
    /// Fixed resolution order:
    ///   1. GetSettlementDread(Settlement) — public wrapper on the behavior (preferred).
    ///   2. Fallback: _store.GetDread(Settlement) via reflection on the private field,
    ///      for builds of the mod that pre-date the public wrapper.
    /// </summary>
    public static class ModBridge
    {
        // ── Shadow & Dread cache ───────────────────────────────────────────
        private static bool _shadowDreadSearched;
        private static object _dreadMethodTarget;
        private static MethodInfo _getDreadMethod;

        // ── Roads of the King cache ────────────────────────────────────────
        private static bool _roadsSearched;
        private static CampaignBehaviorBase _roadsBehavior;
        private static FieldInfo _planField;
        private static FieldInfo _planIsActiveField;
        private static MethodInfo _planGetNextStopMethod;
        private static PropertyInfo _planCountProperty;

        // ═══════════════════════════════════════════════════════════════════
        // Shadow & Dread
        // ═══════════════════════════════════════════════════════════════════

        public static float GetDread(Settlement settlement)
        {
            if (settlement == null) return 0f;
            try
            {
                if (!EnsureShadowDread()) return 0f;
                var result = _getDreadMethod.Invoke(_dreadMethodTarget, new object[] { settlement });
                return result is float f ? f : 0f;
            }
            catch { return 0f; }
        }

        public static bool ShadowDreadLoaded => EnsureShadowDread();

        private static bool EnsureShadowDread()
        {
            if (_shadowDreadSearched)
                return _getDreadMethod != null;

            _shadowDreadSearched = true;

            try
            {
                if (Campaign.Current == null)
                    return false;

                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    if (b == null || b.GetType().Name != "DreadCampaignBehavior")
                        continue;

                    var m = b.GetType().GetMethod(
                        "GetSettlementDread",
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        new[] { typeof(Settlement) },
                        null);

                    if (m != null)
                    {
                        _dreadMethodTarget = b;
                        _getDreadMethod = m;
                        LogBridge("Shadow & Dread linked via GetSettlementDread.");
                        return true;
                    }

                    m = b.GetType().GetMethod(
                        "GetDread",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(Settlement) },
                        null);

                    if (m != null)
                    {
                        _dreadMethodTarget = b;
                        _getDreadMethod = m;
                        LogBridge("Shadow & Dread linked via GetDread fallback.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogBridge("EnsureShadowDread failed: " + ex.Message);
            }

            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Roads of the King
        // ═══════════════════════════════════════════════════════════════════

        public static bool IsRouteActive()
        {
            try
            {
                if (!EnsureRoads()) return false;
                var plan = _planField?.GetValue(_roadsBehavior);
                if (plan == null || _planIsActiveField == null) return false;
                return (bool)_planIsActiveField.GetValue(plan);
            }
            catch { return false; }
        }

        public static string GetNextStopName()
        {
            try
            {
                if (!EnsureRoads()) return null;
                var plan = _planField?.GetValue(_roadsBehavior);
                if (plan == null || _planGetNextStopMethod == null) return null;
                return _planGetNextStopMethod.Invoke(plan, null)?.ToString();
            }
            catch { return null; }
        }

        public static int GetRouteStopCount()
        {
            try
            {
                if (!EnsureRoads()) return 0;
                var plan = _planField?.GetValue(_roadsBehavior);
                if (plan == null || _planCountProperty == null) return 0;
                return (int)_planCountProperty.GetValue(plan, null);
            }
            catch { return 0; }
        }

        public static bool RoadsLoaded => EnsureRoads();

        private static bool EnsureRoads()
        {
            if (_roadsSearched) return _roadsBehavior != null;
            _roadsSearched = true;

            try
            {
                if (Campaign.Current == null) return false;

                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    if (b == null || b.GetType().Name != "RotKCampaignBehavior") continue;

                    _roadsBehavior = b as CampaignBehaviorBase;
                    _planField = b.GetType().GetField("_plan",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    if (_planField != null)
                    {
                        var t = _planField.FieldType;
                        _planIsActiveField = t.GetField("IsActive", BindingFlags.Instance | BindingFlags.Public);
                        _planGetNextStopMethod = t.GetMethod("GetNextStopName", BindingFlags.Instance | BindingFlags.Public);
                        _planCountProperty = t.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
                    }

                    LogBridge("Roads of the King linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureRoads: " + ex.Message); }

            return false;
        }

        // ═══════════════════════════════════════════════════════════════════

        // ── Weight of Rule cache ──────────────────────────────────────────
        private static bool       _weightSearched;
        private static object     _weightTarget;
        private static MethodInfo _weightGetStrain;
        private static MethodInfo _weightGetLegitimacy;
        private static MethodInfo _weightGetEdictPressure;

        // ── Watch of the Marches cache ────────────────────────────────────
        private static bool       _watchSearched;
        private static object     _watchTarget;
        private static MethodInfo _watchGetReadiness;
        private static MethodInfo _watchGetCivicBurden;
        private static MethodInfo _watchGetConfidence;

        // ── Bonds of the Holding cache ────────────────────────────────────
        private static bool       _bondsSearched;
        private static object     _bondsTarget;
        private static MethodInfo _bondsGetVillageStrain;
        private static MethodInfo _bondsGetGrainFlow;
        private static MethodInfo _bondsGetCompactLegitimacy;

        // ── The Long Mending cache ────────────────────────────────────────
        private static bool       _mendingSearched;
        private static object     _mendingTarget;
        private static MethodInfo _mendingGetWoundCount;
        private static MethodInfo _mendingGetSeverity;
        private static MethodInfo _mendingGetRecoveryProgress;

        // ── Burdened Marches cache ────────────────────────────────────────
        private static bool       _marchesSearched;
        private static object     _marchesTarget;
        private static MethodInfo _marchesGetPressure;
        private static MethodInfo _marchesGetTier;

        // ── Founding of Holds cache ───────────────────────────────────────
        private static bool       _foundingSearched;
        private static object     _foundingTarget;
        private static MethodInfo _foundingGetStage;
        private static MethodInfo _foundingGetProgress;

        // ── Hosts in the Field cache ──────────────────────────────────────
        private static bool       _hostsSearched;
        private static object     _hostsTarget;
        private static MethodInfo _hostsGetCohesion;
        private static MethodInfo _hostsGetRequisitionPressure;

        // ── Oaths and Houses cache ────────────────────────────────────────
        private static bool       _oathsSearched;
        private static object     _oathsTarget;
        private static MethodInfo _oathsGetFeudheat;
        private static MethodInfo _oathsGetOathStrength;

        // ── Recovery Legacy Traits cache ──────────────────────────────────
        private static bool       _legacySearched;
        private static object     _legacyTarget;
        private static MethodInfo _legacyGetScore;
        private static MethodInfo _legacyGetTraitCount;

        // ── Court / Council cache ─────────────────────────────────────────
        private static bool       _courtSearched;
        private static object     _courtTarget;
        private static MethodInfo _courtGetPetitionCount;
        private static MethodInfo _courtGetPrestige;

        // ── Roads of Middle-earth cache ───────────────────────────────────
        private static bool       _roadsMESearched;
        private static object     _roadsMETarget;
        private static MethodInfo _roadsMEGetScore;
        private static MethodInfo _roadsMEGetCondition;

        // ── Army Composition cache ────────────────────────────────────────
        private static bool       _armyDocSearched;
        private static object     _armyDocTarget;
        private static MethodInfo _armyDocGetDrift;

        // ── Ranger's Satchel cache ────────────────────────────────────────
        private static bool       _rangerSearched;
        private static object     _rangerTarget;
        private static MethodInfo _rangerGetCampReadiness;
        private static MethodInfo _rangerGetWeatherSeverity;

        // ── Palantír cache ────────────────────────────────────────────────
        private static bool       _palantirSearched;
        private static object     _palantirTarget;
        private static MethodInfo _palantirGetHealth;

        // ═══════════════════════════════════════════════════════════════════
        // Weight of Rule
        // ═══════════════════════════════════════════════════════════════════

        public static float GetAdminStrain(Settlement s)
        {
            try { if (!EnsureWeight() || _weightGetStrain == null || s == null) return 0f;
                  return ToFloat(_weightGetStrain.Invoke(_weightTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetRuleLegitimacy(Settlement s)
        {
            try { if (!EnsureWeight() || _weightGetLegitimacy == null || s == null) return 0f;
                  return ToFloat(_weightGetLegitimacy.Invoke(_weightTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetEdictPressure(Settlement s)
        {
            try { if (!EnsureWeight() || _weightGetEdictPressure == null || s == null) return 0f;
                  return ToFloat(_weightGetEdictPressure.Invoke(_weightTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool WeightLoaded => EnsureWeight();

        private static bool EnsureWeight()
        {
            if (_weightSearched) return _weightTarget != null;
            _weightSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "WeightOfRuleBehavior" && n != "WeightCampaignBehavior") continue;
                    _weightTarget           = b;
                    _weightGetStrain        = FindMethod(b, "GetAdminStrain",     typeof(Settlement));
                    _weightGetLegitimacy    = FindMethod(b, "GetRuleLegitimacy",  typeof(Settlement));
                    _weightGetEdictPressure = FindMethod(b, "GetEdictPressure",   typeof(Settlement));
                    LogBridge("Weight of Rule linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureWeight: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Watch of the Marches
        // ═══════════════════════════════════════════════════════════════════

        public static float GetWatchReadiness(Settlement s)
        {
            try { if (!EnsureWatch() || _watchGetReadiness == null || s == null) return 0f;
                  return ToFloat(_watchGetReadiness.Invoke(_watchTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetCivicBurden(Settlement s)
        {
            try { if (!EnsureWatch() || _watchGetCivicBurden == null || s == null) return 0f;
                  return ToFloat(_watchGetCivicBurden.Invoke(_watchTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetLocalConfidence(Settlement s)
        {
            try { if (!EnsureWatch() || _watchGetConfidence == null || s == null) return 0f;
                  return ToFloat(_watchGetConfidence.Invoke(_watchTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool WatchLoaded => EnsureWatch();

        private static bool EnsureWatch()
        {
            if (_watchSearched) return _watchTarget != null;
            _watchSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "WatchOfTheMarchesBehavior" && n != "WatchCampaignBehavior") continue;
                    _watchTarget         = b;
                    _watchGetReadiness   = FindMethod(b, "GetWatchReadiness",  typeof(Settlement));
                    _watchGetCivicBurden = FindMethod(b, "GetCivicBurden",     typeof(Settlement));
                    _watchGetConfidence  = FindMethod(b, "GetLocalConfidence", typeof(Settlement));
                    LogBridge("Watch of the Marches linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureWatch: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Bonds of the Holding
        // ═══════════════════════════════════════════════════════════════════

        public static float GetVillageStrain(Settlement s)
        {
            try { if (!EnsureBonds() || _bondsGetVillageStrain == null || s == null) return 0f;
                  return ToFloat(_bondsGetVillageStrain.Invoke(_bondsTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetGrainFlow(Settlement s)
        {
            try { if (!EnsureBonds() || _bondsGetGrainFlow == null || s == null) return 0f;
                  return ToFloat(_bondsGetGrainFlow.Invoke(_bondsTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetCompactLegitimacy(Settlement s)
        {
            try { if (!EnsureBonds() || _bondsGetCompactLegitimacy == null || s == null) return 0f;
                  return ToFloat(_bondsGetCompactLegitimacy.Invoke(_bondsTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool BondsLoaded => EnsureBonds();

        private static bool EnsureBonds()
        {
            if (_bondsSearched) return _bondsTarget != null;
            _bondsSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "BondsOfTheHoldingBehavior" && n != "BondsCampaignBehavior") continue;
                    _bondsTarget               = b;
                    _bondsGetVillageStrain     = FindMethod(b, "GetVillageStrain",     typeof(Settlement));
                    _bondsGetGrainFlow         = FindMethod(b, "GetGrainFlow",         typeof(Settlement));
                    _bondsGetCompactLegitimacy = FindMethod(b, "GetCompactLegitimacy", typeof(Settlement));
                    LogBridge("Bonds of the Holding linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureBonds: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // The Long Mending
        // ═══════════════════════════════════════════════════════════════════

        public static int GetWoundCount(Settlement s)
        {
            try { if (!EnsureMending() || _mendingGetWoundCount == null || s == null) return 0;
                  var r = _mendingGetWoundCount.Invoke(_mendingTarget, new object[] { s });
                  return r is int i ? i : r is float f ? (int)f : 0; }
            catch { return 0; }
        }

        public static float GetWoundSeverity(Settlement s)
        {
            try { if (!EnsureMending() || _mendingGetSeverity == null || s == null) return 0f;
                  return ToFloat(_mendingGetSeverity.Invoke(_mendingTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetRecoveryProgress(Settlement s)
        {
            try { if (!EnsureMending() || _mendingGetRecoveryProgress == null || s == null) return 0f;
                  return ToFloat(_mendingGetRecoveryProgress.Invoke(_mendingTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool MendingLoaded => EnsureMending();

        private static bool EnsureMending()
        {
            if (_mendingSearched) return _mendingTarget != null;
            _mendingSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "LongMendingBehavior" && n != "LongMendingCampaignBehavior") continue;
                    _mendingTarget              = b;
                    _mendingGetWoundCount       = FindMethod(b, "GetWoundCount",       typeof(Settlement));
                    _mendingGetSeverity         = FindMethod(b, "GetWoundSeverity",    typeof(Settlement));
                    _mendingGetRecoveryProgress = FindMethod(b, "GetRecoveryProgress", typeof(Settlement));
                    LogBridge("Long Mending linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureMending: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Burdened Marches
        // ═══════════════════════════════════════════════════════════════════

        public static float GetMarchPressure(Settlement s)
        {
            try { if (!EnsureMarches() || _marchesGetPressure == null || s == null) return 0f;
                  return ToFloat(_marchesGetPressure.Invoke(_marchesTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool MarchesLoaded => EnsureMarches();

        private static bool EnsureMarches()
        {
            if (_marchesSearched) return _marchesTarget != null;
            _marchesSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "BurdenedMarchesBehavior" && n != "MarchesCampaignBehavior") continue;
                    _marchesTarget      = b;
                    _marchesGetPressure = FindMethod(b, "GetMarchPressure", typeof(Settlement));
                    _marchesGetTier     = FindMethod(b, "GetMarchTier",     typeof(Settlement));
                    LogBridge("Burdened Marches linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureMarches: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Founding of Holds
        // ═══════════════════════════════════════════════════════════════════

        public static int GetFoundingStage(Settlement s)
        {
            try { if (!EnsureFounding() || _foundingGetStage == null || s == null) return 0;
                  var r = _foundingGetStage.Invoke(_foundingTarget, new object[] { s });
                  return r is int i ? i : r is float f ? (int)f : 0; }
            catch { return 0; }
        }

        public static float GetFoundingProgress(Settlement s)
        {
            try { if (!EnsureFounding() || _foundingGetProgress == null || s == null) return 0f;
                  return ToFloat(_foundingGetProgress.Invoke(_foundingTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool FoundingLoaded => EnsureFounding();

        private static bool EnsureFounding()
        {
            if (_foundingSearched) return _foundingTarget != null;
            _foundingSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "FoundingOfHoldsBehavior" && n != "FoundingCampaignBehavior") continue;
                    _foundingTarget      = b;
                    _foundingGetStage    = FindMethod(b, "GetFoundingStage",    typeof(Settlement));
                    _foundingGetProgress = FindMethod(b, "GetFoundingProgress", typeof(Settlement));
                    LogBridge("Founding of Holds linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureFounding: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Hosts in the Field
        // ═══════════════════════════════════════════════════════════════════

        public static float GetHostCohesion()
        {
            try { if (!EnsureHosts() || _hostsGetCohesion == null) return 0f;
                  return ToFloat(_hostsGetCohesion.Invoke(_hostsTarget, null)); }
            catch { return 0f; }
        }

        public static float GetRequisitionPressure(Settlement s)
        {
            try { if (!EnsureHosts() || _hostsGetRequisitionPressure == null || s == null) return 0f;
                  return ToFloat(_hostsGetRequisitionPressure.Invoke(_hostsTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool HostsLoaded => EnsureHosts();

        private static bool EnsureHosts()
        {
            if (_hostsSearched) return _hostsTarget != null;
            _hostsSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "HostsInTheFieldBehavior" && n != "HostsCampaignBehavior") continue;
                    _hostsTarget                 = b;
                    _hostsGetCohesion            = FindMethod(b, "GetHostCohesion",       System.Type.EmptyTypes);
                    _hostsGetRequisitionPressure = FindMethod(b, "GetRequisitionPressure", typeof(Settlement));
                    LogBridge("Hosts in the Field linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureHosts: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Oaths and Houses
        // ═══════════════════════════════════════════════════════════════════

        public static float GetFeudheat(Hero hero)
        {
            try { if (!EnsureOaths() || _oathsGetFeudheat == null || hero == null) return 0f;
                  return ToFloat(_oathsGetFeudheat.Invoke(_oathsTarget, new object[] { hero })); }
            catch { return 0f; }
        }

        public static float GetOathStrength(Hero hero)
        {
            try { if (!EnsureOaths() || _oathsGetOathStrength == null || hero == null) return 0f;
                  return ToFloat(_oathsGetOathStrength.Invoke(_oathsTarget, new object[] { hero })); }
            catch { return 0f; }
        }

        public static bool OathsLoaded => EnsureOaths();

        private static bool EnsureOaths()
        {
            if (_oathsSearched) return _oathsTarget != null;
            _oathsSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "OathsAndHousesBehavior" && n != "OathsCampaignBehavior") continue;
                    _oathsTarget          = b;
                    _oathsGetFeudheat     = FindMethod(b, "GetFeudheat",    typeof(Hero));
                    _oathsGetOathStrength = FindMethod(b, "GetOathStrength", typeof(Hero));
                    LogBridge("Oaths and Houses linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureOaths: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Recovery Legacy Traits
        // ═══════════════════════════════════════════════════════════════════

        public static float GetLegacyScore(Settlement s)
        {
            try { if (!EnsureLegacy() || _legacyGetScore == null || s == null) return 0f;
                  return ToFloat(_legacyGetScore.Invoke(_legacyTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static int GetActiveTraitCount(Settlement s)
        {
            try { if (!EnsureLegacy() || _legacyGetTraitCount == null || s == null) return 0;
                  var r = _legacyGetTraitCount.Invoke(_legacyTarget, new object[] { s });
                  return r is int i ? i : r is float f ? (int)f : 0; }
            catch { return 0; }
        }

        public static bool LegacyLoaded => EnsureLegacy();

        private static bool EnsureLegacy()
        {
            if (_legacySearched) return _legacyTarget != null;
            _legacySearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "RecoveryLegacyTraitsBehavior" && n != "LegacyTraitsBehavior") continue;
                    _legacyTarget        = b;
                    _legacyGetScore      = FindMethod(b, "GetLegacyScore",      typeof(Settlement));
                    _legacyGetTraitCount = FindMethod(b, "GetActiveTraitCount", typeof(Settlement));
                    LogBridge("Recovery Legacy Traits linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureLegacy: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Court / Council of the Waning Age
        // ═══════════════════════════════════════════════════════════════════

        public static int GetPetitionCount(Settlement s)
        {
            try { if (!EnsureCourt() || _courtGetPetitionCount == null || s == null) return 0;
                  var r = _courtGetPetitionCount.Invoke(_courtTarget, new object[] { s });
                  return r is int i ? i : r is float f ? (int)f : 0; }
            catch { return 0; }
        }

        public static float GetCourtPrestige(Settlement s)
        {
            try { if (!EnsureCourt() || _courtGetPrestige == null || s == null) return 0f;
                  return ToFloat(_courtGetPrestige.Invoke(_courtTarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool CourtLoaded => EnsureCourt();

        private static bool EnsureCourt()
        {
            if (_courtSearched) return _courtTarget != null;
            _courtSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "CourtCouncilBehavior" && n != "CourtCampaignBehavior") continue;
                    _courtTarget           = b;
                    _courtGetPetitionCount = FindMethod(b, "GetPetitionCount", typeof(Settlement));
                    _courtGetPrestige      = FindMethod(b, "GetCourtPrestige",  typeof(Settlement));
                    LogBridge("Court / Council linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureCourt: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Roads of Middle-earth
        // ═══════════════════════════════════════════════════════════════════

        public static float GetCorridorScore(Settlement s)
        {
            try { if (!EnsureRoadsME() || _roadsMEGetScore == null || s == null) return 0f;
                  return ToFloat(_roadsMEGetScore.Invoke(_roadsMETarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static float GetRoadCondition(Settlement s)
        {
            try { if (!EnsureRoadsME() || _roadsMEGetCondition == null || s == null) return 0f;
                  return ToFloat(_roadsMEGetCondition.Invoke(_roadsMETarget, new object[] { s })); }
            catch { return 0f; }
        }

        public static bool RoadsMELoaded => EnsureRoadsME();

        private static bool EnsureRoadsME()
        {
            if (_roadsMESearched) return _roadsMETarget != null;
            _roadsMESearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "RoadsOfMiddleEarthBehavior" && n != "RoadsMECampaignBehavior") continue;
                    _roadsMETarget       = b;
                    _roadsMEGetScore     = FindMethod(b, "GetCorridorScore", typeof(Settlement));
                    _roadsMEGetCondition = FindMethod(b, "GetRoadCondition", typeof(Settlement));
                    LogBridge("Roads of Middle-earth linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureRoadsME: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Army Composition
        // ═══════════════════════════════════════════════════════════════════

        public static float GetCompositionDrift(MobileParty party)
        {
            try { if (!EnsureArmyDoc() || _armyDocGetDrift == null || party == null) return 0f;
                  return ToFloat(_armyDocGetDrift.Invoke(_armyDocTarget, new object[] { party })); }
            catch { return 0f; }
        }

        public static bool ArmyDocLoaded => EnsureArmyDoc();

        private static bool EnsureArmyDoc()
        {
            if (_armyDocSearched) return _armyDocTarget != null;
            _armyDocSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "ArmyCompositionBehavior" && n != "ArmyCompCampaignBehavior") continue;
                    _armyDocTarget   = b;
                    _armyDocGetDrift = FindMethod(b, "GetCompositionDrift", typeof(MobileParty));
                    LogBridge("Army Composition linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureArmyDoc: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Ranger's Satchel
        // ═══════════════════════════════════════════════════════════════════

        public static float GetCampReadiness()
        {
            try { if (!EnsureRanger() || _rangerGetCampReadiness == null) return 0f;
                  return ToFloat(_rangerGetCampReadiness.Invoke(_rangerTarget, null)); }
            catch { return 0f; }
        }

        public static float GetWeatherSeverity()
        {
            try { if (!EnsureRanger() || _rangerGetWeatherSeverity == null) return 0f;
                  return ToFloat(_rangerGetWeatherSeverity.Invoke(_rangerTarget, null)); }
            catch { return 0f; }
        }

        public static bool RangerLoaded => EnsureRanger();

        private static bool EnsureRanger()
        {
            if (_rangerSearched) return _rangerTarget != null;
            _rangerSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "RangersSatchelBehavior" && n != "RangerCampaignBehavior") continue;
                    _rangerTarget             = b;
                    _rangerGetCampReadiness   = FindMethod(b, "GetCampReadiness",   System.Type.EmptyTypes);
                    _rangerGetWeatherSeverity = FindMethod(b, "GetWeatherSeverity", System.Type.EmptyTypes);
                    LogBridge("Ranger's Satchel linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsureRanger: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Palantír
        // ═══════════════════════════════════════════════════════════════════

        public static float GetSaveHealthScore()
        {
            try { if (!EnsurePalantir() || _palantirGetHealth == null) return 0f;
                  return ToFloat(_palantirGetHealth.Invoke(_palantirTarget, null)); }
            catch { return 0f; }
        }

        public static bool PalantirLoaded => EnsurePalantir();

        private static bool EnsurePalantir()
        {
            if (_palantirSearched) return _palantirTarget != null;
            _palantirSearched = true;
            try
            {
                if (Campaign.Current == null) return false;
                foreach (ICampaignBehavior b in Campaign.Current.GetCampaignBehaviors<ICampaignBehavior>())
                {
                    string n = b?.GetType().Name;
                    if (n != "PalantirBehavior" && n != "PalantirCampaignBehavior") continue;
                    _palantirTarget    = b;
                    _palantirGetHealth = FindMethod(b, "GetSaveHealthScore", System.Type.EmptyTypes);
                    LogBridge("Palantir linked.");
                    return true;
                }
            }
            catch (Exception ex) { LogBridge("EnsurePalantir: " + ex.Message); }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Shared reflection helpers
        // ═══════════════════════════════════════════════════════════════════

        // Single-param: Settlement, Hero, MobileParty, etc.
        private static MethodInfo FindMethod(ICampaignBehavior b, string name, System.Type paramType)
            => b.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { paramType }, null);

        // No-arg: GetHostCohesion, GetCampReadiness, GetWeatherSeverity, GetSaveHealthScore
        private static MethodInfo FindMethod(ICampaignBehavior b, string name, System.Type[] paramTypes)
            => b.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, paramTypes, null);

        private static float ToFloat(object r)
        {
            if (r is float f)  return f;
            if (r is int   i)  return (float)i;
            if (r is double d) return (float)d;
            return 0f;
        }

        // Lifecycle
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Reset all cached references. Must be called on every session launch.</summary>
        public static void Reset()
        {
            _shadowDreadSearched = false;
            _dreadMethodTarget = null;
            _getDreadMethod = null;
            _roadsSearched = false;
            _roadsBehavior = null;
            _planField = null;
            _planIsActiveField = null;
            _planGetNextStopMethod = null;
            _planCountProperty = null;

            _weightSearched = false; _weightTarget = null; _weightGetStrain = null; _weightGetLegitimacy = null; _weightGetEdictPressure = null;
            _watchSearched = false;  _watchTarget = null;  _watchGetReadiness = null; _watchGetCivicBurden = null; _watchGetConfidence = null;
            _bondsSearched = false;  _bondsTarget = null;  _bondsGetVillageStrain = null; _bondsGetGrainFlow = null; _bondsGetCompactLegitimacy = null;
            _mendingSearched = false; _mendingTarget = null; _mendingGetWoundCount = null; _mendingGetSeverity = null; _mendingGetRecoveryProgress = null;
            _marchesSearched = false; _marchesTarget = null; _marchesGetPressure = null; _marchesGetTier = null;
            _foundingSearched = false; _foundingTarget = null; _foundingGetStage = null; _foundingGetProgress = null;
            _hostsSearched = false;  _hostsTarget = null;  _hostsGetCohesion = null; _hostsGetRequisitionPressure = null;
            _oathsSearched = false;  _oathsTarget = null;  _oathsGetFeudheat = null; _oathsGetOathStrength = null;
            _legacySearched = false; _legacyTarget = null; _legacyGetScore = null; _legacyGetTraitCount = null;
            _courtSearched = false;  _courtTarget = null;  _courtGetPetitionCount = null; _courtGetPrestige = null;
            _roadsMESearched = false; _roadsMETarget = null; _roadsMEGetScore = null; _roadsMEGetCondition = null;
            _armyDocSearched = false; _armyDocTarget = null; _armyDocGetDrift = null;
            _rangerSearched = false; _rangerTarget = null; _rangerGetCampReadiness = null; _rangerGetWeatherSeverity = null;
            _palantirSearched = false; _palantirTarget = null; _palantirGetHealth = null;
        }

        private static void LogBridge(string msg)
        {
            try { TaleWorlds.Library.Debug.Print("[TAOM_ContextualMessages] ModBridge: " + msg); }
            catch { }
        }
    }
}