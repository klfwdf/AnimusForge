# AF / SETS 他方定居点纯士兵随行与现场英雄可指挥接线 HANDOFF（2026-08-29，Addendum F）

## 0. 一句话结论

本轮已经按最终玩家口径替换 Addendum E 的方案：

- 他方城镇、城堡、村庄的手动随行配置仍为 **最多 2 人**；
- 配置界面 **只能选择健康普通士兵**，同伴、家族成员和其他 Hero 一律不进入候选列表；
- 冲突开始后，当前场景里四散的玩家主队同伴和玩家家族 Hero 会在原地退出日常场景行为，加入玩家 Team 与对应原版 Formation，因而可被玩家指挥；
- 不传送 Hero，不重复生成 Hero，不建立独立 AI 支援 Team；
- 本轮已通过 standalone、静态 verifier、Bannerlord 1.3、1.4、Bootstrap 和统一 Stage 构建，但尚未代替玩家进行真实游戏场景验收。

## 1. 本文优先级

本文是 `af-sets-foreign-settlement-followers-20260828e.md` 的后续修订。

Addendum E 中下列设计已明确作废：

1. 他方定居点配置界面允许选择同伴或家族成员；
2. 选中的 Hero 从场景原生位置传送到玩家附近；
3. 未选中的主队 Hero 进入独立 AI Team，不能被玩家指挥；
4. SceneTaunt 为“独立英雄支援”维护单独武装桥。

后续开发、审查和回归应以本文为准，不得恢复上述旧路径。

## 2. 最终玩家契约

| 场景/阶段 | 最终行为 | 明确不做 |
|---|---|---|
| U 键配置他方定居点随行 | 只列出健康、可转移的普通士兵 | 不列出任何 Hero |
| 他方定居点随行上限 | 总计最多 2 名普通士兵 | 不因同伴或家族成员增加上限 |
| 正常进城、尚未开打 | 维持原版场景里的同伴分散生成 | 不提前抓人、不传送、不强制跟随 |
| SETS 他方冲突开始 | 现场主队同伴/玩家家族 Hero 原地加入玩家 Team 和 Formation | 不创建独立支援 Team |
| 冲突进行中 | 两名选中士兵与已接管 Hero 都可通过原版命令界面指挥 | 不让 Hero 继续跑日常场景 AI |
| 冲突后新生成合格 Hero | `OnAgentBuild` 只登记候选，安全 tick 再并入玩家编队 | 不在 build callback 中改 Team、导航或装备 |
| Hero 伤亡 | 交给原版 Hero 生命周期结算 | 不把 Hero 当普通兵从主队 roster 手动扣除 |

## 3. 资格矩阵

### 3.1 配置界面

| 角色 | 是否可选 | 原因 |
|---|---:|---|
| 健康普通士兵 | 是 | 唯一有效候选 |
| 受伤普通士兵 | 否 | 只构建 healthy roster |
| 玩家本人 | 否 | Hero |
| 玩家同伴 | 否 | Hero，不属于手动随行名额 |
| 玩家家族成员 | 否 | Hero，不属于手动随行名额 |
| 其他贵族 Hero | 否 | Hero |
| 俘虏 | 否 | prisoner roster 不可转移 |
| `IsNotTransferableInPartyScreen` 普通角色 | 否 | 保持原版不可转移约束 |

### 3.2 开战后现场英雄接管

Hero 必须同时满足：

1. 不是玩家主 Agent；
2. 当前 Agent 为有效、存活的人类 Agent；
3. Hero 仍属于 `MobileParty.MainParty`，或仍在主队成员 roster 中；
4. Hero 不是俘虏；
5. Hero 是玩家同伴，或 Hero 的 Clan 等于玩家 Clan；
6. 当前 Mission 是活动中的 SETS 他方定居点冲突；
7. 玩家 Team 已建立，且胜利尚未结算。

因此：

- 玩家主队里的普通同伴：接管；
- 玩家家族里、随主队进入场景的 Hero：接管；
- 只是在场景里的友方贵族，但不属于玩家主队：不接管；
- 已离队、已成为俘虏或第三方 Hero：不接管；
- 没有被原版生成进当前场景的同伴：不额外生成，也不传送进来。

