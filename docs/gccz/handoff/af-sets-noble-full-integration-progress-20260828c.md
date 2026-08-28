# AF 随行贵族俘虏处决、全标签放行与 SETS 后续 Handoff

Date: 2026-08-28 (Addendum C)

前置文档：

- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-progress-20260828b.md`
- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-handoff-20260828.md`

本文件记录本轮**已经落地的随行贵族俘虏功能**、精确运行契约、验证边界，以及下一轮继续 SETS 时必须遵守的顺序。它不把编译通过写成实机通过，也不宣称 SETS 的 shadow state machine 已经接管旧布尔流程。

## 1. 本轮结论

已经完成：

1. 随行贵族俘虏不再被 GCCZ 阶段路由隐藏 AF 功能规则；决斗旧关键词门禁已删除。
2. 玩家可继续使用原有随行俘虏处决确认，并在处决成功后获得 `<被处决者名字>的头颅` RP 道具。
3. 玩家家族成员、同伴、玩家本人以及同一玩家国家的友方贵族，经过当前对话的明确同意后，可在场景中亲自行刑。
4. NPC 行刑表现为：退出当前对话 → 走向随行俘虏 → 拔出真实近战武器或临时补发近战武器 → 第一次有效攻击命中时提交原版处决 → 俘虏死亡 → 行刑者走向玩家 → 处理首级归属 → 主动和玩家开启后续对话。
5. 玩家可在大地图对话中请求合格贵族处决玩家主队中的英雄俘虏；同意后任务会存档，并在 6–18 个游戏小时后的安全地图时机执行。
6. 关系后果只调用 Bannerlord 原版 `KillCharacterAction.ApplyByExecution`，不再手工叠加第二套关系惩罚。
7. 首级交付与原版处决提交均有一次性运行时闩锁，避免异常分支重复结算或重复生成道具。

尚未完成：

- 没有进行真实 Bannerlord 场景实测；当前结论仅覆盖纯策略测试、verifier 和 1.3/1.4/Bootstrap 编译。
- 没有制作可见的独立“3D 人头模型”。首级是 AF 动态命名 RP 道具，外观模板仍由现有 `RewardSystemBehavior` 选择。
- 大地图延迟处决按需求 2 只处理原版处决、关系和消息，不生成首级，也不播放场景动画。
- SETS hostile capture 的 9-state session 仍未从 shadow mode 切换为 live authority；旧 `_conflictActive` / `_victoryReached` 等流程仍驱动实际夺城。

## 2. 双仓库代码表

### 2.1 GCCZ 纯契约层（双仓库镜像）

| 文件 | 作用 | 运行边界 |
|---|---|---|
| `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention\TownAfRuleRoutingPolicy.cs` | 新增 escorted-noble overload；随行贵族俘虏在任意 GCCZ active phase 中保留所有非空、已启用 AF rule id。 | 只决定“规则是否可进入提示/后处理”，不绕过具体功能自己的 live eligibility。 |
| `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention\NoblePrisonerExecutionPolicy.cs` | 处决者分类、关系归属、首级归属、首级名称、动态标签、6–18 小时延迟和存档 codec。 | 不引用 Bannerlord 类型，可由 standalone tests 独立验证。 |
| `G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\Program.cs` | 覆盖全标签路由、处决者分类、互斥首级归属、标签解析、延迟边界和 `AFNE1` 存档 round-trip。 | 不证明 Agent 会在真实 scene template 中实际挥刀。 |

镜像文件：

- `G:\AFMOD\NEW-10\AnimusForge.SiegeAftermathIntervention\TownAfRuleRoutingPolicy.cs`
- `G:\AFMOD\NEW-10\AnimusForge.SiegeAftermathIntervention\NoblePrisonerExecutionPolicy.cs`

两组核心文件已逐文件 SHA256 对比一致。

### 2.2 NEW-10 AF 主体接线

