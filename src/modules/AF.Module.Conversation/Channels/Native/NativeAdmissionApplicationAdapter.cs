using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;
using NativeConversationAdmission = AnimusForge.ShoutBehavior.NativeConversationAdmission;
using NativeConversationAdmissionException = AnimusForge.ShoutBehavior.NativeConversationAdmissionException;

namespace AnimusForge;

internal delegate bool NativeAdmissionTargetResolver(out Hero hero,out CharacterObject character,out string name);
internal delegate bool NativeAdmissionTargetGuard(int agentIndex,Hero hero,CharacterObject character,out string reason);
internal delegate Task<string> NativeAdmissionTurn(NativeConversationAdmission admission,string text,Action<string> stream,string dialog,Action<string> postprocess,Action<string,Hero,CharacterObject> reply,bool opening);

// Request-frequency capture/reservation; injected leaves never own admission or completion.
internal sealed class NativeAdmissionApplicationAdapter
{
    private readonly NativeConversationAdmissionOwner<NativeConversationAdmission> _nativeAdmissionOwner;
    private readonly Func<bool> _isMainThread,_isCurrentOwner,_canSubmit;
    private readonly Action<Action> _post;
    private readonly int _timeoutMs;
    private readonly NativeAdmissionTargetResolver _resolveTarget;
    private readonly Func<Hero,CharacterObject,int> _resolveAgent;
    private readonly NativeAdmissionTargetGuard _targetAvailable;
    private readonly NativeAdmissionTurn _runTurn;
    // At most one admitted opening payload. Its source is consumed only once; a
    // manual retry before a ready reply may reuse it within the same conversation.
    private NativeOpeningRetry _openingRetry;
    private readonly NativeMeetingElapsedOwner _meetingElapsed = new();
    internal void CaptureMeetingElapsedBoundary(NativeConversationAdmission admission, string key, Func<NativeMeetingElapsedSnapshot> capture)
    {
        if (!_isMainThread() || !IsNativeConversationAdmissionCurrent(admission, out _)) return;
        admission.MeetingElapsedBoundary = _meetingElapsed.Capture(admission.Generation, admission.ConversationEpoch, key, capture);
    }
    internal void ConfirmMeetingElapsedBoundary(NativeConversationAdmission admission)
    {
        if (_isMainThread() && IsNativeConversationAdmissionCurrent(admission, out _))
            _meetingElapsed.Confirm(admission.MeetingElapsedBoundary);
    }
    private sealed class NativeOpeningRetry
    {
        internal readonly long Generation, Epoch;
        internal readonly Hero Hero;
        internal readonly CharacterObject Character;
        internal readonly ConversationManager Manager;
        internal readonly int Token;
        internal readonly Mission Mission;
        internal readonly string Fact, Prompt, Source;
        internal NativeOpeningRetry(NativeConversationAdmission admission)
        {
            Generation = admission.Generation; Epoch = admission.ConversationEpoch;
            Hero = admission.Hero; Character = admission.Character;
            Manager = admission.ConversationManager; Token = admission.ConversationToken;
            Mission = admission.Mission;
            Fact = admission.OpeningExtraFact; Prompt = admission.OpeningPrompt; Source = admission.OpeningSource;
        }
        internal bool Matches(NativeConversationAdmission admission) => admission != null
            && Generation == admission.Generation && Epoch == admission.ConversationEpoch
            && ReferenceEquals(Hero, admission.Hero) && ReferenceEquals(Character, admission.Character)
            && ReferenceEquals(Manager, admission.ConversationManager) && Token == admission.ConversationToken
            && ReferenceEquals(Mission, admission.Mission);
        internal void Bind(NativeConversationAdmission admission)
        { admission.OpeningExtraFact = Fact; admission.OpeningPrompt = Prompt; admission.OpeningSource = Source; }
    }
    private NativeOpeningRetry ReadOpeningRetry(NativeConversationAdmission admission, bool retireStale = false)
    {
        NativeOpeningRetry retry = Volatile.Read(ref _openingRetry);
        if (retry != null && !retry.Matches(admission))
        {
            if (retireStale) Interlocked.CompareExchange(ref _openingRetry, null, retry);
            return null;
        }
        return retry;
    }
    internal void RetireOpeningForAcceptedReply(NativeConversationAdmission admission)
    {
        NativeOpeningRetry retry = ReadOpeningRetry(admission);
        if (retry != null) Interlocked.CompareExchange(ref _openingRetry, null, retry);
    }

