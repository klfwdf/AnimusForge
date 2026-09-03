# AF Bridge 绑定与安全接线离线收尾交接

日期：2026-09-03

工作区：`E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel`

分支：`refactor/prepare-af-restructure`

远端：`origin/refactor/prepare-af-restructure`

## 结论

本轮完成 Bridge 配置与既有生产入口的离线安全接线，并完成 OFFLINE-GAP-20260903 的离线
修复、源码/fixture/compiled 和双版本 Stage 回归。当前清单为：

```text
16 bindings / 10 wired / 6 declared-only / configEnabled=10
```

这表示十组 Bridge 已有经过审阅的 source-bound Gate；六组仍只有合同/owner/required cases
登记。它不代表真实 Campaign/Mission、LIVE、SAVE、旧存档、默认入口切换或发布完成。阶段 7
总体保持 `VERIFY`，阶段 8 执行保持 `BLOCKED`。

## OFFLINE-GAP-20260903 追加（当前结论）

本节覆盖本 handoff 原有 Bridge 收尾记录之后的离线缺口修复；当前本地 HEAD 为
`ab6ce72dffa45fb0b9bc8f5f00669b3c33b5981a`，相对 `origin/refactor/prepare-af-restructure`
为 ahead 8 / behind 0，尚未 push。两个既有 `.branch-archive*.zip` 未加入提交、未删除、未读取。

### 修复范围

- Bridge validator 现在会屏蔽 C# 注释/字符串，按真实方法声明和花括号提取方法体，校验 gate
  是否位于凭据、网络、owner 回调、提交或玩法副作用之前；`conversation-siege` 额外检查缓存
  字段初始化器。负例覆盖跨方法 gate、gate 晚于副作用、错误 ID、伪造调用和错误缓存初始化。
- 新增纯 `net8.0` `BridgeRuntimeIsolationTests`，每个场景使用独立子进程和临时
  `AnimusForge/ModuleData`；覆盖缺配置、`enabled=[]`、malformed/duplicate/unknown/大小写错误/
  版本错误、CWD 同名诱导配置、fallback 一致性和 `bridge.unknown`/
  `bridge.contract_version_mismatch`/`bridge.stale_generation` reason code。
- 新增 Phase 8 `entry_inventory.py`，按审阅 pattern 稳定排序输出真实候选路径及来源原因；只补
  `entryPaths`，20 个领域仍为 `ROLE_PLACEHOLDER` / `REPRESENTATIVE`，不扩展 Bridge runtime
  entryPaths，也不制造 COMPLETE/LIVE/SAVE 证据。
- `PersistenceIdentityAudit.py` 当前源码只枚举/读取一次，基线 tree 只加载一次，基线源码使用
  单次 `git cat-file --batch`；新增 stderr 阶段进度和 `--quiet`，`--json` 保持 stdout 纯 JSON，
  失败时 fail-closed。
- `LegacyModelCatalogGateway` 增加稳定错误码、受限只读 `ErrorArguments` 和中英文 formatter；
  `DuelSettings`、`ModOnboardingBehavior` 按错误码映射，同时保留旧构造函数和中文 UI 文案。

### 当前验证

- Bridge validator：`16 bindings / 10 wired / 6 declared-only`；Bridge Python 单测 `20/20 PASS`。
- Bridge 隔离 runner：`9 scenarios PASS`；Phase 8 inventory `PASS`，Phase 8 全套单测 `68/68 PASS`。
- PersistenceIdentityAudit 契约测试：`5/5 PASS`。真实审计因 partial clone 缺少 `89` 个基线源码
  blob 返回 `FAIL`（`baseline source blob unavailable (89 missing)`），这是预期的 fail-closed，
  不能记录为当前 identity PASS。
- ModelCatalog replay：`PASS`，覆盖 URL/API key/cancellation/HTTP/transport 错误码、中文兼容文案、
  英文 fallback、参数边界和凭据不泄露。
- Debug/Release 的 1.3、1.4 和 Bootstrap unified Stage 均为 `0 warning / 0 error`；只做项目内
  `-Stage`，未部署。
- `all-missing` readiness 仍为 `BLOCKED` / exit `2`，`fullProjectReleaseReady=false`，所有
  delete/defaultSwitch/deploy/push/publish 授权均为 `false`。

### 不变边界

未启动游戏、未进入真实 Campaign/Mission、未读取或写入真实存档、未部署、未切换默认入口、未删除
facade，未修改模块发布结构、程序集身份、`SubModule.xml`、`SyncData` key/type 或构建脚本。
阶段 7 继续 `VERIFY`，阶段 8 继续 `BLOCKED`。

