# AF 阶段八整体接续与验证结果

日期：2026-09-06。工作区：`G:\AFMOD\AF-REFACTOR`。分支：`codex/af-main-refactor-continuation-20260831`。
整体工作基线 `97515f3f`，意图 checkpoint `e44de75d`。本记录统一汇总本轮实现，不以多个小切片作为交付终点。

## 推送接续说明

本文件的实现/构建/回放证据绑定代码提交 `fea254c3`（以及前序已记录提交），不覆盖之后其他成员新增的代码。用户已于2026-09-06授权上传代码、本文及制作组简报；新的fetch发现共享远端已到 `8f1fa8db`，包含未纳入本次验证的终端设置/API引导提交。最终推送目标与合并状态见同目录 `2026-09-06-github-handoff.md`；下文“未推送”为实现完成时的历史快照，不是最终发布结果。

## 结论

本轮已统一完成确认缺陷修复、真实死代码清理与相关离线/生产程序集回归。**尚未达成“阶段八整体 DONE / 全部旧实现删除 / 零 BUG”。** 原因不是仅缺一轮人工测试：当前还有实质的三渠道默认迁移和规则等价缺口。保留这些活跃实现是为了不破坏现有玩法，不是保留废代码。

认可制作组既有实测基线：9 月 4 日 intake 绑定 `c01a2fcc`、游戏 `v1.4.8.119303`，19 域与旧档往返报告 PASS；用户 9 月 6 日再次确认其他成员已验收。该结果不自动覆盖随后新加或本轮修改的 DLL；本轮未启动游戏、未部署、未操作真实存档、未推送，自动化仍关闭。

## 已实现的整体修复

| 领域 | 原问题 | 现在的路径与边界 |
|---|---|---|
| 终端查询/臣属/标签 | 截断、无法选择、缺详情、旧列表重复 | 前序已统一为全量分页/搜索/精确身份、详情返回、快照导出；本次完整回归保留 |
| 周报显示 | XML 绑定不存在的 SummaryText，正文/生成按钮等缺失 | 使用实际 BodyText，显示完整正文、日期/周次、稳定度标签、国家选择/数量/空状态；保留查询后的返回位置 |
| 完整周报生成 | await 后直接改 live 记录和 UI，旧档结果可迟到写回 | 主线程捕获请求；HTTP 后仅排完成任务；engine tick 每次最多处理2项，复核 owner/代际/eventId/原素材，拒绝迟到或重复覆盖；reset 完成等待任务，不挂起 |
| 周报 UI 生命周期 | 后台 continuation 刷新已关闭窗口 | UI tick 观察任务完成；失败恢复重试，关闭/owner替换/旧世代不更新窗口 |
| 终端外部功能 | 根终端暂停/焦点残留，子窗口交接不完整 | 政策、外交、随行、数据库、引导等交接前释放根终端；返回路由复用现有统一菜单 |
| 战争备用入口 | 独立 popup 引用已失效 movie，删除会损坏 fallback | 复用统一终端的 war-only 页面；终端 Behavior 不存在时由全局地图层维护 Tick/关闭；打开失败清理已加层 |
| RAG Gateway | 关会话 Bridge 会误关仍启用的知识 Bridge | 共享传输绑定实际 owner；知识和会话两开关独立，不放宽禁用域的凭据/网络边界；保留 public constructor ABI |
| 部署门禁 | 只查 DLLName，错误模块 Id/class 仍 PASS | 核对唯一 Id=AnimusForge、容器/子模块、Bootstrap DLL 与实际 BootstrapSubModule 类；保留前序三 DLL 哈希和显式 game-root |
| 入口清单 | 宽泛排除 terminal，WarStats/新终端未登记仍 PASS | 纳入25个域—路径关联；新候选只触发相关域重新入口复核，不自动获取 owner/LIVE 批准 |

## 删除与保留

删除独立周报 Popup 类及 XML、同名失效 XML 备份、独立战争 Popup 类、旧公告 inquiry 选择树；统一终端已经承接功能与 fallback，真实周报/战争 VM 保留。

