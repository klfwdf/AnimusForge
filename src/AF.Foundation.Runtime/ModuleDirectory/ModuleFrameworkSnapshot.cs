using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.Refactor.Modules;

// 内部状态不依赖 Api.V1 的枚举值。公开版本映射由 API 边界负责。
internal enum ModuleFrameworkLifecycleState
{
    NotInitialized,
    Ready,
    Degraded,
    Stopped
}

/// <summary>
/// 一次显式目录查询的冻结结果。只含不可变声明/状态，不暴露活 Directory 或游戏 owner。
/// 在装配锁内采集后，可在锁外构造 public DTO；停止或重载不修改已捕获结果。
/// </summary>
internal sealed class ModuleFrameworkSnapshot
{
    internal ModuleFrameworkSnapshot(ModuleFrameworkLifecycleState state, string reasonCode,
        IEnumerable<ModuleBindingSnapshot> modules)
    {
        State = state;
        ReasonCode = reasonCode;
        Modules = new ReadOnlyCollection<ModuleBindingSnapshot>(modules.ToArray());
    }

    internal ModuleFrameworkLifecycleState State { get; }
    internal string ReasonCode { get; }
    internal IReadOnlyList<ModuleBindingSnapshot> Modules { get; }
}

/// <summary>复用原内部声明和能力结果；复制小型容器，绝不重新求值桥或缓存跨查询的能力状态。</summary>
internal sealed class ModuleBindingSnapshot
{
    internal ModuleBindingSnapshot(InternalModuleDefinition definition,
        IEnumerable<InternalCapabilityStatus> capabilities)
    {
        Definition = definition;
        Capabilities = new ReadOnlyCollection<InternalCapabilityStatus>(capabilities.ToArray());
    }

    internal InternalModuleDefinition Definition { get; }
    internal IReadOnlyList<InternalCapabilityStatus> Capabilities { get; }
}
