# AF 主体重构：GitHub 发布与制作组总交接

日期：2026-09-02；2026-09-03追加 Bridge 接线收尾与 OFFLINE-GAP 更正

工作区：`E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel`

本地分支：`refactor/prepare-af-restructure`

远端交接分支：`origin/refactor/prepare-af-restructure`

> 本文 2026-09-02 的同步段落和 `3 wired / 13 declared-only` 数字是历史快照；当前状态以
> 下方 2026-09-03 追加章节及 `docs/handoffs/2026-09-03-bridge-binding-closeout.md` 的
> OFFLINE-GAP 追加为准。

## 2026-09-03 Bridge 接线收尾追加（以本节为准）

- 接续起点为本地 `HEAD 231f6cb6`、远端 `e5af64fb`，ahead 2；本轮只在项目工作区完成
  Bridge 接线、配置安全修复、文档同步和离线审查。当前工作树尚未 push，最新本地提交见
  `e7db736`。
- 当前清单结果：`16 bindings / 10 wired / 6 declared-only / configEnabled=10`。
- 10 个 source-bound wired：`conversation-gateway`、`conversation-action`、`action-memory`、
  `action-economy`、`policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、
  `memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration`。
- 6 个仍为 declared-only：`bootstrap-host`、`host-runtime`、`runtime-game-adapter`、
  `persistence-domain-owners`、`scene-duel`、`tools-content-release`。它们只有合同/owner/required
  cases 登记，没有运行时 caller，不能当作功能接入或 LIVE/SAVE 通过。
- `FeatureBridgeRuntime` 现在只从带 `SubModule.xml` 和 `ModuleData` 的 `AnimusForge` 模块边界找配置；
  缺失配置使用审阅过的内建默认值，损坏/未知字段/非法版本/非规范大小写 ID fail-closed。Action、
  Memory Bridge 禁用仍保持现有拒绝/`NoOp` 语义，不回放 legacy 副作用。
- 验证：Bridge validator `PASS`；Bridge Python 单测历史切片为 `15/15`，OFFLINE-GAP 追加后
  当前为 `20/20`；PhaseEightReadiness 历史切片为 `62/62`，追加 inventory 后全套为 `68/68`；
  BridgeFixture `10 cases / 6 invariants`；Composition `18/24`；ModuleCatalog `8/3/16/8`；
  Foundation `6/8/16`；GameAdapter `14`；Persistence/Profile `95/121/44`；LiveHostReadiness `PASS`；
  Interaction、Duel、Economy、Configured Gateway/Validation、Knowledge/RAG、Production hosts 与
  Production Duel fresh replay（`35/35`，1.3/1.4 parity）均通过；Debug/Release 双 API/Bootstrap
  Stage 均 `0 warning / 0 error`。
- 本轮没有启动 Bannerlord、进入 Campaign/Mission、读取或写入真实存档、执行 LIVE/SAVE、部署、
  切换默认入口、删除 facade 或修改终端 UI；阶段 7 总体仍 `VERIFY`，阶段 8 执行仍 `BLOCKED`。
- 推送仍未执行；若未来获得明确授权，命令才限于：

  ```powershell
  git push origin HEAD:refs/heads/refactor/prepare-af-restructure
  ```

## 六、最准确的结论

> **阶段7作为模块接入和离线/compiled验证阶段，大部分工作已经完成；阶段7作为完整交付阶段仍未
> 完成，因为真实Campaign/Mission、旧存档、live Economy/AFEF、Duel真实副作用和默认入口尚未验收。**

因此：

> **当前代码、HANDOFF 与制作组简报尚未 push；未来如获得明确授权才可按下方历史同步规则转交。
> 阶段8仍只能做非破坏性准备，不能把任何 push 解释为阶段7 DONE、阶段8执行许可、默认切换或
> 可发布游戏版本。**

## 本次GitHub同步（2026-09-02历史快照）

- 用户已明确授权关闭自动化并普通push。
- 自动化`af-7-8`与旧`af`均为`PAUSED`，不会继续定时修改仓库。
- 收尾文档编写前：本地HEAD
  `19e5d6b10cd9ef49909dcd03759081633bc111c9`，远端HEAD
  `9566bf3bec0642ccef6764db6b6630edc195300a`，ahead 19 / behind 0，工作树clean。
- 本HANDOFF、制作组简报、公共台账和总纲作为最后一组文档提交后，通过普通fast-forward push同步到
  `origin/refactor/prepare-af-restructure`。
- 完成判据：push后本地HEAD与远端分支HEAD相同，ahead/behind为0/0。
- 禁止force push、rebase共享历史、部署、覆盖游戏、切default或删除facade。

授权的唯一远端写命令：

```powershell
git push origin HEAD:refs/heads/refactor/prepare-af-restructure
```

本授权不包括`main`、tag、GitHub Release、游戏部署、QQ发送或任何其他remote ref。

## 本次推送包含的主要提交组

### 阶段8非破坏性完整准备

```text
9a088f2f  阶段8完整readiness意图checkpoint
b1c5a81a  20领域门禁
1e341c43  Bridge SAVE覆盖
f4a02018  未分配owner保持BLOCKED
00e9e302  阶段8完整准备HANDOFF
6b1d16f1  catalog review闭环
8bdd9363  cleanup audit与canonical Bridge绑定
28787546  记录复核后的阶段8门禁
9955658b  full-domain BLOCKED快照
```

结果：canonical 20领域、16组Bridge、18项cleanup inventory已进入准备态门禁；owner仍为
`ROLE_PLACEHOLDER`、入口仍为`REPRESENTATIVE`，所以真实readiness继续BLOCKED。没有删除任何候选。

### Duel actual-session与exact dispatch

```text
fc3cd722  M1意图checkpoint
16f3cbef  actual-session typed owner/outcome/readback
3522dc3e  M1 HANDOFF
17f617a5  M2意图checkpoint
b93f93df  exact detached dispatch provenance
033f28aa  M2 HANDOFF
8bf0c1e4  阶段8BLOCKED快照刷新
```

结果：Native/Scene exact request在副作用前绑定唯一DuelId；Courier exact拒绝；legacy-unbound保持隔离。
这只证明离线/compiled边界，不证明真实Duel、死亡、stake、Memory/AFEF或Fourberie。

### Shout SSE replay依赖闭环

```text
28ad96f2  C2意图checkpoint
ae49e3c8  第五consumer显式依赖边界与source contract
19e5d6b1  C2 HANDOFF
```

结果：Shout SSE不再硬编码F盘或递归复制全部Modules；五consumer契约5/5、helper 9/9、Debug/Release
runner均PASS，两份78项dependency manifest一致。Release只代表Release runner加载同一Debug AF Stage。

## 关键验证证据

### Duel M2

- Duel Dispatch：16/16 PASS。
- Duel Outcome：18/18 PASS。
- Production Duel：Debug 35/35、Release 35/35，1.3/1.4 parity PASS。
- Debug/Release的1.3、1.4、Bootstrap六项Stage：0 warning / 0 error。
- 证据：`.tmp/validation/duel-dispatch-m2-final-20260902-021935`。
- 技术HANDOFF：`docs/handoffs/2026-09-02-duel-exact-dispatch-provenance.md`。

### Shout SSE C2

- Source consumer boundary：5/5 PASS。
- Replay dependency helper：9/9 PASS。
- Shout SSE Debug/Release runner：PASS。
- Dependency manifest：各78项，SHA-256均为
  `67A5DE630580707B0D4BD4AD607CD854363D2A9B9DD3A8C8D884808C24BBD2A7`。
- 证据：`.tmp/validation/shout-sse-dependency-c2-final-20260902-031458`。
- 技术HANDOFF：`docs/handoffs/2026-09-02-shout-sse-replay-dependency-closure.md`。

### 阶段8准备工具

- PhaseEightReadiness：`62/62 PASS` 为 2026-09-02 历史快照；OFFLINE-GAP 追加入口 inventory 后
  当前全套为 `68/68 PASS`。
- Bridge：10 cases / 6 invariants PASS。
- Composition：18 cases / 24 invariants PASS。
- ModuleCatalog：8 modules / 3 profiles / 16 invalid cases / 8 health states PASS。
- all-missing真实项目报告仍为`BLOCKED`，不是失败误报，也不是发布许可。

### LOCAL-7-C3 与 Persistence 离线收尾

- `LiveHostReadinessAudit` 已改为显式必填 `--game-root`，删除 F 盘默认路径并补纯 fixture/CLI
  测试；C3 测试 `4/4 PASS`，Python 编译与 `git diff --check` 通过。
- Persistence/Profile/Config scanner 已排除 `.tmp`、artifacts、缓存和依赖输出，支持跨 partial
  文件解析唯一常量；catalog 同步至 44 个 flattened dictionary key。
- Persistence/Profile/Config：95 literal / 121 typed / 8 types / 3 profiles / 44 flattened，历史证据
  仍 PASS。Persistence Identity 的 `99 SyncData / 35 CampaignBehavior / AnimusForge / Bootstrap-only`
  是较早完整基线快照；当前 partial clone 缺少 `89` 个基线源码 blob，真实审计按设计 fail-closed，
  不能记录为当前 PASS。
- 阶段8只读复核保持 20 domains / 16 bridges / 18 cleanup candidates，owner 与 entry 仍未认领；
  `all-missing` 仍 `BLOCKED` / exit 2。
- 详见 `docs/handoffs/2026-09-02-offline-closeout-c3-persistence.md`。

### Bridge 配置与安全接线

- 新增 `docs/phase8/bridge-binding-manifest.json`，闭合阶段 8 的 16 组 Bridge：逐组记录
  domains、topology、owner、真实 entry paths/symbols、实现状态、fallback、API line 和 required
  cases。
- 新增 `tools/BridgeBindingContractTests/validate_bridge_bindings.py` 及纯源代码契约测试；
  **2026-09-02 历史结果**为 `16 bindings / 3 wired / 13 declared-only PASS`。`declared-only` 只表示
  合同已登记，不表示运行时功能已接入。
- 历史快照中仅三个已有入口接入一次性/事件边界 Gate；失败或禁用时保留原版/各自 owner 的
  fallback，没有新 Tick 扫描、网络、存档或 live 对象跨边界。当前 10 wired 的完整清单见上方
  2026-09-03 追加章节和 `docs/phase8/bridge-binding-manifest.json`。
- 1.3/1.4/Bootstrap Debug Stage 已重新编译，均 `0 warning / 0 error`；尚未启动游戏或读取真实存档。

### OFFLINE-GAP-20260903 离线缺口追加

- Bridge validator 已加入真实 C# 方法体/顺序解析及跨方法、错误 ID、伪造调用、缓存初始化负例；
  当前 Bridge 单测为 `20/20 PASS`。
- 新增纯 `net8.0` Bridge runtime isolation runner，`9 scenarios PASS`；每个场景独立子进程，
  不加载 Bannerlord DLL，并验证缺失/空列表/损坏配置、CWD 陷阱、fallback 和稳定 reason code。
- 新增 `tools/PhaseEightReadiness/entry_inventory.py`，只补 8 个领域的真实候选 `entryPaths`，
  输出稳定排序和 `reviewed-pattern` 来源；20 个领域仍是 `ROLE_PLACEHOLDER` /
  `REPRESENTATIVE`。
- `PersistenceIdentityAudit.py` 改为单快照、单次 baseline tree、`git cat-file --batch`，增加
  stderr 阶段进度和 `--quiet`。契约测试 `5/5 PASS`；当前 partial clone 缺少 89 个基线源码 blob，
  真实审计按设计返回 `FAIL`/fail-closed，不再把旧的 `99/35 PASS` 误写成当前结果。
- `LegacyModelCatalogGateway` 增加 `model_catalog.*` 稳定错误码、受限只读 `ErrorArguments`、
  中英文 formatter；DuelSettings/ModOnboarding 已按错误码映射，ModelCatalog replay PASS。
- `all-missing` readiness 当前仍为 `BLOCKED` / exit `2`，不改变任何发布或默认入口授权。

### 用户授权的 Debug 编译与测试部署（本地接续）

- 本地实际来源：`F:\AnimusForge-main` 的 `refactor/prepare-af-restructure`，HEAD
  `109835cd18fee09ebd591fa254f0af1aa913acb4`；不是 `main`，也未切换默认分支。
- 使用统一 `build_single_module.ps1 -Configuration Debug -Deploy`；1.3 引用为
  `v1.3.15.110062`，1.4 引用为 `v1.4.6.115628`，1.3/1.4/Bootstrap 均 `0 warning / 0 error`。
- 安装目标：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`。
  事务部署退出码 `0`；部署当时的 Bootstrap、1.3、1.4 DLL 与当时项目 Stage 哈希完全一致：
  `BF57E46CF3C095FB3205DBA4A7428339A1C574BC30B3B8DE882822E4ACC2AAE9`、
  `5F66A4932AB1948BBB71D38C80C6AADC63AD3F5F508004B1F2469FB13544E970`、
  `D28931E9129E3E6F441BC5297466BA99FC886BD9DD15A5C3484B7EFCF598D16C`。
