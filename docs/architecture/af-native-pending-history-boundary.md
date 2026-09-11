# Native 前置历史边界

属于 AF 主体，不是子 MOD 写 API，也不是另一套记忆系统。

## 顺序与身份

1. 完整 Native 仍沿原 LLM/后处理路线；在需要当前玩家输入与 Native 历史时，排到原主线程队列。
2. 出队验证原 admission；固定一次 history key，读取显示名，写 tentative player event，消费该 key 的 pending AFEF，再用原窗口/formatter 构造 Native 历史消息。
3. 回到后台的只有 key、event sequence、显示名与消息列表；后续 prompt 继续使用相同参数与角色结构。
4. 五个拒绝分支 await 主线程清理；action discard 在已占用的主线程消费内调用同一清理。
5. 清理验证原 owner/generation/epoch/manager/token/Mission/revision，不重算 party key，不使用后来替换的 CurrentInstance。只移除固定 key+sequence 的 player 和场景 user 镜像，不删除 fact/system。

加载实际会重置全局 event sequence，因此序号本身不是跨存档身份；它必须和捕获上下文共同使用。上下文已退休时跳过旧清理，不能为了“清干净”盲删新记录。

## 队列责任

history 专用 runner 只服务 prepare/cleanup，仍使用原 _mainThreadActions。只有 queued 能被 deadline 或失败发布取消；claimed 后必须等真实结果。重复 callback 只能执行一次。未开始超时明确抛 TimeoutException，不能静默返回空结果冒充已处理；日志失败不改变结果。计时结束主动取消/释放 timer，无新增 Tick/轮询。

历史批次未全局替换通用 RunNativeConversationMainThreadFuncAsync。后续共用调度修复已独立完成；其普通 fallback 与这里的明确历史异常仍有不同责任，见 af-mainthread-function-boundary.md。

## 保留与性能

复用现有 append、窗口、clone、消息投影与 AFEF consume，只给私有 append/snapshot/renderer 增加捕获键参数；旧默认调用不变。本段历史 key 从多次游戏解析减为一次，每请求一个小数据快照，无热路径全仓/全角色扫描。

只处理 tentative 输入，不回滚已经发生的玩法/持久记忆。普通和主动 NPC 的角色结构、玩家名 fallback、每日窗口和事实保留策略不变。

## 未完成

这不是完整 Native prepare 迁移：此前的人设/规则/持久记忆等游戏读取、TTS 引擎直接回调、Courier prepare 还需接续。无真实游戏/旧存档验收，公共 Api.V1 保持只读。新的快照/清理不是持久化或完整恢复 receipt。

验证入口：tools/NativePendingHistoryBoundaryTests；总状态见 HANDOFF.md。
