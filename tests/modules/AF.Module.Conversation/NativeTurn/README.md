# J07 Native 整回合验收

- `run.py` 编译生产 `NativeConversationTurnCoordinator`、生产 `CaptureOnGameThreadAsync` 和生产后处理 prepare/network/complete 语句。物理独立线程模拟游戏队列；游戏对象、provider 和领域 helper 为可控替身，不是实机测试。
- 覆盖四阶段顺序、每阶段停止/异常、终态必须停止、异常不重试、默认值拒收、捕获身份、排队拒收、原 pending key/sequence 撤销、请求 ExecutionContext 保留/恢复、正文后处理主线程准备与完成、后台网络、无网络/失败回退、迟到回复丢弃。
- `run_mutations.py` 六个反例必须编译成功且命中指定断言；编译/路径失败不算红例。
- `test_source_review.py` + `tools/NativeConversationAdmissionTests/turn_extraction.py` 从**当前阶段方法**还原抽取前算法并核对完整参数/顺序及主文件周围内容，不只比较摘要。测试证明刷新摘要仍不能掩盖被改掉的 TTS 参数。
- 既有 Native 五组/正文/raw 等边界夹具继续覆盖原 owner。它们通过已验证的算法投影适配搬迁，不负责证明新增阶段调度；新增调度由本套件单独执行。不能把投影后的旧夹具称为完整新 Host 实机执行。
- 真实 Campaign、Mission、旧档、音频播放、实际 provider 和帧耗时仍为 NOT-RUN。未开启新的 Scene/Courier 对外提交能力。

从仓库根执行：

```text
python -B tests/modules/AF.Module.Conversation/NativeTurn/run.py
python -B tests/modules/AF.Module.Conversation/NativeTurn/run_mutations.py
python -B tests/modules/AF.Module.Conversation/NativeTurn/test_source_review.py
```
