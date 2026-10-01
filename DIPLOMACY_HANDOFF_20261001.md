# AnimusForge 外交重构交接

交接时间：2026-10-01 03:55（Asia/Shanghai）。用户明确要求写 handoff 并换对话，因此本文件是本次授权的交接材料。不要据此恢复日常 ledger／状态日记维护。

## 目标与当前结论

用户要求：仔细审查外交重构全部缺口，直接补完不符合或未完成的标准，持续到审查结果为重构完成。目标是 skill 的 R1–R9 以及图中的结构：

AF（三渠道、LLM、prompt、标签、记忆、主线程与请求生命周期）→ 薄 Bridge → 唯一 DiplomacyModule → Conversation / World Simulation → Application → 纯 Domain、Persistence、Bannerlord Adapter、Presentation。

以真实源码、真实调用、状态写入与测试为准，不接受只搬目录、回调隐藏旧 owner、只看提交说明的“完成”。用户允许**代码已符合而只缺 LIVE／SAVE 验收**的阶段算代码完成，但不能把未做的游戏／真实存档验收说成已通过。

**还没完成。** DPL-208～211 已提交。DPL-212 有未提交实现；末次独立复审修正之后尚未重新跑完检查。随后还有严格的高频性能缺口和最终全量回归／R8 门禁。不要开始新一轮漫无边际的风格整理，也不要假称当前已完成。

## 唯一工作目录与安全边界

- 工作树：`E:\Mount-Blade-Bannerlord-AnimusForge-mod-main\.wt\diplomacy-latest-20260925`
- 分支：`codex/diplomacy-refactor-20260925`
- 当前 HEAD：`b38a65af3d5e499d6f5db45c962fb2ddad81e396`，DPL-212 空 intent/checkpoint。
- 主会话 cwd 是仓库根目录，不是上述工作树。**所有命令显式指定目标 workdir。**
- 最后 `git diff --check` 干净。当前源码及测试 dirty 全是本会话 DPL-212 改动；另本交接文件是新文件。不要还原。
- 之前用户授权的远端 fetch-all 已完成；不要把 fetch 当 pull/merge。未推送、未部署、未覆盖游戏、未加载真实存档。
- Git 写权限此前用 `require_escalated`，中文说明仅本地提交。AGENTS 要求实质切片之前 intent commit，验证之后切片 commit；不 amend/reset，不重写历史。
- 不碰其他 worktree，不按 AGENTS 历史 G:/F: 标签切换目录；当前显式用户路径优先。
- 不修改一键编译/覆盖流程、模块加载布局、程序集名或 Bootstrap。
- 之前遵循外交 skill 的不写 ledger/状态日记规则；这次仅因用户明确要求写本 handoff。继续工作时不再更新无关账本。
- 已有只读审查 agent 在换对话时停止：execution_audit 与 ownership_audit 状态 interrupted，persistence_audit completed。它们没有写代码。不要假定新会话能恢复这些 agent。

## 必须读的规则

根仓库路径下（不是工作树中的过期副本）：

- `.agents/skills/animusforge-diplomacy-refactor/SKILL.md`
- 其 `references/architecture-and-parity.md`、`references/phases.md`

已应用目标工作树 AGENTS、maintainer、af-core-framework 与协作规则；已有会话也读过：

- `docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md`
- `docs/directive_tag_output_case.md`
- `docs/free_conversation_scene_shout_alignment.md`

兼容源码须支持 BannerlordApi=1.3 和 1.4。三渠道单一执行与事实/历史写入不能拆成第二套。

性能规则有一条不能忽略的硬标准（刚再次从 skill 核实）：

> No full kingdom, hero, document, job, or history scan on per-frame/high-frequency ticks.

因此不能仅凭“Jobs 很少”宣布逐帧全表检查符合要求。实际 job 队列上限是 **24 加压缩任务保留位**，并非绝对 24。

## 已提交切片

