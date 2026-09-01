# LOCAL-7-K：Weekly exact-intent / outcome owner 接续

日期：2026-09-01。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-7-K` 代码与离线/compiled 验证完成，状态 **VERIFY**；阶段 7 仍为 VERIFY，
  阶段 8 破坏性清理/default cutover 仍 BLOCKED。
- 基线 `da15241f`，意图 checkpoint `7cdf6435`，实现提交 `765b2386`；本 handoff
  与公共台账的独立 docs 主提交为 `101fc0fd`。
- 本切片只接受 **Economy-only + whole-plan full owner confirmation**：mixed、known partial、
  `UnknownAfterStart`、rejected、fingerprint mismatch 或任何无法逐项证明的结果都不能 Attach
  weekly material。
- 最终 manifest 记录远端 `origin/refactor/prepare-af-restructure =
  fc8c344e0734ee860ec4012fb29b09e61dbdb240` 且 `pushed=false`；实现提交时本地 ahead 35 / behind 0，
  以最终 fetch/status 为准。
- 没有 push、部署、启动游戏、读写真实存档、切三渠道默认入口、修改游戏目录、NEW-10 或
  GCCZ；没有发送 QQ。

## 审计到的真实顺序

新的 sidecar 顺序是：

```text
InteractionResultCommitter.Commit
  → canonical Economy-only candidate projection
      （不调用 injected gameplay planner）
  → owner.Prepare
      → durable ProbeExistingCandidate
          → same candidate: Duplicate，commit fail-closed
          → same receipt / different candidate: Conflict + quarantine，commit fail-closed
          → NotFound only: freeze live payload + persist Prepared
  → CommitOnce
      → injected Economy planner exactly once
      → Economy port executes the actual whole plan
  → compare actual execution fingerprint with canonical candidate fingerprint
  → owner.Complete
      → exact full success only: Prepared → Confirmed
      → partial / unknown / rejected: terminal non-publish state
  → Confirmed-only frozen data Attach + exact readback
  → Confirmed → Applied
```

关键门槛：

- Candidate source 只接受非空、全 Economy、无 exclusion 且 canonical projection action count
  与 typed `ActionPlan.Actions` 一致的 whole plan。canonical projection 使用独立、无 gameplay state 的
  `LegacyEconomyRewardDebtAdapter`，不会提前消耗可注入 `_economyPlanner`；实际执行中的
  injected planner 每次 commit 只调用一次。
- `Confirmed` 同时要求 actual execution fingerprint 等于 candidate action fingerprint、
  `EffectState == ConfirmedEffect`、`AppliedActionCount == ActionPlan.Actions.Count`，且 core
  commit 为 `Executed + HistoryWritten + ActionsExecuted`。任一条件不成立都不会发布 weekly
  trigger。
- Durable `ProbeExistingCandidate` 位于 live hero/debt/item/value payload 重建之前。因此即使
  process-local `InteractionCommitReceiptCache` 已清空或驱逐，同一 request 仍先命中持久 receipt
  并 fail-closed，不会再次进入 `CommitOnce` 或重放 Economy。
- Attach 只消费 receipt 中冻结的 data-only payload；落盘后会重读并逐项核对 stable key、receipt/
  candidate/payload hash、turn/action fingerprint、memory/day/session/agent、foothold、tags、估值与
  reason，完全一致后才标记 `Applied`。

## 本轮改动

`Refactor\Runtime\WeeklyMemoryMaterialOutcomeReceipt.cs`：

1. 增加 request/turn/ordered-action canonical identity、frozen material payload 与状态机
   `Prepared → Confirmed → Applied`；`Rejected / Partial / Unknown / Quarantined` 都是不可发布
   终态。
2. 增加 bounded、data-only `WeeklyMemoryMaterialOutcomeLedger`：64 个 pending authority、
   512 个 terminal tombstone；`Prepared` 与 `Confirmed` 不驱逐，只裁剪最旧 terminal。
3. wire 使用独立 `AFWM1:`；只序列化 canonical identity、hash、状态、时间戳、诊断码与冻结
   payload，不序列化 raw tag、`ActionPlan`、intent/callback、executor 或任何可执行对象。
4. load 时 persisted `Prepared` 一律转为 terminal `Unknown`，绝不重放 gameplay；只有 persisted
   `Confirmed` 可以重新提供幂等的 data-only Attach。
5. 状态迁移时间使用 `max(previous-authority-time, current-clock)`；系统时钟回拨不会把合法
   `Confirmed → Applied` 或 quarantine 误判为时间倒退冲突。

`Refactor\Adapters\LegacyNativeActionPlanExecutor.cs` 与
`Refactor\Runtime\InteractionResultCommitter.cs`：

- 增加 canonical Economy-only candidate projection、single injected-planner execution、actual
  fingerprint 校验及 full owner-confirmed whole-plan 门槛。
- duplicate/conflict 在 core `CommitOnce` 前 fail-closed；sidecar 的 prepare/complete/publish 异常
  与 core 异常边界分开，绝不把 partial/unknown/rejected 合成为成功。

`MyBehavior.WeeklyActionOutcomeReceipts.cs`：

- 新增独立 symbolic flattened `Dictionary<string,string>` SyncData key
  `_af_weeklyActionOutcomeReceipts_v1`，通过既有 chunk helper 保存 `AFWM1` receipts。
- `ProbeExistingCandidate` 先于 live payload；只有 `Confirmed` receipt 可以 Attach，且 exact
  readback 后才 `Applied`。load/tick 只处理 data-only publish work，不保存或调用 action/callback。
- journal import 失败时保留原始 `_weeklyActionOutcomeStorage`，清零 load-confirmed/generation/work
  flag 并禁用 owner；后续 save 不用空 ledger 覆盖 corrupt raw。load/tick/publish 异常均局部隔离。

`MyBehavior.cs`、`MyBehavior.MemoryRecovery.cs` 与 persistence fixtures 只接入新的独立 K lifecycle/
binding。没有修改 `InteractionMemoryRecoveryLedger`、H seed/components/payload hash/wire、Courier
`AFCI1` 或既有 H/I SyncData key/type；没有修改 `ShoutBehavior`、`CourierDeliveryBehavior`、
`DuelSettings` 或三渠道 default route。

## 验证

最终证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\weekly-outcome-k-final-20260901-163221`

