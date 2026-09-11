# Native 持久历史快照 — 本地验证记录（2026-09-11）

生产/测试提交：`8f1cd479`；检查点：`e1a09954`；真实历史对照：`659bb998`。本记录不是实机或发布验收。

## 变更与复现

Native 先在原主线程队列核对准入并捕获原身份、owner、generation、总览、场景/日期/设置、召回查询和记忆块/AFEF 投影；后台继续执行原召回/筛选/渲染，使用结果前再验同一准入。只替换这条 Native 持久历史入口，不另建算法。

测试把 `659bb998` 的真实 renderer/recall/select 方法编译在候选旁，先取得原输出，再在捕获后更改 owner 数据、AFEF 列表、场景/日期/设置等。比较文本、查询、API prompt、分支次数，并检测后台 owner/game 读取；不是拿候选自身当旧行为。

## 结果

| 检查 | 结果与证据层级 |
|---|---|
| 记忆快照 | 852 检查 / 120 组合；原代码 305 个断言失败，候选通过 |
| Native 接线 | 27 检查；原代码 12 个断言失败，候选通过 |
| 新变异 | 8 个记忆 + 2 个 Native 变异均编译后被运行时断言拒绝 |
| 既有边界 | memory UI 85、prepare 589、main-thread 132、admission 44、presentation 46、action 88、completion 184、pending 111；全部通过 |
| 旧变异复验 | main-thread 7、admission 7、prepare 5、memory UI 7；26 个反例均仍被拒绝 |
| 制作组薄桥 | 308 断言 / 13 方法 / 31 receivers / 3 个变异；严格逆变换未弱化 |
| 最终构建 | Debug / Release × 1.3 / 1.4 / Bootstrap，六项 Stage 成功；全部仅项目内输出 |
| 相关回归 | 最终构建后重跑 16 组，全部符合预期；包括管线、隔离、生产三渠道/经济 commit 回放、Scene、Courier、TTS fallback 等 |
| 新公共 API | 119 断言、256 次并发读、外部访问 internal 的预期 CS0122；实际四份实现 DLL 的 532 项元数据检查通过 |
| 存档契约 | 168 个 typed binding / 142 个 literal key；本轮未改 fixture 或存档身份 |
| 发布门禁 | 无真实证据时仍拒绝（预期 exit 2），不是可发布 PASS |

游戏数据、ONNX 向量、provider 返回和未改的严格 JSON 外部边界用 fixture；不能据此宣称真实网络、Campaign/Mission 或旧存档通过。初期 fixture 缺少 FreezeWatchdog stub 的编译失败已在测试夹具修正，原红日志保留；最终原版反例和变异失败均是运行时断言，不是编译失败。数据模型夹具的 CS0649 警告未屏蔽。

## 复跑入口

在 `G:\AFMOD\AF-REFACTOR` 使用 `G:\Python310\python.exe`：

```powershell
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --native
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --original
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --native --original
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --mutate reuse-afef-list
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --native --mutate drop-accept-guard
G:\Python310\python.exe tools/TeamModulePortParityTests/run.py
G:\Python310\python.exe tools/ModuleFrameworkApiTests/run.py --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts
```

原版/变异命令应返回非零且出现 runtime FAIL；其余应成功。全部变异列表见新测试 README，准确参数与完整日志在 `.tmp/native-history-snapshot-20260911/`；这些原始日志只留本地，可提交摘要与 SHA256 在同名审计 JSON。构建参数沿用 `docs/phase8/framework-v1-execution-20260911.md`，Debug / Release 分别运行原 `build_single_module.ps1 -Stage`，没有部署开关。

最终源码增加小历史 fast path 后，六项构建与 16 组相关回归均已重新执行。`before-small-history-fast-path-*` 是旧记录，不能替代最终产物。JSON 的最终六个 DLL SHA 与各自 `.build.json` 全部相符；回归门禁日志当时 HEAD 为检查点，随后以相同源码提交 `8f1cd479`。

## 清理与保留

删除旧 Native `BuildNativeConversationPersistedHistoryContextForPrompt` 及原 Task.Run 内直接查身份/owner 的接线；生产 C# 中不再有旧 helper 引用。新代码/测试未发现冲突标记或废弃 TODO，自己的 diff 检查通过。原公开历史接口、共享 renderer 和 wilderness helper 仍被 Scene/Courier 实际调用，必须保留；没有以删兼容入口冒充重构完成。

## 暂停与未验证事项

按用户要求，验证及源码提交完成后，`af-7-8` 已通过应用工具设为 PAUSED，回读配置确认；名称、提示词、周期和目标任务均保留。只写交接，不再开始下一项。

这只是 Native 的召回用途投影，不是全量存档对象，也不是全局记忆原子事务。其他后台压缩/维护 writer 并发捕获、主线程实际耗时、persona/规则/周报、TTS 直接回调、Courier prepare、Scene/Courier 快照迁移和真实游戏/旧存档仍未完成。过期结果丢弃不等于真正取消已开始网络。Api.V1 仍只读；未推送、部署或访问真实存档，阶段 8 未 DONE。两份 2026-09-06 用户草稿未修改、未提交。
