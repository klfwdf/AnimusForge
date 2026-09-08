# TTS request lifetime regression

覆盖审查 F6 的两个已确认竞态，并检验 Scene / Native 消费端的请求身份。

```powershell
python -B tools/TtsRequestLifetimeRegressionTests/run.py
python -B tools/TtsRequestLifetimeRegressionTests/run_mutations.py
python -B -m unittest discover -s tools/TtsRequestLifetimeRegressionTests -p test_wiring.py
```

默认读取当前工作树 `TtsEngine.cs` 和 `ShoutBehavior.cs`。输出与日志仅在本目录 `.generated/`。`--consumer-source` 可显式传入待集成源码；日志记录路径和 SHA-256，**该模式不能声称已验证生产接线**。SDK 可通过 `--dotnet` 指定（默认使用 AFMOD 本地 SDK）。

## 证据边界

- 编译完整真实 `TtsEngine.cs`，生命周期、WorkerLoop、ProcessJob、Gateway 调用桥、事件发布、CTS 与实际线程均非测试重写。
- 11 个 P/Invoke 声明只在生成的测试副本中替换为立即抛异常，禁止加载原生音频/窗口 API。游戏对象/设置、网络 Gateway 是 fixture；不调用真实在线 TTS、Bannerlord 或存档。
- 成功音频使用 16 字节 PCM fixture，写入 `.generated/<run>/audio-fixtures/`，以无声 Tableau 路径推进真实事件和计时循环，不播放声音。
- 消费端提取真实请求 owner、注册/完成/timeout wait 方法；Mission、save generation 为 stub。不以简化消费端替代完整 ShoutBehavior 编译和实机验证。
- `test_wiring.py` 检查实际订阅/解绑、主线程 callback 二次身份守卫、Scene/Native onAccepted 预注册及晚音频清理；它是静态接线检查，不冒充全部 UI/动作功能回放。

## 覆盖

43 个可执行场景：队列满、取消、dequeue/activation 空隙、发布前接受回调、锁外回调、回调重入/异常、Stop/Dispose、重复 terminal、worker 存活、极速合成、传给 Gateway 的真实 cancellation token、晚网络失败及已排队 UI callback、同 AgentIndex 新轮、两条同 Agent 合法 FIFO、取消 active-old 保留 queued-new、mission/session/epoch/save generation 失效。

Scene 在 onAccepted 注册 RequestId 独立 owner 及准备回调，在该请求首个主线程事件消费前一次性激活 bubble/token/feed，不以最新 accepted 覆盖仍在正常播放的请求。terminal/cancel 和显式 Mission reset 回收 owner 与 Mission 引用。

8 个定向 mutation 必须由行为断言拒绝，不能以编译失败冒充反例覆盖：发布后注册、锁内接受回调、复活 dequeue job、丢网络 token、重复 terminal、晚 legacy 事件、仅按 AgentIndex 匹配、忽略 Scene epoch。

旧审查红证据来源为提交 `35524b04` 的 `TtsEngine.WorkerLoop/StopPlayback/ProcessJob` 及 Native wait 方法；本套测试固定请求身份语义，而不是固定源码行号。

## 保留与移除

生产保留旧事件 / `SpeakAsync` 公共签名，兼容仍可能存在的外部订阅；项目内消费者必须使用 request-scoped 事件。旧全局 `_cancelCurrent`、全局测试 bypass、无请求身份的内部消费路径应删除。真实声音、口型、地图对话、暂停/切场景和第三方订阅仍需实机验收。
