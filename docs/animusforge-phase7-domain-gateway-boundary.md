# 阶段 7：领域 LLM Gateway 接入边界

## 当前切片：Town Ambient

`TownAmbientAiClient` 仍由自身负责 Town Ambient 的开关、回应者快照、速率
限制、session token budget、48 项回复缓存、多人 JSON 数组解析和失败后的本地
降级。它不再自行创建 HTTP 请求，而是在发送边界把稳定的 role/content 字符串
复制为 `PromptPackage`，交给 `LegacyConfiguredChatGateway`。

共享 Gateway 负责：

- 读取调用者提供的 endpoint/model 和发送边界 credential resolver；
- 标准 chat 请求 JSON、鉴权 headers、超时与 cancellation；
- `LlmApiCompat` 的 endpoint/request 兼容处理和 assistant text extraction；
- 将 HTTP/空回复/取消/异常映射为 `LlmGenerateResult`。

Town Ambient 仍负责：

- `replies` / `responses` 等 JSON 结构的 Town 专用解析；
- 每个回应者的短句清理、缓存和预算记账；
- 将 Gateway 失败转换为环境 AI 的本地降级结果。

## 边界与安全

- API key 只存在 `SendAsync` 的局部变量和 Gateway 的 resolver 返回值，不进入
  `LlmProviderSnapshot`、`PromptPackage`、存档或普通日志。
- Gateway 不接受或保存 Hero、Agent、Settlement、Campaign 等 live 对象。
- 该切片没有把 Town Ambient 变成三渠道对话，也没有改变其可选开关；默认路径
  和 TTS 路径保持原样。

## 性能

请求仍由 Town Ambient 自己的单 in-flight 门闩、每分钟请求上限、session budget
和 48 项回复缓存控制。Gateway 只在线程池的一次发送调用中对已冻结消息做线性
复制，不进入 Tick，不扫描游戏对象。

## 验证与未验证

- `InteractionPipelineContractTests`：`39 cases PASS`。
- 1.3、1.4、Bootstrap project-local Stage：均 `0 warning / 0 error`。
- 真实 Town Ambient HTTP、配置 reload 与游戏内场景回放：`NOT-RUN`。
- 回滚：移除 `TownAmbientAiClient` 对 Gateway 的调用并恢复原 `SendAsync` HTTP
  块；旧 Town Ambient 入口未被删除。

## World Diplomacy

`WorldDiplomacyBehavior` 的排队发送点现在先把领域生成的 `JArray` 复制成
`PromptPackage`，再调用 `LegacyWorldDiplomacyLlmGateway`。适配器复用旧
`WorldDiplomacyLlmClient`，因此 route 选择、凭据 authority、重试/backoff、
thinking plain fallback、stale generation、token/cache/truncation 统计仍由
领域 owner 负责。共享 `LlmGenerateMetadata` 只携带非敏感状态和计数，供原作业
结果继续显示/处理；不携带 response body 或 API key。

当前调用方 cancellation token 已贯穿领域请求、thinking plain retry 和 retry
backoff；调用方主动取消现在返回 shared `Cancelled`，不会被旧的 hard timeout
转换，也不会在取消后继续发起下一次 retry。hard timeout 仍保留原来的
`RetryableFailure`/timeout 降级语义。

## Policy 与 Persona

- `LegacyPolicyLlmGateway` 为 NPC Policy 和事件/叛乱 API 提供 shared contract
  adapter，保留 `PolicyLlmClient` 的 profile、JSON response format、兼容探测、
  限流和重试 authority。NPC ruler 的 draft/effect/repair、玩家政策的
  main/postprocess/repair，以及 `KingdomStrategicProfileBehavior` 的建国卡生成，
  均已通过该 adapter；各调用方的测试 override 仍优先。
- 无名 NPC Persona 生成已通过 `LegacyShoutNetworkGateway`，但 Persona JSON
  解析、文件/存档写入仍留在 `ShoutUtils`。

