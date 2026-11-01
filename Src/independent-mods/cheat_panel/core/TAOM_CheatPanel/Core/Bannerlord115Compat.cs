using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TAOM_CheatPanel.Core
{
    internal static class Bannerlord115Compat
    {
        internal static Vec2 GetPosition2D(MobileParty party)
        {
            return party != null ? party.GetPosition2D : Vec2.Zero;
        }

        internal static Vec2 GetPosition2D(Settlement settlement)
        {
            return settlement != null ? settlement.GetPosition2D : Vec2.Zero;
        }

        internal static void SetPosition2D(MobileParty party, Vec2 position)
        {
            if (party == null)
                return;

            party.Position = new CampaignVec2(position, true);
        }

        internal static void SetPosition2D(MobileParty party, CampaignVec2 position)
        {
            if (party == null)
                return;

            party.Position = position;
        }

        internal static CampaignVec2 ToCampaignPosition(Vec2 position)
        {
            return new CampaignVec2(position, true);
        }

        internal static MobileParty CreateBanditParty(string partyId, Clan banditClan, Vec2 position)
        {
            if (banditClan == null)
                return null;

            PartyTemplateObject template = banditClan.DefaultPartyTemplate;
            if (template == null)
            {
                template = MBObjectManager.Instance.GetObjectTypeList<PartyTemplateObject>()
                    .FirstOrDefault(pt => pt != null
                        && !string.IsNullOrWhiteSpace(pt.StringId)
                        && pt.StringId.IndexOf("bandit", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (template == null)
                return null;

            MobileParty party = BanditPartyComponent.CreateBanditParty(
                partyId,
                banditClan,
                hideout: null,
                isBossParty: false,
                pt: template,
                initialPosition: new CampaignVec2(position, true));

            if (party != null)
            {
                party.Position = new CampaignVec2(position, true);
                try
                {
                    if (party.Ai != null)
                    {
                        party.SetMoveModeHold();
                    }
                }
                catch
                {
                }
            }

            return party;
        }
    }
}
