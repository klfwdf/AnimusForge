# 外交重构 handoff — DPL-070（2026-09-26）

当前状态：**DPL-070_ACTIVE / DPL-070A_OFFLINE_VERIFIED**。已进入 070 并完成首个传播到达切片，尚未宣称整个 070 完成。用户明确要求只维护本文件；根 HANDOFF、主体台账、范围图和代码地图未增加本次状态。

- 工作树 `.wt/diplomacy-latest-20260925`，分支 `codex/diplomacy-refactor-20260925`。060 单一交接已补交为 `b633eacb`；070A 意图 `a05d8c5f`，产品/回放 **`bd360c6b822a169c01940cb3f94107016106dd99`**。先前 Git 权限阻塞已解除。
- 本片完成“每日到达队列 → civilian/noble/kingdom 知识 → 宫廷回执”。`src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPropagationApplication.cs` 的 `ProcessDue` 与 `ReceiveCourt` 持有真实队列和知识转换；`WorldDiplomacyBehavior.ProcessPropagationArrivals` 已接入，旧算法已从 host 删除。host 仍只在同步主线程解析 live Kingdom/Settlement、执行宫廷回执；application 不持有游戏对象或后台任务。
- 顺序不变：读取当日 → 截取最多 1200 条到期前缀 → 整批出队 → 按原顺序重新查文书 → 目标 kingdom 优先、缺失则 settlement 当前 owner 兜底 → 写贵族知识 → 写王国知识 → 首次知情或玩家缺失正式回执时执行原 ProcessCourtArrival。民间传播只写 settlement 知识。空文书/失效目标、null/future 队首、异常中断、重入入队、处理期间文书替换/移除均保留原行为。不会把传闻当正式送达。
- `OnDailyTick`、`ProcessCourtArrival`、`TryScheduleMandatoryCourtResponse` 与 `260ba51e` 逐段内容相同；原 active round、author/representative、mandatory response 和玩家权限门禁保持。保存 record、字段、v1 key、程序集和一键脚本未改，也没有第二份可写 storage。
- 性能：每天既有调度点执行一次。通过索引检查已排序 List 的到期前缀，最多 1200 项，空闲不分配 due 列表；10000 条未来队尾场景不执行未来文书或目标解析。沿用原 List.RemoveRange 的 O(队尾数量) 移位、每条文书查询和知识规则成本，不声称整次回调 O(1200) 或实机帧性能已验。接缝闭包仅同步调用，不缓存 live 对象；为保留处理中替换/撤销的语义，未引入可能过期的批次文书缓存。

## 本片验证

- `tools/WorldDiplomacyRoundLifecycle.SmokeTests`：**2569 assertions**；比 060 新增 99 项，覆盖 38 组新旧差分场景，以及重复日调用、真实 JSON roundtrip 后不重复回执、生产适配器逐字匹配、领域边界。比对完整 serialized state、resolver/effect trace 与异常；world 查询和最终 court effect 是显式 fake，知识规则/records/application 为真实源码。
- 原控制方法保存于 `PropagationHostControls.cs`，逐字来自 `260ba51e` 的 `ProcessPropagationArrivals`，LF/UTF-8 SHA256 `256f812c0eacb08dad527d88ae73917dc687054b68d9305d491c9486a9a2d453`。当前适配器副本同样由源码匹配断言绑定；没有通过弱化旧 player receipt 规则让测试通过。
- `tools/WorldDiplomacyIntentBoundary.SmokeTests`：**1269 assertions**，包含真实 host → application → 原回执绑定。测试套件仍有既存 net6 EOL/nullable 等警告，未宣称测试零 warning。
- 原脚本无 Stage/Deploy 的 **Debug 1.3、1.4、Bootstrap 三构建通过，各 0 warning/0 error**。本片没重跑 Release/全 Phase8/四 DLL metadata，060 历史结果不冒充当前产物验收。
- Debug 1.3：引用 `v1.3.15.110062`，SHA256 `3CC442ECBF71B5A8E136468D62822610637462B525D6E5B9ADCE77048D26AA5B`。
- Debug 1.4：引用 `v1.4.6.115628`，SHA256 `4AA85BEECC9185E8308758D9742F710963085A53E94BD1A560453E095C010366`。
- 命令：`dotnet run --project tools/WorldDiplomacyRoundLifecycle.SmokeTests -c Release --verbosity quiet -p:NuGetAudit=false`；IntentBoundary 同参数。构建参数沿用下方 060 的原 Debug 命令。本机详细日志 `_codex_tmp/dpl070/lifecycle.log`、`intent.log`、`build-debug.log`（忽略目录）。`git diff --check` 通过。
- **NOT-RUN**：真实两版本游戏、旧 SAVE、MCM/UI、provider、实际 court 游戏副作用及帧耗时；没有 push、Stage、部署、打包或游戏/外仓写入。

