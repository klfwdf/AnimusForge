# 本次提交说明：生图/政变/对话UI/复仇处决四功能并入 AF 本体 + 审查修复

日期：2026-09-29
分支：`codex/af-main-refactor-continuation-20260831`
基线：`746a3341`（本次提交前工作树状态 + 本批新增文件）

## 一句话

把原来四个独立功能模块（AI 生图 Illustrator、武装政变 Coup、对话界面 DialogueUI、复仇公开处决 Vengeance）的源码与资源并入 `AnimusForge.dll` 单模块体系，由统一宿主代理生命周期；同时带入内战/派系系统的实现本体、场景表现层新文件，以及整合审查发现的一批修复。

## 本次提交涉及的内容

### 1. 四功能整合主体

- `AnimusForge.csproj`：Illustrator / DialogueUI / Coup 扩展源码与 XihaiAction 核心/运行时源码链回主程序集；`Vengeance/Source/SubModule.cs` 保持排除（避免内置 DLL 出现第二个模块入口）；新增 `System.Drawing` 引用。
- 新增 `IntegratedModuleHost.cs`：统一宿主，暴露 `OwnsIllustrator/OwnsDialogueUi/OwnsCoup/OwnsVengeance`，代理 Start / RegisterCampaign / InstallDialoguePresentation / Tick / Shutdown。
- 新增 `VengeanceRuntimeBridge.cs`：AF 侧复仇宿主——claim 内置实现、注入任务行为、注册战役行为、关闭清理。
- `SubModule.cs`、`ApplicationTickComposition.cs`：主模块生命周期接入上述宿主。
- 双模块互斥：各扩展 `SubModule` 增加 `HostOwnsModule()` 反射探测——AF 已托管时独立模块自动静默，避免重复注册/打补丁。

### 2. 复仇处决（`Vengeance/` 共享源码全集，新增未跟踪→本次入库）

- 同一套源码同时编译进 AF 1.3/1.4 与独立 `RichExecutions.dll`；保持 `RichExecutions.Campaign.*` 命名空间、`_rex_*` 存档键、`Configs/RichExecutions` 预设与逻辑模块 ID 不变。
- 独立版入口 `SubModule.cs` 增加 `IsHostedByAnimusForge()` 探测与三层拦截，反射失败按已托管处理（失败关闭）。
- `ExecutionContinuation`：去掉对 AF 类型的硬依赖，改由宿主注入 `EscortedHeroesProvider` 回调；任务退出前只捕获候选人 ID，稍后主线程按 ID 重解析；连续行刑换受刑者经 `BuildRequest()` 重算证据/合法性/费用。
- 资源入正式清单：`vengeance_pack0.tpac`、3 个 GUI prefab、`vengeance_items.xml`、中英语言文件（共 7 项）。

### 3. 生图（Illustrator）

- 扩展源码整体并入；`DiskImageCacheManager` **取消缓存自动搬迁**，缓存根固定在 `Documents/Mount and Blade II Bannerlord/AnimusForge/IllustratorCache`（原生产路径，旧图原位可读）。
- 会话/百科补丁类新增 `Reset()`，`Shutdown` 解绑 Harmony 后复位 `_patched` 与会话缓存，同进程重载可安全重建。

### 4. 对话 UI（DialogueUI）

- 运行时模块根优先解析 AF 宿主目录，扩展独立存在时回退原 manifest 扫描；`SubModule` 拆出静态 Start/Shutdown/InstallPresentation 供宿主调用。
- 入库 GUI 资源：羊皮纸 prefab ×7、`afdui_*` 分图、`src/Scene/` 会话/轮盘源码、`DialogueUiSettings.cs`，以及素材生成脚本与设计稿。

### 5. 政变（Coup）

- `CoupCaptivityBehavior`：拘押存档解析失败时保留原始 JSON 原样写回（`_saveValid` 门禁），不再被空状态覆盖；损坏期间拘押入口禁用。
- `CoupGuards` / `SettlementEntryTroopSelectionBehavior` 新增 `Reset()`，`SubModule.Shutdown` 复位全部注册标志/委托/能力状态。
- 携带既有政变源码与 `CoupRebellionBridge`（政变后同王国封臣叛乱桥）。

### 6. 关联新文件（同属本批未跟踪内容）

- **内战/派系**：`src/modules/AF.Module.Kingdom/CivilWar/`（状态机 owner、port 适配器、战役行为、存储）+ `src/AF.Contracts/Internal/TeamModules/ICivilWarModulePort.cs` + `WarStats/AfWarStatsFactionKingdomVM.cs`。周推进派系生成/诉求应答/内战爆发/结局（篡位、镇压、转分裂建国）；LLM 后处理标签 `[A:CIVIL_FACTION:*]`；存档键 `_af_kingdom_civil_war_v1`。注意：已提交的 `TeamModuleServices.cs`/`MyBehavior.cs` 已引用这些文件，**不随本批入库则 HEAD 无法编译**。
- **场景表现层**：`src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.Scene*.cs` ×7、`src/AF.Contracts/Internal/TeamModules/ICivilWarModulePort.cs` 同批。
- 其他：`ExecutionAddressLlm.cs`、`MyBehavior.DialogueHistoryDelete.cs`、`extensions/AnimusForge.XihaiAction/src/` 新增 2 文件。
- 测试：`tools/ScenePresentationPolicyTests/`、`tests/ExecutionSpeechLineParserTests/` 等边界测试源码。

### 7. 移除项

- `AnimusForgeUniqueCosmeticItemBehavior`：删除"进游戏自动扫描并删除西海衣服/鞋帽"的整套机制及其 `_af_xihai_legacy_equipment_cleanup_v1` 存档键（旧存档中该键将被忽略）；唯一头饰投放与迁移查找保留。

## 存档与身份契约

不变：`_rex_*`、`af_coup_detentions_v1`、`_afCoupSession_v1`、`_afCoupRebellionBridge_v1`、`_afCoupOutcomeBridge_v1`、`_af_kingdom_civil_war_v1`、`af_xihai_daimao_head` 投放键、`Configs/RichExecutions`、模块 ID `RichExecutions`。
有意移除：`_af_xihai_legacy_equipment_cleanup_v1`（机制删除，旧档安全忽略）。

## 验证状态

- 官方双版本 Debug 构建：1.3 / 1.4 实现各 336 警告 0 错误，Bootstrap 0/0；独立 `RichExecutions.dll` 15 警告 0 错误。
- `content/content-map.json`：195 项映射全部存在、无重复目标（复仇 7 项、DialogueUI 资源齐）。
- `git diff --check` 通过；code-map `verify_code_map.py` PASS（795 锚点，基线 `803a09d`）。
- **未验证**：游戏内实机、旧存档加载、AF+独立模块同启运行时互斥、Stage/部署/打包、真实玩家缓存行为。

## 不随本次提交的内容

本地 scratch/预览/生成产物（`work/`、DialogueUI `outputs/`与`work/`、`tools/*/.generated/`、`af_courier` 投影副本、根目录杂项约 600 文件）不入库；266 个历史提交是否推送另行决定（历史含私密资料的既有警示仍有效）。
