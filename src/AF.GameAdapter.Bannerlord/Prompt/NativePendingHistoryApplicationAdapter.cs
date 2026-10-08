using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using NativeConversationAdmission = AnimusForge.ShoutBehavior.NativeConversationAdmission;
using NativeConversationPendingHistory = AnimusForge.ShoutBehavior.NativeConversationPendingHistory;
using System.Threading;
using System.Threading.Tasks;
namespace AnimusForge.Refactor.Adapters;
internal sealed class NativePendingHistoryApplicationAdapter
{
    private readonly Func<bool> _isMainThread;
    private readonly Action<Action> _enqueue;
    private readonly int _timeoutMs;
    private readonly NativeConversationSessionOwner _sessions;
    private readonly NativeAdmissionApplicationAdapter _admissions;
    private readonly Func<SceneHistoryPromptCaptureAdapter> _sceneCapture, _currentSceneCapture;
    private readonly Func<long,bool> _presentationCurrent;
    internal NativePendingHistoryApplicationAdapter(Func<bool> isMainThread, Action<Action> enqueue, int timeoutMs, NativeConversationSessionOwner sessions = null, NativeAdmissionApplicationAdapter admissions = null, Func<SceneHistoryPromptCaptureAdapter> sceneCapture = null, Func<SceneHistoryPromptCaptureAdapter> currentSceneCapture = null, Func<long,bool> presentationCurrent = null)
    {
        _isMainThread = isMainThread ?? throw new ArgumentNullException(nameof(isMainThread));
        _enqueue = enqueue ?? throw new ArgumentNullException(nameof(enqueue));
        _timeoutMs = timeoutMs;
        _sessions = sessions; _admissions = admissions; _sceneCapture = sceneCapture; _currentSceneCapture = currentSceneCapture; _presentationCurrent = presentationCurrent;
    }
    internal Task<T> RunNativePendingHistoryOnMainThreadAsync<T>(Func<T> operation, string stage)
    {
        if (_isMainThread())
        {
            try { return Task.FromResult(operation()); }
            catch (Exception ex) { ObserveNativePendingHistory(stage + "_error", ex); return Task.FromException<T>(ex); }
        }
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        int state = 0; // queued=0, claimed=1, expired/cancelled before claim=2.
        try
        {
            _enqueue(() =>
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
                        Task.Delay(_timeoutMs, timeout.Token)).ConfigureAwait(false);
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
    internal static void ObserveNativePendingHistory(string stage, Exception error = null)
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
	internal static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistoryEntriesForExternal(NativeConversationSessionOwner nativeSessions, int maxLines = 260)
	{
		try
		{
			if (!SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			int targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			int limit = Math.Max(1, Math.Min(260, maxLines <= 0 ? 260 : maxLines));
			return nativeSessions.GetTail(key, limit);
		}
		catch
		{
			return new List<AnimusForgeDialogueHistoryEntry>();
		}
	}
	internal static void ClearNativeConversationSessionHistoryForExternal(NativeConversationSessionOwner nativeSessions, Hero targetHero, CharacterObject targetCharacter = null, string npcName = null, int dayIndex = -1)
	{
		try
		{
			if (targetHero == null)
			{
				targetHero = targetCharacter?.HeroObject;
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
			}
			int targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			nativeSessions.Clear(key, dayIndex);
		}
		catch
		{
		}
	}
	internal static void SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(NativeConversationSessionOwner nativeSessions,
		Hero targetHero,
		CharacterObject targetCharacter,
		string npcName,
		int dayIndex,
		IEnumerable<AnimusForgeDialogueHistoryEntry> previousEntries,
		IEnumerable<AnimusForgeDialogueHistoryEntry> currentEntries,
		string reason)
	{
		try
		{
			if (targetHero == null)
			{
				targetHero = targetCharacter?.HeroObject;
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
			}
			int targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key) || dayIndex < 0)
			{
				return;
			}

			string result = nativeSessions.SyncDay(key, dayIndex, previousEntries, currentEntries, LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions());
			if (!string.IsNullOrEmpty(result)) Logger.Log("NativeConversationHistory", "manual_daily_memory_sync key=" + key + " day=" + dayIndex + " " + result + " reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationHistory", "[WARN] manual daily memory sync failed: " + ex.Message);
		}
	}
	internal static void RecordNativeConversationNpcLineForExternal(NativeConversationSessionOwner nativeSessions, Func<SceneHistoryPromptCaptureAdapter> currentSceneCapture, Hero targetHero, CharacterObject targetCharacter, string npcName, string text, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
		try
		{
			if (targetHero == null)
			{
				targetHero = targetCharacter?.HeroObject;
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
			string line = ConversationSpeechTextRules.NormalizeNativeConversationHistoryTextForPostprocess(text);
			if (string.IsNullOrWhiteSpace(line))
			{
				return;
			}
			string displayName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? npcName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			if (targetAgentIndex < 0)
			{
				targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			}
			string key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(targetHero, targetCharacter, displayName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			if (nativeSessions.IsLastNpcLine(key, line)) return;
			SceneHistoryPromptCaptureAdapter.AppendNativeConversationSessionHistoryCaptured(nativeSessions, currentSceneCapture, targetHero, targetCharacter, displayName, displayName, line, "npc", targetAgentIndex: targetAgentIndex, npc: npc);
			Logger.Log("NativeConversation", "[ExternalNpcLine] recorded target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? displayName));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] external npc line record failed: " + ex.Message);
		}
	}
	internal static void MarkNativeConversationCurrentDialogRecorded(NativeConversationSessionOwner nativeSessions, Hero targetHero, CharacterObject targetCharacter, string npcName, string currentDialogText, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
		try
		{
			string normalizedLine = ConversationSpeechTextRules.NormalizeNativeConversationHistoryTextForPostprocess(currentDialogText);
			if (string.IsNullOrWhiteSpace(normalizedLine))
			{
				return;
			}
			if (targetAgentIndex < 0)
			{
				targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			}
			string key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			nativeSessions.MarkDialog(key, normalizedLine);
		}
		catch
		{
		}
	}
    internal Task<NativeConversationPendingHistory> PrepareNativeConversationPendingHistoryAsync(
        NativeConversationAdmission admission, NpcDataPacket npc, string npcName, int agentIndex,
        string playerText, bool recordPlayerInput)
    {
        return RunNativePendingHistoryOnMainThreadAsync<NativeConversationPendingHistory>(() =>
        {
            if (!_admissions.IsNativeConversationAdmissionCurrent(admission, out _))
                return null;
            string key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(admission.Hero, admission.Character, npcName, agentIndex, npc);
            if (string.IsNullOrWhiteSpace(key))
                return null;
            string playerName = SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout();
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "玩家";
            long sequence = 0;
            if (recordPlayerInput)
            {
                sequence = SceneConversationHistoryOwner.NextEventSequence();
                SceneHistoryPromptCaptureAdapter.AppendNativeConversationSessionHistoryCaptured(_sessions, _currentSceneCapture, admission.Hero, admission.Character, npcName,
                    playerName, playerText, "player", sequence, targetAgentIndex: agentIndex, npc: npc,
                    capturedHistoryKey: key);
            }
            return new NativeConversationPendingHistory
            {
                HistoryKey = key, PlayerName = playerName, EventSequence = sequence,
                PendingFacts = _sceneCapture().ConsumePendingCurrentNativeAfefFactMessagesForPrompt(key),
                Messages = SceneHistoryPromptCaptureAdapter.BuildNativeConversationSessionHistoryMessages(_sessions, admission.Hero, admission.Character,
                    npcName, agentIndex, npc: npc, capturedHistoryKey: key)
            };
        }, "prepare");
    }
    internal Task RollbackNativeConversationPendingPlayerHistoryAsync(NativeConversationAdmission admission,
        string historyKey, long eventSequence, string reason)
    {
        if (eventSequence <= 0 || string.IsNullOrWhiteSpace(historyKey))
            return Task.CompletedTask;
        return RunNativePendingHistoryOnMainThreadAsync(() =>
        {
            RollbackNativeConversationPendingPlayerHistory(admission, historyKey, eventSequence, reason);
            return true;
        }, "rollback");
    }
	internal void RollbackNativeConversationPendingPlayerHistory(NativeConversationAdmission admission, string historyKey, long eventSequence, string reason)
    {
        // Event sequences reset on load. Never recompute the key or use a new CurrentInstance.
        if (eventSequence <= 0 || string.IsNullOrWhiteSpace(historyKey)
            || !_admissions.IsNativeConversationContextStampCurrent(admission)
            || !_presentationCurrent(admission.PresentationRevision))
            return;
        _sessions.RollbackPlayerEvent(historyKey, eventSequence);
        _sceneCapture().RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(eventSequence);
        ObserveNativePendingHistory("rollback_applied_" + (reason ?? "unknown"));
    }
}
