# AF 主体融合：SETS 城内夺城与贵族俘虏随行详细 HANDOFF

日期：2026-08-28
状态：设计与接线交接；尚未实施本文运行时重构
适用仓库：

- 独立、可复用 GCCZ/SETS 核心：`G:\AFMOD\GCCZ`
- 当前 AF 融合运行树：`G:\AFMOD\NEW-10`
- 上游 AF 参考树：`G:\AFMOD\Mount-Blade-Bannerlord-AnimusForge-mod`

## 0. 文档优先级与使用方式

本文是下一阶段“SETS 与贵族俘虏随行完全融入 AF 主体”的执行交接，重点补充架构、代码边界、迁移顺序、失败策略和验收门槛。

同时保留并阅读：

1. `G:\AFMOD\GCCZ\docs\handoff\sets-urban-capture-refactor-handoff-20260825.md`：旧玩法、数值和玩家体验契约。
2. `G:\AFMOD\GCCZ\docs\handoff\sets-urban-capture-refactor-progress-20260826.md`：Claude 已完成 Slice A/B 的真实进度和当时构建记录。
3. `G:\AFMOD\GCCZ\docs\bridge\noble-prisoner-escort-bridge-20260722.md`：贵族俘虏随行现有功能边界。

发生冲突时：

- 玩法数值和既有结果以 2026-08-25 契约为准；
- 当前代码事实以运行树和本次重新核查为准；
- 架构、风险修复和后续执行顺序以本文为准；
- 不得为了贴合旧文档而覆盖当前用户尚未提交的工作。

## 1. 最终目标与“完全融入”的定义

最终仍只发布一个 AF 模组和一个由 Bootstrap 按游戏版本加载的 `AnimusForge.dll` 实现。SETS、GCCZ、贵族随行不得增加独立运行 DLL、第二套模组入口或平行生命周期。

“完全融入 AF 主体”在本项目中定义为：

1. 由 AF 统一创建和结束 Mission Scope，而不是各功能根据全局变量反推当前场景。
2. 由 AF 统一登记场景参与者、角色能力和 Agent 所有权。
3. 由 AF 统一安装共享 Harmony 补丁；具体功能只返回决策。
4. 由 AF 统一路由场景对话动作和命令 UI。
5. SETS 敌对城镇/城堡夺城由一个状态机和一个幂等账本驱动。
6. 贵族随行由 AF Overlay Service 驱动，不被 SETS/GCCZ 当成士兵、守军或伤亡目标。
7. 所有 Bannerlord 对象查找、反射、Agent 生成、所有权修改和菜单打开仍留在 AF Runtime Adapter。
8. 可复用 SETS 规则继续在 `AnimusForge.SiegeAftermathIntervention` 保持纯 C#，并直接编译进 `AnimusForge.dll`；保留源码模块边界不等于保留独立运行时。

禁止把业务规则重新堆进以下巨型文件：

- `G:\AFMOD\NEW-10\ShoutBehavior.cs`
- `G:\AFMOD\NEW-10\MyBehavior.cs`
- `G:\AFMOD\NEW-10\SiegeAiInterventionBehavior.cs`

这些文件最终只保留薄入口、数据整理、兼容调用和真实游戏副作用。

## 2. 2026-08-28 仓库快照与保护事项

### 2.1 提交基线

| 仓库 | 分支 | 检查时 HEAD | 说明 |
|---|---|---:|---|
| `G:\AFMOD\GCCZ` | `codex/gccz-village` | `92fd44e` | SETS Slice A/B 已在历史提交中；当前另有未提交城镇语音相关工作 |
| `G:\AFMOD\NEW-10` | `main` | `09da37fe` | 当时与 `origin/main` 对齐；当前另有未提交融合工作 |

Claude 的 SETS 基础提交：

| 仓库 | 核心提交 | 进度文档提交 |
|---|---:|---:|
| GCCZ | `7052601` | `19d920b` |
| NEW-10 | `3a4c66d6` | `b3abd924` |

现有回滚标签：

- GCCZ：`backup/pre-sets-refactor-20260825` → `d048516`
- NEW-10：`backup/pre-sets-refactor-20260825` → `0ee4774f`

### 2.2 当前脏工作树

开始任何实现前必须重新执行 `git status --short`。本次检查发现下列改动已经存在，**不是本文工作的一部分，不得回滚、覆盖或混入提交**。

GCCZ：

```text
M  ModuleData/GcczTownPrompt.zh-CN.json
M  src/AnimusForge.SiegeAftermathIntervention/SiegeCastlePrisonerAllocationProfile.cs
M  src/AnimusForge.SiegeAftermathIntervention/SiegeRuntimePromptProfile.cs
M  src/AnimusForge.SiegeAftermathIntervention/TownPromptComposer.cs
M  src/AnimusForge.SiegeAftermathIntervention/TownPromptTextCatalog.cs
M  tests/AnimusForge.SiegeAftermathIntervention.Tests/Program.cs
?? src/AnimusForge.SiegeAftermathIntervention/TownOrdinarySpeakerVoiceSession.cs
```

NEW-10：

```text
M  AfGcczShoutBridge.cs
M  AnimusForge.SiegeAftermathIntervention/SiegeCastlePrisonerAllocationProfile.cs
M  AnimusForge.SiegeAftermathIntervention/SiegeRuntimePromptProfile.cs
M  AnimusForge.SiegeAftermathIntervention/TownPromptComposer.cs
M  AnimusForge.SiegeAftermathIntervention/TownPromptTextCatalog.cs
M  AnimusForge/ModuleData/GcczTownPrompt.zh-CN.json
M  ShoutBehavior.cs
M  SiegeAiInterventionBehavior.cs
?? AnimusForge.SiegeAftermathIntervention/TownOrdinarySpeakerVoiceSession.cs
```

实施者只能路径级暂存自己的文件，例如 `git add -- <explicit paths>`；禁止使用 `git add -A`。

### 2.3 双仓库镜像事实

当前以下 7 个文件在 GCCZ 与 NEW-10 的 SHA256 一致：

- `SetsUrbanCaptureState.cs`
- `SetsUrbanCaptureContext.cs`
- `SetsUrbanCaptureLedger.cs`
- `SetsUrbanCapturePolicy.cs`
- `SetsUrbanCaptureCompletionPlan.cs`
- `SetsUrbanCaptureSession.cs`
- `SetsUrbanCaptureContractProfile.cs`

