using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    // IExecutionSceneHost implementation. The strategy receives these shared
    // capabilities; it owns its own flow/death-chain decisions.
    // ------------------------------------------------------------------
    Agent? IExecutionSceneHost.VictimAgent => _victimAgent;

    Agent? IExecutionSceneHost.ExecutionerAgent => _executionerAgent;

    Agent? IExecutionSceneHost.PlayerAgent => _playerAgent;

    ExecutionActor IExecutionSceneHost.Actor => _actor;

    IExecutionSceneAct? IExecutionSceneHost.SceneAct => _sceneAct;

    ExecutionScenePlacement? IExecutionSceneHost.Placement => _placement;

    Vec3 IExecutionSceneHost.VictimPosition
    {
        get => _victimPosition;
        set => _victimPosition = value;
    }

    Vec3 IExecutionSceneHost.ExecutionActorPosition => GetExecutionActorPosition();

    Vec2 IExecutionSceneHost.ExecutionActorDirection => GetExecutionActorDirection();

    Vec3 IExecutionSceneHost.HangingLiftPosition
    {
        get => _hangingLiftPosition;
        set => _hangingLiftPosition = value;
    }

    GameEntity? IExecutionSceneHost.HangingNooseEntity => _profileHangingNoose;

    GameEntity? IExecutionSceneHost.HangingSupportEntity => _gallowsEntity;

    bool IExecutionSceneHost.ActionStarted
    {
        get => _actionStarted;
        set => _actionStarted = value;
    }

    bool IExecutionSceneHost.LethalAttempted
    {
        get => _lethalAttempted;
        set => _lethalAttempted = value;
    }

    bool IExecutionSceneHost.MethodSequenceCompleted
    {
        get => _methodExecutionPhase == MethodExecutionPhase.Completed;
        set { }
    }

    float IExecutionSceneHost.MaximumExecutionActionProgress
    {
        get => _maximumExecutionActionProgress;
        set => _maximumExecutionActionProgress = value;
    }

    float IExecutionSceneHost.ExecutionActionAttemptElapsed
    {
        get => _executionActionAttemptElapsed;
        set => _executionActionAttemptElapsed = value;
    }

    void IExecutionSceneHost.PoseAgentAt(Agent agent, Vec3 position, Vec2 direction) =>
        PoseAgentAt(agent, position, direction);

    bool IExecutionSceneHost.TryPlayAction(Agent agent, string actionName, string purpose) =>
        TryPlayRequiredAction(agent, actionName, purpose, out _, forceFullBody: false);

    void IExecutionSceneHost.LogInfo(string message) => RexLog.Info(message);

    void IExecutionSceneHost.LogWarning(string message) => RexLog.Warning(message);

    void IExecutionSceneHost.LogError(string message, System.Exception? exception) =>
        RexLog.Error(message, exception);

    void IExecutionSceneHost.RequestLethalFrame() => CommitLethalFrame();

    ExecutionVictimDeathResult IExecutionSceneHost.ApplySharedBattlefieldDeath(Agent? visualActor) =>
        ApplySharedBattlefieldDeath(visualActor);

    void IExecutionSceneHost.CreateDetachedHead() => TryCreateDetachedHeadVisual();

    void IExecutionSceneHost.RestoreDetachedHeadSourceIfUncommitted() =>
        RestoreDetachedHeadSourceIfExecutionWasNotCommitted();

    void IExecutionSceneHost.PlayHangingReleaseSound() => TryPlayHangingReleaseSound();

    bool IExecutionSceneHost.TryStartExecutionActionClip(Agent? actionAgent, string actionName, bool isGenericFallback) =>
        TryStartExecutionActionClip(actionAgent, actionName, isGenericFallback);

    float IExecutionSceneHost.ObserveExecutionAction(Agent? actionAgent) =>
        ObserveExecutionAction(actionAgent);

    void IExecutionSceneHost.ReassertExecutionActionRoot(Agent? actionAgent, string phase) =>
        ReassertExecutionActionRoot(actionAgent, phase);

    void IExecutionSceneHost.EnterPostLethalSceneState(bool applyCrowdReaction) =>
        EnterPostLethalSceneState(applyCrowdReaction, aftermathMessageOverride: null);

    void IExecutionSceneHost.CompleteMethodExecutionSequence(string finalPose) =>
        CompleteMethodExecutionSequence(finalPose);

    void IExecutionSceneHost.StartStagedMethodExecutionAction() => StartStagedMethodExecutionAction();

    bool IExecutionSceneHost.TryEnsureExecutionAxeWielded(Agent agent, string purpose) =>
        TryEnsureExecutionAxeWielded(agent, purpose);

    Vec3 IExecutionSceneHost.GetExecutionActorPosition() => GetExecutionActorPosition();

    Vec2 IExecutionSceneHost.GetExecutionActorDirection() => GetExecutionActorDirection();

    bool IExecutionSceneHost.TryPlayRequiredAction(
        Agent? agent,
        string actionName,
        string purpose,
        bool forceFullBody,
        float blendInPeriod) =>
        TryPlayRequiredAction(
            agent,
            actionName,
            purpose,
            out _,
            forceFullBody,
            blendInPeriod);

    bool IExecutionSceneHost.IsStageReady =>
        _initialized && (UsesGroundExecutionSite() || _gallowsEntity is not null);

    Mission IExecutionSceneHost.Mission => Mission;

    GameEntity? IExecutionSceneHost.SpawnPrefab(string prefabName, Vec3 position, bool createPhysics, bool callScriptCallbacks, float yawDegrees, float pitchDegrees, float rollDegrees, float uniformScale) =>
        SpawnPrefab(prefabName, position, createPhysics, callScriptCallbacks, yawDegrees, pitchDegrees, rollDegrees, uniformScale);

    bool IExecutionSceneHost.RemoveSpawnedEntity(GameEntity? entity) =>
        RemoveSpawnedEntity(entity);

    bool IExecutionSceneHost.TryDisableEntityCollision(GameEntity entity, string purpose) =>
        TryDisableEntityCollision(entity, purpose);

    Agent? IExecutionSceneHost.GetActiveExecutionActor() =>
        _actor == ExecutionActor.Player ? _playerAgent : _executionerAgent;

    string IExecutionSceneHost.GetCeremonyExecutionActionName() => _ceremonyExecutionActionName;

    bool IExecutionSceneHost.IsStagedMethodInProgress() => UsesStagedMethodExecution();

    bool IExecutionSceneHost.HasStartedMethodSequence() => _methodExecutionPhase != MethodExecutionPhase.None;

    bool IExecutionSceneHost.TryClearExecutionActionChannel(Agent actionAgent) =>
        TryClearExecutionActionChannel(actionAgent);

    ActionIndexCache IExecutionSceneHost.GetVictimCurrentAction() =>
        _victimAgent?.GetCurrentAction(0) ?? ActionIndexCache.act_none;

    float IExecutionSceneHost.GetVictimCurrentActionProgress() =>
        _victimAgent?.GetCurrentActionProgress(0) ?? -1f;

    bool IExecutionSceneHost.SaveImpaledCorpseDisplay(string actionName, float actionProgress) =>
        SaveImpaledCorpseDisplay(actionName, actionProgress);

    void IExecutionSceneHost.StartMethodExecutionAction() => StartExecutionAction();

    void IExecutionSceneHost.TickMethodExecutionDispatch(float dt) => TickExecution(dt);

    bool IExecutionSceneHost.TryStartVisibleExecutionFallback(Agent? actionAgent, string failedActionName) =>
        TryStartVisibleExecutionFallback(actionAgent, failedActionName);

    void IExecutionSceneHost.ContinueAfterAnimationFailure(string detail) =>
        ContinueAfterAnimationFailure(detail);

    bool IExecutionSceneHost.TryIgniteExecutionEffectRoles(string stage, params string[] roleIds) =>
        TryIgniteExecutionEffectRoles(stage, roleIds);

    bool IExecutionSceneHost.TryPlayWorldParticleBurst(string particleSystemName, Vec3 position, string purpose) =>
        TryPlayWorldParticleBurst(particleSystemName, position, purpose);

    bool IExecutionSceneHost.ReleaseExecutionActorControl(string purpose) =>
        ReleaseExecutionActorControl(purpose);

    bool IExecutionSceneHost.TryStartStoningFirstThrow(
        Agent thrower,
        bool preservePlayerControl) =>
        TryStartStoningFirstThrow(thrower, preservePlayerControl);

    void IExecutionSceneHost.DriveCrossbowVolleyAction(string actionName, string purpose) =>
        DriveCrossbowVolleyAction(actionName, purpose);

    bool IExecutionSceneHost.LaunchCrossbowVolley(Agent primaryShooter, string trigger) =>
        LaunchCrossbowVolley(primaryShooter, trigger);

    void IExecutionSceneHost.PlayBeheadingLethalEffects() => PlayLethalEffects();

    bool IExecutionSceneHost.DisableCollisionKeepCloth(GameEntity entity, string purpose) =>
        DisableCollisionKeepCloth(entity, purpose);

    bool IExecutionSceneHost.FreezeVictimForSuspension(Vec3 position, string role)
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive())
        {
            RexLog.Warning(
                $"Cannot freeze the prisoner for {role}; the agent is unavailable.");
            return false;
        }

        try
        {
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            // Keep the native post-animation movement channel open. This is the
            // same target-Z/gravity-exclusion mechanism used by the vanilla
            // climbing machine for a human Agent above the navigation surface.
            victim.SetIsPhysicsForceClosed(false);
            victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
            // Keep the current channel-0 pose without invoking a death action
            // override: SetOverridenStrikeAndDeathAction tells the engine the
            // agent is dying, which removes it as Unconscious/Killed. For the
            // fake-death hanging we only freeze position and AI; the campaign
            // hero is settled on mission exit instead.
            var current = victim.GetCurrentAction(0);
            if (current != ActionIndexCache.act_none)
            {
                var progress = victim.GetCurrentActionProgress(0);
                victim.SetCurrentActionProgress(0, MathF.Max(0f, MathF.Min(1f, progress)));
                victim.SetCurrentActionSpeed(0, 0f);
            }

            var direction = victim.LookDirection;
            var horizontalDirection = direction.AsVec2;
            if (!horizontalDirection.IsNonZero())
            {
                horizontalDirection = _placement?.Forward ?? Vec2.Forward;
            }

            horizontalDirection = horizontalDirection.Normalized();
            direction = new Vec3(horizontalDirection.x, horizontalDirection.y, 0f);
            var targetUp = new Vec3(0f, 0f, 1f);
            victim.SetTargetZ(position.z);
            victim.SetTargetPositionAndDirection(position.AsVec2, direction);
            victim.SetTargetUp(in targetUp);
            _hangingSuspensionActive = true;
            _hangingSuspensionProbeElapsed = 0f;
            MaintainFrozenVictimFacialSuppression();
            TryMoveFrozenVictimToPlayerTeam(victim, role);
            RexLog.Info(
                $"Froze the prisoner for {role} at ({position.x:0.00}, {position.y:0.00}, " +
                $"{position.z:0.00}); fake-death suspension active, no RegisterBlow was used.");
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                victim.SetCurrentActionSpeed(0, 1f);
                victim.ClearTargetFrame();
                var zeroUp = Vec3.Zero;
                victim.SetTargetUp(in zeroUp);
                victim.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: false);
                victim.SetIsPhysicsForceClosed(false);
                victim.SetIsAIPaused(false);
            }
            catch (Exception restoreException)
            {
                RexLog.Error(
                    $"Could not restore the prisoner after the {role} freeze failed.",
                    restoreException);
            }

            RexLog.Error($"Could not freeze the prisoner for {role}.", exception);
            return false;
        }
    }

    bool IExecutionSceneHost.SetVictimPresentationFrame(
        Vec3 position,
        Vec2 direction,
        Vec3 up,
        bool freezeAction,
        bool closeEyes,
        string role,
        Vec3? facing)
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive() || !position.IsValid)
        {
            return false;
        }

        try
        {
            var safeDirection = direction.IsNonZero() ? direction.Normalized() : Vec2.Forward;
            var safeUp = up.IsNonZero ? up.NormalizedCopy() : Vec3.Up;
            var forward3 = facing ?? new Vec3(safeDirection.x, safeDirection.y, 0f);
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            victim.SetIsPhysicsForceClosed(false);
            victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
            victim.SetTargetZ(position.z);
            victim.SetTargetPositionAndDirection(position.AsVec2, forward3);
            victim.SetTargetUp(in safeUp);
            if (freezeAction)
            {
                var current = victim.GetCurrentAction(0);
                if (current != ActionIndexCache.act_none)
                {
                    victim.SetCurrentActionSpeed(0, 0f);
                }
            }

            if (closeEyes)
            {
                _hangingSuspensionActive = true;
                MaintainFrozenVictimFacialSuppression();
                TryMoveFrozenVictimToPlayerTeam(victim, role);
            }

            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not maintain prisoner presentation frame for {role}.", exception);
            return false;
        }
    }

    GameEntity? IExecutionSceneHost.GetProfileEntity(string roleId) =>
        _profileEntities.TryGetValue(roleId, out var entity) ? entity : null;

    Vec3 IExecutionSceneHost.GetMethodWorldPoint(float right, float forward, float up) =>
        _placement?.Offset(
            right + GetMethodCeremonyLateralShift(),
            forward,
            up + _stageSurfaceHeight) ?? Vec3.Invalid;

    bool IExecutionSceneHost.UpdateMultiStageUsePoint(
        Vec3 position,
        TextObject description,
        TextObject action,
        bool enabled) =>
        UpdateMultiStageUsePoint(position, description, action, enabled);

    Agent? IExecutionSceneHost.SpawnMethodHelper(
        Vec3 position,
        Vec2 direction,
        string role,
        int cultureSlot) =>
        SpawnMethodHelper(position, direction, role, cultureSlot);

    bool IExecutionSceneHost.MoveAgentToStagePoint(
        Agent agent,
        Vec3 position,
        Vec2 direction,
        string purpose) =>
        MoveAgentToStagePoint(agent, position, direction, purpose);

    bool IExecutionSceneHost.BeginPlayerStageControl(Vec2 direction, string purpose) =>
        BeginPlayerStageControl(direction, purpose);

    bool IExecutionSceneHost.EndPlayerStageControl(string purpose) =>
        EndPlayerStageControl(purpose);

    bool IExecutionSceneHost.TryGetVictimBoneWorldPosition(sbyte boneIndex, out Vec3 position) =>
        TryGetVictimBoneWorldPosition(boneIndex, out position);

    void IExecutionSceneHost.PlayVictimStageBlood(
        sbyte boneIndex,
        float intensity,
        int worldBurstCount,
        string purpose) =>
        PlayVictimStageBlood(boneIndex, intensity, worldBurstCount, purpose);

    void IExecutionSceneHost.PlayVictimVoice(
        SkinVoiceManager.SkinVoiceType voice,
        string purpose) =>
        PlayVictimVoice(voice, purpose);

    bool IExecutionSceneHost.SetEntityBetweenPoints(
        GameEntity entity,
        Vec3 start,
        Vec3 end,
        string purpose) =>
        SetEntityBetweenPoints(entity, start, end, purpose);

}
