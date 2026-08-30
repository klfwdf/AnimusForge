# 阶段 2：Host Registry 纯 Validator 输入/输出 Fixture

- 状态：fixture 设计完成；未实现 validator；未接入运行时
- 日期：2026-08-30
- owner：Host/Composition
- 输入契约来源：`docs/animusforge-phase2-registry-dto-design.md`
- 现状清单来源：`docs/animusforge-phase2-submodule-registration-catalog.md`
- 目的：为后续 manifest/schema、依赖图、owner、profile、线程边界和失败隔离检查提供稳定的纯数据样例

> 本文件是设计期 fixture，不是生产 C#、不是实际 `SubModule.cs` 注册器，也不是要求当前仓库立即生成的 JSON 文件。示例中的 `HostRegistrySnapshot`、`HostContributionGroupDescriptor` 和 `HostContributionDescriptor` 均为逻辑 DTO 名称。

## 1. Validator 边界

纯 validator 只能读取不可变的 registry metadata，输出有界的结构化结果。它不得：

- 创建、持有或调用 `CampaignBehavior`、`MissionBehavior`、Harmony patch、UI、Agent、Hero、Mission、Game 等运行时对象；
- 反射扫描程序集、自动发现类型或根据类型全名生成稳定 ID；
- 读取/写入存档、Prompt、API key、玩家数据、`PlayerExports` 或原始对话；
- 参与 `ApplicationTick`、`EngineTick` 或任何游戏主线程调度；
- 根据校验结果自动改写当前 `SubModule.cs` 注册顺序；
- 把 optional provider 缺失误报为 composition failure；
- 把 required contribution 缺失静默降级为成功。

建议调用时机：启动/诊断阶段或离线测试；频率为 **0（不在 Tick 热路径执行）**。输入和输出均应有上限：

| 限制项 | 设计上限 | 目的 |
|---|---:|---|
| group 数量 | 64 | 防止异常 catalog 造成无界遍历 |
| contribution 数量 | 512 | 覆盖当前 Host 注册面并保留扩展空间 |
| dependency 数量/项 | 32 | 控制依赖图和错误输出规模 |
| validation issue 数量 | 32 | 失败结果可诊断但不会刷屏 |
| issue message 长度 | 240 字符 | 禁止把原始日志/Prompt 塞入结果 |

## 2. Fixture 约定

### 2.1 受限值

```text
Stage:
  Lifecycle | Harmony | Model | CampaignBehavior | MissionAdapter
  | ApplicationTick | EngineTick

ThreadBoundary:
  MainThreadOnly | BackgroundSnapshotOnly | MainThreadApply | NoGameAccess

FailurePolicy:
  ContinueSibling | FailGroup | FailComposition | Degraded

ValidationState:
  Valid | Invalid | Degraded

ApiLines:
  1.3 | 1.4
```

### 2.2 组级规则

1. `GroupId` 全局唯一；每组只能有一个 `Stage`。
2. `ApplicationTick` 与 `EngineTick` 必须是两个不同的组，不能通过同一个组或同一个贡献项混用。
3. 组内贡献项按 `LegacyOrder` 保留当前 `SubModule.cs` 观察到的顺序；validator 只报告冲突，不重排。
4. `GroupOrder` 只记录现状，不授予未来移动或合并注册顺序的权限。
5. 组级 `ThreadBoundary` 只能收紧约束，不能把 `MainThreadOnly` 贡献项标记成后台安全。
6. `Contributions` 是只读元数据列表，不是可执行对象列表。

### 2.3 贡献项级规则

1. `ContributionId` 全局唯一且稳定；重复 ID 是 `Invalid`。
2. `GroupId` 必须存在；未知组是 `Invalid`。
3. `Requires` 只能引用已声明的贡献项 ID；未知依赖是 `Invalid`。
4. 依赖图必须无环；出现环是 `Invalid`。
5. `Owner`、`ContractVersion`、`ApiLines`、`ThreadBoundary`、`FailurePolicy` 和 `EnabledProfile` 必须完整。
6. 后台贡献项只能接受不可变 snapshot；不能携带 live `Game`、`Mission`、`Agent`、`Hero`、`IDataStore` 或同类对象。
7. `SaveImpact` 只描述影响，不改变现有存档类型、程序集身份或 `SyncData` key。
8. `ChannelImpact` 只描述影响，不自动启用或切换信使、自由对话、场景喊话渠道。

## 3. 有效输入：完整 HostRegistrySnapshot

下面的 fixture 覆盖当前 Host 的七个 contribution group，并保留 ApplicationTick/EngineTick 分离。它是用于 schema/依赖/边界测试的最小完整样例，不替代当前 35 个 CampaignBehavior 的逐项现状清单。

