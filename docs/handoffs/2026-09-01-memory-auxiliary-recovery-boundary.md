# LOCAL-7-J：Memory auxiliary recovery 边界接续

日期：2026-09-01。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-7-J` 代码与离线验证完成，状态 **VERIFY**；阶段 7 仍为 VERIFY，阶段 8
  破坏性清理/default cutover 仍 BLOCKED。
- 基线 `d2f37a8a`，意图 checkpoint `3436d739`，实现提交 `84e92f80`。
- 收尾前远端 `origin/refactor/prepare-af-restructure =
  fc8c344e0734ee860ec4012fb29b09e61dbdb240`；实现提交后本地 ahead 31 / behind 0。
  本 handoff 所在 docs 提交后应为 ahead 32 / behind 0，以最终 fetch 为准。
- 没有 push、部署、启动游戏、读写真实存档、切默认入口、删除 facade、修改游戏目录、
  NEW-10 或 GCCZ；没有发送 QQ。

## 审计到的真实顺序

Legacy live 路径：

```text
MyBehavior.AppendExternalDialogueHistory
  → AppendDialogueHistory
  → AppendDialogueHistoryById
  → AppendDailyMemoryLineById
      → add Daily line
      → AttachPendingWeeklyMemoryMaterialTriggers
      → SaveDailyMemoryDraftsById
      → PlayerNotorietyBehavior.NoteConversationLineForExternal (LLM only)
  → publish Recent history
```

Detached/refactor facade 不 literal 调这条 public 链；
`MyBehaviorMemoryFacade.Commit` 直接进入
`CommitExternalDialogueHistoryRecoverable`，再由 H journal 发布 Daily/Recent。

关键事实：

- `_pendingWeeklyMemoryMaterialTriggers` 只在进程内存在，load 会清空；trigger 只有
  memory/day/scene/dialogue，缺 request/recovery/turn/action-outcome 身份。
- weekly Mark 位于 legacy postprocess completion，早于 action owner 终态；它是 raw
  candidate，不是 `ConfirmedEffect` receipt。Detached postprocess 只取 work item 的 prompt，
  不调用该 completion closure，因此当前 detached 根本没有 weekly candidate owner。
- Notoriety 的 external API 返回 void 并吞错；active `LineCount`、session roll 身份与
  negative outcome 不持久。positive roll 会立即写 aggregate
  `KnowsMajorHistory/KnownAtDay`，finalize 再写 session count/bonus/last day；两处都没有
  per-line/session stable key/readback。

## 本轮改动

`MyBehavior.MemoryRecovery.cs`：

1. H 的 Daily recovery writer 现在只发布 core Daily line/marker；不再 Attach weekly，
   也不再从 recovery tick 调 Notoriety。
2. H 对 pending weekly list **既不读取/附着，也不删除**。没有 exact owner 身份时，
   删除也可能误删同 scene 多轮或 Courier loose 同 NPC/同日的另一条合法 legacy candidate。
3. 只在 `BeginStatus.Began + 本次调用内 core Completed + prepared/current recoveryId一致`
   时进入一次 current-runtime Notoriety best-effort 边界。
4. user/assistant 还必须存在 exact Daily marker：recovery ID、overall payload hash、part
   全匹配；AFEF、blank、未发布 line 不计，同一 part 最多一次。
5. Notoriety 异常与已完成 core receipt 隔离。结果只记
   `attempted_unconfirmed / NOT-RECOVERABLE`；`ExistingPending`、Duplicate、load、tick
   永不补写。

`tools/ProductionOptInEntryReplayTests/MemoryRecoveryProductionReplay.cs`：

- 加入 final production DLL 的 reflection/decoded-IL 结构守卫：H writer 不调用
  weekly/Notoriety，legacy writer仍保留原 Attach/Note，recoverable entry 必须经过
  `Began` gate、exact marker readback和异常隔离。
- 覆盖所有非 `Began` 状态、未完成、ID mismatch、user/assistant、AFEF/blank及
  marker hash/part mismatch。
- 这是结构/owner-contract证据，不是 live Notoriety fault/mutation/save-load 测试。

没有修改 `InteractionMemoryRecoveryLedger`、seed/components、schema、payload hash、wire、
`_af_interactionMemoryRecovery_v1`、Courier `AFCI1` 或任何 SyncData key/type。因此 I 绑定的
MyBehavior payload hash 和既有 H/I pending/completed receipt 不变。

## 验证

证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\memory-aux-boundary-20260901-141201-final`

关键 PASS：

- Memory recovery contract：6 ordered steps、12 writer sentinel fault scenarios、
  64 pending / 512 tombstones、zero action replay。
- InteractionPipeline 40；Detached Host boundary 69（三渠道）；request receipt 39。
- Courier inbound completion contract PASS。
- Economy-aware / Economy port、Production Courier/Detached/Configured Host、
  Economy-aware commit、Economy owner 全 PASS。
- Production OptIn：memory auxiliary boundary、Began gate、marker readback、H/legacy
  call isolation PASS，`assertions=3552`；Courier completion `assertions=87`。
