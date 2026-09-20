# 当前交接：J09 Actions / 事实提交离线整包闭合，下一包 J10（2026-09-21）

- **状态**：J07、J08、J09 均 `OFFLINE_VERIFIED`。J09a–J09e 已完成；自动化 `af-7-8` 在本交接提交/推送后将改为从 J10 开始。
- **产品提交**：`20ba9527` Tags；`fd01974b` Plan/Execute；`dbe87c4` Receipts；`21206ec6` raw overflow；`65a14421` 动作终态；`bb223aec`/`f61ec13e` shared channel boundary；`f4f022a3` request-bound compatibility executor；`449227a9` 默认三渠道接线；`beb7dd38` minimal identity capture；`2a1fc124` Scene relay 时序修复。
- **关键结果**：`AF.Module.Actions/{Tags,Plan,Execute,Receipts}` 是唯一共享动作协议。detached 和默认 Native/Scene/Courier 均经过严格 raw/plan、canonical request/action identity 与 success/reject/partial/unknown 终态；默认渠道只用 action-only boundary，不调用 `InteractionResultCommitter`，因此不重复可见历史或 AFEF。领域玩法仍归旧 owner/typed port，不搬入通用 Actions。
- **默认渠道保持**：Native completion/TTS/WorldMap exit 与 pending-history rollback 未迁；Scene 保持 relay 先解析、mood→GCCZ→direct→follow/speech，queued speech 完成后才发布 relay；Courier 仍只有 `DeliveryApplied` 后的到达/回复 owner 执行动作，detached 路径直接调用领域 core，避免边界套娃。
- **验证**：ActionProtocol 14 + 5 变异；Interaction/Economy/Duel；Scene 71/37 + 7 Queue 变异；Courier 39/34 + 8 owner 变异；Native action 91/admission 44/completion 184；默认三渠道 wiring 25；Debug/Release 六构建全部 0 warning/0 error；四实际 DLL 1060 API/metadata；Persistence/Profile 142/168/13/44；Bridge、Phase8 和 334 锚点地图通过。
- **下一步 J10**：只拆 Scene group/relay/passive/reaction 与 Courier transport/pregeneration/arrival/letter/retry 会话 owner；复用 J09 action boundary，不再次改写 parser/receipt。Scene pending AFEF、玩家发言去重、旁听/距离与 speech/relay 终态必须保留；Courier 预生成不得执行动作，旧 retry 不得改变新 session。
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
- `ShoutBehavior.ScenePostprocess.cs:482`
- `CourierDeliveryBehavior.CommitDispatch.cs:22`
- `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs:574`
