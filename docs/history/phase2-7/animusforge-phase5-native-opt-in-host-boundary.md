# Native Conversation opt-in 宿主接线边界

## 目的

本切片只把 Native Conversation 接到重构运行时的宿主边界，不切换现有默认路径。生产宿主现在可以显式创建 `LegacyNativeConversationFacade`，在交互边界捕获 detached `InteractionEnvelope`，再把它交给三阶段 coordinator。

## 生产入口

`ShoutBehavior` 新增两个 external API：

- `CreateNativeConversationRefactorFacadeForExternal(...)`
- `CaptureNativeConversationRefactorEnvelopeForExternal(...)`
- `CaptureNativeConversationRefactorConfigurationForExternal()`

创建和 capture 必须在 Native 对话目标仍有效的主线程边界执行。返回的 envelope 只含字符串、ID、历史消息和不可变事实，不携带 `Hero`、`CharacterObject`、`Agent`、`Campaign` 或 `Session`。

facade 的 `GenerateAsync(...)` 只接收 envelope、`RuntimeConfigSnapshot` 和 provider/module ID；配置快照只含 `legacy-shoutnetwork` 标识、模型元数据和超时，不含 API key、路径或凭据。`Commit(...)` 必须回到主线程，并由渠道 owner 注入 ActionPlan executor 和 Memory adapter。generation 复核发生在动作执行前。

`LegacyShoutNetworkGateway` 的 stage-aware 模式保留现有 provider 语义：`MainReply` 调用旧 `ShoutNetwork`，`Postprocess` 调用旧 `AIConfigHandler` 动作后处理的一次性非交互入口。后处理失败只返回管线失败状态，不弹出旧的阻塞重试窗口；重试由上层协调器决定。

`LegacyChannelInteractionFacade` 是三渠道共用的生命周期实现；Native facade 只是兼容命名包装。场景喊话和信使可以注入各自的 capture delegate 与 ports，继续保留各自 UI/时序，但不再复制请求取消、generation/stale 和主线程提交代码。

`MyBehaviorMemoryFacade` 不再保存 live `Hero`，只保存稳定 HeroId；在交互边界的 `Read/Append` 调用时解析目标。Hero 解析仅发生在交互读写，不进入 tick 热路径；目标失效时不把 Hero memory 错当作 non-Hero memory。

`LegacyDetachedRuleSelector` 已接入现有辅助规则检索 API 的 detached 边界。它只读取玩家输入、secondary/context 字符串和显式排除列表，输出去重后的 `RuleSelection`；Native snapshot 已预留这些事实字段。该 selector 目前只提供 opt-in composition，旧渠道规则资格与调用路径不变。

`LegacyPromptPackageAdapter` 已建立旧 `List<object>{role,content}` 与不可变 `PromptPackage` 的双向转换，并由 legacy gateway 使用；无效 role 归一为 `user`，空消息丢弃，输入集合被复制。该转换是 Prompt port 的共享基础，不等于 Native/场景/信使已经切换其完整 Prompt 组装逻辑。

`DetachedPromptSections` 与 `LegacyDetachedPromptComposer` 已建立共享 detached Prompt 组装边界。渠道 owner 在主线程/交互边界调用现有权威 helper，生成稳定的 system、prefix user 和 suffix user 字符串块，再连同已复制的历史放入 `InteractionEnvelope`。composer 只按场景喊话既有的消息顺序复制为 `PromptPackage`：system → prefix user → history → suffix user → 当前玩家输入；它不读取 Bannerlord 对象、不重建提示词、不注入 ACTION 标签，也不负责规则资格、AFEF 或动作执行。

Native、SceneShout 和 Courier 的 snapshot adapter 现在都提供 `DetachedPromptSections` overload。默认 overload 仍使用空 sections，因此现有入口行为不变；当渠道 owner 已在交互边界完成既有 Prompt helper 组装时，可把相同契约交给共享 composer。`appendCurrentPlayerInput` 由渠道明确设置：若当前输入已进入渠道历史则关闭，防止重构旁路重复写入。

