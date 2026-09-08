# Courier 前置准备线程边界：确认清单与后续方案

- 日期：2026-09-09。
- 基线 HEAD：`5ce8767a3922e17829eafa04f84a80ceb86d2a7a`；分析对象为其上本轮集成工作树。
- 状态：**NOT_FIXED / DESIGN_REQUIRED**。本文不是修复、验收通过或整个重构项目完成声明。
- 本次仅保留另一项已批准的小修：`CreateCourierDetachedPorts` 的 visible normalizer 调用统一 `NormalizeCourierDetachedVisibleReply(rawText)`（由集成 owner 提供：准备/错误拒绝、剥离动作标签及原正文清理）；原始回复仍供动作后处理 owner 使用，Inbound 自身清理阶段不变。
- **没有修改**本文所列的 MyBehavior、AIConfigHandler、KnowledgeLibraryBehavior 共享准备实现；没有引入整段主线程同步网络、`Thread.Sleep` 或伪异步等待。

## 一、当前双向入口与事实边界

`G:\AFMOD\AF-REFACTOR\CourierDeliveryBehavior.cs`：

- 回信：`Begin...` → `Task.Run` → `PrepareAndGenerateCourierReplyOffMainThreadAsync` → persona → `BuildCourierReplyGenerationRequestOnMainThread` → 正文生成。
- 主动来信：`BeginInboundLetterGenerationOnMainThread` → `Task.Run` → `PrepareAndGenerateInboundLetterOffMainThreadAsync` → persona → `BuildInboundLetterGenerationRequestOnMainThread` → 正文生成。
- 两个名字带 `OnMainThread` 的 builder 当前仍被后台准备方法直接调用。名称不构成调度证据。
- 后台方法还直接读 session、解析 Hero、检查 `IsDead`，生成期间跨 await 持有会话/目标引用。已有 generation 检查不能代替每个主线程阶段的会话、目标、owner 身份检查。
- 同步公开 `CaptureCourierReplyRefactorEnvelopeForExternal` / `CaptureCourierInboundRefactorEnvelopeForExternal` 也会调用这两个 builder。解决方案必须核对这些仍保留的接口调用者，不能只修默认入口，或在主线程同步捕获时偷偷增加网络等待。
- 本轮主 Host capture / action postprocess 的主线程改进**不等于**更早的上述前置准备边界已经解决。

## 二、四条执行链（网络与非网络分开）

### 1. 辅助规则前处理：真实网络，且存在二次调用风险

`G:\AFMOD\AF-REFACTOR\MyBehavior.cs`：

`RunCourierRulePreprocessForExternal` → `RunCourierRulePreprocessInternal`

- 输入阶段含规则排除、GCCZ bypass、目标王国/Hero/Character、六项 runtime target、semantic context 和 NPC 最近发言。
- 这些包含活跃游戏对象/场景读取，应由主线程 owner 捕获。

`G:\AFMOD\AF-REFACTOR\AIConfigHandler.cs`：

`GetGuardrailSemanticRuleHitsForPreprocess` → `TryGetGuardrailEvalSnapshot` → `TryBuildAuxiliaryGuardrailEvalSnapshot` → `TryCallAuxiliaryRuleRouterApi` → `TryCallAuxiliaryRuleRouterApiOnce` → `LegacyConfiguredChatGateway.GenerateAsync(...).GetAwaiter().GetResult()`。

- 实际网络为辅助路由 API；不是仅本地关键词匹配。
- 构造候选、场景历史、运行时资格与缓存 key 发生在同一同步链中。
- API 错误和格式错误保留用户重试/放弃路径；格式重试用 `LlmRetryPrompt.PromptRetryBlocking`。合法空 topic 会按现有实现进入 semantic fallback。不得改成无条件接受空命中，也不得删除重试。
- `BuildShoutPromptContextForExternalInternal` 在 auxiliary 模式仍先调用 auxiliary，再合并 `forcedPreprocessRuleIds`。当前传入 forced 命中**不保证不重跑网络**；不得依赖全局 cache 恰好命中作为线程修复。
- semantic fallback 的 ONNX/向量/rerank 是本地计算，但它当前也从实时规则注册表与资格函数取得输入。必须先分离游戏输入，不能把“没有网络”误认为“任意线程都安全”。

