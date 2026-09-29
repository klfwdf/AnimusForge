# 阶段 2：SubModule 注册与调度分组清单

- 状态：只读清单完成；未修改 `SubModule.cs`
- 日期：2026-08-29
- owner：Host/Composition
- 范围：生命周期、Harmony、CampaignGameStarter Model/Behavior、Mission adapter、ApplicationTick/EngineTick
- 约束：保留现有注册顺序、失败隔离、主线程边界、单一 `AnimusForge.dll` 和 1.3/1.4 兼容行为

## 1. 现状总链

```text
OnSubModuleLoad
  → SceneActionsIntegrationBoundary.InitializeRuntime
  → UIExtender.Create/Register/Enable

OnBeforeInitialModuleScreenSetAsRoot
  → 创建 Harmony("com.AnimusForge.spy")
  → 按现有顺序逐组 Patch；每组独立 try/catch

InitializeGameStarter
  → 先注册 Courier/Settlement models
  → 再按固定顺序 AddBehavior

OnBeforeMissionBehaviorInitialize / OnMissionBehaviorInitialize
  → SceneActionsIntegrationBoundary 注册并验证 MissionBehavior

OnApplicationTick
  → 快速 ApplicationTick 阶段
  → 详细性能记录模式下的同序 watched 阶段
  → 每帧异常由外层记录/结束 PerfProbe
```

`SubModule.cs` 仍是组合根，负责启动、Harmony、Model、CampaignBehavior、Mission 接入、Tick 和诊断协调。当前切片只形成可回退的分组目录，不创建新的运行时注册器。

## 2. 生命周期与外部集成

| 顺序 | 入口 | 当前动作 | 目标分组 | 保护要求 |
|---:|---|---|---|---|
| 1 | `OnInitialState` | 标记首次 API 引导提示待处理 | Host/Onboarding | 不影响游戏初始化 |
| 2 | `OnSubModuleLoad` | `SceneActionsIntegrationBoundary.InitializeRuntime()`；初始化 UIExtender，注册当前程序集并 Enable | External integration / UI | 外部 runtime 初始化失败需隔离并记录；只初始化一次 |
| 3 | `OnConfigChanged` | 刷新 SceneActions MCM 覆盖 | Config/External bridge | 不改变 CampaignBehavior 注册 |
| 4 | `OnBeforeMissionBehaviorInitialize` | `SceneActionsIntegrationBoundary.RegisterBeforeMissionInitialization(mission)` | Mission adapter | 由 adapter 检查重复行为 |
| 5 | `OnMissionBehaviorInitialize` | `SceneActionsIntegrationBoundary.VerifyMissionInitialization(mission)` | Mission validation | 只验证并记录，不由 Host 承担 SceneActions 业务 |
| 6 | `OnSubModuleUnloaded` | 关闭 SceneActions runtime，再调用 base | Shutdown | 关闭顺序需保持 |
| 7 | `OnBeforeInitialModuleScreenSetAsRoot` | 应用 Harmony patch；每个功能组独立异常边界 | Harmony composition | 不把 Patch 扫描改成无序全量扫描 |

## 3. Harmony 注册现状与分组

创建的 Harmony ID：`com.AnimusForge.spy`。

### 3.1 核心/入口/救援组（现有顺序）

| 顺序 | 当前入口 | 目标 owner |
|---:|---|---|
| 1 | `Patch_TriggerMassiveHook` | Compatibility/Safety |
| 2 | `Patch_GlobalUI_Click` | UI/GameAdapter |
| 3 | `AiErrorAnalysisInquiry.EnsurePatched` | Diagnostics/Conversation |
| 4 | `Patch_PlayerEncounter_Start` | Encounter/GameAdapter |
| 5 | `Patch_GameMenu_ActivateGameMenu` | UI/GameAdapter |
| 6 | `Patch_MenuHelper_EncounterAttackConsequence_RaidVillageRestore` | Encounter/World |
| 7 | `Patch_NpcSurrender_SkipCapturedLordConversation` + `Patch_NpcSurrender_SkipFreeOrCapturePrisonerHeroConversation` | Conversation/Prisoner |
| 8 | `Patch_PrisonBreakRescue_RecordSuccess` | Prisoner/Progression |

### 3.2 Siege/外交/会面组（现有顺序）