- Persistence chunk PASS；Profile PASS：95 literal / 121 typed / 8 types /
  39 flattened / 41 symbolic；Identity PASS：99 SyncData、35 behaviors。
- Debug 与 Release 的 1.3 / 1.4 / Bootstrap 六项全部 0 warning / 0 error。
- 两名独立只读复核：本 diff `P0=0 / P1=0`。

有一次误调用不存在的 standalone
`ProductionDetachedCommitBoundaryReplayTests.csproj`，属于命令选择错误，不是产品测试失败；
真实 69-case suite 位于 `InteractionPipelineContractTests`，最终已 PASS。manifest 将该记录
单列在 `invalidInvocation`，不列入有效 exit-code 证据。

Stage SHA-256：

```text
Debug Bootstrap  EF5C3E17B10E07F02B4D6A10E7DF58679048CC94FA1F138619A56337221061AE
Debug 1.3        786053FCB34BC4CE43C3F6FFC289C67040F622A67A9D87EFA4D060976D379213
Debug 1.4        F230362A47F35B11E70BCAB4998CCE5C1D5B453134C3D2B7A827BA8CF337D53C
Release Bootstrap 9F03E4B015674A6951715EE95D0DCA143E947659AC132F323797F5B042960C95
Release 1.3       237E453A1BA108C6ECC3A37D189FD994284A34006A6F0860048B3A14166F3222
Release 1.4       A860FB776ECE257D1CB9ABE8DD2416A9DEE33A77D79F96F02D9138BFD7891B89
```

## 仍未验证 / 既有风险

- Legacy weekly candidate仍在 action outcome 前创建且无 exact turn/request 绑定，可能跨轮；
  detached 尚无 weekly owner。J 只阻止 H 伪恢复，**没有修复 weekly 三渠道对齐**。
- Notoriety 没有 durable per-line/session receipt；本轮只保留 brand-new synchronous
  current-runtime best-effort，不能跨 pending/load/finalize 补写。
- 已持久化 draft/block 的 trigger stable key只证明 legacy candidate被收录，不证明对应
  ActionPlan 已成功；不得用它反推 `ConfirmedEffect`。
- 真实 Campaign/Mission、Notoriety mutation/随机 roll/finalize、Courier 地图往返、
  live Economy/AFEF、真实 save/load/reload 与旧存档均 NOT-RUN。
- H→I 中间存档、默认入口、GitHub push、QQ handoff 的既有门禁保持不变。

## 回滚

- 只回滚本切片实现：在干净工作树上正常执行 `git revert 84e92f80`；禁止 hard reset、
  rebase 或 force push。
- 本 handoff/台账为后续独立 docs 提交；完整撤销 J 时再对该 docs 提交执行普通
  `git revert`。
- 不要连带回滚 H `f6e5e694` 或 I `de3220b7`。J 没有改其 wire/hash/key/type。
- 本轮未部署，所以没有游戏目录 DLL/PDB/ModuleData 回滚动作。
- 回滚 J 会恢复 H recovery 中对 transient weekly/Notoriety 的旧 best-effort调用，
  同时恢复其跨 tick/load 无证据副作用风险。

## 下一精确切片

`LOCAL-7-K`：outcome-aware weekly exact-intent owner。

1. 先从 `LegacyNativeActionPlanExecutor` 与三渠道 action-owner factory 画出 full-success、
   known partial、UnknownAfterStart、NoConfirmedEffect 的逐项证据；不得用 raw tag、总 count
   或 H marker推导某个 action成功。
2. 设计独立、data-only、bounded candidate/terminal receipt，绑定 request/trace/channel/
   session/subject/turn/action fingerprint；不进入 H seed/wire，不进入 `AFCI1`。
3. 首个实现只允许能证明全部 action终态成功或 owner明确给出 confirmed subset 的路径；
   partial无法逐项映射、unknown、rejected 一律 `NOT-RECOVERABLE`，不 Attach weekly。
4. 保留 legacy default；完成 focused/production/Profile/Identity 与六 Stage 后再评估三渠道
   opt-in，真实 Host前不切默认。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取 `AGENTS.md`、项目 `animusforge-maintainer` Skill、公共台账和 `docs\handoffs\2026-09-01-memory-auxiliary-recovery-boundary.md`。确认 HEAD 至少包含 `84e92f80`，fetch 但不要覆盖本地历史。按 `LOCAL-7-K` 从 `LegacyNativeActionPlanExecutor`、`InteractionResultCommitter` 与三渠道 action-owner factory 审计 weekly candidate 的 request/turn/action fingerprint和终态 effect证据；建立独立 data-only owner，绝不改 H/I wire、绝不从 raw/partial count/Unknown 合成成功、绝不重放 ActionPlan/Economy，也不切默认入口。完成后跑 focused + production replay、Persistence/Profile/Identity 与 Debug/Release 1.3/1.4/Bootstrap Stage；真实 Host/旧档无直接证据时阶段 7保持 VERIFY，最终门禁前不 push、不发 QQ。
