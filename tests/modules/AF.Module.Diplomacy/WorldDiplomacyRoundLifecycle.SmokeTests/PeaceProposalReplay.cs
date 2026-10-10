using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

// Actual analysis -> orchestration -> ordered execution -> pending offer owner.
// The game-facing effects use the existing detached host, not a live campaign.
internal static class PeaceProposalReplay
{
    private static JObject LogJson() => new JObject {
        ["status"] = "success", ["title_summary"] = "向南帝国倡议停战和议",
        ["addressed_kingdom_ids"] = new JArray("empire_s"), ["mentioned_kingdom_ids"] = new JArray(),
        ["tone"] = "conciliatory", ["confidence"] = 1.0, ["international_reputation_delta"] = 1,
        ["international_reputation_reason"] = "主动倡议停战以息兵戈",
        ["actions"] = new JArray(new JObject {
            ["responding_to_offer_document_id"] = "", ["responding_to_offer_action_id"] = "",
            ["responding_to_threat_document_id"] = "", ["intent"] = "propose_peace",
            ["commitment"] = "proposal", ["requires_response"] = true,
            ["peace_terms"] = new JObject {
                ["tribute_payer_kingdom_id"] = "", ["tribute_receiver_kingdom_id"] = "",
                ["daily_tribute"] = 0, ["duration_days"] = 0,
                ["cession_from_kingdom_id"] = "", ["cession_to_kingdom_id"] = "",
                ["cession_settlement_id"] = ""
            },
            ["target_kingdom_id"] = "empire_s"
        })
    };

    private sealed class Host : ConcurrentOralMigrationReplay.Host, IWorldDiplomacyAnalysisPort
    {
        internal readonly PeaceAdmissionReplay.Port Peace = new()
            { KingdomIds = new[] { "new_kingdom", "empire_s", "a", "b" }, Owner = "new_kingdom" };
        internal readonly DocumentExecutionReplay.Port ExecutionPort = new();
        private readonly PromptWorldFixture _prompt = new();
        public override bool IsPlayerAffiliatedParty(string id) => id == "new_kingdom";
        public override bool IsPlayerParty(string id) => id == "new_kingdom";
        public override string PlayerKingdomId() => "new_kingdom";
        public override int MaxDiplomaticActionsPerDocument() => 4;
        int IWorldDiplomacyAnalysisPort.MaxAutomaticReplyDepth => 3;
        public override IWorldDiplomacyPeaceAdmissionPort PeaceAdmission() => Peace;
        public override IWorldDiplomacyDocumentExecutionPort DocumentExecution() => ExecutionPort;
        public override IWorldDiplomacyAnalysisPort AnalysisPort() => this;
        public override IWorldDiplomacyPromptWorld PromptWorld() => _prompt;
        public WorldDiplomacyStorage Storage => Owner.CurrentStorage;
        public IWorldDiplomacyDocumentExecutionPort Execution => ExecutionPort;
        public int MaxAutomaticReplyDepth => 3;
        public string KingdomName(string id) => id;
    }

    private static (Host Host, WorldDiplomacyOrchestration Owner, WorldDiplomacyDocument Document) Fixture()
    {
        var host = new Host { PublishEnabled = true };
        var owner = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
        host.Owner = owner; host.ExecutionPort.Owner = owner;
        host.ExecutionPort.PlayerKingdomId = "new_kingdom";
        var round = owner.EnsureActiveRound("new_kingdom", "empire_s", true);
        var document = new WorldDiplomacyDocument { DocumentId = "player-peace", AuthorKingdomId = "new_kingdom",
            TargetKingdomId = "empire_s", RoundId = round.RoundId, Body = "致南帝国：我方提出罢兵停战之倡议。",
            IsPlayerAuthored = true, IsReadyForPublication = true, AnalysisStatus = "pending_analysis" };
        owner.CurrentStorage.Documents.Add(document);
        return (host, owner, document);
    }

