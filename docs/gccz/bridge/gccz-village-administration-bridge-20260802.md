# GCCZ 村庄贵族处置融合说明（2026-08-02）

此文档镜像独立 GCCZ 工程中的同名融合契约。GCCZ 村庄只服务于贵族管理自己管辖的村庄：玩家氏族直接拥有上级封地，或玩家作为王国统治者管理本国封臣村庄时允许；普通封臣进入其他贵族的村庄时不激活，保留 AF 原逻辑。

融合点：

- `VillageAftermathBehavior.cs`：Bannerlord 运行时薄适配、存档同步、结果落地。
- `SettlementEntryTroopSelectionBehavior.cs`：SETS 村庄任务生命周期与村民召集桥。
- `AfGcczShoutBridge.cs`：村庄采用非独占后处理，不屏蔽普通 AF/原版标签。
- `AnimusForge.SiegeAftermathIntervention/Village*.cs`：独立权限、标签、数值、提示词和文化改造规则。

村庄处于劫掠、围攻或非正常状态时不激活。村庄处置不自动跳转围城菜单，也不会把普通访问改造成围城战。文化选择界面和即时改造回调必须捕获异常并记录 GCCZ 日志，不得把异常抛回任务 Tick/UI。
