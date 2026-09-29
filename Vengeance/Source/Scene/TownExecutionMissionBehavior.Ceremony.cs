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
    private void TickCeremonyPreparation()
    {
        // The marker needs collision only while Bannerlord is resolving the F
        // interaction. Remove it on the following mission tick, after OnUse has
        // returned, so it cannot overlap the player's restored position or act
        // as a lingering air wall during the aftermath.
        if (_strategy is not IMultiStageExecutionStrategy)
        {
            RemoveConsumedUsePointMarker();
        }

        if (Mission.Mode == MissionMode.Conversation)
        {
            _stateElapsed = 0f;
            return;
        }

        if (_actor == ExecutionActor.Player &&
            !UsesStoningExecution() &&
            _strategy is not IMultiStageExecutionStrategy &&
            !TickExecutionerRetreat())
        {
            return;
        }

        if (!_ceremonyPrepared)
        {
            if (!TryPrepareCeremonyPlayback())
            {
                return;
            }
        }

        if (_stateElapsed < CeremonyPreparationSeconds)
        {
            return;
        }

        try
        {
            var actionAgent = _actor == ExecutionActor.Player
                ? _playerAgent
                : _executionerAgent;
            if (actionAgent is null || !actionAgent.IsActive())
            {
                throw new InvalidOperationException(
                    "The acting executioner became unavailable during ceremony preparation.");
            }

            if (!HavePreparedIdleActionsBecomeVisible(actionAgent))
            {
                // Native can drop a channel-0 request for a frame or two,
                // exactly as the guards do. Keep re-driving the idle for a
                // bounded window instead of cancelling a valid execution.
                _ceremonyIdleRebindElapsed += MathF.Max(0f, _stateElapsed - CeremonyPreparationSeconds);
                if (_ceremonyIdleRebindElapsed < CeremonyIdleRebindTimeoutSeconds)
                {
                    _ceremonyIdleRebindAttemptElapsed += CeremonyIdleRebindIntervalSeconds;
                    RebindCeremonyIdleIfDisplaced(actionAgent);
                    RexLog.Warning(
                        $"Session {Request.SessionId} is re-driving the ceremony idle " +
                        $"'{_ceremonyIdleActionName}' " +
                        $"({_ceremonyIdleRebindElapsed:0.00}s of " +
                        $"{CeremonyIdleRebindTimeoutSeconds:0.00}s before giving up).");
                    return;
                }

                // The idle never settled. The strike clip itself is what the
                // player actually sees, so proceed rather than cancel: the
                // execution action is bound and validated separately below.
                RexLog.Warning(
                    $"Session {Request.SessionId} could not keep the ceremony idle " +
                    $"'{_ceremonyIdleActionName}' visibly bound after " +
                    $"{CeremonyIdleRebindTimeoutSeconds:0.00}s; continuing to the strike, " +
                    "which is validated on its own.");
            }

            // Native idle clips can rotate a human agent after the first pose.
            // Reassert the same safe roots immediately before the strike instead
            // of accepting a flipped player or cancelling an otherwise valid
            // personal execution.
            if (_strategy is IMultiStageExecutionStrategy)
            {
                RexLog.Info(
                    $"Session {Request.SessionId} bypassed generic paired-action positioning for " +
                    $"the staged {CurrentMethod.StringId} strategy; its current F point owns each action root.");
            }
            else
            {
                ReassertCeremonyPoseBeforePlayback(actionAgent);
                LogCeremonyVisualSeparation(actionAgent, "prepared idle");
                VerifyPosedCeremonyFormation(actionAgent);
                if (!IsCeremonyPairStillClear(actionAgent))
                {
                    throw new InvalidOperationException(
                        "A prepared ceremony mark became invalid before playback.");
                }
            }

        }
        catch (Exception exception)
        {
            if (RequiresExecutionTorch() || UsesConfirmedCustomSite)
            {
                RexLog.Error(
                    $"The {CurrentMethod.StringId} ceremony formation diagnostic failed before playback; " +
                    "the error was logged and the sequence continues without cancelling or ending the mission.",
                    exception);
            }
            else
            {
                RexLog.Error(
                    "The authored ceremony formation drifted before playback; cancelling before death.",
                    exception);
                CancelSceneAndReturn(
                    ExecutionFailureReason.UnexpectedError,
                    new TextObject(
                        "{=REX_Error_Animation_Playback}The execution animation could not be played safely. Nothing was spent and the prisoner lives."));
                return;
            }
        }

        _stateMachine.TransitionTo(ExecutionSessionState.Execution);
        _stateElapsed = 0f;
        _executionElapsed = 0f;
        if (UsesStoningExecution())
        {
            _stoningCrowdThrowCount = 0;
            _stoningNextCrowdThrowElapsed =
                StoningFirstThrowDelaySeconds + MBRandom.RandomFloat * 0.30f;
            _stoningPendingProjectileThrower = null;
            _stoningPendingProjectileElapsed = 0f;
            _stoningPendingProjectileNumber = 0;
            _stoningPendingReleaseStarted = false;
            _stoningPendingPreservePlayerControl = false;
        }
        StartExecutionAction();
    }

    private bool TryPrepareCeremonyPlayback()
    {
        _ceremonyPrepared = true;
        _playerExecutionStateRestored = false;
        try
        {
            var player = _playerAgent;
            if (_actor == ExecutionActor.Player &&
                player is not null &&
                !PreparePlayerExecutionEquipment(player))
            {
                throw new InvalidOperationException(
                    "The player's execution equipment could not be prepared safely.");
            }

            if (!RequiresExecutionAxe() && _sceneAct is not null)
            {
                _ceremonyIdleActionName = _sceneAct.ExecutionerIdleAction;
                _ceremonyFallbackIdleActionName = UnarmedExecutionerFallbackIdleAction;
            }

            if (_strategy is IMultiStageExecutionStrategy)
            {
                if (!TryBindVictimIdleAction())
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} could not rebind the staged prisoner idle; " +
                        "the strategy timer and fixed presentation will still continue.");
                }

                RexLog.Info(
                    $"Session {Request.SessionId} bypassed the generic paired execution-action marks for " +
                    $"{CurrentMethod.StringId}; the method strategy owns all operator points.");
                return true;
            }

            PoseCeremonyAgents();
            return true;
        }
        catch (Exception exception)
        {
            if (RequiresExecutionTorch() || UsesCrossbowExecution() || UsesStoningExecution() ||
                UsesConfirmedCustomSite)
            {
                RexLog.Error(
                    $"{CurrentMethod.StringId} ceremony playback setup reported an error; " +
                    "continuing in the same mission for diagnosis instead of cancelling.",
                    exception);
                return true;
            }

            RexLog.Error("Ceremony playback setup failed; cancelling before the lethal moment.", exception);
            CancelSceneAndReturn(
                ExecutionFailureReason.UnexpectedError,
                new TextObject(
                    "{=REX_Error_Unexpected}The execution failed because of an unexpected game or module error."));
            return false;
        }
    }

    private bool HasUnifiedBeheadingAction() =>
        RequiresExecutionAxe() &&
        string.Equals(
            _sceneAct?.ExecutionAction,
            TwoHandedBeheadingExecutionAction,
            StringComparison.OrdinalIgnoreCase);

    private bool UsesStagedMethodExecution() => _strategy.UsesStagedExecution;

    private bool RequiresPreparedExecutionerIdle() =>
        HasUnifiedBeheadingAction() ||
        (UsesStagedMethodExecution() && !UsesPlayerStoningFromCurrentPosition());

    private Vec3 GetExecutionActorPosition() =>
        _placement is null
            ? Vec3.Invalid
            : _actor == ExecutionActor.Player
                ? _playerExecutionActorPosition
                : UsesStoningExecution()
                    ? _executionerStandbyPosition
                    : _npcExecutionActorPosition;

    private bool UsesPlayerGroundIgnitionFromCurrentPosition() =>
        _actor == ExecutionActor.Player && RequiresExecutionTorch();

    private bool UsesPlayerStoningFromCurrentPosition() =>
        _actor == ExecutionActor.Player && UsesStoningExecution();

    private bool UsesPlayerExecutionFromCurrentPosition() =>
        UsesPlayerGroundIgnitionFromCurrentPosition() ||
        UsesPlayerStoningFromCurrentPosition() ||
        (_actor == ExecutionActor.Player && _strategy is IMultiStageExecutionStrategy);

    private bool UsesCrossbowExecution() =>
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.CrossbowExecution,
            StringComparison.OrdinalIgnoreCase);

    private bool UsesStoningExecution() =>
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Stoning,
            StringComparison.OrdinalIgnoreCase);

    private bool UsesImpalementExecution() =>
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Impalement,
            StringComparison.OrdinalIgnoreCase);

    private float GetExecutionActorDistance() =>
        UsesPlayerGroundIgnitionFromCurrentPosition()
            ? BurningMinimumPlayerIgnitionDistance
            : GetRequestedExecutionActorDistance(_actor);

    private float GetRequestedExecutionActorDistance(ExecutionActor actor) =>
        RequiresExecutionTorch()
            ? actor == ExecutionActor.Executioner
                ? BurningExecutionActorDistance
                : BurningMinimumPlayerIgnitionDistance
            : UsesCrossbowExecution()
                ? CrossbowExecutionActorDistance
            : actor == ExecutionActor.Player
                ? PlayerExecutionActorDistance
                : NpcExecutionActorDistance;

    private Vec2 GetExecutionActorDirection()
    {
        if (_actor == ExecutionActor.Player && _playerExecutionFacingLockActive)
        {
            return _playerExecutionFacingDirection;
        }

        return GetDirectionToVictim(GetExecutionActorPosition());
    }

    private Vec2 GetFixedDirectionToVictim(Vec3 fromPosition)
    {
        if (_victimPosition.IsValid && fromPosition.IsValid)
        {
            var towardFixedRitualPoint = _victimPosition.AsVec2 - fromPosition.AsVec2;
            if (towardFixedRitualPoint.IsNonZero())
            {
                return towardFixedRitualPoint.Normalized();
            }
        }

        var forward = _placement?.Forward ?? Vec2.Forward;
        return forward.IsNonZero() ? forward.Normalized() : Vec2.Forward;
    }

    private bool CapturePlayerExecutionFacingLock(Agent player)
    {
        if (_actor != ExecutionActor.Player)
        {
            return true;
        }

        try
        {
            if (!_playerLookDirectionStateCaptured)
            {
                _playerOriginalLookDirectionLocked = player.IsLookDirectionLocked;
                _playerOriginalLookDirection = player.LookDirection;
                _playerLookDirectionStateCaptured = true;
            }

            _playerExecutionFacingDirection = GetFixedDirectionToVictim(
                _playerExecutionActorPosition);
            if (!_playerExecutionFacingDirection.IsNonZero())
            {
                RexLog.Error(
                    $"Session {Request.SessionId} could not resolve the fixed player-to-prisoner facing direction.");
                return false;
            }

            _playerExecutionFacingLockActive = true;
            _playerMinimumExecutionFacingDot = 1f;
            _playerExecutionFacingWriteCount = 0;
            _playerExecutionRootRotationFlagChecked = false;
            MaintainPlayerExecutionFacingLock();
            RexLog.Info(
                $"Session {Request.SessionId} captured player look lock={_playerOriginalLookDirectionLocked} " +
                $"and locked channel-0 execution facing to the fixed prisoner ritual point " +
                $"({_playerExecutionFacingDirection.x:0.000}, {_playerExecutionFacingDirection.y:0.000}).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not capture and lock the player's execution facing.", exception);
            return false;
        }
    }

    private void MaintainPlayerExecutionFacingLock()
    {
        if (!_playerExecutionFacingLockActive)
        {
            return;
        }

        var player = _playerAgent;
        if (player is null || !player.IsActive())
        {
            return;
        }

        try
        {
            // Deliberately write on every mission tick with no angular threshold.
            // The direction is fixed at bind time and never follows corpse rotation.
            player.LookDirection = new Vec3(
                _playerExecutionFacingDirection.x,
                _playerExecutionFacingDirection.y,
                0f);
            player.IsLookDirectionLocked = true;
            _playerExecutionFacingWriteCount++;
            var readBack = player.LookDirection.AsVec2;
            var facingDot = readBack.IsNonZero()
                ? Vec2.DotProduct(
                    readBack.Normalized(),
                    _playerExecutionFacingDirection)
                : -1f;
            _playerMinimumExecutionFacingDot = MathF.Min(
                _playerMinimumExecutionFacingDot,
                facingDot);
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not maintain the player's fixed execution facing; " +
                "the mission remains active for diagnosis.",
                exception);
        }
    }

    private bool RestorePlayerExecutionFacingLock()
    {
        if (!_playerLookDirectionStateCaptured)
        {
            _playerExecutionFacingLockActive = false;
            return true;
        }

        var player = _playerAgent;
        try
        {
            if (player is not null && player.IsActive())
            {
                // Restore only lock ownership. Writing the pre-execution look
                // vector here snapped the post-lethal third-person view away
                // from the condemned immediately after the strike.
                player.IsLookDirectionLocked = _playerOriginalLookDirectionLocked;
            }

            RexLog.Info(
                $"Session {Request.SessionId} restored player look lock to " +
                $"{_playerOriginalLookDirectionLocked} after {_playerExecutionFacingWriteCount} fixed writes " +
                $"(minimumFacingDot={_playerMinimumExecutionFacingDot:0.000}).");
            _playerLookDirectionStateCaptured = false;
            _playerExecutionFacingLockActive = false;
            _playerOriginalLookDirectionLocked = false;
            _playerOriginalLookDirection = Vec3.Zero;
            _playerExecutionFacingDirection = Vec2.Forward;
            _playerMinimumExecutionFacingDot = 1f;
            _playerExecutionFacingWriteCount = 0;
            _playerExecutionRootRotationFlagChecked = false;
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "Could not restore the player's original look-direction lock; cleanup will retry.",
                exception);
            return false;
        }
    }

    private Vec2 GetDirectionToVictim(Vec3 fromPosition)
    {
        var liveVictimPosition = _victimAgent is not null && _victimAgent.IsActive()
            ? _victimAgent.Position
            : _victimPosition;
        if (liveVictimPosition.IsValid && fromPosition.IsValid)
        {
            var towardVictim = liveVictimPosition.AsVec2 - fromPosition.AsVec2;
            if (towardVictim.IsNonZero())
            {
                return towardVictim.Normalized();
            }
        }

        var forward = _placement?.Forward ?? Vec2.Forward;
        return forward.IsNonZero() ? forward.Normalized() : Vec2.Forward;
    }

    private void PoseCeremonyAgents()
    {
        if (_placement is null || _victimAgent is null || _executionerAgent is null)
        {
            return;
        }

        var actionAgent = _actor == ExecutionActor.Player
            ? _playerAgent
            : _executionerAgent;
        if (actionAgent is null || !actionAgent.IsActive())
        {
            throw new InvalidOperationException(
                "The ceremony actors were no longer available before the animation started.");
        }

        if (!IsCeremonyPairStillClear(actionAgent))
        {
            throw new InvalidOperationException(
                "The execution marks became obstructed before the animation started.");
        }

        if (!TryBindVictimIdleAction())
        {
            RexLog.Error(
                "The victim preparation idle could not be started; continuing without ending the mission.");
        }

        // Wield before binding the idle. TryToWieldWeaponInSlot drives its own
        // animation on channel 0, so equipping after the idle silently replaced
        // it and the later read-back cancelled the whole ceremony.
        if (RequiresCeremonyWeapon() &&
            !TryEnsureCeremonyWeaponWielded(actionAgent, GetActingRoleLabel("before idle")))
        {
            RexLog.Error(
                "The acting executioner could not retain the ceremony weapon before the idle; " +
                "continuing so the strike fallback can repair or bypass the animation.");
        }

        if (HasUnifiedBeheadingAction())
        {
            if (!TryPlayRequiredAction(
                    actionAgent,
                    _ceremonyIdleActionName,
                    "authored executioner idle",
                    out _executionerIdleAction,
                    forceFullBody: true))
            {
                var failedIdle = _ceremonyIdleActionName;
                _ceremonyIdleActionName = _ceremonyFallbackIdleActionName;
                RexLog.Warning(
                    $"Ceremony idle '{failedIdle}' could not start; using visible fallback " +
                    $"'{_ceremonyIdleActionName}' without ending the mission.");
                if (!TryPlayRequiredAction(
                        actionAgent,
                        _ceremonyIdleActionName,
                        "ceremony idle fallback",
                        out _executionerIdleAction,
                        forceFullBody: true))
                {
                    _executionerIdleAction = ActionIndexCache.act_none;
                    RexLog.Error(
                        $"Ceremony idle fallback '{_ceremonyIdleActionName}' also failed; " +
                    "continuing to the strike without an enforced idle.");
                }
            }
        }
        else if (UsesStagedMethodExecution() &&
                 !UsesPlayerStoningFromCurrentPosition() &&
                 _sceneAct is not null)
        {
            var requestedIdle = _sceneAct.ExecutionerIdleAction;
            if (!TryPlayRequiredAction(
                    actionAgent,
                    requestedIdle,
                    "method executioner idle",
                    out _executionerIdleAction,
                    forceFullBody: true))
            {
                _ceremonyIdleActionName = UnarmedExecutionerFallbackIdleAction;
                if (string.Equals(
                        requestedIdle,
                        UnarmedExecutionerFallbackIdleAction,
                        StringComparison.OrdinalIgnoreCase) ||
                    !TryPlayRequiredAction(
                        actionAgent,
                        UnarmedExecutionerFallbackIdleAction,
                        "method executioner idle fallback",
                        out _executionerIdleAction,
                        forceFullBody: true))
                {
                    _executionerIdleAction = ActionIndexCache.act_none;
                    RexLog.Error(
                        $"Method idle '{requestedIdle}' could not start; the authored method action " +
                        "will still be attempted without ending the mission.");
                }
            }
        }

        ReassertCeremonyPoseBeforePlayback(actionAgent);

        VerifyPosedCeremonyFormation(actionAgent);
    }

    private string GetActingRoleLabel(string phase) =>
        (_actor == ExecutionActor.Player ? "acting player " : "acting executioner ") + phase;

    private bool TryBindVictimIdleAction()
    {
        var sceneAct = _sceneAct;
        if (sceneAct is null)
        {
            return false;
        }

        return TryPlayRequiredAction(
            _victimAgent,
            sceneAct.VictimIdleAction,
            "required original-prisoner victim idle",
            out _victimIdleAction,
            forceFullBody: true);
    }

    private void ReassertCeremonyPoseBeforePlayback(Agent actionAgent)
    {
        if (_placement is null || _victimAgent is null || _executionerAgent is null)
        {
            throw new InvalidOperationException(
                "The ceremony agents were unavailable while reasserting the safe action roots.");
        }

        var executionActorPosition = GetExecutionActorPosition();
        var executionActorDirection = GetExecutionActorDirection();
        if (RequiresExecutionTorch())
        {
            // Burning owns the original prisoner's vertical position through a
            // native StandingPoint/use-object attachment and target-Z. Calling
            // PoseAgentAt here would clear that target frame and let the action
            // integration push the visible body back to the navigation surface.
            var victimDirection = _placement.Forward.IsNonZero()
                ? _placement.Forward.Normalized()
                : Vec2.Forward;
            _victimAgent.LookDirection = new Vec3(victimDirection.x, victimDirection.y, 0f);
        }
        else
        {
            PoseAgentAt(_victimAgent, _victimPosition, _placement.Forward);
        }

        if (UsesPlayerStoningFromCurrentPosition())
        {
            // The player throws from the interaction position under normal
            // MainAgent input. Do not write root, target frame, look direction
            // or look-lock state during preparation.
        }
        else if (UsesPlayerExecutionFromCurrentPosition())
        {
            MaintainPlayerExecutionFacingLock();
        }
        else
        {
            PoseAgentAt(actionAgent, executionActorPosition, executionActorDirection);
        }

        if (_actor == ExecutionActor.Player)
        {
            if (UsesPlayerStoningFromCurrentPosition())
            {
                // The executioner was born at this mark and does not need a
                // later corrective teleport when the player starts throwing.
                // Keep only the existing controller-free standby state.
                _executionerAgent.DisableScriptedMovement();
                _executionerAgent.Controller = AgentControllerType.None;
                RexLog.Info(
                    $"Session {Request.SessionId} kept player {actionAgent.Index} fully free at the " +
                    $"stoning interaction root; executioner {_executionerAgent.Index} remains at the entry blue-box mark.");
            }
            else if (UsesPlayerExecutionFromCurrentPosition())
            {
                PoseAgentAt(_executionerAgent, _executionerPosition, _executionerRetreatDirection);
                RexLog.Info(
                    $"Session {Request.SessionId} kept player {actionAgent.Index} at the original trigger " +
                    $"root while facing the prisoner; executioner {_executionerAgent.Index} remains clear " +
                    "at the standby mark and no player teleport was issued.");
            }
            else
            {
                PoseAgentAt(_executionerAgent, _executionerPosition, _executionerRetreatDirection);
                RexLog.Info(
                    $"Session {Request.SessionId} moved player {actionAgent.Index} to the executioner's " +
                    $"action mark facing the prisoner; executioner {_executionerAgent.Index} remains clear at the standby mark.");
            }
        }

        if (RequiresCeremonyWeapon())
        {
            var actingRole = _actor == ExecutionActor.Player
                ? "acting player after idle"
                : "acting executioner after idle";
            if (!TryEnsureCeremonyWeaponWielded(actionAgent, actingRole) ||
                (_actor == ExecutionActor.Player &&
                 !TryEnsureCeremonyWeaponWielded(
                     _executionerAgent,
                     "standby executioner during player execution")))
            {
                throw new InvalidOperationException(
                    "An execution actor did not retain the method ceremony weapon after the preparation idle.");
            }

            // Wielding above re-drives channel 0. Restore the ceremony idle now
            // so the pre-playback read-back sees the pose that was requested.
            RebindCeremonyIdleIfDisplaced(actionAgent);
        }
    }

    private void RebindCeremonyIdleIfDisplaced(Agent actionAgent)
    {
        if (!RequiresPreparedExecutionerIdle() ||
            _executionerIdleAction == ActionIndexCache.act_none)
        {
            return;
        }

        try
        {
            if (actionAgent.GetCurrentAction(0) == _executionerIdleAction &&
                actionAgent.GetActionChannelWeight(0) > 0.01f)
            {
                return;
            }

            TrySetPreparedAction(
                actionAgent,
                _executionerIdleAction,
                _ceremonyIdleActionName,
                "ceremony idle rebind after wield",
                forceFullBody: true);
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not rebind ceremony idle '{_ceremonyIdleActionName}' after the wield request.",
                exception);
        }
    }

    private void VerifyPosedCeremonyFormation(Agent actionAgent)
    {
        if (_placement is null || _victimAgent is null)
        {
            throw new InvalidOperationException(
                "The posed ceremony actors were unavailable for read-back validation.");
        }

        var actualPairDistance =
            _victimAgent.Position.AsVec2.Distance(actionAgent.Position.AsVec2);
        if (UsesConfirmedCustomSite)
        {
            if (!_customCeremonyClearanceBypassLogged)
            {
                _customCeremonyClearanceBypassLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} accepted custom ceremony formation read-back " +
                    $"at actualDistance={actualPairDistance:0.000}m without applying automatic-layout gates.");
            }

            return;
        }

        if (UsesPlayerGroundIgnitionFromCurrentPosition() &&
            actualPairDistance > BurningMaximumPlayerIgnitionDistance + 0.01f)
        {
            throw new InvalidOperationException(
                "The player moved beyond the outer ground-pyre ignition range before playback.");
        }

        float requiredPairDistance;
        if (!HasRequiredAgentSeparation(
                _victimAgent.Position,
                GetSafeCollisionRadius(_victimAgent),
                actionAgent.Position,
                GetSafeCollisionRadius(actionAgent),
                GetExecutionActorDistance(),
                out requiredPairDistance,
                out actualPairDistance))
        {
            throw new InvalidOperationException(
                "The posed execution pair did not reach the required visual separation.");
        }

        var standbyAgent = _actor == ExecutionActor.Player
            ? _executionerAgent
            : null;
        if (standbyAgent is not null &&
            (!HasRequiredAgentSeparation(
                 _victimAgent.Position,
                 GetSafeCollisionRadius(_victimAgent),
                 standbyAgent.Position,
                 GetSafeCollisionRadius(standbyAgent),
                 minimumDistance: 0f,
                 out _,
                 out _) ||
             !HasRequiredAgentSeparation(
                 actionAgent.Position,
                 GetSafeCollisionRadius(actionAgent),
                 standbyAgent.Position,
                 GetSafeCollisionRadius(standbyAgent),
                 minimumDistance: 0f,
                 out _,
                 out _)))
        {
            throw new InvalidOperationException(
                "The posed executioner standby mark did not remain clear of the player ceremony pair.");
        }

        RexLog.Info(
            $"Session {Request.SessionId} posed ceremony formation: " +
            $"required={requiredPairDistance:0.000}, actual={actualPairDistance:0.000}, actor={_actor}.");
    }

    private void LogCeremonyVisualSeparation(Agent actionAgent, string phase)
    {
        if (_placement is null || _victimAgent is null ||
            !_victimAgent.IsActive() || !actionAgent.IsActive())
        {
            return;
        }

        try
        {
            var victimChest = _victimAgent.GetChestGlobalPosition();
            var actorChest = actionAgent.GetChestGlobalPosition();
            var rootDelta = actionAgent.Position.AsVec2 - _victimAgent.Position.AsVec2;
            var chestDelta = actorChest.AsVec2 - victimChest.AsVec2;
            GetVictimFacingBasis(out var forward, out var right);
            var rootRightOffset = rootDelta.x * right.x + rootDelta.y * right.y;
            var chestRightOffset = chestDelta.x * right.x + chestDelta.y * right.y;
            var rootForwardOffset = rootDelta.x * forward.x + rootDelta.y * forward.y;
            var chestForwardOffset = chestDelta.x * forward.x + chestDelta.y * forward.y;
            RexLog.Info(
                $"Session {Request.SessionId} visual separation at {phase}: " +
                $"rootDistance={rootDelta.Length:0.000}, chestDistance={chestDelta.Length:0.000}, " +
                $"victimViewRootRightOffset={rootRightOffset:0.000}, " +
                $"victimViewChestRightOffset={chestRightOffset:0.000}, " +
                $"victimViewRootForwardOffset={rootForwardOffset:0.000}, " +
                $"victimViewChestForwardOffset={chestForwardOffset:0.000}, " +
                $"actor={_actor}.");
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Could not sample ceremony visual separation at {phase}: {exception.Message}");
        }
    }

    private bool IsCeremonyPairStillClear(Agent actionAgent)
    {
        if (_victimAgent is null)
        {
            return false;
        }

        if (UsesConfirmedCustomSite)
        {
            // A confirmed custom layout is player-authoritative. Runtime root
            // read-back can differ from the authored marker while an idle binds
            // (the observed beheading case changed 0.85m to 0.55m). Keep this as
            // a diagnostic only; never tear down a user-confirmed ceremony here.
            if (!_customCeremonyClearanceBypassLogged)
            {
                _customCeremonyClearanceBypassLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} skipped custom ceremony distance and " +
                    "environment obstruction vetoes; any drift is logged without cancelling.");
            }

            return true;
        }

        var executionActorPosition = GetExecutionActorPosition();
        var actualPairDistance =
            _victimPosition.AsVec2.Distance(executionActorPosition.AsVec2);
        if (UsesPlayerGroundIgnitionFromCurrentPosition() &&
            actualPairDistance > BurningMaximumPlayerIgnitionDistance + 0.01f)
        {
            RexLog.Error(
                $"Player ground-pyre ignition root moved beyond the allowed outer range " +
                $"(maximum={BurningMaximumPlayerIgnitionDistance:0.000}, actual={actualPairDistance:0.000}).");
            return false;
        }

        float requiredPairDistance;
        if (!HasRequiredAgentSeparation(
                _victimPosition,
                GetSafeCollisionRadius(_victimAgent),
                executionActorPosition,
                GetSafeCollisionRadius(actionAgent),
                GetExecutionActorDistance(),
                out requiredPairDistance,
                out actualPairDistance))
        {
            RexLog.Error(
                $"Execution pair clearance became unsafe " +
                $"(required={requiredPairDistance:0.000}, actual={actualPairDistance:0.000}).");
            return false;
        }

        var standbyAgent = _actor == ExecutionActor.Player
            ? _executionerAgent
            : null;
        if (standbyAgent is not null &&
            (!HasRequiredAgentSeparation(
                 _victimPosition,
                 GetSafeCollisionRadius(_victimAgent),
                 _executionerPosition,
                 GetSafeCollisionRadius(standbyAgent),
                 minimumDistance: 0f,
                 out _,
                 out _) ||
              !HasRequiredAgentSeparation(
                  executionActorPosition,
                  GetSafeCollisionRadius(actionAgent),
                  _executionerPosition,
                  GetSafeCollisionRadius(standbyAgent),
                 minimumDistance: 0f,
                 out _,
                 out _)))
        {
            RexLog.Error(
                "Executioner standby clearance became unsafe before the player animation started.");
            return false;
        }

        if (RequiresExecutionTorch())
        {
            if (!_burningCeremonyClearanceBypassLogged)
            {
                _burningCeremonyClearanceBypassLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} retained agent-to-agent separation but skipped the final " +
                    "environment obstruction veto for the ground pyre; the authored stake and cone roots " +
                    "are expected at the ritual point, and any diagnostic failure will not cancel or end the mission.");
            }

            return true;
        }

        return IsCeremonyPositionStillClear(
                   _victimPosition,
                   _victimAgent,
                   actionAgent,
                   standbyAgent) &&
               IsCeremonyPositionStillClear(
                   executionActorPosition,
                   actionAgent,
                   _victimAgent,
                   standbyAgent) &&
               (standbyAgent is null ||
                IsCeremonyPositionStillClear(
                    _executionerPosition,
                    standbyAgent,
                    _victimAgent,
                    actionAgent));
    }

    private static bool HasRequiredAgentSeparation(
        Vec3 firstPosition,
        float firstRadius,
        Vec3 secondPosition,
        float secondRadius,
        float minimumDistance,
        out float requiredDistance,
        out float actualDistance)
    {
        requiredDistance = GetRequiredAgentSeparation(
            firstRadius,
            secondRadius,
            minimumDistance);
        actualDistance = firstPosition.AsVec2.Distance(secondPosition.AsVec2);
        return actualDistance + 0.01f >= requiredDistance;
    }

    private static float GetRequiredAgentSeparation(
        float firstRadius,
        float secondRadius,
        float minimumDistance) =>
        MathF.Max(
            firstRadius + secondRadius + PairedAgentCollisionMargin,
            minimumDistance);

    private bool IsCeremonyPositionStillClear(
        Vec3 position,
        Agent targetAgent,
        Agent pairedAgent,
        Agent? additionalAllowedAgent = null)
    {
        if (!HasCeremonyAgentClearance(position))
        {
            return false;
        }

        foreach (var agent in Mission.Agents)
        {
            if (agent is null || !agent.IsActive() ||
                ReferenceEquals(agent, targetAgent) ||
                ReferenceEquals(agent, pairedAgent) ||
                ReferenceEquals(agent, additionalAllowedAgent))
            {
                continue;
            }

            var requiredDistance = GetSafeCollisionRadius(targetAgent) +
                                   GetSafeCollisionRadius(agent) +
                                   PairedAgentCollisionMargin;
            if (agent.Position.AsVec2.Distance(position.AsVec2) < requiredDistance)
            {
                RexLog.Warning(
                    $"Execution mark was occupied by active agent {agent.Index}; cancelling before death.");
                return false;
            }
        }

        return true;
    }

    private void ReassertExecutionActionRoot(Agent? actionAgent, string phase)
    {
        if (actionAgent is null || !actionAgent.IsActive())
        {
            return;
        }

        if ((UsesPlayerGroundIgnitionFromCurrentPosition() ||
             (_strategy is IMultiStageExecutionStrategy && _actor == ExecutionActor.Player)) &&
            ReferenceEquals(actionAgent, _playerAgent))
        {
            // The player explicitly chose this root by pressing F beside the
            // pyre. Keep only the facing lock; no position or target-frame API
            // may pull MainAgent to the old fixed ceremony mark.
            MaintainPlayerExecutionFacingLock();
            return;
        }

        var expectedPosition = GetExecutionActorPosition();
        if (!expectedPosition.IsValid)
        {
            return;
        }

        try
        {
            var expectedDirection = GetExecutionActorDirection();
            var before = actionAgent.Position;
            var positionError = before.Distance(expectedPosition);
            var actualDirection = actionAgent.LookDirection.AsVec2;
            var facingDot = actualDirection.IsNonZero()
                ? Vec2.DotProduct(actualDirection.Normalized(), expectedDirection)
                : -1f;
            var needsCorrection =
                positionError > ExecutionActionRootCorrectionThreshold ||
                facingDot < 0.995f;

            if (!needsCorrection)
            {
                return;
            }

            var actionBeforeCorrection = actionAgent.GetCurrentAction(0);
            // Teleporting the Agent root does not restart channel 0. Only issue
            // the teleport when the authored clip actually leaves the 1 cm /
            // heading tolerance, avoiding a redundant transform write every tick.
            PoseAgentAt(actionAgent, expectedPosition, expectedDirection);
            var remainingError = actionAgent.Position.Distance(expectedPosition);
            var actionAfterCorrection = actionAgent.GetCurrentAction(0);
            if (actionAfterCorrection != actionBeforeCorrection)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} channel 0 changed while correcting execution root at {phase}: " +
                    $"before={actionBeforeCorrection.Index}, after={actionAfterCorrection.Index}. The action continues.");
            }

            if (!_executionActionRootCorrectionLogged)
            {
                _executionActionRootCorrectionLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} corrected authored execution root motion at {phase}: " +
                    $"actor={actionAgent.Index}, before=({before.x:0.000}, {before.y:0.000}, {before.z:0.000}), " +
                    $"expected=({expectedPosition.x:0.000}, {expectedPosition.y:0.000}, {expectedPosition.z:0.000}), " +
                    $"positionError={positionError:0.000}m, facingDot={facingDot:0.000}, " +
                    $"remainingError={remainingError:0.000}m. Channel 0 was not restarted.");
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not re-pin the execution actor root at {phase}; the action continues for diagnosis.",
                exception);
        }
    }

    private static void PoseAgentAt(Agent agent, Vec3 position, Vec2 direction)
    {
        var safeDirection = direction.IsNonZero()
            ? direction.Normalized()
            : Vec2.Forward;
        agent.ClearTargetFrame();
        agent.TeleportToPosition(position);
        agent.LookDirection = new Vec3(safeDirection.x, safeDirection.y, 0f);
        agent.ClearTargetFrame();
    }

    private bool UsesAuthoredExecutionRootCompensation(string actionName) =>
        RequiresExecutionAxe() &&
        string.Equals(
            actionName,
            TwoHandedBeheadingExecutionAction,
            StringComparison.OrdinalIgnoreCase);

    private void ApplyAuthoredExecutionRootBindingCompensation(
        Agent actionAgent,
        string actionName)
    {
        if (!UsesAuthoredExecutionRootCompensation(actionName))
        {
            return;
        }

        var verifiedPosition = GetExecutionActorPosition();
        if (!verifiedPosition.IsValid)
        {
            return;
        }

        GetVictimFacingBasis(out _, out var victimRight);
        var bindingPosition = verifiedPosition +
                              new Vec3(
                                  victimRight.x * AuthoredExecutionRootVictimRightCompensation,
                                  victimRight.y * AuthoredExecutionRootVictimRightCompensation,
                                  0f);
        PoseAgentAt(actionAgent, bindingPosition, GetExecutionActorDirection());
        RexLog.Info(
            $"Session {Request.SessionId} pre-compensated authored execution root before channel-0 bind: " +
            $"actor={actionAgent.Index}, victimViewRight=" +
            $"{AuthoredExecutionRootVictimRightCompensation:+0.000;-0.000;0.000}m, " +
            $"verified=({verifiedPosition.x:0.000}, {verifiedPosition.y:0.000}, {verifiedPosition.z:0.000}), " +
            $"binding=({bindingPosition.x:0.000}, {bindingPosition.y:0.000}, {bindingPosition.z:0.000}).");
    }

    private bool HavePreparedIdleActionsBecomeVisible(Agent actionAgent)
    {
        if (_victimAgent is null ||
            _victimIdleAction == ActionIndexCache.act_none)
        {
            return false;
        }

        try
        {
            var victimVisible =
                _victimAgent.GetCurrentAction(0) == _victimIdleAction &&
                _victimAgent.GetActionChannelWeight(0) > 0.01f;
            var executionerVisible =
                !RequiresPreparedExecutionerIdle() ||
                (_executionerIdleAction != ActionIndexCache.act_none &&
                 actionAgent.GetCurrentAction(0) == _executionerIdleAction &&
                 actionAgent.GetActionChannelWeight(0) > 0.01f);
            if (victimVisible && executionerVisible)
            {
                return true;
            }

            RexLog.Warning(
                $"Prepared idle actions were not visibly bound after ceremony setup " +
                $"(victim={victimVisible}, executioner={executionerVisible}).");
            return false;
        }
        catch (Exception exception)
        {
            RexLog.Error("Prepared idle action read-back failed.", exception);
            return false;
        }
    }

    private bool RequiresExecutionAxe() =>
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Beheading,
            StringComparison.OrdinalIgnoreCase);

    private bool RequiresExecutionTorch() =>
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Burning,
            StringComparison.OrdinalIgnoreCase);

    private string? GetCeremonyWeaponItemId()
    {
        if (RequiresExecutionAxe())
        {
            return ExecutionAxeItemId;
        }

        if (RequiresExecutionTorch())
        {
            return ExecutionTorchItemId;
        }

        if (string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.BreakingWheel,
                StringComparison.OrdinalIgnoreCase))
        {
            return BreakingWheelHammerItemId;
        }

        if (string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Stoning,
                StringComparison.OrdinalIgnoreCase))
        {
            return StoningStoneItemId;
        }

        if (UsesCrossbowExecution())
        {
            return CrossbowExecutionItemId;
        }

        return null;
    }

    private bool RequiresCeremonyWeapon() =>
        !string.IsNullOrWhiteSpace(GetCeremonyWeaponItemId());

    private bool UsesGroundExecutionSite() =>
        RequiresExecutionTorch() ||
        UsesCrossbowExecution() ||
        UsesImpalementExecution() ||
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Stoning,
            StringComparison.OrdinalIgnoreCase);

    private bool PreparePlayerExecutionEquipment(Agent player)
    {
        try
        {
            _playerExecutionStateRestored = false;
            if (!_playerWieldStateCaptured)
            {
                _playerPrimaryWieldedBeforeCeremony = player.GetPrimaryWieldedItemIndex();
                _playerOffhandWieldedBeforeCeremony = player.GetOffhandWieldedItemIndex();
                for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                     slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                     slotIndex++)
                {
                    _playerWeaponsBeforeCeremony[slotIndex] =
                        player.Equipment[(EquipmentIndex)slotIndex];
                }

                _playerPrimaryWasTwoHanded =
                    IsRestorablePlayerWeaponSlot(_playerPrimaryWieldedBeforeCeremony) &&
                    !player.Equipment[_playerPrimaryWieldedBeforeCeremony].IsEmpty &&
                    player.Equipment[_playerPrimaryWieldedBeforeCeremony]
                        .CurrentUsageItem?.IsTwoHanded == true &&
                    _playerOffhandWieldedBeforeCeremony == EquipmentIndex.None;
                _playerWieldStateCaptured = true;
            }

            player.TryToSheathWeaponInHand(
                Agent.HandIndex.MainHand,
                Agent.WeaponWieldActionType.Instant);
            player.TryToSheathWeaponInHand(
                Agent.HandIndex.OffHand,
                Agent.WeaponWieldActionType.Instant);

            if (!RequiresCeremonyWeapon())
            {
                return true;
            }

            // Publish the mutation before clearing any slot so cancellation can
            // restore all five stored weapon slots even after a partial failure.
            _playerWeaponSlotsModifiedForCeremony = true;
            if (RequiresExecutionAxe())
            {
                ApplyCeremonyWeaponActions();
            }

            return TryEquipCeremonyWeapon(player, "player");
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not prepare the player's execution equipment.", exception);
            return false;
        }
    }

    private void ApplyCeremonyWeaponActions()
    {
        _ceremonyExecutionActionName = TwoHandedBeheadingExecutionAction;
        _ceremonyIdleActionName = ExecutionerEntryDefaultIdleAction;
        _ceremonyFallbackExecutionActionName = TwoHandedBeheadingFallbackAction;
        _ceremonyFallbackIdleActionName = TwoHandedCeremonyFallbackIdleAction;
    }

    // NPC and player use the method's ceremony item in Weapon0.
    private bool TryEnsureCeremonyWeaponWielded(Agent agent, string role)
    {
        if (RequiresExecutionTorch())
        {
            return TryEnsureExecutionTorchWielded(agent, role);
        }

        if (RequiresExecutionAxe())
        {
            return TryEnsureExecutionAxeWielded(agent, role);
        }

        return TryEnsureAdditionalCeremonyWeaponWielded(agent, role);
    }

    private bool IsCeremonyWeaponStillWielded(Agent agent, string role)
    {
        if (RequiresExecutionTorch())
        {
            return IsExecutionTorchStillWielded(agent, role);
        }

        if (RequiresExecutionAxe())
        {
            return IsExecutionAxeStillWielded(agent, role);
        }

        return IsAdditionalCeremonyWeaponStillWielded(agent, role);
    }

    private bool TryEquipCeremonyWeapon(Agent agent, string role)
    {
        if (RequiresExecutionAxe())
        {
            return TryEquipExecutionAxe(agent, role);
        }

        if (RequiresExecutionTorch())
        {
            return TryEquipExecutionTorch(agent, role);
        }

        return TryEquipAdditionalCeremonyWeapon(agent, role);
    }

    private bool TryEquipExecutionAxe(Agent agent, string role)
    {
        try
        {
            var executionAxe = Game.Current?.ObjectManager.GetObject<ItemObject>(ExecutionAxeItemId);
            if (executionAxe is null)
            {
                RexLog.Error($"Native item '{ExecutionAxeItemId}' is unavailable.");
                return false;
            }

            if (!IsRealPrimaryWeaponSlot(EquipmentIndex.Weapon0) ||
                !TryClearAllWeaponSlots(agent, role))
            {
                return false;
            }

            var weapon = new MissionWeapon(executionAxe, null, agent.Origin?.Banner);
            agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref weapon);
            if (!TryEnsureExecutionAxeWielded(agent, role))
            {
                RexLog.Error(
                    $"The native execution axe was added for the {role}, but Bannerlord did not retain it in hand.");
                return false;
            }

            RexLog.Info(
                $"Session {Request.SessionId} equipped and confirmed the native execution axe for the {role}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not equip the native execution axe.", exception);
            return false;
        }
    }

    private bool TryEquipExecutionTorch(Agent agent, string role)
    {
        try
        {
            var torch = Game.Current?.ObjectManager.GetObject<ItemObject>(ExecutionTorchItemId);
            if (torch is null)
            {
                RexLog.Error($"Native item '{ExecutionTorchItemId}' is unavailable.");
                return false;
            }

            if (!IsRealPrimaryWeaponSlot(EquipmentIndex.Weapon0) ||
                !TryClearAllWeaponSlots(agent, role))
            {
                return false;
            }

            var weapon = new MissionWeapon(torch, null, agent.Origin?.Banner);
            agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref weapon);
            if (!TryEnsureExecutionTorchWielded(agent, role))
            {
                RexLog.Error(
                    $"The native torch was added for the {role}, but Bannerlord did not retain it in hand.");
                return false;
            }

            RexLog.Info(
                $"Session {Request.SessionId} equipped and confirmed the native torch for the {role}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not equip the native execution torch.", exception);
            return false;
        }
    }

    private bool TryEquipAdditionalCeremonyWeapon(Agent agent, string role)
    {
        var itemId = GetCeremonyWeaponItemId();
        if (string.IsNullOrWhiteSpace(itemId))
        {
            RexLog.Error(
                $"Method '{CurrentMethod.StringId}' requested an additional ceremony weapon without an item ID.");
            return false;
        }

        try
        {
            var item = Game.Current?.ObjectManager.GetObject<ItemObject>(itemId);
            if (item is null)
            {
                RexLog.Error($"Native ceremony item '{itemId}' is unavailable.");
                return false;
            }

            if (!IsRealPrimaryWeaponSlot(EquipmentIndex.Weapon0) ||
                !TryClearAllWeaponSlots(agent, role))
            {
                return false;
            }

            var stoneAmount = UsesStoningExecution() &&
                              string.Equals(role, "player", StringComparison.OrdinalIgnoreCase)
                ? 10
                : 1;
            var weapon = UsesStoningExecution()
                ? new MissionWeapon(item, null, agent.Origin?.Banner, (short)stoneAmount)
                : new MissionWeapon(item, null, agent.Origin?.Banner);
            agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref weapon);
            if (UsesCrossbowExecution())
            {
                var boltItem = Game.Current?.ObjectManager.GetObject<ItemObject>(CrossbowBoltItemId);
                if (boltItem is null)
                {
                    RexLog.Error($"Native crossbow ammunition '{CrossbowBoltItemId}' is unavailable.");
                    return false;
                }

                var bolts = new MissionWeapon(boltItem, null, agent.Origin?.Banner);
                agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon1, ref bolts);
            }

            if (!TryEnsureAdditionalCeremonyWeaponWielded(agent, role))
            {
                RexLog.Error(
                    $"Native ceremony item '{itemId}' was added for the {role}, but Bannerlord did not retain it in hand.");
                return false;
            }

            if (UsesCrossbowExecution())
            {
                agent.SetReloadAmmoInSlot(
                    EquipmentIndex.Weapon0,
                    EquipmentIndex.Weapon1,
                    reloadedAmmo: 1);
                RexLog.Info(
                    $"Session {Request.SessionId} loaded native '{CrossbowBoltItemId}' ammunition " +
                    $"into the {role} execution crossbow.");
            }

            RexLog.Info(
                $"Session {Request.SessionId} equipped and confirmed native ceremony item '{itemId}' for the {role}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not equip native ceremony item '{itemId}' for the {role}.",
                exception);
            return false;
        }
    }

    private static bool TryClearAllWeaponSlots(Agent agent, string role)
    {
        try
        {
            agent.TryToSheathWeaponInHand(
                Agent.HandIndex.MainHand,
                Agent.WeaponWieldActionType.Instant);
            agent.TryToSheathWeaponInHand(
                Agent.HandIndex.OffHand,
                Agent.WeaponWieldActionType.Instant);
            for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                 slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                 slotIndex++)
            {
                var slot = (EquipmentIndex)slotIndex;
                if (!agent.Equipment[slot].IsEmpty)
                {
                    agent.RemoveEquippedWeapon(slot);
                }
            }

            for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                 slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                 slotIndex++)
            {
                if (!agent.Equipment[(EquipmentIndex)slotIndex].IsEmpty)
                {
                    RexLog.Error(
                        $"The {role} agent {agent.Index} retained a weapon in slot {slotIndex} after clearing.");
                    return false;
                }
            }

            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not clear all weapon slots for the {role}.", exception);
            return false;
        }
    }

    private static bool IsRealPrimaryWeaponSlot(EquipmentIndex slot) =>
        slot == EquipmentIndex.Weapon0 ||
        slot == EquipmentIndex.Weapon1 ||
        slot == EquipmentIndex.Weapon2 ||
        slot == EquipmentIndex.Weapon3;

    private static bool IsRestorablePlayerWeaponSlot(EquipmentIndex slot) =>
        IsRealPrimaryWeaponSlot(slot) ||
        slot == EquipmentIndex.ExtraWeaponSlot;

    private static bool IsExecutionAxeWeapon(MissionWeapon weapon) =>
        !weapon.IsEmpty &&
        string.Equals(
            weapon.Item?.StringId,
            ExecutionAxeItemId,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsExecutionTorchWeapon(MissionWeapon weapon) =>
        !weapon.IsEmpty &&
        string.Equals(
            weapon.Item?.StringId,
            ExecutionTorchItemId,
            StringComparison.OrdinalIgnoreCase);

    private bool IsAdditionalCeremonyWeapon(MissionWeapon weapon)
    {
        var itemId = GetCeremonyWeaponItemId();
        return !weapon.IsEmpty &&
               !string.IsNullOrWhiteSpace(itemId) &&
               string.Equals(
                   weapon.Item?.StringId,
                   itemId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCrossbowBoltWeapon(MissionWeapon weapon) =>
        !weapon.IsEmpty &&
        string.Equals(
            weapon.Item?.StringId,
            CrossbowBoltItemId,
            StringComparison.OrdinalIgnoreCase);

    private static bool HasOnlyExecutionAxeInPrimarySlot(Agent agent)
    {
        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var slot = (EquipmentIndex)slotIndex;
            var weapon = agent.Equipment[slot];
            if (slot == EquipmentIndex.Weapon0)
            {
                if (!IsExecutionAxeWeapon(weapon))
                {
                    return false;
                }
            }
            else if (!weapon.IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOnlyExecutionTorchInPrimarySlot(Agent agent)
    {
        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var slot = (EquipmentIndex)slotIndex;
            var weapon = agent.Equipment[slot];
            if (slot == EquipmentIndex.Weapon0)
            {
                if (!IsExecutionTorchWeapon(weapon))
                {
                    return false;
                }
            }
            else if (!weapon.IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasOnlyAdditionalCeremonyWeaponInPrimarySlot(Agent agent)
    {
        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var slot = (EquipmentIndex)slotIndex;
            var weapon = agent.Equipment[slot];
            if (slot == EquipmentIndex.Weapon0)
            {
                if (!IsAdditionalCeremonyWeapon(weapon))
                {
                    return false;
                }
            }
            else if (UsesCrossbowExecution() && slot == EquipmentIndex.Weapon1)
            {
                if (!IsCrossbowBoltWeapon(weapon))
                {
                    return false;
                }
            }
            else if (!weapon.IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryEnsureAdditionalCeremonyWeaponWielded(Agent agent, string role)
    {
        var itemId = GetCeremonyWeaponItemId() ?? "<missing>";
        try
        {
            if (!agent.IsActive())
            {
                RexLog.Error(
                    $"Cannot wield ceremony item '{itemId}' for inactive {role} agent {agent.Index}.");
                return false;
            }

            if (!HasOnlyAdditionalCeremonyWeaponInPrimarySlot(agent))
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} does not have only '{itemId}' in Weapon0.");
                return false;
            }

            if (agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.Weapon0 &&
                agent.GetOffhandWieldedItemIndex() == EquipmentIndex.None &&
                IsAdditionalCeremonyWeapon(agent.WieldedWeapon))
            {
                return true;
            }

            agent.TryToWieldWeaponInSlot(
                EquipmentIndex.Weapon0,
                Agent.WeaponWieldActionType.Instant,
                isWieldedOnSpawn: false);
            var retained = agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.Weapon0 &&
                           agent.GetOffhandWieldedItemIndex() == EquipmentIndex.None &&
                           IsAdditionalCeremonyWeapon(agent.WieldedWeapon);
            if (!retained)
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} rejected ceremony item '{itemId}' " +
                    $"(primary={agent.GetPrimaryWieldedItemIndex()}, offhand={agent.GetOffhandWieldedItemIndex()}).");
            }

            return retained;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not confirm ceremony item '{itemId}' in hand for the {role}.",
                exception);
            return false;
        }
    }

    private bool IsAdditionalCeremonyWeaponStillWielded(Agent agent, string role)
    {
        var itemId = GetCeremonyWeaponItemId() ?? "<missing>";
        try
        {
            var retained = HasOnlyAdditionalCeremonyWeaponInPrimarySlot(agent) &&
                           agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.Weapon0 &&
                           agent.GetOffhandWieldedItemIndex() == EquipmentIndex.None &&
                           IsAdditionalCeremonyWeapon(agent.WieldedWeapon);
            if (!retained)
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} lost ceremony item '{itemId}' while binding the action.");
            }

            return retained;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not read back ceremony item '{itemId}' for the {role}.",
                exception);
            return false;
        }
    }

    private bool TryEnsureExecutionTorchWielded(Agent agent, string role)
    {
        try
        {
            if (!agent.IsActive())
            {
                RexLog.Error($"Cannot wield the execution torch for inactive {role} agent {agent.Index}.");
                return false;
            }

            var torchSlot = EquipmentIndex.Weapon0;
            if (!IsRealPrimaryWeaponSlot(torchSlot) ||
                !HasOnlyExecutionTorchInPrimarySlot(agent))
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} does not have only '{ExecutionTorchItemId}' " +
                    "in a real primary weapon slot.");
                return false;
            }

            if (IsExecutionTorchWielded(agent))
            {
                return true;
            }

            agent.TryToWieldWeaponInSlot(
                torchSlot,
                Agent.WeaponWieldActionType.Instant,
                isWieldedOnSpawn: false);
            if (!IsExecutionTorchWielded(agent))
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} rejected the torch wield request " +
                    $"(primary={agent.GetPrimaryWieldedItemIndex()}, " +
                    $"offhand={agent.GetOffhandWieldedItemIndex()}).");
                return false;
            }

            RexLog.Info(
                $"Session {Request.SessionId} confirmed the {role} execution torch in the right/main hand " +
                $"(primary={agent.GetPrimaryWieldedItemIndex()}, " +
                $"offhand={agent.GetOffhandWieldedItemIndex()}).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not confirm the execution torch in hand for the {role}.", exception);
            return false;
        }
    }

    private static bool IsExecutionTorchWielded(Agent agent)
    {
        var primarySlot = agent.GetPrimaryWieldedItemIndex();
        var offhandSlot = agent.GetOffhandWieldedItemIndex();
        return primarySlot == EquipmentIndex.Weapon0 &&
               offhandSlot == EquipmentIndex.None &&
               IsExecutionTorchWeapon(agent.WieldedWeapon);
    }

    private bool IsExecutionTorchStillWielded(Agent agent, string role)
    {
        try
        {
            var retained = HasOnlyExecutionTorchInPrimarySlot(agent) &&
                           IsExecutionTorchWielded(agent);
            if (!retained)
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} lost the execution torch while binding the action " +
                    $"(primary={agent.GetPrimaryWieldedItemIndex()}, " +
                    $"offhand={agent.GetOffhandWieldedItemIndex()}).");
            }

            return retained;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not read back the torch for the {role}.", exception);
            return false;
        }
    }

    private bool TryEnsureExecutionAxeWielded(Agent agent, string role)
    {
        try
        {
            if (!agent.IsActive())
            {
                RexLog.Error($"Cannot wield the execution axe for inactive {role} agent {agent.Index}.");
                return false;
            }

            var axeSlot = EquipmentIndex.Weapon0;
            if (!IsRealPrimaryWeaponSlot(axeSlot) ||
                !HasOnlyExecutionAxeInPrimarySlot(agent))
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} does not have only '{ExecutionAxeItemId}' " +
                    "in a real primary weapon slot.");
                return false;
            }

            var primarySlot = agent.GetPrimaryWieldedItemIndex();
            var wieldedWeapon = agent.WieldedWeapon;
            if (primarySlot == axeSlot &&
                IsExecutionAxeWeapon(wieldedWeapon) &&
                wieldedWeapon.CurrentUsageItem?.IsTwoHanded == true &&
                agent.GetOffhandWieldedItemIndex() == EquipmentIndex.None)
            {
                // A native wield request drives channel 0. When the axe is
                // already correct, keep this a pure readback so it cannot
                // replace the execution action that was just bound.
                return true;
            }

            agent.TryToWieldWeaponInSlot(
                axeSlot,
                Agent.WeaponWieldActionType.Instant,
                isWieldedOnSpawn: false);
            primarySlot = agent.GetPrimaryWieldedItemIndex();
            wieldedWeapon = agent.WieldedWeapon;
            if (!IsRealPrimaryWeaponSlot(primarySlot) ||
                primarySlot != axeSlot ||
                !IsExecutionAxeWeapon(wieldedWeapon) ||
                wieldedWeapon.CurrentUsageItem?.IsTwoHanded != true ||
                agent.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} rejected the execution axe wield request " +
                    $"(primary={primarySlot}, offhand={agent.GetOffhandWieldedItemIndex()}).");
                return false;
            }

            RexLog.Info(
                $"Session {Request.SessionId} confirmed the {role} execution axe in hand " +
                $"(primary={primarySlot}).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not confirm the execution axe in hand for the {role}.", exception);
            return false;
        }
    }

    private bool IsExecutionAxeStillWielded(Agent agent, string role)
    {
        try
        {
            var primarySlot = agent.GetPrimaryWieldedItemIndex();
            var retained = IsRealPrimaryWeaponSlot(primarySlot) &&
                           primarySlot == EquipmentIndex.Weapon0 &&
                           HasOnlyExecutionAxeInPrimarySlot(agent) &&
                           IsExecutionAxeWeapon(agent.WieldedWeapon) &&
                           agent.WieldedWeapon.CurrentUsageItem?.IsTwoHanded == true &&
                           agent.GetOffhandWieldedItemIndex() == EquipmentIndex.None;
            if (!retained)
            {
                RexLog.Error(
                    $"The {role} agent {agent.Index} lost the execution axe while binding the strike " +
                    $"(primary={primarySlot}, offhand={agent.GetOffhandWieldedItemIndex()}).");
            }

            return retained;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not read back the strike weapon for the {role}.", exception);
            return false;
        }
    }

    private bool RestorePlayerExecutionEquipment(Agent player)
    {
        if (!_playerWieldStateCaptured)
        {
            return true;
        }

        try
        {
            if (_playerWeaponSlotsModifiedForCeremony)
            {
                if (!TryClearAllWeaponSlots(player, "player restoration"))
                {
                    return false;
                }

                for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                     slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                     slotIndex++)
                {
                    var previousWeapon = _playerWeaponsBeforeCeremony[slotIndex];
                    if (!previousWeapon.IsEmpty)
                    {
                        player.EquipWeaponWithNewEntity(
                            (EquipmentIndex)slotIndex,
                            ref previousWeapon);
                    }
                }

                if (!ArePlayerWeaponSlotsRestored(player))
                {
                    RexLog.Error("The player's five weapon slots did not survive restoration readback.");
                    return false;
                }

                // Slot rebuilding succeeded. If hand-state readback needs a
                // later retry, never clear and recreate all five slots again.
                _playerWeaponSlotsModifiedForCeremony = false;
            }

            // An existing player weapon can be used without modifying any slot.
            // Always sheath that ceremony wield state first, including when the
            // original saved state had both hands empty.
            player.TryToSheathWeaponInHand(
                Agent.HandIndex.MainHand,
                Agent.WeaponWieldActionType.Instant);
            player.TryToSheathWeaponInHand(
                Agent.HandIndex.OffHand,
                Agent.WeaponWieldActionType.Instant);
            TryRestoreWieldedWeapon(player, _playerPrimaryWieldedBeforeCeremony);
            TryRestoreWieldedWeapon(player, _playerOffhandWieldedBeforeCeremony);
            if (!ArePlayerWeaponSlotsRestored(player) ||
                !IsPlayerWieldStateRestored(player))
            {
                RexLog.Error(
                    "The player's five weapon slots or original wield state did not survive restoration readback.");
                return false;
            }

            _playerWeaponSlotsModifiedForCeremony = false;
            _playerWieldStateCaptured = false;
            _playerPrimaryWasTwoHanded = false;
            for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
                 slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
                 slotIndex++)
            {
                _playerWeaponsBeforeCeremony[slotIndex] = MissionWeapon.Invalid;
            }

            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "Player weapon restoration failed and will be retried during cleanup.",
                exception);
            return false;
        }
    }

    private static void TryRestoreWieldedWeapon(Agent player, EquipmentIndex slot)
    {
        if (!IsRestorablePlayerWeaponSlot(slot) ||
            player.Equipment[slot].IsEmpty)
        {
            return;
        }

        player.TryToWieldWeaponInSlot(
            slot,
            Agent.WeaponWieldActionType.InstantAfterPickUp,
            isWieldedOnSpawn: false);
    }

    private bool ArePlayerWeaponSlotsRestored(Agent player)
    {
        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var expected = _playerWeaponsBeforeCeremony[slotIndex];
            var actual = player.Equipment[(EquipmentIndex)slotIndex];
            if (expected.IsEmpty != actual.IsEmpty)
            {
                return false;
            }

            if (!expected.IsEmpty &&
                (actual.Item != expected.Item ||
                 actual.ItemModifier != expected.ItemModifier ||
                 actual.CurrentUsageIndex != expected.CurrentUsageIndex ||
                 actual.Amount != expected.Amount ||
                 actual.ReloadPhase != expected.ReloadPhase ||
                 actual.Ammo != expected.Ammo))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPlayerWieldStateRestored(Agent player)
    {
        var primaryRestored = IsRestorablePlayerWeaponSlot(_playerPrimaryWieldedBeforeCeremony)
            ? player.GetPrimaryWieldedItemIndex() == _playerPrimaryWieldedBeforeCeremony
            : player.GetPrimaryWieldedItemIndex() == EquipmentIndex.None;
        var offhandRestored = IsRestorablePlayerWeaponSlot(_playerOffhandWieldedBeforeCeremony)
            ? player.GetOffhandWieldedItemIndex() == _playerOffhandWieldedBeforeCeremony
            : player.GetOffhandWieldedItemIndex() == EquipmentIndex.None;
        var twoHandedRestored = !_playerPrimaryWasTwoHanded ||
                                (primaryRestored &&
                                 player.WieldedWeapon.CurrentUsageItem?.IsTwoHanded == true &&
                                 player.GetOffhandWieldedItemIndex() == EquipmentIndex.None);
        return primaryRestored && offhandRestored && twoHandedRestored;
    }

    private bool TryPrepareGenericExecutionFallback(Agent? actionAgent)
    {
        // This is the last visible fallback. Animation failures are diagnostic;
        // they must not eject the player from an otherwise valid execution scene.
        return actionAgent is not null &&
               actionAgent.IsActive();
    }

    private bool TryStartVisibleExecutionFallback(
        Agent? actionAgent,
        string failedActionName)
    {
        if (actionAgent is null || !actionAgent.IsActive())
        {
            return false;
        }

        // The generic clip is the final visible fallback. If it also fails,
        // continue to the lethal flow instead of cycling back to the axe clip.
        if (string.Equals(
                failedActionName,
                GenericExecutionAction,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (RequiresExecutionAxe() &&
            !string.Equals(
                failedActionName,
                _ceremonyFallbackExecutionActionName,
                StringComparison.OrdinalIgnoreCase))
        {
            RexLog.Warning(
                $"Execution action '{failedActionName}' failed; switching to stable axe fallback " +
                $"'{_ceremonyFallbackExecutionActionName}' without ending the mission.");
            if (TryStartExecutionActionClip(
                    actionAgent,
                    _ceremonyFallbackExecutionActionName,
                    isGenericFallback: false))
            {
                return true;
            }
        }

        if (!string.Equals(
                failedActionName,
                GenericExecutionAction,
                StringComparison.OrdinalIgnoreCase))
        {
            RexLog.Warning(
                $"Execution action '{failedActionName}' and its method fallback failed; " +
                $"trying last visible fallback '{GenericExecutionAction}'.");
            if (TryPrepareGenericExecutionFallback(actionAgent) &&
                TryStartExecutionActionClip(
                    actionAgent,
                    GenericExecutionAction,
                    isGenericFallback: true))
            {
                return true;
            }
        }

        return false;
    }

    private void StartExecutionAction()
    {
        if (_strategy is null)
        {
            return;
        }

        _strategy.StartExecutionAction();
    }

    private void TickExecution(float dt)
    {
        if (_strategy is null)
        {
            return;
        }

        TickStoningCrowdThrows(dt);
        _strategy.TickExecution(dt);
        if (_strategy.UsesStagedExecution)
        {
            _strategy.TickMethodSequence(dt);
        }

        if (_lethalAttempted)
        {
            _strategy.TickAfterDeath(dt);
        }
    }

    private void StartBeheadingExecutionAction()
    {
        if (_actionStarted || _sceneAct is null)
        {
            return;
        }

        _actionStarted = true;
        _executionActionRootCorrectionLogged = false;
        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        // NPC and player beheadings share the authored custom clip. Every other
        // execution method keeps its existing scene action.
        var primaryActionName = RequiresExecutionAxe()
            ? _ceremonyExecutionActionName
            : _sceneAct.ExecutionAction;
        if (TryStartExecutionActionClip(
                actionAgent,
                primaryActionName,
                isGenericFallback: false))
        {
            return;
        }

        if (TryStartVisibleExecutionFallback(actionAgent, primaryActionName))
        {
            return;
        }

        ContinueAfterAnimationFailure(
            $"Action '{primaryActionName}' and all visible fallbacks could not start.");
    }

    private void StartStagedMethodExecutionAction()
    {
        if (_actionStarted || _sceneAct is null)
        {
            return;
        }

        _actionStarted = true;
        _executionActionRootCorrectionLogged = false;
        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        var primaryActionName = _sceneAct.ExecutionAction;
        if (TryStartExecutionActionClip(
                actionAgent,
                primaryActionName,
                isGenericFallback: false))
        {
            return;
        }

        RexLog.Error(
            $"Method action '{primaryActionName}' could not start. The visible " +
            $"{CurrentMethod.StringId} victim sequence will continue without ending the mission.");
        _strategy.BeginMethodSequence("method action start failure");
    }

    private void TickBeheadingExecution(float dt)
    {
        if (_lethalAttempted || _sceneAct is null || State != ExecutionSessionState.Execution)
        {
            return;
        }

        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        ReassertExecutionActionRoot(actionAgent, "execution tick");
        var progress = ObserveExecutionAction(actionAgent);
        if (!_executionActionAdvanced)
        {
            var observationLimit = _usingGenericExecutionAction
                ? GenericActionObservationSeconds
                : PrimaryActionObservationSeconds;
            if (_executionActionAttemptElapsed < observationLimit)
            {
                return;
            }

            if (TryStartVisibleExecutionFallback(actionAgent, _executionActionName))
            {
                return;
            }

            ContinueAfterAnimationFailure(
                $"Execution action '{_executionActionName}' did not bind to channel 0 and advance.");
            return;
        }

        if (progress >= _sceneAct.LethalProgress ||
            _maximumExecutionActionProgress >= _sceneAct.LethalProgress)
        {
            if (actionAgent is not null)
            {
                ReassertExecutionActionRoot(actionAgent, "lethal frame");
                LogCeremonyVisualSeparation(actionAgent, "lethal frame");
            }

            RexLog.Info(
                $"Session {Request.SessionId} action '{_executionActionName}' reached the lethal frame " +
                $"(progress={MathF.Max(progress, _maximumExecutionActionProgress):0.000}, " +
                $"weight={GetExecutionActionWeight(actionAgent):0.000}).");
            CommitLethalFrame();
            return;
        }

        if (_executionElapsed >= _sceneAct.MaximumActionSeconds)
        {
            if (!TryStartVisibleExecutionFallback(actionAgent, _executionActionName))
            {
                ContinueAfterAnimationFailure(
                    $"Execution action '{_executionActionName}' stalled at progress " +
                    $"{_maximumExecutionActionProgress:0.000} before its lethal frame.");
            }
        }
    }

    private void TickStagedMethodExecution(float dt)
    {
        var safeDt = MathF.Max(0f, dt);
        _executionElapsed += safeDt;
        _executionActionAttemptElapsed += safeDt;
        if (!_actionStarted)
        {
            StartStagedMethodExecutionAction();
        }

        if (_lethalAttempted || _sceneAct is null || State != ExecutionSessionState.Execution)
        {
            return;
        }

        if (_methodExecutionPhase != MethodExecutionPhase.None)
        {
            TickMethodExecutionSequence(safeDt);
            return;
        }

        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        ReassertExecutionActionRoot(actionAgent, $"{CurrentMethod.StringId} method-action tick");
        var progress = ObserveExecutionAction(actionAgent);
        if (!_executionActionAdvanced)
        {
            if (_executionActionAttemptElapsed < PrimaryActionObservationSeconds)
            {
                return;
            }

            RexLog.Error(
                $"Method action '{_executionActionName}' did not visibly bind to channel 0. " +
                $"The {CurrentMethod.StringId} victim sequence will continue without a generic kick fallback.");
            BeginMethodExecutionSequence("method action read-back failure");
            return;
        }

        if (progress >= _sceneAct.LethalProgress ||
            _maximumExecutionActionProgress >= _sceneAct.LethalProgress)
        {
            if (actionAgent is not null)
            {
                ReassertExecutionActionRoot(actionAgent, $"{CurrentMethod.StringId} release frame");
                LogCeremonyVisualSeparation(actionAgent, $"{CurrentMethod.StringId} release frame");
            }

            RexLog.Info(
                $"Session {Request.SessionId} method action '{_executionActionName}' reached its release frame " +
                $"(progress={MathF.Max(progress, _maximumExecutionActionProgress):0.000}, " +
                $"weight={GetExecutionActionWeight(actionAgent):0.000}); starting the dedicated " +
                $"{CurrentMethod.StringId} victim sequence before battlefield death.");
            BeginMethodExecutionSequence("method release frame");
            return;
        }

        if (_executionElapsed >= _sceneAct.MaximumActionSeconds)
        {
            RexLog.Error(
                $"Method action '{_executionActionName}' stalled at progress " +
                $"{_maximumExecutionActionProgress:0.000}. The dedicated {CurrentMethod.StringId} " +
                "victim sequence will continue without ending the mission.");
            BeginMethodExecutionSequence("method action timeout");
        }
    }

    private void BeginMethodExecutionSequence(string trigger)
    {
        if (_methodExecutionPhase != MethodExecutionPhase.None || _lethalAttempted)
        {
            return;
        }

        _methodExecutionPhaseElapsed = 0f;
        MaintainMethodVictimRoot();
        if (string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Hanging,
                StringComparison.OrdinalIgnoreCase))
        {
            _methodExecutionPhase = MethodExecutionPhase.HangingStruggle;
            TryPlayHangingReleaseSound();
            TryPlayMethodVictimAction(
                HangingVictimStruggleAction,
                "hanging victim struggle");
            RexLog.Info(
                $"Session {Request.SessionId} began the hanging sequence after {trigger}: " +
                $"struggle={HangingStruggleSeconds:0.00}s, " +
                $"deathPoseLeadIn={HangingDeathPoseLeadInSeconds:0.00}s.");
            return;
        }

        if (string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Burning,
                StringComparison.OrdinalIgnoreCase))
        {
            _methodExecutionPhase = MethodExecutionPhase.BurningBaseAndLegs;
            TryIgniteExecutionEffectRoles(
                "base and legs",
                "pyre_fire_base",
                "pyre_fire_left_leg",
                "pyre_fire_right_leg");
            TryPlayMethodVictimAction(
                BurningVictimIgnitionReactionAction,
                "burning victim ignition reaction");
            RexLog.Info(
                $"Session {Request.SessionId} began the staged burning sequence after {trigger}: " +
                $"base/legs={BurningBaseAndLegsSeconds:0.00}s, waist={BurningWaistSeconds:0.00}s, " +
                $"torso={BurningTorsoSeconds:0.00}s, " +
                $"deathPoseLeadIn={BurningDeathPoseLeadInSeconds:0.00}s.");
            return;
        }

        RexLog.Error(
            $"Method '{CurrentMethod.StringId}' entered the staged execution dispatcher without a " +
            "dedicated sequence; continuing through the existing lethal flow.");
        _methodExecutionPhase = MethodExecutionPhase.Completed;
        CommitLethalFrame();
    }

    private void TickMethodExecutionSequence(float dt)
    {
        if (_methodExecutionPhase is MethodExecutionPhase.None or MethodExecutionPhase.Completed)
        {
            return;
        }

        MaintainMethodVictimRoot();
        if (string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Hanging,
                StringComparison.OrdinalIgnoreCase))
        {
            UpdateHangingLift(dt);
            UpdateHangingRopeToVictim();
            MaintainHangingSuspension(dt);
        }

        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        ReassertExecutionActionRoot(
            actionAgent,
            $"{CurrentMethod.StringId} sequence {_methodExecutionPhase}");
        _methodExecutionPhaseElapsed += MathF.Max(0f, dt);

        switch (_methodExecutionPhase)
        {
            case MethodExecutionPhase.HangingStruggle
                when _methodExecutionPhaseElapsed >= HangingStruggleSeconds:
                EnterMethodExecutionPhase(MethodExecutionPhase.HangingDeathPose);
                TryPlayMethodVictimAction(
                    _sceneAct?.DeathAction ?? "act_death_by_arrow_neck1",
                    "hanging final neck-death pose");
                break;

            case MethodExecutionPhase.HangingDeathPose
                when _methodExecutionPhaseElapsed >= HangingDeathPoseLeadInSeconds:
                CompleteMethodExecutionSequence("hanging neck-death pose");
                break;

            case MethodExecutionPhase.BurningBaseAndLegs
                when _methodExecutionPhaseElapsed >= BurningBaseAndLegsSeconds:
                EnterMethodExecutionPhase(MethodExecutionPhase.BurningWaist);
                TryIgniteExecutionEffectRoles("waist", "pyre_fire_waist");
                TryPlayMethodVictimAction(
                    BurningVictimSustainedFearAction,
                    "burning victim sustained fear");
                break;

            case MethodExecutionPhase.BurningWaist
                when _methodExecutionPhaseElapsed >= BurningWaistSeconds:
                EnterMethodExecutionPhase(MethodExecutionPhase.BurningTorso);
                TryIgniteExecutionEffectRoles("torso", "pyre_fire_torso");
                TryPlayMethodVictimAction(
                    BurningVictimTorsoReactionAction,
                    "burning victim torso reaction");
                break;

            case MethodExecutionPhase.BurningTorso
                when _methodExecutionPhaseElapsed >= BurningTorsoSeconds:
                EnterMethodExecutionPhase(MethodExecutionPhase.BurningDeathPose);
                TryPlayMethodVictimAction(
                    _sceneAct?.DeathAction ?? "act_death_by_fire_front",
                    "burning final fire-death pose");
                break;

            case MethodExecutionPhase.BurningDeathPose
                when _methodExecutionPhaseElapsed >= BurningDeathPoseLeadInSeconds:
                CompleteMethodExecutionSequence("burning fire-death pose");
                break;
        }
    }

    private void EnterMethodExecutionPhase(MethodExecutionPhase nextPhase)
    {
        _methodExecutionPhase = nextPhase;
        _methodExecutionPhaseElapsed = 0f;
        RexLog.Info(
            $"Session {Request.SessionId} entered {CurrentMethod.StringId} phase {nextPhase}.");
    }

    private void CompleteMethodExecutionSequence(string finalPose)
    {
        MaintainMethodVictimRoot();
        _methodExecutionPhase = MethodExecutionPhase.Completed;
        _methodExecutionPhaseElapsed = 0f;
        try
        {
            var victimAction = _victimAgent?.GetCurrentAction(0) ?? ActionIndexCache.act_none;
            var victimProgress = _victimAgent?.GetCurrentActionProgress(0) ?? -1f;
            var completionPath = _strategy.DefersVictimDeathToMissionExit
                ? "The final-pose freeze now takes over without RegisterBlow or ragdoll."
                : "The existing single-Blow death chain now takes over.";
            RexLog.Info(
                $"Session {Request.SessionId} completed the visible {CurrentMethod.StringId} sequence " +
                $"at {finalPose}; victimAction={victimAction.Index}, " +
                $"victimProgress={victimProgress:0.000}. {completionPath}");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not read back the {CurrentMethod.StringId} final pose; the lethal flow continues.",
                exception);
        }

        CommitLethalFrame();
    }

    private bool TryPlayMethodVictimAction(string actionName, string purpose)
    {
        if (!TryPlayRequiredAction(
                _victimAgent,
                actionName,
                purpose,
                out var preparedAction,
                forceFullBody: true))
        {
            RexLog.Error(
                $"Victim action '{actionName}' for {purpose} could not start. The timed " +
                $"{CurrentMethod.StringId} sequence will continue without ending the mission.");
            return false;
        }

        try
        {
            var currentMatches = _victimAgent is not null &&
                                 _victimAgent.GetCurrentAction(0) == preparedAction;
            var weight = _victimAgent?.GetActionChannelWeight(0) ?? 0f;
            RexLog.Info(
                $"Session {Request.SessionId} requested victim action '{actionName}' for {purpose} " +
                $"(channel=0, readBack={currentMatches}, weight={weight:0.000}).");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Victim action '{actionName}' started but its channel-0 read-back failed; " +
                "the timed sequence continues.",
                exception);
        }

        return true;
    }

    private void MaintainMethodVictimRoot()
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive() || !_victimPosition.IsValid || _placement is null)
        {
            return;
        }

        if (RequiresExecutionTorch())
        {
            // BurningExecutionStrategy owns the original prisoner's complete
            // native usable-object attachment and target-Z state. The shared
            // TeleportToPosition correction clears that state and was the
            // reason only the hand IK moved upward while the body stayed on the
            // navigation surface.
            return;
        }

        try
        {
            var expectedDirection = _placement.Forward.IsNonZero()
                ? _placement.Forward.Normalized()
                : Vec2.Forward;
            // While the hanging lift is active, maintain the prisoner at the
            // raised root so the rope visually hauls the victim off the deck.
            var targetPosition = string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Hanging,
                StringComparison.OrdinalIgnoreCase) &&
                _hangingLiftPosition.IsValid
                ? _hangingLiftPosition
                : _victimPosition;
            var positionError = victim.Position.Distance(targetPosition);
            var actualDirection = victim.LookDirection.AsVec2;
            var facingDot = actualDirection.IsNonZero()
                ? Vec2.DotProduct(actualDirection.Normalized(), expectedDirection)
                : -1f;
            if (positionError <= ExecutionActionRootCorrectionThreshold && facingDot >= 0.995f)
            {
                return;
            }

            var actionBeforeCorrection = victim.GetCurrentAction(0);
            PoseAgentAt(victim, targetPosition, expectedDirection);
            var actionAfterCorrection = victim.GetCurrentAction(0);
            if (actionAfterCorrection != actionBeforeCorrection)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} victim channel 0 changed while maintaining the " +
                    $"{CurrentMethod.StringId} ritual root: before={actionBeforeCorrection.Index}, " +
                    $"after={actionAfterCorrection.Index}. The timed sequence continues.");
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not maintain the victim at the {CurrentMethod.StringId} ritual point; " +
                "the timed sequence continues without ending the mission.",
                exception);
        }
    }

    private void TryPlayHangingReleaseSound()
    {
        if (_hangingReleaseSoundAttempted)
        {
            return;
        }

        _hangingReleaseSoundAttempted = true;
        try
        {
            var soundId = SoundEvent.GetEventIdFromString(HangingReleaseSoundEvent);
            if (soundId < 0)
            {
                RexLog.Warning(
                    $"Hanging release sound '{HangingReleaseSoundEvent}' could not be resolved; " +
                    "the visible sequence continues.");
                return;
            }

            var owner = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
            Mission.MakeSound(
                soundId,
                _victimPosition,
                soundCanBePredicted: false,
                isReliable: true,
                owner?.Index ?? -1,
                _victimAgent?.Index ?? -1);
            RexLog.Info(
                $"Session {Request.SessionId} played the native gallows release sound.");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "The gallows release sound failed; the visible hanging sequence continues.",
                exception);
        }
    }

    private bool TryStartExecutionActionClip(
        Agent? actionAgent,
        string actionName,
        bool isGenericFallback)
    {
        if (actionAgent is null)
        {
            return false;
        }

        // Every player-performed sentence must begin facing the fixed prisoner
        // ritual point. This now explicitly includes hanging: its lever action
        // used to bind without the player-facing lock and could start while the
        // player was looking away from the condemned.
        var usesPlayerFacingLock =
            _actor == ExecutionActor.Player && !UsesStoningExecution();
        var enforcePlayerRootRotation = usesPlayerFacingLock;
        if (usesPlayerFacingLock &&
            !_playerExecutionFacingLockActive &&
            !CapturePlayerExecutionFacingLock(actionAgent))
        {
            return false;
        }

        var actingRole = _actor == ExecutionActor.Player ? "acting player" : "acting executioner";
        if (RequiresCeremonyWeapon() &&
            !TryEnsureCeremonyWeaponWielded(actionAgent, actingRole + " before strike"))
        {
            return false;
        }

        try
        {
            // Wield/readback can replace channel 0 and authored idles can rotate
            // or translate the skeleton. Fixed-root methods re-pin to their
            // verified action mark; player burning keeps the captured F-key
            // position and reasserts facing only. Validate the pair one final time.
            ReassertCeremonyPoseBeforePlayback(actionAgent);
            VerifyPosedCeremonyFormation(actionAgent);
            if (!IsCeremonyPairStillClear(actionAgent))
            {
                return false;
            }

            LogCeremonyVisualSeparation(actionAgent, "immediately before strike binding");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "The execution actor could not be re-pinned to the verified side mark before the strike.",
                exception);
            return false;
        }

        if (!TryResolveAndPrefetchAction(
                actionAgent,
                actionName,
                isGenericFallback ? "generic execution fallback" : "execution action",
                out var action))
        {
            return false;
        }

        // The imported custom clip contains Armature-object location keys even
        // though its pelvis translation is zero. Move only its bind root by the
        // measured inverse offset before native animation evaluation starts.
        // Once the channel is bound, TeleportToPosition cannot remove this
        // authored displacement, so doing it before SetActionChannel is vital.
        ApplyAuthoredExecutionRootBindingCompensation(actionAgent, actionName);

        var accepted = TrySetPreparedAction(
            actionAgent,
            action,
            actionName,
            isGenericFallback ? "generic execution fallback" : "execution action",
            forceFullBody: true,
            enforceRootRotation: enforcePlayerRootRotation);
        if (!accepted)
        {
            return false;
        }

        ReassertExecutionActionRoot(actionAgent, "immediately after channel-0 bind");
        LogCeremonyVisualSeparation(actionAgent, "immediately after compensated strike binding");

        if (RequiresCeremonyWeapon() &&
            (!TryEnsureCeremonyWeaponWielded(actionAgent, actingRole + " after strike binding") ||
             !IsCeremonyWeaponStillWielded(actionAgent, actingRole + " strike") ||
             !IsPreparedActionStillBound(actionAgent, action, actionName)))
        {
            RexLog.Error(
                $"The {actingRole} could not retain both action '{actionName}' and its ceremony weapon.");
            return false;
        }

        ReassertExecutionActionRoot(actionAgent, "after weapon/action read-back");

        _executionAction = action;
        _executionActionName = actionName;
        _executionActionPlaying = true;
        _executionActionAdvanced = false;
        _usingGenericExecutionAction = isGenericFallback;
        _executionActionAttemptElapsed = 0f;
        _executionElapsed = 0f;
        _maximumExecutionActionProgress = 0f;
        RexLog.Info(
            $"Session {Request.SessionId} requested visible action '{actionName}' " +
            $"(index={action.Index}, actor={_actor}, generic={isGenericFallback}, " +
            $"channel=0, enforceRootRotation={enforcePlayerRootRotation}).");
        return true;
    }

    private static bool IsPreparedActionStillBound(
        Agent agent,
        ActionIndexCache expectedAction,
        string actionName)
    {
        try
        {
            return agent.GetCurrentAction(0) == expectedAction;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not read back prepared action '{actionName}' after weapon confirmation.",
                exception);
            return false;
        }
    }

    private float ObserveExecutionAction(Agent? actionAgent)
    {
        if (!_executionActionPlaying ||
            actionAgent is null ||
            !actionAgent.IsActive() ||
            _executionAction == ActionIndexCache.act_none)
        {
            return 0f;
        }

        try
        {
            var current = actionAgent.GetCurrentAction(0);
            var weight = actionAgent.GetActionChannelWeight(0);
            var progress = current == _executionAction
                ? actionAgent.GetCurrentActionProgress(0)
                : 0f;
            if (current == _executionAction && weight > 0.01f)
            {
                _maximumExecutionActionProgress = MathF.Max(
                    _maximumExecutionActionProgress,
                    progress);
                if (!_executionActionAdvanced &&
                    _maximumExecutionActionProgress >= MinimumVisibleActionProgress)
                {
                    _executionActionAdvanced = true;
                    RexLog.Info(
                        $"Session {Request.SessionId} visibly advancing action '{_executionActionName}' " +
                        $"(weight={weight:0.000}, progress={progress:0.000}).");
                    LogCeremonyVisualSeparation(actionAgent, "action start");
                }

                if (!_playerExecutionRootRotationFlagChecked &&
                    _actor == ExecutionActor.Player &&
                    string.Equals(
                        _executionActionName,
                        TwoHandedBeheadingExecutionAction,
                        StringComparison.OrdinalIgnoreCase) &&
                    _executionActionAdvanced)
                {
                    _playerExecutionRootRotationFlagChecked = true;
                    var animationFlags = actionAgent.GetCurrentAnimationFlag(0);
                    var rootRotationEnforced =
                        (animationFlags & AnimFlags.anf_enforce_root_rotation) != 0;
                    if (rootRotationEnforced)
                    {
                        RexLog.Info(
                            $"Session {Request.SessionId} confirmed anf_enforce_root_rotation on the " +
                            "player's advancing channel-0 execution action.");
                    }
                    else
                    {
                        RexLog.Error(
                            $"PLAYER_FACING_INVARIANT_FAILED session={Request.SessionId}: the player's advancing " +
                            "channel-0 action did not read back anf_enforce_root_rotation. " +
                            "The fixed look-direction lock remains active and the mission continues.");
                    }
                }

            }

            return progress;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not observe execution action '{_executionActionName}'.",
                exception);
            return 0f;
        }
    }

    private float GetExecutionActionWeight(Agent? actionAgent)
    {
        try
        {
            return actionAgent is not null && actionAgent.IsActive()
                ? actionAgent.GetActionChannelWeight(0)
                : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    private void TickPostLethalExecutionAction()
    {
        if (!_postLethalExecutionActionPending || !_executionActionPlaying)
        {
            return;
        }

        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        ReassertExecutionActionRoot(actionAgent, "post-lethal action tick");
        CompleteExecutionActionPlayback(force: false);
    }

    private void CompleteExecutionActionPlayback(bool force)
    {
        if (!_postLethalExecutionActionPending ||
            _executionAction == ActionIndexCache.act_none)
        {
            return;
        }

        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        if (actionAgent is null || !actionAgent.IsActive())
        {
            _postLethalExecutionActionPending = false;
            _executionActionPlaying = false;
            if (_actor == ExecutionActor.Player)
            {
                RestorePlayerExecutionFacingLock();
            }

            return;
        }

        try
        {
            var current = actionAgent.GetCurrentAction(0);
            var currentMatchesExecutionAction = current == _executionAction;
            var progress = _maximumExecutionActionProgress;
            var wrapped = false;
            if (currentMatchesExecutionAction)
            {
                progress = actionAgent.GetCurrentActionProgress(0);
                wrapped = progress + ExecutionActionLoopWrapTolerance <
                          _maximumExecutionActionProgress;
                _maximumExecutionActionProgress = MathF.Max(
                    _maximumExecutionActionProgress,
                    progress);
                if (!force &&
                    !wrapped &&
                    progress < ExecutionActionOneShotCompletionProgress)
                {
                    return;
                }
            }

            if (_actor == ExecutionActor.Executioner && _sceneAct is not null)
            {
                if (!TryBindExecutionerPostExecutionIdle(actionAgent))
                {
                    if (currentMatchesExecutionAction &&
                        TryClearExecutionActionChannel(actionAgent))
                    {
                        _executionActionPlaying = false;
                    }

                    RexLog.Warning(
                        $"Session {Request.SessionId} has not visibly bound the post-execution idle yet; retrying.");
                    return;
                }
            }
            else if (!TryClearExecutionActionChannel(actionAgent))
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} could not confirm that execution action '{_executionActionName}' left channel 0; retrying.");
                return;
            }

            var preserveBeheadingStrikeMark = string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Beheading,
                StringComparison.OrdinalIgnoreCase);
            if (!preserveBeheadingStrikeMark && !TryRestoreExecutionerStandby())
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} has not restored the executioner standby position yet; retrying.");
                return;
            }

            if (preserveBeheadingStrikeMark)
            {
                RexLog.Info(
                    $"Session {Request.SessionId} kept the beheading actor at the lethal strike mark; " +
                    "no post-lethal return to the entry standby position was issued.");
            }

            if (_actor == ExecutionActor.Player &&
                !RestorePlayerExecutionFacingLock())
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} cleared channel 0 but has not restored the " +
                    "player's original look lock yet; retrying.");
                return;
            }

            _postLethalExecutionActionPending = false;
            _executionActionPlaying = false;
            SetCeremonyAlliesFormationControl(enabled: true, "execution action completed");
            RexLog.Info(
                $"Session {Request.SessionId} completed execution action '{_executionActionName}' once " +
                $"and stopped it before replay (progress={progress:0.000}, wrapped={wrapped}, forced={force}).");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not finish execution action '{_executionActionName}' without replay; retrying.",
                exception);
        }
    }

    private bool TryBindExecutionerEntryDefaultIdle(Agent executioner)
    {
        if (!RequiresExecutionAxe())
        {
            return TryBindMethodExecutionerEntryIdle(executioner);
        }

        if (TryBindExecutionerIdleAction(
                executioner,
                ExecutionerEntryDefaultIdleAction,
                "entry authored executioner idle",
                out _))
        {
            return true;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} could not bind custom entry idle " +
            $"'{ExecutionerEntryDefaultIdleAction}'; using stable fallback " +
            $"'{TwoHandedCeremonyFallbackIdleAction}' without ending the mission.");
        if (TryBindExecutionerIdleAction(
                executioner,
                TwoHandedCeremonyFallbackIdleAction,
                "entry idle fallback",
                out _))
        {
            return true;
        }

        RexLog.Error(
            $"Session {Request.SessionId} could not bind custom or fallback entry idle; " +
            "continuing at the verified standby mark without ending the mission.");
        return true;
    }

    private bool TryBindMethodExecutionerEntryIdle(Agent executioner)
    {
        var methodIdleAction = _sceneAct?.ExecutionerIdleAction ??
                               UnarmedExecutionerFallbackIdleAction;
        if (TryBindExecutionerIdleAction(
                executioner,
                methodIdleAction,
                $"{CurrentMethod.StringId} entry idle",
                out _))
        {
            return true;
        }

        if (!string.Equals(
                methodIdleAction,
                UnarmedExecutionerFallbackIdleAction,
                StringComparison.OrdinalIgnoreCase) &&
            TryBindExecutionerIdleAction(
                executioner,
                UnarmedExecutionerFallbackIdleAction,
                $"{CurrentMethod.StringId} entry idle fallback",
                out _))
        {
            RexLog.Warning(
                $"Session {Request.SessionId} could not bind method idle '{methodIdleAction}'; " +
                $"using '{UnarmedExecutionerFallbackIdleAction}' without ending the mission.");
            return true;
        }

        RexLog.Error(
            $"Session {Request.SessionId} could not bind the {CurrentMethod.StringId} entry idle; " +
            "continuing at the verified standby mark without ending the mission.");
        return true;
    }

    private bool TryBindExecutionerIdleAction(
        Agent executioner,
        string actionName,
        string purpose,
        out ActionIndexCache boundAction)
    {
        boundAction = ActionIndexCache.act_none;
        try
        {
            if (!TryResolveAndPrefetchAction(
                    executioner,
                    actionName,
                    purpose,
                    out boundAction))
            {
                return false;
            }

            var current = executioner.GetCurrentAction(0);
            if (current == boundAction)
            {
                return ExecutionActionPlaybackPolicy.IsPreparedActionVisiblyBound(
                    currentMatchesPreparedAction: true,
                    executioner.GetActionChannelWeight(0));
            }

            var requestAccepted = TrySetPreparedAction(
                executioner,
                boundAction,
                actionName,
                purpose,
                forceFullBody: true);
            var visiblyBound = ExecutionActionPlaybackPolicy.IsPreparedActionVisiblyBound(
                executioner.GetCurrentAction(0) == boundAction,
                executioner.GetActionChannelWeight(0));
            if (!requestAccepted && !visiblyBound)
            {
                RexLog.Warning($"The {purpose} request was rejected.");
            }

            return visiblyBound;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not confirm {purpose}.", exception);
            return false;
        }
    }

    private bool TryBindExecutionerPostExecutionIdle(Agent executioner)
    {
        if (_sceneAct is null)
        {
            return false;
        }

        try
        {
            if (_executionerIdleAction == ActionIndexCache.act_none &&
                !TryResolveAndPrefetchAction(
                    executioner,
                    _sceneAct.ExecutionerIdleAction,
                    "post-execution idle",
                    out _executionerIdleAction))
            {
                return false;
            }

            var current = executioner.GetCurrentAction(0);
            if (current == _executionerIdleAction)
            {
                return ExecutionActionPlaybackPolicy.IsPreparedActionVisiblyBound(
                    currentMatchesPreparedAction: true,
                    executioner.GetActionChannelWeight(0));
            }

            var requestAccepted = TrySetPreparedAction(
                executioner,
                _executionerIdleAction,
                _sceneAct.ExecutionerIdleAction,
                "post-execution idle",
                forceFullBody: true);
            var visiblyBound = ExecutionActionPlaybackPolicy.IsPreparedActionVisiblyBound(
                executioner.GetCurrentAction(0) == _executionerIdleAction,
                executioner.GetActionChannelWeight(0));
            if (!requestAccepted && !visiblyBound)
            {
                RexLog.Warning("The post-execution idle request was rejected.");
            }

            return visiblyBound;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not confirm the post-execution idle action.", exception);
            return false;
        }
    }

    private bool TryRestoreExecutionerEntryStandby()
    {
        return TryRestoreExecutionerStandby(
            "during initial scene setup",
            useEntryDefaultIdle: true);
    }

    private bool TryRestoreExecutionerStandby(
        string phase = "after the strike",
        bool useEntryDefaultIdle = false)
    {
        if (_executionerStandbyRestored)
        {
            return true;
        }

        var executioner = _executionerAgent;
        if (executioner is null || !executioner.IsActive() ||
            !_executionerStandbyPosition.IsValid)
        {
            return false;
        }

        try
        {
            executioner.DisableScriptedMovement();
            executioner.Controller = AgentControllerType.None;
            PoseAgentAt(
                executioner,
                _executionerStandbyPosition,
                _executionerStandbyDirection);
            var idleBound = useEntryDefaultIdle
                ? TryBindExecutionerEntryDefaultIdle(executioner)
                : TryBindExecutionerPostExecutionIdle(executioner);
            if (!idleBound)
            {
                return false;
            }

            PoseAgentAt(
                executioner,
                _executionerStandbyPosition,
                _executionerStandbyDirection);
            _executionerPosition = _executionerStandbyPosition;
            _executionerRetreatStarted = false;
            _executionerRetreatCompleted = false;
            _executionerStandbyRestored = true;
            RexLog.Info(
                $"Session {Request.SessionId} settled executioner {executioner.Index} " +
                $"at the original standby mark {phase}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not restore the executioner to the original standby mark.", exception);
            return false;
        }
    }

    private bool TryClearExecutionActionChannel(Agent actionAgent)
    {
        try
        {
            var executionActionResolved = _executionAction != ActionIndexCache.act_none;
            var currentMatchesExecutionAction =
                executionActionResolved &&
                actionAgent.GetCurrentAction(0) == _executionAction;
            var cleared = ExecutionActionPlaybackPolicy.HasConfirmedExecutionActionClear(
                executionActionResolved,
                currentMatchesExecutionAction);
            var needsExplicitUnresolvedPlayerClear =
                _actor == ExecutionActor.Player &&
                _playerExecutionFacingLockActive &&
                !executionActionResolved;
            cleared &= !needsExplicitUnresolvedPlayerClear;
            if (cleared)
            {
                if (_actor == ExecutionActor.Player)
                {
                    _playerExecutionActionClearedForRestore = true;
                }

                return true;
            }

            var none = ActionIndexCache.act_none;
            var accepted = actionAgent.SetActionChannel(
                0,
                in none,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart);
            currentMatchesExecutionAction = executionActionResolved &&
                                            actionAgent.GetCurrentAction(0) == _executionAction;
            if (executionActionResolved)
            {
                cleared = ExecutionActionPlaybackPolicy.HasConfirmedExecutionActionClear(
                    executionActionResolved,
                    currentMatchesExecutionAction);
            }
            else
            {
                var currentFlags = actionAgent.GetCurrentAnimationFlag(0);
                cleared = accepted &&
                          (currentFlags & AnimFlags.anf_enforce_root_rotation) == 0;
            }

            if (_actor == ExecutionActor.Player)
            {
                _playerExecutionActionClearedForRestore = cleared;
            }

            if (!accepted && !cleared)
            {
                RexLog.Warning("The execution action channel rejected its clear request.");
            }

            return cleared;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not clear the completed execution action channel.", exception);
            return false;
        }
    }

    private void ContinueAfterAnimationFailure(string detail)
    {
        RexLog.Error(
            $"Execution animation failed before the lethal frame. {detail} " +
            "The mission will remain active and the lethal flow will continue instead of cancelling.");
        if (_actor == ExecutionActor.Player && _playerAgent is not null)
        {
            if (TryClearExecutionActionChannel(_playerAgent))
            {
                RestorePlayerExecutionFacingLock();
            }
            else
            {
                RexLog.Error(
                    $"PLAYER_FACING_INVARIANT_FAILED session={Request.SessionId}: the failed player " +
                    "action did not confirm a channel-0 clear. The fixed facing lock remains active " +
                    "and cleanup will retry without cancelling the mission.");
            }
        }

        CommitLethalFrame();
    }

}
