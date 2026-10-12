# 债务承诺与决斗结算显示收口

阶段：修改意图。main 基线 `59e8a70eb`。用户已停止 Jev 联动；本片不用 Jev。

范围：DebtPromiseQuest 的标题/默认备注/无 Hero 玩家名称显示、既有加载时日志格式迁移；EconomyUiTexts 与既有 afeconomy 双资源；DebtLedger 的独立显示日期及兼容欠款输出重载；DuelBehavior 的声望/赌注结果通知及既有 af_ui 双资源。原始中文存档默认值、参与者匹配 token、模型事实、日志、转账/欠款创建、结果状态与存档字段身份保留。新重载只创建一次欠款，不让显示异常改变已完成的业务结果。

调查发现：E 报告把 BuildDebtEditorSummary 误列为未投影；当前外层到期句子已通过 BuildDebtEditorDueStatusText 投影，但 DATE 参数来自 FormatAbsDayAsGameDate 的中文日期，仍是实际显示缺口。新增显示日期使用同一季/年长度计算，原日期函数及 Prompt 消费者不改。Duel 的两次欠款结果同时构建事实和 HUD，事实使用原到期句子、HUD 使用并行显示值；结算结果返回值的三个消费者都是 QuickInfo。

运行频率：标题按原界面访问解析；日志仅创建/加载升级时构建；结算文本仅原结算事件生成。无 Tick 扫描、语言探测、反射、额外后台本地化或重复业务调用。

退出门：中英资源键/占位符/英文无汉字；真实显示消费者接线；保存字段、原始事实和业务操作保真；配置季/年长度与到期边界检查；26 对注册资源合同；可用既有两版本 Localization DLL 的资源解析。当前产品编译/程序集契约、游戏、旧档、Stage 和正式 CN/EN ZIP 均 NOT-RUN。不构建、部署、Push，不改其他作者未提交资产或 HANDOFF。

回滚：使用本片产品提交的 focused inverse，禁止整文件回滚共享资源。详细实际结果在验证后追加。