    internal NativeAdmissionApplicationAdapter(NativeConversationAdmissionOwner<NativeConversationAdmission> owner,
        Func<bool> isMainThread,Func<bool> isCurrentOwner,Func<bool> canSubmit,Action<Action> post,int timeoutMs,
        NativeAdmissionTargetResolver resolveTarget,Func<Hero,CharacterObject,int> resolveAgent,
        NativeAdmissionTargetGuard targetAvailable,NativeAdmissionTurn runTurn)
    { _nativeAdmissionOwner=owner;_isMainThread=isMainThread;_isCurrentOwner=isCurrentOwner;_canSubmit=canSubmit;
      _post=post;_timeoutMs=timeoutMs;_resolveTarget=resolveTarget;_resolveAgent=resolveAgent;_targetAvailable=targetAvailable;_runTurn=runTurn; }
    internal bool IsBusy() => IsNativeConversationAdmissionCurrent(_nativeAdmissionOwner.Current,out _);
    internal bool IsBusyForUi()
    { var a=_nativeAdmissionOwner.Current;return a!=null&&a.Lifetime?.Token.IsCancellationRequested!=true&&_nativeAdmissionOwner.Owns(a)&&IsNativeConversationContextStampCurrent(a); }
    internal void EndConversation() { Interlocked.Exchange(ref _openingRetry, null);_nativeAdmissionOwner.Current?.Lifetime?.Retire();_nativeAdmissionOwner.EndConversation(); }
    internal PresentationLease CapturePresentation()
    {
        if(!_isMainThread()||!_canSubmit()||!_isCurrentOwner()) return null;
        var snapshot=CaptureNativeConversationContext(SaveRuntimeGuard.CaptureGeneration(),_nativeAdmissionOwner.ConversationEpoch);
        ReadOpeningRetry(snapshot, retireStale: true);
        return snapshot==null?null:new PresentationLease(this,snapshot);
    }
    internal sealed class PresentationLease
    {
        private readonly NativeAdmissionApplicationAdapter _owner;
        private NativeConversationAdmission _snapshot;
        private int _submissionStarted;
        internal PresentationLease(NativeAdmissionApplicationAdapter owner,NativeConversationAdmission snapshot) { _owner=owner;_snapshot=snapshot; }
        internal bool IsOwnedBy(NativeAdmissionApplicationAdapter owner)=>ReferenceEquals(_owner,owner);
        internal bool TryBeginSubmission()=>Interlocked.CompareExchange(ref _submissionStarted,1,0)==0;
        internal void Bind(NativeConversationAdmission a) { _snapshot=a; }
        internal bool HasCurrentContext()=>_snapshot!=null&&_owner._nativeAdmissionOwner.IsPresentationCurrent(_snapshot.PresentationRevision)&&_owner.IsNativeConversationContextStampCurrent(_snapshot);
        internal bool IsCurrent()=>HasCurrentContext()&&_owner.IsNativeConversationContextCurrent(_snapshot,out _);
        internal bool HasCurrentConversationContext()=>_snapshot!=null&&_owner.IsNativeConversationContextCurrent(_snapshot,out _);
    }
internal async Task<string> SubmitNativeConversationAdmittedAsync(string playerText,
        Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted,
        Action<string, Hero, CharacterObject> onMainReplyReady, bool npcInitiatedOpening,
        PresentationLease presentationScope = null, CoreDialogueOperation moduleOperation = null)
    {
        if (!npcInitiatedOpening && string.IsNullOrWhiteSpace(playerText))
            return "";
        long generation = SaveRuntimeGuard.CaptureGeneration();
        long conversationEpoch = _nativeAdmissionOwner.ConversationEpoch;
        NativeConversationAdmission admission = await CaptureNativeConversationAdmissionAsync(
            generation, conversationEpoch, npcInitiatedOpening).ConfigureAwait(false);
        if (admission == null)
            return "";
        try
        {
            // Overlay calls admission on the main thread, before its first stream callback can run.
            admission.ModuleOperation = moduleOperation;
            moduleOperation?.MarkOwnerAdmitted();
            presentationScope?.Bind(admission);
            NativeOpeningRetry openingRetry = ReadOpeningRetry(admission);
            Action<string, Hero, CharacterObject> replyReady = (text, hero, character) =>
            {
                // A late callback can retire only the payload captured by this turn.
                if (openingRetry != null && !string.IsNullOrWhiteSpace(text))
                    Interlocked.CompareExchange(ref _openingRetry, null, openingRetry);
                onMainReplyReady?.Invoke(text, hero, character);
            };
            return await Task.Run(async delegate
            {
                using IDisposable requestWorker = admission.Lifetime.Enter();
                using IDisposable cancellationScope = LlmNonStreamingTransport.PushOwnerCancellation(admission.Lifetime.Token);
                SynchronizationContext.SetSynchronizationContext(null);
                return await _runTurn(admission, playerText, onStreamText,
                    currentDialogTextOverride, onPostprocessStarted, replyReady, npcInitiatedOpening).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch
        {
            RetireOpeningForAcceptedReply(admission);
            throw;
        }
        finally
        {
            // 旧请求晚完成只能释放自己的票据，不能清除换会话后新请求的 busy。
            admission.Lifetime.Retire();
            _nativeAdmissionOwner.Release(admission);
        }
    }
internal Task<NativeConversationAdmission> CaptureNativeConversationAdmissionAsync(long generation, long conversationEpoch, bool npcInitiatedOpening)
    {
        if (_isMainThread())
            return Task.FromResult(CaptureNativeConversationAdmissionOnMainThread(generation, conversationEpoch, npcInitiatedOpening));

        var completion = new TaskCompletionSource<NativeConversationAdmission>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchClaim = new NativeConversationDispatchClaim();
        _post(() =>
        {
            if (!dispatchClaim.TryStart())
                return;
            try { completion.TrySetResult(CaptureNativeConversationAdmissionOnMainThread(generation, conversationEpoch, npcInitiatedOpening)); }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        return AwaitCapture();

        async Task<NativeConversationAdmission> AwaitCapture()
        {
            Task winner = await Task.WhenAny(completion.Task,
                Task.Delay(_timeoutMs)).ConfigureAwait(false);
            if (winner != completion.Task && dispatchClaim.TryExpireBeforeStart())
                throw new NativeConversationAdmissionException("native.admission_timeout", "对话请求尚未开始：主线程暂未处理，请稍后重试。");
            // 一旦捕获已开始，就必须接收它的结果；不放弃一个可能已占用后端票据的返回值。
            return await completion.Task.ConfigureAwait(false);
        }
    }
internal NativeConversationAdmission CaptureNativeConversationAdmissionOnMainThread(long generation, long conversationEpoch, bool npcInitiatedOpening)
    {
        if (!_isMainThread())
            throw new InvalidOperationException("native.admission_requires_main_thread");
        if (!_isCurrentOwner())
        {
            Interlocked.Exchange(ref _openingRetry, null);
            return null;
        }
        // An old queued capture must not clear a newer conversation's retry.
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
            || !_nativeAdmissionOwner.IsConversationEpochCurrent(conversationEpoch)) return null;
        if (!_canSubmit()) return null;
        if (IsNativeConversationAdmissionCurrent(_nativeAdmissionOwner.Current, out _))
            throw new NativeConversationAdmissionException("native.busy", "上一轮对话仍在处理，请稍后再提交。");
        NativeConversationAdmission admission = CaptureNativeConversationContext(generation, conversationEpoch);
        if (admission == null)
        {
            Interlocked.Exchange(ref _openingRetry, null);
            return null;
        }
        _nativeAdmissionOwner.Current?.Lifetime?.Retire();
        admission.Lifetime = new AnimusForge.Refactor.Runtime.ConversationRequestLifetime();
        _nativeAdmissionOwner.ReserveCaptured(admission);
        try
        {
            // 主动开场与普通输入共用准入；拒绝 busy 之前绝不消费待开场状态。
            NativeOpeningRetry openingRetry = ReadOpeningRetry(admission, retireStale: true);
            if (npcInitiatedOpening)
            {
                if (openingRetry != null)
                {
                    openingRetry.Bind(admission);
                }
                else
                {
                    if (!NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(admission.Hero,
                        out admission.OpeningExtraFact, out admission.OpeningPrompt, out admission.OpeningSource))
                    {
                        admission.Lifetime.Retire();
                        _nativeAdmissionOwner.Release(admission);
                        return null;
                    }
                    Volatile.Write(ref _openingRetry, new NativeOpeningRetry(admission));
                }
            }
            admission.PresentationRevision = _nativeAdmissionOwner.BeginPresentation();
            return admission;
        }
        catch
        {
            admission.Lifetime.Retire();
            _nativeAdmissionOwner.Release(admission);
            throw;
        }
    }
internal NativeConversationAdmission CaptureNativeConversationContext(long generation, long conversationEpoch)
    {
        if (!_resolveTarget(out Hero hero, out CharacterObject character, out string npcName))
            return null;
        ConversationManager manager = Campaign.Current?.ConversationManager;
        if (manager == null || !manager.IsConversationInProgress)
            return null;
        var admission = new NativeConversationAdmission
        {
            Generation = generation,
            ConversationEpoch = conversationEpoch,
            PresentationRevision = _nativeAdmissionOwner.PresentationRevision,
            ConversationManager = manager,
            ConversationToken = manager.ActiveToken,
            Mission = Mission.Current,
            Hero = hero,
            Character = character,
            NpcName = npcName,
            AgentIndex = _resolveAgent(hero, character)
        };
        if (!_targetAvailable(admission.AgentIndex, hero, character, out _))
            return null;
        return admission;
    }
internal bool IsNativeConversationAdmissionCurrent(NativeConversationAdmission admission, out string reason)
    {
        reason = "native.admission_stale";
        return admission != null && admission.Lifetime?.Token.IsCancellationRequested != true
            && _nativeAdmissionOwner.Owns(admission)
            && IsNativeConversationContextCurrent(admission, out reason);
    }
internal bool IsNativeConversationContextStampCurrent(NativeConversationAdmission admission)
    {
        if (admission == null || !_isMainThread()
            || !_isCurrentOwner()
            || !SaveRuntimeGuard.IsCurrentGeneration(admission.Generation)
            || !_nativeAdmissionOwner.IsConversationEpochCurrent(admission.ConversationEpoch))
            return false;
        ConversationManager current = Campaign.Current?.ConversationManager;
        return ReferenceEquals(current, admission.ConversationManager) && current?.IsConversationInProgress == true
            && current.ActiveToken == admission.ConversationToken && ReferenceEquals(Mission.Current, admission.Mission);
    }
internal bool IsNativeConversationContextCurrent(NativeConversationAdmission admission, out string reason)
    {
        reason = "native.admission_stale";
        if (!IsNativeConversationContextStampCurrent(admission) || PlayerEncounterCompat.IsInPostBattleResultFlow())
            return false;
        if (!_resolveTarget(out Hero hero, out CharacterObject character, out _)
            || !ReferenceEquals(hero, admission.Hero) || !ReferenceEquals(character, admission.Character)
            || _resolveAgent(hero, character) != admission.AgentIndex)
            return false;
        return _targetAvailable(admission.AgentIndex,
            admission.Hero, admission.Character, out reason);
    }
}
