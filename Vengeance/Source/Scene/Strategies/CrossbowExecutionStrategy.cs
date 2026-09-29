using System;
using RichExecutions.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Draws the native light crossbow, holds the aimed pose for two full seconds,
/// then starts the release clip and creates real native bolts on the same Tick.
/// At calculated impact time the original prisoner blends into the custom static
/// crucifix death pose and remains alive/frozen until mission-exit settlement,
/// avoiding native ragdoll and corpse-pool removal. A real pre-ceremony combat
/// kill is still handled independently by the host's external-Killed path.
/// </summary>
internal sealed class CrossbowExecutionStrategy : StagedPunishmentExecutionStrategy
{
    private const string ReadyAction = "act_ready_crossbow";
    private const string ReadyHoldAction = "act_ready_continue_crossbow";
    private const float ReadyToHoldSeconds = 0.65f;
    private const float AimedHoldSeconds = 2.00f;
    private const float NativeBoltSpeed = 70.00f;
    private const float PrimaryNpcAimCenterDelaySeconds = 0.30f;
    private const float MinimumBoltFlightSeconds = 0.08f;
    private const float MaximumBoltFlightSeconds = 0.30f;
    private const float DeathPoseBlendSeconds = 0.40f;

    private bool _aimStarted;
    private bool _primaryReadyStarted;
    private bool _holdAttempted;
    private bool _releaseStarted;
    private bool _boltFlightStarted;
    private bool _deathPoseStarted;
    private bool _lethalFrameRequested;
    private float _aimElapsed;
    private float _boltFlightElapsed;
    private float _expectedBoltFlightSeconds;
    private float _deathPoseElapsed;

    public override string MethodId => ExecutionMethodRules.CrossbowExecution;

    protected override float ReactionSeconds => 0.90f;

    public override bool DefersVictimDeathToMissionExit => true;

    protected override string SequenceDescription =>
        "two-second aimed hold, staggered native bolt release, and frozen crucifix death pose";

    public override void StartExecutionAction()
    {
        if (_aimStarted || _releaseStarted || _boltFlightStarted || _lethalFrameRequested)
        {
            return;
        }

        _aimStarted = true;
        _aimElapsed = 0f;
        Host.ActionStarted = true;
        var actor = Host.GetActiveExecutionActor();
        if (actor is null || !actor.IsActive())
        {
            Host.LogError(
                "The crossbow execution actor is unavailable before the two-second aimed hold.");
            return;
        }

        if (Host.Actor == ExecutionActor.Executioner)
        {
            Host.LogInfo(
                $"NPC crossbow firing line begins from the visual left; the center executioner " +
                $"aim is delayed by {PrimaryNpcAimCenterDelaySeconds:0.00}s after the two left supports.");
            Host.DriveCrossbowVolleyAction(ReadyAction, "left-to-right volley ready");
        }
        else if (!Host.TryStartExecutionActionClip(actor, ReadyAction, isGenericFallback: false))
        {
            Host.LogWarning(
                $"Crossbow ready action '{ReadyAction}' could not bind; the actor will retain the loaded " +
                "crossbow at the firing mark while the same two-second hold continues.");
        }

        if (Host.Actor != ExecutionActor.Executioner)
        {
            Host.DriveCrossbowVolleyAction(ReadyAction, "volley ready");
        }

        Host.LogInfo(
            $"Began crossbow aim at the actor's current firing mark; holding for {AimedHoldSeconds:0.00}s " +
            "before the native release action.");
    }

