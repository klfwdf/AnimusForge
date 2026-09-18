# 当前入口：AF 主体完整模块化总计划（2026-09-19，PLAN_READY / J04 继续 ACTIVE）

用户要求按三份仓库 Skill 一次性写出整体拆分计划。计划位于[主台账总计划节](docs/animusforge-refactoring-and-repository-reorganization-plan.md#modularization-master-plan-20260919)：基于源码 `d6824d9d` 的实际盘点（三大类方法簇统计、30 个 Saveable 文件、Harmony 密度、`Refactor/` 44 文件），给出 J04f–h → J05 Memory → J06 Knowledge → J07 Conversation/Native → J08 LLM 传输 → J09 Actions → J10 Scene/Courier → J11 制作组桥 → J12 Economy/Diplomacy/WorldMap → J13 其他领域（Weekly 首包）→ J14 public API（含用户授权的 Scene/Courier 开放）→ J15/J16/J17 结项，每包列真实入口坐标、目标 owner、切片与验收 runner。本节只是计划，不改变 J04_PARTIAL 状态，不授权推送/部署。下一执行切片为 J04f（Native/Courier 执行位置搬移）。

## 以下为 J04 第二批回执

# 当前接续：J04 第二批切片 J04_PARTIAL，五阶段边界已显式化（2026-09-19）

同分支 `codex/af-modularize-j04-20260918`，生产切片 `6315fd26`→`d6824d9d`。共享 Prompt builder 从 771 行单体拆为 80 行 orchestrator + 五阶段：`CapturePromptBuildRequest`（游戏读）→ `PromptTopicRoutingStage`（detached 输入 + host ports）→ `CapturePromptSections`（游戏读）→ `PromptAssemblyStage`（纯）→ `ApplyPromptRuntimeAppendices`（游戏读）；新增 `PromptRuleBlockText`、`PromptBuildRequest`/`PromptExclusionSets`、`PromptContextDecisions`、`PromptAssemblyStage` 五个 owner，旧实现删除。Composition 契约 142 项 + BuildPhases 源码接线契约（各 2 变异拒收）、J03 六契约与三渠道全部 runner 复跑 PASS、原脚本 Debug/Release 双 API + Bootstrap 六项 0 警告/0 错误、241 锚点地图两模式通过。**J04 仍未 OFFLINE_VERIFIED：阶段边界已显式化但执行位置未搬，Native/Courier 仍在后台线程跑全部阶段；`BuildTriggeredRuleInstructions` 未段落化。** 实机/旧档/provider `NOT-RUN`；未推送。详见[主台账第二批回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j04-slice2-20260919)与[范围图](docs/architecture/af-framework-code-scope.md)。

## 以下为 J04 首批回执

# 当前接续：J04 首批切片 J04_PARTIAL（2026-09-18）

分支 `codex/af-modularize-j04-20260918`（基线 `25a89cea`），生产切片 `e0aa8142`→`2a191526`，测试/工具/地图 `11f90fec`。共享 Prompt 组合的规则 ID 策略、内置话题路由、duel/reward/loan sticky 状态、preprocess ID 收敛、Extras 段落组合与目标身份发布已迁入 `src/modules/AF.Module.Prompt/Composition`（7 个 owner），旧实现删除，`PromptComposer.cs` 死代码删除；MyBehavior/Shout/AIConfigHandler 净 −680 行。新契约 86 项 + 2 变异拒收，J03 六契约与 Courier/Scene/Native/HeroAsset 全部 runner 复跑 PASS，原脚本 Debug/Release 双 API + Bootstrap 六项 0 警告/0 错误（无 Stage/Deploy），231 锚点地图两模式通过。**J04 未 OFFLINE_VERIFIED**：共享 builder 线程边界与 `BuildTriggeredRuleInstructions` 段落化未做；实机/旧档/provider `NOT-RUN`。用户要求的 Scene/Courier 公开提交已登记映射 J10+J14，本轮未实现。未推送。详见[主台账 J04 回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j04-slice1-20260918)与[范围图](docs/architecture/af-framework-code-scope.md)。

## 以下为 J03 交付与历史

# 当前交付：J03 源码、测试与完整规划/验收文档（2026-09-18）

用户本轮明确要求全部推送远端；本次包含下方 J03 已完成源码、测试、地图及此前未提交的两份规划/验收文档，目标为 `origin/codex/af-main-refactor-continuation-20260831`，不操作 main、不强推。范围核实见[台账交付记录](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-delivery-20260918)。`.dotnet-cli-home/`、构建产物和玩家数据不上传；下方“未推送”保留为当时实施记录，实际发布结果以远端 ref 核对为准。产品状态仍为 J03_OFFLINE_VERIFIED，不代表实机/旧档通过。

# 当前交接：J03_OFFLINE_VERIFIED（2026-09-18）

起点 `602df8fa` 的 J03a–J03e 源码与离线验收现已收口：`01dd8267` 隔离六份真实配置模型的源/兼容读取方修改，`e6c82d8d` 给生产评估入口增加仅内部逐调用的确定性 provider/资格接缝；My、Reward、Scene、Native、Policy 候选边界按实际提取方法、源码调用护栏和 Policy 实际程序集分别核对。配置 36、真实模型 18、检索 135、生产命中 7、评估/warmup 22、My 4、Reward/Scene 原片段 11、Scene/Native 包装 3、Policy 历史 1115；Courier、Scene、Native、HeroAsset 和 PersistenceProfile 回归均通过。四个获准生成目录逐一预检后，原脚本 Debug/Release 的 Bannerlord 1.3、1.4、Bootstrap 六项 0 警告/0 错误，无 Stage/Deploy/打包；两版各 789 Compile/7 EmbeddedResource，221 锚点地图 recorded/working-tree 通过。**状态仅 `J03_OFFLINE_VERIFIED`；实机、旧档、真实 provider 各 `NOT-RUN`，游戏域 fixture 不冒充实机。** 未推送，未改 J04/J06；原未提交文档差异与 `.dotnet-cli-home/` 保留。完整命令、失败反例、性能样本、源码坐标及剩余适配见[主台账 J03](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)和[范围图](docs/architecture/af-framework-code-scope.md)。

## 以下为 J03 历史部分实施交接

本次从 `602df8fa` 接续，`01dd8267` 隔离六份发布配置的源/读取方嵌套修改，`2c741536` 增加实际生产命中入口跨代契约，`a3d6c3d2` 补真实 async yield scope/mentions 检查及 220 锚点地图。配置 36、检索 132、生产入口 7，去 revision pin 的变异被拒收；相关 Courier、Scene、Native、PersistenceProfile 回归通过。预检获准的四个生成目录后，原脚本 Debug／Release 的 Bannerlord 1.3／1.4 与 Bootstrap 均 0 警告/0 错误，未 Stage/Deploy；两版各 789 Compile／7 资源，地图两模式通过。五类消费者完整生产调用链、确定性 provider/资格接缝、所有 warmup 入口及性能测量仍缺，**J03 保持 `PARTIAL / NOT_ACCEPTED`**；实机、旧档、真实 provider `NOT-RUN`。详见[主台账 J03 最新回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)和[范围图](docs/architecture/af-framework-code-scope.md)。原未提交文档差异与 `.dotnet-cli-home/` 均保留，未推送、Stage、Deploy 或打包。

最新本地切片 `313b6133`、`52cd7e47`、`3586331e` 将意图拆分／2+2 输入批次与完整规则检索编排迁入 Prompt Retrieval，生产 `AIConfigHandler` 已改为调用唯一管线，后处理规则 getter 不再泄露可变列表；`9242bcfa` 进一步为命中结果与规则正文加同 revision 外层 pin。`81b6de2a` 更新 220 锚点代码地图及范围图，两模式通过；`d34d74f3` 补双意图与配置关键词脱离契约。配置 34／检索 128；Courier、Scene、Native 相关回归与 PersistenceProfile 已复跑。最终 Debug／Release 各 1.3、1.4、Bootstrap 均 0 警告／0 错误，无 Stage/Deploy。Native History 原 runner 曾遇本机 SDK apphost 8.0.30 缺包；`dfe6b12c` 仅修构建／启动方式、未改断言，现原 runner 普通 852 项和 `--native` 27 项均通过。消费者全链路、深层不可变配置、真实 provider 与实机／旧档仍无充分证据，故 **J03 `PARTIAL / NOT_ACCEPTED`，不可记 `J03_OFFLINE_VERIFIED`**。本文件和主台账原先的未提交改动仍只作保留式增量；`.dotnet-cli-home/` 保持未跟踪。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)及[范围图](docs/architecture/af-framework-code-scope.md)。

## 以下为本轮较早的 J03 接续回执

# 当前接续：J03 部分实施，尚未验收（2026-09-18）

**最新增量（生产 `3ff315ba`，测试 `79c6dbb8`／`de6bd963`）：** 真实逐意图重排、规则文本、最终评估与辅助评分进一步归 Retrieval owner；内置 RP fallback 跨 revision 共享模型已用旧红修复。配置 31、Retrieval 119（含完整生产候选 facade 与 warmup coordinator 直接编译）、Courier 252／59／39、Scene 71／37／30、Native 589／44／184／111／852；最终生产 Debug／Release 双 Bannerlord API 加 Bootstrap 均按原脚本成功，无 Stage/Deploy。[代码范围图](docs/architecture/af-framework-code-scope.md)和 217 锚点地图两模式通过。PersistenceProfile runner 的非编译测试 key 误扫及 52 个纯行号漂移已在保留严格断言下修正，最终 **PASS**；真实 provider／实机／旧档 `NOT-RUN`。配置深层只读、端到端消费者与网络入口契约及旧类残余编排未收口，故 **`PARTIAL / NOT_ACCEPTED`，不得标 `J03_OFFLINE_VERIFIED`**。详见[主台账最新回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)。原先未提交的本文件与台账规划改动只作保留式增量，不整文件并入本地代码提交；`.dotnet-cli-home/` 未跟踪、未删除。

继续实施至源码 `e4f94429`：六配置 loader／registry、revisioned 配置与派生缓存、候选纯算法与 80-key 索引、语义召回/跨意图聚合/最终命中、辅助实体、sticky、请求 scope 和所属线程 warmup seed 均已作本地验证切片；地图 211 锚点两模式通过。生产 loader 22、Retrieval 88、Courier 252/59/39、Scene 71/37/30、Native 589/44/184/111/852；Debug/Release 原脚本 1.3/1.4/Bootstrap 成功，无 Stage/Deploy。**J03 仍 `PARTIAL / NOT_ACCEPTED`**：完整生产契约、深层只读及旧类剩余 ONNX/辅助网络与评估编排未闭合，PersistenceProfileConfigContract 仍报 `extra=['synthetic-only-key']`；实机、旧档、真实 provider `NOT-RUN`。当前详见[主台账实施回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)与[范围图](docs/architecture/af-framework-code-scope.md)。原有未提交规划差异继续保留，不整文件纳入本地代码提交；无推送/部署。

## 以下为较早的三切片部分实施回执（历史）

以[主台账当前实施状态](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)为准。三个本地切片 `3ef5e7e9`、`848fc4c2`、`d11eb572` 已将意图规范化、纯候选排序与索引、revisioned 六配置快照、请求 ambient/scope 归位并接旧入口；`AIConfigHandler` 仍持有六份 loader、规则召回/评分、sticky、辅助实体缓存及 warmup，故 **J03 PARTIAL / NOT_ACCEPTED**。197 点代码地图两模式通过；局部 34、Scene 71、queue 37 通过，Debug/Release 双 API+Bootstrap 原脚本构建通过且未 Stage/Deploy。真实配置旧红矩阵与全部消费者回归未齐，PersistenceProfileConfigContract runner 的 `extra=['synthetic-only-key']` 失败未掩盖。实机、旧档、真实 provider 均 `NOT-RUN`。未推送、部署或修改游戏目录。本任务起点已有的两份未提交规划文档改动受保护，不能误并入切片提交。

## 以下为 J03 规划前的历史交接

# 当前接续：J03 配置与检索计划就绪，尚未实施（2026-09-18）

当前入口为[主台账 J03 计划](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j03-current-plan)：实际基线 `062c5939`，已调查配置/缓存/AsyncLocal/候选与真实消费者；`IntentAnalyzer` 不存在，实际为 `IntentQueryOptimizer`。顺序为基线契约→配置 owner→纯检索/候选→规则检索/上下文→集成验收。计划已细化到可直接执行：接手模型连续完成 J03a–J03e，自行处理普通实现、接线与回归，不逐包返回规划者等待调度。本轮仍仅修改规划文档，产品尚未实施；不改产品、不构建、不派代理或发布。

产品仍为 [J02_OFFLINE_VERIFIED](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j02-full-completion)，[现有代码范围图](docs/architecture/af-framework-code-scope.md)不因计划改写。J04/J06 只定义接缝；完整线程捕获、实机/旧档及容量遗留风险保持台账所列边界。

## 以下为既有规划与交付历史，不构成本轮实施授权

# 当前接续：精简规划与执行规则补充（2026-09-18）

仓库维护 Skill 的工作包指南已补充规划与执行分工，验证见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#skill-plan-execution-20260918)。仅仓库副本和交接说明变化；外部主源/全局副本未同步，产品仍为 J02_OFFLINE_VERIFIED，本轮不启动 J03、不推送或部署。

## 以下为既有交付与实施历史，不构成本轮执行或发布授权

# 当前交付：R2、J01、J02 源码与 Skill 0.2.0（2026-09-18）

