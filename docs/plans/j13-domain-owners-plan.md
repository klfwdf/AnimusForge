# J13 其他领域职责拆分与离线验收计划

> 执行后状态（2026-09-25）：`J13_OFFLINE_VERIFIED`；最终候选、各包证据及未验范围见[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md)与[当前交接](../../HANDOFF.md)。这只表示本计划的有限离线收尾，不是实机或发布验收，J14 未启动。
> 规划时点状态（2026-09-24）：`J13_PLANNED`；该轮只编写计划，未开始产品实现。
> 规划源码基线：`0624d5025fac98877332ccb1d5221dd4f85f836e`；J12 产品终点 `5c3e7b0e`，J07–J12 保持 `OFFLINE_VERIFIED`。
> 本文细化主台账的 J13a–f，不授权 J14、新玩法、自动化、推送、Stage、部署、打包或外部目录写入。

## 1. 新对话启动指令

把下面内容发给新对话即可开始实施；本轮规划者不执行它：

```text
执行当前仓库 docs/plans/j13-domain-owners-plan.md。
先按 AGENTS.md、HANDOFF.md 和维护/框架两个仓库 Skill 核实实际 Git 根、分支、HEAD、dirty 与最新主台账，不能按历史 G: 路径切换工作树。
从 G0 开始，按 J13a Weekly → J13b Kingdom → J13c Persona → J13d Social/Issue/WorldEvents/WarStats → J13e 场景领域 → J13f UI/Onboarding → J13g 离线收口推进。
先完成一个真实责任切片并验证，不先批量搬文件，不以 partial、转发壳或减少行数代替职责迁移。复用真实生产实现和已有回归；普通实现细节自行处理。
本地记录开工意图/检查点，每个验证切片单独提交，只提交本任务文件。每包结束更新主台账、代码地图和简短 HANDOFF；中断时记录下一条具体动作。
不要自动 push、Stage、部署、打包、写存档、改自动化、安装全局工具或修改制作组玩法。构建脚本存在目录清理，执行前检查并取得精确清理范围的授权；不要绕过安全限制或引用校验。
只有当前切片相关的真实失败、设计冲突或授权边界才阻塞该切片；必要门禁未完成不能报 OFFLINE_VERIFIED。离线证据不得冒充实机/旧档/provider/性能验收。J13 完成后交接 J14，不自行开始 J14。
```

## 2. 范围、顺序与设计决策

- 目标是同一个 `AnimusForge.dll` 内的真实领域 owner；不拆新 DLL，不增加无消费者的 manifest/Host/公开接口。
- 保留既有玩家规则、MCM 开关、Prompt/标签、正常/失败语义、存档身份、Harmony 目标与注册顺序。缺陷修复必须有具名复现和应保持/有意改变的说明，不能夹带改玩法。
- CampaignBehavior、MissionBehavior、Saveable 类型和 Harmony patch 类保留原身份；**不迁 Harmony 类本身**。领域算法/瞬态状态归模块，主线程游戏访问与事件回调可保留为薄适配。
- 对依赖私有嵌套存档类型的代码，允许先用原类型的 partial 迁出完整职责单元，保持同一状态实例；但整文件 R100、机械 partial 和新类回调整个旧算法只能记结构进度，不能单独完成责任包。每包必须有真正承担决策/状态转换的实现及真实消费者证据。
- Weekly 只消费 Kingdom/Memory/Actions 提供的材料和结果，不吞并这些领域。Persona 已有真实预约 owner，优先复用，不另造生命周期。UI 负责呈现/输入/订阅，不成为第二个业务 owner。
- J13d 明确补齐旧提纲入口里已有、但 a–f 简表容易遗漏的 WorldEvents、WarStats、主动请求与 Vanilla Issue；不是增设玩法。J12 的外交/经济/地图 owner 不重开，除非本包接缝产生具体回归。
- 不包含 Policy/Gathering/GCCZ 业务重写、J14 三渠道 public submit、J15 资源重排、J16 工具大迁移和 J17 全仓清理；不得为了清空 `MyBehavior`/根目录删除活动兼容入口。

