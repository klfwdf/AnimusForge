# 阶段 3：AF.Contracts Capability / Event / DTO / Contract-Version 设计

- 状态：设计完成；未创建 `AF.Contracts` 生产项目；未修改生产 C#
- 日期：2026-08-29
- owner：Foundation/Host/Composition；各独立 consumer 共同审阅
- 依据：`extensions/AnimusForge.XihaiAction/src/CoreProject/Contracts.cs`、`SceneActionFrameworkV1.cs`–`SceneActionFrameworkV4.cs`、`PolicySystem/Effects/PolicyEffectModuleContracts.cs`、`PolicySystem/Effects/PolicyEffectDataContracts.cs`
- fixture：`F:\AnimusForge-main\docs\fixtures\phase3-af-contracts\`
- runner：`F:\AnimusForge-main\tools\AFContractsContractTests\validate_af_contracts.py`

> 本文只定义未来 `AF.Contracts` 的最小公共边界。当前 `AnimusForge.dll`、Bootstrap、旧 facade、PolicySystem、SceneActions、Conversation、存档和三渠道行为不变。

## 1. 设计原则

1. 只有当前存在两个独立 consumer 的稳定能力才进入 `AF.Contracts`；单模块 helper 留在模块内部。
2. Contract 只包含 immutable、可序列化、可脱离 Bannerlord live object 表达的 DTO、typed event 和 capability metadata。
3. 不公开 `Behavior`、静态字段、Harmony target、UI VM、raw save dictionary、`JToken`/`object`/`dynamic` 执行载荷、delegate 或 `MethodInfo`。
4. Calls/queries 使用 capability service；通知使用 typed event；不创建通用 middleware/event bus 来隐式改变执行顺序。
5. DTO 的稳定 ID、枚举值、字段语义和 contract version 独立于程序集版本、游戏版本、save schema 和 Prompt 版本。
6. 生产实现必须先保留旧入口作为 facade；本设计通过不接入新 contract 即可回滚。
7. 1.3/1.4 API 差异留在 GameAdapter/implementation；公共 contract 不引用 TaleWorlds 类型。

## 2. Contract version 规则

### 2.1 版本层级

| 层级 | 示例 | 作用 | 变更要求 |
|---|---|---|---|
| Catalog schema | `1` | 描述 catalog 自身字段 | 字段语义变化递增 |
| Contract version | `1` | 单个 capability/event/DTO 的语义版本 | 破坏性字段/语义变化递增 major 语义；当前以整数表示 |
| Payload schema | `1` | 模块私有 payload | 由模块 owner 管理 migration |
| Runtime/save schema | `1` | 运行时/存档数据 | 由 persistence owner 管理；不等同 contract version |
| Assembly/game version | `AnimusForge`、`1.3/1.4` | 编译/发布身份 | 不用于替代 contract negotiation |

### 2.2 兼容规则

- 同一个 contract ID 不允许两个不兼容的语义版本同时作为 active provider；
- consumer 声明 `minVersion`/`maxVersion`，provider 声明实际 `version`；
- 缺失或不兼容的 required contract → `Blocked`；
- optional contract 缺失 → `Degraded`，必须给出明确 fallback；
- 未知字段可以被忽略的前提是 owner 在 contract 规则中声明 forward-compatible；
- 删除、改名、改变 role/target/fact 语义或改变存档身份必须新版本并有 migration/回滚证据；
- contract version 不能改变 `AnimusForge` 程序集身份、SyncData key 或存档类型。

## 3. Capability contract

### 3.1 `CapabilityReference`

```text
CapabilityReference
  CapabilityId: stable string
  Version: positive integer
  ContractVersion: positive integer
  ProviderModuleId: stable module ID
  Scope: stable scope string
  Profile: stable profile ID