关键 PASS：

- Weekly exact-intent pure contract：fingerprint v1、13 identity fields、direction/order/hidden
  semantic、duplicate/conflict/payload mismatch、2 durable preflight、7 states、5 wire、4 capacity、
  2 atomic import、Prepared-load→Unknown、Confirmed retry、Applied idempotency、2 clock rollback、
  data-only 全 PASS。
- Economy-aware executor 与 Economy port 全 PASS；InteractionPipeline 40、Detached Host boundary 69
  （三渠道）、interaction commit receipts 39 全 PASS。
- Memory recovery、Courier inbound completion、AF/Foundation/GameAdapter contract 全 PASS。
- 8 个 production/compiled runner 全 PASS：Production OptIn、Economy-aware commit、Economy owner、
  Detached Host、Courier Host、Configured Host、Configured Chat Gateway 与 Courier inbound。
- Persistence Profile：95 literal / 121 typed / 42 symbolic / 40 flattened；Migration：10 cases、
  2 corrupt-journal-retained；Identity：99 SyncData、35 behaviors。
- Debug 与 Release 的 1.3 / 1.4 / Bootstrap 六项全部 0 warning / 0 error。
- 独立只读 source review 最终结论：`P0=0 / P1=0`。

有一次误调用不存在的 `AFContractsContractTests.csproj`，属于命令选择错误；真实 AFContracts
Python validator 已 PASS。不存在的 `ProductionDetachedCommitBoundaryReplayTests` 也只记为 N/A；
真实 69-case boundary suite 位于 `InteractionPipelineContractTests` 并已 PASS。两项都没有混入
有效 exit-code 证据。

Stage SHA-256：

```text
Debug Bootstrap   45302CFAE284B00868FBB4C5FEEB9E2B22326DF312CA5906A530AD7A6F34B393
Debug 1.3         C06B50A8A00AAA69DEFD04E5CF36E1704E27805E1AAD0D06D490398BA2134F2A
Debug 1.4         5E12DCD6B71062895949585EE4C1DBD911D1757CABDF15E247845A1B6B2F3721
Release Bootstrap 03D3457AE403F60E73C9F360B0CC42AF17C44B6EB73BCC56B97380FBAC5AE150
Release 1.3       3745C77AA1CC5D76E1CB14326A5E22662666B93DBB54F9D9D7D4278C0B9769C6
Release 1.4       EDB3FCDBC4B23624B88EF396DAA58DB03772CB37239FEDA06B9A5C1C5B434EAF
```