```yaml
HostRegistrySnapshot:
  SchemaVersion: 1
  Source: SubModule.current
  ApiLines: [1.3, 1.4]
  ValidationState: Valid
  ValidationIssues: []
  Groups:
    - GroupId: host.lifecycle
      Stage: Lifecycle
      Owner: Host.Composition
      GroupOrder: 10
      FailurePolicy: FailComposition
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.lifecycle.scene-actions-runtime
          GroupId: host.lifecycle
          Owner: Host.Composition
          Stage: Lifecycle
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: Degraded
          SaveImpact: None
          ChannelImpact: [None]
          Requires: []
          ContractVersion: 1
          Notes: Initialize external scene runtime once; isolate failure.
        - ContributionId: host.lifecycle.ui-extender
          GroupId: host.lifecycle
          Owner: UI.Diagnostics
          Stage: Lifecycle
          LegacyOrder: 2
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: ContinueSibling
          SaveImpact: None
          ChannelImpact: [NativeConversation]
          Requires: []
          ContractVersion: 1
          Notes: Register and enable UI extension at module load.

    - GroupId: host.harmony
      Stage: Harmony
      Owner: Host.Composition
      GroupOrder: 20
      FailurePolicy: ContinueSibling
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.harmony.core-entry
          GroupId: host.harmony
          Owner: Compatibility.Safety
          Stage: Harmony
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: ContinueSibling
          SaveImpact: None
          ChannelImpact: [None]
          Requires: []
          ContractVersion: 1
          Notes: Core entry/safety patch group.
        - ContributionId: host.harmony.conversation-input
          GroupId: host.harmony
          Owner: Conversation.UI
          Stage: Harmony
          LegacyOrder: 17
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: ContinueSibling
          SaveImpact: None
          ChannelImpact: [SceneShout, NativeConversation, Courier]
          Requires: [host.harmony.core-entry]
          ContractVersion: 1
          Notes: Preserve input focus and native conversation boundary.

    - GroupId: host.models
      Stage: Model
      Owner: Host.Composition
      GroupOrder: 30
      FailurePolicy: FailGroup
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.models.courier
          GroupId: host.models
          Owner: Courier
          Stage: Model
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: FailGroup
          SaveImpact: ReadsExisting
          ChannelImpact: [Courier]
          Requires: []
          ContractVersion: 1
          Notes: Register courier model before campaign behaviors.
        - ContributionId: host.models.settlement
          GroupId: host.models
          Owner: Settlement.Siege
          Stage: Model
          LegacyOrder: 2
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: FailGroup
          SaveImpact: ReadsExisting
          ChannelImpact: [SceneShout, NativeConversation, Courier]
          Requires: []
          ContractVersion: 1
          Notes: Register settlement model without changing save identity.

    - GroupId: host.campaign-behaviors
      Stage: CampaignBehavior
      Owner: Host.Composition
      GroupOrder: 40
      FailurePolicy: ContinueSibling
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.campaign-behaviors.memory
          GroupId: host.campaign-behaviors
          Owner: Conversation.AI
          Stage: CampaignBehavior
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: FailGroup
          SaveImpact: LegacyCompatibilityRequired
          ChannelImpact: [SceneShout, NativeConversation, Courier]
          Requires: []
          ContractVersion: 1
          Notes: Preserve MyBehavior history, memory, AFEF and SyncData contract.
        - ContributionId: host.campaign-behaviors.courier
          GroupId: host.campaign-behaviors
          Owner: Courier
          Stage: CampaignBehavior
          LegacyOrder: 2
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: ContinueSibling
          SaveImpact: ReadsExisting
          ChannelImpact: [Courier]
          Requires: [host.models.courier]
          ContractVersion: 1
          Notes: Register CourierDeliveryBehavior after courier model.
        - ContributionId: host.campaign-behaviors.scene-actions
          GroupId: host.campaign-behaviors
          Owner: SceneActions
          Stage: CampaignBehavior
          LegacyOrder: 3
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: Degraded
          SaveImpact: None
          ChannelImpact: [SceneShout, NativeConversation, Courier]
          Requires: [host.lifecycle.scene-actions-runtime]
          ContractVersion: 1
          Notes: Thin AF adapter; reusable runtime remains separately owned.

    - GroupId: host.mission-adapters
      Stage: MissionAdapter
      Owner: Host.Composition
      GroupOrder: 50
      FailurePolicy: Degraded
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.mission-adapters.scene-actions
          GroupId: host.mission-adapters
          Owner: SceneActions
          Stage: MissionAdapter
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: Degraded
          SaveImpact: None
          ChannelImpact: [SceneShout]
          Requires: [host.lifecycle.scene-actions-runtime]
          ContractVersion: 1
          Notes: Register/verify mission behavior; no business logic in host.

    - GroupId: host.application-tick
      Stage: ApplicationTick
      Owner: Host.Composition
      GroupOrder: 60
      FailurePolicy: ContinueSibling
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.application-tick.fast-path
          GroupId: host.application-tick
          Owner: Host.Composition
          Stage: ApplicationTick
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: ContinueSibling
          SaveImpact: None
          ChannelImpact: [None]
          Requires: []
          ContractVersion: 1
          Notes: Fast application-level tick path; bounded work only.
        - ContributionId: host.application-tick.diagnostics
          GroupId: host.application-tick
          Owner: UI.Diagnostics
          Stage: ApplicationTick
          LegacyOrder: 2
          EnabledProfile: [diagnostics]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: Degraded
          SaveImpact: None
          ChannelImpact: [None]
          Requires: [host.application-tick.fast-path]
          ContractVersion: 1
          Notes: Optional watched diagnostics path; not a replacement for fast path.

    - GroupId: host.engine-tick
      Stage: EngineTick
      Owner: Host.Composition
      GroupOrder: 70
      FailurePolicy: ContinueSibling
      ThreadBoundary: MainThreadOnly
      ContractVersion: 1
      Contributions:
        - ContributionId: host.engine-tick.tts
          GroupId: host.engine-tick
          Owner: Conversation.AI
          Stage: EngineTick
          LegacyOrder: 1
          EnabledProfile: [default]
          ApiLines: [1.3, 1.4]
          ThreadBoundary: MainThreadOnly
          FailurePolicy: Degraded
          SaveImpact: None
          ChannelImpact: [SceneShout, NativeConversation, Courier]
          Requires: []
          ContractVersion: 1
          Notes: Main-thread TTS pump; keep separate from ApplicationTick.
```

