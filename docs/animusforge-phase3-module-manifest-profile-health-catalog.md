# 阶段 3：Module Manifest / Profile / Dependency / Health Catalog

- 状态：设计完成；未实现 Foundation/Registry；未修改生产 C#、`SubModule.cs` 或程序集图
- 日期：2026-08-29
- owner：Foundation/Host/Composition（各模块 owner 共同审阅）
- 依据：`.claude/skills/animusforge-maintainer/references/plugin-architecture.md`、`module-and-bridge-workflow.md`、`validation.md`
- 纯 fixture：`F:\AnimusForge-main\docs\fixtures\phase3-module-catalog\`
- 独立 runner：`F:\AnimusForge-main\tools\ModuleCatalogContractTests\validate_module_catalog.py`

> 本文的 module ID 是目标逻辑模块/Bridge 的设计 catalog，不代表当前仓库已经创建这些物理项目、DLL、入口类型或发布目录。当前仍保持单一 `AnimusForge.dll`、Bootstrap 单实现选择和旧 facade。

## 1. Catalog 与运行时的边界

### Catalog 可以描述

- 稳定 module/bridge ID、kind、版本和 contract version；
- owner、maintainer、profiles、API 线；
- required/optional modules 和 capabilities；
- provided capabilities/events；
- persistence namespace/schema 的所有权声明；
- activation/lifecycle、Harmony/UI/Tick/background 影响；
- 健康状态、失败阶段、fallback、trace ID 和有界诊断；
- staged package 的 DLL/content closure 预期。

### Catalog 不可以持有

- `Behavior`、`MissionBehavior`、Harmony target、delegate、`MethodInfo`；
- `Game`、`Mission`、`Agent`、`Hero`、`IDataStore` 等 live 对象；
- Prompt 正文、API key、原始网络响应、完整对话、PlayerExports 内容；
- 存档实例、raw save dictionary 或可执行闭包；
- 任何要求 `SubModule.cs` 改变注册顺序的隐式指令。

Catalog/validator 运行频率为 **0**：启动诊断、离线验证或测试时按需读取；不进入 ApplicationTick/EngineTick，不做每帧反射或全量扫描。

## 2. Module manifest 设计

设计期 schema 如下；实现阶段 `entryType` 必须由实际模块提供，设计 fixture 可以使用 `entryTypeStatus: Pending`，不能把占位符发布到游戏。

```yaml
ModuleManifest:
  id: af.module.example
  kind: module | foundation | adapter | bridge
  version: 0.1.0-design
  contractVersion: 1
  owner: Host.Composition
  maintainers: [owner-account]
  profiles: [single-player, developer]
  entryTypeStatus: Pending | Bound
  requiredModules:
    - id: af.foundation.runtime
      versionRange: ">=1.0.0 <2.0.0"
  optionalModules: []
  requiresCapabilities:
    - id: game-state.read
      versionRange: ">=1.0.0 <2.0.0"
  providesCapabilities:
    - id: example.read
      version: 1.0.0
      contractVersion: 1
  persistence:
    namespace: example
    schemaVersion: 1
    identityChange: Forbidden
  lifecycle:
    activation: save-load-boundary
    runtimeUnload: unsupported
    harmonyPatches: false
    campaignBehavior: false
    missionBehavior: false
    ui: false
    applicationTick: false
    engineTick: false
    backgroundWork: false
  compatibility:
    bannerlord: [1.3, 1.4]
  rollback:
    facade: existing-entry-point
    fallback: native-or-explicit-safe-mode
