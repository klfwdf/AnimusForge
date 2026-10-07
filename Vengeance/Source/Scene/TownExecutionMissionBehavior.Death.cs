using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RichExecutions.Campaign;
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
    private void CommitLethalFrame()
    {
        if (_lethalAttempted)
        {
            return;
        }

        try
        {
            _lethalAttempted = true;
            var validation = _service.ValidateAndStageLethalFrame(Request);
            if (!validation.IsValid)
            {
                RexLog.Error(
                    $"DEATH_INVARIANT_FAILED session={Request.SessionId}: lethal-frame validation " +
                    $"failed with {validation.Reason}: {validation.Message}. " +
                    "Campaign death is not pending and the mission remains open for diagnosis.");
                EnterPostLethalSceneState(
                    applyCrowdReaction: false,
                    aftermathMessageOverride: validation.Message);
                return;
            }

            // From this point the sentence is valid and must settle exactly once
            // when the player leaves, even if mission death/corpse presentation
            // reports an invariant failure below.
            _campaignCommitPending = true;
            TryBlockVictimConversationAfterLethalFrame();
            // Method-specific lethal-frame effects (pyre ignition, detached
            // head) are owned by the isolated execution-method strategy.
            _strategy.ApplyLethalFrameEffects();

            if (UsesStoningExecution() && !TryReleaseStoningVictimForLethalFrame())
            {
                RexLog.Error(
                    $"DEATH_INVARIANT_FAILED session={Request.SessionId}: the stoning prisoner could not " +
                    "be restored from its fivefold barrage health pool before the single lethal Blow. " +
                    "The death call will still be attempted and the mission will not auto-exit.");
            }

            // Let each method apply its mission presentation. Beheading requires
            // the dedicated single-Blow death logic; hanging and burning keep the
            // original Agent alive in their final pose until campaign death is
            // committed on mission exit.
            var visualActor = _actor == ExecutionActor.Player
                ? _playerAgent
                : _executionerAgent;
            var requiresMissionBattlefieldDeath = !_strategy.DefersVictimDeathToMissionExit;
            var deathLogicRegistered = Mission.MissionBehaviors.Contains(_deathLogic);
            if (requiresMissionBattlefieldDeath && !deathLogicRegistered)
            {
                RexLog.Error(
                    $"DEATH_INVARIANT_FAILED session={Request.SessionId}: the dedicated death logic " +
                    "is not registered in this mission. This can only occur through the legacy public " +
                    "controller constructor; the mission remains active and settlement stays deferred.");
            }

            var canApplyMethodOutcome = !requiresMissionBattlefieldDeath ||
                                        (visualActor is not null && deathLogicRegistered);
            var deathApplied = canApplyMethodOutcome && _strategy.ApplyDeath();
            _missionDeathApplied = deathApplied;
            if (!_missionDeathApplied)
            {
                RexLog.Error(
                    $"DEATH_INVARIANT_FAILED session={Request.SessionId}: the method-specific mission " +
                    "outcome was not applied. No replacement, second Blow, manual ragdoll or " +
                    "automatic mission exit will be attempted; campaign settlement remains pending.");
            }
            else if (_strategy.DefersVictimDeathToMissionExit)
            {
                RexLog.Info(
                    $"Session {Request.SessionId} froze the original prisoner Agent in the visible " +
                    $"{CurrentMethod.StringId} death pose without RegisterBlow or ragdoll; " +
                    "campaign death remains pending mission exit.");
            }
            else
            {
                RexLog.Info(
                    $"Session {Request.SessionId} confirmed the original hero CharacterObject's mission " +
                    "Agent as Killed through the dedicated single-Blow chain; the campaign hero remains " +
                    "alive and settlement is pending mission exit.");
            }

            EnterPostLethalSceneState(applyCrowdReaction: true);
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"DEATH_INVARIANT_FAILED session={Request.SessionId}: an unexpected lethal-frame " +
                "exception escaped. The mission remains active; no replacement, supplemental kill or " +
                "automatic exit will be attempted.",
                exception);
            EnterPostLethalSceneState(
                applyCrowdReaction: _campaignCommitPending,
                aftermathMessageOverride: _campaignCommitPending
                    ? null
                    : new TextObject(
                        "{=REX_Error_Unexpected}The execution failed because of an unexpected game or module error."));
        }
    }

    private bool TryProtectStoningVictim()
    {
        var victim = _victimAgent;
        if (!UsesStoningExecution() || victim is null || !victim.IsActive())
        {
            return false;
        }

        try
        {
            if (!_stoningVictimProtectionCaptured)
            {
                _stoningVictimOriginalMortality = victim.CurrentMortalityState;
                _stoningVictimOriginalHealth = victim.Health;
                _stoningVictimOriginalHealthLimit = victim.HealthLimit;
                _stoningVictimProtectionCaptured = true;
            }

            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            victim.SetMortalityState(Agent.MortalityState.Mortal);
            victim.HealthLimit = MathF.Max(1f, _stoningVictimOriginalHealthLimit * 5f);
            victim.Health = MathF.Min(
                victim.HealthLimit,
                MathF.Max(1f, _stoningVictimOriginalHealth * 5f));
            _stoningVictimProtectionReleased = false;
            MaintainStoningVictimRestraint();
            RexLog.Info(
                $"Session {Request.SessionId} gave the kneeling stoning prisoner an effective fivefold health pool: " +
                $"mortality={_stoningVictimOriginalMortality}->Mortal, " +
                $"health={_stoningVictimOriginalHealth:0.0}->{victim.Health:0.0}, " +
                $"limit={_stoningVictimOriginalHealthLimit:0.0}->{victim.HealthLimit:0.0}. Real stone hits remain visible and lethal.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not prepare the stoning prisoner's fivefold health pool.",
                exception);
            return false;
        }
    }

    private void MaintainStoningVictimRestraint()
    {
        var victim = _victimAgent;
        if (!UsesStoningExecution() || _lethalAttempted ||
            victim is null || !victim.IsActive() || _sceneAct is null || _placement is null)
        {
            return;
        }

        try
        {
            if (!_stoningVictimProtectionCaptured && !TryProtectStoningVictim())
            {
                return;
            }

            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            var expectedDirection = _placement.Forward.IsNonZero()
                ? _placement.Forward.Normalized()
                : Vec2.Forward;
            if (victim.Position.Distance(_victimPosition) > 0.025f)
            {
                PoseAgentAt(victim, _victimPosition, expectedDirection);
            }
            else
            {
                victim.LookDirection = new Vec3(expectedDirection.x, expectedDirection.y, 0f);
            }

            if (_victimIdleAction == ActionIndexCache.act_none &&
                !TryResolveAndPrefetchAction(
                    victim,
                    _sceneAct.VictimIdleAction,
                    "stoning prisoner permanent kneeling idle",
                    out _victimIdleAction))
            {
                return;
            }

            if (victim.GetCurrentAction(0) == _victimIdleAction &&
                victim.GetActionChannelWeight(0) > 0.01f)
            {
                return;
            }

            TrySetPreparedAction(
                victim,
                _victimIdleAction,
                _sceneAct.VictimIdleAction,
                "stoning prisoner permanent kneeling idle after hit",
                forceFullBody: true);
        }
        catch (Exception exception)
        {
            if (!_stoningVictimRestraintFailureLogged)
            {
                _stoningVictimRestraintFailureLogged = true;
                RexLog.Error(
                    $"Session {Request.SessionId} could not maintain the stoning prisoner's kneeling restraint; " +
                    "later ticks will retry without ending the mission.",
                    exception);
            }
        }
    }

    private bool TryReleaseStoningVictimForLethalFrame()
    {
        if (!UsesStoningExecution() || _stoningVictimProtectionReleased)
        {
            return true;
        }

        var victim = _victimAgent;
        if (victim is null || !victim.IsActive())
        {
            _stoningVictimProtectionReleased = true;
            RexLog.Info(
                $"Session {Request.SessionId} observed the stoning prisoner already dead before the shared lethal frame; " +
                "no supplemental mission kill is required and campaign settlement remains single-shot on exit.");
            return true;
        }

        try
        {
            var scaledRemainingHealth = victim.Health / 5f;
            victim.HealthLimit = MathF.Max(1f, _stoningVictimOriginalHealthLimit);
            victim.Health = MathF.Max(
                1f,
                MathF.Min(victim.HealthLimit, scaledRemainingHealth));
            victim.SetMortalityState(Agent.MortalityState.Mortal);
            _stoningVictimProtectionReleased = true;
            RexLog.Info(
                $"Session {Request.SessionId} restored the stoning prisoner's native health scale " +
                $"at the shared lethal frame (health={victim.Health:0.0}, limit={victim.HealthLimit:0.0}, mortality=Mortal).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not restore the stoning prisoner's native health scale.",
                exception);
            return false;
        }
    }

    private bool RestoreStoningVictimProtection()
    {
        if (!_stoningVictimProtectionCaptured)
        {
            return true;
        }

        var victim = _victimAgent;
        if (victim is null || !victim.IsActive() || _missionDeathApplied)
        {
            return true;
        }

        try
        {
            victim.HealthLimit = MathF.Max(1f, _stoningVictimOriginalHealthLimit);
            victim.Health = MathF.Min(_stoningVictimOriginalHealth, victim.HealthLimit);
            victim.SetMortalityState(_stoningVictimOriginalMortality);
            victim.SetIsAIPaused(false);
            _stoningVictimProtectionCaptured = false;
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not restore the uncommitted stoning prisoner.",
                exception);
            return false;
        }
    }
    private void MaintainCrossbowVictimRestraint()
    {

        if (!UsesCrossbowExecution() ||
            _deathLogic.HasObservedVictimKilled ||
            _victimAgent is null || !_victimAgent.IsActive() ||
            _placement is null || !_victimPosition.IsValid)
        {
            return;
        }

        try
        {
            var victim = _victimAgent;
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            victim.SetIsPhysicsForceClosed(false);
            victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);

            var forward = _placement.Forward.IsNonZero()
                ? _placement.Forward.Normalized()
                : Vec2.Forward;
            var direction = new Vec3(forward.x, forward.y, 0f);
            victim.SetTargetZ(_victimPosition.z);
            victim.SetTargetPositionAndDirection(_victimPosition.AsVec2, direction);
            var targetUp = Vec3.Up;
            victim.SetTargetUp(in targetUp);

            // The beam is authoritative. Do not read live hand bones and do not
            // move the beam to whatever pose the previous animation produced.
            // That feedback loop was the source of the raised/wrong hand IK.
            var handAnchorRight = GetCrossbowHandAnchorRight(in direction);
            var handAnchorCenter = _victimPosition + new Vec3(0f, 0f, CrossbowHandAnchorHeight);
            var handAnchorHalfSpan = 0.62f;
            if (_burningCrossbeamEntity is not null)
            {
                var beamFrame = _burningCrossbeamEntity.GetGlobalFrame();
                handAnchorCenter = beamFrame.origin;
                handAnchorRight = GetCrossbowHandAnchorRight(in direction);
                handAnchorHalfSpan = GetCrossbowHandAnchorHalfSpan(
                    _burningCrossbeamEntity,
                    handAnchorCenter,
                    handAnchorRight);

                if (!_crossbowHandAnchorSourceLogged)
                {
                    _crossbowHandAnchorSourceLogged = true;
                    RexLog.Info(
                        $"Session {Request.SessionId} bound both ceremony hands to the fixed crossbeam " +
                        $"at ({handAnchorCenter.x:0.000},{handAnchorCenter.y:0.000},{handAnchorCenter.z:0.000}), " +
                        $"lateralAxis=({handAnchorRight.x:0.000},{handAnchorRight.y:0.000}), " +
                        $"halfSpan={handAnchorHalfSpan:0.000}m; live hand bones are not used as anchors.");
                }
            }

            // SetHandInverseKinematicsFrame takes left first, right second.
            // The beam axis has already been normalized against the victim's
            // authored right axis, so negative is always the victim's left.
            var rotation = Mat3.CreateMat3WithForward(in direction);
            var leftHandFrame = new MatrixFrame(
                rotation,
                handAnchorCenter - handAnchorRight * handAnchorHalfSpan);
            var rightHandFrame = new MatrixFrame(
                rotation,
                handAnchorCenter + handAnchorRight * handAnchorHalfSpan);
            if (!victim.SetHandInverseKinematicsFrame(in leftHandFrame, in rightHandFrame) &&
                !_crossbowVictimRestraintFailureLogged)
            {
                _crossbowVictimRestraintFailureLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} could not retain both fixed crossbeam hand IK anchors; " +
                    "root, feet and gravity restraint remain active.");
            }
        }
        catch (Exception exception)
        {
            if (!_crossbowVictimRestraintFailureLogged)
            {
                _crossbowVictimRestraintFailureLogged = true;
                RexLog.Error(
                    $"Session {Request.SessionId} could not maintain fixed crossbeam hand IK; " +
                    "later ticks will retry without ending the mission.",
                    exception);
            }
        }
    }
    private Vec3 GetCrossbowHandAnchorRight(in Vec3 direction)
    {
        var fallback = _placement is not null && _placement.Right.IsNonZero()
            ? new Vec3(_placement.Right.x, _placement.Right.y, 0f)
            : new Vec3(direction.y, -direction.x, 0f);
        if (fallback.IsNonZero)
        {
            fallback.Normalize();
        }
        var beam = _burningCrossbeamEntity;
        if (beam is null)
        {
            return fallback;
        }

        try
        {
            var frame = beam.GetGlobalFrame();
            var candidates = new[] { frame.rotation.s, frame.rotation.f };
            var best = fallback;
            var bestAbsDot = 0f;
            foreach (var candidate in candidates)
            {
                var horizontal = new Vec3(candidate.x, candidate.y, 0f);
                if (!horizontal.IsNonZero)
                {
                    continue;
                }

                horizontal.Normalize();
                var dot = horizontal.x * fallback.x + horizontal.y * fallback.y;
                if (MathF.Abs(dot) > bestAbsDot)
                {
                    bestAbsDot = MathF.Abs(dot);
                    best = dot < 0f ? -horizontal : horizontal;
                }
            }

            return best;
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Session {Request.SessionId} could not read the crossbow crossbeam axis; " +
                $"falling back to the placement right axis ({exception.Message}).");
            return fallback;
        }
    }

    private float GetCrossbowHandAnchorHalfSpan(
        GameEntity beam,
        Vec3 center,
        Vec3 handAnchorRight)
    {
        try
        {
            var bounds = beam.GetGlobalBoundingBox();
            var maximumProjection = 0f;
            for (var index = 0; index < 8; index++)
            {
                var projection = MathF.Abs(Vec3.DotProduct(bounds[index] - center, handAnchorRight));
                maximumProjection = MathF.Max(maximumProjection, projection);
            }

            return MathF.Max(
                0.35f,
                MathF.Min(
                    CrossbowHandAnchorMaximumHalfSpan,
                    maximumProjection - CrossbowHandAnchorMinimumInset));
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Session {Request.SessionId} could not project crossbow beam bounds for hand IK; " +
                $"using the 0.62m fallback ({exception.Message}).");
            return 0.62f;
        }
    }
    private bool TryPrepareCrossbowVictimForCeremonialSuspension()
    {
        if (!UsesCrossbowExecution())
        {
            return true;
        }

        if (_crossbowCeremonialSuspensionArmed)
        {
            return true;
        }

        var victim = _victimAgent;
        if (victim is null || !victim.IsActive() || _deathLogic.HasObservedVictimKilled)
        {
            return false;
        }

        try
        {
            // Before the ceremony the victim remains Mortal so a player who
            // picks up the ground crossbow and kills the prisoner directly is
            // still observed by the real external-Killed settlement path. Once
            // the sentence starts, switch only the visible mission Agent to an
            // invulnerable restrained presentation so native ragdoll/corpse
            // cleanup cannot pull it off the crucifix.
            if (_crossbowDeathOverrideApplied)
            {
                var none = ActionIndexCache.act_none;
                victim.SetOverridenStrikeAndDeathAction(in none, in none);
                _crossbowDeathOverrideApplied = false;
            }

            victim.SetMortalityState(Agent.MortalityState.Invulnerable);
            victim.Health = victim.HealthLimit;
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            _crossbowCeremonialSuspensionArmed = true;
            MaintainCrossbowVictimRestraint();
            RexLog.Info(
                $"Session {Request.SessionId} armed deferred crossbow crucifix presentation for original " +
                $"prisoner Agent {victim.Index}: mortality=Invulnerable, health={victim.Health:0.0}; " +
                "a real pre-ceremony Killed event remains supported, while the formal volley will freeze " +
                "the living Agent and commit campaign execution on mission exit.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not arm the deferred crossbow crucifix presentation.",
                exception);
            return false;
        }
    }

    private bool RestoreCrossbowVictimRestraint()
    {
        var victim = _victimAgent;
        if (!UsesCrossbowExecution() || victim is null || !victim.IsActive() ||
            _deathLogic.HasObservedVictimKilled)
        {
            return true;
        }

        try
        {
            victim.ClearHandInverseKinematics();
            victim.ClearTargetFrame();
            var zeroUp = Vec3.Zero;
            victim.SetTargetUp(in zeroUp);
            victim.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: false);
            victim.SetIsPhysicsForceClosed(false);
            victim.SetIsAIPaused(false);
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not restore the uncommitted crossbow prisoner's restraint.",
                exception);
            return false;
        }
    }

    private bool TryPrepareCrossbowVictimForRealCombatDeath()
    {
        var victim = _victimAgent;
        if (!UsesCrossbowExecution() || victim is null || !victim.IsActive())
        {
            return false;
        }

        if (_crossbowDeathEnvironmentCaptured)
        {
            return true;
        }

        try
        {
            _crossbowOriginalDisableDying = Mission.DisableDying;
            _crossbowVictimOriginalMortality = victim.CurrentMortalityState;
            _crossbowVictimOriginalHealth = victim.Health;
            _crossbowDeathEnvironmentCaptured = true;

            Mission.DisableDying = false;
            victim.SetMortalityState(Agent.MortalityState.Mortal);
            victim.Health = MathF.Min(1f, victim.HealthLimit);

            const string deathActionName = "act_ver_burning_crucifix_death";
            if (!TryResolveAndPrefetchAction(
                    victim,
                    deathActionName,
                    "crossbow crucifix static death override",
                    out var deathAction))
            {
                TryRestoreCrossbowDeathEnvironment();
                return false;
            }

            victim.SetOverridenStrikeAndDeathAction(in deathAction, in deathAction);
            _crossbowDeathOverrideApplied = true;
            if (!_deathLogic.TryArmExternalVictimDeath())
            {
                TryRestoreCrossbowDeathEnvironment();
                return false;
            }

            RexLog.Info(
                $"Session {Request.SessionId} armed the original crossbow prisoner for real missile death: " +
                $"Mission.DisableDying {_crossbowOriginalDisableDying}->false, " +
                $"mortality {_crossbowVictimOriginalMortality}->Mortal, " +
                $"health {_crossbowVictimOriginalHealth:0.0}->{victim.Health:0.0}, " +
                $"deathAction={deathActionName}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not arm the crossbow prisoner for a real native hit.",
                exception);
            TryRestoreCrossbowDeathEnvironment();
            return false;
        }
    }

    private bool TryStageObservedProjectileVictimDeath(string source, bool updateScene = true)
    {
        if ((!UsesCrossbowExecution() && !UsesStoningExecution()) ||
            _projectileExternalDeathStaged ||
            !_deathLogic.HasObservedVictimKilled)
        {
            return false;
        }

        try
        {
            _projectileExternalDeathStaged = true;
            if (updateScene && UsesCrossbowExecution())
            {
                TryRestoreCrossbowMissionDyingFlag();
            }
            if (_actor == ExecutionActor.Undecided)
            {
                // The only pre-ceremony manual lethal route is the player's own
                // projectile hit. Preserve a concrete settlement actor instead
                // of leaving the request in Undecided.
                _actor = ExecutionActor.Player;
            }

            if (_lethalAttempted)
            {
                _missionDeathApplied = true;
                RexLog.Info(
                    $"Session {Request.SessionId} observed the real projectile death during an already staged lethal flow ({source}).");
                return true;
            }

            _lethalAttempted = true;
            var validation = _service.ValidateAndStageLethalFrame(Request);
            if (!validation.IsValid)
            {
                RexLog.Error(
                    $"DEATH_INVARIANT_FAILED session={Request.SessionId}: the original projectile prisoner was Killed " +
                    $"but lethal-frame validation failed with {validation.Reason}: {validation.Message}. " +
                    "No duplicate Blow or automatic mission exit is attempted.");
                return true;
            }

            _campaignCommitPending = true;
            _missionDeathApplied = true;
            if (!updateScene)
            {
                // Mission teardown may arrive without another tick. Preserve the
                // validated outcome without touching native visuals or UI after end.
                return true;
            }
            TryBlockVictimConversationAfterLethalFrame();
            _strategy.ApplyLethalFrameEffects();
            _usePoint?.SetIsDeactivatedSynched(true);
            RemoveConsumedUsePointMarker();

            if (State == ExecutionSessionState.WaitingForPlayer)
            {
                _stateMachine.TransitionTo(ExecutionSessionState.CeremonyPreparation);
            }

            if (State == ExecutionSessionState.CeremonyPreparation)
            {
                _stateMachine.TransitionTo(ExecutionSessionState.Execution);
            }

            EnterPostLethalSceneState(applyCrowdReaction: true);
            RexLog.Info(
                $"Session {Request.SessionId} staged campaign settlement after observing the original projectile " +
                $"prisoner Killed by a real native hit ({source}); no synthetic Blow or method ApplyDeath call was used.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"DEATH_INVARIANT_FAILED session={Request.SessionId}: projectile Killed observation " +
                $"failed while staging mission-exit settlement ({source}). The mission remains active " +
                "and no duplicate Blow or automatic exit is attempted.",
                exception);
            return true;
        }
    }

    private bool TryRestoreCrossbowMissionDyingFlag()
    {
        if (!_crossbowDeathEnvironmentCaptured)
        {
            return true;
        }

        try
        {
            Mission.DisableDying = _crossbowOriginalDisableDying;
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not restore Mission.DisableDying after crossbow death.",
                exception);
            return false;
        }
    }

    private bool TryRestoreCrossbowDeathEnvironment()
    {
        if (!_crossbowDeathEnvironmentCaptured)
        {
            return true;
        }

        var restored = TryRestoreCrossbowMissionDyingFlag();
        var victim = _victimAgent;
        if (victim is not null &&
            victim.IsActive() &&
            !_deathLogic.HasObservedVictimKilled)
        {
            try
            {
                if (_crossbowDeathOverrideApplied)
                {
                    var none = ActionIndexCache.act_none;
                    victim.SetOverridenStrikeAndDeathAction(in none, in none);
                }

                victim.SetMortalityState(_crossbowVictimOriginalMortality);
                victim.Health = MathF.Min(
                    _crossbowVictimOriginalHealth,
                    victim.HealthLimit);
            }
            catch (Exception exception)
            {
                restored = false;
                RexLog.Error(
                    $"Session {Request.SessionId} could not restore the uncommitted crossbow prisoner's combat state.",
                    exception);
            }
        }

        if (restored)
        {
            _crossbowDeathEnvironmentCaptured = false;
            _crossbowDeathOverrideApplied = false;
            _crossbowCeremonialSuspensionArmed = false;
        }

        return restored;
    }

    private void EnterPostLethalSceneState(
        bool applyCrowdReaction,
        TextObject? aftermathMessageOverride = null)
    {
        _postLethalExecutionActionPending = _executionActionPlaying;
        _aftermathMessageOverride = aftermathMessageOverride;
        if (State == ExecutionSessionState.Execution &&
            _stateMachine.CanTransitionTo(ExecutionSessionState.CrowdReaction))
        {
            _stateMachine.TransitionTo(ExecutionSessionState.CrowdReaction);
        }

        _stateElapsed = 0f;
        if (applyCrowdReaction)
        {
            ApplyCrowdReaction();
        }

        if (!_postLethalExecutionActionPending)
        {
            SetCeremonyAlliesFormationControl(enabled: true, "post-lethal presentation completed");
        }
    }

    private bool TryBlockVictimConversationAfterLethalFrame()
    {
        if (_victimConversationBlockAttempted)
        {
            return _victimConversationBlockApplied;
        }

        _victimConversationBlockAttempted = true;
        var victim = _victimAgent;
        // Frozen-pose methods leave the Agent active; hosts use this mark to
        // stop treating the executed prisoner as a living shout target.
        VengeanceIntegration.MarkExecutedVictim(victim);
        if (_conversationLogic is null || victim is null)
        {
            RexLog.Error(
                $"CONVERSATION_INVARIANT_FAILED session={Request.SessionId}: " +
                "the native conversation logic or original victim Agent was unavailable; " +
                "conversation remains disabled for this mission as a fallback.");
            return false;
        }

        try
        {
            // v1.4.8 SandBox.MissionConversationLogic stores the exact exclusion
            // set in this private field.  Keep the reflection narrow and verify
            // both the declaring type and the expected collection shape before
            // mutating it; this avoids affecting unrelated mission behaviors.
            var field = typeof(MissionConversationLogic).GetField(
                "_uninteractableAgents",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(_conversationLogic) is not ICollection<Agent> agents)
            {
                RexLog.Error(
                    $"CONVERSATION_INVARIANT_FAILED session={Request.SessionId}: " +
                    "native _uninteractableAgents field was not a mutable list; " +
                    "conversation remains disabled for this mission as a fallback.");
                return false;
            }

            if (!agents.Contains(victim))
            {
                agents.Add(victim);
            }

            _victimConversationBlockApplied = agents.Contains(victim);
            RexLog.Info(
                $"Session {Request.SessionId} blocked post-execution F interaction for victim Agent " +
                $"{victim.Index}: applied={_victimConversationBlockApplied}.");
            return _victimConversationBlockApplied;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"CONVERSATION_INVARIANT_FAILED session={Request.SessionId}: could not add victim Agent " +
                "to the native per-Agent conversation exclusion set; conversation remains disabled " +
                "for this mission as a fallback.",
                exception);
            return false;
        }
    }

    private void PlayLethalEffects()
    {
        if (_victimAgent is null)
        {
            return;
        }

        try
        {
            if (string.Equals(CurrentMethod.StringId, "beheading", StringComparison.OrdinalIgnoreCase))
            {
                var neck = _victimAgent.GetEyeGlobalPosition();
                neck.z -= 0.18f;
                var frame = MatrixFrame.Identity;
                frame.origin = neck;
                Mission.AddParticleSystemBurstByName(
                    "psys_game_blood_sword_enter",
                    frame,
                    synchThroughNetwork: false);
                _victimAgent.CreateBloodBurstAtLimb(_victimAgent.Monster.NeckRootBoneIndex, 1.2f);
                TryCreateDetachedHeadVisual();
                Mission.MakeSound(
                    CombatSoundContainer.SoundCodeMissionCombatCutHigh,
                    neck,
                    soundCanBePredicted: false,
                    isReliable: true,
                    _actor == ExecutionActor.Player ? _playerAgent?.Index ?? -1 : _executionerAgent?.Index ?? -1,
                    _victimAgent.Index);
            }
        }
        catch (Exception exception)
        {
            RexLog.Error("A lethal visual or sound effect failed; campaign commit continues.", exception);
        }
    }

    private void TryCreateDetachedHeadVisual()
    {
        if (_detachedHeadVisual is not null || _victimAgent is null)
        {
            return;
        }

        var stageSurfaceWorldZ = _gallowsDeckBoundsMax.IsValid
            ? _gallowsDeckBoundsMax.z
            : float.NaN;
        if (!DetachedHeadVisual.TryCreate(
                Mission,
                _victimAgent,
                stageSurfaceWorldZ,
                out var detachedHead) ||
            detachedHead is null)
        {
            return;
        }

        _detachedHeadVisual = detachedHead;
        _spawnedEntities.Add(detachedHead.Entity);
    }

    private void RestoreDetachedHeadSourceIfExecutionWasNotCommitted()
    {
        if (_detachedHeadVisual is null || _missionDeathApplied || _outcome?.DeathCommitted == true)
        {
            return;
        }

        _detachedHeadVisual.RestoreSourceHead();
    }

    private bool IgniteExecutionEffects()
    {
        if (_visualProfile?.IgniteAtLethalFrame != true)
        {
            return true;
        }

        return TryIgniteExecutionEffectRoles(
            "lethal-frame completion",
            _visualProfile.IgnitionEffects
                .Select(effect => effect.RoleId)
                .ToArray());
    }

    private bool TryIgniteExecutionEffectRoles(string stage, params string[] roleIds)
    {
        var visualProfile = _visualProfile;
        if (visualProfile?.IgniteAtLethalFrame != true)
        {
            RexLog.Warning(
                $"Method '{CurrentMethod.StringId}' requested ignition stage '{stage}' without an ignition profile.");
            return false;
        }

        var spawnedCount = 0;
        var newlyAttemptedCount = 0;
        foreach (var roleId in roleIds)
        {
            if (string.IsNullOrWhiteSpace(roleId) || !_ignitionRolesAttempted.Add(roleId))
            {
                continue;
            }

            newlyAttemptedCount++;
            var ignitionEffect = visualProfile.IgnitionEffects.FirstOrDefault(
                candidate => string.Equals(
                    candidate.RoleId,
                    roleId,
                    StringComparison.OrdinalIgnoreCase));
            if (ignitionEffect is null)
            {
                RexLog.Warning(
                    $"Ignition stage '{stage}' requested unknown role '{roleId}'; remaining roles continue.");
                continue;
            }

            try
            {
                var spawned = false;
                if (TryGetVictimFireBone(ignitionEffect.RoleId, out var victimBone) &&
                    TryAttachVictimFireParticle(victimBone, ignitionEffect.RoleId))
                {
                    spawned = true;
                }
                else if (SpawnProfileProp(ignitionEffect) is not null)
                {
                    // If skeleton attachment is unavailable, retain the
                    // profile's world-space native fire prefab as a fallback.
                    spawned = true;
                }

                if (spawned)
                {
                    spawnedCount++;
                    _ignitionRolesSucceeded.Add(ignitionEffect.RoleId);
                }
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"Ignition visual '{ignitionEffect.RoleId}' failed; remaining effects and campaign commit continue.",
                    exception);
            }
        }

        _ignitionSucceeded = _ignitionRolesSucceeded.Count > 0;
        if (newlyAttemptedCount == 0)
        {
            RexLog.Info(
                $"Session {Request.SessionId} ignition stage '{stage}' was already complete; " +
                $"successfulRoles={_ignitionRolesSucceeded.Count}.");
        }
        else if (spawnedCount == 0)
        {
            RexLog.Warning(
                $"Method '{CurrentMethod.StringId}' ignition stage '{stage}' spawned no new native fire effect " +
                $"(attempted={newlyAttemptedCount}, successfulTotal={_ignitionRolesSucceeded.Count}).");
        }
        else
        {
            RexLog.Info(
                $"Session {Request.SessionId} ignition stage '{stage}' started {spawnedCount} native fire effects " +
                $"(successfulTotal={_ignitionRolesSucceeded.Count}).");
        }

        return _ignitionSucceeded;
    }

    private bool TryPlayWorldParticleBurst(
        string particleSystemName,
        Vec3 position,
        string purpose)
    {
        if (string.IsNullOrWhiteSpace(particleSystemName) || !position.IsValid)
        {
            RexLog.Warning(
                $"Could not play world particle burst for {purpose}; its resource or position was invalid.");
            return false;
        }

        try
        {
            var runtimeId = ParticleSystemManager.GetRuntimeIdByName(particleSystemName);
            if (runtimeId < 0)
            {
                RexLog.Warning(
                    $"Could not resolve world particle '{particleSystemName}' for {purpose}.");
                return false;
            }

            var frame = MatrixFrame.Identity;
            frame.origin = position;
            Mission.AddParticleSystemBurstByName(
                particleSystemName,
                frame,
                synchThroughNetwork: false);
            RexLog.Info(
                $"Session {Request.SessionId} played world particle '{particleSystemName}' for {purpose} " +
                $"at ({position.x:0.00},{position.y:0.00},{position.z:0.00}).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not play world particle '{particleSystemName}' for {purpose}; the ceremony continues.",
                exception);
            return false;
        }
    }

    private bool ReleaseExecutionActorControl(string purpose)
    {
        var actionAgent = _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;
        if (actionAgent is null || !actionAgent.IsActive())
        {
            _executionActionPlaying = false;
            _postLethalExecutionActionPending = false;
            if (_actor == ExecutionActor.Player)
            {
                RestorePlayerExecutionState();
                return _playerExecutionStateRestored;
            }

            return true;
        }

        if (!TryClearExecutionActionChannel(actionAgent))
        {
            RexLog.Warning(
                $"Session {Request.SessionId} could not yet release the execution actor after {purpose}; " +
                "the clear will retry without reasserting the completed action mark.");
            return false;
        }

        _executionActionPlaying = false;
        _executionActionAdvanced = false;
        _postLethalExecutionActionPending = false;
        if (_actor == ExecutionActor.Player)
        {
            RestorePlayerExecutionState();
            if (!_playerExecutionStateRestored)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} cleared the player staged execution action after {purpose}, " +
                    "but equipment or facing restoration still needs another tick.");
                return false;
            }
        }

        RexLog.Info(
            $"Session {Request.SessionId} released {_actor} movement after {purpose}; " +
            "channel 0 will no longer be pinned to the ignition mark.");
        return true;
    }

    private static bool TryGetVictimFireBone(string roleId, out HumanBone bone)
    {
        switch (roleId)
        {
            case "pyre_fire_left_leg":
                bone = HumanBone.CalfL;
                return true;
            case "pyre_fire_right_leg":
                bone = HumanBone.CalfR;
                return true;
            case "pyre_fire_left_thigh":
                bone = HumanBone.ThighL;
                return true;
            case "pyre_fire_right_thigh":
                bone = HumanBone.ThighR;
                return true;
            case "pyre_fire_waist":
                bone = HumanBone.Abdomen;
                return true;
            case "pyre_fire_torso":
                bone = HumanBone.Thorax;
                return true;
            default:
                bone = HumanBone.Invalid;
                return false;
        }
    }

    private bool TryAttachVictimFireParticle(HumanBone bone, string roleId)
    {
        var visuals = _victimAgent?.AgentVisuals;
        if (visuals is null || !visuals.IsValid())
        {
            return false;
        }

        try
        {
            var realBoneIndex = visuals.GetRealBoneIndex(bone);
            if (realBoneIndex < 0)
            {
                RexLog.Warning(
                    $"Victim fire '{roleId}' could not resolve bone {bone}.");
                return false;
            }

            var particleSystemIndex = ParticleSystemManager.GetRuntimeIdByName(
                VictimFireParticleSystem);
            if (particleSystemIndex < 0)
            {
                RexLog.Warning(
                    $"Victim fire '{roleId}' could not resolve the known torch flame " +
                    $"'{VictimFireParticleSystem}'.");
                return false;
            }

            var attachedCount = 0;
            for (var index = 0; index < VictimFireLocalOffsets.Length; index++)
            {
                try
                {
                    var localFrame = MatrixFrame.Identity;
                    localFrame.origin = VictimFireLocalOffsets[index];
                    visuals.CreateParticleSystemAttachedToBone(
                        particleSystemIndex,
                        realBoneIndex,
                        ref localFrame);
                    attachedCount++;
                }
                catch (Exception exception)
                {
                    RexLog.Error(
                        $"Victim fire '{roleId}' could not attach copy {index + 1}/" +
                        $"{VictimFireLocalOffsets.Length} of '{VictimFireParticleSystem}' to bone {bone}; " +
                        "remaining copies continue.",
                        exception);
                }
            }

            if (attachedCount > 0)
            {
                RexLog.Info(
                    $"Session {Request.SessionId} attached {attachedCount}/" +
                    $"{VictimFireLocalOffsets.Length} offset victim fire instances for '{roleId}' " +
                    $"to {bone} with known-loadable '{VictimFireParticleSystem}'.");
            }

            return attachedCount > 0;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Victim fire '{roleId}' could not prepare bone {bone}; using its world-space fallback.",
                exception);
            return false;
        }
    }

    private bool TryStartStoningFirstThrow(
        Agent thrower,
        bool preservePlayerControl)
    {
        if (!UsesStoningExecution() || !thrower.IsActive())
        {
            return false;
        }

        try
        {
            if (!TryEquipAdditionalCeremonyWeapon(
                    thrower,
                    preservePlayerControl
                        ? "player stoning first throw"
                        : "executioner stoning first throw"))
            {
                return false;
            }

            if (!preservePlayerControl)
            {
                // The executioner was spawned at the screenshot-authored blue
                // box and remains there. Only rotate the visible body toward
                // the prisoner; never resolve or teleport to a later mark.
                var lookDirection = GetDirectionToVictim(thrower.Position);
                thrower.LookDirection = new Vec3(
                    lookDirection.x,
                    lookDirection.y,
                    0f);
                PrepareNativeRangedCombatTarget(
                    thrower,
                    holdFire: true,
                    "executioner first-stone aim");
                if (!TryPlayAction(
                        thrower,
                        "act_ready_stone",
                        "executioner native first-stone aim from permanent entry mark"))
                {
                    return false;
                }
            }
            else
            {
                if (!TryPlayPlayerRangedOverlay(
                        thrower,
                        "act_ready_stone",
                        "player free-movement native first-stone aim"))
                {
                    return false;
                }
            }

            var throwNumber = _stoningCrowdThrowCount + 1;
            _stoningPendingProjectileThrower = thrower;
            _stoningPendingProjectileElapsed = StoningAimSeconds;
            _stoningPendingProjectileNumber = throwNumber;
            _stoningPendingReleaseStarted = false;
            _stoningPendingPreservePlayerControl = preservePlayerControl;
            _stoningCrowdThrowCount = throwNumber;
            RexLog.Info(
                $"Session {Request.SessionId} began native ready/hold/release stoning throw {throwNumber} from agent " +
                $"{thrower.Index}; preservePlayerControl={preservePlayerControl}, " +
                $"executionerPlayerEntrySpawn={(!preservePlayerControl)}, aim={StoningAimSeconds:0.00}s.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                preservePlayerControl
                    ? "The player's free-movement first stone could not be started."
                    : "The executioner's blue-box first stone could not be started.",
                exception);
            return false;
        }
    }

    private void TickStoningCrowdThrows(float dt)
    {
        TickPendingStoningProjectile(dt);
        if (!UsesStoningExecution() || _lethalAttempted ||
            State != ExecutionSessionState.Execution)
        {
            return;
        }

        if (_stoningPendingProjectileThrower is not null)
        {
            return;
        }

        _stoningNextCrowdThrowElapsed -= MathF.Max(0f, dt);
        if (_stoningNextCrowdThrowElapsed > 0f)
        {
            return;
        }

        _stoningNextCrowdThrowElapsed = StoningMinimumThrowIntervalSeconds +
                                        MBRandom.RandomFloat * StoningThrowIntervalRangeSeconds;
        var activeThrowers = _crowdAgents
            .Where(agent => agent is not null && agent.IsActive())
            .Concat(
                _executionerAgent is not null && _executionerAgent.IsActive()
                    ? new[] { _executionerAgent }
                    : Array.Empty<Agent>())
            .Distinct()
            .ToArray();
        if (activeThrowers.Length == 0)
        {
            return;
        }

        var startIndex = Math.Min(
            activeThrowers.Length - 1,
            (int)(MBRandom.RandomFloat * activeThrowers.Length));
        Agent? thrower = null;
        for (var offset = 0; offset < activeThrowers.Length; offset++)
        {
            var candidate = activeThrowers[(startIndex + offset) % activeThrowers.Length];
            if (candidate.IsActive())
            {
                thrower = candidate;
                break;
            }
        }

        if (thrower is null)
        {
            return;
        }

        try
        {
            thrower.DisableScriptedMovement();
            thrower.Controller = AgentControllerType.None;
            thrower.ClearTargetFrame();
            var lookDirection = _victimPosition.AsVec2 - thrower.Position.AsVec2;
            if (lookDirection.IsNonZero())
            {
                lookDirection = lookDirection.Normalized();
                thrower.LookDirection = new Vec3(lookDirection.x, lookDirection.y, 0f);
            }

            var throwNumber = _stoningCrowdThrowCount + 1;
            if (!TryEquipAdditionalCeremonyWeapon(
                    thrower,
                    $"stoning crowd thrower {throwNumber}"))
            {
                RexLog.Warning(
                    $"Stoning public thrower {thrower.Index} could not take a fresh native stone; " +
                    "the randomized barrage continues with another thrower.");
                return;
            }

            PrepareNativeRangedCombatTarget(
                thrower,
                holdFire: true,
                $"stoning public aim {throwNumber}");
            if (!TryPlayAction(
                    thrower,
                    "act_ready_stone",
                    $"stoning crowd native aim {throwNumber}"))
            {
                RexLog.Warning(
                    $"Stoning public thrower {thrower.Index} could not bind 'act_ready_stone'; " +
                    "the randomized barrage continues.");
                return;
            }

            _stoningPendingProjectileThrower = thrower;
            _stoningPendingProjectileElapsed = StoningAimSeconds;
            _stoningPendingProjectileNumber = throwNumber;
            _stoningPendingReleaseStarted = false;
            _stoningPendingPreservePlayerControl = false;
            _stoningCrowdThrowCount = throwNumber;
            RexLog.Info(
                $"Session {Request.SessionId} stoning public throw {_stoningCrowdThrowCount}: " +
                $"agent={thrower.Index}, executioner={ReferenceEquals(thrower, _executionerAgent)}, " +
                $"nextInterval={_stoningNextCrowdThrowElapsed:0.00}s.");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Stoning public thrower {thrower.Index} failed during its randomized throw.",
                exception);
        }
    }

    private void TickPendingStoningProjectile(float dt)
    {
        var thrower = _stoningPendingProjectileThrower;
        if (thrower is null)
        {
            return;
        }

        _stoningPendingProjectileElapsed -= MathF.Max(0f, dt);
        if (_stoningPendingProjectileElapsed > 0f)
        {
            return;
        }

        if (!_stoningPendingReleaseStarted)
        {
            _stoningPendingReleaseStarted = true;
            _stoningPendingProjectileElapsed = StoningProjectileReleaseDelaySeconds;
            var releaseStarted = _stoningPendingPreservePlayerControl
                ? TryPlayPlayerRangedOverlay(
                    thrower,
                    "act_release_stone",
                    "player free-movement native stone release")
                : TryPlayAction(
                    thrower,
                    "act_release_stone",
                    "native stoning combat release");
            PrepareNativeRangedCombatTarget(
                thrower,
                holdFire: false,
                "native stone release");
            if (!releaseStarted)
            {
                RexLog.Warning(
                    $"Stoning thrower {thrower.Index} could not bind the native release action; " +
                    "the already-aimed real stone will still launch after the release delay.");
            }

            return;
        }

        var throwNumber = _stoningPendingProjectileNumber;
        _stoningPendingProjectileThrower = null;
        _stoningPendingProjectileElapsed = 0f;
        _stoningPendingProjectileNumber = 0;
        _stoningPendingReleaseStarted = false;
        _stoningPendingPreservePlayerControl = false;
        if (_lethalAttempted || State != ExecutionSessionState.Execution || !thrower.IsActive())
        {
            return;
        }

        TryLaunchStoningProjectile(thrower, throwNumber);
    }

    private bool TryPlayPlayerRangedOverlay(Agent player, string actionName, string purpose)
    {
        if (!TryResolveAndPrefetchAction(player, actionName, purpose, out var action))
        {
            return false;
        }

        try
        {
            var none = ActionIndexCache.act_none;
            player.SetActionChannel(
                1,
                in none,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart);
            return player.SetActionChannel(
                1,
                in action,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart,
                blendWithNextActionFactor: 0f,
                actionSpeed: 1f,
                blendInPeriod: -0.2f,
                blendOutPeriodToNoAnim: 0.25f,
                startProgress: 0f);
        }
        catch (Exception exception)
        {
            RexLog.Error($"Player ranged overlay '{actionName}' failed during {purpose}.", exception);
            return false;
        }
    }

    private bool TryLaunchStoningProjectile(Agent thrower, int throwNumber)
    {
        try
        {
            var stoneItem = Game.Current?.ObjectManager.GetObject<ItemObject>(StoningStoneItemId);
            if (stoneItem is null)
            {
                RexLog.Warning(
                    $"Native stoning projectile item '{StoningStoneItemId}' is unavailable; " +
                    "the crowd throw animation continues without a missile.");
                return false;
            }

            var horizontalDirection = _victimPosition.AsVec2 - thrower.Position.AsVec2;
            if (!horizontalDirection.IsNonZero())
            {
                return false;
            }

            horizontalDirection = horizontalDirection.Normalized();
            var start = thrower.Position + new Vec3(
                horizontalDirection.x * 0.30f,
                horizontalDirection.y * 0.30f,
                StoningProjectileSpawnHeight);
            var lateralOffset =
                (MBRandom.RandomFloat * 2f - 1f) * StoningProjectileLateralSpread;
            var target = _victimPosition + new Vec3(
                _placement?.Right.x * lateralOffset ?? 0f,
                _placement?.Right.y * lateralOffset ?? 0f,
                StoningProjectileTargetHeight + MBRandom.RandomFloat * 0.24f);
            var horizontalDistance = start.AsVec2.Distance(target.AsVec2);
            var flightSeconds = horizontalDistance / StoningProjectileSpeed;
            target.z += 4.905f * flightSeconds * flightSeconds;
            var missileDirection = target - start;
            if (!missileDirection.IsNonZero)
            {
                return false;
            }

            missileDirection.Normalize();
            var orientation = Mat3.CreateMat3WithForward(in missileDirection);
            var missileWeapon = new MissionWeapon(
                stoneItem,
                null,
                thrower.Origin?.Banner,
                1);
            Mission.AddCustomMissile(
                thrower,
                missileWeapon,
                start,
                missileDirection,
                orientation,
                StoningProjectileSpeed,
                StoningProjectileSpeed,
                addRigidBody: false,
                missionObjectToIgnore: null!);
            RexLog.Info(
                $"Session {Request.SessionId} launched native stoning projectile {throwNumber} " +
                $"from crowd agent {thrower.Index} at {StoningProjectileSpeed:0.00}m/s.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Stoning crowd projectile {throwNumber} could not be launched; " +
                "the ceremony remains active and later throws continue.",
                exception);
            return false;
        }
    }

    private void ApplyCrowdReaction()
    {
        var activeCrowd = _crowdAgents
            .Where(agent => agent.IsActive())
            .ToArray();
        if (UsesImpalementExecution())
        {
            ApplyImpalementCrowdReaction(activeCrowd);
            return;
        }
        var baseReaction = ExecutionRuleMath.GetCrowdReaction(
            Request.LegitimacyTier,
            Request.Tone,
            CurrentMethod.StringId);
        var cheeringCount = 0;
        foreach (var agent in activeCrowd)
        {
            try
            {
                // Reactions are chosen per spectator. Everyone remains anchored at
                // the generated viewing point; no reaction is allowed to order a retreat.
                agent.DisableScriptedMovement();
                agent.Controller = AgentControllerType.None;
                agent.ClearTargetFrame();
                var roll = MBRandom.RandomFloat;
                var individualReaction = baseReaction switch
                {
                    CrowdReactionType.Cheer => roll < 0.42f
                        ? CrowdReactionType.Cheer
                        : roll < 0.78f ? CrowdReactionType.Divided : CrowdReactionType.Panic,
                    CrowdReactionType.Panic => roll < 0.10f
                        ? CrowdReactionType.Cheer
                        : roll < 0.62f ? CrowdReactionType.Panic : CrowdReactionType.Divided,
                    _ => roll < 0.22f
                        ? CrowdReactionType.Cheer
                        : roll < 0.76f ? CrowdReactionType.Divided : CrowdReactionType.Panic
                };
                switch (individualReaction)
                {
                    case CrowdReactionType.Cheer:
                        TryPlayAction(agent, "act_arena_spectator", "individual crowd cheer", randomStart: true);
                        agent.SetWantsToYell();
                        cheeringCount++;
                        break;
                    case CrowdReactionType.Panic:
                        TryPlayAction(agent, "act_idle_unarmed_4", "individual crowd fear", randomStart: true);
                        agent.SetWantsToYell();
                        agent.SetLookToPointOfInterest(_victimPosition);
                        break;
                    default:
                        agent.SetLookToPointOfInterest(_victimPosition);
                        break;
                }
            }
            catch (Exception exception)
            {
                RexLog.Error($"Crowd reaction failed for agent {agent.Index}.", exception);
            }
        }

        if (cheeringCount > 0)
        {
            TryPlayCrowdCheerSound();
        }

        RexLog.Info(
            $"Session {Request.SessionId} applied per-spectator reactions: " +
            $"{cheeringCount}/{activeCrowd.Length} cheered without retreating.");
    }

    private void ApplyImpalementCrowdReaction(Agent[] activeCrowd)
    {
        // Shuffle the actual active spectators, then cheer exactly half. The
        // remainder keep a random standing idle; nobody retreats or panics.
        for (var index = activeCrowd.Length - 1; index > 0; index--)
        {
            var swapIndex = Math.Min(
                index,
                (int)(MBRandom.RandomFloat * (index + 1)));
            (activeCrowd[index], activeCrowd[swapIndex]) =
                (activeCrowd[swapIndex], activeCrowd[index]);
        }

        var cheeringTarget = activeCrowd.Length / 2;
        var cheeringCount = 0;
        for (var index = 0; index < activeCrowd.Length; index++)
        {
            var agent = activeCrowd[index];
            try
            {
                agent.DisableScriptedMovement();
                agent.Controller = AgentControllerType.None;
                agent.ClearTargetFrame();
                if (index < cheeringTarget)
                {
                    TryPlayAction(
                        agent,
                        "act_arena_spectator",
                        "randomly selected impalement crowd cheer",
                        randomStart: true);
                    agent.SetWantsToYell();
                    cheeringCount++;
                }
                else
                {
                    const string idlePurpose = "non-cheering impalement crowd town-resident ambient idle";
                    if (TryPickAmbientIdleAction(agent, CrowdAmbientIdleActionNames, out var idleAction))
                    {
                        TryPlayAction(agent, idleAction, idlePurpose, randomStart: true);
                    }
                    else if (!_ambientIdleUnavailableLogged)
                    {
                        _ambientIdleUnavailableLogged = true;
                        RexLog.Warning(
                            $"No ambient idle in the shared list has a clip in agent {agent.Index}'s action set ({idlePurpose}); " +
                            "spectators will keep their default stance for this session.");
                    }
                    agent.SetLookToPointOfInterest(_victimPosition);
                }
            }
            catch (Exception exception)
            {
                RexLog.Error($"Impalement crowd reaction failed for agent {agent.Index}.", exception);
            }
        }

        if (cheeringCount > 0)
        {
            TryPlayCrowdCheerSound();
        }

        RexLog.Info(
            $"Session {Request.SessionId} selected exactly {cheeringCount}/{activeCrowd.Length} " +
            "impalement spectators to cheer; all others retained random standing idles without retreating.");
    }

    private void TryPlayCrowdCheerSound()
    {
        try
        {
            var soundId = SoundEvent.GetEventIdFromString(
                "event:/mission/ambient/detail/arena/cheer_medium");
            Mission.MakeSound(
                soundId,
                _placement?.Origin ?? _victimPosition,
                soundCanBePredicted: false,
                isReliable: false,
                -1,
                -1);
        }
        catch (Exception exception)
        {
            RexLog.Error("Crowd cheer sound failed.", exception);
        }
    }

    private void FinishAftermath()
    {
        if (State == ExecutionSessionState.CrowdReaction)
        {
            _stateMachine.TransitionTo(ExecutionSessionState.Aftermath);
        }

        if (State != ExecutionSessionState.Aftermath)
        {
            return;
        }

        CompleteExecutionActionPlayback(force: true);
        RestorePlayerExecutionState();
        if (!_aftermathMessageDisplayed)
        {
            try
            {
                var message = _outcome?.Message ??
                              _aftermathMessageOverride ??
                              new TextObject("{=REX_Result_Success}The public sentence has been carried out.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                _aftermathMessageDisplayed = true;
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not display the execution result; aftermath cleanup will retry.", exception);
            }
        }

        if (!_aftermathConversationsRestored)
        {
            try
            {
                if (_victimConversationBlockApplied || TryBlockVictimConversationAfterLethalFrame())
                {
                    _conversationLogic?.DisableStartConversation(false);
                    _aftermathConversationsRestored = true;
                }
                else
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} kept ordinary conversations disabled because " +
                        "the executed victim could not be added to the native exclusion set.");
                }
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not restore town conversations; aftermath cleanup will retry.", exception);
            }
        }

        if (!_aftermathSessionReleased)
        {
            try
            {
                // Every step here is idempotent, so the flag is set only after
                // all of them ran; a failure retries on the next tick.
                ExecutionSessionCoordinator.Release(Request.SessionId);
                if (!_aftermathSpeechPlayed)
                {
                    // Replaying the phase would repeat its lines, so it runs once.
                    _aftermathSpeechPlayed = true;
                    ExecutionAddressLlmBridge.Play(Request.SessionId, ExecutionSpeechPhase.Aftermath);
                }
                ExecutionContinuation.Queue(Request);
                _aftermathSessionReleased = true;
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not release the execution session; aftermath cleanup will retry.", exception);
            }
        }
        RestoreExecutionEscortControl();
    }

    private bool HasDynamicAgentClearance(Agent player, Vec3 position)
    {
        var playerRadius = GetSafeCollisionRadius(player);
        foreach (var agent in Mission.Agents)
        {
            if (agent is null || !agent.IsActive() || ReferenceEquals(agent, player))
            {
                continue;
            }

            var requiredDistance = playerRadius +
                                   GetSafeCollisionRadius(agent) +
                                   PairedAgentCollisionMargin;
            if (agent.Position.AsVec2.Distance(position.AsVec2) < requiredDistance)
            {
                return false;
            }
        }

        return true;
    }

    private void RestorePlayerExecutionState()
    {
        if (_playerExecutionStateRestored)
        {
            return;
        }

        var needsPlayerState =
            (_actor == ExecutionActor.Player && _executionAction != ActionIndexCache.act_none) ||
            _playerWieldStateCaptured ||
            _playerWeaponSlotsModifiedForCeremony ||
            _playerLookDirectionStateCaptured ||
            _playerExecutionFacingLockActive;
        if (!needsPlayerState)
        {
            _playerExecutionStateRestored = true;
            return;
        }

        var player = _playerAgent;
        var playerIsActive = false;
        if (player is not null)
        {
            try
            {
                playerIsActive = player.IsActive();
            }
            catch (Exception exception)
            {
                RexLog.Warning(
                    $"The player mission agent was already released during cleanup ({exception.Message}).");
            }
        }

        if (player is null || !playerIsActive)
        {
            // All temporary equipment belongs to this mission agent. Once the
            // agent is gone there is no mission action or equipment state
            // left for this behavior to restore.
            _playerWieldStateCaptured = false;
            _playerWeaponSlotsModifiedForCeremony = false;
            _playerPrimaryWasTwoHanded = false;
            _playerLookDirectionStateCaptured = false;
            _playerExecutionFacingLockActive = false;
            _playerExecutionStateRestored = true;
            RexLog.Warning(
                "The player mission agent ended before temporary execution state could be restored.");
            return;
        }

        var actionRestored =
            _actor != ExecutionActor.Player ||
            _playerExecutionActionClearedForRestore ||
            TryClearExecutionActionChannel(player);
        var facingRestored =
            actionRestored && RestorePlayerExecutionFacingLock();
        var equipmentRestored =
            (!_playerWieldStateCaptured && !_playerWeaponSlotsModifiedForCeremony) ||
            RestorePlayerExecutionEquipment(player);
        _playerExecutionStateRestored = actionRestored && facingRestored && equipmentRestored;
        if (!_playerExecutionStateRestored)
        {
            RexLog.Warning(
                "Temporary player execution action or equipment restoration will retry during cleanup.");
        }
    }

    private bool TryPickAmbientIdleAction(
        Agent agent,
        string[] candidates,
        out string actionName) =>
        TryPickAmbientIdleAction(
            agent,
            candidates,
            (int)(MBRandom.RandomFloat * candidates.Length),
            out actionName);

    private bool TryPickAmbientIdleAction(
        Agent agent,
        string[] candidates,
        int start,
        out string actionName)
    {
        actionName = string.Empty;
        try
        {
            var actionSet = agent.ActionSet;
            for (var index = 0; index < candidates.Length; index++)
            {
                var candidate = candidates[(start + index) % candidates.Length];
                var action = ActionIndexCache.Create(candidate);
                if (action != ActionIndexCache.act_none &&
                    MBActionSet.CheckActionAnimationClipExists(actionSet, in action))
                {
                    actionName = candidate;
                    return true;
                }
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Ambient idle availability check failed for agent {agent.Index}.",
                exception);
        }

        return false;
    }

    private bool TryPlayAction(
        Agent? agent,
        string actionName,
        string purpose,
        bool randomStart = false,
        bool forceFullBody = false)
    {
        if (!TryResolveAndPrefetchAction(agent, actionName, purpose, out var action) ||
            agent is null)
        {
            return false;
        }

        return TrySetPreparedAction(
            agent,
            action,
            actionName,
            purpose,
            forceFullBody,
            randomStart);
    }

    private bool TryPlayRequiredAction(
        Agent? agent,
        string actionName,
        string purpose,
        out ActionIndexCache preparedAction,
        bool forceFullBody,
        float blendInPeriod = -0.2f)
    {
        preparedAction = ActionIndexCache.act_none;
        if (!TryResolveAndPrefetchAction(
                agent,
                actionName,
                purpose,
                out var resolvedAction) ||
            agent is null ||
            !TrySetPreparedAction(
                agent,
                resolvedAction,
                actionName,
                purpose,
                forceFullBody,
                blendInPeriod: blendInPeriod))
        {
            return false;
        }

        preparedAction = resolvedAction;
        return true;
    }

    private void PrefetchCeremonyActions()
    {
        if (_sceneAct is null)
        {
            return;
        }

        TryResolveAndPrefetchAction(
            _victimAgent,
            _sceneAct.VictimIdleAction,
            "victim idle prefetch",
            out _);
        if (string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Hanging,
                StringComparison.OrdinalIgnoreCase))
        {
            TryResolveAndPrefetchAction(
                _victimAgent,
                HangingVictimStruggleAction,
                "hanging victim struggle prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _victimAgent,
                _sceneAct.DeathAction,
                "hanging victim death pose prefetch",
                out _);
        }
        else if (string.Equals(
                     CurrentMethod.StringId,
                     ExecutionMethodRules.Burning,
                     StringComparison.OrdinalIgnoreCase))
        {
            TryResolveAndPrefetchAction(
                _victimAgent,
                BurningVictimIgnitionReactionAction,
                "burning victim ignition reaction prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _victimAgent,
                BurningVictimSustainedFearAction,
                "burning victim sustained fear prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _victimAgent,
                BurningVictimTorsoReactionAction,
                "burning victim torso reaction prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _victimAgent,
                _sceneAct.DeathAction,
                "burning victim death pose prefetch",
                out _);
        }
        TryResolveAndPrefetchAction(
            _executionerAgent,
            ExecutionerEntryDefaultIdleAction,
            "executioner entry default idle prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _executionerAgent,
            TwoHandedCeremonyFallbackIdleAction,
            "executioner entry fallback idle prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _executionerAgent,
            _sceneAct.ExecutionerIdleAction,
            "executioner idle prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _executionerAgent,
            _sceneAct.ExecutionAction,
            "NPC execution prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _executionerAgent,
            GenericExecutionAction,
            "NPC generic execution prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _playerAgent,
            _sceneAct.ExecutionerIdleAction,
            "player executioner idle prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _playerAgent,
            _sceneAct.ExecutionAction,
            "player executioner-action prefetch",
            out _);
        TryResolveAndPrefetchAction(
            _playerAgent,
            GenericExecutionAction,
            "player generic execution prefetch",
            out _);
        if (RequiresExecutionAxe())
        {
            // Both possible weapon grips resolve to the same authored action and
            // idle, but prefetch both contracts used by the existing selection path.
            TryResolveAndPrefetchAction(
                _playerAgent,
                OneHandedBeheadingExecutionAction,
                "player one-handed beheading prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _playerAgent,
                TwoHandedBeheadingExecutionAction,
                "player two-handed beheading prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _playerAgent,
                OneHandedCeremonyIdleAction,
                "player one-handed ceremony idle prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _playerAgent,
                OneHandedBeheadingFallbackAction,
                "player one-handed beheading fallback prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _playerAgent,
                TwoHandedBeheadingFallbackAction,
                "player two-handed beheading fallback prefetch",
                out _);
            TryResolveAndPrefetchAction(
                _playerAgent,
                OneHandedCeremonyFallbackIdleAction,
                "player one-handed ceremony fallback idle prefetch",
                out _);
        }

        try
        {
            _strategy.PrefetchActions();
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"The {CurrentMethod.StringId} strategy could not prefetch its method-specific actions; " +
                "the ceremony will keep the shared action fallbacks.",
                exception);
        }
    }

    private bool TryResolveAndPrefetchAction(
        Agent? agent,
        string actionName,
        string purpose,
        out ActionIndexCache action)
    {
        action = ActionIndexCache.act_none;
        if (agent is null || !agent.IsActive() || string.IsNullOrWhiteSpace(actionName))
        {
            return false;
        }

        try
        {
            action = ActionIndexCache.Create(actionName);
            if (action == ActionIndexCache.act_none)
            {
                RexLog.Warning($"Action '{actionName}' for {purpose} is unavailable.");
                return false;
            }

            var actionSet = agent.ActionSet;
            if (!MBActionSet.CheckActionAnimationClipExists(actionSet, in action))
            {
                RexLog.Warning(
                    $"Action '{actionName}' for {purpose} has no clip in agent {agent.Index}'s action set.");
                action = ActionIndexCache.act_none;
                return false;
            }

            MBAnimation.PrefetchAnimationClip(actionSet, action);
            return true;
        }
        catch (Exception exception)
        {
            action = ActionIndexCache.act_none;
            RexLog.Error($"Action '{actionName}' for {purpose} could not be prefetched.", exception);
            return false;
        }
    }

    private bool TrySetPreparedAction(
        Agent agent,
        ActionIndexCache action,
        string actionName,
        string purpose,
        bool forceFullBody,
        bool randomStart = false,
        bool enforceRootRotation = false,
        float blendInPeriod = -0.2f)
    {
        try
        {
            var preserveCrossbowVictimHandIk =
                forceFullBody &&
                UsesCrossbowExecution() &&
                ReferenceEquals(agent, _victimAgent);
            var flags = forceFullBody
                ? AnimFlags.anf_restart |
                  AnimFlags.anf_enforce_all |
                  AnimFlags.anf_lock_movement
                : 0;
            if (forceFullBody && !preserveCrossbowVictimHandIk)
            {
                flags |= AnimFlags.anf_disable_hand_ik;
            }
            var isImpalementDeath = UsesImpalementExecution() &&
                ReferenceEquals(agent, _victimAgent) &&
                (ImpalementExecutionStrategy.VictimDeathActions.Contains(actionName) ||
                 string.Equals(actionName, "act_ver_hanging_death", StringComparison.Ordinal));
            if (isImpalementDeath)
            {
                // Match the existing suspended-victim action contract. This
                // applies only to this method's original prisoner/death clips.
                flags |= AnimFlags.anf_restart | AnimFlags.anf_enforce_all |
                         AnimFlags.anf_lock_movement | AnimFlags.anf_enforce_root_rotation |
                         AnimFlags.anf_disable_foot_ik | AnimFlags.anf_disable_hand_ik |
                         AnimFlags.anf_disable_agent_agent_collisions;
                RexLog.Info($"Impalement death action '{actionName}' uses suspended full-body/root flags with foot IK disabled.");
            }
            if (UsesAuthoredExecutionRootCompensation(actionName))
            {
                // The inverse bind offset briefly overlaps the two Agent
                // capsules before the authored root displacement is evaluated.
                // Suppress only Agent-Agent collision for this action channel;
                // static floor and stage collision remain intact.
                flags |= AnimFlags.anf_disable_agent_agent_collisions;
            }
            if (enforceRootRotation)
            {
                flags |= AnimFlags.anf_enforce_root_rotation;
            }
            var none = ActionIndexCache.act_none;
            agent.SetActionChannel(
                0,
                in none,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart);
            return agent.SetActionChannel(
                0,
                in action,
                ignorePriority: true,
                additionalFlags: flags,
                blendWithNextActionFactor: 0f,
                actionSpeed: 1f,
                blendInPeriod: blendInPeriod,
                blendOutPeriodToNoAnim: 0.35f,
                startProgress: randomStart ? MBRandom.RandomFloat * 0.85f : 0f);
        }
        catch (Exception exception)
        {
            RexLog.Error($"Action '{actionName}' for {purpose} failed.", exception);
            return false;
        }
    }

    private void GetExecutionFocusText(
        Agent requesterAgent,
        IFocusable focusableObject,
        bool isInteractable,
        out FocusableObjectInformation information)
    {
        information = default;
        if (_strategy is IMultiStageExecutionStrategy &&
            State != ExecutionSessionState.WaitingForPlayer)
        {
            return;
        }
        if (!IsBoundPlayer(requesterAgent) || focusableObject is not Agent focusedAgent ||
            !_spawnedAgents.Contains(focusedAgent))
        {
            return;
        }

        information.IsActive = true;
        information.PrimaryInteractionText = focusedAgent == _executionerAgent
            ? new TextObject("{=REX_Executioner_Name}Executioner")
            : focusedAgent == _victimAgent
                ? Request.Victim.Name
                : focusedAgent.NameTextObject;
        information.SecondaryInteractionText = TextObject.GetEmpty();

        if (!CanPlayerUseExecutionPoint)
        {
            return;
        }

        if (focusedAgent == _executionerAgent)
        {
            information.SecondaryInteractionText = BuildKeyAction(
                new TextObject("{=REX_Action_Talk_Executioner}talk to the executioner"));
        }
        else if (focusedAgent == _victimAgent)
        {
            information.SecondaryInteractionText = BuildKeyAction(
                new TextObject(
                    _visualProfile?.InteractionAction ??
                    "{=REX_Action_Carry_Out}carry out the sentence"));
        }
    }

}