| 包 | 依赖和完成范围 | 拟定目录（尚非完成事实） |
| --- | --- | --- |
| J13a | 首包：Weekly 调度、材料、请求/完成、回执与发布 | `src/modules/AF.Module.Weekly/{Scheduling,Materials,Generation,Receipts,Publication}` |
| J13b | Weekly 接缝确定后：Kingdom 稳定度、关系偏移、周度叛乱协调 | `src/modules/AF.Module.Kingdom/` |
| J13c | 已有人设预约/生成/准备状态与真实消费者归属 | `src/modules/AF.Module.Persona/` |
| J13d | 分别闭合 Social、Issue、WorldEvents、WarStats；不一把迁完 | `src/modules/AF.Module.{Social,Issue,WorldEvents,WarStats}/` |
| J13e | Duel → Taunt → Encounter → Settlement/Inspection → Exercise | `src/modules/AF.Module.{Duel,Encounter,Settlement,Exercise}/`；Taunt 按和平冲突责任独立子包 |
| J13f | 上述领域 UI 薄适配、Overlay、Onboarding | 原 GameAdapter/GUI 惯例；`src/modules/AF.Module.Onboarding/` |
| J13g | 最终候选的离线兼容、接线、台账与地图收口 | 不增加运行模块 |

## 3. 开工 G0：有限基线与安全检查

