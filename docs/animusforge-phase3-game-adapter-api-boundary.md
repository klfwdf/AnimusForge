# 阶段 3：GameAdapter 与 Bannerlord 1.3/1.4 API 边界

- 状态：设计完成；未创建 GameAdapter 生产项目；未修改生产 C#
- 日期：2026-08-30
- owner：Compatibility/GameAdapter；Host、Conversation、Mission、Settlement/Siege、Policy owner 共同审阅
- 依据：`BannerlordApiCompat.cs`、`PlayerEncounterCompat.cs`、`AnimusForge.Bootstrap/BootstrapRuntime.cs`、`Properties/AssemblyInfo.cs`、`docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md`
- fixture：`F:\AnimusForge-main\docs\fixtures\phase3-game-adapter-api/`
- runner：`F:\AnimusForge-main\tools\GameAdapterContractTests\validate_game_adapter.py`

> 本文只建立兼容边界和验证矩阵，不授权修改 `BannerlordApiCompat.cs`、`PlayerEncounterCompat.cs`、Bootstrap 或任何生产调用方。

## 1. GameAdapter 的职责边界

GameAdapter 只负责：

- 1.3/1.4 API 差异的最小适配；
- null-safe、fail-closed 的 Bannerlord 对象访问；
- 缓存反射 MethodInfo/PropertyInfo，避免热路径重复反射；
- API capability 探测和版本 metadata；
- Harmony target/signature 的版本化描述；
- 主线程端口和安全结果；
- 缺失成员时的 `Unsupported`/`Degraded`/`Blocked` 结果。

GameAdapter 不负责：

- Conversation、Policy、Siege、Courier 等玩法规则；
- Prompt、Action tag、AFEF、UI VM 或模块私有存档；
- 改变 `SubModule.cs` 注册顺序；
- 推断不存在的 1.3 API；
- 把 compile symbol 当成实际 reference provenance；
- 让公共 `AF.Contracts` 直接引用 `Game`、`Mission`、`Agent`、`Hero`、`Settlement` 等 live 类型。

## 2. API capability 设计

```text
GameApiCapability
  CapabilityId: stable string
  ApiLine: 1.3 | 1.4
  Support: Supported | Unsupported | Degraded | Blocked
  MemberShape: stable signature description
  HelperId: existing compatibility helper or empty
  ReflectionCached: true | false
  MainThreadOnly: true | false
  FallbackId: explicit fallback
  ReasonCode: bounded stable code
  ContractVersion: positive integer
```

调用方只消费 capability/result，不直接判断私有版本细节。若成员只在 1.4 存在，1.3 结果必须明确 `Unsupported` 或安全 fallback，不能静默调用。

## 3. 当前真实差异与适配策略

| capability | 1.3 | 1.4 | 当前 helper/边界 | 缺失策略 |
|---|---|---|---|---|
| `mobile-party.trade-agreement` | 可能是带 out 参数的 `HasTradeAgreement` | 可能是无 out 参数 | `BannerlordApiCompat.HasTradeAgreement`；方法查找已缓存于调用逻辑边界 | false/Degraded；不改变外交事实 |
| `encounter.restart` | 3 参数 | 4 参数增加 raid flag | `PlayerEncounterCompat.RestartPlayerEncounter` 统一入口 | Unsupported/Blocked；不直接调用原版方法 |
| `encounter.map-event-access` | 公开/私有形状可能不同 | 字段/属性形状可能不同 | `PlayerEncounterCompat.Get*Safe` | null-safe；回原版 encounter |
| `mission.spawn-inspection-troop` | 额外 bool 参数 | 无额外 bool | `BannerlordApiCompat.SpawnInspectionTroop` + `TroopInspectionBehavior` 分支 | Unsupported；不生成半初始化 Agent |
| `agent.combat-target` | 公开 API 形状不稳定 | 方法可通过适配访问 | `Get/SetAgentCombatTarget`、`TrySetAgentAutomaticTargetSelection` | false；保持原版 AI |
| `mobile-party.ai-model.port-patrol` | `FortificationPortPatrolDistanceAsDays` 不存在 | 存在 | `CourierMobilePartyAIModel` 仅在 1.4 条件编译 | 1.3 不声明 capability |
| `battle-rewards.prefix` | 旧 float/gold/ref signature | `ExplainedNumber` / loot rate signature | `MilitaryExerciseBattleRewardsZeroPatch` 双签名 | 未匹配时跳过 patch并记录 |
| `map-event.renown-influence` | `ApplyRenownAndInfluenceChanges` 可 patch | 缺失/变化 | `MilitaryExerciseBehavior` 仅 1.3 patch | 1.4 skip，不伪造奖励结果 |
| `mission-camera.fade` | `MissionCameraFadeView` 路径可用 | 旧路径不可用 | `MeetingBattleLockMissionBehavior` 条件分支 | 1.4 跳过 startup fade delay |
| `gauntlet.mouse-release` | `OnMouseReleased()` override | late-update polling | `DevMultilineEditableTextWidget` 双路径 | 保持输入安全，不阻断 UI |
| `party.hero-roles` | 旧角色 API 可能 MissingMethod | `GetHeroPartyRoles` 可用 | `ShoutBehavior` 使用 shared role holder fallback | 缺失时使用稳定角色 fallback |
| `assembly.api-marker` | AssemblyMetadata=1.3 | AssemblyMetadata=1.4 | `Properties/AssemblyInfo.cs` + Bootstrap validation | marker 缺失/错配时 fail-closed |

