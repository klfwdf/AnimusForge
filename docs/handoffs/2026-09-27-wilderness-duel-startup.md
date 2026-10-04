# 野外决斗启动修复（2026-09-27）

## 当前模块化发布验证（2026-10-05）

- 用户明确要求保留此修复并推送远端。修复位于 `src/modules/AF.Module.Duel/Host/DuelBehavior.cs`，回放位于 `tests/modules/AF.Module.Duel/ProductionDuelOutcomeReplayTests/Program.cs`；下方根目录路径属于 9 月 27 日历史上下文。
- 以远端 `c8e68748` 为基准，只覆盖本修复源码、对应回放和本说明三个文件；没有恢复旧外交或其他未提交工作。
- 使用原 `scripts/build/build_single_module.ps1` 构建 Debug 双实现与 Bootstrap，参考版本为 1.3.15 / 1.4.6；两实现各 341 warning / 0 error，Bootstrap 0 / 0。两个实际 DLL 的决斗契约回放 `passed=35 failed=0 variants=2`。
- 启动检查每 0.1 秒运行一次，仅持续至部署完成或原有 30 秒超时；不增加全量扫描、反射或逐帧日志。
- 验证仅为源码边界与实际 DLL 契约；未部署游戏、未修改真实存档、实机场景加载仍 NOT_RUN。

## 范围与证据

- 主工作区 `E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`，分支 `main`，源码基线 `2ccca1ea`，意图检查点 `d7ae2244`。
- 游戏日志连续五次记录 `WildernessDuelBattleMissionLogic.AfterStart → Mission.SetMissionMode → MissionScreen.GetSpectatingData` 空引用，随后 AF 主动中止 mission；最后一次为 04:03:44，对手 `lord_5_10`。
- 实际运行版本 `v1.4.7.117484`。已反编译安装目录中的 MissionState、MissionScreen、DeploymentMissionController、BattleDeploymentMissionController、SandboxBattleInitializationModel，以及已安装 AF 的 DuelBehavior，核对实际代码而非仅引用旧版源码。
- 已安装 AF DLL SHA256：`E653449821882D78687D5673C7F2B6DB1399714E4328C3143043D097F1159A6E`。本轮结束核对仍一致，没有部署。
- 原版 MissionState.FinishMissionLoading 先执行 Mission.AfterStart，再执行 OnMissionLoadingFinished；后者才由 MissionScreen.InitializeMissionView 赋值 Mission.InputManager。AF 在 AfterStart 强制 Deployment → Battle 会提前触发镜头对输入的访问。这是与堆栈相符的生命周期错误；日志本身没有原版内部故障行号。
- 1.4.7 的部署控制器在 SetupTeams/FinishDeployment 操作 InitialPlayerAgent，结束部署才恢复玩家控制。不能把“MainAgent 已就绪”放在调用 FinishDeployment 的前置条件里。
- 原版允许部署的战斗需要结束完整部署流程；不允许部署的小规模战斗可自动完成。玩家可控兵力至少 20 是原版允许部署的条件之一，不能仅凭玩家军团总兵力推断所有现场分支，也未证明其他模组是根因。

## 源码职责

- `DuelBehavior.cs:217–223`，WildernessDuelBattleMissionLogic.AfterStart：只缓存原版 DeploymentMissionController，不再切换 mission mode。
- `DuelBehavior.cs:315–368`，OnMissionTick：每 0.1 秒尝试一次启动阶段检查；完成后不再检查。完整部署完成后才等待 MainAgent/目标和继续原有决斗逻辑，保留 30 秒超时和失败结果归属。
- `DuelBehavior.cs:395–444`，TryCompleteNativeDeployment：等待输入及 TeamSetupOver；原版已进入 Battle 时直接通过，否则调用 FinishDeployment，让原版恢复人物显示、玩家控制、AI、部署回调和战斗模式。部分失败后标记 Unknown/Abort，禁止再次尝试和结算。
- `tools/ProductionDuelOutcomeReplayTests/Program.cs:1626–1645`：检查两个实际编译 DLL 的调用关系，禁止 AfterStart 提前切模式，要求原版部署入口及输入/队伍就绪检查，并锁定结束部署先于 MainAgent 获取的顺序。
- 新增检查只在独立野外决斗启动期间运行，无反射或新增人物全量扫描，无逐帧日志。原有定居点、竞技场决斗、死亡/赌注/债务结算规则和存档契约没有修改。

## 验证与限制

- 主工作区直接构建被已有未跟踪 `third_party_analysis/Retinues-2.0.0.12-decompiled` 文件误入编译阻断，37 个错误；没有修改或删除这些文件，没有修改构建脚本/项目配置。
- 使用仓库内既有干净副本 `_codex_tmp/duel-entry-20260927`，以相同提交源码加本次两处改动，通过官方 unified Stage 构建验证。该副本不包含主目录其他人的未提交改动，因此不是主目录全部改动的集成验收。
- `DuelBehavior.cs` 主目录与验证副本 SHA256 一致：`48F575EE90972A038DB242B6731A487DE5C3018551AF8455CF1D26A43189E69D`。
- Debug / Bannerlord 1.3.15、1.4.7、Bootstrap 三项编译全部 0 warning / 0 error；生产 DLL 决斗契约回放 `passed=35 failed=0 variants=2`。
- 1.3 使用仓库原有 `_deps_auto` 引用覆盖，覆盖不完整的已知限制仍遵循兼容文档；新使用的部署方法已同时核对 1.3 DLL。
- 1.3 产物 SHA256：`E374A5F9F0619A08125F9775FBACE7493C03A489732B74BC24F7516929FC4BBB`。
- 1.4 产物 SHA256：`06D193AB76A783A120A7911B04BAC147253B2E3C9BFF5157A9A2583DAA5731F0`。
- 本地详细证据：`_codex_tmp/duel-diagnosis-20260927/validation-clean.log`、`validation-main.log` 和该目录反编译结果。生成物不提交。
- 未推送、未覆盖游戏、未改真实存档。上述检查验证编译和调用契约，不证明游戏内场景已成功加载。
- 待实机复测：同一存档与科林的大地图野外决斗；确认玩家可操作、对手可见且能战斗，退出/胜负结算和返回地图正常；同时覆盖原版自动结束部署的小队场景。

## 后续：主工作区编译阻断已修复

- 用户随后提供主目录编译错误，确认 `third_party_analysis` 下反编译源码、独立工具及其 `obj` 被 SDK 默认文件匹配纳入 AF。
- `AnimusForge.csproj` 的 `DefaultItemExcludes` 增加该分析目录，保留其文件内容和独立项目；没有修改一键脚本、依赖或覆盖流程。
- 本次直接在 `main` 主工作区（包含现有未提交改动）重新运行官方 unified Stage：1.3.15 / 1.4.7 / Bootstrap 均 0 warning / 0 error，35 项决斗生产契约检查全部通过。该结果取代上文“主目录构建被阻断、仅干净副本验证”的当前状态；其余功能和实机验收仍未由本次检查覆盖。
- 证据：`_codex_tmp/duel-diagnosis-20260927/validation-main-exclusion.log`。仅生成项目内 Stage，未覆盖游戏或推送。
