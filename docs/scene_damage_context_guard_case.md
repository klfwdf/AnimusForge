# 场景伤害逻辑上下文防误触案例

本文记录 AnimusForge 场景伤害相关逻辑的通用防坑原则。以后新增或修改任何场景内伤害、击中、击倒、死亡、队伍敌对转换、犯罪惩罚、忠诚度惩罚、伤害倍率补丁时，都应先按本文检查上下文 gate。

## 核心教训

不要把“当前有定居点”“定居点属于玩家”“当前 Agent 是 NPC”“玩家造成了伤害”当成机制可以生效的充分条件。

Bannerlord 里很多不同类型的 mission 都可能同时满足这些表面条件：

- 玩家自有城镇街道、村庄、领主大厅等和平 location mission。
- 玩家自有定居点正在攻城或守城的 siege / sally out mission。
- 战斗、藏身点、竞技场、训练场、决斗、部署、潜行等原版战斗或特殊 mission。
- 任务、会面、巷战、原版 MissionFightHandler 自定义 fight。

如果新机制只看 `Settlement.CurrentSettlement`、`MobileParty.MainParty.CurrentSettlement` 或 `settlement.OwnerClan == Clan.PlayerClan`，就很容易把攻城守城、竞技场、藏身点等原版场景误当成和平场景，导致伤害被清零、忠诚度乱扣、死亡被错误延迟、队伍关系被强行改写。

## 硬性原则

1. 先判定机制所属场景，再处理伤害结果。
   `OnAgentHit`、`OnScoreHit`、`OnAgentRemoved`、伤害倍率 postfix、队伍转换和死亡拦截都必须先确认当前 mission 类型。

2. 使用 allowlist，而不是只靠 blacklist。
   如果机制只应在和平定居点内生效，应明确要求 `PlayerEncounter.LocationEncounter != null`、`CampaignMission.Current?.Location != null`，并排除 battle context。不要写成“不是某几个战斗类型就允许”。

3. 原版战斗场景默认放行。
   对攻城、守城、野战、藏身点、竞技场、训练场、决斗、部署、潜行等场景，AnimusForge 的和平场景伤害逻辑默认不应改变伤害、队伍、死亡和惩罚。

4. “回归原版逻辑”不是把伤害改成 0。
   如果一个开关的语义是关闭本模组的和平场景挑衅/冲突机制，就应该让本模组退出处理，把伤害结算交还原版；不要在伤害倍率、OnScoreHit 或 OnAgentHit 里直接把结果改成 0。

5. 队伍敌对转换是高风险操作。
   `Agent.SetTeam`、`Team.SetIsEnemyOf`、`Mission.PlayerTeam`、`MissionFightHandler` 旧队伍缓存等修改只应在明确属于本机制的 mission 内执行，并且要记录原队伍，结束时恢复。

6. 后续维护可以依赖已初始化状态，但初始化必须严格。
   如果机制已经在合法和平场景里初始化，可以允许后续 tick、击倒、死亡延迟、姿势维护继续完成。但第一次初始化必须通过严格场景 gate，不能在攻城/守城等战斗 mission 内初始化。

## 推荐上下文 Gate

和平定居点伤害机制至少应同时满足：

- `Mission.Current != null`
- `PlayerEncounter.LocationEncounter != null`
- `CampaignMission.Current?.Location != null`
- 当前 settlement 能和 `PlayerEncounter.LocationEncounter.Settlement` 对上，或至少不是战斗 map event 推导出的 settlement。
- 没有活动中的 `PlayerEncounter.Battle` / `PlayerEncounter.EncounteredBattle` / `MapEvent.PlayerMapEvent`。
- mission 没有 `CampaignSiegeStateHandler`。
- `Mission.MissionTeamAIType` 不是 `Siege`、`SallyOut`、`FieldBattle`。
- `Mission.Mode` 不是 `Deployment`、`Stealth`、`Duel`。
- location 不是 `arena`、`training_field` 这类原版特殊战斗/练习场。
- 当前 settlement 不处于 `IsUnderSiege`，除非该机制明确就是为了攻城场景设计。

