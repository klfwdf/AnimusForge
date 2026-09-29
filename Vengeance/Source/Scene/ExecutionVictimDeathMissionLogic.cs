using System;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

internal enum ExecutionVictimDeathResult
{
    NotAttempted = 0,
    Applied = 1,
    AlreadyApplied = 2,
    NotBound = 3,
    InvalidBinding = 4,
    RegisterBlowFailed = 5,
    DeathNotConfirmed = 6
}

/// <summary>
/// Owns the mission-only death of the selected hero's original Agent. Campaign
/// execution remains the controller's responsibility when the mission ends.
/// </summary>
internal sealed class ExecutionVictimDeathMissionLogic : MissionLogic, IAgentStateDecider
{
    private const int ExecutionCorpseBudget = 64;
    private const float ExecutionCorpseFadeOutSeconds = 900f;
    private const float CorpseRetentionRetryIntervalSeconds = 0.25f;
    private const float CorpseRetentionStatusIntervalSeconds = 1f;

    // A staged prisoner is deliberately held on the hostile side with its AI
    // paused and its morale left to decay. Vanilla CommonAIComponent then
    // panics it (MoraleThresholdForPanicking = 0.01) and the engine removes
    // the Agent as Routed, which destroys the ceremony before the address can
    // play. Hold morale at full while the prisoner is bound so the engine can
    // never route a prisoner who is physically restrained on the scaffold.
    private const float HostageMoralePerTick = 100f;

    private ExecutionRequest _request;
    private IAgentStateDecider? _downstreamDecider;

    private Agent? _victim;
    private Agent? _blowOwner;
    private bool _deathDecisionGate;
    private bool _externalVictimDeathArmed;
    private bool _externalKilledDecisionLogged;
    private bool _blowAttempted;
    private bool _killedObserved;
    private bool _victimDeletedObserved;
    private bool _campaignExecutionCommitted;
    private int _forcedKilledDecisionCount;
    private bool _corpseRetentionPending;
    private bool _corpseRetentionConfirmed;
    private bool _corpseRetentionFailureLogged;
    private bool _corpseRetentionProbeStarted;
    private bool _corpseRegistrationLossLogged;
    private bool _corpseSupportLossLogged;
    private long _missionTickSerial;
    private long _corpseRetentionEligibleTick;
    private int _corpseRetentionAttemptCount;
    private float _corpseRetentionRetryElapsed;
    private float _corpseRetentionStatusElapsed;
    private float _corpseInitialAgentHeight = float.NaN;
    private ExecutionVictimDeathResult _result = ExecutionVictimDeathResult.NotAttempted;

    private bool _hostageHoldActive;
    private bool _hostageHoldLogged;
    private bool _hostageRoutObserved;
    private bool _victimRoutQueryLogged;
    private bool _unexpectedVictimRemoval;

