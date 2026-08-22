using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public readonly struct TownHiddenResidentSpawnOffset
{
    public TownHiddenResidentSpawnOffset(float forwardMeters, float rightMeters)
    {
        ForwardMeters = forwardMeters;
        RightMeters = rightMeters;
    }

    public float ForwardMeters { get; }

    public float RightMeters { get; }

    public float DistanceSquared => ForwardMeters * ForwardMeters + RightMeters * RightMeters;
}

/// <summary>
/// Dependency-free candidate ordering for hidden residents appearing outside the player's view.
/// </summary>
public static class TownHiddenResidentSpawnPositionPolicy
{
    public const float MinimumPlayerDistance = 18f;

    public const float MinimumExistingAgentDistance = 2.5f;

    public const float NavmeshSampleMinRadius = 1.5f;

    public const float NavmeshSampleMaxRadius = 4.5f;

    public const int NavmeshSampleAttempts = 6;

    private static readonly IReadOnlyList<TownHiddenResidentSpawnOffset> CandidateOffsets =
        Array.AsReadOnly(new[]
        {
            new TownHiddenResidentSpawnOffset(-18f, -15f),
            new TownHiddenResidentSpawnOffset(-18f, 15f),
            new TownHiddenResidentSpawnOffset(-8f, -24f),
            new TownHiddenResidentSpawnOffset(-8f, 24f),
        });

    public static IReadOnlyList<TownHiddenResidentSpawnOffset> GetCandidateOffsets()
    {
        return CandidateOffsets;
    }

    public static TownHiddenResidentSpawnOffset GetGroupSlotOffset(int index)
    {
        int safeIndex = Math.Max(0, index);
        int column = safeIndex % 3;
        int row = safeIndex / 3;
        return new TownHiddenResidentSpawnOffset(-row * 1.4f, (column - 1) * 1.25f);
    }
}
