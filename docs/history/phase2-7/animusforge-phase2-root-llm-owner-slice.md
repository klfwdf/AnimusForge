# 阶段 2 首条只读切片：根 AF 基础 LLM 对话 Owner 映射

- 阶段：阶段 2：模块目录与所有权地图
- 状态：只读映射完成；未移动源码
- 日期：2026-08-29
- 基线存档：用户已选择并手动测试 `C:\Users\29310\Documents\Mount and Blade II Bannerlord\Game Saves\saveauto2.sav`；详细测试记录按用户决定跳过
- 范围：根 AF 基础 LLM 对话/场景喊话链路，不扩展到全量玩法回归
- 约束：保持单一 `AnimusForge.dll`、现有入口、程序集身份、SyncData key、存档类型和三渠道兼容边界

## 1. 目标链路

```text
SubModule / Host
  → CampaignGameStarter 注册 MyBehavior、ShoutBehavior、CourierDeliveryBehavior
  → ShoutBehavior 组织场景喊话/Native Conversation 的会话上下文
  → MyBehavior 提供历史、记忆、AFEF 和 ShoutPromptContext facade
  → AIConfigHandler 提供配置、规则、Prompt 和后处理规则
  → ShoutNetwork 调用主 LLM（stream / non-stream）并处理 stale generation、重试和响应提取
  → ShoutBehavior 处理可见回复、后处理/动作入口和主线程动作
  → MyBehavior.AppendExternal*History 写入对话历史/AFEF/记忆
  → SubModule 每帧调度主线程动作、TTS、UI 和 engine tick
```

## 2. 当前入口与目标 Owner

| 当前入口/文件 | 已核对职责 | 目标逻辑 owner | 本切片结论 |
|---|---|---|---|
| `SubModule.cs` | `OnSubModuleLoad` 初始化外部 SceneActions；`OnBeforeInitialModuleScreenSetAsRoot` 应用 Harmony；`InitializeGameStarter` 注册行为；`OnApplicationTick` 调度主线程动作、UI/TTS 和各行为 engine tick | Host/Composition | 组合根瓶颈；先保留注册顺序和调度顺序，不移动 |
| `MyBehavior.cs` | `SyncData`；对话历史/每日记忆/压缩记忆/AFEF；`OnSessionLaunched` 调用 `AIConfigHandler.ReloadConfig()`；`BuildShoutPromptContextForExternalInternal`；`AppendExternalDialogueHistory` / `AppendExternalSceneDialogueHistory` | Conversation facade + Memory/Persistence | 不整体迁移；先作为公共 facade 和存档 owner 保留 |
| `ShoutBehavior.cs` | 场景喊话、Native Conversation、Prompt 组装、后处理、动作、TTS、目标/Agent、主线程动作队列；调用 `ShoutNetwork.CallApiWithMessages*`；调用 `MyBehavior.AppendExternal*History` | Conversation orchestration + Scene/Action adapter | 当前主编排器；后续只按 DTO、目标解析、ActionPlan 等可回退 seam 拆分 |
| `AIConfigHandler.cs` | 配置状态；`BuildMatchedExtraRuleInstructions`；规则资格/关键词/语义命中；后处理规则；`ReloadConfig`；主 API 与后处理 API 配置解析 | Prompt/Rule + LLM Gateway configuration | 纯规则/配置责任候选；不得把领域副作用放入这里 |
| `ShoutNetwork.cs` | `CallApiWithMessages` 非流式、`CallApiWithMessagesStream` 流式；HTTP 认证、请求重试、响应提取、token 统计、`SaveRuntimeGuard` generation 检查 | LLM Gateway adapter | 后台网络边界；不得携带 live TaleWorlds 对象，结果必须回主线程复核 |
| `LlmApiCompat.cs` | 认证头、请求格式、assistant/reasoning/stream 文本提取和 API 兼容 | Game-independent LLM compatibility | 保持纯输入输出方向，后续可独立测试 |
| `LlmVisibleReplyNormalizer.cs` | 完整/流式回复 envelope 清理、可见文本规范化、内部格式隔离 | LLM Gateway/Safety | 可见回复安全边界；内部 action/AFEF 标签不能泄露 |
| `AnimusForgeNativeConversationOverlay.cs` | Native 对话 UI 的 tick、输入、提交、历史/编辑入口；调用 `ShoutBehavior` facade | UI + Conversation adapter | UI 不拥有 campaign state；保持主线程和输入焦点边界 |
| `CourierDeliveryBehavior.cs` | 信件、Courier party、异步回复、送达/返回动作和持久化状态机 | Courier + Conversation adapter | 不并入根 LLM 编排；仅共享公共 Conversation/Memory/Action 契约 |

