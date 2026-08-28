# SETS 他方定居点两人随行与英雄自主助战 Handoff

Date: 2026-08-28 (Addendum E)

前置文档：

- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-progress-20260828d.md`
- `G:\AFMOD\GCCZ\docs\handoff\af-sets-noble-full-integration-progress-20260828c.md`

本文件记录他方城镇/城堡/村庄的随行选择新契约，以及原版随机散布的同伴、家族成员在 SETS 冲突中的两种不同运行身份。

## 1. 玩家向结果

他方定居点随行配置现在总共只有两个明确选择名额。两个名额共享同一上限，可选择：

- 健康普通士兵；
- 玩家同伴；
- 玩家家族成员。

玩家本人、俘虏、受伤成员、离队成员和无关贵族不能进入这两个名额。

英雄进入场景后分两类：

| 身份 | 场景开始 | 冲突开始 | 玩家能否指挥 |
|---|---|---|---:|
| 被选中的同伴/家族成员 | 从原版散布点拉回玩家附近，加入所选随行 formation 并跟随玩家 | 与所选士兵一起保持玩家侧、拔出武器 | 是 |
| 未被选中但已由原版生成在场景中的主队同伴/家族成员 | 保持原版随机位置，不强制跟随 | 在当前位置进入独立 AI 支援队，自主寻找并攻击 SETS 守军 | 否 |

因此“选中英雄”不再只是保存一个名字，而是明确获得归队、formation 和 order-controller 权限；“散落英雄”只获得战斗阵营和自主 AI，不会被玩家的 1–9 编队命令带走。

## 2. 两人上限

纯契约：

```csharp
SetsSettlementEntryProfile.OtherSettlementSelectedFollowerLimit = 2;
```

AF adapter：

```csharp
OtherSettlementEntryLimit = SetsSettlementEntryProfile.OtherSettlementSelectedFollowerLimit;
```

这是总名额，不是“两名士兵外加无限英雄”。合法组合示例：

- 2 名普通兵；
- 1 名普通兵 + 1 名同伴；
- 1 名同伴 + 1 名家族成员；
- 2 名家族/同伴英雄。

旧存档里的他方 profile 若超过 2 人，会由现有 sanitize 流程按 roster 顺序截断到 2 人；不会继续偷偷带入第 3–10 人。玩家可按 U 重新配置。

自有定居点 profile 的 100 人契约没有改变，并继续只接受普通士兵；本轮英雄选择只开放给他方 profile。

## 3. 候选资格

纯策略入口：

```csharp
SetsSettlementEntryProfile.IsConfigurableFollower(...)
```

规则：

1. 普通兵仍可选择。
2. Hero 只有在他方 profile 且满足 `hero.Clan == Clan.PlayerClan` 或 `hero.IsPlayerCompanion` 时可选择。
3. `Hero.MainHero` 永远拒绝。
4. 运行时仍从 `MobileParty.MainParty.MemberRoster` 构造候选，因此不把军团中其他领主、城内本地贵族或远处家族成员伪造为随行。
5. Hero 不再因为 `CharacterObject.IsNotTransferableInPartyScreen` 被这个“配置用 dummy roster”误排除；玩家本人仍由纯策略单独拒绝。
6. 保存时重新对当前主队 roster、健康数量和 profile kind 做 live validation，不能靠旧 UI 数据带入已离队英雄。

## 4. 被选中英雄的运行链

入口：

```text
TrySpawnSelectedAllies
  -> ExpandRoster(selectedRoster, limit=2)
  -> SpawnAgentsNearPlayer
       -> Hero: TryAdoptExistingSelectedHero
       -> regular/missing Hero: SpawnAgent fallback
```

### 4.1 优先接管原版 Agent

选择项是 Hero 时，先按 `HeroObject` 在 `Mission.Agents` 中查找原版已经生成的活跃 Agent：

```text
TryAdoptExistingSelectedHero
  -> PrepareSelectedHeroForCommand
  -> TeleportToPosition(玩家后方安全格)
  -> RegisterSelectedFollowerRuntimeAgent
