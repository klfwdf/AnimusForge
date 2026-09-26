# 独立子 MOD 公共 API V1

## 当前交付边界

**目录查询及 Native、Scene、Courier 提交/结果/开始前取消已接线；J14 同候选最终离线验收已完成（`J14_OFFLINE_VERIFIED`）。**

J14b3 在原草稿准入与运输 owner 上开放 Courier 公共接口，返回只读阶段回执。完成需要必要动作/历史接受、回信实际入库及运输收尾确认，预生成/consumed/Completed 标志不单独判成功。开放前独立消费者、内部枚举重排及 Debug 双版本/Bootstrap/元数据已验证；最终六构建、四 DLL 元数据、旧 Native 二进制 ABI、三渠道/记忆回读及当前 DLL 回放均通过，详细证据见主台账；不能以此宣称实机或发布完成。

当前物理分区：纯 V1 契约位于 `src/AF.Contracts/PublicApi/V1/AfApiContracts.cs`；同一 `AnimusForge.dll` 内的入口/客户端在 `src/modules/AF.Module.PublicApi/V1/{AfApi,AfDialogueClient}.cs`，快照/对话投影在 `src/modules/AF.Module.PublicApi/Internal/{AfV1SnapshotProjection,AfV1DialogueProjection}.cs`。上述分区不改变 `AnimusForge.Api.V1` namespace 或程序集身份；本轮方法/DTO 是保留旧 ABI 的兼容性扩展。

| 能力 | 状态 | 实际含义 |
|---|---|---|
| `CatalogRead` | Available | 任意线程查询框架装配、只读模块接缝目录 |
| `NativeSubmit` | Available | 对当前 AF 原生自由对话提交玩家文本，复用实际准入、正文、标签、动作、记忆 owner |
| `SceneSubmit` | Available | 对当前 AF 场景喊话框选签发 client 绑定票据，再由原群组 owner 提交；等待相关发言、语音发布、后处理和必要记忆回执 |
| `CourierSubmit` | Available | 领取原 UI 已完成选择的草稿；同一派出与运输 owner 执行，等待实际回信交付及必要收尾 |
| `ActionExecute` / `MemoryWrite` / `ExtensionRegister` | NotSupported | 不提供任意动作、事实写入或第三方 provider 注册 |

`Available` 是 API 契约存在，不是当前游戏/目标可执行，更不是实机验收通过。框架快照的 `Ready` 也仅代表装配。

## 最小调用方式

```csharp
using AnimusForge.Api.V1;

// 每个使用者维护自己的 client；不要每次重试都创建新的 client。
AfDialogueClient client = AfApi.CreateDialogueClient();
if (AfApi.GetCapability(AfCapabilityIds.NativeSubmit, 1).State == AfCapabilityState.Available)
{
    AfDialogueOperation operation = client.SubmitNative("conversation-42-turn-1", "你好，近来如何？");
    AfDialogueResult receipt = await operation.Completion;
    if (receipt.State == AfDialogueState.Completed)
    {
        string reply = receipt.Reply;
        // 若需要更新自有 UI，子 MOD 自行切回自己的主线程。
    }
    else
    {
        // 根据 State / EffectState / ReasonCode 提示；不要自动换 ID 重发。
    }
}
// owner 卸载时释放；只取消自己尚未开始的请求，不撤销已开始的游戏效果。
client.Dispose();
```

Scene 必须先由游戏主线程在当前有效的 AF 场景喊话框选期间调用 `client.CaptureSceneContextTicket()`；返回 `null` 表示当前无可签发上下文。签发只预览框选，不推进玩家输入序号、不发送请求或写历史。随后可从任意线程提交：

```csharp
string ticket = client.CaptureSceneContextTicket(); // 在游戏主线程调用
if (ticket != null)
{
    AfDialogueOperation operation = client.SubmitScene(ticket, "scene-turn-1", "请说说你的看法");
    AfDialogueResult receipt = await operation.Completion;
    if (receipt.State == AfDialogueState.Completed)
    {
        foreach (AfSceneUtterance utterance in receipt.SceneUtterances)
        {
            // SpeakerAgentIndex 仅供诊断；不是可长期持有的 Agent 句柄。
            string line = utterance.SpeakerName + ": " + utterance.Text;
        }
    }
}
```

