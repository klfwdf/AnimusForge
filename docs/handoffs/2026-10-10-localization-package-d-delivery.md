# Package D：外交内政与内战显示 交付报告

状态：DELIVERED / 静态契约通过；编译相关验证 NOT-RUN（交主线）
基线分支：`main`；起始基线 `33a9ba44f`
工作包定义：[2026-10-10-localization-package-d-diplomacy-civilwar.md](2026-10-10-localization-package-d-diplomacy-civilwar.md)

---

## 一、Focused Commits

1. **`d7f2c0fe0`** `AF localization: add kingdom rebellion reason strings (Package D / D10)`
   - 2 个资源文件，成对 +3 个 Key。
   - **只提交这 3 行**：两个文件同时载有其他包 49 行未提交工作，因此用 `HEAD + 这 3 行`
     合成 blob 直接写索引，工作区未被触碰，其他包工作仍为 dirty。

2. **`a67d2e24e`** `AF localization: show Chinese in diplomacy and civil-war surfaces (Package D)`
   - 17 个文件（13 个 C# + 4 个资源），+1017 / −161。
   - Diplomacy 资源对各 +83 行（D1–D4 的 60 + D9 的 23）；Kingdom 资源对各 +41 行（D4）。

3. **`4cbcb4d11`** `tests: add the Package D localization contract`
   - 新增 `tests/modules/AF.Module.Diplomacy/PackageDLocalizationContractTests/PackageDLocalizationContractTests.py`（357 行）。

三个提交均为 `--only` / 精确路径提交；其他包当时存在的脏文件与未跟踪文件未被纳入。
索引在提交后为空。

---

## 二、显示投影结构（本包关键设计）

### 1. 内战失败与提示：并行投影通道

- `CivilWarEffectContext` 新增 `FailureCode / FailureArg / FailureArg2`，并在 `Notes` 之外
  新增并行的 `NoteProjections`。
- `Notes` 中的原始中文**逐字节不变**，继续供逻辑与历史消费；UI 只读投影。
- `CivilWarNoteProjection` 带对齐守卫：仅当 `projection.Text == raw` 才使用投影，否则回退原文，
  因此不会丢条或错标。
- 异常自带载荷：`CivilWarRetryableEffectException` 与新增的 `CivilWarReviewRequiredException`
  携带 `Code / Arg / Arg2`；**无法自我描述的非预期异常保留原始消息**，不猜翻译
  （`CivilWarEffects.cs:210-211` 注释即此契约）。
- 空 code 安全网：`FailureText("")` → `FailureFallback("")` → `_ => null` → 跳过投影 → 保留原始中文。
  含义：任何只设中文 `reason` 而未设 `Code` 的失败路径仍会显示中文，属"正确但不完整"；
  已用 25 个失败码 / 15 个提示码逐一核对，0 缺失。

### 2. 纪事（存档类型）

- `KingdomCivilWarHistoryEntry` 是存档类型（仅 `Week` + `Text`），**未新增存档字段**。
- 改为在写入边界增加显示专用兄弟参数：
  `PublishPoliticalResult(..., string text, IEnumerable<Hero> affectedParticipants = null, string displayText = null)`。
- 原因：`text` 有三个消费者——纪事、LLM/AFEF 政治结果素材、AFEF 事实行。
  直接替换会改变模型输入，故只增不改。

### 3. 单一来源就地本地化

`GetBilateralDiplomacyAgendaTitle` / `ProposerText` / `TypeText` 同时供 LLM 提示词与 VM 显示使用，
按"单一来源"就地本地化（同表达式内的兄弟 fallback 已是本地化文本，`GetDecisionDisplayTitleForAgenda`
也已是部分本地化且同样喂提示词），未拆双通道。

---

## 三、明确不动的三类文本

