# 领主部队精锐化效果研究

## 当前实现决策

- 可见模块 ID：`lordPartyTroopXp`，执行类型为只在编译期存在的 `Composite`。
- 可见 payload：`{"onceDelta": integer, "dailyDelta": integer}`；两项单位均为每名普通士兵的原版兵种经验。
- `onceDelta` 范围 `0～5000`，展开为隐藏 `lordPartyTroopXpOnce`，在政策通过后的下一游戏日执行一次。
- `dailyDelta` 范围 `0～100`，展开为隐藏 `lordPartyTroopXpPerDay`，从同一天起在政策有效期间每日执行。
- 两项均为 0 时不生成运行实例；两项均非零时会在首个执行日叠加。
- Composite 只执行一次 funding 缩放；两个隐藏子模块均为 `Unscaled/None`。
- 目标沿用现有政策 Hero 投影，执行时仅保留亲自带领 `IsLordParty` 的 NPC Hero。
- once 的完整 before/after 日志继续保存在有界 RuntimeState；daily 的单 Hero/Party 日志保存在该目标自己的 AppliedReceipt，避免多目标覆盖共享 RuntimeState。

## 目标定义

本效果作用于目标 NPC Hero **当前实际带领的 `MobileParty.MemberRoster` 中的普通士兵兵种栈**。

它不增加 Hero 技能经验，不影响玩家部队或 `PrisonRoster`，不直接替换/升级兵种，也不修改士兵数量和伤兵数量。

## 原版 API 与经验单位

- Bannerlord 1.3.x 与 1.4.5 均提供 `TroopRoster.AddXpToTroop(...)` 和 `TroopRoster.AddXpToTroopAtIndex(...)`。
- `TroopRosterElement.Xp` 是整个兵种栈共享的总经验池，不是单兵经验字段，也不是已经兑换好的升级人数。
- 若政策语义是“每名士兵增加 `xpPerTroop` 经验”，对每个兵种栈只调用一次，传入：

  ```text
  stackXpDelta = xpPerTroop * TroopRosterElement.Number
  ```

- 原版 `MobilePartyTrainingBehavior` 也使用“每兵经验乘以兵种栈人数，再调用一次 AddXp”这一语义。
- `Number` 包含伤兵；原版每日训练不会减去 `WoundedNumber`，因此默认精锐化效果也包含伤兵。
- 必须使用 `long` 完成乘法、当前值相加和容量预检，再安全转换为 `int`，避免溢出回绕后被钳制为零。

## 升级阈值与原版行为

- XP 增加后只会产生原版的“可升级人数”，不会自动升级兵种。
- 每条升级路线的可升级人数约为：

  ```text
  min(stackNumber, stackXp / routeUpgradeXpCost)
  ```

- `PartyBase.OnXpChanged` 会把 MemberRoster 的 XP 钳制到该兵种栈可用升级路线的原版容量。
- 最大阶兵种、无升级目标或全部升级目标无效的兵种栈不会保留有效升级 XP，应在执行前跳过，并在调用后读取实际 XP 作为真实应用量。
- 升级成本来自当前 Campaign 的 `PartyTroopUpgradeModel`，可能被其他模组覆盖，不能硬编码 tier 经验表。

## 保存、事件与刷新

- `TroopRosterElement.Xp` 是原版 SaveableField，正常存档会自动保存，不需要额外保存 API。
- `SetElementXp` 已调用 `OwnerParty.OnXpChanged`，负责原版合法性和容量处理。
- XP 变化不应伪造招募、数量变化或士兵升级事件。
- 原版每日训练不会额外调用 `TroopRoster.UpdateVersion()`。若本批次后立即依赖 `GetTroopRoster()` 缓存或 UI 刷新，可在每个已修改 Party 完成后调用一次；不得按兵种栈重复调用。

## Hero 与 Party 目标边界

目标 Hero 必须满足：

- Hero 存在、Active、未死亡、未禁用、未被俘；
- `Hero.PartyBelongedTo` 存在；
- Party active、未解散，且 `party.Party` 存在；
- `party.LeaderHero == hero`，即 Hero 确实在带领该 Party，而不是作为同伴乘坐他人的 Party。

边界规则：

