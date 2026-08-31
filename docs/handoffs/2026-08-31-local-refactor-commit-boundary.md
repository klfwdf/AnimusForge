# AF local continuation: detached commit boundary

日期：2026-08-31。这是 AF 主体重构的一个已实现切片，不是阶段 7 总体验收或可发布版本。

## 身份、范围与回滚

- 本机 worktree：`G:\AFMOD\AF-REFACTOR`。
- 分支：`codex/af-main-refactor-continuation-20260831`。
- 两次 fetch 均确认远端 `origin/refactor/prepare-af-restructure` 为 `182da1db4db4199cf65783f911f3cb6d46b18970`；没有回退到旧 `a096c1b1`。
- 本地 checkpoint：`8020112e`；fixture 导航行号校正：`7acc0c78`；运行时修复：`b24fdf4b34757759379a6d2a1b839e511715f98b`。本说明与 owner matrix/公共台账另作本地文档提交。
- NEW-10 保持 `0006d45b`、GCCZ 保持 `3849f6f`，收尾检查两者工作区干净。其他成员机器的未提交工作未知；没有合并、推送或改变其分支。
- 功能回滚用 `git revert b24fdf4b` 的小型反向提交；本轮未执行回滚。不要 hard reset、覆盖 NEW-10 或恢复旧 DLL 到游戏。
- Owner：Conversation lifecycle / GameAdapter dispatch contract。没有变更 Economy/GCCZ 玩法、公开接口签名、save key/type、程序集身份、资源、Harmony、默认三渠道入口或官方一键构建/覆盖脚本。

## 入口与缺陷证据

入口：Native opt-in runner、Shout detached host、Courier inbound/reply detached host → `DetachedInteractionHost.ExecuteAsync` → dispatcher callback → `InteractionResultCommitter.Commit` → action owner → memory → optional `afterCommit`。

旧 Host 把提交中或提交后的失败当成可重新走旧链路的基础设施失败。例如动作已经执行，随后 memory append 抛异常，committer 返回 `NonRetryableFailure / ActionsExecuted=true / HistoryWritten=false`，Host 仍调用 legacy fallback，并把 fallback 成功返回成 `Succeeded`。`afterCommit` 或 dispatcher 在 callback 后抛异常/丢返回值，也有同样风险。

- 原始源码回归失败日志：`G:\AFMOD\.build-cache\af-refactor-20260831\host-boundary-red.log`。
- 原始 staged 1.4 DLL 实测：`NativeConversation/memory_throw retried a possibly applied commit: Succeeded`，见 `production-configured-boundary-red.log`。
- 这是可控 action/memory port 下的生产 Host 行为，不是实际 Hero 金币已经被转移的游戏证据。

## 修复与清理

- 一个 request-local 原子门控制 callback：pending → consumed，或 pending → closed。无论重复调用还是在 dispatcher 已返回/抛异常后才调用，关闭/已消费的 callback 都不再执行动作、记忆或 `afterCommit`。
- callback 一旦开始，其后失败不再调用 legacy fallback；已观察到的 commit receipt 保留。未知/不完整结果不伪装成功，也不伪造 AFEF。
- 尚未开始的 dispatch 失败仍保留旧 fallback，但先关闭 callback，避免迟到 callback 再执行一次。
- generate 完成后及主线程 callback 入口检查 cancellation；`afterCommit` 只有成功且 `HistoryWritten=true` 才执行。
- 每次交互固定数量的原子操作和局部字段；无 tick、反射热路径、全量扫描、新队列或持久化缓存。
- Removed：原来不区分 commit 是否开始的 fallback 分支被替换；生产 replay 中无用的 `useFallback` 参数删除，facade 用 `using` 释放。尝试直接静态引用 AF DLL 的临时 runner 会触发依赖游戏程序集的 module initializer，已移除其源码，改为扩展仓库既有 reflection replay；未禁用或替换生产 initializer。
- Kept：所有仍有调用的 Legacy facade 与提交前 fallback。它们尚未完成游戏/存档验收，不是可随意删除的死代码。

