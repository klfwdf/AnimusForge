using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Diagnostics;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

internal enum ExecutionPlacementSource
{
    TownGate = 0,
    MissionEntryFallback = 1
}

internal sealed class ExecutionScenePlacement
{
    public ExecutionScenePlacement(
        Vec3 origin,
        Vec2 forward,
        bool supportsFullStage,
        ExecutionPlacementSource source,
        bool isCompatibilityFallback = false)
    {
        Origin = origin;
        Forward = forward.IsNonZero() ? forward.Normalized() : Vec2.Forward;
        SupportsFullStage = supportsFullStage;
        Source = source;
        IsCompatibilityFallback = isCompatibilityFallback;
    }

    public Vec3 Origin { get; }
    public Vec2 Forward { get; }
    public Vec2 Right => new(Forward.y, -Forward.x);
    public bool SupportsFullStage { get; }
    public ExecutionPlacementSource Source { get; }
    public bool IsCompatibilityFallback { get; }

    public MatrixFrame CreateFrame(Vec3 position)
    {
        var forward3 = new Vec3(Forward.x, Forward.y, 0f);
        return new MatrixFrame(Mat3.CreateMat3WithForward(in forward3), position);
    }

    public Vec3 Offset(float right, float forward, float up = 0f) =>
        Origin + new Vec3(Right.x * right + Forward.x * forward,
            Right.y * right + Forward.y * forward,
            up);
}

internal static class ExecutionScenePlacementSolver
{
    private readonly struct VisualObstacleBounds
    {
        public VisualObstacleBounds(string name, Vec3 min, Vec3 max)
        {
            Name = name;
            Min = min;
            Max = max;
        }

        public string Name { get; }
        public Vec3 Min { get; }
        public Vec3 Max { get; }
    }

    // Native town scenes mark the exterior approach to the main gate with
    // this tag. A public execution never falls back to an interior square.
    private const string TownGateExteriorTag = "sp_outside_near_town_main_gate";
    private const float MinimumOpenAirClearance = 6f;
    private const float GallowsFootprintFrontDepth = 5.25f;
    private const float GallowsFootprintHalfWidth = 4.25f;
    private const float GallowsRearClearanceDepth = 2.75f;
    private const float GallowsGroundSupportHeightTolerance = 0.55f;
    private const float TerrainCompatibilityGroundSupportHeightTolerance = 0.90f;
    private const float GroundExecutionFootprintRadius = 1.35f;
    private const float GroundExecutionHeightTolerance = 0.85f;
    private const float GroundExecutionOpenAirClearance = 2.75f;
    private const float PublicApproachNearDistance = 6.00f;
    private const float PublicApproachFarDistance = 9.25f;
    private const float PublicApproachLaneOffset = 1.25f;
    private static readonly (float Right, float Forward)[] GateExteriorOffsets =
    {
        (0f, 0f),
        (0f, 3.5f),
        (-3.0f, 3.5f),
        (3.0f, 3.5f),
        (0f, 7.0f),
        (-3.5f, 7.0f),
        (3.5f, 7.0f),
        (0f, 10.5f),
        (-4.5f, 10.5f),
        (4.5f, 10.5f),
        (0f, 14.0f)
    };
    private static readonly (float Right, float Forward)[] TerrainCompatibilityGateOffsets =
        BuildTerrainCompatibilityGateOffsets();
    private static readonly float[] TerrainCompatibilityHeadingOffsetsDegrees =
    {
        0f,
        -22.5f,
        22.5f
    };

    public static bool TryFind(
        Mission mission,
        bool usesGroundExecutionSite,
        out ExecutionScenePlacement placement)
    {
        placement = null!;
        var main = mission.MainAgent ?? Agent.Main;
        if (main is null || !main.IsActive())
        {
            return false;
        }

        var requiresFullStage = !usesGroundExecutionSite;
        var gateAnchors = mission.Scene.FindEntitiesWithTag(TownGateExteriorTag).ToList();
        if (gateAnchors.Count == 0)
        {
            RexLog.Warning(
                $"Scene '{mission.SceneName}' exposes no '{TownGateExteriorTag}' marker; " +
                "falling back to a bounded search around the live town-entry player instead of cancelling.");
            return TryFindFromMissionEntry(
                mission,
                main,
                requiresFullStage,
                out placement);
        }

        if (TryFindFromGateAnchors(
                mission,
                main,
                gateAnchors,
                GateExteriorOffsets,
                new[] { 0f },
                GallowsGroundSupportHeightTolerance,
                requiresFullStage,
                "preferred",
                out placement))
        {
            return true;
        }

        RexLog.Info(
            "The preferred outside-gate marks were unavailable; starting the wider third-party-terrain compatibility search.");
        if (TryFindFromGateAnchors(
            mission,
            main,
            gateAnchors,
            TerrainCompatibilityGateOffsets,
            TerrainCompatibilityHeadingOffsetsDegrees,
            TerrainCompatibilityGroundSupportHeightTolerance,
            requiresFullStage,
            "terrain-compatible",
            out placement))
        {
            return true;
        }

        RexLog.Warning(
            $"No fully validated {(requiresFullStage ? "scaffold" : "ground-pyre")} placement was available " +
            "at the outside-gate marker; selecting the best navigable exterior fallback instead of cancelling.");
        return TryFindUniversalFallback(
            mission,
            main,
            gateAnchors,
            requiresFullStage,
            out placement);
    }

