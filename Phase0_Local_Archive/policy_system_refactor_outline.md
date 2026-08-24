# 政策系统彻底重构分阶段大纲

> 活文档版本：V9（Phase 0 收尾推进版）
>
> 文档状态：定义产品规则、架构边界、迁移顺序、性能约束和验收门槛，并在第 7 节持续记录 Phase 0 的实际审计证据；更新本文件不代表已经修改政策系统代码。
>
> 工作方式：持续只读审计 → 更新本大纲 → 冻结当前阶段契约 → 小步重构 → 验证阶段门槛 → 再次只读复查。任何阶段未通过退出门槛时，不进入下一阶段，也不靠临时 fallback 掩盖问题。
>
> 学习方式：工程进度正常推进；每完成一个可验证小批次，再结合本批真实代码复盘证据、选择、原因和替代方案。讲解不单独占阶段，也不默认要求用户停下来做练习。

## 1. 文档目的与执行原则

1.1 本文件是政策系统重构的唯一主大纲。后续发现新事实、产品规则变化或实现约束变化时，继续修改本文件并提升版本，不再创建互相竞争的第二份总纲。

1.2 本次重构只有两条产品主线：

1. 政策库、历史检索与政策关系；
2. 本地语义效果模块选择与模块化执行。

1.3 Repository、领域模型、存档适配、生命周期拆分、运行时快照、文件拆分和主 Behavior 变薄都属于支撑重构，只在两条产品主线或已确认期限规则确实需要时实施，不单独扩张成脱离产品目标的大改写。

1.4 每个阶段必须同时列明：目标、前置条件、实施项、性能约束、存档/迁移约束、退出门槛和明确不做的事项。只有退出门槛全部满足，阶段才算完成。

1.5 每个实现批次开始前都要重新只读确认将要触及的方法、调用者、保存键、跨 Behavior 桥和 1.3/1.4 API 差异。V7 的审计基线不是允许以后盲改的永久许可证。

1.6 不按“文件越多越干净”衡量完成度。只有当职责已经有真实调用者、可测试边界和性能预算时才拆文件；不提前创建整套空壳类。

1.7 不把阶段开关当作长期双实现。开关只能用于阶段内 shadow、验收和版本级回滚；某来源完成生产切换后，不允许每次请求失败就偷偷回落到旧 AI 数值效果。

1.8 本文中的“永久政策”统一表示“政策事实持续存在，直到明确主动废除”，不表示永远不可废除；“永久效果”和“有限效果”只描述机械效果生命周期。

## 2. 已冻结的产品规则

2.1 玩家王国、地方和附庸政策统一遵循以下期限语义：

1. 玩家不填写天数：政策永久，效果永久，效果较弱；
2. 玩家填写正天数：政策仍永久，天数只表示较强效果的持续时间；
3. 有限效果到期后只结束效果，不废除政策，不自动续期，也不创建到期表决；
4. 政策只有在明确主动废除时才进入 `Abolished`。

2.2 NPC 统治者政策遵循另一套已确认期限意图：

1. 政策永久，直到明确主动废除；
2. 机械效果始终有限；
3. 效果到期后政策继续存在；
4. 不因天数自动续期或自动废除。

2.3 玩家王国、地方、附庸在政策核心中的差异只应是来源、作用域和目标。现有审批方式、资源成本、附庸独立度结算、脱离阈值、失败回滚等差异保留在来源/目标/提交适配边缘，不继续形成三套政策库或三套效果引擎。

2.4 NPC 与玩家来源共享同一政策库契约、关系模型、生命周期模型、模块目录、语义选择算法和效果运行时；玩家与 NPC 仍按 2.1、2.2 各自已确认的期限意图运行。

2.5 每次正式发布政策前，必须使用现有 ONNX embedding 与 cross-encoder 能力检索本存档内的相关政策，并把受条数和字符预算限制的相关上下文交给 AI 阅读。

2.6 ONNX 是确定存在的运行条件。直接复用现有 `OnnxEmbeddingEngine` 和 `OnnxCrossEncoderReranker`；不设计“没有 ONNX”的生产分支，也不允许 sparse 检索代替生产 dense recall。若保留 sparse，只能用于诊断、shadow 对照或离线评测。

2.7 已废除政策保留为历史事实，在相关时允许 AI 读取，但必须：

1. 明确标记为 `Abolished`；
2. 提供废除日期和可用的废除原因；
3. 明确说明其效果当前不运行；
4. 使用独立历史配额并降低排序权重；
5. 与现行政策分组注入 prompt；
6. 不参与当前效果快照，也不因被召回而恢复效果。

2.8 被拒绝、撤回、生成失败或尚未正式提交的草案默认只进入审计记录，不进入可检索政策语料。它们以后若要用于专门分析，必须走独立、显式的数据集，不得污染正式历史检索。

2.9 政策之间需要持久化表达支持、补充、冲突、限制和取代等关系。关系必须引用稳定政策 ID，并由本地代码校验端点、类型和状态；不能每次依靠全库全文扫描临时猜测。

2.10 一项政策允许命中多个互补效果模块，也允许零模块达到阈值：

1. 多命中必须经过固定数量上限、冲突组、依赖和全局数值边界裁决；
2. 零命中时政策仍正常发布并进入政策库，只是不创建机械效果；
3. 不得为避免空结果而强塞低相关模块。

2.11 第二条产品主线完成生产切换后，AI 不再输出可直接执行的数值效果 JSON，也不输出模块 ID。AI 只负责政策评价、民众公开反馈、影响叙述和受限的关系建议；本地语义检索根据政策名称、正文、民众反馈、作用域和合法目标摘要选择模块，再由模块代码生成确定性效果计划。

2.12 民众公开反馈可以参与模块语义判断，但不能创造新的合法目标。目标合法性始终来自政策正文、来源上下文和本地 `PolicyTargetResolver`；NPC foreign target 仍必须来自允许候选并被政策正文明确提及。

2.13 已经修复的动态 `PolicyObject` 读档恢复链路视为正确且必须保留的生产不变量，不再质疑“能不能用”，也不允许重构使玩家政策在原版政策界面读档后消失。

2.14 已交付玩家的存档属于生产数据。重构不得要求重开档，不得静默丢失政策、效果、议程、稳定 ID、原版活动政策成员关系、NPC 记录或历史事实。

2.15 一套源码必须继续支持 `BannerlordApi=1.3`、`BannerlordApi=1.4` 和现有 Bootstrap 单模块双实现输出。不得擅自修改一键编译、覆盖、推送、模块 ID、DLL 加载或打包流程。

## 3. 已确认的当前实现基线与技术债

3.1 玩家政策 UI 当前把空天数描述为“由 AI 决定”，`SubmitPolicyFromPopup(...)` 只在玩家填写正整数时设置 `ManualDurationDays`；这与新期限规则冲突。

3.2 `BuildMainMessages(...)`、效果解析和稀疏编译当前要求 AI 返回正 `durationDays`，`ApplyPolicyEffects(...)` 与 `ApplyLocalPolicyEffects(...)` 会跳过非正持续时间。永久效果不能只改 UI 文案就成立。

3.3 `CompleteApprovedPlayerPolicy(...)` 当前用单个 `hasTimedEffect` 同时决定扣费、效果激活、成功历史、管家经验和自然到期议程。必须先等价拆开这些职责，再切换期限语义。

3.4 当前最后一个王国效果结束会进入 `TryQueueNaturalExpiryAbolition(...)`，把动态政策改为 `expiry_vote_pending` 并建立反向 `KingdomPolicyDecision`；结果会续期或废除政策。

3.5 玩家地方/附庸来源现有独立保存 DTO、倒计时、续期、主动停止、目标丢失、关系终止和直接提交写入。发布、续约、停止、`targets_lost`、`relationship_ended` 以及对应 `_localPolicyRecords` / `_activePolicyEffects` 写入都是迁移时必须逐项收口的旁路。

3.6 NPC 当前要求 AI 返回有限正天数和扁平数值效果；采纳后注册有限 active effect，并立即准备到期议程。最后一个效果到期会驱动 NPC 记录与动态政策进入续期/废除链路，读档恢复也内建同一假设。

3.7 玩家效果当前由 `PolicyMainAssessmentResult.effects` / `PolicyEffectDto` 的 AI JSON 提供目标、指标、数值和天数；NPC 使用 `NpcRulerPolicyEffectDto`。二者最终桥接到现有 active effect 运行时和模型 patch。

3.8 玩家民众反馈与数值效果目前来自同次 LLM 结果，成功提交后再延迟展示。未来模块选择必须发生在反馈已经生成之后；反馈可以作为语义输入，但不获得目标授权能力。

3.9 当前可执行指标至少包括：繁荣、食物、炉火、忠诚、安全、民兵、税收百分比、建造力和王国稳定度一次性变化。附庸独立度、成本和其他来源特有一次性副作用必须单独盘点，不能在模块化时遗漏或重复结算。

3.10 `CustomPolicyBehavior.SyncData(...)` 当前至少维护以下四个生产保存键，迁移期间必须兼容读取：

1. `_afCustomPolicyRecordHistory_v1`；
2. `_afLocalPolicyRecords_v1`；
3. `_afCustomPolicyActiveEffects_v1`；
4. `_afDynamicPolicyRegistry_v1`。

3.11 NPC 侧还维护 `_afNpcRulerPolicyRecords_v1` 以及生成/检查时钟保存键。Repository 迁移不能只收口玩家四组字典而遗漏 NPC 桥。

3.12 当前 active effect 读档路径只恢复 `RemainingDays > 0` 的项。新迁移器必须先读取原始字典再分类，否则零天数、已结束和损坏记录会在迁移判断前丢失。

3.13 `INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()` 会在原版注销 non-ready 对象前调用动态政策初始化/修复逻辑，并核对对象管理器、`Kingdom.ActivePolicies`、未决议程和动态注册数据。1.3 与 1.4.5 的相关加载顺序一致，这条链路必须零退化。

3.14 当前 `EnsureDynamicPoliciesRegistered(...)` 会从旧状态、原版成员关系和未决议程反推续期或废除。新模型中的“政策 Active 但效果已结束/不存在”会被旧推导误判，必须通过显式 schema 与受控迁移隔离。

3.15 现有知识库已经提供版本化索引、精确向量召回和批量 rerank 的实现参考，但知识规则、人物提及、实体路由和 `CollectCandidateRules(...)` 属于知识领域逻辑，不能直接当政策库或模块目录复用。

3.16 现有 embedding/reranker 内部缓存容量有限且达到阈值时可能整表清空。政策历史索引和模块索引必须拥有自己的不可变文档向量快照、版本和结果缓存，不能依赖引擎的小缓存承担语料缓存职责。

3.17 当前 `AddActivePolicySettlementEffects(...)` 等模型热路径会复制并扫描全部 active effect，可能反序列化 JSON；税收和建造力只有部分缓存，其他指标仍可能全量扫描。这是必须在生产模块切换前解决的 O(N) 和分配热点。

3.18 当前每日维护还会遍历 active effect 和目标定居点，而连续数值实际由 Harmony 模型查询应用。需要改成增量快照与到期调度，不能保留无意义的重复全量工作。

## 4. 目标架构与职责边界

4.1 `CustomPolicyBehavior.cs` 继续作为团队要求的主文件和 Campaign Behavior 宿主，但最终只保留最小连接职责：

1. Campaign 事件注册与公开桥入口；
2. `SyncData(...)` 对 Repository 兼容门面的调用；
3. 一次性捕获主线程世界快照、运行代数和取消令牌；
4. 编排“前处理 → 评价 → 后处理/计划 → 主线程原子提交”；
5. 现有 Harmony/Behavior 接入与跨 Behavior 调度。

4.2 三段业务流水线可行，但不是把现有巨型类机械切成三个新巨型文件：

1. `PolicyPreprocessor.cs`：输入归一化、资格预检、期限意图、世界快照、历史检索和 prompt 上下文；
2. `PolicyEvaluator.cs`：LLM 评价、民众反馈、关系建议、结构校验、有限重试；
3. `PolicyPostprocessor.cs`：合法目标解析、旧效果适配或模块选择、确定性计划生成和提交命令组装。

4.3 支撑组件按真实职责逐步抽离，暂定边界如下；文件名在每个批次开始前按现有目录约定复核：

1. `PolicyModels.cs`：不可变命令、快照、结果、生命周期和保存 DTO；
2. `PolicyRepository.cs`：旧键兼容、政策/效果/关系的唯一写入门面和只读查询快照；
3. `PolicySemanticIndex.cs`：政策历史独立 ONNX 索引、召回、rerank、版本和缓存；
4. `PolicyRelationStore.cs`：关系边校验、邻接快照和增量失效；
5. `PolicySourceAdapter.cs` / `PolicyTargetResolver.cs` / `PolicyCommitAdapter.cs`：来源差异、合法目标和原子落地边缘；
6. `DynamicPolicyObjectAdapter.cs`：原版 `PolicyObject`、活动成员、议程与读档前清理恢复；
7. `PolicyEffectModuleContracts.cs`：模块描述符、选择上下文和确定性计划合同；
8. `PolicyEffectModuleCatalog.cs`：稳定 ID、版本、冲突/依赖校验和不可变目录；
9. `PolicyEffectModuleSemanticIndex.cs`：模块文档的独立 ONNX 索引；
10. `PolicyEffectEngine.cs`：计划提交、一次性执行幂等、到期调度和模型指标快照。

4.4 `PolicySystemUi.cs` 与 `PolicySystemLog.cs` 只消费公开查询模型、命令和诊断事件，不直接修改 Repository 内部字典。

4.5 `NpcRulerPolicyBehavior.cs`、`KingdomAgendaCustomPolicyBehavior.cs`、`RulerPolicyProposalBehavior.cs` 最终作为来源/流程适配器接入统一核心；迁移期间不得形成第二套新真相。

4.6 后台阶段只读取不可变 `PolicyWorldSnapshot` 和纯数据 ID，不持有或访问可变 TaleWorlds 对象；TaleWorlds 对象的读取与最终提交只发生在主线程。

4.7 依赖方向固定为：宿主/来源适配器 → 流水线 → 领域服务/索引/模块合同 → Repository 接口。Repository 与模块不得反向依赖 UI、具体 prompt 或 Campaign Behavior。

4.8 本阶段大纲不创建任何上述文件。每次只在旧代码中确认了真实所有权、调用者、测试边界和迁移落点后抽一个职责。

## 5. 目标领域模型与生命周期真相

5.1 政策记录与效果实例是两个独立聚合：政策状态不能由 `DurationDays`、`RemainingDays` 或“是否有 timed effect”反推；效果结束也不能自动改变政策状态。

5.2 新政策状态至少包括：`Pending`、`Active`、`Rejected`、`Abolished`、`OwnerUnavailable`。`Expired` 只属于效果状态，不是政策状态。

5.3 命令被接受并准备创建 pending/agenda 记录时立即分配稳定 `RecordId` 与 `PolicyObjectId`；正式发布并进入检索语料发生在提交成功后，两者不能混为一谈。

5.4 新效果使用显式 `EffectLifetimeKind`：`Permanent` 或 `Timed`。禁止再用 0、负数或缺省天数隐式编码永久。

5.5 永久效果保存 `EffectLifetimeKind = Permanent`、空 `EffectDurationDays`、空 `ExpiresOnDay`，不进入每日到期调度。

5.6 有限效果保存 `EffectLifetimeKind = Timed`、正 `EffectDurationDays`、`ActivatedOnDay` 和绝对 `ExpiresOnDay`。`RemainingDays` 只作为旧档兼容字段或 UI 派生值，不再作为新真相。

5.7 效果状态至少包括：`Active`、`Expired`、`EndedByAbolition`、`SuppressedByPolicyRelation`、`TargetUnavailable`、`ModuleUnavailable`。

5.8 玩家三来源的 `EffectLifetimeIntent` 只有 `PermanentWeak` 与 `TimedStrong`；NPC 使用 `NpcTimed`。意图在前处理阶段确定，不允许 AI 覆盖。

5.9 新 schema 的议程保存显式 `DecisionIntent`，至少区分 `Adoption` 与 `ExplicitAbolition`。新系统不再创建 `NaturalExpiryAbolition`。

5.10 `PolicyLibraryEntry` 是由权威政策记录投影出的检索文档，不是第二份可写政策实体。它至少包含稳定 ID、来源、作用域、目标摘要、名称、正文/摘要、公开反馈、政策状态、效果状态摘要、发布时间、废除信息、版本和可见性。

5.11 正式发布的 `Active` 与 `Abolished` 政策进入检索语料；效果已过期但政策仍 `Active` 时仍属于现行政策组，并明确标注“当前无活动效果”。被拒绝、撤回和生成失败的草案默认不进入语料。

5.11.1 `LegacyDecisionReconciliationPending` 不是政策状态。证据对账完成前，该记录默认不投影到现行政策组或已废除历史组，也不参与 world event、weekly material、外交快照或 agenda 上下文；它只出现在诊断/兼容审计视图。对账完成后才按确定的正式政策状态投影。

5.12 政策关系边至少保存：稳定边 ID、起点政策 ID、终点政策 ID、关系类型、方向、证据摘要、来源、置信/校验状态、创建版本和当前有效性。关系端点缺失时隔离边，不删除政策。

5.13 模块选择结果至少保存：目录版本、模型指纹、查询版本、候选与分数摘要、最终模块 ID/版本、强度档、合法目标 ID、冲突/依赖裁决、生成的 `PolicyEffectPlan` 和幂等执行键。

5.14 零模块命中时仍创建并提交政策记录、原版 `PolicyObject` 和政策库文档；效果计划为空，政策状态为 `Active`，不能被读档逻辑当作失败或到期。

5.15 主动废除是唯一正常的政策终止入口：它更新政策为 `Abolished`，结束尚未结束的效果，从原版活动政策成员中移除对应对象，并保留完整历史文档。

5.16 发布者/目标消失不能伪装为主动废除。政策事实保留，分别用 `OwnerUnavailable`、`TargetUnavailable` 或明确运行状态表达；具体恢复规则在启用前冻结。

5.17 所有落地使用一个不可变提交计划，在主线程内校验存档代数和当前世界前提后原子完成：政策记录、原版成员、效果、关系、成本、一次性副作用、历史和通知要么按计划一致提交，要么不提交，不留下半状态。

## 6. 总阶段表与依赖顺序

| 阶段 | 主目标 | 产品行为变化 | 必须产出 |
| --- | --- | --- | --- |
| Phase 0 | 冻结只读基线 | 无 | 调用图、存档矩阵、性能基线、测试样本 |
| Phase 1 | 存档与架构安全底座 | 无，保持旧行为等价 | `PolicyObject` 适配、Repository 门面、迁移 dry-run、新 schema shadow |
| Phase 2 | 政策库首个可用版本 | 发布前开始读取相关历史 | 统一语料、ONNX 检索、废除历史配额、关系事实层 |
| Phase 3 | 统一来源与期限语义 | 政策/效果生命周期正式分离 | 玩家三来源统一切换、NPC 到期链拆除、旧档受控迁移 |
| Phase 4 | 效果模块基础与等价证明 | 生产仍走旧数值效果 | 模块合同、目录、目标解析、内置模块、旧效果 round-trip |
| Phase 5 | 模块语义 shadow 与评测 | 无真实效果切换 | 模块 ONNX 索引、multi/no-match、冲突依赖、差异报告 |
| Phase 6 | 模块生产切换与关系执行 | AI 数值效果退出生产 | 玩家三来源原子切换、NPC 分批切换、确定性计划、O(1) 快照 |
| Phase 7 | 玩家可扩展模块 | 增加可维护扩展入口 | 模板、合同测试、启动时发现；外部 DLL 独立审计 |
| Phase 8 | 清理与最终收口 | 删除确认无调用的旧生产路径 | 薄主文件、兼容读取边界、最终性能与双版本验收 |

6.1 阶段顺序固定。尤其不得在 Phase 1 的存档保护、迁移隔离和等价拆线完成前切期限语义；不得在 Phase 4 的模块表达等价和 Phase 5 的 shadow gate 完成前让模块生产执行。

6.2 Phase 2 与 Phase 4/5 使用两套独立语料、snapshot、版本和缓存：政策历史索引不能混入模块描述符，模块索引不能混入玩家政策全文。

6.3 玩家王国、地方、附庸属于同一个产品切换单元。可以在隐藏 staging/shadow 路径中按适配器分别测试和修复，但 Phase 2 的发布前检索、Phase 3 的新期限写入和 Phase 6 的模块生产切换都必须在同一发布批次对三来源一致启用，不能在生产中半新半旧。Phase 3 原子切换的是期限语义，Phase 6A 原子切换的是效果引擎，两者不是同一次改动。

6.4 NPC 使用同一核心，但可在每个产品阶段内设独立子门槛，因为其议程、self/foreign 目标和周报上下文不同；这不是另建一套引擎。

6.5 所有阶段的失败回滚优先回滚整个未完成批次。不得通过删除生产存档、重建玩家档案或静默降级到 sparse/旧 AI 效果来“恢复”。

## 7. Phase 0：只读基线冻结

### 7.1 目标

建立可以证明“重构前是什么”的最小完整基线，为后续每阶段提供对照，而不是继续无限阅读却没有可验收产物。

### 7.2 实施项

7.2.1 固定玩家王国、地方、附庸和 NPC 的入口、prompt、解析、审批/提交、效果激活、到期、主动废除、UI、日志与跨 Behavior 调用图。

7.2.2 固定所有生产保存键、DTO 字段、状态值和 `PolicyObject` 恢复顺序；保存一组脱敏的旧档测试样本，覆盖正常、边界和损坏记录。

7.2.3 记录当前生产行为：扣费、经验、公告、公开反馈、一次性副作用、目标展开、续期、废除和读档恢复结果。

7.2.4 建立性能基线，至少记录：政策/效果数量、模型 getter 调用频率、每日 tick 工作量、JSON 反序列化次数、分配量、索引构建时间、单次 embedding、dense recall 和 batch rerank 的 P50/P95/Max。

7.2.5 建立语义评测初始集，但与正式索引语料分开：相关政策正例/困难负例，以及效果模块正例、无匹配、多匹配、冲突和依赖样例。

### 7.3 性能约束

Phase 0 只测量，不在未证明瓶颈前引入 ANN、复杂线程池或新缓存框架；所有测量记录文档数、Top-K、模型指纹和硬件环境，避免孤立毫秒数字失去意义。

### 7.4 存档与安全约束

只读加载测试使用副本；不得为了生成样本改写玩家原档。任何损坏样本都人工构造或脱敏复制。

### 7.5 退出门槛

1. 四类来源调用图和写入点完整；
2. 旧档矩阵与可重复加载步骤明确；
3. 当前 `PolicyObject` 修复成功判据明确；
4. 性能基线能够重复采集；
5. 后续阶段涉及的方法清单已按 1.3/1.4 复核。

### 7.6 本阶段不做

不改业务行为、不拆文件、不改保存格式、不改 prompt、不优化尚未测量的路径。

### 7.7 Phase 0 执行账本（第一轮）

7.7.1 本轮代码观察基线为 `main@3bee8b7fa108`，审计日期为 2026-08-06。工作区存在用户既有未提交内容，因此本节只冻结方法、调用方向、保存键和已观察行为；每个后续实现批次仍须按 1.5 重新核对，不能把本轮行号当作永久接口。

7.7.2 当前完成度：

