using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

internal interface IWorldDiplomacyLobbyistEvidence
{
    IReadOnlyList<DiplomacySponsorReceipt> PlayerGoldReceipts(string ruler);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    internal void ObserveSponsorPayment(string id, string ruler, int gold)
    {
        if (!_persistenceHealthy || !_host.WorldDiplomacyEnabled() || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(ruler) || gold <= 0) return;
        bool eligible = _host.AllKingdomIds().Any(x => DialogueRulerIsCurrent(x, ruler)
            && !_host.IsPlayerParty(x) && !_host.IsEliminatedParty(x) && _host.HasIndependentAuthority(x));
        if (!eligible) return;
        Storage.SponsorReceipts ??= new();
        if (Storage.SponsorReceipts.Any(x => x?.Id == id)) return;
        // Both transfer adapters call here after money moved but before this
        // transfer's AFEF is written. Import pre-existing facts at that boundary;
        // a newly observed transfer is never an alias of an older imported one.
        try { SponsorPayments(ruler); }
        catch (Exception ex) { _host.Log("sponsor prior evidence unavailable: " + ex.GetType().Name); }
        Storage.SponsorReceipts.Add(new DiplomacySponsorReceipt { Id = id, Ruler = ruler, Gold = gold, Day = _host.CurrentDay() });
        // Observed transfers only. Never debit, reserve, refund, or write memory.
        Storage.SponsorReceipts.RemoveAll(x => x != null && x.Day < _host.CurrentDay() - 365 && string.IsNullOrEmpty(x.UsedBy)
            && !Storage.DialogueArrangements.Any(a => a.SponsorReceipt == x.Id || a.RulerId == x.Ruler && a.SponsorGold > 0
                && (a.Status == "waiting_condition" || a.Status == "waiting_clarification")));
    }

    private IReadOnlyList<DiplomacySponsorReceipt> SponsorPayments(string ruler)
    {
        Storage.SponsorReceipts ??= new();
        var typed = (Storage.SponsorReceipts ?? new()).Where(x => x != null && x.Ruler == ruler && x.Gold > 0 && !string.IsNullOrEmpty(x.Id)).ToList();
        var historical = _lobbyistRecoveryPass ? Array.Empty<DiplomacySponsorReceipt>()
            : (_host as IWorldDiplomacyLobbyistEvidence)?.PlayerGoldReceipts(ruler) ?? Array.Empty<DiplomacySponsorReceipt>();
        // A typed transfer and its legacy rendered AFEF are the same money. Do
        // not offer both witnesses as separately spendable receipts.
        foreach (var old in historical.Where(x => x != null && x.Ruler == ruler && x.Gold > 0 && !string.IsNullOrEmpty(x.Id)).GroupBy(x => x.Id).Select(x => x.First()))
            if (!typed.Any(t => t.Id == old.Id || !t.Id.StartsWith("afef_gold_", StringComparison.Ordinal)
                && t.Day == old.Day && t.Gold == old.Gold))
            { typed.Add(old); Storage.SponsorReceipts.Add(old); }
        var oldClaims = Storage.DialogueArrangements.Where(x => !string.IsNullOrEmpty(x.SponsorReceipt))
            .GroupBy(x => x.SponsorReceipt).ToDictionary(x => x.Key, x => x.First().ArrangementId, StringComparer.Ordinal);
        foreach (var receipt in typed)
            if (string.IsNullOrEmpty(receipt.UsedBy) && oldClaims.TryGetValue(receipt.Id, out string prior)) receipt.UsedBy = prior;
        return typed;
    }
    private WorldDiplomacyDialogueArrangement _lobbyistSubmission;
    private WorldDiplomacyStorage _lobbyistMaintenanceOwner;
    private int _lobbyistMaintenanceDay = -1;
    private bool _lobbyistRecoveryPass;
    internal bool IsLobbyistMatter(DialogueTagPayload parsed, string actor, string playerAuthority) => parsed != null
        && (string.IsNullOrEmpty(playerAuthority)
            || !string.Equals(parsed.TargetKingdomId, playerAuthority, StringComparison.OrdinalIgnoreCase))
        && !DialogueRoutingRules.UsesLegacyOralVassalage(parsed, actor, playerAuthority);