    internal ExecutionVictimDeathMissionLogic(
        ExecutionRequest request,
        IAgentStateDecider? downstreamDecider)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _downstreamDecider = downstreamDecider;
    }

    internal ExecutionVictimDeathResult Result => _result;

    // The custom deployment studio may still change the condemned prisoner while
    // the scene is provisional. Rebinding is only legal before any victim Agent
    // is bound and before anything has been committed, so the death chain can
    // never be pointed at a different hero once it is live.
    internal bool TryRebindProvisionalRequest(ExecutionRequest request)
    {
        if (request is null)
        {
            return false;
        }

        if (_victim is not null ||
            _blowOwner is not null ||
            _blowAttempted ||
            _killedObserved ||
            _campaignExecutionCommitted ||
            _result != ExecutionVictimDeathResult.NotAttempted)
        {
            RexLog.Warning(
                $"Refused to rebind the death logic to session {request.SessionId}: the chain is already live.");
            return false;
        }

        _request = request;
        return true;
    }

    internal bool IsCorpseRetentionConfirmed => _corpseRetentionConfirmed;

    internal int CorpseRetentionAttemptCount => _corpseRetentionAttemptCount;

    internal bool HasObservedVictimKilled => _killedObserved;

    internal bool HasUnexpectedVictimRemoval => _unexpectedVictimRemoval;

    internal void MarkCampaignExecutionCommitted()
    {
        _campaignExecutionCommitted = true;
        _corpseRetentionPending = false;
        RexLog.Info(
            $"Session {_request.SessionId} marked the mission victim teardown as post-campaign-commit cleanup.");
    }

    internal bool TryAttachDownstreamDecider(IAgentStateDecider? downstreamDecider)
    {
        if (_blowAttempted || ReferenceEquals(downstreamDecider, this))
        {
            return false;
        }

        if (_downstreamDecider is not null &&
            !ReferenceEquals(_downstreamDecider, downstreamDecider))
        {
            return false;
        }

        _downstreamDecider = downstreamDecider;
        return true;
    }

    public override void AfterStart()
    {
        base.AfterStart();
        try
        {
            var deciders = Mission.MissionBehaviors
                .OfType<IAgentStateDecider>()
                .ToList();
            var ownIndex = deciders.FindIndex(decider => ReferenceEquals(decider, this));
            var order = deciders.Count == 0
                ? "<none>"
                : string.Join(
                    " -> ",
                    deciders.Select(decider =>
                        decider.GetType().FullName ?? decider.GetType().Name));
            if (ownIndex == 0)
            {
                RexLog.Info(
                    $"Session {_request.SessionId} death-decider order verified: {order}; " +
                    "deathDeciderIndex=0.");
            }
            else
            {
                LogDeathInvariantFailed(
                    $"death logic was no longer the first Agent-state decider after mission start " +
                    $"(deathDeciderIndex={ownIndex}, order={order})");
            }
        }
        catch (Exception exception)
        {
            LogDeathInvariantFailed("could not inspect the Agent-state decider order", exception);
        }
    }

    internal bool TryBind(Agent victim, Agent blowOwner)
    {
        if (victim is null)
        {
            throw new ArgumentNullException(nameof(victim));
        }

        if (blowOwner is null)
        {
            throw new ArgumentNullException(nameof(blowOwner));
        }

        if (_victim is not null || _blowOwner is not null)
        {
            var sameBinding = ReferenceEquals(_victim, victim) &&
                              ReferenceEquals(_blowOwner, blowOwner);
            if (!sameBinding)
            {
                LogDeathInvariantFailed(
                    "attempted to replace the already-bound original prisoner or Blow owner");
            }

            return sameBinding;
        }

        if (!IsValidBinding(victim, blowOwner, requireActive: true, out var failure))
        {
            LogDeathInvariantFailed($"rejected death binding: {failure}");
            return false;
        }

        _victim = victim;
        _blowOwner = blowOwner;
        ArmHostageHold(victim, "death-logic binding");
        RexLog.Info(
            $"Session {_request.SessionId} bound original prisoner Agent {victim.Index} " +
            $"({victim.Origin.GetType().Name}) and non-hero Blow owner {blowOwner.Index}; " +
            "campaign execution remains deferred until mission exit.");
        return true;
    }

    /// <summary>
    /// Re-arms the morale hold that keeps the staged prisoner from routing.
    /// Safe to call repeatedly; it only ever raises morale and clears panic.
    /// </summary>
    internal void EnsureHostageHold()
    {
        if (_victim is null)
        {
            return;
        }

        _hostageHoldActive = true;
    }

    private void ArmHostageHold(Agent victim, string reason)
    {
        _hostageHoldActive = true;
        _hostageHoldLogged = false;
        _hostageRoutObserved = false;
        try
        {
            var ai = victim.CommonAIComponent;
            if (ai is not null)
            {
                if (ai.IsRetreating)
                {
                    ai.StopRetreating();
                }

                if (ai.Morale < HostageMoralePerTick)
                {
                    ai.Morale = HostageMoralePerTick;
                }
            }
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Could not raise the staged prisoner's initial morale during {reason}. " +
                exception.Message);
        }
    }

    internal bool TryArmExternalVictimDeath()
    {
        if (_victim is null || _blowOwner is null || _blowAttempted)
        {
            return false;
        }

        _externalVictimDeathArmed = true;
        _externalKilledDecisionLogged = false;
        ConfigureCorpseRetention();
        RexLog.Info(
            $"Session {_request.SessionId} armed the original prisoner Agent {_victim.Index} for " +
            "real external combat death; any native lethal hit is decided as Killed and campaign " +
            "execution remains deferred until mission exit.");
        return true;
    }

    internal ExecutionVictimDeathResult ApplyBattlefieldDeath(Agent visualActor)
    {
        if (_blowAttempted)
        {
            return _result == ExecutionVictimDeathResult.Applied
                ? ExecutionVictimDeathResult.AlreadyApplied
                : _result;
        }

        if (_victim is null || _blowOwner is null)
        {
            _result = ExecutionVictimDeathResult.NotBound;
            LogDeathInvariantFailed("battlefield death was requested before the original prisoner was bound");
            return _result;
        }

        if (visualActor is null || !visualActor.IsActive())
        {
            _result = ExecutionVictimDeathResult.InvalidBinding;
            LogDeathInvariantFailed("the visible execution actor was unavailable at the lethal frame");
            return _result;
        }

        var victim = _victim;
        var blowOwner = _blowOwner;
        if (!IsValidBinding(victim, blowOwner, requireActive: true, out var failure))
        {
            _result = ExecutionVictimDeathResult.InvalidBinding;
            LogDeathInvariantFailed($"the bound Agents were invalid at the lethal frame: {failure}");
            return _result;
        }

        var isBeheading = string.Equals(
            _request.Method.StringId,
            "beheading",
            StringComparison.OrdinalIgnoreCase);
        var hitPosition = isBeheading
            ? victim.GetEyeGlobalPosition() - new Vec3(0f, 0f, 0.18f)
            : victim.GetChestGlobalPosition();
        var direction = visualActor.LookDirection;
        direction.z = 0f;
        if (!direction.IsNonZero)
        {
            direction = victim.Position - visualActor.Position;
            direction.z = 0f;
        }

        if (!direction.IsNonZero)
        {
            direction = new Vec3(0f, 1f, 0f);
        }
        else
        {
            direction.Normalize();
        }

        var victimBone = isBeheading
            ? victim.Monster.NeckRootBoneIndex
            : (sbyte)0;
        var victimBodyPart = isBeheading
            ? BoneBodyPartType.Head
            : BoneBodyPartType.Chest;
        var damageType = isBeheading
            ? DamageTypes.Cut
            : DamageTypes.Blunt;
        var blow = new Blow(blowOwner.Index)
        {
            DamageType = damageType,
            InflictedDamage = 2000,
            DamageCalculated = true,
            BaseMagnitude = 0f,
            GlobalPosition = hitPosition,
            Direction = direction,
            SwingDirection = direction,
            AttackType = AgentAttackType.Standard,
            StrikeType = StrikeType.Swing,
            VictimBodyPart = victimBodyPart,
            BoneIndex = victimBone,
            BlowFlag = BlowFlags.None,
            NoIgnore = true,
            DefenderStunPeriod = 0f
        };
        blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
        var collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
            _attackBlockedWithShield: false,
            _correctSideShieldBlock: false,
            _isAlternativeAttack: false,
            _isColliderAgent: true,
            _collidedWithShieldOnBack: false,
            _isMissile: false,
            _isMissileBlockedWithWeapon: false,
            _missileHasPhysics: false,
            _entityExists: false,
            _thrustTipHit: false,
            _missileGoneUnderWater: false,
            _missileGoneOutOfBorder: false,
            collisionResult: CombatCollisionResult.StrikeAgent,
            affectorWeaponSlotOrMissileIndex: -1,
            StrikeType: (int)StrikeType.Swing,
            DamageType: (int)damageType,
            CollisionBoneIndex: victimBone,
            VictimHitBodyPart: victimBodyPart,
            AttackBoneIndex: 0,
            AttackDirection: Agent.UsageDirection.AttackDown,
            PhysicsMaterialIndex: 0,
            CollisionHitResultFlags: CombatHitResultFlags.NormalHit,
            AttackProgress: 0.5f,
            CollisionDistanceOnWeapon: 0f,
            AttackerStunPeriod: 0f,
            DefenderStunPeriod: 0f,
            MissileTotalDamage: 2000,
            MissileInitialSpeed: 0f,
            ChargeVelocity: 0f,
            FallSpeed: 0f,
            WeaponRotUp: Vec3.Up,
            _weaponBlowDir: direction,
            CollisionGlobalPosition: hitPosition,
            MissileVelocity: Vec3.Zero,
            MissileStartingPosition: hitPosition,
            VictimAgentCurVelocity: Vec3.Zero,
            GroundNormal: Vec3.Up);

        var previousMode = Mission.Mode;
        var changedMissionMode = previousMode is MissionMode.Conversation or MissionMode.CutScene;
        var restoreDisableDying = Mission.DisableDying;
        _blowAttempted = true;
        _killedObserved = false;
        _victimDeletedObserved = false;
        _forcedKilledDecisionCount = 0;
        _corpseRetentionPending = false;
        _corpseRetentionConfirmed = false;
        _corpseRetentionFailureLogged = false;
        _corpseRetentionProbeStarted = false;
        _corpseRegistrationLossLogged = false;
        _corpseSupportLossLogged = false;
        _corpseRetentionEligibleTick = 0;
        _corpseRetentionAttemptCount = 0;
        _corpseRetentionRetryElapsed = 0f;
        _corpseRetentionStatusElapsed = 0f;
        _corpseInitialAgentHeight = float.NaN;

        var lethalPoseAction = ActionIndexCache.act_none;
        var lethalPoseActionProgress = -1f;
        var lethalPoseOverrideApplied = false;
        var previousMortality = victim.CurrentMortalityState;
        var mortalityChanged = false;
        Exception? registerBlowException = null;
        try
        {
            ConfigureCorpseRetention();
            if (changedMissionMode)
            {
                Mission.SetMissionMode(MissionMode.Battle, atStart: false);
            }

            if (restoreDisableDying)
            {
                Mission.DisableDying = false;
            }

            victim.DisableScriptedMovement();
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            try
            {
                lethalPoseAction = victim.GetCurrentAction(0);
                lethalPoseActionProgress = victim.GetCurrentActionProgress(0);
                if (lethalPoseAction == ActionIndexCache.act_none)
                {
                    LogDeathInvariantFailed(
                        "the original prisoner's channel-0 lethal pose resolved to act_none; " +
                        "native death will continue without an action override");
                }
                else
                {
                    victim.SetOverridenStrikeAndDeathAction(
                        in lethalPoseAction,
                        in lethalPoseAction);
                    lethalPoseOverrideApplied = true;
                }
            }
            catch (Exception exception)
            {
                LogDeathInvariantFailed(
                    "could not retain the current kneeling action as the strike/death override; " +
                    "native death will continue",
                    exception);
            }

            RexLog.Info(
                $"Session {_request.SessionId} registering exactly one zero-impulse battlefield Blow " +
                $"from non-hero owner {blowOwner.Index}/team {blowOwner.Team.TeamIndex} to original " +
                $"prisoner {victim.Index}/team {victim.Team.TeamIndex}; damage=2000, " +
                $"knockDown=False, lethalPoseAction={lethalPoseAction.Index}, " +
                $"lethalPoseProgress={lethalPoseActionProgress:0.000}, " +
                $"deathOverride={lethalPoseOverrideApplied}, " +
                "manualRagdoll=False.");
            _deathDecisionGate = true;
            if (victim.CurrentMortalityState != Agent.MortalityState.Mortal)
            {
                mortalityChanged = true;
                victim.SetMortalityState(Agent.MortalityState.Mortal);
            }
            victim.RegisterBlow(blow, in collisionData);
        }
        catch (Exception exception)
        {
            // Bannerlord can complete Die/OnAgentRemoved before a later native
            // HandleBlowAux stage throws. Preserve that already-observed Killed
            // result and decide from cached callbacks after the finally block;
            // never turn a completed death into a false RegisterBlow failure.
            registerBlowException = exception;
        }
        finally
        {
            _deathDecisionGate = false;
            if (mortalityChanged && !_victimDeletedObserved && !_killedObserved)
            {
                try
                {
                    if (victim.IsActive() && victim.CurrentMortalityState == Agent.MortalityState.Mortal)
                    {
                        victim.SetMortalityState(previousMortality);
                    }
                }
                catch (Exception exception)
                {
                    LogDeathInvariantFailed("could not restore pre-lethal damage protection", exception);
                }
            }
            try
            {
                Mission.DisableDying = restoreDisableDying;
            }
            catch (Exception exception)
            {
                LogDeathInvariantFailed("could not restore Mission.DisableDying", exception);
            }

            try
            {
                if (changedMissionMode && Mission.Mode == MissionMode.Battle)
                {
                    Mission.SetMissionMode(previousMode, atStart: false);
                }
            }
            catch (Exception exception)
            {
                LogDeathInvariantFailed(
                    $"could not restore mission mode to {previousMode}",
                    exception);
            }
        }

        var killed = _killedObserved;
        var victimStateDescription = _victimDeletedObserved
            ? "Deleted"
            : killed
                ? "Killed(callback)"
                : "<unreadable>";
        if (!killed && !_victimDeletedObserved)
        {
            try
            {
                var victimState = victim.State;
                victimStateDescription = victimState.ToString();
                killed = victimState == AgentState.Killed;
            }
            catch (Exception exception)
            {
                LogDeathInvariantFailed(
                    "could not read the original prisoner's state after RegisterBlow",
                    exception);
            }
        }

        if (registerBlowException is not null)
        {
            if (!killed)
            {
                ClearLethalPoseOverrideAfterFailedDeath(victim, lethalPoseOverrideApplied);
                _result = ExecutionVictimDeathResult.RegisterBlowFailed;
                LogDeathInvariantFailed(
                    "the single native RegisterBlow threw before Killed was observed",
                    registerBlowException);
                return _result;
            }

            LogDeathInvariantFailed(
                "the single native RegisterBlow threw after native Killed was already observed; " +
                "the completed death is retained",
                registerBlowException);
        }

        if (_forcedKilledDecisionCount != 1)
        {
            LogDeathInvariantFailed(
                $"expected one forced Killed decision but observed {_forcedKilledDecisionCount}");
        }

        if (!killed)
        {
            ClearLethalPoseOverrideAfterFailedDeath(victim, lethalPoseOverrideApplied);
            _result = ExecutionVictimDeathResult.DeathNotConfirmed;
            LogDeathInvariantFailed(
                $"the single RegisterBlow returned without Killed (state={victimStateDescription})");
            return _result;
        }

        _result = ExecutionVictimDeathResult.Applied;
        if (_victimDeletedObserved)
        {
            _corpseRetentionPending = false;
            LogDeathInvariantFailed(
                "native Killed completed but the original prisoner Agent was synchronously deleted " +
                "before corpse-pool registration");
            return _result;
        }

        _corpseRetentionPending = true;
        // The controller normally runs before this behavior in Bannerlord's
        // reverse MissionBehavior tick order. +2 guarantees one complete later
        // mission tick before visibility monitoring begins. Do not call
        // AddAsCorpse here: Bannerlord owns the native death/corpse transition,
        // and forcing it one tick after RegisterBlow was the only non-native
        // intervention left in the v0.3.28 corpse path.
        _corpseRetentionEligibleTick = _missionTickSerial + 2;
        try
        {
            _corpseInitialAgentHeight = victim.Position.z;
        }
        catch (Exception exception)
        {
            LogDeathInvariantFailed("could not capture the killed prisoner's initial root height", exception);
        }

        RexLog.Info(
            $"Session {_request.SessionId} confirmed mission-only Killed for original prisoner " +
            $"Agent {victim.Index}; native corpse visibility monitoring begins after a later mission tick " +
            "without forcing native corpse registration.");
        if (!_request.Victim.IsAlive)
        {
            LogDeathInvariantFailed(
                "the campaign hero died during the mission Blow despite BasicBattleAgentOrigin");
        }

        return _result;
    }

    private void ClearLethalPoseOverrideAfterFailedDeath(Agent victim, bool overrideApplied)
    {
        if (!overrideApplied || _victimDeletedObserved)
        {
            return;
        }

        try
        {
            if (!victim.IsActive())
            {
                RexLog.Info(
                    $"Session {_request.SessionId} left original prisoner Agent {victim.Index}'s " +
                    "temporary lethal-pose override untouched because the Agent was no longer active.");
                return;
            }

            var none = ActionIndexCache.act_none;
            victim.SetOverridenStrikeAndDeathAction(in none, in none);
            RexLog.Info(
                $"Session {_request.SessionId} cleared the original prisoner Agent {victim.Index}'s " +
                "temporary lethal-pose override because Killed was not confirmed.");
        }
        catch (Exception exception)
        {
            LogDeathInvariantFailed(
                "could not clear the temporary lethal-pose override after an unconfirmed death",
                exception);
        }
    }

    public AgentState GetAgentState(
        Agent affectedAgent,
        float deathProbability,
        out bool usedSurgery)
    {
        usedSurgery = false;
        if (ReferenceEquals(affectedAgent, _victim) &&
            (_deathDecisionGate || _externalVictimDeathArmed))
        {
            if (_deathDecisionGate)
            {
                _forcedKilledDecisionCount++;
                if (_forcedKilledDecisionCount == 1)
                {
                    RexLog.Info(
                        $"Session {_request.SessionId} forced the original prisoner Agent's mission-only " +
                        "death decision to Killed; the campaign hero remains alive until mission exit.");
                }
                else
                {
                    LogDeathInvariantFailed(
                        $"the one RegisterBlow requested Killed {_forcedKilledDecisionCount} times");
                }
            }
            else if (!_externalKilledDecisionLogged)
            {
                _externalKilledDecisionLogged = true;
                RexLog.Info(
                    $"Session {_request.SessionId} accepted a real external lethal hit on the original " +
                    $"prisoner Agent {affectedAgent.Index} as Killed.");
            }

            return AgentState.Killed;
        }

        // None delegates to the native death fallback; it is NOT a veto.
        // Non-projectile actors are protected with native Invulnerable mortality
        // until the validated lethal frame. Unexpected removal is detected by
        // OnAgentRemoved and safely cancels the still-uncommitted ceremony.
        if (ReferenceEquals(affectedAgent, _victim))
        {
            if (!_victimRoutQueryLogged)
            {
                _victimRoutQueryLogged = true;
                RexLog.Warning(
                    $"Session {_request.SessionId} received an unexpected native death query for the " +
                    "protected prisoner outside the lethal flow; native fallback will decide it, " +
                    "and unexpected removal will cancel the uncommitted ceremony.");
            }

            return AgentState.None;
        }

        if (_downstreamDecider is null)
        {
            return AgentState.None;
        }

        try
        {
            return _downstreamDecider.GetAgentState(
                affectedAgent,
                deathProbability,
                out usedSurgery);
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {_request.SessionId} could not forward an unrelated Agent-state decision; " +
                "native fallback will decide it.",
                exception);
            usedSurgery = false;
            return AgentState.None;
        }
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        _missionTickSerial++;
        MaintainBoundVictimHostageHold();
        MaintainVictimCorpseRetention(dt);
    }

    /// <summary>
    /// Keeps the still-living staged prisoner from being routed by the engine's
    /// own morale model. Vanilla removes a panicked Agent from the mission as
    /// <see cref="AgentState.Routed"/>; for a restrained prisoner that is a
    /// scene-killing misfire (the ceremony is destroyed long before the lethal
    /// blow). The hold stays on until the prisoner is actually killed or the
    /// mission tears down, and never touches the death decision itself.
    /// </summary>
    private void MaintainBoundVictimHostageHold()
    {
        if (!_hostageHoldActive)
        {
            return;
        }

        var victim = _victim;
        if (victim is null)
        {
            _hostageHoldActive = false;
            return;
        }

        AgentState state;
        try
        {
            state = victim.State;
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Could not read the held prisoner's Agent state; the hostage hold stays armed. " +
                exception.Message);
            return;
        }

        if (state != AgentState.Active)
        {
            _hostageHoldActive = false;
            RexLog.Info(
                $"Session {_request.SessionId} ended the hostage morale hold because original prisoner " +
                $"Agent {victim.Index} is no longer Active (state={state}).");
            return;
        }

        try
        {
            var ai = victim.CommonAIComponent;
            if (ai is null)
            {
                return;
            }

            if (ai.IsRetreating || ai.IsPanicked)
            {
                if (!_hostageRoutObserved)
                {
                    _hostageRoutObserved = true;
                    RexLog.Warning(
                        $"Session {_request.SessionId} original prisoner Agent {victim.Index} entered " +
                        $"panic/retreat (panicked={ai.IsPanicked}, retreating={ai.IsRetreating}, " +
                        $"morale={ai.Morale:0.00}); the hostage hold is clearing it so the ceremony " +
                        "cannot be cancelled by a morale rout.");
                }

                // StopRetreating also clears IsPanicked and lifts morale off the
                // panic floor, which is the state the engine routes from.
                if (ai.IsRetreating)
                {
                    ai.StopRetreating();
                }
            }

            if (ai.Morale < HostageMoralePerTick)
            {
                ai.Morale = HostageMoralePerTick;
            }

            if (!_hostageHoldLogged)
            {
                _hostageHoldLogged = true;
                RexLog.Info(
                    $"Session {_request.SessionId} is holding original prisoner Agent {victim.Index} " +
                    $"at morale {HostageMoralePerTick:0} so a restrained prisoner cannot be removed " +
                    "as Routed before the lethal blow.");
            }
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Could not top up the held prisoner's morale this tick; the hold will retry. " +
                exception.Message);
        }
    }

    public override void OnAgentRemoved(
        Agent affectedAgent,
        Agent affectorAgent,
        AgentState agentState,
        KillingBlow blow)
    {
        base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        if (!ReferenceEquals(affectedAgent, _victim))
        {
            return;
        }

        if (_campaignExecutionCommitted && agentState != AgentState.Killed)
        {
            RexLog.Info(
                $"Session {_request.SessionId} ignored original prisoner Agent {affectedAgent.Index} " +
                $"removal as {agentState} during post-commit mission teardown.");
            return;
        }

        if (agentState == AgentState.Killed &&
            (_blowAttempted || _externalVictimDeathArmed))
        {
            _killedObserved = true;
            if (_externalVictimDeathArmed && !_blowAttempted)
            {
                _result = ExecutionVictimDeathResult.Applied;
                _corpseRetentionPending = true;
                _corpseRetentionEligibleTick = _missionTickSerial + 2;
                _corpseRetentionRetryElapsed = 0f;
                _corpseRetentionStatusElapsed = 0f;
                _corpseRetentionProbeStarted = false;
                _corpseRetentionConfirmed = false;
                _corpseRetentionFailureLogged = false;
                _corpseRegistrationLossLogged = false;
                _corpseSupportLossLogged = false;
                try
                {
                    _corpseInitialAgentHeight = affectedAgent.Position.z;
                }
                catch (Exception exception)
                {
                    LogDeathInvariantFailed(
                        "could not capture the externally killed prisoner's initial root height",
                        exception);
                }
            }

            RexLog.Info(
                $"Session {_request.SessionId} observed native Killed removal for original prisoner " +
                $"Agent {affectedAgent.Index} (affector={affectorAgent?.Index ?? -1}, " +
                $"externalCombat={_externalVictimDeathArmed && !_blowAttempted}).");
            return;
        }

        _unexpectedVictimRemoval = true;
        LogDeathInvariantFailed(
            $"original prisoner Agent {affectedAgent.Index} was removed as {agentState} " +
            $"(blowAttempted={_blowAttempted})");

        // A Routed prisoner removes the whole visual reference for the ceremony.
        // Turn it into a hard, self-describing failure so a future regression in
        // the hostage morale hold is impossible to miss in the log.
        if (agentState == AgentState.Routed)
        {
            RexLog.Error(
                $"DEATH_INVARIANT_FAILED session={_request.SessionId}: original prisoner " +
                $"Agent {affectedAgent.Index} was routed out of the scene; the pre-execution " +
                "address and lethal flow can no longer run for this session. The hostage morale " +
                "hold should have prevented this.");
            _hostageHoldActive = false;
        }
    }

    public override void OnAgentDeleted(Agent affectedAgent)
    {
        base.OnAgentDeleted(affectedAgent);
        if (!ReferenceEquals(affectedAgent, _victim))
        {
            return;
        }

        _victimDeletedObserved = true;
        _corpseRetentionPending = false;
        if (_campaignExecutionCommitted)
        {
            RexLog.Info(
                $"Session {_request.SessionId} ignored original prisoner Agent {affectedAgent.Index} " +
                "deletion during post-commit mission teardown.");
            return;
        }

        _unexpectedVictimRemoval = !_killedObserved;
        LogDeathInvariantFailed(
            $"original prisoner Agent {affectedAgent.Index} was deleted " +
            $"(result={_result}, nativeCorpseObserved={_corpseRetentionConfirmed}, " +
            $"retentionProbes={_corpseRetentionAttemptCount})");
    }

    private bool IsValidBinding(
        Agent victim,
        Agent blowOwner,
        bool requireActive,
        out string failure)
    {
        if (ReferenceEquals(victim, blowOwner))
        {
            failure = "victim and Blow owner are the same Agent";
            return false;
        }

        if ((requireActive && (!victim.IsActive() || !blowOwner.IsActive())) ||
            victim.State is AgentState.Killed or AgentState.Unconscious or AgentState.Deleted)
        {
            failure = $"Agent availability is invalid (victim={victim.State}, owner={blowOwner.State})";
            return false;
        }

        if (!ReferenceEquals(victim.Character, _request.Victim.CharacterObject))
        {
            failure = "victim CharacterObject is not the selected hero";
            return false;
        }

        if (victim.Origin is not BasicBattleAgentOrigin)
        {
            failure = $"victim origin is {victim.Origin?.GetType().Name ?? "null"}, not BasicBattleAgentOrigin";
            return false;
        }

        if (blowOwner.Character?.IsHero != false)
        {
            failure = "Blow owner is missing or is a hero";
            return false;
        }

        if (victim.Team is null || blowOwner.Team is null ||
            !blowOwner.Team.IsEnemyOf(victim.Team) ||
            !victim.Team.IsEnemyOf(blowOwner.Team))
        {
            failure = "victim and Blow owner do not have mutual enemy teams";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    private void ConfigureCorpseRetention()
    {
        try
        {
            Mission.SetOverrideCorpseCount(ExecutionCorpseBudget);
        }
        catch (Exception exception)
        {
            LogDeathInvariantFailed("could not set the native corpse budget to 64", exception);
        }

        try
        {
            Mission.SetMissionCorpseFadeOutTimeInSeconds(ExecutionCorpseFadeOutSeconds);
        }
        catch (Exception exception)
        {
            LogDeathInvariantFailed("could not set native corpse fade-out to 900 seconds", exception);
        }
    }

    private void MaintainVictimCorpseRetention(float dt)
    {
        if (!_corpseRetentionPending)
        {
            return;
        }

        if (_missionTickSerial < _corpseRetentionEligibleTick)
        {
            return;
        }

        var elapsed = MathF.Max(0f, dt);
        _corpseRetentionRetryElapsed += elapsed;
        _corpseRetentionStatusElapsed += elapsed;
        if (_corpseRetentionProbeStarted &&
            _corpseRetentionRetryElapsed < CorpseRetentionRetryIntervalSeconds)
        {
            return;
        }

        var firstProbe = !_corpseRetentionProbeStarted;
        _corpseRetentionProbeStarted = true;
        _corpseRetentionRetryElapsed = 0f;
        var reportStatus = firstProbe ||
                           _corpseRetentionStatusElapsed >= CorpseRetentionStatusIntervalSeconds;
        if (firstProbe)
        {
            // Mission-mode changes and third-party mission behaviors can rewrite
            // these global native settings after the lethal call. Reapply them
            // once after the original mission mode has been restored.
            ConfigureCorpseRetention();
        }

        if (reportStatus)
        {
            _corpseRetentionStatusElapsed = 0f;
        }

        var victim = _victim;
        if (victim is null || _victimDeletedObserved)
        {
            _corpseRetentionPending = false;
            LogDeathInvariantFailed(
                "could not retain the original prisoner corpse because its Agent was deleted");
            return;
        }

        AgentState victimState;
        try
        {
            victimState = victim.State;
        }
        catch (Exception exception)
        {
            if (!_corpseRetentionFailureLogged || _corpseRetentionAttemptCount % 8 == 0)
            {
                _corpseRetentionFailureLogged = true;
                LogDeathInvariantFailed("could not read the retained prisoner's Agent state", exception);
            }

            return;
        }

        if (victimState != AgentState.Killed)
        {
            if (!_corpseRetentionFailureLogged)
            {
                _corpseRetentionFailureLogged = true;
                LogDeathInvariantFailed(
                    $"cannot retain the original prisoner corpse while state={victimState}; monitoring continues");
            }

            return;
        }

        try
        {
            _corpseRetentionAttemptCount++;
            var nativeCorpseRegistered = victim.IsAddedAsCorpse();
            if (nativeCorpseRegistered && !_corpseRetentionConfirmed)
            {
                _corpseRetentionConfirmed = true;
                _corpseRegistrationLossLogged = false;
                RexLog.Info(
                    $"Session {_request.SessionId} observed Bannerlord register original prisoner " +
                    $"Agent {victim.Index} in its native corpse pool after " +
                    $"{_corpseRetentionAttemptCount} probe(s); no forced corpse-registration call was used.");
            }
            else if (!nativeCorpseRegistered &&
                     _corpseRetentionConfirmed &&
                     !_corpseRegistrationLossLogged)
            {
                _corpseRegistrationLossLogged = true;
                LogDeathInvariantFailed(
                    "Bannerlord stopped reporting the killed prisoner in its corpse pool; " +
                    "the Agent is left untouched and visibility monitoring continues");
            }

            var isFadingOut = victim.IsFadingOut();
            var visuals = victim.AgentVisuals;
            var visualsValid = visuals is not null && visuals.IsValid();
            var entity = visualsValid ? visuals!.GetEntity() : null;
            var entityVisible = entity is not null && entity.IsVisibleIncludeParents();

            var visualHeight = entity is null ? float.NaN : entity.GlobalPosition.z;
            var agentHeight = victim.Position.z;
            var ragdollState = "<unavailable>";
            if (visualsValid)
            {
                var skeleton = visuals!.GetSkeleton();
                if (skeleton is not null && skeleton.IsValid)
                {
                    ragdollState = skeleton.GetCurrentRagdollState().ToString();
                }
            }

            if (!_corpseSupportLossLogged &&
                !float.IsNaN(_corpseInitialAgentHeight) &&
                agentHeight < _corpseInitialAgentHeight - 2f)
            {
                _corpseSupportLossLogged = true;
                LogDeathInvariantFailed(
                    $"the killed prisoner's root fell more than 2m below its lethal height " +
                    $"(initialZ={_corpseInitialAgentHeight:0.000}, currentZ={agentHeight:0.000}); " +
                    "this indicates physical support loss rather than visual fade");
            }

            if (reportStatus)
            {
                RexLog.Info(
                    $"CORPSE_STATUS session={_request.SessionId} agent={victim.Index} " +
                    $"state={victimState} nativeAdded={nativeCorpseRegistered} " +
                    $"fading={isFadingOut} visualsValid={visualsValid} visible={entityVisible} " +
                    $"agentZ={agentHeight:0.000} visualZ={visualHeight:0.000} " +
                    $"ragdoll={ragdollState} probes={_corpseRetentionAttemptCount} " +
                    $"fade={ExecutionCorpseFadeOutSeconds:0}s budget={ExecutionCorpseBudget}.");
            }
        }
        catch (Exception exception)
        {
            if (!_corpseRetentionFailureLogged || _corpseRetentionAttemptCount % 8 == 0)
            {
                _corpseRetentionFailureLogged = true;
                LogDeathInvariantFailed(
                    $"native corpse visibility probe {_corpseRetentionAttemptCount} threw; monitoring continues",
                    exception);
            }
        }
    }

    private void LogDeathInvariantFailed(string detail, Exception? exception = null)
    {
        RexLog.Error(
            $"DEATH_INVARIANT_FAILED session={_request.SessionId}: {detail}. " +
            "The mission remains active; no replacement, forced exit or campaign death is performed here.",
            exception);
    }
}
