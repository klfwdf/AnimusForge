using System;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// 同 DLL typed 接缝的显式目录装配；模块清单可在这里演进，不冻结主体实现。
/// 唯一调用方是 ModuleFrameworkRuntime；复用现有 Directory，不增加注册器。
/// 只声明现有桥，Ready 仍只表示 adapter 已绑定，不表示完整玩法/存档已验收。
/// </summary>
internal static class TeamModuleRegistration
{
    private const int InternalContractVersion = 1;

    internal static InternalModuleDirectory CreateDirectory()
    {
        var directory = new InternalModuleDirectory(IsKnownBridge, GetBridgeRejectionReason);
        RegisterAdapter(directory, "af.team.policy", "af.team.policy.dialogue",
            TeamModuleServices.Policy != null);
        RegisterAdapter(directory, "af.team.gathering", "af.team.gathering.dialogue",
            TeamModuleServices.Gathering != null);
        RegisterAdapter(directory, "af.team.siege", "af.team.siege.dialogue",
            TeamModuleServices.Siege != null, FeatureBridgeIds.ConversationSiege);

        return directory;
    }

    private static void RegisterAdapter(InternalModuleDirectory directory, string moduleId,
        string capabilityId, bool adapterBound, params string[] featureBridges)
    {
        if (!adapterBound)
            throw new InvalidOperationException("module.adapter_missing");
        var definition = new InternalModuleDefinition(moduleId, InternalContractVersion,
            new[] { new InternalCapabilityDefinition(capabilityId, InternalContractVersion, featureBridges) });
        if (!directory.TryRegister(definition, out string reason))
            throw new InvalidOperationException(reason);
    }

    private static bool IsKnownBridge(string id)
    {
        foreach (string knownId in FeatureBridgeIds.All)
            if (string.Equals(knownId, id, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static string GetBridgeRejectionReason(string id)
    {
        FeatureBridgeDecision decision = FeatureBridgeRuntime.Evaluate(id, FeatureBridgeIds.ContractVersion);
        return decision.Status == FeatureBridgeDecisionStatus.Allowed ? null : decision.ReasonCode;
    }
}
