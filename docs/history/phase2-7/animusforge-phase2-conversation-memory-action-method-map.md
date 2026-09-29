# 阶段 2：Conversation / Memory / Action 方法级调用点地图

- 状态：只读方法映射完成；未修改生产 C#；未接入 DTO/fixture runner
- 日期：2026-08-29
- owner：Conversation/AI、Memory/Persistence、Action/Host 共同维护
- fixture 目录：`F:\AnimusForge-main\docs\fixtures\phase2-conversation-memory-action\`
- 说明：行号是本次调查时的观察值；后续源码变化后必须重新核对，不能把行号当稳定 API。

## 1. 方法级主链

```text
入口渠道
  → Patch_Conversation_* / Native overlay / CourierDeliveryBehavior
  → ShoutBehavior 组装历史、规则和场景上下文
  → MyBehavior 提供 history/memory/AFEF/persistence facade
  → AIConfigHandler / PromptComposer 提供 rule/profile/prompt metadata
  → ShoutNetwork.CallApiWithMessages 执行后台 LLM 请求
  → LlmVisibleReplyNormalizer 清理可见回复
  → ShoutBehavior 后处理标签、授权动作和主线程队列
  → MyBehavior.AppendExternal* 写入 history/AFEF
  → SaveRuntimeGuard 拒绝 stale generation
