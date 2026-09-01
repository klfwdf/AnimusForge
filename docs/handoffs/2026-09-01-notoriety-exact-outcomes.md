# LOCAL-7-L：Notoriety exact line/session outcome 接续

日期：2026-09-01。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-7-L` 代码与离线/compiled验证完成，状态 **VERIFY**；阶段7不标DONE。
- 基线 `68dce8e9`，意图checkpoint `cddc7628`，实现提交 `80729cb9`。
- 用户已明确要求把当前协作分支正常推送到GitHub，并把HANDOFF放进仓库；本文件随文档提交进入该推送。
- 没有部署/启动游戏、读写真实存档、切默认入口、删除facade、修改NEW-10/GCCZ或游戏目录。

## 旧路径的决定性问题

旧 `PlayerNotorietyBehavior` 不是可恢复owner：

1. `DoesObserverKnowPlayer` 等read路径也可能创建按Hero聚合的transient active并roll；positive会立刻写
   `KnowsMajorHistory/KnownAtDay`，negative outcome与active均不入档。
2. `NoteConversationLineForExternal(string)` 和两个finalize API均为吞错`void`，成功/no-op/异常无法区分。
3. line只增加易失 `LineCount`；save/load会清active。finalize旧顺序先Remove active，再写
   `CompletedConversationSessions`、bonus和day，异常后无法重试。
4. scene/native/courier对同Hero可混入一个bucket；迟到的旧ConversationEnded可能误消费新session；
   纯read创建的零line active也会被旧代码计为完成session。
5. H Daily marker、aggregate delta和debug log都不能证明某个Notoriety line/session成功；K `AFWM1`
   只证明Economy→weekly data attach，与Notoriety无关。

## 新owner顺序

```text
H core Daily/Recent receipt completed
  → exact Daily marker readback（只证明line存在）
  → L session/line identity（opaque session + runtime/save + recovery/payload/part）
  → AFNR1 ProbeLine
      → duplicate tombstone: return Duplicate，不roll
      → conflict/capacity/corrupt: fail closed，不roll
      → missing: freeze current active roll
  → staged owner commit
      → publish aggregate known-state + AFNR1 line witness into same Notoriety JSON
  → native exact session end / prior-day stale close
      → freeze absolute known/bonus/session/day target
      → apply monotonic target → readback → Applied
```

## 本轮改动

### `Refactor/Runtime/NotorietyConversationOutcomeReceipt.cs`

- `AFNR1` checksum wire；Open/Confirmed/Applied/Unknown/Rejected/Quarantined状态。
- session receipt绑定subject、raw memory-session的hash、runtime/save；start clock只进candidate hash，
  同session跨小时/跨日仍是同一receipt。
- line ID绑定H recovery ID、payload hash、`user`/`assistant` part与origin clock；不保存原文或raw session。
- 64 pending / 512 terminal / 每session 260 lines；atomic import、conflict quarantine、clock clamp、clone。
- zero-line不能Confirm；loaded Open→Unknown且保留line tombstone；Confirmed只提供冻结绝对target。

### `PlayerNotorietyBehavior.ConversationOutcomes.cs`

- witness嵌入既有 `_af_player_notoriety_state_v1` JSON的
  `ConversationOutcomeReceipts : Dictionary<string,string>`；没有新增SyncData key/type。
- duplicate probe位于active创建/RNG之前；容量满、terminal缺line、坏journal均不reroll。
- read-path roll只冻结到active；首个实际exact line才同批发布positive known-state和receipt。
- finalize冻结绝对target，先readback再Applied；target用单调赋值，重复调用不再加一次delta。
- 不同exact session先准确收尾旧session；迟到旧session key不能finalize新session。
- legacy/exact混用先把L receipt变Unknown，再回旧行为；零line read session不再增加完成次数。
- Open load一律Unknown、不补roll/finalize；Confirmed load只重放绝对data target。embedded wire坏时保留raw并禁用L。

### 兼容与H/J/K边界

- 原public `void NoteConversationLineForExternal(string)` 与finalize overload保留；default入口未切。
- legacy Daily writer改为先确认line已发布，再调用旧void owner；仍无exact turn/session，明确NOT-RECOVERABLE。
- H只在brand-new、同步core Completed且exact marker存在后调用L typed owner；H marker不等于L成功。
- H seed/hash/wire、Courier `AFCI1`、K `AFWM1`、weekly storage与95个literal key/type均未改。

## 验证

证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\notoriety-outcome-l-20260901-174858`