## 实际验证

| 检查 | 结果 / 层次 |
| --- | --- |
| Debug 1.3 + 1.4 + Bootstrap unified Stage | 初始与修复后均 PASS，每项 0 warning / 0 error；project-local only |
| InteractionPipelineContractTests | 原 40 cases PASS；新 fault matrix 48 cases PASS（三渠道 × 16 场景） |
| ProductionConfiguredHostReplayTests | 新 1.4 DLL 的原三渠道/provider/cancel 测试 PASS；提交后 4 类故障 × 3 渠道 = 12 cases PASS |
| ProductionEconomyAwareCommit / ProductionEconomyOwner | PASS；mixed/economy-only receipt 与 Hero/Party/Merchant factory fail-closed，不是 live inventory |
| ProductionDetachedHost / ProductionCourierHost / ProductionValidationProvider | PASS；控制 Host、inbound/reply 与 provider-specific fixture |
| ConfiguredChatGateway / ConfiguredChatValidation / ModelCatalog / PrimaryLlmGateway / KnowledgeRagGateway / XihaiClassifierTransport | 已执行的 replay PASS；ConfiguredChatGateway 为基线执行，其生产代码本轮未改 |
| EconomyRewardDebtPort / EconomyAwareActionPlanExecutor / PersistenceChunk | PASS；PersistenceChunk 为基线执行，其生产代码本轮未改 |
| BridgeFixture / FoundationRuntime / AFContracts / ModuleCatalog / GameAdapter / CompositionMatrix | 六项 Python metadata/fixture runner PASS |
| PersistenceProfileConfig / PersistenceMigration / PersistenceIdentity / EconomyOwnerState | 四项 Python runner PASS；identity 为 sync=99、behavior=35、module=AnimusForge |
| Cleanup | `git diff --check`、冲突标记、TODO/HACK/TEMP/旧参数/新增测试引用检查 PASS；legacy/temporary 命中仅为仍有效 fallback 或故障 fixture |

初始 Persistence/Profile/Config 报 `typed SyncData binding catalog drifted`。121 条绑定中仅 `MyBehavior.cs` 的 `_patienceStates_v1` 两个 ref 行号过时（37172/37181 → 37010/37019）。只改 fixture 导航行号，仍严格比较 key/type/ref/source/line；没有放宽检查或改生产 SyncData。

### 未通过或未执行

- 环境加载失败，断言未完成：PolicyGatewayReplayTests、WorldDiplomacyGatewayReplayTests 缺 `MCMv5, Version=5.12.3.0`；TtsGatewayReplayTests、ProductionOptInEntryReplayTests 缺 `TaleWorlds.CampaignSystem`。这四个 `.csproj` 依赖复制硬编码另一台机器的 `F:\SteamLibrary`；完整失败日志为 `final-<Runner>.log`。不能说全套回归全绿。
- 真实 Campaign/Mission、Hero/Party/Merchant 库存/债务、AFEF 写入和旧存档加载：NOT-RUN。未启动或部署游戏；readiness 的 `gameRunning=false`、`installedMatchesStage=false`，后者仅比较 Bootstrap。
- `.NET 10` PromptLab/PlayerExportsEditor：NOT-RUN，本机已确认只有 SDK 8.0.422，未降目标框架或安装 SDK。
- XihaiAction 独立 net472 runtime、Release build、ZIP/package、真实外部 provider：本轮未执行；不能把别台机器的 Developer Pack 失败当成本机已经重现的事实。
- 保护范围限一次 `ExecuteAsync`。尚未证明跨新请求、重新创建 facade、读档后的 action receipt exactly-once，也未实现部分经济事务回滚或 memory 失败后的恢复补写。下一阶段必须单独审查这些边界。

