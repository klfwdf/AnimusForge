using System;
using System.Collections.Generic;
using RichExecutions.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// A physically simulated severed head used solely by this module's scripted
/// beheading. It deliberately does not patch combat or campaign death code:
/// the campaign execution remains owned by <see cref="KillCharacterAction"/>.
/// </summary>
internal sealed class DetachedHeadVisual
{
    private const string PhysicsShapeTemplate = "bo_axe_short";
    private const string HeadPhysicsMaterialName = "flesh";
    private const float MinimumHeadCollisionRadius = 0.17f;
    private const float MaximumHeadCollisionRadius = 0.21f;
    private const float HeadCollisionPadding = 0.012f;
    private const float EyeToCenterBackwardOffset = 0.065f;
    private const float EyeToCenterDownwardOffset = 0.060f;
    private const float HeadMass = 4.2f;
    private const float InitialForwardSpeed = 0.22f;
    private const float InitialSideSpeed = 0.04f;
    private const float InitialUpwardSpeed = 0.12f;
    private const float InitialPitchSpeed = 1.35f;
    private const float InitialRollSpeed = 0.40f;
    private const float MaximumLinearSpeed = 2.4f;
    private const float MaximumAngularSpeed = 4.0f;
    private const float MaximumDepenetrationSpeed = 2.50f;
    private const float HeadBodyRestOffset = 0.010f;
    private const float InitialFloorClearance = 0.015f;
    private const float MaximumInitialFloorLift = 0.35f;
    private const float HeadLinearDamping = 0.18f;
    private const float HeadAngularDamping = 0.22f;
    private const float MinimumValidHeadRootOffset = -0.25f;
    private const float MaximumValidHeadRootOffset = 3.00f;

    private readonly GameEntity _entity;
    // Keep the copied native resource wrapper alive for as long as the entity
    // owns the dynamic rigid body.
    private readonly PhysicsShape _physicsShape;
    private readonly IReadOnlyList<HiddenVisualMesh> _hiddenSourceComponents;
    private bool _sourceHeadVisibilityRestored;

    private DetachedHeadVisual(
        GameEntity entity,
        PhysicsShape physicsShape,
        IReadOnlyList<HiddenVisualMesh> hiddenSourceComponents)
    {
        _entity = entity;
        _physicsShape = physicsShape;
        _hiddenSourceComponents = hiddenSourceComponents;
    }

    public GameEntity Entity => _entity;

    /// <summary>
    /// Restores the source agent's head meshes when a lethal-frame transaction
    /// is cancelled before it commits. Successful executions intentionally do
    /// not call this: the source prisoner must remain visually headless while
    /// the detached proxy is visible.
    /// </summary>
    public void RestoreSourceHead()
    {
        if (_sourceHeadVisibilityRestored)
        {
            return;
        }

        var restored = true;
        foreach (var hidden in _hiddenSourceComponents)
        {
            try
            {
                if (!hidden.TryRestore())
                {
                    restored = false;
                }
            }
            catch (Exception exception)
            {
                restored = false;
                RexLog.Error(
                    "Could not restore a source head mesh after a cancelled detached-head setup.",
                    exception);
            }
        }

        _sourceHeadVisibilityRestored = restored;
    }

