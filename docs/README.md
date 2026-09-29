# AnimusForge 文档索引

当前事实只在三处维护，其他文档不写“当前状态”：

- [主台账](animusforge-refactoring-and-repository-reorganization-plan.md)：进度、证据、登记表（R01–R09）与执行顺序。
- [`HANDOFF.md`](../HANDOFF.md)：当前段与下一步。
- [`plans/`](plans/)：每阶段实施计划（J07–J16）。

## 案例（被 `CLAUDE.md` / `AGENTS.md` 引用，路径不动）

| 案例 | 用途 |
| --- | --- |
| [单模块双实现输出](bannerlord_dual_module_output.md) | 一键编译/覆盖/打包、1.3/1.4 双实现、`SubModule.xml` |
| [1.3 → 1.4.5 兼容差异](bannerlord_1_3_to_1_4_5_compatibility_diff.md) | Bannerlord API、Harmony、Gauntlet、双构建 |
| [百科按钮注入](encyclopedia_button_injection_case.md) | 百科页按钮、弹窗与快捷键拦截 |
| [指令标签输出](directive_tag_output_case.md) | 前处理/主链路/后处理标签与执行入口 |
| [场景 Agent 命令移动](scene_agent_command_movement_case.md) | 带路、传唤、跟随 |
| [军团成员自定义会面目标](army_member_custom_meeting_target_case.md) | 军团成员会面目标解析 |
| [场景伤害上下文防误触](scene_damage_context_guard_case.md) | 伤害/死亡/敌对关系 allowlist |
| [三渠道对齐](free_conversation_scene_shout_alignment.md) | 信使、自由对话、场景喊话 |

其他领域案例：[信使信件物品持久化](courier_letter_inventory_item_persistence_case.md)、[周报事件素材](event_weekly_material_case.md)、[存档字符串溢出坏档](save_string_overflow_bad_save_case.md)、[野外非英雄部队记忆](wilderness_nonhero_party_memory_case.md)、[自定义政策生命周期](custom_policy_lifecycle_v2.md)、[政策效果源模块契约](policy_effect_source_module_contract.md)、[外交政策历史增长修复](diplomacy_policy_history_growth_fix.md)、[自定义 UI 窗口](custom_ui_window_methods.md)、[场景动作集成](scene_actions_integration.md)、[Anthropic API 兼容](anthropic_api_compatibility.md)、[PlayerExports 编辑器设计](player_exports_editor_design.md)。

## 架构

- [`architecture/`](architecture/)：框架概览、内部模块/公共 API 指南、主线程与 Native/Memory 边界、团队模块接缝。
- [范围图与证据 E01+](architecture/af-framework-code-scope.md)；代码地图 `architecture/af-framework-code-map.json` 由 `.agents/skills/af-core-framework/scripts/verify_code_map.py` 校验。
- 仓库边界：[boundary audit](animusforge-repository-boundary-audit.md)、[decision table](animusforge-repository-boundary-decision-table.md)、[owner matrix](animusforge-owner-matrix.md)、[refactor map](animusforge-refactor-map.md)、[request/commit receipts](animusforge-request-commit-receipts.md)。

## GCCZ 与桥接

- [`gccz/`](gccz/)：攻城处置的审计、桥接、交接、迁移与计划。
- [`bridge/`](bridge/)：AF ↔ GCCZ 融合适配与城堡/城镇桥接记录。
- [`testing/`](testing/)：实机测试序列。

## 历史与证据（只读，不代表当前状态）

- [`history/phase2-7/`](history/phase2-7/)：阶段 2–7 设计与边界文档。
- [`phase8/`](phase8/)：阶段 8 readiness 目录（`full-domain-readiness-catalog.json` 等仍被 `tools/PhaseEightReadiness` 与 bridge 契约测试读取，路径不动）。
- [`handoffs/`](handoffs/)：按日期的交接记录；[`audits/`](audits/)：审计与验证证据（含 J16 基线）。
- 早期总计划：[完整重构计划 2026-08-31](animusforge-complete-refactor-program-20260831.md)、[基线 2026-08-30](animusforge-baseline-2026-08-30.md)。

## 参考资料

[原版 Issue 审计](vanilla_issue_audit.md)、[原版文本导出](vanilla_text_export/)、[百科参考](bannerlord_encyclopedia/)、[部队经验效果研究](hero_party_troop_xp_effect_research.md)、[战况终端交接](war_stats_terminal_integration_handover.md)、[世界外交交接](world_diplomacy_handoff_20260810.md)。

链接由 `tests/docs/LinkCheck/check_links.py` 校验（0 断链为门槛）。