```

## 2. 真实调用点与 contract 对应

| 文件与观察行 | 真实方法/调用点 | 对应 contract | 线程/频率 | 后续纯测试重点 | 回滚边界 |
|---|---|---|---|---|---|
| `MyBehavior.cs:17991` | `SyncData(IDataStore dataStore)` | `MemoryExchangeRecord` 的持久化适配边界 | 存档/加载边界；非 Tick | key/type/chunk 保持；空字段恢复 | 保留旧 `SyncData` 实现 |
| `MyBehavior.cs:27343` | `AppendDailyMemoryLine(...)` | Memory record 输入 | 对话/事实事件；主线程写入 | speaker、AFEF、scene session、target metadata | 不接入新 DTO，继续旧方法 |
| `MyBehavior.cs:27353` | `AppendDailyMemoryLineById(...)` | Memory record normalized form | 对话/事实事件；主线程写入 | memory ID、空文本、day/hour、session | 保留 ID 归一化和 eligibility |
| `MyBehavior.cs:27626` | `AppendExternalDialogueHistory(...)` | `MemoryExchangeRecord` facade | 跨域事件入口 | NPC=`assistant`、玩家=`user`、fact=`AFEF` | 保留 static facade |
| `MyBehavior.cs:27637` | `AppendExternalSceneDialogueHistory(...)` | SceneShout memory record | 场景对话事件 | scene session、agent index/name | 关闭新 adapter 仍可调用旧 facade |
| `MyBehavior.cs:27670` | `AppendDialogueHistory(...)` | Memory write orchestration | 事件触发；主线程边界 | 三段输入顺序和空值规则 | 不移动原实现 |
| `MyBehavior.cs:27977` | `GetDialogueHistoryEntriesForExternal(...)` | `MemoryView` | Prompt 组装前；按需读取 | maxLines 有界、缺行为 fallback | 返回旧 history entries |
| `MyBehavior.cs:28011` | `GetDialogueHistoryEntries(...)` | `MemoryView` 内部读取 | 按需 | hero→memory ID 映射 | 保留现有解析 |
| `MyBehavior.cs:34003` | `BuildHistoryContext(...)` | `ConversationContextSnapshot.VisibleHistory` | Prompt 组装前 | current/secondary input、active scene session | 旧 Prompt history builder |
| `MyBehavior.cs:34012` | `BuildHistoryContextById(...)` | `ConversationContextSnapshot.MemoryView` | Prompt 组装前 | non-hero memory ID、maxLines | 旧 by-id facade |
| `MyBehavior.cs:16723` | `BuildPlayerCustomPromptRuleBlock()` | `EligibleRuleIds` / profile prompt input | 按请求读取配置 | 空规则、边界文本、敏感信息不外泄 | 旧 custom rule block |
| `ShoutBehavior.cs:13564` | `GenerateGroupConversationTurnLineAsync(...)` | `ConversationContextSnapshot` → reply envelope | 后台生成；结果回主线程 | immutable input、NPC audience、extra fact、取消/stale | 关闭新 orchestration，旧入口继续 |
| `ShoutBehavior.cs:1764` | `DrainMainThreadActionsForMissionTick()` | `AuthorizedActionPlan` apply queue | Mission tick；1.3 有预算，1.4 逐项 drain | 队列上限、预算、丢弃/剩余统计 | 不改当前 drain |
| `ShoutBehavior.cs:1800` | `ExecuteMainThreadAction(Action)` | action apply boundary | 主线程 | 异常隔离、执行一次、诊断 | 保留原 action queue |
| `ShoutBehavior.cs:1860` | `OnApplicationTickForMainThreadActionsExternal()` | Host→main-thread drain adapter | ApplicationTick | 与 EngineTick 不混用 | 取消新 adapter |
| `ShoutBehavior.cs:34082` | `TryConvertAfefFactToStrictChatMessage(...)` | AFEF → prompt message | Prompt 组装前 | AFEF 角色为 `user`、current fact 标记 | 旧消息转换逻辑 |
| `ShoutBehavior.cs:34098/34103` | `TryConvertSceneMessageToStrictChatMessage(...)` | history → strict chat message | Prompt 组装前 | user/assistant、目标 agent、旁听标签 | 保留现有渠道格式 |
| `ShoutBehavior.cs:34170` | `BuildConversationMessageDedupeKey(...)` | history dedupe metadata | Prompt 组装前 | AFEF/role/content 去重 | 旧 dedupe key |
| `ShoutBehavior.cs:24845` | `TryRunSceneUnifiedActionPostprocess(...)` | reply → `AuthorizedActionPlan` | 后处理；结果进入主线程 | `{tag_rules}`、授权、target identity、结果事实 | 回到旧 postprocess fallback |
| `ShoutBehavior.cs:22003` | `BuildPostprocessRuleTextForScene(...)` | postprocess rule closure | 后处理 Prompt 组装 | 话题命中与后处理规则必须同源 | 保留规则文本 builder |
| `ShoutBehavior.cs:25509` | `BuildSceneActionPostprocessUserPrompt(...)` | action postprocess request | 后处理请求 | history/reply/rules bounded | 旧 user prompt template |
| `ShoutNetwork.cs:751` | `CallApiWithMessages(...)` | snapshot → LLM gateway | 后台网络；capture generation | API key/model error、retry、stale response | native/safe fallback |
| `ShoutNetwork.cs:398` | `BuildPrimaryChatPayload(...)` | domain messages → network payload | 后台请求前 | API line payload compatibility | 保留原 payload builder |
| `LlmVisibleReplyNormalizer.cs:28` | `NormalizeComplete(...)` | reply envelope → visible text | 回复处理 | envelope、think block、标签泄漏 | 旧 normalizer |
| `LlmVisibleReplyNormalizer.cs:43` | `NormalizeStreamingPreview(...)` | stream chunk → preview | 流式回复 | partial envelope、无界文本 | 停止新 stream adapter |
| `AnimusForgeNativeConversationOverlay.cs:1519` | `QueuePostprocessNotice(...)` | action result → UI notice | 主线程/overlay queue | generation、NPC 名称、延迟显示 | 禁用新 UI notice |
| `AnimusForgeNativeConversationOverlay.cs:1538` | `FlushPendingPostprocessNotice()` | pending UI notice → visible UI | overlay tick | stale notice、关闭/输入焦点 | 回原版 UI |
| `CourierDeliveryBehavior.cs:8692` | `PrepareNpcReplyForActionPostprocess(...)` | courier reply → postprocess input | Courier 回复链 | 去 envelope/think/header；保留标签 | 旧 courier clean path |
| `CourierDeliveryBehavior.cs:8704` | `CleanNpcReply(...)` | reply → courier visible text | Courier 回复链 | API error 不误清理、敏感文本清理 | 原有 Courier fallback |
| `CourierDeliveryBehavior.cs:2863+` | `MyBehavior.AppendExternalDialogueHistory(...)` 调用点 | Courier → MemoryExchangeRecord | 交付/回信事件 | delivery fact、NPC/player role | 保留 Courier 直接 facade |
| `SaveRuntimeGuard.cs:12` | `CaptureGeneration()` | snapshot generation | 每次后台请求开始 | generation 正确附着 | 保留 guard |
| `SaveRuntimeGuard.cs:35` | `IsStale(...)` | result generation validation | 网络/后台结果返回 | stale 不写 history、不执行 action | 保留 stale rejection |

## 3. 方法级不变量

1. `MyBehavior` 是现有 Conversation/Memory 的兼容 facade；不能以 DTO 抽取为理由改变既有 `SyncData` key/type。
2. `ShoutBehavior` 可以组织流程，但 action 必须经过后处理、授权、当前状态验证和主线程 apply；LLM 正文不能直接成为游戏事实。
3. `ShoutNetwork` 的输入必须是不可变/可序列化快照；不得捕获 `Hero`、`Agent`、`Mission`、`Game` 或 UI 对象。
4. `LlmVisibleReplyNormalizer` 只负责可见文本安全化，不能吞掉动作标签结果，也不能把动作标签泄漏到 NPC 可见文本。
5. Courier 不得另造与 SceneShout/NativeConversation 不兼容的 history、role、AFEF 或 postprocess 语义。
6. `TryConvertAfefFactToStrictChatMessage` 产生的 AFEF prompt message 使用 `user` 语义；NPC 可见发言使用 `assistant` 语义。
7. `DrainMainThreadActionsForMissionTick` 和 `OnApplicationTickForMainThreadActionsExternal` 仍是不同调度面；contract 不得合并二者。
8. `SaveRuntimeGuard.IsStale` 失败时只丢弃过期结果，不回写存档、不执行动作、不伪造 fallback 事实。

## 4. 纯 fixture 与源码的关系

- fixture 放在 `F:\AnimusForge-main\docs\fixtures\phase2-conversation-memory-action\`，不在任何 `.csproj` 中；
- fixture 的 ID、channel、role、generation、rule ID 和 action result 只能作为纯数据输入；
- fixture 不引用 TaleWorlds 程序集，不实例化 AF Behavior，不调用网络，不读存档；
- 未来若添加测试项目，必须先复用这些输入并保持输出 contract，不得让测试 fixture 反向定义生产 save identity。

## 5. 尚未完成

- 未为上述方法新增 DTO 或 adapter；
- 未建立方法级自动调用图；当前是人工读取的稳定入口清单；
- 未运行纯 contract tests、三渠道回归、旧存档、双版本构建或游戏测试；
- 未授权修改生产 C#。