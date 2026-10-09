# TTS request lifetime regression

覆盖审查 F6 的两个已确认竞态，并检验 Scene / Native 消费端的请求身份。

```powershell
python -B tests/modules/AF.Module.Llm/TtsRequestLifetimeRegressionTests/run.py
python -B tests/modules/AF.Module.Llm/TtsRequestLifetimeRegressionTests/run_mutations.py
python -B -m unittest discover -s tests/modules/AF.Module.Llm/TtsRequestLifetimeRegressionTests -p test_wiring.py
```

默认读取当前工作树 `src/modules/AF.Module.Llm/Tts/TtsEngine.cs` 和 `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs`。输出与日志仅在本目录 `.generated/`。`--consumer-source` 可显式传入待集成源码；日志记录路径和 SHA-256，**该模式不能声称已验证生产接线**。SDK 可通过 `--dotnet` 指定（默认使用 AFMOD 本地 SDK）。

## 证据边界

- 编译完整真实 `src/modules/AF.Module.Llm/Tts/TtsEngine.cs`，生命周期、WorkerLoop、ProcessJob、Gateway 调用桥、事件发布、CTS 与实际线程均非测试重写。
- 11 个 P/Invoke 声明只在生成的测试副本中替换为立即抛异常，禁止加载原生音频/窗口 API。游戏对象/设置、网络 Gateway 是 fixture；不调用真实在线 TTS、Bannerlord 或存档。
- 成功音频使用 16 字节 PCM fixture，写入 `.generated/<run>/audio-fixtures/`，以无声 Tableau 路径推进真实事件和计时循环，不播放声音。
- 消费端提取真实请求 owner、注册/完成/timeout wait 方法；Mission、save generation 为 stub。不以简化消费端替代完整 ShoutBehavior 编译和实机验证。
- `test_wiring.py` 检查实际订阅/解绑、主线程 callback 二次身份守卫、Scene/Native onAccepted 预注册及晚音频清理；它是静态接线检查，不冒充全部 UI/动作功能回放。

## 覆盖

61 个可执行场景（原 43 项 + 6 项测试按钮预检 + 2 项 V3 路由 + 10 项测试失败回执）：队列满、取消、dequeue/activation 空隙、发布前接受回调、锁外回调、回调重入/异常、Stop/Dispose、重复 terminal、worker 存活、极速合成、传给 Gateway 的真实 cancellation token、晚网络失败及已排队 UI callback、同 AgentIndex 新轮、两条同 Agent 合法 FIFO、取消 active-old 保留 queued-new、mission/session/epoch/save generation 失效。

Scene 在 onAccepted 注册 RequestId 独立 owner 及准备回调，在该请求首个主线程事件消费前一次性激活 bubble/token/feed，不以最新 accepted 覆盖仍在正常播放的请求。terminal/cancel 和显式 Mission reset 回收 owner 与 Mission 引用。

8 个定向 mutation 必须由行为断言拒绝，不能以编译失败冒充反例覆盖：发布后注册、锁内接受回调、复活 dequeue job、丢网络 token、重复 terminal、晚 legacy 事件、仅按 AgentIndex 匹配、忽略 Scene epoch。

旧审查红证据来源为提交 `35524b04` 的 `TtsEngine.WorkerLoop/StopPlayback/ProcessJob` 及 Native wait 方法；本套测试固定请求身份语义，而不是固定源码行号。

## 保留与移除

生产保留旧事件 / `SpeakAsync` 公共签名，兼容仍可能存在的外部订阅；项目内消费者必须使用 request-scoped 事件。旧全局 `_cancelCurrent`、全局测试 bypass、无请求身份的内部消费路径应删除。真实声音、口型、地图对话、暂停/切场景和第三方订阅仍需实机验收。

## V3 兼容升级（2026-10-10）

- runner 编译完整当前 `TtsEngine` 和真实 `VolcTtsGateway` 路由，网络 V1/V3 Gateway 均为可控 fixture；不声称这里验证真实 HTTP。
- 当前 SceneAudioLipSyncController / NativeConversationPlaybackWaitAdapter 的 12 个消费方法/类型直接抽取，只有测试容器访问修饰符与 Native 静态存储适配；方法体不改写。端口仅提供 scene/session 值。原 historical_source 全仓绑定因无关 WarStats 改动失效，不再作为此生产消费者的来源；未改历史 oracle 或放宽其断言。
- 测试按钮 delegate 从当前 DuelSettings 原文抽取，覆盖 V3 无 AppID 入队、总开关/专用开关关闭、空 URL、不支持 V3 协议、V1 缺 AppID 禁止入队；UI 显示是 stub，非游戏点击。
- 真 HTTP 见现有 `tests/replay/TtsGatewayReplayTests`：7 组 V1 契约检查 + 29 项 V3 回放，实际候选 DLL、实际 HttpClient、loopback TCP chunked server，包含真实播放器 WAV 解析和生产 V3 禁重定向客户端。30 秒请求体停顿测试不访问线上服务。
- 所有输出使用 `tests/output_isolation.py` 的独立 artifacts run root。`--consumer-source` 只用于显式当前 owner 拼接候选，不能以旧平铺源码假冒当前消费链。
- 遗留 `test_wiring.py` 仍绑定历史全仓 projection，不在本轮扩展修复范围；上述行为测试不是全仓静态接线/实机验收。

### 测试语音失败反馈修补（2026-10-10）

`TtsEngine` 的每条测试 job 直接携带失败回执，无全局事件订阅；共用原终态/取消守卫。runner 编译真实 `TtsTestFeedback`，在后台只排队、主线程 tick 才显示；UI stub 会拒绝后台调用。新增 10 项覆盖 V1/V3 错误可见、成功不误报、普通对话不串入测试提示、队列满拒绝、Stop 前后迟到错误抑制、重复点击、重复终态与异常回执隔离。应用 tick 快/观察两路径接线计数为静态辅助检查，不等同于真实游戏 tick。

修前红例：只显示“测试中”、失败事件已发出但无可见错误（上一轮只读审查的本地回放为 51 PASS / 1 FAIL）。新回执不增加合成、重试、成功提示或声音通道，不绕过 Scene owner；无真实游戏音频或收费 API 调用。
