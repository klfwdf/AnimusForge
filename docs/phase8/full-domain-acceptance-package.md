# AF 阶段 8：完整 20 领域验收包

日期：2026-09-01；2026-09-03追加 Bridge 接线收尾。切片：`LOCAL-8-A`。本文件是**准备态验收包**，不是发布许可。

## 最准确结论

> 阶段 7 的模块接入与离线/等价 Host 验证已完成大部分，但真实 Campaign/Mission、旧档、
> live Economy、AFEF/Notoriety 和默认入口仍未闭合，因此阶段 7 继续为 `VERIFY`。

> 阶段 8 现在已有完整 20 领域目录、Bridge 矩阵、清理候选、逐项回滚要求和 fail-closed
> readiness 工具，可以并行采集证据；删除旧入口、默认切换、Release/最终部署和发布仍为
> `BLOCKED`。本机另有一次用户明确授权的 Debug 测试部署，但不构成阶段 8 的发布或 LIVE/SAVE 证据。

> **2026-09-03 Bridge 接线收尾与 OFFLINE-GAP 追加：**当前 `FeatureBridgeRuntime` 与既有生产入口的离线绑定已审阅
> 并更新为 `16 bindings / 10 wired / 6 declared-only`。本次 wired 仅表示 source-bound Gate 已在
> 入口调用，不表示真实 Campaign/Mission、LIVE 或 SAVE 验收；六组 `declared-only` 仍不得宣称已接入。
> 项目内 Debug/Release Stage 已重新生成但未部署。较早环境检查曾记录 `status=PASS`、
> `installedMatchesStage=false`、`gameRunning=false`；本轮 `PersistenceIdentityAudit` 因 partial clone
> 缺少 89 个基线源码 blob 按设计返回 `FAIL`/fail-closed，不能把当前审计写成 PASS。本轮没有启动游戏、
> 读取/写入真实存档、部署或切换默认入口。

> OFFLINE-GAP 同步内容包括：真实方法体/顺序 validator 与负例、纯 net8 Bridge 隔离 runner、Phase 8
> 入口候选 inventory（仅补 `entryPaths`）、PersistenceIdentityAudit 单快照/batch/progress/quiet，
> 以及 ModelCatalog 稳定错误码、受限参数和中英文 formatter。20 个领域仍为
> `ownerAssignmentState=ROLE_PLACEHOLDER`、`entryCoverage=REPRESENTATIVE`；readiness 继续 `BLOCKED`。

## Git 与范围

- 已推送共同基线：`9566bf3bec0642ccef6764db6b6630edc195300a`。
- `LOCAL-8-A` 意图 checkpoint：`9a088f2fe765d6b9ec52e902979519c745c77768`。
- 完整 20 领域门禁实现：`b1c5a81a5f4a0b7ccd361f2693b50adc792af0f8`。
- full-domain Bridge SAVE 门禁：`1e341c433c5de34df79d64f525b9addebc920ff3`。
- 未认领领域 owner fail-closed：`f4a02018e7fdc11f4fb8faf3505bc2743669a081`。
- post-review topology/symbol/rollback闭合：`6b1d16f12bc787208126c5c356dadffaecf41dcd`。
- canonical Bridge与cleanup audit最终闭合：`8bdd936345363d869cbdd267c54006cc20a3a694`。
- `LOCAL-8-A` 准备链的历史切片只改纯 Python 工具、fixture/catalog 和文档；2026-09-03
  Bridge 收尾另修改了 `Refactor/Runtime/FeatureBridgeRuntime.cs` 与其纯契约测试。两轮均未修改默认入口、
  `SyncData` key/type、玩法、GCCZ/NEW-10、游戏目录、ONNX 或玩家存档。

## 权威材料

- 完整责任目录：`docs/phase8/full-domain-readiness-catalog.json`。
- 清理盘点：`docs/phase8/cleanup-candidates.json`。
- 证据模板：`docs/phase8/evidence-record.template.json`。
- 空证据清单：`docs/phase8/all-missing.evidence.json`。
- 门禁工具：`tools/PhaseEightReadiness/readiness.py`。

