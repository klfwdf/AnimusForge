# J07 Conversation 核心 / Native —— 实施计划与注意事项

> 基线：分支 `codex/af-main-refactor-continuation-20260831`，产品源码 `70db6ec2`，测试终点 `08699b4f`，文档 `63e74e7d`。
> 本文件是 J07 开工前的可执行计划，**不改任何源码**。总计划条目见[主台账 J07 节](../animusforge-refactoring-and-repository-reorganization-plan.md)。
> 状态：`PLAN_READY / NOT_STARTED`。

---

## 0. 为什么 J07 和前面几包不一样（先读这段）

J04–J06 搬的是**纯算法**：输入输出确定、可脱离游戏对象、可用 fake port 逐字节对照。J07 搬的是**会话生命周期**——它的正确性不在"返回值一致"，而在：

- 一次玩家提交只被受理一次（admission ticket）；
- 异步跳跃后目标仍是同一个人、同一局存档、同一轮对话（generation / epoch / presentationRevision 三重身份）；
- 失败时历史要回滚，成功时提交只发生一次（pending history + commit receipt）；
- 已经开始的主线程操作**不能**被取消或重试（动作已经改了游戏状态）。

这些是**时序不变量**，不是函数值。所以 J07 的验收重点从"文本逐字节相同"转向"**边界断言 + 变异拒收**"：每条不变量都要有一条会因删除守卫而变红的测试。

---

## 1. 现状盘点（本轮实测，一基行号）

### 1.1 已经成型的 owner（在 `Refactor/`，只需归位，不需重写）

| 文件 | 类型 | 职责 | 读游戏对象 |
| --- | --- | --- | --- |
| `Refactor/Runtime/InteractionRequestCoordinator.cs:15` | `InteractionRequestCoordinator : IDisposable` | 每 key 一个在飞 lease（`_inFlight:18`），新请求取消旧的；pipeline 前后各查一次 generation | 否 |
| `Refactor/Runtime/InteractionRequestLease.cs:12` | `InteractionRequestLease` | 单请求 lease；协作式取消，明确不声称回滚 | 否 |
| `Refactor/Runtime/DetachedInteractionHost.cs:16` | `DetachedInteractionHost` | capture→generate→commit；**仅在 commit 开始前**允许 fallback（`ExecuteAsync:32`、`FallbackAsync:332`） | 否（capture 是 `Func<string, InteractionEnvelope>` 委托 `:18`） |
| `Refactor/Runtime/InteractionResultCommitter.cs:16` | `InteractionResultCommitter` | 主线程唯一提交边界：先执行 ActionPlan 再写历史（`Commit:25`） | 否（经 `IActionPlanExecutor`/`IInteractionMemory`） |
| `Refactor/Runtime/InteractionCommitReceiptCache.cs:13` | 幂等回执缓存，上限 512（`:15`） | 只有终态条目可淘汰 | 否 |
| `Refactor/Runtime/NpcPersonaGenerationOwner.cs:10` + `PersonaGenerationWaiter.cs:9` | 人物生成预约 + 可放弃等待 | lease 身份跨 async 存活 | 否 |
| `Refactor/Runtime/RuntimeConfigSnapshotStore.cs:13` | 每次交互冻结配置快照 | 隔离后续 MCM 改动 | 否 |
| `Refactor/Contracts/InteractionPipeline.cs:12`、`FullInteractionPipeline.cs:14` | 两个 `IInteractionPipeline` 实现 | 同步 run host | 否 |
| `Refactor/Contracts/InteractionContracts.cs` | 全部 DTO 与端口（`InteractionEnvelope:121`、`ActionPlan:305`、`InteractionResult:427`、`IInteractionMemory:456` …） | 契约词汇表 | 否 |

**结论：J07a 主要是"搬家 + 接线"，不是"重写"。** 这 9 个文件已经没有 TaleWorlds 依赖，用 `git mv` 纯改路径即可（参照 J05b 九文件 100% rename 的做法）。

> 命名陷阱：`NotorietyConversationOutcomeReceipt.cs`、`CourierInboundCompletionReceipt.cs`、`DuelOutcomeReceipt.cs` 名字里有 Conversation/Completion，但它们是**领域回执账本**，不是会话宿主。J07 不动它们（Notoriety→J13，Courier→J10，Duel→J09）。