## AI 错误分析与 XihaiAction 辅助分类器

`AiErrorAnalysisInquiry` 的辅助 API 请求，以及 XihaiAction 的 SceneActions、NPC
同意和 BattleSpeech 分类器，现均可通过共享 `LegacyConfiguredChatGateway` 发送。
调用方/分类器继续拥有各自的提示词、闭集协议解析、动作白名单、single-flight、
取消和失败 fallback；XihaiAction 在辅助配置缺失或 Gateway transport 创建失败时
保留原 `ShoutNetwork.CallApiWithMessages` 反射 transport。

- Gateway DTO 只接收 endpoint/model 和冻结的 role/content；API key 只由发送边界
  resolver 闭包提供，不进入 DTO、存档或普通日志。
- 分类器在每次发送开始时复制当前辅助配置，进行中的请求不读取后续 reload 的配置。
- 该接入不改变 SceneActions 的动作/同意协议、战术权限、反射兼容条件或默认三渠道。
- 错误分析继续使用脱敏后的标题/详情，并在原非阻塞错误展示链路中显示结果。

## 尚未完成

World/Policy/Persona、错误分析和 XihaiAction 的真实 provider HTTP、旧存档加载后的运行时回放和游戏内
验证仍为 `NOT-RUN`。Knowledge/RAG、主动 NPC、辅助分类器和火山 TTS 的完整领域
验收仍需分别完成；不能以本阶段的编译成功代替这些验收。

## Dedicated TTS

`TtsEngine` 的火山 V1 网络请求已移动到 `LegacyVolcTtsGateway`。请求 DTO 只含
endpoint、应用/资源/音色标识、文本和音频参数；token 作为调用参数在发送边界
提供。Gateway 保留旧的 `Authorization: Bearer;token`、`X-Api-*` header 映射、
V1 payload、provider `code=3000` 成功条件和 base64 解码。音频播放、WAV/PCM
解析、lip-sync、Agent 校验、队列与失败通知仍归 `TtsEngine`。

这一步不改变 TTS 开关或语音映射，也没有把二进制音频放入 LLM Prompt/存档。
真实火山服务、取消时序和游戏内播放尚未验证。

## Event / WeeklyReport

`MyBehavior` 的周报批量、单组、完整周报和第 0 周短摘要调用现在在显式周报
入口使用 `LegacyConfiguredChatGateway`。调用点先复制最终的 system/user
prompt 为不可变 `PromptPackage`，再由 Gateway 负责标准 OpenAI-compatible
请求、鉴权、超时、thinking 控制、400 plain retry 和 assistant text extraction。

周报 owner 仍负责批量组织、每分钟限速、三次重试、JSON/标记解析、失败回退、
手动重试和主线程事件记录写入；没有改变周报调度、存档结构或 UI 时序。
EventAndRebellion 专用配置和缺省回退仍由旧配置解析逻辑决定，凭据只通过发送
边界 resolver 提供，响应正文不进入共享 contract DTO。非周报的 EventAndRebellion
调用仍保留旧通用入口，三渠道默认路径也未切换。

性能边界：周报按既有批次和每分钟预算运行；Gateway 只对每次调用的两条字符串
消息做一次线性复制，不进入 Tick 扫描，不持有 Hero、Settlement 或其他游戏对象。

验证：7 个阶段 Python runner、InteractionPipeline `39 cases PASS`、XihaiAction
Core `88 passed / 0 failed`、GiveAssetTagCodec `80557 assertions PASS`，以及
1.3/1.4/Bootstrap unified stage 均 `0 warning / 0 error`。真实周报 HTTP、旧存档
和游戏内周报回放仍为 `NOT-RUN`。

## Memory 压缩与主动 NPC