| 子项 | 状态 | 当前证据 | 进入下一步前还缺什么 |
| --- | --- | --- | --- |
| Phase 0A 四来源调用图 | 第一轮已冻结 | 玩家王国、地方、附庸和 NPC 的入口、评价、提交、效果、到期与废除链已定位 | 把每个实际字典写入口转成可机械检查的清单，并在首次代码改动前重跑搜索 |
| Phase 0B 旧档/对象恢复矩阵 | 步骤与判据已冻结，实机执行待完成 | 7.7.14 已固定 1.3/1.4 候选指纹、只读副本流程、对象图断言和 `pre-cleanup-policy-restore-complete` 日志证据 | 实际对 1.3 与 1.4 候选档各跑一遍“复制原档 → 加载副本 → 另存新副本 → 退出 → 再加载 → 断言”，并回填证据 |
| Phase 0C 性能基线 | 可重复受控基线已完成 | `Phase0_Local_Archive/baseline/run_baseline.ps1` 已对 1.3/1.4 Stage DLL 采集 getter 扫描、每日 effect lookup、JSON、索引、embedding、dense recall、batch rerank 的冷热 P50/P95/Max 与分配 | 后续阶段用同规模重跑；真实战局每秒 getter 次数与完整 target expansion 另记为运行态补充，不改变本基线 |
| Phase 0D 语义评测集 | 第一轮已完成 | `Phase0_Local_Archive/baseline/cases/` 下两套独立 JSONL 共 21 例，运行器校验 ID、引用、scope、outcome 与稳定性组 | 后续只能增量扩充；在 Phase 2/5 的实现出现后再计算正式质量指标，不得并入生产 prompt |
| 1.3/1.4 方法复核 | 第一轮已完成关键加载链 | 两个版本都在 behavior data 加载后、non-ready 删除前调用同一 handler | Phase 1A 实现前再逐个核对所触及 TaleWorlds 成员并执行双实现构建 |

7.7.3 玩家王国当前主链：

1. `CustomPolicyBehavior.SubmitPolicyFromPopup(...)`（`CustomPolicyBehavior.cs:3307`）归一化输入、解析天数、冻结目标与 prompt 上下文，再把 LLM 工作放到后台；
2. `GeneratePolicyResultAsync(...)`（`:3416`）仍要求并解析 AI 数值效果 JSON；`CompletePolicyGeneration(...)`（`:3532`）在主线程重新校验并创建 `DynamicPolicySaveData(status=pending)`；
3. `TrySubmitDynamicPolicyAgenda(...)`（`:1849`）创建/初始化动态 `PolicyObject`、保存 pending 记录并提交正向 `KingdomPolicyDecision`；
4. 议程通过后 `CompleteDynamicPolicyAdoption(...)`（`:2380`）先把 dynamic 状态写为 `active`，再调用 `CompleteApprovedPlayerPolicy(...)`（`:2447`）扣费、写历史、激活效果、记玩家行为、发公告/反馈和经验；
5. 最后一个效果结束时 `TryQueueNaturalExpiryAbolition(...)`（`:2665`）全扫 active effect，随后把政策改为 `expiry_vote_pending` 并创建反向废除议程；
6. 反向议程结果进入 `CompleteNaturalExpiryRenewal(...)`（`:2395`）或 `CompleteDynamicPolicyAbolition(...)`。这只是必须保留的旧行为基线，不是目标语义。

7.7.4 `hasTimedEffect` 的第一轮耦合基线：

1. `HasAnyTimedPolicyEffect(...)`（`CustomPolicyBehavior.cs:9685`）只回答“`KingdomEffects` 中是否存在 `DurationDays > 0` 的元素”；
2. 同一事实目前直接或间接决定扣费、成功历史、统一玩家政策记录、效果激活、玩家行动、管家经验、公开反馈和自然到期议程；
3. `RecordSuccessfulPolicy(...)`（`:7347`）内部再次用相同条件拒绝写历史，因此 `recordWritten` 也不是纯粹的持久化结果；
4. 地方发布（`:1321`）和附庸发布（`:1965`）同样用该谓词阻断零 timed effect；
5. Phase 1E 的第一刀必须是行为等价拆线：分别命名“命令成功、扣费资格、历史写入结果、效果计划存在、效果激活、经验资格和旧到期议程资格”，但在 Phase 3/6 正式切换前仍输出旧结果；不能只把一个布尔值复制成多个同义变量。

7.7.5 玩家地方与附庸当前旁路：

1. 两者共用 `SubmitPolicyFromPopup(...)`、`GeneratePolicyResultAsync(...)` 和 AI 数值效果解析，但在 `CompletePolicyGeneration(...)` 后分叉；
2. 地方走 `CompleteLocalPolicyGeneration(...)`（`CustomPolicyBehavior.cs:1321`），直接扣费、调用 `ActivateLocalPolicyEffect(...)`、写 `_localPolicyRecords`，不进入王国议程；
3. 附庸走 `CompleteVassalPolicyGeneration(...)`（`:1965`），直接激活效果、写 `_localPolicyRecords`、调用 `VassalageBehavior` 结算独立度并额外注册统一玩家政策记录；
4. 本地/附庸续约、主动停止、`targets_lost`、`relationship_ended` 仍有各自入口；每日维护会重新解析所有相关记录并重新展开目标；
5. 所以“地方和附庸只是对象不同”是目标核心的正确约束，但当前代码还不是同一提交路径。Phase 3 收口时必须保留附庸独立度、宗主关系失效回滚和地方目标丢失等边缘语义，不能只把两个方法合并后漏掉副作用。

7.7.6 NPC 当前主链：

1. `NpcRulerPolicyBehavior.TryStartPolicyGeneration(...)`（`NpcRulerPolicyBehavior.cs:2444`）以及两个建议入口先在主线程分帧构造纯数据 snapshot，再由 `ProcessPolicyGenerationJobAsync(...)`（`:2548`）后台调用 LLM；
2. `ProcessPendingPolicyCommitStage(...)`（`:2844`）先写 `_policyRecords(status=pending)`，再通过 `CustomPolicyBehavior.TrySubmitNpcPolicyAgendaForExternal(...)` 映射为动态政策议程；
3. 议程批准后 NPC 记录先进入 `approved_pending_commit`，再分阶段写 world event、公开反馈、active effect 和 weekly material，最终改为 `active`；
4. active effect 由 `CustomPolicyBehavior.TryRegisterPolicyActiveEffectForExternal(...)` 落入玩家共用的 `_activePolicyEffects`，进度再桥接回 NPC DTO；
5. 当前最后一个 NPC 效果结束仍会走 `TryQueueNaturalExpiryAbolition(...)` → `expiry_vote_pending` → 续期或废除。这是 Phase 3 必须停止的新生产链，但 Phase 1 只能等价包裹，不能提前删除 legacy 对账能力；
6. NPC 仍有 world event、延迟公众反馈、weekly material 和 self/foreign 合法目标语义，统一核心时这些都属于必须保留的适配边缘，不是另一套政策库或效果引擎。

7.7.7 当前生产保存真相：

| 所有者 | 保存键 | 当前用途/注意点 |
| --- | --- | --- |
| `CustomPolicyBehavior` | `_afCustomPolicyRecordHistory_v1` | 玩家王国成功历史；当前受 timed effect 门控 |
| `CustomPolicyBehavior` | `_afLocalPolicyRecords_v1` | 地方与附庸记录、续约、目标和结束状态 |
| `CustomPolicyBehavior` | `_afCustomPolicyActiveEffects_v1` | 三类玩家与 NPC 共用的运行中效果 JSON |
| `CustomPolicyBehavior` | `_afDynamicPolicyRegistry_v1` | 原版动态 `PolicyObject`、议程和状态恢复依据 |
| `NpcRulerPolicyBehavior` | `_afNpcRulerPolicyRecords_v1` | NPC 政策、效果镜像、agenda/反馈/周报状态 |
| `NpcRulerPolicyBehavior` | `_afNpcRulerPolicyLastGeneratedDay_v1`、`_afNpcRulerPolicyLastGeneratedHour_v1`、`_afNpcRulerPolicyLastCheckDay_v1` | NPC 生成与检查时钟 |

`CustomPolicyBehavior.SyncData(...)` 当前加载 active effect 时只保留 `RemainingDays > 0` 的反序列化结果；Phase 1C 迁移 dry-run 必须在该过滤之前读取原始恢复字典，否则零天数、已结束或损坏项没有机会被分类和隔离。

7.7.8 已确认的动态 `PolicyObject` 读档修复机制：

1. 修复来自提交 `60f0e649`：`CustomPolicyBehavior` 开始实现 `INonReadyObjectHandler`，并新增 `InitializeLoadedDynamicPoliciesBeforeNonReadyCleanup()`；
2. 两个原版版本的真实时序一致：behavior data 加载 → behavior 注册事件 → 调用所有 `INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()` → `UnregisterNonReadyObjects()`。对应 1.3 源码 `Campaign.cs:1949-1960`，1.4.5 源码 `Campaign.cs:1427-1438`；
3. handler 从 `_dynamicPolicyRegistry` 读取必须保留的 pending/active/expiry-vote 记录，收集对象管理器、`Kingdom.ActivePolicies` 和未决 `KingdomPolicyDecision` 中所有同 ID 引用；
4. `EnsureDynamicPolicyObject(...)` 在缺少 canonical 对象时用 `RegisterPresumedObject(new PolicyObject(id))` 补注册，随后 `TryInitializeDynamicPolicyObject(...)` 用保存的名称、摘要、说明和政治权重初始化每一个已被存档对象图引用的实例；
5. 关键不是“读档后再 AddPolicy”，而是**在原版清除 non-ready 对象之前，把所有仍被对象图引用的动态政策变成 ready，并统一 canonical 引用**；之后 `EnsureDynamicPoliciesRegistered(false/true)` 才负责重新绑定/去重议程、恢复 active membership 和状态对账；
6. Phase 1A 抽离时必须保持 handler 接口、调用时序、`PolicyObjectId`、初始化字段、canonical/active/decision 多引用处理和后续两次 reconcile 完全等价。任何只保留 `OnGameLoaded` 或只恢复有 active effect 的简化都会重现玩家读档后政策从原版界面消失的问题。

7.7.9 ONNX 与现有知识库的第一轮复用边界：

1. 直接复用 `OnnxEmbeddingEngine.TryGetEmbedding(...)` 和 `OnnxCrossEncoderReranker.TryScoreBatch(...)`；前者输出 L2 归一化向量，后者能对未命中的 query-document pair 做一次 batch 推理；
2. 参考 `KnowledgeLibraryBehavior` 的 corpus version、索引锁、构建后引用替换、一次 query embedding、精确点积召回、固定候选 batch rerank 和查询结果缓存模式；
3. 不复用 `CollectCandidateRules(...)`、人物提及槽位、topic/evidence seed、知识规则文本拼接或 sparse fallback，因为它们是知识库领域策略；
4. embedding 引擎 query cache 上限为 256，reranker pair cache 上限为 512，两者满后整表清空；它们只可作为引擎小缓存，不能承担政策语料向量 snapshot 或长期结果缓存；
5. 现有 ONNX session 推理没有全局并发闸门。政策历史索引和模块索引必须分别限制后台并发并测量，不能假设多路 `Task.Run` 会自动提高吞吐。

7.7.10 第一轮静态性能热点：

1. 六个 settlement 模型 getter 最终进入 `AddActivePolicySettlementEffects(...)`（`CustomPolicyBehavior.cs:1416`）；每次调用复制 `_activePolicyEffects.Values.ToList()`、遍历全部效果、按 JSON 文本缓存反序列化结果，并为命中项创建说明文本。调用成本随 active effect 总数线性增长；
2. 税收与建造力已有按 kingdom/settlement 的部分缓存，繁荣、炉火、食物、忠诚、安全和民兵仍走上述全扫路径；
3. `EnsureActivePolicyEffectWorkScheduled(...)`（`:4645`）每天复制并反序列化全部 active effect；本地效果工作还会重新解析封地所有权、提及目标和 settlement 展开；
4. `TryQueueNaturalExpiryAbolition(...)` 为判断同 record 是否还有效果会再次全表 JSON 扫描；
5. NPC 多个上下文、反馈和恢复入口反复执行 `_policyRecords.Values.Select(DeserializeRecord)`；
6. 已有 `PerfProbe` 只给部分工作段采样，尚不能直接回答 getter 调用频率、每次扫描项数、JSON miss 次数和分配量。Phase 0C 先补可重复观测，再在 Phase 6 用按目标/指标预聚合 snapshot 和到期桶替换热点；不得现在凭静态观察直接声称优化完成。

7.7.11 当前验证设施结论：

1. 双实现与 Bootstrap 的权威构建入口仍是 `一键编译覆盖推送/build_single_module.ps1 -Stage`；不修改一键流程，也不使用直接 1.3 build 代替其版本来源校验；
2. 仓库存在若干临时 net472 policy 回归程序，证明可以通过反射加载实际 `AnimusForge.dll` 做轻量回归，但它们位于 `.tmp`/`obj`，不能当成稳定测试资产；
3. 目前没有可自动执行的游戏内 1.3/1.4 存档往返 harness。因此 Phase 1A 之前必须先固定脱敏旧档、人工/半自动步骤和日志断言；只有编译成功不能证明 pre-cleanup 修复零退化；
4. 第一份生产代码批次仍定为单独的 Phase 1A，不和 Repository、迁移器或 `hasTimedEffect` 拆线混做；但只有 7.7.2 中 Phase 0B/0C/0D 的最低基线产物满足退出门槛后才开始。

7.7.12 本机真实旧档候选只读盘点：

1. 标准游戏存档目录中存在一个 `ApplicationVersion=v1.3.15.110062`、`Module_AnimusForge=v1.2.2.2` 的候选档；其创建时间早于提交 `60f0e649` 的 pre-cleanup 修复，并且解压后的字符串语料至少命中 7 个不同 `af_policy:` ID，可用于验证“旧实现写出的动态政策能否由当前实现恢复”；
2. 同目录中存在多个 `ApplicationVersion=v1.4.7.117484`、`Module_AnimusForge=v1.2.8.0` 的候选档；抽查样本至少命中 2—7 个不同 `af_policy:` ID，可用于 1.4 往返对照；
3. 上述检查只读取 `.sav` 的 metadata 和 Deflate payload，没有加载游戏对象图、没有修改原档、没有把原档复制进仓库；`af_policy:` token 数只证明候选不是空政策档，不等价于完成状态/成员/议程断言；
4. 正式矩阵对每个版本都必须在游戏内验证：相同 `PolicyObjectId`、名称/说明、`Kingdom.ActivePolicies` 成员、pending/反向议程方向、动态 registry 条目数、active effect 和 NPC 镜像；保存动作只允许“另存为测试副本”，再退出并重新加载该副本；
5. 当前成功路径 `pre-cleanup-policy-restore-complete` 已有聚合日志调用，但此前被 `PolicySystemLog` 正常阶段白名单过滤。Phase 0B 只把该聚合阶段加入白名单，以证明 handler 实际初始化了多少引用；不启用逐政策正文日志，也不改变恢复算法。真正的 UI/对象图断言仍必须在游戏内完成。

7.7.13 第一批可观测性改动与构建证据：

1. `PolicySystemLog.cs` 仅新增一个正常阶段白名单项 `pre-cleanup-policy-restore-complete`；生产调用点仍只有 `CustomPolicyBehavior.InitializeLoadedDynamicPoliciesBeforeNonReadyCleanup()` 中的一处聚合写入，未新增逐政策日志、保存字段、恢复分支或业务条件；
2. 这条信号只回答“清理前 handler 是否执行、合计初始化了多少个引用”，不能单独证明动态对象、王国成员、议程方向和效果镜像均正确；因此它是旧档往返矩阵的观察点，不是测试替代品；
3. 直接 `dotnet build` 曾因项目默认游戏路径不存在，以及工作区未跟踪的 `analysis/` 反编译源码被 SDK 默认通配纳入而失败；已按真实失败原因停止重试，没有删除、移动或修改这些用户文件，也没有为绕过环境问题改写项目或一键脚本；
4. 随后使用仓库权威 `build_single_module.ps1 -Stage`，仅通过当前构建进程的 `DefaultItemExcludes` 排除未跟踪的 `analysis/` 与 `TrueAutoBlock/`。Bannerlord 1.3 实现、Bannerlord 1.4 实现和 Bootstrap 均构建成功，各为 0 警告、0 错误；统一模块 Stage 组装验证成功，且脚本明确报告未修改游戏目录；
5. 上述结果证明这一行改动保持双实现和 Bootstrap 的编译/输出契约，但尚未证明游戏内 non-ready 调用时序与旧档对象图。因此 Phase 0B 继续保持“进行中”，下一门槛仍是 1.3/1.4 候选档各自的“只读加载 → 另存测试副本 → 退出 → 再加载 → 断言”。

7.7.14 Phase 0B 旧档往返矩阵、步骤与证据（2026-08-07 冻结）：

| 矩阵项 | 1.3 候选 | 1.4 候选 | 通过条件 |
| --- | --- | --- | --- |
| 版本指纹 | `ApplicationVersion=v1.3.15.110062`、`Module_AnimusForge=v1.2.2.2`，至少 7 个不同 `af_policy:` token | `ApplicationVersion=v1.4.7.117484`、`Module_AnimusForge=v1.2.8.0`，优先选择至少 7 个不同 `af_policy:` token 的样本 | 版本、模块版本、文件大小、最后写入时间和 SHA-256 均记录；原文件前后 SHA-256 相同 |
| 第一轮加载 | 对应 1.3 游戏运行时只加载统一模块中的 `versions/1.3/AnimusForge.dll` | 对应 1.4 游戏运行时只加载统一模块中的 `versions/1.4/AnimusForge.dll` | Bootstrap 选择正确实现；只加载测试副本，不加载或保存原档 |
| 第二轮加载 | 加载第一轮另存的 `AF_PHASE0_RT_13_A_<UTC>.sav` | 加载第一轮另存的 `AF_PHASE0_RT_14_A_<UTC>.sav` | 两轮对象图断言完全一致，且没有新增重复对象、成员、议程或效果 |

固定执行步骤：

1. 完全退出游戏和启动器；对候选原档记录绝对路径、版本 metadata、字节数、最后写入时间和 SHA-256。原档只读，不改属性、不改名、不在游戏中直接加载。
2. 为每个版本先创建字节完全相同的测试源副本 `AF_PHASE0_SRC_13_<UTC>.sav` / `AF_PHASE0_SRC_14_<UTC>.sav`，复制后校验其 SHA-256 与原档相同；后续只操作这些测试副本。
3. 启动匹配的游戏版本，关闭自动保存，确认启动日志中的 Bootstrap 版本选择与矩阵一致。记录 `Modules/AnimusForge/Logs/PolicySystem.txt` 启动前字节偏移；不清空、覆盖或删除既有日志。
4. 加载 `AF_PHASE0_SRC_*`。在 campaign 完全进入后记录第一轮对象图表：每个 live registry 记录的 `RecordId`、`PolicyObjectId`、状态、名称/说明摘要、owner kingdom、活动成员计数、未决议程计数与方向、active effect 计数，以及存在时的 NPC mirror 状态。
5. 日志新增片段必须出现一次 `[Agenda] [pre-cleanup-policy-restore-complete] references={N}` 且 `N > 0`；不得出现 `pre-cleanup-policy-restore-failed`、`policy-object-register-failed`、`pending-agenda-restore-failed` 或同 ID 重复对象迹象。该聚合值只证明清理前初始化执行，不能替代对象图断言。
6. 逐状态断言：`active` 必须有且只有一个同 ID canonical `PolicyObject` 并在正确 `Kingdom.ActivePolicies` 中出现一次；`pending` 必须不在 active member 中且只有一个正向未决议程；`expiry_vote_pending` 必须只有一个反向未决议程；`abolished/rejected` 不得被恢复为 active，也不得合成新议程。
7. 断言名称、说明、政治权重初始化成功；registry、active effect 与 NPC mirror 的 `RecordId/PolicyObjectId` 交叉引用不丢失。没有 active effect 的 live 动态政策也必须恢复，禁止以 effect 是否存在作为成功判据。
8. 使用游戏“另存为”写入新的 `AF_PHASE0_RT_13_A_<UTC>.sav` / `AF_PHASE0_RT_14_A_<UTC>.sav`，不得覆盖测试源副本；完全退出到桌面后重新启动对应版本，仅加载该 A 副本。
9. 第二轮重复步骤 3—7，并与第一轮逐项比较。记录第二轮新日志片段；要求稳定 ID、状态、成员、议程方向、effect/NPC mirror 数量相同，且一次性状态没有重放。
10. 测试结束后重新计算原档 SHA-256，必须与步骤 1 相同。把两版的样本指纹、两轮断言表、日志字节区间和结论回填本节；在这一步实际完成前，Phase 0B 和 Phase 0 总退出均保持未通过。

7.7.15 Phase 0C 可重复性能基线（2026-08-07）：

1. 非生产工具现归档于 `Phase0_Local_Archive/baseline/run_baseline.ps1`。它从 `-StageModuleRoot` 反射加载真实 1.3/1.4 `AnimusForge.dll`，直接调用现有 `OnnxEmbeddingEngine.TryGetEmbedding(...)` 与 `OnnxCrossEncoderReranker.TryScoreBatch(...)`；`-RerankerModelDir` 必填，任一模型不可用就失败，不存在无 ONNX 降级。
2. 工具只在忽略的 `bin/Debug/policy_phase0_runtime/` 下创建可丢弃的运行时 shadow、硬链接和指向现有 reranker 资产的 junction；不修改生产代码、prompt、保存格式、项目文件或游戏目录。报告默认写入本地归档的 `Phase0_Local_Archive/reports/`。
3. 固定规模：11 次受控调用、32 条 corpus/32 个 active effect、Top-K 8、rerank batch 8、embedding 维度 512。硬件为 i5-12400F、12 logical processors、约 17.0 GB RAM、64-bit CLR 10.0.2。
4. 模型指纹完整保存在报告；embedding model 前缀 `69b353bb2aa2`、embedding data `e72da961b036`、tokenizer `3d09c84ebd10`、reranker model `15b9a8c3da82`、reranker tokenizer `9eb652ac4e40`。两套评测集的完整 SHA-256 也写入报告。

| 测量项 | 1.4 P50/P95/Max ms；alloc/op | 1.3 P50/P95/Max ms；alloc/op |
| --- | --- | --- |
| getter scan core，cold | `0.3185 / 0.4646 / 0.4646；154266 B` | `0.3224 / 0.4501 / 0.4501；154266 B` |
| getter scan core，warm | `0.0063 / 0.0071 / 0.0071；282 B` | `0.0064 / 0.0071 / 0.0071；282 B` |
| daily effect lookup core，cold | `0.2751 / 0.8127 / 0.8127；157673 B` | `0.2894 / 0.6765 / 0.6765；157673 B` |
| daily effect lookup core，warm | `0.0019 / 0.0103 / 0.0103；1280 B` | `0.0019 / 0.0106 / 0.0106；1280 B` |
| 单条 active effect JSON 反序列化 | `0.0126 / 0.0303 / 0.0303；4737 B` | `0.0097 / 0.0369 / 0.0369；4737 B` |
| 32 文档索引构建，cold | `183.7060 / 183.7060 / 183.7060；17429336 B` | `188.4039 / 188.4039 / 188.4039；17429336 B` |
| 32 文档索引构建，warm | `0.0062 / 0.0196 / 0.0196；1344 B` | `0.0063 / 0.0249 / 0.0249；1344 B` |
| 单次 embedding，cold | `8.3100 / 23.9297 / 23.9297；976592 B` | `7.6362 / 24.2477 / 24.2477；976592 B` |
| 单次 embedding，warm cache hit | `0.0003 / 0.0104 / 0.0104；40 B` | `0.0005 / 0.0199 / 0.0199；40 B` |
| 32 向量 exact dense Top-K | `0.0336 / 0.0339 / 0.0339；112 B` | `0.0384 / 0.0420 / 0.0420；112 B` |
| 8 文档 batch rerank，cold | `287.1863 / 304.3242 / 304.3242；153244 B` | `286.0034 / 306.6160 / 306.6160；153244 B` |
| 8 文档 batch rerank，warm cache hit | `0.0013 / 0.0188 / 0.0188；1824 B` | `0.0012 / 0.0235 / 0.0235；1824 B` |

