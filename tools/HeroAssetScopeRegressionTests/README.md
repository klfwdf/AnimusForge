# Hero 单资产范围回归

本工具将 `.tmp/full-audit-35524b04/channels` 的一次性 F1 见证转为正式回归，不依赖该未跟踪目录。

## 覆盖与边界

- 每次直接提取当前（或指定 Git ref）生产 `TryReplayGiveAsset`、Hero 整个 replay/action 分发/金币/unknown-after-start 路径，以及 Party/Merchant 的 asset 和 gold 方法。
- 直接执行生产的 Hero 授权 wrapper、精确授权选择、transfer key、ALL 数量统计、market token 拆分、数量/金币/RP token 规则、action/result 契约，以及 `LegacyEconomyRewardDebtMainThreadPort`。不会手抄第二套被测业务算法。
- 库存获取与 Hero/Party/Settlement、实际资产转移、RP 生成、快照发布和日志只提供确定性边界 fixture；测试记录调用目标、数量、forceComplete、观察结果与确认事实，不调用游戏、不写真实存档。
- notable Hero 的身份/所在地/市场职业资格由 fixture 提供；生产 `ResolveNotableMarketSettlement` 与 `IsNotableMarketHero` 的游戏查找、全局模糊名称解析没有重演。市场 fallback 和旧红基线使用精确 fixture resolver，不能据此声称全局模糊匹配已验收。

67 个行为场景覆盖：

- 指定 item ALL 不碰其他物品，精确 modifier、中文名称、同 key 多 stack、数量饱和、空库存、未知物品、非法数量、拒绝 asset=ALL。
- finite RP literal 保真、普通 Hero / Party / Merchant 既有库存路径、GOLD 别名。
- notable Hero 的 `market@` 资产按原 Settlement owner 转移、modifier 保留、ALL 先用授权 key 计数再去掉市场前缀、已授权裸资产保留旧市场 fallback、无市场匹配的个人物品和 RP 不误走市场。
- notable GOLD 使用 Settlement 而不是 Hero 钱包；普通 Hero 保持个人 owner。
- 无效果不生成确认事实，先成功再未知保留已有事实、停止后续动作，物品/RP observation 与金币抛错后的 UnknownAfterStart。
- 同一真实 main-thread port 联动：创建线程正常执行，物理 `Task.Run` 工作线程与 stale target 不能触达资产 owner，unknown 结果不伪造回执。

`ALL` 是数量，**不是“全部资产”标识**。原 Hero `ApplyRewardTags` 和 Party/Merchant 均拒绝 `asset=ALL`。finite 未授权 label 仍按旧 Hero 规则作为 RP literal 处理，包括真实世界里存在但当前授权物品清单中没有的裸物品名；不借此重新允许全局模糊资产转移。

## 运行

```powershell
python -B tools/HeroAssetScopeRegressionTests/run.py
python -B tools/HeroAssetScopeRegressionTests/run.py --source-ref 35524b04 --output-name red-35524b04
python -B tools/HeroAssetScopeRegressionTests/run.py --mutate force-all --output-name mutant-force-all
python -B tools/HeroAssetScopeRegressionTests/run.py --mutate drop-modifier --output-name mutant-drop-modifier
python -B tools/HeroAssetScopeRegressionTests/run.py --mutate drop-observation --output-name mutant-drop-observation
python -B tools/HeroAssetScopeRegressionTests/run.py --mutate drop-market-route --output-name mutant-drop-market-route
```

默认 SDK 为 `G:\AFMOD\.dotnet-sdk\dotnet.exe`，可用 `--dotnet` 覆盖。生成物和日志隔离在 `.generated/<output-name>/`，带源码摘要；不编译到游戏目录。

当前修复应为 **67 PASS / 0 FAIL**；红基线为 **41 PASS / 26 FAIL**（exit 1）。四种 mutation 必须 exit 1：ALL 强制生成、丢 modifier、断开 mutation observation、丢 market owner 路由。预期拒绝不代表测试基础设施失败；应确认失败来自相应行为断言而非编译错误。

构建通过与这些隔离回放 **不等于真实 Hero/Party/Merchant 库存、装备、市场职业筛选和存档实机验收**。
