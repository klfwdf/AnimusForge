# AF 贵族俘虏完整接入与 SETS 受保护接管 Handoff

Date: 2026-08-28 (Addendum D)

前置文档：

- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-progress-20260828c.md`
- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-progress-20260828b.md`
- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-handoff-20260828.md`

本文件是当前最新接手入口。Addendum C 仍是“随行贵族俘虏处决、全标签放行、首级和大地图委托”的完整行为说明；本文件补充本轮已经落地的 SETS Slice B、精确决策边界、回滚点、验证结果和后续禁止跨越的风险线。

## 1. 当前总结果

### 已落地

1. 随行贵族俘虏已进入 AF 主体对话/动作链，GCCZ 活跃阶段不再额外隐藏其 AF 功能标签，包含决斗。
2. 玩家、家族英雄/同伴及同国友方贵族可在明确同意后进行场景处决；第一次有效攻击才提交原版处决。
3. 场景处决可生成 `<被处决者名字>的头颅` RP 道具；家族/同伴必须交给玩家，友方贵族按 AI 标签决定交付或保留。
4. 玩家可委托合格贵族在大地图延迟处决主队英雄俘虏；任务存档、到期重验并走原版关系归属。
5. SETS hostile Town/Castle session 已不再是完全未使用的基础层：
   - `ShouldBlockExit` 进入带旧逻辑回退的 guarded-authority 路径；
   - allied follower casualty 和 defender reserve casualty 由 session ledger 首次判重；
   - 旧 HashSet 只作为无 session/挂起回退及同步保险。
6. 修复了最后一名守军倒下到 `ReachVictory` 提交之间可能提前放行 TAB 的策略缺口。

### 仍未落地

1. `_conflictActive`、`_victoryReached` 仍是 StartConflict/ReachVictory 的 live owner；session transition 仍在比较模式。
2. `SetsUrbanCaptureCompletionPlanner` 尚未接到 mission → map 的 ownership/native-menu pump。
3. session 还没有跨 Mission 保存/恢复；不能把 mission 内对象直接当作地图阶段唯一真相源。
4. 没有 Bannerlord 实机验证，本轮只证明纯契约、verifier 与 1.3/1.4/Bootstrap 编译通过。
5. 没有覆盖游戏目录；产物仅在项目本地 stage。

结论：**贵族俘虏两项功能已经接入 AF 主体；SETS 已开始受保护接管幂等账本和 TAB 判定，但尚未接管夺城完成链。**

## 2. 本轮 SETS 问题与修复

### 2.1 已确认问题：TAB 一 tick 窗口

旧纯策略：

```csharp
return liveObjectiveDefenders > 0 || !reserveExhausted;
```

当最后一名 objective defender 已倒下且 reserve 已耗尽，但 mission tick 尚未调用 `ReachVictory` 时：

- session state 仍为 `ConflictActive`；
- `IsVictoryReady(...)` 已为 true；
- 旧 `ShouldBlockExit(...)` 却先返回 false；
- 玩家理论上能在 victory event/ledger commit 之前请求结束 Mission；
- `ConflictActive -> EndMission` 在状态机中又是非法转换。

修复后的职责拆分：

```csharp
ShouldBlockExit(...)  // 只看是否仍处于 ConflictActive
IsVictoryReady(...)   // 单独看 defender count + reserve exhausted
```

因此只有 `ReachVictory` 被合法提交、state 离开 `ConflictActive` 后，TAB 才能放行。守军数量只决定“能否触发胜利”，不再直接决定“能否退出”。

### 2.2 TAB guarded authority

运行路径：

```text
OnEndMissionRequest
  -> 计算 proven legacyBlocked
  -> ResolveGuardedCaptureExitBlock(legacyBlocked)
       -> 无 session / session suspended：legacyBlocked
       -> 计算 sessionBlocked
       -> 两者一致：返回 sessionBlocked
       -> 两者分歧：记录 DIVERGENCE + fallback=legacy
       -> 异常：记录失败 + fallback=legacy
  -> exitBlocked 决定是否显示【SETS内部暴乱】阻断框
```

关键原则：

- 这是受保护接管，不是盲切新状态机。
- 正常一致路径已经读取 session policy。
- 任何不确定情况都维持当前已知 live 行为。
- 日志明确写 `fallback=legacy`，后续实机能区分“新策略生效”与“保险回退”。

日志关键字：

```text
SETS guarded-authority DIVERGENCE at ExitBlock
SETS guarded-authority exit-block failed; fallback=legacy
```

### 2.3 casualty ledger 首次成为运行时判重者

旧路径：

```text
SettleAlliedCasualty
  -> _settledCasualtyAgentIndexes.Add