| 文件 | 本轮改动 | 为什么必须在 AF 主体 |
|---|---|---|
| `G:\AFMOD\NEW-10\SubModule.cs` | 注册 `NoblePrisonerExecutionOrderBehavior`。 | Campaign behavior 生命周期与存档必须由 AF 启动器持有。 |
| `G:\AFMOD\NEW-10\NoblePrisonerExecutionOrderBehavior.cs` | 动态 consent rules、场景委托分发、大地图延迟任务、存档恢复、原版处决调用。 | 是 Bannerlord/Campaign adapter；纯规则仍留在 GCCZ core。 |
| `G:\AFMOD\NEW-10\NoblePrisonerExecutionRuntime.cs` | 重做场景行刑状态机、武器保证、临时敌我隔离、first-hit commit、首级奖励、返回玩家和后续对话。 | 需要 `Mission`、`Agent`、`Team`、原版 notification 和 AF RP 物品接口。 |
| `G:\AFMOD\NEW-10\NoblePrisonerEscortBehavior.cs` | 暴露可处决随行俘虏/Agent；删除旧决斗关键词数组和旧 autonomous-duel block helper。 | 仍是随行 roster 与 scene agent 的唯一 registry。 |
| `G:\AFMOD\NEW-10\NoblePrisonerEscortMissionBehavior.cs` | 转发 `OnAgentHit` / `OnAgentRemoved`；执行或决斗期间停止和平 escort maintenance。 | 防止每个 tick 再次收刀、暂停 AI、改回无敌或改回编队。 |
| `G:\AFMOD\NEW-10\DuelBehavior.cs` | 新增 `ControlsAgentForExternal`。 | 告诉 escort maintenance 当前 Agent 已由决斗系统持有。 |
| `G:\AFMOD\NEW-10\AIConfigHandler.cs` | 暴露当前 guardrail target agent index。 | 让 GCCZ/AF 路由按当前随行俘虏目标判定，而不是全场粗暴放开。 |
| `G:\AFMOD\NEW-10\AfGcczShoutBridge.cs` | preprocess/postprocess/rule allowance 增加 target-aware overload。 | 只对当前 escorted agent 关闭 GCCZ exclusive routing。 |
| `G:\AFMOD\NEW-10\MyBehavior.cs` | 将 target agent index 传入 preprocess bridge。 | 保证 courier/native prompt 目标一致。 |
| `G:\AFMOD\NEW-10\ShoutBehavior.cs` | 注入/规范化/分发新标签；随行目标保留 enabled AF rules；删除旧决斗额外 gate。 | 主对话链必须识别 consented action tag，不能用中文关键词直接授权。 |
| `G:\AFMOD\NEW-10\ShoutUtils.cs` | 删除最终 duel dispatch 的旧随行俘虏 gate。 | 避免前面已放行、最终分发又被旧代码拦截。 |

## 3. “AF 功能标签全部放开”的准确含义

对当前目标是随行贵族俘虏时：

1. `TownAfRuleRoutingPolicy.IsAllowed(..., isEscortedNoblePrisoner: true)` 对任何非空 enabled rule id 返回 true。
2. `BuildExcludedRuleIds(..., true)` 返回空集合。
3. GCCZ active phase 不再强制使用 GCCZ-only preprocess/postprocess routing。
4. scene mechanism 的 stage-level exclusion 不再隐藏随行目标规则。
5. `duel` 的 `AllowsGenericDuelForPlayerInput`、`LogBlockedAutonomousDuel`、`DuelIntentTerms` 和执行意图冲突检查已删除；`ShoutBehavior` 与 `ShoutUtils` 不再二次拦截。

这不表示所有标签可以无条件篡改游戏状态。以下原有安全条件继续有效：

- AF 全局功能开关、action-postprocess 可用性；
- 玩家原有 clan tier/功能资格；
- 目标是否有 live Agent、是否仍是玩家俘虏；
- 交易、婚姻、加入、王国命令等各功能自己的身份/库存/阵营条件；
- 一次只允许一个主 action tag 的 SETS/GCCZ postprocess contract。