`MyBehavior` 的 Daily Memory 压缩、重大履历摘要和 Memory Overview 三个后台
非流式入口现在也通过 `LegacyConfiguredChatGateway` 发送。入口在已有资格检查、
来源裁剪和队列 worker 边界复制最终 system/user 字符串；Gateway 使用 Auxiliary
配置并保持 force-thinking-disabled、配置化 token/temperature、超时和 assistant
提取。原 owner 继续负责重试、JSON/标签解析、stale/obsolete 判断、Memory/AFEF
存储和主线程提交。

主动 NPC 审查确认没有独立的 HTTP/LLM transport：它负责低频增量候选扫描、需求
资格和会面状态机，开场 prompt 交给 Native/Scene/Courier facade，摘要交给上述
Memory owner。因此不新增重复 Gateway 或平行历史/存档协议。

性能边界：主动 NPC 候选扫描保持现有批量/预算切片，不在每个 Tick 做全量扫描；
Memory 请求只处理已冻结字符串，Gateway 不持有 Hero 等 live 对象。真实 Memory、
主动 NPC HTTP、旧存档和游戏内回放仍未验证。

## Persona / Promoted Companion

NPC Persona、升格同伴 Persona 和升格同伴技能的三个 Auxiliary 后台生成入口已
通过同一 `LegacyConfiguredChatGateway`。发送边界只复制最终 system/user 字符串；
Persona/技能 owner 仍负责事实素材、JSON 解析、失败 fallback、stale 检查以及
主线程的 Persona/技能存储。普通 Persona 生成沿用 Auxiliary 的 thinking 设置和
plain retry；Memory 摘要则继续 force-thinking-disabled，二者不共享可变请求配置。

该接入不改变 Persona/技能的存档字段、现有生成资格、重试冷却或三渠道默认入口。
真实 Persona provider、旧存档读取和游戏内生成人设/技能仍为 `NOT-RUN`。

## Kingdom Rebellion Naming

叛乱王国命名的 EventAndRebellion 请求现在在专用命名入口使用 shared
`LegacyConfiguredChatGateway`。入口只把已组装的 system/user prompt 复制为
不可变 `PromptPackage`，Gateway 负责 URL/model、鉴权、60 秒请求超时、thinking
控制、400 plain retry 和 assistant text extraction；命名 owner 继续负责外层
三次重试、`[NAME]/[SHORT]/[LORE]` 解析、重复国名校验及王国创建主线程边界。

请求超时、stale、空回复和 provider 错误仍只会使本次命名失败并中止本次建国，
不会改变王国存档结构或其他 LLM 模块。真实命名 HTTP、旧存档和游戏内叛乱回放
仍为 `NOT-RUN`。

## External Auxiliary Facade / PlayerNotoriety

`MyBehavior.CallAuxiliaryApiTextForExternal` 现在复用 Auxiliary Gateway helper，
其现有生产调用方 `PlayerNotorietyBehavior` 的摘要请求因此不再直接进入旧的
通用 HTTP 方法。外部 facade 仍返回纯文本；PlayerNotoriety owner 仍负责摘要
解析、周期调度、stale 判断、存储和失败降级/提示。

该 facade 保持 force-thinking-disabled 和原有失败弹窗语义。Gateway 只在发送
边界解析凭据并接收冻结字符串，不携带 Hero/live 对象；其他默认三渠道入口未切换。
真实 PlayerNotoriety provider、旧存档和游戏内摘要回放仍为 `NOT-RUN`。

## 直接 transport 盘点（2026-08-30）

本轮对生产 C#（排除参考源码、`bin/obj` 和 Refactor adapter）完成直接 LLM transport 扫描：