    private static bool TryFindFromGateAnchors(
        Mission mission,
        Agent main,
        IReadOnlyList<GameEntity> gateAnchors,
        IReadOnlyList<(float Right, float Forward)> offsets,
        IReadOnlyList<float> headingOffsetsDegrees,
        float groundSupportHeightTolerance,
        bool requiresFullStage,
        string searchLabel,
        out ExecutionScenePlacement placement)
    {
        placement = null!;
        var rejectionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var candidateCount = 0;
        foreach (var gateAnchor in gateAnchors)
        {
            try
            {
                var exteriorForward = gateAnchor.GetGlobalFrame().rotation.f.AsVec2;
                if (!exteriorForward.IsNonZero())
                {
                    RexLog.Warning(
                        "A town-gate exterior marker had no usable forward direction; it was skipped.");
                    continue;
                }

                exteriorForward = exteriorForward.Normalized();
                var exteriorRight = new Vec2(exteriorForward.y, -exteriorForward.x);
                foreach (var offset in offsets)
                {
                    var candidatePosition = gateAnchor.GlobalPosition + new Vec3(
                        exteriorRight.x * offset.Right + exteriorForward.x * offset.Forward,
                        exteriorRight.y * offset.Right + exteriorForward.y * offset.Forward,
                        0f);
                    foreach (var headingOffsetDegrees in headingOffsetsDegrees)
                    {
                        candidateCount++;
                        var candidateForward = RotateDirection(
                            exteriorForward,
                            headingOffsetDegrees);
                        var gateOrigin = Vec3.Invalid;
                        var gateForward = Vec2.Forward;
                        var rejectionReason = string.Empty;
                        var accepted = requiresFullStage
                            ? TryCreateAt(
                                mission,
                                main,
                                candidatePosition,
                                candidateForward,
                                groundSupportHeightTolerance,
                                out gateOrigin,
                                out gateForward,
                                out rejectionReason)
                            : TryCreateGroundExecutionAt(
                                mission,
                                main,
                                candidatePosition,
                                candidateForward,
                                groundSupportHeightTolerance,
                                out gateOrigin,
                                out gateForward,
                                out rejectionReason);
                        if (!accepted)
                        {
                            rejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
                                ? "unspecified safety check"
                                : rejectionReason;
                            rejectionCounts.TryGetValue(rejectionReason, out var rejected);
                            rejectionCounts[rejectionReason] = rejected + 1;
                            continue;
                        }

                        placement = new ExecutionScenePlacement(
                            gateOrigin,
                            gateForward,
                            supportsFullStage: requiresFullStage,
                            ExecutionPlacementSource.TownGate);
                        RexLog.Info(
                            $"Accepted {searchLabel} outside-gate candidate at offset " +
                            $"({offset.Right:0.00}, {offset.Forward:0.00}), " +
                            $"headingOffset={headingOffsetDegrees:0.0} degrees, " +
                            $"profile={(requiresFullStage ? "full-scaffold" : "ground-pyre")}, " +
                            $"groundTolerance={groundSupportHeightTolerance:0.00}m.");
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                // A third-party scene may replace an exterior helper. Isolate
                // that one marker but never look for an interior fallback.
                RexLog.Error("A town-gate exterior marker was invalid; trying the next gate marker.", exception);
            }
        }

        var rejectionSummary = rejectionCounts.Count == 0
            ? "no candidate reached a classified check"
            : string.Join(
                ", ",
                rejectionCounts
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{pair.Key}={pair.Value}"));
        RexLog.Info(
            $"Rejected all {candidateCount} {searchLabel} outside-gate candidate(s) " +
            $"from {gateAnchors.Count} marker(s): " +
            rejectionSummary + ".");
        return false;
    }

    private static (float Right, float Forward)[] BuildTerrainCompatibilityGateOffsets()
    {
        // Scene overhauls commonly move the usable roadside clearing sideways
        // or farther from the vanilla marker. Search a bounded exterior grid,
        // keeping the nearest/central points first so the normal presentation
        // is preferred and the scan remains deterministic.
        var forwardOffsets = new[] { 0f, 4f, 8f, 12f, 16f, 20f, 24f, 28f };
        var rightOffsets = new[] { 0f, -4f, 4f, -8f, 8f, -12f, 12f };
        var result = new List<(float Right, float Forward)>();
        foreach (var forward in forwardOffsets)
        {
            foreach (var right in rightOffsets)
            {
                // The exact marker origin was already checked by the preferred
                // pass. Avoid repeating its comparatively expensive probes.
                if (MathF.Abs(right) < 0.001f && MathF.Abs(forward) < 0.001f)
                {
                    continue;
                }

                result.Add((right, forward));
            }
        }

        return result.ToArray();
    }

    private static bool TryFindUniversalFallback(
        Mission mission,
        Agent main,
        IReadOnlyList<GameEntity> gateAnchors,
        bool requiresFullStage,
        out ExecutionScenePlacement placement)
    {
        placement = null!;
        var forwardOffsets = new[] { 0f, 4f, 8f, 12f, 16f, 20f, 24f, 28f, 32f, 36f, 40f };
        var rightOffsets = new[] { 0f, -4f, 4f, -8f, 8f, -12f, 12f, -16f, 16f, -20f, 20f };
        var headingOffsets = new[] { 0f, -22.5f, 22.5f, -45f, 45f, -90f, 90f };
        var candidateCount = 0;

        foreach (var gateAnchor in gateAnchors)
        {
            var exteriorForward = gateAnchor.GetGlobalFrame().rotation.f.AsVec2;
            if (!exteriorForward.IsNonZero())
            {
                continue;
            }

            exteriorForward = exteriorForward.Normalized();
            var exteriorRight = new Vec2(exteriorForward.y, -exteriorForward.x);
            foreach (var forwardOffset in forwardOffsets)
            {
                foreach (var rightOffset in rightOffsets)
                {
                    var candidate = gateAnchor.GlobalPosition + new Vec3(
                        exteriorRight.x * rightOffset + exteriorForward.x * forwardOffset,
                        exteriorRight.y * rightOffset + exteriorForward.y * forwardOffset,
                        0f);
                    foreach (var headingOffset in headingOffsets)
                    {
                        candidateCount++;
                        var candidateForward = RotateDirection(exteriorForward, headingOffset);
                        if (!TryCreateUniversalAt(
                                mission,
                                main,
                                candidate,
                                candidateForward,
                                requiresFullStage,
                                out var origin,
                                out var forward))
                        {
                            continue;
                        }

                        placement = new ExecutionScenePlacement(
                            origin,
                            forward,
                            supportsFullStage: requiresFullStage,
                            ExecutionPlacementSource.TownGate,
                            isCompatibilityFallback: true);
                        RexLog.Warning(
                            $"Accepted universal outside-gate fallback after {candidateCount} candidate(s): " +
                            $"offset=({rightOffset:0.00}, {forwardOffset:0.00}), " +
                            $"headingOffset={headingOffset:0.0} degrees, " +
                            $"profile={(requiresFullStage ? "full-scaffold" : "ground-pyre")}. " +
                            "Terrain spread, full-footprint overhead and decorative fixed-physics vetoes are diagnostic only for this fallback.");
                        return true;
                    }
                }
            }
        }

        RexLog.Warning(
            $"All {candidateCount} bounded outside-gate fallback candidates were unavailable; " +
            "using the live town-entry area as the final city-compatibility source.");
        return TryFindFromMissionEntry(
            mission,
            main,
            requiresFullStage,
            out placement);
    }

    private static bool TryFindFromMissionEntry(
        Mission mission,
        Agent main,
        bool requiresFullStage,
        out ExecutionScenePlacement placement)
    {
        placement = null!;
        var playerForward = main.LookDirection.AsVec2;
        if (!playerForward.IsNonZero())
        {
            playerForward = Vec2.Forward;
        }

        playerForward = playerForward.Normalized();
        var placementForward = -playerForward;
        var placementRight = new Vec2(placementForward.y, -placementForward.x);
        var distances = new[] { 8f, 12f, 16f, 20f, 24f };
        var lateralOffsets = new[] { 0f, -4f, 4f, -8f, 8f, -12f, 12f };
        foreach (var distance in distances)
        {
            foreach (var lateralOffset in lateralOffsets)
            {
                var candidate = main.Position + new Vec3(
                    playerForward.x * distance + placementRight.x * lateralOffset,
                    playerForward.y * distance + placementRight.y * lateralOffset,
                    0f);
                if (!TryCreateUniversalAt(
                        mission,
                        main,
                        candidate,
                        placementForward,
                        requiresFullStage,
                        out var origin,
                        out var forward))
                {
                    continue;
                }

                placement = new ExecutionScenePlacement(
                    origin,
                    forward,
                    supportsFullStage: requiresFullStage,
                    ExecutionPlacementSource.MissionEntryFallback,
                    isCompatibilityFallback: true);
                RexLog.Warning(
                    $"Accepted town-entry fallback at distance={distance:0.00}m, " +
                    $"lateral={lateralOffset:0.00}m, " +
                    $"profile={(requiresFullStage ? "full-scaffold" : "ground-pyre")}. " +
                    $"Scene '{mission.SceneName}' can continue without a usable outside-gate marker.");
                return true;
            }
        }

        var forcedRaw = main.Position + new Vec3(
            playerForward.x * 8f,
            playerForward.y * 8f,
            0f);
        var forcedOrigin = TrySnapToGround(mission.Scene, forcedRaw, out var grounded) &&
                           mission.IsPositionInsideBoundaries(grounded.AsVec2)
            ? grounded
            : main.Position;
        placement = new ExecutionScenePlacement(
            forcedOrigin,
            placementForward,
            supportsFullStage: requiresFullStage,
            ExecutionPlacementSource.MissionEntryFallback,
            isCompatibilityFallback: true);
        RexLog.Error(
            $"FORCED_CITY_PLACEMENT scene='{mission.SceneName}' at " +
            $"({forcedOrigin.x:0.00}, {forcedOrigin.y:0.00}, {forcedOrigin.z:0.00}); " +
            "the scene supplied no fully navigable fallback, so runtime construction will continue and log individual invariant failures instead of cancelling at placement selection.");
        return true;
    }

    private static bool TryCreateUniversalAt(
        Mission mission,
        Agent main,
        Vec3 candidate,
        Vec2 preferredForward,
        bool requiresFullStage,
        out Vec3 origin,
        out Vec2 forward)
    {
        origin = Vec3.Invalid;
        forward = Vec2.Forward;
        if (!TrySnapToGround(mission.Scene, candidate, out var grounded) ||
            !mission.IsPositionInsideBoundaries(grounded.AsVec2) ||
            mission.Scene.GetNavigationMeshForPosition(in grounded) == UIntPtr.Zero ||
            mission.IsPositionOnAnyBlockerNavMeshFace(grounded))
        {
            return false;
        }

        forward = preferredForward.IsNonZero()
            ? preferredForward.Normalized()
            : Vec2.Forward;
        var right = new Vec2(forward.y, -forward.x);
        var playerEntryForward = requiresFullStage
            ? GallowsFootprintFrontDepth + 1.60f
            : 3.25f;
        if (!HasFallbackNavigableMark(
                mission,
                grounded,
                forward,
                right,
                rightOffset: -2.45f,
                forwardOffset: 0.45f) ||
            !HasFallbackNavigableMark(
                mission,
                grounded,
                forward,
                right,
                rightOffset: 0f,
                forwardOffset: playerEntryForward))
        {
            return false;
        }

        var start = new WorldPosition(mission.Scene, main.Position);
        var end = new WorldPosition(mission.Scene, grounded);
        if (!mission.Scene.GetPathDistanceBetweenPositions(ref start, ref end, 0.35f, out _))
        {
            return false;
        }

        var activeAgentRadius = requiresFullStage ? 4.75f : 1.75f;
        var activeAgentRadiusSquared = activeAgentRadius * activeAgentRadius;
        if (mission.Agents.Any(agent =>
                agent is not null &&
                agent.IsActive() &&
                !ReferenceEquals(agent, main) &&
                agent.Position.AsVec2.DistanceSquared(grounded.AsVec2) < activeAgentRadiusSquared))
        {
            return false;
        }

        origin = grounded;
        return true;
    }

    private static bool HasFallbackNavigableMark(
        Mission mission,
        Vec3 origin,
        Vec2 forward,
        Vec2 right,
        float rightOffset,
        float forwardOffset)
    {
        var probe = origin + new Vec3(
            right.x * rightOffset + forward.x * forwardOffset,
            right.y * rightOffset + forward.y * forwardOffset,
            2f);
        return TrySnapToGround(mission.Scene, probe, out var ground) &&
               mission.IsPositionInsideBoundaries(ground.AsVec2) &&
               mission.Scene.GetNavigationMeshForPosition(in ground) != UIntPtr.Zero &&
               !mission.IsPositionOnAnyBlockerNavMeshFace(ground);
    }

    private static Vec2 RotateDirection(Vec2 direction, float degrees)
    {
        var normalized = direction.IsNonZero() ? direction.Normalized() : Vec2.Forward;
        if (MathF.Abs(degrees) < 0.001f)
        {
            return normalized;
        }

        var radians = degrees * (MathF.PI / 180f);
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        return new Vec2(
            normalized.x * cosine - normalized.y * sine,
            normalized.x * sine + normalized.y * cosine).Normalized();
    }

    private static bool TryCreateGroundExecutionAt(
        Mission mission,
        Agent main,
        Vec3 candidate,
        Vec2 preferredForward,
        float groundSupportHeightTolerance,
        out Vec3 origin,
        out Vec2 forward,
        out string rejectionReason)
    {
        origin = Vec3.Invalid;
        forward = Vec2.Forward;
        rejectionReason = string.Empty;
        if (!TrySnapToGround(mission.Scene, candidate, out var grounded))
        {
            rejectionReason = "ground height unavailable";
            return false;
        }

        if (!mission.IsPositionInsideBoundaries(grounded.AsVec2))
        {
            rejectionReason = "outside mission boundary";
            return false;
        }

        if (mission.Scene.GetNavigationMeshForPosition(in grounded) == UIntPtr.Zero)
        {
            rejectionReason = "center has no navigation mesh";
            return false;
        }

        if (mission.IsPositionOnAnyBlockerNavMeshFace(grounded))
        {
            rejectionReason = "center is on blocker navigation";
            return false;
        }

        if (!HasAcceptableLocalSlope(
                mission.Scene,
                grounded,
                groundSupportHeightTolerance))
        {
            rejectionReason = "center slope exceeds ground-pyre tolerance";
            return false;
        }

        if (!HasVerticalClearance(
                mission.Scene,
                grounded,
                GroundExecutionOpenAirClearance))
        {
            rejectionReason = "ground pyre lacks human-height overhead clearance";
            return false;
        }

        forward = preferredForward.IsNonZero()
            ? preferredForward.Normalized()
            : Vec2.Forward;
        if (!HasCompactGroundExecutionSupport(
                mission,
                grounded,
                forward,
                out rejectionReason))
        {
            return false;
        }

        var start = new WorldPosition(mission.Scene, main.Position);
        var end = new WorldPosition(mission.Scene, grounded);
        var directDistance = MathF.Max(1f, main.Position.AsVec2.Distance(grounded.AsVec2));
        if (!mission.Scene.GetPathDistanceBetweenPositions(ref start, ref end, 0.35f, out var pathDistance) ||
            pathDistance > directDistance * 3f + 8f)
        {
            rejectionReason = "player path cannot reach ground pyre center";
            return false;
        }

        origin = grounded;
        RexLog.Info(
            $"Accepted compact ground-pyre placement at ({origin.x:0.00}, {origin.y:0.00}, {origin.z:0.00}), " +
            $"forward=({forward.x:0.00}, {forward.y:0.00}), " +
            $"radius={GroundExecutionFootprintRadius:0.00}m, fullStage=false.");
        return true;
    }

    private static bool HasCompactGroundExecutionSupport(
        Mission mission,
        Vec3 origin,
        Vec2 forward,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;
        for (var index = 0; index < 8; index++)
        {
            var angle = index * (MathF.PI * 2f / 8f);
            var sample = origin + new Vec3(
                MathF.Cos(angle) * GroundExecutionFootprintRadius,
                MathF.Sin(angle) * GroundExecutionFootprintRadius,
                2f);
            if (!TrySnapToGround(mission.Scene, sample, out var sampleGround))
            {
                rejectionReason = "ground-pyre footprint support is unavailable";
                return false;
            }

            if (!mission.IsPositionInsideBoundaries(sampleGround.AsVec2))
            {
                rejectionReason = "ground-pyre footprint extends outside mission boundary";
                return false;
            }

            if (MathF.Abs(sampleGround.z - origin.z) > GroundExecutionHeightTolerance)
            {
                rejectionReason = "ground-pyre footprint terrain height spread exceeds tolerance";
                return false;
            }

            if (!HasVerticalClearance(
                    mission.Scene,
                    sampleGround,
                    GroundExecutionOpenAirClearance))
            {
                rejectionReason = "ground-pyre footprint lacks human-height overhead clearance";
                return false;
            }
        }

        var right = new Vec2(forward.y, -forward.x);
        if (!HasFallbackNavigableMark(
                mission,
                origin,
                forward,
                right,
                rightOffset: -2.45f,
                forwardOffset: 0.45f))
        {
            rejectionReason = "ground pyre has no navigable executioner mark";
            return false;
        }

        var interactionOffsets = new[]
        {
            (Right: 0f, Forward: 3.25f),
            (Right: -1.10f, Forward: 3.35f),
            (Right: 1.10f, Forward: 3.35f),
            (Right: 0f, Forward: 4.20f)
        };
        foreach (var offset in interactionOffsets)
        {
            var probe = origin + new Vec3(
                right.x * offset.Right + forward.x * offset.Forward,
                right.y * offset.Right + forward.y * offset.Forward,
                2f);
            if (TrySnapToGround(mission.Scene, probe, out var interactionGround) &&
                mission.IsPositionInsideBoundaries(interactionGround.AsVec2) &&
                mission.Scene.GetNavigationMeshForPosition(in interactionGround) != UIntPtr.Zero &&
                !mission.IsPositionOnAnyBlockerNavMeshFace(interactionGround))
            {
                return true;
            }
        }

        rejectionReason = "ground pyre has no navigable ignition edge";
        return false;
    }

    private static bool TryCreateAt(
        Mission mission,
        Agent main,
        Vec3 candidate,
        Vec2 preferredForward,
        float groundSupportHeightTolerance,
        out Vec3 origin,
        out Vec2 forward,
        out string rejectionReason)
    {
        origin = Vec3.Invalid;
        forward = Vec2.Forward;
        rejectionReason = string.Empty;
        if (!TrySnapToGround(mission.Scene, candidate, out var grounded))
        {
            rejectionReason = "ground height unavailable";
            return false;
        }

        if (!mission.IsPositionInsideBoundaries(grounded.AsVec2))
        {
            rejectionReason = "outside mission boundary";
            return false;
        }

        if (mission.Scene.GetNavigationMeshForPosition(in grounded) == UIntPtr.Zero)
        {
            rejectionReason = "center has no navigation mesh";
            return false;
        }

        if (mission.IsPositionOnAnyBlockerNavMeshFace(grounded))
        {
            rejectionReason = "center is on blocker navigation";
            return false;
        }

        var towardPlayer = main.Position.AsVec2 - grounded.AsVec2;
        forward = preferredForward.IsNonZero()
            ? preferredForward.Normalized()
            : towardPlayer.IsNonZero()
                ? towardPlayer.Normalized()
                : Vec2.Forward;
        if (!HasStableGallowsGroundSupport(
                mission,
                grounded,
                forward,
                groundSupportHeightTolerance,
                out rejectionReason))
        {
            return false;
        }

        var start = new WorldPosition(mission.Scene, main.Position);
        var end = new WorldPosition(mission.Scene, grounded);
        var directDistance = MathF.Max(1f, main.Position.AsVec2.Distance(grounded.AsVec2));
        if (!mission.Scene.GetPathDistanceBetweenPositions(ref start, ref end, 0.35f, out var pathDistance) ||
            pathDistance > directDistance * 2.5f + 5f)
        {
            rejectionReason = "player path cannot reach center";
            return false;
        }

        if (!HasClearCeremonyFront(
                mission,
                grounded,
                forward,
                groundSupportHeightTolerance,
                out rejectionReason))
        {
            return false;
        }

        origin = grounded;
        RexLog.Info(
            $"Accepted execution placement at ({origin.x:0.00}, {origin.y:0.00}, {origin.z:0.00}), " +
            $"forward=({forward.x:0.00}, {forward.y:0.00}), " +
            $"groundTolerance={groundSupportHeightTolerance:0.00}m, fullStage=true.");
        return true;
    }

    private static bool HasStableGallowsGroundSupport(
        Mission mission,
        Vec3 origin,
        Vec2 forward,
        float groundSupportHeightTolerance,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (!HasAcceptableLocalSlope(
                mission.Scene,
                origin,
                groundSupportHeightTolerance))
        {
            rejectionReason = "center slope exceeds terrain tolerance";
            return false;
        }

        if (!HasVerticalClearance(mission.Scene, origin))
        {
            rejectionReason = "center lacks overhead clearance";
            return false;
        }

        var normalizedForward = forward.IsNonZero()
            ? forward.Normalized()
            : Vec2.Forward;
        var right = new Vec2(normalizedForward.y, -normalizedForward.x);
        var supportOffsets = new[]
        {
            (Right: -4.05f, Forward: -2.15f),
            (Right: 0f, Forward: -2.15f),
            (Right: 4.05f, Forward: -2.15f),
            (Right: -4.05f, Forward: 0.75f),
            (Right: 4.05f, Forward: 0.75f),
            (Right: -4.05f, Forward: 3.65f),
            (Right: 0f, Forward: 3.65f),
            (Right: 4.05f, Forward: 3.65f)
        };
        foreach (var offset in supportOffsets)
        {
            var probe = origin + new Vec3(
                right.x * offset.Right + normalizedForward.x * offset.Forward,
                right.y * offset.Right + normalizedForward.y * offset.Forward,
                2f);
            if (!TrySnapToGround(mission.Scene, probe, out var supportGround))
            {
                rejectionReason = "gallows footprint support ground is unavailable";
                return false;
            }

            if (!mission.IsPositionInsideBoundaries(supportGround.AsVec2))
            {
                rejectionReason = "gallows footprint extends outside mission boundary";
                return false;
            }

            if (MathF.Abs(supportGround.z - origin.z) > groundSupportHeightTolerance)
            {
                rejectionReason = "gallows footprint terrain height spread exceeds tolerance";
                return false;
            }

            if (!HasVerticalClearance(mission.Scene, supportGround))
            {
                rejectionReason = "gallows footprint lacks overhead clearance";
                return false;
            }
        }

        return true;
    }

    private static bool HasClearCeremonyFront(
        Mission mission,
        Vec3 origin,
        Vec2 forward,
        float groundSupportHeightTolerance,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;
        var normalizedForward = forward.IsNonZero()
            ? forward.Normalized()
            : Vec2.Forward;
        var right = new Vec2(normalizedForward.y, -normalizedForward.x);
        const float frontDepth = GallowsFootprintFrontDepth;
        const float halfWidth = GallowsFootprintHalfWidth;

        if (!HasClearFixedPhysicsCeremonyFront(
                mission.Scene,
                origin,
                normalizedForward,
                right,
                frontDepth,
                halfWidth))
        {
            rejectionReason = "gallows footprint intersects fixed physics";
            return false;
        }

        if (!HasClearRearCollisionCorridor(
                mission.Scene,
                origin,
                normalizedForward,
                right,
                halfWidth))
        {
            rejectionReason = "gallows rear clearance intersects fixed physics";
            return false;
        }

        if (!HasNavigablePublicApproach(
                mission,
                origin,
                normalizedForward,
                right,
                groundSupportHeightTolerance,
                out rejectionReason))
        {
            return false;
        }

        return true;
    }

    private static bool HasNavigablePublicApproach(
        Mission mission,
        Vec3 origin,
        Vec2 forward,
        Vec2 right,
        float groundSupportHeightTolerance,
        out string rejectionReason)
    {
        var laneOffsets = new[] { 0f, -PublicApproachLaneOffset, PublicApproachLaneOffset };
        foreach (var lateralOffset in laneOffsets)
        {
            if (!TryResolveNavigableApproachPoint(
                    mission,
                    origin,
                    forward,
                    right,
                    lateralOffset,
                    PublicApproachNearDistance,
                    groundSupportHeightTolerance,
                    out var near) ||
                !TryResolveNavigableApproachPoint(
                    mission,
                    origin,
                    forward,
                    right,
                    lateralOffset,
                    PublicApproachFarDistance,
                    groundSupportHeightTolerance,
                    out var far))
            {
                continue;
            }

            var pathStart = new WorldPosition(mission.Scene, origin);
            var pathEnd = new WorldPosition(mission.Scene, far);
            var directDistance = MathF.Max(1f, origin.AsVec2.Distance(far.AsVec2));
            if (!mission.Scene.GetPathDistanceBetweenPositions(
                    ref pathStart,
                    ref pathEnd,
                    0.35f,
                    out var pathDistance) ||
                pathDistance > directDistance * 1.75f + 2f)
            {
                continue;
            }

            var lowerStart = near + new Vec3(0f, 0f, 1.10f);
            var lowerEnd = far + new Vec3(0f, 0f, 1.10f);
            if (mission.Scene.RayCastForClosestEntityOrTerrain(
                    lowerStart,
                    lowerEnd,
                    out var collisionDistance,
                    out _,
                    out _,
                    0.10f) &&
                collisionDistance < near.AsVec2.Distance(far.AsVec2) * 0.92f)
            {
                continue;
            }

            rejectionReason = string.Empty;
            return true;
        }

        rejectionReason = "no navigable public approach beside the gallows stairs";
        return false;
    }

    private static bool TryResolveNavigableApproachPoint(
        Mission mission,
        Vec3 origin,
        Vec2 forward,
        Vec2 right,
        float lateralOffset,
        float forwardOffset,
        float groundSupportHeightTolerance,
        out Vec3 ground)
    {
        var probe = origin + new Vec3(
            right.x * lateralOffset + forward.x * forwardOffset,
            right.y * lateralOffset + forward.y * forwardOffset,
            2f);
        return TrySnapToGround(mission.Scene, probe, out ground) &&
               mission.IsPositionInsideBoundaries(ground.AsVec2) &&
               mission.Scene.GetNavigationMeshForPosition(in ground) != UIntPtr.Zero &&
               !mission.IsPositionOnAnyBlockerNavMeshFace(ground) &&
               HasAcceptableLocalSlope(
                   mission.Scene,
                   ground,
                   groundSupportHeightTolerance) &&
               HasVerticalClearance(mission.Scene, ground);
    }

    private static bool HasClearFixedPhysicsCeremonyFront(
        TaleWorlds.Engine.Scene scene,
        Vec3 origin,
        Vec2 forward,
        Vec2 right,
        float frontDepth,
        float halfWidth)
    {
        var corridorStart = -GallowsRearClearanceDepth;
        const float castBoxHalfDepth = 0.10f;
        const float horizontalPadding = 0.12f;
        var castDistance = MathF.Max(
            0f,
            frontDepth - corridorStart - castBoxHalfDepth * 2f);
        if (castDistance <= 0f)
        {
            return true;
        }

        var castCenterForward = corridorStart + castBoxHalfDepth;
        var castCenter = origin + new Vec3(
            forward.x * castCenterForward,
            forward.y * castCenterForward,
            0f);
        var paddedHalfWidth = halfWidth + horizontalPadding;
        var halfExtentX =
            MathF.Abs(right.x) * paddedHalfWidth +
            MathF.Abs(forward.x) * castBoxHalfDepth;
        var halfExtentY =
            MathF.Abs(right.y) * paddedHalfWidth +
            MathF.Abs(forward.y) * castBoxHalfDepth;
        var boxMin = new Vec3(
            castCenter.x - halfExtentX,
            castCenter.y - halfExtentY,
            origin.z + 0.32f);
        var boxMax = new Vec3(
            castCenter.x + halfExtentX,
            castCenter.y + halfExtentY,
            origin.z + MinimumOpenAirClearance);
        var castDirection = new Vec3(forward.x, forward.y, 0f);
        var excludeFlags =
            BodyFlags.CommonFocusRayCastExcludeFlags |
            BodyFlags.Dynamic |
            BodyFlags.DroppedItem |
            BodyFlags.WaterBody |
            BodyFlags.AgentOnly |
            BodyFlags.MissileOnly |
            BodyFlags.StealthBox;
        if (!scene.BoxCast(
                boxMin,
                boxMax,
                castSupportRay: false,
                Vec3.Zero,
                castDirection,
                castDistance,
                out var collisionDistance,
                out var closestPoint,
                out _,
                excludeFlags))
        {
            return true;
        }

        RexLog.Info(
            "Rejected a placement whose ceremony volume intersects fixed scene physics " +
            $"at distance {collisionDistance:0.00}m near " +
            $"({closestPoint.x:0.00}, {closestPoint.y:0.00}, {closestPoint.z:0.00}).");
        return false;
    }

    private static bool HasClearRearCollisionCorridor(
        TaleWorlds.Engine.Scene scene,
        Vec3 origin,
        Vec2 forward,
        Vec2 right,
        float halfWidth)
    {
        var excludeFlags =
            BodyFlags.Dynamic |
            BodyFlags.DroppedItem |
            BodyFlags.WaterBody |
            BodyFlags.AgentOnly |
            BodyFlags.MissileOnly |
            BodyFlags.StealthBox;
        var lateralOffsets = new[] { -halfWidth, 0f, halfWidth };
        foreach (var lateralOffset in lateralOffsets)
        {
            var stripOrigin = origin + new Vec3(
                right.x * lateralOffset,
                right.y * lateralOffset,
                0f);
            var stripRear = stripOrigin - new Vec3(
                forward.x * GallowsRearClearanceDepth,
                forward.y * GallowsRearClearanceDepth,
                0f);
            var directDistance = stripOrigin.AsVec2.Distance(stripRear.AsVec2);
            var lowerStart = stripOrigin + new Vec3(0f, 0f, 1.10f);
            var lowerEnd = stripRear + new Vec3(0f, 0f, 1.10f);
            var upperStart = stripOrigin + new Vec3(0f, 0f, 1.75f);
            var upperEnd = stripRear + new Vec3(0f, 0f, 1.75f);
            if (scene.RayCastForClosestEntityOrTerrainFixedPhysics(
                    lowerStart,
                    lowerEnd,
                    out var lowerCollisionDistance,
                    out var lowerClosestPoint,
                    out _,
                    0.10f,
                    excludeFlags) &&
                lowerCollisionDistance < directDistance * 0.92f)
            {
                RexLog.Info(
                    "Rejected a placement whose rear ceremony corridor intersects fixed scene physics " +
                    $"near ({lowerClosestPoint.x:0.00}, {lowerClosestPoint.y:0.00}, {lowerClosestPoint.z:0.00}).");
                return false;
            }

            if (scene.RayCastForClosestEntityOrTerrainFixedPhysics(
                    upperStart,
                    upperEnd,
                    out var upperCollisionDistance,
                    out var upperClosestPoint,
                    out _,
                    0.10f,
                    excludeFlags) &&
                upperCollisionDistance < directDistance * 0.92f)
            {
                RexLog.Info(
                    "Rejected a placement whose rear ceremony corridor intersects fixed scene physics " +
                    $"near ({upperClosestPoint.x:0.00}, {upperClosestPoint.y:0.00}, {upperClosestPoint.z:0.00}).");
                return false;
            }
        }

        return true;
    }

    private static List<VisualObstacleBounds> CaptureVisibleObstacleBounds(
        TaleWorlds.Engine.Scene scene)
    {
        var sceneEntities = new List<GameEntity>();
        scene.GetEntities(ref sceneEntities);
        var result = new List<VisualObstacleBounds>(sceneEntities.Count);
        var visited = new HashSet<GameEntity>();
        var entityBoundsCount = 0;
        var componentBoundsCount = 0;
        foreach (var entity in sceneEntities)
        {
            CaptureVisibleObstacleBoundsRecursive(
                entity,
                visited,
                result,
                ref entityBoundsCount,
                ref componentBoundsCount);
        }

        RexLog.Info(
            $"Placement solver captured {result.Count} visible obstacle bounds " +
            $"({entityBoundsCount} entity, {componentBoundsCount} component) " +
            $"from {visited.Count} scene entities.");
        return result;
    }

    private static void CaptureVisibleObstacleBoundsRecursive(
        GameEntity? entity,
        ISet<GameEntity> visited,
        ICollection<VisualObstacleBounds> result,
        ref int entityBoundsCount,
        ref int componentBoundsCount)
    {
        if (entity is null || !visited.Add(entity))
        {
            return;
        }

        var childCount = 0;
        try
        {
            childCount = entity.ChildCount;
            if (entity.IsVisibleIncludeParents())
            {
                var name = !string.IsNullOrWhiteSpace(entity.Name)
                    ? entity.Name
                    : entity.GetPrefabName();
                name = string.IsNullOrWhiteSpace(name) ? "<unnamed>" : name;

                var min = entity.GlobalBoxMin;
                var max = entity.GlobalBoxMax;
                if (TryAddVisualObstacleBounds(name, min, max, result))
                {
                    entityBoundsCount++;
                }

                // Valid entity boxes can still omit prefab children or contain
                // stale bounds in modified town scenes. Always inspect render
                // components instead of trusting the aggregate box alone.
                componentBoundsCount += CaptureComponentObstacleBounds(
                    entity,
                    name,
                    result);
            }
        }
        catch (Exception exception)
        {
            // Third-party scenes can expose unloaded or malformed helper
            // entities. Ignore only that entity and retain every usable bound
            // collected from the rest of the scene hierarchy.
            RexLog.Error(
                "A scene entity did not expose a usable visual bound during placement sampling.",
                exception);
        }

        for (var index = 0; index < childCount; index++)
        {
            try
            {
                CaptureVisibleObstacleBoundsRecursive(
                    entity.GetChild(index),
                    visited,
                    result,
                    ref entityBoundsCount,
                    ref componentBoundsCount);
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    "A scene entity child could not be inspected during placement sampling.",
                    exception);
            }
        }
    }

    private static int CaptureComponentObstacleBounds(
        GameEntity entity,
        string entityName,
        ICollection<VisualObstacleBounds> result)
    {
        var added = 0;
        var entityFrame = entity.GetGlobalFrame();
        for (var componentIndex = 0;
             componentIndex < entity.MultiMeshComponentCount;
             componentIndex++)
        {
            try
            {
                var metaMesh = entity.GetMetaMesh(componentIndex);
                if (metaMesh is null || !metaMesh.IsValid)
                {
                    continue;
                }

                var componentFrame = entityFrame.TransformToParent(metaMesh.Frame);
                var componentBox = metaMesh.GetBoundingBox();
                TransformBoundsToWorld(
                    componentFrame,
                    componentBox.min,
                    componentBox.max,
                    out var componentMin,
                    out var componentMax);
                if (TryAddVisualObstacleBounds(
                        $"{entityName}/metamesh[{componentIndex}]",
                        componentMin,
                        componentMax,
                        result))
                {
                    added++;
                    continue;
                }

                // Some town environments batch many props into one oversized
                // meta mesh. Fall through to its individual render meshes so a
                // market roof or shed does not disappear with the city-sized
                // parent bound.
                for (var meshIndex = 0; meshIndex < metaMesh.MeshCount; meshIndex++)
                {
                    var mesh = metaMesh.GetMeshAtIndex(meshIndex);
                    if (mesh is null)
                    {
                        continue;
                    }

                    var meshFrame = componentFrame.TransformToParent(mesh.GetLocalFrame());
                    TransformBoundsToWorld(
                        meshFrame,
                        mesh.GetBoundingBoxMin(),
                        mesh.GetBoundingBoxMax(),
                        out var meshMin,
                        out var meshMax);
                    var meshName = string.IsNullOrWhiteSpace(mesh.Name)
                        ? $"mesh[{meshIndex}]"
                        : mesh.Name;
                    if (TryAddVisualObstacleBounds(
                            $"{entityName}/{meshName}",
                            meshMin,
                            meshMax,
                            result))
                    {
                        added++;
                    }
                }
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"A render component on scene entity '{entityName}' could not be inspected.",
                    exception);
            }
        }

        if (added == 0)
        {
            try
            {
                // A number of scene props expose a direct Mesh instead of a
                // MetaMesh component. GetFirstMesh is the only public 1.4.7
                // path to that render bound.
                var mesh = entity.GetFirstMesh();
                if (mesh is not null)
                {
                    var meshFrame = entityFrame.TransformToParent(mesh.GetLocalFrame());
                    TransformBoundsToWorld(
                        meshFrame,
                        mesh.GetBoundingBoxMin(),
                        mesh.GetBoundingBoxMax(),
                        out var meshMin,
                        out var meshMax);
                    var meshName = string.IsNullOrWhiteSpace(mesh.Name)
                        ? "first-mesh"
                        : mesh.Name;
                    if (TryAddVisualObstacleBounds(
                            $"{entityName}/{meshName}",
                            meshMin,
                            meshMax,
                            result))
                    {
                        added++;
                    }
                }
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"The direct render mesh on scene entity '{entityName}' could not be inspected.",
                    exception);
            }
        }

        return added;
    }