### 2. NPC 人设：真实异步网络，落盘阶段未独立调度

`G:\AFMOD\AF-REFACTOR\CourierDeliveryBehavior.cs`：

`EnsureCourierPersonaContextReadyAsync` → `MyBehavior.EnsureNpcPersonaGeneratedForExternalAsync`。

`G:\AFMOD\AF-REFACTOR\MyBehavior.cs`：

`EnsureNpcPersonaGeneratedAsync` → `GenerateNpcPersonaAsync` → `CallAuxiliaryGatewayDetailed` → `LegacyConfiguredChatGateway.GenerateAsync`。

- 准备阶段读取 Hero 身份、已有人设、事实与生成条件。
- API 返回后再次读取当前 profile，执行 `SaveNpcPersonaProfile`，并更新 in-flight / cooldown 状态。
- 从 Courier 后台入口调用时，没有独立的主线程 profile-complete 调度；不能因使用 await 就声称后续自动回到游戏主线程。
- 原实现已有 generation 失效检查、去重锁、失败 cooldown、补全缺失字段、重新生成时保留 VoiceId 的规则。拆分必须逐项保留。
- `LlmRetryPrompt.ShowFailurePopup` 自身已有主线程发布逻辑，本文不将其笼统判为未调度 UI；确认缺口是游戏输入与 profile owner 阶段。

### 3. 历史/压缩记忆：条件性真实网络，不能整段搬主线程

`G:\AFMOD\AF-REFACTOR\MyBehavior.cs`：

`BuildHistoryContextForExternal` → `BuildHistoryContext` → `BuildHistoryContextById` → `BuildCompressedMemoryContextById`。

- 加载 overview、压缩块、drafts，执行本地候选召回。
- 小于注入上限时可直接选择；较多候选时调用 `TrySelectMemoryIdsWithPreprocess`。
- 该方法构造包含日期、年龄、场景、候选 DisplayId 的 prompt，然后调用 `AIConfigHandler.TryCallAuxiliarySimpleDialogue`。**这是另一类实际网络调用**。
- 现有 mode 2 使用 `Task.Run` 后立即 `memoryTask.Wait()`，仍同步阻塞调用线程。把上层 builder 放到主线程不会让它变成非阻塞。
- 必须保留最新记忆块预留、有效 ID 白名单、选不足时的补全、排序、AFEF 行、不同 mode、错误 popup 和格式异常语义；不能缩减记忆或跳过选取来制造“不卡顿”。

### 4. Lore：正常 Courier 链为本地计算，不是在线 RAG API

`G:\AFMOD\AF-REFACTOR\AIConfigHandler.cs`：`GetLoreContext` →
`G:\AFMOD\AF-REFACTOR\KnowledgeLibraryBehavior.cs`：`BuildLoreContext` → `BuildLoreContextInternal` → `CollectCandidateRules` → 本地 embedding / ONNX rerank / 条目筛选及文本渲染。

- 正常 Courier 路径包含 Hero/王国/定居点、玩家外观/人设可见性、关键词插槽和规则版本缓存读取。
- 本地计算仍有大库耗时和共享数据版本一致性风险；需区分捕获、CPU 计算和依赖游戏对象的最终渲染。
- 文件内确有 `RequestLlmTextOnce` 的同步 Gateway，但已核对其调用来自 `GenerateRagShortTextsByLlm` 的知识编辑器短文生成，**不能据此声称正常 Courier lore 调用了在线接口**。
- 当前 `usePrefetchedLoreContext=true` 且 prefetched 为空时仍会走 fallback 查询。将来需要“已计算为空”与“未计算”不同状态，不能把空字符串当未准备。