| 提交 | 内容及已知验证 |
|---|---|
| `a5cc2fc6` 及更早 DPL-201～207 | 已修编译导入、15/21/28 轮长、court delivery MaxValue；纯 authority/pressure；历史 owner；单一执行；口头 prompt/memory bridge；宣战效果读回；威胁/压力效果移出 Domain。更早全量 21 suite 6165 assertions；架构 2558+8 mutations、ports 285+4 mutations、双配置双版本+Bootstrap。该数字不是当前 HEAD 的最终全量结果。 |
| `2c467fb4` DPL-208；`e6b55cec` 空白修复 | 从 Domain 移出 ResultSlot、OfferCooldown、LlmMessage、JobPreparation、Prestige recovery 等隐藏编排回调；修复 cooldown 替换列表时丢后续项。round 3315、intent 1314、compression 306、result 537、架构 2567+8、Debug 双版本+Bootstrap。 |
| `12e824cb` DPL-209 | 游戏 action 尝试一次后读实际状态；区分 known/applied/complete。六口头 action、world offer、cession、initial peace、声望关系对账及 canonical partial-result 修复。新增实际 adapter 绑定的 `DiplomacyEffectReceipts.SmokeTests`。 |
| `a19e5a72` DPL-210 | 边境/宫廷/关系画像与 clan eligibility 归纯 `WorldDiplomacyWorldProfileRules`；三种 lifecycle 恢复顺序归 `WorldDiplomacyLifecycleApplication`。修正独立审查指出的宫廷冷缓存新增地图扫描与 prestige attempted 数量。round 3347、intent 1312、ports 289+4、架构 2600+8、Debug 双版本+Bootstrap。 |
| `a1db20a7` DPL-211 | Reward 政治标签走 ConversationBridge→Module→PoliticalRewardApplication→能力 port；终端/百科/时间线走 PresentationBridge；最近和平保护状态归 Application，45 秒规则归 Domain；外部 owner AST 门禁扩到整个模块顶层类型。最终 ports 351+4、架构 2626（另外新增了 owner mutation，日志仍硬编码写 8）、timeline 96、真实 receipt 131、J12 三项、Debug 双版本+Bootstrap通过。 |
| `b38a65af` | DPL-212 空 checkpoint，当前 HEAD。 |

### DPL-209 必须保留的关键语义

- `Refactor/Adapters/DiplomacyEffectReadback.cs`：只调用 action 一次；action 抛异常也读结果；读失败是未知，不是 false。
- `DiplomacyPeaceTermsService.ApplyPeace/ConfirmPeace`：分别确认 PeaceKnown/Applied 与 TermsKnown/Match。实际贡金用 **带符号** `stance.GetDailyTributeToPay(payer)`，实际期数用 `DailyTributeInstallments`；不是 remaining payment count。两个游戏版本均已编译。
- 口头议和状态枚举只追加 `PartiallyApplied = 15`，没有改旧值。部分和平应如实通知且不冒充条款全部履行。
- World offer 状态区分 Applied/Partial/Failed/Unknown，未知不得记录“未生效”或自动重放。
- canonical external fact 使用实际 MechanicalResult/Body，不丢掉部分成功限定。
- prestige 原存储条目只新增可选 `pendingEffect`（before/expectedAfter，NullValueHandling.Ignore）；未知读回保留 pending，下次读到 before/expectedAfter 再恢复，第三种状态不盲重试。没有改旧字段、保存 key。
- `BannerlordApiCompat.TryGetTradeAgreementState` 不吞掉“是否已知”信息；老 HasTradeAgreement 兼容方法仍存在，但不得拿它的 false 当效果确认。

### DPL-211 已修的迁移陷阱

