using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed partial class SceneConversationSessionRuntime
{
    private readonly object _moduleSceneGroupGate = new object();
    private SceneGroupReceipt _activeModuleSceneGroup;

    internal void RegisterModuleSceneGroup(SceneGroupReceipt receipt)
    {
        SceneGroupReceipt previous;
        lock (_moduleSceneGroupGate)
        {
            previous = _activeModuleSceneGroup;
            _activeModuleSceneGroup = receipt;
        }
        previous?.Retire("scene.stale_context");
    }

    internal void RetireModuleSceneGroup(string reason)
    {
        SceneGroupReceipt current;
        lock (_moduleSceneGroupGate)
        {
            current = _activeModuleSceneGroup;
            _activeModuleSceneGroup = null;
        }
        current?.Retire(reason);
    }

    internal void ReleaseModuleSceneGroup(SceneGroupReceipt receipt)
    {
        lock (_moduleSceneGroupGate)
            if (ReferenceEquals(_activeModuleSceneGroup, receipt)) _activeModuleSceneGroup = null;
    }

    internal bool IsModuleSceneGroupCurrent(SceneGroupReceipt receipt, long generation, int sceneSessionId, int conversationEpoch)
    {
        if (!IsSceneContinuationCurrent || !_ports.IsOwnerCurrent() || !SaveRuntimeGuard.IsCurrentGeneration(generation)
            || sceneSessionId != _ports.SceneSessionId() || !IsSceneConversationEpochCurrent(conversationEpoch))
        { receipt?.Fail("scene.stale_context"); return false; }
        if (receipt == null) return true;
        bool active;
        lock (_moduleSceneGroupGate) active = ReferenceEquals(_activeModuleSceneGroup, receipt);
        if (active && !receipt.IsRetired && _ports.IsOwnerCurrent()
            && SaveRuntimeGuard.IsCurrentGeneration(generation)
            && sceneSessionId == _ports.SceneSessionId()
            && IsSceneConversationEpochCurrent(conversationEpoch)) return true;
        receipt.Fail("scene.stale_context");
        return false;
    }

    private sealed class SceneMainThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Action<Action> _post;
        private readonly PendingOperationRegistry _pending;
        private int _retired;
        internal bool IsRetired => Volatile.Read(ref _retired) != 0;
        internal SceneMainThreadSynchronizationContext(Action<Action> post, PendingOperationRegistry pending) { _post = post; _pending = pending; }
        public override void Post(SendOrPostCallback callback, object state)
        {
            int claimed = 0;
            IDisposable registration = null;
            void Resume(bool retired)
            {
                if (Interlocked.CompareExchange(ref claimed, 1, 0) != 0) return;
                if (retired) Interlocked.Exchange(ref _retired, 1);
                SynchronizationContext previous = Current;
                SetSynchronizationContext(this);
                try { callback(state); }
                finally { SetSynchronizationContext(previous); registration?.Dispose(); }
            }
            // Queue clearing must resume the state machine to release its existing lifetime;
            // the retired context guard forbids every subsequent live capture/effect.
            registration = _pending.Register(_pending.Version, () => Resume(true));
            if (registration == null) return;
            if (Volatile.Read(ref claimed) != 0) { registration.Dispose(); return; }
            try { _post(() => Resume(false)); }
            catch { Resume(true); }
        }
    }

    private static bool IsSceneContinuationCurrent => !(SynchronizationContext.Current is SceneMainThreadSynchronizationContext context) || !context.IsRetired;
    private bool IsSceneRequestSourceCurrent(long generation, int sessionId, int epoch) => IsSceneContinuationCurrent
        && _ports.IsOwnerCurrent() && SaveRuntimeGuard.IsCurrentGeneration(generation)
        && sessionId == _ports.SceneSessionId() && epoch == ConversationEpoch;

    internal Task RunSceneGroupOnMainThreadAsync(Func<Task> run)
    {
        if (!IsBannerlordMainThreadForNativeActions())
            throw new InvalidOperationException("scene.group_requires_main_thread");
        SynchronizationContext previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SceneMainThreadSynchronizationContext(_ports.PostMainThread, _pendingMainThreadFunctions));
        try { return run(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    // The public Scene path uses the same shared prompt phases as Courier: live capture and
    // assembly on the game thread, detached routing/knowledge retrieval on a worker.
    internal async Task<MyBehavior.ShoutPromptContext> BuildModuleScenePromptContextAsync(
        Hero hero, CharacterObject character, string playerText, string extraFact,
        string cultureId, string kingdomId, int agentIndex, bool isHero,
        bool hasLore, string lore, List<string> excludedRules,
        long generation, int sessionId, int conversationEpoch)
    {
        SharedPromptRoutingWork routingWork = await _dispatcher.RunAsync(
            "module_scene_prompt_capture", "scene", agentIndex, () =>
            {
                if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || sessionId != _ports.SceneSessionId()
                    || !IsSceneConversationEpochCurrent(conversationEpoch)
                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(agentIndex, hero, character, out _))
                    return null;
                return _ports.CapturePromptRoutingWork(new ScenePromptCaptureRequest(hero, character, playerText, extraFact, cultureId, kingdomId, agentIndex, isHero, hasLore, lore, excludedRules));
            }, (SharedPromptRoutingWork)null);
        if (routingWork == null) return null;
        PromptBuildPhases phases = routingWork.Phases;

        await Task.Run(() =>
        {
            using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
            AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
            try { SharedPromptRoutingRuntime.Run(routingWork); }
            finally { AIConfigHandler.ClearGuardrailRuntimeTarget(); }
        });

        PromptKnowledgeWorkInput knowledge = await _dispatcher.RunAsync(
            "module_scene_knowledge_capture", "scene", agentIndex, () =>
            {
                if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || sessionId != _ports.SceneSessionId()
                    || !IsSceneConversationEpochCurrent(conversationEpoch)
                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(agentIndex, hero, character, out _))
                    return null;
                return _ports.CapturePromptKnowledge(phases, hero ?? character?.HeroObject);
            }, (PromptKnowledgeWorkInput)null);
        if (knowledge == null) return null;
        PromptKnowledgeWorkResult retrieved = await Task.Run(() => MyBehavior.RunSharedKnowledgeRetrieval(knowledge));

        return await _dispatcher.RunAsync(
            "module_scene_prompt_complete", "scene", agentIndex, () =>
            {
                if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || sessionId != _ports.SceneSessionId()
                    || !IsSceneConversationEpochCurrent(conversationEpoch)
                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(agentIndex, hero, character, out _))
                    return null;
                using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
                AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
                try
                {
                    MyBehavior.ApplySharedKnowledgeRetrieval(phases, retrieved);
                    return _ports.CompletePromptCapture(phases, hero, character);
                }
                finally { AIConfigHandler.ClearGuardrailRuntimeTarget(); }
            }, (MyBehavior.ShoutPromptContext)null);
    }

    internal sealed class SceneGroupReceipt
    {
        private readonly SceneConversationSessionRuntime _owner;
        private readonly CoreDialogueOperation _operation;
        private readonly List<CoreSceneUtterance> _utterances = new List<CoreSceneUtterance>();
        private readonly List<Task<bool>> _speech = new List<Task<bool>>();
        private readonly List<Task<ScenePostprocessOutcome>> _postprocess = new List<Task<ScenePostprocessOutcome>>();
        private readonly TaskCompletionSource<bool> _retirement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private string _failure;
        private int _retired;

        internal SceneGroupReceipt(SceneConversationSessionRuntime owner, CoreDialogueOperation operation)
        {
            _owner = owner;
            _operation = operation;
        }

        internal int ConversationEpoch { get; private set; }
        internal long RuntimeGeneration { get; private set; }
        internal int SceneSessionId { get; private set; }
        internal string Failure => _failure ?? "scene.owner_completion_missing";
        internal bool IsRetired => Volatile.Read(ref _retired) != 0;
        internal IReadOnlyList<CoreSceneUtterance> Utterances =>
            new ReadOnlyCollection<CoreSceneUtterance>(new List<CoreSceneUtterance>(_utterances));
        internal string Reply => string.Join("\n", _utterances.Select(item => item.Text));

        internal void BindSource(long generation, int sceneSessionId)
        {
            RuntimeGeneration = generation;
            SceneSessionId = sceneSessionId;
        }
        internal void Bind(int conversationEpoch)
        {
            ConversationEpoch = conversationEpoch;
            _owner.RegisterModuleSceneGroup(this);
        }
        internal void Retire(string reason)
        {
            if (Interlocked.Exchange(ref _retired, 1) != 0) return;
            Fail(reason);
            _retirement.TrySetResult(true);
            _operation.RecordSceneProgress(Utterances);
            _operation.Finish(reason);
        }
        internal void Fail(string reason)
        {
            if (_failure == null) _failure = reason;
        }
        internal void AddTurn(int speakerAgentIndex, string speakerName, string text, Task<bool> speechCompletion)
        {
            if (string.IsNullOrWhiteSpace(text) || speechCompletion == null)
            {
                Fail("scene.no_visible_reply");
                return;
            }
            _utterances.Add(new CoreSceneUtterance(speakerAgentIndex, speakerName, text));
            _speech.Add(speechCompletion);
        }
        internal void AddPostprocess(Task<ScenePostprocessOutcome> completion) => _postprocess.Add(completion);

        internal async Task<bool> SettleAsync()
        {
            if (IsRetired) return false;
            if (_utterances.Count == 0) Fail("scene.no_confirmed_reply");
            Task all = Task.WhenAll(_speech.Cast<Task>().Concat(_postprocess));
            using (var timeout = new CancellationTokenSource())
            {
            Task winner = await Task.WhenAny(all, _retirement.Task,
                Task.Delay(ScenePostprocessGateWaitTimeoutMilliseconds, timeout.Token)).ConfigureAwait(false);
            if (winner == _retirement.Task) return false;
            if (winner != all)
                {
                    Fail("scene.dependency_timeout");
                    return false;
                }
                timeout.Cancel();
            }
            try { await all.ConfigureAwait(false); }
            catch (Exception) { Fail("scene.dependency_failed"); }
            if (_speech.Any(task => task.Status == TaskStatus.RanToCompletion && !task.Result))
                Fail("scene.speech_not_published");
            if (_postprocess.Any(task => task.Status == TaskStatus.RanToCompletion && !task.Result.Succeeded))
                Fail("scene.postprocess_unconfirmed");
            return _failure == null;
        }
    }


    internal async Task RunModuleSceneDialogueAsync(CoreDialogueOperation operation)
    {
        var receipt = new SceneGroupReceipt(this, operation);
        try
        {
            Task execution = await _dispatcher.RunAsync(
                "module_scene_claim", "module", -1, () =>
                {
                    if (!_ports.IsOwnerCurrent() || !_pendingMainThreadFunctions.Accepting
                        || !operation.TryBegin()) return (Task)null;
                    if (!TryClaimModuleSceneTicket(operation.ClientId, operation.ContextIdentity,
                        out ScenePlayerShoutRequest request))
                    {
                        operation.Finish("scene.context_unavailable");
                        return (Task)null;
                    }
                    operation.MarkOwnerAdmitted();
                    receipt.BindSource(request.RuntimeGeneration, request.SceneSessionId);
                    return ProcessCapturedScenePlayerShoutAsync(operation.PlayerText, null,
                        request.TargetingContext.PrimaryAgentIndex, request, null, receipt);
                }, (Task)null).ConfigureAwait(false);
            if (execution != null) await execution.ConfigureAwait(false);
            else receipt.Fail("scene.not_started");
            bool settled = await receipt.SettleAsync().ConfigureAwait(false);
            operation.RecordSceneProgress(receipt.Utterances);
            bool current = settled && await _dispatcher.RunAsync(
                "module_scene_completion", "module", -1, () =>
                    _ports.IsOwnerCurrent() && receipt.ConversationEpoch > 0
                    && SaveRuntimeGuard.IsCurrentGeneration(receipt.RuntimeGeneration)
                    && receipt.SceneSessionId == _ports.SceneSessionId()
                    && IsSceneConversationEpochCurrent(receipt.ConversationEpoch), false).ConfigureAwait(false);
            if (settled && !current) receipt.Fail("scene.stale_context");
            if (current) operation.RecordOwnerCompletion(receipt.Reply, receipt.Utterances);
            operation.Finish(current ? "scene.owner_completion_missing" : receipt.Failure);
        }
        catch (Exception)
        {
            operation.RecordSceneProgress(receipt.Utterances);
            operation.Finish("scene.execution_failed");
        }
        finally { ReleaseModuleSceneGroup(receipt); }
    }

    private async Task<T> RunSceneReplyOnGameThreadAsync<T>(Func<Task<T>> run, string phase, int agentIndex)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        Task<T> started = await _dispatcher.RunAsync(phase, "scene", agentIndex, () =>
        {
            if (!_ports.IsOwnerCurrent() || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return null;
            SynchronizationContext previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SceneMainThreadSynchronizationContext(_ports.PostMainThread, _pendingMainThreadFunctions));
            try { return run(); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }, (Task<T>)null).ConfigureAwait(false);
        return started == null ? default : await started.ConfigureAwait(false);
    }
}