SettleDefenderReserveDefeat
  -> _settledDefenderReserveAgentIndexes.Add
```

新路径：

```text
SettleAlliedCasualty
  -> TryRecordAlliedCasualtyOnce
       -> active session: Ledger.TryRecordAlliedCasualty
       -> no/suspended session: legacy HashSet fallback

SettleDefenderReserveDefeat
  -> TryRecordDefenderCasualtyOnce
       -> active session: Ledger.TryRecordDefenderCasualty
       -> no/suspended session: legacy HashSet fallback
```

session ledger 首次记录成功时，同时把 agent index 镜像到旧集合。原因不是保留第二套长期真相，而是避免 session 在任务中途挂起后，同一 Agent 的后续回调从空的 fallback 集合再次扣 roster。等完整 session authority 与恢复机制实机稳定后，才可删除这两个旧集合。

## 3. 代码表

| Repo | 文件 | 本轮作用 | 当前权威级别 |
|---|---|---|---|
| GCCZ | `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention\SetsUrbanCapturePolicy.cs` | TAB 阻断改为保持到 `ReachVictory` 已提交。 | 纯策略权威，双仓库镜像。 |
| GCCZ | `G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\Program.cs` | 增加“守军耗尽仍阻断”和“VictoryReached 才放行”断言。 | 纯契约验证。 |
| GCCZ | `G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1` | 固定 guarded exit、ledger 接线、legacy fallback 和 policy 证据。 | 边界/镜像 verifier。 |
| NEW-10 | `G:\AFMOD\NEW-10\AnimusForge.SiegeAftermathIntervention\SetsUrbanCapturePolicy.cs` | 与 GCCZ 纯策略逐字镜像。 | AF 编译输入。 |
| NEW-10 | `G:\AFMOD\NEW-10\SettlementEntryTroopSelectionBehavior.cs` | TAB guarded authority；两类 casualty session-ledger-first。 | Bannerlord Mission adapter。 |

前序贵族功能的完整文件表见 Addendum C 第 2 节。核心入口仍是：

- `G:\AFMOD\NEW-10\NoblePrisonerExecutionOrderBehavior.cs`
- `G:\AFMOD\NEW-10\NoblePrisonerExecutionRuntime.cs`
- `G:\AFMOD\NEW-10\NoblePrisonerEscortBehavior.cs`
- `G:\AFMOD\NEW-10\NoblePrisonerEscortMissionBehavior.cs`
- `G:\AFMOD\NEW-10\AfGcczShoutBridge.cs`
- `G:\AFMOD\NEW-10\ShoutBehavior.cs`

## 4. 当前 live owner 矩阵

| 决策/副作用 | 当前 owner | session 参与方式 | 下一步条件 |
|---|---|---|---|
| hostile session 身份 | `SetsUrbanCaptureContext` | 已按 settlement/scene/player clan 创建。 | 实机核对 operation 生命周期。 |
| StartConflict | `_conflictActive` | `ShadowApply(StartConflict)` 比较合法性。 | 无 transition divergence 后再切。 |
| victory readiness | legacy mission count/reserve 逻辑 | 纯 `IsVictoryReady` 已定义，尚未成为唯一 caller。 | 逐 tick 对照日志。 |
| ReachVictory | `_victoryReached` | transition + `Ledger.TryCommitVictory()` 已跟随。 | 验证重复回调和 EndMission 顺序。 |
| TAB block | guarded resolver | 一致时采用 session；分歧/异常回退 legacy。 | 完成实机矩阵后删除 comparison input。 |
| allied casualty 幂等 | session ledger first | 成功后镜像 fallback HashSet。 | session 可恢复后删除旧集合。 |
| defender reserve 幂等 | session ledger first | 成功后镜像 fallback HashSet。 | 同上。 |
| EndMission | legacy queue/boolean | session `EndMission` 仍跟随。 | 需要可持久 handoff record。 |
| ownership transfer | `SiegeAiInterventionBehavior` completion adapter | planner 未接线。 | map pump + live owner recovery。 |
| native aftermath menu | 旧 pending bridge | planner 未接线。 | 必须在 ownership structured outcome 后。 |

## 5. 为什么本轮没有直接删除旧布尔字段

在项目日志和文本产物中没有找到可证明真实 Bannerlord 已跑完 hostile capture 的 `SETS shadow DIVERGENCE` 样本，也没有相应存档/复现包。此时直接删除：

- `_conflictActive`
- `_victoryReached`
- pending settlement victory bridge
- 旧 casualty fallback sets

会同时改变战斗、TAB、Mission teardown、ownership 和 native menu 五个边界，无法定位第一处故障。当前选择是一刀一 owner：先接幂等账本和可回退 TAB，构建通过后等待实机证据，再接完成泵。

## 6. 当前 session 适用范围

`SetsUrbanCaptureContext.TryCreateHostile(...)` 只接受：

- Town 或 Castle；
- 非玩家 clan 当前所有；
- 有合法 settlement/player clan/operation id。

Owned/attached settlement incident 与 Village 不创建 hostile capture session，继续使用各自原有 profile/path。因此旧评审中“owned incident 错进 hostile state machine”的问题在当前设计里已通过上下文分域消除，而不是继续给同一状态机堆 `IncidentTriggered` 分支。

恢复契约也已要求 live owner 必须仍为 previous owner 或玩家 clan：若第三方已夺取目标，`ResolveRecovery` 返回 `Suspend`，不得沿旧完成流程继续。

## 7. 提交与回滚

### 7.1 本轮 SETS

| Repo | Commit | 内容 |
|---|---|---|
| GCCZ | `411204f66e33197badac14cccfdb8d805e4ad8cd` | 修复退出策略、补 tests、固定 verifier 边界。 |
| NEW-10 | `af9aabc40c6efacc72ed1b67021c5ec277bd7c46` | 接入 guarded TAB authority 与 session casualty ledger。 |

接线前回滚标签（两个 repo 各自指向当时 HEAD）：

```text
backup/pre-sets-guarded-authority-slice-20260828
```

### 7.2 前序贵族功能

| Repo | Commit | 内容 |
|---|---|---|
| GCCZ | `2c3b0ea` | 贵族处决纯策略、标签/存档 codec、全标签路由和 tests。 |
| GCCZ | `0882bb2` | 贵族 runtime contract verifier 与 Addendum C。 |
| NEW-10 | `b902c23e` | 场景处决、大地图委托、首级与 AF 主体接线。 |
| NEW-10 | `11483625` | 每 Mission 复用一对 execution isolation teams。 |
| NEW-10 | `75081490` | 镜像 Addendum C。 |

贵族接线前标签：

```text
backup/pre-noble-captive-execution-integration-20260828
```

回滚应使用 `git revert <commit>`；禁止 hard reset 或改写并发提交历史。

## 8. 已完成验证

### 8.1 standalone

命令：

```powershell
dotnet run --project 'G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\AnimusForge.SiegeAftermathIntervention.Tests.csproj' -c Debug
```

结果：全部通过。新增关键断言：

- `ConflictActive + 0 defenders + reserve exhausted` 仍阻断退出；
- `VictoryReached` 才放行；
- 既有 ledger allied/defender duplicate tests 继续通过。

### 8.2 verifier

命令：

```powershell
& 'G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1' `
  -StandaloneRoot 'G:\AFMOD\GCCZ' `
  -FusedRoot 'G:\AFMOD\NEW-10'