- 原四条政治标签 regex、先支持后不支持、vassalage 再 annexation、两种独立 success flag、effect→诊断→双方 facts→展示→AF 单一 history writer 的次序保留。
- `DiplomacyPoliticalRewardPort` 延迟解析 Hero：普通无标签回复零查找，每个 port 有标签最多解析两个人一次；player 比较必须非 null。真实 adapter 的三项回放已加，不只是 fake。
- `CampaignObjectManager.Find<T>` 底层是线性遍历；之前代码注释说 index 不准确。不要在最终结论中宣称其 O(1)。有机会仅修正注释。
- 时间线已读入口保留 Trim 和大小写不敏感的 `diplomacy:` 前缀剥离；`WorldDiplomacyTimelineApplication.MarkTimelineRead` 是唯一规范化 owner，旧 command adapter 只是转发。查询/已读异常回退保留。
- `DiplomacyFactionSnapshot` 只 catch MapFaction 获取；映射后 StringId 读取异常不偷偷换回原 faction。与原保护语义一致。
- 近期和平：45 秒包含端点；时钟倒退不错误过期；登记刷新同 pair；query 只索引目标 pair，不再每次扫字典；低频 register 才按 45 秒门控 sweep。

## 当前未提交 DPL-212：做了什么

### 新文件

- `Refactor/Domain/WorldDiplomacyEventRules.cs`
- `Refactor/Domain/WorldDiplomacyGeographyRules.cs`
- `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyNativeDecisionApplication.cs`
- 同目录 `WorldDiplomacyBattleApplication.cs`、`WorldDiplomacyGeographyApplication.cs`
- `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.NativeDecisionPort.cs`
- 同目录 `WorldDiplomacyBehavior.BattlePort.cs`、`WorldDiplomacyBehavior.GeographyPort.cs`
- `tools/DiplomacyEffectReceipts.SmokeTests/ImmediateReplay.cs`
- `tools/WorldDiplomacyRoundLifecycle.SmokeTests/RetainedHostRulesReplay.cs`

### 已有文件改动

- `WorldDiplomacyImmediateActionApplication.cs` receipt 新增 Known；Applied 必须 known && applied。
- `WorldDiplomacyBehavior.ImmediateActionPort.cs` 三种即时动作统一只执行一次+读回；cancel trade 采用 TryGetTradeAgreementState；未知文案是“无法确认”。
- `WorldDiplomacyBehavior.cs` 的 OnMapEventEnded、CaptureNativeDiplomacyDecision、RemoveQueuedNativeDiplomacyDecisions 变薄转发；18/24/32 硬期限转 Domain；删除 24/42 native signal 常量。
- `WorldDiplomacyBehavior.OrchestrationHost.cs` 的 native signal weight 转 Domain，传播 snapshot 方法转 GeographyApplication。
- `WorldDiplomacyBehavior.PublicationPort.cs` 变单一 GeographyApplication 转发。
- 架构测试新增禁止事件处理器长回业务／传播适配器长回筛选的门禁；Python whitelist 和 forwarder 清单同步。
- EffectReceipts 项目链接真实 ImmediateActionPort + Application，Engine fault 增加 NoOpUnreadable / BeforeUnreadable；WorldPortHarness 补周边 engine stub。
- RoundLifecycle 项目链接新增 rules/app 和 `WorldDiplomacyDocumentReadCommandContracts.cs`。**后一个是 DPL-211 新 TimelineApplication API 带来的测试项目依赖，当前只在 DPL-212 dirty 中补齐，不要遗漏。**

### DPL-212 要保住的行为

1. Native queue：逐 kingdom、逐决议，capture→record→remove；capture 返回 false 仍删除；capture 抛异常才跳过此项删除；remove 异常不阻断后续项。玩家收到 peace/alliance/trade 需反转 source/target；war 不反转。
2. Battle：无赢家/hideout 不扫描双方；双方各自必须有对方没有的 kingdom 才抓详情并记录。stable key 顺序与旧规则一致。
3. Geography：初次传播排除 hideout、空 ID、灭亡王国、作者宫廷；未知 court distance 用最大值。重算包含作者宫廷，保留 hideout 的 distance lookup，但 maxCivilianDistance 排除 hideout；重复 settlement ID lookup 保留第一项。
4. Immediate：NoOp/Before/After × unreadable 必须区分。未知不写 ChangedDiplomaticState=true、不清战争压力、不记 LastOffensiveWarDay。

