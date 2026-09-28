# J16 tests / tools / scripts / docs / Bootstrap 实施计划

> 规划日期：2026-09-28。状态：**计划完成，施工未开始**。详细进度只记录在[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md)，[HANDOFF](../../HANDOFF.md)保留简短入口；本文不建立竞争台账。
> 规划源码基线：`e7936b04`（分支 `codex/af-j15-closeout-20260928`，基于远端 `8af57b39`）。J15 离线部分已收口：内容 runner 108 映射 / 0 HOLD，六构建 0/0，StaticVerifier 13/0；F5 实机/旧档仍 NOT-RUN（见第 2.3 节）。
> 本文细化主台账 “J16 tests / tools / scripts / docs / Bootstrap” 条目，不改变其授权边界：**一键脚本与 Bootstrap 只在另获授权时迁**。

## 1. 新任务启动指令

```text
执行当前仓库 docs/plans/j16-tests-tools-docs-plan.md。
先核实实际 Git 根、分支、HEAD、dirty，再读 AGENTS.md、HANDOFF.md 当前段、主台账 J15/J16 条目和本计划。不要按历史盘符切换副本。
按 G0 → J16a 测试归位 → J16b 工具与输出 → J16c 文档权威入口 → J16d 脚本/Bootstrap（仅获授权后）→ J16e 收口 执行。
每个验证切片本地独立提交，只暂存自己的具名文件；移动用 git mv 保留历史，一个 runner 一组，移动后立即跑该 runner。
不 push、不部署、不写游戏/存档/外仓；不改产品代码行为；不删除唯一副本；不改写 Git 历史（含私人数据，严禁推送）。
```

## 2. 基线、范围与完成定义

### 2.1 已核实的规划基线（`e7936b04`）

| 区域 | 现状 | 备注 |
| --- | --- | --- |
| `tools/` 子目录 | 105 个：测试 runner 99（Python 54、csproj 45），工具 4（`ActionPostprocessPromptLab`、`PreprocessTopicPromptLab`、`PlayerExportsEditor`、`PhaseEightReadiness`），依赖目录 1（`ReplayDependencies`），误跟踪缓存 1（`__pycache__`） | 另有 12 个根级散文件（6 个测试/审计脚本、`af2_migrate.py`、`repository_source_inventory.py` 等） |
| `tests/` | 已迁 49 个 runner：`AF.Persistence` 3、`content` 1、`modules/AF.Module.*` 39（Actions/Conversation/Economy/Kingdom/Knowledge/Llm/Memory/Prompt/Weekly） | 已迁 runner 用 `parents[3]`/`parents[4]` 定位仓库根 |
| 跨 runner 依赖 | `tools/ChannelCutoverBoundaryTests/run.py` 被 105 处引用、`ModuleFrameworkApiTests/run.py` 21 处、另有 6 个 run.py 被 5–10 处引用 | 移动这些“公共 helper”会连锁影响；必须先抽出 |
| 旧结构引用 | 40 个 runner 直接读 `Refactor/` 源码 | `Refactor/` 清空属 J17，不在 J16 |
| 机器专用路径 | 56 个 runner 目录写死 `G:\AFMOD\.dotnet-sdk`、`G:\Python310`、`F:\my mod\...`、`E:\steam\...` 等 | 本机已无 `G:\Python310`、`local/dotnet/8.0.425`、pwsh 7-preview |
| 递归清理 | 6 处含 `rmtree`/`Remove-Item -Recurse`/`Directory.Delete(...,true)`：`tests/AF.Persistence`、`tests/content`、`tests/modules`、`tools/BridgeRuntimeIsolationTests`、`tools/PlayerExportsEditor`、`tools/PolicyEffectModule.ContractTests` | 迁移前逐个确认清理目标只在其专属输出根 |
| 系统 Temp 写入 | 10 个 runner 目录 | 统一改为显式输出根，或在执行单中把 TEMP/TMP 指向 G: |
| 已跟踪产物 | `tools/PlayerExportsEditor/dist` 291 MB（inventory `HOLD:tool-distribution`）、`.tmp/build_check` 80 MB（161 个 1.3/1.4 引用 DLL，构建脚本回退候选）、根目录 `Logs (10).zip`/`Logs (11).zip` 约 40 MB、31 个根级 `artifacts_*.png`、`EarlyException_2026-06-13.html`、`DuelSettings.cs.broken-backup-*` | 去跟踪 ≠ 删除；Git 历史不改 |
| `docs/` | 346 个跟踪文件：根 63、handoffs 84、audits 58、gccz 35、phase8 32、fixtures 30、architecture 19、plans 8 等；104 个文档引用 `tools/` 路径 | `CLAUDE.md`/`AGENTS.md` 各引用 14 处 docs 案例文档 |
| 脚本 / Bootstrap | `一键编译覆盖推送/`（5 个 ps1 + 7 个 bat）、`AnimusForge.Bootstrap/`（4 文件）、`myaimod.sln`（2 项目） | 授权边界见第 4 节 |

