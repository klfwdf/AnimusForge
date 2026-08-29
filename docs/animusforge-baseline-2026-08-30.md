# AnimusForge 重构起点基线

- 记录日期：2026-08-30
- 记录用途：重构前起点；用于后续比较，不代表所有构建和游戏测试已经通过。

## 仓库与 Git

- 工作区：`E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`
- Git 根目录：当前工作区
- 基线分支：`refactor/prepare-af-restructure`（由 `main` 创建）
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`
- 基线父提交：`96a1c60f1877813a9fb3440ddad068d6e92afa1e`（policy 功能基线）
- origin：`https://github.com/klfwdf/AnimusForge.git`
- 另有 remote：`old-origin` → `https://github.com/daughenbaughedouard-sketch/Mount-Blade-Bannerlord-AnimusForge-mod.git`
- 启动准备时工作树不是干净状态：`AnimusForge/SubModule.xml` 存在用户已有修改。
- 本次准备没有回滚或覆盖该修改。

## 已知用户已有修改

`AnimusForge/SubModule.xml`：

- Module Version 从 `v1.3.7` 改为 `v1.3.7.2`；
- 文件末尾换行状态与 HEAD 不同。

该文件当前仍视为用户修改，后续不得将其误认为本次重构变更。

## 当前发布与构建契约

- `AnimusForge/SubModule.xml` 的模块 Id/Name 为 `AnimusForge`。
- XML 和 Assemblies 只声明 `AnimusForge.Bootstrap.dll`。
- Bootstrap 项目：`AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj`。
- 实现项目：`AnimusForge.csproj`，程序集名为 `AnimusForge`。
- 目标框架：`.NET Framework 4.7.2`，x64。
- 目标发布形态：一个 `Modules/AnimusForge`，内部包含 Bootstrap 与 `versions/1.3`、`versions/1.4` 两个实现位置。
- 1.3 实现应通过仓库统一构建脚本验证，不应仅凭编译常量判断引用来源。
- `README_BUILD.md` 与 `docs/bannerlord_dual_module_output.md` 是当前构建/发布契约参考。

## 构建状态

- 1.3 实现：NOT-RUN（本基线阶段尚未运行统一构建）。
- 1.4 实现：NOT-RUN（本基线阶段尚未运行）。
- Bootstrap：NOT-RUN（本基线阶段尚未运行）。
- 完整 stage/package/deploy：NOT-RUN。
- 已使用用户提供的实际 Bannerlord 根目录重试统一 stage 构建：已确认已安装游戏的 `TaleWorlds.Library.dll` 内嵌版本为 `v1.4.7.117484`；仓库 `.tmp\build_check\1.4` 目前是 `v1.4.6.115628`。
- 兼容目标按 Bannerlord `1.4.x` API 线管理，不把单个补丁版本号当作整个重构的唯一目标；但每次可复现构建仍必须记录实际引用的精确 `BuildInfo`，当前机器的代表性 1.4 引用应优先更新为与已安装游戏一致的 `v1.4.7.117484`。
- 统一构建曾成功识别仓库 1.3 引用 `v1.3.15.110062`，但当前 1.4 overlay 是 1.4.6，不能冒充 1.4.7 验证；此外实际游戏目录和 AF 源码运行目录没有 `0Harmony.dll`，默认 MCMv5 路径也不存在。
- 构建失败原因不是 C# 编译错误；当前需要准备/确认与目标 API 线匹配的引用和外部依赖闭包。不同开发者可以使用各自合法的 1.4.x 安装，但必须在基线/构建记录中写明精确版本；共享验收则应使用一套固定的代表性引用 overlay。没有部署到游戏目录。

## 功能和存档状态

- 全量功能回归：NOT-RUN。
- 三渠道交互回归：NOT-RUN。
- 代表性旧存档加载：NOT-RUN。
- 游戏内 Campaign/Mission/Encounter/Gauntlet 场景：NOT-RUN。
- 游戏基线不要求用户现在立即完成全量手测；先定义最小关键场景和记录格式，重构每个相关切片前后重复同一场景。
- 游戏基线的价值是比较“重构前/重构后”以及帮助新开发者复现，不是要求所有开发者长期重复完整测试。
- 旧存档兼容是硬目标：不得无证据改变程序集身份、序列化类型或 SyncData key；必要变化必须配套迁移测试。

## 规模和结构初步观察

- Git 索引约有 21,399 个 tracked files；其中约 17,039 个 `.cs`，约 3,331 个 `.json`，约 75 个 `docs/**` 文件（统计包含仓库现有的原版参考源码等内容）。
- 当前根项目仍是一个主要 `AnimusForge.csproj`，并包含大量根级 C# 文件和按功能分组的子目录。
- 已识别的 AF 运行/领域热点包括：`MyBehavior.cs`、`ShoutBehavior.cs`、`RewardSystemBehavior.cs`、`DuelBehavior.cs`、`DiplomacyBehavior.cs`、`WorldMapPartyCommandBehavior.cs`、`CourierDeliveryBehavior.cs`、`KnowledgeLibraryBehavior.cs`、`PolicySystem/`、Siege/Aftermath、场景/伤害、UI/百科和对话历史相关代码。
- 第一版重构地图：`docs/animusforge-refactor-map.md`。
- 第一版逐文件 owner matrix：`docs/animusforge-owner-matrix.md`。
- 这些文档是导航和审计结果，不是大规模移动源码的授权；最终迁移前仍需以实际调用、存档、线程、资源和双 API 证据复核。

