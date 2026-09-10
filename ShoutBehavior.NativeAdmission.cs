using System;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // 后端票据只覆盖本次完整请求；不把 Task 完成解释成 TTS 播放完成。
    private NativeConversationAdmission _nativeConversationAdmission;
    private long _nativeConversationAdmissionEpoch;
    private long _nativeConversationPresentationRevision;

    internal sealed class NativeConversationAdmission
    {
        internal long Generation;
        internal long ConversationEpoch;
        internal long PresentationRevision;
        internal ConversationManager ConversationManager;
        internal int ConversationToken;
        internal Mission Mission;
        internal Hero Hero;
        internal CharacterObject Character;
        internal string NpcName;
        internal int AgentIndex;
        internal string OpeningExtraFact = "";
        internal string OpeningPrompt = "";
        internal string OpeningSource = "";
    }

    internal sealed class NativeConversationAdmissionException : InvalidOperationException
    {
        internal NativeConversationAdmissionException(string reasonCode, string message) : base(message)
        {
            ReasonCode = reasonCode;
        }
        internal string ReasonCode { get; }
    }

    // 不改变 CanSubmit：它还负责 Overlay 是否存在，busy 不能让正在生成的 UI 被关闭。
    internal static bool IsNativeConversationBackendBusy()
    {
        ShoutBehavior owner = CurrentInstance;
        return owner != null && owner.IsNativeConversationAdmissionCurrent(
            Volatile.Read(ref owner._nativeConversationAdmission), out _);
    }

    // 复用已注册的真实 ConversationEnded 事件，而不是 UI 关闭或可重用的 ActiveToken 推测会话结束。
    internal static void InvalidateNativeConversationAdmissionOnConversationEnd()
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner != null)
        {
            // 同 token 重开也属于另一轮；尚未进入主线程的旧请求同样必须失效。
            Interlocked.Increment(ref owner._nativeConversationAdmissionEpoch);
            Interlocked.Exchange(ref owner._nativeConversationAdmission, null);
        }
    }

    private async Task<string> SubmitNativeConversationAdmittedAsync(string playerText,
        Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted,
        Action<string, Hero, CharacterObject> onMainReplyReady, bool npcInitiatedOpening,
        NativeConversationPresentationScope presentationScope = null)
    {
        if (!npcInitiatedOpening && string.IsNullOrWhiteSpace(playerText))
            return "";
        long generation = SaveRuntimeGuard.CaptureGeneration();
        long conversationEpoch = Interlocked.Read(ref _nativeConversationAdmissionEpoch);
        NativeConversationAdmission admission = await CaptureNativeConversationAdmissionAsync(
            generation, conversationEpoch, npcInitiatedOpening).ConfigureAwait(false);
        if (admission == null)
            return "";
        try
        {
            // Overlay calls admission on the main thread, before its first stream callback can run.
            presentationScope?.Bind(admission);
            return await Task.Run(async delegate
            {
                SynchronizationContext.SetSynchronizationContext(null);
                return await SubmitNativeConversationTextInternalAsync(admission, playerText, onStreamText,
                    currentDialogTextOverride, onPostprocessStarted, onMainReplyReady, npcInitiatedOpening).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        finally
        {
            // 旧请求晚完成只能释放自己的票据，不能清除换会话后新请求的 busy。
            Interlocked.CompareExchange(ref _nativeConversationAdmission, null, admission);
        }
    }

    private Task<NativeConversationAdmission> CaptureNativeConversationAdmissionAsync(long generation, long conversationEpoch, bool npcInitiatedOpening)
    {
        if (IsBannerlordMainThreadForNativeActions())
            return Task.FromResult(CaptureNativeConversationAdmissionOnMainThread(generation, conversationEpoch, npcInitiatedOpening));

        var completion = new TaskCompletionSource<NativeConversationAdmission>(TaskCreationOptions.RunContinuationsAsynchronously);
        int dispatchState = 0; // 0 queued; 1 started; 2 expired before start.
        _mainThreadActions.Enqueue(() =>
        {
            if (Interlocked.CompareExchange(ref dispatchState, 1, 0) != 0)
                return;
            try { completion.TrySetResult(CaptureNativeConversationAdmissionOnMainThread(generation, conversationEpoch, npcInitiatedOpening)); }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        return AwaitCapture();

        async Task<NativeConversationAdmission> AwaitCapture()
        {
            Task winner = await Task.WhenAny(completion.Task,
                Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs)).ConfigureAwait(false);
            if (winner != completion.Task && Interlocked.CompareExchange(ref dispatchState, 2, 0) == 0)
                throw new NativeConversationAdmissionException("native.admission_timeout", "对话请求尚未开始：主线程暂未处理，请稍后重试。");
            // 一旦捕获已开始，就必须接收它的结果；不放弃一个可能已占用后端票据的返回值。
            return await completion.Task.ConfigureAwait(false);
        }
    }

    private NativeConversationAdmission CaptureNativeConversationAdmissionOnMainThread(long generation, long conversationEpoch, bool npcInitiatedOpening)
    {
        if (!IsBannerlordMainThreadForNativeActions())
            throw new InvalidOperationException("native.admission_requires_main_thread");
        if (!ReferenceEquals(CurrentInstance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation)
            || conversationEpoch != Interlocked.Read(ref _nativeConversationAdmissionEpoch)
            || !CanSubmitNativeConversationForExternal())
            return null;
        if (IsNativeConversationAdmissionCurrent(Volatile.Read(ref _nativeConversationAdmission), out _))
            throw new NativeConversationAdmissionException("native.busy", "上一轮对话仍在处理，请稍后再提交。");
        NativeConversationAdmission admission = CaptureNativeConversationContext(generation, conversationEpoch);
        if (admission == null)
            return null;
        Interlocked.Exchange(ref _nativeConversationAdmission, admission);
        try
        {
            // 主动开场与普通输入共用准入；拒绝 busy 之前绝不消费待开场状态。
            if (npcInitiatedOpening && !NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(admission.Hero,
                out admission.OpeningExtraFact, out admission.OpeningPrompt, out admission.OpeningSource))
            {
                Interlocked.CompareExchange(ref _nativeConversationAdmission, null, admission);
                return null;
            }
            admission.PresentationRevision = Interlocked.Increment(ref _nativeConversationPresentationRevision);
            return admission;
        }
        catch
        {
            Interlocked.CompareExchange(ref _nativeConversationAdmission, null, admission);
            throw;
        }
    }

    // Capture-only snapshot: no reservation, opening consumption, network or action side effects.
    private NativeConversationAdmission CaptureNativeConversationContext(long generation, long conversationEpoch)
    {
        if (!TryResolveNativeConversationTarget(out Hero hero, out CharacterObject character, out string npcName))
            return null;
        ConversationManager manager = Campaign.Current?.ConversationManager;
        if (manager == null || !manager.IsConversationInProgress)
            return null;
        var admission = new NativeConversationAdmission
        {
            Generation = generation,
            ConversationEpoch = conversationEpoch,
            PresentationRevision = Interlocked.Read(ref _nativeConversationPresentationRevision),
            ConversationManager = manager,
            ConversationToken = manager.ActiveToken,
            Mission = Mission.Current,
            Hero = hero,
            Character = character,
            NpcName = npcName,
            AgentIndex = TryResolveNativeConversationAgentIndex(hero, character)
        };
        if (!IsNativeConversationResponseTargetAvailableForActionDispatch(admission.AgentIndex, hero, character, out _))
            return null;
        return admission;
    }

    private bool IsNativeConversationAdmissionCurrent(NativeConversationAdmission admission, out string reason)
    {
        reason = "native.admission_stale";
        return admission != null && ReferenceEquals(Volatile.Read(ref _nativeConversationAdmission), admission)
            && IsNativeConversationContextCurrent(admission, out reason);
    }

    // Cheap stamp used from an existing UI Tick: no target/agent enumeration or provider work.
    private bool IsNativeConversationContextStampCurrent(NativeConversationAdmission admission)
    {
        if (admission == null || !IsBannerlordMainThreadForNativeActions()
            || !ReferenceEquals(CurrentInstance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(admission.Generation)
            || admission.ConversationEpoch != Interlocked.Read(ref _nativeConversationAdmissionEpoch))
            return false;
        ConversationManager current = Campaign.Current?.ConversationManager;
        return ReferenceEquals(current, admission.ConversationManager) && current?.IsConversationInProgress == true
            && current.ActiveToken == admission.ConversationToken && ReferenceEquals(Mission.Current, admission.Mission);
    }

    private bool IsNativeConversationContextCurrent(NativeConversationAdmission admission, out string reason)
    {
        reason = "native.admission_stale";
        if (!IsNativeConversationContextStampCurrent(admission) || PlayerEncounterCompat.IsInPostBattleResultFlow())
            return false;
        if (!TryResolveNativeConversationTarget(out Hero hero, out CharacterObject character, out _)
            || !ReferenceEquals(hero, admission.Hero) || !ReferenceEquals(character, admission.Character)
            || TryResolveNativeConversationAgentIndex(hero, character) != admission.AgentIndex)
            return false;
        return IsNativeConversationResponseTargetAvailableForActionDispatch(admission.AgentIndex,
            admission.Hero, admission.Character, out reason);
    }

    // This is an internal read-only observation capability, not permission to execute an action.
    // It survives backend Task completion, but not a later admission or a conversation/save change.
    internal sealed class NativeConversationPresentationScope
    {
        private readonly ShoutBehavior _owner;
        private NativeConversationAdmission _snapshot;
        private int _submissionStarted;

        internal NativeConversationPresentationScope(ShoutBehavior owner, NativeConversationAdmission snapshot)
        { _owner = owner; _snapshot = snapshot; }

        internal bool IsOwnedBy(ShoutBehavior owner) => ReferenceEquals(_owner, owner);
        internal bool TryBeginSubmission() => Interlocked.CompareExchange(ref _submissionStarted, 1, 0) == 0;
        internal void Bind(NativeConversationAdmission admission) { _snapshot = admission; }
        internal bool HasCurrentContext()
            => _snapshot != null && _snapshot.PresentationRevision == Interlocked.Read(ref _owner._nativeConversationPresentationRevision)
                && _owner.IsNativeConversationContextStampCurrent(_snapshot);
        internal bool IsCurrent()
            => HasCurrentContext() && _owner.IsNativeConversationContextCurrent(_snapshot, out _);
    }

    internal static NativeConversationPresentationScope CaptureNativeConversationPresentationScopeForOverlay()
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner == null || !IsBannerlordMainThreadForNativeActions() || !CanSubmitNativeConversationForExternal())
            return null;
        NativeConversationAdmission snapshot = owner.CaptureNativeConversationContext(SaveRuntimeGuard.CaptureGeneration(),
            Interlocked.Read(ref owner._nativeConversationAdmissionEpoch));
        return snapshot == null ? null : new NativeConversationPresentationScope(owner, snapshot);
    }

    internal static Task<string> SubmitNativeConversationForOverlayAsync(NativeConversationPresentationScope scope,
        string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted,
        Action<string, Hero, CharacterObject> onMainReplyReady, bool npcInitiatedOpening)
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner == null || scope == null || !scope.IsOwnedBy(owner) || !scope.IsCurrent())
            return Task.FromResult("");
        if (!scope.TryBeginSubmission())
            return Task.FromException<string>(new NativeConversationAdmissionException("native.presentation_scope_reused", "本次对话票据已使用，请重新提交。"));
        return owner.SubmitNativeConversationAdmittedAsync(playerText, onStreamText, currentDialogTextOverride,
            onPostprocessStarted, onMainReplyReady, npcInitiatedOpening, scope);
    }
}
