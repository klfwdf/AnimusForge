# AnimusForge 逐文件 Owner Matrix（第一版）

> 用于重构前导航和代码评审。这里的 owner 是逻辑责任，不是当前 DLL 边界，也不是立即移动文件的授权。以当前 `d4cb1467` 及后续已记录的准备文档为基线。

## 使用规则

- 新功能优先进入一个明确 owner，不把玩法逻辑继续添加到 `SubModule.cs`。
- 跨 owner 行为只通过公开能力/事件协作；确实属于双方共同玩法时再建立 Bridge。
- 文件如果同时包含多个责任，先建立 facade/adapter，再逐步提取，不做整文件搬迁。
- 生产程序集暂时仍为单一 `AnimusForge.dll`；物理 DLL 拆分延后。
- 任何涉及 `SyncData`、Harmony、Mission、UI、Tick 或 LLM 动作的切片，都要在这里补充影响和验收记录。

## 运行与基础设施

| 当前文件/路径 | 当前职责 | 目标 owner | 迁移策略 | 风险/验收 |
|---|---|---|---|---|
| `AnimusForge/SubModule.xml` | 统一模块声明、Bootstrap-only 加载、Items XML | Bootstrap / Host | 保持不变；只允许 Bootstrap DLL | Id/Name/版本/资源路径；不得声明实现 DLL |
| `AnimusForge.Bootstrap/BootstrapSubModule.cs` | 生命周期转发、启动失败处理 | Bootstrap | 保持独立 | 启动、卸载、类型注册、1.3/1.4 |
| `AnimusForge.Bootstrap/BootstrapRuntime.cs` | API 线检测、实现 DLL 选择/加载、resolver | Bootstrap | 保持最小 | 只加载一个实现；版本歧义 fail-closed |
| `AnimusForge.Bootstrap/BootstrapLog.cs` | Bootstrap 诊断 | Bootstrap | 保持独立 | 输出实际版本、路径、实现版本 |
| `SubModule.cs` | 所有行为/模型/Harmony/Mission/每帧调度的组合根 | Host/Composition | 先按注册、Patch、Tick、模型、外部集成分组；保持顺序 | 任何一帧成本、注册顺序、失败隔离 |
| `AnimusForge.csproj` | 单实现项目、1.3/1.4 条件编译、依赖解析、资源嵌入 | Host/Build boundary | 暂不重排；先建立包含/资源/依赖清单 | 两 API 线、资源嵌入、链接源码 |
| `AnimusForgeModulePaths.cs` | 活动模块根、legacy 只读迁移路径 | Foundation/GameAdapter | 提取路径端口，保留迁移规则 | 不把 legacy 目录当活动输出 |
| `Logger.cs`, `TraceHelper.cs`, `FeatureDiagnosticLogFile.cs` | 日志、trace、功能诊断 | Foundation/Diagnostics | 统一 trace 与有界日志 | 不记录 API key/无限制玩家文本 |
| `PerfProbe.cs`, `FreezeWatchdog.cs`, `CampaignTickDiagnosticsPatch.cs` | 性能、冻结和 Tick 诊断 | Foundation/Runtime Safety | 保持低分配；按阶段抽取 | 每帧预算、队列深度、丢弃/过期统计 |
| `BannerlordExceptionSentinel.cs`, `NonBlockingErrorReport.cs` | 原版异常边界和错误报告 | Compatibility/Safety | 与功能 owner 分离；保留 fail-open/native fallback 语义 | 不因 AF 关闭而吞掉原版逻辑 |
| `SaveRuntimeGuard.cs` | 存档/加载 generation 和 stale work 防护 | Foundation/Persistence | 作为所有异步模块的公共端口 | load 后 stale completion 必须被拒绝 |

## 交互、AI、记忆和动作

