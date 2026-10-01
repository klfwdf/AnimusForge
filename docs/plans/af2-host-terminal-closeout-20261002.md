# AF 2.0 大宿主终态收官：源码核查后重写

日期：2026-10-02。状态：PLAN_READY / IMPLEMENTATION_NOT_STARTED。

本文替换本文件上一版。上一版只抽查少量方法，就把全量差集留给未来 T0，并先定包数、行数门槛，依据不足；这些安排撤销。旧 J、上一轮 F1–F5/F5d 成果不撤销，也不重新施工。

本次只改计划，不改产品、不执行测试、不调度代理、不操作其他会话或 Git 索引。按用户要求，当前测试补齐视为规划输入将完成，不再列成新重构欠账；这不表示本次已经运行或验证通过。

## 1. 核查依据与证据限度

- 工作区 `E:/AnimusForge-refactor-continuation-20260831`；分支 `codex/af-main-refactor-continuation-20260831`；初始读取 HEAD `9488c9be`，计划复核结束时为 `9cff3ee8`；两个主文件 SHA256 未变。
- 实际读取了两个主文件的状态、规则、生命周期、保存、导入及生产调用；枚举 24 个 MyBehavior 同名文件、28 个 ShoutBehavior 同名文件的长度、声明与 owner 引用；对关键 partial 和新 owner 进一步读实现。另核对主项目 Compile 排除规则、既有责任图和相关测试入口。
- 这不是逐行审完约九万行，也不是编译器全成员语义证明。下表是已经读到实现和消费者的确定工作，不再让执行者从零发现范围。历史责任图仅用于补齐导航，不把历史 OPEN 自动当成本次缺陷。
- 两主文件本次读取 SHA256：MyBehavior `92aa0b712202a9ecd5c9a7a72ad25cf24a72311ce33c581a7da034fe966e7eb8`；ShoutBehavior `ab04ace8adac26c494821dcf936b2106841663ec85028180e1696c55dd6c4e05`。实施前对照其他会话最终候选的差异，不重复全盘审计。
- 当前 runner/readiness/测试改动、原有 Courier SessionTransport 修改及未跟踪目录全部保护。本计划不暂存或提交，避免干扰唯一集成人。

### 1.1 规模不是猜测

| 对象 | 上轮前 320c1aad | 当前实际文本行 |
| --- | ---: | ---: |
| MyBehavior.cs | 52,401 | 49,851 |
| MyBehavior 同名文件族 | 58,682 / 24 文件 | 53,292 / 24 文件 |
| ShoutBehavior.cs | 37,192 | 33,900 |
| ShoutBehavior 同名文件族 | 44,217 / 27 文件 | 40,502 / 28 文件 |

同名文件族不是严格的类型成员计数，其中有辅助类型；不把它伪装成 Roslyn 统计。删除空行、挪到同一 partial、换成另一个万能类都不算职责交付。

### 1.2 已完成部分：只接线，不重做

