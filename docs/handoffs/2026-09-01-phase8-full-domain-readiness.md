# LOCAL-8-A：阶段 8 完整 20 领域准备态接续

日期：2026-09-01。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

## 结论

- `LOCAL-8-A` 代码/fixture/文档与离线验证完成，状态 **VERIFY**。
- 基线/已推送远端 `9566bf3b`；意图 checkpoint `9a088f2f`；实现 `b1c5a81a`；
  full-domain Bridge SAVE补强`1e341c43`；未认领owner gate `f4a02018`。
- 阶段 7 仍是 VERIFY；阶段 8 只完成非破坏性准备，执行仍 BLOCKED。
- 没有改生产 C#、默认入口、存档 key/type、GCCZ/NEW-10、游戏、ONNX 或玩家数据；
  没有删除任何 facade/bridge/flag/shim，也没有部署、启动游戏或推送本轮本地提交。

## 旧门禁缺口

原 `PhaseEightReadiness` 只绑定早期 8 个 design ID、两个 Bridge 和 18 个 Composition case。
合成 49-record fixture 可以 `FIXTURE-VALID`，但 Economy、Duel、Courier、WorldMap、Memory、
UI/TTS、Tools/Package 等其余责任领域完全不参与判定。这是本轮红基线，不是产品测试失败。

原工具还存在三个准备态缺口：

1. Bridge/Composition case 只检查 OFFLINE/LIVE，不要求 SAVE case 覆盖。
2. `HEAD` 自身可通过 ancestor 检查，可能被冒充回滚点。
3. 同 module 的 generic evidence 可支持任意清理候选，没有绑定具体 symbol/entry。

## 本轮实现

### 完整责任目录

`docs/phase8/full-domain-readiness-catalog.json` 新增：

- 总纲 1–20 的稳定英文 domain ID、owner/maintainers。
- maintainer当前全部显式为`ROLE_PLACEHOLDER`；real mode在团队确认并改成`ASSIGNED`前
  一律`UNASSIGNED_DOMAIN_OWNER/BLOCKED`，不冒充制作组成员已认领。
- 真实 project-local entry file；路径在门禁加载时解析并限制在项目根。
- Prompt / ActionPlan 的 `APPLICABLE`、`NOT_APPLICABLE`、`MIXED`。
- persistence responsibility、已知 key/type、failure fallback、default route。
- offline/compiled/live/save/release 当前状态和 blocking gates。
- 16 组跨域 Bridge、endpoint、entry、implementation state、required cases 和 live gate。

这些是验收责任桶，不是生产模块声明；早期 8-ID catalog 和 Pending entry type 没有被覆盖。

### 清理与逐项回滚目录

`docs/phase8/cleanup-candidates.json` 记录 16 项：

- 10 `KEEP`
- 3 `HOLD`
- 3 `REVIEW_REMOVAL`

`REVIEW_REMOVAL` 仅包括私有窄 flag/恒值分支：`VerboseInspectionLogs`、
`RefreshAllPlayerAgents`、`_enableRhubarbSoundEventPlayback`。它们仍缺 live/save/release 替代证据，
不授权删除。活跃 Legacy facade、Gateway、old-save adapter、MCM/反射、save tombstone、
compatibility 和 GCCZ bridge 均被明确 KEEP/HOLD。

### Readiness fail-closed 补强

`tools/PhaseEightReadiness/readiness.py` 现在：

- 严格校验20领域所有字段、真实入口、Bridge与清理目录。
- evidence record必须声明已知唯一`domainIds`；module与全部domain maintainer共同审核。
- 20领域各自要求OFFLINE、LIVE 1.3/1.4、SAVE 1.3/1.4、RELEASE证据。
- 两组既有Bridge、18-case Composition和16组full-domain Bridge都检查
  OFFLINE/LIVE/SAVE五个索引。
- replacement与rollback evidence必须用`cleanupCandidateIds`精确绑定静态盘点项和owner domain。
- 全局rollback必须严格早于HEAD；HEAD不再是合法rollback point。
- `fullProjectReleaseReady=false`和五项授权false保持不变；不执行manifest中的命令。

## 验证

