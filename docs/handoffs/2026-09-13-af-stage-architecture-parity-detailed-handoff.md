# AF 详细 HANDOFF：阶段、原始对照、功能复现与后续收口

> 审查日期：2026-09-13。**本轮只审查、整理文档并交付 GitHub，不恢复生产开发或自动化。**
> 当前工作区：`G:\AFMOD\AF-REFACTOR`。根 `HANDOFF.md` 以本篇为最新审查入口；旧暂停交接保留 WIP 实施细节。

## 1. 先给结论

**现在按项目称谓处于阶段 8；实际停在 B1 / P1「记忆可靠性」的暂停 WIP，既没有完成 B1 整批验收，也没有完成阶段 8。**

- 已有成果不是空壳：共享 Gateway/契约、若干实际渠道接线、Scene 完整后处理边界、Native 生命周期、记忆来源校验、制作组 typed ports 和只读公共 API 已有真实代码及分层测试。
- 但也不是“主体已彻底拆完，只差游戏测试”：**大类的业务所有权仍高度集中、Courier 准备线程缺口还在、最新 WIP 未完成联合验证、公共请求 API 未实现**。这些包含源码/架构工作，不全是外部验收问题。
- 当前多数功能的延续靠“保留原实现 + 新边界接入 + 定向修复”，不是所有功能都已由独立新服务完全复现。原实现也存在缺陷，复现应保持有效行为、修复已确认问题，而不是复制旧 Bug。
- 不给 60%/90% 等无统一分母的进度；以本文的功能与验收门槛表判断。**不能宣布全功能复现、旧代码清零或无 Bug。**
- 建议更新已有后续计划，而非再开一套阶段编号。本次已把收口顺序写入[原 P0–P6 计划第 15 节](../phase8/af-core-precloseout-plan-20260913.md)；B1/B2/B3 只是其交付批次。

## 2. 版本、交付与自动化

| 项目 | 当前事实 |
|---|---|
| 审查起点 / 已有远端末端 | `007dbeee76b45a6ddd5924487b8df4ccc3a32213`；本轮初次 fetch 后本地与目标远端 0 ahead / 0 behind |
| 生产源码现场 | `c21523f81c314e9d6a6c6062ba2b3b4affd21ade`，**暂停 WIP** |
| 最后完整离线联验生产 | `62abfdb3039b64660f6aebce6ebd4af2a60e719a`；不是实机合格版 |
| 本地分支 | `codex/af-framework-skill-delivery-20260911` |
| 唯一获准推送目标 | `origin/codex/af-main-refactor-continuation-20260831`，普通快进；不碰 main 或其他成员分支 |
| 自动化 | 本轮再次只读核对 `af-7-8` 为 `PAUSED`，未恢复或修改计划任务 |
| 本轮变动 | 详细交接、原计划当前入口/第 15 节、总 HANDOFF、总台账、审计 JSON；无生产或测试实现修改 |
| 发布性质 | **开发分支上的 WIP 与审查文档交接，不是 Release、可覆盖游戏包或发布验收通过** |

