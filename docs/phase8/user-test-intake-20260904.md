# 阶段 8 实机结果接收台账（用户报告）

日期：2026-09-04
工作区：`F:\\AnimusForge-main`
分支：`refactor/prepare-af-restructure`
用户实测运行时代码基线：`c01a2fcc3e9471d6c38f513423cab9ce91ca44f1`
当前静态审查基线：`57f10cec90fbdf6eb59f55e60b5c1aebdd10bbc6`
接收台账提交：见本文件所在提交；用户实测结果不自动代表后续 WarStats 代码已实测。

## 1. 当前结论

用户已报告：

- 1.4.8 旧档 SAVE round-trip：`PASS`。
- 领域 `1、2、4、5、6、7、8、9、10、11、12、13、14、15、16、17、18、19、20`：`PASS`。
- 领域 `3 runtime-diagnostics`：本次消息未报告，保持 `PENDING`，不能推断为 PASS。

以上是用户结果接收，不等同于 readiness manifest 中的正式 `LIVE/SAVE` 证据。正式记录仍需要
操作步骤、预期/实际观察、Campaign/Mission 证明、存档 identity、日志附件哈希和 ownerReview。

## 2. 实机环境绑定

- 游戏 BuildInfo：`v1.4.8.119303`
- API line：`1.4`
- Bootstrap SHA-256：`81417DDC1B7A457A3C2ACD2D35523C793F58ED91C6AB9FCA83798E9E4648523C`
- 1.3 implementation SHA-256：`B3CF2ABE582BE00BDA67ECF5EE85AD166DE3A33BE8D0598A97D78A755BACD2E6`
- 1.4 implementation SHA-256：`973C1B21027F63A784468951233224179E684882EBCE144A7F44EC65FF4EA9A3`
- 统一模块目录：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`

Bootstrap 日志已出现 `selected API=1.4` 和 `versions\\1.4\\AnimusForge.dll`，说明本次 1.4.8
启动选择了 1.4 implementation；仍需把该日志作为每条正式 evidence 的附件绑定。

## 3. SAVE 工件

用户 round-trip 的日志和存档快照位于被忽略的目录：

```text
F:\AnimusForge-main\artifacts\phase8-live-save-20260904\user-run-01
```

其中：

- `saves/save003.sav`：原始旧档快照，SHA-256 `763A78C0755CD0ED2A8832328F25A7FE5D7FADE5FFD4D6D70B3F3059ADCC9499`
- `saves/save003-20260903-1.4.8-original-copy.sav`：隔离输入副本，SHA-256 同上
- `saves/save003 - 副本.sav`：本次用户报告的结果候选，SHA-256 `9492B3A582A84D72D186049F94507E0BAA82DDB9792DBE43D4DAD56A4D46784A`
- `user-test-intake-20260904.json`：机器采集的哈希、日志和用户结果映射

原始游戏存档仍保留在：

```text
C:\Users\29310\Documents\Mount and Blade II Bannerlord\Game Saves\save003.sav
```

## 4. 20 域状态处理

本轮已根据用户确认的单一真实账号完成 owner roster 和静态入口 review：

- 20 个 domain：`ownerAssignmentState = ASSIGNED`
- 20 个 domain：`entryCoverage = COMPLETE`
- 入口路径和 reviewed-pattern candidates：全部存在，`entry_inventory.py --check` 为 `PASS`
- 形式化 LIVE/SAVE 证据：仍为 `PENDING_OWNER_REVIEW`
- `runtime-diagnostics`（domain 3）：明确保持 `NOT_RUN`

详细映射见 `docs/phase8/owner-roster-entry-review-20260904.json`。入口晋级不等于运行时证据通过；所有
LIVE/SAVE 记录仍必须绑定实际步骤、Campaign/Mission、BuildInfo、存档 identity、日志哈希和 ownerReview。

用户可以用一个真实账号兼任全部 20 个逻辑 owner；记录时需要显式列出账号到每个 domain/Bridge
角色的映射，不能只写“制作组已确认”。

## 5. 下一步门禁

1. 补 domain `runtime-diagnostics` 的最小 LIVE 证据：Campaign tick、队列/缓存边界、退出清理、
   无 stale completion。
2. 将用户报告的 19 个领域分别拆成正式 evidence record，绑定 `domainIds`、真实 `bridgeIds`、
   当前源码 commit、BuildInfo、DLL/存档哈希和日志附件。
3. 一个账号承担 20 个逻辑角色的 owner roster 与完整静态入口 review 已完成，见
   `docs/phase8/owner-roster-entry-review-20260904.json`。
4. 对失败或缺附件的领域保持 `BLOCKED/PENDING`，不以口头 PASS 补齐证据。
5. 全部 LIVE/SAVE 和 owner review 完成后，才进入 Release Stage/ZIP、安装验证、rollback drill、
   最终 diff 审查和推送。

WarStats 终端整合交接说明已纳入当前分支：`docs/war_stats_terminal_integration_handover.md`；它会在最终
diff 审查通过后随本分支一起推送。本文档不改变 Release、rollback、default cutover、删除 facade 或
push 的最终门禁。
