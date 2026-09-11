# AF 主体框架暂停／转手 HANDOFF（2026-09-12）

## 1. 停止边界

用户已明确要求停止继续重构并准备转手。**不得从本文自动推导恢复授权。** 本文开始前已停在安全边界：没有正在编辑的生产切片、没有 staged/unstaged/untracked 文件、没有后台构建；最后一个实现切片已经独立提交并有证据提交。本文只记录交接，不启动下一切片。

总体状态：`PAUSED / TRANSFER_READY`；阶段八仍为 `NOT_DONE`。`LIVE=NOT_RUN`、真实旧存档 `SAVE=NOT_RUN`、真实 provider `NOT_RUN`。

自动化 `C:\Users\klfwdf\.codex\automations\af\automation.toml` 已于本次只读回读确认 `status = "PAUSED"`；本次没有修改自动化。

## 2. 实际仓库与 Git 状态

| 项目 | 核实值 |
|---|---|
| 实际 worktree | `C:\Users\klfwdf\.codex\worktrees\cbbb\Mount-Blade-Bannerlord-AnimusForge-mod-main` |
| 交接文档开始前 HEAD | `f948f419b124b26fd71da92e674c0580251dfc8e`（detached） |
| 最后生产源码提交 | `9040d184998120d3476426337160403f08fdfee9` |
| 意图/checkpoint | `909550d44229406454e7895ab3618d7a2f87e54f` |
| 来源与预期远端目标 | `origin/codex/af-main-refactor-continuation-20260831` = `e40c92d72524b7ea80a5dc0e36dc996963a62e66` |
| 相对远端 | `3 ahead / 0 behind`（交接文档提交前） |
| 本地同名 branch ref | `codex/af-main-refactor-continuation-20260831` = `e40c92d7`；当前 checkout 不在该 branch 上 |
| push / deploy / game cover | 截至交接写作前均未执行；用户随后只授权本专题 docs-only 提交后的一次精确 fast-forward push，部署/覆盖仍未授权 |

本机实际任务位于上述 C 盘 Codex worktree；根 `AGENTS.md` 和旧记录中的 `G:\AFMOD\AF-REFACTOR` 是历史机器/续作标签，不能用来覆盖当前 checkout。其他 worktree 只读列举过，未写入、未清理；E 盘 main checkout 与 Claude worktree 均不属于本次交接写入范围。

本地提交链：

```text
f948f419 docs: record memory summary thread boundary
9040d184 fix: commit memory summaries on campaign thread
909550d4 docs: checkpoint memory summary commit boundary
e40c92d7 origin/codex/af-main-refactor-continuation-20260831
```

本专题和根 `HANDOFF.md` 将形成一个 docs-only 交接提交；该提交的精确哈希由最终 `git log -1` 和交接回复记录，因为提交不能在自身内容中预知自身哈希。它不得包含生产代码或测试。该提交完成后，用户已授权先 `git fetch origin --prune`，确认目标远端 ref 仍为当前 HEAD 的祖先且为普通 fast-forward，再且仅再执行 `git push origin HEAD:refs/heads/codex/af-main-refactor-continuation-20260831`；不得 force 或改写历史。推送结果由最终交接回复记录。

## 3. 脏文件清单

交接写作前执行 `git status --porcelain=v2 --branch`、`git diff --name-status`、`git diff --cached --name-status`：

- staged：无；
- unstaged：无；
- untracked：无；
- branch：detached at `f948f419`。

交接写作仅允许出现并提交：

- `docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md`（新增）；
- `HANDOFF.md`（根当前入口更新）。

若最终 `git status --short` 不是空，接手者应先只读辨认文件来源并停止，不得把未知改动并入下一切片，也不得 stash/reset/清理。

## 4. 最后已完成切片：压缩记忆 post-await 主线程提交

### 原行为 → 新行为

原行为：Campaign tick 启动 `ProcessMemorySummaryQueueAsync` 后，第一次 provider `await` 之后的 continuation 直接执行三类 `Apply*Success`、对应 `Mark*Failure`、队列清理、玩家提示和 `_memorySummaryProcessing` 释放；这些会改写当前 `MyBehavior` owner，却没有 Bannerlord 主线程接受边界。

