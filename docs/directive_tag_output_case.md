# Directive Tag Output Case

本文档记录 AnimusForge 的“指令标签输出”成功案例。以后新增 LLM 驱动的游戏机制时，优先按这个案例检查前处理、主链路、后处理和实际触发，不要只加正文规则。

## 聊天链路

AnimusForge 的主对话不是单层提示词，而是三段式链路：

1. 前处理：根据玩家输入和上下文选择话题。
2. 主链路：把对应话题的正文规则注入 NPC 回复生成，让 NPC 只自然说话，不直接负责稳定触发机制。
3. 后处理：把对应话题的 `PostprocessRules` 注入标签输出器，让它只输出标签；C# 再解析标签并触发游戏机制。

成功机制必须同时满足两件事：

- 前处理选中话题后，主链路注入了正文规则。
- 同一话题的后处理标签规则也注入了 `{tag_rules}`，并且输出标签后有 C# 解析和执行入口。

只完成其中一半都不算成功。

## 成功案例

这些案例目前比较可靠，新增机制时优先参考它们：

- `Duel`
- `IBarter, Bestow, or Exchange Assets`
- `Debts and credit`
- `hero_join_party`
- `Change Settlement Ownership`

它们成功的共同点：

- `RuleBehaviorPrompts.json` 中有清晰的话题配置。
- 话题配置中同时存在主链路规则和 `PostprocessRules`。
- 后处理链路只在该话题被注入时拿到对应标签表。
- 标签格式和 C# 正则/解析器完全一致。
- 标签输出后确实进入机制执行函数，而不是只被清理掉。

## 关键文件

- `AnimusForge/ModuleData/RuleBehaviorPrompts.json`
- `AnimusForge/ModuleData/ActionPostprocessPrompts.json`
- `AIConfigHandler.cs`
- `MyBehavior.cs`
- `ShoutBehavior.cs`
- `RewardSystemBehavior.cs`
- `DuelBehavior.cs`

按机制类型还要看：

- 决斗：`DuelBehavior.cs`、`ChatDuelHandler.cs`
- 交易/债务/领地转移/入队：`RewardSystemBehavior.cs`
- 场景喊话链路：`ShoutBehavior.cs`
- 直接对话链路：`MyBehavior.cs`

## 提示词文本归属

所有发送给 LLM 的规则文本、标签说明、运行边界、禁止条件和示例，都必须优先写在 `AnimusForge/ModuleData/RuleBehaviorPrompts.json`。

目的：

- 方便开发者直接改提示词，不需要翻 C#。
- 让前处理、主链路、后处理能从同一个话题配置读取规则。
- 避免同一机制的提示词散落在多个 C# 文件里，导致主链路和后处理版本不一致。
- 方便把成功案例迁移到 MCM、外部配置或后续工具链。

C# 代码只负责：

- 读取配置。
- 注入配置。
- 组装运行时事实。
- 过滤当前不允许输出的标签。
- 解析标签并执行游戏机制。

除非是纯运行时事实、日志、错误提示或不可配置的安全兜底，不要在 C# 里硬编码新的 LLM 提示词正文。若必须临时硬编码，应在注释中说明原因，并优先安排回迁到 `RuleBehaviorPrompts.json`。

## 新增标签机制清单

新增一个 LLM 标签机制时，至少逐项检查：

