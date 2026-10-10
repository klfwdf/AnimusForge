# 第二轮 C：政策、王国、外交、周报、记忆知识 — 交付记录

状态：DONE（含 NOT-RUN 与待裁决项）。工作区 `F:/AnimusForge-main`，分支 `main`。
上游：[审查与共同约束](2026-10-10-localization-wave2-review.md)、[C 接力](2026-10-10-localization-wave2-c-handoff.md)。

本轮**未构建、未 Stage、未部署、未打包、未 push**；双版本统一交主线（接力共同约束第 6 条）。

## 一、提交清单（10 个 focused commit）

| SHA | 内容 |
|---|---|
| `35e1eee77` | CivilWar：面板/通知显示投影层（28 键） |
| `98bacc340` | Knowledge：RAG 失败弹窗外壳 + 资源三项修复（6 键） |
| `8c0dc0c8e` | Policy：管理面成本/历史与 NPC 统治者重试弹窗（34 键） |
| `b06c7342a` | Kingdom：内政页 prefab 与叛乱/命名原因（38 键） |
| `afb84709c` | CivilWar：复审发现的投影缺口收口（2 键） |
| `758228e53` | Diplomacy：Vassalage 文本字面换行修复（0 新键） |
| `f87f3c77b` | Memory：编辑器 Speaker fallback（复用既有键，0 新键） |
| `a7833ff80` | Weekly：批次/分组标签 UI 专用投影 + 新建资源对（7 键） |
| `4b2821e2a` | Diplomacy：Vassalage 与 WorldDiplomacy UI 外壳（119 键） |

新增键合计 **234 对**（CivilWar 30 + Knowledge 6 + Policy 34 + Kingdom 68 + Weekly 7 + Diplomacy 119），
另复用既有键 16 个（Policy 15 + Memory 1）。跨资源键冲突 **0**。

各资源实测键数：`afkingdom` 73、`afknowledge` 640、`afpol` 375、`afdip` 976、
`afweekly` 7、`afmemory` 242（未改）。

## 二、各模块结果

### 1. Kingdom CivilWar — 显示投影（本轮重点）

复审指出「CivilWar 面板诉求、建派选项和完整名称继续使用中文原始规则文案」，
以及「两个通知入口直接显示共享的中文 `result.Message`」。本轮按**显示边界投影**收口，
不改任何共享事实。

**投影层**（`CivilWarUiText`）：
- `DemandDisplayText(demand, target)` — 诉求显示
- `FoundingOptionText(demand, target, rawText)` — 建派菜单项
- `FactionDisplayName(faction, demand)` — 面板派系名
- `ResultNotice(result)` — 通知投影，**无键即回退原始 `Message`**

**事实层原样保留**（有消费者证据）：
- `CivilWarCatalog.FactionNameFormat` / `demand.Text` / `FormatDemand` / `FactionName` /
  `faction.TargetName` —— 仍服务 `AddHistory`、`PublishPoliticalResult`、
  `WriteFact`（AFEF）、周报素材。`CivilWarCatalog.cs` 本轮零改动。
- `CivilWarActionResult.Message` —— 存档 receipt（`CivilWarOperation`）、AFEF、周报素材。
  新增的 `MessageProjectionKey/Arg/Arg2` 是**瞬态**字段，已核实不进入
  `CivilWarOperation`，也不被 `JsonConvert.SerializeObject(_owner.Storage)` 触及。

**可靠 fallback**（复审明确要求）：`ResultNotice` 在 key 为空或未知时返回原始 `Message`，
因此**旧存档回执**与本构建未描述的结果都显示真实原文，绝不用 `Status` 反推。
已核实全仓无任何 `Status == Applied ? 本地化 : 原文` 式推断。

**复审发现并已修复的 5 个缺口**（`afb84709c`）：
1. `ChangeDemand` 原本**没有投影键** → 新增 `DemandChanged`，`ChangePlayerFactionDemand`
   追加 `out` 参数输出显示用诉求与保留家族数。
2. 最后通牒「接受」路径直接返回 `result.Message` → 改走 `ResultNotice`。
3. `DescribeUltimatum` / `DescribeFollowPrompt` 把中文 `FactionName`/`FormatDemand`
   插进英文模板 → 改用显示投影。
4. `ResolveDemandTargetDisplay` 的空值守卫**失效**：它检查
   `CivilWarWorld.KingdomName(null)` 的返回值，而该值是中文字面量「某王国」、非空白。
   改为检查 `Kingdom` 对象本身；`FactionDisplayName` 对 `ClanName` 的「某家族」同样处理。
   两者都回退到已存事实，不回退到占位符。
