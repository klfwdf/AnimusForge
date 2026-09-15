# 独立子 MOD 公共 API V1

## 当前交付边界

**目录查询与 Native 提交/结果/开始前取消已接线；Scene / Courier 仍未开放。三渠道全部开放仍是整体收尾必交，不因 Native 完成而缩小任务。**

| 能力 | 状态 | 实际含义 |
|---|---|---|
| `CatalogRead` | Available | 任意线程查询框架装配、只读模块接缝目录 |
| `NativeSubmit` | Available | 对当前 AF 原生自由对话提交玩家文本，复用实际准入、正文、标签、动作、记忆 owner |
| `SceneSubmit` | NotSupported | 群组启动目前 fire-and-forget；尚缺完整接力/旁听/最终结果绑定 |
| `CourierSubmit` | NotSupported | 尚缺运输会话、预生成与到达提交的公共安全边界 |
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

同一 client + 同一 request ID + 完全相同文本再次提交，复用同一个内部 operation，绝不再发 LLM 或执行动作。相同 ID 的不同文本返回 `dialogue.request_id_conflict`，不覆盖已有任务。ID 区分大小写，不 trim；长度 1–128，仅允许英文字母、数字、`.` / `_` / `-` / `:`。文本不能为空，最多 16000 UTF-16 字符，实际规范化仍由原 Native owner 完成。

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
| `Completed` | 原唯一动作/必要记忆收尾产生了真实完成回执；不是按返回字符串推测 |
| `Rejected` | 未被原 Native owner 准入；包括 busy、失效、不可用、非法输入 |
| `Cancelled` | 取消赢在开始之前，没有本 operation 的游戏副作用 |
| `Failed` + `UnknownAfterStart` | 原 owner 已准入但没有完整回执；前置动作/部分记忆可能已经发生，不自动重试 |
| `Cancel()` | 返回 `CancelledBeforeStart` / `TooLate` / `AlreadyTerminal`；后两者不会改写真实结果 |

- `GetSnapshot()` 返回不可变副本；`Completion` 返回终态 DTO。后台结果/回调不直接操作游戏对象。
- `CompletedByOwner` 不代表 TTS 播放结束，也不表示每一个生成标签都必然被游戏资格规则接受；资格/效果仍由原 owner 处理。
- 原方法返回空字符串、网络错误文案或换档错误文案都不能产生成功回执。异常不向公共 DTO 暴露原始消息、路径、Prompt 或凭据。
- V1 从不承诺正在执行的 HTTP 可被强制取消。已开始后的取消/Dispose 不抢占最终回执。

## Native 上下文与线程边界

- 不通过此入口指定任意 Hero、不创建第二场对话、不替子 MOD 弹自有 UI。必须已有当前 AF 可提交的 Native 对话。
- 任意线程可调用。请求在调用时绑定当前 owner、存档 generation 、真实 ConversationEnded epoch 和 presentation revision；排队期间换档、owner 替换或 A 对话结束改为 B 对话，旧请求不能进入 B；排队期间原UI先完成了另一回合，旧API请求也不会再补做。
- Hero / Character / Mission / token 在原主线程准入时捕获，随后全程使用原身份守卫。API 不声称后台提交瞬间已读取/冻结游戏 Hero；同一未结束会话在准入前改变目标时，由原准入时目标决定。准入后目标变化会拒绝晚结果。
- 与原 UI 共用后端 busy/admission。API 不能绕过正在运行的玩家对话；对现有 UI 的默认入口、开关和表现不做切换。
- 内部枚举经显式 V1 投影，不把内部 enum 数值布局变成公开协议。

## 引用、兼容与目录查询

继续只依赖 `AnimusForge.Api.V1`。引用匹配游戏线的 `AnimusForge.dll`（`Private=false`），由 AF Bootstrap 选择唯一 1.3/1.4 实现；子 MOD 发行目录不能再携带另一份 AF DLL。

既有 `AfApi.GetSnapshot()` / `GetCapability()`、只读目录 DTO、枚举数值及版本 1 保持。新增方法/DTO 为兼容性扩展；今后不兼容修改另开版本。能力 ID 校验仍精确区分大小写：空/空白/超过128字符为 InvalidRequest，非1版本 VersionMismatch，未知ID UnknownCapability。

目录中的制作组 `IsExternallyCallable` 仍为 false：Native API 不是把制作组内部 port 公开。模块加载前/卸载后查询不初始化 AF、不发 LLM。同进程 DLL 不是安全沙箱；此层不承诺防御反射型恶意 MOD。

## 证据与仍未完成

`tools/NativeModuleSubmissionTests/README.md` 记录真实入口、物理主/后台线程、重复、取消、会话切换与失败回执测试。Provider正文、游戏动作及记忆底层是显式 fixture；不是完整 LLM/真实 Bannerlord 验收。

仍需完成 Scene 全群组最终回执、Courier 完整运输/到达提交公共边界、当前候选独立子 MOD 实机加载与 LIVE/SAVE；三渠道剩余工作不计 DONE。