## 4. 代码变更表

| 仓库 | 文件 | 关键改动 | 运行边界 |
|---|---|---|---|
| GCCZ + NEW-10 镜像 | `SetsSettlementEntryProfile.cs` | `IsConfigurableRegularFollower` 只接受非 Hero；新增 `ShouldJoinForeignConflictAsCommandableHero`；更新开战提示 | 纯规则，无 TaleWorlds 运行副作用 |
| GCCZ | `Program.cs` | 锁定 2 人上限、Hero 不可选、主队同伴/家族接管、俘虏和非主队拒绝、提示文本 | standalone 契约 |
| NEW-10 | `SettlementEntryTroopSelectionBehavior.cs` | 配置 roster 去掉 profile-specific Hero 例外；新增现场 Hero 候选登记、接管和编队；删除选中 Hero 传送与独立 Team | 仅活动 SETS 他方冲突 |
| NEW-10 | `SceneTauntBehavior.cs` | 删除独立英雄武装桥及宽泛的“任意主队 Hero”fallback；统一使用 SETS commandable follower 注册表 | 防止普通场景主队 Hero 被意外武装 |
| GCCZ | `verify_gccz_town_refactor.ps1` | 验证新接线存在，并明确禁止旧 Hero 可选/传送/独立 Team 路径回归 | 双仓库静态门禁 |

## 5. 运行时顺序

### 5.1 配置阶段

`OpenProfileSelection` 现在按以下顺序工作：

1. 读取玩家主队 roster；
2. `BuildConfigSelectableRoster` 只保留健康普通士兵；
3. 已保存 profile 通过 `SanitizeEntryProfileRoster` 清除历史遗留 Hero；
4. `ResolveProfileRosterForEntry` 再次确认角色仍为普通士兵、仍在主队且健康；
5. 他方 profile 应用 `OtherSettlementSelectedFollowerLimit = 2`；
6. PartyScreen 左侧固定显示“可选健康普通士兵”。

旧存档中如果 Addendum E 时期已经把 Hero 写进随行 profile，首次读取时会被过滤，不会再沿用旧 Hero 选择。

### 5.2 冲突开始

`StartConflict` 的决定性顺序是：

1. 标记 `_conflictActive`；
2. 创建或确认玩家 command Team；
3. 保持两名选中普通士兵在玩家编队；
4. 创建敌方 Team 并切换 Battle mode；
5. 标记当前定居点守卫为敌方；
6. `ReadyScatteredPlayerHeroesForConflict` 单次扫描当前场景 Agent；
7. 合格 Hero 调用 `PrepareScatteredPlayerHeroForCommand`，停止 SceneFollow / DailyBehavior / scripted movement；
8. `RegisterCommandableFollowerRuntimeAgent` 将 Hero 写入 SETS allied 与 commandable follower 注册表；
9. 按角色类型加入玩家 infantry / ranged / cavalry Formation；
10. 维护装备就绪，必要时通过既有 follower fallback 路径补武器并拔出；
11. 给相关 Formation 下初始 follow order，同时保留原版玩家指挥权；
12. 最后刷新原版命令 UI。

Hero 的位置不会被修改；`AssignAgentToFormation` 会设置 `SetShouldCatchUpWithFormation(true)`，所以远处 Hero 会从原地自行赶往当前编队。

### 5.3 冲突 tick 与晚生成 Agent

- `OnAgentBuild` 不直接改 Agent，只把合格 Hero 放进 `_pendingScatteredPlayerHeroAgentsByIndex`；
- `MaintainConflictTeams` 只遍历三个已跟踪集合：pending Hero、allied Agent、enemy Agent；
- pending Hero 在安全 tick 中注册到玩家编队；
- allied Agent 的玩家 Team、Formation、警戒状态和武装状态持续被纠偏；
- periodic maintenance 不每 tick 全量扫描 `Mission.Agents`。

这样既支持原版晚生成同伴，也避免在 Agent build callback 中发生 Team/Formation/导航重入。

## 6. 指挥与装备为什么现在能工作

