# 阶段 2：影响面、候选 Bridge 与回滚入口地图

- 状态：只读设计完成；未移动生产源码；未接入运行时
- 日期：2026-08-30
- owner：Host/Composition（与各目标域 owner 共同维护）
- 依据：`docs/animusforge-owner-matrix.md`、`docs/animusforge-phase2-root-llm-owner-slice.md`、`docs/animusforge-phase2-submodule-registration-catalog.md`、`docs/animusforge-phase2-registry-dto-design.md`
- 目的：在任何生产 C# 抽取之前，固定每个切片的影响面、跨模块协作边界、非目标和可逆入口

> 本文是阶段 2 的设计地图，不是迁移授权。当前 `SubModule.cs`、旧 facade、注册顺序、存档类型、SyncData key、三渠道链路和单一模块发布结构保持不变。

## 1. 不可变总约束

| 约束 | 本阶段结论 | 验收证据方向 |
|---|---|---|
| 物理模块 | 继续一个 `Modules/AnimusForge`，不拆多个玩法 DLL | 1.3/1.4/Bootstrap stage 输出仍为单一发布结构 |
| 程序集身份 | 保持 `AnimusForge` 与 `AnimusForge.Bootstrap` 现有身份 | 程序集名、Bootstrap 选择日志、SubModule.xml 未改变 |
| 存档 | 现有序列化类型和 `SyncData` key 不变；迁移只能另行设计并提供证据 | key/type catalog、旧存档加载/迁移测试 |
| API 线 | 同一源码保持 `BannerlordApi=1.3` 和 `BannerlordApi=1.4`；优先现有兼容 helper | 两套实现构建、API diff 复核 |
| 交互渠道 | 信使、自由对话、场景喊话共享既有话题/规则/历史/标签执行契约 | 三渠道 contract/回归矩阵 |
| 线程 | Game/Mission/Agent/Hero/UI/存档写入均在主线程边界；后台只接收不可变 snapshot | dispatch、队列上限、stale generation、主线程日志 |
| Host | `SubModule.cs` 仍是 facade/组合根；本阶段不替换直接注册调用 | `SubModule.cs` 无改动，注册顺序清单与真实代码一致 |
| 性能 | registry/validator 频率为 0；不在 Tick 中反射、扫描或重建 catalog | perf probe、源码检查、tick 预算记录 |

## 2. 影响面标注维度

每个目标模块在后续切片中必须显式填写以下字段，不能只写“无影响”：

1. **Save**：读取/写入哪些既有状态；是否触及 `SyncData` key、序列化类型、generation、chunk；默认要求 `No identity change`。
2. **Prompt/Rule/Tag**：是否改变前处理、主链路、后处理 `{tag_rules}`、标签解析、动作授权、AFEF 事实或三渠道历史。
3. **Harmony**：是否新增、移动、排序或撤销 patch；目标方法、失败隔离和原版 fallback 是什么。
4. **Tick**：属于 ApplicationTick、EngineTick、hourly、daily、mission tick 还是后台完成队列；频率、预算、缓存、队列上限必须明确。
5. **UI**：是否涉及真实 `RootWidget`、datasource、输入焦点、Backspace/翻页快捷键、延迟关闭或主线程。
6. **Thread**：后台快照输入、取消、stale generation、主线程 apply 和异常边界。
7. **API**：1.3/1.4 是否有签名差异；能否使用已有 compatibility helper；不可用条件和 fallback。
8. **User data/content**：是否读取 `PlayerExports`、Prompt/Rule JSON、ONNX、语言资源或用户配置；不得把用户数据误当 runtime source。

## 3. 目标 owner 影响矩阵