5. `KingdomFactionTab.cs:354` 用中文「最后通牒」比较已本地化的 `DeadlineLabel`
   → 改为稳定标志 `DeadlineIsUltimatum`，与 `:431` 的 `NoFactionFormed` 同一模式。

**语言相关字符串比较清单**（本轮已消除的）：
| 位置 | 原写法 | 现写法 |
|---|---|---|
| `KingdomFactionTab.cs:431` | `_panel.StageText == "尚无派系成形"` | `_panel.NoFactionFormed` |
| `KingdomFactionTab.cs:354` | `f.DeadlineLabel == "最后通牒"` | `f.DeadlineIsUltimatum` |

**仍存在、未修（有证据的配对契约）**：
`UI/Kingdom/EncyclopediaKingdomStabilityPatch.cs:12` 的哨兵 `"【王国稳定度】"`
与生产者 `Kingdom/KingdomStabilityGameAdapter.cs:401` 配对，且该哨兵**从不上屏**。
只改消费者会让 `RemoveExistingStabilityBlock` 永久失效、百科稳定度区块每次刷新重复追加；
只改生产者同样立即出现重复。**必须成对处理**，故两边均未动。建议做法见第七节。

### 2. Policy

- `CustomPolicyBehavior.Management.cs`：`BuildPolicyRecordCostText`、
  `BuildLocalPolicyHistoryData`、重评议可用性/错误提示、冻结/重试/公告 fallback 全部收口，
  删除 50 条中文字面量。新增两个**仅显示用**成本片段 helper，使第纳尔/影响力单位也能本地化；
  共享的中文 `FormatGoldCostText`/`FormatCostText` 未动（其他消费者会持久化其输出）。
  另有 15 个既有键本次接线，其中 5 个此前**零引用**。
- `NpcRulerPolicyBehavior.Generation.cs`：重试弹窗标题/正文/按钮本地化。
  `InquiryData` 实参位置、重试委托、`CustomPolicyResultPopup.ShowRetry` 签名均未变。
- **`CustomPolicyBehavior.Effects.cs` 零改动**：其 78 条 CJK 全部是存档
  `EndReason`/`TargetLabel`/`KingdomName`、LLM 修复提示词输入，或日志文本。
  另有测试不变量 `!ContainsAsciiLetter(line)`（`PolicyEffectModule.ContractTests/Program.cs:4576-4600`）
  直接约束这些输出行，译成英文会违反该断言。

### 3. Kingdom（非 CivilWar）

- `KingdomInteriorPanel.cs`：22 处 prefab 文本改走 `KingdomFactionUiText`。
  **重要约束**：这些值在 prefab 构造期被拼进**单引号 XML 属性** `Text='…'`，
  zh-CN 译文不得含 `'` `<` `>` `&`，否则 `XmlDocument.LoadXml` 抛异常、整个内政页 prefab 加载失败。
  写入脚本已内置拒绝逻辑，21 个 `AF_KINGDOM_UI_Interior_*` 键已逐条校验通过。
- `RebellionRules.cs` / `RebellionNamingOwner.cs`：17 条上屏原因与命名失败外壳本地化。
- `RebellionNamingRules.cs` 保留：`BuildSystemPrompt`/`BuildUserPrompt` 全文与素材模板
  进 LLM（`KingdomRebellionRuntimeController`、`NpcPersonaGenerationApplicationAdapter`）；
  `Trim(…)` 字符集用于解析模型输出，非上屏。
- 独立复核：Kingdom 模块 CivilWar 之外仅剩 `RebellionNamingRules.cs` 一个文件含 CJK（46 条），
  全部为上述 prompt/素材，**无遗漏的显示外壳**。

### 4. Knowledge

- 两处 RAG 失败弹窗（`KnowledgeLibraryBehavior.cs:8482/8513`）标题与固定说明改走
  `KnowledgeUiTexts`。模型原始回复 `raw` 与 `ex.Message` **逐字保留**。
- RAG 短句、关键词、语义原型、嵌入输入、知识正文全部未动（含
  `BuildDeterministicRagShortTextFallback`、`BuildDeterministicPrototypeFallback` 的产出）。