| 当前文件/路径 | 当前职责 | 目标 owner | 迁移策略 | 风险/验收 |
|---|---|---|---|---|
| `MyBehavior.cs` | 自由对话、记忆、事件、Persona、AFEF、周报、外部 facade、巨大 `SyncData` | Conversation facade + Memory/Persistence 子域 | 不整体移动；先从公开 facade、保存镜像和纯转换器开始 | 旧 key/type、三渠道历史、save/load |
| `ShoutBehavior.cs` | 场景喊话、Native Conversation、Prompt、后处理、动作、TTS、Mission、目标 | Conversation orchestration；Scene/Action adapter | 保留为过渡编排器；先抽 DTO、目标解析、ActionPlan | 三渠道、主线程、可见文本、stale target |
| `AIConfigHandler.cs` | Prompt 配置、preprocess、guardrail、postprocess、AFEF normalization、辅助 LLM | Prompt/Rule + LLM Gateway + ActionPostprocess | 分成纯配置/规则、网络、归一化三个边界；不放领域副作用 | JSON/C# 标签同步、超时、fallback |
| `PromptComposer.cs`, `PromptListRetrievalService.cs`, `IntentQueryOptimizer.cs` | Prompt 组合、规则/知识候选检索、意图优化 | Prompt/Retrieval | 保持纯输入输出；缓存和批量策略明确 | 不做热路径全量扫描 |
| `LlmApiCompat.cs`, `LlmRetryPrompt.cs`, `LlmVisibleReplyNormalizer.cs` | LLM API 兼容、重试、可见回复清理 | LLM Gateway/Safety | 统一请求/结果状态和 trace | 不把内部标签泄露给玩家 |
| `Guardrail*.cs`, `AnimusForgeTextInputSanitizer.cs` | 输入/规则安全和文本清理 | Safety/Prompt | 与领域动作分离 | 关闭 AF 时不改变原版行为 |
| `OnnxEmbeddingEngine.cs`, `OnnxCrossEncoderReranker.cs`, `RagWarmupCoordinator.cs`, `WorldEntityRetrievalService.cs` | 本地 embedding/rerank/RAG | Knowledge/Retrieval | 通过 provider 接口使用；模型运行时不进入 Foundation | ONNX 资源/线程/缓存/热路径 |
| `KnowledgeLibraryBehavior.cs` | 知识文件、RAG 索引、知识存档、导入导出 | Knowledge/Persistence | 先抽 storage codec/index facade；保留旧 chunk key | 自定义 chunk 协议、SemanticPrototypes 丢弃行为 |
| `CampaignSaveChunkHelper.cs` | UTF-8 安全分块、字典展平和恢复 | Persistence | 先冻结兼容协议，再提供新 facade | 旧 suffix/key、缺 chunk、大小上限 |
| `EncounterConversationTargetResolver.cs` | 从参数/遭遇解析明确 Hero | Conversation/GameAdapter | 统一三个 conversation patch 的解析入口 | 军团成员不能被 LeaderHero 覆盖 |
| `Patch_Conversation_Start_Intercept.cs` | Native Conversation 启动拦截 | Conversation/GameAdapter | 只保留入口 patch；业务判断下沉 | 反射发现、早返回顺序、原版 fallback |
| `Patch_ConversationManager_OpenMapConversation.cs` | 地图会话入口拦截 | Conversation/GameAdapter | 调用统一 resolver 和授权端口 | selected target、prisoner/native context |
| `Patch_ConversationManager_SetupAndStartMapConversation.cs` | 地图会话设置/启动拦截 | Conversation/GameAdapter | 与上者共用 resolver | 重复拦截、参数签名漂移 |
| `AnimusForgeNativeConversationOverlay*.cs` | Native 对话 UI、输入、提交、历史/动作入口 | UI + Conversation adapter | UI 只持有公开 Conversation session | 主线程、输入焦点、关闭后 stale work |
| `ConversationVMCapturePatch.cs`, `NativeConversationAnswerAreaController.cs` | 原版 VM/回答区接入 | UI/GameAdapter | 保持 UI patch 薄 | Gauntlet 版本差异、事件抢占 |
| `ConversationHelper.cs`, `ConversationMessage.cs` | 会话消息和辅助逻辑 | Conversation.Contracts | 先冻结 role/AFEF 语义 | user=玩家，assistant=NPC |
| `ShoutNetwork.cs` | 场景/对话网络调用和消息请求 | LLM Gateway adapter | 与渠道编排分离 | 后台请求不可携带 live game object |
| `ShoutUtils.cs` | 场景辅助、Persona、历史和数据导入导出 | Conversation/Knowledge compatibility | 按功能拆 facade，不整体搬迁 | 外部 JSON 与 save authority |
| `ActionPostprocessConfigModel.cs`, `PreprocessPromptsConfigModel.cs`, `AIConfigModel.cs` | 配置 DTO | Prompt/Contracts | 纯模型，禁止游戏副作用 | 配置版本和 unknown fields |
| `GiveAssetTagCodec.cs`, `TransferQuantitySpec.cs` | 资产动作标签/参数编解码 | Action/Asset transfer | 纯 parser 先独立测试 | 规则、parser、executor 三方一致 |

