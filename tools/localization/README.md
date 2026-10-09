# 本地化盘点工具

运行：

```powershell
python tools/localization/extract_localization_inventory.py `
  --output artifacts/localization/localization-inventory.json
```

工具只读扫描模组源码和内容资源，输出每条中文记录的文件、行号、分类、是否已有本地化 ID 和处理状态。它不会改写源文件。

分类是保守的审查入口：`player_visible_ui`、`player_visible_or_review`、`prompt_or_internal`、`log_or_diagnostic`、`comment`。Prompt、日志和内部匹配文本必须在产品修改前单独确认，不能因为扫描结果自动进入 UI 翻译。