5. 权威报告为 `Phase0_Local_Archive/reports/phase0_closeout_1.4_20260807.json` 与 `Phase0_Local_Archive/reports/phase0_closeout_1.3_20260807.json`；实现 DLL SHA-256 分别为 `2dba16aca9feea1159d9c1e5985b4e5e646f4793ccc5aa98737ac8b409f3ae3c` 与 `9c02b3a9562caba3a1ae2d14cd88640c0f1eede8896cf037002e8704c44cb265`。
6. 边界：getter 项测量当前“复制 snapshot + JSON cache 扫描”核心，不含 TaleWorlds settlement filter 与 explanation 创建；daily 项调用真实私有 `GetActivePolicyEffectForWork(...)`，不含 target expansion/settlement application。11 个样本采用 nearest-rank，因此 P95 等于 Max；后续比较必须保持相同规模、模型与冷热定义。
7. 受控基线的调用计数固定为每项 11 次，可用于阶段前后回归；真实战局每秒 getter 调用频率与完整每日 target expansion 仍属于运行态补充观测，不得把本表外推成整帧/整日耗时，也不得据此提前实施 Phase 6 优化。

7.7.16 Phase 0D 两套分离评测集（2026-08-07）：

1. 政策历史检索集位于 `Phase0_Local_Archive/baseline/cases/policy_history_retrieval.jsonl`，共 11 例，SHA-256 为 `6bc9196c05b0fb84e9d0b43a8c8deeef9c96b0691ec2634171c57bec3a5dc880`。覆盖 active/abolished、废除配额、同词异目标困难负例、政策与历史 effect 区分、玩家 kingdom/local/vassal scope、NPC foreign 允许/拒绝、no-match 与同义稳定性对。
2. 效果模块选择集位于 `Phase0_Local_Archive/baseline/cases/effect_module_selection.jsonl`，共 10 例，SHA-256 为 `48b8f4b4457ace1b64940bca661aa7258df6df8d6ebd24adc05aa13c151342c2`。覆盖 single/no/multi match、冲突裁决、依赖拒绝/满足、相邻模块辨析、非法 target 与同义稳定性对。
3. 两套 JSONL 各自携带候选与 gold ID，不引用生产 prompt，也不互相混入语料。基线运行器会拒绝空/重复 `case_id`、空/重复候选 ID、悬空 gold 引用、缺失 scope/outcome、非法 Top-K/废除配额和孤立稳定性组。
4. 当前只冻结评测输入与期望；Phase 2 历史检索器和 Phase 5 模块选择器尚未实现，因此本批次不伪造 Precision/Recall、排序或模块选择通过率。将来只能用对应引擎分别评分这两套集合。

7.7.17 本批次改动、验证、未验证项与风险：

1. 生产代码只保留 `PolicySystemLog.cs` 的一行白名单改动；只读基线脚本和两套 JSONL 现归档于 `Phase0_Local_Archive/baseline/`。未修改政策业务行为、主文件结构、保存键/DTO、prompt、TaleWorlds API、`TroopInspectionBehavior.cs`、`analysis/` 或 `TrueAutoBlock/`。
2. `run_baseline.ps1` 已实际对 Stage 中 1.4 与 1.3 实现各运行一次，两个报告均生成；实际使用现有 embedding/reranker，模型缺失时 fail-closed。两套评测集通过运行器结构校验。
3. 权威 `一键编译覆盖推送/build_single_module.ps1 -Stage` 已再次执行：Bannerlord 1.3、Bannerlord 1.4 和 Bootstrap 均 `0 warning / 0 error`，统一模块 Stage 成功；输出仅在项目 `bin/Debug/single_module_stage/AnimusForge`，脚本明确报告未修改游戏目录。`DefaultItemExcludes` 只在该构建进程中排除 `analysis/**` 与 `TrueAutoBlock/**`，未修改 csproj。
4. 尚未验证：1.3/1.4 游戏内旧档两轮往返、对象图逐项断言、原档前后哈希、实际 `PolicySystem.txt` 新增日志片段，以及真实战局 getter 调用频率/完整每日 target expansion。玩家原档未被读取后写入，也未生成任何存档。
5. 已知风险：`pre-cleanup-policy-restore-complete` 只在 `initializedReferences > 0` 时写且只给聚合数；缺失该行可能是样本无 live 记录，也可能是 handler 未成功，必须结合候选分类和对象图判断。性能 cold/warm 数字受现有 256/512 引擎缓存和 11 样本粒度影响，只能同口径比较。
6. 工作区仍有用户侧删除的 `原版游戏本体代码1.4.5.rar`、补丁/ZIP、`analysis/`、`TrueAutoBlock/` 等状态，本批次均未处理。`git status` 仍显示四个由只读索引/换行统计造成的 `M`，但 `git diff` 对 `CustomPolicyBehavior.cs`、`NpcRulerPolicyBehavior.cs`、`OnnxCrossEncoderReranker.cs`、`PerfProbe.cs` 均为空；不得据此覆盖这些文件或刷新只读 `.git` 索引。

7.7.18 Phase 0 退出判定与 Phase 1A 精确交接：

1. Phase 0A 已冻结；Phase 0C 的可重复受控基线和 Phase 0D 初始评测集已完成；Phase 0B 的步骤、断言和证据格式已冻结，但 1.3/1.4 实机往返尚未执行。因此本轮**不宣告 Phase 0 退出，不进入 Phase 1**。
2. 下一入口只能是执行 7.7.14 两个版本的旧档往返并回填证据。两版均通过后，将 7.7.2 的 Phase 0B 改为完成并确认 8.2 前置条件；不得以编译或聚合日志代替该门槛。
3. 门槛通过后的 Phase 1A 文件所有权：未来实现者只负责新增 `DynamicPolicyObjectAdapter.cs` 和对 `CustomPolicyBehavior.cs` 做薄委托改动；`PolicySystemLog.cs` 仅保留现有观察点，其他 Repository、效果、prompt、保存格式和 `hasTimedEffect` 逻辑不在该批次所有权内。
4. Phase 1A 必须逐项迁移并保持等价的方法/入口为：`CustomPolicyBehavior.cs:419` 的 `INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()`、`:1034` 的 `InitializeLoadedDynamicPoliciesBeforeNonReadyCleanup()`、`:1478` 的 `EnsureDynamicPoliciesRegistered(bool)`、`:1606` 的 `FindDynamicPolicyDecisions(...)`、`:1618` 的 `IsUsableDynamicPolicyDecision(...)`、`:1631` 的 `TryRebindDynamicPolicyDecision(...)`、`:2727` 的 `EnsureDynamicPolicyObject(...)`、`:2770` 的 `TryUnregisterDynamicPolicyObject(...)`、`:2871` 的 `LoadDynamicPolicies()`、`:2886` 的 `StoreDynamicPolicy(...)`、`:2895` 的 `SyncData(...)` 动态 registry 边界，以及 `:7274` 的 `TryInitializeDynamicPolicyObject(...)`。行号只对应本轮工作树，实施前必须重搜符号。
5. Phase 1A 的硬断言仍是：handler 接口和 pre-cleanup 时序不变；`_afDynamicPolicyRegistry_v1`、状态字符串、`RecordId/PolicyObjectId` 与初始化字段不变；canonical、active member 和 unresolved decision 多引用处理不变；没有 active effect 的 live 政策仍恢复；算法、legacy 生命周期、Repository 和 `hasTimedEffect` 均不重写。

7.7.19 产品方向调整后的最小维护批次（2026-08-07）：

1. 用户决定暂停完整政策系统重构，不进入 Phase 1—7。本节覆盖 7.7.18 的“下一入口”安排：只有用户以后明确恢复大重构时，才重新执行 Phase 0B 门槛并启用 Phase 1A 交接；当前只维护已经获得玩家认可的生产政策功能。
2. `CustomPolicyBehavior.BuildRecentPlayerPolicyPromptContext()` 直接合并现有 `_policyRecordHistory` 与 `_localPolicyRecords`，按 `SubmittedDay/CreatedUtcTicks/RecordId` 稳定选择最近两条成功发布记录，再按由旧到新顺序注入玩家政策 prompt。范围包括玩家王国、地方和附庸国政策；附庸国记录继续使用现有 `PolicyScopeVassal`、目标国家和效果摘要，不新增保存字段或第二份历史真相。
3. 历史上下文每条政策正文最多 220 字、效果最多 180 字，只格式化最终两条，并明确把旧正文视为历史数据而不是系统指令。读取发生在玩家主动提交政策时，不进入模型 getter、DailyTick 或其他热路径；当前实现仍需低频扫描并反序列化已有有界记录字典，未为两条历史引入新缓存或索引。
4. “简单效果模块”不建立 Phase 4—6 的新目录、ONNX 模块索引或新执行器。现有 sparse effect IR 的 9 个稳定 metric 直接作为轻量模块 ID：`prosperityPerDay`、`foodPerDay`、`hearthPerDay`、`loyaltyPerDay`、`securityPerDay`、`militiaPerDay`、`taxIncomePct`、`constructionPerDay`、`kingdomStabilityOnce`。prompt 只要求选择相关模块，允许零/一/多个；代码继续复用现有目标句柄校验、数值解析、缩放、active effect、到期和保存逻辑。
5. 本批次没有把数值强度改成新的固定模块数值，也没有切换到本地 ONNX 自动选模块；LLM 仍返回已有模块的有符号数值。这是为避免改变当前玩家满意的平衡和边缘行为而刻意保留的最小边界，不能对外宣称完整确定性模块引擎已经完成。
6. 反射回归已构造王国、地方、附庸四条交错历史，确认只保留最新两条、包含附庸政策且排除更旧记录；同时确认 9 个目录 ID 全部被现有 `IsSupportedPolicyMetric(...)` 接受，未知 ID 被拒绝。
7. 权威 `一键编译覆盖推送/build_single_module.ps1 -Stage` 已在最终源码上执行：Bannerlord 1.3、Bannerlord 1.4 和 Bootstrap 均 `0 warning / 0 error`，统一 Stage 成功且未修改游戏目录。尚未验证真实游戏内 LLM 请求正文与最终玩家可见效果，因此本批次不宣称完成实机验收。

7.7.20 Phase 0 本地归档（2026-08-07）：

1. 完整重构继续暂停，不进入 Phase 1。原唯一大纲从 `docs/` 移入 `Phase0_Local_Archive/policy_system_refactor_outline.md`，没有复制或创建第二份重构大纲；第 8 节及之后仅保留为历史规划。
2. 基线脚本、两套分离 JSONL 与四份既有报告统一归档在 `Phase0_Local_Archive/`。运行器默认把新报告写入归档内的 `reports/`；可丢弃 runtime shadow 仍留在忽略的 `bin/Debug/policy_phase0_runtime/`，不属于归档且本批次未删除。
3. 四份报告保持原始证据内容不改；其中历史 `evaluation_sets.path` 仍记录移动前路径，评测集身份以报告内 SHA-256 和归档文件实算 SHA-256 对照为准。
4. Phase 0 唯一生产代码残留仍是 `PolicySystemLog.cs` 放行 `pre-cleanup-policy-restore-complete` 聚合日志，不能移入资料文件夹。7.7.19 的最近两条玩家政策与轻量效果模块实现属于当前生产功能，也不属于 Phase 0 资料归档。

## 8. Phase 1：存档安全底座与等价架构收口

### 8.1 目标

在不改变任何玩家可见行为的前提下，把最危险的读档恢复、分散字典写入和 `hasTimedEffect` 多重职责收口，为之后的政策库与期限切换建立安全边界。

### 8.2 前置条件

Phase 0 全部门槛通过；至少一组 1.3 和一组 1.4 旧档可重复执行“加载 → 保存 → 退出 → 再加载”。

### 8.3 子批次与实施项

8.3.1 Phase 1A：精确抽离 `DynamicPolicyObjectAdapter`。

1. 只迁移现有动态 `PolicyObject` 创建、清理前恢复、活动成员和未决议程对账；
2. 保持 `INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()` 调用时序、保存键、状态和对象 ID 完全不变；
3. 不重写算法，不顺手修正 legacy 生命周期。

8.3.2 Phase 1B：建立 Repository 兼容门面。

1. 先包住 `_dynamicPolicyRegistry`、`_activePolicyEffects`、`_localPolicyRecords`、`_policyRecordHistory` 和 NPC 记录桥；
2. 所有旧 DTO/旧 JSON/旧状态仍照常读写；
3. 把写入口逐项登记，未纳管旁路直接阻塞下一子批次；
4. 不同时保留“门面新字典”和“旧字典”两套权威真相。

8.3.3 Phase 1C：增加 legacy migration dry-run 与隔离元数据。

1. 迁移器先读 active effect 原始字典，再做分类；
2. 对 `expiry_vote_pending`、反向决议和 `NaturalExpiryAgendaRejected` 只收集证据，不乐观判定续期或废除；
3. 证据不足的记录进入独立 `LegacyDecisionReconciliationPending` 元数据；
4. 元数据以 `RecordId` 为主键、`PolicyObjectId` 为交叉核对键，保存隔离原因、证据摘要、首次/最近检查版本；
5. 重复 dry-run 幂等，不回写 legacy 业务字段，不合成新终态。
6. 若 `RecordId` 缺失、为空或冲突，使用“legacy 保存键 + 字典 entry key + 原始 DTO 规范化哈希”生成确定性隔离键；`PolicyObjectId` 只作交叉核对，不作为唯一主键前提。即使两个 ID 都缺失，重复 dry-run 和隔离持久化也必须命中同一条元数据。

8.3.4 Phase 1D：增加新 schema 的双读/shadow 能力。

1. 系统能读旧 schema 并投影新模型，但生产仍写旧 schema；
2. 投影结果与旧对象图进行差异报告；
3. `status=active + 无 active effect` 必须投影为政策仍 Active，而不是自动续期/废除；
4. `PolicyObject` 恢复以政策记录为依据，不以效果存在为前提。

8.3.5 Phase 1E：等价拆开 `hasTimedEffect`。

至少分离以下判定：命令是否成功、是否扣费、是否写历史、是否发经验、是否存在效果计划、是否激活效果、是否安排效果到期、是否安排旧自然到期议程。该子批次仍保持旧结果完全等价，不提前停掉旧到期链。

### 8.4 性能约束

1. Repository 门面不得在热路径增加 JSON 往返、反射或全字典复制；
2. 新 schema shadow 只在加载、保存或明确诊断时运行，不进入模型 getter；
3. 写入口收口后开始暴露增量变更事件，但暂不重建完整运行时快照。

### 8.5 存档与迁移约束

1. 旧保存键继续原样读取和写入；
2. 单条迁移失败只隔离该记录，不污染其他记录；
3. 加载/保存/再加载不得重复创建 `PolicyObject`、活动成员、未决议程、效果或隔离元数据；
4. 1.3 与 1.4 都必须实测 pre-cleanup restore；
5. 不删除任何 legacy 读取或恢复路径。

### 8.6 退出门槛

1. `PolicyObject` 修复链在旧档矩阵中零退化；
2. 所有已知政策/效果/地方记录/NPC 写入都经兼容门面或被明确列为下一阶段阻塞项；
3. dry-run 可稳定分类且不改写原记录；
4. 新模型 shadow 的差异均有解释；
5. 拆开 `hasTimedEffect` 后玩家可见行为与基线一致；
6. 两个 Bannerlord API 版本的目标构建与旧档往返验证通过。

### 8.7 本阶段不做

不改期限规则、不启用新政策库 prompt、不改变 AI JSON、不创建模块生产执行、不删除续期/自然到期代码。

## 9. Phase 2：政策库、历史检索与关系事实层

### 9.1 目标

交付第一条产品主线的首个可用版本：所有正式政策形成统一可查询历史，玩家每次发布前都通过 ONNX 读取相关现行/废除政策，NPC 生成链按同一文档契约接入；关系先成为可持久化、可检索、可审计的事实。

### 9.2 前置条件

Phase 1 Repository 双读和稳定 ID 投影可用；政策提交成功与草案失败能够可靠区分；ONNX 模型指纹和现有知识库调用方式已复核。

### 9.3 实施项

9.3.1 建立统一 `PolicyLibraryEntry` 投影，覆盖玩家王国、地方、附庸和 NPC。所有来源使用相同字段与版本协议，来源差异只进入 `SourceKind`、scope 和 target summary。

9.3.2 建立独立政策语料版本和不可变 `PolicySemanticIndexSnapshot`：

1. 文档只在权威字段或可见性变化时增量更新；
2. 后台构建新 snapshot，完成后原子替换；
3. 查询线程只读 snapshot，不持长锁；
4. 换档、文档版本或模型指纹变化时精确失效。

9.3.3 每次查询先按存档、可见性、来源/scope、政策状态和目标资格做硬过滤，再执行：

1. 一次 query embedding；
2. 对过滤后文档做精确 dense recall；
3. 固定候选集至多一次 batch cross-encoder rerank；
4. 稳定 tie-break；
5. 按条数、单条长度和总字符预算组装上下文。

9.3.4 玩家王国、地方、附庸的发布前检索在同一批次启用，不允许某一玩家来源继续跳过政策库。NPC 使用同一索引和上下文格式，在其生成入口通过独立集成门槛后启用。

9.3.5 prompt 必须分成两组：

1. 【当前有效/仍存在的相关政策】：包括仍 Active 但效果已过期或为空的政策，并明确当前效果状态；
2. 【已废除的相关历史政策】：独立低权重配额，标注废除日期、原因和“当前无效果”。

9.3.6 废除政策不能占满现行政策配额；现行和历史各自 Top-K、字符预算和最低分数独立配置。被拒绝、撤回、生成失败草案不进入任一组。

9.3.7 建立 `PolicyRelationStore`：

1. 支持 `Supports`、`Complements`、`ConflictsWith`、`Limits`、`Replaces` 等稳定枚举；
2. AI 可返回受限关系建议，但不能直接写入 Repository；
3. 本地代码校验端点是否在本次相关候选或明确允许集合中、关系类型是否合法、方向是否成立；
4. 提交成功后才持久化关系；失败/未知端点只记录诊断；
5. 维护按政策 ID 的不可变邻接快照，不在查询时全图扫描。

9.3.8 Phase 2 的关系主要服务检索、AI 上下文、UI 解释和以后机械裁决。尚未有明确确定性规则的关系不得在本阶段直接改变数值效果。

9.3.9 政策正文、反馈或状态变化使用版本化文档替换；不得把同一政策每次查询生成的新摘要当新文档追加，避免语料无限膨胀。

### 9.4 性能约束

1. ONNX 构建与查询不在游戏主线程运行；
2. 索引状态显式为 `not_started`、`building`、`ready`、`failed`，调用方等待异步结果或明确拒绝，不空转轮询；
3. 生产索引未 ready/failed 时不得以 sparse 检索成功发布；
4. 初期使用硬过滤后的精确 O(N×D) dense recall，只有真实规模测量证明需要时才另立 ANN 方案；
5. 关系查询使用邻接快照与受限深度/数量，不做全库图遍历；
6. 缓存键至少包含 save identity、corpus type、corpus version、model fingerprint、query hash、recallTopK 和 rerankTopK。

### 9.5 存档与迁移约束

1. 政策库是权威记录的可重建投影，索引缓存损坏可以重建，但政策记录不能依赖缓存恢复；
2. 稳定政策 ID 不得因重建索引变化；
3. 旧档加载后先恢复政策记录与 `PolicyObject`，再异步构建索引；
4. 关系边保存失败不能回滚已经成功的旧政策记录；提交计划必须在写入前完成一致性校验。

### 9.6 退出门槛

1. 四类来源正式政策都可投影且无重复 ID；
2. 玩家三来源每次发布都能读取限定预算内的相关政策；
3. 已废除政策在相关时可召回、降权、限额、明确标记，且不进入效果快照；
4. 拒绝/撤回/失败草案不会污染检索；
5. Top-K 正例、困难负例和稳定性评测达到在 Phase 0 冻结的门槛；
6. 冷启动和查询 P50/P95/Max、分配量、文档数与 Top-K 已记录且无主线程 ONNX；
7. sparse 仅存在于诊断/shadow 日志，不在生产成功路径。

### 9.7 本阶段不做

不切换政策/效果期限语义，不让关系直接执行未定义数值，不移除 AI 数值效果，不创建玩家扩展模块。

## 10. Phase 3：来源统一与政策/效果期限语义切换

### 10.1 目标

让四类来源共享同一生命周期模型与状态机真相，并正式实现各自已确认的期限意图：玩家空天数永久弱、正天数有限强；NPC 政策永久、效果有限；任何效果到期都不再自动续期或废除政策。

### 10.2 前置条件

Phase 1 的 Repository、migration dry-run、新 schema 双读和 `hasTimedEffect` 等价拆线通过；Phase 2 能检索“政策存在但无活动效果”的文档；旧档迁移矩阵已跑通。

### 10.3 实施项

10.3.1 完成统一 `PolicySourceAdapter`、`PolicyTargetResolver` 与 `PolicyCommitAdapter` 边界：

1. 核心命令、政策记录、效果生命周期和提交计划不再按玩家王国/地方/附庸复制；
2. 王国议程、地方直接发布、资源成本、附庸独立度、脱离阈值和回滚只留在适配器；
3. NPC 议程/审批接同一核心，但保留其来源资格和公告流程。

10.3.2 在玩家三来源原子切换前，逐项收口地方/附庸所有 active 写路径：发布、续约、主动停止、关系终止自动结束、目标丢失自动结束，以及 `_localPolicyRecords` / `_activePolicyEffects` 的每个写入点。未证明无旁路时不得切换。

10.3.3 玩家前处理直接从输入产生期限意图：

1. 空天数 → `PermanentWeak`；
2. 正天数 → `TimedStrong`，玩家输入是唯一效果期限；
3. AI 返回的旧 `durationDays` 在兼容期被忽略，不能覆盖玩家意图。

10.3.4 在模块系统生产切换前，旧 AI 数值先进入 `LegacyAiEffectAdapter`：

1. `PermanentWeak` 使用本地、版本化、可测试的弱效果限幅/缩放配置；
2. `TimedStrong` 使用本地强效果配置并保存绝对到期日；
3. 具体倍数、上下限和曲线在进入本阶段生产切换前冻结，不在大纲中擅自写死；
4. 旧 AI 数值不能继续向新领域模型扩散。

10.3.5 NPC 继续暂读旧 AI 有限数值和天数，但天数只属于效果；停止新建自然到期议程。效果到期只标记 `Expired` 并更新快照，政策仍 `Active`。

10.3.6 对新 schema 停止创建 `expiry_vote_pending`、自然到期废除和自动续期。主动废除改为显式 `DecisionIntent = ExplicitAbolition`，继续走现有合法审批/提交适配。

10.3.7 原版 `PolicyObject` 活动成员关系只跟政策状态同步：政策 `Active` 即使没有效果也必须留在原版政策界面；只有明确主动废除或经过产品确认的 owner-unavailable 规则才移除。

10.3.8 地方/附庸 legacy 记录迁移规则：

1. `active` → Active 政策 + 原剩余有限效果；
2. `expired` → Active 政策 + `Expired` 效果；
3. `targets_lost` / `relationship_ended` → 保留非废除政策事实，效果为 `TargetUnavailable` 并保留原原因；
4. 只有明确 `abolished` → `Abolished`；
5. 旧续约 UI 只有在玩家三来源的相关记录全部迁移成功后才停用；任一来源仍有失败记录时，整个玩家三来源切换均不发布。按来源保留旧入口只用于发布前 dry-run/隔离诊断，不允许形成生产半切换。

10.3.9 dynamic/NPC legacy `expiry_vote_pending` 继续按证据对账；证据不足保持 `LegacyDecisionReconciliationPending`，不乐观转成 Active、Abolished 或 Renewed。

