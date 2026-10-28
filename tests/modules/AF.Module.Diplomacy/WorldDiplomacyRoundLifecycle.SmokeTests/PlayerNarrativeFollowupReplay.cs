using AnimusForge;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private sealed class PlayerNarrativeHost : Host, IDiplomacyEventCalendarHost
    {
        internal int FormattedDay;
        public string FormatEventDate(int day) { FormattedDay = day; return DiplomacyUiLocalization.CampaignDate(day, 21, 84); }
    }

    internal static void PlayerNarrativeFollowup(string incidentPath = null)
    {
        var incident = incidentPath == null ? new JArray() : JArray.Parse(File.ReadAllText(incidentPath));
        JObject Actual(string time, JObject fallback) => (JObject)(incident.OfType<JObject>()
            .FirstOrDefault(x => (string)x["time"] == time)?["draft"]?.DeepClone() ?? fallback);
        var (host, owner) = Owner(new PlayerNarrativeHost { PublishEnabled = true, PlayerAwareExecution = true,
            CurrentDayValue = 91114, OralRange = (0, 3000) });
        var playerHost = (PlayerNarrativeHost)host;

        var concept = Actual("00:40:09", new JObject {
            ["participants"] = new JArray(Position("empire_w")),
            ["narrative"] = new JArray(new JObject { ["kind"] = "allegation", ["source"] = "world:current",
                ["subject"] = "empire_w", ["hero_ids"] = new JArray("lord_1_7"), ["text"] = "Garios demanded the stolen weapons back." })
        });
        var playerPosition = concept["participants"]!.OfType<JObject>().Single(x => (string)x["kingdom"] == "empire_w");
        object[] positionArgs = { "p", playerPosition, null!, null! };
        Test.True(!(bool)Call(owner, "TryReadConceptPosition", positionArgs) && ((string)positionArgs[3]).Contains("player_agency"),
            "actual concept cannot assign the human emperor fabricated negotiating aims");
        var allegation = concept["narrative"]!.OfType<JObject>().Single(x => (string)x["kind"] == "allegation");
        // Rebind both the speaker kingdom and its ruler to the same game-port
        // fixture identity. Mentioned heroes alone do not identify the speaker.
        allegation["subject"] = "p";
        allegation["hero_ids"] = new JArray(host.PartyRulerId("p"));
        object[] factArgs = { allegation, null! };
        Test.True(!(bool)Call(owner, "ValidatePlayerNarrativeSource", factArgs) && ((string)factArgs[1]).Contains("actual_published_player_document"),
            "actual player accusation mislabeled allegation cannot use world:current as proof of player speech");
        var neutralPlayer = (JObject)playerPosition.DeepClone();
        foreach (string key in new[] { "agenda", "gain", "cost", "red_line", "alternative" }) neutralPlayer[key] = "";
        neutralPlayer["address_to"] = new JArray();
        object[] neutralArgs = { "p", neutralPlayer, null!, null! };
        Test.True((bool)Call(owner, "TryReadConceptPosition", neutralArgs), "player may be an affected participant without authoring a decision for them");

        string effect = (string)Call(owner, "DescribeEventEffect", new DiplomacyEventEffectState { Kind = "town_loyalty",
            SettlementId = "town_B2", ExpiresDay = 91115, AppliedTotal = -1, Delta = -1, Reason = "Political tension." });
        Test.True(playerHost.FormattedDay == 91115 && effect.Contains(SmokeLocalizationEnvironment.EnglishUi ? "Year 1084, Autumn 18" : "1084年秋季18日") && !effect.Contains("91115"),
            "effect UI projects the actual campaign date, never exposes the absolute day counter");
        foreach (var language in new[] { "English", "简体中文" })
        {
            Test.True(TaleWorlds.Localization.MBTextManager.ChangeLanguage(language), "actual calendar UI language is registered");
            Test.True(DiplomacyUiLocalization.CampaignDate(91115, 21, 84) == (language == "English" ? "Year 1084, Autumn 18" : "1084年秋季18日"),
                "calendar follows UI language independently of the installed prompt package");
        }
        TaleWorlds.Localization.MBTextManager.ChangeLanguage(SmokeLocalizationEnvironment.EnglishUi ? "English" : "简体中文");

        var round = owner.EnsureActiveRound("a", "p", false); Foundation(round);
        round.RoundTopic = "The forest dispute";
        round.EventSystemVersion = 1; round.EventPhase = "discussing"; round.EventBeginningId = round.RoundId + ":started";
        round.InitiatorKingdomId = "a";
        owner.CurrentStorage.EventAnnouncements.Add(new DiplomacyEventAnnouncement { Id = round.EventBeginningId,
            EventId = round.RoundId, Stage = "started", State = "public", Title = "The forest dispute", Body = "The NPC court issues a warning.",
            Acknowledged = true, CanonicalHistoryRecorded = true });
        const string documentId = "diplomacy_document:6dec81fe29f2462e9e01c8b27c37aad1";
        var warning = new WorldDiplomacyDocument { DocumentId = documentId, RoundId = round.RoundId,
            AuthorKingdomId = "a", TargetKingdomId = "p", IsReadyForPublication = true, RequiresResponse = false,
            Title = "Warning", Body = (string)Actual("00:47:44", new JObject { ["body"] = "Do not interfere." })["body"]!,
            Intent = "warning", RoundParticipation = "continue", RoundStatus = "continue" };
        owner.CurrentStorage.Documents.Add(warning); Know(owner, "a", documentId); Know(owner, "p", documentId);
        var npc = round.Participants.Single(x => x.KingdomId == "a"); npc.State = "active";
        npc.AgendaExpressionDocumentId = documentId; npc.AgendaAddressTo = new() { "p" };
        var player = round.Participants.Single(x => x.KingdomId == "p"); player.State = "observer";
        string playerAgenda = player.Agenda;
        var progressRows = incident.OfType<JObject>().Where(x => (string)x["draft"]?["next_actor"] == "empire_w"
            && (string)x["draft"]?["source_document"] == documentId).Select(x => (JObject)x["draft"]!.DeepClone()).ToList();
        if (progressRows.Count == 0) progressRows.Add(new JObject { ["outcome"] = "wait", ["next_actor"] = "empire_w",
            ["source_document"] = documentId, ["reason"] = "Wait for a response.", ["positions"] = new JArray(),
            ["wait"] = new JObject { ["kind"] = "offer", ["source_id"] = documentId, ["until_day"] = 91124, ["reason"] = "Await player." } });
        int count = 0;
        foreach (var raw in progressRows)
        {
            // Only runtime country identities are rebound. Preserve the actual
            // invalid route, wait object, prose and missing action identity.
            var draft = JObject.Parse(raw.ToString().Replace("\"empire_w\"", "\"p\"").Replace("\"battania\"", "\"a\""));
            var job = new WorldDiplomacyJob { JobId = "actual-progress-" + count++, RoundId = round.RoundId,
                AuthorKingdomId = "a", Kind = "event_progress" };
            owner.CurrentStorage.Jobs.Add(job); owner.PrepareSharedRequest(job); Complete(owner, job, draft);
            Test.True(job.Kind == "event_progress" && job.SemanticRepairAttempts == 1
                && (job.PrivacySourceMaterial.Contains("player_actor_forbidden") || job.PrivacySourceMaterial.Contains("wait_without_open_offer")),
                "actual impossible route is rejected before spending an approval request with actionable feedback");
            Test.True(round.EventWait == null && owner.CurrentStorage.Documents.Count == 1 && player.Agenda == playerAgenda,
                "invalid progress cannot fabricate an offer, a player response or a changed player agenda");
            owner.RemoveJob(job.JobId);
        }
        var progress = new WorldDiplomacyJob { JobId = "corrected-progress", RoundId = round.RoundId, AuthorKingdomId = "a", Kind = "event_progress" };
        owner.CurrentStorage.Jobs.Add(progress); owner.PrepareSharedRequest(progress);
        var closure = new JObject { ["outcome"] = "deferred", ["reason"] = "The warning is optional; no player reply or agreement has occurred.",
            ["positions"] = new JArray(Position("a")), ["next_actor"] = "", ["source_document"] = "", ["wait"] = null,
            ["effect_update"] = null, ["closure"] = new JArray(round.Participants.Select(x => new JObject {
                ["kingdom"] = x.KingdomId, ["status"] = "unresolved", ["reason"] = "No agreement or further actual response.", ["sources"] = new JArray(documentId) })) };
        Complete(owner, progress, closure); owner.PrepareSharedRequest(progress);
        var review = Approved(progress); review["closure_consistent"] = true;
        Complete(owner, progress, review);
        Test.True(round.EventPhase == "closing" && player.Agenda == playerAgenda && !owner.CurrentStorage.Documents.Any(x => x.IsPlayerAuthored),
            "controlled honest deferred conclusion advances without inventing human words or overwriting human state");
        var ending = owner.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary" && x.ForcedIntent == "ended");
        owner.PrepareSharedRequest(ending);
        string badBody = (string)Actual("00:42:05", new JObject { ["body"] = "Garios demanded weapons and issued accusations." })["body"]!;
        var boundary = new JObject { ["title"] = round.RoundTopic, ["body"] = badBody, ["scene"] = "Negotiation",
            ["effects"] = new JArray(), ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(), ["narrative"] = new JArray() };
        Complete(owner, ending, boundary); owner.PrepareSharedRequest(ending);
        ending.EventNarrativeReviewVersion = 0; // A cached legacy audit cannot bypass the new publication guard.
        Complete(owner, ending, new JObject { ["approved"] = true, ["actor_review"] = new JArray() });
        Test.True(ending.Kind == "event_boundary_audit" && ending.PrivacyAuditRetries == 1
            && !owner.CurrentStorage.EventAnnouncements.Any(x => x.Stage == "ended"), "old generic approval cannot publish unsupported player conduct in a conclusion");
        review = Approved(ending); review["narrative_safety"]!["player_agency_valid"] = false;
        review["narrative_safety"]!["reason"] = "The emperor is the human player and no actual player accusation exists.";
        Complete(owner, ending, review);
        Test.True(ending.Kind == "event_boundary" && ending.PrivacySourceMaterial.Contains("narrative_safety_rejected"),
            "whole-body semantic rejection reaches the writer instead of approving allegations by label");
        boundary["body"] = "The NPC court issued a warning. No player reply or agreement occurred; this case is deferred.";
        Complete(owner, ending, boundary); owner.PrepareSharedRequest(ending);
        review = Approved(ending); review["narrative_safety"]!["digest"] = "another-event-or-opening";
        Complete(owner, ending, review);
        Test.True(ending.Kind == "event_boundary_audit", "another event or earlier narrative review cannot approve this ending");
        Complete(owner, ending, Approved(ending));
        Test.True(owner.CurrentStorage.EventAnnouncements.Any(x => x.Stage == "ended" && x.EventId == round.RoundId
            && x.Body == (string)boundary["body"] && x.State == "public"), "corrected ending is published under the exact opening event identity: "
                + ending.Kind + " / " + ending.PrivacySourceMaterial + " / " + ending.SuspendedReason);
        Reload(owner);
        Test.True(owner.CurrentStorage.EventAnnouncements.Count(x => x.Stage == "ended" && x.EventId == round.RoundId) == 1,
            "reload preserves one conclusion without replaying a fabricated player action");

        var (_, recovery) = Owner(); var recoveryRound = recovery.EnsureActiveRound("a", "p", false);
        recoveryRound.EventSystemVersion = 1;
        var human = recoveryRound.Participants.Single(x => x.KingdomId == "p"); human.State = "active";
        human.Agenda = "Actual player-authored demand";
        var foundation = new JObject { ["background"] = "The player opened this dispute; the NPC court may choose its own response.",
            ["positions"] = new JArray(Position("a")) };
        object[] foundationArgs = { recoveryRound, "started", foundation, null!, null!, null! };
        var foundationMethod = typeof(WorldDiplomacyOrchestration).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Single(x => x.Name == "TryReadOpeningFoundation" && x.GetParameters().Length == 6);
        Test.True((bool)foundationMethod.Invoke(recovery, foundationArgs)! && human.Agenda == "Actual player-authored demand",
            "player-initiated opening requires only NPC foundation positions and preserves actual human input");
        var held = new WorldDiplomacyJob { JobId = "old-route-hold", Kind = "event_progress", RoundId = recoveryRound.RoundId,
            AuthorKingdomId = "a", TechnicalRecoveryAttempts = 3,
            SuspendedReason = "event_progress_repair_exhausted:event_progress_invalid" };
        recovery.CurrentStorage.Jobs.Add(held);
        held.RecoveryConfiguration = (string)Call(recovery, "SpeechConfiguration");
        held.RecoveryRevision = recoveryRound.ConversationRevision;
        held.RecoveryKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(recoveryRound, "a");
        Call(recovery, "TryResumeTechnicalRequest", held, false);
        Test.True(held.SuspendedReason == "" && held.EventProgressContractVersion == 1 && held.PrivacySourceMaterial.Contains("route_contract_changed"),
            "an old exhausted impossible route receives one explicit new-contract recovery");
        held.TechnicalRecoveryAttempts = 3; held.SuspendedReason = "event_progress_repair_exhausted:event_progress_invalid";
        held.RetryAfterUtcTicks = held.NextRecoveryProbeUtcTicks = 0;
        Reload(recovery); held = recovery.CurrentStorage.Jobs.Single(x => x.JobId == "old-route-hold");
        Call(recovery, "TryResumeTechnicalRequest", held, false);
        Test.True(held.EventProgressContractVersion == 1 && held.TechnicalRecoveryAttempts == 3 && held.SuspendedReason != "",
            "reload cannot repeatedly replenish the migrated route's finite recovery budget");
    }
}
