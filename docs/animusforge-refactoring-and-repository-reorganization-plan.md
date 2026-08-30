# AnimusForge 重构执行清单

> 本文件是 AF 重构的公共进度台账。它记录目标、阶段、当前状态、验证证据和交接信息；不替代 `.claude/skills/animusforge-maintainer/` 中的长期工作规范。

## 当前状态

- 项目：Mount & Blade II: Bannerlord AnimusForge mod
- canonical worktree：`F:\AnimusForge-main`
- 当前分支：`refactor/prepare-af-restructure`
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`
- 基线父提交：`96a1c60f1877813a9fb3440ddad068d6e92afa1e`（policy 功能基线）
- 当前工作 HEAD：`7567c59fd9a8883097b66a5dd007fcdc9ef9f495`（准备文档、地图、owner matrix 与 handoff 已推送分支）
- 当前阶段：阶段 4 待启动，Persistence / Profile / Config（阶段 1 清理 HOLD；阶段 3 设计已完成；阶段 0 基线详细记录按用户决定跳过）
- 当前任务：盘点 SyncData、JSON、PlayerExports、AFEF 和旧数据；仍只做设计/fixture，不实现生产 C#，不接入 `SubModule.cs`
- 当前负责人：待由后续 handoff 指定
- 物理程序集策略：暂不拆分为多个玩法 DLL；先在单一 `AnimusForge.dll` 内完成逻辑模块化
- 旧存档目标：必须兼容；至少保持现有程序集身份、序列化类型和 SyncData key，必要变更必须提供迁移与证据
- 游戏基线策略：保留可复现的测试记录，但不要求现在由用户立刻完成全量手测；优先记录关键功能和重构前后对比结果
- BannerlordRoot：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord`
- 已安装模块目录：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`
- 主要游戏内测试版本：Bannerlord `v1.4.8.119303`（本机当前安装）
- 1.4 构建引用要求：按 `1.4.x` API 线管理；每个开发者可以使用自己的合法 1.4.x 安装，但构建记录必须写明精确 `BuildInfo`，共享验收使用固定代表性 overlay
- 最后更新：2026-08-30
- 状态：IN PROGRESS
- 最近验证：统一 Debug stage 构建已成功；输出位于 `bin\Debug\single_module_stage\AnimusForge`，未部署。
- 依赖记录：实际外部模块路径已解析；当前机器游戏 BuildInfo 为 `v1.4.8.119303`，可复现 1.4 overlay 为 `v1.4.6.115628`。
- 阶段 1 阻塞：用户已决定先保持仓库现状；1.3.x/1.4.x 游戏源码参考仓库保留在 tracked reference plane，其他未决对象也不做清理，许可证/第三方 provenance 继续作为待确认项。

## 重要工作区事实

- 工作区在准备开始时并非完全干净：`AnimusForge/SubModule.xml` 有用户已有修改（版本从 `v1.3.7` 变为 `v1.3.7.2`，且文件末尾换行发生变化）。本次准备不回滚、不覆盖该修改。
- 项目当前采用一个 `Modules/AnimusForge` 模块、Bootstrap 加载单一版本实现的发布契约。
- 当前主实现项目是 `AnimusForge.csproj`，Bootstrap 项目是 `AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj`。

## 阶段总览

### 阶段 0：准备与基线 — IN PROGRESS

- [x] 创建本地准备分支
- [x] 安装项目级 `animusforge-maintainer` skill（未提交）
- [x] 创建基线报告
- [x] 创建本公共重构台账
- [x] 审阅 skill、基线报告和本台账
- [x] 完成当前仓库只读结构盘点
- [x] 确认代表性存档和游戏内基线方案
- [x] 形成第一版功能—owner—依赖—风险重构地图（见 `docs/animusforge-refactor-map.md`）
- [x] 完成第一版逐文件 owner matrix（见 `docs/animusforge-owner-matrix.md`）
- [x] 记录可运行的 1.3.x、1.4.x、Bootstrap 构建结果（Debug unified stage；1.4 使用 v1.4.6.115628 overlay）

### 阶段 1：仓库边界与可重复性 — IN PROGRESS

- [x] 盘点源码、内容、测试、工具、脚本、文档、引用、依赖和产物平面（见 `docs/animusforge-repository-boundary-audit.md`）
- [x] 确认 `.gitignore` 与用户数据/生成产物边界（历史 tracked 生成物仍需后续分批处置）
- [x] 固化构建、stage、package、deploy 的现状说明（见 `docs/animusforge-repository-boundary-audit.md` 与 `README_BUILD.md`）
- [x] 建立初版仓库边界与分发决策表（见 `docs/animusforge-repository-boundary-decision-table.md`；法律/许可证确认仍未完成）
- [ ] 确认许可证、分发和第三方文件处理原则

### 阶段 2：模块目录与所有权地图 — DONE（设计完成；生产迁移未开始）

- [x] 建立现有功能 → 当前入口/文件 → 目标 owner 映射（首条根 AF 基础 LLM 对话只读切片见 `docs/animusforge-phase2-root-llm-owner-slice.md`）
- [x] 建立 `SubModule.cs` 注册/调度分组清单（只读；未改变注册顺序或运行行为）
- [x] 设计 Host/Composition registry DTO 与独立 contribution groups（只读；报告见 `docs/animusforge-phase2-registry-dto-design.md`；未接入运行时）
- [x] 建立纯 validator 输入/输出 fixture（见 `docs/animusforge-phase2-registry-validator-fixtures.md`；未实现 validator，运行频率 0）
- [x] 建立阶段 2 影响面、候选 Bridge、模块非目标与回滚入口地图（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；仅首轮高层设计）
- [x] 首轮标注存档、Prompt、标签、Harmony、Tick、UI、主线程和版本影响（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；逐文件细化已在 Conversation/Memory/Action 切片完成）
- [x] 首轮标注跨模块行为和候选 Bridge（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；contract test 已有独立 fixture/runner）
- [x] 为每个目标模块建立非目标和回滚入口模板（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；具体切片填写留待生产迁移）
- [x] 建立首轮 Conversation/Memory/Action contract 边界逐文件影响表与纯 contract test matrix（见 `docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；仅设计，测试 NOT-RUN）
- [x] 将 contract matrix 映射到真实方法/调用点并建立独立纯 fixture 目录（见 `docs/animusforge-phase2-conversation-memory-action-method-map.md` 与 `docs/fixtures/phase2-conversation-memory-action/`；YAML parser NOT-RUN）
- [x] 细化 Settlement/Siege 与 Policy/Diplomacy 候选 Bridge contract（见 `docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`；仅设计，未实现）
- [x] 为两组 Bridge fixture 建立纯 contract 验证矩阵/runner（见 `tools/BridgeFixtureContractTests/`；不接入生产 `.csproj`）

