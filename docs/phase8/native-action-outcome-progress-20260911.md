# Native 动作队列失败结果续作（2026-09-11）

起点 646dd987，生产 32230a64；fetch 后同名远端 ahead 11 / behind 0。两份用户草稿保持，不推送、不部署。

已确认：旧主线程动作队列 catch 把预先创建的 Content 结果交给 TaskCompletionSource，ResponseDiscarded 仍为 false；后续 Native 正常收尾会继续写历史。外层还把 enqueue 后的日志异常视为未派发失败，但已入队动作可能随后执行。

范围：统一 direct/queued 动作派发的异常结果；仅在动作 owner 进入之前与之后区分 NoConfirmedEffect / UnknownAfterStart（复用现有枚举）。异常不能返回普通成功回复、不自动重试、不删除已确认的先前效果。观测日志异常不得替代动作 Task 结果。沿用完整原 Action core、原准入与 UI 展示 scope，不重写业务规则或开放新 API。

本轮不宣称整个回合无副作用：Raw taunt/自然动作等可能在动作派发之前发生。完整历史/事实原子回执、Native prepare 和 Courier prepare 仍待后续。先执行旧真实队列反例，再验证真实新派发器、结果传播与 UI 处理，最后 Stage、回归和交接。

## 本轮落地

- direct/queued 共用真实同步执行边界；复用现有 ActionExecutionEffectState 表达未进入 owner 与 UnknownAfterStart，不把异常装成普通回复。
- 唯一 TaskCompletionSource 由 claim 成功的 callback 完成；若 enqueue 发布后抛错，只能取消尚未 claim 的 callback，不能覆盖已开始 callback 的结果。
- 诊断日志与 FreezeWatchdog 观察失败不再改变 Task 结果或触发另一条 fallback。
- 两条 Overlay 对类型化动作异常显示明确警告，抑制正常 ready 提示，不调用自动重试。消息只描述本次派发，不承诺整个回合零副作用。
- 业务 Core 与正常返回内容未改；已经发生或各 owner 已记录的效果不删除。历史/AFEF 的完整原子提交仍待继续。

## 当前证据

原真实队列反例已复现。新 59 个检查、6 个行为变异；原准入/展示及相关回归与最终六项 Stage 构建详见本轮审计记录。未推送、未部署、未实机验收。

后续仍需处理未消费动作队列的超时/取消、成功路径的主线程完成与事实回执、Native prepare/TTS 直接回调、Courier 双向 prepare。自动化继续，不把本次故障语义修正当作整个 Native 服务完成。

本轮实现/测试已提交 `8da4fbd7`；精确证据见 `docs/audits/2026-09-11-native-action-outcome-verification.md` 及同名 JSON。断开后已复核全部回归日志和六 DLL marker，并重新执行 59 项定向检查。

## 同链续作检查点：未消费队列等待

起点 `b7128a7d`（生产 `8da4fbd7`）。当前派发 Task 对永远未消费的队列没有期限，后端 busy 因此可能一直占用。下一步先执行真实派发方法的有限等待反例，再复用已有 30 秒主线程等待预算，仅允许 CAS 从 queued 转为 expired；callback 已 claim 时不得超时放弃真实结果。结束时取消计时器，不新增 Tick/轮询/请求重试或物理网络取消承诺。保持旧业务 Core、准入、展示、渠道路由和存档身份不变。

## 未消费等待已落地（9a5335be）

- 复用 30 秒预算，唯一 CAS 只允许未 claim 请求过期；晚到 callback 无效。已经 claim 的动作等待真实结果，正常完成取消计时器。
- 队列超时使用显式错误分类，守卫的 TimeoutException 不冒充队列过期；只取消此队列待执行项，不回滚先前效果。
- 原方法有限等待反例和修复前运行时失败均保存；当前 88 检查 / 9 变异、原准入/展示/ports、六项 Stage 和 16 组回归通过。
- 审计：`docs/audits/2026-09-11-native-action-timeout-verification.md` 及同名 JSON；技术责任：`docs/architecture/af-native-action-dispatch.md`。
- 下一项：Native 成功完成路径的主线程事实/记忆边界。先验证旧会话晚完成与 owner 合法结束会话两种情况，既不能污染新会话，也不能丢掉实际已发生事实；不能直接换一个 guard 就宣称事务/回执完成。其后处理 Native prepare/TTS 与 Courier 双向 prepare。