## 下一切片

先梳理 `StartDocumentPropagation` 的文书发布、origin/author 知识、民间/宫廷路径快照、到达队列替换，以及 `RecalculatePendingPropagationIfNeeded` 的设置变化重算。按稳定 ID/距离值分离主线程地图读取与领域排程，保持已知知识跳过、玩家正式回执补发、按日排序和原 due-day 公式，再做旧实现对照回放。

之后继续文书/chain/version、提案/义务、响应/过期、cooldown/threat/pressure、settlement/jobs 的实际归属和到期工作；070A 仅覆盖到达消费与正式知识写入，不等于上述范围全部完成。不要把未调用的 `SynchronizeCourtKnowledge` helper 当作已迁移真实入口。DPL-080 尚未开始。

## DPL-060 历史交接（下文数字和产物身份仅对应当时提交）

# 外交重构 handoff — DPL-060（2026-09-26）

状态：**DPL-060_OFFLINE_VERIFIED**。DPL-070 尚未开始。按用户要求，外交状态、证据、限制和下一步集中在本文件，取消本次其他四个文档的重复更新。

- 工作树：`.wt/diplomacy-latest-20260925`；分支：`codex/diplomacy-refactor-20260925`。保留其他工作树与主体 J14 历史。
- 基线：`fb4af2d81418febe7696963e2766801b683ad3ce`；整合意图 `6022e5f9`、整合 `98f7ac76`、收尾意图 `348f7a35`；最终产品/回放 **`260ba51e69ed1c27a32e6887a791ac6217572f5d`**。
- 本文件为唯一外交交接；先前审批服务故障后，本次已恢复本地 Git 写入并补交文档，没有撤回产品代码。
- 下一步：完成这个 handoff 的本地提交后，按 DPL-070 从文书创建、chain/version、发布、传播、knowledge/jobs 调用链选择完整行为切片；保持玩家优先、正式知情与传闻区别、来源版本绑定及晚结果拒绝。

### 真实责任及保留语义

- `WorldDiplomacyRoundApplication` 已接真实 Campaign host，拥有普通候选轮换、开局、relay advance、玩家声明加入、mandatory admission、关闭；`WorldDiplomacyPolicyRoundApplication` 拥有政策信号 attach/defer/reject/open。原 host 中对应算法已删除，端口提供主线程 live 解析、时钟、预算、生成入队、冷却/摘要/日志。application 只传稳定 ID、值和 canonical records，不保持 live Kingdom/Hero，也没有第二份 storage。关键入口见本文件末尾，按产品提交与实际源码定位。
- suspension/resume、load reconcile、延迟工作、生命周期与结果 settlement 继续由已接线的 `WorldDiplomacyRoundLifecycleRules` / `WorldDiplomacyResultSettlementRules` 执行。保留 `rotation → budget → open → enqueue`，关闭时 `closed/status → offers expire → cooldown → published docs/threat settlement → round queues/opportunities → archive/active=null → next day → summary → release contract → log/compress`。原关闭路径**不删除 storage.Jobs**，本次同样保留，不把遗留任务责任误交给 Close。
- DK 生成/完成组合接回当前主体 immutable request、request lease、generation、晚结果丢弃与玩家宣战 guard。job selection 保留空 JobId 过滤与 cache-affinity Trim；completion 用局部 Trim + OrdinalIgnoreCase 路由，不修改 persisted Kind。七类口头外交通过同一 direct dispatch、command facade 与 Bannerlord game-action port，现有跨域 owner 保留。
- 四实际 DLL 的 metadata 检查发现整合来源的十个 facade 暴露为 public；本次恢复 **assembly-internal**（七动作、document-read、timeline query/revision），原 metadata 门禁未削弱。保存 public records、命名空间、字段及程序集身份未改，未新增 Api.V1 capability。

### 验收证据

