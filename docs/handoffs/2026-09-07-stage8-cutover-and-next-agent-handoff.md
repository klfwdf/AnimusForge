# AnimusForge 阶段 8 重构接续与交接文档（2026-09-07）

**日期**：2026-09-07  
**交接对象**：下一位 AnimusForge 开发者 / Agent  
**当前状态**：阶段 8 核心重构与接线完成（VERIFY / LOCAL-PASS），三渠道默认接管与平滑降级已就绪，等待实机 LIVE/SAVE 验收与清理收尾。

---

## 一、当前 Git 与环境基线

- **工作区路径**：`F:\AnimusForge-main`
- **当前 Git 分支**：`refactor/prepare-af-restructure`
- **本地 HEAD**：`d9f974ce` (`feat(refactor): wire stage 8 remaining bridges and enable three-channel default cutover`)
- **分支状态**：领先 `origin/refactor/prepare-af-restructure` 1 个本地提交（包含本次完成的 13 个文件修改，均已验证）。
- **未跟踪文件保护**：`AnimusForge/GUI/SpriteParts/af_courier/*.png`（共 21 个卷轴与火漆素材图片），严禁暂存、提交或清理，必须保持未跟踪状态。
- **构建环境**：.NET 6 / .NET 10，Visual Studio MSBuild / dotnet CLI。

---

## 二、本轮已完成的工作（Done）

### 1. 阶段 8 剩余 3 组 Bridge 生产接线落地（10 -> 13 Wired）
- **`runtime-game-adapter`**：在 `InteractionComponentSafePatch.cs` 的 Harmony Patch 首部植入 `FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.RuntimeGameAdapter)` 门禁。
- **`scene-duel`**：在 `DuelBehavior.cs` 与 `DuelBehavior.Outcomes.cs` 的 `TryQueueDuel` 前置拦截中接入 `IsSceneDuelBridgeEnabled()`。
- **`host-runtime`**：在 `CampaignTickDiagnosticsPatch.cs` Harmony 诊断注入首部植入 `FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.HostRuntime)` 门禁。
- **契约与元数据同步**：`FeatureBridges.json`、`FeatureBridgeRuntime.cs`、`bridge-binding-manifest.json`、`full-domain-readiness-catalog.json` 全部同步升级，形成 `16 total = 13 wired + 3 declared-only`。

### 2. 三渠道默认切流向统一 DetachedInteractionHost（Three-Channel Default Cutover）
严格遵循 `docs/free_conversation_scene_shout_alignment.md`，实现三渠道对齐与统一动作/记忆执行：
- **Native 自由对话**：`ShoutBehavior.SubmitNativeConversationTextForExternalAsync` 默认路由到 `SubmitNativeConversationRefactorOptInCoreAsync`；若前置未满足平滑安全回退旧版 `SubmitNativeConversationTextInternalAsync`。
- **Scene Shout 场景喊话**：`ShoutBehavior.HandleGroupResponsePerHeroIndependent` 在后台 Task 中通过主线程捕获 `InteractionEnvelope`，对接 `SubmitSceneShoutRefactorOptInForExternalAsync`，统一动作提交与记忆落库，并通过 `sceneShoutDetachedCommitted` 标志守卫跳过旧版重复动作执行与历史记录，避免双重扣减。
- **Courier 信使回信**：`CourierDeliveryBehavior.PrepareAndGenerateCourierReplyOffMainThreadAsync` 在 Bridge 启用时接入 `SubmitCourierReplyRefactorOptInForExternalAsync`，主线程安全终结状态并推进 session；若发生异常平滑回退 `GenerateNpcReplyAsync`。

### 3. 全量测试与双版本构建 100% 通过
- `dotnet build AnimusForge.csproj -c Debug -p:BannerlordApi=1.4` -> 0 警告 0 错误
- `dotnet build AnimusForge.csproj -c Debug -p:BannerlordApi=1.3 -p:Bannerlord13ReferencesVerified=true` -> 0 警告 0 错误
- `dotnet build AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj -c Debug` -> 0 警告 0 错误
- `validate_bridge_bindings.py` -> PASS (`wired=13, declaredOnly=3, configEnabled=13`)
- `test_validate_bridge_bindings.py` -> 20/20 PASS
- `BridgeRuntimeIsolationTests` -> 9 场景 PASS
- `PhaseEightReadinessTests` -> 72/72 PASS
- `InteractionPipelineContractTests` -> 40 项基础 + 69 项 Detached Commit 契约全部 PASS
- `DuelDispatchContractTests` -> 16/16 PASS