- `SubModule.xml` 仅加载 Bootstrap；合并 4,753 个 `PlayerExports` 文件，保留既有 Logs/PlayerExports/ONNX，临时部署与备份目录已清理，未发现旧版模块目录。
- 2026-09-02 的离线日志边界修复曾触发 Debug Stage 重建；当时 Stage 实现哈希为
  `BB157A03F97F606158203E3A68F53AEC7687F6BFD5850728760446285CFC2ABE`（1.3）和
  `F43DFD482596BA58501A48723225CF6999E3C2143B0E7029B4363410ED6A5376`（1.4），而安装目录仍为上面的部署时哈希。2026-09-03 收尾后项目内 Stage 已重新生成，安装目录未改；最新 readiness 为
  `installedMatchesStage=false`，该字段仅比较 Bootstrap，不足以证明三份 DLL 一致，实机测试前必须在明确授权下重新部署当前已核验 Stage。精确新哈希见 `docs/handoffs/2026-09-03-bridge-binding-closeout.md`。
- 这是用户明确授权的 Debug 测试安装，不是 Release、默认切换、最终发布或真实 LIVE/SAVE 验收；本轮没有启动游戏。

### Release 离线构建、Duel 回放与 ZIP

- 使用统一 `build_single_module.ps1 -Configuration Release -Stage` 重建当前源码的 1.3、1.4、Bootstrap；三项均 `0 warning / 0 error`，且未修改游戏目录。
- Production Duel Release replay：`35/35 PASS`，1.3/1.4 parity 通过；该回放只读取生产 Stage 元数据/IL，不启动 Bannerlord、不读取存档。
- ZIP：`F:\AnimusForge-main\.tmp\packages\release-final-20260902\AnimusForge_v1.3.7.2_20260902_100952_233.zip`，4919 entries，SHA-256 `1215A88666E6FCCD949BE413C75719B2C96BCA061546FCAD86DB9AB0F805ACE5`；Bootstrap-only XML、双实现 marker/hash、ONNX/旧模块排除均通过。
- 这是 Release 的离线工件证据，不是实际安装、Campaign/Mission、LIVE/SAVE、默认切换或发布签收。

