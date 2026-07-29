# AnimusForge AI 外交系统 HANDOFF

> 更新时间：2026-07-20  
> 工作区：`E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`  
> 分支 / 基线提交：`main` / `3132d663`  
> 当前最高优先级：只处理 AI 外交 Tokens 消耗。暂时不要处理“无王国玩家看不到外交事件”和“第三国不爱发言”两个问题。

## 1. 当前状态结论

- 新 AI 外交系统主体已经写入 `WorldDiplomacyBehavior.cs`，主要新代码目前仍是未跟踪文件，禁止执行清理未跟踪文件、整树覆盖或回滚。
- `DiplomacyPeaceTermsService.cs` 也是未跟踪的新核心文件。
- MCM 接入位于 `DuelSettings.cs`。
- 已完成 Bannerlord 1.3、1.4 与 Bootstrap 三套构建，均为 0 警告、0 错误。
- 最新构建已覆盖到：
  `E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`
- 已确认部署产物与构建产物 SHA256 一致。
- 游戏内测试证明传播、王庭到达、目标国后台回应以及第三国参与判断均实际运行，但发现严重 Tokens 消耗问题；该问题尚未修复。

最新构建哈希：

```text
Bootstrap  BE3F488522B74A161E7300E73E1E9110F5947A66A20406E070A3FAF7FCBAFEFF
1.3 DLL   7F790BE1BA345816B7FB64F475403C90578B321485AAA0BADE42119A45287069
1.4 DLL   D5BD7FBF63B56B9B74841A5C6FFA9A8ABF11D0C1018FE134D646729CFC4FAA9C
```

## 2. 用户确认的最终设计边界

### 2.1 入口和既有功能

- U 键“王国公告”统一入口中先选择“自定义政策”或“外交文书”。
- 自定义政策是其他开发者的既有功能，铁律是不破坏、不复制替换其执行链。
- 外交文书只要求玩家输入正文，由 LLM 判断外交意图。
- 王国公告档案同时展示自定义政策和外交公文。
- 外交公文生成必须在后台运行，不暂停游戏等待模型。

### 2.2 外交机制

- AI 外交接管原版王国决议式宣战、议和、同盟和贸易提案。
- 面对面国王口头外交保留，不受禁用；达成口头外交后可以产生公告反馈。
- 宣战可以在发文时立即生效；其他需要双方同意的外交动作在有效回应后结算。
- 战争状态、战局、最近真实战斗、战争中领地变化、统治者关系、人物亲属快照、文化领地和历史公文均进入生成素材。
- 议和是一套统一机制，可同时谈贡金与割地；割地必须通过实体检测，并受严重战争劣势门槛限制。
- 战争结束后清除该场战争的临时事件账本。
- 战争升级阈值保留且可关闭；主要由回合内言语冲突累积，原版决议信号只提供较低权重。

### 2.3 多国外交事件

- 一个外交事件不是固定 A/B 一问一答，也不是“推特式全世界即时获知”。
- 公文按地理位置传播；某国王庭收到后才可根据当时已经抵达的公文参与事件。
- 非指向国可以评论、支持、威胁、搅局、提出合作或转向事件内其他国家。
- 被直接指向的国家不能完全不回应。
- 国家可以自然退出；退出后没有新的直接点名或重大利益变化时，不应重复调用 LLM。
- 事件按长期无新内容、争议已经解决或达到软时间尺度自然结束，不因战争阈值武装而强制切断。

## 3. 已完成实现

### 3.1 世界传播双轨制

当前代码已经将传播拆成两个独立作用域：

1. 王庭传播
   - MCM：`王庭最远送达天数`。
   - 范围 3～14 天，默认 7 天。
   - 所有存续王庭按当前地图距离归一化，最远王庭不超过设置值，近处更早到达。
   - 王庭收到后记录王国知识、推进外交参与或强制回应。
   - 公文抵达某国王庭时，该国全部贵族立即获知。

2. 民间传播
   - MCM：`民间传遍大陆所需天数`。
   - 范围 7～42 天，默认 21 天（一季）。
   - 控制当地平民和要人的知情速度，不再反向触发王庭外交。
   - 发文王庭所在定居点当日获知；其他定居点按距离到达。

关键存档结构：

- `WorldDiplomacyPropagationArrival.Scope`：`court` / `civilian`
- `WorldDiplomacyPropagationArrival.KingdomId`
- `WorldDiplomacyStorage.KingdomKnowledge`
- `WorldDiplomacyStorage.NobleKnowledge`
- `WorldDiplomacyStorage.SettlementKnowledge`
- `LastAppliedCourtDeliveryDays`
- `LastAppliedCivilianSpreadDays`

旧存档迁移：

