# AnimusForge 重构地图（第一版）

> 这是基于 `d4cb1467` 的只读结构盘点结果。它是重构导航图，不是立即移动文件的授权；当前先保持单一 `AnimusForge.dll`，按逻辑模块逐步整理。

## 1. 当前运行链

```text
AnimusForge/SubModule.xml
    ↓ 只声明
AnimusForge.Bootstrap.dll
    ↓ 检测 Bannerlord API 线并选择一个实现
versions/1.3/AnimusForge.dll 或 versions/1.4/AnimusForge.dll
    ↓ 生命周期转发
SubModule.cs
    ├─ CampaignBehavior / 模型注册
    ├─ Harmony 注册
    ├─ MissionBehavior 接入
    ├─ 每帧与延迟任务调度
    └─ 配置、诊断、关闭协调
```

### 已有的良好边界

- `AnimusForge.Bootstrap/*`：版本选择、程序集加载、生命周期转发；应保持最小稳定。
- `AnimusForge.SiegeAftermathIntervention/*`：可复用 GCCZ/攻城后规则和 profile；AF 根目录代码尽量作为薄适配器。
- `SceneActionsIntegrationBoundary.cs`：外部 SceneActions/BattleSpeech 的初始化、MCM 覆盖、MissionBehavior 注册和验证；是现有薄适配器范例。
- `tools/PlayerExportsEditor/*`：外部编辑器，不编译进主运行程序集。

## 2. 当前最大耦合中心

### `SubModule.cs` — 组合根瓶颈

同时承担启动/关闭、Harmony 注册、CampaignBehavior 注册、模型注册、Mission 接入、每帧调度和全局诊断。重构方向是把注册表和调度分组提取为 Host/Composition 组件，但第一步必须保持现有顺序和异常隔离。

### `MyBehavior.cs` — 记忆与持久化中心

包含对话历史、每日/压缩记忆、记忆概览、NPC 行为摘要、Persona、AFEF、事件数据、稳定度/周报、语音映射、非英雄记忆、外部 Facade 和大型 `SyncData`。不要整体搬家；按持久化域逐步提取服务，并保留旧 Facade。

### `ShoutBehavior.cs` — 交互编排中心

包含场景喊话、Native Conversation、Prompt 组装、后处理、动作标签、TTS、Mission、目标解析、历史和延迟动作。场景喊话目前是三渠道参考实现；先抽公共契约和目标解析，再拆内部责任。

### `CourierDeliveryBehavior.cs` — 异步状态机中心

包含信使路线、信件、回复、后处理、送达/返回时动作、UI、队伍导航和持久化。应共享 Conversation/Memory/Action 契约，但保留“送达/返回时执行”的渠道时序。

## 3. 目标逻辑模块与当前候选 owner

