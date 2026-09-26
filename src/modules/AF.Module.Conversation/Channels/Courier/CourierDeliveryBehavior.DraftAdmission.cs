using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
    private const int MaximumCourierDraftTickets = 128;
    private sealed class CourierDraftTicket
    {
        internal string ClientId;
        internal PendingCourierFlow Flow;
        internal long Revision;
        internal Hero Recipient;
    }
    private readonly object _courierDraftTicketGate = new object();
    private readonly Dictionary<string, CourierDraftTicket> _courierDraftTickets =
        new Dictionary<string, CourierDraftTicket>(StringComparer.Ordinal);

    // Capture is main-thread only, has no dispatch effects, and never constructs a draft.
    internal static string IssueModuleCourierTicket(string clientId)
    {
        CourierDeliveryBehavior owner = Instance;
        if (owner == null || !TWParallel.IsMainThread() || string.IsNullOrEmpty(clientId) || clientId.Length > 128)
            return null;
        PendingCourierFlow flow = owner._pendingFlow;
        if (flow == null || !flow.ReadyToSend || flow.Recipient == null
            || !owner.IsPendingCourierFlowCurrent(flow, flow.Revision)) return null;
        lock (owner._courierDraftTicketGate)
        {
            if (owner._courierDraftTickets.Count >= MaximumCourierDraftTickets) return null;
            string ticket = Guid.NewGuid().ToString("N");
            owner._courierDraftTickets.Add(ticket, new CourierDraftTicket
            {
                ClientId = clientId, Flow = flow, Revision = flow.Revision, Recipient = flow.Recipient
            });
            return ticket;
        }
    }

    private bool TryTakeModuleCourierTicket(string clientId, string ticketId, out PendingCourierFlow flow, out long revision)
    {
        flow = null;
        revision = -1;
        if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
            || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(ticketId)) return false;
        CourierDraftTicket ticket;
        lock (_courierDraftTicketGate)
        {
            if (!_courierDraftTickets.TryGetValue(ticketId, out ticket)
                || !string.Equals(ticket.ClientId, clientId, StringComparison.Ordinal)) return false;
            _courierDraftTickets.Remove(ticketId);
        }
        if (!IsPendingCourierFlowCurrent(ticket.Flow, ticket.Revision) || !ticket.Flow.ReadyToSend
            || !ReferenceEquals(ticket.Recipient, ticket.Flow.Recipient)) return false;
        flow = ticket.Flow;
        revision = ticket.Revision;
        return true;
    }

    internal static void RevokeModuleCourierTickets(string clientId)
    {
        CourierDeliveryBehavior owner = Instance;
        if (owner == null || string.IsNullOrEmpty(clientId)) return;
        lock (owner._courierDraftTicketGate)
        {
            var remove = new List<string>();
            foreach (var entry in owner._courierDraftTickets)
                if (string.Equals(entry.Value.ClientId, clientId, StringComparison.Ordinal)) remove.Add(entry.Key);
            foreach (string id in remove) owner._courierDraftTickets.Remove(id);
        }
    }

    private void InvalidateCourierDraftTickets()
    {
        lock (_courierDraftTicketGate) _courierDraftTickets.Clear();
    }

    // Explicit send only: one current roster lookup per selected crew type and one payload
    // option snapshot. No new tick polling, all-session traversal, or cached mutable stock.
    private string ValidateCourierDraftForDispatch(PendingCourierFlow flow, long revision, string input)
    {
        if (!IsPendingCourierFlowCurrent(flow, revision) || !flow.ReadyToSend) return "courier.context_unavailable";
        if (string.IsNullOrWhiteSpace(input) || input.Length > Refactor.Modules.CoreDialogueClient.MaximumTextLength)
            return "dialogue.invalid_request";
        Hero recipient = flow.Recipient;
        if (recipient == null || recipient == Hero.MainHero || recipient.CharacterObject?.IsHero != true
            || recipient.IsDead || IsHeroInPlayerPartyForCourier(recipient)
            || (!flow.AllowLetterReply && !ShouldShowCourierButtonForExternal(recipient, informationHidden: false)))
            return "courier.recipient_unavailable";
        if (HasActiveCourierForHero(recipient)) return "courier.recipient_busy";
        TroopRoster current = MobileParty.MainParty?.MemberRoster;
        if (current == null || flow.CrewRoster == null || flow.CrewRoster.TotalManCount <= 0)
            return "courier.crew_unavailable";
        foreach (TroopRosterElement selected in SnapshotRoster(flow.CrewRoster))
        {
            CharacterObject character = selected.Character;
            int index = character == null ? -1 : current.FindIndexOfTroop(character);
            if (index < 0 || character.IsPlayerCharacter || selected.Number <= 0)
                return "courier.crew_unavailable";
            TroopRosterElement available = current.GetElementCopyAtIndex(index);
            if (available.Number < selected.Number || available.WoundedNumber < selected.WoundedNumber
                || available.Number - available.WoundedNumber < selected.Number - selected.WoundedNumber
                || available.Xp < selected.Xp)
                return "courier.crew_unavailable";
        }
        switch (flow.Mode)
        {
            case CourierPayloadMode.Normal:
                return flow.SelectedEntries.Count == 0 ? null : "courier.payload_unavailable";
            case CourierPayloadMode.Give:
            case CourierPayloadMode.Show:
                break;
            case CourierPayloadMode.GiveTroops:
            case CourierPayloadMode.GivePrisoners:
                if (!MyBehavior.IsPartyTransferLordEligibleForExternal(recipient, recipient.CharacterObject))
                    return "courier.mode_ineligible";
                break;
            case CourierPayloadMode.GiveSettlements:
                if (!MyBehavior.IsSettlementTransferLeaderEligibleForExternal(recipient, recipient.CharacterObject))
                    return "courier.mode_ineligible";
                break;
            default: return "courier.mode_ineligible";
        }
        if (flow.SelectedEntries.Count == 0) return "courier.payload_unavailable";
        var stock = new Dictionary<Tuple<string, string, string>, int>();
        foreach (CourierTradeOption option in BuildCourierTradeOptions(flow, flow.Mode))
        {
            var key = CourierCargoIdentity(option.Kind, option.Id, option.PartyEntry?.SourceSettlement?.StringId);
            // A duplicate option cannot multiply the underlying stock.
            if (!stock.ContainsKey(key)) stock.Add(key, option.AvailableAmount);
        }
        foreach (CourierCargoEntry entry in flow.SelectedEntries)
        {
            if (entry == null || entry.Amount <= 0) return "courier.payload_unavailable";
            var key = CourierCargoIdentity(entry.Kind, entry.Id, entry.SourceSettlementId);
            if (!stock.TryGetValue(key, out int remaining) || remaining < entry.Amount)
                return "courier.payload_unavailable";
            stock[key] = remaining - entry.Amount;
        }
        return null;
    }

    private static Tuple<string, string, string> CourierCargoIdentity(string kind, string id, string source)
        => Tuple.Create((kind ?? "").Trim().ToLowerInvariant(), (id ?? "").Trim().ToLowerInvariant(),
            (source ?? "").Trim().ToLowerInvariant());
}
