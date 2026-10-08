using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private readonly NativeAdmissionApplicationAdapter NativeAdmissions;
    // 后端票据只覆盖本次完整请求；不把 Task 完成解释成 TTS 播放完成。
    private readonly NativeConversationAdmissionOwner<NativeConversationAdmission> _nativeAdmissionOwner =
        new NativeConversationAdmissionOwner<NativeConversationAdmission>();

    internal sealed class NativeConversationAdmission
    {
        internal AnimusForge.Refactor.Runtime.ConversationRequestLifetime Lifetime;
        internal CoreDialogueOperation ModuleOperation;
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
        internal NativeMeetingElapsedBoundary MeetingElapsedBoundary;
        internal string MeetingElapsedContext => MeetingElapsedBoundary?.Accepted == false ? MeetingElapsedBoundary.Context : "";
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
        return CurrentInstance?.NativeAdmissions.IsBusy() == true;
    }

    // UI tick observation only. Submission still uses the full target validation
    // above; never run its target/agent resolution every frame just to grey a button.
    internal static bool IsNativeConversationBackendBusyForUi()
    {
        return CurrentInstance?.NativeAdmissions.IsBusyForUi() == true;
    }

    // 复用已注册的真实 ConversationEnded 事件，而不是 UI 关闭或可重用的 ActiveToken 推测会话结束。
    internal static void InvalidateNativeConversationAdmissionOnConversationEnd()
    {
        CurrentInstance?.NativeAdmissions.EndConversation();
    }

    private Task<string> SubmitNativeConversationAdmittedAsync(string playerText,
        Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted,
        Action<string, Hero, CharacterObject> onMainReplyReady, bool npcInitiatedOpening,
        NativeConversationPresentationScope presentationScope = null, CoreDialogueOperation moduleOperation = null)
    {
        return NativeAdmissions.SubmitNativeConversationAdmittedAsync(playerText,onStreamText,currentDialogTextOverride,onPostprocessStarted,onMainReplyReady,npcInitiatedOpening,presentationScope?.Lease,moduleOperation);
    }

    private Task<NativeConversationAdmission> CaptureNativeConversationAdmissionAsync(long generation, long conversationEpoch, bool npcInitiatedOpening)
    {
        return NativeAdmissions.CaptureNativeConversationAdmissionAsync(generation,conversationEpoch,npcInitiatedOpening);
    }

    private NativeConversationAdmission CaptureNativeConversationAdmissionOnMainThread(long generation, long conversationEpoch, bool npcInitiatedOpening)
    {
        return NativeAdmissions.CaptureNativeConversationAdmissionOnMainThread(generation,conversationEpoch,npcInitiatedOpening);
    }

    // Capture-only snapshot: no reservation, opening consumption, network or action side effects.
    private NativeConversationAdmission CaptureNativeConversationContext(long generation, long conversationEpoch)
    {
        return NativeAdmissions.CaptureNativeConversationContext(generation,conversationEpoch);
    }

    private bool IsNativeConversationAdmissionCurrent(NativeConversationAdmission admission, out string reason)
    {
        return NativeAdmissions.IsNativeConversationAdmissionCurrent(admission,out reason);
    }

    // Cheap stamp used from an existing UI Tick: no target/agent enumeration or provider work.
    private bool IsNativeConversationContextStampCurrent(NativeConversationAdmission admission)
    {
        return NativeAdmissions.IsNativeConversationContextStampCurrent(admission);
    }

    private bool IsNativeConversationContextCurrent(NativeConversationAdmission admission, out string reason)
    {
        return NativeAdmissions.IsNativeConversationContextCurrent(admission,out reason);
    }

    // This is an internal read-only observation capability, not permission to execute an action.
    // It survives backend Task completion, but not a later admission or a conversation/save change.
    internal sealed class NativeConversationPresentationScope
    {
        internal readonly NativeAdmissionApplicationAdapter.PresentationLease Lease;
        internal NativeConversationPresentationScope(NativeAdmissionApplicationAdapter.PresentationLease lease) { Lease=lease; }
        internal NativeConversationPresentationScope(ShoutBehavior owner,NativeConversationAdmission snapshot)
        { Lease=new NativeAdmissionApplicationAdapter.PresentationLease(owner.NativeAdmissions,snapshot); }
        internal bool IsOwnedBy(ShoutBehavior owner)=>Lease.IsOwnedBy(owner.NativeAdmissions);
        internal bool TryBeginSubmission()=>Lease.TryBeginSubmission();
        internal void Bind(NativeConversationAdmission admission)=>Lease.Bind(admission);
        internal bool HasCurrentContext()=>Lease.HasCurrentContext();
        internal bool IsCurrent()=>Lease.IsCurrent();
        internal bool HasCurrentConversationContext()=>Lease.HasCurrentConversationContext();
    }

    internal static NativeConversationPresentationScope CaptureNativeConversationPresentationScopeForOverlay()
    {
        var lease = CurrentInstance?.NativeAdmissions.CapturePresentation(); return lease == null ? null : new NativeConversationPresentationScope(lease);
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
