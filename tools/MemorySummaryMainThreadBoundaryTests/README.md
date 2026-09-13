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

## P1-01：真实完成业务链回放

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