    private static bool TryAddVisualObstacleBounds(
        string name,
        Vec3 min,
        Vec3 max,
        ICollection<VisualObstacleBounds> result)
    {
        if (!min.IsValid ||
            !max.IsValid ||
            max.x <= min.x ||
            max.y <= min.y ||
            max.z <= min.z)
        {
            return false;
        }

        var extentX = max.x - min.x;
        var extentY = max.y - min.y;
        var extentZ = max.z - min.z;
        if ((extentX < 0.20f && extentY < 0.20f && extentZ < 0.20f) ||
            extentX > 80f ||
            extentY > 80f ||
            extentZ > 80f)
        {
            // Ignore point-like helpers and whole-scene/container bounds. The
            // caller decomposes oversized visible containers into components.
            return false;
        }

        result.Add(new VisualObstacleBounds(name, min, max));
        return true;
    }

    private static void TransformBoundsToWorld(
        MatrixFrame frame,
        Vec3 localMin,
        Vec3 localMax,
        out Vec3 worldMin,
        out Vec3 worldMax)
    {
        worldMin = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
        worldMax = new Vec3(float.MinValue, float.MinValue, float.MinValue);
        for (var xIndex = 0; xIndex < 2; xIndex++)
        {
            for (var yIndex = 0; yIndex < 2; yIndex++)
            {
                for (var zIndex = 0; zIndex < 2; zIndex++)
                {
                    var local = new Vec3(
                        xIndex == 0 ? localMin.x : localMax.x,
                        yIndex == 0 ? localMin.y : localMax.y,
                        zIndex == 0 ? localMin.z : localMax.z);
                    var world = frame.TransformToParent(local);
                    worldMin.x = MathF.Min(worldMin.x, world.x);
                    worldMin.y = MathF.Min(worldMin.y, world.y);
                    worldMin.z = MathF.Min(worldMin.z, world.z);
                    worldMax.x = MathF.Max(worldMax.x, world.x);
                    worldMax.y = MathF.Max(worldMax.y, world.y);
                    worldMax.z = MathF.Max(worldMax.z, world.z);
                }
            }
        }
    }