| 类别 | 例子 | 处理 |
| --- | --- | --- |
| (a) 玩家可见显示文本 | 面板、通知、按钮、状态 | 加 Key + 英文 fallback |
| (b) LLM 提示词 / 事实文本 | `RecordBilateralDiplomacyMemory` 叙事、`BuildDialogueFact`、AFEF 事实行、`ResolveRuleRuntimeText` 令牌、政治结果素材 | 不改（改了就是改模型输入） |
| (c) 逻辑令牌 | `tag.EndsWith("派")`、`name.IndexOf("为首的")`、`option.SponsorName == "未知"`、`RecruitBlockReason.Length == 0`、`[AFEF NPC行为补充]`、`【附加规则:kingdom_agenda】`、百科稳定度标记 | 不单方面翻转 |

已登记的例外：`KingdomCivilWarOwner.cs:703` 的日志现在输出本地化后的原因（一个值同时服务日志与 UI），
接受该取舍。

未知 code 与旧值一律回退到**真实原文**，绝不回退成通用英文串
（`LocalizeStoredProfileError:834 return value;`、`NoteDisplay` 的原文回退、D9 `OutcomeName` 的
`default: return outcome?.Name ?? "";`），契约测试对三个内战显示助手均断言此行为。

---

## 四、验证结果（均 exit 0）

| 检查 | 结果 |
| --- | --- |
| 静态契约（提交后重跑） | `PASS Package D localization contract: diplomacy pair=1059 kingdom pair=114 named keys=1216 fallbacks checked=938 dynamic keys=66 produced codes=39` |
| D4 清单 | `inline failure codes: 25, inline note codes: 15, manifest rows: 41` / `RESULT: PASS (0 problem(s))` |
| 合并后资源 | diplomacy EN=1059 CN=1059 placeholders=ok；kingdom EN=114 CN=114 placeholders=ok；EN 无 CJK |
| 反向对照 | 人为改坏一个 fallback 后门禁确实失败，证明断言有效（非空转） |

补充确认：`AF_DIP_UI_Vote_Meta` 的 C# 字面量与资源值**逐字节相同**（`Proposer` 后双空格、`·` 两侧三空格），
空白在本资源集中是语义的一部分，因此比较前只解码 C# 转义、不做空白折叠。

动态 Key 家族（`_Failure_ + code`、`_Note_ + code`、`_Source_ + id`、`_Stance_ + id`、`_Demand_ + id`、
`_FactionTag_ + id`、`AF_KINGDOM_CW_Notice_ + key`）在 C# 中由拼接产生，不存在完整字面量
（已全仓 grep 确认），故产出码表检查才是权威门禁，字面量扫描只作辅助。

---

## 五、未关闭项与遗留（如实登记）

1. **13 个继承性 fallback 漂移 Key（9 个不同 Key，均早于本包）**
   - 空白差异（12 行）：资源侧尾随双空格、C# 侧单空格。
     `AF_DIP_UI_WorldDiplomacy_ImpactDeltaLabel`×3、`_ImpactReasonLabel`×3、`_ActiveEventParticipants`、
     `_ActiveEventPrefix`、`_IndexMetaDeclaration`、`_StandingPrestigeLabel`、`_StandingReputationLabel`、
     `AF_DIP_UI_Vassalage_Terminal_IndependenceLabel`。
   - 措辞差异（5 行）：`AF_DIP_UI_KingdomProfile_GenerationConfigError`（`KingdomStrategicProfileBehavior.DevUi.cs:791`
     是陈旧副本，规范副本在 `KingdomStrategicProfileBehavior.cs:777` 且与资源一致）；
     `AF_DIP_UI_Vassalage_Terminal_{CannotViewLedger,NoSettlementGain,NotRuler,NotRulerShort}`
     （资源用弯引号 `’`，C# 用直引号 `'`）。
   - **处理：未改。** 逐条已用 `git show HEAD:<path>` 证明与基线逐字节相同，且不在任何本包 manifest 中。
     改 `WorldDiplomacyPresentationQueries.cs` 会让本包多出第 10 个 C# 文件，Vassalage 同理。
     门禁因此采用"默认严格 + 显式例外清单"：新增漂移会失败，继承漂移打印 `NOTE` 列出实际命中的 Key，
     而非静默放过。