    // Called from the shared tag dispatcher on the campaign thread. No native
    // player tag is promoted to a promise, and no text extraction can supply one.
    internal bool TryProcessLobbyistTag(string ruler, string actor, string action, string payload, DialogueInteractionOrigin origin)
    {
        if (!_host.WorldDiplomacyEnabled()) return false;
        if (!string.Equals(action, "COMMIT", StringComparison.OrdinalIgnoreCase)) return false;
        if (!DialogueTagPayload.TryParse(payload, out var parsed))
        { _host.Log("lobbyist rejected: invalid_payload"); return true; }
        if (!IsLobbyistMatter(parsed, actor, CurrentPlayerAuthority())) return false;
        if (!_persistenceHealthy || origin == null || string.IsNullOrWhiteSpace(origin.FullNpcText)
            || !DialogueRulerIsCurrent(actor, ruler) || _host.IsPlayerParty(actor) || !_host.HasIndependentAuthority(actor)
            || !_host.PartyResolved(parsed.TargetKingdomId) || _host.IsEliminatedParty(parsed.TargetKingdomId)
            || !_host.HasIndependentAuthority(parsed.TargetKingdomId) || actor == parsed.TargetKingdomId)
        { _host.Log("lobbyist rejected: authority_or_participants"); return true; }
        if (parsed.Move == DialogueDiplomaticMove.Discussion) return true;
        Storage.DialogueArrangements ??= new();
        var sameTurn = Storage.DialogueArrangements.FirstOrDefault(x => x.IsLobbyistCommitment
            && x.RulerId == ruler && x.SourceChannel == origin.Channel && x.SourceSessionId == origin.SessionId
            && x.SourceInteractionId == origin.InteractionId && x.LobbyistPayload == payload);
        if (sameTurn != null && sameTurn.Status != "waiting_condition" && sameTurn.Status != "waiting_clarification") return true;
        var item = sameTurn;
        if (!string.IsNullOrEmpty(parsed.Continuation))
        {
            item = Storage.DialogueArrangements.FirstOrDefault(x => x.ArrangementId == parsed.Continuation
                && x.IsLobbyistCommitment && x.RulerId == ruler && x.ActorKingdomId == actor);
            if (item == null || item.TargetKingdomId != parsed.TargetKingdomId || item.Action != parsed.Action
                || item.Move != parsed.Move || !CompletesLobbyistPayload(item, payload))
            { _host.Log("lobbyist rejected: continuation_mismatch"); return true; }
            if (item.Status != "waiting_condition" && item.Status != "waiting_clarification") return true;
            // A short confirmation cannot erase a payment obligation.
            if (item.SponsorGold > 0 && (parsed.SponsorGold != item.SponsorGold || parsed.Condition != "player_gold"))
            { _host.Log("lobbyist rejected: dropped_sponsorship_condition"); return true; }
        }
        else if (item == null)
        {
            if (Storage.DialogueArrangements.Any(x => x.IsLobbyistCommitment && x.RulerId == ruler
                && x.ActorKingdomId == actor && x.TargetKingdomId == parsed.TargetKingdomId && x.Action == parsed.Action && x.Move == parsed.Move
                && x.Terms?.ToTerms().Equals(parsed.Terms) == true && x.SourceDocumentId == parsed.SourceDocumentId && x.SourceActionId == parsed.SourceActionId
                && x.SponsorGold == parsed.SponsorGold && (string.IsNullOrEmpty(parsed.SponsorReceipt) || x.SponsorReceipt == parsed.SponsorReceipt)
                && (string.IsNullOrEmpty(parsed.EventRoundId) || x.RoundId == parsed.EventRoundId)
                && (x.Status is "accepted" or "deferred" || x.Status == "published" && IsLiveRound(ResolveRound(x.RoundId))
                    || x.Status == "executed" && x.CreatedDay == _host.CurrentDay()))) return true;
            // Bind exact outstanding terms only. Multiple matches require an ID.
            var pending = Storage.DialogueArrangements.Where(x => x.IsLobbyistCommitment && x.RulerId == ruler
                && x.ActorKingdomId == actor && x.TargetKingdomId == parsed.TargetKingdomId && x.Action == parsed.Action
                && (x.Status == "waiting_condition" || x.Status == "waiting_clarification")).Take(2).ToList();
            if (pending.Count > 0)
            { _host.Log("lobbyist held: exact_continuation_required"); return true; }
        }
        if (item == null)
        {
            item = new WorldDiplomacyDialogueArrangement { ArrangementId = _host.NewId("dialogue_arrangement"),
                IsLobbyistCommitment = true, LobbyistPayload = payload, LobbyistCondition = parsed.Condition,
                SponsorGold = parsed.SponsorGold, RulerId = ruler, ActorKingdomId = actor,
                TargetKingdomId = parsed.TargetKingdomId, Action = parsed.Action, Move = parsed.Move,
                Terms = WorldDiplomacyDialogueTerms.From(parsed.Terms), SourceDocumentId = parsed.SourceDocumentId,
                SourceActionId = parsed.SourceActionId, SourceInteractionId = origin.InteractionId,
                SourceChannel = origin.Channel, SourceSessionId = origin.SessionId,
                SourcePlayerText = origin.PlayerText, SourceNpcText = origin.NpcText,
                Status = "waiting_clarification", CreatedDay = _host.CurrentDay() };
            Storage.DialogueArrangements.Add(item);
        }
        if (item.LobbyistCondition == "clarify" && parsed.Condition != "clarify" && parsed.Condition != "player_gold")
        { item.Reason = "unrepresented_condition_requires_supported_evidence"; return true; }
        item.LobbyistPayload = payload;
        item.LobbyistCondition = parsed.Condition;
        item.Terms = WorldDiplomacyDialogueTerms.From(parsed.Terms);
        item.SourceDocumentId = parsed.SourceDocumentId; item.SourceActionId = parsed.SourceActionId;
        item.Status = "waiting_clarification";
        item.SourcePlayerText = origin.PlayerText; item.SourceNpcText = origin.NpcText;
        item.Reason = "terms_or_condition_require_clarification";
        if (!DialogueTermsValidation.Validate(parsed.Action, parsed.Terms, actor, parsed.TargetKingdomId, out var error)
            && parsed.Move == DialogueDiplomaticMove.NewMatter)
        { item.Reason = error; return true; }
        if (parsed.Condition == "clarify") return true;
        if (parsed.Move != DialogueDiplomaticMove.NewMatter && string.IsNullOrEmpty(parsed.SourceDocumentId))
        { item.Reason = "exact_source_proposal_required"; return true; }
        if (parsed.Move != DialogueDiplomaticMove.NewMatter && QueryAnswerableProposals(ruler, actor, parsed.TargetKingdomId,
            parsed.Action, new DialogueProposalReference("", parsed.SourceDocumentId, parsed.SourceActionId)).Proposals
            .Count(x => x.IsOpen && x.IsKnownByRuler && x.IsCurrentlyExecutable) != 1)
        { item.Reason = "exact_source_proposal_invalid"; return true; }
        if (parsed.Move == DialogueDiplomaticMove.NewMatter && !string.IsNullOrEmpty(parsed.EventRoundId) && !ValidLobbyistEvent(parsed.EventRoundId, actor, parsed.TargetKingdomId))
        { item.Reason = "event_not_known_live_or_matching_participants"; return true; }
        if (parsed.Condition == "player_gold")
        {
            item.SponsorGold = parsed.SponsorGold;
            IReadOnlyList<DiplomacySponsorReceipt> receipts;
            try { receipts = SponsorPayments(ruler); }
            catch (Exception ex)
            { item.Reason = "registration_retry_" + ex.GetType().Name; return true; }
            var eligible = receipts.Where(x => x.Ruler == ruler && x.Gold >= item.SponsorGold
                && (string.IsNullOrEmpty(x.UsedBy) || x.UsedBy == item.ArrangementId)
                && (x.Day >= item.CreatedDay - 1 || !string.IsNullOrEmpty(parsed.SponsorReceipt)) && x.Day <= _host.CurrentDay()
                && (string.IsNullOrEmpty(parsed.SponsorReceipt) || x.Id == parsed.SponsorReceipt)
                && !Storage.DialogueArrangements.Any(a => a != item && a.SponsorReceipt == x.Id))
                .GroupBy(x => x.Id).Select(x => x.First()).Take(2).ToList();
            if (eligible.Count != 1)
            {
                item.Status = "waiting_condition";
                item.Reason = eligible.Count == 0 ? "actual_player_payment_required" : "exact_payment_receipt_required";
                return true;
            }
            item.SponsorReceipt = eligible[0].Id;
            eligible[0].UsedBy = item.ArrangementId;
        }
        var previous = _lobbyistSubmission;
        try
        {
            _lobbyistSubmission = item;
            var detached = new DialogueInteractionOrigin(origin.Channel, origin.InteractionId, origin.SessionId,
                origin.FullPlayerText, origin.FullNpcText, false);
            string result = SubmitReviewedOralCommitment(ruler, actor, payload, detached, out bool registered, parsed.EventRoundId);
            if (!registered && item.Reason == "terms_or_condition_require_clarification") item.Reason = "registration_validation_pending";
            if (registered) LinkLegacyLobbyistEvidence(item, parsed);
            _host.Log("lobbyist commitment id=" + item.ArrangementId + " registered=" + registered + " reason=" + item.Reason);
        }
        catch (Exception ex)
        {
            // Once the original item has a round it belongs to the existing
            // declaration recovery owner. Never create a replacement task.
            if (!string.IsNullOrEmpty(item.RoundId) && item.Status == "accepted") item.Status = "deferred";
            item.Reason = "registration_retry_" + ex.GetType().Name;
            _host.Log("lobbyist retained id=" + item.ArrangementId + " reason=" + item.Reason);
        }
        finally { _lobbyistSubmission = previous; }
        return true;
    }

