using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private sealed class CampPrefabRecord
    {
        internal readonly string Key;
        internal readonly Vec3 Position;
        internal readonly MatrixFrame Frame;
        internal readonly GameEntity Entity;
        internal CampPrefabRecord(string key, Vec3 position, MatrixFrame frame, GameEntity entity)
        { Key = key; Position = position; Frame = frame; Entity = entity; }
    }
    private readonly List<CampPrefabRecord> _fieldCampPrefabRecords = new();
    private readonly HashSet<GameEntity> _fieldCampReusedPrefabs = new();
    private readonly Dictionary<GameEntity, MatrixFrame> _fieldCampInitialFrames = new();
    private readonly HashSet<Agent> _borrowedCampActors = new();
    private readonly Dictionary<Agent, (Vec3 Position, Vec2 Direction)> _fieldCampEntryMarks = new();
    private bool _buildingFieldCampScene;
    private bool _retainedFieldCampSite;
    private bool _fieldCampActorsEntering;
    private bool _fieldCampVictimEntryStarted;
    private int _fieldCampHelperNumber;
    private float _fieldCampEntryPoll;
    private IReadOnlyList<Vec3>? _fieldCampStairApproach;

    private CharacterObject GetCampRoleCharacter(string role)
    {
        var agent = VengeanceIntegration.FieldCampAcquireActor?.Invoke(Request.SessionId, role, null);
        if (agent?.Character is not CharacterObject character)
            throw new InvalidOperationException("No existing camp resident is available for " + role);
        _borrowedCampActors.Add(agent);
        return character;
    }

    private Agent BorrowExistingFieldCampActor(string role, Vec3 position, Vec2 direction, bool noWeapons,
        bool invulnerable, Team? team)
    {
        var agent = VengeanceIntegration.FieldCampAcquireActor?.Invoke(Request.SessionId, role,
            role == "victim" ? Request.Victim : null);
        if (agent is null || !agent.IsActive())
            throw new InvalidOperationException("The requested existing camp Agent is unavailable: " + role);
        _borrowedCampActors.Add(agent);
        _fieldCampEntryMarks[agent] = (position, direction);
        agent.DisableScriptedMovement(); agent.ClearTargetFrame(); agent.SetIsAIPaused(true);
        agent.Controller = AgentControllerType.None;
        if (team is not null && agent.Team != team) agent.SetTeam(team, false);
        if (invulnerable) agent.SetMortalityState(Agent.MortalityState.Invulnerable);
        if (noWeapons)
        {
            for (var i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
                agent.RemoveEquippedWeapon((EquipmentIndex)i);
            agent.UpdateWeapons();
        }
        _occupiedSpawnPositions.Add(position);
        RexLog.Info($"Session {Request.SessionId} borrowed camp role={role} Agent={agent.Index}; no Agent was spawned.");
        return agent;
    }

    private bool TryReuseFieldCampPrefab(string key, Vec3 position, out GameEntity? entity)
    {
        foreach (var record in _fieldCampPrefabRecords)
        {
            if (record.Key != key || record.Position.DistanceSquared(position) > 0.04f ||
                !_spawnedEntities.Contains(record.Entity) || !_fieldCampReusedPrefabs.Add(record.Entity)) continue;
            entity = record.Entity;
            return true;
        }
        entity = null;
        return false;
    }

    internal void AdoptRetainedFieldCampSite(TownExecutionMissionBehavior previous)
    {
        _retainedFieldCampSite = true;
        _spawnedEntities.AddRange(previous._spawnedEntities);
        _spawnedAgents.AddRange(previous._spawnedAgents);
        _pendingEntityRemovals.AddRange(previous._pendingEntityRemovals);
        _gallowsRampEntities.AddRange(previous._gallowsRampEntities);
        _fieldCampPrefabRecords.AddRange(previous._fieldCampPrefabRecords);
        foreach (var pair in previous._profileEntities) _profileEntities[pair.Key] = pair.Value;
        foreach (var pair in previous._entityBaseLengths) _entityBaseLengths[pair.Key] = pair.Value;
        foreach (var pair in previous._fieldCampInitialFrames)
        {
            if (!_spawnedEntities.Contains(pair.Key)) continue;
            pair.Key.SetGlobalFrame(pair.Value);
            _fieldCampInitialFrames[pair.Key] = pair.Value;
        }
        _executionerAgent = previous._executionerAgent;
        _executionerStandbyPosition = previous._executionerStandbyPosition;
        _executionerStandbyDirection = previous._executionerStandbyDirection;
        _gallowsEntity = previous._gallowsEntity;
        _gallowsDeckBoundsMin = previous._gallowsDeckBoundsMin; _gallowsDeckBoundsMax = previous._gallowsDeckBoundsMax;
        _stageSurfaceHeight = previous._stageSurfaceHeight; _stageSideExtent = previous._stageSideExtent;
        _stageFrontExtent = previous._stageFrontExtent; _frontInteractionOffset = previous._frontInteractionOffset;
        _burningStakeEntity = previous._burningStakeEntity; _burningCrossbeamEntity = previous._burningCrossbeamEntity;
        _burningStakeBaseFrame = previous._burningStakeBaseFrame; _burningCrossbeamBaseFrame = previous._burningCrossbeamBaseFrame;
        _burningStakeFrameCaptured = previous._burningStakeFrameCaptured; _burningCrossbeamFrameCaptured = previous._burningCrossbeamFrameCaptured;
        _burningSupportEntities.AddRange(previous._burningSupportEntities);
        _burningSupportBaseFrames.AddRange(previous._burningSupportBaseFrames);
        _fieldCampPreparationAreas = previous._fieldCampPreparationAreas;
        _fieldCampPreparationRadius = previous._fieldCampPreparationRadius;
        _fieldCampStairApproach = previous._fieldCampStairApproach;
        RexLog.Info($"Session {Request.SessionId} retained {_spawnedEntities.Count} existing site entities; stage teardown/rebuild skipped.");
    }

    private void DetachFieldCampSiteForContinuation()
    {
        EndCeremonySpeech();
        RestorePlayerExecutionState();
        if (_focusCallbackRegistered)
        {
            Mission.FocusableObjectInformationProvider.RemoveInfoCallback(GetExecutionFocusText);
            _focusCallbackRegistered = false;
        }
        // Release sentence-specific restraints/use bindings and transient effects only.
        // Strategy cleanup does not own the shared stage or camp personnel.
        if (!TryCleanupExecutionStrategy()) throw new InvalidOperationException("Previous sentence bindings could not be released.");
        // Only retire the previous sentence's dead presentation and consumed focus proxy.
        // The actual site, executioner, guards and their original camp snapshots remain owned.
        if (_victimAgent?.IsActive() == true) _victimAgent.FadeOut(true, false);
        if (_usePointHost is not null) RemoveSpawnedEntity(_usePointHost);
        if (_visualProfile is not null)
            foreach (var effect in _visualProfile.Props.Where(prop => prop.IsIgnitionEffect))
                if (_profileEntities.TryGetValue(effect.RoleId, out var entity))
                { RemoveSpawnedEntity(entity); _profileEntities.Remove(effect.RoleId); }
        RestoreStoningVictimProtection(); RestoreCrossbowVictimRestraint(); TryRestoreCrossbowDeathEnvironment();
        _cleanupComplete = true;
        _strategyCleanupComplete = true;
    }

    private IReadOnlyList<Vec3> FieldCampStageApproach(Vec3 destination)
    {
        if (_placement is null || _stageSurfaceHeight <= 0.35f || destination.z < _placement.Origin.z + 0.45f)
            return Array.Empty<Vec3>();
        if (_fieldCampStairApproach is not null) return _fieldCampStairApproach;
        GameEntity? ramp = _gallowsRampEntities.FirstOrDefault();
        if (ramp is null && _gallowsEntity is not null)
            ramp = _gallowsEntity.GetEntityAndChildren().FirstOrDefault(part =>
                (part.Name ?? string.Empty).IndexOf("stairs", StringComparison.OrdinalIgnoreCase) >= 0);
        if (ramp is not null &&
            TryProjectStairFootprint(ramp.GetGlobalFrame(), ramp.GetLocalBoundingBox(),
                out var runAlongForward, out var runMinimum, out var runMaximum, out var widthCenter, out _) &&
            TrySampleStairSurface(ramp, ramp.GetGlobalBoundingBox(), runAlongForward,
                runMinimum, runMaximum, widthCenter, out var samples))
        {
            // The existing static-body probes measure actual treads; a local
            // bounding-box top has the same Z at both ends of an authored stair.
            bool ascending = samples[samples.Count - 1].Height >= samples[0].Height;
            var approach = new Vec3[samples.Count + 1];
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[ascending ? i : samples.Count - 1 - i];
                approach[i + 1] = ComposePlacementPoint(runAlongForward, sample.RunCoordinate,
                    widthCenter, sample.Height + 0.04f);
            }
            var away = approach[1].AsVec2 - approach[approach.Length - 1].AsVec2;
            if (away.IsNonZero())
            {
                away = away.Normalized();
                var foot = approach[1] + new Vec3(away.x * 1.6f, away.y * 1.6f, 0f);
                foot.z = Mission.Scene.GetTerrainHeight(foot.AsVec2, true);
                approach[0] = foot;
                _fieldCampStairApproach = approach;
                return approach;
            }
        }
        var entrance = _placement.Offset(0f, _frontInteractionOffset + 2f);
        entrance.z = Mission.Scene.GetTerrainHeight(entrance.AsVec2, true);
        _fieldCampStairApproach = new[] { entrance, _placement.Offset(0f, _stageFrontExtent - 0.5f, _stageSurfaceHeight) };
        return _fieldCampStairApproach;
    }

    private void BeginFieldCampActorEntry()
    {
        foreach (var entity in _spawnedEntities)
            if (!_fieldCampInitialFrames.ContainsKey(entity)) _fieldCampInitialFrames[entity] = entity.GetGlobalFrame();
        _fieldCampActorsEntering = true; _fieldCampVictimEntryStarted = false; _fieldCampEntryPoll = 0f;
        foreach (var pair in _fieldCampEntryMarks)
        {
            if (pair.Key == _victimAgent) continue;
            if (VengeanceIntegration.FieldCampWalkActor?.Invoke(Request.SessionId, pair.Key,
                pair.Value.Position, pair.Value.Direction, FieldCampStageApproach(pair.Value.Position)) != true)
                throw new InvalidOperationException("Camp personnel cannot walk to the retained ceremony mark: " + pair.Key.Index);
        }
        RexLog.Info($"Session {Request.SessionId} site exists; resetting borrowed personnel before spectators and prisoner entry.");
    }

    private bool TickFieldCampActorEntry(float dt)
    {
        if (!_fieldCampActorsEntering) return true;
        _fieldCampEntryPoll -= MathF.Max(0f, dt);
        if (_fieldCampEntryPoll > 0f) return false;
        _fieldCampEntryPoll = 0.5f;
        var ready = VengeanceIntegration.FieldCampActorReadiness?.Invoke(Request.SessionId) ?? FieldCampCeremonyReadiness.Failed;
        if (ready == FieldCampCeremonyReadiness.Pending) return false;
        if (ready == FieldCampCeremonyReadiness.Failed)
        {
            CancelSceneAndReturn(ExecutionFailureReason.ScenePlacementFailed,
                new TextObject("{=REX_Error_FieldCamp_Unavailable}The field camp cannot host an execution right now."));
            return false;
        }
        if (!_fieldCampVictimEntryStarted)
        {
            VengeanceIntegration.FieldCampArrangeSpectators?.Invoke(Request.SessionId);
            _fieldCampVictimEntryStarted = true;
            if (_victimAgent is null || VengeanceIntegration.FieldCampWalkActor?.Invoke(Request.SessionId, _victimAgent,
                _victimPosition, _placement!.Forward, FieldCampStageApproach(_victimPosition)) != true)
                CancelSceneAndReturn(ExecutionFailureReason.ScenePlacementFailed,
                    new TextObject("{=REX_Error_FieldCamp_Unavailable}The field camp cannot host an execution right now."));
            return false;
        }
        _fieldCampActorsEntering = false;
        foreach (var pair in _fieldCampEntryMarks)
            if (pair.Key.IsActive()) PoseAgentAt(pair.Key, pair.Value.Position, pair.Value.Direction);
        TryPlayAction(_victimAgent!, _sceneAct!.VictimIdleAction, "borrowed prisoner arrived");
        _executionerInitialStandbySettlePending = !TryRestoreExecutionerEntryStandby();
        RexLog.Info($"Session {Request.SessionId} existing camp prisoner reached the existing site; F execution entry enabled.");
        return true;
    }
}