| 顺序 | 当前入口 | 目标 owner |
|---:|---|---|
| 9 | `SiegeAftermathPatchBootstrap.Apply` | Settlement/Siege bridge |
| 10 | `Patch_Meeting_SuppressDeclareWarAction` | Encounter/Diplomacy |
| 11 | `PermanentAllianceGuard.RegisterHarmonyPatches` | Diplomacy/Safety |
| 12 | `Patch_Vassalage_DeclareWarAction` | Vassalage/Diplomacy |
| 13 | `Patch_Vassalage_MakePeaceAction` | Vassalage/Diplomacy |
| 14 | `NpcTributeVassalageBehavior.RegisterHarmonyPatches` | Vassalage/Tribute |
| 15 | `Patch_Meeting_SuppressChangeRelationAction` | Encounter/Relationship |
| 16 | `Patch_Meeting_SuppressEncounterHostileAction` | Encounter/Safety |

### 3.3 输入/领域行为组（现有顺序）

| 顺序 | 当前入口 | 目标 owner |
|---:|---|---|
| 17 | `ShoutTextInputFocusChangePatch` | Conversation/UI |
| 18 | `Patch_PlayerKingdomNameChange_RecordMaterials` | Progression/Kingdom |
| 19 | `TroopInspectionBehavior.RegisterHarmonyPatches` | Mission/Prisoner |
| 20 | `SettlementEntryTroopSelectionBehavior.RegisterHarmonyPatches` | Settlement/Mission |
| 21 | `MilitaryExerciseBehavior.RegisterHarmonyPatches` | Mission/Training |
| 22 | `DuelBehavior.RegisterHarmonyPatches` | Duel/Mission |
| 23 | `CourierDeliveryBehavior.RegisterHarmonyPatches` | Courier |
| 24 | `WorldDiplomacyBehavior.RegisterHarmonyPatches` | World Diplomacy |
| 25 | `RewardSystemBehavior.RegisterHarmonyPatches` | Economy/Reward |
| 26 | `NobleGatheringBehavior.RegisterHarmonyPatches` | Social/World Event |
| 27 | `SexualConceptionBehavior.RegisterHarmonyPatches` | Social/Progression |

每组当前通过独立 `try/catch` 失败隔离。后续提取注册器时，必须保持上述相对顺序、每组的异常语义和日志可追踪性；不能把所有 Harmony 目标改成无序扫描。

## 4. CampaignGameStarter Model 注册

入口：`InitializeGameStarter(Game game, IGameStarter starterObject)`，仅在 `starterObject is CampaignGameStarter` 时执行。

当前 Model 注册顺序：

1. `RegisterCourierFoodConsumptionModel`
2. `RegisterCourierMobilePartyAiModel`
3. `RegisterAnimusForgeSettlementAccessModel`
4. `RegisterAnimusForgeSettlementLoyaltyModel`

Model helper 的共同模式：

- 遍历已有 `campaignGameStarter.Models` 查找原版 inner model；
- 排除已经是 AF wrapper 的实例，避免重复包裹；
- 找不到 inner model 时使用原版 Default model；
- 以 AF wrapper 重新 `AddModel<T>`；
- 异常记录后不让单个 Model 注册阻断整个组合根。

当前 Model owner：

| Model | Wrapper | owner |
|---|---|---|
| `MobilePartyFoodConsumptionModel` | `CourierFoodConsumptionModel` | Courier/GameAdapter |
| `MobilePartyAIModel` | `CourierMobilePartyAIModel` | Courier/GameAdapter |
| `SettlementAccessModel` | `AnimusForgeSettlementAccessModel` | Settlement/GameAdapter |
| `SettlementLoyaltyModel` | `AnimusForgeSettlementLoyaltyModel` | Settlement/GameAdapter |

## 5. CampaignBehavior 注册顺序

以下顺序来自 `SubModule.cs:637-671`，迁移前视为行为订阅和共享状态的兼容契约：