1. 运行 `git rev-parse --show-toplevel`、`git branch --show-current`、`git rev-parse HEAD`、`git status --short --branch`、`git diff --stat`、`git diff --cached --stat`。本次规划树为 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`；这是定位记录，不覆盖新对话的实际 Git 状态。
2. 读取根 `AGENTS.md`、`HANDOFF.md`、两个仓库 Skill、框架协调/工作包/验证参考，以及[主台账当前入口](../animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。J12 结论见[最终交接](../handoffs/2026-09-22-j12-final-closeout.md)，不用重演历史错误回执。
3. 保留未跟踪 `.dotnet-cli-home/`，不纳入提交、不清理。若实际 HEAD 比规划基线新，先核对涉及 J13 的差异；他人对同 owner 的改动先协调，禁止 reset/stash 覆盖。不要自动拉取/合并或改其他工作树。
4. 核实 SDK、Python、Newtonsoft、Bannerlord 双版本引用、Harmony/MCM 与现有构建入口。历史 G: SDK/游戏路径都不是本机已验证配置。仅使用必要路径，不全盘扫描，不打印密钥/配置正文。
5. 环境陷阱已在源码确认：`WeeklyReportSchedulePolicy.SmokeTests` 是 net6.0；`HeroPersonaGenerationTests/run.py` 写死 G: SDK；`EncounterLifecycleBoundaryTests/run.py` 使用仓库父目录 `.dotnet-sdk`。缺依赖先诊断；必要时仅给测试工具增加显式路径参数，保留所有断言。不能安装全局 SDK或降低产品目标框架凑 PASS。
6. `PhaseEightParityReplayTests/Program.cs` 当前读取 Stage DLL。不要为测试偷偷 Stage 或读取旧 DLL冒充新候选；需要该测试时允许仅对测试宿主增加显式候选 DLL 参数、来源/新鲜度校验，仍调用原生产实现和全部相关断言。
7. 先跑本次切片的最小既有基线，并记录失败是本地环境、旧路径还是产品回归。不在 G0 重跑全仓所有套件。`MemorySummaryMainThreadBoundaryTests` 的历史 `Missing TagSceneSessionHistoryLine` 不计 PASS、不靠删除断言解决；只有涉及它的真实责任时定向处理。
8. 在主台账登记当前切片的目标、owner/消费者、保留身份、风险与少量退出门，提交本地意图/检查点后才开始实质生产改动。不要 `git add .`；只暂存自己的具名文件。

## 4. 核实过的源码入口（规划基线）

下表是初始定位，不是完整迁移清单；范围为一基行号，实施时按符号重定位。旧主台账的“352/120/63 方法”等计数只作历史规模参考，不作当前完成指标。每个后续包开始前先读取其方法体和调用者，再在同一主台账补齐责任清单。

| 责任 | 路径 / 一基行号 / 符号 | 当前消费者与保留边界 |
| --- | --- | --- |
| Weekly 自动调度/分批准备 | `MyBehavior.cs:5847–5867` `QueueDeferredAutoWeeklyReportsForWeek`；`:6066–6157` 初始化与预算处理；根 `WeeklyReportSchedulePolicy.cs` | 日维护队列、每周触发；保留补周顺序、叛乱暂停和开关资格，不搬整个日维护器 |
| Weekly 按需全文/回写 | `MyBehavior.cs:42647–42779` `GenerateWeeklyReportFullByEventIdAsync`、`QueueWeeklyFullReportCompletionAsync`、`ProcessWeeklyFullReportCompletions` | UI 请求，worker 网络，engine pump 主线程提交；源材料变更/已发布胜出者/旧代必须拒绝或按原成功语义处理 |
| Weekly 多模式生成 | `MyBehavior.cs:43158` group retry、`:43213` batch retry、`:45664` minute burst、`:45856` commit、`:46643` batched、`:46808` dispatch；`:33261` API client | 不静默丢掉任一生成模式、重试路径、失败 UI 或缓存/route 行为；复用 J08 传输 |
| Weekly action outcome | `MyBehavior.WeeklyActionOutcomeReceipts.cs:15–27` key/ledger/runtime；`:29` prepare、`:93` complete；`Refactor/Runtime/WeeklyMemoryMaterialOutcomeReceipt.cs` | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs:782,792` 与 `src/modules/AF.Module.Actions/Receipts/InteractionResultCommitter.cs:85–116`；共享唯一 Actions 执行结果，不由 LLM 文本推测成功 |
| Kingdom | `MyBehavior.cs:10946–11042` 稳定度读取/调整/百科；`:11214` relation pair、`:13099,13106` weekly rebellion | 百科、定居点模型、Weekly 和真实 Campaign 事件；不吞并 J12 Diplomacy 或 PolicySystem |
| Persona | `MyBehavior.PersonaGeneration.cs:14–17` owner/work；`:50` capture；`MyBehavior.PersonaReadiness.cs:12–28` snapshot；`MyBehavior.cs:19210` facts | 既有 `src/modules/AF.Module.Conversation/Internal/NpcPersonaGenerationOwner.cs`；编辑器、外部入口、三渠道 preparation 共用预约与代际 |
| Social 招募/入队残余 | `RewardSystemBehavior.cs:4232–4245` Hero join；`:4391–4406` non-Hero tag/Native request 接缝 | J12 保留的 mixed-domain 兼容责任；入队、升格与身份/原队伍清理归 Social，经济资产执行仍留 J12 owner |
| Social/Issue | `PlayerNotorietyBehavior.cs:71,95,154`；`RomanceSystemBehavior.cs:83,115,131`；`ProactiveNpcRequestBehavior.cs:103,112,301–368`；`VanillaIssuePromptBehavior.cs:16–35`；`VanillaIssueOfferBridge.cs:177–196,493,673` | 事件/SyncData、对话上下文、主动请求消费、任务受理/同伴延迟回调；每个具体领域单独闭包 |
| WorldEvents/WarStats | `WorldEvents/WorldEventInbox.cs:43–50` records/unread/version；`WarStats/AfWarStatsBehavior.cs:15` 原 Campaign owner | `src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs:31,54` 注册；WarStats 原 `AFWarStatsTerminal.Behaviors` namespace 保持 |
| 场景领域 | `DuelBehavior.cs:174–377` 生命周期；`SceneTauntBehavior.cs:115–132,295–360`；`LordEncounterBehavior.cs:280–384`；`SettlementEntryTroopSelectionBehavior.cs:108–255`；`TroopInspectionBehavior.cs:147–219,530`；`MilitaryExerciseBehavior.cs:135–197` | Harmony/任务事件仍在原 host；已读入口声明，整个领域依赖闭包必须在各包施工前逐符号核对 |
| UI/Onboarding | `ModOnboardingBehavior.cs:236–352`；`src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs:47,65,88,106` | Weekly popup/百科轮询，onboarding 启动提示；保留默认配置、焦点和订阅清理 |

## 5. J13a Weekly：首个可执行工作包

### a0：锁定链路，不直接切 9k 行

从 `RegisterEvents`、`OnEngineTick`、`SyncData` 追踪：日维护入队 → 准备材料 → 生成模式选择 → 请求/重试 → pending commit → 发布/通知/全文补生成，以及 Actions → prepare/complete → ledger → 材料发布。

