# 阶段 3：Foundation Runtime Contract 设计

- 状态：设计完成；未创建 `AF.Foundation.Runtime` 生产项目；未修改生产 C#
- 日期：2026-08-29
- owner：Foundation/Runtime Safety；Host、GameAdapter 和各模块 owner 共同审阅
- 依据：`SaveRuntimeGuard.cs`、`FreezeWatchdog.cs`、`SubModule.cs`、`ShoutBehavior.cs` 的主线程队列、`AiErrorAnalysisInquiry.cs`、`.claude/skills/animusforge-maintainer/references/runtime-safety.md`
- fixture：`F:\AnimusForge-main\docs\fixtures\phase3-foundation-runtime\`
- runner：`F:\AnimusForge-main\tools\FoundationRuntimeContractTests\validate_foundation_runtime.py`

> 本文只定义 Foundation 的公共 runtime boundary。当前 `SubModule.cs`、`SaveRuntimeGuard`、`FreezeWatchdog`、各 Behavior 的队列、Tick 顺序、存档和程序集身份不变。

## 1. Foundation 的最小职责

Foundation 只拥有跨独立模块都需要、且用于保护 Host 的能力：

- 主线程 dispatch 和预算；
- 后台 snapshot、取消和 stale generation 结果门禁；
- diagnostics、trace、health 和有界错误；
- SafeMode/profile resolution；
- capability/module inventory；
- 可逆注册的生命周期句柄（不能声称能逆转不可逆 engine state）。

Foundation 不拥有：

- Conversation、Policy、Siege、Courier、WorldDiplomacy 的玩法规则；
- 模块私有 Prompt、标签、存档模型、UI VM；
- 模块 pair-specific 的 Bridge 逻辑；
- `SubModule.cs` 当前注册顺序；
- Bannerlord live object 的长期引用。

## 2. Main-thread dispatch contract

### 2.1 `MainThreadDispatchRequest`

```text
MainThreadDispatchRequest
  RequestId: stable bounded ID
  OwnerModuleId
  RuntimeGeneration
  TraceId
  WorkKind: typed bounded enum
  MaxWorkItems
  BudgetMilliseconds
  QueueBound
  RequiresStateRevalidation: true
  CancellationEpoch
  Source
```

要求：

- request 不携带 delegate、closure、`Game`、`Mission`、`Agent`、`Hero` 或 `IDataStore`；
- 实际 callable/Action 由 owner 的内部实现保存，不进入 public contract；
- main thread apply 前必须重新解析 ID、检查 generation、当前目标、权限和状态；
- `MaxWorkItems`、`BudgetMilliseconds`、`QueueBound` 必须为正且有上限；
- 超出预算/队列上限的行为必须有明确 `Dropped`/`Deferred` 结果，不得无界增长；
- `ApplicationTick` 与 `EngineTick` 是不同 runtime domain，不能用一个通用请求隐式合并。

### 2.2 `MainThreadDispatchResult`

```text
MainThreadDispatchResult
  RequestId
  Status: Applied | Deferred | Dropped | Cancelled | Stale | Rejected | Failed
  ProcessedCount
  RemainingCount
  ElapsedMilliseconds
  RuntimeGeneration
  TraceId
  ReasonCode
```

`Applied` 只表示 dispatch work 完成，不代表业务 action 成功；业务结果必须由具体 module/action contract 返回。

## 3. Background snapshot / cancellation contract

### 3.1 `BackgroundSnapshotRequest`

```text
BackgroundSnapshotRequest
  RequestId
  OwnerModuleId
  RuntimeGeneration
  SnapshotSchemaVersion
  SnapshotKind
  ImmutablePayloadHash
  PayloadItemCount
  PayloadCharCount
  CancellationEpoch
  TimeoutMilliseconds
  TraceId
```

后台 worker 只接收 detached immutable snapshot；不能接收 live Bannerlord 对象、UI VM、`IDataStore`、raw save dictionary 或模块实例。真正的 payload 必须是 typed DTO，不能用 `object`/`dynamic`/raw `JObject`。

### 3.2 `BackgroundOperationResult`

```text
BackgroundOperationResult
  RequestId
  Status: Completed | Cancelled | TimedOut | Stale | Failed | Rejected
  RuntimeGeneration
  ResultSchemaVersion
  ResultCode
  ResultItemCount
  ResultCharCount
  RequiresMainThreadApply
  TraceId
  ReasonCode
```

规则：

1. cancellation、timeout、stale 和 failure 是不同状态；
2. `Stale` 结果不能写 history、save、AFEF 或执行 action；
3. worker 不直接修改 Game/Mission/Agent/Hero/UI/存档；
4. completion 回主线程后必须重新验证 generation/target/state/permission；
5. 每个 owner 自己拥有 cancellation source/epoch、task handle 和 queue；Foundation 只提供安全端口和诊断，不吞 owner 生命周期。

## 4. Diagnostics / trace contract

### 4.1 `FoundationDiagnosticRecord`

```text
FoundationDiagnosticRecord
  TraceId
  ModuleId / BridgeId
  RuntimeGeneration
  SaveGeneration
  ProfileId
  ApiLine
  Stage
  Status
  ElapsedMilliseconds
  QueueDepth
  DroppedCount
  ErrorCode
  BoundedSummary
  FirstOccurrence
  RepeatCount