远端：[专门重构分支](https://github.com/klfwdf/AnimusForge/tree/codex/af-main-refactor-continuation-20260831)。最终文档提交号和是否推送成功以本轮 Git 回执及远端 ref 为准，不用文档内自引用 hash 冒充发布确认。

适用双 Skill：`.claude/skills/animusforge-maintainer/SKILL.md` 与 `.agents/skills/af-core-framework/SKILL.md`。中文可用于提示词、解释、玩家文案；代码标识符与协议 ID 保持英文。稳定的是边界和外部承诺，不把主体算法、模块名单或本版只读能力写成永久限制。

## 3. 为什么“到了阶段 8”仍有前面阶段的缺口

| 原阶段 / 当前批次 | 准确解释 |
|---|---|
| 原阶段 0–1 | 起点、清单、构建流程已建立；仓库/依赖分发与原许可 HOLD 仍不能擅自勾完成 |
| 原阶段 2–3 | 历史 DONE 明确只指**设计完成**；现已落地一部分契约/runtime，不能据旧勾选推导完整 Host 已实现 |
| 原阶段 4–6 | 有存档身份保护、管线、Prompt/Action/Memory 边界及大量定向修复；完整 owner 提取、所有入口一致性与当前候选验收仍未闭合 |
| 原阶段 7 | 多数选定模块的 AF 侧接线已有；不等于所有组合、真实经济/AFEF/旧档和所有领域功能验收通过 |
| 原阶段 8 | 正在做边界修复、功能对照与清理准备；最终删旧、默认迁移和同一候选验收未完成 |
| 当前 B1 / P1 | 记忆可靠性已多轮推进；最新索引/封存是 WIP，整批未放行 |
| 后续 B2 / P2 | **9 月 13 日新计划的 B2 批次尚未启动**；不代表 Native/Scene/Courier 从来没有重构或测试 |
| 后续 B3 / P3、P4-01、部分 P5/P6 | 内部接缝与综合候选收口未进入；基础三组 ports/目录/只读 API 已存在 |
| P4 扩展 / P5/P6 出口 | 公共提交能力需确认范围；最终真实 Host/旧档/候选验收、删旧/默认/发布决策独立保留 |

总台账中旧 `ACTIVE`、`TODO`、`DONE` 是各日期切片记录。当前入口已明确覆盖它们，未篡改旧证据。

## 4. 对比最初 AF：究竟拆了多少

比较来源为[正式重构起点记录](../animusforge-baseline-2026-08-30.md)的 `d4cb1467`，不是已经重构过的 `182da1db`。本轮确认 `d4cb1467` 与父提交 `96a1c60f` 的 C# 内容无差异。

统计基于 Git 跟踪文件；大类行数包括空行/注释。不是全仓库源码总量，也不是完成率。

| 指标 | 原始 `d4cb1467` | 早期重构 `182da1db` | 当前 `c21523f8` |
|---|---:|---:|---:|
| 仓库根目录 C# 文件 | 289 | 292 | 319 |
| `Refactor/` C# | 0 | 34 | 48 |
| `Api/V1/` C# | 0 | 0 | 2 |
| 三个核心 owner 的额外 partial 文件，**不含主文件** | 0 | 0 | 19 |
| `MyBehavior.cs` 行数 | 59,365 | 59,431 | 58,795 |
| `ShoutBehavior.cs` 行数 | 38,870 | 39,835 | 39,883 |
| `CourierDeliveryBehavior.cs` 行数 | 9,859 | 10,501 | 10,797 |

当前 `Refactor/` 为 9 个 Contracts、13 个 Runtime、21 个 Adapters、5 个 Modules 文件。19 个 partial 为 MyBehavior 11、Shout 6、Courier 2。

**判断：接口和接缝拆分是真实的；主体大类拆薄仍明显不足。** partial 仍编译成同一类、共享私有状态，不能把“移到另一个文件”算成独立 owner。三大主文件合计从 108,094 行变成 109,475 行；期间含新功能/其他提交，不能简单据增长评判质量，但足以反驳“大类已基本清空”。

原始 AF 已采用“一模块 + Bootstrap + 1.3/1.4 唯一实现”的发布方式；本轮重构是**保持这项契约**，不是新发明该布局。

### 当前真实结构，而非纯目标图

```text
AnimusForge.dll（由 Bootstrap 选择对应游戏版本）
├─ 主体：MyBehavior / ShoutBehavior / CourierDeliveryBehavior + partial
│  ├─ 原 owner 仍负责大量资格、Prompt、标签、状态和副作用
│  └─ Refactor Contracts/Runtime/Gateway/Adapters 接入其中
├─ internal 制作组层：3 组 typed ports、13 个方法、原业务 adapters
│  └─ 政策 / 宴会 / GCCZ 原 owner 继续掌握业务和存档
└─ public Api.V1：版本/能力/目录查询
   └─ 提交对话、执行动作、写记忆、自定义注册尚未开放
```

- `LegacyInteractionPipelineComposition.Create` 通过 delegates 调用原规则/Prompt/后处理 owner；`Legacy` 名称不等于无用代码，当前仍被实际调用。
- `ModuleFrameworkRuntime.Initialize` 把 adapters 绑定和目录校验投影为 Ready；**不代表 Campaign 可接单、完整生命周期 Host、所有模块可独立卸载/恢复**。
- 三组 internal ports 是同 DLL 代码契约，不是外部 SDK。原业务类已有其他公开接口，不能说“AF 完全没有外部 API”；准确说法是**新的统一 V1 目前只读，旧公开面并存且未整体收口**。
- 主体应逐条提取真正的规则/Prompt/动作/记忆责任，保留唯一动作与事实提交者；不要为追求新目录增加第二个执行器，也不重写政策、宴会、GCCZ 玩法。

## 5. 功能复现矩阵：已有、保留和未证明分开

“离线通过”只代表相应 runner 的真实抽取代码/契约与替身边界，不等于真实游戏。

| 功能组 | 当前实现 / 已有证据 | 尚未闭合 |
|---|---|---|
| Native 普通/流式/主动开场 | 实际普通入口接 admission，再进原完整实现；已拆准备、完成、pending history、动作边界，有旧版对照与生命周期回归 | 不等于独立 opt-in 已替代普通入口；当前候选完整游戏交互、关窗/换目标/音频待验 |
| Scene 正文 | 真实主循环可走生成专用管线，正文后才进入原完整后处理；本轮 ChannelCutover 132/0 | 准备上游及全部实际游戏副作用不是这 132 项的证明范围 |
| Scene 标签/接力/旁听 | work item 准备/请求/完成与完整后处理已拆；单次完成、接力/旁听相关代码和抽取对照存在 | 实际多 NPC、移动/离场、动作失败与记忆/AFEF 的同一候选端到端验收 |
| Courier 回信/主动来信 | 仍保留会话/到达时提交语义，有 detached 后处理及入站完成边界 | **后台准备读取 live 对象/历史的源码问题仍在**；预生成不可提前当成已交付/已执行 |
| LLM / Prompt / 规则 | 共享 Gateway、消息/配置快照、前主后处理边界有接线；大量原规则构造仍由原 owner 负责 | 不能说已完全独立为新服务；各渠道真实上下文和规则开关组合仍需对照 |
| 标签 / ActionPlan | 契约、解析/白名单、原权威执行器接线已存在 | 未证明所有动作的重复、部分失败、未知结果都能统一恢复；排队/正文成功不能冒充动作成功 |
| Economy / Reward / Debt | Hero/Party/Merchant owner 与渠道 commit 已有离线证据；保留原领域算法 | 当前候选真实金币/物品/商人/债务及 AFEF 前后账本对照缺失，不能承诺资产完全一致 |
| 历史 / 三类摘要 / AFEF | `62abfdb3` 已联验主线程捕获、raw/上下文重验、晚结果拒绝、写入/编辑/导入边界 | 最新封存/素材 WIP 联验未完成；单大来源原子成本及完整旧档/live 验收仍待补 |
| 素材索引 / 封存维护 | `c21523f8` 素材 23 场景 + 7 反例、封存 30 场景的冻结记录存在 | 最终封存反例/严格 inverse/共享 suite/六项构建未闭合，不能当已验生产替换 |
| 展示 / TTS | 文本/播放队列/生命周期有局部回归；真实音频机制仍保留 | Task 完成不等于播放完成；真实气泡、口型、取消/退场 FIFO 需实机 |
| 周报 / 主动 NPC / Issue 等 | 既有 Gateway/owner 接线与部分专项修复仍在 | 未在本轮逐条重验所有业务、周期调度、失败降级和跨渠道事实 |
| 政策 / 宴会 / GCCZ 等 | AF 侧 3 组 typed ports 有真实 consumers 和 adapter 测试 | 不表示其玩法已重写或本轮负责重写；需模块组合/停用/失败隔离验收 |
| 旧档 / 配置 / ABI | `62abfdb3` 已有 146 SyncData keys / 36 behaviors、实际 DLL 元数据/双版本构建证据 | 身份不变≠旧档可加载且行为正确；当前 WIP 的同候选构建、档案往返与真实配置仍未验 |
| 新统一子 MOD API | V1 的 CatalogRead Available；其他 6 类能力 NotSupported 是真实契约 | 没有完整新提交/结果/取消/通知 SDK；首版公开范围待 D-A/D-B 确认 |

## 6. 本次确认的问题和优先级

### REV-01：Courier 准备线程边界缺口（高优先，源码确认，未修）

回信调用链：

```text
CourierDeliveryBehavior.cs:4518 Task.Run
  → PrepareAndGenerateCourierReplyOffMainThreadAsync :4527
  → 读取 session / ResolveRecipient / Hero.IsDead
  → EnsureCourierPersonaContextReadyAsync(...).ConfigureAwait(false) :4556
  → BuildCourierReplyGenerationRequestOnMainThread(...) :4561
  → :4668 起读取 MyBehavior 历史、目标、规则与 CharacterObject 等 live 状态
```

主动来信对应 `5019 → 5028 → 5066 → 5092`。`OnMainThread` 只是方法名，这条实际路径没有因此切回主线程；generation 检查也不是线程封送。

**影响判断：**确有违背当前游戏对象/owner 线程边界的读取；可能导致上下文竞态或读档/对象变化时异常。**本轮未在游戏复现某个具体崩溃，不能断言用户遇到的所有信使问题都由它造成。**

后续应在正确 owner 主线程捕获 persona 准备后的实际只读请求；worker 仅网络/解析；接受前校验 generation、当前会话和目标。分别验证回信、预生成、到达后、主动来信，不能把到达前的动作提前执行。

### REV-02：最新 WIP 尚未接回整套验证（交付阻塞，本轮重现）

- 旧版 53 点代码地图在其绑定的 `62abfdb3` 上 PASS。
- 同一地图对工作树检查 FAIL：`Stale source content: MyBehavior.cs`。
- 严格 inverse 对工作树 FAIL：`Unreviewed B1 declaration: private void TryRunCampaignMemoryMaintenance(`。
- **这是未完成集成审查的阻塞，不是已证明游戏编译失败或玩法 Bug。** 不能只刷新 hash 消除红灯，应解释精确 WIP 差异、补对应真实回归/故障反例后再更新门禁和地图。
- 本轮 ChannelCutover 仍 132 PASS，说明不能把某一门禁失败扩大成“所有测试都坏了”。

### REV-03：B1 全局性能门槛未通过（已确认限制，不是实机卡顿结论）

- 封存的 128 个便宜操作 / 8 个昂贵操作是**每调用**预算；现有调度可同一逻辑 Tick 调用 9 次，不能包装为每 Tick 128/8。
- raw 来源摘要、首次捕获、单 owner 净化、完成 owner 绑定检查、排序/Apply 等仍有原子工作。分段外围循环不能证明单条深记录有硬上限。
- 已验候选的 1000 行初捕获观察为 18.197 ms / 1,834,552 B，旧初捕获 13.690 ms / 1,579,704 B：**不能称首次捕获变快**。重复重验分配约 1.57 MB → 0.24 MB 是已证明的局部收益；非实机帧率。
- 若拟用 revision 替换完整 raw hash，必须先覆盖所有 writer：SyncData、导出/菜单的原地净化、Save 前嵌套写、直接 state/queue 写、跨实体迁移。只拦 Save 会漏变动。

### REV-04：目标架构仍有未实现范围（完成度问题，不冒充 Bug）

大类未真正拆薄、目录 Ready 不等于完整 Host、新 V1 不支持提交。正确处理是明确首版必需责任与延期项，而不是堆接口、假 Ready、开放 raw 标签或把原实现移进 legacy 文件夹算完成。

### REV-05：交接顶部与真实状态不一致（本轮已修正文档入口）

原 P0–P6 计划顶部仍写 `B1 ACTIVE / e77602f9 / 第12节`，末尾才写暂停与 `c21523f8`。本轮在根 HANDOFF、原计划和总台账顶部统一最新入口，保留历史记录。没有改动两份受保护用户草稿。

### REV-06：当前候选缺少明确的游戏问题复现与验收账本（发布阻塞）

制作组此前说“测试过没问题”是有价值的反馈，但未与**当前源码/DLL、具体场景、存档和日志**绑定。现有审查材料不足以证明最新 WIP 的 LIVE/SAVE/Economy/AFEF/音频/外部 DLL 全部通过。本轮没有私自启动游戏、读取存档或采集 QQ。

用户提到“有不少问题”，但本轮没有收到逐项症状、触发步骤和日志。**本文是架构/真实调用链与验证审计，不是穷尽所有游戏 Bug 的保证。** 后续按第 9 节先建立可复现的问题单，不拿增加测试数量代替修正玩家症状。

## 7. 已经修过什么，不要重做或误报成新问题

- `62abfdb3`：三类摘要主线程捕获、完整 raw + 有效上下文来源检查；重试/最终接受不再重复生成整份 Prompt；覆盖 raw 状态未净化差异和 absent / present-null。
- 相关此前提交：规划/清理按槽续跑；普通提交、代表编辑/导入的跨档晚回调保护；部分完成异常进入提示而非静默吞掉。
- Scene：正文生成与原完整后处理分开，保留接力/旁听、玩家输入去重和 speech 队列相关边界；本轮没有发现证据把这些已修问题重新宣告为未修。
- `c21523f8` WIP：素材源换表后孤立索引命中修复；七阶段封存/维护续跑、同日无 job 停住及替换/旧 key 等场景修复。**冻结测试通过不替代整批联验。**
- 部分 Apply 异常能通知/停止≠事务回滚、自动尾项恢复或全系统 exactly-once。已有实际副作用不能盲重放。

## 8. 当前生产代码坐标与责任注释

以下坐标均在 `c21523f8` 核对；一基行号范围是定位入口/相关片段，不表示整个方法已独立提取或全部验收。跨版本优先按符号定位。[机器审计索引](../audits/2026-09-13-af-stage-architecture-parity-review.json)含文件 hash、统计与本轮验证；**不覆盖旧已验代码地图**。

| 源码位置 | 符号 | 责任与边界 |
|---|---|---|
| `Refactor/Adapters/LegacyInteractionPipelineComposition.cs:75-78` | `public static InteractionRequestCoordinator Create` | 配置真实管线；规则、Prompt、后处理仍经 delegates 回到原 owner。 |
| `Refactor/Modules/TeamModulePorts.cs:7-15` | `internal interface IPolicyModulePort` | 同 DLL 政策 typed 契约；不重写政策算法。 |
| `Refactor/Modules/TeamModulePorts.cs:17-25` | `internal interface IGatheringModulePort` | 同 DLL 宴会 typed 契约。 |
| `Refactor/Modules/TeamModulePorts.cs:27-37` | `internal interface ISiegeModulePort` | 同 DLL GCCZ 接缝；玩法仍归原模块。 |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:22-58` | `internal static bool Initialize` | 装配与目录 Ready，不是完整 Campaign/Mission Host。 |
| `Api/V1/AfApi.cs:13-28` | `public static class AfApi` | V1 当前只有目录查询可用，六类写/提交/扩展能力 NotSupported。 |
| `ShoutBehavior.cs:18067-18075` | `public static Task<string> SubmitNativeConversationTextForExternalAsync` | 普通入口使用 admission/原完整实现；不是新 opt-in 的同义名。 |
| `ShoutBehavior.cs:20082-20085` | `private async Task<string> SubmitNativeConversationTextInternalAsync` | 原 owner 的完整 Native 主体仍在运行，不能整类删除。 |
| `ShoutBehavior.cs:17107-17110` | `public static Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInForExternalAsync` | 单独 opt-in 接口；不能据此声称普通入口已全部迁移。 |
| `ShoutBehavior.ScenePostprocess.cs:72-77` | `private static string CompleteSceneUnifiedActionPostprocess` | 后处理 work item 的单次完成边界。 |
| `ShoutBehavior.ScenePostprocess.cs:94-97` | `private Task<int> QueueDeferredScenePostprocessActions` | 完整后处理、接力与旁听相关责任，非另起缩水执行链。 |
| `CourierDeliveryBehavior.cs:4527-4562` | `private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync` | Task.Run 进入；await 后仍调用读取 live owner 的准备方法。 |
| `CourierDeliveryBehavior.cs:4668-4696` | `private CourierReplyGenerationRequest BuildCourierReplyGenerationRequestOnMainThread` | 名称为 OnMainThread 不提供封送；读取历史和游戏目标。 |
| `CourierDeliveryBehavior.cs:5028-5067` | `private async Task PrepareAndGenerateInboundLetterOffMainThreadAsync` | 主动来信后台准备；同样存在 live 读取。 |
| `CourierDeliveryBehavior.cs:5092-5120` | `private InboundLetterGenerationRequest BuildInboundLetterGenerationRequestOnMainThread` | 读取发信人、历史、规则等 live 数据。 |
| `MyBehavior.MemorySummaryInput.cs:338-343` | `private bool IsMemorySummaryInputCurrent` | 完整 raw 来源及有效上下文重验；不能仅用 Save 调用计数替代。 |
| `MyBehavior.MemorySummaryPlanning.cs:159-162` | `private async Task<MemorySummaryPlan> BuildMemorySummaryPlanAsync` | 跨槽规划和冻结计划；不是每条深记录的硬预算。 |
| `MyBehavior.MemorySummaryMainThread.cs:100-106` | `private bool TryApplyMemorySummaryMainThreadAction` | 主线程/current owner/generation 接受边界。 |
| `MyBehavior.cs:13705-13763` | `private void RecordEventSourceMaterial` | WIP 素材追加/更新，保留原重复键语义。 |
| `MyBehavior.EventSourceMaterialIndex.cs:16-25` | `private bool IsEventSourceMaterialIndexCurrent` | WIP source/map/list 绑定，适用已审计串行 writer。 |
| `MyBehavior.MemorySealing.cs:198-203` | `private bool ContinueDailyMemorySeal` | WIP 七阶段续跑；仍有原子尾步和每调用预算限制。 |
| `MyBehavior.cs:17741-17770` | `private void TryRunCampaignMemoryMaintenance` | WIP 同日续跑/完成回执；旧严格 inverse 在此拒绝未审变更。 |

其他确切调用点：Scene 主循环 `ShoutBehavior.cs:28164` 调用 `GenerateSceneShoutMainReplyAsync`；Native 的 `ShoutBehavior.NativeAdmission.cs:83` 进入原完整实现；框架装配/停止由 `SubModule.cs:60` / `:110` 调用。地图定位不是运行证明，具体触发仍按第 9 节实测。

## 9. 后续计划：先恢复可信候选，再做完整功能闭环

本次只写计划，未开始以下生产改动；完整验收任务沿用[原计划第 15 节](../phase8/af-core-precloseout-plan-20260913.md)。

| 顺序 | 一次完成的工作 | 出口，未满足不前进 |
|---|---|---|
| 1：B1 WIP 集成闭环 | 解释并审查 `62abfdb3 → c21523f8`；补封存正常/旧版/故障反例；更新精确 source adapter/代码图；同候选跑受影响 suite 和六项构建 | 当前源码而非旧候选的联合证据可复现；不能靠刷新 hash 假通过 |
| 2：B1 剩余责任 | 计量真实每 Tick/每 job/每 record 成本；解决实际深来源/原子尾步风险；保留来源变化拒绝、失败/部分结果和档代边界 | 合理游戏预算或经明确接受的有界限制；B1 才能离线整批放行 |
| 3：B2 三渠道完整闭环 | 优先 Courier 主线程准备；集中验证 Native/Scene/Courier 的上下文→正文→标签→执行→历史/AFEF→展示及失败路径 | 成功、拒绝、重复、取消、晚回包、部分动作、换目标/档/Mission 的真实入口对照 |
| 4：B3 制作组接缝与候选 | 梳理实际 callers；必要生命周期/错误隔离、能力贡献与权威 owner；不重写制作组玩法 | 启停/缺失/异常/组合不污染普通 AF，ports 有真实消费者而非空壳 |
| 5：已选 public 能力 | D-A/D-B 确认后做提交/结果/取消/生命周期或明确首版仅查询；复用同一完整主体链 | 独立 DLL 编译与实际加载；未知/缺失/版本不符/退场/回调线程语义准确 |
| 6：P5/P6 同候选收尾评审 | 双版本/Bootstrap、ABI/存档、真实游戏、性能、问题账本；逐符号删除候选、默认迁移与回滚方案 | 选定必需项都有证据；关键丢功能/重复副作用/串档问题关闭，进入 READY_FOR_CLOSEOUT_REVIEW |
| 最终另行决策 | 确认删旧范围、默认切换、打包/发布 | 不能因“当前阶段 8”或本次 GitHub 推送跳过上述门槛 |

### 玩家视角最小复现/验收包

每个问题一条：**版本/SHA + 游戏版本 + 存档副本 + 模块开关 + 前置状态 + 输入 + 应有结果 + 实际结果 + 日志**。用修复前确定失败、修复后同条件通过的证据结单。

| 场景 | 至少观察什么 |
|---|---|
| Native | 普通/流式/主动开场各一次；生成中关窗/换目标/读档；无串人、重复记录、晚回复覆盖 |
| Scene | 单 NPC/多 NPC 接力/旁听；玩家输入仅一份；旁听记忆、动作、TTS 顺序；NPC 离场与 Mission 结束 |
| Courier | 回信、提前准备、到达后、主动来信；准备/等待时读档或目标失效；未到达不提前扣款/执行/记事实 |
| Economy/AFEF | Hero、Party、Merchant 的金币和物品前后差值、债务 owner；成功只记真实事实一次，失败不记已发生 |
| 记忆维护 | 大历史、空/无效/重复素材、换表、同日续跑、来源在请求中变化、失败/重试、部分 Apply；确认不丢任务或覆盖新内容 |
| 编辑/导入/旧档 | 开窗口后读档再保存、同代记录改变、单 NPC/批量导入；原档备份与副本加载往返，未知字段/版本错误不破坏原档 |
| 制作组组合 | 普通 AF、不启用目标模块、单模块、组合、桥失败；无政策/宴会/GCCZ 上下文越界污染 |
| TTS/外部 API | 真正播放完毕与 Task 完成分开；退场不播旧音；独立子 MOD 的缺能力、解绑、版本和异常隔离 |

其他成员可以负责游戏侧证据；本主体任务负责定位 AF 接缝与修改影响。游戏部署/存档操作仍需相应明确授权。

## 10. 验证账本：不要把历史成绩贴到当前 WIP

| 层级 / 绑定版本 | 本轮或历史结果 | 不能证明什么 |
|---|---|---|
| 本轮 Git 原始对照 | `d4cb1467` 与 `96a1c60f` 的 C# 相同；表中文件/行数从 Git 重新计算 | 行数不是功能完成率 |
| 本轮当前源码渠道抽取回归 | ChannelCutover `passes=132 failures=0`；覆盖实际抽取边界，`live=NOT_RUN apiNetwork=NOT_RUN` | 未跑完整游戏链、真实网络、全部领域副作用 |
| 本轮代码地图 | 记录版 53 anchors PASS；工作树 FAIL | 坐标通过不是玩法通过；工作树失败不是编译失败 |
| 本轮严格 inverse | 在 `TryRunCampaignMemoryMaintenance` 未审声明处 FAIL | 这是集成门禁，不是有效旧版失败反例，也不能算期望业务红例 |
| 已验生产 `62abfdb3` | captured109 / business36 / planning24 / terminal85 / commit51 / admission54，相关故障反例；UI85 / history852 / native27 | 每项有替身边界，不等于所有调用者/游戏都验过 |
| 同上构建/身份 | Debug/Release × 1.3/1.4/Bootstrap 六项、API119、并发读取256、实际 DLL 元数据532、SyncData146 / behaviors36 | 不覆盖后来的 WIP，也不是实际旧档加载 |
| WIP `c21523f8` 冻结证据 | 素材23/0、7反例；封存30/0。旧封存17例时为12绿/5红，扩30后旧版/新mutation未重跑 | 共享 suite/strict/最终 Stage、深来源预算尚未全部验证 |
| 本轮未运行 | 全套产品构建、LIVE、真实 provider、SAVE、真实资产/AFEF、音频、外部 DLL 加载 | 不宣称当前可发布或无 Bug |

可复现命令（在仓库根目录；解释器/SDK 路径是本机工具，不是生产硬编码）：

```powershell
# 原始基线：应无 C# 差异
git diff --name-only 96a1c60f d4cb1467 -- '*.cs'

# 53 点地图：记录版 PASS；当前 WIP 预期显示 stale，不要修改 hash 掩盖
G:\Python310\python.exe -X utf8 -B .agents\skills\af-core-framework\scripts\verify_code_map.py
G:\Python310\python.exe -X utf8 -B .agents\skills\af-core-framework\scripts\verify_code_map.py --working-tree

# 使用新 output-name，避免覆盖本次证据目录
G:\Python310\python.exe -X utf8 -B tools\ChannelCutoverBoundaryTests\run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --output-name review-replay
```

严格 inverse 的最小复现，不改任何源码：

```python
from pathlib import Path
import importlib.util
p = Path("tools/MemorySummaryMainThreadBoundaryTests/source_parity.py")
spec = importlib.util.spec_from_file_location("b1_inverse_review", p)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.restore_memory_summary_source(
    "MyBehavior.cs", Path("MyBehavior.cs").read_text(encoding="utf-8-sig"))
```

实际日志在本地忽略目录 `.tmp/stage-architecture-review-20260913/`；索引记录 log SHA256，GitHub 上传的是源码、runner、统计与结论，不上传大二进制/本机依赖。新机器需按 `README_BUILD.md`、`docs/bannerlord_dual_module_output.md` 准备现有合法依赖；不保证 clone 后零配置可跑全部本机 suite。

## 11. 恢复与回滚边界

- 从 WIP 继续必须先确认当前 Git 状态和本篇，不因历史 ACTIVE 自行重启自动化。不要重新拉一个旧工作树来覆盖当前源码。
- 保留 `62abfdb3` 为最后已联验参照；`c21523f8` 为未完成工作现场。若决定撤销 WIP，先审查 `4a16b0be..c21523f8`，按新指示定向 revert/inverse；不 hard reset，不覆盖用户改动。撤销后的组合也要验证。
- 本次只有文档修改，若回滚本次文档须同时恢复根入口、计划、审计索引的一致性；不需要也不允许顺手回滚生产。
- 两份用户草稿 `docs/handoffs/2026-09-06-integrated-phase8-handoff.md`、`2026-09-06-team-brief.md` 保持原 hash、不暂存。
- 用户指定的 `docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md` 不上传，检查文件树和待推历史；新版简明说明另留本地忽略目录，不让 GitHub 文档依赖它。
- 不修改 GCCZ、其他工作树、第三方技能安装目录、游戏 DLL/ONNX 或真实存档；不改构建覆盖脚本。

## 12. 转发给制作组的短说明

> AF 目前在阶段 8 的主体收口，最新工作停在 B1 记忆可靠性，尚未整批验收。共享管线、部分渠道边界、制作组内部接口和只读子 MOD API 已有，但主体大类还没真正拆薄，公共提交 API 也没完成。最新源码是 WIP，不是可直接覆盖游戏的正式包。本次审查确认信使仍有后台准备读取游戏对象的线程缺口，最新记忆封存/索引也需完成联合验证。下一步先闭合 B1，再集中做三渠道完整功能与内部接缝对照。请把遇到的问题按“版本 + 场景 + 操作 + 预期/实际 + 日志”提供，尤其是信使、多人对话、真实资产/AFEF 和读档；只说以前测过不能替代本候选验收。

相关入口：[暂停现场](2026-09-13-af-automation-pause-handoff.md)、[后续计划](../phase8/af-core-precloseout-plan-20260913.md)、[已验 B1 证据](../audits/2026-09-13-b1-context-admission-candidate.json)、[WIP 冻结索引](../audits/2026-09-13-af-pause-snapshot.json)。