20 个条目是**验收责任桶**，不是 20 个新 DLL、20 个已上线 module，也不改变早期 8-ID
design catalog 的 `entryTypeStatus=Pending` 事实。

目录中的maintainer是逻辑角色占位符，当前20项均为`ROLE_PLACEHOLDER`；入口清单均明确为
`REPRESENTATIVE`，不是全调用图。real readiness在角色改为`ASSIGNED`、入口由owner补齐并改为
`COMPLETE`前，会追加`UNASSIGNED_DOMAIN_OWNER`和
`INCOMPLETE_DOMAIN_ENTRY_INVENTORY/BLOCKED`。

## 20 领域当前验收表

`LOCAL-PASS` 只表示已有离线/compiled 证据；`VERIFY` 表示材料不完整；所有 LIVE/SAVE 仍需
真实游戏与存档证据。

| # | Domain ID | Owner | Default | Offline/compiled | LIVE | SAVE | Release |
|---:|---|---|---|---|---|---|---|
| 1 | `bootstrap-build` | Build.Release | ACTIVE | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 2 | `host-composition` | Foundation.Composition | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 3 | `runtime-diagnostics` | Foundation.Runtime | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 4 | `game-adapter-compatibility` | Compatibility.GameAdapter | LEGACY_DEFAULT | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 5 | `persistence-config` | Foundation.Persistence | LEGACY_DEFAULT | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 6 | `conversation-encounter` | Conversation.Encounter | OPT_IN | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 7 | `gateway-prompt-protocol` | Conversation.Gateway | MIXED_DEFAULT | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 8 | `action-commit` | Interaction.ActionCommit | OPT_IN | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 9 | `memory-afef` | Memory.Persistence | LEGACY_DEFAULT | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 10 | `economy-reward-debt` | Economy.RewardDebt | MIXED_DEFAULT | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 11 | `policy-political` | Policy.Political | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 12 | `world-simulation-worldmap` | World.Simulation | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 13 | `settlement-siege-gccz-sets` | Settlement.Siege | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 14 | `scene-mission-combat` | Scene.Mission | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 15 | `duel` | Duel.Combat | LEGACY_DEFAULT | LOCAL-PASS | NOT-RUN | NOT-RUN | BLOCKED |
| 16 | `courier-proactive-issue` | Courier.ProactiveIssue | MIXED_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 17 | `social-progression-reports` | Social.ProgressionReports | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 18 | `knowledge-persona-profile` | Knowledge.PersonaProfile | LEGACY_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 19 | `ui-tts-external-integration` | UI.ExternalIntegration | MIXED_DEFAULT | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |
| 20 | `tools-content-package` | Tools.ContentRelease | TOOL_ONLY | VERIFY | NOT-RUN | NOT-RUN | BLOCKED |

每个 JSON 条目还绑定真实 `entryPaths`、Prompt/ActionPlan 是否适用、存档责任、失败降级、
blocking gates 和对应 Bridge；没有证据的字段不能用 Stage、截图或注释补成 PASS。

## Bridge 门禁

完整目录记录 16 组跨域责任：

1. `bootstrap-host`
2. `host-runtime`
3. `runtime-game-adapter`
4. `persistence-domain-owners`
5. `conversation-gateway`
6. `conversation-action`
7. `action-memory`
8. `action-economy`
9. `policy-world-diplomacy`
10. `conversation-siege`
11. `scene-duel`
12. `conversation-courier`
13. `memory-social-reports`
14. `gateway-knowledge-profile`
15. `ui-runtime-integration`
16. `tools-content-release`

