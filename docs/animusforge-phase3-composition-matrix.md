# 阶段 3：No-op / Dependency / SafeMode / Failure-Isolation 组合矩阵

- 状态：纯组合设计与 fixture 完成；未实现 Module Host/Registry；未修改生产 C#
- 日期：2026-08-30
- owner：Foundation/Host/Composition；参与模块 owner 共同审阅
- 依据：`docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/animusforge-phase3-foundation-runtime-contracts.md`、`.claude/skills/animusforge-maintainer/references/module-and-bridge-workflow.md`、`validation.md`
- fixture：`F:\AnimusForge-main\docs\fixtures\phase3-composition-matrix\`
- runner：`F:\AnimusForge-main\tools\CompositionMatrixContractTests\validate_composition_matrix.py`

> 本文件验证的是设计期的组合语义，不是实际加载器。它不创建模块实例、不调用 `SubModule.cs`、不启动游戏、不读取存档、不执行 Harmony 或网络操作。

## 1. 组合测试目标

组合矩阵必须证明：

1. no-op module 可以在 Foundation 上启动，不注册玩法、不写存档、不产生隐藏副作用；
2. required dependency/provider 缺失会使 dependent=`Blocked`，但无关 module 保持可用；
3. optional provider 缺失只产生显式 `Degraded` 和 fallback；
4. SafeMode 只保留 Foundation、GameAdapter、persistence metadata 和 diagnostics；
5. stale completion 不写 save/history/AFEF、不执行 action、不发布 confirmed event；
6. 部分启动失败会清理已经确认可逆的注册、任务和 listener；
7. Bridge 失败不拖垮 A/B，不删除 A/B 状态，不写入对方 namespace；
8. `runtime-toggle-safe` 与 Harmony/CampaignBehavior/MissionBehavior/persistence 冲突时返回 `RestartRequired`；
9. incompatible API/contract/profile 不得被降级成“看似成功”。

## 2. 测试对象与状态

设计期对象：

```text
Foundation       = af.foundation.runtime
GameAdapter      = af.game-adapter
NoOp             = af.module.noop-test-only
Conversation     = af.module.conversation
Siege            = af.module.siege-aftermath
Policy           = af.module.policy-effects
WorldDiplomacy   = af.module.world-diplomacy
Bridge           = af.bridge.policy-diplomacy 或 af.bridge.conversation-siege
```

生命周期状态：

```text
Discovered | Disabled | Blocked | Starting | Active | Degraded | Failed | RestartRequired
```

fixture 只记录状态和有界结果，不携带模块实例、delegate、Harmony target、Game/Mission/Agent/Hero、存档对象或原始日志。

## 3. 组合矩阵

| 类别 | 组合 | 必须证明 |
|---|---|---|
| 基础 | Foundation + NoOp | NoOp Active；无 gameplay/save/queue side effect |
| 基础 | Foundation + GameAdapter + NoOp | API/主线程端口可见；NoOp 仍无玩法副作用 |
| 独立模块 | Foundation + A | A 独立可用 |
| 独立模块 | Foundation + B | B 独立可用 |
| 无 Bridge | Foundation + A + B | A/B 都可用；无隐藏跨域行为 |
| 正常 Bridge | Foundation + A + B + Bridge | 只执行声明的跨域 contract |
| required 缺失 | Foundation + A，缺 required provider | A Blocked；Foundation Active |
| required 级联 | Foundation + A + dependent，A Failed | dependent Blocked；无关模块 Active |
| optional 缺失 | Foundation + A，缺 optional provider | A Degraded；显式 fallback |
| 版本不兼容 | Foundation + A，API/contract 不兼容 | Blocked/Incompatible；不执行入口 |
| SafeMode | SafeMode profile | 仅 Foundation/GameAdapter；数据保留 |
| stale | 后台 completion generation 过期 | Expired/Stale；无副作用 |
| 部分启动失败 | A 已注册部分可逆项后失败 | cleanup 完成；A Failed |
| Bridge 故障 | A/B Active，Bridge Failed | A/B Active；只隔离 Bridge |
| Bridge 禁用 | A/B Active，Bridge Disabled | 无跨域写入；Bridge data 保留 |
| toggle 冲突 | runtime-toggle-safe 声明带 Harmony/save | RestartRequired |
| 健康异常 | health issue 超限/diagnostic failure | 有界报告；不伪造 Active |

## 4. 状态转移与数据规则

### 4.1 Required dependency

```text
required module/provider missing or incompatible
  → dependent Blocked
  → entry point not invoked
  → unrelated modules continue
  → module/bridge data preserved
```

### 4.2 Optional provider

```text
optional provider missing
  → consumer Degraded
  → explicit FallbackId required
  → no silent Active claim
  → no data deletion
```

### 4.3 SafeMode

```text
SafeMode includes:
  af.foundation.runtime
  af.game-adapter
  persistence metadata
  diagnostics

SafeMode excludes:
  gameplay modules
  gameplay bridges
  unknown providers
  automatic replacement gameplay
```

SafeMode 不删除未知 namespace，不自动迁移模块数据，不伪造“与正常模式完全相同”的 gameplay state。

### 4.4 Stale completion

```text
capturedGeneration < currentGeneration
  → Stale/Expired
  → no main-thread apply
  → no action
  → no save/history/AFEF
  → no confirmed event
```

### 4.5 Failure isolation

- module start 失败：清理已确认可逆注册，模块=`Failed`；
- dependent module=`Blocked`；
- unrelated module 保持 `Active`；
- Bridge 失败：A/B 不受影响，Bridge=`Failed`；
- diagnostics 失败：记录受限错误，不把健康状态改成 Active；
- disabled bridge：不跨域写入，已有 Bridge namespace 保留。

## 5. 纯 fixture 约束

- fixture 运行频率为 0，仅按需执行；
- JSON 根对象、案例数量、模块列表、issue 列表均有上限；
- 不引用 Bannerlord 或 AnimusForge 程序集；
- 不读取存档、PlayerExports、Prompt、API key 或用户路径；
- 不包含 live game object、delegate、MethodInfo、Harmony target；
- 预期结果区分 `Active`、`Degraded`、`Blocked`、`Failed`、`Expired`、`RestartRequired`；
- runner 通过只证明设计 fixture 闭合，不证明生产 Host 已实现。

## 6. 实施前门槛

进入第一个生产 Foundation/Contract C# 切片前，必须：

1. 本矩阵所有 fixture 通过；
2. no-op、required/optional、SafeMode、stale、partial failure、Bridge failure 有结构化结果；
3. 有真实 Module Host composition test，不只依赖手工构造对象；
4. 旧 facade、存档 namespace、程序集身份、SyncData key/type 保持不变；
5. 1.3/1.4 GameAdapter 边界另行完成；
6. 用户明确授权修改生产 C#。

## 7. 当前非目标与下一步

本切片不做：

- 创建真实 Module Host/Registry；
- 创建 `AF.Foundation.Runtime.csproj` 或 `AF.Contracts.csproj`；
- 接入 `SubModule.cs`、改变 Tick/注册顺序；
- 修改程序集、存档类型、SyncData key、MCM key 或发布结构；
- 实际热卸载 Harmony、CampaignBehavior、MissionBehavior 或持久化模块；
- 把 SafeMode 做成自动修复/自动迁移引擎。

下一项：

> 整理 GameAdapter 与 Bannerlord 1.3/1.4 API 边界，建立兼容 helper、API capability、missing-member 和版本差异 fixture；完成后再做阶段 3 最终设计审查。