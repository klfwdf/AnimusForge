# B1 原文指纹成本：本轮实际改进与未关闭的门槛

> 根整合状态（2026-09-16）：生产源码 `6e419f6d` 已本地提交，最终六Stage与4DLL1056元数据通过；整体收尾仍ACTIVE。当前边界以[总交接](../../docs/handoffs/2026-09-16-parallel-closeout-handoff.md)为准，以下包内记录保留原验证上下文。


## 本轮已完成

只修改 `Refactor/Runtime/MemorySourceFingerprintWriter.cs:36–81`：字符串按现有 4096 字节缓冲的剩余容量成块编码，消除每个字节重复的方法调用、状态与满缓冲检查。奇数边界先处理跨块字符；统一 `FlushBuffer`，没有第二份 SHA 状态。

没有修改源字段映射、JSON/存档格式、Prompt、排序、RPM、队列或接受条件。原始 UTF-16 code unit（包括孤立 surrogate）和前缀 framing 必须逐字节相同。不是 `Encoding.Unicode` 转码，不会把非法 surrogate 替换成 `U+FFFD`。

- 对照 writer：`155f1b7a`，规范化源码 SHA256 见 `writer-review.json`。
- 当前候选尚未独立提交；以 `benchmark-summary.json` 的 writer SHA256 为准，提交号由根任务统一补充。
- 生产调用者没有另接一条路径：仍由 `MyBehavior.MemorySourceFingerprint.cs:12–233` 的既有唯一 mapper 消费。

## 已执行的证据

| 检查 | 结果 |
| --- | --- |
| 实际 10 类私有 DTO、CopyForSummary、完整 source mapper/writer | 原/新各 65,808 检查，0 失败 |
| 单独每个 UTF-16 code unit、奇偶 4096 边界、孤立 surrogate、128 随机输入 | 原/新全部通过 |
| 13 组 Daily / Major / Overview 完整源 digest | 原/新 SHA256 完全一致 |
| 真正生效的三个故障：去掉字符串长度、高字节清零、遗漏满缓冲 hash | 均编译通过并行为失败（exit 1），不是工具/编译红 |
| 原 `run_fingerprint.py` | 9 向量 + 5 生命周期/列表守卫通过 |
| 三类源分片后修改前面元素字段；两类嵌套列表修改 | List 根结构探针未发现，而生产完整 digest 检出；保留最终完整绑定 |

第二轮同机微基准的 `raw-digest` 中位数（实际生产方法，合成数据，不是游戏帧时间）：

| 合成输入 | 原 writer | 新 writer | 每调用分配变化 |
| --- | ---: | ---: | ---: |
| Daily 1024 条 × 256 字符 | 2.841 ms | 1.678 ms | 0 |
| Major 4096 条 × 256 字符 | 12.988 ms | 7.809 ms | 0 |
| Overview 4096 块 × 256 字符 | 18.360 ms | 11.019 ms | 0 |
| Daily 单条 2,000,000 字符 | 17.470 ms | 13.135 ms | 0 |

全部 13 组观察下降 11.7%–44.4%；CPU 调度/JIT/并行构建会影响绝对时间，不把该范围作性能承诺。每组 3 次预热、7 次测量，记录中位数和最大值。`dto-copy` 行只测真实 detached copy；`capture-copy-binding-subset` 行是两次真实 raw digest + 一次真实 copy，**不是完整 Capture**，不包含 sanitize、Prompt 和游戏对象查找。

可提交摘要：`benchmark-summary.json`；原始 build/run/manifest 冻结在 `.tmp/memory-budget-20260916/`，普通可重建测试产物在本目录 `.generated/`，均不提交。

## 未完成：B1 不得标 DONE

- `MyBehavior.MemorySummaryInput.cs:252–323` 的首次 raw digest、完整 clone、sanitize、Prompt 构造仍在一个主线程回调；`339–357` 的最终完整源绑定仍是 O(records + chars)。本轮只是降低常数，不提供 records/chars 帧预算。
- 单条 2M 字符仍需约 13 ms 指纹时间；“每帧两个 callback”不能限制这个成本。
- 当前 DTO 标量字段可直接改写，多层容器仍是 `List<T>`。跨 tick 的 List version/Count/引用检查不会发现已扫元素的字段修改，也不会发现未持有其探针的子列表变化。分片复查末尾再只检查 owner/generation，仍会错过已查片的后续变化。
- 不删除完整 raw binding，不靠只在 `Save*` 增 revision 或只比较 root List 版本伪装成严格一致性。

### 后续真正有界的接线前提（设计，未实施）

先收敛一个实体/摘要种类的所有 mutation authority；只有输入发布的 immutable root + 对应 source epoch 成为唯一权威后，才能 O(1) 捕获/接受并将记录/字符计算分片或转后台。runtime stamp 不进入存档格式；reset/game retirement 仍由现有 owner/run/generation 决定。所有源变更必须在可见写入前使旧 epoch 失效，即使随后写入抛异常，也不能继续接受旧输入。