## 4. 版本选择与程序集边界

1. Bootstrap 优先使用权威 `BuildInfo.GameVersion`；无法获得时只接受明确的正向 feature 证据。
2. 无法确定 API 线时必须 fail-closed，不能根据目录名或缺失 1.4 feature 猜 1.3。
3. Bootstrap 校验实现 DLL 存在、managed image 可读、assembly simple name 为 `AnimusForge`、API metadata 与选择线一致。
4. SubModule.xml 仍只加载 `AnimusForge.Bootstrap.dll`；不得直接声明 1.3/1.4 implementation DLL。
5. 1.3/1.4 implementation 仍分别位于 unified module 的 `versions/1.3`、`versions/1.4`，但逻辑 GameAdapter 可先留在单一实现源码树中。
6. 1.3/1.4 构建 selector、compile symbol 和 reference provenance 是三件不同的事；必须分别记录。

## 5. Harmony/反射边界

每个兼容敏感 patch 必须记录：

```text
Owner: GameAdapter or named module
PatchId
ApiLine
TargetType
TargetMethod/signature
ReflectionCached
ConflictPolicy
FailurePolicy
Fallback
FocusedTest
```

规则：

- 反射只在初始化/非热路径解析并缓存；
- missing member 只记录一次或限频，不在 Tick 无界重试；
- 业务模块不复制 `AccessTools` 私有字段探测；
- 不通过 patch 顺序形成未声明的仲裁链；
- 不能安全撤销的 Harmony/engine state 不声明 runtime-toggle-safe；
- patch 缺失时保留原版逻辑，不能用伪造成功结果替代。

## 6. 主线程和结果边界

```text
GameAdapter query/read
  → detached typed snapshot (background-safe)
background work
  → typed result + RuntimeGeneration
main thread
  → re-resolve ID
  → re-check state/permission
  → execute once
  → publish confirmed result/fact
```

GameAdapter 不能把 live object 放入后台 snapshot，也不能让后台线程直接改变 Agent、Mission、Campaign、UI 或存档。

## 7. 纯版本差异验证矩阵

| case | 内容 | 预期 |
|---|---|---|
| api-13-supported | 1.3 capability 使用已有 helper | Supported/Degraded，fallback 明确 |
| api-14-supported | 1.4-only member 存在 | Supported |
| api-13-missing-14-feature | 1.3 查询 1.4-only feature | Unsupported，不猜测版本 |
| restart-3-args | 3 参数 RestartPlayerEncounter | 统一 helper 可用 |
| restart-4-args | 4 参数含 raid flag | 统一 helper 可用 |
| spawn-signature | SpawnTroop 参数差异 | 版本分支/adapter，禁止错误签名 |
| rewards-signature | battle rewards prefix 差异 | 只应用匹配 API line |
| missing-member | reflection target 不存在 | bounded failure + native fallback |
| api-marker-mismatch | implementation metadata 错配 | Bootstrap fail-closed，不加载另一版本 |
| unknown-version | 无法确定 API line | NotSupported/Blocked，不猜测 |
| cached-reflection | repeated call | 使用初始化缓存，不能每次扫描 |
| main-thread-apply | adapter result 应用到 live state | 仅主线程 apply |
| package-boundary | implementation declaration | 只有 Bootstrap 在 SubModule.xml |
| dual-build | 1.3/1.4 selector | 两条实现分别构建，逻辑 contract 不变 |

## 8. 非目标、回滚和完成门槛

非目标：

- 不修改现有 helper 实现；
- 不添加新的条件编译分支；
- 不改构建/打包/部署脚本；
- 不改变程序集身份、SyncData key、存档类型、SubModule.xml 或 unified module 输出；
- 不把所有 Bannerlord API 包装成巨型通用接口；
- 不以 catalog/fixture 通过代替双版本构建和实机验证。

回滚：

- 不注册新 GameAdapter capability consumer；
- 继续使用当前 `BannerlordApiCompat`、`PlayerEncounterCompat`、Bootstrap 和已有 fallback；
- fixture/runner 可独立删除，不影响生产行为。

进入生产 GameAdapter 小切片前必须：

1. 本文 fixture/runner 通过；
2. 真实目标方法在 1.3/1.4 reference 中分别核对；
3. 1.3 与 1.4 build 使用精确 BuildInfo 记录；
4. missing-member、unknown-version、marker mismatch 和 native fallback 有证据；
5. 用户明确授权修改生产 C#。

下一项：

> 进行阶段 3 最终设计审查；若 GameAdapter fixture 通过，则阶段 3 可标记为“设计完成，生产实现未开始”，随后进入阶段 4 Persistence/Profile/Config 设计。