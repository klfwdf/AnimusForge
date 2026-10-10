# LLM failure display localization

阶段：源码与静态集成完成；全英文发行仍 ACTIVE / NOT RELEASE READY。

产品切片 `dfae5984d`，工作区 `F:/AnimusForge-main`、`main`。回滚仅 focused inverse。
11 个失败报告入口新增显示投影，17 对键进入已注册的 `af_ui_strings`：

| 源码 | 符号与覆盖范围 |
| --- | --- |
| `src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs:120-166` | `ShowLocalizedFailurePopup`、模板解析、完整回复/响应显示。只在原有 `PostToMainThread` 回调内解析。 |
| `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs:308,710` | 信使回信/来信前处理失败标题；原异常完整保留。 |
| `src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeMainReply.cs:55-57` | `ReportProviderFailure` 自由对话失败标题；原 provider output 完整保留。 |
| `src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs:957-979` | 无名 NPC 人设 gateway/空回复/原错误/解析失败；三个已知固定原因显示独立翻译。 |
| `src/AF.GameAdapter.Bannerlord/Weekly/WeekZeroOpeningSummaryGenerationController.cs:329-331` | 第 0 周短周报失败标题与备用原因；各次失败原详情仍完整保留，中文尝试序号尚未投影。 |
| `src/modules/AF.Module.Llm/Application/ConfiguredChatApplicationAdapter.cs:549-567` | 辅助模型三种失败标题；固定缺 host/备用原因与完整回复/响应标题投影。异常正文通过变量插入，不当作模板解析。 |

原 `BuildFailureDetail`、公开 `ShowFailurePopup`、重试判定、dispatcher、取消/代际与返回分支未改。
内部显示重载将原诊断继续交给 `NonBlockingErrorReport` 的日志、AI 分析、去重与密钥脱敏。
模型正文和原始响应完整追加，仅省略完全相同的重复响应，保留既有 Trim 规范。
新入口不添加 Tick、扫描、线程、反射或语言探测；只在失败报告发生时解析。

实际验证：

- `LlmFailureDisplayLocalizationContractTests.py`：PASS。将 11 处新调用逆向还原后，五个消费者文件除调用表达式外与 `3963e0ef5` 一致；诊断字符串、Prompt、parser 和业务分支保持。原错误方法逐方法一致；命名/嵌套参数盘点及 Courier 当前 fixture 主线程报告边界存在。
- `CourierPromptPreparationTests/verify_failure_ui_static.py`：PASS；原信使生命周期/事实/历史逆向证据保持。
- `ErrorUiLocalizationContractTests.py`：PASS。
- `LocalizationResourceContractTests.py`：PASS，26 对。
- 既有 `.tmp/build_check/1.3` / `1.4` 本地化引用 DLL：各 12,628 次 UI 资源解析 PASS，覆盖英文、简体中文、其他语言回退英文与切回英文。这不证明当前产品 C# 已编译。
- 工作树 `git diff --check`：PASS。

盘点识别新增入口，不能因方法改名把原中文诊断藏掉：最新工作树扫描 HEAD `3963e0ef5` 包含本切片候选，确认 UI 7 位置（全部 Policy），消费者待审 **1,220**，扫描期间无变化。
较旧扫描多 6 个待审，是重新登记明确保留的原诊断，而非新增中文。19→7 包含未编译候选与分类调整，不能当全英文发行百分比。
原始 provider/旧错误内容、周报每次失败模板、已保存历史和所有运行时中文外置尚未完成。
世界书识别规则仍未完整识别 B 的英文目录，不用其 2,641 匹配缺项否定 B 交付。

NOT-RUN：当前产品/fixture C# 编译执行、双 API/Bootstrap 构建、游戏 UI、旧档、正式 CN/EN ZIP 和推送部署。
其他作者 Policy/Economy、HANDOFF 在途改动、两个 NuGet 目录和英文资产全部保留。

下一主线切片意图：已追踪 `PlayerNotorietyBehavior.BuildPlayerNotorietyDisplayText` 唯一消费者为玩家百科，固定中文标签/11 个评级及日期空值 fallback 尚未翻译；只改显示、原历史正文和 Prompt/存档不变。
百科当前通过中文 `PlayerNotorietyMarker` 去重，需按页缓存旧追加块来兼容语言切换；不添加 Tick 扫描或重复反射。
涉及 Social host、Hero 百科显示与 Social 资源对，C 政策/E 经济路径不写。