### 末次独立复审修正（尚未重新跑检查）

execution_audit 指出两个 parity/性能问题，根代理已经改了：

- NativeDecisionPort.Reason 原先按 ID 全局重新 ResolveKingdom，会新增扫描且可能换掉被捕获的真实对象。现在 `parties` 保存每个 token 的原始 proposer/target 引用，Reason 接收 `bool incomingPlayerOffer` 并从 token 选择原始引用；Domain `IsIncomingPlayerOffer` 决定反转；测试 fake signature 已同步。
- GeographyPort 原先初次传播也读取 hideout/空 ID 的 GatePosition。现在 Settlements 仅返回 raw metadata + Index，Application 先筛选，再通过 `SettlementDistance(index)` 读取；重算仍读有效 ID 的 hideout。新增 `RetainedHostRulesReplay` 检查初次只读取索引 0、1。

**本交接之前最后一次源码修改就是以上两项及 fake/test 同步。最后 `git diff --check` 干净，但不能把之前绿灯算成这组修改后的绿灯。**

### DPL-212 已有测试证据（末次修正之前）

日志都在目标工作树 `artifacts/independent-diplomacy-audit-a6c89ea3/`，这是 ignored 本地 evidence，不要 stage：

- `dpl212-debug.log`：Debug 1.3/1.4+Bootstrap成功；在 GeographyPort 初版之后、末次两项修正之前。
- `dpl212-WorldDiplomacyRoundLifecycle.SmokeTests.log`：3366 assertions。
- `dpl212-WorldDiplomacyIntentBoundary.SmokeTests.log`：1312。
- `dpl212-DiplomacyEffectReceipts.SmokeTests.log`：182。
- `dpl212-architecture.log`：157 个纯源码独立编译，1163 production files，两预处理分支，2654 assertions；该程序日志硬编码“8 rejected mutations”，实际后来又添加了检查，最终应修正统计描述。
- `dpl212-round.log` 是更早一次缺 DocumentReadResult 链接的失败日志，已加项目引用并由后续 round log 通过。别把旧失败误判为还没修。
- 没有已知仍在运行的 root unified exec session；最近 52749/49680/69081 都已取回完成。用户中断仍要求新会话自行核验，不无脑再启动重复构建。

## 接下来按这个顺序继续

1. 核实 Git，读取本 handoff 与 skill，不切目录、不覆盖 dirty。复查末次 Native/Geography 修改，跑受影响 round、intent、effect receipts、architecture 和 module ports，并重新做双版本 Debug。修到绿后本地提交 DPL-212（已有 checkpoint，无须重复）。
2. 做有限的高频性能收尾，先空 intent commit。**性能工作未实现，不应标完成。** 见下节。
3. 补 R8 完整 retained-host 分类门禁与 inverse/caller 证据，完成独立最终审查。不要只看老门禁已登记的方法。
4. 运行最终全量 22 smoke suites + architecture + module ports + J12；Debug、Release 的 1.3/1.4+Bootstrap。所有证据必须匹配最终源码。
5. 提交已验证切片，检查无意外 dirty，最终按 R1–R9 给代码完成结论与验证限制；不以尚未 LIVE/SAVE 为唯一代码阻塞，不声称做过游戏验收。

## 待完成：高频性能（重要）

只读审查最新结论：

