using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class OralTreatyExecutionReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host, IDiplomacyEventHost
    {
        internal bool Allied, Trading, StateKnown = true;
        internal string Failure = "";
        public override bool PartiesAllied(string first, string second) => Allied;
        public override bool TradeAgreementExists(string first, string second) => Trading;
        public override bool AllianceKnown() => StateKnown;
        public override bool TradeKnown() => StateKnown;
        public override IWorldDiplomacyOfferActionPort OfferAction() => new OfferPort(this);
        public long EventGeneration => 1;
        public string CaptureEventWorld(IReadOnlyCollection<string> participants) => "{}";
        public bool TryReadEventEffect(DiplomacyEventEffectState effect, out float value, out float min, out float max)
        { value = 0; min = -100; max = 100; return true; }
        public void ApplyEventEffectDelta(DiplomacyEventEffectState effect, float delta) { }
        public bool ShowEvent(DiplomacyEventPresentation data, out string imageId, Action acknowledged, IReadOnlyList<string> recentImages)
        { imageId = ""; return true; }
    }

    private sealed class OfferPort : IWorldDiplomacyOfferActionPort, IWorldDiplomacyTimedTradePort
    {
        private readonly Host h;
        internal OfferPort(Host host) => h = host;
        public WorldDiplomacyStorage Storage => h.Owner.CurrentStorage;
        public int CurrentDay => h.CurrentDayValue;
        public WorldDiplomacyRound ResolveRound(string id) => h.Owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => h.Owner.ResolveDocument(id);
        public bool ResolveParties(WorldDiplomacyRoundOffer offer) => true;
        public WorldDiplomacyOfferActionReceipt ExecutePeace(string a, string b, WorldDiplomacyPeaceTerms terms) => new(false, "unused");
        public WorldDiplomacyOfferActionReceipt ReadPeace(string a, string b, WorldDiplomacyPeaceTerms terms) => new(false, "unused");
        public WorldDiplomacyCessionReceipt ApplyCession(string a, string b, WorldDiplomacyPeaceTerms terms) => default;
        public WorldDiplomacyOfferActionReceipt ExecuteAlliance(string a, string b) { h.Effects++; return Execute(true); }
        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string a, string b) => ExecuteTrade(a, b, 0);
        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string a, string b, int days)
        { h.TradeExecutions++; h.TradeDuration = days; return Execute(false); }
        private WorldDiplomacyOfferActionReceipt Execute(bool alliance)
        {
            if (h.Failure == "rejected") return new(false, "effect rejected");
            if (h.Failure == "unknown") return new(false, "readback unavailable", known: false);
            if (alliance) h.Allied = true; else h.Trading = true;
            if (h.Failure == "after_write") throw new InvalidOperationException("after write");
            return new(true, "treaty applied");
        }
        public bool HasTakenEffect(string intent, string a, string b) => intent == "propose_alliance" ? h.Allied : h.Trading;
        public void Log(string message) { }
    }

    private static object? Call(WorldDiplomacyOrchestration owner, string method, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(owner, args);

    private static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        return (h, o);
    }

    private static WorldDiplomacyDocument Draft(WorldDiplomacyOrchestration o, string action, string channel)
    {
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=" + action + ";move=NewMatter;target=p" + (action == "Trade" ? ";days=14" : ""),
            new DialogueInteractionOrigin(channel, "agreed", "session", "I agree to this treaty.", "I agree to this treaty.", true));
        var item = o.CurrentStorage.DialogueArrangements.Single();
        var job = o.CurrentStorage.Jobs.Single(x => x.DialogueArrangementId == item.ArrangementId);
        Call(o, "CommitOralDeclaration", job, new JObject { ["title"] = "Treaty declaration",
            ["body"] = action == "Trade" ? "We agree to trade for 14 days and announce its implementation." : "We agree to form an alliance and announce its implementation.",
            ["actions"] = new JArray() }.ToString());
        return o.ResolveDocument(item.DocumentId);
    }

    // Invoke the real daily/load normalizer with a detached world snapshot.
    private static void Normalize(WorldDiplomacyOrchestration o, WorldDiplomacyDocument document) =>
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(document, o.CurrentStorage, false, 10, 13, 8,
            id => id, (ids, author) => ids.Where(x => !string.IsNullOrEmpty(x) && x != author).Distinct().ToList(),
            day => day.ToString(), Array.Empty<string>(), Array.Empty<string>());

    private static void Open(WorldDiplomacyOrchestration o, WorldDiplomacyRound round)
    {
        object[] args = { round, "started", new JObject {
            ["background"] = "The two rulers agreed to the treaty.", ["positions"] = new JArray(EventBoundaryReplay.Position("a")),
            ["title"] = "Treaty talks", ["body"] = "The two rulers prepare to fulfill their agreement.", ["scene"] = "Negotiation",
            ["effects"] = new JArray(), ["narrative"] = new JArray(), ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray() }, "" };
        Test.True((bool)Call(o, "TryPublishEventBoundary", args)!, "real opening boundary publishes: " + args[3]);
        Call(o, "CommitPendingEventDocuments", round, false);
    }

    internal static void Run()
    {
        foreach (string action in new[] { "Alliance", "Trade" })
        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            var (h, o) = Fixture(); var d = Draft(o, action, channel);
            Test.True(d != null && d.EventExecutionPending && !d.IsReadyForPublication,
                "oral treaty waits for event opening: " + action + ":" + channel);
            h.CurrentDayValue++;
            Normalize(o, d); o.MaintainDialogueAndMemory();
            Test.True(!d.IsReadyForPublication && d.EventExecutionPending && h.Effects + h.TradeExecutions == 0,
                "daily normalization must not publish or consume pending oral treaty: " + action + ":" + channel);
            var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
            var reload = Fixture(); reload.o.ReplaceStorage(saved); o = reload.o; h = reload.h;
            d = o.ResolveDocument(d.DocumentId); Normalize(o, d); o.MaintainDialogueAndMemory();
            Open(o, o.ResolveRound(d.RoundId));
            Test.True(d.ChangedDiplomaticState && h.Effects + h.TradeExecutions == 1
                && o.CurrentStorage.DialogueArrangements.Single().Status == "executed",
                "restored agreement executes without a player acceptance declaration: " + action + ":" + channel
                + JsonConvert.SerializeObject(new { d.IsReadyForPublication, d.EventExecutionPending, d.AnalysisStatus, d.MechanicalResult,
                    item = o.CurrentStorage.DialogueArrangements.Single(), h.Effects, h.TradeExecutions }));
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            d = o.ResolveDocument(d.DocumentId);
            Call(o, "CommitPendingEventDocuments", o.ResolveRound(d.RoundId), false); o.MaintainDialogueAndMemory();
            o.ProcessAnalyzedDocument(d, d.Intent, d.Commitment, false, "", 1);
            Test.True(h.Effects + h.TradeExecutions == 1 && o.CurrentStorage.Documents.Count == 1,
                "repeated maintenance cannot duplicate treaty execution or publication");
            if (action == "Trade") Test.True(h.TradeDuration == 14, "agreed trade duration reaches effect owner unchanged");
        }
        RecoverLegacy();
        FailedAndInterrupted();
        RevalidateBeforeEffect();
        PreserveOrdinaryLegacy();
    }

    private static void RecoverLegacy()
    {
        foreach (string action in new[] { "Alliance", "Trade" })
        foreach (string phase in new[] { "accepted", "published", "already_active" })
        {
            var (h, o) = Fixture(); var d = Draft(o, action, "native");
            var item = o.CurrentStorage.DialogueArrangements.Single();
            item.TreatyExecutionState = ""; item.Status = phase == "accepted" ? "accepted" : "published";
            d.IsReadyForPublication = true; d.EventExecutionPending = phase == "accepted";
            string id = d.DocumentId, body = d.Body;
            var reload = Fixture(); reload.o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            (h, o) = reload; d = o.ResolveDocument(id); item = o.CurrentStorage.DialogueArrangements.Single();
            if (phase == "already_active") { h.Allied = action == "Alliance"; h.Trading = action == "Trade"; h.TradeDuration = 7; }
            o.MaintainDialogueAndMemory(); Open(o, o.ResolveRound(d.RoundId)); o.MaintainDialogueAndMemory();
            Test.True(item.Status == "executed" && h.Effects + h.TradeExecutions == (phase == "already_active" ? 0 : 1),
                "narrow legacy recovery fulfills or reads existing treaty without extra player declaration: " + action + ":" + phase + ":" + item.Reason);
            Test.True(d.DocumentId == id && d.Body == body && o.CurrentStorage.Documents.Count == 1,
                "legacy recovery preserves public document identity and prose");
            if (phase == "already_active") Test.True(!d.ChangedDiplomaticState && h.TradeDuration == 7,
                "existing treaty does not gain a fabricated creation receipt or restarted trade expiry");
            Call(o, "CommitPendingEventDocuments", o.ResolveRound(d.RoundId), false); o.MaintainDialogueAndMemory();
            Test.True(h.Effects + h.TradeExecutions == (phase == "already_active" ? 0 : 1), "legacy recovery is idempotent");
        }
        foreach (string defect in new[] { "consent", "ruler", "version", "terms", "third_party", "closed", "war", "unknown_state", "failure", "uncertain", "withdrawal", "source", "superseded" })
        {
            var (h, o) = Fixture(); var d = Draft(o, "Alliance", "native");
            var item = o.CurrentStorage.DialogueArrangements.Single();
            item.Status = "published"; item.TreatyExecutionState = ""; d.IsReadyForPublication = true; d.EventExecutionPending = false;
            switch (defect)
            {
                case "consent": item.ConfirmedPlayerRulerId = ""; break;
                case "ruler": h.Dead = true; break;
                case "version": d.DialogueArrangementVersion++; break;
                case "terms": d.TreatyTerms.DurationDays = 1; break;
                case "third_party": item.TargetKingdomId = d.TargetKingdomId = "b"; break;
                case "closed": o.ResolveRound(d.RoundId).State = "closed"; break;
                case "war": h.WorldWar = true; break;
                case "unknown_state": h.StateKnown = false; break;
                case "failure": d.MechanicalResult = "known failure"; break;
                case "uncertain": item.Status = "execution_uncertain"; break;
                case "withdrawal": item.IsWithdrawal = true; break;
                case "source": d.SourceDocumentId = "different offer"; break;
                case "superseded": item.SupersededByArrangementId = "later"; break;
            }
            o.MaintainDialogueAndMemory();
            Test.True(!d.EventExecutionPending && h.Effects == 0 && item.TreatyExecutionState != "pending",
                "legacy recovery cannot invent consent or retry conflicting/unknown evidence: " + defect);
        }
    }

    private static void FailedAndInterrupted()
    {
        foreach (string action in new[] { "Alliance", "Trade" })
        foreach (string failure in new[] { "rejected", "unknown", "after_write", "interrupted" })
        {
            var (h, o) = Fixture(); var d = Draft(o, action, "courier"); var item = o.CurrentStorage.DialogueArrangements.Single();
            h.Failure = failure;
            if (failure == "interrupted")
            {
                item.TreatyExecutionState = "started"; d.IsReadyForPublication = true; d.EventExecutionPending = false;
                var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
                var reload = Fixture(); reload.o.ReplaceStorage(saved); (h, o) = reload;
                d = o.ResolveDocument(d.DocumentId); item = o.CurrentStorage.DialogueArrangements.Single();
                o.MaintainDialogueAndMemory();
            }
            Open(o, o.ResolveRound(d.RoundId));
            Test.True(item.TreatyExecutionState == (failure == "rejected" ? "not_applied" : "uncertain"),
                "execution records failure/unknown separately from publication: " + action + ":" + failure);
            if (failure != "rejected") Test.True(item.Status == "execution_uncertain", "unknown result stays held");
            int attempts = h.Effects + h.TradeExecutions;
            h.Failure = ""; o.MaintainDialogueAndMemory(); Call(o, "CommitPendingEventDocuments", o.ResolveRound(d.RoundId), false);
            o.ProcessAnalyzedDocument(d, d.Intent, d.Commitment, false, "", 1);
            Test.True(h.Effects + h.TradeExecutions == attempts && attempts == (failure == "interrupted" ? 0 : 1),
                "maintenance and duplicate callbacks cannot retry failed or unknown effects");
        }
    }

    private static void PreserveOrdinaryLegacy()
    {
        var (_, o) = Fixture();
        var ordinary = new WorldDiplomacyDocument { DocumentId = "legacy", AnalysisStatus = "success", AuthorKingdomId = "a" };
        Normalize(o, ordinary);
        Test.True(ordinary.IsReadyForPublication, "old non-oral analyzed records retain legacy visibility migration");
    }

    private static void RevalidateBeforeEffect()
    {
        foreach (string action in new[] { "Alliance", "Trade" })
        {
            var (h, o) = Fixture(); var d = Draft(o, action, "scene");
            h.Dead = true; // Authority changes while the opening is still waiting.
            Open(o, o.ResolveRound(d.RoundId));
            Test.True(h.Effects + h.TradeExecutions == 0 && !d.EventExecutionPending
                && o.CurrentStorage.DialogueArrangements.Single().TreatyExecutionState == "not_applied",
                "opening rechecks authority before a previously confirmed treaty is executed: " + action);
        }
    }
}
