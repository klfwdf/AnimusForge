# SceneShout 单目标 detached ports

## 本切片

`ShoutBehavior.BuildSceneShoutDetachedPromptSectionsForExternal(...)` 在场景
交互边界使用现有单 NPC 场景喊话组装 helper，复制为不可变的 main 和
postprocess sections：

- `BuildSceneSystemTopPromptIntroForSingle`
- `BuildSceneUserRuntimeContextForSingle`
- `BuildSceneMechanismPromptSection`
- `BuildSceneSystemRuleBlock`
- `BuildStrictSceneMessagesSystemPrompt`
- `BuildSceneActionPostprocessUserPrompt`
- 当前场景公共历史、持久记忆和已配置的 ActionPostprocess 模板

`CreateSceneShoutRefactorFacadeForExternal(...)` 是显式 opt-in 入口。它固定
当前目标 Agent 的交互边界，之后 detached 管线只接收 `InteractionEnvelope`
中的字符串、ID 和复制后的历史消息。默认 SceneShout 入口没有切换。

`CreateSceneShoutDetachedPortsForExternal(...)` 复用共享 composer；当旧规则
检索没有返回玩法规则时，使用 `scene_shout` 作为仅文本生成的基线资格，避免
普通对话被错误跳过。它不向后处理 allowlist 添加动作能力。

`SubmitSceneShoutRefactorOptInForExternalAsync(...)` 将该 facade 接入共享
`DetachedInteractionHost`，并通过现有 SceneShout 主线程队列执行 commit；它
只允许显式 opt-in 调用，失败仍可回退旧入口。

## 动作边界

本切片不把 SceneShout 的巨大旧动作队列复制成第二套执行器。现在提供显式
`CreateSceneShoutActionPlanExecutorForExternal(...)`，调用方仍须：

1. 提供带明确 allowlist 的 `LegacyInteractionPipelinePorts`；
2. 在主线程 commit 回调中复核当前 Agent/目标状态；
3. 使用该 executor 在主线程执行动作和写入 AFEF；executor 会重新解析当前
   Agent，核对 captured candidate 的稳定身份，并复用既有 mood/direct/follow
   入口。

因此，detached 结果的 `ActionPlan` 不能单独视为已执行；stale、取消、目标
失效和 ActionPlan 验证失败都不得执行动作或确认事实。Courier 时序、旧
SceneShout 规则和旧 fallback 保持不变。

## 性能与回滚

该 builder 只在显式 opt-in 的一次交互 capture 运行，不进入 Tick；它会对
当前在场 NPC 做一次边界快照，并复用现有场景 prompt helper。它不会发起主
回复或后处理 Gateway 请求，但现有 `BuildShoutPromptContextForExternal` 仍
可能按当前配置执行既有的前处理规则检索，因此默认旧路径不会受到影响，且
后续应把该检索进一步移到 detached 字符串端口。回滚点是继续
使用原 `ProcessShoutConfirmedInternal` / `HandleGroupResponse` 路径；本切片
未修改默认调用点、SyncData、存档类型、程序集身份或构建/部署脚本。

## 当前未验证

- 真实 SceneShout detached HTTP 请求和游戏内 opt-in 调用；
- SceneShout ActionPlan 的完整游戏内执行和 AFEＦ 写入；
- 旧存档与实机三渠道端到端验收；
- Courier 的真实 detached HTTP 和游戏内 prompt/action 验收。

