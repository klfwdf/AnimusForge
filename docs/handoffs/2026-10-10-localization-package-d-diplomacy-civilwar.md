# Package D：外交内政与内战显示

阶段：待接力。先读[共同规则与文件边界](2026-10-10-localization-cde-plan.md)。
任务：收口外交/内战实际可见中文，包括英文通知中的动态中文尾巴。

## 独占范围

- `src/modules/AF.Module.Diplomacy/` 的显示及必要接线：Agenda/VoteDealBehavior*、
  Profiles/KingdomStrategicProfileBehavior*、Application结果通知与既有UI文本目录。
- `src/modules/AF.Module.Kingdom/CivilWar/` 的显示投影及Actions/Panel/DialogueTags接线。
- `src/AF.GameAdapter.Bannerlord/UI/Kingdom/KingdomFactionTab.cs`、
  `src/AF.GameAdapter.Bannerlord/UI/Editors/KingdomStabilityLabController.cs`
  且用于稳定度哨兵接线；除此GameAdapter需要先给主线窄路径，不抢共享文件。
- `afdip_ui_strings` 和 `afkingdom_ui_strings` 两对资源，路径在
  `content/modules/AF.Module.Diplomacy/ModuleData/Languages/` 与
  `content/modules/AF.Module.Kingdom/ModuleData/Languages/`。
- 对应外交/内战专项测试；独立交付
  `docs/handoffs/2026-10-10-localization-package-d-diplomacy-civilwar-delivery.md`。
- 禁止修改CustomPrompts JSON（主线协调）、PolicySystem（C持有）、
  RewardSystemBehavior.cs（E持有）、MyBehavior.cs、共享注册与af_ui_strings（主线持有）。
  不触碰B的世界书资产；共享内部接口若需新增显示端口，先给主线精确接口建议。

## 优先级

1. VoteDeal内政面板：历史报告指出 `.cs`/`.Agenda.cs` 约129旧中文行，
   数字是代码候选不是129词条。分别追踪实际面板/HUD与Prompt/AFEF生产者。
2. `WorldDiplomacyAnalysisApplication.Commit` :30 的
   `execution.Notify(document.MechanicalResult)`，中文MechanicalResult生产点包括
   :73/:270/:326/:389。独立显示结果，原存档字段不变，别泛化错误丢掉失败原因。
3. KingdomStrategicProfile生成错误与DevUi中文前缀比较，旧位置主类
   :774/:822-823/:881、DevUi:785-810。必须成对改，优先结构化状态避免显示文字判逻辑。
4. CivilWar `ConcessionPending`/`Conceded`：Actions旧:379/381将reason/ctx.Notes
   拼入通知。保原事实，独立显示效果与原因，未知旧回执保真实原文并报告限制。
5. CivilWar对话标签 `TryApplyTag` / `RecruitBlockReason` 仍原中文，
   消费方 `RewardSystemBehavior.cs:16210-16213` 同一值进入giver/receiver事实后HUD。
   此消费方由E持有；D先提供display方法与精确接线建议，主线协调E完成显示消费接线。
   若须变更共享内部端口，不直接改接口文件；主线先集成，再通知E接线。
6. 百科稳定度哨兵、无派系显示等需按生产/消费成对核实，不翻逻辑匹配token单边。

## 验证

- 资源契约、真实DLL分别 `ResourceName afdip_ui_strings`、`afkingdom_ui_strings`。
- 回放/静态断言：有键通知、新的动态参数、无键旧回执、失败/成功信息完整，
  FactionName/FormatDemand/Notes/MechanicalResult和存档键保持原值。
- 不降断言或更新全仓hash门禁。编译型测试NOT-RUN并交主线；不构建游戏。
- 每职责单元focused commit，不触碰E经济/任务代码、B资产或其他tracked dirty。

退出门：已确认UI全落双语、中文动态尾巴逐项处置、跨包消费接线闭环或明确待主线，
资源和离线检查完成，未知旧内容/原始异常/内部协议限制如实登记。
