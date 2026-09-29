using System;
using System.Collections.Generic;
using RichExecutions.Core;

namespace RichExecutions.Scene;

/// <summary>
/// Describes whether an execution scene is assembled from directly relevant
/// native assets or uses an explicit, safe symbolic stand-in.
/// </summary>
public enum ExecutionSceneVisualFidelity
{
    NativeComposition = 0,
    GenericPlaceholder = 1
}

/// <summary>
/// A relative point in the execution site's right/forward/up basis.
/// </summary>
public readonly struct ExecutionSceneRelativePoint
{
    public ExecutionSceneRelativePoint(float right, float forward, float up)
    {
        Right = right;
        Forward = forward;
        Up = up;
    }

    public float Right { get; }
    public float Forward { get; }
    public float Up { get; }
}

/// <summary>
/// One native-prop slot in an execution scene. Prefab candidates are ordered
/// fallbacks: the mission behavior should instantiate the first available one.
/// Collision is enabled only for explicitly approved solid decorations; ropes,
/// fire effects, interaction hosts, and generic placeholders remain visual-only.
/// </summary>
public sealed class ExecutionScenePropPlacement
{
    private readonly string[] _prefabCandidates;
    private readonly bool _createPhysics;

    public ExecutionScenePropPlacement(
        string roleId,
        string[] prefabCandidates,
        float right,
        float forward,
        float up,
        float yawDegrees = 0f,
        float pitchDegrees = 0f,
        float rollDegrees = 0f,
        float uniformScale = 1f,
        bool isIgnitionEffect = false,
        bool callScriptCallbacks = false,
        bool? createPhysicsOverride = null)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("A visual prop role ID is required.", nameof(roleId));
        }

        if (prefabCandidates is null || prefabCandidates.Length == 0)
        {
            throw new ArgumentException("At least one prefab candidate is required.", nameof(prefabCandidates));
        }

        _prefabCandidates = (string[])prefabCandidates.Clone();
        for (var index = 0; index < _prefabCandidates.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(_prefabCandidates[index]))
            {
                throw new ArgumentException("Prefab candidates cannot be empty.", nameof(prefabCandidates));
            }
        }

        RoleId = roleId;
        Offset = new ExecutionSceneRelativePoint(right, forward, up);
        YawDegrees = yawDegrees;
        PitchDegrees = pitchDegrees;
        RollDegrees = rollDegrees;
        UniformScale = uniformScale;
        IsIgnitionEffect = isIgnitionEffect;
        CallScriptCallbacks = callScriptCallbacks;
        _createPhysics = createPhysicsOverride ??
                         (roleId is "burning_stake" or "crossbow_stake" ||
                          roleId.StartsWith("burning_support_", StringComparison.OrdinalIgnoreCase) ||
                          roleId.StartsWith("breaking_wheel_cradle_", StringComparison.OrdinalIgnoreCase) ||
                          roleId == "breaking_wheel_axle");
    }

    public string RoleId { get; }
    public IReadOnlyList<string> PrefabCandidates => _prefabCandidates;
    public ExecutionSceneRelativePoint Offset { get; }
    public float YawDegrees { get; }
    public float PitchDegrees { get; }
    public float RollDegrees { get; }
    public float UniformScale { get; }
    public bool IsIgnitionEffect { get; }
    public bool CallScriptCallbacks { get; }

    public bool CreatePhysics => _createPhysics;
}

/// <summary>
/// Data-only visual configuration for one execution method. The mission
/// behavior can traverse <see cref="Props"/> once during setup, then traverse
/// only entries with <see cref="ExecutionScenePropPlacement.IsIgnitionEffect"/>
/// at the lethal frame when <see cref="IgniteAtLethalFrame"/> is true.
/// </summary>
public sealed class ExecutionSceneVisualProfile
{
    private readonly ExecutionScenePropPlacement[] _props;
    private readonly ExecutionScenePropPlacement[] _decorations;
    private readonly ExecutionScenePropPlacement[] _ignitionEffects;
    private readonly ExecutionSceneRelativePoint[] _fireOffsets;