- 规则/映射类型显示标签复核结论：`UI/Editors/` 下**不存在** Knowledge 编辑器，
  编辑器全部在 `KnowledgeLibraryBehavior.cs` 内；文本映射 `Kind`/`CategoryKey` 标签
  已由既有提交 `c753ee587` 本地化，底层 `option.Kind` 仍作 `InquiryElement` 标识符。
  143/143 个 `TextMappingKind*` 常量值在 EN/CN 均有键。
- 保留：`KnowledgeLibraryBehavior.cs:1418` 的 `RuleIndexItem.Label` **同时是搜索命中值**
  （`IsRuleListMatch:1459` 用 `IndexOf`），不得翻译。

### 5. Memory

- `MemoryEditorController.cs:498`：空 `Speaker` 的显示 fallback 改走既有键
  `AF_MEMORY_UI_Manual`，与 `MemoryEditorProjection.cs:208,220` 的既有投影一致。
- **未翻译且不得翻译**：`MemoryEditorController.cs:757/764/816` 的「手动」是**存档哨兵**，
  随 `DailyMemoryDraft.Lines` 经 `CampaignMemoryPersistenceAdapter.cs:27-30` 写入
  `_af_dailyMemoryDrafts_v1`，同时进 LLM；生产者 `MemoryDeveloperEditOwner.Daily.cs:351`。
- `MemoryRecallContextOwner.cs:475,477` 的「时」是**双用途**：既进 Prompt（`:199`、`:372`），
  又进编辑器 UI。按双用途规则保留；如需本地化须在显示边界另做投影。

### 6. Weekly

`BuildWeeklyReportBatchDisplayLabel` 与 `BuildWeeklyReportGroupDisplayLabel` 经消费者追踪确认
是**双用途**：同一字符串既进 prompt 交换日志（`Logger.LogEventPromptExchange`，经
`WeeklyGenerationAttemptOwner`），又进编辑器 UI。因此在原地翻译会改变被记录的素材，
改为**新增两个 UI 专用生产者**（`...ForUi`），原方法字节不变。

- 编辑器批量列表与失败/重试弹窗改读 UI 投影，投影缺失时**回退原值**。
- `WeeklyReportRetryContext` 新增瞬态 `FailedGroupTitleForUi`；`FailedGroupTitle` 本身未变。
- `TranslateEventKindForDev` / `TranslateEventMaterialTypeForDev` **逐字保留**（LLM 素材类型标签）。
- 新建 `afweekly_ui_strings` 成对文件（7 键，UTF-8 无 BOM + CRLF）。
- 该分区子智能体**实跑了双实现构建**：1.3.x + 1.4.x + Bootstrap 全部成功，
  `Build Result : success (dual coup seam gates passed)`，其改动文件零 error / 零 warning。
  （注意：这是本包唯一一次构建，且发生在他人在途改动造成整仓 break **之前**；见第七节。）

### 7. Diplomacy

Vassalage 与 WorldDiplomacy 的 UI 外壳已改走模块既有 `DiplomacyUiLocalization`
（`Text`/`Format`），未新建第二套 helper。字面换行缺陷由本包修复并提交（`758228e53`）。

**保留项（有证据）**：`GetVassalageTypeDisplayName` 的朝贡国/卫戍国/附庸国进 LLM 素材
（`CaptureWorldBulletinVassalage` → `WorldBulletinCampaignMaterialPolicy`）；
`TryApplyVassalageAction` / `TryCreatePlayerVassalage` 的 `statusText` 是 **AFEF 事实**；
`:3852/:5280/:5639/:5706` 的 `reason` 是**归因记录**（`OnVassalRelationshipEndedForExternal`）；
`document.MechanicalResult` 同时进存档与素材；`"外交宣言"`/`"外交回应"` 作为文档 `Title` 被保存。

**未闭环（不计入完成）**：`Agenda/VoteDealBehavior{,.Agenda}.cs` 的内政面板文案（约 129 行，
同文件还含 prompt 与 AFEF 事实，需逐行分诊）与
`Profiles/KingdomStrategicProfileBehavior.cs:774/822-823/881`（与
`KingdomStrategicProfileBehavior.DevUi.cs:785-810` 的中文前缀比较耦合）。
子智能体已确认**完全未改动**这三个文件，故其中文是「未触及的残余」而非半截迁移。

**本包另修的两处该分区遗漏**（`4b2821e2a` 内）：
1. `WorldDiplomacyAnalysisApplication.cs:495` 的 `port.Notify` 仍硬编码中文前缀，
   而其同级分支已投影 → 新增 `DeclarePublishedMechanicRejected`。
