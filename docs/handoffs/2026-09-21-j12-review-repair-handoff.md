# J12 审查修复技术 HANDOFF（2026-09-21）

## 结论

审查基点 `87749ae0` 的 J12 全部 `OFFLINE_VERIFIED` 结论不成立：`1c62c2c9` 对 Direct/World/WorldMap 是 R100 目录迁移，没有满足原计划 b2–b4/c1–c4 的职责和生命周期退出门。本轮没有回滚已完成迁移，而是在真实消费者上修复已复现问题，并把状态纠正为 `J12_REOPENED_PARTIAL`。

产品修复提交：`a50ab3ad`。交付目标为 `origin/codex/af-main-refactor-continuation-20260831`。

## 已修复

| 责任 | 生产位置 | 行为 |
| --- | --- | --- |
| Direct 外交动作 owner | `src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs`、`DirectDiplomacyWarGuard.cs` | 七类动作实算法从 Prompt/Campaign host 分离；玩家方向宣战必须当前为国王且 payload 王国匹配；Apply 后只有观察到真实战争才通知 WorldDiplomacy |
| WorldDiplomacy 请求 owner | `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyRequestLeaseCoordinator.cs`、`WorldDiplomacyBehavior.cs` | 单一 lease 冻结 jobId/runtime generation/maxTokens/timeout；worker 只捕获 detached messages 与 immutable snapshot；迟到旧代 completion 不能释放新请求 |
| WorldMap 延迟请求 owner | `src/modules/AF.Module.WorldMap/Runtime/WorldMapDelayedRequestCoordinator.cs`、`WorldMapPartyCommandBehavior.cs` | 打开 UI 生成进程内票据；callback 只 claim 一次且只释放自己；旧/重复 callback 零游戏副作用，不清新请求 busy |
| WorldMap 回执 | `WorldMapPartyCommandBehavior.TryAppendQueue` 与 `TryApplyWorldMapOrderTagsForExternal` | `AddedCommandCount` 使用经过资格过滤、实际写入队列的数量，不再回报原输入数 |

没有新增 SyncData key、Saveable 类型、公开 V1 ABI、程序集或默认开关。请求票据均为进程内状态，符合原生 UI/网络请求不能跨读档恢复的生命周期。

## 本轮验证

- `tools/J12DomainLifecycleRegressionTests`：32 断言通过，覆盖非国王/错误王国/成功宣战观察、同 jobId 异代 completion、错误 owner release、旧/重复 WorldMap callback。
- 完整 `WorldDiplomacyIntentBoundary` 临时 net8 runner：1175 断言通过；测试已跟随 `DiplomacyBehavior.Actions.cs` 与真实 `StartupPatchComposition` 注册路径。`package_policy_system_source_overlay.py` 已包含新增 request lease 依赖。
- 精确生产方法离线复现：
  - 非国王经 normalizer→public dispatch→parser→执行：修前 `war=1/notify=1`，修后 `0/0`。
  - 最近和平 Harmony 拦截：修前 `war=0/notify=1`，修后 `0/0`。
  - 正常国王宣战对照：`war=1/notify=1`。
  - WorldMap A 完成→B 打开→旧 A 再回调：修后 dispatch 保持 1 且 B busy 保持 true。
- Debug/Release：Bannerlord 1.3、1.4、Bootstrap 六构建均 0 warning / 0 error。
- 四个 Debug/Release 实现 DLL：API/metadata 1060 断言通过。`PersistenceProfileConfig` 142 literal / 168 typed / 13 chunked / 44 flattened；Persistence chunk 8、Identity 5、Bridge binding 23、runtime isolation 12、J09 wiring 25、Phase8 readiness 73 通过。`python tools/test_repository_source_inventory.py`：7/7 通过。
- `git diff --check` 与 touched roots conflict/TODO 清理搜索通过。

PhaseEightParityReplay 需要先由既有官方流程准备当前 Stage；本轮没有获得 Stage/部署授权，且项目本地当前 Stage 不存在，因此该 runner `NOT-RUN`，没有用旧 DLL 冒充当前候选。上述离线替身没有声称真实游戏 UI 会重复触发 callback，只证明即使重复/迟到发生也不会再造成第二次游戏 mutation。

## 仍未完成

1. J12b3 完整 world diplomacy queue/start/completion/commit owner；当前 request lease 已落地，但大型领域队列与提交仍在 `WorldDiplomacyBehavior`。
2. J12b2/b4 的剩余资格/动作回执与 Vassalage/Annexation 唯一 owner 消费证据。
3. J12c1 协议/capture、c2 受理分支、c3 队列/事件生命周期的实际分离与 source-linked 行为回放。
4. J12c4 governor expedition 延迟请求的 request identity/claim/completion；本轮仅闭合同伴建队 UI 请求。
5. 本轮 Release/API/Persistence/Bridge 已通过；剩余生产代码落地后仍须在最终候选重跑这些门禁，并补三渠道、Phase8 和代码地图。

因此 J13 暂不启动。不得把此次 bug 修复写成 J12 全部完成，也不得靠继续移动整个混合文件或增加无消费者 wrapper 结项。

## 下一步与回滚

按原计划继续：J12b3 剩余 → J12b4 → J12c1 → c2 → c3 → c4 governor → J12d。每个包都要有正常路径、能命中具名断言的有效反例和真实消费者接线。

回滚优先定向 `git revert a50ab3ad`；文档提交在其后单独 revert。禁止 hard reset/rebase/强推。真实 Campaign/Mission、旧 SAVE、live Economy/Diplomacy/WorldMap/AFEF、provider、音频和性能仍 `NOT-RUN`。