- 不能为省几十个 scalar 读就引入一个覆盖全模块的 shared revision/cache。
- 但 skill 明文禁止逐帧/高频全 job/document scan，故仅单 pass 替换 LINQ 还不足以严格宣布完成。
- 当前 `WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression` 每 tick 在 ensureInitialized/syncSources 后 Any awaiting、Any compress、Where/Min target；需要任务视图在变更时刷新或具有可靠独立 revision，不改 source sync 的原小时门控与先后顺序。
- 当前 `WorldDiplomacyLlmDispatchApplication.SelectAndPrepareLlmJob` 每次构造 runnable List、Max、OrderBy/ThenBy/First，预算耗尽也会继续进来。候选排序是 priority 降序→与 trim 后 lastCacheAffinityKey 匹配优先→CreatedDay 升序→JobId OrdinalIgnoreCase 升序→完全相等稳定保留原序。
- **不得缓存整个 SelectAndPrepareLlmJob 返回值/资格结果。** 配置、当前权限、legal action signature、day/hour、cooldown、发送前 history/prompt/预算都要保持即时验证；只能缓存候选和统计。
- rumor 当前已经 bounded Top3，但仍逐秒扫描 archive；formal notice 仍 `OrderDocumentsChronologically(...).Take(3)` 全排序。Skill 的高频条款意味着应设计可靠 pending view/revision，不用 Count-only 或全量 hash 假装缓存。
- Documents 保留上限约 420；通知 UTC 每秒、map-view/reset 与显示失败语义不可改。disabled 通知也会消费已读但未 shown 的文书，不能缓存时预先排除 IsRead。

可参考的失效入口清单（上一轮已完整枚举，但实现前核对当前行号）：

**Jobs 集合**：Orchestration.EnqueueJob、RemoveJob、EnqueueCompressionJob；RoundApplication.Disable 清空；BeginOrExtendRoundResultSettlement 移除；NormalizeStorage/migration；SyncData 换 storage、新游戏/reset。

**Jobs 原位字段**：CompletionApplication 的 IsRunning=false/ServiceCooldown；JobPreparation rebuild/refresh 时 IsRunning/CacheAffinity；LlmDispatch IsRunning、awaiting、InputBudgetHistoryTargetTokens；HistoryCompression commit 清 awaiting/retry；ResetRuntime 清 running；LastCacheAffinityKey 清理和设置。候选依赖 JobId/IsRunning/Awaiting/Priority/CreatedDay/CacheAffinityKey/Kind/AuthorKingdomId；统计还依赖 target tokens。不能每帧无条件 invalidate，只在实际 mutation/完成事件冷边界。

**History/time**：currentHour 与 retry/service deadline 应直接作为决策输入；边界 equality 可恢复，倒退/load 同小时不能失效遗漏。canonical history 的 Revision/EstimatedTokens/NextSequence/Snapshot coverage 及配置 trigger/target 要考虑；已有 InvalidateCanonicalHistoryRenderCache 是状态更新冷边界，但不要把 source sync 关掉。

**Documents**：AddDocument 同 ID 替换/retention 换列表；SuppressInvalid 删除；normalize/notification migration；ProcessAnalyzedDocument ready；Finalize/PublishPlayerImmediately/StartPropagation；court arrival/relay/RecoverCourtKnowledge；GeneratedCompletion 改 day；notification formal/read/rumor flag、自身 notified set/reset；PresentationQueries.MarkRead 经 TimelineApplication 与 Behavior.Presentation 两条入口。候选依赖 DocumentId、IsPlayerAuthored、IsReadyForPublication、RumorNotified、HasReachedPlayerCourt、IsRead、FormalNoticeShown、Day、CreatedUtcTicks。

**性能验收应包含**：同 count 替换、原位资格/affinity、hour 边界/倒退、compression 解锁、court/read/ready 变更、disabled 通知、view reopen、通知失败、load 同 count/hour；idle 1000 tick / 100 poll 只构建一次视图；变更后与基线选择同一对象和顺序。不要增加全 archive/job hash 当 cache key。

## 待完成：R8 retained-host 分类与最终审查

