# 阶段 5：统一交互请求协调器

`Refactor/Runtime/InteractionRequestCoordinator.cs` 是三渠道切换前的公共运行时外层。它不负责选择玩法规则、合成 Prompt、解析标签或执行游戏动作，只负责把已有 `InteractionPipeline` 安全地包在请求生命周期中。

`Refactor/Adapters/LegacyInteractionPipelineComposition.cs` 提供显式 ports，把各旧入口已有的规则选择、Prompt 合成、后处理上下文、标签解析和可见文本规范化注入同一个组合根；它不复制第二套规则，也不持有旧模块私有字段。

`Refactor/Adapters/LegacyNativeConversationFacade.cs` 是第一个旁路渠道 facade：它将调用方注入的 Native snapshot capture、三阶段 coordinator 和主线程 commit 串起来。生产入口可以传入 `LegacyInteractionSnapshotAdapters.CaptureNativeConversation`，纯测试则可传入 fixture capture，因此不会把 Bannerlord 依赖带入公共组合根，也不会悄悄替换当前 `ShoutBehavior` 默认路径。

## 已实现边界

- 从不可变 `InteractionEnvelope` 读取 channel/session identity；
- 从 `RuntimeConfigSnapshot` 解析模块开关和 provider；
- 同一 channel/session 的新请求取消旧请求；
- 外部取消映射为 `CancelledAsStale`；
- 请求启动前、LLM 返回后检查 runtime generation，读档后结果不会继续流向 facade；
- 请求结束释放 linked `CancellationTokenSource`；
- 协调异常隔离为 `NonRetryableFailure`，不执行 ActionPlan。
- 通过 `LegacyInteractionPipelinePorts` 让三渠道共享同一组实现端口。
- `InteractionResultCommitter` 将 ActionPlan 复核/执行与 Memory/AFEF 提交固定在主线程边界；stale、取消和失败结果不会进入提交。

## 刻意未做

- 没有修改 `ShoutBehavior`、`MyBehavior`、`CourierDeliveryBehavior` 的现有调用点；
- 没有把任一渠道的旧规则/Prompt/tag parser 假装成已经接入；必须由对应入口在主线程捕获后显式提供 ports；
- 没有把 `RuntimeConfigSnapshot` 设为 `DuelSettings/MCM` 的实际 authority；
- 没有在后台持有 `Hero`、`Agent`、`Campaign`、Courier session 或其他 live 对象；
- 没有把取消/stale 结果写入历史、AFEF 或游戏状态。

## 运行频率和性能

协调器只在一次 LLM 交互请求的生命周期内运行，不进入 Campaign/Mission tick。请求表按 `channel:sessionId` 建立，取消和清理为 O(1) 字典操作；每个请求创建一个 linked CTS，完成后立即释放。