用户已明确要求将已完成源码与本轮重构文档一起推送，而非仅交付 Skill。完整范围、历史核实与验证见[唯一当前台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#full-delivery-20260918)。目标为 `origin/codex/af-main-refactor-continuation-20260831`，普通快进；不推 main、不强推、不改写历史，不部署游戏或上传本地产物。

R2 API 归位、J01 协议/消息策略、J02 Foundation 与宿主职责抽取均在本交付历史中，生产源码仍为 `9d14a1ec`；双版本构建采用该同源候选已有验收，本次未重跑游戏构建。185 点代码地图两模式与 Skill 11 项回归再次通过；实机/旧档/provider 和全仓整理未完成项保持原边界。

旧“本分支禁止推送”来自普通工程文档误混入提交时的未授权状态，不能等同于发现秘密。现已核对误提交只涉及根 HANDOFF 与主台账的规划/验证增量；受保护的本地专用交接、玩家数据及生成产物未进入本次新增历史。这些工程文档现随本次明确授权完整交付，下方旧禁推及仅本地记录仅保留历史语境。推送结果以远端 ref 核对为准。

## 以下为 Skill 重整及先前交接记录

# 当前接续：AF Skill 与配套文档重整

唯一当前状态及验收证据见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#skill-restructure-20260918)。维护 Skill 0.2.0 的主源/仓库副本、框架协调与入口已对齐；两份工具回归各 11 tests / OK，三个 Skill 元数据校验通过。当前为 SKILL_VERIFIED，仅修改 Skill、文档、模板与校验工具；保持 1.3/1.4 双版本，产品源码和构建流程未改。

产品最近验收仍是[J02 源码与离线完成](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j02-full-completion)，不等于实机/旧档验收或 G0.7 完成。无推送、部署、打包、全局安装授权；本分支含本地私密历史，不得发布。

## 以下为历史交接原文，不是当前执行或调度指令

# 当前接续：完整 J02 源码与离线验收完成（2026-09-18）

唯一当前状态为[台账 J02_OFFLINE_VERIFIED](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j02-full-completion)，取代下方仅目录子包/ACTIVE历史。生产本地提交 B `82660997`、C `469e3712`、A `9d14a1eca2c25075975134605c24d48666ee123a`；没有推送、部署或打包。

| 责任 / 已核实路径和一基坐标（源码 `9d14a1ec`） | 实际迁移与消费者 | 保留 / 未覆盖 |
| --- | --- | --- |
| `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs:7-95` | 目录4状态字段、Initialize/Shutdown/CaptureSnapshot唯一owner；Runtime门面接入；本项沿用102eab84并已集成复验 | 目录不等于通用插件热卸载Host |
| `src/AF.Foundation.Runtime/Diagnostics/DiagnosticTraceContext.cs:6-54`、`MetricWindow.cs:6-57` | Logger的AsyncLocal trace/父scope及180秒指标窗口真实退出旧根；Logger BeginTrace:280、Metric:508调用 | Logger记录文本/路径/MCM、TraceScope输出门面保留；RecordHitRate:522-616为J03/J06领域观测 |
| `src/AF.Foundation.Runtime/Diagnostics/BoundedLogWriteQueue.cs:10-147` | 4096/8192背压、drop计数/摘要节流、唯一worker门控/批量/排空；Logger EnqueueLogWrite:1366调用 | UTF8文件sink/清理原位置；Logger tokenStats队列:769-845属J08 LLM消息dump，隐私与领域队列不冒称已解决 |
| `src/AF.Foundation.Runtime/Diagnostics/PerformanceWindow.cs:8-264`、`FreezeWatchState.cs:9-215` | Perf帧/桶/事件/30秒窗口与Freeze心跳、scope、256事件环/缓存唯一owner；根门面接入 | Perf保250ms MCM缓存；Freeze保唯一实际监控线程、游戏现场读取、OS dump、文件sink；无新逐tick委托/扫描 |
| `src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs:6-65`、`GameLifetimeCoordinator.cs:7-39`；`src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs:9-88` | 已提取owner原字节归位：generation、实际Game身份、退役/准入暂停/Seal/reset-clear不重写；四迁移raw SHA相同 | Registry不是第二业务队列；J05记忆预算/J07渠道调度/请求lease与业务SyncData不在J02 |
| `src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs:11-65` | 实际My/Shout/Courier实例捕获/退役；SubModule GameEnd:98-103、Unload:105-112、InitializeGameStarter:120-140继续接线 | 不接管团队业务存档/整个游戏生命周期平台 |
| `src/AF.GameAdapter.Bannerlord/Composition/{CampaignComposition,CampaignModelComposition,TeamModuleRegistration,TeamModuleServices,ModuleFrameworkRuntime}.cs` | 五原owner归位，namespace/类型/内容不变；Runtime:11-36仍工厂与静态兼容门面，未造第二Host | Git100% rename/归一原文证明；迁前raw SHA未捕获，不冒称双向raw哈希 |
| `src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs:7-548`、`ApplicationTickComposition.cs:7-137` | Startup注册顺序/逐组catch与36相位fast/watched/异常finally/WarStats真实移交；SubModule:114-118、142-145仅引擎壳 | SubModule UIExtender/欢迎/Mission/WarStats适配仍必要引擎或J13领域责任；不重写玩法，不改公开ABI |

验证：B生命周期36+12变异、bindings15/commit19/Guard8；C全SubModule inverse、Tick36相位+5反例、Campaign42+5、Team308+3、Scene71、Native41/重排41+8；A控制6组、旧24声明oracle4组、6指定变异，A不是全三根严格inverse。Debug/Release原双API+Bootstrap+Stage日志均success，6组DLL hash一致；双API762 Compile/7资源、实际4 DLL API1060、185地图两模式通过。详细命令与证据路径见唯一台账；集成证据 `artifacts/workspace-j02-completion-20260917/integration/`。Stage的lastExternalExitCode=1为robocopy复制成功值，invocationStatus=true且无throw；Logger纯换行恢复仅归一SHA对应构建，C五搬迁缺迁前raw SHA均已声明。

保留：J03/J06 query、J08 LLM token dump、J05/J07业务预算/队列和J13 UI责任未吞入Foundation。LIVE/旧SAVE/provider/真实Harmony未验；1.3原混合引用、资产/许可/用户数据HOLD和G0.7未闭合。Stage含PlayerExports，仅本地；历史含私密提交，本分支禁推送。六份既有dirty正文保留。下一计划J03本次不自动启动。

## 以下为既有验收与交接历史

# 当前接续：J02 目录生命周期联合包已离线验证

唯一状态见[原台账 G0-CLOSE / J02-Lifecycle](docs/animusforge-refactoring-and-repository-reorganization-plan.md#parallel-controller-handover)。分类工具 `648bb084`、生产/测试 `102eab84134ee8e2ab2edb2e25d9f9aa7f560837` 已本地提交；3个Sol包完成分类/本地依赖/真实owner提取，另1个独立核验。Foundation `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs:7-95` 唯一持有4字段与Initialize:14-56、Shutdown:58-66、CaptureSnapshot:68-94；旧Runtime:11-36仅工厂和转接（RegisterCampaign:22-25）。两目录/快照源Git 100%归位，非新Host/全J02完成，迁前物理字节hash未捕获。

已验证：分类7 tests/UNKNOWN=0；源码inverse、目录44、API36/119、Composition42、Native41/重排41及18个行为反例；双API各755 Compile/7资源；用户具名批准六生成根后，Debug/Release各1.3+1.4+Bootstrap+Stage通过、6组hash一致；当次4 DLL/1060 API元数据、地图两模式167通过。日志仅在本地`artifacts/workspace-j02-directory-lifecycle/after/`。资产/许可/用户数据/旧缓存仍HOLD，G0.7全仓结项未关闭；Stage含PlayerExports，禁止打包/上传/发布，本分支禁推送不变。LIVE/旧SAVE/provider未验，1.3既有混合引用不构成纯1.3/实机证明。下一包为J02余项真实游戏生命周期/调度预算owner闭包；六份既有dirty原文保留。下方为J01及交接历史。

# 新任务交接：三实施包 + 独立核验（2026-09-17）

用户要求本任务写交接后创建新总控任务；详见[并行总控交接](docs/handoffs/2026-09-17-j01-parallel-controller-handoff.md)与[唯一台账的交接入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#parallel-controller-handover)。新任务采用 Astra 总控、最多 3 个 Sol 实施包 + 1 个 Sol 核验代理；总控独占 Git 索引/集成构建。J01 已离线完成，本任务不再实施；新任务先核门禁与冲突，不能据此跳过 G0/HOLD、推送或部署。以下 J01 停止回执仍是已完成范围，不表示 J02 已开工。

# 当前接续：J01 离线验收完成并停止；J02 未开始

- 唯一当前状态：[台账 J01 当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j01-current-status)；`J01_OFFLINE_VERIFIED / STOPPED_AFTER_J01 / J02_NOT_STARTED`，取代下方 BLOCKED/ACTIVE 过程记录。用户明确授权修 overlay 三条陈旧 `runtime_assets` 清单并保留 J01 后续有界许可；解阻提交 `0e6be296` 仅两 Policy 路径替换/一废弃旧根项删除，真实 `build_file_set()` 迁前/迁后均 297 文件/类别且按单一 Compat 路径映射全等。未调用 `create_package()`、写 dist 或动提示词/loader。
- 源码提交 `02f1747c4e226d9c8e187f2503c6197ed6148156`：`src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs:1-720` 与 `LlmVisibleReplyNormalizer.cs:1-485`（`StreamFilter:62-144`）原始字节迁移、根副本退出；原 SHA-256 分别 `95911a1ffbb2324529e4fa1156a864e13091d3c2020555c30194f76a8b1b8a74` / `76a660ee99846d4c4251dc00bf4af1a1ec472d7772f53d06765eefc48533e440`。Courier runner `:31`/单测 `:26`、overlay host_files `:102` 仅更新物理路径。J01b 的 `PrimaryChatMessagePolicy.cs:9-235` 及 `ShoutNetwork.cs` 13 接线算法不变；旧宿主传输、配置/统计/姓名责任仍未提取。
- 两 API 完整 Compile 753→754（两路径映射+policy）和 7 资源全等；协议 relocated 13 PASS/7 变异拒绝，Courier 39 PASS/8 原变异与单测 8 OK，LegacyShout 3 OK；fresh Debug/Release 各 1.3+1.4+Bootstrap+Stage、API 实物四 DLL/1056、Composition 42+5、Native 41/重排41/8变异均通过。所有真实退出码、六 DLL SHA 及单次生成日志在 `artifacts/workspace-j01-llm-protocol/after/`，协议日志在 `artifacts/tests/llm-protocol/j01cd_*`；Debug/Courier 首次**包装层**误判已留原日志，最终真实脚本/runner 成功与产品失败区分清楚。Stage 私密副本不上网、未部署。
- J01e 以源码修订 `02f1747c4e226d9c8e187f2503c6197ed6148156` 更新[代码地图](docs/architecture/af-framework-code-map.json)、[范围图](docs/architecture/af-framework-code-scope.md)与[owner matrix](docs/animusforge-owner-matrix.md)：原 142 项逐字段不变，新增 21 项协议/宿主边界锚点，recorded/working-tree 两模式各 163 PASS、exit 0；不将旧 ShoutNetwork 的真实 HTTP/SSE 调度、配置/统计/姓名、三渠道业务算作完成。已知逐字符 Unicode 流发射旧缺陷保留，LIVE/旧 SAVE/真实 provider 均 NOT-RUN。
- **本地 Git 修正与停止：** `ae8e6b89` 误把既有 dirty 文档增量纳入本地提交，`3a57007d` focused inverse 已还原该部分，工作树字节未变、六 dirty 恢复、索引空；但误提交仍在历史，含本地专用材料，**本分支禁止推送/发布**，从未取得新推送授权。J01e 本轮只编辑工作树，定向索引/提交由独立验收方负责。用户要求 J01 后停，**J02 NOT_STARTED**；不部署/建自动化。

## 以下为本轮过程记录

# 当前接续：Astra 全仓路线与 J01 联合包计划（2026-09-17）

> 当前执行入口更新：用户已批准台账 [J01a 限定基线](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j01a-执行意图2026-09-17active)；本地 J01a 为 ACTIVE，尚无新测试/构建通过回执。仅准备协议测试与 fresh before 证据，不提取/迁移生产算法，不续跑 J01b。以下原计划状态属于批准前记录。

> **J01a 最新回执（2026-09-17）：BASELINE_VERIFIED，J01b NOT_STARTED。** 本段取代上行 ACTIVE 及下方 `EXECUTION_NOT_AUTHORIZED` 批准前快照；完整命令/退出码/范围/已知原缺陷见[台账 J01a 基线回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j01a-基线回执2026-09-17baseline_verifiedj01b-not_started)。意图提交 `1d7d2cbf`，测试切片 `26444eb0`；固定/当前 before 各 13 PASS、7 变异运行拒绝，Courier 39 PASS/8 变异及单测 8 OK，LegacyShout 3 OK；fresh Debug/Release 双 API+Bootstrap+Stage 及 API/Composition/Native 原门禁通过。三生产文件未改，13 接线仍在旧 owner；迁后、LIVE、旧 SAVE、provider 网络未执行。证据根 `artifacts/workspace-j01-llm-protocol/before/`，Stage 含私密副本不可上传。等待独立验收及 J01b 单独调度，不自动续迁。

> **J01b ACTIVE（2026-09-17）：** J01a 已经 Astra 独立验收；沿用 `1d7d2cbf` 检查点，仅按台账 J2 将 8 方法/4 常量从 `ShoutNetwork.cs` 提取至新 `PrimaryChatMessagePolicy.cs` 并重接 13 调用。两个协议文件暂留根，不动 tests/csproj/overlay/地图；真实 after/extracted 与 7 变异/inverse 合格后提交，等待独立验收再迁移。

> **J01b 最新：EXTRACTION_VERIFIED / J01c NOT_STARTED（源码 `156e6836`）。** 新 owner `src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs:9-235` 拥有 4 常量和 8 原方法（逐项坐标见[台账 J01b 回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j01b-extraction)）；`ShoutNetwork.cs:260,381,408,424,450,670,751,793,800,911,1018,1253,1256` 共 13 接线，旧普通/流传输、配置/统计/姓名责任未提取。实际 after/extracted 13 PASS、7 变异指定拒绝、12 块原文与旧宿主整文件 inverse 通过；两协议文件仍在根，迁移/Stage/LIVE/旧 SAVE/provider 网络未跑。仅此切片完成，待 Astra 独立验收后另派 J01c。

> **J01c/J01d ACTIVE（2026-09-17）：** Astra 已独立验收 J01b；起点 `5611ad88`，只迁两份协议原文件与 Courier/overlay 的已列路径，运行迁后聚焦/完整离线门禁。地图/owner 文档留 J01e，Stage 私密不上网；LIVE、旧 SAVE、provider 网络仍 NOT-RUN。

> **J01c_BLOCKED / J01b_EXTRACTED_VERIFIED / J01_OFFLINE_NOT_COMPLETE（2026-09-17，取代上方 ACTIVE/STOPPED 初报）。** 真实 `build_file_set()` 退出 `1`，日志与退出码：`artifacts/workspace-j01-llm-protocol/after/overlay-build-file-set-before.{log,json}`。既有 overlay `runtime_assets` 三旧路径需对齐已跟踪 Policy 资源：`AnimusForge/CustomPrompts/CustomPolicyEvaluatorPrompt.json` → `AnimusForge/CustomPrompts/Policy/CustomPolicyEvaluatorPrompt.json`；`AnimusForge/CustomPrompts/NpcRulerPolicyPrompt.json` → `AnimusForge/CustomPrompts/Policy/NpcRulerPolicyPrompt.json`；`CustomPrompts/CustomPolicyEvaluatorPrompt.json` → 同一 Policy/CustomPolicyEvaluatorPrompt 候选且无独立 tracked 根文件，去重/意图待核。这是路径清单陈旧，不是用户资产丢失。J3 当前只准改 `host_files` Compat 路径，未授权修 `runtime_assets` 或豁免真实 `build_file_set()`；需另行精确扩 J3 后再验证。两协议源未迁移/哈希未变，未跑 Stage；迁移索引未写，仅本阻断回执写入文档索引并提交。

- 唯一当前入口：[全仓联合路线与首包执行单](docs/animusforge-refactoring-and-repository-reorganization-plan.md#workspace-joint-execution-plan)，首包详见 [J01](docs/animusforge-refactoring-and-repository-reorganization-plan.md#joint-j01-execution)。本轮仅补台账/HANDOFF，静态验证通过；`ROADMAP_READY / J01_PLAN_READY / EXECUTION_NOT_AUTHORIZED`。未修改生产/游戏 Prompt/测试/Skill，未构建、迁移、提交、推送、部署或派发代理。
- 计划给出 G0 各项缺口/解除条件、J01–J17 依赖与 owner、J01 精确路径/状态/调用者/验证/回滚。后续包是路线而非已审完的实施清单；全仓清理 gate 尚未 CLOSED，历史数据/参考/产物 HOLD 不因局部计划解除。B0 和五文件 B1 已离线验证，不重做。
- J01 是 `AF.Module.Llm/Protocol` 子域：两文件原字节归位，8 方法/4 常量从 ShoutNetwork 真正提取，13 处原调用直接接新 owner。仅协议职责，不等于网络/SSE 调度、共享 Prompt、Memory 或三渠道完成；实际算法、结构、离线构建、LIVE/旧SAVE 分栏验收。发现的测试 Newtonsoft 旧路径缺失已给固定 SDK DLL 参数方案；旧 Primary replay 的历史引用/副作用隔离留 J08，不伪装为本包已跑。
- 起点 HEAD `99360142b9b4fa5ca309cadf2cf62b627b1cdda8`，分支 `codex/af-main-refactor-continuation-20260831`；原六份 dirty 文档保护、暂存区为空。Sol 下一步须获得 J6 的限定执行批准（含本地切片提交、生成物和六个指定输出根重置）；只读批准只能核验 G0，不允许自动实施或续跑 J02。

| 本轮核实源码坐标（均为 HEAD `99360142`，一基范围） | 符号 / 计划覆盖 | 仍未覆盖 |
| --- | --- | --- |
| `ShoutNetwork.cs:251-344,364-394,451-467,486-496,602-667` | 4 常量；HasEmptyResponseRetryMarker、IsBattleSpeechRequest、GetLastMessageRole、EnsureFinalUserTurn、BuildEmptyResponseRetryMessages、ContainsAnyIgnoreCase、LooksLikeThinkingControlError、TryReadMessage；精确分段与 13 接线点见台账 J2 | `24-217` transport override/实时姓名过滤、`889-1590` 实际普通/流调用及其他混合责任；本轮未修改 |
| `LlmApiCompat.cs:1-720` | LlmApiCompat 请求/响应协议与认证头；拟原字节归位，现有 namespace/public 成员不变 | 供应商真实网络兼容、认证行为改动不在本包 |
| `LlmVisibleReplyNormalizer.cs:1-485`，`StreamFilter:62-144` | 可见回复解析与每流实例状态；现有 Courier/Scene/RpItem 消费者核实，拟原字节归位 | 不代表动作权威/三渠道执行或流缓冲性能缺口已解决 |

本轮最终静态验证回执见台账 J7；新增测试、生产构建/迁移、LIVE/旧SAVE 均非本轮执行结果。下方 B0/B1 源码基线 `64eaa7a8` 与地图记录仍保留其原含义，不改写历史通过数字。

## 以下为已完成的 R2 B0/B1 验收记录；当前任务以上方为准

# R2 B0 与五文件 B1 结构切片已离线验证（2026-09-17）

- 唯一计划为 [WORKSPACE-STRUCTURE-20260917 / P9](docs/animusforge-refactoring-and-repository-reorganization-plan.md#workspace-structure-r2-execution)。B0 初次组合编译缺口已按用户追加授权，仅在 `tools/CampaignCompositionTests/HostStubs.cs` 增加未执行的 Native API 编译桩；组合正常路径 42 项及 5 个反例通过。Native 正常/枚举重排各 41 项通过，8 个独立反例均编译成功且被运行断言拒绝。机器默认 cp936，原 UTF-8 fixture 按 `python -X utf8 -B` 运行；未改 Host/断言。
- B0a–B0e **VERIFIED**：Debug/Release 各 1.3+1.4+Bootstrap+Stage；API CoreOnly、3 反例、外部拒绝及 4 DLL/1056 元数据、142 地图记录/工作树与两项 inverse 均通过。SDK8.0.425、1.4.7 的 63 引用 manifest 和日志在 ignored 的 `local/` / `artifacts/workspace-structure-20260917/before/`。本地检查点 `e3a02cc5` 与 `aab6a5de` 仅含 B0 文件；原六份 dirty 文档仍保留且未暂存。
- 五份 V1 源文件按 P3 原字节迁移，源码/消费者提交 `64eaa7a819fcb5746a9b7ceda1e93a5040a8f0d8`，142 点当前地图提交 `99360142` 并以该源码提交为 `sourceRevision`；记录/工作树模式及两项源码 inverse 通过。1.3/1.4 Compile 各 753 项按映射完全一致、7 项资源 LogicalName 不变；迁后六 Stage、API 实际 DLL 1056 元数据、组合及 Native 正常/重排和全部预期反例通过。迁前后 DLL 原始 SHA-256 不同但大小相同，未声称 DLL 字节相等；验证依据是原始源字节、完整成员集、资源和实际 ABI 元数据。

| 已核实源码坐标（均为 `64eaa7a8`、一基行号） | 符号 / 当前责任与消费者 | 未覆盖 |
|---|---|---|
| `src/AF.Contracts/PublicApi/V1/AfApiContracts.cs:1-116` | `AfCapabilityIds:35-38` 与 V1 ID/DTO 纯契约；AfApi、两个投影及外部 client 消费 | 不代表独立契约 DLL 或新能力 |
| `src/modules/AF.Module.PublicApi/V1/AfApi.cs:14-17,34-37,55-57` | `AfApi.GetSnapshot` 调 `ModuleFrameworkRuntime.CaptureSnapshot` 与投影；`CreateDialogueClient` 调 `CoreDialogueServices` | 不代表 Scene/Courier 可提交 |
| `src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs:34-36,53-64` | `AfDialogueOperation` / `AfDialogueClient` 转发内部 Native 提交、结果与取消 | 不代表真实子 MOD 的游戏内加载 |
| `src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs:12-57` | `AfV1SnapshotProjection.Create` 将内部冻结快照投影为 V1 DTO；AfApi 与快照边界测试消费 | 不拥有框架原状态/捕获 |
| `src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs:7-39` | `AfV1DialogueProjection` 显式映射内部对话枚举；客户端与 enum 重排测试消费 | 不改变内部服务协议 |

- 本包仅目录迁移，运行频率及新增扫描/分配/锁成本为零，namespace、程序集、公开 ABI、存档和协议身份不改。原六份 dirty 文档未整份暂存；本 HANDOFF 与台账的当前状态留在其既有工作区差异中。未推送、部署、操作游戏/存档或上传含 PlayerExports 的 Stage；LIVE/旧SAVE 仍独立 NOT-RUN，业务职责拆分、B2–B7 与整体 B1 深预算未完成。

## 以下为 Astra R2 规划状态与历史记录；执行进度以上方为准

# 历史接续：Astra R2 已具体化 B0 准备方案，待批准由 Sol 执行（2026-09-17）

- 唯一当前计划：[WORKSPACE-STRUCTURE-20260917 / R2执行单P9](docs/animusforge-refactoring-and-repository-reorganization-plan.md#workspace-structure-r2-execution)，全局结构/P3首包映射仍在同一台账。工作区 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，HEAD `d92c4b3e`；本轮只更新台账与本链接式摘要，保留已有六份dirty文档，不提交/推送。
- 原 `src/content/tests/tools/scripts/docs/references/design/local/artifacts` 标准不变。首包修正为纯公开契约→Contracts、其余四个门面/投影→建议的 PublicApi 适配目录；具体五文件路径、已核实行号/符号/源码版本/调用者与未覆盖责任见 P3，不把混合大类搬目录当拆分完成。
- 已修正D盘bin直接作1.4引用目录的错误：P9固定从当前1.4.7安装按63项真实引用生成本地平铺目录，不混旧1.4.6。SDK固定官方8.0.425 ZIP，URL/SHA-512与仓库内解压路径已列明；不是把已有8.0.25当成满足旧SDK10的8.0.30要求。local排除、Native runner最小补丁、完整命令/反例/6个Stage重置根均已具体化。
- 下一步一次确认P9.7的B0→B1范围，Sol即可按固定路线先准备验证再迁五文件；不需要重新设计引用目录/SDK方案。本轮没有下载二进制、生成依赖目录、迁移、构建、安装、部署、恢复自动化或续跑旧业务；计划可执行不等于B0已验证或LIVE/旧SAVE已验收。

## 以下为 Sol 初盘历史；首包归属与环境判断以上方链接计划为准

# 历史接续：模块化工作区整理盘点，结构迁移待门禁（2026-09-17）

- 当前 Git 根 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，起点 HEAD `d92c4b3e`；本轮新写入仅[原执行台账的 `WORKSPACE-STRUCTURE-20260917` 盘点/映射](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与本摘要。上一任务 5 个未提交说明文件保留，不回滚、不作为本轮产品成果。
- 按原 `src/content/tests/tools/scripts/docs/references/design/local/artifacts` 标准建立真实 owner/消费关系、类别/HOLD 和首包旧→新路径方案。公共 V1 `Api/V1` 3 文件及 `Api/Internal` 2 文件是首个**候选**结构切片，仍编入单一 `AnimusForge.dll`；本轮未移动/删除/取消跟踪任何文件，也未提取业务责任。`MyBehavior`、`ShoutBehavior`、`CourierDeliveryBehavior` 仍为混合过渡 owner。
- 迁移前静态成员诊断：默认 1.3 与 1.4 各 753 个 Compile、0 重复；142 点代码地图的记录提交与工作树模式、API 源码逆变换 PASS。诊断使用临时 MSBuild 属性绕开本机 Windows SDK 路径读取拒绝，**不等于构建通过**。公共 API 聚焦 runner 因缺 net8.0 8.0.30 ref packs 报 NU1100；官方 1.3/1.4/Bootstrap Stage 因游戏根与完整私有 runtime 依赖缺失未运行，历史产物不可复用为本机基线。
- 下一步：在合法既有来源恢复依赖/测试 SDK 后，用原一键 Stage 和聚焦测试建立迁移前基线；再按台账精确五文件及受影响消费者清单请求成批移动确认。PlayerExports、原版参考树、许可/依赖、隐私日志与保护交接继续分类 HOLD。不推送、不部署、不改游戏或真实存档；当前目录盘点不表示阶段 8 业务重构完成。

## 以下为上一任务及历史记录

# 本机接续说明（2026-09-17，仅 AGENTS / Skill 文档调整）

- 当前核实工作区为 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，修改前 HEAD 为 `d92c4b3e`；下方 G 盘路径和其他本地分支属于历史环境，不作为本机操作目标。
- 本轮仅修正 AGENTS 工作区/分支定位、两处 API 只读说明及代码地图校验命令；生产源码和整体阶段不变，不安装外部 Skill、不推送或部署。
- 后续方向已调整为先按真实模块归属整理工作区，而非立即继续 Prompt 业务拆分。本轮仅将职责/消费者盘点、路径映射、迁移前基线、结构与行为分离验收写入既有 Skill；先形成完整结构方案，再选择可验证的模块迁移切片，不能用少量图片归档替代目标。现阶段未移动/删除/取消跟踪任何文件；历史 HOLD 按具体类别和后续明确授权处理，不一概解除。
- 本机只读核查：审计列出的 18 个生产源哈希匹配，142 个代码坐标在记录提交与工作树模式均通过；历史 6 个产物及 17 个最终日志本机缺失，未重跑构建/功能测试。此前约 60% 的口头估计不是正式验收进度。
- 本地 `origin/main` 为 `96a1c60f`，与下方历史回执不同；本轮未联网核实远端，不据此推断已同步 main。整体阶段 8 仍未完成，后续生产工作继续按最新详细 HANDOFF 和原计划推进。

# 最新发布回执（2026-09-16）

**代码及详细HANDOFF已推送到专用重构分支，远端已核对`6538cc36`；本发布说明为后续文档追加，生产源码仍6e419f6d。**

- 目标：`origin/codex/af-main-refactor-continuation-20260831`，普通快进，不推main、不强推。
- [详细HANDOFF](docs/handoffs/2026-09-16-parallel-closeout-handoff.md)及[同源码验证](docs/audits/2026-09-16-parallel-closeout-verification.json)已在仓库；下方历史“仅本地/未推送”是发布前状态。
- fresh fetch发现main新增`0a641aab`，本次未合并；当前行为对照仍固定437925b8，不能当成最新main已完整同步。
- **整体阶段8仍未完成**，剩余责任见详细交接；未部署游戏、操作存档、切默认或恢复自动化。用户草稿和本地专用简明版未上传。

## 以下为已发布候选的实施与验收记录

# 当前接续：三路并行整合已验证，整体阶段8仍未完成（2026-09-16）

**生产源码6e419f6d，仅本地。不是“全部代码完成，只差实机”。**

- [最新详细HANDOFF](docs/handoffs/2026-09-16-parallel-closeout-handoff.md) / [同候选证据](docs/audits/2026-09-16-parallel-closeout-verification.json) / [142点代码地图](docs/architecture/af-framework-code-map.json)。
- 已合并：摘要完整run授权；UTF16指纹等价提速；Native内部服务/公开submit-result-cancel；Courier双向最终准备及同源失效终结。互审修复Native跨会话/跨UI回合错投、Courier卡等待/旧标签重放。
- 最终六Stage、4DLL1056元数据、实际1.4 Courier Host、Native41/8反例、Courier252+59、记忆95终端、邻接回归、main保存身份146/36通过。详细fixture/LIVE边界见交接。
- 必交未完成：共享Prompt规则/lore及live线程、记忆记录/字符硬预算、Scene/Courier SDK、完整内部反向服务、主体大类拆净/全功能与当前候选LIVE/SAVE。旧retry生命周期也单列，不把Start reservation当整个Courier授权。
- 最后已核实推送仍10defeb4；未部署/动存档/切默认/恢复自动化，三份用户文档受保护。本地制作组简版：.tmp/parallel-closeout-20260916/team-handoff.md。

## 以下为历史记录，最新执行状态以上方及唯一台账为准

# 当前全范围收尾续点：Courier 失败回执校正（2026-09-15）

**生产/测试51844800，仅本地；阶段8全范围仍ACTIVE，非整个项目DONE。**

- [最新补充HANDOFF](docs/handoffs/2026-09-15-courier-commit-outcome-handoff.md) / [同候选审计](docs/audits/2026-09-15-courier-commit-outcome-verification.json)。[前一完整生命周期交接](docs/handoffs/2026-09-15-game-lifetime-closeout-handoff.md)仍说明此前主体接线。
- Courier已开始后的异常/空回执明确不可自动重试且效果不确定；真实成功/未开始拒绝不变，入站转译保留效果状态，日志异常不抢占回执。
- 34检查/4有效反例、邻接回归、六Stage、4DLL728元数据及实际1.4 DLL的回执→Host不重试消费通过；125点代码地图同步。
- 必交仍是规则/lore/角色资产消息准备、B1真实成本、内部双向服务、外部Native/Scene/Courier提交/结果/取消、main主体核对和当前候选LIVE/SAVE。政策/宴会/GCCZ业务不扩围。
- 最后已推送仍10defeb4；未部署/动存档/切默认/恢复自动化，三份用户文档受保护。

## 以下为此前候选记录

# 当前全范围收尾续点：Game 生命周期与待办退役（2026-09-15）

**生产/测试29448d1b，仅本地；阶段8全范围收尾仍ACTIVE，不能标DONE。**

- [最新详细HANDOFF](docs/handoffs/2026-09-15-game-lifetime-closeout-handoff.md) / [同候选审计](docs/audits/2026-09-15-game-lifetime-verification.json) / [124点坐标图](docs/architecture/af-framework-code-map.json)。
- 已接真实Game开始/结束/卸载、旧Game隔离；Native/Courier准备与最终动作/commit待办退役、已claim回执保护；My清理窗口新generation准入与订阅释放。
- 同候选六Stage、4DLL728元数据、实际Courier Host回放、主体/接口邻接回归和main保存身份146/36通过；不是实机或完整SDK验收。
- 后续必交仍为规则/lore/角色资产消息准备、B1真实成本、内部双向服务、Native/Scene/Courier版本化提交/结果/取消、main全功能/删旧与LIVE/SAVE。
- 最后已推送仍10defeb4。未部署/动存档/切默认/恢复自动化；三份用户保护文档不动。本地简明版在.tmp/game-lifetime-20260915/team-handoff.md。

## 以下为此前记录（最新状态以上方为准）

# 当前连续收尾：三渠道人设消费与信使回执（2026-09-15）

**新生产/测试807bc5b9，仅本地；全范围收尾仍ACTIVE。**

- [最新详细HANDOFF](docs/handoffs/2026-09-15-channel-persona-and-courier-receipt-handoff.md) / [候选证据](docs/audits/2026-09-15-channel-persona-verification.json)。
- 三渠道人设主线程捕获/接受及协作等待、信使双向准入、已claim动作回执已修复，保持渠道原失败策略；大类净减259行。
- 新169/10反例、owner phase16/3反例、邻接链路/原39+8反例、接口/4DLL700元数据/六Stage/身份146+36通过。112点地图绑定同源码；不是完整三渠道功能或SDK已完成。
- 下一段优先GameEnd真实失效/退役，再推进其余准备、B1、稳定内外接口。上次已推送10defeb4不包含本包；未部署/动存档/恢复自动化，用户草稿保留。

## 以下为之前的实现记录

# 当前全范围收尾：共享Hero人设路径已修复（2026-09-15）

**本地新生产/测试043b62b4；已发布到GitHub的是10defeb4，两者不要混淆。** 全范围主体收尾仍ACTIVE，三渠道提交SDK和实机/B1等必交未完成。

- [最新详细HANDOFF](docs/handoffs/2026-09-15-full-closeout-persona-handoff.md) / [同候选证据](docs/audits/2026-09-15-full-closeout-persona-verification.json)。
- 正常Hero自动补全/原外部Ensure/编辑器重生已接回主线程捕获与提交；独立预约/冷却owner，保护玩家中途编辑与清理后的新请求；原Prompt和音色语义保持，MyBehavior净减192行。
- 当前125项、7有效故障、6逆变换守卫及原15守卫通过；相关历史/渠道/内部ports/API回归、4DLL680元数据、六Stage和main身份146/36通过。105点地图绑定同源码。
- 下一步仍需渠道外围状态/准备、升格同伴、规则/lore、完整生命周期/B1、内部双向服务与三渠道SDK。未部署/动存档/恢复自动化，用户草稿不动。

## 以下为推送回执及此前候选；当前新代码以上方为准

# 当前状态：GitHub已同步，开始全范围主体收尾（2026-09-15）

- 已正常快进推送f03557fb→`10defeb4976f3ffa096a77e847fba254308f6aba`到`origin/codex/af-main-refactor-continuation-20260831`，远端ref已核对。main仍437925b8；本地草稿/专用简明版未上传。
- 用户已授权开始全范围收尾，执行依旧按[main完整矩阵](docs/phase8/af-core-main-closeout-matrix-20260915.md)和[14职责计划](docs/phase8/af-core-responsibility-decomposition-plan-20260915.md)，不是另开阶段编号。第一完整请求路径为共享Hero人设生成的捕获/网络/提交/重试生命周期；其他范围保持必交。
- 下面历史“仅本地”说明已由本发布回执更新；后续新实现不能自动当作已推送。仍不部署/操作存档/开启自动化。

## 以下为上一候选实现与验证，当前执行看总台账顶端

# 当前续点：信使双向历史捕获已接线（2026-09-15）

**生产/测试 `af754ab6`，仅本地；阶段 8 主体收尾仍 ACTIVE。** Native / Scene / Courier 对外提交 SDK 全部必交，目前仍未完成。

- [最新详细 HANDOFF](docs/handoffs/2026-09-15-courier-history-capture-handoff.md) / [同候选验证清单](docs/audits/2026-09-15-courier-history-capture-verification.json)。
- 正常回信/来信：原主线程 owner phase 捕获交付事实/历史快照，后台复用原检索，再检查会话/目标/档代；已准备空文本不重读。两个旧同步 Capture ABI 保留，不新增队列或公开能力。
- 122 新边界断言/4 有效故障、4 源码守卫，既有历史852/Native27、渠道132/后处理39、内部ports308、实际DLL648项元数据和Courier回放、六Stage及main存档身份146/36通过。100点地图绑定同源码。
- 未完成：其余 persona/preprocess/lore/消息准备、B1成本、完整生命周期、内部双向服务、三渠道SDK和实机/旧档。不能写整个信使/主体已完成。
- 未推送/部署/开启自动化，三份保护文件不变。上次远端交付f03557fb不是本轮发布回执；本轮未重新查询远端。

## 以下为历史记录；当前续点以上方为准

# 当前主体收尾：main对照与三渠道外部接口必交（2026-09-15）

**本轮生产/测试73774a94，整体收尾ACTIVE；仅本地未推送。** 用户已确认Native/Scene/Courier全部对外开放为必交目标，当前Api.V1只读仍未完成该目标。基线冻结origin/main437925b8；制作组政策/宴会/GCCZ玩法排除，只维护AF接缝。

- [主体功能/内外接口收尾矩阵](docs/phase8/af-core-main-closeout-matrix-20260915.md)
- [详细HANDOFF](docs/handoffs/2026-09-15-main-closeout-lifetime-handoff.md) / [验证清单](docs/audits/2026-09-15-main-closeout-lifetime-verification.json)
- 已完成C1基础切片：修复main的5类取消/释放缺陷，独立内部request lease，原公开签名保持；普通30项main对照、新51项/3故障、旧编译消费者换新核心、相邻pipeline/Courier/内部ports/API与六Stage通过，main存档身份146/36保持。
- 未完成：三渠道public提交/结果/取消SDK、内部双向服务边界、完整Campaign/Mission生命周期、Courier线程准备、B1深复制/预算和其他主体职责/实机/旧档验收。不能标整个主体完成。
- 自动化PAUSED，三份保护文件不变，未部署。下方已推送f03557fb是上次交付，不包含本轮新代码。

## 以下为历史记录；当前收尾以上方为准

# GitHub 重构交付回执（2026-09-15）

已正常快进推送代码与详细HANDOFF到 `klfwdf/AnimusForge` 的 `codex/af-main-refactor-continuation-20260831`。远端已核对交付提交 `1345b0bce8c2f73de6a6dfe8a8d87330280de681`；其后本回执提交仅更新文档，生产/测试仍 `f07cb2a2`。

- [最新详细HANDOFF](docs/handoffs/2026-09-15-snapshot-boundary-handoff.md) / [验证清单](docs/audits/2026-09-15-snapshot-boundary-verification.json)。同轮前置修复/装配/记忆调度HANDOFF也已包含。
- 两份用户草稿未提交；本地专用Native简明版及`.tmp`转交版、构建产物未上传。未改main、未强推、未部署游戏，自动化仍暂停。
- 阶段8/B1尚未完整验收，实机/旧存档与剩余生命周期/记忆预算门槛保留；推送不表示最终发布或零BUG。
- 下方“仅本地/未推送”为各实现轮次产生时的历史记录，已由此交付回执更新。

## 以下为实现与历史接续记录

# AF 总 HANDOFF — 内外快照边界已拆、完整对照恢复通过（2026-09-15）

**生产/测试f07cb2a2，本切片离线验证完成；整体阶段8/B1仍未完成。** Runtime不再引用Api.V1，原锁内捕获冻结内部快照、API侧独立投影，公开承诺不变。历史完整制作组测试已通过原B1严格逆变换恢复，不再有上轮的旧方法定位阻断。

- [详细HANDOFF](docs/handoffs/2026-09-15-snapshot-boundary-handoff.md) / [验证与产物哈希](docs/audits/2026-09-15-snapshot-boundary-verification.json) / [91点代码地图](docs/architecture/af-framework-code-map.json)
- 结果：完整port308/3故障、Memory逆变换15、CoreOnly无API编译、快照32/并发128/3故障、API119/并发256、Campaign42/5故障、六Stage、实际DLL584、SyncData146/行为36通过。
- 下一步：真实Campaign/Mission生命周期接缝，随后继续原14类业务职责迁移/B1记忆复制与预算；本次小目录快照不代表记忆深复制已解决，实机/旧存档/live Economy/AFEF未验。
- 仅本地提交，未推送/部署，自动化PAUSED，三份保护文件不变。下方“完整port测试仍失败/公共投影待拆”为历史状态，以上方更新为准。

## 以下为历史记录；当前实现以上方为准

# AF 总 HANDOFF — 框架装配已开始真实拆分（2026-09-15）

**生产/测试955a6be3；I1 装配切片离线验证完成，阶段8/B1整体仍未合格。** 不再只是蓝图：36个CampaignBehavior与4个包装模型装配移出SubModule，由现有ModuleFrameworkRuntime唯一委托；制作组3组typed目录声明单独提取，玩法/存档/API不变。

- [详细HANDOFF与实例作用域表](docs/handoffs/2026-09-15-composition-extraction-handoff.md)
- [验证与产物哈希](docs/audits/2026-09-15-composition-extraction-verification.json) / [86点代码地图](docs/architecture/af-framework-code-map.json)
- 验证：装配42项+5故障反例、API119/并发256、实际4DLL元数据556、六Stage、SyncData146/Behavior36；历史完整TeamModulePortParityTests仍有已存在的Native/Memory源码定位阻断，独立308port断言通过≠完整套件通过。
- 下一步先处理真实Campaign/Mission生命周期接缝，随后公共投影分离与原14类职责迁移；不把注册/目录Ready当成读档就绪，不宣称原AF所有功能已完美复现。
- 仅本地提交，未推送/部署，自动化PAUSED；保护文件未动。蓝图下方“未改C#”为历史状态，已由本次实现替代。

## 以下为历史记录，当前实现以上方为准

# AF 总 HANDOFF — 当前先做整体框架编排（2026-09-15）

用户最新优先级是框架编排，暂不继续深复制细节。本轮完成[整体编排蓝图](docs/architecture/af-core-composition-blueprint-20260915.md)：唯一装配入口、作用域/生命周期、核心依赖和一次对话执行顺序；制作组internal与子MOD public分开。

**这是设计与接续顺序，尚未改动生产装配代码。** 源码仍61d57892、阶段8/B1未整批合格；不把目录Ready当Campaign可接单，不新建第二套注册器/队列/缩水管线。

下一步先核对实例创建/释放表，再演进现有装配入口和最小真实生命周期，编排骨架稳定后继续[14类职责计划](docs/phase8/af-core-responsibility-decomposition-plan-20260915.md)。原深复制/功能对照/存档门槛保留，不勾DONE。

本轮仅文档；两份用户草稿与本地专用文件未改，自动化PAUSED，未推送/部署。已有生产验证和源码坐标见[最近模块HANDOFF](docs/handoffs/2026-09-15-memory-dispatch-owner-handoff.md)。

## 以下为历史实施记录；当前优先级以上方为准

# AF 总 HANDOFF — Memory调度职责已独立提取（2026-09-15）

**当前生产/测试61d57892；M1/M2线程接受基础子包已离线验证，整体阶段8/B1仍未整批合格。自动化PAUSED，仅本地提交，未推送/部署。**

- [本轮详细HANDOFF](docs/handoffs/2026-09-15-memory-dispatch-owner-handoff.md) / [typed内部契约](docs/architecture/af-memory-dispatch-contract.md)：队列/claim-retire/额度/耗时/异常完成迁至独立MemorySummaryDispatcher，Host156→57行，实际规划读取新owner；不保留旧队列算法。
- 共同旧新32项均过，当前37项含5个新契约；7有效反例；captured116/business36/planning24/writers238/sealing88/terminal85/commit51、15守卫、六Stage、API/存档身份通过。[验收JSON](docs/audits/2026-09-15-memory-dispatch-owner-verification.json) / [81点代码图](docs/architecture/af-framework-code-map.json)。
- **首次深来源capture/copy尚未分段**，完整writer与原子尾步仍待做。本次是独立调度owner，不是全部Memory模块/主体已拆完；按[职责计划](docs/phase8/af-core-responsibility-decomposition-plan-20260915.md)下一包接首次捕获与接受一致性，不进B2。
- 工作区G:/AFMOD/AF-REFACTOR；本地分支codex/af-framework-skill-delivery-20260911，fresh fetch远端af618912；三份保护文件原样。公开API/制作组ports、玩法/存档键与默认未改，实机/旧档/真实资产AFEF/TTS/provider/外部DLL未验。

## 以下为历史交接；当前完成范围以上方为准

# AF 总 HANDOFF — 内层覆盖修复完成，模块化收尾前计划已细化（2026-09-15）

**本轮修复生产/测试9617f96a，影响面离线验证完成；整体仍阶段8/B1，未整批验收。自动化PAUSED，仅本地提交，未推送/部署。**

- 当前技术入口：[修复与规划HANDOFF](docs/handoffs/2026-09-15-inner-structure-fix-and-modularization-handoff.md)。line/trigger内层List的同数量替换/删补/换位失效检测已修，复用原重封路径；清理两个不可达line发布标志，不改变同步规则/存档身份。
- 实际验证：88/0、旧4d同88例80/8、3个有效故障控制、captured116/business36/terminal85、13项精确源守卫、六项Stage、API/存档身份通过；[验收JSON](docs/audits/2026-09-15-inner-structure-fix-verification.json)与[78点代码图](docs/architecture/af-framework-code-map.json)绑定该代码。
- [真正职责拆分计划](docs/phase8/af-core-responsibility-decomposition-plan-20260915.md)细化原P0–P6/B1–B3，要求业务owner/实际调用迁移/旧算法删除/兼容壳和同候选验收；不是只拆partial，不冒称计划已经落地。
- 下一工作包仍是B1首次capture/copy、完整writer与接受/预算；之后B2三渠道（先Courier）、B3内部生命周期及大类剩余责任、所选public能力、P5/P6收尾前验收。单纯引用/count方案不能再次漏掉同数量变化。
- 唯一写入G:/AFMOD/AF-REFACTOR，分支codex/af-framework-skill-delivery-20260911；保留两份用户草稿及本地专用文件。未进行实机/旧档/真实资产AFEF/TTS/provider/外部DLL加载，不切默认，不改制作组玩法。

## 以下为历史交接，不覆盖当前修复与计划状态

# AF 总 HANDOFF — 深 line/trigger 预算已离线联验（2026-09-14）

**生产/测试`4d6994bc7cf219a2f894377d7262a90b466f9cbe`，本轮局部完成；阶段8/B1继续VERIFY、未整批合格。** 检查点`eb6389f4`，前生产`86805518`。自动化保持PAUSED；不进入B2；未推送、部署、改默认或操作真实存档。

- 当前入口：[深line/trigger HANDOFF](docs/handoffs/2026-09-14-b1-deep-line-trigger-handoff.md)，含源码坐标、时序变化、量测、验证及下一步；[验收JSON](docs/audits/2026-09-14-b1-deep-line-trigger-verification.json) / [77点代码图](docs/architecture/af-framework-code-map.json)绑定`4d6994bc`。
- 单draft 1024行与trigger bind按共享metadata计费；实测1×1024行9窗、窗内最多127行。trigger列表sanitize仍一次原子。同步Sanitize与续跑共用原line/bind规则。
- 封存76/0，旧40b同76例61绿15红；新反例`unbudgeted-line-normalize`与`ignore-line-source`有效。本轮未重跑captured/六Stage/API/存档身份；LIVE/SAVE=NOT_RUN。
- 明确剩余：首次capture/复制、全owner/raw/最终绑定、Apply/public/weekly尾步。不重做本轮line预算或已完成五项。[计划第16节](docs/phase8/af-core-precloseout-plan-20260913.md)直接接这些剩余项，不进入B2。
- 本工作区为 Codex worktree（detached HEAD）；指定远端仍`origin/codex/af-main-refactor-continuation-20260831`。不要占用另一 worktree 上的同名分支。

## 以下为历史暂停与切片；不覆盖上方当前续点

# AF 总 HANDOFF — 用户暂停，整体进度与代码交付（2026-09-14）

**生产开发及自动化 PAUSED；当前阶段 8 / B1，未整批合格，整个重构未 DONE。** 本轮只做核查、文档和用户明确授权的专门重构分支普通推送，不部署/操作存档/删旧/切默认。下方历史 ACTIVE 与自动继续安排全部由本入口覆盖。

- 当前唯一详细入口：[整体暂停与 GitHub 交接](docs/handoffs/2026-09-14-af-core-pause-overall-delivery-handoff.md)，含阶段、原 AF 对照、拆分/复现矩阵、源码坐标、验证、风险与恢复顺序。
- 最新已影响面离线验证生产/测试 `86805518`；源码未因本轮文档改变。[本轮核查 JSON](docs/audits/2026-09-14-af-core-paused-overall-delivery.json) / [原执行证据](docs/audits/2026-09-14-b1-owner-normalize-verification.json)。实际推送提交以远端 ref/回执为准，不把源码 commit 当文档最终 HEAD。
- 拆分现状：52 个 Refactor C#、20 个核心 owner 额外 partial；三大家族 115,983 行。新组件有真实消费者，但大类仍混合新旧，不是主体彻底拆完或仅剩实机。
- 原未审 WIP 已完成联验；索引提取、Campaign 共享预算、可续跑排序、完整 raw 摘要低分配和逐 draft 净化已完成局部验证。单 draft 深 line、初 capture/完整绑定/Apply 仍有原子成本；Courier live 读取/完整三渠道、内部生命周期、所选 public 与 LIVE/SAVE 仍未完成。
- `af-7-8` 已经应用工具暂停并读回，旧 `af` 也 PAUSED；未来实施需用户明确恢复。[原计划第 17 节](docs/phase8/af-core-precloseout-plan-20260913.md)保存此次暂停与后续入口，不重开阶段编号。
- 唯一写入 G:/AFMOD/AF-REFACTOR；本地分支 codex/af-framework-skill-delivery-20260911，指定远端 origin/codex/af-main-refactor-continuation-20260831。两份用户草稿保持 dirty 不暂存，本地专用 Native 简明版不上传。
- 本地主体简明版：G:/AFMOD/AF-REFACTOR/.tmp/AF主体简明HANDOFF-20260914.md（不提交、不作为 GitHub 文档依赖）。

- 实际交付已核实：源码和详细交接普通推送至 `dcc17ee70832e2c63725bf08b0a284f9a94429d3`（远端 ls-remote 一致）；随后只有回执文档更新，生产仍86805518。自动化保持PAUSED，交付完成不等于重构完成。

## 以下为历史切片；不覆盖上方暂停状态和当前交接

# AF 总 HANDOFF — owner净化按实际草稿计费已离线联验（2026-09-14）

**生产/测试86805518，本轮局部完成；阶段8/B1继续VERIFY、未整批合格。af-7-8每小时ACTIVE，未推送/部署。**

- 当前入口：[owner逐记录净化HANDOFF](docs/handoffs/2026-09-14-b1-owner-normalize-handoff.md)，含源码坐标、时序变化、量测、验证及下一步；前生产40b92e67、检查点7c1da946。[前轮raw摘要交接](docs/handoffs/2026-09-14-b1-raw-digest-handoff.md)保留。
- 257/65记录owner由一次全净化改为有限窗口最多8 draft；原entry规则精确共用，删/去重/排序结果私有且发布前核对key/empty/owner列表；失效重新封存建队列，保留新事实。不是owner事务。
- 75场景/26有效反例、旧40b62绿13红、12项精确源守卫与相邻/最终六Stage/API/存档身份通过。[验收JSON](docs/audits/2026-09-14-b1-owner-normalize-verification.json) / [74点代码图](docs/architecture/af-framework-code-map.json)绑定86805518。
- 明确剩余：单draft1024行仍原子处理，key/empty与全owner绑定、首次capture/raw/Apply等未硬切分。[计划第16节](docs/phase8/af-core-precloseout-plan-20260913.md)直接接这些剩余项，不重做本轮/typed raw/排序/共享窗口，不进入B2。
- 唯一写入G:/AFMOD/AF-REFACTOR，分支codex/af-framework-skill-delivery-20260911；fresh fetch远端仍3f00fefa，3份受保护文件保持。LIVE/SAVE/资产AFEF/TTS/provider/外部DLL及完整三渠道/内部接缝仍未完。

## 以下为历史交付与配置，不覆盖当前续点

# AF 总 HANDOFF — 按新计划恢复每小时自动推进（2026-09-14）

用户已授权“设置自动化开做，计划编写好”。现有 `af-7-8` 已通过应用工具恢复为 **ACTIVE，每小时一次**，目标仍为本任务，提示词及原频率已读回核对。生产仍73a6977c，前一轮交接55af6d3e；本轮只更新计划/调度配置，不新增生产修改。

- 自动执行入口：[原P0–P6计划第16节](docs/phase8/af-core-precloseout-plan-20260913.md)。从B1剩余深来源/原子预算继续，不重做已完成9项集成或素材索引提取。
- 顺序：B1记忆预算/可靠性 → 合格后B2三渠道（优先Courier线程）→ B3内部接缝 → 已选public能力与综合验收。单代理、真实职责拆薄、完整关联范围实施后集中验证。
- 当前仍阶段8/B1，未整批合格；接缝/fixture/六构建不代替实机或旧档。依赖未满足不跳批，存在明确独立工作时不为单一外部待决项全部停下。
- 唯一写入区 `G:/AFMOD/AF-REFACTOR`；保留分支、用户草稿和本地专用文档边界，不自动推送、部署、切默认、融合分支或改制作组玩法。
- 完成获准可做工作或仅余外部阻塞时，自动暂停并写技术/制作组两份HANDOFF。未来用户暂停指示立即优先，不以本次自动提示覆盖。

## 以下为历史交付；源码/测试细节保留，调度状态以上方为准

# AF 总 HANDOFF — B1 索引职责提取与集成已离线验证（2026-09-14）

**本轮生产/测试 `73a6977c` 已本地提交；整体阶段 8 / B1 仍 VERIFY、未整批合格。自动化保持暂停，未推送/部署。**

- 当前唯一入口：[本轮详细 HANDOFF](docs/handoffs/2026-09-14-b1-index-owner-integration-handoff.md)，包含原行为、源码坐标、验证、剩余问题和回滚。
- 工作区 `G:/AFMOD/AF-REFACTOR`，分支 `codex/af-framework-skill-delivery-20260911`；收到远端 `3f00fefa`，意图检查点 `ebdabd63`。其他机器路径只属历史。
- 独立事件素材索引组件已接入，删除旧 MyBehavior 索引 partial；主类家族减少45行，不冒称主体整体已拆完。权威记录/存档和制作组玩法不变。
- 旧9项源差异已通过精确审查纳入：54声明/2删除/组件锁，8个防误放测试；素材23/7反例、封存30/8反例及相邻回归、六项Stage、API532实际DLL断言/持久化身份通过。[验收JSON](docs/audits/2026-09-14-b1-index-owner-integration.json) / [58点定位图](docs/architecture/af-framework-code-map.json)绑定73a6977c。
- 下一步仍是[原计划第15节](docs/phase8/af-core-precloseout-plan-20260913.md)的 B1 深来源/原子预算；不是重做旧9项，也不进入B2。Courier live读取、完整三渠道/实机/旧档/资产AFEF/音频与所选public能力未完成。
- 两份用户草稿和指定本地专用 Native 简明版保留；本轮不恢复自动化、不覆盖游戏、不改默认或公开写能力。

## 以下为历史，当前实施以上方入口为准

# AF 总 HANDOFF — B1-P1 步骤 A：未审反例日志指针（2026-09-14）

**用户已恢复开发。当前只做阶段 8 / B1-P1 步骤 A，不是阶段 8 DONE，不进入 B2/B3。**

- 工作树：`C:\Users\klfwdf\.codex\worktrees\5d8e\Mount-Blade-Bannerlord-AnimusForge-mod-main`。当前 detached HEAD 起始于 `e8847c75`。远端基线仍是 `origin/codex/af-main-refactor-continuation-20260831`（`c2ce7947`）。生产 WIP 仍是 `c21523f8`；最后完整离线联验仍是 `62abfdb3`。
- 本切片只改说明：`tools/MemorySummaryMainThreadBoundaryTests/README.md` 补 sealing/materials `--mutate` 复现命令，并写明未审反例指针不能让 inverse 变绿。日志指针已在 `e8847c75` 的 `source-review-b1.json`。**未改生产 C#、已审 `declarations[].sha256`、`productionFileSha256`、代码地图、一键脚本。**
- Inverse `restore_memory_summary_source` 仍 **FAIL**，一次列出 9 项：`RecordEventSourceMaterial`、`RebuildEventSourceMaterialIndex`、`IsEventSourceMaterialIndexCurrent`、`BindEventSourceMaterialIndex`、`TrySealPastDailyMemoryDrafts`、`ContinueDailyMemorySeal`、`TryRunCampaignMemoryMaintenance`、`ResetDailyMemoryDraftSealSliceState`、删除 `HasPastDailyMemoryDrafts`。
- 封存 current 30/0；`--original` `62abfdb3` 19/11；8 个 mutate BUILD_PASS EXIT=1。素材 23/0、7 个 mutate EXIT=1。LIVE/SAVE=NOT_RUN。
- 六项 Stage **NOT-RUN**：本机游戏 `Modules` 缺 Harmony/MCM/UIExtenderEx。不要 `-Deploy`。MCM 仍缺。
- 代码地图仍 `sourceRevision=62abfdb3`。未推送、未部署、未覆盖游戏、未操作存档。旧自动化 `af` 仍 PAUSED。

回滚：定向 revert 本切片 README + HANDOFF 提交，保留其后用户改动，不 hard reset。

下一步：仍停在 B1-P1 步骤 A。Stage 依赖未齐前不要假装六项构建已跑；不要进 B2/B3，不要把离线 PASS 写成 LIVE/SAVE 或阶段 DONE。

## 以下为历史交接，当前状态以上方入口为准
# AF 总 HANDOFF — 用户中断，交给下一位（2026-09-14）

**当时按中断交接。用户随后要求继续 B1-P1 测试适配，不要为 B2 停住。**

- 唯一可写工作树：`C:\Users\klfwdf\.codex\worktrees\5d8e\Mount-Blade-Bannerlord-AnimusForge-mod-main`（detached）。远端基线 `origin/codex/af-main-refactor-continuation-20260831`（`c2ce7947`）。生产 WIP 仍是 `c21523f8`；最后完整离线联验仍是 `62abfdb3`。
- **生产 C# 未改。** 提交 `e8847c75` 把 sealing/materials 汇总、变体日志和 per-symbol `logs` 写入 `source-review-b1.json`。Inverse 必须继续 FAIL 并一次列出 9 个未审项。禁止刷新已审 `declarations[].sha256` / `productionFileSha256`。
- 六项 Stage **NOT-RUN**：本机游戏 `Modules` 缺 Harmony/MCM/UIExtenderEx。要用 `一键编译覆盖推送/build_single_module.ps1 -Stage`（不要 `-Deploy`），`BannerlordRoot`=`E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord`，overlay=`./.tmp/build_check/1.3` 与 `1.4`，并传 `-HarmonyCorePath .\.tmp\build_check\1.4\0Harmony.dll`；MCM 仍缺。



# AF 总 HANDOFF — B1-P1 步骤 A：WIP 反例挂到未审符号（2026-09-14）

**用户已恢复开发。当前只做阶段 8 / B1-P1 步骤 A，不是阶段 8 DONE，不进入 B2/B3。**

- 工作树：`C:\Users\klfwdf\.codex\worktrees\5d8e\Mount-Blade-Bannerlord-AnimusForge-mod-main`。起始 detached HEAD `b3bf2e4a`（sealing `--mutate`）。远端基线仍是 `origin/codex/af-main-refactor-continuation-20260831`（`c2ce7947`）。生产 WIP 仍是 `c21523f8`；最后完整离线联验仍是 `62abfdb3`。
- 本切片只改审查表/inverse：`source-review-b1.json` 给 8 个未审符号和删除的 `HasPastDailyMemoryDrafts` 挂上 sealing/materials 反例；`source_parity.py` 对未审 evidence 做 runner/harness hash 锁。**未改生产 C#、已审 `declarations[].sha256`、`productionFileSha256`、代码地图、一键脚本。**
- Inverse `restore_memory_summary_source` 仍 **FAIL**，一次列出 9 项：`RecordEventSourceMaterial`、`RebuildEventSourceMaterialIndex`、`IsEventSourceMaterialIndexCurrent`、`BindEventSourceMaterialIndex`、`TrySealPastDailyMemoryDrafts`、`ContinueDailyMemorySeal`、`TryRunCampaignMemoryMaintenance`、`ResetDailyMemoryDraftSealSliceState`、删除 `HasPastDailyMemoryDrafts`。未审 evidence 锁的是测试源码，不是验收。
- 封存 mutate 仍以上一提交 `b3bf2e4a` 为准：current 30/0；original 19/11；8 个 mutate BUILD_PASS EXIT=1。素材 23/0、7 个 mutate EXIT=1。LIVE/SAVE=NOT_RUN。
- 代码地图仍 `sourceRevision=62abfdb3`。未推送、未部署、未覆盖游戏、未操作存档。

回滚：定向 revert 本切片审查表/parity + HANDOFF 提交，保留其后用户改动，不 hard reset。

下一步：仍停在 B1-P1 步骤 A；未审 WIP 联验的六项 Stage（Debug/Release × 1.3/1.4/Bootstrap）尚未跑。不要进 B2/B3，不要把离线 PASS 写成 LIVE/SAVE 或阶段 DONE。

## 以下为历史交接，当前状态以上方入口为准

# AF 总 HANDOFF — B1-P1 步骤 A：business 编译适配（2026-09-14）

**用户已恢复开发。当前只做阶段 8 / B1-P1 步骤 A，不是阶段 8 DONE，不进入 B2/B3。**

- 工作树：`C:\Users\klfwdf\.codex\worktrees\5d8e\Mount-Blade-Bannerlord-AnimusForge-mod-main`。当前 detached HEAD 为 `6f424b45`，测试适配 `a3aa35d9`，hash 锁定 `724a0b02`，起始于 `b78b10a9`（在 `6dd5fbd7` / 审查表 `2de78e5d` 之上）；远端基线仍是 `origin/codex/af-main-refactor-continuation-20260831`（`c2ce7947`）。生产 WIP 仍是 `c21523f8`；最后完整离线联验仍是 `62abfdb3`。
- 测试适配提交 `a3aa35d9` 只改 business runner/harness，使抽出的 `TryRunCampaignMemoryMaintenance` 能编译。**未改生产 C#、已审生产 hash、代码地图、一键脚本**。`productionFileSha256` 与 `TryRunCampaignMemoryMaintenance` 的已审 `sha256` 仍绑 `62abfdb3`。
- 未把 `MyBehavior.MemorySealing.cs` 编进 business：该 suite 的 past-draft seal 仍是 fixture；`--original` `e40c92d7` 与 current 共用同一 harness，旧 `TryRun` 仍调用两参 `TrySeal` / `HasPastDailyMemoryDrafts` stub。编入真实 sealing 会在 completion 用例里跑封存并需要大量额外抽取。
- 测试替身（`BusinessHarness.cs.txt:85-89`）：`object _dailyMemorySealState`、`int _dailyMemoryDraftSealTargetDay`、`bool _dailyMemorySealCompletedPass`；`TrySealPastDailyMemoryDrafts(long, double, bool requirePendingProbe = false)` 仍 `return true`，不置 completed-pass（保持旧 `HasPast=false` 空队列不 `TryStart`）。`run_business.py:238` 默认 SDK 改为 `C:/Program Files/dotnet/dotnet.exe`，与 `run_sealing.py` 一致。
- 生产定位（行号只是定位，身份是签名，源码仍是 WIP / 未审）：`TryRunCampaignMemoryMaintenance` `MyBehavior.cs:17741`；`TrySealPastDailyMemoryDrafts(..., requirePendingProbe)` `MyBehavior.cs:4814`；`_dailyMemoryDraftSealTargetDay` `MyBehavior.cs:1926`；`_dailyMemorySealState` / `_dailyMemorySealCompletedPass` `MyBehavior.MemorySealing.cs:13` / `:16`。
- 验证（`DOTNET_EXE=C:\Program Files\dotnet\dotnet.exe`）：current **BUILD_PASS 36/0 EXIT=0**；`--original` `e40c92d7` **BUILD_PASS 4/32 EXIT=1**（新 36 例对旧实现，不是编译失败）；`--mutate maintenance-rescan` **BUILD_PASS 34/2 EXIT=1**（有效红例，不是 EXIT=2）。证据 `tools/MemorySummaryMainThreadBoundaryTests/.generated/business/{current,original,maintenance-rescan}/`。LIVE/SAVE=NOT_RUN。
- `source-review-b1.json` 仅锁定 business runner/harness 的 `testSourceSha256`，使 inverse 仍先过测试源码锁再一次列出 9 个未审项；未刷新已审 `productionFileSha256` / 声明 hash。Inverse 本机 `restore_memory_summary_source` 仍一次列出 9 个未审项。代码地图仍 `sourceRevision=62abfdb3`。未推送、未部署、未覆盖游戏、未操作存档。封存与相邻 suite 数字仍以上一入口为准。

回滚：定向 revert `6f424b45`/`724a0b02`/`a3aa35d9`，保留其后用户改动，不 hard reset。

下一步：仍停在 B1-P1 步骤 A；不要把 business 绿当成封存/WIP 已审或阶段 8 DONE。

## 以下为历史交接，当前状态以上方入口为准

# AF 总 HANDOFF — 恢复 B1-P1 步骤 A：WIP 审查表（2026-09-13）

**用户已恢复开发。当前只做阶段 8 / B1-P1 步骤 A，不是阶段 8 DONE，不进入 B2/B3。**

- 工作树：`C:\Users\klfwdf\.codex\worktrees\5d8e\Mount-Blade-Bannerlord-AnimusForge-mod-main`。当前 detached HEAD 为记录提交 `6dd5fbd7`，在审查表切片 `2de78e5d` 之上；远端基线仍是 `origin/codex/af-main-refactor-continuation-20260831`（`c2ce7947`）。生产 WIP 仍是 `c21523f8`；最后完整离线联验仍是 `62abfdb3`。
- 本切片改审查表/inverse 适配，并将 `run_sealing.py` 默认 SDK 指到本机 dotnet。**未改生产代码、代码地图 hash、一键脚本**。`productionFileSha256` 与 `TryRunCampaignMemoryMaintenance` 的已审 `sha256` 仍绑 `62abfdb3`。
- `tools/MemorySummaryMainThreadBoundaryTests/source-review-b1.json` 新增 `unreviewedWip`（8 个符号）以及未审删除 `HasPastDailyMemoryDrafts`。`source_parity.py` 在恢复旧声明前收集全部未审项并失败。
- 未审符号（行号只是定位，身份是签名）：`RecordEventSourceMaterial` `MyBehavior.cs:13705`；`RebuildEventSourceMaterialIndex` `MyBehavior.cs:20126`；`IsEventSourceMaterialIndexCurrent` / `BindEventSourceMaterialIndex` `MyBehavior.EventSourceMaterialIndex.cs:16` / `:27`；`TrySealPastDailyMemoryDrafts` `MyBehavior.cs:4814`；`ContinueDailyMemorySeal` `MyBehavior.MemorySealing.cs:198`；`TryRunCampaignMemoryMaintenance` `MyBehavior.cs:17741`；`ResetDailyMemoryDraftSealSliceState` `MyBehavior.cs:4835`；删除 `HasPastDailyMemoryDrafts`（下一锚 `SyncData`，不是业务红例）。
- Inverse **FAIL**，一次列出 9 个未审项（含删除的 `HasPastDailyMemoryDrafts`）。已审 `productionFileSha256` / `TryRunCampaignMemoryMaintenance` hash 未刷新。日志 `.tmp/b1-wip-review-20260913/official-inverse.log`。本机 `Python312\python.exe` 仍拒绝访问，改用 Codex bundled Python 跑同一 `source_parity.py`。
- 本地提交这 5 个文件：`2de78e5d`（detached HEAD，信息 `docs: inventory unreviewed B1 WIP declarations without accepting inverse`）。未 push、未 hard reset。
- 封存 30 例已跑。current **30/0**；`--original` `62abfdb3` **19 绿 / 11 红**（新 30 例对旧实现，不是 WIP 回退）。`run_sealing.py` 无 `--mutate`。证据 `.tmp/b1-sealing-20260914/`，绑定 `MyBehavior.cs` sha256 `03afca51…`。LIVE/SAVE=NOT_RUN。
- 相邻 suite（绑定当前 WIP 源码）：materials current 23/0，7 个 mutation 均 exit=1（有效红例），`62abfdb3` baseline 23 例 8 红；planning 24/0；writers 238/0；commit_writers 51/0；captured 109/0；terminal 85/0。business **EXIT=2 编译失败**（抽出的 `TryRunCampaignMemoryMaintenance` 依赖 `MemorySealing.cs` 字段/`requirePendingProbe`，共享 harness 未纳入该 partial）。未改生产语义。
- 代码地图仍 `sourceRevision=62abfdb3`。未推送、未部署、未覆盖游戏、未操作存档。

回滚：定向 `git revert 2de78e5d`，保留其后用户改动，不 hard reset。

## 以下为历史交接，当前状态以上方入口为准

# AF 总 HANDOFF — 阶段/架构/功能复现审查（2026-09-13）

**本轮最新请求是审查当前进度、详细交接和 GitHub 上传；不是恢复生产重构。自动化 `af-7-8` 保持 PAUSED。**

- 最新审查入口：[详细 HANDOFF](docs/handoffs/2026-09-13-af-stage-architecture-parity-detailed-handoff.md)，含原始 AF 对照、功能矩阵、确认问题、22 处源码坐标、复现命令和转发文案。
- **准确阶段：阶段 8 / B1-P1，PAUSED_WIP / NOT_BATCH_ACCEPTED。** 最新生产 `c21523f8`；最后完整离线联验 `62abfdb3`。不是只剩实机验收：Courier 准备线程、WIP 集成/预算、主体实际拆薄与所选公共能力仍有工作。
- 架构已有 48 个 Refactor C#、3 组 typed ports、只读 V1；19 个 owner partial 仍属原大类。不能把目录/adapter Ready 或大文件分段称为完整模块化。
- 本轮当前源码 ChannelCutover 132/0；记录版代码图 53 点通过、工作树 stale；严格 inverse 在维护入口拒绝未审 WIP。上述阻塞未通过修改 hash 掩盖。无新产品全构建、无 LIVE/SAVE。
- 后续沿用 [P0–P6 原计划第 15 节](docs/phase8/af-core-precloseout-plan-20260913.md)：先闭合 B1，再 B2 三渠道、B3 内部接缝和经选择的 public 能力；最终同候选验收后评审删旧/默认/发布，不另起阶段号。
- 只新增/修正文档和审计索引，未改生产、测试实现、游戏/存档或其他工作树。用户草稿及指定旧 Native 简明版不暂存、不上传。
- 交付分支 `codex/af-framework-skill-delivery-20260911` → `origin/codex/af-main-refactor-continuation-20260831`。本次授权普通快进，禁止分叉融合/force/顺带 main；最终推送状态看本轮 Git 回执。

## 以下为历史交接，当前状态以上方审查入口为准

# AF 总 HANDOFF — 用户要求暂停，WIP交接并推送（2026-09-13）

**当前指令：停止开发。自动化 `af-7-8` 已 PAUSED，所有实施/测试代理已停止。以下历史ACTIVE/继续B1不再生效；新指示前不恢复。**

- [简明暂停 HANDOFF / 进展表](docs/handoffs/2026-09-13-af-automation-pause-handoff.md)为当前唯一交接入口，含WIP源码坐标和未完成验证。
- 最后完整离线联验生产 `62abfdb3`；本次暂停现场 `c21523f8` 是 **WIP、未完成最终联验**。不能把前者六项Stage/API等成绩套到后者。
- 本次用户明确授权推送现有工作及HANDOFF到专门重构分支；仅允许普通快进 `origin/codex/af-main-refactor-continuation-20260831`，不融合分叉、不force/部署/切默认。
- B1仍未整批合格，B2/B3未进入，阶段8未DONE。两份用户草稿和旧local-only简明版保持；最新制作组简明版留`.tmp/af-core-precloseout-team-handoff.md`。

## 以下为历史记录，不是自动恢复或发布授权

# AF 总 HANDOFF — B1三类重验与入队资格已联验，整批预算仍未放行（2026-09-13）

## 当前唯一续点：继续同一 B1，不进入 B2

- **生产/测试 `62abfdb3`，检查点 `c3ffdd25`，前生产 `e77602f9`。** 本轮统一改三类初捕获/重试/最终来源检查、修raw状态漏检，并压掉overview资格判断的整图复制；原Build/Parse/Apply/Mark、Prompt文字、存档与公共ABI不改。
- **已验证：** captured109 / business36 / planning24 / terminal85 / commit51 / admission54；28+5+2相关故障反例、真实旧Input30红，UI/history/native与最终六项Stage、API/元数据/146键/36 behaviors保持。准确层级、路径/符号/行号和回滚见[计划第13节](docs/phase8/af-core-precloseout-plan-20260913.md)，[候选证据](docs/audits/2026-09-13-b1-context-admission-candidate.json)，[53点代码范围图](docs/architecture/af-framework-code-scope.md)。
- **玩家可理解的结果：** 请求途中数据改了，旧成功/失败结果不会覆盖新记忆；不再在每次重试/确认时重复生成所有提示词。1000行重复检查分配约1.57MB→0.24MB；2000块资格查询完整块复制2000→0。首次绑定检查增加初捕获成本；未证明实机不卡顿或深记录硬预算。
- **下一轮只做剩余责任：** 完整raw/首次捕获/Apply原子成本，封存/维护外围全扫，public/weekly尾部index miss全史扫描。不要重做已通过的分片/编辑导入或本次raw/context；B1与阶段8仍非DONE，真实游戏/旧档等继续待验。
- 工作区`G:\AFMOD\AF-REFACTOR`，分支`codex/af-framework-skill-delivery-20260911`。自动化`af-7-8`只读确认每小时ACTIVE；未新fetch/推送、未覆盖游戏/碰存档，远端`bd2ed35f`仅已知快照。D-A–D-E/public扩展/默认迁移/广泛删旧未擅自执行。
- 971项最终源码、测试/产品二进制与日志冻结在`.tmp/b1-20260913/context-admission-62abfdb3/`；两份用户草稿及旧local-only简明HANDOFF原hash保持、不暂存。简明版留`.tmp/af-core-precloseout-team-handoff.md`，不上传。

## 历史：前候选接续记录

# AF 总 HANDOFF — B1分段调度/编辑导入已联验，深来源预算未放行（2026-09-13）

## 当前任务：继续同一B1，不进入B2

- **生产/测试 `e77602f9`，检查点 `637da7f5`，前生产 `7f89e18d`。** 本轮联动完成初筛/extra/cleanup槽分片、结构验证后线性整理、计划标记与后台排序/失败汇总，补普通提交/编辑/导入实际链路，并修复旧窗口读档后误写。
- **集中验证：** 主业务36、捕获70、规划24、terminal47、commit-writers49；4096槽分类/整理各4096访问，最多8槽/片、16槽/Tick。UI/history、最终六项Stage、API/四DLL元数据、146键/36 behaviors保持；旧实际代码/故障反例仍有效。
- **不混淆边界：** 槽级分片已通过；剩余是完整来源/Pending/Apply内部及外围生产/维护原子成本，不再说整个队列规划仍未做。1000行原子capture仍约11ms观察，不能宣称深记录硬预算已过。部分失败不是事务恢复；LIVE/SAVE仍未测。
- **唯一准确续点：** [计划第12节](docs/phase8/af-core-precloseout-plan-20260913.md)，[候选证据](docs/audits/2026-09-13-b1-resumable-writers-candidate.json)，[源码位置/责任](docs/architecture/af-framework-code-scope.md)。下一步优先“大源×重复重验”，不重新盘点或重做已通过分片。
- 唯一工作区`G:\AFMOD\AF-REFACTOR`，分支`codex/af-framework-skill-delivery-20260911`。自动化`af-7-8`每小时ACTIVE；未推送、未新fetch/融合、未覆盖游戏或操作存档，已知远端快照`bd2ed35f`。完整本轮输入/日志/六DLL已保留忽略证据快照，不只依赖会刷新的current目录。
- 两份2026-09-06用户草稿及旧local-only简明HANDOFF原hash保持、不暂存。简明版留`.tmp/af-core-precloseout-team-handoff.md`，不上传。D-A–D-E及高风险动作仍待授权；回滚按用户指示定向revert `e77602f9`，不reset。
- 下方历史不覆盖本段；B1及阶段8未DONE，未承诺零BUG。

## 已确认基线：最新工程已整合并通过本机构建（启用自动化前）

用户 2026-09-13 最新要求为“拉取最新项目，先读内置 HANDOFF，按双 SKILL 开始构建”。当前只手动完成最新工程整合与本机构建基线；旧文档的暂停记录保留为历史，不覆盖本轮新授权，也不构成推送、游戏部署或恢复定时任务的授权。

- 当前唯一工作区：`G:\AFMOD\AF-REFACTOR`；本地分支 `codex/af-framework-skill-delivery-20260911`。
- 已普通合并的输入：本地双 Skill/交接 `7f0fb904` 与远端 `origin/codex/af-main-refactor-continuation-20260831@bd2ed35f`；同步检查点 `09f52234`。仅 HANDOFF 有文字冲突，保留两边历史；不改写 Git 历史。
- 运行源码与远端一致，最后生产变更为 `9040d184`（压缩记忆 post-await 主线程提交）。现在不是此前只 fetch 未落地的状态；本轮没有另外修改游戏算法/Prompt/存档/API。
- 维护 Skill：`.claude/skills/animusforge-maintainer/SKILL.md`（0.1.1 + 本地协调适配）；专门框架 Skill：`.agents/skills/af-core-framework/SKILL.md`，根 AGENTS 协调读取，同 DLL internal/public 分层不变。
- 普通合并 `4304f5bb` 已完成；本机 Debug/Release × 1.3/1.4/Bootstrap 六项构建与两套 Stage 通过，相关记忆/存档身份/实际 DLL 校验完成。准确证据和未验证边界只维护在 [同步构建台账](docs/phase8/sync-build-progress-20260913.md) 与 [验证 JSON](docs/audits/2026-09-13-sync-build-verification.json)。下面旧进度、盘符、ahead/behind、未推送/暂停描述均绑定当时版本，不能作当前状态读取。
- 阶段 8 仍 NOT_DONE。实际 job/record 预算、真实业务链回归、三类 summary 输入快照/source fingerprint 仍待实现；完整框架/公共写 API/真实 Host 与旧存档验收未完成。仅构建通过不能关闭这些项目。
- 两份 2026-09-06 用户草稿、本地专用简明 HANDOFF 保留；自动化 `af-7-8` 仍 PAUSED，不部署或操作真实存档，不自动推送。

## 历史：本地双 Skill 融合记录（同步前）维护 Skill 与框架 Skill 已协调（不继续生产重构）

- 用户本轮只授权列表核对两套 Skill 并融入本地。已提交 `6e5d1785`：维护 Skill 导入 ZIP 0.1.1 并标注本地协调适配，保留专门框架 Skill，根 AGENTS 协调读取；不全局安装、不推送、不恢复自动化。
- [本轮对照列表与交接](docs/handoffs/2026-09-13-local-skill-integration-handoff.md) 包含职责/歧义/处理结果、规则代码位置和验证层级。
- 框架仍为同一 DLL 的主体 / internal 制作组接口 / public 子 MOD API；允许批准的主体功能演进。通用 Foundation/Module/Bridge 描述不自动扩大业务范围或要求拆 DLL。
- **本机运行源码未变**：起点 `e40c92d7`（生产 `8f1cd479`）。远端已 fetch 到 `bd2ed35f`，其 4 个后续提交尚未合并；不能把远端 `9040d184` 修复当成本机已经落地。本次本地 Skill 提交也尚未上传，后续整合需另行核对，不能直接强推。
- 两个 Skill 格式、维护包零警告校验、5 个路由场景和 38 个相对链接通过；未做宿主重新发现/游戏构建或实机验收。自动化仍 PAUSED，原草稿和本地专用简明 HANDOFF 保留。


## 历史：远端暂停转手记录（bd2ed35f）

> **当前指令：停止继续重构，不得启动下一切片。** 专题转手文档见 [`docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md`](docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md)。交接写作前 `HEAD=f948f419`、detached、工作树干净；最后生产源码为 `9040d184`，相对目标远端 `e40c92d7` 为 3 ahead / 0 behind。自动化 `af` 已只读确认 `PAUSED`。用户只额外授权 docs-only 交接提交后、fetch/祖先核验通过时，精确 fast-forward push 当前历史到 `origin/codex/af-main-refactor-continuation-20260831`；不授权 force、其他 refspec、部署或继续开发。之后只有新的明确用户授权才可按专题文档的安全恢复步骤继续；不得据下方历史“下一切片/自动化继续”文字自行恢复。

## 最新本地切片：压缩记忆 post-await 主线程提交（生产/测试 `9040d184`）

状态：`OFFLINE_COMPLETE / LIVE_SAVE_PENDING`。活动 worktree 为当前仓库；checkout 保持 detached，来源/预期远端目标仍是 `origin/codex/af-main-refactor-continuation-20260831` 的 `e40c92d7`。本轮在来源上创建 intent/checkpoint `909550d4` 和聚焦代码/测试提交 `9040d184`；没有 switch、pull、merge、rebase、reset、cherry-pick、stash、push、部署或覆盖游戏。

- `MyBehavior.MemorySummaryMainThread.cs:44-73` 的 `RunMemorySummaryMainThreadAsync`：主线程直达；后台发布前后双检 `MyBehavior.Instance` 与 save generation，避免 reset 后把等待者挂在不再 tick 的旧 owner。
- `MyBehavior.MemorySummaryMainThread.cs:75-110` 的 `TryApplyMemorySummaryMainThreadAction` / `ProcessMemorySummaryMainThreadActions`：只在物理主线程接受，并复核当前 Campaign behavior；EngineTick 每次最多处理 2 个。
- `MyBehavior.cs:4957-5129` 的 `ProcessMemorySummaryQueueAsync`：三类 post-await Apply/Mark、队列清理、玩家提示和 `_memorySummaryProcessing` 释放经新边界；`MyBehavior.cs:20311-20333` 的 `OnEngineTick` 是消费者。
- `MyBehavior.cs:2415-2419` / `48278-48282`：loaded-save 瞬态重置和当前存档清理退休未开始工作。没有新增持久字段。
- 原 prompt、三渠道 role/AFEF/动作、重试/RPM/成功失败文字、默认入口和公开 ABI 不变；`Api.V1` 仍只读。政策/宴会/GCCZ 业务未改。
- 性能：低频日结 worker 每批等待接受，EngineTick 有 2 个动作硬上限；使用 ConcurrentQueue/CAS，不加热路径全量扫描、反射、锁等待或轮询。

验证：专用 17/17；精确旧源码 `e40c92d7` 和 generation/owner/unbounded-drain 三个 mutation 均按预期失败；HistorySnapshot 852、Native 27、MemoryFailureUi 85、memory recovery、weekly material、团队端口 308/3 mutation 均通过。相对来源的持久化身份为 SyncData 146/146、CampaignBehavior 36/36，单一 AnimusForge/Bootstrap 不变。最终 Debug/Release × 1.3/1.4/Bootstrap 六项构建和两套项目内 Stage 通过，四份实现 DLL 532 个元数据断言通过；仅有无法联网读取漏洞元数据的 `NU1900`，0 error。

详细边界：[架构说明](docs/architecture/af-memory-summary-mainthread-boundary.md)；[验证 MD](docs/audits/2026-09-12-memory-summary-mainthread-verification.md) / [JSON](docs/audits/2026-09-12-memory-summary-mainthread-verification.json)；[本轮进度](docs/phase8/memory-summary-mainthread-progress-20260912.md)；[范围图](docs/architecture/af-framework-code-scope.md) / [机器定位图](docs/architecture/af-framework-code-map.json)。

明确未覆盖：首次 await 前的调度快照和三个 Execute job 的 live prompt/目标准备仍可能在异步 continuation 读 owner/game；逐任务 source revision/fingerprint 未实现。真实游戏、provider、旧存档和读档晚返回均未验收，不能宣称完整 memory 线程安全或阶段八完成。

下一独立切片：只为 `ExecuteMemorySummaryJobAsync` / `ExecuteMajorActionSummaryJobAsync` / `ExecuteMemoryOverviewJobAsync` 在 Campaign 主线程捕获只读输入和精确 source fingerprint，后台只做 provider/解析，并在本轮接受边界拒绝来源已改变的结果。先建精确旧源码红例，不扩到 Courier、公共写 API 或默认切换。回滚只按用户指示定向 inverse/revert `9040d184`。

## 前序交付：框架 Skill / 代码位置 / 新旧范围

**GitHub 交付已确认：`38c003ab` 已推到原重构分支（此前为 `a58c2191`）。本记录随后单独提交；确切最新末端以远端 ref 为准。指定制作组简明 HANDOFF 未进入上传文件树或新增提交历史。**

用户本轮授权把框架和维护要求写成仓库 Skill 并推送专门重构分支，**指定 Native history 制作组简明版只留本地**。本轮不改游戏运行代码，不恢复自动化；下面历史段落中的“未推送/自动化继续”等仅代表当时状态。

- Skill：`.agents/skills/af-core-framework/SKILL.md`，根 `AGENTS.md` 已接入读取规则。允许批准的主体功能演进，不把当前算法、模块名单、只读 API 状态写成永久上限；稳定分层、公开兼容与唯一权威提交责任。
- 新的可上传交接：[框架 Skill GitHub HANDOFF](docs/handoffs/2026-09-11-framework-skill-github-handoff.md)，包含逐项代码路径、行号、符号和责任注释。
- 当前源码坐标：[范围图](docs/architecture/af-framework-code-scope.md) / [JSON](docs/architecture/af-framework-code-map.json)，核对源码 `8f1cd479`，25 个定位点；新接缝/混合 owner/仍运行旧入口/不处理业务分开标注，不搬动仍在用的旧源码。
- 本地干净交付分支：`codex/af-framework-skill-delivery-20260911` → 获准远端 `origin/codex/af-main-refactor-continuation-20260831`；旧远端基线 `a58c2191`。原本地同名来源分支只保留历史，不能直接推送其中含本地专用文档的提交。
- 发布校验/状态见 `docs/phase8/framework-skill-publish-progress-20260911.md`。运行代码仍为 `8f1cd479`；本轮为 Skill/文档/定位校验工具，未产生新游戏构建或实机证据。自动化仍 PAUSED，阶段 8 未 DONE。

### 主体关键位置（源码 8f1cd479）

- `Refactor/Modules/TeamModulePorts.cs:7-10` — `internal interface IPolicyModulePort`：政策 typed 接缝，业务归原 owner。
- `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` — `internal static class ModuleFrameworkRuntime`：装配与只读投影，不是第二套执行器。
- `Api/V1/AfApi.cs:13-16` — `public static class AfApi`：当前只读；其他提交/写能力未开放。
- `MyBehavior.HistoryPromptSnapshot.cs:35-38` — `internal static Func<string> CaptureHistoryContextWorkById`：召回用途投影，非全局记忆事务。
- `ShoutBehavior.cs:16180-16183` — `private static Func<string> CaptureNativeConversationPersistedHistoryWork`：原身份解析在主线程捕获，非全 Shout 重写。
- `MyBehavior.cs:28087-28090` — `public static string BuildHistoryContextForExternal(`：Scene/Courier 仍调用，共享兼容入口不能盲删。

## 上一轮状态：记忆快照完成，自动化暂停

**上一轮要求为“工作完成后暂停自动化，写两份 handoff”。Native 持久历史快照已验证并本地提交 `8f1cd479`，`af-7-8` 已设为 PAUSED 并回读确认；此刻只交接，不自动开始下一项。下面历史记录中的“自动化继续/下一轮”仅是当时状态，不构成恢复授权。整个阶段 8 未 DONE。**

上一轮的制作组简明版按新要求仅留本地，不作为 GitHub 交付文档或链接依赖；本轮可上传版本见顶部专题 HANDOFF。公共 Api.V1 仍只读。

## 最新续作：Native 持久历史输入快照（生产/测试 8f1cd479）

- 在原主线程队列核对 Native admission，捕获原 Hero/普通人物身份、owner、generation、总览、场景/日期/设置、召回查询和块/AFEF 投影；后台复用原召回/筛选/格式，结果使用前再验原 admission。
- 保留原最新块、两种筛选模式、候选/填充/顺序、Summary 与 AFEF、主动开场输入语义和 history-only 失败空串 fallback。少量历史跳过不需要的草稿查询构造，ONNX/API 不整段搬到主线程。
- 删除被替代的 `BuildNativeConversationPersistedHistoryContextForPrompt` 和原后台 identity/owner 接线。原 public 历史签名与 Scene/Courier 的共享默认入口仍有调用责任，未删除；存档身份/默认渠道/制作组业务均未改。
- 新 memory 852 / 120 组合、Native 27 检查；旧实现分别 305 / 12 个断言失败，候选通过；10 个新变异被 runtime 拒绝。既有 UI/Native/ports 及 26 个旧变异复验通过。最终源码的六项 Stage、16 组相关回归、实际四 DLL 532 元数据检查均通过；无证据的发布门禁仍拒绝。
- 技术边界：[记忆快照说明](docs/architecture/af-native-history-snapshot-boundary.md)；证据：[审计 MD](docs/audits/2026-09-11-native-history-snapshot-verification.md) / [JSON](docs/audits/2026-09-11-native-history-snapshot-verification.json)；台账：[本轮进度](docs/phase8/native-history-snapshot-progress-20260911.md)。原始日志在 `.tmp/native-history-snapshot-20260911/`，只留本地。
- 代码文件：`MyBehavior.HistoryPromptSnapshot.cs`、`MyBehavior.cs`、`ShoutBehavior.cs`；真实旧代码对照 `659bb998`，本轮检查点 `e1a09954`。回滚需经用户指示定向反转 `8f1cd479` 并复跑测试；不 reset、不覆盖用户草稿。

### 恢复后才做的工作

1. 先核对实际 HEAD、工作树和新用户指示；两份 2026-09-06 草稿仍有用户改动，不纳入自己的提交。不能仅因定时提示或历史 handoff 就恢复自动化。
2. 继续检查其他后台维护/压缩 writer；当前证明的是捕获后不再共享可变列表，**未证明与所有 writer 并发捕获的全局原子性**。此投影缺少本路径不读的字段，不能作为完整存档块写回。
3. 再沿 Native persona/规则/独立周报绑定/剩余游戏对象读取与 TTS 直接回调检查；随后推进 Courier 双向早期 prepare。Scene/Courier 本轮尚未接入该快照入口。
4. 实机核对英雄/普通人物、空/多历史、主动开场、换会话/读档晚返回、失败提示、AFEF 内容及主线程耗时，再考虑新公共提交/生命周期能力。已开始网络不可伪称取消，空历史 fallback 不可伪称严格读取成功。
5. 整个阶段 8 的真实 Host、旧存档、新外部 DLL 加载/升级与最终清理仍需独立验收；不因本轮 PASS 自动切默认路径、删所有旧 facade 或推送/部署。

## 前序续作：记忆失败提示（生产/测试 6f0bac67）

- 深层记忆审查发现 9 处失败出口可从后台直接弹 UI；已改为有界待提示，由原 EngineTick 消费，核对 owner / 实际 Campaign / 操作 generation / 展示 revision。旧确认、显示失败重入、日志失败不再干扰新提示。
- 原错误文字、按钮/暂停、召回/筛选/总结算法与重试策略保持。读档和现有数据清理的瞬态重置点只同步清理新提示；不执行或改变数据清理业务。
- 85 检查 / 7 变异；原 Native 589/132/44/46/88/184/111、ports 308/3、最终六项 Stage、16 组相关回归、四 DLL 532 元数据通过。存档绑定仅刷新两处 -14 行号，168 个身份不变。
- 简明版：`docs/handoffs/2026-09-11-memory-failure-ui-team-handoff.md`；技术说明：`docs/architecture/af-memory-failure-presentation-boundary.md`；审计：`docs/audits/2026-09-11-memory-failure-ui-verification.md`；台账：`docs/phase8/memory-failure-ui-progress-20260911.md`。
- 检查点 `88777e45`；未推送/部署/真实存档访问。Api.V1 仍只读，真实游戏/旧存档未验收，阶段 8 未完成。
- 下一项仍是记忆数据快照：原身份/owner、可变 blocks/drafts、总览、场景/日期和召回输入；保留原检索/格式，不整段主线程化。已确认召回不写块内 embedding，引擎缓存按原锁保留。本轮不是完整记忆线程安全；自动化继续。

## 前序续作：Native 初始场景准备（生产/测试 0306beba）

- NPC、文化/已有历史标记、挑衅规则、传唤/带路候选与规则排除表改在原 request_target_validation 消费中一次准备；先验证原 admission，删除被替代的后台读取片段。原 helper、参数/结果/顺序与默认业务链保留。
- private 准备包仍有既有 LocationCharacter/Location 引用，不是公共 immutable DTO；没有把持久历史召回/前处理 Task 整段搬主线程。
- 589 检查 / 5 变异；共用调度 132/7、准入 44/7、展示 46、动作 88、收尾 184、前置历史 111、ports 308/3；六项最终 Stage、16 组相关回归、四 DLL 532 元数据通过。原 7 个前置守卫仍为 6 + 1，未弱化门禁。
- 简明版：`docs/handoffs/2026-09-11-native-preparation-team-handoff.md`；技术说明：`docs/architecture/af-native-initial-preparation-boundary.md`；审计：`docs/audits/2026-09-11-native-preparation-verification.md`；台账：`docs/phase8/native-preparation-progress-20260911.md`。
- 检查点 `62468e7c`；未推送/部署/真实存档访问，Api.V1 仍只读。真实游戏/主线程耗时尚未验收，整个阶段 8 未完成。
- 下一项：沿实际持久历史链拆游戏/owner 数据读取、可变记忆集合与召回/选择，不删记忆、不整段主线程化；之后继续 persona/周报绑定、TTS、Courier prepare。自动化继续。

## 前序续作：共用主线程函数（生产/测试 5bf830f3）

- Native/Scene 共用调度改为 queued/claimed/retired CAS：未开始才可过期，已开始等真实结果；失败发布不遗留晚到工作，日志/错误消息格式化不改变结果。direct/queued 前处理格式异常一致，普通 fallback 兼容责任保留。
- 仅两个私有调度声明改变，25 个业务调用点保持原样；移除 bool timeout、wait 吞错和重复执行处理。未改制作组业务、存档键、默认路径或公共写 API。
- 132 检查 / 7 变异；Native 44/46/88/184/111、ports 308/3、六项最终 Stage、16 组相关回归、实际四 DLL 532 元数据通过；不是实机。
- 简明版：`docs/handoffs/2026-09-11-mainthread-function-team-handoff.md`；技术说明：`docs/architecture/af-mainthread-function-boundary.md`；审计：`docs/audits/2026-09-11-mainthread-function-verification.md`；台账：`docs/phase8/mainthread-function-progress-20260911.md`。
- 检查点 `84d7097b`。未推送/部署/真实存档访问；Api.V1 仍只读。普通 fallback 仍不能证明没有部分副作用，已开始的同步 owner 不能强行取消。
- 下一项：Native 更早 prepare（尤其后台 persisted history 与人设/规则构造的游戏读取）、TTS 直接回调，再处理 Courier prepare。现有 whole-host inverse 是本轮严格对照，后续其他 host 变更要补独立审查证据，不弱化断言。自动化继续。

## 前序续作：Native 前置历史（生产/测试 128e9842）

- 玩家显示名、tentative 输入、pending AFEF 与 Native 历史消息改在同一次主线程消费里准备；原 history key 只解析一次，私有 helper 默认行为不变。
- 五个拒绝分支与 action discard 共用固定 key + 原 owner/generation/会话/revision 的清理，改为只删 player/user，不误删事实或新存档重用序号。未开始队列超时明确失败，晚到不补做；原 Action core 未改。
- 111 检查 / 12 变异，原 184/15、88/9、44/7、46/6、ports 308/3，六项 Stage、四 DLL 532 元数据和 16 组相关回归通过；不是实机。
- 简明版：`docs/handoffs/2026-09-11-native-pending-history-team-handoff.md`；技术说明：`docs/architecture/af-native-pending-history-boundary.md`；审计：`docs/audits/2026-09-11-native-pending-history-verification.md`；台账：`docs/phase8/native-pending-history-progress-20260911.md`。
- 检查点 `1547460a`。未推送、未部署、未操作真实存档；公共 Api.V1 仍只读。
- 下一项：更早 Native 人设/规则/持久记忆 prepare，以及通用 main-thread func 的 bool timeout 问题；保持网络在后台，随后继续 TTS 直接回调、Courier prepare。不要把本段完成当成全 Native 或最终阶段 8 DONE。

## 前序续作：Native 记忆接受结果（生产/测试 18f48678）

- Native 已从 void 历史外壳接到一个支持 sceneSessionId 的 internal strict owner，检查运行期接受结果；原 public 六参接口保留 -1 loose 与 ABI。原 Action core、底层 Append/AFEF/数值未改。
- owner 缺失/false/失败不再按正常完成处理，提示记忆未确认，不重放动作或删除部分记录。必要关窗在记忆失败后也保留且仍绑定原会话；非持久 NPC/空 payload 不伪造写入请求。
- 184 检查 / 15 变异，原 88/9、44/7、46/6、ports 308/3，六项 Stage 和实际四 DLL 532 元数据通过。存档契约仅修正两处 -34 的源码行号，168 绑定身份不变，复验 PASS。
- 简明版：`docs/handoffs/2026-09-11-native-memory-acceptance-team-handoff.md`；技术说明沿用更新后的 `docs/architecture/af-native-completion-boundary.md`；审计：`docs/audits/2026-09-11-native-memory-acceptance-verification.md`；台账：`docs/phase8/native-memory-acceptance-progress-20260911.md`。
- 检查点 `c5de2186`。Applied 仅为运行期接受，不是磁盘/SyncData/跨动作事务或恢复 receipt；新 Api.V1 仍只读。未推送、未部署、未实机验收。
- 下一项：更早 Native prepare/失败 pending 清理，再做 TTS 直接回调、Courier prepare 和完整生命周期/恢复证据；不改制作组业务或直接开放新公共提交。

## 前序续作：Native 主线程收尾（生产/测试 d7ab9610）

- 动作后的历史派发、短期记录/显示标记和最终 TTS 改到同一次主线程消费；不再先检查目标再返回后台写游戏状态。原 Action core 未改。
- 动作前捕获 scene session 与非 Hero party memory identity；动作合法结束会话时保留原目标历史派发，临时状态/延迟关窗仍绑定原 context/revision。动作 discard 清理也限定原上下文主线程。
- 新 102 检查 / 9 变异，原动作 88 / 9、准入 44 / 7、展示 46 / 6、ports 308 / 3，最终六项 Stage 和 16 组回归通过；不是实机/旧存档验收。
- 最新简明版：`docs/handoffs/2026-09-11-native-completion-team-handoff.md`；技术说明：`docs/architecture/af-native-completion-boundary.md`；审计：`docs/audits/2026-09-11-native-completion-verification.md`；台账：`docs/phase8/native-completion-progress-20260911.md`。
- 本轮检查点 `e49aabbd`。未推送、未部署，公共 Api.V1 仍只读；旧 ForExternal 兼容入口不等于新公开 SDK。
- 下一项：复用已有 MemoryCommitResult 严格接受边界并保留 Native scene session；随后处理更早 prepare/失败 pending 清理、TTS 直接回调、Courier prepare。旧 void 历史 owner 仍可能吞错/无 owner，不能宣称已实现可靠持久化或完整原子 AFEF receipt。

## 前序续作：Native 动作派发边界（生产/测试 9a5335be）

- 前半批 `8da4fbd7` 修复动作异常被当成功、日志异常让回复提前结束；本次 `9a5335be` 继续补齐未消费动作队列的等待期限。原业务 Core 未改。
- 仅尚未 claim 的动作可在 30 秒后过期，晚到不补做；已开始动作等待真实结果，不按超时伪装取消或自动重试。两个 Overlay 失败分支仍在原展示 scope 内，目前共 16 个受保护异步 UI 消费点。
- 88 检查 / 9 变异、原准入 44 / 7、展示 46 / 6、ports 308 / 3、六项 Stage 构建及 16 组相关回归通过；不是实机验收。
- 简明交接：`docs/handoffs/2026-09-11-native-action-outcome-handoff.md`；技术边界：`docs/architecture/af-native-action-dispatch.md`；最终审计：`docs/audits/2026-09-11-native-action-timeout-verification.md`；台账：`docs/phase8/native-action-outcome-progress-20260911.md`。
- 前半批审计保留在 `docs/audits/2026-09-11-native-action-outcome-verification.md`；检查点分别为 `861dd7a7`、`841e8751`。
- 未推送、未部署、公共 API 仍只读。下一项：Native 成功路径的主线程事实/记忆收尾（须区分旧会话晚返回和 owner 合法结束会话），然后更早 prepare/TTS、Courier prepare。不要把本次派发取消当整个回合回滚。

## 前序续作：Native 展示观察（生产/测试 32230a64）

- 两个 Overlay 提交入口复用同一内部观察桥和完整旧 Native 流程；14 个 UI 异步消费位置在出队时核对捕获会话，而不是只看当前 NPC 可用。
- 后端已释放时，合法最终结果仍能显示；换会话/读档/新 revision 后旧结果失效，并只释放本地旧 busy，不操作新显示。
- 新 46 检查 / 6 变异、原准入 44 / 7、六项构建和相关回归通过。公共 V1 仍只读；没有推送或部署，实机未验收。
- 最新短版：`docs/handoffs/2026-09-11-native-presentation-handoff.md`；技术边界：`docs/architecture/af-native-presentation-lifetime.md`；验证：`docs/audits/2026-09-11-native-presentation-verification.md`。
- 前序准入生产 `77d4a940`，记录保留在 `docs/phase8/native-admission-progress-20260911.md`；本轮台账为 `docs/phase8/native-presentation-progress-20260911.md`。
- 下一轮：继续 Native prepare/动作后事实回执及剩余 TTS 直接回调边界，然后处理 Courier 双向 prepare；不能把 Overlay 观察票据当成完整公共请求服务。

## 1. 当前结论

**已进入确认架构的初版实施；本轮新接口不是整个阶段 8 或完整 SDK 的最终完成。**

```text
AnimusForge.dll
├─ AF 主体：对话、LLM、Prompt、标签、记忆、调度
├─ internal 模块接口与薄桥 → 政策 / 宴会 / GCCZ
└─ public Api.V1（首版只读） ← 独立子 MOD DLL
```

当前工作树：`G:\AFMOD\AF-REFACTOR`。
分支：`codex/af-main-refactor-continuation-20260831`。
初版实施前：`df6ab928`；本轮意图/回滚检查点：`6e0de826`。
框架初版生产与测试提交：`a616958c`；最新生产见上方续作段。
精确最终提交请运行 `git log -3 --oneline`；本文与本轮源码一起提交，不编造包含自身的未来 commit hash。

给制作组直接看的最新短版见上方；框架初版说明保留在 `docs/handoffs/2026-09-11-framework-v1-team-handoff.md`。

## 2. 框架初版真实变更（a616958c）

- `Refactor/Modules/InternalModuleDirectory.cs`：内部定义、依赖/版本校验、冻结与只读目录。未初始化不报告可用，冲突不覆盖 provider。
- `TeamModulePorts.cs / TeamModuleAdapters.cs / TeamModuleServices.cs`：3 组 internal 接口、13 个原样转接方法、单例薄桥。没有改额外模块业务实现。
- `ShoutBehavior.cs / ShoutBehavior.ScenePostprocess.cs / MyBehavior.cs / CourierDeliveryBehavior.cs`：共 31 处 receiver 接入；既有默认流程、参数和权威提交顺序保留。
- `ModuleFrameworkRuntime.cs / SubModule.cs`：加载时显式装配，卸载时发布停止状态；不是 Campaign/game ready 事件，也没有新增 Tick。
- `Api/V1/AfApi.cs / AfApiContracts.cs`：稳定英文 ID、V1 能力查询、框架只读快照。内部能力 `IsExternallyCallable=false`。
- 新增契约/外部编译/薄桥回归，更新受真实receiver迁移影响的旧测试接线。
- 存档对照表只刷新因新增 using 引起的源码行号；168 个 key/ref/type/source 身份保持不变。

### 不要夸大

- 当前只登记 `af.team.policy/gathering/siege` 的选定 `dialogue` 接缝，不是所有模块功能完成迁移。
- 目录的可用状态不是执行授权，也不拦截全部历史 ForExternal 调用；每个真实请求仍由原 owner 检查。
- 新公共 API 只有 `CatalogRead`；Native/Scene/Courier 提交、动作、记忆和扩展注册明确 `NotSupported`。
- 旧 `NpcRulerPolicyBehavior.BuildActivePolicyDialogueContextForExternal` 目前本就返回空上下文；新桥没有恢复/更改业务。

## 3. 文档导航

- 总体图及责任：`docs/architecture/af-framework-v1-overview.md`
- 制作组接入步骤：`docs/architecture/af-internal-module-guide-v1.md`
- 子 MOD 使用示例/兼容边界：`docs/architecture/af-public-api-guide-v1.md`
- 本轮实施与验证台账：`docs/phase8/framework-v1-execution-20260911.md`
- 原逐项清单：`docs/phase8/af-core-review-checklist-20260910.md`（历史审批快照；用户随后已授权本轮初版）
- 上一生产修复：`docs/handoffs/2026-09-09-recovery-fixes-handoff.md`
- Courier 深层线程缺口：`docs/audits/2026-09-09-courier-thread-boundary-plan.md`

## 4. 框架初版验证（最新 Native 验证见上方）

已完成：Debug/Release × 1.3/1.4/Bootstrap 六项构建全部通过；新目录 44、公共 API 119、薄桥 308 个断言通过，四份实际实现 DLL 的 472 个元数据断言通过；原 Scene/Courier/管线与所选生产回放通过。原始命令/日志在 `.tmp/framework-v1-20260911/`，可提交的摘要在 `docs/audits/2026-09-11-framework-v1-verification.md`。

**离线回归/构建不能替代实机验收。** 前次制作组对旧候选的测试反馈，不会自动成为本轮新接口的 LIVE/SAVE 证据。

## 5. 后续工作（待用户恢复后，按顺序，不重写额外模块业务）

1. Native：准入、排队 epoch、共享后端 busy、Overlay 队列观察与动作派发失败/未开始超时和主线程收尾边界已落地；单次运行期记忆接受结果也已接入；前置历史与五个拒绝清理也已收敛；共用主线程函数的等待/诊断边界也已修复；初始场景准备与本轮 Native 持久历史输入也已捕获；继续更深 prepare、完整请求/恢复证据及 TTS 引擎直接回调边界，再评估有限公共普通文本提交。
2. Courier 双向更早的 prepare：拆开游戏读取、网络/人设/记忆准备和主线程完成，避免把整段含网络的 builder 搬主线程。
3. 保持 Scene 主体的接力、旁听、后处理、记忆/AFEF 和 TTS 回归；新接缝必须有原功能对照。
4. 在稳定请求与事实回执上再扩充公共结果/生命周期通知、内部贡献协议、经过批准的制作组能力转接或子 MOD 扩展。
5. 真实 1.3/1.4 Host、旧存档、新外部 DLL 加载/升级验收完成后，再单独确认默认迁移及有证据的旧路径删除。

不要把旧 MOOD fallback 差异、同步 Action 网络不能真正取消、Courier 前置线程缺口写成“本轮已修”。

## 6. 构建 / 回滚 / 协作边界

使用既有 `一键编译覆盖推送/build_single_module.ps1 -Stage`，一套源码构建 1.3、1.4、Bootstrap；不改脚本、不拆 Contracts DLL、不改程序集/存档身份。准确本机构建参数保存在验证日志及实施台账中。

当前自动化 `af-7-8` 已按用户最新要求暂停（PAUSED），需用户明确指示后才恢复。暂停前仅本地推进、验证和提交；没有推送、部署、安装 SDK 或操作真实存档。原两份 2026-09-06 用户草稿改动保留，未 stage 进本轮提交。

回滚采用本轮实现提交的定向 `git revert <commit>` 并保留用户改动，不 hard reset，不 force-push。`6e0de826` 是框架初版检查点；最新两批检查点见顶部，需回滚时定向反转对应实现提交并保留用户改动。

不要推原共享 `refactor/prepare-af-restructure` 或恢复其已改写历史；远端交付要使用经用户确认的专门重构分支。最新 fetch 时同名远端为 `a58c2191`，本地已有源码/测试/文档领先；新修改尚未推送。