```

`ProviderModuleId` 只描述 catalog 身份，不是 provider 实例；consumer 只依赖 capability ID/version，不导入 provider 私有实现。

### 3.2 `RequiredCapability`

```text
RequiredCapability
  CapabilityId
  MinVersion
  MaxVersion
  Required: true | false
  FallbackId: explicit fallback or empty
```

required/optional 语义必须由 manifest/profile 固化，不能藏在执行方法里。fallback 不能改变模块的 save identity，也不能把 optional 缺失伪装为 Active。

### 3.3 当前候选 capability

| Capability | provider 候选 | consumer 候选 | 类型 |
|---|---|---|---|
| `main-thread.dispatch` | Foundation | Conversation、Policy、Settlement、UI | service |
| `persistence.facade` | Foundation/Persistence owner | 有存档需求的 module | service |
| `conversation.action-authority` | Conversation/Action owner | Scene/Siege、Policy/Diplomacy、Courier | service |
| `settlement.siege.actions` | Settlement/Siege | Conversation/Scene adapter | service |
| `policy.effect.execution` | PolicySystem | Policy/Diplomacy Bridge | service |
| `world.diplomacy.events` | WorldDiplomacy | Policy/Diplomacy Bridge、Reports | typed event |
| `diagnostics.health` | Foundation/Diagnostics | Host、UI、developer profile | service |

这些是候选公共能力，不代表当前代码已经注册、解析或加载它们。

## 4. Typed event contract

### 4.1 共同 envelope

```text
AfEventEnvelope
  EventId: stable event kind ID
  EventVersion: positive integer
  Sequence: monotonic source sequence
  SourceModuleId: stable module ID
  SourceChannel: SceneShout | NativeConversation | Courier | World | Policy | Mission | System
  RuntimeGeneration: positive generation
  OccurredAt: bounded campaign time representation
  TraceId: bounded diagnostic ID
