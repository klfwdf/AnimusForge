# 第二轮 C：政策、王国、外交、周报、记忆知识

状态：READY，待外部 Agent 开工。先读[审查与共同约束](2026-10-10-localization-wave2-review.md)。旧 Half B/C 的路径分配已被本轮替代。

## 所有权

负责 `PolicySystem/**`、`src/modules/AF.Module.{Kingdom,Diplomacy,Weekly,Memory,Knowledge}/**` 及对应 Adapter/Kingdom UI、Memory/Weekly/Knowledge 编辑器。首轮快照 **404 候选 / 80 文件**，见[文件表](localization-wave2-c-files.md)。文件表漏掉的实际消费者仍归本包。

成对资源：`afpol_ui_strings`、`afdip_ui_strings`、`afkingdom_ui_strings`、`afmemory_ui_strings`、`afknowledge_strings`。Weekly 新增键请新建 `afweekly_ui_strings.xml` / `CNs/afweekly_ui_strings-zh-CN.xml`，提供映射/注册建议，由主线集成；已有 AF_WEEKLY 键可继续引用。不要直接改 A 的 `af_ui_strings`。

## 已确认必须收尾的路径

1. Policy：`CustomPolicyBehavior.Management.cs` 的 `BuildPolicyRecordCostText`、`BuildLocalPolicyHistoryData`；`NpcRulerPolicyBehavior.Generation.cs` 的 `ShowSuggestedPolicyGenerationRetry` / 重试拒绝提示。仅固定外壳，政策模型输出与效果原因原值保留。
2. Knowledge：`KnowledgeLibraryBehavior.cs:8482/8513` 两处 RAG 失败弹窗；编辑器规则类型显示标签也审查。RAG 文本、关键词、知识正文、嵌入输入不翻译。
3. CivilWar：`ToPanelFaction` 的 DemandTitle/Name、`GetFoundingOptions` 菜单文本。接入 `CivilWarUiText.DemandText` 等显示投影；不要改 `CivilWarCatalog.FactionNameFormat` 或 `FormatDemand` 的内部事实用途。
4. CivilWar：`KingdomFactionTab.Actions.cs:289`、`CivilWarCampaignBehavior.cs:83` 的 `result.Message` 通知。做单独 UI 投影，原 `Message` 仍保留给存档/AFEF/周报。旧回执/未知消息也需可靠 fallback；不能只用成功状态推测发生了什么。
5. `KingdomFactionTab.cs:431`：不要依赖中文 StageText 判断专用标题，保留原资格/状态逻辑。
6. Weekly：`WeeklyReportEditorController.cs:553`、`WeeklyReportRuntimeOwner.cs:146/757/916/923` 的批次/分组标签。新建 UI 专用标签消费者，原模型素材标签保持语义。
7. Diplomacy/Vassalage、Memory 编辑器：追踪其余实际 UI 残余/动态 fallback；Memory 注册和17对换行主线已修，不重复覆盖。

## 禁止边界

- 不英化政治 Message、ctx.Notes、PublishPoliticalResult、CivilWarEffects.reason 等共享原始事实。
- 不改 Prompt/周报正文/REPORT_BLOCK/TAGS/AFEF、存档、语言探测。
- 不写 B 领域、Illustrator/MCM/Terminal、共享注册/map/测试/主台账；如需新增 DTO 字段，先确认不属于存档身份，保持业务契约。不要用字符串替换整段存档历史冒充可靠翻译。
- `UI/Editors/KingdomRebellionEditorController.cs` 本轮保留给 A，和 `UI/Kingdom/**` 的 C 范围不同；以文件表及显式保留项为准。

## 交付

按模块 focused commit；资源契约、ID/变量检查、自己范围残余审查与 `git diff --check`。跳过构建，双版本统一交主线。报告写 `docs/handoffs/2026-10-10-localization-wave2-c-delivery.md`，给具体键集合和真实保留证据，未知/待审条目不可标完成。游戏/旧档标 NOT-RUN。