`LegacyActionTagParser` 已建立 detached ACTION 标签解析边界：只解析允许的 tag family，限制动作数量，复制 target/参数并保留 raw postprocess trace；不解析或执行任何 Bannerlord 对象。动作执行仍必须回到主线程渠道 executor。

该 parser 现在覆盖已有协议族 `ACTION`、`A`、`AD`、`ADP`、`ASS`、`GUI`、`ATT`、`ATP`、`RELAY`、`FOL`、`STP`、`END`。`ACTION` 和 `A` 保留旧的动作名/目标/参数拆分，`A:H_J_P_P_C&L` 只授权有限的 `C`/`L` 具体实例；`AFEF`、`CONTENT` 和其他正文/事实标记不会被误当作 ActionPlan。该扩展仍是 detached opt-in 解析，不改变默认执行器。

`DetachedPostprocessPromptSections` 与 `LegacyDetachedPostprocessPromptComposer` 已补齐后处理 Prompt port。渠道 owner 可在交互边界把现有 `{tag_rules}`、mood rules、history/AFEF、runtime target facts 和最新回复块复制为 sections；composer 只冻结为 `system → prefix user/history → suffix runtime facts → latest visible reply`，不把 raw reply 或动作标签泄露给可见正文，也不重新生成规则。

## 保持不变

- `SubmitNativeConversationTextInternalAsync` 及 NPC opening 入口仍是默认路径。
- `CaptureNativeConversationRefactorEnvelopeForExternal(string, DetachedPromptSections)` 与 `CreateDetachedPromptComposerForExternal(...)` 仅为显式 opt-in 旁路；未提供真实 rule/prompt/action ports 时不会启动新管线。
- `CreateNativeConversationRefactorFacadeForExternal(..., Func<string, DetachedPromptSections>)` 允许 Native 调用方在 capture 边界提供现有 helper 生成的 Prompt blocks；`CreateSceneShoutInteractionFacade(...)` 和 `CreateCourierInteractionFacade(...)` 让另外两条渠道复用同一 lifecycle facade。三个工厂都不替换默认入口。
- `DetachedInteractionPromptSections` 和对应 provider overload 将主 Prompt 与后处理 Prompt sections 作为一次 capture 的原子 bundle 传递，避免配置 reload/重复 capture 导致 `{tag_rules}`、AFEF 或 runtime facts 与主回复错配。
- SceneShout/Courier 的 `LegacyInteractionSnapshotAdapters.CaptureSceneShout(...)` / `CaptureCourier(...)` 也支持同一 sections 契约；这只是 capture 复用，不代表两条默认链路已经切换。
- 旧规则选择、Prompt 合成、后处理标签解析、动作执行、TTS、历史/AFEF 写入和 courier 时序没有被替换。
- 不改变程序集身份、SubModule、SyncData key/type、构建脚本或游戏目录。
- `RuntimeConfigSnapshot` 不携带 API key、凭据或本地路径；过渡 gateway 仍由旧 `ShoutNetwork` authority 处理配置。

## 当前限制与下一步

该入口本身不代表真实 Native 端到端迁移完成。真实 ports 仍需把旧 rule/prompt/action 实现按 detached 输入重新划分，并把所有 live 游戏对象解析和动作执行留在主线程。未完成前，不应把空 ports、测试 ports 或旧对象闭包接入生产默认路径。

## 验证

- InteractionPipeline contract runner：PASS，17 cases（含 channel-neutral facade 与 detached rule selector）。
- InteractionPipeline contract runner：PASS，22 cases（含 detached Prompt sections、后处理 sections、atomic bundle、协议族 allowlist、canonical 顺序和已记录输入去重边界）。
- Unified capture boundary：SceneShout/Courier overload 已完成编译验证；真实游戏对象捕获、真实 Prompt helper 对照和三渠道端到端仍 NOT-RUN。
- Persistence/Profile/Config runner：PASS，95 literal keys、9 namespaces、幂等迁移和 SafeMode fixture。
- 1.3、1.4、Bootstrap：各 0 warning、0 error。
- unified stage：PASS，输出到 `bin\Debug\single_module_stage\AnimusForge`，未部署。
- 真实 Native 网络、旧存档和游戏内三渠道：NOT-RUN。