    private void LinkLegacyLobbyistEvidence(WorldDiplomacyDialogueArrangement item, DialogueTagPayload parsed)
    {
        foreach (var old in Storage.SpeechReviews.Where(x => x.Status == "legacy_lobbyist_evidence"
            && x.Ruler == item.RulerId && x.Actor == item.ActorKingdomId))
        {
            var tags = System.Text.RegularExpressions.Regex.Matches(old.TagHints ?? "",
                @"\[ACTION:DIPLOMACY:COMMIT:([^\]]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (tags.Count == 1 && DialogueTagPayload.TryParse(tags[0].Groups[1].Value, out var prior)
                && prior.Action == parsed.Action && prior.Move == parsed.Move && prior.TargetKingdomId == parsed.TargetKingdomId
                && prior.Terms.Equals(parsed.Terms) && prior.SourceDocumentId == parsed.SourceDocumentId && prior.SourceActionId == parsed.SourceActionId
                && (string.IsNullOrEmpty(prior.EventRoundId) || prior.EventRoundId == item.RoundId)
                && (!prior.HasLobbyistFields || prior.Condition == parsed.Condition && prior.SponsorGold == parsed.SponsorGold))
            { old.Status = "continued_by_tag"; old.Reason = item.ArrangementId; }
        }
    }

