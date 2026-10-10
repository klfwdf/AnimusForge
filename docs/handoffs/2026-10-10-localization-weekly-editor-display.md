# Weekly editor display localization slice

阶段：验证完成，未编译、未部署、未打包。

本切片把周报编辑器的固定显示文本接入 `TextObject` 资源：素材类型、篇幅档位、编辑器字段、Prompt 预览标题、归档空值和日期未知均有英文 fallback 与简体中文资源。后台运行时仍使用 `WeeklyDiagnosticPreviewProjection` 的原始诊断格式；两个诊断方法与 `76e592ce0` 基线逐字一致，Prompt 正文、保存的预览和事实值不经过本地化显示投影。

周报编辑器调用 `BuildWeeklyReportPromptPreviewTextForDisplay` / `BuildWeeklyBatchPromptPreviewTextForDisplay`，运行时调用原诊断方法。诊断字段使用原始中文冒号格式，避免 NPC 编辑器共享字段格式化函数改变保存文本。周报近况标题继续保留存档中的日期区间；`CreatedDate` 属性按既有单日显示格式解析，区间英文重建仍是后续缺口。

验证：

- `WeeklyProjectionLocalizationContractTests.py`：34 个投影键、fallback 与资源一致、UI/runtime 分离、诊断基线一致。
- `LocalizationResourceContractTests.py`：26 对资源通过。
- 1.3 与 1.4 本地化 DLL：每个 `af_ui_strings` 资源 12,116 次英文、简中、其他语言回退及切回英文解析通过。
- `ErrorUiLocalizationContractTests.py`、`NpcActionEditorLocalizationContractTests.py`、`UntranslatedInventoryTests.py`：通过。
- Editor、Bulletin、Memory 13/14 runner 的 `--prepare-only`：全部成功；未调用 dotnet。
- 盘点快照：确认 UI 143 处 / 31 文件 / 115 个去重文本；另有 1,279 个消费者候选待追踪。该数字是保守 UI 下限，不是全仓剩余量。

未验证：C# 编译、编译型 fixture、实机、旧档、正式 CN/EN ZIP、Stage、部署、N 网发布。回滚使用本切片 focused inverse；不回滚 B 世界书或 C/D/E 并发修改。
