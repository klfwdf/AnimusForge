# LOCAL-7-G：UnknownAfterStart effect state 接续

## 状态与边界

- canonical worktree：`G:\AFMOD\AF-REFACTOR`。
- branch：`codex/af-main-refactor-continuation-20260831`。
- 基线 `c2a2be96`；意图 checkpoint `899effbb`；源码实现/测试提交 `d765270a`。
- `origin/refactor/prepare-af-restructure` 在最终 fetch 时仍为 `fc8c344e`；源码提交后本地 ahead 22 / behind 0。
- `LOCAL-7-G` 代码与离线验证完成，状态 VERIFY；阶段 7 仍 VERIFY，阶段 8 破坏性执行仍 BLOCKED。
- 未推送、未部署/启动游戏、未读写玩家存档、未切默认入口、未改一键覆盖脚本或 GCCZ；`NEW-10`、`GCCZ` 均保持 clean。
- 用户已授权在**全部可自主工作达到最终发布门槛后**正常 fast-forward push，并把最终精简 HANDOFF 发到 QQ 群 `骑砍2 AnimusForge mod 元老院 (7)`；本切片不是全项目完成点，因此没有提前推送或发送 QQ。

## 本轮结果

旧路径把 owner callback 已开始但效果无法确认的情况降成普通 validation rejection，可能允许 fallback/replay，并丢失已确认的早先效果。本轮建立三态事实边界：

| 场景 | count / facts | EffectState | ActionsExecuted | 后续 |
| --- | --- | --- | --- | --- |
| owner 未开始且前置拒绝 | `0 / 0` | `NoConfirmedEffect` | `false` | 可按原安全策略处理 |
| full / known partial | `>0 / owner-confirmed` | `ConfirmedEffect` | `true` | 终态，不重复 owner |
| owner 已开始，当前效果未知 | `0 / 0` | `UnknownAfterStart` | `false` | `NonRetryableFailure`，不 fallback/replay |
| 先有已知 Economy，后续 unknown | `>0 / 仅先前已确认` | `UnknownAfterStart` | `true` | 保留已知事实，不为未知 action 造 fact |

`ActionsExecuted=false` 不代表允许重试；count-zero unknown 的防重放来自 terminal receipt + request reservation，不能伪造成功 bit。

## 实现

1. **Additive contract / ABI**
   - `EconomyRewardDebtReplayStatus.UnknownAfterStart=6` 尾增，旧值 `0..5` 不变。
   - 新增 `ActionExecutionEffectState` 与 `IActionPlanExecutionEffectReceipt`，不修改旧 outcome interface。
   - 保留 executor 六参构造器、Economy result 四参构造器和 `InteractionCommitResult` 唯一公开四参构造器。

2. **Port 与 owner**
   - domain callback throw/null 以及 callback 返回的越界 count、`Applied+0`、非法 partial、count/facts 与失败状态矛盾等，统一为 fact-free unknown。
   - main-thread、target、capability、plan 等 callback 前拒绝仍为 `NoConfirmedEffect`。
   - Hero、Party、Merchant owner 首个未知 action 后停止，保留其前的 count/facts。
   - `EconomyMutationObservation` 显式贯穿旧 helper 吞错路径：普通/RP 物品 roster add、RP generation shell、settlement/workshop/caravan transfer、NPC equipment restore queue，以及 rollback exception/no-op/partial readback。
   - 旧 public/internal bool/int helper 继续走兼容 wrapper；只有 replay-aware core 接受 observation。

3. **Executor / Committer / receipt cache**
   - Economy gate、Economy port、legacy owner callback in-flight throw 都成为 structured unknown；gate clean rejection仍为前置拒绝。
   - Unknown 始终 `NonRetryableFailure`；known-before-unknown facts 保留，count-zero 强制零 facts。
   - Memory 成功/失败都保留 terminal effect state；duplicate、in-progress、同 request payload mismatch 不执行 action/memory 第二次。
   - direct executor throw 也写一次可见 exchange（若 memory owner接受），但不写伪造 action fact。

4. **Detached Host**
   - callback 的 `observedCommit` 是唯一权威；dispatcher 不能用 fake success 升级真实 unknown/rejection。
   - callback 已开始但 throw/null/in-flight：terminal unknown，不 fallback、不 `afterCommit`。
   - dispatcher 在 callback 尚未开始时伪报 success：清空伪 receipt 的 action/history bit并走仍安全的 legacy fallback。
   - dispatcher 已返回后，in-flight callback 不再发布到 Host receipt，也不触发 `afterCommit`；late callback 被关闭。

## 验证

日志目录：`G:\AFMOD\AF-REFACTOR\.tmp\validation\unknown-effects-20260901-0754`。

