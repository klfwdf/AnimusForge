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
- Inbound detached host 使用 `appendPlayerInput: false`。`LOCAL-7-I` 后由 inbound
  专用 batch memory wrapper 在 MyBehavior owner 开始前，把 `AFCI1` completion
  intent 写入既有 `_af_courier_sessions_v1` session JSON；receipt 绑定 owner
  recovery payload hash、session/sender/current-player/party 和冻结 visible letter。
  初次 owner 成功仍可在原主线程 callback 立即完成；memory pending、duplicate 或
  callback 丢失则由 Courier tick 在 payload-matched Completed 后幂等补
  `LetterText`、`ReplyGenerated`/生成中标记并调用既有 `ProcessSessionById`。
  信件发放仍只由 `DeliverInboundLetterToPlayer` 负责。
- load/delivery gate 会阻止带 Pending/Ready/坏 receipt 的 session 重开第二次 LLM
  或交付未确认正文。每个 Campaign tick 最多处理一条 actionable receipt；坏 wire、
  Missing/Disabled/Quarantined/PayloadMismatch、pre-owner rejection 或 commit 无 receipt
  会终止该 inbound Courier 并释放等待暂停，不重放 Memory commit、ActionPlan、
  Economy 或 postprocess。Outbound 的 `PostprocessConsumed` 不参与此 receipt。
- ActionPlan 仍必须由 Courier 宿主在主线程做 allowlist、目标复核和执行，旁路
  结果不能直接视为已发生事实。

默认 capture/generation 仍未切到 detached；正常 legacy Courier 不进入 receipt
路径。只有存在未完成 `AFCI1` 的 opt-in session 才在节流后的 Campaign tick 轮转
检查；已完成且仍在路上的信件使用 flags 快路，不反复解码大 receipt。

## 未完成与回滚

契约 runner 已覆盖 host commit 回调、inbound seed 历史隔离、stale/rejected 隔离、
arm-before-memory、inner throw、owner outcome、session JSON/load/Applied 恢复、
fail-closed 和 one-per-tick；真实 detached HTTP、Courier ActionPlan 完整游戏内执行、
旧存档和游戏内验收仍未完成。
失败时回滚点是继续使用原 Courier reply/inbound generation 方法；未修改
SyncData key/type、存档类型、程序集身份、送达时序、构建脚本或游戏目录。
