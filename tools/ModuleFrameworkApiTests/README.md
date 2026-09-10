# 模块框架 V1：公共 API 契约验收

## 这套测试回答什么

**子 MOD 看得到什么、看不到什么；框架状态是否如实报告。** 不加载 Bannerlord，不操作游戏或存档。

- 实际源码链接：`Api/V1/AfApi.cs`、`AfApiContracts.cs`、`ModuleFrameworkRuntime.cs`、`InternalModuleDirectory.cs` 与原 `FeatureBridgeContracts.cs`。
- `ModuleFrameworkUnderTest` 是独立测试 library；`ModuleFrameworkExternalClient` 是另一程序集，仅通过 `AnimusForge.Api.V1` 使用生产公开接口。
- `ModuleFrameworkControl` 是测试专用 friend 控制库，用于模拟宿主启动/停止、门禁和缺失适配器；**生产 DLL 没有增加 friend 权限**。
- `TeamModuleServices`、`FeatureBridgeRuntime` 是明确标注的测试 stub；本套不据此声称真实模块业务通过。

## 运行

在仓库根目录：

```powershell
python tools/ModuleFrameworkApiTests/run.py
# 同时检查实际构建产物，不加载 DLL，只读 PE 元数据：
python tools/ModuleFrameworkApiTests/run.py `
  --artifact-root bin/Debug/single_module_artifacts `
  --artifact-root bin/Release/single_module_artifacts
```

默认使用 `G:/AFMOD/.dotnet-sdk/dotnet.exe`，可传 `--dotnet`。仅需 SDK 8，无 NuGet 网络包；复用仓库 `.tmp/dotnet-cli` 与 `.tmp/nuget-packages`，禁用开发证书生成。生成物和原始日志留在本工具 `.generated/current/`，已忽略，不提交产物。

## 已覆盖

1. 启动前 `NotInitialized`，查询不初始化适配器、不读取门禁。
2. 公共能力仅 catalog 是 `Available`；提交对话、执行动作、写记忆、注册扩展均明确 `NotSupported`。未知 ID、精确版本、大小写、长度与空 ID 分开处理。
3. 三个初版模块的描述来自真实装配代码；Siege 门禁关闭/抛异常时 fail closed，政策不能被 `policy-world-diplomacy` 跨域门禁误当成全局关闭。
4. 重复加载、停止、重新加载、初始化失败与显式恢复；框架就绪不等于 Campaign 就绪。
5. DTO 无 setter、集合只读且构造时复制；旧快照不会被后续停止、门禁变化或源列表修改污染；256 次并发查询。
6. 独立外部 client 编译成功；另一禁止访问样例必须收到 `CS0122`（internal 类型不可访问），不能把普通编译失败算 PASS。
7. 可选实际 DLL 元数据：1.3/1.4、Debug/Release 的 V1 类型、方法参数/返回值、常量和默认参数一致；内部 ports/adapter/目录仍为 internal，公开 DTO 没有暴露游戏或内部类型。

2026-09-11：**119 个 API 断言 + 256 次并发读取 + 1 个预期拒绝编译案例；实际四份 DLL 472 个元数据断言通过。** 断言数量随覆盖更新，不代表玩法数量。

## 尚未证明

没有验证真实子 MOD 的 CLR/Bootstrap 加载顺序、游戏主线程、Campaign/Mission 状态、旧存档、live Economy、AFEF 或任何新公共执行入口。元数据相同也不等于不同游戏版本的运行行为相同。