### 3.1 有效输入的预期输出

```yaml
ValidationResult:
  State: Valid
  Issues: []
  Counts:
    Groups: 7
    Contributions: 14
    Errors: 0
    Warnings: 0
```

说明：`diagnostics` profile 未启用不构成错误；它是已声明且闭合的 optional contribution。fixture 的 `Counts` 是示例输出，不是当前生产注册项总数。

## 4. 无效输入与预期输出

每个无效 case 都从第 3 节有效 fixture 复制，只修改标出的字段。validator 应在发现多个问题时继续收集，但最多返回 32 条 issue。

| Case | 输入变更 | 预期状态 | 预期 issue code | 预期 severity |
|---|---|---|---|---|
| invalid-01 | 两项使用同一个 `ContributionId` | Invalid | `DuplicateContributionId` | Error |
| invalid-02 | 同组两项使用同一个 `LegacyOrder` | Invalid | `DuplicateLegacyOrder` | Error |
| invalid-03 | contribution 的 `GroupId=host.unknown` | Invalid | `UnknownGroupId` | Error |
| invalid-04 | `Requires=[host.missing]` | Invalid | `UnknownDependency` | Error |
| invalid-05 | A requires B，B requires A | Invalid | `DependencyCycle` | Error |
| invalid-06 | `host.application-tick` contribution 标记为 `EngineTick` | Invalid | `StageGroupMismatch` | Error |
| invalid-07 | `host.engine-tick` 与 application tick 使用同一 `GroupId` | Invalid | `TickStageNotSeparated` | Error |
| invalid-08 | 清空 `Owner` | Invalid | `MissingOwner` | Error |
| invalid-09 | 清空 `ContractVersion` | Invalid | `MissingContractVersion` | Error |
| invalid-10 | 清空 `ApiLines` | Invalid | `MissingApiLine` | Error |
| invalid-11 | 后台贡献项 Notes/metadata 声明携带 live `Mission` 或 `Agent` | Invalid | `LiveGameObjectInBackgroundContribution` | Error |
| invalid-12 | DTO 序列化形状包含 `Behavior`、delegate、`MethodInfo` 或 raw dictionary | Invalid | `RuntimeObjectLeak` | Error |
| invalid-13 | required contribution 缺失 | Invalid | `RequiredContributionMissing` | Error |
| invalid-14 | optional provider 缺失，但 required graph 仍闭合 | Degraded | `OptionalProviderMissing` | Warning |
| invalid-15 | profile 引用了未声明的 contribution | Invalid | `ProfileClosureViolation` | Error |
| invalid-16 | `SaveImpact` 写成新类型/新 key 的运行时指令 | Invalid | `PersistenceContractMutation` | Error |

> Case 11/12 的“携带”是 schema/metadata 语义上的携带，不表示运行时对象真的被实例化。纯 validator 应能拒绝不允许的字段形状或标记，而不是尝试解析对象。

### 4.1 代表性错误输出

#### duplicate `ContributionId`

