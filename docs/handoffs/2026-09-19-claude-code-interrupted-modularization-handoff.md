# Claude Code 中断后的 AF 模块化详细 HANDOFF

> GitHub更新：本文、J04/J05/J06a-c及J06d WIP checkpoint已按用户指定普通快进到[`codex/af-main-refactor-continuation-20260831`](https://github.com/klfwdf/AnimusForge/tree/codex/af-main-refactor-continuation-20260831)；首次正确目标远端核对`f6ddd56567fa0aa13fc585a3792d423c6dca8279`。WIP/NOT-RUN边界不因发布改变。


日期：2026-09-19。此文件是中断恢复与发布交接；详细执行状态继续以同仓库主台账为唯一权威入口。

## 1. 结论

- 真正的施工工作区不是旧 `G:/AFMOD/AF-REFACTOR`，而是 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`。
- 本地施工分支：`codex/af-modularize-j04-20260918`；GitHub：`https://github.com/klfwdf/AnimusForge`；权威发布目标：`origin/codex/af-main-refactor-continuation-20260831`。
- 基线：远端重构提交 `25a89cea9e1a45d5108f5f6680ad159ae54ff080`。Claude Code 已在其上完成29个本地提交，最后已提交切片为 `157dc7f21ec98f3cddff96d5548c470a526aacfb`（J06c）。
- 断开时有9个未提交生产/项目文件，内容属于 **J06d规则资格事实捕获**。本交接代理没有继续实现，只原样封存为 `bd2582aa776d9763002c85d75509ed9ccdb7b424`：`wip(prompt): checkpoint interrupted J06d eligibility capture`。
- **J04_OFFLINE_VERIFIED、J05_OFFLINE_VERIFIED；J06a/b/c代码已提交；J06父包未完成；J06d仍WIP/NOT_ACCEPTED。** 实机、旧档、真实provider均未验证。
- 本轮只为确认中断状态执行现有Composition与双配置编译；没有Stage、部署、打包、写存档、切默认、恢复自动化或安装Skill。

## 2. 路径、用户与Git身份

| 项目 | 实际值 |
|---|---|
| 活跃工作树 | `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918` |
| 共享Git目录 | `G:/AFMOD/NEW-10/.git`；worktree gitdir 为其 `worktrees/modularize-20260918` |
| 分支 | `codex/af-modularize-j04-20260918` |
| GitHub仓库 | `https://github.com/klfwdf/AnimusForge`，仓库owner `klfwdf` |
| 权威远端重构分支 | `origin/codex/af-main-refactor-continuation-20260831`；本包基线25a89cea，已普通快进到f6ddd565 |
| 辅助远端分支 | `origin/codex/af-modularize-j04-20260918`；早先因目标理解偏差创建，暂保留在f6ddd565，不作为权威入口 |
| 当前checkpoint | `bd2582aa776d9763002c85d75509ed9ccdb7b424` |
| Claude最后已提交代码 | `157dc7f21ec98f3cddff96d5548c470a526aacfb`，J06c |
| 本地Git作者 | 提交记录为`GM`；这不是GitHub登录账号证明 |
| Windows路径标识 | `C:/Users/28358`；不能与Git作者/仓库owner混为一人 |
| 旧工作区 | `G:/AFMOD/AF-REFACTOR`，保留两份用户草稿和另一条文档分支；本包未合并/覆盖 |
| 本地人工转发版 | `G:/AFMOD/AF-REFACTOR/.tmp/claude-code-direct-handoff-20260919.md`，不提交GitHub |

## 3. 三份SKILL的约束如何落到后续

### 3.1 `animusforge-maintainer` 0.2.0

- 只完成当前真实责任包，不能把普通修复变成全仓一次性重写。
- 双版本、一套源码、单模块、Bootstrap唯一加载、存档键/类型身份必须保持。
- 游戏对象在所属主线程读取/写入；后台只消费detached输入，回写时重验owner、generation、目标与来源。
- 目录搬迁与职责拆分分别验收；只有算法/状态归新owner并接真实消费者才算迁移完成。
- 构建、Stage、部署、打包、发布互不等价。OFFLINE_VERIFIED不等于LIVE/SAVE。

### 3.2 `af-core-framework`

- 固定三层：AF主体；同DLL typed internal制作组服务/薄桥；独立子MOD使用版本化public API。
- internal与public都复用同一真实LLM、动作和Memory/AFEF权威，不造缩水第二管线。
- Api能力以实际实现为准；当前Native开放，Scene/Courier仍要按用户已经批准的三渠道目标在J10/J14完成，不能用历史默认NotSupported降范围。
- 稳定的是分层/兼容承诺，不是内部类名和算法；不兼容public变化另开版本。
- 代码地图/台账保存详细证据，HANDOFF只给当前摘要和下一动作。

### 3.3 `animusforge-policy-effect-module`

- 只在 `PolicySystem/Effects/Modules` 的源码政策effect任务中直接触发；不是第三方DLL插件协议。
- MCM启停只控制未来检索，不能取消/改写已准备或已持久化实例；source→runtime lineage、canonical targets、幂等/回执/补偿必须保持。
- J06d可以捕获“某规则当前是否可进入检索”的detached事实，但不能把政策模块业务、调度、存档或运行中kill语义吞进Prompt owner。
- J11处理Policy/Gathering/Siege桥时只改获准AF侧接缝；玩法和数值仍归各模块owner。

额外继续适用全局 `afmod-clean-code-guard`：替代路径接线后删除旧实现/重复helper/过期flag；真正Saveable/ABI/消费者仍在的兼容壳必须写明理由。

## 4. Claude Code这段时间实际完成

### J04 Prompt Composition — `J04_OFFLINE_VERIFIED`

- 把共享约771行Prompt builder拆成detached request、规则ID/排除/sticky/topic routing、routing、section capture、纯assembly、runtime appendices等Composition owner。
- Native/Courier按“主线程capture → worker routing → 主线程complete”调度，每次线程跳转重验；Scene保留原语义，完整调度归J10。
- 被替代的旧组合实现已删除；13个Composition owner和实际消费者接线已经提交。
- 终点 `8faf5fbe`，文档 `d73b1215`，当时地图253锚点；离线回归与双配置六构建有记录。LIVE/SAVE/provider NOT-RUN。

### J05 Memory/Persistence — `J05_OFFLINE_VERIFIED`

- J05a：`NpcActionLedger`/`DialogueHistoryLedger`收敛记录规则。
- J05b：9个已有memory owner原样归位到 `src/modules/AF.Module.Memory/{Records,Summary,Recovery}`，保持类型/namespace和调用。
- J05c：`OwnerJsonStorageCodec`收敛7处SyncData JSON循环。
- J05d：`PlayerExportsStore`/`NpcDataFileName`删除My/Onboarding/StrategicProfile三个宿主的重复副本。
- 终点 `d903df67`，文档 `c91bd2a9`，地图262锚点绑定该终点。五个嵌套Saveable类型、Sanitize、约6k行Dev UI以及硬预算仍明确保留。
- 两个预先存在失败：`PersistenceIdentityAudit.py`与`MemoryFailureUiBoundaryTests`在25a89cea基线同样失败，没有为本包改断言凑绿。

### J06 Knowledge — 已提交但父包未验收

- J06a `8337f0b7`：`KnowledgeRuleIndex`接管规则索引/查询纯责任；宿主保留Campaign/ONNX生命周期适配。
- J06b `e66ba0d7`：`LoreCandidateRetriever`接管Lore候选检索算法，宿主保留live数据构造与格式化。
- J06c `157dc7f2`：`EntityNameMatcher`、`EntityMentionList`、`EntityInjectionAllocator`接管世界实体纯算法；`WorldEntityRetrievalService`保留游戏候选枚举、位置/距离、称谓与最终Prompt块。
- 三切片提交说明各记录focused契约与双配置构建，但没有父包HANDOFF/台账最终回执，也没有把代码地图从J05的262锚点重新绑定。

## 5. 断开时正在做的J06d

目标：消除J03/J04后台规则检索对 `Hero.Find`、Mission和其他live资格函数的依赖。主线程在捕获 `PromptRuntimeTargetBinding` 时同时冻结规则资格事实，worker只读detached `PromptRuleEligibility`。

### 已写入checkpoint的代码

| 位置（checkpoint `bd2582aa`） | 当前作用 |
|---|---|
| `AIConfigHandler.cs:5711` | `static PromptRuleEligibility CapturePromptRuleEligibility(` |
| `AIConfigHandler.cs:5683` | `static void ApplyGuardrailRuntimeTarget(PromptRuntimeTargetBinding binding, PromptRuleEligibility eligibility)` |
| `AIConfigHandler.cs:1734` | `static bool IsRuleCurrentlyEligibleForRag(` |
| `MyBehavior.cs:19672` | `static PromptRuleEligibility CapturePromptRuleEligibility(` |
| `MyBehavior.cs:30319` | `PromptBuildRequest CapturePromptBuildRequest(` |
| `src/modules/AF.Module.Prompt/Composition/PromptRuleEligibility.cs:12` | `sealed class PromptRuleEligibility` |
| `src/modules/AF.Module.Prompt/Composition/PromptBuildRequest.cs:12` | `sealed class PromptBuildRequest` |
| `src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalContextOwner.cs:25` | `static class PromptRetrievalContextOwner` |
| `src/modules/AF.Module.Knowledge/Index/KnowledgeRuleIndex.cs:39` | `sealed class KnowledgeRuleIndex` |
| `src/modules/AF.Module.Knowledge/Entities/EntityNameMatcher.cs:21` | `static class EntityNameMatcher` |

九个checkpoint文件：
- `AIConfigHandler.cs`
- `CourierDeliveryBehavior.PromptSchedule.cs`
- `MyBehavior.cs`
- `ShoutBehavior.NativePromptBuild.cs`
- `ShoutBehavior.cs`
- `src/modules/AF.Module.Prompt/Composition/PromptBuildRequest.cs`
- `src/modules/AF.Module.Prompt/Composition/PromptRuleEligibility.cs`
- `src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalContextOwner.cs`
- `tests/modules/AF.Module.Prompt/Composition/PromptCompositionTests.csproj`

### 为什么它还不是完成

1. `PromptComposition`只是因csproj加入新类而编译，现有155项没有针对11个资格位和旧switch顺序的旧新对照。
2. 没有可执行断言证明worker路径不会落回`ResolveConversationTargetHero`/`Hero.Find`/Mission或模块live方法。
3. `CapturePromptRuleEligibility`调用Vassalage/Diplomacy/WorldDiplomacy/Marriage/NpcMajorActions/LordsHall等live入口；须逐一确认只在主线程执行、无不应提前消费/写状态的副作用、异常fallback与旧行为一致。
4. ambient `Eligibility`已加入copy-on-write context并由scope恢复，但尚缺嵌套、异常、`Task.Yield`、并发请求和Clear后的隔离用例。
5. 旧setter-only消费者允许eligibility=null回退live路径；必须列清哪些仍合法同步调用，不能让worker悄悄走旧fallback。
6. Native/Courier/Scene/My所有真实调用点虽已传值，但未跑J03 ProductionEvaluation/Consumers、J04 BuildPhases、完整三渠道和Knowledge邻接矩阵。
7. 代码地图仍绑定J05 `d903df67`：recorded 262 PASS，当前working-tree因`ShoutBehavior.cs`内容变化FAIL。这是已知未收口证据，不可刷新hash冒充验收。
8. 没有更新范围图、owner matrix、J06父包状态或清理旧live资格路径；没有旧红/新绿与编译后behavior mutation。

### 本交接代理的最小验证

- `tests/modules/AF.Module.Prompt/Composition/PromptCompositionTests.csproj`：`PASS prompt-composition checks=155`；出现fixture字段未赋值编译警告，因此不当作生产0警告。
- 原脚本Debug：1.3、1.4、Bootstrap均成功，0警告/0错误；无Stage/Deploy。
- 原脚本Release：1.3、1.4、Bootstrap均成功，0警告/0错误；无Stage/Deploy。
- `git diff --check`在checkpoint前通过；checkpoint后tracked工作树干净。
- 没有运行实机、旧档、真实provider、J06d专项行为测试或整套回归。

机器可读证据：`docs/audits/2026-09-19-claude-code-interrupted-checkpoint.json`。构建日志位于本机 `.tmp`，未提交二进制/日志。

## 6. 下一位应如何完成J06d/J06

1. **先保持checkpoint不再扩大范围**：从`bd2582aa`继续，为`PromptRuleEligibility`建立旧live逻辑oracle与每条资格位的纯契约；先复现worker会调用live资格，再让captured路径绿。
2. **验证捕获无副作用**：逐个检查Vassalage/Diplomacy/WorldDiplomacy/Marriage/NpcMajorActions/LordsHall/GCCZ调用。若某入口消费状态或含网络，不得塞入capture；拆成只读资格查询或把责任留正确owner。
3. **验证ambient寿命**：嵌套scope、异常、yield、并发、Clear、旧setter-only回退；eligibility不能串请求，mentions merge仍按J03规则。
4. **锁死线程边界**：Native/Courier scheduler的Begin/Complete必须主线程、Routing/Preprocess worker只读DTO；Scene当前无完整scheduler，不能用WIP掩盖J10责任。新增source-linked断言而不是字符串hash刷新。
5. **跑邻接矩阵**：J03 Configuration/Models/Retrieval/ProductionEvaluation/Consumers；J04 Composition/BuildPhases；Knowledge Index/Entities/HeroAsset；Native/Courier/Scene；三份Skill要求的双API+Bootstrap。任何已有失败先与25a89基线比较，不能删除断言。
6. **清理旧体**：确认所有worker消费者都走captured事实后，删除只为后台准备保留的重复live解析；仍被同步/兼容消费者使用的入口列理由，不机械删。
7. **收口J06父包**：完成Knowledge导入校验8静态方法的归属复核、a/b/c/d实际消费者与性能/容量证据；更新范围图、owner matrix、代码地图和主台账。只有地图两模式与验收矩阵完成，才标`J06_OFFLINE_VERIFIED`。
8. **之后按总计划继续**：J07 Conversation/Native → J08 LLM传输 → J09 Actions/事实 → J10 Scene/Courier → J11制作组接缝 → J12领域 → J13其他领域 → J14三渠道public API → J15内容/profile → J16 tests/tools/Bootstrap → J17结项。

## 7. 仍未完成的整体内容

- J06d及J06父包验收；当前地图/范围文档落后于源码。
- J07真实会话/Native owner拆分；J08唯一LLM传输/Streaming/ModelCatalog/TTS；J09标签→计划→唯一执行→AFEF/receipt。
- J10 Scene多人接力/旁听/输入去重/退场，以及Courier运输/预生成/到达/旧retry生命周期。
- J11内部制作组桥，仅AF接缝；Policy/Gathering/GCCZ玩法不重写。政策模块MCM只影响未来检索。
- J12 Economy/Diplomacy/WorldMap、J13 Weekly及其他领域责任拆分。
- J14依用户授权开放Native/Scene/Courier三渠道public API；历史旧表的“默认不开放Scene/Courier”已被当前总计划J14和用户明确要求取代。
- J15资源/profile唯一归属；J16测试/工具/脚本/docs/Bootstrap边界；J17 UNASSIGNED=0、无双核心、同候选最终验收。
- 当前候选的真实Campaign/Mission、旧存档、live Economy/AFEF、真实provider、独立子MOD加载仍NOT-RUN。

## 8. 提交清单（基线25a89cea之后）

| 提交 | 内容 |
|---|---|
| `23b5861d` | docs(prompt): record J04 intent, verified entry points and three-channel target mapping |
| `e0aa8142` | refactor(prompt): extract rule-id policy, built-in topic routing and sticky carry into Composition owners |
| `be91c047` | refactor(prompt): move preprocess rule-id convergence into PromptPreprocessRuleIdAssembler |
| `27ec5e26` | refactor(prompt): compose shared Extras from captured sections via PromptExtrasComposer |
| `2a191526` | refactor(prompt): publish retrieval target identity through PromptRuntimeTargetBinding |
| `11f90fec` | test(prompt): teach lifetime parity and postprocess harnesses the J04 target binding |
| `96d9f8f5` | docs(prompt): record J04 first-slice offline receipt, 231-anchor map and scope |
| `6315fd26` | refactor(prompt): own the injected rule block text format in PromptRuleBlockText |
| `7176ebe8` | refactor(prompt): run built-in topic routing through PromptTopicRoutingStage |
| `05f4ef1d` | refactor(prompt): own exclusion-set assembly in PromptExclusionSets; add PromptBuildRequest DTO |
| `6e7b531d` | refactor(prompt): decide context flags, clarification gate and lore source in PromptContextDecisions |
| `7d0772be` | refactor(prompt): assemble final prompt context in PromptAssemblyStage from captured inputs |
| `d6824d9d` | refactor(prompt): split shared prompt build into capture/route/capture/assemble/append phases |
| `e9f6ecb6` | docs(prompt): record J04 second-slice receipt, 241-anchor map and explicit phase boundary |
| `249dd1ae` | docs: master modularization plan J04f–J17 with verified entry points and acceptance runners |
| `72f8d342` | refactor(prompt): schedule the Native prompt build as three steps on the correct threads |
| `52247a51` | refactor(prompt): schedule the Courier prompt build as owner phases and thread-pool retrieval steps |
| `00d82601` | docs(prompt): record J04f receipt (Native/Courier scheduled build), 250-anchor map and scope |
| `8faf5fbe` | refactor(prompt): resolve mentions in the routing step; compose triggered rule block from captured sections |
| `d73b1215` | docs(prompt): record J04_OFFLINE_VERIFIED, 253-anchor map, retained scope and pre-existing persistence runner failure |
| `416da085` | test(persistence): make source exclusion worktree-relative; refresh drifted MyBehavior binding lines |
| `3cc6f0d7` | refactor(memory): own NPC action and dialogue history record rules in AF.Module.Memory/Records |
| `8b712247` | refactor(memory): relocate existing memory owners from Refactor/ to src/modules/AF.Module.Memory |
| `321c7318` | refactor(persistence): own per-owner JSON storage codec in AF.Persistence (J05c) |
| `d903df67` | refactor(persistence): own PlayerExports store and NPC data file naming (J05d) |
| `c91bd2a9` | docs(memory): record J05_OFFLINE_VERIFIED, 262-anchor map rebound to moved owners, retained scope and pre-existing failures |
| `8337f0b7` | refactor(knowledge): own rule retrieval index in AF.Module.Knowledge/Index (J06a) |
| `e66ba0d7` | refactor(knowledge): own lore candidate retrieval in AF.Module.Knowledge/Lore (J06b) |
| `157dc7f2` | refactor(knowledge): own entity name matching, mention list and injection allocation (J06c) |
| `bd2582aa` | wip(prompt): checkpoint interrupted J06d eligibility capture |

其中`bd2582aa`是本交接代理为防断线丢失而保存的WIP，不是Claude宣称完成的切片。Claude最后正常提交是`157dc7f2`。

## 9. 推送/回滚/协作边界

- 按用户指定发布到原重构分支`codex/af-main-refactor-continuation-20260831`，不修改`main`。
- 推送前fetch确认目标`25a89cea`是本地`f6ddd565`祖先，比较为本地领先32、落后0；全程普通快进，禁止force push。
- 辅助分支`codex/af-modularize-j04-20260918`因早先目标理解偏差而存在，未获明确授权前不删除；后续交付与比较只认原重构分支。
- 回退J06d用focused revert `bd2582aa`；回退J06c/b/a依次为`157dc7f2`、`e66ba0d7`、`8337f0b7`。不要hard reset共享历史。
- 不提交`.tmp`、bin/obj、日志、DLL、玩家配置或本地直发版。
- 旧工作区两份用户草稿不在此工作树，本轮未改；不要因新分支干净就清理旧目录。
- 后续并行必须按文件/owner隔离；Git索引、共享地图/台账及最终集成由一位总控管理。

## 10. 直接启动语

> 请在 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918` 工作；本地施工分支是`codex/af-modularize-j04-20260918`，权威GitHub交付/比较分支是`codex/af-main-refactor-continuation-20260831`。先读AGENTS、三份仓库SKILL、根HANDOFF、本详细交接、主台账J04–J06和范围图。`bd2582aa776d9763002c85d75509ed9ccdb7b424` 是J06d中断WIP checkpoint，不是验收完成；当前HEAD应以`git rev-parse HEAD`为准。先补PromptRuleEligibility旧新行为、线程/ambient隔离与真实消费者测试，确认无live读/副作用，再跑J03/J04/Knowledge/三渠道/双版本矩阵并更新262点之后的新地图。未获新授权不部署游戏、操作存档、安装全局Skill、切默认或恢复自动化。用户要求三渠道public API最终开放，政策/宴会/GCCZ玩法不重写。