关键PASS：

- AFNR1 pure contract：14/14（session/line identity、duplicate、positive/negative frozen、
  zero-line gate、confirm/apply、Open load Unknown、Confirmed retry、tamper/conflict、64/512/260、
  atomic corrupt import、clock clamp、data-only、clone）。
- InteractionPipeline 40；Detached Host boundary 69（三渠道）；request receipts 39。
- Memory recovery、Courier inbound、Economy-aware executor、Economy port均PASS。
- fresh Production OptIn：embedded witness、duplicate-before-roll、exact finalize、Open load Unknown、
  Confirmed reconcile、legacy ABI、data-only；Production Configured/Detached/Courier Host、Economy owner/commit均PASS。
- Persistence/Profile：95 literal、121 typed、42 symbolic、40 flattened；Migration 10/corrupt retained 2；
  Identity 99 SyncData / 35 CampaignBehavior。
- Debug与Release的1.3/1.4/Bootstrap六项全部0 warning / 0 error。

Stage SHA-256：

```text
Debug Bootstrap   53563386399A06AB60632053B86C517CDEA212A5C0FC4F3652625C106CD34242
Debug 1.3         C2250C8D8E73D41301E6A83BCE119C85A3D769461B9D34937FABD3CDF8A28CA0
Debug 1.4         E5AD255812CB66CFE5774C01C5A49CCFCFE03A9EFAD79ABFFDC26AFEE5C7EB8B
Release Bootstrap B8F472CE08FC484814F8A856A9812571880623865DD36177238B9C78E7CC7F94
Release 1.3       B57BB65AF8BE93D1AD6D7993C743591C90E0B7F3F6D0E8BCFB64F0A8DA89B9BF
Release 1.4       F6AB9E5CF89E6411124DD68371AF026B6CBD5E1229EDBEF9482039227D30C3BD
```

有两次无效中间验证未计入PASS：一次runner持有staged DLL导致Stage assembly权限失败；一次在生产源
更新后用stale DLL触发freshness/missing-method失败。runner退出并重建fresh Stage后，最终命令均PASS。
独立子代理终审因Codex usage limit未能执行；本轮保留该事实，使用compiled guard、focused矩阵、
`git diff --check`和主代理逐项source review收尾，不冒充独立review。

## 仍未验证 / 风险

- fixture/reflection/IL不是live Campaign、真实MBRandom、ConversationEnded顺序或SaveSystem原子性证明。
- legacy default line没有H exact identity，仍使用旧void owner；L不是全渠道默认切换。
- save中Open session会转Unknown并丢弃未finalize bonus/session count，宁可遗漏也不重roll。
- embedded AFNR1 wire损坏可隔离；但外层旧Notoriety JSON/chunk损坏仍会按旧行为重置全state。
- terminal tombstone有512上限，不承诺永久exactly-once；真实旧档/crash/reload仍需验收。
- 真实Campaign/Mission、live Economy/AFEF、旧档和默认入口仍NOT-RUN，因此阶段8执行继续BLOCKED。

## 回滚

- 实现：在干净工作树正常执行 `git revert 80729cb9`。
- 意图checkpoint：`cddc7628`；本HANDOFF/台账是后续独立docs提交，可分别普通revert。
- 禁止hard reset、rebase已共享历史或force push。本轮未部署，无游戏DLL/PDB/ModuleData回滚。

## 下一步

可以并行进入 `LOCAL-8-A` 非破坏性准备：

1. 刷新Bridge矩阵与20领域owner/entry/save/Prompt/ActionPlan清单。
2. 只登记旧facade、bridge、flag和compat shim清理候选，不删除。
3. 为每个候选绑定替代证据、live验收、回滚提交与数据风险。
4. 汇总最终验收包；真实Host/旧档/live Economy/AFEF前不切default、不打包发布。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取 `AGENTS.md`、公共台账、
> `docs\handoffs\2026-09-01-notoriety-exact-outcomes.md` 和制作组简报。确认HEAD至少包含
> `80729cb9`，fetch但不要覆盖本地历史。并行进入 `LOCAL-8-A` 非破坏性准备：刷新Bridge矩阵、
> 清理候选、回滚和最终验收包；所有删除/default cutover/部署保持BLOCKED。同时保留阶段7真实
> Campaign/Mission、旧档、live Economy/AFEF清单为硬门禁。完成后跑相关contract/production、
> Persistence/Identity与Debug/Release Stage；没有真实证据不得把阶段7或阶段8执行标DONE。
