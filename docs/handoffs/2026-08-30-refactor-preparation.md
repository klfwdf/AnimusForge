# Handoff：AF 重构准备阶段

- 日期：2026-08-30
- 当前分支：`refactor/prepare-af-restructure`
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`（包含已单独推送的模块版本提交）
- 当前准备分支已推送提交：`23449caf0c6d1e38235d752f8fa1b0975dce17a5`（重构地图与 owner matrix）
- 当前状态：IN PROGRESS（阶段 2 设计切片已完成；阶段 3 设计与纯验证已完成，最终审查 PASS WITH LIMITATIONS；阶段 4 Persistence/Profile/Config 待启动）
- 最近验证：6 个纯 fixture runner 均成功（Bridge 10/6、Module catalog 8/3/16/8、AF.Contracts 9/3/6/18、Foundation 6/8/16、Composition 18/24、GameAdapter 14/2/7）；14 个 JSON fixture 解析 PASS；`git diff --check` PASS；未接入运行时。
- 用户已确认：允许将准备文件和 skill 提交并推送到新的远端分支；暂不拆分多个玩法 DLL；旧存档兼容是硬目标；游戏基线采用最小可复现场景记录，不要求现在立即完成全量手测。

## 本次已完成

- 已确认当前工作区为 `F:\AnimusForge-main`。
- 已确认 `origin` 指向 `https://github.com/klfwdf/AnimusForge.git`。
- 已创建本地准备分支，并已将准备文档与重构地图推送到 `origin/refactor/prepare-af-restructure`。
- 已将 `D:\APP\QQ\document\af-skill.zip` 安装到项目级 `.claude/skills/animusforge-maintainer/`。
- 已创建公共重构台账：`docs/animusforge-refactoring-and-repository-reorganization-plan.md`。
- 已创建起点基线：`docs/animusforge-baseline-2026-08-30.md`。
- 已完成有限的只读结构盘点。
- 已完成第一版重构地图：`docs/animusforge-refactor-map.md`，涵盖运行链、组合根、目标 owner、持久化、交互管线、风险和推荐顺序。
- 已完成仓库边界、功能域和持久化审计；审计结论已合并到 `docs/animusforge-refactor-map.md`。
- 已完成第一版逐文件 owner matrix：`docs/animusforge-owner-matrix.md`，涵盖运行、交互、持久化、Policy、World、Siege、Mission、Social、Knowledge、UI、工具和参考树。
- 已确认 Bootstrap 已是清晰的独立边界；`SceneActionsIntegrationBoundary` 是现有的薄适配器边界范例。
- 已确认后续 owner map 至少应区分 Host/Composition、Conversation/AI、World Simulation、Settlement/Siege、Mission/Combat、Policy、Progression/Social、UI/Diagnostics 与 Compatibility/Safety；这些目前只是逻辑所有权，不代表立即拆 DLL。
- 阶段 1 首轮审计报告：`docs/animusforge-repository-boundary-audit.md`；未清理 tracked 参考/用户/生成物，未修改脚本。
- 阶段 1 初版决策表：`docs/animusforge-repository-boundary-decision-table.md`；采用保守“不发布未确认来源/许可证内容”原则，未执行清理或移动。
- 阶段 2 根 AF 基础 LLM owner 映射报告：`docs/animusforge-phase2-root-llm-owner-slice.md`；只读完成，未移动源码或改变运行行为。
- 阶段 2 SubModule 注册/调度分组清单报告：`docs/animusforge-phase2-submodule-registration-catalog.md`；只读完成，未改变注册顺序或运行行为。
- 阶段 2 registry DTO 设计报告：`docs/animusforge-phase2-registry-dto-design.md`；只读完成，未新增运行时类型或改变行为。
- 阶段 2 registry validator fixture：`docs/animusforge-phase2-registry-validator-fixtures.md`；覆盖有效快照、无效输入、依赖/顺序/owner/profile/线程/失败隔离输出；未实现 validator，未接入运行时。
- 阶段 2 影响面、候选 Bridge 与回滚地图：`docs/animusforge-phase2-impact-bridge-rollback-map.md`；首轮覆盖 Save、Prompt/Rule/Tag、Harmony、Tick、UI、线程、API、用户数据、Bridge、非目标和回滚模板；未移动源码或改变运行行为。
- 阶段 2 Conversation/Memory/Action contract matrix：`docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；定义不可变 snapshot、记忆/AFEF、授权动作结果、逐文件影响、三渠道一致性和纯 fixture；未实现 DTO/测试，测试 NOT-RUN。
- 阶段 2 Conversation/Memory/Action 方法级映射与纯 fixture：`docs/animusforge-phase2-conversation-memory-action-method-map.md`、`docs/fixtures/phase2-conversation-memory-action/`；已核对真实方法行号并建立纯输入/预期输出样例；`git diff --check` PASS，YAML parser NOT-RUN（环境无 parser）。
- 阶段 2 Settlement/Siege 与 Policy/Diplomacy Bridge contract：`docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`、`docs/fixtures/phase2-settlement-policy-bridges/`；已定义现有边界、五种组合、失败/回滚语义；3 个 JSON fixture 已通过 `ConvertFrom-Json`；未实现 Bridge 或 runner。
- 阶段 2 Bridge fixture runner：`tools/BridgeFixtureContractTests/validate_bridge_fixtures.py`；普通输出和 `--json` 输出均 PASS，10 个组合案例、6 项不变量；独立运行，不引用 Bannerlord/生产程序集。
- 阶段 3 module catalog：`docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/fixtures/phase3-module-catalog/`、`tools/ModuleCatalogContractTests/validate_module_catalog.py`；普通输出和 `--json` 输出均 PASS，8 modules、3 profiles、16 invalid cases、8 health states；未实现 Foundation/Registry。
- 阶段 3 AF.Contracts：`docs/animusforge-phase3-af-contracts-design.md`、`docs/fixtures/phase3-af-contracts/`、`tools/AFContractsContractTests/validate_af_contracts.py`；普通输出和 `--json` 输出均 PASS，9 contracts、3 events、6 capabilities、18 invalid cases；未创建生产 contract 项目。
- 阶段 3 Foundation runtime：`docs/animusforge-phase3-foundation-runtime-contracts.md`、`docs/fixtures/phase3-foundation-runtime/`、`tools/FoundationRuntimeContractTests/validate_foundation_runtime.py`；普通输出和 `--json` 输出均 PASS，6 contracts、8 health states、16 invalid cases；未创建生产 Foundation 项目。
- 本轮审查：修正公共台账中阶段 3 条目误放阶段 2及陈旧验证记录；四个独立 runner 均重新运行通过，未发现生产/脚本/配置路径变化。
- 阶段 3 纯组合矩阵：`docs/animusforge-phase3-composition-matrix.md`、`docs/fixtures/phase3-composition-matrix/`、`tools/CompositionMatrixContractTests/validate_composition_matrix.py`；普通输出和 `--json` 输出均 PASS，18 cases、24 invariants；未实现 Module Host。
- 阶段 3 GameAdapter API boundary：`docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/validate_game_adapter.py`；普通输出和 `--json` 输出均 PASS，14 cases、2 API lines、7 helper boundaries；未修改生产 helper，未重新构建/部署。
- 阶段 3 最终设计审查：`docs/animusforge-phase3-final-review.md`；确认阶段 3 设计清单闭合、6 个 runner 和 14 个 JSON fixture 通过；结论 PASS WITH LIMITATIONS；生产实现、双版本运行时、旧存档和游戏内验收仍未完成。
- 提交/推送状态：已创建本地提交（当前 HEAD，包含阶段 2/3 架构准备材料）；两次推送均失败，错误分别为 `Recv failure: Connection was reset` 和 `Failed to connect to github.com port 443`；当前分支领先远端 1 个提交，远端尚未更新。
- 用户决定先保持仓库现状：参考源码、生成物、用户数据、第三方依赖、工具发行物和归档均不删除、不移动、不取消跟踪；不修改 `.gitignore`。
- 用户已明确：`原版游戏本体代码1.3.x/` 与 `原版游戏本体代码1.4.5/` 是游戏源码参考仓库，应保留在 tracked reference plane；它们不属于 AF 生产源码，也不进入客户端 ZIP。

## 重要注意

- 开始准备时 `AnimusForge/SubModule.xml` 已有用户修改：版本 `v1.3.7` → `v1.3.7.2`，且末尾换行变化。本次没有回滚或覆盖它。
- 已运行统一 Debug stage 构建；未运行打包、部署或游戏测试。打包/部署/游戏测试仍为 `NOT-RUN`。
- 已使用本机 Bannerlord 根目录识别实际游戏版本 `v1.4.8.119303`；统一构建成功完成 1.3、1.4 和 Bootstrap，未部署到游戏目录。
- 用户已确认主要游戏内基线版本为 Bannerlord 1.4，采用 `1.4.x` 兼容目标，并确认存在可备份的代表性存档。
- 已从实际游戏 `TaleWorlds.Library.dll` 读取到版本 `v1.4.8.119303`；仓库 `.tmp\build_check\1.4` 是 `v1.4.6.115628`。本次构建 marker 已记录两条精确 BuildInfo；不同开发者可使用不同 1.4.x 补丁，但共享验收仍需指定固定 overlay。
- 依赖闭包已在本机解析：Harmony `2.4.2.225`、UIExtenderEx `2.13.2`、MCM/MBOptionScreen `5.11.4`，以及 AnimusForge 私有运行时 6 项均存在；1.4 overlay 内部引用完整。
- 不要把临时 ZIP 解压目录 `D:\APP\QQ\document\.af-skill-inspect\` 当作源码目标；skill 已从原 ZIP 安装到项目内。
- 不要修改现有一键编译/覆盖/推送流程。

## 下一步建议

- 已选定本机最新旧存档：`C:\Users\29310\Documents\Mount and Blade II Bannerlord\Game Saves\saveauto2.sav`（修改时间 2026-08-28 14:23:10）；用户已完成手动测试，详细结果按决定跳过；未复制或修改存档。
1. 旧存档基础测试已由用户完成；详细结果不进入本轮台账，后续不再阻塞阶段 2 只读 owner 映射。
2. 如需针对当前安装游戏做精确验收，准备完整 `v1.4.8.119303` reference overlay；不修改构建脚本。
3. 进入阶段 4 设计：盘点 SyncData、JSON、PlayerExports、AFEF 和旧数据，建立 persistence namespace/key/type/chunk/legacy fallback catalog；继续不接入 `SubModule.cs`，不实现生产 C#。



## 接手规则

新对话或新开发者应先读取：

- `CLAUDE.md`
- `.claude/skills/animusforge-maintainer/SKILL.md`
- `docs/animusforge-refactoring-and-repository-reorganization-plan.md`
- `docs/animusforge-baseline-2026-08-30.md`
- 本 handoff

随后核对 `git status`、当前分支、HEAD 和实际文件状态，再进行任何写入。