13组二元`PAIR`必须覆盖A、B、A+B无Bridge、A+B+Bridge、Bridge disabled/failure；
`persistence-domain-owners`、`ui-runtime-integration`、`tools-content-release`是多端点
`CROSS_CUT`，改用`EACH_OWNER_ALONE`、`ALL_WITHOUT_COORDINATOR`、
`ALL_WITH_COORDINATOR`、coordinator disabled/failure。所有组分别检查OFFLINE、LIVE 1.3/1.4、
SAVE 1.3/1.4，证据必须显式列出相应`bridgeIds`。

早期两个显式 Bridge 契约继续保留：

- `af.bridge.conversation-siege`
- `af.bridge.policy-diplomacy`

它们各自原有 case、Composition 的 compatibility/failure/data-preserved/safe-mode case，和
Foundation 全部 18 个 Composition case 也必须在 SAVE 层覆盖；通用 save-roundtrip 不能替代。

### Bridge 绑定清单（离线配置）

`docs/phase8/bridge-binding-manifest.json` 是 16 组 Bridge 的唯一离线绑定清单，逐组记录
`domains`、`topology`、`owner`、真实 `entryPaths`/`symbols`、`implementationState`、
`fallback`、`apiLines` 和 required cases。`runtimeBinding.state` 明确区分：

- `wired`：仅限已经审阅并在生产入口调用 `FeatureBridgeRuntime` Gate 的
  `conversation-gateway`、`conversation-action`、`action-memory`、`action-economy`、
  `policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、
  `memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration`；
- `declared-only`：合同和责任已登记，但没有运行时 caller，不能当作功能已接入或已验收。

纯源代码校验器会拒绝绝对/遍历路径、生成物、终端 UI 文件、缺失 symbol、热路径频率和未经审阅的
`wired` 标记。它只读元数据与源码，不加载游戏、不读存档、不执行 Bridge：

```powershell
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py
python -B -m unittest discover -s .\tools\BridgeBindingContractTests -p 'test_*.py' -v
```

2026-09-02 的历史快照为 `16 bindings / 3 wired / 13 declared-only`；当前离线状态为
`16 bindings / 10 wired / 6 declared-only`。两者都不是 LIVE/SAVE 证据，也不授权默认切换、删除、部署或发布。

## 清理候选

当前目录共18项：

- `KEEP`：12项。包括活跃三渠道facade、Configured Gateway、Policy/World MCM old-save adapter、
  MCM compatibility、save migration tombstone、双 API/Encounter compatibility、GCCZ bridge 和
  仍有真实 caller 的 `CallUniversalApiDetailed`。
- `HOLD`：3 项。包括 Native opt-in runner/result ABI、Prompt parity，以及公共 refactor opt-in
  cutover seam；它们当前不能删。
- `REVIEW_REMOVAL`：3 项，仅为私有窄 flag/恒值分支：
  `VerboseInspectionLogs`、`RefreshAllPlayerAgents`、`_enableRhubarbSoundEventPlayback`。

`REVIEW_REMOVAL` **不等于可删除**。每项仍必须提供：

1. 精确 candidate ID 与 file SHA-256。
2. 静态 caller、反射/Harmony/MCM/注册、public ABI、save identity 均为空的复核。
3. 与 candidate ID 绑定的 OFFLINE、LIVE 1.3/1.4、SAVE 1.3/1.4、RELEASE replacement evidence。
4. 与 candidate ID 绑定的 rollback drill；目标提交必须严格早于 HEAD。
5. 数据副作用说明；`git revert` 不撤销已写入存档的金币、债务、领地、装备等变化。

本轮没有删除任何候选，也没有发现可立即安全删除的完整文件。

## 领域 owner 应提交的证据

每条 evidence record 至少包含：

- `moduleId`：仍使用早期设计目录中的逻辑 ID。
- `domainIds`：本记录实际覆盖的完整责任领域。
- `bridgeIds`：本记录实际覆盖的Bridge；仅写case文本而不绑定ID不计覆盖。
- `cleanupCandidateIds`：仅在替代/rollback 证据时填写。
- `layer` / `kind` / `apiLine`。
- 完整 source commit、release version、artifact hashes 和带时区时间。
- 可重复 steps、expected、observed、case IDs 和至少一份 hash-bound 日志/观察附件。
- `ownerReview=ACCEPTED`，同时包含 module maintainer 与所有相关 domain maintainer。

真实 LIVE/SAVE 记录还必须有：

- 对应 1.3/1.4 的准确 `BuildInfo`。
- 已初始化的 `Campaign` 或 `Mission`，不能是主菜单/进程存在。
- 明确的新档/测试档 identity。
- Bootstrap 与实际实现 DLL 哈希。
- SAVE 层的代表旧档已加载、执行后保存、退出/重载与状态复核。

## 推荐实机顺序

1. Bootstrap 1.3 / 1.4 各启动一次测试 Campaign，确认只加载一个实现。
2. Hero 金币单动作：输入 → owner → ActionPlan → 主线程变化 → confirmed facts → AFEF → 保存重载。
3. Party、Merchant、Debt 分开验证；每次只改变一种副作用。
4. Native、SceneShout、Courier 分开验证历史、取消、退出、重复 completion 和 fallback。
5. Notoriety/weekly 的 exact receipt、真实 `MBRandom`、`ConversationEnded`、保存中断与重载。
6. Duel、WorldMap、Policy/Diplomacy、Scene/Mission、GCCZ/SETS 和普通场景隔离。
7. Proactive/Issue、Knowledge/Persona、UI/TTS/XihaiAction、周报与资源加载。
8. 旧档/坏数据/未知字段组合、Release Stage/ZIP、安装、回滚演练。

使用独立测试存档；不要在唯一正式存档上测试经济、死亡、领地或装备迁移。

## 可重复命令

```powershell
Set-Location -LiteralPath 'E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel'