- 已由 shared Gateway 或领域 Gateway adapter 覆盖：Policy、World Diplomacy、Town Ambient、TTS、AIConfigHandler 的 ACTION/Simple Dialogue/辅助入口，以及既有 Persona、周报、Memory、叛乱命名和辅助分类器切片。
- 暂保留 legacy facade：Scene/Native/Courier 主回复及流式回复仍由 `ShoutNetwork` 承接，默认路径未切换；这是计划中的兼容边界，不视为漏迁移。
- 继续由配置/向导 owner 持有：`DuelSettings` 和 `ModOnboardingBehavior` 的用户主动 API/模型连通性验证，需要保留逐目标提示、响应诊断和配置写入时序。
- `MyBehavior.CallUniversalApiDetailed` 目前没有生产调用者，仅被同文件旧私有包装引用；先保留并记录为未删除 legacy dead path，不凭扫描结果删除。

下一项准确任务：将本地可控 provider 回放扩展到保留的 `ShoutNetwork` SSE 主/流式 facade，验证增量/最终文本不重复、取消/stale 不提交、thinking retry 与 ACTION 隔离；仍不切换默认三渠道。

## ShoutNetwork 流式 Gateway 契约

`LegacyShoutNetworkGateway` 现在同时实现 `ILlmGateway` 和可选的
`ILlmStreamingGateway`。流式契约把旧 `ShoutNetwork.CallApiWithMessagesStream`
包在不可变 `LlmGenerateRequest` 外：

- `onDelta` 只接收增量观察通知；返回的 `LlmGenerateResult.RawText` 由完成回调
  提供的最终可见文本确定，避免增量和完成文本重复提交。
- 只允许 `InteractionStage.MainReply`；后处理不能走流式 facade，会返回
  `stream_stage_not_supported`，动作仍只能走独立后处理契约。
- 旧 ShoutNetwork 继续负责 API 配置、SSE 解析、动态玩家名过滤、空回复重试、
  stale 和取消行为；本契约不把 live 游戏对象、API key 或 response body 带入公共 DTO。
- 该能力是 opt-in wiring seam，默认 Scene/Native/Courier 主回复和流式入口未切换。

性能边界：每次请求只复制一次已冻结的 role/content 消息；增量回调不进入 Tick，
不做规则扫描、不重新解析游戏对象。

当前验证：`tools/ConfiguredChatGatewayReplayTests` 已使用本地可控 provider 运行通过，覆盖 success、thinking plain retry、5xx retryable failure、cancellation 和 credential boundary；`tools/ShoutNetworkSseReplayTests` 已通过旧 `ShoutNetwork` SSE 传输边界的 success、thinking plain retry、5xx retryable failure、cancellation、stale、增量/最终文本一致性和 ACTION 隔离回放；`InteractionPipelineContractTests` 已通过 `40 cases`，覆盖三渠道 opt-in facade 的 identity、三阶段、commit 和 user/assistant 历史边界；`tools/ProductionOptInEntryReplayTests` 已直接加载生产 1.4 stage，验证 Native/SceneShout/Courier capture 与 ports factory 的 fail-closed 和 identity 边界；双版本/Bootstrap 构建及纯契约测试也通过。已初始化游戏 host 的真实生成/主线程 commit、旧存档和游戏内回放仍为 `NOT-RUN`。下一步是接入可控 provider 到已初始化 host，验证真实 commit/回退时序，仍不切换默认三渠道。

## Knowledge/RAG Gateway owner 边界（2026-08-30）

`KnowledgeLibraryBehavior` 的 RAG 短句生成现在通过 `LegacyKnowledgeRagGateway` 进入 shared configured transport。Knowledge owner 继续负责 prompt、`RagShortTextGenerationMaxTokens` 限制、候选解析、去重、知识数据写入和确定性 fallback；Gateway 只负责 provider 配置、禁用 thinking、取消和凭据发送边界。`Postprocess` 会被明确拒绝，避免 RAG 生成误触 ACTION 管线。

`tools/KnowledgeRagGatewayReplayTests` 已通过 success、empty、provider failure、caller cancellation、非 MainReply exclusion 和 credential boundary。真实知识库、旧存档及游戏内回放仍为 `NOT-RUN`。

下一项：Courier inbound/reply 的可控 provider 回放和主线程 commit/历史边界；默认三渠道仍不切换。

