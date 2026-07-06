# GCCZ 线程 handoff：AF 1.09/latest 对攻城处置影响审计

生成时间：2026-07-06

## 背景

本线程专门处理 SETS。GCCZ 相关只做影响审计与 handoff，不在本线程继续扩展 GCCZ 本体。

当前源码基线：

- 上游 AF checkout：`G:\AFMOD\Mount-Blade-Bannerlord-AnimusForge-mod`
- 上游最新提交：`89efa425 auto: pull+build+push 周一 2026/07/06 9:56:03.26`
- SETS 融合树：`G:\AFMOD\NEW-10`
- SETS 分支：`codex/new-10-sets`
- `G:\AFMOD\NEW-10` 已合并上游 `origin/main` 到 `89efa425`。

## AF 1.09/latest 本轮上游主要变化

相对旧基线 `b69eaf12`，上游新增 3 个提交：

| 提交 | 摘要 | 主要改动区域 |
|---|---|---|
| `b3f6b117` | 修bug | `RuleBehaviorPrompts.json`、`MyBehavior.cs`、`NobleGatheringBehavior.cs` |
| `89efa425` | auto pull/build/push | `RuleBehaviorPrompts.json` 小修 |

没有直接改动：

- `SiegeAiInterventionBehavior.cs`
- `AfGcczShoutBridge.cs`
- `SceneTauntBehavior.cs`
- `ShoutBehavior.cs`
- `SubModule.cs`

## GCCZ 规则现状

`G:\AFMOD\NEW-10\AnimusForge\ModuleData\RuleBehaviorPrompts.json` 中仍存在被动规则：

- `Id`: `siege_intervention_aftermath`
- `Group`: `scene`
- `Priority`: `95`
- `TriggerKeywords`: 空数组（保持“只由运行时注入”设计）
- `PostprocessRules` 标签：
  - `[ACTION:宽恕]`
  - `[ACTION:救济]`
  - `[ACTION:宣抚]`
  - `[ACTION:盟誓]`
  - `[ACTION:安兵]`
  - `[ACTION:召集]`
  - `[ACTION:抢钱]`
  - `[ACTION:搜掠]`
  - `[ACTION:血洗]`
  - `[ACTION:殖民]`

## 影响判断

### 直接编译/桥接影响：低

AF 1.09/latest 没直接动 GCCZ 桥接主文件和场景行为文件。现有以下桥接入口仍在：

- `AfGcczShoutBridge.RuleId`
- `AfGcczShoutBridge.AppendRuntimePromptToShoutContext(...)`
- `AfGcczShoutBridge.AddExclusivePreprocessRuleExclusions(...)`
- `SiegeAiInterventionBehavior.ShouldRunSiegeInterventionPostprocessForExternal(...)`
- `SiegeAiInterventionBehavior.BuildRuntimePostprocessRulesForExternal(...)`
- `SiegeAiInterventionBehavior.BuildRuntimePostprocessContextForExternal(...)`
- `SiegeAiInterventionBehavior.NormalizeSiegeInterventionPostprocessTagsForExternal(...)`
- `SiegeAiInterventionBehavior.TryProcessAiActionTags(...)`

### 后处理语义影响：中到高

AF 1.09/latest 大幅改写了 `ActionPostprocessPrompts.json` 和 `RuleBehaviorPrompts.json`，重点是动作许可门槛、RELAY、GIVE、WORLD_MAP_ORDER、history recall/acceptance 等规则。GCCZ 的标签仍在，但后处理模型可能更严格地要求“NPC 已明确接受/执行”。

GCCZ 的特殊点是：

- `宽恕` 是玩家单方处置，不应被普通“NPC 必须同意”门槛误杀。
- `救济/宣抚/盟誓/安兵/召集` 需要 NPC 接受/传达/执行。
- `抢钱` 只应由战败平民/商人/工匠/头人/要人直接回应玩家索取财物时触发。
- `搜掠/血洗/殖民` 只应由玩家己方入城士兵直接回应玩家明确命令时触发。
- GCCZ 运行时注入规则必须压过全局后处理的普通对话标签倾向。

因此 GCCZ 线程应重点测“规则还在”之外的行为：全局后处理新门槛是否会漏掉 GCCZ 处置标签，或误把 RELAY/GIVE/WORLD_MAP_ORDER 放到 GCCZ 输出里。

## 建议 GCCZ 线程优先检查

1. 对照 `SiegeActionTagCatalog` / `SiegePostprocessRuleCatalog`，确认上述 10 个标签全部被当前运行时归一化和处理。
2. 用真实 GCCZ 场景测试：
   - 玩家对平民说“不杀不抢/放过你们” → 应触发 `宽恕`，不要求平民同意。
   - 玩家给共享物资后命令己方士兵分发 → 应触发 `救济`。
   - 玩家向平民索要第纳尔/粮食/货物 → 应触发 `抢钱`，不触发原版 Pillage。
   - 玩家命令己方士兵搜掠全城 → 应触发 `搜掠`。
   - 玩家命令己方士兵血洗/迁殖 → 应触发 `血洗` / `殖民`。
   - 士兵互聊、平民恐惧、主动请示 → 不应直接结算 `搜掠/血洗/殖民`。
3. 检查 `ActionPostprocessPrompts.json` 新的 RELAY 规则是否会在 GCCZ 场景里抢占输出。
4. 检查 `MyBehavior.cs` 新版 action facts / prompt assembly 是否仍会把 GCCZ runtime context 放进 active rule block。
5. 检查 `ProactiveNpcRequestBehavior.cs` / `NobleGatheringBehavior.cs` 的新主动 NPC 逻辑是否可能在 GCCZ active stage 插入普通对话/赴宴逻辑。
6. 若要修 GCCZ 本体，先改 `G:\AFMOD\GCCZ` 的独立源码/规则或记录桥接补丁，再镜像到融合树；不要在 SETS 线程里把 GCCZ 业务逻辑塞回 AF 大文件。

## SETS 线程已做/准备做的边界处理

SETS 只保留城镇内部暴乱胜利后进入 GCCZ；城堡/村庄胜利后 GCCZ 入口仍关闭。

本线程针对 SETS 只做一个最小桥接保护：避免 GCCZ 旧 active 状态残留时，错误阻塞 SETS 后续正常进城随行准备。具体在 `SiegeAiInterventionBehavior.IsInterventionMissionOpenOrPendingForExternal()` 中改为：

- `_pendingMode != None` 时算 pending；
- `_activeMode != None` 且当前 mission 确实带 `InterventionMissionBehavior` 时才算 active；
- 没有当前 mission 时，只在有 summary/encounter/aftermath/direct massacre/plunder 等后续流程待处理时才算 pending。

这属于 SETS 防误伤桥，不改变 GCCZ 处置策略。

## 风险结论

- GCCZ 编译级风险不大。
- GCCZ 运行时后处理语义风险较大，主要来自 AF 1.09/latest 全局动作后处理 prompt 改写。
- 需要 GCCZ 线程用真实进城处置场景验证标签触发，不要只看 JSON 存在。
