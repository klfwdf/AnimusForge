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
    private bool TryResolveStaticCrowdGroundFallback(Vec3 raw, out Vec3 safe)
    {
        safe = Vec3.Invalid;
        if (_placement is null)
        {
            return false;
        }

        // The downward probe returns the topmost collidable surface. On a town
        // square that surface is frequently a market awning, a tent or a roof
        // rather than the pavement, which used to leave spectators standing on
        // top of the tents after a custom deployment. Only accept surfaces that
        // sit at the ceremony ground level.
        var probe = raw + new Vec3(0f, 0f, 4f);
        var height = Mission.Scene.GetGroundHeightAtPosition(probe);
        if (!IsFinite(height))
        {
            return false;
        }

        safe = new Vec3(raw.x, raw.y, height + 0.04f);
        var heightDelta = MathF.Abs(safe.z - _placement.Origin.z);
        if (heightDelta > MaximumSpawnHeightDelta)
        {
            ReportCrowdOverheadSurfaceRejection(raw, safe.z, heightDelta);
            safe = Vec3.Invalid;
            return false;
        }

        if (!Mission.IsPositionInsideBoundaries(safe.AsVec2) ||
            !HasAgentBodyClearance(safe, requireNavigationGround: false))
        {
            safe = Vec3.Invalid;
            return false;
        }

        var candidate = safe;
        if (Mission.Agents.Any(agent =>
                agent is not null &&
                agent.IsActive() &&
                agent.Position.DistanceSquared(candidate) < 0.75f) ||
            _occupiedSpawnPositions.Any(position => position.DistanceSquared(candidate) < 0.75f))
        {
            safe = Vec3.Invalid;
            return false;
        }

        return true;
    }

    private void ReportCrowdOverheadSurfaceRejection(Vec3 raw, float surfaceZ, float heightDelta)
    {
        _crowdOverheadSurfaceRejections++;
        if (_crowdOverheadSurfaceRejections > CrowdOverheadSurfaceRejectionLogLimit)
        {
            return;
        }

        RexLog.Info(
            $"Session {Request.SessionId} rejected crowd position " +
            $"({raw.x:0.00},{raw.y:0.00}) because the only surface there is " +
            $"{heightDelta:0.00}m above the ceremony ground " +
            $"(surface z={surfaceZ:0.00}, ground z={(_placement?.Origin.z ?? 0f):0.00}); " +
            "spectators never spawn on awnings, tents or roofs. " +
            $"Rejections so far: {_crowdOverheadSurfaceRejections}.");
    }

    private bool TryResolveExecutionActorPosition(
        ExecutionActor actor,
        out Vec3 safe)
    {
        safe = Vec3.Invalid;
        var player = _playerAgent;
        var customRole = actor == ExecutionActor.Player ? "player_interaction" : "executioner";
        if (TryGetCustomMarkerWorld(customRole, out var customActorPosition, out _))
        {
            safe = AdjustCustomFullStageActorMarker(customActorPosition);
            RexLog.Info(
                $"Session {Request.SessionId} used custom marker '{customRole}' for {actor} at " +
                $"({safe.x:0.000}, {safe.y:0.000}, {safe.z:0.000}).");
            return true;
        }
        if (actor is not ExecutionActor.Player and not ExecutionActor.Executioner ||
            _placement is null || _victimAgent is null ||
            _executionerAgent is null || player is null)
        {
            return false;
        }

        var victimRadius = GetSafeCollisionRadius(_victimAgent);
        var actionAgent = actor == ExecutionActor.Player
            ? player
            : _executionerAgent;
        var actorRadius = GetSafeCollisionRadius(actionAgent);
        var requestedDistance = GetRequestedExecutionActorDistance(actor);
        var requiredCollisionSeparation = GetRequiredAgentSeparation(
            victimRadius,
            actorRadius,
            minimumDistance: 0f);
        if (requestedDistance + 0.01f < requiredCollisionSeparation)
        {
            RexLog.Error(
                $"Session {Request.SessionId} cannot place {actor} at the exact requested " +
                $"{requestedDistance:0.000}m mark because the two collision capsules require " +
                $"{requiredCollisionSeparation:0.000}m.");
            return false;
        }

        GetVictimFacingBasis(out var prisonerForward, out var prisonerRight);
        if (UsesCrossbowExecution())
        {
            var localRightOffset = actor == ExecutionActor.Executioner
                ? CrossbowExecutionerFiringRightOffset
                : 0f;
            var rawFiringOrigin = _placement.Offset(localRightOffset, 0f);
            if (!TryResolveCeremonySurfaceMark(
                    rawFiringOrigin,
                    out safe,
                    allowedAgentA: _victimAgent,
                    allowedAgentB: _executionerAgent,
                    allowedAgentC: player,
                    ignoreTrackedPositions: true))
            {
                return false;
            }

            var crossbowActualDistance = safe.AsVec2.Distance(_victimPosition.AsVec2);
            var crossbowVictimToActor = safe.AsVec2 - _victimPosition.AsVec2;
            var crossbowPrisonerRightOffset = Vec2.DotProduct(crossbowVictimToActor, prisonerRight);
            var crossbowPrisonerForwardOffset = Vec2.DotProduct(crossbowVictimToActor, prisonerForward);
            RexLog.Info(
                $"Session {Request.SessionId} collision-safe {actor} crossbow mark resolved at " +
                $"local Right={localRightOffset:0.00}m: requested={requestedDistance:0.000}, " +
                $"actual={crossbowActualDistance:0.000}, " +
                $"victimViewRightOffset={crossbowPrisonerRightOffset:0.000}, " +
                $"victimViewForwardOffset={crossbowPrisonerForwardOffset:0.000}.");
            return true;
        }

        // "Left" is defined from the prisoner's live view, not from the town
        // marker or gallows prefab axes. Keep the action root directly on that
        // left ray; the actor then faces back toward the prisoner.
        var raw = _victimPosition - new Vec3(
            prisonerRight.x * requestedDistance,
            prisonerRight.y * requestedDistance,
            0f);
        if (!TryResolveCeremonySurfaceMark(
                raw,
                out safe,
                allowedAgentA: _victimAgent,
                allowedAgentB: _executionerAgent,
                allowedAgentC: player,
                ignoreTrackedPositions: true))
        {
            if (!RequiresExecutionTorch())
            {
                return false;
            }

            // The pyre and its native StandingPoint can make an otherwise
            // usable outdoor mark disappear from the local navigation query.
            // Burning must remain testable in that situation: preserve the
            // exact requested horizontal distance and use the terrain height
            // directly instead of cancelling the whole mission.
            var groundProbe = raw + new Vec3(0f, 0f, 4f);
            var groundHeight = Mission.Scene.GetGroundHeightAtPosition(groundProbe);
            if (!float.IsNaN(groundHeight) && !float.IsInfinity(groundHeight))
            {
                var groundOnly = new Vec3(raw.x, raw.y, groundHeight + 0.04f);
                if (Mission.IsPositionInsideBoundaries(groundOnly.AsVec2))
                {
                    safe = groundOnly;
                    RexLog.Warning(
                        $"Session {Request.SessionId} accepted the exact {requestedDistance:0.00}m " +
                        $"burning {actor} action mark from terrain height after local navigation rejected it; " +
                        "the failure was logged without cancelling the scene.");
                }
            }

            if (!safe.IsValid)
            {
                if (actor == ExecutionActor.Executioner && _executionerPosition.IsValid)
                {
                    safe = _executionerPosition;
                    RexLog.Warning(
                        $"Session {Request.SessionId} retained the already spawned burning executioner " +
                        "position after both navigation and terrain action-mark probes failed; " +
                        "the scene will continue instead of cancelling.");
                    return true;
                }

                return false;
            }
        }

        var actualDistance = safe.AsVec2.Distance(_victimPosition.AsVec2);
        var victimToActor = safe.AsVec2 - _victimPosition.AsVec2;
        var prisonerRightOffset = Vec2.DotProduct(victimToActor, prisonerRight);
        var prisonerForwardOffset = Vec2.DotProduct(victimToActor, prisonerForward);
        if (MathF.Abs(actualDistance - requestedDistance) > 0.02f)
        {
            return false;
        }

        RexLog.Info(
            $"Session {Request.SessionId} collision-safe {actor} action mark resolved on the prisoner's left: " +
            $"requested={requestedDistance:0.000}, collisionRequired={requiredCollisionSeparation:0.000}, " +
            $"actual={actualDistance:0.000}, " +
            $"victimViewRightOffset={prisonerRightOffset:0.000}, " +
            $"victimViewForwardOffset={prisonerForwardOffset:0.000}, " +
            $"victimRadius={victimRadius:0.000}, actorRadius={actorRadius:0.000}.");
        return true;
    }

    private Vec3 AdjustCustomFullStageActorMarker(Vec3 marker)
    {
        if (!UsesConfirmedCustomSite ||
            _placement is null ||
            _stageSurfaceHeight <= 0f)
        {
            return marker;
        }

        // Custom actor markers are authored relative to the preset root. A full
        // native gallows raises its deck after those markers are read, so carry
        // both the player and executioner action points onto the same deck.
        return new Vec3(
            marker.x,
            marker.y,
            marker.z + _stageSurfaceHeight);
    }

    private void GetVictimFacingBasis(out Vec2 forward, out Vec2 right)
    {
        forward = _placement?.Forward ?? Vec2.Forward;
        var victim = _victimAgent;
        if (victim is not null && victim.IsActive())
        {
            var liveForward = victim.LookDirection.AsVec2;
            if (liveForward.IsNonZero())
            {
                forward = liveForward;
            }
        }

        forward = forward.IsNonZero() ? forward.Normalized() : Vec2.Forward;
        right = new Vec2(forward.y, -forward.x);
    }

    private bool TryResolveCeremonySurfaceMark(
        Vec3 raw,
        out Vec3 safe,
        Agent? allowedAgentA = null,
        Agent? allowedAgentB = null,
        Agent? allowedAgentC = null,
        bool ignoreTrackedPositions = false)
    {
        if (!UsesGroundExecutionSite())
        {
            return TryResolveGallowsDeckMark(
                raw,
                out safe,
                allowedAgentA,
                allowedAgentB,
                allowedAgentC,
                ignoreTrackedPositions);
        }

        return TrySnapNavigable(
            raw,
            out safe,
            allowedAgentA,
            allowedAgentB,
            allowedAgentC,
            ignoreTrackedPositions,
            requireDirectLineFromPlacement: false);
    }

    private bool TryResolveGallowsDeckMark(
        Vec3 raw,
        out Vec3 safe,
        Agent? allowedAgentA = null,
        Agent? allowedAgentB = null,
        Agent? allowedAgentC = null,
        bool ignoreTrackedPositions = false)
    {
        safe = Vec3.Invalid;
        if (_placement is null || _stageSurfaceHeight <= 0f)
        {
            return false;
        }

        var candidate = new Vec3(
            raw.x,
            raw.y,
            _placement.Origin.z + _stageSurfaceHeight);
        if (!Mission.IsPositionInsideBoundaries(candidate.AsVec2) ||
            !IsGallowsDeckSurfacePosition(candidate) ||
            !HasGallowsDeckPhysicalSupport(candidate, out var supported) ||
            !HasCeremonyAgentClearance(supported))
        {
            return false;
        }

        if (Mission.Agents.Any(agent =>
                agent.IsActive() &&
                !ReferenceEquals(agent, allowedAgentA) &&
                !ReferenceEquals(agent, allowedAgentB) &&
                !ReferenceEquals(agent, allowedAgentC) &&
                agent.Position.AsVec2.Distance(supported.AsVec2) < 0.75f))
        {
            return false;
        }

        if (!ignoreTrackedPositions &&
            _occupiedSpawnPositions.Any(position =>
                position.AsVec2.Distance(supported.AsVec2) < 0.75f))
        {
            return false;
        }

        safe = supported;
        return true;
    }

    private bool TrySpawnCrossbowGroundWeapons()
    {
        if (!UsesCrossbowExecution() ||
            _placement is null ||
            !_npcExecutionActorPosition.IsValid)
        {
            return false;
        }

        try
        {
            var crossbowItem = Game.Current?.ObjectManager.GetObject<ItemObject>(
                CrossbowExecutionItemId);
            var boltItem = Game.Current?.ObjectManager.GetObject<ItemObject>(
                CrossbowBoltItemId);
            if (crossbowItem is null || boltItem is null)
            {
                RexLog.Error(
                    $"Crossbow pickup items were unavailable: crossbow={crossbowItem is not null}, " +
                    $"bolts={boltItem is not null}.");
                return false;
            }

            var right = _placement.Right.IsNonZero()
                ? _placement.Right.Normalized()
                : Vec2.Side;
            var forward = GetDirectionToVictim(_npcExecutionActorPosition);
            var crossbowFrame = MatrixFrame.Identity;
            crossbowFrame.origin = _npcExecutionActorPosition + new Vec3(
                right.x * -1.20f + forward.x * 0.20f,
                right.y * -1.20f + forward.y * 0.20f,
                0.42f);
            var crossbowDirection = new Vec3(forward.x, forward.y, 0f);
            crossbowFrame.rotation = Mat3.CreateMat3WithForward(in crossbowDirection);

            var boltFrame = MatrixFrame.Identity;
            boltFrame.origin = _npcExecutionActorPosition + new Vec3(
                right.x * 1.20f + forward.x * 0.20f,
                right.y * 1.20f + forward.y * 0.20f,
                0.38f);
            boltFrame.rotation = Mat3.CreateMat3WithForward(in crossbowDirection);

            var crossbowWeapon = new MissionWeapon(
                crossbowItem,
                null,
                _executionerAgent?.Origin?.Banner,
                1);
            var boltWeapon = new MissionWeapon(
                boltItem,
                null,
                _executionerAgent?.Origin?.Banner,
                20);
            var crossbowEntity = Mission.SpawnWeaponWithNewEntity(
                ref crossbowWeapon,
                Mission.WeaponSpawnFlags.WithPhysics,
                crossbowFrame);
            var boltEntity = Mission.SpawnWeaponWithNewEntity(
                ref boltWeapon,
                Mission.WeaponSpawnFlags.WithPhysics,
                boltFrame);
            if (crossbowEntity is null || boltEntity is null)
            {
                RexLog.Error(
                    $"Crossbow pickup entity creation returned null: crossbow={crossbowEntity is not null}, " +
                    $"bolts={boltEntity is not null}.");
                return false;
            }

            _spawnedEntities.Add(crossbowEntity);
            _spawnedEntities.Add(boltEntity);
            RexLog.Info(
                $"Session {Request.SessionId} spawned a pickup '{CrossbowExecutionItemId}' and 20 " +
                $"'{CrossbowBoltItemId}' at the shared player/executioner firing line.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not spawn the pickup crossbow and bolts.",
                exception);
            return false;
        }
    }

    private bool TryResolveExecutionerWaitingPosition(out Vec3 safe)
    {
        safe = Vec3.Invalid;
        var player = _playerAgent;
        if (_placement is null || _victimAgent is null ||
            _executionerAgent is null || player is null)
        {
            return false;
        }

        var victimRadius = GetSafeCollisionRadius(_victimAgent);
        var executionerRadius = GetSafeCollisionRadius(_executionerAgent);
        var actionRadius = MathF.Max(executionerRadius, GetSafeCollisionRadius(player));
        var requiredFromVictim = GetRequiredAgentSeparation(
            victimRadius,
            executionerRadius,
            minimumDistance: 0f);
        var requiredFromActor = GetRequiredAgentSeparation(
            actionRadius,
            executionerRadius,
            minimumDistance: 0f);
        var baseRadius = MathF.Max(requiredFromVictim, requiredFromActor) +
                         ExecutionerStandbyClearanceMargin;
        var ceremonyLateralShift = GetMethodCeremonyLateralShift();
        var preferredOffsets = new[]
        {
            new Vec2(
                ExecutionerPreferredStandbyLateralOffset,
                ExecutionerPreferredStandbyForwardOffset),
            new Vec2(-2.60f, 0.35f),
            new Vec2(-2.30f, 0.55f)
        };
        for (var preferredIndex = 0; preferredIndex < preferredOffsets.Length; preferredIndex++)
        {
            var preferredOffset = preferredOffsets[preferredIndex];
            if (TryAcceptExecutionerWaitingPosition(
                    _placement.Offset(
                        preferredOffset.x + ceremonyLateralShift,
                        preferredOffset.y),
                    requiredFromVictim,
                    requiredFromActor,
                    $"left-front preferred {preferredIndex + 1}/{preferredOffsets.Length}",
                    out safe))
            {
                return true;
            }
        }

        // Altered town scenes can block every authored left-front mark. Only
        // then rotate through a clearance-checked radial fallback that is still
        // constrained to the condemned's left side.
        var preferredStandbyDirection = new Vec2(
            ExecutionerPreferredStandbyLateralOffset,
            ExecutionerPreferredStandbyForwardOffset).Normalized();

        for (var ring = 0; ring < ExecutionerStandbyRingCount; ring++)
        {
            var radius = baseRadius + ring * ExecutionerStandbyRingSpacing;
            for (var slot = 0; slot < ExecutionerStandbySlotCount; slot++)
            {
                var angle = slot * (2f * MathF.PI / ExecutionerStandbySlotCount);
                var cosine = MathF.Cos(angle);
                var sine = MathF.Sin(angle);
                var right = preferredStandbyDirection.x * cosine -
                            preferredStandbyDirection.y * sine;
                var forward = preferredStandbyDirection.x * sine +
                              preferredStandbyDirection.y * cosine;
                var raw = _placement.Offset(
                    ceremonyLateralShift + right * radius,
                    forward * radius);
                if (TryAcceptExecutionerWaitingPosition(
                        raw,
                        requiredFromVictim,
                        requiredFromActor,
                        $"radial fallback ring={ring + 1},slot={slot + 1}",
                        out safe))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryAcceptExecutionerWaitingPosition(
        Vec3 raw,
        float requiredFromVictim,
        float requiredFromActor,
        string source,
        out Vec3 safe)
    {
        safe = Vec3.Invalid;
        if (_placement is null || _victimAgent is null || _executionerAgent is null ||
            !TryResolveCeremonySurfaceMark(
                raw,
                out var candidate,
                allowedAgentA: _victimAgent,
                allowedAgentB: _executionerAgent,
                ignoreTrackedPositions: true) ||
            !HasDynamicAgentClearance(_executionerAgent, candidate))
        {
            return false;
        }

        var actualFromVictim = candidate.AsVec2.Distance(_victimPosition.AsVec2);
        var actualFromPlayerAction = candidate.AsVec2.Distance(_playerExecutionActorPosition.AsVec2);
        var actualFromNpcAction = candidate.AsVec2.Distance(_npcExecutionActorPosition.AsVec2);
        var victimToCandidate = candidate.AsVec2 - _victimPosition.AsVec2;
        var prisonerRightOffset = victimToCandidate.x * _placement.Right.x +
                                  victimToCandidate.y * _placement.Right.y;
        if (actualFromVictim + 0.01f < requiredFromVictim ||
            actualFromPlayerAction + 0.01f < requiredFromActor ||
            actualFromNpcAction + 0.01f < requiredFromActor ||
            prisonerRightOffset > -MinimumExecutionerStandbyLeftDistance)
        {
            return false;
        }

        safe = candidate;
        RexLog.Info(
            $"Session {Request.SessionId} executioner standby '{source}': " +
            $"victim required/actual={requiredFromVictim:0.000}/{actualFromVictim:0.000}, " +
            $"playerAction required/actual={requiredFromActor:0.000}/{actualFromPlayerAction:0.000}, " +
            $"npcAction required/actual={requiredFromActor:0.000}/{actualFromNpcAction:0.000}, " +
            $"prisonerRightOffset={prisonerRightOffset:0.000}.");
        return true;
    }

    private bool TryResolveExecutionerRetreatPosition(
        out Vec3 safe,
        out Vec2 lookDirection)
    {
        safe = Vec3.Invalid;
        lookDirection = Vec2.Forward;
        if (_placement is null || _victimAgent is null || _executionerAgent is null ||
            _playerAgent is null)
        {
            return false;
        }

        var retreatOrigin = _executionerAgent.Position;
        if (!retreatOrigin.IsValid)
        {
            return false;
        }

        var victimRadius = GetSafeCollisionRadius(_victimAgent);
        var executionerRadius = GetSafeCollisionRadius(_executionerAgent);
        var actionRadius = MathF.Max(executionerRadius, GetSafeCollisionRadius(_playerAgent));
        var requiredFromVictim = GetRequiredAgentSeparation(
            victimRadius,
            executionerRadius,
            minimumDistance: 0f);
        var requiredFromActor = GetRequiredAgentSeparation(
            actionRadius,
            executionerRadius,
            minimumDistance: 0f);

        // v0.3.23 moves the authored standby mark to the already verified blue
        // side-observation position. Keeping the NPC on that supported mark is
        // clearer than stepping him another 0.65 m outward when the player acts.
        if (TryAcceptExecutionerWaitingPosition(
                retreatOrigin,
                requiredFromVictim,
                requiredFromActor,
                "retreat keep authored side-observation mark",
                out var retainedStandby))
        {
            safe = retainedStandby;
            lookDirection = GetDirectionToVictim(retainedStandby);
            RexLog.Info(
                $"Session {Request.SessionId} retained executioner at the supported standby/observation " +
                "mark while the player takes the action root; no second lateral jump was needed.");
            return true;
        }

        // The player will take the executioner's action root. The NPC therefore
        // steps farther out on the condemned's left (or a little back from the
        // crowd), never toward the front-row guards or spectators.
        var candidates = new[]
        {
            (Lateral: -ExecutionerRetreatSideStepDistance, Forward: 0f, Source: "preferred side-step"),
            (Lateral: -0.45f, Forward: -0.25f, Source: "short side-back fallback"),
            (Lateral: -0.85f, Forward: -0.25f, Source: "wide side-back fallback")
        };
        foreach (var candidateOffset in candidates)
        {
            var raw = retreatOrigin +
                      new Vec3(
                          _placement.Right.x * candidateOffset.Lateral +
                          _placement.Forward.x * candidateOffset.Forward,
                          _placement.Right.y * candidateOffset.Lateral +
                          _placement.Forward.y * candidateOffset.Forward,
                          0f);
            if (!TryAcceptExecutionerWaitingPosition(
                    raw,
                    requiredFromVictim,
                    requiredFromActor,
                    $"retreat {candidateOffset.Source}",
                    out var candidate))
            {
                continue;
            }

            var movement = candidate.AsVec2 - retreatOrigin.AsVec2;
            var outwardSideStep = -(
                movement.x * _placement.Right.x +
                movement.y * _placement.Right.y);
            var crowdAdvance =
                movement.x * _placement.Forward.x +
                movement.y * _placement.Forward.y;
            if (outwardSideStep < ExecutionerRetreatMinimumSideStep ||
                crowdAdvance > ExecutionerRetreatMaximumCrowdAdvance)
            {
                continue;
            }

            safe = candidate;
            lookDirection = _victimPosition.AsVec2 - candidate.AsVec2;
            if (!lookDirection.IsNonZero())
            {
                lookDirection = -_placement.Forward;
            }

            lookDirection = lookDirection.Normalized();
            RexLog.Info(
                $"Session {Request.SessionId} executioner retreat resolved at the nearby ceremony side " +
                $"({candidateOffset.Source}, side-step={outwardSideStep:0.00}m, " +
                $"crowd-advance={crowdAdvance:0.00}m).");
            return true;
        }

        return false;
    }

    private bool TryStartExecutionerRetreat()
    {
        var executioner = _executionerAgent;
        if (_actor != ExecutionActor.Player || executioner is null || !executioner.IsActive() ||
            !TryResolveExecutionerRetreatPosition(
                out _executionerRetreatPosition,
                out _executionerRetreatDirection))
        {
            return false;
        }

        try
        {
            var none = ActionIndexCache.act_none;
            if (!executioner.SetActionChannel(
                    0,
                    in none,
                    ignorePriority: true,
                    additionalFlags: AnimFlags.anf_restart))
            {
                RexLog.Warning(
                    "The executioner standby action did not acknowledge its release before retreat movement.");
            }

            executioner.ClearTargetFrame();
            executioner.DisableScriptedMovement();
            executioner.Controller = AgentControllerType.None;
            PoseAgentAt(
                executioner,
                _executionerRetreatPosition,
                _executionerRetreatDirection);
            if (!HasDynamicAgentClearance(executioner, _executionerRetreatPosition) ||
                !HasExecutionerRetreatSurfaceSupport(
                    _executionerRetreatPosition,
                    out _))
            {
                return false;
            }

            _executionerPosition = _executionerRetreatPosition;
            _executionerRetreatStarted = true;
            _executionerRetreatCompleted = true;
            _executionerStandbyRestored = false;
            RexLog.Info(
                $"Session {Request.SessionId} moved executioner {executioner.Index} directly to the " +
                "collision-supported nearby side observation mark; no ground navmesh walk was used.");
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                executioner.DisableScriptedMovement();
                executioner.Controller = AgentControllerType.None;
            }
            catch
            {
                // The caller cancels the session without a lethal commit.
            }

            RexLog.Error("The executioner could not complete the nearby side-step.", exception);
            return false;
        }
    }

    private bool TickExecutionerRetreat()
    {
        if (_actor != ExecutionActor.Player)
        {
            return true;
        }

        var executioner = _executionerAgent;
        if (!_executionerRetreatStarted || !_executionerRetreatCompleted ||
            executioner is null || !executioner.IsActive())
        {
            if (ContinueGroundExecutionAfterRetreatDiagnostic(
                    "The executioner became unavailable before settling at the nearby side mark."))
            {
                return true;
            }

            CancelExecutionerRetreat(
                "The executioner became unavailable before settling at the nearby side mark.");
            return false;
        }

        if (!HasDynamicAgentClearance(executioner, _executionerRetreatPosition) ||
            !HasExecutionerRetreatSurfaceSupport(_executionerRetreatPosition, out _))
        {
            if (ContinueGroundExecutionAfterRetreatDiagnostic(
                    "The nearby side observation mark lost its physical support or clearance."))
            {
                return true;
            }

            CancelExecutionerRetreat(
                "The nearby side observation mark lost its physical support or clearance.");
            return false;
        }

        return true;
    }

    private bool HasExecutionerRetreatSurfaceSupport(Vec3 position, out Vec3 supported)
    {
        if (!UsesGroundExecutionSite())
        {
            return HasGallowsDeckPhysicalSupport(position, out supported);
        }

        if (!TrySnapNavigable(
                position,
                out supported,
                allowedAgentA: _executionerAgent,
                allowedAgentB: _victimAgent,
                allowedAgentC: _playerAgent,
                ignoreTrackedPositions: true,
                requireDirectLineFromPlacement: false))
        {
            return false;
        }

        return supported.AsVec2.Distance(position.AsVec2) <= 0.05f &&
               MathF.Abs(supported.z - position.z) <= 0.25f;
    }

    private bool ContinueGroundExecutionAfterRetreatDiagnostic(string detail)
    {
        if (!UsesGroundExecutionSite() && !UsesConfirmedCustomSite)
        {
            return false;
        }

        if (!_groundRetreatDiagnosticLogged)
        {
            _groundRetreatDiagnosticLogged = true;
            RexLog.Error(
                $"{detail} The executioner remains at the current observation mark; " +
                "the mission and execution sequence remain active.");
        }

        return true;
    }

    private void CancelExecutionerRetreat(string detail)
    {
        RexLog.Error($"{detail} Cancelling before death or cost commit.");
        CancelSceneAndReturn(
            ExecutionFailureReason.ScenePlacementFailed,
            new TextObject(
                "{=REX_Error_Executioner_Retreat}The executioner could not move to the nearby side position safely. Nothing was spent and the prisoner lives."));
    }

    private static float GetSafeCollisionRadius(Agent agent)
    {
        try
        {
            var radius = agent.CollisionCapsule.Radius;
            return IsFinite(radius) && radius >= 0.15f && radius <= 0.75f
                ? radius
                : DefaultHumanCollisionRadius;
        }
        catch
        {
            return DefaultHumanCollisionRadius;
        }
    }

    private bool TryResolveInitialExecutionerPosition(out Vec3 safe)
    {
        safe = Vec3.Invalid;
        if (_placement is null)
        {
            return false;
        }

        if (TryGetCustomMarkerWorld("executioner", out var customExecutionerPosition, out _))
        {
            safe = customExecutionerPosition;
            RexLog.Info(
                $"Session {Request.SessionId} used the custom executioner spawn marker at " +
                $"({safe.x:0.000}, {safe.y:0.000}, {safe.z:0.000}).");
            return true;
        }

        var ceremonyLateralShift = GetMethodCeremonyLateralShift();
        if (UsesStoningExecution())
        {
            var stoningOffsets = new[]
            {
                new Vec2(
                    StoningExecutionerStandbyRightOffset,
                    StoningExecutionerStandbyForwardOffset),
                new Vec2(-0.35f, 3.40f),
                new Vec2(0.35f, 3.40f)
            };
            for (var index = 0; index < stoningOffsets.Length; index++)
            {
                var offset = stoningOffsets[index];
                if (!TrySnapNavigable(
                        _placement.Offset(offset.x, offset.y),
                        out safe,
                        requireDirectLineFromPlacement: false))
                {
                    continue;
                }

                RexLog.Info(
                    $"Session {Request.SessionId} resolved stoning executioner blue-box entry spawn " +
                    $"{index + 1}/{stoningOffsets.Length} at local Right={offset.x:0.00}m, " +
                    $"Forward={offset.y:0.00}m; this is also the permanent standby mark.");
                return true;
            }

            var preferred = _placement.Offset(
                StoningExecutionerStandbyRightOffset,
                StoningExecutionerStandbyForwardOffset);
            var preferredProbe = preferred + new Vec3(0f, 0f, 4f);
            var preferredHeight = Mission.Scene.GetGroundHeightAtPosition(preferredProbe);
            if (!float.IsNaN(preferredHeight) && !float.IsInfinity(preferredHeight))
            {
                safe = new Vec3(preferred.x, preferred.y, preferredHeight + 0.04f);
                if (Mission.IsPositionInsideBoundaries(safe.AsVec2))
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} used the exact stoning blue-box ground spawn without " +
                        "a local navigation result; the NPC will still enter and remain at that mark.");
                    return true;
                }
            }

            safe = Vec3.Invalid;
            return false;
        }

        if (UsesCrossbowExecution())
        {
            var preferred = _placement.Offset(CrossbowExecutionerFiringRightOffset, 0f);
            if (TrySnapNavigable(
                    preferred,
                    out safe,
                    requireDirectLineFromPlacement: false))
            {
                RexLog.Info(
                    $"Session {Request.SessionId} resolved the crossbow executioner's direct entry/action " +
                    $"spawn at local Right={CrossbowExecutionerFiringRightOffset:0.00}m, Forward=0.00m.");
                return true;
            }

            var crossbowGroundProbe = preferred + new Vec3(0f, 0f, 4f);
            var crossbowGroundHeight = Mission.Scene.GetGroundHeightAtPosition(crossbowGroundProbe);
            if (!float.IsNaN(crossbowGroundHeight) && !float.IsInfinity(crossbowGroundHeight))
            {
                safe = new Vec3(preferred.x, preferred.y, crossbowGroundHeight + 0.04f);
                if (Mission.IsPositionInsideBoundaries(safe.AsVec2))
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} retained the exact crossbow firing-line spawn without " +
                        "a local navigation result; the NPC will still enter directly on its action mark.");
                    return true;
                }
            }

            safe = Vec3.Invalid;
            return false;
        }

        var offsets = new[]
        {
            new Vec2(
                ExecutionerPreferredStandbyLateralOffset,
                ExecutionerPreferredStandbyForwardOffset),
            new Vec2(-3.10f, 0.20f),
            new Vec2(3.10f, 0.20f),
            new Vec2(-2.70f, 1.40f),
            new Vec2(2.70f, 1.40f),
            new Vec2(-3.40f, -1.10f),
            new Vec2(3.40f, -1.10f)
        };
        foreach (var offset in offsets)
        {
            if (TrySnapNavigable(
                    _placement.Offset(offset.x + ceremonyLateralShift, offset.y),
                    out safe,
                    requireDirectLineFromPlacement: false))
            {
                return true;
            }
        }

        var raw = _placement.Offset(
            ExecutionerPreferredStandbyLateralOffset + ceremonyLateralShift,
            ExecutionerPreferredStandbyForwardOffset);
        var probe = raw + new Vec3(0f, 0f, 4f);
        var height = Mission.Scene.GetGroundHeightAtPosition(probe);
        if (float.IsNaN(height) || float.IsInfinity(height))
        {
            return false;
        }

        safe = new Vec3(raw.x, raw.y, height + 0.04f);
        if (!Mission.IsPositionInsideBoundaries(safe.AsVec2))
        {
            safe = Vec3.Invalid;
            return false;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} used a ground-only executioner spawn at " +
            $"({safe.x:0.00}, {safe.y:0.00}, {safe.z:0.00}) as the final city-compatible fallback; " +
            "the missing local navigation result was logged but did not cancel scene construction.");
        return true;
    }

    private bool TrySnapNavigable(
        Vec3 raw,
        out Vec3 safe,
        Agent? allowedAgentA = null,
        Agent? allowedAgentB = null,
        Agent? allowedAgentC = null,
        bool ignoreTrackedPositions = false,
        bool requireDirectLineFromPlacement = true)
    {
        var probe = raw;
        probe.z += 4f;
        var height = Mission.Scene.GetGroundHeightAtPosition(probe);
        if (float.IsNaN(height) || float.IsInfinity(height))
        {
            safe = Vec3.Invalid;
            return false;
        }

        safe = new Vec3(raw.x, raw.y, height + 0.04f);
        if (
            !Mission.IsPositionInsideBoundaries(safe.AsVec2) ||
            Mission.Scene.GetNavigationMeshForPosition(in safe) == UIntPtr.Zero ||
            Mission.IsPositionOnAnyBlockerNavMeshFace(safe) ||
            !HasSafeAgentClearance(safe))
        {
            return false;
        }

        var snapped = safe;
        if (Mission.Agents.Any(agent =>
                agent.IsActive() &&
                !ReferenceEquals(agent, allowedAgentA) &&
                !ReferenceEquals(agent, allowedAgentB) &&
                !ReferenceEquals(agent, allowedAgentC) &&
                agent.Position.DistanceSquared(snapped) < 0.75f))
        {
            return false;
        }

        if (_placement is null)
        {
            return false;
        }

        if (MathF.Abs(safe.z - _placement.Origin.z) > MaximumSpawnHeightDelta ||
            (requireDirectLineFromPlacement && !HasDirectLineFromPlacement(safe)))
        {
            return false;
        }

        var start = new WorldPosition(Mission.Scene, _placement.Origin);
        var end = new WorldPosition(Mission.Scene, safe);
        var directDistance = MathF.Max(1f, _placement.Origin.AsVec2.Distance(safe.AsVec2));
        if (!Mission.Scene.GetPathDistanceBetweenPositions(ref start, ref end, 0.35f, out var pathDistance) ||
            pathDistance > directDistance * 2.5f + 5f)
        {
            return false;
        }

        return ignoreTrackedPositions ||
               !_occupiedSpawnPositions.Any(position => position.DistanceSquared(snapped) < 0.75f);
    }

    private bool HasSafeAgentClearance(Vec3 ground) =>
        HasAgentBodyClearance(ground, requireNavigationGround: true);

    private bool HasCeremonyAgentClearance(Vec3 position)
    {
        if (IsGallowsDeckSurfacePosition(position))
        {
            // Temporarily bypass the generic body-clearance acceptance rule on
            // the complete native gallows. Its authored overhead collision can
            // intersect the vertical clearance probe even when the deck mark is
            // valid. Deck bounds, physical support and live-agent separation
            // are still checked independently.
            return true;
        }

        return HasSafeAgentClearance(position);
    }

    private bool IsGallowsDeckSurfacePosition(Vec3 position)
    {
        if (_stageSurfaceHeight <= 0f ||
            !_gallowsDeckBoundsMin.IsValid ||
            !_gallowsDeckBoundsMax.IsValid ||
            MathF.Abs(position.z - _gallowsDeckBoundsMax.z) >
            GallowsDeckSurfaceTolerance + GallowsDeckSupportHeightTolerance)
        {
            return false;
        }

        return position.x >= _gallowsDeckBoundsMin.x - 0.12f &&
               position.x <= _gallowsDeckBoundsMax.x + 0.12f &&
               position.y >= _gallowsDeckBoundsMin.y - 0.12f &&
               position.y <= _gallowsDeckBoundsMax.y + 0.12f;
    }

    private bool HasGallowsDeckPhysicalSupport(Vec3 position, out Vec3 supported)
    {
        supported = Vec3.Invalid;
        if (_placement is null || !IsGallowsDeckSurfacePosition(position))
        {
            return false;
        }

        var expectedSurfaceZ = _placement.Origin.z + _stageSurfaceHeight;
        var start = new Vec3(
            position.x,
            position.y,
            expectedSurfaceZ + GallowsDeckSupportProbeHeight);
        var end = new Vec3(
            position.x,
            position.y,
            expectedSurfaceZ - GallowsDeckSupportProbeDepth);
        var excludeFlags =
            BodyFlags.Dynamic |
            BodyFlags.DroppedItem |
            BodyFlags.WaterBody |
            BodyFlags.AgentOnly |
            BodyFlags.MissileOnly |
            BodyFlags.StealthBox;
        if (!Mission.Scene.RayCastForClosestEntityOrTerrainFixedPhysics(
                start,
                end,
                out _,
                out var closestPoint,
                out _,
                0.06f,
                excludeFlags) ||
            !closestPoint.IsValid ||
            MathF.Abs(closestPoint.z - expectedSurfaceZ) >
            GallowsDeckSupportHeightTolerance)
        {
            return false;
        }

        supported = new Vec3(position.x, position.y, closestPoint.z + 0.015f);
        return IsGallowsDeckSurfacePosition(supported);
    }

    private bool HasAgentBodyClearance(Vec3 ground, bool requireNavigationGround)
    {
        const float localProbeDistance = 0.38f;
        const float maximumLocalHeightDelta = 0.32f;
        var directions = new[]
        {
            new Vec2(1f, 0f),
            new Vec2(-1f, 0f),
            new Vec2(0f, 1f),
            new Vec2(0f, -1f),
            new Vec2(0.7071068f, 0.7071068f),
            new Vec2(-0.7071068f, 0.7071068f),
            new Vec2(0.7071068f, -0.7071068f),
            new Vec2(-0.7071068f, -0.7071068f)
        };

        foreach (var direction in directions)
        {
            if (requireNavigationGround)
            {
                var probe = ground + new Vec3(
                    direction.x * localProbeDistance,
                    direction.y * localProbeDistance,
                    4f);
                var nearbyHeight = Mission.Scene.GetGroundHeightAtPosition(probe);
                if (float.IsNaN(nearbyHeight) ||
                    float.IsInfinity(nearbyHeight) ||
                    MathF.Abs((nearbyHeight + 0.04f) - ground.z) > maximumLocalHeightDelta)
                {
                    return false;
                }
            }

            var chest = ground + new Vec3(0f, 0f, AgentChestHeight);
            var wallProbe = chest + new Vec3(
                direction.x * AgentBodyClearanceRadius,
                direction.y * AgentBodyClearanceRadius,
                0f);
            if (Mission.Scene.RayCastForClosestEntityOrTerrain(
                    chest,
                    wallProbe,
                    out _,
                    out _,
                    out _,
                    0.16f,
                    BodyFlags.CommonCollisionExcludeFlagsForAgent))
            {
                return false;
            }
        }

        var clearanceStart = ground + new Vec3(0f, 0f, 0.18f);
        var clearanceEnd = ground + new Vec3(0f, 0f, 2.25f);
        return !Mission.Scene.RayCastForClosestEntityOrTerrain(
            clearanceStart,
            clearanceEnd,
            out _,
            out _,
            out _,
            0.16f);
    }

    private bool HasDirectLineFromPlacement(Vec3 ground)
    {
        if (_placement is null)
        {
            return false;
        }

        var start = _placement.Origin + new Vec3(0f, 0f, AgentChestHeight);
        var end = ground + new Vec3(0f, 0f, AgentChestHeight);
        if (start.DistanceSquared(end) < 0.04f)
        {
            return true;
        }

        return !Mission.Scene.RayCastForClosestEntityOrTerrain(
            start,
            end,
            out _,
            out _,
            out _,
            0.16f,
            BodyFlags.CommonCollisionExcludeFlagsForAgent);
    }

    private Agent SpawnCharacter(
        CharacterObject? character,
        Vec3 position,
        Vec2 direction,
        bool civilianEquipment,
        bool noWeapons,
        bool invulnerable,
        Equipment? equipmentOverride = null,
        BodyProperties? bodyPropertiesOverride = null,
        Team? teamOverride = null,
        bool? femaleOverride = null,
        int? ageOverride = null,
        int? raceOverride = null,
        uint? clothingColor1Override = null,
        uint? clothingColor2Override = null,
        bool fixedEquipment = false,
        IAgentOriginBase? originOverride = null,
        bool joinPlayerFormation = false)
    {
        if (character is null)
        {
            throw new InvalidOperationException("A culture character template is missing.");
        }

        if (!direction.IsNonZero())
        {
            direction = Vec2.Forward;
        }

        var origin = originOverride ?? new SimpleAgentOrigin(character, -1, null, default);
        Formation? playerFormation = null;
        if (joinPlayerFormation && teamOverride is not null && teamOverride.IsValid)
        {
            try
            {
                var formationClass = character.DefaultFormationClass;
                if ((int)formationClass < 0 || formationClass >= FormationClass.NumberOfRegularFormations)
                {
                    formationClass = FormationClass.Infantry;
                }

                playerFormation = teamOverride.GetFormation(formationClass);
            }
            catch (Exception exception)
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} could not resolve player formation for " +
                    $"'{character.StringId}'; the Agent still spawns on the player team ({exception.Message}).");
            }
        }

        var buildData = new AgentBuildData(origin)
            .Controller(AgentControllerType.AI)
            .InitialPosition(in position)
            .InitialDirection(direction.Normalized())
            .Team(teamOverride ?? Team.Invalid)
            .NoHorses(true)
            .NoWeapons(noWeapons)
            .CivilianEquipment(civilianEquipment)
            .ClothingColor1(clothingColor1Override ?? Request.Venue.MapFaction.Color)
            .ClothingColor2(clothingColor2Override ?? Request.Venue.MapFaction.Color2)
            .CanSpawnOutsideOfMissionBoundary(false);
        if (playerFormation is not null)
        {
            buildData = buildData.Formation(playerFormation);
        }

        if (femaleOverride.HasValue)
        {
            buildData = buildData.IsFemale(femaleOverride.Value);
        }

        if (ageOverride.HasValue)
        {
            buildData = buildData.Age(Math.Max(18, ageOverride.Value));
        }

        if (raceOverride.HasValue)
        {
            buildData = buildData.Race(raceOverride.Value);
        }

        if (equipmentOverride is not null)
        {
            buildData = buildData.Equipment(equipmentOverride);
        }

        if (fixedEquipment)
        {
            buildData = buildData.FixedEquipment(true);
        }

        if (bodyPropertiesOverride.HasValue)
        {
            buildData = buildData.BodyProperties(bodyPropertiesOverride.Value);
        }

        var agent = Mission.SpawnAgent(buildData, spawnFromAgentVisuals: false);
        agent.Controller = AgentControllerType.None;
        if (invulnerable)
        {
            agent.ToggleInvulnerable();
        }

        if (playerFormation is not null)
        {
            RexLog.Info(
                $"Session {Request.SessionId} spawned '{character.StringId}' in player formation " +
                $"{playerFormation.FormationIndex} from the player's party troop template; roster count was not changed.");
        }

        _spawnedAgents.Add(agent);
        _occupiedSpawnPositions.Add(position);
        return agent;
    }

    private bool TryResolveCeremonyTeams(out Team executionTeam, out Team victimTeam)
    {
        executionTeam = Team.Invalid;
        victimTeam = Team.Invalid;

        try
        {
            var playerTeam = Mission.PlayerTeam;
            if (!IsUsableTeam(playerTeam) && IsUsableTeam(_playerAgent?.Team))
            {
                playerTeam = _playerAgent!.Team;
            }

            if (!IsUsableBattleSideTeam(playerTeam))
            {
                playerTeam = Mission.Teams.FirstOrDefault(IsUsableBattleSideTeam);
            }

            if (!IsUsableBattleSideTeam(playerTeam))
            {
                playerTeam = Mission.Teams.Add(
                    BattleSideEnum.Attacker,
                    Request.Venue.MapFaction.Color,
                    Request.Venue.MapFaction.Color2,
                    banner: null,
                    isPlayerGeneral: true,
                    isPlayerSergeant: false,
                    isSettingRelations: true);
                RexLog.Info(
                    $"Session {Request.SessionId} created execution team {playerTeam.TeamIndex} " +
                    "because the town mission had no usable player battle side.");
            }

            if (!IsUsableBattleSideTeam(playerTeam))
            {
                return false;
            }

            executionTeam = playerTeam;
            if (Mission.PlayerTeam != executionTeam)
            {
                Mission.PlayerTeam = executionTeam;
            }

            if (_playerAgent is not null && _playerAgent.Team != executionTeam)
            {
                _playerAgent.SetTeam(executionTeam, sync: false);
            }

            var oppositeSide = executionTeam.Side == BattleSideEnum.Attacker
                ? BattleSideEnum.Defender
                : BattleSideEnum.Attacker;
            var existingEnemy = Mission.PlayerEnemyTeam;
            if (!IsUsableTeam(existingEnemy) ||
                existingEnemy.Side != oppositeSide ||
                !executionTeam.IsEnemyOf(existingEnemy))
            {
                var resolvedExecutionTeam = executionTeam;
                existingEnemy = Mission.Teams.FirstOrDefault(
                    team => IsUsableTeam(team) &&
                            team != resolvedExecutionTeam &&
                            team.Side == oppositeSide);
            }

            if (!IsUsableTeam(existingEnemy))
            {
                existingEnemy = Mission.Teams.Add(
                    oppositeSide,
                    Request.Venue.MapFaction.Color,
                    Request.Venue.MapFaction.Color2,
                    banner: null,
                    isPlayerGeneral: false,
                    isPlayerSergeant: false,
                    isSettingRelations: true);
                RexLog.Info(
                    $"Session {Request.SessionId} created hostile victim team {existingEnemy.TeamIndex} " +
                    $"on {oppositeSide} for the native battlefield death pipeline.");
            }

            if (!IsUsableTeam(existingEnemy))
            {
                return false;
            }

            victimTeam = existingEnemy;
            executionTeam.SetIsEnemyOf(victimTeam, isEnemyOf: true);
            victimTeam.SetIsEnemyOf(executionTeam, isEnemyOf: true);
            var valid = executionTeam.IsEnemyOf(victimTeam) &&
                        victimTeam.IsEnemyOf(executionTeam) &&
                        _playerAgent?.Team == executionTeam;
            RexLog.Info(
                $"Session {Request.SessionId} ceremony teams: execution={executionTeam.TeamIndex}/" +
                $"{executionTeam.Side}, victim={victimTeam.TeamIndex}/{victimTeam.Side}, " +
                $"mutualEnemy={valid}.");
            return valid;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not establish the battlefield execution teams.",
                exception);
            executionTeam = Team.Invalid;
            victimTeam = Team.Invalid;
            return false;
        }
    }

    private static bool IsUsableTeam(Team? team) =>
        team is not null && team.IsValid;

    private static bool IsUsableBattleSideTeam(Team? team) =>
        IsUsableTeam(team) &&
        team!.Side is BattleSideEnum.Attacker or BattleSideEnum.Defender;

    private bool EnsureGeneratedAllyOnPlayerTeam(Agent ally, string role)
    {
        var playerTeam = Mission.PlayerTeam;
        if (ally is null || !ally.IsActive() ||
            !IsUsableBattleSideTeam(playerTeam) ||
            _victimTeam is null || !IsUsableBattleSideTeam(_victimTeam))
        {
            RexLog.Error(
                $"Session {Request.SessionId} could not verify player-team ownership for generated {role}.");
            return false;
        }

        try
        {
            if (ally.Team != playerTeam)
            {
                ally.SetTeam(playerTeam, sync: false);
            }

            playerTeam.SetIsEnemyOf(_victimTeam, isEnemyOf: true);
            _victimTeam.SetIsEnemyOf(playerTeam, isEnemyOf: true);
            var retained = ally.Team == playerTeam &&
                           playerTeam.IsEnemyOf(_victimTeam) &&
                           _victimTeam.IsEnemyOf(playerTeam);
            if (retained)
            {
                RexLog.Info(
                    $"Session {Request.SessionId} confirmed generated {role} agent {ally.Index} " +
                    $"on player team {playerTeam.TeamIndex}/{playerTeam.Side}.");
            }
            else
            {
                RexLog.Error(
                    $"Session {Request.SessionId} generated {role} agent {ally.Index} did not retain " +
                    $"player team {playerTeam.TeamIndex}/{playerTeam.Side}.");
            }

            return retained;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not assign generated {role} to the player's mission team.",
                exception);
            return false;
        }
    }

    private float GetMethodCeremonyLateralShift() =>
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Beheading,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.Hanging,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            CurrentMethod.StringId,
            ExecutionMethodRules.BreakingWheel,
            StringComparison.OrdinalIgnoreCase)
            ? ScaffoldCeremonyLateralShift
            : 0f;

    private float GetMethodVictimForwardShift() =>
        UsesCrossbowExecution()
            ? CrossbowVictimRearwardOffset
            : 0f;

    private IEnumerable<Agent> EnumeratePlayerFormationCeremonyAllies() =>
        new[] { _executionerAgent }
            .Concat(_frontGuardAgents)
            .Concat(_crossbowVolleyAgents)
            .Concat(_methodHelperAgents)
            .Where(agent => agent is not null)
            .Select(agent => agent!)
            .Distinct();

    private void SetCeremonyAlliesFormationControl(bool enabled, string reason)
    {
        if (_ceremonyAlliesReleasedToPlayerFormation == enabled)
        {
            return;
        }

        var playerTeam = Mission.PlayerTeam;
        var updated = 0;
        var failed = 0;
        foreach (var ally in EnumeratePlayerFormationCeremonyAllies())
        {
            if (!ally.IsActive() || playerTeam is null || !playerTeam.IsValid || ally.Team != playerTeam)
            {
                continue;
            }

            try
            {
                ally.DisableScriptedMovement();
                ally.ClearTargetFrame();
                ally.Controller = enabled
                    ? AgentControllerType.AI
                    : AgentControllerType.None;
                updated++;
            }
            catch (Exception exception)
            {
                failed++;
                RexLog.Error(
                    $"Session {Request.SessionId} could not {(enabled ? "release" : "take control of")} " +
                    $"ceremony ally {ally.Index} for {reason}.",
                    exception);
            }
        }

        _ceremonyAlliesReleasedToPlayerFormation = enabled;
        RexLog.Info(
            $"Session {Request.SessionId} {(enabled ? "released" : "reclaimed")} {updated} ceremony allies " +
            $"{(enabled ? "to" : "from")} player formation control for {reason}; failures={failed}.");
    }
    private void TryMoveFrozenVictimToPlayerTeam(Agent victim, string role)
    {
        try
        {
            var playerTeam = Mission.PlayerTeam;
            if (!IsUsableTeam(playerTeam))
            {
                if (!_frozenVictimPlayerTeamTransferFailureLogged)
                {
                    _frozenVictimPlayerTeamTransferFailureLogged = true;
                    RexLog.Warning(
                        $"Session {Request.SessionId} could not clear the nearby-enemy retreat gate after {role}; " +
                        "Mission.PlayerTeam was unavailable.");
                }
                return;
            }

            if (victim.Team != playerTeam)
            {
                victim.SetTeam(playerTeam, sync: false);
            }

            if (victim.Team == playerTeam)
            {
                if (!_frozenVictimPlayerTeamTransferLogged)
                {
                    _frozenVictimPlayerTeamTransferLogged = true;
                    RexLog.Info(
                        $"Session {Request.SessionId} moved frozen original prisoner {victim.Index} to " +
                        $"player team {playerTeam.TeamIndex} after {role}; the living visual no longer blocks retreat as a nearby enemy.");
                }
            }
            else if (!_frozenVictimPlayerTeamTransferFailureLogged)
            {
                _frozenVictimPlayerTeamTransferFailureLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} original prisoner {victim.Index} did not retain " +
                    $"player team {playerTeam.TeamIndex} after {role}; the mission remains active for diagnosis.");
            }
        }
        catch (Exception exception)
        {
            if (!_frozenVictimPlayerTeamTransferFailureLogged)
            {
                _frozenVictimPlayerTeamTransferFailureLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} could not clear the nearby-enemy retreat gate after {role}; " +
                    $"the frozen pose remains active. {exception.Message}");
            }
        }
    }

    private GameEntity? SpawnProfileProp(ExecutionScenePropPlacement prop)
    {
        if (_placement is null)
        {
            return null;
        }

        var position = _placement.Offset(
            prop.Offset.Right + (UsesConfirmedCustomSite ? 0f : GetMethodCeremonyLateralShift()),
            prop.Offset.Forward,
            prop.Offset.Up + _stageSurfaceHeight);
        if (string.Equals(
                prop.RoleId,
                "crossbow_crossbeam",
                StringComparison.OrdinalIgnoreCase) &&
            _victimPosition.IsValid)
        {
            // The victim root can sit above or below the sampled terrain in
            // third-party scenes. A profile-relative Z therefore cannot keep
            // the plank on the same line as the wrist IK targets. Align the
            // crossbeam center to the exact world-space hand-anchor height.
            position.z = _victimPosition.z + CrossbowHandAnchorHeight;
            RexLog.Info(
                $"Session {Request.SessionId} aligned the crossbow crucifix beam anchor to the " +
                $"original prisoner's hand IK height: rootZ={_victimPosition.z:0.000}, " +
                $"handOffset={CrossbowHandAnchorHeight:0.000}, beamCenterZ={position.z:0.000}.");
        }

        foreach (var prefabName in prop.PrefabCandidates)
        {
            if (!GameEntity.PrefabExists(prefabName))
            {
                continue;
            }

            try
            {
                var entity = SpawnPrefab(
                    prefabName,
                    position,
                    prop.CreatePhysics,
                    prop.CallScriptCallbacks,
                    prop.YawDegrees,
                    prop.PitchDegrees,
                    prop.RollDegrees,
                    prop.UniformScale);
                if (entity is not null)
                {
                    _profileEntities[prop.RoleId] = entity;
                    if (string.Equals(
                            prop.RoleId,
                            "burning_stake",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            prop.RoleId,
                            "crossbow_stake",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        TryAlignProfileStake(
                            entity,
                            position,
                            prop.UniformScale,
                            prop.RoleId,
                            prefabName);
                        CaptureBurningCrucifixPart(entity, prop.RoleId);
                    }
                    else if (string.Equals(
                            prop.RoleId,
                            "burning_crossbeam",
                            StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(
                                 prop.RoleId,
                                 "crossbow_crossbeam",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        TryAlignProfileCrossbeam(
                            entity,
                            position,
                            prop.UniformScale,
                            prop.RoleId,
                            prefabName);
                        CaptureBurningCrucifixPart(entity, prop.RoleId);
                    }
                    else if (prop.RoleId.StartsWith(
                                 "burning_support_",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        TryAlignBurningSupport(
                            entity,
                            position,
                            prop.RoleId,
                            prefabName);
                        _burningSupportEntities.Add(entity);
                        _burningSupportBaseFrames.Add(entity.GetGlobalFrame());
                    }

                    if (string.Equals(
                            prop.RoleId,
                            "hanging_noose",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // This decoration is intentionally spawned here, after
                        // the full gallows and ceremony marks are established.
                        // The hanging strategy may track it on later mission
                        // ticks, but must never instantiate it from its earlier
                        // SetupSceneVisuals callback.
                        _profileHangingNoose = entity;
                    }

                    RexLog.Info(
                        $"Session {Request.SessionId} visual '{prop.RoleId}': {prefabName}.");
                    return entity;
                }
            }
            catch (Exception exception)
            {
                RexLog.Error(
                    $"Visual '{prop.RoleId}' candidate '{prefabName}' failed; trying the next native candidate.",
                    exception);
            }
        }

        RexLog.Warning(
            $"No prefab candidate was available for visual role '{prop.RoleId}'.");
        return null;
    }

    private bool TryAlignProfileStake(
        GameEntity entity,
        Vec3 desiredBase,
        float uniformScale,
        string roleId,
        string prefabName) =>
        TryAlignProfilePlank(
            entity,
            desiredBase,
            uniformScale,
            roleId,
            prefabName,
            mapLengthToSiteRight: false);

    private bool TryAlignProfileCrossbeam(
        GameEntity entity,
        Vec3 desiredCenter,
        float uniformScale,
        string roleId,
        string prefabName) =>
        TryAlignProfilePlank(
            entity,
            desiredCenter,
            uniformScale,
            roleId,
            prefabName,
            mapLengthToSiteRight: true);

    private bool TryAlignProfilePlank(
        GameEntity entity,
        Vec3 desiredAnchor,
        float uniformScale,
        string roleId,
        string prefabName,
        bool mapLengthToSiteRight)
    {
        try
        {
            if (_placement is null)
            {
                return false;
            }

            var localBounds = entity.GetLocalBoundingBox();
            if (!IsFiniteBounds(localBounds))
            {
                throw new InvalidOperationException(
                    $"Native prefab '{prefabName}' exposed non-finite local visual bounds.");
            }

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
                    $"Native prefab '{prefabName}' did not expose distinct plank axes.");
            }

            var widthAxis = 3 - lengthAxis - thicknessAxis;
            var siteRight = new Vec3(_placement.Right.x, _placement.Right.y, 0f);
            var siteUp = new Vec3(0f, 0f, 1f);
            var axisVectors = new Vec3[3];
            axisVectors[lengthAxis] = mapLengthToSiteRight ? siteRight : siteUp;
            axisVectors[widthAxis] = mapLengthToSiteRight ? siteUp : siteRight;
            axisVectors[thicknessAxis] = thicknessAxis switch
            {
                0 => CrossProduct(axisVectors[1], axisVectors[2]),
                1 => CrossProduct(axisVectors[2], axisVectors[0]),
                _ => CrossProduct(axisVectors[0], axisVectors[1])
            };

            var rotation = default(Mat3);
            rotation.s = axisVectors[0];
            rotation.f = axisVectors[1];
            rotation.u = axisVectors[2];
            var appliedScale = IsFinite(uniformScale) && uniformScale > 0.01f
                ? uniformScale
                : 1f;
            if (MathF.Abs(appliedScale - 1f) > 0.001f)
            {
                rotation.ApplyScaleLocal(appliedScale);
            }

            var mappedLength = sizes[lengthAxis] * appliedScale;
            var desiredCenter = mapLengthToSiteRight
                ? desiredAnchor
                : desiredAnchor + (siteUp * (mappedLength * 0.5f));
            var localCenter = (localBounds.min + localBounds.max) * 0.5f;
            var rotatedCenter = rotation.TransformToParent(in localCenter);
            var frame = new MatrixFrame(rotation, desiredCenter - rotatedCenter);
            entity.SetGlobalFrame(in frame, isTeleportation: true);

            var afterBounds = entity.GetGlobalBoundingBox();
            if (!IsFiniteBounds(afterBounds))
            {
                throw new InvalidOperationException(
                    $"Native prefab '{prefabName}' exposed non-finite visual bounds after recentering.");
            }

            var afterCenter = (afterBounds.min + afterBounds.max) * 0.5f;
            var remainingError = afterCenter.Distance(desiredCenter);
            var bottomError = mapLengthToSiteRight
                ? 0f
                : MathF.Abs(afterBounds.min.z - desiredAnchor.z);
            var message = mapLengthToSiteRight
                ? $"Aligned visual '{roleId}' ({prefabName}) to the execution-site right axis at " +
                  $"shoulder anchor ({desiredCenter.x:0.000},{desiredCenter.y:0.000},{desiredCenter.z:0.000}): " +
                  $"nativeAxes(length/width/thickness)={lengthAxis}/{widthAxis}/{thicknessAxis}, " +
                  $"mappedSize=({mappedLength:0.000} long," +
                  $"{sizes[widthAxis] * appliedScale:0.000} high," +
                  $"{sizes[thicknessAxis] * appliedScale:0.000} thick), " +
                  $"remainingCenterError={remainingError:0.000}m."
                : $"Aligned visual '{roleId}' ({prefabName}) to world up from ground anchor " +
                  $"({desiredAnchor.x:0.000},{desiredAnchor.y:0.000},{desiredAnchor.z:0.000}): " +
                  $"nativeAxes(length/width/thickness)={lengthAxis}/{widthAxis}/{thicknessAxis}, " +
                  $"mappedSize=({mappedLength:0.000} high," +
                  $"{sizes[widthAxis] * appliedScale:0.000} wide," +
                  $"{sizes[thicknessAxis] * appliedScale:0.000} thick), " +
                  $"bottomError={bottomError:0.000}m, remainingCenterError={remainingError:0.000}m.";
            if (remainingError <= 0.03f && (mapLengthToSiteRight || bottomError <= 0.03f))
            {
                RexLog.Info(message);
                return true;
            }

            RexLog.Warning(message);
            return false;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not align visual '{roleId}' ({prefabName}); " +
                "the scene remains active so the placement failure is visible in one test run.",
                exception);
            return false;
        }
    }

    private bool TryAlignBurningSupport(
        GameEntity entity,
        Vec3 groundAnchor,
        string roleId,
        string prefabName)
    {
        try
        {
            if (_placement is null)
            {
                return false;
            }

            var localBounds = entity.GetLocalBoundingBox();
            if (!IsFiniteBounds(localBounds))
            {
                throw new InvalidOperationException(
                    $"Native prefab '{prefabName}' exposed non-finite support bounds.");
            }

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

            if (lengthAxis == thicknessAxis || sizes[lengthAxis] <= 0.01f)
            {
                throw new InvalidOperationException(
                    $"Native prefab '{prefabName}' did not expose a usable support length axis.");
            }

            var widthAxis = 3 - lengthAxis - thicknessAxis;
            var isLeft = roleId.EndsWith("_left", StringComparison.OrdinalIgnoreCase);
            var topRight = isLeft ? -0.20f : 0.20f;
            var topAnchor = _placement.Offset(
                topRight,
                -0.24f,
                _stageSurfaceHeight + 2.20f);
            var supportVector = topAnchor - groundAnchor;
            var supportLength = supportVector.Length;
            if (!IsFinite(supportLength) || supportLength <= 0.10f)
            {
                throw new InvalidOperationException(
                    $"Support '{roleId}' produced an invalid endpoint span.");
            }

            var lengthDirection = supportVector / supportLength;
            var widthDirection = new Vec3(-lengthDirection.y, lengthDirection.x, 0f);
            if (!widthDirection.IsNonZero)
            {
                widthDirection = new Vec3(_placement.Right.x, _placement.Right.y, 0f);
            }

            widthDirection.Normalize();
            var axisVectors = new Vec3[3];
            axisVectors[lengthAxis] = lengthDirection;
            axisVectors[widthAxis] = widthDirection;
            axisVectors[thicknessAxis] = thicknessAxis switch
            {
                0 => CrossProduct(axisVectors[1], axisVectors[2]),
                1 => CrossProduct(axisVectors[2], axisVectors[0]),
                _ => CrossProduct(axisVectors[0], axisVectors[1])
            };

            var rotation = default(Mat3);
            rotation.s = axisVectors[0];
            rotation.f = axisVectors[1];
            rotation.u = axisVectors[2];
            var appliedScale = supportLength / sizes[lengthAxis];
            var localScale = new Vec3(
                appliedScale * BurningSupportCrossSectionScale,
                appliedScale * BurningSupportCrossSectionScale,
                appliedScale * BurningSupportCrossSectionScale);
            if (lengthAxis == 0)
            {
                localScale.x = appliedScale;
            }
            else if (lengthAxis == 1)
            {
                localScale.y = appliedScale;
            }
            else
            {
                localScale.z = appliedScale;
            }

            rotation.ApplyScaleLocal(in localScale);

            var desiredCenter = (groundAnchor + topAnchor) * 0.5f;
            var localCenter = (localBounds.min + localBounds.max) * 0.5f;
            var rotatedCenter = rotation.TransformToParent(in localCenter);
            var frame = new MatrixFrame(rotation, desiredCenter - rotatedCenter);
            entity.SetGlobalFrame(in frame, isTeleportation: true);

            var horizontalRun = supportVector.AsVec2.Length;
            var angleFromGround = MathF.Atan2(
                                      MathF.Abs(supportVector.z),
                                      MathF.Max(0.001f, horizontalRun)) *
                                  (180f / MathF.PI);
            RexLog.Info(
                $"Aligned burning support '{roleId}' ({prefabName}) from ground " +
                $"({groundAnchor.x:0.000},{groundAnchor.y:0.000},{groundAnchor.z:0.000}) to crucifix " +
                $"({topAnchor.x:0.000},{topAnchor.y:0.000},{topAnchor.z:0.000}); " +
                $"angle={angleFromGround:0.0} degrees, length={supportLength:0.000}m, " +
                $"nativeLengthAxis={lengthAxis}, lengthScale={appliedScale:0.000}, " +
                $"crossSectionScale={appliedScale * BurningSupportCrossSectionScale:0.000} " +
                $"({BurningSupportCrossSectionScale:0.00}x of fitted length scale).");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not align burning support '{roleId}' ({prefabName}) to the crucifix; " +
                "the ceremony remains active and the original prefab placement is retained.",
                exception);
            return false;
        }
    }

    private void CaptureBurningCrucifixPart(GameEntity entity, string roleId)
    {
        try
        {
            var frame = entity.GetGlobalFrame();
            if (string.Equals(roleId, "burning_stake", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleId, "crossbow_stake", StringComparison.OrdinalIgnoreCase))
            {
                _burningStakeEntity = entity;
                _burningStakeBaseFrame = frame;
                _burningStakeFrameCaptured = true;
            }
            else if (string.Equals(
                         roleId,
                         "burning_crossbeam",
                         StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                         roleId,
                         "crossbow_crossbeam",
                         StringComparison.OrdinalIgnoreCase))
            {
                _burningCrossbeamEntity = entity;
                _burningCrossbeamBaseFrame = frame;
                _burningCrossbeamFrameCaptured = true;
            }
            else
            {
                return;
            }

            if (!_burningCrucifixReferenceVictimPosition.IsValid && _victimPosition.IsValid)
            {
                // The strategy has already raised VictimPosition to the authored
                // cone-top target before decorations are spawned. Keep that target
                // as the common reference and move both planks by the difference
                // between it and the original Agent's real runtime root.
                _burningCrucifixReferenceVictimPosition = _victimPosition;
            }

            RexLog.Info(
                $"Session {Request.SessionId} captured '{roleId}' crucifix base frame at " +
                $"({frame.origin.x:0.000},{frame.origin.y:0.000},{frame.origin.z:0.000}); " +
                $"victimReference=({_burningCrucifixReferenceVictimPosition.x:0.000}," +
                $"{_burningCrucifixReferenceVictimPosition.y:0.000}," +
                $"{_burningCrucifixReferenceVictimPosition.z:0.000}).");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Could not capture the runtime frame for crucifix part '{roleId}'; " +
                "the fire execution remains active and the failure will be visible in the log.",
                exception);
        }
    }

    private void SynchronizeBurningCrucifixToVictim()
    {
        if ((!_burningStakeFrameCaptured && !_burningCrossbeamFrameCaptured) ||
            !_burningCrucifixReferenceVictimPosition.IsValid)
        {
            return;
        }

        var victim = _victimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        var actualVictimPosition = victim.Position;
        if (!actualVictimPosition.IsValid)
        {
            return;
        }

        var delta = actualVictimPosition - _burningCrucifixReferenceVictimPosition;
        if (!IsFinite(delta.x) || !IsFinite(delta.y) || !IsFinite(delta.z))
        {
            if (!_burningCrucifixSyncFailureLogged)
            {
                _burningCrucifixSyncFailureLogged = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} skipped crucifix synchronization because the " +
                    "original prisoner's runtime root delta was non-finite.");
            }

            return;
        }

        var synchronizedParts = 0;
        if (TrySynchronizeBurningCrucifixPart(
                _burningStakeEntity,
                _burningStakeFrameCaptured,
                _burningStakeBaseFrame,
                delta,
                "burning_stake"))
        {
            synchronizedParts++;
        }

        if (TrySynchronizeBurningCrucifixPart(
                _burningCrossbeamEntity,
                _burningCrossbeamFrameCaptured,
                _burningCrossbeamBaseFrame,
                delta,
                "burning_crossbeam"))
        {
            synchronizedParts++;
        }

        for (var index = 0;
             index < _burningSupportEntities.Count && index < _burningSupportBaseFrames.Count;
             index++)
        {
            if (TrySynchronizeBurningCrucifixPart(
                _burningSupportEntities[index],
                    true,
                    _burningSupportBaseFrames[index],
                    delta,
                    $"burning_support_{index + 1}"))
            {
                synchronizedParts++;
            }
        }

        if (!_burningCrucifixSyncLogged && synchronizedParts > 0)
        {
            _burningCrucifixSyncLogged = true;
            RexLog.Info(
                $"Session {Request.SessionId} synchronized {synchronizedParts}/" +
                $"{2 + _burningSupportEntities.Count} crucifix/support parts " +
                $"to original prisoner Agent {victim.Index}: " +
                $"reference=({_burningCrucifixReferenceVictimPosition.x:0.000}," +
                $"{_burningCrucifixReferenceVictimPosition.y:0.000}," +
                $"{_burningCrucifixReferenceVictimPosition.z:0.000}), " +
                $"actual=({actualVictimPosition.x:0.000},{actualVictimPosition.y:0.000}," +
                $"{actualVictimPosition.z:0.000}), " +
                $"delta=({delta.x:0.000},{delta.y:0.000},{delta.z:0.000}).");
        }
    }

    private bool TrySynchronizeBurningCrucifixPart(
        GameEntity? entity,
        bool frameCaptured,
        MatrixFrame baseFrame,
        Vec3 delta,
        string roleId)
    {
        if (!frameCaptured || entity is null)
        {
            return false;
        }

        try
        {
            if (!entity.HasScene())
            {
                return false;
            }

            var targetFrame = baseFrame;
            targetFrame.origin += delta;
            var currentFrame = entity.GetGlobalFrame();
            if (currentFrame.origin.DistanceSquared(targetFrame.origin) > 0.000001f)
            {
                entity.SetGlobalFrame(in targetFrame, isTeleportation: true);
            }

            return true;
        }
        catch (Exception exception)
        {
            if (!_burningCrucifixSyncFailureLogged)
            {
                _burningCrucifixSyncFailureLogged = true;
                RexLog.Error(
                    $"Could not synchronize crucifix part '{roleId}' to the original prisoner's " +
                    "runtime root; the ceremony remains active.",
                    exception);
            }

            return false;
        }
    }

    private GameEntity? SpawnPrefab(
        string prefabName,
        Vec3 position,
        bool createPhysics,
        bool callScriptCallbacks,
        float yawDegrees = 0f,
        float pitchDegrees = 0f,
        float rollDegrees = 0f,
        float uniformScale = 1f)
    {
        if (!GameEntity.PrefabExists(prefabName) || _placement is null)
        {
            RexLog.Warning($"Native prefab '{prefabName}' is unavailable.");
            return null;
        }

        var frame = _placement.CreateFrame(position);
        const float degreesToRadians = MathF.PI / 180f;
        frame.rotation.RotateAboutUp(yawDegrees * degreesToRadians);
        frame.rotation.RotateAboutSide(pitchDegrees * degreesToRadians);
        frame.rotation.RotateAboutForward(rollDegrees * degreesToRadians);
        if (MathF.Abs(uniformScale - 1f) > 0.001f)
        {
            frame.rotation.ApplyScaleLocal(uniformScale);
        }

        var entity = BannerlordApiCompatibility.InstantiatePrefab(
            Mission.Scene,
            prefabName,
            createPhysics,
            frame,
            callScriptCallbacks);
        if (entity is null)
        {
            RexLog.Warning($"Native prefab '{prefabName}' could not be instantiated.");
            return null;
        }

        // Track the native handle before any post-spawn validation. A prefab can
        // contain child bodies even when InstantiateWithRestOffset was called
        // with createPhysics=false. If disabling those bodies or removing a
        // rejected prefab throws, the handle must remain reachable so the next
        // mission tick and final cleanup can retry instead of leaving an
        // invisible collision shell in the town scene.
        _spawnedEntities.Add(entity);

        if (!createPhysics && !TryDisableEntityCollision(entity, $"visual prefab '{prefabName}'"))
        {
            RexLog.Error(
                $"Visual prefab '{prefabName}' could not be made collision-free and was rejected.");
            RemoveSpawnedEntity(entity);
            return null;
        }

        return entity;
    }

    private bool TryPrepareGroundExecutionSite()
    {
        if (_placement is null || _victimAgent is null)
        {
            return false;
        }

        ResetGallowsStageState();
        _gallowsEntity = null;
        var rawVictimPosition = _placement.Offset(
            GetMethodCeremonyLateralShift(),
            GetMethodVictimForwardShift());
        if (!TrySnapNavigable(
                rawVictimPosition,
                out var groundVictimPosition,
                allowedAgentA: _victimAgent,
                allowedAgentB: _executionerAgent,
                allowedAgentC: _playerAgent,
                ignoreTrackedPositions: true,
                requireDirectLineFromPlacement: false))
        {
            groundVictimPosition = rawVictimPosition;
            RexLog.Warning(
                $"Session {Request.SessionId} kept the selected method ground point after the local " +
                "navigation/body-clearance read-back failed; scene construction continues without a placement cancellation.");
        }

        _victimPosition = groundVictimPosition;
        _frontInteractionOffset = 3.25f;
        PoseAgentAt(_victimAgent, _victimPosition, _placement.Forward);
        RexLog.Info(
            $"Session {Request.SessionId} prepared a platform-free ground execution site at " +
            $"({_victimPosition.x:0.00}, {_victimPosition.y:0.00}, {_victimPosition.z:0.00}); " +
            "the gallows, deck, stairs and hidden ramp were not instantiated.");
        return true;
    }

    private bool TryCreateGallowsStage()
    {
        if (_placement is null || _victimAgent is null)
        {
            return false;
        }

        ResetGallowsStageState();
        GameEntity? gallows = null;
        var stageAccepted = false;
        var groundVictimPosition = _victimPosition;
        try
        {
            // Spawn the complete prefab below the actors, move its root back
            // to the gate ground, then enable its authored collision tree.
            // The visible stepped stairs remain, but their foot-catching body
            // is replaced by a hidden continuous plank ramp before acceptance.
            PoseAgentAt(
                _victimAgent,
                _placement.Offset(0f, 0f, GallowsSpawnDepth),
                _placement.Forward);
            gallows = SpawnPrefab(
                GallowsPrefab,
                _placement.Offset(0f, 0f, -GallowsSpawnDepth),
                createPhysics: true,
                callScriptCallbacks: false);
            if (gallows is null)
            {
                return false;
            }

            gallows.SetMobility(GameEntity.Mobility.Stationary);
            var frame = gallows.GetGlobalFrame();
            frame.origin = _placement.Origin;
            gallows.SetGlobalFrame(in frame, isTeleportation: true);
            _gallowsEntity = gallows;

            if (!TryConfigureGallowsFullCollisionAndBounds(
                    gallows,
                    out var deckBounds,
                    out var stairsBounds) ||
                !TryApplyGallowsDeckGeometry(deckBounds, stairsBounds))
            {
                if (!UsesConfirmedCustomSite)
                {
                    RexLog.Warning(
                        "The full native gallows failed its own prefab collision, deck, stairs or bounds validation.");
                    RemoveSpawnedEntity(gallows);
                    ResetGallowsStageState();
                    return false;
                }

                // A player-confirmed custom root is authoritative. The old automatic-site
                // validator is intentionally stricter than the custom builder contract and
                // must not cancel the mission merely because the town terrain overlaps the
                // prefab bounds. Restore authored collision and use the stable native gallows
                // dimensions measured across previously accepted scenes.
                var collisionRestored = TryEnableCompleteGallowsCollision(gallows);
                _stageSurfaceHeight = 1.68f;
                _stageSideExtent = 6.33f;
                _stageFrontExtent = 5.25f;
                _frontInteractionOffset = 6.20f;
                _gallowsDeckBoundsMin = new Vec3(
                    _placement.Origin.x - 6.50f,
                    _placement.Origin.y - 6.50f,
                    _placement.Origin.z);
                _gallowsDeckBoundsMax = new Vec3(
                    _placement.Origin.x + 6.50f,
                    _placement.Origin.y + 6.50f,
                    _placement.Origin.z + _stageSurfaceHeight);
                _victimPosition = new Vec3(
                    groundVictimPosition.x,
                    groundVictimPosition.y,
                    _placement.Origin.z + _stageSurfaceHeight);
                stageAccepted = true;
                RexLog.Warning(
                    $"Session {Request.SessionId} CUSTOM_GALLOWS_VALIDATION_BYPASSED: " +
                    $"retained the player-confirmed gallows root with fallback deck geometry " +
                    $"(surface={_stageSurfaceHeight:0.00}m, collisionRestored={collisionRestored}); " +
                    "the custom ceremony continues instead of cancelling the mission.");
                return true;
            }

            var deckAgentClear = HasStageEntityAgentClearance(deckBounds);
            var stairsAgentClear = HasStageEntityAgentClearance(stairsBounds);
            var victimDeckResolved = TryResolveGallowsDeckMark(
                groundVictimPosition,
                out var victimDeckPosition,
                allowedAgentA: _victimAgent,
                allowedAgentB: _executionerAgent,
                allowedAgentC: _playerAgent,
                ignoreTrackedPositions: true);
            if (!deckAgentClear || !stairsAgentClear || !victimDeckResolved)
            {
                if (!_placement.IsCompatibilityFallback)
                {
                    RexLog.Warning(
                        "The full native gallows failed live-agent clearance or victim deck-support validation.");
                    RemoveSpawnedEntity(gallows);
                    ResetGallowsStageState();
                    return false;
                }

                if (!victimDeckResolved)
                {
                    var rawVictimDeckPosition = new Vec3(
                        groundVictimPosition.x,
                        groundVictimPosition.y,
                        _placement.Origin.z + _stageSurfaceHeight);
                    victimDeckPosition = HasGallowsDeckPhysicalSupport(
                            rawVictimDeckPosition,
                            out var supportedVictimDeckPosition)
                        ? supportedVictimDeckPosition
                        : rawVictimDeckPosition;
                }

                RexLog.Warning(
                    $"Session {Request.SessionId} retained the native gallows under universal city fallback " +
                    $"(deckAgentClear={deckAgentClear}, stairsAgentClear={stairsAgentClear}, " +
                    $"victimDeckResolved={victimDeckResolved}); the individual failures are logged and no " +
                    "placement cancellation is issued.");
            }

            _victimPosition = victimDeckPosition;
            stageAccepted = true;
            RexLog.Info(
                $"Session {Request.SessionId} full gallows: deck={_stageSideExtent * 2f:0.00}m wide, " +
                $"surface={_stageSurfaceHeight:0.00}m, " +
                $"interactionForward={_frontInteractionOffset:0.00}m.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("The full native gallows could not be created safely.", exception);
            ResetGallowsStageState();
            RemoveSpawnedEntity(gallows);
            return false;
        }
        finally
        {
            var victimPosition = stageAccepted
                ? _victimPosition
                : groundVictimPosition;
            PoseAgentAt(_victimAgent, victimPosition, _placement.Forward);
        }
    }

    /// <summary>
    /// Adds a clean, realistic hanging visual on top of the native gallows:
    /// the prefab's own decorative rope meshes at deck height are suppressed,
    /// then one drop rope is stretched from the beam down to the prisoner's
    /// neck and a noose collar is kept at neck height. This is a pure visual
    /// pass for the hanging ceremony; it never touches the death pipeline.
    /// </summary>
    private void TryCreateHangingRopeVisuals()
    {
        if (_placement is null ||
            _victimAgent is null ||
            _gallowsEntity is null ||
            !string.Equals(
                CurrentMethod.StringId,
                ExecutionMethodRules.Hanging,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TryHideGallowsNativeRopes();
        TryCreateHangingDropRope();
    }

    private void TryHideGallowsNativeRopes()
    {
        if (_gallowsEntity is null)
        {
            return;
        }

        var hidden = 0;
        try
        {
            foreach (var part in _gallowsEntity.GetEntityAndChildren())
            {
                var partName = part.Name;
                var isNativeRope =
                    string.Equals(partName, GallowsNativeRopeA, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(partName, GallowsNativeRopeB, StringComparison.OrdinalIgnoreCase);
                if (!isNativeRope)
                {
                    continue;
                }

                for (var index = 0; index < part.MultiMeshComponentCount; index++)
                {
                    var metaMesh = part.GetMetaMesh(index);
                    if (metaMesh is null || !metaMesh.IsValid)
                    {
                        continue;
                    }

                    metaMesh.SetVisibilityMask((VisibilityMaskFlags)0);
                    hidden++;
                }
            }
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                "Could not hide every native gallows rope mesh; the ceremony proceeds with the prefab visuals. " +
                exception.Message);
        }

        RexLog.Info(
            $"Session {Request.SessionId} hanging visuals: suppressed {hidden} native gallows rope mesh(es).");
    }

    private void TryCreateHangingDropRope()
    {
        if (_placement is null || _victimAgent is null)
        {
            return;
        }

        try
        {
            // Beam: the gallows root is aligned to the placement origin, so the
            // authored beam local height translates directly to world height.
            var beamHeight = _placement.Origin.z + GallowsBeamLocalHeight;
            var neck = _victimAgent.GetEyeGlobalPosition();
            neck.z -= HangingNeckDropFromEye;
            var targetLength = beamHeight - neck.z;
            if (targetLength < HangingDropRopeMinimumLength ||
                targetLength > HangingDropRopeMaximumLength)
            {
                RexLog.Warning(
                    $"Hanging drop rope target length {targetLength:0.00}m is outside the safe range; " +
                    "the drop rope was skipped.");
                return;
            }

            // Native rope prefabs (bd_rope_a/b/b1) are coiled rope heaps, not
            // straight cords. The native "bd_cloth_hanging_rope" prefab is the
            // hanging banner-rope entity (a real vertical cord used as a flag
            // rope in Banner.xml), which matches a beam-to-neck drop rope. It
            // is instantiated as a collision-free static visual; when that
            // prefab is unavailable, the horse rein mesh is copied instead.
            GameEntity? rope = null;
            if (GameEntity.PrefabExists(HangingDropRopeClothPrefab))
            {
                try
                {
                    rope = SpawnPrefab(
                        HangingDropRopeClothPrefab,
                        neck,
                        createPhysics: false,
                        callScriptCallbacks: false);
                }
                catch (Exception exception)
                {
                    RexLog.Warning(
                        $"Hanging drop rope prefab '{HangingDropRopeClothPrefab}' failed; " +
                        "falling back to the rein mesh. " + exception.Message);
                    rope = null;
                }
            }

            if (rope is null)
            {
                var reinMesh = MetaMesh.GetCopy(
                    HangingDropRopeReinMesh,
                    showErrors: false,
                    mayReturnNull: true);
                if (reinMesh is null || !reinMesh.IsValid)
                {
                    RexLog.Warning(
                        $"Hanging drop rope rein mesh '{HangingDropRopeReinMesh}' is unavailable; " +
                        "the drop rope was skipped.");
                    return;
                }

                rope = GameEntity.CreateEmpty(
                    Mission.Scene,
                    isModifiableFromEditor: false,
                    createPhysics: false,
                    callScriptCallbacks: false);
                if (rope is null)
                {
                    RexLog.Warning(
                        "Could not create the hanging drop rope entity; the drop rope was skipped.");
                    return;
                }

                rope.AddMultiMesh(reinMesh);
            }
            rope.SetMobility(GameEntity.Mobility.Stationary);
            if (!_spawnedEntities.Contains(rope))
            {
                _spawnedEntities.Add(rope);
            }

            if (!TryDisableEntityCollision(rope, "hanging drop rope (cloth/rein visual)"))
            {
                RemoveSpawnedEntity(rope);
                return;
            }

            // Measure the real rope bounds, rotate its length axis to
            // world-up, then stretch it to span beam-to-neck around their
            // midpoint so both ends land exactly on the beam and the neck.
            var localMin = rope.GetBoundingBoxMin();
            var localMax = rope.GetBoundingBoxMax();
            var sizeX = MathF.Max(0.0001f, localMax.x - localMin.x);
            var sizeY = MathF.Max(0.0001f, localMax.y - localMin.y);
            var sizeZ = MathF.Max(0.0001f, localMax.z - localMin.z);
            var lengthAxis =
                sizeY >= sizeX && sizeY >= sizeZ
                    ? 1
                    : sizeX >= sizeY && sizeX >= sizeZ
                        ? 0
                        : 2;
            var localLength = lengthAxis == 0
                ? sizeX
                : lengthAxis == 1
                    ? sizeY
                    : sizeZ;
            var meshCenterLocal = new Vec3(
                (localMin.x + localMax.x) * 0.5f,
                (localMin.y + localMax.y) * 0.5f,
                (localMin.z + localMax.z) * 0.5f);
            // Reserve ~8% length slack so the rope's authored end caps (rope
            // head, knotted tail, etc.) do not visibly overshoot the beam or
            // the prisoner's neck on either side.
            var scaleFactor = targetLength / localLength * HangingDropRopeLengthSlack;
            if (!IsFinite(scaleFactor) || scaleFactor <= 0.01f || scaleFactor > 20f)
            {
                RexLog.Warning(
                    $"Hanging drop rope scale factor {scaleFactor:0.00} is invalid; the drop rope was skipped.");
                RemoveSpawnedEntity(rope);
                return;
            }

            // Build a frame whose chosen local axis points world-up, then
            // stretch along that same local axis. Apply scale on the local
            // length axis (not always Z) and compensate the origin for the
            // mesh's off-center local pivot so the visual center lands on
            // beam/neck midpoint, not on the mesh authoring pivot.
            var frame = rope.GetGlobalFrame();
            frame.rotation = Mat3.Identity;
            if (lengthAxis == 0)
            {
                // mesh +X -> world +Z: rotate about forward (mesh Y) by +90°.
                frame.rotation.RotateAboutForward(MathF.PI * 0.5f);
            }
            else if (lengthAxis == 1)
            {
                // mesh +Y -> world +Z: rotate about side (mesh X) by +90°.
                frame.rotation.RotateAboutSide(MathF.PI * 0.5f);
            }
            // lengthAxis == 2: mesh +Z is already world +Z, no rotation.

            var rotatedCenter = frame.rotation.TransformToParent(meshCenterLocal);
            var midZ = (beamHeight + neck.z) * 0.5f;
            frame.origin = new Vec3(neck.x, neck.y, midZ) - rotatedCenter;
            var localScale = lengthAxis == 0
                ? new Vec3(scaleFactor, 1f, 1f)
                : lengthAxis == 1
                    ? new Vec3(1f, scaleFactor, 1f)
                    : new Vec3(1f, 1f, scaleFactor);
            frame.rotation.ApplyScaleLocal(in localScale);
            rope.SetGlobalFrame(in frame, isTeleportation: true);
            _hangingDropRope = rope;
            _hangingRopeBeamAnchor = new Vec3(neck.x, neck.y, beamHeight);
            _hangingRopeBaseLength = localLength;
            RexLog.Info(
                $"Session {Request.SessionId} hanging drop rope: beam={beamHeight:0.00}m, " +
                $"neck={neck.z:0.00}m, length={targetLength:0.00}m, ropeBounds=" +
                $"({localMin.x:0.00},{localMin.y:0.00},{localMin.z:0.00})-" +
                $"({localMax.x:0.00},{localMax.y:0.00},{localMax.z:0.00}), " +
                $"center=({meshCenterLocal.x:0.00},{meshCenterLocal.y:0.00},{meshCenterLocal.z:0.00}), " +
                $"lengthAxis={lengthAxis}, localLength={localLength:0.00}m, scale={scaleFactor:0.00}.");
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                "Could not create the hanging drop rope visual; the ceremony proceeds without it. " +
                exception.Message);
        }
    }

    /// <summary>
    /// Keeps the drop rope's lower end attached to the prisoner's head skeleton
    /// every tick so the rope visually follows the victim (head movement,
    /// struggle and the final lift). The upper end stays anchored at the beam.
    /// This is a pure visual update; it never touches the death pipeline.
    /// </summary>
    private void UpdateHangingRopeToVictim()
    {
        if (_hangingDropRope is null ||
            _victimAgent is null ||
            !_hangingRopeBeamAnchor.IsValid ||
            _hangingRopeBaseLength <= 0.01f)
        {
            return;
        }

        try
        {
            Vec3 head = Vec3.Invalid;
            try
            {
                var visuals = _victimAgent.AgentVisuals;
                var skeleton = visuals is null ? null : visuals.GetSkeleton();
                if (visuals is not null && skeleton is not null && skeleton.IsValid)
                {
                    var headFrame = skeleton.GetBoneEntitialFrameWithName("head");
                    var agentFrame = visuals.GetGlobalFrame();
                    head = agentFrame.TransformToParent(headFrame).origin;
                }
            }
            catch (Exception exception)
            {
                RexLog.Warning(
                    "Could not read the prisoner's head skeleton for the hanging rope; " +
                    "falling back to the eye position. " + exception.Message);
                head = Vec3.Invalid;
            }

            if (!head.IsValid)
            {
                head = _victimAgent.GetEyeGlobalPosition();
            }

            head.z = MathF.Max(head.z, _hangingRopeBeamAnchor.z - HangingDropRopeMaximumLength);
            var targetLength = _hangingRopeBeamAnchor.z - head.z;
            if (targetLength < HangingDropRopeMinimumLength ||
                targetLength > HangingDropRopeMaximumLength)
            {
                return;
            }

            var scaleFactor = targetLength / _hangingRopeBaseLength * HangingDropRopeLengthSlack;
            if (!IsFinite(scaleFactor) || scaleFactor <= 0.01f || scaleFactor > 20f)
            {
                return;
            }

            var frame = _hangingDropRope.GetGlobalFrame();
            frame.origin = new Vec3(
                head.x,
                head.y,
                (_hangingRopeBeamAnchor.z + head.z) * 0.5f);
            frame.rotation = Mat3.Identity;
            frame.rotation.ApplyScaleLocal(new Vec3(1f, scaleFactor, 1f));
            _hangingDropRope.SetGlobalFrame(in frame, isTeleportation: true);
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                "Could not update the hanging drop rope to the prisoner's head; " +
                "the rope stays at its last pose. " + exception.Message);
        }
    }

    /// <summary>
    /// Lifts the prisoner off the deck during the hanging struggle phase so
    /// the drop rope visibly hauls the victim upward. Only the root height
    /// changes; the existing victim root maintenance still keeps the ceremony
    /// point in the horizontal plane.
    /// </summary>
    private void UpdateHangingLift(float dt)
    {
        if (_victimAgent is null || !_victimPosition.IsValid || _placement is null)
        {
            return;
        }

        if (!_hangingLiftPosition.IsValid)
        {
            _hangingLiftPosition = _victimPosition;
        }

        if (_hangingLiftElapsed >= HangingLiftRampSeconds)
        {
            return;
        }

        _hangingLiftElapsed += MathF.Max(0f, dt);
        var progress = MathF.Min(1f, _hangingLiftElapsed / HangingLiftRampSeconds);
        var eased = SmoothStep(progress);
        var liftedHeight = _victimPosition.z + HangingLiftHeight * eased;
        var liftedPosition = new Vec3(
            _victimPosition.x,
            _victimPosition.y,
            liftedHeight);
        _hangingLiftPosition = liftedPosition;
        PoseAgentAt(_victimAgent, liftedPosition, _placement.Forward);
    }

    /// <summary>
    /// Pins the killed prisoner at the lifted, suspended height so the body
    /// stays hanging on the rope instead of collapsing to the deck. Native
    /// corpse-pool registration is never forced; once Bannerlord reports the
    /// victim as added to its corpse pool, further position writes stop to
    /// avoid the v0.3.31 native crash. All monitoring stays read-only.
    /// </summary>
    private void MaintainHangingSuspension(float dt)
    {
        if (!_hangingSuspensionActive ||
            _victimAgent is null ||
            !_hangingLiftPosition.IsValid)
        {
            return;
        }

        _hangingSuspensionProbeElapsed += MathF.Max(0f, dt);
        if (_hangingSuspensionProbeElapsed < HangingSuspensionProbeIntervalSeconds)
        {
            return;
        }

        _hangingSuspensionProbeElapsed = 0f;
        try
        {
            if (_victimAgent.IsAddedAsCorpse() || _victimAgent.IsFadingOut())
            {
                _hangingSuspensionActive = false;
                RexLog.Info(
                    $"Session {Request.SessionId} stopped pinning the hung prisoner because " +
                    "Bannerlord took over its native corpse presentation.");
                return;
            }

            if (_victimAgent.State != AgentState.Killed)
            {
                return;
            }

            _victimAgent.TeleportToPosition(_hangingLiftPosition);
        }
        catch (Exception exception)
        {
            _hangingSuspensionActive = false;
            RexLog.Warning(
                "Stopped pinning the hung prisoner after a suspension error; " +
                "the native corpse pipeline proceeds. " + exception.Message);
        }
    }

}
