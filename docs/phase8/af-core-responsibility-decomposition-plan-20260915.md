# AF 主体真正模块化：到收尾评审前的执行计划

> 2026-09-15。沿用原 **P0–P6 / B1–B3**，本文是职责拆分设计与验收清单，不是第二本执行台账。
> 本轮只实际修复内层列表失效检测并验证；下列大范围提取尚未实施。自动化仍暂停，不自动部署/推送/切默认。

## 1. 目标：不是拆文件，而是拆清责任

用户确定的发布布局不变：一个 `AnimusForge.dll`，内部模块接口和外部子 MOD API 分开；Bootstrap 只选中当前游戏版本的一份实现。

```text
AnimusForge.dll
├─ AF Core
│  ├─ Conversation：回合/请求编排及结果状态
│  ├─ Prompt & Rules：上下文、话题资格、消息/标签规则构造
│  ├─ LLM Gateway：provider协议、网络、解析、重试策略
│  ├─ Actions：标签解释、计划、资格重验、动作回执
│  ├─ Memory：历史、AFEF事实、三类摘要、恢复与来源一致性
│  ├─ Runtime：所属线程、预算、排队、档代/会话生命周期
│  ├─ Persistence：现有存档契约/迁移适配，运行状态由各owner持有
│  └─ Presentation：文本/流式/TTS/气泡的展示生命周期
├─ Host Adapters
│  ├─ Native / Scene / Courier：不同交互语义，不另起三套核心业务
│  └─ Bannerlord / Settings / DeveloperTools：引擎接线和编辑入口
├─ Team Internal Ports & Bridges
│  └─ 政策 / 宴会 / GCCZ 等制作组业务owner（本任务不重写）
└─ Public API（版本化）
   ↑ 独立子 MOD DLL，仅能调用已明确开放能力
```

这些是**责任名称，不是要求立即生成同名空文件夹/程序集**。已有 `Refactor/Runtime`、`Contracts`、`Adapters`、`Modules` 与 `Api/V1` 能承载的优先复用，只有实际消费方和责任需要时才创建新类型。

### 可判定的“拆干净”标准

1. 每一项核心业务只有一个状态/执行 owner；跨模块只通过窄 typed 契约，不通过对方的私有集合、反射或万能 ServiceLocator。
2. MyBehavior、ShoutBehavior、CourierDeliveryBehavior 最终仅保留引擎回调、主线程对象解析、薄转接，以及明确必要的序列化/ABI壳；不再拥有一整段 Prompt 构造、摘要算法、标签规则或第二套对话链。
3. `partial` 只是整理方式。把方法移到同类 partial、包装旧静态入口、堆 delegates 回调旧业务，不记为“职责提取完成”。
4. 同职责的旧实现随真正替换一起删除；不留 feature flag 双跑、新旧双写、注释掉代码或无消费者兼容层。兼容壳只转接，不保留业务副本。
5. 每个提取单元都有真实入口、状态归属、替换调用点、行为回归、故障路径、成本说明与回滚点。
6. 目标是**无已知关键回归、边界和覆盖可核查**；不承诺数学意义“零 Bug”。大类行数下降只是辅助观测，不是功能等价证明。

## 2. 当前起点与限制

- 拉取基线 `af618912`，上游生产 `4d6994bc`，修复前的同数量变动已通过真实抽取封存入口复现；本轮修复见最新 HANDOFF，不重做已经完成的共享预算/排序/raw摘要等。
- 52 个 Refactor C#、20 个核心 owner partial 的存在，不代表三大类拆薄完成。大类家族仍约 11.6 万行，含开发编辑器、导入导出、存档、规则与交互混合责任。
- 原始 AF 行为参考 `d4cb1467`；当前修复/迁移对照也要保留最近稳定父提交。不能把原始 AF 的已知缺陷复制回来来满足“完全一样”。
- B1 尚未整批验收；Courier 后台 live 读取、完整三渠道、内部生命周期、所选 public 能力与同候选实机仍有待办。
- `.tmp/build_check`、参考源码/用户导出等历史仓库材料的来源/许可/HOLD 单独处理；它们不是可随手删除的“业务旧代码”。本计划不解除该 HOLD。

## 3. 依赖方向和接口纪律