## 仓库保护边界

- 不将游戏原版 DLL、存档、PlayerExports、日志、ONNX、本地缓存和编译产物作为普通源码提交内容。
- 不修改现有一键编译/覆盖/推送脚本，除非另行明确授权并先遵循双模块输出文档。
- 不在本基线阶段移动、删除或重命名生产 C#。
- 不改变程序集身份、SyncData key 或存档类型。

## 下一步

1. 审阅并修正本基线中的工作区事实。
2. 完成源码/内容/工具/脚本/文档/引用/产物分类盘点。
3. 确认代表性存档和游戏内测试方案。
4. 运行并记录可用的双版本和 Bootstrap 构建。
5. 形成第一版功能—owner—依赖—风险矩阵。

## 运行入口与组合根初步盘点

以下结论来自对 `d4cb1467` 的只读审计：

- `AnimusForge/SubModule.xml` 只声明 `AnimusForge.Bootstrap.dll`。
- `AnimusForge.Bootstrap/BootstrapSubModule.cs` 负责 Bannerlord 生命周期转发；`BootstrapRuntime.cs` 负责检测 API 线、选择一个版本化 `AnimusForge.dll` 并加载。
- Bootstrap 目前是清晰的独立边界，应保持最小化。
- 根级 `SubModule.cs` 同时承担应用组合根、Harmony 注册表、CampaignBehavior 注册、模型注册、每帧调度和关闭协调，是当前最明显的组合/拆分瓶颈。
- `SubModule.InitializeGameStarter` 注册大量 CampaignBehavior；行为注册顺序可能影响事件订阅和共享状态，迁移时必须保持顺序或显式化依赖。
- Campaign 时间事件（Tick/hourly/daily）、引擎帧 Tick（`OnApplicationTick`/`OnEngineTick`）和 Mission 生命周期目前是三种不同调度面，不能在重构中混为单一 Tick。
- `SceneActionsIntegrationBoundary` 已经是一个较好的现有边界：它负责外部 SceneActions/BattleSpeech runtime 初始化、MCM 覆盖、MissionBehavior 注册和验证；AF 侧应保持薄适配器。

## 初步目标 owner（待完整审计确认）

| 目标 owner | 当前主要入口/文件 | 当前判断 |
|---|---|---|
| Bootstrap | `AnimusForge.Bootstrap/*`, `AnimusForge/SubModule.xml` | 已有物理边界，保留 |
| Host/Composition | `SubModule.cs`, `Logger.cs`, `PerfProbe.cs`, `FreezeWatchdog.cs`, `CompatibilityAudit.cs` | 优先提取注册与调度，但先保持回调顺序 |
| Conversation/AI | `MyBehavior.cs`, `ShoutBehavior.cs`, `LordEncounterBehavior.cs`, `CourierDeliveryBehavior.cs`, proactive chat/request 类、conversation patches | 统一三渠道，但不能直接大搬家 |
| Policy | `PolicySystem/**`, `VoteDealBehavior.cs` | 已有明显逻辑边界，需继续核对存档和 UI 依赖 |
| World Simulation | `DiplomacyBehavior.cs`, `WorldDiplomacyBehavior.cs`, `VassalageBehavior.cs`, `KingdomAnnexationBehavior.cs`, `WorldMapPartyCommandBehavior.cs`, `WorldEvents/**` | 世界外交、附庸、兼并、大地图和世界事件候选域 |
| Settlement/Siege | `SiegeAiInterventionBehavior.cs`, `VillageAftermathBehavior.cs`, `AnimusForge.SiegeAftermathIntervention/**`, `CastleAftermath*.cs` | GCCZ reusable rules 与 AF 薄适配器需保持分离 |
| Mission/Combat | `DuelBehavior.cs`, `SceneTauntBehavior.cs`, `MilitaryExerciseBehavior.cs`, `TroopInspectionBehavior.cs`, prisoner mission behaviors | MissionBehavior/死亡状态/战斗生命周期候选域 |
| Progression/Social | `RewardSystemBehavior.cs`, `PlayerNotorietyBehavior.cs`, `RomanceSystemBehavior.cs`, `SexualConceptionBehavior.cs` | 奖励、RP、声望和社交候选域 |
| UI/Diagnostics | 各类 Popup/VM、百科 patch、对话 overlay、`NonBlockingErrorReport.cs` | 应从 Host 每帧调度中逐步抽出 |
| Compatibility/Safety | 各类 SafePatch、兼容 patch、异常边界 | 按实际 owner 分组，暂不创建大而全的安全 God Object |

上述 owner 只是第一版地图，不等于立即创建独立 DLL；最终迁移前还要补齐调用方、持久化、资源、线程和构建证据。
