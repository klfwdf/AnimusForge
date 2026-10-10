# 第二轮 B：对话、经济、社交、世界地图

状态：READY，待外部 Agent 开工。先读[审查与共同约束](2026-10-10-localization-wave2-review.md)。旧 Half B/C 的路径分配已被本轮替代。

## 所有权

负责 `src/modules/AF.Module.{Conversation,Economy,Social,WorldMap,Issue}/**`、对应 `content` UI（排除 Prompt/资产正文）、`extensions/AnimusForge.DialogueUI/**` 以及对应 Economy/Social/WorldMap 适配器。首轮快照 **435 候选 / 26 文件**，见[文件表](localization-wave2-b-files.md)。这不是漏译总数。

可写成对资源：`afeconomy_strings`、`afsocial_ui_strings`、`afworldmap_strings`、`afconv_ui_strings`、`afconv_ambient_strings`、`afd_ui_strings`。如果发现本包 ID 放在 A 共享 `af_ui_strings`，新增键写本包专属资源，不直接改共享 XML。

## 顺序

1. 追踪 Conversation 输入弹窗、失败通知、固定 fallback 台词、DialogueUI 状态与菜单的实际显示消费者；检查不仅含 Chinese 的文件，也检查显示入口调用的共享 helper。
2. RewardSystemBehavior 与 DebtLedger：审查确属玩家可见的固定结果和错误外壳。现存 179 UI 候选大部分是 Prompt/物品匹配，不批量翻译 `reason` 或 AFEF。
3. WorldMap Admission/DelayedRequests 等原始失败原因：查是否进入 UI 或 LLM/事实；仅显示端投影，不将运行结果本体换语言。
4. Social/Recruitment：现有 SocialUiLocalization 映射是否涵盖实际结果；检查 fallback 仍泄漏中文的路径。
5. Issue、TownAmbient：对白 Text 已在先前提交英化；Memory/ReplyHints 和规则匹配仍须保留。不能为扫描归零而改这些内容。

## 交付条件

- 每个确认 UI 字面量转换为原生 ID + 英文 fallback，中文资源键/占位符对齐。
- 保留项报告必须给调用者/用途证据，不能仅以文件名含 Prompt/Memory 为由豁免。
- 运行资源契约、自己范围的静态 ID/中文扫描与 `git diff --check`。不要构建或修改测试 oracle。
- 交付自己的 focused commits 和 `docs/handoffs/2026-10-10-localization-wave2-b-delivery.md`。新增资源注册交主线，未注册资源需自行额外校验，不能用 registered-pairs PASS 冒充覆盖。
- 不写 Policy/Kingdom/Diplomacy/Weekly/Memory/Knowledge、Illustrator、MCM、Terminal、共享注册/map/测试/台账。

快速开始：确认工作区后运行 `python tools/localization/extract_localization_inventory.py --output artifacts/localization/wave2-b-current.json`，结合逐条快照筛选本包。纯字符串搜索不能替代实际 UI 调用链审查。
