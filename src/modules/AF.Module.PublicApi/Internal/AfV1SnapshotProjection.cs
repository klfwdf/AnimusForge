using System.Collections.Generic;
using AnimusForge.Api.V1;
using AnimusForge.Refactor.Modules;

namespace AnimusForge.Api.Internal;

/// <summary>
/// 子 MOD V1 的唯一快照投影；依赖方向为 API → 内部契约，主体不引用 public DTO。
/// 不初始化目录、不执行模块、不读取游戏对象，也不在投影时再次评估 gate。
/// 仅显式查询分配小型快照；没有 Tick、轮询或对内部实现枚举的数值强转。
/// </summary>
internal static class AfV1SnapshotProjection
{
    internal static AfFrameworkSnapshot Create(ModuleFrameworkSnapshot snapshot,
        IReadOnlyList<AfCapabilityInfo> publicCapabilities)
    {
        var modules = new List<AfModuleInfo>();
        foreach (ModuleBindingSnapshot module in snapshot.Modules)
        {
            var capabilities = new List<AfModuleCapabilityInfo>();
            foreach (InternalCapabilityStatus status in module.Capabilities)
                capabilities.Add(new AfModuleCapabilityInfo(status.CapabilityId, status.ContractVersion,
                    MapStatus(status.State), status.ReasonCode));
            modules.Add(new AfModuleInfo(module.Definition.Id, module.Definition.ContractVersion, capabilities));
        }
        return new AfFrameworkSnapshot(MapState(snapshot.State), snapshot.ReasonCode, publicCapabilities, modules);
    }

    private static AfFrameworkState MapState(ModuleFrameworkLifecycleState state)
    {
        switch (state)
        {
            case ModuleFrameworkLifecycleState.NotInitialized: return AfFrameworkState.NotInitialized;
            case ModuleFrameworkLifecycleState.Ready: return AfFrameworkState.Ready;
            case ModuleFrameworkLifecycleState.Stopped: return AfFrameworkState.Stopped;
            default: return AfFrameworkState.Degraded;
        }
    }

    private static AfModuleCapabilityState MapStatus(InternalCapabilityState state)
    {
        switch (state)
        {
            case InternalCapabilityState.Available:
                return AfModuleCapabilityState.Available;
            case InternalCapabilityState.RegistrationOpen:
            case InternalCapabilityState.ModuleNotInitialized:
                return AfModuleCapabilityState.NotInitialized;
            case InternalCapabilityState.UnknownCapability:
            case InternalCapabilityState.Blocked:
            case InternalCapabilityState.VersionMismatch:
                return AfModuleCapabilityState.InvalidRegistration;
            default:
                return AfModuleCapabilityState.Unavailable;
        }
    }
}
