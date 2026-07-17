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

## 硬性边界：指令标签不进主链路

指令标签绝对不可以写进主链路规则。主链路规则只能指导 NPC 如何自然说话、如何表态、如何遵守事实边界；不得包含 `[ACTION:...]`、`[A:...]`、`[AD;...]`、`[ADP;...]` 等最终动作标签格式、标签示例或“输出标签”的要求。

所有指令标签格式、输出条件、禁止条件和示例都必须写在同一话题的 `PostprocessRules` 中，并通过后处理链路的 `{tag_rules}` 注入。这样 NPC 正文不会泄露内部标签，游戏机制也只由后处理标签和 C# 解析器稳定触发。

### 例外：无话题的全局事实检测标签

如果机制明确不改变 NPC 正文，只检测每轮已经发生的事实，可以把规则放入 `ActionPostprocessPrompts.json` 的独立全局规则组，并在统一后处理入口按运行时资格合并。此类规则不得进入前处理或主链路，也不得依赖话题命中；三个交流渠道必须共用同一规范化与执行器，不适用渠道要用显式 chain gate 排除。

`[ACTION:INTIMACY_INTERNAL]` 是当前案例：只在面对面交流、玩家与当前成年异性 Hero NPC 的上下文中提供；信使链路显式排除。标签在本轮明确已经发生性行为，并且明确发生内射或男方在女方体内射精时成立；不额外要求具体描写插入过程，但只有亲吻、拥抱、抚摸、调情或暧昧亲密仍然不够。标签表达“本轮事实已发生”，游戏层再读取 MCM 的“15. 亲密行为与怀孕”概率进行怀孕判定，默认 50%，不能把标签本身解释为必定怀孕。

## 成功案例

这些案例目前比较可靠，新增机制时优先参考它们：

- `Duel`
- `IBarter, Bestow, or Exchange Assets`
- `Debts and credit`
- `hero_join_party`
- `Change Settlement Ownership`
- `INTIMACY_INTERNAL`（无话题全局事实检测例外）

它们成功的共同点：

- `RuleBehaviorPrompts.json` 中有清晰的话题配置。
- 话题配置中同时存在主链路规则和 `PostprocessRules`。
- 主链路规则不写指令标签；标签格式和输出要求只存在于 `PostprocessRules`。
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
- 主链路规则只教 NPC 如何表态；绝对不写最终动作标签格式、标签示例或要求正文输出标签。
- 同一个规则项里有 `PostprocessRules`，并且标签描述写明“何时输出”和“何时禁止输出”。
- 后处理调用处只在前处理命中该话题时合并这组 `PostprocessRules`。
- `ActionPostprocessPrompts.json` 的 `{tag_rules}` 能收到这组标签表，不会是空。
- C# 解析器的正则包含新标签格式。
- 参数化标签模板在归一化白名单中显式映射到全部合法具体标签，不能只按模板字符串完全相等判断。
- 标签输出后有明确执行函数，执行后会移除标签，避免玩家看到内部标签。
- 日志至少能看到四段：话题命中、后处理 `RAW` 输出、归一化 `FINAL` 保留、机制执行结果。

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

7. 参数化模板存在，但归一化白名单只做字符串完全相等判断。
   现象：`RAW` 有合法具体标签，`FINAL` 却把它删除；直接 TAG 测试可能仍然成功。

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
- 提示词规则模板：`[A:H_J_P_P_C&L]` 或 `[A:H_J_P_P_C/L]`
- 实际可执行标签：`[A:H_J_P_P_C]`、`[A:H_J_P_P_L]`
- `C` 表示成为玩家同伴，`L` 表示成为玩家家族领主成员；旧标签 `[A:H_J_P_P]` 不再执行。
- 短标签只表示当前说话的 NPC 本人或其野外非 Hero `MobileParty` 代表触发对应入队机制，不能表示无关第三人加入。
- 场景内非 Hero 使用 `C/L` 升格为对应 Hero 身份；原生 Hero 使用 `C/L` 切换为对应身份；野外非 Hero `MobileParty` 代表优先走整支部队并入玩家主队的既有逻辑。

### Change Settlement Ownership

- 话题：`Change Settlement Ownership`
- 标签：`[ACTION:SETTLEMENT_TRANSFER:TO_PLAYER:定居点ID或编号]`
- 关键点：前处理和后处理都要注入运行时硬约束。NPC 必须是合格家族族长；目标只能来自当前可转移城市/城堡清单；玩家转给 NPC 不走这个标签。

## 参数化标签模板归一化案例：RAW 有标签但 FINAL 丢失

### 现象

后处理模型已经正确输出具体标签，例如：

```text
[A:H_J_P_P_C]
```

`Token_Stats.txt` 或统一后处理日志的 `RAW` 也能看到该标签，但游戏机制没有触发。进一步查看日志会发现：

