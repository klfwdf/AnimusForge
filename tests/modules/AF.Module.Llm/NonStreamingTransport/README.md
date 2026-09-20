# J08 非流式 HTTP 责任包

`run.py` 用确定性 `HttpMessageHandler` 执行当前生产 transport、当前/旧 Primary 非流方法以及当前/旧 Configured Gateway。协议序列化/认证/正文抽取使用生产 `LlmApiCompat`；MCM、玩家姓名、日志、UI 重试与存档代际为明确的替身，不读游戏、不访问真实 provider。

- Primary 15 组旧/新对照：成功、thinking 400、不可自动重试的 400/500、空回复一次补救/持续空回复、坏 JSON、网络异常、缺配置、首次/重试的 header/body 代际变化、用户确认后重试。比较最终文本、完整请求 JSON、请求次数和代际检查顺序。
- Configured 7 组旧/新对照 + 调用方取消/超时，保留状态/正文/错误码、完整请求、Retry-After、调用方 token 所有权。坏 JSON 按真实旧协议兼容逻辑对照，不凭测试作者想象改成新错误策略。
- 额外验证 Primary 调用方取消传播、拒收/异常时释放 response、headers 拒收不继续处理 body。
- 复现旧 Primary 成功响应未 Dispose，验证新 owner 所有已获取 response 都被释放。该资源生命周期修复是有意差异；文本/重试策略不变。
- 五个变异必须编译成功并命中预定失败断言；路径错误或编译失败不算红例。
- `primary-source-review.json` 仅把 J08 已执行对照的 Primary 方法差异投影掉，让既有 J01 的完整源码逆向检查继续工作。它不是行为验收的替代品，也不复制旧业务实现到生产目录。

```text
python -B tests/modules/AF.Module.Llm/NonStreamingTransport/run.py
python -B tests/modules/AF.Module.Llm/NonStreamingTransport/run_mutations.py
```

本套件不证明完整游戏 Host、真实 provider、真实时钟网络超时精度或帧性能；streaming/model/TTS 仍按后续 J08 责任包验收。
