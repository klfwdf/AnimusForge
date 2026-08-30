# Handoff：AF 重构准备阶段

- 日期：2026-08-30
- 当前分支：`codex/af-full-llm-refactor`（canonical worktree：`F:\AF测试重构`）
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`（包含已单独推送的模块版本提交）
- 当前准备分支已推送提交：`23449caf0c6d1e38235d752f8fa1b0975dce17a5`（重构地图与 owner matrix）
- 当前状态：IN PROGRESS（阶段 4 Persistence/Profile/Config 与阶段 5 Conversation 统一管线 VERIFY；阶段 6 Memory/AFEF、Action 协议和 Economy/Reward/Debt contract VERIFY；真实 HTTP/游戏内验收和默认切换未完成）
- 最近验证：InteractionPipeline `39 cases PASS`、Persistence/Profile/Config runner、阶段 2/3 runners、1.3/1.4/Bootstrap 和 project-local unified stage 均 PASS；未部署到游戏目录。
- 当前任务：在已初始化 host 中验证 Native/SceneShout/Courier opt-in 的真实生成与主线程 commit；AI 错误分析、XihaiAction、World Diplomacy cancellation 和 Dedicated TTS 本地回放切片已完成，真实游戏内回放仍待后续。默认路径保持不变。

## 阶段 7 继续进度

- 已新增 `Refactor/Adapters/LegacyConfiguredChatGateway.cs`，集中处理标准
  OpenAI-compatible chat 的请求、鉴权、超时、取消和 assistant text extraction。
- `TownAmbientAiClient.cs` 已接入该 Gateway；Town 专用开关、缓存、速率/预算、
  多人 JSON 解析和失败降级保持在原 owner。具体边界见
  `docs/animusforge-phase7-domain-gateway-boundary.md`。
- 共享 Gateway 接入后仍未进行真实 Town Ambient HTTP 或游戏内回放；默认三渠道
  路径未切换，未部署。
- `LegacyWorldDiplomacyLlmGateway` 已建立并接入 `WorldDiplomacyBehavior` 的
  排队发送点；先复制不可变 PromptPackage，再复用原 WorldDiplomacy client 的
  route、重试、thinking fallback、stale 和 token/cache/truncation 结果语义。
- `LegacyPolicyLlmGateway` 已建立 NPC Policy/事件叛乱的 shared contract adapter；
Policy 各自 profile/JSON/重试 authority 保留；NPC ruler、玩家政策和王国战略建国卡的实际调用方已接入，测试 override 仍优先。

- Knowledge/RAG 的 RAG 专用短句生成已接入 `LegacyConfiguredChatGateway`；知识 owner
  继续负责提示词、候选解析、确定性 fallback、知识文件写入和 UI 时序。真实 RAG
  HTTP、写入和游戏内回放仍为 `NOT-RUN`。

- `AiErrorAnalysisInquiry` 已通过 `LegacyConfiguredChatGateway` 发送脱敏后的错误分析
  prompt；XihaiAction 的 SceneActions/Consent/BattleSpeech 分类器已增加配置完整时
  使用 shared Gateway 的 transport，并保留原反射 transport fallback。真实 HTTP、
  取消和游戏内验证仍为 `NOT-RUN`。
- `ShoutUtils` 的无名 NPC Persona 生成已通过 `LegacyShoutNetworkGateway`，其
  JSON 解析、文件/存档写入仍由原 owner 负责。
- 上述领域的真实 HTTP、旧存档和游戏内回放仍为 `NOT-RUN`；默认三渠道入口未切换。
- Dedicated TTS 已新增 `TtsSynthesisRequest` / `TtsSynthesisResult` 与
  `LegacyVolcTtsGateway`，并由 `TtsEngine` 调用；保留 V1 payload/header、code
  3000、base64 音频、播放队列和失败回调。Token 仍只在发送边界存在。
- TTS 真实服务、取消时序和游戏内播放仍未验证；本次未部署。

## 本轮继续进度

- 已在 `ShoutBehavior.cs` 增加 Native Conversation opt-in facade、detached envelope capture 和无凭据 runtime configuration snapshot 入口；旧 `SubmitNativeConversation*` 默认路径未替换；通用 `LegacyChannelInteractionFacade` 已抽出供三渠道复用。
- `MyBehaviorMemoryFacade` 已改为只保存稳定 HeroId，在交互边界解析 Hero，避免 live Hero 跨异步请求持有；失效目标安全返回空 memory，不回退成错误的 non-Hero memory。
- 已新增 `LegacyDetachedRuleSelector`：仅从 immutable snapshot 读取输入/context/排除列表，复用现有辅助规则检索 API；纯 runner 当前 17 cases PASS。
- 已新增 `LegacyPromptPackageAdapter`：在旧消息对象与不可变 `PromptPackage` 之间复制 role/content，并由 legacy gateway 使用；纯 runner 当前 18 cases PASS。
- 已新增 `DetachedPromptSections` 与 `LegacyDetachedPromptComposer`：由渠道 owner 在交互边界提供已生成的 stable system/prefix/suffix 字符串块，composer 按场景喊话权威顺序复制为不可变 `PromptPackage`；Native 增加显式 opt-in overload，旧默认路径保持不变；纯 runner 当前 22 cases PASS（含后处理 composer 与 atomic bundle）。
- Native、SceneShout、Courier 的 snapshot adapter 均已支持同一 `DetachedPromptSections` overload；`appendCurrentPlayerInput` 明确控制已记录输入去重，三渠道默认链路仍未切换。
- `LegacyActionTagParser` 已扩展为有限白名单的既有协议族 detached parser：覆盖 `ACTION/A/AD/ADP/ASS/GUI/ATT/ATP/RELAY/FOL/STP/END`，保留 `[ACTION:NAME:target:args]` 的旧拆分，拒绝旧 `[A:H_J_P_P]` 和未授权标签；不解析 AFEF/CONTENT，不执行动作。
- 已新增 `DetachedPostprocessPromptSections` 与 `LegacyDetachedPostprocessPromptComposer`：后处理由渠道 owner 提供已生成的 tag rules、history/AFEF、runtime facts 和最新可见回复 sections，composer 按既有顺序冻结为不可变 PromptPackage；raw reply 不自动进入后处理可见块，默认三渠道路径不变。
- Native opt-in facade 新增 Prompt sections provider overload；SceneShout/Courier 新增共用 `LegacyChannelInteractionFacade` 工厂，三渠道现在可在不改默认入口的情况下接入同一 coordinator/committer lifecycle。
- Native opt-in 另增 `DetachedInteractionPromptSections` atomic bundle overload，一次 capture 同时冻结主 Prompt 与后处理 Prompt sections，防止两阶段使用不同轮次的规则/事实。
- 已新增 `LegacyNativePromptParity`：在 Native 现有 `BuildStrictSceneMessagesForNpc` 和后处理最终 system/user 组装点之后，复制最终字符串/role-content 结果生成 detached sections，比较顺序与内容摘要，并汇合 atomic main/postprocess bundle；显式诊断失败时 fail-open 到旧 Native。
- Native parity fixture 为 `docs/fixtures/phase5-native-prompt-parity/native-message-order.json`，诊断说明为 `docs/animusforge-phase5-native-prompt-parity.md`；默认 parity 日志关闭，不改变默认 Native、SceneShout、Courier。
- 已新增 `LegacyNativeConversationOptInRunner` 与 `CreateNativeConversationOptInRunnerForExternal`：显式旁路现在具备 detached Generate、宿主主线程 commit 回调、基础设施失败回退旧 Native，以及 stale/cancel 不重试隔离；仍不自动接管 `SubmitNativeConversationTextInternalAsync`。
- 已新增 `LegacyNativeActionPlanExecutor` 与 `CreateNativeConversationActionPlanExecutorForExternal`：对 detached raw 后处理标签做严格 ordered plan 校验后，在主线程复用现有 `ApplyNativeConversationGameActionsCore`，保留现有领域动作、通知和 AFEF 权威入口；raw 中多出的动作标签会被拒绝。
- 当前切片：补齐 `CaptureSceneShout` 的交互边界记忆快照，使 Hero 与非 Hero 都通过稳定 memory id/namespace 复制已有历史进入共享 detached envelope；默认场景喊话入口不变。
- 已新增 `Refactor/Runtime/DetachedInteractionHost.cs`：通过 facade delegates 统一 Native/SceneShout/Courier 的 capture、stale/cancel、主线程 dispatch、ActionPlan 和 Memory 提交；action/memory facade 在 capture 后、Generate 前创建，避免异步目标漂移。Native opt-in helper 已直接复用该 host；不切换旧默认入口。
- 已补齐 inbound Courier detached host 的成功 commit 送达状态回调：仅在主线程
  commit 成功且历史写入成功后更新 session 的 letter/reply 状态并重新进入现有
  `ProcessSessionById`；stale、拒绝或失败结果不会推进送达状态。
- 下一切片：建立统一 Memory/AFEF 写入 facade 并接入 detached commit；继续保留旧
  默认入口和独立 fallback，不部署到游戏目录。
- 上一切片意图：用纯契约测试锁定 detached host 的 commit 回调、NPC inbound seed
  不写入 user history，以及 stale/rejected 不回调、不走旧 fallback；默认三渠道
  入口未切换。
- 当前配置切片已新增 `RuntimeConfigSnapshotStore`：detached capture 原子读取已发布
  快照，`AIConfigHandler.ReloadConfig()` 发布完整替换，reload 失败保留 last-known-good；
  旧 DuelSettings/MCM 仍为来源，未改变旧入口。
- 当前切片验证：InteractionPipeline `39 cases PASS`（含 atomic reload/failure
  isolation）；Persistence/Profile/Config 与阶段 2/3 runners PASS；1.3/1.4/Bootstrap
  unified stage 均 `0 warning / 0 error`，未部署。
- 已新增 `CreateNativeConversationDetachedPortsForExternal`：共享 detached rule selector、Prompt/postprocess composer、可见文本规范化和 allowlisted action parser 可由 Native 宿主一次组合；tag allowlist 必须由宿主显式提供，空 allowlist 不执行动作。
- 本轮组合入口验证：InteractionPipeline 31 cases（新增 Native ActionPlan 精确执行、raw 完整性拒绝、宿主异常隔离）、Persistence/Profile/Config runner、5 个阶段 2/3 pure runner、`git diff --check`、统一 1.3/1.4/Bootstrap stage 均 PASS；stage 位于 `bin\Debug\single_module_stage\AnimusForge`，未部署。
- `LegacyShoutNetworkGateway` 已支持分阶段路由：主回复继续使用 `ShoutNetwork`，后处理使用 `AIConfigHandler` 的一次性非交互动作后处理入口；后处理失败不弹出旧阻塞重试窗口。
- 本轮涉及 `AIConfigHandler.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、
  `Refactor/Contracts`、`Refactor/Adapters`、`Refactor/Runtime` 及对应阶段文档/台账；
  未修改构建、覆盖、推送脚本，未部署，未提交。
