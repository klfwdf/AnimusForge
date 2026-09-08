using System.Collections.Concurrent;
using System.Reflection;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

internal static class AsyncInteractionOwnerTests
{
    private static readonly CapabilitySet Capabilities = new(new[] { "prompt.compose", "llm.generate", "action.parse" });
    private static readonly RuntimeConfigSnapshot Config = new("async-owner", 1,
        new Dictionary<string, bool> { ["conversation"] = true },
        new Dictionary<string, LlmProviderSnapshot> { ["main"] = new("main", "https://fixture.invalid", "fixture", 1000, 5000) });

    public static async Task RunAsync()
    {
        int count = 0;
        var failures = new List<string>();
        async Task Case(string name, Func<Task> test)
        {
            count++;
            try { await test(); }
            catch (Exception exception) { failures.Add(name + ": " + exception.Message); }
        }
        await Case("legacy-constructor-and-main-only", LegacyMainOnlyAsync);
        await Case("legacy-synchronous-three-stage", LegacyFullAsync);
        await Case("async-prepare-http-normalize-order", OrderedAsync);
        foreach (string timing in new[] { "before-prepare", "during-prepare", "before-normalize", "during-normalize" })
            await Case("cancel-" + timing, () => CancellationAsync(timing));
        foreach (string stage in new[] { "prepare-throw", "normalize-throw", "prepare-cancel", "normalize-cancel", "prepare-null", "normalize-null", "postprocess-failure" })
            await Case(stage, () => FailureAsync(stage));
        await Case("async-prepare-sync-parser", AsyncPrepareSyncParserAsync);
        await Case("async-parser-without-capture-rejected", InvalidPortsAsync);
        await Case("concurrent-request-context-isolation", () => ConcurrentAsync(false));
        await Case("same-session-supersedes-prepare", () => ConcurrentAsync(true));
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        Console.WriteLine("PASS asyncInteractionOwner cases=" + count + " realComposition=true realCoordinator=true realCommitBoundary=true game=NOT_RUN");
    }

    private static async Task LegacyMainOnlyAsync()
    {
        ConstructorInfo original = typeof(LegacyInteractionPipelinePorts).GetConstructors().Single(info => info.GetParameters().Length == 7);
        Check(original.GetParameters()[6].IsOptional, "old optional seven-argument ABI disappeared");
        var events = new List<string>();
        var gateway = new Gateway((request, _) => { events.Add("main-http"); return Task.FromResult(Success("raw-main")); });
        var ports = new LegacyInteractionPipelinePorts(
            _ => { events.Add("rules"); return Rules("sync"); },
            (_, _, _) => { events.Add("main-prompt"); return Prompt("main"); },
            (_, _, _) => { events.Add("context"); return Context("sync"); },
            (raw, _) => { events.Add("parse:" + raw); return Plan("sync"); },
            (_, _) => { events.Add("visible"); return "visible"; }, Capabilities);
        Check(ports.ComposePostprocessPromptAsync == null && ports.ParseActionsAsync == null, "old constructor enabled new owner hooks");
        using var coordinator = LegacyInteractionPipelineComposition.Create(ports, gateway, () => 4L);
        InteractionResult result = await Execute(coordinator, Envelope("sync"));
        Check(result.Status == InteractionStatus.Succeeded && result.ActionPlan.Actions.Single().TargetId == "sync", "legacy one-stage result changed");
        Check(events.SequenceEqual(new[] { "rules", "main-prompt", "main-http", "context", "parse:raw-main", "visible" }), "legacy single-stage callback order changed");
    }

