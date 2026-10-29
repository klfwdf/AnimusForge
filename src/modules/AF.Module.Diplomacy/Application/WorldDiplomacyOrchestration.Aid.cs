using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal interface IDiplomacyAidHost
{
    bool SettleLegacyAidCost(DiplomacyAidRequest request, out string reason);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    internal string ExecutingEventId { get; private set; } = "";
    private WorldDiplomacyStorage _aidStorage;
    private readonly Dictionary<string, DiplomacyAidRequest> _aidByIdentity = new(StringComparer.Ordinal);
    private readonly List<DiplomacyAidRequest> _pendingAid = new();
    private int _aidCursor, _aidHour = -1;
    private void EnsureAidIndex()
    {
        if (ReferenceEquals(Storage, _aidStorage)) return;
        _aidStorage = Storage; Storage.AidRequests ??= new(); _aidByIdentity.Clear(); _pendingAid.Clear();
        foreach (var item in Storage.AidRequests.Where(x => x != null))
        {
            _aidByIdentity[item.Identity] = item;
            if (item.LegacyCostState == "applying") item.LegacyCostState = "uncertain";
            if (item.Status is "requested" or "conditional" or "accepted") _pendingAid.Add(item);
        }
        _aidHour = -1; _aidCursor = 0;
    }
    internal DiplomacyAidRequest CaptureAidRequest(string calling, string called, string enemy, string warIdentity,
        string source, bool accepted = false, int legacyCost = 0, bool playerPaying = false)
    {
        if (!_persistenceHealthy || !_host.WorldDiplomacyEnabled() || string.IsNullOrEmpty(warIdentity)
            || calling == called || calling == enemy || called == enemy || !_host.PartiesAtWar(calling, enemy)
            || !_host.PartiesAllied(calling, called)) return null;
        EnsureAidIndex();
        string identity = calling + "|" + called + "|" + enemy + "|" + warIdentity;
        if (_aidByIdentity.TryGetValue(identity, out var prior))
        {
            if (string.IsNullOrEmpty(prior.MaterialFingerprint)) prior.MaterialFingerprint = AidMaterialFingerprint(calling, called, enemy);
            if (accepted && prior.Status is "requested" or "conditional" or "suggested")
            {
                prior.Status = "accepted"; prior.SourceKind = source; prior.AcceptedRulerId = _host.PartyRulerId(called);
                prior.LegacyCost = Math.Max(0, legacyCost); prior.LegacyPlayerPaying = playerPaying;
                prior.LegacyCostState = legacyCost > 0 ? "pending" : "none";
                if (!_pendingAid.Contains(prior)) _pendingAid.Add(prior); _aidHour = -1;
            }
            else if (source == "player_request" && prior.Status == "suggested")
            { prior.Status = "requested"; prior.SourceKind = source; _pendingAid.Add(prior); _aidHour = -1; }
            bool changed = prior.Status is "rejected" or "closed_unresolved"
                && !string.IsNullOrEmpty(prior.MaterialFingerprint)
                && prior.MaterialFingerprint != AidMaterialFingerprint(calling, called, enemy);
            // A changed world permits a new request, never new consent. Keep the
            // refused version and do not revive a fulfilled or accepted promise.
            if (!changed) return prior;
        }
        var item = new DiplomacyAidRequest { Id = _host.NewId("aid"), Identity = identity, CallingKingdomId = calling,
            CalledKingdomId = called, EnemyKingdomId = enemy, WarIdentity = warIdentity, SourceKind = source,
            Status = _host.PartiesAtWar(called, enemy) ? "fulfilled" : accepted ? "accepted" : source == "player_suggestion" ? "suggested" : "requested",
            AcceptedRulerId = accepted ? _host.PartyRulerId(called) : "", Day = _host.CurrentDay(),
            LegacyCost = accepted ? Math.Max(0, legacyCost) : 0, LegacyPlayerPaying = accepted && playerPaying,
            LegacyCostState = accepted && legacyCost > 0 ? "pending" : "none",
            RoundId = IsLiveRound(ResolveRound(ExecutingEventId)) ? ExecutingEventId : "" };
        item.PreviousRequestId = prior?.Id ?? ""; item.Revision = prior == null ? 0 : prior.Revision + 1;
        item.MaterialFingerprint = AidMaterialFingerprint(calling, called, enemy);
        Storage.AidRequests.Add(item); _aidByIdentity[identity] = item;
        if (item.Status is "requested" or "accepted") _pendingAid.Add(item);
        _aidHour = -1;
        return item;
    }
    internal void RecordNativeAidRefusal(string calling, string called, string enemy)
    {
        EnsureAidIndex();
        foreach (var item in _pendingAid.Where(x => x.CallingKingdomId == calling && x.CalledKingdomId == called
            && x.EnemyKingdomId == enemy && x.Status == "requested"))
        { item.Status = "rejected"; item.Reason = "explicit_native_refusal";
            item.MaterialFingerprint = AidMaterialFingerprint(calling, called, enemy); }
    }
    private void MaintainAidRequests()
    {
        if (!_host.WorldDiplomacyEnabled()) return;
        EnsureAidIndex();
        int hour = _host.CurrentHour(); if (_aidHour == hour) return; _aidHour = hour;
        int count = Math.Min(4, _pendingAid.Count);
        for (int i = 0; i < count && _pendingAid.Count > 0; i++)
        {
            if (_aidCursor >= _pendingAid.Count) _aidCursor = 0;
            var item = _pendingAid[_aidCursor++];
            if (item.Status is not "requested" and not "conditional" and not "accepted") continue;
            if (_host.PartiesAtWar(item.CalledKingdomId, item.EnemyKingdomId)) { FulfillAid(item); continue; }
            if (!_host.PartiesAtWar(item.CallingKingdomId, item.EnemyKingdomId)
                || _host.IsEliminatedParty(item.CalledKingdomId) || _host.IsEliminatedParty(item.EnemyKingdomId))
            { item.Status = "obsolete"; item.Reason = "underlying_war_or_party_no_longer_exists"; continue; }
            var round = ResolveRound(item.RoundId);
            if (round == null)
            {
                bool playerLane = _host.IsPlayerParty(item.CallingKingdomId) || _host.IsPlayerParty(item.CalledKingdomId);
                // A direct player request is an independent authorized matter. NPC
                // normal/player agenda lanes each retain one active event.
                if (item.SourceKind != "player_request" && item.Status != "accepted" && GetLiveRounds().Any(x => IsLiveRound(x)
                    && x.BirthLane == (playerLane ? "player" : "normal"))) continue;
                round = CreateIndependentEventRound(item.CallingKingdomId, item.CalledKingdomId, "", item.Status == "accepted" ? "legacy_accepted_aid" : "alliance_aid", playerLane);
                round.NpcInitiatedPlayerEvent = playerLane && !_host.IsPlayerParty(item.CallingKingdomId) && item.Status != "accepted";
                round.RoundTopic = DiplomacyPromptTexts.Format("Aid_Title", "Aid requested by {CALLER}", "CALLER", _host.PartyNameOrEmpty(item.CallingKingdomId));
                round.ExternalOpeningContext = JsonConvert.SerializeObject(new { alliance_aid = item, ordinary_alliance_is_not_automatic_war_consent = true });
                EnsureRoundParticipant(round, item.CalledKingdomId, "active", true);
                item.RoundId = round.RoundId;
            }
            if (!IsLiveRound(round)) continue;
            if (string.IsNullOrEmpty(item.RequestDocumentId)) PublishAidRequest(item, round);
            if (item.Status != "accepted") continue;
            CommitPendingEventDocuments(round, true);
            if (string.IsNullOrEmpty(item.AnswerDocumentId)) PublishLegacyAidAcceptance(item, round);
            if (_host.PartiesAtWar(item.CalledKingdomId, item.EnemyKingdomId)) { FulfillAid(item); continue; }
            var answer = ResolveDocument(item.AnswerDocumentId);
            if (answer?.IsReadyForPublication != true || item.LastAttemptDay == _host.CurrentDay()) continue;
            item.LastAttemptDay = _host.CurrentDay();
            var actions = answer.Actions ?? new();
            if (DiplomacyConditionalExecutionRules.DeferredActions(actions, ResolveDocument).Count > 0) continue;
            var receipt = ExecuteImmediateIntent(item.CalledKingdomId, item.EnemyKingdomId, "declare_war", answer);
            if (_host.PartiesAtWar(item.CalledKingdomId, item.EnemyKingdomId)) FulfillAid(item);
            else item.Reason = receipt.Message;
        }
        _pendingAid.RemoveAll(x => x.Status is not "requested" and not "conditional" and not "accepted");
    }
    private void PublishAidRequest(DiplomacyAidRequest item, WorldDiplomacyRound round)
    {
        var document = NewAidDocument(item, round, item.CallingKingdomId, item.CalledKingdomId);
        document.Title = round.RoundTopic;
        document.Body = DiplomacyPromptTexts.Format("Aid_Request", "{CALLER} requests that {ALLY} join the existing war against {ENEMY}. This specific request requires a separate decision; our alliance alone does not promise participation. Any aid explicitly accepted must be fulfilled.",
            "CALLER", _host.PartyNameOrEmpty(item.CallingKingdomId), "ALLY", _host.PartyNameOrEmpty(item.CalledKingdomId), "ENEMY", _host.PartyNameOrEmpty(item.EnemyKingdomId));
        document.Intent = "statement"; document.RequiresResponse = true;
        item.RequestDocumentId = document.DocumentId;
        if (string.IsNullOrEmpty(round.RootDocumentId)) round.RootDocumentId = document.DocumentId;
        ProcessAnalyzedDocument(document, "statement", WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("statement"), true, "neutral", 1);
    }
    private void PublishLegacyAidAcceptance(DiplomacyAidRequest item, WorldDiplomacyRound round)
    {
        var document = NewAidDocument(item, round, item.CalledKingdomId, item.EnemyKingdomId);
        document.Title = round.RoundTopic;
        document.Body = DiplomacyPromptTexts.Format("Aid_AcceptedDeclaration", "{ALLY} confirms its already accepted commitment to aid {CALLER} by declaring war on {ENEMY}. The commitment is fulfilled when war actually begins; it sets no minimum war duration.",
            "CALLER", _host.PartyNameOrEmpty(item.CallingKingdomId), "ALLY", _host.PartyNameOrEmpty(item.CalledKingdomId), "ENEMY", _host.PartyNameOrEmpty(item.EnemyKingdomId));
        document.Intent = "declare_war"; document.SourceDocumentId = item.RequestDocumentId;
        item.AnswerDocumentId = document.DocumentId;
        document.AidDecisions.Add(new() { RequestId = item.Id, Decision = "accept" });
        ProcessAnalyzedDocument(document, "declare_war", WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("declare_war"), false, "firm", 1);
    }
    private WorldDiplomacyDocument NewAidDocument(DiplomacyAidRequest item, WorldDiplomacyRound round, string author, string target)
    {
        var document = new WorldDiplomacyDocument { DocumentId = _host.NewId("aid_declaration"), RoundId = round.RoundId, ExchangeId = round.RoundId,
            AuthorKingdomId = author, AuthorKingdomName = _host.PartyNameOrEmpty(author), AuthorRulerId = _host.PartyRulerId(author),
            AuthorRulerName = _host.PartyRulerName(author), TargetKingdomId = target, TargetKingdomName = _host.PartyNameOrEmpty(target),
            AddressedKingdomIds = new() { target }, Day = _host.CurrentDay(), CreatedUtcTicks = DateTime.UtcNow.Ticks,
            GameDate = _host.FormatCampaignDate(_host.CurrentDay()), IsPlayerAuthored = _host.IsPlayerParty(author),
            AnalysisStatus = "success", PlayerLifecycleClassified = true };
        AddDocument(document); return document;
    }
    private bool CaptureAidDecisions(WorldDiplomacyDocument document, JObject json)
    {
        if (json?["aid_decisions"] == null) return !Storage.AidRequests.Any(x => x.RoundId == document.RoundId
            && x.CalledKingdomId == document.AuthorKingdomId && x.RequestDocumentId == document.SourceDocumentId
            && DialogueDocumentKnown(document.AuthorKingdomId, x.RequestDocumentId));
        if (json["aid_decisions"] is not JArray rows || rows.Count > 8) return false;
        var decisions = new List<DiplomacyAidDecision>();
        foreach (var row in rows.OfType<JObject>())
        {
            string id = (string)row["request"] ?? "", choice = (string)row["decision"] ?? "";
            var request = Storage.AidRequests.FirstOrDefault(x => x.Id == id);
            if (request == null || request.RoundId != document.RoundId || request.CalledKingdomId != document.AuthorKingdomId
                || !DialogueDocumentKnown(document.AuthorKingdomId, request.RequestDocumentId)
                || choice is not "accept" and not "reject" and not "conditional") return false;
            if (choice == "accept" && !((document.Intent == "declare_war" && document.TargetKingdomId == request.EnemyKingdomId)
                || document.Actions?.Any(x => x.Intent == "declare_war" && x.TargetKingdomId == request.EnemyKingdomId) == true)) return false;
            if (request.Status == "accepted" && choice != "accept") return false;
            decisions.Add(new() { RequestId = id, Decision = choice });
        }
        if (decisions.Count != rows.Count) return false;
        document.AidDecisions = decisions; return true;
    }
    private void HandleAidPublication(WorldDiplomacyDocument document)
    {
        if (document?.IsReadyForPublication != true) return;
        EnsureAidIndex();
        foreach (var decision in document.AidDecisions ?? new())
        {
            var item = Storage.AidRequests.FirstOrDefault(x => x.Id == decision.RequestId);
            if (item == null || item.RoundId != document.RoundId || item.CalledKingdomId != document.AuthorKingdomId
                || item.Status is "fulfilled" or "rejected" or "obsolete") continue;
            if (item.Status == "accepted" && decision.Decision != "accept") continue;
            item.MaterialFingerprint = AidMaterialFingerprint(item.CallingKingdomId, item.CalledKingdomId, item.EnemyKingdomId);
            item.AnswerDocumentId = document.DocumentId;
            item.Status = decision.Decision == "accept" ? "accepted" : decision.Decision == "reject" ? "rejected" : "conditional";
            if (decision.Decision == "accept") item.AcceptedRulerId = document.AuthorRulerId;
            if (_host.PartiesAtWar(item.CalledKingdomId, item.EnemyKingdomId)) FulfillAid(item);
        }
    }
    private void FulfillAid(DiplomacyAidRequest item)
    {
        // Q50: real war fulfils this call. Never create a native minimum-war or
        // separate-peace agreement as a side effect of the new request.
        if (!_host.PartiesAtWar(item.CalledKingdomId, item.EnemyKingdomId)) return;
        item.Status = "fulfilled"; item.Reason = "actual_war_started";
        if (item.LegacyCostState == "pending" && _host is IDiplomacyAidHost port)
        {
            item.LegacyCostState = "applying";
            try { item.LegacyCostState = port.SettleLegacyAidCost(item, out string reason) ? "complete" : "uncertain"; if (!string.IsNullOrEmpty(reason)) item.Reason = reason; }
            catch (Exception ex) { item.LegacyCostState = "uncertain"; item.Reason = ex.GetType().Name; }
        }
    }
    private void RecordAidCaseClosed(WorldDiplomacyRound round)
    {
        foreach (var item in Storage.AidRequests.Where(x => x.RoundId == round.RoundId && x.Status is "requested" or "conditional"))
        { item.Status = "closed_unresolved"; item.Reason = round.CloseReason;
            item.MaterialFingerprint = AidMaterialFingerprint(item.CallingKingdomId, item.CalledKingdomId, item.EnemyKingdomId); }
    }
    private string BuildAidContext(WorldDiplomacyRound round, string actor)
    {
        EnsureAidIndex();
        var requests = Storage.AidRequests.Where(x => x.RoundId == round.RoundId
            && (x.CallingKingdomId == actor || DialogueDocumentKnown(actor, x.RequestDocumentId))).ToList();
        return requests.Count == 0 ? "" : DiplomacyPromptTexts.Get("Aid_DecisionContract", "") + "\n" + JsonConvert.SerializeObject(requests);
    }
}