- Game adapter 在所属主线程把 live `Hero/Agent/Campaign/session` 转为明确输入/身份；后台模块只处理被允许的网络/计算数据。纯算法层不反向引用 MyBehavior/Shout/Courier。
- Conversation 负责串起一次请求，不承担每个模块内部业务。主流程为：接入/资格 → 捕获 → 前处理/Prompt → LLM正文 → 权威后处理/ActionPlan → 执行回执 → Memory/AFEF → 展示通知。
- Actions 调用原真实领域执行器；只有已发生结果才能交给 Memory 记事实。不得把排队、正文成功、网络成功或未支持写成动作成功。
- 内部制作组模块贡献上下文/规则/动作结果，必须声明作用域、顺序、资格与失败语义；不让 GCCZ 规则进入普通 AF 场景。
- Public API 不绕过上述同一完整链，不暴露可变游戏对象/内部状态/密钥；合同版本稳定，未支持明确返回 NotSupported。旧公开方法按实际外部使用/ABI责任迁移，不能按名称删除。
- 配置按真实可配置需求放现有设置/规则文件：记录默认值、校验、读取时机、更新方式和回退。英文代码标识符/协议，中文提示词和玩家说明均可；不是把全部实现写死，也不是把所有细节抽象成可配置。

## 4. 核心职责包：从哪里拆、拆成什么、何时能删旧

下表是**计划状态**；最终类型/文件名在检查调用者后确定，避免先造一套无人使用的框架。

| 包 / 对应阶段 | 当前实际源头 | 目标责任与允许依赖 | 旧位置退出条件 |
|---|---|---|---|
| M1 来源与快照 / P1 | MyBehavior.MemorySummaryInput、MemorySourceFingerprint、MemorySourceWrites | Memory source owner 负责版本/快照与所有writer登记；纯复制/摘要只读DTO；Host保留live捕获 | 三类summary输入/重试/接受与真实writer全部接同一策略；不再从大类复制第二份规则 |
| M2 维护与摘要 / P1 | MemorySealing、MemorySummaryPlanning/MainThread；MyBehavior.ProcessMemorySummaryQueueAsync | 有独立状态的维护/规划/完成协调职责，复用现有预算与排序组件；不持有整MyBehavior | 封存、过期拒绝、排队、部分结果、释放/重试均由新owner负责；旧循环删除 |
| M3 历史与事实 / P1→P2 | MyBehavior.HistoryPromptSnapshot、DialogueHistoryCommit、MemoryRecovery、原历史/AFEF/周报接缝 | 区分可见历史、持久记录、已发生事实和摘要；一个写入接受owner | 三渠道/编辑/导入/摘要/周报writer对齐，无隐式双写；保留原存档兼容 |
| C1 回合编排 / P2 | InteractionRequestCoordinator/ResultCommitter、原主链路 | 回合状态、阶段回执、取消/过期/重复、故障与结束；通过窄ports调用Prompt/LLM/Actions/Memory | 普通真实入口不再走第二套旧编排；不是只迁opt-in演示入口 |
| PR1 上下文/话题/Prompt / P2 | MyBehavior.BuildShoutPromptContextForExternalInternal、RunCourierRulePreprocessForExternal；LegacyPrompt/Preprocessor adapters | 纯规则资格/上下文组装与消息预算策略；live资料由Host捕获 | Native/Scene/Courier同规则来源、前主后处理顺序和tag_rules；旧构造/筛选副本删除 |
| G1 LLM/网络策略 / P2 | ShoutNetwork、LlmApiCompat、LegacyConfiguredChatGateway、RuntimeConfigSnapshotStore | provider消息协议/配置快照、预算/限流、网络重试、raw响应与流式事件；游戏对象与业务提交不进入网络层 | 渠道不再各自拼网络请求/重试；已有Gateway有真实消费者，取消能力按底层实际支持声明，不重试已开始游戏副作用 |
| A1 标签/计划/回执 / P2 | ActionPlan/标签契约、LegacyInteractionPipelineComposition、原后处理 | 标签规范化/资格/计划/回执独立；执行仍唯一领域owner，副作用必须主线程重验 | 未知/冲突/过期/部分成功一致；旧重复解析、计划与重复commit入口退场 |
| N1 Native adapter / P2 | ShoutBehavior.NativeAdmission/Preparation/Completion、SubmitNativeConversationTextInternalAsync | Native的busy/窗口/流式/主动开场/session适配，不拥有核心Prompt/记忆规则 | 默认普通入口、流式和主动开场全部接真实核心；不丢历史/关窗/晚返回语义 |
| S1 Scene adapter / P2 | ShoutBehavior.ScenePostprocess、Scene主循环/完成回调 | Mission目标/多人接力/旁听/发言队列与展示适配；核心执行一次 | 玩家输入一份、接力/旁听正确、退场不补交旧动作；原第二执行链删除 |
| Q1 Courier adapter / P2 | CourierDeliveryBehavior准备/请求/到达/完成入口 | Persona后主线程捕获，后台网络，主线程接受；运输/到达时机仍归原业务owner | 回信/预生成/到达/主动来信都接核心且无后台live读取；不提前执行资产与事实 |
| V1 展示 / P2 | Shout文本/流式回调、TTS/气泡/播放队列及UI接缝 | 文本可见与真实播放生命周期；FIFO/取消/退场、订阅释放由展示owner | 不在Conversation/Memory里夹音频队列逻辑；区分生成/提交/播放完成 |
| D1 持久化与开发工具 / P1→P3 | MyBehavior.SyncData、OpenDev*、Import*/Export*、配置/诊断入口 | 运行owner持有运行态；旧序列化identity留薄兼容适配；编辑器与导入导出独立服务调用Memory命令 | 大类不保留完整编辑器/导入算法；146键/36行为及Saveable身份不因搬文件改变 |
| I1 内部模块 / P3 | TeamModulePorts/Adapters/Services、InternalModuleDirectory、ModuleFrameworkRuntime、SubModule | 同DLL真实依赖/激活/失败/停止；三组制作组能力只通过typed接缝 | Ready来自真实可用状态；必要释放/组合证明，不重写制作组业务或伪称任意热卸载 |
| E1 外部API / P4 | Api/V1、已有ForExternal公共面 | 版本能力/结果/生命周期/错误边界；复用核心，不透传内部owner | 已选能力外部DLL可实际调用，旧客户端兼容；未选/未做不假成功 |

