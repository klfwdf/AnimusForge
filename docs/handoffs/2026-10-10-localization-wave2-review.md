# 第二轮本地化：B/C 交付审查与三方接力

状态：VERIFY。工作区 `F:/AnimusForge-main`，分支 `main`。本文件替代旧 Half B/C 文档中的执行范围；旧交付记录保留其历史结论。

## 审查结论

交付提交均存在，但不能将“资源成对/编译成功”当作玩家可见 UI 全部完成。两份旧报告共用了若干相同提交，不能相加计算贡献或完成条数。

| 级别 | 发现 | 处理 / 归属 |
| --- | --- | --- |
| P1 | Memory 的 242 键资源未注册到两份 LanguageData，中文不会加载 | 主线修复 `d0144359e` |
| P1 | afdip 中英 XML 无声明，真实原生加载器在 LoadLanguage 的 ChildNodes[1] 处异常 | 主线修复 `a329a700b`，全资源运行时复验通过 |
| P2 | Memory 17 对文本把换行写成字面反斜线 n | 主线修复 `e96f9a133` |
| P2 | CivilWar 面板诉求、建派选项和完整名称继续使用中文原始规则文案 | 新 C：显示投影；保留 Catalog 和事实原文 |
| P2 | CivilWar 两个通知入口直接显示共享的中文 result.Message | 新 C：显示投影；保留存档/AFEF/周报的原始 Message |
| P2 | Weekly 编辑器/失败提示使用中文批次、分组标签 | 新 C：独立显示标签；保留模型素材标签 |
| P2 | Policy 历史/成本、NPC 重试弹窗和 Knowledge RAG 失败弹窗仍有中文 UI | 新 C |
| P2 | Illustrator 下拉选项、编辑器与模型拉取通知仍有中文 UI | 新 A；编辑器/通知第一切片已开始 |
| P3 | KingdomFactionTab:431 比较中文 StageText，英文下专用标题分支失效 | 新 C：不依赖显示译文判断状态 |

证据入口（源基线 `c0d452361`）：

- `KingdomCivilWarOwner.cs:1142-1148` 的 ToPanelFaction；`KingdomCivilWarOwner.Actions.cs:52-58` 的 GetFoundingOptions；`CivilWarUiText.cs:36-50` 的 DemandText 无调用者。
- `KingdomFactionTab.Actions.cs:289` 和 `CivilWarCampaignBehavior.cs:83` 直接显示 result.Message。
- `WeeklyReportEditorController.cs:553` 消费 `WeeklyReportRuntimeOwner.cs:146` 的批次标签。
- `CustomPolicyBehavior.Management.cs:1294` / `2090` 的成本/历史显示；`NpcRulerPolicyBehavior.Generation.cs:1980-2036` 的重试弹窗。
- `KnowledgeLibraryBehavior.cs:8482` / `8513` 的 RAG 失败弹窗。
- 原生证据：`原版游戏本体代码1.4.5/TaleWorlds.Localization/TaleWorlds/Localization/LocalizedTextManager.cs:231-246`。

B 报告称 Policy 新增 349 对，实际提交 `07fcfc634` 前 165 键、后 341 键，新增 **176 对**。349 是 XML diff 新增行数，不能作为词条数。

## 还剩多少

分工前快照 `c0d452361`：全量中文记录 93,668，UI 候选 1,375 条 / 193 文件。主线完成上述修复及首个 UI 切片后，最新快照绑定 `aafcbe336`：全量中文记录 **93,658**；排除语言资源、中文备份、测试等后，保守 UI 候选 **1,352 条 / 192 文件**。

这不是确认漏译条数，也不是完整 UI 上限。扫描以单行关键词判断，会把 XML 注释、Prompt、内部值算入，也会漏掉没有 UI 关键词的字符串。当前能确认上述残余路径，完整“真实待翻译条数”仍需逐项追踪消费者。不得据候选下降推算完成百分比。

本轮快照中的每个文件只归一个工作包，工具验证无重复、无遗漏：

