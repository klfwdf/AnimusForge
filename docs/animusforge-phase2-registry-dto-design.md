# 阶段 2：Host/Composition Registry DTO 与 Contribution Group 设计

- 状态：设计完成，未接入运行时
- 日期：2026-08-29
- owner：Host/Composition
- 范围：为 `SubModule.cs` 的生命周期、Harmony、Model、CampaignBehavior、Mission adapter、ApplicationTick、EngineTick 建立只读元数据契约
- 非目标：不修改 `SubModule.cs`，不替换现有注册调用，不移动生产 C#，不创建物理 DLL，不改变运行行为

## 1. 设计目的

当前 `SubModule.cs` 直接承担多个注册面和多个 Tick 面。第一步不是把这些调用搬进“大一统注册器”，而是先定义一套只描述“谁、在哪个阶段、以什么顺序、由谁负责、失败如何处理”的 metadata DTO。

该 DTO 的用途是：

- 让 Host/Composition 的注册清单可被机器校验和诊断读取；
- 固化现有顺序、失败隔离、线程边界和 API 线影响；
- 为将来逐组抽取提供稳定的 strangler seam；
- 让模块 owner、Bridge 和验证工具共享同一套描述，不暴露私有运行时对象。

该 DTO **不负责**：

- 创建或持有 `CampaignBehavior`、`MissionBehavior`、Harmony、UI、Agent、Hero、Mission、Game 或其他 TaleWorlds 对象；
- 保存 delegate、反射 `MethodInfo`、Harmony target、DI 容器或可执行闭包；
- 保存 raw dictionary、SyncData 实例、用户数据或 Prompt 文本；
- 在每帧重新扫描程序集或反射发现贡献项；
- 取代现有 `SubModule` facade。

## 2. 核心 DTO

以下是逻辑设计，不是当前要新增的生产 C# 类型。

### 2.1 `HostContributionDescriptor`

描述一个可识别的注册/调度贡献项，只包含稳定元数据：

| 字段 | 类型方向 | 约束 |
|---|---|---|
| `ContributionId` | 非空稳定字符串 | 全局唯一；不使用类型全名自动生成；一旦写入验证/日志需保持稳定 |
| `GroupId` | 非空稳定字符串 | 必须指向一个已声明的 contribution group |
| `Owner` | 非空稳定字符串/受限枚举 | 逻辑 owner，如 `Host.Composition`、`Conversation`、`Courier`、`Policy` |
| `Stage` | 受限枚举 | `Lifecycle`、`Harmony`、`Model`、`CampaignBehavior`、`MissionAdapter`、`ApplicationTick`、`EngineTick` |
| `LegacyOrder` | 非负整数 | 对应当前 `SubModule.cs` 的实际顺序；不是重新排序授权 |
| `EnabledProfile` | 稳定 profile 名称列表 | 只描述资格，不持有运行时配置对象；默认 profile 不得隐式改变 |
| `ApiLines` | 1.3/1.4 标志 | 必须明确支持面；不以编译常量推断运行时版本 |
| `ThreadBoundary` | 受限枚举 | `MainThreadOnly`、`BackgroundSnapshotOnly`、`MainThreadApply`、`NoGameAccess` |
| `FailurePolicy` | 受限枚举 | `ContinueSibling`、`FailGroup`、`FailComposition`、`Degraded` |
| `SaveImpact` | 受限枚举 | `None`、`ReadsExisting`、`WritesExisting`、`LegacyCompatibilityRequired` |
| `ChannelImpact` | 标志集合 | `SceneShout`、`NativeConversation`、`Courier`、`None`；不能用来自动启用渠道 |
| `Requires` | 稳定 ID 列表 | 只声明元数据依赖；不构造依赖，不形成隐式执行顺序 |
| `ContractVersion` | 整数/语义版本 | 与 DTO/组契约版本分开；不表示程序集版本 |
| `Notes` | 短文本 | 只记录边界和证据索引，不放 Prompt、密钥、存档数据或大段日志 |

### 2.2 `HostContributionGroupDescriptor`

描述一组同类贡献项：

| 字段 | 类型方向 | 约束 |
|---|---|---|
| `GroupId` | 非空稳定字符串 | 全局唯一 |
| `Stage` | 受限枚举 | 一个组只属于一个调度面；ApplicationTick 与 EngineTick 必须分开 |
| `Owner` | 非空稳定字符串/受限枚举 | 组 owner 不覆盖具体贡献项 owner |
| `GroupOrder` | 非负整数 | 只记录当前组在 Host 中的相对位置 |
| `FailurePolicy` | 受限枚举 | 组级失败策略；不能吞掉贡献项的结构化失败 |
| `ThreadBoundary` | 受限枚举 | 组级最低线程要求 |
| `Contributions` | 只读 descriptor 列表 | 按 `LegacyOrder` 排序；不包含可执行对象 |
| `ContractVersion` | 整数/语义版本 | 用于验证 catalog 结构，不等于游戏或程序集版本 |

