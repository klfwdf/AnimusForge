# Package C：政策正文与效果显示

阶段：待接力。先读[共同规则与文件边界](2026-10-10-localization-cde-plan.md)。
任务：完成政策系统剩余实际玩家可见中文的中英显示，保留原效果、事实与存档语义。
此包不包含历史C包的Memory/Knowledge/Weekly/Diplomacy。

## 独占范围

- `PolicySystem/` 中实际UI/结果生产者及必要显示接线；重点 Core/Management、
  Core/Effects、History、Npc与UI。允许新增小型Policy显示目录，不重写效果执行规则。
- `content/modules/PolicySystem/ModuleData/Languages/afpol_ui_strings.xml`
  与 `CNs/afpol_ui_strings-zh-CN.xml`；中英资源仅此一对。
- 政策专项测试，重点 `tests/bridges/Policy/PolicyEffectModule.ContractTests/`。
- 独立进度/交付：`docs/handoffs/2026-10-10-localization-package-c-policy-delivery.md`。
- 禁止写 `content/modules/PolicySystem/CustomPrompts/**`；那是E的英文资产范围。
  不改PolicyEffectPromptService等LLM规则内容或跨模块接口；发现此类中文登记主线。

## 优先级

1. `Core/CustomPolicyBehavior.Management.cs` 的 `BuildLocalPolicyEffectText`
   （旧:2278）：无模块效果、未知国家、发布地/本国提及目标及原因拼接。
   实际 `LocalPolicyHistoryRecordData.EffectText` 上屏，不能归类仅内部。
2. 追踪 `BuildPlayerVisibleEffectLinesForExternal` 所有消费者：国家政策、地方政策、
   附庸政策、NPC公告效果行。共享moduleId/数值/目标不能变；显示统一术语与正负号。
3. 各种状态、剩余时间、周期、续期/成本/独立度原因、终止原因中的中文尾巴；
   用户/模型原始文本不凭空重写，固定系统reason用投影，并明确未知旧内容回退。
4. 复查本包所有Inquiry/HUD/VM公开字段、按钮Hint及错误正文，不能只扫直接TextObject。

## 测试必须跟随真实行为

- 原 `Program.cs:4581/4599/4619` 对效果行要求中文且禁ASCII，需维护双语显示契约。
  保留数值、效果模块、目标与周期断言，新增英文和中文分支，不删功能断言。
- 原剩余时间两条断言锁中文、生产已英文fallback；先定位后按语义修复。
- 静态验证资源一致与UI/事实分流，实际本地化DLL验证 `ResourceName afpol_ui_strings`。
  需要编译的专项只prepare或标NOT-RUN，等主线统一构建时执行。
- 不刷新 `tests/j17_terminal_migration_review.json`、source_parity、全仓基线。
  相关binding变化给主线真实方法/行范围供统一复审。

退出门：本包所有已确认UI缺口落双语，数值/行为不变，动态原因完整，
本包待核查/内部中文清单明确，资源检查通过，focused提交与未验证项齐全。
