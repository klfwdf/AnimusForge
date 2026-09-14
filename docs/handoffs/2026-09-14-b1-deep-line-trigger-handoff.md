# AF 深 line/trigger 预算 HANDOFF（2026-09-14）

## 一句话结论

**阶段8 / B1继续VERIFY，未整批合格；本轮单draft深line与trigger bind已按共享metadata计费并离线联验。** 生产/测试`4d6994bc7cf219a2f894377d7262a90b466f9cbe`，检查点`eb6389f4`，前生产`86805518`。自动化保持PAUSED；不进入B2；未推送、部署、改默认或操作真实存档。

本工作区为 Codex worktree（detached HEAD `4d6994bc`）；指定远端仍是 `origin/codex/af-main-refactor-continuation-20260831`。不要占用另一 worktree 上的同名分支。不重做索引/共享窗口/排序/typed raw/逐draft owner额度。

## 实际修改

| 责任 | 当前结果 |
|---|---|
| 单draft 1024行 | 不再随一次expensive身份把1024行原子做完。每条line消耗共享metadata；有限窗口最多128 metadata，实测1×1024行9窗、窗内最多127行 |
| trigger bind | 原MemoryId/day/date逐条绑定按metadata计费；`SanitizeWeeklyMemoryMaterialTriggers`仍在bind完成后一次原子调用 |
| 共用规则 | 抽出`BindDailyMemoryDraftWeeklyTrigger`与`SanitizeDailyMemoryDraftLine`；同步`SanitizeDailyMemoryDraftEntry`与续跑路径共用，无第二套规则 |
| 来源安全 | 未完成draft的line/trigger列表保持私有；列表引用/count变化使该规范化失效并重新封存，不按旧cursor删新行 |
| 保留入口 | 同步Sanitize仍服务读/存档；oracle仍是40b92e67原`SanitizeDailyMemoryDrafts` |

**有意时序变化：** 单draft的line/trigger bind可跨窗口；元数据trim/key占用在draft身份时立即可见。不是整draft事务。trigger列表sanitize仍原子。

运行频率：Campaign维护/deferred封存，不是每帧。缓存/分批：共享窗口128 metadata + 8 expensive；不能抢占一次仍原子的trigger列表sanitize。

## 代码位置（4d6994bc7cf219a2f894377d7262a90b466f9cbe，一基行号）

| 路径 | 行号 / 符号 | 责任 |
|---|---|---|
| `MyBehavior.cs` | 26573–26583 `SanitizeDailyMemoryDrafts` | 保留同步入口与原day稳定顺序 |
| 同上 | 26585–26593 `BindDailyMemoryDraftWeeklyTrigger` | 原逐条MemoryId/day/date绑定 |
| 同上 | 26596–26633 `SanitizeDailyMemoryDraftLine` | 原Where+Select体 |
| 同上 | 26635–26679 `SanitizeDailyMemoryDraftEntry` | 同步入口组合helper |
| `MyBehavior.MemorySealing.cs` | 128–213 `DailyMemoryDraftNormalization` | 逐draft expensive身份，内嵌可续跑entry |
| 同上 | 218–371 `DailyMemoryDraftEntryNormalization` | line/trigger bind metadata续跑 |
| 同上 | 642–734 `RunDailyMemorySealDrafts` | 真实caller，失效重封 |

[77点定位图](../architecture/af-framework-code-map.json)通过记录提交与工作树双校验；不是整文件完成白名单。

## 验证结果

[验收JSON](../audits/2026-09-14-b1-deep-line-trigger-verification.json)记录本轮实际命令与返回码。

| 检查 | 本轮结果 |
|---|---|
| 封存当前 | 76/0（原75+line-change） |
| 旧40b92e67 | 同76例61绿15红（原13项owner红 + inner-cost现要求line预算 + 新line-change） |
| 新反例 | `unbudgeted-line-normalize` BUILD_PASS后inner-cost红（75/1）；`ignore-line-source` 3红（empty-grows / kept-empties / line-change） |
| 工作量 | 257记录owner仍每窗最多8 draft；1×1024行9窗、max_lines=127、total=1024 |
| 原语义 | 同步oracle合同、empty-first/别名引用、中途owner变更用例保持 |
| 源码守卫 | `test_source_parity.py` 12/0；code-map 记录提交与 `--working-tree` 77锚点通过 |

未重跑完整captured/business/六项Stage/API/存档身份；LIVE/SAVE仍NOT_RUN。

## 未完成与下一步

- trigger列表`SanitizeWeeklyMemoryMaterialTriggers`仍一次原子；长字符串/全owner绑定/首次capture/copy/Apply未切分。
- 继续原计划第16节剩余：初次capture/复制、全owner/raw/最终绑定、Apply/public/weekly尾步。不重做本轮line预算或已完成五项。
- B1整批合格前不进B2。

## 回滚

定向revert生产/测试`4d6994bc`及随后本轮文档提交；检查点`eb6389f4`。不要hard reset/force。
