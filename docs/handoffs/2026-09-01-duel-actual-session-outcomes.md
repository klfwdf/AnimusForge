# LOCAL-7-M1：Duel actual-session outcome owner 接续

日期：2026-09-01。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-7-M1` 的代码与离线/compiled 验证完成，状态为 **VERIFY**；阶段 7 仍不能标为 DONE。
- 基线 `9955658b`，意图 checkpoint `fc3cd722`，实现提交 `16f3cbef`。
- 本切片建立的是**实际 Duel session 的进程内结果 owner**，不是某个 detached ActionPlan request 已导致 Duel 成功的证明。
- meeting、arena/local、wilderness 三条真实结算路径均接入 typed owner；成功绑定的 session 先锁定 `ResultIdentity`，再执行和记录 Memory、renown、stake/debt、death、UI 等分量结果。
- legacy detached Duel callback 不再返回玩法成功，而是终态 `UnknownAfterStart` / `duel.outcome_pending`；不得 fallback、重派或从标签推断胜负。
- 没有推送、部署或启动游戏，没有读写真实存档，没有切默认入口、删除 facade、修改 NEW-10/GCCZ、覆盖游戏或改 ONNX。

## 旧路径的决定性问题

1. `PrepareDuel(...)` 的兼容入口为 public `void`；legacy callback 返回只能证明调用结束，不能区分拒绝、排队、开始、取消或最终结算。
2. `_lastDuelResults` 只按 Hero 保存易失字符串，读取还会消费；它没有 request、trace、channel、session、runtime/save generation 或具体 Duel 身份。
3. 三条终态路径分别位于 `ArenaDuelMissionBehavior.EndDuelLocal`、`SettleWildernessDuelRuntime` 和 `DuelBehavior.EndDuel`，旧代码没有共同 receipt。
4. stake、deferred debt 和 after-lines 只按 Hero 暂存；若当前请求未启动，旧数据可能被同 Hero 的后续 Duel 消费。
5. Memory/AFEF facade 与部分死亡/规则调用是吞错 `void`；renown、stake 和 debt 也没有统一、可查询的分量状态。
6. `_duelCooldowns : Dictionary<string,float>` 是唯一 Duel 持久字段；旧存档不能恢复一个正在运行的 Mission，也不能安全重放死亡或资产转移。

## 本轮实现

### Pure typed owner

`Refactor/Runtime/DuelOutcomeReceipt.cs` 新增纯数据契约和线程安全 owner：

- request/start/result 三层不可变 identity，绑定 subject、runtime/save generation、channel/session、session kind、action/artifact fingerprint 和唯一 `DuelId`；
- 状态覆盖 Rejected、Queued、Started、OutcomeKnown、Completed、PartiallyCompleted、UnknownAfterStart、Cancelled；
- 分量状态覆盖 NotApplicable、Confirmed、Partial、AttemptedUnconfirmed、Unknown；
- 64 active / 512 retained 的有界进程内 owner；duplicate 幂等，身份冲突 fail-closed；
- `OutcomeKnown → UnknownAfterStart` 保留已知 `ResultIdentity`，不会因后续副作用异常抹掉胜负；
- 不保存 TaleWorlds 对象、callback、原始对话文本，不序列化，也没有 load/tick gameplay replay。

### Production host 与 readback

`DuelBehavior.Outcomes.cs` 为现有 partial `DuelBehavior` 提供：

- `TryBeginDuelOutcome`、`TryRecordDuelOutcome`、`TryFinalizeDuelOutcome`、`MarkDuelOutcomeUnknown`；
- 按精确 `DuelId` 查询和最多 256 subject / 512 queue entries 的 latest-by-subject 查询；
- 只有 active count 为 0 且 retained 达上限时才安全轮换 owner；host 的 serial + nonce `DuelId` 在进程内不复用；
- meeting、arena/local、wilderness actual-start 都调用 owner 尝试 reserve receipt；成功后才绑定 artifact，三条 writer 在各自 one-shot result guard 后立即记录结果，再处理副作用；reserve 失败则丢弃 unbound artifact、保持无 typed readback，绝不伪造成功；
- 异常时同一 receipt 转 Unknown，不再启动 Mission、死亡、转账、写 Memory 或再次 finalize；
- load 只把当前内存引用的 active receipt 标成 Unknown 并清引用，不开始/结束 Mission，不补 death、stake、debt 或 Memory。

### stake/debt/after-lines 精确绑定

- 只有同一回复同时含精确 `[ACTION:DUEL]` 才能 arm stake/after-lines；普通下注文本不能污染以后 Duel。
- 新回复替换旧的未开始 artifact；实际开始时把 unbound artifact 一次性绑定到具体 `DuelId`。
- 已绑定 artifact 不能改绑，terminal consumer 只消费期望的同一 `DuelId`。
- Fourberie/eligibility/open、queue timeout、encounter/mission/agent/team 等 pre-start 失败清理 unbound artifact；Completed/Unknown 清理匹配的 bound leftovers。
- artifact 内容只进入 action digest/fingerprint，不把原始 note/line 写入 receipt。

### Detached executor 边界

`Refactor/Adapters/LegacyNativeActionPlanExecutor.cs` 对 delegated Duel 返回：

```text
InteractionStatus.NonRetryableFailure
EffectState = UnknownAfterStart
ExecutionErrorCode = duel.outcome_pending
```

这是一条已开始边界后的终态未知，不允许 fallback/retry，也不生成 Duel 成功 fact。mixed plan 中已经由 Economy owner 确认的 count/facts 仍保留，Duel 未知不能覆盖已确认 subset。

## 身份、存档和兼容边界

- legacy live 路径当前使用明确标记的 `Domain / legacy-unbound` request/trace/session token；它只证明某个实际 Duel session 的生命周期，**不能**证明特定 detached ActionPlan request 与该 Duel 的因果关系。
- 下一切片 `LOCAL-7-M2` 必须从真实 detached dispatch 携带 request/trace/channel/session/action fingerprint，并绑定到同一 queued/started `DuelId`；不得倒推或伪造 provenance。
- 原 public `void PrepareDuel(...)` ABI、Fourberie optional seam、MCM/default route、Saveable ID 和 `_duelCooldowns` key/type 均保持不变。
- typed receipt 不入档、不可恢复；process restart 后只能 fail-closed，不能重放 Mission 或非幂等副作用。

## 验证

最终证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\duel-outcome-m-final-20260901-222417`

