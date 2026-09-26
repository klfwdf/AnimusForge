using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Modules;

// Owns only the bounded internal directory lifecycle; not a second gameplay host.
internal static class ModuleDirectoryLifecycleOwner
{
    private static readonly object Sync = new object();
    private static InternalModuleDirectory _directory;
    private static ModuleFrameworkLifecycleState _state = ModuleFrameworkLifecycleState.NotInitialized;
    private static string _reason = "framework.not_initialized";

    internal static bool Initialize(Func<InternalModuleDirectory> createDirectory, out string reasonCode)
    {
        lock (Sync)
        {
            if (_state != ModuleFrameworkLifecycleState.NotInitialized && _state != ModuleFrameworkLifecycleState.Stopped)
            {
                reasonCode = _reason;
                return _state == ModuleFrameworkLifecycleState.Ready;
            }

            // 每次模块加载最多装配一次；不引入 DLL 发现、反射或每帧扫描。
            try
            {
                var directory = createDirectory();
                InternalModuleValidationResult validation = directory.CompleteRegistration();
                _directory = directory;
                if (!validation.IsValid)
                {
                    _state = ModuleFrameworkLifecycleState.Degraded;
                    _reason = "framework.registration_invalid";
                }
                else
                {
                    // 这里只报告 typed adapter 已构造，绝不表示新存档/旧存档或完整模块已验收。
                    foreach (InternalModuleStatus module in directory.GetSnapshot())
                        directory.UpdateRuntimeState(module.Definition.Id, InternalModuleRuntimeState.Ready,
                            "module.adapter_bound");
                    _state = ModuleFrameworkLifecycleState.Ready;
                    _reason = "framework.adapters_bound";
                }
            }
            catch (Exception)
            {
                // 新目录不能使原有 gameplay 初始化崩溃；也不能对外伪造 Ready。
                // 不把异常消息/路径等实现细节通过公共 DTO 泄露给子 MOD。
                _directory = null;
                _state = ModuleFrameworkLifecycleState.Degraded;
                _reason = "framework.initialization_failed";
            }
            reasonCode = _reason;
            return _state == ModuleFrameworkLifecycleState.Ready;
        }
    }

    internal static void Shutdown()
    {
        lock (Sync)
        {
            _state = ModuleFrameworkLifecycleState.Stopped;
            _reason = "framework.stopped";
            // 保留只含字符串的目录说明；绝不保留当前 Hero/Agent、任务或存档引用。
        }
    }

    internal static ModuleFrameworkSnapshot CaptureSnapshot()
    {
        lock (Sync)
        {
            var modules = new List<ModuleBindingSnapshot>();
            if (_directory != null)
            {
                foreach (InternalModuleStatus module in _directory.GetSnapshot())
                {
                    var capabilities = new List<InternalCapabilityStatus>();
                    foreach (InternalCapabilityDefinition definition in module.Definition.Capabilities)
                    {
                        if (_state == ModuleFrameworkLifecycleState.Stopped)
                        {
                            capabilities.Add(new InternalCapabilityStatus(definition.Id, module.Definition.Id, definition.ContractVersion,
                                definition.ContractVersion, InternalCapabilityState.ModuleUnavailable, "framework.stopped"));
                            continue;
                        }
                        InternalCapabilityStatus status = _directory.GetCapabilityStatus(definition.Id, definition.ContractVersion);
                        capabilities.Add(status);
                    }
                    modules.Add(new ModuleBindingSnapshot(module.Definition, capabilities));
                }
            }
            return new ModuleFrameworkSnapshot(_state, _reason, modules);
        }
    }
}