## 本机重现命令

```powershell
Set-Location -LiteralPath 'G:\AFMOD\AF-REFACTOR'
$env:DOTNET_ROOT = 'C:\Users\28358\AppData\Local\Microsoft\dotnet'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
$env:DOTNET_CLI_HOME = 'G:\AFMOD\.build-cache\af-refactor-cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:NUGET_PACKAGES = 'G:\AFMOD\NEW-10\.dotnet_cli\.nuget\packages'

powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 `
  -ProjectRoot 'G:\AFMOD\AF-REFACTOR' `
  -BannerlordRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
  -Bannerlord13ReferenceDir 'G:\AFMOD\NEW-10\_deps_auto' `
  -Bannerlord14ReferenceDir 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
  -WorkshopContentDir 'E:\steam\steamapps\workshop\content\261550' `
  -RuntimeDependencyDir 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' `
  -Configuration Debug -Stage

dotnet run --project .\tools\InteractionPipelineContractTests\InteractionPipelineContractTests.csproj
dotnet run --project .\tools\ProductionConfiguredHostReplayTests\ProductionConfiguredHostReplayTests.csproj
dotnet run --project .\tools\ProductionEconomyAwareCommitReplayTests\ProductionEconomyAwareCommitReplayTests.csproj
dotnet run --project .\tools\ProductionEconomyOwnerReplayTests\ProductionEconomyOwnerReplayTests.csproj
python -B .\tools\PersistenceProfileConfigContractTests\validate_persistence_profile_config.py
python -B .\tools\PersistenceIdentityAudit.py
git diff --check
```

其他已执行 .NET runner 的命令同形：`dotnet run --project .\tools\<Runner>\<Runner>.csproj`；Python 的六项 metadata runner 位于相应 `tools\*ContractTests\validate_*.py`，Migration/Identity/OwnerState 为 tools 根目录的同名 `.py`。每次执行后单独检查 `$LASTEXITCODE`；不要用后一个命令的退出码掩盖失败。

本机日志统一放在 `G:\AFMOD\.build-cache\af-refactor-20260831`。Stage 为 `G:\AFMOD\AF-REFACTOR\bin\Debug\single_module_stage\AnimusForge`。本机游戏 BuildInfo 为 `v1.4.7.117484`，不能和固定构建引用混淆。

| 修复后产物 | ReferenceGameVersion | SHA-256 |
| --- | --- | --- |
| Bootstrap | v1.3.15.110062 | `61CE5EA5D5749BD587B1BF92389813F2C787027470F9C3AB72B68BCBE7C99579` |
| 1.3 implementation | v1.3.15.110062 | `0EEE3FC6ABDA9CB438FB6E05DE0F360F19EE20729F34B1DDA1E9E9BEA24F8377` |
| 1.4 implementation | v1.4.6.115628 | `5FDA213828DF3A8EFE297C0456A93892D70220E938E35A3FADC17A7561A61DE1` |

## 下一任务与安全边界

`LOCAL-7-A/B` 留在 VERIFY：核心源码与离线验证完成，扩展环境和真实 Host 验收尚未闭合。本线程没有运行中后台任务。

下一步 `LOCAL-7-C`：在 test-tool owner 范围审查上述四个 runner 的依赖复制，使用显式本机路径和固定版本引用补齐闭包，再重跑；不修改官方一键构建/覆盖流程，不将 TaleWorlds/MCM 塞入客户端 Stage。随后审查请求身份/经济 receipt 与部分提交恢复；真实 Host 测试需独立测试存档、重置点和明确部署授权。

没有全局安装 Skill、默认切换三渠道、推送 main/重构分支、覆盖游戏、读取旧存档内容或改动 ONNX/玩家数据。仅改 AF 基础提交边界；未改 GCCZ 核心/桥接，因此不将 AF 主体复制到 GCCZ 或旧 new- 目录。
