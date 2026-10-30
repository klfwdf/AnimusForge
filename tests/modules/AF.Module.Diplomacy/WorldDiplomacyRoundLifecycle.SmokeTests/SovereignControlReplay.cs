using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Real request, review, publication and offer owners; model answers and game
// effect ports are controlled leaves. No live-provider or game claim.
internal static class SovereignControlReplay
{
    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject value) =>
        o.TryCompleteDiplomacyPipeline(job, new() { Success = true, Content = value.ToString() });
    private static void Prepare(WorldDiplomacyOrchestration o, WorldDiplomacyJob job)
    {
        for (int i = 0; i < 40; i++) { job.RetryAfterUtcTicks = 0; o.PrepareSharedRequest(job); if (!job.RequestPreparationPending) return; }
        throw new Exception("bounded request preparation failed");
    }
    private static JObject Position(string country, bool blank = false) => new() {
        ["kingdom"] = country, ["agenda"] = blank ? "" : "Negotiate border security",
        ["gain"] = blank ? "" : "Secure the frontier", ["cost"] = blank ? "" : "Commit royal resources",
        ["red_line"] = blank ? "" : "Retain sovereignty", ["alternative"] = blank ? "" : "Maintain existing relations",
        ["source_keys"] = new JArray("world:current"), ["address_to"] = blank ? new JArray() : new JArray(country == "a" ? "b" : "a") };

    internal static void Run()
    {
        foreach (var identity in new[] { ("independent", "", ""), ("own_vassal", "a", ""),
            ("foreign_vassal", "c", ""), ("own_mercenary", "a", ""), ("foreign_mercenary", "c", ""), ("monarch", "p", "p") })
        foreach (string channel in new[] { "native", "scene", "courier" })
        foreach (string action in new[] { "DeclareWar", "Alliance", "Trade" })
            Persuasion(identity.Item1, identity.Item2, identity.Item3, channel, action);
        PlanningAndRecovery();
        PromptMemoryReplay.Run();
        // Existing real-owner checks retain the bilateral, already-agreed monarch
        // path, including no second acceptance declaration and duplicate completion.
        OralDeclarationReplay.Run();
    }

    private static void Persuasion(string identity, string affiliation, string sovereign, string channel, string action)
    {
        var h = new ConcurrentOralMigrationReplay.Host { PlayerAffiliation = affiliation, PlayerSovereign = sovereign,
            PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var origin = new DialogueInteractionOrigin(channel, identity + action, "session", "Please act toward b.", "I agree to act toward b.");
        Test.True(!o.CaptureSpeechReview("ordinary_lord", "a", origin, "[ACTION:DIPLOMACY:REVIEW_REMAINING]" + ("")), "ordinary nobles cannot authorize national acts");
        string payload = "action=" + action + ";move=NewMatter;target=b" + (action == "Trade" ? ";days=14" : "");
        Test.True(o.TryProcessLobbyistTag("ruler_a", "a", "COMMIT", payload, origin), "ruler persuasion enters sole tag owner: " + identity + channel + action);
        Test.True(!o.CurrentStorage.Jobs.Any(x => x.Kind is "oral_review" or "oral_review_audit"), "lobbyist never re-enters extraction/audit");
        string context = o.BuildOralArrangementContext("ruler_a", "a");
        Test.True(context.Contains(DiplomacyPromptTexts.Get("Lobbyist_Conversation", "missing")), "selected package lobbyist contract reaches actual conversation consumer");
        var item = o.CurrentStorage.DialogueArrangements.Single();
        Test.True(item.ConfirmedPlayerRulerId == "" && item.ActorKingdomId == "a" && item.TargetKingdomId == "b", "NPC promise is not player or third-country consent");
        Test.True(o.CurrentStorage.Jobs.Count == 1, "publication job: " + identity + "/" + action + " status=" + item.Status + " reason=" + item.Reason);
        var writing = o.CurrentStorage.Jobs.Single(); Prepare(o, writing);
        string intent = action == "DeclareWar" ? "declare_war" : action == "Trade" ? "propose_trade" : "propose_alliance";
        writing.Kind = "generate"; writing.PrivacyApproved = true; // controlled semantic audit
        o.CommitGeneratedDocument(writing, OralDeclarationReplay.Draft(item, intent,
            action == "DeclareWar" ? "The ruler of a formally declares war on b."
                : "The ruler of a proposes cooperation with b, subject to its ruler's acceptance. The trade term is fourteen days."));
        var doc = o.ResolveDocument(item.DocumentId);
        Test.True(doc?.IsReadyForPublication == true && doc.Intent == intent, "agreed NPC action is formally published: " + identity + action);
        if (action == "DeclareWar")
        { Test.True(h.Effects == 1 && !doc.RequiresResponse, "war executes without target or player acceptance"); return; }
        var round = o.ResolveRound(item.RoundId); var offer = round.PendingOffers.Single(x => x.SourceDocumentId == doc.DocumentId);
        Test.True(h.Effects == 0 && h.TradeExecutions == 0 && doc.RequiresResponse && offer.Status == "open",
            "bilateral proposal waits for target ruler; persuasion alone cannot execute");
        Test.True(o.CurrentStorage.KingdomKnowledge.Any(x => x.KingdomId == "b" && x.DocumentIds.Contains(doc.DocumentId)), "proposal reaches the target court");
        var acceptance = new WorldDiplomacyDocument { DocumentId = "target_accept", RoundId = round.RoundId,
            AuthorKingdomId = "b", TargetKingdomId = "a", Intent = action == "Trade" ? "accept_trade" : "accept_alliance",
            RespondingToOfferDocumentId = doc.DocumentId, RespondingToOfferActionId = offer.SourceActionId };
        Test.True(o.TrySettleRelayOffer(acceptance) == WorldDiplomacyOfferOutcome.Applied, "target ruler's matching response applies bilateral offer");
        Test.True(action == "Trade" ? h.TradeExecutions == 1 && h.TradeDuration == 14 : h.Effects == 1, "exact bilateral game effect is applied once");
        o.TrySettleRelayOffer(acceptance);
        Test.True(action == "Trade" ? h.TradeExecutions == 1 : h.Effects == 1, "duplicate acceptance cannot replay effects");
    }

    private static void PlanningAndRecovery()
    {
        var h = new EventStructureReplay.Host { PlayerAffiliation = "a", PlayerSovereign = "", PublishEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryProcessLobbyistTag("ruler_a", "a", "COMMIT", "action=DeclareWar;move=NewMatter;target=b",
            new("native", "confirmed-war", "session", "Please declare war.", "I will formally declare war on b."));
        var item = o.CurrentStorage.DialogueArrangements.Single();
        var declaration = o.CurrentStorage.Jobs.Single(); Prepare(o, declaration);
        Complete(o, declaration, new() { ["safe"] = true, ["public_context"] = "The ruler agreed to declare war on b." });
        Prepare(o, declaration);
        o.CommitGeneratedDocument(declaration, OralDeclarationReplay.Draft(item, "declare_war", "The ruler of a formally declares war on b."));
        Prepare(o, declaration); Complete(o, declaration, new() { ["safe"] = true, ["agreement_consistent"] = true });
        var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        Prepare(o, job); var request = JObject.Parse(job.UserPrompt);
        Test.True((string)request["player_kingdom"] == "" && job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SovereignControlContract", "missing")), "real opening writer identifies vassal's king as NPC");
        var controls = (JArray)request["sovereign_speakers"]!;
        Test.True(controls.OfType<JObject>().All(x => !(bool)x["human_controlled"]!), "no realm is human-controlled for a vassal");
        var countries = ((JArray)request["frozen_participants"]!).Values<string>().ToArray();
        var draft = new JObject { ["title"] = "Border dispute", ["background"] = "The ruler agreed to announce war; execution remains pending.",
            ["initiator"] = "a", ["material_keys"] = new JArray("world:current"),
            ["participants"] = new JArray(countries.Select(c => Position(c, c == "a"))),
            ["narrative"] = new JArray(new JObject { ["kind"] = "allegation", ["subject"] = "a", ["source"] = "world:current",
                ["text"] = "The ruler disputes the border policy.", ["hero_ids"] = new JArray() }) };
        Complete(o, job, draft);
        Test.True(job.MaterialRepairFeedback.Contains("human_controlled=false") && job.MaterialRepairFeedback.Contains("ruler_hero_id=ruler_a"), "incident's empty NPC positions receive explicit authority correction: " + job.SuspendedReason + " / " + job.MaterialRepairFeedback);
        Test.True(o.CurrentStorage.DialogueArrangements.Single().ArrangementId == item.ArrangementId && h.Effects == 0, "repair preserves original promise without premature execution");
        job.NextRecoveryProbeUtcTicks = 0; Call(o, "TryResumeTechnicalRequest", job, false); Prepare(o, job);
        Test.True(job.UserPrompt.Contains("player_affiliation_is_not_sovereignty") && !job.UserPrompt.Contains("I will formally declare war on b.")
            && (int)JObject.Parse(job.EventTriggerMaterial)["Action"] == (int)DialogueDiplomaticAction.DeclareWar,
            "actual retry contains correction and fixed action without private conversation");
        var repaired = (JObject)draft.DeepClone();
        repaired["participants"] = new JArray(countries.Select(c => Position(c)));
        Complete(o, job, repaired);
        Test.True(job.Kind == "event_concept_audit", "complete NPC positions advance the same event to independent audit: " + job.SuspendedReason);
        Prepare(o, job);
        Test.True(job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SovereignControlContract", "missing"))
            && ((JArray)JObject.Parse(job.UserPrompt)["sovereign_speakers"]!).OfType<JObject>().All(x => !(bool)x["human_controlled"]!),
            "real event auditor receives the same NPC control facts");
        // Restore the exact old suspended shape from the reported incident.
        job.Kind = "event_concept"; job.PrivacyDraft = draft.ToString(); job.TechnicalRecoveryAttempts = 3;
        job.SuspendedReason = "event_concept_invalid:participants[a].agenda_gain_cost_red_line_alternative_require_nonempty_strings_at_most_1000_chars";
        job.NextRecoveryProbeUtcTicks = 0; job.RetryAfterUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", job, false);
        Test.True(job.SuspendedReason == "" && job.TechnicalRecoveryAttempts == 0 && job.MaterialRepairFeedback.Contains("human_controlled=false"), "exact legacy vassal hold receives one finite retry window");
        for (int i = 0; i < 3; i++)
        {
            Call(o, "HoldTechnicalRequest", job, job.MaterialRepairFeedback);
            job.NextRecoveryProbeUtcTicks = 0; Call(o, "TryResumeTechnicalRequest", job, false);
        }
        Test.True(job.TechnicalRecoveryAttempts == 3 && job.SuspendedReason.Contains("human_controlled=false"), "repeated new failures remain bounded");
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        o.ReplaceStorage(saved); job = saved.Jobs.Single(x => x.Kind == "event_concept"); job.NextRecoveryProbeUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", job, false);
        Test.True(job.TechnicalRecoveryAttempts == 3 && job.SuspendedReason != "", "reload does not replenish repaired hold");
        foreach (bool human in new[] { false, true })
        {
            h.PlayerSovereign = human ? "a" : "";
            object[] args = { "a", Position("a", human), null!, "" };
            Test.True((bool)Call(o, "TryReadConceptPosition", args), "real validator preserves both NPC and human position rules");
            args = new object[] { "a", Position("a", !human), null!, "" };
            Test.True(!(bool)Call(o, "TryReadConceptPosition", args), "cannot evade validation by swapping authority roles");
        }
    }
}
