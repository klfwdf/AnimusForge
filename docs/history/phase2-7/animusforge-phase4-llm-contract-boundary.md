# AF Contracts 第一版生产边界

- 状态：ACTIVE（生产契约，尚未接入旧 Behavior）
- 文件：`Refactor/Contracts/InteractionContracts.cs`、`Refactor/Contracts/LlmContracts.cs`
- 程序集：仍为现有 `AnimusForge.dll`

## 设计目的

这一切片只建立跨渠道、LLM Gateway、Memory 和 Action 的稳定输入输出类型，不搬迁业务实现。类型位于 `AnimusForge.Refactor.Contracts`，由同一程序集内的新旧实现共同使用。

## 约束

- DTO 只允许字符串、数值、枚举、不可变集合和 detached ID。
- 不引用 `Hero`、`Agent`、`Mission`、`MobileParty`、`Campaign` 或任何其他 live TaleWorlds 类型。
- `LlmProviderSnapshot` 不携带 API key；凭据由 provider implementation 的安全配置边界解析。
- 后台 `ILlmGateway` 只接收 `LlmGenerateRequest`，返回 `LlmGenerateResult`；结果必须带 `TraceContext` 对应的 generation，由主线程重新解析目标并调用 `IActionPlanExecutor`。
- `InteractionResult.ConfirmedFacts` 只允许已确认的事实；LLM 文本和未执行标签不能直接成为 AFEF 成功事实。
- 集合在构造时复制并暴露为只读视图；配置 reload 不会改变已经创建的 request/prompt snapshot。

## 当前非目标

- 不替换 `MyBehavior`、`ShoutBehavior`、`CourierDeliveryBehavior`。
- 不改变 `SubModule.cs` 注册顺序、旧存档 key/type、Prompt JSON、Action tag 或 Bootstrap。
- 不实现 HTTP provider、动作执行器、Memory 存储器或三渠道 adapter。
- 不声明运行时热卸载；后续接入仍按 boot/save-load lifecycle 处理。

## 后续接入顺序

1. 为现有 `ShoutNetwork` 建立 adapter，保留重试、stream、stale generation 和 API 兼容行为。
2. 为 `MyBehavior` 建立 Memory/AFEF facade，先只读旧 key/type。
3. 让场景喊话、Native/free conversation、Courier 生成相同的 `GameInteractionSnapshot` 和 `PromptPackage`。
4. 逐个接入 postprocess/action handler，主线程二次验证后才写入事实。

`LegacyShoutNetworkGateway` 是第一版过渡 adapter，目前不改变任何调用点。它只把 contract 消息转换为旧网络层的 detached dictionary；endpoint、model、API key 仍由 `DuelSettings` 旧配置实际解析，直到 ConfigSnapshot 切片接管。

`InteractionPipeline` 是共享顺序的第一版实现：它完成资格判断、Prompt、LLM、可见文本和 ActionPlan 生成，然后停止。它不执行游戏动作、不写入 Memory/AFEF；各渠道必须在主线程重新解析目标、验证权限、执行一次并在成功后写入事实。
