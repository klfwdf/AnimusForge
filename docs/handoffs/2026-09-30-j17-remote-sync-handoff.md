# 交接：远端 `5a4ecd0d` / `8ae0f831` 合并后状态与未测清单（2026-09-30）

> 接续入口。详细证据以主台账 [远端合并与撤销 WorldBulletin](../animusforge-refactoring-and-repository-reorganization-plan.md#remote-merge-no-bulletin-20260930) 与 [范围图](../architecture/af-framework-code-scope.md) 为准。本文件补充用户在推送后口头说明的**未完成/未测项**，供下一位接手者按清单验证。

## 1. 这两个提交做了什么

| 提交 | 作者/时间 | 内容 |
| --- | --- | --- |
| `5a4ecd0d` | RUD，09-30 10:53 | `chore: checkpoint before merging remote 391ceb74`——空检查点（0 文件变更），只作回滚锚点，父为 `e127507b`（J17-A 盘点最新文档提交）。 |
| `8ae0f831` | RUD，09-30 11:05 | `merge: integrate remote changes without unfinished WorldBulletin`——合并 `5a4ecd0d` 与 von.branden 的 `391ceb74`；41 文件，+3,621/−1,535。**已由用户推送**到 `origin/codex/af-main-refactor-continuation-20260831`。 |

`8ae0f831` 的实质内容：

1. **保留** von.branden 的内战 v2（`60e9c989`/`20aca83b`/`c3eb128c`）：
   - `src/modules/AF.Module.Kingdom/CivilWar/` 新增 `CivilWarCatalog.cs`（478）、`CivilWarEffects.cs`（359）、`CivilWarRules.cs`（188）、`CivilWarWorld.cs`（169）；`KingdomCivilWarOwner.cs`（+387/−566）、`KingdomCivilWarState.cs`、`CivilWarModuleAdapter.cs`、`CivilWarCampaignBehavior.cs` 改写；`ICivilWarModulePort` 加 3 行。
   - 存档键升到 **`_af_kingdom_civil_war_v2`**（`MyBehavior.cs:17983,18403`；模型见 `KingdomCivilWarState.cs:6`，只存 id 与数字）。v1 键不再读取——**旧档内战进度不迁移**。
   - 新 MCM 分组 **“12b. 内战派系”**（`DuelSettings.CivilWar.cs`，GroupOrder 125）：启用内战派系（默认开，且依赖“启用王国稳定度与叛乱”）、玩家为国王时也形成派系（默认关）、结果随机度、成派不满阈值 35、最后通牒间隔 2 周、内战最长 12 周、派系冷却 8 周。
   - 每周推进入口 `MyBehavior.cs:12909` `TeamModuleServices.CivilWar.AdvanceWeek(...)`；叛军王国创建回调 `QueueCivilWarRebellionForExternal :12982` → `NotifyRebelKingdomCreated :13137`；战况面板派系页 `WarStats/AfWarStatsPopupVM.cs:1297` 与 `AfWarStatsFactionKingdomVM.cs`。
   - 新 smoke：`tests/modules/AF.Module.Kingdom/CivilWarRules.SmokeTests/`。
2. **保留** 远端其余改动：Coup（`CoupCampaignBehavior` +93/−41、`CoupSession`、`CoupMissionBehavior`、`SettlementEntryTroopSelectionBehavior` 两处）、Illustrator 9 文件、Vengeance 4 文件、`ExecutionAddressLlm.cs`、`ShoutUtils.cs`（752 行等量替换）、`SceneTauntBehavior.cs`、`ShoutBehavior.cs` 小改。
3. **撤销未完成的 WorldBulletin（即时快报）**：反向应用 15 个 hunk（`DuelSettings.cs` 1、`MyBehavior.cs` 12、`MyBehavior.MemoryRecovery.cs` 1、`CivilWarCampaignBehavior.cs` 1）：
   - 删除 MCM 项“启用即时快报（替代旧周报）”`UseWorldBulletin` 与 `IsLegacyWeeklyAutoGenerationActive()`；自动周报重新只受“每周自动生成周报”`AutoGenerateWeeklyReports`（`DuelSettings.cs:2324–2326`，分组“12. 事件系统（开发）”）控制。
   - 恢复旧周报的未读筛选和三栏布局（`useChronicleColumns: hasFullReport`）；去掉 bulletin 的事件注册/重置/主线程 tick/存档调用。内战事件材料记录保留。
   - 已跟踪 C# 中不再含 `WorldBulletin / IsLegacyWeeklyAutoGenerationActive / retainedBulletinIds / isBulletin`。
4. **远端记录的离线验证**：参考线 `v1.3.15.110062` / `v1.4.7.117484`；1.3、1.4 各 **0 errors / 338 warnings**，Bootstrap 0/0（隔离输出 `bin|obj/remote-merge-391ceb74-no-bulletin/`，日志 `.tmp/remote-merge-391ceb74-no-bulletin/`）；`CivilWarRules`、`WeeklyReportSchedulePolicy` smoke PASS。未 Stage/Deploy，未跑原构建脚本的目录清理。原 22 个未跟踪目录（1,527 文件）未入库。

## 2. 用户明确说明未做 / 未测（必须逐项验证）

| # | 项 | 现状 | 验证要点 |
| --- | --- | --- | --- |
| U1 | **MCM 合并未做** | 各功能仍是独立 MCM 设置页：AF 主体 `AnimusForge_global_settings`（`DuelSettings`，含新“12b. 内战派系”）、`AnimusForge_Coup_v1`（显示名“AnimusForge - 宣权篡位”）、`AnimusForge_DialogueUI_v1`、`AnimusForge_Illustrator_v1`。 | 若要合并，须保持各设置的**存储 Id 与属性名**（玩家已存 MCM 值依赖它们），迁移旧值；按 `docs/bannerlord_dual_module_output.md` 保持单模块。未经确认不要改 Id。 |
| U2 | **周报“第二种模式”未测** | 周报有两种输出：`WeeklyReportOutputMode.FullReport`（三栏全文）与 `TitleShortTagsOnly`（标题+短标签），定义 `MyBehavior.cs:79–83`。选择逻辑：`WeeklyMaterialBatchPlanner.SelectFullReportKingdomIds` 按玩家距离取最近 **3** 个王国出全文（`src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs:9–25`），其余王国走短报；世界组恒为全文（`:66–75`）。`WeeklyPromptMaterialOwner.Prepare`（`:12–20`）按 `shortOnly` 设模式与素材；弹窗按 `useShortReportLayout: !hasFullReport` 渲染（`MyBehavior` 周报弹窗调用处）。 | **无 MCM 开关**——不能靠选项切换。需实测：让 ≥4 个王国同周有素材，确认远处王国生成短报、`ShortSummary` 写入并通过 `IsWeeklyReportCommitWinner`（`MyBehavior.cs:42284–42296`）判胜、弹窗走短报布局、未读筛选与阅读经验正常；再测全文王国对照。开发者测试可用开发者周报预览（`DevWeeklyReportPopup`）核对两批次 `batch_mode=title_short_tags_only/full_report`（`MyBehavior.cs:43734`）。 |
| U3 | **可定位隐藏选项**（用户建议） | 与周报相关的可见 MCM 项在“12. 事件系统（开发）”分组（`DuelSettings.cs:2316–2346`：长度预设、每分钟请求数、每周自动生成、字号、地图通知、阅读经验等），以及“1. AI 核心配置/4. 事件与王国叛乱API”（`:1832–1900`）。被删除的 `UseWorldBulletin` 曾在同一“12.”分组 `Order = 10`。 | 若需在 MCM 中暴露“短报/全文”或“全文王国数量（当前写死 3）”，应作为新设置加到“12. 事件系统（开发）”分组并在 `SelectFullReportKingdomIds` 读取；属行为变更，须用户批准。先用上面的定位做实测，不要复活 WorldBulletin。 |
| U4 | **内战叛乱派系未测** | 规则与状态已在 `AF.Module.Kingdom/CivilWar`；只有 `CivilWarRules` 单元 smoke 通过。 | 实测：开启“启用王国稳定度与叛乱”+“启用内战派系”；制造不满（处决、封地落选、强推政策/战争/和约、领地遭劫）→ 家族不满达阈值成派 → 按“最后通牒间隔”递交诉求 → 拒绝后升级为内战（临时叛军王国+真实战争）→ 最长周数强制结算 → 冷却期不再成派。检查战况面板派系页、玩家为国王时的开关、`BlocksNewOffensiveWar` 对新开战的阻断、定居点忠诚度 delta、存读档 `_af_kingdom_civil_war_v2` 往返。**旧档（v1 键）内战状态不迁移**需确认可接受。 |
| U5 | **篡权政变（宣权篡位）未测** | Coup 以内置方式编入 AF（`IntegratedModuleHost.cs:34` 注册）。入口：城镇菜单“宣权篡位”（`CoupCampaignBehavior.cs:104`），条件——玩家家族属某王国但非统治家族、身处城镇、Coup 启用、存档记录有效、Mission 保护与 AF 接缝（`SettlementEntryTroopSelectionBehavior`、`CoupRebellionBridge`）可用（`:118–140`）；失败有“重试政变结算”（`:105`）。存档键 `_afCoupSession_v1`、`af_coup_detentions_v1`、`_afCoupRebellionBridge_v1`、`_afCoupOutcomeBridge_v1`。 | 实测：在国王所在城镇发起（街道 ≤60、大厅 ≤20 人）→ 成功夺位 / 失败带地叛离并开战两分支 → 拘押、结算重试 → 存读档。另测**独立 Coup 模组同时启用**时的互斥（宿主探测）与每帧反射探测开销（R08-b）。与内战派系的交互（政变失败叛离是否计入派系/内战）需一并观察。 |

## 3. 其它仍未验证（沿用远端记录）

- 游戏实机、旧存档、扩展模块运行、独立+内置双模块互斥均未验。
- 被移除的 `_af_xihai_legacy_equipment_cleanup_v1` 与内战 v1→v2 键的旧档兼容未验。
- J17-A 仍未闭（20 桶签 7：bootstrap-build、host-composition、tools-content-package、runtime-diagnostics、action-commit、duel、policy-political），J17-B 未启动；最新证据到 E119。

## 4. 本地工作区（`G:\AFMOD\AF-J17`）

- 已普通合并远端 `8ae0f831`，无冲突；HEAD 为合并提交 + 本文件。5 个未跟踪 `AnimusForge.SiegeAftermathIntervention/SiegeCouncil*.cs` 是他人在制，勿动勿提交。
- `artifacts/j17a/` 下本地 A2-5 分批结果已被远端 E72–E81 取代，只作导航。
- 网络：本机 git 走 `127.0.0.1:7892` 代理时不通；用 `env -u HTTPS_PROXY -u HTTP_PROXY git -c http.proxy= fetch ...` 直连（本轮已成功）。

## 5. 用户决定（带入后续）

1. J15 按用户决定结项；J17 只做 AF 主体重构，GCCZ 与 `PolicySystem/` 整体排除。
2. 授权 J17-A 后连续做 J17-B；**死代码确认完全无用即直接删除**（被反射/测试/XML 真实消费的不算死代码）。
3. 推送只普通推送同名分支，不 force-push、不部署。

## 6. 建议下一步

1. 先做 U2/U4/U5 的实机验证（需要游戏环境与部署授权；部署前按单模块规则备份原模块）；U1、U3 为待用户决策的功能项。
2. 重构线：按远端 HANDOFF 顶部与主台账 §3.3 继续签余下 13 桶 → J17-A 出口 → J17-B（B0 删死代码 → B1 … B7）。