- 在 `RuleBehaviorPrompts.json` 里有独立规则项，`IsEnabled`、`TopicLabel`、`Instruction` 或 `DialogueInstruction` 明确。
- 新机制必须分配新的连续 `TopicNumber`。如果当前最新机制是 `16 Oppression via noble status / Using noble title to intimidate`，下一个新机制就应使用 `17`，不要复用旧编号，也不要留空跳号，除非明确是在整理废弃机制。
- 所有发给 LLM 的固定提示词文本都在 `RuleBehaviorPrompts.json`，C# 不硬编码机制正文规则。
- 前处理能选中该规则项；必要时补 `TriggerKeywords`、语义种子或运行时 gate。
- 主链路规则只教 NPC 如何表态，不要求正文直接输出最终动作标签，除非这是旧机制兼容路径。
- 同一个规则项里有 `PostprocessRules`，并且标签描述写明“何时输出”和“何时禁止输出”。
- 后处理调用处只在前处理命中该话题时合并这组 `PostprocessRules`。
- `ActionPostprocessPrompts.json` 的 `{tag_rules}` 能收到这组标签表，不会是空。
- C# 解析器的正则包含新标签格式。
- 标签输出后有明确执行函数，执行后会移除标签，避免玩家看到内部标签。
- 日志至少能看到三段：话题命中、后处理输出、机制执行结果。

## 常见失败形态

1. 只有主链路规则，没有后处理规则。
   现象：NPC 正文会答应，但后处理标签表没有对应标签，机制永远不触发。

2. 有 `PostprocessRules`，但后处理调用处没有合并这组规则。
   现象：前处理选中了话题，正文规则生效，但 `{tag_rules}` 里没有这个标签。

3. 后处理输出了标签，但 C# 正则不认。
   现象：日志能看到标签输出，游戏里没有效果。

4. C# 识别了标签，但没有接到机制执行函数。
   现象：标签被清掉或记录了，但没有改变游戏状态。

5. 标签格式在 JSON 和 C# 中不一致。
   例子：JSON 写 `[ACTION:SOMETHING:目标]`，C# 只匹配 `[A:SOMETHING]`。

6. 运行时事实没有注入后处理。
   现象：后处理知道标签格式，但不知道可用目标、可转移定居点、可加入英雄、债务 ID 等事实，容易输出无效目标。

## 成功案例模式

### Duel

- 话题：`Duel`
- 标签：`[ACTION:DUEL]`、`[AD;金额;天数;备注内容]`、`[ACTION:DUEL_LINE_WIN:...]`、`[ACTION:DUEL_LINE_LOSE:...]`
- 关键点：决斗标签和赌注/台词标签都在后处理中出现；债务标签与决斗同轮出现时需要延后到决斗结算后处理。

### Barter, Bestow, or Exchange Assets

- 话题：`IBarter, Bestow, or Exchange Assets`
- 标签：`[ACTION:GIVE_GOLD:金额]`、`[ACTION:GIVE_ITEM:物品名称:数量]`、`[AD;金额;天数;备注内容]`、`[ADP;债务ID]`
- 关键点：主链路强调系统事实和库存限制，后处理拿到物品清单、玩家可见装备和债务提示。

### Debts and credit

- 话题：`Debts and credit`
- 标签：和交易链路共享 `GIVE_GOLD`、`GIVE_ITEM`、`AD`、`ADP`
- 关键点：必须限定“玩家欠 NPC”，不要让 NPC 欠玩家的债务误触发同一套标签。

### hero_join_party

- 话题：`Recruit Hero NPCs to Player's Party`
- 标签：`[A:H_J_P_P]`
- 关键点：短标签只表示当前说话的有名 NPC 本人加入玩家队伍，不能表示第三人加入。

### Change Settlement Ownership

- 话题：`Change Settlement Ownership`
- 标签：`[ACTION:SETTLEMENT_TRANSFER:TO_PLAYER:定居点ID或编号]`
- 关键点：前处理和后处理都要注入运行时硬约束。NPC 必须是合格家族族长；目标只能来自当前可转移城市/城堡清单；玩家转给 NPC 不走这个标签。

## 给 Codex 的调用方式

以后可以直接说：

```text
按指令标签输出案例，加一个新的后处理标签机制。
```

或：

```text
参考 Duel / 交易 / 债务 / hero_join_party / 领地转移那套三段式链路，检查这个新标签为什么不触发。
```

Codex 应先检查本文件，再检查 `RuleBehaviorPrompts.json`、`ActionPostprocessPrompts.json` 和对应 C# 解析/执行函数。
