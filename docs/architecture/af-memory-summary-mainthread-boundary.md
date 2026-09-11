# 压缩记忆结果主线程提交边界

源码版本：`9040d184998120d3476426337160403f08fdfee9`；来源基线：`e40c92d72524b7ea80a5dc0e36dc996963a62e66`。

## 责任与数据流

`TryStartMemorySummaryQueue` 仍从 Campaign tick 启动原 `ProcessMemorySummaryQueueAsync`。网络、RPM 波次、重试和响应解析不搬到 EngineTick；第一次网络 await 之后，三类成功/失败结果不再直接写 `MyBehavior`。

```text
Campaign tick
  -> ProcessMemorySummaryQueueAsync
  -> 原 provider / retry / parse
  -> RunMemorySummaryMainThreadAsync 发布完成动作
  -> OnEngineTick 每 tick 最多消费 2 个
  -> 复核 main thread + Instance + Campaign behavior + generation
  -> Apply*Success / Mark*Failure / queue cleanup / notice / processing release
```

权威 owner 仍是当前 Campaign 的 `MyBehavior`。新队列不是第二套记忆服务，也不保存到 SyncData。

## 已接线源码

| 路径 / 行号（`9040d184`） | 符号 | 责任 |
|---|---|---|
| `MyBehavior.MemorySummaryMainThread.cs:12-42` | `MemorySummaryMainThreadAction` / `_memorySummaryMainThreadActions` | generation-bound 完成动作和单一低频队列 |
| `MyBehavior.MemorySummaryMainThread.cs:44-73` | `RunMemorySummaryMainThreadAsync` | 主线程直达；后台入队前后双检 owner/generation，避免旧 owner 队列悬挂 |
| `MyBehavior.MemorySummaryMainThread.cs:75-90` | `TryApplyMemorySummaryMainThreadAction` | 复核物理主线程、Instance、save generation 与当前 Campaign behavior；异常隔离为拒绝 |
| `MyBehavior.MemorySummaryMainThread.cs:92-110` | `ProcessMemorySummaryMainThreadActions` | `OnEngineTick` 每次最多消费 2 个，只执行已 claim 一次的动作 |
| `MyBehavior.MemorySummaryMainThread.cs:112-119` | `ResetMemorySummaryMainThreadActions` | 读档/清数据时退休尚未开始的工作并完成等待者 |
| `MyBehavior.cs:4957-5129` | `ProcessMemorySummaryQueueAsync` | 三批 post-await 接受、失败登记、队列清理、提示和 processing 释放经新边界 |
| `MyBehavior.cs:20311-20333` | `OnEngineTick` | 在其他记忆/UI 消费者前排空本队列的有界份额 |
| `MyBehavior.cs:2415-2419` / `48278-48282` | 两个 reset 调用点 | loaded-save 瞬态重置与当前存档清理不遗留等待者 |

## 保持不变

- 三渠道 prompt、role、AFEF、动作执行入口和 daily draft owner 不变。
- 三次重试、RPM burst、批间延迟、成功/失败玩家文字、Summary/重大履历/overview 顺序不变。
- 没有新增或删除 SyncData key、CampaignBehavior、模块身份、公开类型或公开方法；`Api.V1` 仍只读。
- 政策、宴会、GCCZ 业务和默认交互入口未改。

## 性能与失败语义

日结 worker 在每次发布后等待接受，正常不会无界堆积；EngineTick 上限为 2，避免一帧吞掉任意数量完成动作。使用 `ConcurrentQueue` 与 CAS，不做反射、全局扫描、锁等待或轮询。旧 owner/generation 在入队前直接返回；与入队竞争的 reset 在入队后再次退休，等待任务不会挂死。主线程验证或提交异常返回 false，不在后台重放写入。

## 明确未覆盖

本切片只封住 post-await 可变提交。`ProcessMemorySummaryQueueAsync` 首次 await 前的调度快照，以及 `ExecuteMemorySummaryJobAsync`、`ExecuteMajorActionSummaryJobAsync`、`ExecuteMemoryOverviewJobAsync` 的 prompt/目标准备仍可能在异步 continuation 上读取 live owner/game 状态；当前任务也没有逐任务 source fingerprint。未执行真实游戏、provider、旧存档或读档晚返回验收，不能标记完整记忆线程安全或阶段八完成。

下一独立切片应在 Campaign 主线程捕获三类任务的只读输入与精确来源 revision/fingerprint，后台只做 provider/解析，并在本边界接受时拒绝来源已改变的结果。
