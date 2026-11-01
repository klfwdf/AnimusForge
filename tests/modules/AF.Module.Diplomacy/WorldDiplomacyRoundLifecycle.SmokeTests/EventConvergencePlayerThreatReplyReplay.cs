using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class EventConvergenceReplay
{
    private static void PlayerThreatReplyRepair()
    {
        DirectedPlayerThreatReplies();
        PlayerThreatReplyRecovery();
        PlayerThreatReplyGuards();
        var (h, o, r) = Fixture("a", "p");
        var source = Publish(o, r, "player-ultimatum", "a", "p", intent: "ultimatum", response: true);
        var provisional = o.EnsureActiveRound("p", "a", true);
        provisional.DialogueArrangementId = "player_manual_declaration";
        var reply = new WorldDiplomacyDocument { DocumentId = "late-routed-answer", RoundId = provisional.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "a", IsPlayerAuthored = true, IsReadyForPublication = true,
            AnalysisStatus = "success", ThreatConsequencesSettled = true, Intent = "statement",
            Day = 2, CreatedUtcTicks = 20, Title = "Reply", Body = "The weather is lovely." };
        o.AddDocument(reply);
        var job = new WorldDiplomacyJob { JobId = "late-routing", RoundId = provisional.RoundId,
            DocumentId = reply.DocumentId, Kind = "player_reply_routing", ReplyRoutingCandidateDocumentIds = new() { source.DocumentId } };
        o.CurrentStorage.Jobs.Add(job);
        Call(o, "CompletePlayerReplyRouting", job, new JObject { ["answered_npc_document_ids"] = new JArray(source.DocumentId) });
        var threat = o.CurrentStorage.DiplomaticThreats.Single();
        Test.True(threat.TargetDecision == "noncomplied" && threat.TargetDecisionDocumentId == reply.DocumentId,
            "late semantic attachment settles the exact pending ultimatum even after initial publication was settled");
        var wait = r.PlayerNegotiationWaits.Single(x => x.SourceDocumentId == source.DocumentId);
        Test.True(wait.AnswerDocumentId == reply.DocumentId && !DiplomacyEventRules.SilenceDue(wait, 10000),
            "an actual answer cannot also be treated as human silence");
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "a", reply.DocumentId, 2);
        Call(o, "BeginEventEnding", r, ""); Advance(o, r);
        Test.True(o.CurrentStorage.Jobs.Any(x => x.Kind == "generate" && x.AuthorKingdomId == "a"
            && x.SourceDocumentId == reply.DocumentId), "issuer receives the follow-through turn instead of waiting for player again: "
            + string.Join(",", o.CurrentStorage.Jobs.Select(x => x.Kind + "/" + x.AuthorKingdomId + "/" + x.SourceDocumentId)));
        Test.True(!r.EventConvergence.ActualWar, "registering noncompliance does not itself execute war");
        Call(o, "RegisterPublishedPlayerAnswer", reply);
        Test.True(threat.NonComplianceEvents.Count == 1 && threat.TargetDecisionDocumentId == reply.DocumentId,
            "repeated answer callbacks preserve one decision receipt");
        Console.WriteLine("Player threat replies: late routing, off-topic directed answers, silence, compliance, isolation and load recovery PASS.");
    }

    private static WorldDiplomacyDocument RawPlayerThreatReply(WorldDiplomacyOrchestration o, WorldDiplomacyRound r,
        string id, string target = "a", string source = "")
    {
        var reply = new WorldDiplomacyDocument { DocumentId = id, RoundId = r.RoundId, AuthorKingdomId = "p",
            TargetKingdomId = target, SourceDocumentId = source, IsPlayerAuthored = true, IsReadyForPublication = true,
            AnalysisStatus = "success", ThreatConsequencesSettled = true, Intent = "statement", Day = 2,
            CreatedUtcTicks = 20, Title = "Reply", Body = "The weather is lovely." };
        o.AddDocument(reply);
        return reply;
    }

    private static void DirectedPlayerThreatReplies()
    {
        foreach (bool explicitEntry in new[] { false, true })
        {
            var (_, o, r) = Fixture("a", "p");
            var source = Publish(o, r, "direct-ultimatum", "a", "p", intent: "ultimatum", response: true);
            var destination = explicitEntry ? o.EnsureActiveRound("p", "a", true) : r;
            if (explicitEntry) destination.DialogueArrangementId = "player_manual_declaration";
            // Explicit reply entry survives even a model which extracts no target
            // and classifies none of the NPC documents as answered.
            var reply = RawPlayerThreatReply(o, destination, "off-topic-answer", explicitEntry ? "" : "a",
                explicitEntry ? source.DocumentId : "");
            Call(o, "BindPlayerDeclarationToSharedEvent", reply);
            Call(o, "RegisterPublishedPlayerAnswer", reply);
            var threat = o.CurrentStorage.DiplomaticThreats.Single();
            Test.True(reply.RoundId == r.RoundId && threat.TargetDecision == "noncomplied",
                "directed off-topic statement counts without model semantic approval: explicit=" + explicitEntry);
            Test.True(r.PlayerNegotiationWaits.Single().AnswerDocumentId == reply.DocumentId,
                "deterministic source binding stops silence for exactly the answered threat");
        }
        {
            var (_, o, r) = Fixture("a", "p");
            Publish(o, r, "compliance-source", "a", "p", intent: "ultimatum", response: true);
            Publish(o, r, "compliance-answer", "p", "a", "compliance-source", "comply_ultimatum");
            var threat = o.CurrentStorage.DiplomaticThreats.Single();
            Test.True(threat.Status == "complied" && threat.TargetDecision != "noncomplied",
                "validated player compliance retains the original compliance execution result");
        }
        {
            var (_, o, r) = Fixture("a", "p");
            Publish(o, r, "silent-ultimatum", "a", "p", intent: "ultimatum", response: true);
            var wait = r.PlayerNegotiationWaits.Single(); wait.NotifiedHour = 10;
            Call(o, "BeginEventEnding", r, ""); Advance(o, r);
            Test.True(!DiplomacyEventRules.SilenceDue(wait, 81) && DiplomacyEventRules.SilenceDue(wait, 82)
                && o.CurrentStorage.DiplomaticThreats.Single().TargetDecision == "pending",
                "no player publication retains the exact original 72-hour silence boundary");
        }
        {
            var (_, o, r) = Fixture("a", "p");
            Publish(o, r, "ordinary-question", "a", "p", response: true);
            var reply = RawPlayerThreatReply(o, r, "ordinary-unrelated");
            Call(o, "RegisterPublishedPlayerAnswer", reply);
            var wait = r.PlayerNegotiationWaits.Single(); wait.NotifiedHour = 10;
            Test.True(DiplomacyEventRules.SilenceDue(wait, 82), "ordinary non-threat silence has unchanged relevance rules");
            reply.AnsweredNpcDocumentIds.Add("ordinary-question"); Call(o, "RegisterPublishedPlayerAnswer", reply);
            Test.True(!DiplomacyEventRules.SilenceDue(wait, 82), "ordinary recognized reply still ends ordinary silence");
        }
    }

    private static void PlayerThreatReplyRecovery()
    {
        var (h, o, r) = Fixture("a", "p");
        Publish(o, r, "saved-ultimatum", "a", "p", intent: "ultimatum", response: true);
        var reply = RawPlayerThreatReply(o, r, "saved-answer");
        r.PlayerNegotiationWaits.Single().AnswerDocumentId = reply.DocumentId;
        r.EventConvergence.Ending = true;
        h.CurrentDayValue = 100;
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        var threat = o.CurrentStorage.DiplomaticThreats.Single();
        Test.True(threat.TargetDecision == "noncomplied" && threat.TargetDecisionDocumentId == reply.DocumentId
            && threat.TargetDecisionDay == reply.Day, "load repairs exact saved answer receipt with its original date");
        Call(o, "NormalizePlayerLifecycleRecovery");
        Test.True(threat.NonComplianceEvents.Count == 1, "repeated load normalization cannot duplicate a threat consequence");
    }

    private static void PlayerThreatReplyGuards()
    {
        foreach (string scenario in new[] { "other-country", "other-event", "unseen", "older-answer", "pending-analysis",
            "pending-execution", "closed-threat", "lost-authority", "settled-compliance", "unknown-old-answer" })
        {
            var (h, o, r) = Fixture("a", "p");
            var source = Publish(o, r, "guard-ultimatum", "a", "p", intent: "ultimatum", response: true);
            var threat = o.CurrentStorage.DiplomaticThreats.Single();
            var reply = RawPlayerThreatReply(o, r, "guard-answer", scenario == "other-country" ? "c" : "a");
            if (scenario == "other-event") reply.RoundId = o.EnsureActiveRound("p", "a", true).RoundId;
            if (scenario == "unseen") { o.CurrentStorage.KingdomKnowledge.Clear(); o.CurrentStorage.NobleKnowledge.Clear(); }
            if (scenario == "older-answer") { reply.Day = 0; reply.CreatedUtcTicks = 0; }
            if (scenario == "pending-analysis") reply.AnalysisStatus = "pending_analysis";
            if (scenario == "pending-execution") reply.EventExecutionPending = true;
            if (scenario == "closed-threat") threat.Status = "withdrawn";
            if (scenario == "lost-authority") h.UnavailableParty = "a";
            if (scenario == "settled-compliance") threat.Status = "complied";
            if (scenario == "unknown-old-answer")
            { r.PlayerNegotiationWaits.Single().AnswerDocumentId = "missing-document"; Call(o, "RecoverPublishedPlayerThreatAnswers"); }
            else Call(o, "RegisterPublishedPlayerAnswer", reply);
            Test.True(threat.TargetDecision == "pending" && string.IsNullOrEmpty(threat.TargetDecisionDocumentId),
                "unrelated, invalid or already settled work does not acquire a refusal: " + scenario);
        }
        {
            var (_, o, r) = Fixture("a", "p");
            Publish(o, r, "first-ultimatum", "a", "p", intent: "ultimatum", response: true);
            var second = o.EnsureActiveRound("a", "p", false);
            Publish(o, second, "second-ultimatum", "a", "p", intent: "ultimatum", response: true, settle: false);
            // Restore an independent legacy obligation; ordinary creation may
            // supersede an earlier threat between the same parties.
            var other = JsonConvert.DeserializeObject<WorldDiplomacyThreat>(JsonConvert.SerializeObject(o.CurrentStorage.DiplomaticThreats.Single()))!;
            other.ThreatId = "independent-other-threat"; other.StageDocumentId = "second-ultimatum"; other.StageRoundId = second.RoundId;
            o.CurrentStorage.DiplomaticThreats.Add(other);
            var reply = RawPlayerThreatReply(o, r, "first-only", source: "first-ultimatum");
            Call(o, "RegisterPublishedPlayerAnswer", reply);
            Test.True(o.CurrentStorage.DiplomaticThreats.Single(x => x.StageDocumentId == "first-ultimatum").TargetDecision == "noncomplied"
                && o.CurrentStorage.DiplomaticThreats.Single(x => x.StageDocumentId == "second-ultimatum").TargetDecision == "pending",
                "same issuer in another concurrent event keeps its independent pending decision");
        }
    }
}