也就是说，本轮删除的是“因为目标是随行贵族俘虏而额外隐藏/拦截”的旧门禁，不是删除整个 AF 的合法性检查。

## 4. 处决者、首级和关系归属矩阵

| 行刑者 | 是否允许 | 是否需要本轮明确同意 | 原版处决 responsible hero | 首级处理 | 处决后对话 |
|---|---:|---:|---|---|---:|
| 玩家 | 是 | 玩家 confirmation 即同意 | `Hero.MainHero` | 必须进入玩家主队物品栏 | 无法与自己对话 |
| 玩家家族英雄 | 是 | 是 | `Hero.MainHero` | 必须交给玩家 | 是 |
| 玩家同伴 | 是 | 是 | `Hero.MainHero` | 必须交给玩家 | 是 |
| 同一玩家国家的友方贵族 | 是 | 是 | 当前贵族本人 | AI 可选 `GIVE_HEAD` 或 `KEEP_HEAD` | 是 |
| 敌对贵族、普通平民、俘虏、死人 | 否 | 即使模型伪造标签也拒绝 | 无 | 无 | 无 |

关系归属没有自己计算数值。场景和大地图最终都只调用：

```csharp
KillCharacterAction.ApplyByExecution(victim, responsibleHero, showNotification: true, isForced: true);
```

因此：

- 玩家家族成员/同伴行刑时，原版看到的执行者是玩家，关系与家族后果归玩家侧。
- 友方贵族行刑时，原版看到的执行者是该贵族，后果归该角色及其原版关系链。
- AF 不再额外调用 `ChangeRelationAction`，避免重复扣关系。

## 5. 新动作标签与同意契约

### 5.1 场景随行俘虏

```text
[ACTION:NOBLE_EXECUTE_ESCORT:<victimHeroId>:GIVE_HEAD]
[ACTION:NOBLE_EXECUTE_ESCORT:<victimHeroId>:KEEP_HEAD]
```

`KEEP_HEAD` 只会作为友方贵族的可选 rule；玩家家族/同伴若伪造该标签会被纯策略拒绝。

### 5.2 大地图主队俘虏

```text
[ACTION:NOBLE_EXECUTE_PARTY_PRISONER:<victimHeroId>]
```

标签只有在以下条件同时成立时才会执行：

1. 本轮确实有玩家输入；
2. 当前回应者仍满足 actor eligibility；
3. victim id 来自本轮动态 allow-list；
4. 目标仍活着、仍是玩家主队英雄俘虏；
5. 场景标签只在 `Mission.Current != null`，地图标签只在 `Mission.Current == null`；
6. 模型只能命中一个互斥 allow-list tag；同时输出多个候选会 fail closed。

拒绝、犹豫、询问、转述、假设、引用旧历史、请求处决另一个人，规则文本都明确要求不得输出标签。可见回复中的执行标签会被剥离。

## 6. 场景行刑状态机

状态：

```text
QueuedConfirmation             // 玩家亲自处决时使用原版确认框
  -> WaitingForNotification
  -> WaitingForConversationExit
  -> PerformingAttack
  -> ApproachingPlayer          // NPC 行刑者
  -> 完成/恢复
```

NPC consent path 从 `WaitingForConversationExit` 开始，保证私人对话退出后再行动。

### 6.1 开打前快照

保存 actor/prisoner 的：

- Team
- Formation
- Controller
- Agent flags
- Mortality
- Speed limit
- 原 Mission mode

然后释放 AF conversation/follow control。

### 6.2 隔离敌我关系

每个 Mission 最多创建并复用一对 isolation teams：

- actor team 与 prisoner team 彼此敌对；
- 两个 isolation team 与场景其他 Team 全部中立；
- 原 settlement/player/guard/civilian Team 之间的敌我关系不改；
- 行刑结束后 actor 回原 Team，prisoner 已死亡或在中止时回原 Team。

这样不会重演“在自己地盘挑衅一个人后，全城和军团本族人都变成玩家敌人”。不能调用 `Mission.Teams.Remove` 假装删除临时 Team，因为原版 `TeamCollection.Add` 同时创建 native Team；所以本轮选择每个 Mission 固定复用一对，原生 Team 增量封顶为 2。