### 2.3 `HostRegistrySnapshot`

设计期和诊断期使用的顶层快照：

| 字段 | 类型方向 | 约束 |
|---|---|---|
| `SchemaVersion` | 正整数 | 变更字段语义时递增 |
| `Source` | 稳定字符串 | 例如 `SubModule.current`；不写本机绝对路径 |
| `ApiLines` | 标志集合 | 快照声明适用 API 线 |
| `Groups` | 只读组列表 | 不可在运行时 Tick 中重建 |
| `ValidationState` | `Valid`/`Invalid`/`Degraded` | 仅描述 catalog 校验结果 |
| `ValidationIssues` | 有界只读问题列表 | 记录 ID、顺序、依赖、线程、owner 等问题；限制数量和长度 |

快照不能包含 `Behavior` 实例、`Mission`、`Game`、`Hero`、`Agent`、`IDataStore`、API key、Prompt、原始对话、PlayerExports 或任何用户数据。

## 3. Contribution Group 划分

不要建立一个同时负责所有注册面的单一 registry。第一版分成以下独立组：

| GroupId | Stage | 当前来源 | 责任边界 |
|---|---|---|---|
| `host.lifecycle` | Lifecycle | `SubModule` 生命周期覆写 | 启动/关闭/配置变更/主屏幕初始化的顺序和失败隔离 |
| `host.harmony` | Harmony | `SubModule.OnBeforeInitialModuleScreenSetAsRoot` | Harmony patch 分组、顺序和异常记录；不做无序扫描 |
| `host.models` | Model | `InitializeGameStarter` 前 4 个 helper | 原版 inner model 查找、wrapper 去重和 fallback |
| `host.campaign-behaviors` | CampaignBehavior | `InitializeGameStarter` 的 `AddBehavior` 序列 | 行为实例化顺序；不改变事件订阅顺序 |
| `host.mission-adapters` | MissionAdapter | `OnBeforeMissionBehaviorInitialize` / `OnMissionBehaviorInitialize` | SceneActions/BattleSpeech/AF mission adapter 的添加和验证 |
| `host.application-tick` | ApplicationTick | `RunFastApplicationTickPhases` / watched phases | UI deferred close、主线程 action、TTS、诊断等应用帧工作 |
| `host.engine-tick` | EngineTick | `RunFastApplicationTickPhases` / watched phases | Campaign/行为 engine tick；不与 ApplicationTick 合并 |

### 分组不变量

1. `GroupId + Stage` 唯一；不得把同一贡献项同时声明在 ApplicationTick 和 EngineTick。
2. 每个 `ContributionId` 全局唯一，`LegacyOrder` 在同一 Stage 内稳定且无冲突。
3. `host.campaign-behaviors` 的顺序必须与当前 `AddBehavior` 顺序一致，至少在实际代码切片接入前保持 1:1。
4. `host.harmony` 的失败策略必须保留当前每组 `try/catch` 的隔离语义。
5. `host.mission-adapters` 不拥有 SceneActions/BattleSpeech 的可复用业务逻辑；AF 侧仍是薄适配器。
6. `host.application-tick` 和 `host.engine-tick` 的顺序、预算和线程边界分别验证。
7. 组之间的 `Requires` 只能作为验证信息；若未来需要执行依赖，必须显式定义，不得由字符串排序推断。

## 4. 失败与健康状态

Registry DTO 只描述失败策略和诊断状态，不直接执行恢复：

| 状态 | 含义 | 当前行为 |
|---|---|---|
| `Declared` | 元数据已登记但未验证 | 只用于设计/审计阶段 |
| `Validated` | ID、owner、stage、顺序、依赖和线程约束通过 | 不改变现有直接调用 |
| `Degraded` | 可选贡献项或外部 provider 缺失 | 保留明确降级原因；不吞掉日志 |
| `Blocked` | 必需依赖/契约不满足 | 未来组合层可阻断该贡献项；本设计阶段不接管运行时 |
| `Failed` | 实际注册/启动失败 | 由现有 Host 失败隔离和日志负责；DTO 只记录结果摘要 |

不允许把 `Degraded` 或 `Failed` 伪装成 `Validated`，也不允许将“没有异常”推断为“完整贡献成功”。

## 5. 依赖和顺序校验

设计期 validator 至少检查：