| 目标 owner | 主要当前入口/文件 | Save | Prompt/Rule/Tag | Harmony | Tick/线程 | UI/API/内容风险 | 最小回滚入口 |
|---|---|---|---|---|---|---|---|
| Host/Composition | `SubModule.cs`、`AnimusForge.csproj` | 只读现有行为；不改 key/type | 不拥有玩法 prompt/tag | 维持现有逐组顺序和独立 catch | `OnApplicationTick` 与 `OnEngineTick` 分开；主线程 | 组合根受 1.3/1.4 API 影响；不扫描全量程序集 | 保留 `SubModule.cs` 直接注册路径；撤销新 metadata 读取 |
| Conversation/AI | `MyBehavior.cs`、`ShoutBehavior.cs`、`AIConfigHandler.cs`、`ShoutNetwork.cs`、`LlmApiCompat.cs`、`LlmVisibleReplyNormalizer.cs` | `MyBehavior` 既有历史/记忆/AFEF 和 save contract | 三段式前处理→主链路→后处理；标签必须进入 `{tag_rules}` | 只保留已有入口 patch；失败回到 native/原有回复 | 网络后台只用不可变快照；回复/动作/历史写回主线程；队列有界 | Native conversation 输入焦点；LLM/API 版本差异；不记录 key/原始敏感文本 | 旧 `MyBehavior`/`ShoutBehavior` facade；关闭 AF 时回原版对话 |
| Memory/Persistence | `MyBehavior.cs`、`SaveRuntimeGuard.cs`、AFEF/记忆相关实现 | key/type/generation/chunk 不变；迁移单独登记 | 事实写入必须用 AFEF；历史角色语义保持 user/assistant | 不以 patch 绕过存档边界 | stale completion 必须拒绝；写入主线程/存档边界 | 旧存档兼容和保存大小；不读取 PlayerExports 作为 save state | 保留原 facade 和既有 SyncData 读写；新 facade 可旁路 |
| Courier | `CourierDeliveryBehavior.cs`、Courier model 注册 | 读取既有 courier 状态；不新建 save identity | 复用三渠道 topic/rule/history/tag contract；不另造请求体 | 维持 `RegisterHarmonyPatches` 顺序 | delivery 结果和 world state 主线程；LLM 回复后台快照化 | Courier 与 native conversation 入口目标一致；1.3/1.4 helper | 只关闭新 adapter，保留原 `CourierDeliveryBehavior` 注册 |
| SceneActions | `extensions/AnimusForge.XihaiAction/src/**`、`SceneActionsIntegrationBoundary` | 默认 None；如有事实写入走公开 contract | 场景喊话动作必须使用统一后处理标签和授权 | adapter patch 独立失败；不能让 Host 承担业务规则 | Agent/Mission 只在主线程；后台不得携带 live 对象 | Agent/AgentIndex/LocationCharacter 目标边界；场景 UI/任务生命周期 | 关闭 SceneActions adapter，保留原版 scene behavior；Host 继续启动 |
| World Simulation | `WorldDiplomacyBehavior.cs`、world action/party 相关文件 | 既有 kingdom/party/settlement state；不改存档身份 | 只消费 Action/Fact contract，不直接拥有 prompt 文本 | world patch 失败时回原版 action | hourly/daily/campaign tick 按现状；长任务分批/缓存 | 领地、外交、军团目标和 1.3/1.4 API 差异 | 关闭新 world adapter；回到旧 behavior/原版 action |
| Settlement/Siege | `SiegeAftermathPatchBootstrap`、Settlement/Siege 相关行为 | siege/settlement 既有字段不变 | 通过 Bridge 接收已授权 action；不在 siege owner 内拼 LLM prompt | patch 分组独立；攻城/出击/野战排除条件保留 | campaign/mission boundary 明确；场景对象主线程 | 伤害上下文、攻城/竞技场/训练场防误触；兼容 diff 必读 | 移除新 aftermath adapter；保留原版 siege/settlement fallback |
| Policy/Diplomacy | `PolicySystem/**`、`NpcPolicyContracts.cs`、`VoteDealBehavior*.cs` | policy history/save namespace 只读兼容 | Policy prompt/rule 由 Policy owner 管；跨域通过 typed contract | patch 失败不得阻断原版投票/外交 | proposal/effect apply 主线程；LLM 结果 stale 时丢弃 | 1.3/1.4 policy API、MCM 配置、UI 与 runtime 分离 | 关闭 bridge/effect module；保留原 policy behavior |
| Mission/Combat | `DuelBehavior.cs`、`MilitaryExerciseBehavior.cs`、`MeetingBattleLockMissionBehavior.cs`、相关 mission patch | mission session state 默认 None；事实确认后再写 | 场景动作标签只触发已授权 command；不把坐标当 Agent 主目标 | Mission patch 独立隔离；普通战斗 fallback 原版 | Agent/Team/Mission 全部主线程；无后台 live 引用 | 和平伤害 allowlist、攻城/竞技场/训练场/藏身点排除 | 关闭 AF 场景伤害/动作适配器，退出模组处理回归原版 |
| Progression/Social | `RewardSystemBehavior*.cs`、`RomanceSystemBehavior.cs`、`SexualConceptionBehavior.cs`、`NobleGatheringBehavior.cs` | RP、关系、生育、奖励等既有 save contract | 事实/动作结果经公开 contract；不在 UI 中直接写状态 | 维持现有 register patch 失败隔离 | hourly/daily 任务按既有频率；不每帧扫描 | item identity、关系副作用、旧存档和内容资源 | 保留旧 behavior facade；新纯服务可旁路 |
| UI/Diagnostics | `AnimusForgeNativeConversationOverlay*.cs`、百科 patch、`Terminal*`、reports | UI 不拥有 save state；编辑器写用户数据需备份 | UI 只展示/提交 contract；不复制 prompt 规则 | UI patch 独立失败；真实 RootWidget 优先 | Gauntlet/UI 主线程；关闭/输入队列有界 | RootWidget/datasource、Backspace、延迟 close、MCM | 禁用新 UI patch/overlay；恢复原版页面和旧入口 |
| Knowledge/Content | `KnowledgeLibraryBehavior.cs`、`AnimusForge/ModuleData/**`、`PlayerExports/**`、`ONNX/**` | content/user data 与 runtime save 分界 | owner 唯一；规则/Prompt/token 版本化但不隐式改历史 | 通常 None；若有 UI patch 单独登记 | index rebuild 分批；不可在 Tick 全量重建 | provenance、语言、ONNX paired asset、用户隐私 | 使用上一版 content/profile；runtime 读取失败走安全默认值 |
| Compatibility/Safety | `BannerlordExceptionSentinel.cs`、`NonBlockingErrorReport.cs`、`FreezeWatchdog.cs`、`LlmVisibleReplyNormalizer.cs` | 不改变存档身份 | 不泄漏 API key、原始网络响应或无限文本 | 保护边界失败要可诊断；不吞原版异常 | watchdog/diagnostics 低分配；统一 cancellation/stale guard | 1.3/1.4 API helper、SafeMode、原版 fallback | 禁用新保护层并保留原版路径；不能用“伤害=0”代替退出处理 |

