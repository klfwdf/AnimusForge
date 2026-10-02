using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class OfferActionReplay
{
    private sealed class Port : IWorldDiplomacyOfferActionPort
    {
        internal readonly List<string> Events = new();
        internal readonly WorldDiplomacyRound Round = new() { RoundId = "r" };
        internal readonly WorldDiplomacyDocument Source = new() { DocumentId = "source", PeaceTerms = new() { DailyTribute = 50 } };
        internal bool TermsValid = true, Applied = true, Throw, EffectAfterThrow, MissingParty;
        internal WorldDiplomacyCessionReceipt Cession = new(false, false, true, "");
        internal bool TermsComplete = true;
        public WorldDiplomacyStorage Storage { get; } = new();
        public int CurrentDay => 42;
        public WorldDiplomacyRound ResolveRound(string id) => Round;
        public WorldDiplomacyDocument ResolveDocument(string id) { Events.Add("source"); return Source; }
        public bool ResolveParties(WorldDiplomacyRoundOffer o) { Events.Add("parties"); return !MissingParty; }
        public WorldDiplomacyOfferActionReceipt ExecutePeace(string a, string b, WorldDiplomacyPeaceTerms terms)
        {
            Test.True(!ReferenceEquals(terms, Source.PeaceTerms) && terms.DailyTribute == 50, "peace executes an exact copied source contract");
            return Effect("peace");
        }
        private WorldDiplomacyOfferActionReceipt Effect(string kind)
        { Events.Add(kind); if (Throw) throw new InvalidOperationException("effect failed"); return new(Applied, Applied ? "ok" : "failed", TermsComplete); }
        public WorldDiplomacyCessionReceipt ApplyCession(string a, string b, WorldDiplomacyPeaceTerms terms)
        {
            Test.True(Storage.LastPeaceDayByPair[WorldDiplomacyRoundLifecycleRules.PairKey(a, b)] == 42,
                "peace bookkeeping precedes cession just as in the predecessor");
            Events.Add("cession"); return Cession;
        }
        public WorldDiplomacyOfferActionReceipt ExecuteAlliance(string a, string b) => Effect("alliance");
        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string a, string b) => Effect("trade");
        public WorldDiplomacyOfferActionReceipt ReadPeace(string a, string b, WorldDiplomacyPeaceTerms terms)
        { Events.Add("readback"); return new(EffectAfterThrow, EffectAfterThrow ? "ok" : "failed", TermsComplete); }
        public bool HasTakenEffect(string intent, string a, string b) { Events.Add("readback"); return EffectAfterThrow; }
        public void Log(string m) => Events.Add("log");
    }
    private sealed class Orch : FakeOrchestration
    {
        private readonly Port _p;
        internal Orch(Port port) { _p = port; }
        public override void PruneInvalidOffers(WorldDiplomacyRound round) => _p.Events.Add("prune");
        public override bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason) { reason = ""; return false; }
        public override bool AreOfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source) { _p.Events.Add("terms"); return _p.TermsValid; }
    }

    internal static void Run()
    {
        foreach (string kind in new[] { "peace", "alliance", "trade" })
        foreach (string outcome in new[] { "success", "failure", "throw", "post-effect-throw", "missing-party", "late", "rejected" })
        {
            var p = new Port { Applied = outcome != "failure", Throw = outcome.Contains("throw"), EffectAfterThrow = outcome == "post-effect-throw", MissingParty = outcome == "missing-party" };
            var orch = new Orch(p);
            var offer = new WorldDiplomacyRoundOffer { Intent = "propose_" + kind, SourceDocumentId = "source", SourceActionId = "a", ProposerKingdomId = "one", TargetKingdomId = "two", Status = "open" };
            p.Round.PendingOffers.Add(offer);
            var response = new WorldDiplomacyDocument { RoundId = "r", DocumentId = "response", Intent = (outcome == "rejected" ? "reject_" : "accept_") + kind,
                AuthorKingdomId = "two", TargetKingdomId = "one", RespondingToOfferDocumentId = "source", RespondingToOfferActionId = outcome == "late" ? "old" : "a" };
            WorldDiplomacyOfferApplication.Settle(response, p, orch);
            string expected = outcome switch { "success" => "accepted", "post-effect-throw" => kind == "peace" ? "accepted" : "partially_executed", "missing-party" => "invalidated", "late" => "open", "rejected" => "rejected", _ => "execution_failed" };
            Test.True(offer.Status == expected, kind + " " + outcome + " preserves offer disposition");
            if (outcome is "failure" or "throw" or "missing-party" or "late" or "rejected")
                Test.True(!response.ChangedDiplomaticState && !p.Events.Contains("cession"), "unsuccessful action cannot publish a successful receipt or cede land");
            if (outcome == "failure") Test.True(response.MechanicalResult == "failed", "effect failure is not overwritten as peace-term invalidation");
            if (outcome == "post-effect-throw" && kind == "peace")
                Test.True(p.Events.Contains("cession") && p.Storage.LastPeaceDayByPair.Count == 1,
                    "confirmed peace after exception must still finish bookkeeping and cession");
            if (outcome == "success")
            {
                var expectedEvents = kind == "peace" ? new[] { "prune", "source", "parties", "terms", "peace", "cession" } : new[] { "prune", "source", "parties", kind };
                Test.True(p.Events.SequenceEqual(expectedEvents), "acceptance owns effect and bookkeeping order");
                int count = p.Events.Count(x => x == kind);
                WorldDiplomacyOfferApplication.Settle(response, p, orch);
                Test.True(p.Events.Count(x => x == kind) == count, "duplicate response cannot repeat a closed offer effect");
            }
        }
        foreach (bool termsValid in new[] { false, true })
        {
            var p = new Port { TermsValid = termsValid, Cession = new(true, false, true, "transfer failed without magic Chinese substring") };
            var orch = new Orch(p);
            var offer = new WorldDiplomacyRoundOffer { Intent = "propose_peace", SourceDocumentId = "source", ProposerKingdomId = "one", TargetKingdomId = "two", Status = "open" };
            p.Round.PendingOffers.Add(offer);
            var response = new WorldDiplomacyDocument { Intent = "accept_peace", AuthorKingdomId = "two", TargetKingdomId = "one", RespondingToOfferDocumentId = "source" };
            WorldDiplomacyOfferApplication.Settle(response, p, orch);
            Test.True(offer.Status == (termsValid ? "partially_executed" : "invalidated"), "drifted terms and partial cession are distinct outcomes");
            Test.True(termsValid || !p.Events.Contains("peace"), "drifted exact terms prevent peace effect");
        }
    }
}
