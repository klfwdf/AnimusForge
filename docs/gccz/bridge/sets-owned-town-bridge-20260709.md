# SETS 自有/附属城镇处置桥接说明（2026-07-09）

## 背景

`G:\AFMOD\NEW-10` 的 SETS 城镇入口现在分为两类：

- 他方/敌对城镇：玩家带入配置士兵，触发内部暴乱，击溃守卫/驻军/民兵/驻城领主部队后进入原版围城胜利菜单。
- 自有/附属城镇：玩家带入配置士兵；若玩家在城镇中攻击 NPC，不刷守卫、不启动 SETS 后备波次，平民逃跑，玩家可 TAB 退出并进入原版围城胜利菜单。

## GCCZ/AF 桥接约束

- 自有/附属城镇从 SETS 进入原版 `menu_settlement_taken` 时，必须保留当前城主，不应把附属领主城镇转给玩家家族。
- SETS 随行士兵身份以本次 mission 内注册的 agent index 白名单为准；不要按普通场景阵营或同 troop 模板把无关 NPC 直接当随行，现有场景 agent 不再用同 troop 模板自动挂成 SETS 随行。
- 玩家侧误伤 SETS 随行士兵时，`OnAgentHit` 与 `OnScoreHit` 都必须恢复随行士兵友军状态并清理敌对目标，避免 AF/SceneTaunt/原版伤害路径让随行士兵反目。
- `SiegeAiInterventionBehavior.TryOpenSettlementEntryVictoryMenu(...)` 因此带 `transferOwnership` 参数：
  - `true`：他方/敌对城镇 SETS 胜利，按原 SETS 捕获逻辑转移给玩家家族。
  - `false`：自有/附属城镇 SETS 事件，仅打开原版胜利菜单/GCCZ 入口，不执行占领转移。
- 打开菜单前会重置旧 GCCZ runtime guard，再重新准备本次 SETS 菜单上下文，避免同城旧 resolved/pending 状态误拦截 native menu。

## 当前 fused patch 点

- `G:\AFMOD\NEW-10\SettlementEntryTroopSelectionBehavior.cs`
  - 自有/附属城镇 SETS 不启用守军刷兵镇压。
  - 玩家攻击 NPC 后标记 SETS 事件并让平民逃跑。
  - TAB 退出后排队原版胜利菜单，且 `skipOwnershipTransfer=true`。
- `G:\AFMOD\NEW-10\SiegeAiInterventionBehavior.cs`
  - `TryOpenSettlementEntryVictoryMenu(..., transferOwnership)` 控制是否执行 SETS 占领转移。
- `G:\AFMOD\NEW-10\SceneTauntBehavior.cs`
  - SETS 自有/附属城镇入口 active 时，抑制 SceneTaunt 普通街头冲突接管，避免刷守卫/敌对队伍。