### 阶段 3：Contracts 与基础运行时 — DONE（设计完成；生产实现未开始）

- [x] 定义模块身份、能力、事件、DTO、契约版本（见 `docs/animusforge-phase3-af-contracts-design.md`；未创建生产项目）
- [x] 设计模块 manifest、profile、依赖和健康状态（见 `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`；未实现 Foundation/Registry）
- [x] 整理 Foundation、主线程调度、后台任务、诊断和 SafeMode（见 `docs/animusforge-phase3-foundation-runtime-contracts.md`；未创建生产项目）
- [x] 整理 GameAdapter 与 1.3/1.4 API 边界（见 `docs/animusforge-phase3-game-adapter-api-boundary.md`；未修改生产 helper）
- [x] 为上述 catalog/contract 建立纯 metadata runner（见 `tools/ModuleCatalogContractTests/`、`tools/AFContractsContractTests/`、`tools/FoundationRuntimeContractTests/`；不接入生产 `.csproj`）
- [x] 设计 no-op module、dependency-missing、optional-provider、SafeMode 和 failure-isolation 纯组合矩阵（见 `docs/animusforge-phase3-composition-matrix.md` 与 `docs/fixtures/phase3-composition-matrix/`；18 cases、24 invariants）
- [x] 建立 GameAdapter API boundary 纯 fixture/runner（见 `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/`；14 cases）
- [x] 进行阶段 3 最终设计审查并确认进入阶段 4（见 `docs/animusforge-phase3-final-review.md`；PASS WITH LIMITATIONS）
### 阶段 4：Persistence / Profile / Config — TODO（待启动）