| 顺序 | CampaignBehavior | 目标 owner |
|---:|---|---|
| 1 | `ModOnboardingBehavior` | Host/Onboarding |
| 2 | `MyBehavior` | Conversation facade + Memory/Persistence |
| 3 | `KingdomStrategicProfileBehavior` | World Profile |
| 4 | `ShoutBehavior` | Conversation orchestration + Scene/Action adapter |
| 5 | `CourierDeliveryBehavior` | Courier + Conversation adapter |
| 6 | `DuelBehavior` | Duel/Mission |
| 7 | `RewardSystemBehavior` | Economy/Reward |
| 8 | `PlayerNotorietyBehavior` | Social/Progression |
| 9 | `AnimusForgeTerminalBehavior` | UI/Reports |
| 10 | `AnimusForgeUniqueCosmeticItemBehavior` | Progression/Items |
| 11 | `CustomPolicyBehavior` | Policy |
| 12 | `NpcRulerPolicyBehavior` | Policy/LLM |
| 13 | `AnimusForgeWorldEventBehavior` | World Events |
| 14 | `WorldMessageTimelineMenuBehavior` | UI/Reports |
| 15 | `RomanceSystemBehavior` | Social/Progression |
| 16 | `KnowledgeLibraryBehavior` | Knowledge |
| 17 | `LordEncounterBehavior` | Encounter/Conversation/Mission |
| 18 | `ProactiveNpcRequestBehavior` | Conversation/Proactive |
| 19 | `CompanionProactiveChatBehavior` | Conversation/Proactive |
| 20 | `SceneTauntBehavior` | Scene/Mission/Combat |
| 21 | `GcczSettlementCulturePersistenceBehavior` | Settlement/Siege |
| 22 | `SiegeAiInterventionBehavior` | Settlement/Siege |
| 23 | `VillageAftermathBehavior` | Settlement/Aftermath |
| 24 | `SettlementEntryTroopSelectionBehavior` | Settlement/Mission |
| 25 | `NoblePrisonerEscortBehavior` | Prisoner Logistics |
| 26 | `NoblePrisonerExecutionOrderBehavior` | Prisoner/Execution |
| 27 | `VoteDealBehavior` | Policy/Diplomacy |
| 28 | `WorldDiplomacyBehavior` | World Diplomacy |
| 29 | `DiplomacyBehavior` | World Diplomacy |
| 30 | `VanillaIssuePromptBehavior` | Conversation/Vanilla bridge |
| 31 | `WorldMapPartyCommandBehavior` | WorldMap |
| 32 | `NobleGatheringBehavior` | Social/World Event |
| 33 | `VassalageBehavior` | Vassalage |
| 34 | `NpcTributeVassalageBehavior` | Vassalage/Tribute |
| 35 | `KingdomAnnexationBehavior` | World/Kingdom |

特别约束：

- `MyBehavior` 在 `ShoutBehavior` 前注册；`ShoutBehavior` 通过其 facade 访问历史/记忆/AFEF。
- `CourierDeliveryBehavior` 紧随 `ShoutBehavior`，但其异步状态机不能并入一般场景喊话生命周期。
- Policy、World、Siege、Mission、Social 等行为虽然都在同一组合根注册，不能据此推断它们属于同一 owner。
- 迁移时先保持 `AddBehavior` 顺序，再逐组验证事件订阅、存档和 Tick 影响。

## 6. Mission adapter 注册

### Host 入口

- `OnBeforeMissionBehaviorInitialize` → `SceneActionsIntegrationBoundary.RegisterBeforeMissionInitialization(mission)`。
- `OnMissionBehaviorInitialize` → `SceneActionsIntegrationBoundary.VerifyMissionInitialization(mission)`。

### SceneActions adapter

`SceneActionsIntegrationBoundary` 负责按当前 Mission 缺失情况添加/验证：

- `SceneActionsMissionBehavior`；
- `BattleSpeechMissionBehavior`；
- `BattleSpeechPerformanceMissionBehavior`。

AF 侧保持薄 adapter，不把 SceneActions/BattleSpeech reusable runtime 的业务逻辑重新堆回 `SubModule.cs`。

### ShoutBehavior Mission 行为

在场景/任务启动路径中，`ShoutBehavior` 添加：

1. `ShoutMissionBehavior`；
2. `FloatingTextMissionView`；
3. `TownAmbientDialogueMissionBehavior`；
4. 条件性 `InterventionNativeTownCivilianPopulationMissionBehavior`。

同一路径还启动 RAG/semantic warmup、清理旧 scene runtime、重置 speech queue 和 scene history session。该部分属于 Scene/Conversation adapter，不应作为 Host 组合根的通用注册逻辑复制。

## 7. ApplicationTick / EngineTick 顺序

`OnApplicationTick(float dt)` 先调用 `RunFastApplicationTickPhases()`；启用详细诊断时改走 `RunWatchedApplicationTickPhases()`，后者保持对应工作项的既有顺序并增加 scope。

### 快速 ApplicationTick 顺序

