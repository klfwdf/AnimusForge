# Native 现有 Prompt 结果 parity 旁路

## 范围

本切片只把 Native 现有组装点的最终结果接入 detached parity 诊断，不切换默认运行路径，也不重新生成规则文本。

权威结果仍来自：

- `BuildSceneSystemTopPromptIntroForSingle`
- `BuildSceneUserRuntimeContextForSingle`
- `BuildSceneSystemRuleBlock`
- `BuildSceneCompositeUserBlock`
- `BuildStrictSceneMessagesForNpc`
- `BuildActionPostprocessSystemPrompt`
- `BuildSceneActionPostprocessUserPrompt`

`LegacyNativePromptParity` 只复制这些方法已经产生的字符串/旧 role-content 消息，构造 detached sections，对比消息数量、角色、内容顺序，并在日志中只记录 SHA-256 摘要，不记录 Prompt 正文。

## 启用方式

默认关闭。仅在主线程/交互诊断窗口明确调用：

```csharp
ShoutBehavior.SetNativeConversationDetachedPromptParityLoggingForExternal(true);
```

启用后，Native 主回复组装完成时记录 main parity；后处理最终 system/user 块组装完成时记录 postprocess parity，并汇合为同一 `DetachedInteractionPromptSections`。任一 parity 诊断异常都 fail-open，继续使用原 Native 主回复和原后处理调用。

## 边界

- 不复制或改写 `RuleBehaviorPrompts` / `ActionPostprocessPrompts`。
- 不把 raw reply 或 ACTION 标签注入主回复 Prompt。
- 不让 Hero、Agent、Campaign、Session 或凭据进入 detached sections。
- 不执行 ActionPlan，不写 AFEF，不改变历史、SyncData、存档类型或 TTS。
- parity 只在显式启用时运行，不进入 ApplicationTick/EngineTick 热路径。
- 真实 detached provider/action executor 和默认 Native 切换仍待后续切片；诊断失败的回滚点是原方法调用。

## Opt-in runner

`LegacyNativeConversationOptInRunner` 现在提供显式的 Native detached 生命周期；可通过 `ShoutBehavior.CreateNativeConversationOptInRunnerForExternal` 创建：

1. 宿主在交互边界完成 `Capture`；
2. runner 通过 `LegacyNativeConversationFacade` 执行 detached 三阶段 Generate；
3. 宿主通过 `commitOnMainThread` 回调在游戏主线程完成目标复核、ActionPlan 执行和 Memory/AFEF 提交；
4. detached 生成或提交基础设施失败时调用旧 Native fallback。

取消/stale 结果不会自动重试旧路径，避免存档代际变化后重复产生副作用。该 runner 仍是显式 opt-in，不会自动接管 `SubmitNativeConversationTextInternalAsync`。

`ShoutBehavior.CreateNativeConversationDetachedPortsForExternal` 可创建共享 detached ports：规则选择复用现有辅助规则检索，Prompt 使用共享 composer，后处理使用共享 postprocess composer，动作解析使用显式 allowlist。tag allowlist 由渠道宿主提供，空 allowlist 不产生可执行 ActionPlan，避免旁路扩大权限。

## Fixture 与验证

fixture：`docs/fixtures/phase5-native-prompt-parity/native-message-order.json`

它固定 Native 主链路的 `system → prefix → history → suffix` 顺序、后处理的 `system → user` 顺序、已记录输入去重和 atomic bundle 安全边界。
