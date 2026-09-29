# J17 主体职责收尾与分层验收执行计划

> 编制日期：2026-09-29；编制基线 `944712f8`。本文是执行规格，不是完成回执，也不授予产品实施、外仓写入、部署或推送权限。恢复任务时重新核实 Git，不按历史盘符切换工作区。
> 当前进度与责任状态唯一见[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)及其 R01–R09 登记表；源码证据见[范围图 E01–E61](../architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)。本文不建立第二份状态表。

**当前执行顺序（2026-09-29 用户更正）**：用户已授权执行仓内 J17，必须先满足第 4 节的 **J17-A 出口**，再进入第 5 节 J17-B 并收尾；下方编制时“待授权再进入产品迁移”的可复制提示不再代表当前授权状态。此授权不扩展为仓外写入、游戏部署、真实玩家资料改写、推送或高风险清理许可。

## 1. 承接现状与目标

- J16 a–e 已离线收口，不重新执行测试搬迁、Bootstrap 或构建脚本迁移。历史全量结果是 257 项、209 PASS、38 PREEXISTING_FAIL、6 NEEDS_INPUT、3 SUPERSEDED_BY_RUNNER、1 ENV_STATE、0 FAIL，不是全部测试通过。
- J17-A 已有 R04 保存边界、R02 摘要、R01 Weekly、R03 导入导出的初步闭包。E09–E13 基于 `99ca85ae`，不能直接作为当前源码的完成证据。
- 旧盘点的 940 个编译文件、10,704 个混合宿主成员、3,332 个残余只是旧基线数字。7,372 个按名称初分的成员也须核实方法体、状态和消费者；不能只清零 residual 就称全量审查完成。
- 目标：范围内算法、权威状态与生命周期都有真实 owner，全部生产消费者接通；宿主保留有证据的引擎/线程/保存/ABI 适配。允许旧类和必要边界存在，不以行数、文件数或目录清空判完成。

## 2. 范围与安全约束

1. 保持同 DLL 内部 typed 接缝与独立子 MOD 的版本化 public API 分离；不新增通用 Host、第二套 LLM/记忆/动作核心或无消费者接口。
2. 保持一套源码、1.3/1.4 双实现、Bootstrap 唯一加载和单模块输出；不改一键入口、部署语义、程序集和存档身份。
3. 保持玩法、Prompt/标签、MCM、三渠道历史/AFEF、失败与部分成功语义。确认的行为缺陷先登记复现、影响及批准变化，不借迁移暗改规则。
4. Policy/Gathering/GCCZ 制作组内部玩法与状态机沿主台账排除，仅审 AF 接缝；新增集成功能不得从实际 Compile 分母消失，但不因此授权重写其业务。
5. 不写真实玩家资料、旧档或游戏目录；不批量清理旧 `.generated`、忽略材料或未跟踪文件。外部写入、目录移动/批量删除、发布及历史治理按实际范围另获明确批准。
6. J15 已按用户决定结项；实机/旧档和素材发布遗留继续保留。不重开默认提示词覆盖保护、ONNX 安装方案或已完成领域 owner。
7. 编制轮仅写计划；当前用户已授权仓内实施，但先 A 后 B，A 出口前不启动 B 的产品迁移。当前工作区优先，历史 G: 工作树不构成切换指令。

## 3. G0：恢复检查与基线准备

### G0.1 必读与现场确认

- 读根 AGENTS、维护 Skill、框架 Skill、HANDOFF 当前段、主台账当前入口及本文；按实际变更加载三渠道/保存/场景/兼容案例。
- 执行 `git rev-parse --show-toplevel`、`git branch --show-current`、`git rev-parse HEAD`、`git status --short`；保护其他作者改动。不要自动 fetch、切分支、merge 或清理。
- 核对相对 `944712f8` 的新增源码，尤其 MyBehavior、Shout/Native、IntegratedModuleHost、显式编译扩展、项目与测试引用。
- 实质产品修改前做仅包含本包的本地意图/检查点；随后按已验证完整责任单元提交，不 `git add .`，不 hard reset/rebase/force-push。

### G0.2 验证入口和运行环境

- 阅读 `tests/run_all.py`、`tests/runners.json` 与本包 runner；核实 SDK、参考程序集和环境变量的实际路径，不沿用默认 G: 路径。
- 总 runner 默认 TEMP 在仓外，部分测试会写合成资料或清理 fixture；运行前逐项确认写入和清理范围。若需要仓外合成根，先取得该精确路径的批准；不得用真实玩家根代替，也不得强放仓内以绕过 DataPaths 防护。
- 先取得相关测试的迁移前结果，记录实际失败信号。既有失败单列，不能通过降级 expected 状态或刷新 hash 隐藏新增回归。
- 文档任务只验链接与差异；A 阶段可做受控 Compile 求值，不要求运行游戏、全量构建或部署。

