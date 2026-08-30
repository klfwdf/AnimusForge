# 阶段 2：Conversation / Memory / Action Contract Matrix

- 状态：纯 contract 设计完成；未实现 contract 类型；未运行测试
- 日期：2026-08-29
- owner：Conversation/AI、Memory/Persistence 与各动作域 owner 共同维护
- 依据：`docs/animusforge-phase2-root-llm-owner-slice.md`、`docs/animusforge-phase2-impact-bridge-rollback-map.md`、`docs/free_conversation_scene_shout_alignment.md`

> 本文只定义逐文件影响、DTO 方向、纯 fixture 和验证矩阵。它不授权移动 `MyBehavior.cs`、`ShoutBehavior.cs` 或任何生产 C#，也不替换三渠道现有链路。

## 1. 三条 contract 边界

### 1.1 `ConversationContextSnapshot`

由信使、自由对话、场景喊话入口分别组装，交给 Conversation/AI facade。只包含不可变、可脱离游戏对象表达的数据：

```text
Channel: SceneShout | NativeConversation | Courier
ParticipantIds: stable hero/party identifiers
ConversationGeneration: stale-work guard
UserText: bounded sanitized text
VisibleHistory: bounded role/content entries
MemoryView: bounded facts/observations
EligibleRuleIds: stable rule IDs
PostprocessRuleIds: stable rule IDs for {tag_rules}
ProfileId: stable profile name
ApiLine: 1.3 | 1.4
```

禁止包含 `Hero`、`Agent`、`Mission`、`Game`、`IDataStore`、UI VM、API key、raw network response 或无限长度文本。

### 1.2 `MemoryExchangeRecord`

由 Conversation/AI 产生、由 Memory/Persistence facade 在主线程边界写入：

```text
Channel
ParticipantId
Role: user | assistant
Text: bounded visible text
OccurredAt / CampaignTime
ConversationGeneration
Facts: AFEF-only confirmed facts
Source: SceneShout | NativeConversation | Courier
```

NPC 发言使用 `assistant`，玩家发言使用 `user`；已发生事实必须使用 AFEF 结构，不能把模型猜测写成事实。该 contract 不改变已有 `SyncData` key/type。

### 1.3 `AuthorizedActionPlan` / `ActionExecutionResult`

后处理标签解析后形成受限动作计划，主线程验证当前状态并执行：

```text
AuthorizedActionPlan:
  ActionId
  TargetIdentity
  Parameters: typed bounded values
  SourceChannel
  ConversationGeneration
  RequiredRuleId
  ExpectedStateFingerprint
  ContractVersion

ActionExecutionResult:
  Status: Applied | Rejected | Expired | Unsupported | Failed
  ActionId
  TargetIdentity
  ReasonCode
  AppliedFacts: AFEF-only confirmed facts
  ConversationGeneration
```

动作计划不携带 live Hero/Agent/Mission，不直接调用另一个模块的私有 Behavior；目标解析、授权、状态验证、主线程 apply 和结果记录是四个可诊断步骤。

## 2. 逐文件影响表

