using AnimusForge;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;

internal static class Program
{
    private static int _checks;
    private static void Check(bool ok, string name) { _checks++; if (!ok) throw new Exception(name); }
    private static SpeechCue Cue(string text, bool last = true) => new(SpeechSpeaker.Victim, -1, text, "", 3, isLastStatement: last);
    private sealed class Sink : ISpeechPlaybackSink
    {
        public bool Ready = true;
        public bool IsReady => Ready;
        public bool HasFailed => false;
        public bool Available = true;
        public bool DisplayWorks = true;
        public List<SpeechCue> Shown = new();
        public bool IsSpeakerAvailable(SpeechSpeaker s, int i) => Available;
        public bool TryStart(SpeechCue cue) { if (!DisplayWorks) return false; Shown.Add(cue); return true; }
        public void FinishLine() { }
        public void AbortLines() { }
    }
    private static void Main()
    {
        Parser(); ResponseReceiver(); Playback(); Archive(); Orders(); MemoryBridge();
        Console.WriteLine($"PASS execution memory/order contracts: {_checks} assertions");
    }
    private static void Parser()
    {
        Check(!ExecutionMethodRules.IsVisibleInSelection("breaking_wheel") && !ExecutionMethodRules.IsVisibleInSelection("BREAKING_WHEEL"), "wheel hidden from new selections");
        Check(ExecutionMethodRules.TryGet("breaking_wheel", out _), "wheel identity retained for old history/presets");
        Check(ExecutionMethodRules.All.Where(x => x.StringId != "breaking_wheel").All(x => ExecutionMethodRules.IsVisibleInSelection(x.StringId)), "other methods unchanged");
        var rule = ExecutionPromptConfiguration.CreateOrderRule();
        Check(rule.Id == PublicExecutionOrderPolicy.RuleId && rule.IsEnabled, "embedded default rule available for older configurations");
        Check(!rule.Instruction.Contains("[ACTION:") && rule.PostprocessRules.Single().Tag == PublicExecutionOrderPolicy.Tag, "tag lives exclusively in postprocess rules");
        Check(ExecutionPromptConfiguration.SystemPrompt.Contains("[最后陈述]") && ExecutionPromptConfiguration.SystemPrompt.Contains("{language}"), "configured ceremony schema and language slot");
        var eligible = new PromptRuleEligibility();
        Check(!eligible.CanInjectRuleTopicIntoPreprocess(rule.Id) && !eligible.IsRuleEligibleForRag(rule.Id), "unbound target excludes topic");
        eligible.PublicExecutionEligible = true;
        Check(eligible.CanInjectRuleTopicIntoPreprocess(rule.Id) && eligible.IsRuleEligibleForRag(rule.Id), "captured executioner offers topic");
        string bounded = ExecutionAddressContextPolicy.Compose("案件" + new string('a', 9000), new string('b', 9000), new string('c', 9000), new string('d', 9000));
        Check(bounded.Length <= 6000 && bounded.StartsWith("案件"), "bounded context prioritizes captured case");
        Check(bounded.Contains("相关过往记忆") && bounded.Contains("近期对话") && bounded.Contains("重大经历"), "all memory layers represented");
        var parser = new ExecutionSpeechLineParser();
        Check(parser.Append("[开场]\n刽子手: 判决\n[最后").Single().IsLastStatement == false, "declaration not last words");
        Check(parser.Append("陈述]\n死刑犯: 不忘").Count == 0, "marker split and line tail buffered");
        var result = parser.Append("旧恩。\n死刑犯: 第二句。\n围观: 听见了。\n[行刑中]\n死刑犯: 啊。\n[结束后]\n死刑犯: ghost\n围观: 结束了。\n[开场]\n死刑犯: resurrect\n");
        Check(result.Count == 5, "after death and backwards phase reject victim");
        Check(result.Take(2).All(x => x.IsLastStatement), "explicit final statement");
        Check(result.Skip(2).All(x => !x.IsLastStatement), "crowd and during not formal last statement");
        Check(result.Last().Phase == ExecutionSpeechPhase.Aftermath, "phase monotonic");
        var legacy = new ExecutionSpeechLineParser().Append("死刑犯: legacy\n");
        Check(legacy.Count == 1 && !legacy[0].IsLastStatement, "legacy still parses without inventing last-word label");
    }
    private static void ResponseReceiver()
    {
        const string full = "[开场]\n刽子手: 宣读判决。\n[最后陈述]\n死刑犯: 保重。";
        var nonstream = new List<ExecutionSpeechLine>();
        var receiver = new ExecutionSpeechResponseReceiver(lines => nonstream.AddRange(lines));
        receiver.OnComplete(full);
        Check(nonstream.Count == 2 && nonstream[1].IsLastStatement, "nonstream completed reply parses full text and tail");
        var streamed = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => streamed.AddRange(lines));
        receiver.OnChunk(full.Substring(0, 13)); receiver.OnChunk(full.Substring(13));
        receiver.OnComplete(full);
        Check(streamed.Count == 2 && streamed.Select(x => x.Text).SequenceEqual(nonstream.Select(x => x.Text)), "stream completion never duplicates deltas");
        var fallback = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => fallback.AddRange(lines));
        receiver.OnChunk(""); receiver.OnComplete(full);
        Check(fallback.Count == 2, "empty chunk preserves completed-only transport fallback");
        var empty = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => empty.AddRange(lines));
        receiver.OnComplete("");
        Check(empty.Count == 0, "empty completed reply invents no speech");
        string longReply = "[开场]\n刽子手: FIRST\n[最后陈述]\n死刑犯: LAST\n[行刑中]\n" +
            string.Concat(Enumerable.Range(0, 12).Select(i => "围观: " + i + new string('x', 180) + "\n")) +
            "[结束后]\n刽子手: AFTER\n";
        var longCompleted = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => longCompleted.AddRange(lines));
        receiver.OnComplete(longReply);
        Check(longReply.Length > 2000 && longCompleted.Count == 15, "long completed reply retains every valid line");
        Check(longCompleted[0].Text == "FIRST" && longCompleted[0].Phase == ExecutionSpeechPhase.Opening, "long completed reply preserves opening declaration");
        Check(longCompleted[1].Text == "LAST" && longCompleted[1].IsLastStatement, "long completed reply preserves last statement");
        Check(longCompleted.Skip(2).Take(12).All(x => x.Phase == ExecutionSpeechPhase.During), "long completed reply preserves during phase");
        Check(longCompleted.Last().Text == "AFTER" && longCompleted.Last().Phase == ExecutionSpeechPhase.Aftermath, "long completed reply preserves aftermath phase");
        var longStreamed = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => longStreamed.AddRange(lines));
        for (int offset = 0; offset < longReply.Length; offset += 80)
            receiver.OnChunk(longReply.Substring(offset, Math.Min(80, longReply.Length - offset)));
        receiver.OnComplete(longReply);
        Check(longStreamed.Count == longCompleted.Count && longStreamed.Zip(longCompleted, (a, b) =>
            a.Text == b.Text && a.Role == b.Role && a.Phase == b.Phase && a.IsLastStatement == b.IsLastStatement).All(x => x), "long stream and completed reply are identical without duplicates");
        var largeDelta = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => largeDelta.AddRange(lines));
        receiver.OnChunk(longReply); receiver.OnComplete(longReply);
        Check(largeDelta.Count == 15 && largeDelta[1].IsLastStatement && largeDelta[2].Phase == ExecutionSpeechPhase.During, "large single delta preserves phases without duplicate completion");
        var limited = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => limited.AddRange(lines));
        receiver.OnComplete(string.Concat(Enumerable.Range(0, 30).Select(i => "刽子手: " + i + new string('x', 200) + "\n")));
        Check(limited.Count == ExecutionSpeechLineParser.MaximumLines && limited[0].Text.StartsWith("0") && limited.Last().Text.StartsWith("23"), "long full reply retains original 24-line limit and earliest lines");
        var boundary = new List<ExecutionSpeechLine>();
        receiver = new ExecutionSpeechResponseReceiver(lines => boundary.AddRange(lines));
        receiver.OnComplete(new string(' ', 507) + "\n[最后陈述]\n死刑犯: 跨界遗言。");
        Check(boundary.Count == 1 && boundary[0].IsLastStatement && boundary[0].Text == "跨界遗言。", "batch boundary preserves partial marker and final tail");

    }
    private static void Playback()
    {
        var emptyStream = new ExecutionSpeechPlayback(true);
        var waitingSink = new Sink();
        emptyStream.Tick(.1f, waitingSink);
        emptyStream.Tick(10f, waitingSink);
        Check(!emptyStream.HasStarted && emptyStream.IsBusy, "empty ready stream preserves local fallback");
        emptyStream.Complete();
        emptyStream.Tick(.1f, waitingSink);
        Check(!emptyStream.HasStarted && !emptyStream.IsBusy, "empty completed stream was never spoken");
        var pending = new ExecutionSpeechPlayback(true);
        var delayedSink = new Sink { Ready = false };
        pending.TryAppend(Cue("等待气泡初始化"));
        pending.Tick(10f, delayedSink);
        Check(!pending.HasStarted && delayedSink.Shown.Count == 0 && pending.IsBusy, "UI not ready waits without consuming line");
        delayedSink.Ready = true;
        pending.Tick(.1f, delayedSink);
        Check(pending.HasStarted && delayedSink.Shown.Count == 1, "UI readiness resumes actual first line");
        var rejected = new ExecutionSpeechPlayback(new SpeechPlan(new[] { Cue("显示失败") }));
        rejected.Tick(.1f, new Sink { DisplayWorks = false });
        Check(!rejected.HasStarted && rejected.WasAborted, "rejected display never marks speech started");
        var sink = new Sink();
        var playback = new ExecutionSpeechPlayback(new SpeechPlan(new[] { Cue("已显示"), Cue("未显示") }));
        playback.Tick(.1f, sink);
        Check(sink.Shown.Count == 1 && sink.Shown[0].Text == "已显示", "first visible unit");
        playback.Abort(sink); playback.Tick(100f, sink);
        Check(sink.Shown.Count == 1, "early execution never records queued second line");
        var failed = new Sink { DisplayWorks = false };
        new ExecutionSpeechPlayback(new SpeechPlan(new[] { Cue("没显示") })).Tick(.1f, failed);
        Check(failed.Shown.Count == 0, "failed display no transcript");
        var dead = new Sink { Available = false };
        new ExecutionSpeechPlayback(new SpeechPlan(new[] { Cue("死者") })).Tick(.1f, dead);
        Check(dead.Shown.Count == 0, "unavailable speaker skipped");
        var streaming = new ExecutionSpeechPlayback(true);
        streaming.TryAppend(Cue("未播放")); streaming.Complete(); streaming.Abort(sink);
        Check(sink.Shown.Count == 1, "generated/queued is not spoken");
        var context = new SpeechContext(Guid.NewGuid(), "victim", "executor", "charge", "method", "beheading", "treason", "venue", "", EvidenceStrength.None, ExecutionTone.Judicial, LegitimacyTier.Disputed, 1, 1, 0, 0);
        var fallback = new LocalSpeechPlanProvider().Build(context, new SpeechRecentHistory(), (id, text) => text.Replace("{VICTIM}", "victim").Replace("{EXECUTOR}", "executor").Replace("{CHARGE}", "charge").Replace("{METHOD}", "method").Replace("{VENUE}", "venue"));
        Check(fallback.Cues.Any(x => x.IsLastStatement), "local fallback marks victim statement");
        Check(!fallback.Cues.Any(x => x.Speaker == SpeechSpeaker.Crowd), "no crowd no invented speakers");
    }
    private static void Archive()
    {
        var store = new ExecutionTranscriptStore();
        var record = store.Ensure(new ExecutionTranscript { SessionId = "s", VictimName = "victim" });
        var line = new ExecutionTranscriptLine { Sequence = 1, Text = "一字不改『旧恩』", LastStatement = true };
        Check(store.Append("s", line), "accept actual shown evidence");
        Check(!store.Append("s", line), "dedupe repeated display callback");
        Check(record.PublicLastWords == "", "no death no public last words");
        record.Outcome = "cancelled";
        Check(record.PublicLastWords == "", "cancel is not execution");
        record.Outcome = "executed";
        Check(record.PublicLastWords.Contains(line.Text), "public quotation exact");
        var saved = store.Save(); saved["unknown"] = "{broken";
        var loaded = new ExecutionTranscriptStore(); loaded.Load(saved);
        Check(loaded.Find("s").LastStatement == line.Text, "save/load keeps original text");
        Check(loaded.Save()["unknown"] == "{broken", "corrupt original preserved");
        loaded.Load(null); Check(loaded.Find("s") == null, "old save empty no fabricated last words");
        for (int i = 0; i < 105; i++) loaded.Ensure(new ExecutionTranscript { SessionId = i.ToString() });
        Check(loaded.Save().Count == 100 && loaded.Find("104") != null && loaded.Find("0") == null, "100 newest even same day");
        var r = loaded.Find("104");
        for (int i = 1; i <= 24; i++) Check(loaded.Append("104", new ExecutionTranscriptLine { Sequence = i, Text = "x" }), "line cap accepts " + i);
        Check(!loaded.Append("104", new ExecutionTranscriptLine { Sequence = 25, Text = "x" }), "line cap 24");
    }
    private static TownExecutionMissionBehavior Setup()
    {
        PublicExecutionOrderRuntime.Reset();
        var agent = new Agent { Index = 7 };
        var c = new TownExecutionMissionBehavior { Executioner = agent };
        Mission.Current = new Mission { Controller = c, Agents = new List<Agent> { agent, new Agent { Index = 8 } } };
        Campaign.Current = new Campaign(); ShoutBehavior.Facts = 0;
        return c;
    }
    private static PublicExecutionOrderRuntime.Permit Capture(bool selected = true, string chain = "scene") => PublicExecutionOrderRuntime.Capture(7, chain, selected, true, "刽子手，行刑！");
    private static void Orders()
    {
        foreach (string text in new[] { "不要行刑", "谁执行", "等他说完再动手", "如果他说完就行刑", "你说过行刑", "‘execute’", "do not execute", "wait then execute", "行刑吗", "行刑吗？", "别动手" })
            Check(!PublicExecutionOrderPolicy.AllowsImmediateOrder(text), "reject " + text);
        foreach (string text in new[] { "行刑", "现在执行判决", "刽子手，动手！", "proceed", "execute now" })
            Check(PublicExecutionOrderPolicy.AllowsImmediateOrder(text), "permit candidate " + text);
        var c = Setup(); Check(Capture(false) == null, "unselected/public shout rejected");
        Check(Capture(true, "courier") == null, "courier excluded");
        Check(!PublicExecutionOrderRuntime.IsEligible(8), "wrong npc rejected");
        Check(PublicExecutionOrderRuntime.Capture(7, "scene", true, true, "行刑", "现在不能行刑") == null, "refused reply cannot arm order");
        var permit = Capture(); string tag = PublicExecutionOrderRuntime.Normalize(permit, PublicExecutionOrderPolicy.Tag);
        Check(!string.IsNullOrEmpty(tag), "postprocessor accepts bound tag");
        string replay = tag;
        PublicExecutionOrderRuntime.Consume(7, ref tag);
        Check(c.Starts == 1 && ShoutBehavior.Facts == 1 && tag == "", "scene starts and records only start");
        PublicExecutionOrderRuntime.Consume(7, ref replay); Check(c.Starts == 1, "duplicate no second start");
        c = Setup(); tag = PublicExecutionOrderPolicy.Tag; PublicExecutionOrderRuntime.Consume(7, ref tag);
        Check(c.Starts == 0, "raw main reply bare tag cannot execute");
        c = Setup(); tag = PublicExecutionOrderRuntime.Normalize(Capture(), PublicExecutionOrderPolicy.Tag); c.Request.SessionId = Guid.NewGuid(); PublicExecutionOrderRuntime.Consume(7, ref tag);
        Check(c.Starts == 0, "late reply cannot start next prisoner");
        c = Setup(); tag = PublicExecutionOrderRuntime.Normalize(Capture(), PublicExecutionOrderPolicy.Tag); SaveRuntimeGuard.CurrentGeneration++; PublicExecutionOrderRuntime.Consume(7, ref tag);
        Check(c.Starts == 0, "save generation stale rejected");
        c = Setup(); tag = PublicExecutionOrderRuntime.Normalize(Capture(), PublicExecutionOrderPolicy.Tag); c.AcceptStart = false; PublicExecutionOrderRuntime.Consume(7, ref tag);
        Check(c.Starts == 0 && ShoutBehavior.Facts == 0, "controller failure no success memory");
        c = Setup(); tag = PublicExecutionOrderRuntime.Normalize(Capture(true, "native_conversation"), PublicExecutionOrderPolicy.Tag); PublicExecutionOrderRuntime.Consume(7, ref tag);
        Check(c.Starts == 1 && ShoutBehavior.Facts == 1 && Campaign.Current.ConversationManager.EndCalls == 1, "confirmed native order closes conversation and starts execution without another player click");
        Campaign.Current.ConversationManager.End(); Check(c.Starts == 1, "late native close cannot duplicate the execution");
        c = Setup(); Campaign.Current.ConversationManager.DeferEnd = true;
        tag = PublicExecutionOrderRuntime.Normalize(Capture(true, "native_conversation"), PublicExecutionOrderPolicy.Tag); PublicExecutionOrderRuntime.Consume(7, ref tag);
        var manager = Campaign.Current.ConversationManager; PublicExecutionOrderRuntime.Reset(); manager.End();
        Check(c.Starts == 0, "retirement removes pending close handler");
    }

    private sealed class Save : IDataStore
    {
        public bool IsSaving { get; set; }
        public bool IsLoading => !IsSaving;
        public object Data;
        public void SyncData<T>(string key, ref T value) { if (IsSaving) Data = value; else value = Data == null ? default : (T)Data; }
    }
    private static void MemoryBridge()
    {
        var c = Setup(); var owner = new MyBehavior(); MyBehavior.Memories.Clear();
        var victim = new Agent { Index = 9, Name = "Victim", Character = new CharacterObject { HeroObject = c.Request.Victim } };
        var witness = new Agent { Index = 10, Name = "Friend", Character = new CharacterObject { HeroObject = new Hero { StringId = "friend" } } };
        var far = new Agent { Index = 11, Name = "Far", Position = new Position { X = 99 }, Character = new CharacterObject { HeroObject = new Hero { StringId = "far" } } };
        Mission.Current.Agents.AddRange(new[] { victim, witness, far });
        owner.RecordExecutionSpeech(c.Request, 1, Cue("说过的遗言"), victim, new[] { victim, c.Executioner });
        string id = c.Request.SessionId.ToString("N");
        Check(owner.Transcript(id).LastStatement == "说过的遗言", "real bridge stores original");
        Check(MyBehavior.Memories.Count == 2 && MyBehavior.Memories.All(x => x.SubjectId != "far"), "only living nearby identities receive memory");
        Check(MyBehavior.Memories.All(x => x.Facts.Single().Text.Contains("最后陈述") && !x.Facts.Single().Text.Contains("已经死亡")), "predeath is testimony not death");
        owner.RecordExecutionSpeech(c.Request, 1, Cue("说过的遗言"), victim, new[] { victim });
        Check(MyBehavior.Memories.Count == 2, "duplicate callback no repeated memory");
        Check(owner.News.Count == 0, "no news before confirmed death");
        owner.CompleteExecutionTranscript(c.Request, true);
        Check(owner.Materials == 1 && owner.News.Single().Contains("说过的遗言") && owner.News.Single().Contains("转述"), "confirmed death adds public quote with hearsay boundary");
        owner.CompleteExecutionTranscript(c.Request, true);
        Check(owner.Materials == 1 && owner.News.Count == 1, "completion idempotent");
        var save = new Save { IsSaving = true }; owner.TestSync(save);
        var loaded = new MyBehavior(); save.IsSaving = false; loaded.TestSync(save);
        Check(loaded.Transcript(id).LastStatement == "说过的遗言", "real adapter save roundtrip");
        c = Setup(); owner = new MyBehavior(); victim.Character.HeroObject = c.Request.Victim; Mission.Current.Agents.Add(victim);
        MyBehavior.FailMemory = true;
        owner.RecordExecutionSpeech(c.Request, 1, Cue("保留实录"), victim, new[] { victim });
        MyBehavior.FailMemory = false;
        Check(owner.Transcript(c.Request.SessionId.ToString("N")).LastStatement == "保留实录", "memory failure preserves authoritative transcript");
        owner.CompleteExecutionTranscript(c.Request, false);
        Check(owner.Materials == 0 && owner.News.Count == 0, "cancelled scene does not publish execution news");
    }
}
