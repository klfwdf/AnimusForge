# 双语动态模板回放

从当前源码与已留存基线逐字提取 `BuildRuntimeAnnexationInstructionForExternal`、`ApplyRuntimeTemplate` 及显示名辅助方法，链接真实 PromptComposerTexts、DiplomacyMiscPromptTexts、ConversationRuntimeTexts 和 NpcDataPacket。

在 1.3/1.4 × CN/EN 中验证普通值、带花括号的动态名称、大小写、空值及未知模板变量；保留旧顺序替换的负控和真实资源 reader。环境闲聊及其原生探针已随功能退役，不再复制预设资源或提供对应参数。

运行：`python tests/content/J15ContentContractTests/BilingualDynamicTemplateReplay/run.py`。结果仅代表受测模板与离线资源链路，不代表实机或全部英文语义验收。