10.3.9.1 NPC legacy `approved_pending_commit` 不是新 schema 的长期状态，而是 commit-in-flight 过渡态。证据充足时幂等补完同一提交并转为 `Active`；证据不足时进入 `LegacyDecisionReconciliationPending`。不得直接转为 `Abolished`、`Renewed`、`Rejected`，也不得重复执行成本、通知或一次性副作用。

10.3.10 UI 同步更新：

1. 输入提示明确“留空＝永久弱效果，填写天数＝有限强效果”；
2. 政策状态与效果状态分开展示；
3. 有限效果显示到期日/剩余日，永久效果显示“永久弱效果”；
4. 效果到期后显示“政策仍有效，当前机械效果已结束”；
5. 旧续约按钮在安全迁移后改为只读历史，不允许新旧语义并跑。

### 10.4 性能约束

1. 永久效果不进入每日倒计时；
2. 有限效果使用绝对到期日和到期桶/小顶堆，只处理当天到期项；
3. 状态变化通过增量事件更新政策库投影和运行时快照，不在每日 tick 全库重算；
4. 来源适配不得在热路径重复展开 TaleWorlds 对象集合。

### 10.5 存档与迁移约束

1. 新写入和“停止新自然到期链”必须在同一原子发布切换，不能先做一半；
2. 旧 schema 读取、隔离和受控迁移继续保留；
3. 老存档不因加载新版本被批量自动改写为 `Abolished`、`Rejected` 或 `Renewed`；
4. 玩家三来源的新期限写入在同一发布批次以 all-or-nothing 启用；
5. 任一玩家来源未通过迁移/切换门槛时，整组三来源都不启用新期限写入；“保持旧模式”只能是整组回退。按来源阻止写入或保留旧按钮只用于未发布的 dry-run/隔离处理，不通过生产半切换制造无人管理记录。

### 10.6 退出门槛

1. 玩家三来源空天数与正天数行为符合 2.1；
2. NPC 效果到期后政策仍存在且不出现续期/废除议程；
3. `Active + no effect` 读档后仍恢复同 ID `PolicyObject` 和原版活动成员；
4. 地方/附庸所有旁路写入已接门面或明确停用；
5. 扣费、经验、历史、公告和一次性副作用不再由“是否 timed”间接决定；
6. 旧档加载/保存/再加载幂等，隔离记录不扩散；
7. 1.3、1.4 与 Bootstrap 目标验证通过。

### 10.7 本阶段不做

不宣称效果模块系统已经完成；生产效果仍可由 `LegacyAiEffectAdapter` 读取旧 AI 数值。除期限必要改动外，不调整现有数值平衡。

## 11. Phase 4：效果模块合同、目录与等价证明

### 11.1 目标

先证明模块系统能够完整、确定性地表达现有机械效果，再考虑语义选择或生产切换；本阶段生产效果来源仍是旧 AI 数值路径。

### 11.2 前置条件

Phase 3 的生命周期、来源适配和提交边界稳定；现有全部指标、目标展开、成本缩放和一次性副作用已经形成清单与回放样本。

### 11.3 实施项

11.3.1 定义纯数据模块描述符，至少包含：稳定 `ModuleId`、版本、显示名、语义描述、支持来源/scope、需要的目标种类、支持的期限意图、冲突组、依赖、最大实例数、优先级和数值边界。

11.3.2 定义纯函数合同：

1. `CanSelect(PolicySemanticContext)` 只判断资格，不访问 TaleWorlds 对象、不写状态；
2. `BuildPlan(PolicyModuleBuildContext)` 只生成确定性 `PolicyEffectPlan`；
3. 相同输入、目录版本和配置必须得到相同计划；
4. 模块不得直接扣费、改存档、发通知或执行一次性副作用。

11.3.3 建立统一合法目标解析结果 `ResolvedPolicyTargets`，覆盖：

1. 玩家王国目标；
2. 地方 `S/L/C/R` 及生命周期 anchor；
3. 附庸 `K0/K1/K2+` 双边目标；
4. NPC self 与允许的 foreign candidate；
5. 目标解析只依据政策正文、来源快照和允许候选，公开反馈不能新增目标。

11.3.3.1 每次发布在模块选择前只产生一个冻结的 `ResolvedPolicyTargets`。`BuildPlan(...)` 只能使用其中已批准的目标，不得新增、重定向、扩展、折叠或替换目标；NPC self/foreign 身份在 resolver 后冻结到 commit。公开反馈、模块检索、关系裁决和成本缩放只能在已授权目标集合内选择、抑制、增强或置空效果，不能改写目标集合、scope 或 foreign legality。

11.3.4 为当前所有真实指标建立内置模块，至少覆盖繁荣、食物、炉火、忠诚、安全、民兵、税收、建造力和王国稳定度一次性变化；每个模块都提供正负方向、目标资格、弱/强档和全局限幅规则。

11.3.5 来源特有副作用保持边界清楚：

1. 附庸独立度、发布成本、脱离阈值和回滚默认留在来源/提交适配器；
2. 若以后抽成专用模块，必须单独定义幂等与不可重复结算规则；
3. 不能把代码已额外结算的附庸代价再次混入普通模块。

11.3.6 先建立“旧 numeric DTO → 模块等价计划 → numeric operation”回放适配，对玩家和 NPC 现有样本验证：目标、期限、符号、数值、原因、一次性语义和下游 active effect 注册均等价。

11.3.7 成本缩放固定发生在完整计划编译之后、提交之前，继续保持“先确定政策本体效果，再按实际支付比例缩放最终数值”的现有意图。模块选择本身不因支付比例变化。

11.3.8 一次性效果只由统一 committer 执行，使用至少由政策 ID、模块 ID、模块版本、目标 ID 和动作种类组成的幂等键；读档、重试、续接或 agenda commit 都不得重复结算。

11.3.8.1 恢复、重新激活、旧续约恢复、关系解除和读档重建默认都不重放一次性副作用；只有产品规则明确创建了新的政策实例/效果实例时，才允许产生新的幂等键。计划或模块版本变化本身不能绕过旧执行键。

11.3.9 模块目录在启动/加载阶段一次性构建为不可变快照，校验 ID 冲突、版本、依赖环、冲突组和非法配置；禁止每次政策发布时反射扫描程序集。

### 11.4 性能约束

1. `CanSelect` 与 `BuildPlan` 只操作已捕获纯数据和有界候选；
2. 模块目录读取 O(1)，依赖/冲突裁决只针对 Top-K 候选，不遍历所有模块；
3. round-trip 回放属于测试/诊断，不进入生产发布热路径；
4. 模块配置解析和反射发现只允许启动时一次。

### 11.5 存档与迁移约束

1. 本阶段不改变 active effect 生产保存结构；
2. 新模块计划先作为 shadow/诊断数据，不覆盖旧权威效果；
3. 模块 ID 与版本一旦进入测试存档即按持久化标识管理，禁止随意改名复用；
4. 未知模块版本只能标记 `ModuleUnavailable`，不能删除对应政策。

### 11.6 退出门槛

1. 现有全部指标和一次性副作用均有表达路径；
2. 玩家三来源与 NPC 的旧效果样本 round-trip 无未解释差异；
3. target resolver 不串地方/附庸/NPC foreign 目标；
4. 一次性副作用在初次提交、重试、读档中都只执行一次；
5. 成本缩放顺序与基线一致；
6. 目录校验、模块合同单测和双版本编译通过。
7. 等价回放不仅比较 DTO 字段，还比较下游 active effect 注册前输入和注册后持久化结果，至少覆盖 RecordId、target handle/label、期限、地方生命周期 anchor、zero-effect anchor、一次性幂等语义和 `_activePolicyEffects` 结果。

### 11.7 本阶段不做

不让 AI 输出模块 ID，不用语义结果执行真实效果，不删除旧 numeric JSON，不开放外部 DLL。

## 12. Phase 5：模块语义索引、shadow 与评测门槛

### 12.1 目标

用本地 ONNX 从政策语义选择零个、一个或多个效果模块，在不影响生产效果的前提下积累可解释差异和性能数据，直到达到生产切换门槛。

### 12.2 前置条件

Phase 4 模块目录、目标解析、确定性计划和等价回放稳定；民众反馈已经在模块选择前生成；政策历史索引与模块索引能够使用独立缓存命名空间。

### 12.3 实施项

12.3.1 建立独立 `PolicyEffectModuleDocument` 和不可变模块索引 snapshot。文档由模块描述符、适用场景、正反例提示和版本生成，不把玩家政策全文追加到模块语料。

12.3.2 模块查询文本固定由以下纯数据组成：政策名称、政策正文、AI 民众公开反馈、来源/scope、合法目标摘要和期限意图。AI 不提供模块 ID，旧 AI 数值不进入模块检索查询。

12.3.2.1 民众反馈可以影响已冻结目标集合内的模块类型判断、强度档、社会反应解释和同目标优先级，但不得改变 `ResolvedPolicyTargets` 的目标集合、scope 或 self/foreign 合法性。有无民众反馈时授权目标集合必须一致；模块命中集合和强度可以不同。

12.3.3 选择算法顺序固定：

1. 按来源、scope、目标种类、期限支持做硬过滤；
2. 一次 query embedding；
3. 精确 dense recall；
4. 至多一次 batch rerank；
5. 应用最低相关阈值，允许零命中；
6. 取有界多个候选；
7. 按稳定优先级/分数/ModuleId 做 tie-break；
8. 解冲突、补依赖并再次校验资格；
9. 施加每政策模块上限、每指标/目标上限和全局数值边界；
10. 生成确定性 shadow `PolicyEffectPlan`。

12.3.3.1 zero-module/no-match 对玩家王国、地方、附庸都不是失败。Phase 5 不得因此短路现有生产链；Phase 6 切换后也必须保留各来源正常的审批/发布、记录、反馈、议程或来源状态流转，仅机械 `PolicyEffectPlan` 为空。成本、经验和附庸等非机械副通道按进入 Phase 6 前冻结的产品规则处理。

12.3.4 玩家王国、地方、附庸在同一 shadow 批次运行，但分别报告 target/scope 差异；生产仍统一使用旧 AI 数值，不双执行 shadow 计划。

12.3.5 NPC 使用同一选择器，并分别评测 self 与 foreign：

1. foreign 候选必须先由本地 resolver 生成；
2. 政策正文必须明确提及候选；
3. 公开反馈、影响摘要或事件前提不能单独制造 foreign target；
4. 一项 NPC 政策的 self/foreign 数量继续受本地上限约束。

12.3.6 shadow 差异至少记录：目标集合、指标集合、符号、强度档、期限意图、模块数、零命中、冲突/依赖裁决、一次性副作用和成本缩放后结果。

12.3.6.1 另外执行有/无民众反馈的目标保真检查：授权 target set、scope 或 self/foreign 身份出现任何差异都直接记为 blocker；模块类型/强度变化只按语义正确性评测。

12.3.7 评测集至少覆盖：

1. 单模块正例；
2. 相邻模块困难负例；
3. 明确无机械效果的 no-match；
4. 多个互补模块；
5. 冲突组互斥；
6. 依赖补全/依赖不满足；
7. 相同输入稳定性；
8. 玩家三类 scope；
9. NPC self/foreign 合法与非法目标；
10. 已废除政策只影响上下文、不直接生成当前效果。

12.3.8 评测样例默认不自动写回模块检索主语料，避免把测试答案泄漏进索引；训练/调参资料与正式评测集分离并版本化。

### 12.4 性能约束

1. 模块索引状态同样显式为 `not_started/building/ready/failed`；
2. 每次选择只做一次 embedding 和零/一次 batch rerank，禁止逐候选 `TryScore`；
3. 模块目录版本变化只重建模块 snapshot，不使政策历史索引失效；
4. 查询结果缓存键沿用第 16.3 节的完整最小集合，并额外包含 module catalog version 与 scope/target hard-filter version；
5. shadow 有采样/限流和有界日志，不能因诊断让每个 NPC 周期重复双倍推理；
6. 所有推理在后台，主线程只接收带运行代数的结果。

### 12.5 存档与安全约束

shadow 结果默认不成为权威存档，不执行效果、不扣费、不发通知、不写一次性幂等键。若为对照保留诊断记录，必须有版本、大小上限和清理策略。

### 12.6 退出门槛

1. 正例 Top1/Top-K、no-match、多模块、冲突/依赖和稳定性达到事先冻结的阈值；
2. 玩家三来源无 scope/target 越权；
3. NPC foreign 无反馈诱导越权；
4. 所有未解释 shadow 差异完成分类；
5. 冷启动、单次查询、batch rerank 的 P50/P95/Max 和分配量满足 Phase 0 确定的预算；
6. 未 ready/failed 时明确失败，未出现 sparse 或旧数值替代模块选择的生产假成功；
7. 生产切换所需的数值边界、模块上限和 tie-break 已版本化冻结。

### 12.7 本阶段不做

不执行 shadow 计划，不修改 active effect，不停止旧 AI 数值输出，不开放玩家自定义 DLL。

## 13. Phase 6：模块化生产切换、关系裁决与高性能运行时

### 13.1 目标

让本地语义模块成为唯一生产效果来源，结束 AI 可执行数值 JSON；同时把已经冻结的政策关系规则接入确定性计划裁决，并把所有模型热路径改为只读快照。

### 13.2 前置条件

Phase 5 全部语义与性能门槛通过；模块目录、阈值、弱/强配置、一次性幂等、零命中产品提示、关系机械规则和旧档 module-unavailable 行为均已冻结。

### 13.3 Phase 6A：玩家三来源原子切换

13.3.1 玩家 LLM 契约删除可执行数值效果和 AI 决定天数，只保留政策评价、影响叙述、民众反馈和受限关系建议。

13.3.2 生产链固定为：

1. 前处理与政策历史检索；
2. AI 评价和公开反馈；
3. 本地合法目标解析；
4. ONNX 模块 dense recall + batch rerank；
5. multi/no-match、冲突、依赖和全局边界裁决；
6. 模块生成确定性 `PolicyEffectPlan`；
7. 成本缩放；
8. 主线程重新校验并原子提交政策、关系、效果、成本、历史和通知。

13.3.2.1 同一政策内全部可缩放机械模块使用同一实际支付比例；支付比例不得改变模块选择、目标或期限，只能缩放已生成计划的数值幅度。一次性副作用是否缩放必须与 Phase 0 基线一致并单独验收；zero-module 时的成本按 20.2 冻结规则执行。

13.3.3 玩家王国、地方、附庸先在隐藏 staging/shadow 路径分别通过完整提交流程门槛，再由一个生产开关在同一发布版本 all-or-nothing 切换。任一来源未通过时整组不切换；生产不能留下某一来源继续让 AI 决定数值。

13.3.4 已切换后不允许请求级回退到旧 AI numeric JSON；模块索引未 ready/failed 时等待或明确拒绝。索引 ready 且没有模块达到阈值时按零命中正常发布。

13.3.4.1 玩家 legacy numeric → legacy module 桥在 Phase 6A 后只允许用于旧档读取、恢复和历史兼容；新创建、新提交以及任何 legacy 续约恢复后的新效果计划一律禁止走该桥，并记录生产调用计数为零。

13.3.4.2 玩家 zero-module 政策仍完成各来源正常审批/发布、稳定记录、原版成员、政策库、关系、反馈和来源提交状态；只是不创建机械 active effect。非机械成本/经验/附庸副通道按已冻结规则执行，不从空计划偷偷推导。

13.3.5 同一政策不得同时执行旧 numeric effect 和新 module plan。提交前以来源、记录 ID 和计划版本断言唯一执行源。

### 13.4 Phase 6B：NPC 生产切换

13.4.1 NPC 使用同一模块目录、选择器、计划和运行时，政策仍永久、效果按模块配置有限。

13.4.2 先通过 NPC self 集成门槛，再启用 foreign；foreign 始终受 allowed candidate、正文明确提及和数量上限三重校验。两个子批次共享一套代码，不复制引擎。self 已切而 foreign 尚未切的短暂阶段中，每个目标效果只能有一个明确来源：self 使用模块计划，foreign 继续由单一 legacy 适配源生产并投影到统一查询模型；不得对同一效果双写、混合解释或让外部查询直接读取两套 DTO。该 mixed-source 只允许存在于 Phase 6B 的限时过渡子批次，必须有诊断计数和退出期限，不能成为长期模式；foreign 门槛通过后，同一 NPC policy 的全部新效果计划统一来自 module path，新 NPC 提交彻底禁用 numeric 源，Phase 6B 不得在 mixed-source 尚存时完成。

13.4.3 NPC prompt 同样删除可执行 numeric effect 与模块 ID；默认有限天数、强度和每个模块的适用范围由本地版本化配置决定。

13.4.4 NPC world event、延迟公开反馈、weekly material、外交快照和 agenda 状态继续读取统一政策/效果查询模型，不能再从旧 DTO 猜当前效果。

13.4.5 Phase 6B 完成前必须对 world event、延迟公开反馈、weekly material、外交快照和 agenda 上下文逐入口验证：全部只从统一查询模型取数；直接读取旧 DTO 或以旧 `RemainingDays/IsEnded` 推断政策状态的诊断计数为零。

### 13.5 Phase 6C：政策关系机械裁决

13.5.1 关系事实在计划提交前形成有界 `PolicyRelationResolution`，只允许执行已冻结的确定性规则。未定义机械语义的关系继续只用于检索和解释。

13.5.2 `Replaces`、`ConflictsWith`、`Limits` 等若会抑制、结束或限幅效果，必须明确：方向、作用范围、优先级、是否可恢复、恢复时是否重新计算到期日、一次性效果是否永不重放。

13.5.3 被关系抑制的效果使用 `SuppressedByPolicyRelation`，不把政策改为 `Abolished`；废除关系端点时按已冻结恢复规则增量重算受影响邻接项。

13.5.4 `Supports` / `Complements` 若允许增强，只能通过确定性配置和全局上限调整计划，不能把 AI 的自然语言强度直接转成任意数值。

### 13.6 运行时与性能实施项

13.6.1 Repository 每次效果或关系提交后，在后台/提交尾部构建新的不可变运行时快照并原子替换。

13.6.2 为繁荣、食物、炉火、忠诚、安全、民兵、税收、建造力和其他模型 getter 建立按目标 ID/指标的聚合值；Harmony 热路径只做 ID 查找和数值相加，目标为 O(1) 或 O(k_small)，不扫描全效果、不反序列化 JSON、不分配 `ToList()`。

13.6.3 永久效果没有每日工作；有限效果使用绝对到期桶/小顶堆，每日只处理到期项并增量更新受影响目标快照。

13.6.4 关系变更只重算关系邻接范围和受影响模块，不全库重建；政策库文档和效果快照使用不同版本号，避免无关失效。

13.6.5 UI 使用分页/有界查询模型，不能为打开页面反序列化全历史；日志只写 ID、版本、阶段、耗时和有界候选摘要。

### 13.7 存档与迁移约束

1. 新存档保存模块 ID、模块版本、强度档、目标、计划版本、编译后的必要操作和一次性执行键；
2. 旧 numeric effect 继续兼容读取并投影为 legacy module/operation，但该桥只服务旧档恢复与历史兼容；对应来源完成 Phase 6 切换后，新创建、新提交和新效果计划不得调用它；
3. 模块缺失/版本不可解析时只把对应效果标为 `ModuleUnavailable` 并保留政策；
4. 切换版本加载旧档、保存、再加载不得重复一次性副作用或改变剩余到期日；
5. 版本回滚方案必须在接受新 schema 写入前验证，不能依靠旧版本忽略新数据后继续保存。

### 13.8 退出门槛

1. 玩家三来源生产均无 AI numeric effect 调用和双执行；
2. NPC self/foreign 分别通过合法目标、事件、周报和外交上下文门槛；
3. multi-module、zero-module、冲突、依赖和关系抑制均按确定性规则运行；
4. 所有指标 getter 不再扫描/反序列化全 active effect；
5. 每日工作与当天到期/变更项成比例，不与全部政策×全部定居点成比例；
6. 主线程零 LLM、零 ONNX，后台结果过期时不会提交；
7. 旧档往返、一次性幂等和 1.3/1.4 构建/运行验证通过；
8. 生产索引失败不会 sparse 降级，也不会重新启用 AI 数值效果。
9. NPC world event、延迟反馈、weekly material、外交快照和 agenda 上下文直接读取旧 DTO 的诊断计数为零。
10. agenda commit 重试、读档重建、关系抑制/解除和 legacy 续约恢复均不会重放一次性副作用。

### 13.9 本阶段不做

不立即删除所有 legacy 读取代码，不在运行时下载/编译玩家代码，不把尚未定义的关系强行机械化。

## 14. Phase 7：玩家 vibe coding 与效果模块扩展边界

### 14.1 目标

让玩家或团队能够以最小、局部、可验证的改动加入新效果模块，而不修改中央 switch、不复制政策引擎，也不牺牲启动稳定性和热路径性能。

### 14.2 前置条件

Phase 6 内置模块生产稳定；模块描述符、版本、保存兼容、目标解析、全局边界和合同测试已经固定。

### 14.3 Phase 7A：源码级即插即用

14.3.1 首个支持目标是“加入一个模块类 + 描述符/配置 + 测试即可被目录发现”，不要求修改中央枚举分支或多处 prompt。

14.3.2 提供最小模块模板，明确：

1. 稳定 ID/版本命名；
2. 语义描述和适用范围；
3. 目标资格；
4. `PermanentWeak` / `TimedStrong` / `NpcTimed` 支持；
5. 冲突、依赖、数值边界；
6. 纯 `CanSelect` / `BuildPlan`；
7. 单元测试与 no-match/误匹配测试。

14.3.3 提供目录合同测试工具，启动或测试时一次性报告重复 ID、缺失依赖、依赖环、非法目标类型、超界配置和不稳定计划。

14.3.4 新模块加入后只使模块目录版本和模块索引失效，不全量重建政策历史索引。

### 14.4 Phase 7B：外部 DLL Provider（独立子阶段）

14.4.1 只有完成 Bootstrap、加载顺序、1.3/1.4 ABI、程序集冲突、异常隔离和保存版本兼容审计后，才考虑外部程序集 Provider。

14.4.2 外部 DLL 只允许启动时有界发现和校验，不在每次发布时反射扫描，不支持运行时热编译或网络下载代码。

14.4.3 安全风险必须明确展示：外部 DLL 与普通 mod 一样拥有游戏进程权限，不是沙箱；恶意或错误模块可以读写文件、崩溃游戏或破坏存档。只加载玩家明确安装和信任的程序集。

14.4.4 单个外部模块加载失败应隔离该 Provider，并把受影响旧效果标记 `ModuleUnavailable`；不能让整个政策库或其他模块失效。

### 14.5 性能约束

1. 发现、反射和描述符验证只发生在启动/显式重载点；
2. 生产选择只读取不可变目录和预计算模块向量；
3. 玩家模块不得绕过统一目标解析、全局上限、committer 或运行时快照；
4. 禁止模块自行注册每帧/每 getter 全库扫描。

### 14.6 退出门槛

1. 示例模块无需修改中央执行分支即可加入；
2. 合同测试能拒绝 ID 冲突、非法依赖和不确定性输出；
3. 新旧模块版本存档往返稳定；
4. 模块缺失只影响对应效果，不删除政策；
5. 若启用外部 DLL，双版本 ABI、加载失败隔离和安全提示已验证。

### 14.7 本阶段不做

不提供任意脚本沙箱承诺，不自动下载代码，不允许模块直接访问 Repository 内部字典或绕过提交事务。

## 15. Phase 8：旧路径清理、主文件变薄与最终收口

### 15.1 目标

只有新系统经过真实存档与运行窗口后，才删除确认无生产调用的旧执行路径，并完成职责拆分、兼容边界和最终性能收口。

### 15.2 前置条件

1. 玩家和 NPC 模块生产均稳定；
2. 新 schema 经过足够真实存档往返窗口；
3. `LegacyDecisionReconciliationPending` 已清零或只剩有证据、有人工作业说明的白名单；
4. 日志证明不存在旧 AI numeric effect 生产调用；
5. 回滚和旧档兼容策略已验证。

