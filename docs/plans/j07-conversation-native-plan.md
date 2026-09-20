# J07 Conversation 核心 / Native —— 实施计划与注意事项

> 基线：分支 `codex/af-main-refactor-continuation-20260831`，产品源码 `70db6ec2`，测试终点 `08699b4f`，文档 `63e74e7d`。
> 本文件是 J07 可执行计划；各切片实施与验证由主台账当前节记录。总计划条目见[主台账 J07 节](../animusforge-refactoring-and-repository-reorganization-plan.md)。
> 状态：`ACTIVE / J07a_RELOCATED`。G1/G2 补强和 J07a 已实施；下一步 J07b Native 职责拆分。见[当前回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j07a-relocation-20260920)。J07a 的限定完成不代表 J07 父包验收。
> GitHub 指定交付分支已核对并补推至 `b9b2215b`（原远端 `2946bf3d`，3 个提交普通快进）。本地施工分支仍为 `codex/af-modularize-j04-20260918`；不要用本地分支名替代发布目标。
> 本轮当前证据入口：[2026-09-20 推送与接续计划](../animusforge-refactoring-and-repository-reorganization-plan.md#j07-plan-review-20260920)。旧版“J06 只剩实机”的判断由该节纠正。

---

## 0. 为什么 J07 和前面几包不一样（先读这段）

J04–J06 主要抽取检索、组合算法，同时已经调整捕获与后台调度，不能统称无时序风险的纯搬迁。J07 重点拆**会话生命周期**，除了保留文本/功能语义，还必须验证：

- 一次玩家提交只被受理一次（admission ticket）；
- 异步跳跃后目标仍是同一个人、同一局存档、同一轮对话（generation / epoch / presentationRevision 三重身份）；
- 失败只回滚本请求拥有、仍允许撤销的 pending history；不撤销或重试已经开始的权威动作，成功提交只发生一次；
- 已经开始的主线程操作**不能**被取消或重试（动作已经改了游戏状态）。

这些是**时序不变量**，不是函数值。所以 J07 的验收重点从"文本逐字节相同"转向"**边界断言 + 变异拒收**"：每条不变量都要有一条会因删除守卫而变红的测试。

---

### 0.1 本次复核后的执行门槛

| 顺序 | 工作与边界 | 完成标准 / 下一步 |
| --- | --- | --- |
| G0 基线 | 核实 Git、SDK/引用、原 runner、五组 Native、公开 API/存档基线；列出历史红测与环境缺失 | 基线证据可复跑；不得只改 hash 消红。无关历史失败分开登记，相关失败先查清 |
| G1 J06 原文分支 | 修正实体差分共同输入，并覆盖后台 raw-only、显式王国限定、多国同称谓和长短称谓遮蔽 | 旧同步/新 capture→worker→complete 使用同一 input/mentions；只删新侧 raw 分支必须在具名用例失败 |
| G2 J06 类别与请求 | 补定居点/家族/王国、可见队伍、常驻实体及失败回退；接最终 Native/Courier 请求 | 非空正文、ID、计数、显式王国集合、顺序逐项对照；保留完整序列化请求比较与文本丢失反例 |
| J07a 归位 | 8 个 Runtime 文件 + 2 个 Pipeline 文件原字节迁移，更新实际路径消费者 | 100% rename；契约词汇表不迁入 Internal；Compile/资源、ABI、原 runner 和双版本无回归 |
| J07b1 准入 | 提取票据身份与准入判定，仍由宿主捕获游戏对象 | busy 不吃 opening、旧请求只释放自己、同 token 新 epoch、load/owner 替换拒收 |
| J07b2 编排 | 一次抽一个实际阶段，保留 fork/join、五步 Prompt、提前可见输出和后处理次序 | 每次抽取后编译及 Native 五组；新 owner 真正承担阶段顺序/判定，不能只拆 partial |
| J07b3 终态 | 收敛 pending 撤销、排队/已领取派发、unknown 回执和完成退出 | 强制 yield 下取消、异常、迟到、重复完成与回执重放均不重复动作/历史；不关闭新会话 |
| J07c 交互 | 固化主动开场、失败提示、TTS 和关窗行为 | 玩家可见文本及已批准时序不变；主文早显示、动作驱动退出、历史写失败均有断言 |
| J07d 包验收 | 共享生命周期消费者回归、公开 API/存档、双版本/Bootstrap、地图/台账 | 只在必要离线门槛通过后记 `J07_OFFLINE_VERIFIED`；LIVE/SAVE/provider 单独列 NOT-RUN |

默认串行 G0→G1→G2→J07a→J07b1/b2/b3→J07c→J07d。G0 清楚且不改变行为时，可先做独立 J07a；不能借此跳过 G1/G2 就把新的 Native 编排签收。实机帧性能并非纯 rename 的前置条件，不把整包无限卡在无关实机事项。

**已确认的 J06 证据缺口（测试/文档问题，不等于已复现产品缺陷）：**

- `tests/modules/AF.Module.Knowledge/EntityTextDifferential/Program.cs:136` 给新侧 `MatchDetachedCandidates` 传 `""`，但 capture/complete 使用非空原文；现有 Hero 例碰巧同结果，不能验证后台原文称谓路径。G1 必须统一输入，不能只给最终 formatter 换字符串。
- 同文件 `:99-113` 把定居点/家族候选置空，多类 formatter 和常驻实体仍为假端口。可以提取实际生产方法、用可控游戏替身扩大离线验证；真实 TaleWorlds 属性成本另列实机，不能称全部“离线不可闭合”。
- `tests/modules/AF.Module.Prompt/SharedCompletionDifferential/run.py:90-145` 当前把组件差分产生的文本输送给共享 completion 和最终请求构造器，证明的是已选 fixture 的文本传递。G2 应统一各组件的共同 input/mentions/目标，验证 DTO→实际消费者与失败回退；不能以把字符串塞入环境变量冒充完整真实 Host 链路。
- G1/G2 仅允许补测试、必要的测试路径适配和证据；如暴露生产回归，单独记录旧/新行为、修复点和回归范围后再做最小修复，不顺带重写政策/宴会/GCCZ。

## 1. 现状盘点（基线源码一基行号，2026-09-20 复核）

本节保留开工基线 `70db6ec2` 的定位；J07a 迁移后当前路径见上方回执，不能再次从已移除旧路径重复搬家。

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
| `Refactor/Contracts/InteractionPipeline.cs:12`、`FullInteractionPipeline.cs:14` | 两个 `IInteractionPipeline` 实现 | 异步生成编排，经端口执行 | 否 |
| `Refactor/Contracts/InteractionContracts.cs` | 全部 DTO 与端口（`InteractionEnvelope:121`、`ActionPlan:305`、`InteractionResult:427`、`IInteractionMemory:456` …） | 契约词汇表 | 否 |

**结论：J07a 主要是原样归位，不是重写。** 明确迁移清单共 **10 个文件**：表中的 8 个 Runtime 文件，以及 `InteractionPipeline.cs`、`FullInteractionPipeline.cs` 两个实现。`InteractionContracts.cs` 是共享契约词汇表，**本包保留原路径和全部类型身份**，不随实现挪入 Internal；后续契约目录整理另按 J14/全仓规划处理。不得把“9 个表格项”误当成 9 个文件。

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
- **admission / epoch / presentationRevision 与 pending 请求控制对象不新增持久化**；`NativePendingHistory.cs:10-18` 的请求对象不是持久化 commit receipt。仍保留其调用既有历史/AFEF owner 的效果，不能把“不新增 key”误读为“不写历史”。
- Native 的完成历史入口为 `NativeCompletion.cs:95` → `MyBehavior.CommitDialogueHistoryWithScene(...)`；pending history 与动作产生的 AFEF/领域存档仍有各自 owner，不能据此断言 Native 全链路只有一个持久写点。J07 不迁移或新增这些 key。

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

1. `git mv` 第 1.1 节明确的 10 个文件到 `src/modules/AF.Module.Conversation/Internal/`（含 `Pipeline/` 两个）。**命名空间、类型名、成员可见性一律不改**（照搬 J05b 做法，git 应报 100% similarity）。
2. 更新真正依赖文件路径的 `.csproj`、提取 runner、地图等；全仓搜索旧路径确保无漏项。`LegacyChannelInteractionFacade` / `LegacyNativeActionPlanExecutor` 等为类型消费者，命名空间不变就不应为搬家改其业务或 using。逐项核对 Compile/资源集合只有预期路径映射，无重复/遗漏。
3. 验收：`InteractionPipelineContractTests`、`InteractionRequestLifetimeTests`、`DuelDispatchContractTests`、`CourierInboundCompletionContractTests` 全绿；Debug+Release × 1.3/1.4/Bootstrap。
4. 不新增产品契约或为搬家造新接口；沿用原行为契约，可补必要的路径完整性检查。既有 public 类型不能因为放入名为 Internal 的目录就改成 internal。

### J07b —— Native 阶段序列 owner（核心，最高风险）

把 484 行的 `SubmitNativeConversationTextInternalAsync` 拆成**显式、强类型的阶段编排**，先按当前真实消费者固定顺序，再抽职责；不强制创建无消费者的通用阶段表框架：

```
准入重验 → 人物就绪 → 准备快照 → [并行] 历史 Task.Run
        → J04/J06 五步 Prompt（Begin→Routing→Knowledge capture→worker→Complete）
        → 历史 join + 重验 → 消息装配 → pending 历史写入
        → LLM 调用（J08 接缝，本包不重写传输）
        → 回复重验 → 既有原文挑衅/场景观察/提前 TTS
        → 后处理开始重验 → 主文展示回调、后处理上下文及既有直接命令
        → 统一后处理 → 后处理结果重验
        → 主线程动作派发与完成提交 / 有条件 pending 撤销
```

- 顺序证据：`ShoutBehavior.cs:20504` 先 `TryRunSceneUnifiedActionPostprocess`，`:20536` 后 `ApplyNativeConversationGameActionsOnMainThreadAsync`；`:20349` 的原文挑衅、`:20376` 的提前 TTS 及中间直接场景命令为既有特例，不可借统一编排挪后或重复执行。
- `NativeConversationStageSequencer`（拟议）持有阶段顺序和边界重验策略，必须由真实 Native 入口调用；每次 await 的接收与副作用前仍在宿主所属线程核验，不能靠“表里写了要检查”代替实际执行。
- 票据中的 Hero/Mission/ConversationManager 活引用保留宿主捕获层。纯判定 owner 只消费冻结身份/事实；不得用 ID 字符串相等替代槽位引用身份，也不将游戏对象送到后台读取。
- `NativeConversationRollbackPolicy`（纯）决定"哪些失败原因需要回滚 pending 玩家历史"——当前 5 处回滚的 reason 串（`main_thread_validation_failed` 等）收敛到一处。
- 宿主方法体建议目标：**约 120 行以内**。真正验收标准是职责归属、唯一入口与时序不变量，不为压行数合并语句、加转发壳或隐藏副作用。
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
| `NativeRollback` | 5 个当前回滚调用点按真实阶段逐个映射；区分仅排队、已领取/执行、成功/unknown；只撤销本请求 pending，不撤销已执行动作 | 将 unknown 当可重试；旧请求撤销新请求的 pending；已领取后超时再次派发 |

### 4.2 既有 runner 全绿（J07 每个切片后复跑）

`NativeConversationAdmission`（6 个入口）、`NativePreparationBoundary`、`NativePendingHistoryBoundary`（+mut）、`NativeActionDispatchOutcome`（+mut）、`NativeCompletionBoundary`（+mut）、`NativeHistorySnapshot`（+parity）、`NativeModuleSubmission`（+source_boundary）、`NativeTtsFallbackBoundary`、`InteractionRequestLifetime`（+compat）、`InteractionPipelineContract`。

### 4.3 硬门槛

- `AfDialogueClient` / `ShoutBehavior.ModuleNativeSubmission.cs` 对外签名**一字不改**（子 MOD 契约）。
- `SyncData` key 集合不变（4.1 的套件里加一条源码级断言：`ShoutBehavior.SyncData` 仍只有 `_sceneHeroRevisitDays_v1`）。
- 经既有 `一键编译覆盖推送/build_single_module.ps1` 或仅传机器参数的 wrapper 执行 Debug/Release × 1.3/1.4/Bootstrap；记录原命令、引用版本、退出码及 warning/error。无 Stage/Deploy，不更改构建流程；脚本重置生成目录前另做范围/授权核查。
- 内外接口分开验收：已有 internal 消费者和两实现实际 DLL 的 public API/Native 提交契约都不退化；Scene/Courier 公开提交的未完成能力仍归 J10/J14，不在 J07 偷改 NotSupported 或宣称开放。
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
7. **capture 一旦开始，等待方必须取回它的结果**（`NativeAdmission.cs:120`），避免丢掉已占用票据；不等于过期结果可以无条件继续执行，后续仍要做 owner/generation/epoch/revision 重验。
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
17. 所有宿主改写继续用 **Python 字节级脚本**（精确字节锚点或保留 `CRLF` 的正则，保留原 BOM 状态）；`ShoutBehavior.cs` 是 CRLF + 39,669 行，**禁止用 Edit 工具**（会规范化行尾，制造假 diff）。
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

- 产品源码（下一实施任务）：`src/modules/AF.Module.Conversation/{Internal,Channels/Native}`；宿主保留薄游戏线程适配。建议压薄入口但不以行数替代真实职责拆分。当前 Internal 已原样归位；Channels/Native 的拟议新 owner 仍待 J07b 实现。
- 测试：`tests/modules/AF.Module.Conversation/{Lifecycle,NativeTicket,NativeStageSequence,NativeRollback}` 四套 + 各自变异。
- 文档：主台账 J07 回执（含保留项与未闭合项）、代码地图刷新绑定、`af-framework-code-scope.md` 更新、HANDOFF 置顶。

## 7. 验证执行与停点

- 第 5 节 **25 条编号保留**。其中 6–15、24–25 是源码时序/归属约束；16–22 是工程约定而不是凭空编造的源码注释。开工绑定当前源码 revision 与 symbol，旧行号仅导航。
- “Native 五组”明确为 `tools/NativeConversationAdmissionTests`、`NativePreparationBoundaryTests`、`NativePendingHistoryBoundaryTests`、`NativeActionDispatchOutcomeTests`、`NativeCompletionBoundaryTests`。每个实际阶段抽取后编译两 API 并复跑这五组正常入口；相关守卫变异随切片执行，完整生命周期/历史/API/共享渠道矩阵在 J07d 集成验收。
- G0 先查清 `NativePreparationBoundaryTests` 所关联 `GameLifetimeTests/source_parity.py` 已记录的旧基线断言失败：旧源码同样红与新回归分开，不删除断言或刷新 hash。更新定位需要独立的行为等价证据。
- 每条负向变异记录：编译成功、命中指定用例、得到预期失败原因。编译失败、路径缺失、任意非零退出都不算守卫拒收；全部变异跑完也不能替代正常用例 PASS。
- 使用强制异步 yield 和可控调度推进 busy、owner 替换、load、epoch 变更、presentation 重开、排队前超时、已领取后超时、抛异常、重复回执。禁止靠睡眠碰运气或只 grep 守卫字符串证明时序安全。
- 不删除当前仍服务 Scene/同步消费者的旧入口；对被替代路径做调用搜索，记录保留原因。迁移后的旧路径引用应仅剩明确历史文档，不保留第二套活动编排。
- 当前机器可用 SDK 为 `G:/AFMOD/.dotnet-sdk/dotnet.exe`（8.0.422），Newtonsoft 为同 SDK `sdk/8.0.422/Newtonsoft.Json.dll`；项目默认 `local/dotnet/8.0.425` 在本工作树不存在。通过 runner 已支持的 `AF_DOTNET`/`AF_NEWTONSOFT`、`--dotnet` 等显式选择，记录实际版本；不要改生产引用、全局 PATH 或为了 PASS 暗中下载 SDK。缺少配置入口时单独列出最小 runner 可移植性修复。
- 2026-09-20 较早的计划修订轮只改文档、未重跑整套构建；后续 J07a 的六构建与验证见当前主台账。较早审查复跑的证据是：291 锚点两模式、共享最终请求 12 场景、Native Knowledge 8 项/4 场景、Courier liveness 59 项/16 场景 PASS；后两 harness 有 CS0649 警告，不冒称全仓零警告或完整功能验收。

## 8. 交接、回滚与 J07 之后

- 计划修订起点 `b9b2215b` 已发布，当时产品源码为 `70db6ec2`；当前 J07a 源码已为 `fb5dc1ca`，后续仍以实际 Git/台账为准。后续每个完整职责切片独立提交，提交前保留其他作者更改；原样搬迁与算法/状态调整分别提交，便于 focused revert。
- 无论本地分支叫什么，获准发布目标均为 `origin/codex/af-main-refactor-continuation-20260831`。每次推送前 fetch/祖先核验，分叉即停止汇报；不强推、不动 main、辅助分支不擅自删除。
- `.dotnet-cli-home/`、`.tmp/`、构建产物、玩家数据及人工转发版不纳入提交；本计划本身不授予游戏部署权限；自动化状态按最新用户授权及 HANDOFF。
- 后续主线依主台账：J08 LLM 传输 → J09 Actions → J10 Scene/Courier → J11 制作组薄桥 → J12/J13 主体领域 → J14 版本化三渠道 public API → J15 内容归属 → J16 工具/Bootstrap/兼容 → J17 全仓交付验收。这里只记录依赖方向，不把计划当成已实施。
- 主体、同 DLL 制作组 internal 接口、独立子 MOD public API 三层不变；政策/宴会/GCCZ 业务规则不重写。全项目“收尾”必须单独核对行为复现、保留旧入口、API、双版本、旧存档和所需实机证据，不随 J07 局部完成自动 DONE。
