# LOCAL-7-M2：Duel exact detached dispatch provenance 接续

日期：2026-09-02。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-7-M2` 的代码与离线/compiled验证完成，状态为 **VERIFY**；Duel领域的
  offline/compiled evidence 可记为 `LOCAL-PASS`，阶段7仍不能标为DONE。
- 基线 `3522dc3e`，意图checkpoint `17f617a5`，实现提交 `b93f93df`。
- Native和SceneShout的detached Duel request现在能在任何Economy/legacy gameplay副作用前，
  把canonical request/trace/channel/session/subject/runtime/save/action fingerprint绑定到唯一DuelId，
  并沿真实排队、延迟host、actual start和结果owner贯穿同一identity。
- Courier没有真实`PrepareDuel` owner，exact路径明确拒绝，未借本切片启用远程决斗。
- 四个可读状态为`Rejected`、`Queued`、`Started`、`UnknownAfterStart`。它们终结本次
  `InteractionCommitResult`；Queued/Started/Unknown均non-retryable。底层owner仍可沿同一DuelId
  推进实际session/outcome，但绝不回填或升级已经返回的commit结果。
- 没有推送、部署、启动或覆盖游戏，没有读写真实存档，没有切默认入口、删除facade、修改
  NEW-10/GCCZ/ONNX或改写共享历史。

## 决定性旧缺口

M1已经能记录实际Duel session，但detached committer生成的canonical request ID没有进入公开
`IActionPlanExecutor.ValidateAndExecute(ActionPlan, GameInteractionSnapshot)`。因此旧路径只能建立
`Domain / legacy-unbound` actual-session receipt；它不能证明某个ActionPlan request导致了该Duel。

红基线：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\duel-dispatch-m2-red-20260902-001250`

红测有6个CS0246，以缺失`IDetachedDuelDispatchOwner`和`DetachedDuelDispatchContext`失败，精确证明
M1源码不存在M2要求的typed seam；它不是live行为红测。没有通过放宽断言或把legacy callback
伪装为成功来转绿。

## 本轮实现

### Request-bound seam与canonical identity

- 保留公开`IActionPlanExecutor`、公开executor构造器、公开Duel API和公开
  `InteractionCommitResult`四参构造器；只新增internal `IRequestBoundActionPlanExecutor`。
- committer在stale gate和request reservation后提供canonical request ID与ActionPlan fingerprint；
  executor独立重算并校验；无效canonical request/fingerprint或不合法的identity字段在Queue前
  fail-closed。Native/Scene当前live token/candidate的再次校验位于Queue后、Economy/callback前，
  若已stale则取消刚建立的Queue，不执行Economy或gameplay。
- `DetachedDuelDispatchContext`只携带有界identity/digest，不携带TaleWorlds对象、callback、原始回复、
  持久化wire或gameplay replay authority。
- exact DuelId由canonical request稳定派生；显式request readback同时校验generation与完整identity。

### 四态语义

| 状态 | 含义 | fallback / replay |
| --- | --- | --- |
| `Rejected` | 在可确认的副作用前拒绝或取消未开始Queue | 本次commit不重派、无fallback、不生成Duel fact |
| `Queued` | owner已接受同一request，但actual Duel尚未Start | terminal、non-retryable；不能宣称玩法成功 |
| `Started` | actual-session owner已记录StartIdentity | terminal、non-retryable；最终结果仍需outcome receipt |
| `UnknownAfterStart` | 保守umbrella：host side-effect/opening/companion effect或dispatch不确定，可能没有StartIdentity | terminal、non-retryable；不证明actual session已start，禁止fallback/replay |

表中的terminal只指本次Interaction commit已经终结；底层Duel owner可继续沿同一DuelId记录后续
session/outcome，但不会改写该commit。side-effect boundary只作为内部保守门禁，不伪造成新的
receipt状态。`Duel+Mood`保留既有合法组合；
若Mood可能已经发生则返回Unknown，绝不错误报告`NoConfirmedEffect`。第二个独立gameplay action、多个
Duel action或Courier Duel在副作用前拒绝。

### Owner、host与顺序

- Queue严格先于Economy和gameplay；Economy拒绝或异常会取消未开始Queue并释放容量。
- Native与Scene建立各自精确factory和provenance gate；Native捕获
  `native_conversation_token`，Scene校验session/candidate。Courier保留防御性拒绝。
- meeting pending、conversation-exit queue、arena/local Mission和wilderness runtime都转移同一
  immutable context；Hero与non-Hero subject identity独立校验，不从当前target重新猜测。
- holder先发布，随后才`HostAccepted`；所有delayed consumer必须满足`Queued && HostAccepted`。
- encounter/mission结束、Mission opening、arena setup和wilderness participant timeout等边界失败，
  在可能跨过副作用后统一转Unknown，而不是错误写Cancelled。
