# AF 主体重构：阶段 7 / 阶段 8 制作组简报

日期：2026-09-02；2026-09-03追加 Bridge 接线收尾与 OFFLINE-GAP 更正

> **当前状态（2026-09-03）：**本简报中的 2026-09-02 编译、部署、Identity 和推送描述均为
> 历史快照。当前工作区为
> `E:\AnimusForge-klfwdf\_worktrees\refactor-prepare-af-restructure-schannel`，本地 HEAD
> `ab6ce72` 及其后的本地 handoff 文档提交，尚未 push。当前结论以
> `docs/handoffs/2026-09-03-bridge-binding-closeout.md` 的 `OFFLINE-GAP-20260903` 追加为准：
> Bridge `20/20`、Phase 8 `68/68`、Bridge isolation `9 scenarios`、Persistence contract `5/5`
> 和 ModelCatalog replay 均通过；真实 PersistenceIdentityAudit 因缺少 `89` 个基线源码 blob
> 按设计 fail-closed，readiness 仍 `BLOCKED`。未启动游戏、未读写真实存档、未部署、未切默认。

## GitHub交接状态（历史快照与当前更正）

- 远端目标：`origin/refactor/prepare-af-restructure`；2026-09-02 原计划随收尾提交普通 push，
  但当前 OFFLINE-GAP 文档和测试提交尚未 push。
- 自动化`af-7-8`与旧`af`均已暂停；没有AF后台定时任务继续改仓库。
- 制作组接手前先`git fetch origin --prune`，确认远端分支HEAD，再在不覆盖本地改动的前提下接续。
- 总交接：`docs/handoffs/2026-09-02-github-publish-and-team-handoff.md`。
- 发布到GitHub只表示源码与文档完成交接，不代表真实游戏验收、默认切换或阶段7/8完成。
- 本地接续已按用户授权完成一次 Debug 双版本编译与统一模块测试部署；来源是
  `refactor/prepare-af-restructure`，不是 `main`。本轮未启动游戏，部署不代表 Release 或 LIVE/SAVE 通过。
- 随后日志边界修复重建了当前 Debug Stage；已安装目录的实现 DLL 仍是部署时版本，实机前必须先
  在明确授权下重新部署当前 Stage。最新只读审计为 `installedMatchesStage=false`、`gameRunning=false`；
  该字段只比较 Bootstrap，不能证明三份实现 DLL 一致。

## 2026-09-03 Bridge 接线收尾（当前状态）

- 当前离线清单为 `16 bindings / 10 wired / 6 declared-only / configEnabled=10`；2026-09-02
  的 `16 bindings / 3 wired / 13 declared-only` 是历史快照。