不要再以“C# 文件总数”证明镜像完整；旧 HANDOFF 中 `169→176` 与实际提交树计数不一致。以后使用明确文件清单和逐文件 SHA256。

## 3. 已确认的当前运行状态

### 3.1 已经做到的

- `G:\AFMOD\NEW-10\AnimusForge.csproj` 的程序集名为 `AnimusForge`。
- `G:\AFMOD\NEW-10\SubModule.cs` 已注册：
  - `GcczSettlementCulturePersistenceBehavior`
  - `SiegeAiInterventionBehavior`
  - `VillageAftermathBehavior`
  - `SettlementEntryTroopSelectionBehavior`
  - `NoblePrisonerEscortBehavior`
- SETS Harmony 入口也从 `SubModule.cs` 注册。
- 因此 SETS 和贵族随行已经编译进同一 AF DLL，不存在独立 DLL 装载问题。
- Claude 新增的 SETS 核心有状态、上下文、账本、完成计划和约 100 项契约测试。

### 3.2 尚未做到的

- `SettlementEntryTroopSelectionBehavior.cs` 尚未创建或驱动 `SetsUrbanCaptureSession`。
- `SiegeAiInterventionBehavior.cs` 仍通过旧静态字段和布尔桥完成所有权、反射和菜单处理。
- 新账本没有拦截实际战役副作用。
- 贵族随行通过静态注册表和全局状态猜测 Mission Mode。
- SETS、贵族随行、GCCZ 对 Agent、命令 UI 和部分 Harmony 目标存在交叉管理。

所以当前只能说“同 DLL、已注册、基础核心可编译”，不能说“AF 主体融合完成”。

## 4. 必须冻结的玩家体验

除非用户后续明确批准，不调整：

- SETS 自有聚落最多 100 名普通随从、其他聚落最多 10 名；英雄排除。
- 随从生成延迟 0.75 秒，每批 10 名，批间隔 0.15 秒。
- 守军每波 30 名，三阶段，最多四个活跃波次，间隔 30 秒。
- 强制结束回退为 2 秒。
- 敌对 Town/Castle 胜利后进入原版 `menu_settlement_taken`，继续兼容 GCCZ 后续处置。
- 自有或统治者附属聚落事件不夺城。
- Village 继续走独立奖励路线。
- 原版宽恕、掠夺、毁灭以及 GCCZ 奖励、惩罚、关系和阈值保持不变。
- 贵族随行四个配置与上限保持：TownAftermath 5、SettlementEntry 5、LordsHall 5、WorldMapEncounterMeeting 1。
- 贵族随行仍使用第 8 编队语义；只有领主大厅允许非攻击型移动/布阵命令。
- 贵族处决仍要求 AI 输出标签、玩家本轮直接回应、目标仍是登记俘虏、原版确认界面四层成立。
- 贵族临时 Agent 的场景伤亡、撤退不得写回战役层死亡或受伤。

## 5. 当前关键缺陷

### 5.1 SETS P0 缺陷

| 编号 | 已确认问题 | 后果 | 修复原则 |
|---|---|---|---|
| S-01 | `SetsUrbanCapturePolicy` 的转换表不读取 Context | 敌对目标可误触 `TriggerOwnedIncident`；自有目标可误触 `StartConflict` | 敌对夺城 Session 只接受 Hostile Town/Castle；自有事件和村庄完全分路 |
| S-02 | `IncidentTriggered` 被混进 `ReachVictory`，但 `IsVictoryReady` / `ShouldBlockExit` 只认 `ConflictActive` | 直接接线会产生判胜和 TAB 语义冲突 | 不要简单把 `IncidentTriggered` 加进判胜；从敌对夺城状态机移除该分支 |
| S-03 | `CommitOwnership` 只要求 `AwaitingMap` 和 Hostile，不要求 Victory Ledger | 可经错误事件链绕过真实胜利 | 所有权资格必须同时要求 `VictoryReached` 来源和 `Ledger.VictoryCommitted` |
| S-04 | `OpenNativeMenu` 可从 `AwaitingMap` 直接进入 | 可能先开菜单后夺城 | 完成计划一次只返回一个动作，严格按所有权→原版上下文→菜单推进 |
| S-05 | `MatchesLiveState(..., ownershipAlreadyCommitted=true)` 只检查聚落 ID | 第三方已夺城仍可能继续旧结算 | Context 加 `PlayerClanId`；已提交时必须验证当前 owner 就是该 Clan |
| S-06 | State 与 Ledger 可独立恢复成不可能组合 | 读档后可能继续非法阶段 | 增加恢复校验器；非法组合进入 Suspended，绝不猜测成功 |
| S-07 | 当前完成桥吞掉部分反射/所有权失败，调用方只看到笼统 bool | 可能所有权失败但菜单继续，或重试重复副作用 | 每一步返回结构化结果；只在真实成功或可证明已生效后提交 Ledger |
| S-08 | Pending 胜利记录是静态对象且未 `SyncData` | 退出游戏或读档后流程丢失 | 新增版本化、稳定 ID 的 Pending Record；禁止序列化 Agent/Mission/Team |
| S-09 | 失败重试可能无限进行 | 每 Tick 重试、日志刷屏、状态悬挂 | 有界重试和退避；超过上限进入 Suspended，并给玩家安全提示 |
| S-10 | verifier 仍冻结旧布尔和旧桥签名 | 正确重构会被旧测试阻止 | 先改 verifier 为新不变量，再删除旧布尔路径 |

### 5.2 贵族随行 P0/P1 缺陷

