# 阶段 6：Memory / AFEF 统一写入边界

## 本切片目标

把 detached 三渠道的历史与确认事实写入收敛到一个提交契约，同时保留旧
`MyBehavior` 作为存储权威。此切片不切换 Native、SceneShout 或 Courier 的默认
入口。原始 batch 切片不改变 `SyncData`；`LOCAL-7-H` 后续以唯一 additive
`Dictionary<string,string>` key 增加 memory-only 恢复日志，旧 key/type 和已有可见
历史格式仍不变。

## 契约

- `InteractionMemoryCommit`：携带稳定 `commitId`、渠道/session/subject，以及可选
  user、assistant 和 confirmed AFEF facts；不携带 Hero、Agent、Campaign、凭据或
  可变配置。
- `IInteractionMemoryBatchCommitter`：可选的新边界；旧的 `IInteractionMemory.Append`
  保留作为兼容 fallback。
- `MemoryCommitResult`：区分 Applied、Duplicate、Rejected、Failed。
- `MemoryCommitReceiptCache`：仅进程内、有界 512 项，不进入存档；只在 owner
  成功后保留兼容诊断。它不再作为写入授权或 duplicate 权威，所有 batch commit
  都必须经过持久 ledger 的 payload hash/blocked-id 校验。

## 规则

1. 成功结果先通过 ActionPlan 主线程复核；动作拒绝时只提交可见 user/assistant，
   confirmed AFEF 为空。
2. 成功 ActionPlan 才携带 `InteractionResult.ConfirmedFacts`。
3. user → assistant 由一次 batch commit 交给旧 `MyBehavior.AppendExternal...`，避免
   Courier/SceneShout/Native 各自再写一遍；已确认的重复 commit 不会再次执行动作。
4. inbound Courier 通过 `appendPlayerInput=false`，不会把 NPC seed 伪造为 user。
5. stale、cancel、生成失败和提交拒绝不会写确认事实；host 也不会走旧 fallback
   来重复执行一个已判定的 stale/validation 结果。

## 性能与线程边界

- 初次 Commit 只在一次交互的主线程 commit 边界执行；`LOCAL-7-H` 的 tick 只在
  O(1) pending flag 置位时、每 tick 最多补一个 memory store component。
- receipt cache 为固定上限 512 的 O(1) 哈希查找；不读取或修改存档。
- facade 只保留稳定 HeroId 或 non-Hero memory id，提交时才解析 Hero。
- legacy history/AFEF 写入成功后才登记 receipt；旧 owner 抛出异常时返回
  `Failed(legacy_memory_append_failed)`，不会把失败写入永久伪装成 `Duplicate`。

## 持久 memory-only 恢复（LOCAL-7-H）

- owner 仍是 `MyBehavior`；新增 key `_af_interactionMemoryRecovery_v1`，物理类型为
  `Dictionary<string,string>`，复用既有 UTF-8 chunk flatten/restore。没有新增
  `CampaignBehavior`、Saveable type、程序集或默认入口。
- journal 投影只有 channel/session、original/projection subject、trace/generation、
  原始日时/场景、Scene target 和已渲染的 user/assistant/owner-confirmed AFEF 文本。
  raw commit id 只用于 length-prefixed SHA-256 opaque id，不持久化；类型和 tick 路径
  不含 ActionPlan、postprocess、executor 或 `afterCommit`。
- user/fact/assistant 各拆为 Daily 与 Recent，共六个严格顺序组件。写入以 copy-on-write
  owner collection 和隐藏 marker 同时发布；重启时 matching marker→Applied、missing
  marker→仅该组件 Pending、冲突→quarantine，绝不重放 action 或其他已完成组件。
- pending 64、completed tombstone 512、quarantine 64；pending 不淘汰，tombstone 只
  淘汰最旧 completed。单组件失败按 last-attempt 轮转，五次后隔离，避免队首饿死。
  unknown schema/checksum/hash/state/容量、同 ID 不同 payload、同 ID quarantine 都
  fail-closed；oversize quarantine 只保留有界诊断。
- completed tombstone 清除 user/assistant/facts，但保留 opaque id/hash、original 与
  projection subject、expected marker mask 和 daily storage day。load 同时核对 Daily/
  Recent owner marker；daily draft 已压缩时允许 marker 生命周期结束，recent marker
  缺失则 tombstone 隔离，不能伪报 Duplicate。
- SceneShout 冻结 scene session/target，Courier 不再启动 native dialogue session；
  adapter trace 加 process nonce，避免重启后 generation/session 序号复用误撞旧
  tombstone。non-Hero alias 迁移只 retarget projection subject；部队销毁会隔离对应
  pending/tombstone，不能由 tick 重新“复活” owner。
- 跨日请求若 origin day 已压缩，Daily 写入当前 open draft并保留 additive origin-day
  provenance；Recent 仍按冻结上下文写入。weekly trigger/notoriety 保持 owner 内的
  guarded best-effort side effect，不由恢复日志重复调用；若 core marker 发布后进程
  中断，它们可能缺失，本切片不把辅助副作用伪装成可 exactly-once 恢复。

验证：`MemoryCommitRecoveryContractTests` 覆盖六阶段、12 个 marker 前/后故障、
restart/export/import、nonce、12k CJK Courier 输入、20k reply、容量/损坏/轮转/迁移；
`ProductionOptInEntryReplayTests` 反射最终 1.4 DLL 验证 ABI、marker/readback、缺 marker
隔离、Scene/Courier provenance。Debug/Release 的 1.3、1.4、Bootstrap unified Stage
均为 0 warning / 0 error；真实 Campaign、AFEF 和旧档往返仍 NOT-RUN。

## 回滚与未验证

- 回滚点：移除 `IInteractionMemoryBatchCommitter` 接入并恢复
  `InteractionResultCommitter` 的 legacy Append 分支；旧入口仍未切换。
- 已验证：InteractionPipeline `40 cases PASS`；Policy Gateway replay 通过；1.3、1.4、Bootstrap 及 unified
  stage 构建均 `0 warning / 0 error`；未部署游戏目录。
- 未验证：真实三渠道 detached HTTP、旧存档加载后的恢复、AFEF 游戏内展示/动作
  联动，以及默认路径切换。Courier inbound 的 session completion 属于独立
  `afterCommit` owner；memory 修好后仍可能卡住，必须由下一切片的 Courier 持久
  completion receipt 解决，不能把 callback 塞进本 journal。
