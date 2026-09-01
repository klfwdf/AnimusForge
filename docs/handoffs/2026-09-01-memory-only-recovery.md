# LOCAL-7-H：持久 memory-only recovery 接续

## 状态与边界

- canonical worktree：`G:\AFMOD\AF-REFACTOR`。
- branch：`codex/af-main-refactor-continuation-20260831`。
- 基线：`a8001b87`；意图 checkpoint：`6f8d8cc0`；实现/测试提交：`f6e5e694`。
- `LOCAL-7-H` 代码与离线验证完成，状态 **VERIFY**；阶段 7 仍为 VERIFY，阶段 8 破坏性执行仍 BLOCKED。
- 本轮开始时 `git fetch origin --prune` 曾因 `schannel: failed to receive handshake, SSL/TLS connection failed` 失败；未用不安全 TLS 绕过。收尾重试成功，`origin/refactor/prepare-af-restructure` 仍为 `fc8c344e`，实现提交后本地 ahead 25 / behind 0。
- 未推送、未部署/启动游戏、未读写玩家存档、未切默认入口、未改官方构建/覆盖/推送脚本或 GCCZ；`NEW-10`、`GCCZ` 未写入。
- 用户授权的 GitHub push 与 QQ 群 HANDOFF 只在**全部可自主工作达到最终发布门槛**后执行。本切片仍有后续 owner 工作和 live 验收，所以没有提前推送或发送 QQ。

## 结果

旧 batch owner 只有进程内 receipt；动作已经终态后，memory/AFEF 失败既不能安全重放整次请求，也无法跨 save/load 补写。本轮增加一个严格 **memory-only** 的持久 owner：

1. action owner 仍只执行一次；恢复路径没有 ActionPlan、postprocess、executor 或 afterCommit 类型/回调；
2. user、owner-confirmed AFEF、assistant 拆为 Daily 三步和 Recent 三步，只补 marker 未确认的单一 store component；
3. save/load 后以 owner marker、payload hash 和 full-wire checksum 决定 Applied/Pending/quarantine；绝不从文本相等或计数猜成功；
4. completed tombstone 缺少应有 Recent marker 时会隔离，不能返回虚假 Duplicate；
5. corruption、容量、owner 迁移、部队销毁和跨进程 identity 均 fail-closed。

## 实现

### 持久 ledger

- owner：`MyBehavior`；新增 partial `MyBehavior.MemoryRecovery.cs`，没有新增 CampaignBehavior。
- key：`_af_interactionMemoryRecovery_v1`；物理类型 `Dictionary<string,string>`；通过 `CampaignSaveChunkHelper.FlattenStringDictionary/RestoreStringDictionary` 保存。
- wire：BCL `BinaryWriter` + Base64，schema 1；SHA-256 opaque recovery ID、payload fingerprint 与全记录 checksum。raw commit ID 不进入存档。
- 上限：64 pending、512 completed tombstones、64 quarantine；每个 value/文本/总 UTF-8 payload 有硬上限。oversize quarantine 仅保留有界诊断。
- 重试：每 tick 由 O(1) flag 进入，最多一个组件；last-attempt 轮转避免队首饿死，单组件五次失败后 quarantine。pending 不淘汰，只淘汰最旧 completed。
- load：Started→Unknown；matching marker→Applied，missing marker→该组件 Pending，conflict→整记录 quarantine。unknown schema/checksum/hash/state/mask/size、valid+q 同 ID、disabled sentinel/总容量异常都不可执行。

### MyBehavior owner

- Daily/Recent 写入都先 clone，再以包含隐藏 marker 的 owner collection reference 发布；marker 与该 store 内容不可分离。
- `DailyMemoryLine` 尾增非显示 `MemoryCommitId/Part/Hash` 和 origin day/date；`DialogueDay` 尾增 marker map。旧 JSON 缺字段安全初始化。
- normal append、single-use fact cleanup、lore trim、owner merge 和 dev history filter 均保留 marker；tombstone 淘汰或 quarantine 只清 marker，不删可见历史。
- completed/pending receipt 在 load 时核对 Daily/Recent marker masks。Daily storage day 已压缩时允许 marker 生命周期结束；Recent marker 丢失一律隔离。
- 跨日请求遇 origin day 已压缩时写当前 open Daily draft，同时保留 origin provenance；不会生成一个随后被 seal 直接丢弃的旧日 draft。
- non-Hero alias merge 保留 immutable original subject，只 retarget projection subject；destroy cleanup 同时扫描 ledger-only subject 并 quarantine，tick 不会重新创建被销毁部队 owner。
- weekly material trigger / notoriety 保持原 owner 的 best-effort side effect；marker 防重复，但 core marker 发布后若进程中断，这些辅助效果可能缺失，本轮不宣称 exactly-once。

### 提交与 ABI

- `InteractionMemoryCommit` 保留唯一 public 七参构造器；internal overload 增加 trace/day/location/scene target provenance。
- `InteractionResultCommitter` 从 detached snapshot 冻结 Scene session、target agent/name；Courier 不再被错误归入 native dialogue session。
- `LegacyInteractionSnapshotAdapters` 的 trace 加 `ProcessTraceNonce`，避免进程重启后 generation/session sequence 重用碰撞旧 tombstone。
- `MyBehaviorMemoryFacade` 调用不同名称的 internal `CommitExternalDialogueHistoryRecoverable`；旧 public 六参 `CommitExternalDialogueHistory` 仍唯一，四个 public void API 不变。
- `MemoryCommitReceiptCache` 只保留 post-success 兼容诊断；即使 cache 命中，也必须重新经过持久 owner 的 payload/quarantine 校验。

## 验证

