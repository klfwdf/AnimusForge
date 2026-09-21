# AF 制作组内部模块接缝矩阵（J11）

> 产品提交：`cdbd077a`；一基行号仅作当前导航，后续按符号定位。
> 这里描述同一 `AnimusForge.dll` 内部的 Policy / Gathering / Siege typed 接缝，不是独立子 MOD API，也不表示玩法迁入 AF 主体。

## 1. 分层和所有权

| 层 | 当前位置 | 责任 | 禁止承担 |
| --- | --- | --- | --- |
| internal contracts | `src/AF.Contracts/Internal/TeamModules/*.cs` | 13 个现有语义方法的 internal 签名 | 具体玩法、状态、I/O、注册或 public DTO |
| thin adapters | `src/bridges/{Policy,Gathering,Siege}/*.cs` | 参数、默认值、ref/out、返回与异常一一转发 | Prompt 拼装、标签解析、事实提交、通知、retry、缓存 |
| composition | `src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs` | 每个 port 一个无状态实例，冷装配 | 每次调用字典查询、反射扫描、热替换 |
| gameplay owners | `PolicySystem/**`、`NobleGatheringBehavior.cs`、`AfGcczShoutBridge.cs` 及 GCCZ 领域目录 | 资格、规则、数值、状态、存档、执行和领域失败语义 | 反向依赖 AF public API 或复制主体管线 |

## 2. 方法、频率和副作用 owner

| Port | 方法 | 调用点 | 分类 / 运行频率 | 权威副作用与门禁 |
| --- | --- | ---: | --- | --- |
| Policy | `IsEligibleTargetForExternal` | 2 | 每次被选中的 postprocess prepare 查询 | Policy owner；只读资格 |
| Policy | `BuildRuntimePostprocessRulesForExternal` | 2 | 每次被选中的 postprocess rule capture | Policy owner；不执行动作 |
| Policy | `TryProcessAcceptedAgendaTag` | 3 | Native/Courier 到达后的 action commit | Policy owner；失败原因、状态变化和日志归原实现 |
| Policy | `BuildActivePolicyDialogueContextForExternal` | 1 | Prompt context capture | `NpcRulerPolicyBehavior`；当前兼容空结果保持 |
| Gathering | `BuildRuntimePostprocessRulesForExternal` | 2 | postprocess rule capture | Gathering owner；只构建规则 |
| Gathering | `BuildPostprocessContextForExternal` | 2 | postprocess context capture | Gathering owner；不提交事实 |
| Gathering | `NormalizeNobleGatheringPostprocessTagsForExternal` | 2 | LLM 后处理结果 normalize | Gathering owner；normalize 不执行 |
| Gathering | `BuildFeastAttendanceContext` | 1 | Prompt extras capture | Gathering owner；资格/文本归原实现 |
| Gathering | `TryApplyNobleGatheringTagsForExternal` | 5 | Native/Scene/Courier action commit | 原 owner 返回 facts/notifications；调用方唯一提交/展示 |
| Siege | `BuildPostprocessRules` | 3 | active Scene / Native rule capture | `AfGcczShoutBridge` 先检查 `conversation-siege` 与 active stage |
| Siege | `BuildPostprocessContext` | 2 | active Scene context capture | 同上；无 active/selected 返回空 |
| Siege | `NormalizePostprocessTags` | 3 | active Scene normalize | 同上；normalize 不执行 |
| Siege | `TryProcessActionTags` | 3 | Native/Scene action commit | 同上；具体村庄/城镇 GCCZ owner 执行 |

合计 **13 个方法 / 31 个生产调用点**。adapter 不新增线程切换；调用线程、Hero/Character/Agent 捕获和提交顺序仍由各真实渠道 owner 负责。

## 3. 当前生产调用点

### Policy（8）

- `MyBehavior.cs:30690` — Prompt active-policy context。
- `ShoutBehavior.cs:18614` — Native accepted agenda apply。
- `ShoutBehavior.cs:22177,22316` — Native/共享 postprocess eligibility + rules。
- `Channels/Courier/CourierDeliveryBehavior.DeliveryLifetime.cs:53` — 既有 delivery lifetime commit。
- `Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs:61` — request-bound Courier domain commit。
- `Channels/Scene/ShoutBehavior.ScenePostprocess.cs:673,821` — Scene eligibility + rules。

### Gathering（12）

- `MyBehavior.cs:30647` — Prompt feast attendance context。
- `ShoutBehavior.cs:10493,18718,26708` — Native/旧 Scene action commit callers。
- `ShoutBehavior.cs:22324,22563,22627` — 共享 postprocess rules/context/normalize。
- `Channels/Courier/CourierDeliveryBehavior.DeliveryLifetime.cs:82`、`DomainCommit.cs:90` — Courier 两个实际到达责任点。
- `Channels/Scene/ShoutBehavior.ScenePostprocess.cs:829,1094,1182` — Scene rules/context/normalize。

### Siege（11）

- `ShoutBehavior.cs:18685` — Native action commit。
- `ShoutBehavior.cs:22328,22571,22630` — 共享 postprocess rules/context/normalize。
- `ShoutBehavior.cs:25486,25528` — Native Siege rule/normalize helper。
- `Channels/Scene/ShoutBehavior.SceneConversationChains.cs:1988` — active Scene reaction commit。
- `Channels/Scene/ShoutBehavior.ScenePostprocess.cs:580,846,1102,1186` — Scene action/rules/context/normalize。

领域内部直接调用自己的 owner 不属于跨域绕过；AF 主体跨域入口则必须经 `TeamModuleServices`。当前主体检索未发现上述 13 个 owner 方法的直接绕过。

## 4. 生命周期、失败和稳定性

- `TeamModuleServices` 静态初始化三个无状态 adapter；`TeamModuleRegistration` 只把实际绑定能力登记入 InternalModuleDirectory。
- Directory `Ready` 只表示 adapter 已装配，不替代 Campaign、目标、模块开关或业务资格。
- Policy/Gathering 异常保持原样越过 adapter；Siege disabled/inactive 由 `AfGcczShoutBridge` fail closed。
- facts/notifications、AFEF/history 和 UI 通知仍由原渠道/领域 owner 唯一提交；桥层没有第二提交点。
- 无新增 SyncData key、Saveable type、程序集或 public API；独立子 MOD 仍只通过版本化 public 层。
- 热路径为静态实例直接接口调用；不扫描程序集/目录、不反射、不轮询、不做文件或网络 I/O。

## 5. 可执行证据

- `TeamModulePortParityTests`：13 方法、31 call expressions；308 行为断言；资格反转、Siege selected 反转、player/speaker text 交换三个变异均被拒绝。
- `CampaignCompositionTests`：42 正常断言和 5 个行为变异；实际 `ModuleFrameworkRuntime → CampaignComposition`。
- `PolicyEffectModule.ContractTests`：all-modules 1406；history 1115。
- Bridge bindings 16（12 wired / 4 declared-only）、23 单测；runtime isolation 12；CompositionMatrix 18/24。
- 上述均为离线证据；真实 Campaign/Mission、旧 SAVE、GCCZ 场景、制作组玩法结果仍需 LIVE/SAVE 验收。