    public static bool TryCreate(
        Mission mission,
        Agent victim,
        float stageSurfaceWorldZ,
        out DetachedHeadVisual? detachedHead)
    {
        detachedHead = null;
        if (mission is null || victim is null || !victim.IsActive())
        {
            return false;
        }

        var visuals = victim.AgentVisuals;
        if (visuals is null || !visuals.IsValid())
        {
            RexLog.Warning("The beheading victim has no valid native visuals; skipped detached-head proxy.");
            return false;
        }

        GameEntity? sourceEntity;
        Skeleton? sourceSkeleton;
        try
        {
            sourceEntity = visuals.GetEntity();
            sourceSkeleton = visuals.GetSkeleton();
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not read the beheading victim visual hierarchy.", exception);
            return false;
        }

        if (sourceEntity is null || sourceSkeleton is null || !sourceSkeleton.IsValid)
        {
            RexLog.Warning("The beheading victim visual hierarchy has no usable skeleton; skipped detached-head proxy.");
            return false;
        }

        var headComponents = FindHeadVisualSources(
            sourceEntity,
            sourceSkeleton,
            out var hasBaseHeadMesh);
        if (!hasBaseHeadMesh || headComponents.Count == 0)
        {
            RexLog.Warning(
                "No separable native head mesh component was found on the beheading victim; " +
                "the normal corpse is retained instead.");
            return false;
        }

        GameEntity? proxyEntity = null;
        var hiddenComponents = new List<HiddenVisualMesh>();
        try
        {
            var skeletonName = sourceSkeleton.GetName();
            if (string.IsNullOrWhiteSpace(skeletonName) ||
                !Skeleton.SkeletonModelExist(skeletonName))
            {
                RexLog.Warning(
                    "The beheading victim skeleton model is unavailable for a detached-head proxy; " +
                    "the normal corpse is retained instead.");
                return false;
            }

            sourceSkeleton.ForceUpdateBoneFrames();
            var sourceFrame = sourceEntity.GetGlobalFrame();
            var headWorldPosition = victim.GetEyeGlobalPosition();
            var hasCurrentHeadBasis = TryGetCurrentHeadBasisLocal(
                sourceSkeleton,
                victim.Monster.HeadLookDirectionBoneIndex,
                out var headForwardLocal,
                out var headUpLocal);
            proxyEntity = GameEntity.CreateEmpty(
                mission.Scene,
                isModifiableFromEditor: false,
                createPhysics: false,
                callScriptCallbacks: false);
            var proxySkeleton = Skeleton.CreateFromModelWithNullAnimTree(
                proxyEntity,
                skeletonName,
                boneScale: 1f);
            if (proxySkeleton is null || !proxySkeleton.IsValid)
            {
                throw new InvalidOperationException("The detached-head skeleton could not be created.");
            }

            proxyEntity.Skeleton = proxySkeleton;
            CopyCurrentPose(sourceSkeleton, proxySkeleton);
            var copiedHeadMeshes = new List<MetaMesh>(headComponents.Count);
            foreach (var component in headComponents)
            {
                var copiedMesh = component.CreateCopy();
                proxyEntity.AddMultiMeshToSkeleton(copiedMesh);
                copiedHeadMeshes.Add(copiedMesh);
            }

            proxyEntity.SetMobility(GameEntity.Mobility.Dynamic);
            proxySkeleton.Freeze(true);

            if (!TryResolveCollisionGeometry(
                    copiedHeadMeshes,
                    sourceFrame,
                    headWorldPosition,
                    headForwardLocal,
                    headUpLocal,
                    hasCurrentHeadBasis,
                    out var localHeadCenter,
                    out var collisionRadius,
                    out var geometrySource))
            {
                throw new InvalidOperationException("The detached-head collision geometry was invalid.");
            }

            var proxyFrame = sourceFrame;
            ApplyInitialFloorClearance(
                ref proxyFrame,
                localHeadCenter,
                collisionRadius,
                stageSurfaceWorldZ);
            proxyEntity.SetGlobalFrame(in proxyFrame, isTeleportation: true);

            var eyeLocal = sourceFrame.TransformToLocal(headWorldPosition);
            var eyeToCenterLocal = localHeadCenter - eyeLocal;
            var sphereWorldCenter = proxyFrame.TransformToParent(in localHeadCenter);

            if (!TryCreateDynamicPhysics(
                    proxyEntity,
                    proxyFrame,
                    localHeadCenter,
                    collisionRadius,
                    geometrySource,
                    out var physicsShape) ||
                physicsShape is null)
            {
                throw new InvalidOperationException(
                    "The detached-head proxy could not create a dynamic rigid body.");
            }

            var sourceHeadInstances = FindHeadSkeletonVisualInstances(
                sourceSkeleton,
                out var sourceSkeletonHasBaseHeadMesh);
            if (!sourceSkeletonHasBaseHeadMesh || sourceHeadInstances.Count == 0)
            {
                // Some custom visual skeletons expose only their component-level
                // MetaMeshes. Keep that layout working, but prefer the individual
                // skeleton mesh instances above so separate eyes and teeth are not
                // left visible on the retained body.
                sourceHeadInstances = headComponents;
            }

            foreach (var component in sourceHeadInstances)
            {
                hiddenComponents.Add(component.Hide());
            }

            detachedHead = new DetachedHeadVisual(
                proxyEntity,
                physicsShape,
                hiddenComponents);
            RexLog.Info(
                $"Created a visual detached-head proxy from {headComponents.Count} native head mesh component(s), " +
                $"hiding {hiddenComponents.Count} source head/face instance(s) and using only copied native head visuals " +
                $"for victim agent {victim.Index} with a native dynamic sphere body " +
                $"(eyeLocal=({eyeLocal.x:0.000}, {eyeLocal.y:0.000}, {eyeLocal.z:0.000}), " +
                $"centerLocal=({localHeadCenter.x:0.000}, {localHeadCenter.y:0.000}, {localHeadCenter.z:0.000}), " +
                $"eyeToCenterLocal=({eyeToCenterLocal.x:0.000}, {eyeToCenterLocal.y:0.000}, {eyeToCenterLocal.z:0.000}), " +
                $"centerWorld=({sphereWorldCenter.x:0.000}, {sphereWorldCenter.y:0.000}, {sphereWorldCenter.z:0.000}), " +
                $"headForwardLocal=({headForwardLocal.x:0.000}, {headForwardLocal.y:0.000}, {headForwardLocal.z:0.000}), " +
                $"headUpLocal=({headUpLocal.x:0.000}, {headUpLocal.y:0.000}, {headUpLocal.z:0.000}), " +
                $"radius={collisionRadius:0.000}m, stageSurfaceZ={stageSurfaceWorldZ:0.000}, source={geometrySource}).");
            return true;
        }
        catch (Exception exception)
        {
            foreach (var hidden in hiddenComponents)
            {
                try
                {
                    hidden.TryRestore();
                }
                catch (Exception restoreException)
                {
                    RexLog.Error("Could not restore a head mesh after detached-head setup failed.", restoreException);
                }
            }

            TryRemoveProxyEntity(proxyEntity);
            RexLog.Error(
                "Detached-head setup failed; restored the original victim head and continued the normal execution.",
                exception);
            return false;
        }
    }