**G0 出口**：当前根/分支/修订、dirty 保护范围、可运行工具链、相关基线和外部写入限制明确；环境缺失只阻塞所依赖的门禁。

## 4. J17-A：补完全量职责盘点

### A0 更新真实编译输入和旧证据

1. 读取当前项目及实际导入，分别求值 `BannerlordApi=1.3`、`1.4` 的 Compile 集合；记录参数、引用来源和两集合差异。不能直接沿用旧 940，也不能只用 `git ls-files *.cs` 代替编译输入。
2. 检查旧 `artifacts/j17a/` 材料是否存在。当前计划编制时未找到 `host-member-buckets.json`；材料缺失时重建只读清单，不假造已恢复旧结果，不写历史外仓。
3. 新清单覆盖文件、类型、字段、属性、方法及全部 partial；条件编译、生成代码、表达式成员等可能造成提取遗漏，要同源码交叉核对。启发式名称分类仅作导航。
4. 复核 E09–E13 的符号/调用链并更新源码坐标；涉及语义变化必须补行为证据，不能仅换 hash。详细输出留仓内 ignored artifacts；足够接续的成员分区、责任范围和结论进入已有范围图/主台账，不能只依赖未跟踪 JSON。

### A1 先更新已有闭包

依次复核 R04 → R02 → R03 → R01：

- **R04**：JSON 字段/类型/默认值、serializer 设置、SyncData 键、Saveable 注册、反射/动态引用及 clone/原地净化语义。旧结论“11 个记录类型可迁”是待当前源码重验的候选，不等于真实旧档通过。`static`/不读实例字段不等于引用透明：当前部分 `Sanitize*` 依 `TWParallel.IsMainThread()` 在原地修改与克隆之间切换，施工必须保持这层线程/别名契约。
- **R02**：daily/major/overview 的捕获→规则→生成/重试→接受；标明状态唯一写者、owner/generation、来源变化和 Native 清理接缝。开发者编辑器按 UI 适配另分 R02-dev。
- **R03**：Build/Apply 导入导出及全部消费者，区分通用文件 I/O、Memory 权威状态与 dirty 索引写入、UI 选择与提示；确认没有双份状态。当前 Build 在主线程净化时亦可能修改原记录，不能假设快照无副作用。
- **R01**：Weekly 的数据类型、静态规则、aggregate 构造与真实回调；Hero/Clan/Kingdom/Settlement 显示名及死亡关系的传递游戏读取都须明确捕获边界，其余 Weekly 簇归 A2 而非漏掉。旧“仅 DeathLine 读 Hero”不成立。

### A2 完成 R07 的 20 桶和 R05/R06/R08 闭包

按风险与依赖分组推进，不按每个 helper 新开包：

1. MyBehavior 的剩余状态、导入导出、社交/周报、公共 LLM 调用与宿主编排。
2. Shout/Native、Courier、Proactive、Encounter：真实入口→资格→Prompt/history→后处理→动作/显示→AFEF 回读与释放；覆盖群聊接力、旁听、玩家输入去重、预生成、重试、取消及晚结果。
3. Reward/Economy、Vassalage、Duel、SceneTaunt、MilitaryExercise、TroopInspection、Notoriety、Romance 及其他实际领域；不得把未审 AF 业务统归“制作组”。
4. Knowledge、配置、UI、运行时/诊断、装配及其余编译输入；新增四功能集成核对 AF 接入与生命周期，制作组实现标清范围。
5. Refactor 中每个活动文件列真实 owner、Compile/测试/反射引用和目标位置；纯结构迁移与算法抽取分别登记，不当作废文件批量删。
6. 对以上各组交叉检查 Harmony、反射、事件订阅、保存入口和动态消费者；每个高频路径写触发频率、单次实际 job/record 量、缓存/分批与释放策略。

每个责任单元在主台账沿用 R 编号或新增子项，必填：当前符号与状态、全部消费者、目标 owner、保留边界及理由、依赖、行为约束、性能、有限测试/反例和退出门。分组必须附可对账成员范围。

**A 出口**：当前实际编译成员无遗漏/重复权威归属；20 桶全部完成语义复核；未知成员为 0；每个余项具名。允许业务 `OPEN > 0`，但不再存在未审的匿名大包；此时只能说“盘点完成”，不能说“迁移完成”。A 完成前不批量启动 B。

## 5. J17-B：按依赖完成真实业务迁移