另删除 MyBehavior / ShoutBehavior 中 **29 个无生产入口的 private 方法**，包括旧单域后处理及其递归孤儿 helper。真实 Native/Scene 统一后处理和 active normalizer 不删除。Duel 原业务断言仍保留，并改测真实共享入口；仅移除对死方法的重复测试。

仍保留：`LegacyChannelInteractionFacade`、`LegacyConfiguredChatGateway`、实际 ActionPlan/owner、Snapshot adapter、旧存档/JSON/MCM 兼容与 1.3/1.4 适配。它们承担真实调用、ABI 或数据兼容职责。原版参考树、第三方集成、玩家数据、ONNX、官方构建/覆盖脚本均未清理或修改。

## 统一验证

- 官方原脚本 Debug/Release × 1.3/1.4/Bootstrap 六项构建：全部 0 warning / 0 error，project-local Stage PASS。
- 引用版本：1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`。不能据此声称真实 1.4.8 已运行。
- PhaseEightParityReplay：真实 Debug 1.4 生产 DLL；终端、标签、周报国家/排序/12000+字符正文/标签/空状态/返回/失败恢复/关闭、XML绑定、旧弹窗类型删除、战争 owner归档/列表往返 PASS。
- WeeklyReportOwnerReplay：真实 owner 排队与执行线程、每tick限额、重复pump、读档前后stale、reset等待完成、异常与owner替换 PASS；不冒充游戏 IDataStore 或完整 HTTP→游戏 UI 实测。
- ProductionDuelOutcomeReplay：Debug 与 Release 各35项 PASS，分别覆盖真实1.3/1.4 Stage。仅源码/IL/owner回放，不是实际决斗。
- ProductionOptInEntry、ProductionDetachedHost、ProductionCourierHost、ProductionEconomyAwareCommit：全部 PASS；后者验证混合动作、部分及未知结果。Opt-in runner 自己仍明确 `noDefaultCutover=1`。
- InteractionPipelineContractTests 全套 PASS（含 detachedHostCommitBoundary 69、commit receipts 39）；Configured Gateway、Validation Gateway、Knowledge RAG、Bridge runtime 9情形 PASS。
- RAG 新的4种开关组合在旧实现上复现 `conversation=False, knowledge=True` 失败，修复后全部 PASS；禁用域不读凭据、不发请求。
- Persistence/Profile/Config：142 keys /168 bindings /43 symbolic sources PASS。全部既有 key/source/ref/type 多重集合保持不变，只同步50个源码行号；迁移10情形 PASS。
- 部署门禁：8个 unittest方法 PASS（15个入口身份负向、原6个DLL缺失/过期、5个XML负向）。
- PhaseEightReadiness 完整72测试 PASS；入口10测试 PASS，包含19个真实 CLI 缺入口负向。all-missing 仍正确 BLOCKED/exit2。
- XML parse、废弃符号引用、diff --check PASS。部分纯 .NET 回放记录 NU1900（NuGet漏洞数据库网络不可用）；业务断言通过不代表依赖漏洞审计通过。

主要日志都在被忽略的本机 `.tmp`：`phase8-integrated-build-Debug.log`、`phase8-integrated-build-Release.log`、`phase8-integrated-PhaseEightParityReplayTests.log`、`phase8-integrated-duel-Debug.log`、`phase8-integrated-duel-Release.log`、`phase8-rag-isolation-before.log`、`phase8-rag-isolation-after.log`；其他 runner 使用 `phase8-integrated-<runner>.log`。

## 没有被“通过测试”掩盖的剩余问题

1. `ShoutBehavior.SubmitNativeConversationRefactorOptInForExternalAsync`、`SubmitSceneShoutRefactorOptInForExternalAsync`、Courier reply/inbound 两个 Submit 均没有生产静态调用者。默认 Native、多人 Scene relay、Courier 仍走实际原 owner。不能删除这条活跃主流程。
2. `ShoutBehavior.cs` 的 detached Scene prompt（当前约17307）以 `knowledgeExtras/ruleBlock` 构造 postRules；现行统一后处理还有运行时 PostprocessRules、资格、库存/债务及归一化。直接把单NPC capture切成多人/三渠道默认会丢机制，不能复制另一渠道的规则“补齐”。
3. Native 默认流式显示、主动开场、阶段回调与多人Scene relay 尚无完整等价的 detached 接入。这是实现缺口，不是再跑一次 fixture 就能消除。
4. SubModule 仍真实 AddBehavior；8-module catalog 的入口仍 Pending，不能把20个逻辑owner/领域当作20个已迁入的新物理模块，也不为完成率添加空 ModuleHost。
5. 目录仍20个owner ASSIGNED；此次增加真实入口后，world/social/UI三域entryCoverage回到REPRESENTATIVE待复核，其余17域保留旧COMPLETE。候选登记PASS只证明所列候选一致，不证明全功能完成。

这些限制必须先由真实替代实现和对应回归解决，才能作全量删除、默认切换与发布结论。不要回滚到“只要文档写PASS就能删”的方式。

## 回滚/复现

回滚当前整体代码提交使用定向 git revert；整体变更前代码为 `97515f3f`，意图为 `e44de75d`。不 hard reset，不影响其他作者工作树。构建沿用 `docs/handoffs/2026-09-01-framework-continuation.md` 的本机显式引用路径，仅使用 `-Stage`。生产 replay 依赖参数仍见 `tools/ReplayDependencies/README.md`，不得 `--no-build` 绕过输入核对。

## 当前 Stage 二进制绑定

- `Debug/AnimusForge.Bootstrap.dll` SHA256 `088BA9D275DF2D478D53CCF3FC3D41AD8B66A2CE90F44E0D91F41306C5203B82`
- `Debug/versions/1.3/AnimusForge.dll` SHA256 `381789C6AE91F8950779EA3D200906491656F5E086B65AD818A72BE041C7661D`
- `Debug/versions/1.4/AnimusForge.dll` SHA256 `905F82122A05BA51A1455954C1758871C394E00C1DC92ADE8C61D7625CF99A68`
- `Release/AnimusForge.Bootstrap.dll` SHA256 `50C67C08E98C779A5C186BEBA7FE86A248B070F4E9439112058E758A9839FDB4`
- `Release/versions/1.3/AnimusForge.dll` SHA256 `1B8D72904E3986DE628DE088973485D91033C0A9BF750EACC5B4718E60E37814`
- `Release/versions/1.4/AnimusForge.dll` SHA256 `2212DBFE5764E972871CC8A59769F50F1DAE5AFAD4483E809CD7EB2ED77DDD63`

## 已删私有方法明细

- `MyBehavior.cs`：`BuildDuelPostprocessItemList`、`BuildPostprocessRuleText`、`BuildRewardPostprocessItemList`、`FindDuelStakeOptionByToken`、`FindRewardItemByToken`、`MergeNormalizedPostprocessBlocks`、`MergePostprocessRules`、`NormalizeDuelPostprocessTags`、`NormalizeKingdomServicePostprocessTags`、`NormalizePlayerNameForPostprocess`、`NormalizeRewardPostprocessTags`、`StripDuelActionTags`、`StripKingdomServiceActionTags`、`StripRewardActionTags`、`TranslateDuelStakeItemNames`、`TranslateRewardItemIndexes`、`TryRunDuelActionPostprocess`、`TryRunKingdomServiceActionPostprocess`、`TryRunLoanActionPostprocess`、`TryRunRewardActionPostprocess`、`TryRunTransactionActionPostprocess`。
- `ShoutBehavior.cs`：`StripKingdomServiceActionTagsForScene`、`StripRewardActionTagsForScene`、`TryRunSceneDuelActionPostprocess`、`TryRunSceneKingdomServiceActionPostprocess`、`TryRunSceneLoanActionPostprocess`、`TryRunSceneLordsHallAccessActionPostprocess`、`TryRunSceneRewardActionPostprocess`、`TryRunSceneTransactionActionPostprocess`。
