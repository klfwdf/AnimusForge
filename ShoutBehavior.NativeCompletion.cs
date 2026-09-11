using System;
using System.Threading;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class ShoutBehavior
{
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

        // These existing void owners do not acknowledge durable success. Returning from this
        // dispatch is NOT an atomic memory/AFEF receipt; keep public write capabilities closed.
        // An action may legitimately end this conversation. Preserve its original-target history
        // in the same campaign, without recreating an ended/new conversation's transient state.
        if (hero != null)
        {
            if (scope.SceneSessionId >= 0)
                MyBehavior.AppendExternalSceneDialogueHistory(hero, scope.Request.PlayerText, historyReplyText,
                    scope.Request.OpeningFact, scope.SceneSessionId);
            else
                MyBehavior.AppendExternalDialogueHistory(hero, scope.Request.PlayerText, historyReplyText, scope.Request.OpeningFact);
        }
        else if (scope.HasNonHeroMemory)
        {
            if (scope.SceneSessionId >= 0)
                MyBehavior.AppendExternalNonHeroSceneDialogueHistory(scope.NonHeroMemoryId, scope.NonHeroMemoryName,
                    scope.Request.PlayerText, historyReplyText, scope.Request.OpeningFact, scope.SceneSessionId);
            else
                MyBehavior.AppendExternalNonHeroDialogueHistory(scope.NonHeroMemoryId, scope.NonHeroMemoryName,
                    scope.Request.PlayerText, historyReplyText, scope.Request.OpeningFact);
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
        return string.IsNullOrWhiteSpace(visible) ? cleaned.Trim() : visible.Trim();
    }
}
