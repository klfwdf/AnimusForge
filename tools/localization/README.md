# 本地化盘点工具

全模组源码中文硬编码（不排除 Prompt、日志、协议或已有显示投影的原值）：

```powershell
python -B tools/localization/inventory_runtime_literals.py `
  --output artifacts/localization/runtime-literals-mainline-current.json
```

同时输出逐条 CSV、模块/文件计数和来源哈希。转义的中文也先解码再计数；
注释、测试和历史构建物不属于产品运行时字面量。结果是词法出现位置，
不是全部待翻译 UI 的数量，也不是编译器或实机验收。

输入兼容词表的资源化与输出文案翻译分开验收。例如
`GeneratedRpVocabulary.json` 同时保留原中文和英文别名，其 1470 个词项、
顺序、类型和边界规则由 `EconomyVocabularyContractTests.py` 验证；它不是
删去中文识别能力或为中文日志加扫描豁免。当前纯英文包对兼容资源的语义
验证仍须收口，源文件残留下降不表示发行门禁已通过。

尚无英文对应的盘点：

```powershell
python tools/localization/inventory_untranslated.py `
  --output artifacts/localization/untranslated-only-current.json
```

只将已注册键、已追踪显示投影或有效平行英文 JSON 视为英文证据。
标准世界书逐文件匹配相对路径、人物 ID 前缀或知识规则 JSON Id；不能因为
`Calradia Chronicles` 目录存在就排除整个中文原版。已改成英文 Id 的规则且无
明确映射时仍标为待核查，不按名字猜测等价。报告含源文件 hash、扫描期间变化、
解析错误、UI 确认下限和消费者候选，内容资产与 UI 分开统计。
两个旧协议模板可在资产盘点保留，但不代表纯英文发行门禁通过。

运行：

```powershell
python tools/localization/extract_localization_inventory.py `
  --output artifacts/localization/localization-inventory.json
```

工具只读扫描模组源码和内容资源，输出每条中文记录的文件、行号、模块 owner、分类、是否已有本地化 ID 和处理状态。扫描范围包括 `src`、`extensions`、`PolicySystem`、`Vengeance`、`WarStats`、`WorldEvents`、`content` 和顶层 `AnimusForge`。

扫描会排除 `artifacts`、`stage`、`deploy-backups`、`regen-check`、`bin`、`obj`、`NuGet`、原版源码目录和 Git 元数据，避免历史构建物重复污染清单。语言 XML 单独标记为 `localization_resource`，不计入待翻译 UI。

分类是保守的审查入口：`player_visible_ui`、`player_visible_or_review`、`prompt_or_internal`、`log_or_diagnostic`、`comment`。Prompt、日志和内部匹配文本必须在产品修改前单独确认，不能因为扫描结果自动进入 UI 翻译。

`tests/content/J15ContentContractTests/LocalizationResourceContractTests.py` 会从 Foundation 的中英文 `LanguageData` 和 `content-map.json` 解析全部注册资源，校验每一对 XML 的键集合、占位符和跨资源 ID 唯一性；新增语言文件必须同时注册、映射并提供英文 fallback。