    public ExecutionSceneVisualProfile(
        string methodId,
        ExecutionSceneVisualFidelity fidelity,
        string interactionNameText,
        string interactionActionText,
        ExecutionSceneRelativePoint interactionPoint,
        bool igniteAtLethalFrame,
        ExecutionScenePropPlacement[] props)
    {
        if (string.IsNullOrWhiteSpace(methodId))
        {
            throw new ArgumentException("An execution method ID is required.", nameof(methodId));
        }

        if (string.IsNullOrWhiteSpace(interactionNameText))
        {
            throw new ArgumentException("Localized interaction name text is required.", nameof(interactionNameText));
        }

        if (string.IsNullOrWhiteSpace(interactionActionText))
        {
            throw new ArgumentException("Localized interaction action text is required.", nameof(interactionActionText));
        }

        if (props is null)
        {
            throw new ArgumentNullException(nameof(props));
        }

        _props = (ExecutionScenePropPlacement[])props.Clone();
        var decorations = new List<ExecutionScenePropPlacement>(_props.Length);
        var ignitionEffects = new List<ExecutionScenePropPlacement>(_props.Length);
        var fireOffsets = new List<ExecutionSceneRelativePoint>(_props.Length);
        var roleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < _props.Length; index++)
        {
            if (_props[index] is null)
            {
                throw new ArgumentException("Visual prop entries cannot be null.", nameof(props));
            }

            if (!roleIds.Add(_props[index].RoleId))
            {
                throw new ArgumentException(
                    $"Visual prop role '{_props[index].RoleId}' is duplicated in method '{methodId}'.",
                    nameof(props));
            }

            if (_props[index].IsIgnitionEffect)
            {
                ignitionEffects.Add(_props[index]);
                fireOffsets.Add(_props[index].Offset);
            }
            else
            {
                decorations.Add(_props[index]);
            }
        }

        _decorations = decorations.ToArray();
        _ignitionEffects = ignitionEffects.ToArray();
        _fireOffsets = fireOffsets.ToArray();
        if (igniteAtLethalFrame != (_ignitionEffects.Length > 0))
        {
            throw new ArgumentException(
                $"Method '{methodId}' must have ignition effects if and only if lethal-frame ignition is enabled.",
                nameof(props));
        }

        MethodId = methodId;
        Fidelity = fidelity;
        InteractionNameText = interactionNameText;
        InteractionActionText = interactionActionText;
        InteractionPoint = interactionPoint;
        IgniteAtLethalFrame = igniteAtLethalFrame;
    }

    public string MethodId { get; }
    public ExecutionSceneVisualFidelity Fidelity { get; }
    public bool UsesGenericPlaceholder => Fidelity == ExecutionSceneVisualFidelity.GenericPlaceholder;
    public string InteractionNameText { get; }
    public string InteractionActionText { get; }
    public string InteractionDescription => InteractionNameText;
    public string InteractionAction => InteractionActionText;
    public ExecutionSceneRelativePoint InteractionPoint { get; }
    public bool IgniteAtLethalFrame { get; }
    public IReadOnlyList<ExecutionScenePropPlacement> Props => _props;
    public IReadOnlyList<ExecutionScenePropPlacement> Decorations => _decorations;
    public IReadOnlyList<ExecutionScenePropPlacement> IgnitionEffects => _ignitionEffects;
    public IReadOnlyList<ExecutionSceneRelativePoint> FireOffsets => _fireOffsets;
}

/// <summary>
/// Built-in v1.4.8 visual profiles. None of these entries uses the native
/// monolithic "gallows" prefab, a water plane, or siege soil. Large physical
/// shells from those assets are unsuitable for runtime placement in arbitrary
/// and third-party town-center scenes.
/// </summary>
public static class ExecutionSceneVisualProfiles
{
    private const string GenericActionText =
        "{=REX_Action_Carry_Out}carry out the sentence";
    private const float BurningCrucifixVisualLift = 1.32f;
    private const float CrossbowCrucifixVisualLift = 0.12f;

    private static readonly ExecutionSceneVisualProfile[] RegisteredProfiles = CreateProfiles();

    private static readonly Dictionary<string, ExecutionSceneVisualProfile> ProfilesByMethod =
        BuildLookup(RegisteredProfiles);

    public static IReadOnlyList<ExecutionSceneVisualProfile> All => RegisteredProfiles;

    public static bool TryGet(string methodId, out ExecutionSceneVisualProfile profile)
    {
        if (string.IsNullOrWhiteSpace(methodId))
        {
            profile = null!;
            return false;
        }

        return ProfilesByMethod.TryGetValue(methodId, out profile!);
    }

    /// <summary>
    /// Gives third-party method registrations a collision-free original-prop
    /// placeholder without silently pretending that they have dedicated art.
    /// </summary>
    public static ExecutionSceneVisualProfile CreateGenericPlaceholder(string methodId)
    {
        return new ExecutionSceneVisualProfile(
            methodId,
            ExecutionSceneVisualFidelity.GenericPlaceholder,
            "{=REX_Use_Execution_Point}Execution point",
            GenericActionText,
            Point(0f, -0.45f, 0f),
            igniteAtLethalFrame: false,
            new[]
            {
                Prop(
                    "generic_marker",
                    Candidates("wooden_platform_plank_f_column", "bd_pole_3m"),
                    0f,
                    0.45f,
                    0f)
            });
    }