| 编号 | 已确认问题 | 后果 | 修复原则 |
|---|---|---|---|
| N-01 | 每个 Mission 都先附加行为，再等待最多 8 秒猜 Mode | 无关场景也运行；可能读到迟到或错误全局状态 | Coordinator 明确发放 Mission Scope 和 Noble Profile |
| N-02 | 维护循环约每 0.5 秒重设 Team、Formation、无敌、AI Target | 已允许的决斗会被维护循环破坏 | 决斗/处决使用 Action Lease，租约期间暂停非战斗维护 |
| N-03 | 为每个随行俘虏扫描全部 Mission Agents 去重 | 复杂度约为 escorts×agents | Registry 使用 Agent/Hero 索引；仅在一次性接管时做受控扫描 |
| N-04 | 发现同 Hero Agent 时可能接管；晚到重复 Agent 可能直接淡出 | 可能删除原版或其他 AF 功能拥有的 Agent | 使用 Spawn Lease；未知 Owner 的 Agent 绝不删除 |
| N-05 | 接管现有 Agent 后没有完整恢复原状态 | 任务退出或功能关闭后残留 Team/Formation/AI 状态 | 接管时快照，只恢复本功能确实修改且仍匹配的字段 |
| N-06 | 任一 `OnMissionEnded` 可能清理全局注册 | 迟到的结束事件可能清掉新 Mission 的登记 | 所有清理必须匹配 MissionGeneration + OperationId |
| N-07 | 维护期间不持续验证俘虏仍属于玩家主队 | 释放、转移、死亡后仍在场景存在 | 低频重新验证；失效后仅清理自有 Agent或恢复接管 Agent |
| N-08 | 处决异步状态缺少完整 Mission 世代句柄 | 延迟 AI 回复或 AgentIndex 复用可能命中错误目标 | Execution Ticket 必须含 Generation、OperationId、HeroId、AgentIndex |
| N-09 | 贵族 Agent Origin 把 MainParty/玩家指挥语义暴露给其他功能 | 被误算为 SETS 士兵、胜利单位或伤亡对象 | 所有业务查询改用 Participant Role/Capability，不再推测 Origin |
| N-10 | 没有贵族随行专项自动化测试和 verifier | 回归只能靠实机发现 | 增加服务、票据、租约、注册表和静态契约测试 |

### 5.3 AF 共享入口缺陷

已确认存在重复或交叉处理：

- `BasicLeaveMissionLogic.OnEndMissionRequest`
- `MissionFightHandler.OnEndMissionRequest`
- `AgentNavigator.SetTarget`
- `UsableMissionObject.OnAIMoveToUse`
- `Mission.CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase`
- 原版 Order UI / `OrderController` / `OrderTroopPlacer`

SETS、SceneTaunt、SiegeAI、贵族随行现在通过多个 Prefix、静态查询和相互回调拼装结果。最终应做到：每个共享目标只有一个 AF 决策 Prefix；其他第三方 Mod 的补丁不属于本项目删除范围。

## 6. 目标架构

```text
SubModule
  ├─ AfMissionFeatureCoordinator
  │    ├─ immutable AfMissionScope
  │    ├─ one PrimaryFeature
  │    └─ optional overlays (NobleEscort)
  ├─ AfSceneParticipantRegistry
  │    ├─ owner / role / capabilities
  │    ├─ Agent + Hero indexes
  │    └─ SpawnLease / ActionLease
  ├─ AfMissionHarmonyDispatcher
  │    └─ one AF decision prefix per shared TaleWorlds target
  ├─ AfSceneDialogueFeatureBridge
  │    └─ provider-based prompt / tag / validation / memory routing
  ├─ AfCommandUiCoordinator
  │    └─ one order-controller decision from participant capabilities
  ├─ SETS runtime
  │    ├─ SettlementEntryTroopSelectionBehavior (campaign config + entry)
  │    ├─ SettlementEntryTroopSelectionMissionLogic (mission effects)
  │    ├─ SetsUrbanCaptureRuntimeAdapter (event translation)
  │    ├─ SetsUrbanCaptureCompletionCoordinator (map transaction)
  │    └─ NativeSiegeAftermathCompatAdapter (reflection boundary)
  └─ Noble runtime
       ├─ NoblePrisonerEscortBehavior (save/config)
       ├─ NoblePrisonerEscortService (eligibility/profile)
       ├─ NoblePrisonerEscortMissionBehavior (participant maintenance)
       └─ NoblePrisonerExecutionRuntime (validated action ticket)
```

## 7. AF 公共模型合同

### 7.1 `AfMissionScope`

建议放在 NEW-10 的小型 AF runtime 目录中，例如 `MissionFeatures/`，命名空间仍使用 `AnimusForge`。

必需字段：

```text
long MissionGeneration
string OperationId
AfSceneKind SceneKind
AfPrimaryMissionFeature PrimaryFeature
string SettlementId
string PlayerClanId
string OwnerClanIdAtOpen
DateTime/float CreatedAt（仅诊断，不作身份依据）
```

建议 `AfSceneKind` 至少覆盖：

- `SetsHostileTownEntry`
- `SetsHostileCastleEntry`
- `SetsOwnedIncident`
- `GcczTownAftermath`
- `GcczCastleAftermath`
- `VillageAftermath`
- `SettlementCenter`
- `LordsHall`
- `WorldMapMeeting`
- `Other`

硬性规则：

- 同一 Mission 只有一个 Primary Feature。
- 贵族随行是 `NonCombatOverlay`，不是 Primary Feature。
- 新 Mission 创建时 Generation 单调增加。
- 所有迟到回调、AI 回复和结束事件必须先验证 Generation。
- Mission Scope 不持久化；跨存档只保存 SETS 的 Campaign Pending Record。

### 7.2 `AfSceneParticipantRegistry`

建议登记结构：

```text
MissionGeneration
OperationId
FeatureOwner
AfSceneParticipantRole
AfSceneParticipantCapabilities
HeroId / CharacterId
AgentIndex
Agent reference（仅 Mission 内存）
AfSpawnOwnership: SpawnedByFeature | AdoptedExternal
CleanupPolicy
Optional restore snapshot
```

建议角色：

- `SelectedFollower`
- `EscortedNoblePrisoner`
- `GcczAlliedSoldier`
- `GcczSelectedPrisoner`
- `GatheredCivilian`
- `SettlementDefender`

建议能力分开表达，不要使用单个 `IsFriendly`：

- `CanFight`
- `CanReceiveMovementOrders`
- `CanReceiveOffensiveOrders`
- `CanDialogue`
- `CanDuel`
- `CanBeExecuted`
- `CountsForSetsVictory`
- `CountsForSetsCasualty`
- `CountsAsGcczSoldier`
- `ProtectedFromSetsDamage`
- `MustDespawnBeforeMeetingCombat`

注册表必须以 MissionGeneration 隔离，按 Agent 和 Hero 建立 O(1) 索引。不得在每帧或每个随行者维护周期全量扫描 `Mission.Agents`。

### 7.3 `AfSpawnLease`

- `SpawnedByFeature`：功能可以在匹配 Mission 结束时淡出或清理。
- `AdoptedExternal`：功能没有删除权；只能解除自己的登记并恢复自己改变的状态。
- 未知 Owner 的同 Hero Agent：报告冲突，默认复用但不改变危险状态，或放弃生成；绝不直接淡出。
- AgentIndex 只能和 MissionGeneration、Agent 引用共同使用，不能单独作为长期身份。