    private static bool CompletesLobbyistPayload(WorldDiplomacyDialogueArrangement item, string next)
    {
        var fields = next.Split(';').Select(x => x.Split(new[] { '=' }, 2))
            .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);
        foreach (var part in item.LobbyistPayload.Split(';'))
        {
            var pair = part.Split(new[] { '=' }, 2);
            string key = pair[0].Trim().ToLowerInvariant(), value = pair[1].Trim();
            if (key == "continuation" || key == "sponsor_receipt" || value == "" || key == "condition" && value == "clarify") continue;
            // Only program-rejected fields may be explicitly corrected. All
            // determined terms and payment obligations remain immutable.
            bool invalid = item.Reason switch {
                "event_not_known_live_or_matching_participants" => key == "event",
                "exact_source_proposal_invalid" => key == "source_document" || key == "source_action",
                "peace_duration_requires_explicit_tribute_period" or "action_has_no_fixed_duration" => key == "days",
                "treaty_roles_must_match_participants" or "unexpected_treaty_roles" => key == "receiving" || key == "joining",
                "tribute_requires_peace_participant_roles" => key == "payer" || key == "receiver",
                "cession_requires_peace_participant_roles" => key == "cession_from" || key == "cession_to",
                _ => false };
            if (invalid && fields.ContainsKey(key)) continue;
            if (!fields.TryGetValue(key, out string current) || !string.Equals(value, current, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private void MaintainLobbyistPending()
    {
        if (ReferenceEquals(_lobbyistMaintenanceOwner, Storage) && _lobbyistMaintenanceDay == _host.CurrentDay()) return;
        _lobbyistMaintenanceOwner = Storage; _lobbyistMaintenanceDay = _host.CurrentDay();
        foreach (var item in Storage.DialogueArrangements.Where(x => x.IsLobbyistCommitment
            && (x.Status == "waiting_condition" || x.Reason.StartsWith("registration_retry_", StringComparison.Ordinal))
            && x.GenerationRetryAfterDay <= _host.CurrentDay()).OrderBy(x => x.GenerationRetryAfterDay).ThenBy(x => x.CreatedDay).Take(4).ToList())
        {
            item.GenerationRetryAfterDay = _host.CurrentDay() + 1;
            if (!DialogueRulerIsCurrent(item.ActorKingdomId, item.RulerId))
            { item.Status = "waiting_clarification"; item.Reason = "original_ruler_no_longer_authorized"; continue; }
            string payload = item.LobbyistPayload;
            if (payload.IndexOf("continuation=", StringComparison.OrdinalIgnoreCase) < 0) payload += ";continuation=" + item.ArrangementId;
            try
            {
                _lobbyistRecoveryPass = true;
                TryProcessLobbyistTag(item.RulerId, item.ActorKingdomId, "COMMIT", payload,
                    new DialogueInteractionOrigin(item.SourceChannel, item.SourceInteractionId, item.SourceSessionId,
                        item.SourcePlayerText, item.SourceNpcText, false));
            }
            finally { _lobbyistRecoveryPass = false; }
        }
    }

    private string BuildLobbyistContext(string ruler, string actor)
    {
        EnsureSpeechReviewIndex();
        var body = new System.Text.StringBuilder(DiplomacyPromptTexts.Get("Lobbyist_Conversation", ""));
        foreach (var item in Storage.DialogueArrangements.Where(x => x.IsLobbyistCommitment && x.RulerId == ruler
            && (x.Status == "waiting_condition" || x.Status == "waiting_clarification")).Take(12))
            body.AppendLine("\n" + DiplomacyPromptTexts.Get("Lobbyist_Pending", "") + Newtonsoft.Json.JsonConvert.SerializeObject(new {
                continuation = item.ArrangementId, payload = item.LobbyistPayload, status = item.Status, reason = item.Reason }));
        var receipts = SponsorPayments(ruler);
        foreach (var receipt in (receipts ?? Array.Empty<DiplomacySponsorReceipt>()).Where(x => x.Day >= _host.CurrentDay() - 14)
            .GroupBy(x => x.Id).Select(x => x.First()).Take(12))
            body.AppendLine("\n" + DiplomacyPromptTexts.Get("Lobbyist_Receipt", "") + Newtonsoft.Json.JsonConvert.SerializeObject(new {
                sponsor_receipt = receipt.Id, gold = receipt.Gold, recipient = receipt.Ruler, day = receipt.Day,
                used = !string.IsNullOrEmpty(receipt.UsedBy) }));
        foreach (var old in Storage.SpeechReviews.Where(x => x.Ruler == ruler && x.Actor == actor
            && x.Status == "legacy_lobbyist_evidence").Take(4))
            body.AppendLine("\n" + DiplomacyPromptTexts.Get("Lobbyist_Legacy", "") + Newtonsoft.Json.JsonConvert.SerializeObject(new {
                original_player = old.PlayerText, original_npc = old.NpcText, old.Reason }));
        return body.ToString();
    }

    private bool ValidLobbyistEvent(string id, string actor, string target)
    {
        var round = ResolveRound(id);
        return IsLiveRound(round)
            && (Storage.EventAnnouncements.Any(x => x.EventId == id && x.State == "public")
                && round.Participants.Any(x => x.KingdomId == actor) && round.Participants.Any(x => x.KingdomId == target)
            || Storage.Documents.Any(x => x.RoundId == id && x.IsReadyForPublication
                && DialogueDocumentKnown(actor, x.DocumentId)
                && (x.AuthorKingdomId == actor && (x.TargetKingdomId == target || x.Actions?.Any(a => a.TargetKingdomId == target) == true)
                    || x.AuthorKingdomId == target && (x.TargetKingdomId == actor || x.Actions?.Any(a => a.TargetKingdomId == actor) == true))));
    }
}
