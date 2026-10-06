using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private bool TryInitializeScene()
    {
        if (_initialized || _initializationFailed)
        {
            return _initialized;
        }

        var currentPlayer = _playerAgent ?? Mission.MainAgent ?? Agent.Main;
        if (currentPlayer is null || !currentPlayer.IsActive())
        {
            return false;
        }

        if (_playerAgent is null)
        {
            _playerAgent = currentPlayer;
            RexLog.Info(
                $"Session {Request.SessionId} bound the town-entry player agent " +
                $"(index={_playerAgent.Index}, character={_playerAgent.Character?.StringId ?? "<unknown>"}).");
        }

        if (!EnsureBoundPlayerRemainsMainAgent())
        {
            return false;
        }

        var validation = _service.Validate(Request, ExecutionValidationStage.MissionStart);
        if (!validation.IsValid)
        {
            CancelSceneAndReturn(validation.Reason, validation.Message);
            return false;
        }

        if (!_sceneActs.TryGet(CurrentMethod.SceneActId, out _sceneAct))
        {
            CancelSceneAndReturn(
                ExecutionFailureReason.ScenePlacementFailed,
                new TextObject(
                    "{=REX_Error_Scene_Act_Missing}The selected punishment has no registered scene act. Nothing was spent and the prisoner lives."));
            return false;
        }

        if (!ExecutionSceneVisualProfiles.TryGet(CurrentMethod.StringId, out _visualProfile))
        {
            _visualProfile = ExecutionSceneVisualProfiles.CreateGenericPlaceholder(
                CurrentMethod.StringId);
            RexLog.Warning(
                $"Method '{CurrentMethod.StringId}' has no registered visual profile; using the collision-free generic placeholder.");
        }

        if (UsesConfirmedCustomSite)
        {
            _visualProfile = BuildCustomSiteVisualProfile(_visualProfile);
        }

        if (!UsesConfirmedCustomSite &&
            _initializationElapsed + 0.0001f < _nextPlacementAttemptElapsed)
        {
            return false;
        }

        if (!UsesConfirmedCustomSite)
        {
            _nextPlacementAttemptElapsed = _initializationElapsed + PlacementRetryIntervalSeconds;
            try
            {
                if (!ExecutionScenePlacementSolver.TryFind(
                        Mission,
                        UsesGroundExecutionSite(),
                        out _placement))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    "The town-center placement solver failed on this scene; cancelling without committing the execution.",
                    exception);
                CancelSceneAndReturn(
                    ExecutionFailureReason.ScenePlacementFailed,
                    new TextObject(
                        "{=REX_Error_No_Safe_Position}No safe navigable execution site could be found in this town center. Nothing was spent and the prisoner lives."));
                return false;
            }
        }
        else
        {
            RexLog.Info(
                $"Session {Request.SessionId} bypassed the automatic town-gate placement solver and used the player-confirmed custom root.");
        }

        try
        {
            var missionEntryPosition = _playerAgent.Position;
            // Decoration agents need the exclusion set during scene construction.
            _conversationLogic = Mission.GetMissionBehavior<MissionConversationLogic>();
            BuildRuntimeScene();
            _conversationLogic?.DisableStartConversation(true);
            Mission.FocusableObjectInformationProvider.AddInfoCallback(GetExecutionFocusText);
            _focusCallbackRegistered = true;
            _stateMachine.TransitionTo(ExecutionSessionState.WaitingForPlayer);
            _initialized = true;
            SetCeremonyAlliesFormationControl(enabled: true, "scene-ready standby");
            _stateElapsed = 0f;
            _speechOverlayWaitElapsed = 0f;

            var placementText = UsesConfirmedCustomSite
                ? new TextObject(
                    "{=REX_Builder_Ceremony_Ready}The custom execution site is ready. The prisoner and ceremony personnel have now entered the scene.")
                : new TextObject(
                    "{=REX_Scene_Ready}You have been escorted beside the public execution scaffold outside the main gate.");
            InformationManager.DisplayMessage(new InformationMessage(placementText.ToString()));
            var entryDistanceBeforeEscort = missionEntryPosition.AsVec2.Distance(_placement!.Origin.AsVec2);
            RexLog.Info(
                $"Session {Request.SessionId} scene ready ({_placement.Source}, " +
                $"entryDistanceBeforeEscort={entryDistanceBeforeEscort:0.00}m, " +
                $"fullStage={_placement.SupportsFullStage}, raised={_stageSurfaceHeight > 0f}, " +
                $"compatibilityFallback={_placement.IsCompatibilityFallback}, " +
                $"crowd={_crowdAgents.Count}).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Runtime execution-scene construction failed.", exception);
            CancelSceneAndReturn(
                ExecutionFailureReason.ScenePlacementFailed,
                new TextObject(
                    "{=REX_Error_Scene_Build}The execution site could not be built safely. Nothing was spent and the prisoner lives."));
            return false;
        }
    }

    private void BuildRuntimeScene()
    {
        if (_placement is null || _visualProfile is null)
        {
            throw new InvalidOperationException(
                "A placement and visual profile are required before building the execution scene.");
        }

        var ceremonyLateralShift = GetMethodCeremonyLateralShift();
        var ceremonyForwardShift = GetMethodVictimForwardShift();
        _victimPosition = TryGetCustomMarkerWorld("victim", out var customVictimPosition, out _)
            ? customVictimPosition
            : _placement.Offset(ceremonyLateralShift, ceremonyForwardShift);
        if (MathF.Abs(ceremonyLateralShift) > 0.001f)
        {
            var lateralDirection = ceremonyLateralShift < 0f ? "left" : "right";
            RexLog.Info(
                $"Session {Request.SessionId} shifted the complete {CurrentMethod.StringId} ritual group " +
                $"{MathF.Abs(ceremonyLateralShift):0.00}m toward scaffold {lateralDirection}.");
        }
        if (MathF.Abs(ceremonyForwardShift) > 0.001f)
        {
            RexLog.Info(
                $"Session {Request.SessionId} moved the {CurrentMethod.StringId} prisoner ritual point " +
                $"{MathF.Abs(ceremonyForwardShift):0.00}m behind the shared firing origin; " +
                "the player and executioner firing area remains at the original site mark.");
        }
        if (!TryResolveCeremonyTeams(out var executionTeam, out var victimTeam))
        {
            throw new InvalidOperationException(
                "The ceremony could not establish an execution side and a genuinely hostile victim side.");
        }

        _executionTeam = executionTeam;
        _victimTeam = victimTeam;
        if (!TryResolveInitialExecutionerPosition(out _executionerPosition))
        {
            throw new InvalidOperationException(
                "The executioner waiting position is not safely navigable.");
        }

        var facing = _placement.Forward;
        if (!TrySpawnOriginalHeroVictim(facing, out var originalHeroAgent))
        {
            throw new InvalidOperationException(
                "The selected prisoner hero could not be staged safely; no substitute is permitted.");
        }

        _victimAgent = originalHeroAgent;
        VengeanceIntegration.RegisterProtectedVictim(_victimAgent);
        RexLog.Info(
            $"Session {Request.SessionId} spawned only the original prisoner hero character " +
            $"'{Request.Victim.CharacterObject.StringId}' with its visual equipment and a deferred " +
            "campaign-death presentation.");
        TryApplyVictimActionSet(originalHeroAgent);
        if (UsesStoningExecution() && !TryProtectStoningVictim())
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not establish the stoning prisoner's barrage protection; " +
                "the scene remains open and posture maintenance will keep retrying.");
        }
        var executionerCharacter = SelectExecutionerTroopForCurrentSite() ??
                                   throw new InvalidOperationException(
                                       "The venue culture has no eligible executioner troop.");
        _executionerAgent = SpawnCharacter(
            executionerCharacter,
            _executionerPosition,
            facing,
            civilianEquipment: false,
            noWeapons: true,
            invulnerable: true,
            teamOverride: _executionTeam,
            joinPlayerFormation: true);
        if (!EnsureGeneratedAllyOnPlayerTeam(_executionerAgent, "executioner") ||
            _executionerAgent.Team != _executionTeam ||
            _victimTeam is null ||
            !_executionerAgent.Team.IsEnemyOf(_victimTeam))
        {
            throw new InvalidOperationException(
                "The executioner did not retain the valid team opposing the prisoner.");
        }
        if (!_deathLogic.TryBind(_victimAgent, _executionerAgent))
        {
            throw new InvalidOperationException(
                "The execution death logic could not bind the original prisoner and non-hero executioner.");
        }
        if (UsesStoningExecution() && !_deathLogic.TryArmExternalVictimDeath())
        {
            throw new InvalidOperationException(
                "The stoning prisoner could not be bound to real projectile-death settlement.");
        }
        if (UsesCrossbowExecution() && !TryPrepareCrossbowVictimForRealCombatDeath())
        {
            throw new InvalidOperationException(
                "The crossbow prisoner could not be armed for real native missile death.");
        }

        if (RequiresCeremonyWeapon() &&
            !TryEquipCeremonyWeapon(_executionerAgent, "executioner"))
        {
            throw new InvalidOperationException(
                $"The ceremony weapon for '{CurrentMethod.StringId}' could not be equipped.");
        }
        else if (!RequiresCeremonyWeapon())
        {
            _executionerAgent.TryToSheathWeaponInHand(
                Agent.HandIndex.MainHand,
                Agent.WeaponWieldActionType.Instant);
            _executionerAgent.TryToSheathWeaponInHand(
                Agent.HandIndex.OffHand,
                Agent.WeaponWieldActionType.Instant);
        }
        RexLog.Info(
            $"Session {Request.SessionId} executioner troop: {executionerCharacter.StringId} (T{executionerCharacter.Tier}).");

        // Beheading and hanging retain the complete native gallows. Burning is
        // deliberately staged on the verified gate ground with only its stake,
        // kindling and fire effects; no platform or stair entity is created.
        if (UsesGroundExecutionSite()
                ? !TryPrepareGroundExecutionSite()
                : !TryCreateGallowsStage())
        {
            throw new InvalidOperationException(
                UsesGroundExecutionSite()
                    ? "The ground burning site could not be prepared at the outside gate."
                    : "The native gallows could not be created safely at the outside gate.");
        }

        _strategy.SetupSceneVisuals();

        // The player arrives at the method-appropriate entry: beside the
        // verified stairs for scaffold methods, or in front of the ground pyre.
        if (!TryPlacePlayerAtScaffoldEntry())
        {
            throw new InvalidOperationException(
                "No collision-safe player entry position could be found beside the execution site.");
        }

        if ((UsesStoningExecution() || UsesCrossbowExecution()) &&
            _playerAgent is not null &&
            !PreparePlayerExecutionEquipment(_playerAgent))
        {
            throw new InvalidOperationException(
                $"The player could not receive the entry {CurrentMethod.StringId} weapon and ammunition.");
        }

        if (RequiresExecutionTorch())
        {
            if (_playerAgent is null)
            {
                throw new InvalidOperationException(
                    "The player Agent disappeared after the burning-scene entry was established.");
            }

            // Burning is intentionally performed from the player's current
            // position anywhere along the validated outer ignition band. Do
            // not demand a second exact 3m action mark during scene creation:
            // compact and third-party town scenes may have a valid entrance
            // and ignition band without exposing that one sampled point.
            _playerExecutionActorPosition = _playerAgent.Position;
            RexLog.Info(
                $"Session {Request.SessionId} retained the burning player's validated entry position " +
                $"({_playerExecutionActorPosition.x:0.000}, {_playerExecutionActorPosition.y:0.000}, " +
                $"{_playerExecutionActorPosition.z:0.000}); the live position will be captured again when F is pressed.");
        }
        else if (!TryResolveExecutionActorPosition(
                     ExecutionActor.Player,
                     out _playerExecutionActorPosition))
        {
            throw new InvalidOperationException(
                "The player execution mark could not be resolved.");
        }

        if (UsesCrossbowExecution())
        {
            // The crossbow executioner was created directly on this exact firing
            // mark. Re-sampling the mark after the Agent exists can produce a
            // second ground/nav result or reject the position as occupied, which
            // used to cause a visible correction and could abort NPC execution.
            // Keep one authoritative root from entry through ready, aim and fire.
            _npcExecutionActorPosition = _executionerPosition;
            RexLog.Info(
                $"Session {Request.SessionId} bound the crossbow NPC action mark directly to its " +
                $"entry spawn at ({_npcExecutionActorPosition.x:0.000}, " +
                $"{_npcExecutionActorPosition.y:0.000}, {_npcExecutionActorPosition.z:0.000}); " +
                "no second placement sample or pre-shot relocation will occur.");
        }
        else if (!TryResolveExecutionActorPosition(
                     ExecutionActor.Executioner,
                     out _npcExecutionActorPosition))
        {
            throw new InvalidOperationException(
                "The NPC execution mark could not be resolved.");
        }

        if (UsesCrossbowExecution() && !TrySpawnCrossbowGroundWeapons())
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not create one or more pickup weapons at the crossbow firing line; " +
                "the equipped executioner crossbow and real missile execution remain available.");
        }

        if (UsesCrossbowExecution())
        {
            SpawnCrossbowVolleyAgents();
        }

        // Resolve fixed guard slots first. The executioner standby sampler can
        // then see their real agents and collision capsules, including troops
        // supplied by replacement culture trees, instead of guessing from the
        // authored offsets.
        SpawnGuards(_victimPosition, facing);

        var initialExecutionerPosition = _executionerPosition;
        Vec3 executionerWaitingPosition;
        if (UsesStoningExecution() || UsesCrossbowExecution())
        {
            // Stoning and crossbow executioners are born directly on their
            // authored action marks. Never run the generic waiting sampler or
            // introduce a visible second move before the sentence starts.
            executionerWaitingPosition = initialExecutionerPosition;
            RexLog.Info(
                $"Session {Request.SessionId} retained {CurrentMethod.StringId} executioner entry spawn " +
                "as its permanent standby/action mark " +
                $"at ({executionerWaitingPosition.x:0.000}, {executionerWaitingPosition.y:0.000}, " +
                $"{executionerWaitingPosition.z:0.000}).");
        }
        else if (!TryResolveExecutionerWaitingPosition(out executionerWaitingPosition))
        {
            throw new InvalidOperationException(
                "No collision-safe gallows-deck standby mark could remain clear of both ceremony actors.");
        }

        _occupiedSpawnPositions.RemoveAll(
            position => position.DistanceSquared(initialExecutionerPosition) < 0.01f);
        _executionerPosition = executionerWaitingPosition;
        _executionerStandbyPosition = executionerWaitingPosition;
        _executionerStandbyDirection = GetDirectionToVictim(executionerWaitingPosition);
        _executionerStandbyRestored = false;
        _occupiedSpawnPositions.Add(_executionerPosition);
        _executionerAgent.DisableScriptedMovement();
        _executionerAgent.Controller = AgentControllerType.None;
        PoseAgentAt(
            _executionerAgent,
            _executionerPosition,
            _executionerStandbyDirection);

        ApplyStageElevationToCeremonyMarks(facing);
        foreach (var decoration in _visualProfile.Decorations)
        {
            SpawnProfileProp(decoration);
        }
        SpawnCustomSiteLoosePieces();

        // Moving method props remain collision-free. Fixed frames/supports use
        // authored physics selected by the visual profile. The compact hidden
        // prisoner focus proxy; burning receives a wide raycast-only capsule that
        // reaches the outer kindling edge without creating a physical air wall.
        TryCreateUsePointMarker();

        SpawnCrowd();
        if (UsesStoningExecution())
        {
            _executionerRetreatPosition = _executionerStandbyPosition;
            _executionerRetreatDirection = _executionerStandbyDirection;
        }
        else if (!TryResolveExecutionerRetreatPosition(
                out _executionerRetreatPosition,
                out _executionerRetreatDirection))
        {
            RexLog.Warning(
                "No executioner retreat mark is currently clear; player execution will retry after interaction.");
        }

        PrefetchCeremonyActions();
        TryPlayAction(_victimAgent, _sceneAct!.VictimIdleAction, "victim idle");
        // Take the axe/torch before the standby idle is bound. Wielding drives its own
        // channel-0 animation, so equipping afterwards displaces the idle and the
        // read-back reports a pose the executioner never kept.
        if (RequiresCeremonyWeapon() &&
            !TryEnsureCeremonyWeaponWielded(_executionerAgent, "standby executioner before idle"))
        {
            throw new InvalidOperationException(
                "The executioner could not take the ceremony weapon before the standby action was bound.");
        }

        if (!TryRestoreExecutionerEntryStandby())
        {
            _executionerInitialStandbySettlePending = true;
            _executionerInitialStandbySettleElapsed = 0f;
            RexLog.Warning(
                $"Session {Request.SessionId} will retry the executioner standby mark on the first mission tick.");
        }

        if (RequiresCeremonyWeapon() &&
            !TryEnsureCeremonyWeaponWielded(_executionerAgent, "standby executioner after idle"))
        {
            throw new InvalidOperationException(
                "The executioner could not retain the ceremony weapon after the standby action was bound.");
        }

        // Run apparatus binding last. Generic victim/executioner idle setup is
        // complete now, so it cannot overwrite the wheel-bound or suspended
        // placeholder pose on the following initialization statement.
        if (_strategy is IMethodSceneReadyStrategy sceneReadyStrategy)
        {
            sceneReadyStrategy.OnMethodSceneReady();
        }

        RexLog.Info(
            $"Session {Request.SessionId} visual profile: {_visualProfile.MethodId} ({_visualProfile.Fidelity}).");
    }

    private bool TrySpawnOriginalHeroVictim(Vec2 facing, out Agent spawnedAgent)
    {
        spawnedAgent = null!;
        var sourceCharacter = Request.Victim.CharacterObject;
        if (!sourceCharacter.IsHero || !ReferenceEquals(sourceCharacter.HeroObject, Request.Victim))
        {
            RexLog.Warning(
                $"Session {Request.SessionId} could not use the requested prisoner as the direct source " +
                "because its current CharacterObject no longer resolves to that hero.");
            return false;
        }

        try
        {
            if (_victimTeam is null || !_victimTeam.IsValid)
            {
                RexLog.Error(
                    $"Session {Request.SessionId} has no valid hostile team for the original prisoner hero.");
                return false;
            }

            var candidate = SpawnCharacter(
                sourceCharacter,
                _victimPosition,
                facing,
                civilianEquipment: false,
                noWeapons: true,
                // Each method explicitly enables its validated real-death path.
                // Until then native damage must not remove the staged actor.
                invulnerable: true,
                equipmentOverride: CreateVictimPresentationEquipment(),
                bodyPropertiesOverride: Request.Victim.BodyProperties,
                teamOverride: _victimTeam,
                femaleOverride: Request.Victim.IsFemale,
                ageOverride: GetVictimPresentationAge(),
                raceOverride: sourceCharacter.Race,
                clothingColor1Override: GetVictimClothingColor1(),
                clothingColor2Override: GetVictimClothingColor2(),
                fixedEquipment: true,
                originOverride: new BasicBattleAgentOrigin(sourceCharacter));
            if (!HasUsableVictimVisuals(candidate, "original hero character"))
            {
                RetireProvisionalVictimAgent(candidate, "its native visuals were unavailable");
                return false;
            }

            if (candidate.Team != _victimTeam ||
                _executionTeam is null ||
                !_executionTeam.IsEnemyOf(candidate.Team) ||
                !candidate.Team.IsEnemyOf(_executionTeam))
            {
                RetireProvisionalVictimAgent(candidate, "its hostile battle-team assignment was not retained");
                return false;
            }

            spawnedAgent = candidate;
            RexLog.Info(
                $"Session {Request.SessionId} staged the original hero CharacterObject with " +
                $"BasicBattleAgentOrigin on hostile team {candidate.Team.TeamIndex}; " +
                "mission death cannot call campaign ApplyByBattle through this origin.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not spawn the original prisoner hero character; " +
                "the ceremony will be cancelled without a substitute.",
                exception);
            return false;
        }
    }

    private Equipment CreateVictimPresentationEquipment()
    {
        try
        {
            var civilianEquipment = Request.Victim.CivilianEquipment;
            if (!civilianEquipment.IsEmpty())
            {
                return civilianEquipment.Clone(cloneWithoutWeapons: true);
            }

            var battleEquipment = Request.Victim.BattleEquipment;
            if (!battleEquipment.IsEmpty())
            {
                return battleEquipment.Clone(cloneWithoutWeapons: true);
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not clone the prisoner's equipment for the " +
                "execution presentation; using the prisoner's empty equipment state.",
                exception);
        }

        return new Equipment();
    }

    private int GetVictimPresentationAge() =>
        Math.Max(18, Math.Min(90, (int)MathF.Round(Request.Victim.Age)));

    private uint GetVictimClothingColor1() =>
        Request.Victim.MapFaction?.Color ?? Request.Venue.MapFaction.Color;

    private uint GetVictimClothingColor2() =>
        Request.Victim.MapFaction?.Color2 ?? Request.Venue.MapFaction.Color2;

    private bool HasUsableVictimVisuals(Agent candidate, string role)
    {
        var visuals = candidate.AgentVisuals;
        if (candidate.IsActive() && visuals is not null && visuals.IsValid())
        {
            return true;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} {role} agent {candidate.Index} was not active with valid native visuals.");
        return false;
    }

    private void RetireProvisionalVictimAgent(Agent agent, string reason)
    {
        try
        {
            if (agent.IsActive())
            {
                agent.FadeOut(hideInstantly: true, hideMount: true);
            }

            _spawnedAgents.Remove(agent);
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not retire provisional victim agent {agent.Index} ({reason}).",
                exception);
        }
    }

    private bool TryPlacePlayerAtScaffoldEntry()
    {
        if (_placement is null || _playerAgent is null || !_playerAgent.IsActive())
        {
            return false;
        }

        if (UsesConfirmedCustomSite)
        {
            RexLog.Info(
                $"Session {Request.SessionId} retained the player's live builder position; no custom-site escort teleport was applied.");
            return true;
        }

        if (UsesGroundExecutionSite())
        {
            return TryPlacePlayerAtGroundExecutionEntry();
        }

        var front = MathF.Max(GallowsMinimumInteractionForward, _frontInteractionOffset);
        var candidates = new[]
        {
            (Right: 0f, Forward: front + GallowsEntryStandOff, Source: "stairs-front"),
            (Right: -1.10f, Forward: front + GallowsEntryStandOff, Source: "stairs-left"),
            (Right: 1.10f, Forward: front + GallowsEntryStandOff, Source: "stairs-right"),
            (Right: 0f, Forward: front + GallowsEntryStandOff + 1.10f, Source: "front fallback")
        };
        foreach (var candidate in candidates)
        {
            if (!TrySnapNavigable(
                    _placement.Offset(candidate.Right, candidate.Forward),
                    out var entryPosition,
                    allowedAgentA: _playerAgent,
                    requireDirectLineFromPlacement: false) ||
                !HasDynamicAgentClearance(_playerAgent, entryPosition))
            {
                continue;
            }

            PoseAgentAt(_playerAgent, entryPosition, -_placement.Forward);
            RexLog.Info(
                $"Session {Request.SessionId} escorted player {_playerAgent.Index} to gallows entry " +
                $"'{candidate.Source}' at ({candidate.Right:0.00}, {candidate.Forward:0.00}).");
            return true;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} found no collision-safe navigable player entry beside the gallows stairs.");
        if (TryPlacePlayerAtCompatibilityEntry(front + GallowsEntryStandOff, "gallows"))
        {
            return true;
        }

        return false;
    }

    private bool TryPlacePlayerAtGroundExecutionEntry()
    {
        if (_placement is null || _playerAgent is null || !_playerAgent.IsActive())
        {
            return false;
        }

        var candidates = UsesCrossbowExecution()
            ? new[]
            {
                (Right: 0f, Forward: 0f, Source: "crossbow firing origin"),
                (Right: -0.80f, Forward: 0.15f, Source: "crossbow firing-left"),
                (Right: 0.80f, Forward: 0.15f, Source: "crossbow firing-right"),
                (Right: 0f, Forward: 0.90f, Source: "crossbow firing fallback")
            }
            : UsesStoningExecution()
                ? new[]
                {
                    (Right: 0f, Forward: StoningPlayerEntryForwardOffset, Source: "stoning player rear"),
                    (Right: -1.10f, Forward: StoningPlayerEntryForwardOffset + 0.10f, Source: "stoning player rear-left"),
                    (Right: 1.10f, Forward: StoningPlayerEntryForwardOffset + 0.10f, Source: "stoning player rear-right"),
                    (Right: 0f, Forward: StoningPlayerEntryForwardOffset + 0.85f, Source: "stoning player fallback")
                }
                : new[]
            {
                (Right: 0f, Forward: 3.25f, Source: "pyre-front"),
                (Right: -1.10f, Forward: 3.35f, Source: "pyre-front-left"),
                (Right: 1.10f, Forward: 3.35f, Source: "pyre-front-right"),
                (Right: 0f, Forward: 4.20f, Source: "pyre-front fallback")
            };
        foreach (var candidate in candidates)
        {
            if (!TrySnapNavigable(
                    _placement.Offset(candidate.Right, candidate.Forward),
                    out var entryPosition,
                    allowedAgentA: _playerAgent,
                    requireDirectLineFromPlacement: false) ||
                !HasDynamicAgentClearance(_playerAgent, entryPosition))
            {
                continue;
            }

            PoseAgentAt(
                _playerAgent,
                entryPosition,
                UsesCrossbowExecution()
                    ? GetDirectionToVictim(entryPosition)
                    : -_placement.Forward);
            RexLog.Info(
                $"Session {Request.SessionId} escorted player {_playerAgent.Index} to ground execution entry " +
                $"'{candidate.Source}' at ({candidate.Right:0.00}, {candidate.Forward:0.00}).");
            return true;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} found no collision-safe navigable player entry for the ground execution site.");
        var compatibilityForward = UsesCrossbowExecution()
            ? 0f
            : UsesStoningExecution()
                ? StoningPlayerEntryForwardOffset
                : 3.25f;
        if (TryPlacePlayerAtCompatibilityEntry(compatibilityForward, "ground execution site"))
        {
            return true;
        }

        return false;
    }

    private bool TryPlacePlayerAtCompatibilityEntry(float forwardOffset, string purpose)
    {
        if (_placement is null || _playerAgent is null || !_playerAgent.IsActive())
        {
            return false;
        }

        var raw = _placement.Offset(0f, forwardOffset);
        var probe = raw + new Vec3(0f, 0f, 4f);
        var height = Mission.Scene.GetGroundHeightAtPosition(probe);
        if (float.IsNaN(height) || float.IsInfinity(height))
        {
            return false;
        }

        var entryPosition = new Vec3(raw.x, raw.y, height + 0.04f);
        if (!Mission.IsPositionInsideBoundaries(entryPosition.AsVec2))
        {
            return false;
        }

        PoseAgentAt(_playerAgent, entryPosition, -_placement.Forward);
        RexLog.Warning(
            $"Session {Request.SessionId} placed the player at the {purpose} compatibility entry " +
            $"({entryPosition.x:0.00}, {entryPosition.y:0.00}, {entryPosition.z:0.00}) without a local " +
            "navigation/clearance veto; the universal city fallback remains active.");
        return true;
    }

    private void SpawnGuards(Vec3 center, Vec2 facing)
    {
        if (_placement is null)
        {
            return;
        }

        var guardCount =
            UsesCrossbowExecution() ? 2 :
            UsesImpalementExecution() ? 0 :
            4;
        // Two guards form a line immediately ahead of the crowd while two
        // stand on the verified deck behind the condemned. Each role has
        // narrow fallbacks for altered town navmesh or live ambient agents.
        var offsetCandidates = new (float Right, float Forward)[][]
        {
            new (float Right, float Forward)[]
            {
                (-4.60f, 8.85f), (-3.20f, 8.85f), (-1.25f, 8.85f), (-0.75f, 8.25f)
            },
            new (float Right, float Forward)[]
            {
                (4.60f, 8.85f), (3.20f, 8.85f), (1.25f, 8.85f), (0.75f, 9.05f)
            },
            new (float Right, float Forward)[]
            {
                (-3.10f, -1.55f), (-3.45f, -1.20f), (-2.75f, -1.85f)
            },
            new (float Right, float Forward)[]
            {
                (3.10f, -1.55f), (3.45f, -1.20f), (2.75f, -1.85f)
            }
        };
        if (UsesCrossbowExecution())
        {
            // Crossbow execution keeps only the two public-side guards. The
            // prisoner-side pair is intentionally omitted so the crucifix and
            // hand IK remain visually unobstructed.
            offsetCandidates = new (float Right, float Forward)[][]
            {
                new (float Right, float Forward)[]
                {
                    (-4.60f, 8.85f), (-3.20f, 8.85f), (-2.60f, 7.60f)
                },
                new (float Right, float Forward)[]
                {
                    (4.60f, 8.85f), (3.20f, 8.85f), (2.60f, 7.60f)
                }
            };
        }
        else if (RequiresExecutionTorch())
        {
            // Pull the two pyre-side guards 1.25 metres farther out so the
            // enlarged flames do not visually engulf them.
            offsetCandidates[2] = new[]
            {
                (Right: -4.35f, Forward: -1.55f),
                (Right: -4.70f, Forward: -1.20f),
                (Right: -4.00f, Forward: -1.85f)
            };
            offsetCandidates[3] = new[]
            {
                (Right: 4.35f, Forward: -1.55f),
                (Right: 4.70f, Forward: -1.20f),
                (Right: 4.00f, Forward: -1.85f)
            };
        }
        for (var index = 0; index < guardCount; index++)
        {
            var guardCharacter = SelectMeleeGuardTroopForCurrentSite(index);
            if (guardCharacter is null)
            {
                throw new InvalidOperationException(
                    $"The venue culture has no eligible tier-3 melee guard troop for slot {index}.");
            }

            // Guard actor markers are legacy automatic-layout data, not a player
            // placement control. Ignore them and always resolve a safe ceremony
            // position from the current scene/navmesh so guards cannot spawn on the
            // custom root or action point.
            var offset = (Right: 0f, Forward: 0f);
            var position = Vec3.Invalid;
            var foundSafePosition = false;
            foreach (var candidateOffset in offsetCandidates[index])
            {
                var rawPosition = _placement.Offset(
                    candidateOffset.Right,
                    candidateOffset.Forward,
                    0f);
                var accepted = index < 2
                    ? TrySnapNavigable(
                        rawPosition,
                        out position,
                        requireDirectLineFromPlacement: false)
                    : TryResolveCeremonySurfaceMark(
                        rawPosition,
                        out position,
                        allowedAgentA: _victimAgent,
                        allowedAgentB: _executionerAgent,
                        allowedAgentC: _playerAgent);
                if (!accepted)
                {
                    continue;
                }

                foundSafePosition = true;
                offset = candidateOffset;
                break;
            }

            if (!foundSafePosition)
            {
                RexLog.Warning(
                    $"Guard slot {index} has no collision-safe ceremony position and was skipped.");
                continue;
            }

            var lookTarget = UsesCrossbowExecution()
                ? _victimPosition.AsVec2
                : _placement.Origin.AsVec2;
            var lookDirection = lookTarget - position.AsVec2;
            if (!lookDirection.IsNonZero())
            {
                lookDirection = index < 2 ? -facing : facing;
            }

            var hasCulturePolearm = CultureTroopSelector.TryCreateGuardCeremonyEquipment(
                Request,
                guardCharacter,
                out var guardEquipment,
                out var polearmId);
            var guard = SpawnCharacter(
                guardCharacter,
                position,
                lookDirection.Normalized(),
                civilianEquipment: false,
                noWeapons: false,
                invulnerable: true,
                equipmentOverride: guardEquipment,
                teamOverride: _executionTeam,
                joinPlayerFormation: true);
            if (!EnsureGeneratedAllyOnPlayerTeam(guard, $"ceremony guard {index + 1}/{guardCount}"))
            {
                throw new InvalidOperationException(
                    $"Ceremony guard slot {index} did not retain the player's mission team.");
            }

            if (index < 2)
            {
                _frontGuardAgents.Add(guard);
            }

            var standbyAction = "engine weapon stance";
            var actionSetCode = guard.ActionSet.GetName();
            var meleeSlot = EquipmentIndex.None;
            var usesLordHallPose = false;
            var lordHallPosePrepared =
                hasCulturePolearm &&
                TryApplyGuardActionSet(guard, out actionSetCode) &&
                TryWieldGuardPolearm(guard, out meleeSlot);
            if (lordHallPosePrepared)
            {
                TrySetGuardStandbyAction(
                    guard,
                    GuardSpearIdleAction,
                    out _);
                standbyAction = GuardSpearIdleAction;
                var retainedOnSpawnFrame = IsGuardStandbyStateRetained(
                    guard,
                    standbyAction,
                    meleeSlot);
                usesLordHallPose = true;
                if (!retainedOnSpawnFrame)
                {
                    RexLog.Warning(
                        $"Guard slot {index} did not retain the lord-hall spear pose during its spawn frame; " +
                        "the mission tick will keep reapplying it.");
                }
            }
            else
            {
                if (hasCulturePolearm)
                {
                    RexLog.Warning(
                        $"Guard slot {index} could not retain the lord-hall spear pose with '{polearmId}'; " +
                        "falling back to a weapon-matched idle.");
                }
                else
                {
                    RexLog.Warning(
                        $"Guard slot {index} found no non-consumable polearm in venue culture " +
                        $"'{Request.Venue.Culture.StringId}'; falling back to a weapon-matched idle.");
                }

                if (!TryWieldGuardMeleeWeapon(guard, out meleeSlot))
                {
                    throw new InvalidOperationException(
                        $"Guard slot {index} could not switch to a melee weapon.");
                }

                if (!TryPlayGuardStandbyAction(guard, meleeSlot, out standbyAction))
                {
                    standbyAction = "engine weapon stance";
                    RexLog.Warning(
                        $"Guard slot {index} will retain its engine weapon stance because its ActionSet " +
                        "does not expose a compatible vanilla idle.");
                }
            }

            if (!string.Equals(
                    standbyAction,
                    "engine weapon stance",
                    StringComparison.Ordinal) &&
                !IsGuardStandbyStateRetained(guard, standbyAction, meleeSlot))
            {
                RexLog.Warning(
                    $"Guard slot {index} did not retain both '{standbyAction}' and melee slot " +
                    $"{meleeSlot} during its spawn frame; the mission tick will repair them.");
            }

            if (!string.Equals(
                    standbyAction,
                    "engine weapon stance",
                    StringComparison.Ordinal) &&
                meleeSlot != EquipmentIndex.None)
            {
                _guardStandbyBindings.Add(new GuardStandbyBinding(
                    guard,
                    standbyAction,
                    meleeSlot));
            }

            var formationRole = UsesCrossbowExecution()
                ? index < 2 ? "crossbow-crowd-front" : "crossbow-prisoner-side"
                : index < 2 ? "crowd-front" : "prisoner-rear";
            RexLog.Info(
                $"Session {Request.SessionId} guard slot {index}: {guardCharacter.StringId} " +
                $"(T{guardCharacter.Tier}, {formationRole}, actionSet={guard.ActionSet.GetName()}, " +
                $"requestedActionSet={actionSetCode}, pose={(usesLordHallPose ? "lord-hall-spear" : "fallback")}, " +
                $"weapon={(string.IsNullOrWhiteSpace(polearmId) ? "troop-melee" : polearmId)}, " +
                $"idle={standbyAction}, meleeSlot={meleeSlot}).");
        }
    }

    private void SpawnCrossbowVolleyAgents()
    {
        if (!UsesCrossbowExecution() || _placement is null || _executionTeam is null)
        {
            return;
        }

        for (var slot = 0; slot < CrossbowVolleyRightOffsetCandidates.Length; slot++)
        {
            var character = SelectRangedGuardTroopForCurrentSite(slot) ??
                            throw new InvalidOperationException(
                                $"The venue culture has no eligible regular crossbow-volley troop for slot {slot}.");
            var position = Vec3.Invalid;
            var foundSafePosition = false;
            var selectedRight = 0f;
            // Ranged guard actor markers from legacy presets can point at the
            // custom deployment root. Resolve every shooter from the current
            // scene candidates so no guard is spawned on the player's setup
            // point or an obsolete marker.
            foreach (var rightOffset in CrossbowVolleyRightOffsetCandidates[slot])
            {
                var raw = _placement.Offset(rightOffset, 0f);
                if (!TryResolveCeremonySurfaceMark(
                        raw,
                        out position,
                        allowedAgentA: _victimAgent,
                        allowedAgentB: _executionerAgent,
                        allowedAgentC: _playerAgent))
                {
                    continue;
                }

                foundSafePosition = true;
                selectedRight = rightOffset;
                break;
            }

            if (!foundSafePosition)
            {
                throw new InvalidOperationException(
                    $"No collision-safe crossbow-volley firing mark could be resolved for slot {slot}.");
            }

            var direction = GetDirectionToVictim(position);
            var shooter = SpawnCharacter(
                character,
                position,
                direction,
                civilianEquipment: false,
                noWeapons: true,
                invulnerable: true,
                teamOverride: _executionTeam,
                joinPlayerFormation: true);
            if (!EnsureGeneratedAllyOnPlayerTeam(
                    shooter,
                    $"crossbow volley shooter {slot + 1}/{CrossbowVolleyRightOffsetCandidates.Length}") ||
                !TryEquipAdditionalCeremonyWeapon(
                    shooter,
                    $"crossbow volley shooter {slot + 1}"))
            {
                throw new InvalidOperationException(
                    $"Crossbow-volley shooter slot {slot} could not retain the player's team and loaded crossbow.");
            }

            shooter.DisableScriptedMovement();
            shooter.Controller = AgentControllerType.None;
            PoseAgentAt(shooter, position, direction);
            var idleActionName = _sceneAct?.ExecutionerIdleAction ?? "act_idle_crossbow_1";
            if (_crossbowVolleyIdleAction == ActionIndexCache.act_none &&
                !TryResolveAndPrefetchAction(
                    shooter,
                    idleActionName,
                    "shared executioner/crossbow-volley idle",
                    out _crossbowVolleyIdleAction))
            {
                RexLog.Warning(
                    $"Crossbow-volley shooter {slot + 1} could not resolve the shared executioner idle '{idleActionName}'.");
            }
            else
            {
                TrySetPreparedAction(
                    shooter,
                    _crossbowVolleyIdleAction,
                    idleActionName,
                    $"crossbow volley shooter {slot + 1} entry idle",
                    forceFullBody: true);
            }
            _crossbowVolleyAgents.Add(shooter);
            _crossbowVolleyPositions.Add(position);
            _crossbowVolleyReadyStarted.Add(false);
            _crossbowVolleyHoldStarted.Add(false);
            _crossbowVolleyReleased.Add(false);
            RexLog.Info(
                $"Session {Request.SessionId} spawned culture crossbowman {slot + 1}/" +
                $"{CrossbowVolleyRightOffsetCandidates.Length}: {character.StringId} (T{character.Tier}) " +
                $"at firing-line Right={selectedRight:0.00}m; NPC volley=true, player volley=false.");
        }
    }

    private void MaintainCrossbowVolleyStandby()
    {
        if (!UsesCrossbowExecution() || State != ExecutionSessionState.WaitingForPlayer)
        {
            return;
        }

        for (var index = 0;
             index < _crossbowVolleyAgents.Count && index < _crossbowVolleyPositions.Count;
             index++)
        {
            var shooter = _crossbowVolleyAgents[index];
            if (shooter is null || !shooter.IsActive())
            {
                continue;
            }

            try
            {
                var position = _crossbowVolleyPositions[index];
                var direction = GetDirectionToVictim(position);
                if (shooter.Position.Distance(position) > 0.03f)
                {
                    PoseAgentAt(shooter, position, direction);
                }
                else
                {
                    shooter.LookDirection = new Vec3(direction.x, direction.y, 0f);
                }

                if (!TryEnsureAdditionalCeremonyWeaponWielded(
                        shooter,
                        $"waiting crossbow volley shooter {index + 1}"))
                {
                    continue;
                }

                var idleActionName = _sceneAct?.ExecutionerIdleAction ?? "act_idle_crossbow_1";
                if (_crossbowVolleyIdleAction == ActionIndexCache.act_none &&
                    !TryResolveAndPrefetchAction(
                        shooter,
                        idleActionName,
                        "shared executioner/crossbow-volley standby idle",
                        out _crossbowVolleyIdleAction))
                {
                    continue;
                }
                if (shooter.GetCurrentAction(0) == _crossbowVolleyIdleAction &&
                    shooter.GetActionChannelWeight(0) > 0.01f)
                {
                    continue;
                }

                TrySetPreparedAction(
                    shooter,
                    _crossbowVolleyIdleAction,
                    idleActionName,
                    $"crossbow volley shooter {index + 1} standby",
                    forceFullBody: true);
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"Crossbow-volley shooter {index + 1} standby maintenance failed; later ticks continue.",
                    exception);
            }
        }
    }

    private void DriveCrossbowVolleyAction(string actionName, string purpose)
    {
        if (!UsesCrossbowExecution() || _actor != ExecutionActor.Executioner)
        {
            return;
        }

        if (string.Equals(actionName, "act_ready_crossbow", StringComparison.OrdinalIgnoreCase))
        {
            _crossbowVolleyAimElapsed = 0f;
            _crossbowVolleyAimActive = true;
            _crossbowVolleyHoldRequested = false;
            _crossbowVolleyReleaseActive = false;
            for (var index = 0; index < _crossbowVolleyAgents.Count; index++)
            {
                _crossbowVolleyReadyStarted[index] = false;
                _crossbowVolleyHoldStarted[index] = false;
                _crossbowVolleyReleased[index] = false;
            }

            if (_executionerAgent is not null)
            {
                PrepareNativeRangedCombatTarget(
                    _executionerAgent,
                    holdFire: true,
                    "primary crossbow aim");
            }

            RexLog.Info(
                $"Session {Request.SessionId} armed the crossbow firing-line stagger: primary=0.00s, " +
                $"support interval={CrossbowVolleyAimStartStaggerSeconds:0.00}s.");
            return;
        }

        if (string.Equals(actionName, "act_ready_continue_crossbow", StringComparison.OrdinalIgnoreCase))
        {
            _crossbowVolleyHoldRequested = true;
            return;
        }

        if (string.Equals(actionName, "act_release_crossbow", StringComparison.OrdinalIgnoreCase))
        {
            _crossbowVolleyAimActive = false;
            _crossbowVolleyReleaseElapsed = 0f;
            _crossbowVolleyReleaseActive = true;
            return;
        }

        for (var index = 0;
             index < _crossbowVolleyAgents.Count && index < _crossbowVolleyPositions.Count;
             index++)
        {
            var shooter = _crossbowVolleyAgents[index];
            if (shooter is null || !shooter.IsActive())
            {
                continue;
            }

            try
            {
                var position = _crossbowVolleyPositions[index];
                var direction = GetDirectionToVictim(position);
                PoseAgentAt(shooter, position, direction);
                if (!TryEnsureAdditionalCeremonyWeaponWielded(
                        shooter,
                        $"crossbow volley shooter {index + 1} before {purpose}") ||
                    !TryPlayRequiredAction(
                        shooter,
                        actionName,
                        $"crossbow volley shooter {index + 1} {purpose}",
                        out _,
                        forceFullBody: true))
                {
                    RexLog.Warning(
                        $"Crossbow-volley shooter {index + 1} could not visibly bind '{actionName}' " +
                        $"for {purpose}; its real bolt remains scheduled on the common release frame.");
                }
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"Crossbow-volley shooter {index + 1} failed while entering {purpose}; " +
                    "the remaining shooters continue.",
                    exception);
            }
        }
    }

    private void TickCrossbowVolleyStagger(float dt)
    {
        if (!UsesCrossbowExecution() || _actor != ExecutionActor.Executioner ||
            State != ExecutionSessionState.Execution)
        {
            return;
        }

        var safeDt = MathF.Max(0f, dt);
        if (_crossbowVolleyAimActive)
        {
            _crossbowVolleyAimElapsed += safeDt;
            for (var visualOrder = 0;
                 visualOrder < _crossbowVolleyAgents.Count &&
                 visualOrder < _crossbowVolleyPositions.Count;
                 visualOrder++)
            {
                var index = GetCrossbowVolleyAgentIndexForVisualOrder(visualOrder);
                var startDelay = (visualOrder + 1) * CrossbowVolleyAimStartStaggerSeconds;
                if (!_crossbowVolleyReadyStarted[index] &&
                    _crossbowVolleyAimElapsed >= startDelay)
                {
                    _crossbowVolleyReadyStarted[index] = true;
                    StartCrossbowVolleyAgentPhase(index, "act_ready_crossbow", "staggered native ready", holdFire: true);
                }

                if (_crossbowVolleyHoldRequested &&
                    !_crossbowVolleyHoldStarted[index] &&
                    _crossbowVolleyAimElapsed >= CrossbowVolleyReadyToHoldSeconds + startDelay)
                {
                    _crossbowVolleyHoldStarted[index] = true;
                    StartCrossbowVolleyAgentPhase(index, "act_ready_continue_crossbow", "staggered native aimed hold", holdFire: true);
                }
            }
        }

        if (!_crossbowVolleyReleaseActive)
        {
            return;
        }

        _crossbowVolleyReleaseElapsed += safeDt;
        for (var visualOrder = 0;
             visualOrder < _crossbowVolleyAgents.Count &&
             visualOrder < _crossbowVolleyPositions.Count;
             visualOrder++)
        {
            var index = GetCrossbowVolleyAgentIndexForVisualOrder(visualOrder);
            if (_crossbowVolleyReleased[index] ||
                _crossbowVolleyReleaseElapsed < (visualOrder + 1) * CrossbowVolleyAimStartStaggerSeconds)
            {
                continue;
            }

            _crossbowVolleyReleased[index] = true;
            var shooter = _crossbowVolleyAgents[index];
            StartCrossbowVolleyAgentPhase(
                index,
                "act_release_crossbow",
                $"staggered native release left-to-right order {visualOrder + 1}",
                holdFire: false);
            if (shooter is not null && shooter.IsActive())
            {
                TryLaunchCrossbowBolt(shooter, $"staggered support release left-to-right order {visualOrder + 1}");
            }
        }

        if (_crossbowVolleyReleased.Count == 0 || _crossbowVolleyReleased.All(released => released))
        {
            _crossbowVolleyReleaseActive = false;
        }
    }

    private int GetCrossbowVolleyAgentIndexForVisualOrder(int visualOrder)
    {
        if (visualOrder >= 0 && visualOrder < CrossbowVolleyLeftToRightOrder.Length)
        {
            var mapped = CrossbowVolleyLeftToRightOrder[visualOrder];
            if (mapped >= 0 && mapped < _crossbowVolleyAgents.Count &&
                mapped < _crossbowVolleyPositions.Count)
            {
                return mapped;
            }
        }

        return Math.Max(0, Math.Min(
            _crossbowVolleyAgents.Count - 1 - visualOrder,
            Math.Min(_crossbowVolleyAgents.Count, _crossbowVolleyPositions.Count) - 1));
    }

    private void StartCrossbowVolleyAgentPhase(
        int index,
        string actionName,
        string purpose,
        bool holdFire)
    {
        if (index < 0 || index >= _crossbowVolleyAgents.Count ||
            index >= _crossbowVolleyPositions.Count)
        {
            return;
        }

        var shooter = _crossbowVolleyAgents[index];
        if (shooter is null || !shooter.IsActive())
        {
            return;
        }

        try
        {
            var position = _crossbowVolleyPositions[index];
            var direction = GetDirectionToVictim(position);
            PoseAgentAt(shooter, position, direction);
            PrepareNativeRangedCombatTarget(shooter, holdFire, $"crossbow support {index + 1} {purpose}");
            if (!TryEnsureAdditionalCeremonyWeaponWielded(shooter, $"crossbow support {index + 1} {purpose}") ||
                !TryPlayRequiredAction(
                    shooter,
                    actionName,
                    $"crossbow support {index + 1} {purpose}",
                    out _,
                    forceFullBody: true))
            {
                RexLog.Warning(
                    $"Crossbow support {index + 1} could not bind '{actionName}' for {purpose}; " +
                    "its real native bolt schedule continues.");
            }
        }
        catch (Exception exception)
        {
            RexLog.Error($"Crossbow support {index + 1} failed during {purpose}.", exception);
        }
    }

    private void PrepareNativeRangedCombatTarget(Agent shooter, bool holdFire, string purpose)
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive() || shooter is null || !shooter.IsActive())
        {
            return;
        }

        try
        {
            shooter.SetTargetAgent(victim);
            shooter.SetWatchState(Agent.WatchState.Alarmed);
            shooter.SetFiringOrder(
                holdFire
                    ? FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire
                    : FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill);
            RexLog.Info(
                $"Session {Request.SessionId} assigned native combat target Agent {victim.Index} " +
                $"to ranged Agent {shooter.Index} for {purpose}; holdFire={holdFire}.");
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Native ranged target preparation failed for Agent {shooter.Index} during {purpose}; " +
                $"the real Mission missile path remains active. {exception.Message}");
        }
    }

    private bool LaunchCrossbowVolley(Agent primaryShooter, string trigger)
    {
        if (!UsesCrossbowExecution())
        {
            return false;
        }

        PrepareNativeRangedCombatTarget(primaryShooter, holdFire: false, "primary crossbow release");
        var launched = TryLaunchCrossbowBolt(primaryShooter, trigger) ? 1 : 0;

        RexLog.Info(
            $"Session {Request.SessionId} released the primary real native crossbow bolt after {trigger}; " +
            $"launched={launched}, actor={_actor}, supporting bolts=" +
            $"{(_actor == ExecutionActor.Executioner ? "staggered at 0.10s intervals" : "disabled for player execution")}.");
        return launched > 0;
    }

    private bool TryLaunchCrossbowBolt(Agent shooter, string trigger)
    {
        try
        {
            var victim = _victimAgent;
            var boltItem = Game.Current?.ObjectManager.GetObject<ItemObject>(CrossbowBoltItemId);
            if (victim is null || !victim.IsActive() || boltItem is null)
            {
                RexLog.Error(
                    $"Could not launch a real crossbow bolt from Agent {shooter.Index} after {trigger}: " +
                    $"victimActive={victim?.IsActive() == true}, boltAvailable={boltItem is not null}.");
                return false;
            }

            var start = shooter.GetEyeGlobalPosition() - new Vec3(0f, 0f, 0.20f);
            var target = victim.GetChestGlobalPosition();
            var horizontalDistance = start.AsVec2.Distance(target.AsVec2);
            var flightSeconds = horizontalDistance / CrossbowBoltSpeed;
            target.z += 4.905f * flightSeconds * flightSeconds;
            var direction = target - start;
            if (!direction.IsNonZero)
            {
                return false;
            }

            direction.Normalize();
            var orientation = Mat3.CreateMat3WithForward(in direction);
            var missileWeapon = new MissionWeapon(
                boltItem,
                null,
                shooter.Origin?.Banner,
                1);
            Mission.AddCustomMissile(
                shooter,
                missileWeapon,
                start,
                direction,
                orientation,
                CrossbowBoltSpeed,
                CrossbowBoltSpeed,
                addRigidBody: false,
                missionObjectToIgnore: null!);
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"The real crossbow bolt from Agent {shooter.Index} could not be created; " +
                "the remaining volley continues.",
                exception);
            return false;
        }
    }

    /// <summary>
    /// Forces the prisoner onto the base human warrior ActionSet. Hero victims
    /// spawn with a civilian/culture ActionSet that lacks the prisoner idle,
    /// conversation-reaction and death clips used by every execution method
    /// (native death clips resolve only from as_human_warrior). Without this
    /// switch every victim action fails with "no clip in the agent's action
    /// set" and the prisoner stands frozen through the whole ceremony.
    /// </summary>
    private void TryApplyVictimActionSet(Agent victim)
    {
        const string victimActionSetCode = "as_human_warrior";
        try
        {
            if (string.Equals(
                    victim.ActionSet.GetName(),
                    victimActionSetCode,
                    StringComparison.Ordinal))
            {
                return;
            }

            var actionSet = MBGlobals.GetActionSet(victimActionSetCode);
            var missing = new List<string>();
            foreach (var required in new[]
                     {
                         _sceneAct?.VictimIdleAction,
                         _sceneAct?.DeathAction,
                         "act_conversation_aggressive_loop",
                         "act_conversation_aggressive_very_negative",
                     })
            {
                if (required is null || required.Length == 0)
                {
                    continue;
                }

                var action = ActionIndexCache.Create(required);
                if (action == ActionIndexCache.act_none ||
                    !MBActionSet.CheckActionAnimationClipExists(actionSet, in action))
                {
                    missing.Add(required);
                }
            }

            if (missing.Count > 0)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} target victim ActionSet '{victimActionSetCode}' " +
                    $"is missing clips: {string.Join(", ", missing)}; the prisoner keeps its spawned set.");
                return;
            }

            var animationSystemData = victim.Monster.FillAnimationSystemData(
                actionSet,
                victim.Character.GetStepSize(),
                false);
            victim.SetActionSet(ref animationSystemData);
            var retained = string.Equals(
                victim.ActionSet.GetName(),
                victimActionSetCode,
                StringComparison.Ordinal);
            RexLog.Info(
                $"Session {Request.SessionId} switched prisoner Agent {victim.Index} to ActionSet " +
                $"'{victimActionSetCode}' (retained={retained}) so ceremony/death clips can bind.");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not switch the prisoner to the warrior ActionSet; " +
                "victim actions may fail to bind.",
                exception);
        }
    }

    private static bool TryApplyGuardActionSet(Agent guard, out string actionSetCode)
    {
        actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(
            guard.Monster,
            guard.IsFemale,
            ActionSetCode.GuardSuffix);
        try
        {
            var action = ActionIndexCache.Create(GuardSpearIdleAction);
            var actionSet = MBGlobals.GetActionSet(actionSetCode);
            if (action == ActionIndexCache.act_none ||
                !MBActionSet.CheckActionAnimationClipExists(actionSet, in action))
            {
                RexLog.Warning(
                    $"Guard {guard.Index} target ActionSet '{actionSetCode}' has no " +
                    $"'{GuardSpearIdleAction}' clip.");
                return false;
            }

            var animationSystemData = guard.Monster.FillAnimationSystemData(
                actionSet,
                guard.Character.GetStepSize(),
                false);
            guard.SetActionSet(ref animationSystemData);
            if (!string.Equals(
                    guard.ActionSet.GetName(),
                    actionSetCode,
                    StringComparison.Ordinal))
            {
                RexLog.Warning(
                    $"Guard {guard.Index} did not retain requested ActionSet '{actionSetCode}'.");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Guard {guard.Index} could not switch to ActionSet '{actionSetCode}'.",
                exception);
            return false;
        }
    }

    private static bool TryWieldGuardPolearm(Agent guard, out EquipmentIndex polearmSlot)
    {
        polearmSlot = EquipmentIndex.None;
        guard.TryToSheathWeaponInHand(
            Agent.HandIndex.MainHand,
            Agent.WeaponWieldActionType.Instant);
        guard.TryToSheathWeaponInHand(
            Agent.HandIndex.OffHand,
            Agent.WeaponWieldActionType.Instant);

        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var candidate = (EquipmentIndex)slotIndex;
            var weapon = guard.Equipment[candidate];
            var usage = weapon.IsEmpty ? null : weapon.CurrentUsageItem;
            if (usage is null ||
                !usage.IsMeleeWeapon ||
                usage.IsConsumable ||
                usage.WeaponClass is not (
                    WeaponClass.OneHandedPolearm or
                    WeaponClass.TwoHandedPolearm or
                    WeaponClass.LowGripPolearm))
            {
                continue;
            }

            guard.TryToWieldWeaponInSlot(
                candidate,
                Agent.WeaponWieldActionType.Instant,
                isWieldedOnSpawn: false);
            if (guard.GetPrimaryWieldedItemIndex() == candidate)
            {
                polearmSlot = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryWieldGuardMeleeWeapon(Agent guard, out EquipmentIndex meleeSlot)
    {
        meleeSlot = EquipmentIndex.None;
        guard.TryToSheathWeaponInHand(
            Agent.HandIndex.MainHand,
            Agent.WeaponWieldActionType.Instant);
        guard.TryToSheathWeaponInHand(
            Agent.HandIndex.OffHand,
            Agent.WeaponWieldActionType.Instant);

        for (var slotIndex = (int)EquipmentIndex.WeaponItemBeginSlot;
             slotIndex < (int)EquipmentIndex.NumAllWeaponSlots;
             slotIndex++)
        {
            var candidate = (EquipmentIndex)slotIndex;
            var weapon = guard.Equipment[candidate];
            var usage = weapon.IsEmpty ? null : weapon.CurrentUsageItem;
            if (usage is null || !usage.IsMeleeWeapon || usage.IsConsumable)
            {
                continue;
            }

            guard.TryToWieldWeaponInSlot(
                candidate,
                Agent.WeaponWieldActionType.Instant,
                isWieldedOnSpawn: false);
            if (guard.GetPrimaryWieldedItemIndex() == candidate)
            {
                meleeSlot = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryPlayGuardStandbyAction(
        Agent guard,
        EquipmentIndex meleeSlot,
        out string selectedActionName)
    {
        selectedActionName = string.Empty;
        var usage = guard.Equipment[meleeSlot].CurrentUsageItem;
        var fallbackActions = usage is null
            ? GuardOneHandedIdleActionNames
            : GetWeaponMatchedGuardIdleActions(usage.WeaponClass);
        foreach (var fallbackAction in fallbackActions)
        {
            if (TrySetGuardStandbyAction(guard, fallbackAction, out selectedActionName))
            {
                return true;
            }
        }

        RexLog.Warning(
            $"Guard {guard.Index} action set '{guard.ActionSet.GetName()}' has no " +
            "weapon-matched vanilla idle.");
        return false;
    }

    private bool TrySetGuardStandbyAction(
        Agent guard,
        string actionName,
        out string selectedActionName)
    {
        selectedActionName = string.Empty;
        if (!TryResolveAndPrefetchAction(
                guard,
                actionName,
                "guard standby",
                out var action) ||
            !TrySetPreparedAction(
                guard,
                action,
                actionName,
                "guard standby",
                forceFullBody: true,
                randomStart: false))
        {
            return false;
        }

        try
        {
            if (guard.GetCurrentAction(0) != action)
            {
                RexLog.Warning(
                    $"Guard {guard.Index} accepted '{actionName}' but channel 0 did not retain it.");
                return false;
            }

            selectedActionName = actionName;
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Guard {guard.Index} idle '{actionName}' could not be read back.",
                exception);
            return false;
        }
    }

    private static string[] GetWeaponMatchedGuardIdleActions(WeaponClass weaponClass) =>
        weaponClass switch
        {
            WeaponClass.OneHandedPolearm or
            WeaponClass.TwoHandedPolearm or
            WeaponClass.LowGripPolearm => GuardPolearmIdleActionNames,
            WeaponClass.TwoHandedSword or
            WeaponClass.TwoHandedAxe or
            WeaponClass.TwoHandedMace => GuardTwoHandedIdleActionNames,
            _ => GuardOneHandedIdleActionNames
        };

    private static bool IsGuardStandbyStateRetained(
        Agent guard,
        string actionName,
        EquipmentIndex meleeSlot)
    {
        try
        {
            var action = ActionIndexCache.Create(actionName);
            return action != ActionIndexCache.act_none &&
                   guard.GetCurrentAction(0) == action &&
                   guard.GetPrimaryWieldedItemIndex() == meleeSlot;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Guard {guard.Index} standby action and weapon read-back failed.",
                exception);
            return false;
        }
    }

    private void MaintainGuardStandbyStates(float dt)
    {
        if (_guardStandbyBindings.Count == 0 || _ceremonyAlliesReleasedToPlayerFormation)
        {
            _guardStandbyMaintenanceElapsed = 0f;
            return;
        }

        _guardStandbyMaintenanceElapsed += MathF.Max(0f, dt);
        if (_guardStandbyMaintenanceElapsed < GuardStandbyMaintenanceIntervalSeconds)
        {
            return;
        }

        _guardStandbyMaintenanceElapsed = 0f;
        foreach (var binding in _guardStandbyBindings)
        {
            var guard = binding.Guard;
            if (guard is null || !guard.IsActive() ||
                IsGuardStandbyStateRetained(guard, binding.ActionName, binding.MeleeSlot))
            {
                continue;
            }

            var repaired = TryWieldGuardWeaponSlot(guard, binding.MeleeSlot) &&
                           TrySetGuardStandbyAction(
                               guard,
                               binding.ActionName,
                               out _) &&
                           IsGuardStandbyStateRetained(
                               guard,
                               binding.ActionName,
                               binding.MeleeSlot);
            if (repaired)
            {
                if (binding.RepairFailureLogged)
                {
                    RexLog.Info(
                        $"Guard {guard.Index} recovered standby '{binding.ActionName}' with melee slot {binding.MeleeSlot}.");
                }

                binding.RepairFailureLogged = false;
            }
            else if (!binding.RepairFailureLogged)
            {
                binding.RepairFailureLogged = true;
                RexLog.Warning(
                    $"Guard {guard.Index} has not retained standby '{binding.ActionName}' yet; " +
                    "later mission ticks will keep retrying.");
            }
        }
    }

    private static bool TryWieldGuardWeaponSlot(Agent guard, EquipmentIndex meleeSlot)
    {
        try
        {
            if (meleeSlot == EquipmentIndex.None || guard.Equipment[meleeSlot].IsEmpty)
            {
                return false;
            }

            if (guard.GetPrimaryWieldedItemIndex() != meleeSlot)
            {
                guard.TryToWieldWeaponInSlot(
                    meleeSlot,
                    Agent.WeaponWieldActionType.Instant,
                    isWieldedOnSpawn: false);
            }

            return guard.GetPrimaryWieldedItemIndex() == meleeSlot;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Guard {guard.Index} could not restore melee slot {meleeSlot}.",
                exception);
            return false;
        }
    }

    private void SpawnCrowd()
    {
        if (_placement is null)
        {
            return;
        }

        var activeAgentCount = Mission.Agents.Count(agent => agent is not null && agent.IsActive());
        var activeHumanCount = Mission.Agents.Count(
            agent => agent is not null && agent.IsActive() && agent.IsHuman);
        // Crowd marks are now derived from nearby usable ground only. Old crowd_*
        // preset markers are intentionally ignored so spectators do not pile up
        // around hand-authored points or consume the player's entry lane.
        var candidates = BuildCrowdCandidatePositions(
            CrowdCandidateBudget,
            out var groundFallbackCandidateCount);
        // Candidate positions are already spaced and filtered against occupied or
        // unsafe ground. Fill those positions up to the explicit crowd cap instead
        // of dividing the accepted count again, which left both automatic and
        // custom ceremonies with only five or six spectators.
        var targetCount = Math.Min(
            MaximumExecutionCrowdCount,
            candidates.Count);
        RexLog.Info(
            $"Session {Request.SessionId} crowd spawn plan: activeAgents={activeAgentCount}, " +
            $"activeHumans={activeHumanCount}, target={targetCount}, candidates={candidates.Count}, " +
            $"groundFallbackCandidates={groundFallbackCandidateCount}, " +
            $"compatibilityFallback={_placement.IsCompatibilityFallback}.");
        for (var index = 0; index < candidates.Count && _crowdAgents.Count < targetCount; index++)
        {
            var character = index % 3 == 0
                ? Request.Venue.Culture.Townswoman
                : Request.Venue.Culture.Townsman;
            character ??= Request.Venue.Culture.Townsman ?? Request.Venue.Culture.Townswoman;
            if (character is null)
            {
                break;
            }

            var crowdLookTarget = UsesStoningExecution()
                ? _victimPosition.AsVec2
                : _placement.Origin.AsVec2;
            var lookDirection = crowdLookTarget - candidates[index].AsVec2;
            if (!lookDirection.IsNonZero())
            {
                lookDirection = -_placement.Forward;
            }

            var crowdAgent = SpawnCharacter(
                character,
                candidates[index],
                lookDirection.Normalized(),
                civilianEquipment: true,
                noWeapons: true,
                invulnerable: true,
                teamOverride: _executionTeam);
            _crowdAgents.Add(crowdAgent);

            // SpawnCharacter defaults to AgentControllerType.AI, and an AI-driven
            // agent has its channel 0 re-driven by the mission AI every tick.
            // Left as AI, the varied idle below survives a couple of frames and
            // then every spectator collapses onto the same default civilian pose
            // - the "row of shop mannequins" look. Keep spectators off AI during
            // the ceremony (including stoning); aftermath releases them in small
            // batches to native town walking after the crowd reaction completes.
            crowdAgent.DisableScriptedMovement();
            crowdAgent.Controller = AgentControllerType.None;

            var idlePurpose = UsesStoningExecution()
                ? "varied stoning ring town-resident ambient idle"
                : "crowd town-resident ambient idle";
            if (TryPickAmbientIdleAction(crowdAgent, CrowdAmbientIdleActionNames, out var idleAction))
            {
                TryPlayAction(crowdAgent, idleAction, idlePurpose, randomStart: true);
            }
            else if (!_ambientIdleUnavailableLogged)
            {
                _ambientIdleUnavailableLogged = true;
                RexLog.Warning(
                    $"No ambient idle in the shared list has a clip in agent {crowdAgent.Index}'s action set ({idlePurpose}); " +
                    "spectators will keep their default stance for this session.");
            }
        }

        var player = _playerAgent;
        if (player is not null && player.IsActive() && _crowdAgents.Count > 0)
        {
            var nearestCrowdDistance = _crowdAgents.Min(
                crowd => crowd.Position.AsVec2.Distance(player.Position.AsVec2));
            RexLog.Info(
                $"Session {Request.SessionId} crowd clearance: " +
                $"nearestPlayer={nearestCrowdDistance:0.00}m, " +
                $"required={MinimumCrowdPlayerClearance:0.00}m, " +
                $"escapeHalfWidth={CrowdEscapeCorridorHalfWidth:0.00}m.");
        }

        if (_crowdAgents.Count < targetCount)
        {
            RexLog.Warning(
                $"Spawned {_crowdAgents.Count}/{targetCount} execution spectators from " +
                $"{candidates.Count} accepted positions; the scene continues without duplicate spawns.");
        }
        else
        {
            RexLog.Info(
                $"Session {Request.SessionId} spawned the full execution crowd " +
                $"({_crowdAgents.Count}/{targetCount}).");
        }
    }

    private List<Vec3> BuildCrowdCandidatePositions(
        int maximumCandidates,
        out int groundFallbackCandidateCount)
    {
        groundFallbackCandidateCount = 0;
        var result = new List<Vec3>();
        if (_placement is null)
        {
            return result;
        }

        if (UsesStoningExecution())
        {
            return BuildStoningCrowdCandidatePositions(
                maximumCandidates,
                out groundFallbackCandidateCount);
        }

        var player = _playerAgent;
        var corridorCenterRight = 0f;
        var corridorStartForward = 0f;
        if (player is not null && player.IsActive())
        {
            var playerDelta = player.Position.AsVec2 - _placement.Origin.AsVec2;
            var right = _placement.Right;
            var forward = _placement.Forward;
            corridorCenterRight = playerDelta.x * right.x + playerDelta.y * right.y;
            corridorStartForward = playerDelta.x * forward.x + playerDelta.y * forward.y;
        }

        var minimumPlayerClearanceSquared =
            MinimumCrowdPlayerClearance * MinimumCrowdPlayerClearance;
        var minimumCrowdSpacingSquared = MinimumCrowdSpacing * MinimumCrowdSpacing;

        for (var ring = 0; ring < 6 && result.Count < maximumCandidates; ring++)
        {
            var distance = CrowdFrontRowDistance + ring * CrowdRingSpacing;
            var slots = 10 + ring * 2;
            var lateralSpan = 3.40f + ring * 0.72f;
            var lateralOffsets = Enumerable.Range(0, slots)
                .Select(slot =>
                {
                    var fraction = slots == 1 ? 0f : slot / (float)(slots - 1);
                    return (fraction * 2f - 1f) * lateralSpan;
                })
                .OrderBy(lateral => MathF.Abs(lateral))
                .ThenBy(lateral => lateral)
                .ToArray();
            foreach (var lateral in lateralOffsets)
            {
                if (result.Count >= maximumCandidates)
                {
                    break;
                }

                if (distance >= corridorStartForward - CrowdEscapeCorridorStartPadding &&
                    MathF.Abs(lateral - corridorCenterRight) < CrowdEscapeCorridorHalfWidth)
                {
                    continue;
                }

                var raw = _placement.Offset(lateral, distance);
                var usedGroundFallback = false;
                if (!TrySnapNavigable(
                        raw,
                        out var safe,
                        requireDirectLineFromPlacement: false))
                {
                    if (!_placement.IsCompatibilityFallback ||
                        !TryResolveStaticCrowdGroundFallback(raw, out safe))
                    {
                        continue;
                    }

                    usedGroundFallback = true;
                }

                if (
                    (player is not null && player.IsActive() &&
                     player.Position.DistanceSquared(safe) < minimumPlayerClearanceSquared) ||
                    result.Any(existing =>
                        existing.DistanceSquared(safe) < minimumCrowdSpacingSquared))
                {
                    continue;
                }

                result.Add(safe);
                if (usedGroundFallback)
                {
                    groundFallbackCandidateCount++;
                }
            }
        }

        return result;
    }

    private List<Vec3> BuildStoningCrowdCandidatePositions(
        int maximumCandidates,
        out int groundFallbackCandidateCount)
    {
        groundFallbackCandidateCount = 0;
        var result = new List<Vec3>();
        if (_placement is null || !_victimPosition.IsValid || maximumCandidates <= 0)
        {
            return result;
        }

        var player = _playerAgent;
        var entranceDirection = player is not null && player.IsActive()
            ? player.Position.AsVec2 - _victimPosition.AsVec2
            : _placement.Forward;
        entranceDirection = entranceDirection.IsNonZero()
            ? entranceDirection.Normalized()
            : _placement.Forward.Normalized();
        var entranceDotThreshold = MathF.Cos(StoningCrowdEntranceHalfAngle);
        var minimumPlayerClearanceSquared =
            MinimumCrowdPlayerClearance * MinimumCrowdPlayerClearance;
        var minimumCrowdSpacingSquared = MinimumCrowdSpacing * MinimumCrowdSpacing;
        var slotCount = Math.Max(32, Math.Min(48, maximumCandidates));
        for (var slot = 0; slot < slotCount && result.Count < maximumCandidates; slot++)
        {
            var baseAngle = slot * (2f * MathF.PI / slotCount);
            var angleJitter = (MBRandom.RandomFloat * 2f - 1f) * 0.11f;
            var angle = baseAngle + angleJitter;
            var localRight = MathF.Cos(angle);
            var localForward = MathF.Sin(angle);
            var radialDirection = new Vec2(
                _placement.Right.x * localRight + _placement.Forward.x * localForward,
                _placement.Right.y * localRight + _placement.Forward.y * localForward).Normalized();
            if (Vec2.DotProduct(radialDirection, entranceDirection) >= entranceDotThreshold)
            {
                continue;
            }

            var radius = 4.20f + MBRandom.RandomFloat * 1.20f;
            var raw = _victimPosition + new Vec3(
                radialDirection.x * radius,
                radialDirection.y * radius,
                0f);
            var usedGroundFallback = false;
            if (!TrySnapNavigable(
                    raw,
                    out var safe,
                    requireDirectLineFromPlacement: false))
            {
                if (!_placement.IsCompatibilityFallback ||
                    !TryResolveStaticCrowdGroundFallback(raw, out safe))
                {
                    continue;
                }

                usedGroundFallback = true;
            }

            if ((player is not null && player.IsActive() &&
                 player.Position.DistanceSquared(safe) < minimumPlayerClearanceSquared) ||
                result.Any(existing =>
                    existing.DistanceSquared(safe) < minimumCrowdSpacingSquared))
            {
                continue;
            }

            result.Add(safe);
            if (usedGroundFallback)
            {
                groundFallbackCandidateCount++;
            }
        }

        RexLog.Info(
            $"Session {Request.SessionId} prepared {result.Count} stoning crowd ring candidates " +
            $"around the kneeling prisoner (radius={StoningCrowdRingRadius:0.00}m, " +
            $"entranceHalfAngle={StoningCrowdEntranceHalfAngle * 180f / MathF.PI:0.0} degrees).");
        return result;
    }

}
