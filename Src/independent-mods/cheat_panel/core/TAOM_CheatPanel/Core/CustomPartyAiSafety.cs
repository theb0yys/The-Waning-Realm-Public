using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TAOM_CheatPanel.Core
{
    internal enum CustomPartyAiSafetyOutcome
    {
        None = 0,
        GoToSettlement = 1,
        PatrolSettlement = 2,
        Held = 3,
        Failed = 4
    }

    internal static class CustomPartyAiSafety
    {
        internal static CustomPartyAiSafetyOutcome TryGoToOrPatrol(MobileParty party, Settlement destination, Settlement patrolFallback, string source)
        {
            Settlement safeDestination = ResolveUsableSettlement(destination, party, null);
            Settlement safeFallback = ResolveUsableSettlement(patrolFallback, party, safeDestination);

            if (safeDestination != null && safeDestination != safeFallback)
            {
                return TryApplySettlementMove(party, safeDestination, true, source);
            }

            if (safeFallback != null)
            {
                return TryApplySettlementMove(party, safeFallback, false, source);
            }

            return TryHold(party, source, "no usable settlement for go-to/patrol");
        }

        internal static CustomPartyAiSafetyOutcome TryPatrolAroundSettlement(MobileParty party, Settlement target, string source)
        {
            Settlement safeTarget = ResolveUsableSettlement(target, party, null);
            if (safeTarget != null)
            {
                return TryApplySettlementMove(party, safeTarget, false, source);
            }

            return TryHold(party, source, "no usable settlement for patrol");
        }

        internal static Settlement ResolveUsableSettlement(Settlement candidate, MobileParty party, Settlement fallback)
        {
            if (IsUsableSettlement(candidate))
            {
                return candidate;
            }

            if (IsUsableSettlement(fallback))
            {
                return fallback;
            }

            return ResolveNearestUsableSettlement(GetPartyOrPlayerPosition(party));
        }

        internal static bool IsUsableSettlement(Settlement settlement)
        {
            return settlement != null
                && !settlement.IsHideout
                && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
        }

        private static CustomPartyAiSafetyOutcome TryApplySettlementMove(MobileParty party, Settlement settlement, bool goTo, string source)
        {
            if (party == null || party.Ai == null)
            {
                return CustomPartyAiSafetyOutcome.Failed;
            }

            if (!IsUsableSettlement(settlement))
            {
                return TryHold(party, source, "invalid settlement target");
            }

            try
            {
                if (goTo)
                {
                    party.SetMoveGoToSettlement(settlement, MobileParty.NavigationType.Default, false);
                }
                else
                {
                    party.SetMovePatrolAroundSettlement(settlement, MobileParty.NavigationType.Default, false);
                }

                return goTo ? CustomPartyAiSafetyOutcome.GoToSettlement : CustomPartyAiSafetyOutcome.PatrolSettlement;
            }
            catch (Exception ex)
            {
                TAOM_CheatPanel.Runtime.Log.ErrorOnce(
                    "custom.party.ai.apply." + CleanSignature(source) + "." + (goTo ? "go_to" : "patrol"),
                    ex);
                return TryHold(party, source, "settlement move failed");
            }
        }

        private static CustomPartyAiSafetyOutcome TryHold(MobileParty party, string source, string reason)
        {
            try
            {
                if (party != null && party.Ai != null)
                {
                    party.SetMoveModeHold();
                    TAOM_CheatPanel.Runtime.Log.Warn("[CustomPartyAiSafety] Held " + DescribeParty(party) + " from " + CleanSource(source) + ": " + reason + ".");
                    return CustomPartyAiSafetyOutcome.Held;
                }
            }
            catch (Exception ex)
            {
                TAOM_CheatPanel.Runtime.Log.ErrorOnce("custom.party.ai.hold." + CleanSignature(source), ex);
            }

            return CustomPartyAiSafetyOutcome.Failed;
        }

        private static Settlement ResolveNearestUsableSettlement(Vec2 position)
        {
            try
            {
                return Settlement.All
                    .Where(IsUsableSettlement)
                    .OrderBy(s => SafeDistance(position, s))
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static Vec2 GetPartyOrPlayerPosition(MobileParty party)
        {
            try
            {
                if (party != null)
                {
                    return party.GetPosition2D;
                }

                if (MobileParty.MainParty != null)
                {
                    return MobileParty.MainParty.GetPosition2D;
                }
            }
            catch
            {
            }

            return Vec2.Zero;
        }

        private static float SafeDistance(Vec2 position, Settlement settlement)
        {
            try
            {
                return settlement.GetPosition2D.Distance(position);
            }
            catch
            {
                return 9999f;
            }
        }

        private static string DescribeParty(MobileParty party)
        {
            if (party == null)
            {
                return "unknown-party";
            }

            return string.IsNullOrWhiteSpace(party.StringId) ? "unnamed-party" : party.StringId;
        }

        private static string CleanSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "unknown" : source;
        }

        private static string CleanSignature(string source)
        {
            string value = CleanSource(source);
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