## 制作组接下来做什么

1. 先获取远端，不覆盖自己已有工作：

   ```powershell
   git fetch origin --prune
   git log -1 --oneline origin/refactor/prepare-af-restructure
   ```

2. 阅读本总交接、制作组简报、Duel M2与Shout C2技术HANDOFF。
3. 实机人员优先在隔离存档采集Duel accept/reject/queue/start/cancel/death/exit、stake/debt、
   Memory/AFEF、Fourberie和旧档往返证据。
4. 其他领域按`docs/phase8/full-domain-acceptance-package.md`补20领域LIVE/SAVE evidence、owner assignment、
   entry coverage与rollback drill。
5. `LOCAL-7-C3` 与 Persistence 离线收尾已完成；自动化仍暂停。实机人员应先在明确授权下把当前
   已核验 Stage 重新部署，再在隔离存档按20领域验收包补LIVE/SAVE与rollback drill，不能把本轮
   离线证据提升为真实验收。
6. 所有真实门禁完成后，另行评审default cutover、旧facade删除、Release/最终包和再次部署；不得提前执行。

## 尚未完成 / 不得误报

- 真实Bannerlord Campaign/Mission：NOT-RUN。
- 真实旧存档加载与保存后重载：NOT-RUN。
- live金币、物品、Merchant、债务、Memory/AFEF/Notoriety：NOT-RUN。
- Duel live死亡、stake/debt、Fourberie、退出/取消时序：NOT-RUN。
- WorldMap、Diplomacy、Siege/GCCZ、周报、主动NPC、Issue等完整领域LIVE/SAVE签收：未完成。
- 默认Native/SceneShout/Courier切换、facade删除、最终 Release 安装和发布：BLOCKED；当前
  已生成 Release 离线 ZIP，但 Debug 测试模块的实现 DLL 相对当前 Stage 已过时，不构成发布许可。