下表是依现有证据确定的施工顺序；A 若发现真实依赖变化，先更新主台账与本规格，不默默跳项或增加全仓重写。

| 切片 | 具体动作与保留边界 | 必须满足的有限退出门 |
| --- | --- | --- |
| B1 / R04a | 当前闭包确认后的记忆记录及 Sanitize 规则归 Memory/Records；线程选择/克隆边界与记录变换分别明确，不把原地改写误称纯函数。R04b 的保存键/注册和必要编解码接入保持。按消费者必要性确定可见性，不随意 public 化。 | 旧 JSON 形状/字段类型与默认语义冻结；往返、空/坏输入、clone 与原地语义回归；现有反射测试全部改接真实新 owner；双 API 构建。 |
| B2 / R02-rules | 摘要规则、Prompt 构造和纯解析接入已有 Memory owner，不重造 dispatcher/HTTP。 | daily/major/overview 同输入输出约束；AFEF 原文、公开/私密和自称语义不变；旧宿主不再持有已迁规则的第二实现。 |
| B3 / R02-accept | 捕获留主线程，后台只持有脱离游戏对象的数据；重试与接受编排归明确 owner，Native 清理保留窄适配。 | 强制异步完成、同代来源变化、owner/generation 失效、取消/迟到、重复回调与唯一提交；状态不双写；预算和两 API 回归。 |
| B4 / R03、R02-dev | Memory 接管导入应用/业务快照、同一权威状态和 dirty 通知；UI 只作选择与显示。开发者编辑器去业务依赖，不借机换 UI。 | 单人/整组、覆盖/合并、空/坏内容、队列/overview 与主线程净化别名语义保持；实际数据不作 fixture；UI→真实 owner 接线可追踪。 |
| B5 / R01 | Weekly 类型/规则/aggregate 最小闭包归已有 owner；全部游戏名称、地点、死亡关系查询在主线程捕获事实，不只处理 DeathLine 的一个 Hero lookup。 | 自动/同步、全文/短报、掠夺各结果、多事件顺序、类别归并/关联 ID/来源计数保持；旧回调不再执行已迁算法。 |
| B6 / R05 与 A 新增子项 | 一次一个已确认的渠道或领域余项，按输入和唯一写者依赖排序；已闭 owner 不重做。 | 全生产消费者接线；正常/失败/取消/重入及跨渠道事实语义；性能、线程和实际领域回归；不得用薄转发壳报迁移。 |
| B7 / R06 | 按已确认归属迁活动 Refactor 文件，保留必要 namespace/ABI；读取全部引用后再确定精确移动方案与授权。 | Compile 无漏编/双编，测试/反射/资源消费者改接，双 API 通过；不产生第二核心，旧实现退出条件有证据。 |

R08 性能随每个切片验，不压到最后：区分请求/事件/每帧/批处理，检查单回调内部记录量；不引入热路径全量扫描、重复反射、无界积压或失控日志。帧耗时留 D 实测，不用 callback 数冒充工作量上界。

每片统一完成顺序：核对闭包与基线 → 有限测试/关键反例 → 最小迁移与全部消费者改接 → 相关回归和双构建 → 检查旧实现/状态 → 更新主台账及范围图 → 本地提交。满足出口即进入下一片，不为每个 helper 复制 harness。

### 已知遗留如何纳入

- `IntegratedModuleHost.Tick` 无宿主层异常隔离：归 host-composition 审查，先查模块内部异常/生命周期策略与可复现影响；若确认须改行为，记录批准的隔离/失败策略，并验证一个模块失败不会误吞其状态错误或无限逐帧刷日志。本文不直接授权加 catch。
- SceneActions/仪式处决的已接受回复门控：保留 J16 修复，后续渠道迁移必须回归 stale/discard 不产生副作用；未跑的三渠道回放不能沿用“已修”代替。
- 移除的西海旧键、net472 AuxiliaryTests、历史非 PASS 和发布素材遗留分别保留。与当前切片相关的必要失败先诊断；无关遗留不强行扩成 B 的全部返工。

## 6. 测试复用与执行安全

以下路径在编制时已核实存在；执行前阅读实际 runner、依赖与副作用，不表示本轮已运行：