### 7.4 `AfParticipantActionLease`

决斗和处决需要显式租约：

1. 获取租约前重新验证 Scope、Participant、Hero 状态和当前动作。
2. 租约暂停贵族非战斗维护。
3. 快照本功能将修改的 Team、Formation、Invulnerable、AI Target/自动选敌和武器状态。
4. 动作结束、取消或 Mission 结束时幂等释放。
5. 仅当同一 Generation、同一 Agent 且字段仍属于本租约时恢复，避免覆盖其他系统的新状态。

## 8. SETS 详细设计

### 8.1 状态机只处理敌对 Town/Castle

当前 `SetsUrbanCaptureState/Event` 中尚未接入运行时的 owned/village 分支应在核心修正阶段删除，而不是继续扩展。

目标敌对流程：

```text
Inactive
  -> EntryPrepared
  -> MissionActive
  -> ConflictActive
  -> VictoryReached
  -> AwaitingMap
  -> OwnershipCommitted
  -> NativeMenuOpened
  -> Completed
```

`Abort` 可以从尚未完成的安全阶段回到 `Inactive`；一旦发生战役层副作用，不得用 Abort 假装回滚，只能进入恢复或 Suspended。

自有/附属聚落：继续使用独立 `SetsOwnedSettlementIncidentPlan` 或现有 Profile 驱动，不创建 `SetsUrbanCaptureSession`。
Village：继续由 `VillageAftermathBehavior` 与 `SetsVillageVictoryRewardProfile` 处理，不进入城市夺城 Session。

### 8.2 Context 修正

`SetsUrbanCaptureContext` 至少增加：

```text
PlayerClanId
ExpectedOwnerClanIdAtOpen
SchemaVersion（若与持久记录共用 DTO）
```

构造时直接拒绝：

- 非 Town/Castle；
- 非 Hostile；
- 空 OperationId、SettlementId 或 PlayerClanId；
- 玩家已拥有目标却尝试走敌对夺城。

恢复验证：

| Live 状态 | 处理 |
|---|---|
| settlement 不存在 | Abandon，无副作用 |
| owner 仍为 ExpectedOwnerClanId | 可从已验证阶段继续 |
| owner 已为 PlayerClanId 且 Victory 已提交 | 将所有权阶段协调为已完成，只继续后续步骤 |
| owner 为第三方 Clan | Suspended，禁止继续夺城和开菜单 |
| PlayerClan 不存在/变化 | Suspended，禁止猜测 |

### 8.3 Policy 与 Ledger 一致性

所有事件校验必须同时读取 Context、State 和 Ledger。至少满足：

- `StartConflict`：Hostile Town/Castle + `MissionActive`。
- `ReachVictory`：`ConflictActive` + 无存活目标 + Reserve Exhausted。
- `EndMission`：只有 `VictoryReached + VictoryCommitted` 才能进入 `AwaitingMap`；普通离开回到 Inactive。
- `CommitOwnership`：`AwaitingMap + VictoryCommitted + live world valid`。
- `OpenNativeMenu`：`OwnershipCommitted + OwnershipLedgerCommitted + native context prepared`。
- `Complete`：菜单确实已激活或明确由 GCCZ/native 接管。

增加 `ValidateRestoredCombination(...)`，拒绝例如：

- `OwnershipCommitted` 但 `VictoryCommitted=false`；
- `NativeMenuOpened` 但 `OwnershipCommitted=false`；
- `Completed` 但没有任何合法完成依据；
- Hostile Session 带 owned/village commit。

### 8.4 一次只计划一个副作用

当前 `SetsUrbanCaptureCompletionPlan` 可能同时返回 Transfer 和 OpenMenu。应改为单步计划：

```text
None
CommitOwnership
PrepareNativeAftermathContext
OpenNativeMenu
Complete
Suspend
```

每次 Pump：

1. 解析一个 Next Action。
2. Runtime Adapter 执行一个 Bannerlord 副作用。
3. 获得结构化结果。
4. 只有成功或可证明世界状态已经一致时，提交 Ledger 并推进 State。
5. 下一次 Pump 再解析下一动作。

禁止在一个大 `try` 中完成夺城、反射写字段、开菜单、清理全局状态。

建议结果结构：

```text
Succeeded
AlreadyApplied
Retryable
FailureCode
Diagnostic
```

### 8.5 Runtime 接线边界

`SettlementEntryTroopSelectionBehavior.cs` 最终只保留：

- 两个配置的 `SyncData`；
- Town/Castle/Village 入口路由；
- 创建 `AfMissionScope` 和敌对 SETS Session；
- Campaign Tick / Mission Started / Mission Ended 的薄转发；
- Pending Record 的持久化入口。

把嵌套 Mission Logic 移到：

- `G:\AFMOD\NEW-10\SettlementEntryTroopSelectionMissionLogic.cs`

新增：

- `G:\AFMOD\NEW-10\SetsUrbanCaptureRuntimeAdapter.cs`
- `G:\AFMOD\NEW-10\SetsUrbanCaptureCompletionCoordinator.cs`
- `G:\AFMOD\NEW-10\NativeSiegeAftermathCompatAdapter.cs`

Mission Logic 负责真实场景副作用：生成、队伍、波次、伤亡、命令、导航救援和击中事件；Runtime Adapter 把唯一事件送入 Session；Completion Coordinator 只处理 MapState 后事务。

接线点至少覆盖：

- Mission prepared → `PrepareEntry`
- `OnMissionStarted` → `StartMission`
- 第一次合法物理命中 → `StartConflict`
- 唯一伤亡回调 → Ledger casualty gate
- Reserve 从来源抽取 → Ledger reserve gate
- 胜利条件成立 → `TryCommitVictory` + `ReachVictory`
- 结束请求 → `ShouldBlockExit`
- Mission 结束 → `EndMission`
- MapState Pump → 单步完成计划

旧字段可以短暂作为观测镜像，但不得继续参与决策；完成影子比对后必须删除。

### 8.6 Pending Record

新增版本化持久记录，例如 `SetsPendingUrbanCaptureRecordV1`，只保存稳定值：

