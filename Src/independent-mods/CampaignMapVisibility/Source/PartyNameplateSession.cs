using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace CampaignMapVisibility
{
    internal sealed class PartyNameplateSession
    {
        private static readonly ConditionalWeakTable<PartyNameplatesVM, PartyNameplateSession> Sessions =
            new ConditionalWeakTable<PartyNameplatesVM, PartyNameplateSession>();
        private static readonly Action<PartyNameplatesVM, MobileParty> Create =
            AccessTools.MethodDelegate<Action<PartyNameplatesVM, MobileParty>>(
                AccessTools.Method(typeof(PartyNameplatesVM), "CreateNameplateFor"));
        private static readonly Action<PartyNameplatesVM, PartyNameplateVM> Remove =
            AccessTools.MethodDelegate<Action<PartyNameplatesVM, PartyNameplateVM>>(
                AccessTools.Method(typeof(PartyNameplatesVM), "RemoveNameplate"));
        private static readonly AccessTools.FieldRef<PartyNameplatesVM, Dictionary<MobileParty, PartyNameplateVM>> ByParty =
            AccessTools.FieldRefAccess<PartyNameplatesVM, Dictionary<MobileParty, PartyNameplateVM>>("_nameplatesByParty");

        private readonly PartyNameplatesVM _owner;
        private bool _revealing;

        internal static void ValidateBindings()
        {
            if (Create == null || Remove == null || ByParty == null)
                throw new InvalidOperationException("Native party nameplate bindings are unavailable.");
        }

        private PartyNameplateSession(PartyNameplatesVM owner)
        {
            _owner = owner;
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnCreated);
        }

        internal static void Initialize(PartyNameplatesVM owner)
        {
            if (Sessions.TryGetValue(owner, out var existing)) existing.Dispose();
            Sessions.Remove(owner);
            var session = new PartyNameplateSession(owner);
            Sessions.Add(owner, session);
            session.UpdateOptions();
        }

        internal static bool TryGet(PartyNameplatesVM owner, out PartyNameplateSession session) =>
            Sessions.TryGetValue(owner, out session);

        internal static void Finalize(PartyNameplatesVM owner)
        {
            if (Sessions.TryGetValue(owner, out var session)) session.Dispose();
            Sessions.Remove(owner);
        }

        private void Dispose() => CampaignEvents.MobilePartyCreated.ClearListeners(this);

        internal void UpdateOptions()
        {
            MapOptions.Refresh();
            if (_revealing == MapOptions.Reveal) return;
            _revealing = MapOptions.Reveal;

            // One O(N) reconciliation at a settings transition; no global scan per frame.
            if (!_revealing)
            {
                var plates = _owner.Nameplates;
                var retained = new List<PartyNameplateVM>(plates.Count);
                var lookup = ByParty(_owner);
                for (int i = 0; i < plates.Count; i++)
                {
                    var plate = plates[i];
                    if (ShouldShow(plate.Party)) retained.Add(plate);
                    else
                    {
                        lookup.Remove(plate.Party);
                        // Native Clear releases subscriptions. Do not return every revealed
                        // plate to the pool: that would retain the entire map after reveal ends.
                        plate.Clear();
                    }
                }
                plates.Clear();
                for (int i = 0; i < retained.Count; i++) plates.Add(retained[i]);
                return;
            }
            var parties = MobileParty.All;
            for (int i = 0; i < parties.Count; i++) Refresh(parties[i]);
        }

        private void OnCreated(MobileParty party)
        {
            if (MapOptions.Reveal) Refresh(party);
        }

        private static bool ShouldShow(MobileParty party) => party != null &&
            VisibilityPolicy.ShowParty(party.IsActive, party.IsGarrison || party.IsMilitia,
                party.CurrentSettlement != null, party.IsSpotted(), MapOptions.Reveal);

        internal void Refresh(MobileParty party)
        {
            // Let native player-nameplate handling continue unchanged.
            if (party == null || party.IsMainParty) return;
            var lookup = ByParty(_owner);
            bool present = lookup.TryGetValue(party, out var plate);
            if (ShouldShow(party))
            {
                if (!present) Create(_owner, party);
            }
            else if (present) Remove(_owner, plate);
        }
    }

    [HarmonyPatch(typeof(PartyNameplatesVM), nameof(PartyNameplatesVM.Initialize))]
    internal static class PartyInitializePatch
    {
        private static void Postfix(PartyNameplatesVM __instance) => PartyNameplateSession.Initialize(__instance);
    }

    [HarmonyPatch(typeof(PartyNameplatesVM), nameof(PartyNameplatesVM.Update))]
    internal static class PartyUpdatePatch
    {
        private static void Prefix(PartyNameplatesVM __instance)
        {
            if (PartyNameplateSession.TryGet(__instance, out var session)) session.UpdateOptions();
        }
    }

    [HarmonyPatch(typeof(PartyNameplatesVM), "UpdateMobilePartyVisibility")]
    internal static class PartyVisibilityPatch
    {
        private static bool Prefix(PartyNameplatesVM __instance, MobileParty mobileParty)
        {
            if (!MapOptions.Reveal || mobileParty == null || mobileParty.IsMainParty ||
                !PartyNameplateSession.TryGet(__instance, out var session)) return true;
            session.Refresh(mobileParty);
            return false;
        }
    }

    [HarmonyPatch(typeof(PartyNameplatesVM), "OnSettlementLeft")]
    internal static class PartyLeftSettlementPatch
    {
        private static void Postfix(PartyNameplatesVM __instance, MobileParty party)
        {
            if (MapOptions.Reveal && PartyNameplateSession.TryGet(__instance, out var session)) session.Refresh(party);
        }
    }

    [HarmonyPatch(typeof(PartyNameplatesVM), nameof(PartyNameplatesVM.OnFinalize))]
    internal static class PartyFinalizePatch
    {
        private static void Prefix(PartyNameplatesVM __instance) => PartyNameplateSession.Finalize(__instance);
    }
}