- load清空pending trigger、death/UI/menu/queue字段和共享runtime；active outcome转Unknown，绝不在
  load/tick补开Mission、补转stake、补death或补Memory。

### 结果、artifact与容量

- meeting、arena/local、wilderness三条结算路径都必须先成功`TryRecordDuelOutcome`，才能继续
  result-linked Memory、renown、stake/debt、death和UI；terminal/abort不继续gameplay。
- exact stake/debt/after-lines按DuelId绑定和清理；legacy pending-meeting锁只在`context == null`时
  保留，避免改变M1/default玩法。
- outcome owner为64 active / 512 retained；host另保留process-lifetime 4096个exact-ID seen
  tombstone。seen表满后新的exact request fail-closed；owner rollover丢失完整readback后，旧exact
  request返回`duel.dispatch_retention_expired`，不会复活或重派。
- commit throw、request conflict和duplicate仍保留原typed Duel receipt；不降级成无provenance的
  generic failure。

## ABI、存档与默认路径

- public Duel方法、`IActionPlanExecutor`和旧构造器签名保持不变。
- `_duelCooldowns : Dictionary<string,float>`、95个literal SyncData key、typed binding、
  SaveableTypeDefiner base `711070` / class `1`、MCM/旧JSON和Fourberie optional seam保持不变。
- public ABI保持；M2只增加internal类型、方法和factory surface。
- exact dispatch/outcome receipt为bounded process-local metadata，不入档、不可恢复、不可执行。
- legacy-unbound路径仍保留并明确隔离；M2没有把它升级或回填为exact ActionPlan证据。
- 默认Native/SceneShout/Courier入口没有切换，阶段8删除候选没有执行。

## 验证