## 渠道适配器

| 当前文件/路径 | 当前职责 | 目标 owner | 迁移策略 | 风险/验收 |
|---|---|---|---|---|
| `ShoutBehavior.cs` scene path | 场景喊话、Agent 候选、Scene prompt、TTS | Conversation + Scene adapter | 作为参考实现，先抽 shared pipeline | Agent/LocationCharacter、Mission 生命周期 |
| `ShoutBehavior.cs` native path | Native input/session/history/action | Conversation + Native adapter | 抽 `InteractionSession` 和 shared ActionPlan | static session history 未进 SyncData |
| `CourierDeliveryBehavior.cs` | 信件、Courier party、异步回复、送达/返回动作、保存 | Courier + Conversation adapter | 保留状态机；共享 Prompt/Memory/Action 契约 | exactly-once flags、延迟执行、save/load |
| `CourierFoodConsumptionModel.cs`, `CourierMobilePartyAIModel.cs`, `CourierPartyTransition*.cs` | Courier party 模型/迁移 | Courier/GameAdapter | 与信件语义分开 | 原版模型 fallback、队伍生命周期 |
| `CourierLetterInputPopup*.cs`, `CourierLetterReplyPopup*.cs`, `CourierVisibleLetterSanitizer.cs` | Courier UI 和可见文本 | Courier UI | UI 只调用 Courier facade | 文本清理、玩家/NPC role |
| `CompanionProactiveChatBehavior.cs` | Companion 主动会话 | Conversation/Proactive | 接入 shared session/memory/action | save load Opening→Pending |
| `ProactiveNpcRequestBehavior.cs` | NPC 主动请求、候选扫描、冷却 | Conversation/Proactive | 区分持久 session 与 transient scan | scan 不持久化、stale request |
| `VanillaIssuePromptBehavior.cs`, `VanillaIssueOfferBridge.cs` | 原版 Issue 的 Prompt/offer | Conversation/Vanilla bridge | 只保留原版入口适配 | issue completion fact |
| `LordEncounterBehavior.cs` | 遭遇目标、会面菜单、meeting Mission | Encounter/Conversation + Mission adapter | 目标解析先独立；会面生命周期后拆 | Army member target、mission start/end |

## Policy / World / Economy

| 当前文件/路径 | 当前职责 | 目标 owner | 迁移策略 | 风险/验收 |
|---|---|---|---|---|
| `PolicySystem/Core/CustomPolicyBehavior*.cs` | 玩家/王国自定义政策生命周期、管理、效果、目标、持久化 | Policy | 当前最清晰域；优先以现有目录为边界 | record/effect/registry 跨 key |
| `PolicySystem/Effects/**` | 效果合同、编译、路由、保存、模块 | Policy Effects | 新效果继续走模块目录，不塞回 Behavior | 延迟执行、目标冻结、双版本 |
| `PolicySystem/Effects/Modules/**` | 金币、稳定、生产、队伍规模、XP 等效果 | Policy Effect Modules | 保持单一效果 owner；纯计算优先 | campaign side effects、执行 receipts |
| `PolicySystem/Targets/**` | 目标目录、语义路由和计划 | Policy Targeting | 作为 policy 内部公开能力 | 别名扩大、ownership snapshot |
| `PolicySystem/Npc/**`, `NpcPolicyContracts.cs` | NPC ruler policy、生成、保存、LLM | NPC Policy | 与 WorldDiplomacy 通过 contract 协作 | policy history、LLM stale state |
| `PolicySystem/UI/**` | 政策 UI、编辑器、效果管理 | Policy UI | 保持 UI/状态分离 | ApplicationTick、Gauntlet |
| `VoteDealBehavior*.cs` | 投票交易、议程、提案和外交 deal | Policy/Diplomacy bridge | 根据行为归属拆 deal contract 与执行 | clan/proposer/target identity |
| `RewardSystemBehavior*.cs` | 奖励、RP、物品、信任、债务、生成物品 | Economy/Reward | 先拆纯资产/债务/奖励服务，保留 facade | item identity、save keys、ActionPlan |
| `DebtPromiseQuest.cs` | 债务任务/兑现流程 | Economy/Debt | 与 Reward 共享 Debt contract | quest/save/action facts |
| `DiplomacyBehavior.cs` | 原版外交扩展、和平、关系 | Diplomacy/GameAdapter | 先提纯规则，保留行为入口 | native fallback、战争状态 |
| `WorldDiplomacyBehavior.cs` | 世界外交 LLM、威胁、冷却、和平/割让结果、通知 | World Diplomacy | 先拆纯 rules，再拆 LLM/persistence | campaign-wide side effects |
| `WorldDiplomacy*Rules.cs`, `DiplomacyPeaceTermsService.cs` | 外交纯规则/和平条款 | Diplomacy Rules | 优先独立测试 | cooldown/threat/result correctness |
| `VassalageBehavior.cs`, `NpcTributeVassalageBehavior.cs` | 附庸、贡金、保护、服从 | Political Progression | 统一契约后再和外交协作 | kingdom state、payments、save |
| `KingdomAnnexationBehavior.cs` | 王国兼并 | World/Political | 保留独立 owner | settlement/clan identity |
| `WorldMapPartyCommandBehavior.cs` | 大地图 Party 命令、detach/expedition/guest | WorldMap | 与 Scene Agent 移动严格分开 | party lifecycle、native fallback |
| `WorldEvents/**`, `WorldMessageTimelineMenuBehavior.cs` | 世界事件、时间线、未读消息 | World Events/Timeline | 事件事实与 UI 分离 | save/read state、notifications |