### 1.2 真正要拆的宿主（在 `ShoutBehavior*.cs`）

已经 partial 化的五个文件（合计 693 行），边界清晰、注释完整：

| 文件 | 行数 | 关键入口 |
| --- | --- | --- |
| `ShoutBehavior.NativeAdmission.cs` | 260 | `SubmitNativeConversationAdmittedAsync:65`、`CaptureNativeConversationAdmissionOnMainThread:125`、`CaptureNativeConversationContext:159`（**读 `Campaign.Current.ConversationManager:163`、`ActiveToken:172`、`Mission.Current:173`**）、`IsNativeConversationAdmissionCurrent:184`、`NativeConversationPresentationScope:219` |
| `ShoutBehavior.NativePreparation.cs` | 83 | `CaptureNativeConversationPreparation:27`（**读 `Mission.Current.Agents:67`、Hero/CharacterObject、meeting/scene taunt 指令 :51/:54**） |
| `ShoutBehavior.NativePendingHistory.cs` | 134 | `PrepareNativeConversationPendingHistoryAsync:20`、`RollbackNativeConversationPendingPlayerHistoryAsync:51`、`RunNativePendingHistoryOnMainThreadAsync<T>:65` |
| `ShoutBehavior.NativeCompletion.cs` | 138 | `CaptureNativeConversationCompletionOnMainThread:44`、`IsNativeConversationCompletionCampaignCurrent:61`、`CompleteNativeConversationReplyOnMainThread:75`（→ `MyBehavior.CommitDialogueHistoryWithScene:95`）、`QueueNativeConversationCompletionExit:124` |
| `ShoutBehavior.NativeActionDispatch.cs` | 78 | `ExecuteNativeConversationActionDispatch:30` |

**问题集中在 `ShoutBehavior.cs` 里还没 partial 化的三块：**

| 位置 | 行数 | 说明 |
| --- | --- | --- |
| `SubmitNativeConversationTextInternalAsync:20081-20564` | **484 行** | Native 一回合的唯一编排体。实测含 **9 次 `await` 主线程往返**、**1 次 `Task.Run`**（`persistedHeroHistoryTask:20163`）、**4 次 `SaveRuntimeGuard.IsStale` 检查**（20118/20180/20309/…）、**5 处回滚调用**（20323/20360/20390/20463/20531） |
| `ApplyNativeConversationGameActionsOnMainThreadAsync:19549-19630` | 82 行 | 排队派发，`dispatchState` 0/1/2 三态（`:19576`）+ 超时 `AwaitDispatch:19605` |
| `ApplyNativeConversationGameActionsCore:19632-19684` | 53 行 | 实际执行体 |

### 1.3 身份/代际字段（异步跳跃后重验的依据）

- `_nativeConversationAdmission`（`NativeAdmission.cs:14`）—— 当前票据槽，身份用**引用相等**判断（`:187`）
- `_nativeConversationAdmissionEpoch`（`:15`）—— `ConversationEnded` 时 `Interlocked.Increment`（`:60`）
- `_nativeConversationPresentationRevision`（`:16`）—— 覆盖层重开时递增（`:148`）
- `SaveRuntimeGuard.CaptureGeneration()` / `IsCurrentGeneration()` —— 存档代际
- `CurrentInstance`（`ShoutBehavior.cs:2515`）—— 静态 owner 身份
- 每张票据冻结副本：`Generation:22`、`ConversationEpoch:23`、`PresentationRevision:24`、`ConversationManager:25`、`ConversationToken:26`、`Mission:27`、`Hero:28`、`Character:29`、`AgentIndex:31`

### 1.4 持久化边界（**J07 一个 key 都不能动**）

- `ShoutBehavior.SyncData:10851` 只存 `"_sceneHeroRevisitDays_v1"`（`:10875`/`:10878`）。
- **admission / epoch / presentationRevision / pending history 全部不入档**——它们是进程内状态。这条是 J07 的护栏：搬家后仍然不许有任何新 SyncData key。
- Native 的持久写只有一条间接路径：`NativeCompletion.cs:95` → `MyBehavior.CommitDialogueHistoryWithScene(...)`，落到 `_dialogueHistory_v2`（J05 已归位的 owner）。

### 1.5 现有 runner（J07 的既有安全网）

