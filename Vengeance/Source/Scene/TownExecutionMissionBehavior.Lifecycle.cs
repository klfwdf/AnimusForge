using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private static TextObject BuildKeyAction(TextObject action)
    {
        var keyAction = GameTexts.FindText("str_key_action");
        keyAction.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
        keyAction.SetTextVariable("ACTION", action);
        return keyAction;
    }

    private void CancelSceneAndReturn(ExecutionFailureReason reason, TextObject message)
    {
        if (_initializationFailed)
        {
            return;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} is cancelling with reason {reason}: {message.ToString()}");

        EndCeremonySpeech();
        _initializationFailed = true;
        if (State != ExecutionSessionState.Cancelled &&
            _stateMachine.CanTransitionTo(ExecutionSessionState.Cancelled))
        {
            _stateMachine.TransitionTo(ExecutionSessionState.Cancelled);
        }

        try
        {
            _service.Cancel(Request, reason, message);
        }
        catch (Exception exception)
        {
            RexLog.Error("Execution-service cancellation failed; local cleanup continues.", exception);
        }
        finally
        {
            // Make mission exit inevitable before optional cleanup and UI work.
            // Another module throwing from one of those APIs must not strand the
            // player in a partially initialized town-center mission.
            _endMissionNextTick = true;
            try
            {
                RestorePlayerExecutionState();
            }
            catch (Exception exception)
            {
                RexLog.Error("Unexpected player execution-state cleanup failure during cancellation.", exception);
            }

            try
            {
                CleanupRuntimeObjects(removeAgents: true, removeEntities: true);
            }
            catch (Exception exception)
            {
                RexLog.Error("Unexpected runtime-object cleanup failure during cancellation.", exception);
            }

            try
            {
                ExecutionSessionCoordinator.Release(Request.SessionId);
            }
            catch (Exception exception)
            {
                RexLog.Error("Execution-session release failed during cancellation.", exception);
            }

            try
            {
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not display the scene-cancellation message.", exception);
            }
        }
    }

    private bool CleanupRuntimeObjects(bool removeAgents, bool removeEntities)
    {
        var completed = TryCleanupExecutionStrategy();
        completed &= RestoreStoningVictimProtection();
        completed &= RestoreCrossbowVictimRestraint();
        completed &= TryRestoreCrossbowDeathEnvironment();
        RestoreDetachedHeadSourceIfExecutionWasNotCommitted();
        if (removeAgents)
        {
            var agentsRemoved = true;
            foreach (var agent in _spawnedAgents.Where(agent => agent is not null && agent.IsActive()))
            {
                try
                {
                    agent.FadeOut(hideInstantly: true, hideMount: true);
                }
                catch (Exception exception)
                {
                    completed = false;
                    agentsRemoved = false;
                    RexLog.Error($"Could not remove temporary agent {agent.Index}.", exception);
                }
            }
            if (agentsRemoved) completed &= _sceneLocationCharacters.Clear();
        }

        if (removeEntities)
        {
            for (var index = _spawnedEntities.Count - 1; index >= 0; index--)
            {
                completed &= RemoveSpawnedEntity(_spawnedEntities[index]);
            }
        }

        return completed;
    }

    private bool TryCleanupExecutionStrategy()
    {
        if (_strategyCleanupComplete)
        {
            return true;
        }

        try
        {
            _strategy.Cleanup();
            _strategyCleanupComplete = true;
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"The {CurrentMethod.StringId} execution strategy escaped its cleanup boundary; cleanup will retry.",
                exception);
            return false;
        }
    }

    protected override void OnEndMission()
    {
        EndCeremonySpeech();
        TryStageObservedProjectileVictimDeath("mission end", updateScene: false);
        if (CommitCampaignExecutionOnMissionExit())
        {
            FinalizeSession();
        }
        else
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not complete its first campaign commit call; " +
                "the active request is retained for one retry during behavior removal.");
        }

        base.OnEndMission();
    }

    public override void OnRemoveBehavior()
    {
        EndCeremonySpeech();
        if (_campaignCommitPending && Mission.MissionEnded)
        {
            if (!CommitCampaignExecutionOnMissionExit())
            {
                RexLog.Error(
                    $"Session {Request.SessionId} could not complete its final campaign commit retry " +
                    "during mission teardown.");
            }
        }
        else if (_campaignCommitPending)
        {
            RexLog.Error(
                $"DEATH_INVARIANT_FAILED session={Request.SessionId}: the execution controller was " +
                "removed while the mission was still running. Campaign death will not be committed early.");
        }

        FinalizeSession();
        base.OnRemoveBehavior();
    }

    private bool CommitCampaignExecutionOnMissionExit()
    {
        if (!_campaignCommitPending)
        {
            return true;
        }

        if (_campaignCommitAttempted)
        {
            RexLog.Error(
                $"Session {Request.SessionId} retained a pending campaign commit after a completed " +
                "commit call; refusing a duplicate settlement.");
            return false;
        }

        RexLog.Info(
            $"Session {Request.SessionId} is leaving the mission; committing the pending campaign execution now.");
        try
        {
            _outcome = _service.Commit(Request, _actor);
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "The pending campaign execution escaped its service boundary during mission exit.",
                exception);
            return false;
        }

        _campaignCommitAttempted = true;
        _campaignCommitPending = false;
        if (_outcome.Success && _outcome.DeathCommitted)
        {
            CommitPendingImpaledCorpseDisplay();
            _deathLogic.MarkCampaignExecutionCommitted();
            RexLog.Info(
                $"Session {Request.SessionId} committed campaign death, costs and consequences at mission exit.");
            return true;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} could not commit its pending campaign execution at mission exit: " +
            $"{_outcome.FailureReason}.");
        try
        {
            InformationManager.DisplayMessage(new InformationMessage(_outcome.Message.ToString()));
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not display the mission-exit campaign-commit failure.", exception);
        }

        return true;
    }

    private void FinalizeSession()
    {
        VengeanceIntegration.ClearProtectedVictim(_victimAgent);
        _pendingImpaledCorpseDisplay = null;
        if (_cleanupComplete)
        {
            return;
        }

        var completed = true;
        try
        {
            _customSiteBuilder?.Dispose();
        }
        catch (Exception exception)
        {
            completed = false;
            RexLog.Error("Custom execution-site builder cleanup failed.", exception);
        }

        if (_outcome?.DeathCommitted != true)
        {
            try
            {
                var cancellationReason = _outcome?.FailureReason ?? ExecutionFailureReason.SessionNotActive;
                var cancellationMessage = _outcome?.Message ??
                    (_missionDeathApplied
                        ? new TextObject(
                            "{=REX_Error_Unexpected}The execution failed because of an unexpected game or module error.")
                        : new TextObject(
                            "{=REX_Cancelled_Left_Mission}The ceremony ended before the lethal moment. Nothing was spent and the prisoner lives."));
                _service.Cancel(
                    Request,
                    cancellationReason,
                    cancellationMessage);
            }
            catch (Exception exception)
            {
                completed = false;
                RexLog.Error("Execution-service cancellation failed during final mission cleanup.", exception);
            }
        }

        try
        {
            RestorePlayerExecutionState();
            completed &= _playerExecutionStateRestored;
        }
        catch (Exception exception)
        {
            completed = false;
            RexLog.Error("Player execution-state restoration escaped during final mission cleanup.", exception);
        }

        if (_focusCallbackRegistered)
        {
            try
            {
                Mission.FocusableObjectInformationProvider.RemoveInfoCallback(GetExecutionFocusText);
                _focusCallbackRegistered = false;
            }
            catch (Exception exception)
            {
                completed = false;
                RexLog.Error("Could not unregister the execution focus callback.", exception);
            }
        }

        try
        {
            // If the per-Agent exclusion could not be applied, keep the
            // mission-wide guard in place until the mission is actually gone;
            // otherwise the completed victim would become an F target again.
            if (_victimConversationBlockApplied || !_campaignCommitAttempted)
            {
                _conversationLogic?.DisableStartConversation(false);
            }
        }
        catch (Exception exception)
        {
            completed = false;
            RexLog.Error("Could not restore ordinary town conversations during final cleanup.", exception);
        }

        try
        {
            completed &= CleanupRuntimeObjects(removeAgents: true, removeEntities: true);
        }
        catch (Exception exception)
        {
            completed = false;
            RexLog.Error("Runtime execution objects escaped their final cleanup boundary.", exception);
        }

        try
        {
            ExecutionSessionCoordinator.Release(Request.SessionId);
        }
        catch (Exception exception)
        {
            completed = false;
            RexLog.Error("Execution-session release failed during final cleanup.", exception);
        }

        _cleanupComplete = completed;
        if (!completed)
        {
            RexLog.Warning("Final execution cleanup was incomplete and will retry from the next mission-end callback.");
        }
    }
}