2. `DescribeRejectedPlayerMechanic` 中 `warReason` 分支返回硬编码中文 `"宣战未执行："`
   → 新增 `Reject_DeclareWarNotExecuted`。

**回退了一处越界改动**：子智能体把 `DescribeRejectedPlayerMechanic` 末尾的机器码包裹
从全角 `（code）` 改为半角 `(code)`。该串经 `port.Notify` 上屏，但属**本地化范围外的行为改动**
（`code` 是机器令牌，本就不翻译），已按要求回退为全角，使 diff 严格限定于本地化。

**该文件保留的 5 处中文**：全部是 `document.MechanicalResult` 写入点
（`:73/:270/:326/:389/:491`）。该字段被保存并作为「经游戏机制确认：」正文进入正史与素材，
属双用途，故保留。**已知观感问题**：`readableReason` 同时服务该保存字段与上屏 Notify，
因此正史里会出现「中文前缀 + 英文详述」的混排；这是「投影做在共享值上」的固有取舍，
若要彻底分离需把 `MechanicalResult` 的构造也拆成保存值与显示值两路。

## 三、资源与注册

六对资源：`afpol_ui_strings`、`afdip_ui_strings`、`afkingdom_ui_strings`、
`afmemory_ui_strings`、`afknowledge_strings`，以及**新建**的 `afweekly_ui_strings`。

**注册是双重的，两半都必需**：
1. `content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml`（EN）
   与 `.../CNs/language_data.xml`（zh-CN）中的 `<LanguageFile>` 条目 = 游戏加载器索引；
2. `content/content-map.json` 中的 `{owner, source, target}` 条目 = Stage/deploy 投影。

**实测注册状态**（脚本核验，非推断）：

| 资源 | EN 索引 | zh 索引 | content-map |
|---|---|---|---|
| afpol_ui_strings | ✅ | ✅ | ✅ |
| afdip_ui_strings | ✅ | ✅ | ✅ |
| afkingdom_ui_strings | ✅ | ✅ | ✅ |
| afmemory_ui_strings | ✅ | ✅ | ✅ |
| afknowledge_strings | ✅ | ✅ | ✅ |
| **afweekly_ui_strings** | ❌ | ❌ | ❌ |

`afweekly_ui_strings` 是**本轮新建**，按接力共同约束第 5 条，注册由 A 集成。
A 需补 4 行（格式取自现有条目）：

`content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml`：
```xml
  <LanguageFile xml_path="afweekly_ui_strings.xml" />
```
`.../CNs/language_data.xml`：
```xml
  <LanguageFile xml_path="CNs/afweekly_ui_strings-zh-CN.xml" />
```
`content/content-map.json` 的 `entries` 追加（owner 沿用该模块既有值 `AF.Module.Weekly`）：
```json
{ "owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/ModuleData/Languages/afweekly_ui_strings.xml", "target": "ModuleData/Languages/afweekly_ui_strings.xml" },
{ "owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/ModuleData/Languages/CNs/afweekly_ui_strings-zh-CN.xml", "target": "ModuleData/Languages/CNs/afweekly_ui_strings-zh-CN.xml" }
```

**未注册的后果已被既有事实证明**：`afmemory_ui_strings` 曾「有文件、有 map 条目、无 `LanguageFile`」，
中文永不加载（复审 P1，主线 `d0144359e` 修复）。Weekly 目前是同样状态，**中文不会生效**。

## 四、验证结果

| 项目 | 方法 | 结果 |
|---|---|---|
| 六对资源自洽 | 自写 `verify_c_pairs.py`（键集对称 / 占位符一致 / 无空文本 / 无 BOM） | **PASS 6/6**，`ph_mismatch=0 empty=0` |
| 跨资源键冲突 | 全 `content/**/Languages` 扫描，按资源基名归并双胞胎 | **0 冲突**（8592 个不同键） |
| 字面换行缺陷 | 全六对 `text` 属性扫描 `\n` | **0 残留**（修复前 Policy 1、Diplomacy 20、Knowledge 22 行） |
| 投影契约 | 每个 `Project` 键 ↔ 每个 `NoticeFallback` 分支 | **26 ↔ 26，无孤儿、无死分支** |
| 注册状态 | 两份 `language_data.xml` + `content-map.json` 逐对核对 | 5/6 完整；`afweekly` 待 A |
| C# 语法 | `csc /nostdlib+` 解析（不产生产物） | **无 CS1xxx/CS0116 语法错误**；其余诊断均为 `/nostdlib` 缺引用所致 |
| 空白检查 | `git diff --check` | **exit 0** |
| 提交完整性 | `git merge-base --is-ancestor` | 全部提交均为 HEAD 祖先 |