    private static bool TryFindVisibleObstacleInCeremonyFront(
        Vec3 origin,
        Vec2 forward,
        Vec2 right,
        float frontDepth,
        float halfWidth,
        IReadOnlyList<VisualObstacleBounds> visualObstacles,
        out string blockingEntity)
    {
        var corridorStart = -GallowsRearClearanceDepth;
        const float horizontalPadding = 0.12f;
        var minimumVisibleHeight = origin.z + 0.32f;
        var maximumVisibleHeight = origin.z + MinimumOpenAirClearance;
        var corridorLength = MathF.Max(0f, frontDepth - corridorStart);
        var corridorHalfDepth = corridorLength * 0.5f;
        var corridorCenterForward = corridorStart + corridorHalfDepth;
        var corridorCenterX = origin.x + forward.x * corridorCenterForward;
        var corridorCenterY = origin.y + forward.y * corridorCenterForward;
        foreach (var obstacle in visualObstacles)
        {
            if (obstacle.Max.z < minimumVisibleHeight ||
                obstacle.Min.z > maximumVisibleHeight)
            {
                continue;
            }

            if (IntersectsCeremonyFrontFootprint(
                    obstacle,
                    corridorCenterX,
                    corridorCenterY,
                    forward,
                    right,
                    corridorHalfDepth,
                    halfWidth,
                    horizontalPadding))
            {
                blockingEntity =
                    $"{obstacle.Name} " +
                    $"[min=({obstacle.Min.x:0.00}, {obstacle.Min.y:0.00}, {obstacle.Min.z:0.00}), " +
                    $"max=({obstacle.Max.x:0.00}, {obstacle.Max.y:0.00}, {obstacle.Max.z:0.00})]";
                return true;
            }
        }

        blockingEntity = string.Empty;
        return false;
    }

