namespace AnimusForge.Refactor.Modules;

// 静态装配、每种接口一个无状态实例；热路径不查询 registry，也不静默降级。
// 仅覆盖已选接缝：旧 public 兼容入口及其他直接调用仍由原 owner 保留。
internal static class TeamModuleServices
{
    internal static IPolicyModulePort Policy { get; } = new PolicyModuleAdapter();
    internal static IGatheringModulePort Gathering { get; } = new GatheringModuleAdapter();
    internal static ISiegeModulePort Siege { get; } = new SiegeModuleAdapter();
}