- 已知限制：旧动作后处理入口内部仍为同步 HTTP；外层 coordinator 能丢弃取消/stale 结果，但尚不能中断底层请求。真实 Native opt-in 宿主调用、旧存档、AFEF 和游戏内动作仍未验收；默认 Native 仍未切换。
- 本切片性能边界：parity 默认关闭；启用时只在 Native 一次交互的主/后处理组装边界复制字符串并计算摘要；runner 只按交互请求运行，不进入 ApplicationTick/EngineTick，不扫描 live 游戏对象，不在每个标签或消息上重新生成规则/反射；旧请求路径仍是唯一默认路径。
- 本切片验证：InteractionPipeline 28 cases、Persistence/Profile/Config runner、`git diff --check`、统一 1.3/1.4/Bootstrap stage 均 PASS；stage 位于 `bin\Debug\single_module_stage\AnimusForge`，未部署。
- 用户已确认：允许将准备文件和 skill 提交并推送到新的远端分支；暂不拆分多个玩法 DLL；旧存档兼容是硬目标；游戏基线采用最小可复现场景记录，不要求现在立即完成全量手测。

## 本次已完成

- 已确认当前工作区为 `F:\AnimusForge-main`。
- 已确认 `origin` 指向 `https://github.com/klfwdf/AnimusForge.git`。
- 已创建本地准备分支，并已将准备文档与重构地图推送到 `origin/refactor/prepare-af-restructure`。
- 已将 `D:\APP\QQ\document\af-skill.zip` 安装到项目级 `.claude/skills/animusforge-maintainer/`。
- 已创建公共重构台账：`docs/animusforge-refactoring-and-repository-reorganization-plan.md`。
- 已创建起点基线：`docs/animusforge-baseline-2026-08-30.md`。
- 已完成有限的只读结构盘点。
- 已完成第一版重构地图：`docs/animusforge-refactor-map.md`，涵盖运行链、组合根、目标 owner、持久化、交互管线、风险和推荐顺序。
- 已完成仓库边界、功能域和持久化审计；审计结论已合并到 `docs/animusforge-refactor-map.md`。
- 已完成第一版逐文件 owner matrix：`docs/animusforge-owner-matrix.md`，涵盖运行、交互、持久化、Policy、World、Siege、Mission、Social、Knowledge、UI、工具和参考树。
- 已确认 Bootstrap 已是清晰的独立边界；`SceneActionsIntegrationBoundary` 是现有的薄适配器边界范例。
- 已确认后续 owner map 至少应区分 Host/Composition、Conversation/AI、World Simulation、Settlement/Siege、Mission/Combat、Policy、Progression/Social、UI/Diagnostics 与 Compatibility/Safety；这些目前只是逻辑所有权，不代表立即拆 DLL。
- 阶段 1 首轮审计报告：`docs/animusforge-repository-boundary-audit.md`；未清理 tracked 参考/用户/生成物，未修改脚本。
- 阶段 1 初版决策表：`docs/animusforge-repository-boundary-decision-table.md`；采用保守“不发布未确认来源/许可证内容”原则，未执行清理或移动。
- 阶段 2 根 AF 基础 LLM owner 映射报告：`docs/animusforge-phase2-root-llm-owner-slice.md`；只读完成，未移动源码或改变运行行为。
- 阶段 2 SubModule 注册/调度分组清单报告：`docs/animusforge-phase2-submodule-registration-catalog.md`；只读完成，未改变注册顺序或运行行为。
- 阶段 2 registry DTO 设计报告：`docs/animusforge-phase2-registry-dto-design.md`；只读完成，未新增运行时类型或改变行为。
- 阶段 2 registry validator fixture：`docs/animusforge-phase2-registry-validator-fixtures.md`；覆盖有效快照、无效输入、依赖/顺序/owner/profile/线程/失败隔离输出；未实现 validator，未接入运行时。
- 阶段 2 影响面、候选 Bridge 与回滚地图：`docs/animusforge-phase2-impact-bridge-rollback-map.md`；首轮覆盖 Save、Prompt/Rule/Tag、Harmony、Tick、UI、线程、API、用户数据、Bridge、非目标和回滚模板；未移动源码或改变运行行为。
- 阶段 2 Conversation/Memory/Action contract matrix：`docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；定义不可变 snapshot、记忆/AFEF、授权动作结果、逐文件影响、三渠道一致性和纯 fixture；未实现 DTO/测试，测试 NOT-RUN。
- 阶段 2 Conversation/Memory/Action 方法级映射与纯 fixture：`docs/animusforge-phase2-conversation-memory-action-method-map.md`、`docs/fixtures/phase2-conversation-memory-action/`；已核对真实方法行号并建立纯输入/预期输出样例；`git diff --check` PASS，YAML parser NOT-RUN（环境无 parser）。
- 阶段 2 Settlement/Siege 与 Policy/Diplomacy Bridge contract：`docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`、`docs/fixtures/phase2-settlement-policy-bridges/`；已定义现有边界、五种组合、失败/回滚语义；3 个 JSON fixture 已通过 `ConvertFrom-Json`；未实现 Bridge 或 runner。
- 阶段 2 Bridge fixture runner：`tools/BridgeFixtureContractTests/validate_bridge_fixtures.py`；普通输出和 `--json` 输出均 PASS，10 个组合案例、6 项不变量；独立运行，不引用 Bannerlord/生产程序集。
- 阶段 3 module catalog：`docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/fixtures/phase3-module-catalog/`、`tools/ModuleCatalogContractTests/validate_module_catalog.py`；普通输出和 `--json` 输出均 PASS，8 modules、3 profiles、16 invalid cases、8 health states；未实现 Foundation/Registry。
- 阶段 3 AF.Contracts：`docs/animusforge-phase3-af-contracts-design.md`、`docs/fixtures/phase3-af-contracts/`、`tools/AFContractsContractTests/validate_af_contracts.py`；普通输出和 `--json` 输出均 PASS，9 contracts、3 events、6 capabilities、18 invalid cases；未创建生产 contract 项目。
- 阶段 3 Foundation runtime：`docs/animusforge-phase3-foundation-runtime-contracts.md`、`docs/fixtures/phase3-foundation-runtime/`、`tools/FoundationRuntimeContractTests/validate_foundation_runtime.py`；普通输出和 `--json` 输出均 PASS，6 contracts、8 health states、16 invalid cases；未创建生产 Foundation 项目。
- 本轮审查：修正公共台账中阶段 3 条目误放阶段 2及陈旧验证记录；四个独立 runner 均重新运行通过，未发现生产/脚本/配置路径变化。
- 阶段 3 纯组合矩阵：`docs/animusforge-phase3-composition-matrix.md`、`docs/fixtures/phase3-composition-matrix/`、`tools/CompositionMatrixContractTests/validate_composition_matrix.py`；普通输出和 `--json` 输出均 PASS，18 cases、24 invariants；未实现 Module Host。
- 阶段 3 GameAdapter API boundary：`docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/validate_game_adapter.py`；普通输出和 `--json` 输出均 PASS，14 cases、2 API lines、7 helper boundaries；未修改生产 helper，未重新构建/部署。
- 阶段 3 最终设计审查：`docs/animusforge-phase3-final-review.md`；确认阶段 3 设计清单闭合、6 个 runner 和 14 个 JSON fixture 通过；结论 PASS WITH LIMITATIONS；生产实现、双版本运行时、旧存档和游戏内验收仍未完成。
- 提交/推送状态：已创建本地提交（当前 HEAD，包含阶段 2/3 架构准备材料）；两次推送均失败，错误分别为 `Recv failure: Connection was reset` 和 `Failed to connect to github.com port 443`；当前分支领先远端 1 个提交，远端尚未更新。
- 用户决定先保持仓库现状：参考源码、生成物、用户数据、第三方依赖、工具发行物和归档均不删除、不移动、不取消跟踪；不修改 `.gitignore`。
- 用户已明确：`原版游戏本体代码1.3.x/` 与 `原版游戏本体代码1.4.5/` 是游戏源码参考仓库，应保留在 tracked reference plane；它们不属于 AF 生产源码，也不进入客户端 ZIP。

## 重要注意

- 开始准备时 `AnimusForge/SubModule.xml` 已有用户修改：版本 `v1.3.7` → `v1.3.7.2`，且末尾换行变化。本次没有回滚或覆盖它。
- 已运行统一 Debug stage 构建；未运行打包、部署或游戏测试。打包/部署/游戏测试仍为 `NOT-RUN`。
- 已使用本机 Bannerlord 根目录识别实际游戏版本 `v1.4.8.119303`；统一构建成功完成 1.3、1.4 和 Bootstrap，未部署到游戏目录。
- 用户已确认主要游戏内基线版本为 Bannerlord 1.4，采用 `1.4.x` 兼容目标，并确认存在可备份的代表性存档。
- 已从实际游戏 `TaleWorlds.Library.dll` 读取到版本 `v1.4.8.119303`；仓库 `.tmp\build_check\1.4` 是 `v1.4.6.115628`。本次构建 marker 已记录两条精确 BuildInfo；不同开发者可使用不同 1.4.x 补丁，但共享验收仍需指定固定 overlay。
- 依赖闭包已在本机解析：Harmony `2.4.2.225`、UIExtenderEx `2.13.2`、MCM/MBOptionScreen `5.11.4`，以及 AnimusForge 私有运行时 6 项均存在；1.4 overlay 内部引用完整。
- 不要把临时 ZIP 解压目录 `D:\APP\QQ\document\.af-skill-inspect\` 当作源码目标；skill 已从原 ZIP 安装到项目内。
- 不要修改现有一键编译/覆盖/推送流程。
- 本轮已完成 SceneShout 单目标 detached Prompt/Action ports 边界：新增
  `ShoutBehavior.CreateSceneShoutRefactorFacadeForExternal` 与
  `CaptureSceneShoutRefactorEnvelopeForExternal`，并使用现有单 NPC prompt
  helper 生成 atomic main/postprocess sections；默认 SceneShout 未切换。
- 本轮验证：InteractionPipeline `32 cases PASS`；统一 Debug Stage 的 1.3、
  1.4、Bootstrap 均 `0 warning / 0 error`，输出为
  `F:\AF测试重构\bin\Debug\single_module_stage\AnimusForge`；未部署。
- 本轮另完成 Courier reply/inbound detached Prompt ports：复用
  `BuildCourierReplyGenerationRequestOnMainThread` 与
  `BuildInboundLetterGenerationRequestOnMainThread` 的最终消息列表，复制为
  immutable PromptPackage/history；reply 的后处理 sections 由现有
  `TryPrepareCourierActionPostprocessForExternal` 捕获。Courier 默认入口及
  送达/返回状态机未切换。
- 本轮继续完成 SceneShout/Courier detached ActionPlan 主线程执行适配：
  `CreateSceneShoutActionPlanExecutorForExternal` 在 commit 边界重新解析并校验
  Agent 稳定身份，复用既有 mood/direct/follow 动作入口；
  `CreateCourierReplyActionPlanExecutorForExternal` 只携带稳定 session ID，重新
  校验 Courier recipient、送达状态和 subject 后复用既有 Courier 领域动作入口。
  Courier detached executor 关闭旧重复历史写入，由共享 `InteractionResultCommitter`
  统一写入 user/assistant；默认三渠道仍未切换。
- 本轮验证：InteractionPipeline `32 cases PASS`；Persistence/Profile/Config
  runner PASS；Bridge/ModuleCatalog/AFContracts/Foundation/CompositionMatrix/
  GameAdapter 六个阶段 2/3 runner PASS；统一 Debug stage 的 1.3、1.4、Bootstrap
  均 `0 warning / 0 error`，stage 位于 `bin\Debug\single_module_stage\AnimusForge`，
  未部署到游戏目录。
- 随后补齐三渠道 detached 普通对话资格和显式 host：Native/Courier/SceneShout
  ports 在无玩法规则命中时分别使用 `native_conversation`、`courier_reply`、
  `scene_shout` 仅文本基线，不授予动作权限；新增
  `SubmitSceneShoutRefactorOptInForExternalAsync` 与
  `SubmitCourierReplyRefactorOptInForExternalAsync`。Courier commit 经 engine
  tick 队列回主线程并有 30 秒迟到门闩。默认路径仍未切换。

## 下一步建议

- SceneShout 与 Courier Prompt/Action ports 及显式 host 已完成；两者的真实
  detached 网络、旧存档和游戏内验收仍未完成，继续保留旧默认路径和 fallback。
- 当前切片已建立统一 Memory/AFEF batch commit facade 并接入 detached commit：
  `InteractionMemoryCommit`、`IInteractionMemoryBatchCommitter`、有界进程内 receipt
  cache，以及旧 `MyBehavior` 的稳定 ID facade。成功动作才写 confirmed AFEF；拒绝、
  stale/cancel 不写确认事实；inbound seed 不写 user；重复 detached commit 不会重复
  执行动作或写历史；默认三渠道入口仍未切换。
- 当前验证：InteractionPipeline `39 cases PASS`；统一 1.3/1.4/Bootstrap stage
  均 `0 warning / 0 error`，stage 位于 `F:\AF测试重构\bin\Debug\single_module_stage\AnimusForge`，
  未部署到游戏目录。
- Economy/Reward/Debt detached capability 与 replay contract 已完成，见
  `docs/animusforge-phase6-economy-reward-debt-replay-boundary.md`；合法动作目录
  另已修正 `ACTION:DUEL_STAKE_*` 变体覆盖。
- 下一项：对真实 opt-in HTTP 做最小三渠道回放与旧存档运行时写入验收；仍不切换默认
  运行路径。动作协议切片已进一步完成平衡括号解析、冒号资产 token 和有限
  `LegacyActionTagCatalog`；随后接入 Economy/Reward/Debt 领域回放。

- 已选定本机最新旧存档：`C:\Users\29310\Documents\Mount and Blade II Bannerlord\Game Saves\saveauto2.sav`（修改时间 2026-08-28 14:23:10）；用户已完成手动测试，详细结果按决定跳过；未复制或修改存档。
1. 旧存档基础测试已由用户完成；详细结果不进入本轮台账，后续不再阻塞阶段 2 只读 owner 映射。
2. 如需针对当前安装游戏做精确验收，准备完整 `v1.4.8.119303` reference overlay；不修改构建脚本。
3. 阶段 4 persistence/profile/config 设计与纯 runner 已完成首轮；运行时迁移、
   旧存档 fixture 和配置 reload 实机验证仍待后续切片，不接入 `SubModule.cs`。



## 接手规则

新对话或新开发者应先读取：

- `CLAUDE.md`
- `.claude/skills/animusforge-maintainer/SKILL.md`
- `docs/animusforge-refactoring-and-repository-reorganization-plan.md`
- `docs/animusforge-baseline-2026-08-30.md`
- 本 handoff

随后核对 `git status`、当前分支、HEAD 和实际文件状态，再进行任何写入。

## 本轮追加（2026-08-30）

- 修正 `AIConfigHandler.cs` 辅助规则路由异常分支的无效 `rawResponse` 引用；1.3、1.4、Bootstrap unified stage 均重新构建通过，0 warning / 0 error。
- 完成阶段 7 周报 Event/WeeklyReport Gateway 切片：`MyBehavior.cs` 的批量、单组、完整周报和第 0 周短摘要显式调用 shared `LegacyConfiguredChatGateway`；保留 EventAndRebellion 配置解析、thinking/plain retry、周报批量/解析/重试/降级/限速和主线程写入。非周报调用与三渠道默认路径未切换。
- `LegacyConfiguredChatGateway` 新增可选 thinking enabled/reasoning effort 参数，既有调用默认行为不变。
- 本轮验证：7 个阶段 Python runner PASS；InteractionPipeline `39 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；仅 project-local stage，未部署。
- 当前阶段仍为阶段 7 ACTIVE；周报切片登记为 `VERIFY`。真实周报 HTTP、取消时序、旧存档和游戏内周报回放仍 `NOT-RUN`。
- 下一项建议：按相同边界审查并接入主动 NPC 的独立 LLM 压缩/摘要入口（如确认其复用既有 Memory/三渠道 facade，则登记 no-op/owner-only），随后处理剩余独立 auxiliary/event API；不得切换默认三渠道。
- 本轮完成 Memory 切片：`MyBehavior.cs` 的 Daily Memory 压缩、重大履历摘要和 Memory Overview 三个后台非流式入口通过 shared `LegacyConfiguredChatGateway`，保留 Auxiliary 配置、force-thinking-disabled、重试、解析、stale/obsolete、Memory/AFEF 存储及主线程提交。全套 7 个 Python runner、InteractionPipeline `39 cases`、XihaiAction Core `88/0`、GiveAssetTagCodec `80557` 和 1.3/1.4/Bootstrap stage 均通过，未部署。
- 主动 NPC owner-only 审查完成：未发现独立 HTTP/LLM transport；候选扫描/需求状态机复用 Native/Scene/Courier 开场 facade，摘要复用 Memory owner，不新增平行管线。该项登记为 `VERIFY`。
- 下一项准确任务：盘点 `MyBehavior.cs` 剩余独立 `CallUniversalApiDetailed` 调用（Persona、技能、叛乱命名及其他 auxiliary/event），逐项确认 owner、配置路由和是否已由 shared Gateway 覆盖；然后选择下一个未覆盖入口登记 active task。
- 本轮完成 Persona 切片：NPC Persona、升格同伴 Persona、升格同伴技能三个 Auxiliary 后台入口通过 shared `LegacyConfiguredChatGateway`；Persona/技能沿用 Auxiliary thinking 与 plain retry，Memory 摘要继续 force-thinking-disabled；原 JSON 解析、fallback、stale、主线程存储和存档结构不变。
- 本轮验证：7 个 Python runner PASS；InteractionPipeline `39 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；project-local stage，未部署。
- 当前阶段仍为阶段 7 ACTIVE；Persona 切片登记为 `VERIFY`。剩余独立入口包括叛乱王国命名，以及可能的外部/辅助 API facade；真实 Persona、旧存档和游戏内回放仍 `NOT-RUN`。
- 下一项准确任务：审查并接入叛乱王国命名的 EventAndRebellion LLM 入口，保留其 60 秒超时、重试和命名解析语义；随后再处理其余独立 API facade。
- 本轮完成外部 Auxiliary facade 切片：`MyBehavior.CallAuxiliaryApiTextForExternal` 及其唯一生产调用方 `PlayerNotorietyBehavior` 摘要改走 shared Auxiliary Gateway，保留纯文本返回、force-thinking-disabled、失败弹窗、摘要解析、周期调度、stale 和存储语义；登记为 `VERIFY`。
- 本轮验证：7 个 Python runner PASS；InteractionPipeline `39 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；project-local stage，未部署。
- 下一项准确任务：继续审查 `AIConfigHandler.cs` 中剩余直接 HTTP 的动作后处理/辅助分类入口，区分已由 Gateway 覆盖的规则路由与尚未覆盖的旧 transport，登记下一个 active slice。
- 本轮完成叛乱王国命名切片：唯一独立 EventAndRebellion 命名入口通过 shared `LegacyConfiguredChatGateway`，保留 60 秒超时、thinking/plain retry、三次重试、`[NAME]/[SHORT]/[LORE]` 解析、重名校验和王国创建主线程边界；该切片登记为 `VERIFY`。
- 本轮验证：7 个 Python runner PASS；InteractionPipeline `39 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；project-local stage，未部署。
- 下一项准确任务：盘点 `MyBehavior` 外部 Auxiliary facade（`CallAuxiliaryApiTextForExternal`）及剩余辅助模型入口，区分已由 Gateway 覆盖者与仍使用旧通用 HTTP 者，再登记下一个切片。

- 本轮完成 AIConfigHandler ACTION 后处理与 Auxiliary Simple Dialogue Gateway 切片验证：7 个 Python runner、InteractionPipeline `39 cases`、XihaiAction Core `88 passed / 0 failed`、GiveAssetTagCodec `80557 assertions`、1.3/1.4/Bootstrap unified stage 均通过；未部署。
- 本轮完成直接 LLM transport 盘点：Policy/World/Town/TTS 及已登记辅助入口已有 Gateway adapter；Scene/Native/Courier 主/流式回复按计划保留 `ShoutNetwork` legacy facade；DuelSettings/ModOnboarding 是配置向导连通性验证；`MyBehavior.CallUniversalApiDetailed` 无生产调用者，保留为 legacy dead path。
- 当前阶段仍为阶段 7 ACTIVE；ACTION/Simple Dialogue 切片与本轮审查登记 `VERIFY`。真实 HTTP、取消时序、旧存档运行时回放和游戏内验收仍 `NOT-RUN`。
- 下一项准确任务：为保留的 `ShoutNetwork` 主/流式 facade 设计并运行 opt-in 真实 HTTP 回放，覆盖取消、stale、重试、可见文本和 ACTION 边界；不得切换默认三渠道。

- 本轮新增 `ILlmStreamingGateway`，`LegacyShoutNetworkGateway` 为既有 ShoutNetwork SSE 主回复提供 opt-in 流式契约；增量回调与最终 `LlmGenerateResult` 分离，非 MainReply 阶段 fail-closed，不改变默认三渠道。
- 本轮验证：1.3/1.4/Bootstrap unified stage 均 `0 warning / 0 error`，7 个 Python runner、InteractionPipeline `39 cases`、XihaiAction Core `88/0`、GiveAssetTagCodec `80557` 均通过；未部署。
- 当前阶段仍为阶段 7 ACTIVE；ShoutNetwork 流式契约登记 `VERIFY`。真实流式 HTTP、取消/stale 时序、旧存档运行时和游戏内验证仍 `NOT-RUN`。
- 下一项准确任务：为该 opt-in 流式契约建立本地可控 provider 回放，验证 SSE 增量/最终文本不重复、取消/stale 不提交、thinking retry 与 ACTION 隔离；仍不切换默认三渠道。

- 新增 `tools/ConfiguredChatGatewayReplayTests/` 本地可控 provider 回放工程，直接链接生产 `LegacyConfiguredChatGateway.cs`；已通过 success、thinking plain retry、5xx retryable failure、cancellation 和 credential boundary。
- 该回放不等同于 ShoutNetwork SSE 实机验收：旧 SSE 的真实 HTTP、取消/stale 时序、可见文本/标签边界、旧存档和游戏内回放仍 `NOT-RUN`。
- 下一项准确任务：为旧 ShoutNetwork SSE transport 建立同等可控 provider 回放或注入 seam，验证增量/最终文本不重复、取消/stale 不提交、thinking retry 与 ACTION 隔离；仍不切换默认三渠道。

## 本轮追加（2026-08-30，SSE 回放验证）

- 完成 `ShoutNetwork` SSE opt-in 流式 Gateway 回放：保留旧 SSE 解析、重试、取消和 stale generation 处理，仅通过 Debug transport override 注入本地可控 provider；默认 Scene/Native/Courier 三渠道未切换。
- 验证通过：`tools/ShoutNetworkSseReplayTests` 输出 `success=1 thinkingPlainRetry=1 cancellation=1 stale=1 deltaFinalParity=1 actionIsolation=1`；Bootstrap 产物存在；1.3/1.4 实现构建各 0 warning / 0 error。
- 全套回归通过：7 个阶段 Python runner、Configured Chat Gateway replay、InteractionPipeline `40 cases`（含三渠道 opt-in facade replay）、XihaiAction Core、GiveAssetTagCodec `80557 assertions`。
- 当前阶段仍为阶段 7 ACTIVE；SSE opt-in 切片登记为 `VERIFY`。真实游戏内 SSE、旧存档和三渠道运行时回放仍 `NOT-RUN`，未部署、未提交、未推送。
- 下一项准确任务：对 Native/SceneShout/Courier opt-in host 做真实入口回放，覆盖主线程 commit、取消/stale、失败回退、历史/AFEF 边界；仍不切换默认三渠道。

## 本轮追加（2026-08-30，生产 opt-in entry 回放）

- 新增 `tools/ProductionOptInEntryReplayTests/`，直接加载 project-local 1.4 `AnimusForge.dll`，调用生产 `ShoutBehavior` / `CourierDeliveryBehavior` 的 Native、SceneShout、Courier opt-in capture 与 detached ports factory。
- 回放通过：`productionOptInEntryReplay native=1 scene=1 courier=1 identity=1 failClosed=1 ports=1 noDefaultCutover=1`。验证无活动游戏会话时不伪造 live 对象，保留三渠道 identity、Courier session id 和玩家输入。
- 该回放不等同于已初始化游戏 host 的真实 LLM 生成、主线程 commit、动作执行或存档回放；这些仍为 `NOT-RUN`。默认三渠道未切换，未部署、未提交、未推送。
- 下一项准确任务：在已初始化 host 中接入可控 provider，验证三渠道真实生成、主线程 commit、取消/stale、失败回退及历史/AFEF 边界；仍不切换默认三渠道。

## 本轮 active task（2026-08-30）

- 发现 `LegacyWorldDiplomacyLlmGateway` 的 `CancellationToken` 没有贯穿旧 `WorldDiplomacyLlmClient`：调用方取消可能被旧 client 转成 timeout 结果并进入 retryable 分支。
- 先登记后修改：目标是保持 route、JSON、thinking fallback、stale、token/cache metadata、存档和默认外交行为不变，只修正调用方取消传播与 retry delay 取消。
- 下一步：修改 `WorldDiplomacyLlmClient.cs` 的可选 token overload/内部传递，adapter 传入 token；增加本地可控 cancellation/timeout 回放并重跑 1.3/1.4/Bootstrap。

## 本轮完成（2026-08-30，World Diplomacy cancellation）

- `WorldDiplomacyLlmClient.CallMessagesWithRetriesAsync` 新增可选 `CancellationToken`，并贯穿单次请求、thinking plain retry 与 retry backoff；旧调用签名保持兼容。
- `LegacyWorldDiplomacyLlmGateway` 已传入 shared Gateway 的调用方 token。主动取消不再被转换成 timeout/retryable failure，也不会在 backoff 后继续 retry；hard timeout 语义保持不变。
- 新增并通过 `tools/WorldDiplomacyGatewayReplayTests/`：caller cancellation、retry-delay cancellation、timeout isolation、credential boundary 全部 PASS。
- 1.3/1.4/Bootstrap unified stage 已构建；真实 provider、旧存档和游戏内外交回放仍 `NOT-RUN`。默认三渠道未切换，未部署、未提交、未推送。
- 下一项准确任务：按同一策略审查 Native/SceneShout/Courier opt-in host 的实际生成/commit 回放，或继续处理剩余领域 adapter 的取消与 stale 边界。

## 本轮 active task（2026-08-30，Dedicated TTS）

- World Diplomacy shared Gateway 取消传播已完成并通过本地回放；转入已有 `LegacyVolcTtsGateway` 的真实 HTTP 回放验证。
- 目标：锁定 Volc V1 的 payload/header/code/base64 兼容和 caller cancellation；token 只允许出现在发送 header，不进入 TTS contract、存档或普通日志。
- 不修改 TtsEngine 播放队列、语音映射、Rhubarb 或默认路径；完成回放后再更新阶段 7 状态。

## 本轮完成（2026-08-30，Dedicated TTS Gateway 回放）

- `tools/TtsGatewayReplayTests/` 已修正回放服务器的构造参数和完整 request body 读取，避免仅读取 header 导致凭据边界无法验证。
- 本地回放通过：`ttsGatewayReplay success=1 headers=1 credentialBoundary=1 providerError=1 invalidAudio=1 cancellation=1 malformedExtra=1`。
- 1.3、1.4、Bootstrap 均构建成功，均为 `0 warning / 0 error`；project-local unified stage 成功，未修改游戏目录。
- TTS 真实服务、游戏内播放和旧存档运行时仍 `NOT-RUN`；未切换默认路径、未提交、未推送。
- 下一项准确任务：在已初始化 host 中验证 Native/SceneShout/Courier opt-in 的真实生成与主线程 commit，覆盖取消/stale、失败回退及历史/AFEF 边界；仍不得切换默认三渠道。

## 本轮回归记录（2026-08-30）

- 已通过：Configured Chat Gateway replay、InteractionPipeline `40 cases`、Production opt-in entry replay、ShoutNetwork SSE replay、TTS Gateway replay、World Diplomacy Gateway replay。
- 已通过：7 个 Python contract runner（AFContracts、Bridge、Composition、Foundation、GameAdapter、ModuleCatalog、Persistence/Profile/Config）。
- 额外 net6 smoke tests 未运行：当前机器只有 .NET 5.0.17 和 8.0.27，缺少 .NET 6.0 runtime；未安装运行时，也未修改 smoke 工程目标框架。
- 当前仍无已初始化的 Bannerlord host，因此三渠道真实 provider 生成、主线程 commit、动作执行、旧存档运行时和游戏内播放仍 `NOT-RUN`。

## 本轮追加（2026-08-30，三渠道主回复 transport 收口）

- `ShoutBehavior.cs` 与 `CourierDeliveryBehavior.cs` 剩余的旧 `ShoutNetwork.CallApiWithMessages*` 调用已统一改经 `LegacyShoutNetworkGateway.SendLegacyMessagesAsync/SendLegacyMessagesStreamAsync`；Gateway 内仍保留对旧 transport 的兼容委托。
- 原有 `recordTokenStats`、`promptRetryOnError`、thinking、取消、SSE 增量/完成/错误回调和错误文本路径均原样透传；本轮未改规则、Prompt、Action、历史、AFEF、存档或默认业务时序。
- 静态检查确认生产外部调用点只剩 Gateway 内部两处 legacy transport；Gateway regression suite（Configured、Interaction、Production opt-in、SSE、TTS、World Diplomacy replay）全部 PASS。
- 1.3、1.4、Bootstrap unified stage 均 `0 warning / 0 error`，仅写入 project-local stage，未部署、未提交、未推送。
- 下一项准确任务：在已初始化 host 或等价可控 host fixture 中运行三渠道 detached pipeline 的真实生成/主线程 commit，之后才评估默认入口切换；不得用当前静态/纯回放结果替代游戏内验收。

## 本轮追加（2026-08-30，Primary 非流式 transport 回放）

- `ShoutNetwork` 新增仅 Debug 的 scoped 非流式发送 override；真实默认代码仍回到 `DuelSettings.GlobalClient`，没有改变发布构建的 transport 行为。
- 新增 `tools/PrimaryLlmGatewayReplayTests/`，直接加载 project-local 1.4 Stage 程序集，通过 `LegacyShoutNetworkGateway.SendLegacyMessagesAsync` 验证 thinking plain retry、最终文本、credential 不进入 body 和 caller cancellation。
- 回放通过：`primaryLlmGatewayReplay success=1 thinkingPlainRetry=1 credentialBoundary=1 cancellation=1`。
- 该回放证明的是 production Gateway/primary transport seam，不等同于已初始化游戏中的规则命中、ActionPlan 执行、历史/AFEF 写入或三渠道默认切换。

## 本轮追加（2026-08-30，生产 detached host 集成回放）

- 新增 `tools/ProductionDetachedHostReplayTests/`，直接加载 project-local 1.4 Stage 生产程序集；通过动态 delegate/proxy 提供 fixture capture、ports、Gateway 和 memory，不复制生产 pipeline 实现。
- 回放通过：`productionDetachedHostReplay capture=1 main=1 postprocess=1 visibleFinal=1 commit=1 memoryBoundary=1 fallbackIsolation=1`。
- 该回放实际覆盖生产 `LegacyChannelInteractionFacade`、`FullInteractionPipeline`、`DetachedInteractionHost` 和 `InteractionResultCommitter` 的组合边界，但不等同于已初始化 Bannerlord host 的 Agent/Hero/Courier session、真实 ActionPlan 执行、真实历史/AFEF 存档写入或游戏内验收。
- 下一项准确任务：如能提供已初始化游戏 host，运行 Native/SceneShout/Courier 的真实规则命中、provider、主线程 commit、动作和历史/AFEF 验收；否则继续补齐剩余领域 Gateway 的真实 provider 回放与取消/stale 边界。

## 本轮追加（2026-08-30，发布构建验证）

- Release 配置下 1.3、1.4、Bootstrap unified stage 均构建成功，均 `0 warning / 0 error`；仅生成 project-local stage，未写入游戏目录。
- 下一项保持不变：建立可控 host fixture 覆盖三渠道 detached pipeline 的真实 Gateway 生成、主线程 commit、ActionPlan/历史/AFEF 提交，以及取消/stale/失败回退；在该验收完成前不切换默认入口。

## 本轮 active task（2026-08-30，Policy EventAndRebellion cancellation）

- 当前切片：修正 `LegacyPolicyLlmGateway` 的 EventAndRebellion 路径未传递 `CancellationToken` 的边界；保留旧 profile/route、JSON、重试、thinking/兼容降级、stale、主线程提交和存档语义。
- 预期改动：`PolicySystem/Npc/PolicyLlmClient.cs`、`Refactor/Adapters/LegacyPolicyLlmGateway.cs`，以及不接入生产项目的 `tools/PolicyGatewayReplayTests/`。
- 验证计划：本地可控 provider 覆盖调用方取消、retry backoff 取消、hard timeout 不被误判、请求凭据不泄露；随后构建 1.3、1.4 和 Bootstrap unified stage。
- 安全边界：不切换 Native/SceneShout/Courier 默认入口，不修改 SyncData key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。

## 本轮完成（2026-08-30，Policy EventAndRebellion cancellation）

- `PolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync` 新增可选 `CancellationToken`，并贯穿到旧 Policy HTTP 请求与 retry backoff；`NpcPolicyLlmClient` wrapper 保持兼容，`LegacyPolicyLlmGateway` 已传入 shared Gateway token。
- 新增 `tools/PolicyGatewayReplayTests/`，本地回放通过：`callerCancellation=1 retryDelayCancellation=1 timeoutIsolation=1 credentialBoundary=1`。EventAndRebellion 仍保持旧入口只使用 system prompt 的请求语义。
- Debug/Release unified stage 均构建成功：1.3、1.4、Bootstrap 各 `0 warning / 0 error`；输出仅位于 `bin\Debug\single_module_stage` 与 `bin\Release\single_module_stage`，未部署、未提交、未推送。
- 真实 Policy provider、旧存档加载、王国/政策游戏内回放仍为 `NOT-RUN`；本切片登记为 `VERIFY`。
- 下一项准确任务：继续为剩余领域 Gateway（优先 Knowledge/RAG、Weekly/Persona/辅助 API 或 Courier inbound/reply）补齐可控 provider 的 cancellation/stale/retry/fallback/credential 回放；在真实 host 验收完成前不得切换三渠道默认入口。

## 本轮追加（2026-08-30，Policy contract regression alignment）

- 修正 `tools/PolicyEffectModule.ContractTests/Program.cs` 的过时静态断言：Policy 调用方已通过 shared `LegacyPolicyLlmGateway.GenerateAsync` 时，不再错误要求调用方源码显式包含旧 `CallPolicyApiWithRetriesAsync(..., 3)` 形态；旧直接路径仍要求有界重试。
- 1.4 pinned reference 下完整 Policy contract runner 通过：`PASS assertions=9031 modules=18 syntheticDescriptors=64 activeContributions=100`。
- 该修正只影响验证表达，不改变生产 C# 行为；Policy Gateway replay、Debug/Release unified stage 均保持通过，未部署、未提交、未推送。

## 本轮完成（2026-08-30，Memory/AFEF receipt failure retry）

- `MyBehaviorMemoryFacade.Commit` 现在先检查已有 receipt，调用旧 `MyBehavior` history/AFEF 写入；只有写入成功后才登记 `MemoryCommitReceiptCache`。legacy owner 抛出异常时返回 `Failed(legacy_memory_append_failed)`，同一 commit 可以重试，不会被错误标成 `Duplicate`。
- receipt 仍是 512 项上限的进程内运行时缓存，不进入存档；用户/助手角色、AFEF 文本、SyncData key/type 和主线程提交边界未改变。
- 验证：InteractionPipeline contract `40 cases PASS`；Policy Gateway replay PASS；Debug/Release unified stage 的 1.3、1.4、Bootstrap 均 `0 warning / 0 error`，仅写入 project-local stage。
- 真实旧存档加载后的 history/AFEF 写入、三渠道游戏内运行和默认入口切换仍 `NOT-RUN`；未部署、未提交、未推送。
- 下一项准确任务：继续补齐 Knowledge/RAG、Weekly/Persona、辅助 API 或 Courier inbound/reply 的可控 provider cancellation/stale/retry/fallback/credential 回放；随后再推进已初始化 host 的三渠道真实 commit 验收。

## 本轮 active task（2026-08-30，Knowledge/RAG Gateway owner 边界）

- 当前切片：将 KnowledgeLibraryBehavior 的 RAG 短句生成改由 LegacyKnowledgeRagGateway 承担，保留现有 prompt、token cap、禁用 thinking、解析与确定性 fallback。
- 预期改动：`KnowledgeLibraryBehavior.cs`、`Refactor/Adapters/LegacyKnowledgeRagGateway.cs`，以及不接入生产 `.csproj` 的 `tools/KnowledgeRagGatewayReplayTests/`。
- 验证计划：本地可控 provider 覆盖成功、空响应、provider failure、caller cancellation、非 MainReply 排除和 credential boundary；随后构建 1.3、1.4 与 Bootstrap unified stage。
- 安全边界：不切换 Native/SceneShout/Courier 默认入口，不修改 SyncData/key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。

## 本轮完成（2026-08-30，Knowledge/RAG Gateway owner 边界）

- `KnowledgeLibraryBehavior` 的 RAG 短句生成已切换到新增 `LegacyKnowledgeRagGateway`；保留既有 prompt、token cap、禁用 thinking、解析、确定性 fallback 和知识数据写入时序。Gateway 显式拒绝非 `MainReply` 阶段，caller cancellation 贯穿 configured transport，凭据只在 HTTP header 发送边界。
- `tools/KnowledgeRagGatewayReplayTests/` 通过：`success=1 empty=1 providerFailure=1 cancellation=1 nonMainExclusion=1 credentialBoundary=1`。
- Debug/Release unified stage 的 1.3、1.4、Bootstrap 均成功，均 `0 warning / 0 error`；输出仅在 `bin\Debug\single_module_stage` 与 `bin\Release\single_module_stage`，未部署。InteractionPipeline `40 cases`、Policy/Configured Gateway、Production detached host 和 7 个纯契约 runner 均通过。
- 真实知识库 provider、旧存档和游戏内回放仍为 `NOT-RUN`；未提交、未推送。
- 下一项准确任务：Courier inbound/reply 的可控 provider 回放，覆盖主线程 commit、取消/stale、失败回退及 user/assistant 历史边界；真实 host 可用前不切换默认三渠道。

## 下一轮 active task（2026-08-30，Courier inbound/reply provider 回放）

- 目标：在等价可控 host 中调用生产 Courier inbound/reply opt-in 入口，覆盖 provider 生成、主线程 commit、取消/stale、失败回退以及 user/assistant 历史边界。
- 约束：保留信使送达/返回时序、旧 facade、SyncData/key/type 和 AFEF 语义；真实 Bannerlord host 可用前不切换默认 Native/SceneShout/Courier 路径，不部署、不提交、不推送。

## 本轮 active task（2026-08-30，Courier inbound/reply 生产 opt-in host 回放）

- 目标：在不切换默认 Courier 状态机的前提下，使用生产 Courier capture、ports、facade 和 detached host 接入可控 Gateway，覆盖 reply 主/后处理、inbound seed 不写 user history、commit 和失败隔离。
- 预期改动：仅新增 `tools/ProductionCourierHostReplayTests/` 与本轮台账记录；若发现生产边界缺陷，先停在回放 fixture，不扩大到默认入口。
- 安全边界：不修改 SyncData/key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。

## 本轮完成（2026-08-30，Courier inbound/reply 生产 opt-in host 回放）

- 新增 `tools/ProductionCourierHostReplayTests/`，直接加载 project-local 1.4 implementation，接入生产 Courier capture、detached ports、facade 与 host。
- 回放通过：`productionCourierHostReplay courierPorts=1 replyMain=1 replyPostprocess=1 replyCommit=1 inboundMain=1 inboundCommit=1 inboundNoUserSeed=1 cancellationBoundary=1 fallbackIsolation=1`。
- 实际验证 reply 历史为 `user → assistant`，inbound 仅写 `assistant`；取消/stale 不提交；缺失 provider 独立走 legacy fallback。
- Debug/Release unified stage 与既有 Gateway/契约回归保持通过；未部署、未提交、未推送。真实 Bannerlord host、旧存档和游戏内 Courier 回放仍为 `NOT-RUN`。
- 下一项准确任务：审查剩余 `MyBehavior.CallUniversalApiDetailed` 与 auxiliary/event 入口，逐项确认 shared Gateway 覆盖、owner、凭据和失败边界；默认三渠道仍不切换。

## 本轮 active task（2026-08-30，NPC Policy generation job 取消传播）

- 发现：`NpcRulerPolicyBehavior.Generation.cs` 的后台 Policy draft/effect/repair 仍向 shared Gateway 传 `CancellationToken.None`，已有 stale/version 检查但不能中断旧 provider 请求。
- 目标：为每个运行时 generation job 建立 CTS；读档/新游戏清理时取消；API 取消后不进入 pending commit 队列；保留现有 Policy JSON、存档和主线程边界。
- 预期改动：`PolicySystem/Npc/NpcRulerPolicyBehavior.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs`，必要时补充独立契约验证；不部署、不提交、不推送。

## 本轮完成（2026-08-30，NPC Policy generation job 取消传播）

- 每个 Policy generation job 已建立 runtime-only CTS；draft/effect/repair Gateway 请求贯穿 job token；读档/新游戏清理会取消旧 job；取消结果不会进入 pending commit 队列；CTS 不参与存档。
- 保留 Policy route/profile/JSON、重试、stale/version、主线程提交和存档语义；未切换三渠道，未部署、未提交、未推送。
- Debug/Release unified stage 的 1.3、1.4、Bootstrap 均 `0 warning / 0 error`；既有 Policy Gateway、InteractionPipeline 与契约回归通过。真实 Policy provider、旧存档和游戏内回放仍 `NOT-RUN`。

## 本轮 active task（2026-08-30，auxiliary/event LLM 入口 owner 审查）

- 当前阶段：调查。首批只读审查 `MyBehavior.CallUniversalApiDetailed`、`DuelSettings`/`ModOnboardingBehavior` 验证入口及其他 auxiliary/event transport，确认共享 Gateway、owner、credential、cancellation、stale、fallback 和线程边界。
- 首批预期只读路径：`MyBehavior.cs`、`DuelSettings*.cs`、`ModOnboardingBehavior.cs`、`Refactor/Adapters/*Gateway.cs` 及对应工具/文档；不修改生产 C#，除非审查后另登记最小实现切片。
- 安全边界：不切换 Native/SceneShout/Courier 默认路径，不修改 SyncData/key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。

## 本轮 active task（2026-08-30，DuelSettings 聊天连接测试 POST Gateway 收口）

- 目标：将 `DuelSettings` 的四条聊天连接测试 POST 通过共享配置 Gateway 的原始验证交换边界发送；保留各设置 owner 的 prompt、thinking/temperature、响应解析和 UI 错误语义。
- 预期改动：`DuelSettings.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、独立 `tools/ConfiguredChatValidationReplayTests/`；GET `/models`、`ModOnboardingBehavior` 组合验证和旧 `CallUniversalApiDetailed` 暂不纳入本切片。
- 验证：可控 provider 成功、HTTP 错误、caller cancellation、timeout isolation、credential 不入 payload/body；随后双 API 线和 Bootstrap unified stage 构建。
- 安全边界：不切换三渠道，不改变 SyncData/key/type、程序集身份、构建/覆盖/推送脚本，不部署、不提交、不推送。


## 本轮完成（2026-08-30，ModOnboarding GET models/模型获取 Gateway 边界）

- 新增 `Refactor/Adapters/LegacyModelCatalogGateway.cs`，将 GET `/models` 的 URL 生成、可选认证、响应捕获、状态码、异常和调用方取消收口到独立 adapter；不解析模型名、不缓存凭据、不进入公共 LLM DTO。
- `ModOnboardingBehavior` 的 Base URL 探测和带 Key 模型获取，以及 `DuelSettings` 的 MCM 模型刷新均接入 adapter；保留原有模型解析、状态码策略、UI pending 状态和版本/stale 保护。
- `tools/ModelCatalogGatewayReplayTests/` 通过：`probeNoCredential=1 fetchCredentialBoundary=1 httpFailure=1 cancellation=1 invalidConfig=1`。另行回归 `ConfiguredChatGatewayReplay`、`ConfiguredChatValidationReplay`、`InteractionPipelineContractTests` 均 PASS。
- Debug/Release unified stage 的 1.3、1.4、Bootstrap 均 `0 warning / 0 error`；只生成项目内 `bin\{Debug,Release}\single_module_stage`，未部署、未提交、未推送。
- 静态核对：`ModOnboardingBehavior.cs` 和 `DuelSettings.cs` 中模型目录 GET 直连已移除；剩余 onboarding 直连是聊天验证 POST，归入下一切片。真实 provider、游戏内 onboarding、旧存档运行时仍 `NOT-RUN`。

## 本轮 active task（2026-08-30，ModOnboarding 聊天验证 POST Gateway 收口）

- 当前阶段：调查后进入最小实现。审查 `ModOnboardingBehavior.ValidateApiTargetAsync`、MCM API 验证和组合验证中的聊天 POST，统一到已登记的配置聊天验证 adapter；不混入 GET `/models` 模型目录协议。
- 保留认证 header、provider-specific payload、响应解析、HTTP 状态/错误提示、`_apiValidationVersion`/CTS/stale/UI pending 时序；凭据不进入公共 DTO、存档或普通日志。
- 安全边界：不切换 Native/SceneShout/Courier 默认路径，不修改 SyncData/key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。


## 本轮完成（2026-08-30，ModOnboarding 聊天验证 POST Gateway 收口）

- `ModOnboardingBehavior` 的组合验证 `ValidateApiTargetAsync` 和 MCM 单目标验证已通过 `LegacyConfiguredChatGateway.SendValidationAsync` 发送；保留 provider-specific payload 构造、原始响应解析、HTTP 状态/错误提示、`_apiValidationVersion`、CTS、stale 和 UI pending 时序。
- 取消由 adapter exchange 显式转回原有 onboarding 取消流程；网络/无状态异常保留 onboarding 的异常失败提示；凭据仍只在发送边界。
- 回归：`configuredChatValidationReplay success=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`、`modelCatalogGatewayReplay probeNoCredential=1 fetchCredentialBoundary=1 httpFailure=1 cancellation=1 invalidConfig=1`、`InteractionPipelineContractTests cases=40` 均 PASS。1.4 direct、Debug/Release unified stage 的 1.3/1.4/Bootstrap 均 `0 warning / 0 error`。
- 模型目录 GET 与 onboarding 聊天 POST 生产直连均已清零；真实 provider-specific onboarding、游戏内 MCM、旧存档运行时仍 `NOT-RUN`。未部署、未提交、未推送。

## 本轮 active task（2026-08-30，ModOnboarding provider-specific 验证失败语义）

- 当前阶段：调查。核对共享 validation exchange 对 OpenAI-compatible、Anthropic-compatible、YJ/Gemini 等 provider-specific payload/response 的保持程度，覆盖成功、空回复、坏 JSON、HTTP 错误、取消和 stale。
- 首选只读/纯回放路径：`ModOnboardingBehavior.cs`、`LlmApiCompat.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、现有 validation/model-catalog replay；只有证据显示语义缺口时才新增最小 adapter/测试改动。
- 安全边界：不切换 Native/SceneShout/Courier 默认路径，不修改 SyncData/key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。


## 本轮完成（2026-08-30，ModOnboarding provider-specific 验证失败语义）

- 生产程序集回放加载项目内 1.4 Debug stage 的真实 `LegacyConfiguredChatGateway`/`LlmApiCompat`，通过 OpenAI-compatible、Anthropic-compatible 和 YJ/Gemini thinking 控制验证：`productionValidationProviderReplay openAi=1 anthropic=1 yjGeminiThinking=1 credentialBoundary=1`。
- 发现并修复 `DuelSettings` 辅助连接测试的 provider-specific 双转换风险：`BuildAuxiliaryRouterRequestJsonForExternal` 已准备的 JSON 改用 `SendValidationJsonAsync` 原样发送；结构化 payload 仍使用 `SendValidationAsync`。
- `configuredChatValidationReplay success=1 preparedJsonPreserved=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；ModelCatalog/InteractionPipeline 回归通过；Debug/Release unified stage 的 1.3、1.4、Bootstrap 均 `0 warning / 0 error`。
- `ModOnboarding` 组合验证与 MCM 验证的 provider-specific payload、原始响应解析、状态码/错误提示、取消/stale/UI pending 保持；真实 provider、游戏内 onboarding、旧存档仍 `NOT-RUN`。未部署、未提交、未推送。

## 本轮 active task（2026-08-30，XihaiAction auxiliary classifier transport owner 审查）

- 当前阶段：调查。审查 `AfV130AuxiliaryTextClassifier`、`AfClassifierTransport`、`AfV130ConfiguredGatewayTransport` 与 `AfV130CallApiTransport` 的 owner、credential、single-flight、caller/lifetime cancellation、fallback 和 1.3/1.4 兼容边界。
- 先运行现有 classifier contract/extension static verifier，确认 shared Gateway 覆盖和可选依赖失败隔离；只有发现具体缺口才新增最小生产改动与回放。
- 安全边界：不切换 Native/SceneShout/Courier 默认路径，不修改 SyncData/key/type、模块身份、构建/覆盖/推送脚本，不部署、不提交、不推送。


## 本轮完成（2026-08-30，XihaiAction auxiliary classifier transport owner 审查）

- 已确认 classifier 在配置完整时使用 `AfV130ConfiguredGatewayTransport`/`LegacyConfiguredChatGateway`，配置缺失或 transport 创建失败时保留 `AfV130CallApiTransport` legacy fallback；配置在每次发送开始时复制，classifier 不持有凭据或 live 游戏对象。
- 发现并修复 `AfV130AuxiliaryTextClassifier.Dispose()` 与已取得 `SemaphoreSlim` 的后台请求竞态：lifetime cancellation 仍先触发，门闩释放对 disposed race 幂等处理，不改变协议/动作白名单。
- `tools/XihaiClassifierTransportReplayTests`：`shortCircuit=1 closedSet=1 ordinarySingleFlight=1 consentLimit=1 battleSpeechLimits=1 lifetimeCancellation=1`；XihaiAction Core `88 passed / 0 failed`；StaticVerifier `13 passed / 0 failed`。
- 主模块 Debug/Release unified stage 的 1.3、1.4、Bootstrap 均 `0 warning / 0 error`；扩展 runtime 独立编译因当前环境缺少 .NET Framework 4.7.2 Developer Pack `NOT-RUN`；真实扩展加载、provider、旧存档和游戏内分类器仍 `NOT-RUN`。未部署、未提交、未推送。

## 本轮 active task（2026-08-30，SyncData key/type/owner/chunk 兼容审计）

- 当前阶段：调查。读取 `MyBehavior`、各 CampaignBehavior `SyncData`、`CampaignSaveChunkHelper` 和阶段 4 namespace fixture，补齐字面量/符号 key、owner、序列化类型、chunk 边界、legacy fallback 与 SafeMode 未知数据保留证据。
- 首批只做源码审计、catalog 对账和纯 fixture；不改变现有程序集身份、CampaignBehavior 类型、SyncData key/type、存档、构建/部署脚本或游戏目录。
- 真实旧存档加载、游戏内读档和运行时迁移仍 `NOT-RUN`。


## 本轮完成（2026-08-30，SyncData key/type/owner/chunk 兼容审计）

- 阶段 4 catalog 与当前源码对账通过：95 个 exact literal key、40 个 symbolic source；新增 chunk/storage 证据为 13 个 `Save/LoadChunkedString` 基础 key、38 个 `FlattenStringDictionary` 基础 key，以及 `CampaignSaveChunkHelper` 的 12000/240/262144 和 metadata prefix 契约。
- `persistenceProfileConfig` validator：`literalKeys=95 symbolicSources=40 chunkedStringKeys=13 flattenedDictionaryKeys=38 chunkMaxBytes=12000 ... PASS`。
- `persistenceChunkReplay`：`smallInline=1 utf8Boundary=1 missingChunk=1 oversizeCount=1 legacyFallback=1 dictionaryRoundTrip=1 corruptDictionary=1 safeSyncIsolation=1`。
- 未修改生产存档 owner、程序集身份、CampaignBehavior 类型、SyncData key/type 或 SaveSystem；真实旧存档加载、typed runtime binding、SafeMode 运行时和游戏内读档仍 `NOT-RUN`。未部署、未提交、未推送。

## 本轮 active task（2026-08-30，typed SyncData ref 绑定与 legacy save fixture）

- 当前阶段：调查后进入纯审计 fixture。为 95 个 exact key 记录 `ref` 变量/容器和 save/load 双向绑定，识别 scalar/list/dictionary/chunk wrapper 类型，并覆盖缺失 legacy key、类型不一致和 SafeMode 未知数据保留。
- 不替换生产 `SyncData` 调用，不改变 key/type、程序集身份、CampaignBehavior 注册、迁移运行时、构建/覆盖/推送脚本或游戏目录；真实旧存档仍 `NOT-RUN`。


## 本轮完成（2026-08-30，typed SyncData ref 绑定与 legacy save fixture）

- 新增 `docs/fixtures/phase4-persistence-profile-config/syncdata-binding-catalog.json`：记录 95 个 exact key、121 次 `ref` 绑定、8 类静态 C# 类型，并对账 owner、源码行、ref 变量和 save/load 类型一致性。
- `persistenceProfileConfig` validator 输出 `typedBindings=121 typedBindingKeys=95 typedBindingTypes=8`；之前的 95 literal/40 symbolic/13 chunked/38 flattened 对账仍 PASS。
- `persistenceChunkReplay` 继续通过 UTF-8 chunk、缺失/超限、legacy inline、字典损坏和 SafeSyncData 隔离。
- 未修改生产 `SyncData` 调用、key/type、程序集身份、CampaignBehavior 注册或 SaveSystem；真实旧存档、TaleWorlds typed binding、SafeMode runtime 和游戏内读档仍 `NOT-RUN`。未部署、未提交、未推送。

## 本轮 active task（2026-08-30，legacy-first SafeMode/缺失字段纯迁移 fixture）

- 当前阶段：调查后进入纯 fixture。基于 typed binding catalog 构造 scalar/list/dictionary/TroopRoster/chunked storage 的旧表示、缺失 key、类型不一致、未知字段和迁移幂等场景，验证失败时不发布新表示、未知数据仍可见。
- 不接入真实 SaveSystem，不修改生产 `SyncData`/key/type、程序集身份、CampaignBehavior、构建/覆盖/推送脚本或游戏目录；真实旧存档仍 `NOT-RUN`。


## 本轮完成（2026-08-30，legacy-first SafeMode/缺失字段纯迁移 fixture）

- 新增 `docs/fixtures/phase4-persistence-profile-config/legacy-first-safe-mode-migration-cases.json` 与 `tools/PersistenceMigrationContractTests.py`。
- runner 通过：`persistenceMigrationContract cases=6 unknownRetention=1 missingOptional=1 typeMismatchRollback=1 chunkFailureClosed=1 idempotent=1 legacyFirst=1 PASS`。
- 该证据覆盖缺失可选 key、不一致类型回滚、chunk 失败闭合、未知字段保留和幂等；不删除 legacy key。
- 未接入真实 SaveSystem，不修改生产 SyncData/key/type、程序集身份、CampaignBehavior 或构建/部署流程；真实旧存档、SaveSystem typed binding、SafeMode runtime 和游戏内读档仍 `NOT-RUN`。未部署、未提交、未推送。

## 本轮 active task（2026-08-30，存档 owner/type/程序集身份基线对账）

- 当前阶段：调查。对比当前工作树与基线 `d4cb1467376c6e923f4295dcefc7878c11dbc7c1` 的 SyncData owner 类型、CampaignBehavior 类型名、`AnimusForge` 程序集名、SubModule/Bootstrap 注册边界，记录 deliberate changes。
- 只读 Git/source/assembly audit；不回滚用户改动，不修改生产 C#、SyncData/key/type、构建/覆盖/推送脚本或游戏目录；真实旧存档仍 `NOT-RUN`。


## 本轮完成（2026-08-30，存档 owner/type/程序集身份基线对账）

- 新增 `tools/PersistenceIdentityAudit.py`，支持泛型 `SyncData<T>`、多行 CampaignBehavior 继承声明和基线提交对比。
- 对账结果：`persistenceIdentity sync=99 behavior=35 module=AnimusForge bootstrap=1 PASS`；added/removed key/type 和行为类型均为空。当前 Debug stage 两个实现程序集均为 `AnimusForge`，Bootstrap 为 `AnimusForge.Bootstrap`；主模块仍只声明 `AnimusForge.Bootstrap.dll`。
- 期间修复了审计工具的两个误报：泛型 `SyncData<T>` 未匹配、Python 子进程 Git glob pathspec 为空；未改生产存档代码。真实旧存档、SaveSystem、SafeMode runtime 和游戏内读档仍 `NOT-RUN`。

## 本轮完成（2026-08-30，最终审查与回原重构分支推送准备）

- 当前阶段：验证。已完成 staged 文件、凭据/私有路径、程序集/模块身份、SyncData key/type、回放和双版本构建的最终审查。
- 已清理误暂存的 Python `.pyc` 生成物；禁止生成物扫描无输出。
- 已创建本轮提交：`refactor: continue full LLM migration and compatibility verification`；下一步将无强制推送到 `origin/refactor/prepare-af-restructure`。
- 本地项目目录为 `F:\AF测试重构`；仓库目标仍为原重构分支，不建立独立远程分支。
- 不 force push、不部署游戏目录、不修改构建/覆盖/推送脚本；真实旧存档、游戏 Host、XihaiAction runtime 独立编译仍为 `NOT-RUN`。


## 本轮完成（2026-08-30，configured-chat 流式 Gateway 与 Universal 遗留路径收敛）

- `LegacyConfiguredChatGateway` 新增通用 SSE 流式 transport、delta/final 分离和 adapter-local generation diagnostics；保留 credential、thinking plain retry、取消、HTTP 错误和 response sampling 边界。
- `MyBehavior.CallUniversalApiDetailed` 已收敛到 shared configured Gateway；旧 `ApiCallResult`、stale、限流、token 统计和失败详情映射保留，不切换默认三渠道。
- 验证：Configured Gateway replay（`success=1 streaming=1 thinkingPlainRetry=1 retryable5xx=1 cancellation=1 credentialBoundary=1`）、Configured validation、Knowledge/RAG、Primary Gateway 均 PASS；1.4 direct 与 1.3/1.4/Bootstrap unified stage 均 `0 warning / 0 error`。
- 本轮未部署游戏目录；真实 provider、游戏内 host/commit、旧存档和 XihaiAction runtime 独立编译仍 `NOT-RUN`。
- 下一项准确任务：为已初始化 host 接入可控 provider，验证真实生成、主线程 commit 与 legacy fallback。


## 本轮完成（2026-08-30，生产三渠道等价可控 Host fixture 验证）

- 新增 `tools/ProductionConfiguredHostReplayTests/`，直接加载 project-local 1.4 production stage，并使用真实 `LegacyConfiguredChatGateway`、`LegacyChannelInteractionFacade` 与 `DetachedInteractionHost`；loopback provider 不使用 fake Gateway。
- 回放通过：`productionConfiguredHostReplay native=1 scene=1 courier=1 mainPostprocess=1 commitHistory=1 credentialBoundary=1 providerFallback=1 cancellationBoundary=1`。
- 已验证三 channel 的 provider 主/后处理请求、commit/history 角色边界、失败回退和取消不提交；真实 Bannerlord Host、live Agent/Hero、ActionPlan 游戏执行、AFEF、旧存档仍 `NOT-RUN`。
- 未部署游戏目录；下一项准确任务：Economy/Reward/Debt 主线程 replay port 与 ActionPlan 真实域校验。
