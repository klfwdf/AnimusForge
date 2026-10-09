# 本地化盘点工具

运行：

```powershell
python tools/localization/extract_localization_inventory.py `
  --output artifacts/localization/localization-inventory.json
```

工具只读扫描模组源码和内容资源，输出每条中文记录的文件、行号、模块 owner、分类、是否已有本地化 ID 和处理状态。扫描范围包括 `src`、`extensions`、`PolicySystem`、`Vengeance`、`WarStats`、`WorldEvents`、`content` 和顶层 `AnimusForge`。

扫描会排除 `artifacts`、`stage`、`deploy-backups`、`regen-check`、`bin`、`obj`、`NuGet`、原版源码目录和 Git 元数据，避免历史构建物重复污染清单。语言 XML 单独标记为 `localization_resource`，不计入待翻译 UI。

分类是保守的审查入口：`player_visible_ui`、`player_visible_or_review`、`prompt_or_internal`、`log_or_diagnostic`、`comment`。Prompt、日志和内部匹配文本必须在产品修改前单独确认，不能因为扫描结果自动进入 UI 翻译。

`tests/content/J15ContentContractTests/LocalizationResourceContractTests.py` 会从 Foundation 的中英文 `LanguageData` 和 `content-map.json` 解析全部注册资源，校验每一对 XML 的键集合、占位符和跨资源 ID 唯一性；新增语言文件必须同时注册、映射并提供英文 fallback。