- 本机 5 个 net6 smoke 均通过（3、PASS、269、1168、453）；ActionPostprocessPromptLab 与
  PreprocessTopicPromptLab net10 smoke 均通过。PlayerExportsEditor smoke 因现有内容校验错误退出码
  `1`（RagShortText 长度及 JSON 字段类型），不是运行时缺失；未修改生产 PlayerExports 数据。

## 回滚

- 本次远端同步的共同基线为
  `9566bf3bec0642ccef6764db6b6630edc195300a`；完整提交范围以
  `git log origin/refactor/prepare-af-restructure..HEAD`为准。
- 阶段8准备链按HANDOFF中的逆序普通`git revert`，不得hard reset或force push。
- Duel M2实现：`git revert b93f93df`；M1实现：`git revert 16f3cbef`。
- Shout C2实现：`git revert ae49e3c8`。
- 文档提交独立revert；源码revert不会撤销游戏/存档副作用，也不会清理ignored Stage、runner output或
  validation目录。回滚后相关产物全部视为stale，必须重新构建/验证。
- 本次 Debug 部署已通过事务备份/替换完成，部署脚本生成的临时 staging/backup 已清理；当前回滚只允许
  对明确的 `Modules\\AnimusForge` 目标执行 scoped 事务恢复或重新部署已核验 artifact，不得用 Git
  回滚代替游戏目录回滚。源码回滚不会撤销可能产生的存档副作用；本轮未启动游戏，未产生新的存档副作用。

## 新任务启动语

> 请先读取 `E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel\docs\handoffs\2026-09-02-github-publish-and-team-handoff.md`、
> `docs\handoffs\2026-09-02-stage7-stage8-team-brief.md`和公共执行台账；fetch
> `origin/refactor/prepare-af-restructure`但不要覆盖本地未提交内容。阶段7保持VERIFY、阶段8执行保持
> BLOCKED。C3 与 Persistence 离线收尾已完成；Debug 测试模块已按明确授权部署，但尚未启动游戏。
> 优先由实机人员先重新部署当前已核验 Stage 后补LIVE/SAVE；未经新的具体授权，不切default、不删facade、不再部署/覆盖游戏、
> 不force push。
