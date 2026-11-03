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
    private int _fieldCampHelperNumber;

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

    private void BeginFieldCampActorEntry()
    {
        foreach (var entity in _spawnedEntities)
            if (!_fieldCampInitialFrames.ContainsKey(entity)) _fieldCampInitialFrames[entity] = entity.GetGlobalFrame();
        _fieldCampActorsEntering = true;
        foreach (var pair in _fieldCampEntryMarks)
        {
            if (VengeanceIntegration.FieldCampWalkActor?.Invoke(Request.SessionId, pair.Key,
                pair.Value.Position, pair.Value.Direction, Array.Empty<Vec3>()) != true)
                throw new InvalidOperationException("Camp personnel could not be positioned at the ceremony mark: " + pair.Key.Index);
        }
        VengeanceIntegration.FieldCampArrangeSpectators?.Invoke(Request.SessionId);
        RexLog.Info($"Session {Request.SessionId} existing ceremony personnel and prisoner positioned immediately; spectators walk independently.");
    }

    private bool TickFieldCampActorEntry(float dt)
    {
        if (!_fieldCampActorsEntering) return true;
        var ready = VengeanceIntegration.FieldCampActorReadiness?.Invoke(Request.SessionId) ?? FieldCampCeremonyReadiness.Failed;
        if (ready == FieldCampCeremonyReadiness.Pending) return false;
        if (ready == FieldCampCeremonyReadiness.Failed)
        {
            CancelSceneAndReturn(ExecutionFailureReason.ScenePlacementFailed,
                new TextObject("{=REX_Error_FieldCamp_Unavailable}The field camp cannot host an execution right now."));
            return false;
        }
        _fieldCampActorsEntering = false;
        foreach (var pair in _fieldCampEntryMarks)
            if (pair.Key.IsActive()) PoseAgentAt(pair.Key, pair.Value.Position, pair.Value.Direction);
        TryPlayAction(_victimAgent!, _sceneAct!.VictimIdleAction, "borrowed prisoner positioned");
        _executionerInitialStandbySettlePending = !TryRestoreExecutionerEntryStandby();
        RexLog.Info($"Session {Request.SessionId} existing camp actors ready; F execution entry enabled without spectator gate.");
        return true;
    }
}