具体机制可以继续增加更窄的条件，例如只允许城镇 center、村庄 center、领主大厅或酒馆，但不要放宽上述 battle context 排除。

## 典型反例

不要这样写：

```csharp
private bool IsOwnedSettlementScene()
{
    Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
    return settlement != null && settlement.OwnerClan == Clan.PlayerClan;
}
```

这会把玩家自有定居点上的攻城守城也判成“自有定居点和平场景”。

不要这样写：

```csharp
if (!settings.EnablePeaceSceneConflict)
{
    __result = 0f;
}
```

这不是回归原版，而是覆盖原版伤害，竞技场、藏身点、战斗场景都可能被误伤。

## 推荐模式

```csharp
private bool IsFeaturePeaceLocationScene()
{
    Mission mission = Mission.Current;
    Settlement settlement = ResolveFeatureSettlement();
    if (mission == null || settlement == null)
    {
        return false;
    }
    if (IsFeatureAlreadyInitialized())
    {
        return true;
    }
    if (IsCampaignBattleContext(mission, settlement))
    {
        return false;
    }
    if (PlayerEncounter.LocationEncounter == null || CampaignMission.Current?.Location == null)
    {
        return false;
    }
    if (PlayerEncounter.LocationEncounter.Settlement != null && PlayerEncounter.LocationEncounter.Settlement != settlement)
    {
        return false;
    }
    return IsAllowedFeatureLocation(CampaignMission.Current.Location.StringId);
}
```

关键点：

- 初始化前必须走完整 context gate。
- 已初始化状态只用于继续完成本机制，不用于打开新机制入口。
- `IsCampaignBattleContext(...)` 要集中维护，不要在多个伤害入口复制半套条件。
- 机制关闭时应 `return`，不要改写原版伤害。

## 本次事故复盘

自有定居点被动攻击链路原本只判断：

```csharp
settlement != null && Clan.PlayerClan != null && settlement.OwnerClan == Clan.PlayerClan
```

结果玩家在自己的定居点攻城或守城时，攻击任何 NPC 都会进入和平场景惩罚链路，造成忠诚度扣除。

修复方式是把入口收紧为“自有定居点的和平 location mission”，并排除 battle / siege / sally out / field battle / deployment / stealth / duel / arena / training_field / settlement under siege 等上下文。这样攻城守城回归原版战斗逻辑，和平定居点内攻击普通 NPC 的新链路仍然可用。

## 新增伤害逻辑检查清单

新增或修改以下代码时，必须检查本文：

- `OnAgentHit`
- `OnScoreHit`
- `OnAgentRemoved`
- `SandboxMissionDifficultyModel` 或其他伤害倍率 patch。
- `Agent.SetTeam`、`Team.SetIsEnemyOf`、`Mission.PlayerTeam`。
- `MissionFightHandler.StartFight` / `StartCustomFight` / 原队伍缓存修正。
- 死亡压制、死亡延迟、昏迷转死亡、名人死亡处理。
- 犯罪、关系、忠诚度、安全度、繁荣度等后果。
- “关闭后回归原版”的 MCM 开关。

每次至少确认：

- 这个机制属于和平场景、战斗场景、meeting 场景、犯罪巷战，还是专用任务场景。
- 是否有明确 allowlist。
- 是否明确排除了 battle context。
- 关闭开关时是否只是退出本模组处理，而不是改变原版结果。
- 是否会影响藏身点、竞技场、攻城守城、训练场、决斗等原版场景。
- 是否有日志能看出命中的 mission mode、location id、settlement、battle context 和目标 agent。

## 给 Codex 的调用方式

以后可以直接说：

```text
按场景伤害上下文防误触案例，检查这段 OnScoreHit / OnAgentHit / 伤害倍率逻辑。
```

或：

```text
这个机制只应该在和平定居点生效，按 scene damage context guard 兼容攻城、竞技场、藏身点和原版战斗。
```

Codex 应先阅读本文，再检查相关 C# 入口和 TaleWorlds 原版 mission 代码。
