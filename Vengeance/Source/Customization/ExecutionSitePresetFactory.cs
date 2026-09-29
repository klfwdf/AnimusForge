using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RichExecutions.Core;
using RichExecutions.Scene;

namespace RichExecutions.Customization;

public static class ExecutionSitePresetFactory
{
    private const string LegacyCrucifixStakeDecorationRole = "decor_crucifix_stake";
    private const string LegacyCrucifixCrossbeamDecorationRole = "decor_crucifix_crossbeam";

    private static readonly IReadOnlyDictionary<string, string[]> RequiredMarkerRoles =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [ExecutionMethodRules.Beheading] = new[]
            {
                "victim", "executioner", "player_interaction", "beheading_block"
            },
            [ExecutionMethodRules.Hanging] = new[]
            {
                "victim", "executioner", "player_interaction", "hanging_anchor", "hanging_suspension", "hanging_release"
            },
            [ExecutionMethodRules.Burning] = new[]
            {
                "victim", "executioner", "player_interaction", "burning_cross_center", "burning_fuel_center", "burning_ignition"
            },
            [ExecutionMethodRules.BreakingWheel] = new[]
            {
                "victim", "executioner", "player_interaction", "wheel_center", "wheel_strike_axis", "wheel_raise_control"
            },
            [ExecutionMethodRules.Impalement] = new[]
            {
                "victim", "executioner", "player_interaction", "impalement_base", "impalement_axis"
            },
            [ExecutionMethodRules.Stoning] = new[]
            {
                "victim", "executioner", "player_interaction", "stoning_first_throw"
            },
            [ExecutionMethodRules.CrossbowExecution] = new[]
            {
                "victim", "executioner", "player_interaction", "crossbow_cross_center", "crossbow_crossbeam", "crossbow_shooter_center", "crossbow_ranged_line"
            }
        };

    private const string CeremonialBuiltInVariant = "ceremonial";

    public static IReadOnlyList<ExecutionSitePreset> CreateBuiltInPresets(
        string methodId,
        ExecutionSceneVisualProfile profile)
    {
        if (!string.Equals(methodId, profile.MethodId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The visual profile does not match the requested method.", nameof(profile));
        }

        var ceremonial = CreateBuiltInPreset(
            methodId,
            profile,
            CeremonialBuiltInVariant,
            T("{=REX_Builder_Builtin_Ceremonial}Built-in ceremonial layout"));
        AddCeremonialMarkers(ceremonial);
        return new[] { ceremonial };
    }

    public static int GetBuiltInPresetRank(string methodId, Guid presetId)
    {
        if (presetId == GetBuiltInPresetId(methodId, CeremonialBuiltInVariant)) return 0;
        return 10;
    }

    public static Guid GetBuiltInPresetId(string methodId, string variantId)
    {
        if (string.IsNullOrWhiteSpace(methodId)) throw new ArgumentException("A method id is required.", nameof(methodId));
        if (string.IsNullOrWhiteSpace(variantId)) throw new ArgumentException("A variant id is required.", nameof(variantId));
        using var hash = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(
            $"RichExecutions|execution-site-preset|schema-1|{methodId.ToLowerInvariant()}|{variantId.ToLowerInvariant()}");
        return new Guid(hash.ComputeHash(bytes));
    }

    public static bool IsDecorationPreset(ExecutionSitePreset? preset) =>
        // Legacy decoration-only presets belong in Add decoration. A complete
        // execution layout remains selectable when users add those same pieces.
        preset?.Pieces is { Count: > 0 } pieces &&
        (preset.Markers is null || preset.Markers.Count == 0) &&
        pieces.All(piece => piece is not null && (
            string.Equals(piece.RoleId, ExecutionSiteCrucifixDecoration.RoleId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(piece.RoleId, LegacyCrucifixStakeDecorationRole, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(piece.RoleId, LegacyCrucifixCrossbeamDecorationRole, StringComparison.OrdinalIgnoreCase) ||
            ImpaledCorpseDisplayStore.TryParseRoleId(piece.RoleId, out _)));

    private static ExecutionSitePreset CreateBuiltInPreset(
        string methodId,
        ExecutionSceneVisualProfile profile,
        string variantId,
        string name)
    {
        var preset = CloneAutomaticLayout(methodId, profile, name);
        preset.Id = GetBuiltInPresetId(methodId, variantId);
        return preset;
    }
    public static IReadOnlyList<string> GetRequiredMarkerRoles(string methodId) =>
        RequiredMarkerRoles.TryGetValue(methodId, out var roles)
            ? roles
            : new[] { "victim", "executioner", "player_interaction" };

    public static IReadOnlyList<string> GetMissingRequiredPieceRoles(ExecutionSitePreset preset)
    {
        var present = new HashSet<string>(
            preset.Pieces.Where(piece => !string.IsNullOrWhiteSpace(piece.RoleId)).Select(piece => piece.RoleId!),
            StringComparer.OrdinalIgnoreCase);
        return ExecutionSiteFunctionalCatalog.Get(preset.MethodId)
            .Select(definition => definition.RoleId)
            .Where(role => !present.Contains(role))
            .ToList();
    }

    public static IReadOnlyList<string> GetMissingRequiredMarkerRoles(ExecutionSitePreset preset)
    {
        var present = new HashSet<string>(
            preset.Markers.Select(marker => marker.RoleId),
            StringComparer.OrdinalIgnoreCase);
        return GetRequiredMarkerRoles(preset.MethodId)
            .Where(role => !present.Contains(role))
            .ToList();
    }

    public static ExecutionSitePreset CreateEmpty(string methodId, string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        MethodId = methodId,
        RootAnchor = new ExecutionPresetTransform(),
        Pieces = new List<ExecutionSitePiece>(),
        Markers = new List<ExecutionSiteMarker>(),
        Troops = new ExecutionSiteTroopSelection()
    };

    public static ExecutionSitePreset CloneAutomaticLayout(
        string methodId,
        ExecutionSceneVisualProfile profile,
        string name)
    {
        if (!string.Equals(methodId, profile.MethodId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The visual profile does not match the requested method.", nameof(profile));
        }

        var preset = CreateEmpty(methodId, name);
        if (UsesScaffold(methodId))
        {
            preset.Pieces.Add(new ExecutionSitePiece
            {
                ResourceKey = "gallows",
                ResourceType = ExecutionSiteResourceType.Prefab,
                RoleId = "site_stage_gallows",
                Collision = ExecutionSiteCollisionPolicy.Authored,
                Transform = new ExecutionPresetTransform()
            });
        }

        foreach (var prop in profile.Props)
        {
            preset.Pieces.Add(new ExecutionSitePiece
            {
                ResourceKey = prop.PrefabCandidates[0],
                ResourceType = ExecutionSiteResourceType.Prefab,
                RoleId = prop.RoleId,
                Collision = prop.CreatePhysics
                    ? ExecutionSiteCollisionPolicy.Enabled
                    : ExecutionSiteCollisionPolicy.Disabled,
                Transform = new ExecutionPresetTransform
                {
                    X = prop.Offset.Right,
                    Y = prop.Offset.Forward,
                    Z = prop.Offset.Up,
                    Pitch = prop.PitchDegrees,
                    Yaw = prop.YawDegrees,
                    Roll = prop.RollDegrees,
                    Scale = prop.UniformScale
                }
            });
        }

        foreach (var definition in ExecutionSiteFunctionalCatalog.Get(methodId))
        {
            if (preset.Pieces.Any(piece => string.Equals(piece.RoleId, definition.RoleId, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            preset.Pieces.Add(new ExecutionSitePiece
            {
                ResourceKey = definition.ResourceKey,
                ResourceType = definition.ResourceType,
                RoleId = definition.RoleId,
                Collision = definition.Collision,
                Transform = definition.Transform.Clone()
            });
        }

        foreach (var marker in CreateDefaultMarkers(methodId, profile))
        {
            preset.Markers.Add(marker);
        }

        return preset;
    }

    private static IEnumerable<ExecutionSiteMarker> CreateDefaultMarkers(
        string methodId,
        ExecutionSceneVisualProfile profile)
    {
        var victimForward = string.Equals(methodId, ExecutionMethodRules.CrossbowExecution, StringComparison.OrdinalIgnoreCase)
            ? -10f
            : 0f;
        yield return Marker("victim", ExecutionSiteMarkerKind.Actor, 0f, victimForward, 0f);
        yield return Marker("executioner", ExecutionSiteMarkerKind.Actor, -1.35f, 0f, 0f);
        yield return Marker(
            "player_interaction",
            ExecutionSiteMarkerKind.Interaction,
            profile.InteractionPoint.Right,
            profile.InteractionPoint.Forward,
            profile.InteractionPoint.Up);

        foreach (var role in GetRequiredMarkerRoles(methodId).Skip(3))
        {
            var marker = role switch
            {
                "beheading_block" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0.20f, 0f),
                "hanging_anchor" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0.12f, 3.2f),
                "hanging_suspension" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0.12f, 1.45f),
                "hanging_release" => Marker(role, ExecutionSiteMarkerKind.Interaction, 0.85f, -0.35f, 0f),
                "burning_cross_center" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, -0.24f, 1.32f),
                "burning_fuel_center" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0.12f, 0f),
                "burning_ignition" => Marker(role, ExecutionSiteMarkerKind.Interaction, 0.8f, -0.45f, 0f),
                "wheel_center" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0.10f, 0.32f),
                "wheel_strike_axis" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0.10f, 0.32f),
                "wheel_raise_control" => Marker(role, ExecutionSiteMarkerKind.Interaction, 1.20f, -0.25f, 0f),
                "impalement_base" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0f, 0f),
                "impalement_axis" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0f, 1f),
                "stoning_first_throw" => Marker(role, ExecutionSiteMarkerKind.Interaction, 0.85f, -0.35f, 0f),
                "crossbow_cross_center" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, -10.24f, 0.12f),
                "crossbow_crossbeam" => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, -10.24f, 1.48f),
                "crossbow_shooter_center" => Marker(role, ExecutionSiteMarkerKind.Actor, 0f, 0f, 0f),
                "crossbow_ranged_line" => Marker(role, ExecutionSiteMarkerKind.Actor, 0f, 0f, 0f),
                _ => Marker(role, ExecutionSiteMarkerKind.Functional, 0f, 0f, 0f)
            };
            yield return marker;
        }
    }

    private static void AddCeremonialMarkers(ExecutionSitePreset preset)
    {
        var victim = preset.Markers.FirstOrDefault(marker =>
            string.Equals(marker.RoleId, "victim", StringComparison.OrdinalIgnoreCase));
        var victimForward = victim?.Transform.Y ?? 0f;
        var midpointForward = victimForward * 0.5f;

        AddMarkerIfMissing(preset, Marker("guard_melee_1", ExecutionSiteMarkerKind.Actor, -2.2f, victimForward + 0.45f, 0f));
        AddMarkerIfMissing(preset, Marker("guard_melee_2", ExecutionSiteMarkerKind.Actor, 2.2f, victimForward + 0.45f, 0f));
        AddMarkerIfMissing(preset, Marker("guard_ranged_1", ExecutionSiteMarkerKind.Actor, -3.1f, midpointForward - 1.3f, 0f));
        AddMarkerIfMissing(preset, Marker("guard_ranged_2", ExecutionSiteMarkerKind.Actor, 3.1f, midpointForward - 1.3f, 0f));
        AddMarkerIfMissing(preset, Marker("crowd_1", ExecutionSiteMarkerKind.Crowd, -4.0f, midpointForward + 2.0f, 0f));
        AddMarkerIfMissing(preset, Marker("crowd_2", ExecutionSiteMarkerKind.Crowd, 4.2f, midpointForward + 1.6f, 0f));
        AddMarkerIfMissing(preset, Marker("crowd_3", ExecutionSiteMarkerKind.Crowd, -4.4f, midpointForward - 2.0f, 0f));
        AddMarkerIfMissing(preset, Marker("crowd_4", ExecutionSiteMarkerKind.Crowd, 4.0f, midpointForward - 2.3f, 0f));
    }

    private static void AddMarkerIfMissing(ExecutionSitePreset preset, ExecutionSiteMarker marker)
    {
        if (preset.Markers.Any(existing =>
                string.Equals(existing.RoleId, marker.RoleId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        preset.Markers.Add(marker);
    }

    private static string T(string text) => new TaleWorlds.Localization.TextObject(text).ToString();
    private static ExecutionSiteMarker Marker(
        string roleId,
        ExecutionSiteMarkerKind kind,
        float right,
        float forward,
        float up) => new()
    {
        RoleId = roleId,
        Kind = kind,
        Transform = new ExecutionPresetTransform
        {
            X = right,
            Y = forward,
            Z = up,
            Scale = 1f
        }
    };

    private static bool UsesScaffold(string methodId) =>
        string.Equals(methodId, ExecutionMethodRules.Beheading, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(methodId, ExecutionMethodRules.Hanging, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(methodId, ExecutionMethodRules.BreakingWheel, StringComparison.OrdinalIgnoreCase);
}
