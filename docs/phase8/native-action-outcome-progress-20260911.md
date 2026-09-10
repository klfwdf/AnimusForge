# Native 动作队列失败结果续作（2026-09-11）

起点 646dd987，生产 32230a64；fetch 后同名远端 ahead 11 / behind 0。两份用户草稿保持，不推送、不部署。

已确认：旧主线程动作队列 catch 把预先创建的 Content 结果交给 TaskCompletionSource，ResponseDiscarded 仍为 false；后续 Native 正常收尾会继续写历史。外层还把 enqueue 后的日志异常视为未派发失败，但已入队动作可能随后执行。

范围：统一 direct/queued 动作派发的异常结果；仅在动作 owner 进入之前与之后区分 NoConfirmedEffect / UnknownAfterStart（复用现有枚举）。异常不能返回普通成功回复、不自动重试、不删除已确认的先前效果。观测日志异常不得替代动作 Task 结果。沿用完整原 Action core、原准入与 UI 展示 scope，不重写业务规则或开放新 API。

本轮不宣称整个回合无副作用：Raw taunt/自然动作等可能在动作派发之前发生。完整历史/事实原子回执、Native prepare 和 Courier prepare 仍待后续。先执行旧真实队列反例，再验证真实新派发器、结果传播与 UI 处理，最后 Stage、回归和交接。