    public static bool TryHideHeadComponents(Agent victim)
    {
        if (victim is null || !victim.IsActive())
        {
            return false;
        }

        try
        {
            var visuals = victim.AgentVisuals;
            if (visuals is null || !visuals.IsValid())
            {
                return false;
            }

            var source = visuals.GetEntity();
            var skeleton = visuals.GetSkeleton();
            if (source is null || skeleton is null || !skeleton.IsValid)
            {
                return false;
            }

            var entityComponents = FindHeadOnlyComponents(
                source,
                out var entityHasBaseHeadMesh);
            var hiddenSkeletonMeshCount = HideEveryHeadSkeletonMesh(
                skeleton,
                out var skeletonHasBaseHeadMesh);
            if ((!entityHasBaseHeadMesh && !skeletonHasBaseHeadMesh) ||
                (entityComponents.Count == 0 && hiddenSkeletonMeshCount == 0))
            {
                return false;
            }

            foreach (var component in entityComponents)
            {
                new HeadVisualSource(component.Mesh).Hide();
            }

            RexLog.Info(
                $"Hidden {entityComponents.Count} entity and {hiddenSkeletonMeshCount} skeleton " +
                $"head/face/eye/mouth components on source victim agent {victim.Index}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not hide every head mesh on the source victim Agent.", exception);
            return false;
        }
    }

    private static int HideEveryHeadSkeletonMesh(
        Skeleton skeleton,
        out bool hasBaseHeadMesh)
    {
        hasBaseHeadMesh = false;
        var hiddenCount = 0;
        foreach (var mesh in skeleton.GetAllMeshes())
        {
            if (mesh is null || !mesh.IsValid)
            {
                continue;
            }

            var meshName = NormalizeName(mesh.Name);
            if (string.IsNullOrWhiteSpace(meshName) ||
                IsBodyOrEquipmentLabel(meshName) ||
                !IsHeadVisualLabel(meshName))
            {
                continue;
            }

            // Do not deduplicate by resource name here. Left/right eyes and
            // other facial instances can share one source resource while each
            // keeps its own visibility mask on the retained skeleton.
            new HeadVisualSource(mesh, mesh.Name).Hide();
            hiddenCount++;
            hasBaseHeadMesh |= IsBaseHeadLabel(meshName);
        }

        return hiddenCount;
    }

    private static List<HeadVisualSource> FindHeadVisualSources(
        GameEntity root,
        Skeleton skeleton,
        out bool hasBaseHeadMesh)
    {
        // Prefer the skeleton inventory. A single entity-level head component can
        // coexist with independent eye, pupil and teeth meshes on the skeleton;
        // returning early from the entity list used to omit those facial parts
        // from both the detached proxy and the source-body hide set.
        var skeletonSources = FindHeadOnlySkeletonMeshes(skeleton, out hasBaseHeadMesh);
        if (hasBaseHeadMesh && skeletonSources.Count != 0)
        {
            return skeletonSources;
        }

        var entityComponents = FindHeadOnlyComponents(root, out hasBaseHeadMesh);
        var componentSources = new List<HeadVisualSource>(entityComponents.Count);
        foreach (var component in entityComponents)
        {
            componentSources.Add(new HeadVisualSource(component.Mesh));
        }

        return componentSources;
    }