2. **D10 两个未收口残留**
   - `KingdomRebellionEditorController.cs:259`：已有现成 Key `AF_UI_KingdomRebellion_NamingBackgroundFailurePrefix`，一行可修。
   - `KingdomStabilityGameAdapter.cs:387-426`：整面重写，超出本包范围。

3. **`AF_KINGDOM_CW_Behavior_*`（11 个）**：已确认定义在 Diplomacy 资源对；
   `CivilWarCampaignBehavior.cs` 保持未编辑。测试范围据此按"两对资源联合查找"实现，
   因为 `KIN_SOURCE_DIRS` / `DIP_SOURCE_DIRS` 共用 `src/AF.GameAdapter.Bannerlord/UI/Kingdom`，
   单对查找会报 418 个假缺失。

4. **中文资源中无 CJK 的条目（信息性，非缺陷）**
   - Diplomacy 8 条：`AF_DIP_UI_KingdomProfile_IdLabel`、`_GenerationAttemptsSuffix`、
     `AF_DIP_UI_Gathering_HeroLabel`、`_HeroHint`、`AF_DIP_UI_KingdomProfile_ImportFileError`、
     `AF_KINGDOM_CW_Panel_ShortName`、`AF_KINGDOM_CW_Panel_ValueWeekNumber`、
     `AF_DIP_UI_WorldDiplomacy_Reject_WithCode`。
   - Kingdom 1 条：`AF_KINGDOM_CW_NoteSeparator`。均为纯占位符 / 分隔符 / 数字模板。
   - 注意：`[\u4e00-\u9fff]` 只覆盖汉字，全角标点（U+FF08/U+FF09）读作"无 CJK"，
     故该清单是信息而非缺陷清单。

5. **编译相关验证 NOT-RUN**：本包未编译、未构建、未部署、未打包、未推送。
   `BannerlordApi=1.3` / `1.4` 两实现编译与实机验收交主线统一执行。
   静态契约不构成游戏或编译器验收。

6. **子智能体异常（如实登记）**：D9 的执行子智能体结束时既无收尾消息也未产出 `D9.json`，
   其 C# 改动留在工作区。已从 `git diff` 反推并手工补齐 `D9.json`（23 条），
   再走正常合并与校验流程；未因此跳过任何校验。

7. **未验证边界**：本包只做静态资源/源码一致性验证，未在游戏中打开任何面板确认最终渲染文本；
   显示宽度、换行、字体回退等实机表现未覆盖。

---

## 六、Module.UI 资源对的所有权说明（需主线确认）

D10 的 3 个 Key 落在主线所有的 `content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml`
与 `CNs/af_ui_strings-zh-CN.xml`，且该文件当时载有其他包 49 行未提交工作。

- 已用 blob 合成方式只提交这 3 行；其他包 49 行仍为 dirty（已核对：HEAD 含这 3 个 Key，
  工作区中非 D10 的新增行仍为 49）。
- **不能整文件提交**，也不能用 `git checkout HEAD -- <path>` 或 `git commit --only <path>`，
  否则会连带提交或丢弃其他包的工作。
- 保留理由：配对的 C# 改动把可见中文原因换成英文 fallback，若不带中文 Key 即为中文界面回归。
  如主线决定不接收该文件，请连同该回归风险一并处置。
- 该资源对的 `<string>` 缩进惯例是 2 空格（`af_ui_strings.xml` 缩进普查 `{0:3, 2:3004, 4:125, 6:1}`），
  与 Diplomacy / Kingdom 两对的 4 空格不同；本包这 3 行已按 2 空格落位，消除了原先唯一的 6 空格离群行。

---

## 七、临时产物

`.tmp_pkg_d/` 为本次的临时工作目录（Key manifest、合并驱动、校验脚本），未纳入任何提交。
探针脚本与中间诊断输出已在交接前删除。
