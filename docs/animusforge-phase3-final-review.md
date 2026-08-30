# 阶段 3 最终设计审查

- 审查日期：2026-08-30
- 审查结论：PASS WITH LIMITATIONS
- 阶段状态：DONE（设计完成；生产实现和实机验收未开始）
- owner：Foundation/Host/Composition；GameAdapter、各模块 owner 共同审阅
- 工作区：`F:\AnimusForge-main`
- 分支：`refactor/prepare-af-restructure`
- 审查范围：阶段 3 Contracts 与基础运行时的设计、fixture、独立 runner、台账和 handoff

## 1. 审查结论

阶段 3 的设计范围已完成，可以进入阶段 4 `Persistence / Profile / Config` 的设计准备。该结论只表示公共边界、metadata、fixture 和纯验证材料完成，不表示已经创建或接入 `AF.Contracts`、`AF.Foundation.Runtime`、Module Host、Registry、GameAdapter 或任何新物理 DLL。

阶段 3 不应被解释为：

- 生产 C# 已重构；
- 1.3/1.4 运行时行为已经重新验收；
- 旧存档已经完成迁移验收；
- SafeMode/Module Host 已经在游戏中运行；
- Bridge 已经实现；
- 可以删除旧 facade 或改变 `SubModule.cs`。

## 2. 审查项目

| 项目 | 证据 | 结果 |
|---|---|---|
| Module manifest/profile/dependency/health | `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/fixtures/phase3-module-catalog/`、`tools/ModuleCatalogContractTests/` | PASS |
| AF.Contracts capability/event/DTO/version | `docs/animusforge-phase3-af-contracts-design.md`、`docs/fixtures/phase3-af-contracts/`、`tools/AFContractsContractTests/` | PASS |
| Foundation dispatch/background/diagnostics/SafeMode | `docs/animusforge-phase3-foundation-runtime-contracts.md`、`docs/fixtures/phase3-foundation-runtime/`、`tools/FoundationRuntimeContractTests/` | PASS |
| No-op/dependency/optional/SafeMode/failure isolation | `docs/animusforge-phase3-composition-matrix.md`、`docs/fixtures/phase3-composition-matrix/`、`tools/CompositionMatrixContractTests/` | PASS |
| GameAdapter 1.3/1.4 boundary | `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/` | PASS |
| Stage ownership and cross-cutting constraints | phase 2 maps, owner matrix, SubModule catalog, handoff | PASS |

## 3. 纯验证证据

本次审查实际运行：

```text
Bridge fixture runner:
PASS bridgeFixtureCases=10 invariants=6

Module catalog runner:
PASS moduleCatalog modules=8 profiles=3 invalidCases=16 healthStates=8

AF.Contracts runner:
PASS afContracts contracts=9 events=3 capabilities=6 invalidCases=18

Foundation runtime runner:
PASS foundationContracts contracts=6 healthStates=8 invalidCases=16

Composition matrix runner:
PASS compositionMatrix cases=18 categories=6 invariants=24

GameAdapter runner:
PASS gameAdapter cases=14 apiLines=2 helpers=7
```

补充验证：

- Phase 2/3 JSON fixture `ConvertFrom-Json`：14 个文件全部 PASS；
- `git diff --check`：PASS；
- 触碰文档/runner 中无字面量 `\\n`；
- 禁止生产、构建、部署和配置路径：NONE。

## 4. 已确认的不可变约束

- 继续单一 `Modules/AnimusForge` 发布模块；
- `SubModule.xml` 只加载 Bootstrap；
- Bootstrap 只选择一个实现；
- 保持 `AnimusForge` / `AnimusForge.Bootstrap` 程序集身份；
- 不改变现有 SyncData key、序列化类型和旧存档身份；
- 不把 `Game`、`Mission`、`Agent`、`Hero`、`Settlement` 等 live 类型放入公共 contract；
- 不把 delegate、MethodInfo、raw dictionary、Prompt、API key、原始网络响应放入公共 contract；
- Game/Mission/Agent/UI/存档修改仍在主线程；后台只接收 immutable snapshot；
- ApplicationTick 与 EngineTick 保持分离；
- Bridge 缺失/失败不拖垮参与模块；
- SafeMode 保留数据，不删除未知 namespace，不自动迁移，不自动替换 gameplay；
- 阶段 1 仓库现状继续保持，不删除、不移动、不取消跟踪、不改 `.gitignore`。

## 5. 未验证项和限制

以下内容明确保持 `NOT-RUN` 或未完成：

- 真实 `AF.Contracts` / `AF.Foundation.Runtime` 生产类型；
- 真实 Module Host、Registry、lifecycle disposer；
- 真实 GameAdapter 生产抽取；
- 1.3/1.4 本轮重新构建；
- 精确 `v1.4.8.119303` overlay 构建复现；
- Harmony runtime target 和 patch conflict 实机行为；
- SafeMode 游戏内加载；
- 旧存档重新加载和迁移；
- 打包、部署、ZIP allowlist/hash；
- 三渠道实机回归；
- 许可证、第三方 provenance 和公开分发原则。

这些未验证项不阻止“阶段 3 设计完成”，但阻止生产重构和最终发布验收。

## 6. 下一阶段入口

阶段 4 `Persistence / Profile / Config` 尚未开始。下一项准确任务：

1. 盘点 `SyncData`、JSON、PlayerExports、AFEF 和旧数据；
2. 建立 persistence namespace/key/type/chunk/legacy fallback catalog；
3. 设计 migration catalog 和 idempotency/commit point；
4. 设计 profile/settings snapshot 与模块开关边界；
5. 保留旧程序集身份、序列化类型和 SyncData key；
6. 为缺失、损坏、旧版本、disabled/failed module data 建立纯 fixture；
7. 先不修改生产 C#、不改构建脚本、不部署。

只有完成阶段 4 设计和对应验证，并由用户明确授权后，才考虑第一个生产切片。

## 7. 回滚点

本审查没有生产代码变化。未提交工作区的基准参考仍为：

```text
7567c59f docs(refactor): refresh AF handoff status
```

回滚应只撤销本次审查文档和阶段状态更新，不应整体恢复计划/handoff 文件，以免丢失此前阶段记录。