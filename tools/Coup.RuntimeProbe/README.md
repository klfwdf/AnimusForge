# 政变子 MOD 实际注册探针

此工具在独立 .NET Framework 4.7.2 x64 进程中，加载已安装 AF、真实游戏 managed DLL 与指定的 `AnimusForge.Coup.dll`，调用生产实现的：

- `SettlementEntryTroopSelectionBehavior.Register(Harmony)`
- `CoupRebellionBridge.Initialize()`
- `CoupGuards.Register(Harmony)`

检查四个可用标志，枚举真实 Harmony 目标和 prefix/transpiler 数量，记录源 DLL 哈希、MVID 与实际依赖路径。仅将 AF 的 `GetLogsDirectory` 重定向到工作区；不制造 `Game`、`Campaign`、`Mission`、`Agent`，不替换游戏 API，不发送 LLM 请求，不修改输入 DLL。当前内置 Coup 可将两个 DLL 参数均指定为同一份新构建的 AF 实现；必须与游戏依赖版本匹配。

```powershell
dotnet build .\tools\Coup.RuntimeProbe\Coup.RuntimeProbe.csproj -c Release -p:GameRoot='<游戏目录>'
& '.\tools\Coup.RuntimeProbe\bin\Release\net472\Coup.RuntimeProbe.exe' `
  '<游戏目录>' `
  '<游戏目录>\Modules\AnimusForge\bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll' `
  '.\extensions\AnimusForge.Coup\artifacts\1.4\AnimusForge.Coup.dll' `
  '.\artifacts\coup-runtime-probe\1.4'
```

必须选择与实际游戏安装匹配的 AF 和子 MOD 实现。不要用 1.4 游戏依赖运行 1.3 实现并宣称验证了 1.3。输出目录必须位于游戏目录之外。退出码 `0` 表示注册通过，`1` 表示绑定或可用性失败，`2` 表示参数错误；详细记录在输出目录的 `registration.log`。

本检查比成员存在性检查多验证了私有 delegate 创建、Harmony 绑定和拘押 transpiler 的实际 IL 匹配。它不启动游戏，不验证各补丁在真实 AF 全部其他补丁挂载后的组合行为，不验证原生场景、导航、指挥 UI、伤亡政治结算或存档兼容性。

入口回归调用真实 Coup delegate 和主体 owner 查询，逐项注入/清理六种 pending 对象、随行活动/保护/叛乱状态，验证缺失 delegate 和查询异常继续关闭入口。叛乱回归使用跳过构造的 MyBehavior fixture，驱动真实调度 owner 的命名、完成、消费和取消；未构造游戏或任务对象，随行 Active 仅作为状态 fixture 直接设置。拘押回归先验证未构造行为时不可用，再正常构造 CoupCaptivityBehavior 验证注册成功，并验证存档有效标志保护。因此四标志检查在行为构造后执行，避免把加载期无实例误报为拘押补丁失败；不声称测试了真实存档加载。

选兵回归另调用生产 `CreateInitializationData`，用真实managed类型的受限fixture验证owner/ItemRoster引用、四份临时名册隔离、人数上限与确认/取消回调。PartyBase真实构造需要Campaign，fixture明确用FormatterServices跳过构造；没有创建Game/Campaign/PartyVM，没有执行原版升级信息刷新、转移箭头或重置UI。日志会单列此验证边界，不能将29项数据工厂检查称作实机选兵通过。