| 包 | 执行人 | 候选 / 文件 | 范围 |
| --- | --- | ---: | --- |
| A | 主会话 | 513 / 86 | 设置、终端、公共 UI、Illustrator/Coup/GCCZ 等扩展、注册和验证集成 |
| B | 外部 Agent | 435 / 26 | Conversation/DialogueUI、Economy、Social、WorldMap、Issue |
| C | 外部 Agent | 404 / 80 | PolicySystem、Kingdom、Diplomacy、Weekly、Memory、Knowledge 及对应适配 UI |

按职责与文件边界分三份，并非均分候选条数。A 含较多注释/诊断；C 包含较复杂的事实与显示分离。任务包括消费者审查，所以文件表并非只允许修改表内文件。

- [B 接力](2026-10-10-localization-wave2-b-handoff.md)
- [C 接力](2026-10-10-localization-wave2-c-handoff.md)
- [A 文件表](localization-wave2-a-files.md)、[B 文件表](localization-wave2-b-files.md)、[C 文件表](localization-wave2-c-files.md)
- 当前逐条快照：忽略的 `artifacts/localization/localization-wave2-current.json`；分工前快照保留在 `localization-wave2-candidates.json`。完整扫描放在 `artifacts/localization/localization-inventory.json`。

## 三方共同约束

1. 开始先查 Git 根、分支、HEAD、未提交差异并读仓库维护技能。保留其他作者、NuGet 和平行英文资产；不 reset/stash/覆盖。
2. 仅翻译玩家可见固定外壳。英文 fallback + 中文 XML；保留变量、换行、数值格式、按钮行为。解析发生在打开/状态改变时，不增加 Tick 扫描或反射。
3. 不改 Prompt、世界书、模型正文、AFEF、内部匹配、协议、存档键/字段、加载语言探测。尤其保留 `[ACTION:SCENE_ACT:动作程序]` 和 `[RELAY:接力编号]`。
4. `CivilWarCatalog`、政治结果 Message 和 Weekly 模型标签保留原值，不等于允许在英文 UI 直接泄漏；应在显示边界作独立投影。遇到双用途内容先追踪调用者。
5. B/C 不写 A 的共享文件：`af_ui_strings*.xml`、两份 foundation LanguageData、`content/content-map.json`、共享测试/扫描工具、主台账/HANDOFF。需要注册的新资源先交付成对文件及映射建议，由 A 集成。
6. 本轮不构建、不 Stage、不部署、不打包、不 push。等三方收敛后主线统一验证双版本；不得改现有构建脚本。
7. 每个模块 focused commit，显式 add 自己的文件。交付报告列出新增键集合、已确认 UI 替换数、保留项及其消费者证据、提交 SHA、验证与 NOT-RUN。

## 验证与回滚

已执行：24 对资源契约 PASS；扩展真实 1.3、1.4 引用 DLL 检查各 24 对 / **37,748 次**解析 PASS，覆盖英文、中文、无翻译 French 回退和切回英文（包括本轮 Illustrator 资源）。仅资源加载/变量解析，不代表游戏 UI 或最新产品构建。

主线 A 首个 UI 切片 `aafcbe336` 已新增 30 对 Illustrator 编辑器/模型拉取文案；后台只返回错误状态/数据，主线程事件完成时解析，无 Tick 新工作。Dropdown 中文哨兵和默认 Prompt 仍保留，后续只处理其显示边界。

新测试会拒绝 mapped-only 未注册资源、缺失声明和 Memory 字面换行。资源代码 fallback 的 TTS JSON 花括号也在 `a329a700b` 修复。

未执行：本轮双 API/Bootstrap 构建、Stage 装配、全部 UI 契约、游戏/旧档/完整战役/输入焦点/英文截断，均 NOT-RUN。完整 J15 仍有 GCCZ Prompt 未审漂移的历史失败，未刷新 hash，不推断其后的全部门禁通过。

回滚：对本轮 focused commit 作 `git revert <SHA>`；不重写历史，不回滚其他作者。
