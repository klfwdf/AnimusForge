using System;
using RichExecutions.Core;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Hanging pipeline that uses only the original hero Agent. The prisoner
/// struggles while being lifted, then stays alive in the mission with the
/// current struggle pose, AI, physics and root position frozen. No
/// clone, hidden duplicate, RegisterBlow, ragdoll or corpse-pool path is used;
/// the campaign execution is committed once when the player leaves the mission.
/// </summary>
internal sealed class HangingExecutionStrategy : IExecutionMethodStrategy
{
    private const string HangingHoistAction = "act_ver_hanging_struggle";
    private const string HangingSuspendedStruggleAction = HangingHoistAction;
    private const string HangingSuspendedDeathAction = "act_ver_hanging_death";
    private const string ClosedEyesFacialAction = "idle_sleep";
    private const string HangingReleaseSoundEvent = "event:/mission/siege/siegetower/dooropen";
    private const string HangingDropRopeClothPrefab = "bd_cloth_hanging_rope";
    private const string HangingDropRopeReinMesh = "horse_harness_a_new_rein";
    private const float HangingDropRopeLengthSlack = 0.92f;
    private const float HangingNeckDropFromEye = 0.18f;
    private const float HangingDropRopeMinimumLength = 0.60f;
    private const float HangingDropRopeMaximumLength = 4.50f;
    private const float HangingRopeMaxHorizontalDrift = 2.0f;
    private const float HangingLiftHeight = 0.75f;
    private const float HangingLiftRampSeconds = 1.10f;
    private const float HangingLethalDelaySeconds = 8.00f;
    private const float HangingDeathPoseBlendSeconds = 0.40f;
    private const float HangingStruggleSeconds =
        HangingLethalDelaySeconds - HangingDeathPoseBlendSeconds;
    private const float HangingDeathPoseSettleSeconds = HangingDeathPoseBlendSeconds;
    private const float HangingMidStruggleVoiceSeconds = 4.00f;
    private const float HangingMinimumVisibleNeckLift = 0.45f;
    private const float HangingSuspendedPoseProbeSeconds = 0.25f;
    private const float ActorReleaseRetrySeconds = 0.25f;

    private enum HangingPhase
    {
        None = 0,
        Struggle = 1,
        DeathPose = 2,
        Completed = 3
    }

    private IExecutionSceneHost _host = null!;
    private GameEntity? _dropRope;
    private GameEntity? _hoistUsePointHost;
    private HangingHoistUsePoint? _hoistUsePoint;
    private Vec3 _beamAnchor = Vec3.Invalid;
    private float _ropeBaseLength;
    private int _ropeLengthAxis;
    private Vec3 _ropeCenterLocal;
    private Vec3 _nooseCenterLocal;
    private MatrixFrame _nooseRelativeToHeadFrame = MatrixFrame.Identity;
    private float _liftElapsed;
    private Vec3 _liftBasePosition = Vec3.Invalid;
    private Vec3 _liftPosition = Vec3.Invalid;
    private Vec3 _liftBaseNeckPosition = Vec3.Invalid;
    private HangingPhase _phase;
    private float _phaseElapsed;
    private float _actorReleaseRetryElapsed;
    private bool _executionActorReleased;
    private bool _releaseSoundAttempted;
    private bool _releaseFrameLogged;
    private bool _firstUpdateLogged;
    private bool _liftFailureLogged;
    private bool _liftBodyPrepared;
    private bool _liftCompletionLogged;
    private bool _suspensionFrozen;
    private bool _nooseBoundsResolved;
    private bool _noosePlacementLogged;
    private bool _nooseHeadBindingCaptured;
    private bool _nooseRotationFollowLogged;
    private bool _nativeLiftCompletionLogged;
    private bool _nativeLiftFailureLogged;
    private bool _nativeHoistActionStarted;
    private bool _suspendedStruggleStarted;
    private bool _suspendedDeathStarted;
    private float _suspendedPoseProbeElapsed;
    private bool _suspendedPoseProbeLogged;
    private bool _visibleLiftInvariantChecked;
    private bool _hoistUseStateLogged;
    private bool _hoistUseStateFailureLogged;
    private bool _victimMortalityCaptured;
    private bool _liftVoiceAttempted;
    private bool _midStruggleVoiceAttempted;
    private bool _deathVoiceAttempted;
    private bool _facialSuppressionLogged;
    private bool _closedEyesLogged;
    private bool _facialSuppressionFailureLogged;
    private Agent.MortalityState _victimMortalityBeforeLift;

    public string MethodId => ExecutionMethodRules.Hanging;

    public bool UsesStagedExecution => true;

    public bool RequiresExecutionAxe => false;

    public bool DefersVictimDeathToMissionExit => true;

