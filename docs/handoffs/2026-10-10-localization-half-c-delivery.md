# AnimusForge 中文硬编码本地化接力（Half C）— 交付记录

状态：DONE（含 NOT-RUN 项）

上游接力说明：`docs/handoffs/2026-10-10-localization-half-c-handoff.md`

## 一、本次交付的 7 个 focused commit

| commit | 内容 |
|---|---|
| `94db4bcb5` | Weekly：周报生成通知改走 `AF_WEEKLY_*`（12 键，EN+zh-CN） |
| `4ed8e1416` | DialogueUI：给予/展示面板类型标签与 3 条提交期校验错误改走 `AFD_UI_Trade_*`（7 键） |
| `33b24d405` | 修 DialogueUI 测试夹具缺 `DialogueUiLocalization` 导致的 CS0103 编译回归 |
| `360d094cb` | 新建并登记 `afkingdom_ui_strings` 中英资源对（5 个种子键） |
| `1b56b271e` | 在 `run.py` 登记 Kingdom 资源对；把政变面板断言改为匹配英文 fallback |
| `3f385af5a` | 修复 `DuelSettings.cs` 49 处畸形的 MCM group 属性（整树编译阻断） |
| `2052ebc5a` | 修 Weekly `Ui(...)` 传 int 导致的 CS1503（由主线代为提交） |

## 二、各模块结果

### 1. Kingdom CivilWar UI — 已完成（主线提交 `1389afba0`）

原工作区最大的缺口。由于发现 `CivilWarUiText.cs` 被另一 Agent 实时编辑，经用户裁决后
把 Kingdom 让给该 Agent，并由我派出 8 个分区子智能体并行产出键表与证据，最终由主线
提交为 `1389afba0 feat: localize kingdom civilwar ui`（10 文件，+996/-223）。

覆盖范围：面板/步骤/提示、核心历史/通知、动作报价与回执、政变/事件/玩家控制。

**关键边界判定（全部有代码证据，均已遵守）：**

- `PublishPoliticalResult` 的 text 是**四用途**（面板 + 存档 + 周报素材 + AFEF），一律不译。
- `ctx.Notes`、`CivilWarEffects` 的 `reason`、`RestoreCoupDynastyEffect` 同理，一律不译。
- `CivilWarCatalog.FactionNameFormat` / `demand.Text` **不可英文化**：
  `KingdomCivilWarOwner.FactionTag()` 用 `IndexOf("为首的")` 做中文结构解析，
  `FactionName()` 用 raw `.Replace("{leader}", ...)`。改英文会让 Tag/ShortName 静默退化。
- `CivilWarUiText.SourceName` 以中文显示名做 switch key，故 `CivilWarCatalog` 的
  grievance `Name` 本轮保持零改动。
- `KingdomFactionTab.cs:431` 对 `StageText == "尚无派系成形"` 做**跨程序集字符串比较**，
  该键中文侧必须保持「尚无派系成形」。
- `DialogueTags.cs` 的 `【派系运行时事实】` 整段为 LLM 事实正文，逐行保持原样。

### 2. Weekly / World Bulletin UI — 已完成

- `WeeklyReportRuntimeOwner.cs`：30 处 `DisplayMessage` / `FullFailure` 固定外壳改走
  `AF_WEEKLY_*`；新增 12 键（EN+zh-CN）。
- `WorldBulletinCampaignMaterialPolicy.cs`：**零改动**。该文件全文为公报正文生成，
  属"报告正文/素材"，按接力说明不翻译。
- 同一文件内 `BuildWeeklyReportBatchDisplayLabel`、`BuildWeeklyReportGroupDisplayLabel`、
  `TranslateEventMaterialTypeForDev` 等**同时进入 LLM 素材**的字符串保持原样。

### 3. DialogueUI UI — 已完成

- `DialogueUiSettings.cs` 与 `AFDialogueNativeOverlay.xml` 由另一 Agent 在 `dc25986b2` 完成，
  本次复核确认无需追加。
- `InlineTradeBridge.cs`：4 个显示字面量 + 3 条异常外壳改走 `AFD_UI_Trade_*`（7 键）。
  **`"give_troops"` 协议标签保持字面量**（被 `InlineTradeBridge`、`SceneTradeController`、
  `SceneShoutInputController`、`CourierDeliveryBehavior` 做相等比较）。

