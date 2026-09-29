using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using SandBox.Conversation;
using TaleWorlds.CampaignSystem;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
    {
        base.OnAgentInteraction(userAgent, agent, agentBoneIndex);
        if (_strategy is IMultiStageExecutionStrategy &&
            State != ExecutionSessionState.WaitingForPlayer)
        {
            // Subsequent stages are apparatus control/strike points only. The
            // prisoner and executioner must never become alternate F shortcuts.
            return;
        }
        if (!IsBoundPlayer(userAgent) || !CanPlayerUseExecutionPoint)
        {
            return;
        }

        if (agent == _executionerAgent)
        {
            try
            {
                ConversationMission.StartConversationWithAgent(agent);
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not start the executioner conversation.", exception);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject(
                        "{=REX_Error_Conversation_Fallback}The executioner cannot speak right now. Use the marked execution point to carry out the sentence yourself.").ToString()));
            }

            return;
        }

        if (agent == _victimAgent)
        {
            TryBeginExecution(ExecutionActor.Player);
        }
    }

    public override bool IsThereAgentAction(Agent userAgent, Agent agent)
    {
        if (_strategy is IMultiStageExecutionStrategy &&
            State != ExecutionSessionState.WaitingForPlayer)
        {
            return false;
        }
        if (!IsBoundPlayer(userAgent) || !CanPlayerUseExecutionPoint)
        {
            return false;
        }

        return agent == _executionerAgent || agent == _victimAgent;
    }

    public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
    {
        TryStageObservedProjectileVictimDeath("mission-exit request");
        if (State is ExecutionSessionState.CeremonyPreparation or ExecutionSessionState.Execution ||
            (State == ExecutionSessionState.CrowdReaction &&
             !(_lethalAttempted && _strategy is IMultiStageExecutionStrategy)))
        {
            canPlayerLeave = false;
            return null!;
        }

        canPlayerLeave = true;
        return null!;
    }

    public bool IsConversationWithExecutioner()
    {
        if (!CanPlayerUseExecutionPoint || _executionerAgent is null || BannerlordCampaign.Current is null)
        {
            return false;
        }

        return BannerlordCampaign.Current.ConversationManager.OneToOneConversationAgent == _executionerAgent;
    }

    public bool IsCrossbowConversationWithExecutioner() =>
        UsesCrossbowExecution() && IsConversationWithExecutioner();

    public bool TryBeginExecution(ExecutionActor actor)
    {
        if (actor is not ExecutionActor.Executioner and not ExecutionActor.Player ||
            !CanPlayerUseExecutionPoint ||
            _victimAgent is null ||
            _executionerAgent is null)
        {
            return false;
        }

        if (actor == ExecutionActor.Player)
        {
            var player = _playerAgent;
            if (player is null || !player.IsActive())
            {
                return false;
            }

            var distance = player.Position.AsVec2.Distance(_victimAgent.Position.AsVec2);
            GetPlayerExecutionTriggerRange(out var minimumDistance, out var maximumDistance);
            if (distance < minimumDistance || distance > maximumDistance)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    RequiresExecutionTorch()
                        ? new TextObject(
                            "{=REX_Error_Player_Ignition_Distance}Move beside the outer edge of the kindling pile, then press the execution key to light it.").ToString()
                        : new TextObject(
                            "{=REX_Error_Player_Execution_Distance}Move within the wider execution area around the condemned before carrying out the sentence. You will take the executioner's action mark.").ToString()));
                RexLog.Warning(
                    $"Player execution trigger rejected at {distance:0.00}m; allowed range is " +
                    $"{minimumDistance:0.00}-{maximumDistance:0.00}m for method '{CurrentMethod.StringId}'.");
                return false;
            }

            if (RequiresExecutionTorch() || UsesStoningExecution() ||
                _strategy is IMultiStageExecutionStrategy)
            {
                _playerExecutionActorPosition = player.Position;
                RexLog.Info(
                    $"Session {Request.SessionId} captured the player's current " +
                    $"{(UsesStoningExecution() ? "stoning first-throw" : "ground-pyre ignition")} root " +
                    $"at ({_playerExecutionActorPosition.x:0.000}, {_playerExecutionActorPosition.y:0.000}, " +
                    $"{_playerExecutionActorPosition.z:0.000}); the player will not be teleported to the NPC action mark.");
            }
        }

        _actor = actor;
        if (UsesCrossbowExecution() && !TryPrepareCrossbowVictimForCeremonialSuspension())
        {
            _actor = ExecutionActor.Undecided;
            RexLog.Error(
                $"Session {Request.SessionId} could not protect the original crossbow prisoner for the " +
                "fixed crucifix death presentation. The scene remains open and the execution can be retried.");
            return false;
        }

        if (UsesPlayerGroundIgnitionFromCurrentPosition() &&
            (_playerAgent is null || !CapturePlayerExecutionFacingLock(_playerAgent)))
        {
            _actor = ExecutionActor.Undecided;
            return false;
        }

        SetCeremonyAlliesFormationControl(enabled: false, "execution start");
        // Opening lines stop here. Lines already received for the execution
        // itself continue on the same agents.
        EndCeremonySpeech(keepLaterPhases: true);
        ExecutionAddressLlmBridge.Play(Request.SessionId, ExecutionSpeechPhase.During);
        _executionerStandbyRestored = false;
        _usePoint?.SetIsDeactivatedSynched(true);
        if (actor == ExecutionActor.Player && UsesStoningExecution())
        {
            // The stone-throwing executioner already spawned in the authored
            // blue-box position. Player interaction must not make the NPC
            // retreat, side-step, or re-resolve a second position.
            _executionerRetreatPosition = _executionerStandbyPosition;
            _executionerRetreatDirection = _executionerStandbyDirection;
            _executionerPosition = _executionerStandbyPosition;
            _executionerRetreatStarted = false;
            _executionerRetreatCompleted = false;
            RexLog.Info(
                $"Session {Request.SessionId} retained the stoning executioner at the entry blue-box spawn mark for player execution.");
        }
        else if (actor == ExecutionActor.Player &&
                 _strategy is not IMultiStageExecutionStrategy &&
                 !TryStartExecutionerRetreat())
        {
            // The NPC is already standing on a verified ceremony mark. A retreat
            // read-back failure is diagnostic, not a reason to throw the player
            // out of the whole scene while testing unrelated runtime systems.
            _executionerRetreatPosition = _executionerAgent.Position;
            _executionerRetreatDirection = GetDirectionToVictim(_executionerRetreatPosition);
            _executionerPosition = _executionerRetreatPosition;
            _executionerRetreatStarted = true;
            _executionerRetreatCompleted = true;
            _executionerStandbyRestored = false;
            PoseAgentAt(
                _executionerAgent,
                _executionerRetreatPosition,
                _executionerRetreatDirection);
            RexLog.Error(
                $"Session {Request.SessionId} could not validate a separate executioner retreat; " +
                "the NPC remains at the current ceremony mark and player execution continues for diagnosis.");
        }

        _stateMachine.TransitionTo(ExecutionSessionState.CeremonyPreparation);
        _stateElapsed = 0f;
        RexLog.Info($"Session {Request.SessionId} triggered by {actor}.");
        return true;
    }

    internal bool TryUseExecutionPoint(ExecutionActor actor)
    {
        if (State is ExecutionSessionState.Execution or ExecutionSessionState.CrowdReaction &&
            actor == ExecutionActor.Player &&
            _strategy is IMultiStageExecutionStrategy multiStage)
        {
            return multiStage.TryAdvancePlayerStage();
        }

        return TryBeginExecution(actor);
    }

    private void GetPlayerExecutionTriggerRange(out float minimum, out float maximum)
    {
        if (RequiresExecutionTorch())
        {
            minimum = BurningMinimumPlayerIgnitionDistance;
            maximum = BurningMaximumPlayerIgnitionDistance;
            return;
        }

        if (UsesCrossbowExecution())
        {
            minimum = CrossbowMinimumPlayerFiringDistance;
            maximum = CrossbowMaximumPlayerFiringDistance;
            return;
        }

        if (UsesStoningExecution())
        {
            minimum = MinimumPlayerExecutionDistance;
            maximum = StoningMaximumPlayerThrowDistance;
            return;
        }

        minimum = MinimumPlayerExecutionDistance;
        maximum = MaximumPlayerExecutionDistance;
    }

}