### 15.3 实施项

15.3.1 按“先停新生产、最后动兼容读取”的顺序清理：

1. Phase 6 切换时已经原子停止新生产 prompt 对 AI numeric 的要求，并停止新提交消费 numeric；
2. Phase 8 先用诊断计数确认玩家/NPC 新政策持续不再读取 AI numeric effect；
3. 再删除已不可达的新提交 numeric 分支和旧数值执行源调用；
4. 旧档 parser、repair、DTO/reader 与 legacy bridge 继续保留在明确 compatibility namespace/adapter 中；
5. 只有真实迁移窗口和隔离白名单允许时，最后才删除不再需要的 parser/repair/平铺字段特判。

15.3.2 只有所有 legacy 记录完成迁移后，才移除新建自然到期议程、自动续期和旧续约写入口的死代码；兼容恢复代码是否删除以真实存档窗口为准。

15.3.3 按第 4 节边界逐步让 `CustomPolicyBehavior.cs` 变薄。每次抽离一个有真实调用者和测试的职责，不做一次性巨型搬家，也不改公开 Behavior 注册名。

15.3.4 删除热路径中的旧 JSON 扫描、重复缓存和不再使用的每日遍历；以性能剖析证明替代，而不是凭代码看起来更短宣告完成。

15.3.5 更新政策系统开发文档：新增模块流程、存档兼容规则、关系语义、性能预算、故障排查和双版本验证步骤。

### 15.4 性能约束

最终剖析必须覆盖真实规模和极端规模：大量现行政策、大量已废除历史、多目标效果、多模块政策、密集关系、到期高峰、UI 翻页和读档冷启动。不得只测空档。

### 15.5 存档与安全约束

1. 不通过批量删字段/删记录“清理”旧档；
2. 不删除 `PolicyObject` pre-cleanup 保护；
3. 不改一键构建/覆盖流程；
4. 任何旧 reader 删除前都要有真实遥测/样本证明和可审查迁移说明；
5. 不回滚用户或其他开发者已有改动。

### 15.6 退出门槛

1. 无旧 AI numeric 生产调用、无双执行、无新自然到期政策议程；
2. 主 Behavior 顶层可直接看出“接收 → 前处理 → 评价 → 后处理/计划 → 主线程提交”；
3. compatibility reader 与新写入边界清楚；
4. 所有热路径达到第 16 节预算；
5. 完整旧档矩阵、语义评测、集成测试和双版本构建通过；
6. 现有单模块 Bootstrap 输出方式未改变。

### 15.7 本阶段不做

不做与两条产品主线无关的全仓清理、广泛重命名、构建脚本改写或其他系统重构。

## 16. 跨阶段性能方案与硬约束

16.1 先按运行频率设计，不把所有逻辑塞进同一“执行”方法：

| 路径 | 频率 | 允许工作 | 禁止工作 |
| --- | --- | --- | --- |
| 政策发布/生成 | 低频事件 | 一次历史检索、一次模块选择、有限 LLM | 主线程 ONNX/LLM、全库 prompt |
| 索引文档变更 | 政策提交/状态变化时 | 增量向量构建、snapshot 替换 | 每次查询重建语料 |
| 模型 getter | 极高频 | O(1) / O(k_small) 快照读取 | JSON、LINQ 全扫描、锁等待、分配 |
| 每日 tick | 每游戏日 | 处理当天到期桶和待提交事件 | 全政策×全定居点空转 |
| 关系变化 | 低频事件 | 重算受影响邻接范围 | 全关系图遍历 |
| UI 查询 | 玩家打开/翻页 | 分页只读投影 | 全历史反序列化 |
| 存读档 | 低频重操作 | O(N) 迁移/校验与有界日志 | 静默丢记录、重复创建对象 |

16.2 政策历史索引与模块索引各自维护：corpus version、model fingerprint、不可变文档向量 snapshot、query result cache 和 rerank cache。上层缓存采用有界 LRU/分代策略，不沿用“满了整表清空”作为长期方案。

16.3 缓存键至少包含 save identity、corpus type、corpus version、model fingerprint、query hash、recallTopK、rerankTopK；模块查询额外包含 catalog version、scope/target hard-filter version。

16.4 每次 query 只允许一次 embedding；候选固定后只允许零或一次 batch rerank。禁止在循环里逐候选调用 cross-encoder，禁止同一发布链重复生成相同 query embedding。

16.5 ONNX session 的并发度必须有界。在没有测量证明更高并发安全有效前，索引构建和查询采用小并发/串行批次；不做无界 `Task.Run`，不让多个大 batch 争抢 session 导致主流程抖动。

16.6 主线程只负责：捕获纯数据快照、检查索引状态、接收带代数的结果、重新校验和提交。后台结果返回时若存档代数、目标或政策命令已变化，丢弃结果并给出明确状态，不提交过期计划。

16.7 模型效果快照按目标 ID 和指标预聚合，通过原子引用替换或短临界区发布。读路径无 JSON、无反射、无全量复制，尽量无锁；写路径允许低频重建受影响小片段。

16.8 永久效果不注册每日任务；有限效果保存绝对到期日并进入到期桶/小顶堆。政策是否 Active 不参与到期调度判断，避免效果结束触发政策生命周期工作。

16.9 关系存储维护出边/入边邻接表，查询与重算都有硬数量和深度上限。只对召回候选和直接受影响政策做关系扩展，不以全库图搜索作为发布热路径。

16.10 初期坚持硬过滤后的精确 dense recall。只有基准显示文档规模、内存或 P95 已超过预算，并且 ANN 召回质量通过相同评测集时，才单独引入 ANN；不能提前增加复杂度。

16.11 每个性能报告必须同时写：P50、P95、Max、分配量、语料/效果/关系数量、Top-K、模型指纹、冷/热缓存状态和测试硬件。只报告平均值或单个最快值不算通过。

16.12 每阶段以 Phase 0 基线冻结具体预算。最低硬门槛是：

1. 主线程无 LLM/ONNX；
2. 模型 getter 不随 active effect 总数线性增长；
3. 每日维护不随全部政策×全部目标线性增长；
4. 发布查询只做一次 embedding 和一次 batch rerank；
5. 缓存、snapshot、日志和隔离记录都有明确大小上限；
6. 任何优化不得删掉既有规则、目标合法性、存档兼容或一次性语义。

## 17. 存档迁移矩阵与不可退化判据

17.1 dynamic policy 必测矩阵：

1. `pending` / `active` / `expiry_vote_pending` / `abolished` / `rejected`；
2. 原版 active member 存在/缺失；
3. unresolved decision 存在/缺失/方向不明；
4. active effect 为正天数/零天数/已结束/损坏 JSON；
5. `NaturalExpiryAgendaRejected` 有/无；
6. RecordId/PolicyObjectId 正常、缺失、冲突。

17.2 玩家地方/附庸必测矩阵：

1. `active`；
2. `expired`；
3. `targets_lost`；
4. `relationship_ended`；
5. `abolished`；
6. 旧续约 pending/成功/失败；
7. source target 存在/消失/统治关系改变；
8. 附庸独立度一次性副作用已结算/未结算/重试。

17.3 NPC 必测矩阵：

1. pending、approved_pending_commit、active、expiry_vote_pending、abolished；
2. self effect、合法 foreign、非法 foreign；
3. effect 有剩余/已到期/缺失；
4. agenda decision 存在/缺失；
5. world event、公众反馈通知、weekly material 已发/待发；
6. dynamic registry 与 NPC record 状态一致/不一致。

17.4 新模型必测矩阵：

1. Active 政策 + Permanent effect；
2. Active 政策 + Timed active effect；
3. Active 政策 + Expired effect；
4. Active 政策 + zero module/no effect；
5. Active 政策 + Suppressed/TargetUnavailable/ModuleUnavailable；
6. Abolished 政策 + 历史效果；
7. 多模块与一次性幂等键；
8. 模块目录升级/模块缺失。

17.5 每个样本执行：加载 → 校验对象图 → 原地保存副本 → 退出 → 再加载 → 再校验。判据包括：记录数、稳定 ID、政策状态、效果状态/到期日、原版成员、未决议程、关系边、一次性键和通知状态均幂等。

17.6 `LegacyDecisionReconciliationPending` 不是新政策状态，而是迁移隔离元数据。证据不足时不自动续期、不自动废除、不合成新决议；失败只隔离单记录并保留旧 DTO 原样保存。

17.7 动态 `PolicyObject` 清理前恢复始终以政策记录为依据，不能退化为“只有 active effect 存在才恢复”。这是所有阶段的硬门槛。

17.8 legacy reader 删除的前提不是“新代码已经能写”，而是所有样本、真实存档窗口和隔离白名单都证明旧数据已安全处理。

## 18. 测试、验证与阶段晋级门槛

18.1 单元测试至少覆盖：

1. 生命周期状态机与期限意图；
2. legacy 投影与幂等迁移；
3. target resolver；
4. module catalog、冲突、依赖和稳定 tie-break；
5. `PolicyEffectPlan` 确定性与全局限幅；
6. 一次性执行键；
7. 关系边校验与受影响邻接范围；
8. policy/module 两套 cache key 和失效。

18.2 语义评测至少覆盖：政策历史 Top-K、废除历史配额、困难负例、模块 no-match、multi-match、冲突/依赖、相邻模块辨析、同输入稳定性、玩家三 scope 和 NPC foreign 越权。

18.3 集成测试至少覆盖：

1. 玩家王国/地方/附庸发布、效果到期和主动废除；
2. NPC 采纳、self/foreign 效果到期、主动废除、反馈和周报；
3. 零模块政策仍发布、入库、读档后仍在原版界面；
4. 已废除政策可检索但不恢复效果；
5. 关系抑制/恢复不重复一次性副作用；
6. 索引 building/failed 时等待或明确失败，无 sparse/legacy 假成功。

18.4 兼容验证必须使用仓库既有流程分别构建 `BannerlordApi=1.3`、`BannerlordApi=1.4` 和 Bootstrap；涉及 TaleWorlds API、Campaign Behavior、PolicyObject、议程或 UI 的阶段还要执行对应版本运行/读档验证。

18.5 性能验证按第 16 节采集，并与 Phase 0 同规模基线对比。若性能门槛失败，先定位真实热点再调整；不得删功能、降低检索正确性或取消存档校验换取通过。

18.6 阶段完成必须留下可审查证据：改动文件、调用图变化、迁移结果、测试输出、性能数据、未验证项和已知风险。没有实际运行的检查不能写成“已验证”。

## 19. UI、日志与可观测性

19.1 玩家 UI 必须分别显示政策状态与效果状态，不再把“剩余天数 0”展示为政策已废除。

19.2 政策详情至少能区分：永久弱效果、有限强效果及到期日、效果已过期、无模块/无机械效果、关系抑制、目标不可用、模块不可用和政策已废除。

19.3 政策历史页支持现行/已废除筛选和分页；废除政策保留正文、发布日期、废除日期/原因和历史效果摘要，但不提供误导性的续约入口。

19.4 正常日志只记录有界结构化摘要：command/record/policy/module ID、来源、阶段、索引/目录版本、候选数量、最终选择、耗时、迁移状态和提交结果。默认不反复输出完整 prompt、全部政策正文或大段 JSON。

19.5 诊断模式可记录候选 ID、分数、硬过滤原因、冲突/依赖裁决和 shadow diff，但必须有条数/字符/文件大小上限，且不得记录敏感配置或凭据。

19.6 必须可观察：两套索引状态、构建版本/耗时、缓存命中、embedding/rerank 次数、主线程等待、过期结果丢弃、运行时 snapshot 版本、到期桶长度、隔离记录数和 legacy production caller 数。

19.7 玩家可见失败要区分：历史索引未 ready、模块索引未 ready、LLM 评价失败、目标非法、模块零命中、提交前世界状态变化和存档隔离；零命中不是系统错误。

## 20. 尚未擅自决定、但有明确冻结时点的项目

20.1 `PermanentWeak` 与 `TimedStrong` 各指标的具体倍数、上下限和曲线：必须在 Phase 3 生产切换前用现有平衡数据冻结。

20.2 零模块政策的扣费、经验与玩家提示：必须在 Phase 6A 前冻结；无论如何政策仍可发布和入库。

20.3 NPC 各模块默认有限天数与强度：必须在 Phase 6B 前冻结，不能继续由 AI 自由给数值；这些默认值必须属于版本化目录/配置数据，不得散落成模块代码内隐常量。

20.4 关系抑制解除后的恢复方式、到期日处理和增强上限：必须在 Phase 6C 前冻结；一次性效果默认不得重放。

20.5 各检索组 Top-K、字符预算、最低分数、模块最大命中数和性能具体数值：由 Phase 0/2/5 评测确定并版本化，不在无数据时拍脑袋写死。

20.6 外部 DLL Provider 的 ABI、加载顺序、版本兼容和用户信任提示：必须在 Phase 7B 前冻结；不影响 Phase 7A 源码级扩展。

20.7 owner/target 恢复后的政策/效果行为：必须在 Phase 3 对应来源切换前冻结；不得通过默认改成 `Abolished` 回避决定。

20.8 这些开放项不阻塞 Phase 0/1 的只读与安全底座，也不允许在未到冻结点前由实现者暗中选择默认产品规则。

## 21. 最终完成标准

21.1 玩家王国、地方、附庸与 NPC 正式政策进入同一政策库；每个接入的生成/发布入口都使用同一 ONNX dense 检索契约读取相关历史。

21.2 已废除政策可在相关时被 AI 读取，使用独立低权重配额并清楚标明历史状态；它只进入历史上下文，不进入当前效果计划，也不会恢复效果。被拒绝、撤回和生成失败草案默认不进入正式检索语料。

21.3 政策与效果生命周期完全分离：玩家三来源按“空天数永久弱、正天数有限强”，NPC 按“政策永久、效果有限”；效果到期不续期、不废除政策。

21.4 动态 `PolicyObject` 在旧档和新档中都能在 non-ready 清理前恢复；Active 政策即使没有活动效果，读档后仍保留同 ID 原版对象和正确活动成员关系。

21.5 AI 不再输出生产可执行数值效果或模块 ID；本地 ONNX 语义选择支持零模块、多模块、冲突、依赖和稳定裁决，模块代码生成确定性计划。

21.6 玩家三来源共享同一模块核心，来源差异只在 adapter/target/commit 边缘；NPC 共享同一核心并保留其已确认期限意图和 target legality。

21.7 政策关系已持久化、可检索、可解释；启用机械影响的关系全部有确定性规则、全局边界和增量运行时处理，未定义关系不会擅自改数值。

21.8 零模块政策正常发布、入库、显示、保存和读档，不会被强塞模块，也不会被误判为失败、到期或废除。

21.9 所有模型 getter 使用预聚合不可变快照，不扫描/反序列化全 active effect；永久效果无每日 tick，有限效果按到期桶处理；主线程无 LLM/ONNX。

21.10 生产索引未 ready/failed 时明确等待或失败，不使用 sparse 或旧 AI 数值路径静默降级；policy/module 两套索引和缓存彼此隔离、版本化、可观测。

21.11 旧档加载、保存、再加载幂等；无静默丢政策、重复 `PolicyObject`、重复议程、重复效果、重复关系或重复一次性副作用。legacy 隔离记录已清零或有明确白名单处理。

21.12 `CustomPolicyBehavior.cs` 最终是薄主文件，能够直接看出三段流水线与主线程提交；拆出的文件拥有真实职责、调用者、测试和性能预算，而不是为了文件数量重排代码。

21.13 玩家新增源码级效果模块不需要修改中央执行 switch；若启用外部 DLL，加载、ABI、异常隔离和安全风险均已明确验证。

21.14 `BannerlordApi=1.3`、`BannerlordApi=1.4` 和 Bootstrap 使用仓库现有流程验证通过，单模块双实现输出未被擅自修改。

21.15 只有以上标准全部满足，才可以宣告“政策库和模块效果执行重做完成”；普通清理、文件行数下降或某一个来源先跑通都不能代替最终验收。

## 22. 独立效果模块化批次执行账本（不恢复大重构）

### 22.1 边界

2026-08-07 用户明确：本批不是恢复 Phase 1 及后续大重构，只重构玩家王国、地方、附庸国政策的效果实现层。本节记录该独立批次，不改变前文尚未退出的 Phase 0 状态，也不把本批结果解释为 Phase 1 开始。

本批不拆政策生命周期、动态 `PolicyObject`、议程、记录库或 active-effect 保存恢复主链；不实现氏族影响力转移等新效果语义；不触碰 `TroopInspectionBehavior.cs`、`analysis/`、`TrueAutoBlock/` 或用户删除的原版源码归档。

### 22.2 已改动

1. 新建生产源码目录 `PolicyEffects/`：
   - `PolicyEffectModuleContracts.cs`：现有扁平效果值槽与模块合同；模块必须分别提供 `RetrievalText`、极短 `CatalogSummary`、`MainInstruction`、`PostprocessRule`、数值标准化和写入实现；
   - `PolicyEffectModuleCatalog.cs`：首次使用时一次性反射发现源码模块，校验 ID、摘要长度/单行、合法作用域和重复项，并冻结稳定顺序、ID 字典、按 scope 模块数组及极短能力目录；
   - `PolicyEffectModuleRouter.cs`：一次 query embedding、一次 batch rerank 的政策专用 ONNX 选模；索引按 ID 冻结，查询直接读取 scope 模块数组；无无-ONNX降级；
   - `Modules/`：现有 9 个效果各自一个 `.cs`，每个文件拥有检索文本、极短目录摘要、主评议说明、后处理量纲规则和写入现有效果值槽的实现。
2. `CustomPolicyBehavior.cs` 的中央 metric 常量目录、支持白名单和 `ApplySparsePolicyMetric(...)` 分支已移除；稀疏 IR 编译按冻结模块字典解析。最终执行硬边界是“模块已注册 + 当前 scope 允许 + 数值有限 + target/metric 不重复”，ONNX 选中集只决定详细提示词注入，不再是执行授权白名单；合法的未召回模块会执行并记录 `outsideRecall=true`，未知或越界模块仍失败关闭。
3. 玩家王国、地方、附庸国政策保持三段：
   - 本地 ONNX 从当前政策名称与正文优先召回 4–6 个效果模块；相关性分数集中时扩到 6，差距明显时只保留 4；
   - 主请求的第一个 system message 是稳定 JSON 契约和当前 scope 的“模块 ID + 一句话用途”极短目录，第二个 system message 才放本次规则及召回模块 `MainInstruction`，完整世界、知识库、最近两条政策、句柄和当前政策全部后置；
   - 独立效果后处理同样先放稳定 JSON 契约和极短目录，只给召回模块注入完整 `PostprocessRule`；它只读取政策摘要、最多 800 字原文摘录、`impactSummary`、`numericIntent`、权威期限和紧凑合法句柄，不重复完整世界/知识/历史/民众反馈；
   - 主评议不再要求仅供第二段重复转述的 `effectIntensity`、`executionReach`、`durationLogic`、`confirmedTargetHandles`；兼容 DTO 字段保留但不作为新请求契约。
4. 主请求与效果后处理请求都使用 `maxAttempts=1`；空响应、截断、JSON 或语义校验失败即失败关闭，不重试、不猜数值、不扣费、不发布。
5. 后处理成功后把最终已编译 effects 回写现有 `PolicyMainAssessmentResult.Effects`。待议程、批准、续期仍从原字段重建效果，没有新增保存 key、没有改变现有 9 项扁平保存字段，也没有改动态 `PolicyObject` 读档恢复机制。
6. 后处理可从 `NormalizePolicyTargetHandles(request.TargetHandles)` 中选择本次 scope 的全部合法句柄，不再要求主评议重复确认；C# 仍按地方 S/L*/C*/R*、王国/附庸 K* 的既有 kind 规则拒绝虚构或越界目标。零效果政策的 K0（地方为 S）生命周期锚点仍只由编译器内部补入。
7. 后处理顶层 `durationDays` 在 JObject 层被覆盖为“玩家手动期限优先，否则主评议已验证正整数”的权威值；缺失、类型错误或数值不一致只记录纠正，不再让后处理反向改变期限。主评议本身缺失正期限仍失败关闭。
8. 旧版扁平 effect 只在目标唯一且字段一对一可映射时，先确定性转换为 `{targets,changes,reason}`，然后始终走 `allowAlreadyCompiled:false` 的稀疏编译器。稀疏/扁平混用、重复 construction 别名、未知字段、目标不唯一、非有限数、非法模块或 scope 仍拒绝；内部既有 already-compiled 路径不变。
9. 官方 `api.deepseek.com` 的玩家政策 profile 会添加 `response_format={"type":"json_object"}`；不启用 beta assistant prefix，不切换 beta URL，也不设置 `user_id`。其他 OpenAI-compatible/Anthropic 路径保持原载荷。MCM 新增“效果解析最大输出Tokens”（512–12000，默认 1000），后处理实际上限为它与当前玩家政策有效上限的较小值；主/后处理分阶段记录实际 prompt/completion/cache hit/cache miss，不记录 Key 或完整 prompt。

### 22.3 性能边界

1. 程序集类型扫描、模块实例化、按 scope 数组和极短目录拼接都只在目录首次使用时执行一次；每日 tick、模型 getter 和 active-effect 热路径没有反射、目录扫描或新增 JSON。
2. 模块文档 embedding 只在路由索引首次使用时构建一次并由 ID 字典持有；每次政策提交只生成一次 query embedding，scope 查询不再每次遍历 `AllowedScopes`。
3. scope 过滤后 dense recall 上限为 7；cross-encoder 只调用一次 batch rerank；主请求和后处理只读取召回 4–6 个模块的完整详细规则，其他 scope 合法模块仅占一行极短目录，不全读详细提示词。
4. ONNX 路由在既有后台政策生成任务内执行；主线程提交、每日维护和模型 getter 未增加 ONNX 工作。后处理兼容转换和 JSON 解析每次请求各执行一次，不在 tick/getter 路径。

### 22.4 已验证

2026-08-07 使用仓库现有 `一键编译覆盖推送/build_single_module.ps1 -Stage`，仅在当前构建进程通过 `DefaultItemExcludes` 排除用户的 `analysis/**` 与 `TrueAutoBlock/**`，未修改 `.csproj`、未部署游戏目录。第一次仍使用已经失效的旧 Steam 根目录 `D:\SteamLibrary\...`，在编译前因缺少 `TaleWorlds.CampaignSystem.dll` 失败；只读检查 Steam 清单后改用真实安装根目录 `D:\steam\steamapps\common\Mount & Blade II Bannerlord`，同一命令重新验证成功：

- Bannerlord 1.3：0 warning / 0 error；
- Bannerlord 1.4：0 warning / 0 error；
- Bootstrap：0 warning / 0 error；
- Stage：success，输出 `bin/Debug/single_module_stage/AnimusForge`。

### 22.5 未验证项与风险

1. 尚未实机调用真实 ONNX + 两次 API，未观察模糊政策（例如“大额投入推动王国发展”）的生产候选排名、主评议 JSON、后处理 JSON、DeepSeek JSON mode、缓存命中率和最终游戏效果。
2. 尚未实机覆盖玩家王国、地方、附庸国三种发布链，也未执行 1.3/1.4 旧档另存副本往返；因此不能把编译通过写成运行态或存档兼容已验证。
3. 当前源码模块化严格保持旧扁平保存结构。新增一个使用现有 9 个值槽的模块可以只增加源码文件并自动发现；全新的持久化效果形状仍需要后续增加通用 payload/runtime hook，不能声称复杂新效果已经做到单文件即插即用。
4. 当前只有召回 4–6 个模块会获得完整量纲规则；极短目录允许后处理在主意图明确时补用未召回合法模块，但这类 fallback 没有同等详细提示，质量仍需评测。上线前必须用独立效果模块评测集和实机模糊政策样本校准，不能靠增加重试兜底。
5. 本批没有改已有国家/氏族/定居点提及扫描；其重复全量扫描问题仍属于后续独立目标检索批次，不能混入本批效果模块实现。