日志：`G:\AFMOD\AF-REFACTOR\.tmp\validation\memory-recovery-20260901-105448`。

| 检查 | 结果 |
| --- | --- |
| Memory recovery pure contract | PASS：六步顺序、12 个 marker 前/后故障、restart、process nonce、64/512/64、checksum/hash/corrupt、same-ID quarantine、12k CJK input、20k reply、retry rotation/exhaustion、projection retarget/destroy；`initialMutationReplay=0` |
| Interaction pipeline | PASS：40 pipeline、69 Host×三渠道、4 Native callback fault、39 request receipts；新增 Scene/Courier provenance |
| Production OptIn / Memory owner | PASS：missing Campaign/thread/旧 ABI；memory recovery ABI/payload isolation/marker rebuild/tombstone reconcile/orphan/wrong-owner/nonce/Scene provenance；`3498 assertions` |
| Production Host / Economy / owner | Configured、Courier、Detached Host，Economy-aware、Economy owner、Validation provider 全 PASS |
| Persistence chunk | PASS：UTF-8 boundary、missing/corrupt chunks、dictionary round trip、SafeSync isolation |
| Persistence/Profile/Config | PASS：95 literal keys、121 typed bindings、8 types、41 symbolic sources、39 flattened keys、8 legacy-first cases |
| Persistence identity | PASS：99 SyncData signatures、35 CampaignBehavior，added/removed 均空，Bootstrap-only module assembly |
| Debug + Release Stage | 固定 1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`、Bootstrap 六项均 0 warning / 0 error；只写 project-local Stage |
| Cleanup / review | `git diff --check`、conflict/TODO/forbidden payload/save-key singleton 扫描 PASS；独立终审 P0=0 |
| Live game / real save | NOT-RUN：未部署/启动 Campaign/Mission，未读写旧档，未验证 live AFEF/weekly/notoriety |

### 最终产物 SHA-256

| 配置 | 产物 | SHA-256 |
| --- | --- | --- |
| Debug | Bootstrap | `A7B6D073B88B73324EFD243AA8D9D12C6C076418F3EDBAFB27C41DC8816E311F` |
| Debug | 1.3 | `033E38FD05997F3227B3511222F1FD54D2817F343CF123BDDBA9C194E54E579A` |
| Debug | 1.4 | `EC7B9604F80D6FD49B40AC562758C92F87A6F8E3A3E52C8102E5B1E2559B4217` |
| Release | Bootstrap | `BFB46AEAB1BE2B4F65604A910F29BA2E1CBAD0C9426CB4B582C1ACB72D12A7D5` |
| Release | 1.3 | `FB6CB8DD42E065091CD68A90142EE3445B189F8BB8102CF8186E1C4EC25E0126` |
| Release | 1.4 | `46051D70098194A66146AB71A4017A827FD68D138F023FA28A1F02E66AEAF293` |

## 仍未解决 / 下一精确任务

### `LOCAL-7-I`：Courier inbound 持久 session completion

当前已确认的离线 P1：当初次 memory commit 返回 pending/failed 时，`DetachedInteractionHost` 正确抑制 `afterCommit`；H 后续虽能修好 history/AFEF，却不会调用 Courier 的 `CompleteCourierInboundDetachedCommit`。因此 inbound session 的 `LetterText`、`ReplyGenerated`、`ReplyGenerationStarted` 与 `ProcessSessionById` 可能永久卡住，读档后还可能重新生成。

下一切片必须由 **Courier owner** 新建独立、持久、幂等 completion receipt：

1. 在提交前冻结 session ID、方向、visible reply 与 memory recovery/request identity；
2. 只有 memory owner 达到 terminal Applied/Duplicate 后，主线程每 tick 最多完成一个 Courier session；
3. 重解析 session/recipient，核对方向、delivery、terminal/consumed 与 payload hash；
4. completion 成功后保存 tombstone，重复 tick/load 不再推进；
5. 不把 afterCommit、ActionPlan、executor 或 raw postprocess 放入 memory journal；不重放 Economy/action。

随后仍需真实 Campaign/Mission、旧存档、live Economy/AFEF、weekly/notoriety、Courier 往返和默认入口验收。没有这些直接证据，阶段 7 不能 DONE，阶段 8 不能删除 facade/切默认/发布。

## 回滚

- 回滚实现提交：`git revert f6e5e694`（不要 hard reset/rewrite history）。
- checkpoint：`6f8d8cc0`；前一稳定 handoff：`a8001b87`。
- additive save key 缺失对旧档安全；若已经写入新 key 后回滚，旧实现会忽略 unknown key，禁止主动删除或改写玩家存档来“清理”。
- 未改游戏目录、ONNX、GCCZ/NEW-10、官方部署脚本或远端分支，无部署回滚动作。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取 `AGENTS.md`、项目 `animusforge-maintainer` Skill、公共台账与 `docs\handoffs\2026-09-01-memory-only-recovery.md`。确认 HEAD 至少包含 `f6e5e694`，fetch 但不要覆盖本地历史。按 `LOCAL-7-I` 只实现 Courier inbound channel-owned 持久/幂等 session completion：memory 修复成功后补 `LetterText/ReplyGenerated/ReplyGenerationStarted/ProcessSessionById`，绝不把 afterCommit 或 ActionPlan 塞进 memory journal、绝不重放 Economy/action。先 checkpoint/红测，再 owner fault/restart/save 回放、production DLL、PersistenceIdentityAudit 与 Debug/Release 1.3/1.4/Bootstrap Stage。真实 Host/旧档无直接证据时保持阶段 7 VERIFY，不部署、不切默认、不删除 facade；全部自主工作达到最终门槛前不推送、不发送 QQ。
