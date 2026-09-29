using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RichExecutions.Customization;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private const string ImpaledCorpseDisplayMesh = "verjianchi02";
    private const float ImpaledCorpseDisplayLength = 5.60f;
    private const float ImpaledCorpseDisplayWidthScale = 0.82f;

    private sealed class ImpaledCorpseDisplayState
    {
        internal ImpaledCorpseDisplayState(
            Agent agent,
            Vec3 root,
            Vec2 direction,
            ActionIndexCache action,
            string actionName,
            float actionProgress)
        {
            Agent = agent;
            Root = root;
            Direction = direction;
            Action = action;
            ActionName = actionName;
            ActionProgress = actionProgress;
        }

        internal Agent Agent { get; }
        internal Vec3 Root { get; }
        internal Vec2 Direction { get; }
        internal ActionIndexCache Action { get; }
        internal string ActionName { get; }
        internal float ActionProgress { get; }
        internal bool FailureLogged { get; set; }
    }

    private readonly List<ImpaledCorpseDisplayState> _impaledCorpseDisplays = new();

    private bool UsesConfirmedCustomSite =>
        Request.SceneMode == ExecutionSceneMode.CustomPreset &&
        _customSiteLayoutConfirmed &&
        _customSitePreset is not null &&
        _placement is not null;

    private ExecutionSceneVisualProfile BuildCustomSiteVisualProfile(
        ExecutionSceneVisualProfile source)
    {
        if (!UsesConfirmedCustomSite || _customSitePreset is null)
        {
            return source;
        }

        var sourceRoles = new HashSet<string>(
            source.Props.Select(prop => prop.RoleId),
            StringComparer.OrdinalIgnoreCase);
        var props = new List<ExecutionScenePropPlacement>();
        foreach (var sourceProp in source.Props)
        {
            var piece = _customSitePreset.Pieces.LastOrDefault(candidate =>
                candidate.ResourceType == ExecutionSiteResourceType.Prefab &&
                string.Equals(candidate.RoleId, sourceProp.RoleId, StringComparison.OrdinalIgnoreCase));
            props.Add(piece is null
                ? sourceProp
                : CreateCustomProp(piece, sourceProp.RoleId, sourceProp.IsIgnitionEffect, sourceProp.CallScriptCallbacks));
        }


        var interaction = source.InteractionPoint;
        if (TryGetCustomMarker("player_interaction", out var interactionMarker))
        {
            interaction = new ExecutionSceneRelativePoint(
                interactionMarker.Transform.X,
                interactionMarker.Transform.Y,
                interactionMarker.Transform.Z);
        }

        RexLog.Info(
            $"Session {Request.SessionId} built a custom visual profile from preset {_customSitePreset.Id:D}: " +
            $"props={props.Count}, interaction=({interaction.Right:0.00},{interaction.Forward:0.00},{interaction.Up:0.00}).");
        return new ExecutionSceneVisualProfile(
            source.MethodId,
            source.Fidelity,
            source.InteractionNameText,
            source.InteractionActionText,
            interaction,
            source.IgniteAtLethalFrame,
            props.ToArray());
    }

    private static ExecutionScenePropPlacement CreateCustomProp(
        ExecutionSitePiece piece,
        string roleId,
        bool isIgnition,
        bool callScriptCallbacks) => new(
        roleId,
        new[] { piece.ResourceKey },
        piece.Transform.X,
        piece.Transform.Y,
        piece.Transform.Z,
        piece.Transform.Yaw,
        piece.Transform.Pitch,
        piece.Transform.Roll,
        piece.Transform.Scale,
        isIgnition,
        callScriptCallbacks,
        piece.Collision != ExecutionSiteCollisionPolicy.Disabled);

    private bool TryGetCustomMarker(string roleId, out ExecutionSiteMarker marker)
    {
        marker = null!;
        return UsesConfirmedCustomSite &&
               _customSiteMarkers is not null &&
               _customSiteMarkers.TryGetValue(roleId, out marker!);
    }

    private bool TryGetCustomMarkerWorld(
        string roleId,
        out Vec3 position,
        out Vec2 forward)
    {
        position = Vec3.Invalid;
        forward = Vec2.Forward;
        if (_placement is null || !TryGetCustomMarker(roleId, out var marker))
        {
            return false;
        }

        position = _placement.Offset(
            marker.Transform.X,
            marker.Transform.Y,
            marker.Transform.Z);
        forward = RotateDirection(_placement.Forward, marker.Transform.Yaw);
        return position.IsValid && forward.IsNonZero();
    }

    private static Vec2 RotateDirection(Vec2 direction, float degrees)
    {
        var angle = degrees * MathF.PI / 180f;
        var rotated = new Vec2(
            direction.x * MathF.Cos(angle) - direction.y * MathF.Sin(angle),
            direction.x * MathF.Sin(angle) + direction.y * MathF.Cos(angle));
        return rotated.IsNonZero() ? rotated.Normalized() : Vec2.Forward;
    }

    private IReadOnlyList<Vec3> GetCustomMarkerWorldPositions(string prefix)
    {
        if (!UsesConfirmedCustomSite || _customSiteMarkers is null)
        {
            return Array.Empty<Vec3>();
        }

        return _customSiteMarkers
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => _placement!.Offset(
                pair.Value.Transform.X,
                pair.Value.Transform.Y,
                pair.Value.Transform.Z))
            .Where(position => position.IsValid && Mission.IsPositionInsideBoundaries(position.AsVec2))
            .ToList();
    }

    private CharacterObject? SelectExecutionerTroopForCurrentSite() =>
        UsesConfirmedCustomSite
            ? CultureTroopSelector.SelectPresetExecutioner(
                Request,
                _customSitePreset?.Troops.ExecutionerCharacterId)
            : CultureTroopSelector.SelectCeremonyExecutioner(Request);

    private CharacterObject? SelectMeleeGuardTroopForCurrentSite(int slot) =>
        UsesConfirmedCustomSite
            ? CultureTroopSelector.SelectPresetGuard(
                Request,
                _customSitePreset?.Troops.MeleeGuardCharacterId,
                slot)
            : CultureTroopSelector.SelectCeremonyGuard(Request, slot);

    private CharacterObject? SelectRangedGuardTroopForCurrentSite(int slot) =>
        UsesConfirmedCustomSite
            ? CultureTroopSelector.SelectPresetCrossbowman(
                Request,
                _customSitePreset?.Troops.RangedGuardCharacterId,
                slot)
            : CultureTroopSelector.SelectCeremonyCrossbowman(Request, slot);

    private void SpawnCustomSiteLoosePieces()
    {
        if (!UsesConfirmedCustomSite || _customSitePreset is null || _placement is null)
        {
            return;
        }

        var profileRoles = ExecutionSceneVisualProfiles.TryGet(CurrentMethod.StringId, out var profile)
            ? new HashSet<string>(profile.Props.Select(prop => prop.RoleId), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var piece in _customSitePreset.Pieces)
        {
            if (ExecutionSitePresetStore.IsObsoletePiece(piece))
            {
                // v0.5.5 merged the former verjianchi01 bottom Mesh into the
                // single verjianchi02 apparatus Mesh; legacy presets that still
                // list it must not spawn a second, now missing, entity.
                continue;
            }

            if (ImpaledCorpseDisplayStore.TryParseRoleId(piece.RoleId, out _))
            {
                SpawnImpaledCorpseDisplay(piece);
                continue;
            }
            if (string.Equals(
                    piece.RoleId,
                    ExecutionSiteCrucifixDecoration.RoleId,
                    StringComparison.OrdinalIgnoreCase))
            {
                SpawnCustomCrucifixDecoration(piece);
                continue;
            }

            if (string.Equals(piece.RoleId, "site_stage_gallows", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(piece.RoleId) && profileRoles.Contains(piece.RoleId!)) ||
                string.Equals(piece.RoleId, "impalement_base", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(piece.RoleId, "impalement_spike", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var position = _placement.Offset(piece.Transform.X, piece.Transform.Y, piece.Transform.Z);
            if (piece.ResourceType == ExecutionSiteResourceType.Prefab)
            {
                var prefab = SpawnPrefab(
                    piece.ResourceKey,
                    position,
                    piece.Collision != ExecutionSiteCollisionPolicy.Disabled,
                    callScriptCallbacks: false,
                    piece.Transform.Yaw,
                    piece.Transform.Pitch,
                    piece.Transform.Roll,
                    piece.Transform.Scale);
                if (prefab is not null)
                {
                    RexLog.Info(
                        $"Spawned loose custom Prefab '{piece.ResourceKey}' role='{piece.RoleId ?? "<decoration>"}' at exact preset coordinates.");
                }
                continue;
            }

            GameEntity? entity = null;
            try
            {
                var mesh = MetaMesh.GetCopy(piece.ResourceKey, showErrors: false, mayReturnNull: true);
                if (mesh is null || !mesh.IsValid)
                {
                    RexLog.Warning($"Custom Mesh '{piece.ResourceKey}' is unavailable; continuing with remaining pieces.");
                    continue;
                }

                entity = GameEntity.CreateEmpty(Mission.Scene, false, false, false);
                if (entity is null) continue;
                entity.AddMultiMesh(mesh);
                var frame = _placement.CreateFrame(position);
                frame.rotation.RotateAboutUp(piece.Transform.Yaw * MathF.PI / 180f);
                frame.rotation.RotateAboutSide(piece.Transform.Pitch * MathF.PI / 180f);
                frame.rotation.RotateAboutForward(piece.Transform.Roll * MathF.PI / 180f);
                frame.rotation.ApplyScaleLocal(MathF.Max(0.10f, MathF.Min(5f, piece.Transform.Scale)));
                entity.SetGlobalFrame(frame);
                entity.SetVisibilityExcludeParents(true);
                _spawnedEntities.Add(entity);
                RexLog.Info($"Spawned loose custom Mesh '{piece.ResourceKey}' at exact preset coordinates.");
            }
            catch (Exception exception)
            {
                RexLog.Error($"Custom Mesh '{piece.ResourceKey}' failed to spawn; continuing.", exception);
                try { entity?.Remove(0); } catch { }
                if (entity is not null) _spawnedEntities.Remove(entity);
            }
        }
    }

    private void SpawnCustomCrucifixDecoration(ExecutionSitePiece piece)
    {
        if (_placement is null)
        {
            return;
        }

        GameEntity? crucifix = null;
        try
        {
            crucifix = ExecutionSiteCrucifixDecoration.Create(
                Mission.Scene,
                createPhysics: piece.Collision != ExecutionSiteCollisionPolicy.Disabled,
                preview: false);
            if (crucifix is null)
            {
                return;
            }

            var position = _placement.Offset(
                piece.Transform.X,
                piece.Transform.Y,
                piece.Transform.Z);
            var frame = _placement.CreateFrame(position);
            const float degreesToRadians = MathF.PI / 180f;
            frame.rotation.RotateAboutUp(piece.Transform.Yaw * degreesToRadians);
            frame.rotation.RotateAboutSide(piece.Transform.Pitch * degreesToRadians);
            frame.rotation.RotateAboutForward(piece.Transform.Roll * degreesToRadians);
            frame.rotation.ApplyScaleLocal(MathF.Max(0.10f, MathF.Min(5f, piece.Transform.Scale)));
            crucifix.SetGlobalFrame(in frame, isTeleportation: true);
            foreach (var entity in crucifix.GetEntityAndChildren())
            {
                try { entity.SetMobility(GameEntity.Mobility.Stationary); } catch { }
            }
            _spawnedEntities.Add(crucifix);
            RexLog.Info(
                $"Spawned composite crucifix decoration at ({position.x:0.00},{position.y:0.00},{position.z:0.00}) " +
                $"with scale {piece.Transform.Scale:0.00}.");
        }
        catch (Exception exception)
        {
            RexLog.Error("Composite crucifix decoration failed to spawn; the ceremony remains active.", exception);
            try { crucifix?.Remove(0); } catch { }
            if (crucifix is not null) _spawnedEntities.Remove(crucifix);
        }
    }

    private ImpaledCorpseDisplaySnapshot? _pendingImpaledCorpseDisplay;

    private bool SaveImpaledCorpseDisplay(string requestedActionName, float requestedActionProgress)
    {
        var victim = _victimAgent;
        if (victim is null || !victim.IsActive())
        {
            RexLog.Warning("Could not save the impaled-corpse decoration because the original prisoner Agent is unavailable.");
            return false;
        }

        try
        {
            var currentAction = victim.GetCurrentAction(0);
            var actualActionName = currentAction != ActionIndexCache.act_none
                ? currentAction.GetName()
                : requestedActionName;
            if (string.IsNullOrWhiteSpace(actualActionName))
            {
                actualActionName = requestedActionName;
            }

            var actualProgress = victim.GetCurrentActionProgress(0);
            if (float.IsNaN(actualProgress) || float.IsInfinity(actualProgress) || actualProgress < 0f)
            {
                actualProgress = requestedActionProgress;
            }
            if (float.IsNaN(actualProgress) || float.IsInfinity(actualProgress) || actualProgress < 0f)
            {
                actualProgress = 0f;
            }
            actualProgress = MathF.Max(0f, MathF.Min(1f, actualProgress));

            var snapshot = new ImpaledCorpseDisplaySnapshot
            {
                Id = Guid.NewGuid(),
                HeroCharacterId = Request.Victim.CharacterObject.StringId,
                DisplayName = Request.Victim.Name.ToString(),
                BodyProperties = victim.BodyPropertiesValue.ToString(),
                EquipmentCode = victim.SpawnEquipment.CalculateEquipmentCode(),
                IsFemale = victim.IsFemale,
                Age = Math.Max(18, Math.Min(90, (int)MathF.Round(victim.Age))),
                Race = victim.Character.Race,
                ClothingColor1 = victim.ClothingColor1,
                ClothingColor2 = victim.ClothingColor2,
                ActionName = actualActionName,
                ActionProgress = actualProgress,
                CapturedUtc = DateTime.UtcNow.ToString("O")
            };
            // Capture while the Agent exists, but publish only after campaign
            // death succeeds. Failed or cancelled ceremonies must leave no trophy.
            _pendingImpaledCorpseDisplay = snapshot;
            RexLog.Info(
                $"Staged impaled-corpse decoration for hero '{snapshot.HeroCharacterId}': " +
                $"action='{snapshot.ActionName}', progress={snapshot.ActionProgress:0.000}, " +
                $"equipmentLength={snapshot.EquipmentCode.Length}; awaiting campaign execution commit.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not capture the original prisoner's exact impaled-corpse decoration.", exception);
            return false;
        }
    }

    private void CommitPendingImpaledCorpseDisplay()
    {
        if (_outcome?.Success != true || !_outcome.DeathCommitted)
        {
            return;
        }

        var snapshot = _pendingImpaledCorpseDisplay;
        _pendingImpaledCorpseDisplay = null;
        if (snapshot is null)
        {
            return;
        }

        if (ImpaledCorpseDisplayStore.TrySave(snapshot, out var saveReference))
        {
            RexLog.Info($"Published impaled-corpse decoration '{saveReference}' after campaign execution commit.");
        }
        else
        {
            RexLog.Warning("Could not publish the impaled-corpse decoration; campaign execution is already committed.");
        }
    }

    private void SpawnImpaledCorpseDisplay(ExecutionSitePiece piece)
    {
        if (_placement is null || _executionTeam is null)
        {
            RexLog.Warning("Skipped the impaled-corpse display because placement or ceremony team data was unavailable.");
            return;
        }

        if (!ImpaledCorpseDisplayStore.TryParseRoleId(piece.RoleId, out var snapshotId) ||
            !ImpaledCorpseDisplayStore.TryLoad(snapshotId, out var snapshot))
        {
            RexLog.Warning(
                $"Skipped impaled-corpse decoration role '{piece.RoleId ?? "<none>"}' because its saved snapshot is unavailable.");
            return;
        }

        CharacterObject? character;
        BodyProperties bodyProperties;
        Equipment equipment;
        ActionIndexCache action;
        try
        {
            character = Game.Current?.ObjectManager.GetObject<CharacterObject>(snapshot.HeroCharacterId);
            if (character is null || !character.IsHero)
            {
                RexLog.Warning(
                    $"Skipped impaled-corpse decoration {snapshot.Id:D}: exact hero CharacterObject " +
                    $"'{snapshot.HeroCharacterId}' is unavailable; no substitute will be generated.");
                return;
            }
            if (!BodyProperties.FromString(snapshot.BodyProperties, out bodyProperties))
            {
                RexLog.Warning(
                    $"Skipped impaled-corpse decoration {snapshot.Id:D}: its exact body properties could not be parsed.");
                return;
            }

            equipment = Equipment.CreateFromEquipmentCode(snapshot.EquipmentCode);
            action = ActionIndexCache.Create(snapshot.ActionName);
            if (action == ActionIndexCache.act_none)
            {
                RexLog.Warning(
                    $"Skipped impaled-corpse decoration {snapshot.Id:D}: exact action '{snapshot.ActionName}' is unavailable.");
                return;
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Skipped impaled-corpse decoration {snapshot.Id:D}: exact appearance or action data could not be restored.",
                exception);
            return;
        }

        var basePosition = _placement.Offset(piece.Transform.X, piece.Transform.Y, piece.Transform.Z);
        var direction = RotateDirection(_placement.Forward, piece.Transform.Yaw);
        GameEntity? pole = null;
        Agent? corpse = null;
        try
        {
            var mesh = MetaMesh.GetCopy(ImpaledCorpseDisplayMesh, showErrors: false, mayReturnNull: true);
            if (mesh is null || !mesh.IsValid)
            {
                RexLog.Warning($"Impaled-corpse display Mesh '{ImpaledCorpseDisplayMesh}' was unavailable.");
                return;
            }

            pole = GameEntity.CreateEmpty(Mission.Scene, false, false, false);
            if (pole is null)
            {
                return;
            }

            pole.AddMultiMesh(mesh);
            var min = pole.GetBoundingBoxMin();
            var max = pole.GetBoundingBoxMax();
            var extents = max - min;
            var localCenter = (min + max) * 0.5f;
            var lengthAxis = LongestDisplayAxis(extents);
            var nativeLength = MathF.Max(0.05f, DisplayAxisExtent(extents, lengthAxis));
            var lengthScale = ImpaledCorpseDisplayLength / nativeLength;
            var rotation = BuildDisplayPoleRotation(Vec3.Up, lengthAxis, direction);
            rotation.ApplyScaleLocal(BuildDisplayPoleScale(
                lengthAxis,
                lengthScale,
                lengthScale * ImpaledCorpseDisplayWidthScale));
            var visibleStart = basePosition - Vec3.Up * 1.05f;
            var visibleCenter = visibleStart + Vec3.Up * (ImpaledCorpseDisplayLength * 0.5f);
            var rotatedCenter = rotation.TransformToParent(in localCenter);
            var poleFrame = new MatrixFrame(rotation, visibleCenter - rotatedCenter);
            pole.SetGlobalFrame(in poleFrame, isTeleportation: true);
            pole.SetMobility(GameEntity.Mobility.Stationary);
            pole.SetVisibilityExcludeParents(true);
            TryDisableEntityCollision(pole, "custom impaled-corpse display pole");
            _spawnedEntities.Add(pole);

            var corpseRoot = basePosition + Vec3.Up * 2.25f;
            corpse = SpawnCharacter(
                character,
                corpseRoot,
                direction,
                civilianEquipment: false,
                noWeapons: false,
                invulnerable: true,
                equipmentOverride: equipment,
                bodyPropertiesOverride: bodyProperties,
                teamOverride: _executionTeam,
                femaleOverride: snapshot.IsFemale,
                ageOverride: snapshot.Age,
                raceOverride: snapshot.Race,
                clothingColor1Override: snapshot.ClothingColor1,
                clothingColor2Override: snapshot.ClothingColor2,
                fixedEquipment: true,
                originOverride: new BasicBattleAgentOrigin(character));
            corpse.Controller = AgentControllerType.None;
            corpse.SetIsAIPaused(true);
            corpse.SetMortalityState(Agent.MortalityState.Invulnerable);
            corpse.SetIsPhysicsForceClosed(false);
            corpse.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
            var up = Vec3.Up;
            var forward = new Vec3(direction.x, direction.y, 0f);
            corpse.SetTargetZ(corpseRoot.z);
            corpse.SetTargetPositionAndDirection(corpseRoot.AsVec2, forward);
            corpse.SetTargetUp(in up);

            var actionProgress = MathF.Max(0f, MathF.Min(1f, snapshot.ActionProgress));
            if (!TryBindImpaledCorpseDisplayAction(corpse, action, actionProgress))
            {
                throw new InvalidOperationException(
                    $"Exact saved action '{snapshot.ActionName}' was rejected by the spawned hero Agent.");
            }

            AddUninteractableDisplayAgent(corpse);
            _impaledCorpseDisplays.Add(new ImpaledCorpseDisplayState(
                corpse,
                corpseRoot,
                direction,
                action,
                snapshot.ActionName,
                actionProgress));
            RexLog.Info(
                $"Spawned exact impaled-corpse decoration for hero '{character.StringId}', " +
                $"action='{snapshot.ActionName}', progress={actionProgress:0.000}, " +
                $"equipmentLength={snapshot.EquipmentCode.Length}, collision-free pole at " +
                $"({basePosition.x:0.00},{basePosition.y:0.00},{basePosition.z:0.00}).");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"Exact impaled-corpse decoration {snapshot.Id:D} failed to spawn; no substitute was generated.",
                exception);
            if (corpse is not null)
            {
                RetireProvisionalVictimAgent(corpse, "its saved appearance or action could not be restored exactly");
            }
            if (pole is not null)
            {
                RemoveSpawnedEntity(pole);
            }
        }
    }

    private static bool TryBindImpaledCorpseDisplayAction(
        Agent corpse,
        ActionIndexCache action,
        float actionProgress)
    {
        var none = ActionIndexCache.act_none;
        corpse.SetActionChannel(
            0,
            in none,
            ignorePriority: true,
            additionalFlags: AnimFlags.anf_restart);
        var accepted = corpse.SetActionChannel(
            0,
            in action,
            ignorePriority: true,
            additionalFlags: AnimFlags.anf_restart |
                             AnimFlags.anf_lock_movement |
                             AnimFlags.anf_enforce_all |
                             AnimFlags.anf_enforce_root_rotation |
                             AnimFlags.anf_disable_hand_ik |
                             AnimFlags.anf_disable_foot_ik |
                             AnimFlags.anf_disable_agent_agent_collisions,
            blendWithNextActionFactor: 0f,
            actionSpeed: 0f,
            blendInPeriod: 0.40f,
            blendOutPeriodToNoAnim: 0f,
            startProgress: actionProgress);
        if (!accepted || corpse.GetCurrentAction(0) != action)
        {
            return false;
        }

        corpse.SetCurrentActionProgress(0, actionProgress);
        corpse.SetCurrentActionSpeed(0, 0f);
        return true;
    }

    private void TickImpaledCorpseDisplays(float dt)
    {
        foreach (var display in _impaledCorpseDisplays)
        {
            var agent = display.Agent;
            if (agent is null || !agent.IsActive())
            {
                continue;
            }

            try
            {
                agent.Controller = AgentControllerType.None;
                agent.SetIsAIPaused(true);
                agent.SetIsPhysicsForceClosed(false);
                agent.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
                var forward = new Vec3(display.Direction.x, display.Direction.y, 0f);
                var up = Vec3.Up;
                agent.SetTargetZ(display.Root.z);
                agent.SetTargetPositionAndDirection(display.Root.AsVec2, forward);
                agent.SetTargetUp(in up);
                agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, FrozenVictimClosedEyesFacialAction, true);
                agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, string.Empty, false);
                agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Low, string.Empty, false);

                if (agent.GetCurrentAction(0) != display.Action &&
                    !TryBindImpaledCorpseDisplayAction(agent, display.Action, display.ActionProgress))
                {
                    throw new InvalidOperationException(
                        $"Saved action '{display.ActionName}' could not be rebound on channel 0.");
                }
                agent.SetCurrentActionProgress(0, display.ActionProgress);
                agent.SetCurrentActionSpeed(0, 0f);
            }
            catch (Exception exception)
            {
                if (!display.FailureLogged)
                {
                    display.FailureLogged = true;
                    RexLog.Warning(
                        $"Stopped updating custom impaled-corpse display Agent {agent.Index}; " +
                        $"the rest of the ceremony remains active. {exception.Message}");
                }
            }
        }
    }

    private void AddUninteractableDisplayAgent(Agent agent)
    {
        try
        {
            var field = _conversationLogic?.GetType().GetField(
                "_uninteractableAgents",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(_conversationLogic) is ICollection<Agent> agents && !agents.Contains(agent))
            {
                agents.Add(agent);
            }
        }
        catch (Exception exception)
        {
            RexLog.Warning(
                $"Could not add impaled-corpse display Agent {agent.Index} to the native conversation exclusion set. " +
                exception.Message);
        }
    }

    private static int LongestDisplayAxis(Vec3 extents) =>
        extents.x >= extents.y && extents.x >= extents.z ? 0 : extents.y >= extents.z ? 1 : 2;

    private static float DisplayAxisExtent(Vec3 extents, int axis) =>
        axis == 0 ? extents.x : axis == 1 ? extents.y : extents.z;

    private static Vec3 BuildDisplayPoleScale(int lengthAxis, float lengthScale, float crossScale) =>
        lengthAxis switch
        {
            0 => new Vec3(lengthScale, crossScale, crossScale),
            2 => new Vec3(crossScale, crossScale, lengthScale),
            _ => new Vec3(crossScale, lengthScale, crossScale)
        };

    private static Mat3 BuildDisplayPoleRotation(Vec3 direction, int localLengthAxis, Vec2 referenceForward)
    {
        direction.Normalize();
        var reference = new Vec3(referenceForward.x, referenceForward.y, 0f);
        reference = (reference - direction * Vec3.DotProduct(reference, direction)).NormalizedCopy();
        if (localLengthAxis == 0)
        {
            var up = Vec3.CrossProduct(direction, reference).NormalizedCopy();
            return new Mat3(direction, reference, up);
        }
        if (localLengthAxis == 2)
        {
            var side = Vec3.CrossProduct(reference, direction).NormalizedCopy();
            return new Mat3(side, reference, direction);
        }

        var sideAxis = Vec3.CrossProduct(direction, reference).NormalizedCopy();
        return new Mat3(sideAxis, direction, reference);
    }
}