python -B -m unittest discover -s .\tools\PhaseEightReadiness -p 'test_*.py' -v
python -B .\tools\BridgeFixtureContractTests\validate_bridge_fixtures.py
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py
python -B .\tools\CompositionMatrixContractTests\validate_composition_matrix.py
python -B .\tools\ModuleCatalogContractTests\validate_module_catalog.py

python -B .\tools\PhaseEightReadiness\readiness.py `
  --project-root 'E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel' `
  --manifest 'E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel\docs\phase8\all-missing.evidence.json'
# 必须 BLOCKED / exit 2；这是缺证据的正确结果。
```

## 当前发布门禁

以下授权在 readiness 输出中恒为 `false`：

- `delete`
- `defaultSwitch`
- `deploy`
- `push`
- `publish`

即使真实证据结构最终通过，工具也只返回 `READY-FOR-OWNER-REVIEW`，仍需集成人员和用户对
具体默认切换、删除、Release/最终部署与发布分别授权；工具的 `deploy=false` 不撤销本机已获授权的
Debug 测试安装，也不能把该安装当作发布验收。

## 当前下一步

`LOCAL-7-M1/M2` 已建立 Duel actual-session owner以及 exact detached request-to-Duel provenance；
Native/Scene request在副作用前绑定同一 queued/started `DuelId`，Courier明确拒绝，legacy-unbound
保持独立。Duel仍需真实Campaign/Mission的accept/reject/cancel/death/exit、stake/Memory、旧档和
Fourberie证据，不能把compiled/fixture提升为LIVE或SAVE。`LOCAL-7-C2`、`LOCAL-7-C3` 与本轮
Persistence/Profile 离线收尾已完成；Identity 契约为 `5/5 PASS`，但当前真实审计因 partial clone
缺少 `89` 个基线源码 blob 按设计 fail-closed，不应把旧的 `99/35 PASS` 当作当前结果。没有新的破坏性代码切片获授权。下一步由实机人员
按本验收包补齐20领域LIVE/SAVE、Bridge和rollback drill；在真实证据到齐前，工具闭环不能替代
任何Duel、WorldMap或其他领域的LIVE/SAVE证据。