### 6.3 武器与第一次命中

1. actor 先尝试使用当前装备中的第一把真实近战武器。
2. 若 actor 完全没有近战武器，在空 weapon slot 临时装备 `iron_spatha_sword_t2`。
3. escort maintenance 在 execution/duel ownership 期间停止，不再每 tick 让 actor 或 prisoner 收刀、抱头、暂停或改回和平状态。
4. prisoner 设为 invulnerable + AI paused，避免其他人误伤或在 actor 真正命中前死亡。
5. 只有 `affected == prisoner && affector == actor && stage == PerformingAttack` 的第一次 hit 回调才能提交 campaign execution。
6. 提交成功后才令 scene prisoner 死亡；攻击超时 24 秒则中止，不写入处决结果。

### 6.4 幂等结算顺序

```text
NativeCommitInProgress
  -> 原版 ApplyByExecution
  -> 确认 victim 已是 Executed/ExecutionAfterMapEvent/死亡
  -> Committed = true
  -> ExecutionEffectsCompleted = true
  -> DispositionCompleted = true
```

- `NativeCommitInProgress` 防止原版同步移除 Agent 时被误判为“命中前死亡”。
- `ExecutionEffectsCompleted` 防止 profile cleanup、消息和 meeting escalation 重复。
- `DispositionCompleted` 在调用动态 RP item generator 之前置位；即使后续历史写入抛异常也不会第二次发首级。

### 6.5 返回玩家和首级交付

- NPC 行刑完成后恢复原 Team/flags/controller，再以 AI 走向玩家。
- 距离达到 2.4 米或 16 秒 approach timeout 后结算首级归属并调用 `MissionConversationLogic.StartConversation`。
- actor/player 丢失、Mission 提前结束等 fallback 会先完成一次 disposition，避免家族成员已经处决成功却永久吞掉首级。
- 玩家亲自行刑没有“走向自己”阶段，处决成功后立即结算首级。

RP 道具：

```text
名称：<victim display name>的头颅
identity key：noble_head_<victimHeroId>
数量：1
目标 roster：MobileParty.MainParty.ItemRoster
```

## 7. 大地图延迟委托

Campaign behavior：`NoblePrisonerExecutionOrderBehavior`

存档 key：

```text
_afNoblePrisonerExecutionOrders_v1
```

记录 schema：

```text
AFNE1|operationId|actorHeroId|prisonerHeroId|dueCampaignHour
```

流程：

1. NPC 本轮明确同意后生成 operation。
2. 延迟由 actor id + victim id 稳定计算，范围包含 6–18 小时。
3. 每小时检查；Mission、对话或 MapEvent 活跃时不执行。
4. 到期时重新验证 actor 活着、未被俘、仍是玩家家族/同伴或同一玩家国家友方贵族；victim 仍是玩家主队活着的英雄俘虏。
5. 验证失败则删除过期任务，不处决错误目标。
6. 验证成功则用同一 relation-attribution policy 调用原版处决，左下角显示 `XXX处决XXX`，并写入不可否认的 AF 对话历史事实。
7. 保存时序列化当前 pending tasks；加载时只接受合法 `AFNE1` 记录，损坏记录隔离丢弃。

场景 pending execution 不写存档；Mission 结束会恢复可恢复状态并中止未提交事务。已提交处决即使 Mission 提前结束，也会先完成一次首级 disposition。

## 8. 玩家反馈与本轮对应修复