```

事件 payload 必须 immutable；consumer 不能原地修改事件或用事件回写 provider 的私有状态。`Sequence`/`RuntimeGeneration` 用于去重和 stale 防护，不是存档 identity。

### 4.2 最小事件集合

#### `ActionExecutionCompleted`

- `ActionId`；
- `TargetIdentity`（稳定 ID/AgentIndex 元数据，不是 live Agent）；
- `Status`：`Applied` / `Rejected` / `Expired` / `Unsupported` / `Failed`；
- `ReasonCode`；
- `ConfirmedFacts`；
- `MainThreadApplied`；
- `ReceiptId`。

只有 `Applied` 且 `MainThreadApplied=true` 才能产生 confirmed fact；LLM 文本或标签本身不是事实。

#### `MemoryFactRecorded`

- `MemoryId`；
- `Role`：`user` / `assistant` / `system-fact`；
- `FactKind`；
- `Confirmed`；
- `SourceActionId`/`SourceChannel`；
- `Text`（有界可见文本，不能包含 API key/raw response）。

#### `WorldDiplomacyResolved`

- `OutcomeId`；
- `InitiatorId`；
- `TargetId`；
- `OutcomeKind`；
- `PolicyReceiptId`（可选）；
- `ChangedDiplomaticState`；
- `MechanicalResult`（有界结果摘要）；
- `Confirmed`。

只有真实原版外交/Policy receipt 确认后才允许 `Confirmed=true`；Bridge 失败不能伪造该事件。

## 5. DTO 设计规则

### 5.1 允许字段

- stable ID、bounded string、bounded integer/float、bool、受限 enum；
- typed list（有明确 element type 和上限）；
- nested DTO（每层有 schema/contract version）；
- `RuntimeGeneration`、`Sequence`、`TraceId`；
- typed result status/reason/fallback。

### 5.2 禁止字段

- `Game`、`Mission`、`Agent`、`Hero`、`Settlement`、`Kingdom`、`IDataStore` 等 live 对象；
- `object`、`dynamic`、delegate、`MethodInfo`、Harmony instance/target；
- raw `Dictionary<string, object>`、raw `JObject`/`JToken` 作为公共行为载荷；
- API key、原始 HTTP response、完整 Prompt、无限玩家文本、完整存档；
- 仅凭名称解析目标、裸坐标作为主要 Agent target；
- 把 `SaveData`、`PromptText`、`ActionText` 混在同一个未分层 DTO 中。

### 5.3 边界要求

- public DTO 的所有集合必须有上限和拒绝策略；
- target identity 与 target display text 分开；
- requested/authorized/applied/confirmed 四种状态不能合并成一个 bool；
- error 必须是稳定 `ReasonCode` + 有界 message；
- DTO 的 serialize/deserialize 不能自动执行模块代码或类型元数据。

## 6. 当前 contract 到已有代码的映射

| Contract | 已有真实边界 | 现阶段处理 |
|---|---|---|
| `ConversationContextSnapshot` | `ShoutBehavior` 场景/native history 转换、`MyBehavior.BuildHistoryContext`、Courier prompt 链 | 保留现有方法；只做 metadata 设计 |
| `MemoryExchangeRecord` | `MyBehavior.AppendExternalDialogueHistory`、`AppendDailyMemoryLineById`、`SyncData` | 不改变历史/AFEF/key/type |
| `AuthorizedActionPlan` | `ShoutBehavior.TryRunSceneUnifiedActionPostprocess`、`AfGcczShoutBridge.TryProcessActionTags`、Policy compiler result | 不让标签直接执行；保持主线程 apply |
| `ActionExecutionCompleted` | `ShoutBehavior` main-thread action queue、各 runtime receipt/result | 先记录 typed event 形状，不接入 event bus |
| `PolicyEffectReceipt` | `PolicyEffectExecutionCoordinator`、`PolicyEffectDataContracts`、`PolicyEffectSaveCodec` | 复用 PolicySystem receipt/save owner，不重复存储 |
| `WorldDiplomacyResolved` | `DiplomacyBehavior` → `WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved` | 先保留现有通知入口，未来再包装公开 event |
| `SettlementActionResult` | `SiegeActionRoutingPolicy`、`SiegeCastleDirectActionAuthorizationPolicy`、`AfGcczShoutBridge` | 复用纯 policy；live apply 留在 runtime |

## 7. 组合、失败与回滚

- required capability/version 缺失：consumer=`Blocked`，entry point 不调用；
- optional capability 缺失：consumer=`Degraded`，明确 `FallbackId`；
- event consumer 失败：不回滚已确认 provider result；记录 bounded diagnostic；
- stale generation：丢弃结果，不发 confirmed event，不写 history/save；
- contract mismatch：不猜测字段，不降级成“看似成功”；进入 `Incompatible`/`Blocked`；
- SafeMode：只保留 Foundation、GameAdapter、persistence metadata 和 diagnostics；不发 gameplay Bridge event；
- 回滚：不注册新 capability/event，旧 facade 和原版 fallback 继续；不删除已有存档/receipt/fact。

## 8. 非目标与下一步

本切片不做：

- 创建 `AF.Contracts.csproj`、公共 C# 接口或 event bus；
- 修改当前 `ConversationMessage`、PolicyEffect、SceneActions Core 或 WorldDiplomacy 类型；
- 改变 `SubModule.cs`、Bootstrap、程序集身份、SyncData key、存档类型、MCM key 或三渠道行为；
- 将 `PolicyEffectModuleContracts` 或 SceneActions Core 直接移动成 Foundation；
- 把 contract version 当成 DLL/game/save version；
- 运行时热加载/卸载有 Harmony、CampaignBehavior 或持久化副作用的模块。

下一项：

> 建立阶段 3 Foundation 的主线程 dispatch、后台 snapshot/cancellation、diagnostics/trace 和 SafeMode contract；完成后才能考虑首个纯 DTO/转换函数生产切片（且仍需用户明确授权）。