## Settlement / Siege / Mission / Combat

| 当前文件/路径 | 当前职责 | 目标 owner | 迁移策略 | 风险/验收 |
|---|---|---|---|---|
| `AnimusForge.SiegeAftermathIntervention/**` | 可复用 GCCZ/SETS 规则、profile、routing、memory codec | Settlement/Siege Rules | 继续作为规则 owner；AF 根只做 adapter | contract、bridge matrix、save codec |
| `SiegeAiInterventionBehavior*.cs` | 攻城介入 campaign/mission、后处理、居民/士兵/结算 | Siege Runtime | 先拆纯 profile/rules，再拆 runtime adapters | settlement ownership、Mission、save |
| `VillageAftermathBehavior.cs` | 村庄后果和文化过渡 | Settlement Aftermath | 保留行为 facade | daily state、culture transition |
| `CastleAftermath*.cs` | 城堡后果、领主/囚犯/装备/建设/mission bridges | Castle Aftermath | 每个 bridge 只做单一运行时适配 | mission lifecycle、persistent state |
| `GcczTownRuleMemory*.cs`, `GcczSettlementCulturePersistenceBehavior.cs` | 城镇规则记忆/文化保存 | Settlement Memory | 复用 GCCZ codec/store | namespace、legacy key |
| `SettlementEntryTroopSelectionBehavior.cs` | 定居点进入时队伍选择 | Settlement Entry | 行为与 MissionLogic 分离 | roster、mission initialization |
| `TownAmbientDialogueBehavior.cs`, civilian population mission files | 城镇居民/环境对话和人口 | Scene/Settlement | 共用 Conversation，但不混 WorldMap | Agent、场景 context |
| `DuelBehavior.cs`, `DuelBehavior.Outcomes.cs`, `Refactor/Runtime/DuelOutcomeReceipt.cs`, `DuelSettings*.cs` | 决斗配置、stakes、Mission、死亡和结果；process-local typed dispatch/session/outcome/readback | Duel/Combat | legacy public facade不变；exact Native/Scene request在副作用前Queue唯一DuelId并贯穿delayed host/actual start/三终态；Courier拒绝；load只转Unknown且绝不重放 | exact provenance已离线VERIFY；仍需live accept/reject/cancel/death/exit、stake/Memory、旧档、Fourberie |
| `SceneTauntBehavior.cs`, `SceneTaunt*Patch.cs` | 场景挑衅、冲突升级、Mission | Scene Combat | 严格 damage-context guard；先拆规则 | 和平/竞技场/训练/攻城排除 |
| `MilitaryExerciseBehavior.cs` | 训练/演习 campaign+Mission | Mission/Training | 行为/mission logic 分离 | roster、native damage |
| `TroopInspectionBehavior.cs`, prisoner slaughter runtime | 队伍检查/囚犯处置 | Mission/Prisoner | 保留原版安全 fallback | prisoner identity、Mission |
| `NoblePrisonerEscortBehavior*.cs` | 囚犯护送 campaign/mission | Prisoner Logistics | campaign session 与 mission adapter 分离 | party/hero ownership |
| `NoblePrisonerExecutionOrderBehavior.cs`, `NoblePrisonerExecutionRuntime.cs` | 执行命令和运行时结果 | Prisoner/Execution | 结果确认后写事实 | death/relationship/save |
| `MeetingBattleLockMissionBehavior.cs`, meeting patches | 会面战斗锁定和目标 | Encounter Mission | 依赖统一 target resolver | 不误触普通战斗 |
| `extensions/AnimusForge.XihaiAction/src/**` | SceneActions/BattleSpeech 核心和 Mission runtime | SceneActions Runtime | 保持 reusable runtime；AF 侧薄 adapter；当前链接入主 DLL | source-link drift、Agent target、MCM |