### 2.2 范围

1. **J16a** 测试 runner 随 owner 迁 `tests/`，公共 helper 抽出，路径定位与工具链可移植。
2. **J16b** 工具只留源码；运行输出进 `artifacts/`（已忽略）；已跟踪产物去跟踪并留恢复说明。
3. **J16c** 文档一事实一权威入口：当前事实只在主台账 + HANDOFF + 各阶段计划；案例文档保留原路径；历史归档先修链接再移动。
4. **J16d**（需授权）一键脚本与 Bootstrap 的精确映射；根一键入口保持兼容。
5. **J16e** 统一 runner 清单与一次性全量离线回归，绑定同一候选。

**不做**：不改产品代码行为；不清空 `Refactor/`（J17）；不改 CI/CD/部署语义；不重写或合并测试断言（只改路径/定位/工具链）；不把失败测试删掉换 PASS。

### 2.3 J15 遗留（J16 期间并行，不阻塞）

- **F5 实机**：1.4.7 部署待用户确认（候选 Release Stage 124 项，备份与写入清单见 HANDOFF）；1.3 线本机无环境 NOT-RUN。
- **F4-A**：`pack0.tpac` 由另一位制作组成员负责，交接见主台账 F4-A 条。J16 不改 TPAC 与 Xihai manifest。

### 2.4 完成定义

- `tools/` 下不再有测试 runner；每个测试目录位于 `tests/<owner>/<Name>/`，owner 与 `src/` 目录一致。
- 所有 runner 用统一 helper 定位仓库根，不依赖自身目录深度；工具链路径只来自环境变量或统一配置，**仓内无机器专用绝对路径**（历史文档、审计 JSON 除外）。
- 统一清单（`tests/runners.json`）列出每个 runner 的入口、工具链、输出根、清理范围、是否需要游戏引用；一个总入口能按清单全量或按 owner 运行，逐项报退出码。
- 全量离线回归在同一候选上跑通，失败项有诊断且不以跳过冒充通过。
- inventory `unknown=0`；`HOLD:tool-distribution`、`HOLD:run-log`、根级杂项有明确去向。
- `docs/` 当前事实只有一个权威入口；`verify_code_map.py` 两模式通过；文档本地链接 0 断链。
- J16d 仅在获授权时完成；未获授权则明确记 `NOT-AUTHORIZED`，不计入 J16 失败。

## 3. 执行包与退出门

### G0：开工基线

- 核实 HEAD/dirty，记录意图检查点并提交。
- 生成基线：对 `tools/` 与 `tests/` 全部 runner 在**当前位置**跑一遍，记录每个的退出码、耗时、输出目录、是否写 Temp/清理。已知环境阻塞（缺 SDK 10、缺游戏引用）单独记 `BLOCKED_ENV`，不计失败。
- 这份“移动前结果表”是 J16a 每个切片的对照：移动后结果必须与移动前一致（PASS→PASS，BLOCKED_ENV 可变 PASS，不可 PASS→FAIL）。
- **退出门**：基线表已提交；失败项已诊断为“既有失败”或“环境阻塞”。

### J16a：测试归位（按 owner 分批）

**J16a-0 公共 helper 先行**（最高风险，单独切片）：
- 新建 `tests/_shared/`：`repo_root.py`（向上找 `AnimusForge.csproj` 定位根，替代 `parents[N]`）、`toolchain.py`（`AF_DOTNET8`、`AF_DOTNET10`、`AF_PWSH`、`AF_PYTHON` 环境变量，缺省值按现有可用路径解析，找不到时明确报 `BLOCKED_ENV`）、`module_loader.py`。
- 把 `ChannelCutoverBoundaryTests/run.py`、`ModuleFrameworkApiTests/run.py` 中被别人 import 的函数抽到 `tests/_shared/`；原文件保留薄转发，使移动期间两种位置都可用，J16a 末尾删转发。
- 退出门：所有引用 helper 的 runner 在原位置跑出与基线一致的结果。

