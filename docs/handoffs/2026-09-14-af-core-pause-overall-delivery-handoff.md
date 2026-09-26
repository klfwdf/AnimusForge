# AF 主体整体暂停与 GitHub 交接 HANDOFF

> 2026-09-14。本轮按用户要求关闭自动化、审查整体进度、交付已有代码与文档；**没有继续修改生产代码**。
> 唯一工作区：`G:/AFMOD/AF-REFACTOR`。双 Skill：`.claude/skills/animusforge-maintainer/SKILL.md` + `.agents/skills/af-core-framework/SKILL.md`。

## 1. 最准确的结论

**现在是阶段 8 / B1「记忆可靠性与实际预算」；开发已按用户要求暂停，B1 尚未整批合格，整个重构没有收尾。**

- 已不再是前次“最新封存/素材 WIP 未接入验证”的状态：该集成缺口已关闭，后续四项改动也有对应离线证据，最新生产/测试为 `86805518259ee44e3924da787bfba97654208074`。
- 已形成用户要求的“AF 主体 → 同 DLL 制作组内部接口 / 单独对外 API”分层基础；真实 Gateway、部分渠道边界、记忆防过期接受和 typed ports 已接线，不是只有文档。
- **主体大类的真正职责拆分仍明显不足。** 当前大部分功能仍由原 owner 运行，新组件/partial 接入其中；不能写成“旧代码清零”“全部用新实现复现”或“只剩实机”。
- 最近主要改善记忆维护的正确性、预算和分配成本，并未完成整个对话系统的独立服务化。Courier 后台读取 live 游戏/历史对象的源码缺口仍在。
- 当前候选缺少真实 Campaign/Mission、旧档、资产/AFEF、TTS/provider 和外部 DLL 加载验收。旧版本“别人测试过”的反馈不能替代本候选证据。
- **不提供无统一分母的 60%/90% 总进度。** 看下面“架构/拆分/功能/验证”四个维度；阶段编号也不意味着 8/8 已完成。

## 2. 版本、暂停与交付边界

| 项目 | 核实事实 |
|---|---|
| 最新源码/测试 | `86805518259ee44e3924da787bfba97654208074`，已完成受影响范围离线联验，非发布验收 |
| 本轮开始 HEAD | `8f0e3ab8fe523bb0999d09bb8c47e17fe1a495fe`，此前最新交接提交 |
| 本轮文档意图检查点 | `ef7432f6`；只登记暂停和文档交付，不是生产修改 |
| 本地分支 | `codex/af-framework-skill-delivery-20260911` |
| 指定 GitHub 目标 | `origin/codex/af-main-refactor-continuation-20260831`；不推 main、legacy-github 或其他成员分支 |
| 本轮最初 fetch | 目标 `3f00fefa4040df8caa12d42195f8543b3d7f0296`；本地 ahead 17 / behind 0，包含 5 次生产/测试提交及配套文档 |
| 自动化 | 应用工具已将 `af-7-8` 设为 **PAUSED**，读回确认；旧 `af` 同样 PAUSED。历史 ACTIVE 不再生效 |
| 发布性质 | 已获准把已有重构代码与详细交接普通快进推到开发分支；不是 Release、不上传游戏 DLL/存档/原始运行日志 |
| 本轮不做 | 继续生产开发、部署、真实存档操作、默认切换、删旧、业务融合、制作组玩法变更、强推 |

