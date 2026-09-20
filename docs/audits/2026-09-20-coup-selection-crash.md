# 政变选兵“全部转入”崩溃修复

当前阶段：总结。检查点 `8533df8`，生产修复与相关回归源码 `63de43234287a905a980ae1edb12c8eda04c0e10`。仅改独立政变子模组及测试，未修改/覆盖 AF 主体或其他子模组。

## 实机证据与原因

游戏 v1.4.8，原部署 DLL SHA256 `9353774FE1FB0A3D749094A5C7358CCB422F74E55E1E0789E05967AE9C7203D1`。`rgl_log_14300.txt` 显示 07:59:12.428 选兵界面已打开，07:59:13.649 执行 `ExecuteTransferAllOtherTroops` 发生空引用。

WER `TaleWorlds.MountAndBlade.Launcher.exe.14300.dmp`（145,412,553字节，07:59:28）中，线程27108的内层 `NullReferenceException` 方法栈为：

```text
PartyCharacterVM.InitializeUpgrades
PartyCharacterVM..ctor
PartyVM.InitializePartyList / InitializeTroopLists / Update
PartyScreenLogic.TransferAllTroops
PartyVM.TransferAllCharacters / ExecuteTransferAllOtherTroops
```

旧选兵数据将 `RightOwnerParty` 置空。真实游戏 `TaleWorlds.CampaignSystem.ViewModelCollection.dll`（MVID `e3da507e-5119-485a-84a1-f8172483aa6a`）在 `InitializeUpgrades` 的 IL_00b5/IL_00ba 读取 `RightOwnerParty.ItemRoster`，到 IL_0272 才读“禁止升级”标志。右侧最初为空，所以能打开界面；有升级目标的普通兵转入后便在刷新中崩溃。单兵/整组转入也有同一风险，不能只禁用全选。

仓库1.3和1.4.5参考源码亦存在该顺序。转储仅读取异常类型/方法栈，不读取局部变量或无关堆内容；线程最后异常与rgl记录吻合。证据保存在 `artifacts/coup-selection-crash-20260920/managed-crash-evidence.json` 和该目录README。

## 修复坐标与职责

源码提交均为 `63de4323`：

- `extensions/AnimusForge.Coup/src/CoupSystem/CoupTroopSelection.cs:16–27`，`Open`：从主队取得原版UI所需上下文；初始化后复制 `CurrentData.RightItemRoster`，取消操作只恢复界面快照，不清空/重写玩家真实背包。
- 同文件 `:30–80`，`CreateInitializationData`：绑定 `RightOwnerParty` 与 `RightLeaderHero`，四份兵员/俘虏列表仍使用独立临时名册；拒绝无效owner、误传主队真实名册及非法上限，禁用升级和金钱交易。街道和大厅选兵共用这条修复。
- `tools/Coup.RuntimeProbe/SelectionRegression.cs:21–126`，`Run`：对实际生产DLL的数据工厂做29项绑定、隔离、人数与回调检查；`Program.cs`接入并明确验证边界。

性能变化只在打开选兵界面时复制一次物品快照，无每帧扫描。王权、战斗和叛乱规则未改。

## 验证、部署与剩余事项

- 独立模块 1.3/1.4 Release：各0警告、0错误。
- 已部署同一DLL的选兵数据工厂检查：**29 PASS**。使用真实managed类型的受限fixture，没有创建Game/Campaign/PartyVM，也没有假造游戏API；这不是实机全选验证。
- 真实程序集离线Harmony注册仍为44目标、42 prefix、2 transpiler，4项可用标志全部true。
- 2026-09-20 **08:10:22（UTC+8）** 已覆盖游戏 `Modules/AnimusForge_Coup` 的4个文件，逐文件哈希一致；AF 1.3/1.4原DLL哈希均未变。覆盖前已确认游戏进程退出，未启动游戏或调用模型。
- 新DLL SHA256：`9141B301EC8481F45F38C4DE2999E26DCDB1900FE4BE3805E82CADAB997BA920`；MVID：`e548649d-8d0b-4590-892e-e87c42afc682`。

仍待实机：重启游戏后，以有升级目标的普通兵分别尝试单个/整组/全部转入、退回、确认和取消；核对主队兵员/物品不变，再尝试大厅选兵。此前的32项状态检查和注册检查没有覆盖原版升级信息刷新，不能据此宣称旧版选兵已通过。

本机验证日志为 `artifacts/coup-selection-crash-20260920/build.log`、`deploy.log` 和 `deployed-probe/registration.log`。回滚文件位于 `artifacts/deploy-backups/AnimusForge_Coup/20260920-081022/module`，其中保留的就是本次已确认存在崩溃的旧版；源码使用针对 `63de4323` 的逆提交，勿回滚其他任务改动。