已实现新行为：网络/RPM/retry/parse 保持原样；post-await 可变结果通过低频完成队列发布，由 `MyBehavior.OnEngineTick` 在物理主线程接受。接受时复核 `MyBehavior.Instance`、当前 Campaign behavior 和 save generation；后台入队前后双检，避免 load/reset 竞争后把等待者挂在已不再 tick 的旧 owner。EngineTick 每次最多处理 2 个动作。

### 已完成符号与真实调用链（源码 `9040d184`）

| 状态 | 位置与符号 | 真实 caller / owner / 线程 / 生命周期 | 责任与验证 |
|---|---|---|---|
| `wired-boundary` | `MyBehavior.MemorySummaryMainThread.cs:12-42`，`MemorySummaryMainThreadAction` / `_memorySummaryMainThreadActions` | owner 是当前 Campaign 的 `MyBehavior`；非持久瞬态队列 | generation-bound work + CAS 状态；无 SyncData |
| `wired-boundary` | `MyBehavior.MemorySummaryMainThread.cs:44-67`，`RunMemorySummaryMainThreadAsync` | `ProcessMemorySummaryQueueAsync` 的 post-await continuation 发布；主线程 caller 可直达 | 入队前后拒绝旧 `Instance`/generation；旧 owner 不悬挂 |
| `wired-boundary` | `MyBehavior.MemorySummaryMainThread.cs:69-90`，`TryApplyMemorySummaryMainThreadAction` | 只允许 `TWParallel.IsMainThread()`；再查 `Campaign.Current.GetCampaignBehavior<MyBehavior>()` | owner/Campaign/generation fail closed，提交异常隔离为 false |
| `wired-boundary` | `MyBehavior.MemorySummaryMainThread.cs:92-110`，`ProcessMemorySummaryMainThreadActions` | `SubModule.cs:854-857` 的 application tick watcher → `MyBehavior.OnEngineTick` | 每 EngineTick 最多 2 个，claim 后只完成一次 |
| `wired-boundary` | `MyBehavior.MemorySummaryMainThread.cs:112-119`，`ResetMemorySummaryMainThreadActions` | loaded-save transient reset 与当前存档清理 | 退休尚未开始的 work，并完成等待者 |
| `mixed-host` | `MyBehavior.cs:4957-5129`，`ProcessMemorySummaryQueueAsync` | `TryStartMemorySummaryQueue` fire-and-forget 启动；当前 `MyBehavior` 是权威状态 owner | 三批 Apply/Mark、queue cleanup、notice、processing release 经新边界；前置准备仍混合 |
| `mixed-host` | `MyBehavior.cs:20311-20333`，`OnEngineTick` | `SubModule.cs:856` 调用 | 新队列消费者；同方法仍承载其他既有 UI/周报/rebellion 消费者 |
| `lifecycle` | `MyBehavior.cs:2413-2432`，`ResetLocalTransientRuntimeForLoadedSave` | save load/new game reset 路径 | 在其他记忆瞬态重置前退休新队列 |
| `lifecycle` | `MyBehavior.cs:48278-48305`，`ClearAllDataForCurrentSave` | 开发清数据入口 | 退休新队列后清原权威数据；本切片未改变清数据业务 |
| `scheduler` | `MyBehavior.cs:17920-18023`，`OnCampaignTick` / `TryRunCampaignMemoryMaintenance` | `RegisterEvents` 在 `MyBehavior.cs:2331-2342` 注册 `CampaignEvents.TickEvent` | 原有 bounded maintenance 调度；没有把 provider 搬进 tick |

保持不变：三渠道 prompt/role/AFEF/动作、三次重试、RPM burst、60 秒波次、玩家成功/失败文字、默认交互入口、公开 ABI、SyncData、模块身份、政策/宴会/GCCZ 业务。`Api.V1` 仍只读。

详细边界和已提交证据：

- `docs/architecture/af-memory-summary-mainthread-boundary.md`；
- `docs/audits/2026-09-12-memory-summary-mainthread-verification.md` / `.json`；
- `docs/phase8/memory-summary-mainthread-progress-20260912.md`；
- `docs/architecture/af-framework-code-map.json`（28 anchors，source `9040d184`）。

## 5. 尚未开始的下一切片（不是脏代码）

没有“当前未完成实现文件”。下一建议仅存在于 ledger/HANDOFF，尚未建立 intent commit、测试或生产 diff：为三类压缩任务在 Campaign 主线程捕获只读输入及精确 source revision/fingerprint，后台只做 provider/解析，并在本轮接受边界拒绝来源已变化的结果。