## Courier inbound/reply 生产 opt-in host 回放（2026-08-30）

`tools/ProductionCourierHostReplayTests` 直接加载 project-local 1.4 implementation，并使用生产 `CourierDeliveryBehavior` 的 capture 与 `CreateCourierDetachedPortsForExternal` 接入 `DetachedInteractionHost`。回放验证了 reply 的 main/postprocess/commit，以及 inbound 的 main/commit。内存记录确认 reply 为 `user → assistant`，inbound 只写 `assistant`，不会把 NPC seed 伪造成玩家历史；取消、stale 和缺失 provider 均不会错误提交，缺失 provider 只走 legacy fallback。

回放结果：`productionCourierHostReplay courierPorts=1 replyMain=1 replyPostprocess=1 replyCommit=1 inboundMain=1 inboundCommit=1 inboundNoUserSeed=1 cancellationBoundary=1 fallbackIsolation=1`。真实 Bannerlord host、旧存档和游戏内 Courier 回放仍为 `NOT-RUN`；默认 Courier 状态机未切换。

下一项：审查剩余 `MyBehavior.CallUniversalApiDetailed` 与 auxiliary/event 入口，确认 shared Gateway 覆盖和 owner 边界。


## Configured Chat Gateway streaming and Universal legacy path (2026-08-30)

`LegacyConfiguredChatGateway` 现在同时提供非流式和通用 SSE 流式能力。流式
transport 只把增量作为观察回调，`LlmGenerateResult.RawText` 仍是唯一权威的
最终文本；`ConfiguredChatGenerationExchange` 只在 adapter/legacy caller 边界保留
状态码、响应采样、请求体和 control mode，不进入公共 contract、存档或普通日志。

`MyBehavior.CallUniversalApiDetailed` 已改为构造不可变 `PromptPackage`、
`LlmProviderSnapshot` 和 `TraceContext` 后调用该 Gateway，保留原 `ApiCallResult`
映射、stale generation、限流标记、错误详情和 token 统计。旧方法没有生产调用点，
但现在即使被历史包装重新使用，也不会恢复第二套直接 HTTP/SSE transport。

验证：`ConfiguredChatGatewayReplayTests` 覆盖非流式成功、流式 delta/final parity、
thinking plain retry、5xx、取消和 credential boundary；Configured validation、
Knowledge/RAG、Primary Gateway replay 及 1.4 direct/1.3/1.4/Bootstrap unified stage
均通过。默认三渠道仍未切换；真实 provider、游戏内 host、旧存档和 Universal
legacy wrapper 的实机调用仍为 `NOT-RUN`。下一项是为已初始化 host 接入可控 provider，
验证真实生成、主线程 commit 与 legacy fallback。


## Production configured Host equivalent fixture (2026-08-30)

`tools/ProductionConfiguredHostReplayTests` 直接加载 project-local 1.4 stage 中的
生产 `AnimusForge.dll`，不使用 fake `ILlmGateway`，而是通过 loopback provider
实例化 `LegacyConfiguredChatGateway`、`LegacyChannelInteractionFacade` 和
`DetachedInteractionHost`。NativeConversation、SceneShout、Courier 三种
channel identity 都完成 main/postprocess 请求、主线程 commit/history、provider
failure fallback 和 caller cancellation 回放。

成功路径确认每个 channel 只写入 `user → assistant` 历史，失败和取消不提交；
所有 HTTP 请求的 Authorization 只由 gateway credential resolver 在发送边界提供。
该 fixture 证明生产程序集和 Host 生命周期可以在等价可控环境闭环，但不等同
真实 Bannerlord campaign/mission host：live Agent/Hero、动作执行、AFEF、旧存档
和游戏内三渠道仍为 `NOT-RUN`。下一项按计划进入 Economy/Reward/Debt 主线程
replay port 与 ActionPlan 当前状态复核。
