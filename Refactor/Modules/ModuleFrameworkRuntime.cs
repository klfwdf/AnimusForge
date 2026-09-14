using System;
using System.Collections.Generic;
using AnimusForge.Api.V1;
using TaleWorlds.Core;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// 唯一的首版装配根：显式绑定同 DLL 的真实薄桥，然后发布只读目录。
/// 目录不是第二套执行器或总开关；旧请求仍由原 owner 校验目标、线程、标签和提交资格。
/// Campaign 注册另委托无状态装配清单；不读取当前 Campaign/Mission，不把 adapter 已装配误报成游戏请求可执行。
/// </summary>
internal static class ModuleFrameworkRuntime
{
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
                var directory = TeamModuleRegistration.CreateDirectory();
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

    /// <summary>
    /// 引擎 Campaign 回调的唯一委托点。目录降级不阻断原玩法初始化；不新增启动锁或去重。
    /// 这里只注册实例，不宣告存档就绪，不持有 starter/behavior，也不在 Shutdown 重放副作用。
    /// </summary>
    internal static void RegisterCampaign(IGameStarter starterObject)
    {
        CampaignComposition.Register(starterObject);
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