    private static void Commit(WorldDiplomacyOrchestration owner, WorldDiplomacyDocument document, JObject json)
        => owner.CommitAnalysis(new() { DocumentId = document.DocumentId, Kind = "analyze" }, json.ToString());

    private static IEnumerable<WorldDiplomacyRoundOffer> Offers(WorldDiplomacyOrchestration owner, WorldDiplomacyDocument doc)
        => owner.ResolveRound(doc.RoundId).PendingOffers.Where(x => x.SourceDocumentId == doc.DocumentId);

    internal static void Run()
    {
        foreach (string shape in new[] { "log", "omitted", "flat", "tribute" })
        {
            var (host, owner, doc) = Fixture(); var json = LogJson();
            if (shape == "omitted") ((JObject)json["actions"][0]).Remove("peace_terms");
            if (shape == "tribute") json["actions"][0]["peace_terms"] = Terms(150, "new_kingdom", "empire_s");
            if (shape == "flat")
            {
                var action = (JObject)json["actions"][0]; json.Remove("actions");
                foreach (var property in action.Properties()) json[property.Name] = property.Value.DeepClone();
            }
            Commit(owner, doc, json);
            var offer = Offers(owner, doc).SingleOrDefault();
            Test.True(doc.AnalysisStatus != "published_action_rejected" && offer?.Status == "open"
                && offer.Intent == "propose_peace", "actual player peace admission registers proposal: " + shape);
            Test.True(offer.SourceActionId == (shape == "flat" ? "" : "action_1")
                && owner.AreOfferedPeaceTermsCurrentlyExecutable(offer, doc), "registered offer retains exact usable identity: " + shape);
            Test.True(!doc.ChangedDiplomaticState && doc.IsReadyForPublication
                && doc.Body.Contains("停战"), "proposal retains public text and does not pretend peace is made: " + shape);
            if (shape == "tribute") Test.True(doc.PeaceTerms.DailyTribute == 150 && doc.PeaceTerms.DurationDays == 100,
                "explicit proposal tribute remains unchanged");
            Commit(owner, doc, json);
            Test.True(Offers(owner, doc).Count() == 1, "duplicate analysis cannot register a second proposal: " + shape);
        }

        foreach (bool peaceFirst in new[] { false, true })
        foreach (bool invalid in new[] { false, true })
        {
            var (host, owner, doc) = Fixture(); var json = LogJson();
            var peace = (JObject)json["actions"][0];
            peace["peace_terms"] = Terms(150, invalid ? "outside" : "new_kingdom", "empire_s");
            var trade = new JObject { ["intent"] = "propose_trade", ["commitment"] = "proposal",
                ["target_kingdom_id"] = "b", ["requires_response"] = true };
            json["actions"] = peaceFirst ? new JArray(peace, trade) : new JArray(trade, peace);
            Commit(owner, doc, json);
            Test.True(invalid ? doc.AnalysisStatus == "published_action_rejected" && !Offers(owner, doc).Any()
                    : Offers(owner, doc).Count() == 2 && doc.Actions.Single(x => x.Intent == "propose_peace").PeaceTerms.DailyTribute == 150,
                "every peace action validates its own terms regardless of order: first=" + peaceFirst + " invalid=" + invalid);
            if (invalid) Test.True(host.ExecutionPort.Events.Any(x => x.StartsWith("notify:") && x.Contains("支付国")
                    && !x.Contains("peace_terms_not_executable")) && doc.MechanicalResult.Contains("支付国"),
                "rejected proposal reports actual clause reason in notification and document");
        }

        foreach (string invalid in new[] { "negative", "bad-number", "duration", "cession" })
        {
            var (host, owner, doc) = Fixture(); var json = LogJson();
            var terms = (JObject)json["actions"][0]["peace_terms"];
            if (invalid == "negative") terms["daily_tribute"] = -1;
            if (invalid == "bad-number") terms["daily_tribute"] = "invalid";
            if (invalid == "duration") terms["duration_days"] = 7;
            if (invalid == "cession") { terms["cession_from_kingdom_id"] = "new_kingdom";
                terms["cession_to_kingdom_id"] = "empire_s"; terms["cession_settlement_id"] = "missing"; }
            Commit(owner, doc, json);
            Test.True(doc.AnalysisStatus == "published_action_rejected" && !Offers(owner, doc).Any()
                && doc.IsReadyForPublication && host.ExecutionPort.Events.Any(x => x.StartsWith("notify:") && x.Contains("和平提案未执行")),
                "real clause rejection still preserves speech and has a readable reason: " + invalid);
            Commit(owner, doc, json);
            Test.True(!Offers(owner, doc).Any(), "duplicate rejected completion cannot execute: " + invalid);
        }

        var (_, pairOwner, pairDoc) = Fixture(); var pairJson = LogJson();
        var first = (JObject)pairJson["actions"][0];
        first["peace_terms"] = Terms(150, "new_kingdom", "empire_s");
        var second = (JObject)first.DeepClone(); second["target_kingdom_id"] = "b";
        second["peace_terms"] = Terms(80, "b", "new_kingdom");
        pairJson["actions"] = new JArray(first, second); Commit(pairOwner, pairDoc, pairJson);
        var pairOffers = Offers(pairOwner, pairDoc).ToList();
        Test.True(pairOffers.Count == 2 && pairOffers.Select(x => x.SourceActionId).SequenceEqual(new[] { "action_1", "action_2" })
            && pairOffers.All(x => pairOwner.AreOfferedPeaceTermsCurrentlyExecutable(x, pairDoc)),
            "different peace targets keep their own exact offer action identities");
        Test.True(pairDoc.Actions[0].PeaceTerms.DailyTribute == 150 && pairDoc.Actions[1].PeaceTerms.DailyTribute == 80
            && pairDoc.Actions[1].PeaceTerms.TributePayerKingdomId == "b", "two peace proposals never borrow each other's terms or direction");

        var (lateHost, lateOwner, lateDoc) = Fixture(); var lateJson = LogJson();
        lateHost.Peace.Owned.Add(new("castle", "new_kingdom", false, false, true, false));
        lateJson["actions"][0]["peace_terms"] = new JObject { ["cession_from_kingdom_id"] = "new_kingdom",
            ["cession_to_kingdom_id"] = "empire_s", ["cession_settlement_id"] = "castle" };
        int ownerReads = 0;
        lateHost.Peace.BeforeOwnerRead = () => { if (++ownerReads == 2) lateHost.Peace.Owner = "b"; };
        Commit(lateOwner, lateDoc, lateJson);
        Test.True(ownerReads == 2 && !Offers(lateOwner, lateDoc).Any() && !lateDoc.Actions[0].ChangedDiplomaticState
            && lateDoc.Actions[0].MechanicalResult.Contains("割地") && !lateDoc.Actions[0].MechanicalResult.Contains("peace_terms_not_executable"),
            "actual effect loop revalidates changed cession and retains readable failure without registering an offer");

        var (liveHost, liveOwner, liveDoc) = Fixture();
        liveHost.Peace.Owned.Add(new("castle", "new_kingdom", false, false, true, false));
        liveDoc.PeaceTerms = new() { CessionFromKingdomId = "new_kingdom", CessionToKingdomId = "empire_s", CessionSettlementId = "castle" };
        Test.True(!liveOwner.TryGetPlayerWorldStateIntentViolation(liveDoc, "propose_peace", "proposal", "new_kingdom", "empire_s", out _),
            "current cession passes the real owner action guard");
        liveHost.Peace.Owner = "b";
        Test.True(liveOwner.TryGetPlayerWorldStateIntentViolation(liveDoc, "propose_peace", "proposal", "new_kingdom", "empire_s", out string staleReason)
            && staleReason.Contains("割地"), "real action guard rechecks live land ownership rather than cached admission");
    }

    private static JObject Terms(int amount, string payer, string receiver) => new()
        { ["daily_tribute"] = amount, ["duration_days"] = 100, ["tribute_payer_kingdom_id"] = payer,
            ["tribute_receiver_kingdom_id"] = receiver };
}
