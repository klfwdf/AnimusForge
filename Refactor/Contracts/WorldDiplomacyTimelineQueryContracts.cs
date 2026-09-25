using System;

namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyTimelineRevisionStatus
{
    Unavailable,
    Available,
    Failed
}

/// <summary>
/// Allocation-free result for the high-frequency world-message timeline revision read.
/// Unavailable and failed reads intentionally project to revision zero at the legacy boundary.
/// </summary>
public readonly struct WorldDiplomacyTimelineRevisionResult
{
    private WorldDiplomacyTimelineRevisionResult(
        WorldDiplomacyTimelineRevisionStatus status,
        long revision)
    {
        Status = status;
        Revision = status == WorldDiplomacyTimelineRevisionStatus.Available
            ? Math.Max(0L, revision)
            : 0L;
    }

    public WorldDiplomacyTimelineRevisionStatus Status { get; }
    public long Revision { get; }
    public bool IsAvailable => Status == WorldDiplomacyTimelineRevisionStatus.Available;

    public static WorldDiplomacyTimelineRevisionResult Available(long revision)
    {
        return new WorldDiplomacyTimelineRevisionResult(
            WorldDiplomacyTimelineRevisionStatus.Available,
            revision);
    }

    public static WorldDiplomacyTimelineRevisionResult Unavailable()
    {
        return new WorldDiplomacyTimelineRevisionResult(
            WorldDiplomacyTimelineRevisionStatus.Unavailable,
            0L);
    }

    public static WorldDiplomacyTimelineRevisionResult Failed()
    {
        return new WorldDiplomacyTimelineRevisionResult(
            WorldDiplomacyTimelineRevisionStatus.Failed,
            0L);
    }
}

public interface IWorldDiplomacyTimelineRevisionQuery
{
    WorldDiplomacyTimelineRevisionResult Query();
}
