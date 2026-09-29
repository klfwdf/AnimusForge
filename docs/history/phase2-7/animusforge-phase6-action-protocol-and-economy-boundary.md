# 阶段 6：Action 协议与 Economy / Reward / Debt 边界

## 本切片完成内容

- `LegacyActionTagParser` 改为平衡括号扫描，保留 `[ROT]` 等嵌套 RichText
  资产名，不再以第一个 `]` 错误截断 `GIVE_ASSET`。
- `GIVE_ASSET` 的资产 token 支持内部冒号，最后一个段作为数量，保持现有
  `GIVE_ASSET:<asset>:<quantity>` 语义。
- 新增 `LegacyActionTagCatalog`，以有限的既有动作名称/协议族替代 detached
  executor 的 `ACTION:*` / `A:*` 全量通配授权；覆盖当前 Reward/Trade/Debt、
  Duel、GCCZ 数字动作、Diplomacy、WorldMap、Scene、Issue、Marriage、Courier
  等既有协议。
- 识别到但不在 allowlist 的协议标签现在会使主线程 ActionPlan 校验拒绝，避免
  raw 后处理文本和已授权计划不一致。

## 保持不变

- 只解析，不在后台解析或解析游戏对象；最终资产、数量、债务、交易和目标资格
  仍由现有 `RewardSystemBehavior`、领域 owner 和主线程执行器复核。
- 不新增玩法，不改变 `SyncData` key/type、存档类型、程序集身份、默认三渠道
  入口、Courier 时序或 Bootstrap 发布结构。

## 性能与验证

- 标签扫描只在一次交互的后处理/commit 边界执行；线性扫描输入，受最大动作数
  64 限制，不进入 Tick，不做全局对象扫描。
- `InteractionPipelineContractTests`：`40 cases PASS`。
- `GiveAssetTagCodec.StressTests`：`80557 assertions PASS`，20,000 fuzz，25,000
  pressure tags，耗时约 19 ms。
- 1.3、1.4、Bootstrap 统一 stage：均 `0 warning / 0 error`。

## 未验证与回滚

- 未验证真实 detached 三渠道 HTTP、旧存档加载后的游戏内资产/债务动作，以及
  默认入口切换。
- 回滚可移除 `LegacyActionTagCatalog` 的 detached executor 接入并恢复旧 parser
  路径；默认旧入口未被切换。