    public override void TickExecution(float dt)
    {
        if (_lethalFrameRequested)
        {
            return;
        }

        var safeDt = MathF.Max(0f, dt);
        if (_deathPoseStarted)
        {
            _deathPoseElapsed += safeDt;
            if (_deathPoseElapsed >= DeathPoseBlendSeconds)
            {
                _lethalFrameRequested = true;
                Host.LogInfo(
                    $"Completed the {DeathPoseBlendSeconds:0.00}s crossbow crucifix death-pose blend; " +
                    "requesting the deferred frozen lethal frame without native ragdoll.");
                Host.RequestLethalFrame();
            }

            return;
        }

        if (_boltFlightStarted)
        {
            _boltFlightElapsed += safeDt;
            if (_boltFlightElapsed >= _expectedBoltFlightSeconds)
            {
                StartCrucifixDeathPose();
            }

            return;
        }

        if (!_aimStarted)
        {
            StartExecutionAction();
        }

        var actor = Host.GetActiveExecutionActor();
        if (actor is null || !actor.IsActive())
        {
            return;
        }

        Host.ReassertExecutionActionRoot(actor, MethodId + " aimed hold");

        if (!_releaseStarted)
        {
            _aimElapsed += safeDt;
            if (Host.Actor == ExecutionActor.Executioner &&
                _aimElapsed < PrimaryNpcAimCenterDelaySeconds)
            {
                Host.ObserveExecutionAction(actor);
                return;
            }

            if (Host.Actor == ExecutionActor.Executioner &&
                !_holdAttempted &&
                !_primaryReadyStarted &&
                _aimElapsed >= PrimaryNpcAimCenterDelaySeconds)
            {
                _primaryReadyStarted = true;
                if (!Host.TryStartExecutionActionClip(actor, ReadyAction, isGenericFallback: false))
                {
                    Host.LogWarning(
                        $"Crossbow ready action '{ReadyAction}' could not bind after the left supports; " +
                        "the executioner retains the loaded crossbow at the center firing mark.");
                }

            }

            Host.ObserveExecutionAction(actor);
            if (!_holdAttempted && _aimElapsed >= ReadyToHoldSeconds)
            {
                _holdAttempted = true;
                if (Host.TryStartExecutionActionClip(
                        actor,
                        ReadyHoldAction,
                        isGenericFallback: false))
                {
                    Host.LogInfo(
                        $"Crossbow actor entered native aimed hold '{ReadyHoldAction}' and remains fixed on target.");
                }
                else
                {
                    Host.LogWarning(
                        $"Crossbow aimed hold '{ReadyHoldAction}' could not bind; retaining the current ready pose " +
                    "until the two-second release time.");
                }

                Host.DriveCrossbowVolleyAction(ReadyHoldAction, "volley aimed hold");
            }

            if (_aimElapsed < ReadyToHoldSeconds + AimedHoldSeconds)
            {
                return;
            }

            _releaseStarted = true;
            Host.MaximumExecutionActionProgress = 0f;
            Host.ExecutionActionAttemptElapsed = 0f;
            var releaseAction = Host.SceneAct?.ExecutionAction ?? "act_release_crossbow";
            Host.DriveCrossbowVolleyAction(releaseAction, "volley release");
            if (!Host.TryStartExecutionActionClip(
                    actor,
                    releaseAction,
                    isGenericFallback: false))
            {
                Host.LogError(
                    $"Crossbow release action '{releaseAction}' could not bind after the full aimed hold; " +
                    "launching the same native combat bolt on this release tick without cancelling the mission.");
            }
            else
            {
                Host.LogInfo(
                    $"Completed the draw plus {AimedHoldSeconds:0.00}s crossbow hold and started native release " +
                    $"'{releaseAction}' from the unchanged firing mark.");
            }

            BeginNativeBoltFlight(actor, releaseAction);
            return;
        }
    }

    private void BeginNativeBoltFlight(Agent shooter, string releaseAction)
    {
        var victim = Host.VictimAgent;
        var distance = victim is not null
            ? shooter.GetEyeGlobalPosition().Distance(victim.GetChestGlobalPosition())
            : NativeBoltSpeed * 0.18f;
        _expectedBoltFlightSeconds = MathF.Max(
            MinimumBoltFlightSeconds,
            MathF.Min(MaximumBoltFlightSeconds, distance / NativeBoltSpeed));
        _boltFlightElapsed = 0f;
        _boltFlightStarted = true;

        var launched = Host.LaunchCrossbowVolley(
            shooter,
            "same-tick native release after two-second aim");
        Host.LogInfo(
            $"Crossbow release '{releaseAction}' and native bolt creation occurred on the same mission tick; " +
            $"launched={launched}, distance={distance:0.00}m, " +
            $"expectedFlight={_expectedBoltFlightSeconds:0.000}s.");
    }

    private void StartCrucifixDeathPose()
    {
        if (_deathPoseStarted)
        {
            return;
        }

        _deathPoseStarted = true;
        _deathPoseElapsed = 0f;
        var victim = Host.VictimAgent;
        var deathAction = Host.SceneAct?.DeathAction ?? "act_ver_burning_crucifix_death";
        if (victim is null || !victim.IsActive())
        {
            Host.LogError(
                "The original crossbow prisoner became unavailable at the calculated bolt impact time; " +
                "the deferred lethal frame will still be requested without spawning a replacement.");
            return;
        }

        if (!Host.TryPlayRequiredAction(
                victim,
                deathAction,
                "crossbow crucifix static death at bolt impact",
                forceFullBody: true,
                blendInPeriod: DeathPoseBlendSeconds))
        {
            Host.LogWarning(
                $"Crossbow death pose '{deathAction}' could not bind at the calculated impact time; " +
                "the current restrained pose will be frozen after the same blend interval.");
        }

        Host.LogInfo(
            $"Reached calculated native bolt impact after {_expectedBoltFlightSeconds:0.000}s; " +
            $"started crucifix death pose '{deathAction}' with a {DeathPoseBlendSeconds:0.00}s blend.");
    }

    public override bool ApplyDeath()
    {
        var victim = Host.VictimAgent;
        if (victim is null || !victim.IsActive() || !Host.VictimPosition.IsValid)
        {
            Host.LogError(
                "The original crossbow prisoner or crucifix root was unavailable at the frozen lethal frame.");
            return false;
        }

        var frozen = Host.FreezeVictimForSuspension(
            Host.VictimPosition,
            "original-prisoner crossbow crucifix death pose");
        if (frozen)
        {
            victim.SetCurrentActionSpeed(0, 0f);
            Host.LogInfo(
                $"Original crossbow prisoner Agent {victim.Index} remains alive and fixed to the crucifix; " +
                "campaign execution is deferred until mission exit and native corpse/ragdoll removal is bypassed.");
        }

        return frozen;
    }
}
