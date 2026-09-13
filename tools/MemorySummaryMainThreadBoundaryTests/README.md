# Memory summary main-thread boundary tests

```powershell
python tools/MemorySummaryMainThreadBoundaryTests/run.py
python tools/MemorySummaryMainThreadBoundaryTests/run.py --original
python tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate ignore-generation
```

The runtime harness compiles the production `MyBehavior.MemorySummaryMainThread.cs`
boundary with a small Campaign fixture. It covers direct and queued execution, bounded
drain, owner/Campaign/generation rejection, reset completion, wrong-thread drains and
exceptions. `run.py --original` only detects the old `e40c92d7` source and exits with
failure; it does **not** execute that business method. Its three helper mutations
must fail at runtime. The source-fragment checks are auxiliary wiring evidence, not
proof that all business operations execute behind the boundary.

This is an offline concurrency/ownership replay. It does not call a provider, load a
save, or prove Bannerlord live acceptance.

## P1-01 首轮（历史基线 `2c90ef8a`）：真实完成业务链回放

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py --original
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py --mutate worker-primary
```

- `run_business.py` 提取完整 `ProcessMemorySummaryQueueAsync`、六个实际 Apply/Mark、队列 sanitize/pending、状态归一化及对应数据模型；不在 fixture 中复写这些业务。`--original` 提取并编译执行 `e40c92d7` 的实际方法，这才是运行时旧问题红例。
- 14 个场景覆盖三类型成功/部分失败、真正 dictionary/list/draft/重试状态、顺序、空队列重入、过期/畸形结果、额外 overview 成功/失败、换 owner/读档/退休、额外等待期读档、provider 异常、来源 owner 不匹配、目标失效和实际 Apply 数量观测。
- 14 个 mutation：`worker-primary`、`worker-extra`、`worker-cleanup`、`worker-release`、`omit-release`、`omit-cleanup`、`omit-mark-daily`、`omit-mark-major`、`omit-mark-overview`、`duplicate-apply`、`accept-obsolete`、`ignore-owner`、`ignore-generation`、`ignore-draft-owner`。前 11 个修改实际调用方法，末三项修改所属守卫；必须编译成功后执行断言失败，提取/编译错误不能计为成功红例。
- 正常返回 0；运行断言失败返回 1；编译/非正常进程没有业务结果返回 2，提取异常也不能作为红例。每次在 `.generated/business/<case>/` 留生成源码、源码行号/符号/hash manifest、build.log、run.log；不修改生产或打包这些 fixture。

### 哪些是替身，哪些确实执行

`BusinessHarness.cs.txt` 的 TaleWorlds/Campaign/Hero、provider executor、底层内存存取/序列化、block/action 深层 sanitizer、目标清理、overview 入队资格、周报/声望/Native 历史/UI 发布末端均为明确替身；真实 Apply/Mark 的控制流和 list/dictionary/草稿改写执行。provider 的 incomplete TCS 强制真实 Process 跨 await，不访问网络；不能证明真实三类 Execute 的输入捕获、重试或 RPM。

只对生成的业务代码做三类测试变换：60 秒延时替换为受控异步时钟门；六个 Apply/Mark 入口记线程事件；真实 `_memorySummaryProcessing = false` 前记事件。其余实际业务条件和写入不替换。真实主线程队列 helper 与 `SaveRuntimeGuard.cs` 一起编译；fixture 直接驱动 drain，不是完整 `OnEngineTick`/Campaign。异常 popup 的 publish 可以后台调用，真正 UI 消费由原 `MemoryFailureUiBoundaryTests` 覆盖。

### 尚未解决的生产问题

当前基线中，12 个 daily 结果可同一 Tick 被一次回调全部应用（只观测处理数，非真实帧时）；过期 successful payload 虽未 Apply，仍被计入完成提示，并可能再次规划 overview。新工具输出 `BUDGET_OBSERVATION` / `STALE_UI_OBSERVATION` 如实记录，没有断言这些缺陷必须永远保留。修复须增加对应目标断言，不能只改说明或把观察值当预算已通过。

本项是完成层离线证据；同 generation 的精确 source fingerprint、实际 job/record/耗时预算、真实 provider、游戏线程/旧存档验收仍由 P1-02/03/04 与后续验收承接。

## B1 当前联合候选（继续同一批，未整批合格）

沿用上面的 `DOTNET_EXE` 和 Python 环境，一次检查这五层，而不是仅跑 helper：

| Runner | 结果 / 实际执行责任 | 明确替身与边界 |
|---|---|---|
| `run.py` | 32 个断言；FIFO、owner/load/reset、inline/queued 共享数量和实际 Stopwatch 计时，超时后下一操作留给下个 Tick | 直接驱动 drain；1 ms 时间场景使用真实耗时，其他场景隔离 JIT/机器负载；不是硬抢占或完整 EngineTick |
| `run_business.py` | 33 场景；真实入场/maintenance/扫描策略、三型 Process/Apply/Mark、终态清理、部分执行异常提示和释放 | provider executor、源有效性和底层游戏端为明确 seam；不是完整来源证明 |
| `run_captured.py` | 61 场景；真实 Capture/Execute/Build/Parse/JSON/tag/retry/source；10 模型+2列表反射覆盖351个标量检查、49个可变节点分离，251次逐字段来源变动拒绝 | HTTP/game/settings/rendering为替身；不模拟真实 provider/游戏 |
| `run_writers.py` | 238 项；8个真实旧 façade、封送、参数/嵌套DTO复制 | 存储/weekly末端为替身，由 terminal 补具体真实链路 |
| `run_terminal.py` | 47 场景；真实Daily append/Save/readback、Recovery Daily→Recent及ledger、Weekly outcome/回读/ledger、Major记录和压缩块Save→在途总结失效；两处真实Apply部分写入异常 | 场景/人物事实、provider、UI显示、若干下游发布是隔离边界；不等于普通提交/导入/编辑所有调用方已验证 |

```powershell
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py --mutate swallow-completion-failure
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py --mutate drop-forced-rescan
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate omit-time-charge
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_captured.py --mutate drop-nested-copy
```

正常返回0；反例必须编译成功后执行断言失败，提取/编译/工具错误不能计成红例。所有 runner 的 `--help` 列出对应故障变体。保存于忽略的 `.generated/<suite>/<variant>/`，保留输入清单、精确提取声明、生成 hash、build/run 日志；历史结果不可冒充当前源码。

`source_parity.py` / `source-review-b1.json` 只对37个已审声明逆变换（原25+10模型+2真实调度入口），并约束对应 runner/harness hash；随后原 whole-owner/default 校验完整执行。不得仅刷新hash消除未解释的变化。

### 实际变化与仍未通过的门槛

- typed copy 分离完整可变图；SHA256 流式接收完整来源/Prompt/解析依赖，不再构造嵌套转义大JSON。1000行源仍全部保留，测试分配由旧9,257,688降到1,579,504 bytes；耗时随机器负载变化，不作游戏帧率承诺。
- 入场只读raw数量，真实资格筛选集中到guarded planner；无效队列仍清掉，强制扫描/节流及terminal重复项顺序保留。候选ID排队不冒充创建了总结job。
- 实际累计耗时超过既有维护配置后不启动下一操作；**单个大源、初筛/排序/extra/cleanup/Apply内循环仍可超预算**，不能把协作式上限当硬record/time保证。
- 部分Apply异常不再静默消失：异常沿专用Completion包装进入原通知，保留已发生副作用、停止本轮、不盲重放；这是partial/unknown通知，**不是事务回滚、尾项恢复或全局exactly-once已经完成**。
- Recent-only不会虚构为Daily来源变化；普通DialogueHistoryCommit、编辑/导入调用方线程责任、完整恢复load/retention、LIVE/SAVE仍未验。继续B1，不提前进入B2。