```

这样不会因为同伴原版出生在城镇另一端，就只让随机的一名原版 follower 靠近玩家。

`PrepareSelectedHeroForCommand` 会：

- 停止 AF/原版旧跟随脚本；
- 解除 AI pause；
- 清除 scripted movement、target frame 和 location daily behavior；
- 恢复速度；
- 随后加入玩家 Team 与按武器类型解析出的 infantry/ranged formation。

若场景中确实没有该 Hero Agent，才使用已有 `AgentBuildData` 路径在玩家附近生成一个战斗装备代理。这个 fallback 需要实机检查第三方场景模组是否会在更晚时刻再次生成同一 Hero。

### 4.2 可指挥身份

所有被选中的士兵和英雄统一登记到：

- `_alliedAgentIndexes`
- `SetsSelectedFollowerAgentIndexes`

然后只有该 registry 会被：

- `EnsureSetsCommandUiReadyForExternal` 纳入命令 UI；
- `AssignAgentToFormation` 标记为 player-commandable；
- `TrySetFollowerFormationFollowOrder` 下达跟随玩家命令；
- `MaintainProtectedFollowersFriendlyState` 保护免受玩家侧误伤导致的阵营叛变。

## 5. 未选中英雄的自主助战链

纯策略入口：

```csharp
SetsSettlementEntryProfile.ShouldJoinForeignConflictAsIndependentHero(...)
```

必须同时满足：

- 没有出现在 selected roster；
- 是玩家主队 Hero；
- 是玩家家族成员或玩家同伴；
- 不是俘虏；
- 当前是他方 SETS defender conflict。

运行入口：

```text
StartConflict
  -> ReadyIndependentPlayerHeroesForConflict
  -> TryMaintainIndependentHeroSupportAgent
  -> EnsureIndependentHeroSupportTeam
  -> PrepareIndependentHeroSupportAgent
