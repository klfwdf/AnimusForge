# 阶段 2：Settlement/Siege 与 Policy/Diplomacy Bridge Contract

- 状态：候选 Bridge contract 设计完成；未实现 Bridge；未修改生产 C#
- 日期：2026-08-29
- owner：Settlement/Siege、Policy/Diplomacy、Conversation/AI 与 Host/Composition 共同审阅
- 依据：`AfGcczShoutBridge.cs`、`SiegeAiInterventionBehavior.cs`、`AnimusForge.SiegeAftermathIntervention/**`、`PolicySystem/Effects/**`、`DiplomacyBehavior.cs`、`WorldDiplomacyBehavior.cs`
- fixture：`F:\AnimusForge-main\docs\fixtures\phase2-settlement-policy-bridges\`

> 本文只定义候选边界和组合验证输入，不授权新增 Bridge 类、不移动代码、不改变现有 `AfGcczShoutBridge` 或 PolicySystem 行为。

## 1. 共同规则

1. Bridge 只暴露稳定的 capability/event/DTO；不暴露私有 Behavior、Harmony target、UI VM、raw save dictionary 或 live TaleWorlds 对象。
2. Bridge 不负责发现模块、不扫描程序集、不改变 `SubModule.cs` 注册顺序。
3. Bridge 只能在明确的 owner 之间传递数据；没有共同 owner 或没有 public contract，就不实现 Bridge。
4. `A`、`B` 在 Bridge 缺失时必须仍可独立工作；Bridge 失败不能把 A 或 B 变成不可启动。
5. Bridge 输出必须包含 `ContractVersion`、`Status`、`ReasonCode`、`Fallback` 和有界诊断信息。
6. 1.3/1.4 API 差异留在各自 GameAdapter/实现中，Bridge DTO 不直接引用 Bannerlord API 类型。
7. Bridge 的频率为事件触发或按现有 campaign/mission 生命周期触发；禁止进入每帧全量扫描。

## 2. `AF.Bridge.SettlementSiegeAction`

### 2.1 现有边界

| 角色 | 当前入口 | 可复用责任 | 不能转移的责任 |
|---|---|---|---|
| Host/Conversation adapter | `AfGcczShoutBridge.IsActive`、`BuildRuntimePromptForExternal`、`BuildPostprocessRules`、`TryProcessActionTags` | active stage、prompt/postprocess 路由、失败隔离 | Siege 业务规则、Mission live object 保存 |
| Siege runtime | `SiegeAiInterventionBehavior.BuildRuntimePromptForPromptContext`、`TryProcessAiActionTags` | 场景事实、规则筛选、标签处理、目标验证 | LLM 网络调用、Host 注册顺序 |
| Pure policy | `SiegeActionRoutingPolicy.Evaluate`、`SiegeCastleDirectActionAuthorizationPolicy.Evaluate` | 无副作用的授权/路由决定 | Agent/Mission/Settlement 直接修改 |
| Reusable GCCZ rules | `AnimusForge.SiegeAftermathIntervention/**` | profile、catalog、facts、routing policy、memory codec | AF Host 生命周期和 UI |

### 2.2 请求 DTO（设计）

```text
SettlementSiegeActionRequest
  ContractVersion
  SettlementId
  SceneKind
  EncounterMode
  MissionContext
  SourceChannel
  ConversationGeneration
  TargetIdentity
    HeroId / CharacterId / AgentIndex / LocationId
  RequestedActionId
  PreprocessRuleIds
  PostprocessRuleIds
  CurrentStateFingerprint
```

约束：

- `TargetIdentity` 必须优先使用 Agent/AgentIndex/LocationCharacter 等可解析身份；裸坐标不能作为主目标；
- `MissionContext` 只允许枚举和稳定 ID，不允许 `Mission`、`Agent`、`Hero` 实例；
- `CurrentStateFingerprint` 用于主线程重新验证，不能当作授权本身；
- `ConversationGeneration` 过期时拒绝执行，不写 AFEF，不写存档；
- `PostprocessRuleIds` 必须闭合，不能只注入前处理规则。

### 2.3 输出 DTO（设计）

```text
SettlementSiegeActionResult
  ContractVersion
  Status: Applied | Authorized | Denied | Degraded | Unsupported | Expired | Failed
  ActionId
  TargetIdentity
  ReasonCode
  Fallback: Native | NoOp | SafeMode | RetryAtBoundary
  ConfirmedFacts
  ConversationGeneration
```

Bridge 只产生 `Authorized` 或 routing result；真正的 Agent/Mission/Settlement 修改仍由主线程上的现有 runtime 执行，完成后才产生 `Applied` 和 confirmed AFEF fact。

### 2.4 必须保留的安全排除

- 普通原版战斗、攻城、SallyOut、FieldBattle、Deployment、Stealth、Duel、竞技场、训练场和被围攻定居点不能被“和平场景”误判；
- `Settlement.CurrentSettlement`、`MobileParty.MainParty.CurrentSettlement` 或 owner clan 单独不能作为 allowlist；
- 关闭 AF 处理时必须退出模组处理并回到原版，而不是把伤害改成 0；
- Agent/Team/Mission 的状态改变只在严格命中的机制场景中初始化；
- Bridge failure 不能吞掉原版伤害、会面或场景行为。

## 3. `AF.Bridge.PolicyDiplomacy`

### 3.1 现有边界

| 角色 | 当前入口 | 可复用责任 | 不能转移的责任 |
|---|---|---|---|
| Policy catalog/contracts | `PolicySystem/Effects/PolicyEffectModuleContracts.cs`、`PolicyEffectModuleCatalog.cs` | module descriptor、scope、payload、contract/version 检查 | 世界外交语义、自由文本目标解析 |
| Policy compile/route | `PolicyEffectCompiler.cs`、`PolicyEffectModuleRouter.cs` | normalize、target plan、funding、module selection、compiled plan | 直接改 Kingdom/Settlement 状态之外的外交编排 |
| Policy execution | `PolicySystem/Core/PolicyEffectExecutionCoordinator.cs` | lifecycle、execute、receipt、idempotency、rollback/compensation | Conversation/LLM prompt、原版外交 patch |
| Conversation diplomacy | `DiplomacyBehavior.ProcessDiplomacyTagsDispatch`、`TryExecuteDeclareWar`、`TryExecuteMakePeace` | 入口授权、当前 faction/kingdom 验证、原版 action 调用 | Policy module 私有实现、WorldDiplomacy 私有存储 |
| World diplomacy | `WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved`、`NotifyExternalDiplomacyResolvedInternal` | 已解决外交事实、世界外交 history/通知 | Conversation 标签解析、Policy UI |

现有 `DiplomacyBehavior → WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved` 已是跨域事实通知入口。候选 Bridge 的第一阶段应优先把它记录成 contract，而不是再添加一条平行通知链。

### 3.2 请求 DTO（设计）

```text
PolicyDiplomacyRequest
  ContractVersion
  SourceChannel
  ConversationGeneration
  InitiatorKingdomId / InitiatorClanId
  TargetKingdomId / TargetClanId
  PolicyIntentId
  PolicyModuleIds
  Scope
  TargetPlanHandles
  CurrentStateFingerprint
  RequestedOutcome
```

约束：

- 目标必须来自 PolicySystem 的 canonical target plan；不能在执行模块内按自由文本名称扫描英雄、家族、军队或定居点；
- `PolicyModuleIds` 只引用 catalog 中已启用且版本兼容的模块；
- `RequestedOutcome` 不是已发生事实，必须等原版 action/Policy coordinator 返回 receipt 后再通知 WorldDiplomacy；
- Bridge 不改变 PolicyEffect 的 persistence namespace、save codec、schema migration 或 MCM retrieval kill switch；
- source module disabled 只影响未来 retrieval，不强行停止已经持久化的 active policy effect。

### 3.3 输出 DTO（设计）

```text
PolicyDiplomacyResult
  ContractVersion
  Status: Accepted | Applied | Rejected | Degraded | Incompatible | Expired | Failed
  OutcomeId
  InitiatorId
  TargetId
  PolicyReceiptId
  ReasonCode
  Fallback: NativeDiplomacy | NativePolicy | SafeMode | NoOp
  ConfirmedWorldFact
  ConversationGeneration
```

只有 `Applied` 且带有效 receipt 的结果，才允许调用现有 `NotifyExternalDiplomacyResolved` 语义；`Accepted`、`Rejected`、`Degraded` 和 LLM 文本不能直接写成世界外交事实。

## 4. 五种组合验证矩阵

每个 Bridge 都必须验证：A、B、A+B、A+B+Bridge、Bridge failure。

### 4.1 Settlement/Siege

- **A：Settlement/Siege runtime alone**：场景规则、routing policy 和原版 aftermath 可独立工作；没有 Conversation/AI 输入时不生成 LLM action。
- **B：Conversation/Scene adapter alone**：三渠道可继续生成普通对话；没有 Siege provider 时不注入 Siege 专属规则。
- **A+B without Bridge**：两边同时启用，但不共享 Siege action；普通对话和 Siege runtime 各自保持边界。
- **A+B+Bridge**：后处理标签经过 identity、授权、当前状态和主线程 apply；完成后才产生 confirmed fact。
- **Bridge failure**：Bridge 返回 `Degraded`/`Failed`，Conversation 显示安全结果，Siege 保留原版或自身 fallback，不能重复执行。

### 4.2 Policy/Diplomacy

- **A：PolicySystem alone**：Policy catalog/compiler/coordinator 可执行已授权 policy effect，不依赖 Conversation 或 WorldDiplomacy。
- **B：Diplomacy/World alone**：原版外交与 `DiplomacyBehavior`/`WorldDiplomacyBehavior` 可独立处理，不依赖 Policy retrieval。
- **A+B without Bridge**：政策效果和外交世界状态同时存在，但不会因为相似文本自动互相触发。
- **A+B+Bridge**：canonical target、receipt、generation、contract version 全部通过后，才发布世界外交事实。
- **Bridge failure**：Policy effect 保留自己的 coordinator/receipt，WorldDiplomacy 保留自己的 state/history；只丢弃跨域通知，不回滚无关模块。

## 5. 非目标、回滚和实现门槛

### 非目标

- 不新增第二套 LLM prompt、标签解析、历史或 AFEF 结构；
- 不把 `AfGcczShoutBridge` 直接改造成通用 God Bridge；
- 不把 `PolicySystem` foundation 化并让它拥有外交玩法；
- 不改 `SubModule.cs`、`SubModule.xml`、程序集身份、SyncData key、存档类型、MCM key 或单一模块输出；
- 不删除旧 facade，不进行大规模文件移动；
- 不让 Bridge 持有 live `Game`、`Mission`、`Agent`、`Hero` 或 UI 对象。

### 可逆入口

| Bridge | 禁用点 | 保留行为 | 回滚结果 |
|---|---|---|---|
| Settlement/Siege | 停止新 action adapter/bridge 输入 | Siege runtime 与原版 scene/aftermath fallback | 不共享动作，不影响普通对话 |
| Policy/Diplomacy | 停止跨域通知 adapter | Policy coordinator、DiplomacyBehavior、WorldDiplomacy 各自运行 | 不发布跨域事实，不删除已有 receipt/state |

### 实现前置门槛

- 两个参与 owner 明确共同维护人；
- contract version、profile、API line、save/channel/user-data impact 已登记；
- A/B 五种组合 fixture 通过纯 contract test；
- 失败、缺依赖、不兼容、stale generation 和 SafeMode 均有结构化结果；
- 旧 facade、原版 fallback、1.3/1.4 构建和存档验证计划已列出；
- 用户明确授权后，才可进入单个小切片的生产 C# 修改。

## 6. 当前结论

- 两个 Bridge 目前仍是“候选 contract”，不是实施许可。
- Settlement/Siege 优先复用现有 `AfGcczShoutBridge` 和 `AnimusForge.SiegeAftermathIntervention` 纯 policy；不新增平行动作通道。
- Policy/Diplomacy 优先把现有 PolicySystem contract 与 `NotifyExternalDiplomacyResolved` 记录成边界；不让 Policy module 直接拥有外交世界状态。
- 当前 fixture 是纯 JSON，位于 `docs/fixtures/phase2-settlement-policy-bridges/`，不在生产 `.csproj` 中。
- 下一阶段仍然是阶段 2 设计工作；本轮未修改、编译、部署或测试生产 C#。