Python harness（`python tools/<dir>/run.py`，编译 `*.cs.txt` 成 `Proof.csproj`）：
`NativeConversationAdmissionTests`（+`run_mutations`/`run_original`/`run_presentation`×3）、`NativePreparationBoundaryTests`、`NativePendingHistoryBoundaryTests`（+mutations）、`NativeActionDispatchOutcomeTests`（+mutations）、`NativeCompletionBoundaryTests`（+mutations）、`NativeHistorySnapshotTests`（+`source_parity.py`）、`NativeModuleSubmissionTests`（+`source_boundary.py`）、`NativeTtsFallbackBoundaryTests`、`InteractionRequestLifetimeTests`（+`verify_compat.py`）。

csproj 契约 runner：`InteractionPipelineContractTests`（含 `InteractionCommitReceiptTests` / `DetachedHostCommitBoundaryTests` / `AsyncInteractionOwnerTests`）、`DuelDispatchContractTests`、`CourierInboundCompletionContractTests`、`NotorietyConversationOutcomeContractTests`。

**`tests/modules/` 下目前没有任何 Conversation 套件——J07 要新建 `tests/modules/AF.Module.Conversation/`。**

---

## 2. 目标形态

```
src/modules/AF.Module.Conversation/
├── Internal/                      # 渠道无关的请求生命周期（J07a，纯 rename 为主）
│   ├── InteractionRequestCoordinator.cs
│   ├── InteractionRequestLease.cs
│   ├── DetachedInteractionHost.cs
│   ├── InteractionResultCommitter.cs
│   ├── InteractionCommitReceiptCache.cs
│   ├── NpcPersonaGenerationOwner.cs
│   ├── PersonaGenerationWaiter.cs
│   ├── RuntimeConfigSnapshotStore.cs
│   └── Pipeline/{InteractionPipeline,FullInteractionPipeline}.cs
└── Channels/Native/               # Native 渠道的会话状态机（J07b/c，真拆）
    ├── NativeConversationTicket.cs        # 票据 DTO + 三重身份比较（纯）
    ├── NativeConversationAdmissionPolicy.cs  # 准入判定（纯，不含 Campaign 读）
    ├── NativeConversationStageSequencer.cs   # 一回合阶段顺序 + 每阶段重验点（纯）
    └── NativeConversationRollbackPolicy.cs   # 失败原因 → 回滚/不回滚（纯）
```

宿主保留：`ShoutBehavior.Native*.cs` 五个 partial 仍是**游戏线程适配器**（读 `Campaign`/`Mission`/`Hero`、排队 `_mainThreadActions`、调 UI 覆盖层），但判定逻辑改调 owner。`AnimusForgeNativeConversationOverlay*.cs` 完全不动。

---

## 3. 切片

### J07a —— Internal 生命周期 owner 归位（低风险，先做）

1. `git mv` 上表 9 个文件到 `src/modules/AF.Module.Conversation/Internal/`（含 `Pipeline/` 两个）。**命名空间、类型名、成员可见性一律不改**（照搬 J05b 做法，git 应报 100% similarity）。
2. 更新引用路径：`Refactor/Adapters/LegacyChannelInteractionFacade.cs:17,28,57`、`LegacyNativeActionPlanExecutor.cs:244,246`，以及所有 `tools/*/​*.csproj` 里的 `<Compile Include>`（预计 10+ 个工程，逐个 grep 确认）。
3. 验收：`InteractionPipelineContractTests`、`InteractionRequestLifetimeTests`、`DuelDispatchContractTests`、`CourierInboundCompletionContractTests` 全绿；Debug+Release × 1.3/1.4/Bootstrap。
4. **不新增契约**——纯 rename 不产生新行为，新增断言留给 J07b。

### J07b —— Native 阶段序列 owner（核心，最高风险）

把 484 行的 `SubmitNativeConversationTextInternalAsync` 拆成**阶段表驱动**：

```
准入重验 → 人物就绪 → 准备快照 → [并行] 历史 Task.Run
        → J04 三步 Prompt 构建（已完成，不动）
        → 历史 join + 重验 → 消息装配 → pending 历史写入
        → LLM 调用（J08 接缝，本包只留端口）
        → 回复重验 → 动作派发 → 后处理（J09 接缝）
        → 完成提交 / 失败回滚
```

