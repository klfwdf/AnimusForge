using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class ThreatSettlementReplay
{
    private sealed class Port : IWorldDiplomacyThreatSettlementPort
    {
        internal readonly WorldDiplomacyStorage Storage;
        internal readonly List<string> Events = new();
        internal readonly Dictionary<string, WorldDiplomacyConsequenceClan> Clans = new();
        internal readonly Dictionary<string, int> Relations = new();
        internal int Scans, Captures, Reward = 10;
        internal string? ThrowAfter, ThrowBefore;
        internal bool FailPolicy, FailHistory;
        public bool CampaignAvailable => true;
        public int IssuerRelationRewardMax => 100;
        public int UltimatumComplianceRoyalRelationPenalty => -20;
        public int UltimatumCompliancePrestigeChange => 10;
        public int WarningCompliancePrestigeChange => 5;
        public int UltimatumFollowThroughPrestigePenalty => 25;
        public int WarningFollowThroughPrestigePenalty => 10;
        public int ZeroPrestigeUltimatumBreachRelationPenalty => -5;
        public int ZeroPrestigeWarningBreachRelationPenalty => -2;
        public int GetThreatComplianceIssuerRelationReward() => Reward;
        public int CurrentDay() => 27;
        public void Log(string message) { }
        public string KingdomName(string id) => id;
        public Port(WorldDiplomacyStorage storage)
        {
            Storage = storage;
            foreach (string id in new[] { "target", "issuer" })
            {
                Clans[id + "-r"] = new(false, id + "-rh");
                Clans[id + "-v"] = new(false, id + "-vh");
                Relations[id + "-vh"] = 0;
            }
        }
        public WorldDiplomacyConsequenceParty ReadParty(string id) => new(id, false, true);
        public WorldDiplomacyConsequenceSnapshot CaptureConsequenceSnapshot(string id)
        { Captures++; return new(id + "-r", new[] { id + "-v" }); }
        public void PrepareClans(HashSet<string> ids) { Scans++; }
        public WorldDiplomacyConsequenceClan ReadClan(string id) => Clans.GetValueOrDefault(id)!;
        public int ReadRelation(string first, string second) => Relations[Clans[first].LeaderId];
        public void ChangeRelation(string first, string second, int amount)
        {
            first = Clans[first].LeaderId;
            Events.Add("relation:" + first);
            if (ThrowBefore == first) throw new InvalidOperationException("before effect");
            Relations[first] = Math.Clamp(Relations[first] + amount, -100, 100);
            if (ThrowAfter == first) throw new InvalidOperationException("after effect");
        }
        public bool CancelPolicy(string id, string owner, string reason, out string name, out string result)
        { Events.Add("policy:" + id); name = "Policy"; result = "cancelled"; return !FailPolicy; }
        public int ApplyNationalPrestigeDelta(string id, int delta, WorldDiplomacyDocument doc, string reason)
        {
            Events.Add("prestige:" + id);
            return WorldDiplomacyReputationRules.ApplyNationalPrestigeDelta(Storage.NationalPrestigeByKingdom,
                id, delta, doc, reason, KingdomName, _ => { });
        }
        public void ApplyZeroPrestigeBreachRelationPenalty(string id, int amount) => Events.Add("zero:" + amount);
        public WorldDiplomacyDocument ResolveDocument(string id) => Storage.Documents.FirstOrDefault(x => x.DocumentId == id)!;
        public void AppendCanonicalDocumentEvents(WorldDiplomacyDocument doc)
        { Events.Add("canonical:" + doc.DocumentId); if (FailHistory) throw new InvalidOperationException("history"); doc.HistoryDeclarationRecorded = true; doc.HistoryResultRecorded = true; }
        public void ScheduleDeferredCanonicalHistoryRetry(string id) => Events.Add("defer:" + id);
        public void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat)
        { Events.Add("history:" + threat.ThreatId); threat.HistoryResultRecorded = true; }
        public void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat)
        { Events.Add("domestic:" + threat.ThreatId); threat.DomesticPenaltyHistoryRecorded = true; }
        public void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat)
        { Events.Add("reward:" + threat.ThreatId); threat.IssuerRewardHistoryRecorded = true; }
        public void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent decision)
        { Events.Add("noncompliance:" + threat.ThreatId); decision.HistoryRecorded = true; }
    }
    private static WorldDiplomacyThreat Threat(string id = "t") => new()
    {
        ThreatId = id, IssuerKingdomId = "issuer", TargetKingdomId = "target", Stage = "ultimatum",
        StageDocumentId = "stage", StageActionId = "action", Status = "open", TargetDecision = "pending",
        PolicyConditionPolicyId = "p", PolicyConditionOwnerKingdomId = "target"
    };
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        var threat = Threat(); storage.DiplomaticThreats.Add(threat);
        var doc = new WorldDiplomacyDocument { DocumentId = "decision", RoundId = "r", ProcessingActionId = "comply",
            RespondingToThreatDocumentId = "stage", RespondingToThreatActionId = "wrong" };
        storage.Documents.Add(doc);
        var port = new Port(storage);
        
        Test.True(!WorldDiplomacyThreatSettlementApplication.ResolveDiplomaticThreatCompliance(storage, port, doc, "target", "issuer") && port.Events.Count == 0,
            "stale action cannot settle a threat or invoke an effect");
        doc.RespondingToThreatActionId = "action";
        port.ThrowAfter = "target-vh";
        Test.True(WorldDiplomacyThreatSettlementApplication.ResolveDiplomaticThreatCompliance(storage, port, doc, "target", "issuer"), "exact stage compliance settles");
        Test.True(string.Join(",", port.Events) == "prestige:issuer,prestige:target,relation:target-vh,policy:p,relation:issuer-vh",
            "compliance preserves prestige, domestic, policy and issuer effect ordering");
        Test.True(doc.ChangedDiplomaticState && threat.DomesticPenaltyCompleted && threat.IssuerRewardCompleted
            && threat.PolicyConditionCancellationCompleted && threat.DomesticPenaltyAppliedClanIds.SequenceEqual(new[] { "target-v" }),
            "post-effect exception readback counts exactly once and commits all consequences");
        int effects = port.Events.Count;
        WorldDiplomacyThreatSettlementApplication.ResolveDiplomaticThreatCompliance(storage, port, doc, "target", "issuer");
        Test.True(port.Events.Count == effects && port.Scans == 2 && port.Captures == 2,
            "duplicate compliance skips every effect and scan");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        var restoredPort = new Port(restored);
        
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatDomesticPenalties(restored, restoredPort); WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatComplianceConsequences(restored, restoredPort);
        Test.True(restoredPort.Scans == 0 && restoredPort.Events.Count == 0, "save/load retains completed effect boundaries");
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatHistoryResults(restored, restoredPort);
        Test.True(restoredPort.Events[0] == "canonical:decision" && restored.DiplomaticThreats[0].HistoryResultRecorded,
            "history recovery publishes source before consequences");

        threat = Threat(); threat.Status = "complied"; threat.ComplianceDocumentId = doc.DocumentId;
        storage.DiplomaticThreats.Clear(); storage.DiplomaticThreats.Add(threat);
        port.ThrowAfter = null; port.ThrowBefore = "target-vh"; port.Events.Clear();
        Test.True(!WorldDiplomacyThreatSettlementApplication.TryApplyUltimatumComplianceDomesticPenalty(storage, port, threat, "target", out int count) && count == 0,
            "pre-effect failure remains pending");
        int captured = port.Captures;
        port.ThrowBefore = null; port.FailHistory = true;
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatDomesticPenalties(storage, port);
        Test.True(threat.DomesticPenaltyCompleted && port.Captures == captured && port.Events.Contains("defer:decision"),
            "retry uses frozen snapshot and schedules failed canonical append");
        port.FailPolicy = true;
        Test.True(!WorldDiplomacyThreatSettlementApplication.TryApplyDiplomaticThreatPolicyConditionCancellation(storage, port, threat) && !threat.PolicyConditionCancellationCompleted,
            "policy failure preserves retry state");
        port.FailPolicy = false; port.Reward = 0;
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatComplianceConsequences(storage, port);
        Test.True(threat.PolicyConditionCancellationCompleted && threat.IssuerRewardCompleted && threat.IssuerRewardAmount == 0,
            "policy recovery and zero reward terminate without relation effects");

        storage.DiplomaticThreats.Clear(); port.Events.Clear();
        for (int i = 0; i < 12; i++)
        { var pending = Threat("t" + i.ToString("D2")); pending.Status = "complied"; pending.ComplianceDocumentId = "missing"; storage.DiplomaticThreats.Add(pending); }
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatComplianceConsequences(storage, port);
        Test.True(storage.DiplomaticThreats.Count(x => x.PolicyConditionCancellationCompleted) == 8
            && storage.DiplomaticThreats.Count(x => x.IssuerRewardCompleted) == 8,
            "policy and reward retry batches each remain capped at eight");
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatComplianceConsequences(storage, port);
        Test.True(storage.DiplomaticThreats.All(x => x.PolicyConditionCancellationCompleted && x.IssuerRewardCompleted),
            "subsequent batch reaches remaining consequences");
        threat = Threat(); storage.NationalPrestigeByKingdom["issuer"] = 0; port.Events.Clear();
        WorldDiplomacyThreatSettlementApplication.ApplyDiplomaticThreatReputationPenalty(storage, port, threat, doc);
        WorldDiplomacyThreatSettlementApplication.ApplyDiplomaticThreatReputationPenalty(storage, port, threat, doc);
        Test.True(string.Join(",", port.Events) == "prestige:issuer,zero:-5" && threat.ReputationPenaltyApplied,
            "zero-prestige breach penalty settles once and retains extra relation loss");
        threat = Threat(); threat.Status = "complied"; threat.ComplianceDocumentId = "decision";
        port.Clans["target-v"] = new(false, null!);
        Test.True(!WorldDiplomacyThreatSettlementApplication.TryApplyUltimatumComplianceDomesticPenalty(storage, port, threat, "target", out _),
            "leaderless formal clan remains pending instead of being skipped");
        port.Clans["target-v"] = new(false, "target-vh");
        port.Clans.Remove("target-r");
        Test.True(WorldDiplomacyThreatSettlementApplication.TryApplyUltimatumComplianceDomesticPenalty(storage, port, threat, "target", out count)
            && count == 0 && threat.DomesticPenaltySkippedClanIds.Contains("target-v"),
            "missing snapshotted ruling clan completes unresolved consequences as skipped");
    }
}