在主台账列出方法/字段/嵌套 DTO 的 owner 清单：Weekly 独占、跨域只读输入、Campaign/保存兼容保留。至少核对自动/手动、摘要/全文、minute burst/batched、失败重试/恢复和载入清理入口；不得只按名称含 Weekly 的文本搜索批量迁移。

### a1：调度与材料

- 先归位 `WeeklyReportSchedulePolicy`、`WeeklyReportTextHelper` 等现有实现与测试 Compile 引用，再实际转移补周选择、分组、聚合、过滤/排序和准备游标的责任。
- 保持首次安装只从最新完整周开始、已有游标按最早缺周补齐；叛乱进行中延期而不是丢报告。禁用后不新开请求，原 in-flight 的既有完成语义需读代码锁定。
- 游戏对象/配置捕获在所属主线程；跨线程输入必须 detached。保留现有分批阶段和预算，不让“每帧一次”包住全量历史扫描。初始化 snapshot 的 O(N) 成本与频率单独登记，不虚报已经变成常数成本。
- 退出：同一组材料和日期得到相同 eligibility、顺序、分组、周界；真实自动入口调用新责任实现，未保留双份游标或并行队列。

### a2：请求与完成生命周期

- 转移生成协调、预约/in-flight、重试结果和 completion 队列所有权；API/领域 Prompt 接缝保持原 route 和 J08 transport，UI 不持有请求状态。
- worker 不读取 live Hero/Kingdom/EventRecord/可变 owner。提交至少重验 owner、SaveRuntimeGuard generation、目标记录与源状态；清理必须结算等待者，旧回包不能释放新请求。
- 保留已有全文胜出者，不覆盖被编辑的源材料；保留真实成功/失败、部分完成、popup 恢复与一次发布语义。`at most two commits` 只证明回调数，另量化每次 commit 的 record/字符工作量与积压；没有实机数据就不承诺帧耗时。
- 退出：自动与按需两条真实链都接通；正常、强制异步、同代材料变化、换 owner/读档、重复 completion、异常、清理等待者、队列积压有生产实现证据。

### a3：动作材料回执与发布

- 将 `WeeklyMemoryMaterialOutcomeReceipt` 的真实 ledger/fingerprint/serialization 及对应 host 接缝归 Weekly；跨域契约保持现有类型/枚举值和唯一消费者，不把 Economy 执行算法迁入 Weekly。
- 保留 `_af_weeklyActionOutcomeReceipts_v1`、导入/隔离/容量/重试语义，prepare 不等于已发生。Rejected/Partial/Unknown/Quarantined 必须按既有规则处理，不能统一升级为 Confirmed/Applied。
- 复用 J09 权威提交；Three-channel 同类结果进入同一账本，不能直接把标签/正文写成 AFEF 或绕过执行回执重复发材料。
- 退出：重复、fingerprint 冲突、容量不足、失败重试、存档导入、载入边界、成功只发布一次均有断言；原 DTO/存档键和外部入口身份不变。

### a4：Weekly 有限退出门

1. a1–a3 的实际算法/状态 owner、生产消费者和保留 host 在代码地图可追踪；不是仅 R100 或转发壳。
2. schedule smoke、material outcome、现有 Weekly production replay 加新增关键生命周期/预算反例通过；复用原 harness，不为每个 helper 建新套件。
3. 受影响的三渠道提交、持久化/API/编译集合检查通过；两个 Bannerlord 实现可构建。
4. 本地切片提交与台账明确未迁移项、NOT-RUN 和 J13b 下一动作。上述退出门满足即进入下一包，不因 `MyBehavior` 仍大而无限追加工作。

## 6. 后续责任包：施工步骤与退出门

以下为既定责任与验收，不是未读代码的逐方法改写指令。每包开始补读实现/消费者，细化最多几个可验证切片；若边界需要改变，先更新本计划和主台账，不静默遗漏。

### J13b Kingdom