- `RAW` 包含 `[A:H_J_P_P_C]`。
- `FINAL` 只剩 NPC 正文和情绪标签，具体动作标签已经消失。
- 后续确实经过 `nonhero_join_before/after` 或 Hero 奖励动作入口，但输入文本长度不变，也没有 `HeroJoin` / `NonHeroJoin` 执行日志。
- 直接使用内部 TAG 测试入口提交 `[A:H_J_P_P_C]` 却能成功执行。

这说明问题不在最终 C# 正则或游戏机制执行器，而在“后处理原始输出到最终动作文本”之间的归一化/白名单阶段。

### 根因

配置里的标签是一个参数化模板：

```text
[A:H_J_P_P_C&L]
```

LLM 按规则选择后输出其中一个具体标签：

```text
[A:H_J_P_P_C]
[A:H_J_P_P_L]
```

旧归一化器只用字符串完全相等判断“LLM 输出的标签是否存在于本轮规则表”。具体标签不等于模板文本，因此被当作越权标签删除。最终执行器根本收不到标签。

由于自由对话、场景喊话以及其他共享后处理渠道都使用同一个归一化入口，这不是野外劫匪专属问题，而会同时影响：

- 场景内非 Hero。
- 野外非 Hero `MobileParty` 代表。
- 原生 Hero。
- 所有复用该统一后处理入口的交互场景和渠道。

### 正确修法

参数化标签必须同时定义两层资格：

1. 配置模板是否在本轮规则表中，例如 `[A:H_J_P_P_C&L]` 或 `[A:H_J_P_P_C/L]`。
2. LLM 输出是否是该模板允许的具体实例，例如只能是 `[A:H_J_P_P_C]` 或 `[A:H_J_P_P_L]`。

归一化器应当先扫描一次本轮规则表，缓存“是否允许该参数化标签族”，然后在扫描 LLM 输出时按有限集合接受具体实例。不要在每个标签上重复扫描规则表、重复创建正则或做无界参数放行。

这套映射只负责授权具体标签通过归一化层，不得把模板本身当成可执行标签：

- `[A:H_J_P_P_C]`：允许并交给执行器。
- `[A:H_J_P_P_L]`：允许并交给执行器。
- `[A:H_J_P_P_C&L]` / `[A:H_J_P_P_C/L]`：仅为规则模板，不直接执行。
- `[A:H_J_P_P]`：旧格式，不执行。
- 其他后缀：拒绝。

### 为什么直接 TAG 测试会误导

内部直接 TAG 测试通常把标签直接送入游戏动作执行器，可能绕过统一后处理归一化器。因此：

- 直接 TAG 测试成功，只能证明解析器和游戏机制执行器可用。
- 不能证明真实 LLM 链路中的标签能从 `RAW` 保留到 `FINAL`。
- 新增或修改参数化标签时，必须同时做真实后处理测试和直接执行器测试。

### 日志验收标准

参数化标签的一次成功触发必须能连续追到：

1. 本轮 `mergedRules` / `merged_rules` 中包含对应模板。
2. 后处理 `RAW` 中包含具体标签。
3. 后处理 `FINAL` 中仍包含同一个具体标签。
4. 动作入口收到含标签的文本。
5. 出现具体机制成功日志，例如 `wilderness_party_join`、`moved_to_player_companion` 或 `moved_to_player_clan`。
6. 标签从玩家可见正文中移除，并写入正确的 AFEF 事实、历史和通知。

如果 `RAW` 有标签而 `FINAL` 没有，优先检查归一化白名单，不要先修改提示词，也不要误判为游戏 API 或队伍转移失败。

### 必测矩阵

修改 `hero_join_party` 或其他参数化标签后，至少验证：

| 目标/渠道 | 具体标签 | 预期结果 |
| --- | --- | --- |
| 原生 Hero | `[A:H_J_P_P_C]` | 成为玩家同伴并进入玩家主队 |
| 原生 Hero | `[A:H_J_P_P_L]` | 成为玩家家族领主成员并进入玩家主队 |
| 场景内非 Hero | `[A:H_J_P_P_C]` | 升格为玩家同伴 |
| 场景内非 Hero | `[A:H_J_P_P_L]` | 升格为玩家家族 Hero |
| 野外非 Hero `MobileParty` 代表 | `[A:H_J_P_P_C]` 或 `[A:H_J_P_P_L]` | 按既有野外逻辑把整支部队成员及俘虏并入玩家主队 |
| 任意目标 | `[A:H_J_P_P]` | 不执行旧格式 |
| 任意目标 | 模板原文或非法后缀 | 不执行且不得泄露到玩家可见正文 |

三个交流渠道适用时还必须分别验证自由对话、场景喊话和信使；若某渠道不适用，必须由显式 gate 排除。

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
