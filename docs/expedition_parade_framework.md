# Expedition Parade / 出征阅兵框架

需求来源：`G:\A=MY MOD\HANDOFF.md`。本轮按产品方最新指示先建立总体框架，不沿用上一版阶段 0 场景探针实现。

## 已建立的层次

```text
AnimusForge.ExpeditionParade/
├─ Campaign/
│  ├─ ParadeCampaignBehavior.cs
│  ├─ ParadeEligibilityService.cs
│  ├─ ParadeRosterSnapshot.cs
│  ├─ ParadeSampleAllocator.cs
│  └─ ParadeAgentBudget.cs
├─ Configuration/ParadeSettings.cs
├─ Core/ParadePrimitives.cs
├─ Diagnostics/ParadeSessionDiagnostics.cs
├─ Mission/
│  ├─ ParadeCoordinator.cs
│  ├─ ParadeFormationController.cs
│  ├─ ParadeMissionLogic.cs
│  ├─ ParadeMissionRuntimeContracts.cs
│  ├─ CrowdReactionController.cs
│  └─ ParadeCleanupService.cs
├─ Routing/
│  ├─ ParadeRouteModels.cs
│  ├─ ParadeRoutingContracts.cs
│  └─ ParadeRoutePipeline.cs
├─ Presentation/ParadePresentationContracts.cs
├─ Runtime/ParadeFrameworkRuntime.cs
└─ ExpeditionParadeBootstrap.cs
```

框架已经固化以下不可变边界：

- 统一领地权限判断，村庄跟随绑定城镇/城堡；
- 真实健康兵员快照和不超额比例抽样；
- Mission Agent 预算预检；
- `Spawn → Assembly → Street → GateApproach → GatePassage → OutsideRoad → ExitZone` 路线数据模型；
- 覆盖、缓存、自动发现、候选规划、隐藏测试 Agent 验证的流水线接口；
- 阅兵总状态机、编队状态机、卡住恢复阶梯和队间启动闸门；
- 平民文化约束、稳定随机混合反应和观众状态机；
- 反向执行、只运行一次的幂等清理注册表；
- 相机和调试叠加层接口。

## 当前故意没有接入的功能

- 不注册“举行出征阅兵”菜单，避免向玩家暴露无法完成的半成品流程。
- 不生成或控制 Agent，不接管原生平民，不修改部队或存档。
- `ParadeMissionLogic` 已负责通用初始化、Tick、异常中止和 Mission 结束清理，但在提供经实测的 `IParadeMissionRuntimeAdapter` 前不会被挂入任何 Mission。
- 不硬编码未经 v1.4.7 实测的场景实体名、标签、动作、音频或红色边界 API。
- 不提供假路线解析器、穿墙传送或无提示删除降级。

## 后续适配器

下一步只需围绕现有契约补充 Bannerlord 运行时适配器：

1. `ISceneAnchorResolver`：读取实际场景实体、标签、出生点和边界；
2. `IParadeRoutePlanner`：生成分段候选路线；
3. `IParadeRouteValidator`：执行导航查询、门洞宽度和隐藏 Agent 试走；
4. Mission 行为：把 `ParadeCoordinator` / `ParadeFormationController` 的状态转换映射到真实 Formation 与 Agent；
5. Crowd 适配器：只登记明确识别的平民并把恢复动作注册到 `ParadeCleanupService`；
6. 完成一个城镇纵向切片后再开放聚落菜单。

所有运行时适配器必须通过本地 v1.4.7 程序集和游戏内证据确认后再写入，不得依据旧版本示例猜测。
