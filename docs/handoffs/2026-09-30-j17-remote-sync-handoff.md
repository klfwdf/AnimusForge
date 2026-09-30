# J17 接续交接：远端最新状态与本地同步阻塞（2026-09-30）

> 本文件只做接续入口。当前进度与证据以远端 `HANDOFF.md` 顶部、[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md) 和[范围图](../architecture/af-framework-code-scope.md)为准；下面的远端内容是经 GitHub REST API 只读获取的，本地工作区尚未拉到这些提交。

## 1. 远端现状（`klfwdf/AnimusForge`，分支 `codex/af-main-refactor-continuation-20260831`）

- **最新提交**：`8ae0f831`（2026-09-30 11:05，RUD）`merge: integrate remote changes without unfinished WorldBulletin`。相对本地 `bc7b389b` 领先 **56 个提交**，本地不领先，无分叉；`bc7b389b` 之前本地的 E68–E71 已在远端祖先链中。
- **产品源码变化**（42 文件）：
  - von.branden：内战 v2 运行时与存档（`src/modules/AF.Module.Kingdom/CivilWar/` 新增 `CivilWarCatalog/Effects/Rules/World`，`KingdomCivilWarOwner` 大改，`DuelSettings.CivilWar.cs` 新增，`CivilWarRules.SmokeTests` 新增）。
  - Coup、Illustrator、Vengeance、`ShoutUtils.cs`（1,504 行变动）、`SettlementEntryTroopSelectionBehavior.cs`、`ExecutionAddressLlm.cs` 等调整。
  - `8ae0f831` 按用户要求**撤销未完成的 WorldBulletin**（15 个 hunk，恢复旧周报开关/布局），保留其余远端改动。远端记录：1.3/1.4 各 **0 errors / 338 warnings**、Bootstrap 0/0，CivilWarRules 与 WeeklyReportSchedulePolicy smoke PASS；游戏/旧档/扩展运行未验，未 Stage/Deploy。
- **J17-A 盘点**：范围图续到 **E119**（E72–E76 A2-5 宿主成员登记、E77–E81 MyBehavior/Romance/SceneTaunt/TroopInspection、E82–E119 各桶逐成员分区）。20 桶 A 责任签收 **7/20**：`bootstrap-build`、`host-composition`、`tools-content-package`、`runtime-diagnostics`、`action-commit`、`duel`、`policy-political`（按用户更正 `PolicySystem/` 整体排除，仅签 AF 侧 `PolicyModuleAdapter`）。
- **未签 13 桶**：game-adapter-compatibility、persistence-config、conversation-encounter、gateway-prompt-protocol、memory-afef、economy-reward-debt、world-simulation-worldmap、settlement-siege-gccz-sets、scene-mission-combat、courier-proactive-issue、social-progression-reports、knowledge-persona-profile、ui-tts-external-integration。J17-A 未闭，J17-B 未启动。

## 2. 本地现状（`G:\AFMOD\AF-J17`，分支 `codex/af-j17a-inventory-20260928`）

- HEAD = `bc7b389b` + 本文件；工作区另有 5 个未跟踪 `AnimusForge.SiegeAftermathIntervention/SiegeCouncil*.cs`（他人在制，勿动、勿提交）。
- **同步阻塞**：本机到 `github.com:443` 的 git 传输不通（直连与 `127.0.0.1:7892` 代理均失败，`codeload.github.com` 亦不通），只有 `api.github.com` 可达。网络恢复后执行：
  `env -u HTTPS_PROXY -u HTTP_PROXY git -c http.proxy= fetch origin codex/af-main-refactor-continuation-20260831`，再 `git merge --ff-only`（本文件为新增文件，若已提交则普通 merge 无冲突）。
- **本地盘点材料已被远端取代**：`artifacts/j17a/` 下 A2-5 分批结果（`_a25_b*.out.json`）、`a2-5-hosts.partial.json`、`local-pending/` 汇总是旧坐标/并行工作；远端已用 E72–E81 完成 A2-5 与 16 宿主分母对账（E79）。**拉取后不要再用本地 A2-5 结果另起 E 编号**，仅在远端证据缺项时作导航参考。

## 3. 用户决定（截至 2026-09-30，需带入后续工作）

1. J15 按用户决定结项（实机/旧档/发布遗留保留）。
2. **J17 只做 AF 主体重构**：GCCZ（`AnimusForge.SiegeAftermathIntervention/`）与 `PolicySystem/` 整体排除，不审内部、不迁移；AF 宿主中调用它们的代码仍按 AF 责任处理。
3. 授权“J17-A 后连续做 J17-B”。
4. **死代码确认完全无用即直接删除**（2026-09-30）。据此，此前标 `HOLD_USER_DECISION` 的 R06-M0（Native opt-in facade/runner 链，0 生产调用者）改为可删：删除时同批移除 `InteractionPipelineContractTests` 中测它的用例与 csproj 链接，并把 `docs/phase8/cleanup-candidates.json` 条目标 removed。被反射/测试/XML 真实消费的成员（如 `GetPassiveNpcResponse` 被 Xihai 反射、`CreateCourierInboundMemoryFacadeForExternal` 被回放反射）不属死代码。

## 4. 下一步

1. 恢复网络后拉取 `8ae0f831`，按远端 `HANDOFF.md` 顶部与主台账 §3.3 接续，不重做已签 7 桶。
2. 继续签余下 13 桶 → 按规格 §4 判 J17-A 出口。
3. 进入 J17-B：B0 删死代码（含 R06-M0，按第 3.4 条授权）→ B1 R04a（按 E42 拆子包，不整体搬 11 类）→ B2/B3 R02 → B4 R03 → B5 R01 → B6 R05 子项 → B7 `Refactor/` 迁移。每片同批改接按路径读源码的测试，双 API + Bootstrap 构建与 `tests/run_all.py` 全量 FAIL=0 后提交。
4. 推送：只普通推送同名分支（本地已有此前的推送授权记录）；不 force-push、不部署。