    private static List<HeadVisualSource> FindHeadOnlySkeletonMeshes(
        Skeleton skeleton,
        out bool hasBaseHeadMesh)
    {
        hasBaseHeadMesh = false;
        var sources = new List<HeadVisualSource>();
        var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mesh in skeleton.GetAllMeshes())
        {
            if (mesh is null || !mesh.IsValid)
            {
                continue;
            }

            var sourceName = mesh.Name;
            var meshName = NormalizeName(sourceName);
            if (string.IsNullOrWhiteSpace(meshName) ||
                IsBodyOrEquipmentLabel(meshName) ||
                !IsHeadVisualLabel(meshName))
            {
                continue;
            }

            var resourceName = GetMetaMeshResourceName(sourceName);
            if (string.IsNullOrWhiteSpace(resourceName) ||
                !resourceNames.Add(resourceName))
            {
                continue;
            }

            sources.Add(new HeadVisualSource(mesh, resourceName));
            hasBaseHeadMesh |= IsBaseHeadLabel(meshName);
        }

        return sources;
    }

    private static List<HeadVisualSource> FindHeadSkeletonVisualInstances(
        Skeleton skeleton,
        out bool hasBaseHeadMesh)
    {
        hasBaseHeadMesh = false;
        var sources = new List<HeadVisualSource>();
        foreach (var mesh in skeleton.GetAllMeshes())
        {
            if (mesh is null || !mesh.IsValid)
            {
                continue;
            }

            var sourceName = mesh.Name;
            var meshName = NormalizeName(sourceName);
            if (string.IsNullOrWhiteSpace(meshName) ||
                IsBodyOrEquipmentLabel(meshName) ||
                !IsHeadVisualLabel(meshName))
            {
                continue;
            }

            // Do not deduplicate here. Left/right facial parts can share a
            // resource name while carrying independent visibility masks.
            sources.Add(new HeadVisualSource(mesh, GetMetaMeshResourceName(sourceName)));
            hasBaseHeadMesh |= IsBaseHeadLabel(meshName);
        }

        return sources;
    }

    private static List<HeadMeshComponent> FindHeadOnlyComponents(
        GameEntity root,
        out bool hasBaseHeadMesh)
    {
        hasBaseHeadMesh = false;
        var components = new List<HeadMeshComponent>();
        var entities = new List<GameEntity> { root };
        foreach (var entity in root.GetEntityAndChildren())
        {
            if (!ReferenceEquals(entity, root))
            {
                entities.Add(entity);
            }
        }

        foreach (var entity in entities)
        {
            for (var index = 0; index < entity.MultiMeshComponentCount; index++)
            {
                var metaMesh = entity.GetMetaMesh(index);
                if (metaMesh is null || !metaMesh.IsValid ||
                    !IsHeadOnlyComponent(metaMesh, out var componentHasBaseHeadMesh))
                {
                    continue;
                }

                components.Add(new HeadMeshComponent(metaMesh));
                hasBaseHeadMesh |= componentHasBaseHeadMesh;
            }
        }

        return components;
    }

    private static bool IsHeadOnlyComponent(MetaMesh metaMesh, out bool hasBaseHeadMesh)
    {
        hasBaseHeadMesh = false;
        var componentName = NormalizeName(metaMesh.GetName());
        if (IsBodyOrEquipmentLabel(componentName))
        {
            return false;
        }

        var hasHeadLabel = IsHeadVisualLabel(componentName);
        hasBaseHeadMesh = IsBaseHeadLabel(componentName);
        if (metaMesh.MeshCount <= 0)
        {
            return hasHeadLabel;
        }

        for (var index = 0; index < metaMesh.MeshCount; index++)
        {
            var mesh = metaMesh.GetMeshAtIndex(index);
            var meshName = NormalizeName(mesh?.Name);
            if (string.IsNullOrWhiteSpace(meshName) || IsBodyOrEquipmentLabel(meshName) ||
                !IsHeadVisualLabel(meshName))
            {
                return false;
            }

            hasHeadLabel = true;
            hasBaseHeadMesh |= IsBaseHeadLabel(meshName);
        }

        return hasHeadLabel;
    }

    private static string GetMetaMeshResourceName(string sourceMeshName)
    {
        if (string.IsNullOrWhiteSpace(sourceMeshName))
        {
            return string.Empty;
        }

        var name = sourceMeshName.Trim();
        var lodIndex = name.IndexOf(".lod", StringComparison.OrdinalIgnoreCase);
        if (lodIndex > 0)
        {
            name = name.Substring(0, lodIndex);
        }

        var variantIndex = name.IndexOf('.');
        return variantIndex > 0 ? name.Substring(0, variantIndex) : name;
    }

    private static string NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value!.Trim().ToLowerInvariant();

    private static bool IsHeadVisualLabel(string label) =>
        label.Contains("head") ||
        label.Contains("face") ||
        label.Contains("hair") ||
        label.Contains("beard") ||
        label.Contains("eyebrow") ||
        label.Contains("eye") ||
        label.Contains("pupil") ||
        label.Contains("teeth") ||
        label.Contains("tooth") ||
        label.Contains("mouth") ||
        label.Contains("tongue") ||
        label.Contains("helmet") ||
        label.Contains("coif") ||
        label.Contains("hood") ||
        label.Contains("turban") ||
        label.Contains("hat") ||
        label.Contains("cap");

    private static bool IsBaseHeadLabel(string label) =>
        (label.Contains("head") || label.Contains("face")) &&
        !label.Contains("helmet") &&
        !label.Contains("coif") &&
        !label.Contains("hood") &&
        !label.Contains("turban") &&
        !label.Contains("hat") &&
        !label.Contains("cap");

    private static bool IsBodyOrEquipmentLabel(string label) =>
        label.Contains("body") ||
        label.Contains("torso") ||
        label.Contains("chest") ||
        label.Contains("shoulder") ||
        label.Contains("arm") ||
        label.Contains("hand") ||
        label.Contains("leg") ||
        label.Contains("foot") ||
        label.Contains("boot") ||
        label.Contains("glove") ||
        label.Contains("cape") ||
        label.Contains("weapon") ||
        label.Contains("horse");

    private static void CopyCurrentPose(Skeleton source, Skeleton destination)
    {
        var boneCount = Math.Min((int)source.GetBoneCount(), (int)destination.GetBoneCount());
        for (var index = 0; index < boneCount; index++)
        {
            var bone = (sbyte)index;
            var entitialFrame = source.GetBoneEntitialFrameWithIndex(bone);
            var parent = source.GetParentBoneIndex(bone);
            var localFrame = parent >= 0
                ? source.GetBoneEntitialFrameWithIndex(parent).TransformToLocal(in entitialFrame)
                : entitialFrame;
            destination.SetBoneLocalFrame(bone, localFrame);
        }

        destination.UpdateEntitialFramesFromLocalFrames();
        destination.ForceUpdateBoneFrames();
    }

    private static bool TryResolveCollisionGeometry(
        IReadOnlyList<MetaMesh> copiedHeadMeshes,
        MatrixFrame sourceFrame,
        Vec3 headWorldPosition,
        Vec3 headForwardLocal,
        Vec3 headUpLocal,
        bool hasCurrentHeadBasis,
        out Vec3 localHeadCenter,
        out float collisionRadius,
        out string geometrySource)
    {
        // MetaMesh bounds are resource/rest-pose data. They are reliable for
        // physical size, but not for the current kneeling/death pose. Always
        // locate the body from the live eye position, then use mesh bounds only
        // to size the sphere.
        // The eye point is in the front/upper half of the visible head. A
        // sphere centred only behind the eyes leaves the jaw below its contact
        // envelope, so the rigid body can be physically resting while the
        // visible chin is already inside the deck. Follow the live head bone's
        // forward and up axes so the sphere sits inside the cranium in any pose.
        localHeadCenter = sourceFrame.TransformToLocal(headWorldPosition) -
                          headForwardLocal * EyeToCenterBackwardOffset -
                          headUpLocal * EyeToCenterDownwardOffset;
        if (!IsFinite(localHeadCenter))
        {
            collisionRadius = MinimumHeadCollisionRadius;
            geometrySource = "invalid-current-eye";
            return false;
        }

        // Do not clamp this current-pose coordinate to standing-person heights.
        // A kneeling/downward-looking head can legitimately be low; moving only
        // the sphere would desynchronise it from the frozen visible skeleton.
        if (localHeadCenter.z < MinimumValidHeadRootOffset ||
            localHeadCenter.z > MaximumValidHeadRootOffset)
        {
            collisionRadius = MinimumHeadCollisionRadius;
            geometrySource = "implausible-current-head-root-offset";
            return false;
        }

        collisionRadius = 0.18f;
        geometrySource = hasCurrentHeadBasis
            ? "current-eye+current-head-basis+mesh-bounds-radius"
            : "current-eye+root-basis+mesh-bounds-radius";

        var minimum = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maximum = new Vec3(float.MinValue, float.MinValue, float.MinValue);
        var hasBounds = false;
        foreach (var metaMesh in copiedHeadMeshes)
        {
            if (metaMesh is null || !metaMesh.IsValid)
            {
                continue;
            }

            BoundingBox bounds;
            try
            {
                bounds = metaMesh.GetBoundingBox();
            }
            catch (Exception exception)
            {
                RexLog.Warning(
                    $"Could not read a copied head mesh bound; remaining meshes will still be used ({exception.Message}).");
                continue;
            }

            if (!IsFinite(bounds.min) || !IsFinite(bounds.max) ||
                bounds.max.x < bounds.min.x ||
                bounds.max.y < bounds.min.y ||
                bounds.max.z < bounds.min.z)
            {
                continue;
            }

            var componentFrame = metaMesh.Frame;
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var z = 0; z < 2; z++)
                    {
                        var corner = new Vec3(
                            x == 0 ? bounds.min.x : bounds.max.x,
                            y == 0 ? bounds.min.y : bounds.max.y,
                            z == 0 ? bounds.min.z : bounds.max.z);
                        var transformed = componentFrame.TransformToParent(in corner);
                        if (!IsFinite(transformed))
                        {
                            continue;
                        }

                        minimum = Vec3.Vec3Min(minimum, transformed);
                        maximum = Vec3.Vec3Max(maximum, transformed);
                        hasBounds = true;
                    }
                }
            }
        }

        if (hasBounds)
        {
            var size = maximum - minimum;
            var largestHalfExtent = MathF.Max(
                size.x,
                MathF.Max(size.y, size.z)) * 0.5f;
            if (IsFinite(size) &&
                size.x > 0.04f && size.y > 0.04f && size.z > 0.04f &&
                size.x < 1.20f && size.y < 1.20f && size.z < 1.20f)
            {
                collisionRadius = MathF.Max(
                    MinimumHeadCollisionRadius,
                    MathF.Min(
                        MaximumHeadCollisionRadius,
                        largestHalfExtent + HeadCollisionPadding));
                RexLog.Info(
                    $"Detached-head visual bounds size=({size.x:0.000}, {size.y:0.000}, {size.z:0.000}); " +
                    $"derived collision radius from copied native meshes while retaining the current-pose eye center.");
                return true;
            }

            RexLog.Warning(
                $"Copied head mesh bounds were implausible " +
                $"(size=({size.x:0.000}, {size.y:0.000}, {size.z:0.000})); " +
                $"using the corrected current-eye center and fallback radius {collisionRadius:0.000}m.");
        }

        geometrySource = hasCurrentHeadBasis
            ? "current-eye+current-head-basis+fallback-radius"
            : "current-eye+root-basis+fallback-radius";
        return IsFinite(localHeadCenter);
    }

    private static bool TryGetCurrentHeadBasisLocal(
        Skeleton skeleton,
        sbyte headBoneIndex,
        out Vec3 headForwardLocal,
        out Vec3 headUpLocal)
    {
        headForwardLocal = Vec3.Forward;
        headUpLocal = Vec3.Up;
        if (headBoneIndex < 0)
        {
            RexLog.Warning("The victim skeleton has no head-look bone; using root forward/up for head physics.");
            return false;
        }

        try
        {
            var headFrame = skeleton.GetBoneEntitialFrameWithIndex(headBoneIndex);
            headForwardLocal = headFrame.rotation.f;
            headUpLocal = headFrame.rotation.u;
            if (!TryNormalizeDirection(ref headForwardLocal) ||
                !TryNormalizeDirection(ref headUpLocal))
            {
                headForwardLocal = Vec3.Forward;
                headUpLocal = Vec3.Up;
                RexLog.Warning("The current head basis was invalid; using root forward/up for head physics.");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            headForwardLocal = Vec3.Forward;
            headUpLocal = Vec3.Up;
            RexLog.Warning(
                $"Could not read the current head basis; using root forward/up ({exception.Message}).");
            return false;
        }
    }

    private static bool TryNormalizeDirection(ref Vec3 direction)
    {
        if (!IsFinite(direction) || direction.LengthSquared < 0.0001f)
        {
            return false;
        }

        direction /= MathF.Sqrt(direction.LengthSquared);
        return true;
    }

    private static void ApplyInitialFloorClearance(
        ref MatrixFrame proxyFrame,
        Vec3 localHeadCenter,
        float collisionRadius,
        float stageSurfaceWorldZ)
    {
        if (float.IsNaN(stageSurfaceWorldZ) || float.IsInfinity(stageSurfaceWorldZ))
        {
            return;
        }

        var sphereWorldCenter = proxyFrame.TransformToParent(in localHeadCenter);
        if (!IsFinite(sphereWorldCenter))
        {
            return;
        }

        var requiredCenterZ = stageSurfaceWorldZ + collisionRadius +
                              HeadBodyRestOffset + InitialFloorClearance;
        var requestedLift = MathF.Max(0f, requiredCenterZ - sphereWorldCenter.z);
        if (requestedLift <= 0.0001f)
        {
            return;
        }

        var appliedLift = MathF.Min(requestedLift, MaximumInitialFloorLift);
        proxyFrame.origin.z += appliedLift;
        RexLog.Warning(
            $"Raised the complete detached-head proxy by {appliedLift:0.000}m before physics " +
            $"to clear the gallows deck (requested={requestedLift:0.000}m, " +
            $"capped={requestedLift > MaximumInitialFloorLift}).");
    }

    private static bool IsFinite(Vec3 value) =>
        !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
        !float.IsNaN(value.z) && !float.IsInfinity(value.z);

    private static bool TryCreateDynamicPhysics(
        GameEntity entity,
        MatrixFrame sourceFrame,
        Vec3 localHeadCenter,
        float collisionRadius,
        string geometrySource,
        out PhysicsShape? physicsShape)
    {
        physicsShape = null;
        try
        {
            var template = PhysicsShape.GetFromResource(
                PhysicsShapeTemplate,
                mayReturnNull: true);
            if (template is null)
            {
                RexLog.Warning(
                    $"The native physics shape template '{PhysicsShapeTemplate}' was unavailable.");
                return false;
            }

            physicsShape = template.CreateCopy();
            physicsShape.Clear();
            physicsShape.InitDescription();
            physicsShape.AddSphere(new SphereData(collisionRadius, localHeadCenter));
            physicsShape.Prepare();

            var forward = sourceFrame.rotation.f;
            forward.z = 0f;
            if (forward.LengthSquared < 0.0001f)
            {
                forward = Vec3.Forward;
            }
            else
            {
                forward /= MathF.Sqrt(forward.LengthSquared);
            }

            var right = new Vec3(-forward.y, forward.x, 0f);
            var initialVelocity =
                forward * InitialForwardSpeed +
                right * InitialSideSpeed +
                Vec3.Up * InitialUpwardSpeed;
            var angularVelocity =
                right * InitialPitchSpeed +
                forward * InitialRollSpeed;

            entity.RemoveBodyFlags(BodyFlags.Disabled, applyToChildren: true);
            entity.AddBodyFlags(
                BodyFlags.DroppedItem |
                BodyFlags.Moveable |
                BodyFlags.NotDestructible |
                BodyFlags.BodyOwnerEntity,
                applyToChildren: false);
            var physicsMaterial = PhysicsMaterial.GetFromName(HeadPhysicsMaterialName);
            if (!physicsMaterial.IsValid)
            {
                RexLog.Warning(
                    $"Native physics material '{HeadPhysicsMaterialName}' was unavailable; using the engine default.");
                physicsMaterial = PhysicsMaterial.InvalidPhysicsMaterial;
            }

            entity.AddPhysics(
                HeadMass,
                localHeadCenter,
                physicsShape,
                initialVelocity,
                angularVelocity,
                physicsMaterial,
                isStatic: false,
                collisionGroupID: -1);
            entity.SetPhysicsStateOnlyVariable(isEnabled: true, setChildren: true);
            entity.SetDamping(HeadLinearDamping, HeadAngularDamping);
            entity.SetSolverIterationCounts(positionIterationCount: 8, velocityIterationCount: 3);
            entity.SetMaxDepenetrationVelocity(MaximumDepenetrationSpeed);
            try
            {
                BannerlordApiCompatibility.TryApplyBodyRestOffset(entity, HeadBodyRestOffset);
            }
            catch (Exception exception)
            {
                RexLog.Warning(
                    $"Could not apply the detached-head body rest offset; physics continues ({exception.Message}).");
            }

            entity.SetVelocityLimits(
                maxLinearVelocity: MaximumLinearSpeed,
                maxAngularVelocity: MaximumAngularSpeed);
            if (!entity.HasDynamicRigidBody())
            {
                throw new InvalidOperationException(
                    "The engine did not create a dynamic rigid body for the detached head.");
            }

            RexLog.Info(
                $"Detached-head physics configured: radius={collisionRadius:0.000}m, mass={HeadMass:0.00}kg, " +
                $"material={(physicsMaterial.IsValid ? physicsMaterial.Name : "<engine-default>")}, " +
                $"geometry={geometrySource}, " +
                $"initialLinear=({initialVelocity.x:0.000}, {initialVelocity.y:0.000}, {initialVelocity.z:0.000})/" +
                $"{initialVelocity.Length:0.000}m/s, " +
                $"initialAngular=({angularVelocity.x:0.000}, {angularVelocity.y:0.000}, {angularVelocity.z:0.000})/" +
                $"{angularVelocity.Length:0.000}rad/s, " +
                $"velocityLimits={MaximumLinearSpeed:0.0}/{MaximumAngularSpeed:0.0}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not configure native detached-head physics.", exception);
            physicsShape = null;
            return false;
        }
    }

    private static void TryRemoveProxyEntity(GameEntity? entity)
    {
        if (entity is null)
        {
            return;
        }

        try
        {
            if (entity.HasScene())
            {
                entity.AddBodyFlags(BodyFlags.Disabled, applyToChildren: true);
                entity.SetPhysicsState(isEnabled: false, setChildren: true);
                entity.SetVisibilityExcludeParents(false);
                entity.Remove(removeReason: 0);
            }
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not remove a failed detached-head proxy.", exception);
        }
    }

    private readonly struct HeadMeshComponent
    {
        public HeadMeshComponent(MetaMesh mesh)
        {
            Mesh = mesh;
        }

        public MetaMesh Mesh { get; }
    }

    private readonly struct HeadVisualSource
    {
        private readonly MetaMesh? _metaMesh;
        private readonly Mesh? _sourceMesh;
        private readonly string? _resourceName;

        public HeadVisualSource(MetaMesh metaMesh)
        {
            _metaMesh = metaMesh;
            _sourceMesh = null;
            _resourceName = null;
        }

        public HeadVisualSource(Mesh sourceMesh, string resourceName)
        {
            _metaMesh = null;
            _sourceMesh = sourceMesh;
            _resourceName = resourceName;
        }

        public MetaMesh CreateCopy()
        {
            if (_metaMesh is not null)
            {
                var copiedComponent = _metaMesh.CreateCopy();
                copiedComponent.Frame = _metaMesh.Frame;
                return copiedComponent;
            }

            if (_sourceMesh is null || string.IsNullOrWhiteSpace(_resourceName))
            {
                throw new InvalidOperationException("A detached-head mesh source was incomplete.");
            }

            var copiedResource = MetaMesh.GetCopy(
                _resourceName,
                showErrors: false,
                mayReturnNull: true);
            if (copiedResource is null || !copiedResource.IsValid)
            {
                throw new InvalidOperationException(
                    $"The native head MetaMesh '{_resourceName}' could not be copied.");
            }

            for (var index = 0; index < copiedResource.MeshCount; index++)
            {
                var copiedMesh = copiedResource.GetMeshAtIndex(index);
                if (copiedMesh is null || !copiedMesh.IsValid)
                {
                    continue;
                }

                copiedMesh.Color = _sourceMesh.Color;
                copiedMesh.Color2 = _sourceMesh.Color2;
            }

            return copiedResource;
        }

        public HiddenVisualMesh Hide()
        {
            if (_metaMesh is not null)
            {
                var originalMask = _metaMesh.GetVisibilityMask();
                _metaMesh.SetVisibilityMask((VisibilityMaskFlags)0);
                return new HiddenVisualMesh(_metaMesh, originalMask);
            }

            if (_sourceMesh is null)
            {
                throw new InvalidOperationException("A detached-head mesh source could not be hidden.");
            }

            var sourceMask = _sourceMesh.VisibilityMask;
            _sourceMesh.SetVisibilityMask((VisibilityMaskFlags)0);
            return new HiddenVisualMesh(_sourceMesh, sourceMask);
        }
    }

    private readonly struct HiddenVisualMesh
    {
        private readonly MetaMesh? _metaMesh;
        private readonly Mesh? _mesh;
        private readonly VisibilityMaskFlags _originalMask;

        public HiddenVisualMesh(MetaMesh mesh, VisibilityMaskFlags originalMask)
        {
            _metaMesh = mesh;
            _mesh = null;
            _originalMask = originalMask;
        }

        public HiddenVisualMesh(Mesh mesh, VisibilityMaskFlags originalMask)
        {
            _metaMesh = null;
            _mesh = mesh;
            _originalMask = originalMask;
        }

        public bool TryRestore()
        {
            if (_metaMesh is not null)
            {
                if (_metaMesh.IsValid)
                {
                    _metaMesh.SetVisibilityMask(_originalMask);
                }

                return true;
            }

            if (_mesh is not null && _mesh.IsValid)
            {
                _mesh.SetVisibilityMask(_originalMask);
            }

            return true;
        }
    }
}