现场 Hero 接管后不只调用 `SetTeam`，还完成了四层注册：

1. `_alliedAgentIndexes`：SETS 玩家侧身份与伤亡边界；
2. `_alliedAgentsByIndex`：后续 tick 的精确维护集合；
3. `RegisterSetsSelectedFollowerAgent`：复用现有原版命令 UI 和 SETS follower 装备桥；
4. `AssignAgentToFormation(... markPlayerCommandable: true)`：关闭 Formation AI 控制并绑定玩家 owner。

因此新方案不是“Hero 只在阵营上友好”，而是实际进入玩家可下令的 Formation。

武器处理也不再依赖已删除的独立英雄入口：Hero 先进入 commandable follower 注册表，再调用 `MaintainSetsFollowerArmedCombatReadyForExternal`，所以空手 Hero 能走与两名随行士兵一致的装备检查/fallback 路径。

## 7. 已删除的旧代码

| 已删除路径 | 删除原因 |
|---|---|
| `IsConfigurableFollower` 的 Hero 例外 | 与“只能选纯士兵”冲突 |
| `ShouldJoinForeignConflictAsIndependentHero` | 新要求为可指挥，而非独立 AI |
| `TryAdoptExistingSelectedHero` | Hero 不再可选；旧路径还会传送原生 Agent |
| `PrepareSelectedHeroForCommand` | 仅服务旧选中 Hero 传送流程 |
| `_independentHeroSupportTeam` | 不再创建不可指挥的第三方支援 Team |
| `_independentHeroSupportAgentIndexes` | 独立 Team 账本失去用途 |
| `EnsureIndependentHeroSupportTeam` | 独立 Team 创建器失去用途 |
| `TryMaintainIndependentHeroSupportAgent` | 被玩家 command formation 接管替代 |
| `ConfigureIndependentHeroSupportFormation` | 不再需要 AI charge formation |
| `ReleaseIndependentHeroSupportAfterVictory` | 不再存在独立 Team 关系需要解除 |
| `MaintainSetsIndependentHeroArmedCombatReadyForExternal` | commandable follower 装备桥已覆盖 |
| SceneTaunt 对任意主队 Hero 的宽泛 fallback | 防止非 SETS/未注册 Hero 被意外改装 |

verifier 现在把这些名称作为 forbidden evidence；旧路径若被重新加入会直接失败。

## 8. 状态与副作用边界

- 接管只发生在 `_defenderConflictEnabled && _conflictActive && !_victoryReached`；
- 不影响自有定居点普通场景、非 SETS 任务、世界地图或普通 AF 对话；
- 不改变 Settlement 所有权、政治后果或 SETS 胜利状态机；
- 不改变两名普通士兵的既有受伤/伤亡处理；
- Hero 死亡不手动从 `MobileParty.MainParty.MemberRoster` 扣除，交回原版 Hero lifecycle；
- 没有游戏目录部署，没有覆盖 DLL、PDB、ModuleData 或 ONNX。

## 9. 自动验证结果

### 9.1 GCCZ standalone

```powershell
$env:DOTNET_CLI_HOME='G:\AFMOD\.dotnet-home'
& 'G:\AFMOD\.dotnet-sdk\dotnet.exe' run `
  --project 'G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\AnimusForge.SiegeAftermathIntervention.Tests.csproj' `
  -c Debug
```

结果：`All GCCZ standalone core tests passed.`

### 9.2 双仓库 verifier

```powershell
& 'G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1' `
  -StandaloneRoot 'G:\AFMOD\GCCZ' `
  -FusedRoot 'G:\AFMOD\NEW-10'
```

结果：通过；最终统计为 183 个 core source、8 个 player resources、14 个 handoff documents。

### 9.3 Bannerlord 双 API 与 Bootstrap

```powershell
$env:DOTNET_CLI_HOME='G:\AFMOD\.dotnet-home'
$env:PATH='G:\AFMOD\.dotnet-sdk;' + $env:PATH
& 'G:\AFMOD\NEW-10\一键编译覆盖推送\build_single_module.ps1' `
  -ProjectRoot 'G:\AFMOD\NEW-10' `
  -BannerlordRoot 'E:\Steam\steamapps\common\Mount & Blade II Bannerlord' `
  -WorkshopContentDir 'E:\Steam\steamapps\workshop\content\261550' `
  -Configuration Debug `
  -Stage
