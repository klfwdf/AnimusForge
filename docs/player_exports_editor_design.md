# AnimusForge PlayerExports 独立编辑器设计草案

## 目标

创建一个完全独立于 Bannerlord 和 AnimusForge mod 的轻量软件，用来创建、浏览、编辑、删除和维护 `PlayerExports` 数据包。

这个软件面向不能打开游戏但需要持续维护数据包的作者。它应当像普通桌面工具一样启动，直接操作磁盘上的导出包，不启动游戏，不加载存档，不依赖 TaleWorlds 运行时。

## 独立性边界

必须满足：

- 不启动 Mount & Blade II: Bannerlord。
- 不加载 `AnimusForge.dll`。
- 不引用 `TaleWorlds.*`、`SandBox.*` 或游戏目录 DLL。
- 不修改本项目的一键编译、覆盖、推送、打包流程。
- 只读写用户选择的 `PlayerExports/<数据包名>` 目录。
- 保存时强制使用 UTF-8。
- 保存前先写入临时文件，再替换目标文件。
- 每次覆盖保存前自动创建备份。

允许满足：

- 软件可以放在本仓库内开发。
- 软件可以读取本仓库已有的示例数据包。
- 软件可以读取离线参考索引，例如英雄、定居点、王国、技能、文化等静态 ID 列表。
- 软件可以导出一个完整数据包，供游戏内现有导入功能继续使用。

## 现有数据包结构

当前仓库示例：

```text
AnimusForge/PlayerExports/
  卡拉迪亚编年史/
  战锤旧世界编年史/
```

以 `卡拉迪亚编年史` 为例，目前包含：

```text
event_data/
  WorldOpeningSummary.json
  KingdomOpeningSummaries.json
  EventRecords.json

knowledge/
  rules/
    rule_xxx__xxx.json

personality_background/
  HeroId__Name.json

unnamed_persona/
  UnnamedNpcProfiles.json

voice_mapping/
  VoiceMapping.json
```

第一版编辑器必须把这些目录视为一个完整数据包，而不是一批散装 JSON。

## 当前模型依据

知识规则模型来自 `KnowledgeLibraryBehavior.cs`：

```text
KnowledgeFile
  Version
  PlayerAppearance
  Rules[]

LoreRule
  Id
  Keywords[]
  RagShortTexts[]
  SemanticPrototypes[]
  Variants[]
  TextMappings[]

LoreVariant
  Priority
  When
  Content

LoreWhen
  HeroIds[]
  Cultures[]
  KingdomIds[]
  SettlementIds[]
  Roles[]
  IdentityIds[]
  IsFemale
  IsClanLeader
  SkillMin

LoreTextMapping
  SourceText
  Kind
  TargetId
  AgeMin
  AgeMax
  EmptyValueText
  TrueText
  FalseText
```

NPC 个性背景模型来自 `MyBehavior.cs`：

```text
NpcPersonaProfile
  Personality
  Background
  VoiceId
```

导入导出范围来自 `MyBehavior.cs`：

```text
All
HeroNpcAll
PersonalityBackground
UnnamedPersona
DialogueHistory
Debt
EventData
Knowledge
VoiceMapping
```

第一版只覆盖当前示例包中已经稳定出现的部分：

- `Knowledge`
- `PersonalityBackground`
- `UnnamedPersona`
- `EventData`
- `VoiceMapping`

`DialogueHistory` 和 `Debt` 暂不作为第一版主功能，除非后续数据包中确认需要离线编辑。

## MVP 功能

第一版目标是“能安全、清楚、快速地维护数据包”，不要先追求和游戏内菜单逐项一模一样。

必须实现：

- 打开一个 `PlayerExports` 根目录。
- 列出所有数据包。
- 新建数据包。
- 复制数据包。
- 重命名数据包。
- 删除数据包，删除前确认并进入回收站或备份目录。
- 打开数据包后显示五个主要分区：知识、人物、未命名 NPC、声音映射、事件数据。
- 全局搜索 RuleId、关键词、RAG 短句、提示词内容、NPC 文件名、VoiceId。
- 保存前校验 JSON 结构。
- 保存前校验 ID 冲突。
- 保存前校验重复条件。
- 保存前校验 RAG 短句非空和长度。
- 保存前显示会被修改的文件数量。
- 支持撤销最近一次保存。

