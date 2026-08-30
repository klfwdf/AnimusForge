# Courier detached Prompt/Action ports

## 已完成

Courier 的两个现有生成入口现在都有显式 detached capture 边界：

- `CreateCourierReplyRefactorFacadeForExternal(...)`
- `CreateCourierInboundRefactorFacadeForExternal(...)`
- `CaptureCourierReplyRefactorEnvelopeForExternal(...)`
- `CaptureCourierInboundRefactorEnvelopeForExternal(...)`

capture 在游戏主线程通过现有：

- `BuildCourierReplyGenerationRequestOnMainThread`
- `BuildInboundLetterGenerationRequestOnMainThread`
- `BuildCourierReplyMessages`
- `BuildInboundNpcLetterMessages`

取得已经组装完成的 legacy role/content 消息，再由
`LegacyPromptPackageAdapter` 复制成不可变 `PromptPackage` 和 history。这样
保留了信件身份、关系、位置、日期、记忆、规则、送达事实和当前信件/意图的
原有顺序，没有复制第二套 Prompt 规则。

reply 还通过现有 `TryPrepareCourierActionPostprocessForExternal` 捕获配置化的
后处理 system/user sections；inbound letter 保持无动作后处理。

## 运行与安全边界

- facade 仅显式 opt-in，默认 Courier 生成和送达/返回状态机不变；
- session、Hero 和库存/领域对象只在 capture/commit 交互边界解析；
- detached envelope 只携带字符串、稳定 ID 和复制后的历史；
- `CreateCourierDetachedPortsForExternal` 从 capture 的规则命中读取规则选择，
  不重复调用规则检索；
- Courier ports 始终加入 `courier_reply` 仅文本基线资格，因此普通回信不会因
  没有可选玩法规则而被共享管线跳过；这不授予任何 ActionPlan 标签权限。
- `SubmitCourierReplyRefactorOptInForExternalAsync(...)` 已接入共享
  `DetachedInteractionHost`，commit 通过 Courier engine tick 队列返回主线程，
  并带有 30 秒超时门闩，迟到回调不会再执行。
- `CreateCourierReplyActionPlanExecutorForExternal(...)` 只保存稳定 session ID；
  commit 时重新解析 session/recipient，核对渠道、session、subject、送达状态和
  目标存活状态，再复用既有 Courier 领域动作入口。
- Courier executor 关闭旧的重复历史写入，由共享 `InteractionResultCommitter`
  统一写入 user → assistant；动作领域自身产生的 AFEF/通知仍由原入口负责。
- Inbound detached host 使用 `appendPlayerInput: false`，并在成功 commit 的主线程
  回调中重新校验 session、方向和终止状态，再更新 `LetterText`、
  `ReplyGenerated`/生成中标记并调用既有 `ProcessSessionById`。这只推进原有送达
  状态机，不直接发放信件或写入新的存档事实；信件发放仍由
  `DeliverInboundLetterToPlayer` 负责。
- ActionPlan 仍必须由 Courier 宿主在主线程做 allowlist、目标复核和执行，旁路
  结果不能直接视为已发生事实。

该切片只在一次显式交互 capture 运行，不进入 Tick；当前 session 查找为一次
边界字典读取，未增加轮询或后台全量扫描。

## 未完成与回滚

契约 runner 已覆盖 host commit 回调、inbound seed 历史隔离、stale/rejected 隔离；
真实 detached HTTP、Courier ActionPlan 完整游戏内执行、旧存档和游戏内验收仍未完成。
失败时回滚点是继续使用原 Courier reply/inbound generation 方法；未修改
SyncData key/type、存档类型、程序集身份、送达时序、构建脚本或游戏目录。
