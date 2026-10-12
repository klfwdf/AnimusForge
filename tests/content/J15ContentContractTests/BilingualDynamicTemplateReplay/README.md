# 双语动态模板独立重放

`python tests/content/J15ContentContractTests/BilingualDynamicTemplateReplay/run.py`

默认引用实际 `_deps_auto` 1.3.x 与 `.tmp/build_check/1.4` 1.4.x DLL。runner 读取 `TaleWorlds.Library.dll` 内嵌 BuildInfo 核实版本；可通过 `--reference-13` / `--reference-14` 指定其他真实引用。输出使用 `tests/output_isolation.py` 创建独立目录；不覆盖产品、资源、现有测试或游戏。

逐字提取当前及 `artifacts/localization-audit-repair-20261012/before` 中三方法完整正文：`BuildRuntimeAnnexationInstructionForExternal`、`Render`、`ApplyRuntimeTemplate`，正文不重写。显示名 fallback、ambient spoken-text extraction/title/status/context town、player-context 完整类与所需时段方法同样来自真实源。整类链接实际 PromptComposerTexts / DiplomacyMiscPromptTexts / ConversationRuntimeTexts / NpcDataPacket。使用真实 TaleWorlds.TextObject，注册真实 ambient XML 并切换 UI 语言；包选择在独立进程固定。

受控边界：王国资格状态输入与日志叶、Hero/Kingdom/Settlement/Village/Clan 对象图与已捕获名字字符串、CampaignTime 输入、资源根目录。真实 TextObject 的原始名字会先解释并清除未定义槽；专项独立记录该引擎行为，名字以字符串叶输入，验证 formatter 得到这些动态值后不会再次解释。未运行真实 Campaign 资格判定、场景采集、规则检索与动作执行；没有全工程构建或实机验收。RuleBehavior 模板重放直接读取实际 JSON 字符串，不能证明其检索/注入条件。

每个 1.3/1.4 × CN/EN 进程都要求旧 formatter 重现三个动态值污染，再要求新 formatter 保留 `{SPEAKER}` / `{TARGET_ID}` / `{player_gold}` / `{npcName}` / `$1` 字面值。原始 before Render 的 `new TextObject(spokenText).ToString()` 还会先删掉 named 槽；保留其完整未改方法重放，另生成仅更换这一 raw-localization 叶的 `BeforeAmbientRawBoundary`，逐字保留旧 Replace 链以独立重现顺序替换。普通输入对全部 ambient Text/变体比较 current 与该 old formatter 同 raw 模板输出，并记录原始 before 的预期删槽修复差异；不是把两者不同的原生阶段称为相同。其他两个方法直接对完整 before/current 比较。unknown、大小写、null/empty、未知 ID、fallback、locale 切换与时段/税情绪边界单独验证。断言通过不代表所有英文句义已人工核验。

证据：`inputs.json` 保存 before/after 全输入 SHA-256、实际方法行号/正文哈希、引用版本；`summary.json` 保存各进程计数、三项负控完整输入/前后输出及未覆盖范围。回滚只需移除此新测试目录和对应独立输出目录。

`--native-probe-only` 仅编译并运行真实两版引擎叶，不能算三个 formatter 专项通过。它验证本地 SetTextVariable 的占位符递归删除，以及公开 raw translation API 在 CN 返回原模板、English/unknown ID 返回 null 的行为。正式 runner 不设置任何全局文本变量。