- Hero 无 Party、仅在定居点停留：跳过。
- Hero 带领自己的 Party 进入定居点：可以作用。
- 军团成员：作用于该 Hero 自己带领的军团成员 Party，不改为军团长 Party。
- 不回退到 `PartyBelongedToAsPrisoner`、定居点驻军或民兵。
- 排除 MainParty、临时信使 Party、民兵和驻军。
- Hero 确实带领商队时，按“当前带领的 MobileParty”语义可包含；如果产品只允许战团，应由 payload/模块合同显式排除商队。
- 多个目标 Hero 指向同一 `MobileParty` 时按 Party 稳定 ID 去重。

## MemberRoster 过滤

仅处理满足以下条件的 MemberRoster 元素：

- `Character != null`；
- `Number > 0`；
- `!Character.IsHero`，排除目标 NPC 本人和所有其他 Hero；
- 存在至少一条合法升级路线。

不读取或修改 `PrisonRoster`。

## 推荐效果语义

当前可见 payload：

```json
{
  "onceDelta": 300,
  "dailyDelta": 20
}
```

- 两项均为非负整数，单位是“每名普通士兵获得的原版经验”；隐藏运行腿只接受严格正值。
- 一次集训、集中整编或单次改革成果使用 `onceDelta`；常设、每日、长期或制度化训练使用 `dailyDelta`。
- 只有正文明确“先集中整编、后持续操练”时才同时填写；模糊“提高精锐程度”默认一次性，避免无依据地创造累积每日收益。
- 作用于目标 Party 的全部合格普通兵种栈，不同时引入 tier、文化或兵种树过滤。
- 对已经接近经验上限的栈执行饱和增加，记录实际变化量；不产生负 XP。
- “明显效果”应通过模块提示词给出清晰强度标尺，而不是改变原版升级成本或直接升级兵种。

建议强度标尺以原版实际表现为准：

- 较弱或局部训练：约 `100-150` XP/兵；
- 常规明确训练：约 `150-300` XP/兵；
- 明显精锐化：约 `300-600` XP/兵；
- 重大集中成果：约 `600-1200` XP/兵；
- 极强值仍受原版兵种栈升级容量钳制。

具体标尺必须通过 1.3.x/1.4.5 实机中低阶、中阶和高阶兵种栈校准。

当前模块采用更严格的“领主野战部队”边界：要求 `MobileParty.IsLordParty`，因此商队不在两个运行模块的作用范围内。

## 生命周期建议

Composite 同时支持“下一游戏日一次性”与“同日起每日”：

- 复用现有政策 DailyScheduler、冻结目标、回执、有限重试和补偿链；
- once 与首个 daily 均在下一游戏日执行，调度顺序为 once 先提交、daily 后执行；
- 政策在首次执行日前废除则不应用；
- 已应用后废除或到期不追回历史经验；
- 回滚只用于技术事务失败，不用于政策语义上的废除。

- 政策结束只停止未来 daily，不能反转之前的合法训练经验或原版已经完成的升级。

## 原子性、幂等与补偿

执行顺序：

1. 从冻结 Hero ID 解析目标 Hero 和其当前带领 Party；
2. 按 Party ID 去重并稳定排序；
3. 读取所有目标栈的 before、人数、升级路线与原版容量；
4. 使用 `long` 计算请求量和实际可用容量；
5. 在首次写入前验证完整回滚日志能够通过保存合同；
6. 对每个栈只执行一次经验写入；
7. 即使调用抛异常，也重新读取实际 XP，识别“先写入、后回调失败”；
8. 失败时按逆序对已经写入的栈执行 CAS 补偿。

补偿规则：

- 当前值等于记录的 after：恢复 before；
- 当前值已经等于 before：视为幂等完成；
- 当前值是其他值，或兵种栈已升级、移动、消失：失败关闭，不直接减去 delta，避免删除战斗或原版训练新获得的合法 XP。

确定性无目标、目标 Hero 消失、无 Party、无合格兵种栈应返回 `Skipped`，不进行无意义重试。技术错误才进入现有有限重试链。

## 保存体积风险

政策效果 receipt/payload 和运行状态存在严格大小上限。面向“全部领主”时，一个完整 before/after 兵种栈日志可能超过限制。

首版必须：