- `ContributionId`、`GroupId` 唯一且非空；
- 所有 contribution 都属于声明的 group；
- `Stage` 与 group 的 stage 一致；
- owner 非空，且不把 Bridge 伪装成单一模块 owner；
- 同一 group 内 `LegacyOrder` 不冲突；
- `Requires` 指向已知 ID，且不存在循环；
- MainThread/BackgroundSnapshotOnly 与 Stage 组合合法；
- `SaveImpact`/`ChannelImpact`/`ApiLines` 有明确值；
- registry schema/contract version 可识别；
- 不存在 Behavior 实例、TaleWorlds 对象、delegate、反射句柄、raw dictionary 或用户数据字段。

校验失败时生成有界的 `ValidationIssue`：

```text
Code
Severity
ContributionId / GroupId
Message
SuggestedOwner
```

validator 不在热路径运行。未来若接入，应该在模块启动或显式诊断命令中构建一次并缓存快照；Tick 只调用现有的直接注册/调度 facade。

## 6. 与旧 `SubModule` facade 的接入策略

第一条实际代码切片必须采用“旁路登记、旧入口不动”：

1. 先以静态 catalog 或启动期 builder 生成 DTO；
2. 对 DTO 做一次结构校验；
3. 保留 `SubModule` 现有 Harmony、Model、Behavior、Mission、Tick 直接调用；
4. 只把 catalog 用于日志、诊断和测试，不能由 catalog 自动反射执行；
5. 经过 1.3、1.4、Bootstrap、旧存档、三渠道和 failure-isolation 验证后，才考虑逐组把一个现有调用改为 facade 转发；
6. 每次只替换一个 group，并保留旧方法作为 rollback facade。

禁止的快捷路径：

- 用反射扫描程序集自动发现所有注册项；
- 把所有 Patch、Behavior、Model 和 Tick 合成一个 `RegisterEverything()`；
- 用 DTO 列表顺序取代 `SubModule.cs` 当前真实顺序但不做对比校验；
- 让 registry 持有对象实例或执行 delegate；
- 让 registry 直接写存档、执行动作或修改游戏状态。

## 7. 性能、线程和兼容性

- 本设计当前运行频率为 0，因为尚未接入运行时。
- 若未来启用：catalog 在启动/显式诊断时构建一次；快照只读缓存，Tick 不重建、不全量反射、不扫描文件系统。
- `ShoutNetwork` 等后台网络工作继续只接收不可变消息/字符串快照；registry 不把 live TaleWorlds 对象传入后台。
- Game state mutation、Harmony 注册、CampaignBehavior/Model/Mission 注册和 UI 操作仍在主线程。
- `ApiLines` 必须同时标注 1.3/1.4 影响；不使用 compile symbol 代替 reference provenance。
- 保存影响只做元数据标注，不能改变现有程序集身份、序列化类型、SyncData key 或存档 schema。

## 8. 测试与验收计划

设计阶段可先做纯 validator 测试，不需要启动 Bannerlord：

1. 有效 catalog：所有组、贡献项、owner、顺序和 stage 合法；
2. duplicate ID / duplicate legacy order；
3. unknown group / unknown dependency；
4. dependency cycle；
5. ApplicationTick 与 EngineTick 错分；
6. 后台线程标记包含 live game object；
7. SaveImpact/ChannelImpact/API line 缺失；
8. Behavior 实例、delegate、raw dictionary 等禁止字段；
9. required contribution `Blocked` 与 optional contribution `Degraded` 的诊断结果；
10. catalog snapshot 有界问题列表和稳定序列化。

产品级接入后另需：

- 1.3 / 1.4 / Bootstrap 构建；
- 旧存档和 `SyncData` 兼容；
- Harmony patch failure-isolation；
- CampaignBehavior/Model/Mission 注册顺序；
- ApplicationTick/EngineTick 顺序、预算和队列上限；
- Scene shout、Native Conversation、Courier 三渠道；
- SafeMode、缺依赖和 bridge 故障矩阵。

## 9. 本切片状态

- 设计文档已完成；
- 当前运行时 registry：不存在；
- `SubModule.cs`：未修改；
- 生产 C#：未修改；
- 构建/覆盖/打包/推送脚本：未修改；
- `.gitignore`、`SubModule.xml`、程序集身份、SyncData key、存档类型：未修改；
- 游戏目录：未部署；
- 运行频率：0；
- 回滚：删除本设计文档并恢复台账/handoff 文档即可，无生产代码回滚需求。

## 10. 下一项准确任务

> 仅做纯 validator 的输入/输出样例设计，或建立静态 catalog 草案；不接入 `SubModule.cs`，不让 registry 参与运行时执行。