1. `ShoutTextInputPopup.ProcessDeferredCloseIfNeeded`
2. `ShoutTextInputPopup.KeepMissionPausedIfOpen`
3. `DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded`
4. `PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded`
5. `PlayerRpForgePopup.ProcessDeferredCloseIfNeeded`
6. `PolicyEffectModuleManagerPopup.ProcessDeferredCloseIfNeeded`
7. `AnimusForgeConversationHistoryLogPopup.OnApplicationTick`
8. `AnimusForgeNativeConversationOverlay.OnApplicationTick`
9. `AiErrorAnalysisInquiry.OnApplicationTick`
10. `ShoutBehavior.OnApplicationTickForMainThreadActionsExternal`
11. `NativeConversationAnswerAreaController.OnApplicationTick`
12. `ShoutBehavior.OnApplicationTickForNativeConversationTtsExternal`
13. `ConversationHelper.Tick`
14. 首次 API guide notice 处理
15. `Logger.OnApplicationTick`
16. `BannerlordExceptionSentinel.OnApplicationTick`
17. `McmDropdownRuntimeRefresh.OnApplicationTick`
18. `EncyclopediaHeroPersonaPatch.OnApplicationTick`
19. `EncyclopediaTownRuleMemoryPatch.OnApplicationTick`
20. `SiegeAiInterventionBehavior.OnEngineTickForExternal`
21. `ModOnboardingBehavior.Instance?.OnEngineTick()`
22. `MyBehavior.Instance?.OnEngineTick()`
23. `CourierDeliveryBehavior.Instance?.OnEngineTick()`
24. `DuelBehavior.Instance?.OnEngineTick()`
25. `RewardSystemBehavior.Instance?.OnEngineTick()`
26. `LordEncounterBehavior.OnEngineTick()`
27. `AnimusForgeTerminalBehavior.Instance?.OnEngineTick()`
28. `CustomPolicyBehavior.Instance?.OnEngineTick()`
29. `NpcRulerPolicyBehavior.Instance?.OnEngineTick()`
30. `WorldDiplomacyBehavior.Instance?.OnEngineTick()`
31. `PolicySystemUi.OnApplicationTick()`
32. `NobleGatheringBehavior.Instance?.OnEngineTick()`
33. `VassalageBehavior.Instance?.OnEngineTick()`

### 详细诊断模式

`RunWatchedApplicationTickPhases()` 对上面的 UI、Conversation、main-thread action、TTS、diagnostics、Scene/Siege、Campaign behavior 和 Policy 项目使用 `RunWatchedTickPhase(name, action)`，保持同一业务顺序并记录 scope。详细列表位于 `SubModule.cs:804-837`。

### Tick 分组风险

- Application Tick、Mission Tick、Campaign hourly/daily、EngineTick 不是同一调度面，不能合并成一个泛化 tick。
- `ShoutBehavior` 的 main-thread action drain、Native TTS 和 Mission behavior tick 必须保持主线程。
- `MyBehavior` 的 memory/save maintenance 不能未经预算直接加入每帧热路径。
- 任何分组抽取都必须保持 deferred close、输入焦点、TTS、Conversation history 和 diagnostics 的相对顺序。

## 8. 推荐的后续提取顺序（仍不改代码）

1. 先把本清单固化为 Host/Composition 的注册证据。
2. 设计只读 registry DTO：注册名、owner、阶段、顺序、失败策略、主线程要求、存档/渠道影响；不让 DTO 持有 Behavior 实例或 raw dictionary。
3. 将 Model 注册、Behavior 注册、Harmony 注册、Mission adapter、ApplicationTick、EngineTick 作为不同 contribution group；不要做一个“大一统注册器”。
4. 对每组建立 no-op/failure-isolation 设计，再考虑最小可回退桥接。
5. 第一条实际代码切片必须保留旧 `SubModule` facade，并分别验证 1.3、1.4、Bootstrap、存档和三渠道。

## 9. 本切片边界

- 只读取 `SubModule.cs`、`ShoutBehavior.cs`、`SceneActionsIntegrationBoundary.cs` 和现有 owner/refactor 文档。
- 未修改生产 C#、项目文件、脚本、`.gitignore`、`SubModule.xml` 或游戏目录。
- 未改变任何注册顺序、Harmony 行为、Tick 频率、队列上限或线程边界。
- 运行频率：0；本文件不产生运行时成本。
