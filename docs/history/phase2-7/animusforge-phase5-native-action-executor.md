# 阶段 5 Native ActionPlan 执行器

## 目标

让 Native opt-in detached 管线的 `ActionPlan` 能复用现有
`ShoutBehavior.ApplyNativeConversationGameActionsCore`，而不是复制 Duel、Reward、Debt、Policy、WorldMap、Siege/GCCZ、Romance、Issue 或 AFEF 规则。

## 边界

- `LegacyNativeActionPlanExecutor` 只接收不可变 `ActionPlan` 和
  `GameInteractionSnapshot`，并支持由渠道 owner 提供的显式 action-family
  allowlist；默认 Native 仍使用兼容通配符。
- Native detached ports 在旧规则检索为空时使用 `native_conversation` 仅文本
  基线资格，使普通 Native 对话仍能生成回复；动作权限仍由调用方 allowlist
  和主线程 executor 决定。
- `ActionPlan.RawPostprocessId` 仅是后处理原文追踪值，执行前会用通配符协议解析器重新解析，并要求解析出的标签、顺序、目标和参数与已授权计划完全一致；多出的原文动作标签直接拒绝。
- `ShoutBehavior.CreateNativeConversationActionPlanExecutorForExternal` 只在交互边界捕获当前 Native 目标、Agent、召唤/带路目标和会话 token。返回对象只能由宿主在主线程 commit 回调使用。
- 真正执行仍由 `ApplyNativeConversationGameActionsCore` 负责，因此现有主线程目标校验、领域资格、动作副作用、生成的 AFEF 和通知保持单一权威入口。
- stale/cancel 在 `InteractionResultCommitter` 进入执行器前被拒绝；目标在 core 内再次复核失效时不会执行。
- 默认 `SubmitNativeConversationTextInternalAsync` 不变，未自动切换。

## 性能与安全

执行器每个 detached 交互最多做一次 raw 标签重解析和一次主线程 core 调用，不进入 Tick、不扫描全场 Agent、不做反射；Native 目标列表仍是当前旧入口的一项交互边界快照。后处理失败或动作拒绝只降级该交互，不能伪造确认事实。

## 验证

纯 runner 覆盖：精确计划执行、增加未授权 raw 标签拒绝、宿主异常隔离。真实 Bannerlord 目标、动作、AFEF、旧存档、网络和游戏内验证仍为 `NOT-RUN`，需要 opt-in 宿主接线后在不部署游戏目录的前提下验收。