- 旧传播记录默认视为民间传播。
- 设置改变后，下一个每日 Tick 会重新计算尚未抵达的记录并补建王庭传播记录。
- 已有王庭知识会通过 `CourtKnowledgeMigratedToNobles` 一次性同步给全国贵族，不做每日重复迁移。

### 3.2 NPC 记忆注入

- `BuildDiplomacyMemoryContext(...)` 已通过共享三渠道 prompt 构建链注入。
- 平民和要人读取当前定居点已经传播到的公文。
- 普通领主读取所属王国的 `NobleKnowledge`。
- 统治家族和统治者读取 `KingdomKnowledge`。
- 只注入角色按传播结果实际知道的内容，不做全世界即时共享。
- 回合结束后生成外交事件摘要；年度还有长期压缩，避免永久注入全部原文。

### 3.3 战争、议和与事实约束

- 已有 `WarSituationSnapshot`，读取双方战争状态、战力、战争进展、其他战争、议和开放度和割地劣势评分。
- `DiplomacyPeaceTermsService.cs` 负责复用/承接贡金与领地条件结算。
- 最近真实战斗由 `MapEventEnded` 记录，提示词没有记录时明确禁止模型编造具体战役和伤亡数字。
- 战争中定居点易主记录保存在临时战争账本，优先作为归还失地候选；战争结束清理。
- 已经交战的国家不得再次把当前战争写成“刚刚正式宣战”。

### 3.4 统治者文风与人物事实

- 公文生成注入具体统治者的文化、Traits、自定义个性、背景和头衔。
- 注入发文统治者与目标统治者的父母、配偶、子女和直接关系快照。
- 明确禁止把同文化、同阵营或其他王室成员错误认作亲属。
- 默认提示词要求自然、直白的现代中文世界内表达，避免文言腔和反复使用“本王”。
- MCM 只有一个 `AI外交自定义提示词` 编辑入口，同时影响决策偏好和文风，不能覆盖硬事实、传播和 JSON 契约。

### 3.5 UI 与通知

- 外交撰写弹窗使用羊皮纸界面，后台解析，不暂停等待 LLM。
- U 键输入冲突已做输入态屏蔽。
- 王国外交原版提案按钮已禁用/置灰并提示改用 AI 外交。
- 外交通知使用自定义圆形图，查看正文复用信使羊皮纸。
- 公文档案显示游戏内日期、类型、AI 生成标题和公告正文。

## 4. MCM 当前项目

组名：`17. AI外交`

```text
启用AI外交
AI外交活跃程度
王庭最远送达天数（3～14，默认7）
民间传遍大陆所需天数（7～42，默认21）
外交事件长度
启用战争升级阈值
战争升级阈值
主动战争冷却
和平保护期
AI外交自定义提示词
```

旧的 `WorldDiplomacyPropagationSpeedPercent` 仅为兼容旧配置保留，已经隐藏，不应重新接回传播计算。

## 5. 当前最严重未修复问题：Tokens 暴涨

游戏日志位置：

```text
E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\Logs\Token_Stats.txt
E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\Logs\Mod_Logic.txt
```

最近一次 19:40～19:52 的实测统计：

| 请求类型 | 次数 | Tokens |
|---|---:|---:|
| AI 外交第三国参与判断 | 14 | 32,658 |
| AI 外交公文生成 | 6 | 30,141 |
| AI 外交语义分析 | 6 | 13,553 |
| NPC 自定义政策 | 4 | 12,265 |

该窗口总计 88,617 Tokens，其中 AI 外交 76,352，约占 86%。用户挂机一段时间看到四十多万 Tokens，与该速率一致。

传播到 441 个定居点本身不调用 LLM，不是 Tokens 来源。

### 5.1 根因一：强制回应无限往返

日志明确显示阿塞莱与南帝国持续交替回应：

```text
A 公文到达 B 王庭
→ B mandatory response queued
→ B 的回应又把 A 设为必须回应
→ A mandatory response queued
→ 无限重复
```

直接代码原因：

- `StartDocumentPropagation(...)` 当前把 `AddressedKingdomIds + TargetKingdomId` 全部以 `mandatoryReply: true` 加入参与者。
- 这里没有正确服从 `document.RequiresResponse`，也没有区分“首篇主张”和“已经是回应的普通表态”。
- `ProcessCourtArrival(...)` 对所有 `directlyAddressed` 公文都会调用 `TryScheduleMandatoryCourtResponse(...)`。

这会使活动回合永远存在 `MandatoryReplyPending`，无法自然结束，也不断生成新公文。

### 5.2 根因二：每篇 AI 公文调用两次模型

`BuildGenerationPrompt(...)` 已经要求模型返回：

```json
{
  "title": "...",
  "body": "...",
  "author_intent": {
    "intent": "...",
    "commitment": "..."
  },
  "peace_terms": {}
}
```

