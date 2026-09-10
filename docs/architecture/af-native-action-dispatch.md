# Native 动作派发：状态与责任

这是 `AnimusForge.dll` 内的主体后处理接缝，不是子 MOD 新公开 API，也不是新的动作执行器。唯一业务执行器仍为原 `ApplyNativeConversationGameActionsCore`。

## 执行顺序

1. 完整 Native 后处理生成标签，调用 `ApplyNativeConversationGameActionsOnMainThreadAsync`。
2. 已在主线程则直接走统一执行边界；后台调用则排到既有主线程队列。
3. callback 先取得唯一 claim，再重新校验原请求准入/上下文；不合法就丢弃，不能执行旧会话动作。
4. 紧接 owner 调用之前标记 started。成功保留原结果；异常变为类型化失败，不伪造普通回复。诊断失败不改业务结果。
5. 调用方 await 成功才进入原正常收尾；失败由两个 Overlay 在原展示 scope 内报告，不自动重试整轮。

## 等待与结果不是一回事

| 状态/事件 | 行为 |
|---|---|
| queued | 尚未 claim；复用主线程等待预算 30 秒 |
| claimed | callback 已取得执行归属；等待真实结果，不按时钟抛弃 |
| expired/cancelled-before-claim | CAS queued→expired 成功；后来的 callback 无权执行 |
| 入队失败但 callback 已 claim | 只有 callback 能决定结果，不能用 enqueue 异常覆盖 |
| owner 前守卫失败 | NoConfirmedEffect，仅表示这次派发没进入 owner |
| owner 已进入后异常/空结果 | UnknownAfterStart，可能已有部分效果，禁止整轮自动重放 |
| owner 正常返回 | 保留原结果；不等于所有异步玩法效果和事实最终成功 |

计时器仅对后台排队请求建立一次；正常结束后取消/释放。不新增 Tick、热路径全量扫描或独立重试任务。计时只访问本次 CAS/Task 和已捕获诊断字段，不在后台访问游戏对象。

`native.actions.dispatch_timeout` 只由真实 queued→expired 分支指定；不能把任意 TimeoutException 当成队列超时。此时当前队列动作不会晚到补做，但前面的 raw/taunt/自然动作等可能已经发生。不是整个回合回滚，也不是物理网络取消。

## 保留与未完成

原 Action core、三渠道规则/标签、经济等 owner、已发生效果与事实不变。没有公共动作授权，没有新的 save key、持久化票据或 Contracts DLL。

主线程若在已开始动作中永久卡住，此边界不能安全中断它；也不能保证 UI 在主线程卡死时立即显示报错。Native 成功路径的事实/记忆主线程完成、更早 prepare、TTS 直接回调以及 Courier 双向 prepare 仍需接续。

## 验证入口

`tools/NativeActionDispatchOutcomeTests` 执行真实派发与失败消费代码；游戏业务由 fixture 代替。原 59 检查加上未消费等待、晚到 callback、跨期限真实线程及错误分类，共 88 检查 / 9 变异；结合原准入、展示、ports 和 Stage 回归使用，不能当成实机验收。
