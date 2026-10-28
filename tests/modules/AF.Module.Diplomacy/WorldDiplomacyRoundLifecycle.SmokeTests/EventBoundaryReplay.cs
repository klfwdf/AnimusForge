using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class EventBoundaryReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host, IDiplomacyEventHost
    {
        internal bool Enabled = true, FailAfterWrite, Unreadable, LoseSecondReadback;
        internal float Relation, Loyalty = 50;
        internal int Writes, Displays;
        internal Action? Ack;
        internal DiplomacyEventPresentation? Last;
        public override bool WorldDiplomacyEnabled() => Enabled;
        public long EventGeneration => 1;
        public string CaptureEventWorld(IReadOnlyCollection<string> participants) => "{}";
        public bool TryReadEventEffect(DiplomacyEventEffectState e, out float value, out float min, out float max)
        { value = e.Kind == "ruler_relation" ? Relation : Loyalty; min = e.Kind == "ruler_relation" ? -100 : 0; max = 100; return !Unreadable && !(LoseSecondReadback && Writes >= 2 && e.SettlementId == "town_second"); }
        public void ApplyEventEffectDelta(DiplomacyEventEffectState e, float delta)
        {
            Writes++; if (e.Kind == "ruler_relation") Relation += delta; else Loyalty += delta;
            if (FailAfterWrite) throw new InvalidOperationException("after_write");
        }
        public bool ShowEvent(DiplomacyEventPresentation data, out string imageId, Action acknowledged, IReadOnlyList<string> recentImages)
        { Last = data; Displays++; imageId = "scene_1"; Ack = acknowledged; return true; }
    }
    private static object? Invoke(WorldDiplomacyOrchestration owner, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, args);
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("a", "b", false);
        if (r.Participants.All(x => x.KingdomId != "b")) r.Participants.Add(new() { KingdomId = "b", State = "active" });
        r.Participants.First(x => x.KingdomId == "b").State = "active";
        return (h, o, r);
    }
    internal static JObject Position(string id) => id == "p" ? new() { ["kingdom"] = id, ["agenda"] = "Not stated by player",
        ["gain"] = "Not stated by player", ["cost"] = "Not stated by player", ["red_line"] = "Not stated by player", ["alternative"] = "Not stated by player" } : new() { ["kingdom"] = id, ["agenda"] = "clarify the public undertaking",
        ["gain"] = "settle the stated issue", ["cost"] = "only the explicit agreed terms", ["red_line"] = "no unagreed obligation", ["alternative"] = "leave the remaining dispute unresolved" };
    private static JObject Draft(string kind = "ruler_relation", float delta = 1, int days = 0) => new() {
        ["background"] = "The treatment of an envoy has raised competing public claims.", ["positions"] = new JArray(Position("a"), Position("b")),
        ["title"] = "The envoy dispute", ["body"] = "Two courts face a dispute over the treatment of an envoy.", ["scene"] = "Negotiation",
        ["effects"] = new JArray(new JObject { ["kind"] = kind, ["kingdom"] = "a", ["other"] = "b", ["settlement"] = "town_a", ["delta"] = delta, ["days"] = days, ["reason"] = "The dispute has strained trust." }),
        ["narrative"] = new JArray(new JObject { ["kind"] = "adopted_npc_story", ["subject"] = "a", ["source"] = "Adopted NPC story: envoy treatment", ["text"] = "An unnamed envoy was mistreated.", ["actor_scope"] = "unnamed" }),
        ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray() };
    private static bool Publish(WorldDiplomacyOrchestration o, WorldDiplomacyRound r, string stage, JObject draft)
    { object[] args = { r, stage, draft, "" }; return (bool)Invoke(o, "TryPublishEventBoundary", args)!; }
    private static void Tick(WorldDiplomacyOrchestration o)
    { typeof(WorldDiplomacyOrchestration).GetField("_nextEventRuntimeUtc", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(o, 0L); o.AdvanceEventRuntime(); }
    internal static void Run()
    {
        foreach (string defect in new[] { "background", "positions", "red_line", "duplicate", "foreign", "non_string" })
        {
            var f = Fixture(); var broken = Draft();
            if (defect is "background" or "positions") broken.Remove(defect);
            else if (defect == "red_line") ((JObject)broken["positions"]![0]!).Remove("red_line");
            else if (defect == "duplicate") broken["positions"]![1]!["kingdom"] = "a";
            else if (defect == "foreign") broken["positions"]![1]!["kingdom"] = "p";
            else broken["positions"]![0]!["kingdom"] = new JObject();
            Invoke(f.o, "EnsureEventBoundary", f.r, "started");
            var retry = f.o.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary");
            f.o.TryCompleteDiplomacyPipeline(retry, new LlmJobResult { Success = true, Content = broken.ToString() });
            Test.True(retry.Kind == "event_boundary" && retry.SemanticRepairAttempts == 1 && f.h.Writes == 0
                && f.r.EventBackground == "" && retry.PrivacySourceMaterial.StartsWith("opening_"),
                "invalid foundation rejected before audit without effects: " + defect);
            var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(f.o.CurrentStorage))!;
            f.o.ReplaceStorage(saved); retry = saved.Jobs.Single(x => x.Kind == "event_boundary");
            f.o.PrepareSharedRequest(retry); var request = JObject.Parse(retry.UserPrompt);
            Test.True((string)request["validation_error"]! == retry.PrivacySourceMaterial
                && (string)request["rejected_boundary"]! == broken.ToString(Formatting.None)
                && request["required_foundation_participants"]!.Values<string>().SequenceEqual(new[] { "a", "b" })
                && retry.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventBoundaryRepair", "MISSING")),
                "reload retains precise repair request and actual package instructions: " + defect);
            f.o.TryCompleteDiplomacyPipeline(retry, new LlmJobResult { Success = true, Content = Draft().ToString() });
            Test.True(retry.Kind == "event_boundary_audit" && f.h.Writes == 0, "corrected foundation still needs audit");
            f.o.PrepareSharedRequest(retry);
            f.o.TryCompleteDiplomacyPipeline(retry, new LlmJobResult { Success = true,
                Content = MaterialCountryThirdReplay.Audit(retry,Array.Empty<string>()).ToString() });
            Test.True(f.h.Writes == 1 && !saved.Jobs.Contains(retry) && saved.EventAnnouncements.Single().State == "public",
                "corrected and audited opening publishes once");
        }
        var exhausted = Fixture(); Invoke(exhausted.o, "EnsureEventBoundary", exhausted.r, "started");
        var heldJob = exhausted.o.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary");
        for (int attempt = 0; attempt < 3; attempt++)
            exhausted.o.TryCompleteDiplomacyPipeline(heldJob, new LlmJobResult { Success = true, Content = "{}" });
        Test.True(heldJob.SemanticRepairAttempts == 3 && heldJob.SuspendedReason == "opening_background_missing_or_invalid"
            && exhausted.h.Writes == 0, "missing foundation retains finite technical hold instead of forced publication");
        heldJob.SuspendedReason = "opening_background_or_positions_missing"; heldJob.TechnicalRecoveryAttempts = 3;
        heldJob.RetryAfterUtcTicks = 0; heldJob.NextRecoveryProbeUtcTicks = 0;
        exhausted.r.TechnicalHoldReason = heldJob.SuspendedReason;
        Invoke(exhausted.o, "TryResumeTechnicalRequest", heldJob, false);
        Test.True(heldJob.SuspendedReason == "" && heldJob.TechnicalRecoveryAttempts == 0
            && heldJob.PrivacySourceMaterial == "opening_background_or_positions_missing",
            "exhausted old foundation contract receives one bounded recovery with feedback");
        heldJob.SuspendedReason = "opening_background_missing_or_invalid"; heldJob.TechnicalRecoveryAttempts = 3;
        heldJob.RetryAfterUtcTicks = 0; heldJob.NextRecoveryProbeUtcTicks = 0;
        Invoke(exhausted.o, "TryResumeTechnicalRequest", heldJob, false);
        Test.True(heldJob.SuspendedReason == "opening_background_missing_or_invalid" && heldJob.TechnicalRecoveryAttempts == 3,
            "new foundation failures cannot repeatedly trigger legacy contract recovery");
        foreach (string defect in new[] { "background", "positions", "red_line" })
        {
            var invalid = Fixture(); var draft = Draft();
            if (defect == "red_line") ((JObject)draft["positions"]![0]!).Remove("red_line"); else draft.Remove(defect);
            Test.True(!Publish(invalid.o, invalid.r, "started", draft) && invalid.h.Writes == 0
                && invalid.r.EventBackground == "" && invalid.o.CurrentStorage.EventAnnouncements.Count == 0,
                "missing opening foundation cannot partially mutate event state or effects: " + defect);
        }
        var (h, o, r) = Fixture();
        Test.True(Publish(o, r, "started", Draft()), "opening applies real effect");
        Test.True(r.EventBackground.Contains("envoy") && r.Participants.All(x => x.RedLine == "no unagreed obligation")
            && r.EventNarrative.Single().Kind == "adopted_npc_story", "opening stores background, five-part positions and narrative for later negotiation");
        Test.True(h.Writes == 1 && h.Relation == 1 && r.EventBeginningId != "", "effect receipt and opening identity exist before UI");
        Test.True(o.CurrentStorage.EventAnnouncements.Single().Narrative.Single().Kind == "adopted_npc_story", "adopted narrative provenance persists");
        var archive = o.CurrentStorage.CanonicalHistory.DeltaEntries;
        Test.True(archive.Count(x=>x.Kind=="event_boundary")==1 && !archive.Single(x=>x.Kind=="event_boundary").Verified
            && archive.Single(x=>x.Kind=="event_boundary").Text.Contains("adopted_npc_story"),"public narrative enters common compaction archive without certifying allegations");
        Test.True(archive.Single(x=>x.Intent=="event_effect").Verified && archive.Single(x=>x.Intent=="event_effect").Text.Contains("AppliedTotal"),"actual boundary effects have separate confirmed result receipt");
        Test.True(Publish(o, r, "started", Draft("ruler_relation", 5)) && h.Writes == 1, "duplicate boundary cannot reroll effects");
        Tick(o); Test.True(h.Displays == 1 && h.Last!.Effects.Count == 1 && !o.CurrentStorage.EventAnnouncements.Single().Acknowledged, "real presentation displays actual effects before acknowledgement");
        h.Ack!(); Tick(o); Test.True(h.Writes == 1 && h.Displays == 1, "acknowledgement never executes effects or reopens notice");
        Test.True(o.CurrentStorage.RecentEventImages.Single() == "scene_1", "image selection is persisted for deduplication");
        var storage = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        var loaded = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = loaded; loaded.ReplaceStorage(storage);
        var lr = loaded.ResolveRound(r.RoundId); Tick(loaded);
        Test.True(lr.EventBackground == r.EventBackground && lr.Participants.All(x => x.ExpectedCost == "only the explicit agreed terms"),
            "opening foundation survives JSON reload without reconstructing or replacing positions");
        Test.True(Publish(loaded, lr, "started", Draft()) && h.Writes == 1, "reload cannot replay an opening effect");
        Test.True(loaded.CurrentStorage.CanonicalHistory.DeltaEntries.Count(x=>x.Kind=="event_boundary")==1,"reload does not duplicate already archived boundary");
        Test.True(Publish(loaded, lr, "ended", Draft("town_loyalty", -1, 3)), "conclusion has independent actual effect");
        loaded.CloseRound("player_negotiation_finished", lr);
        Test.True(lr.State != "active", "verified conclusion releases the case");
        h.Enabled = false; h.CurrentDayValue++; Tick(loaded);
        Test.True(h.Loyalty == 48 && h.Writes == 3, "existing effect advances once per day while disabled after closure");
        Tick(loaded); Test.True(h.Writes == 3, "same day repeated ticks never reapply");
        h.CurrentDayValue += 3; Tick(loaded);
        Test.True(h.Writes == 3 && storage.EventEffects.Last().Status == "expired", "expiry is original and skipped days are not retroactive penalties");
        var (ch, co, cr) = Fixture(); ch.Relation = 100;
        Test.True(!Publish(co, cr, "started", Draft()) && ch.Writes == 0 && co.CurrentStorage.EventAnnouncements.Count == 0, "clamped non-effect cannot create a false public opening");
        var bad = Draft(); bad["narrative"]![0]!["subject"] = "p"; cr.Participants.Add(new() { KingdomId = "p", State = "active" }); ((JArray)bad["positions"]!).Add(Position("p"));
        ch.Relation = 0;
        Test.True(!Publish(co, cr, "started", bad) && ch.Writes == 0, "NPC fiction cannot invent a player realm act");
        cr.Participants.RemoveAll(x => x.KingdomId == "p");
        cr.Participants.First(x => x.KingdomId == "b").State = "observer";
        Test.True(!Publish(co, cr, "started", Draft()) && ch.Writes == 0, "observer cannot receive an event effect before joining");
        cr.Participants.First(x => x.KingdomId == "b").State = "active";
        ch.FailAfterWrite = true;
        Test.True(Publish(co, cr, "started", Draft()) && ch.Writes == 1, "exception after mutation uses actual readback receipt");
        Test.True(Publish(co, cr, "started", Draft()) && ch.Writes == 1, "post-write exception cannot double apply");
        var (mh, mo, mr) = Fixture(); mh.LoseSecondReadback = true;
        var multiple = Draft();
        ((JArray)multiple["effects"]).Add(new JObject { ["kind"] = "town_loyalty", ["kingdom"] = "a", ["settlement"] = "town_second", ["delta"] = -1, ["days"] = 3, ["reason"] = "Local uncertainty." });
        Test.True(!Publish(mo, mr, "started", multiple) && mh.Writes == 2 && mr.EventBeginningId == ""
            && mo.CurrentStorage.EventAnnouncements.Single().State == "pending", "one successful effect cannot publish a boundary whose second application has unknown outcome");
        Test.True(!Publish(mo, mr, "started", multiple) && mh.Writes == 2, "retry never replays an uncertain application or the prior successful effect");
        var ms = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(mo.CurrentStorage))!;
        var ml = new WorldDiplomacyOrchestration(mh, new WorldDiplomacyRuntimeState()); mh.Owner = ml; ml.ReplaceStorage(ms);
        Test.True(!Publish(ml, ml.ResolveRound(mr.RoundId), "started", multiple) && mh.Writes == 2
            && ms.EventEffects.Last().Status == "uncertain", "reload preserves partial receipts and cannot turn uncertainty into successful opening");

        var (ah, ao, ar) = Fixture();
        Invoke(ao, "EnsureEventBoundary", ar, "started");
        var job = ao.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary");
        Test.True(ao.CanDispatchDiplomacyJob(job), "boundary participates in real scheduler admission");
        ao.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true, Content = Draft().ToString() });
        Test.True(job.Kind == "event_boundary_audit" && ah.Writes == 0, "writer output cannot execute before confidentiality and consistency audit");
        ao.PrepareSharedRequest(job);
        ao.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true,
            Content = MaterialCountryThirdReplay.Audit(job,Array.Empty<string>()).ToString() });
        Test.True(ah.Writes == 1 && !ao.CurrentStorage.Jobs.Contains(job), "audited completion executes through actual pipeline");
        var (ph, po, pr) = Fixture();
        var d = new WorldDiplomacyDocument { DocumentId = "pending_statement", RoundId = pr.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b", Body = "A statement", Title = "Statement" };
        po.CurrentStorage.Documents.Add(d);
        po.ProcessAnalyzedDocument(d, "statement", "none", false, "neutral", 1);
        Test.True(d.EventExecutionPending && !d.IsReadyForPublication, "process declaration waits for the opening boundary");
        Test.True(Publish(po, pr, "started", Draft()), "pending document opening can settle");
        Invoke(po, "CommitPendingEventDocuments", pr, false);
        Test.True(!d.EventExecutionPending && d.IsReadyForPublication, "verified opening releases the same pending declaration");
    }
}
