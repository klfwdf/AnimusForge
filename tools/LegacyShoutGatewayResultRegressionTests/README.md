# Legacy Shout Gateway result regression

```powershell
python -B tools/LegacyShoutGatewayResultRegressionTests/run.py
python -B -m unittest discover -s tools/LegacyShoutGatewayResultRegressionTests -p test_compatibility.py
# Intentional red baseline (must fail behavioral assertions, not compilation):
python -B tools/LegacyShoutGatewayResultRegressionTests/run.py --source-ref 5ce8767a --output-name red-5ce8767a
```

- 当前：40 PASS / 0 FAIL；旧 `5ce8767a`：18 PASS / 22 FAIL。
- 3 个兼容/来源检查；`SendLegacyMessagesAsync`、`SendLegacyMessagesStreamAsync`、`GenerateStreamAsync` 对旧基线逐字保持不变。
- 编译真实完整 Gateway、契约 DTO、Prompt adapter、SaveRuntimeGuard；错误 envelope 使用真实 `LlmRetryPrompt.BuildFailureDetail` 方法构造。仅 ShoutNetwork 传输及同步辅助后处理为确定性 stub，不调用在线 API、游戏或存档。
- 覆盖成功、空、HTTP/解析/配置/程序错误、stale 文本、取消/读档前后、晚成功/失败/异常、取消优先级、合法正文异常词、旧 facade 参数与返回值、原流式返回、同步 Action 返回后的拒收。
- 失败识别只匹配 ShoutNetwork 当前实际发出的保留前缀及 `BuildFailureDetail` 详情标记。没有用 `IsRetryableLlmError` 的宽泛全文异常关键词误伤正文；字符串式旧协议仍不能从信息论上区分 NPC 故意完整仿造的错误 envelope，这不是增加一个字符串判断可以消除的歧义。
- `AIConfigHandler.TryCallAuxiliaryActionPostprocessOnceForExternal` 没有 cancellation 参数，**运行中的同步网络不能通过本 Gateway 中止**。本轮防止启动已失效调用并拒收晚返回；测试明确在取消后、释放同步 barrier 前任务仍未结束。未宣称实现底层可中断取消。
- 生成物/日志在 `.generated/`，仅本目录私有，不污染正式项目。`--source-ref` 只替换 Gateway 旧版本，用相同当前契约和相同断言比较。
