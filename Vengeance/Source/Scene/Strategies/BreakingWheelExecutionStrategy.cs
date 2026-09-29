using System;
using RichExecutions.Core;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Five-stage breaking-wheel ceremony on the scaffold. Each strike is followed
/// by one 35-degree in-plane index rotation driven by a single guard. After the
/// fifth rotation the same guard automatically raises the wheel to vertical.
/// Campaign death remains deferred until mission exit and the original hero
/// Agent is kept as the fixed tableau body.
/// </summary>
internal sealed class BreakingWheelExecutionStrategy :
    IExecutionMethodStrategy,
    IMultiStageExecutionStrategy,
    IMethodSceneReadyStrategy
{
    private const float StrikeSeconds = 1.15f;
    private const float ImpactProgress = 0.58f;
    private const float IndexRotationSeconds = 0.55f;
    private const float IndexRotationRadians = 35f * MathF.PI / 180f;
    private const float RaiseSeconds = 2.80f;
    private const float NpcArrivalTimeoutSeconds = 3.0f;
    private const string StrikeAction = "act_smithing_machine_anvil_part_1";
    private const string GuardTurnAction = "act_usage_siege_tower_open_gate_begin";
    private const string VictimBoundAction = "act_ver_burning_crucifix_idle";
    private const string VictimDeathAction = "act_ver_burning_crucifix_death";

    private static readonly (float Right, float Forward)[] StrikePoints =
    {
        (-0.72f, 0.55f),
        (0.72f, 0.55f),
        (-0.80f, -0.38f),
        (0.80f, -0.38f),
        (0f, -0.92f)
    };

    private static readonly string[] UseDescriptions =
    {
        "{=REX_Wheel_Use_Left_Leg}Left leg strike point",
        "{=REX_Wheel_Use_Right_Leg}Right leg strike point",
        "{=REX_Wheel_Use_Left_Arm}Left arm strike point",
        "{=REX_Wheel_Use_Right_Arm}Right arm strike point",
        "{=REX_Wheel_Use_Chest}Chest strike point"
    };

    private static readonly string[] UseActions =
    {
        "{=REX_Wheel_Action_Left_Leg}strike the left leg",
        "{=REX_Wheel_Action_Right_Leg}strike the right leg",
        "{=REX_Wheel_Action_Left_Arm}strike the left arm",
        "{=REX_Wheel_Action_Right_Arm}strike the right arm",
        "{=REX_Wheel_Action_Chest}deliver the fatal chest blow"
    };

    private IExecutionSceneHost _host = null!;
    private GameEntity? _wheel;
    private MatrixFrame _wheelBaseFrame = MatrixFrame.Identity;
    private Agent? _wheelGuard;
    private int _stage;
    private bool _started;
    private bool _busy;
    private bool _movingToStage;
    private bool _rotatingAfterStrike;
    private bool _raisingWheel;
    private bool _impactApplied;
    private bool _lethalRequested;
    private bool _finalPoseFrozen;
    private bool _presentationComplete;
    private float _stageElapsed;
    private float _wheelSpinRadians;
    private float _rotationStartRadians;
    private float _raiseProgress;
    private Vec3 _operatorPoint = Vec3.Invalid;
    private Vec2 _operatorDirection = Vec2.Forward;

    public string MethodId => ExecutionMethodRules.BreakingWheel;
    public bool UsesStagedExecution => true;
    public bool RequiresExecutionAxe => false;
    public bool DefersVictimDeathToMissionExit => true;
    public bool IsWaitingForPlayerInput =>
        _started && _host.Actor == ExecutionActor.Player && !_busy && _stage < StrikePoints.Length;
    public bool IsStageBusy => _busy;
    public bool IsPresentationComplete => _presentationComplete;

    public void AttachHost(IExecutionSceneHost host) =>
        _host = host ?? throw new ArgumentNullException(nameof(host));

    public void SetupSceneVisuals()
    {
        // The runtime profile entities and F host are created later by the
        // shared scene builder; OnMethodSceneReady owns apparatus binding.
    }

    public void OnMethodSceneReady()
    {
        _wheel = _host.GetProfileEntity("breaking_wheel");
        if (_wheel is not null)
        {
            _wheelBaseFrame = _wheel.GetGlobalFrame();
            _host.TryDisableEntityCollision(_wheel, "moving breaking wheel");
        }
        else
        {
            _host.LogError("The core breaking-wheel entity is missing; progression remains paused for diagnosis.");
        }

        var guardPoint = _host.GetMethodWorldPoint(1.48f, -0.42f);
        _wheelGuard = _host.SpawnMethodHelper(
            guardPoint,
            DirectionToWheel(guardPoint),
            "wheel turn-and-raise guard",
            20);

        _host.TryPlayRequiredAction(
            _host.VictimAgent,
            VictimBoundAction,
            "wheel bound victim placeholder",
            forceFullBody: true,
            blendInPeriod: 0.4f);
        ApplyWheelFrame();
        MaintainVictimOnWheel(closeEyes: false, freezeAction: false);
        UpdateUsePoint(enabled: true);
    }

    public void TickSceneVisuals(float dt)
    {
        ApplyWheelFrame();
        MaintainVictimOnWheel(
            closeEyes: _lethalRequested,
            freezeAction: _lethalRequested);
    }

    public void PrefetchActions()
    {
        _host.TryPlayRequiredAction(
            _host.VictimAgent,
            VictimBoundAction,
            "wheel victim bound prefetch",
            forceFullBody: true,
            blendInPeriod: 0.4f);
        _host.TryPlayRequiredAction(
            _host.VictimAgent,
            VictimDeathAction,
            "wheel victim death prefetch",
            forceFullBody: true,
            blendInPeriod: 0.4f);
    }

    public void StartExecutionAction()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _host.ActionStarted = true;
        if (_host.Actor == ExecutionActor.Player)
        {
            BeginStage();
        }
        else
        {
            BeginNpcStageMovement();
        }
    }

    public bool TryAdvancePlayerStage()
    {
        if (!IsWaitingForPlayerInput)
        {
            return false;
        }

        BeginStage();
        return true;
    }

    public void TickExecution(float dt)
    {
        if (!_started || _presentationComplete)
        {
            return;
        }

        var safeDt = MathF.Max(0f, dt);
        if (_movingToStage)
        {
            TickNpcMovement(safeDt);
            return;
        }

        if (_rotatingAfterStrike)
        {
            TickIndexRotation(safeDt);
            return;
        }

        if (_raisingWheel)
        {
            TickRaiseStage(safeDt);
            return;
        }

        if (!_busy)
        {
            return;
        }

        _stageElapsed += safeDt;
        var actorAgent = GetOperator();
        if (actorAgent is not null && actorAgent.IsActive())
        {
            if (_host.Actor == ExecutionActor.Player)
            {
                _host.PoseAgentAt(actorAgent, _operatorPoint, _operatorDirection);
            }
            else
            {
                _host.ReassertExecutionActionRoot(actorAgent, $"wheel strike stage {_stage + 1}");
            }
        }

        if (!_impactApplied && _stageElapsed >= StrikeSeconds * ImpactProgress)
        {
            _impactApplied = true;
            ApplyStrikeImpact();
        }

        if (_stageElapsed >= StrikeSeconds)
        {
            CompleteStrikeStage();
        }
    }

    public void BeginMethodSequence(string trigger) { }
    public void TickMethodSequence(float dt) { }
    public void ApplyLethalFrameEffects() { }

    public bool ApplyDeath()
    {
        if (!_finalPoseFrozen)
        {
            _host.TryPlayRequiredAction(
                _host.VictimAgent,
                VictimDeathAction,
                "wheel fixed death placeholder",
                forceFullBody: true,
                blendInPeriod: 0.4f);
            _finalPoseFrozen = MaintainVictimOnWheel(closeEyes: true, freezeAction: true);
        }

        return _finalPoseFrozen;
    }

    public void TickAfterDeath(float dt) => TickExecution(dt);

    public void Cleanup() => UpdateUsePoint(enabled: false);

    private void TickNpcMovement(float dt)
    {
        _stageElapsed += dt;
        var actor = _host.ExecutionerAgent;
        if (actor is null || !actor.IsActive())
        {
            _host.LogError("The wheel executioner became unavailable; the mission remains open.");
            return;
        }

        if (actor.Position.AsVec2.Distance(_operatorPoint.AsVec2) > 0.22f &&
            _stageElapsed < NpcArrivalTimeoutSeconds)
        {
            return;
        }

        if (_stageElapsed >= NpcArrivalTimeoutSeconds)
        {
            _host.LogWarning("Wheel executioner movement timed out; posing the NPC on the current stage mark.");
            _host.PoseAgentAt(actor, _operatorPoint, _operatorDirection);
        }

        _movingToStage = false;
        BeginStageAction(actor);
    }

    private void BeginNpcStageMovement()
    {
        if (_stage >= StrikePoints.Length)
        {
            BeginFinalRaise();
            return;
        }

        var actor = _host.ExecutionerAgent;
        if (actor is null || !actor.IsActive())
        {
            return;
        }

        _operatorPoint = GetStagePoint(_stage);
        _operatorDirection = DirectionToWheel(_operatorPoint);
        _movingToStage = _host.MoveAgentToStagePoint(
            actor,
            _operatorPoint,
            _operatorDirection,
            $"wheel stage {_stage + 1}");
        _busy = true;
        _stageElapsed = 0f;
        if (!_movingToStage)
        {
            _host.PoseAgentAt(actor, _operatorPoint, _operatorDirection);
            BeginStageAction(actor);
        }
    }

    private void BeginStage()
    {
        if (_busy || _stage >= StrikePoints.Length)
        {
            return;
        }

        _busy = true;
        _stageElapsed = 0f;
        _impactApplied = false;
        UpdateUsePoint(enabled: false);
        var actor = GetOperator();
        if (actor is null || !actor.IsActive())
        {
            _host.LogError($"Wheel stage {_stage + 1} has no active operator; progression is paused.");
            _busy = false;
            UpdateUsePoint(enabled: _host.Actor == ExecutionActor.Player);
            return;
        }

        _operatorPoint = _host.Actor == ExecutionActor.Player ? actor.Position : GetStagePoint(_stage);
        _operatorDirection = DirectionToWheel(_operatorPoint);
        if (_host.Actor == ExecutionActor.Player &&
            !_host.BeginPlayerStageControl(_operatorDirection, $"wheel stage {_stage + 1}"))
        {
            _host.LogWarning(
                $"Wheel stage {_stage + 1} could not acquire player facing control; its timer continues.");
        }
        BeginStageAction(actor);
    }

    private void BeginStageAction(Agent actor)
    {
        _busy = true;
        _stageElapsed = 0f;
        _impactApplied = false;
        actor.Controller = _host.Actor == ExecutionActor.Player
            ? AgentControllerType.Player
            : AgentControllerType.None;
        _host.PoseAgentAt(actor, _operatorPoint, _operatorDirection);
        if (!_host.TryPlayRequiredAction(
                actor,
                StrikeAction,
                $"wheel strike stage {_stage + 1}",
                forceFullBody: true))
        {
            _host.LogWarning(
                $"Wheel strike animation failed at stage {_stage + 1}; the 1.15-second timer continues.");
        }
    }

    private void ApplyStrikeImpact()
    {
        var victim = _host.VictimAgent;
        if (victim is null)
        {
            return;
        }

        sbyte bone;
        if (_stage == 0)
        {
            bone = victim.Monster.LeftFootIkEndEffectorBoneIndex;
        }
        else if (_stage == 1)
        {
            bone = victim.Monster.RightFootIkEndEffectorBoneIndex;
        }
        else if (_stage == 2)
        {
            bone = victim.Monster.LeftUpperArmBoneIndex;
        }
        else if (_stage == 3)
        {
            bone = victim.Monster.RightUpperArmBoneIndex;
        }
        else
        {
            bone = victim.Monster.SpineUpperBoneIndex;
        }

        var lethal = _stage == StrikePoints.Length - 1;
        _host.PlayVictimVoice(
            lethal ? SkinVoiceManager.VoiceType.Death : SkinVoiceManager.VoiceType.Pain,
            lethal ? "fatal wheel chest strike" : $"wheel strike {_stage + 1}");
        _host.PlayVictimStageBlood(
            bone,
            lethal ? 1.65f : 0.90f,
            lethal ? 3 : 1,
            lethal ? "fatal wheel chest strike" : $"wheel strike {_stage + 1}");
    }

    private void CompleteStrikeStage()
    {
        var actor = GetOperator();
        if (_host.Actor == ExecutionActor.Player)
        {
            _host.EndPlayerStageControl($"wheel stage {_stage + 1}");
        }
        else if (actor is not null && actor.IsActive())
        {
            actor.DisableScriptedMovement();
            actor.Controller = AgentControllerType.None;
        }

        _busy = true;
        _rotatingAfterStrike = true;
        _stageElapsed = 0f;
        _rotationStartRadians = _wheelSpinRadians;
        if (_wheelGuard is not null && _wheelGuard.IsActive())
        {
            _host.TryPlayAction(
                _wheelGuard,
                GuardTurnAction,
                $"wheel guard index rotation after strike {_stage + 1}");
        }
        _host.LogInfo(
            $"Wheel strike {_stage + 1} completed; the guard is rotating the horizontal wheel by 35 degrees.");
    }

    private void TickIndexRotation(float dt)
    {
        _stageElapsed += dt;
        var progress = MathF.Min(1f, _stageElapsed / IndexRotationSeconds);
        _wheelSpinRadians = _rotationStartRadians + IndexRotationRadians * progress;
        ApplyWheelFrame();
        MaintainVictimOnWheel(
            closeEyes: _stage == StrikePoints.Length - 1,
            freezeAction: _stage == StrikePoints.Length - 1);
        if (progress < 1f)
        {
            return;
        }

        _rotatingAfterStrike = false;
        _busy = false;
        _wheelSpinRadians = _rotationStartRadians + IndexRotationRadians;
        _stage++;
        if (_stage >= StrikePoints.Length)
        {
            if (!_lethalRequested)
            {
                _lethalRequested = true;
                _host.RequestLethalFrame();
            }
            BeginFinalRaise();
            return;
        }

        if (_host.Actor == ExecutionActor.Player)
        {
            UpdateUsePoint(enabled: true);
        }
        else
        {
            BeginNpcStageMovement();
        }
    }

    private void BeginFinalRaise()
    {
        if (_raisingWheel || _presentationComplete)
        {
            return;
        }

        _busy = true;
        _raisingWheel = true;
        _stageElapsed = 0f;
        _raiseProgress = 0f;
        UpdateUsePoint(enabled: false);
        if (_wheelGuard is not null && _wheelGuard.IsActive())
        {
            _host.TryPlayAction(_wheelGuard, GuardTurnAction, "wheel guard final vertical raise");
        }
        _host.LogInfo("The fifth 35-degree index rotation completed; the same guard is raising the wheel to vertical.");
    }

    private void TickRaiseStage(float dt)
    {
        _stageElapsed += dt;
        _raiseProgress = MathF.Min(1f, _stageElapsed / RaiseSeconds);
        ApplyWheelFrame();
        MaintainVictimOnWheel(closeEyes: true, freezeAction: true);
        if (_raiseProgress < 1f)
        {
            return;
        }

        _busy = false;
        _raisingWheel = false;
        _presentationComplete = true;
        UpdateUsePoint(enabled: false);
        _host.LogInfo(
            "The single wheel guard completed the 2.80-second final raise; the fixed original prisoner remains on the vertical wheel.");
    }

    private void ApplyWheelFrame()
    {
        if (_wheel is null)
        {
            return;
        }

        var frame = _wheelBaseFrame;
        frame.rotation.RotateAboutForward(_wheelSpinRadians);
        frame.rotation.RotateAboutSide(-MathF.PI * 0.5f * _raiseProgress);
        _wheel.SetGlobalFrame(in frame, isTeleportation: true);
    }

    private bool MaintainVictimOnWheel(bool closeEyes, bool freezeAction)
    {
        if (_host.VictimAgent is null)
        {
            return false;
        }

        var center = _wheel?.GetGlobalFrame().origin ?? _host.VictimPosition + new Vec3(0f, 0f, 0.34f);
        var placement = _host.Placement;
        var baseForward = placement?.Forward ?? Vec2.Forward;
        var baseRight = placement?.Right ?? new Vec2(baseForward.y, -baseForward.x);
        var cosine = MathF.Cos(_wheelSpinRadians);
        var sine = MathF.Sin(_wheelSpinRadians);
        var spunDirection = baseRight * cosine + baseForward * sine;
        var spunHorizontalUp = new Vec3(
            baseForward.x * cosine - baseRight.x * sine,
            baseForward.y * cosine - baseRight.y * sine,
            0f);
        var up = spunHorizontalUp * (1f - _raiseProgress) + Vec3.Up * _raiseProgress;
        if (!up.IsNonZero)
        {
            up = Vec3.Up;
        }

        return _host.SetVictimPresentationFrame(
            center,
            spunDirection,
            up.NormalizedCopy(),
            freezeAction,
            closeEyes,
            "breaking-wheel tableau");
    }

    private Agent? GetOperator() =>
        _host.Actor == ExecutionActor.Player ? _host.PlayerAgent : _host.ExecutionerAgent;

    private Vec3 GetStagePoint(int stage)
    {
        var safeStage = Math.Max(0, Math.Min(stage, StrikePoints.Length - 1));
        return _host.GetMethodWorldPoint(
            StrikePoints[safeStage].Right,
            StrikePoints[safeStage].Forward);
    }

    private Vec2 DirectionToWheel(Vec3 from)
    {
        var target = _host.GetMethodWorldPoint(0f, 0.10f);
        var direction = target.AsVec2 - from.AsVec2;
        return direction.IsNonZero() ? direction.Normalized() : Vec2.Forward;
    }

    private void UpdateUsePoint(bool enabled)
    {
        if (_stage >= StrikePoints.Length)
        {
            _host.UpdateMultiStageUsePoint(
                _host.GetMethodWorldPoint(0f, 0.10f),
                new TextObject(UseDescriptions[UseDescriptions.Length - 1]),
                new TextObject(UseActions[UseActions.Length - 1]),
                enabled: false);
            return;
        }

        _host.UpdateMultiStageUsePoint(
            GetStagePoint(_stage),
            new TextObject(UseDescriptions[_stage]),
            new TextObject(UseActions[_stage]),
            enabled);
    }
}