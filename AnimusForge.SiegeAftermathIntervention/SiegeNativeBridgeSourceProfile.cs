namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free source codes for native Bannerlord bridge hooks used by the GCCZ scene.
/// AF adapters still own Harmony patches, mission views, order-controller binding, and agent side effects.
/// </summary>
public static class SiegeNativeBridgeSourceProfile
{
    public const string UsableTargetPrefixSource = "usable_target_prefix";

    public const string NativeFleeTickPrefixSource = "native_flee_tick_prefix";

    public const string OrderUiInitializeSource = "order_ui_initialize";

    public const string MissionOrderVmControllerSource = "mission_order_vm_controller";

    public const string MissionOrderVmHasTroopsSource = "mission_order_vm_has_troops";

    public const string MissionOrderVmCheckOpenSource = "mission_order_vm_check_open";

    public const string OrderControllerGetterSource = "order_controller_getter";

    public const string OrderPlacerAfterStartSource = "order_placer_after_start";

    public const string InjectNativeOrderViewsSource = "inject_native_order_views";

    public const string OrderUiReadySource = "order_ui_ready";

    public const string ResolveOrderControllerSource = "resolve_order_controller";

    public const string MissionOrderVmTeamSource = "mission_order_vm_team";

    public const string HasCommandableAgentsSource = "has_commandable_agents";

    public const string ControlTickOrderControllerSource = "control_tick";
}