仍未覆盖的真实符号（源码 `9040d184`）：

| 状态 | 位置与符号 | 当前仍运行的责任 / 风险 |
|---|---|---|
| `retained-live / not-started` | `MyBehavior.cs:4962-4980`，`ProcessMemorySummaryQueueAsync` 首次 await 前准备 | 调度快照、候选和提示仍直接读取 owner/game；尚无不可变输入包 |
| `retained-live / not-started` | `MyBehavior.cs:5225-5277`，`ExecuteMemorySummaryJobAsync` | 查 Hero/draft、构造 prompt、retry 时再查 pending；无逐任务 source fingerprint |
| `retained-live / not-started` | `MyBehavior.cs:5279-5351`，`ExecuteMajorActionSummaryJobAsync` | 读取 Hero、`_npcMajorActions`、已有 summary/cursor 并构造 prompt；异步重试期间来源可能变化 |
| `retained-live / not-started` | `MyBehavior.cs:5353-5433`，`ExecuteMemoryOverviewJobAsync` | 读取 Hero、blocks、settings、已有 overview 并构造 prompt；异步重试期间来源可能变化 |

不要把本轮“post-await 写入回主线程”写成“后台只剩网络”或“完整 memory 线程安全”。下一切片也不得顺便扩到 Courier、TTS、公共写 API、默认迁移或制作组玩法。

## 6. 已执行验证与结果

专用红绿/突变：

```powershell
python tools/MemorySummaryMainThreadBoundaryTests/run.py
python tools/MemorySummaryMainThreadBoundaryTests/run.py --original
python tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate ignore-generation
python tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate ignore-owner
python tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate unbounded-drain
```

结果：当前 17/17 PASS；精确读取 `e40c92d7:MyBehavior.cs` 的旧实现 exit 1；三个 mutation 都 exit 1，并分别捕获 stale generation、replaced owner、unbounded drain。

相关回归：

```powershell
python tools/NativeHistorySnapshotTests/run.py
python tools/NativeHistorySnapshotTests/run.py --native
python tools/MemoryFailureUiBoundaryTests/run.py
dotnet run --project tools/MemoryCommitRecoveryContractTests/MemoryCommitRecoveryContractTests.csproj -c Release
dotnet run --project tools/WeeklyMemoryMaterialOutcomeContractTests/WeeklyMemoryMaterialOutcomeContractTests.csproj -c Release
python tools/TeamModulePortParityTests/run.py --dotnet "C:\Program Files\dotnet\dotnet.exe"
python tools/ModuleFrameworkApiTests/run.py --dotnet "C:\Program Files\dotnet\dotnet.exe" --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts
python tools/PersistenceIdentityAudit.py --baseline e40c92d7 --json --quiet
python .agents/skills/af-core-framework/scripts/verify_code_map.py
python .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree
```

最终结果：HistorySnapshot 852/852、native 27/27、MemoryFailureUi 85/85、memory recovery PASS、weekly material PASS、team ports 308 + 3 mutation PASS、public API 119 + 256 concurrent reads PASS、四份实现 DLL 532 个 PE 元数据断言 PASS；持久化相对来源为 SyncData 146/146、CampaignBehavior 36/36；code map 28/28 recorded + working-tree PASS。

构建使用未修改的仓库脚本，两次都带相同依赖参数，仅 configuration 不同：

```powershell
$env:APPDATA=(Resolve-Path '.tmp\appdata').Path
powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 `
  -ProjectRoot . `
  -BannerlordRoot 'E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord' `
  -Bannerlord13ReferenceDir 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main\_deps_auto' `
  -Bannerlord14ReferenceDir 'E:\Mount-Blade-Bannerlord-AnimusForge-mod-main\.tmp\build_check\1.4' `
  -WorkshopContentDir 'E:\SteamLibrary\steamapps\workshop\content\261550' `
  -RuntimeDependencyDir 'E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\bin\Win64_Shipping_Client' `
  -Configuration Debug -Stage
# 上述命令把 Debug 改为 Release 后再次执行。
```

最终 Debug/Release × Bannerlord 1.3/1.4/Bootstrap 六项均 0 error；两个项目内 unified module Stage 均 PASS，脚本明确报告未修改游戏目录。每个构建只有 2 个 `NU1900`，原因是沙箱无法读取 nuget.org 漏洞元数据。