```

结果：

- Bannerlord 1.3 implementation：0 warnings / 0 errors；
- Bannerlord 1.4 implementation：0 warnings / 0 errors；
- Bootstrap：0 warnings / 0 errors；
- Stage Result：success；
- 输出：`G:\AFMOD\NEW-10\bin\Debug\single_module_stage\AnimusForge`；
- 游戏目录：未修改。

## 10. 必做游戏内验收

自动测试不能证明原版场景 Agent、Formation UI 和武器动画在所有地图模板上都正确。发布前至少逐项验证：

1. 主队准备 2 名以上健康普通士兵、2 名以上同伴/家族 Hero；
2. U 键打开“他方定居点随行”，确认 Hero 完全不出现；
3. 确认最多只能保存 2 名健康普通士兵；
4. 带伤士兵不出现在可选 roster；
5. 进入他方城镇，确认同伴仍按原版分散位置生成，开战前没有瞬移；
6. 触发 SETS 冲突，确认两名士兵和场景内全部合格 Hero 都是玩家侧；
7. 打开原版命令 UI，确认这些 Hero 出现在可选 Formation 人数内；
8. 连续下达跟随、移动、冲锋、停止命令，确认远处 Hero 会响应；
9. 确认 Hero 从原地赶来，而不是瞬移到玩家身边；
10. 测试空手同伴，确认冲突后能获得可用武器并拔出；
11. 确认定居点守军/民兵不会被错误划入玩家侧，主队 Hero 也不会被划入敌方；
12. 测试一个冲突开始后晚生成的主队 Hero，确认下一维护 tick 可被接管；
13. 测试 Hero 倒地/死亡，确认没有重复扣 roster 或破坏原版伤亡状态；
14. 杀尽守军并耗尽 reserve，确认胜利、TAB 和后续结算仍按 SETS 状态机执行；
15. 离开任务后进入普通城镇，确认 AF 普通场景同伴行为未被永久污染。

建议保留关键日志：

```text
Registered scattered main-party hero in the player SETS command formation.
commandable=true, teleported=false
Conflict started. ... scatteredHeroesCommandable=true
```

## 11. 已知风险与未验证项

1. 原版不同城镇/城堡 scene template 的 Hero 生成时机可能不同，必须用至少一个城镇和一个城堡实测晚生成路径；
2. 若第三方 Mod 替换原版 Formation/OrderController，Hero 虽已正确 SetTeam，命令 UI 仍可能需要兼容适配；
3. fallback 武器只在没有可用战斗 loadout 时生效；特殊装备 Mod 的 weapon usage 判定仍需实测；
4. 当前只接管已经存在于场景的主队 Hero，不承诺把未生成的全部主队同伴强制刷入场景；
5. 自动构建通过不等于玩家反馈问题已经在游戏内复现并关闭。

## 12. 提交与回滚

代码提交：

- GCCZ：`48b782e refactor: command scattered heroes in foreign SETS conflicts`
- NEW-10：`c4bfe83f refactor: make scattered SETS heroes commandable`

本轮前双仓库回滚标签：

```text
backup/pre-foreign-settlement-commandable-companions-20260829
```

回滚应使用 `git revert` 对上述提交做可审查反向提交；不要 hard reset，不要改写并发开发历史。

## 13. 后续维护规则

后续如继续优化“别人城镇”SETS，请遵守：

1. 选择资格和提示先改 GCCZ `SetsSettlementEntryProfile` 与 standalone tests；
2. NEW-10 只保留 TaleWorlds Agent/Team/Formation 接线；
3. 不恢复 Hero 手动选择；
4. 不恢复 Hero 传送；
5. 不恢复独立不可指挥 Team；
6. 新增接线必须同时更新 verifier forbidden/evidence；
7. 每轮至少跑 standalone、verifier、1.3/1.4/Bootstrap Stage；
8. 只有完成第 10 节游戏内验收后，才能对外宣称“玩家反馈已修复”。
