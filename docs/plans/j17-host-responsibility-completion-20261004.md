# J 系列夙愿接续：两个大宿主的真实职责拆分计划

日期：2026-10-04。状态：**PLAN_READY / IMPLEMENTATION_NOT_STARTED**。

本轮用户授权是“写清楚怎么拆的计划”，不是产品实施、启动代理、提交、推送或部署授权。本文承接 J01–J17 与已完成的 F/P 切片，不重开一套重构工程；实施进度与证据仍只记在[主台账当前条目](../animusforge-refactoring-and-repository-reorganization-plan.md#j17-host-responsibility-completion-20261004)。

## 1. 我们最终要得到什么

**不是让 MyBehavior.cs / ShoutBehavior.cs 看起来变短，而是让修改一项 AF 规则、状态或交互流程时，不必再到两个万能宿主里求解。**

J 系列原意见[总纲](../animusforge-complete-refactor-program-20260831.md)、[J17 规格](j17-responsibility-closeout-plan.md)及[主台账结项目标](../animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)。本次将它落实为六个不能缩水的结果：

1. **真实归属**：范围内每项规则、状态转换、队列、重试与资源生命周期有具名 owner，全部生产消费者接它，不是只让测试接它。
2. **单权威**：不复制第二份人物、历史、记忆、库存、会话、任务或事实账本；同一状态的读取、写入、保存和退役责任一致。
3. **依赖单向**：宿主装配领域模块和游戏适配；领域模块不持有整个 MyBehavior/ShoutBehavior，不通过“大业务回调”返回旧宿主继续求解。
4. **三渠道同源**：Native、Scene、Courier 的同类资格、Prompt/history、标签、真实效果和 AFEF 由同一套领域 owner 决定；合法渠道差异留明确理由。
5. **行为与兼容不减**：保留既有玩法、设置、标签、失败/部分成功语义，以及双 API、程序集、存档和外部消费者身份。
6. **可维护且可验**：具名规则变更可以在所属模块与必要适配内闭环；有旧新行为、实际接线、生命周期、工作量及兼容证据。

**本计划不再以“先可用、拆分后置”关闭宿主职责目标。** 制作组接入可并行推进，但 `INTEGRATION_HANDOFF_READY` 不是“J 系列主体职责拆分完成”。实机和旧档仍独立验收，不能反过来用无实机环境阻塞安全的源码抽取。

## 2. 当前基线与历史成果的正确读法

### 2.1 本次只读核实

| 项目 | 编制时实际值 |
| --- | --- |
| 工作区 | `E:/AnimusForge-refactor-continuation-20260831` |
| 分支 | `codex/af-main-refactor-continuation-20260831` |
| HEAD | `28b0a246b31b49db0e38c9303a55150957b3403f` |
| MyBehavior 主文件 | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs`，29,840 行 |
| ShoutBehavior 主文件 | `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs`，21,301 行 |
| 受保护产品 dirty | `CourierDeliveryBehavior.SessionTransport.cs`；不顺带暂存、修改或清理 |

主文件 raw SHA256：My `53cc56b1034d828c5e859f437d896ff13a9cb977a3b1fa651b3b7969430de520`；Shout `95d71938fa037e8e842b441a4d9187c1f4c40a763afbdebb70f72117b67ea0f0`。SessionTransport raw SHA256 `a393a98b42c1f2d37150578d5d5ede170540be8eb4874751fbc2821eb9cfe23e`。

这是源码定位，不是本轮构建或行为验收。已核实项目 Compile 排除/显式包含规则、当前源文件、部分实际消费者、原始 J 目标与历史收据；**未声称逐行审完所有宿主成员或重新求值两个 API 的完整 Compile 集合**。H0 只补当前差集与未证范围，不重新从 J01 开始全仓审计。

### 2.2 已完成的东西不返工

- 上一轮 [P1–P8 计划](af2-host-terminal-closeout-20261002.md)的具名迁移，在[最终离线回执](../animusforge-refactoring-and-repository-reorganization-plan.md#af2-host-terminal-final-offline-20261002)中记录为 `OFFLINE_VERIFIED_WITH_LIMITS`；它没有把所有“保留适配”证明为永久终态。
- MemoryBusinessStateOwner、MemoryRecordRules、Recall owners，Weekly runtime/material/publication，Native/Scene turn/effects/history runtimes，SceneMovementController、SceneAudioLipSyncController、ScenePresentationController 等现有权威继续复用。
- 当前 `SanitizeDailyMemoryDrafts` 等已经转调 MemoryRecordRules；`TryBuildMemoryRecallCandidates` 已调用 MemoryRecallContextOwner。不能因为它们还在 partial 中就重写算法。
- 上轮 Roslyn 真类型规模是 My 48 partial / 2408 direct syntax declarations / 34642 member-span lines，Shout 29 / 1352 / 22219。它是上轮源码证据，不是本轮重新统计；同名前缀文件数也不等于真正 partial 数。
- 上轮 355 入口是 343 PASS 与 12 个非 PASS 分类，不是“全部测试通过”，更不能覆盖后来的人设、政策、外交等产品变化。
- 10 月 2 日[接入交付计划](af2-module-integration-delivery-closeout-20261002.md)明确后置宿主治理。本次恢复的是该后置目标；保留当时真实交付结果，不把当时未完成改写成当时已完成。

## 3. 终态架构：宿主到底可以留下什么

```text
MyBehavior / ShoutBehavior / 原 MissionBehavior
    ├─ 引擎入口、注册顺序、所属线程派发、owner 的构造/退役
    ├─ 原保存/ABI/反射身份的窄兼容门面
    └─ 具名能力调用
          ├─ AF.Module.*：规则、状态转换、请求/提交顺序
          ├─ AF.GameAdapter.Bannerlord：活对象捕获/执行、UI、引擎资源
          └─ AF.Persistence：既有文件/JSON/chunk 机制
```

### 3.1 允许保留与必须迁出的边界

| 可以留在宿主 | 不可以用该理由留在宿主 |
| --- | --- |
| 引擎 override、原事件订阅和具名 owner 调用顺序 | 整段周报、Prompt、交易、耐心、交互超时或导入算法 |
| 读取少量入口参数并提交到所属能力 | 反复扫描/组装整份人物、场景、军团或 roster 上下文 |
| `SyncData` 的域保存适配调用、必要原类型壳 | 以“保存兼容”为由保留业务字典的独立第二写者 |
| 原 public/internal/反射签名的窄转接 | 以“外部调用”为由保留整段执行器工厂与效果编排 |
| 构造、停止和组合各具名 runtime/controller | 换一个 UniversalOwner/SceneRuntime 把同样的大类整体搬走 |

活对象读取不等于必须写在 Behavior：完整事实捕获归具名 Bannerlord adapter，完整 UI/音频/注意力控制归具名 controller。它们可以依法持有游戏对象，但不能同时接管领域政策。领域 owner 后台只接 detached 输入；并非所有抽取都要后台化。

DTO/嵌套类型若有保存或 ABI 身份约束，可以保留原全名、字段及类型壳，甚至用同一 partial 组织兼容声明；**壳不再拥有业务算法和状态转换**。不能为缩行数改变类型全名，或把兼容声明数算成未迁算法数。

### 3.2 本次范围

- 两个完整宿主类型：主文件、全部真正 partial、嵌套运行类型及外部文件中的同类型声明；不是只处理两个 `.cs`。
- 这些职责的实际 Native/Scene/Courier、Onboarding、内部制作组、public V1、Harmony/反射/UI 消费者与 AF 侧适配。
- 复用 J01–J17 已验证 owner；只对实际调用证明仍有宿主业务的部分继续迁移。
- Policy/Gathering/GCCZ 和四扩展的独立业务不重写；只改获准 AF 接缝。不写外仓，不自动镜像 GCCZ。
- 不拆 DLL、不新增通用插件平台、service locator/事件总线/manifest、不改变默认入口/玩法/MCM/公开能力，不改一键流程。

## 4. H0：冻结完整剩余差集，防止再漏一截

H0 是一次有出口的接续准备，不是长期审计阶段。

1. 重新取 Git 根、分支、HEAD/dirty；保护原 SessionTransport、tools/NuGet、当前作者与资源变化。绑定当前实际候选，不使用旧 `dd6f45bf` 产物冒充现树。
2. 用已有 Compile 求值、Roslyn inventory、代码图与源码交叉核对两个完整类型；分别覆盖 1.3/1.4 条件编译。枚举字段、属性、方法、嵌套类型、任务/订阅、保存和动态入口。
3. 以[既有代码范围图](../architecture/af-framework-code-scope.md)和代码图的 `terminalResponsibilities` / `retainedHostDeclarations` 为历史导航，逐项对照当前方法体。原“retained-live/provider/UI”等标签不能自动豁免。
4. 每个成员唯一落入：`已真实迁移`、`H1–H7具名待迁簇`、`必要薄边界`、`纯DTO/身份壳`、`获准排除业务`。可复用证据须源码、消费者与依赖仍适用；未知、未审与不明用途不能划为薄壳。
5. 同时记录字段实际写者和反向回调：controller 是否只导航，是否调用 host 的完整 Import/Trade/Prompt 方法；代理属性后面的权威容器与调用方是否一致。
6. 最后发现的漏项必须落到 H1–H7 的具名子簇，写入口、状态、目标、依赖和出口；不留“其他适配/以后再拆”桶。若涉及新玩法、制作组内部或破坏性身份变更，单列需决策范围，不能暗中纳入或当完成。

**H0 交付**：在现有代码图/范围图下增量保存全成员差集与保留理由；主台账记录分母、重复去重和未证项。新机器清单只在确需机械对账时使用仓内 artifacts，不另起永久台账。

**H0 出口**：两个类型及其本次 AF 接缝无匿名剩余责任；每个待迁簇有消费者/状态/实施包。已迁部分不返工。H0 完成只等于范围冻结，业务仍未完成。

## 5. H1–H7：怎么实际拆

下列目标类型名未在现树存在时只是设计建议，不预先创建空类；优先扩展已读、职责匹配的现有 owner。源码锚点绑定编制 HEAD，执行按符号重定位。

### H1 — Campaign 状态、记录与保存适配收口

**现态**：My 主文件 `:1304–1330` 的记忆/行动容器已部分代理到 MemoryBusinessStateOwner，但 `_npcPersonaProfiles`、人物 storage、开局总结、事件记录与部分 revision 仍由宿主持有；`:25182` 的 SyncPatienceData 仍做完整 JSON/状态恢复循环。

**拆法**：

- Memory 的历史/行动/queue 使用现有 MemoryBusinessStateOwner 与记录规则；只把宿主中仍有的转换和游标归真正使用者，不再复制缓存。
- Persona profile、Weekly/event/opening summary、shown-record 等按现有 domain authority 分域收口。已有 state owner 能承载就扩展；确实缺失时才增加该域窄 store/owner，禁止一个 CampaignBusinessState 包全域。
- 域操作通过窄读写方法承接，当前引用/原地修改与净化语义保持。宿主代理字段逐步退为兼容接缝；实际 consumers 改接后才退出，无“双写过渡”。
- storage 字典与 JSON/chunk 归现有 Persistence adapters。耐心恢复/演算继续由 PatienceOwner/PatienceRules 管，不重写；提取其保存适配，原 `_patienceStates_v1` 键及坏项继续规则保留。
- 原嵌套 DTO 身份逐个核保存/反射/ABI；不整体改名迁走。记录构造、Sanitize、clone 与索引各归已存在权威，不把纯类型壳当算法。

**全消费者**：每日/事件记录、Prompt/read、导入编辑、外部记录 hook、SyncData、ClearAll、读档/退役与 Weekly/Memory 辅助提交。

**有限验证**：旧 JSON/chunk/默认值和坏项、引用/clone 行为、重载/清空/旧 generation、域恢复顺序；每状态只有一写者。频率沿原事件/请求/保存；不新增每日全表同步或每 tick 复制存档。

**出口**：宿主不再独立持有该域业务状态或执行完整状态恢复算法；保存身份/调用顺序保留，真实状态与保存来源一致。

### H2 — 人物/场景事实捕获与 Prompt 规则组装

**现态**：My `:15414` BuildHeroFactsForPersonaGeneration、`:17124` BuildNpcIdentityInfoForPrompt、`:21204` BuildPlayerArmyRuntimeFactForPrompt；`:20423` BuildExtraRuleInstructions 混合 live 资格、规则选择、替换与转介。Shout `:3559` NPC 机制列表、`:5455` roster 投影、`:6236` 场景地点、`:6806` 公共历史和 `:16379` 场景后处理规则仍有宿主逻辑。

**拆法**：

1. **主线程 capture**：人物/家族/关系/军团、地点、roster、Agent、机制资格和配置按用途成为具名 GameAdapter；一次请求共享已有快照与可复用 ID 查找，不生成“所有世界信息”快照。
2. **detached 规则与文本**：身份/称谓/条目排序/显示格式、额外规则选择/正文替换、场景机制规则去重和呈现归 Persona/Prompt/Conversation 已有 composer 或对应窄策略。
3. **保持实际效果时序**：Trust/Duel 等原有消费与领域资格仍留其实际 owner；捕获不能提前消耗状态。先区分只读资格与提交动作，不能把 live 函数全部塞进 DTO factory。
4. **三渠道接线**：Native/Scene/Courier，以及 ordinary/GCCZ voice、被动回复、系统 NPC、外部 Prompt hook 都使用同份所属能力；渠道只提供上下文，不各造身份/标签规则版本。
5. 已完成 PersonaIntroMessageComposer、PromptRuleInstructionComposer、SceneHistoryMessageAssemblyOwner 与 Recall algorithms 直接复用；这里只迁其剩余输入构造/宿主规则，不另起完整 Prompt 系统。

**有限验证**：Hero/非Hero、同家族/无家族、军团选中成员、地点/场景变化；AFEF 不占会话 cap，角色/顺序/当前输入去重保持；资格失败无漏标签，后台不回落 live 查询；原规则逐项次序与 scope 嵌套/异常保持。

**性能**：按请求/主题触发，记录不同 ID、Agent、roster entry 与历史行工作量；请求内共用快照，缓存绑定 mission/generation/来源，不能全局缓存失效身份。

**出口**：宿主不再决定上下文算法/完整规则块；保留原 hook 签名但转到具名 capture/composer，修改身份/格式/规则选择不再跨两个大类。

### H3 — LLM 调用方的配置、请求与结果适配

**现态**：My `:23890` CallWeeklyReportApiDetailed、`:23964` naming、`:24040` auxiliary、`:24116` CallUniversalApiDetailed；Shout `:13410` CallNativeConversationApiAsync。已有 LegacyConfiguredChatGateway 等传输 owner，不是重新做 HTTP。

**拆法**：

- 路由/模型/token/temperature/thinking 配置捕获归既有 Configuration/调用能力适配；每次请求冻结需要的配置，凭据只在受控传输能力内取得，不进入公开 DTO、日志或计划附件。
- 调用方的 request/trace 构造与 ApiCallResult 映射归具名 LLM application adapter；Weekly、naming、辅助和 Native 的预算、重试、fallback、提示、可见输出继续由各自调用 owner 决定。
- HTTP/SSE/cancel/timeout 继续消费既有传输；保留 stream 与非stream、限速/配额/RetryAfter、thinking fallback 及 stale result 的既有差异。
- 不把 CancellationToken.None 批量改成强取消，也不把“迟到丢弃”冒称“网络已取消”；只迁责任，新的取消语义另行明确。

**全消费者/验证**：主回复、周报、命名、辅助、Native streaming；成功/空回复/截断/错误/限速/部分流/取消/旧档结果，并核最终展示和失败分类。复用配置网关回放，无真实网络依赖。

**性能/出口**：每 attempt 不重复解析配置/构造重复无用快照；不新增后台轮询。两个宿主不再拥有 provider 配置解析或整段 request/result pipeline，领域策略不被通用传输吞并。

### H4 — 喊话输入、目标选择与给予/展示/交易交互

**现态**：Shout `:1684–1707` 持 trade options/pending/mode/目标快照/完成回调；`:13509` TriggerShout、`:13681` TryPrepareShoutTarget、`:14055` BeginShoutTradeFlow、`:14134` BuildShoutTradeOptions、`:14416` OnShoutTradeChatConfirmed、`:14593` ApplyShoutGiveTransfer、`:15215` fact text、`:16959` reset；ScenePresentationTrade partial 与 DialogueUI 也消费同套宿主逻辑。

**拆法**：

- 输入/框选/快捷键/菜单由具名 Scene input/target controller 负责，热键与进入菜单的默认行为不变；不是把整个输入控制塞入 Presentation owner。
- trade controller 持**唯一** transient options、pending item、mode、目标 snapshot、数量选择、action-only 完成与 staged 生命周期。单次弹窗和持久会话面板共用它，不保留两份 stage。
- 控制器调用现有 Economy eligibility/projection/execution 与 PartyAssetTransferBannerlordAdapter；不复制金币/库存/领地真值，不改 ALL/索引/资格规则。
- UI 展示与选择先构造不执行的计划；确认后由当前权威 effect/receipt 执行。保留现有实际执行/历史/后处理时序，已发生部分效果不能退回菜单重试世界动作。
- 事实文本由所属执行结果/Conversation 投影构造；必须保留 source-only/partial/unknown 与不成功事实。原对话 UI/static hooks 保持签名，内部改接 controller。

**全消费者**：轮盘、直接输入、单次给予/展示弹窗、持久 Scene give panel、Native action-only、道具/部队/俘虏/固定资产与信件显示。

**有限验证**：取消任一步、切目标/非Hero Agent 消失、数量不足/过期索引、自己/非法领主、重复确认、部分扣除、无回复 action-only、离场后旧 UI 回调、两种 UI 同时重入不双 stage。

**性能/出口**：候选只在打开/实际失效时构造，不每帧扫描库存；确认重验当前目标/量而非信任显示项。宿主没有 trade 状态机，UI 不成为第二个资产执行者，正常窗口链完整接通。

### H5 — 注意力、超时、自主行为与发言后续/Native wait

**现态**：Shout `:1932` `_activeInteractionSessions`、`:1962` timeout arms；`:18561` group idle、`:19386` refresh、`:20061` timeout、`:20404` expire 与 stare/移动抑制/自主恢复共用状态；Mission tick `:323–405` 逐帧驱动。`:2949` Native TTS/wait 仍是完整兼容适配。

**拆法**：

- 会话 timeout/arm/token/group release 策略归 Conversation 的具名 interaction lifecycle；主线程 Agent 朝向、注意力 hold、自主恢复与安全上下文归具名 Scene attention adapter/controller。
- 复用 SceneMovementController 和 Presentation/Audio owners，**不迁第二份 guide/summon/audio 状态**。明确“谁持 interaction、谁持移动任务、谁持音频”及各自释放请求。
- speech completion 后的 halls/meeting/worldmap 退出采用具名 follow-up coordinator 调领域 capability；禁止用音频 ended callback 随意访问旧宿主全状态。
- Native engine wait/fallback 归具名 adapter；复用原 TCS/claim/音频 identity，保留目标、conversation token 与接受门，不创建另一套 waiter。
- 原 Mission override 只按原顺序调用组件、保持 early-return 与异常隔离；会面、和平/战斗、跟随/带路、音频暂停等条件逐个保持。

**有限验证**：arming 延迟与旧 token、多人 grouped timeout、超距、Mission/save 换代、跟随/传唤期间不误释放、Hostile/meeting 自主逻辑、玩家倒地、重复 speech ended、旧 Native waiter 不清新音频/暂停/TCS。

**性能**：当前 timeout 对每 active session 做 Agents 查找，是明确工作量观察点。先记录现频率/active数/Agent数；迁移不得更差，可用同 mission 索引/一次扫描与失效维护减少重复查找。任何分批都必须保持到期/组释放与原顺序，不能以跳过检查优化。

**出口**：完整状态/clock/cleanup 在真正控制器内；宿主没有 attention/timeout/native-wait 状态机，Mission end 与读档只调用各 authority 的退役入口。

### H6 — 社交结果、动作工厂与 AF 侧跨域接缝

**现态**：My `:24848` ApplyPatienceFromHeroResponse 仍组合耐心变化、关系/私爱效果与日志；`:2499` RP crafting、`:21904` siege hook。Shout `:11282` Native 和 `:11649` Scene executor factories 仍解析目标/构造捕获/guard/领域执行组合。

**拆法**：

- 耐心算法/状态保留 PatienceOwner/PatienceRules；Social application responsibility 持“计算→关系/私爱叶效果→观察结果”，GameAdapter 执行 Hero/关系操作，Prompt 只投影耐心文字。
- 动作工厂归具名 Conversation/Actions composition adapter，主线程冻结真实上下文与来源，复用原 family executors、domain ports 和现有 claims/receipts。
- RP/攻城/部队/拘押等 hook 只保 ABI 门面；完整 AF 观察、文本转换、唯一记忆提交归所属 record/bridge/application responsibility。制作组规则继续调用其真实 port，不搬进 AF 基础设施。
- 不统一成新的 AllActions executor，也不新建一套“内部 API 动作核心”。三渠道和 internal/public 请求最终进入原同一个效果/AFEF 权威。

**全消费者/验证**：默认与公开动作入口、三渠道后处理、制作组 typed 接缝和真实反射；未知标签/资格拒绝、目标换代、执行前取消、执行中部分副作用、记忆失败/回调失败不重复执行，耐心归零与原关系规则保持。

**性能/出口**：每 request 构造必要 factory/capture，guard 复用真实来源，不重复全场景枚举。两个宿主不再编排业务效果；保留原签名/类型/null/失败协议，不能把 NotSupported 或部分结果改成成功。

### H7 — 导入导出的具体格式适配与剩余编辑器流程

**现态**：不是只看主文件。`MyBehavior.ImportExportUi.cs:61–115` 的 controllers 仍回调宿主大量 Import/Export；`:161` 单人 Persona、`:350` 单人导入、`:972` event map、`:2085` Knowledge 导入等仍包含目录解析、枚举、数据准备、发布与提示。已有 UI controller 不证明这些具体流程已迁完。

**拆法**：

- UI controller 保目录选择、页码、确认与反馈；各域已有 import/edit owner 保校验、合并/覆盖、真实状态写入。
- 具体文件格式/包路径/单人定位/发布由对应 Persona/Memory/Weekly/Knowledge 等导入导出 adapter 承接；复用 PlayerExportsStore/NpcDataFileName/原 codec，不做新的通用数据库/格式迁移。
- 跨域 package controller 只保原有顺序和部分成功协调，不回调整个旧宿主业务；需要游戏身份的文件名/目标解析用具名主线程能力。
- 清晰保留 `All`、`HeroNpcAll`、`DialogueHistory` 等旧格式范围差异、覆盖/跳过、原地净化及非事务语义；DatabaseReload 的既有 rollback 与普通导入不可合成假事务。
- 同包检查 Onboarding、设置弹窗和开发编辑器的实际 callbacks；已有真正 controller 原样接线，不按文件名整包重写。

**有限验证**：合成目录/坏 JSON/重复Key/缺文件/单人定位/确认后换档/取消/部分域成功；旧 schema 与导出文件集合、发布原子边界、reload 回滚、提示真假保持。发现真实反馈缺陷先具名复现并明确批准变化，不无声改语义。

**性能/出口**：手工冷路径记录文件数/不同 Hero数，域/请求内复用定位结果，不逐文件重扫全部人物。UI 不扫描/写业务表，宿主没有完整文件包算法，所有实际 controller 回调已变为窄 domain/IO 能力。

## 6. 推荐施工顺序与接线纪律

**默认单执行线**：`H0 → H1 → H4 → H5 → H2 → H3 → H6 → H7 → H8`。先固定 authority，再做完整交易交互与场景生命周期，随后收 Prompt/LLM/动作/文件包，最后宿主组合与总验收。H7 可以在 H1 完成后提前，但 H8 必须等所有范围内簇闭合。

- 每包先冻结有限出口，直接完成“状态/算法→具名 owner/adapter→全部消费者→原实现退出→验证”；不按每个 helper 新开一轮。
- 不预设代理或新工作树。用户后续明确授权并行时：独占簇才并行，一文件一写者；唯一集成人负责两个主文件、共享契约、索引和最终构建。普通独占实现/测试仍由各执行者完成，避免集成人二次写整包接线。
- 可用过渡 partial 暂时隔离交错簇，但所属包关闭前必须成为真正实现/窄壳；不得作为最后留给 H8 的等量业务堆。
- 窄 port/delegate 允许，但其 body 是叶捕获/效果/调用；若回调 `BuildAllContext/ImportAll/RunWholeTurn` 等完整旧业务，须继续拆。依赖判断看实现，不只 grep My/Shout 名字。
- 所有 setter、字典、TCS、锁、subscription、资源 lease 有一个 owner。迁移阶段共享原 authority 并转移唯一写者，不复制再同步。
- 保留旧 ABI/反射/保存门面前，列实际消费者、理由和 body 限制；无消费者/身份责任的旧完整实现只按确认范围精确移除。没有批量删除/清理授权。
- 产品实施先做本地 intent/checkpoint，验证完整切片后按精确文件提交；不混 SessionTransport、其他作者工作或缓存，不改写历史。本文编制阶段不执行这些提交。

## 7. H8：把两个宿主真正收成组合根，并对账 J 系列

H8 不承接前包未迁完的匿名算法；它只做组合、保留身份核验与同候选出口。

### 7.1 全类型覆盖与旧实现退出

- 重新核两 API 的两个完整类型与消费者，逐字段/方法/类型对账 H0；不漏 `WeeklyGenerationModels.cs` / `WeeklyLegacyDtos.cs` 这类非同名前缀中的 MyBehavior partial。
- 终态每个宿主成员只能是具名引擎/组合/身份/窄兼容边界或纯 DTO；完整域算法、业务可变状态、UI/交互/导入状态机全部归独立组件。
- 把当前 host→owner→host 的反向调用逐条解释；窄叶能力可以存在，完整旧业务回调不能关闭。
- 所有默认/公开/反射/XML/测试/保存消费者改接或保留签名；不以“静态调用为零”删除动态入口。`GetPassiveNpcResponse` 等真实反射契约继续核对。
- 各组件由原 Campaign/Mission 边界装配与退役；不改变 Behavior 注册/保存身份、引擎回调顺序、默认渠道入口和 Bootstrap 加载方式。

### 7.2 可维护性验收，不只看行数

每类改动各选一项**只读影响分析**，列“现在改哪里/为何不用改两个宿主/应重验什么”，不为演示真的改变玩法：

| 代表变更 | 应归属的修改面 |
| --- | --- |
| 身份/人物事实文本或额外规则排序 | Persona/Prompt composer + 必要 capture schema |
| 给予选择/数量交互和提示 | trade controller/UI projection，真实 Economy 执行规则不复制 |
| 超时/group attention 释放规则 | interaction policy/controller，移动/音频 owner 不被重写 |
| provider 结果/限速映射 | LLM application adapter，Weekly/Native 策略仍各自持有 |
| 耐心与关系结果的组合 | Social application + game effect leaf，不回到 My 主文件 |
| 导入一个现有域的数据格式 | 所属 import/export adapter + domain validation，不改万能宿主 |

宿主只有必要新能力接线才允许变化；如果一次普通规则修改仍必须在两个主文件找算法/修共享字段，本项失败。新类若聚合了不相干的完整业务也失败。

输出实际主文件行数、真正 partial 成员/跨度、各 owner/controller 规模与职责分布。**不设任意 5000/10000 行门槛**；若只有主文件变短、真正宿主业务规模没变或形成等量万能 owner，判失败。兼容 DTO 规模单列，不能用它掩盖活算法。

### 7.3 与 J01–J17 夙愿对账

- 复用原 20 桶及 J 责任记录，增量对照本次变更/保留边界，不重做已验证且未变的领域。
- 每项区别：真实迁移、必要身份边界、获准制作组排除、未开放设计目标、尚待源码/运行验证。排除或延期不算实现。
- 本次两个宿主及具名 AF 接缝达标，只可先记 `HOST_RESPONSIBILITY_OFFLINE_COMPLETE`。
- 只有 J 系列获准 AF 范围内无未分配成员/未迁业务、全部消费者/必要离线证据闭合，才记 `J_SOURCE_OFFLINE_COMPLETE`。若别的 AF owner 有确证余项，具名列出并决定有限接续包；不能凭两个文件瘦身宣称全 J 完成。
- `J_USE_ACCEPTED` 必须另有声明游戏版本的实机/代表性旧档/真实依赖证据；发布/部署是另一个状态，均不由源码完成推出。

## 8. 验证与最终交付

### 8.1 每包的最小可信验证

1. 基线/新实现有相同输入与应保持行为；真实 owner/state/实际调用链被测试，不只比较名字/hash。
2. 状态簇必须有取消、重复、迟到、owner/generation/目标变化与部分副作用反例；纯文本包不复制整套异步测试。
3. 相关公开/反射/JSON/key/默认值与域顺序有兼容证据，游戏叶/网络/UI 替身边界说明清楚。
4. 产品切片按原流程验证两 API；最后同候选 Debug/Release × 双 API + Bootstrap。固定输出串行，独立输出才能并行。
5. 不降低断言、不刷旧 review hash 消除失败、不把编译失败算 mutation 拒收；既有失败与当前回归分开，相关失败不能靠换分类通过。

复用现有入口：Prompt 的 PersonaIntroCapture / ExtraRule / Composition，Memory 的 Recall / J17ImportExport，Economy PartyTransfer，Social PatienceOwner，Conversation ChannelHistoryRole / NativeTurn / SceneSpeechExecution / NativeWaitAudioBoundary，以及 TeamModulePortParity 的 actual adapters。需要补 whole-consumer 覆盖时优先扩展对应已有 runner，不给每个方法新建 fixture。

### 8.2 必须补的三条整体边界

历史关键原子/owner 通过不替代以下当前整体执行路径。H8 中需要绑定真实消费者，必要底层网络/游戏叶可替身，但调度、开始/接受/退出不能只抽方法体：

| 路径 | 这次要证明的闭包 |
| --- | --- |
| Courier PromptSchedule 与终态 | 实际 session→capture/routing/complete→后处理→到达接受；预生成不提前提交，取消/迟到/部分结果不二次效果 |
| ModuleNativeSubmission | 真实 internal/public request→context/ticket→调度/权威执行→结果/取消；三渠道公开能力与默认路径不分裂 |
| NativeWait whole consumer | 实际 Native 回复→engine wait→TTS/presentation 接受/退出；旧回调、重复 finish/fallback 不污染新会话 |

这些是既有明确盲区的有限补证，不重写完成的渠道核心，也不等于真实网络/实机已验。受保护 SessionTransport 如确实需要产品修改，先明确精确差异与原 dirty 归属，不自动接管。

### 8.3 工具、写入与同候选约束

- 最终入口使用当前 `tests/run_all.py`；`--ids` 接收 ID 文件，不是逗号字符串。各 runner 的默认路径、当前 SDK/引用、写入/清理范围开工时核实；原 355 数量不固定为新分母。
- 非破坏构建 wrapper 是 `docs/handoffs/j17-offline-build-gate.py`，参数 `--configuration Debug|Release --output-parent <本轮仓内全新输出父目录>`；使用前再次读其实际原脚本适配/路径，不改一键构建行为。
- `AF_TEST_TEMP_ROOT` 等若要求仓外新合成根，另取精确路径授权；不继承历史 TEMP 权限、不绕 DataPaths、不读写真实 PlayerExports/旧档/音频。
- SDK 初始化按现有安全环境禁证书自动生成，不安装 SDK/插件，不做全局环境配置。记录实际版本和依赖，不抄历史 G:/F: 命令。
- 将最后代码、配置/资源/引用输入和 DLL hash 绑定同一候选，改变相关输入后重验；不拼旧候选成绩。代码图 recorded/working-tree 只证定位，与行为分别记录。
- 收据保留每入口 PASS/FAIL/NOT_RUN/BLOCKED/有证据的历史分类。必要当前入口失败不关闭；无关业务工具缺输入不伪 PASS。任何总体成功都附非 PASS 明细。

### 8.4 实机、旧档与权限

源码/离线出口之后，在另获授权的确切 1.3/1.4 测试环境验证：载入、三渠道正常/动作/记忆、双 NPC 与旁听、发送到回信、交易两种 UI、带路传唤/归位、超时/关窗/离场、TTS/输入焦点、跨日周、save→reload 与一份代表性旧档副本。沿这些流程观察实际帧成本，不用冷路径微基准代替。

缺环境或旧档就留 NOT_RUN，不关闭功能缩样本；不操作玩家原档。推送、Stage、打包、游戏覆盖、仓外写入、批量移动/删除、系统变更分别授权，历史交接与本计划不授权它们。

### 8.5 最终交付清单与停止条件

最终只交一套：

1. H0 全成员差集的终态对账：旧 symbol→所属 owner/adapter→全部 production consumer→唯一 state/lifecycle，未迁/未审业务为 0；身份壳有理由。
2. 两个宿主全部保留成员的用途/实际消费者/身份约束与限制，无“其它适配”大桶；所有过渡 partial 已退出活业务。
3. 每包有限行为证据与可维护性影响分析、真实类型/owner 规模、性能频率/工作量和未验层。
4. 同候选构建/受影响与最终门禁、存档/API/反射身份核验、三条整体盲区补证及非 PASS 明细。
5. 主台账/代码范围图更新，HANDOFF 只留摘要；完整命令/log/hash 留本地 artifacts，按 verified slice 提供可逆恢复提交。

**达到对应出口就停止，不因为还能拆 helper 无限续包；但不得把“责任已分配”“目录归位”“工作包结束”“测试退出0”当成以上出口。** 如中途用户暂停，记录已迁与未闭簇，不降低验收口径、不将本计划目标再次默默后置。