### 22.6 下一入口

1. 先只做实机 smoke：玩家王国、地方、附庸国各一条，外加“大额投入推动王国发展”模糊样本；记录 ONNX 候选 ID/分数、主/后处理是否各只读候选模块、最终 effects 与失败关闭行为。
2. smoke 通过后，下一代码子批才设计“新效果通用 payload + 模块 runtime hook + 旧扁平字段兼容投影”，并以氏族影响力转移作为第一个复杂模块；该子批必须同时解决王国/附庸 K0 内 C*/R* 合法目标与确定性氏族选择，不能在当前 9 项扁平 DTO 上硬塞。
3. Phase 0B 的 1.3/1.4 旧档往返门槛仍按本大纲原账本执行；本批 `-Stage` 成功不替代该门槛，也不宣告 Phase 0 退出。
### 22.7 2026-08-07 本批追加

#### 22.7.1 改动

1. `PolicySystemLog.cs` 补上本批新增的生产审计节点白名单：`effect-module-selected`、`effect-module-outside-recall`、`policy-effect-duration-normalized`、`policy-effect-legacy-normalized`，使召回选模、未召回放行、权威期限纠正和旧扁平效果归一都有真实日志证据。
2. 本批没有改动存档 schema、动态 `PolicyObject` 恢复机制、最近两条政策读取逻辑、ONNX 路由前提或原有 9 项扁平保存字段。

#### 22.7.2 已验证

1. 2026-08-07 在仓库本地源码上复核：
   - official DeepSeek JSON mode 只在 `api.deepseek.com` host 下启用 `response_format={"type":"json_object"}`，没有加 `user_id`，也没有切换 beta prefix / beta URL；
   - 后处理 Token 实际上限仍为 `min(玩家政策当前生效上限, 效果解析最大Tokens)`；
   - 后处理可使用本次 scope 的全部合法句柄，但未知、越界、非有限数值、重复 target+module 仍会失败关闭；
   - 未召回但已注册且合法的模块已放行，并有 `outsideRecall=true` 日志；
   - 后处理 `durationDays` 以玩家手动期限或主评议权威值为准，不再反向影响政策期限；
   - 旧版扁平 effect 在目标唯一时会先转为稀疏 IR，再走稀疏编译器。
2. 2026-08-07 再次使用仓库既有 `一键编译覆盖推送\build_single_module.ps1 -Stage` 验证，只在当前构建进程设置 `DefaultItemExcludes` 排除 `analysis/**` 和 `TrueAutoBlock/**`，不修改 `.csproj`，不部署游戏目录。

#### 22.7.3 未验证

1. 尚未在 2026-08-07 当前这个批次再跑实机 ONNX + 真实 API smoke；缓存命中率、模糊政策质量、ONNX 候选分布和未召回模块补用质量仍属实机未验证项。
2. 尚未在 2026-08-07 本批进行 1.3/1.4 旧档另存副本往返；动态 `PolicyObject` 保留、local/vassal 历史、最近两条上下文逻辑本批只能算只读复核，不能写成运行态已验证。

#### 22.7.4 风险

1. `PolicySystemLog` 白名单日志机制仍是 allowlist；未来新增任何非 failure 型的路由/纠正/shadow 证据节点时，必须同步补入白名单，否则会被静默丢弃。

#### 22.7.5 下一入口

1. 按现有 22.6，先做实机 smoke 和独立效果模块评测集，不进入新效果语义。
2. 若 smoke 需要证据补充，优先看本批新增日志节点，而不是回到旧的 prompt 全量打印或重试策略。

### 22.8 2026-08-07 空 JSON 边界收尾

#### 22.8.1 改动

1. `CustomPolicyBehavior.TryParsePolicyPostprocessResult(...)` 在成功解析 `JObject` 后显式拒绝空对象 `{}`，错误为“后处理 JSON 对象为空”。空响应、空对象和截断 JSON 均保持失败关闭；仍然不重试、不扣除游戏内费用、不提交任何部分效果。
2. 该守卫不要求后处理重复决定期限：非空 JSON 中缺失、类型错误或不一致的 `durationDays` 仍会被覆盖为玩家手动期限或主评议权威期限。旧扁平 effect 的唯一映射转换、已注册且 scope 合法的未召回模块放行，以及未知模块、越界目标、非有限数和重复 target+module 拒绝规则均未改变。

#### 22.8.2 验证

1. 修改后再次使用唯一允许的 `一键编译覆盖推送/build_single_module.ps1 -Stage`，仅在当前构建进程用 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success。
2. Stage 输出仍为 `bin/Debug/single_module_stage/AnimusForge`，脚本明确报告 `no game directory was modified`；未修改 `.csproj`，未触碰 `TroopInspectionBehavior.cs`、`analysis/`、`TrueAutoBlock/`、用户删除的 RAR 或原始存档。

#### 22.8.3 未验证与下一入口

1. 当前桌面沙箱允许只读加载 Stage DLL，但阻止了进一步反射执行私有解析方法，因此没有把源码检查写成“程序集级断言已通过”。空 JSON、期限纠正、旧扁平转换和未召回模块仍需在实机 smoke 中观察真实日志与最终 effects。
2. 下一入口保持 22.6：只做玩家王国、地方、附庸国及模糊政策 smoke，并按只读原档、另存测试副本规则完成 1.3/1.4 往返；不进入影响力重分配等新效果语义。

### 22.9 2026-08-08 真实 DeepSeek API smoke

#### 22.9.1 输入与边界

1. 使用本机 MCM 已配置的玩家政策主 API：官方 `api.deepseek.com`、模型 `deepseek-v4-pro`；Key 只在进程内读取，未输出、未写入报告。旧配置尚无新 MCM 字段，因此效果后处理上限按代码默认 1000 Tokens；主评议保持现有 12000 Tokens 上限。
2. 样本为王国政策“我出一百万为了王国发展。”，合法句柄仅 K0。API-only smoke 固定注入 6 个合理候选模块的详细规则；本轮不把固定候选写成 ONNX 实际召回证据。
3. 主评议与效果后处理均只允许一次模型请求，不做内容或兼容重试；失败不进入游戏提交链，因此没有游戏内扣费或部分效果写入。

#### 22.9.2 实际结果

1. Python 3.14 `urllib` 在发出 HTTP 前发生 `SSL UNEXPECTED_EOF_WHILE_READING`；改用系统 `curl.exe` Schannel 做无鉴权探针，得到 HTTP 401，确认该传输路径可到达官方主机。
2. 随后的真实主评议请求成功返回：官方 `response_format={"type":"json_object"}` 被接受，响应 JSON 可解析，必需字段齐全，不含 `effects`、`changes`、`confirmedTargetHandles`、`effectIntensity`、`executionReach`、`durationLogic`，且 `durationDays` 为正整数。
3. 唯一一次效果后处理尝试在 HTTP 前发生 `curl 35: schannel failed to receive handshake`。按单次请求规则未重试，因此后处理 JSON、最终 effects、阶段 token/cache usage 和模糊政策最终效果质量仍未验证。
4. 脱敏证据写入 `Phase0_Local_Archive/reports/player_policy_api_smoke_20260808_transport_failure.json`；没有记录 Key 或完整提示词。主评议响应与 usage 在后续传输失败前未落盘，因此明确记为未知，不伪造指标。

#### 22.9.3 下一入口

1. 若要继续真实 API 验证，需要用户明确允许一个**新的后处理验证请求**；这会突破本次 smoke 已用完的单次后处理尝试，但仍不需要重跑主评议，可使用固定的脱敏主意图夹具单独验证后处理传输和 JSON。
2. 在获得该明确指令前，不再自动调用 API；生产代码、存档、游戏目录与既有 Stage 产物均保持不变。

### 22.10 2026-08-08 用户授权的独立后处理补测

1. 用户明确允许一个新的独立后处理请求。本轮没有重跑主评议，使用固定脱敏主意图、K0 句柄、现有极短目录和 6 个候选模块详细规则；`response_format=json_object`、1000 Tokens、单次请求、无重试。
2. 为减少协议协商变量，传输显式使用系统 `curl.exe` Schannel、HTTP/1.1 和 TLS 1.2+。该唯一请求仍在 HTTP 前失败：`curl 35: schannel failed to receive handshake`；因此没有模型响应、usage、cache 指标或 effects，也没有游戏内扣费和部分提交。
3. 只读脱敏检查确认当前 shell 的 `HTTP_PROXY`、`HTTPS_PROXY`、`ALL_PROXY` 均指向本机 `127.0.0.1:7890`，而 WinHTTP 为直连。结合相同主机上的主评议曾成功、失败发生在 HTTP 内容发送前，当前最强推断是本地代理隧道或其上游线路对新 TLS 连接不稳定，不是后处理提示词、JSON schema、模型名、Token 上限或 Key 校验导致。精确故障跳点尚未验证。
4. 脱敏证据写入 `Phase0_Local_Archive/reports/player_policy_postprocess_followup_20260808_transport_failure.json`。后续若要验证直连或调整代理，必须另行明确授权；本轮不绕过失败路径继续重试。
5. 传输差异必须保留：本 smoke 每个阶段启动独立 `curl.exe` 进程，因此后处理一定新建 TLS 隧道；生产 `PolicyLlmClient` 使用共享的 `DuelSettings.GlobalClient`，可能复用主评议已建立的连接。当前失败不能直接证明游戏内主评议成功后，后处理也会发生同样错误。

### 22.12 2026-08-08 用户再次授权的后处理调用

1. 用户再次要求 API 调用。本轮只发送一次效果后处理，不重跑主评议、不启用 curl 自动重试，仍使用官方 DeepSeek、`deepseek-v4-pro`、1000 Tokens、`response_format=json_object` 和相同脱敏政策夹具。
2. 与前两次 TLS 失败不同，本轮已成功到达 HTTP：响应外层 envelope 可解析且 assistant content 非空，但正文 JSON 解析失败，错误为 `Unterminated string starting at line 1 column 18`，属于未完成/截断 JSON；没有进入 effects 校验或游戏提交。
3. 复核发现 smoke 载荷与生产请求存在关键差异：生产 `PolicyLlmClient.BuildCompatibleChatRequestBody(...)` 对 DeepSeek 明确写入 `thinking={"type":"disabled"}`，本轮手工 smoke 未带该字段。因而不能把这次 1000 Tokens 截断直接归因于生产默认额度过低；模型可能在未禁用思考时消耗了输出预算。
4. 脱敏证据写入 `Phase0_Local_Archive/reports/player_policy_postprocess_retry2_20260808_malformed_json.json`。若继续，下一次必须先由用户明确授权，并严格补齐生产 `thinking.type=disabled` 后只调用后处理一次；不能用当前非等价载荷下调或上调生产 Token 默认值。

### 22.13 2026-08-08 提高效果后处理默认 Token 上限

1. 用户根据现有 MCM 各 API 通道的高 Token 配置，明确允许提高效果解析额度。`DefaultPlayerPolicyEffectPostprocessMaxTokens` 从 1000 调整为 12000，MCM 提示同步改为默认 12000；可选范围仍为 512–12000。
2. 后处理实际额度公式不变，仍为 `min(玩家政策当前有效上限, 效果解析上限)`。因此默认配置下主评议与效果后处理均可使用最高 12000，但不会突破玩家政策自己的有效上限；请求次数、无重试规则、提示词、保存格式和效果语义均未改变。
3. API 按实际生成 Token 计量，调高 `max_tokens` 只移除截断风险，不代表每次必然生成 12000 Tokens；真实 completion/cache usage 仍以阶段日志和 API usage 为准。
4. 修改后使用现有 `build_single_module.ps1 -Stage` 验证，且仅在当前构建进程排除 `analysis/**` 与 `TrueAutoBlock/**`：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success，未修改游戏目录。本次新 Stage 尚未覆盖到游戏模块。
5. 随后按生产等价载荷真实调用一次效果后处理：官方 DeepSeek `deepseek-v4-pro`、`thinking.type=disabled`、`response_format=json_object`、12000 Tokens、单次请求无重试。请求 3256.1 ms 完成，`finish_reason=stop`；prompt 594、completion 104、cache hit 512、cache miss 82。返回 durationDays=30，无需纠正；effects 只有 K0 的 `prosperityPerDay=1`，结构、scope、模块注册、有限数字和去重校验均通过。
6. 该结果证明提高上限不会迫使模型生成 12000 Tokens，并验证了 DeepSeek 缓存与完整 JSON 输出；但也暴露质量风险：固定主意图明确提及建设、粮食、忠诚和治安，后处理仍只选择繁荣模块。当前问题已从“Token 截断”转为“效果覆盖偏保守”，不能再靠继续提高 Token 解决；若要调整，应单独审议后处理规则是否要求逐项覆盖 numericIntent 中的明确可映射变化。
7. 脱敏成功报告为 `Phase0_Local_Archive/reports/player_policy_postprocess_production_equivalent_20260808.json`。本次新默认值尚未再次部署到游戏目录。

### 22.11 2026-08-08 用户明确授权同步游戏目录