- 在首次修改前构造并序列化预检完整回滚日志；
- 超限时以带原因回执的零写入 `Skipped` 完成，不能截断日志或阻塞后续每日调度；
- 必要时限制单实例目标 Party/兵种栈数量，或设计有界分批执行；
- once receipt 只保存摘要，完整补偿日志存在受保存合同保护的 RuntimeState 中。
- daily 每次只处理一个 Hero，完整单 Party 日志必须放在该目标的 receipt 中；不得放入同一实例共享 RuntimeState，否则后一个 Hero 会覆盖前一个 Hero 的补偿证据。

## 性能合同

每日调度不得扫描全部 Hero 或 `MobileParty.All`。

应从现有政策目标解析器生成的冻结 Hero ID 和索引出发，只访问这些 Hero 当前带领的 Party，并且每个去重后的 MemberRoster 只遍历一次。

目标复杂度：

```text
O(targetHeroCount + uniqueTargetPartyTroopStackCount)
```

不得在热路径重复反射、逐兵分配对象、逐栈刷新 roster version 或进行全世界 Party 查找。

## 双版本证据

- 1.3.x：`原版游戏本体代码1.3.x/TaleWorlds.CampaignSystem/Roster/TroopRoster.cs`
- 1.4.5：`原版游戏本体代码1.4.5/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/Roster/TroopRoster.cs`
- 原版每日训练：两个版本的 `CampaignBehaviors/MobilePartyTrainingBehavior.cs`
- XP 容量钳制：两个版本的 `Party/PartyBase.cs` 中 `OnXpChanged`

两个版本的相关公开 API 与经验语义一致，当前预计不需要新增 `#if BANNERLORD_1_4_OR_GREATER`，但实现后仍必须分别编译 `BannerlordApi=1.3` 和 `BannerlordApi=1.4` 并进行实机验证。

### 2026-08-21 已执行真实 DLL API 探针

测试入口：`tools/PolicyEffectModule.ContractTests/Program.cs` 的
`--lord-party-troop-xp-bannerlord-api-only`。

已用真实 TaleWorlds 程序集执行同一组 20 项断言：

- 固定 1.3 引用：`_deps_auto`，BuildInfo `v1.3.15.110062`，通过；
- 仓库现有 1.4 引用快照：`.tmp/build_check/1.4`，BuildInfo `v1.4.6.115628`，通过。

运行时烟雾结果在两个版本一致：创建一个 10 人普通兵种栈，调用一次
`AddXpToTroopAtIndex(index, 3000)` 后，`TroopRosterElement.Xp == 3000`，而不是
`30000`；随后调用 `AddXpToTroop(troop, 200)` 后总值为 `3200`，士兵数量和伤兵数量
均不变。`AddXpToTroopAtIndex` 不会自行递增 `TroopRoster.VersionNo`，显式调用
`UpdateVersion()` 才递增一次；`SetElementXp` 可以把栈 XP 精确恢复到指定值而不改数量。

真实 DLL 探针同时确认了 `Hero.PartyBelongedTo`、`MobileParty.MemberRoster`、
`PartyBase.MemberRoster`、领主 Party 边界属性、`GetUpgradeXpCost(PartyBase, int)`、
`GetElementCopyAtIndex`、`AddXpToTroop`、`AddXpToTroopAtIndex`、`SetElementXp`、
`UpdateVersion` 和 `CampaignObjectManager.Find<T>(string)` 的实际可用签名。

仓库当前没有 BuildInfo 为 1.4.5 的原版 DLL 快照，因此本轮无法声称执行了“精确
1.4.5 二进制”测试；1.4.5 仍由仓库内反编译源码逐项核验，真实二进制探针则使用最接近
的 1.4.6 快照。完整 Campaign 中的升级容量钳制、UI 与保存/读档仍属于实机验证范围。

## AI 领主自动升级兵种链

1.3.x 与 1.4.5 的原版实现一致，入口为
`CampaignBehaviors/PartyUpgraderCampaignBehavior.cs`：

- `MapEventEnded` 对参战的每个 `PartyBase` 调用 `UpgradeReadyTroops`；
- `DailyTickParty` 在 Party 不处于 `MapEvent` 时调用 `UpgradeReadyTroops`；
- `UpgradeReadyTroops` 明确排除 `PartyBase.MainParty`，但会处理其他 active Party，因此领主
  AI 部队属于原版自动升级范围；
