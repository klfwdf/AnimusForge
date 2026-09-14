# AF 框架代码范围图

本图是当前已验证源码 `86805518` 的定位快照，与 GitHub 原重构分支基线 `3f00fefa` 区分。不是完整功能完成清单，也不把未列到的代码当成可删垃圾。实际行号/符号和逐文件摘要见同目录 `af-framework-code-map.json`；主体调整后更新当前图，而不是把路径或方法名永久锁死。

当前已包含独立素材索引、Campaign共享预算、稳定队列排序和完整raw摘要写入组件；封存Daily/Major尾部实际消费排序组件。旧索引partial/嵌套预算类及封存原子排序路径已替换；仍被同步调用的Sanitize保留，不误删。主体家族本次为可靠性增加净化边界与状态，不能用这一步宣称大类整体已拆薄。B1深来源/原子净化与真实验收未完成。

## 新旧责任分区（不搬动运行代码）

| 分区 | 当前含义 |
|---|---|
| `Refactor/Modules/` | 新 internal 契约/目录/薄桥，只有选定接缝已接线，非整个制作组业务迁移 |
| `Api/V1/` | 新公开只读接口；未来按需求和兼容证据扩展 |
| 本次具名 Native / Memory partial 边界 | 已接线的主体局部边界，不代表整个 Native / Memory 完成 |
| `ShoutBehavior.cs` / `MyBehavior.cs` / `CourierDeliveryBehavior.cs` / `SubModule.cs` | 新旧混合 owner，按符号标界，不能整文件打 DONE |
| 原 Scene/Courier 默认历史、Native persona/周报/剩余 TTS、压缩记忆 record/time 预算与其他尚未验证的维护 writer | 保留运行责任，未完成部分仍需接续 |
| 政策 / 宴会 / `AnimusForge.SiegeAftermathIntervention` 业务 | 本轮不重写，只处理 AF 侧接口，不误删旧业务 |
| GitHub 主分支及其他旧版本 | 分支/固定基线隔离，不复制到活动编译目录，不整片覆盖当前重构分支 |

## 已核实代码坐标

以下一基行号均属于源码 `9158132c`，仅为导航，不代表整个方法的改动量。用符号和固定提交重新定位。