1. 用户在先前“只 Stage、不部署”的边界之后明确要求同步游戏目录，因此本轮按仓库既有统一模块流程执行 `一键编译覆盖推送/build_single_module.ps1 -Deploy`；仅在该构建进程用 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`，没有修改构建脚本或 `.csproj`。
2. 部署前确认 Bannerlord 未运行。脚本重新构建 Bannerlord 1.3、Bannerlord 1.4 与 Bootstrap，三者均为 0 warning / 0 error；随后事务式替换 `D:\steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`，部署结果 success。既有 Logs 被保留，PlayerExports 按现有非删除合并规则处理；没有覆盖 TaleWorlds 原版 DLL，也没有删除历史双模块目录。
3. 部署后 SHA-256 逐项核验通过：游戏目录中的 Bootstrap、1.3 实现和 1.4 实现分别与本次 `single_module_artifacts` 完全一致。`SubModule.xml` 的 Id/Name 均为 `AnimusForge`，且只声明 `AnimusForge.Bootstrap.dll`；事务 staging/backup 临时目录残留数为 0。
4. 本轮只证明构建、事务部署与文件身份正确；尚未启动游戏，因此 Bootstrap 运行时版本选择、真实政策 API 后处理、ONNX 召回和旧档往返仍未实机验证。

### 22.14 2026-08-08 按政策主链路实际额度回调效果解析默认值

1. 复核源码与本机 MCM 配置：政策自定义额度的源码默认值为 12000，但本机 `PlayerPolicyFollowSelectedApiTokens=true`，且所选主 API 的 `MainApiMaxTokens=5000`，因此玩家政策主链路当前实际生效额度为 5000，而不是未启用的自定义 12000。
2. `DefaultPlayerPolicyEffectPostprocessMaxTokens` 从上一轮尚未部署的 12000 回调为 5000，MCM 提示同步为默认 5000；可调范围仍保留 512–12000，实际请求公式仍为 `min(玩家政策当前有效上限, 效果解析上限)`。本轮不改变请求次数、无重试规则、提示词、效果语义或保存格式。
3. 使用仓库现有 `一键编译覆盖推送/build_single_module.ps1 -Stage` 验证，仅在当前构建进程通过 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success；未部署游戏目录。
4. 当前本地源码与 Stage 产物默认值为 5000。上一轮 12000 默认值从未部署；游戏目录仍保持此前已部署版本，本轮未覆盖。真实游戏内 MCM 迁移与实际 API 输出质量仍未重新验证。

### 22.15 2026-08-08 效果 AI 漏选诊断 A/B

1. 为判断上一轮只返回 `prosperityPerDay` 是模块/解析器漏项，还是测试夹具问题，新增本地只读 API 评测脚本 `Phase0_Local_Archive/baseline/run_effect_api_ab.ps1`。脚本从既有 MCM 配置进程内读取凭据，不记录 Key；使用官方 DeepSeek、`deepseek-v4-pro`、`thinking.type=disabled`、`response_format=json_object`、temperature 0.25、5000 Tokens，每个案例只请求一次且无重试。
2. 两个案例保持相同的多效果 `numericIntent` 和相同 6 个详细模块规则，只改变原文：A 为“我出一百万为了王国发展”，B 明确写出公共工程、粮仓、忠诚、治安和繁荣。两次均 HTTP 200、`finish_reason=stop`、JSON 可解析：
   - A：prompt 859、completion 88、2313.1 ms，输出 `prosperityPerDay`、`constructionPerDay`、`foodPerDay`、`loyaltyPerDay`、`securityPerDay`；
   - B：prompt 894、completion 139、2611.8 ms，输出相同 5 个模块。
3. C# 稀疏编译路径会逐项遍历 `changes`，只校验注册、scope、有限数字和重复 target+module，不会按 ONNX 召回白名单删除模块；本次返回的 K0 与 5 个模块均满足现有执行边界。因此当前证据排除“模块加载器或 C# 解析器静默丢掉四项”。
4. 复核上一轮报告 `player_policy_postprocess_production_equivalent_20260808.json`，其中政策名称和正文分别是字面量 6 个与 12 个 `?`，且报告没有保存当时的 `numericIntent`。该手工 smoke 存在中文代码页损坏，不能作为“正确载荷下效果 AI 只选繁荣”的证据；此前据此记录的效果覆盖偏保守结论由本节纠正。
5. 本次脱敏证据写入 `Phase0_Local_Archive/reports/player_policy_effect_ai_vague_vs_explicit_5000_20260808.json`。没有修改生产提示词、政策业务行为、保存格式或游戏目录；5000 Tokens 对这两个多效果响应均有充分余量。
6. 未验证项：本次固定了多效果主评议意图与候选规则，没有在游戏内执行真实 ONNX 召回和主评议请求，因此仍不能替代完整实机三段式 smoke。若实机再次漏项，应首先记录正确 UTF-8 的 ONNX 选择、主评议 `numericIntent` 和后处理模块 ID，再判断是主评议意图不足还是后处理漏映射，不应仅凭最终 effects 推断。

### 22.16 2026-08-08 地方、跨国与附庸效果 AI 方向矩阵

1. 新增本地 API 评测脚本 `Phase0_Local_Archive/baseline/run_effect_api_scope_matrix.ps1`，按现有作用域约束构造 8 个独立后处理案例。脚本只从 MCM 配置进程内读取凭据，不记录 Key；使用官方 DeepSeek、`deepseek-v4-pro`、`thinking.type=disabled`、`response_format=json_object`、temperature 0.25、5000 Tokens。每个案例只请求一次，总计 8 次，无重试。
2. 句柄矩阵严格按生产规则：地方政策只使用 S/L*/C*/R*，王国与附庸政策只使用 K*；所有案例同时提供合法干扰句柄，并断言未被错误选中。结果 8/8 HTTP 200、`finish_reason=stop`、JSON 可解析、权威期限一致、必需 target+module 与正负方向全部命中，且未知句柄、未知模块、NaN/Infinity、重复 target+module、地方稳定度和禁止目标命中均为 0。
3. 地方案例：
   - 跨发布地影响另一城镇：只对 L0 输出 `foodPerDay=+1`、`constructionPerDay=+1`，未误伤 S/C0；
   - 指定家族当前全部领地：只对 C0 输出 `taxIncomePct=-5`、`loyaltyPerDay=+1`，未误伤 S/L0；
   - 发布地重税巡防：只对 S 输出 `taxIncomePct=+5`、`militiaPerDay=+2`、`securityPerDay=+1`、`loyaltyPerDay=-1`，未误伤 C0/L0。
4. 王国与附庸案例：
   - 外国援助：只对 K1 输出粮食与建造力各 +1，未误伤玩家国 K0 或干扰国 K2；
   - 外国制裁：只对 K1 输出税收 -5、粮食 -3、繁荣 -2；
   - 双边共同市场：K0/K1 均输出繁荣 +1、税收 +5，未误伤 K2；
   - 外国向玩家国转移税收：K1 `taxIncomePct=-15`、K0 `taxIncomePct=+15`；
   - 附庸向宗主转移税收：附庸 K0 -10、宗主 K1 +10，未误伤 K2。
5. 总 usage：prompt 6848、completion 696、cache hit 1024、cache miss 5824；单例最大 completion 133，说明当前 5000 上限对这些多目标、多方向稀疏 JSON 有充分余量。脱敏完整结果写入 `Phase0_Local_Archive/reports/player_policy_effect_scope_matrix_5000_20260808.json`。
6. 本批只增加归档评测脚本和报告，没有修改生产提示词、业务效果、保存格式或游戏目录，因此未重复执行 Stage。未验证项仍是游戏内真实 ONNX 召回与主评议生成；本矩阵固定了合理候选规则和 `numericIntent`，证明的是效果 AI 对合法句柄、跨目标方向和多模块 JSON 的选择能力及现有 C# 接受边界，不替代完整三段式实机结论。

### 22.17 2026-08-08 效果数值质量提示词审计

1. 复核确认 22.15/22.16 的 API 夹具复用了当前生产 `PlayerPolicyEffectStableSystemPrefix`、作用域规则、极短能力目录、召回模块 `PostprocessRule`、硬规则、temperature 0.25、`thinking.type=disabled` 和 JSON mode；固定的是合成 `numericIntent` 与候选模块，而不是另造一套数值提示词。
2. 当前生产主评议仍完整读取 MCM 玩家政策评判提示词，其中保留模块化前的数值尺度、资源投入与强度规则；但主评议新契约禁止输出具体数值，只把方向和定性强弱写入 `numericIntent`。最终效果后处理不读取该完整评判提示词，而各模块当前 `PostprocessRule` 只保留单位和正负方向，没有迁移原始数值档位。
3. 因此模块化拆分存在明确的信息断层：主评议读到了建设 ±20～60、持续工程 +60～150、全国重大建设 +300～1000，以及全国重大政策繁荣/粮食/民兵约 ±10～40、忠诚/治安约 ±2～6、税收约 ±20%～60%、稳定度一次性约 ±8～22 等原规则，却不能把具体尺度传给后处理；后处理最终只能自行保守猜测，产生建设 +1、粮食 +1、税收 ±5 等偏弱结果。
4. 22.16 的 8/8 通过只证明目标句柄、模块方向、跨国/附庸双向表达和 JSON 合法性正确，不能解释为数值质量与模块化前持平。当前结果不是 Token 上限问题，也不是 C# 归一化压低，而是召回模块后处理规则过短。
5. 本节只记录审计结论，没有修改生产代码、提示词或游戏目录。下一精确入口应是把模块化前每项指标的因果边界、量纲和数值档位迁入对应模块 `PostprocessRule`，并只为 ONNX 召回模块注入；同时保留一段只出现一次的投入/覆盖/持续时间通用强度规则。随后用同一 scope matrix 增加数值档位断言再测，不能仅断言正负方向。
6. 额外风险：玩家在 MCM 自定义评判提示词中写入的数值偏好目前只到达主评议，不会原样到达最终效果后处理。后续修复必须明确保留这条用户定制通道，不能只修内置默认尺度后宣称完全兼容。

### 22.18 2026-08-08 恢复模块数值尺度与提示词分流

#### 22.18.1 改动

1. 将模块化前的因果边界、量纲和数值档位迁入 `PolicyEffects/Modules/` 的 9 个 `PostprocessRule`：繁荣、粮食、户数、忠诚、治安、民兵、税收、固定点数建造力和一次性王国稳定度各自由本模块持有。`kingdomStabilityOnce` 额外明确最终值必须为整数，并在模块 `TryNormalizeValue(...)` 中按既有 `AwayFromZero` 规则归一；扁平字段写入和最终运行时语义未改。
2. `CustomPolicyBehavior.PlayerPolicyEffectCommonCalibration` 只补一段跨模块共用的强度原则：每日值不得按持续天数机械摊薄，金额/倍率/档位/范围/强弱只用于定标而不线性换算，同一直接执行方案可以产生多项效果，巨额资金本身已是代价，不得默认把所有变化压成 1 或 5。它与极短能力目录一起进入稳定前缀；完整模块规则仍只由本次 ONNX 召回列表注入。
3. 主评议的 `numericIntent` 契约不再笼统禁止记录具体约束：仍禁止模块 ID 和最终执行字段，但必须紧凑保留政策正文或玩家 MCM 自定义提示中的金额、倍率、范围、档位、强弱与参考尺度，供后处理按召回模块定标。
4. `DuelSettings.DefaultCustomPolicyEvaluatorPrompt` 改为模块化主评议默认文本，只负责卡拉迪亚可执行性、成本、期限、民众反馈、目标/方向/强弱意图及附庸独立度，不再常驻九类效果的完整数值细节。旧内置默认全文保留为 `PreviousDefaultCustomPolicyEvaluatorPromptBeforeEffectModuleRouting` 并加入 `IsBuiltInCustomPolicyEvaluatorPromptText(...)` 迁移识别；带旧政治权重泄漏后缀的内置文本也会先清理再识别。因此旧默认文件会切换到新模块化默认，玩家真正自定义的 MCM 全文仍原样保留，不被拆除或覆盖。MCM 按钮提示和编辑器说明同步解释该分流。
5. 效果后处理固定示例删除 `prosperityPerDay=1` 和两处 `effects=[]` 空数组锚点，改成不含可复制数值、空数组或伪 JSON 裸占位符的字段类型结构；`effects=[]` 只在硬规则中作为“确实没有机械数值效果”的合法语义出现，避免 DeepSeek 机械复制空结果、个位数示例或无效 JSON。
6. 两个脱敏 API 评测脚本同步生产模块原文、通用定标和字段类型结构；scope matrix 另增加稳定度非整数断言。scope matrix 后续输出固定写入 `player_policy_effect_scope_matrix_calibrated_5000_20260808.json`，A/B 输出固定写入 `player_policy_effect_ai_vague_vs_explicit_calibrated_5000_20260808.json`，避免覆盖未校准报告。

#### 22.18.2 真实 API 验证

1. 数值方向矩阵按 9 个计划案例各发起一次、无重试。`local-other-settlement` 在到达 HTTP/模型结果前发生 `The SSL connection could not be established`，按规则没有重发；其余 8 个案例均 JSON 可解析，且目标、方向、最低绝对值、scope、模块注册、有限数值和去重断言全部通过：
   - 地方家族领地减负：C0 税收 -20、忠诚 +1.5；
   - 发布地重税巡防：S 税收 +25、民兵 +3、治安 +0.5、忠诚 -0.8；
   - 援助外国：K1 粮食 +8、建造力 +150；
   - 制裁外国：K1 税收 -20、粮食 -6、繁荣 -5；
   - 双边共同市场：K0/K1 各繁荣 +3、税收 +15；
   - 外国向玩家国转移税收：K1 -15、K0 +15；
   - 百万全国发展：K0 建造力 +800、粮食 +30、繁荣 +30；
   - 附庸向宗主转移税收：附庸 K0 -10、宗主 K1 +10。
2. 上述矩阵成功请求总 usage 为 prompt 11018、completion 762、cache hit 2048、cache miss 8970。脱敏证据位于 `Phase0_Local_Archive/reports/player_policy_effect_scope_matrix_calibrated_5000_20260808.json`。本次校准执行曾覆盖通用旧路径 `player_policy_effect_scope_matrix_5000_20260808.json`；22.16 的原始 8 案例结果仍以本账本记录为准，不伪造或反向改写旧报告。
3. 删除空数组/数值锚点、改用最终字段类型结构并让 A/B 精确复制生产模块原文后，再执行模糊/明确政策 A/B，各案例一次、无重试，2/2 HTTP 200、`finish_reason=stop`、JSON 可解析，均输出 5 个直接模块：K0 繁荣 +15/日、建造力 +500/日、粮食 +12/日、忠诚 +3/日、治安 +1/日。两次总 usage 为 prompt 3147、completion 279、cache hit 0、cache miss 3147。脱敏证据位于 `Phase0_Local_Archive/reports/player_policy_effect_ai_vague_vs_explicit_calibrated_5000_20260808.json`。
4. A/B 与矩阵都固定了合理候选模块和合成 `numericIntent`；它们验证的是后处理提示词的模块覆盖、数值尺度、目标方向与 JSON 结构，不是假称已经验证游戏内真实 ONNX 排名或主评议生成质量。

#### 22.18.3 编译与静态验证

1. PowerShell 7 对 `run_effect_api_scope_matrix.ps1 -SyntaxOnly` 与 `run_effect_api_ab.ps1 -SyntaxOnly` 均输出 `SYNTAX_OK`；`git diff --check` 无空白错误，仅报告既有 LF/CRLF 转换警告。
2. 最终源码使用仓库唯一允许的 `一键编译覆盖推送/build_single_module.ps1 -Stage` 验证，仅在当前构建进程通过 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success，脚本明确报告 `no game directory was modified`。
3. 第一次构建启动尝试在构建前因 PowerShell 编码助手无法编码中文脚本路径而报 `UnicodeEncodeError: surrogates not allowed`；随后直接使用 PowerShell 7 执行同一仓库脚本和相同参数成功。该失败不是源码编译信号，也没有修改构建脚本。

#### 22.18.4 未验证项与风险

1. 尚未在游戏内串行验证“真实 ONNX 召回 → 新默认主评议 → 效果后处理 → C# 提交”；因此模型实际候选排名、玩家自定义 MCM 文本在生产主请求中的行为、缓存命中率、最终扣费与运行时结算仍不能由 API 夹具或 Stage 代替。
2. 校准后的 `local-other-settlement` 案例因 TLS 在模型前失败且未重试，尚无本轮新提示下的 API 结果；其目标方向只在 22.16 旧短规则矩阵中验证过。
3. 已冻结规则仍允许后处理输出未召回但已注册且 scope 合法的模块，并记录 `outsideRecall=true`。这类模块只读到极短目录而没有完整数值档位，质量低于召回模块；本批不擅自改成执行硬白名单，也不增加第二次 LLM。后续若实机频繁出现，应优先改进本地 ONNX 复选/召回覆盖，而不是静默拒绝或重试。
4. 默认内置提示词已完成模块分流；玩家真正自定义的 MCM 文本必须按兼容要求完整保留，所以若玩家自定义全文自行包含九类详细规则，主评议仍会读取这些用户内容。这是保留用户定制而不是默认提示回退。
5. 本批没有部署游戏目录，没有修改保存格式、动态 `PolicyObject` 读档恢复、最近两条玩家/附庸政策读取，也没有触碰 `TroopInspectionBehavior.cs`、`analysis/`、`TrueAutoBlock/`、用户 RAR 状态或原始存档。

#### 22.18.5 下一入口

1. 下一步只做游戏内三段式 smoke：玩家王国、地方跨发布地、地方家族领地、跨国、附庸各一条，记录真实 ONNX 候选、主评议 `numericIntent`、后处理 effects、`outsideRecall`、usage/cache 和最终结算；仍不进入影响力重分配等新语义。
2. 实机 smoke 通过后再决定是否同步本次 Stage 到游戏目录；在用户明确要求部署前保持本地 Stage，不覆盖当前游戏模块。

### 22.19 2026-08-08 国家、家族、定居点 ONNX 语义候选影子链路

#### 22.19.1 已实施边界

1. 新增 `PolicyTargets/PolicyTargetSemanticRouter.cs`。它建立国家、家族、领袖、定居点的稳定语义文档，以及敌国/盟国/国内外、方位、类型、边境、财富/影响力/实力/封地数、粮食/繁荣/忠诚/治安/户数、最近/最远等固定 Facet；ONNX 只返回候选，不能生成自由句柄。
2. 现有确定性目标扫描未删除、未降权：地方政策仍先产生 S 与明确名称命中的 C/R/L；王国/附庸政策仍先产生 K0/K1 及明确外国 K。外国氏族、领袖、定居点的语义命中仍只折叠为所属外国王国 K，不生成外国 C/R/L；附庸政策其他外国仍为 K2+。
3. `PolicyEffectModuleRouter` 已拆分为共享 query embedding、效果模块 dense recall 和按外部 rerank 分数复选。每次玩家政策现在只生成一次 query embedding；效果模块 top 7、实体 top 10、Facet top 8 合并为最多 25 对，并只调用一次 `TryScoreBatch(...)`。效果、实体、Facet 的名额分别计算；效果模块原有 4～6 个选择规则未改。
4. 快照只在政策提交主线程按需构建：DailyTick、宣战/议和、联盟变化只做 O(1) 版本标脏；氏族换国、领地易主、领袖变化、氏族/王国销毁标记结构变化。稳定索引只保存 `DocumentId + Vector`，动态快照刷新时重新绑定当前实体对象，因此财富、实力、外交、粮食等可更新而不会重做稳定文档 embedding；结构版本变化才重建稳定向量。
5. 方位使用地方发布地质心或目标王国定居点质心，Y 正为北、X 正为东；边境要塞以 `GetNeighborFortifications(MobileParty.NavigationType.All)` 是否存在不同 `MapFaction` 为准，村庄继承绑定要塞。动态排序并列按名称、StringId 稳定处理，组合 Facet 先求硬条件交集，不在空结果时放宽作用域。
6. 语义句柄合并、短证据注入和二次 scope 校验代码已经接入，但 `SemanticExpansionEnabled` 明确保持 `false`：当前只写 `target-semantic-shadow`，不把语义结果加入 `TargetHandles`，所以本批没有增加主评议/后处理 Token，也没有改变任何机械目标或业务效果。地方最多 4 个且同 kind 最多 3 个、王国/附庸最多 3 个的上限已固化在待启用路径。
7. 请求快照、语义标记和证据全部 `[JsonIgnore]`；未修改保存键、动态 `PolicyObject` 恢复、政策期限、议程、费用、两次请求/无重试规则、最近两条政策读取或九个效果模块。后处理仍只能选择注册句柄，C# 对语义句柄额外检查快照实体、kind、所属王国和地方发布地排除条件。

#### 22.19.2 验证结果

1. `git diff --check` 无空白错误；仅有仓库既有 LF/CRLF 转换警告。
2. 最终源码使用唯一允许的 `一键编译覆盖推送/build_single_module.ps1 -Stage` 验证，仅在当前构建进程通过 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success；脚本明确报告 `no game directory was modified`。
3. 新增 TaleWorlds 成员在双版本均编译通过，包括政策目标失效事件、联盟事件、`Kingdom.Settlements`、`Clan` 动态指标、`Settlement.GetPosition2D`、`Town.GetNeighborFortifications(...)` 和 `Kingdom.IsAllyWith(...)`。
4. 本批没有调用 DeepSeek 或其他外部 API，没有部署，也没有触碰 `TroopInspectionBehavior.cs`、`analysis/`、`TrueAutoBlock/`、用户 RAR 状态或其他用户修改。

#### 22.19.3 尚未通过的启用门槛

1. 尚未在真实战局用随模组发布的 ONNX 对“北方敌国”“最富有家族”“边境粮仓城市”“附庸以东最弱敌国”、错别字/简称及负例采集 reranker 正负分布，因此不能计算每类 `minPositive`、`maxNegative` 和中点阈值，也不能宣称语义目标召回已机械启用。
2. 下一步必须在游戏内读取 `target-semantic-shadow`：只有对应类别所有正例预期目标均为第一名、负例无越权候选、且 `minPositive - maxNegative >= 0.08`，才为该类别写入校准阈值并启用；不满足的类别继续影子运行，不得用 0.5 等未经校准的固定概率阈值、扩大候选或放松 scope 绕过。
3. 仍需实机确认首次稳定索引构建耗时、第二次同结构版本零文档重嵌入、每日动态版本只刷新快照、每次请求 `combinedPairs <= 25`，以及真实“目标 ONNX + 效果 ONNX → 主评议 → 后处理 → C# 提交”的完整日志与结算。当前 Stage 只证明编译和统一模块组装，不替代这些运行验证。

### 22.20 2026-08-08 目标语义校准、受限启用与 API 句柄测试

#### 22.20.1 本地 ONNX 校准

1. 新增固定样例 `Phase0_Local_Archive/baseline/cases/policy_target_semantic_calibration.jsonl` 和执行器 `run_policy_target_semantic_calibration.ps1`。执行器从本轮 Stage 的 1.4 实现加载实际 `OnnxEmbeddingEngine`、`OnnxCrossEncoderReranker` 与生产 `Facets`，不调用外部 API；报告固定写入 `Phase0_Local_Archive/reports/policy_target_semantic_calibration_20260808.json`。
2. 本轮模型哈希：embedding `69b353bb2aa2d09ab606ddbbc35437b03c843615a6bff28216a37fee7309c2aa`，reranker `15b9a8c3da82eddf263df571281166e00e9308fe19d077084b642ebfcaf06d2b`。生产 Facet dense recall 改为每组最多 2 个、总计最多 8 个，避免大量 metric Facet 挤掉 relation/direction/type；实体仍为 top 10，合并生产 batch 仍不超过 25 对。
3. CrossEncoder 返回值是 sigmoid 后的排序分数而非校准概率。首轮若直接比较 sigmoid 数值差，实体正负样例虽稳定 top-1，却会因数值都接近零而错误失败；最终门槛改为先计算 `logit(score)`，再要求 `minPositiveLogit - maxNegativeLogit >= 0.08`，运行阈值取两者 raw-logit 中点并转换回 sigmoid 分数。校准额外对全部固定 Facet 各打一次分以测量未进入生产 top-8 的真实负例；这不改变生产每次提交最多一次 rerank batch 的约束。
4. 最终 8/8 样例通过：北方敌国、国内最富家族、边境粮仓城市、附庸以东最弱敌国、王国错别字、定居点错别字、家族简称和泛化国内负例均满足预期召回/实体 top-1/无越权目标。实体阈值为 `0.000850`，raw-logit 正负间隔 `0.946670`。
5. 已通过且写入生产白名单的 Facet 阈值如下：`direction_east=0.004697`、`direction_north=0.003827`、`geography_border=0.008407`、`metric_food_high=0.022688`、`metric_strength_low=0.061066`、`metric_wealth_high=0.106118`、`relation_domestic=0.019900`、`relation_enemy=0.011879`、`type_city=0.004513`、`type_clan=0.001939`。对应 raw-logit 间隔依次为 `4.947982`、`6.830704`、`4.787280`、`3.898763`、`7.419089`、`5.010900`、`0.458998`、`2.300079`、`3.254040`、`3.988695`，均高于 `0.08`。
6. `type_kingdom` 等没有满足或没有获得本轮正负标注的类别没有写入生产阈值，继续只出现在影子日志。非地方政策的 Facet 物化本来最终只能生成 K，因此 C# 直接只在王国快照间比较；外国氏族/领袖/定居点只有直接实体语义命中时才折叠为所属 K，仍不生成外国 C/R/L。为阻止“发展本国经济”等泛化文本物化任意实体，Facet 生成候选还必须同时命中至少两个不同语义组；直接实体命中独立使用实体阈值。

#### 22.20.2 启用边界

1. `PolicyTargetSemanticRouter.SemanticExpansionEnabled` 已改为 `true`，启用原因记录为 `calibrated-20260808`。确定性名称/ID 命中仍先建立且不受语义上限挤压；语义候选仍受地方最多 4 个、每 kind 最多 3 个、王国/附庸按所属王国去重最多 3 个约束。
2. 只有阈值白名单中的 Facet 或超过实体阈值的直接实体才可进入 `EnabledProposals`；未校准类别仍保留在 `target-semantic-shadow`。主评议/后处理仍只能从注册句柄选择，合并前和应用前仍由 C# 复核 kind、scope、当前所属王国与发布地排除规则。
3. 没有修改存档格式、动态 `PolicyObject` 恢复、期限、议程、费用、两次请求/无重试规则、最近两条政策读取或九个效果模块；没有新增影响力重分配或其他业务效果。

#### 22.20.3 一次 API 合法句柄选择测试

1. 新增 `Phase0_Local_Archive/baseline/run_policy_target_handle_api_test.ps1`。它从既有 MCM 配置在进程内读取凭据，将 5 个独立案例合并为恰好一次请求，设置 `thinking.type=disabled`、`response_format=json_object`、`temperature=0.25`、`max_tokens=2000`，不重试，不保存完整 prompt、响应正文或 API key。
2. 实际调用 `api.deepseek.com` / `deepseek-v4-pro`：HTTP 200，`finish_reason=stop`，耗时 `2930.8 ms`，usage 为 prompt 618、completion 180、cache hit 0、cache miss 618。北方敌国选 K2、国内最富家族选 C1、边境粮仓城市选 L2、附庸以东最弱敌国选 K3；仅联络和报告的负例返回空目标。5/5 均通过合法句柄、无编造、无跨案例和精确预期检查。脱敏报告位于 `Phase0_Local_Archive/reports/policy_target_handle_selection_api_20260808.json`。
3. 该测试只证明在给定紧凑合法句柄时的 LLM 目标选择，不冒充游戏内完整“真实世界快照 → ONNX → 主评议 → 后处理 → C# 提交/结算”验证。

#### 22.20.4 编译、静态检查与剩余风险

1. 启用后的源码使用唯一允许的 `一键编译覆盖推送/build_single_module.ps1 -Stage` 验证；仅在当前构建进程通过 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`。Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success，脚本明确报告没有修改游戏目录。
2. `run_policy_target_semantic_calibration.ps1 -SyntaxOnly` 与 `run_policy_target_handle_api_test.ps1 -SyntaxOnly` 均输出 `SYNTAX_OK`。最终 `git diff --check` 只允许仓库既有 LF/CRLF 转换警告，不允许空白错误。
3. 本轮仍未部署游戏目录，也未在真实存档中验证结构版本复用、外交/经济 dirty 只刷新动态快照、实际 `combinedPairs <= 25` 日志、主评议增加的实际 token 数、后处理选择以及最终机械结算。上述项目必须在用户后续明确允许部署并进行游戏内 smoke 后再声明完成。
4. 未触碰或回滚 `TroopInspectionBehavior.cs`、`analysis/`、`TrueAutoBlock/`、用户 RAR 状态及其他无关用户修改。

### 22.21 2026-08-08 家族、领袖与定居点真实语料精度收紧

#### 22.21.1 真实语料审计与生产修正

1. `run_policy_target_semantic_calibration.ps1 -EntityPrecision` 现在直接读取本机 Bannerlord `Modules/SandBox/ModuleData` 的 `spclans.xml`、`lords.xml`、`settlements.xml` 及简体中文本地化；村庄没有独立 `owner` 时，按 `Components.Village.bound` 继承绑定城市/城堡的当前氏族。测试语料不再使用虚构名称。
2. 首轮真实语料暴露两类不能只靠 reranker 排名解决的歧义：家族文档包含领袖名、定居点文档包含所属氏族名，导致跨 kind 污染；城堡与绑定村庄又常共享词根，例如“乌斯托科堡/乌斯托科”。因此直接实体启用同时要求：分 kind 校准阈值、原文名称/别名一编辑距离内命中、家族与领袖按同一机械氏族去重、同一竞争类别第一与第二名的 raw-logit 间隔至少 `0.08`。
3. 定居点再增加 C# 类型硬消歧：原文明确含“村/村庄”只允许村庄，含“堡/城堡/要塞/关隘”只允许城堡，含“城市/城镇/城区”只允许城市；“城墙/城防/驻防/守军/戍守”至少排除村庄。该规则只过滤已经注册的本国定居点，不改变外国实体折叠为 K 的产品语义。
4. 为解决一个真实村庄错字在纯 dense top 10 外的问题，一编辑距离名称命中可在 rerank 前强制占用实体 top 10 内最多 4 个位置，而不是额外增加候选；随后仍只有一次统一 batch。实体上限仍为 10，Facet 仍为 8，效果模块仍为 7，`combinedPairs` 上限仍为 25。全量实体检查只发生在政策提交期，规范化 query 只计算一次；DailyTick/getter 没有新增扫描、分配或 ONNX。
5. Facet 动态排名也收紧：包含财富/粮食/繁荣/忠诚/治安/实力等 metric 或最近/最远 distance 的组合只物化稳定排序第一名，避免“最富家族”“边境粮仓城市”同时给 LLM 3 个近似候选；无 metric/distance 的组合仍最多 3 个并继续受地方总 4、同 kind 3、王国去重 3 的外层上限约束。
6. 生产主评议仅在本次确有语义句柄时增加一小段规则：语义依据已经本地 ONNX 召回和 C# scope 校验，简称/轻微错字可按证据理解，但候选不代表必须施效；征税、免税、承担军费属于机械数值变化，联系人/报告/背景对象仍不是效果目标。保存 DTO 的 `IsSemanticTarget`、`SemanticEvidence` 继续为 `[JsonIgnore]`。

#### 22.21.2 实际 ONNX 精度矩阵

1. 最终报告 `Phase0_Local_Archive/reports/policy_target_entity_precision_calibration_20260808.json` 使用与 22.20 相同的真实模型哈希，覆盖 8 个王国/文化组共 48 个模糊名称正例：家族 16、领袖 8、定居点 24（每组各一城市、城堡、村庄），另对每组执行 4 条无名负例，共 32 次。
2. 最终结果为家族 `16/16`、领袖 `8/8`、定居点 `24/24`；无名负例的直接实体激活数全部为 0。家族、领袖、定居点的配置阈值分别为 `0.000100`、`0.000200`、`0.000050`，最小第一/第二 raw-logit 间隔分别为 `2.235983`、单候选、`1.776804`，均高于 `0.08`。
3. 纯 dense top 10 召回为家族 `16/16`、领袖 `8/8`、定居点 `23/24`；唯一遗漏是“于桑克”村庄错写为“于克村”。有界名称强制保留后为 `24/24`，没有扩大 top 10 或 rerank pair 数。城堡/绑定村庄和短城市名碰撞均由类型硬规则正确分开。
4. 固定 Facet 回归报告 `policy_target_semantic_calibration_20260808.json` 再次为 `8/8 passed`，证明北方敌国、最富家族、边境粮仓城市、附庸以东最弱敌国及泛化负例的既有阈值没有被本轮实体精度修正破坏。

#### 22.21.3 聚焦 C/R/L 的真实 API 选择测试

1. `run_policy_target_handle_api_test.ps1 -CrlPrecision` 将 8 个家族/领袖/定居点案例合并为单次请求，仍为 `thinking.type=disabled`、JSON mode、temperature 0.25、2000 Tokens、无自动重试，不记录 Key、完整 prompt 或响应正文。案例包含家族错字与背景氏族、领袖错字、短城市名与相似村名、城堡/绑定村庄双向歧义、最富家族、边境粮仓城市及仅联络报告的负例。
2. 未明确说明“免税也是机械变化、语义证据允许错字映射”的首个独立请求为 HTTP 200、`finish_reason=stop`，7/8 通过；模型只把“免除贡达夫家族赋税”误判为空，没有编造或越权句柄。诊断后没有原样重试，而是将上述缺失约束补入生产主评议和测试镜像。
3. 修正后的新独立请求为 HTTP 200、`finish_reason=stop`、`5720.0 ms`，usage 为 prompt 1004、completion 404、cache hit 0、cache miss 1004；8/8 全部精确命中，负例为空，未知/跨案例/重复句柄均为 0。脱敏报告为 `Phase0_Local_Archive/reports/policy_target_crl_handle_selection_api_20260808.json`。两个请求各自均只调用一次且没有重试。

#### 22.21.4 编译、边界与未验证项