    private static async Task LegacyFullAsync()
    {
        var events = new List<string>();
        var gateway = new Gateway((request, _) => { events.Add("http:" + request.Stage); return Task.FromResult(Success(request.Stage == InteractionStage.MainReply ? "main-raw" : "post-raw")); });
        var ports = new LegacyInteractionPipelinePorts(_ => Rules("sync"), (_, _, _) => Prompt("main"), (_, _, _) => Context("sync"),
            (raw, _) => { events.Add("parse:" + raw); return Plan("sync"); },
            (_, _) => { events.Add("visible"); return "visible"; }, Capabilities,
            (_, _, visible, raw, _) => { Check(visible == "visible" && raw == "main-raw", "sync prepared text changed"); events.Add("prepare"); return Prompt("post"); });
        using var coordinator = LegacyInteractionPipelineComposition.Create(ports, gateway, () => 4L);
        InteractionResult result = await Execute(coordinator, Envelope("sync-full"));
        Check(result.Status == InteractionStatus.Succeeded && result.RawReply == "main-raw" && result.RawPostprocessReply == "post-raw", "sync result raw provenance changed");
        Check(events.SequenceEqual(new[] { "http:MainReply", "visible", "prepare", "http:Postprocess", "parse:post-raw" }), "legacy full-stage callback order changed");
    }

    private static async Task OrderedAsync()
    {
        var fixture = new Fixture("ordered");
        fixture.HoldPrepare = fixture.HoldNormalize = true;
        using var coordinator = fixture.Create();
        Task<InteractionResult> pending = Execute(coordinator, fixture.Envelope);
        await Entered(fixture.PrepareEntered.Task);
        Check(fixture.PostCalls == 0 && fixture.NormalizeCalls == 0 && !pending.IsCompleted, "postprocess HTTP ran before awaited capture completed");
        fixture.PreparePermit.SetResult(true);
        await Entered(fixture.NormalizeEntered.Task);
        Check(fixture.PostCalls == 1 && !pending.IsCompleted, "ActionPlan was published before normalization completed");
        fixture.NormalizePermit.SetResult(true);
        InteractionResult result = await pending;
        Check(result.Status == InteractionStatus.Succeeded && result.ActionPlan.Actions.Single().TargetId == "ordered", "normalized owner plan missing");
        Check(fixture.PrepareCalls == 1 && fixture.NormalizeCalls == 1 && fixture.SyncCalls == 0, "owner reentered or sync fallback called");
        Check(fixture.Events.SequenceEqual(new[] { "main-http", "prepare-enter", "prepare-done", "post-http", "normalize-enter", "normalize-done" }), "async owner ordering changed");
        var effects = new Effects();
        InteractionCommitResult commit = new InteractionResultCommitter(() => 4L).Commit(fixture.Envelope, result, effects, effects);
        Check(commit.ActionsExecuted && effects.Actions == 1 && effects.Messages == 2, "normalized result cannot use the real commit boundary");
    }

    private static async Task CancellationAsync(string timing)
    {
        using var token = new CancellationTokenSource();
        var fixture = new Fixture("cancel-" + timing) { Cancellation = token };
        fixture.CancelAfterMain = timing == "before-prepare";
        fixture.HoldPrepare = timing == "during-prepare";
        fixture.CancelAfterPost = timing == "before-normalize";
        fixture.HoldNormalize = timing == "during-normalize";
        using var coordinator = fixture.Create();
        Task<InteractionResult> pending = Execute(coordinator, fixture.Envelope, token.Token);
        if (fixture.HoldPrepare) { await Entered(fixture.PrepareEntered.Task); token.Cancel(); fixture.PreparePermit.SetResult(true); }
        if (fixture.HoldNormalize) { await Entered(fixture.NormalizeEntered.Task); token.Cancel(); fixture.NormalizePermit.SetResult(true); }
        InteractionResult result = await pending;
        Check(result.Status == InteractionStatus.CancelledAsStale && result.ActionPlan.Actions.Count == 0, "cancelled owner returned a committable plan");
        int expectedPrepare = timing == "before-prepare" ? 0 : 1;
        int expectedNormalize = timing == "during-normalize" ? 1 : 0;
        Check(fixture.PrepareCalls == expectedPrepare && fixture.NormalizeCalls == expectedNormalize && fixture.SyncCalls == 0, "cancelled owner was invoked/reentered unexpectedly");
        if (timing == "during-prepare" || timing == "before-prepare") Check(fixture.PostCalls == 0, "postprocess HTTP ran after cancellation at capture boundary");
        AssertNoCommit(fixture.Envelope, result);
    }

