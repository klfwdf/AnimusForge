# LOCAL-7-E：Courier Economy reservation 接续

## 状态

- canonical worktree：`G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 接手 HEAD `3d9778d2`，意图 checkpoint `bbe35aa8`，实现/测试/owner 文档提交 `b2542fdd`；远端 `origin/refactor/prepare-af-restructure` 仍为 `fc8c344e`。
- `LOCAL-7-E` 的 opt-in runtime owner 代码与离线验证完成，状态 VERIFY；阶段 7 继续 VERIFY，阶段 8 执行仍 BLOCKED。
- 未推送、未部署/启动游戏、未读写玩家存档、未切默认入口、未改一键脚本或 GCCZ。NEW-10/GCCZ 保持 clean。

## 复现和修复

旧 `LegacyNativeActionPlanExecutor` 先调用 Economy port；当过滤后没有 legacy actions 时直接返回 `Executed`。因此 Courier economy-only 完全绕过 `ExecuteCourierActionPlanForExternal` 的 channel/session/subject、delivery、terminal 和 `PostprocessConsumed` 检查。Mixed plan 也在 Courier owner 检查前执行 Economy。

修复保持单一管线：

1. Generic executor 在纯 planning、raw/typed plan equality、capability/action-count 检查后、Economy Replay 前调用可选 channel owner gate。
2. Courier gate 在主线程重新解析 session 和 recipient，要求 Courier identity、expected/session/JSON Id 一致、outbound、已交付、非终态、未消费、recipient 存活且 subject 一致。
3. Mixed plan 只 prevalidate，然后 Economy → filtered legacy callback；economy-only 在 Replay 前设置现有 `PostprocessConsumed=true`，不从 raw postprocess 改写 reply prose。
4. `PostprocessConsumed` 原本就随每个 `CourierSession` JSON 保存到 `_af_courier_sessions_v1`；没有新增 SyncData key/type、字段、receipt 或迁移。
5. 保留原六参 public executor 构造器并转发新实现，避免已编译/反射调用方 ABI 破坏；新七参构造器仅供显式 gate owner。

Gate 每次 commit 最多运行一次，ActionPlan 上限 64；只做当前 session/recipient O(1) lookup 和小计划扫描，无 Tick、全世界扫描、队列或新缓存。

## 验证

本机日志：`G:\AFMOD\AF-REFACTOR\.tmp\validation\courier-reservation-20260901-0556`。

| 检查 | 结果 |
| --- | --- |
| 旧边界复现 | baseline contract 证明 economy-only legacy callback=0；新增 test 对旧源码红编译：不存在 gate 参数 |
| Economy-aware contract | PASS：mixed/receipt/economy-only；gate 5 类覆盖 accept-before-replay、reject、throw、replay fail 和 mixed 顺序 |
| Production Courier reservation fixture | 18 assertions PASS：identity/direction/delivery/terminal/consumed/Returning recovery、二次 reservation、真实 CourierSession JSON roundtrip；`Campaign.Current=null`、saveWrite=0 |
| ProductionEconomyAwareCommit | PASS：mixed、economy-only、receipt；同时证明旧六参构造器仍可反射调用 |
| ProductionOptIn/Courier/Configured/EconomyOwner | 全部 PASS；OptIn 绑定最终 Debug 1.4 SHA |
| Interaction / Economy port | Interaction 40 + Host 48 + Native callback 4 + receipts 38 PASS；Economy port 全矩阵 PASS |
| Persistence/Profile/Config | PASS：95 keys / 121 bindings / 8 types，无 fixture 行号变化 |
| PersistenceIdentityAudit | PASS：99 SyncData signatures / 35 Campaign behaviors / Bootstrap-only |
| Debug/Release 1.3、1.4、Bootstrap | 六项 0 warning / 0 error，project-local Stage success |
| Cleanup / review | `git diff --check`、冲突标记和聚焦旧路径检查 PASS；独立审查发现并修正构造器 ABI、Stage 误门和 raw/reply 混用风险 |
| Live Campaign/save/load/assets/AFEF | NOT-RUN：无部署与隔离存档授权；fixture JSON roundtrip 不是真实 SyncData/save roundtrip |

| 最终产物 | SHA-256 |
| --- | --- |
| Debug Bootstrap | `93089C4876DE9EA6E2C0A5564234C01A4F7037FA700F592E0B5BB331392102D7` |
| Debug 1.3 | `0CB3B884DAB0BBD6C9EE19B41155D24A8C5A9A0B775A0B7436CA04308B6CE0B0` |
| Debug 1.4 | `A8C6B0447DD376E02685639A161E5B0B0ABDD6EFBD51A9D1D09D4E5C66E3CF93` |
| Release Bootstrap | `9FC906ACB9577145EA09301E740502656F2E8644AE048FBAF6E18EB3B2CBCCB6` |
| Release 1.3 | `AA1DAD2680FA5FF57E731172516FD8AC33661DB99082686860D99B5B79F9231C` |
| Release 1.4 | `A96B78D593EC175BF2D5B3DD6968338DB20D441A55B2B8EC662773694B84D766` |

## 重要限制

- 仓内没有 production caller 调用 outbound detached Submit API，默认 Courier 仍走 legacy；本轮没有切默认路径，也没有闭合 detached reply 的 `ReplyText/ReplyGenerated/Stage` completion。
- Reservation 是 fail-closed / at-most-once，不是磁盘事务。Economy 失败、memory 失败或进程在后续游戏保存前崩溃，不能自动重试或宣称原子恢复。
- Mixed plan 仅在 Economy 前 prevalidate；Economy 成功后 legacy 失败/状态变化仍可 partial。多个 Economy action 的部分成功与 facts 丢失也未解决。
- Courier session 完成后会删除，没有 durable tombstone；未知 Stage/Direction 当前可回退 Outbound；pair key/JSON Id 冲突、坏 chunk 和完整协议标签可见清理仍需专门切片。
- 旧档缺 `PostprocessConsumed` 默认为 false。只有真实旧档和 partial session save/load 回放才能证明安全，fixture 不能代替。

## 下一精确任务

`LOCAL-7-F`：先定义 partial outcome/recovery 状态和不可重放原则，再选一个最小纵切片。优先避免 mixed plan 在已执行 Economy 后因 legacy 拒绝丢失事实或被 return fallback 再处理；不得用整套重试或合成成功 AFEF。之后分别处理 memory/afterCommit recovery、Courier completion seam 和坏存档 fail-closed。真实 Host 授权到位时再执行 D/E 的旧档、live资产、AFEF 验收。