## Social / Knowledge / UI

| 当前文件/路径 | 当前职责 | 目标 owner | 迁移策略 | 风险/验收 |
|---|---|---|---|---|
| `PlayerNotorietyBehavior.cs` | 声望、材料、摘要、save | Social Progression | 提取纯状态/事实服务，保留 behavior | chunked JSON、save recovery |
| `RomanceSystemBehavior.cs`, `SexualConceptionBehavior.cs` | 恋爱、婚姻、生育 | Social Progression | 与 Memory/Conversation 通过公开事实协作 | campaign side effects |
| `NobleGatheringBehavior.cs` | 贵族聚会和冷却 | Social/World Event | session/cooldown 与 UI 分开 | hourly/daily cost |
| `AnimusForgeUniqueCosmeticItemBehavior.cs` | 唯一外观物品和迁移 flag | Progression/Items | 保留 key 和一次性迁移语义 | item identity |
| `KnowledgeLibraryBehavior.cs` | 知识/RAG/规则库 | Knowledge | 见交互表；不要把 UI/导入逻辑继续堆入 | custom chunk, index rebuild |
| `KingdomStrategicProfileBehavior*.cs` | 王国战略 profile、导入导出 | World Profile | profile schema 与 UI/文件服务分开 | PlayerExports/save precedence |
| `AnimusForgeNativeConversationOverlay*.cs`, history popups | 对话 UI、历史、编辑 | UI/Conversation | UI 不直接拥有 campaign state | input focus、native keyboard |
| `Encyclopedia*Patch.cs`, `EncyclopediaEntityLink*` | 百科按钮、实体导航、规则/Persona展示 | UI/Knowledge adapter | 遵循真实 RootWidget 和 datasource 缓存案例 | button event、Backspace suppression |
| `Terminal*`, `WorldMessageTimelineUi.cs`, weekly report UI | Terminal、周报、时间线展示 | UI/Reports | view model 不直接改领域状态 | main thread、deferred close |
| `Dev*Popup*.cs`, `PlayerExportsEditor/**` | 开发工具和外部数据编辑 | External Tools/UI | 保持 runtime 外；文件安全写入 | RawJson、backup、privacy |
| `AnimusForge/ModuleData/**` | 发布规则、Prompt、XML、默认 profile | Content Ownership | 每个资源登记唯一 owner；不把静态资源当存档迁移 | embedded vs external loading |
| `AnimusForge/PlayerExports/**` | 用户可编辑导出包 | User Data boundary | 先定义 precedence；不作为普通 runtime source | merge/non-delete/privacy |
| `AnimusForge/ONNX/**` | 模型和 tokenizer | Retrieval Content | 与运行时 DLL/发布策略单独登记 | paired external data、package omission |

## 资源、工具和参考树

| 当前路径 | 目标分类 | 处理原则 |
|---|---|---|
| `tools/ActionPostprocessPromptLab/**` | Prompt tooling/tests | 只保留源码和必要 case；runs/dist/local settings 进 artifacts/local |
| `tools/PreprocessTopicPromptLab/**` | Prompt tooling/tests | 与 runtime Prompt 契约测试分开 |
| `tools/PlayerExportsEditor/**` | External tool | 不编译进主 runtime；发布物不作为源码 |
| `tools/*SmokeTests`, `PolicyEffectModule.ContractTests` | Tests | 纳入 contract/composition/persistence 验证矩阵 |
| `原版游戏本体代码1.3.x/**` | 本地 reference | 以版本/hash manifest 管理，避免被当作 AF 源码 |
| `原版游戏本体代码1.4.5/**` | 本地 reference | 目录名历史保留；目标兼容线是 1.4.x，实际版本单独记录 |
| `一键编译覆盖推送/**` | Build/deploy/package scripts | 未经单独授权不修改；区分 build-only 和 deploy |
| `.tmp/**`, `.codex_tmp/**`, `bin/**`, `obj/**` | Local/artifacts | 先审计消费者和 tracked 状态，再分批清理；不得误删依赖 |