    private static async Task FailureAsync(string stage)
    {
        var fixture = new Fixture(stage) { Failure = stage };
        using var coordinator = fixture.Create();
        InteractionResult result = await Execute(coordinator, fixture.Envelope);
        if (stage.EndsWith("-throw", StringComparison.Ordinal))
        {
            Check(result.Status == InteractionStatus.NonRetryableFailure && result.ErrorCode == "pipeline_exception", "owner exception was falsely accepted");
            AssertNoCommit(fixture.Envelope, result);
        }
        else if (stage.EndsWith("-cancel", StringComparison.Ordinal))
        {
            Check(result.Status == InteractionStatus.CancelledAsStale, "owner cancellation was falsely accepted");
            AssertNoCommit(fixture.Envelope, result);
        }
        else
        {
            Check(result.Status == InteractionStatus.Succeeded && result.VisibleReply == "visible:" + stage && result.ActionPlan.Actions.Count == 0, "non-action result lost visible text or gained actions");
        }
        Check(fixture.PrepareCalls == 1 && fixture.NormalizeCalls <= 1 && fixture.SyncCalls == 0, "failure caused owner retry or sync fallback");
        if (stage.StartsWith("prepare-", StringComparison.Ordinal)) Check(fixture.PostCalls == 0 && fixture.NormalizeCalls == 0, "failed/null capture reached HTTP or normalization");
        if (stage == "postprocess-failure") Check(fixture.NormalizeCalls == 0 && result.ErrorCode == "postprocess_fixture_failure", "failed post HTTP reached normalization");
    }

    private static async Task AsyncPrepareSyncParserAsync()
    {
        int prepares = 0, parses = 0;
        var ports = new LegacyInteractionPipelinePorts(_ => Rules("mixed"), (_, _, _) => Prompt("main"), (_, _, _) => Context("mixed"),
            (_, _) => { parses++; return Plan("mixed"); }, (_, _) => "visible", Capabilities, null,
            async (_, _, _, _, _, _) => { prepares++; await Task.Yield(); return Prompt("post"); }, null);
        using var coordinator = LegacyInteractionPipelineComposition.Create(ports, new Gateway((_, _) => Task.FromResult(Success("raw"))), () => 4L);
        InteractionResult result = await Execute(coordinator, Envelope("mixed"));
        Check(result.Status == InteractionStatus.Succeeded && prepares == 1 && parses == 1, "async capture / synchronous parser compatibility failed");
    }