## 4. 候选 Bridge 清单

Bridge 只暴露稳定、最小、可版本化的 capability/event/DTO；不暴露私有 Behavior、静态字段、UI VM、Harmony target、raw save dictionary 或 live TaleWorlds 对象。

| Bridge | 提供方 → 使用方 | 最小契约内容 | 不应承载 | 失败/降级 | 主要验证 |
|---|---|---|---|---|---|
| `AF.Bridge.ConversationMemory` | Conversation/AI → Memory/Persistence，反向读取公开 memory view | immutable conversation snapshot、history entry、AFEF fact、generation、channel | `MyBehavior` 实例、raw dictionary、原始 API 响应 | 记忆不可用时保留当前会话/安全空视图；不伪造事实 | 三渠道历史/AFEF 对齐、旧存档读取 |
| `AF.Bridge.ConversationAction` | Conversation/AI → World/Settlement/Mission/Progression | authorized action DTO、target identity、expected state、result code | 直接调用领域 behavior、live Hero/Agent/Mission | action rejected/expired 时只记录结果，不重复执行 | 标签输出→解析→主线程执行→结果事实 |
| `AF.Bridge.CourierConversation` | Courier ↔ Conversation/AI | channel、participant identity、message snapshot、reply/result | Courier 私有 save dictionary、另造 prompt schema | LLM 不可用时 native courier fallback | 信使/自由对话/场景喊话相同规则和历史 |
| `AF.Bridge.SceneActionsHost` | Host/Composition ↔ SceneActions runtime | initialize/mission register/verify result、version/status | Scene Agent 对象、坐标主目标、场景业务规则 | adapter Degraded；原版 scene behavior 继续 | mission 生命周期、Agent target、关闭后原版 |
| `AF.Bridge.WorldSettlement` | World Simulation ↔ Settlement/Siege | settlement/party identity snapshot、authorized world result | 直接持有 campaign singleton、LLM prompt | bridge 缺失时原版 world/settlement action | 攻城/出击/野战/和平场景 allowlist |
| `AF.Bridge.PolicyDiplomacy` | Policy ↔ Diplomacy/World | policy proposal/effect DTO、support/result、contract version | Policy UI VM、MCM 原始对象、私有 policy implementation | optional effect provider 缺失→Degraded；原版投票保留 | A、B、A+B、bridge、bridge failure 矩阵 |
| `AF.Bridge.UiDiagnostics` | UI ↔ Foundation/Diagnostics | bounded status、trace id、validation issue、safe command result | Game/Mission/Agent、无限日志、玩家原文 | UI 不可用不阻断 gameplay；日志有界 | Gauntlet root/datasource、输入焦点、诊断预算 |

