using System;
using System.Collections.Generic;
using RichExecutions.Core;

namespace RichExecutions.Customization;

internal sealed class ExecutionSiteFunctionalPieceDefinition
{
    internal ExecutionSiteFunctionalPieceDefinition(
        string roleId,
        string resourceKey,
        ExecutionSiteResourceType resourceType,
        string name,
        ExecutionPresetTransform transform,
        ExecutionSiteCollisionPolicy collision)
    {
        RoleId = roleId;
        ResourceKey = resourceKey;
        ResourceType = resourceType;
        Name = name;
        Transform = transform;
        Collision = collision;
    }

    internal string RoleId { get; }
    internal string ResourceKey { get; }
    internal ExecutionSiteResourceType ResourceType { get; }
    internal string Name { get; }
    internal ExecutionPresetTransform Transform { get; }
    internal ExecutionSiteCollisionPolicy Collision { get; }
}

internal static class ExecutionSiteFunctionalCatalog
{
    internal static IReadOnlyList<ExecutionSiteFunctionalPieceDefinition> Get(string methodId)
    {
        var list = new List<ExecutionSiteFunctionalPieceDefinition>();
        if (methodId is ExecutionMethodRules.Beheading or ExecutionMethodRules.Hanging or ExecutionMethodRules.BreakingWheel)
            list.Add(Prefab("site_stage_gallows", "gallows", "{=REX_Builder_Apparatus_Scaffold}Execution scaffold", 0f, 0f, 0f));
        switch (methodId)
        {
            case ExecutionMethodRules.Hanging:
                list.Add(Prefab("hanging_noose", "bd_rope_a", "{=REX_Builder_Apparatus_Neck_Rope}Hanging noose", 0f, 0.12f, 1.45f, 0.25f, ExecutionSiteCollisionPolicy.Disabled));
                break;
            case ExecutionMethodRules.Burning:
                list.Add(Prefab("burning_stake", "wooden_platform_2_plank_a", "{=REX_Builder_Apparatus_Cross_Upright}Burning cross upright", 0f, -0.24f, 1.32f, 0.90f));
                list.Add(Prefab("burning_crossbeam", "wooden_platform_2_plank_a", "{=REX_Builder_Apparatus_Cross_Beam}Burning cross beam", 0f, -0.24f, 2.80f, 0.78f));
                break;
            case ExecutionMethodRules.BreakingWheel:
                list.Add(Prefab("breaking_wheel", "bd_cart_wheel_a", "{=REX_Builder_Apparatus_Wheel}Breaking wheel", 0f, 0.10f, 0.32f, 1.18f, ExecutionSiteCollisionPolicy.Disabled, pitch: 90f));
                list.Add(Prefab("breaking_wheel_axle", "bd_pole_3m", "{=REX_Builder_Apparatus_Wheel_Axle}Wheel axle", 0f, 0.10f, 0.22f, 0.42f, ExecutionSiteCollisionPolicy.Enabled, roll: 90f));
                break;
            case ExecutionMethodRules.Impalement:
                // v0.5.5: the TPAC asset pack now ships one merged impalement
                // apparatus Mesh. The former separate bottom Mesh
                // (verjianchi01) is gone, so only the merged spike piece is
                // placed and it sits directly on the ceremony ground.
                list.Add(Mesh("impalement_spike", "verjianchi02", "{=REX_Builder_Apparatus_Impalement_Spike}Impalement spike", 0f, 0f, 0f));
                break;
            case ExecutionMethodRules.Stoning:
                list.Add(Prefab("stoning_stones", "merchandise_stones", "{=REX_Builder_Apparatus_Stones}Stoning ammunition pile", 0f, 0.2f, 0.05f, 0.48f));
                break;
            case ExecutionMethodRules.CrossbowExecution:
                list.Add(Prefab("crossbow_stake", "wooden_platform_2_plank_a", "{=REX_Builder_Apparatus_Crossbow_Upright}Crossbow execution upright", 0f, -10.24f, 0.12f, 0.52f));
                list.Add(Prefab("crossbow_crossbeam", "wooden_platform_2_plank_a", "{=REX_Builder_Apparatus_Crossbow_Beam}Crossbow execution beam", 0f, -10.24f, 1.48f, 0.42f));
                break;
        }
        return list;
    }

    private static ExecutionSiteFunctionalPieceDefinition Prefab(
        string role, string key, string name, float x, float y, float z,
        float scale = 1f,
        ExecutionSiteCollisionPolicy collision = ExecutionSiteCollisionPolicy.Authored,
        float pitch = 0f,
        float roll = 0f) => new(role, key, ExecutionSiteResourceType.Prefab, name,
        new ExecutionPresetTransform { X = x, Y = y, Z = z, Scale = scale, Pitch = pitch, Roll = roll }, collision);

    private static ExecutionSiteFunctionalPieceDefinition Mesh(
        string role, string key, string name, float x, float y, float z) => new(role, key,
        ExecutionSiteResourceType.Mesh, name,
        new ExecutionPresetTransform { X = x, Y = y, Z = z, Scale = 1f },
        ExecutionSiteCollisionPolicy.Disabled);
}