## 3. 关键证据位置

- `SubModule.cs:629-665`：行为注册顺序；`MyBehavior` 在 `ShoutBehavior` 前注册，`CourierDeliveryBehavior` 紧随其后。
- `SubModule.cs:727-837`：Application Tick、主线程动作、Native TTS、`MyBehavior`/`CourierDeliveryBehavior` 等 engine tick 调度。
- `MyBehavior.cs:17991`：`SyncData` 入口；保存边界不能在未完成 key/type 审计前改变。
- `MyBehavior.cs:19052`：`OnSessionLaunched`，触发配置 reload。
- `MyBehavior.cs:27626-27670`：对话历史/场景历史/非英雄历史的外部 facade。
- `MyBehavior.cs:30974`：`BuildShoutPromptContextForExternalInternal`，Prompt 上下文的主要构建入口。
- `ShoutNetwork.cs:751`：非流式主 LLM 请求；`ShoutNetwork.cs:992`：流式主 LLM 请求。
- `AIConfigHandler.cs:6941-6956`：额外规则指令组合；`AIConfigHandler.cs:9141`：配置 reload。
- `AnimusForgeNativeConversationOverlay.cs:18`、`:101-170`：Native UI adapter 和 tick 入口。

## 4. 当前边界判断

1. `SubModule.cs` 是 Host/Composition owner，不是根 AF LLM 业务 owner；第一步只能做注册/调度分组设计，不能改变回调顺序。
2. `ShoutBehavior.cs` 是当前交互编排中心，但它依赖 `MyBehavior` 的存档/历史 facade、`AIConfigHandler` 的规则状态和 `ShoutNetwork` 的后台请求；不能整体搬迁。
3. `MyBehavior.cs` 同时拥有持久化和对话/记忆 facade，是旧存档兼容的高风险边界；任何拆分必须先冻结 key/type 和 facade。
4. `AIConfigHandler.cs`、`ShoutNetwork.cs`、`LlmApiCompat.cs` 和 `LlmVisibleReplyNormalizer.cs` 已有较清楚的逻辑候选边界，但当前仍处于单一程序集内部，不代表可以直接拆 DLL。
5. 根 AF 基础 LLM 对话的最小验收应优先覆盖场景喊话主链路；Native Conversation 是兼容/适配入口，Courier 是独立异步状态机，不能因“都调用 LLM”而合并生命周期。

## 5. 性能与线程记录

- 本切片为只读源码映射，运行频率为 0，不增加运行时分配、扫描、锁或 Tick 工作。
- 现有热路径必须继续区分：Application Tick、Mission Tick、Campaign engine tick、LLM 后台请求和主线程 action drain。
- 后台请求只接收消息快照/字符串 DTO；TaleWorlds 对象、Agent、Mission 和存档写入必须留在主线程边界。
- 后续若抽 DTO/ActionPlan，应记录队列上限、generation、取消、stale result、主线程预算和日志证据。

## 6. 非目标与回滚

- 不移动或重命名任何生产 C#。
- 不修改 `SubModule.cs` 注册顺序。
- 不修改 Prompt JSON、action tag、SyncData key、存档类型或程序集身份。
- 不创建新的物理玩法 DLL。
- 回滚方式：删除本只读报告并恢复 live plan/handoff 的文档修改；生产源码无变更。

## 7. 下一项准确任务

> 在不改变运行行为的前提下，为 `SubModule.cs` 建立注册/调度分组清单，或者先定义 `Conversation` 的公开 DTO/`ActionPlan` facade；两者都必须保持旧入口、存档和三渠道契约。
