using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

namespace AnimusForge;

public partial class ShoutBehavior
{
    /// <summary>
    /// Default Native compatibility boundary. It shares the canonical parser,
    /// request identity and terminal action receipt with detached channels,
    /// while the existing Native owner remains solely responsible for live
    /// target checks, Bannerlord effects and WorldMap exit information.
    /// </summary>
    private NativeConversationGameActionResult ApplyNativeConversationGameActionsCore(
        Hero targetHero,
        CharacterObject targetCharacter,
        NpcDataPacket npc,
        List<NpcDataPacket> allNpcData,
        List<SceneSummonPromptTarget> sceneSummonTargets,
        List<SceneGuidePromptTarget> sceneGuideTargets,
        string content,
        string playerText,
        ConversationManager expectedConversationManager,
        int expectedConversationToken,
        DetachedDuelDispatchContext duelDispatchContext = null)
    {
        string raw = content ?? string.Empty;
        var actionCommitter = new LegacyChannelActionCommitter();
        LegacyChannelActionCommitResult prepared = actionCommitter.Prepare(raw);
        if (!prepared.HasActions)
        {
            if (prepared.Execution.Status == InteractionStatus.Succeeded)
            {
                return ApplyNativeConversationGameActionsLegacyCore(
                    targetHero,
                    targetCharacter,
                    npc,
                    allNpcData,
                    sceneSummonTargets,
                    sceneGuideTargets,
                    raw,
                    playerText,
                    expectedConversationManager,
                    expectedConversationToken,
                    duelDispatchContext);
            }
            return RejectedNativeActionPlanResult(raw, prepared.Execution.ErrorCode);
        }

        InteractionEnvelope envelope;
        try
        {
            envelope = LegacyInteractionSnapshotAdapters.CaptureNativeConversation(playerText ?? string.Empty);
        }
        catch (Exception ex)
        {
            Logger.Log("ShoutBehavior", "[NativeConversation] action snapshot rejected error=" + ex.GetType().Name);
            return RejectedNativeActionPlanResult(raw, "native.action_snapshot_invalid");
        }
        if (envelope?.Snapshot?.Identity == null)
        {
            return RejectedNativeActionPlanResult(raw, "native.action_snapshot_missing");
        }

        NativeConversationGameActionResult ownerResult = null;
        var executor = new LegacyChannelActionPlanExecutor(
            envelope.Snapshot.Identity.Channel,
            envelope.Snapshot.Identity.SessionId,
            envelope.Snapshot.Identity.SubjectId,
            (plan, snapshot) =>
            {
                ownerResult = ApplyNativeConversationGameActionsLegacyCore(
                    targetHero,
                    targetCharacter,
                    npc,
                    allNpcData,
                    sceneSummonTargets,
                    sceneGuideTargets,
                    plan.RawPostprocessId,
                    snapshot.PlayerText,
                    expectedConversationManager,
                    expectedConversationToken,
                    duelDispatchContext);
                if (ownerResult == null || ownerResult.ResponseDiscarded)
                {
                    return InteractionStatus.RejectedByValidation;
                }
                LegacyChannelActionCommitResult remaining = actionCommitter.Prepare(ownerResult.Content);
                return remaining.HasActions
                    || remaining.Execution.Status != InteractionStatus.Succeeded
                    ? InteractionStatus.NonRetryableFailure
                    : InteractionStatus.Executed;
            });
        LegacyChannelActionCommitResult committed = actionCommitter.Commit(
            prepared.ActionPlan,
            envelope.Snapshot,
            executor);
        if (ownerResult != null)
        {
            return ownerResult;
        }

        Logger.Log("ShoutBehavior", "[NativeConversation] action plan stopped status="
            + committed.Execution.Status + " effect=" + committed.Execution.EffectState
            + " error=" + committed.Execution.ErrorCode);
        return RejectedNativeActionPlanResult(raw, committed.Execution.ErrorCode);
    }

    private static NativeConversationGameActionResult RejectedNativeActionPlanResult(
        string raw,
        string errorCode)
    {
        string visible = LegacyActionTagParser.RemoveProtocolTags(raw ?? string.Empty, _ => true).Trim();
        Logger.Log("ShoutBehavior", "[NativeConversation] action plan rejected without retry error="
            + (errorCode ?? "action_not_executed"));
        return new NativeConversationGameActionResult
        {
            Content = visible,
            WorldMapResult = new WorldMapPartyCommandBehavior.WorldMapOrderApplyResult()
        };
    }
}