- `NativeConversationStageSequencer`（纯）持有**阶段顺序**和**每个阶段跳跃后必须重验哪几项身份**的表；宿主按表执行，不再手写 9 处散落的 `IsNativeConversationAdmissionCurrent` 调用。
- `NativeConversationRollbackPolicy`（纯）决定"哪些失败原因需要回滚 pending 玩家历史"——当前 5 处回滚的 reason 串（`main_thread_validation_failed` 等）收敛到一处。
- 宿主方法体目标：**≤120 行**，只剩游戏线程读写与 await 编排。
- 同时把 `ApplyNativeConversationGameActionsOnMainThreadAsync` 的 `dispatchState` 三态语义提成 owner 里的显式状态枚举（值和时序不变）。

### J07c —— 主动开场 / 关窗 / 失败文案

- `npcInitiatedOpening` 路径（`:20108-20109` 的 `BuildNpcInitiatedOpeningUserText` / `...PersistentFactText`）与 pending opening 消费顺序（`NativeAdmission.cs:141` 注释明确："不能在 busy 拒绝之前消费"）写进准入 owner 的契约。
- `QueueNativeConversationCompletionExit`（`NativeCompletion.cs:124`）与 `InvalidateNativeConversationAdmissionOnConversationEnd`（`:54`）的先后关系固化为断言。
- 失败文案 `BuildNativeConversationPreprocessUnavailableText` / `SaveRuntimeGuard.BuildStaleRequestErrorText` 逐字不变。

---

## 4. 验收标准（每条都要可执行、可变异）

### 4.1 新建 `tests/modules/AF.Module.Conversation/`

| 套件 | 断言 | 必须被拒收的变异 |
| --- | --- | --- |
| `Lifecycle` | 新请求取消旧 lease；pipeline 前后各查一次 generation；已开始的 commit 不允许 fallback | 删掉 pipeline 后的 generation 复查；把 `FallbackAsync` 放宽到 commit 之后 |
| `NativeTicket` | 三重身份（generation/epoch/presentationRevision）任一不符即判过期；引用相等判票据 | 只比 generation；用值相等替换引用相等 |
| `NativeStageSequence` | 9 个跳跃点的重验项完整；顺序不可交换；pending opening 在 busy 拒绝之后消费 | 删任一重验点；把 opening 消费提到 busy 检查之前 |
| `NativeRollback` | 5 类失败原因各自的回滚/不回滚决策；成功路径不回滚 | 把"动作已派发后失败"改成回滚（会导致重复写历史） |

### 4.2 既有 runner 全绿（J07 每个切片后复跑）

`NativeConversationAdmission`（6 个入口）、`NativePreparationBoundary`、`NativePendingHistoryBoundary`（+mut）、`NativeActionDispatchOutcome`（+mut）、`NativeCompletionBoundary`（+mut）、`NativeHistorySnapshot`（+parity）、`NativeModuleSubmission`（+source_boundary）、`NativeTtsFallbackBoundary`、`InteractionRequestLifetime`（+compat）、`InteractionPipelineContract`。

### 4.3 硬门槛

- `AfDialogueClient` / `ShoutBehavior.ModuleNativeSubmission.cs` 对外签名**一字不改**（子 MOD 契约）。
- `SyncData` key 集合不变（4.1 的套件里加一条源码级断言：`ShoutBehavior.SyncData` 仍只有 `_sceneHeroRevisitDays_v1`）。
- 原脚本等价 Debug+Release × 1.3/1.4/Bootstrap 六项 0 错误。
- 代码地图刷新并绑定新的产品源码提交。

---

## 5. 注意事项清单（开工前逐条确认）

### 5.1 绝对不能碰

1. **不新增任何 SyncData key**，也不改现有 key/类型/`MyBehaviorSaveableTypeDefiner` 编号。会话状态是进程内的，搬家不改这一点。
2. **不动 `AnimusForgeNativeConversationOverlay*.cs`**（UI 覆盖层）和 `AfDialogueClient` 对外签名。
3. **不动 Scene 和 Courier 的会话链**——它们归 J10。J07 只做 Native + 渠道无关的 Internal。本包不得声称"三渠道线程重写完成"。
4. **不删旧同步入口**：`ShoutBehavior.cs` 里仍有明确的 setter-only / 同步消费者（J06 已记录），删掉会静默改行为。
5. 不改 `Refactor/Runtime/` 里三个领域回执账本（Notoriety/Courier/Duel）。