---

## 三、还剩什么没做（To-Do / 剩余待办清单）

当前完成的是**离线工程与代码层面的接线、切流和单元契约闭环**。距离“完全重构交付与发布”仍有以下 5 项关键任务：

### 1. 提交推送与协作同步（等待用户指令）
- 当前本地提交 `d9f974ce` 尚未推送到 `origin/refactor/prepare-af-restructure`；
- 根据项目规范，**严禁未经用户明确授权执行 `git push`**。后续接手者若需推送，先向用户确认。

### 2. 真实游戏环境验证与部署（LIVE / SAVE 验收）
- **重新部署 Stage 到游戏目录**：使用项目官方一键编译/覆盖流程，将最新的 1.3 / 1.4 DLL 及 Bootstrap 部署到游戏 Modules 目录；
- **游戏启动与生命周期**：在实际 Bannerlord 游戏环境中加载，验证 1.3.x 和 1.4.x 双版本识别与单模块加载；
- **三渠道实机对话体验**：
  - Native 自由对话（流式显示、阶段回调、开场白）；
  - Scene 场景喊话（多人环境、跟随/带路、动作执行、AFEF 记忆落库）；
  - Courier 信使（写信、信使派遣、收信回信、状态推进）；
- **经济与副作用验证**：实机验证金币转移、物品交易、债务生成/解除，确认无二次扣款且存档/读档（Save/Load）后数据一致。

### 3. FirstChance 异常与防御性守卫治理
- 历史日志审阅（参见 `docs/handoffs/2026-09-07-firstchance-root-cause-handoff.md`）发现 `FirstChance count=48220`：
  - 核心根因：部分 Behavior（如 `WorldDiplomacyBehavior.CurrentHour()` / `CurrentDay()` 等）在非 Campaign 活动期或初始化/销毁期直接读取 `CampaignTime.Now` / `Clan.PlayerClan`，触发被捕获的 NPE；
  - `CaptureUnifiedNpcPolicyHistorySnapshot()` 缺失玩家政策历史时抛出异常；
  - 待办：为相关 Tick 热路径与访问点添加生命周期状态门禁（`Campaign.Current?.MapTimeTracker != null`），杜绝反复抛出再捕获异常。

### 4. 阶段 8 清理候选审阅（Cleanup Review）
- 参考 `docs/phase8/full-domain-readiness-catalog.json` 中的清理候选列表；
- 随着三渠道默认接管与 Bridge 接线完成，部分旧有的独立私有 helper 已无静态调用者；
- 需逐 symbol 确认是否存在反射调用、外部补丁或存档兼容依赖；经确认完全冗余且用户授权后，方可执行定向废代码清理。
- **牢记底线**：严禁删除 `LegacyChannelInteractionFacade` 等活跃 facade，保持存档 key/type 与 MCM 兼容。

### 5. 阶段 9 最终收敛与 Release 发布
- Release 构型编译与 ZIP 离线打包；
- 双实现 marker 与 hash 校验；
- 制作组联调验收通过后，按既定流程合并并准备发布。

---

## 四、接手开发者快速上手与核心指令

### 1. 验证编译与构建
```powershell
# 编译 Bannerlord 1.4
dotnet build AnimusForge.csproj -c Debug -p:BannerlordApi=1.4

# 编译 Bannerlord 1.3
dotnet build AnimusForge.csproj -c Debug -p:BannerlordApi=1.3 -p:Bannerlord13ReferencesVerified=true

# 编译 Bootstrap 引导程序集
dotnet build AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj -c Debug
```

### 2. 运行核心契约与隔离测试
```powershell
# Bridge 绑定校验
python tools/BridgeBindingContractTests/validate_bridge_bindings.py

# Bridge 隔离测试
dotnet run --project tools/BridgeRuntimeIsolationTests/BridgeRuntimeIsolationTests.csproj

# Phase 8 完整域就绪测试（72 项）
dotnet test tests/PhaseEightReadiness/PhaseEightReadiness.csproj
```

### 3. 必须遵循的铁律
- **绝不擅自 push**：必须等待用户明确授权；
- **双版本兼容**：任何代码改动必须同时保证 `BannerlordApi=1.3` 和 `BannerlordApi=1.4` 编译 0 警告 0 错误；
- **安全回退机制**：所有新通道切流必须保留 Fail-Closed 平滑降级到旧版实现；
- **保护未跟踪素材**：不要暂存或修改 `AnimusForge/GUI/SpriteParts/af_courier/*.png`。