Scene 票据为不透明、一次 claim、当前 owner/游戏代次/场景会话/输入序号/框选目标绑定。改选、UI 抢先提交、场景或存档更替、错 client 或重复领取均拒绝；失败后应重新捕获，不能擅自换 ID 自动重发。每个 Scene owner 最多保留 128 张待领取票据；失效或 client 释放会撤销。三渠道共用每 client 128 个请求 ID 上限。同 ID 的渠道、票据或原文本不同均冲突；完全相同的重试返回同一内部 operation 的包装。

Courier 必须先在原 UI 选好成员、模式和附件；API 不能构造任意收件人或绕过这些选择。主线程捕获，随后可从任意线程提交：

```csharp
string ticket = client.CaptureCourierContextTicket(); // 游戏主线程；无有效草稿时为 null
if (ticket != null)
{
    AfDialogueOperation operation = client.SubmitCourier(ticket, "courier-letter-1", "信件正文");
    AfDialogueResult result = await operation.Completion; // 可能等待多个 Campaign tick
    AfCourierReceipt courier = result.Courier;
    // 仅 ReplyDelivered 确认后公开 Reply；失败也可能保留已确认阶段，不能自动重派。
}
```

Courier 票据绑定 client、owner、游戏 generation、草稿实例与修订；待领票据和 owner 活动/未排空提交各有 128 上限。原 UI/API 共用派出前重验与一次 claim；旧 UI 回调不修改或清理新草稿。API 关联仅在内存，读档不恢复旧 operation、不认领入站来信或恢复运输；读档/退休结算旧任务但不伪造效果回滚。

同一 client + 同一 request ID + 同一渠道/票据 + 完全相同文本再次提交，复用同一个内部 operation，绝不再发 LLM 或执行动作。相同 ID 的不同文本返回 `dialogue.request_id_conflict`，不覆盖已有任务。ID 区分大小写，不 trim；长度 1–128，仅允许英文字母、数字、`.` / `_` / `-` / `:`。文本不能为空，最多 16000 UTF-16 字符，实际规范化仍由相应原渠道 owner 完成。

### 有界去重，不静默淘汰

- 每个 client 的命名空间由 AF 创建，不能凭相同调用者名称取得别人的票据。
- 每个 client 最多接受 128 个不同有效请求 ID（包括终态和取消态）。满后返回 `dialogue.client_capacity`；不会淘汰旧 ID 后把重试变成二次动作。
- 去重只承诺该 client 的生命周期，不跨 client、重载或存档。创建新 client 是新的请求空间，不是旧请求的安全重试；制作组应在明确新会话/新业务周期更新 client。
- `Dispose` 后禁止新提交；已返回的 operation 仍可读结果。结果不写进存档，不添加每帧扫描。

## 结果和取消

| 字段/操作 | 承诺 |
|---|---|
| `Queued` | 尚未被真实主线程入口 claim，可以取消 |
| `Running` | 主线程入口开始处理；不能承诺取消/回滚 |
| `Completed` | 原唯一动作/必要记忆收尾产生真实完成回执；Courier 还需实际回信交付与运输归还确认，不按返回字符串推测 |
| `Rejected` | 未被相应渠道 owner 准入；包括 busy、失效、不可用、非法输入 |
| `Cancelled` | 取消赢在开始之前，没有本 operation 的游戏副作用 |
| `Failed` + `UnknownAfterStart` | 原 owner 已准入但没有完整回执；前置动作/部分记忆可能已经发生，不自动重试 |
| `Cancel()` | 返回 `CancelledBeforeStart` / `TooLate` / `AlreadyTerminal`；后两者不会改写真实结果 |

- `GetSnapshot()` 返回不可变副本；`Completion` 返回终态 DTO。后台结果/回调不直接操作游戏对象。
- Scene 的 `SceneUtterances` 是本次按顺序确认的只读可见发言快照；`Reply` 为这些发言的换行拼接。`CompletedByOwner` 不代表 TTS 播放结束，也不表示每一个生成标签都必然被游戏资格规则接受；资格/效果仍由原 owner 处理。退役/失败可携带部分已产生的发言，但整体仍为 `Failed + UnknownAfterStart`，不能据此重派。
- Courier 的 `Courier` 快照包含 `Dispatched`、`ReplyPrepared`、`Arrived`、`PayloadAccepted`、`DeliveryHistoryAccepted`、`ActionsAccepted`、`ReplyHistoryAccepted`、`ReplyDelivered`、`ContentsReturned` 和独立 `Transport`。字段不代表严格时间顺序；false 表示尚未确认，不证明已回滚。运输结局为 `NotStarted` / `InTransit` / `Returned` / `Destroyed` / `Missing` / `Unconfirmed`；送达后损失保留已接受动作/历史，交付后收尾失败可保留正文。非 Courier 或早期输入拒绝时该字段可为 null。
- 原方法返回空字符串、网络错误文案或换档错误文案都不能产生成功回执。异常不向公共 DTO 暴露原始消息、路径、Prompt 或凭据。
- V1 从不承诺正在执行的 HTTP 可被强制取消。已开始后的取消/Dispose 不抢占最终回执。

