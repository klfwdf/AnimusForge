# 当前交接：J12 审查修复已落地，阶段重新标为 PARTIAL（2026-09-21）

- **状态**：J07–J11 `OFFLINE_VERIFIED`；J12a 与 J12b1 保持 `OFFLINE_VERIFIED`，J12b2–b4 / J12c1–c4 为 `PARTIAL`。J13 暂不启动，避免把未完成职责带入下一阶段。
- **纠正**：`1c62c2c9` 只完成 Direct/World/WorldMap 的目录归位，不能满足原 J12 计划规定的真实职责拆分和生命周期门禁；旧 HANDOFF 的 J12 全部完成结论已撤销。
- **修复提交**：`a50ab3ad`。Direct 七类动作算法移入 `DiplomacyBehavior.Actions.cs`；非国王玩家不能代表王国宣战，只有观察到真实战争状态才发布世界外交成功事实。
- **异步所有权**：`WorldDiplomacyRequestLeaseCoordinator` 冻结 job/generation/token/timeout，worker 不再闭包读取可变 job；旧代完成只能释放自己的 lease。
- **WorldMap**：`WorldMapDelayedRequestCoordinator` 让旧/重复关窗回调零副作用且不能清新请求 busy；`AddedCommandCount` 使用过滤后的实际入队数。
- **新增证据**：`J12DomainLifecycleRegressionTests` 32、完整 Intent 1175 断言；外交两个旧红例修后通过并有成功宣战对照；WorldMap 旧回调注入修后保持新请求 busy；Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；repository source inventory 7、四 DLL API/metadata 1060、Persistence 142/168/13/44、Identity 5、Bridge 23/12、J09 wiring 25、Phase8 readiness 73 通过。
- **仍需完成**：WorldDiplomacy 完整 queue/start/completion/commit 责任包；WorldMap c1 协议、c2 受理、c3 队列/事件以及 governor 延迟请求的生产回放；同一最终候选的 Release/API/存档/三渠道整包。
- **详细入口**：[修复 HANDOFF](docs/handoffs/2026-09-21-j12-review-repair-handoff.md)、[J12 实施计划](docs/plans/j12-domain-owners-plan.md)、[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j12-review-repair-20260921)。
- **未验证**：真实 Campaign/Mission、旧 SAVE、live Economy/Diplomacy/WorldMap/AFEF、provider、音频和性能均 `NOT-RUN`；未 Stage/Deploy/Package、未写游戏或存档。
- **工作区**：`G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`；本地分支 `codex/af-modularize-j04-20260918`；交付分支 `origin/codex/af-main-refactor-continuation-20260831`。