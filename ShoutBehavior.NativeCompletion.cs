using System;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private sealed class NativeConversationHistoryCommitException : InvalidOperationException
    {
        internal NativeConversationHistoryCommitException(MemoryCommitResult result)
            : base("native.memory_commit_unconfirmed: " + (result?.ErrorCode ?? "result_missing"))
        {
            Result = result;
        }
        internal MemoryCommitResult Result { get; }
    }

    // Prepared text only. No public API and no new persistence schema.
    private sealed class NativeConversationCompletionRequest
    {
        internal string PlayerText;
        internal string OpeningFact;
        internal bool TtsAlreadyDispatched;
        internal long PendingPlayerHistorySequence;
    }

    // Captured after admission validation and before actions can change the scene/party.
    private sealed class NativeConversationCompletionScope
    {
        internal NativeConversationAdmission Admission;
        internal NativeConversationCompletionRequest Request;
        internal NpcDataPacket Npc;
        internal string NpcName;
        internal int AgentIndex;
        internal int SceneSessionId;
        internal string NonHeroMemoryId;
        internal string NonHeroMemoryName;
        internal bool HasNonHeroMemory;
    }

    private NativeConversationCompletionScope CaptureNativeConversationCompletionOnMainThread(
        NativeConversationAdmission admission, NpcDataPacket npc, string npcName, int agentIndex,
        NativeConversationCompletionRequest request)
    {
        if (!IsBannerlordMainThreadForNativeActions())
            throw new InvalidOperationException("native.completion_capture_requires_main_thread");
        var scope = new NativeConversationCompletionScope
        {
            Admission = admission, Request = request, Npc = npc, NpcName = npcName,
            AgentIndex = agentIndex, SceneSessionId = TryGetCurrentSceneHistorySessionIdForHistoryPersistence()
        };
        if (admission.Hero == null)
            scope.HasNonHeroMemory = TryResolveWildernessNonHeroMemory(npc, admission.Hero, admission.Character,
                agentIndex, out scope.NonHeroMemoryId, out scope.NonHeroMemoryName);
        return scope;
    }

    private void RollbackDiscardedNativeCompletionOnMainThread(NativeConversationAdmission admission,
        NpcDataPacket npc, string npcName, int agentIndex, long eventSequence)
    {
        // Never resolve an old pending event against another save/scene/conversation. Skipping
        // stale cleanup is safer than deleting an unrelated event from a new history owner.
        if (eventSequence <= 0 || !IsNativeConversationContextStampCurrent(admission)
            || admission.PresentationRevision != Interlocked.Read(ref _nativeConversationPresentationRevision)
            || !TryResolveNativeConversationTarget(out Hero hero, out var character, out _)
            || !ReferenceEquals(hero, admission.Hero) || !ReferenceEquals(character, admission.Character))
            return;
        RollbackNativeConversationPendingPlayerHistory(admission.Hero, admission.Character, npcName,
            agentIndex, npc, eventSequence, "action_dispatch_target_unavailable");
    }

    private static bool IsNativeConversationCompletionCampaignCurrent(NativeConversationCompletionScope scope)
    {
        return scope != null && IsBannerlordMainThreadForNativeActions()
            && SaveRuntimeGuard.IsCurrentGeneration(scope.Admission.Generation)
            && ReferenceEquals(Campaign.Current?.ConversationManager, scope.Admission.ConversationManager);
    }

    // Unlike backend busy, this remains valid after Task completion, but not after a newer request.
    private bool IsNativeConversationCompletionContextCurrent(NativeConversationCompletionScope scope)
    {
        return scope != null && scope.Admission.PresentationRevision == Interlocked.Read(ref _nativeConversationPresentationRevision)
            && IsNativeConversationContextCurrent(scope.Admission, out _);
    }

    private string CompleteNativeConversationReplyOnMainThread(
        NativeConversationCompletionScope scope, NativeConversationGameActionResult result)
    {
        if (!IsNativeConversationCompletionCampaignCurrent(scope))
            return "";
        Hero hero = scope.Admission.Hero;
        var character = scope.Admission.Character;
        string cleaned = result.Content ?? "";
        string visible = SanitizeSceneSpeechText(cleaned);
        bool suppressHistoryWrite = IsNativeConversationNoSpeechPlaceholder(visible);
        string historyReplyText = suppressHistoryWrite ? null : PrepareSceneHistorySpeechText(cleaned);
        if (!suppressHistoryWrite && string.IsNullOrWhiteSpace(historyReplyText))
            historyReplyText = visible;

        // Transient scene NPCs and a genuinely empty payload request no durable-history write.
        // For applicable payloads, inspect the existing owner's acceptance instead of its old void facade.
        bool hasMemoryPayload = !string.IsNullOrWhiteSpace(scope.Request.PlayerText)
            || !string.IsNullOrWhiteSpace(historyReplyText) || !string.IsNullOrWhiteSpace(scope.Request.OpeningFact);
        if ((hero != null || scope.HasNonHeroMemory) && hasMemoryPayload)
        {
            MemoryCommitResult memory = MyBehavior.CommitDialogueHistoryWithScene(
                hero != null ? hero.StringId : scope.NonHeroMemoryId, hero == null,
                hero != null ? scope.NpcName : scope.NonHeroMemoryName, scope.Request.PlayerText,
                historyReplyText, scope.Request.OpeningFact, scope.SceneSessionId);
            if (memory?.HistoryWritten != true)
            {
                // A required action-driven channel exit must not be lost just because history failed.
                QueueNativeConversationCompletionExit(scope, result);
                throw new NativeConversationHistoryCommitException(memory);
            }
        }

        if (!suppressHistoryWrite && IsNativeConversationCompletionContextCurrent(scope))
        {
            RecordNativeConversationNpcLineForExternal(hero, character, GetSceneNpcHistoryNameForPrompt(scope.Npc),
                historyReplyText, scope.AgentIndex, scope.Npc);
            if (IsNativeConversationCompletionContextCurrent(scope))
                MarkNativeConversationCurrentDialogRecorded(hero, character, scope.NpcName, historyReplyText,
                    scope.AgentIndex, scope.Npc);
        }
        if (!suppressHistoryWrite && !scope.Request.TtsAlreadyDispatched && IsNativeConversationCompletionContextCurrent(scope))
            TrySpeakNativeConversationReplyWithTts(hero, character, scope.Npc, scope.AgentIndex, visible);

        QueueNativeConversationCompletionExit(scope, result);
        return string.IsNullOrWhiteSpace(visible) ? cleaned.Trim() : visible.Trim();
    }

    private void QueueNativeConversationCompletionExit(NativeConversationCompletionScope scope, NativeConversationGameActionResult result)
    {
        if (result.WorldMapResult?.NeedsChannelExit == true && IsNativeConversationCompletionContextCurrent(scope))
        {
            // The backend slot can be released before this callback runs. Validate the captured
            // context/revision, not that slot, so an old close cannot shut a new conversation.
            _mainThreadActions.Enqueue(() =>
            {
                if (IsNativeConversationCompletionContextCurrent(scope))
                    CloseNativeConversationForSceneMechanism("worldmap_implicit_party_creation");
            });
        }
    }

}