| 门禁 | 当前结果与边界 |
| --- | --- |
| 外交 smoke | 20/20 套通过；RoundLifecycle 2470、IntentBoundary 1268、ResultSettlement 499、Compression 305、PolicyHistory 102、Timeline 90、StorageShape 70、PersistenceAdapter 15，其余动作/资格/迁移套件全部通过。测试项目仍有既存 net6 EOL/nullable 警告。 |
| 新 application 回放 | normal/cancelled/reloaded/delayed/player-inserted/no-result/terminal；JSON roundtrip 后不重复关闭；orphan recovery；mandatory 枚举分支；政策 open/attach/wait/reject；suspend/reload/resume；候选 4096 项最多一圈/首个合格即停。真实 host 调用边界及旧算法移除有断言。 |
| J12 | domain lifecycle 68、owner source 3 通过；请求租约/同 JobId 新 generation 安全边界保留。 |
| 三渠道相关 | ChannelCutover 133、Scene deferred queue 37 通过；使用现存 net6 Newtonsoft.Json 供 net8 harness 运行。game-domain helpers 明确 stub，live/provider NOT-RUN。 |
| 保存 | audit contract 6、chunk replay 8 场景通过；对主体基线 142 SyncData/36 CampaignBehavior 无增删；模块 AnimusForge，唯一加载 AnimusForge.Bootstrap.dll。`_af_world_diplomacy_v1` 与同一可写 owner 保持。 |
| 构建 | 原 build_single_module.ps1 无 Stage/Deploy：Debug/Release × 1.3/1.4 + Bootstrap，六构建均 0 warning/0 error。引用基线 v1.3.15.110062、v1.4.6.115628。 |
| API / 实际 DLL | V1 142、snapshot 36、并发读取、5 个故障变异及外部 internal 访问预期 CS0122 通过；四 DLL metadata **1296** 通过。 |
| Phase8 | 当前 Debug 1.4 DLL `79C96D71574B66FA61A897788A0CCF556A7ABDFE7572C700783EACED6BF9C8C3` 完整 PhaseEightParityReplay 通过。它补充当前主体集成证据，不替代本轮 source-linked 外交状态机回放。 |

最终实现产物 SHA256（仓内 single_module_artifacts，未 Stage）：

| 配置 | BannerlordApi | SHA256 |
| --- | --- | --- |
| Debug | 1.3 | `4D32C8E783C800ED3431266536A92F7C7B8961E966D9A790636834FA4E9374F0` |
| Debug | 1.4 | `79C96D71574B66FA61A897788A0CCF556A7ABDFE7572C700783EACED6BF9C8C3` |
| Release | 1.3 | `28A91B3C7E36EF1B1ED2B2BFEF3969248BCFD2D85C038FE6852A378529FE40CA` |
| Release | 1.4 | `B4717336D30CB1E6FD908B13CFF3F431030B174391EC638E3A917318FADCF75D` |

### 可复核命令与限制

在当前外交工作树执行，保持原构建流程：

```powershell
# 所有 WorldDiplomacy smoke 项目逐一 dotnet run；Gateway 不在 smoke 批次。
dotnet run --project tools/WorldDiplomacyRoundLifecycle.SmokeTests -c Release --verbosity quiet -p:NuGetAudit=false
dotnet run --project tools/J12DomainLifecycleRegressionTests -c Release --verbosity quiet
python -B -m unittest discover -s tools/J12DomainOwnerSourceContractTests
python -B -m unittest discover -s tools/PersistenceIdentityAuditContractTests
dotnet run --project tools/PersistenceChunkReplayTests -c Release --verbosity quiet -p:NuGetAudit=false
python -B tools/PersistenceIdentityAudit.py --baseline fb4af2d81418febe7696963e2766801b683ad3ce --json --quiet
python -B tools/ChannelCutoverBoundaryTests/run.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --output-name dpl060-final --newtonsoft tools/WorldDiplomacyRoundLifecycle.SmokeTests/bin/Release/net6.0/Newtonsoft.Json.dll
python -B tools/ScenePostprocessParityTests/run_queue.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --output-name dpl060-final
python -B tools/ModuleFrameworkApiTests/run.py --dotnet 'C:/Program Files/dotnet/dotnet.exe' --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts
```

本机原构建命令为 `一键编译覆盖推送/build_single_module.ps1 -ProjectRoot (Get-Location).Path -BannerlordRoot 'E:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord' -Bannerlord13ReferenceDir (Join-Path (Get-Location).Path '_deps_auto') -WorkshopContentDir 'E:/SteamLibrary/steamapps/workshop/content/261550' -Configuration Debug`，Release 同参数。Phase8 显式传 `ReplayCandidateDll` 与同一 DLL 的 SHA256，游戏参考/private deps 只读；完整本机命令和日志位于忽略目录 `_codex_tmp/dpl060/`，本条保留耐久结果和产物身份。