## 接续与范围

- 接续 checkpoint：`13e21560`；接线审查 checkpoint：`231f6cb6`。
- 收尾开始时本地 `HEAD=231f6cb6`，远端 `HEAD=e5af64fb`，ahead 2；最终文档提交后只允许普通
  fast-forward push 到 `refs/heads/refactor/prepare-af-restructure`。
- 本轮修改范围：`Refactor/Runtime/FeatureBridgeRuntime.cs`、相关已接线 adapters/behaviors、
  `AnimusForge/ModuleData/FeatureBridges.json`、Bridge manifest/validator/test，以及总纲、阶段 8
  验收包和交接文档。
- 没有修改终端 UI；本地 UI 备份仍在
  `F:\AFMOD\backups\AF-REFACTOR-terminal-ui-20260902-075113`，不进入提交。
- 没有修改默认 Native/SceneShout/Courier 入口、facade 删除策略、`SyncData` key/type、程序集
  身份、构建/覆盖脚本、双模块输出结构、GCCZ/NEW-10、游戏目录或玩家存档。

## 当前 Bridge 清单

### Wired（10 组，source-bound）

| Bridge | 入口 | 频率 | 禁用/失败 fallback |
|---|---|---|---|
| `conversation-gateway` | `Refactor/Adapters/LegacyConfiguredChatGateway.cs::GenerateExchangeAsync` | event | Native |
| `conversation-action` | `Refactor/Runtime/InteractionResultCommitter.cs::Commit` | event | NoOp/拒绝 |
| `action-memory` | `Refactor/Runtime/InteractionResultCommitter.cs::Commit` | event | NoOp |
| `action-economy` | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs::ValidateAndExecuteCore` | event | NoOp |
| `policy-world-diplomacy` | `WorldDiplomacyBehavior.cs::NotifyExternalDiplomacyResolved` | event | Native |
| `conversation-siege` | `AfGcczShoutBridge.cs::IsActive` | event | Native |
| `conversation-courier` | `CourierDeliveryBehavior.cs::IsCourierBridgeEnabled` | event | NoOp |
| `memory-social-reports` | `PlayerNotorietyBehavior.ConversationOutcomes.cs::IsSocialReportsBridgeEnabled` | event | NoOp |
| `gateway-knowledge-profile` | `Refactor/Adapters/LegacyKnowledgeRagGateway.cs::GenerateAsync` | event | Native |
| `ui-runtime-integration` | `SceneActionsIntegrationBoundary.cs::InitializeRuntime` | startup once | SafeMode/native UI |

`conversation-action` 与 `action-memory` 共享同一个主线程 `Commit` 边界，因此十组 Bridge 对应
九个实际调用点；这不是额外的重复管线。

### Declared-only（6 组）

`bootstrap-host`、`host-runtime`、`runtime-game-adapter`、`persistence-domain-owners`、
`scene-duel`、`tools-content-release`。

这些条目不应添加“伪 caller”来凑数量；必须由对应 owner 提供真实入口、兼容/失败降级和
OFFLINE/LIVE/SAVE 证据后，才能另行变更 `runtimeBinding.state`。

## 安全接线规则

1. `FeatureBridgeRuntime` 只在初始化时读取一次可选配置，并把结果降为不可变 allow-list；没有
   每帧扫描、程序集发现、网络访问或 live 对象缓存。
2. 配置路径必须位于同时具备 `AnimusForge` 文件夹、`SubModule.xml` 和 `ModuleData` 的模块边界。
   缺失模块配置使用审阅过的内建默认值，不读取进程当前目录的同名文件。
3. 配置损坏、重复/未知字段、版本不匹配、非法 ID、非规范大小写、未允许的 implementation
   state 或超限文件均 fail-closed；`Initialize` 返回带错误码的明确诊断。
4. 禁用 action/memory Bridge 时保持现有拒绝/`NoOp` 语义，不把一次失败改成 legacy fallback，
   避免部分副作用重放；其他入口沿 manifest 声明的 Native/owner/SafeMode fallback 处理。
5. Gate 不新增存档字段、网络协议或跨域 Bannerlord 对象持有；真实副作用仍由原 domain owner
   在其既有主线程/事件边界负责。

## 验证结果

- `python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py`：
  `PASS bridgeBindings=16 wired=10 declaredOnly=6 configEnabled=10`。
- `python -B -m unittest discover -s .\tools\BridgeBindingContractTests -p 'test_*.py' -v`：
  历史 Bridge 接线切片为 `15/15 PASS`；OFFLINE-GAP 追加负例后当前为 `20/20 PASS`。
- PhaseEightReadiness：历史接线切片为 `62/62 PASS`；OFFLINE-GAP 入口 inventory 与追加测试当前为
  `68/68 PASS`；`all-missing` 按设计返回 `BLOCKED`/exit 2。
- BridgeFixture：`10 cases / 6 invariants PASS`；Composition：`18 cases / 24 invariants PASS`。
- ModuleCatalog：`8 modules / 3 profiles / 16 invalid cases / 8 health states PASS`；Foundation：
  `6 contracts / 8 health states / 16 invalid cases PASS`；GameAdapter：`14 cases PASS`。
- Persistence/Profile：`95 literal / 121 typed / 44 flattened PASS`；较早完整基线上的 Identity
  结果为 `99/35`、模块身份 `AnimusForge`、Bootstrap-only；本轮 partial clone 重跑因 89 个基线
  blob 不可用而按设计 fail-closed。LiveHostReadiness 历史检查为 `PASS` 且 `gameRunning=0`。
- Interaction、Duel、Economy、Configured Gateway/Validation、Knowledge/RAG、Production host、
  Production Duel fresh replay（`35/35`，1.3/1.4 parity）均通过。
- 1.3/1.4/Bootstrap Debug 与 Release unified Stage：均 `0 warning / 0 error`。
- 本次最终项目内 Stage 哈希（未部署）：Debug Bootstrap `AC6CBCF27133BF4C9B31EBEEB124DC50ED5DB4B70E1752AF1AE70D8DD54E64E2`、
  1.3 `83319FE19035CDED2C1FC9C47115959F059E717B98B8A2EBBD44F3B6E05F4A43`、1.4
  `343CD34D433198088919A016FE4E453AFF7CED69C02EAE9B0509D50F83972F99`；Release Bootstrap
  `65245C00C3BF76405555BC36CF82A26EFCD7F7ACA24C64EF63C6EE4DE42125C3`、1.3
  `B12E2D87132885889209C710EA8625DFEDD37B811380791734BD6BD56A853708`、1.4
  `A71EF195AA725231E560BEF3DCFBB11DB033A442D0DDDAC2DDE96B092C89CFEF`。
- `git diff --check`、凭据/私有路径/生成物/终端 UI/模块身份/`SubModule.xml` 审查通过；
  `SubModule.xml` 仍只加载 `AnimusForge.Bootstrap.dll`。

## 未运行与限制

- 未启动 Bannerlord，未进入真实 Campaign/Mission，未读取或写入真实存档。
- 未验证真实 LIVE/SAVE Bridge、live Economy/AFEF/Notoriety、Duel 真实死亡/赌注/Fourberie、
  旧档往返、默认入口切换、旧 facade 删除、Release 实际安装或最终发布。
- 因此不能把 `wired`、compiled、fixture、Stage 或 Debug 安装写成阶段 7 DONE、阶段 8 DONE 或
  可发布游戏版本。

## 回滚与推送

- 代码和测试回滚使用针对文件的普通反向提交；不得 `git reset --hard`、`git checkout --`、
  force push 或 rebase 共享历史。
- 配置安全修复和接线可分别审阅后以普通 `git revert` 回滚；回滚后所有 Stage/fixture/replay
  工件视为 stale，必须重新构建和验证。
- 终端 UI 只从备份目录恢复或按用户明确指示处理，不从 Git 提交中删除/覆盖备份。
- 授权推送命令：

  ```powershell
  git push origin HEAD:refs/heads/refactor/prepare-af-restructure
  ```

- 推送后必须核对：

  ```powershell
  git fetch origin --prune
  git rev-parse HEAD
  git rev-parse origin/refactor/prepare-af-restructure
  git status --short --branch
  ```

  完成条件是本地与远端 HEAD 相同，ahead/behind 为 `0/0`，且工作树 clean。

## 制作组下一步

先在明确的实机授权下重新部署与当前源码匹配的 Stage，然后按
`docs/phase8/full-domain-acceptance-package.md` 在隔离存档补齐 20 领域的 LIVE/SAVE、Bridge
failure/disabled、旧档往返和 rollback drill；owner/entry coverage 仍需从占位状态改为真实认领。
在这些证据完成前，保持阶段 7 `VERIFY`、阶段 8 执行 `BLOCKED`，不要删除 facade、切默认或发布。