- [ ] 盘点 SyncData、JSON、PlayerExports、AFEF 和旧数据
- [ ] 建立持久化 namespace 与迁移目录
- [ ] 保留现有程序集/类型/key 兼容性
- [ ] 建立配置快照、模块开关和 profile 解析边界

### 阶段 5：Conversation 统一交互管线 — TODO

- [ ] 统一场景喊话、自由对话、信使的快照/资格/Prompt/历史结构
- [ ] 统一后处理标签、动作执行入口和 AFEF 事实写入
- [ ] 保留旧入口作为 facade
- [ ] 验证三渠道规则和记忆一致性

### 阶段 6：Memory / Prompt / Action — TODO

- [ ] 提取 Memory 与事实服务
- [ ] 提取 Prompt/Rule 与前后处理规则
- [ ] 建立统一动作解析、授权、当前状态验证、主线程执行和结果记录
- [ ] 先迁移低风险动作垂直切片

### 阶段 7：领域模块渐进迁移 — TODO

建议顺序（以实际依赖盘点为准）：

1. Economy / Trade / Debt / Reward
2. Policy
3. Courier
4. Duel
5. WorldMap
6. Scene
7. Diplomacy
8. Siege / Battle
9. Knowledge / UI

每个领域都必须保持旧入口可用，完成调用方、存档、双版本、渠道、profile 和组合验证后才删除旧实现。

### 阶段 8：Bridge、旧结构清理与最终验收 — TODO

- [ ] 仅为确有跨模块所有权的行为建立 Bridge
- [ ] 验证 A、B、A+B、A+B+Bridge、Bridge 故障矩阵
- [ ] 清理 God Object、重复注册、旧 facade 和临时代码
- [ ] 验证 1.3、1.4、Bootstrap、stage、package、存档和游戏内场景
- [ ] 记录所有 NOT-RUN 与剩余风险

## 目标逻辑模块（第一版，非最终物理 DLL 方案）

- `AF.Contracts`
- `AF.Foundation.Runtime`
- `AF.GameAdapter`
- `AF.Persistence`
- `AF.Profile` / `AF.Config`
- `AF.Module.Conversation`
- `AF.Module.Memory`
- `AF.Module.Prompt`
- `AF.Module.Action`
- `AF.Module.Policy`
- `AF.Module.Economy`
- `AF.Module.Courier`
- `AF.Module.Duel`
- `AF.Module.WorldMap`
- `AF.Module.Scene`
- `AF.Module.Diplomacy`
- `AF.Module.Siege`
- `AF.Module.Knowledge`
- `AF.Module.UI`
- `AF.Bridge.*`

第一阶段优先建立逻辑边界和公共契约，不为了目录图强行拆成许多 DLL。发布契约仍是一个 Bootstrap 加载一个版本化 `AnimusForge.dll` 实现。

## 每个重构切片的必填记录

- owner：Foundation / GameAdapter / 单一 Module / 联合 Bridge
- 改动文件与公共契约
- 影响的渠道、profile、Bannerlord API 线、Harmony/Tick/UI
- 存档 namespace、key/type 和用户数据影响
- 运行频率、缓存、队列上限、主线程边界
- 验证命令和实际结果
- 回滚 commit 或旧 facade
- 下一步和阻塞项

## 状态规则

- `TODO`：尚未开始
- `IN PROGRESS`：正在处理
- `VERIFY`：实现完成但验收未完成
- `DONE`：验收证据完整
- `BLOCKED`：有明确阻塞原因
- `NOT-RUN`：检查未运行，必须写原因；不能当作通过

## 变更意图记录