最终归档目录（含5份决定性日志）：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\duel-dispatch-m2-final-20260902-021935`

关键结果：

- `DuelDispatchContractTests`：16/16 PASS。
- `DuelOutcomeContractTests`：18/18 PASS。
- Economy-aware executor、Economy Reward/Debt port：本轮运行PASS，日志未复制到final目录。
- Interaction pipeline 40、Detached Host boundary 69、request receipts 39：本轮运行PASS，日志未复制到final目录。
- Production Detached Host、Economy-aware commit、Economy owner、Courier Host：本轮运行PASS，日志未复制到final目录。
- Persistence chunk、Migration 10 + corrupt retained 2、Identity 99 SyncData / 35 behavior：本轮运行PASS，日志未复制到final目录。
- Persistence/Profile：95 literal、121 typed、42 symbolic、40 flattened、3 profiles：PASS。
- Production Duel replay：Debug 35/35、Release 35/35；1.3/1.4 surface parity PASS。
- 文档/阶段8门禁：PhaseEightReadiness 62/62、Bridge 10 cases / 6 invariants、Composition
  18 cases / 24 invariants、ModuleCatalog 8 modules / 3 profiles / 16 invalid cases / 8 health states：PASS。
- readiness catalog JSON parse、Markdown fence、path/link、conflict marker、新增行cleanup与
  `git diff --check`：PASS。
- independent final review：P0=0、P1=0；conflict/TODO/HACK和本切片dead/orphan检查无新增问题。

代表性focused命令：

```powershell
& 'G:\AFMOD\.dotnet-sdk\dotnet.exe' run --project .\tools\DuelDispatchContractTests\DuelDispatchContractTests.csproj -c Release
& 'G:\AFMOD\.dotnet-sdk\dotnet.exe' run --project .\tools\DuelOutcomeContractTests\DuelOutcomeContractTests.csproj -c Release
& '.\一键编译覆盖推送\build_single_module.ps1' -Configuration Debug -Stage
& '.\一键编译覆盖推送\build_single_module.ps1' -Configuration Release -Stage
```

本工作区固定引用：1.3为`G:\AFMOD\AF-REFACTOR\_deps_auto`（`v1.3.15.110062`），1.4为
`G:\AFMOD\AF-REFACTOR\.tmp\build_check\1.4`（`v1.4.6.115628`）。Debug/Release的1.3、1.4、
Bootstrap六项均0 warning / 0 error；`-Stage`只写项目目录，没有覆盖游戏。
Stage先独立fresh构建；最终Production Duel replay通过PowerShell的`-SkipStageBuild`复用刚生成产物，
日志记录`STAGE_BUILD skipped=true`且stage integrity/freshness guard仍PASS。

| 配置 | 产物 | SHA-256 | MVID |
| --- | --- | --- | --- |
| Debug | Bootstrap | `DC5B4B19BADE4D9EFCD6E1F4ACA0BAAE51B6D51408812FD782B635755A5B5088` | `bcd6ef2a-b103-4f89-85e5-0d7f43e459de` |
| Debug | 1.3 | `B474538AF354BACEBC52D0DD7F030641FC664AA65D839212D9AFC553C08B1540` | `b673779f-8bdb-473a-b05a-edacf90ddfe1` |
| Debug | 1.4 | `D806B988B51BF0532617F381D1287A3FD95C3989914184B0F6C5177D1A87B8FF` | `337e6131-fc69-4f31-a86b-ced3e1a65acd` |
| Release | Bootstrap | `AD57237C4C208AF7A619E62381A1D7E6B6ABE0B3BE1E7BD7B704FB498E07295B` | `981288a0-90ee-4c9b-a74f-e46f89031f21` |
| Release | 1.3 | `615AD5D4FE0E366F26DB86F1585FBF6123C463EDE1F259D9EE76B814055974D4` | `5ebdc992-ee55-4434-af0e-bbec351dba7a` |
| Release | 1.4 | `80ABC8F67E614908942810D39D01C85728D2D7AA864FAF471904902FFB28CF27` | `aa7ff917-b1ec-4047-9823-2a8bc0653535` |

无效尝试已登记但不计PASS：

1. 首次Stage未显式提供本机Bannerlord root/reference边界。
2. 改用显式路径后，PATH中的dotnet仍无SDK；后续使用固定8.0.422 SDK。
3. Production Duel runner不支持CLI `--skip-stage-build`；随后使用其PowerShell wrapper支持的
   `-SkipStageBuild`，且只复用本轮刚独立构建、通过freshness guard的Stage。

## 仍未验证 / 风险

- 真实Bannerlord Campaign/Mission的accept/reject/queued/start/cancel/death/exit：NOT-RUN。
- 真实stake/debt/renown、Memory/AFEF、Economy/Mood companion side effects：NOT-RUN。
- 真实旧存档加载、保存后重载、process crash/restart：NOT-RUN。
- 1.3/1.4 live Host、Fourberie安装态、性能与真实UI/菜单/场景时序：NOT-RUN。
- exact receipt只证明request与DuelId的进程内关联，不证明最终玩法结果、落盘或可重放性。
- 默认入口、facade删除、最终打包、部署、GitHub push、QQ发送和发布均未执行。

因此：阶段7保持 **VERIFY**；阶段8可以继续非破坏性准备，但执行、default cutover和发布继续
**BLOCKED**。

## 回滚

- 若只撤实现并保留M2意图：`git revert b93f93df`，同时把台账状态退回ACTIVE。
- 若决定完全放弃M2，再单独`git revert 17f617a5`。M2基线为`3522dc3e`。
- 本HANDOFF和台账提交应使用独立的`git revert <docs commit>`；禁止hard reset、rebase或force push。
- 源码revert不会清理ignored Stage/验证目录；回滚后现有M2 DLL/hash全部视为stale，必须隔离项目
  本地Stage并从回滚源码重新构建后才能使用。
- 本切片没有游戏、NEW-10、GCCZ或远端副作用，无需恢复外部目录。

## 下一精确任务

自动化下一可自主切片为`LOCAL-7-C2 / ShoutNetwork SSE replay dependency closure`：只修复
`tools/ShoutNetworkSseReplayTests/ShoutNetworkSseReplayTests.csproj`仍存在的F盘硬编码和
`Modules\**`递归DLL复制，复用`tools/ReplayDependencies/BannerlordReplayDependencies.targets`，
先红契约、再最小工具实现和Debug/Release回放；不得修改生产C#、业务断言、Stage部署流程或游戏。

Duel领域的下一验收工作单独记为`LOCAL-7-M3`准备项：由测试人员在隔离存档上采集Native/Scene
exact accept/reject/queue/start/cancel/death/exit、stake/Memory/AFEF、Fourberie和旧档往返证据。
没有显式测试授权与真实证据时，自动化不得启动游戏或把M3标为PASS。

## 新线程启动语

> 请读取 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-02-duel-exact-dispatch-provenance.md`，
> 在分支 `codex/af-main-refactor-continuation-20260831` 上继续。先fetch并核对Git、工作树和最新
> 台账，不pull/rebase/reset。确认HEAD至少包含`b93f93df`，按`LOCAL-7-C2`只修复ShoutNetwork SSE
> replay依赖边界；先用红契约锁定F盘硬编码和`Modules\**`递归复制，再复用显式ReplayDependencies
> 做最小工具改动并跑Debug/Release回放。保持阶段7为VERIFY、阶段8执行为BLOCKED；不push、不部署、
> 不覆盖游戏、不读写真实存档、不切default、不删facade。Duel LIVE/SAVE仍由测试人员单独验收。
