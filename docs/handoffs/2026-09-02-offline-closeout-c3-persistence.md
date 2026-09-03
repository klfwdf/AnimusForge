# AF 主体重构：C3 与 Persistence 离线收尾交接

日期：2026-09-02；2026-09-03追加 Bridge 接线收尾与 OFFLINE-GAP 更正

> **2026-09-04 当前工作树校正（优先于历史快照）：**本机以
> `F:\AnimusForge-main` / `refactor/prepare-af-restructure` / HEAD
> `34b3f35811130e26b60a5407451d169de3667dbb` 为准。PersistenceIdentityAudit 实际为 PASS
> （sync=99、behavior=35、module=AnimusForge），Debug Stage 已重新部署并与安装目录三份
> DLL 哈希一致；未启动游戏，LIVE/SAVE 仍未验证。旧的 partial-clone “缺 89 个 blob”记录
> 保留为历史，不适用于本机当前状态。详见
> `docs/handoffs/2026-09-04-live-host-prep-and-current-state.md`。

历史执行工作区（2026-09-02）：`F:\AnimusForge-main`

当前接续工作区（2026-09-03）：`E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel`

本地分支：`refactor/prepare-af-restructure`

远端：`origin/refactor/prepare-af-restructure`

> **当前状态更正（2026-09-03）：**本文 2026-09-02 的 C3、Persistence Identity、Bridge 和
> Debug 部署段落保留为历史快照，不得覆盖当前结论。当前离线缺口以
> `docs/handoffs/2026-09-03-bridge-binding-closeout.md` 的 `OFFLINE-GAP-20260903` 追加为准：
> Bridge validator 为 `20/20 PASS`，Phase 8 全套为 `68/68 PASS`；PersistenceIdentityAudit 契约
> 为 `5/5 PASS`，但本工作树的 partial clone 缺少 `89` 个基线源码 blob，真实审计按设计
> fail-closed，不能记录为当前 Identity `99/35 PASS`。本次本地提交已 push 并完成远端回读；仍未部署、
> 未启动游戏，readiness 仍为 `BLOCKED`。

## 结论

本轮完成了当前可自主执行的离线收尾：`LOCAL-7-C3` 已完成，Persistence/Profile/Config 与
Persistence Identity 的扫描误报已修复，阶段 8 的 20 域/16 Bridge/cleanup/rollback 目录已做
只读一致性复核。阶段 7 仍为 `VERIFY`，阶段 8 执行仍为 `BLOCKED`。

本轮没有启动 Bannerlord，没有进入 Campaign/Mission，没有读取或写入真实存档，没有验证 live Economy、
AFEF、Notoriety、Duel 真实副作用，没有切默认入口或删除 facade。用户随后明确授权了一次 Debug
编译与统一模块测试部署；该部署不等同于 Release/发布验收，且本轮仍未启动游戏。

## 2026-09-03 Bridge 接线收尾追加（历史段落的当前更正）

本追加覆盖上一版 Bridge 段落之后的离线接线与安全修复；上一版 `16 bindings / 3 wired / 13 declared-only`
是 2026-09-02 历史快照，当前结果为 `16 bindings / 10 wired / 6 declared-only / configEnabled=10`。