| 必须纳入的生产边界 | 当前代码定位/责任 |
| --- | --- |
| 日记新增、AFEF、pending weekly 材料附加 | `MyBehavior.cs` 的 `AppendDailyMemoryLineById`、`AttachPendingWeeklyMemoryMaterialTriggers`、`AddWeeklyMemoryMaterialTriggerToDraft`、`MarkWeeklyMemoryMaterialTriggerInternal` |
| 日记/压缩块编辑与保存 | `SaveDailyMemoryDraftsById:26903`、`SaveCompressedMemoryBlocksById:26953`、`ApplyDevDailyMemoryDraftMutation:50205`、`ApplyDevCompressedMemoryBlockMutation:51115`；不能假设编辑 delegate 没在 Save 前改 live 对象 |
| 重大履历写入、游标和失败状态 | `RecordNpcActionInternal:15518`、`ApplyMajorActionSummarySuccess`、`MarkMajorActionSummaryFailure`、`NormalizeNpcActionSequences` |
| 三类成功/失败消费、总览 dirty | `ApplyMemorySummarySuccess`、`MarkMemorySummaryFailure`、`ApplyMemoryOverviewSuccess:5315`、`MarkMemoryOverviewFailure:5351`、`MarkMemoryOverviewDirty` |
| 会改变 live DTO 的 normalize/sanitize 和 sealing | `SanitizeNpcActionEntries:15362`、`SanitizeDailyMemoryDraftEntry:26455`、`SanitizeCompressedMemoryBlocks:26501`；`MyBehavior.MemorySealing.cs` 的原地清理/归一化发布 |
| 来源合并、ID迁移、删除、清空和读档初始化 | `MergeMemoryEntityDataById:25558`、`MergeMemoryOverviewStateById:25839`、`MergeMajorActionSummaryStateById:25869`、`SyncData` 及 purge/reset 入口 |
| 恢复重放/周报事实真正确认 | `MyBehavior.MemoryRecovery.cs:553` 的 `PublishDailyInteractionMemoryComponent`、`MyBehavior.WeeklyActionOutcomeReceipts.cs:304` 的 `TryPublishWeeklyActionOutcome` |
| Job 自身重试/错误/成员资格与 context | 三类 queue job 字段和 root 替换、`CaptureMemorySummaryContextFingerprint` 的实际设置/角色/场景依赖也须绑定；不能只覆盖正文源 |

这是迁移必须覆盖的责任类别和已确认接线点，不是已经完成所有别名写入的证明。实施前须以符号/别名写入审计补足完整 writer 清单，保留现有 116 captured / 95 terminal / 238 writer 等实际生产回归。immutable-string 引用方案若将“等内容新实例”简单当过期，会改变原 digest 语义；不能无说明引入。

## 根任务整合说明

`writer-review.json` 给出基于 `155f1b7a` 的四个精确 before/after hunks（UTF-8、无 BOM、LF）和前后 SHA。旧证据 `tools/MemorySummaryMainThreadBoundaryTests/source-review-b1.json` 所要求的 writer hash 正是该 before SHA。根任务可直接调用 `source_review.restore_writer(path, source)` 接到既有严格逆变换，不能直接刷新旧证据 hash。接口归一化斜杠，只接受精确当前或基线输入，并每次严格核对实际 live writer；6 项路径/旧新/未审查输入与 live 变更守卫已通过。

旧 `run_captured.py --mutate raw-low-code-unit-only` 必须同时修改两个真实编码分支：

```python
runtime = mutation(runtime, 'WriteByte((byte)(character >> 8));', '')
runtime = mutation(runtime, '_buffer[destination++] = (byte)(character >> 8);', '')
```

其他 writer 反例 `unframed-raw-strings`、`truncate-raw-long`、`repeat-raw-list-head`、`ignore-full-raw-buffer` 原唯一锚点保持；最后一个现在命中唯一 `FlushBuffer` 实现。三个新增故障 runner 已同时覆盖跨块/直接写两个分支。没有直接修改根锁住的 source_parity/旧 runner。

## 重放命令

仓库根目录运行（不用网络、游戏部署或真实存档）：

```powershell
$env:DOTNET_EXE='G:\AFMOD\.dotnet-sdk\dotnet.exe'
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryBudgetTests/run.py --writer-baseline 155f1b7a
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryBudgetTests/run.py
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryBudgetTests/verify.py
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryBudgetTests/source_review.py
foreach ($fault in @('omit-string-length','lose-high-code-unit','skip-full-buffer')) {
  & G:\Python310\python.exe -X utf8 -B tools/MemorySummaryBudgetTests/run.py --mutate $fault
  # Expected: exit 1 with BUDGET_RESULT fail > 0. Exit 2 is not a valid counterexample.
}
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_fingerprint.py
```

本子任务未 commit/push、未改游戏/存档、未恢复自动化。六 Stage 和完整相邻回归由根任务基于最终合并候选统一执行；本页不能替代该验证。