**资源契约整体运行的注意事项**：`LocalizationResourceContractTests.py` 当前会因
**B 包** `afworldmap_strings`（约 76 个 EN-only 键）而失败——该文件正被另一 Agent 并发编辑，
与本包无关。本包六对资源已用独立脚本证明自洽，且**未修改该共享测试**。

## 五、保留项（未翻译）汇总与证据

| 类别 | 代表位置 | 流向 |
|---|---|---|
| Prompt 正文 | `RebellionNamingRules.cs:115-162`、`CustomPolicyBehavior.Management.cs:497-500` | LLM prompt |
| LLM 素材 / 周报正文 | `RebellionNamingRules.cs:166-267`、`WeeklyAggregateEventLineOwner.cs`、`WorldBulletinCampaignMaterialPolicy.cs` | 模型素材 |
| AFEF / 存档事实 | `PublishPoliticalResult`、`ctx.Notes`、`CivilWarEffects.reason`、`MemorySummaryRules.cs` | 存档 / AFEF |
| 内部匹配值 | `KnowledgeLibraryBehavior.cs:1418`（同时是搜索命中值）、`RebellionNamingRules.cs:38` | 匹配 |
| 协议标签 | `give_troops`、`[NAME]/[SHORT]/[LORE]`、`status\|source\|condition` | 协议 |
| 存档哨兵 | `MemoryEditorController.cs:757/764/816`「手动」、`KingdomStabilityGameAdapter.cs:56` 前缀 | 存档 / 剥离 |
| 日志 | `PolicySystemLog`、`Logger.Log` 各点 | 日志 |
| 注释中文 | 各文件 | 非上屏 |

**残余 CJK 计数（不得当作完成度）**：C 范围代码仍有 6968 条 CJK 字符串字面量 / 221 文件，
其中被本包改动过的 9 个文件占 490 条。绝大多数属上表白名单类别。
按复审第 36 行的告诫，**该数字不是漏译条数，也不是完成百分比**；本包只对
「纯上屏且不落存档/不进 LLM」的外壳负责，其余逐条追踪消费者后保留。

## 六、已知未闭环

1. **`afweekly_ui_strings` 未注册**（第三节）——中文不会加载，需 A 补 4 行。
2. **Diplomacy 两处 UI 残余未处理**（见第二节第 7 小节）：
   `Agenda/VoteDealBehavior{,.Agenda}.cs` 内政面板、`Profiles/KingdomStrategicProfileBehavior.cs`
   生成错误。两者均**完全未改动**。
3. **Memory 12 处上屏中文**（见第七节待裁决 2）。
4. **CivilWar 对话标签通道**（见第七节待裁决 3）。
5. **百科稳定度哨兵**（见第二节第 1 小节末）。
6. **`afdip` 中 `readableReason` 的双用途混排**（见第二节第 7 小节末）。

以上均**未标为完成**。

## 七、NOT-RUN 与待裁决

**NOT-RUN（务必如实对待）**
1. **本包未自行构建**：1.3 / 1.4 双实现与 Bootstrap 未由本包编译验证。
   例外：Weekly 分区子智能体在其改动后跑过一次完整双实现构建并通过
   （`Build Result : success`），但那是**单点、且发生在他人在途改动造成整仓 break 之前**，
   不能代表本包全部改动可编译。**本包所有 C# 改动均未经过编译器验证**，
   仅做过 `csc /nostdlib+` 解析级语法检查（无 CS1xxx 语法错误）与括号/字符串平衡检查。
2. **未运行测试**：`PolicyEffectModule.ContractTests`、`MemorySummaryMainThreadBoundaryTests`、
   `J17ImportExportTests`、`LocalizationRuntimeTests.ps1` 均未运行。
3. **未实机验证**：无游戏内运行、无 UI 截图、无输入焦点与英文截断确认。
4. **未验证旧档**：未加载任何既有存档。CivilWar 新增字段已确认为瞬态且不参与序列化，
   但未实测。
