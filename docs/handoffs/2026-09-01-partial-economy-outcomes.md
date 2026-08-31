# LOCAL-7-F：known partial Economy outcome 接续

## 状态与边界

- canonical worktree：`G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 接手 HEAD `67603f18`，意图 checkpoint `7186048d`，实现/测试/owner 文档提交 `8f22d737`；远端仍 `fc8c344e`。
- `LOCAL-7-F` known-partial runtime 代码/离线验证完成，状态 VERIFY；阶段 7 继续 VERIFY，阶段 8 执行继续 BLOCKED。
- 未推送、未部署/启动游戏、未读写玩家存档、未切默认入口、未改一键脚本或 GCCZ；NEW-10/GCCZ 保持 clean。

## 问题与修复

三个 production Economy owner 已能逐项执行并返回 `AppliedCount`/真实 fact；旧 partial 仍用 `Status=Applied + short count + partial_replay`。Executor 要求 count 等于计划总数，失败时清空 facts；Economy 成功后 legacy reject/throw 也清空 facts。Committer 随后写空事实并报告 `ActionsExecuted=false`。

本轮最小修复：

1. 在 `EconomyRewardDebtReplayStatus` 尾部追加 `PartiallyApplied=5`；旧 0–4 数值不变。Hero、Party、Merchant owner 在 applied/failed 混合时返回结构化 partial。
2. Main-thread port 仅把旧 `Applied + 0<count<total` 归一化；不把 `Failed/Rejected + count` 升格为已知效果，并拒绝无效 partial count。
3. 新增 additive `IActionPlanExecutionOutcomeReceipt`；不修改既有 receipt interface。Applied count 只来自 Economy owner，不因 legacy callback 的 `Executed` 伪造。
4. Known partial 立即停止 legacy；完整 Economy 后 legacy reject/throw 也返回 `NonRetryableFailure`，保留 Economy facts 和稳定错误码。
5. Committer 对 partial 只提交 owner outcome facts，忽略未执行计划携带的 supplied facts；返回 `ActionsExecuted=true`。Memory 失败仍保留 action bit 和 terminal error；duplicate 不重复 Economy/memory。
6. Detached Host 对 partial 不 fallback、不调用 `afterCommit`，但保留写入成功时的 `HistoryWritten=true`。

仅在一次 commit 的既有 ActionPlan（最多 64）和 fact 列表上做线性处理；无 Tick、世界扫描、队列、缓存或 save 数据。

## 验证

日志：`G:\AFMOD\AF-REFACTOR\.tmp\validation\partial-economy-20260901-0658`。

| 检查 | 结果 |
| --- | --- |
| 红测 | owner `AppliedCount=1/2` + fact 被旧 executor 清空并变为普通 rejection |
| Economy-aware contract | PASS：partial 4（owner short、mixed reject、mixed throw等）+ partial receipt 4（success/duplicate/memory fail）+ 原矩阵 |
| Economy port contract | PASS：partial normalization 4、enum numeric compatibility、原 main-thread/capability/count 矩阵 |
| Detached Host | 51 cases PASS（17×3渠道）；partial 为 NonRetryable/actions/history，不 fallback、afterCommit=0、late callback不重放 |
| ProductionEconomyAwareCommit | PASS：mixed/economy-only/receipt + partial 2，真实最终 Debug 1.4 DLL |
| Production EconomyOwner/Courier/Configured/Detached/OptIn | 全部 PASS；OptIn manifest 绑定最终 Debug 1.4 SHA |
| Interaction | 原 40 + Native callback 4 + request receipt 38 PASS；Host 更新为 51 |
| Persistence/Profile/Config | PASS：95 keys / 121 bindings / 8 types，无 fixture 行号变化 |
| PersistenceIdentityAudit | PASS：99 SyncData signatures / 35 Campaign behaviors / Bootstrap-only |
| Debug/Release 1.3、1.4、Bootstrap | 六项 0 warning / 0 error，project-local Stage success |
| Cleanup / review | `git diff --check`、冲突/临时路径搜索 PASS；独立审查无阻断并确认 ABI/owner-fact 边界 |
| Live Campaign/assets/debt/AFEF/save | NOT-RUN：无部署/隔离存档授权；production DLL replay 不是 live game evidence |

| 最终产物 | SHA-256 |
| --- | --- |
| Debug Bootstrap | `7F76241FBA1F004E95D004FF39D445CA04394F606682835C1D09B7029EF05691` |
| Debug 1.3 | `9079026A6F0F5BB5A8D5813FA73458872E5EC6338DCD33D1560D56B53DC32BB3` |
| Debug 1.4 | `D1FB8A7C083ECF229078C1BD4BD1B4171661CAB86B1EBA63EDC568E0A88AE9E9` |
| Release Bootstrap | `9E57C15437343FEDA30F12A29F44378BEA43DA74E2B18EE3BD6731156AEE13A9` |
| Release 1.3 | `E02B79E17762C698F5AEF5E04BB80CA9C85A891E04E17D11BFBB4BBD727281C4` |
| Release 1.4 | `A913FB8C7516055F128F6F15ABC69BF33295ED501633AA525069DFC378D37070` |

## 仍未解决

- Domain callback/action helper 可能在 mutation 后、`AppliedCount++` 前抛错。这是 `UnknownAfterStart`；本轮没有按 count/facts 猜测效果，也没有生成成功 AFEF。
- Known partial 的 request receipt 仍是 512 项 process-local 缓存，完成项可淘汰且不跨进程/读档。Courier economy-only 另有 session consume；Native/Scene/mixed 没有 durable business tombstone。
- Memory 失败目前只保留“不重放 action”的终态，没有 durable memory-only 补写队列。afterCommit 失败也不会因 duplicate 自动恢复。
- Multiple Economy actions 仍非事务，无补偿/回滚；已发生效果只被真实记录，不被撤销。Legacy owner 内部吞错仍可能把不完整处理视为 consumed。
- 默认 Courier detached path 仍未启用；真实金币、物品、债务、商人、Party、旧存档和 AFEF 全部待实机验收。

## 下一精确任务

`LOCAL-7-G`：为 owner callback 已开始但无已知 count/fact 的 `UnknownAfterStart` 增加结构化 effect state，保证诊断不再伪装成普通 validation rejection；然后选 memory-only 或 afterCommit recovery 的一个最小可持久/可幂等切片。任何恢复只能重试未发生的记录/通知，绝不能再次执行 ActionPlan。