[GitHub 专门重构分支](https://github.com/klfwdf/AnimusForge/tree/codex/af-main-refactor-continuation-20260831)。推送前重新 fetch 并验证祖先关系；实际完成以远端 ref 和最终回执为准，本文不自引用尚未生成的提交号。若远端分叉就停止交付，不擅自融合。

### 实际推送回执

**代码与本篇详细交接已普通推送成功，远端 `refs/heads/codex/af-main-refactor-continuation-20260831` 经 `git ls-remote` 核实为 `dcc17ee70832e2c63725bf08b0a284f9a94429d3`。** [已核实交付提交](https://github.com/klfwdf/AnimusForge/commit/dcc17ee70832e2c63725bf08b0a284f9a94429d3)。此次为 `3f00fefa → dcc17ee7`，19 个提交、41 个差异文件；本段随后仅补写交付状态，生产仍为 86805518。没有强推、融合、部署或上传本地专用材料。

## 3. 整体进度与原计划的对应

| 范围 | 已落地 | 还差什么 | 当前状态 |
|---|---|---|---|
| 原阶段 0–3：基线/设计/契约 | 原 AF 对照、工作区、构建流程、部分契约和 runtime | 仓库/许可等历史 HOLD 不自动解除；设计 DONE 不能等于完整 Host | 基础具备，非全部平台目标完成 |
| 原阶段 4–6：管线与主体边界 | Gateway、Prompt/后处理/ActionPlan/Memory 接缝、存档身份保护 | 真实 owner 大类拆薄、全入口一致性及端到端验收 | 部分实现并验证 |
| 原阶段 7：领域接入 | 多数已选领域 AF 侧桥接/owner 已接上 | 真实 Economy/AFEF/旧档、领域组合与完整失败降级 | 不能据模块接线宣布交付 DONE |
| 阶段 8 / B1，对应 P1 | 最新索引/封存集成已闭合，来源校验、预算、排序、owner 净化通过局部联验 | 深记录、初捕获、完整来源/owner 绑定、Apply 等原子成本及整批出口 | **PAUSED，未整批合格** |
| B2，对应 P2 | 历史 Native/Scene/Courier 接缝与修复保留 | 新计划整批尚未启动；优先 Courier 线程，继而完整三渠道功能对照 | 待 B1 放行并获准恢复 |
| B3，对应 P3 + P4-01 | 三组内部 ports、目录装配、公共只读兼容存在 | 真实生命周期/失败隔离、能力贡献、组合和主类责任提取 | 未完成最终接缝验收 |
| P4 可选公开能力 | V1 版本/能力/目录查询 | 提交/结果/取消/通知及扩展范围 D-A/D-B 未决定、未实现完整 SDK | 范围待定，不假成功 |
| P5/P6：候选/收尾前 | 当前源码六项 Stage、ABI/身份等离线材料 | LIVE/SAVE、问题清单、性能/组合、删除与默认迁移候选 | 未达 READY_FOR_CLOSEOUT_REVIEW |
| 最终删旧/默认/正式发布 | 仅有局部已替代代码的定向删除 | 全部替代接线与兼容责任证明、同候选验收、最终明确决定 | 未执行、不能提前宣布完成 |

## 4. 对照最初 AF：拆分到底做成什么样

原始对照用 `d4cb1467`（重构起点；本轮复核其 C# 与 `96a1c60f` 相同），不是已经经历重构的 `182da1db`。统计仅 Git 跟踪文件，物理行包括空行/注释；家族 = 根目录 `Name.cs` + `Name.*.cs`，不包含独立 runtime。历史间包含功能新增，**行数不能用来证明功能相同或质量下降**。

| 指标 | 最初 d4cb1467 | 本次推送前远端 3f00fefa | 当前 86805518 |
|---|---:|---:|---:|
| 根目录 C# 文件 | 289 | 319 | 320 |
| Refactor C# 文件 | 0 | 48 | 52 |
| Api/V1 C# 文件 | 0 | 2 | 2 |
| 三个核心 owner 的额外 partial 文件 | 0 | 19 | 20 |
| MyBehavior 主文件 / 整个家族行数 | 59,365 / 59,365 | 58,795 / 62,506 | 58,834 / 62,867 |
| ShoutBehavior 主文件 / 整个家族行数 | 38,870 / 38,870 | 39,883 / 41,607 | 39,883 / 41,607 |
| CourierDeliveryBehavior 主文件 / 整个家族行数 | 9,859 / 9,859 | 10,797 / 11,509 | 10,797 / 11,509 |
| 三大家族合计行数 | 108,094 | 115,622 | 115,983 |

当前 `Refactor/` 分布：9 Contracts、17 Runtime、21 Adapters、5 Modules。额外 partial：MyBehavior 12、Shout 6、Courier 2。

**结论：接口/执行边界拆分有真实成果，主体大类拆薄尚未达标。** 三个主文件合计仍 109,514 行，连同 partial 是 115,983 行；partial 共享同一类的私有状态，不是独立业务 owner。最近五次生产提交主要集中在 MyBehavior 的记忆维护，未改 Shout/Courier 主体；不能说这轮已经覆盖所有模块。

### 当前结构，而不是只画目标图

```text
一个 Modules/AnimusForge；Bootstrap 按游戏版本只加载一个实现
AnimusForge.dll
├─ AF 主体：MyBehavior / ShoutBehavior / CourierDeliveryBehavior + partial
│  ├─ 大量原规则、Prompt、标签、游戏状态和副作用仍由原 owner 负责
│  └─ 共享 Gateway/Coordinator/Committer + 独立索引/预算/排序/摘要组件
├─ 制作组 internal typed ports / adapters
│  └─ 政策、宴会、GCCZ 原业务 owner（本任务不重写其玩法/业务存档）
└─ public AnimusForge.Api.V1
   └─ 版本、能力、目录只读查询；提交/动作/写记忆/注册尚不支持
独立子 MOD DLL → 只依赖选定公开契约，不依赖全部制作组模块
```

- `LegacyInteractionPipelineComposition` 仍通过 delegates 使用原规则/Prompt/后处理；Legacy 名称不是可删除证明。
- `ModuleFrameworkRuntime.Initialize` 的 Ready 表示 adapter 装配/目录校验，不等于 Campaign 可以接单或完整模块可以安全卸载/恢复。
- 新 V1 中 1 项 CatalogRead 可用、6 类提交/动作/写入/注册能力 NotSupported。既有其他公开方法仍并存；不能说 AF 完全没有对外接口，也不能把新 V1 说成完整调用 SDK。
- 稳定的是契约与责任边界，主体算法/提示词/策略仍可按用户需求演进。英文用于代码标识符/协议 ID，中文可用于提示词定义、注释和玩家说明；不硬编码测试 NPC、机器路径或固定功能名单来凑实现。

## 5. 这段时间实际做了什么

| 生产/测试提交 | 实际交付 | 删除/保留与限制 |
|---|---|---|
| `73a6977c` | 提取独立 `EventSourceMaterialIndex<T>`，真实 MyBehavior 调用；关闭原 9 项未审源码集成缺口 | 删除 `MyBehavior.EventSourceMaterialIndex.cs` 旧 partial；只提取派生索引责任，权威素材/存档仍属原 owner |
| `9158132c` | Campaign 维护与 deferred 维护共用期限与授予；过期窗口的同日摘要启动可在后续恢复且绑定档代 | 不再把每调用额度说成同一 Campaign cycle 总额；不是整个 EngineTick/游戏帧硬上限 |
| `8bcde78b` | Daily/Major 尾部排序使用稳定、可续跑排序组件；发布前复核来源/键/文化 | 比较/移动与输出逐操作计费；数组分配、键捕获、部分净化/最终绑定仍原子 |
| `40b92e67` | 完整 raw 来源摘要由 JSON 改为显式字段编码 + 小缓冲 SHA256；10 类模型 122 字段，保留 null/empty/顺序等区别 | 去掉该路径重复 JSON/反射/大分配；通用上下文/编辑器 JSON 指纹不变；完整遍历仍 O(N) 原子 |
| `86805518` | owner 净化按实际 draft 授予预算，复用原单条净化体与排序；发布前防旧 empty/key/列表决定误删新事实；失效重新封存 | 原同步 Sanitize 仍有实际调用且共用规则；单 draft 的深 lines/triggers 仍原子，非 owner 事务 |

测量是离线 fixture，不是游戏 FPS 保证：

- 257 草稿及实际 Campaign 的 65 草稿 fixture，在有限窗口中每次最多处理 8 个 draft，稳定源各处理一次。
- 1 draft / 1024 lines 仍可在一次原子净化中处理：这一限制被测试显式记录，**8 draft 不等于 8 line**。
- 1000 记录 × 12 次完整 raw 摘要对照中，分配约减少 96–98%；这是该摘要路径的收益，不等于初次捕获或整帧同幅度提速。
- 净化元数据可按记录提前可见；列表删除/去重/排序只有完成并校验后发布。不能把它称为整 owner 事务或发生异常后自动回滚。

专题细节：[索引集成](2026-09-14-b1-index-owner-integration-handoff.md)、[共享预算](2026-09-14-b1-campaign-budget-handoff.md)、[队列排序](2026-09-14-b1-queue-sort-handoff.md)、[raw 摘要](2026-09-14-b1-raw-digest-handoff.md)、[owner 净化](2026-09-14-b1-owner-normalize-handoff.md)。这些历史文档当时的 ACTIVE/未推送字段不代表本次暂停后的状态。

## 6. 功能复现清单：保留运行不等于已完整证明

| 主体功能 | 当前覆盖 | 尚未证明 / 需要继续 |
|---|---|---|
| Native 普通/流式/主动开场 | admission、准备、pending history、完成/动作边界和旧代码对照已有；实际普通入口仍进入原完整实现 | 不能拿独立 opt-in 短链冒充默认入口全替代；本候选完整游戏交互/换目标/关窗/音频待验 |
| Scene 正文/完整后处理 | 正文生成专用链与原完整后处理分离；保留接力、旁听、去重和单次完成边界 | 多 NPC、离场、部分动作失败、唯一事实/AFEF 与展示顺序的同候选全流程 |
| Courier 回信/预生成/主动来信 | 到达时提交及部分后处理/完成边界保留 | 后台准备仍读 live 对象/历史；预生成不能提前扣款、执行或记已发生事实 |
| LLM/Prompt/标签/ActionPlan | 共用 Gateway、快照/契约、前主后处理及权威执行接缝 | 大量规则构造仍在旧 owner；未知/冲突/过期/部分成功等完整组合回执未全面闭合 |
| 历史/三类摘要/AFEF | 主线程捕获、完整 raw/上下文重验、晚结果拒绝、编辑/导入档代保护、维护/排序已有证据 | 深来源预算、全 writer 覆盖与所有 Apply 尾步；不能承诺全系统 exactly-once/事务恢复 |
| Economy/Reward/Debt | Hero/Party/Merchant owner、渠道 commit 与原算法保留 | 真实金币/物品/商人/债务前后账本、失败不记假事实、当前候选 AFEF |
| 周报/主动 NPC/Issue/决斗/大地图等 | 既有 Gateway/owner/桥接和局部专项修复保留 | 本轮未逐领域重新验玩法、周期调度、重试和降级；不冒充全面复现 |
| TTS/气泡/口型/UI | 既有展示、队列及生命周期局部回归 | 网络生成完成≠真实播放结束；FIFO、取消、退场/读档的真实音频验证 |
| 内部政策/宴会/GCCZ 接口 | 3 组 typed ports / 13 方法和真实 adapters/consumers | 接缝生命周期、缺失/停用/异常/组合验证；玩法由成员 owner 维护 |
| 独立子 MOD | V1 只读查询与 API 可见性/元数据证据 | 完整请求 SDK、通知、取消、写能力未开放；独立 DLL 实际加载待验 |
| 存档/版本/构建 | 146 SyncData keys、36 behaviors 身份与六项 Stage 证据 | 身份不变不等于旧档加载及往返正确；真实 Campaign/Mission/双版本组合待验 |

## 7. 未完成问题的性质与优先级

| 问题 | 结论 | 下一步（仅在用户明确恢复后） |
|---|---|---|
| 原“9 项未审 WIP / 地图过期” | **已关闭**；当前严格差异/地图和受影响测试已接到 86805518 | 不重复修旧阻塞，不只刷新 hash 获得新 PASS |
| B1 深来源/原子成本 | **已确认限制，仍有代码工作**；单 draft、初 capture/copy、完整 raw 遍历、owner/key/empty 绑定和部分 Apply/public/weekly 尾步未硬切分 | 从已量测的深记录与初捕获/最终绑定选完整影响面；保留来源变化拒绝，不删历史来降成本 |
| writer epoch 不完备 | 完整 raw 校验仍必要；只在 Save 上加 revision 会漏原地净化、嵌套/队列/state、跨实体写 | 先列齐实际 writer/读取和生命周期，再决定 revision/快照替代边界 |
| Courier 后台 live 读取 | **源码确认，未修**；不是根据方法名猜线程，也未在游戏复现特定崩溃 | B2 先主线程捕获 persona 完成后的请求，后台仅网络/解析，主线程按档代/session/目标接受 |
| 主体大类和内部 Host | **架构实施未完成，不是单纯文档缺口** | 按职责与真实 consumers 拆 owner；补必要生命周期/失败隔离，不堆空接口或重写制作组玩法 |
| 公开范围 D-A/D-B | 尚未决定；Native 最小完整入口仍是建议，不能替用户选 | 明确首版需要哪些提交/取消/扩展；没选入的不虚构完成 |
| 当前候选 LIVE/SAVE/资产/AFEF/音频/API 加载 | **NOT_RUN / 发布门槛未过** | 由负责人记录同源码/产物、场景、档副本、输入/账本/日志；不沿用无版本信息的笼统验收 |

Courier 当前可直接追踪的链：`CourierDeliveryBehavior.cs:4518 → 4527 → 4556 → 4561 → 4668–4690`；主动来信 `5019 → 5028 → 5061 → 5066 → 5092–5120`。`Task.Run` 中读取 session/Hero，`ConfigureAwait(false)` 后直接调用名为 `OnMainThread` 的准备函数；方法名和 generation 检查都不提供线程封送。可能引起状态竞态，但不将所有玩家 Bug 归因于此。

## 8. 代码坐标与责任注释

以下坐标在 `86805518259ee44e3924da787bfba97654208074` 及当前未变源码上核对，一基行号是定位片段，不代表整个类已提取/验收。跨提交先看符号。完整 [74 点代码图](../architecture/af-framework-code-map.json) 与 [代码范围说明](../architecture/af-framework-code-scope.md) 保留新旧混合边界。

| 路径:行号 | 符号 | 责任 / 覆盖边界 |
|---|---|---|
| `Refactor/Modules/TeamModulePorts.cs:7-10` | `internal interface IPolicyModulePort` | 政策 typed 接缝，业务归原 owner |
| `Refactor/Modules/TeamModulePorts.cs:17-20` | `internal interface IGatheringModulePort` | 宴会 typed 接缝，不迁移玩法 |
| `Refactor/Modules/TeamModulePorts.cs:27-30` | `internal interface ISiegeModulePort` | GCCZ 接缝，保留原场景门禁 |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` | `internal static class ModuleFrameworkRuntime` | 装配与只读投影，不是第二套执行器 |
| `Api/V1/AfApi.cs:13-16` | `public static class AfApi` | 当前只读；其他提交/写能力未开放 |
| `SubModule.cs:60-63` | `ModuleFrameworkRuntime.Initialize(out string moduleFrameworkReason);` | 模块装配，不是 Campaign Ready |
| `SubModule.cs:110-113` | `ModuleFrameworkRuntime.Shutdown();` | 发布停止状态 |
| `ShoutBehavior.NativeAdmission.cs:17-20` | `internal sealed class NativeConversationAdmission` | 准入/忙碌/会话绑定，不是完整公共提交服务 |
| `ShoutBehavior.NativePreparation.cs:14-17` | `private sealed class NativeConversationPreparationSnapshot` | 仍含原 Location 引用，不是公共不可变 DTO |
| `MyBehavior.HistoryPromptSnapshot.cs:35-38` | `internal static Func<string> CaptureHistoryContextWorkById` | 召回用途投影，非全局记忆事务 |
| `ShoutBehavior.ScenePostprocess.cs:25-28` | `private sealed class SceneActionPostprocessWorkItem` | 已有完整后处理；非本次重写 Scene 业务 |
| `CourierDeliveryBehavior.cs:4674-4677` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(recipient,` | 回信早期准备待线程审查，未迁快照 |
| `CourierDeliveryBehavior.cs:5102-5105` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(sender,` | 来信早期准备待线程审查，未迁快照 |
| `MyBehavior.MemorySummaryInput.cs:250-253` | `private MemorySummaryInput CaptureMemorySummaryInput(` | 三类唯一初捕获/复制/原Build，raw与effective context分离；首次绑定检查，深记录原子成本仍未收口 |
| `MyBehavior.MemorySummaryInput.cs:336-339` | `private bool IsMemorySummaryInputCurrent(` | context后fresh raw全字段摘要与动态资格；原owner/retarget/generation门禁，不靠Save-only epoch |
| `MyBehavior.MemorySummaryInput.cs:355-358` | `private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(` | 共用原 provider/Build/Parse；主线程解析、波次/重试退休，完成后释放大 payload |
| `MyBehavior.MemorySourceWrites.cs:22-25` | `private static bool DeferMemorySourceWriteIfNeeded(` | 旧 void façade 主线程同步、后台 owner/generation 排队；不是持久接受回执 |
| `MyBehavior.MemorySummaryMainThread.cs:88-91` | `private async Task<bool> RunMemorySummaryCompletionAsync(long generation, Func<bool> operation)` | 仅协调器传播operation异常，区分拒绝/取消与部分执行失败 |
| `Refactor/Runtime/EventSourceMaterialIndex.cs:11-14` | `internal sealed class EventSourceMaterialIndex<T> where T : class` | 独立派生索引/绑定 owner，无游戏和存档写入 |
| `MyBehavior.cs:13707-13710` | `private void RecordEventSourceMaterial(` | 原记录/追加/发布仍归主体 owner，使用独立索引 |
| `Refactor/Runtime/MemoryMaintenanceWorkBudget.cs:10-13` | `internal sealed class MemoryMaintenanceWorkBudget` | 独立协作预算窗口；不抢占单次深操作 |
| `MyBehavior.cs:17733-17736` | `private void RunCampaignMemoryMaintenanceCycle(` | 真实主/deferred维护共享周期与异常/nested恢复 |
| `MyBehavior.cs:17687-17690` | `private void OnCampaignTick(float dt)` | 实际Campaign入口接入共享维护周期，其他顺序保持 |
| `Refactor/Runtime/CooperativeMemoryQueueSort.cs:12-15` | `internal sealed class CooperativeMemoryQueueSort<T>` | 新增独立稳定排序组件；净化/键捕获/分配不冒充已分片 |
| `Refactor/Runtime/CooperativeMemoryQueueSort.cs:42-45` | `internal bool Step(MemoryMaintenanceWorkBudget budget)` | merge/copy逐单元复用Campaign累计预算，保持相同day/name的原次序 |
| `Refactor/Runtime/MemorySourceFingerprintWriter.cs:13-16` | `internal sealed class MemorySourceFingerprintWriter : IDisposable` | 真正独立的固定buffer/长度分帧/UTF16/SHA生命周期组件，无游戏对象 |
| `MyBehavior.MemorySourceFingerprint.cs:12-15` | `private static string ComputeMemorySummarySourceFingerprint(MemorySummarySourceView source)` | 原private DTO的122字段编码边界；三类真实Capture/IsCurrent消费者，瞬时格式非存档/API |
| `MyBehavior.MemorySealing.cs:127-130` | `private sealed class DailyMemoryDraftNormalization` | 逐draft原子规范化，结果私有；发布前key/empty/列表guard，单draft深文本和guard仍原子 |
| `MyBehavior.MemorySealing.cs:469-472` | `private bool RunDailyMemorySealDrafts(DailyMemorySealState state, MemoryMaintenanceWorkBudget budget)` | 真实caller，复用累计授予；失效重新封存/建索引，空owner直接移除 |
| `MyBehavior.cs:26585-26588` | `private static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(DailyMemoryDraft sourceEntry, HashSet<string> seen)` | 原内层体精确提取；同步/切片共同消费；DTO/clone/AFEF/marker规则保持 |

额外定位：`MyBehavior.cs:26573–26583 SanitizeDailyMemoryDrafts` 仍为同步入口，`26585–26655 SanitizeDailyMemoryDraftEntry` 共用旧单条规则；`MyBehavior.MemorySealing.cs:127–199 DailyMemoryDraftNormalization` 为渐进净化/绑定，`469–561 RunDailyMemorySealDrafts` 是实际消费与失效重新封存入口。索引实际字段在 `MyBehavior.cs:1997–1998`，使用/重建在 `13727` / `20162`；不是新增后无人调用。

## 9. 验证账本：本轮核查与此前运行严格分开

本轮不改代码，所以没有为文档再编译整套项目、重跑所有业务或启动游戏。新 [整体暂停审计](../audits/2026-09-14-af-core-paused-overall-delivery.json) 记录源码/产物/冻结日志 hash、结构统计和交付边界；既有 [86805518 验证 JSON](../audits/2026-09-14-b1-owner-normalize-verification.json) 保存实际执行命令、返回码和明确的替身限制。

| 层级 | 结果与绑定 | 不能推导的结论 |
|---|---|---|
| 本轮重新核查 | Git 原始/远端/当前统计，当前源码未变，74 点地图记录版+工作树均 PASS，3 个保护文件 hash 不变 | 不是全玩法静态证明或实机验收 |
| 本轮材料完整性 | 183 份本地冻结证据 hash、6 份产物与项目 Stage/hash 标记重新核对 | 不声称这些构建/业务测试在本次文档轮重新跑过 |
| 86805518 封存/owner | 75/0；同 75 例旧 40b 为 62/13；26 个故障控制均 BUILD_PASS 后 EXIT=1 | 旧版 13 红包含新预算/时序要求，不称为 13 个原游戏 Bug |
| 捕获/深来源 | 116/0；旧 8bc 112/4；294 字段变化拒绝证据；当前受影响 clone 控制通过预期失败 | 35 个 raw 故障控制主要绑定 40b，并非全部在 868 重跑；完整 raw 仍原子 |
| 相邻记忆测试 | business36 / planning24 / writers238 / terminal85 / commit51 / admission54 / materials23；摘要 writer 9 向量 + 5 守卫 | 测试有明确 game/provider 替身，不代表所有 live callers |
| 历史/UI/渠道 | helper32 / history852 / Native history27 / failure UI85 / Native preparation589 / ChannelCutover132 | 不等于完整三渠道游戏/真实 TTS、全部领域验收 |
| 精确源码守卫 | MyBehavior 58 声明 / 5 新 span / 2 删除精确回到 90201155；Input 4 声明精确回 8bc；8 组件锁、12 守卫 | 这是声明范围逆向对照，不是全仓库完全等价证明 |
| 当前代码构建/ABI | Debug/Release × 1.3/1.4/Bootstrap 六项 Stage；API119 + 并发256、预期外部 CS0122、4 DLL 元数据532；SyncData146 / behaviors36 | 不是游戏部署、真实旧档加载/存档往返或外部 DLL 实际运行 |
| 真实候选验收 | Campaign/Mission、旧档、真实资产/AFEF、provider/TTS、外部 DLL 加载 **NOT_RUN** | 不得填 PASS、DONE、零 Bug |

首次地图命令误用了仓库根 `scripts/verify_code_map.py`，文件不存在；已纠正到 `.agents/skills/af-core-framework/scripts/verify_code_map.py` 后两种模式实际通过。路径错误没有当成有效失败反例或产品问题。

### 制作组复跑入口

在仓库根使用该机器已有 Python/.NET，避免照搬他人盘符：

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
& G:\Python310\python.exe -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --source-baseline 40b92e67
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate unbudgeted-owner-normalize
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_captured.py
```

后两种旧版/故障场景的 EXIT=1 必须来自 BUILD_PASS 后明确断言失败；编译或提取失败不算有效反例。各相邻 runner、guards、完整 Stage 命令/依赖在验证 JSON 与 [测试 README](../../tools/MemorySummaryMainThreadBoundaryTests/README.md)。保持原 `一键编译覆盖推送/build_single_module.ps1 -Stage`；**不要改成 -Deploy**。本机 1.3/1.4 参考与运行依赖来自 NEW-10、游戏/workshop 指定目录，不能把另一机器路径当通用前提。

183 份冻结日志保留在本地 `.tmp/b1-owner-normalize-20260914/final-evidence/`，不入 GitHub；GitHub 提交源码、runner、hash 清单和汇总，成员需要自己的依赖重新运行。项目 Stage 产物在 `bin/{Debug,Release}/single_module_stage/`，不是已覆盖游戏的证明。

## 10. 后续计划：沿原清单继续，不另起编号

**现在全部实施暂停；下面是恢复后的顺序，不是自动继续指令。** 沿用 [P0–P6 原计划](../phase8/af-core-precloseout-plan-20260913.md)，第 17 节为本次暂停交接，第 16 节保留技术工作包但调度已失效。

1. **先登记真实玩家问题。** 每项记录源码/DLL SHA、游戏版本、模块开关、档副本、输入、应有/实际结果、日志；从同条件旧红/新绿结单，不用更多测试数字代替症状定位。
2. **完成 B1 剩余深来源与接受链。** 已完成索引/共享窗口/排序/raw 低分配/逐 draft 净化不重做；完整影响面处理深 line/trigger、初 capture/copy、最终绑定/Apply，并证明来源变动拒绝和部分失败/释放/重试保持。若采用 writer revision，先补全 writer 覆盖，不能砍掉完整检查换预算。
3. **B1 整批出口。** 代码、明确的成本/上限、受影响旧版/故障回归、相邻 suite、同候选六项 Stage 与身份门禁闭合；未接受的关键性能风险不能遗留。之后才进 B2。
4. **B2 三渠道。** 先 Courier 主线程准备，再一并核对上下文→Prompt→正文→标签→权威动作→历史/AFEF→展示；含重复、拒绝、取消、超时、晚回包、换目标/档/Mission。保持 Native 完整普通入口与 Scene 接力/旁听，不用缩水路径统一。
5. **B3 内部接缝和真实拆薄。** 从实际消费者提取高内聚规则/Prompt/记忆职责，保留唯一动作/事实 owner；补内部启停、缺依赖、失败隔离/组合。政策/宴会/GCCZ 只处理 AF 接缝，业务修改交给制作组 owner。
6. **公开范围与候选。** D-A/D-B 确认后才实现所选公开提交/结果/取消/通知/扩展；若首版明确只读，作为范围收窄记录，不叫完整 SDK。实际子 MOD 加载单列。
7. **P5/P6 收尾前评审。** 同候选的 LIVE/SAVE/资产/AFEF/音频、性能和问题清单齐套，逐符号列删除候选/保留原因/默认迁移/回滚。达到 READY_FOR_CLOSEOUT_REVIEW 后再决定最终动作。

### 玩家视角最低验收包

| 情形 | 必须看见的结果 |
|---|---|
| 普通/流式/主动开场，生成中关窗换目标读档 | 不串人、不重复记输入、晚返回不覆盖新会话 |
| 多 NPC 接力/旁听，NPC 离场、Mission 结束 | 正确发言顺序，旁听事实可查询，玩家输入仅一份，退场不补交旧动作 |
| 信使预生成/到达/回信/主动来信 | 到达前不执行/记假事实，失效目标安全失败，背景准备不读 live 对象 |
| 交易/债务/Party/Merchant、部分失败 | 精确资产前后差值，AFEF 只记真实已发生事实，不重复扣付 |
| 大历史、来源变动、编辑/导入、旧档 | 不丢历史/任务、不覆盖新来源、不串档；预算量测包含最大原子单元 |
| 内部模块缺失/关闭/异常/组合 | 普通 AF 仍可工作，GCCZ 规则不污染非 GCCZ 场景 |
| 真 TTS/气泡与外部 DLL | 真实播放结束和任务完成分开；FIFO/解绑/生命周期与能力状态准确 |

## 11. 回滚、协作和本地材料

- 本次只是已有候选 + 暂停交接发布，未执行任何回滚。最新 owner 净化的前生产为 `40b92e67`；五项改动如需退回，依依赖顺序定向逆转 `86805518 → 40b92e67 → 8bcde78b → 9158132c → 73a6977c` 的改动并同步测试/地图，再验证；不要执行一条盲目批量 revert，更不能 hard reset/force-push。
- `3f00fefa` 是本次交付前远端基线，不是“零 Bug 发布版”；`d4cb1467` 是原始对照，不是当前默认回滚目标；`182da1db`、`a096c1b1` 仅历史比较点。
- 未提交的两份用户草稿 `docs/handoffs/2026-09-06-integrated-phase8-handoff.md` 与 `2026-09-06-team-brief.md` 保持原样。工作树因此有已知 dirty，不宣称 clean；本次不把这些修改推上去。
- 指定本地 Native 简明版不在本分支提交历史中；推送前检查树和全部待推送提交，不是只靠 `.gitignore` 排除。其他工作区不写，不向 GCCZ/new- 镜像任何生产改动（本轮未改 siege）。
- 新的本地主体简明版：`G:/AFMOD/AF-REFACTOR/.tmp/AF主体简明HANDOFF-20260914.md`，可直接转发制作组，不提交 GitHub；它不是 GitHub 文档的必需依赖。
- 仓库历史本就跟踪部分 `.tmp/build_check` 参考 DLL；本次核对它们与远端一致，没有新增上传，也不借暂停交付擅自删除历史依赖。新日志/产物与本地简明版仍精确排除。
- 本地最终远端回执：`.tmp/af-core-paused-delivery-20260914/push-receipt.json`，只有实际普通推送并核实远端后才生成。接手先读根 HANDOFF 当前入口、原计划第 17 节、最新 Git 与双 Skill，不从历史 ACTIVE 自动恢复。
