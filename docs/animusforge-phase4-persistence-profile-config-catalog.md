# 阶段 4：Persistence / Profile / Config 目录

- 状态：ACTIVE（设计与纯 fixture）
- 范围：现有存档 key/type、chunk、JSON/PlayerExports/AFEF、配置快照和 profile 边界
- 本切片：不修改生产 C#、`SubModule.cs`、`.csproj`、构建/部署脚本、`SubModule.xml` 或游戏目录

## 已核对事实

- 当前独立工作树：`F:\AF测试重构`。
- 旧存档兼容仍是硬目标；`AnimusForge` 程序集身份、CampaignBehavior 类型和已有 `SyncData` key 不得因目录重构改变。
- 生产源码中已提取 95 个唯一字面量 `SyncData("...")` key，并确认 40 个源码文件包含符号形式的 `SyncData`/常量 key；chunk key 和 JSON 字典存储仍必须在后续切片补齐。
- 已建立 `docs/fixtures/phase4-persistence-profile-config/persistence-namespace-migration-catalog.json`：9 个逻辑 namespace、schema/lifecycle/owner、legacy key prefix 和 legacy-first 幂等迁移策略；这仍是目录契约，不代表运行时已迁移。
- 关键存档入口是 `MyBehavior.SyncData`、`CampaignSaveChunkHelper`、各领域 `CampaignBehavior.SyncData`；不能把 `MyBehavior` 整体搬迁。
- `AnimusForge/ModuleData` 与 `AnimusForge/CustomPrompts` 是静态内容候选；`PlayerExports`、日志、缓存和生成物必须分别分类，不能当作静态覆盖文件。

## 逻辑 owner 与兼容边界

| 逻辑 owner | 现有 owner/内容 | 目标 namespace（设计值） | 兼容要求 |
|---|---|---|---|
| `AF.Persistence.Memory` | `MyBehavior.cs` 的历史、每日/压缩记忆、概览、Persona、AFEF | `af.memory` | 继续由旧 `MyBehavior` facade 读写旧 key/type |
| `AF.Module.Conversation` | `MyBehavior.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs` | `af.conversation` | 三渠道历史语义不变；Courier 时序不合并 |
| `AF.Module.Reward` | `RewardSystemBehavior.cs` 及 RP/债务/生成物存储 | `af.reward` | 旧 key 和生成物恢复路径不变 |
| `AF.Module.Scene` | `SceneTauntBehavior.cs`、`ShoutBehavior.cs` 场景状态 | `af.scene` | Mission/Agent 状态不进入后台快照或新存档类型 |
| `AF.Module.Policy` | `PolicySystem/**` | `af.policy` | Policy receipt/history/effect 存档保持原类型和 key |
| `AF.Module.Siege` / `AF.Bridge.Gccz` | Siege、Village、CastleAftermath、GCCZ 状态 | `af.siege` / `af.bridge.gccz` | Bridge 使用独立 namespace，不写入参与模块私有 key |
| `AF.Module.Profile` | `DuelSettings`、AI/Guardrail/Prompt 配置、MCM | `af.profile` | reload 只影响未来请求；凭据不进存档/日志 |

这些 namespace 是目标逻辑边界，不在本切片直接替换现有 key。迁移必须采用：读取旧表示 → 校验 → 有界转换 → 单点发布；失败时保留旧表示并报告 namespace/key/schema。

## 配置快照规则

1. MCM/JSON reload 生成不可变 `ConfigSnapshot`，请求启动时捕获一次。
2. 进行中的 HTTP/TTS/辅助模型请求不读取可变全局配置。
3. URL、模型名、超时、token 上限和开关可以进入诊断摘要；API key、凭据值、私有完整路径和完整 prompt/回复不得进入存档、manifest、fixture 或普通日志。
4. `single-player`、`developer`、`safe-mode` 是静态 profile；SafeMode 只保留基础设施、GameAdapter、Persistence、Diagnostics 和恢复所需元数据。
5. 需要 Harmony、CampaignBehavior、持久化或后台队列的模块属于 `save-load-boundary` 或 `boot-only`，不能宣称安全运行时热切换。

