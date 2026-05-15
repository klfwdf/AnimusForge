# 事件周报素材注入案例

本案例记录事件系统向周报 LLM 注入素材时的两个关键约束：战斗结果必须进入事件素材，定居点易主必须保留真实移交方式。

## 战斗结果素材

- `CampaignEvents.MapEventEnded` 不能因为“玩家不是胜方”提前退出整个处理函数。
- 玩家击败 NPC 的个人记录只属于玩家战斗状态逻辑；事件素材记录应继续调用 `TrackNpcActionsFromMapEvent(mapEvent)`。
- `map_event` / `map_event_aftermath` 素材依赖 `EventMaterialReference.LocationText` 和 `EventMaterialReference.Won`。
- 复制、清洗、保存、聚合 `EventMaterialReference` 时必须保留 `LocationText` 和 `Won`，否则聚合后的战斗素材会丢失地点和胜负结果。
- 周报素材中应能看到 `战场交锋` 分类，以及“取得了胜利 / 遭遇了失利”这类明确结果。
- 与强盗、劫匪、藏身处等 `IsBanditFaction` 势力的战斗不进入周报战场交锋素材。
- 同一场 `mapevent` 只能聚合成一条战场交锋素材；不要按每个参战领主、胜负侧或 aftermath 重复写多条。
- 战场交锋素材应尽量包含地点、结果、胜方/败方王国、家族、人物、战前兵力、双方阵亡和负伤。
- 短批次里的战场交锋必须进一步压缩为一条统计素材：本周胜利多少场、失败多少场、己方总伤亡、造成敌方总杀伤。

## 定居点易主方式

- `ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter` 表示原版 `ApplyByBarter` 触发的交易/买卖移交，不是攻城。
- 个人记录和周报聚合字段都应把 `ByBarter` 写成“交易/买卖移交（非攻城）”。
- 定居点易主聚合素材应包含 `方式` 和 `事实约束` 字段。
- 当事实约束写明“交易/买卖导致的和平移交，不是攻城夺取”时，周报生成规则必须禁止写成攻陷、攻下、夺城或围城胜利。
- 只有 `BySiege` 才能作为军事攻城易主处理。

## 验证要点

- 运行 `dotnet build .\AnimusForge.csproj`，应为 0 警告、0 错误。
- 在开发菜单查看本周事件素材预览，确认普通领主战斗和围城战斗都会产生一条 `战场交锋` 聚合素材，并包含双方人物、家族/王国、兵力和伤亡。
- 切到短模式王国分组时，`战场交锋` 应只显示一条 `prompt_short_battle` 风格统计句，不再展开每场战斗详情。
- 与强盗/劫匪/野怪的战斗不应出现在 `战场交锋` 聚合素材中。
- 对通过交易转移的定居点，查看素材详情或周报 prompt，确认包含“交易/买卖移交（非攻城）”和“不是攻城夺取”。