- Memory：`tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/run.py`、`MemorySummaryMainThreadBoundaryTests/` 各相关 runner、`MemorySummaryBudgetTests/run.py`。
- Persistence：`tests/AF.Persistence/OwnerJsonStorageCodec/run.py`、`tests/AF.Persistence/PlayerExports/run.py`；Profile/Chunk/Identity 与 Weekly 的具体入口从当前 `tests/runners.json` 和生产引用定位，不照抄旧 tools 路径。
- Conversation：`tests/modules/AF.Module.Conversation/NativeCompletionBoundaryTests/`、`ScenePostprocessParityTests/`、`CourierPostprocessOwnerRegressionTests/`、`CourierDomainCommitTests/`、`CourierCommitOutcomeTests/`。
- Host：`tests/AF.GameAdapter.Bannerlord/HostCompositionTests/run.py`。
- 总回归：`tests/run_all.py` 配合 `tests/runners.json`；先核实外部 TEMP 授权与每个 runner 的实际写入，再运行。

测试必须覆盖迁移后的真实生产 owner，不只验证新建包装器；source-derived harness、fixture、PE/实际 DLL 的边界分别说明。变异用例只有在具名行为断言处失败才算有效；编译失败不是拒绝坏行为的证据。新增测试按现有 owner 目录放置并确认总 runner 实际发现，不重建测试平台。

## 7. J17-C：同候选离线结项

在 B 全部具名切片完成后冻结候选，执行一次最终门禁；有新源码修改则重跑受影响证据，不复用已失效的产物。

1. 主台账范围内 `REVIEW_REQUIRED / OPEN / VERIFY = 0`，所有 `RETAINED_BOUNDARY` 逐符号有理由，排除项不算迁移成果；R07 双向成员/消费者覆盖闭合。
2. 复核最终两 API Compile 与动态入口、旧反向业务回调、partial 共享状态；无重复权威实现/状态，无未接线新 owner。
3. 按现有构建入口和核实参数验证 Debug/Release × 1.3/1.4 与 Bootstrap，记录实际参考版本、命令、退出码和候选 hash。不修改脚本凑通过；入口若捆绑 Stage/覆盖/打包，先明确动作及授权，不擅自执行。
4. 运行受影响契约、保存/ABI、三渠道/领域回放和全量 runner；逐项对照 G0 与 J16 基线。新增回归为 0，且当前变更相关必要门禁须通过；不能仅以总 runner 的 FAIL=0 豁免相关 PREEXISTING_FAIL。
5. source inventory、代码地图 recorded/working-tree、文档链接及 `git diff --check` 通过；这些只证明其各自范围，不替代业务测试。
6. 记录遗留、未执行项和回滚提交；最多标“主体职责重构及离线验收完成”。不得提升 LIVE/SAVE/RELEASE。

## 8. J17-D：实机、旧档与交付分别签收

只有在精确游戏路径、版本、备份/恢复及写入范围获批后执行：

- 两条受支持 API 线上记录确切游戏版本与候选 DLL；检查 Bootstrap 单实现加载、Harmony/模块组合启动及退出。
- 新战役与代表性旧档分别完成 load → 关键机制 → save → reload；包含记忆/摘要/导入导出/周报及旧键风险，不用 JSON fixture 代替 SaveSystem。
- 三渠道正常/失败/取消/重入、群聊、旁听、来信及唯一动作/AFEF 提交；记录表现/UI/音频和实测帧性能。
- Stage、部署、ZIP、推送、发布分别授权；发布另审素材许可、依赖、隐私/密钥、文件白名单及历史风险。不能因用户曾关闭 J15 就撤销发布 HOLD。

无环境/授权时保持 D `NOT-RUN`，清楚交付 C 的离线结果，不为了等待实机而重开已闭责任单元。

## 9. 接续、停点与交接格式

- 最新源码变化：先刷新受影响分区；不机械丢弃未变 owner 的有效证据。
- 类型/保存身份不确定：只暂停该类型移动，保留身份并寻找不改身份的算法边界；不试写真实旧档。
- 新增业务规则、排除范围或外部写入：记录精确决策问题，再请求批准；普通已授权包内的机械步骤不逐次请示。
- 每次交接只写：候选修订、已闭子项、下一子项、测试结果/证据层级、未验/授权风险；详细数据归主台账和范围图。

### 编制时可复制请求（历史模板，以上方当前执行顺序为准）

> 请按 `docs/plans/j17-responsibility-closeout-plan.md` 接续 J17，先核实当前 Git 与 HANDOFF。先执行 G0 和 J17-A 的只读审查及文档更新：刷新 99ca85ae 旧证据到当前源码，补全 20 桶成员/动态消费者责任分区，在主台账登记具名余项与有限退出门。保护已有改动，不清理生成物，不改产品/测试/构建脚本，不写仓外、游戏或玩家数据，不部署/推送。A 完成后汇报真实余项、依赖和 B1 的精确实施范围，待授权再进入产品迁移；不以旧 940/3,332 数字或名称分类冒充当前全量审查完成。
