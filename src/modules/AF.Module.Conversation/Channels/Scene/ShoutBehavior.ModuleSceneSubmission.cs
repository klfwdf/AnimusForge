using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private readonly object _moduleSceneGroupGate = new object();
    private SceneGroupReceipt _activeModuleSceneGroup;

    private void RegisterModuleSceneGroup(SceneGroupReceipt receipt)
    {
        SceneGroupReceipt previous;
        lock (_moduleSceneGroupGate)
        {
            previous = _activeModuleSceneGroup;
            _activeModuleSceneGroup = receipt;
        }
        previous?.Retire("scene.stale_context");
    }

    private void RetireModuleSceneGroup(string reason)
    {
        SceneGroupReceipt current;
        lock (_moduleSceneGroupGate)
        {
            current = _activeModuleSceneGroup;
            _activeModuleSceneGroup = null;
        }
        current?.Retire(reason);
    }

    private void ReleaseModuleSceneGroup(SceneGroupReceipt receipt)
    {
        lock (_moduleSceneGroupGate)
            if (ReferenceEquals(_activeModuleSceneGroup, receipt)) _activeModuleSceneGroup = null;
    }

    private bool IsModuleSceneGroupCurrent(SceneGroupReceipt receipt, long generation, int sceneSessionId, int conversationEpoch)
    {
        if (receipt == null) return true;
        bool active;
        lock (_moduleSceneGroupGate) active = ReferenceEquals(_activeModuleSceneGroup, receipt);
        if (active && !receipt.IsRetired && ReferenceEquals(CurrentInstance, this)
            && SaveRuntimeGuard.IsCurrentGeneration(generation)
            && sceneSessionId == Volatile.Read(ref _sceneHistorySessionId)
            && IsSceneConversationEpochCurrent(conversationEpoch)) return true;
        receipt.Fail("scene.stale_context");
        return false;
    }

    private sealed class SceneMainThreadSynchronizationContext : SynchronizationContext
    {
        private readonly ShoutBehavior _owner;
        internal SceneMainThreadSynchronizationContext(ShoutBehavior owner) { _owner = owner; }
        public override void Post(SendOrPostCallback callback, object state)
        {
            _owner._mainThreadActions.Enqueue(() =>
            {
                SynchronizationContext previous = Current;
                SetSynchronizationContext(this);
                try { callback(state); }
                finally { SetSynchronizationContext(previous); }
            });
        }
    }

    private Task RunSceneGroupOnMainThreadAsync(Func<Task> run)
    {
        if (!IsBannerlordMainThreadForNativeActions())
            throw new InvalidOperationException("scene.group_requires_main_thread");
        SynchronizationContext previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SceneMainThreadSynchronizationContext(this));
        try { return run(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    // The public Scene path uses the same shared prompt phases as Courier: live capture and
    // assembly on the game thread, detached routing/knowledge retrieval on a worker.
    private async Task<MyBehavior.ShoutPromptContext> BuildModuleScenePromptContextAsync(
        Hero hero, CharacterObject character, string playerText, string extraFact,
        string cultureId, string kingdomId, int agentIndex, bool isHero,
        bool hasLore, string lore, List<string> excludedRules,
        long generation, int sessionId, int conversationEpoch)
    {
        MyBehavior owner = null;
        PromptBuildPhases phases = await RunNativeConversationMainThreadFuncAsync(
            "module_scene_prompt_capture", "scene", agentIndex, () =>
            {
                if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || sessionId != Volatile.Read(ref _sceneHistorySessionId)
                    || !IsSceneConversationEpochCurrent(conversationEpoch)
                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(agentIndex, hero, character, out _))
                    return null;
                owner = MyBehavior.Instance;
                return owner?.BeginSharedPromptBuild(hero, playerText, extraFact, cultureId,
                    isHero, character, kingdomId, agentIndex,
                    suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: hasLore,
                    prefetchedLoreContext: lore, excludedRuleIds: null,
                    preprocessExcludedRuleIds: excludedRules, forcedPreprocessRuleIds: null,
                    preprocessMentionedEntities: null);
            }, (PromptBuildPhases)null);
        if (phases == null || owner == null) return null;

        await Task.Run(() =>
        {
            using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
            AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
            try { owner.RunSharedPromptRouting(phases); }
            finally { AIConfigHandler.ClearGuardrailRuntimeTarget(); }
        });

        PromptKnowledgeWorkInput knowledge = await RunNativeConversationMainThreadFuncAsync(
            "module_scene_knowledge_capture", "scene", agentIndex, () =>
            {
                if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || sessionId != Volatile.Read(ref _sceneHistorySessionId)
                    || !IsSceneConversationEpochCurrent(conversationEpoch)
                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(agentIndex, hero, character, out _))
                    return null;
                owner.CaptureSharedKnowledgeSnapshot(phases, hero ?? character?.HeroObject);
                return MyBehavior.CreateSharedKnowledgeWorkInput(phases);
            }, (PromptKnowledgeWorkInput)null);
        if (knowledge == null) return null;
        PromptKnowledgeWorkResult retrieved = await Task.Run(() => MyBehavior.RunSharedKnowledgeRetrieval(knowledge));

        return await RunNativeConversationMainThreadFuncAsync(
            "module_scene_prompt_complete", "scene", agentIndex, () =>
            {
                if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || sessionId != Volatile.Read(ref _sceneHistorySessionId)
                    || !IsSceneConversationEpochCurrent(conversationEpoch)
                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(agentIndex, hero, character, out _))
                    return null;
                using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
                AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
                try
                {
                    MyBehavior.ApplySharedKnowledgeRetrieval(phases, retrieved);
                    return owner.CompleteSharedPromptBuild(phases, hero, character, null);
                }
                finally { AIConfigHandler.ClearGuardrailRuntimeTarget(); }
            }, (MyBehavior.ShoutPromptContext)null);
    }

    private sealed class SceneGroupReceipt
    {
        private readonly ShoutBehavior _owner;
        private readonly CoreDialogueOperation _operation;
        private readonly List<CoreSceneUtterance> _utterances = new List<CoreSceneUtterance>();
        private readonly List<Task<bool>> _speech = new List<Task<bool>>();
        private readonly List<Task<ScenePostprocessOutcome>> _postprocess = new List<Task<ScenePostprocessOutcome>>();
        private readonly TaskCompletionSource<bool> _retirement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private string _failure;
        private int _retired;

        internal SceneGroupReceipt(ShoutBehavior owner, CoreDialogueOperation operation)
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

    internal static void SubmitModuleSceneDialogue(CoreDialogueOperation operation)
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner == null)
        {
            operation.Finish("scene.owner_unavailable");
            return;
        }
        _ = owner.RunModuleSceneDialogueAsync(operation);
    }

    private async Task RunModuleSceneDialogueAsync(CoreDialogueOperation operation)
    {
        var receipt = new SceneGroupReceipt(this, operation);
        try
        {
            Task execution = await RunNativeConversationMainThreadFuncAsync(
                "module_scene_claim", "module", -1, () =>
                {
                    if (!ReferenceEquals(CurrentInstance, this) || !_pendingMainThreadFunctions.Accepting
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
            bool current = settled && await RunNativeConversationMainThreadFuncAsync(
                "module_scene_completion", "module", -1, () =>
                    ReferenceEquals(CurrentInstance, this) && receipt.ConversationEpoch > 0
                    && SaveRuntimeGuard.IsCurrentGeneration(receipt.RuntimeGeneration)
                    && receipt.SceneSessionId == Volatile.Read(ref _sceneHistorySessionId)
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
}
