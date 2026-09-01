# LOCAL-7-I：Courier inbound 持久 completion 接续

日期：2026-09-01。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-7-I` 代码与离线验证完成，状态 **VERIFY**；阶段 7 仍为 VERIFY，阶段 8
  破坏性清理/default cutover 仍 BLOCKED。
- 干净基线 `0e276ce1`，意图 checkpoint `b5395164`，实现提交 `de3220b7`。
- 2026-09-01 收尾 fetch 成功：远端
  `origin/refactor/prepare-af-restructure = fc8c344e0734ee860ec4012fb29b09e61dbdb240`；
  实现提交后本地 ahead 28 / behind 0；本 handoff 所在 docs 提交完成后应为
  ahead 29 / behind 0，以收尾 `git rev-list` 为准。
- 没有 push、部署、启动游戏、读取/写入真实存档、切默认入口、删除 facade、修改
  游戏目录、NEW-10 或 GCCZ；没有发送 QQ。用户要求的 GitHub push + QQ handoff
  仍只在全部自主工作达到最终门槛时执行。

## 修复的问题

旧路径在 detached inbound memory commit 返回 `memory_recovery_pending` 时，
`DetachedInteractionHost` 正确地抑制 `afterCommit`。H 随后只能修复 Daily/Recent
memory，无法再次调用 Courier 的 completion；session 会保持
`ReplyGenerationStarted=true`，读档后旧 reset 又可能重开一次 LLM。

I 增加独立 **Courier owner**，没有把 callback 塞进 memory journal：

1. Inbound 专用 `IInteractionMemoryBatchCommitter` wrapper 在 inner memory owner 开始前
   持久化 `AFCI1` receipt。
2. Receipt 仍存于既有 `_af_courier_sessions_v1 : Dictionary<string,string>` 的
   `CourierSession` JSON，只新增一个 string 字段，不新增 SyncData key/type/Behavior。
3. Receipt 绑定 opaque recovery ID、MyBehavior owner payload hash、session、固定 inbound
   方向、sender、当前玩家 recipient、Courier party 和规范化 visible letter；有 payload
   hash、full-wire checksum、Pending/Ready/Applied/Quarantined lifecycle，支持 32,768
   Unicode 字符。
4. MyBehavior 只增加 internal prepare/status seam；status 必须同时匹配 recovery ID、
   subject 和 owner payload hash，防止相同 CommitId 的冲突 payload 借旧 tombstone完成。
5. 每次节流后的 Courier Campaign tick 用 runtime cursor 最多处理一条 actionable receipt。
   Pending 等 owner；Completed/Applied/Duplicate 才写 Applied tombstone、恢复
   `LetterText`、置 `ReplyGenerated=true`、清 `ReplyGenerationStarted`，再调用原
   `ProcessSessionById`。
6. Load 不会对非空 receipt 重开 LLM；delivery 还会独立校验 Applied + frozen letter。
   如果 legacy/坏档改写 projection，到达时会先 hold、下一 tick用 receipt修复，不能
   抢先交付。
7. Bad wire、Missing、Disabled、Quarantined、subject/payload/session/party/player mismatch、
   deterministic pre-owner rejection、意外 ActionPlan 导致 memory wrapper未调用、commit
   无 receipt，都会终止该 inbound Courier并释放等待暂停；不交付当前生成正文。

恢复 payload/tick 不包含或调用 ActionPlan、ActionRequest、executor、Economy、raw
postprocess 或 `afterCommit` callback，也不复用 outbound Economy owner 字段
`PostprocessConsumed`。Applied receipt随最终 session删除；session不存在即 Courier
owner强终态，不是全局可复用 tombstone。

## 代码与测试

核心文件：

- `CourierDeliveryBehavior.cs`
- `CourierDeliveryBehavior.InboundCompletion.cs`
- `MyBehavior.MemoryRecovery.cs`
- `Refactor/Runtime/InteractionMemoryRecoveryLedger.cs`
- `Refactor/Runtime/CourierInboundCompletionReceipt.cs`
- `Refactor/Runtime/CourierInboundCompletionCommitCoordinator.cs`
- `tools/CourierInboundCompletionContractTests/`
- `tools/ProductionOptInEntryReplayTests/CourierInboundCompletionReplay.cs`

红测先在旧 Debug 1.4 Stage 复现：缺少
`AnimusForge.Refactor.Runtime.CourierInboundCompletionReceipt`。最终证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\courier-inbound-completion-20260901-124752`

关键 PASS：

- Courier completion contract：arm-before-memory、inner throw、pending、Applied、Duplicate、
  conflict/mismatch、checksum、32K Unicode、owner status、data-only payload。
- Memory recovery contract：六步骤、12 fault、64/512 cap、retry/quarantine、long Courier、
  zero action replay。
- Interaction pipeline 40；Detached Host boundary 69（三渠道）；request receipt 39。
- Production Courier/Detached/Configured Host、Economy-aware、Economy owner 全 PASS。
- Production OptIn：Memory recovery 3526 assertions；Courier completion 87 assertions，
  覆盖真实 session JSON、旧 JSON、load gate、Applied crash-window恢复、fail-closed、
  pending轮转和每 tick一条。
- Persistence chunk PASS；Profile PASS：95 literal / 121 typed / 8 types /
  39 flattened / 41 symbolic；Identity PASS：99 SyncData、35 behaviors、added/removed均空。