推荐实现：

- 自动备份到 `.backups/<yyyyMMdd_HHmmss>/`。
- 打开损坏 JSON 时显示错误位置，不直接覆盖。
- 未识别字段原样保留。
- 支持格式化 JSON。
- 支持批量替换关键词或文本。
- 支持按修改时间、RuleId、关键词排序。

## 知识编辑界面

知识列表应显示：

- RuleId
- 第一个关键词
- RAG 短句数量
- 提示词变体数量
- 词汇映射数量
- 是否存在校验问题
- 文件名

知识详情应包含：

- 基本信息：RuleId、关键词、RAG 短句。
- 提示词变体：Priority、条件摘要、Content。
- 条件编辑：文化、王国、定居点、角色、具体身份、性别、是否族长、技能下限。
- 词汇映射：SourceText、Kind、TargetId、年龄范围、空值兜底、真假文本。
- 原始 JSON 预览。

行为规则：

- 新建知识时自动生成 RuleId。
- 修改 RuleId 时同步建议文件名，但不强制立刻改名。
- 删除知识时删除对应 `knowledge/rules/*.json` 文件。
- 一个知识文件保存为一条 `LoreRule`，保持当前 `knowledge/rules` 目录模式。
- 如果未来遇到旧式 `knowledge/KnowledgeRules.json`，只作为导入来源，不作为默认保存格式。

## 人物资料编辑界面

人物列表应显示：

- HeroId 或 CharacterObjectId
- 显示名，从文件名 `Id__Name.json` 解析
- Personality 是否为空
- Background 是否为空
- VoiceId
- 文件名

人物详情应包含：

- Personality 长文本编辑器。
- Background 长文本编辑器。
- VoiceId 输入框和候选列表。
- 原始 JSON 预览。

文件命名规则保持现有模式：

```text
<heroId>__<displayName>.json
```

如果没有显示名，使用：

```text
<heroId>__NPC.json
```

## 声音映射编辑界面

`voice_mapping/VoiceMapping.json` 当前结构是按组存储 voice id：

```text
male_young
male_middle
male_old
female_young
female_middle
female_old
fallback
```

界面应提供：

- 每个组的 voice id 列表。
- 添加、删除、去重、排序。
- 设置 fallback。
- 检查 fallback 是否存在于某个 voice id 列表。
- 检查空组并提示。

## 事件数据编辑界面

`event_data` 第一版支持：

- `WorldOpeningSummary.json`：编辑 `Summary`。
- `KingdomOpeningSummaries.json`：编辑 kingdom id 到摘要文本的映射。
- `EventRecords.json`：先支持只读浏览和 JSON 预览，后续再做完整编辑。

原因：事件记录字段更多，直接图形化编辑容易误导用户。第一版先确保可以安全查看和备份。

## 未命名 NPC 编辑界面

`unnamed_persona/UnnamedNpcProfiles.json` 第一版支持：

- 显示 Version。
- 显示 Profiles 字典。
- 新建、编辑、删除 profile。
- 编辑 Personality 和 Background。
- 保存前保持未知字段。

## 离线参考索引

完全独立后，软件不能从游戏运行时读取：

- Hero
- CharacterObject
- Kingdom
- Clan
- Settlement
- Culture
- Skill
- Role display name

因此需要一个可选的离线参考索引：

```text
reference_index/
  heroes.json
  character_objects.json
  kingdoms.json
  clans.json
  settlements.json
  cultures.json
  skills.json
  voices.json
```

第一版可以没有完整索引，但 UI 必须允许手动输入 ID。等游戏内或脚本导出索引后，编辑器再把输入框升级为可搜索下拉框。

推荐方案：

- 第一版先手动输入。
- 同时预留 `reference_index` 读取接口。
- 后续在 mod 内加一个“导出编辑器参考索引”的功能，单独输出这些静态 ID。

## 校验规则

全局校验：