| 边界 | 源码位置 | 符号 / 责任 | 状态 |
|---|---|---|---|
| `internal.port.policy` | `Refactor/Modules/TeamModulePorts.cs:7-10` | `internal interface IPolicyModulePort` — 政策 typed 接缝，业务归原 owner | `wired-boundary` |
| `internal.port.gathering` | `Refactor/Modules/TeamModulePorts.cs:17-20` | `internal interface IGatheringModulePort` — 宴会 typed 接缝，不迁移玩法 | `wired-boundary` |
| `internal.port.siege` | `Refactor/Modules/TeamModulePorts.cs:27-30` | `internal interface ISiegeModulePort` — GCCZ 接缝，保留原场景门禁 | `wired-boundary` |
| `internal.adapters` | `Refactor/Modules/TeamModuleAdapters.cs:7-10` | `internal sealed class PolicyModuleAdapter : IPolicyModulePort` — 同文件三组薄桥原样转接参数/返回/ref/out | `wired-boundary` |
| `internal.services` | `Refactor/Modules/TeamModuleServices.cs:5-8` | `internal static class TeamModuleServices` — 无状态 typed 单例装配 | `wired-boundary` |
| `internal.directory` | `Refactor/Modules/InternalModuleDirectory.cs:137-140` | `internal sealed class InternalModuleDirectory` — 注册/冻结/依赖校验，非游戏执行授权 | `wired-boundary` |
| `internal.runtime` | `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` | `internal static class ModuleFrameworkRuntime` — 装配与只读投影，不是第二套执行器 | `wired-boundary` |
| `public.api` | `Api/V1/AfApi.cs:13-16` | `public static class AfApi` — 当前只读；其他提交/写能力未开放 | `readonly-api` |
| `public.ids` | `Api/V1/AfApiContracts.cs:35-38` | `public static class AfCapabilityIds` — 公开 ID 和同文件 DTO，可按兼容版本演进 | `readonly-api` |
| `lifecycle.load` | `SubModule.cs:60-63` | `ModuleFrameworkRuntime.Initialize(out string moduleFrameworkReason);` — 模块装配，不是 Campaign Ready | `mixed-host` |
| `lifecycle.unload` | `SubModule.cs:110-113` | `ModuleFrameworkRuntime.Shutdown();` — 发布停止状态 | `mixed-host` |
| `native.admission` | `ShoutBehavior.NativeAdmission.cs:17-20` | `internal sealed class NativeConversationAdmission` — 准入/忙碌/会话绑定，不是完整公共提交服务 | `wired-boundary` |
| `native.preparation` | `ShoutBehavior.NativePreparation.cs:14-17` | `private sealed class NativeConversationPreparationSnapshot` — 仍含原 Location 引用，不是公共不可变 DTO | `wired-boundary` |
| `native.history.capture` | `MyBehavior.HistoryPromptSnapshot.cs:35-38` | `internal static Func<string> CaptureHistoryContextWorkById` — 召回用途投影，非全局记忆事务 | `wired-boundary` |
| `native.history.bridge` | `ShoutBehavior.cs:16180-16183` | `private static Func<string> CaptureNativeConversationPersistedHistoryWork` — 原身份解析在主线程捕获，非全 Shout 重写 | `mixed-host` |
| `native.history.dispatch` | `ShoutBehavior.cs:20159-20162` | `"persisted_history_capture", nativeTargetLog, nativeTargetAgentIndex,` — 原 admission 验证后捕获 work | `mixed-host` |
| `native.history.accept` | `ShoutBehavior.cs:20201-20204` | `if (!await RunNativeConversationMainThreadFuncAsync("persisted_history_accept"` — 使用结果前再验原 admission | `mixed-host` |
| `native.dispatch.shared` | `ShoutBehavior.cs:19437-19440` | `private Task<T> RunNativeConversationMainThreadFuncAsync<T>` — 排队/开始/退休，不假称网络已取消 | `mixed-host` |
| `memory.acceptance` | `MyBehavior.DialogueHistoryCommit.cs:12-15` | `internal static MemoryCommitResult CommitDialogueHistoryWithScene` — 运行期接受，不是磁盘/跨动作事务 | `wired-boundary` |
| `memory.failure.ui` | `MyBehavior.MemoryFailureNotice.cs:53-56` | `private void ProcessPendingMemoryFailureNotice()` — 原 EngineTick 消费；owner/Campaign/generation/revision | `wired-boundary` |
| `memory.summary.dispatch` | `MyBehavior.MemorySummaryMainThread.cs:58-61` | `private Task<bool> RunMemorySummaryMainThreadAsync(long generation, Func<bool> operation)` — 主线程剩余额度内直达，否则排队；原 owner/generation/Campaign 与退休门禁保留 | `wired-boundary` |
| `memory.summary.drain` | `MyBehavior.MemorySummaryMainThread.cs:128-131` | `private void ProcessMemorySummaryMainThreadActions()` — inline/queued 共用两次操作和实际累计耗时；超预算不启动下一操作，单原子与record预算仍未完整 | `wired-boundary` |
| `memory.summary.accept` | `MyBehavior.cs:4961-4964` | `if (ApplyMemorySummarySuccess(result.Job, result.Block)) appliedDaily++;` — 源重验紧接真实Apply；部分/未知错误通知，不盲重放或假报成功 | `mixed-host` |
| `legacy.history` | `MyBehavior.cs:27835-27838` | `public static string BuildHistoryContextForExternal(` — Scene/Courier 仍调用，共享兼容入口不能盲删 | `retained-live` |
| `legacy.recall` | `MyBehavior.cs:33841-33844` | `private string BuildCompressedMemoryContextById(` — snapshot 和默认旧调用共用原算法 | `mixed-host` |
| `scene.postprocess` | `ShoutBehavior.ScenePostprocess.cs:25-28` | `private sealed class SceneActionPostprocessWorkItem` — 已有完整后处理；非本次重写 Scene 业务 | `mixed-host` |
| `courier.prepare.reply` | `CourierDeliveryBehavior.cs:4674-4677` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(recipient,` — 回信早期准备待线程审查，未迁快照 | `retained-live` |
| `courier.prepare.inbound` | `CourierDeliveryBehavior.cs:5102-5105` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(sender,` — 来信早期准备待线程审查，未迁快照 | `retained-live` |
| `memory.summary.capture` | `MyBehavior.MemorySummaryInput.cs:252-255` | `private MemorySummaryInput CaptureMemorySummaryInput(` — 三类唯一初捕获/复制/原Build，raw与effective context分离；首次绑定检查，深记录原子成本仍未收口 | `mixed-host` |
| `memory.summary.source-check` | `MyBehavior.MemorySummaryInput.cs:338-341` | `private bool IsMemorySummaryInputCurrent(` — 不重新Capture/Clone/Build；context后fresh raw摘要与动态资格，retarget先拒绝，不靠Save-only epoch | `mixed-host` |
| `memory.summary.execute` | `MyBehavior.MemorySummaryInput.cs:357-360` | `private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(` — 共用原 provider/Build/Parse；主线程解析、波次/重试退休，完成后释放大 payload | `mixed-host` |
| `memory.source.facades` | `MyBehavior.MemorySourceWrites.cs:22-25` | `private static bool DeferMemorySourceWriteIfNeeded(` — 旧 void façade 主线程同步、后台 owner/generation 排队；不是持久接受回执 | `mixed-host` |
| `memory.summary.rendering` | `PlayerNotorietyBehavior.cs:203-206` | `internal static string CaptureMemorySummaryHistoryRenderingIdentity()` — 无 observer 公称/实际匿名别名纳入 daily 解析来源身份 | `mixed-host` |
| `memory.summary.copy` | `MyBehavior.MemorySummaryInput.cs:46-49` | `private static T CloneMemorySummarySource<T>(T value)` — 10种模型和2种列表的typed复制；各CopyForSummary分离可变图 | `mixed-host` |
| `memory.summary.fingerprint` | `MyBehavior.MemorySummaryInput.cs:323-326` | `private static string ComputeMemorySummaryFingerprint(object identity)` — raw和有效context各自流式SHA256；不再把完整Prompt反复纳入来源digest | `mixed-host` |
| `memory.summary.time` | `MyBehavior.MemorySummaryMainThread.cs:20-23` | `private bool HasMemorySummaryMainThreadAllowance()` — 复用既有维护毫秒配置，实际Stopwatch累计；非同步抢占 | `mixed-host` |
| `memory.summary.partial` | `MyBehavior.MemorySummaryMainThread.cs:88-91` | `private async Task<bool> RunMemorySummaryCompletionAsync(long generation, Func<bool> operation)` — 仅协调器传播operation异常，区分拒绝/取消与部分执行失败 | `mixed-host` |
| `memory.summary.admission` | `MyBehavior.cs:4859-4862` | `private void TryStartMemorySummaryQueue(bool forceOverviewCandidateScan = false)` — raw数量入场；候选ID扫描仍有同步全扫，不代表全部入口已预算 | `mixed-host` |
| `memory.summary.planner` | `MyBehavior.cs:4921-4924` | `private async Task ProcessMemorySummaryQueueAsync(bool forceOverviewCandidateScan = false)` — 分段初筛/extra/cleanup，冻结metadata排序与失败汇总在worker；仅完整接受才计数 | `mixed-host` |
| `memory.summary.maintenance` | `MyBehavior.cs:17759-17762` | `private void TryRunCampaignMemoryMaintenance()` — 不在每Tick重复读完整summary来源；past draft检查仍有全扫 | `mixed-host` |
| `memory.plan.entry` | `MyBehavior.MemorySummaryPlanning.cs:13-16` | `private sealed class MemorySummaryPlanEntry` — 冻结job标记/排序键，Job只作opaque原引用，非新持久owner | `mixed-host` |
| `memory.plan.scan` | `MyBehavior.MemorySummaryPlanning.cs:69-72` | `private async Task<List<MemorySummaryPlanEntry>> ScanMemorySummaryQueueAsync<T>(` — 每片8槽，当前片tombstone；纯引用compaction在结构仍有效时发布，变化则部分defer | `mixed-host` |
| `memory.plan.build` | `MyBehavior.MemorySummaryPlanning.cs:159-162` | `private async Task<MemorySummaryPlan> BuildMemorySummaryPlanAsync(` — 独立重验无效owner；worker仅按冻结metadata去重排序，cleanup不建多余计划 | `mixed-host` |
| `memory.editor.guard` | `MyBehavior.MemorySourceWrites.cs:13-16` | `private bool IsMemorySourceEditorCurrent(long generation)` — 开窗generation、物理主线程、Instance和Campaign owner同时验证 | `mixed-host` |
| `memory.editor.text` | `MyBehavior.cs:50038-50041` | `private void OpenDevDailyMemoryLineTextEditor(` — 文本保存/取消真实红绿证据；同代记录引用/指纹也须保持 | `mixed-host` |
| `memory.import.single` | `MyBehavior.cs:54876-54879` | `private void ImportSingleNpcDialogueHistoryData(` — 单NPC导入窗口生命周期，真实文件路径/ReadJson/选择/Apply受控回放 | `mixed-host` |
| `memory.import.batch` | `MyBehavior.cs:56981-56984` | `private void ImportDialogueHistoryData(` — 记忆批量导入生命周期，保留overwrite/merge业务 | `mixed-host` |
| `memory.import.hero-all` | `MyBehavior.cs:55073-55076` | `private void ImportHeroNpcAllData(` — 仅AF汇总导入窗口门禁；业务owner不重写，本轮结构验证 | `mixed-host` |
| `memory.import.all` | `MyBehavior.cs:57509-57512` | `private void ImportAllData(` — 仅AF全量导入窗口门禁；非全部导入业务已运行验收 | `mixed-host` |
| `memory.summary.raw-view` | `MyBehavior.MemorySummaryInput.cs:126-129` | `private MemorySummarySourceView ReadMemorySummarySource(` — 主线程/owner/队列/目标资格，直接读取 raw 字典状态；不把 live view 留给异步请求 | `mixed-host` |
| `memory.summary.scene-dependencies` | `MyBehavior.MemorySummaryInput.cs:88-91` | `private static void DescribeMemorySummaryDailyContext(` — 首次构建有序 header / 非空正文场景依赖；明确场景和无效设置变动不误退 | `mixed-host` |
| `memory.summary.effective-context` | `MyBehavior.MemorySummaryInput.cs:205-208` | `private string CaptureMemorySummaryContextFingerprint(` — 实际有效目标字数、写作要求、目标 observer、名字 resolver、解析身份 | `mixed-host` |
| `memory.overview.pending-projection` | `MyBehavior.cs:26803-26806` | `private bool HasMemoryOverviewPendingBlocks(` — 一轮资格投影，保留原始ID占位、计数和非幂等标题；不复制/排序无关大图 | `mixed-host` |
| `memory.material.index-owner` | `Refactor/Runtime/EventSourceMaterialIndex.cs:11-14` | `internal sealed class EventSourceMaterialIndex<T> where T : class` — 独立派生索引/绑定 owner，无游戏和存档写入 | `source-linked-offline-verified` |
| `memory.material.index-build` | `Refactor/Runtime/EventSourceMaterialIndex.cs:39-42` | `internal Dictionary<string, T> Build(List<T> source)` — 未发布重建、命名last-wins/空键first-wins | `source-linked-offline-verified` |
| `memory.material.record` | `MyBehavior.cs:13707-13710` | `private void RecordEventSourceMaterial(` — 原记录/追加/发布仍归主体 owner，使用独立索引 | `source-linked-offline-verified` |
| `memory.material.rebuild` | `MyBehavior.cs:20162-20165` | `private void RebuildEventSourceMaterialIndex()` — 重建后复核来源引用，再发布和绑定 | `source-linked-offline-verified` |
| `memory.sealing.continue` | `MyBehavior.MemorySealing.cs:180-183` | `private bool ContinueDailyMemorySeal(long startTimestamp, double budgetMs, bool requirePendingProbe)` — 有限Campaign窗口共享封存授予；独立/同步调用与深原子成本单列 | `source-linked-offline-verified` |
| `memory.budget.runtime` | `Refactor/Runtime/MemoryMaintenanceWorkBudget.cs:10-13` | `internal sealed class MemoryMaintenanceWorkBudget` — 独立协作预算窗口；不抢占单次深操作 | `source-linked-offline-verified` |
| `memory.budget.resolve` | `MyBehavior.MemoryMaintenanceBudget.cs:16-19` | `private void ResolveDailyMaintenanceBudget(` — 有限Campaign周期懒创建共享窗口，空闲不读取预算设置 | `source-linked-offline-verified` |
| `memory.budget.cycle` | `MyBehavior.cs:17733-17736` | `private void RunCampaignMemoryMaintenanceCycle(` — 真实主/deferred维护共享周期与异常/nested恢复 | `source-linked-offline-verified` |
| `memory.budget.caller` | `MyBehavior.cs:17687-17690` | `private void OnCampaignTick(float dt)` — 实际Campaign入口接入共享维护周期，其他顺序保持 | `source-linked-offline-verified` |
| `memory.budget.deferred` | `MyBehavior.cs:5923-5926` | `private void ProcessDeferredDailyMaintenance()` — 复用共享deadline，原子超时后不再开始下一维护域 | `source-linked-offline-verified` |

## 核对或更新

运行仓库 Skill 的 `scripts/verify_code_map.py`，默认按记录的 Git 提交验证符号、一基行号和内容摘要；加 `--working-tree` 核对当前文件。主体变化后，按新源码更新 JSON、此表与 HANDOFF，保留历史提交作对照。不能只刷新行号/hash 就宣称新功能通过验收。

本轮完整raw摘要接入独立4096-byte buffer writer，私有DTO122字段映射留owner边界；不迁移存档类型，不修改通用JSON摘要/Prompt/权威写入。对应字段与code-unit反例和原版成本对照已验证，但仍完整原子O(N)，不是深来源预算或主体总拆薄完成。

owner封存尾部已逐draft计费并复用稳定排序；单draft内line净化与weekly trigger bind现按共享metadata计费，同步Sanitize与续跑共用原line/bind规则，未完成draft的列表保持私有。trigger列表sanitize、首次capture/copy、全owner绑定和Apply仍有原子工作；当前不按“大类行数减少”或“全部拆完”交付。