```

结果：通过。验证了 183 个 core source 镜像、6 个 player resources、当时 9 个 handoff documents，以及 SETS guarded authority / ledger / fallback 静态证据。

### 8.3 Bannerlord build

统一脚本结果：

- Bannerlord API 1.3：0 warnings / 0 errors；
- Bannerlord API 1.4：0 warnings / 0 errors；
- Bootstrap：0 warnings / 0 errors；
- unified stage：success；
- stage 目录：`G:\AFMOD\NEW-10\bin\Debug\single_module_stage\AnimusForge`；
- game directory：未修改。

### 8.4 镜像与清理

- `SetsUrbanCapturePolicy.cs` 双仓库 raw SHA256：`AA8DC3167F766083E92828A165E00EC00AD4EBB428CF846CA90CA1B21EC32972`。
- `git diff --check`：通过。
- 旧 `CompareShadowExitBlock` 已删除；没有保留一个无人调用的兼容 shim。
- 旧 casualty `.Add(...)` 只存在于两个集中 fallback helper，不再散落在结算方法。

## 9. 未验证与风险

1. 未验证实际按 TAB 时 native `BasicLeaveMissionLogic`、`MissionFightHandler` 与本 behavior 的调用顺序。
2. 未验证最后一个 defender removed callback 与 `ReachVictory` tick 的真实时间差。
3. 未验证同一 Agent 是否会收到 Killed + Removed 等多重回调；纯账本能去重，但仍需观察 roster 数量。
4. 未验证 Mission 异常结束时 session suspension 与 legacy fallback 的日志。
5. 未验证 ownership 已由其他模组改变时当前旧 completion bridge 的行为。
6. 贵族场景行刑、首级外观、关系通知、大地图读档任务仍缺真实游戏验证；详见 Addendum C 第 11–12 节。

## 10. SETS 实机测试矩阵

每项测试前记录：存档、settlement id、owner clan、玩家 clan、选兵数量和日志起始时间。每次只改一个变量。

| 编号 | 场景 | 操作 | 必须结果 | 重点日志/数据 |
|---|---|---|---|---|
| S-B01 | 敌对 Town | 触发冲突后立即 TAB | 必须阻断。 | 无 fallback exception。 |
| S-B02 | 敌对 Castle | 触发冲突后立即 TAB | 必须阻断。 | session state=`ConflictActive`。 |
| S-B03 | 最后一名守军倒下 | 在 victory 提示前连续 TAB | 直到 `ReachVictory` 提交前始终阻断。 | 不得提前退出。 |
| S-B04 | 已 ReachVictory | 正常退出 | 必须放行且只 queue 一次后续。 | victory ledger count=1。 |
| S-B05 | allied follower 死亡 | 同一人被多回调 | 主队/存活 roster 只减 1。 | allied ledger count 只增 1。 |
| S-B06 | reserve defender 死亡 | 同一人被多回调 | garrison/militia source 只减 1。 | defender ledger count 只增 1。 |
| S-B07 | 安静进入 hostile settlement | 不触发冲突后退出 | 不错误阻断，不夺城。 | state `MissionActive -> Inactive`。 |
| S-B08 | owned/attached incident | 城内挑衅 | 不创建 hostile capture session。 | 继续 owned incident path。 |
| S-B09 | Village | 进入/冲突/退出 | 不创建 urban capture session。 | 继续 village path。 |
| S-B10 | 模拟异常/挂起 | 让 session 拒绝转换后退出 | live 行为回退旧 guard，不崩溃。 | `fallback=legacy`。 |

若任何一项失败，先回到最早出现分歧的 state/event，不要同时改 ownership/menu。

## 11. 下一 Slice：完成链接线方案

只有 S-B01–S-B10 有可复现日志后再继续：

1. 定义可保存的 `SetsUrbanCaptureHandoffRecord`，至少包含 operation id、settlement id、previous owner、player clan、state 和 committed ledger stages。
2. `ReachVictory` 成功后生成一次 handoff record；Mission end 只移交，不直接丢弃 session 身份。
3. Campaign tick 在 MapState 重建/恢复 session，先调用 `ResolveRecovery(...)` 比较 live owner。
4. 每 tick 只执行 `ResolveNextAction(...)` 返回的一个 side effect：
   - `CommitOwnership`
   - `PrepareNativeAftermathContext`
   - `OpenNativeMenu`
   - `Complete`
5. 每个 adapter 返回 `Succeeded / AlreadyApplied / Retryable / Failed`，先写 ledger commit，再推进 event。
6. Retryable 达 5 次必须 `Suspend`；Failed 立即 `Suspend`；不得继续弹菜单或重复转移所有权。
7. 新泵与旧 pending bridge 先做 guarded compare；验证一致后才删除旧 pending owner。
8. 最后逐项删除 `_conflictActive`、`_victoryReached` 和 legacy casualty sets；每删一个 owner 都单独 build + 实机。

禁止事项：

- 不得在没有 live owner recovery 的情况下把 `ownershipAlreadyCommitted=true` 直接当成功；
- 不得在 `AwaitingMap` 直接打开 native menu；
- 不得把 owned incident 或 Village 塞回 hostile capture session；
- 不得同时重写 SceneTaunt、贵族 escort 和 completion pump；
- 不得用 catch 后“假装成功”推进 ledger。

## 12. 继续工作命令

```powershell
$env:PATH='G:\AFMOD\.dotnet-sdk;' + $env:PATH
$env:DOTNET_CLI_HOME='G:\AFMOD\.dotnet-home'
$env:NUGET_PACKAGES='C:\Users\28358\.nuget\packages'
$env:TargetFrameworkRootPath='G:\AFMOD\.build_refpacks\Microsoft.NETFramework.ReferenceAssemblies.net472\1.0.3\build\'

dotnet run --project `
  'G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\AnimusForge.SiegeAftermathIntervention.Tests.csproj' `
  -c Debug

& 'G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1' `
  -StandaloneRoot 'G:\AFMOD\GCCZ' `
  -FusedRoot 'G:\AFMOD\NEW-10'

& 'G:\AFMOD\NEW-10\一键编译覆盖推送\build_single_module.ps1' `
  -ProjectRoot 'G:\AFMOD\NEW-10' `
  -BannerlordRoot 'E:\Steam\steamapps\common\Mount & Blade II Bannerlord' `
  -WorkshopContentDir 'E:\Steam\steamapps\workshop\content\261550' `
  -Configuration Debug `
  -Stage
```

不要直接 `dotnet build AnimusForge.csproj`；项目在缺少 Bannerlord API/ref 参数时按设计 fail closed。