1. 核对稳定度/关系偏移/皇家领地忠诚度、周度平衡/叛乱的字段、保存与调用者；将规则、批次游标和协调归 Kingdom。
2. 关系成对调整的既有偏移记账、应用/撤销和重复事件语义保持；Weekly 只触发/读取其窄接缝。真实 Kingdom/Clan/Hero mutation 仍在主线程。
3. 不重写 `VassalageBehavior`/`KingdomAnnexationBehavior` 的 J12 接缝或制作组政策。不借目录名移动全部含 Kingdom 的文件。
4. 退出：稳定度边界、重复应用/撤销、失效王国/成员、周触发重复、叛乱阻塞与恢复、读档游标与实际模型消费者有证据；必要双版本/保存检查通过。

### J13c Persona

1. 优先归位既有 `NpcPersonaGenerationOwner` 与 readiness/生成责任，更新真实三渠道 preparation、百科编辑与外部调用；不创建第二个预约/冷却表。
2. 保持 capture → detached request → owner/lease/generation/原内容重验 → 主线程 commit；用户中途编辑不能被旧结果覆盖，旧请求失败不能清掉新预约，voice/profile 后续行为不丢。
3. 单独核对升格同伴/未命名角色等现有路径；不能用 Hero fixture 证明所有 Persona 分支。有未迁移分支则写明实际责任和证据。
4. 退出：`HeroPersonaGenerationTests`、`ChannelPersonaPreparationTests` 及原 source parity 有效；编辑器/三渠道/外部真实消费者覆盖，存档/公开入口保持。仅路径更新不能删除逆向/行为断言。

### J13d Social、Issue、WorldEvents、WarStats

- **d1 Notoriety/Romance/Recruitment**：将名声观察/传播/低调身份、恋爱/婚姻资格与上下文消费分别归真实 owner。保留一次性 marriage postprocess context、关系/身份和存档规则。另以独立切片承接 J12 留下的 Hero/non-Hero 入队/升格领域算法，核对家族/配偶身份保留、俘虏释放、原队伍清理、Native token/Agent 身份和生成后的人设接缝；不重复执行 Economy 转移。不强删仍有调用的 `ApplyRewardTags`，其退出条件是所有 mixed-domain 消费者已真实接通。验证重复输入、缺 owner、失效对象、失败不伪造事实和通道差异。
- **d2 Proactive/Issue**：主动请求的资格、状态、pending opening 与消费归 Social 子包；原版 Issue 的 offer/in-progress/turn-in/完成回执归 Issue。核对小时触发、取消、失效任务、同伴/部队窗口迟到回调、重复领取；复用 J10 Courier 生命周期，不把“信件已送到”当“任务已接受”。
- **d3 WorldEvents**：收件箱 records/unread、stable key 去重/容量/version 与发布投影归业务 owner；map/UI 留适配。Weekly/政策/其他源的事件不得重复发布或丢未读状态。
- **d4 WarStats**：战争计数、归档、历史/死亡记录归其真实领域实现；原 `AFWarStatsTerminal` 类型、v5 保存布局、事件装配与终端消费者保持。区分实际 Campaign 事件与 fixture 归档证据。
- 每个子包各自通过一个聚合源码接线契约和相关行为用例；复用 `NotorietyConversationOutcomeContractTests`、`PhaseEightParityReplayTests` 的 WarStats/终端覆盖，缺少的状态反例集中补充，不把四域仅移动文件就标完成。

### J13e 场景高风险包（按顺序独立提交）

开工必读[伤害上下文](../scene_damage_context_guard_case.md)、[军团会面目标](../army_member_custom_meeting_target_case.md)；涉及带路/跟随再读[场景 Agent 移动](../scene_agent_command_movement_case.md)。通道/动作变化同时按[三渠道对齐](../free_conversation_scene_shout_alignment.md)与[指令标签](../directive_tag_output_case.md)核验。

