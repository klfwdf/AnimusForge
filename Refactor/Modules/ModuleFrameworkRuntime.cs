using System;
using System.Collections.Generic;
using AnimusForge.Api.V1;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// 唯一的首版装配根：显式绑定同 DLL 的真实薄桥，然后发布只读目录。
/// 目录不是第二套执行器或总开关；旧请求仍由原 owner 校验目标、线程、标签和提交资格。
/// 本类不读取 Campaign/Mission，不把 adapter 已装配误报成游戏请求可执行。
/// </summary>
internal static class ModuleFrameworkRuntime
{
    private const int InternalContractVersion = 1;
    private static readonly object Sync = new object();
    private static InternalModuleDirectory _directory;
    private static AfFrameworkState _state = AfFrameworkState.NotInitialized;
    private static string _reason = "framework.not_initialized";

    internal static bool Initialize(out string reasonCode)
    {
        lock (Sync)
        {
            if (_state != AfFrameworkState.NotInitialized && _state != AfFrameworkState.Stopped)
            {
                reasonCode = _reason;
                return _state == AfFrameworkState.Ready;
            }

            // 每次模块加载最多装配一次；不引入 DLL 发现、反射或每帧扫描。
            try
            {
                var directory = new InternalModuleDirectory(IsKnownBridge, GetBridgeRejectionReason);
                RegisterAdapter(directory, "af.team.policy", "af.team.policy.dialogue",
                    TeamModuleServices.Policy != null);
                RegisterAdapter(directory, "af.team.gathering", "af.team.gathering.dialogue",
                    TeamModuleServices.Gathering != null);
                RegisterAdapter(directory, "af.team.siege", "af.team.siege.dialogue",
                    TeamModuleServices.Siege != null, FeatureBridgeIds.ConversationSiege);

                InternalModuleValidationResult validation = directory.CompleteRegistration();
                _directory = directory;
                if (!validation.IsValid)
                {
                    _state = AfFrameworkState.Degraded;
                    _reason = "framework.registration_invalid";
                }
                else
                {
                    // 这里只报告 typed adapter 已构造，绝不表示新存档/旧存档或完整模块已验收。
                    foreach (InternalModuleStatus module in directory.GetSnapshot())
                        directory.UpdateRuntimeState(module.Definition.Id, InternalModuleRuntimeState.Ready,
                            "module.adapter_bound");
                    _state = AfFrameworkState.Ready;
                    _reason = "framework.adapters_bound";
                }
            }
            catch (Exception)
            {
                // 新目录不能使原有 gameplay 初始化崩溃；也不能对外伪造 Ready。
                // 不把异常消息/路径等实现细节通过公共 DTO 泄露给子 MOD。
                _directory = null;
                _state = AfFrameworkState.Degraded;
                _reason = "framework.initialization_failed";
            }
            reasonCode = _reason;
            return _state == AfFrameworkState.Ready;
        }
    }

    internal static void Shutdown()
    {
        lock (Sync)
        {
            _state = AfFrameworkState.Stopped;
            _reason = "framework.stopped";
            // 保留只含字符串的目录说明；绝不保留当前 Hero/Agent、任务或存档引用。
        }
    }

    internal static AfFrameworkSnapshot GetSnapshot(IReadOnlyList<AfCapabilityInfo> publicCapabilities)
    {
        lock (Sync)
        {
            var modules = new List<AfModuleInfo>();
            if (_directory != null)
            {
                foreach (InternalModuleStatus module in _directory.GetSnapshot())
                {
                    var capabilities = new List<AfModuleCapabilityInfo>();
                    foreach (InternalCapabilityDefinition definition in module.Definition.Capabilities)
                    {
                        if (_state == AfFrameworkState.Stopped)
                        {
                            capabilities.Add(new AfModuleCapabilityInfo(definition.Id, definition.ContractVersion,
                                AfModuleCapabilityState.Unavailable, "framework.stopped"));
                            continue;
                        }
                        InternalCapabilityStatus status = _directory.GetCapabilityStatus(definition.Id, definition.ContractVersion);
                        capabilities.Add(new AfModuleCapabilityInfo(definition.Id, definition.ContractVersion,
                            MapStatus(status.State), status.ReasonCode));
                    }
                    modules.Add(new AfModuleInfo(module.Definition.Id, module.Definition.ContractVersion, capabilities));
                }
            }
            return new AfFrameworkSnapshot(_state, _reason, publicCapabilities, modules);
        }
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
