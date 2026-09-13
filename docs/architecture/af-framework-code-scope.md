# AF 框架代码范围图

本图是当前已验证源码 `aece8f3d` 的定位快照，与 GitHub 原重构分支基线 `e40c92d7` 区分。不是完整功能完成清单，也不把未列到的代码当成可删垃圾。实际行号/符号和逐文件摘要见同目录 `af-framework-code-map.json`；主体调整后更新当前图，而不是把路径或方法名永久锁死。

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

以下一基行号均属于源码 `aece8f3d`，仅为导航，不代表整个方法的改动量。用符号和固定提交重新定位。

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
| `memory.summary.dispatch` | `MyBehavior.MemorySummaryMainThread.cs:46-49` | `private Task<bool> RunMemorySummaryMainThreadAsync(long generation, Func<bool> operation)` — 主线程剩余额度内直达，否则排队；原 owner/generation/Campaign 与退休门禁保留 | `wired-boundary` |
| `memory.summary.drain` | `MyBehavior.MemorySummaryMainThread.cs:96-99` | `private void ProcessMemorySummaryMainThreadActions()` — inline/queued 共用每 Tick 两次操作；只约束 job，不是完整 record/time 上限 | `wired-boundary` |
| `memory.summary.accept` | `MyBehavior.cs:5007-5010` | `if (ApplyMemorySummarySuccess(result.Job, result.Block)) appliedDaily++;` — daily 代表点：提交前源重验并依据实际 Apply 回执计数；三型同方法，B1 record/time 仍待验 | `mixed-host` |
| `legacy.history` | `MyBehavior.cs:27923-27926` | `public static string BuildHistoryContextForExternal(` — Scene/Courier 仍调用，共享兼容入口不能盲删 | `retained-live` |
| `legacy.recall` | `MyBehavior.cs:33929-33932` | `private string BuildCompressedMemoryContextById(` — snapshot 和默认旧调用共用原算法 | `mixed-host` |
| `scene.postprocess` | `ShoutBehavior.ScenePostprocess.cs:25-28` | `private sealed class SceneActionPostprocessWorkItem` — 已有完整后处理；非本次重写 Scene 业务 | `mixed-host` |
| `courier.prepare.reply` | `CourierDeliveryBehavior.cs:4674-4677` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(recipient,` — 回信早期准备待线程审查，未迁快照 | `retained-live` |
| `courier.prepare.inbound` | `CourierDeliveryBehavior.cs:5102-5105` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(sender,` — 来信早期准备待线程审查，未迁快照 | `retained-live` |
| `memory.summary.capture` | `MyBehavior.MemorySummaryInput.cs:47-50` | `private MemorySummaryInput CaptureMemorySummaryInput(` — 三类完整来源主线程深拷贝，单大源仍是未切分原子单元 | `mixed-host` |
| `memory.summary.source-check` | `MyBehavior.MemorySummaryInput.cs:125-128` | `private bool IsMemorySummaryInputCurrent(` — 完整内容/Prompt/解析元数据重验，不靠 Save-only revision | `mixed-host` |
| `memory.summary.execute` | `MyBehavior.MemorySummaryInput.cs:132-135` | `private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(` — 共用原 provider/Build/Parse；主线程解析、波次/重试退休，完成后释放大 payload | `mixed-host` |
| `memory.source.facades` | `MyBehavior.MemorySourceWrites.cs:12-15` | `private static bool DeferMemorySourceWriteIfNeeded(` — 旧 void façade 主线程同步、后台 owner/generation 排队；不是持久接受回执 | `mixed-host` |
| `memory.summary.rendering` | `PlayerNotorietyBehavior.cs:203-206` | `internal static string CaptureMemorySummaryHistoryRenderingIdentity()` — 无 observer 公称/实际匿名别名纳入 daily 解析来源身份 | `mixed-host` |

**B1 仍在进行，尚未通过 record/time 门槛。** 新 capture/源检查/接受与旧 façade 封送已接入并有离线证据，不代表完整存储 writer、实机或全记忆重构完成；原子大源复制与全队列规划/整理仍需处理。

`wired-boundary` 仅局部接线验证；`mixed-host` 是新旧共用 host；`retained-live` 仍有实际调用/兼容责任；`readonly-api` 是当前公开只读面。没有 LIVE/SAVE 标签，因为本轮未运行真实游戏/存档。

## 核对或更新

运行仓库 Skill 的 `scripts/verify_code_map.py`，默认按记录的 Git 提交验证符号、一基行号和内容摘要；加 `--working-tree` 核对当前文件。主体变化后，按新源码更新 JSON、此表与 HANDOFF，保留历史提交作对照。不能只刷新行号/hash 就宣称新功能通过验收。
