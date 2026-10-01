# AF 2.0 主体职责最终收官计划

日期：2026-10-01。状态：**PLAN_READY / IMPLEMENTATION_NOT_STARTED / FINAL_SCOPE_NOT_FROZEN**。

本文件按用户要求独立重写，不往旧 J 计划追加内容。本轮只交付计划，不实施产品改造。

## 1. 要解决什么，何时算完成

目标不是让所有文件变短，而是兑现原定的 2.0 模块化标准：

> AF 业务规则、运行状态及业务生命周期由所属模块承担；生产消费者使用唯一权威实现；MyBehavior、ShoutBehavior 等混合宿主只保留有证据的引擎接入、线程适配、保存身份和兼容门面。以后修改某项规则，主要修改所属模块，不再跨两个大宿主修改同一业务。

**仅拆完目前发现的五组，不能无条件保证全主体达标。** 五组是已确认的最低施工范围，不是已经核算过的全量剩余清单。本计划通过 F0 的差集核定和 F6 的终态检查补上这个缺口；两道门都通过，才可以宣布“主体职责与离线验收完成”。实机、旧档及发布分别签收，不混成一个 DONE。

不新增第二套框架，不从头重跑 J，不要求旧类消失，不承诺以后永远没有局部重构或零 BUG。交付承诺是：不能再把本次已知、属于原目标的核心耦合留给下一场全面重构。

### 1.1 与旧材料的关系

- 本文是获准继续后的新实施规格；旧 J 总计划、J17 计划和回执保留历史，不修改、不重写当年的结论。本文不继承其中的执行、删除、推送或部署授权。
- 复用旧 J 已验证的实现和证据，不把同一个已完成职责再拆一次。
- 本文不是第二份执行进度台账。将来实际施工仍按仓库规则在已有主台账记录证据、由 HANDOFF 链接；**本次只新增本文，不修改旧计划、主台账或 HANDOFF**。
- 若新实施规格与已批准的保存、行为或 API 合同冲突，以保持合同为先；不能凭本计划偷偷改玩法或弱化验收。

## 2. 基线、保护范围与事实边界

- Git 根：`E:/AnimusForge-refactor-continuation-20260831`。
- 分支：`codex/af-main-refactor-continuation-20260831`。
- 已核对产品基线：`d57e9be23788348a9e8885bae5c4b7950783662e`。规划意图检查点：`0ff37a8e`；没有产品变化。
- 原有 `CourierDeliveryBehavior.SessionTransport.cs` 显示修改，另有未跟踪 tools/NuGet 等目录；均不清理、不暂存、不覆盖。恢复施工必须重新检查，而不是假定这份快照永远有效。
- 同源码双 API 1.3/1.4、单 AnimusForge 模块、Bootstrap 唯一版本选择、程序集/保存类型/SyncData/JSON/API 身份保持。
- Policy、GCCZ 等制作组内部玩法不是本次重写对象；AF 侧接缝仍须核对。新外交及其他后续功能只核对本次受影响接缝，不借收官重写其产品业务。
- 不改一键构建、默认渠道入口或发布方式；不写游戏、真实玩家数据、其他工作树或 G 盘镜像；不安装、推送、部署或批量删除。
- 历史日志曾有凭据暴露记录，轮换状态未确认；本计划不读取或复述秘密，不把代码修复等同凭据风险解除。

### 2.1 本次确认的源码锚点

以下一基行号绑定上述产品基线。施工先按符号重定位；不要求为行号变化修改行为。完整历史证据仍在已有代码范围图，不在本文复制整个 E 表。

