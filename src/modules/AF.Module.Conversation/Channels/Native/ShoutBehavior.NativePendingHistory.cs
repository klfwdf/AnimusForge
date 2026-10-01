using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // Request-owned prompt data and tentative-input identity, not a persistent commit receipt.
    private sealed class NativeConversationPendingHistory
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
        return RunNativePendingHistoryOnMainThreadAsync<NativeConversationPendingHistory>(() =>
        {
            if (!IsNativeConversationAdmissionCurrent(admission, out _))
                return null;
            string key = BuildNativeConversationHistoryKey(admission.Hero, admission.Character, npcName, agentIndex, npc);
            if (string.IsNullOrWhiteSpace(key))
                return null;
            string playerName = GetPlayerDisplayNameForShout();
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "玩家";
            long sequence = 0;
            if (recordPlayerInput)
            {
                sequence = NextConversationEventSequence();
                AppendNativeConversationSessionHistory(admission.Hero, admission.Character, npcName,
                    playerName, playerText, "player", sequence, targetAgentIndex: agentIndex, npc: npc,
                    capturedHistoryKey: key);
            }
            return new NativeConversationPendingHistory
            {
                HistoryKey = key, PlayerName = playerName, EventSequence = sequence,
                PendingFacts = ConsumePendingCurrentNativeAfefFactMessagesForPrompt(key),
                Messages = BuildNativeConversationSessionHistoryMessages(admission.Hero, admission.Character,
                    npcName, agentIndex, npc: npc, capturedHistoryKey: key)
            };
        }, "prepare");
    }

    private Task RollbackNativeConversationPendingPlayerHistoryAsync(NativeConversationAdmission admission,
        string historyKey, long eventSequence, string reason)
    {
        if (eventSequence <= 0 || string.IsNullOrWhiteSpace(historyKey))
            return Task.CompletedTask;
        return RunNativePendingHistoryOnMainThreadAsync(() =>
        {
            RollbackNativeConversationPendingPlayerHistory(this, admission, historyKey, eventSequence, reason);
            return true;
        }, "rollback");
    }

    // Only these input-history operations use this bound wait. Existing general-purpose Scene
    // operations retain their separate policy; a started history mutation must not be abandoned.
    private Task<T> RunNativePendingHistoryOnMainThreadAsync<T>(Func<T> operation, string stage)
    {
        if (IsBannerlordMainThreadForNativeActions())
        {
            try { return Task.FromResult(operation()); }
            catch (Exception ex) { ObserveNativePendingHistory(stage + "_error", ex); return Task.FromException<T>(ex); }
        }
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        int state = 0; // queued=0, claimed=1, expired/cancelled before claim=2.
        try
        {
            _mainThreadActions.Enqueue(() =>
            {
                if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;
                try
                {
                    ObserveNativePendingHistory(stage + "_started");
                    T result = operation();
                    completion.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                    ObserveNativePendingHistory(stage + "_error", ex);
                }
            });
        }
        catch (Exception ex)
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) == 0) completion.TrySetException(ex);
            ObserveNativePendingHistory(stage + "_queue_error", ex);
            return completion.Task;
        }
        return AwaitCompletion();

        async Task<T> AwaitCompletion()
        {
            using (var timeout = new CancellationTokenSource())
            {
                try
                {
                    Task winner = await Task.WhenAny(completion.Task,
                        Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs, timeout.Token)).ConfigureAwait(false);
                    if (winner != completion.Task && Interlocked.CompareExchange(ref state, 2, 0) == 0)
                    {
                        completion.TrySetException(new TimeoutException("对话历史操作尚未开始，已停止等待；此排队操作不会随后补做。请检查游戏状态后再继续。"));
                        ObserveNativePendingHistory(stage + "_expired_before_start");
                    }
                    return await completion.Task.ConfigureAwait(false);
                }
                finally { timeout.Cancel(); }
            }
        }
    }

    private static void ObserveNativePendingHistory(string stage, Exception error = null)
    {
        try
        {
            string detail = stage + (error == null ? "" : " " + error);
            Logger.Log("NativeHistory", detail);
            FreezeWatchdog.Mark("NativeConversation.history_" + stage, detail, immediate: true);
        }
        catch (Exception)
        {
            // Optional diagnostics cannot alter queue ownership or report a fake completion.
            return;
        }
    }
}