```

### Manifest validator 必须拒绝

- 空/重复/不稳定 ID；
- 缺 owner、maintainer、contract version、profile 或 API line；
- persistence namespace 重复或宣称改变旧 identity；
- required dependency/capability 不存在、版本不兼容或形成环；
- `runtime-toggle-safe` 但声明 Harmony、CampaignBehavior、MissionBehavior、持久化或不可清理后台任务；
- Bridge 未声明参与模块、共同 owner、独立 namespace 和失败 fallback；
- 以文件名自动注册未知 module，或把设计占位 `entryType` 当作可执行入口。

## 3. 当前逻辑 catalog 草案

以下是目标逻辑边界，不是当前物理 DLL 要求：

| ID | kind | owner | required modules | provides | lifecycle | persistence namespace |
|---|---|---|---|---|---|---|
| `af.foundation.runtime` | foundation | Foundation/Host | — | `module.inventory`、`main-thread.dispatch`、`persistence.facade`、`safe-mode` | boot-only | foundation |
| `af.game-adapter` | adapter | Compatibility/GameAdapter | foundation | `game-state.read`、`harmony.ports`、`api-line.1.3-1.4` | boot-only | none |
| `af.module.conversation` | module | Conversation/AI | foundation、game-adapter | `conversation.context`、`conversation.action-authority` | save-load-boundary | conversation |
| `af.module.siege-aftermath` | module | Settlement/Siege | foundation、game-adapter | `settlement.siege.context`、`settlement.siege.actions` | save-load-boundary | siege-aftermath |
| `af.module.policy-effects` | module | Policy | foundation、game-adapter | `policy.effect.catalog`、`policy.effect.execution` | save-load-boundary | policy-effects |
| `af.module.world-diplomacy` | module | World/Diplomacy | foundation、game-adapter | `world.diplomacy.state`、`world.diplomacy.events` | save-load-boundary | world-diplomacy |
| `af.bridge.conversation-siege` | bridge | Conversation + Settlement/Siege | conversation、siege-aftermath | `conversation.siege.action-bridge` | save-load-boundary | bridge.conversation-siege |
| `af.bridge.policy-diplomacy` | bridge | Policy + World/Diplomacy | policy-effects、world-diplomacy | `policy.diplomacy.bridge` | save-load-boundary | bridge.policy-diplomacy |

Bridge 的 logical manifest 仍然不能导入参与模块私有实现；只消费 public capability/event。`AfGcczShoutBridge` 是当前已有的适配边界，后续实现必须先判断它是否已经满足该 contract，不能直接再建平行入口。

## 4. Profile 设计

| profile | 允许内容 | 明确排除 | 生命周期规则 |
|---|---|---|---|
| `single-player` | foundation、GameAdapter、已验证的 gameplay modules 和通过矩阵的 Bridge | 未声明/未验证 module、服务器专用内容 | CampaignBehavior/Harmony 在 boot 或 save-load boundary 决定 |
| `safe-mode` | foundation、GameAdapter、persistence metadata、diagnostics | gameplay modules、Bridge、未知 provider、自动替换 gameplay | 保护存档、显示 inventory/health；不删除数据、不自动迁移 |
| `developer` | single-player 的闭合集合 + inventory、trace、contract checker/test hooks | 未声明 DLL、网络下载模块、未知 C# | test hooks 不进入发布 profile；运行时仍受主线程/队列界限 |

Profile closure 规则：

1. profile 直接包含的每个 module 必须存在；
2. 每个 required module 和 required capability 必须在同一 profile 闭包中；
3. optional provider 缺失可以产生 `Degraded`，不能把 profile 判成 `Valid` 后静默使用；
4. `safe-mode` 不得包含 Bridge 或 gameplay module；
5. disabled/failed module 的保存 metadata 保留，不删除未知 namespace；
6. profile 不是 DLL 下载器，不允许运行未知插件。

## 5. Dependency / capability graph

解析顺序由显式依赖图决定，不依赖 Harmony 注册顺序、目录枚举顺序或反射发现顺序。

```text
af.foundation.runtime
  → af.game-adapter
    → af.module.conversation
    → af.module.siege-aftermath
    → af.module.policy-effects
    → af.module.world-diplomacy

conversation + siege-aftermath
  → af.bridge.conversation-siege

