# AF 0.9.1 / GCCZ BUG 修复专用 HANDOFF

更新时间：2026-06-21

## 本轮定位

这是给后续线程专门修 BUG 用的交接文档，不是功能设计 handoff。

当前 0.9.1 目录角色：

- `G:\AFMOD\ym0.9.1\Mount-Blade-Bannerlord-AnimusForge-mod-main`
  - AF 0.9.1 上游源码位置。
  - 只作为新版 AF 对照源，不要在这里直接做 GCCZ 融合修复，除非用户明确要求。
- `G:\AFMOD\new-0.9.1\Mount-Blade-Bannerlord-AnimusForge-mod-main`
  - AF 0.9.1 + GCCZ 已融合的编译/测试树。
  - 当前 0.9.1 修 BUG 的主要落点。
  - 这里已经包含新版 GCCZ，不要把旧 `G:\AFMOD\GCCZ` 整包搬进来覆盖。
- `G:\AFMOD\GCCZ`
  - GCCZ 独立源码/文档镜像区。
  - 后续凡是改到 `new-0.9.1` 中 `AnimusForge.SiegeAftermathIntervention`、GCCZ 规则、handoff、bridge 契约，都要同步回这里。

关键同步原则：

> 0.9.1 修 BUG 时，以 `new-0.9.1` 里的新版融合 GCCZ 为当前基线；修完后把同一份 GCCZ 相关改动同步到 `G:\AFMOD\GCCZ`。不要反过来把旧 GCCZ 整包覆盖到 `new-0.9.1`。

## 当前线程底层限制 / 操作边界

本线程当前权限层：

- 文件系统：`danger-full-access` / unrestricted。
- 网络：enabled。
- approval：`never`，不能请求交互批准。
- shell：PowerShell。
- 当前工作根：`G:\AFMOD`。

但仍必须遵守项目边界：

- 不 hard reset、不 force push、不删除参考 dump、不覆盖游戏目录，除非用户明确要求。
- 修改 GCCZ 相关内容时，必须保持 `new-0.9.1` 与 `GCCZ` 的 GCCZ 部分同步。
- `ym0.9.1` 是上游对照源，默认只读。
- AF 主体文件只放桥接、guard、runtime 对象查找、数据传递和副作用；GCCZ 规则/文案/策略/契约常量优先放 `AnimusForge.SiegeAftermathIntervention`。
- 判断冲突时，以实际运行 DLL/API 和日志为准；源码、注释和旧 handoff 只能解释，不能推翻运行时证据。

## 已知 BUG 与修复契约

### BUG 1：自行处置入口 `OpenTroopSelection` 运行时签名不匹配

现象：

- 攻城/平叛后点“亲自进城决定/自行处置”失败。
- 日志出现 `EnterIntervention failed: System.MissingMethodException`。
- 找不到带 `List<TaleWorlds.CampaignSystem.Naval.Ship>` 的 `MenuContext.OpenTroopSelection(...)`。

根因：

- 某些 1.3.x/本机 1.4.x 运行 DLL 只有 6 参数 `OpenTroopSelection`。
- 源码或上游分支可能按带船只参数的 API 编译/调用。

修复要求：

- `SiegeAiInterventionBehavior.TryOpenInterventionTroopSelection(...)` 不能硬绑定单一新签名。
- 桥接层要按运行时 API 做兼容：有 ship-aware 版本时用新版；没有时回退 6 参数版本。
- `EnterIntervention` catch 必须释放 GCCZ aftermath guard，不能让玩家卡在战后菜单。
- GCCZ 独立区保留以下契约常量/方法，供 AF bridge 记录释放来源：
  - `SiegeAftermathTransitionSourceProfile.ResetInterventionEntryFailedSource`
  - `SiegeAftermathTransitionSourceProfile.BuildResetStaleEntryGuardSource(...)`

### BUG 2：原版三个战后按钮生效但 UI 不跳转

现象：

- 原版毁坏 / 掠夺 / 宽恕效果已结算。
- 页面仍停在攻城战后菜单，像是按钮“没反应”。
- 日志可能反复出现类似：
  - `Suppressed native siege aftermath SwitchToMenu activation after GCCZ intervention. Menu=siege_aftermath_contextual_summary`

根因：

- GCCZ 的 native aftermath menu guard 把 `siege_aftermath_contextual_summary` 拦掉了。
- 实际 AF/GCCZ 处置未成功接管或 guard 已过期。

修复要求：

- 只有在 AF/GCCZ 处置流程真实进入、真实完成、或确有 pending transition 时才压制原版 summary。
- 如果只是 stale `WaitingDecision` / stale entry guard，必须：
  1. `ResetAftermathRuntimeGuards(SiegeAftermathTransitionSourceProfile.BuildResetStaleEntryGuardSource(menuId))`
  2. 放行原版 `SwitchToMenu`。
- 玩家点原版三个按钮时，不能因为 GCCZ entry guard 残留而吞掉原版 summary。

### BUG 3：`TriggerImmediateSceneBehaviorReactionForExternal` 返回类型不能退回 void

当前 `new-0.9.1` 中：