| 子包 | 实施责任 | 必须保持/验证 |
| --- | --- | --- |
| e1 Duel | 复用既有 typed outcome，转移受理、运行状态转换、结算协调；原 Mission/Harmony host 留适配 | exact DuelId/subject/fingerprint、延迟启动/超时、stake/debt 和终局一次结算；Courier 拒绝等现有渠道例外不强行同化；复用三个 Duel 套件 |
| e2 Taunt | 和平冲突资格、挑衅升级、惩罚与恢复协调 | allowlist 排除攻城/野战/部署/潜行/决斗/竞技场/训练场等非本机制上下文；关闭是退出模组处理，不把伤害设 0；敌对队伍只在合法初始化后变更并恢复 |
| e3 Encounter | 目标解析、释放授权/超时、pending 返回与会面生命周期 | 原版选中军团成员优先，合法 `_targetHero` 不被军团长覆盖；换 party/Mission/save 后拒绝旧回调；复用 `EncounterLifecycleBoundaryTests` |
| e4 Settlement/Inspection | 入场队伍选择、临时队伍/Agent 状态和离场清理 | Agent 身份而非裸坐标为主目标；取消/重复打开/死亡/中止/载入均有清理；GCCZ 只核对已有薄接缝，不改其业务或外仓 |
| e5 Exercise | 演习初始化、临时结算、结束恢复 | 保持 1.3/1.4 奖励签名、补丁排除条件及战斗结果语义；不能误抑制真实战斗伤害/奖励 |

每个子包记录 Harmony target/patch 类/签名/条件/注册点/调用 owner/清理点核对表，留在主台账或代码地图；不新增重复 PatchAll/事件订阅，不重排既有注册。至少包含本机制正例、非本机制反例、MCM 关闭、结束/重入和关键失效回调；离线 stub 不能证明原版实际战斗事件顺序。

### J13f UI / Overlay / Onboarding

1. 先区分业务状态和 UI 的打开/关闭/焦点/订阅状态；业务调用前述 owner。Overlay 保留 J07 的真实 Native 连接，不能借 UI 整理改默认对话入口。
2. 读取[百科按钮案例](../encyclopedia_button_injection_case.md)：真实 root/datasource 绑定、子控件事件、MCM 开关和输入框拦截百科快捷键都保持。
3. Onboarding 保留启动提示时机、幂等和失败恢复、现有配置储存与脱敏；不得把 key/token 写进日志、测试 fixture 或交接。
4. 退出：打开/关闭/反复打开、失效 owner、订阅释放、输入焦点与相关资源引用完成离线核验；Gauntlet 实际点击/渲染/焦点未实测必须 NOT-RUN。不迁 J15 内容文件和运行资源名。

## 7. 验证、构建与命令入口

### 7.1 每包与最终门禁

- 每个 a–f 包：真实 owner 接线契约、对应行为/负向回归、受影响保存/API/Compile 集合和双版本构建；中间小切片按风险定向运行，不每改一个 helper 就六构建。
- J13g 最终候选：Debug/Release × 1.3/1.4 + Bootstrap；四实现 DLL API/metadata、Persistence Profile/Chunk/Identity contract、Bridge/Phase8 readiness/source inventory、recorded/working-tree 代码地图；按实际触及接缝复跑 Native/Scene/Courier 与 J12 相关回归。不照抄历史断言数量当完成条件。
- 没有必要环境、构建未通过或相关行为门禁失败，保持该包 `VERIFY/BLOCKED`；受影响产品不得标离线完成。未受影响的 live/旧档/provider/Stage/音频/帧性能维持 NOT-RUN，不阻止诚实限定的离线收口。
- 对只验证目录的检查如代码地图、源码接线、PE 元数据明确证据层级；不拿它们替代真实算法和生命周期行为测试。

### 7.2 已存在的入口

下面均从实际仓库根运行。`$dotnet` 必须在 G0 指向本机核实过的 SDK；先读 runner/README 的写入和依赖规则，不能直接使用旧 G: 默认。生产 replay 的候选 DLL 必须来自当前源码构建且身份匹配。

```powershell
# 只读定位；不代表玩法验证
python -B .agents/skills/af-core-framework/scripts/verify_code_map.py
python -B .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree

# Weekly 两个现有契约入口；第二个需要可用的 net6 targeting/runtime
& $dotnet run --project tools/WeeklyMemoryMaterialOutcomeContractTests/WeeklyMemoryMaterialOutcomeContractTests.csproj
& $dotnet run --project tools/WeeklyReportSchedulePolicy.SmokeTests/WeeklyReportSchedulePolicy.SmokeTests.csproj

# 最终当前四 DLL 契约入口；环境与候选就绪后运行
python -X utf8 -B tools/ModuleFrameworkApiTests/run.py --dotnet $dotnet --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts
python -B tools/test_repository_source_inventory.py
```