policy-effects + world-diplomacy
  → af.bridge.policy-diplomacy
```

状态规则：

```text
Discovered
  → Disabled       # profile/settings intent
  → Blocked        # required dependency/capability/version/cycle/conflict
  → Starting
  → Active
  → Degraded       # optional provider missing + explicit fallback
  → Failed         # start/health/runtime failure
  → RestartRequired # unsafe to change in current process/campaign
```

- required dependency失败：dependent=`Blocked`；无关模块继续；
- optional provider缺失：consumer=`Degraded`，必须记录 fallback identity/reason；
- Bridge失败：A/B 保持可用，Bridge=`Failed`，不得写入对方 namespace；
- SafeMode：Bridge absent，存档 metadata 保留，inventory 解释原因；
- 活跃 Harmony、CampaignBehavior、持久化模块不能假装支持热卸载，toggle 应返回 `RestartRequired` 或 `save-load-boundary`。

## 6. Health catalog

每个 module/Bridge 的 health entry 至少包含：

```text
ModuleId
State
RunGeneration
ProfileId
HealthCheckId
FailureStage
ReasonCode
FallbackId
TraceId
BoundedMessage
ObservedAt
```

健康检查最小集合：

| check ID | 检查内容 | 失败状态 |
|---|---|---|
| `manifest.schema` | 字段和值域 | Invalid/Blocked |
| `identity.unique` | module ID、namespace、capability ID 唯一 | Blocked |
| `dependency.graph` | required/optional dependency 和 cycle | Blocked |
| `capability.provider` | provider 存在、版本兼容、profile 闭合 | Blocked/Degraded |
| `profile.closure` | include/exclude/required 闭合 | Blocked |
| `lifecycle.claims` | toggle、Harmony、save、tick、background 声明一致 | Blocked/RestartRequired |
| `api-line.support` | 1.3/1.4 支持声明与实现 metadata 一致 | Blocked |
| `persistence.ownership` | namespace/schema/legacy owner 唯一 | Blocked |
| `package.closure` | staged DLL/content allowlist 闭合 | Blocked |
| `runtime.failure-isolation` | failed module 不留下可逆注册/任务 | Failed |

Health 输出必须有界：最多 32 条 issue，每条消息最多 240 字符，不包含异常全文、玩家输入、Prompt、网络响应或密钥。

## 7. SafeMode 与回滚

- SafeMode 保留 Foundation、GameAdapter、persistence metadata 和 diagnostics；
- SafeMode 不加载 gameplay module/Bridge，不删除未知数据，不自动迁移；
- `boot-only` / `save-load-boundary` 变更必须返回 `RestartRequired` 或要求退出/重载 campaign；
- Bridge 失败只阻止跨域行为，不回滚不属于 Bridge 的 A/B 状态；
- 旧入口仍作为 facade，先旁路新 catalog/contract，再逐步切换；
- 没有真实 disposer 的 Harmony、Mission、Team、Agent 或存档副作用不能声明可逆；
- 任何生产切片都必须先通过 no-op、required dependency missing、optional provider missing、incompatible version、SafeMode 和 failure-isolation 组合验证。

## 8. 当前非目标与下一项

本阶段不做：

- 新建 `AF.Foundation.Runtime` 项目或物理 DLL；
- 接入 `SubModule.cs`、改变注册顺序或替换当前 Bootstrap 输出；
- 把现有 PolicySystem、GCCZ、Conversation 或 WorldDiplomacy 代码移动到新目录；
- 改变程序集身份、SyncData key、存档类型、MCM key 或三渠道语义；
- 下载/加载未知 DLL 或生成 C#；
- 以 catalog 通过代替 1.3/1.4 构建、旧存档和游戏验收。

下一项：

> 用独立 runner 验证 manifest 唯一性、依赖闭包、profile closure、SafeMode、optional/required failure、lifecycle contradiction 和 health 输出；通过后才考虑阶段 3 的纯 contract DTO 设计。