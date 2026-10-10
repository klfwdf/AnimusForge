# Package E：经济、RP制造与任务结果显示

阶段：待接力。先读[共同规则与文件边界](2026-10-10-localization-cde-plan.md)。
用户已把世界书交B；本包替代旧E英文资产计划，只做经济/任务源码与显示资源。
任务：完成本包剩余玩家可见中文的中英显示，修复动态尾巴漏译或丢失原信息。
运行时中文外置为最终目标；本包不能用Unicode转义、删除信息或更改协议制造清零。

## 独占范围

- `src/modules/AF.Module.Economy/` 显示与必要接线，重点：
  `Host/EconomyUiTexts.cs`、`Host/RewardSystemBehavior.cs`、
  `Host/RewardSystemBehavior.PlayerRpCrafting.cs`。允许新增小型显示目录。
- `src/modules/AF.Module.Issue/` 显示与必要接线，重点：
  `Host/VanillaIssueOfferBridge.cs`、`Actions/IssueActionOwner.cs`。
- `content/modules/AF.Module.Economy/ModuleData/Languages/afeconomy_strings.xml`
  与 `CNs/afeconomy_strings-zh-CN.xml`。
- Issue复用的 `content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml`
  与 `CNs/afconv_ui_strings-zh-CN.xml`；本轮C/D/主线不写此对资源。
- `tests/modules/AF.Module.Economy/` 的受影响专项；Issue专项仅
  `tests/replay/PhaseEightParityReplayTests/IssueActionOwnerReplay.cs`、
  `IssueAlternativeDispatchOwnerReplay.cs`、`IssueCompletionReceiptOwnerReplay.cs`、
  `IssueRuntimeStateOwnerReplay.cs`，不要改共享runner/项目/其他Replay。
- 独立交付 `docs/handoffs/2026-10-10-localization-package-e-economy-issue-delivery.md`。
- 不写世界书、默认JSON/Prompt、C的Policy、D的Diplomacy/CivilWar、
  GameAdapter/Terminal、Memory、WorldMap、共享注册/接口/门禁、打包脚本。
  `RewardSystemBehavior.cs` 虽大，也由E独占；其他包给精确接线建议，不抢文件。

## 已核实缺口与顺序

以下行号绑定 `33a9ba44f` 工作树，接手时重新核实符号，不能靠旧行号直接替换。

1. `VanillaIssueOfferBridge.cs:699` 的“静默原版任务交付异常”赋给out error，
   `IssueActionOwner.cs:303` 直接ShowInfo，属于实际上屏内容。
   同样检查原版任务探测/接取/收尾原因的返回值和实际消费者。
2. `RewardSystemBehavior.PlayerRpCrafting.cs:740` 的锻造等级/概率变更拒绝、
   refund/rollback错误仍中文，Terminal实际显示该out error。
   保留确认票据、等级/概率重验、退款/回滚行为；通过显示端口分流，
   Terminal接线给主线。不能为了英化跳过安全检查。
3. `EconomyUiTexts.FactionIdentityStatusForDisplay:639` 仍缺无玩家家族、退出方不符、
   无效ID、未知serviceType、族长异常等生产状态；逐分支核对失败原因，不统一成泛化失败。
4. `EconomyUiTexts.cs:626` 王位成功regex吞 `transitionSummary`；
   `:735/:741/:747` 家族归附吞 `carriedFiefSummary/transitionNotes`。
   当前中文显示也会丢细节。保留并双语显示领地、旧王国修复/解散、执政转换与异常信息，
   不用一句成功摘要替代完整结果；尽量复用结构化动作结果，避免贪婪regex吞字段。
5. `RewardSystemBehavior.cs:16209-16213` CivilWar返回值先进入giver/receiverFacts后HUD。
   D持有CivilWar生产者并提供独立显示能力；主线协调共享端口，E只改HUD消费，
   原事实/历史/存档文本保持。没有已集成端口时登记依赖，不虚构方法名造成编译回归。
6. 复审本包所有Inquiry/ShowInfo/DisplayMessage/out error实际UI路径，
   包括交易、债务、资产与RP制造结果；模型正文、内部匹配和事实文本单独登记。
   未知旧回执或原始异常保留真实原文，并明确这不等于全英文已完成。

更多原证据见[第二轮B世界地图/经济复审](2026-10-10-localization-wave2-b-worldmap-review-fix.md)。

## 契约与性能

- 既有业务结果用于三渠道、AFEF、存档时，保留原语义与写入；只在UI边界解析显示。
  内部匹配中文后续外置需与生产者/消费者同步，不能仅翻比较值或动作标签。
- 本包不改给予/债务/Issue标签、金额、资产ID、任务奖励调用、Action顺序或存档类型/键。
- 中文文本放CN资源，代码保持英文fallback，变量、换行与数值格式不变。
  显示解析只在发布/弹窗/状态变更边界，不新增Tick扫描、重复反射或后台游戏对象读取。

## 验证与交付

- 资源对键集、唯一ID、变量及英文fallback一致；两对均已注册，新增键无需抢共享注册文件。
  若新增资源对，交主线精确注册建议，未接线不报完成。
- 原 `GiveAssetTagCodec.StressTests/Program.cs:499` 按中文锁概率变更错误；
  允许维护双语显示断言，保留错误触发和拒绝执行的行为断言，不删安全oracle。
- 对失败原因、带领地/转换详情成功结果、退款/回滚、Issue失败/奖励一次、
  CivilWar事实/显示分流建立或维护必要专项证据；禁止仅以0中文字符替代行为验证。
- 运行纯脚本/资源解析/静态检查；编译型回放只prepare或标NOT-RUN。
  不构建模组、不Stage、不部署、不正式打包、不push、不刷新共享hash基线。
- 每职责单元focused commit；交付SHA、真实路径/符号/行范围、键数、剩余中文分类、
  PASS/FAIL/NOT-RUN、D与主线待接线。不得夹带未跟踪EN资产或其他作者dirty。

退出门：所有确认玩家可见路径双语完整显示、原功能与事实不变、动态详情不丢，
跨包依赖已闭环或明确未完成，资源/离线证据与focused提交齐全。
