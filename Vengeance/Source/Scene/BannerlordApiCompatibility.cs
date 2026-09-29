using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

internal static class BannerlordApiCompatibility
{
    private static readonly MethodInfo? PrefabWithRestOffset = FindStatic(
        "InstantiateWithRestOffset",
        typeof(TaleWorlds.Engine.Scene), typeof(string), typeof(bool), typeof(MatrixFrame),
        typeof(float), typeof(bool));

    // Both supported engine lines expose this five-argument form. It carries
    // the initial frame, so a prefab does not appear at the scene origin.
    private static readonly MethodInfo? PrefabWithFrame = FindStatic(
        "Instantiate",
        typeof(TaleWorlds.Engine.Scene), typeof(string), typeof(MatrixFrame), typeof(bool), typeof(string));

    private static readonly MethodInfo? AgentBoneFrame = typeof(Agent).GetMethod(
        "GetBoneEntitialFrame",
        BindingFlags.Instance | BindingFlags.Public,
        null,
        new[] { typeof(sbyte), typeof(bool) },
        null);

    private static readonly MethodInfo? BodyRestOffset = typeof(WeakGameEntity).GetMethod(
        "UpdateBodyRestOffset",
        BindingFlags.Instance | BindingFlags.Public,
        null,
        new[] { typeof(float) },
        null);

    public static GameEntity? InstantiatePrefab(
        TaleWorlds.Engine.Scene scene,
        string prefabName,
        bool createPhysics,
        MatrixFrame frame,
        bool callScriptCallbacks)
    {
        if (PrefabWithRestOffset is not null)
        {
            var arguments = PrefabWithRestOffset.GetParameters().Length == 7
                ? new object[] { scene, prefabName, createPhysics, frame, 0f, callScriptCallbacks, string.Empty }
                : new object[] { scene, prefabName, createPhysics, frame, 0f, callScriptCallbacks };
            return PrefabWithRestOffset.Invoke(null, arguments) as GameEntity;
        }

        if (PrefabWithFrame is not null)
        {
            return PrefabWithFrame.Invoke(
                null,
                new object[] { scene, prefabName, frame, callScriptCallbacks, string.Empty }) as GameEntity;
        }

        throw new MissingMethodException(typeof(GameEntity).FullName, "Instantiate");
    }

    private static MethodInfo? FindStatic(string name, params Type[] requiredPrefix)
    {
        foreach (var method in typeof(GameEntity).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (!string.Equals(method.Name, name, StringComparison.Ordinal)) continue;
            var parameters = method.GetParameters();
            if (parameters.Length < requiredPrefix.Length) continue;
            var matches = true;
            for (var index = 0; index < requiredPrefix.Length; index++)
            {
                if (parameters[index].ParameterType != requiredPrefix[index])
                {
                    matches = false;
                    break;
                }
            }

            if (matches) return method;
        }

        return null;
    }

    public static MatrixFrame GetVictimBoneFrame(Agent victim, sbyte boneIndex)
    {
        if (AgentBoneFrame is not null)
        {
            return (MatrixFrame)AgentBoneFrame.Invoke(victim, new object[] { boneIndex, true })!;
        }

        var method = typeof(Agent).GetMethod(
            "GetBoneEntitialFrame",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(sbyte) },
            null);
        if (method is null)
        {
            throw new MissingMethodException(typeof(Agent).FullName, "GetBoneEntitialFrame");
        }

        return (MatrixFrame)method.Invoke(victim, new object[] { boneIndex })!;
    }

    public static void TryApplyBodyRestOffset(GameEntity entity, float offset)
    {
        if (BodyRestOffset is null)
        {
            return;
        }

        object weakEntity = entity.WeakEntity;
        BodyRestOffset.Invoke(weakEntity, new object[] { offset });
    }
}
