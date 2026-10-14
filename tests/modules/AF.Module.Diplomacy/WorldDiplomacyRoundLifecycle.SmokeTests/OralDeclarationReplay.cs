using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class OralDeclarationReplay
{
    internal static string Draft(WorldDiplomacyDialogueArrangement item, string intent, string body = null)
    {
        var t = item.Terms;
        return new JObject {
            ["title"] = "关于履行两国约定的宣言",
            ["body"] = body ?? "王庭向两国臣民宣布：经双方统治者商议，我们公开确认所议条件。为保障边境安宁与往来秩序，有关官员须依约办理，不得任意增添限制。"
                + (item.Action == DialogueDiplomaticAction.Annexation ? "p向a并入，p放弃独立王权，全部家族及领地归入a，原王国解散。"
                    : intent == "propose_trade" ? "我方提出期限14日的贸易方案，须由对方正式接受后生效，当前尚未建立贸易协定。"
                    : "我方与对方已谈妥期限14日的贸易协定，现正式宣布履行。商人在约定期间可按两国规则通行。")
                + "我们的公开承诺由王庭承担责任，望两国官员与臣民知悉此文。",
            ["actions"] = new JArray(new JObject {
                ["target_kingdom_id"] = item.TargetKingdomId, ["intent"] = intent,
                ["commitment"] = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent),
                ["responding_to_offer_document_id"] = item.SourceDocumentId, ["responding_to_offer_action_id"] = item.SourceActionId,
                ["peace_terms"] = new JObject { ["tribute_payer_kingdom_id"] = t.TributePayerKingdomId,
                    ["tribute_receiver_kingdom_id"] = t.TributeReceiverKingdomId, ["daily_tribute"] = t.DailyTribute,
                    ["duration_days"] = t.DurationDays, ["cession_from_kingdom_id"] = t.CessionFromKingdomId,
                    ["cession_to_kingdom_id"] = t.CessionToKingdomId, ["cession_settlement_id"] = t.CessionSettlementId },
                ["treaty_terms"] = new JObject { ["receiving_kingdom_id"] = t.ReceivingKingdomId, ["joining_kingdom_id"] = t.JoiningKingdomId }
            }), ["tone"] = "firm", ["international_reputation_delta"] = 1,
            ["international_reputation_reason"] = "履行已经达成的协议" }.ToString(Formatting.None);
    }

    private static (ConcurrentOralMigrationReplay.Host h, WorldDiplomacyOrchestration o,
        WorldDiplomacyDialogueArrangement item, WorldDiplomacyJob job) Fixture(string target = "p", bool origin = true)
    {
        var (h, o) = ConcurrentOralMigrationReplay.Fixture(); h.PublishEnabled = true; h.PlayerAwareExecution = true; h.OralEffectPortsEnabled = true;
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=" + target + ";days=14",
            origin ? new DialogueInteractionOrigin("native", "oral-test", "session", "双方同意贸易十四天。", "我同意开放贸易十四天。") : null);
        return (h, o, o.CurrentStorage.DialogueArrangements.Single(), o.CurrentStorage.Jobs.Single());
    }
    private struct TagSource : IDiplomacyOralTagSource, IDiplomacyCommitmentTagSource
    {
        internal bool Missing, Unavailable, Throw;
        public bool HasSpeaker => !Missing;
        public bool IsAvailable => !Unavailable;
        public string SpeakerHeroId => "ruler_a";
        public string SpeakerKingdomId => "a";
        public bool UseFormalCommitments => true;
        public string SubmitCommitment(string payload) => Throw ? throw new InvalidOperationException("internal failure") : "正式宣言已发布，外交行动已生效。";
        public string ControlCommitment(string payload) => "约定已登记。";
        public string SubmitLegacyCommitment(string action, string payload) => "正式宣言已发布。";
        public string DeclareWar(string payload) => "执行失败。";
        public string MakePeace(string payload) => "执行失败。";
        public string IndependentClanPeace(string payload) => "执行失败。";
        public string FormAlliance(string payload) => "执行失败。";
        public string BreakAlliance(string payload) => "执行失败。";
        public string MakeTrade(string payload) => "执行失败。";
        public string CancelTrade(string payload) => "执行失败。";
        public void Log(string message) { }
    }

    internal static void Run()
    {
        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            string text = "我接受这些条件。 [ACTION:DIPLOMACY:COMMIT:action=Trade;move=NewMatter;target=p]";
            DiplomacyOralTagApplication.Process(new TagSource(), ref text);
            Test.True(text == "我接受这些条件。", "system receipts never become NPC speech/history/TTS: " + channel);
        }
        foreach (var source in new[] { new TagSource { Missing = true }, new TagSource { Unavailable = true }, new TagSource { Throw = true }, new TagSource() })
        {
            string text = "自然发言。 [ACTION:DIPLOMACY:COMMIT:x] [ACTION:DIPLOMACY:bad-format:]";
            DiplomacyOralTagApplication.Process(source, ref text);
            Test.True(text == "自然发言。", "missing speaker, unavailable owner, failures and malformed tags never expose internal text");
        }
        var (host, owner) = ConcurrentOralMigrationReplay.Fixture(); host.PublishEnabled = true; host.PlayerAwareExecution = true; host.OralEffectPortsEnabled = true;
        owner.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=p;days=14",
            new DialogueInteractionOrigin("native", "oral-test", "session", "双方同意贸易十四天。", "我同意开放贸易十四天。"));
        Test.True(owner.CurrentStorage.Documents.Count == 0, "oral consent queues AI writing without publishing template or effect");
        Test.True(owner.CurrentStorage.Jobs.Any(x => x.Kind == "generate" && x.AuthorKingdomId == "a" && x.TargetKingdomId == "p"),
            "oral declaration uses existing generation queue: " + string.Join(";", owner.CurrentStorage.Jobs.Select(x => x.Kind))
                + " arrangements=" + string.Join(";", owner.CurrentStorage.DialogueArrangements.Select(x => x.Status + ":" + x.Reason)));
        var item = owner.CurrentStorage.DialogueArrangements.Single(); var job = owner.CurrentStorage.Jobs.Single();
        owner.PrepareSharedRequest(job);
        Test.True(WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(job)
            && job.UserPrompt.Contains(item.SourcePlayerText) && job.UserPrompt.Contains(item.SourceNpcText)
            && job.UserPrompt.Contains("PRIVATE_MEMORY") && job.PrivacyDeclarationSystem.Contains(SmokeLocalizationEnvironment.EnglishPackage ? "complete diplomatic" : "完整外交")
            && job.RoundConversationRevision == owner.ResolveRound(item.RoundId).ConversationRevision,
            "oral isolation retains original writing contract and private input; public material is tested separately");
        string generated = Draft(item, "accept_trade");
        ApprovedGeneration(owner, job, generated);
        var document = owner.ResolveDocument(item.DocumentId);
        Test.True(item.Status == "executed" && host.TradeDuration == 14 && document.IsReadyForPublication,
            "agreed player trade executes exact duration when AI declaration publishes: " + item.Status + ":" + item.Reason);
        Test.True(document.Body == (string)JObject.Parse(generated)["body"] && document.Intent == "accept_trade"
            && !document.IsPlayerAuthored && owner.ResolveRound(item.RoundId).PendingOffers.Count == 0,
            "AI prose preserved, NPC publishes fulfillment rather than inventing a player acceptance document");
        Test.True(document.HasReachedPlayerCourt && document.HistoryResultRecorded && document.PropagationCompleted,
            "declaration and real result reach normal court publication and canonical history");
        string[] beforeDuplicateJobs = owner.CurrentStorage.Jobs.Select(x => x.JobId).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        ApprovedGeneration(owner, job, generated);
        owner.ProcessAnalyzedDocument(document, document.Intent, document.Commitment, false, "firm", 1);
        Test.True(owner.CurrentStorage.Documents.Count == 1 && host.Notices.Count == 0
            && !owner.CurrentStorage.Jobs.Any(x => x.DialogueArrangementId == item.ArrangementId)
            && owner.CurrentStorage.Jobs.Select(x => x.JobId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(beforeDuplicateJobs),
            "duplicate completion/publication produces neither additional artifact nor notification receipt: documents=" + owner.CurrentStorage.Documents.Count
                + "; notices=" + host.Notices.Count + "; before=" + string.Join(",", beforeDuplicateJobs)
                + "; jobs=" + string.Join(",", owner.CurrentStorage.Jobs.Select(x => x.JobId + ":" + x.Kind + ":" + x.DialogueArrangementId)));
        foreach (bool provenance in new[] { true, false })
        {
            var f = Fixture(provenance ? "b" : "p", provenance);
            ApprovedGeneration(f.o, f.job, Draft(f.item, "propose_trade"));
            Test.True(f.item.Status == "published" && f.h.TradeDuration == 0
                && f.o.ResolveRound(f.item.RoundId).PendingOffers.Count == 1,
                "third country or legacy missing consent remains a normal open proposal");
        }
        foreach (var action in new[] { ("Peace", "accept_peace"), ("Alliance", "accept_alliance"), ("DeclareWar", "declare_war"),
            ("Annexation", "accept_annexation"), ("Tributary", "accept_tributary"), ("Garrison", "accept_garrison"), ("Vassal", "accept_vassal") })
        {
            var f = ConcurrentOralMigrationReplay.Fixture(); f.h.PublishEnabled = true; f.h.PlayerAwareExecution = true; f.h.OralEffectPortsEnabled = true;
            string terms = action.Item1 is "Annexation" or "Tributary" or "Garrison" or "Vassal" ? ";receiving=a;joining=p" : "";
            f.o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=" + action.Item1 + ";move=NewMatter;target=p" + terms,
                new DialogueInteractionOrigin("courier", "agreement", "session", "双方达成上述约定。", "我同意正式宣布上述决定。"));
            var agreement = f.o.CurrentStorage.DialogueArrangements.Single(); var writing = f.o.CurrentStorage.Jobs.Single();
            string clause = action.Item1 switch { "Peace" => "我方与p国正式议和，双方立即结束战争，和平不附贡金与割地。",
                "Alliance" => "我方与p国正式缔结同盟，愿两国在共同利益上互助。", "DeclareWar" => "我方正式向p国宣战，命军队准备作战。",
                "Annexation" => "p向a并入，p放弃独立王权，全部家族与领地归入a，原王国解散。",
                _ => "p向a承认宗主权，p作为臣属依照双方所议的条约履行义务。" };
            string body = "王庭向两国臣民郑重宣布：经双方国王商议，我们已明确决定依照议定条件办理两国事务，此项决定从宣言发布之日起执行。"
                + clause
                + "这项公开决定为两国臣民提供明确依据，边境守军及有关官员须知悉此文并依命办理。";
            ApprovedGeneration(f.o, writing, Draft(agreement, action.Item2, body));
            Test.True(agreement.Status == "executed" && f.h.Effects == 1,
                "AI-written oral action executes once through the real Application effect owner: " + action.Item1 + ":" + agreement.Reason);
            ApprovedGeneration(f.o, writing, Draft(agreement, action.Item2, body));
            Test.True(f.h.Effects == 1, "duplicate draft cannot repeat effect: " + action.Item1);
        }
        foreach (string mutation in new[] { "intent", "target", "terms", "multiple", "empty", "capacity", "tag", "internal_metric", "internal_round", "json" })
        {
            var f = Fixture(); var json = JObject.Parse(Draft(f.item, "accept_trade")); var action = (JObject)json["actions"][0];
            switch (mutation)
            {
                case "intent": action["intent"] = "declare_war"; break;
                case "target": action["target_kingdom_id"] = "c"; break;
                case "terms": action["peace_terms"]["duration_days"] = 15; break;
                case "multiple": ((JArray)json["actions"]).Add(action.DeepClone()); break;
                case "empty": json["body"] = "   "; break;
                case "capacity": json["body"] = new string('文', 6001); break;
                case "tag": json["body"] = json["body"] + "[ACTION:DIPLOMACY:MAKE_TRADE:p]"; break;
                case "internal_metric": json["body"] = json["body"] + "游戏数值"; break;
                case "internal_round": json["body"] = json["body"] + "本回合"; break;
            }
            ApprovedGeneration(f.o, f.job, mutation == "json" ? "broken" : json.ToString());
            Test.True(f.item.Status == "deferred" && f.h.TradeDuration == 0 && f.o.CurrentStorage.Documents.Count == 0
                && f.o.CurrentStorage.Jobs.Count == 0 && f.item.GenerationRetryAfterDay == f.h.CurrentDayValue + 1,
                "invalid AI draft delays without template fallback, action or visible receipt: " + mutation);
        }
        foreach (string mutation in new[] { "cancel", "defer", "ruler", "revision", "round", "load" })
        {
            var f = Fixture(); string draft = Draft(f.item, "accept_trade");
            switch (mutation)
            {
                case "cancel": f.o.CancelUnpublishedCommitmentWork(f.item.ArrangementId, f.item.Version, "撤销"); break;
                case "defer": f.o.ControlOralDiplomaticCommitment("ruler_a", "a", "arrangement=" + f.item.ArrangementId + ";state=deferred;reason=等候"); break;
                case "ruler": f.h.Dead = true; break;
                case "revision": f.item.Version++; break;
                case "round": f.o.ResolveRound(f.item.RoundId).State = "closed"; break;
                case "load": f.o.CurrentStorage.Jobs.Clear(); break;
            }
            ApprovedGeneration(f.o, f.job, draft);
            Test.True(f.o.CurrentStorage.Documents.Count == 0 && f.h.TradeDuration == 0,
                "late/stale AI completion cannot publish or fulfill: " + mutation);
        }
        var delayed = Fixture(); delayed.o.CommitFailedJob(delayed.job, "service unavailable");
        delayed.o.MaintainDialogueAndMemory();
        Test.True(delayed.o.CurrentStorage.Jobs.Count == 1 && delayed.job.SuspendedReason == "service unavailable"
            && !delayed.job.IsRunning, "failed writer retains technical recovery evidence without spinning or retrying");
        delayed.h.CurrentDayValue++; delayed.o.MaintainDialogueAndMemory();
        Test.True(delayed.o.CurrentStorage.Jobs.Count == 1 && delayed.job.SuspendedReason == "service unavailable", "finite retry exhaustion does not become an unbounded daily writing loop");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyDialogueArrangement>(JsonConvert.SerializeObject(item));
        var old = JsonConvert.DeserializeObject<WorldDiplomacyDialogueArrangement>("{\"ArrangementId\":\"old\"}");
        Test.True(restored.ConfirmedPlayerRulerId == "ruler_p" && old.ConfirmedPlayerRulerId == ""
            && old.GenerationRetryAfterDay == -1, "optional consent binding roundtrips; old saves cannot infer player agreement");
        var brief = Fixture(); brief.h.OralRange = (20, 80);
        brief.o.PrepareSharedRequest(brief.job);
        ApprovedGeneration(brief.o, brief.job, Draft(brief.item, "accept_trade",
            "我方同意与贵国缔结贸易协定，期限14日，现宣布依约实施。商人按两国规则通行，有关官员须依照双方所议条件办理。"));
        Test.True(brief.h.TradeDuration == 14, "oral AI declarations can use the configured preferred length range");
        RunExpressiveLengthPreferences();
        RunYieldedCompletion();
        RunWithdrawal();
        NativeDiplomacyFallbackReplay.Run();
    }

    private static void RunExpressiveLengthPreferences()
    {
        string expressive = "你说得很重，我听见了。你先把猜疑收起来，我答应的事不会变。我们已谈妥期限14日的贸易协定，现宣布履行。";
        Test.True(!WorldDiplomacyTextRules.TryGetImmersionViolation(expressive, out _),
            "personal pronouns and conversational retorts are valid diplomatic expression");
        foreach (var sample in new[] {
            (minimum: 300, maximum: 1000, body: expressive, label: "below preferred minimum"),
            (minimum: 20, maximum: 40, body: expressive, label: "above preferred maximum"),
            (minimum: 20, maximum: 40, body: expressive + new string('文', 6000 - expressive.Length), label: "at storage capacity") })
        {
            var f = Fixture(); f.h.OralRange = (sample.minimum, sample.maximum);
            f.o.PrepareSharedRequest(f.job);
            ApprovedGeneration(f.o, f.job, Draft(f.item, "accept_trade", sample.body));
            var document = f.o.ResolveDocument(f.item.DocumentId);
            Test.True(f.item.Status == "executed" && f.h.TradeDuration == 14
                && document?.IsReadyForPublication == true && document.Body == sample.body,
                "expressive oral declaration preserves full prose and agreed effect " + sample.label);
        }
    }

    private static void RunWithdrawal()
    {
        foreach (bool acceptedElsewhere in new[] { false, true })
        {
            var f = Fixture("b"); ApprovedGeneration(f.o, f.job, Draft(f.item, "propose_trade"));
            var source = f.o.ResolveDocument(f.item.DocumentId);
            var offer = f.o.ResolveRound(f.item.RoundId).PendingOffers.Single();
            f.o.ControlOralDiplomaticCommitment("ruler_a", "a", "arrangement=" + f.item.ArrangementId + ";state=cancelled;reason=局势变化",
                new DialogueInteractionOrigin("scene", "withdrawal", "session", "请撤回之前的贸易提案。", "我答应撤回这份提案。"));
            var withdrawal = f.o.CurrentStorage.DialogueArrangements.Single(x => x.IsWithdrawal);
            var writing = f.o.CurrentStorage.Jobs.Single(x => x.DialogueArrangementId == withdrawal.ArrangementId);
            Test.True(f.o.CurrentStorage.Documents.Count == 1 && offer.Status == "open"
                && writing.UserPrompt.Contains("我答应撤回这份提案。"), "oral withdrawal also waits for AI writing with current conversation material");
            string[] beforeWithdrawalJobs = f.o.CurrentStorage.Jobs.Select(x => x.JobId).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            f.o.ControlOralDiplomaticCommitment("ruler_a", "a", "arrangement=" + f.item.ArrangementId + ";state=cancelled;reason=局势变化");
            Test.True(f.o.CurrentStorage.Jobs.Count(x => x.DialogueArrangementId == withdrawal.ArrangementId) == 1
                && f.o.CurrentStorage.Jobs.Select(x => x.JobId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(beforeWithdrawalJobs), "duplicate withdrawal consent keeps one AI request and does not add any job");
            if (acceptedElsewhere) offer.Status = "accepted";
            string body = "王庭现向两国臣民宣布：我们撤回此前向对方提出的贸易方案，原方案自此不再作为开放提案。此前商议不构成已经生效的贸易协定，双方也没有据此承担新的条约义务。此项决定只涉及尚未被接受的方案，不影响其他已经生效的条约。有关官员须依此宣言处理后续文书，避免再将已撤回的方案视作现行提议。";
            ApprovedGeneration(f.o, writing, Draft(withdrawal, "withdraw_offer", body));
            Test.True(acceptedElsewhere ? offer.Status == "accepted" && f.o.CurrentStorage.Documents.Count == 1
                : offer.Status == "withdrawn" && f.item.Status == "withdrawn" && withdrawal.Status == "executed"
                    && f.o.ResolveDocument(withdrawal.DocumentId).Body == body,
                "AI withdrawal publishes with normal result; late acceptance prevents withdrawal of a fulfilled agreement");
            Test.True(f.h.TradeDuration == 0 && f.h.Notices.Count == 0, "withdrawal does not execute treaty or expose a system receipt");
        }
    }

    private readonly struct CompletionSource : IWorldDiplomacyCompletionSource
    {
        private readonly WorldDiplomacyOrchestration owner;
        private readonly Queue<LlmJobResult> results;
        internal CompletionSource(WorldDiplomacyOrchestration value, LlmJobResult result) { owner = value; results = new(); results.Enqueue(result); }
        public WorldDiplomacyStorage Storage => owner.CurrentStorage;
        public long RuntimeGeneration => 7;
        public bool IsSaveRuntimeStale(long generation) => generation != 7;
        public int CurrentHour => 24;
        public int FailedServiceCooldownHours => 12;
        public bool TryDequeue(out LlmJobResult result) => results.TryDequeue(out result);
        public void ReleaseLease(string job, long generation) { }
        public void LogUsage(WorldDiplomacyJob job, LlmJobResult result) { }
        public void RemoveJob(string id) => owner.RemoveJob(id);
        public void Log(string message) { }
    }

    // Downstream semantic/authority oracle: confidentiality approval is a modeled leaf.
    // PlayerIndependentReplay drives the real isolation -> writing -> audit dispatcher.
    private static void ApprovedGeneration(WorldDiplomacyOrchestration owner, WorldDiplomacyJob job, string raw)
    {
        job.Kind = "generate"; job.PrivacyApproved = true;
        try { owner.CommitGeneratedDocument(job, raw); }
        finally { job.PrivacyApproved = false; }
    }

    private static void RunYieldedCompletion()
    {
        foreach (string change in new[] { "none", "terms", "cancel", "save" })
        {
            var f = Fixture(); string frozen = Draft(f.item, "accept_trade");
            f.o.PrepareSharedRequest(f.job);
            f.job.Kind = "generate"; f.job.PrivacyApproved = true; // modeled audit approval for the existing late-result oracle
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Worker captures only a serialized fixture reply and a gate; the
            // production completion/drain runs back on this calling thread.
            Task<string> request = Task.Run(async () => { await gate.Task; return frozen; });
            Test.True(!request.IsCompleted, "forced async yield before completion");
            if (change == "terms") f.item.Terms.DurationDays = 15;
            if (change == "cancel") f.o.CancelUnpublishedCommitmentWork(f.item.ArrangementId, 1, "撤销");
            gate.SetResult(true);
            var result = new LlmJobResult { JobId = f.job.JobId, Content = request.GetAwaiter().GetResult(), Success = true,
                RuntimeGeneration = change == "save" ? 6 : 7, RoundConversationRevision = f.job.RoundConversationRevision,
                OwnershipEpoch = f.job.OwnershipEpoch, SchedulingOwner = f.job.SchedulingOwner, PlayerAuthorityRulerId = f.job.PlayerAuthorityRulerId };
            var source = new CompletionSource(f.o, result);
            WorldDiplomacyCompletionApplication.Run(ref source, f.o);
            Test.True(change == "none" ? f.h.TradeDuration == 14 : f.h.TradeDuration == 0 && f.o.CurrentStorage.Documents.Count == 0,
                "actual completion dispatcher admits current AI result and rejects stale source/save/cancel after yield: " + change);
            if (change == "terms") Test.True(f.o.CurrentStorage.Jobs.Contains(f.job) && f.job.UserPrompt.Contains("15"),
                "same-generation changed terms rebuild writing without publishing the stale draft");
        }
    }
}