### 4.1 Bridge 形成条件

只有满足以下条件才可从候选变成实施切片：

- 至少两个明确 owner 需要同一 capability，且不能通过已有公开入口完成；
- DTO 可脱离 TaleWorlds live 对象表达，并定义 `ContractVersion`；
- 已写明 API 线、profile、save/channel/user-data 影响；
- 已定义 provider 缺失、版本不兼容、执行失败和 SafeMode/fallback；
- 已有纯 contract test 或 fixture；
- 可保留旧入口作为 facade，且可以只关闭该 bridge 回滚。

## 5. 每个模块切片的非目标与回滚模板

后续每次生产变更必须在对应模块 README/ledger 中复制并填写：

```yaml
Slice:
  Owner: <one owner or named bridge owners>
  Files: [<concrete paths>]
  InScope: [<one behavior seam>]
  NonGoals:
    - Do not change SubModule registration order.
    - Do not change assembly identity, SubModule.xml or output layout.
    - Do not change SyncData key, save type or historical serialization shape.
    - Do not replace the three-channel interaction pipeline.
    - Do not move user data, reference source or generated artifacts.
    - Do not add a second physical gameplay DLL.
  Compatibility:
    ApiLines: [1.3, 1.4]
    ExistingHelpersFirst: true
  Runtime:
    Frequency: <boot|save-load|event|hourly|daily|mission|application-tick|engine-tick>
    ThreadBoundary: <main-thread/snapshot/main-thread-apply/no-game-access>
    QueueBound: <number or none>
    CacheOrBatch: <explicit policy>
  Persistence:
    SaveImpact: <None|ReadsExisting|WritesExisting|LegacyCompatibilityRequired>
    SyncDataKeyChange: Forbidden
    SaveTypeChange: Forbidden
  Rollback:
    DisableSwitch: <explicit profile/module/adapter switch>
    Facade: <old entry point>
    Fallback: <native/original behavior>
    Evidence: [<test/log/fixture>]
```

## 6. 当前阶段结论

- 阶段 2 的 Host registry、LLM owner、SubModule 注册面和 validator fixture 已有独立文档；它们都是频率 0 的设计/诊断材料。
- 当前仍未完成的是全量 owner 影响标注的逐文件落地、Bridge contract test 矩阵和各目标模块的具体 README/module manifest。
- 下一切片优先级：先完成 Conversation/Memory/Action 三条 contract 边界的逐文件影响表，再处理 Settlement/Siege 与 Policy/Diplomacy 候选 Bridge。
- 在阶段 3 manifest/profile/dependency/health 设计完成前，不执行大规模生产 C# 移动，不删除旧入口，不改变程序集、存档或发布结构。

## 7. 验证与未验证项

已执行：

- 读取 owner matrix、root LLM owner slice、SubModule registration catalog 和 registry DTO 设计；
- 建立本文件及其非目标/回滚模板；
- 文档层 `git diff --check`（后续同步完成后需再次执行）。

未执行：

- 未修改/编译/运行生产 C#；
- 未实现或运行 validator/contract tests；
- 未运行打包、部署、游戏启动、旧存档加载或三渠道实机回归；
- 未确认许可证、第三方 provenance 或精确 `v1.4.8.119303` overlay 的构建复现。