| 检查 | 最终结果 |
| --- | --- |
| Economy Port contract | PASS：unknown isolation 8、ordinary no-effect failure 1、partial normalization 4、enum ABI |
| Economy-aware executor/committer | PASS：gate 5、partial 4、partial receipt 4、unknown 3、unknown receipt 4 |
| Interaction pipeline | PASS：原 40、Native callback failure 4 |
| Detached Host | 69 cases PASS（23×3渠道）；含 throw/null/in-flight、fake success、callback-not-started、late close |
| Request receipt | 39 cases PASS；含 duplicate、reentrant、in-progress/mismatch effect、bounded cache、公开 ctor ABI |
| Production Economy-aware | PASS：mixed/economy-only/receipt、partial 2、unknown 3；加载最终 Debug 1.4 DLL |
| Production Economy owner | PASS：三 owner unknown、7 个 swallowed mutation marker、3 条 replay-aware helper、4 条 propagation chain |
| Production Host/Courier/OptIn | CourierHost、ConfiguredHost、DetachedHost、OptIn 全部 PASS；Memory/Courier fixture 36 assertions PASS |
| Persistence/Profile/Config | PASS：95 keys、121 typed bindings、8 types、3 profiles |
| Persistence identity | PASS：99 SyncData signatures、35 CampaignBehavior types，added/removed 均空，Bootstrap-only |
| Debug + Release Stage | 1.3、1.4、Bootstrap 六项全部 0 warning / 0 error；仅 project-local Stage |
| Cleanup / review | `git diff --check`、冲突/凭据/临时路径扫描 PASS；独立终审无 P0/P1 blocker |
| Live game / real save | NOT-RUN：未部署、未启动 Campaign/Mission、未注入真实 TaleWorlds mutation fault、未读写旧档 |

固定引用：1.3 `v1.3.15.110062`，1.4 `v1.4.6.115628`。

| 最终产物 | SHA-256 |
| --- | --- |
| Debug Bootstrap | `BEECC3169CF8E6AE6C98122B256E8AD724902268DD352AFA17F0D48319B44CDE` |
| Debug 1.3 | `8CBC2A641C4C48F3A0292AF92A25DA07A333C5B86D1447DBDFFEF5BC7372ACDD` |
| Debug 1.4 | `55ED1FBBD7074BFBD0C7606BB1411FBA9FCB27FF9DAE3F8111318E0E857256D4` |
| Release Bootstrap | `F9009343300D8AA0DF95D926F28022F605EA7D400FA26F6D17EB7DF87F592B46` |
| Release 1.3 | `B216BC4B297F28D075E013B75E43B971D4E21A449D88D955AFDA5B821EF3F384` |
| Release 1.4 | `BBD4C6999A89B4E08AE2BC166228631D9EF6B59351C0B827E53B541D6429C10F` |

## 仍未解决

- `InteractionCommitReceiptCache` 仍只有 512 项、process-local、completed 可淘汰；不跨重启/读档提供 durable exactly-once。
- Unknown 只阻止危险重放，不补偿或回滚实际 gameplay mutation。
- Memory 失败后没有 durable memory-only 补写队列；duplicate 不会自动补写 AFEF/history。
- `afterCommit` 失败或被安全抑制后没有 channel-owned 持久恢复。
- Courier economy-only 的 `PostprocessConsumed` 仍需真实 save/load；Native/Scene/mixed 没有 durable business tombstone。
- 真实金币、库存/RP 物品、商人市场、债务、固定资产、AFEF、Campaign/Mission 与旧存档均待隔离实机验收。
- 默认三渠道没有切换；阶段 8 不能删除活跃 facade 或执行破坏性清理。

## 下一精确任务

`LOCAL-7-H`：先做 **memory-only recovery** 的最小可持久/可幂等切片。

必须满足：

1. recovery payload 只含可见 user/assistant 与 owner-confirmed facts，不含可再次执行的 ActionPlan；
2. 只重试未完成的 memory/AFEF 写入，绝不调用 action executor；
3. 使用明确 save namespace/key/type、版本和上限，旧档缺字段安全初始化，损坏数据 fail-closed；
4. 写入成功后以 owner readback/receipt 完成 tombstone，重复 tick/load 不重复事实；
5. 先做纯 persistence/owner failure matrix，再做 production DLL replay、identity audit 和双版本 Stage；
6. `afterCommit` recovery 另列下一切片，不与 memory recovery 混在同一 owner。

真实 Campaign/Mission、live Economy/AFEF 与旧档往返有直接证据前，阶段 7 保持 VERIFY；阶段 8 仅可继续非破坏性准备。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取 `AGENTS.md`、项目 `animusforge-maintainer` Skill、`docs\animusforge-refactoring-and-repository-reorganization-plan.md` 与本 handoff。确认 HEAD 至少包含 `d765270a`，fetch 但不要覆盖本地历史。按 `LOCAL-7-H` 先实现只补 memory/AFEF、绝不重放 ActionPlan 的最小可持久幂等 recovery；先 checkpoint/红测，再最小实现、清理、focused + production replay、PersistenceIdentityAudit 与 Debug/Release 1.3/1.4/Bootstrap Stage。真实 Host/旧档无直接证据时保持阶段 7 VERIFY，不部署、不切默认入口、不删除 facade。只有全部可自主工作达到最终发布门槛后，才按用户授权正常 fast-forward push，并生成最终 HANDOFF、核对 QQ 群名后发送精简文本；禁止 force push。
