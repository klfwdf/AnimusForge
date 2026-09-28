using System;
using System.Collections.Generic;
using AnimusForge;

internal static class AdmissionBindingReplay
{
    private sealed class Reads { internal int Documents, Policies, Representatives, Prestige; }
    private struct AdmissionPort : IWorldDiplomacyNoActionPort
    {
        internal Reads Reads;
        internal WorldDiplomacyDocument Root;
        internal bool Player;
        public bool AuthorResolved => true;
        public bool TargetResolved => true;
        public bool SameParty => false;
        public bool AuthorIsPlayer => Player;
        public bool AuthorEliminated => false;
        public bool TargetEliminated => false;
        public bool AuthorHasAuthority => true;
        public bool TargetHasAuthority => true;
        public string AuthorId => "a";
        public string TargetId => "b";
        public int MaxParticipants => 6;
        public WorldDiplomacyDocument ResolveDocument(string id) { Reads.Documents++; return Root; }
        public bool IsRepresentativeFor(WorldDiplomacyDocument document) { Reads.Representatives++; return false; }
    }
    private sealed class BindingPort : IWorldDiplomacyThreatBindingPort
    {
        internal Reads Reads = new();
        internal WorldDiplomacyRound Round = new();
        internal bool Active = true;
        public WorldDiplomacyRound ResolveRound(string id) => Round;
        public bool IsPolicyActive(string policy, string owner, string affected) { Reads.Policies++; return Active; }
        public string RepresentativeId(string id) { Reads.Representatives++; return id == "vassal-b" ? "b" : id; }
        public int CurrentDay() => 12;
        public string NewId(string kind) => "threat";
        public int EscalationPrestigeReward => 3;
        public int WarPrestigeReward => 4;
        public void ApplyPrestige(string id, int delta, WorldDiplomacyDocument doc, string reason) { Reads.Prestige++; }
        public void ResolveCompliance(WorldDiplomacyDocument doc, string target, string issuer) => throw new Exception("unexpected compliance");
        public void Log(string message) { }
    }
    internal static void Run()
    {
        var reads = new Reads();
        var port = new AdmissionPort { Reads = reads, Player = true,
            Root = new WorldDiplomacyDocument { DocumentId = "root", IsReadyForPublication = true, Intent = "warning" } };
        var round = new WorldDiplomacyRound { RoundId = "r", RootDocumentId = "root", State = "active",
            RelayPlanned = true, RelayWaiting = true, RelayRouteKingdomIds = new List<string> { "a", "b" }, RelayCursor = 0 };
        Test.True(!WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, true) && reads.Documents == 0,
            "player rejection does not resolve documents or enumerate routes");
        port.Player = false;
        Test.True(WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, true), "current AI relay cursor may decline an actionable root");
        round.RelayCursor = 1;
        Test.True(!WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, true), "a delayed cursor cannot authorize no-action");
        round.ResultSettlementPending = true; round.ResultSettlementCurrentSlotId = "slot";
        round.ResultSettlementSlots.Add(new WorldDiplomacyResultSettlementSlot { SlotId = "slot", KingdomId = "a", RelatedKingdomIds = new List<string> { "b" } });
        Test.True(WorldDiplomacyNoActionApplication.IsAllowed(round, "slot", port, true)
            && !WorldDiplomacyNoActionApplication.IsAllowed(round, "stale", port, true), "settlement authorization binds the current slot");
        round.ResultSettlementSlots[0].RelatedKingdomIds[0] = "c";
        Test.True(!WorldDiplomacyNoActionApplication.IsAllowed(round, "slot", port, true), "route membership cannot bypass explicit related targets");
        round.ResultSettlementPending = false; round.RelayPlanned = false;
        var response = new WorldDiplomacyDocument { DocumentId = "reply", RoundId = "r", IsReadyForPublication = true,
            IsPlayerAuthored = true, AuthorKingdomId = "b", TargetKingdomId = "a" };
        round.Participants.Add(new WorldDiplomacyRoundParticipant { KingdomId = "a", MandatoryReplyPending = true, LastTriggeredDocumentId = "reply" });
        Test.True(WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, false, true, response), "exact mandatory external response may decline");
        response.RoundId = "other"; reads.Representatives = 0;
        Test.True(!WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, false, true, response) && reads.Representatives == 0,
            "foreign-round source rejects before representative lookup");
        response.RoundId = "r"; response.AuthorKingdomId = "c";
        Test.True(!WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, false, true, response), "source author cannot be substituted");
        response.AuthorKingdomId = "b"; round.Participants[0].LastTriggeredDocumentId = "newer";
        Test.True(!WorldDiplomacyNoActionApplication.IsAllowed(round, "", port, false, true, response), "older response cannot discharge the new obligation");

        var binding = new BindingPort();
        var document = new WorldDiplomacyDocument { DocumentId = "d", RoundId = "r", Intent = "warning",
            AuthorKingdomId = "a", TargetKingdomId = "b", ProcessingActionId = "act" };
        Test.True(!WorldDiplomacyThreatBindingApplication.TryResolvePolicyConditionForThreat(document, "a", "b", binding, out _), "no signal means no policy binding");
        var signal = new WorldDiplomacyPolicySignal { SignalKey = "s", PolicyId = "p", PolicyKind = "kingdom", IssuerKingdomId = "vassal-b", TargetKingdomId = "a" };
        binding.Round.AttachedPolicySignals.Add(signal); binding.Active = false;
        Test.True(!WorldDiplomacyThreatBindingApplication.TryResolvePolicyConditionForThreat(document, "a", "b", binding, out _) && binding.Reads.Representatives == 0,
            "inactive policy is excluded before live representative resolution");
        binding.Active = true;
        Test.True(WorldDiplomacyThreatBindingApplication.TryResolvePolicyConditionForThreat(document, "a", "b", binding, out var selected) && ReferenceEquals(signal, selected),
            "active policy uses representatives to bind the opposite threat parties");
        binding.Round.AttachedPolicySignals.Add(new WorldDiplomacyPolicySignal { SignalKey = "s2", PolicyId = "p2", PolicyKind = "kingdom", IssuerKingdomId = "b", TargetKingdomId = "a" });
        Test.True(!WorldDiplomacyThreatBindingApplication.TryResolvePolicyConditionForThreat(document, "a", "b", binding, out _), "ambiguous active policies cannot select an arbitrary condition");
        binding.Round.AttachedPolicySignals.RemoveAt(1);
        var storage = new WorldDiplomacyStorage();
        WorldDiplomacyThreatBindingApplication.Process(storage, document, "a", "b", false, binding);
        Test.True(storage.DiplomaticThreats.Count == 1 && storage.DiplomaticThreats[0].PolicyConditionPolicyId == "p"
            && storage.DiplomaticThreats[0].StageActionId == "act", "Application dispatch registers exact action and policy");
        int policyReads = binding.Reads.Policies;
        WorldDiplomacyThreatBindingApplication.Process(storage, document, "a", "b", false, binding);
        Test.True(storage.DiplomaticThreats.Count == 1 && binding.Reads.Policies == policyReads, "duplicate stage avoids policy resolution and new records");
        storage.DiplomaticThreats[0].TargetDecision = "noncomplied";
        document.DocumentId = "ult"; document.Intent = "ultimatum"; document.ProcessingActionId = "ult-action";
        WorldDiplomacyThreatBindingApplication.Process(storage, document, "a", "b", false, binding);
        Test.True(storage.DiplomaticThreats.Count == 1 && storage.DiplomaticThreats[0].Stage == "ultimatum"
            && storage.DiplomaticThreats[0].PolicyConditionPolicyId == "p" && binding.Reads.Prestige == 1, "escalation retains bound policy and rewards once");
        WorldDiplomacyThreatBindingApplication.Process(storage, document, "a", "b", false, binding);
        Test.True(binding.Reads.Prestige == 1, "duplicate ultimatum cannot repeat prestige");
        storage.DiplomaticThreats[0].TargetDecision = "noncomplied";
        document.DocumentId = "war"; document.Intent = "declare_war"; document.ChangedDiplomaticState = true;
        WorldDiplomacyThreatBindingApplication.Process(storage, document, "a", "b", false, binding);
        Test.True(storage.DiplomaticThreats[0].Status == "enforced" && storage.DiplomaticThreats[0].ResolutionDocumentId == "war" && binding.Reads.Prestige == 2,
            "confirmed war settles enforcement after its prestige effect");
        WorldDiplomacyThreatBindingApplication.Process(storage, document, "a", "b", false, binding);
        Test.True(binding.Reads.Prestige == 2, "duplicate war completion cannot reward enforced threat twice");
    }
}
