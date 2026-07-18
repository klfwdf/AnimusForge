# AF `0aa3f5d0` / GCCZ / SETS 融合适配（2026-07-19）

## 基线与边界

- 最新 AF 基线：`0aa3f5d0`。
- GCCZ 可复用 core 仍以 `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention` 为准，并逐文件镜像到 `G:\AFMOD\NEW-10\AnimusForge.SiegeAftermathIntervention`。
- AF、SETS 与 Bannerlord live side effects 只留在 `NEW-10`；不把 AF 主体复制进 GCCZ 或 SETS 独立仓库。

## 本轮冲突适配

- 上游与融合树只有 `ShoutBehavior.cs` 发生文本冲突。
- 保留上游即时场景反应的 `canStillPublish` 过期响应门禁和 `onCompleted` 完成回调。
- 同时保留 GCCZ 的 `runSiegeReactionPostprocess` 参数；只有 active castle stage 明确请求时，见证士兵回复才进入 GCCZ 统一后处理与标签路由。
- 过期回复在 GCCZ 后处理之前即被丢弃；完成回调只排队一次，避免重复状态推进。

## 运行时不变量

- `siege_intervention_aftermath` 仍是被动规则：`TriggerKeywords` 为空，只能由 active GCCZ runtime 注入。
- SETS 城镇内部暴乱胜利后先打开原版定居点胜利处置菜单，再由玩家选择是否进入 GCCZ；旧“SETS 直接进入 GCCZ”记录已被当前实现取代。
- SETS 不单独创建城堡处置入口；城堡内部夺取只有在原版胜利菜单出现后才可进入城堡 GCCZ。
- SceneTaunt 对 SETS 随行者的身份过滤、SETS 胜利退出以及 GCCZ pending/open 门禁保持有效。
- 不复活旧 GCCZ-C 桥，不把 core 策略复制回 `SiegeAiInterventionBehavior.cs`。