| 玩家反馈 | 本轮/前序修复 |
|---|---|
| “随行的士兵空手不掏武器” | 前序 NEW-10 `88c9f3f1` 已给 SETS selected follower 恢复真实武器/fallback；本轮又让 execution actor 在无近战武器时临时补发，并让 duel/execution ownership 暂停 escort 的强制收刀维护。 |
| “自己地盘挑衅别人后，自己的士兵和城里所有人都变敌人” | 前序 `88c9f3f1` 用 `SetsCityConflictPolicy` 修复 SETS/SceneTaunt 阵营；本轮处决不复用整座城的 Team 敌对关系，而使用两支对其他 Team 中立的隔离 Team。 |
| “直接打士兵，士兵只抱头求饶” | 前序 `88c9f3f1` 已把 Guard/PrisonGuard/Soldier/Lord 从 owned passive surrender 分到 armed conflict；本轮不改写该分流。 |
| “军团里的本族人也会打我” | 前序统一了玩家直辖与玩家作为国王时的本国封臣领地 authority 定义；本轮保持该定义，不把 noble execution 的临时敌对传播给本国人。 |

## 9. 已删除的旧代码

以下旧门禁及其唯一调用点已删除，没有保留 compatibility shim：

- `ExecutionIntentTerms`
- `ExecutionNegationTerms`
- `DuelIntentTerms`
- `ContainsExecutionIntentForDuelConflict`
- `AllowsGenericDuelForPlayerInput`
- `LogBlockedAutonomousDuel`

原因：同意与执行应由 allow-listed action tag + live validation 决定，不能同时存在中文/英文关键词授权和动作标签授权两套真相源。

## 10. 提交、回滚与并发历史

回滚标签（本轮改动前）：

```text
backup/pre-noble-captive-execution-integration-20260828
```

代码提交：

| Repo | Commit | 内容 |
|---|---|---|
| GCCZ | `2c3b0ea` | 纯策略、标签/存档 codec、全标签 stage routing 和 standalone tests。 |
| NEW-10 | `b902c23e` | AF 主体 consented scene execution、map order、首级、路由和决斗接线。 |
| NEW-10 | `11483625` | 每个 Mission 复用一对 isolation teams，避免连续处决不断创建 native Team。 |

本轮进行时另一个会话提交了：

- GCCZ `11b8eaa`：meeting exit safety contract 文档。
- NEW-10 `de1f84e0`、`cef30f2b`：meeting teardown snapshot / hostile castle native meeting gate。

这些提交未被回滚、覆盖或混入上述功能提交。需要回退本轮功能时优先 `git revert` 上表提交，不要 hard reset。

## 11. 验证结果

已验证：

1. GCCZ standalone tests：全部通过，包括新处决契约断言。
2. `G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1`：检查双仓库 core 镜像、SETS 旧数值/所有权/阵营契约，以及本轮 consent tags、关系归属、first-hit commit、首级幂等、存档任务、决斗旧门禁删除。
3. NEW-10 Bannerlord API 1.3：0 warning / 0 error。
4. NEW-10 Bannerlord API 1.4：0 warning / 0 error。
5. Bootstrap：0 warning / 0 error。
6. unified stage：成功；输出 `G:\AFMOD\NEW-10\bin\Debug\single_module_stage\AnimusForge`。
7. Stage mode，没有修改游戏目录。
8. `git diff --check` 和旧决斗门禁搜索通过。

未验证：

- Bannerlord 真实 AI 是否在所有 town/castle/lordshall/meeting scene template 中按期寻路、拔刀、命中。
- 原版 execution relation notification 的具体本地化显示与第三方模组兼容性。
- 动态 RP 首级物品的最终外观模板是否满足玩家审美。
- 保存/加载后跨 6–18 小时的大地图任务实机恢复。
- 连续处决 5 名随行俘虏后的 Mission native Team 行为。

## 12. 必做实机矩阵

### A. 场景处决

1. 玩家亲自处决一名随行贵族：确认框取消一次、确认一次；取消不得死亡/发首级，确认后只发一个首级。
2. 无武器同伴同意处决：退出私聊后应补发临时剑、走近、拔刀、第一次命中死亡；结束后临时剑移除。
3. 家族英雄处决：关系后果应归玩家侧；行刑者走回玩家并开启对话；首级入玩家主队。
4. 友方贵族 `GIVE_HEAD`：关系归该贵族，首级入玩家主队。
5. 友方贵族 `KEEP_HEAD`：关系归该贵族，玩家背包不增加首级，但行刑者仍走回玩家并对话。
6. 拒绝/犹豫：不得产生 action tag 或 pending execution。
7. actor/victim 在走路途中消失或 Mission 结束：未提交时不得杀人；已提交时不得重复关系/重复发首级。
8. 连续处决 5 名：Mission 只复用同一对 isolation teams；其他守卫、居民、玩家随从不得变红名敌人。