上一轮只读审查读完全部 26 个 WorldDiplomacyBehavior 文件与 Direct 两文件，发现的真正结构残余就是 DPL-212 五项：native queue、battle admission、propagation filtering、两个 scalar rules、immediate unknown receipt。其余没有发现另一个完整业务用例藏在 Behavior。

保留职责概览（需变成明确可执行清单/门禁，而不是写新进度表）：

- lifecycle：构造/RegisterEvents/SyncData、campaign/engine hooks、游戏事件转换、ResetTransientRuntime、Harmony注册；CampaignSource/CompletionSource/LifecycleSource/TickSource；LlmDispatchSource 的 lease、后台启动和完成队列交接。
- identity/context：ActionSelectionPort、AnalysisPort、DocumentExecutionPort 的读数、HistoryCapturePort、JobPreparationPort、NoActionPort、PeaceAdmissionPort、PromptWorld、WarAdmissionPort、ThreatBindingPort；StorageNormalizationSource 的事实/配置；主类 Resolve/Capture、世界/战争/亲属/人物读数、旧政策分页、已知文书查询。
- presentation：NotificationWorld、PresentationWorld 的 read model/有效性 guard、日期姓名/百科/公告格式、原版按钮禁用、map notification 注册、compose popup、diagnostics。
- compat-forwarder：仍保留的旧外部 static 方法和 wrapper，必须只有转发；PresentationWorld.Submit/MarkRead、旧 normalization wrapper 同理。
- main-thread effect：RunDiplomaticAction、TryApplyValidatedCession、Immediate/Offer/InitialPeace action ports、Prestige ChangeRelationAndMeasure、Threat ChangeRelation/CancelPolicy、通知展示 sink。
- Direct：module/Harmony/lifecycle；prompt postfix 是渠道适配；Actions 七个执行方法进 Application；身份读取是 adapter；JobRuntime partial 当前空壳，无第二调度器。

下一会话应枚举 retained 方法，按上述分类登记明确成员/类例外，拒绝新增未分类成员与换名后的业务回流。之前要求 execution_audit 生成准确方法清单时被用户换会话中断，**该完整成员清单还没交付或写入文件。**

外部绕行独立扫描结果：已计划且 DPL-211 修完的 Reward/Terminal/Encyclopedia/Timeline/RecentPeace 之外，没有发现额外真实 module concrete owner caller。合法 DTO 包括 WorldDiplomacyTimelineDocument/TimelineCountryReference、AfTributePowerContext、WorldDiplomacyPolicySignalSnapshot、PublishedPolicyArtifactLedgerEntry、DiplomacyPeaceEffectReceipt。WorldDiplomacyPolicyContext 是政策侧 source/cache，不能误杀。

现有架构 AST 已收集 module 顶层类型，禁止外部直接依赖；不要收集嵌套通用类名（WorkItem/Parties/CompletionSource 等会误报）。仍需复核 Refactor owner 的具体豁免是否太宽。当前两个 bridge 例外是 ModuleServices 的 DiplomacyModule 装配，RecentPeaceBridge 的 DiplomacyFactionSnapshot 技术身份转换。

## 测试运行方式：绝对不要弹崩溃窗口

用户已经明确抱怨 `WorldDiplomacyIntentBoundary.SmokeTests.exe` 弹 Windows 应用程序错误窗口。

**禁止 `dotnet run`，禁止直接执行任何 smoke EXE。** 用已准备的反射 DLL host 捕获异常并抑制 WER：

`artifacts/independent-diplomacy-audit-a6c89ea3/TestHost/bin/Debug/net8.0/TestHost.dll`

host 源码在同一 TestHost/Program.cs，设置 SetErrorMode(0x8003)，依赖 resolver 与 AppContext 目标目录；支持 Task/Environment.ExitCode。普通 suite 是 net6.0，PolicyHistory 是 net8.0。