```text
SchemaVersion
OperationId
SettlementId
SceneKind
PlayerClanId
ExpectedOwnerClanIdAtOpen
PreviousOwnerClanId
State
VictoryCommitted
OwnershipCommitted
NativeContextPrepared
MenuCommitted
CompletionCommitted
SurvivingFollower character/count snapshot
RetryCount
LastFailureCode
```

禁止保存：

- `Agent`
- `Mission`
- `Team`
- `Formation`
- Mission 内 AgentIndex
- 未经 Bannerlord 保存兼容验证的复杂运行对象

建议自动重试只在 MapState 和合法菜单状态发生，使用有限次数与退避；达到上限后保留 Suspended 记录，显示一次安全提示并停止每 Tick 重试。不要自动清除已经发生所有权副作用的记录。

### 8.7 所有权和反射适配器

将所有 `SiegeAftermathCampaignBehavior` 私有字段访问集中到 `NativeSiegeAftermathCompatAdapter.cs`，启动时缓存并探测一次，不在热路径重复 `GetField`。

当前需要统一管理的字段至少包括：

- `_besiegerParty`
- `_prevSettlementOwnerClan`
- `_siegeEventPartyContributions`
- `_wasPlayerArmyMember`
- `_settlementProsperityCache`
- `_playerEncounterAftermathDamagedBuildings`
- `_playerEncounterAftermath`

适配器要求：

- 对 1.3 与 1.4 分别验证；
- 缺字段时返回明确失败，不吞异常后假装成功；
- 只熔断 SETS/native aftermath 兼容功能，不让整个 AnimusForge 启动失败；
- 日志只写一次兼容性摘要；
- verifier 禁止这些字段名出现在适配器之外。

所有权副作用继续复用现有 `SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs`，但它必须返回明确结果。不得在 SETS Behavior 内直接新增第二套 `ChangeOwnerOfSettlementAction`。

## 9. 贵族俘虏随行详细设计

### 9.1 归属

贵族随行主体只存在于 NEW-10 的 AF Host。GCCZ 不复制该功能源码，只保存桥接合同和本文 HANDOFF。城堡 GCCZ 原有战后检阅/俘虏选择继续由 GCCZ 管理。

### 9.2 Campaign 层

`NoblePrisonerEscortBehavior.cs` 最终只负责：

- 四个版本化 Profile 的 SyncData；
- U 键配置入口；
- 调用 `NoblePrisonerEscortService` 生成合法候选；
- 在 Coordinator 明确允许的 Scope 上申请 Overlay；
- Mission 开始/结束的薄生命周期转发。

新增 `NoblePrisonerEscortService.cs`：

- 读取配置快照；
- 验证 Hero 存活、仍为玩家主队俘虏、未逃脱/释放/转移；
- 根据明确 SceneKind 选择 Profile，不读取零散全局变量猜 Mode；
- 返回稳定 HeroId 列表，不直接生成 Agent。

### 9.3 Mission 层

`NoblePrisonerEscortMissionBehavior` 只在 Scope 已批准 Overlay 时附加。启动时获得不可变 Launch Request：

```text
MissionGeneration
OperationId
SceneKind
NoblePrisonerEscortMode
HeroIds
```

生成流程：

1. 逐个重新验证 Hero。
2. 查询 Participant Registry 是否已有本功能登记。
3. 若场景已有外部同 Hero Agent，申请 Adopt Lease；不能安全接管则跳过并记录。
4. 否则生成本功能 Agent，登记为 `SpawnedByFeature`。
5. 按场景赋予能力；贵族始终不参与 SETS/GCCZ 胜利和伤亡统计。

维护循环：

- 频率可继续保持约 0.5 秒，但只遍历本 Mission Registry 中的贵族记录。
- 每次验证 MissionGeneration、Hero 俘虏状态、Agent 有效性和 Action Lease。
- 有决斗/处决租约时不得重设 Team/Formation/Invulnerable/AI Target。
- 失效时只清理自己生成的 Agent；接管 Agent 解除登记并恢复自己的修改。

### 9.4 场景能力

| 场景 | 生成 | 战斗 | 移动命令 | 攻击命令 | 对话 | 处决 | 决斗 |
|---|---:|---:|---:|---:|---:|---:|---:|
| TownAftermath | 是 | 否 | 否 | 否 | 是 | 是 | 明确挑战后 |
| SettlementEntry | 是 | 否 | 否 | 否 | 是 | 是 | 明确挑战后 |
| LordsHall | 是 | 否 | 是 | 否 | 是 | 是 | 明确挑战后 |
| WorldMapEncounterMeeting | 最多 1 | 否；战斗前撤离 | 否 | 否 | 是 | 是 | 明确挑战后 |
| GCCZ Castle prisoner selection | 不重复生成 | 由 GCCZ 管理 | 由 GCCZ 管理 | 由 GCCZ 管理 | 由 GCCZ 管理 | 由 GCCZ 管理 | 由 GCCZ 管理 |

### 9.5 处决票据

`NoblePrisonerExecutionRuntime` 的 Pending 请求必须改为不可变 Ticket：

```text
Token
MissionGeneration
OperationId
HeroId
AgentIndex
ReplyTurnId / ConversationEpoch
ReplyIsDirectPlayerResponse
RequestedAt
```

打开原版确认前和玩家确认后都重新验证：

- Scope 仍相同；
- 当前 Agent 引用、Index、HeroId 和 Registry 记录一致；
- Hero 仍存活且仍被 MainParty 拘押；
- 标签来自玩家本轮直接回复；
- Ticket 未消费、未取消、未过期。

只有原版确认接受后才能调用原生 `KillCharacterAction`、移除 Profile 并处理会面升级战斗。取消确认必须无副作用并释放 Action Lease。

### 9.6 决斗

现有“玩家明确提出挑战、NPC 接受后才允许 `[ACTION:DUEL]`”规则保持。新增流程：

1. Dialogue Bridge 验证直接回复和当前贵族 Participant。
2. 获取 Duel Action Lease。
3. 贵族维护暂停。
4. 交给 AF 现有 Duel Runtime。
5. 胜负、取消、Agent 移除或 Mission 结束时释放租约。
6. 仅恢复同一 Generation 下仍属于租约的状态。

## 10. 跨功能参与者矩阵

