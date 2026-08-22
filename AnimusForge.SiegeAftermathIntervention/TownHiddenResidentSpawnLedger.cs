using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownHiddenResidentSpawnStatus
{
    Ready = 0,
    Spawned = 1,
    ResidentsAlreadyVisible = 2,
    SceneSpawnLimitReached = 3,
    SceneAgentLimitReached = 4,
    OperationSnapshotLocked = 5,
    DestructiveCombatActive = 6,
    RuntimeUnavailable = 7,
    NoSafeCorner = 8,
    SpawnFailed = 9,
}

public readonly struct TownHiddenResidentSpawnPlan
{
    public TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus status, int requestedCount)
    {
        Status = status;
        RequestedCount = Math.Max(0, requestedCount);
    }

    public TownHiddenResidentSpawnStatus Status { get; }

    public int RequestedCount { get; }

    public bool CanSpawn => Status == TownHiddenResidentSpawnStatus.Ready && RequestedCount > 0;
}

public readonly struct TownHiddenResidentSpawnOutcome
{
    public TownHiddenResidentSpawnOutcome(
        TownHiddenResidentSpawnStatus status,
        int requestedCount,
        int spawnedCount)
    {
        Status = status;
        RequestedCount = Math.Max(0, requestedCount);
        SpawnedCount = Math.Max(0, spawnedCount);
    }

    public TownHiddenResidentSpawnStatus Status { get; }

    public int RequestedCount { get; }

    public int SpawnedCount { get; }

    public bool HasSpawnedResidents => Status == TownHiddenResidentSpawnStatus.Spawned && SpawnedCount > 0;
}

/// <summary>
/// Owns the scene-local anti-farming budget for residents brought out of hiding.
/// </summary>
public sealed class TownHiddenResidentSpawnLedger
{
    public const int PerRequestLimit = 6;

    public const int PerSceneLimit = 12;

    public const int VisibleCivilianLimit = 24;

    private int _spawnedCount;

    public int SpawnedCount => _spawnedCount;

    public TownHiddenResidentSpawnPlan Plan(
        int currentVisibleCivilianCount,
        int currentActiveHumanCount,
        int sceneAgentSoftLimit,
        bool operationSnapshotLocked,
        bool destructiveCombatActive)
    {
        if (destructiveCombatActive)
        {
            return new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.DestructiveCombatActive, 0);
        }
        if (operationSnapshotLocked)
        {
            return new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.OperationSnapshotLocked, 0);
        }

        int visible = Math.Max(0, currentVisibleCivilianCount);
        if (visible >= VisibleCivilianLimit)
        {
            return new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.ResidentsAlreadyVisible, 0);
        }
        if (_spawnedCount >= PerSceneLimit)
        {
            return new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.SceneSpawnLimitReached, 0);
        }

        int humanCount = Math.Max(0, currentActiveHumanCount);
        int softLimit = Math.Max(0, sceneAgentSoftLimit);
        int sceneRoom = Math.Max(0, softLimit - humanCount);
        if (sceneRoom == 0)
        {
            return new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.SceneAgentLimitReached, 0);
        }

        int requested = Math.Min(PerRequestLimit, PerSceneLimit - _spawnedCount);
        requested = Math.Min(requested, VisibleCivilianLimit - visible);
        requested = Math.Min(requested, sceneRoom);
        return requested > 0
            ? new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.Ready, requested)
            : new TownHiddenResidentSpawnPlan(TownHiddenResidentSpawnStatus.SceneAgentLimitReached, 0);
    }

    public TownHiddenResidentSpawnOutcome Record(TownHiddenResidentSpawnPlan plan, int spawnedCount)
    {
        if (!plan.CanSpawn)
        {
            return new TownHiddenResidentSpawnOutcome(plan.Status, plan.RequestedCount, 0);
        }

        int accepted = Math.Min(plan.RequestedCount, Math.Max(0, spawnedCount));
        accepted = Math.Min(accepted, Math.Max(0, PerSceneLimit - _spawnedCount));
        _spawnedCount += accepted;
        return new TownHiddenResidentSpawnOutcome(
            accepted > 0 ? TownHiddenResidentSpawnStatus.Spawned : TownHiddenResidentSpawnStatus.SpawnFailed,
            plan.RequestedCount,
            accepted);
    }

    public void Reset()
    {
        _spawnedCount = 0;
    }
}
