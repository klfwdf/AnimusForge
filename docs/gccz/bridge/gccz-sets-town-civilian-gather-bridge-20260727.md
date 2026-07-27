# GCCZ / SETS 城镇平民召集桥（2026-07-27）

## 独立策略

- `SiegeCivilianGatherInteractionProfile.NativeCommandFormationClassIndex = 2`：GCCZ 召集民众进入原版指挥界面的 3 号编队。
- GCCZ 旗手继续留在 `FormationClass.Ranged`（2 号编队），不得与民众混编。
- `SetsTownCivilianGatherProfile`：普通 SETS 自有/附属城镇场景中，识别玩家明确的召集平民命令，并把成年平民编入 3 号民众编队。
- `SiegeLocalAttackProfile.ShouldNormalizeCivilianHealth(...)`：只为活动中的 GCCZ 场景提供异常生命值判断，不修改普通 AF/原版场景。

## NEW-10 薄桥触点

- `SettlementEntryTroopSelectionBehavior.cs`
  - 只在 SETS 接管的自有/附属城镇中心、且尚未发生内部冲突时召集成年平民。
  - 对话/API 回调线程只登记纯数据请求，不得直接读取或修改 `Mission`、`Agent`、`Team`、`Formation`。
  - 请求由 `OnMissionTick` 主线程消费；退出对话后延迟 0.8 秒，以每批 8 人、间隔 0.12 秒的方式编入玩家 3 号编队。
  - 每名平民必须先成功切换到玩家 `Team`，再挂接玩家编队；全部批次完成并额外稳定 0.25 秒后，才统一下达跟随、松散、停火和指挥 UI 刷新。
  - 一旦触发自有/附属城镇冲突，取消召集状态，继续走既有逃散/处置逻辑。
- `SiegeAiInterventionBehavior.cs`
  - GCCZ 民众编队读取独立策略，不再硬编码到 2 号编队。
  - 普通城镇的明确玩家命令或 `[ACTION:召集]` 只转发给 SETS 召集桥。
  - 玩家攻击 GCCZ 平民前再次确保其为可死亡状态；仅当生命值明显超过角色正常值时才校正。
  - 传令者使用原版 `ScriptBehavior.AddAgentTarget` 前先确认 `CampaignAgentComponent`、导航器和 `DailyBehaviorGroup` 可用；缺失时回退到已有目标点移动，避免重复空引用。

## 隔离约束

- 普通 SETS 召集不激活 GCCZ，不创建攻城处置结果，也不改变城镇所有权。
- 非自有/非附属城镇、城堡、村庄、后巷战斗、已经发生的 SETS 内部暴乱均不接受普通召集。
- 不得在后台对话线程即时批量调用 `Agent.Formation` / `TryAttachToFormation()`；也不得把仍属于城镇中立队伍的平民直接挂入玩家编队。
- SETS 胜利菜单必须等待真正返回 `MapState` 后再打开，避免菜单藏在尚未结束的任务场景后面。