但 `CommitGeneratedDocument(...)` 读取这些字段后，仍然对每篇非强制 AI 公文执行 `EnqueueAnalysisJob(...)`，再次发送全文做语义分析。生成和分析各 6 次，存在明显重复。

玩家自由输入仍然需要语义分析；AI 自己按照固定 JSON 契约生成的公文应优先直接验证并提交。

### 5.3 根因三：第三国反复判断旁观

- 每篇新回应传播到各国王庭后都会再次排队参与判断。
- `withdrawn` 会被跳过，但 `observer` 会随着每篇新文书反复判断。
- 参与 prompt 会为每个候选国重复拼入相同的公文正文，随着回合增长越来越长。
- 当前每日两篇限制只统计“公文生成”，没有统一限制 `generate / analyze / participate / compress` 的 LLM 总调用。

## 6. DeepSeek 缓存现状

DeepSeek 上下文缓存默认自动启用，只能命中从第 0 个 Token 开始完全一致的输入前缀。缓存命中会降低输入计费和延迟，但不会减少控制台或统计中显示的 Tokens 总数，输出 Tokens 也不能缓存。

官方说明：`https://api-docs.deepseek.com/guides/kv_cache`

最近测试的缓存命中率：

| 请求类型 | 命中率 |
|---|---:|
| NPC 政策 | 约 76.6% |
| 外交公文生成 | 约 31.4% |
| 第三国参与判断 | 约 6.3% |
| 外交语义分析 | 0% |

外交 prompt 的人物、国家、日期、回合天数和正文等动态内容出现得太早，后面的固定规则与 JSON 契约无法成为公共前缀。

## 7. 下一位开发者的当前任务顺序

用户最新明确要求：先解决 Tokens，其他两个问题暂时不管。不要顺手重做回合可见性或强行提高第三国发言率。

### P0：停止无限回应链

建议规则：

- 首篇明确指向公文仍保证目标国回应一次。
- 回应公文只有真正提出新提案、反条件、最后通牒或明确新问题时，才产生新的回应义务。
- `accept_*`、`reject_*`、`statement`、普通 `condemn`、`apology` 等默认不产生下一次强制回应。
- `StartDocumentPropagation(...)` 和 `ProcessCourtArrival(...)` 必须共同服从经过验证的 `document.RequiresResponse`。
- 增加自动往返安全熔断，防止模型异常反复输出“需要回应”；熔断只停止自动强制回应，不强制关闭整个外交事件。

### P0：取消 AI 公文的重复语义调用

- 扩展 AI 生成 JSON，直接包含：主要对象、直接指向国、提及国、是否要求回应、语气和置信度。
- `CommitGeneratedDocument(...)` 在 C# 中校验王国 ID、动作合法性、战争状态和议和条件后，直接调用 `ProcessAnalyzedDocument(...)`。
- 玩家自由输入、公文格式损坏或关键字段缺失时，才回退到 `EnqueueAnalysisJob(...)`。
- 这项修改不应改变玩家公文的 LLM 语义判定能力。

### P1：提高缓存命中

对 `BuildGenerationPrompt(...)`、`BuildAnalysisPrompt(...)`、`BuildParticipationPrompt(...)`：

1. 将完全固定的角色说明、规则、输出枚举和 JSON 契约移到最前面。
2. 稳定的玩家自定义提示词紧跟固定规则。
3. 王国清单按 `StringId` 排序，保证同一存档内前缀稳定。
4. 作者、对象、日期、战局、人物快照、公文正文和回合材料全部放在最后。
5. 不要在固定输出契约之前插入活动程度、回合天数等动态字段。
6. 第三国批量判断中，相同事件材料只写一份共享摘要；每个候选国只附加本国特有利益事实，禁止重复整篇正文。

注意：缓存优化降低费用但不降低“总 Tokens”显示值，因此必须排在减少重复请求之后实施。

### P1：增加总请求保险

当前只有 `MaxAiDocumentsStartedPerDay = 2`，它不限制分析和参与判断。建议新增统一预算，覆盖所有外交 LLM job：

- 每游戏日总请求数上限；
- 同一外交事件自动请求数安全上限；
- 现实时间最小请求间隔；
- 强制回应优先于参与判断和普通生成。

具体默认值尚未由用户最终确认，不要擅自在 MCM 再增加大量项目。可以先采用代码内保守默认值并记录日志，或在实施前与用户确认。

## 8. 已知但按用户要求暂缓的问题

### 8.1 无王国玩家看不到事件

日志证明正常回合在新游戏约第三个游戏日已经启动，但无名小卒没有玩家王庭：

- NPC 公文不会设置 `HasReachedPlayerCourt`；
- 右侧弹窗不会出现；
- 公告档案只显示玩家公文或已经抵达玩家王庭的公文。