| 已核实实现 | 保留决定 |
| --- | --- |
| [MemoryBusinessStateOwner](../../src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs) 持 drafts/blocks/overview/queues 并执行 ApplyDaily；[宿主 4852](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L4852) 实际调用 | 不再拆摘要成功提交、另建状态表 |
| [MemorySummaryPlanning partial](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryPlanning.cs#L7) 与 [Sealing partial](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySealing.cs#L9) 已转调模块，只注入预算/时间/资格能力 | 保留或随组合根收拢，不能当未迁算法重新实现 |
| [MemoryDeveloperEditOwner](../../src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.cs#L11)、[MemoryImportExportOwner](../../src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs#L61) 实际改权威容器，宿主 42923/46176 接入 | 剩余是 UI/跨域导入编排，不是再重写 Memory 编辑核心 |
| [公共信使后处理门面](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L21307) 已转交 ConversationActionPostprocessOwner | 不重复上轮公共 parser/normalizer 抽取；本次只消除剩余宿主上下文依赖 |
| [NativeConversationTurnCoordinator](../../src/modules/AF.Module.Conversation/Channels/Native/NativeConversationTurnCoordinator.cs#L9) 已负责四阶段顺序 | 复用这个顺序，不制造第二个 turn sequencer；处理尚嵌在 Shout 的 turn 实现 |
| [SceneSpeechQueueOwner](../../src/modules/AF.Module.Conversation/Channels/Scene/SceneSpeechQueueOwner.cs#L10)、[ScenePendingAfefFactsOwner](../../src/modules/AF.Module.Conversation/Channels/Scene/ScenePendingAfefFactsOwner.cs#L11) 分别持队列租约、待注入事实 | 保留这些真正 owner，但不能把它们解释成整条发言/历史链已经迁完 |

## 2. 实际未闭责任清单及设计决定

以下 P1–P8 是责任包，不是八个新大类。位置是目标归属；新类型仅在现有实现无法承载时增加。行号绑定以上源码，执行按符号重定位。

### P1 — Campaign 事件材料与周报触发提交

**证据。** [政治原因 9292–9451](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L9292) 在宿主选择材料、识别核心原因、按词类排序；宣战 7038、议和 7090、政策 7133 等生产调用。不是单纯读游戏事实。

[MarkWeeklyMemoryMaterialTriggerInternal 12839](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L12839) 决定立即挂接或 pending；12907/12929/12966 管去重、会话匹配、过期。24024 的实际记忆追加调用 AttachPending。1357 的 pending 属性已经别名到 MemoryBusinessStateOwner，所以问题是**状态归属已迁但转换操作仍散在宿主**，不是缺一张新表。

[WeeklyActionOutcomeReceipts 302](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyActionOutcomeReceipts.cs#L302) 仍决定跨日落在哪个 draft、等待重试、精确 trigger 检查及写入；现有 PublicationOwner 不等于这一提交簇已经离开宿主。

**实施。** 事件监听/活对象采集留游戏适配；事实文本分类、优先级和材料构造归 Weekly/Materials。pending 挂接、去重、期限和 draft 变更由已有 MemoryBusinessStateOwner 的窄操作承接；Weekly 发布者调用这个操作并保持原 receipt 顺序。WeeklyActionOutcomePublicationOwner 继续持发布机制，不新增并行账本。事件/近期行动记录的构造、索引和去重随同一事实流归其现有记录模块，不能留在 MyBehavior 当未归属尾部。

**全消费者/出口。** Campaign 事件、直接触发、每日追加、恢复发布以及 Weekly 材料回调共用真实实现；宿主不再直接遍历修改 pending 列表或计算材料优先级。

**验证。** 同日重复、不同 scene/native 会话、无会话、两天过期、旧日已压缩、新日 draft 未就绪、发布部分失败；复用 WeeklyMemoryMaterialOutcomeContractTests，补实际挂接算法对照。运行频率按事件/追加/恢复 tick；保持原有限 tick，不借抽取增加每帧全扫描。

### P2 — Memory 召回、历史检索与恢复后的跨域通知

**证据。** [TryBuildMemoryRecallCandidates 29185–29279](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L29185) 直接做 embedding 获取、向量点积、排序/截断/显示序号；29827 是实际调用。少量候选走日期顺序，超过上限 embedding 不可用会阻塞，**不是允许静默回退**。[FindRelevantArchiveHits 28605](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L28605) 另有多意图历史检索，不能只移一个排序函数。

[MemoryRecovery 270](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs#L270) 仍迭代 component、判 Daily marker、去重并调用 Notoriety receipt；H marker 不能冒充 Notoriety 成功。

**实施。** Memory 内承接 query/candidate/selection/context 整簇；复用 ONNX engine、现有 snapshot、RunOwner、RecoveryStateOwner。主线程捕获 Hero/历史快照及显示错误，脱离游戏对象后的检索由模块执行。恢复后的跨域通知由具名提交协调职责承接，Notoriety 仍是原权威；不把玩法吞进 Memory。

**出口/验证。** 候选小于/等于/大于上限、相同分数 tie-break、缺富标题、全部 embedding 失败、预处理选 ID 无效、取消/来源变化；验证完整 context 而不只候选数。恢复重复与通知不可用不重复游戏动作。检查每请求扫描量和缓存调用，不新增每 tick embedding，不以 Take(N) 冒充输入预算。

### P3 — 部队/俘虏/志愿兵与领地转移的完整执行边界

**证据。** [TryApplyPartyTransferTagsForExternal 22612](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L22612) 在 MyBehavior 判资格、读四种快照、解析 ATT/ATP、夹取数量、实际转移、统计部分结果及事实。Scene 10249/10325/23659/23736、Native 17982/18061、[Courier DomainCommit 120](../../src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs#L120) 均消费。21171 的税收/收入调用则确实属于 live 捕获，不应强行改成纯领域算法。

**实施。** 与现有 Economy/Authorization、Execution、Projection 归并：资格决策、请求/数量解析、结果分类归模块；roster/Settlement/Clan 的读写归具名 Bannerlord 适配。Prompt 列表索引与真实执行使用同一快照契约。领地/资产投影同簇处理，绝不整体搬到 RewardSystemBehavior 继续堆大类。

**出口/验证。** 现有公开静态门面保留签名但不再执行核心；三渠道同一执行路径。ALL、部分库存、野外非 Hero 权限、志愿兵、俘虏、索引过期、源扣后目标写入失败、实际数量/价值及 AFEF 一致。不能将异常返回 false 解释成零副作用，不能借重构改经济规则。

### P4 — Prompt 上下文、场景历史与消息构造

**证据。** [CapturePromptSections 27742](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L27742) 混合实时资格、模块决策和效果时序，已用 PromptContextDecisions，不能整体当纯函数后台化。原注释明确 Duel 消费次序影响 Reward/Trust。

[KeepAfefFactsAndRecentHistoryLines 9276](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L9276) 实现只截会话、不截 AFEF 的策略；[BuildStrictSceneMessagesForNpc 29940](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L29940) 合并历史、消费 pending、构造角色消息。除主文件 25727 外，SceneConversationChains 164/325/738/1474 的群聊/被动/即时反应也调用。

**实施。** 复用 MainPromptMessageAssemblyOwner、ConversationRoleClassificationOwner 与现有 Prompt capture/routing/retrieval 分层；共同文本与角色语义归 Prompt，Scene 历史容器及会话级消费归 Conversation。快照提供者捕获活对象和距离，消息构造不反查 Shout 私有字段。NativeConversationSessionOwner、Memory 持久权威均不复制。

**出口/验证。** 所有上述消费者及 Native/Courier 同类投影完成接线；AFEF 保留、当前输入已记录/未记录、历史上限、重复序号、旁听角色、消费一次、重试/失败是否重放保持原行为。复用 ChannelHistoryRoleFixtureTests 和 NativeHistorySnapshotTests，按新 owner 更新测试入口，不能只更新源码 hash。

### P5 — Native/Scene 回合及发言执行生命周期

**证据。** [NativeTurnHost 19](../../src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs#L19) 是 Shout 的嵌套 partial，持 `_owner`、admission、目标、提示词/回合状态；NativeTurnCommit 仍执行准备后处理、主线程完成和提交。已有 Coordinator 只管四阶段推进。

[RunSpeechQueueWorker 23514](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L23514) 仍负责完整队列消费、异步等待、主线程游戏动作、历史/发言与完成回执。SceneSpeechQueueOwner 自己也明确只管队列和 worker lease。[ApplyNativeConversationActionTags 17868](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L17868) 仍做跨域动作分发。

**实施。** Native 回合上下文/执行体成为独立渠道实现，复用 admission/session/coordinator；Scene 群聊/单人/被动/即时反应以及队列 payload 执行成为独立会话运行实现。游戏能力通过窄端口注入，不能让新类保存 ShoutBehavior 并回调整段旧业务。公共 Actions 执行、receipt、后处理 owner 不重写，渠道只管自己的顺序和接入。

**出口/验证。** 所有 turn partial 与 SceneConversationChains/ScenePostprocess 纳入，不只移主文件；只有一份任务、闸门、完成源与租约。验证 primary-first、relay、旁听、处理时排队、晚回包、任务取消、mission 结束、Native 退出后执行、每次效果/历史至多按原协议提交。保留原非事务失败语义，禁止以重试重复效果。

### P6 — 场景命令运动与会话后续动作

**证据。** [StartSceneSummonBatchAction 27028](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L27028) 不只是 Agent 调用：操作 follow-return 状态、取消旧 guide/summon、创建批次队列、发布 active 状态及失败回撤。27025 为真实入口。相邻召回/带路/门代理/归位方法共用这套状态，不能按函数平均拆。

**实施。** 在 GameAdapter 场景适配层形成一个完整运动控制器，持 summon/guide/follow/return/session 状态、目标解析、计时及清理。规则可用已有 SceneActions；禁止重写成坐标目标系统。Shout 只提交具名命令并接收结果，发言完成后续动作走 P5 的明确回执。

**出口/验证。** 嵌套请求类型随内部 owner 迁移，公开/反射身份必要时留门面；一套 tick/mission-end/reset。按仓库场景移动案例验证 Agent/LocationCharacter 目标、跨门、连续传唤、取消/重新命令、目标消失、带路到达后归位，不回归以裸坐标为主要目标。热点不得新增全 Agent 查找。

### P7 — 场景会话呈现、音频/口型资源与 UI 接缝

**证据。** [ScenePresentationSession 34](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePresentationSession.cs#L34) 持成员/active/version 等会话状态；[Runtime 15](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePresentationRuntime.cs#L15) 每 0.1 秒处理使命切换、玩家倒地、伤害收起及范围变化，由 MissionTick 615 调用。这五个 Presentation partial 不是已经独立的控制器。

[CleanupSceneLipSyncAfterPlaybackFinished 11425](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L11425) 持锁摘除 SoundEvent/wav/xml/安全脱离状态；10886/11388 回调调用，12027 StopAll 与 mission/安全切换共同清理。不能只移 TTS 播放方法留下资源字典。

**实施。** 同一包分两个独立 owner：会话呈现控制器、场景音频/口型资源控制器；不混成万能 SceneRuntime。复用 ScenePresentationPolicy、TtsEngine 和 UI 层。公开 hooks/轮询版本、DialogueUI 消费签名不改；内部状态全部单点持有。

**出口/验证。** 五个 Presentation partial、TTS 订阅/自然结束/主动停止/延迟清理/读档退役一起闭合。10 Hz 与逐帧纯读语义保持，重复结束幂等，旧回调不能清掉新音频；只处理已持有资源，不扩展临时文件删除范围。本计划不授权真实音频文件清理操作。

### P8 — Campaign 保存适配、开发编辑器、跨域导入与最终组合根

**证据。** [SyncData 16154](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L16154) 仍集中各域保存和恢复，包含原键、chunk、逐项坏数据处理；这些是兼容边界，不应把整段当纯领域算法。[DeleteDevCompressedMemoryBlock 42915](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L42915) 已调用真实编辑 owner，剩余是加载、展示和导航。

[ValidateUnnamedPersonaKeysForImport 47815](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L47815) 判重复/已有 Key；46673/48668 调用。[ImportAllData 48905](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L48905) 跨域准备与导入；不是 MemoryImportExportOwner 一个类就能替代。主文件约 39,600 行以后大量是这些管理 UI/数据操作，不能再只盯前半段规则。

**实施。** UI 按现有 GameAdapter/UI/Editors、Persona、Weekly 等目录建立具名控制器，选择/页码/确认状态跟 UI 走；各域数据校验与提交仍用其真正 owner。跨域导入用一个有明确顺序的协调器，不建第二份活状态、不引入新事务语义。保存入口/保存类型标识留原 Behavior；按域提取保存适配，原键、chunk、JSON 形状、坏项继续规则不变。最终事件注册/退役/组合接线归 MyBehavior/ShoutBehavior。

**出口/验证。** Onboarding 和外部反射入口改接或留兼容门面；坏 JSON、重复 Key、部分域导入失败、覆盖前确认、取消、读档重置、旧任务迟到、清空后恢复 marker 保持原语义。Memory 编辑/导入算法不重复实现。J17ImportExportTests 已有固定产物目录，归集成人单次运行，不能多人同时覆盖。

## 3. 全文件家族如何交付，不再漏 partial

| 文件簇 | 本次决定 |
| --- | --- |
| MyBehavior 的 Planning/Sealing/Fingerprint/Run/FailureNotice | 已有 owner 复用；预算/时间/线程能力与组合接线由 P8 收拢 |
| MemoryRecovery、SummaryInput/MainThread、HistoryPromptSnapshot、DialogueHistoryCommit/Delete、ExecutionMemory、SourceWrites | P2 负责剩余语义与提交协调；P8 负责活对象、保存与生命周期适配。已转发的算法不重新实现 |
| WeeklyActionOutcomeReceipts、WorldBulletin/WorldBulletinNpc/WorldBulletinPanel | P1 收剩余材料/提交；已有 Bulletin owner 保留；真实 UI 进入 P8，不能新建重复 Bulletin 状态 |
| PersonaGeneration/PromotedPersonaGeneration/PersonaReadiness、CivilWarPolitics | 已有 Persona/Kingdom owner 保留；P4/P8 接管人物快照、适配上下文与组合，不重开规则 |
| 所有 Native turn/admission/pending/prompt/action/completion partial | P4 历史/消息；P5 回合状态与执行；必要外部门面仍在 Shout |
| SceneConversationChains/ScenePostprocess/ModuleScene*/SceneActionDirective | P4/P5 主链和执行接入；P6 接运动命令；不是只移动主文件方法 |
| 五个 ScenePresentation partial | P7：Api、Round、Runtime、Session、Trade 与相关文件一并形成真正控制器；交易真实效果仍归 P3/原 Economy |
| 两宿主 CampaignLifetime、公开 DTO/Saveable 类型与门面 | P8 保留兼容身份和组合；不得只为缩短文件改类型全名 |

此表用于约束所有文件的去向，不声称每一行都已完成语义审查。执行者只需对所属完整簇补齐字段/嵌套类型/外部引用明细及基线差异，而不是重新决定要做什么。超出上述责任的真实漏项须具名报告；不能作为匿名“杂项”搬入新大类。

## 4. 终态是什么，哪些明确不做

**两个宿主允许留下：** 引擎 override/监听注册、实例兼容身份、保存键与必要序列化类型壳、公开/反射门面、构造和销毁各 owner 的组合代码。入口可以捕获少量参数，但完整 UI、回合、运动、音频或业务状态机必须在自己的控制器中。

**明确不能留下：** P1 的材料优先级/pending 转换，P2 的召回评分/选择，P3 的转移规则/完整执行编排，P4 的历史/消息算法，P5 的整段 turn/worker，P6/P7 的运动/呈现/音频状态簇，以及 P8 的完整编辑器/导入流程。每项有上面真实符号，不用一句“主线程适配”整体豁免。

不设缺少依据的 5,000/10,000 行硬门槛，也不承诺两文件最终具体行数。按这些整簇抽出，主文件与完整宿主类型都应显著减少；最终必须交主文件行数、真正 partial 类型成员/总行数、各新 owner 规模和宿主保留清单。若主文件缩小而 partial 总规模基本不变，或出现等量万能 Owner，不能验收。

WorldDiplomacy、Reward、Knowledge、Siege 等现有领域大类不能仅因大就全部重写；本次处理上述消费者/AF 接缝，不吞入制作组玩法。本文能承诺的是**这两个大宿主及列明主体接缝的架构收官**，不是所有领域实现零技术债。未完成实机/旧档验收也不能叫全产品无条件完美。

## 5. 实施组织：四条独立执行线，持续集成

本节按用户最新选择替换原三线安排。**共 6 个模型：1 个主控、4 个执行者、1 个集成人。** P1–P8 保留为验收责任包，不对应八个会话或八轮重新调查。

| 角色 | 模型 / 推理强度 | 责任 |
| --- | --- | --- |
| 轻主控 | GPT-6.1 Sol / xhigh（`gpt-6.1-sol` / `xhigh`） | 范围、完整任务卡、文件所有权、跨线契约和退出判断；不接手普通实现与测试修复 |
| 执行 A | GPT-6.1 Sol / medium | P1 Campaign/周报触发与 P3 转移执行；P8 中相关领域数据操作仍由 A 完成 |
| 执行 B | GPT-6.1 Sol / medium | P2 Memory 召回与 P4 历史/Prompt；负责相关快照和消息语义 |
| 执行 C | GPT-6.1 Sol / medium | P5 回合/发言执行与 P6 场景运动；统一命令、取消、完成回执边界 |
| 执行 D | GPT-6.1 Sol / medium | P7 呈现/音频，随后领取 P8 独立编辑器/导入 UI 簇；不重写其他域 owner |
| 唯一集成人 | GPT-6.1 Sol / medium | 边界准备、公共契约、两个主文件组合入口、共享保存适配、项目/清单、Git 索引、集中构建与交付；不代写四线业务 |

### 5.1 先解除共享文件争用，不让所有接线排队

1. 集成人先绑定当前测试补齐后的候选，确认已有改动和适用指令；不重跑本计划已经完成的源码调查。
2. 先确定三组实际接缝：A↔B 的 draft/trigger 窄操作，A↔C 的转移结果/事实回执，B↔C↔D 的 detached 消息、主线程效果、呈现和完成回执。只定义即将消费的接口，不建通用框架。
3. 已有完整职责 partial 直接分配唯一写者，不再把全部 partial 永久锁给集成人。NativeTurn/SceneConversationChains 的执行体归 C；其消息/历史能力由 B 在独占模块提供，B 不同时改 C 的文件。五个 ScenePresentation partial 归 D；共享门面变更仍交集成人。
4. 仍交错在两个主文件的簇，由集成人先做必要的小范围机械分离，放到该线独占的过渡 partial，或者直接建立最终独占实现文件。保持行为、符号及条件编译，检查声明/调用差异和相应最小验证后移交。**只准备正在开工的簇，不先把九万行整体搬一遍。**
5. 过渡 partial 是施工隔离，不是完成成果。执行者随后将算法、状态及生命周期迁入真实 owner，并直接完成其独占接线文件；该责任包结束时不得把过渡 partial 内的大段原业务作为交付终态。
6. 一个文件同一时刻只有一个写者；交接时记录基线 blob、符号和写者。跨线共用源/DTO 由集成人拥有；其他执行者提交精确补丁，不能暗改别人的文件。正常包内修改与测试自行完成，越界或高风险操作按第 6 节处理。

由此把原来的“执行者写模块、集成人再做一次完整宿主接线”改为“执行者独立交付实现、消费者接线和验证，集成人只处理真正共享接缝”。不得给新 controller 传整个 MyBehavior/ShoutBehavior 来逃避依赖拆分。

### 5.2 流动排程，不设整波等待

- 边界就绪的责任立即启动，未就绪的先做独占实现；不为了开满四人让多线抢同一接口。不强制 P 编号串行，也不等待“第一波全部完成”。
- A 内 P1/P3、B 内 P2/P4、C 内 P5/P6 按真实依赖交错推进。C 与 D 先约定发言完成/呈现退出接口，运动与音频控制器即可分别实现。
- 每个完整职责切片完成就合入，后续消费者立即使用；每线最多保留一个待集成切片。合入积压时先修复/消化，不继续扩大宿主补丁。
- D 完成 P7 后转 P8；其他线提前完成可以接独立 P8 子簇，但必须正式转交文件所有权，不以“帮忙”为由同时编辑。保存/组合接线由集成人收拢，P8 不整体甩给他。
- 不默认创建多个工作树：工作树隔离不能解决相同代码的语义冲突；确需隔离时再按授权及独立输出安排，不能借此取消单文件写者规则。

### 5.3 每位执行者完成闭环，主控保持轻量

任务卡一次给全：具体结果、基线及保护改动、独占/禁止文件、真实入口与已有证据、输入契约、行为/保存/线程/性能约束、依赖、验证命令与输出隔离、退出条件、授权和交付位置。

执行者自行完成“最小读取 → 实现 → 所属消费者接线 → 定向验证 → 修复 → 完整切片交付”。只报告完成、真实阻塞、重大范围变化；接线材料直接交集成人，不经主控反复转发。不设常驻审核者，不重复全文审查，不要求普通失败逐次请示。

集成人不重复四线的包内开发与验证；做合入后的受影响检查、必要构建及最后同候选门禁。固定输出独占，独立输出才并行；全套验证不随每个 helper 改动重复运行，也不省掉必要跨包验证。

**效率检查：** 利用已有交付记录观察待集成数量、阻塞原因、补丁重定位次数和重复验证。若瓶颈仍是共享文件，先改善所有权/契约，而不是继续加代理；不为此另造监控平台。不承诺两波完成、固定天数或几倍提速。

本安排借鉴 [Parallel Change](https://martinfowler.com/bliki/ParallelChange.html) 的渐进迁移，以及 Anthropic 对[依赖密集多智能体任务](https://www.anthropic.com/engineering/multi-agent-research-system)和[并行编码争用](https://www.anthropic.com/engineering/building-c-compiler)的经验；四线分工是基于 AF 源码的工程判断，不是外部资料已证明的最优配置。本次只更新计划，不启动这些模型或实施边界准备。

## 6. 验证、权限和退出门

- 现有测试补齐作为输入；每包只重验变动行为及消费者，未变有效证据复用。不刷 hash、降低断言或改失败分类凑通过。
- 已读测试入口：`tests/modules/AF.Module.Memory/J17ImportExportTests/run.py`、`tests/modules/AF.Module.Conversation/ChannelHistoryRoleFixtureTests/run.py`、`tests/run_all.py`。后者支持 `--only`/`--ids`/`--jobs`/`--out`，依赖经授权 TEMP；执行前用当前最终配置，不照抄旧机器路径。固定输出 runner 串行归集成人，独占输出才并行。
- P1/P2/P3 的规则测试直接覆盖实际 owner；P4 同事实三渠道；P5/P6/P7 必须覆盖退出/取消/重复/迟到的状态和资源，不能仅构建；P8 覆盖保存/导入形状和部分成功。
- 集成人绑定同一候选执行规定构建和受影响总门禁，一套源码 1.3/1.4、Bootstrap、单模块发布身份保持。不拼接不同候选通过记录。
- 当前仅计划；无推送、部署、批量移动/删除、仓外写入、真实玩家资料操作、全局安装授权。实现时按每包正常修改权限执行；这些高风险边界另行取得精确授权。保存/导入尤其不得用真实档做破坏性试验。
- 实机签收单独列：三渠道交流/动作/记忆、跨日跨周、保存读档、mission 出入、带路传唤/归位、TTS/呈现结束与实际帧成本。没有结果就写未验，不用用户对当前测试的假设代替实机记录。

最终收官须同时交付：

1. P1–P8 的旧符号→真实 owner→生产消费者→状态/退出证据，旧完整算法/状态簇不再留宿主。
2. 两个完整宿主的剩余清单：每项具体用途、保留理由和实际消费者；无“其他适配”大桶。
3. 代表性规则变更只读影响分析：每包至少一项，核心修改不再跨两个宿主求解。
4. 同候选离线验证、实际文件/类型规模对比；产品实机/旧档层另行签收。
5. 现有主台账、代码图、HANDOFF 的唯一交付入口；不建立第二套永久台账。

到此结束，不因为还有可以细分的内聚类继续扩项。若上述明确责任仍在宿主，不能再以“分配了 owner”“测试全绿”“工作包结束”宣布架构完成。