## 下一步

- 补齐符号常量 key、chunk key、字典字段类型和 PlayerExports 读写者目录。
- 基于本目录建立纯 Persistence/Profile/Config contract runner。
- 通过 fixture 后，才进入 `AF.Contracts`/Foundation 的生产 DTO；仍保留旧 facade，不整体移动 `MyBehavior` 或 `ShoutBehavior`。


## 2026-08-30 增量验证：chunk 与 storage key 对账

本轮没有修改生产存档 owner，而是把现有源码事实纳入纯 fixture/validator：

- 95 个 `SyncData("...")` 字面量 key 与 17 个 catalog owner 文件完全一致；
- 13 个 `SaveChunkedString`/`LoadChunkedString` 基础 key 与 38 个 `FlattenStringDictionary` 基础 key 已登记；
- `CampaignSaveChunkHelper` 的 `12000` 字节 chunk 上限、`240` 字节 legacy inline fallback、`262144` chunk count 上限和四类 metadata prefix 已对账；
- 真实 helper 回放覆盖 UTF-8 多字节边界、缺失 chunk、超限 count、legacy inline fallback、字典展开/恢复、损坏字典值隔离和 `SafeSyncData` 异常隔离。

证据：`python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py` 输出 `literalKeys=95 symbolicSources=40 chunkedStringKeys=13 flattenedDictionaryKeys=38 ... PASS`；`tools/PersistenceChunkReplayTests` 输出 `... PASS`。真实旧存档、SaveSystem typed binding 和游戏内读档仍为 `NOT-RUN`，下一项是 typed `SyncData` ref 绑定与 legacy save fixture。


## 2026-08-30 增量验证：typed SyncData ref 绑定

`syncdata-binding-catalog.json` 记录当前生产源码中 95 个 exact literal key 的 121 次 `ref` 绑定，覆盖 8 类静态类型：scalar、`List<string>`、`Dictionary<string, string>`、`Dictionary<string, int>`、`Dictionary<string, float>` 和 `TroopRoster` 等。validator 会按源码行、owner、ref 名和类型逐项对账，并拒绝同一 key 在 save/load 分支出现类型漂移。

当前证据只证明源码绑定和纯 fake `IDataStore` 行为，不宣称真实 TaleWorlds SaveSystem、旧存档加载或 SafeMode runtime 已通过；下一项是 legacy-first 缺失字段/未知字段/失败回滚纯迁移 fixture。


## 2026-08-30 增量验证：legacy-first / SafeMode 纯迁移契约

新增 `legacy-first-safe-mode-migration-cases.json` 与 `tools/PersistenceMigrationContractTests.py`，以 typed binding catalog 的代表性类型构造纯数据场景。它验证：缺失可选 key 不凭空生成数据；已知 key 类型不一致时不发布新表示并保留 legacy；chunk 计数/片段损坏时 fail-closed；未知字段在 SafeMode 中仍可见；第二次迁移输出与第一次一致；legacy key 不删除。

这是迁移策略的纯 contract 证据，不是 TaleWorlds SaveSystem 或旧存档运行时证据。真实旧存档、程序集加载后的序列化类型、SafeMode runtime 和游戏内读档仍为 `NOT-RUN`。


## 2026-08-30 增量验证：存档身份基线对账

`tools/PersistenceIdentityAudit.py` 对比当前工作树与基线 `d4cb1467376c6e923f4295dcefc7878c11dbc7c1`：`SyncData` 绑定 `99/99`，CampaignBehavior 类型 `35/35`，无 added/removed key/type 或行为类型；`AnimusForge/SubModule.xml` 仍只加载 `AnimusForge.Bootstrap.dll`，主模块 Id/Name 仍为 `AnimusForge`。

该结果证明源码和发布身份边界未漂移，不等同于真实旧存档加载通过；真实 SaveSystem/旧存档/SafeMode runtime 仍为 `NOT-RUN`。