```powershell
$env:APPDATA = Join-Path $PWD 'artifacts/independent-diplomacy-audit-a6c89ea3/appdata'
dotnet build tools/WorldDiplomacyRoundLifecycle.SmokeTests --no-restore -v:q *> artifacts/independent-diplomacy-audit-a6c89ea3/build.log
if ($LASTEXITCODE -eq 0) {
    dotnet artifacts/independent-diplomacy-audit-a6c89ea3/TestHost/bin/Debug/net8.0/TestHost.dll tools/WorldDiplomacyRoundLifecycle.SmokeTests/bin/Debug/net6.0/WorldDiplomacyRoundLifecycle.SmokeTests.dll *> artifacts/independent-diplomacy-audit-a6c89ea3/test.log
}
```

保留真实退出码再读日志，或核对日志明确 PASS；`Get-Content` 成功不能证明前面的测试成功。

Architecture/ModulePort 自带 run.py 内部会 dotnet run，**不要直接运行**。安全 wrapper 已把内部调用替换为 build --no-restore Release 后经 TestHost 执行 DLL：

```powershell
& 'C:/Users/klfwdf/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' artifacts/independent-diplomacy-audit-a6c89ea3/safe-checks.py DiplomacyArchitectureTests
& 'C:/Users/klfwdf/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' artifacts/independent-diplomacy-audit-a6c89ea3/safe-checks.py DiplomacyModulePortTests
```

J12 Python 源契约可直接运行 `tools/J12DomainOwnerSourceContractTests/test_j12_domain_owners.py`。其中旧 JobRuntime/source 断言已改为真实 Orchestration/Application，不要还原成旧 owner。

最终 22 suites = 当前 tools 下 21 个 `WorldDiplomacy*.SmokeTests` 加 `DiplomacyEffectReceipts.SmokeTests`；先枚举核实项目/TargetFramework，再顺序 build+safe host。不要借机跑无关 EXE。原来的 6165 总数是旧证据，最终重新计数。

### 本地双版本构建（不是部署）

```powershell
$env:APPDATA = Join-Path $PWD 'artifacts/independent-diplomacy-audit-a6c89ea3/appdata'
$env:RestoreConfigFile = Join-Path $PWD 'tools/InternalModuleDirectoryTests/NuGet.Config'
$env:NuGetAudit = 'false'
& './一键编译覆盖推送/build_single_module.ps1' -ProjectRoot $PWD `
  -BannerlordRoot 'E:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord' `
  -WorkshopContentDir 'E:/SteamLibrary/steamapps/workshop/content/261550' `
  -HarmonyCorePath (Join-Path $PWD '.tmp/build_check/1.4/0Harmony.dll') `
  -Configuration Debug
```

最终再用 `-Configuration Release`。当前安装 refs 为 1.3.15/1.4.6，源码预处理配置仍 1.3/1.4。只用 build_single_module，不运行 stage/deploy/cover 脚本。此前全部成功产物在本 worktree bin。

## 操作细节

- Windows Python 默认编码可能 GBK；读写明确 utf-8/utf-8-sig。某些原文件混合 CRLF，git diff --check 曾将整行 CR 当尾随空白；只对受影响文件保留/规范正确行尾，别全库格式化。
- `rg` 先列真实目录/文件，别把 `tools/WorldDiplomacy*.SmokeTests` 当现有路径传入。部分 ignore 规则会漏文件，必要时对已知文件或 production manifest 检索。
- 不要把脚本临时 helper（artifacts 中 dpl211/dpl212-*.py）重跑；它们是一次性文本替换，重复执行会断言失败或重复插入。直接继续改当前源码。
- 保留原存档 key `_af_world_diplomacy_v1`、旧 DTO 名/字段/枚举值、module DLL 标识与三渠道写入主权。
- 用户很不喜欢反复确认；必要工作继续自治完成。换对话本次只交接，不在当前对话继续实现。