1. 最终源码使用唯一允许的 `一键编译覆盖推送/build_single_module.ps1 -Stage`，只在当前构建进程以 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`；Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success，脚本明确报告未修改游戏目录。
2. 两个校准/测试脚本 `-SyntaxOnly` 均通过；`git diff --check` 无空白错误，仅显示工作区既有 LF/CRLF 转换警告，新增未跟踪目标文件另经尾随空白扫描通过。本批未修改 `OnnxEmbeddingEngine`、`OnnxCrossEncoderReranker`、`WorldEntityRetrievalService`、九个效果模块、保存格式、动态 `PolicyObject` 恢复、期限、议程、费用、两次生产请求/无重试规则，也未增加影响力或其他业务效果。
3. 本批没有部署。仍未由真实存档验证首次稳定索引耗时、同版本第二次提交零文档重嵌入、外交/经济 dirty 只刷新动态快照、实际日志 `combinedPairs <= 25`，以及完整“世界快照 → 目标/效果 ONNX → 主评议 → 后处理 → C# 提交与结算”。Stage、离线 ONNX 和独立 API 夹具不能替代这条游戏内 smoke。
4. 未触碰或回滚 `TroopInspectionBehavior.cs`、`analysis/`、`TrueAutoBlock/`、用户 RAR 状态及其他无关用户修改。

### 22.22 2026-08-08 政策源码物理结构整理

#### 22.22.1 检查点与整理边界

1. 整理前先创建检查点提交 `2b2a24d5`（`feat: modularize policy effects and semantic targets`），只包含当时的政策实现与 Phase 0 账本/校准资料；用户 RAR 状态、`analysis/`、`TrueAutoBlock/`、根目录测试压缩包/补丁及其他无关修改均未进入提交。
2. 本节所述整理在该检查点之后进行，仅调整物理路径、拆分混合职责文件并移除已证明无引用的重复实现；命名空间、类型名、保存结构、动态 `PolicyObject` 恢复、政策期限、议程、费用、两次请求/无重试规则和业务语义均未改变。
3. 项目仍使用 SDK 默认递归编译；没有修改 `.csproj`、一键构建脚本、Bootstrap 或统一模块输出方式。

#### 22.22.2 当前权威源码路径

1. 政策生产源码统一收拢到 `PolicySystem/`：
   - 核心编排与日志：`PolicySystem/Core/CustomPolicyBehavior.cs`、`PolicySystem/Core/PolicySystemLog.cs`；
   - 议程：`PolicySystem/Agenda/KingdomAgendaCustomPolicyBehavior.cs`；
   - NPC 政策：`PolicySystem/Npc/NpcRulerPolicyBehavior.cs`；
   - 世界上下文：`PolicySystem/Context/WorldDiplomacyPolicyContext.cs`；
   - 政策 UI：`PolicySystem/UI/PolicySystemUi.cs`；
   - 效果路由、契约、目录与九个模块：`PolicySystem/Effects/`；
   - 目标语义召回：`PolicySystem/Targets/PolicyTargetSemanticRouter.cs`。
2. 原 `PolicySystemUi.cs` 的 33 个顶层类型按职责无损拆分：22 个政策 UI 类型保留在 `PolicySystem/UI/PolicySystemUi.cs`，9 个世界事件/收件箱类型移至 `WorldEvents/WorldEventInbox.cs`，2 个通用列表面板类型移至 `UI/Widgets/AnimusForgeListPanels.cs`；拆分前后类型集合完全一致且无重复。
3. `RulerPolicyProposalBehavior.cs` 与活跃的 `KingdomAgendaCustomPolicyBehavior.cs` 规范化内容相似度约 `98.67%`，并在源码、配置和自定义提示词中均无外部引用，因此移除死重复，只保留活跃议程实现。
4. 九个效果模块继续保持一效果一文件，未为了减少文件数重新合并；`CustomPolicyBehavior.cs`、`NpcRulerPolicyBehavior.cs` 和 `PolicyTargetSemanticRouter.cs` 也未在本批进行高风险逻辑切割。整理后实际编译的政策职责文件共 19 个；另有从混合 UI 文件提取出的 2 个非政策职责文件。
5. 本账本 22.22 以前出现的根目录 `CustomPolicyBehavior.cs`、`PolicySystemLog.cs`、`PolicyEffects/`、`PolicyTargets/` 等路径均保留为历史记录；从本节起以上述当前路径为准。

#### 22.22.3 验证

1. 对全部纯移动文件逐一比较 Git blob，确认内容未变；混合 UI 文件另以顶层类型集合守恒检查确认 33 个类型完整保留。删除重复实现后再次检查，源码与配置中不存在 `RulerPolicyProposalBehavior` 或其保存标签引用。
2. 整理后的源码使用唯一允许的 `一键编译覆盖推送/build_single_module.ps1 -Stage` 验证，只在当前构建进程以 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`；Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success，未部署游戏目录。
3. 本批仍未进行真实游戏存档或完整 ONNX → 主评议 → 后处理 → C# 提交/结算 smoke；物理整理的编译通过不能替代 22.21.4 所列运行态验证。

### 22.23 2026-08-08 政策源码职责拆分

#### 22.23.1 提交与拆分结果

1. 目录整理以提交 `21e6f896`（`refactor: organize policy source layout`）固化；UI/NPC 职责拆分以提交 `3c3a9d22`（`refactor: split policy ui and npc responsibilities`）固化；`CustomPolicyBehavior` 七个 partial 文件与本节账本以提交 `7f2053ce`（`refactor: split custom policy behavior partials`）固化。所有提交均使用显式路径暂存，没有使用 `git add -A`。
2. 政策 UI 现分为三个文件：`PolicySystem/UI/PolicySystemUi.cs` 只保留静态入口，`KingdomPolicyUi.cs` 保存玩家王国政策编写/结果/历史 UI，`LocalPolicyUi.cs` 保存地方政策编写/封地选择/历史 UI。拆分前后的 22 个政策 UI 顶层类型集合完全一致。
3. NPC 政策现分为五个文件：contracts、LLM client、Behavior core、Generation partial、Persistence partial。原 Behavior 类体按 `ProcessInitialGenerationCheck(...)` 与 `GetRecentPolicyRecordsInternal(...)` 两个连续成员边界原样分区；9 个原顶层类型、保存键和类体字符分区均守恒。
4. `CustomPolicyBehavior` 现分为七个文件：主文件保留常量、字段、构造、事件注册和外部入口；其余依次为 Lifecycle、Generation、Targets、Effects、Management、Models。所有 partial 仍为同一 `AnimusForge.CustomPolicyBehavior`；26 个嵌套类型继续嵌套在该类型下，因此 CLR 类型全名与保存结构不变。
5. 最终 `PolicySystem/` 下实际编译的政策职责文件共 31 个。九个效果模块继续一效果一文件；`PolicyTargetSemanticRouter.cs` 保持完整，没有拆散索引、动态快照、Facet、候选物化和性能上限。

#### 22.23.2 守恒与编译验证

1. `CustomPolicyBehavior` 拆分前后共核对 376 个方法块哈希、26 个嵌套类型块和 4 个保存键字面量，全部一致；七个 partial 声明只产生一个运行时类型。主文件移走成员后产生的长空白段只做空白压缩，没有修改方法体。
2. 第二批首次 Stage 暴露 `UI/Widgets/AnimusForgeListPanels.cs` 与 `WorldEvents/WorldEventInbox.cs` 文件尾被上一批空白修正误写为字面量 `\\r\\n`，Bannerlord 1.3 编译报 `CS1056`。没有盲目重试或重写历史；精确删除两个字面量并恢复单一文件尾换行后，重新执行完整 Stage 成功，该修复包含在 `3c3a9d22`。
3. UI/NPC 拆分修正后以及 `CustomPolicyBehavior` 最终拆分后，均只使用 `一键编译覆盖推送/build_single_module.ps1 -Stage`，并只在当前构建进程通过 `DefaultItemExcludes` 排除 `analysis/**` 与 `TrueAutoBlock/**`。两次最终结果均为 Bannerlord 1.3、Bannerlord 1.4、Bootstrap 0 warning / 0 error，Stage success，脚本明确报告未修改游戏目录。
4. 最终新增源码经尾随空白、单一文件尾换行、partial 声明数量和 `git diff --check` 检查；未修改 `.csproj`、构建脚本、命名空间、公开类型、方法签名、构造顺序、事件注册、保存格式或业务逻辑。

#### 22.23.3 固定边界与未验证项

1. 本批未修改 ONNX、目标召回阈值、效果模块路由、动态政策恢复、政策期限、议程、费用、两次 API 请求/无重试规则或任何政策效果，也未调用外部 API。
2. 本批没有部署，也没有进行游戏内存档或完整 ONNX → 主评议 → 后处理 → C# 提交/结算 smoke；Stage 只证明双版本编译与统一模块组装。
3. 未触碰、回滚或暂存用户 RAR、`analysis/`、`TrueAutoBlock/`、`OnnxCrossEncoderReranker.cs`、`PerfProbe.cs` 及其他无关用户修改。

### 22.24 2026-08-09 实机 ONNX 资产缺失与全国政策资格修复

#### 22.24.1 日志证据与根因

1. 游戏目录 `Logs/PolicySystem.txt` 在 `2026-08-09 00:06:13` 记录稳定实体索引构建完成（`entities=573`），随后统一 rerank 以“`reranker 目录不存在`”失败；主评议和后处理均未开始。游戏模块原有 `ONNX/` 只包含 embedding 模型，没有 `ONNX/reranker/`。
2. 22.20/22.21 校准实际使用的 reranker 位于本机外部模型目录；其 `model.onnx` 与 `tokenizer.json` SHA-256 分别为 `15b9a8c3da82eddf263df571281166e00e9308fe19d077084b642ebfcaf06d2b`、`9eb652ac4e40cc093272bbbe0f55d521cf67570060227109b5cdc20945a4489e`，与既有校准报告完全一致。故障是生产游戏目录漏装已校准资产，不是 embedding、CrossEncoder 推理或阈值逻辑回归。
3. `EvaluateEligibility(...)` 原条件为“非统治者且无封地才阻止”，因此拥有城镇/城堡的非统治者会被明确放行全国政策。地方政策已经独立存在后，这条旧资格规则与当前产品边界冲突；同一资格方法又被撰写界面、提交入口和评议完成前复核共同调用，所以日志中的全国政策提交不是 UI 假象。

#### 22.24.2 修复

1. 全国政策资格改为必须通过 `IsPlayerRuler(playerKingdom)`；拥有封地但不是统治者的玩家会收到“请使用地方政策”的阻止提示。地方政策资格、附庸国政策资格和现有费用规则均未修改。
2. 将上述已校准 reranker 安装到当前游戏模块 `Modules/AnimusForge/ONNX/reranker/`。统一部署脚本按既有规则保留已安装的 `ONNX` 目录，因此随后的事务性 DLL 覆盖没有删除或替换模型。
3. 未把约 1.13 GB 的模型复制进 Git 工作树，也未硬编码机器下载路径；当前修复针对已明确部署的本机游戏模块。未来全新安装仍需由发布/安装流程提供同哈希 reranker 资产，不能把仅含 embedding 的目录视为完整 ONNX 安装。

#### 22.24.3 验证与剩余边界

1. 当前 1.4 Stage DLL 使用同哈希 embedding/reranker 重新执行固定语义校准，8/8 样例通过，实体与 Facet 门槛均通过，`api_calls=0`。
2. 权威一键构建与部署重新执行：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，`Deploy Result: success`。部署后游戏目录 reranker 两个文件的 SHA-256 与校准资产逐字节一致。
3. 尝试让校准器直接以 D: 游戏模块作为 `StageModuleRoot` 时，在创建跨盘硬链接阶段被 Windows 拒绝（项目位于 E:）；失败发生在测试运行时组装、尚未加载 ONNX。当前采用的等价证据为“E: Stage DLL + 同哈希模型实际推理通过”以及“部署 DLL/模型分别与通过项哈希一致”。校准报告随后恢复为测试前字节内容。
4. 仍需玩家重新启动游戏后确认两项实机行为：非统治者全国政策按钮不可提交；正常政策日志出现 reranker 初始化、`target-semantic-shadow/selected` 和后续主评议，而不再出现目录缺失。尚未宣称完整 ONNX → 主评议 → 后处理 → C# 结算 smoke 已完成。

### 22.25 2026-08-09 玩家政策移除强制 Reranker 与 embedding-only 校准

#### 22.25.1 生产链路修正

1. `DuelSettings` 新增玩家政策“效果模块详规数量”，范围 `1–30`、默认 `6`、无需重启，并通过安全 getter 将旧配置或越界值收敛到合法范围；MCM 提示明确高值会增加 prompt 长度和 Token 消耗。
2. `PolicyEffectModuleRouter` 的作用域内 embedding recall 上限改为 `30`。选择阶段不再执行 CrossEncoder，也不再使用原 `4–6 + rerank margin` 自适应策略；现在只按 `RecallScore` 降序、再按模块 `Order/Id` 稳定排序，并精确返回 `min(MCM, 当前作用域合法模块数, 30)`。无人调用的 `Select(query, scope)` 同样改为 embedding-only，避免未来重新引入强制 reranker。
3. `GeneratePolicyResultAsync(...)` 只生成一次 `routingQuery` embedding，并把同一向量同时传给效果模块和目标路由。合并文档、`CombinedRerankPairLimit`、`OnnxCrossEncoderReranker.Instance` 与统一 batch rerank 已从玩家政策调用图移除；日志改为 `detailLimit/recalled/recallScores/targetPairs`，不再记录 `rerankScores/combinedPairs`。
4. `PolicyTargetSemanticRouter.Complete(...)` 直接消费 recall 阶段的 embedding cosine。实体 top 10、Facet top 8、一编辑距离名称提升、同家族去重、至少两个独立 Facet 组、每组只取最高项、关系/方向/类型/指标/作用域硬过滤、地方/王国候选上限及最终 C# 句柄校验均保留；类型 Facet 另增加原文显式类型词硬门槛。
5. embedding cosine 门槛写入生产路由：Facet 为 `direction_east 0.409850`、`direction_north 0.431938`、`geography_border 0.423639`、`metric_food_high 0.464818`、`metric_strength_low 0.431590`、`metric_wealth_high 0.532520`、`relation_domestic 0.467539`、`relation_enemy 0.468700`、`type_city 0.383388`、`type_clan 0.517851`；实体为 kingdom `0.569633`、clan `0.550144`、ruler `0.612518`、settlement `0.411177`，直接实体最小 cosine 竞争差为 `0.03`。语义扩展启用原因为 `embedding-calibrated-20260809`。

#### 22.25.2 离线校准结果

1. `run_policy_target_semantic_calibration.ps1` 已删除 `-RerankerModelDir`、reranker junction/hash、CrossEncoder delegate/call 与 logit 换算；运行时只加载仓库既有 embedding，不复制或下载模型，不调用 API。新报告独立写入 `policy_target_embedding_calibration_20260809.json` 与 `policy_target_entity_embedding_calibration_20260809.json`，没有覆盖已存在且带用户改动的 `policy_target_semantic_calibration_20260808.json`。
2. 固定/Facet 校准为 `11/11 passed`，覆盖 10 个已启用 Facet 和 kingdom/clan/ruler/settlement 固定实体类别；效果模块矩阵对 kingdom/local/vassal 三种作用域各验证 `1/6/30`，共 `9/9 passed`。当前 kingdom/vassal 有 9 个合法模块，local 有 8 个，因此 30 档分别只返回 9/8；排序重复调用完全一致，地方作用域没有 `kingdomStabilityOnce`。
3. 真实 XML/简中语料实体精度为 `48/48 passed`：家族 `16/16`、领袖 `8/8`、定居点最终召回与 top1 `24/24`；定居点纯 dense top 10 为 `23/24`，既有有界近似名称提升后为 `24/24`。三类无名负例激活均为 0；家族最小竞争差 `0.046688`、定居点 `0.030093`，均不低于生产 `0.03`。
4. 两份报告的 embedding SHA-256 均为 `69b353bb2aa2d09ab606ddbbc35437b03c843615a6bff28216a37fee7309c2aa`，`api_calls=0`，`passed=true`；脚本 UTF-8 BOM 与 Windows PowerShell 语法检查通过。

#### 22.25.3 构建、验证包、部署与资产清理

1. 阈值写回后使用既有 `build_single_module.ps1 -Stage` 再次构建：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，Stage success。工作树未跟踪的 `analysis/**` 与 `TrueAutoBlock/**` 会被 SDK 默认 glob 误纳入编译，因此仍只在当前构建进程设置 `DefaultItemExcludes`，没有修改 `.csproj` 或一键脚本。
2. 源模块与 Stage 的 `ONNX/` 均恰好只有 `config.json`、`model.onnx`、`model.onnx_data`、`tokenizer.json`、`tokenizer_config.json` 五个既有 embedding 文件，逐项 SHA-256 一致且没有 `reranker/`。从 Stage 以 `package_mod.ps1 -NoBump` 生成的验证 ZIP 共 1817 个条目，其中 ONNX 与 reranker 条目均为 0；Stage `SubModule.xml` 哈希未变。
3. 现有 `build_single_module.ps1 -Deploy` 重新完成 1.3、1.4、Bootstrap 构建和统一模块事务部署，三项均 0 warning / 0 error，`Deploy Result: success`；游戏目录三份 DLL 与本次 artifacts 哈希一致，`.AnimusForge.deploy/.backup` 残留为 0。
4. 部署后精确解析并删除的唯一目录为 `D:\steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\ONNX\reranker`。删除前断言其父目录为目标模块 ONNX 且不是重解析点；删除后父目录五个 embedding 文件数量与逐项 SHA-256 均未变化，外部源 `D:\下载\rag_temp_pack\models\reranker` 仍存在且未触碰。
5. 本批未修改 `OnnxEmbeddingEngine.cs`、`OnnxCrossEncoderReranker.cs`、Stage/Deploy/打包脚本或任何 ONNX 文件，也未暂存或提交。工作树中这些文件若原已修改、删除或未跟踪，继续按用户状态保留。

#### 22.25.4 实机边界

1. Steam Launcher 已在完成部署和 reranker 清理后启动；启动前 `PolicySystem.txt` 长度基线为 2362721 bytes、最后写入 UTC 为 `2026-08-08T16:06:14.3339216Z`。
2. 尚待玩家在可丢弃会话中加载存档并分别以 MCM `1/6/30` 提交真实政策，因此当前不能宣称主评议、效果后处理、最终 C# 发布、单政策一次 query embedding 和无 reranker 初始化错误已经完成实机验证；完成后应只读取上述基线之后的新增日志并把结果续记在本节。
3. 本轮 Steam 启动实际产生 Launcher 和 Bannerlord 运行进程；Bannerlord 的 `rgl_log_10892.txt` 记录到 01:59:52 加载基础依赖/ButterLib 后进程退出，尚未到达 AnimusForge Bootstrap。没有生成对应崩溃转储或 `rgl_log_errors` 错误，`AnimusForge.Bootstrap.log` 与 `PolicySystem.txt` 均没有新增内容。因此该次退出发生在 AF 初始化前，不能归因于本次政策改动，也不能计作实机 smoke 通过；在玩家能稳定进入游戏后仍需执行上一条三档验证。

### 22.26 2026-08-09 恢复城镇/城堡主目标与村庄随附语义

#### 22.26.1 目标边界与执行展开

1. `PolicyTargetSemanticRouter` 的 settlement 实体索引现只收录城市和城堡；村庄、`type_village`、`metric_hearth_high/low`、`IsVillage` 与实体 `Hearth` 字段已从目标召回、Facet、筛选和排序中移除。王国、家族、统治者的封地数量、位置和候选物化也只按城镇/城堡父级计算。
2. `ResolvePrimaryPolicyFief(...)` 统一处理目标：城镇/城堡返回自身，村庄归并到 `Village.Bound`，没有合法父级则拒绝。明确点名村庄时只生成父级 `L*`；同一父级下多个村庄会按父级 ID 去重；发布地范围内的村庄只沿用 `S`，不再生成额外句柄。
3. 直接 `L*` 及旧保存数据中的村庄 ID 在运行时先归并父级，再经 `ExpandLocalPolicySettlements(...)` 展开为父级和全部附属村庄。新生成的 settlement 句柄必须同时通过“实体是城镇/城堡、属于玩家王国、不是发布地”的 C# 硬校验；村庄实体 ID 不能进入后处理白名单。
4. `S/L*/C*/R*` 的 LLM 可见覆盖数量只计城镇/城堡父级；`K*` 的语义候选数量同样只计父级。实际 C# 结算仍把父级附属村庄纳入范围：Town 模型补丁只结算繁荣、粮食、忠诚、治安、民兵、税收和建设，Village 模型补丁只结算 `hearthPerDay`。

#### 22.26.2 LLM 上下文与 fail-closed 规则

1. 地方世界上下文不再输出逐村庄名称、ID、户数或“展开后定居点”数量；王国概况不再输出村庄数量、户数均值或村庄极值名称。知识实体快照命中明确村庄时只登记其父级城镇/城堡。
2. 主评议和效果后处理均明确：村庄没有独立句柄，父级句柄由 C# 自动包含附属村庄，`hearthPerDay` 必须写在合法父级或 `K*` 句柄上，禁止 `S附属村庄`、`L0附属村庄` 等复合句柄，也禁止把村庄名称或 ID 当作 target。
3. 后处理没有新增纠错或重试 API。离线反射验证把 `S附属村庄` 送入正式 `TryBuildFinalPolicyPostprocess(...)`，结果为 `false`，错误为“后处理目标不是本次作用域合法句柄：S附属村庄”；另用伪造的 `kind=settlement`、`IsCity=false`、`IsCastle=false` 快照实体验证 `L0:village_test` 同样被 `IsPolicyTargetHandleAllowedForRequest(...)` 拒绝。因此非法目标继续在扣费和生效前 fail-closed。

#### 22.26.3 embedding-only 校准

1. 固定校准报告独立写入 `Phase0_Local_Archive/reports/policy_target_primary_fief_embedding_calibration_20260809.json`：11/11 固定案例通过，包含城镇/城堡简称/错字、村庄错字不产生独立实体目标、Facet 分离、效果模块 `1/6/30` 稳定选择、复合村庄句柄拒绝与源码边界断言；`passed=true`、`api_calls=0`。
2. 真实 SandBox XML 校准报告独立写入 `policy_target_primary_fief_entity_embedding_calibration_20260809.json`：40/40 通过，其中家族 16/16、统治者 8/8、城镇/城堡 16/16；XML 中存在 273 个村庄源节点，273/273 均具有合法城镇/城堡 `Bound`，其中 118 个父级绑定多个村庄，但索引村庄数为 0，城镇/城堡实体为 120，估算生产稳定实体总数为 274，低于旧日志的 573。
3. 两份报告只使用既有 embedding，SHA-256 为 `69b353bb2aa2d09ab606ddbbc35437b03c843615a6bff28216a37fee7309c2aa`；没有下载、复制或调用任何新 ONNX/CrossEncoder 资产。生产 cosine 阈值和一次 query embedding 结构保持 22.25 的已校准值。

#### 22.26.4 构建、打包与部署

1. 最终使用既有 `build_single_module.ps1` 构建并部署：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 warning / 0 error，`Deploy Result: success`。未跟踪 `analysis/**` 与 `TrueAutoBlock/**` 仍只通过当前进程的 `DefaultItemExcludes` 排除，没有修改 `.csproj` 或一键脚本。
2. 从 Stage 使用既有 `package_mod.ps1 -NoBump` 生成验证包，1817 个条目，`ONNX/` 与 reranker 条目均为 0。源码、Stage、游戏目录的 ONNX 均只有原有五个 embedding 文件且 SHA-256 一致；游戏目录没有 `ONNX/reranker`。
3. 游戏目录 Bootstrap、1.3 实现和 1.4 实现 DLL 与本次 artifacts 的 SHA-256 分别一致；事务部署残留 `.AnimusForge.deploy/.backup` 为 0。未修改 ONNX、Reranker、Stage/Deploy/打包脚本，未暂存或提交。
4. 本节尚未宣称真实存档中的主评议、效果后处理、每日结算或 MCM `1/6/30` 实机 smoke 已通过；该部分仍需玩家进入可丢弃会话提交政策后读取新增 `PolicySystem.txt` 验证。
