# 阶段 6：Economy / Reward / Debt detached replay 边界

## 本切片目的

为现有 `RewardSystemBehavior` / `MyBehavior` 建立 detached 管线可以使用的
字符串/稳定 ID-only capability，不替换领域 owner，也不让后台线程直接执行
资产、债务或定居点变更。

## 已建立的边界

- `EconomyRewardDebtCapabilityIds` 固定五项能力：
  `give_asset`、`give_gold`、`debt_create`、`debt_resolve`、
  `settlement_transfer`。
- `LegacyEconomyRewardDebtAdapter` 把现有 `ActionPlan` 投影为
  `EconomyRewardDebtReplayPlan`：
  - `[ACTION:GIVE_ASSET:<asset>:<quantity>]` 保留完整资产 token（包括
    `[ROT]` 和内部冒号）以及数量；
  - `[ACTION:GIVE_GOLD:target:amount]` / `GIVE_ITEM` 只保留目标和数量；
  - `[AD:value:days:N|P:note]` 保留金额、期限和 N/P 类型；
  - `[ADP:debtId]` 保留债务 ID；
  - `[ACTION:SETTLEMENT_TRANSFER:direction:settlementToken]` 保留方向和
    定居点稳定 token。
- 缺少 capability、非法金额/期限/数量、缺失 debt/settlement token，以及对
  当前 Economy 域不适用的动作，都返回明确 `exclusionReasons`；不会从排除项
  生成 confirmed AFEF。
- `IEconomyRewardDebtMainThreadPort` 是执行端口：实际 Hero、库存、债务、
  settlement 资格解析和既有领域调用必须在主线程重新校验后完成。当前切片不
  提供生产执行实现，因此不改变默认三渠道。

## 保持不变

- `RewardSystemBehavior`、`MyBehavior` 仍是资产、债务和固定资产转移的唯一
  authority。
- 不改变存档类型、程序集身份、SyncData key/type、AFEF 文本协议、Courier
  时序、默认 Native/SceneShout/Courier 入口或 Bootstrap 发布结构。
- 不新增玩法；本切片只把既有动作协议纳入可测试的 detached contract。

## 性能与线程边界

规划器只在一次交互的后处理/commit 边界运行，对已生成的有限 ActionPlan 做
线性遍历；不进入 `ApplicationTick` / `EngineTick`，不扫描 live 游戏对象，不
做反射。动作数量仍受上游 parser 的 64 项上限约束。执行端口只允许主线程
消费当前快照，并在执行前重新解析 live 状态。

## 验证

- `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj`：`39 cases PASS`。
- 阶段 2/3/4 独立 runner：全部 PASS。
- `build_single_module.ps1 -Stage`：1.3、1.4、Bootstrap 均 `0 warning / 0 error`。
- 真实 HTTP、旧存档运行时资产/债务执行和游戏内回放：`NOT-RUN`；默认路径未切换。

## 回滚点

删除本切片的 contract/adapter/test include 和本文件即可；它没有替换旧入口，
旧路径无需回滚。构建/覆盖/推送脚本和游戏目录未修改。


## Main-thread replay port contract (2026-08-30)

新增 `LegacyEconomyRewardDebtMainThreadPort`，实现
`IEconomyRewardDebtMainThreadPort` 的 fail-closed 边界。它只负责确认主线程、
快照目标仍有效、计划是否含有阻断性 capability/参数排除、空计划、领域回调
异常和 `AppliedCount` 合法性；实际 Hero、物品、债务、定居点解析与 Campaign
变更仍必须由 `RewardSystemBehavior` 或对应领域 owner 回调完成。

因此本切片没有复制或替换 `ApplyRewardTags`，也没有把 detached worker 结果
直接当成游戏事实。非经济动作的 `economy.action_not_applicable` 排除不会阻断
同一 ActionPlan 中的有效经济动作；能力缺失、经济参数非法、主线程/目标失效
和领域异常都会明确拒绝。

验证：`tools/EconomyRewardDebtPortContractTests` 覆盖有效 replay、非主线程、
目标过期、capability fail-closed、非经济排除、无适用动作、领域异常和计数
校验，全部通过；1.4 production implementation 编译通过。尚未完成
`RewardSystemBehavior` 生产回调接线、真实资产/债务/定居点校验和游戏内动作验收。
