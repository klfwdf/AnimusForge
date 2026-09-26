# AF Memory 调度模块契约（internal，初版）

这是职责计划 M1/M2 的捕获/接受线程基础，不是通用游戏调度器或公开子 MOD API。整个实现仍编入选定游戏版本的 AnimusForge.dll；接口和实现均internal，不改变Api.V1。

## 责任与依赖

| 层 | 唯一责任 | 禁止承担 |
|---|---|---|
| `IMemorySummaryDispatchHost` | 线程判断、worker可读的owner/档代、主线程Campaign身份、现有预算设置、失败诊断 | 队列、重试业务、Prompt、记忆数据、动作执行策略 |
| `MemorySummaryDispatcher` | 并发入队、待办claim/retire、FIFO、inline/queued额度与实际耗时、普通/完成异常语义 | TaleWorlds引用、读取Hero/Campaign、持久化或LLM调用 |
| `MyBehavior.MemorySummaryMainThread` | 每owner惰性发布一个dispatcher，提供游戏host，保留实际调用入口 | 复制旧队列、旧计数器、第二份异常/预算实现 |
| `MemorySummaryPlanning` | 规划任务本身，读取dispatcher的实际已用耗时 | 自己维护第二份dispatch耗时或重新发放同tick额度 |

生产调用仍是原 `RunMemorySummaryMainThreadAsync` / `RunMemorySummaryCompletionAsync`、Tick/Reset入口；捕获、writers、summary planner和完成均共用同一个dispatcher，不并排保留旧路径。

## 接口承诺

### Host（同DLL代码契约）

- `IsMainThread`：可用于任何提交线程；只判断所属线程。
- `IsOwnerGenerationCurrent(long)`：可在提交worker执行；**不得读取Campaign/Hero/Agent属性**，只核对owner身份及档代。
- `IsExecutionContextCurrent()`：只在主线程、执行operation前调用，核对真实Campaign owner。
- `GetBudgetMilliseconds()`：只在主线程读取，保持当前动态设置语义，不将配置固定在构造时。
- `ReportFailure(Exception)`：执行异常诊断；诊断自身失败不会挂起待办完成通知。

### Dispatcher

| 方法/读值 | 语义 |
|---|---|
| 构造(host, actionsPerTick) | host必需、额度必须正数；不查询游戏状态 |
| `Submit(generation, operation)` | worker可提交；主线程/无前序排队/有额度时同步执行，否则排队；普通operation异常记录并返回false |
| `SubmitCompletion(...)` | 调用同一Submit链；业务完成若部分执行后抛错，原异常抛回协调者，不自动重放副作用 |
| `Tick()` | 非主线程不执行；重置本tick计数/耗时，按FIFO和额度处理；当前Host仍2操作/原设置耗时 |
| `Reset()` | 仅退役队列中尚未claim的任务并返回false；不能取消已开始操作、网络调用或回滚资产/记忆 |
| `ElapsedTicks` | 主线程读取实际执行累计时间；用于原规划的剩余预算，不包括worker等待时间 |
| `PendingCount` / `HasPending` | 诊断快照，不是并发事务或“操作已完成”证明 |

生命周期：每个MyBehavior owner只发布一个实例，竞争构造时仅CAS胜出的实例可接单；多余未使用候选没有任务。空闲Tick/Reset不初始化模块。不在读档时静默换一个新dispatcher绕过旧待办退役责任。

## 变化、兼容与限制

- 提取前后的32项共同调度行为相同；新增5项检查惰性初始化、并发首次提交、失败诊断和异常身份。
- 原owner/generation判断、Campaign执行前核验、FIFO、两类异常语义和2操作上限保持。必要的私有Host调用入口是薄适配，不是兼容业务副本。
- 稳定接口指责任/调用线程/成功失败语义明确；同DLL签名未来如确需调整，必须同时迁移所有实际consumer和测试，不能静默改变语义。公开API破坏性变化另行版本化，不从本接口直接开放游戏对象。
- 本次**没有**把整图capture/copy变为逐记录预算，也没有解决完整raw/单条深操作/Apply的全部原子成本。dispatcher的2个operation不是“全系统每帧最多2条record”。
- 不承诺任意线程并发修改记忆源安全、不把队列Reset称为真正的网络取消；持久化/旧档/实机仍单独验收。

## 验证入口

仓库 `tools/MemorySummaryMainThreadBoundaryTests/run.py` 编译实际Host+runtime+契约；`--source-baseline 9617f96a` 编译原Host，执行共同32项；当前执行37项。七类 `--mutate` 定向破坏实际host/runtime的档代、owner、额度和计时防护，必须BUILD_PASS后断言失败。

相邻captured/business/planning/writers/sealing/terminal/commit-writers都编入真实dispatcher而不是替身。最终六项Stage、API/元数据和存档身份，以及源码依赖方向门禁另列在本轮HANDOFF。测试修改只是换真实状态的读取位置，不保留假的旧队列字段。