| 包 | 当前实现与生产消费者 | 已有成果与真正缺口 |
| --- | --- | --- |
| F1 Kingdom | [MyBehavior](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L11626)：`ComputeKingdomRebellionCandidateScore` 11626、`ResolveKingdomRebellion` 11809；评分消费者 11802，解析消费者 12418/34568/34585 | Stability/Scheduling 已有模块；候选、排序、概率判断和选择仍含宿主业务。当前是概率门后选排序首个合格候选，**不是另造加权随机选人**。 |
| F2 耐心 | [MyBehavior](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L32867)：状态表 1771、`GetOrCreateStateUnsafe` 32867、恢复 32886、增减 33224、后处理纠偏 33248、关系规则 33108；[Scene 消费](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L10557) 10557/10650/26813/26910 | 存档/人物访问可以保留；耐心算法、状态转换和同步责任不能以保存兼容整体豁免。 |
| F3 Memory | [MyBehavior](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L5157)：`ApplyMemorySummarySuccess` 5157–5195，实际接受回调 4543；`RetargetMemoryQueues` 24413–24469，消费者 24210 | Records/Summary/ImportExport 已真实抽取；[Accept](../../src/modules/AF.Module.Memory/Summary/MemorySummaryAttemptRunner.cs#L13) 仍回调宿主业务提交，成功后的多状态写入、队列迁移和跨域通知未全部闭合。 |
| F4 Weekly | [MyBehavior](../../src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs#L38007)：重试 38007、解析 39105、wave 调用 40762、通知 41399；Tick 消费通知 18862 | 材料、wave、commit queue 等成果保留；生成规则仍在宿主，通知仍有 tick 下集合净化/复制/扫描。 |
| F5 Conversation | [Courier 调用](../../src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs#L435) → [Shout 准备](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L22228)；[公共后处理文件](../../src/modules/AF.Module.Conversation/Internal/Postprocess/ShoutBehavior.UnifiedActionPostprocess.cs#L21) 仍为同一 partial；[Native 调用](../../src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnCommit.cs#L87) | 公共规则/编排仍依赖场景类型；Native 会话历史 13820 及待注入投影要有明确渠道 owner，不混入 Memory 持久权威。 |

### 2.2 明确不重复施工的内容

- [Courier 主消息](../../src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptMessages.cs#L41) 已接 `MainPromptMessageAssemblyOwner`；不能沿旧报告再说“全部留在 Courier”。是否还有共同语义重复，在 F5 按实际输出差异核定，不重写已共享部分。
- [后处理准备](../../src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs#L22256) 已合并多项前处理命中；不能只因两调用端写法不同就重复修复“漏命中”。
- [MemoryImportExportOwner](../../src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs#L27) 已直接操作同一权威状态视图，不是空转发壳；不重做导出格式/UI。
- `Refactor/` tracked 文件已归位；群聊不可达尾段已有精确删除；旧公开 opt-in、Xihai 反射入口不能当死代码批量删除。
- 已有 LLM transport、Prompt 配置/检索、Actions 执行和回执、内部 ports、公开 V1、静态内容及测试归位，不因宿主仍大而整体返工。

## 3. 达标标准：六个不可替代的条件

| 条件 | 验收要求 | 不能冒充通过的证据 |
| --- | --- | --- |
| S1 业务归属 | 原目标内每个剩余责任有真实所属模块；宿主不再决定该业务规则/状态转换 | 类名叫 Owner、移动目录、拆 partial、登记 owner 名字 |
| S2 唯一状态 | 每组可变业务状态只有一个生命周期管理者；保存适配可持同一容器引用，不保留第二份活状态 | 新旧字典并存、双向同步、只把锁搬走 |
| S3 真实接线 | 默认、手动、自动、重试及兼容入口都进入同一权威业务；新模块不回调宿主求解完整业务 | 只有测试调用新 owner、外壳再调旧算法 |
| S4 行为保持 | 规则数值、随机消费顺序、三渠道语义、部分成功、取消/迟到、保存及 ABI 按具名合同验证 | 仅构建成功、刷新 hash、降低断言或忽略相关旧失败 |
| S5 性能与线程 | 游戏读写在所属线程；后台输入脱离活对象；按实际记录/字符/任务量检查预算，热路径无新增全扫/反射/忙等 | 每帧 N 个回调、只限制输出 Take(N)、没有实测却声称无卡顿 |
| S6 维护边界 | 用五组各一项代表性规则变更做只读影响分析，核心变更应落在其模块；宿主最多改具名适配契约 | 为演示额外改玩法、要求所有文件低于某行数 |

允许保留：事件注册、Harmony/引擎约束、所属线程捕获/应用、保存身份、真实 UI 呈现、必要公开/反射门面。每项必须写出符号和理由。**“兼容”“主线程”“后来再拆”不是整段业务的保留理由。**

同一职责在模块内部使用 partial 并不违规；跨渠道公共职责若仍共享场景宿主私有业务字段，则不能仅靠换文件过关。

## 4. F0：一次性核定最终范围，不先拆完再发现漏项

### 输入与动作

1. 复核 Git/dirty、适用 Skill 和当前真实 Compile 输入；不恢复旧路径、旧作者改动或历史任务权限。
2. 复用原 J17-A 的 20 桶、逐成员证据及后续提交。只补查源码变化、旧保留项和未闭业务回调；不另建第二套全仓审计平台。
3. 对原 J 范围做差集：**已真实闭合 / 必须保留的适配 / 原授权排除 / 仍需迁移 / 证据不足**。不能把“已分配 owner”直接当“算法已迁”。
4. 至少覆盖两个大宿主的全部 partial、共享接缝，以及原 J 中仍标保留/OPEN 的其他混合宿主责任。独立且内聚的领域 Behavior 不因大而自动拆；但也不因位于模块目录就免审。
5. 对五组记录方法簇、字段/嵌套类型、静态/动态/保存消费者、唯一写者、捕获与提交点、频率、现有测试和旧入口退出条件。用完整职责成组，不为每个 helper 创建任务。
6. 核定旁支：叛乱命名/Prompt 等是否仍有同域未迁业务；Memory 其他成功/失败及合并路径；Weekly 自动/手动/首周/全文；Conversation 默认/detached 和兼容入口。本文已读到的样例不是这些旁支已审完的证明。

### F0 出口

- 差集内没有未分类、匿名待办或无理由的“保留”；每项落实到 F1–F5，或成为具名新增包。
- 如果出现五组之外的实质欠账，**在产品开工前报告范围、依据和新增预算，取得范围确认**；不隐瞒、不把它塞成无关优化，也不静默扩成第二场 J。
- 冻结责任清单、必需测试和验收口径。F0 完成不代表产品完成。
- 后续仅因新的相关运行证据、实际回归或真实漏项重新开门；文件行数、还能细分、可再加一个测试不是扩项理由。

## 5. 实施顺序与责任包

默认单执行者按 `F0 → F1 → F2 → F3 → F4 → F5 → F6 → F7` 推进。当前请求只授权写计划；不得据此启动实现、代理或自动化。

F1 是首个边界较清楚的验证切片；F2 固定对话状态接缝；F3 固定 Memory 的唯一接受/通知边界后，F4/F5 接入。每包尽量只分“规则/状态迁移”和“全消费者/回归收口”两个有意义切片，不为每个小函数建立阶段。

### F1 — Kingdom 候选与选择职责

- **目标位置**：已有 `src/modules/AF.Module.Kingdom/`；扩展现有职责结构，只有无合适承载者时新建窄内部实现，不新建 DLL 或通用服务。
- **实施**：主线程捕获候选所需的声望、等级、兵力、领地、关系、文化和资格事实；模块承担资格决策、评分、排序、触发与选择。真实家族/领地变更、存档和 UI 留适配。
- **随机边界**：沿用原随机源，惰性取样并保持消费次数/先后；不能提前采样改变整局随机序列。测试用确定随机输入验证原比较与排序 tie-break。
- **消费者**：自动周度、预览、强制/手动入口分别改接；名称/游戏对象采集不在后台运行。F0 确认的同域旁支不能因首个评分函数已迁而遗留。
- **频率**：周触发/手动请求；每次捕获候选一次，复用请求内事实，不新增每帧王国/家族扫描。
- **退出门**：评分边界、同分顺序、无候选、免疫/MCM、强制与正常概率门、跟随者和失效目标回归；规则变化不再需要修改 MyBehavior 核心算法。
- **复用验证**：[Kingdom DomainOwners](../../tests/modules/AF.Module.Kingdom/DomainOwners/DomainOwners.csproj) 及已有 Kingdom production replay。补到本包实际迁出的候选逻辑，不能只运行旧稳定度测试。

### F2 — 耐心规则、状态和结果应用

- **目标位置**：已有 `src/modules/AF.Module.Social/`；Conversation 调用窄状态服务，不让 Memory 或 Foundation 接管社交规则。
- **实施**：把最大值/恢复/扣减、NoInterestRounds、mood override 和耐心状态生命周期放到同一业务 owner。关系、私人好感、忠诚度变化先产生保持原语义的结果，再由各自权威游戏执行端应用。
- **状态/保存**：复用同一状态容器，明确锁与载入替换、退役清理；必要嵌套类型和 SyncData/JSON 身份可保留原位，通过窄映射接入。不复制第二张表，不把整个状态表公开给渠道。
- **消费者**：自由对话、Scene Hero/非 Hero、后处理 mood override、UI 快照、载入/保存入口全接入；保留原来各渠道的合法差异，不自动给信使新增耐心扣减。
- **频率**：对话/查询时按 key 恢复，保持常数级查找；不增加全角色每日或每帧恢复扫描。
- **退出门**：跨日恢复、饱和上下界、连续无兴趣、neutral→mood 纠偏不重复扣减、耗尽/拒绝、Hero/非 Hero 身份、存读与退役均有生产实现回归；关系效果仍由原权威执行端执行一次。
- **复用验证**：NativeTurn、ScenePostprocess 和社交 outcome 测试。若不存在耐心规则直接测试，只补一套聚合回归并接总 runner，不为每个 mood 建 harness。

### F3 — Memory 提交、身份迁移和业务状态闭合

- **目标位置**：`src/modules/AF.Module.Memory/`，复用 Records/Summary/Recovery/ImportExport；保存 I/O 继续由现有 Persistence 承担。
- **实施**：将 daily/major/overview 成功接受后的业务写入、失败状态和任务移除/排队归 Memory；关联身份合并/重定向进入同一状态权威。宿主只捕获资格/游戏事实、校验当前会话并执行具名外部效果。
- **关键设计**：Memory 直接操作唯一状态及业务顺序，不把完整 `ApplyMemorySummarySuccess` 当 delegate 原样回调宿主。通知 Native 清理、Social 公共记忆、Weekly 材料使用窄接缝；不新建通用事件总线或第二套提交账本。
- **保持行为**：保留现有块替换、草稿/队列删除、跨域通知及 overview 排队先后。不得把已有部分写入虚报事务成功或持久 exactly-once；如果发现实际一致性缺陷，单独复现并明确修复语义，不借抽取偷偷改政策。
- **身份/线程**：主线程原地净化、worker clone、别名、旧 JSON/键、generation/source 验证和旧恢复账本保持；保存适配引用同一容器不等于继续允许宿主写业务。
- **频率**：每次摘要完成/导入/身份合并；复用现有预算/索引，审查单回调内记录数，不增加每 tick 深拷贝或全队列遍历。
- **退出门**：三摘要成功/失败、同代来源修改、读档/换 owner、迟到/重复回调、跨域通知失败、身份合并及队列重定向回归；`Accept` 后不再回到宿主执行本包业务求解。
- **复用验证**：[J17SummaryOwnerTests](../../tests/modules/AF.Module.Memory/J17SummaryOwnerTests/run.py)、[MemorySummaryMainThreadBoundaryTests](../../tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/)、[MemorySummaryBudgetTests](../../tests/modules/AF.Module.Memory/MemorySummaryBudgetTests/run.py)、MemoryCommitRecovery、J17ImportExport、保存契约及 Native 历史回归。相关 captured/sealing 和完整历史 inverse 旧失败须诊断，不能用有限 15 项代替整个受影响链。

### F4 — Weekly 生成、解析、重试与通知

- **目标位置**：已有 `src/modules/AF.Module.Weekly/{Generation,Materials,Scheduling}`；不重建 wave、材料聚合和通用 HTTP。
- **实施**：迁入批量响应解析、预期报告身份/缺失项判定、尝试次数/限流等待、失败与部分结果决策；自动、手动、恢复及按需全文入口用同一职责。宿主提供已冻结 Prompt/目标事实、网络窄调用与主线程效果应用。
- **状态**：复用现有 wave/commit queue/revision owner，明确新增职责与旧 state 的单一管理者；禁止两个 retry loop、两个提交队列或新旧报告表双写。
- **性能**：通知显示留 UI 适配，但“还有哪些通知需要处理”归业务 owner。按发布/已读/载入使缓存或队列失效；空闲 tick 不再重复净化和复制全部未读集合。分批消费需界定实际记录量，保持通知顺序与已读语义。
- **退出门**：正常/缺块/重复块/非法或不匹配身份/部分结果、限流、用尽重试、取消/晚回包、源材料改变、手动恢复、自动/全文路径回归。仅发现规则缺陷时另定批准变化，不以测试“更合理”替换原行为。
- **复用验证**：[WaveCoordination](../../tests/modules/AF.Module.Weekly/WaveCoordination/WaveCoordination.csproj)、WeeklySchedule/MaterialOutcome、[J17WeeklyAggregationRules](../../tests/modules/AF.Module.Weekly/J17WeeklyAggregationRules/)、生产 Weekly replay。新增解析/重试及通知积压用例接真实 owner，不拿材料聚合通过代表生成通过。

### F5 — 三渠道公共业务脱离场景宿主

- **目标位置**：共享编排在 `AF.Module.Conversation/Internal`；Prompt 装配在现有 Prompt owner；标签协议在 Actions；Native/Scene/Courier 各有明确渠道状态 owner；游戏捕获/效果仍在适配边界。
- **F5a 公共后处理**：把规则命中/资格合成、请求准备、响应归一化与共同完成规则从 Shout 类型依赖中分离。现有含 Hero/Character 闭包的 work item 不是 detached DTO；捕获与归一化所需 live 操作留受保护主线程，worker 只接真正脱离对象的网络/文本输入。
- **F5b 渠道状态**：Native 会话历史、tentative 玩家事件和待注入 AFEF 归 Native 会话 owner；这不是把 Memory 的持久提交搬进 Conversation。Scene 保留群聊/旁听/接力状态；Courier 保留运输、预生成、到达和来信时点。
- **F5c 共同语义与实际入口**：默认/detached/公开/反射兼容路径改接共同阶段；不擅自切默认 gateway 或删除活的 opt-in。允许外层触发方式不同，不允许相同业务由两套算法决定。规则差异通过具名渠道输入表达，不散落复制布尔表。
- **标签边界**：动作执行 parser 与可见文本 strip/count 用途不同；不强行让显示过滤执行完整动作解析。共享相同标签协议/规范化语义，必要显示策略留渠道并说明差异，不因“已有 parser”就要求所有正则消失。
- **消息边界**：复用 `MainPromptMessageAssemblyOwner` 和已有 role 分类，不恢复已迁代码。检验共同 block、历史角色、AFEF 和事实窗口；信件事实/场景事实允许不同，不能要求三渠道正文逐字相同。
- **频率**：按回合/请求捕获；会话按 key 管理，沿用原限制和退役，禁止用共享化引入每帧全场景/全角色扫描。
- **退出门**：三渠道同类 hit→规则→标签→唯一执行→可见输出→AFEF 回读都有真实生产覆盖；群聊 primary-first、旁听、玩家输入去重、预生成不提前提交、来信恢复、取消/迟到/重入保持。Courier 和公共模块不再依赖 Shout 的核心业务实现或私有会话状态；兼容门面可保留但只有适配责任。
- **复用验证**：[CourierPostprocessOwnerRegressionTests](../../tests/modules/AF.Module.Conversation/CourierPostprocessOwnerRegressionTests/)、[ScenePostprocessParityTests](../../tests/modules/AF.Module.Conversation/ScenePostprocessParityTests/)、NativeCompletion/NativeTurn、NativePendingHistory、SceneGroupReceipt、InteractionRequestLifetime、CourierPrompt/Commit/Inbound、J09DefaultChannelActionWiring、三渠道公开提交和实际 DLL message/history 回放。

## 6. F6：一次集成终验，防止第二次假收官

### 6.1 结构与职责

1. 回读 F0 冻结清单：每项要么真实迁移并接通，要么有逐符号必要适配理由；范围内未分类、未接线及未验证的必要职责为零。
2. 对两个大宿主所有 partial 和 F0 标出的其他混合点，审查剩余业务回调、状态写者、动态/保存消费者；不能只 grep 新类名，不能把某个大文件整体称为薄壳。
3. 每个保留 wrapper 说明：兼容消费者、所属线程、捕获/应用责任及为何不能直接去掉；不为缩短文件破坏 ABI/旧档。
4. 做 S6 的五项维护影响检查；只读分析，不额外实施新玩法。完成的是可维护的责任边界，不是某个行数目标。

### 6.2 离线验证

- 每完整切片跑受影响回归与双 API 编译；共享合同、保存或装配变化影响面不明时跑相关集成。Bootstrap 按受影响范围验证，并纳入最终 Debug/Release × 双 API + Bootstrap 矩阵。
- 最终冻结同一产品候选。复用已审安全入口跑当前候选全量离线 runner，区分 PASS、FAIL、PREEXISTING_FAIL、NEEDS_INPUT、环境及人工工具；不能用“43 项通过”代称全量。
- 当前包相关必要旧失败必须解决或给出等价完整证据；无关历史失败单列，不无限拖回重构。不得刷新源码 hash/expected 抹红、删除有效断言，或把编译失败当行为反例成功。
- 两 API Compile/资源无遗漏、无双编；保存/公开 API/反射身份、地图定位和链接检查通过。实际 DLL 与纯 fixture 的覆盖分别报告。
- [tests/run_all.py](../../tests/run_all.py) 要求显式仓外合成 TEMP；历史许可不自动继承。执行前获取精确根和写入范围许可，不通过真实用户目录或读取凭据环境绕过限制。当前构建/runner 只做了必要入口阅读，**未全面复核全部传递副作用**。
- 保留现有 [单模块构建入口](../../scripts/build/build_single_module.ps1)，先核实实参、引用来源和输出/清理副作用再执行；本计划不提供带默认 `-Stage`/`-Deploy` 的一键命令，也不授权改脚本。

### 6.3 结果措辞

F0–F6 全通过才写：**“本计划冻结范围内的主体职责重构与离线验收完成；原 J 范围差集已闭合；实机/旧档状态另列。”**

如果五组已完成但 F0/F6 还有原目标欠账，只能写具名包完成，不能写 2.0 架构收官；更不能临时把欠账改名为未来优化。

## 7. F7：实机、旧档与发布是独立门

需明确版本、游戏路径、代表性旧档、备份/恢复及写入范围授权后执行：

- 两条受支持 API 线检查真实 Bootstrap 唯一加载、Harmony、进入/退出战役。
- 新战役与代表性旧档完成 load→五组关键机制→save→reload，覆盖记忆、耐心、周报、叛乱及三渠道事实。
- 三渠道正常/失败/取消/重入、群聊/旁听/信使到达、UI/音频和晚结果无重复效果。
- 记录真实帧表现与积压；没有测量就不承诺无卡顿。必要测试失败回到具名包，不重新推翻全部 J。
- 部署、打包、推送、素材/依赖许可和凭据风险分别核准；LIVE/SAVE 通过也不自动授予发布权限。

没有实机条件时可以交付已完成的离线架构结果，但保持 `LIVE/SAVE NOT-RUN`；不为了等待游戏验收无限重开已闭代码包。

## 8. 工作量与范围控制

- 当前低置信度预算：五组产品迁移加定向/集成回归约 **10–20 个有效工程人日量级**；理解为约 2–4 周单人等效工作，不是 AI 连续运行时长或交付日期保证。
- Conversation、Memory 是主要风险和工作量来源；Kingdom/耐心通常较小。缺环境修复、真实游戏测试排期、新功能及 F0 新发现的额外职责不包含在此初估。
- “整个 J 的约三分之一”仅是对话中的粗量级比喻，没有可信的历史工时分母，**不能作为完成比例、考核值或固定工期依据**。
- F0 核定后只更新一次基于实际职责与测试闭包的预算；每包通过退出门立即进入下一包。不为目录美化、全量死代码清理、改命名、拆所有大类或更多通用抽象追加工作。
- 实现需要规则/默认行为变更、外部写入、广域删除或超出冻结范围时，先说明影响并取得相应确认；普通获准包内的窄编辑和验证不用每步请示。

## 9. 获准实施后的交付要求

- 遵守仓库本地意图检查点与已验证切片提交；只暂存本包明确路径，保护原 dirty/untracked，不使用 hard reset 或历史改写。
- 每包交付：真实 owner、全部消费者、旧实现去留、状态/线程/频率、测试结果与盲区、相关修订、下一包。详细证据沿用主台账/代码范围图，本文保留实施规格而非复制进度流水。
- 不新增常驻自动化，不自动开子代理或新 chat。需要并行时另有明确授权并划分唯一写者；共享构建/索引不得抢写。
- 最终只保留一份真实结论：结构、职责、离线、实机、旧档、发布分开签收。满足原目标后停止专项重构，转回功能开发。

## 10. 本次计划交付验证与参考

本次只新增计划文档。核实了当前 Git、保护文件、具名生产符号/消费者、测试路径及相关 runner/构建入口；不将阅读入口误称已运行测试。没有运行产品构建、业务回放、真实 provider、游戏或旧档。

计划交付检查：相对链接和源码行号/符号定位、必须退出门、Markdown 差异检查、旧计划/台账/HANDOFF/原 dirty 文件保护核对；具体执行结果以本次交付说明为准。

参考（历史成果和规范，不自动恢复其执行权限）：

- [原 2.0 最终目标、保留边界和 J17 退出门](../animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)
- [既有代码责任范围图](../architecture/af-framework-code-scope.md)
- [旧 J17 实施规格](j17-responsibility-closeout-plan.md)
- [三渠道对齐](../free_conversation_scene_shout_alignment.md)
- [指令标签输出案例](../directive_tag_output_case.md)
- [Bannerlord 双 API 兼容](../bannerlord_1_3_to_1_4_5_compatibility_diff.md)
