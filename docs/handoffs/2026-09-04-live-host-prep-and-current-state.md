# AF 当前工作树与实机验收准备交接

日期：2026-09-04

## 权威工作区

- 工作区：`F:\AnimusForge-main`
- 分支：`refactor/prepare-af-restructure`
- 文档初次记录时的 HEAD（历史快照）：`34b3f35811130e26b60a5407451d169de3667dbb`
- 远端：`origin/refactor/prepare-af-restructure`
- 文档初次记录时的本地/远端状态（历史快照）：`ahead 0 / behind 0`
- 文档初次记录时的工作树状态（历史快照）：clean

本文件是 2026-09-04 本机复核结果。此前交接中出现的
`E:\AnimusForge-klfwdf\_worktrees\...` 是另一工作树路径；在本机执行命令时必须使用上述
`F:\AnimusForge-main`。

> **04:20 后续复核覆盖（优先于本文件下方较早的部署快照）：**本轮仅为离线验证重新生成了
> Debug/Release unified Stage，没有再次部署或启动游戏。新的 Debug Stage 哈希为 Bootstrap
> `1D0759FFA937E5BC29ABD8471C5B314369EFB3A820CD7AEBBB5DC00F35602565`、1.3
> `4774B6504788EFA4DBC1D40302E92B80720CE4C8001E81DEB3E4DB295298A9EC`、1.4
> `D861D1171BE3DB5FA9893E1825DCD03C233540A93B42CC5EA6EDBB9CEDBE78CD`；游戏目录仍保留先前部署的
> Bootstrap `8411354A1D8B2CBCEF8F30D7C514C2A1F9219310E6E8A2C560036812DEB2DC1F`、1.3
> `27FBB101D8C5ABA194E4812B7549F2648807C33DAE0DF6235C31F8C60B13EDCC`、1.4
> `5B19B8267E5DFEE626DA7E917CBA85A087FBD06244BA59D300A10B5BA0BA20D8`。因此当前
> `installedMatchesStage=false`；制作组开始实机前必须重新部署与源码匹配的 Stage，并重新记录哈希。

## 本轮已完成

### 离线与编译

- Bridge validator：`16 bindings / 10 wired / 6 declared-only / configEnabled=10`。
- Bridge Python 单测：`20/20 PASS`。
- Bridge runtime isolation：`9 scenarios PASS`。
- Phase Eight Readiness 单测：`68/68 PASS`；`entry_inventory --check` PASS。
- PersistenceIdentityAudit：当前基线 `d4cb1467376c6e923f4295dcefc7878c11dbc7c1`，
  `sync=99/99`、`behavior=35/35`、`module=AnimusForge`，真实审计 `PASS`。
- PersistenceIdentityAudit 契约测试：`5/5 PASS`。
- ModelCatalog gateway replay：PASS。
- Debug 与 Release 的 1.3、1.4、Bootstrap unified Stage：均 `0 warning / 0 error`。

### 较早 Debug 测试部署快照（已被 04:20 后续复核覆盖）

按用户授权，使用官方 `一键编译覆盖推送\build_single_module.ps1 -Configuration Debug -Deploy`
把当前分支构建结果部署到：

`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`

部署脚本报告成功，保留 `CustomPrompts`、`Logs`，合并 `4,753` 个 `PlayerExports` 文件，未删除
旧版模块目录。部署后逐文件 SHA-256 与项目 Stage 相同：

| 文件 | SHA-256 |
|---|---|
| Bootstrap | `8411354A1D8B2CBCEF8F30D7C514C2A1F9219310E6E8A2C560036812DEB2DC1F` |
| 1.3 implementation | `27FBB101D8C5ABA194E4812B7549F2648807C33DAE0DF6235C31F8C60B13EDCC` |
| 1.4 implementation | `5B19B8267E5DFEE626DA7E917CBA85A087FBD06244BA59D300A10B5BA0BA20D8` |

`SubModule.xml` 只加载 `AnimusForge.Bootstrap.dll`；临时 deploy/backup 目录已清理。以上部署结果
仅属于较早快照，不能代表当前项目 Stage 已安装。
LiveHostReadiness 当前为：`PASS`、`installedMatchesStage=true`、`gameRunning=false`。

## 仍未完成

- 尚未启动 Bannerlord，尚未进入真实 Campaign/Mission。
- 尚未产生真实 LIVE/SAVE、旧存档往返、live Economy/AFEF/Notoriety 或 Duel 副作用证据。
- 20 个阶段 8 领域仍为 `ROLE_PLACEHOLDER` / `REPRESENTATIVE`；全量 owner 认领和完整入口覆盖未完成。
- 六组 `declared-only`（`bootstrap-host`、`host-runtime`、`runtime-game-adapter`、
  `persistence-domain-owners`、`scene-duel`、`tools-content-release`）仍不得伪造 caller。
- cleanup candidate 仍为 `KEEP 12 / HOLD 3 / REVIEW_REMOVAL 3`，没有任何删除授权。
- 默认三渠道切换、facade 删除、Release 实际安装/发布仍禁止。

## 制作组下一步

1. 使用已部署的当前 Debug Stage，在隔离测试存档启动 1.3 和 1.4 Campaign，确认 Bootstrap 每次只加载一个 implementation。
2. 依次验证 Hero 金币、Party、Merchant、Debt；每次只改变一种副作用，并记录输入、主线程变化、confirmed facts、AFEF 和保存重载结果。
3. 分别验证 Native、SceneShout、Courier 的正常、取消、退出、重复 completion 和 fallback。
4. 验证 Notoriety/weekly、Duel（accept/reject/queue/start/cancel/death/exit、stake/debt、Memory/AFEF、Fourberie）以及其余领域。
5. 对新档、代表旧档、缺失/损坏/未知数据做 SAVE round-trip；记录 BuildInfo、模块组合、三份 DLL hash、存档 identity、步骤和日志。
6. 将每条结果写入阶段 8 evidence manifest，显式绑定 `domainIds`、`bridgeIds`、`apiLine`、`source.commit`、产物哈希和 ownerReview。
7. 所有 LIVE/SAVE 与 rollback drill 完成后，另行评审默认切换、cleanup、Release 安装和发布；不得提前执行。

## 安全边界

- 不要把 `wired` 或离线/compiled 证据写成阶段 7 DONE 或阶段 8 DONE。
- 不要删除 facade、切换默认入口、启用旧版 `AnimusForge_1_3_x` / `AnimusForge_1_4_5`，或手工覆盖 TaleWorlds DLL。
- 任何新 push、Release 部署、默认切换和发布都需要单独明确授权。
- 当前终端 UI 修改仍不在提交中；备份位于
  `F:\AFMOD\backups\AF-REFACTOR-terminal-ui-20260902-075113`。

权威配套资料：

- `docs/phase8/full-domain-acceptance-package.md`
- `docs/animusforge-complete-refactor-program-20260831.md`
- `docs/animusforge-refactoring-and-repository-reorganization-plan.md`
- `docs/handoffs/2026-09-03-bridge-binding-closeout.md`