    private static ExecutionSceneVisualProfile[] CreateProfiles()
    {
        return new[]
        {
            Profile(
                ExecutionMethodRules.Beheading,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Use_Block}Execution block",
                "{=REX_Action_Behead}carry out the beheading",
                Point(0f, -0.55f, 0f),
                ignite: false),

            Profile(
                ExecutionMethodRules.Hanging,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Use_Gallows}Gallows release",
                "{=REX_Action_Use_Gallows}release the gallows",
                Point(0.85f, -0.35f, 0f),
                ignite: false,
                Prop(
                    "hanging_noose",
                    Candidates("bd_rope_a", "bd_rope_b"),
                    0f,
                    0.12f,
                    1.45f,
                    uniformScale: 0.25f)),

            Profile(
                ExecutionMethodRules.Burning,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Use_Pyre}Prepared pyre",
                "{=REX_Action_Light_Pyre}light the pyre",
                Point(0.8f, -0.45f, 0f),
                ignite: true,
                Prop(
                    "burning_stake",
                    Candidates(
                        "wooden_platform_2_plank_a",
                        "wooden_platform_plank_f_column",
                        "battania_castle_keep_wood_stake_a"),
                    0f,
                    -0.24f,
                    BurningCrucifixVisualLift,
                    uniformScale: 0.90f),
                Prop(
                    "burning_crossbeam",
                    Candidates(
                        "wooden_platform_2_plank_a",
                        "wooden_platform_plank_f_column"),
                    0f,
                    -0.24f,
                    2.38f + BurningCrucifixVisualLift,
                    uniformScale: 0.62f),
                Prop("burning_support_front_left", Candidates("wooden_platform_plank_f_column", "bd_pole_3m"), -1.78f, 1.50f, 0.06f),
                Prop("burning_support_front_right", Candidates("wooden_platform_plank_f_column", "bd_pole_3m"), 1.78f, 1.50f, 0.06f),
                Prop("burning_support_rear_left", Candidates("wooden_platform_plank_f_column", "bd_pole_3m"), -1.78f, -1.95f, 0.06f),
                Prop("burning_support_rear_right", Candidates("wooden_platform_plank_f_column", "bd_pole_3m"), 1.78f, -1.95f, 0.06f),
                Fire("pyre_fire_left_leg", -0.28f, 0.18f, 1.18f),
                Fire("pyre_fire_right_leg", 0.28f, 0.22f, 1.24f),
                Fire("pyre_fire_left_thigh", -0.20f, 0.15f, 1.50f),
                Fire("pyre_fire_right_thigh", 0.20f, 0.17f, 1.54f),
                Fire("pyre_fire_waist", -0.10f, 0.14f, 1.72f),
                Fire("pyre_fire_torso", 0.10f, 0.16f, 2.16f)),

            Profile(
                ExecutionMethodRules.BreakingWheel,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Use_Breaking_Wheel}Execution wheel",
                "{=REX_Action_Break_On_Wheel}break the condemned on the wheel",
                Point(0.85f, -0.35f, 0f),
                ignite: false,
                Prop(
                    "breaking_wheel",
                    Candidates("bd_cart_wheel_a", "bd_cart_wheel_b"),
                    0f,
                    0.10f,
                    0.32f,
                    pitch: 90f,
                    uniformScale: 1.18f),
                Prop("breaking_wheel_cradle_left", Candidates("wooden_platform_plank_f_column", "bd_pole_3m"), -0.82f, 0.10f, 0.12f, pitch: 90f, uniformScale: 0.52f),
                Prop("breaking_wheel_cradle_right", Candidates("wooden_platform_plank_f_column", "bd_pole_3m"), 0.82f, 0.10f, 0.12f, pitch: 90f, uniformScale: 0.52f),
                Prop("breaking_wheel_axle", Candidates("bd_pole_3m", "wooden_platform_plank_f_column"), 0f, 0.10f, 0.22f, roll: 90f, uniformScale: 0.42f)),

            Profile(
                ExecutionMethodRules.Impalement,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Impalement_Use_Pole}Impalement pole",
                "{=REX_Impalement_Action_Pelvis}drive the pole through the pelvis",
                Point(1.30f, -0.30f, 0f),
                ignite: false),
            Profile(
                ExecutionMethodRules.Stoning,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Use_Stoning_Ground}Stoning ground",
                "{=REX_Action_Stone}cast the first stone",
                Point(0.85f, -0.35f, 0f),
                ignite: false,
                Prop("stoning_pile_front_left", Candidates("merchandise_stones"), -0.72f, -0.34f, 0.05f, yaw: 17f, uniformScale: 0.46f),
                Prop("stoning_pile_front_right", Candidates("merchandise_stones"), 0.70f, -0.28f, 0.05f, yaw: 93f, uniformScale: 0.42f),
                Prop("stoning_pile_left", Candidates("merchandise_stones"), -0.88f, 0.25f, 0.05f, yaw: 148f, uniformScale: 0.48f),
                Prop("stoning_pile_right", Candidates("merchandise_stones"), 0.90f, 0.30f, 0.05f, yaw: 231f, uniformScale: 0.44f),
                Prop("stoning_pile_rear_left", Candidates("merchandise_stones"), -0.58f, 0.72f, 0.05f, yaw: 304f, uniformScale: 0.40f),
                Prop("stoning_pile_rear_right", Candidates("merchandise_stones"), 0.62f, 0.76f, 0.05f, yaw: 347f, uniformScale: 0.45f)),

            Profile(
                ExecutionMethodRules.CrossbowExecution,
                ExecutionSceneVisualFidelity.NativeComposition,
                "{=REX_Use_Crossbow_Execution}Execution cross",
                "{=REX_Action_Crossbow_Execution}shoot the condemned",
                Point(0f, 0f, 0f),
                ignite: false,
                Prop(
                    "crossbow_stake",
                    Candidates(
                        "wooden_platform_2_plank_a",
                        "wooden_platform_plank_f_column",
                        "bd_pole_3m"),
                    0f,
                    -10.24f,
                    CrossbowCrucifixVisualLift,
                    uniformScale: 0.52f),
                Prop(
                    "crossbow_crossbeam",
                    Candidates(
                        "wooden_platform_2_plank_a",
                        "wooden_platform_plank_f_column"),
                    0f,
                    -10.24f,
                    1.48f,
                    uniformScale: 0.42f)),

        };
    }

    private static ExecutionSceneVisualProfile Profile(
        string methodId,
        ExecutionSceneVisualFidelity fidelity,
        string interactionNameText,
        string interactionActionText,
        ExecutionSceneRelativePoint interactionPoint,
        bool ignite,
        params ExecutionScenePropPlacement[] props)
    {
        return new ExecutionSceneVisualProfile(
            methodId,
            fidelity,
            interactionNameText,
            interactionActionText,
            interactionPoint,
            ignite,
            props);
    }

    private static ExecutionScenePropPlacement Prop(
        string roleId,
        string[] candidates,
        float right,
        float forward,
        float up,
        float yaw = 0f,
        float pitch = 0f,
        float roll = 0f,
        float uniformScale = 1f)
    {
        return new ExecutionScenePropPlacement(
            roleId,
            candidates,
            right,
            forward,
            up,
            yaw,
            pitch,
            roll,
            uniformScale);
    }

    private static ExecutionScenePropPlacement Fire(
        string roleId,
        float right,
        float forward,
        float up,
        float uniformScale = 1.20f)
    {
        return new ExecutionScenePropPlacement(
            roleId,
            Candidates(
                "prt_fire",
                "fire_only",
                "prt_torch_fire",
                "siege_fire_particle_b",
                "fire_stones_bonfire"),
            right,
            forward,
            up,
            uniformScale: uniformScale,
            isIgnitionEffect: true,
            callScriptCallbacks: true);
    }

    private static ExecutionSceneRelativePoint Point(float right, float forward, float up) =>
        new(right, forward, up);

    private static string[] Candidates(params string[] prefabIds) => prefabIds;

    private static Dictionary<string, ExecutionSceneVisualProfile> BuildLookup(
        IEnumerable<ExecutionSceneVisualProfile> profiles)
    {
        var result = new Dictionary<string, ExecutionSceneVisualProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            if (result.ContainsKey(profile.MethodId))
            {
                throw new InvalidOperationException(
                    $"Duplicate execution visual profile '{profile.MethodId}'.");
            }

            result.Add(profile.MethodId, profile);
        }

        foreach (var method in ExecutionMethodRules.All)
        {
            if (!result.ContainsKey(method.StringId))
            {
                throw new InvalidOperationException(
                    $"Execution method '{method.StringId}' has no visual profile.");
            }
        }

        return result;
    }
}