    private static bool IntersectsCeremonyFrontFootprint(
        VisualObstacleBounds obstacle,
        float corridorCenterX,
        float corridorCenterY,
        Vec2 forward,
        Vec2 right,
        float corridorHalfDepth,
        float corridorHalfWidth,
        float horizontalPadding)
    {
        var obstacleHalfX = (obstacle.Max.x - obstacle.Min.x) * 0.5f + horizontalPadding;
        var obstacleHalfY = (obstacle.Max.y - obstacle.Min.y) * 0.5f + horizontalPadding;
        var obstacleCenterX = (obstacle.Min.x + obstacle.Max.x) * 0.5f;
        var obstacleCenterY = (obstacle.Min.y + obstacle.Max.y) * 0.5f;
        var deltaX = obstacleCenterX - corridorCenterX;
        var deltaY = obstacleCenterY - corridorCenterY;

        // Exact 2D separating-axis test between the world-axis-aligned visual
        // bound and the oriented ceremony-front rectangle. This cannot skip a
        // thin post or awning between the old discrete probe points.
        var corridorRadiusOnWorldX =
            MathF.Abs(right.x) * corridorHalfWidth +
            MathF.Abs(forward.x) * corridorHalfDepth;
        if (MathF.Abs(deltaX) > obstacleHalfX + corridorRadiusOnWorldX)
        {
            return false;
        }

        var corridorRadiusOnWorldY =
            MathF.Abs(right.y) * corridorHalfWidth +
            MathF.Abs(forward.y) * corridorHalfDepth;
        if (MathF.Abs(deltaY) > obstacleHalfY + corridorRadiusOnWorldY)
        {
            return false;
        }

        var centerDistanceOnRight = MathF.Abs(deltaX * right.x + deltaY * right.y);
        var obstacleRadiusOnRight =
            MathF.Abs(right.x) * obstacleHalfX +
            MathF.Abs(right.y) * obstacleHalfY;
        if (centerDistanceOnRight > corridorHalfWidth + obstacleRadiusOnRight)
        {
            return false;
        }

        var centerDistanceOnForward = MathF.Abs(
            deltaX * forward.x + deltaY * forward.y);
        var obstacleRadiusOnForward =
            MathF.Abs(forward.x) * obstacleHalfX +
            MathF.Abs(forward.y) * obstacleHalfY;
        return centerDistanceOnForward <= corridorHalfDepth + obstacleRadiusOnForward;
    }