## 三、不改变规则的接口方案（尚未实现）

1. **建立请求身份与阶段所有权**：主线程验证 behavior 实例、generation、sessionId、发送方/接收方 ID、方向、生成标志；捕获来信、事实及配置。每次 owner 完成前重新验证，拒绝跨读档/替换目标/重复请求。
2. **记忆选择 prepare/request/complete**：主线程加载并复制候选身份与所需文本；本地重排在无游戏对象快照上运行；网络仅消费冻结消息与 provider 参数；主线程按原 ID 白名单、数量和排序规则完成历史文本。小候选 direct 路径仍不请求网络。
3. **规则路由 prepare/request/complete**：主线程冻结排除、可选 topic、runtime context、历史和缓存身份；网络仅使用字符串 prompt；复用真实 parser 和原 fallback。重试使用既有 `PromptRetryAsync` 并保留用户选择，不同步等待主线程。semantic fallback 使用已捕获规则输入，结果发布前检查当前 owner。
4. **显式已完成前处理结果**：在 `BuildShoutPromptContextForExternalInternal` 增加向后兼容的准备结果入口；未提供时保留现有调用行为，提供时无论命中为空/非空均不再次请求。完整 rule eligibility / 排除 / 动作资格仍归原 owner，不另写一套规则。
5. **persona 阶段拆分**：保留现有 public API 包装，内部形成主线程 prepare、字符串 Gateway request、主线程 profile-complete。将 in-flight/cooldown 退场规则纳入同一请求身份，旧请求不得清理新请求标记。
6. **Lore 独立性能边界**：先测量实际候选库规模和冷/热延迟；只将可证明不访问游戏对象的本地计算放后台，主线程保留可见性与插槽渲染；区分已准备空值与未准备，不引入新的在线请求。
7. **双向 Courier 统一接入**：复用已有 `RunCourierOwnerPhaseAsync<T>` 调度；后台编排只传递请求 ID、不可变文本/数据快照，不直接读取 session/Hero。公开同步 envelope capture 必须单独核对：消费既有 prepared snapshot 或增加明确 async 入口，不能同步等网络，也不能悄悄返回缩水 prompt。

初步影响约 18–22 个方法（数量是设计估计，不是已完成变更），跨 Courier、MyBehavior、AIConfigHandler，Lore 是否需生产拆分取决于性能实测。应作为独立可回滚里程碑，不能在六项窄缺陷修复收尾中仓促扩大。

## 四、验收门槛

- 直接提取真实生产方法，先保留红基线：后台游戏 getter/profile-save 线程见证、阻塞网络期间主线程 tick 见证、aux 调用次数与空 prefetched 结果见证。
- 使用真实隔离 main dispatcher 与真正后台任务，不以同线程 stub 假称线程安全；所有游戏读取/写入记录物理线程 ID。
- 回信及主动来信都覆盖：正常/无须人设/已有并发人设/空命中/非法 JSON/用户重试与放弃/网络错误/记忆 direct 与各 mode/本地 semantic fallback。
- 读档、session 结束、目标替换/死亡、重复回调、取消、owner 被替换及迟到网络：不重复请求、不写旧 profile/记忆、不提交旧动作、不清理新请求状态。
- 逐字段对照 prompt、规则命中、history、AFEF、返回值、异常、重试次数、库存及动作 owner，不只验证可编译。
- 必须有变异反例：断开 owner 调度、重复前处理、空值触发重查、移除 generation/target 校验、跨请求清理、后台 profile 落盘时，测试确实失败。
- 冷/热路径主线程耗时和网络挂起期间可响应性达标；1.3/1.4、Bootstrap 和正式 Courier/Memory/Persona 回归通过后，再交制作组做实机/旧档/多人来信验证。

**退出结论：NOT_FIXED。** 本文仅交代真实缺口、边界和验证计划，不提高 LIVE/SAVE 验收状态，不授权删旧、切默认或部署游戏，不证明整个重构项目已收尾。
