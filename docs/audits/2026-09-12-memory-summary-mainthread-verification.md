# 压缩记忆结果主线程提交验证（2026-09-12）

## 结论

源码 `9040d184998120d3476426337160403f08fdfee9` 相对来源 `e40c92d72524b7ea80a5dc0e36dc996963a62e66` 完成离线、双版本构建和实际 DLL 层验证。结论只覆盖压缩记忆 post-await 结果接受/提交边界；`LIVE=NOT_RUN`、`SAVE=NOT_RUN`、真实 provider `NOT_RUN`。

## 专用红绿与突变

| 案例 | 结果 | 证明范围 |
|---|---:|---|
| 当前 `MemorySummaryMainThreadBoundaryTests` | PASS，17/17 | 后台只发布；主线程一次提交；每 tick 上限 2；owner/Campaign/generation；reset；错误隔离；错误线程拒绝；旧 owner 不悬挂 |
| `--original` | 预期 FAIL（exit 1） | 从 Git 精确读取 `e40c92d7:MyBehavior.cs`，确认 await 后直接 writer 且无新边界 |
| `--mutate ignore-generation` | 预期 FAIL（exit 1，2 项） | stale 接受与已 stale worker 均被测试捕获 |
| `--mutate ignore-owner` | 预期 FAIL（exit 1，2 项） | replaced owner 接受与旧队列悬挂均被测试捕获 |
| `--mutate unbounded-drain` | 预期 FAIL（exit 1，2 项） | EngineTick 无界 drain 被测试捕获 |

测试链接真实生产 `MyBehavior.MemorySummaryMainThread.cs` 与 `SaveRuntimeGuard.cs`，Campaign/TWParallel 为 fixture；它不加载 Bannerlord。

## 相关回归

| 套件 | 结果 |
|---|---|
| `NativeHistorySnapshotTests` | PASS，852/852；owner data/network stubbed |
| `NativeHistorySnapshotTests --native` | PASS，27/27；真实 capture/accept 与 renderer，game stubbed |
| `MemoryFailureUiBoundaryTests` | PASS，85/85；UI stubbed |
| `MemoryCommitRecoveryContractTests` | PASS；ordered/retry/quarantine/overflow/receipt 契约 |
| `WeeklyMemoryMaterialOutcomeContractTests` | PASS；fingerprint/duplicate/conflict/load/idempotency 契约 |
| `TeamModulePortParityTests` | PASS，308 source-linked assertions；3 个行为突变预期失败 |
| `ModuleFrameworkApiTests` | PASS，119 API assertions、256 concurrent reads、预期 `CS0122` 拒绝 |

`NativeHistorySnapshotTests/source-review.json` 和 `TeamModulePortParityTests/reviewed-native-admission-deltas.json` 只刷新 `ResetLocalTransientRuntimeForLoadedSave`、`ProcessMemorySummaryQueueAsync`、`OnEngineTick`、`ClearAllDataForCurrentSave` 四个已审声明的精确 SHA；未把其他 host 差异加入豁免。

## 存档与模块身份

相对本切片来源 `e40c92d7` 的只读审计：SyncData 146/146、CampaignBehavior 36/36，无增加/删除；`SubModule.xml` 仍为 `Name/Id=AnimusForge`，仅加载 `AnimusForge.Bootstrap.dll`。默认较早基线 `d4cb1467` 会正确报告战争统计既有 47 个新增绑定和 1 个 behavior；那是来源基线之前的已合入历史，不属于本切片。

## 构建、Stage 与实际产物

既有 `build_single_module.ps1 -Stage` 在最终源码上分别运行 Debug、Release；Bannerlord 1.3 参考 `v1.3.15.110062`，1.4 参考 `v1.4.6.115628`。两套均为 1.3/1.4/Bootstrap 0 error，项目内统一模块 Stage PASS，并明确输出 `no game directory was modified`。每个项目仅有 2 个 `NU1900`：沙箱无法获取 nuget.org 漏洞元数据，不是编译错误。

| 产物 | SHA-256 |
|---|---|
| Debug 1.3 `AnimusForge.dll` | `cf4b1d2556c1f07d19f484775f4da15bd8d77206b8a5213441cd5c47e209acf0` |
| Debug 1.4 `AnimusForge.dll` | `732a793571e0ccb0f1f0034325dfa3e36ad92601c8b3f370976129ca546bac94` |
| Debug `AnimusForge.Bootstrap.dll` | `f99d5d94becc85a2438cf458751ca79fb57cc85a522842db59df7f163126c92f` |
| Release 1.3 `AnimusForge.dll` | `0854d673081ff418db96e7e2d29cba8438ec92f0ac8aa4ca00496600bb1d155a` |
| Release 1.4 `AnimusForge.dll` | `51579c1d6a4c482dbb50ba52b40f18df6cade706c2056cb580113af23fe46edc` |
| Release `AnimusForge.Bootstrap.dll` | `f5985d11bec75afb13c27585d5757d73881ba6f6d81cb4437fcd7f7d6ba20617` |

六份校验值均来自最终构建后的文件读取，并与同目录 build marker 的记录一致。四份实现 DLL 另通过 532 个 PE 元数据断言；未加载 CLR/Bannerlord host。

## 未覆盖

- 首次 await 前的调度快照与三个 Execute job 的 live prompt/目标准备。
- 逐任务 source revision/fingerprint 以及来源变更拒收。
- 真实 Campaign/Mission、provider、旧存档、读档晚返回、玩家可见提示和帧耗时。
- Courier prepare、公共写 API、默认迁移、制作组玩法业务与阶段八最终清理。

因此状态是 `OFFLINE_COMPLETE / LIVE_SAVE_PENDING`，不是整套 memory、Native、三渠道或阶段八 DONE。