    private static bool IsAreaSafe(
        Mission mission,
        Agent main,
        Vec3 center,
        float radius,
        float allowedHeightDelta)
    {
        if (!TrySnapToGround(mission.Scene, center, out var groundedCenter) ||
            !HasAcceptableLocalSlope(
                mission.Scene,
                groundedCenter,
                allowedHeightDelta))
        {
            return false;
        }

        if (!HasVerticalClearance(mission.Scene, groundedCenter))
        {
            return false;
        }

        // Sample both the middle and outer rings. Checking only the perimeter
        // misses market stalls, wells and other obstacles inside the footprint.
        var rings = new[] { radius * 0.5f, radius };
        foreach (var ringRadius in rings)
        {
            for (var index = 0; index < 12; index++)
            {
                var angle = index * (MathF.PI * 2f / 12f);
                var direction = new Vec2(MathF.Cos(angle), MathF.Sin(angle));
                var sample = groundedCenter +
                             new Vec3(direction.x * ringRadius, direction.y * ringRadius, 2f);
                if (!TrySnapToGround(mission.Scene, sample, out var sampleGround) ||
                    MathF.Abs(sampleGround.z - groundedCenter.z) > allowedHeightDelta ||
                    !HasAcceptableLocalSlope(
                        mission.Scene,
                        sampleGround,
                        allowedHeightDelta) ||
                    !HasVerticalClearance(mission.Scene, sampleGround) ||
                    !mission.IsPositionInsideBoundaries(sampleGround.AsVec2) ||
                    mission.Scene.GetNavigationMeshForPosition(in sampleGround) == UIntPtr.Zero ||
                    mission.IsPositionOnAnyBlockerNavMeshFace(sampleGround))
                {
                    return false;
                }

                var rayStart = groundedCenter + new Vec3(0f, 0f, 1.15f);
                var rayEnd = sampleGround + new Vec3(0f, 0f, 1.15f);
                if (mission.Scene.RayCastForClosestEntityOrTerrain(
                        rayStart,
                        rayEnd,
                        out var collisionDistance,
                        out _,
                        out _,
                        0.08f) &&
                    collisionDistance < ringRadius * 0.88f)
                {
                    return false;
                }
            }
        }

        var agentClearanceSquared = radius * radius;
        if (mission.Agents.Any(agent =>
                agent != main &&
                agent.IsActive() &&
                agent.Position.DistanceSquared(groundedCenter) < agentClearanceSquared))
        {
            return false;
        }

        return true;
    }

