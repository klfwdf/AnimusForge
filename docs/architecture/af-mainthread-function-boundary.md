# Native / Scene 共用主线程函数边界

## 本次解决什么

`RunNativeConversationMainThreadFuncAsync` 虽带 Native 名称，实际上有 25 个直接调用点，覆盖 Native、Scene 与 detached commit 薄桥。它不是纯读取工具，也会提交历史、排入语音、持有/释放接力参与者或执行直接指令。

旧实现用普通 bool 在 30 秒后返回 fallback；若操作已开始，调用方可能继续走失败路径，而主线程稍后仍产生副作用。日志又与完成结果混在同一个 try/catch，可能把成功改为 fallback。排队路径的 wait catch 还会吞掉原本需向上层报告的 `PreprocessFormatException`。

## 修复后的规则

| 状态 | 允许谁完成 | 等待到期后 |
|---|---|---|
| queued（0） | 回调 claim 或等待者 retire，使用 CAS 竞争 | retire（2），返回原 fallback，晚到不执行 |
| claimed（1） | 实际执行回调 | 继续等待实际结果；不宣称取消、不自动重做 |
| retired（2） | 已确定的原 fallback | 重复/晚到回调全部跳过 |

- 直接与排队执行共用局部 Execute：正常值保持；普通异常仍降级为原调用者 fallback；格式异常保留原对象向原上层失败处理传播。
- 发布失败只能 retire 尚未 claim 的工作；即使故障注入发生在发布后，也不会遗留可执行的失败回调。已 claim 的结果仍由回调负责。
- Logger/FreezeWatchdog 仅作 best-effort 观察，不拥有完成结果；`_finished` 只表示执行结束，不表示成功或持久化。
- 没有新队列/调度器、Tick 或轮询。每个排队操作一个 TCS 和可释放 timer；完成后取消 timer，不持续保留至 30 秒。去掉了观察日志中的队列 Count 读取。
- 所有业务调用点、参数和 fallback 均保持。准入/会话/存档代际是否当前，仍由原调用者检查；通用函数不冒充上下文验证器。

## 与专用边界的区别

Native 动作 runner 有类型化未知结果，pending-history runner 对未开始超时抛明确异常；本通用函数仍有原来的 fallback 兼容责任，不能混成同一种公开结果契约。因此没有强行合并这三条真实职责不同的等待边界，也没有开放 Api.V1 写操作。

普通异常 fallback **不代表没有部分副作用**。已开始的同步游戏操作也不能安全强停；若 owner 永不返回，本函数不能凭时间制造成功/失败回执。这些是后续完整请求生命周期审查的边界，不是本轮承诺的原子回滚。

## 影响面与验证

- Native：admitted request、目标验证、周报快照、主回复/后处理验证、含挑衅/直接指令的验证。
- Scene：输入/回复历史、语音入队、接力验证与参与者持有/释放、候选快照、直接指令、失败/闲置收尾。
- detached：Native opt-in 与 Scene commit 派发。

`tools/MainThreadFunctionBoundaryTests` 链接真实两个方法；132 检查、7 个变异。对两个声明做逆变换后，规范化全文等于基线，25 个调用点未变；Team ports 的 13 方法 / 31 接缝仍做原严格对照。构建/离线证据见本轮审计，不能替代真实游戏验收。

后续仍先处理 Native 更早 prepare/TTS、Courier 双向 prepare，再决定有限公共提交。不要借本次调度修复改玩法、扩大 SDK 或标整个阶段 8 DONE。

后续 Native 初始准备抽取已独立验证；runner suite 仅按精确 SHA 还原该 Submit 声明后继续全文逆变换，原 132 检查 / 7 变异保留。见 af-native-initial-preparation-boundary.md。
