# J11 制作组内部模块接缝实施计划

> 状态：`J11_OFFLINE_VERIFIED`（2026-09-21）
> 前置：J07–J10 已完成必要离线验收；J10 推送终点 `98faded3`。
> 本计划只治理编入 `AnimusForge.dll` 的 Policy / Gathering / Siege 内部 typed 接缝与薄适配，不迁移或重写玩法，不开放独立子 MOD API，不修改默认入口、存档、游戏目录或 `G:/AFMOD/GCCZ`。

## 0. 最终回执

| 包 | 状态 | 产品 / 证据 |
| --- | --- | --- |
| J11-G0 | DONE | `docs/architecture/af-team-module-seam-matrix.md`：13 方法、31 调用点、分类/频率/gate/副作用 owner |
| J11a contracts | DONE | `cdbd077a`：3 个接口分文件归位；签名/namespace/internal 可见性不变 |
| J11b Policy | DONE | 4 方法 / 8 调用点；Policy 1.3/1.4 各 1406 + 1115 |
| J11c Gathering | DONE | 5 方法 / 12 调用点；facts/notifications 仍由真实调用方唯一提交 |
| J11d Siege | DONE | 4 方法 / 11 调用点；FeatureBridge/active-stage/GCCZ owner 不变，未写外部 GCCZ 树 |
| J11e composition | DONE | Campaign 42+5、Composition 18/24、Bridge 16/23/12、API lifecycle 通过 |
| J11f cleanup/final | OFFLINE_VERIFIED | 旧 2 文件删除；六构建、1060 API、142/168 persistence、394 map 及三渠道影响面通过 |

详细证据见 `docs/handoffs/2026-09-21-j11-team-module-seams-offline-closeout.md`。J12–J14 未在本包偷渡，LIVE/SAVE 仍独立。

## 1. 目标架构

```text
AnimusForge.dll
├─ AF 主体（Conversation / Prompt / LLM / Actions / Memory）
│      │ 只依赖 internal typed contracts
│      ▼
├─ src/AF.Contracts/Internal/TeamModules
│   ├─ Policy port
│   ├─ Gathering port
│   └─ Siege port
│      │
│      ▼
├─ src/bridges/{Policy,Gathering,Siege}
│   └─ 无状态薄 adapter：参数/返回/ref/out/异常一一转发
│      │
│      ▼
└─ 原玩法 owner
    ├─ PolicySystem/**
    ├─ NobleGatheringBehavior.cs
    └─ AnimusForge.SiegeAftermathIntervention/** + AfGcczShoutBridge.cs

独立子 MOD DLL ──> Public Api.V1 / 后续版本（不属于 J11）
```

`src/bridges` 表示主体与制作组业务之间的代码接缝，并不自动把每个薄 adapter 提升为有独立状态/生命周期的正式 FeatureBridge。只有真正跨域组合行为才使用 `FeatureBridgeRuntime` 的既有契约。

## 2. 当前已核实基线

| 模块 | internal port | 方法数 | 当前生产调用点 | 原业务 owner |
| --- | --- | ---: | ---: | --- |
| Policy | `IPolicyModulePort` | 4 | 8 | `KingdomAgendaCustomPolicyBehavior`、`NpcRulerPolicyBehavior` |
| Gathering | `IGatheringModulePort` | 5 | 12 | `NobleGatheringBehavior` |
| Siege | `ISiegeModulePort` | 4 | 11 | `AfGcczShoutBridge`，再进入 GCCZ 领域 owner |
| 合计 | 3 个接口 | 13 | 31 | 玩法仍归原 owner |

当前真实文件：

- 启动基线契约：`Refactor/Modules/TeamModulePorts.cs`（已由 J11 删除）
- 启动基线 adapter：`Refactor/Modules/TeamModuleAdapters.cs`（已由 J11 删除）
- 装配：`src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs`
- 能力目录：`src/AF.Foundation.Runtime/ModuleDirectory/*`、`src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs`
- 可选跨域门禁：`Refactor/Runtime/FeatureBridgeRuntime.cs`

J10 最终 `TeamModulePortParityTests` 已证明 13 个方法的 31 个调用表达式在 receiver 替换后，与 `df6ab928` 基线的参数、顺序、ref/out 和数量一致；308 项行为断言与 3 个变异通过。它是 J11 的起点，不代表 J11 的物理归位已完成。

## 3. 完成定义

只有同时满足以下条件，J11 才可标 `OFFLINE_VERIFIED`：