**J16a-1…n 按 owner 移动**（每个 owner 一个切片，顺序由依赖少到多）：

| 批次 | 目标 | 数量（按名称初分，开工时按实际编译源核实） |
| --- | --- | --- |
| 1 | `tests/AF.Persistence/`、`tests/AF.Contracts/`、`tests/AF.Foundation.Runtime/` | 3 + 2 + 5 |
| 2 | `tests/modules/AF.Module.{Economy,Duel,Social,Persona,Kingdom,Encounter,Weekly,Diplomacy}` | 5 + 3 + 1 + 2 + 2 + 1 + 1 + 5 |
| 3 | `tests/modules/AF.Module.{Memory,Llm}` | 6 + 11 |
| 4 | `tests/modules/AF.Module.Conversation`（Courier/Native/Scene/Channel/Interaction） | 34 |
| 5 | `tests/bridges/`、`tests/AF.GameAdapter.Bannerlord/`、`extensions/AnimusForge.XihaiAction/tests/` | 5 + 8 + 1 |
| 6 | 横切回放 `tests/replay/`（PhaseEight、ProductionOptIn/ValidationProvider）；`BattleSpeechCapturedTriggerRegressionTests` 按实际 owner 核定 | 4 + 1 |

每个切片：
1. `git mv` 整个目录；runner 改用 `tests/_shared/repo_root.py`；csproj 相对 `Include` 与 `ReplayDependencies.targets` 引用按新深度修正（只改路径）。
2. 更新引用它的其他 runner、`.gitignore` 中的 `.generated/` 路径、`repository_source_inventory.py` 分类、代码地图锚点。
3. 跑该 runner 与所有 import 它的 runner，结果与基线一致；跑 inventory。
4. 独立提交。

**owner 判定规则**：以 runner 实际编译/读取的生产源文件所在 `src/` 目录为准，名称只作初分；读多个 owner 的归 `tests/integration/<名称>/`，不强行拆断言。

**退出门**：`tools/` 下无 `*Tests`/`*SmokeTests`/测试散脚本；公共 helper 转发已删；全部 runner 结果与基线一致。

### J16b：工具与输出

- **保留为源码工具**（留 `tools/`）：`ActionPostprocessPromptLab`、`PreprocessTopicPromptLab`、`PlayerExportsEditor`、`PhaseEightReadiness`、`ReplayDependencies`、`af2_migrate.py`、`repository_source_inventory.py`、`package_policy_system_source_overlay.py`、`PersistenceIdentityAudit.py`、文本/研究导出脚本。各自测试随 J16a 迁 `tests/tools/<工具名>/`。
- **输出统一**：所有工具/runner 默认输出到 `artifacts/<tool>/<run-id>/`，拒绝覆盖已存在根；不再写 `tools/**/runs`、`.generated`、系统 Temp。
- **已跟踪产物去跟踪**（每组单独列清单并获批，`git rm --cached`，文件原地保留或移到忽略的 `local/`，附 SHA manifest）：
  - `tools/PlayerExportsEditor/dist`（291 MB）→ 以 `publish-win-x64.ps1` 重新生成；发布物不入库。
  - `tools/__pycache__/*.pyc` → 直接去跟踪并加忽略。
  - 根级 `Logs (10).zip`、`Logs (11).zip`、`EarlyException_2026-06-13.html`、31 个 `artifacts_*.png`、`DuelSettings.cs.broken-backup-*`、`错误堆栈.lnk`、`_patch_diplomacy_proactive.py` → 逐项核实无消费者后去跟踪/归档。
  - `.tmp/build_check`（80 MB 引用 DLL）→ **暂留**：构建脚本仍把它作回退候选，改动属 J16d。
- **退出门**：inventory `unknown=0`；`HOLD:tool-distribution`/`HOLD:run-log` 清零或逐项写明保留原因；六构建与编辑器 smoke 仍通过。

### J16c：文档权威入口