| 角色 | Feature Owner | SETS 伤亡 | SETS 判胜 | GCCZ 士兵 | 非攻击移动命令 | 对话 | 处决 |
|---|---|---:|---:|---:|---:|---:|---:|
| SETS 选中随从 | SETS | 是 | 是 | 否 | 是 | 是 | 否 |
| SETS 聚落守军 | SETS | Defender Ledger | 是 | 否 | 否 | 现规则 | 否 |
| 贵族随行俘虏 | Noble Overlay | **否** | **否** | **否** | 仅 LordsHall | 是 | 是 |
| GCCZ 城镇盟军 | GCCZ | 否 | 否 | 是 | 是 | 现规则 | 否 |
| GCCZ 城堡选中俘虏 | GCCZ Castle | 否 | 否 | 否 | 现规则 | 现规则 | GCCZ 规则 |
| 聚集平民 | GCCZ/Scene | 否 | 否 | 否 | 否 | 是 | 否 |

任何旧代码如果继续使用 `BattleCombatant == MainParty`、`IsUnderPlayersCommand`、Hero 类型或 Team 作为业务身份，必须迁移为 Registry Capability 查询；引擎确实要求这些字段时可以设置，但不能再让其他 AF 功能据此推测角色。

## 11. Harmony Dispatcher 迁移

禁止一次性删除全部补丁。按两步迁移：

### 阶段 H1：统一决策，保留原安装点

- 新建 `AfMissionHarmonyDispatcher` 的纯决策方法。
- 现有 Prefix 先改为调用 Dispatcher。
- 在 Debug/Verbose 下记录旧结果与新结果；不改变玩家行为。
- 每个调用都带 MissionGeneration 和 Scope。

### 阶段 H2：统一安装

- 由 `SubModule` 只调用一次 Dispatcher 注册。
- 删除 SETS、SceneTaunt、SiegeAI 内对应重复 `harmony.Patch(...)`。
- 保留各 Feature 的决策 Provider，不保留第二条实际 Patch。
- verifier 对每个共享 MethodInfo 调用 `Harmony.GetPatchInfo`，断言 AF Harmony Owner 下只有一个决策 Prefix。

Dispatcher 决策优先级建议：

```text
无有效 Scope -> fall through vanilla
SETS Hostile Capture -> SETS provider
GCCZ Aftermath -> GCCZ provider
Owned Incident / Village -> 对应 provider
普通 AF SceneTaunt -> SceneTaunt provider
Noble Overlay -> 只贡献 Participant 保护/命令能力，不接管 Primary Feature
```

## 12. 对话与命令 UI

### 12.1 对话

新增 `AfSceneDialogueFeatureBridge` Provider 接口，统一：

- Prompt 片段；
- 当前回应者身份；
- 动作标签允许列表；
- 直接玩家回复验证；
- 后处理标签解析；
- 最终执行票据；
- 已发生事实/记忆写入。

贵族处决和决斗规则只在以下条件下注入：当前 Scope 有 Noble Overlay、当前 Agent 是 Registry 中的贵族参与者、渠道是有真实场景 Agent 的对话。不得把贵族场景标签扩散到信使或没有场景 Participant 的普通自由对话。

现有 `AfGcczShoutBridge` 应逐步成为 Provider；`ShoutBehavior.cs` 不再保存贵族/SETS 业务判断副本。

### 12.2 命令 UI

新增 `AfCommandUiCoordinator`，从 Scope 和 Participant Capability 统一解析：

- 当前 Player Team；
- `OrderController`；
- 可选择 Formation；
- 可用命令类别；
- 是否需要注入原版 Order UI。

贵族在 LordsHall 只允许移动、跟随、停止和布阵类命令；攻击、冲锋、开火等继续过滤。其他场景的贵族不应为了“显示第 8 编队”而开启整套战斗命令 UI。

## 13. 实施切片与提交计划

每个切片都必须在绿灯后再进入下一片，不做 Big Bang。

| 切片 | 仓库 | 代码工作 | 清理工作 | 退出条件 |
|---|---|---|---|---|
| A. 基线与新测试 | 双仓库 | 修正 SETS 测试预期；新增贵族/共享补丁 verifier 骨架 | 不改运行路径 | 旧行为测试全绿，新缺陷测试先红且原因明确 |
| B. AF Scope/Registry | NEW-10；GCCZ 记桥文档 | 新增 Coordinator、Scope、Participant Registry、Lease | 无关 Mission 不再附加贵族行为 | 双 API 构建；Scope 生命周期测试通过 |
| C. SETS 核心修正 | GCCZ 先行，NEW-10 镜像 | 敌对专用状态机、Context/PlayerClan、恢复校验、单步计划 | 删除未上线 owned/village 状态事件 | standalone 全绿，逐文件 SHA256 一致 |
| D. SETS Mission 接线 | NEW-10；GCCZ 更新桥/verifier | Session 成为唯一决策源；抽出 Mission Logic | 删除已替代布尔决策和重复 casualty set | 敌对完整流程静态/构建绿 |
| E. SETS 事务与兼容 | NEW-10；GCCZ 更新桥/verifier | Pending V1、Completion Coordinator、Native Compat Adapter | 删除旧 Pending Entry、分散反射和吞异常桥 | 故障注入和存读档模型测试通过 |
| F. Noble Service/Registry | NEW-10；GCCZ 更新桥文档 | Profile Service、Launch Request、Spawn Lease | 删除全 Mission 猜 Mode、全量重复扫描 | 四 Profile 和无关 Mission 测试通过 |
| G. Noble Action Lease | NEW-10；GCCZ 更新桥文档 | Duel/Execution Ticket、Action Lease、过期回调防护 | 删除旧全局 Pending 和不带世代清理 | 处决/决斗场景测试通过 |
| H. Dispatcher/UI/Dialogue | NEW-10；GCCZ 更新桥文档 | 统一 Harmony、命令 UI、场景对话 Provider | 删除重复 Patch 和跨模块静态查询 | 每个共享目标一个 AF Prefix |
| I. 资源与收尾 | 双仓库 | 移动本轮触及的 CJK 字符串、诊断摘要 | 删除死代码、旧桥、临时影子日志 | 全套测试、verifier、双 API、Bootstrap 绿 |

提交要求：

- GCCZ 核心改动先提交，再镜像到 NEW-10。
- AF-only 代码只提交 NEW-10，但必须在 GCCZ 的 bridge/handoff 中记录接口。
- 每个提交只暂存本切片文件，保护第 2.2 节已有用户改动。
- 开始代码前在两个仓库创建新的可回滚标签，例如 `backup/pre-af-sets-noble-integration-20260828`；如果同名已存在则不要移动标签。
- 禁止 hard reset、force push、删除历史备份；回退使用独立 revert 或恢复工作树副本。