红基线目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\duel-outcome-m-red-20260901-211217`

红基线证明：public void Prepare、aggregate legacy Executed、Hero-only result、stake 先消费、三 terminal writers、无 typed owner。契约先改后还复现 mixed Duel 被错误提升为玩法成功。

关键 focused/production 结果：

- `DuelOutcomeContractTests`：16/16 PASS。
- Economy-aware executor、Economy port、InteractionPipeline 40、Detached Host boundary 69、request receipts 39：PASS。
- Production Configured/Detached/Courier Host、Production Economy-aware commit、Production OptIn：PASS。
- Persistence chunk、Migration 10 + corrupt retained 2、Identity 99 SyncData / 35 behavior：PASS。
- Persistence/Profile：95 literal、121 typed、42 symbolic、40 flattened、3 profiles：PASS。
- Production Duel replay：Debug 32/32、Release 32/32，1.3/1.4 surface parity PASS。
- 阶段8文档门禁：PhaseEightReadiness 62/62、Bridge 10/6、Composition 18/24、ModuleCatalog 8/3/16/8，全部 exit 0。

Production Duel 产物身份：

| 配置 | API | SHA-256 | MVID |
| --- | --- | --- | --- |
| Debug | 1.3 | `A8751EA2D8F679E756DAE1552FFA23FB5E5962C5BFA6BA1754E1E6284505978F` | `01c995d0-65d6-4ab9-9117-88b316907969` |
| Debug | 1.4 | `9356DBFFEA6187491A875F5F52ED21BDB4486C10616186BCCF3DDFD65C58AD0C` | `71d30724-f9e7-4e56-8797-8ad3c7eea6c8` |
| Release | 1.3 | `CE2A8B953A0608FFA89C45030656B693046B5C1B835ACEAFE3DF24F613F9C9DA` | `80abdf94-4c06-4220-bbf7-f5126a573c41` |
| Release | 1.4 | `DF181947879AA7CA47A98C5D7567E709632E51ECF335F8230C3C8353EBE3CB4A` | `4ca3ad80-204a-4bf8-afe5-a9679df1b30c` |

官方 project-local Stage 命令：

```powershell
& '.\一键编译覆盖推送\build_single_module.ps1' -Configuration Debug -Stage
& '.\一键编译覆盖推送\build_single_module.ps1' -Configuration Release -Stage
```

固定引用：1.3 为 `G:\AFMOD\NEW-10\_deps_auto`（`v1.3.15.110062`），1.4 为
`G:\AFMOD\NEW-10\.tmp\build_check\1.4`（`v1.4.6.115628`）。Debug/Release 的 1.3、1.4、Bootstrap 六项均 0 warning / 0 error，Stage 只写项目目录：

| 配置 | Bootstrap SHA-256 | 1.3 SHA-256 | 1.4 SHA-256 |
| --- | --- | --- | --- |
| Debug | `F69C5D909E92864D18CCE97B364C5626F05EC7BD605EFFF7FE67276B172646F5` | `A8751EA2D8F679E756DAE1552FFA23FB5E5962C5BFA6BA1754E1E6284505978F` | `9356DBFFEA6187491A875F5F52ED21BDB4486C10616186BCCF3DDFD65C58AD0C` |
| Release | `C8DC52DE470B8398BAB0007AED38521B4B708EC952C5A0E12D5B4D825DA6A0B9` | `CE2A8B953A0608FFA89C45030656B693046B5C1B835ACEAFE3DF24F613F9C9DA` | `DF181947879AA7CA47A98C5D7567E709632E51ECF335F8230C3C8353EBE3CB4A` |

以下中间命令不计 PASS，但已保留原日志：

- PowerShell 参数拼接解析错误；未运行任何测试或写生产目录。
- PATH 上的 dotnet 无 SDK；后续统一使用 `G:\AFMOD\.dotnet-sdk\dotnet.exe`（8.0.422）。
- 误把空目录 `ProductionDetachedCommitBoundaryReplayTests` 当 csproj。
- 误把 Python runner `PersistenceProfileConfigContractTests` 当 csproj。
- Production OptIn 首次未传必需的 GameRoot/reference/Harmony/MCM/UIExtender/private runtime properties；补齐显式属性后 PASS。
- 文档清理首次对整份历史台账搜索 `TODO`，命中既有阶段状态而错误失败；随后只审计本次新增行，conflict 与新增 TODO/HACK/FIXME 均 PASS。

独立只读审查初次报告的四个 P1（result lock 太晚、stake/debt 串场、owner 满容量、subject index 误删）均已逐项修复；最终源码复核为 P0=0，未留下新的明确 P1。

## 仍未验证 / 风险

- 真实 Bannerlord Campaign/Mission、accept/reject/cancel/death/exit、stake/debt/renown、AFEF/Memory readback：NOT-RUN。
- 真实旧存档、process crash/restart、Fourberie 安装态、1.3/1.4 live Host：NOT-RUN。
- `Domain / legacy-unbound` 不是 exact detached request provenance；因此 M1 receipt 不得转换为特定 ActionPlan 成功。
- receipt 仅进程内保留；安全 rollover 后更早 readback 不再可用，且没有 durable recovery/compensation。
- observation reservation 若在 Mission 已开始时失败，不会为了 sidecar 强制取消 legacy Mission；它只清 unbound artifact并保持无 typed readback，该 fault edge 尚未实机验证。
- Memory/AFEF 与 delayed death 只能标 attempted-unconfirmed，不能以 compiled IL 代替 live effect 证明。
- 默认三渠道、facade 删除、最终打包/部署/发布仍被阶段 7 live 门禁阻断。

## 回滚

- 实现回滚：`git revert 16f3cbef`。
- 文档提交完成后应单独 `git revert <本HANDOFF文档提交>`；不要 hard reset、rebase 或 force push。
- 意图 checkpoint：`fc3cd722`；基线：`9955658b`。
- 上述 Git revert 只回滚源码，不会移除 ignored 的 `bin\Debug\single_module_stage`、
  `bin\Release\single_module_stage` 或本验证目录；回滚后这些 M1 Stage/hash 全部视为 stale，禁止
  继续用于验证或打包。确需回滚产物时，先隔离/清理这两个**项目本地** Stage 目录，再从回滚后的
  源码重新运行 Debug/Release `-Stage` 并记录新 hash。
- 本切片没有游戏目录、NEW-10、GCCZ 或远端副作用，无需恢复游戏文件。

## 下一精确任务

`LOCAL-7-M2`：只建立 exact detached dispatch provenance。真实 request/trace/channel/session/subject/runtime/save/action fingerprint 必须在 queue/start 前绑定同一 `DuelId`，并显式返回 rejected/queued/started；不得把 M1 的 `legacy-unbound` receipt 冒充 ActionPlan 成功，也不得顺带删除 facade、切默认入口、改变存档 identity 或重放 gameplay effect。

## 新线程启动语

> 请读取 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-01-duel-actual-session-outcomes.md`，在分支 `codex/af-main-refactor-continuation-20260831` 上按 `LOCAL-7-M2` 继续；先核对 Git/远端/工作树和本 HANDOFF 所列未验证项，只补 Duel exact detached dispatch provenance，保持阶段 7 为 VERIFY、阶段 8 执行为 BLOCKED，禁止删除 facade、切默认入口、部署或从 legacy receipt 推测玩法成功。
