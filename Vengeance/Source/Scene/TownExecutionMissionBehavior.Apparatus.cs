using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private void MaintainFrozenVictimFacialSuppression()
    {
        if (!_hangingSuspensionActive || _victimAgent is null || !_victimAgent.IsActive())
        {
            return;
        }

        try
        {
            // Clearing High alone lets Bannerlord's idle voice/facial system
            // re-queue do_blink. Keep High occupied by the native non-blinking
            // sleep face while clearing the other priorities every tick.
            _victimAgent.SetAgentFacialAnimation(
                Agent.FacialAnimChannel.High,
                FrozenVictimClosedEyesFacialAction,
                true);
            _victimAgent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, string.Empty, false);
            _victimAgent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Low, string.Empty, false);
            if (!_frozenVictimFacialSuppressionLogged)
            {
                _frozenVictimFacialSuppressionLogged = true;
                RexLog.Info(
                    $"Session {Request.SessionId} is pinning facial High to " +
                    $"'{FrozenVictimClosedEyesFacialAction}' and clearing Mid/Low every tick " +
                    $"for frozen prisoner agent {_victimAgent.Index}.");
            }
        }
        catch (Exception exception)
        {
            if (_frozenVictimFacialSuppressionFailureLogged)
            {
                return;
            }

            _frozenVictimFacialSuppressionFailureLogged = true;
            RexLog.Warning(
                $"Could not suppress the frozen prisoner's facial animation; " +
                $"the execution scene remains active. {exception.Message}");
        }
    }

    private static float SmoothStep(float t)
    {
        var clamped = MathF.Max(0f, MathF.Min(1f, t));
        return clamped * clamped * (3f - 2f * clamped);
    }

    private bool TryConfigureGallowsFullCollisionAndBounds(
        GameEntity gallows,
        out BoundingBox deckBounds,
        out BoundingBox stairsBounds)
    {
        deckBounds = default;
        stairsBounds = default;
        // Restore the first proven gallows setup: disable the prefab's broad
        // compound collision tree, then re-enable only the authored walkable
        // deck and stair bodies. No substitute ramp or support gate participates.
        if (!TryDisableEntityCollision(gallows, "full native gallows"))
        {
            return false;
        }

        var hasDeck = false;
        var hasDeckCover = false;
        var hasStairs = false;
        var enabledStructuralBeamCount = 0;
        foreach (var part in gallows.GetEntityAndChildren())
        {
            var partName = part.Name;
            var isDeck = string.Equals(
                partName,
                GallowsDeckPartName,
                StringComparison.OrdinalIgnoreCase);
            var isStairs = string.Equals(
                partName,
                GallowsStairsPartName,
                StringComparison.OrdinalIgnoreCase);
            var isDeckCover = string.Equals(
                partName,
                GallowsDeckCoverPartName,
                StringComparison.OrdinalIgnoreCase);
            var isStructuralBeam = string.Equals(
                partName,
                GallowsStructuralBeamPartName,
                StringComparison.OrdinalIgnoreCase);
            if (!isDeck && !isDeckCover && !isStairs && !isStructuralBeam)
            {
                continue;
            }

            if (isStructuralBeam)
            {
                if (TryEnableGallowsWalkableCollision(part, "gallows structural beam"))
                {
                    enabledStructuralBeamCount++;
                }
                else
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} could not enable one " +
                        $"'{GallowsStructuralBeamPartName}' beam; the ceremony continues for diagnosis.");
                }

                continue;
            }

            if (isDeckCover)
            {
                // This raised plank sits under the execution action root. The
                // v0.3.25 minimal collision restore omitted it, so the Agent
                // capsule fell through to the lower deck while the visible
                // model stayed on the plank.
                if (TryEnableGallowsWalkableCollision(part, "gallows execution plank"))
                {
                    hasDeckCover = true;
                }
                else
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} could not enable collision on " +
                        $"'{GallowsDeckCoverPartName}'; continuing with the native deck and stairs.");
                }

                continue;
            }

            if (!TryEnableGallowsWalkableCollision(
                    part,
                    isDeck ? "gallows deck" : "gallows stairs") ||
                !TryAccumulateBounds(
                    part.GetGlobalBoundingBox(),
                    ref deckBounds,
                    ref hasDeck,
                    isDeck) ||
                !TryAccumulateBounds(
                    part.GetGlobalBoundingBox(),
                    ref stairsBounds,
                    ref hasStairs,
                    isStairs))
            {
                return false;
            }
        }

        var configured = hasDeck && hasStairs &&
                         IsFiniteBounds(deckBounds) &&
                         IsFiniteBounds(stairsBounds);
        if (configured)
        {
            if (!hasDeckCover)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} found no enabled '{GallowsDeckCoverPartName}' " +
                    "execution plank; the ceremony continues for diagnosis.");
            }


            if (enabledStructuralBeamCount == 0)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} found no '{GallowsStructuralBeamPartName}' " +
                    "structural beam collision to restore; the ceremony continues for diagnosis.");
            }

            RexLog.Info(
                $"Session {Request.SessionId} restored the original gallows collision contract: " +
                $"'{GallowsDeckPartName}', '{GallowsDeckCoverPartName}' " +
                $"(enabled={hasDeckCover}), '{GallowsStairsPartName}' and " +
                $"{enabledStructuralBeamCount} '{GallowsStructuralBeamPartName}' beams remain solid; " +
                "no hidden ramp or stair support gate was created.");
        }

        return configured;
    }

    private static bool TryEnableGallowsWalkableCollision(GameEntity part, string purpose)
    {
        try
        {
            part.RemoveBodyFlags(BodyFlags.Disabled, applyToChildren: false);
            part.SetPhysicsState(isEnabled: true, setChildren: false);
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not restore collision for {purpose}.", exception);
            return false;
        }
    }

    private void ConfigureGallowsStairWalkway(
        GameEntity stairsEntity,
        BoundingBox stairsBounds,
        BoundingBox deckBounds)
    {
        if (TryReplaceGallowsStairCollisionWithSmoothRamp(
                stairsEntity,
                stairsBounds,
                deckBounds))
        {
            return;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} could not instantiate the hidden stair walkway; " +
            $"the authored stepped '{GallowsStairsPartName}' collision remains because no replacement " +
            "fixed body exists. The execution continues and the failure is diagnostic only.");
    }

    private static bool TryDisableGallowsStairCorridorObstructions(
        GameEntity gallows,
        BoundingBox stairsBounds)
    {
        var disabledNames = new List<string>();
        var disabledEntityCount = 0;
        foreach (var part in gallows.GetEntityAndChildren())
        {
            var partName = part.Name;
            // The deck is load-bearing: the top of the stairs hands the player
            // over to it. Disabling wooden_platform_c leaves the stair head
            // unsupported, so it can never be treated as a stair obstruction
            // even though its bounds legitimately touch the corridor.
            if (ReferenceEquals(part, gallows) ||
                string.Equals(partName, GallowsDeckPartName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(partName, GallowsDeckCoverPartName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(partName, GallowsStairsPartName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(partName, GallowsStructuralBeamPartName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            BoundingBox partBounds;
            try
            {
                partBounds = part.GetGlobalBoundingBox();
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"Could not inspect gallows child '{partName}' near the stair corridor.",
                    exception);
                return false;
            }

            if (!IsFiniteBounds(partBounds) ||
                !BoundsOverlapExpandedStairCorridor(partBounds, stairsBounds))
            {
                continue;
            }

            if (!TryDisableEntityCollisionOnly(
                    part,
                    $"gallows stair-corridor child '{partName}'"))
            {
                return false;
            }

            disabledEntityCount++;
            disabledNames.Add(string.IsNullOrWhiteSpace(partName) ? "<unnamed>" : partName);
        }

        var distinctNames = disabledNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        RexLog.Info(
            disabledEntityCount == 0
                ? "The native gallows stair corridor contained no overlapping non-walkable collision body."
                : $"Disabled {disabledEntityCount} non-walkable gallows collision entity/entities " +
                  $"({distinctNames.Count} distinct name(s)) overlapping the stair corridor: " +
                  string.Join(", ", distinctNames) + ".");
        return true;
    }

    private static bool BoundsOverlapExpandedStairCorridor(
        BoundingBox partBounds,
        BoundingBox stairsBounds)
    {
        return partBounds.max.x >= stairsBounds.min.x - GallowsStairCorridorPadding &&
               partBounds.min.x <= stairsBounds.max.x + GallowsStairCorridorPadding &&
               partBounds.max.y >= stairsBounds.min.y - GallowsStairCorridorPadding &&
               partBounds.min.y <= stairsBounds.max.y + GallowsStairCorridorPadding &&
               partBounds.max.z >= stairsBounds.min.z - GallowsStairCorridorPadding &&
               partBounds.min.z <= stairsBounds.max.z + GallowsStairCorridorPadding;
    }

    private static bool TryEnableCompleteGallowsCollision(GameEntity gallows)
    {
        try
        {
            gallows.RemoveBodyFlags(BodyFlags.Disabled, applyToChildren: true);
            gallows.SetPhysicsState(isEnabled: true, setChildren: true);

            if (!gallows.GetEntityAndChildren().Any(
                    part => string.Equals(
                        part.Name,
                        GallowsStairsPartName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                RexLog.Warning("The native gallows did not expose a wooden_platform_stairs_a child.");
                return false;
            }

            // The prefab root owns a broad compound body in addition to its
            // authored child collision. Disabled flags proved insufficient for
            // the stepped body, so remove the root's own regular/engine physics
            // as well. This call is deliberately non-recursive: deck, stairs,
            // side rails and all child bodies remain authored at this point.
            return TryRemoveEntityPhysicsOnly(
                gallows,
                "native gallows root compound body");
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not enable every native gallows collision body.", exception);
            return false;
        }
    }

    private readonly struct StairSurfaceSample
    {
        public StairSurfaceSample(float runCoordinate, float height)
        {
            RunCoordinate = runCoordinate;
            Height = height;
        }

        public float RunCoordinate { get; }
        public float Height { get; }
    }

    private bool TryReplaceGallowsStairCollisionWithSmoothRamp(
        GameEntity stairs,
        BoundingBox stairsBounds,
        BoundingBox deckBounds)
    {
        GameEntity? ramp = null;
        var stairCollisionMutationAttempted = false;
        try
        {
            if (_placement is null)
            {
                RexLog.Warning("The hidden stair walkway needs a resolved placement basis.");
                return false;
            }

            var stairsFrame = stairs.GetGlobalFrame();
            var stairsLocalBounds = stairs.GetLocalBoundingBox();
            if (!IsFiniteBounds(stairsLocalBounds))
            {
                RexLog.Warning("The native gallows stairs exposed a non-finite local bounding box.");
                return false;
            }

            var stairsLocalSize = stairsLocalBounds.max - stairsLocalBounds.min;
            RexLog.Info(
                $"Stair walkway input: localSize=({stairsLocalSize.x:0.000}, {stairsLocalSize.y:0.000}, " +
                $"{stairsLocalSize.z:0.000}), worldBounds=[min=({stairsBounds.min.x:0.00}, " +
                $"{stairsBounds.min.y:0.00}, {stairsBounds.min.z:0.00}), max=({stairsBounds.max.x:0.00}, " +
                $"{stairsBounds.max.y:0.00}, {stairsBounds.max.z:0.00})].");

            // Measure the stair footprint in the placement basis using the real
            // oriented box corners. A world AABB alone cannot separate the climb
            // direction from the stair width once the prefab is rotated.
            if (!TryProjectStairFootprint(
                    stairsFrame,
                    stairsLocalBounds,
                    out var runAlongForward,
                    out var runMinimum,
                    out var runMaximum,
                    out var widthCenter,
                    out var stairWidth))
            {
                return false;
            }

            var footprintRun = runMaximum - runMinimum;
            RexLog.Info(
                $"Stair walkway footprint: runAxis={(runAlongForward ? "placementForward" : "placementRight")}, " +
                $"run={footprintRun:0.000}m, width={stairWidth:0.000}m, widthCenter={widthCenter:0.000}m.");
            if (footprintRun < GallowsSmoothRampMinimumRun ||
                stairWidth < GallowsStairMinimumWidth)
            {
                RexLog.Warning(
                    $"The stair footprint was too small for a walkway ramp " +
                    $"(run={footprintRun:0.000}m, width={stairWidth:0.000}m).");
                return false;
            }

            // Sample the authored stepped collision itself. This is the only
            // source that reports the real tread heights; the bounding box only
            // reports the total envelope.
            if (!TrySampleStairSurface(
                    stairs,
                    stairsBounds,
                    runAlongForward,
                    runMinimum,
                    runMaximum,
                    widthCenter,
                    out var samples))
            {
                return false;
            }

            var lowSample = samples[0];
            var highSample = samples[samples.Count - 1];
            var climbsTowardRunMaximum = highSample.Height >= lowSample.Height;
            var bottomSample = climbsTowardRunMaximum ? lowSample : highSample;
            var topSample = climbsTowardRunMaximum ? highSample : lowSample;
            var sampledRun = MathF.Abs(topSample.RunCoordinate - bottomSample.RunCoordinate);
            var sampledRise = topSample.Height - bottomSample.Height;
            if (sampledRun < GallowsSmoothRampMinimumRun ||
                sampledRise < GallowsSmoothRampMinimumRise)
            {
                RexLog.Warning(
                    $"The sampled stair surface did not describe a climbable slope " +
                    $"(run={sampledRun:0.000}m, rise={sampledRise:0.000}m, " +
                    $"minimumRun={GallowsSmoothRampMinimumRun:0.000}m, " +
                    $"minimumRise={GallowsSmoothRampMinimumRise:0.000}m).");
                return false;
            }

            // Extend outside the first visible tread and into the deck. The
            // authored plank is created in its final frame, so its fixed body and
            // render frame cannot diverge as they did after non-uniform scaling.
            var bottomRunCoordinate = climbsTowardRunMaximum ? runMinimum : runMaximum;
            var topRunCoordinate = climbsTowardRunMaximum ? runMaximum : runMinimum;
            var climbRunSign = topRunCoordinate >= bottomRunCoordinate ? 1f : -1f;
            bottomRunCoordinate -= climbRunSign * GallowsSmoothRampGroundLeadIn;
            topRunCoordinate += climbRunSign * GallowsSmoothRampDeckOverlap;
            var bottomHeight = _placement.Origin.z - GallowsSmoothRampSurfaceInset;
            var topHeight = deckBounds.max.z - GallowsSmoothRampSurfaceInset;
            var pitchRadians = MathF.Atan2(
                topHeight - bottomHeight,
                MathF.Abs(topRunCoordinate - bottomRunCoordinate));
            var bottomWorld = ComposePlacementPoint(
                runAlongForward,
                bottomRunCoordinate,
                widthCenter,
                bottomHeight);
            var topWorld = ComposePlacementPoint(
                runAlongForward,
                topRunCoordinate,
                widthCenter,
                topHeight);
            RexLog.Info(
                $"Stair walkway slope: sampledRun={sampledRun:0.000}m, sampledRise={sampledRise:0.000}m, " +
                $"fullRun={footprintRun:0.000}m, extendedRun=" +
                $"{MathF.Abs(topRunCoordinate - bottomRunCoordinate):0.000}m, " +
                $"pitch={pitchRadians * (180f / MathF.PI):0.00} degrees, " +
                $"groundLead={GallowsSmoothRampGroundLeadIn:0.000}m, " +
                $"deckOverlap={GallowsSmoothRampDeckOverlap:0.000}m, " +
                $"bottom=({bottomWorld.x:0.00}, {bottomWorld.y:0.00}, {bottomWorld.z:0.00}), " +
                $"top=({topWorld.x:0.00}, {topWorld.y:0.00}, {topWorld.z:0.00}).");

            ramp = TryCreateNativeGallowsRamp(
                bottomWorld,
                topWorld,
                stairWidth);
            if (ramp is null)
            {
                RexLog.Warning("The authored native fixed-physics stair ramp could not be created.");
                return false;
            }

            // Only mute the stepped body after an authored replacement exists.
            // Never destroy the native body: if Agent-filtered support fails,
            // it can be re-enabled immediately as a load-bearing fallback.
            stairCollisionMutationAttempted = true;
            var stairTreeDisableConfirmed = TryDisableEntityCollisionTree(
                stairs,
                "native stepped gallows stair tree");
            if (!stairTreeDisableConfirmed)
            {
                RexLog.Warning(
                    "The stepped gallows collision could not be disabled cleanly; restoring it and " +
                    "discarding the replacement ramp so the stairs cannot be left unsupported.");
                TryEnableEntityCollisionTree(
                    stairs,
                    "native stepped gallows stair tree fallback after disable failure");
                RemoveGallowsRampEntities();
                return false;
            }

            var supportGridConfirmed = false;
            try
            {
                supportGridConfirmed = HasContinuousNativeRampAgentSupport(
                    ramp,
                    bottomWorld,
                    topWorld,
                    stairsBounds,
                    stairWidth);
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    "The Agent-filtered native-ramp support check threw; restoring authored stairs.",
                    exception);
            }

            if (!supportGridConfirmed)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} rejected the unconfirmed hidden native ramp and " +
                    "restored the original stepped collision. The execution continues; no path leaves both " +
                    "stair bodies disabled.");
                TryEnableEntityCollisionTree(
                    stairs,
                    "native stepped gallows stair tree fallback after Agent support failure");
                RemoveGallowsRampEntities();
                return false;
            }

            RexLog.Info(
                $"Session {Request.SessionId} disabled the stepped '{GallowsStairsPartName}' collision " +
                $"and overlaid an authored hidden '{GallowsSmoothRampPrefab}' fixed body " +
                $"{GallowsSmoothRampSurfaceInset:0.000}m below the visible stair edges at " +
                $"{pitchRadians * (180f / MathF.PI):0.00} degrees " +
                $"(agentSupportConfirmed={supportGridConfirmed}, requestedWidthMarginEachSide=" +
                $"{GallowsSmoothRampWidthMargin:0.000}m).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not replace the gallows stair collision with a smooth ramp.", exception);
            if (stairCollisionMutationAttempted)
            {
                TryEnableEntityCollisionTree(
                    stairs,
                    "native stepped gallows stair tree fallback after ramp setup exception");
            }

            RemoveGallowsRampEntities();
            return false;
        }
    }

    private GameEntity? TryCreateNativeGallowsRamp(
        Vec3 bottomWorld,
        Vec3 topWorld,
        float stairWidth)
    {
        GameEntity? boundsProbe = null;
        try
        {
            var slopeDelta = topWorld - bottomWorld;
            var slopeLength = slopeDelta.Length;
            var horizontalRun = slopeDelta.AsVec2.Length;
            if (!IsFinite(slopeLength) || !IsFinite(horizontalRun) ||
                slopeLength <= GallowsSmoothRampMinimumRun || horizontalRun <= 0.01f)
            {
                return null;
            }

            boundsProbe = SpawnPrefab(
                GallowsSmoothRampPrefab,
                bottomWorld,
                createPhysics: false,
                callScriptCallbacks: false);
            if (boundsProbe is null)
            {
                return null;
            }

            var localBounds = boundsProbe.GetLocalBoundingBox();
            if (!IsFiniteBounds(localBounds))
            {
                throw new InvalidOperationException(
                    $"Native '{GallowsSmoothRampPrefab}' exposed a non-finite local bounding box.");
            }

            RemoveSpawnedEntity(boundsProbe);
            boundsProbe = null;

            var localSize = localBounds.max - localBounds.min;
            var sizes = new[] { localSize.x, localSize.y, localSize.z };
            var lengthAxis = 0;
            var thicknessAxis = 0;
            for (var axis = 1; axis < 3; axis++)
            {
                if (sizes[axis] > sizes[lengthAxis])
                {
                    lengthAxis = axis;
                }

                if (sizes[axis] < sizes[thicknessAxis])
                {
                    thicknessAxis = axis;
                }
            }

            if (lengthAxis == thicknessAxis)
            {
                throw new InvalidOperationException(
                    $"Native '{GallowsSmoothRampPrefab}' did not expose distinct plank axes.");
            }

            var widthAxis = 3 - lengthAxis - thicknessAxis;
            var nativeLength = sizes[lengthAxis];
            var nativeWidth = sizes[widthAxis];
            if (!IsFinite(nativeLength) || !IsFinite(nativeWidth) ||
                nativeLength + 0.02f < slopeLength || nativeWidth <= 0.20f)
            {
                RexLog.Warning(
                    $"Native '{GallowsSmoothRampPrefab}' cannot cover the requested stair line without " +
                    $"non-uniform scaling (nativeLength={nativeLength:0.000}m, " +
                    $"requestedLength={slopeLength:0.000}m, nativeWidth={nativeWidth:0.000}m).");
                return null;
            }

            var slopeDirection = slopeDelta / slopeLength;
            var widthDirection = new Vec3(
                slopeDelta.y / horizontalRun,
                -slopeDelta.x / horizontalRun,
                0f);
            var normalDirection = CrossProduct(widthDirection, slopeDirection);
            var normalLength = normalDirection.Length;
            if (!IsFinite(normalLength) || normalLength <= 0.01f)
            {
                return null;
            }

            normalDirection /= normalLength;
            if (normalDirection.z < 0f)
            {
                normalDirection = -normalDirection;
                widthDirection = -widthDirection;
            }

            var axisVectors = new Vec3[3];
            axisVectors[lengthAxis] = slopeDirection;
            axisVectors[widthAxis] = widthDirection;
            axisVectors[thicknessAxis] = normalDirection;
            var rotation = default(Mat3);
            rotation.s = axisVectors[0];
            rotation.f = axisVectors[1];
            rotation.u = axisVectors[2];

            // Map the high end of the plank's top face to the deck endpoint.
            // The native plank is longer than the requested line, so its spare
            // length extends below and outside the ground entrance instead of
            // protruding above the deck.
            var anchorLocal = (localBounds.min + localBounds.max) * 0.5f;
            SetAxisComponent(
                ref anchorLocal,
                lengthAxis,
                GetAxisComponent(localBounds.max, lengthAxis));
            SetAxisComponent(
                ref anchorLocal,
                thicknessAxis,
                GetAxisComponent(localBounds.max, thicknessAxis));
            var rotatedAnchor = rotation.TransformToParent(in anchorLocal);
            var baseFrame = new MatrixFrame(rotation, topWorld - rotatedAnchor);

            var requestedWidth = stairWidth + GallowsSmoothRampWidthMargin * 2f;
            var laneCount = Math.Max(1, (int)MathF.Ceiling(requestedWidth / nativeWidth));
            var laneCenterSpan = MathF.Max(0f, requestedWidth - nativeWidth);
            GameEntity? firstRamp = null;
            for (var laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                var laneFraction = laneCount == 1
                    ? 0.5f
                    : laneIndex / (float)(laneCount - 1);
                var lateralOffset = (laneFraction - 0.5f) * laneCenterSpan;
                var laneFrame = baseFrame;
                laneFrame.origin += widthDirection * lateralOffset;

                // Instantiate directly in the final frame. No later SetGlobalFrame
                // and no non-uniform scale can separate authored bo_ physics from
                // the entity that owns it.
                var ramp = BannerlordApiCompatibility.InstantiatePrefab(
                    Mission.Scene,
                    GallowsSmoothRampPrefab,
                    createPhysics: true,
                    laneFrame,
                    callScriptCallbacks: false);
                if (ramp is null)
                {
                    throw new InvalidOperationException(
                        $"Native '{GallowsSmoothRampPrefab}' lane {laneIndex + 1} could not be instantiated.");
                }

                _spawnedEntities.Add(ramp);
                _gallowsRampEntities.Add(ramp);
                ramp.SetMobility(GameEntity.Mobility.Stationary);
                ramp.RemoveBodyFlags(
                    BodyFlags.Disabled | BodyFlags.Dynamic | BodyFlags.Moveable,
                    applyToChildren: true);
                ramp.SetPhysicsState(isEnabled: true, setChildren: true);
                ramp.SetVisibilityExcludeParents(false);
                if (!ramp.HasBody() || !ramp.HasStaticPhysicsBody())
                {
                    throw new InvalidOperationException(
                        $"Native '{GallowsSmoothRampPrefab}' lane {laneIndex + 1} did not retain a static body.");
                }

                firstRamp ??= ramp;
            }

            var actualBottom = topWorld - slopeDirection * nativeLength;
            RexLog.Info(
                $"Created authored gallows ramp physics in final frames: prefab={GallowsSmoothRampPrefab}, " +
                $"lanes={laneCount}, nativeSize=({localSize.x:0.000}, {localSize.y:0.000}, " +
                $"{localSize.z:0.000})m, requestedLength={slopeLength:0.000}m, " +
                $"nativeLength={nativeLength:0.000}m, requestedWidth={requestedWidth:0.000}m, " +
                $"coveredWidth={nativeWidth + laneCenterSpan:0.000}m, " +
                $"actualLow=({actualBottom.x:0.00}, {actualBottom.y:0.00}, {actualBottom.z:0.00}), " +
                $"top=({topWorld.x:0.00}, {topWorld.y:0.00}, {topWorld.z:0.00}).");
            return firstRamp;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not create authored native physics for the gallows stairs.", exception);
            if (boundsProbe is not null)
            {
                RemoveSpawnedEntity(boundsProbe);
            }

            RemoveGallowsRampEntities();
            return null;
        }
    }

    private static Vec3 CrossProduct(Vec3 left, Vec3 right) =>
        new(
            left.y * right.z - left.z * right.y,
            left.z * right.x - left.x * right.z,
            left.x * right.y - left.y * right.x);

    private static float GetAxisComponent(Vec3 value, int axis) =>
        axis == 0 ? value.x : axis == 1 ? value.y : value.z;

    private static void SetAxisComponent(ref Vec3 value, int axis, float component)
    {
        if (axis == 0)
        {
            value.x = component;
        }
        else if (axis == 1)
        {
            value.y = component;
        }
        else
        {
            value.z = component;
        }
    }

    private static bool TryRemoveEntityPhysicsTree(GameEntity entity, string purpose)
    {
        var tree = new List<GameEntity> { entity };
        try
        {
            foreach (var child in entity.GetEntityAndChildren())
            {
                if (!tree.Contains(child))
                {
                    tree.Add(child);
                }
            }
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not enumerate the complete entity tree for {purpose}.", exception);
        }

        var bodiesRemaining = new List<string>();
        for (var index = tree.Count - 1; index >= 0; index--)
        {
            var part = tree[index];
            var partName = string.IsNullOrWhiteSpace(part.Name) ? "<unnamed>" : part.Name;
            if (!TryRemoveEntityPhysicsOnly(part, $"{purpose} child '{partName}'"))
            {
                bodiesRemaining.Add(partName);
            }
        }

        if (bodiesRemaining.Count > 0)
        {
            RexLog.Warning(
                $"Removed physics from {tree.Count - bodiesRemaining.Count}/{tree.Count} entity/entities " +
                $"for {purpose}; residual body read-back: " +
                string.Join(", ", bodiesRemaining.Distinct(StringComparer.OrdinalIgnoreCase)) + ".");
            return false;
        }

        RexLog.Info(
            $"Removed physics from all {tree.Count} entity/entities for {purpose} " +
            "while retaining their visible meshes.");
        return true;
    }

    private static bool TryRemoveEntityPhysicsOnly(GameEntity entity, string purpose)
    {
        var apiWarnings = 0;
        try
        {
            entity.AddBodyFlags(BodyFlags.Disabled, applyToChildren: false);
        }
        catch (Exception exception)
        {
            apiWarnings++;
            RexLog.Warning($"Could not disable {purpose} before physics removal: {exception.Message}");
        }

        try
        {
            entity.SetPhysicsState(isEnabled: false, setChildren: false);
        }
        catch (Exception exception)
        {
            apiWarnings++;
            RexLog.Warning($"SetPhysicsState(false) failed for {purpose}: {exception.Message}");
        }

        try
        {
            entity.RemovePhysics(clearingTheScene: false);
        }
        catch (Exception exception)
        {
            apiWarnings++;
            RexLog.Warning($"RemovePhysics(false) failed for {purpose}: {exception.Message}");
        }

        bool bodyRemains;
        try
        {
            bodyRemains = entity.HasBody();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not read back {purpose} after RemovePhysics: {exception.Message}");
            return false;
        }

        var usedEngineFallback = false;
        if (bodyRemains)
        {
            usedEngineFallback = true;
            try
            {
                entity.RemoveEnginePhysics();
            }
            catch (Exception exception)
            {
                apiWarnings++;
                RexLog.Warning($"RemoveEnginePhysics fallback failed for {purpose}: {exception.Message}");
            }

            try
            {
                bodyRemains = entity.HasBody();
            }
            catch (Exception exception)
            {
                RexLog.Warning($"Could not read back {purpose} after engine fallback: {exception.Message}");
                return false;
            }
        }

        if (bodyRemains)
        {
            RexLog.Warning($"{purpose} still owns a physics body after every removal attempt.");
            return false;
        }

        RexLog.Info(
            $"Removed physics from {purpose} without touching child entities " +
            $"(engineFallback={usedEngineFallback}, apiWarnings={apiWarnings}).");
        return true;
    }

    private bool TryProjectStairFootprint(
        MatrixFrame stairsFrame,
        BoundingBox stairsLocalBounds,
        out bool runAlongForward,
        out float runMinimum,
        out float runMaximum,
        out float widthCenter,
        out float stairWidth)
    {
        runAlongForward = true;
        runMinimum = 0f;
        runMaximum = 0f;
        widthCenter = 0f;
        stairWidth = 0f;
        if (_placement is null)
        {
            return false;
        }

        var right = _placement.Right;
        var forward = _placement.Forward;
        var origin = _placement.Origin;
        var rightMinimum = float.MaxValue;
        var rightMaximum = float.MinValue;
        var forwardMinimum = float.MaxValue;
        var forwardMaximum = float.MinValue;
        for (var xIndex = 0; xIndex < 2; xIndex++)
        {
            for (var yIndex = 0; yIndex < 2; yIndex++)
            {
                for (var zIndex = 0; zIndex < 2; zIndex++)
                {
                    var local = new Vec3(
                        xIndex == 0 ? stairsLocalBounds.min.x : stairsLocalBounds.max.x,
                        yIndex == 0 ? stairsLocalBounds.min.y : stairsLocalBounds.max.y,
                        zIndex == 0 ? stairsLocalBounds.min.z : stairsLocalBounds.max.z);
                    var world = stairsFrame.TransformToParent(in local);
                    var deltaX = world.x - origin.x;
                    var deltaY = world.y - origin.y;
                    var rightCoordinate = deltaX * right.x + deltaY * right.y;
                    var forwardCoordinate = deltaX * forward.x + deltaY * forward.y;
                    if (!IsFinite(rightCoordinate) || !IsFinite(forwardCoordinate))
                    {
                        RexLog.Warning(
                            "A stair bounding-box corner projected to a non-finite placement coordinate.");
                        return false;
                    }

                    rightMinimum = MathF.Min(rightMinimum, rightCoordinate);
                    rightMaximum = MathF.Max(rightMaximum, rightCoordinate);
                    forwardMinimum = MathF.Min(forwardMinimum, forwardCoordinate);
                    forwardMaximum = MathF.Max(forwardMaximum, forwardCoordinate);
                }
            }
        }

        var rightExtent = rightMaximum - rightMinimum;
        var forwardExtent = forwardMaximum - forwardMinimum;
        if (!IsFinite(rightExtent) || !IsFinite(forwardExtent) ||
            rightExtent <= 0f || forwardExtent <= 0f)
        {
            RexLog.Warning(
                $"The stair footprint had unusable placement extents " +
                $"(right={rightExtent:0.000}m, forward={forwardExtent:0.000}m).");
            return false;
        }

        // A staircase is longer along the climb than it is wide, so the larger
        // horizontal extent is the run.
        runAlongForward = forwardExtent >= rightExtent;
        runMinimum = runAlongForward ? forwardMinimum : rightMinimum;
        runMaximum = runAlongForward ? forwardMaximum : rightMaximum;
        var widthMinimum = runAlongForward ? rightMinimum : forwardMinimum;
        var widthMaximum = runAlongForward ? rightMaximum : forwardMaximum;
        widthCenter = (widthMinimum + widthMaximum) * 0.5f;
        stairWidth = widthMaximum - widthMinimum;
        return true;
    }

    private Vec3 ComposePlacementPoint(
        bool runAlongForward,
        float runCoordinate,
        float widthCoordinate,
        float height)
    {
        if (_placement is null)
        {
            return Vec3.Invalid;
        }

        var right = _placement.Right;
        var forward = _placement.Forward;
        var origin = _placement.Origin;
        var alongRight = runAlongForward ? widthCoordinate : runCoordinate;
        var alongForward = runAlongForward ? runCoordinate : widthCoordinate;
        return new Vec3(
            origin.x + right.x * alongRight + forward.x * alongForward,
            origin.y + right.y * alongRight + forward.y * alongForward,
            height);
    }

    private bool TrySampleStairSurface(
        GameEntity stairs,
        BoundingBox stairsBounds,
        bool runAlongForward,
        float runMinimum,
        float runMaximum,
        float widthCenter,
        out List<StairSurfaceSample> samples)
    {
        samples = new List<StairSurfaceSample>(GallowsStairSurfaceProbeCount);
        var excludeFlags =
            BodyFlags.Dynamic |
            BodyFlags.DroppedItem |
            BodyFlags.WaterBody |
            BodyFlags.AgentOnly |
            BodyFlags.MissileOnly |
            BodyFlags.StealthBox;
        var runSpan = runMaximum - runMinimum;
        var missedProbes = 0;
        for (var index = 0; index < GallowsStairSurfaceProbeCount; index++)
        {
            var fraction = 0.5f - GallowsStairSurfaceProbeSpan * 0.5f +
                           GallowsStairSurfaceProbeSpan * index /
                           (GallowsStairSurfaceProbeCount - 1f);
            var runCoordinate = runMinimum + runSpan * fraction;
            var probe = ComposePlacementPoint(
                runAlongForward,
                runCoordinate,
                widthCenter,
                0f);
            var start = new Vec3(
                probe.x,
                probe.y,
                stairsBounds.max.z + GallowsStairSurfaceProbeHeight);
            var end = new Vec3(
                probe.x,
                probe.y,
                stairsBounds.min.z - GallowsStairSurfaceProbeDepth);
            if (!Mission.Scene.RayCastForClosestEntityOrTerrainFixedPhysics(
                    start,
                    end,
                    out _,
                    out var closestPoint,
                    out var collidedEntity,
                    0.04f,
                    excludeFlags) ||
                !closestPoint.IsValid ||
                collidedEntity != stairs)
            {
                missedProbes++;
                continue;
            }

            samples.Add(new StairSurfaceSample(runCoordinate, closestPoint.z));
        }

        if (samples.Count < 3)
        {
            RexLog.Warning(
                $"Only {samples.Count} of {GallowsStairSurfaceProbeCount} stair-surface probes hit the " +
                $"authored stepped collision ({missedProbes} missed); the walkway ramp cannot be fitted.");
            return false;
        }

        RexLog.Info(
            $"Stair surface probes: {samples.Count}/{GallowsStairSurfaceProbeCount} hit the stepped body " +
            "at run/height " +
            string.Join(
                ", ",
                samples.Select(sample => $"({sample.RunCoordinate:0.00}, {sample.Height:0.000})")) +
            ".");
        return true;
    }

    private bool HasContinuousNativeRampAgentSupport(
        GameEntity ramp,
        Vec3 bottomWorld,
        Vec3 topWorld,
        BoundingBox stairsBounds,
        float stairWidth)
    {
        _ = stairsBounds;
        var runFractions = new[]
        {
            GallowsSmoothRampEndSampleInset,
            0.5f,
            1f - GallowsSmoothRampEndSampleInset
        };
        var widthFractions = new[] { 0.12f, 0.50f, 0.88f };
        var slopeDelta = topWorld - bottomWorld;
        var horizontalRun = slopeDelta.AsVec2.Length;
        if (!IsFinite(horizontalRun) || horizontalRun <= 0.01f)
        {
            return false;
        }

        var widthDirection = new Vec3(
            slopeDelta.y / horizontalRun,
            -slopeDelta.x / horizontalRun,
            0f);
        var requestedWidth = stairWidth + GallowsSmoothRampWidthMargin * 2f;
        var rampEntities = _gallowsRampEntities.Count > 0
            ? _gallowsRampEntities.ToList()
            : new List<GameEntity> { ramp };
        var allConfirmed = true;

        for (var widthIndex = 0; widthIndex < widthFractions.Length; widthIndex++)
        {
            var lateralOffset = (widthFractions[widthIndex] - 0.5f) * requestedWidth;
            for (var runIndex = 0; runIndex < runFractions.Length; runIndex++)
            {
                var runFraction = runFractions[runIndex];
                var expected = bottomWorld + slopeDelta * runFraction +
                               widthDirection * lateralOffset;

                // First verify that one of the authored plank entities owns a
                // body exactly under this sample, independent of scene filters.
                var entityRayOrigin = expected + new Vec3(0f, 0f, 0.80f);
                var entityRayDirection = new Vec3(0f, 0f, -1f);
                var hitAuthoredRamp = false;
                foreach (var entity in rampEntities)
                {
                    var hitLength = 0f;
                    if (entity.RayHitEntity(
                            entityRayOrigin,
                            entityRayDirection,
                            1.60f,
                            ref hitLength))
                    {
                        hitAuthoredRamp = true;
                        break;
                    }
                }

                var groundProbe = expected + new Vec3(0f, 0f, 0.60f);
                var groundHeight = Mission.Scene.GetGroundHeightAndBodyFlagsAtPosition(
                    groundProbe,
                    out var contactFlags,
                    BodyFlags.CommonCollisionExcludeFlagsForAgent);
                var heightError = MathF.Abs(groundHeight - expected.z);
                var agentSupportConfirmed =
                    hitAuthoredRamp &&
                    IsFinite(groundHeight) &&
                    heightError <= GallowsSmoothRampAgentHeightTolerance &&
                    (contactFlags & BodyFlags.Disabled) == 0;
                if (!agentSupportConfirmed)
                {
                    allConfirmed = false;
                    RexLog.Warning(
                        $"Agent-filtered gallows ramp sample width={widthFractions[widthIndex]:0.00}, " +
                        $"run={runFraction:0.00} failed: authoredRay={hitAuthoredRamp}, " +
                        $"expectedZ={expected.z:0.000}, groundZ={groundHeight:0.000}, " +
                        $"error={heightError:0.000}, flags={contactFlags}.");
                    continue;
                }

                RexLog.Info(
                    $"Agent-filtered gallows ramp sample width={widthFractions[widthIndex]:0.00}, " +
                    $"run={runFraction:0.00} confirmed at Z={groundHeight:0.000} " +
                    $"(error={heightError:0.000}, flags={contactFlags}).");
            }
        }

        if (allConfirmed)
        {
            RexLog.Info(
                $"Session {Request.SessionId} confirmed the authored hidden ramp across the full " +
                "left/centre/right 3x3 Agent-collision grid.");
        }

        return allConfirmed;
    }

    private bool HasContinuousSmoothRampSupport(
        GameEntity ramp,
        Vec3 bottomWorld,
        Vec3 topWorld,
        BoundingBox stairsBounds,
        float stairWidth)
    {
        var runFractions = new[]
        {
            GallowsSmoothRampEndSampleInset,
            0.5f,
            1f - GallowsSmoothRampEndSampleInset
        };
        var widthFractions = new[] { 0.10f, 0.50f, 0.90f };
        var supportHeights = new float[widthFractions.Length, runFractions.Length];
        var expectedHeights = new float[widthFractions.Length, runFractions.Length];
        var hitGrid = new bool[widthFractions.Length, runFractions.Length];
        var allDiagnosticsPassed = true;
        var excludeFlags =
            BodyFlags.Dynamic |
            BodyFlags.DroppedItem |
            BodyFlags.WaterBody |
            BodyFlags.AgentOnly |
            BodyFlags.MissileOnly |
            BodyFlags.StealthBox;
        var slopeDelta = topWorld - bottomWorld;
        var horizontalRun = MathF.Sqrt(
            slopeDelta.x * slopeDelta.x + slopeDelta.y * slopeDelta.y);
        if (!IsFinite(horizontalRun) || horizontalRun <= 0.01f)
        {
            RexLog.Warning("The hidden gallows ramp support grid had no horizontal run.");
            return false;
        }

        var widthDirection = new Vec3(
            slopeDelta.y / horizontalRun,
            -slopeDelta.x / horizontalRun,
            0f);
        var rampEntities = ramp.GetEntityAndChildren().ToList();
        for (var widthIndex = 0; widthIndex < widthFractions.Length; widthIndex++)
        {
            var widthOffset = (widthFractions[widthIndex] - 0.5f) * stairWidth;
            for (var runIndex = 0; runIndex < runFractions.Length; runIndex++)
            {
                var runFraction = runFractions[runIndex];
                var sample = new Vec3(
                    bottomWorld.x + slopeDelta.x * runFraction + widthDirection.x * widthOffset,
                    bottomWorld.y + slopeDelta.y * runFraction + widthDirection.y * widthOffset,
                    bottomWorld.z + slopeDelta.z * runFraction);
                expectedHeights[widthIndex, runIndex] =
                    sample.z - GallowsSmoothRampSurfaceInset;
                var start = new Vec3(
                    sample.x,
                    sample.y,
                    stairsBounds.max.z + GallowsSmoothRampRayHeight);
                var end = new Vec3(
                    sample.x,
                    sample.y,
                    stairsBounds.min.z - GallowsSmoothRampRayDepth);
                var rayHit = Mission.Scene.RayCastForClosestEntityOrTerrainFixedPhysics(
                    start,
                    end,
                    out _,
                    out var closestPoint,
                    out var collidedEntity,
                    0.06f,
                    excludeFlags);
                var hitRamp = rayHit &&
                              closestPoint.IsValid &&
                              rampEntities.Any(entity => collidedEntity == entity);
                if (!hitRamp)
                {
                    allDiagnosticsPassed = false;
                    var collisionName = !rayHit
                        ? "<no fixed-physics hit>"
                        : !collidedEntity.IsValid
                            ? "<terrain>"
                            : string.IsNullOrWhiteSpace(collidedEntity.Name)
                                ? "<unnamed entity>"
                                : collidedEntity.Name;
                    RexLog.Warning(
                        $"Hidden gallows ramp support grid sample width={widthFractions[widthIndex]:0.00}, " +
                        $"run={runFraction:0.00} at ({sample.x:0.00}, {sample.y:0.00}) did not hit " +
                        $"the ramp fixed collision tree; closest={collisionName}. The remaining grid " +
                        "samples will still be collected.");
                    continue;
                }

                hitGrid[widthIndex, runIndex] = true;
                supportHeights[widthIndex, runIndex] = closestPoint.z;
            }
        }

        for (var widthIndex = 0; widthIndex < widthFractions.Length; widthIndex++)
        {
            var laneComplete = true;
            for (var runIndex = 0; runIndex < runFractions.Length; runIndex++)
            {
                laneComplete &= hitGrid[widthIndex, runIndex];
            }

            if (!laneComplete)
            {
                allDiagnosticsPassed = false;
                RexLog.Warning(
                    $"Hidden gallows ramp lane {widthFractions[widthIndex]:0.00} was incomplete " +
                    $"(start/middle/end={hitGrid[widthIndex, 0]}/{hitGrid[widthIndex, 1]}/" +
                    $"{hitGrid[widthIndex, 2]}). This is diagnostic only; the broad ramp remains active.");
                continue;
            }

            var lanePassed = true;
            var totalRise = MathF.Abs(
                supportHeights[widthIndex, 2] - supportHeights[widthIndex, 0]);
            var expectedMiddle =
                (supportHeights[widthIndex, 0] + supportHeights[widthIndex, 2]) * 0.5f;
            var middleError = MathF.Abs(
                supportHeights[widthIndex, 1] - expectedMiddle);
            if (totalRise < GallowsSmoothRampMinimumRise ||
                middleError > GallowsSmoothRampContinuityTolerance)
            {
                allDiagnosticsPassed = false;
                lanePassed = false;
                RexLog.Warning(
                    $"Hidden gallows ramp lane {widthFractions[widthIndex]:0.00} was not continuous " +
                    $"(start={supportHeights[widthIndex, 0]:0.000}, " +
                    $"middle={supportHeights[widthIndex, 1]:0.000}, " +
                    $"end={supportHeights[widthIndex, 2]:0.000}, rise={totalRise:0.000}, " +
                    $"middleError={middleError:0.000}).");
            }

            for (var runIndex = 0; runIndex < runFractions.Length; runIndex++)
            {
                var surfaceError = MathF.Abs(
                    supportHeights[widthIndex, runIndex] -
                    expectedHeights[widthIndex, runIndex]);
                if (surfaceError > GallowsSmoothRampSurfaceMatchTolerance)
                {
                    allDiagnosticsPassed = false;
                    lanePassed = false;
                    RexLog.Warning(
                        $"Hidden gallows ramp grid sample width={widthFractions[widthIndex]:0.00}, " +
                        $"run={runFractions[runIndex]:0.00} sat {surfaceError:0.000}m away from " +
                        $"the expected surface (measured={supportHeights[widthIndex, runIndex]:0.000}, " +
                        $"expected={expectedHeights[widthIndex, runIndex]:0.000}, " +
                        $"tolerance={GallowsSmoothRampSurfaceMatchTolerance:0.000}m).");
                }
            }

            if (lanePassed)
            {
                RexLog.Info(
                    $"Hidden gallows ramp lane {widthFractions[widthIndex]:0.00} support " +
                    $"start/middle/end={supportHeights[widthIndex, 0]:0.000}/" +
                    $"{supportHeights[widthIndex, 1]:0.000}/{supportHeights[widthIndex, 2]:0.000}.");
            }
        }

        if (allDiagnosticsPassed)
        {
            RexLog.Info(
                $"Session {Request.SessionId} confirmed continuous hidden-ramp support across the " +
                "left/centre/right 3x3 fixed-physics grid.");
        }
        else
        {
            RexLog.Warning(
                $"Session {Request.SessionId} completed the full hidden-ramp 3x3 diagnostic with one " +
                "or more warnings; no warning can delete the ramp or restore stepped collision.");
        }

        return allDiagnosticsPassed;
    }

    private static bool TryAccumulateBounds(
        BoundingBox partBounds,
        ref BoundingBox combinedBounds,
        ref bool hasCombinedBounds,
        bool shouldAccumulate)
    {
        if (!shouldAccumulate)
        {
            return true;
        }

        if (!IsFiniteBounds(partBounds) ||
            partBounds.max.x <= partBounds.min.x ||
            partBounds.max.y <= partBounds.min.y ||
            partBounds.max.z <= partBounds.min.z)
        {
            return false;
        }

        if (!hasCombinedBounds)
        {
            combinedBounds = partBounds;
            hasCombinedBounds = true;
            return true;
        }

        combinedBounds.min.x = MathF.Min(combinedBounds.min.x, partBounds.min.x);
        combinedBounds.min.y = MathF.Min(combinedBounds.min.y, partBounds.min.y);
        combinedBounds.min.z = MathF.Min(combinedBounds.min.z, partBounds.min.z);
        combinedBounds.max.x = MathF.Max(combinedBounds.max.x, partBounds.max.x);
        combinedBounds.max.y = MathF.Max(combinedBounds.max.y, partBounds.max.y);
        combinedBounds.max.z = MathF.Max(combinedBounds.max.z, partBounds.max.z);
        return true;
    }

    private bool TryApplyGallowsDeckGeometry(
        BoundingBox deckBounds,
        BoundingBox stairsBounds)
    {
        if (_placement is null || !IsFiniteBounds(deckBounds) || !IsFiniteBounds(stairsBounds))
        {
            return false;
        }

        // The gallows rotates with the outside-gate marker. World-AABB X/Y
        // dimensions swap (and inflate diagonally) as that marker turns, so
        // they cannot identify the authored long deck axis. Measure the same
        // bounds on the placement's right/forward basis instead.
        var deckWorldX = deckBounds.max.x - deckBounds.min.x;
        var deckWorldY = deckBounds.max.y - deckBounds.min.y;
        var deckWidth = GetProjectedBoundsSpan(deckBounds, _placement.Right);
        var deckDepth = GetProjectedBoundsSpan(deckBounds, _placement.Forward);
        var deckSurfaceHeight = deckBounds.max.z - _placement.Origin.z;
        if (!IsFinite(deckWidth) || !IsFinite(deckDepth) || !IsFinite(deckSurfaceHeight) ||
            deckWidth < GallowsMinimumDeckWidth ||
            deckDepth < GallowsMinimumDeckDepth ||
            deckWidth > GallowsMaximumDeckDimension ||
            deckDepth > GallowsMaximumDeckDimension ||
            deckSurfaceHeight < 0.35f || deckSurfaceHeight > 3.5f)
        {
            RexLog.Warning(
                $"Rejected gallows deck geometry: worldAabb={deckWorldX:0.00}x{deckWorldY:0.00}m, " +
                $"placementAxes={deckWidth:0.00}x{deckDepth:0.00}m, " +
                $"surface={deckSurfaceHeight:0.00}m, " +
                $"forward=({_placement.Forward.x:0.000},{_placement.Forward.y:0.000}).");
            return false;
        }

        _stageSurfaceHeight = deckSurfaceHeight + 0.015f;
        _gallowsDeckBoundsMin = deckBounds.min;
        _gallowsDeckBoundsMax = deckBounds.max;
        var deckSideExtent = GetMaximumProjectedExtent(deckBounds, _placement.Right);
        var deckForwardExtent = GetMaximumProjectedExtent(deckBounds, _placement.Forward);
        var stairsForwardExtent = GetMaximumProjectedExtent(stairsBounds, _placement.Forward);
        if (!IsFinite(deckSideExtent) || !IsFinite(deckForwardExtent) ||
            !IsFinite(stairsForwardExtent))
        {
            return false;
        }

        _stageSideExtent = deckSideExtent;
        _stageFrontExtent = MathF.Max(deckForwardExtent, stairsForwardExtent);
        _frontInteractionOffset = MathF.Max(
            GallowsMinimumInteractionForward,
            stairsForwardExtent + 0.95f);
        return true;
    }

    private static float GetProjectedBoundsSpan(BoundingBox bounds, Vec2 axis)
    {
        if (!IsFiniteBounds(bounds) || !axis.IsNonZero())
        {
            return float.NaN;
        }

        var normalized = axis.Normalized();
        var minimum = float.MaxValue;
        var maximum = float.MinValue;
        for (var xIndex = 0; xIndex < 2; xIndex++)
        {
            for (var yIndex = 0; yIndex < 2; yIndex++)
            {
                var x = xIndex == 0 ? bounds.min.x : bounds.max.x;
                var y = yIndex == 0 ? bounds.min.y : bounds.max.y;
                var projection = x * normalized.x + y * normalized.y;
                minimum = MathF.Min(minimum, projection);
                maximum = MathF.Max(maximum, projection);
            }
        }

        return maximum - minimum;
    }

    private float GetMaximumProjectedExtent(BoundingBox bounds, Vec2 axis)
    {
        if (_placement is null || !IsFiniteBounds(bounds))
        {
            return float.NaN;
        }

        var maximum = float.MinValue;
        for (var xIndex = 0; xIndex < 2; xIndex++)
        {
            for (var yIndex = 0; yIndex < 2; yIndex++)
            {
                var point = new Vec3(
                    xIndex == 0 ? bounds.min.x : bounds.max.x,
                    yIndex == 0 ? bounds.min.y : bounds.max.y,
                    _placement.Origin.z);
                var delta = point.AsVec2 - _placement.Origin.AsVec2;
                maximum = MathF.Max(maximum, delta.x * axis.x + delta.y * axis.y);
            }
        }

        return maximum;
    }

    private void ResetGallowsStageState()
    {
        RemoveGallowsRampEntities();
        _stageSurfaceHeight = 0f;
        _stageSideExtent = 0f;
        _stageFrontExtent = 0f;
        _frontInteractionOffset = GallowsMinimumInteractionForward;
        _gallowsDeckBoundsMin = Vec3.Invalid;
        _gallowsDeckBoundsMax = Vec3.Invalid;
    }

    private void RemoveGallowsRampEntities()
    {
        for (var index = _gallowsRampEntities.Count - 1; index >= 0; index--)
        {
            RemoveSpawnedEntity(_gallowsRampEntities[index]);
        }

        _gallowsRampEntities.Clear();
    }

    private void ApplyStageElevationToCeremonyMarks(Vec2 facing)
    {
        if (_placement is null || _victimAgent is null)
        {
            return;
        }

        // Every mark has already been resolved by a downward fixed-physics
        // raycast. Preserve each local plank height instead of flattening all
        // actors to the combined visual bounding-box maximum.
        PoseAgentAt(_victimAgent, _victimPosition, facing);
        if (_executionerAgent is not null)
        {
            PoseAgentAt(
                _executionerAgent,
                _executionerPosition,
                _executionerStandbyDirection);
        }
    }

    private bool HasStageEntityAgentClearance(BoundingBox bounds)
    {
        foreach (var agent in Mission.Agents)
        {
            // The platform is deliberately created underneath the two ceremony
            // actors. They are moved onto its verified top surface immediately
            // after acceptance, so their horizontal footprint is expected here.
            // Everyone else (including the player during an NPC execution) must
            // remain outside the physical platform and stair bounds.
            if (agent is null || !agent.IsActive() ||
                ReferenceEquals(agent, _victimAgent) ||
                ReferenceEquals(agent, _executionerAgent))
            {
                continue;
            }

            var position = agent.Position;
            var xDistance = position.x < bounds.min.x
                ? bounds.min.x - position.x
                : position.x > bounds.max.x
                    ? position.x - bounds.max.x
                    : 0f;
            var yDistance = position.y < bounds.min.y
                ? bounds.min.y - position.y
                : position.y > bounds.max.y
                    ? position.y - bounds.max.y
                    : 0f;
            var required = GetSafeCollisionRadius(agent) + PairedAgentCollisionMargin;
            if (xDistance * xDistance + yDistance * yDistance < required * required)
            {
                RexLog.Warning(
                    $"A stage entity would overlap active agent {agent.Index}; rejecting its physics.");
                return false;
            }
        }

        return true;
    }

    private static bool IsFiniteBounds(BoundingBox bounds) =>
        IsFinite(bounds.min.x) && IsFinite(bounds.min.y) && IsFinite(bounds.min.z) &&
        IsFinite(bounds.max.x) && IsFinite(bounds.max.y) && IsFinite(bounds.max.z) &&
        bounds.max.x >= bounds.min.x &&
        bounds.max.y >= bounds.min.y &&
        bounds.max.z >= bounds.min.z;

    private void TryCreateUsePointMarker()
    {
        if (_placement is null || _visualProfile is null || !_victimPosition.IsValid)
        {
            return;
        }

        GameEntity? host = null;
        try
        {
            // A bare runtime GameEntity with an appended capsule does not enter
            // Bannerlord's native focus/UI pipeline reliably. Use the authored
            // rope entity as a real, visible IVisible host, hide only its meshes,
            // and turn its body into a raycast-only interaction proxy.
            host = SpawnPrefab(
                UsePointMarkerPrefab,
                _victimPosition,
                createPhysics: true,
                callScriptCallbacks: false);
            if (host is null)
            {
                throw new InvalidOperationException(
                    $"The native use-point host '{UsePointMarkerPrefab}' could not be instantiated.");
            }

            host.SetMobility(GameEntity.Mobility.Stationary);
            var configuredBodies = ConfigureUsePointHostBodies(host);
            var hiddenMeshes = HideUsePointHostMeshes(host);
            if (configuredBodies <= 0 || !host.HasBody())
            {
                throw new InvalidOperationException(
                    $"The native use-point host '{UsePointMarkerPrefab}' did not retain an authored body.");
            }

            var capsuleBottom = RequiresExecutionTorch()
                ? BurningUsePointCapsuleBottom
                : UsePointCapsuleBottom;
            var capsuleTop = RequiresExecutionTorch()
                ? BurningUsePointCapsuleTop
                : UsePointCapsuleTop;
            var capsuleRadius = RequiresExecutionTorch()
                ? BurningUsePointCapsuleRadius
                : UsePointCapsuleRadius;
            try
            {
                host.WeakEntity.PushCapsuleShapeToEntityBody(
                    new Vec3(0f, 0f, capsuleBottom),
                    new Vec3(0f, 0f, capsuleTop),
                    capsuleRadius,
                    physicsMaterialName: string.Empty);
            }
            catch (Exception exception)
            {
                // The authored raycast-only body is still a valid focus host.
                // Keep it active and log the loss of the larger aiming volume;
                // never cancel the execution merely because this convenience
                // capsule could not be appended.
                RexLog.Warning(
                    $"Could not append the prisoner-sized focus capsule to '{UsePointMarkerPrefab}'; " +
                    $"the authored raycast body remains active ({exception.Message}).");
            }

            if (!TryConfigureUsePoint(host))
            {
                RemoveSpawnedEntity(host);
                return;
            }

            RexLog.Info(
                $"Session {Request.SessionId} attached the F-key use point to native host " +
                $"'{UsePointMarkerPrefab}' at {_victimPosition} (raycastBodies={configuredBodies}, " +
                $"hiddenMeshes={hiddenMeshes}, capsuleBottom={capsuleBottom:0.00}, " +
                $"capsuleTop={capsuleTop:0.00}, radius={capsuleRadius:0.00}, " +
                $"groundPyre={RequiresExecutionTorch()}).");
        }
        catch (Exception exception)
        {
            _usePoint = null;
            _usePointHost = null;
            RemoveSpawnedEntity(host);
            RexLog.Error(
                "Could not create the native prisoner F-key focus host; direct Agent interaction remains available.",
                exception);
        }
    }

    private static int ConfigureUsePointHostBodies(GameEntity host)
    {
        var configured = 0;
        var flagsToRemove =
            BodyFlags.CommonFocusRayCastExcludeFlags |
            BodyFlags.Ladder |
            BodyFlags.HasSteps |
            BodyFlags.AgentOnly |
            BodyFlags.MissileOnly |
            BodyFlags.BodyOwnerFilter;

        foreach (var entity in host.GetEntityAndChildren())
        {
            if (!entity.HasBody())
            {
                continue;
            }

            entity.RemoveBodyFlags(flagsToRemove, applyToChildren: false);
            entity.AddBodyFlags(
                BodyFlags.OnlyCollideWithRaycast |
                BodyFlags.NotDestructible |
                BodyFlags.BodyOwnerEntity,
                applyToChildren: false);
            entity.SetPhysicsState(isEnabled: true, setChildren: false);
            configured++;
        }

        // UsableMissionObject.IVisible checks entity visibility. Keep the entity
        // visible even though every render component below is masked out.
        host.SetVisibilityExcludeParents(true);
        return configured;
    }

    private static int HideUsePointHostMeshes(GameEntity host)
    {
        var hidden = 0;
        foreach (var entity in host.GetEntityAndChildren())
        {
            for (var index = 0; index < entity.MultiMeshComponentCount; index++)
            {
                var metaMesh = entity.GetMetaMesh(index);
                if (metaMesh is null || !metaMesh.IsValid)
                {
                    continue;
                }

                metaMesh.SetVisibilityMask((VisibilityMaskFlags)0);
                hidden++;
            }
        }

        host.SetVisibilityExcludeParents(true);
        return hidden;
    }

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private void RetryPendingEntityRemovals()
    {
        for (var index = _pendingEntityRemovals.Count - 1; index >= 0; index--)
        {
            RemoveSpawnedEntity(_pendingEntityRemovals[index]);
        }
    }

    private bool RemoveSpawnedEntity(GameEntity? entity)
    {
        if (entity is null)
        {
            return true;
        }

        try
        {
            if (entity.HasScene())
            {
                TryDisableEntityCollision(entity, "temporary execution entity");
                entity.SetVisibilityExcludeParents(false);
                entity.Remove(removeReason: 0);
            }

            // Keep the entity tracked until removal succeeds (or the engine
            // confirms that it has already left the scene) so a transient
            // native failure can be retried by the next tick/final cleanup.
            _spawnedEntities.Remove(entity);
            _pendingEntityRemovals.Remove(entity);
            ReleaseBurningCrucifixTracking(entity);
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "Could not remove a temporary execution entity; it remains tracked for retry.",
                exception);
            if (!_pendingEntityRemovals.Contains(entity))
            {
                _pendingEntityRemovals.Add(entity);
            }

            return false;
        }
    }

    private void ReleaseBurningCrucifixTracking(GameEntity entity)
    {
        if (ReferenceEquals(entity, _burningStakeEntity))
        {
            _burningStakeEntity = null;
            _burningStakeFrameCaptured = false;
        }

        if (ReferenceEquals(entity, _burningCrossbeamEntity))
        {
            _burningCrossbeamEntity = null;
            _burningCrossbeamFrameCaptured = false;
        }

        if (_burningStakeEntity is null && _burningCrossbeamEntity is null)
        {
            _burningCrucifixReferenceVictimPosition = Vec3.Invalid;
            _burningCrucifixSyncLogged = false;
            _burningCrucifixSyncFailureLogged = false;
        }
    }

    private static bool TryDisableEntityCollision(GameEntity entity, string purpose)
    {
        var disabled = false;
        try
        {
            entity.AddBodyFlags(BodyFlags.Disabled, applyToChildren: true);
            disabled = true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not add disabled body flags to {purpose}.", exception);
        }

        try
        {
            entity.SetPhysicsState(isEnabled: false, setChildren: true);
            disabled = true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not disable physics for {purpose}.", exception);
        }

        return disabled;
    }

    /// <summary>
    /// Marks an entity's bodies as disabled so it does not block agents, but
    /// keeps physics state enabled so cloth simulation keeps animating. Used
    /// for the hanging drop rope when a cloth prefab is instantiated.
    /// </summary>
    private static bool DisableCollisionKeepCloth(GameEntity entity, string purpose)
    {
        var disabled = false;
        try
        {
            entity.AddBodyFlags(BodyFlags.Disabled, applyToChildren: true);
            disabled = true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not add disabled body flags to {purpose}.", exception);
        }

        return disabled;
    }

    private static bool TryDisableEntityCollisionTree(GameEntity entity, string purpose)
    {
        List<GameEntity> tree;
        try
        {
            tree = entity.GetEntityAndChildren().ToList();
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not enumerate the complete entity tree for {purpose}.", exception);
            tree = new List<GameEntity> { entity };
        }

        // Apply recursively first, then repeat against every concrete entity.
        // Some nested prefab bodies do not reliably inherit a parent-only
        // SetPhysicsState call even when setChildren is requested.
        var apiAttemptsSucceeded = TryDisableEntityCollision(entity, purpose);
        foreach (var part in tree)
        {
            var partName = string.IsNullOrWhiteSpace(part.Name) ? "<unnamed>" : part.Name;
            apiAttemptsSucceeded &= TryDisableEntityCollisionOnly(
                part,
                $"{purpose} child '{partName}'");
        }

        var unconfirmedNames = new List<string>();
        foreach (var part in tree)
        {
            try
            {
                if ((part.BodyFlag & BodyFlags.Disabled) != BodyFlags.Disabled)
                {
                    unconfirmedNames.Add(
                        string.IsNullOrWhiteSpace(part.Name) ? "<unnamed>" : part.Name);
                }
            }
            catch (Exception exception)
            {
                RexLog.Error($"Could not read back disabled body flags for {purpose}.", exception);
                unconfirmedNames.Add(
                    string.IsNullOrWhiteSpace(part.Name) ? "<unnamed/readback-failed>" : part.Name);
            }
        }

        if (!apiAttemptsSucceeded || unconfirmedNames.Count > 0)
        {
            RexLog.Warning(
                $"Collision-tree disable for {purpose} was not fully confirmed " +
                $"(entities={tree.Count}, apiAttemptsSucceeded={apiAttemptsSucceeded}, " +
                $"unconfirmed={string.Join(", ", unconfirmedNames.Distinct(StringComparer.OrdinalIgnoreCase))}).");
            return false;
        }

        RexLog.Info(
            $"Recursively disabled and read back {tree.Count} collision entity/entities for {purpose}.");
        return true;
    }

    private static bool TryEnableEntityCollisionTree(GameEntity entity, string purpose)
    {
        List<GameEntity> tree;
        try
        {
            tree = entity.GetEntityAndChildren().ToList();
            if (!tree.Contains(entity))
            {
                tree.Insert(0, entity);
            }
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not enumerate the collision tree while restoring {purpose}.", exception);
            tree = new List<GameEntity> { entity };
        }

        var restored = true;
        foreach (var part in tree)
        {
            var partName = string.IsNullOrWhiteSpace(part.Name) ? "<unnamed>" : part.Name;
            try
            {
                part.RemoveBodyFlags(BodyFlags.Disabled, applyToChildren: false);
                part.SetPhysicsState(isEnabled: true, setChildren: false);
                if ((part.BodyFlag & BodyFlags.Disabled) != 0)
                {
                    restored = false;
                    RexLog.Warning(
                        $"Collision restore read-back still showed Disabled for {purpose} child '{partName}'.");
                }
            }
            catch (Exception exception)
            {
                restored = false;
                RexLog.Error(
                    $"Could not restore collision for {purpose} child '{partName}'.",
                    exception);
            }
        }

        if (restored)
        {
            RexLog.Info(
                $"Restored and read back {tree.Count} collision entity/entities for {purpose}.");
        }

        return restored;
    }

    private static bool TryDisableEntityCollisionOnly(GameEntity entity, string purpose)
    {
        var disabled = false;
        try
        {
            entity.AddBodyFlags(BodyFlags.Disabled, applyToChildren: false);
            disabled = true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not add disabled body flags to {purpose}.", exception);
        }

        try
        {
            entity.SetPhysicsState(isEnabled: false, setChildren: false);
            disabled = true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not disable physics for {purpose}.", exception);
        }

        return disabled;
    }

    private bool TryConfigureUsePoint(GameEntity host)
    {
        if (_visualProfile is null)
        {
            return false;
        }

        try
        {
            host.CreateAndAddScriptComponent(typeof(ExecutionUsePoint).Name, callScriptCallbacks: true);
            _usePoint = host.GetFirstScriptOfType<ExecutionUsePoint>() ??
                        throw new InvalidOperationException(
                            "ExecutionUsePoint could not be attached to the runtime prefab.");
            _usePoint.Configure(
                this,
                ExecutionActor.Player,
                new TextObject(_visualProfile.InteractionDescription),
                new TextObject(_visualProfile.InteractionAction));
            _usePointHost = host;
            return true;
        }
        catch (Exception exception)
        {
            _usePoint = null;
            _usePointHost = null;
            RexLog.Error(
                "The prisoner interaction proxy could not be registered; direct Agent interaction remains available.",
                exception);
            return false;
        }
    }

    private bool UpdateMultiStageUsePoint(
        Vec3 position,
        TextObject description,
        TextObject action,
        bool enabled)
    {
        if (_usePointHost is null || _usePoint is null || !position.IsValid)
        {
            RexLog.Error(
                $"Session {Request.SessionId} cannot update the multi-stage interaction proxy; " +
                "its native host or target position is unavailable.");
            return false;
        }

        try
        {
            var frame = _usePointHost.GetGlobalFrame();
            frame.origin = position;
            _usePointHost.SetGlobalFrame(in frame, isTeleportation: true);
            _usePoint.UpdateStage(description, action, enabled);
            RexLog.Info(
                $"Session {Request.SessionId} updated the staged F point at " +
                $"({position.x:0.00}, {position.y:0.00}, {position.z:0.00}); enabled={enabled}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not update the staged execution F point.", exception);
            return false;
        }
    }

    private Agent? SpawnMethodHelper(
        Vec3 position,
        Vec2 direction,
        string role,
        int cultureSlot)
    {
        if (_executionTeam is null || !position.IsValid)
        {
            return null;
        }

        try
        {
            var character = SelectMeleeGuardTroopForCurrentSite(cultureSlot);
            if (character is null)
            {
                RexLog.Error($"No local-culture helper troop was available for {role}.");
                return null;
            }

            var helper = SpawnCharacter(
                character,
                position,
                direction,
                civilianEquipment: false,
                noWeapons: true,
                invulnerable: true,
                teamOverride: _executionTeam,
                joinPlayerFormation: true);
            if (!EnsureGeneratedAllyOnPlayerTeam(helper, role))
            {
                return null;
            }

            helper.DisableScriptedMovement();
            helper.Controller = AgentControllerType.None;
            PoseAgentAt(helper, position, direction);
            var helperPurpose = role + " idle";
            if (TryPickAmbientIdleAction(
                    helper,
                    StoningCrowdIdleActionNames,
                    Math.Abs(cultureSlot) % StoningCrowdIdleActionNames.Length,
                    out var helperIdleAction))
            {
                TryPlayAction(helper, helperIdleAction, helperPurpose, randomStart: true);
            }
            else if (!_ambientIdleUnavailableLogged)
            {
                _ambientIdleUnavailableLogged = true;
                RexLog.Warning(
                    $"No idle in the shared list has a clip in agent {helper.Index}'s action set ({helperPurpose}); " +
                    "the helper will keep its default stance for this session.");
            }
            _methodHelperAgents.Add(helper);
            _occupiedSpawnPositions.Add(position);
            return helper;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not spawn method helper '{role}'.", exception);
            return null;
        }
    }

    private bool MoveAgentToStagePoint(
        Agent agent,
        Vec3 position,
        Vec2 direction,
        string purpose)
    {
        if (agent is null || !agent.IsActive() || !position.IsValid)
        {
            return false;
        }

        try
        {
            agent.Controller = AgentControllerType.AI;
            var world = new WorldPosition(Mission.Scene, position);
            agent.SetScriptedPositionAndDirection(
                ref world,
                direction.RotationInRadians,
                addHumanLikeDelay: false,
                Agent.AIScriptedFrameFlags.NoAttack);
            RexLog.Info($"Agent {agent.Index} began scripted movement for {purpose}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error($"Could not begin scripted movement for {purpose}.", exception);
            return false;
        }
    }

    private bool BeginPlayerStageControl(Vec2 direction, string purpose)
    {
        var player = _playerAgent;
        if (_actor != ExecutionActor.Player || player is null || !player.IsActive())
        {
            return false;
        }

        _playerExecutionActorPosition = player.Position;
        _playerExecutionFacingDirection = direction.IsNonZero()
            ? direction.Normalized()
            : GetFixedDirectionToVictim(player.Position);
        if (!CapturePlayerExecutionFacingLock(player))
        {
            return false;
        }

        // CapturePlayerExecutionFacingLock derives from the current root. The
        // explicit direction is restored afterwards so apparatus controls can
        // face the wheel/winch rather than a moving victim bone.
        if (direction.IsNonZero())
        {
            _playerExecutionFacingDirection = direction.Normalized();
            MaintainPlayerExecutionFacingLock();
        }

        RexLog.Info(
            $"Session {Request.SessionId} captured player root " +
            $"({_playerExecutionActorPosition.x:0.000},{_playerExecutionActorPosition.y:0.000}," +
            $"{_playerExecutionActorPosition.z:0.000}) for {purpose}; no cross-point teleport is used.");
        return true;
    }

    private bool EndPlayerStageControl(string purpose)
    {
        var player = _playerAgent;
        if (_actor != ExecutionActor.Player || player is null)
        {
            return true;
        }

        var cleared = TryClearExecutionActionChannel(player);
        var restored = RestorePlayerExecutionFacingLock();
        if (!cleared || !restored)
        {
            RexLog.Warning(
                $"Player stage control cleanup for {purpose} returned cleared={cleared}, restored={restored}; " +
                "final mission cleanup will retry.");
        }
        return cleared && restored;
    }

    private bool TryGetVictimBoneWorldPosition(sbyte boneIndex, out Vec3 position)
    {
        position = Vec3.Invalid;
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive() || boneIndex < 0)
        {
            return false;
        }

        try
        {
            var local = BannerlordApiCompatibility.GetVictimBoneFrame(victim, boneIndex);
            var world = victim.Frame.TransformToParent(in local);
            position = world.origin;
            return position.IsValid;
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not sample prisoner bone {boneIndex}: {exception.Message}");
            return false;
        }
    }

    private void PlayVictimStageBlood(
        sbyte boneIndex,
        float intensity,
        int worldBurstCount,
        string purpose)
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        try
        {
            victim.CreateBloodBurstAtLimb(boneIndex, MathF.Max(0.1f, intensity));
        }
        catch (Exception exception)
        {
            RexLog.Warning($"The limb blood burst for {purpose} failed: {exception.Message}");
        }

        if (!TryGetVictimBoneWorldPosition(boneIndex, out var position))
        {
            position = victim.GetChestGlobalPosition();
        }

        for (var index = 0; index < Math.Max(0, Math.Min(worldBurstCount, 3)); index++)
        {
            TryPlayWorldParticleBurst(
                "psys_game_blood_sword_enter",
                position + new Vec3(0f, 0f, 0.025f * index),
                purpose);
        }
    }

    private void PlayVictimVoice(SkinVoiceManager.SkinVoiceType voice, string purpose)
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        try
        {
            victim.MakeVoice(
                voice,
                SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
        }
        catch (Exception exception)
        {
            RexLog.Warning($"The prisoner voice for {purpose} failed: {exception.Message}");
        }
    }

    private bool SetEntityBetweenPoints(
        GameEntity entity,
        Vec3 start,
        Vec3 end,
        string purpose)
    {
        if (entity is null || !start.IsValid || !end.IsValid)
        {
            return false;
        }

        try
        {
            var delta = end - start;
            var length = delta.Normalize();
            if (length <= 0.01f)
            {
                return false;
            }

            if (!_entityBaseLengths.TryGetValue(entity, out var nativeLength))
            {
                var bounds = entity.GetGlobalBoundingBox();
                var extent = bounds.max - bounds.min;
                nativeLength = MathF.Max(0.05f, MathF.Max(extent.x, MathF.Max(extent.y, extent.z)));
                _entityBaseLengths[entity] = nativeLength;
            }
            var direction = delta;
            var frame = MatrixFrame.Identity;
            frame.rotation = Mat3.CreateMat3WithForward(in direction);
            frame.rotation.ApplyScaleLocal(new Vec3(1f, length / nativeLength, 1f));
            frame.origin = (start + end) * 0.5f;
            entity.SetGlobalFrame(in frame, isTeleportation: true);
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not stretch entity for {purpose}: {exception.Message}");
            return false;
        }
    }

    private void RemoveConsumedUsePointMarker()
    {
        var marker = _usePointHost;
        if (marker is null)
        {
            return;
        }

        try
        {
        _usePoint?.SetIsDeactivatedSynched(true);
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not deactivate the consumed execution interaction marker.", exception);
        }

        if (RemoveSpawnedEntity(marker))
        {
            _usePoint = null;
            _usePointHost = null;
            RexLog.Info($"Session {Request.SessionId} removed the consumed prisoner F-key proxy before ceremony playback.");
        }
        else
        {
            RexLog.Warning(
                $"Session {Request.SessionId} will retry removal of the consumed prisoner F-key proxy.");
        }
    }

}