### B. 决斗和全标签回归

1. 与随行贵族俘虏明确提出决斗并得到同意；escort maintenance 不得令其重新收刀或无敌。
2. 未提决斗、模型拒绝时不得自动决斗。
3. 在 GCCZ normal/atrocity active phase 各测试一次 reward/loan/duel/scene mechanism 的规则可见性；不满足具体功能资格时应由该功能拒绝，而不是 GCCZ router 提前隐藏。

### C. 大地图委托

1. 玩家家族成员同意处决主队英雄俘虏，存档、读档、等待 6–18 小时；应只执行一次，关系归玩家侧。
2. 友方贵族同意后在到期前离开玩家国家；任务应失效，不得远程处决。
3. victim 到期前被释放、逃跑、转移或死亡；任务应失效。
4. 到期时处于 Mission/对话/MapEvent；应延后到安全 hourly tick，而不是强行在战斗中结算。

### D. SETS 回归

继续执行 `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-progress-20260828b.md` 第 6 节矩阵：玩家直辖城、国王进入封臣领地、普通封臣进入他人领地、敌对城、alley、普通居民、徒手冲突升级。重点同时观察：

- SETS follower 是否有武器；
- player/opponent side 是否互斥；
- native Team 敌对是否被处决或决斗污染；
- `SETS shadow DIVERGENCE` 是否出现。

## 13. 下一轮 SETS 顺序

1. 先完成上面 A–D 实机矩阵并保存日志/存档，失败时只修最早出现的 runtime 边界。
2. 若 SETS shadow log 无 divergence，再逐点把 `StartConflict`、`ReachVictory`、TAB block、mission exit 决策切到 `SetsUrbanCaptureSession`；每切一个点就删一个旧 decision owner，并单独构建/实测。
3. 之后接 `ResolveNextAction` 到 ownership/native menu pump，落实 S-07/S-08；`AlreadyApplied`、`Retryable`、`Failed` 必须走 structured outcome，禁止继续堆布尔 flag。
4. 完成 authority 切换后再抽离 mission logic / reflection adapter；不要在 shadow 尚未实测时同时爆改 SETS、SceneTaunt 和 noble escort。
5. 最终共享层应只共享 participant/action lease 与 team-ownership 边界；SETS selected followers、随行贵族俘虏和城内本地守卫必须保持不同 registry，不得用一个 HashSet 冒充所有身份。

## 14. 继续工作的最小命令

```powershell
# standalone tests
$env:PATH='G:\AFMOD\.dotnet-sdk;' + $env:PATH
$env:DOTNET_CLI_HOME='G:\AFMOD\.dotnet-home'
$env:NUGET_PACKAGES='C:\Users\28358\.nuget\packages'
dotnet run --project 'G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\AnimusForge.SiegeAftermathIntervention.Tests.csproj' -c Debug

# verifier
powershell -NoProfile -ExecutionPolicy Bypass -File 'G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1'

# unified 1.3 + 1.4 + Bootstrap, project-local stage only
& 'G:\AFMOD\NEW-10\一键编译覆盖推送\build_single_module.ps1' `
  -ProjectRoot 'G:\AFMOD\NEW-10' `
  -BannerlordRoot 'E:\Steam\steamapps\common\Mount & Blade II Bannerlord' `
  -WorkshopContentDir 'E:\Steam\steamapps\workshop\content\261550' `
  -Configuration Debug `
  -Stage
```

不要直接 `dotnet build AnimusForge.csproj`：该项目对缺少 `BannerlordApi`/引用目录的直接构建是 fail-closed，必须使用上面的单模块脚本或传齐 API 参数。