```

### 5.1 为什么使用独立 Team

如果把散落英雄直接塞进 `_playerTeam` 的 infantry/ranged formation，那么同一 formation 被标记为 player-commandable 后，玩家仍可选中并命令这些英雄，违背需求。

本轮为每个 Mission 最多创建一个：

```text
_independentHeroSupportTeam
isPlayerGeneral = false
isPlayerSergeant = false
```

关系：

- 与玩家 Team 双向非敌对；
- 与 SETS enemy Team 双向敌对；
- 与 victory neutral Team 双向非敌对。

该 Team 没有玩家 OrderController。它自己的 formations 保持 `SetControlledByAI(true, false)` 并收到 `MovementOrderCharge`，所以英雄会自主参战但不能接受玩家编队命令。

### 5.2 保留散落位置

未选中英雄不会执行 `TeleportToPosition`。冲突前仍由原版放在酒馆、街道或其他场景位置；开打后在当前位置解除 daily behavior、进入战斗 AI 并向敌人移动。

每秒 `MaintainConflictTeams` 会重新发现晚出现的合格 Hero，并维持独立 Team/敌对关系，避免原版 location behavior 或其他 scene tick 把他们切回和平状态。

### 5.3 武器

新增：

```csharp
SceneTauntMissionBehavior.EnsureSetsIndependentHeroArmedCombatReadyForExternal(...)
```

它会清理旧目标、报警、选择真实武器；主队 Hero 若没有可用真实武器，也允许使用现有 fallback sword 逻辑。独立英雄不需要伪装成 selected follower 才能拔刀，因此不会被命令 UI 错收。

## 6. 胜利与伤亡边界

### 6.1 胜利

`ReleaseIndependentHeroSupportAfterVictory` 会：

- 清除独立支援 Team 与 enemy Team 的敌对；
- 清理每名支援英雄的目标缓存；
- 恢复 Patrolling watch state；
- 随后原 SETS victory exit 继续处理敌人 neutralization 和 Mission 退出。

### 6.2 选中英雄伤亡

旧 `SettleAlliedCasualty` 会直接从 `MobileParty.MainParty.MemberRoster` 减 1，这对普通士兵正确，但对 Hero 会和原版 Hero death/casualty 生命周期重复。

现在：

- 普通兵：继续由 SETS ledger 幂等后从主队 roster 减 1；
- Hero：只从 SETS `_survivingRoster` 移除，主队/死亡状态交给 Bannerlord 原版 Hero lifecycle；
- 不手工二次移除家族成员或同伴。

未选中英雄从未进入 `_alliedAgentIndexes`，因此也不会误走普通士兵 casualty deduction。

## 7. 代码表

| Repo | 文件 | 作用 |
|---|---|---|
| GCCZ | `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention\SetsSettlementEntryProfile.cs` | 两人上限、英雄选择资格、独立助战资格、玩家提示。 |
| GCCZ | `G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\Program.cs` | 资格矩阵、上限、selected/independent 互斥与提示测试。 |
| GCCZ | `G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1` | 镜像、常量、adoption、独立 Team、武器与 native Hero casualty 证据。 |
| NEW-10 | `G:\AFMOD\NEW-10\AnimusForge.SiegeAftermathIntervention\SetsSettlementEntryProfile.cs` | GCCZ 纯策略镜像。 |
| NEW-10 | `G:\AFMOD\NEW-10\SettlementEntryTroopSelectionBehavior.cs` | UI/profile、原版 Hero adoption、可指挥 registry、独立 AI Team、胜利释放和伤亡分流。 |
| NEW-10 | `G:\AFMOD\NEW-10\SceneTauntBehavior.cs` | 未选中主队 Hero 的武器准备桥。 |

## 8. 提交与回滚

功能提交：

| Repo | Commit | 内容 |
|---|---|---|
| GCCZ | `7ce2e3f` | 两人上限、Hero 资格、独立支援纯策略与 tests。 |
| GCCZ | `e953a7d` | 明确 selected/independent 玩家提示并补测试。 |
| NEW-10 | `aa030c78` | Profile 镜像、配置 UI、Hero adoption、独立 AI Team 主接线。 |
| NEW-10 | `c85ccd46` | Hero transferable 特判、独立英雄 daily behavior/武器、native casualty 分流。 |
| NEW-10 | `f4c96188` | 删除无用 adoption out 参数。 |
| NEW-10 | `5abeb7fe` | 镜像 selected/independent 玩家提示。 |

接线前标签：

```text
backup/pre-foreign-settlement-hero-followers-20260828
```

回滚使用 `git revert`，不要 hard reset。工作期间存在另一个城堡会面/本地化会话，相关提交不得为回退本功能而删除。

## 9. 已验证

1. GCCZ standalone tests：全部通过。
2. `verify_gccz_town_refactor.ps1`：通过。
3. Bannerlord API 1.3：0 warning / 0 error。
4. Bannerlord API 1.4：0 warning / 0 error。
5. Bootstrap：0 warning / 0 error。
6. project-local unified stage：成功。
7. 没有覆盖游戏目录。
8. `SetsSettlementEntryProfile.cs` 双仓库 normalized hash 一致。

编译只能证明 Bannerlord API 和代码组合合法，不证明原版所有 town scene template 的 Agent 出生时序与 AI 寻路。

## 10. 必做实机矩阵

| 编号 | 配置/场景 | 必须结果 |
|---|---|---|
| F-01 | 他方城镇选 2 普通兵 | 两人出现在玩家附近、跟随且可指挥；第 3 人无法保存。 |
| F-02 | 选 1 同伴 + 1 家族成员 | 两个原版散落 Agent 被拉回玩家附近；命令 UI 可选中其 formations。 |
| F-03 | 选 1 普通兵 + 1 同伴 | 两人共享 2 人上限，均跟随和可指挥。 |
| F-04 | 场景另有 2 名未选同伴 | 冲突前保持散落；冲突后从原地赶来助战；玩家命令不改变其 AI formation。 |
| F-05 | 未选家族成员没有武器 | 开战后应获得/拔出 fallback sword，不抱头或继续逛街。 |
| F-06 | 玩家误伤 selected Hero | 不得变成敌人；不得重复扣主队 Hero roster。 |
| F-07 | selected Hero 被守军击倒/杀死 | 原版 Hero 状态只结算一次，SETS 不手工二次移除。 |
| F-08 | 胜利后等待自动退出 | 独立英雄停止攻击；不得继续追砍 neutralized defender。 |
| F-09 | 自有城镇配置 | 上限仍为 100 且只列普通兵；本轮不能改变自有 profile。 |
| F-10 | 读取旧 10 人他方 profile | 自动截为 2 人并提示可重新配置，不得带入 3–10 人。 |
| F-11 | 同伴原版 Agent 未出现 | fallback 只生成一个 Hero 代理；观察是否有晚生成 duplicate。 |
| F-12 | 与贵族处决/决斗同场 | 独立支援 Team 不得污染 execution isolation teams 或把全城变敌人。 |

重点日志：

```text
Adopted native scattered hero into selected SETS formation
Registered unselected player hero as independent SETS support
independentHeroesCommandable=false
Selected hero casualty left to native hero lifecycle
```

## 11. 下一步

1. 先跑 F-01–F-12，保存 `SETS.log`、存档、settlement id 和 scene kind。
2. 若 Hero fallback 出现 duplicate，优先延长 adoption 等待窗口或接 MissionLocationLogic 的完整出生回调，不得用杀死 duplicate 的方式掩盖。
3. 若独立英雄仍受玩家命令，检查其 live Team 是否被其他 behavior 拉回 `_playerTeam`，不要把其登记进 selected registry。
4. 若独立英雄不攻击，先检查 daily behavior 是否真正停用、support/enemy 双向敌对和 formation AI order，再改寻路。
5. 通过后再继续 Addendum D 的 SETS completion pump，不要同时重写 ownership/native menu。