## 2026-08-31 Conversation Host commit boundary

- Owner: Conversation lifecycle / GameAdapter dispatch contract, not Economy gameplay or GCCZ rules.
- `Refactor/Runtime/DetachedInteractionHost.cs`: each submitted commit callback is consumed once and closed before pre-commit fallback; failures after callback entry are terminal, retaining any observed receipt. `afterCommit` requires successful history. Queued cancellation is checked on callback entry.
- Entry points: Native opt-in runner and Shout/Courier detached hosts. Default channel entries, public signatures, save identity/key/type and resources are unchanged; no new Harmony, tick, queue or scan work.
- Validation: `InteractionPipelineContractTests` fault matrix and `ProductionConfiguredHostReplayTests`; actual results/rollback/NOT-RUN scope are recorded in the execution ledger and `docs/handoffs/2026-08-31-local-refactor-commit-boundary.md`.

## 2026-09-01 Request receipts and validation framework

- Conversation/Memory lifecycle: `InteractionResultCommitter` reserves a bounded request receipt before owner execution; `InteractionCommitReceiptCache` retains terminal failures, rejects changed payloads/reentry, and distinguishes duplicates. `DetachedInteractionHost` does not repeat `afterCommit` for a duplicate. The public Native opt-in runner retains its signature but no longer falls back after its callback starts.
- Request identity remains channel/session/subject + trace/runtime/save generation, with the existing Courier direction value. `LOCAL-7-H` adds an internal process nonce to trace identity so restart-local sequence reuse cannot collide with a durable tombstone; public capture/session contracts remain unchanged.
- Test-tool owner: `tools/ReplayDependencies` replaces four machine-specific copy targets with explicit, validated dependency sources. It never changes official build/deploy scripts or game files.
- Validation owner (`LOCAL-8-A`): `tools/PhaseEightReadiness` keeps the existing8-ID design catalog/Bridge/Composition contracts, and additionally source-binds canonical20 acceptance responsibility buckets, canonical16 `PAIR`/`CROSS_CUT` Bridges and18 cleanup candidates. Domain roles remain `ROLE_PLACEHOLDER` and entry lists `REPRESENTATIVE`, so real readiness blocks until owners assign/complete them. Evidence declares exact domain/bridge/candidate IDs; OFFLINE/LIVE/SAVE coverage, cleanup audit/replacement/rollback and strict pre-HEAD checkpoint are separately gated. The tool never authorizes cutover/deletion/deployment/push/publish and never upgrades fixture/Stage evidence into live proof.
- Memory owner acceptance (`LOCAL-7-D`): MyBehavior's existing daily/recent writer now returns runtime readback confirmation to the batch facade; no receipt is created on missing owner, rejection or unconfirmed writes. Public void/non-batch compatibility remains. Identity, day and newly published object references are checked after sanitization; this is not a transaction or proof of disk/save persistence. See `docs/animusforge-request-commit-receipts.md`.
- Courier/Economy owner gate (`LOCAL-7-E`): the existing Courier session is revalidated before any Economy replay; economy-only reserves persisted `PostprocessConsumed` first, while mixed plans continue through the filtered legacy callback. No new key/field or parallel receipt was added. This is at-most-once/fail-closed and still needs live save/load evidence.
- Economy outcome receipt (`LOCAL-7-F`): Hero/Party/Merchant owners distinguish structured known partials; the executor/committer retains only owner-confirmed facts as a non-retryable action receipt and never counts legacy actions without a receipt. No persistence identity changes; request idempotency remains bounded/process-local.
- Unknown effect receipt (`LOCAL-7-G`): post-callback throw/null/malformed owner receipts and replay-aware Hero/Party/Merchant helper uncertainty become `UnknownAfterStart`; known prior count/facts survive, the uncertain action creates no fact, and Host/cache/duplicate paths cannot fallback, replay or accept dispatcher fake success. Existing bool/int helper ABI and save identity remain unchanged.
- Persistent memory owner (`LOCAL-7-H`): `MyBehavior.MemoryRecovery.cs` owns one additive `_af_interactionMemoryRecovery_v1 : Dictionary<string,string>` ledger and hidden Daily/Recent markers. It repairs only missing user/AFEF/assistant components, bounded at 64 pending/512 tombstones, validates checksum/hash/marker masks on load, preserves Scene target and non-Hero projection migrations, and never references or invokes an action/afterCommit owner. The old six-argument strict API and four public void APIs remain unchanged.
- Courier inbound completion owner (`LOCAL-7-I`): `CourierDeliveryBehavior` persists one additive `AFCI1` string inside its existing `_af_courier_sessions_v1` session JSON before the memory owner starts. The receipt binds opaque recovery ID + MyBehavior payload hash + session/sender/current-player/party + frozen visible letter. MyBehavior exposes only internal prepare/status seams; Courier consumes at most one actionable receipt per tick and advances only after payload-matched completion. Invalid, missing, disabled, quarantined or conflicting receipts abort the inbound Courier and release its wait lock. This owner does not call an ActionPlan/Economy/postprocess executor and does not reuse outbound `PostprocessConsumed`.
- Memory auxiliary recovery boundary (`LOCAL-7-J`): H's recovery writer now owns only Daily/Recent projection and markers. It never consumes or attaches the process-local weekly candidate and never calls notoriety from tick/load/`ExistingPending`. A brand-new receipt that completes synchronously may make one current-runtime notoriety attempt for each exact marker-backed user/assistant component; the void owner has no readback, so the result remains `attempted_unconfirmed / NOT-RECOVERABLE`. The legacy live `Attach→Save→Note` path and all H/I persistence identities remain unchanged.
- Weekly exact-intent/outcome owner (`LOCAL-7-K`): `InteractionResultCommitter` prepares an independent bounded receipt before Economy replay and publishes only after the whole Economy-only ActionPlan has owner-confirmed full count/effect, an exact actual execution fingerprint and written memory. Candidate projection uses a canonical stateless adapter and never consumes the injected gameplay planner. `MyBehavior.WeeklyActionOutcomeReceipts.cs` owns additive symbolic `_af_weeklyActionOutcomeReceipts_v1 : Dictionary<string,string>` storage (64 pending / 512 terminal); durable identity is probed before live value/foothold reconstruction, and load may retry only a Confirmed data-only trigger attach. Prepared load becomes Unknown; mixed/legacy/partial/unknown/rejected/unsupported values never publish. The receipt stores no raw ActionPlan, executor or callback and does not enter H or Courier `AFCI1`.
- Notoriety exact line/session owner (`LOCAL-7-L`): `PlayerNotorietyBehavior.ConversationOutcomes.cs` embeds bounded `AFNR1` witnesses in the existing `_af_player_notoriety_state_v1` JSON, so aggregate state and witness share one owner/save value without adding a SyncData key. Exact detached lines bind opaque memory session, runtime/save generation and H recovery/payload/part identity; duplicate probes happen before active creation or roll. The first actual line publishes the frozen positive/negative roll witness; finalize uses an absolute target and readback before Applied. Open load becomes Unknown and never rerolls; Confirmed reconciliation sets only monotonic absolute fields. Legacy void ABI/default route remain, and legacy lines without exact turn/session identity remain non-recoverable.
- Remaining joint owner work: MyBehavior still needs live AFEF/old-save acceptance. K deliberately covers only Economy-only whole-plan exact outcomes; L deliberately covers only exact detached line/session identity. Live value/readback/save evidence, legacy/mixed/subset action semantics, legacy Notoriety lines, real MBRandom/session ending and corrupt old saves remain unverified. I closes the known Courier `afterCommit` session P1 for newly created receipts, but H→I intermediate saves without a Courier receipt cannot recover the old visible reply. Structured unknown receipts do not compensate gameplay effects; default cutover and destructive cleanup remain blocked.

## 首批切片候选

1. **Host 注册/调度只读分组设计**：先生成注册清单和阶段接口，不改变 `SubModule` 行为。
2. **Conversation target resolver contract**：统一三个入口的解析规则，保持现有 fallback 顺序并增加针对军团成员的纯测试。
3. **ActionPlan/Memory facade 设计**：先定义 DTO 和结果状态，不迁移玩法动作。
4. **Persistence key catalog**：只读生成 key/owner/type/chunk/legacy fallback 清单，不改变存档。

在构建依赖和仓库 gate 完成前，不执行大规模生产 C# 移动或删除。
