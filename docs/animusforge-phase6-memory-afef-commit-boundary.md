# 阶段 6：Memory / AFEF 统一写入边界

## 本切片目标

把 detached 三渠道的历史与确认事实写入收敛到一个提交契约，同时保留旧
`MyBehavior` 作为存储权威。此切片不切换 Native、SceneShout 或 Courier 的默认
入口，不改变 `SyncData` key/type、存档类型或已有历史格式。

## 契约

- `InteractionMemoryCommit`：携带稳定 `commitId`、渠道/session/subject，以及可选
  user、assistant 和 confirmed AFEF facts；不携带 Hero、Agent、Campaign、凭据或
  可变配置。
- `IInteractionMemoryBatchCommitter`：可选的新边界；旧的 `IInteractionMemory.Append`
  保留作为兼容 fallback。
- `MemoryCommitResult`：区分 Applied、Duplicate、Rejected、Failed。
- `MemoryCommitReceiptCache`：仅进程内、有界 512 项，不进入存档；在 batch facade
  确认写入成功后及 committer 执行动作前用于 detached 重复回调抑制。写入失败不
  登记 receipt，允许同一 commit 重试。

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

- Commit 只在一次交互的主线程 commit 边界执行，不进入 Tick、规则扫描或后台
  generation 循环。
- receipt cache 为固定上限 512 的 O(1) 哈希查找；不读取或修改存档。
- facade 只保留稳定 HeroId 或 non-Hero memory id，提交时才解析 Hero。
- legacy history/AFEF 写入成功后才登记 receipt；旧 owner 抛出异常时返回
  `Failed(legacy_memory_append_failed)`，不会把失败写入永久伪装成 `Duplicate`。

## 回滚与未验证

- 回滚点：移除 `IInteractionMemoryBatchCommitter` 接入并恢复
  `InteractionResultCommitter` 的 legacy Append 分支；旧入口仍未切换。
- 已验证：InteractionPipeline `40 cases PASS`；Policy Gateway replay 通过；1.3、1.4、Bootstrap 及 unified
  stage 构建均 `0 warning / 0 error`；未部署游戏目录。
- 未验证：真实三渠道 detached HTTP、旧存档加载后的运行时写入、AFEF 游戏内
  展示/动作联动，以及默认路径切换。
