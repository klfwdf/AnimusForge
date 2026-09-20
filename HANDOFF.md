# 当前交接：J10a Scene 闭合，J10b Courier 会话准备 owner 归位（2026-09-21）

- **状态**：J07–J09、J10a Scene `OFFLINE_VERIFIED`；J10b `IN_PROGRESS`。`bdf58283` 已归位 prompt-run/session reservation 与五阶段 schedule；`5cb7dc6e` 又把 Campaign 退役、准入、人设等待、双向历史和 detached 后处理/owner phase 原内容归位 `Channels/Courier`。真实消费者仍调用同一 partial class，没有新增 facade、存档键、公开 API 或执行路径。
- **当前结果**：`Channels/Scene` 持真实 group/relay/passive/reaction 编排、request/audience identity、pending AFEF one-shot、FIFO/单 worker lease、共享 postprocess/relay completion；live Agent、interaction timeout、TTS/audio、movement、History/Memory/动作副作用有意保留游戏线程 host adapter。
- **验证**：ChannelCutover 132 + 提取 14；ProductionConsumers 正常 + 3 有效变异；scope/pending AFEF/speech queue 共 16 正常 + 9 变异；Scene parity 71、Queue 37+7、request lifetime 30、BattleSpeech 18+2、默认 wiring 25；Debug 1.3/1.4/Bootstrap 均 0 warning/0 error。代码地图 342 锚点绑定 `cbf7f453`。
- **已知验证阻塞**：TeamModule parity 在比较 Scene 前被既有 MemorySummary fixture dependency hash 漂移阻断；本轮未改该依赖、未刷新 hash 绕过，不把它计 PASS。
- **Courier 证据**：History 122/30、Persona 169、owner phase 16、postprocess 39 与 8 个有效变异、Prompt 550/76、liveness 59/16、GameLifetime bindings 15、Phase8 inventory 11；Debug 1.3/1.4/Bootstrap 均 0 warning/0 error。既有 whole-file 逆变换会被 J07/J09 后合法主文件漂移前置阻断，本轮未刷新旧基线绕过。
- **下一步**：J10b3 处理 `CommitDispatch`、`InboundCompletion` 和主类内剩余 transport/pregeneration/arrival/delivery/letter/retry；不重开 Scene、prompt-run 或本轮会话准备 owner，除非出现新复现。
- **边界**：真实 provider、Campaign/Mission、旧 SAVE、live Economy/外交、真实音频和帧/网络性能均 NOT-RUN；未 Stage/Deploy/Package、未操作游戏/存档。
- **位置**：唯一施工树 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`；本地分支 `codex/af-modularize-j04-20260918`；交付目标 `origin/codex/af-main-refactor-continuation-20260831`。`.dotnet-cli-home/` 保留且不纳入 Git。

代码定位（一基行号，后续改动后以符号搜索为准）：

- `src/modules/AF.Module.Actions/Tags/LegacyActionTagCatalog.cs:14`
- `src/modules/AF.Module.Actions/Tags/LegacyActionTagParser.cs:14`
- `src/modules/AF.Module.Actions/Plan/ActionPlanIntegrityPolicy.cs:13`
- `src/modules/AF.Module.Actions/Execute/LegacyNativeActionPlanExecutor.cs:27`
- `src/modules/AF.Module.Actions/Execute/LegacyChannelActionCommitter.cs:15`
- `src/modules/AF.Module.Actions/Execute/LegacyChannelActionPlanExecutor.cs:14`
- `src/modules/AF.Module.Actions/Receipts/ActionExecutionCommitter.cs:13`
- `src/modules/AF.Module.Actions/Receipts/InteractionResultCommitter.cs:16`
- `src/modules/AF.Module.Actions/Receipts/InteractionCommitReceiptCache.cs:13`
- `ShoutBehavior.NativeActionCommit.cs:20`
- `src/modules/AF.Module.Conversation/Channels/Scene/SceneShoutConversationScope.cs:161`
- `src/modules/AF.Module.Conversation/Channels/Scene/ScenePlayerShoutRequestOwner.cs:56`
- `src/modules/AF.Module.Conversation/Channels/Scene/ScenePendingAfefFactsOwner.cs:11`
- `src/modules/AF.Module.Conversation/Channels/Scene/SceneSpeechQueueOwner.cs:10`
- `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs:95,482`
- `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs:85,185,770,1568`
- `CourierDeliveryBehavior.CommitDispatch.cs:22`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs:19`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PreparationAdmission.cs:20,46`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs:36,48,59`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs:24,117`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs:26,156`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptSchedule.cs:17`
- `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs:574`