## Native 上下文与线程边界

- 不通过此入口指定任意 Hero、不创建第二场对话、不替子 MOD 弹自有 UI。必须已有当前 AF 可提交的 Native 对话。
- 任意线程可调用。请求在调用时绑定当前 owner、存档 generation 、真实 ConversationEnded epoch 和 presentation revision；排队期间换档、owner 替换或 A 对话结束改为 B 对话，旧请求不能进入 B；排队期间原UI先完成了另一回合，旧API请求也不会再补做。
- Hero / Character / Mission / token 在原主线程准入时捕获，随后全程使用原身份守卫。API 不声称后台提交瞬间已读取/冻结游戏 Hero；同一未结束会话在准入前改变目标时，由原准入时目标决定。准入后目标变化会拒绝晚结果。
- 与原 UI 共用后端 busy/admission。API 不能绕过正在运行的玩家对话；对现有 UI 的默认入口、开关和表现不做切换。
- 内部枚举经显式 V1 投影，不把内部 enum 数值布局变成公开协议。

Scene 使用原 `ProcessCapturedScenePlayerShoutAsync` 与每 Hero 群组链，不创建第二套 Prompt/动作/记忆 writer。live 游戏对象捕获和回写在游戏主线程；可分离的路由/知识检索在 worker，等待后重验 owner、generation、会话、轮次与目标。主群组、相关接力及后处理的回执才可完成；必要历史/AFEF 不可确认、无可见回复、语音未发布、超时或旧上下文均失败。群组、票据与去重仅按显式请求/阶段事件处理，无新增每帧全 client 扫描；API 分支每个发言在主线程同步装配持久历史，尚未进行实机帧耗时测量。

## 引用、兼容与目录查询

继续只依赖 `AnimusForge.Api.V1`。引用匹配游戏线的 `AnimusForge.dll`（`Private=false`），由 AF Bootstrap 选择唯一 1.3/1.4 实现；子 MOD 发行目录不能再携带另一份 AF DLL。

既有 `AfApi.GetSnapshot()` / `GetCapability()`、只读目录 DTO、枚举数值及版本 1 保持。新增方法/DTO 为兼容性扩展；今后不兼容修改另开版本。能力 ID 校验仍精确区分大小写：空/空白/超过128字符为 InvalidRequest，非1版本 VersionMismatch，未知ID UnknownCapability。

目录中的制作组 `IsExternallyCallable` 仍为 false：Native API 不是把制作组内部 port 公开。模块加载前/卸载后查询不初始化 AF、不发 LLM。同进程 DLL 不是安全沙箱；此层不承诺防御反射型恶意 MOD。

## 离线证据与未验证边界

`tools/NativeModuleSubmissionTests/README.md` 记录真实入口、物理主/后台线程、重复、取消、会话切换与失败回执测试。Provider正文、游戏动作及记忆底层是显式 fixture；不是完整 LLM/真实 Bannerlord 验收。

Scene 与 Courier 离线证据包括真实源码群组/运输/退休 owner、独立外部消费者、显式枚举重排和兼容构建；详见[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md)。Courier 消费者执行真实准入/到达/提交接受/信件确认/运输收尾方法，但底层游戏资产、provider 和最低层 writer 为 fixture。三渠道均不增加每帧全请求扫描，性能仍须实测。J14c 已通过最终四实现/旧 ABI/真实记忆回读/当前 DLL 回放及代码地图两模式。旧 Native 消费者先对基线 V1 编译，再以同一消费者二进制运行当前源码链接宿主；四游戏实现另验元数据，不把它们等同于游戏加载。独立子 MOD 实机加载、两版本 LIVE、真实旧 SAVE、provider、音频和帧性能分别 NOT-RUN，不是发布 READY。
