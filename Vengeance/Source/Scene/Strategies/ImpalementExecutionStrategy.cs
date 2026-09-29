using System;
using RichExecutions.Core;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Two-stage horizontal impalement using the original prisoner Agent. The
/// prisoner is kept as a kneeling, forward-leaning tableau while two helpers
/// hold the body and two helpers drive the pole. Campaign death remains
/// deferred until mission exit; no clone, corpse snapshot, or native corpse is
/// created here.
/// </summary>
internal sealed class ImpalementExecutionStrategy :
    IExecutionMethodStrategy,
    IMultiStageExecutionStrategy,
    IMethodSceneReadyStrategy
{
    private const float StrikeSeconds = 1.15f;
    private const float ImpactProgress = 0.58f;
    private const float NpcStageIntervalSeconds = 0.80f;
    private const float PoleRaiseSeconds = 2.00f;
    private const float PoleVisualLength = 5.60f;
    private const float PoleCrossSectionScale = 0.82f;
    private const float VictimStrikeBlendSeconds = 0.20f;
    // Two-second death blend-in requested by design review: it only controls
    // how slowly the death pose fades in and runs in parallel with the pole
    // raise, which still follows PoleRaiseSeconds unchanged.
    private const float VictimDeathBlendSeconds = 2.00f;
    // Distance from the pole rear point to the pelvis entry wound measured
    // along the shaft: the rear point sits 2.35 m behind the victim root and
    // the first stage drives the tip 0.40 m past the root (0.72 - 0.32 m).
    private const float PelvisPoleAnchorDistance = 2.75f;
    private const float BodyLeanRadians = 30f * MathF.PI / 180f;
    private const string VictimBoundAction = "act_main_story_conspirator_kneel_down_1_continue";
    private const string VictimStruggleFallbackAction = "act_ver_hanging_struggle";
    private const string VictimDeathAction = "act_ver_hanging_death";
    private const string VictimStrikeReactionAction = "act_ver_impale_strike_normal_b";
    private const string HelperIdleAction = "act_idle_unarmed_2";
    private const string PoleMeshName = "verjianchi02";
    private const string PolePhysicsMeshName = "bo_battania_castle_keep_wood_stake_a";

    private static readonly float[] PoleTipDepths = { 0.72f, 1.70f };
    internal static readonly string[] VictimDeathActions =
    {
        "act_ver_impale_death_head_exaggerated",
        "act_ver_impale_death_head_normal_a",
        "act_ver_impale_death_head_normal_b",
        "act_ver_impale_death_left_shoulder_exaggerated",
        "act_ver_impale_death_left_shoulder_normal_a",
        "act_ver_impale_death_left_shoulder_normal_b",
        "act_ver_impale_death_right_shoulder_normal_a",
        "act_ver_impale_death_right_shoulder_normal_b"
    };
    private static readonly string[] StageDescriptions =
    {
        "{=REX_Impalement_Use_Pole}Impalement pole",
        "{=REX_Impalement_Use_Pole}Impalement pole"
    };
    private static readonly string[] StageActions =
    {
        "{=REX_Impalement_Action_Pelvis}drive the pole through the body",
        "{=REX_Impalement_Action_Head}complete the head penetration"
    };

    private IExecutionSceneHost _host = null!;
    private GameEntity? _pole;
    private GameEntity? _polePhysics;
    private int _poleLengthAxis = 1;
    private float _poleNativeLength = 1f;
    private Vec3 _poleLocalCenter = Vec3.Zero;
    private Vec3 _poleLocalExtents = Vec3.One;
    private int _polePhysicsLengthAxis = 1;
    private float _polePhysicsNativeLength = 1f;
    private Vec3 _polePhysicsCenter = Vec3.Zero;
    private Vec3 _polePhysicsExtents = Vec3.One;
    private bool _poleCollisionEnabled;
    private readonly Agent?[] _bodyHandlers = new Agent?[2];
    private readonly Agent?[] _poleHandlers = new Agent?[2];
    private int _stage;
    private bool _started;
    private bool _busy;
    private bool _impactApplied;
    private bool _lethalRequested;
    private bool _finalPoseFrozen;
    private bool _raisingPole;
    private bool _presentationComplete;
    private bool _deathActionStarted;
    private bool _displaySnapshotAttempted;
    private bool _boneTrackingFailureLogged;
    private int _bodyAlignmentLogCount;
    private string? _selectedDeathAction;
    private float _stageElapsed;
    private float _npcIntervalElapsed;
    private float _raiseElapsed;
    private Vec3 _initialVictimRoot = Vec3.Invalid;
    private Vec3 _victimRoot = Vec3.Invalid;
    private Vec3 _victimUp = Vec3.Up;
    private Vec3? _victimFacing;
    private Vec3 _poleRearPoint = Vec3.Invalid;
    private Vec3 _poleTip = Vec3.Invalid;
    private Vec2 _forward = Vec2.Forward;
    private Vec3 _operatorPoint = Vec3.Invalid;
    private Vec2 _operatorDirection = Vec2.Forward;

    public string MethodId => ExecutionMethodRules.Impalement;
    public bool UsesStagedExecution => true;
    public bool RequiresExecutionAxe => false;
    public bool DefersVictimDeathToMissionExit => true;
    public bool IsWaitingForPlayerInput =>
        _started && _host.Actor == ExecutionActor.Player && !_busy && !_raisingPole && !_presentationComplete && _stage < PoleTipDepths.Length;
    public bool IsStageBusy => _busy || _raisingPole;
    public bool IsPresentationComplete => _presentationComplete;

    public void AttachHost(IExecutionSceneHost host) =>
        _host = host ?? throw new ArgumentNullException(nameof(host));

    public void SetupSceneVisuals() { }

    public void OnMethodSceneReady()
    {
        var placement = _host.Placement;
        _forward = placement?.Forward.IsNonZero() == true ? placement.Forward.Normalized() : Vec2.Forward;
        _initialVictimRoot = _host.VictimPosition;
        _victimRoot = _initialVictimRoot;
        _victimUp = GetInitialVictimUp();
        _poleRearPoint = _victimRoot - Horizontal(_forward) * 2.35f + new Vec3(0f, 0f, 0.92f);
        _poleTip = _victimRoot - Horizontal(_forward) * 0.32f + new Vec3(0f, 0f, 0.92f);

        CreateRuntimePoleMeshes();
        SpawnHelpers();
        BindVictimIdle();
        UpdatePole();
        UpdateUsePoint(enabled: true);
        _host.LogInfo(
            $"Two-stage impalement initialized: victimRoot=({_victimRoot.x:0.00},{_victimRoot.y:0.00},{_victimRoot.z:0.00}), " +
            $"lean={BodyLeanRadians * 180f / MathF.PI:0}deg, stages={PoleTipDepths.Length}.");
    }

    public void TickSceneVisuals(float dt)
    {
        MaintainVictimPresentation(
            closeEyes: _lethalRequested || _finalPoseFrozen,
            freezeAction: _finalPoseFrozen);
        UpdatePole();
    }

    public void PrefetchActions()
    {
        _host.TryPlayRequiredAction(_host.VictimAgent, VictimBoundAction, "impalement kneeling idle prefetch", true, 0.4f);
        _host.TryPlayRequiredAction(_host.VictimAgent, VictimStruggleFallbackAction, "impalement struggle fallback prefetch", true, 0.4f);
        _host.TryPlayRequiredAction(_host.VictimAgent, VictimDeathAction, "impalement fixed death prefetch", true, 0.4f);
        _host.TryPlayRequiredAction(
            _host.VictimAgent,
            VictimStrikeReactionAction,
            "impalement strike reaction prefetch",
            true,
            VictimStrikeBlendSeconds);
        foreach (var deathAction in VictimDeathActions)
        {
            _host.TryPlayRequiredAction(
                _host.VictimAgent,
                deathAction,
                "impalement random death prefetch",
                true,
                VictimDeathBlendSeconds);
        }
    }

    public void StartExecutionAction()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _host.ActionStarted = true;
        BeginStage();
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
        if (_raisingPole)
        {
            TickPoleRaise(safeDt);
            return;
        }

        if (!_busy)
        {
            if (_host.Actor == ExecutionActor.Executioner && !_lethalRequested)
            {
                _npcIntervalElapsed += safeDt;
                if (_stage == 0 || _npcIntervalElapsed >= NpcStageIntervalSeconds)
                {
                    BeginStage();
                }
            }

            return;
        }

        _stageElapsed += safeDt;
        var progress = MathF.Min(1f, _stageElapsed / StrikeSeconds);
        _poleTip = GetPoleTipForProgress(progress);
        UpdatePole();
        MaintainVictimPresentation(_lethalRequested || _finalPoseFrozen, _lethalRequested || _finalPoseFrozen);

        if (!_impactApplied && progress >= ImpactProgress)
        {
            _impactApplied = true;
            ApplyStageImpact();
        }

        if (progress < 1f)
        {
            return;
        }

        CompleteStage();
    }

    public void BeginMethodSequence(string trigger) { }
    public void TickMethodSequence(float dt) { }
    public void ApplyLethalFrameEffects() { }

    public bool ApplyDeath()
    {
        EnsureDeathActionStarted();
        MaintainVictimPresentation(closeEyes: true, freezeAction: false);
        return true;
    }

    public void TickAfterDeath(float dt)
    {
        TickExecution(dt);
        MaintainVictimPresentation(closeEyes: true, freezeAction: _finalPoseFrozen);
        UpdatePole();
    }

    public void Cleanup()
    {
        if (_host.Actor == ExecutionActor.Player && _busy)
        {
            _host.EndPlayerStageControl("impalement cleanup");
        }

        UpdateUsePoint(enabled: false);
        if (_pole is not null)
        {
            _host.RemoveSpawnedEntity(_pole);
            _pole = null;
        }
        if (_polePhysics is not null)
        {
            _host.RemoveSpawnedEntity(_polePhysics);
            _polePhysics = null;
        }

        _poleCollisionEnabled = false;
    }

    private void SpawnHelpers()
    {
        var bodyLeft = _host.GetMethodWorldPoint(-0.92f, -0.28f);
        var bodyRight = _host.GetMethodWorldPoint(0.92f, -0.28f);
        var poleLeft = _host.GetMethodWorldPoint(-0.92f, -1.95f);
        var poleRight = _host.GetMethodWorldPoint(0.92f, -1.95f);
        _bodyHandlers[0] = _host.SpawnMethodHelper(bodyLeft, DirectionToVictim(bodyLeft), "impalement left body handler", 0);
        _bodyHandlers[1] = _host.SpawnMethodHelper(bodyRight, DirectionToVictim(bodyRight), "impalement right body handler", 1);
        _poleHandlers[0] = _host.SpawnMethodHelper(poleLeft, DirectionToPole(poleLeft), "impalement left pole handler", 2);
        _poleHandlers[1] = _host.SpawnMethodHelper(poleRight, DirectionToPole(poleRight), "impalement right pole handler", 3);

        for (var index = 0; index < _bodyHandlers.Length; index++)
        {
            PrepareHelper(_bodyHandlers[index], $"impalement body handler {index + 1}");
        }
        for (var index = 0; index < _poleHandlers.Length; index++)
        {
            PrepareHelper(_poleHandlers[index], $"impalement pole handler {index + 1}");
        }
    }

    private void PrepareHelper(Agent? helper, string role)
    {
        if (helper is null || !helper.IsActive())
        {
            _host.LogWarning($"{role} could not be spawned; the visual sequence will continue.");
            return;
        }

        helper.DisableScriptedMovement();
        helper.Controller = AgentControllerType.None;
        _host.TryPlayRequiredAction(helper, HelperIdleAction, role + " idle", true, 0.2f);
    }

    private void BindVictimIdle()
    {
        var victim = _host.VictimAgent;
        if (victim is null)
        {
            _host.LogError("Impalement has no original prisoner Agent.");
            return;
        }

        if (!_host.TryPlayRequiredAction(
                victim,
                VictimBoundAction,
                "kneeling impalement victim idle",
                forceFullBody: true,
                blendInPeriod: 0.4f))
        {
            _host.TryPlayRequiredAction(
                victim,
                VictimStruggleFallbackAction,
                "impalement victim idle fallback",
                forceFullBody: true,
                blendInPeriod: 0.4f);
        }
    }

    private void BeginStage()
    {
        if (_busy || _raisingPole || _lethalRequested || _stage >= PoleTipDepths.Length)
        {
            return;
        }

        _busy = true;
        _stageElapsed = 0f;
        _impactApplied = false;
        _npcIntervalElapsed = 0f;
        _operatorPoint = _host.Actor == ExecutionActor.Player
            ? (_host.PlayerAgent?.Position ?? _host.GetMethodWorldPoint(1.30f, -0.30f))
            : _host.GetMethodWorldPoint(1.30f, -0.30f);
        _operatorDirection = DirectionToPole(_operatorPoint);
        UpdateUsePoint(enabled: false);
        // The pole is a moved prop during a stage; suspending its collision keeps
        // the pinned prisoner and the handlers from being pushed by the driven
        // shaft. It is restored, still matching the visible pole, as soon as the
        // stage stops moving.
        SetPoleCollisionEnabled(false);

        var operatorAgent = _host.Actor == ExecutionActor.Player ? _host.PlayerAgent : _host.ExecutionerAgent;
        if (operatorAgent is null || !operatorAgent.IsActive())
        {
            _host.LogWarning($"Impalement stage {_stage + 1} has no active operator; timed progression is paused.");
            _busy = false;
            SetPoleCollisionEnabled(true);
            UpdateUsePoint(enabled: _host.Actor == ExecutionActor.Player);
            return;
        }

        if (_host.Actor == ExecutionActor.Player)
        {
            if (!_host.BeginPlayerStageControl(_operatorDirection, $"impalement stage {_stage + 1}"))
            {
                _host.LogWarning($"Player facing lock failed at impalement stage {_stage + 1}; timer continues.");
            }
        }
        else
        {
            _host.PoseAgentAt(operatorAgent, _operatorPoint, _operatorDirection);
        }

        _host.LogInfo(
            $"Impalement stage {_stage + 1} began without an operator strike animation; " +
            "the F input drives the pole and prisoner reaction directly.");
    }

    private void CompleteStage()
    {
        if (_host.Actor == ExecutionActor.Player)
        {
            _host.EndPlayerStageControl($"impalement stage {_stage + 1}");
        }
        else if (_host.ExecutionerAgent is { } executioner && executioner.IsActive())
        {
            executioner.DisableScriptedMovement();
            executioner.Controller = AgentControllerType.None;
        }

        _busy = false;
        _stage++;
        _npcIntervalElapsed = 0f;
        if (_stage < PoleTipDepths.Length)
        {
            UpdatePole();
            SetPoleCollisionEnabled(true);
            UpdateUsePoint(enabled: _host.Actor == ExecutionActor.Player);
            return;
        }

        // Last stage: trigger the pole raise (the rest of the raise runs in
        // TickPoleRaise over PoleRaiseSeconds).
        _lethalRequested = true;
        _raisingPole = true;
        _raiseElapsed = 0f;
        UpdateUsePoint(enabled: false);
        _host.RequestLethalFrame();
        foreach (var handler in _poleHandlers)
        {
            if (handler is not null && handler.IsActive())
            {
                _host.TryPlayRequiredAction(handler, "act_usage_siege_tower_open_gate_begin", "pole handlers raise the impalement pole", true, 0.2f);
            }
        }
    }

    private void ApplyStageImpact()
    {
        var victim = _host.VictimAgent;
        if (victim is null)
        {
            return;
        }

        var lethal = _stage == PoleTipDepths.Length - 1;
        var bone = lethal
            ? victim.Monster.NeckRootBoneIndex
            : victim.Monster.PelvisBoneIndex;
        _host.PlayVictimVoice(
            lethal ? SkinVoiceManager.VoiceType.Death : SkinVoiceManager.VoiceType.Pain,
            lethal ? "fatal impalement head impact" : $"impalement stage {_stage + 1} impact");
        _host.PlayVictimStageBlood(
            bone,
            lethal ? 1.85f : 1.05f,
            lethal ? 3 : 2,
            lethal ? "fatal impalement" : $"impalement stage {_stage + 1} blood impact");

        if (lethal)
        {
            // Continue the initial 30-degree forward lean to 90 degrees:
            // the head points forward along the shaft and the face points down.
            _victimUp = Horizontal(_forward);
            _victimFacing = -Vec3.Up;
            EnsureDeathActionStarted();
            MaintainVictimPresentation(closeEyes: true, freezeAction: false);
        }
        else if (!_host.TryPlayRequiredAction(
                     victim,
                     VictimStrikeReactionAction,
                     $"impalement stage {_stage + 1} strike reaction",
                     forceFullBody: true,
                     blendInPeriod: VictimStrikeBlendSeconds))
        {
            _host.LogWarning(
                $"Impalement strike reaction failed at stage {_stage + 1}; the pole and timing continue.");
        }
    }

    private void EnsureDeathActionStarted()
    {
        if (_deathActionStarted)
        {
            return;
        }

        var victim = _host.VictimAgent;
        if (victim is null)
        {
            return;
        }

        _selectedDeathAction ??= VictimDeathActions[
            Math.Min(
                VictimDeathActions.Length - 1,
                (int)(MBRandom.RandomFloat * VictimDeathActions.Length))];
        var selectedAction = _selectedDeathAction;
        // The host gives impalement death actions their suspension flags:
        // full-body/root control with foot IK disabled. Do not leave a planted
        // foot solver competing with the requested ninety-degree forward lean.
        if (!_host.TryPlayRequiredAction(
                victim,
                selectedAction,
                "random impalement death",
                forceFullBody: true,
                blendInPeriod: VictimDeathBlendSeconds))
        {
            _host.LogWarning(
                $"Random impalement death '{selectedAction}' failed; using the hanging death fallback.");
            selectedAction = VictimDeathAction;
            if (!_host.TryPlayRequiredAction(
                    victim,
                    selectedAction,
                    "impalement death fallback",
                    forceFullBody: true,
                    blendInPeriod: VictimDeathBlendSeconds))
            {
                return;
            }
        }

        _selectedDeathAction = selectedAction;
        _deathActionStarted = true;
        _host.LogInfo(
            $"Impalement selected death action '{selectedAction}' with a " +
            $"{VictimDeathBlendSeconds:0.00}s blend.");
    }

    private void TickPoleRaise(float dt)
    {
        _raiseElapsed += dt;
        var progress = MathF.Min(1f, _raiseElapsed / PoleRaiseSeconds);
        // The pole rotates around the original rear point (v0.5.15 geometry).
        // The body rides the rotation axis at the same offset as the strike
        // formula, so the prisoner stays bound to the pole.
        var horizontal = Horizontal(_forward);
        var axis = horizontal * MathF.Cos(progress * MathF.PI * 0.5f) +
                   Vec3.Up * MathF.Sin(progress * MathF.PI * 0.5f);
        var poleLength = 2.35f + PoleTipDepths[PoleTipDepths.Length - 1] - 0.32f;
        _poleTip = _poleRearPoint + axis * poleLength;
        _victimRoot = _poleRearPoint + axis * 2.35f - Vec3.Up * 0.92f;
        // Body up **follows the rotation axis**, so the prisoner is laid out
        // along the pole when the pole is horizontal and stands upright
        // when the pole is vertical. This is the only change vs. the v0.5.15
        // formula: previously the up interpolated from a near-vertical
        // "GetInitialVictimUp" to Up, which kept the body looking upright
        // the whole time instead of rotating with the pole.
        _victimUp = axis;
        _victimFacing = horizontal * MathF.Sin(progress * MathF.PI * 0.5f) -
                        Vec3.Up * MathF.Cos(progress * MathF.PI * 0.5f);
        UpdatePole();
        MaintainVictimPresentation(closeEyes: true, freezeAction: false);
        if (progress < 1f)
        {
            return;
        }

        _raisingPole = false;
        _presentationComplete = true;
        _finalPoseFrozen = MaintainVictimPresentation(closeEyes: true, freezeAction: true);
        SetPoleCollisionEnabled(true);
        SaveFinalDisplaySnapshot();
        _host.LogInfo("Impalement pole was raised vertically; original prisoner remains fixed on the pole.");
    }

    private void SaveFinalDisplaySnapshot()
    {
        if (_displaySnapshotAttempted)
        {
            return;
        }

        _displaySnapshotAttempted = true;
        if (!_finalPoseFrozen || string.IsNullOrWhiteSpace(_selectedDeathAction))
        {
            _host.LogWarning(
                "The impaled-corpse decoration was not saved because the original prisoner's final pose was not frozen.");
            return;
        }

        var progress = _host.GetVictimCurrentActionProgress();
        if (!_host.SaveImpaledCorpseDisplay(_selectedDeathAction!, progress))
        {
            _host.LogWarning(
                "The impaled-corpse decoration snapshot could not be saved; the current ceremony remains active.");
        }
    }

    private bool MaintainVictimPresentation(bool closeEyes, bool freezeAction)
    {
        if (!_victimRoot.IsValid)
        {
            return false;
        }

        PinPelvisToPole();
        return _host.SetVictimPresentationFrame(
            _victimRoot,
            _forward,
            _victimUp.IsNonZero ? _victimUp.NormalizedCopy() : Vec3.Up,
            freezeAction,
            closeEyes,
            "kneeling forward-leaning impalement victim",
            facing: _victimFacing);
    }

    /// <summary>
    /// Pins the victim's real animated pelvis onto the pole axis once the
    /// death action is blending in. The death clips carry their own
    /// pelvis-to-root offset, which is exactly what used to push the body off
    /// the shaft while it rose. The pole is authoritative: the correction is
    /// recomputed absolutely from the actual pelvis every tick, never
    /// accumulated, and never moves the pole toward the body. Pole raising is
    /// deliberately not gated on this so the 2-second death blend cannot
    /// delay the raise.
    /// </summary>
    private void PinPelvisToPole()
    {
        if (!_deathActionStarted || !_poleRearPoint.IsValid)
        {
            return;
        }

        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive() || !victim.Position.IsValid)
        {
            return;
        }

        if (!_host.TryGetVictimBoneWorldPosition(victim.Monster.PelvisBoneIndex, out var pelvis))
        {
            if (!_boneTrackingFailureLogged)
            {
                _boneTrackingFailureLogged = true;
                _host.LogWarning(
                    "Impalement pelvis tracking could not sample the victim's pelvis bone; " +
                    "the body keeps the formula placement for this ceremony.");
            }
            return;
        }

        var axis = _victimUp.IsNonZero ? _victimUp.NormalizedCopy() : Vec3.Up;
        var anchor = _poleRearPoint + axis * PelvisPoleAnchorDistance;
        _victimRoot = victim.Position + (anchor - pelvis);

        if (_bodyAlignmentLogCount < 4)
        {
            _bodyAlignmentLogCount++;
            var torsoNote = string.Empty;
            if (_host.TryGetVictimBoneWorldPosition(victim.Monster.NeckRootBoneIndex, out var neck))
            {
                var torso = neck - pelvis;
                if (torso.Normalize() > 0.01f)
                {
                    torsoNote = $", torsoAxisDot={Vec3.DotProduct(torso, axis):0.000}";
                }
            }
            _host.LogInfo(
                $"Impalement body alignment sample {_bodyAlignmentLogCount}/4: " +
                $"pelvisError={(pelvis - anchor).Length:0.000}m{torsoNote}.");
        }
    }

    private void UpdatePole()
    {
        try
        {
            if (!_poleRearPoint.IsValid || !_poleTip.IsValid)
            {
                return;
            }

            var direction = _poleTip - _poleRearPoint;
            if (direction.Normalize() <= 0.01f)
            {
                return;
            }

            // The visible spike and its collision body are both derived from this
            // single axis and single centre. They therefore share one orientation,
            // one position and one length while the pole is horizontal, while it
            // advances, and after it has been raised, so the collision body can
            // never disagree with the pillar the player actually sees.
            var visibleCenter = _poleTip - direction * (PoleVisualLength * 0.5f);

            if (_pole is not null)
            {
                var visualFrame = ComputeVisualPoleFrame(direction, visibleCenter);
                _pole.SetGlobalFrame(in visualFrame, isTeleportation: true);
            }

            if (_polePhysics is not null)
            {
                var collisionFrame = ComputeCollisionPoleFrame(direction, visibleCenter);
                _polePhysics.SetGlobalFrame(in collisionFrame, isTeleportation: true);
            }
        }
        catch (Exception exception)
        {
            _host.LogWarning($"Impalement pole update failed for this tick: {exception.Message}");
        }
    }

    private MatrixFrame ComputeVisualPoleFrame(Vec3 direction, Vec3 visibleCenter)
    {
        var uniformScale = PoleVisualLength / _poleNativeLength;
        var rotation = BuildPoleRotation(direction, _poleLengthAxis, _forward);
        rotation.ApplyScaleLocal(BuildPoleScale(
            _poleLengthAxis,
            uniformScale,
            uniformScale * PoleCrossSectionScale));
        var rotatedLocalCenter = rotation.TransformToParent(in _poleLocalCenter);
        return new MatrixFrame(rotation, visibleCenter - rotatedLocalCenter);
    }

    private MatrixFrame ComputeCollisionPoleFrame(Vec3 direction, Vec3 visibleCenter)
    {
        var visualUniformScale = PoleVisualLength / _poleNativeLength;
        var targetCrossSection = MathF.Max(
            0.02f,
            LargestCrossSection(_poleLocalExtents, _poleLengthAxis) *
            visualUniformScale *
            PoleCrossSectionScale);
        var scale = new Vec3(
            _polePhysicsLengthAxis == 0
                ? PoleVisualLength / _polePhysicsNativeLength
                : targetCrossSection / MathF.Max(0.02f, _polePhysicsExtents.x),
            _polePhysicsLengthAxis == 1
                ? PoleVisualLength / _polePhysicsNativeLength
                : targetCrossSection / MathF.Max(0.02f, _polePhysicsExtents.y),
            _polePhysicsLengthAxis == 2
                ? PoleVisualLength / _polePhysicsNativeLength
                : targetCrossSection / MathF.Max(0.02f, _polePhysicsExtents.z));
        var rotation = BuildPoleRotation(direction, _polePhysicsLengthAxis, _forward);
        rotation.ApplyScaleLocal(scale);
        var rotatedCenter = rotation.TransformToParent(in _polePhysicsCenter);
        return new MatrixFrame(rotation, visibleCenter - rotatedCenter);
    }

    private void CreateRuntimePoleMeshes()
    {
        if (_host.Placement is null)
        {
            _host.LogWarning("Impalement runtime Mesh creation skipped because placement is unavailable.");
            return;
        }

        // v0.5.5: the asset pack merged the former bottom Mesh (verjianchi01)
        // into verjianchi02, so the impalement apparatus is created from the
        // single merged Mesh and no separate base entity is spawned any more.
        _pole = CreateMeshEntity(PoleMeshName, _host.Placement.CreateFrame((_poleRearPoint + _poleTip) * 0.5f), "moving impalement spike mesh", null);
        if (_pole is null)
        {
            _host.LogError($"Impalement spike Mesh '{PoleMeshName}' was unavailable; the mission remains open for diagnosis.");
            return;
        }

        CapturePoleLengthAxis();
        _host.TryDisableEntityCollision(_pole, "moving impalement spike mesh");
        CreatePoleCollisionBody();
        UpdatePole();
        _host.LogInfo(
            $"Impalement runtime Mesh created: pole='{PoleMeshName}' (merged base+spike) " +
            $"axis={_poleLengthAxis} nativeLength={_poleNativeLength:0.000} " +
            $"extents=({_poleLocalExtents.x:0.000},{_poleLocalExtents.y:0.000},{_poleLocalExtents.z:0.000}); " +
            $"collision='{PolePhysicsMeshName}' axis={_polePhysicsLengthAxis} nativeLength={_polePhysicsNativeLength:0.000} " +
            $"tracking=visible-pole displayedLength={PoleVisualLength:0.000}.");
    }

    private GameEntity? CreateMeshEntity(string meshName, MatrixFrame frame, string purpose, string? physicsMeshName)
    {
        GameEntity? entity = null;
        try
        {
            var mesh = MetaMesh.GetCopy(meshName, showErrors: false, mayReturnNull: true);
            if (mesh is null || !mesh.IsValid)
            {
                return null;
            }

            entity = GameEntity.CreateEmpty(_host.Mission.Scene, isModifiableFromEditor: false, createPhysics: false, callScriptCallbacks: false);
            if (entity is null)
            {
                return null;
            }

            entity.AddMultiMesh(mesh);
            entity.SetMobility(GameEntity.Mobility.Stationary);
            entity.SetGlobalFrame(in frame, isTeleportation: true);

            if (!string.IsNullOrWhiteSpace(physicsMeshName))
            {
                var physicsShape = PhysicsShape.GetFromResource(physicsMeshName, mayReturnNull: true);
                if (physicsShape is null || !physicsShape.IsValid)
                {
                    _host.LogWarning($"Physics Mesh '{physicsMeshName}' was unavailable for {purpose}; visual Mesh remains active.");
                }
                else
                {
                    entity.AddPhysics(1f, Vec3.Zero, physicsShape, Vec3.Zero, Vec3.Zero, PhysicsMaterial.InvalidPhysicsMaterial, isStatic: true, collisionGroupID: -1);
                    entity.SetPhysicsStateOnlyVariable(isEnabled: true, setChildren: true);
                }
            }

            return entity;
        }
        catch (Exception exception)
        {
            if (entity is not null)
            {
                _host.RemoveSpawnedEntity(entity);
            }
            _host.LogWarning($"Could not create {purpose} from Mesh '{meshName}': {exception.Message}");
            return null;
        }
    }

    private void CapturePoleLengthAxis()
    {
        if (_pole is null)
        {
            return;
        }

        var min = _pole.GetBoundingBoxMin();
        var max = _pole.GetBoundingBoxMax();
        var extents = max - min;
        _poleLocalExtents = extents;
        _poleLocalCenter = (min + max) * 0.5f;
        _poleLengthAxis = extents.x >= extents.y && extents.x >= extents.z ? 0 : extents.y >= extents.z ? 1 : 2;
        _poleNativeLength = MathF.Max(0.05f, _poleLengthAxis == 0 ? extents.x : _poleLengthAxis == 1 ? extents.y : extents.z);
    }

    private static Vec3 BuildPoleScale(int lengthAxis, float lengthScale, float crossSectionScale) =>
        lengthAxis switch
        {
            0 => new Vec3(lengthScale, crossSectionScale, crossSectionScale),
            2 => new Vec3(crossSectionScale, crossSectionScale, lengthScale),
            _ => new Vec3(crossSectionScale, lengthScale, crossSectionScale)
        };

    private static float AxisExtent(Vec3 extents, int axis) =>
        axis == 0 ? extents.x : axis == 1 ? extents.y : extents.z;

    private static int LongestAxis(Vec3 extents) =>
        extents.x >= extents.y && extents.x >= extents.z ? 0 : extents.y >= extents.z ? 1 : 2;

    private static float LargestCrossSection(Vec3 extents, int lengthAxis) =>
        lengthAxis switch
        {
            0 => MathF.Max(extents.y, extents.z),
            2 => MathF.Max(extents.x, extents.y),
            _ => MathF.Max(extents.x, extents.z)
        };

    private static Mat3 BuildPoleRotation(Vec3 direction, int localLengthAxis, Vec2 referenceForward)
    {
        direction.Normalize();
        var reference = Vec3.Up;
        if (MathF.Abs(Vec3.DotProduct(direction, reference)) > 0.94f)
        {
            reference = new Vec3(referenceForward.x, referenceForward.y, 0f);
        }
        reference = (reference - direction * Vec3.DotProduct(reference, direction)).NormalizedCopy();

        if (localLengthAxis == 0)
        {
            var forward = reference;
            var up = Vec3.CrossProduct(direction, forward).NormalizedCopy();
            return new Mat3(direction, forward, up);
        }
        if (localLengthAxis == 2)
        {
            var forward = reference;
            var side = Vec3.CrossProduct(forward, direction).NormalizedCopy();
            return new Mat3(side, forward, direction);
        }

        var upAxis = reference;
        var sideAxis = Vec3.CrossProduct(direction, upAxis).NormalizedCopy();
        return new Mat3(sideAxis, direction, upAxis);
    }

    /// <summary>
    /// Builds the pole collision body once, from the measured PhysicsShape
    /// bounds of <see cref="PolePhysicsMeshName"/>, and leaves it attached to
    /// the visible spike. Its frame is refreshed from the very same pole axis
    /// and centre as the visible Mesh on every tick by
    /// <see cref="UpdatePole"/>, so the collision body follows the pillar while
    /// it lies horizontally, while it drives forward and after it stands up.
    /// </summary>
    private void CreatePoleCollisionBody()
    {
        if (_pole is null || _polePhysics is not null)
        {
            return;
        }

        try
        {
            var physicsShape = PhysicsShape.GetFromResource(PolePhysicsMeshName, mayReturnNull: true);
            if (physicsShape is null || !physicsShape.IsValid)
            {
                _host.LogWarning(
                    $"Impalement collision '{PolePhysicsMeshName}' is unavailable; the visual pole remains active without collision.");
                return;
            }

            physicsShape.GetBoundingBox(out var physicsBounds);
            _polePhysicsExtents = physicsBounds.max - physicsBounds.min;
            _polePhysicsCenter = (physicsBounds.min + physicsBounds.max) * 0.5f;
            _polePhysicsLengthAxis = LongestAxis(_polePhysicsExtents);
            _polePhysicsNativeLength = MathF.Max(0.05f, AxisExtent(_polePhysicsExtents, _polePhysicsLengthAxis));

            var entity = GameEntity.CreateEmpty(
                _host.Mission.Scene,
                isModifiableFromEditor: false,
                createPhysics: false,
                callScriptCallbacks: false);
            if (entity is null)
            {
                return;
            }

            entity.SetMobility(GameEntity.Mobility.Stationary);
            entity.AddPhysics(
                1f,
                Vec3.Zero,
                physicsShape,
                Vec3.Zero,
                Vec3.Zero,
                PhysicsMaterial.InvalidPhysicsMaterial,
                isStatic: true,
                collisionGroupID: -1);
            entity.SetPhysicsStateOnlyVariable(isEnabled: true, setChildren: true);
            _polePhysics = entity;
            _poleCollisionEnabled = true;
            _host.LogInfo(
                $"Impalement pole collision body created from PhysicsShape bounds: axis={_polePhysicsLengthAxis}, " +
                $"extents=({_polePhysicsExtents.x:0.000},{_polePhysicsExtents.y:0.000},{_polePhysicsExtents.z:0.000}), " +
                $"center=({_polePhysicsCenter.x:0.000},{_polePhysicsCenter.y:0.000},{_polePhysicsCenter.z:0.000}); " +
                "it now tracks the visible spike instead of being built as a fixed vertical pillar.");
        }
        catch (Exception exception)
        {
            if (_polePhysics is not null)
            {
                _host.RemoveSpawnedEntity(_polePhysics);
                _polePhysics = null;
            }
            _host.LogWarning($"Could not create the impalement spike collision body: {exception.Message}");
        }
    }

    private void SetPoleCollisionEnabled(bool enabled)
    {
        if (_polePhysics is null || _poleCollisionEnabled == enabled)
        {
            return;
        }

        try
        {
            _polePhysics.SetPhysicsStateOnlyVariable(isEnabled: enabled, setChildren: true);
            _poleCollisionEnabled = enabled;
            UpdatePole();
            _host.LogInfo(
                $"Impalement pole collision {(enabled ? "enabled" : "suspended")} for the current spike axis " +
                $"({_poleTip.x:0.00},{_poleTip.y:0.00},{_poleTip.z:0.00}); the body keeps the visible pole orientation.");
        }
        catch (Exception exception)
        {
            _host.LogWarning($"Could not toggle the impalement spike collision: {exception.Message}");
        }
    }

    private Vec3 GetPoleTipForProgress(float progress)
    {
        var safeStage = Math.Max(0, Math.Min(_stage, PoleTipDepths.Length - 1));
        var depth = PoleTipDepths[safeStage];
        if (_stage >= PoleTipDepths.Length)
        {
            depth = PoleTipDepths[PoleTipDepths.Length - 1];
        }

        if (_busy)
        {
            var previousDepth = safeStage == 0 ? -0.32f : PoleTipDepths[safeStage - 1];
            depth = previousDepth * (1f - progress) + depth * progress;
        }

        return _victimRoot + Horizontal(_forward) * (depth - 0.32f) + new Vec3(0f, 0f, 0.92f);
    }

    private Vec2 DirectionToVictim(Vec3 from)
    {
        var direction = _victimRoot.AsVec2 - from.AsVec2;
        return direction.IsNonZero() ? direction.Normalized() : _forward;
    }

    private Vec2 DirectionToPole(Vec3 from)
    {
        var direction = _victimRoot.AsVec2 - from.AsVec2;
        return direction.IsNonZero() ? direction.Normalized() : _forward;
    }

    private void UpdateUsePoint(bool enabled)
    {
        var safeStage = Math.Max(0, Math.Min(_stage, StageActions.Length - 1));
        _host.UpdateMultiStageUsePoint(
            _host.Actor == ExecutionActor.Player && _host.PlayerAgent is { } player && player.IsActive()
                ? player.Position
                : _host.GetMethodWorldPoint(1.30f, -0.30f),
            new TextObject(StageDescriptions[safeStage]),
            new TextObject(StageActions[safeStage]),
            enabled && !_busy && !_raisingPole && !_presentationComplete && _stage < StageActions.Length);
    }

    private Vec3 GetInitialVictimUp() =>
        (Vec3.Up * MathF.Cos(BodyLeanRadians) + Horizontal(_forward) * MathF.Sin(BodyLeanRadians)).NormalizedCopy();

    private static Vec3 Horizontal(Vec2 direction) => new(direction.x, direction.y, 0f);
}