M/P/V 等包名为本文标签；P0–P6 阶段编号仍以原计划为准，不新增另一套阶段计数。

## 5. 执行顺序：完整职责包交付，而非零散拆方法

### 第一步：B1 / P1，修复并收口 Memory

1. 本轮来源结构安全修复 → 旧红/新绿/故障反例、相邻联验与同源码双版本构建。
2. 首次 capture/copy、trigger整批sanitize、全raw/owner/最终绑定、Apply/public/weekly尾步逐一确定最大原子单元、数量/时间预算和真实writer边界。不能只计回调数量。
3. 在这条真实业务链上提取 M1/M2/M3：owner持有状态，Host只做capture/commit/生命周期接缝；复用已完成组件，不为了“拆干净”重新实现其算法。
4. B1整批出口：过期来源/同代变动/档代/owner/部分失败/重试均保持；实际成本量测有边界，未接受的关键超限为零。未满足不借模块化搬迁掩盖风险。

### 第二步：B2 / P2，完整对话核心与三渠道

1. **优先 Courier 真实线程边界**：persona完成后捕获，网络/解析后台，接受主线程；回信与主动来信均验证。
2. 提取 PR1 上下文/Prompt、G1 LLM网络策略、A1 计划/回执、C1 编排。每包迁完所有已识别真实消费者，保持运行路径唯一，不并存三套核心。
3. N1/S1/Q1 变成渠道适配器，完成 V1 展示接缝；保留渠道本身应有差异，不牺牲Scene多人接力/旁听来凑形式统一。
4. 三渠道按同一功能矩阵校验：上下文/话题→正文→标签→领域执行→真实回执→历史/AFEF→展示，加拒绝/忙碌/重复/取消/超时/晚回包/换目标/读档/Mission结束。

### 第三步：B3 / P3，大类剩余职责与内部模块

1. 完成 D1：把开发菜单、编辑器、导入导出、设置/诊断整理到对应服务或Host adapter，避免主体大类还塞着整套工具实现。
2. 核对全部三组ports的实际callers和ref/out/副作用，梳理必需依赖、启动/失败/停止及订阅/队列释放。
3. 每个模块组合分别验证：普通AF、单模块、A+B无桥、A+B+桥、缺失/禁用/初始化失败/运行失败；GCCZ只在其场景作用。
4. 做全主体“剩余符号清仓”：每个原大类成员归入新owner、薄Host、序列化/ABI保留或明确外域；不能留“以后再说”的核心业务块却宣布模块化完成。

### 第四步：P4，已选 public 能力

- D-A（首版渠道）与D-B（扩展注册/制作组能力公开）仍待明确选择；建议最小完整Native请求链，但本计划不替用户确认。
- 即使API首版只读，也必须写成已选范围，不得把完整请求SDK勾完成。真正实现所选提交/结果/取消/通知后，以独立DLL编译+实际加载验收。
- 等待公开范围不阻塞已经明确的核心/内部职责工作；不能通过降低公开目标来掩盖主体未拆完。

### 第五步：P5/P6，同候选验收与收尾前材料

- 同一最终源码/配置/依赖：Debug/Release×1.3/1.4/Bootstrap、API可见性/ABI、存档身份、clean-checkout可复现构建/包清单。
- 真实Campaign/Mission、代表性旧档副本加载/保存往返、真实资产/债务/AFEF、TTS/provider与所选外部DLL。原来“成员测过”必须绑定具体candidate和场景。
- 交付逐符号删除/兼容保留表、原AF功能对照结果、性能/失败路径、模块组合、回滚顺序、详细/简明HANDOFF。
- 只有达到 **READY_FOR_CLOSEOUT_REVIEW** 才进入最终收尾决策。默认切换、不可逆存档迁移、最终打包发布/推送按明确授权执行。