- 可升级数为 `min(健康人数, 栈总 XP / 当前路线升级 XP 成本)`；伤兵暂时不升级，但共享栈 XP
  会保留，伤愈后仍可在后续 tick 使用；
- 升级时原版先扣除 `upgradeXpCost * upgradedCount`，再从源兵种减员并向目标兵种加同等人数；
- 多条升级路线由 `GetUpgradeChanceForTroopUpgrade` 加权选择，领主偏好阵型对应路线具有极高权重；
- 工资上限、`PartyTradeGold`、强盗转正规军所需条件等都可能把本次可升级人数压到零。条件
  不足时 XP 不会凭空消失，会留待以后再次检查。

默认升级成本按源阶到目标阶逐阶累加；常规相邻阶成本为：

| 源阶→目标阶 | 每名升级 XP |
| --- | ---: |
| T0→T1 | 100 |
| T1→T2 | 300 |
| T2→T3 | 550 |
| T3→T4 | 900 |
| T4→T5 | 1300 |
| T5→T6 | 1700 |

因此在士兵健康、领主资金和工资上限允许时：

- `300 XP/兵` 足以让 T0、T1 栈全部升一级，并让 T2/T3/T4/T5 栈分别约
  `54%/33%/23%/17%` 升一级；
- `600 XP/兵` 足以让 T0、T1、T2 栈全部升一级，并让 T3/T4/T5 栈分别约
  `66%/46%/35%` 升一级。

这说明模块提示词采用的 `300～600 XP/兵` 确实能造成明显精锐化，而不是只有 UI 数字变化。
它不会在写 XP 的同一调用中直接升级；实际兵种替换发生在原版下一次
`DailyTickParty`，或该 Party 的 `MapEventEnded`。真实 1.3.15 和 1.4.6 DLL 探针也确认
`PartyUpgraderCampaignBehavior.DailyTickParty(MobileParty)` 与
`UpgradeReadyTroops(PartyBase)` 两个公开入口都存在。

## 真实 LLM 数值判断探针

专用场景 `lord_party_troop_xp` 的候选模块改为 `lordPartyTroopXp`，目标固定为
`H0 = hero:v1:role:lords:k1`。一次性“明显精锐化”应输出 `onceDelta=300～600`、`dailyDelta=0`；
常设训练、明确两阶段和排除语义需要在新的真实 LLM 矩阵中分别复验。
离线消息与目标合同已经通过。

2026-08-21 已使用用户剪贴板显式指定的 `yjapi.manqiaotechnology.com`
与 `gemini-3-flash` 执行一次真实两阶段请求；密钥只在当次进程环境中使用，
未记录提示词、原始回答或密钥。旧版模型正确选择了 `H0`、`lordPartyTroopXpOnce`
和 `{"value":450}`；该结果只证明旧一次性标尺有效，不能替代新 Composite 双字段语义复验。

该请求未通过最终映射合同，原因不是 XP 数值：旧测试政策包含“提供充足经费”，
主阶段因而合理拆出了财政成本腿 `I0L0`，但本场景故意只提供士兵经验模块，后阶段
无法伪造财政模块来映射该腿。已把场景收窄为纯“集中训练→士兵经验”语义并重建测试
EXE（0 警告、0 错误）。严格合同复验尚未发送：第二次执行时剪贴板已不再包含密钥，
工具在发请求前中止，等待用户重新复制。

## 测试范围

无完整 Campaign 环境可测试：

- payload、强度范围和单位；
- 每兵乘算仅执行一次；
- Hero/俘虏/最大阶过滤；
- Party 去重和稳定顺序；
- 溢出、容量饱和、保存体积边界；
- 写入后抛错、部分失败补偿、CAS 冲突；
- 同日重试、读档后幂等、到期前废除。

必须实机验证：

- 伤兵在 `Number` 中的实际表现；
- 中低高阶兵种的明显强度；
- UI 可升级人数和缓存刷新；
- 军团、商队、驻军、民兵、定居点、被俘和无 Party 边界；
- 保存/读档后不重放；
- AI 后续仍使用原版升级流程。