```

诊断规则：

- 单一 AF diagnostics service + module/category/trace，不创建无界的每模块日志文件；
- message 最长 240 字符，issue 最多 32 条；
- 不记录 API key、完整 Prompt、完整玩家对话、完整模型回复、私有用户路径或存档 payload；
- 首次异常保留完整 stack 的受控引用/文件位置，重复异常只聚合计数；
- 兼容性、Tick、UI 和网络重复错误必须限频；
- diagnostic 写入失败不能阻断原版逻辑，也不能伪造成功状态。

## 5. SafeMode / lifecycle resolution contract

### 5.1 `SafeModeResolution`

```text
SafeModeResolution
  RequestedProfileId
  SelectedProfileId
  State: Normal | SafeMode | Blocked | RestartRequired
  IncludedModuleIds
  ExcludedModuleIds
  PreservedPersistenceNamespaces
  FallbackId
  ReasonCode
  TraceId
```

SafeMode 必须：

- 保留 Foundation、GameAdapter、persistence metadata 和 diagnostics；
- 报告 failed/disabled/blocked module；
- 保留未知 module data，不删除、不静默迁移；
- 不自动激活替代 gameplay；
- 不发 gameplay Bridge event；
- 对有 Harmony、CampaignBehavior、MissionBehavior、持久化或未清理线程副作用的模块返回 `RestartRequired` 或 `save-load-boundary`，不能声称 runtime toggle safe。

### 5.2 生命周期状态

```text
Discovered → Disabled | Blocked | Starting → Active | Degraded | Failed | RestartRequired
```

启动失败必须：

- 在 Host boundary 捕获；
- 清理已确认可逆的 service/event/UI/timer/task 注册；
- 将模块标为 `Failed`；
- 将依赖它的模块标为 `Blocked`；
- 保持无关模块继续；
- 输出 bounded failure 和 trace。

## 6. 与现有实现的映射

| Foundation contract | 当前参考实现 | 设计处理 |
|---|---|---|
| dispatch budget | `ShoutBehavior.DrainMainThreadActionsForMissionTick`、`SubModule` ApplicationTick/EngineTick 调用 | 只提炼 metadata/结果，不替换队列或合并 Tick |
| generation/stale | `SaveRuntimeGuard.CaptureGeneration`、`IsStale`、`AdvanceGeneration` | 保留现有 guard；公共 contract 只表达 generation 和结果状态 |
| watchdog/trace | `FreezeWatchdog.Scope`、`Mark`、frame heartbeat | 只定义有界诊断字段，不替换 watchdog |
| cancellation | `ShoutNetwork`、`AIConfigHandler`、Courier 等 timeout/CTS | 各 owner 继续拥有 source；Foundation 统一 result semantics |
| health/failure | `BannerlordExceptionSentinel`、`NonBlockingErrorReport`、module catalog | 统一状态/错误码/trace，不吞原版异常 |
| SafeMode | 当前 profile/关闭开关和原版 fallback 规则 | 先做 metadata/fixture；不实现通用 repair engine |

## 7. 性能与线程预算

| contract 面 | 频率 | 限制 |
|---|---|---|
| catalog/health validation | 0 | 启动/诊断/离线按需；禁止 Tick 反射扫描 |
| main-thread dispatch | 由 owner 声明 | item/time/queue 三重上限；1.3/1.4 策略分别记录 |
| background snapshot | 事件/请求触发 | immutable snapshot；timeout/cancel/stale 必须可观测 |
| diagnostics | 事件/限频 | bounded message、聚合重复错误、低分配 |
| SafeMode resolution | 启动/profile/save-load boundary | 不在热路径重算；变化返回 restart/reload 状态 |

## 8. 非目标、回滚和下一步

本切片不做：

- 创建 `AF.Foundation.Runtime` 或 `AF.Contracts` 生产项目；
- 改动 `SubModule.cs` 的 Tick/注册顺序；
- 把现有 Behavior 队列迁移进 Foundation；
- 改变 `SaveRuntimeGuard`、SyncData key、存档类型、程序集身份或 Bootstrap；
- 声称能热卸载 Harmony、CampaignBehavior、MissionBehavior 或 persistent state；
- 把 SafeMode 做成自动修复/自动迁移引擎；
- 以“返回 0 伤害”代替退出模组处理并回原版逻辑。

回滚方式：不注册新的 Foundation contract consumer，继续使用现有队列、guard、watchdog、fallback 和 module facade；fixture/runner 可独立删除，不影响生产模块。

下一项：

> 运行 Foundation contract fixture validator，并在通过后设计 no-op module、dependency-missing、optional-provider、SafeMode、stale completion 和 failure-isolation 的纯组合矩阵；仍不实现生产 C#。