```yaml
ValidationResult:
  State: Invalid
  Issues:
    - Code: DuplicateContributionId
      Severity: Error
      ContributionId: host.campaign-behaviors.memory
      GroupId: host.campaign-behaviors
      Message: ContributionId must be globally unique; duplicate declaration found.
      SuggestedOwner: Host.Composition
```

#### dependency cycle

```yaml
ValidationResult:
  State: Invalid
  Issues:
    - Code: DependencyCycle
      Severity: Error
      ContributionId: host.campaign-behaviors.memory
      GroupId: host.campaign-behaviors
      Message: Dependency cycle detected: memory -> courier -> memory.
      SuggestedOwner: Host.Composition
```

#### optional provider missing

```yaml
ValidationResult:
  State: Degraded
  Issues:
    - Code: OptionalProviderMissing
      Severity: Warning
      ContributionId: host.application-tick.diagnostics
      GroupId: host.application-tick
      Message: Optional diagnostics provider is unavailable; fast path remains active.
      SuggestedOwner: UI.Diagnostics
```

#### required contribution missing

```yaml
ValidationResult:
  State: Invalid
  Issues:
    - Code: RequiredContributionMissing
      Severity: Error
      ContributionId: host.campaign-behaviors.memory
      GroupId: host.campaign-behaviors
      Message: Required memory contribution is absent; composition cannot be considered valid.
      SuggestedOwner: Conversation.AI
```

## 5. 输出契约

输出必须是有界、可记录、与运行时对象无关的结构：

```text
ValidationResult
  State: Valid | Invalid | Degraded
  Issues: IReadOnlyList<ValidationIssue> (最多 32 项)
  Counts: bounded counters

ValidationIssue
  Code: stable machine-readable code
  Severity: Error | Warning | Info
  ContributionId: stable ID or empty when issue is group/snapshot scoped
  GroupId: stable ID or empty when issue is snapshot scoped
  Message: short sanitized text, maximum 240 chars
  SuggestedOwner: owner string or empty
```

禁止输出：

- `Behavior`/`MissionBehavior` 实例、Harmony target、delegate、`MethodInfo`、DI 对象；
- `Game`、`Mission`、`Agent`、`Hero`、`IDataStore` 等 live object；
- 原始 Prompt、API key、完整对话、存档内容、`PlayerExports` 内容；
- 无上限的异常堆栈、玩家输入或网络响应正文。

## 6. 状态与失败隔离映射

| 条件 | registry 状态 | 允许的后续行为 |
|---|---|---|
| 所有 group、ID、依赖、owner、版本、线程边界和 profile closure 合法 | Valid | 仅供诊断/后续 composition 检查读取，不自动执行 |
| 只有 optional provider 缺失，required graph 合法 | Degraded | 保持无 provider 的安全 fallback；记录 warning |
| required contribution 缺失、依赖环、未知组、线程边界违规或契约破坏 | Invalid | 阻止该 composition 进入“可验证成功”；由 host 选择显式 SafeMode/fallback |
| validator 本身异常或输入超出上限 | Invalid | 返回有界 `ValidatorFailure`，不得吞掉原版逻辑或进入无界重试 |

这里的 `SafeMode` 只是后续 foundation/host 的选择结果，不在本 fixture 中实现，也不改变现有 AF 关闭后的原版 fallback 语义。

## 7. 与当前代码和重构阶段的关系

- 不修改 `SubModule.cs`；当前注册调用、Harmony 顺序、CampaignBehavior 顺序、Mission adapter、ApplicationTick 和 EngineTick 仍以现状为准。
- 不把此 fixture 自动生成成生产 registry；阶段 2 只固化可审阅的元数据边界。
- 不改变 `AnimusForge` / `AnimusForge.Bootstrap` 程序集身份，不改变 `SubModule.xml`、SyncData key、存档类型或单一模块发布结构。
- 不改变 1.3/1.4 双实现策略；fixture 明确声明 `[1.3, 1.4]`，但不代替两套构建验证。
- 不改变信使、自由对话、场景喊话三渠道；`ChannelImpact` 只用于 owner/风险诊断。
- 不改变 Prompt、标签、AFEF、TTS 或记忆持久化；`SaveImpact`/`ChannelImpact` 只记录边界。
- 本切片运行频率为 0；未运行游戏、未加载存档、未部署模块。

## 8. 后续准确任务

1. 把阶段 2 的剩余 owner map 补全：存档、Prompt、标签、Harmony、Tick、UI、主线程和 1.3/1.4 影响。
2. 标注跨模块行为和候选 Bridge，特别是 Conversation ↔ Memory/Action、Settlement/Siege ↔ World、Policy ↔ Diplomacy。
3. 为每个目标模块补充非目标、回滚入口和最小验证矩阵。
4. 进入阶段 3 前，再设计 manifest/profile/dependency/health catalog；本 fixture 不直接升级为生产实现。