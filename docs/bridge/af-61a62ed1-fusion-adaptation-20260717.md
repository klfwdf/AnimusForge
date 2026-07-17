# AF 61a62ed1 + SETS/GCCZ 融合适配（2026-07-17）

## 基线

- AF 上游：`Mount-Blade-Bannerlord-AnimusForge-mod` 的 `origin/main`，提交 `61a62ed1`。
- 融合树：`NEW-10`，合并提交 `ad3cf201`。
- 本轮只更新源码与项目本地 stage，不覆盖正在测试中的游戏目录。

## 上游变化

- 新增/重构本地王国政策、政策历史 UI、议程上下文和政策评估提示词。
- 更新主动 NPC 请求、世界地图部队命令、信使、奖励、知识库和稳定性诊断链。
- 更新统一模块部署脚本；仍保持 Bootstrap + 1.3/1.4 双实现结构。

## 融合边界

1. `RuleBehaviorPrompts.json` 保留全部上游政策与议程规则；`siege_intervention_aftermath` 继续保持空触发词，只能由 GCCZ active stage 运行时注入。
2. `ShoutBehavior` 保留上游 preprocess / main reply / deferred postprocess 流程。GCCZ 只通过 `AfGcczShoutBridge` 接入，并且仅在 active stage 使用独占标签路由；普通 AF 对话仍可使用政策、议程、世界地图命令等上游规则。
3. `RewardSystemBehavior` 保留上游奖励与资产逻辑；城堡 GCCZ 仅保留族长归附、领主军械和原版赎卖价格等窄外部桥。
4. SETS 继续由融合树中的入口选择、场景冲突、胜利菜单与 GCCZ handoff 代码实现；`GCCZ`/`SETS` 独立目录不接收 AF 主体代码。
5. 城堡普通战俘处置仍为可改判暂定命令：非屠戮标签不立即删除场景 Agent 或战役名册，离场只结算最后一道有效命令；屠戮只按实际死亡结算。

## 验证

- GCCZ standalone core tests 全部通过。
- AF 1.3、AF 1.4 与 Bootstrap Release 构建均为 0 warning / 0 error。
- 项目本地统一模块 stage 成功，未修改游戏目录。
- `GCCZ/src/AnimusForge.SiegeAftermathIntervention/*.cs` 与 `NEW-10/AnimusForge.SiegeAftermathIntervention/*.cs` 应保持逐文件 SHA-256 一致。

