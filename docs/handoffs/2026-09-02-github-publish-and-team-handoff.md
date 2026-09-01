# AF 主体重构：GitHub 发布与制作组总交接

日期：2026-09-02

工作区：`G:\AFMOD\AF-REFACTOR`

本地分支：`codex/af-main-refactor-continuation-20260831`

远端交接分支：`origin/refactor/prepare-af-restructure`

## 六、最准确的结论

> **阶段7作为模块接入和离线/compiled验证阶段，大部分工作已经完成；阶段7作为完整交付阶段仍未
> 完成，因为真实Campaign/Mission、旧存档、live Economy/AFEF、Duel真实副作用和默认入口尚未验收。**

因此：

> **当前代码、HANDOFF与制作组简报可以推送GitHub并转交；阶段8仍只能做非破坏性准备，不能把这次
> push解释为阶段7 DONE、阶段8执行许可、默认切换或可发布游戏版本。**

## 本次GitHub同步

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

- PhaseEightReadiness：62/62 PASS。
- Bridge：10 cases / 6 invariants PASS。
- Composition：18 cases / 24 invariants PASS。
- ModuleCatalog：8 modules / 3 profiles / 16 invalid cases / 8 health states PASS。
- all-missing真实项目报告仍为`BLOCKED`，不是失败误报，也不是发布许可。

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
5. 若继续自动化原计划，下一安全工具切片是`LOCAL-7-C3`：让LiveHostReadinessAudit要求显式
   `--game-root`并用纯fixture/CLI验证；自动化当前已暂停，必须由接手人明确恢复或人工执行。
6. 所有真实门禁完成后，另行评审default cutover、旧facade删除、最终包和游戏部署；不得提前执行。

## 尚未完成 / 不得误报

- 真实Bannerlord Campaign/Mission：NOT-RUN。
- 真实旧存档加载与保存后重载：NOT-RUN。
- live金币、物品、Merchant、债务、Memory/AFEF/Notoriety：NOT-RUN。
- Duel live死亡、stake/debt、Fourberie、退出/取消时序：NOT-RUN。
- WorldMap、Diplomacy、Siege/GCCZ、周报、主动NPC、Issue等完整领域LIVE/SAVE签收：未完成。
- 默认Native/SceneShout/Courier切换、facade删除、最终打包、安装和发布：BLOCKED。

## 回滚

- 本次远端同步的共同基线为
  `9566bf3bec0642ccef6764db6b6630edc195300a`；完整提交范围以
  `git log origin/refactor/prepare-af-restructure..HEAD`为准。
- 阶段8准备链按HANDOFF中的逆序普通`git revert`，不得hard reset或force push。
- Duel M2实现：`git revert b93f93df`；M1实现：`git revert 16f3cbef`。
- Shout C2实现：`git revert ae49e3c8`。
- 文档提交独立revert；源码revert不会撤销游戏/存档副作用，也不会清理ignored Stage、runner output或
  validation目录。回滚后相关产物全部视为stale，必须重新构建/验证。
- 本次没有部署或游戏文件写入，所以无需恢复游戏目录、NEW-10、GCCZ或ONNX。

## 新任务启动语

> 请先读取 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-02-github-publish-and-team-handoff.md`、
> `docs\handoffs\2026-09-02-stage7-stage8-team-brief.md`和公共执行台账；fetch
> `origin/refactor/prepare-af-restructure`但不要覆盖本地未提交内容。阶段7保持VERIFY、阶段8执行保持
> BLOCKED。优先由实机人员补LIVE/SAVE；若只做可自主离线工作，则从LOCAL-7-C3显式game-root工具
> 边界开始。未经新的具体授权，不切default、不删facade、不部署、不覆盖游戏、不force push。