1. 三组 internal 契约各自归位 `src/AF.Contracts/Internal/TeamModules`，且生产只存在一份声明；不为未来可能需求增加占位方法。
2. 混合 `TeamModuleAdapters.cs` 被拆成三个真实薄 adapter，分别位于 `src/bridges/Policy`、`Gathering`、`Siege`；真实 31 个消费者仍经 `TeamModuleServices` 使用同一实例和同一语义。
3. Policy、Gathering、Siege 玩法算法、数值、MCM、存档和调度不迁入主体或 adapter；异常不被吞掉，facts/notifications 不在桥内二次提交。
4. `TeamModuleServices` 只做冷装配，不成为 service locator、运行时扫描器或每次调用字典查询；InternalModuleDirectory 的 Ready/能力快照不替代实际玩法资格。
5. `conversation-siege` 等既有 FeatureBridge 门禁仍在原实际入口且早于副作用；internal port 不绕过门禁或扩大权限。
6. 旧契约/混合 adapter 仅在所有消费者、源码提取测试、目录清单和代码地图改接后删除；无重复实现、转发壳链或直连绕过。
7. 聚焦契约、有效变异、Policy/Gathering/Siege 影响面、Debug/Release 双 API + Bootstrap、API/存档/代码地图门禁通过；LIVE/SAVE 仍单列。

## 4. 稳定接口规则

1. **内部与外部分开**：J11 的接口保持 `internal`，不得放入 `AnimusForge.Api.V1`，不得向独立子 MOD 暴露 Hero、CharacterObject、Behavior 或内部规则类型。
2. **初始迁移不改语义签名**：第一轮只做有证据的物理归位和 adapter 分拆，namespace、可见性、参数、默认值、ref/out、返回值和异常保持；不借搬家设计第二套“更漂亮”协议。
3. **不写死主体实现**：主体只按 Policy/Gathering/Siege 语义调用 port，不依赖具体 Behavior 私有字段、存档字典、Harmony 顺序或具体文件位置。
4. **不写死玩法实现**：adapter 只转发，不缓存 Hero/Mission/session，不拼 Prompt、不解析标签、不写历史/AFEF、不显示通知、不替领域 owner retry。
5. **DTO 需要证据才引入**：只有多个真实消费者因同一参数簇频繁同步变更，且能证明减少耦合时，才另包引入 immutable internal request/result；不得同时保留旧长参数与新 DTO 两条执行链。
6. **失败语义保持**：资格失败、空规则、normalizer 异常、apply 异常、Bridge 禁用/失败继续由原 owner 定义；薄桥不得 catch 后返回空值伪成功。
7. **性能**：静态无状态实例、直接虚调用；不在 Tick/对话热路径扫描程序集、目录或反射发现，不增加网络、文件 I/O、全世界扫描或无界缓存。

## 5. 有限施工顺序

### J11-G0：冻结 owner / consumer / gate 矩阵

- 记录 13 个方法、31 个生产调用点的渠道、线程、输入来源、输出消费者、现有 FeatureBridge gate 和副作用 owner。
- 区分三类入口：Prompt/rule 查询、normalize、执行/apply；禁止 normalize 顺手执行或查询接口写状态。
- 记录直接 owner 调用：领域内部自用可保留；AF 主体跨域调用必须经过 typed port。不能为了“全局零直连”让领域自己绕远路。
- 固定无新增 SyncData key、Saveable type、程序集、public ABI、默认开关和玩法变化的基线。

**退出门**：矩阵中每个调用点有唯一归属；任何无法归类的调用先解决设计，不先移动文件。

### J11a：internal contracts 归位

- 将 `TeamModulePorts.cs` 按 Policy/Gathering/Siege 拆为三个文件，归入 `src/AF.Contracts/Internal/TeamModules/`。
- 初始保持 `AnimusForge.Refactor.Modules` namespace 和全部签名，避免无收益的全仓 namespace 变化；是否改名另立有消费者证据的后续包。
- 更新实际 Compile/代码地图/路径型测试；禁止在旧路径留 Link、复制或同名接口。

**退出门**：13 个声明各一份；当前 31 个消费者全部编译；port 文件无具体玩法实现、状态、I/O 或 service lookup。

### J11b：Policy 薄桥

- 把 `PolicyModuleAdapter` 独立归位 `src/bridges/Policy/`，仍只转发四个接口。
- 核对 8 个主体调用点的目标 Hero/Character、chain、player proposal、NPC reply、content ref 和 failure reason。
- `PolicySystem/` 的 module Catalog、MCM retrieval、冻结 module IDs、active instance、save codec、execution/rollback 不动。
- `BuildActivePolicyDialogueContextForExternal` 当前兼容行为（包括可能为空）原样保留，不把搬家误写成功能恢复。

**退出门**：Policy port parity 与 `PolicyEffectModule.ContractTests --policy-all-modules-contract-only`、`--policy-history-only` 通过；禁用未来检索不影响现存实例的政策契约保持。

### J11c：Gathering 薄桥

- 把 `GatheringModuleAdapter` 归位 `src/bridges/Gathering/`。
- 核对 12 个调用点的 rule/context/normalize/apply 分层；apply 的 facts/notifications 仍由原调用方提交/展示。
- `NobleGatheringBehavior` 的资格、玩法状态、存档和通知内容不迁移。

**退出门**：五个方法逐参数/返回/ref/out 对照；Native/Scene/Courier 同类路径没有复制 normalizer 或双写事实。

### J11d：Siege / GCCZ 薄桥