| 当前文件 | 现有责任 | Contract 方向 | 影响面 | 非目标 | 回滚入口 |
|---|---|---|---|---|---|
| `MyBehavior.cs` | 历史、记忆、AFEF、Prompt 上下文、持久化 facade | 读 `MemoryView`；写 `MemoryExchangeRecord` | Save、三渠道历史、generation、AFEF | 不改 SyncData key/type；不把 facade 变成新存档类型 | 保留现有方法；新 facade 旁路 |
| `ShoutBehavior.cs` | 场景喊话组织、回复、后处理、动作和主线程队列 | 组装/消费三类 contract | Prompt、tag_rules、动作、主线程、队列 | 不重写场景喊话协议；不在后台执行游戏动作 | 关闭新 orchestration adapter，保留旧入口 |
| `AIConfigHandler.cs` | 配置、规则、Prompt、profile | 提供稳定 rule/profile IDs | Prompt/Rule、profile closure、内容加载 | 不在 DTO 内保存 Prompt 正文或密钥 | 回到既有配置读取和默认 profile |
| `ShoutNetwork.cs` | LLM gateway 请求和回复 | 输入 snapshot；输出 bounded reply envelope | 后台、取消、重试、网络错误 | 不把 live game object 传入后台；不写 save | 禁用 gateway adapter，走 native/安全回复 |
| `LlmApiCompat.cs` | LLM API 兼容层 | 适配 reply envelope | 1.3/1.4、API schema | 不改变游戏 API 线选择，不把兼容差异扩散到 domain contract | 保留旧 API 调用路径 |
| `LlmVisibleReplyNormalizer.cs` | 可见回复安全归一化 | reply envelope → bounded visible text | 输出安全、标签剥离、隐私 | 不吞掉动作标签执行结果；不记录 API key | 原有 normalizer/fallback |
| `AnimusForgeNativeConversationOverlay.cs` | Native conversation UI/adapter | UI 输入 → `ConversationContextSnapshot` | UI、RootWidget、输入焦点、主线程 | 不让 UI VM 拥有 campaign state；不处理存档 | 禁用 overlay，回原版 UI |
| `AnimusForgeNativeConversationOverlayVM.cs` | UI datasource/交互 | 展示 reply/status | UI、延迟关闭、Backspace | 不直接执行 action 或写 memory | 保留原 datasource 和 native page |
| `CourierDeliveryBehavior.cs` | 信使入口、回复和交付状态 | Courier ↔ Conversation contract | Courier save、渠道、目标身份 | 不创建独立 prompt/history schema | 保留原 Courier behavior 注册和 fallback |
| `ConversationMessage.cs` | 对话消息模型 | `MemoryExchangeRecord` 的候选输入 | user/assistant 角色、长度边界 | 不直接等同于 confirmed AFEF fact | 旧消息模型继续使用 |
| `PromptComposer.cs` / `PromptListRetrievalService.cs` | Prompt 组合和规则读取 | rule/profile IDs → bounded prompt snapshot | Prompt、profile、内容 ownership | 不让 Bridge 保存 Prompt 正文或用户数据 | 默认 profile/旧 composer |
| `ActionPostprocessConfigModel.cs` | 后处理动作配置模型 | tag rule ID → action definition metadata | `{tag_rules}`、内容版本 | 不在配置模型执行游戏动作 | 旧标签解析和安全忽略未知标签 |
| `SaveRuntimeGuard.cs` | save/load generation、stale work guard | generation 附着在 snapshot/result | Save、线程、取消 | 不改存档类型或 key | 既有 stale guard 原路径 |
| `Patch_Conversation_Start_Intercept.cs`、`Patch_ConversationManager_OpenMapConversation.cs`、`Patch_ConversationManager_SetupAndStartMapConversation.cs` | 原版 conversation/encounter 入口适配 | 入口 → participant identity snapshot | Harmony、军团成员目标、原版 fallback | 不无条件覆盖已解析 target Hero | 关闭 patch，回原版 conversation |

## 3. 三渠道一致性矩阵

| 能力 | SceneShout | NativeConversation | Courier | 统一要求 |
|---|---|---|---|---|
| 话题资格 | 必须使用同一规则资格来源 | 必须使用同一规则资格来源 | 必须使用同一规则资格来源 | 不允许渠道私有规则绕过 owner |
| 历史读取 | `MemoryView` | `MemoryView` | `MemoryView` | 相同 role 语义和有界顺序 |
| 玩家发言 | `user` | `user` | `user` | 写入同一记忆结构 |
| NPC 回复 | `assistant` | `assistant` | `assistant` | 可见回复与历史一致 |
| 后处理规则 | `{tag_rules}` | `{tag_rules}` | `{tag_rules}` | 话题命中必须注入后处理规则 |
| 动作执行 | `AuthorizedActionPlan` | `AuthorizedActionPlan` | `AuthorizedActionPlan` | 统一授权/验证/主线程执行 |
| 事实写入 | AFEF confirmed fact | AFEF confirmed fact | AFEF confirmed fact | 只记录已发生结果，不记录猜测 |
| LLM 不可用 | 安全 fallback | 原版/安全 fallback | 原版 courier fallback | 不因 AF 关闭而吞原版逻辑 |

## 4. 纯 contract fixture

### 4.1 有效输入/输出