- **当前事实**只在三处：主台账（进度与证据）、`HANDOFF.md`（当前段 + 下一步）、`docs/plans/`（每阶段计划）。其他文档不再写“当前状态”。
- **案例文档保留原路径**：`CLAUDE.md`/`AGENTS.md` 引用的 8 个 `docs/*_case.md` 等不移动，避免 agent 指令断链。
- **历史归档**：`docs/handoffs/`（84）、`docs/phase8/`、`docs/animusforge-phase{2..7}-*.md` 等已完成阶段文档移入 `docs/history/<阶段>/`；**先**修正所有指向它们的链接（含 `tools/`/`tests/` README 与主台账），**再**移动。
- 统一 `docs/README.md` 索引：权威入口、案例、架构、历史各一节。
- 新增文档链接检查（复用现有 verify 思路，放 `tests/docs/LinkCheck/`）：本地 Markdown 链接与引用路径 0 断链。
- **退出门**：链接检查 0 断链；代码地图两模式通过；HANDOFF 与主台账当前状态一致。

### J16d：一键脚本与 Bootstrap（需另获授权）

未获授权前只做**只读映射**并写入主台账：

| 对象 | 候选去向 | 必须保持 |
| --- | --- | --- |
| `一键编译覆盖推送/*.ps1` | `scripts/build/`，根目录 `.bat` 保留为兼容入口转调 | 单模块双实现输出、`-Stage`/`-Deploy` 互斥、受管部署与回滚语义、FeatureBridges 冲突预检；`docs/bannerlord_dual_module_output.md` 所述规则 |
| `AnimusForge.Bootstrap/` | `src/AF.Bootstrap/`（项目名、程序集名 `AnimusForge.Bootstrap` 不变） | `SubModule.xml` 只加载 Bootstrap；按版本只加载一个实现 |
| `.tmp/build_check` 回退候选 | 改为显式 `-Bannerlord1xReferenceDir` 或 `local/bannerlord-refs/`，去跟踪 | 两条 API 线引用版本可核验 |
| `myaimod.sln` | 补齐工具/测试项目或保持最小 | 不影响一键构建 |

获授权后：每项单独切片；六构建、Stage 124 项、StaticVerifier、ManagedDeploy 合成契约、根一键入口实跑全部通过才提交。

### J16e：收口

- 总入口 `tests/run_all.py`：读 `tests/runners.json`，按 owner/全量运行，每项独立退出码，汇总 PASS/FAIL/BLOCKED_ENV；失败不短路统计但总退出码非 0。
- 在同一候选上全量运行；六构建；内容 runner；StaticVerifier；编辑器隔离 smoke；inventory；代码地图两模式；`git diff --check`。
- 更新主台账 J16 条、HANDOFF、GCCZ 侧交接（GCCZ 源码不变时只记录）。

## 4. 授权停点

| 停点 | 执行者先准备 | 未确认时 |
| --- | --- | --- |
| A1 已跟踪产物去跟踪 | 每组精确路径、大小、SHA、消费者核查、恢复办法 | 可做测试迁移；不去跟踪 |
| A2 一键脚本/Bootstrap 迁移 | J16d 只读映射表与验证计划 | 只读映射；不移动 |
| A3 递归清理的测试 | 每个清理目标绝对路径与新输出根 | 在隔离根运行；不在默认根盲跑 |
| A4 推送 | —— | Git 历史含私人数据，**严禁推送** |

## 5. 风险与对策

- **大规模移动破坏相对路径**：helper 先行 + 每 owner 一个切片 + 移动前后结果表对照。
- **跨 runner import 连锁**：J16a-0 先抽公共函数并留转发。
- **测试把名称误当 owner**：以实际编译/读取源判定；跨 owner 归 integration。
- **环境差异冒充失败**：统一 toolchain 探测，缺工具报 `BLOCKED_ENV`；不改断言。
- **文档移动导致 agent 指令断链**：案例文档不动；先修链接再移动；链接检查兜底。
- **误删唯一副本**：去跟踪一律 `--cached` + 忽略目录 SHA 归档。

## 6. 预计规模

J16a 约 8–10 个切片（helper 1 + owner 6 批 + 收尾），J16b 3–4 个，J16c 3 个，J16e 1 个；J16d 视授权 3–4 个。