- 已接线 10 组：`conversation-gateway`、`conversation-action`、`action-memory`、
  `action-economy`、`policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、
  `memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration`。
- 仍 declared-only 6 组：`bootstrap-host`、`host-runtime`、`runtime-game-adapter`、
  `persistence-domain-owners`、`scene-duel`、`tools-content-release`。只登记合同/owner/required
  cases，没有运行时 caller，也没有 LIVE/SAVE 证据。
- 配置读取严格限定在 `AnimusForge` 模块边界；缺失配置使用内建默认值，损坏配置和非规范大小写
  ID fail-closed。Action/Memory 禁用保持既有拒绝/`NoOp` 语义，不回退重放 legacy 副作用。
- Bridge validator、纯契约测试、各领域 fixture/replay、Production hosts、Duel fresh replay 与
  双 API Debug/Release/Bootstrap Stage 全部通过；本轮不启动游戏、不读写真实存档、不部署、不切默认。

## 六、最准确的结论

> **阶段7作为“模块接入与离线验证阶段”，大部分工作已经完成：统一Gateway、三渠道管线、
> ActionPlan、Memory/AFEF边界、Economy owner、请求回执以及Duel exact provenance都已接入并通过
> 相应离线/compiled验证。阶段7作为完整交付仍未完成，因为真实Campaign/Mission、旧存档、
> live Economy/AFEF、Duel真实副作用和默认入口尚未验收。**

因此：

> **可以继续阶段8的Bridge矩阵、清理候选、回滚与验收包准备；不能把阶段7标为DONE，也不能
> 删除旧facade、切默认路径、覆盖游戏或直接收尾阶段8。**

## 当前推荐顺序

### 阶段7

- 模块接入、契约、离线/等价Host与compiled回放：**大部分完成，整体仍为VERIFY**。
- Duel M1/M2：actual-session结果owner和exact detached request-to-DuelId均已离线`LOCAL-PASS`。
- 五个managed production replay（含Shout SSE）已统一显式依赖owner，不再由consumer硬编码盘符或扫描全部Modules。
- 真实Host、旧档、live金币/物品/商人/债务、Memory/AFEF、Duel死亡/赌注/Fourberie：**待验收**。
- Native / SceneShout / Courier默认入口：**尚未统一切换**。

### 阶段8准备（可以并行）

- 20领域owner、16组Bridge和完整真实入口清单。
- 每项旧facade/bridge/flag/shim的KEEP/HOLD/REVIEW_REMOVAL证据。
- 逐项回滚点、存档副作用说明、LIVE/SAVE验收记录和最终打包清单。
- `LOCAL-7-C3` 与 Persistence/Profile 离线收尾的历史证据仍为 C3 `4/4`、Persistence `95/121/44`；
  Identity `99/35` 是较早完整基线上的历史 PASS。当前 partial clone 缺少 89 个基线源码 blob，
  `PersistenceIdentityAudit.py` 按设计 fail-closed；详情和当前更正见
  `docs/handoffs/2026-09-02-offline-closeout-c3-persistence.md` 与
  `docs/handoffs/2026-09-03-bridge-binding-closeout.md`。

## OFFLINE-GAP-20260903 当前离线追加

- Bridge caller validator 增加真实方法体、gate 顺序、缓存初始化检查及负例；当前 Python 测试
  `20/20 PASS`，清单保持 `16 bindings / 10 wired / 6 declared-only`。
- 纯 `net8.0` Bridge runtime isolation runner 为 `9 scenarios PASS`，不加载 Bannerlord DLL；
  Phase 8 entry inventory 只补真实 `entryPaths`，全套测试 `68/68 PASS`，20 个领域仍为
  `ROLE_PLACEHOLDER` / `REPRESENTATIVE`。
- PersistenceIdentityAudit 已改为单快照、batch baseline 读取、stderr 进度和 `--quiet`；契约
  `5/5 PASS`。真实审计因 partial clone 缺少 `89` 个基线源码 blob 返回 `FAIL`/fail-closed，
  不能把历史 `99/35 PASS` 当作当前结论。
- ModelCatalog 已采用稳定 `model_catalog.*` 错误码、受限参数和中英文 formatter；replay PASS。
- `all-missing` readiness 仍 `BLOCKED` / exit `2`；本轮不启动游戏、不读写真实存档、不部署、不
  切默认入口、不删除 facade，两个 `.branch-archive*.zip` 保持不处理。
- Bridge 绑定清单 `docs/phase8/bridge-binding-manifest.json` 已闭合 16 组；**2026-09-02 历史快照**
  为 `16 bindings / 3 wired / 13 declared-only`。当前结果为 `16 bindings / 10 wired / 6 declared-only`；
  `wired` 仅表示 source-bound Gate 已在既有入口调用，`declared-only` 不代表运行时已接入。
- Release 离线 Stage 与 ZIP 已重建并校验：双 API/Bootstrap 各 `0 warning / 0 error`，Production
  Duel Release `35/35 PASS`；ZIP 位于 `.tmp/packages/release-final-20260902/`，不等同于实际安装或
  发布签收。

### 阶段8执行（当前禁止）

- 删除仍有调用、反射、存档或兼容责任的旧入口。
- 切默认三渠道、再次覆盖游戏、Release 打包发布或最终安装。
- 在LIVE/SAVE证据不完整时宣布阶段7或阶段8完成。

## 本轮新增闭环：Duel M2

- canonical request/trace/channel/session/subject/runtime/save/action fingerprint在副作用前绑定唯一DuelId。
- Native/Scene精确区分Rejected、Queued、Started、UnknownAfterStart；Courier明确拒绝。
- Queue先于Economy/gameplay；duplicate/conflict/capacity/load全部fail-closed，不fallback、不重放。
- 三条结算路径先记录同一result receipt，再进入Memory、renown、stake/death等副作用。
- contract 16/16 + outcome 18/18，Production Duel Debug/Release各35/35，1.3/1.4/Bootstrap
  Debug+Release均0 warning / 0 error；这些仍不是实机验收。

## 制作组成员接下来交付

1. 每个领域登记真实入口、owner、Prompt/ActionPlan适用性、save key/type和失败降级。
2. 在隔离存档上提供Campaign/Mission步骤、前后状态、BuildInfo、DLL hash、PASS日志和回滚点。
3. Duel重点覆盖accept/reject/queue/start/cancel/death/exit、stake/debt、Memory/AFEF和Fourberie。
4. 验证代表旧档加载、保存后重载、缺失/损坏数据隔离，不用fixture或DLL加载冒充SAVE PASS。
5. 每个清理候选先证明替代路径和rollback drill；没有真实证据继续写`NOT-RUN / BLOCKED`。
6. 本机 5 个 net6 smoke 与 ActionPostprocess/Preprocess net10 smoke 均通过；PlayerExportsEditor
   smoke 因现有内容校验错误退出码 `1`（RagShortText 长度及 JSON 字段类型），不得吞错或批量改生产数据。

## 一句话结论

> **现在可以并行完善阶段8准备，但不能跳过阶段7真实验收直接做破坏性清理、默认切换或发布。**

总交接：`docs/handoffs/2026-09-02-github-publish-and-team-handoff.md`。

最新技术证据：`docs/handoffs/2026-09-02-shout-sse-replay-dependency-closure.md`。
