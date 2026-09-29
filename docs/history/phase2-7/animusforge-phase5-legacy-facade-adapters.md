# 阶段 5：旧入口 detached facade 适配器

## 本轮范围

`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs` 建立三条旧入口到公共契约的首轮边界：

- `CaptureNativeConversation`：调用 `ShoutBehavior` 的 Native 目标 facade，并从 `MyBehavior` 读取既有内存角色消息；
- `CaptureSceneShout`：在交互输入边界按 `Agent.Index` 捕获场景目标，复制稳定 ID、名称和存活状态；
- `CaptureCourier`：接收现有信使流程已经确定的收件人、信件和送达事实，读取既有记忆，并只查询现有 Courier 资格状态；
- `MyBehaviorMemoryFacade`：将公共 `IInteractionMemory` 的读写映射到原有 Hero/non-Hero 历史和 AFEF 入口。
- `CreateNativeConversationFacade`：提供生产侧 Native capture wiring，但仍要求渠道 owner 显式注入 ports 和 gateway。

## 安全边界

1. 这些方法必须由游戏主线程在交互边界调用。Hero、Agent、Campaign 等 live 对象只在捕获/提交 facade 内使用，不能放进 `GameInteractionSnapshot`、`InteractionEnvelope` 或后台任务闭包。
2. 捕获完成后只向后台传递字符串、ID、整数、布尔值和不可变列表/字典。
3. 本轮不替换 `ShoutBehavior`、`MyBehavior`、`CourierDeliveryBehavior` 的生产调用点，不改变现有 Prompt、标签解析、动作执行、AFEF、SyncData、信使时序或 `DuelSettings` 配置 authority。
4. `CaptureSceneShout` 只在用户交互时扫描 `Mission.Current.Agents` 中的目标索引，不放入 `OnMissionTick` 或其他热路径；后续若需要全量候选，必须从已有输入边界快照复用，而不是每帧扫描。
5. 当前 `LegacyShoutNetworkGateway` 仍由旧 `DuelSettings` 提供实际 endpoint/model 配置；本适配器不会宣称 `RuntimeConfigSnapshot` 已接管运行时配置。

## 尚未完成

- 尚未把三条旧入口切换到 `InteractionPipeline`；
- 尚未把现有真实规则选择器、Prompt composer、标签 parser 和主线程 Action executor 接到这些契约；
- 尚未进行旧存档、真实网络、游戏内三渠道或全领域验收。