## 6. 每包必须填写的迁移清单

没有这张表的证据，不算真正完成提取；不能仅用“新建N文件/减N行”交差。

| 字段 | 要求 |
|---|---|
| 原功能/旧符号 | 路径、符号、源码revision；原始有效行为与批准变化分开 |
| 当前入口/全部消费者 | 普通/外部/异步/编辑器/存档等真实callers；不能只查直接方法名漏delegate/Harmony |
| 新owner/状态 | 可变数据谁拥有、线程/生命周期、依赖方向；禁止双份权威状态 |
| 接口契约 | typed输入/输出、错误/取消/部分结果/版本/能力，不暴露私有对象图 |
| 替代与删除 | 新路径真实接线→差异验证→同提交删除已无责任的旧实现；不是只改名搬入Legacy目录 |
| 兼容保留 | 仅列确有Saveable/SyncData/ABI/引擎责任的符号、薄壳内容及替代前提；核心算法不能借兼容名义继续躲在旧类 |
| 行为/成本证据 | 新旧正常与边界、有效旧红/故障反例、数量/时间/分配；枚举器结构版本不等于字段版本或并发集合 |
| 回滚 | 定向inverse/revert与依赖顺序，不能靠hard reset/覆盖他人改动 |

每包先盘点，完整实现它的受影响调用链，然后集中验收；内部允许可回滚提交，但不把半个包报完成。不要同时启动多个相互依赖的大类迁移，单代理顺序推进。

## 7. 必须通过的“模块化完成”门禁

| 门禁 | PASS标准 |
|---|---|
| 责任分离 | 原三大owner每个成员已归属；Host中的业务算法残留为0（明确兼容壳/外域保留单列） |
| 依赖 | 纯领域/算法不引用MyBehavior/Shout/Courier或对方内部状态；避免循环引用和万能context |
| 唯一执行/写入 | 单一权威动作执行与事实接受；无重复LLM、重复commit/AFEF及隐式双写 |
| 旧代码删除 | 替代实现真接线且所有调用/反射/存档兼容已核对；重复实现/死helper/过期flags为0 |
| 三渠道复现 | 默认真实入口全部有完整成功/失败/生命周期对照；opt-in/fixture不能冒充 |
| 配置与扩展 | 主体功能可按需求调整，作用域/默认/校验清晰；不存在业务规则散落三渠道硬编码 |
| 兼容与组合 | 1.3/1.4唯一实现、存档身份/旧档、模块缺失/异常/组合和所选子MOD真实加载成立 |
| 性能 | 热路径不做新增全仓反射/扫描；实际记录和最长原子步骤有量测；不删数据凑预算 |
| 玩家可见结果 | 不串人/串档、不丢记忆/资产、不重复动作，错误提示与真实状态一致，失败后能继续 |
| 交接 | 源码坐标、candidate与产物hash、PASS/NOT_RUN、剩余项和回滚一致，无“历史DONE=当前完成” |

### 允许保留的不等于未拆干净

- 引擎必须识别的 `CampaignBehaviorBase` / `MissionBehavior` 类型、既有 `SyncData`键、Saveable类型身份：薄壳及数据兼容责任可以保留，但核心算法迁走。
- 政策/宴会/GCCZ等成员自有业务：不是本任务删除或全面模块化的对象；本任务只要求AF接缝干净。
- 未确定来源/许可的参考材料和用户数据：继续HOLD，不拿删参考树冒充主体代码清理。

## 8. 原则与后续第一步

本轮修复验收通过后，下一完整工作包仍是 **B1首次capture/copy + 明确writer与最终接受边界**，并按M1/M2的目标提取真正owner；不是直接跳到默认切换或一口气挪走整个MyBehavior。

每次将本表中的包映射回原台账P1/P2/P3/P4/P5/P6，只更新真实完成项。源码/测试通过、结构已提取、实机已验收是三个不同状态；其中一个PASS不能代替另两个。


## 9. 已实施增量：M1/M2线程接受基础（2026-09-15）

- `61d57892`把队列/待办claim-retire/额度与耗时/完成异常迁到独立MemorySummaryDispatcher；新增internal Host契约，原MyBehavior只做引擎适配，规划读取同一owner状态。详见[本轮HANDOFF](../handoffs/2026-09-15-memory-dispatch-owner-handoff.md)。
- 这一子包已接真实capture/writer/planner/completion并离线验证；**M1首次深复制、M2其余规划/summary业务、M3历史/事实仍未全部迁移**。不把一项Runtime提取勾成整包或整个主体完成。
- 下一包仍首次capture/copy + source/writer/接受一致性；利用独立dispatcher时间读值，不再向MyBehavior增加另一套队列/额度。公共/内部模块边界保持，未恢复自动化/部署/推送。