```yaml
Input:
  Channel: Courier
  ParticipantIds: [hero_1]
  ConversationGeneration: 42
  UserText: "请把这封信交给领主"
  VisibleHistory:
    - Role: user
      Text: "请把这封信交给领主"
  MemoryView:
    - FactId: fact_001
      Text: "双方已约定在城门会面"
      Confirmed: true
  EligibleRuleIds: [courier_delivery]
  PostprocessRuleIds: [courier_delivery]
  ProfileId: default
  ApiLine: 1.4

Output:
  Status: Accepted
  Next: LlmGatewayRequest
  ActionRulesInjected: true
  SaveWrite: DeferredToMainThread
```

### 4.2 无效输入/输出

| fixture | 违规 | 结果 |
|---|---|---|
| `live-object-input` | snapshot 携带 `Hero`/`Mission`/`Agent` | `Rejected: LiveObjectNotAllowed` |
| `missing-postprocess-rules` | 有话题规则但 `PostprocessRuleIds=[]` | `Rejected: PostprocessRuleClosureViolation` |
| `wrong-role` | NPC 回复写成 `user` | `Rejected: InvalidHistoryRole` |
| `unconfirmed-fact` | 未确认模型猜测放入 AFEF | `Rejected: FactConfirmationRequired` |
| `stale-generation` | result generation 小于当前 save/conversation generation | `Expired: StaleGeneration` |
| `unknown-action-target` | action 只有裸坐标或未知 identity | `Rejected: TargetIdentityRequired` |
| `background-apply` | 后台线程直接执行 Hero/Agent/Mission 修改 | `Rejected: MainThreadApplyRequired` |
| `unbounded-text` | history/reply 超过 contract 上限 | `Rejected: BoundedTextLimitExceeded` |
| `optional-memory-provider` | 可选 Memory provider 缺失但 native history 可用 | `Degraded: MemoryProviderUnavailable` |

## 5. 纯 contract test matrix（设计，不执行）

| Case | 输入 | 预期 | 当前状态 |
|---|---|---|---|
| channel parity | 三渠道同一 participant/rule/fact | snapshot 字段语义一致 | NOT-RUN |
| postprocess closure | topic rule + 对应 postprocess rule | `{tag_rules}` 含对应规则 | NOT-RUN |
| role semantics | user/assistant 交替历史 | 角色不被渠道改写 | NOT-RUN |
| AFEF confirmation | applied action result | 仅 confirmed result 进入 facts | NOT-RUN |
| stale completion | old generation reply | 不写 history、不执行 action | NOT-RUN |
| main-thread apply | background reply + main queue | 只在主线程 apply | NOT-RUN |
| action rejection | wrong target/current state | `Rejected`，无副作用 | NOT-RUN |
| optional provider | Memory provider missing | `Degraded` + fallback | NOT-RUN |
| required provider | Conversation facade missing | `Invalid`/SafeMode selection | NOT-RUN |
| API line parity | 1.3 and 1.4 adapter snapshots | domain contract 不变 | NOT-RUN |
| persistence identity | serialize existing record | key/type 不变 | NOT-RUN |
| bounded output | oversized text/network error | 有界错误，不重试空转 | NOT-RUN |

## 6. 下一步与回滚

- 下一步不是移动这些文件，而是把本矩阵中的字段映射到现有真实方法/调用点，并为纯 contract test 建立独立测试输入目录。
- 第一实施切片应只引入不可变 DTO/纯转换函数，保留 `MyBehavior`、`ShoutBehavior`、`CourierDeliveryBehavior` 作为 facade；不得同时改 Prompt、标签执行和存档。
- 若 contract 不能表达目标身份、当前状态、generation 或事实确认，先修 contract，不增加第三个 workaround。
- 任何失败均可通过不接入新 DTO、保留旧 facade、禁用单个 adapter 或回到原版 fallback 回滚。
- 不改变 `SubModule.cs` 注册顺序、`SubModule.xml`、程序集身份、SyncData key/type、三渠道语义和 1.3/1.4 双实现构建策略。

## 7. 未验证项

- 未实现 contract DTO、纯转换函数或测试项目；
- 未运行三渠道、旧存档、1.3/1.4 构建、打包、部署和游戏内验证；
- 未完成所有文件的真实方法级调用图；本表是 owner/contract 设计切片，不是迁移清单；
- 未确认精确 `v1.4.8.119303` overlay、许可证和第三方 provenance。