using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private AnimusForge.Refactor.Adapters.NativePendingHistoryApplicationAdapter _nativePendingHistoryApplication;
    private AnimusForge.Refactor.Adapters.NativePendingHistoryApplicationAdapter NativePendingHistoryApplication => _nativePendingHistoryApplication ??= new AnimusForge.Refactor.Adapters.NativePendingHistoryApplicationAdapter(
        IsBannerlordMainThreadForNativeActions, _mainThreadActions.Enqueue, NativeConversationMainThreadPreprocessTimeoutMs,
        _nativeSessionOwner, NativeAdmissions, () => SceneHistoryPromptCapture, CurrentSceneHistoryPromptCapture, _nativeAdmissionOwner.IsPresentationCurrent);
    // Request-owned prompt data and tentative-input identity, not a persistent commit receipt.
    internal sealed class NativeConversationPendingHistory
    {
        internal string HistoryKey;
        internal string PlayerName;
        internal long EventSequence;
        internal List<ConversationMessage> PendingFacts;
        internal List<ConversationMessage> Messages;
    }

    private Task<NativeConversationPendingHistory> PrepareNativeConversationPendingHistoryAsync(
        NativeConversationAdmission admission, NpcDataPacket npc, string npcName, int agentIndex,
        string playerText, bool recordPlayerInput)
    {
        return NativePendingHistoryApplication.PrepareNativeConversationPendingHistoryAsync(admission, npc, npcName, agentIndex, playerText, recordPlayerInput);
    }

    private Task RollbackNativeConversationPendingPlayerHistoryAsync(NativeConversationAdmission admission,
        string historyKey, long eventSequence, string reason)
    {
        return NativePendingHistoryApplication.RollbackNativeConversationPendingPlayerHistoryAsync(admission, historyKey, eventSequence, reason);
    }

    // Only these input-history operations use this bound wait. Existing general-purpose Scene
    // operations retain their separate policy; a started history mutation must not be abandoned.
    private Task<T> RunNativePendingHistoryOnMainThreadAsync<T>(Func<T> operation, string stage)
    {
        return NativePendingHistoryApplication.RunNativePendingHistoryOnMainThreadAsync(operation, stage);
    }

    private static void ObserveNativePendingHistory(string stage, Exception error = null)
    {
        AnimusForge.Refactor.Adapters.NativePendingHistoryApplicationAdapter.ObserveNativePendingHistory(stage, error);
    }
}