## 三、资源与契约

- 新建资源对：`content/modules/AF.Module.Kingdom/ModuleData/Languages/afkingdom_ui_strings.xml`
  与 `CNs/afkingdom_ui_strings-zh-CN.xml`（UTF-8 无 BOM，CRLF，中英键集一致）。
- 登记位置：`content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml`
  与 `CNs/language_data.xml`，以及 `content/content-map.json`。
- **登记是强制的**：`run.py` 要求 language_data 中每个 `LanguageFile` 都在 content-map 中映射；
  未登记的 `afmemory_ui_strings` 即为反例（有文件、有 map 条目，但无 `LanguageFile`，
  因此中文永不加载）。

## 四、验证结果

| 项目 | 命令 | 结果 |
|---|---|---|
| 资源对契约 | `py tests/content/J15ContentContractTests/LocalizationResourceContractTests.py` | **PASS pairs=23** |
| 空白检查 | `git diff --check` | 通过（exit 0） |
| 双实现构建 | `scripts/build/build_single_module.ps1`（1.3/1.4/Bootstrap） | **Build Result : success**；1.3、1.4、Bootstrap 三个产物均已生成 |
| 键覆盖 | 自查：CivilWar 代码引用 286 个 `AF_*` 键（4 个为拼接前缀） | **282/282 具体键全部已定义，0 缺失** |
| content-map 数量 | 自查复现 `run.py:961/965` | 3404 == 3404，无 map-only / expected-only |

## 五、NOT-RUN 项（务必如实对待）

1. **实机未验证**：无游戏内运行、无 UI 截图确认。所有结论均来自编译、契约测试与静态追踪。
2. **旧档未验证**：未加载任何既有存档。Kingdom CivilWar 的 `History` / `Operations` 走
   `SaveChunkedString` 分块（12000B/块），英文变长理论上无溢出风险，但未实测。
3. **全量内容契约未跑通**：`run.py` 在 `:861` 因既有 GCCZ 漂移中止
   （`GcczTownPrompt.zh-CN.json`，HEAD `52177454b` 干净、非本次改动）。
   按接力说明**未刷新该 Prompt 基线**。我的登记检查（`:961/965`）在其后，已单独复现通过。
4. **运行时本地化解析测试未通过，且与本次改动无关**：
   `LocalizationRuntimeTests.ps1` 在 `AF_MCM_Tts_ExtraParam_Hint` 处报 Text mismatch。
   根因是该键文本含**未转义的花括号**（`{"disable_markdown_filter":true}`），
   `TextObject` 会将其解析为变量引用。源自 `2c9872d45`（另一 Agent 的 TTS 本地化提交）。
   主线随后以 `364d528c8` 修复，但**本记录未在修复后重跑该测试**。
5. **`CivilWarUiText.Stance` 为无调用者的死方法**（`AF_KINGDOM_CW_Stance_*` 三键因此未定义），
   本轮未删除，留待 owner 处置。
6. **UI 截断观感未验证**：英文比中文长约 1.6–2 倍，面板 `CivilWarWorld.Limit(text, 46)`、
   `KingdomFactionTab` 的 `Clip(18/40/48)` 与 `Brief()` 的 14 字符上限会让英文更早被截断，
   禁用按钮副标题可能回落为通用文案。属显示观感问题，需实机确认。

## 六、回滚

每个切片均为独立 focused commit，使用 focused inverse commit 回滚，禁止 hard reset。
`3f385af5a` 修的是他人在 `2c9872d45` 引入的整树编译阻断，回滚前请先确认该缺陷已另行处理。

## 七、协作注意（重要）

本次作业期间，**主线上有其他 Agent 高频并发提交**（HEAD 从 `b705d7a09` 推进到 `5684e7786`），
并出现两次同路径争用：

- `CivilWarUiText.cs`（未跟踪、无引用）在我读取后被实时修改 → 据用户裁决把 Kingdom 让出。
- `DuelSettings.cs` 的 `git diff` 曾因 **stat 缓存陈旧**上报"干净"，而磁盘文件实际与 HEAD
  不一致，掩盖了真实的语法缺陷。

**教训**：在并发工作区中，`git status` / `git diff` 的"干净"结论不可单独作为文件完整性的证据；
涉及他人文件时应显式核对字节数或强制 `git update-index --refresh`。