| 目标模块 | 当前主要 owner/入口 | 主要职责 | 主要风险 |
|---|---|---|---|
| Bootstrap | `AnimusForge.Bootstrap/*`, `AnimusForge/SubModule.xml` | API 线检测、单实现加载、生命周期转发 | 加载两套实现、版本误判 |
| Host/Composition | `SubModule.cs`, `CompatibilityAudit.cs`, `Logger.cs`, `PerfProbe.cs`, `FreezeWatchdog.cs` | 启动组合、注册表、调度、诊断 | 注册顺序和失败隔离变化 |
| Contracts | 当前尚未集中存在；由外部 Facade/模型逐步归纳 | DTO、能力、事件、结果和版本 | 过早暴露私有类或 raw dictionary |
| Foundation/Runtime | `Logger.cs`, `PerfProbe.cs`, `FreezeWatchdog.cs`, `BannerlordExceptionSentinel.cs`, `SaveRuntimeGuard.cs` | 主线程、后台队列、缓存、generation、SafeMode、诊断 | 热路径分配、线程越界、静默失败 |
| GameAdapter/Compatibility | 各类 `*SafePatch`, `CompatibilityAudit.cs`, Bannerlord API helpers | TaleWorlds/Harmony/1.3-1.4 适配 | API 漂移、反射和 Patch 失败 |
| Persistence | `CampaignSaveChunkHelper.cs`, 各行为 `SyncData` | 存档 key、chunk、迁移、namespace | 35 个 SyncData owner、key 冲突、部分迁移 |
| Conversation/AI | `MyBehavior.cs`, `ShoutBehavior.cs`, `CourierDeliveryBehavior.cs`, `LordEncounterBehavior.cs`, conversation patches | 三渠道交互、目标、规则资格、LLM 编排 | 渠道漂移、会面目标错配 |
| Memory/AFEF | `MyBehavior.cs`, `AIConfigHandler.cs` 的 AFEF normalization | 历史、记忆、事实、角色语义 | 运行时/存储镜像不一致、事实误写 |
| Prompt/Rule | `AIConfigHandler.cs`, `AnimusForge/ModuleData/*.json`, `ShoutBehavior.cs` runtime rule composition | preprocess、主 Prompt、postprocess rules | JSON 与 C# parser/executor 漂移 |
| Action | `ShoutBehavior.cs`, `AIConfigHandler.cs`, 各领域 handler | 标签解析、授权、当前状态验证、执行一次、结果事实 | LLM 结果过期、重复执行、可见标签泄露 |
| Policy | `PolicySystem/**`, `VoteDealBehavior.cs` | 政策、投票、效果、NPC policy、授权 | record/effect/registry 跨 key 一致性 |
| Economy/Reward/Debt | `RewardSystemBehavior.cs` partials, `DebtPromiseQuest.cs` | 金币、物品、交易、奖励、债务、信任 | 混合 v1/v2 key、动作与事实不一致 |
| World Simulation | `DiplomacyBehavior.cs`, `WorldDiplomacyBehavior.cs`, `VassalageBehavior.cs`, `KingdomAnnexationBehavior.cs`, `WorldMapPartyCommandBehavior.cs`, `WorldEvents/*` | 外交、战争/和平、附庸、兼并、世界事件、大地图命令 | 高耦合 campaign tick、对象生命周期 |
| Settlement/Siege | `SiegeAiInterventionBehavior.cs`, `VillageAftermathBehavior.cs`, `AnimusForge.SiegeAftermathIntervention/*`, `CastleAftermath*` | 村庄/城镇/城堡后果、人口、文化、攻城后处理 | GCCZ/AF 双边界和 Mission 生命周期 |
| Mission/Combat | `DuelBehavior.cs`, `SceneTauntBehavior.cs`, `MilitaryExerciseBehavior.cs`, `TroopInspectionBehavior.cs`, prisoner mission behaviors | Mission、决斗、训练、检查、囚犯、Agent 状态 | 主线程、死亡/战斗上下文、原版战斗误触 |
| Scene | `ShoutBehavior.cs`, `TownAmbientDialogueBehavior.cs`, `SceneTauntBehavior.cs`, SceneActions adapter | Agent/LocationCharacter、喊话、带路、传唤、跟随、场景冲突 | 不能用裸坐标替代 Agent；和平/战斗上下文误判 |
| Courier | `CourierDeliveryBehavior.cs`, courier UI/patch files | 信使队伍、信件、回复、送达/返回动作 | 异步状态机、save/load、重复/丢失执行 |
| Duel | `DuelBehavior.cs`, `FourberieDuelCompatibility.cs` | 决斗资格、任务、死亡、结果 | Mission/Conversation/Action 多依赖 |
| Progression/Social | `PlayerNotorietyBehavior.cs`, `RomanceSystemBehavior.cs`, `SexualConceptionBehavior.cs`, `RewardSystemBehavior.cs`, cosmetic behavior | 声望、关系、恋爱、家庭、RP、物品 | 社交结果与记忆/存档分散 |
| Knowledge/Profile | `KnowledgeLibraryBehavior.cs`, `KingdomStrategicProfileBehavior.cs`, `ShoutUtils.cs`, `WorldEntityRetrievalService.cs` | 知识、RAG、Persona、王国 profile、导入导出 | 文件、runtime、save 三套 authority |
| UI/Diagnostics | Popups/VM、Native overlay、百科 patch、MCM refresh、error reporting | UI、输入焦点、TTS、诊断展示 | UI 必须主线程；原版快捷键抢事件 |
| External Tools | `tools/PlayerExportsEditor`, Prompt Labs, contract/smoke tests | 编辑、静态验证、纯测试、导出 | 不应混入 runtime；输出和私密配置隔离 |
| Bridges | `AfGcczShoutBridge.cs`, `CastleAftermath*Bridge.cs`, town/scene bridges | 仅协调多个 owner 的公开能力 | 无 owner、隐式耦合、持久化 namespace 冲突 |

## 4. 持久化地图

### Campaign Save

当前约有 35 个 AF 侧 `SyncData` 实现。主要集中在：

