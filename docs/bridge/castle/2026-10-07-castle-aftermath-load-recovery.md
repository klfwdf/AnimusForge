# 城堡战后菜单读档恢复（2026-10-07）

## 范围与证据边界

用户授权在不破坏其他功能前提下修复，并明确以 `G:/AFMOD/AF-GCCZ-SYNC-20261006` 为融合目录、`G:/AFMOD/GCCZ` 为独立源；历史 `new-` 不存在，不重建旧目录。初次修复未推送；后续用户明确授权基于最新main只融合本修复并推送，仍不部署、不修改玩家存档。

报告中的“清空 GCCZ 临时账本导致原生宽恕崩溃”没有转储或实际加载 DLL 佐证，不按此推测删除 reset。报告 v1.3.14 / AF v1.3.7.0 与本地候选不同；最后的 castle_outside 日志出现在 OnInit 调用前，不能证明其初始化已完成。

可从仓内原版参考及 AF 消费者确认的缺口：PlayerEncounter.OnLoad 只在 InsideSettlement 且无 Battle（或村庄等待）时创建 LocationEncounter；城堡战后菜单可以恢复在城外。AF 的 SiegeInterventionEntryCondition 又要求 LocationEncounter 非空。这是本片修复的缺失上下文路径，不等价于确认报告崩溃根因。

## 最小修复

- 在原有 game_load_finished 清理之后，仅对三个原版战后菜单中的城堡检查；要求当前 PlayerEncounter 的城堡、遭遇对象和主队驻地不冲突、LocationComplex 存在、无 Mission/战斗/主队 MapEvent、玩家非俘虏且 LocationEncounter 确实缺失。
- 只创建 `new CastleEncounter(castle)` 并赋给原生 LocationEncounter。已有实例（含随行名单）不覆盖。城镇、村庄、普通城堡访问、战斗、俘虏及不匹配的遭遇均不恢复。
- 不调用 EnterSettlement、StartSettlementEncounter、Finish、ApplyAftermath；不伪造攻城者/原领主/贡献，不重放宽恕或改变俘虏、关系、士气、领地和存档键。
- 保留 GCCZ 原有 reset，避免旧 Agent/会话/账本跨档复活。进入 GCCZ、换档/新档、转入无关菜单或城堡返回完成后清理诊断作用域。
- 使用现有 Logger.LogImmediate 添加最多24条 `[CastleAftermathLoad]` 阶段日志：load_finished/location_restored/location_preserved、native_mercy_begin/returned/failed、menu_init_begin/returned/failed；附缺失状态和托管异常原堆栈。Harmony finalizer 原样返回异常，不吞错、不改原版执行顺序。不保证捕获原生崩溃；缺少 returned 的末条日志仅辅助定位。

## 验证

- 23 条新增纯策略行为断言：恢复主路径、总结页、重复恢复、已有实例、非城堡、不匹配、缺资源、Mission/战斗/俘虏和非目标菜单。
- GCCZ 独立现有完整测试：2507 条 `ok`，PASS（工作树包含原有 Council 草稿测试，未将其纳入本片提交）。融合 core 专项：134 条 PASS。
- 原 `scripts/build/build_single_module.ps1` Debug 1.3（引用 v1.3.15）/1.4（引用 v1.4.6）+ Bootstrap 通过、0错误；既有 warning 保留。原构建内双接缝和注册门禁通过，不代表本片真实 Harmony/游戏验收。
- 双侧策略和新增测试内容一致；定向冲突/废路径搜索、git diff --check 通过。没有替换旧业务实现，未发现本片引入的废代码；正常结算与 reset 有活跃消费者，全部保留。
- 历史框架 code-map 校验在执行前置 revision 解析时失败：本仓没有 `7c5c7cd1ad58cf7fae0d34ace0e067a525b2f624`。未修改该历史地图冒充通过，本片使用下表与本地证据单独定位。

融合本地证据：`artifacts/castle-load-recovery-20261007/`，包括 baseline、两个测试日志、build-debug.log 与 verification.json。

## 工程师/玩家视角

工程师自审：修复只发生于加载完成的主线程，完整匹配后创建暂态包装；无后台对象读取/新Tick/存档/玩法变更。诊断 prefix 不返回控制原流程的 bool，finalizer 保留原异常；没有全局兜底跳菜单或重复结算路径。

玩家视角仅源码推演，尚未游戏内实测。用存档副本验收：
1. 城堡战后处置前存档再加载；若缺上下文，记录 location_restored，并检查“亲自进城决定”能按原场景条件启用。
2. 选择原版宽恕并继续，检查 native_mercy 与 summary/castle_outside 阶段成对返回、原版后果仅一次。
3. 已有 LocationEncounter/总结页读档、正常不读档的宽恕、城镇/村庄/战斗/俘虏场景不受影响。
4. 若仍崩溃，保留完整日志和转储及实际加载 DLL；本片不宣称修复任意不完整/损坏存档，也不强制推进缺失结算。

## 代码坐标（融合候选；符号优先于行号）

- `AnimusForge.SiegeAftermathIntervention/SiegeCastleAftermathLoadRecoveryPolicy.cs:6`：恢复资格；独立镜像位于 `src/AnimusForge.SiegeAftermathIntervention/Castle/`。
- `src/bridges/Siege/Host/CastleAftermathLoadRecoveryBridge.cs:27,83,114`：加载完成的真实对象校验/唯一赋值、菜单诊断、宽恕诊断。
- `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.cs:617`：清理后真实加载调用；PrepareInterventionEntryRuntime / ResetAftermathRuntimeGuards 清理诊断生命周期。
- `src/bridges/Siege/Host/Patch_SiegeAftermath_AFIntervention.cs`：原 bootstrap 注册两个只观察的 Harmony patch；无处置动作替代。

## 回滚

初次修复检查点：独立 GCCZ `0915c55`，融合 `b5f36b8d`；原产品提交独立 `dc2acc8`、融合 `68c2bf10`。main融合基线 `52177454b911250acf5b32ec8fac9aaf3040e608`，只移植城堡片，不合并旧本地分支。最终产品提交记录在交付回复/verification.json。仅按本片提交做 focused revert，不 hard-reset、不覆盖其他作者改动。实际加载报告版本与候选仍需映射后再单独授权部署。