- `ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(...)` 返回 `bool`。
- `SiegeAiInterventionBehavior` 等调用点依赖该返回值，例如：
  - `if (!TriggerImmediateSceneBehaviorReactionForExternal(...)) { continue; }`
  - `return TriggerImmediateSceneBehaviorReactionForExternal(...);`

修复要求：

- 不要把它改回 `void`。
- 如果上游 AF 改了签名，围绕调用点统一更新，保持“是否成功入队/触发”的布尔语义。

### BUG 4：辅助模型参数名 / thinking 控制参数变动

当前 `new-0.9.1` 中：

- `AIConfigHandler.BuildAuxiliaryRouterRequestJsonForExternal(...)` 仍有：
  - `disableThinkingControls`
  - `useConfiguredMaxTokens`

修复要求：

- 如果后续上游参数名再次变化，不要只改单个调用点。
- 更新签名和所有调用点，保持“是否禁用 thinking 控制”和“是否使用配置 max tokens”两个语义不混。
- 典型调用风险：`DuelSettings.cs`、后处理辅助判断、GCCZ 标签判断。

### BUG 5：Bannerlord 1.4.x 本机 DLL 可能低于源码参考 API

已遇到过的缺口包括：

- `MobilePartyAIModel.FortificationPortPatrolDistanceAsDays`
- `MobilePartyAIModel.GetSettlementNearbyThreatAndAllyCheckRadius(Settlement,bool)`
- `SetPartyAiAction.GetActionForRaidingSettlement(..., isTargetingPort)`
- `EncounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty`
- `EncounterModel.NeededMaximumLandDistanceForEncounteringMobileParty`
- `MobileParty.GetHeroPartyRoles`
- `ITradeAgreementsCampaignBehavior.HasTradeAgreement(..., out ...)`
- `Mission.SpawnTroop` 新参数布局

修复要求：

- 1.4 构建不要盲信 `原版游戏本体代码1.4.5` 参考 dump。
- 以实际游戏 DLL / 编译引用为准。
- 能用旧签名兼容就用旧签名；只有必要时才加 `#if BANNERLORD_1_4_OR_GREATER`。

## 当前同步状态

本 handoff 写入时的目标：

- `G:\AFMOD\new-0.9.1\Mount-Blade-Bannerlord-AnimusForge-mod-main\AnimusForge.SiegeAftermathIntervention`
- `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention`

应保持源码级一致，排除 `bin/`、`obj/` 等构建产物。

已明确保留的 BUG 修复契约：

- `SiegeAftermathTransitionSourceProfile.ResetInterventionEntryFailedSource`
- `SiegeAftermathTransitionSourceProfile.ResetStaleEntryGuardSourcePrefix`
- `SiegeAftermathTransitionSourceProfile.BuildResetStaleEntryGuardSource(...)`

相关桥接文档：

- `G:\AFMOD\GCCZ\docs\bridge\af090-siege-aftermath-runtime-compat-bridge.md`
- `G:\AFMOD\new-0.9.1\Mount-Blade-Bannerlord-AnimusForge-mod-main\docs\gccz\bridge\af090-siege-aftermath-runtime-compat-bridge.md`

## 推荐修 BUG 流程

1. 先看日志和实际 DLL/API，不先猜源码。
2. 在 `new-0.9.1` 融合树里修复可编译/可运行路径。
3. 如果改的是 GCCZ 独立区：
   - 同步到 `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention`。
4. 如果改的是 AF bridge：
   - 保持修改薄、集中、可回退。
   - 在 `G:\AFMOD\GCCZ\docs\bridge` 或本 handoff 记录契约，避免下次融合丢失。
5. 运行最小验证：
   - `rg` 查冲突标记和临时废代码。
   - GCCZ 独立测试（能跑则跑）。
   - fused tree 编译（若本轮改了 runtime C#）。
6. 两边分别提交本地 git。

## 不要做

- 不要把旧 `G:\AFMOD\GCCZ` 整包覆盖进 `new-0.9.1`。
- 不要把 GCCZ 业务规则塞进 `ShoutBehavior.cs`、`MyBehavior.cs`、`SiegeAiInterventionBehavior.cs` 这类 AF 大文件；这些文件只做桥接。
- 不要为了绕过菜单卡死直接吞掉原版 aftermath summary。
- 不要把空回或 RPM 限制伪装成固定兜底台词；用户之前明确不要“确定性短句兜底直接入队”。

## 快速核对命令

```powershell
# GCCZ 独立区源码差异，排除 bin/obj
$a = 'G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention'
$b = 'G:\AFMOD\new-0.9.1\Mount-Blade-Bannerlord-AnimusForge-mod-main\AnimusForge.SiegeAftermathIntervention'

# 关注关键 BUG 契约
rg -n "ResetInterventionEntryFailedSource|BuildResetStaleEntryGuardSource|TriggerImmediateSceneBehaviorReactionForExternal|OpenTroopSelection|useConfiguredMaxTokens|disableThinkingControls" `
  'G:\AFMOD\new-0.9.1\Mount-Blade-Bannerlord-AnimusForge-mod-main' `
  'G:\AFMOD\GCCZ'
```