## 14. 测试与验收矩阵

### 14.1 GCCZ standalone

必须覆盖：

- 敌对 Town/Castle 全部合法和非法转换；
- owned、attached、village 无法创建敌对 UrbanCapture Session；
- 没有 Victory Ledger 不能 CommitOwnership；
- 重复伤亡、Reserve、Victory、Ownership、Menu、Completion 只提交一次；
- 一次只产生一个 Completion Action；
- owner 仍为旧 owner、已为玩家、已为第三方三种恢复；
- State/Ledger 非法组合拒绝恢复；
- 旧存档没有新键等于没有 Pending，不推测胜利；
- 旧数值和路径快照完全不变。

### 14.2 NEW-10 单元/静态测试

新增可脱离游戏对象测试的接口或 fake adapter，覆盖：

- Scope 的创建、替换、迟到结束事件；
- Registry 的 AgentIndex 复用和 MissionGeneration 隔离；
- Spawned 与 Adopted 清理权限；
- Noble 中途释放、转移、死亡；
- Duel Lease 暂停和幂等恢复；
- Execution Ticket 过期、重复确认、目标变化；
- 无关 Mission 不附加 Noble MissionBehavior；
- 贵族不计入 SETS 胜利、伤亡、GCCZ 士兵；
- 一个共享方法只有一个 AF 决策 Prefix；
- 反射字段只存在于 Native Compat Adapter。

### 14.3 构建和工具

当前已知命令：

```powershell
$env:PATH = 'G:\AFMOD\.dotnet-sdk;' + $env:PATH
$env:DOTNET_CLI_HOME = 'G:\AFMOD\.dotnet-home'
$env:NUGET_PACKAGES = 'C:\Users\28358\.nuget\packages'

G:\AFMOD\.dotnet-sdk\dotnet.exe run `
  --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\AnimusForge.SiegeAftermathIntervention.Tests.csproj

powershell -NoProfile -ExecutionPolicy Bypass `
  -File G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1

Set-Location G:\AFMOD\NEW-10
powershell -NoProfile -ExecutionPolicy Bypass -Command "
  `$env:PATH = 'G:\AFMOD\.dotnet-sdk;' + `$env:PATH;
  `$env:DOTNET_CLI_HOME = 'G:\AFMOD\.dotnet-home';
  `$env:NUGET_PACKAGES = 'C:\Users\28358\.nuget\packages';
  & './一键编译覆盖推送/build_single_module.ps1' -ProjectRoot . `
    -BannerlordRoot 'E:\Steam\steamapps\common\Mount & Blade II Bannerlord' `
    -WorkshopContentDir 'E:\Steam\steamapps\workshop\content\261550' `
    -Configuration Debug -Stage