- 所有 JSON 必须能被解析。
- 所有文本按 UTF-8 读写。
- 文件名不得包含 Windows 非法字符。
- 保存时不删除未知字段。
- 目录缺失时可自动创建。

知识校验：

- RuleId 不能为空。
- RuleId 在数据包内大小写不敏感唯一。
- Keywords 去除空项。
- RagShortTexts 去除空项。
- 导出给游戏使用前，至少需要一个有效 RagShortText。
- RagShortTexts 不超过 mod 当前限制。
- 同一规则内不允许存在完全相同的 When 条件。
- Variant Content 可以为空但要提示。
- TextMapping 的 SourceText 和 Kind 为空时提示。

人物校验：

- 文件名应能解析出 ID。
- Personality 和 Background 都为空时提示。
- VoiceId 为空允许，表示由 VoiceMapping 自动分配。

声音映射校验：

- voice id 不能为空。
- 同一组内去重。
- fallback 为空时提示。
- fallback 不在任何组内时提示。

事件数据校验：

- WorldOpeningSummary 的 Summary 可以为空但提示。
- KingdomOpeningSummaries 的 key 不能为空。
- EventRecords 第一版只做 JSON 结构校验。

## 保存策略

保存流程：

1. 读取当前磁盘版本。
2. 比较内存版本和磁盘版本。
3. 如果磁盘版本在编辑期间变化，提示用户选择重新加载、覆盖保存或另存为。
4. 创建备份目录。
5. 写入 `.tmp` 文件。
6. 重新读取 `.tmp` 并验证 JSON。
7. 替换目标文件。
8. 刷新列表和问题面板。

备份目录：

```text
<数据包>/.backups/<yyyyMMdd_HHmmss>/
```

## 推荐技术路线

推荐使用 C#/.NET 桌面应用：

- 和当前 mod 的 C# 数据模型认知成本最低。
- 可以复刻 Newtonsoft.Json 行为。
- Windows 用户使用最直接。
- 未来如果需要，可以把纯数据模型拆成共享库，但共享库不得引用 TaleWorlds。

建议工程位置：

```text
tools/PlayerExportsEditor/
```

建议项目分层：

```text
PlayerExportsEditor.App
  桌面 UI

PlayerExportsEditor.Core
  数据模型
  文件读写
  校验
  备份
  搜索

PlayerExportsEditor.Tests
  示例数据包校验
  保存回读
  文件命名
  冲突检测
```

## 第一阶段开发顺序

1. 创建 `PlayerExportsEditor.Core`。
2. 定义数据模型。
3. 实现数据包扫描。
4. 实现 UTF-8 JSON 读写。
5. 实现校验器。
6. 用现有两个示例数据包跑校验。
7. 创建桌面 UI 外壳。
8. 实现数据包列表和打开流程。
9. 实现知识列表和详情编辑。
10. 实现人物资料列表和详情编辑。
11. 实现保存、备份、撤销。
12. 再补声音映射、事件数据、未命名 NPC。

## 验收标准

第一版完成时，至少要通过：

- 能打开 `卡拉迪亚编年史`。
- 能打开 `战锤旧世界编年史`。
- 能列出所有知识条目。
- 能编辑一条知识并保存。
- 保存后的知识能被游戏内现有导入逻辑读取。
- 能新增一条知识。
- 能删除一条知识。
- 能编辑一名 NPC 的 Personality、Background、VoiceId。
- 能编辑 VoiceMapping。
- 能编辑 WorldOpeningSummary。
- 所有保存动作都有备份。
- 人为破坏 JSON 后，软件能提示错误并拒绝覆盖。

## 暂不做

第一版暂不做：

- 启动游戏。
- 读取存档。
- 调用 LLM 自动生成内容。
- 修改 mod 源代码。
- 修改一键编译或部署脚本。
- 完整编辑 DialogueHistory。
- 完整编辑 Debt。
- 自动推送到 Bannerlord Modules 目录。

## 下一个待确认问题

问题：第一版是否直接使用 C#/.NET 桌面应用，并把工程放在 `tools/PlayerExportsEditor/`？

推荐答案：是。这样最贴近当前项目技术栈，也方便后续把数据模型、校验器和测试放在同一个仓库内维护。
