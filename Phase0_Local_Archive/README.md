# Phase 0 本地归档

本目录是政策系统 Phase 0 的本地资料归档，不是第二份重构大纲。完整重构当前暂停，不进入 Phase 1。

## 内容

- `policy_system_refactor_outline.md`：唯一的重构大纲与执行账本，已从 `docs/` 移入此处，没有保留副本。
- `baseline/run_baseline.ps1`：只读性能基线运行器；直接复用现有 embedding/reranker，模型不可用时失败，不提供无 ONNX 降级。
- `baseline/cases/`：政策历史检索、效果模块选择两套独立 JSONL 评测集。
- `reports/phase0_closeout_1.3_20260807.json`、`reports/phase0_closeout_1.4_20260807.json`：1.3/1.4 权威收尾报告。
- `reports/baseline_*.json`：收尾前的中间基线报告。

## 边界

- 本目录不参与编译、打包或游戏部署；当前内容已随检查点提交 `2b2a24d5` 纳入版本库。
- Phase 0 唯一生产代码残留现位于 `PolicySystem/Core/PolicySystemLog.cs`：放行 `pre-cleanup-policy-restore-complete` 聚合日志。它是运行时代码，未移入本目录。
- 当前 `PolicySystem/Core/CustomPolicyBehavior.Generation.cs` 中“读取最近两条玩家政策（含附庸国）”及 `PolicySystem/Effects/` 下的轻量效果模块是继续使用的生产功能，不属于 Phase 0 资料归档。
- 历史报告保留原始内容，其中 `evaluation_sets.path` 仍可能指向移动前位置；评测集身份应以报告记录与当前文件的 SHA-256 对照确认。
- `bin/Debug/policy_phase0_runtime/` 是运行器产生的可丢弃 runtime shadow，不属于归档；本次没有删除或改动它。

## 已有验证证据

- 最近一次正式 `build_single_module.ps1 -Stage`：Bannerlord 1.3、Bannerlord 1.4、Bootstrap 均为 0 警告、0 错误，Stage 成功且未部署游戏目录。
- 尚未完成的真实游戏旧档往返和实机观察项，仍按唯一大纲中的执行账本记录；归档动作不把这些项目标记为已验证。