- Debug 与 Release 的 1.3 / 1.4 / Bootstrap 六项 Stage 全部 0 warning / 0 error。
- 三名独立只读复核最终均未报 P0/P1。

Stage SHA-256：

```text
Debug Bootstrap 405B5FFF0FC3465407C237BADDCC3BAA83AB507E4C3B7A2607BA23860A91098B
Debug 1.3       BD4BF331B6E58901EFEFFE03BAAE387ECC29315C139C8AE79C13782744040D12
Debug 1.4       EED57D76D34A7E03669BC291FA51822C979C538F058C1628FA5B966FFABCB697
Release Bootstrap 8F5F629EFC80C0F83007674F37EB28DFDEAE2B0F931CBFDC37A94247E80C9089
Release 1.3       06EE39D154A8DC7363FD76782EE0AD911FBC0386473CD45B854B382BAD1DE344
Release 1.4       57E5BA46E7CEA5BD55993F1B05CD93EC9112E242C0FEA12C3AB0EE3B61D3D13D
```

## 仍未验证 / 风险

- 默认 inbound 尚未调用 detached Submit。未来切换必须在
  `BeginInboundLetterGenerationOnMainThread` 的单一 generation seam **替换** legacy，
  不能并行启动两套 LLM；本轮没有切换。
- 真实 Bannerlord Campaign/Mission、Courier 地图往返、暂停恢复、live AFEF、真实
  save/load/reload 和旧存档均 NOT-RUN。
- H→I 之间已产生的“pending memory、无 `AFCI1` receipt”中间存档没有持久 visible
  reply，I 无法安全反推；它仍可能按旧逻辑重新生成。不得伪称该历史窗口已迁移。
- H 只覆盖 core Daily/Recent projection；weekly material/notoriety 等辅助副作用仍是
  best-effort，尚无 durable terminal receipt/exactly-once 证据。
- GitHub/QQ 最终门禁未满足，因此当前不要 push、不要发送最终 HANDOFF。

## 回滚

- 只回滚本切片生产/测试实现：在干净工作树上正常执行
  `git revert de3220b7`；禁止 hard reset/rebase/force push。
- 本 handoff 与台账是后续独立 docs 提交；如需完整撤销 I，再按 `git log` 对该 docs
  提交执行一次普通 `git revert`。
- 不要连带回滚 H 的 `f6e5e694`：I 依赖其 status owner，但 H 本身也是其他渠道的
  memory-only 修复。
- 旧实现会忽略 `_af_courier_sessions_v1` JSON 内新增的 string 字段；本轮未部署，
  所以没有游戏目录 DLL/PDB/ModuleData 回滚动作。
- 含 in-flight `AFCI1` 的测试存档若回退到 I 之前代码，旧 load reset 可能重新走
  legacy generation；回滚验收应使用 pre-I 存档副本，或先明确终止该测试 session。

## 下一精确切片

`LOCAL-7-J`：先审计 weekly material/notoriety 等 H 未覆盖的 memory 辅助副作用。

1. 从 `MyBehavior.AppendExternalDialogueHistory` → `AppendDialogueHistory` →
   `AppendDialogueHistoryById` → `AppendDailyMemoryLineById` 的真实顺序，重点审计
   `AttachPendingWeeklyMemoryMaterialTriggers`、`SaveDailyMemoryDraftsById` 与
   `PlayerNotorietyBehavior.NoteConversationLineForExternal`，找出 owner、可读回证据和
   failure window，先写矩阵/红测。
2. 只为“动作已经终态、但辅助记录缺失”的可证明项设计 terminal receipt；不能重放
   ActionPlan/Economy，也不能把未知效果合成成功。
3. 保留 H 的 `_af_interactionMemoryRecovery_v1` core payload边界；若 auxiliary owner
   不能安全幂等，就明确 NOT-RECOVERABLE 并提供验收/降级，而不是塞入 core tick。
4. 重跑 focused、production、Profile/Identity 和双配置六 Stage；真实 Host无直接证据
   时继续 VERIFY。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取 `AGENTS.md`、项目 `animusforge-maintainer` Skill、公共台账和 `docs\handoffs\2026-09-01-courier-inbound-completion.md`。确认 HEAD 至少包含 `de3220b7`，fetch 但不要覆盖本地历史。按 `LOCAL-7-J` 从 `AppendExternalDialogueHistory` → `AppendDialogueHistory` → `AppendDialogueHistoryById` → `AppendDailyMemoryLineById` 的真实顺序审计 H 未覆盖的 weekly material/notoriety 辅助副作用和 owner/failure window，重点核对 `AttachPendingWeeklyMemoryMaterialTriggers`、`SaveDailyMemoryDraftsById`、`PlayerNotorietyBehavior.NoteConversationLineForExternal`；先 checkpoint/红测，只补可证明幂等的 terminal receipt，绝不重放 ActionPlan/Economy、绝不把未知效果合成成功，也不切默认入口。完成后跑 focused + production replay、Persistence/Profile/Identity 与 Debug/Release 1.3/1.4/Bootstrap Stage。真实 Host/旧档无直接证据时阶段 7保持 VERIFY；全部自主工作达到最终门槛前不 push、不发 QQ。