- **旧 runner 限制**：`J09DefaultChannelActionWiringTests/run.py` 仍抽取旧 `Task<int> QueueDeferredScenePostprocessActions`，当前生产已为 `Task<ScenePostprocessOutcome>`，本轮未改该生产文件；此旧检查**失败/过期**，不算通过，也不改源码凑断言。当前实际抽取方法由 Scene queue 37 与 ChannelCutover 133 覆盖相关边界。
- `WorldDiplomacyGatewayReplayTests` 仅认 `single_module_stage`，本任务没有 Stage，因此 **NOT-RUN**；没有为消除限制伪造 stage 或改默认脚本。请求租约/完成逻辑有上列专项证据，但真实 gateway/provider/游戏仍未验。
- **性能频率**：普通选择只在原 daily/due 门后构造候选；一次 GetEligibleAiKingdoms 与局部 ID→Kingdom 字典，最多一圈，首个合格即停。政策每个原调度 pass 处理首个信号。关闭/归档只在轮次结束；保留原文书列表扫描排序与摘要成本，不宣称 O(1)。新增 application 无 Task.Run、锁、常驻轮询或跨帧 live object 缓存。真实帧耗时 NOT-RUN。
- **遗留范围**：WorldDiplomacyBehavior 仍是混合 Campaign/live/prompt/document/propagation/query 适配；DPL-070 起逐依赖处理文书/提案/传播/knowledge/jobs，不把大类仍存在当作 060 失败，也不称整个外交已完成。`ProcessPlayerMandatoryResponseTimeout` 旧 helper 检索未见调用，本片未删除、未改玩法。
- LIVE、真实旧存档加载/存回、MCM、UI/通知实机效果、provider、Bootstrap 真实加载与两版本游戏均 **NOT-RUN**。无 push、部署、Stage、打包、游戏/存档/外仓写入。回滚使用聚焦 inverse commit，不重写历史；下一阶段 **DPL-070 尚未开始**。

### 关键代码入口

以下坐标对应产品 `260ba51e`，仅用于定位。真实 host 位于 `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs`，通过 TryScheduleNormalRound、EnsureActiveRound、TrySchedulePolicyTriggeredRound、AdvanceRelay、IntegratePlayerDeclaration、TryScheduleMandatoryCourtResponse 和 CloseActiveRound 接入。

| 锚 | 一基坐标 | 符号和责任 |
| --- | --- | --- |
| `diplomacy.application.round` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:12` | `internal static class WorldDiplomacyRoundApplication` — Canonical round state transitions; synchronous stable-ID/value inputs, no live game object retention. |
| `diplomacy.application.close` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:14` | `internal static void Close(` — Expire offers, cooldown, threats, queues, archive, summary and release in original order; jobs remain owned by their runtime. |
| `diplomacy.application.open` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:59` | `internal static WorldDiplomacyRound EnsureOpen(WorldDiplomacyStorage storage, Func<RoundOpening> resolveOpening)` — Lazy reuse or create one canonical round with preserved serialized deadlines and roles. |
| `diplomacy.application.selection` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:87` | `internal static void TryScheduleNormal(` — Due gate, bounded first-eligible rotation, budget, open and generation admission. |
| `diplomacy.application.relay` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:141` | `internal static void AdvanceRelay(` — Preserve active/settlement/hard-end/next-relay transitions. |
| `diplomacy.application.player` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:198` | `internal static void IntegratePlayerDeclaration(` — Preserve player opportunity and representative route admission; settlement route remains frozen. |
| `diplomacy.application.mandatory` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs:234` | `internal static bool AdmitMandatoryReply(` — Bind required source only after exact mandatory reply admission. |
| `diplomacy.application.policy` | `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPolicyRoundApplication.cs:9` | `internal static class WorldDiplomacyPolicyRoundApplication` — First pending signal per existing daily pass: attach, defer, reject or open through existing ports. |
| `diplomacy.domain.lifecycle` | `Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs:6380` | `public static void ProcessRoundLifecycle(` — Existing cadence, delayed work, settlement and terminal decisions; not a claim that all document/job policy is extracted. |
| `diplomacy.domain.suspend` | `Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs:4636` | `public static bool SuspendActiveExchangeForPlayerInsertion(` — Suspend original exchange without a second store. |
| `diplomacy.domain.resume` | `Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs:4652` | `public static WorldDiplomacyExchange RestoreSuspendedExchangeIfAny(` — Restore persisted exchange and original pause-day shift. |
| `diplomacy.domain.job-select` | `Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs:7391` | `public static WorldDiplomacyJob SelectAndPrepareLlmJob(` — Preserves current selector empty-ID/cache-affinity semantics; real host owns request lease. |
| `diplomacy.domain.job-complete` | `Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs:7525` | `public static void CommitCompletedLlmJobResult(` — Preserves trimmed case-insensitive route classification without rewriting persisted Kind. |
| `diplomacy.persistence.adapter` | `Refactor/Adapters/BannerlordWorldDiplomacyPersistenceAdapter.cs:7` | `internal sealed class BannerlordWorldDiplomacyPersistenceAdapter` — SyncData delegates v1 chunk/JSON storage; same canonical key and serialized types. |
| `diplomacy.persistence.storage` | `Refactor/Persistence/WorldDiplomacyStorageRecord.cs:7` | `public sealed class WorldDiplomacyStorage` — Canonical public serialized identity remains AnimusForge.WorldDiplomacyStorage in AnimusForge. |
