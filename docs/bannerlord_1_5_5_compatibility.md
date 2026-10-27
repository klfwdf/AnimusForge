# Bannerlord 1.5.5 本地适配

本轮用户授权从已安装游戏反编译，并在本次适配 AF。实际游戏为 `v1.5.5.124170`，由 `TaleWorlds.Library.dll` 的 `BuildInfo.GameVersion` 核实。旧 1.3/1.4 实现继续保留；1.5 使用独立 `versions/1.5/AnimusForge.dll`，程序集和存档类型身份仍为 `AnimusForge`。

初始离线适配阶段未授权部署、正式打包或推送。后续用户已明确授权 CN 仅 1.5 定向部署，当前安装状态见下节。既有一键入口、双实现 Stage、ZIP 和部署校验保持原状；它们目前不会交付 1.5 DLL。不要把旧双包当作本轮 1.5 安装包。下一次若要求正式交付 1.5，必须把第三实现同步接入既有 Stage/双语打包/部署 owner 的白名单、marker、hash 与验证，再按授权执行。

## 当前 CN 仅 1.5 安装

2026-10-21 按后续用户授权，AF v1.6.4 CN 已定向部署到本机游戏，安装布局只有 Bootstrap 与 `versions/1.5`；默认一键/双实现发行流程保持原样。新建 Stage 使用真实部署 owner 函数和独立单 1.5 全量白名单门禁；完整 CN 3527、EN 验证投影 3518 项通过，3527 管理文件与 10 个保留 ONNX 文件核验通过。已安装 DLL 的 33 项离线兼容回放通过，未启动游戏或旧档。准确安装路径、hash、脚本与恢复边界见[唯一主台账](animusforge-refactoring-and-repository-reorganization-plan.md#deploy-cn15only-20261021)及本机 `artifacts/deploy-cn15only-20261021/receipt.json`。

## 原版参考

`原版游戏本体代码1.5.5/`：81 个程序集、8115 个 C# 文件、81 个生成工程。本机 ILSpy 11.0.0.9375；来源、SHA-256 和诊断保存在目录内的 `_manifest.json`、`_source.json` 和 `_failures.txt`。旧版本目录保留。

Steam 安装缺少 Epic 的托管 SDK/provider，`EpicPlatformServices.cs` 有 101 条已知类型诊断；没有其他导出失败。这是阅读和适配参考，不是原版工程、AF 编译引用或 C++ 引擎源码。

## 兼容点

| 边界 | 1.5 处理 | 真实消费者 |
| --- | --- | --- |
| Bootstrap | 显式版本和 BuildInfo 均选择 V15、独立目录与 1.5 metadata；不把未来 1.6/2.0 当作受支持版本 | `BootstrapRuntime.LoadImplementation` |
| 编译 | 增加 `BANNERLORD_1_5_OR_GREATER`，复用适用的 1.4 API 分支；标记独立 1.5 flavor | `AnimusForge.csproj`、`Properties/AssemblyInfo.cs` |
| 忠诚度 | 仅旧版保留已被原版删除的两项总督文化属性 override | `AnimusForgeSettlementLoyaltyModel` |
| 王国正式名称 | 1.5 使用原 `title` XML 字段对应的新 `FormalName`，旧版仍用 `EncyclopediaTitle` | NPC 政策事实投影 |
| 藏身点发现状态 | 1.5 读 `Hideout.Settlement.IsVisible`，旧版保持 `IsSpotted` | NPC 清剿与玩家直接命令目标 |
| 队伍 perk | 1.5 新增 `out Hero` 参数，旧签名保持 | 军事演习 |
| 处刑关系 | 1.5 移除 `ExecutionRelationModel`，改接 `ExecutionCampaignBehavior.OnBloodFeudStateChanged` 的两处关系调用 | 嵌入式 Vengeance 注册；保留原版血仇状态、特质和通知 |
| 面部动画 | 1.5 目标迁至 `TaleWorlds.MountAndBlade.Source.Missions.Handlers.MissionFacialAnimationHandler` | 唇形同步抑制及启动审计 |

处刑转接只在当前线程、当前已确认的公开处刑 victim scope 中调整关系；普通处刑和嵌套无关 victim 使用原版结果。仅目标王国或无王国时本家族受影响，负值减半并远离零取整。Harmony 只在注册时安装；实际方法必须恰好包含一处 Set 和一处 Change 调用，否则拒绝部分适配。跟踪血仇唯一 Set 目标的原版关系值，后续按原版实际截限扣减量减半，避免原版已经触底的二次扣罚重新生效。没有新增 Tick 扫描、IO、反射或轮询；其余适配方法为直接属性/方法转接。

## 本地构建

先按原统一入口验证 1.3/1.4/Bootstrap；已安装游戏为 1.5 时，为原入口提供真实的保存版 1.4 引用目录。1.5 构建单独显式执行：

```powershell
& .\scripts\build\build_bannerlord15_candidate.ps1 `
  -BannerlordRoot '<当前游戏目录>' `
  -ReferenceDir '<已核实的完整1.5托管程序集快照>' `
  -OutputRoot '<仓库内尚不存在的候选目录>' `
  -Configuration Release
```

该入口核对 BuildInfo、构建 flavor、引用前后哈希，并执行真实候选 Coup 接缝门禁。候选目录保留 DLL、PDB 和 marker，不携带原版 DLL。入口不修改一键编译/覆盖脚本，不部署或打包。Bootstrap 沿用原统一入口生成的新构建。

## 验证层级

实测引用为 `v1.3.15.110062`、`v1.4.6.115628`、`v1.5.5.124170`。三条实现和 Bootstrap 构建成功，三条 Coup 管理接缝与记忆准入门禁通过。

`tests/AF.GameAdapter.Bannerlord/Bannerlord15CompatibilityTests/` 使用真实候选 DLL，每版本独立进程解析对应引用：所有 AF 托管类型、真实 BuildInfo、显式 1.3/1.4/1.5 选择、未来版本拒绝、74 项关键 API、66/65/65 个声明式游戏 Harmony 目标及命名参数、共享处刑规则夹具均通过。1.5 还验证真实血仇方法安装、重复注册、缺失调用拒绝和真实面部动画方法绑定，共 23/23/33 个顶层断言。

演习的旧 renown patch 自 1.4 起就不注册；回放读取实际注册方法 IL，确认没有该类型 token 后跳过其失效声明。1.4/1.5 的旧 `MissionCameraFadeView` 缺失仍是已有可选诊断，既有代码在这些版本跳过启动淡入延迟；不伪装成关键失败或恢复旧类型。1.3/1.4 原有面部动画可选诊断未在本轮扩大修复。

这些回放没有启动 Game/Campaign/Mission、读取玩家存档、发出模型请求或执行原生引擎动作。完整 UI、三渠道动作、真实处刑血仇交互、战斗、性能和旧档推进尚未实机验收；离线通过不等于录像场景已可用。

当前状态、准确代码坐标、构建/回放日志、候选哈希与回滚点以[唯一主台账](animusforge-refactoring-and-repository-reorganization-plan.md#bannerlord155-compat-20261021)及本机 `artifacts/bannerlord-1.5.5-reference-20261021/receipt.json` 为准。