    private static Task InvalidPortsAsync()
    {
        bool rejected = false;
        try
        {
            _ = new LegacyInteractionPipelinePorts(_ => Rules("invalid"), (_, _, _) => Prompt("main"), (_, _, _) => Context("invalid"),
                (_, _) => Plan("invalid"), (_, _) => "visible", Capabilities, (_, _, _, _, _) => Prompt("post"), null,
                (_, _, _) => Task.FromResult(Plan("invalid")));
        }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "async action owner accepted without async capture");
        return Task.CompletedTask;
    }

    private static async Task ConcurrentAsync(bool sameSession)
    {
        var prepareGates = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        var normalizeGates = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        var contexts = new ConcurrentDictionary<PostprocessContext, string>();
        var prepared = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        var normalizing = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        foreach (string id in new[] { "old", "new" }) { prepareGates[id] = Signal(); normalizeGates[id] = Signal(); prepared[id] = Signal(); normalizing[id] = Signal(); }
        int normalizations = 0;
        var ports = new LegacyInteractionPipelinePorts(snapshot => Rules(snapshot.PlayerText), (envelope, _, _) => Prompt(envelope.Snapshot.PlayerText),
            (snapshot, _, _) => Context(snapshot.PlayerText), (_, _) => throw new InvalidOperationException("sync parser must not run"),
            (raw, _) => "visible:" + raw, Capabilities, null,
            async (envelope, _, visible, raw, context, _) =>
            {
                string id = envelope.Snapshot.PlayerText;
                Check(context.AllowedRuleIds.Single() == id && visible == "visible:" + id && raw == id, "request context crossed before capture");
                contexts[context] = id;
                prepared[id].SetResult(true);
                await prepareGates[id].Task;
                return Prompt(id);
            },
            async (raw, context, _) =>
            {
                string id = contexts[context];
                Check(raw == id && context.AllowedRuleIds.Single() == id, "postprocess output/context crossed requests");
                Interlocked.Increment(ref normalizations);
                normalizing[id].SetResult(true);
                await normalizeGates[id].Task;
                return Plan(id);
            });
        using var coordinator = LegacyInteractionPipelineComposition.Create(ports,
            new Gateway((request, _) => Task.FromResult(Success(request.Prompt.Messages.Single().Content))), () => 4L);
        InteractionEnvelope oldEnvelope = Envelope("old", sameSession ? "shared" : "old");
        InteractionEnvelope newEnvelope = Envelope("new", sameSession ? "shared" : "new");
        Task<InteractionResult> old = Execute(coordinator, oldEnvelope);
        await Entered(prepared["old"].Task);
        Task<InteractionResult> current = Execute(coordinator, newEnvelope);
        await Entered(prepared["new"].Task);
        prepareGates["new"].SetResult(true);
        await Entered(normalizing["new"].Task);
        prepareGates["old"].SetResult(true);
        if (!sameSession) await Entered(normalizing["old"].Task);
        normalizeGates["new"].SetResult(true);
        normalizeGates["old"].SetResult(true);
        InteractionResult oldResult = await old;
        InteractionResult newResult = await current;
        Check(newResult.Status == InteractionStatus.Succeeded && newResult.ActionPlan.Actions.Single().TargetId == "new", "new request was lost or mixed");
        if (sameSession)
        {
            Check(oldResult.Status == InteractionStatus.CancelledAsStale && normalizations == 1, "superseded prepare reached normalize or cancelled current request");
            AssertNoCommit(oldEnvelope, oldResult);
        }
        else Check(oldResult.ActionPlan.Actions.Single().TargetId == "old" && normalizations == 2 && contexts.Count == 2, "independent concurrent contexts were not isolated");
    }

    private sealed class Fixture
    {
        internal readonly InteractionEnvelope Envelope;
        internal readonly string Id;
        internal readonly ConcurrentQueue<string> Events = new();
        internal readonly TaskCompletionSource<bool> PrepareEntered = Signal(), PreparePermit = Signal(), NormalizeEntered = Signal(), NormalizePermit = Signal();
        internal int PrepareCalls, NormalizeCalls, PostCalls, SyncCalls;
        internal bool HoldPrepare, HoldNormalize, CancelAfterMain, CancelAfterPost;
        internal string Failure;
        internal CancellationTokenSource Cancellation;
        private PostprocessContext _capturedContext;
        internal Fixture(string id) { Id = id; Envelope = AsyncInteractionOwnerTests.Envelope(id); }
        internal InteractionRequestCoordinator Create()
        {
            var ports = new LegacyInteractionPipelinePorts(_ => Rules(Id), (_, _, _) => Prompt("main"), (_, _, _) => Context(Id),
                (_, _) => { Interlocked.Increment(ref SyncCalls); throw new InvalidOperationException("sync parse must not run"); },
                (_, _) => "visible:" + Id, Capabilities,
                (_, _, _, _, _) => { Interlocked.Increment(ref SyncCalls); throw new InvalidOperationException("sync compose must not run"); },
                async (_, _, visible, raw, context, _) =>
                {
                    Check(visible == "visible:" + Id && raw == "raw:" + Id, "owner capture lost main raw or visible text");
                    _capturedContext = context;
                    Interlocked.Increment(ref PrepareCalls); Events.Enqueue("prepare-enter"); PrepareEntered.SetResult(true);
                    if (HoldPrepare) await PreparePermit.Task;
                    if (Failure == "prepare-throw") throw new InvalidOperationException("fixture prepare failure");
                    if (Failure == "prepare-cancel") throw new OperationCanceledException();
                    Events.Enqueue("prepare-done");
                    return Failure == "prepare-null" ? null : Prompt("post");
                },
                async (raw, context, _) =>
                {
                    Check(ReferenceEquals(context, _capturedContext) && raw == "tags:" + Id, "normalize lost captured context or postprocess raw");
                    Interlocked.Increment(ref NormalizeCalls); Events.Enqueue("normalize-enter"); NormalizeEntered.SetResult(true);
                    if (HoldNormalize) await NormalizePermit.Task;
                    if (Failure == "normalize-throw") throw new InvalidOperationException("fixture normalize failure");
                    if (Failure == "normalize-cancel") throw new OperationCanceledException();
                    Events.Enqueue("normalize-done");
                    return Failure == "normalize-null" ? null : Plan(Id);
                });
            return LegacyInteractionPipelineComposition.Create(ports, new Gateway((request, _) =>
            {
                if (request.Stage == InteractionStage.MainReply)
                {
                    Events.Enqueue("main-http");
                    if (CancelAfterMain) Cancellation.Cancel();
                    return Task.FromResult(Success("raw:" + Id));
                }
                Interlocked.Increment(ref PostCalls); Events.Enqueue("post-http");
                if (CancelAfterPost) Cancellation.Cancel();
                return Task.FromResult(Failure == "postprocess-failure"
                    ? new LlmGenerateResult(LlmResultStatus.RetryableFailure, "", 0, 0, "fixture_failure") : Success("tags:" + Id));
            }), () => 4L);
        }
    }

    private sealed class Gateway : ILlmGateway
    {
        private readonly Func<LlmGenerateRequest, CancellationToken, Task<LlmGenerateResult>> _handler;
        internal Gateway(Func<LlmGenerateRequest, CancellationToken, Task<LlmGenerateResult>> handler) => _handler = handler;
        public Task<LlmGenerateResult> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken) => _handler(request, cancellationToken);
    }

    private sealed class Effects : IActionPlanExecutor, IInteractionMemory
    {
        internal int Actions, Messages;
        public InteractionStatus ValidateAndExecute(ActionPlan plan, GameInteractionSnapshot snapshot) { Actions++; return InteractionStatus.Executed; }
        public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => Array.Empty<PromptMessage>();
        public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> facts) => Messages++;
    }

    private static void AssertNoCommit(InteractionEnvelope envelope, InteractionResult result)
    {
        var effects = new Effects();
        InteractionCommitResult rejected = new InteractionResultCommitter(() => 4L).Commit(envelope, result, effects, effects);
        Check(!rejected.ActionsExecuted && !rejected.HistoryWritten && effects.Actions == 0 && effects.Messages == 0, "failed/stale owner reached actions or memory");
    }
    private static Task<InteractionResult> Execute(InteractionRequestCoordinator coordinator, InteractionEnvelope envelope, CancellationToken token = default)
        => coordinator.ExecuteAsync(envelope, Config, "conversation", "main", token);
    private static InteractionEnvelope Envelope(string id, string session = null) => new(new GameInteractionSnapshot(
        new InteractionIdentity("async-" + (session ?? id), InteractionChannel.Courier, "hero"),
        new TraceContext("async-" + id + "-" + Guid.NewGuid().ToString("N"), 4, 9, "fixture", "1.4"), id, "town", 12, 8,
        Array.Empty<InteractionCandidate>(), Array.Empty<string>(), new Dictionary<string, string>()), Array.Empty<PromptMessage>());
    private static PromptPackage Prompt(string content) => new(new[] { new PromptMessage("user", content) }, 5000, "fixture");
    private static RuleSelection Rules(string id) => new(new[] { id }, Array.Empty<string>());
    private static PostprocessContext Context(string id) => new(new[] { id }, new[] { "ACTION:TEST" }, Capabilities);
    private static ActionPlan Plan(string id) => new(new[] { new ActionRequest("ACTION:TEST", id, new Dictionary<string, string>()) }, "owner:" + id);
    private static LlmGenerateResult Success(string text) => new(LlmResultStatus.Succeeded, text, 0, 0, "");
    private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task Entered(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5));
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