## 仍未验证 / 既有风险

- **compiled DLL、decoded IL、reflection guard 与 fixture/contract runner 不是实际 Campaign、真实
  save/load/reload 或 live Host/Economy 的证明。** 它们只能证明当前编译产物和受控输入满足
  contract。
- 真实 Bannerlord Campaign/Mission、live gold/item/merchant/debt、live AFEF、Courier 地图往返、
  旧存档迁移及真实存档的 corrupt-journal 保留/禁用行为均 NOT-RUN。
- K 故意不为 mixed plan 或 known partial subset Attach weekly；当前没有 per-action durable subset
  owner，不能把 applied count、raw tag、H marker 或 terminal draft 倒推出逐项成功。
- corrupt journal 会保留原始数据并禁用 K sidecar，但不会把可选 weekly owner 失败升级为 core
  Economy 成功/失败判断；在人工修复前不会生成 weekly material，且 K 的跨重启 durable duplicate
  保护不可用。fixture 尚未证明真实坏档中的用户可见降级流程。
- 三渠道 default cutover、游戏部署、GitHub push 与 QQ handoff 均未执行；阶段 7 仍为 VERIFY。
- Notoriety 仍没有 durable per-line/session receipt；positive roll、aggregate mutation、session
  finalize 与 load/retry 之间仍缺少 exact stable owner，这是下一切片而不是 K 的已解范围。

## 回滚

- 只回滚本切片实现：在干净工作树上正常执行 `git revert 765b2386`；禁止 hard reset、rebase
  或 force push。
- 本 handoff 与公共台账主提交为 `101fc0fd`；完整撤销 K 时再对该 docs 提交及其后续纯文档
  精度修正执行普通 `git revert`。
- 不要连带回滚 J `84e92f80`、H `f6e5e694` 或 I `de3220b7`。K 没有修改 H/I wire/hash/key/type。
- 本轮未部署，所以没有游戏目录 DLL/PDB/ModuleData 回滚动作。

## 下一精确切片

`LOCAL-7-L`：durable Notoriety per-line/session receipt 审计与实现，默认仍不切。

1. 先画出 `NoteConversationLineForExternal`、positive/negative roll、aggregate mutation、session
   line count/bonus/finalize 的真实调用顺序与已发生证据；不得用 void 返回、日志或随机结果猜测
   mutation 是否落地。
2. 为 exact line 与 session 建立稳定身份、terminal outcome/readback 与 bounded data-only receipt，
   明确 crash/load/retry 后哪些状态只能 Unknown、哪些数据发布可以幂等恢复；禁止重新 roll 或重复
   apply gameplay mutation。
3. receipt 必须与 H/I/K storage/wire 分离；异常 fail-closed，保留三渠道 default、legacy route 与
   现有 payload hash，不把 partial/unknown 合成为成功。
4. 完成 focused + production replay、Persistence/Profile/Migration/Identity 与 Debug/Release
   1.3/1.4/Bootstrap Stage；没有真实 Campaign/save/live proof 时阶段 7 继续 VERIFY。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取 `G:\AFMOD\AGENTS.md`、仓库规则、已安装的
> `af-siege-fusion` / `afmod-clean-code-guard` Skill、公共台账和
> `docs\handoffs\2026-09-01-weekly-exact-intent-outcomes.md`。确认 HEAD 至少包含 K 实现
> `765b2386` 与 K docs 主提交 `101fc0fd`；fetch 但不要覆盖本地历史。
> 按 `LOCAL-7-L` 先从 `PlayerNotorietyBehavior.NoteConversationLineForExternal`、line roll、
> aggregate mutation 与 session finalize 审计 exact per-line/session identity、已发生证据、
> readback 和 crash/load 边界，再实现独立 bounded data-only durable receipt。绝不重新 roll、重放
> gameplay mutation或从 void/log/partial/unknown 合成成功；绝不改 H/I/K wire/hash/key/type，绝不
> 切三渠道默认入口。完成后跑 focused + production replay、Persistence/Profile/Migration/Identity
> 与 Debug/Release 1.3/1.4/Bootstrap Stage；compiled/fixture 不能代替真实 Campaign/save/live
> proof，阶段 7保持 VERIFY，最终门禁前不 push、不部署、不启动游戏、不发 QQ。