    public void AttachHost(IExecutionSceneHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public void SetupSceneVisuals()
    {
        _dropRope = TryCreateDropRope();
        _host.LogInfo(
            $"Hanging visuals: drop rope={(_dropRope is null ? "unavailable" : "created")}; " +
            "the noose remains owned by the post-stage visual profile.");
    }

    public void TickSceneVisuals(float dt)
    {
        RetryReleaseExecutionActor(dt);
        // This callback runs throughout the initialized mission, not only after
        // the player starts the execution. The rope must therefore bind to the
        // original prisoner's prepared skeleton as soon as the scene appears.
        // It is also the strategy's final callback for each mission tick. Feed
        // the lifted Z into the complete native usable-object movement channel,
        // while the custom struggle clip supplies the visible pose.
        DriveNativeOriginalVictimLiftAtEndOfTick();
        ProbeSuspendedStrugglePose(dt);
        UpdateRopeToNeck();
        MaintainHangingFacialSuppression();
    }

    public void PrefetchActions()
    {
        var victim = _host.VictimAgent;
        if (victim is null)
        {
            return;
        }

        _host.TryPlayRequiredAction(
            victim,
            HangingHoistAction,
            "original-prisoner custom hanging struggle/hoist prefetch",
            forceFullBody: true);
        _host.TryPlayRequiredAction(
            victim,
            HangingSuspendedDeathAction,
            "original-prisoner suspended death-pose prefetch",
            forceFullBody: true);
    }

    public void StartExecutionAction()
    {
        _host.StartStagedMethodExecutionAction();
    }

    public void TickExecution(float dt)
    {
        // Once the lever is released, only the victim sequence owns motion.
        // The actor must stay free even while action/equipment cleanup retries.
        if (_phase != HangingPhase.None)
        {
            return;
        }

        var safeDt = MathF.Max(0f, dt);
        _host.ExecutionActionAttemptElapsed += safeDt;
        var actionAgent = _host.GetActiveExecutionActor();
        var progress = 0f;
        if (actionAgent is not null && actionAgent.IsActive())
        {
            _host.ReassertExecutionActionRoot(actionAgent, "hanging method-action tick");
            progress = _host.ObserveExecutionAction(actionAgent);
        }

        if (progress >= 0.01f)
        {
            _host.MaximumExecutionActionProgress = MathF.Max(
                _host.MaximumExecutionActionProgress,
                progress);
        }

        var lethal = _host.SceneAct?.LethalProgress ?? 0.52f;
        if (progress >= lethal || _host.MaximumExecutionActionProgress >= lethal)
        {
            if (!_releaseFrameLogged)
            {
                _releaseFrameLogged = true;
                _host.LogInfo(
                    $"Hanging release action reached the release frame (progress={MathF.Max(progress, _host.MaximumExecutionActionProgress):0.000}); " +
                    "starting the original-prisoner hanging sequence.");
                BeginMethodSequence("hanging release frame");
            }

            return;
        }

        if (_host.ExecutionActionAttemptElapsed >= (_host.SceneAct?.MaximumActionSeconds ?? 4.5f) &&
            !_releaseFrameLogged)
        {
            _releaseFrameLogged = true;
            _host.LogWarning(
                $"Hanging release action stalled at progress {MathF.Max(progress, _host.MaximumExecutionActionProgress):0.000}; " +
                "starting the original-prisoner hanging sequence by timeout.");
            BeginMethodSequence("hanging action timeout");
        }
    }

    public void BeginMethodSequence(string trigger)
    {
        if (_phase != HangingPhase.None)
        {
            return;
        }

        _phase = HangingPhase.Struggle;
        _phaseElapsed = 0f;
        _liftElapsed = 0f;
        _liftCompletionLogged = false;
        _actorReleaseRetryElapsed = 0f;
        _executionActorReleased = _host.ReleaseExecutionActorControl($"{trigger}; hanging lift has started");
        if (!_executionActorReleased)
        {
            _host.LogWarning(
                "The hanging release action could not be cleared on the first lift tick; " +
                "player-state cleanup will continue without pinning the player to the lever.");
        }
        TryPlayReleaseSound();
        if (!_liftVoiceAttempted)
        {
            _liftVoiceAttempted = true;
            TryPlayVictimVoice(
                SkinVoiceManager.VoiceType.Pain,
                "the original prisoner begins rising");
        }

        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            _host.LogError(
                "The original prisoner Agent is unavailable at the hanging release frame; the timed sequence remains active for diagnosis.");
        }
        else
        {
            _liftBasePosition = victim.Position.IsValid
                ? victim.Position
                : _host.VictimPosition;
            _liftPosition = _liftBasePosition;
            _host.HangingLiftPosition = _liftPosition;
            _liftBaseNeckPosition = ReadOriginalVictimNeckPoint();
            PrepareOriginalVictimForLift(victim);

            if (!TryStartNativeHoistAction(victim))
            {
                _host.LogWarning(
                    "The original prisoner could not start the custom hanging struggle/hoist action on channel 0; " +
                    "the usable-object state and target-frame drive remain active for diagnosis.");
            }
        }

        _host.LogInfo(
            $"Began the original-prisoner hanging sequence after {trigger}: " +
            $"struggle={HangingStruggleSeconds:0.00}s, deathBlend={HangingDeathPoseBlendSeconds:0.00}s, " +
            $"lethalFromLift={HangingLethalDelaySeconds:0.00}s, " +
            $"lift={HangingLiftHeight:0.00}m.");
    }

    public void TickMethodSequence(float dt)
    {
        var safeDt = MathF.Max(0f, dt);
        if (_phase is HangingPhase.None or HangingPhase.Completed)
        {
            return;
        }

        // The host invokes staged-method ticks as soon as the execution actor's
        // release action starts. Do not begin lifting until BeginMethodSequence
        // has observed the actual release frame and disabled the victim's
        // gravity; otherwise the rope rises while the live Agent is pulled back
        // to the deck.
        UpdateLift(safeDt);
        _phaseElapsed += safeDt;
        if (_phase == HangingPhase.Struggle &&
            !_midStruggleVoiceAttempted &&
            _phaseElapsed >= HangingMidStruggleVoiceSeconds)
        {
            _midStruggleVoiceAttempted = true;
            TryPlayVictimVoice(
                SkinVoiceManager.VoiceType.Yell,
                "midway through the eight-second suspension");
        }

        if (_phase == HangingPhase.Struggle && _phaseElapsed >= HangingStruggleSeconds)
        {
            _phase = HangingPhase.DeathPose;
            _phaseElapsed = 0f;
            if (!_deathVoiceAttempted)
            {
                _deathVoiceAttempted = true;
                TryPlayVictimVoice(
                    SkinVoiceManager.VoiceType.Death,
                    "the suspended death-pose transition");
            }

            var victim = _host.VictimAgent;
            if (victim is null || !victim.IsActive() || !TryStartSuspendedDeathPose(victim))
            {
                _host.LogWarning(
                    "The custom suspended death pose could not be started; the lifted prisoner remains visible and the sequence continues to the freeze point.");
            }
        }
        else if (_phase == HangingPhase.DeathPose &&
                 _phaseElapsed >= HangingDeathPoseSettleSeconds)
        {
            _phase = HangingPhase.Completed;
            _phaseElapsed = 0f;
            _host.LogInfo(
                $"Completed the visible original-prisoner hanging choreography; " +
                $"freezing custom death pose '{HangingSuspendedDeathAction}' at the lifted root " +
                $"at {HangingLethalDelaySeconds:0.00}s from lift start, without RegisterBlow or a native death action.");
            _host.RequestLethalFrame();
        }
    }

    private void RetryReleaseExecutionActor(float dt)
    {
        if (_executionActorReleased || _phase == HangingPhase.None)
        {
            return;
        }

        _actorReleaseRetryElapsed += MathF.Max(0f, dt);
        if (_actorReleaseRetryElapsed < ActorReleaseRetrySeconds)
        {
            return;
        }

        _actorReleaseRetryElapsed = 0f;
        _executionActorReleased = _host.ReleaseExecutionActorControl(
            "hanging sequence release retry");
    }

    public void ApplyLethalFrameEffects()
    {
        // The original Agent already displays the custom static hanging death pose.
    }