重要的中间诊断，不要误判为最终源码失败：

- 直接 `dotnet build AnimusForge.csproj` 在本机缺 net472 Developer Pack/完整外部引用时失败；改用仓库既有双版本 Stage 后通过。
- 首次 Stage 参数曾指向错误 runtime dependency / 受 AppData NuGet 配置权限阻塞；修正为上面的精确路径和仓库内 APPDATA 后通过。
- `ModuleFrameworkApiTests` 首次同样被用户 AppData NuGet.Config 权限挡住；使用仓库内 APPDATA 后通过。
- `PersistenceIdentityAudit.py` 默认历史基线 `d4cb1467` 正确报告既有战争统计新增的 47 个 binding 和 1 个 behavior；按本切片真实来源 `e40c92d7` 比较后 146/146、36/36 PASS。这不是本切片新增存档字段。

## 7. 未验证层级与产物状态

- contract/fixture/replay：已按上节通过；游戏对象和 provider 在相关 runner 中仍为 stub。
- source/code-map：28 anchors 对 `9040d184` 和当前工作树均通过。
- compile/Stage：Debug/Release × 1.3/1.4/Bootstrap 已通过；只生成仓库内 `bin/*/single_module_artifacts` 与 `single_module_stage`。
- actual DLL metadata：四份实现 DLL 532 项通过；没有实际 CLR/Bootstrap host 加载。
- game install/deploy：未执行，游戏目录未修改。
- LIVE Campaign/Mission：`NOT_RUN`。
- real provider/API：`NOT_RUN`。
- real/old save、读档晚返回：`NOT_RUN`。
- default cutover / public write API / team business rewrite：未执行。

最终六份 DLL SHA-256 在 `docs/audits/2026-09-12-memory-summary-mainthread-verification.json`，不要用旧构建日志里的早期哈希替代。

## 8. 安全恢复步骤

只有收到新的明确用户授权后才恢复：

1. 先读根 `AGENTS.md`、`.agents/skills/af-core-framework/SKILL.md`、本文、根 `HANDOFF.md` 当前段、`docs/phase8/project-closeout-execution-20260908.md` 和 code map。
2. 只读核对 `git rev-parse --show-toplevel`、`git rev-parse HEAD`、`git status --porcelain=v2 --branch`、远端目标和 `HEAD...origin/...` 祖先关系。预计交接提交位于 `f948f419` 之上且仅改两份文档；若不同或出现未知脏文件，停止并报告。
3. 不因 checkout detached 自行 `switch`、merge、rebase、reset、cherry-pick 或 stash；分支/整合由用户明确决定。
4. 运行 code-map recorded/working-tree 校验。它只证明坐标，不是玩法验收。
5. 下一位接手者的第一项工作应是**只读提取三个 Execute job 的真实 live 读取和现有 source identity**，先建立对 `9040d184` 会失败的精确旧源码测试/设计边界；用户确认继续后，再创建新的 intent/checkpoint，并且每轮只做一个可回滚切片。
6. 实现候选仍需 Debug/Release 双版本 Stage、相关 memory/history/weekly 回归、存档身份和实际 DLL 元数据；LIVE/SAVE 必须独立记录。

## 9. 禁止事项与回滚点

禁止：继续重构、启动下一切片、部署/覆盖游戏、修改一键脚本、切默认入口、开放公共写 API、安装全局 skill、触碰其他 worktree、清理/stash/reset/回滚未知改动或重写历史。Push 唯一例外是本次用户明确授权的、完成 docs-only 提交并通过 fetch/祖先核验后的精确命令 `git push origin HEAD:refs/heads/codex/af-main-refactor-continuation-20260831`；不允许 force、其他 refspec 或后续自动 push。

可回滚提交（仍需用户明确指示，使用定向 inverse/revert，绝不 hard reset）：

- `9040d184`：最后生产/测试切片；
- `909550d4`：该切片意图/checkpoint 文档；
- `f948f419`：该切片 code map、ledger、审计和根 HANDOFF 证据；
- 本专题 docs-only 交接提交：不改变产品行为，精确哈希见最终交接回复/`git log -1`。

没有已知需要立即回滚的候选；当前要求是暂停和转手，不是撤销已验证实现。