- `MyBehavior.cs`：`_dialogueHistory_v2`、每日/压缩记忆、队列、NPC 行为、Persona、事件、稳定度、语音、非英雄记忆等。
- `CourierDeliveryBehavior.cs`：信使 session、外交信件、letter inventory。
- `PolicySystem/Core/CustomPolicyBehavior.Lifecycle.cs` 与 `PolicySystem/Npc/NpcRulerPolicyBehavior.cs`：政策记录、效果、registry、NPC policy。
- `RewardSystemBehavior.cs`：债务、奖励、生成物品、信任和恢复数据。
- `SiegeAiInterventionBehavior.cs`、`VassalageBehavior.cs`、`WorldMapPartyCommandBehavior.cs`、`DuelBehavior.cs` 等：领域状态。

迁移红线：

1. 不复用已有 key 代表不同类型。
2. 新格式使用新版本 key，保留旧 key 读取和迁移。
3. 现有 chunk 协议必须可读：inline key、`__af_chunk_count`、`__af_chunk_` 和 dictionary synthetic keys。
4. 跨 key 有引用关系的域必须一起迁移，例如 MyBehavior memory/event、Policy record/effect/registry、Reward debt/generated items。
5. 临时任务、缓存、异步 handle 与确认事实必须分开处理；不能把运行时任务当永久事实恢复。

### ModuleData / PlayerExports / Runtime

```text
ModuleData       = 随模块发布的静态规则/Prompt/XML/默认配置
PlayerExports    = 用户可编辑的文件包和导入/导出边界
Campaign Save    = 当前战役的持久化状态
Runtime Cache    = 不应直接作为存档事实的临时状态
```

目前知识、语音映射、未命名 Persona、事件数据和王国 profile 在这些平面之间存在多个 authority。重构前要建立明确的优先级；不能假定改 ModuleData 会自动更新旧存档。

## 5. 交互管线地图

目标共享管线：

```text
渠道适配器
→ 主线程捕获 immutable GameInteractionSnapshot
→ Eligibility / RuleSelection / CapabilitySet
→ PromptComposer
→ 后台 LlmGateway
→ VisibleReplyNormalizer
→ ActionPostprocessor / ActionPlan
→ 主线程重新验证目标并执行一次
→ Memory / AFEF 事实
```

当前状态：

- 场景喊话是最完整的参考路径。
- Native Conversation 复用场景 Prompt，但有静态 session history、UI 和目标解析层。
- Courier 复用 preprocess、memory 和 auxiliary postprocess，但有独立消息编排、状态机和送达/返回时机。
- 三个会面入口的目标解析逻辑重复，应最终统一 `ConversationTargetResolver`。

## 6. 重构顺序

```text
0. 基线、仓库分类、依赖和存档保护
1. Host/Composition：按组整理 SubModule 注册/调度，保持顺序
2. Contracts：快照、能力、结果、模块生命周期
3. Foundation/GameAdapter：主线程、后台、诊断、兼容边界
4. Persistence：key registry、namespace、chunk/migration contract
5. ConversationTargetResolver：统一会面目标解析
6. Conversation/Memory/Prompt：先公共 seam，保留旧 facade
7. Action：建立规则→解析→授权→执行→事实闭环
8. Policy、Economy/Reward/Debt 等已有边界
9. Courier、Duel、WorldMap、Scene
10. Diplomacy、Settlement、Siege
11. Knowledge、Profile、UI、PlayerExports
12. 仅在有真实跨 owner 需求时建立 Bridge
13. 删除已被证据替代的旧 facade/God Object
14. 双 API 线、stage/package、旧存档和游戏内最终验收
```

每一步都保持单一 `AnimusForge.dll`，旧入口先做代理；每个切片必须可回退，并记录 1.3/1.4、存档、频道、profile、线程和性能影响。

## 7. 当前阻塞与未验证项

- 目标 API 线是 Bannerlord `1.4.x`，当前用户安装版本实际为 `v1.4.7.117484`；开发者可以使用不同 1.4.x 补丁，但必须记录精确引用版本。
- 当前统一构建已确认 1.3 `v1.3.15.110062` 和本机 1.4 游戏版本，但被外部依赖路径阻塞：实际模块目录缺少默认位置的 `0Harmony.dll`、`MCMv5.dll` 等；项目 `.tmp/build_check/1.4` 有验证副本，但尚未完成合法依赖闭包的正式构建。
- 尚未完成代表性旧存档加载和游戏内基线；用户已确认有存档，后续只需备份一个代表性副本并记录最小关键场景，不要求全量回归。
- 尚未把本地图扩展成逐文件 owner matrix；本文件先作为第一版导航，后续按模块逐项补证据。
