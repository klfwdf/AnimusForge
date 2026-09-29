using System;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RichExecutions.Customization;

internal static class ExecutionSiteCrucifixDecoration
{
    internal const string ResourceId = "decor_crucifix";
    internal const string RoleId = "decor_crucifix";
    internal const string PrefabName = "wooden_platform_2_plank_a";

    private const float StakeScale = 0.90f;
    private const float CrossbeamScale = 0.62f;
    private const float CrossbeamHeight = 2.38f;

    internal static GameEntity? Create(TaleWorlds.Engine.Scene scene, bool createPhysics, bool preview)
    {
        GameEntity? parent = null;
        GameEntity? stake = null;
        GameEntity? crossbeam = null;
        try
        {
            if (!GameEntity.PrefabExists(PrefabName))
            {
                RexLog.Warning($"Crucifix decoration prefab '{PrefabName}' is unavailable.");
                return null;
            }

            parent = GameEntity.CreateEmpty(scene, false, false, false);
            stake = InstantiatePlank(scene, createPhysics);
            crossbeam = InstantiatePlank(scene, createPhysics);
            if (parent is null || stake is null || crossbeam is null)
            {
                throw new InvalidOperationException("A crucifix decoration entity could not be created.");
            }

            parent.AddChild(stake, false);
            parent.AddChild(crossbeam, false);
            var stakeFrame = BuildPlankFrame(stake, Vec3.Zero, horizontal: false, StakeScale);
            var crossbeamFrame = BuildPlankFrame(
                crossbeam,
                new Vec3(0f, 0f, CrossbeamHeight),
                horizontal: true,
                CrossbeamScale);
            stake.SetLocalFrame(ref stakeFrame, isTeleportation: true);
            crossbeam.SetLocalFrame(ref crossbeamFrame, isTeleportation: true);

            foreach (var entity in parent.GetEntityAndChildren())
            {
                if (!createPhysics)
                {
                    try { entity.SetPhysicsState(false, true); } catch { }
                    try { entity.RemovePhysics(false); } catch { }
                }
                if (preview)
                {
                    try { entity.SetAlpha(0.56f); } catch { }
                }
                try { entity.SetVisibilityExcludeParents(true); } catch { }
            }

            return parent;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not create the composite crucifix decoration.", exception);
            try { parent?.Remove(0); } catch { }
            try { stake?.Remove(0); } catch { }
            try { crossbeam?.Remove(0); } catch { }
            return null;
        }
    }

    private static GameEntity? InstantiatePlank(TaleWorlds.Engine.Scene scene, bool createPhysics) =>
        BannerlordApiCompatibility.InstantiatePrefab(
            scene,
            PrefabName,
            createPhysics,
            MatrixFrame.Identity,
            callScriptCallbacks: false);

    private static MatrixFrame BuildPlankFrame(
        GameEntity entity,
        Vec3 desiredAnchor,
        bool horizontal,
        float scale)
    {
        var bounds = entity.GetLocalBoundingBox();
        var size = bounds.max - bounds.min;
        var sizes = new[] { size.x, size.y, size.z };
        var lengthAxis = 0;
        var thicknessAxis = 0;
        for (var axis = 1; axis < 3; axis++)
        {
            if (sizes[axis] > sizes[lengthAxis]) lengthAxis = axis;
            if (sizes[axis] < sizes[thicknessAxis]) thicknessAxis = axis;
        }
        if (lengthAxis == thicknessAxis || sizes[lengthAxis] <= 0.01f)
        {
            throw new InvalidOperationException(
                $"Crucifix plank '{PrefabName}' did not expose usable local bounds.");
        }

        var widthAxis = 3 - lengthAxis - thicknessAxis;
        var right = new Vec3(1f, 0f, 0f);
        var up = Vec3.Up;
        var axes = new Vec3[3];
        axes[lengthAxis] = horizontal ? right : up;
        axes[widthAxis] = horizontal ? up : right;
        axes[thicknessAxis] = thicknessAxis switch
        {
            0 => Cross(axes[1], axes[2]),
            1 => Cross(axes[2], axes[0]),
            _ => Cross(axes[0], axes[1])
        };

        var rotation = default(Mat3);
        rotation.s = axes[0];
        rotation.f = axes[1];
        rotation.u = axes[2];
        rotation.ApplyScaleLocal(scale);
        var desiredCenter = horizontal
            ? desiredAnchor
            : desiredAnchor + up * (sizes[lengthAxis] * scale * 0.5f);
        var localCenter = (bounds.min + bounds.max) * 0.5f;
        var rotatedCenter = rotation.TransformToParent(in localCenter);
        return new MatrixFrame(rotation, desiredCenter - rotatedCenter);
    }

    private static Vec3 Cross(Vec3 left, Vec3 right) =>
        new(
            left.y * right.z - left.z * right.y,
            left.z * right.x - left.x * right.z,
            left.x * right.y - left.y * right.x);
}