证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\phase8-full-domain-a-20260901-191601`

最终通过：

- PhaseEightReadiness：54 tests / 167.471s / OK。
- Bridge fixture：10 cases / 6 invariants PASS。
- Composition matrix：18 cases / 24 invariants / 6 categories PASS。
- Module catalog：8 modules / 3 profiles / 16 invalid cases / 8 health states PASS。
- Catalog：20 domains / 16 bridges / 16 cleanup candidates（3 review）PASS。
- all-missing：`BLOCKED / exit 2 / full20 / authorization all false`。
- Python syntax、JSON parse/path、`git diff --check` PASS。

无效中间命令保留在 `invalid-command.log`：

1. 第一次用 package-style unittest 路径，sibling `import readiness` 导致 discovery ImportError；
   改为从工具目录运行后正常。
2. 一次 inline Python f-string 被 PowerShell quoting 截断；改用 PowerShell JSON 检查后正常。

本轮没有生产 C# 或构建配置变更，因此 fresh production DLL replay、Persistence/Identity和
Debug/Release六Stage按纯工具/fixture规则记 `N/A`，不能把上一轮Stage结果冒充本轮运行。

## 独立只读审计

- Stage8工具审计确认原8-ID绿色遗漏20领域、HEAD rollback、candidate绑定和SAVE Bridge缺口。
- 20领域入口审计确认：`SubModule`仍是集中生产组合根；新三渠道仍opt-in；Economy已有typed
  owner，非Economy仍委托legacy owner；95 key/121 binding不是旧档实证。
- cleanup审计确认当前没有可立即删除文件；Legacy命名不等于dead，MCM/Harmony/reflection/save
  责任不能靠普通caller scan排除。

以上审计针对修改前基线；最终实现仍需一次post-change P0/P1复核，若代理usage limit阻塞必须
如实记录，不能冒充独立终审通过。

## 仍未验证 / 风险

- 所有20领域的真实Campaign/Mission与1.3/1.4 LIVE/SAVE证据仍未采集。
- Hero/Party/Merchant、债务、AFEF/Notoriety、旧档与default cutover仍NOT-RUN。
- hash与owner声明不能证明观察真实性；工具不反编译产物，也未闭合ZIP allowlist/provenance。
- full-domain Bridge case是责任/证据门禁，不是16个已上线生产Bridge。
- 静态`currentEvidence`是当前快照；未来只能由新hash-bound evidence更新，不能直接把JSON改PASS。
- 源码revert不撤销已经写入存档的资产/债务/领地/装备副作用。

## 回滚

- full-domain Bridge gate：干净工作树普通执行 `git revert 1e341c43`。
- owner assignment gate：普通执行 `git revert f4a02018`。
- 20领域readiness实现/fixture：普通执行 `git revert b1c5a81a`。
- 意图checkpoint：`9a088f2f`；文档提交可独立普通revert。
- 禁止hard reset、rebase共享历史或force push。本轮未部署，无游戏文件回滚。

## 下一步

1. 自动化继续保持30分钟ACTIVE；新ID为`af-7-8`，旧`af`已暂停，避免同仓库并发写。
2. 下一代码纵切片建议`LOCAL-7-M`：Duel typed owner/outcome/readback。
3. 领域owner并行按`docs/phase8/full-domain-acceptance-package.md`采集真实LIVE/SAVE证据。
4. 没有真实门禁与具体用户授权，不删除3项review候选、不切默认、不部署/发布。

## 新线程启动语

> 请在 `G:\AFMOD\AF-REFACTOR` 读取项目AGENTS、公共台账、
> `docs\handoffs\2026-09-01-phase8-full-domain-readiness.md`、
> `docs\phase8\full-domain-acceptance-package.md`及两个JSON目录。确认HEAD至少包含
> `b1c5a81a`、`1e341c43`和`f4a02018`，fetch但不要覆盖本地历史。阶段7保持VERIFY、阶段8执行保持BLOCKED。
> 优先进入`LOCAL-7-M`只读审计Duel stakes/Mission result/death/cancel/exit/Memory事实顺序，
> 先红测再最小typed owner；不得从legacy callback或标签推测成功，不切default、不删facade。
