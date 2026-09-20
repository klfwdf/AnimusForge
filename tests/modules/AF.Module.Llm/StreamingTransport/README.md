# J08 流式传输责任包

- `LlmStreamingTransport` 每次调用只拥有一个 SSE HTTP attempt：认证、request/response/stream/reader 生命周期、HTTP error body、`Retry-After`、`data:`/`[DONE]`、content/reasoning delta、有限 raw sample 与原异常上的部分 raw 证据。
- Primary 仍拥有 thinking retry、无内容降级、玩家可见过滤、token/性能诊断和 UI callbacks；Configured Gateway 保留 typed status/metadata。两个真实消费者均已接线，没有第二条发送管线。
- `run.py` 用确定性 sender 执行生产 owner，覆盖 Unicode/reasoning、无关 SSE 行、坏 chunk 隔离、raw 上限、429、headers/line stale 拒收、非权威 observer、读取异常部分证据、取消和所有终态资源释放。
- `run_mutations.py` 的五项变异必须编译成功并命中具名断言。实际 Debug DLL 由 `tools/PrimaryLlmGatewayReplayTests` 验证 Primary thinking fallback、Unicode、增量/最终不重复、取消，以及已发布部分正文后禁止 stream/non-stream 重放。
- `primary-source-review.json` 只允许 J01 协议 suite 投影已经由上述实际 owner/DLL 测试覆盖的 Primary consumer 差异；不能代替行为验收或掩盖其他 `ShoutNetwork` 变化。

未访问真实 provider，未验证游戏 UI/TTS、真实网络背压/超时精度和帧性能。
