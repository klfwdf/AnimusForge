# 阶段 5 三渠道 detached host

`Refactor/Runtime/DetachedInteractionHost.cs` 是 Native Conversation、SceneShout 和 Courier 共用的生命周期编排层。它不拥有任何渠道玩法，只负责：

1. 在渠道交互边界 capture immutable `InteractionEnvelope`；
2. 在异步 Generate 前创建渠道自己的 memory/action facade，避免目标漂移；
3. 通过渠道提供的 dispatcher 回到主线程提交；
4. 统一 stale/cancel/validation 拒绝语义；
5. 只在 detached 基础设施失败时使用旧入口 fallback。

host 支持由渠道 owner 提供 `afterCommit` 回调，但只在 commit 状态为
`Succeeded`/`Executed` 时调用，且回调发生在渠道的 dispatch 委托内部；因此
Courier inbound 可以在主线程成功写入 assistant 历史后推进既有 session 送达
状态，而 stale、取消、验证拒绝和基础设施失败不会伪造送达事实。`appendPlayerInput`
由渠道明确控制，NPC 主动来信使用 `false`，避免把生成 seed 写成玩家发言。

Native 同时提供 `ShoutBehavior.SubmitNativeConversationRefactorOptInForExternalAsync`，将该 host 与现有 `ApplyNativeConversationGameActionsCore`、`MyBehaviorMemoryFacade` 连接。SceneShout/Courier 仍由各自 owner 保留目标和送达时序，后续只需提供各自 prompt/action ports。

该 host 每次交互只执行一次 capture、一次 detached pipeline 和一次 commit dispatch，不进入 Tick，也不扫描或反射游戏对象。真实三渠道网络、实机、旧存档和完整 AFEF 验证仍未完成；默认运行路径保持原样。
