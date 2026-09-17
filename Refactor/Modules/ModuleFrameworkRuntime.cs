using System;
using TaleWorlds.Core;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// 唯一的首版装配根：显式绑定同 DLL 的真实薄桥，然后发布只读目录。
/// 目录不是第二套执行器或总开关；旧请求仍由原 owner 校验目标、线程、标签和提交资格。
/// Campaign 注册另委托无状态装配清单；不读取当前 Campaign/Mission，不把 adapter 已装配误报成游戏请求可执行。
/// </summary>
internal static class ModuleFrameworkRuntime
{
    internal static bool Initialize(out string reasonCode)
    {
        return ModuleDirectoryLifecycleOwner.Initialize(TeamModuleRegistration.CreateDirectory, out reasonCode);
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
        ModuleDirectoryLifecycleOwner.Shutdown();
    }

    internal static ModuleFrameworkSnapshot CaptureSnapshot()
    {
        return ModuleDirectoryLifecycleOwner.CaptureSnapshot();
    }
}