- 新增的 source-bound Gate 覆盖 `conversation-gateway`、`conversation-action`、`action-memory`、
  `action-economy`、`policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、
  `memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration`。
- `bootstrap-host`、`host-runtime`、`runtime-game-adapter`、`persistence-domain-owners`、`scene-duel`、
  `tools-content-release` 仍是 declared-only；没有运行时 caller，不能当作已接入或已验收。
- `FeatureBridgeRuntime` 的配置路径只接受带 `SubModule.xml`/`ModuleData` 的 `AnimusForge` 模块边界；
  缺失配置使用内建审阅默认值，损坏配置、未知字段、版本错误和非规范大小写 ID 均 fail-closed。
  Action/Memory Bridge 禁用继续保持拒绝/`NoOp`，不触发 legacy 副作用重放。
- Bridge validator `PASS`、Bridge Python 单测历史切片为 `15/15`，OFFLINE-GAP 追加后当前为 `20/20`；
  PhaseEightReadiness 历史切片为 `62/62`，追加入口 inventory 后当前全套为 `68/68`；BridgeFixture、
  Composition、ModuleCatalog、Foundation、GameAdapter、Persistence/Profile、LiveHostReadiness、
  Interaction/Duel/Economy/Gateway/Knowledge/Production suites 与双 API Debug/Release/Bootstrap Stage
  均通过；真实 Campaign/Mission、LIVE/SAVE 仍未运行。

## C3：LiveHostReadinessAudit explicit-root portability

修改范围：

- `tools/LiveHostReadinessAudit/live_host_readiness_audit.py`
- `tools/LiveHostReadinessAudit/README.md`
- `tools/LiveHostReadinessAudit/test_live_host_readiness_audit.py`

行为：

- `--game-root` 改为显式必填，不再默认选择任何 F 盘路径；
- `--project-root` 仍默认解析仓库根目录，可用于纯 fixture；
- 工具继续只读，不启动游戏、不部署、不读取存档内容；
- CLI/fixture 覆盖无参数、显式通过、缺失游戏目录 fail-closed，以及旧机器路径扫描。

证据：C3 测试 `4/4 PASS`；Python 编译通过；`git diff --check` 通过；旧 `F:\SteamLibrary` 与
`F:\AF测试重构` 路径扫描为空。

## Persistence 离线契约收尾

修改范围：

- `tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`
- `tools/PersistenceIdentityAudit.py`
- `docs/fixtures/phase4-persistence-profile-config/persistence-catalog.json`

行为：

- 扫描排除 `.tmp`、`artifacts`、缓存和依赖输出目录，避免把反编译/备份/生成物当生产源码；
- 支持跨 partial 文件解析唯一字符串常量；
- catalog 同步当前 44 个 flattened dictionary storage key；
- 不修改生产 `SyncData` key/type、程序集身份、CampaignBehavior 注册或部署流程。

证据：

- Persistence/Profile/Config：`literalKeys=95`、`typedBindings=121`、`typedBindingTypes=8`、
  `profiles=3`、`flattenedDictionaryKeys=44`、`PASS`；
- Persistence Identity：较早完整基线上的 `sync=99 / behavior=35 / module=AnimusForge / bootstrap=1`
  为历史 `PASS`；本轮 partial clone 缺少 89 个基线源码 blob，真实审计按设计 fail-closed。
- Python 编译通过，历史工作树上的 `git diff --check` 通过；当前工作树的 diff 检查受 partial clone
  缺少 promisor blob 影响，详见 2026-09-03 OFFLINE-GAP handoff。

## Bridge 配置与安全接线

新增 `docs/phase8/bridge-binding-manifest.json` 和
`tools/BridgeBindingContractTests/validate_bridge_bindings.py`，逐路径/逐 symbol 对齐 canonical
16 组 Bridge。2026-09-02 历史状态为 `16 bindings / 3 wired / 13 declared-only`；当前状态为
`16 bindings / 10 wired / 6 declared-only`；`declared-only` 只表示合同、owner 和 required cases
已登记，不表示存在运行时 caller。

当前已有十个边界接入 `FeatureBridgeRuntime`：

- `LegacyConfiguredChatGateway.GenerateExchangeAsync`：conversation-gateway，网络请求前一次性 allow-list Gate，禁用回 Native；
- `InteractionResultCommitter.Commit`：conversation-action/action-memory，共享主线程边界，禁用保持拒绝/NoOp；
- `LegacyNativeActionPlanExecutor.ValidateAndExecuteCore`：action-economy，副作用前校验，禁用回 NoOp；
- `WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved`：policy-world-diplomacy，事件边界，owner 独立；
- `AfGcczShoutBridge.IsActive`：conversation-siege，事件/对话边界，禁用回 native；
- `CourierDeliveryBehavior.IsCourierBridgeEnabled`：conversation-courier，显式 detached 入口共享 Gate；
- `PlayerNotorietyBehavior.ConversationOutcomes.IsSocialReportsBridgeEnabled`：memory-social-reports，exact finalize/weekly sidecar Gate；
- `LegacyKnowledgeRagGateway.GenerateAsync`：gateway-knowledge-profile，委托 configured transport 前 Gate；
- `SceneActionsIntegrationBoundary.InitializeRuntime`：ui-runtime-integration，启动期一次性，失败保持 native UI/runtime。

Gate 不持有 Bannerlord live 对象、不读取存档或文件、不扫描程序集，也没有新增每帧/全量 Tick 工作。
绑定校验及 Debug 双 API Stage 均通过；真实 Bridge/LIVE/SAVE 仍未运行。

## 用户授权的 Debug 编译与测试部署

- 来源：本地 `refactor/prepare-af-restructure`，HEAD `109835cd18fee09ebd591fa254f0af1aa913acb4`；没有切换到 `main`。
- 命令：`powershell -NoProfile -ExecutionPolicy Bypass -File '.\一键编译覆盖推送\build_single_module.ps1' -ProjectRoot 'F:\AnimusForge-main' -BannerlordRoot 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord' -Configuration Debug -Deploy`。
- 引用校验：1.3 `v1.3.15.110062`（`_deps_auto`），1.4 `v1.4.6.115628`（`.tmp\build_check\1.4`）；1.3/1.4/Bootstrap 均 `0 warning / 0 error`。
- 安装目标：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`；事务部署退出码 `0`，部署当时安装模块与项目 Stage 三份 DLL 哈希完全一致。
- 部署时 SHA-256：Bootstrap `BF57E46CF3C095FB3205DBA4A7428339A1C574BC30B3B8DE882822E4ACC2AAE9`；1.3 `5F66A4932AB1948BBB71D38C80C6AADC63AD3F5F508004B1F2469FB13544E970`；1.4 `D28931E9129E3E6F441BC5297466BA99FC886BD9DD15A5C3484B7EFCF598D16C`。
- `SubModule.xml` 仍只声明 `AnimusForge.Bootstrap.dll`；合并 `PlayerExports` 4,753 个文件；既有 `Logs`、`PlayerExports`、`ONNX` 保留；临时部署/备份目录已清理，未发现旧版模块目录。
- 2026-09-02 当时重建的 Debug Stage 实现哈希为 1.3 `BB157A03F97F606158203E3A68F53AEC7687F6BFD5850728760446285CFC2ABE`、1.4 `F43DFD482596BA58501A48723225CF6999E3C2143B0E7029B4363410ED6A5376`；安装目录仍为部署时实现哈希。2026-09-03 收尾后项目内 Stage 已重新生成（精确哈希见 `docs/handoffs/2026-09-03-bridge-binding-closeout.md`），安装目录未改，实机前仍须重新部署。
- readiness：`status=PASS`、`installedMatchesStage=false`、`gameRunning=false` 是 2026-09-02 历史快照，
  只证明当时工具检查通过，不代表 Campaign/Mission 或 LIVE/SAVE。当前 `all-missing` readiness 仍为
  `BLOCKED` / exit 2，且项目 Stage 与安装目录未因本轮离线修复而部署同步。

## Release 离线验证

- 统一 `build_single_module.ps1 -Configuration Release -Stage` 重建 1.3/1.4/Bootstrap，均 `0 warning / 0 error`；未修改游戏目录。
- Production Duel Release replay：`35/35 PASS`，1.3/1.4 parity 通过；只读取生产 Stage 元数据/IL，不启动游戏、不读存档。
- Release ZIP：`F:\AnimusForge-main\.tmp\packages\release-final-20260902\AnimusForge_v1.3.7.2_20260902_100952_233.zip`；4919 entries；SHA-256 `1215A88666E6FCCD949BE413C75719B2C96BCA061546FCAD86DB9AB0F805ACE5`。Bootstrap-only XML、双实现 marker/hash、ONNX/旧模块排除均通过。
- Release 离线工件不等同于实际安装、Campaign/Mission、LIVE/SAVE、默认切换或发布签收。

## 阶段 8 只读一致性复核

- canonical domain：20 个；Bridge：16 组；cleanup inventory：18 项；
- cleanup disposition：`KEEP=12`、`HOLD=3`、`REVIEW_REMOVAL=3`；
- 20 个 owner 仍为 `ROLE_PLACEHOLDER`，20 个入口仍为 `REPRESENTATIVE`；
- readiness 工具的 `delete/defaultSwitch/deploy/push/publish` 授权字段仍全部为 `false`；本次 Debug 部署来自用户单独的明确授权，不改变阶段 8 的 Release/破坏性操作门禁；
- `all-missing.evidence.json` 输出 `BLOCKED`，退出码 2，`acceptedEvidenceCount=0`；
- 阶段 8 readiness 历史切片为 `62/62`，OFFLINE-GAP 追加后为 `68/68`；Bridge 10 cases/6 invariants、Composition 18 cases/24 invariants、
  ModuleCatalog 8 modules/3 profiles/16 invalid cases/8 health states 均通过。

## 环境阻塞与未运行项

- 本机已具备 .NET SDK 6.0.428/8.0.421/10.0.400 及对应 runtime；5 个 net6 smoke 均通过：AgendaMap `3`、WeeklyReport `PASS`、WorldDiplomacy Compression `269`、Intent Boundary `1168`、Result Settlement `453`。SDK 10 对 net6 仅给出生命周期提示 `NETSDK1138`，没有测试失败。
- `ActionPostprocessPromptLab` 与 `PreprocessTopicPromptLab` net10 smoke 均通过；`PlayerExportsEditor` smoke 退出码 `1`，原因是现有 PlayerExports 内容校验错误（多条 `RagShortText exceeds 100 characters`，以及 `Variants[].When.Cultures`/`Keywords` 类型错误），不是 SDK/runtime 缺失。测试临时删除用例已清理，未修改 4,753 个生产数据文件；不得吞掉这些内容错误或擅自批量改数据。
- 真实 Campaign/Mission、LIVE、SAVE、旧档、Release 实际安装、默认切换、最终发布仍 `NOT-RUN/BLOCKED`；Release Stage/ZIP 离线证据已完成，本次 Debug 安装只作为后续获授权实机测试的准备，且当前实现 DLL 需先重新部署。

## 工作树与回滚

终端 UI 本地改动已按用户要求先备份并隔离，不进入本轮提交：

- 备份目录：`F:\AFMOD\backups\AF-REFACTOR-terminal-ui-20260902-075113`
- `AnimusForgeTerminalBehavior.cs` 已恢复远端版本；两个远端不存在的新增 UI 文件已移除；备份哈希与
  原始本地文件一致。

C3/Persistence/Bridge 相关文件是本轮新增或修复。回滚时只针对对应文件审阅并生成普通反向提交；
不要 `hard reset`、不要覆盖用户备份、不要改写共享历史。既有 Duel/C2 实现仍按各自技术 handoff
的普通 `git revert` 规则处理，回滚后离线产物必须重新验证。

## 下一步

实机人员获得明确授权后，先重新部署当前已核验 Stage，再按 `docs/phase8/full-domain-acceptance-package.md` 在隔离存档补齐
20 域 LIVE/SAVE、旧档往返、Bridge 和 rollback drill；在此之前阶段 7 保持 `VERIFY`，阶段 8
破坏性清理、默认切换、Release/最终部署和发布保持 `BLOCKED`。