- 把 `SiegeModuleAdapter` 归位 `src/bridges/Siege/`；AF 侧仍只调用 `AfGcczShoutBridge` 的四个接缝。
- 核对 11 个调用点的 selected、AgentIndex、direct reply、player text、speaker reply、rules 和 actionHandled。
- `conversation-siege`、活跃场景、prompt 注入/排除/bypass 与 GCCZ 独占后处理继续由现有 bridge/领域 owner 判定，typed port 不新增旁路。
- 本包不修改 `G:/AFMOD/GCCZ` 或其他工作树。若实际施工需要改 GCCZ 规则/提示词/共享源码，先取得精确跨工作树授权，再按 AFMOD 双份同步规则单独执行。

**退出门**：Siege 参数交换/selected 反转等有效变异继续会红；GCCZ disabled/failure 不产生副作用；无直接领域状态复制到 AF 主体。

### J11e：装配、目录和 FeatureBridge 核对

- `TeamModuleServices` 继续位于 GameAdapter composition，只创建三个无状态 adapter；不加入运行时替换、反射扫描或热卸载。
- InternalModuleDirectory 只登记实际已装配 capability；`Ready` 不等于 Campaign 可执行，也不绕过业务资格。
- 核对 ModuleFramework initialize/register/shutdown 顺序和 partial-start failure；无新生命周期 owner。
- 核对 `FeatureBridgeRuntime` 的 16 bindings / 12 wired / 4 declared-only，不把 internal adapter 虚报成新增正式 Bridge。

**退出门**：Campaign composition、ModuleFramework/API snapshot、Bridge bindings/runtime isolation 通过；缺依赖/不兼容/禁用时 fail closed 且无关模块可用。

### J11f：旧路径清理与整包验收

- 删除空的 `Refactor/Modules/TeamModulePorts.cs`、`TeamModuleAdapters.cs`；只在所有真实消费者和工具迁移后进行。
- 搜索重复 port/adapter、主体直接跨域 owner 调用、旧路径、冲突/TODO/HACK、无消费者接口和多余 compatibility shim。
- 更新 `af-internal-module-guide-v1.md`、范围图、代码地图、主台账、根 HANDOFF；生成本地制作组简版，不提交玩家/产物资料。

**退出门**：第 3 节全部完成，才标 `J11_OFFLINE_VERIFIED` 并进入 J12；不能因目录变整齐或 31 个调用能编译就提前完成。

## 6. 验证矩阵

### 每个子包

- `TeamModulePortParityTests`：完整 call expression、参数顺序、ref/out、异常、null/populated 输入；对应有效变异必须编译成功并在行为断言失败。
- 实际生产 consumer 搜索：每个跨域调用只走 `TeamModuleServices`；领域内部调用不被误改。
- `git diff --check`、旧/新路径唯一性、Compile 输入；相关 Debug 1.3/1.4 + Bootstrap。

### J11 最终候选

- Policy：`PolicyEffectModule.ContractTests --policy-all-modules-contract-only`、`--policy-history-only`；不运行真实 provider。
- Composition：`CampaignCompositionTests` 正常与 5 变异、`CompositionMatrixContractTests`、ModuleFramework/API tests。
- Bridge：BridgeBinding 16/23、BridgeFixture 10、BridgeRuntimeIsolation 12、Phase8 inventory/readiness。
- 三渠道：J09 default wiring、Scene postprocess/queue、Courier prompt/postprocess/domain commit、Native action/completion 的受影响回归。
- 兼容：Debug/Release × Bannerlord 1.3/1.4 + Bootstrap；四 DLL API/metadata；Persistence/Profile/Chunk/Identity；代码地图 recorded/working-tree。

构建、fixture、metadata 与真实 LIVE/SAVE/provider 必须分层记录。没有 Stage 授权时不运行 Stage 依赖 replay，也不用旧 Stage 证明当前源码。

## 7. 非目标

- 不改 Policy/Gathering/GCCZ 玩法、数值、Prompt 文案、MCM 检索、存档、调度、Harmony 或 UI。
- 不建设通用插件容器、程序集扫描、动态加载、热卸载或独立 DLL。
- 不开放 public `SceneSubmit` / `CourierSubmit` / `ActionExecute` / `MemoryWrite`；外部子 MOD 接口留 J14。
- 不迁 J12 Economy/Diplomacy/WorldMap、J13 Weekly/其他领域，也不清理所有 `Refactor/`。
- 不部署、打包、覆盖游戏、操作存档或自动恢复自动化。

## 8. 提交、协作与回滚

- 每个子包先意图检查点，再产品/测试提交，再文档回执；只暂存本包文件。
- 回滚按 `J11f → J11e → J11d → J11c → J11b → J11a` 定向 `git revert`；不 reset/rebase/force push。
- Policy/Gathering/Siege 可在文件互不重叠时并行，但 `TeamModuleServices`、代码地图、台账、最终构建由单一集成人负责。
- 发现其他作者修改同一 owner、远端分叉，或施工必须改变玩法/公开 API/存档/跨工作树范围时，暂停相关写入并重新决策。