未来方案是让无王国玩家在公文传播到当前定居点后获知，但当前不要处理。

### 8.2 第三国不爱发言

- 第三国确实收到公文并调用了参与 LLM。
- 模型大多返回 `observer / speak_now=false`。
- A/B 无限回应又占满了每日公文额度；即使第三国被选中发言，额度不足时当前代码可能直接跳过且不重试。

未来应做零 Tokens 利益预筛选、重点第三国发言机会和待发言队列，但当前不要处理。

## 9. 关键文件与符号

```text
WorldDiplomacyBehavior.cs
  InitializeSchedule
  TryScheduleNormalRound
  TryStartNextLlmJob
  CommitGeneratedDocument
  CommitAnalysis
  ProcessAnalyzedDocument
  StartDocumentPropagation
  ProcessPropagationArrivals
  ProcessCourtArrival
  TryScheduleMandatoryCourtResponse
  QueueParticipationEvaluation
  EnqueueParticipationBatchIfNeeded
  BuildParticipationPrompt
  ProcessRoundLifecycle
  BuildDiplomacyMemoryContext
  BuildGenerationPrompt
  BuildAnalysisPrompt

DiplomacyPeaceTermsService.cs
DuelSettings.cs
AnimusForgeTerminalBehavior.cs
PolicySystemUi.cs
VoteDealBehavior.cs
VoteDealBehavior.Agenda.cs
SubModule.cs
AnimusForge.csproj

UI：
AnimusForge/GUI/Prefabs/WorldDiplomacyComposePopup.xml
AnimusForge/GUI/SpriteParts/af_world_diplomacy/
```

## 10. 工作树安全提示

当前工作树不是干净状态，并包含大量用户已有修改：

```text
M  AnimusForge.csproj
M  AnimusForgeTerminalBehavior.cs
M  DiplomacyBehavior.cs
M  DuelSettings.cs
M  NobleGatheringBehavior.cs
M  PolicySystemUi.cs
M  README_BUILD.md
M  SubModule.cs
M  VoteDealBehavior.Agenda.cs
M  VoteDealBehavior.cs
M  一键编译覆盖推送/build_single_module.ps1
M  一键编译覆盖推送/deploy_module.ps1
?? AnimusForge/GUI/Prefabs/WorldDiplomacyComposePopup.xml
?? AnimusForge/GUI/SpriteParts/af_world_diplomacy/
?? DiplomacyPeaceTermsService.cs
?? VoteDealBehavior.MapNotification.cs
?? WorldDiplomacyBehavior.cs
```

- 不要回滚上述文件。
- 不要执行 `git clean`。
- 不要用桌面 `111` 中的旧 `.cs` 备份覆盖仓库。
- 不要修改现有一键编译、覆盖、推送流程。
- 涉及 Bannerlord API 后必须同时验证 1.3 和 1.4。

## 11. 构建与部署

构建：

```powershell
& '.\一键编译覆盖推送\build_single_module.ps1' `
  -ProjectRoot 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main' `
  -BannerlordRoot 'E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord' `
  -WorkshopContentDir 'E:\SteamLibrary\steamapps\workshop\content\261550'
```

部署：

```powershell
& '.\一键编译覆盖推送\deploy_module.ps1' `
  -ProjectRoot 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main' `
  -BannerlordRoot 'E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord' `
  -Configuration Debug `
  -BuildDll13 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main\bin\Debug\single_module_artifacts\versions\1.3\AnimusForge.dll' `
  -BuildDll14 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main\bin\Debug\single_module_artifacts\versions\1.4\AnimusForge.dll' `
  -BootstrapDll 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main\bin\Debug\single_module_artifacts\bootstrap\AnimusForge.Bootstrap.dll'
```

只启用统一模块 `AnimusForge`，不要同时启用退役的 `AnimusForge_1_3_x` / `AnimusForge_1_4_5`。

## 12. Tokens 修复后的最低验证清单

1. 新开局或旧档启动一个 NPC 外交事件。
2. 发起国公文到达目标王庭，目标国只产生一次正常回应。
3. 普通接受、拒绝、谴责或表态回应不再自动触发无限反向回应。
4. 真正的反提案仍可要求下一次答复。
5. AI 公文正常生成和结算时不再出现配套的第二次 `外交公文语义裁判` 请求。
6. 玩家自由输入外交文书仍会进行语义分析。
7. 统计一次完整事件的 generate / analyze / participate 调用数和 Tokens。
8. 对比 `prompt_cache_hit_tokens` 与 `prompt_cache_miss_tokens`，确认固定前缀重排后生成、分析和参与判断的命中率提高。
9. 双版本构建通过后再覆盖测试模块，并核对三份 DLL 哈希。