    private static bool TrySnapToGround(
        TaleWorlds.Engine.Scene scene,
        Vec3 position,
        out Vec3 grounded)
    {
        var probe = position;
        probe.z += 4f;
        // Bannerlord 1.4.7 does not populate the normal returned by the
        // GetGroundHeightAtPosition overload. Slope is derived from nearby
        // heights by HasAcceptableLocalSlope instead.
        var height = scene.GetGroundHeightAtPosition(probe);
        if (float.IsNaN(height) || float.IsInfinity(height) || height < -10000f)
        {
            grounded = Vec3.Invalid;
            return false;
        }

        grounded = new Vec3(position.x, position.y, height + 0.04f);
        return grounded.IsValid;
    }

    private static bool HasAcceptableLocalSlope(
        TaleWorlds.Engine.Scene scene,
        Vec3 center,
        float allowedHeightDelta)
    {
        const float probeDistance = 0.45f;
        var localHeightDelta = MathF.Max(0.12f, allowedHeightDelta * 0.55f);
        var directions = new[]
        {
            new Vec2(1f, 0f),
            new Vec2(-1f, 0f),
            new Vec2(0f, 1f),
            new Vec2(0f, -1f)
        };

        foreach (var direction in directions)
        {
            var probe = center + new Vec3(
                direction.x * probeDistance,
                direction.y * probeDistance,
                1f);
            if (!TrySnapToGround(scene, probe, out var nearby) ||
                MathF.Abs(nearby.z - center.z) > localHeightDelta)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasVerticalClearance(TaleWorlds.Engine.Scene scene, Vec3 ground)
    {
        return HasVerticalClearance(scene, ground, MinimumOpenAirClearance);
    }

    private static bool HasVerticalClearance(
        TaleWorlds.Engine.Scene scene,
        Vec3 ground,
        float clearanceHeight)
    {
        var start = ground + new Vec3(0f, 0f, 0.18f);
        var end = ground + new Vec3(0f, 0f, clearanceHeight);
        return !scene.RayCastForClosestEntityOrTerrain(
            start,
            end,
            out _,
            out _,
            out _,
            0.16f);
    }
}
