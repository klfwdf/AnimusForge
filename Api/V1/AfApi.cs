using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnimusForge.Refactor.Modules;

namespace AnimusForge.Api.V1;

/// <summary>
/// 子 MOD 的 V1 入口；与制作组 internal ports 分开，但仍编入 AnimusForge.dll。
/// 本初版只有只读能力。不要反射旧 ForExternal 方法来绕过尚未开放的请求边界。
/// Bootstrap 必须先选中并加载本机游戏版本的唯一 AF 实现；API 不加载另一份 DLL。
/// </summary>
public static class AfApi
{
    public const int ContractVersion = 1;

    private static readonly IReadOnlyList<AfCapabilityInfo> Capabilities =
        new ReadOnlyCollection<AfCapabilityInfo>(new[]
        {
            new AfCapabilityInfo(AfCapabilityIds.CatalogRead, AfCapabilityState.Available, "api.available"),
            Unsupported(AfCapabilityIds.NativeSubmit),
            Unsupported(AfCapabilityIds.SceneSubmit),
            Unsupported(AfCapabilityIds.CourierSubmit),
            Unsupported(AfCapabilityIds.ActionExecute),
            Unsupported(AfCapabilityIds.MemoryWrite),
            Unsupported(AfCapabilityIds.ExtensionRegister)
        });

    /// <summary>
    /// 查询不会初始化 AF。启动前返回 NotInitialized；停止后返回 Stopped。
    /// 不订阅 Tick、不持有 Hero/Agent，也不把“框架就绪”称为“游戏可以提交对话”。
    /// </summary>
    public static AfFrameworkSnapshot GetSnapshot()
    {
        return ModuleFrameworkRuntime.GetSnapshot(Capabilities);
    }

    /// <summary>只探测公共方法的契约，不检查内部模块的玩法资格。ID 使用精确区分大小写匹配。</summary>
    public static AfCapabilityInfo GetCapability(string capabilityId, int requestedContractVersion = ContractVersion)
    {
        if (string.IsNullOrWhiteSpace(capabilityId) || capabilityId.Length > 128)
            return new AfCapabilityInfo(string.Empty, AfCapabilityState.InvalidRequest, "api.invalid_capability_id");
        if (requestedContractVersion != ContractVersion)
            return new AfCapabilityInfo(capabilityId, AfCapabilityState.VersionMismatch, "api.contract_version_mismatch");

        // 七项固定小表，只在显式查询时读取；没有反射、注册扫描或每帧遍历。
        foreach (AfCapabilityInfo capability in Capabilities)
            if (string.Equals(capability.Id, capabilityId, StringComparison.Ordinal))
                return capability;
        return new AfCapabilityInfo(capabilityId, AfCapabilityState.UnknownCapability, "api.unknown_capability");
    }

    private static AfCapabilityInfo Unsupported(string id)
    {
        return new AfCapabilityInfo(id, AfCapabilityState.NotSupported, "api.not_supported_in_v1");
    }
}
