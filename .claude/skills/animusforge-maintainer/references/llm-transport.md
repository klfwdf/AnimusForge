# LLM 传输边界

用于 AF 的非流/流式 HTTP、provider 与模型目录、取消/超时以及 TTS 网络传输。先跟踪真实消费者和当前请求顺序；不要为了统一而新建第二条缩水请求链。

## 分层责任

```text
渠道/领域 owner
→ 配置与模型选择
→ 请求/Prompt policy 与 retry 决策
→ transport attempt（认证、send/read、资源生命周期）
→ 协议解析 / stream state
→ 可见文本、游戏音频或领域结果
```

- **一次 transport 调用只代表一次 attempt。** thinking-control fallback、空回复补救、用户确认重试、领域重试和 backoff 仍由当前策略 owner 明确发起；transport 不在内部静默重发。
- Transport 接收已准备的 detached 请求数据，不读取 `Hero`、`Mission`、MCM UI 或领域状态，不拥有玩家提示、错误文案、Prompt 规则或模型选择。
- 请求、响应、content、reader、linked cancellation source 和其他可释放资源由创建它们的边界释放。先读取需要的 body/headers，再返回脱离 `HttpResponseMessage` 的结果；成功、HTTP 失败、拒收、解析异常和 acceptance callback 异常都要覆盖。
- 认证仅在 send 边界注入；API key 不进入 DTO、请求日志、token dump、存档或 HANDOFF。

## 取消、超时与迟到结果

- 区分 caller cancellation、owner timeout、存档/会话 stale 和 provider/HTTP failure；它们不能都折叠成一个可重试错误。
- 创建 linked timeout 的 owner 只释放 linked source，不取消或释放调用方 source。一个 retry policy 若共用总预算，要明确预算覆盖哪些 attempts；不要每次重试偷偷获得无限新预算。
- `OperationCanceledException` 是否继续传播或映射成 typed `Cancelled` 由现有调用契约决定。对照当前消费者，不因为目录迁移统一成新的语义。
- 网络完成后再接受结果时，检查捕获的 generation/owner/source；拒收不得发布正文、触发动作或再次发送。

## 非流与流式不要假统一

- 非流共享认证、send/read、HTTP 元数据和资源释放；响应正文解析、错误文案及 retry policy 可由上层 owner 保留。
- 流式需要独立的有限状态：headers 接受、逐行/事件读取、`[DONE]`、reasoning/content、可见文本过滤、累计正文、完成/错误回调、取消和迟到关闭。
- SSE 分片不是 Unicode 字符边界。过滤器必须跨 chunk 保留 pending prefix/surrogate/协议状态；回调增量与最终正文不能重复。
- 已发布部分正文后发生错误时，明确是保留部分正文、报告错误还是降级；不得自动重放导致重复 UI/TTS/动作。
- TTS transport 只处理音频请求/字节/错误；voice 选择、`Hero`/`Agent` 捕获、Mission 生命周期、播放/停止仍属于游戏音频 owner。

## 验证方式

优先用确定性 `HttpMessageHandler` 或等价 sender 执行**实际生产 transport 和真实消费者**，而不是只测重新实现的假方法。至少按改动风险覆盖：

- 完整 URL、headers、payload、认证不落 body/log；
- 成功、4xx/5xx、429/`Retry-After`、坏 JSON、空回复、thinking fallback；
- caller cancel、timeout、迟到 stale、acceptance callback 异常；
- request/response/content/reader 在所有终态释放；
- streaming 的 SSE 分片、Unicode、过滤、增量/最终不重复；
- provider/model gateway 的真实消费者与既有错误/status/metadata 语义。

旧/新对照应比较请求、请求次数、接受点顺序、输出和异常/状态，不把测试作者期望替代旧运行语义。若发现资源泄漏等真实缺陷，先保留复现，再把它记录为有意修复。关键变异必须编译成功并命中具名断言；路径或编译失败不算有效红例。

真实 provider、真实音频和游戏 Host 分层标记 `NOT-RUN`；fixture 通过不等于付费 API、Bannerlord 生命周期或帧性能通过。
