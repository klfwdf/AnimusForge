using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production orchestration and notification owners; only model/game/display leaves
// are deterministic. These are cross-stage regressions from the independent audit.
internal static class IndependentAuditFixReplay
{
    private static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(owner, args)!;
    private static (EventStructureReplay.Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r) Fixture()
    {
        var h = new EventStructureReplay.Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("a", "b", false); r.Participants.Single(x => x.KingdomId == "b").State = "active";
        return (h, o, r);
    }
    internal static void Run()
    {
        foreach (string kind in new[] { "generate", "privacy_material", "player_lifecycle" }) RootlessClosure(kind);
        MetadataRecovery(); RecoveredNotice(); EvidenceVisibility(); EffectLanguage(); ArchiveDesign(); ProgressEffectPreflight();
    }
    private static JObject Position(string id) => new() { ["kingdom"] = id, ["agenda"] = "recognition",
        ["gain"] = "recognition", ["cost"] = "negotiation", ["red_line"] = "no invented consent", ["alternative"] = "unresolved" };

    internal static void ProgressEffectPreflight()
    {
        foreach (string defect in new[] { "aliases", "narrative", "nonobject", "bounds", "none" })
        {
            var (h, o, r) = Fixture(); r.EventPhase = "discussing";
            r.EventBackground = "Two courts disputed their border.";
            foreach (var p in r.Participants) p.Agenda = p.PreferredOutcome = p.ExpectedCost = p.RedLine = p.Alternative = "Recorded claim";
            r.EventBeginningId = r.RoundId + ":started";
            o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId, Stage = "started", State = "public" });
            var war = new WorldDiplomacyDocument { DocumentId = "executed-war", RoundId = r.RoundId, AuthorKingdomId = "a",
                TargetKingdomId = "b", AddressedKingdomIds = new() { "b" }, Intent = "declare_war", IsReadyForPublication = true,
                ChangedDiplomaticState = true, Title = "War declaration", Body = "War was declared." };
            o.AddDocument(war); r.RootDocumentId = war.DocumentId;
            foreach (string id in new[] { "a", "b" }) WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, id, war.DocumentId, 1);
            var update = new JObject { ["title"] = "Relations deteriorated", ["body"] = "The declaration damaged relations.",
                ["effects"] = new JArray(new JObject { ["kind"] = "ruler_relation", ["kingdom"] = "a", ["other"] = "b",
                    ["delta"] = -5, ["days"] = 0, ["reason"] = "Executed declaration" }),
                ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(war.DocumentId),
                ["progress_evidence"] = new JArray(war.DocumentId), ["narrative"] = new JArray() };
            var draft = new JObject { ["outcome"] = "escalated", ["reason"] = "War executed; claims remain unresolved.",
                ["positions"] = new JArray(Position("a"), Position("b")), ["effect_update"] = update,
                ["closure"] = new JArray(new[] { "a", "b" }.Select(id => new JObject { ["kingdom"] = id, ["status"] = "unresolved",
                    ["reason"] = "Claims unresolved after war", ["sources"] = new JArray(war.DocumentId) })) };
            var good = (JObject)draft.DeepClone();
            if (defect == "aliases") update["effects"] = JArray.Parse("[{\"kingdom\":\"a\",\"target\":\"b\",\"type\":\"ruler_relation\",\"value\":-5,\"days\":0}]");
            if (defect == "narrative") update["narrative"] = "War caused relations to deteriorate.";
            if (defect == "nonobject") draft["effect_update"] = "Relations changed";
            if (defect == "bounds") update["effects"]![0]!["delta"] = -50;
            if (defect == "none") good["effect_update"] = draft["effect_update"] = null;
            var job = new WorldDiplomacyJob { JobId = "progress-preflight", Kind = "event_progress", RoundId = r.RoundId, AuthorKingdomId = "a" };
            o.EnqueueJob(job); o.PrepareSharedRequest(job);
            string contract = DiplomacyPromptTexts.Get("EventEffectPayloadContract", "");
            Test.True(contract.Contains("\"delta\":-1") && job.SystemPrompt.Contains(contract), "actual packaged progress writer receives exact effect schema");
            Call(o, "CompleteEventProgress", job, draft);
            if (defect != "none")
            {
                Test.True(job.Kind == "event_progress" && job.SemanticRepairAttempts == 1 && job.RetryAfterUtcTicks == 0,
                    "invalid update rejected before model audit without backoff: " + defect);
                Test.True(h.Writes == 0 && o.CurrentStorage.EventEffects.Count == 0 && !r.EventConvergence.Ending,
                    "preflight never applies effects, closes the event or discards the war");
                if (defect == "aliases") Test.True(job.PrivacySourceMaterial.Contains(".delta:number_required") && job.PrivacySourceMaterial.Contains("use_other_not_target"), "repair feedback names missing fields and wrong aliases");
                if (defect == "narrative") Test.True(job.PrivacySourceMaterial.Contains("narrative_requires_array"), "string narrative receives actionable correction");
                o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
                r = o.ResolveRound(r.RoundId); job = o.CurrentStorage.Jobs.Single(x => x.JobId == job.JobId);
                o.PrepareSharedRequest(job);
                Test.True(JObject.Parse(job.UserPrompt)["rejected_progress"]!.Value<string>()!.Contains(defect == "nonobject" ? "Relations changed" : "effects"), "reload preserves rejected draft for correction");
                Call(o, "CompleteEventProgress", job, good);
            }
            Test.True(job.Kind == "event_progress_audit" && h.Writes == 0 && o.CurrentStorage.EventEffects.Count == 0, "valid payload goes to audit with zero preflight side effects");
            o.PrepareSharedRequest(job);
            Test.True(job.SystemPrompt.Contains(contract), "independent progress audit receives same effect schema");
            var audit = MaterialCountryThirdReplay.Approved(job); audit["closure_consistent"] = true;
            Call(o, "CompleteEventProgress", job, audit);
            Test.True(r.EventPhase == "closing" && r.EventConvergence.ActualWar && h.Writes == (defect == "none" ? 0 : 1), "corrected progress enters actual-war conclusion and applies only the approved extra effect once");
            var ending = o.CurrentStorage.Jobs.Single(x => x.ForcedIntent == "ended");
            var endDraft = new JObject { ["title"] = "War concluded the talks", ["body"] = "War began; original claims remain unresolved.",
                ["effects"] = new JArray(), ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(war.DocumentId), ["narrative"] = new JArray() };
            o.PrepareSharedRequest(ending);
            Test.True(ending.SystemPrompt.Contains(contract), "ending writer shares the effect contract");
            Call(o, "CompleteEventBoundary", ending, endDraft); o.PrepareSharedRequest(ending);
            Test.True(ending.SystemPrompt.Contains(contract), "ending audit shares the effect contract");
            Call(o, "CompleteEventBoundary", ending, MaterialCountryThirdReplay.Approved(ending));
            Test.True(r.State != "active" && o.CurrentStorage.EventAnnouncements.Count(x => x.Stage == "ended") == 1
                && h.Writes == (defect == "none" ? 0 : 1) && o.ResolveDocument(war.DocumentId).ChangedDiplomaticState,
                "one public ending preserves executed war without replaying effects");
        }
    }
    private static void Audited(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject response)
    {
        o.PrepareSharedRequest(job);
        Test.True(o.TryCompleteDiplomacyPipeline(job, new() { Success = true, Content = response.ToString() }), "writer accepted");
        o.PrepareSharedRequest(job);
        Test.True(o.TryCompleteDiplomacyPipeline(job, new() { Success = true, Content = new JObject(MaterialCountryThirdReplay.Approved(job)) { ["closure_consistent"] = true }.ToString() }), "audit accepted");
    }
    private static void RootlessClosure(string firstKind)
    {
        var (h, o, r) = Fixture(); r.EventPhase = "discussing"; r.RootDocumentId = "";
        r.EventBackground = "A public dispute opened before the initiator lost independent authority.";
        foreach (var p in r.Participants) { p.Agenda = p.PreferredOutcome = p.ExpectedCost = p.RedLine = p.Alternative = "recorded position"; }
        r.EventBeginningId = r.RoundId + ":started";
        o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId, Stage = "started", State = "public", CanonicalHistoryRecorded = true });
        // Loaded opening effect: ending its remaining duration is a real ending consequence.
        var effect = new DiplomacyEventEffectState { Id = "opening-effect", EventId = r.RoundId, Kind = "town_loyalty", KingdomId = "b",
            SettlementId = "town-b", Status = "active", AppliedTotal = -1, Delta = -1, StartedDay = h.CurrentDayValue, ExpiresDay = h.CurrentDayValue + 10 };
        o.CurrentStorage.EventEffects.Add(effect);
        typeof(WorldDiplomacyOrchestration).GetField("_indexedEventStorage", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(o, null);
        var first = new WorldDiplomacyJob { JobId = "first", Kind = firstKind, RoundId = r.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b" };
        o.EnqueueJob(first); h.UnavailableParty = "a";
        Call(o, "ReconcileEventEligibility", r); Call(o, "AdvanceEventProgress", r);
        var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_progress");
        Test.True(o.CanDispatchDiplomacyJob(job), "neutral reassessment dispatches despite unavailable first speaker");
        o.PrepareSharedRequest(job);
        Test.True((bool)JObject.Parse(job.UserPrompt)["opening_document_missing"]!, "request describes absent first declaration");
        Audited(o, job, new JObject { ["outcome"] = "deferred", ["reason"] = "Initiator lost diplomatic authority before issuing a declaration.",
            ["closure"] = new JArray(r.Participants.Select(p=>new JObject{["kingdom"]=p.KingdomId,
                ["status"]=p.KingdomId=="a"?"unavailable":"unresolved",["reason"]="Initiator unavailable before first declaration.",
                ["sources"]=new JArray(r.EventBeginningId)})),
            ["positions"] = new JArray(r.Participants.Select(p => Position(p.KingdomId))), ["effect_update"] = null });
        Test.True(r.State == "active" && r.EventPhase == "closing", "ending remains required before release");
        var end = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary" && x.ForcedIntent == "ended");
        Audited(o, end, new JObject { ["title"] = "Discussion cannot proceed", ["body"] = "The initiator lost its independent authority; the temporary opening effect ends.",
            ["effects"] = new JArray(), ["remove_effects"] = new JArray(effect.Id), ["confirmed_documents"] = new JArray(), ["narrative"] = new JArray() });
        Test.True(r.State != "active" && effect.Status == "removed" && effect.AppliedTotal == -1, "real ending closes case without undoing past application");
        Test.True(o.CurrentStorage.Documents.Count == 0 && h.Writes == 0, "no invented first declaration or replayed game action");
        Test.True(DiplomacyNativeAdmissionRules.AllowsPeace(true, false, false, false, new[] { r }, "b", "c"), "closed case releases native peace");
        var other = new WorldDiplomacyRound { RoundId = "other", State = "active", EventSystemVersion = 1 };
        other.Participants.Add(new() { KingdomId = "b", State = "active" });
        Test.True(!DiplomacyNativeAdmissionRules.AllowsPeace(true, false, false, false, new[] { r, other }, "b", "c"), "another active case retains its own peace restriction");
        Call(o, "AdvanceEventProgress", r);
        Test.True(!o.CurrentStorage.Jobs.Any(x => x.RoundId == r.RoundId), "closed rootless case does not reopen");
    }
    private static void MetadataRecovery()
    {
        foreach (string route in new[] { "automatic", "explicit", "reload", "enabled_revalidate" })
        {
            var (h, o, r) = Fixture();
            var j = new WorldDiplomacyJob { JobId = "metadata", Kind = "generate", RoundId = r.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b" }; o.EnqueueJob(j);
            string draft = "{\"title\":\"原题 {TITLE}\",\"body\":\"Original conditional wording 🐉\",\"actions\":[]}";
            Test.True((bool)Call(o, "DeferIncompletePlayerLifecycle", j, draft), "metadata detour owns draft");
            o.CommitFailedJob(j, "provider_failed");
            if (route == "reload")
            {
                o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
                r = o.ResolveRound(r.RoundId); j = o.CurrentStorage.Jobs.Single(x => x.JobId == "metadata");
            }
            j.RetryAfterUtcTicks = j.NextRecoveryProbeUtcTicks = 0;
            if (route == "enabled_revalidate") Call(o, "RefreshEventRequests", r, route);
            else Call(o, "TryResumeTechnicalRequest", j, route != "automatic");
            o.PrepareSharedRequest(j);
            Test.True(j.Kind == "player_lifecycle" && j.PrivacyDraft == draft && (string)JObject.Parse(j.UserPrompt)["public_declaration"]! == draft,
                "exact original draft survives " + route);
            Test.True(string.IsNullOrEmpty(j.SuspendedReason), "recovered metadata request remains runnable");
        }
        var (_, owner, round) = Fixture();
        var missing = new WorldDiplomacyJob { JobId = "legacy-empty", Kind = "player_lifecycle", RoundId = round.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b" };
        owner.EnqueueJob(missing); owner.PrepareSharedRequest(missing);
        Test.True(missing.Kind == "privacy_material" && !string.IsNullOrEmpty(missing.UserPrompt), "previously damaged empty draft restarts full generation with isolation");
        Test.True(!missing.UserPrompt.Contains("\"public_declaration\":\"\""), "no empty metadata classification request");
    }
    private sealed class Sink : IWorldDiplomacyNotificationSink
    {
        internal WorldDiplomacyOrchestration Owner = null!; internal int Count; internal bool Enabled = true, Fail;
        public bool MapNotificationsEnabled => Enabled;
        public bool CanPublishMapNotification() => true;
        public bool EnsureMapNotificationRegistered() => true;
        public string KingdomName(string id) => id;
        public string FormatCampaignDate(int day) => day.ToString();
        public int CurrentDay => 2; public double CurrentHour => 48; public string PlayerKingdomId => "p";
        public void RecordShownTime(WorldDiplomacyDocument d, double hour) => Owner.RecordActualPlayerNotice(d, hour);
        public void ShowRumor(string text) { }
        public void ShowNotice(WorldDiplomacyNotice notice) { if (Fail) throw new InvalidOperationException("display unavailable"); Count++; }
        public void Log(string text) { }
    }
    private static void RecoveredNotice()
    {
        var (_, o, r) = Fixture();
        var d = new WorldDiplomacyDocument { DocumentId = "old-notice", RoundId = r.RoundId, AuthorKingdomId = "a", TargetKingdomId = "p",
            IsReadyForPublication = true, HasReachedPlayerCourt = true, Body = "Original declaration", Title = "Original" }; o.AddDocument(d);
        var app = new WorldDiplomacyNotificationApplication(); var sink = new Sink { Owner = o }; var now = DateTime.UtcNow;
        app.Poll(o.CurrentStorage, now, sink);
        Test.True(sink.Count == 1 && d.FormalNoticeShown, "old notice was actually shown");
        var job = new WorldDiplomacyJob { JobId = "classify", Kind = "player_lifecycle", RoundId = r.RoundId, DocumentId = d.DocumentId, ForcedIntent = "recover_published_lifecycle" }; o.EnqueueJob(job);
        Call(o, "CompletePlayerLifecycle", job, JObject.Parse("{\"requires_response\":true,\"round_participation\":\"continue\",\"round_status\":\"continue\"}"));
        sink.Enabled = false; app.Poll(o.CurrentStorage, now.AddSeconds(2), sink);
        Test.True(sink.Count == 1 && r.PlayerNegotiationWaits.Single().NotifiedHour < 0, "disabled notification leaves new obligation untimed");
        sink.Enabled = true; sink.Fail = true; app.Poll(o.CurrentStorage, now.AddSeconds(4), sink);
        Test.True(sink.Count == 1 && r.PlayerNegotiationWaits.Single().NotifiedHour < 0, "failed display does not start reply time");
        sink.Fail = false; app.Poll(o.CurrentStorage, now.AddSeconds(6), sink);
        Test.True(sink.Count == 2 && d.FormalNoticeShown && r.PlayerNegotiationWaits.Single().NotifiedHour == 48, "recovered obligation is shown once and timed from actual display");
        app.Poll(o.CurrentStorage, now.AddSeconds(8), sink); app.ResetView(); app.Poll(o.CurrentStorage, now.AddSeconds(10), sink);
        Test.True(sink.Count == 2, "ordinary poll and rebuild keep duplicate protection");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        new WorldDiplomacyNotificationApplication().Poll(restored, now, sink);
        Test.True(sink.Count == 2, "persisted notice does not replay after load");
    }
    private static void EvidenceVisibility()
    {
        var (h, o, r) = Fixture(); r.Participants.Add(new() { KingdomId = "c", State = "active" });
        r.Participants.Single(x => x.KingdomId == "b").State = "withdrawn";
        var d = new WorldDiplomacyDocument { DocumentId = "execution", RoundId = r.RoundId, AuthorKingdomId = "a", TargetKingdomId = "c", IsReadyForPublication = true, ChangedDiplomaticState = true }; o.AddDocument(d);
        o.CurrentStorage.PropagationArrivals.Add(new() { DocumentId = d.DocumentId, RoundId = r.RoundId, KingdomId = "b", Scope = "court" });
        Test.True(!(bool)Call(o, "EventHasPendingWork", r, null!), "withdrawn court delivery alone does not block progress");
        var body = JObject.Parse("{\"title\":\"Progress\",\"body\":\"Progress effect\",\"effects\":[{\"kind\":\"ruler_relation\",\"kingdom\":\"a\",\"other\":\"b\",\"delta\":1,\"days\":0,\"reason\":\"The declaration reassures both rulers\"}],\"confirmed_documents\":[\"execution\"],\"progress_evidence\":[],\"remove_effects\":[],\"narrative\":[]}");
        object[] args = { r, "progress:100", body, "" };
        Test.True(!(bool)Call(o, "TryPublishEventBoundary", args) && (string)args[3] == "progress_evidence_not_delivered" && h.Writes == 0, "confirmed reference cannot bypass target knowledge");
        Test.True(d.ChangedDiplomaticState && o.CurrentStorage.EventAnnouncements.Count == 0, "rejecting additional effect preserves actual action and stages no effect receipt");
        o.CurrentStorage.KingdomKnowledge.Add(new() { KingdomId = "b", DocumentIds = new() { d.DocumentId } });
        Test.True((bool)Call(o, "TryPublishEventBoundary", args) && h.Writes == 1, "informed targets allow the additional effect");
        Test.True((bool)Call(o, "TryPublishEventBoundary", args) && h.Writes == 1, "same receipt retry never repeats effect");
        args[1] = "progress:101";
        Test.True(!(bool)Call(o, "TryPublishEventBoundary", args) && h.Writes == 1, "another revision cannot reuse the same progress evidence");
    }
    private sealed class CalendarHost : EventStructureReplay.Host
    {
        public override string FormatCampaignDate(int day) => "1084 Autumn " + day;
    }
    private static void EffectLanguage()
    {
        var host = new CalendarHost();
        var o = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState()); host.Owner = o;
        string text = (string)Call(o, "DescribeEventEffect", new DiplomacyEventEffectState { Kind = "ruler_relation", KingdomId = "a", OtherKingdomId = "b", Delta = 1, AppliedTotal = 1, Reason = "Literal {KIND} 原文 🐉" });
        Test.True(text.Contains(SmokeLocalizationEnvironment.EnglishUi ? "ruler relations" : "君主关系"), "effect type follows actual UI, independently of content package");
        Test.True(text.EndsWith("Literal {KIND} 原文 🐉"), "dynamic reason and braces remain literal");
        string duration = (string)Call(o, "DescribeEventEffect", new DiplomacyEventEffectState { Kind = "town_food", SettlementId = "town-b", Delta = 1, AppliedTotal = 2, ExpiresDay = 9, Reason = "reason" });
        Test.True(duration.Contains(SmokeLocalizationEnvironment.EnglishUi ? "food stocks" : "粮食储备") && duration.Contains("1084 Autumn 9"), "duration projects campaign date and kind through real UI templates");
        Test.True(DiplomacyUiLocalization.Format("AF_DIP_UI_EventEffect_Removed", "Ended: {EFFECT}", ("EFFECT", text)).StartsWith(SmokeLocalizationEnvironment.EnglishUi ? "Ended: " : "已结束："), "removed effect label follows UI");
    }
    private static void ArchiveDesign()
    {
        var (_, o, r) = Fixture();
        var d = new WorldDiplomacyDocument { DocumentId = "unreceived", RoundId = r.RoundId, IsReadyForPublication = true, Body = "UNRECEIVED_BODY" }; o.AddDocument(d);
        Test.True(WorldDiplomacyPresentationQueries.Archive(o.CurrentStorage, x => x.ToString(), o.ResolveRound, o.ResolveDocument).Any(x => x.BodyText.Contains(d.Body)), "user-confirmed archive visibility before delivery is preserved");
    }
}
