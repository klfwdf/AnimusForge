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
  settlement 资格解析和既有领域调用必须在主线程重新校验后完成。当前已增加
  `RewardSystemBehavior.CreateEconomyRewardDebtMainThreadPortForExternal()`，
  为 Hero→玩家路径提供生产 owner adapter；本轮另增加
  `CreatePartyEconomyRewardDebtMainThreadPortForExternal()`，支持 PartyBase
  金币/物品/RP 物品转移；本轮另增加
  `CreateMerchantEconomyRewardDebtMainThreadPortForExternal()`，支持商人金币、
  物品/RP 物品和市场债务创建/解除，部队债务/固定资产仍 fail-closed。

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


## RewardSystemBehavior Hero owner adapter (2026-08-30)

`RewardSystemBehavior.EconomyReplay.cs` 增加生产 factory 与 Hero→玩家 replay
owner。factory 只在存在 live `RewardSystemBehavior` owner 时创建
`LegacyEconomyRewardDebtMainThreadPort`，并由 `TWParallel.IsMainThread()`、
当前 `Hero.Find(subjectId)` 和 `Hero.MainHero` 做边界校验。

Hero owner 复用现有 RewardSystem 的金币/物品转移、RP 物品生成、债务创建/解除
和固定资产转移方法；只在实际方法返回成功后生成 `FactRecord`。`ACTION:GIVE_GOLD`
单参数旧写法、债务期限和备注也已在 planner contract 中保留。非 Hero、商人、
部队和真实游戏对象不存在时不执行任何变更。

验证：Economy port contract 与 production owner factory replay 均 PASS；Debug
unified stage 的 1.3、1.4、Bootstrap 均 `0 warning / 0 error`。真实 Bannerlord
Campaign/Mission、live inventory/debt/settlement、三渠道 ActionPlan 游戏内执行、
旧存档和 AFEF 实机写入仍为 `NOT-RUN`。下一项是补齐非 Hero/商人/部队 owner adapter，
再接入三渠道 commit 的 Economy capability。


## PartyBase reward owner adapter (2026-08-30)

`RewardSystemBehavior.EconomyPartyReplay.cs` 增加 PartyBase→玩家的生产
owner adapter。Party 与 `BasicCharacterObject` 必须由渠道 owner 在 capture
边界提供；commit 时再次检查 expected subject、`MobileParty.IsActive`、
`Hero.MainHero` 和主线程，然后复用现有 `TransferGoldFromParty`、
`TransferItemFromParty`、`BuildPartyRewardItemResolutionContext` 和
`GenerateRpAssetToPlayer`。

该切片只授权既有部队奖励金币/普通物品/RP 物品能力；DebtCreate、DebtResolve
和 SettlementTransfer 不会被误解释为部队动作。部分成功返回明确
`economy.party_partial_replay`，只有实际 transfer/generation 返回正数量时才
产生 `FactRecord`。

验证：1.3/1.4/Bootstrap Debug unified stage 均 `0 warning / 0 error`；
`ProductionEconomyOwnerReplay` 的 Party factory 在无 live Campaign/Party 时
`partyFactoryFailClosed=1`；Economy contract 继续 PASS。真实 PartyBase、库存、
游戏内 ActionPlan、旧存档和 AFEＦ 写入仍为 `NOT-RUN`。下一项是商人
CharacterObject/Settlement owner adapter。


## Merchant CharacterObject/Settlement owner adapter (2026-08-30)

`RewardSystemBehavior.EconomyMerchantReplay.cs` 增加商人
`CharacterObject + Settlement` 的生产 owner factory。capture 边界提供商人和
定居点，commit 边界重新确认主线程、当前定居点、商人角色资格、玩家目标和
expected subject。

商人 owner 复用现有 `TransferGoldFromSettlement`、
`TransferItemFromSettlement`、`GenerateRpAssetToPlayer`、
`SetDebtForSettlementMerchant` 和 `ResolveSettlementMerchantDebtByIdByAgreement`；
只有正数量转移或债务方法返回成功时才产生 `FactRecord`。
`SettlementTransfer` 不属于商人库存/市场债务 owner，仍明确拒绝。

验证：1.3/1.4/Bootstrap Debug unified stage 均 `0 warning / 0 error`；
`ProductionEconomyOwnerReplay` 的 `merchantFactoryFailClosed=1`，Economy contract
继续 PASS。真实商人角色、Settlement.CurrentSettlement、市场库存/资金、市场
债务、游戏内 ActionPlan、旧存档和 AFEF 仍为 `NOT-RUN`。下一项是把 Hero/Party/
Merchant 三类 owner 接入三渠道 commit 的 Economy capability，并补真实状态 fixture。

## 本轮完成（2026-08-30，三渠道 Economy-aware ActionPlan commit 接入）

- ActionPlan executor 可选接入 Economy planner/port：Hero、Party、Merchant owner 由各 channel 在主线程 commit 边界选择；Economy action 不再交给旧 channel callback 重复执行，非 Economy action 保持原 authority。
- InteractionResultCommitter 消费 IActionPlanExecutionReceipt 的 confirmed facts；新增 balanced RemoveProtocolTags 工具和纯 contract fixture。
- 验证：Economy-aware executor contract、Economy port、InteractionPipeline、ProductionConfiguredHost、ProductionCourierHost、ProductionOptInEntry、ProductionEconomyOwner 均 PASS；1.3/1.4/Bootstrap Debug unified stage 均 0 warning / 0 error。
- 真实游戏 host、live economy state、旧存档和 AFEF 仍为 NOT-RUN；默认三渠道未切换。下一项是补真实状态 fixture 与 live commit 验收。