    public bool ApplyDeath()
    {
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            _host.LogError(
                "The original prisoner Agent is unavailable and cannot be frozen for hanging suspension.");
            return false;
        }

        var suspensionPosition = _liftPosition.IsValid
            ? _liftPosition
            : _host.VictimPosition;
        if (!suspensionPosition.IsValid)
        {
            _host.LogError(
                "The original prisoner has no valid hanging suspension position; no mission death fallback is attempted.");
            return false;
        }

        _suspensionFrozen = _host.FreezeVictimForSuspension(
            suspensionPosition,
            "original-prisoner hanging death pose");
        if (_suspensionFrozen)
        {
            if (_nativeHoistActionStarted)
            {
                victim.SetCurrentActionSpeed(0, 0f);
            }

            PinOriginalVictimAt(victim, suspensionPosition, "final hanging freeze");
            _host.LogInfo(
                $"Original prisoner Agent {victim.Index} is visually suspended at " +
                $"({suspensionPosition.x:0.00}, {suspensionPosition.y:0.00}, {suspensionPosition.z:0.00}); " +
                "mission Agent remains alive until campaign settlement on exit.");
        }

        return _suspensionFrozen;
    }

    public void TickAfterDeath(float dt)
    {
        MaintainOriginalVictimSuspension(dt);
    }

    public void Cleanup()
    {
        _suspensionFrozen = false;
        var victim = _host.VictimAgent;
        if (victim is not null && victim.IsActive())
        {
            try
            {
                if (_victimMortalityCaptured)
                {
                    victim.SetMortalityState(_victimMortalityBeforeLift);
                }

                victim.ClearTargetFrame();
                var zeroUp = Vec3.Zero;
                victim.SetTargetUp(in zeroUp);
                victim.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: false);
                victim.SetForceAttachedEntity(WeakGameEntity.Invalid);
                victim.SetIsPhysicsForceClosed(false);
                victim.SetCurrentActionSpeed(0, 1f);
                ClearHangingActionChannels(victim);
                if (_hoistUsePoint is not null &&
                    ReferenceEquals(victim.CurrentlyUsedGameObject, _hoistUsePoint))
                {
                    victim.StopUsingGameObject(isSuccessful: false);
                }
            }
            catch (Exception exception)
            {
                _host.LogWarning(
                    $"Could not fully restore the original prisoner after hanging cleanup. {exception.Message}");
            }
        }

        if (_dropRope is not null)
        {
            _host.RemoveSpawnedEntity(_dropRope);
            _dropRope = null;
        }

        _hoistUsePoint?.Unbind();
        _hoistUsePoint = null;
        if (_hoistUsePointHost is not null)
        {
            _host.RemoveSpawnedEntity(_hoistUsePointHost);
            _hoistUsePointHost = null;
        }

    }

    private void TryPlayReleaseSound()
    {
        if (_releaseSoundAttempted)
        {
            return;
        }

        _releaseSoundAttempted = true;
        try
        {
            var soundId = SoundEvent.GetEventIdFromString(HangingReleaseSoundEvent);
            if (soundId < 0)
            {
                _host.LogWarning(
                    $"Hanging release sound '{HangingReleaseSoundEvent}' is unavailable.");
                return;
            }

            var owner = _host.GetActiveExecutionActor();
            _host.Mission.MakeSound(
                soundId,
                _host.VictimPosition,
                soundCanBePredicted: false,
                isReliable: true,
                owner?.Index ?? -1,
                _host.VictimAgent?.Index ?? -1);
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not play the hanging release sound; the sequence continues. {exception.Message}");
        }
    }

    private void UpdateLift(float dt)
    {
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive() || !_host.VictimPosition.IsValid)
        {
            return;
        }

        if (!_liftBasePosition.IsValid)
        {
            _liftBasePosition = victim.Position.IsValid
                ? victim.Position
                : _host.VictimPosition;
        }

        if (!_liftPosition.IsValid)
        {
            _liftPosition = _liftBasePosition;
        }

        if (_liftElapsed < HangingLiftRampSeconds)
        {
            _liftElapsed += MathF.Max(0f, dt);
            var progress = MathF.Min(1f, _liftElapsed / HangingLiftRampSeconds);
            var eased = SmoothStep(progress);
            _liftPosition = new Vec3(
                _liftBasePosition.x,
                _liftBasePosition.y,
                _liftBasePosition.z + HangingLiftHeight * eased);
            _host.HangingLiftPosition = _liftPosition;
        }

        try
        {
            PinOriginalVictimAt(victim, _liftPosition, "hanging lift");
            if (!_liftCompletionLogged && _liftElapsed >= HangingLiftRampSeconds)
            {
                _liftCompletionLogged = true;
                var visualPosition = victim.AgentVisuals?.GetGlobalFrame().origin ?? Vec3.Invalid;
                var neck = ReadOriginalVictimNeckPoint();
                var visibleNeckLift = _liftBaseNeckPosition.IsValid && neck.IsValid
                    ? neck.z - _liftBaseNeckPosition.z
                    : float.NaN;
                _host.LogInfo(
                    $"Original-prisoner hanging lift reached target: target=({_liftPosition.x:0.00}, {_liftPosition.y:0.00}, {_liftPosition.z:0.00}), " +
                    $"agentRoot=({victim.Position.x:0.00}, {victim.Position.y:0.00}, {victim.Position.z:0.00}), " +
                    $"visualRoot=({visualPosition.x:0.00}, {visualPosition.y:0.00}, {visualPosition.z:0.00}), " +
                    $"neck=({neck.x:0.00}, {neck.y:0.00}, {neck.z:0.00}), " +
                    $"visibleNeckLift={visibleNeckLift:0.00}m, physicsClosed={_liftBodyPrepared}. ");

                CheckVisibleLiftInvariant(victim, neck, visibleNeckLift);
            }
        }
        catch (Exception exception)
        {
            if (!_liftFailureLogged)
            {
                _liftFailureLogged = true;
                _host.LogWarning(
                    $"Could not lift the original prisoner Agent; the hanging sequence continues for diagnosis. {exception.Message}");
            }
        }
    }

    private void MaintainOriginalVictimSuspension(float dt)
    {
        if (!_suspensionFrozen || !_liftPosition.IsValid)
        {
            return;
        }

        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            _suspensionFrozen = false;
            _host.LogWarning(
                "Stopped maintaining original-prisoner suspension because the Agent became unavailable.");
            return;
        }

        try
        {
            // Never write to an engine-owned corpse. This path is intended to
            // keep the Agent alive, so any native death means another module or
            // unexpected engine state took over and all visual writes must stop.
            if (victim.State == AgentState.Killed ||
                victim.IsAddedAsCorpse() ||
                victim.IsFadingOut())
            {
                _suspensionFrozen = false;
                _host.LogWarning(
                    "Stopped maintaining original-prisoner suspension because native death/corpse presentation took over.");
                return;
            }

            if (_nativeHoistActionStarted)
            {
                victim.SetCurrentActionSpeed(0, 0f);
            }

            PinOriginalVictimAt(victim, _liftPosition, "maintained hanging suspension");
        }
        catch (Exception exception)
        {
            _suspensionFrozen = false;
            _host.LogWarning(
                $"Stopped maintaining original-prisoner suspension after an error. {exception.Message}");
        }
    }

    private void PrepareOriginalVictimForLift(Agent victim)
    {
        if (_liftBodyPrepared)
        {
            return;
        }

        try
        {
            if (!_victimMortalityCaptured)
            {
                _victimMortalityBeforeLift = victim.CurrentMortalityState;
                _victimMortalityCaptured = true;
            }

            victim.SetMortalityState(Agent.MortalityState.Invulnerable);
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            // Match Bannerlord's complete native climbing-machine use path. A
            // real StandingPoint relation must exist before the custom struggle,
            // gravity exclusion and target frame are allowed to move a live
            // human Agent above the navigation surface.
            EnsureNativeHoistUseState(victim, "lift preparation");
            victim.SetIsPhysicsForceClosed(false);
            victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
            _liftBodyPrepared = true;
            _host.LogInfo(
                $"Prepared original prisoner Agent {victim.Index} for an airborne hanging lift; " +
                $"AI is paused and native UseGameObject/gravity/target-Z movement is active; " +
                $"channel 0 will use custom '{HangingHoistAction}' from the release frame with no visible vanilla climbing action; " +
                $"health={victim.Health:0.0}/{victim.HealthLimit:0.0}, mortality={victim.CurrentMortalityState}.");
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not fully disable gravity before lifting the original prisoner; " +
                $"root pinning will continue every tick. {exception.Message}");
        }
    }

    private void PinOriginalVictimAt(Agent victim, Vec3 position, string phase)
    {
        EnsureNativeHoistUseState(victim, phase);
        if (_nativeHoistActionStarted && !_suspendedStruggleStarted && !_suspensionFrozen)
        {
            var hoist = ActionIndexCache.Create(HangingHoistAction);
            if (hoist != ActionIndexCache.act_none && victim.GetCurrentAction(0) != hoist)
            {
                victim.SetActionChannel(
                    0,
                    in hoist,
                    ignorePriority: false,
                    additionalFlags: AnimFlags.anf_restart |
                                     AnimFlags.anf_lock_movement |
                                     AnimFlags.anf_enforce_all |
                                     AnimFlags.anf_enforce_root_rotation |
                                     AnimFlags.anf_disable_foot_ik |
                                     AnimFlags.anf_disable_agent_agent_collisions,
                    blendWithNextActionFactor: 0f,
                    actionSpeed: 1f,
                    blendInPeriod: 0f,
                    blendOutPeriodToNoAnim: 0f,
                    startProgress: 0f);
            }
        }

        var direction = victim.LookDirection;
        var horizontalDirection = direction.AsVec2;
        if (!horizontalDirection.IsNonZero())
        {
            horizontalDirection = _host.Placement?.Forward ?? Vec2.Forward;
        }

        horizontalDirection = horizontalDirection.Normalized();
        direction = new Vec3(horizontalDirection.x, horizontalDirection.y, 0f);
        var targetUp = new Vec3(0f, 0f, 1f);
        victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
        victim.SetTargetZ(position.z);
        victim.SetTargetPositionAndDirection(position.AsVec2, direction);
        victim.SetTargetUp(in targetUp);
    }

    private bool TryStartNativeHoistAction(Agent victim)
    {
        try
        {
            var hoist = ActionIndexCache.Create(HangingHoistAction);
            if (hoist == ActionIndexCache.act_none)
            {
                return false;
            }

            ClearHangingActionChannels(victim);
            _nativeHoistActionStarted = victim.SetActionChannel(
                0,
                in hoist,
                ignorePriority: false,
                additionalFlags: AnimFlags.anf_restart |
                                  AnimFlags.anf_lock_movement |
                                  AnimFlags.anf_enforce_all |
                                  AnimFlags.anf_enforce_root_rotation |
                                  AnimFlags.anf_disable_foot_ik |
                                  AnimFlags.anf_disable_agent_agent_collisions,
                blendWithNextActionFactor: 0f,
                actionSpeed: 1f,
                blendInPeriod: 0f,
                blendOutPeriodToNoAnim: 0f,
                startProgress: 0f);

            _host.LogInfo(
                $"Requested original-prisoner custom struggle/hoist action on channel 0: " +
                $"started={_nativeHoistActionStarted}, action={victim.GetCurrentAction(0).Index}, " +
                $"usedObject={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                $"standingPointHasUser={_hoistUsePoint?.HasUser == true}.");
            return _nativeHoistActionStarted;
        }
        catch (Exception exception)
        {
            ClearHangingActionChannels(victim);
            _host.LogWarning(
                $"Could not bind the custom hanging struggle/hoist action to channel 0; " +
                $"the usable-object lift remains active for diagnosis. {exception.Message}");
            return false;
        }
    }

    private void CheckVisibleLiftInvariant(Agent victim, Vec3 neck, float visibleNeckLift)
    {
        if (_visibleLiftInvariantChecked)
        {
            return;
        }

        _visibleLiftInvariantChecked = true;
        if (!_liftBaseNeckPosition.IsValid || !neck.IsValid ||
            !IsFinite(visibleNeckLift) ||
            visibleNeckLift < HangingMinimumVisibleNeckLift)
        {
            _host.LogWarning(
                $"Visible hanging lift invariant failed: baseNeckZ={_liftBaseNeckPosition.z:0.00}, " +
                $"currentNeckZ={neck.z:0.00}, visibleLift={visibleNeckLift:0.00}m, " +
                $"required={HangingMinimumVisibleNeckLift:0.00}m, " +
                $"usedObject={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                $"standingPointHasUser={_hoistUsePoint?.HasUser == true}, " +
                $"channel0={victim.GetCurrentAction(0).Index}. " +
                "The mission remains active and no teleport/cancel fallback is applied.");
            return;
        }

        _host.LogInfo(
            $"Visible hanging lift invariant passed: neck rose {visibleNeckLift:0.00}m " +
            $"from {_liftBaseNeckPosition.z:0.00} to {neck.z:0.00}.");
        TryStartSuspendedStruggle(victim);
    }

    private bool TryStartSuspendedStruggle(Agent victim)
    {
        if (_suspendedStruggleStarted)
        {
            return true;
        }

        try
        {
            var struggle = ActionIndexCache.Create(HangingSuspendedStruggleAction);
            if (struggle == ActionIndexCache.act_none)
            {
                _host.LogWarning(
                    $"Suspended struggle action '{HangingSuspendedStruggleAction}' is unavailable; " +
                    "the prisoner remains lifted and the mission continues.");
                return false;
            }

            if (victim.GetCurrentAction(0) == struggle)
            {
                _suspendedStruggleStarted = true;
                _suspendedPoseProbeElapsed = 0f;
                _suspendedPoseProbeLogged = false;
                PinOriginalVictimAt(victim, _liftPosition, "continued custom suspended struggle");
                _host.LogInfo(
                    $"Custom hanging struggle already owns channel 0 at full lift; " +
                    $"continuing action={victim.GetCurrentAction(0).Index} without restart, " +
                    $"usedObject={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                    $"standingPointHasUser={_hoistUsePoint?.HasUser == true}.");
                return true;
            }

            EnsureNativeHoistUseState(victim, "suspended struggle transition");
            _suspendedStruggleStarted = victim.SetActionChannel(
                0,
                in struggle,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart |
                                 AnimFlags.anf_lock_movement |
                                 AnimFlags.anf_enforce_all |
                                 AnimFlags.anf_enforce_root_rotation |
                                 AnimFlags.anf_disable_foot_ik |
                                 AnimFlags.anf_disable_agent_agent_collisions,
                blendWithNextActionFactor: 0f,
                actionSpeed: 1f,
                blendInPeriod: 0.08f,
                blendOutPeriodToNoAnim: 0f,
                startProgress: 0f);
            _suspendedPoseProbeElapsed = 0f;
            _suspendedPoseProbeLogged = false;
            PinOriginalVictimAt(victim, _liftPosition, "suspended struggle transition");
            _host.LogInfo(
                $"Switched original prisoner from the hoist phase to suspended struggle on channel 0: " +
                $"started={_suspendedStruggleStarted}, action={victim.GetCurrentAction(0).Index}, " +
                $"usedObject={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                $"standingPointHasUser={_hoistUsePoint?.HasUser == true}.");
            return _suspendedStruggleStarted;
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not switch the lifted original prisoner to the suspended struggle pose; " +
                $"the mission remains active. {exception.Message}");
            return false;
        }
    }

    private bool TryStartSuspendedDeathPose(Agent victim)
    {
        if (_suspendedDeathStarted)
        {
            return true;
        }

        try
        {
            var death = ActionIndexCache.Create(HangingSuspendedDeathAction);
            if (death == ActionIndexCache.act_none)
            {
                _host.LogWarning(
                    $"Suspended death action '{HangingSuspendedDeathAction}' is unavailable; " +
                    "the current lifted pose will be frozen instead.");
                return false;
            }

            EnsureNativeHoistUseState(victim, "custom suspended death transition");
            _suspendedDeathStarted = victim.SetActionChannel(
                0,
                in death,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart |
                                 AnimFlags.anf_lock_movement |
                                 AnimFlags.anf_enforce_all |
                                 AnimFlags.anf_enforce_root_rotation |
                                 AnimFlags.anf_disable_foot_ik |
                                 AnimFlags.anf_disable_agent_agent_collisions,
                blendWithNextActionFactor: 0f,
                actionSpeed: 1f,
                blendInPeriod: HangingDeathPoseBlendSeconds,
                blendOutPeriodToNoAnim: 0f,
                startProgress: 0f);
            PinOriginalVictimAt(victim, _liftPosition, "custom suspended death transition");
            _host.LogInfo(
                $"Switched original prisoner to custom suspended death pose on channel 0: " +
                $"started={_suspendedDeathStarted}, action={victim.GetCurrentAction(0).Index}, " +
                $"usedObject={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                $"standingPointHasUser={_hoistUsePoint?.HasUser == true}, " +
                $"settle={HangingDeathPoseSettleSeconds:0.00}s.");
            return _suspendedDeathStarted;
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not switch the lifted original prisoner to the custom suspended death pose; " +
                $"the current pose will be frozen and the mission remains active. {exception.Message}");
            return false;
        }
    }

    private void ProbeSuspendedStrugglePose(float dt)
    {
        if (!_suspendedStruggleStarted || _suspendedPoseProbeLogged)
        {
            return;
        }

        _suspendedPoseProbeElapsed += MathF.Max(0f, dt);
        if (_suspendedPoseProbeElapsed < HangingSuspendedPoseProbeSeconds)
        {
            return;
        }

        _suspendedPoseProbeLogged = true;
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        var neck = ReadOriginalVictimNeckPoint();
        var visibleNeckLift = _liftBaseNeckPosition.IsValid && neck.IsValid
            ? neck.z - _liftBaseNeckPosition.z
            : float.NaN;
        var message =
            $"Suspended struggle probe after {HangingSuspendedPoseProbeSeconds:0.00}s: " +
            $"targetZ={_liftPosition.z:0.00}, agentRootZ={victim.Position.z:0.00}, " +
            $"neckZ={neck.z:0.00}, visibleNeckLift={visibleNeckLift:0.00}m, " +
            $"channel0={victim.GetCurrentAction(0).Index}, " +
            $"usedObject={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
            $"standingPointHasUser={_hoistUsePoint?.HasUser == true}.";
        if (IsFinite(visibleNeckLift) && visibleNeckLift >= HangingMinimumVisibleNeckLift)
        {
            _host.LogInfo(message);
        }
        else
        {
            _host.LogWarning(message + " The mission remains active without restoring a vanilla climbing action.");
        }
    }

    private static void ClearHangingActionChannels(Agent victim)
    {
        ClearActionChannel(victim, 0);
        ClearActionChannel(victim, 1);
    }

    private static void ClearActionChannel(Agent victim, int channel)
    {
        var none = ActionIndexCache.act_none;
        victim.SetActionChannel(
            channel,
            in none,
            ignorePriority: true,
            additionalFlags: AnimFlags.anf_restart,
            blendWithNextActionFactor: 0f,
            actionSpeed: 1f,
            blendInPeriod: 0f,
            blendOutPeriodToNoAnim: 0f,
            startProgress: 0f);
    }

    private bool EnsureNativeHoistUseState(Agent victim, string phase)
    {
        if (!TryCreateHoistUsePoint(victim))
        {
            if (!_hoistUseStateFailureLogged)
            {
                _hoistUseStateFailureLogged = true;
                _host.LogWarning(
                    $"Could not create the original prisoner's hidden native hoist use point during {phase}; " +
                    "target-frame driving continues for diagnosis without ending the mission.");
            }

            return false;
        }

        try
        {
            if (!_hoistUsePoint!.IsUsedBy(victim))
            {
                if (victim.CurrentlyUsedGameObject is not null)
                {
                    victim.StopUsingGameObject(isSuccessful: false);
                }

                victim.DisableScriptedMovement();
                victim.UseGameObject(_hoistUsePoint);
            }

            var used = _hoistUsePoint.IsUsedBy(victim);
            var parentMatched = _hoistUsePointHost is not null &&
                                victim.IsAgentParentEntitySameAs(_hoistUsePointHost);
            if (!parentMatched && _hoistUsePointHost is not null)
            {
                victim.SetForceAttachedEntity(_hoistUsePointHost.WeakEntity);
                parentMatched = victim.IsAgentParentEntitySameAs(_hoistUsePointHost);
            }

            if (!_hoistUseStateLogged)
            {
                _hoistUseStateLogged = true;
                _host.LogInfo(
                    $"Original prisoner native hoist use state during {phase}: " +
                    $"currentlyUsed={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                    $"standingPointHasUser={_hoistUsePoint.HasUser}, userMatched={used}, " +
                    $"parentMatched={parentMatched}.");
            }

            if ((!used || !parentMatched) && !_hoistUseStateFailureLogged)
            {
                _hoistUseStateFailureLogged = true;
                _host.LogWarning(
                    $"The engine did not retain the original prisoner's complete hoist use state during {phase}; " +
                    $"used={used}, parentMatched={parentMatched}. The mission remains active for diagnosis.");
            }

            return used && parentMatched;
        }
        catch (Exception exception)
        {
            if (!_hoistUseStateFailureLogged)
            {
                _hoistUseStateFailureLogged = true;
                _host.LogWarning(
                    $"Could not establish or verify the original prisoner's native hoist use state during {phase}; " +
                    $"target-frame driving continues without mission cancellation. {exception.Message}");
            }

            return false;
        }
    }

    private bool TryCreateHoistUsePoint(Agent victim)
    {
        if (_hoistUsePointHost is not null && _hoistUsePoint is not null)
        {
            _hoistUsePoint.Bind(victim);
            return true;
        }

        GameEntity? host = null;
        try
        {
            host = GameEntity.CreateEmpty(
                _host.Mission.Scene,
                isModifiableFromEditor: false,
                createPhysics: false,
                callScriptCallbacks: false);
            if (host is null)
            {
                return false;
            }

            host.Name = "rex_hanging_hoist_use_point";
            host.SetMobility(GameEntity.Mobility.Stationary);
            var frame = MatrixFrame.Identity;
            frame.origin = _liftBasePosition.IsValid
                ? _liftBasePosition
                : victim.Position;
            host.SetGlobalFrame(in frame, isTeleportation: true);
            host.CreateAndAddScriptComponent(
                typeof(HangingHoistUsePoint).Name,
                callScriptCallbacks: true);
            var usePoint = host.GetFirstScriptOfType<HangingHoistUsePoint>();
            if (usePoint is null)
            {
                _host.RemoveSpawnedEntity(host);
                return false;
            }

            usePoint.Bind(victim);
            _hoistUsePointHost = host;
            _hoistUsePoint = usePoint;
            _host.LogInfo(
                $"Created hidden native hanging hoist StandingPoint at " +
                $"({frame.origin.x:0.00}, {frame.origin.y:0.00}, {frame.origin.z:0.00}) for original Agent {victim.Index}.");
            return true;
        }
        catch (Exception exception)
        {
            if (host is not null)
            {
                _host.RemoveSpawnedEntity(host);
            }

            _host.LogWarning(
                $"Could not create the hidden native hanging hoist StandingPoint; " +
                $"the mission remains active. {exception.Message}");
            return false;
        }
    }

    private void DriveNativeOriginalVictimLiftAtEndOfTick()
    {
        if ((_phase == HangingPhase.None && !_suspensionFrozen) || !_liftPosition.IsValid)
        {
            return;
        }

        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive() ||
            victim.State == AgentState.Killed || victim.IsAddedAsCorpse() || victim.IsFadingOut())
        {
            return;
        }

        try
        {
            PinOriginalVictimAt(victim, _liftPosition, "end-of-tick native hanging lift");
            var visuals = victim.AgentVisuals;
            if (visuals is null || !visuals.IsValid())
            {
                return;
            }

            if (!_nativeLiftCompletionLogged && _liftElapsed >= HangingLiftRampSeconds)
            {
                _nativeLiftCompletionLogged = true;
                var visualRoot = visuals.GetGlobalFrame().origin;
                var entityRoot = visuals.GetEntity()?.GetGlobalFrame().origin ?? Vec3.Invalid;
                var neck = ReadOriginalVictimNeckPoint();
                _host.LogInfo(
                    $"Drove native original-prisoner target-Z lift at end of tick: target=({_liftPosition.x:0.00}, {_liftPosition.y:0.00}, {_liftPosition.z:0.00}), " +
                    $"agentRoot=({victim.Position.x:0.00}, {victim.Position.y:0.00}, {victim.Position.z:0.00}), " +
                    $"visualRoot=({visualRoot.x:0.00}, {visualRoot.y:0.00}, {visualRoot.z:0.00}), " +
                    $"entityRoot=({entityRoot.x:0.00}, {entityRoot.y:0.00}, {entityRoot.z:0.00}), " +
                    $"neck=({neck.x:0.00}, {neck.y:0.00}, {neck.z:0.00}).");
            }
        }
        catch (Exception exception)
        {
            if (!_nativeLiftFailureLogged)
            {
                _nativeLiftFailureLogged = true;
                _host.LogWarning(
                    $"Could not drive the original prisoner through the native target-Z hanging path. {exception.Message}");
            }
        }
    }

    private GameEntity? TryCreateDropRope()
    {
        var neck = _host.VictimAgent?.GetEyeGlobalPosition() ?? Vec3.Invalid;
        if (!neck.IsValid)
        {
            return null;
        }

        neck.z -= HangingNeckDropFromEye;
        GameEntity? rope = null;
        if (GameEntity.PrefabExists(HangingDropRopeClothPrefab))
        {
            try
            {
                // Keep the rope as a collision-free visual and update its frame
                // from the original prisoner's neck every tick. Cloth simulation
                // would pin authored vertices and fight this transform tracking.
                rope = _host.SpawnPrefab(
                    HangingDropRopeClothPrefab,
                    neck,
                    createPhysics: false,
                    callScriptCallbacks: false);
            }
            catch (Exception exception)
            {
                _host.LogWarning(
                    $"Hanging drop rope prefab '{HangingDropRopeClothPrefab}' failed; falling back to the rein mesh. {exception.Message}");
                rope = null;
            }
        }

        if (rope is null)
        {
            var reinMesh = MetaMesh.GetCopy(
                HangingDropRopeReinMesh,
                showErrors: false,
                mayReturnNull: true);
            if (reinMesh is null || !reinMesh.IsValid)
            {
                return null;
            }

            rope = GameEntity.CreateEmpty(
                _host.Mission.Scene,
                isModifiableFromEditor: false,
                createPhysics: false,
                callScriptCallbacks: false);
            if (rope is null)
            {
                return null;
            }

            rope.AddMultiMesh(reinMesh);
        }

        rope.SetMobility(GameEntity.Mobility.Stationary);
        if (!_host.TryDisableEntityCollision(
                rope,
                "hanging drop rope (cloth/rein visual)"))
        {
            _host.RemoveSpawnedEntity(rope);
            return null;
        }

        var localMin = rope.GetBoundingBoxMin();
        var localMax = rope.GetBoundingBoxMax();
        var sizeX = MathF.Max(0.0001f, localMax.x - localMin.x);
        var sizeY = MathF.Max(0.0001f, localMax.y - localMin.y);
        var sizeZ = MathF.Max(0.0001f, localMax.z - localMin.z);
        _ropeLengthAxis = sizeY >= sizeX && sizeY >= sizeZ
            ? 1
            : sizeX >= sizeY && sizeX >= sizeZ
                ? 0
                : 2;
        _ropeBaseLength = _ropeLengthAxis == 0
            ? sizeX
            : _ropeLengthAxis == 1
                ? sizeY
                : sizeZ;
        _ropeCenterLocal = new Vec3(
            (localMin.x + localMax.x) * 0.5f,
            (localMin.y + localMax.y) * 0.5f,
            (localMin.z + localMax.z) * 0.5f);

        var beamHeight = _host.Placement is null
            ? 0f
            : _host.Placement.Origin.z + 5.36f;
        var targetLength = beamHeight - neck.z;
        if (targetLength < HangingDropRopeMinimumLength ||
            targetLength > HangingDropRopeMaximumLength)
        {
            _host.RemoveSpawnedEntity(rope);
            return null;
        }

        _beamAnchor = new Vec3(neck.x, neck.y, beamHeight);
        _dropRope = rope;
        if (!ApplyRopeFrame(neck, out var appliedScale))
        {
            _host.RemoveSpawnedEntity(rope);
            _dropRope = null;
            return null;
        }

        _host.LogInfo(
            $"Hanging drop rope created: beam={beamHeight:0.00}m, neck={neck.z:0.00}m, " +
            $"length={targetLength:0.00}m, localLength={_ropeBaseLength:0.00}m, scale={appliedScale:0.00}, " +
            $"lengthAxis={_ropeLengthAxis}.");
        return rope;
    }

    private bool ApplyRopeFrame(Vec3 neckPoint, out float scaleFactor)
    {
        scaleFactor = 0f;
        if (_dropRope is null || !_beamAnchor.IsValid || _ropeBaseLength <= 0.01f)
        {
            return false;
        }

        var horizontalDx = neckPoint.x - _beamAnchor.x;
        var horizontalDy = neckPoint.y - _beamAnchor.y;
        if (horizontalDx * horizontalDx + horizontalDy * horizontalDy >
            HangingRopeMaxHorizontalDrift * HangingRopeMaxHorizontalDrift)
        {
            return false;
        }

        var clampedNeck = neckPoint;
        clampedNeck.z = MathF.Max(
            clampedNeck.z,
            _beamAnchor.z - HangingDropRopeMaximumLength);
        var targetLength = _beamAnchor.z - clampedNeck.z;
        if (targetLength < HangingDropRopeMinimumLength ||
            targetLength > HangingDropRopeMaximumLength)
        {
            return false;
        }

        scaleFactor = targetLength / _ropeBaseLength * HangingDropRopeLengthSlack;
        if (!IsFinite(scaleFactor) || scaleFactor <= 0.01f || scaleFactor > 20f)
        {
            return false;
        }

        var frame = _dropRope.GetGlobalFrame();
        frame.rotation = Mat3.Identity;
        if (_ropeLengthAxis == 0)
        {
            frame.rotation.RotateAboutForward(MathF.PI * 0.5f);
        }
        else if (_ropeLengthAxis == 1)
        {
            frame.rotation.RotateAboutSide(MathF.PI * 0.5f);
        }

        var localScale = _ropeLengthAxis == 0
            ? new Vec3(scaleFactor, 1f, 1f)
            : _ropeLengthAxis == 1
                ? new Vec3(1f, scaleFactor, 1f)
                : new Vec3(1f, 1f, scaleFactor);
        frame.rotation.ApplyScaleLocal(in localScale);
        var rotatedCenter = frame.rotation.TransformToParent(_ropeCenterLocal);
        var midZ = (_beamAnchor.z + clampedNeck.z) * 0.5f;
        frame.origin = new Vec3(clampedNeck.x, clampedNeck.y, midZ) - rotatedCenter;
        _dropRope.SetGlobalFrame(in frame, isTeleportation: true);
        return true;
    }

    private void UpdateRopeToNeck()
    {
        var noose = _host.HangingNooseEntity;
        if (_dropRope is null && noose is null)
        {
            return;
        }

        try
        {
            var neck = ReadOriginalVictimNeckPoint();
            if (!neck.IsValid)
            {
                return;
            }

            if (!_firstUpdateLogged)
            {
                _firstUpdateLogged = true;
                _host.LogInfo(
                    $"Hanging rope first update: neck=({neck.x:0.00}, {neck.y:0.00}, {neck.z:0.00}), " +
                    $"beam=({_beamAnchor.x:0.00}, {_beamAnchor.y:0.00}, {_beamAnchor.z:0.00}), " +
                    $"victim=original, liftPos=({_liftPosition.x:0.00}, {_liftPosition.y:0.00}, {_liftPosition.z:0.00}).");
            }

            if (_dropRope is not null)
            {
                ApplyRopeFrame(neck, out _);
            }

            if (noose is not null)
            {
                var nooseFrame = noose.GetGlobalFrame();
                if (!_nooseBoundsResolved)
                {
                    var localMin = noose.GetBoundingBoxMin();
                    var localMax = noose.GetBoundingBoxMax();
                    if (localMin.IsValid && localMax.IsValid &&
                        IsFinite(localMin.x) && IsFinite(localMin.y) && IsFinite(localMin.z) &&
                        IsFinite(localMax.x) && IsFinite(localMax.y) && IsFinite(localMax.z))
                    {
                        _nooseCenterLocal = (localMin + localMax) * 0.5f;
                    }
                    else
                    {
                        _nooseCenterLocal = new Vec3(0f, 0f, 0f);
                    }

                    _nooseBoundsResolved = true;
                    _host.LogInfo(
                        $"Hanging noose bounds resolved: localMin=({localMin.x:0.000}, {localMin.y:0.000}, {localMin.z:0.000}), " +
                        $"localMax=({localMax.x:0.000}, {localMax.y:0.000}, {localMax.z:0.000}), " +
                        $"localCenter=({_nooseCenterLocal.x:0.000}, {_nooseCenterLocal.y:0.000}, {_nooseCenterLocal.z:0.000}).");
                }

                if (TryReadOriginalVictimHeadWorldFrame(out var headWorldFrame))
                {
                    if (!_nooseHeadBindingCaptured)
                    {
                        _nooseRelativeToHeadFrame =
                            headWorldFrame.TransformToLocal(in nooseFrame);
                        _nooseHeadBindingCaptured = true;
                    }
                    else
                    {
                        nooseFrame = headWorldFrame.TransformToParent(in _nooseRelativeToHeadFrame);
                    }

                    if (!_nooseRotationFollowLogged)
                    {
                        _nooseRotationFollowLogged = true;
                        _host.LogInfo(
                            "Hanging noose captured the initial head-to-noose relative frame; " +
                            "subsequent ticks follow the live head-bone rotation while retaining authored scale.");
                    }
                }

                var rotatedCenter = nooseFrame.rotation.TransformToParent(_nooseCenterLocal);
                nooseFrame.origin = neck - rotatedCenter;
                noose.SetGlobalFrame(in nooseFrame, isTeleportation: true);
                if (!_noosePlacementLogged)
                {
                    _noosePlacementLogged = true;
                    _host.LogInfo(
                        $"Hanging noose first centered placement: neck=({neck.x:0.000}, {neck.y:0.000}, {neck.z:0.000}), " +
                        $"entityOrigin=({nooseFrame.origin.x:0.000}, {nooseFrame.origin.y:0.000}, {nooseFrame.origin.z:0.000}), " +
                        $"rotatedLocalCenter=({rotatedCenter.x:0.000}, {rotatedCenter.y:0.000}, {rotatedCenter.z:0.000}).");
                }
            }
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not update the hanging rope visuals to the original prisoner's neck. {exception.Message}");
        }
    }

    private Vec3 ReadOriginalVictimNeckPoint()
    {
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return Vec3.Invalid;
        }

        if (TryReadOriginalVictimHeadWorldFrame(out var headWorldFrame))
        {
            // The named head bone begins at the neck joint for the human
            // action set. Apply the eye drop only to the fallback below.
            return headWorldFrame.origin;
        }

        var eye = victim.GetEyeGlobalPosition();
        eye.z -= HangingNeckDropFromEye;
        return eye;
    }

    private bool TryReadOriginalVictimHeadWorldFrame(out MatrixFrame headWorldFrame)
    {
        headWorldFrame = MatrixFrame.Identity;
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return false;
        }

        try
        {
            var visuals = victim.AgentVisuals;
            var skeleton = visuals?.GetSkeleton();
            if (visuals is null || skeleton is null || !skeleton.IsValid)
            {
                return false;
            }

            // This named skeleton read is the path already proven safe in game.
            // Do not replace it with the lower-level MBAgentVisuals bone call.
            skeleton.ForceUpdateBoneFrames();
            var headFrame = skeleton.GetBoneEntitialFrameWithName("head");
            var agentFrame = visuals.GetGlobalFrame();
            headWorldFrame = agentFrame.TransformToParent(in headFrame);
            return headWorldFrame.origin.IsValid;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void MaintainHangingFacialSuppression()
    {
        if (_phase == HangingPhase.None && !_suspensionFrozen)
        {
            return;
        }

        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        try
        {
            var keepEyesClosed = _phase is HangingPhase.DeathPose or HangingPhase.Completed ||
                                 _suspensionFrozen;
            victim.SetAgentFacialAnimation(
                Agent.FacialAnimChannel.High,
                keepEyesClosed ? ClosedEyesFacialAction : string.Empty,
                keepEyesClosed);
            victim.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, string.Empty, false);
            victim.SetAgentFacialAnimation(Agent.FacialAnimChannel.Low, string.Empty, false);
            if (!_facialSuppressionLogged)
            {
                _facialSuppressionLogged = true;
                _host.LogInfo(
                    "Hanging facial suppression started with the lift; spontaneous facial channels are cleared every tick.");
            }

            if (keepEyesClosed && !_closedEyesLogged)
            {
                _closedEyesLogged = true;
                _host.LogInfo(
                    $"Hanging death presentation is pinning facial High to '{ClosedEyesFacialAction}' every tick; " +
                    "the suspended prisoner remains permanently closed-eyed.");
            }
        }
        catch (Exception exception)
        {
            if (!_facialSuppressionFailureLogged)
            {
                _facialSuppressionFailureLogged = true;
                _host.LogWarning(
                    $"Could not suppress hanging facial animation; the mission remains active. {exception.Message}");
            }
        }
    }

    private void TryPlayVictimVoice(
        SkinVoiceManager.SkinVoiceType voice,
        string purpose)
    {
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        try
        {
            victim.MakeVoice(
                voice,
                SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
            _host.LogInfo($"Requested hanging prisoner voice '{voice.TypeID}' for {purpose}.");
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not play the hanging prisoner voice for {purpose}; the sequence continues. {exception.Message}");
        }
    }

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private static float SmoothStep(float t)
    {
        var clamped = MathF.Max(0f, MathF.Min(1f, t));
        return clamped * clamped * (3f - 2f * clamped);
    }
}