5. **整仓构建当前被 B 包在途改动阻断（与本包无关）**：`WorldMapPartyCommandBehavior.cs:6111`
   与 `WorldMapPartyCommandBehavior.Admission.cs:271` 调用 `CommandMessageForDisplay(...)`，
   该符号在仓库中不存在（HEAD 两文件均无此调用）。属 WorldMap 在途编辑，
   本包未触碰该两文件。

**待裁决（未标完成）**
1. **Policy 既有红灯（非本轮引入）**：`tests/bridges/Policy/PolicyEffectModule.ContractTests/Program.cs:14286/14289`
   断言 `"无持续数值效果"` / `"持续效果已结束"`，而 wave-1 提交 `07fcfc634` 已把这两处改为
   `PolicyUiTexts.Get(...)`。测试进程从不设置语言，`MBTextManager` 默认 `"English"`，
   故返回英文 fallback → 断言必然失败。属 A 的共享测试范围，本包未改。
2. **Memory 12 处上屏中文被测试契约钉死**：`MemorySummaryQueueRunRuntime.cs:73,113,116,118,126`
   与 `MemoryRecallContextOwner.cs:92,133,147,235,244,253,266` 是真玩家可见中文，经追踪
   **不持久化、不进 LLM**，技术上可译。但四组已评审契约按字面量比较
   （`MemoryFailureUiBoundaryTests/run.py:21-31` 与历史基线逐字比对、
   `MemoryRecallContextTests/Harness.cs.txt:54,56` 的 `Contains`、
   `MemorySummaryMainThreadBoundaryTests/run_business.py:132-133` 的精确匹配、
   `BusinessHarness.cs.txt`/`TerminalHarness.cs.txt` 的精确断言，另有 `source_parity.py` 哈希锁）。
   改源码会立即造红且 `tests/**` 在禁区外。**建议 A 先把断言改为「生产者被观测」而非
   「字面量相等」，再落这 12 处键。**
3. **CivilWar 对话标签通道仍显示原始中文**：`KingdomCivilWarOwner.cs:845/859/866/871/874/890/904`
   与 `DialogueTags.cs:61-71` 的 `RecruitBlockReason` 经 `TryApplyTag` →
   `RewardSystemBehavior.cs:16213` 上屏。该链路跨出本包文件边界，未改。
4. **四个投影的 `{A}`/`{B}` 携带中文事实**（`Conceded` 的 `ctx.Notes`、
   `ConcessionPending` 的 `reason`、`RetrySafeSteps`/`IncompleteReview` 的 `ex.Message`）。
   这是**有意为之**：这些串是存档/AFEF 事实，按白名单不得翻译。结果是「英文句式 + 中文尾巴」，
   属正确性与本地化的取舍，非缺陷。
5. **百科稳定度哨兵**（第二节末）：需生产者与消费者成对修改，本包两边均未动。
6. **Knowledge 动态异常文本仍中文**：`KnowledgeLibraryBehavior.cs:9067/9072/9111`，
   经 `ex.Message` 上屏；且共享弹窗小标题在
   `UI/Errors/LlmRetryPrompt.cs:67/68/73`（跨 Policy/Illustrator 共用），不在本包写权限内。
7. **`AF_UI_Developer_*` 等 A 域键**：扫描显示大量 `AF_UI_Developer_*` 键在代码中被引用，
   属 A 的 `af_ui_strings` 范围，本包未处理（经全资源扫描确认它们**已定义**，
   早先「777 未定义」是对照集合只取六对资源所致的假象）。

## 八、回滚

对本轮 focused commit 作 `git revert <SHA>`；不重写历史，不回滚其他作者。
7 个提交相互独立，可按模块单独回退。`758228e53`（字面换行）与 `f87f3c77b`（Memory fallback）
不依赖其他提交，可最先回退。

## 九、并发作业教训

本轮主线有多个 Agent 高频并发提交（HEAD 从 `8ca1d4324` 推进）。两次实测影响：

1. **共享契约测试会被他人文件带红**：`LocalizationResourceContractTests.py` 因 B 包
   `afworldmap_strings` 正在编辑而失败。**必须区分「本包资源是否自洽」与「全仓契约是否绿」**，
   故本包改用独立脚本证明自己六对资源一致。
2. **`git diff` 的干净结论不可单独作为完整性证据**（沿用上一轮教训）。

**做法**：本包每个资源写入都用带断言的脚本（拒绝 BOM、拒绝会破坏单引号属性的字符、
保留既有 `&#10;` 实体），并在写入后立即用独立脚本复核键集对称与占位符一致。