### 5.2 时序不变量（源码注释已写明，搬家后必须继续成立）

6. **票据只能释放自己**（`NativeAdmission.cs:93`）：迟到完成的旧请求用 `Interlocked.CompareExchange(ref slot, null, admission)`，不能无条件清空。
7. **capture 一旦开始，结果必须被接受**（`:120`）——不能在 capture 中途因为新请求到达就丢弃。
8. **pending opening 在 busy 拒绝之后消费**（`:141`）——顺序反了会吃掉一次开场。
9. **`ConversationEnded` 才递增 epoch**（`:53`/`:59`），不能用 UI 关闭或可复用的 `ActiveToken` 代替；同 token 重开算新一轮。
10. **已发布并被领取的主线程操作不能被强制失败/取消**（`ShoutBehavior.cs:19505`）；**超时计时器不能回滚或重复游戏动作**（`:19613-19614`）。
11. **提交边界只在主线程**（`InteractionResultCommitter.cs:12-14`）；**抛异常的 owner 可能已经改了状态 → 记终态 unknown 回执，不可重试**（`:251-253`）。
12. **只有 commit 开始前允许 fallback**（`DetachedInteractionHost.cs:13-14`）。
13. **completion 校验的是捕获的上下文与 revision，不是当前后端槽**（`NativeCompletion.cs:131-132`）——否则旧的关窗会关掉新会话。
14. **history 写失败不能吞掉动作驱动的退出**（`NativeCompletion.cs:101`）。
15. **诊断/观测不得改变动作是否执行**（`NativeActionDispatch.cs:74`）、不得伪造队列完成（`NativePendingHistory.cs:130`）。

### 5.3 工程纪律

16. **先 J07a 再 J07b**：纯 rename 单独成提交、单独验证，避免"搬家 + 重写"混在一个 diff 里无法二分。
17. 所有宿主改写继续用 **Python 字节级脚本**（`re` + `\r?\n`，保留 BOM）；`ShoutBehavior.cs` 是 CRLF + 39,669 行，**禁止用 Edit 工具**（会规范化行尾，制造假 diff）。
18. 每个切片后必须跑**受影响的 runner**，不是只跑新契约；Native 五组互相耦合。
19. **484 行方法一次只拆一个阶段**，每拆一段就编译 + 跑 Native 五组，不要一次性重排。
20. 变异测试必须**真的会红**：J06 有过 `secondary-unbounded` 变异因为场景不触发而假绿的教训，新增每条变异都要先确认它在修复前失败。
21. 环境：`AF_DOTNET` / `AF_NEWTONSOFT` 环境变量；Scene runner 需显式 `--dotnet`；`ChannelCutoverBoundaryTests` 需 `--newtonsoft`。硬编码本机路径的 csproj 一律改占位符（J06 已修过 Index 一例）。
22. **不推送、不部署、不写游戏目录、不动存档**，除非明确授权。

### 5.4 已知风险

23. `SubmitNativeConversationTextInternalAsync` 中间段（20200–20300）是 **Prompt 消息装配**，属于 J04 已归位部分的调用点——拆阶段时容易误把它当"会话逻辑"搬走。边界：**凡是产出 prompt 文本的都不属于 J07**。
24. `persistedHeroHistoryTask`（`:20163`）是唯一的并行分支，join 点（`:20194`）后紧跟一次重验（`:20195`）。拆阶段时这对"fork/join + join 后重验"必须保持成对。
25. `NativeConversationMainThreadPreprocessTimeoutMs = 30000`（`ShoutBehavior.cs:2250`）与 `_mainThreadActions` 队列（`:2254`）是宿主级共享状态，owner 不应持有它们，只描述"这一步需要主线程"。

---

## 6. 交付物

- 产品源码：`src/modules/AF.Module.Conversation/{Internal,Channels/Native}`；`ShoutBehavior.Native*.cs` 五 partial 改为适配器；`SubmitNativeConversationTextInternalAsync` ≤120 行。
- 测试：`tests/modules/AF.Module.Conversation/{Lifecycle,NativeTicket,NativeStageSequence,NativeRollback}` 四套 + 各自变异。
- 文档：主台账 J07 回执（含保留项与未闭合项）、代码地图刷新绑定、`af-framework-code-scope.md` 更新、HANDOFF 置顶。