其他已存在入口：`tools/{HeroPersonaGenerationTests,ChannelPersonaPreparationTests,EncounterLifecycleBoundaryTests}/run.py`；`tools/{DuelDispatchContractTests,DuelOutcomeContractTests,ProductionDuelOutcomeReplayTests,NotorietyConversationOutcomeContractTests}` 的同名 csproj；`tools/PhaseEightParityReplayTests/PhaseEightParityReplayTests.csproj`、`tools/ProductionOptInEntryReplayTests/WeeklyActionOutcomeProductionReplay.cs`（由其现有宿主调用）。不要把单个 `.cs` 当独立 runner。

net6 依赖缺失时，可在仓库内独立测试临时目录用相同 Program/生产源码与不变断言建立经核实的 net8 测试宿主，明确它与原 net6 runner 的验证差别；不修改产品目标框架、不静默访问网络安装。更改源码读取路径时要验证读取的是新实现，而非只刷新 hash/源片段匹配。

### 7.3 原构建入口及删除风险

沿用 `一键编译覆盖推送/build_single_module.ps1`，参数已核实有 `ProjectRoot`、`BannerlordRoot`、`Bannerlord13ReferenceDir`、`Bannerlord14ReferenceDir`、`RuntimeDependencyDir`、`HarmonyCorePath`、`WorkshopContentDir`、`Configuration`、`Stage`、`Deploy`。J13 离线构建**不传 `-Stage` 或 `-Deploy`**，不照抄兼容文档中带 Stage 的示例。

**即使不传 Stage/Deploy，脚本仍会递归重建 `bin/<Configuration>/single_module_artifacts` 和 `obj/single_module/<Configuration>`，并裁剪构建输出。** 执行者必须先核实解析后绝对路径、目录内容、无越界/链接跳转，再请求对这些精确构建产物目录清理的确认。没有确认不调用脚本；不是通过换 shell、复制脚本或手写 MSBuild 绕过。

获准后按原脚本分别执行 Debug、Release，读取其实际引用版本与 build marker；1.3 引用来源校验必须保留。缺引用或构建被授权边界阻塞就如实记录，不修改一键脚本、默认交互入口或原版 DLL 凑通过。Stage、游戏部署和存档仍独立未授权。

## 8. 提交、交接与中断恢复

- 规划本身只提交计划/主台账/HANDOFF；产品实施前另有本地意图检查点。每个验证切片独立提交，不混入 `.dotnet-cli-home/`、产物、用户配置或其他作者改动，不自动推送。
- 主台账是唯一详细进度证据：每包记录状态、修订、源路径/一基行号/符号、真实消费者、保留 host 和原因、验证命令/退出码/候选 hash、NOT-RUN、下一动作。代码坐标沿用[代码范围图](../architecture/af-framework-code-scope.md)及其 JSON，不为每包复制竞争台账。
- HANDOFF 只写当前包/最新可用提交/阻塞/下一步并链接主台账。中断时说明已完成到哪个切片、dirty 属于谁、正在失败的具体测试、下一条最小动作；未完成不标整个 J13 完成。
- a–f 和 J13g 必要退出门全部满足，才标 `J13_OFFLINE_VERIFIED`。有意保留的游戏 host/兼容面列明，不要求为完成删除所有根文件。实机、旧档、真实服务与性能单列，不能虚报全仓终点。
- 出现新回归先定位所属包；需要撤销时提出针对相关提交的逆向提交方案，履行相应授权，不 hard reset/rebase/强推。不要执行 J12 历史回滚命令或改其他工作树。

## 9. 本次规划验证与未运行项

本轮读取了最新交接/主台账、上述真实源码入口与首包关键方法体、现有测试/构建参数和风险。只更新本文、主台账当前入口和 HANDOFF；产品代码、测试实现、构建脚本、默认配置均未改。文档链接/源码定位及代码地图核验结果以本次主台账回执为准。

本轮未运行产品测试/构建、真实 Campaign/Mission、旧 SAVE、provider、音频或性能测试；没有新产品 PASS，没有 Stage/部署/打包/推送，也没有恢复或修改自动化。