"
```

注意：

- 上述游戏路径是 2026-08-26 观察值，执行前重新确认。
- 不擅自修改现有一键编译/覆盖脚本。
- 使用 `-Stage`，不得在本任务中自动覆盖游戏目录。
- 1.3、1.4 和 Bootstrap 三项都通过才算构建绿。
- 构建成功只证明 API/编译兼容，不证明运行接线生效。

### 14.4 实机测试

SETS：

1. 敌对 Town：0、1、10 名随从。
2. 敌对 Castle：完整战斗、TAB 阻断、胜利、单次夺城、单次原版菜单。
3. 普通退出且未开战：不留下 Pending、不犯罪、不夺城。
4. 玩家自有 Town/Castle：只走事件菜单，不夺城。
5. RulerAttached：只走事件菜单，不夺城。
6. Village：只走村庄奖励，不进入 UrbanCapture。
7. 所有权已成功但菜单失败：重试只开菜单。
8. 反射字段缺失：功能安全熔断，不改所有权、不崩溃。
9. 保存/加载：Victory 后、Ownership 后、Menu 前分别验证。
10. 第三方在 Pending 期间获得聚落：Suspended，不继续旧结算。

贵族：

1. 四个 Profile 的 0/上限/超限和无效俘虏。
2. 同 Hero 已由原版或其他 AF 功能生成：不误删。
3. Mission 中途释放、转移、逃脱、死亡：安全移除或恢复。
4. LordsHall 第 8 编队：移动可用，攻击命令被拒绝。
5. 决斗开始后维护不覆盖 Team/无敌/AI；结束后正确恢复。
6. 处决接受、取消、重复点击、延迟 AI 回复、AgentIndex 复用。
7. WorldMapMeeting 升级战斗前贵族撤离，不计入胜负。
8. GCCZ Castle 俘虏选择存在时不重复生成贵族 Overlay。

共存：

- SETS + 5 贵族随行；
- GCCZ Town + 贵族随行；
- GCCZ Castle + 原有选中俘虏；
- 普通城镇、竞技场、藏身处、训练场、原版攻城、野战不受影响。

压力测试建议：100 名 SETS 随从、4×30 守军、5 名贵族，持续 10 分钟；观察帧时间、GC、日志量、重复 Agent、命令 UI 和 Mission 退出。

## 15. 性能与诊断预算

- Mission 热路径不得全量扫描 `Mission.Agents`；使用 Registry、HashSet 和事件更新。
- 反射信息启动时缓存一次。
- 贵族维护约 0.5 秒一次，只遍历已登记贵族，最多 5 个。
- SETS 波次/胜利统计使用增量集合，不每帧重算完整 roster。
- 默认日志只记录状态转换、一次性失败和结算摘要，不写每帧 Agent Dump。
- Verbose 可输出影子决策，但切换到正式路径后删除临时高频日志。
- 每条关键日志至少包含 `MissionGeneration`、`OperationId`、SettlementId、Feature、旧状态、新状态和 FailureCode。

## 16. 清理清单

最终必须删除或证明仍有调用者：

- 旧 SETS 决策布尔字段；
- `_settledCasualtyAgentIndexes` 与 `_settledDefenderReserveAgentIndexes` 的旧并行实现；
- `PendingSettlementVictoryMenuEntry` 旧静态路径；
- 分散在 `SiegeAiInterventionBehavior` 的 SETS native reflection；
- 重复 Harmony 安装代码；
- SETS、Noble、GCCZ 之间用于命令 UI 的直接静态互查；
- 贵族全 Mission 猜 Mode 和全量重复 Agent 扫描；
- 不带 MissionGeneration 的全局贵族清理；
- 影子迁移结束后的兼容日志、临时 Flag 和 fallback-only 分支；
- 新增代码触及范围内的 C# 硬编码中文文案。

必跑搜索：

```powershell
rg -n '<<<<<<<|=======|>>>>>>>' G:\AFMOD\GCCZ G:\AFMOD\NEW-10
rg -n 'TODO|HACK|TEMP|temporary|dead|unused|old path|fallback-only' <touched paths>
rg -n 'PendingSettlementVictoryMenuEntry|_settledCasualtyAgentIndexes|_settledDefenderReserveAgentIndexes' G:\AFMOD\NEW-10
rg -n 'GetField\("_(besiegerParty|prevSettlementOwnerClan|siegeEventPartyContributions|wasPlayerArmyMember|settlementProsperityCache|playerEncounterAftermathDamagedBuildings|playerEncounterAftermath)' G:\AFMOD\NEW-10
git -C G:\AFMOD\GCCZ diff --check
git -C G:\AFMOD\NEW-10 diff --check
```

## 17. 功能性增强优先级

不改变玩法数值的情况下，可以随主线实现：

| 优先级 | 功能 | 要求 |
|---|---|---|
| P1 | SETS Suspended 安全恢复提示 | 允许下一次加载/菜单手动重试或放弃；放弃不能撤销已发生副作用 |
| P1 | 功能级 Compatibility Circuit Breaker | SETS/Noble 单独熔断，AF 其他功能继续运行 |
| P1 | 贵族配置无效原因与清理 | 显示死亡、释放、转移、不可用，而不是静默不生成 |
| P1 | 构建标记 | 日志记录 Git SHA、dirty、核心哈希和兼容探测结果；不要擅改一键构建脚本 |
| P2 | 一键诊断包 | 收集版本、配置摘要、最近错误和状态，不收集用户私密 Prompt |
| P2 | 日志缓冲/限长 | 避免同步 `AppendAllText` 高频写盘造成卡顿 |
| P2 | 部署长期回滚包 | 真正部署任务中单独实现，本次不覆盖游戏目录 |

## 18. 明确不做

- 不重平衡 SETS、GCCZ 或贵族人数和战斗数值。
- 不把 owned incident 强行并入 hostile capture。
- 不简单通过把 `IncidentTriggered` 加进 `IsVictoryReady/ShouldBlockExit` 掩盖状态机问题。
- 不创建新的独立 DLL 或第二个 Bannerlord Module。
- 不修改 TaleWorlds 原版 DLL。
- 不把 GCCZ/SETS 业务规则复制进 `ShoutBehavior.cs`、`MyBehavior.cs` 或 `SiegeAiInterventionBehavior.cs`。
- 不修改现有一键编译/覆盖流程，除非用户另行明确要求。
- 不在未完成 1.3/1.4/Bootstrap 构建和备份前覆盖游戏目录。
- 不回滚第 2.2 节中已有的用户工作。

## 19. Definition of Done

只有全部满足才可宣称完成：

1. SETS Hostile Town/Castle 实际由 `SetsUrbanCaptureSession` 驱动。
2. 旧布尔状态不再参与决策，旧 Pending 静态路径已删除。
3. 所有权、菜单、奖励、伤亡和完成副作用均经过幂等账本。
4. Pending Record 可安全恢复，owner 变化会 fail closed。
5. Native reflection 只有一个适配器，缺字段不会崩溃或错误夺城。
6. 贵族 MissionBehavior 只在明确 Scope 附加。
7. 贵族使用 Registry/Lease，未知所有者 Agent 不会被删除。
8. 决斗和处决都使用带 Generation 的 Action Ticket/Lease。
9. 贵族不被 SETS/GCCZ 误算为战斗、胜利或伤亡参与者。
10. 每个共享 TaleWorlds 目标只有一个 AF 决策 Prefix。
11. 对话和命令 UI 通过 AF 公共协调器，不再由三套静态逻辑互查。
12. GCCZ 与 NEW-10 的 SETS 核心逐文件 SHA256 一致。
13. standalone、verifier、1.3、1.4、Bootstrap 全绿。
14. 关键实机场景完成并留下简洁可重放日志。
15. 未覆盖游戏目录；若后续部署，另做备份和回滚验证。

## 20. 下一位开发者的第一组具体动作

不要先移动 6,000 行 Mission Logic，也不要先改 `ShoutBehavior.cs`。

按以下顺序开始：

1. 重新读取两个仓库 `git status`，保护现有脏文件。
2. 创建双仓库备份标签，不移动既有标签。
3. 在 GCCZ 为 S-01～S-06 增加失败测试。
4. 将 `SetsUrbanCaptureSession` 收窄为 Hostile Town/Castle，加入 PlayerClanId 和恢复校验。
5. 将 Completion Plan 改为一次一个动作，并让全部 standalone 测试通过。
6. 镜像 7 个核心文件到 NEW-10，逐文件校验 SHA256。
7. 在 NEW-10 新增 `AfMissionScope`、Coordinator 和 Participant Registry 的最小实现；先不改变运行行为。
8. 让现有 SETS 入口创建 Scope/Session，并以影子日志比较旧布尔结果。
9. 只有影子测试一致后，逐个把 StartConflict、Victory、TAB、Casualty、Mission End 决策切换到 Session。
10. 每替换一个旧决策就删除一个旧字段/分支，并立即跑 standalone、verifier 和双 API 构建。

第一阶段交付物应是“小而绿的 SETS 核心修复 + AF Scope/Registry 基础”，而不是一次性完成所有融合。

## 21. 本 HANDOFF 生成时的验证边界

本文基于以下只读核查：

- 两仓库分支、HEAD、未提交文件和历史提交；
- 7 个 SETS 核心文件双仓库 SHA256 一致；
- `AnimusForge.csproj` 程序集名；
- `SubModule.cs` 的 SETS/GCCZ/Noble 注册；
- SETS 状态机、Context、Ledger、Completion Plan 当前源码；
- SETS、SiegeAI、SceneTaunt 的共享 Harmony 目标；
- Noble Profile、Mission attach、Agent 注册/维护、处决 Runtime 当前源码；
- 2026-08-25/26 SETS HANDOFF 和 noble bridge。

本次只写文档，没有实施运行时代码，也没有覆盖游戏目录。2026-08-26 的测试和构建绿灯是历史结果；当前脏工作树必须由实施者重新完整验证。