| 时间 | 任务 | 范围 | 风险 | 验证 | 状态 |
|---|---|---|---|---|---|
| 2026-08-29 | 阶段 2 根 AF 基础 LLM 对话 Owner 映射 | `docs/animusforge-phase2-root-llm-owner-slice.md`；只读核对 Host、Conversation、Prompt/Rule、LLM Gateway、Memory/Persistence、UI adapter 边界 | 保持注册顺序、旧入口、存档 key/type、三渠道和单一程序集；不移动源码 | 已核对真实入口/方法/调用关系；未改生产代码 | IN PROGRESS |
| 2026-08-29 | SubModule 注册/调度分组只读清单 | `docs/animusforge-phase2-submodule-registration-catalog.md`；记录生命周期、Harmony、Model、CampaignBehavior、Mission adapter、ApplicationTick/EngineTick 顺序；不修改源码 | 注册顺序、失败隔离、主线程和 Tick 热路径是组合根风险 | 真实入口与顺序已抽取；运行频率 0；未改生产代码；清单验证通过 | DONE |
| 2026-08-29 | registry DTO 只读设计 | `docs/animusforge-phase2-registry-dto-design.md`；只定义 Host/Composition 元数据和 contribution groups，不接入运行时 | 不持有 Behavior 实例、TaleWorlds 对象、delegate 或 raw dictionary；保持旧 facade、注册顺序、失败隔离、Tick、存档和三渠道 | 设计文档完成；运行频率 0；未编译、未运行、未改变生产行为 | DONE |
| 2026-08-30 | registry validator 输入/输出 fixture | `docs/animusforge-phase2-registry-validator-fixtures.md`；有效快照、无效输入、依赖/顺序/owner/profile/线程/失败隔离输出样例；不实现 validator | 仅文档设计；不持有运行时对象；不改变 SubModule、程序集身份、SyncData key/type、三渠道或 1.3/1.4 构建策略 | 文档已写入；fixture 频率 0；`git diff --check` PASS；工作区边界检查无生产/脚本/配置路径；未编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 2 影响面、候选 Bridge 与回滚地图 | `docs/animusforge-phase2-impact-bridge-rollback-map.md`；首轮覆盖 Save、Prompt/Rule/Tag、Harmony、Tick、UI、线程、API、用户数据、候选 Bridge、非目标与回滚模板 | 只读设计；不移动源码、不接入 registry、不改变旧 facade、三渠道、存档、程序集或发布结构 | 文档已写入；频率 0；`git diff --check` PASS；工作区边界检查无生产/脚本/配置路径；逐文件 contract matrix、实现和实机验证未运行 | DONE |
| 2026-08-29 | Conversation/Memory/Action contract matrix | `docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；三条 contract 边界、逐文件影响、三渠道一致性、有效/无效 fixture 和纯测试矩阵；不实现 DTO/测试 | 保持旧 facade、三渠道、AFEF、存档 key/type、主线程和 1.3/1.4 contract；不移动生产 C# | 设计文档完成；测试 NOT-RUN；方法级映射与 fixture runner 已另行完成；未编译、部署或游戏验证 | DONE |
| 2026-08-29 | Conversation/Memory/Action 方法级映射与纯 fixture 目录 | `docs/animusforge-phase2-conversation-memory-action-method-map.md`、`docs/fixtures/phase2-conversation-memory-action/`；基于真实源码方法行号建立 contract 对应、有效/无效输入和预期输出 | 只读材料；fixture 不在 `.csproj` 中，不引用 TaleWorlds，不改变旧 facade、存档、三渠道或线程边界 | `git diff --check` PASS；生产/脚本/配置路径无变化；YAML 自动解析 NOT-RUN（当前环境无 YAML parser）；未编译/部署/游戏测试 | DONE |
| 2026-08-29 | Settlement/Siege 与 Policy/Diplomacy 候选 Bridge contract | `docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`、`docs/fixtures/phase2-settlement-policy-bridges/`；定义两个候选 Bridge、现有可复用边界、A/B/A+B/A+B+Bridge/Bridge failure 组合和回滚 | 不新增平行动作/通知链；不改变 Policy save/receipt、Settlement/Mission 主线程、旧 facade、程序集、SyncData key/type 或 1.3/1.4 策略 | 文档与 3 个 JSON fixture 已写入；PowerShell `ConvertFrom-Json` 全部 PASS；`git diff --check` PASS；生产/脚本/配置路径无变化；未实现 Bridge/runner，未编译/部署/游戏测试 | DONE |
| 2026-08-29 | Settlement/Siege 与 Policy/Diplomacy Bridge fixture runner | `tools/BridgeFixtureContractTests/validate_bridge_fixtures.py`、`tools/BridgeFixtureContractTests/README.md`；独立标准库 runner，验证 10 个 A/B/A+B/A+B+Bridge/Bridge failure 案例和 6 项不变量 | 不引用 Bannerlord/生产程序集，不调用网络/存档，不接入生产 `.csproj`，不执行 Bridge | 普通输出与 `--json` 输出均 PASS；`bridgeFixtureCases=10`、`invariants=6`；未编译生产 C#、未部署、未游戏测试 | DONE |
| 2026-08-29 | 阶段 3 module manifest/profile/dependency/health catalog 与 runner | `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/fixtures/phase3-module-catalog/`、`tools/ModuleCatalogContractTests/`；设计 8 个逻辑 module/bridge、3 个 profile、依赖/能力/生命周期/health 规则和 16 个无效场景 | 设计-only；不创建 Foundation/Registry，不绑定 entry type，不改变程序集、SubModule、SyncData、存档、构建或发布结构 | runner 普通输出和 `--json` 均 PASS；modules=8、profiles=3、invalidCases=16、healthStates=8；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-29 | 阶段 3 AF.Contracts capability/event/DTO/version 设计与 runner | `docs/animusforge-phase3-af-contracts-design.md`、`docs/fixtures/phase3-af-contracts/`、`tools/AFContractsContractTests/`；设计 9 个 contract、3 个 typed event、6 个 capability 和 18 个无效场景 | 设计-only；不创建 `AF.Contracts` 生产项目，不暴露 live Bannerlord 类型，不改变程序集、SyncData、存档、三渠道或 API 线策略 | 普通输出与 `--json` 均 PASS；contracts=9、events=3、capabilities=6、invalidCases=18；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 Foundation runtime contract 与 runner | `docs/animusforge-phase3-foundation-runtime-contracts.md`、`docs/fixtures/phase3-foundation-runtime/`、`tools/FoundationRuntimeContractTests/`；设计 dispatch、background snapshot/cancellation、diagnostics/trace、SafeMode/lifecycle/health contract 和 16 个无效场景 | 设计-only；不创建 Foundation 生产项目，不接入 SubModule/Tick，不持有 delegate/live object，不改变程序集、存档、SyncData 或 fallback | 普通输出与 `--json` 均 PASS；contracts=6、healthStates=8、invalidCases=16；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 重构台账一致性审查与修正 | `docs/animusforge-refactoring-and-repository-reorganization-plan.md`、`docs/handoffs/2026-08-30-refactor-preparation.md`；修正阶段 3 条目误放阶段 2、阶段状态归属和陈旧验证记录 | 仅文档修正；不改变生产代码、程序集、存档、SyncData、构建/部署流程 | 4 个独立 runner 全部 PASS；`git diff --check` PASS；禁止生产/脚本/配置路径无变化 | DONE |
| 2026-08-30 | 阶段 3 纯组合矩阵与 runner | `docs/animusforge-phase3-composition-matrix.md`、`docs/fixtures/phase3-composition-matrix/`、`tools/CompositionMatrixContractTests/`；覆盖 no-op、required/optional provider、版本不兼容、SafeMode、stale、部分启动失败、Bridge failure、toggle 冲突和 health 边界 | 设计-only；不实现 Module Host、不接入生产 `.csproj`、不改变 SubModule、存档、程序集、Tick 或 fallback | 普通输出与 `--json` 均 PASS；cases=18、invariants=24；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 GameAdapter 1.3/1.4 API boundary 与 runner | `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/`；设计 helper/capability、版本差异、missing member、Bootstrap marker、反射缓存、主线程和 unified package 边界 | 设计-only；不修改现有 helper、条件编译、构建/部署脚本、SubModule、程序集、存档或发布结构 | 普通输出与 `--json` 均 PASS；cases=14、apiLines=2、helpers=7；`git diff --check` PASS；未重新构建/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 最终设计审查 | `docs/animusforge-phase3-final-review.md`；核对阶段 3 checklist、5 份阶段文档、6 个 runner、14 个 JSON fixture、阶段归属、未验证项和禁止路径 | 结论仅覆盖设计/fixture；不代表生产 Foundation/Contracts/GameAdapter、旧存档、双版本运行时或实机验收完成 | 6 个 runner 全部 PASS；14 个 JSON fixture `ConvertFrom-Json` PASS；`git diff --check` PASS；禁止生产/构建/配置路径无变化；审查结论 PASS WITH LIMITATIONS | DONE |
| 2026-08-30 | 准备材料提交与推送 | 当前全部准备文档、fixture 和独立 runner 已暂存并创建本地提交；目标为 `origin/refactor/prepare-af-restructure` | 仅提交已确认的 docs/fixture/runner；无生产 C#、项目、脚本、配置或游戏目录变化 | 本地 commit 已创建；两次 `git push` 均因 GitHub 443 网络连接失败未完成；远端未更新，需网络恢复后重试 | VERIFY |
| 2026-08-29 | 用户决定先保持仓库现状 | 所有参考源码、生成物、用户数据、第三方依赖、工具发行物和归档保持原路径；不删除、不移动、不取消跟踪、不改 `.gitignore` | 暂不处理不会解决许可证/provenance 缺口，但避免误删用户/参考资料 | 用户明确选择 HOLD；未执行清理、移动或去跟踪 | HOLD |
| 2026-08-29 | 参考仓库保留边界确认 | `原版游戏本体代码1.3.x/`、`原版游戏本体代码1.4.5/` 作为用户确认的 tracked 游戏源码参考平面保留；不进入 AF 客户端 ZIP | 参考树与生产源码边界必须清晰；公开分发许可证仍未确认 | 已读取两套参考仓库目录和 tracked 数量；未执行删除/移动/去跟踪 | IN PROGRESS |
| 2026-08-29 | 阶段 1 初版仓库边界与分发决策表 | `docs/animusforge-repository-boundary-decision-table.md`；只建立保守处置分类，不执行删除/移动/去跟踪 | 缺少许可证/第三方清单；用户导出、参考源码、依赖 overlay、ONNX、工具发行物和归档不能默认发布 | 决策表已建立；许可证与 provenance 仍未确认，阶段 1 保持 IN PROGRESS | IN PROGRESS |
| 2026-08-29 | 阶段 1 仓库边界与可重复性审计 | `docs/animusforge-repository-boundary-audit.md`、只读扫描与现有 build/stage/package/deploy 说明；不清理文件、不改脚本 | 17,039 个 `.cs` 中 16,365 个位于原版 1.3/1.4.5 参考树；3,568 个 tracked 文件同时被 ignore 规则命中；许可证/第三方分发政策缺失 | 已完成分类统计、`.gitignore`/tracked-ignored 核对、构建流程读取；许可证/第三方原则、历史 tracked 生成物处置和实际存档/游戏基线仍未完成 | IN PROGRESS |
| 2026-08-30 | 第一版重构地图完成 | `docs/animusforge-refactor-map.md`：运行链、owner、持久化、交互、风险、顺序 | 不移动源码；目标仍为单一 `AnimusForge.dll`、旧存档兼容 | 3 个只读审计结果合并；构建仍被依赖闭包阻塞 | VERIFY |
| 2026-08-30 | 依赖闭包与 unified stage 构建验证 | 无生产 C#、脚本、程序集身份、SyncData key 或游戏目录变更 | 1.3 v1.3.15.110062、1.4 v1.4.6.115628、Bootstrap 均 0 警告/0 错误；stage 成功；实际安装游戏当前为 v1.4.8.119303，未冒充同版本验证 | 仍需旧存档、游戏内与精确 v1.4.8 overlay 验收 | VERIFY |

- 最新 handoff：`docs/handoffs/2026-08-30-refactor-preparation.md`
- 下一位接手者先读取：`CLAUDE.md`、`.claude/skills/animusforge-maintainer/SKILL.md`、本文件、baseline 和最新 handoff。
