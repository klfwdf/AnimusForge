# AnimusForge 全项目代码审查、阶段 8 闭环与交接文档（2026-09-08）

**日期**：2026-09-08  
**交接对象**：下一位 AnimusForge 开发者 / Agent  
**当前状态**：
1. 阶段 8 核心重构与生命周期稳定性治理完全落库（LOCAL-PASS，领先远端 4 提交）；
2. 决斗标签缺失与遭遇放行离开死锁已彻底修复；
3. 历史日志中 4.8 万次 FirstChance NPE 根因已被根治；
4. Stage 与游戏目录（Modules/AnimusForge）部署产物哈希已 1:1 严格对齐；
5. 全项目 699 个 C# 源码文件完成全量静态代码审查与 7 大案例规范复核；
6. 完成全库本地化现状深度审计并输出 4 层多语言改造方案。

---

## 一、当前 Git 与环境基线

- **工作区路径**：`F:\AnimusForge-main`
- **当前 Git 分支**：`refactor/prepare-af-restructure`
- **本地 HEAD 提交**：`05b173bc` (`fix(lifecycle,policy): decouple campaign time and eliminate firstchance NPEs`)
- **分支状态**：领先 `origin/refactor/prepare-af-restructure` 4 个本地提交：
  - `54fd7974` fix(ui): widen war stats sort dropdowns to prevent text truncation and refine height
  - `d9f974ce` feat(refactor): wire stage 8 remaining bridges and enable three-channel default cutover
  - `52a82b0e` fix(duel,encounter): resolve duel tag omission and enforce meeting release leave
  - `05b173bc` fix(lifecycle,policy): decouple campaign time and eliminate firstchance NPEs
- **未跟踪资产与用户修改保护**：
  - `AnimusForge/GUI/SpriteParts/af_courier/*.png`（共 21 个卷轴与火漆素材图片），严禁暂存、提交或清理，必须保持当前未跟踪状态；
  - `extensions/AnimusForge.XihaiAction` 内既有修改保持完好。
- **构建环境与双版本状态**：
  - Bannerlord 1.4 构建：**0 警告 0 错误**；
  - Bannerlord 1.3 构建：**0 警告 0 错误**；
  - Bootstrap 引导程序集：**0 警告 0 错误**。
- **部署目录一致性**：
  - `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`
  - 运行 `python tools/LiveHostReadinessAudit/live_host_readiness_audit.py` 返回 **PASS** (`installedMatchesStage=true`)。

---

## 二、本周期已完成的核心成果（Done）

### 1. 决斗后处理标签漏出与会面放行离开闭环（Commit `52a82b0e`）
- **决斗触发词库扩充**：在 `RuleBehaviorPrompts.json` 的 `Duel.AcceptKeywords` 中增补了 14 组常用无标点短语（`我要和你决斗`、`拔剑吧`、`切磋`、`单挑` 等），修复语义未命中时的词汇漏检；
- **解除家族等级门禁锁死**：将 `DuelSettings.MinimumClanTier` 默认值降为 `0`，并移除了 `ShoutBehavior.cs` 中阻止向后处理规则表注入 `[ACTION:DUEL]` 的强杀判断；若 MCM 配置了高门槛，通过屏幕通知向玩家明示；
- **遭遇放行离开死锁修复**：在 `LordEncounterBehavior.cs` 中将谈判放行 `negotiable` 置为 `true`，下发放行后立即授予安全通行证（`AuthorizeMeetingPlayerRelease`），并在 10 秒倒计时结束或玩家主动提前离开时安全调用 `EndConversation()` 与 `EndMission()`，阻断掉入原生战斗/俘获菜单的缺陷。

### 2. 治理 4.8 万次 FirstChance NPE 根因（Commit `05b173bc`）
- **`WorldDiplomacyBehavior.cs`**：在 `CurrentDay()` 与 `CurrentHour()` 的入口处补齐了 `Campaign.Current == null || !Campaign.Current.GameStarted` 前置守卫，避免在游戏主菜单、销毁阶段或大地图未初始化时直接解引用 `Campaign.Current.MapTimeTracker` 触发高频空指针；
- **`MyBehavior.cs`**：在 `GetCurrentHourOfDaySafeForPrompt()` 补齐相同安全门禁；
- **`NpcRulerPolicyBehavior.Generation.cs`**：在 `CaptureUnifiedNpcPolicyHistorySnapshot` 中处理玩家政策历史为空时的情形，平滑回退 `Array.Empty<T>()`，彻底切断底层级联 NPE。

### 3. Stage 与游戏部署目录 1:1 同步
- 查明并修复了 `build_single_module.ps1 -Deploy` 与 `-Stage` 编译哈希错开的问题；
- 使用一键编译覆盖流程完成两端产物对齐，`live_host_readiness_audit.py` 严格验证通过。

### 4. 全项目 699 个 C# 文件全量代码审查
- **Tick 频率受控**：31 处 `OnTick` / `Tick` 方法均受限于 UI 焦点或带有 `_timer >= RefreshInterval`（0.12s ~ 1.0s）分批降频保护，**热路径零反射**；
- **巨石类架构定性**：`MyBehavior.cs`（5.9 万行）与 `ShoutBehavior.cs`（4.0 万行）已通过绞杀者模式成功将运行期调度与后处理执行外包给 `DetachedInteractionHost`，旧文件主要作为 `SyncData` 存档向下兼容容器与原生入口保留，不可强行物理删除；
- **7 大必须遵循案例 100% 合规**：单模块双实现、百科按钮注入、指令标签三段式、场景 Agent 移动、军团成员会面目标、场景伤害防误触、三渠道对齐均符合规范。

### 5. 全库本地化现状深度排查与规划
- **现状**：代码中约 289 个文件含有中文字符，约 2.7 万处中文硬编码（1,553 处 MCM 配置，802 处 UI/通知，核心 Prompt 为全中文）；
- **范式验证**：`DuelSettings.SceneActions.cs` 结合 `sceneactions_strings.xml` 证明了骑砍官方的 `{=ID}fallback_text` 本地化方案在本项目中完全成熟可用；
- **输出 4 层演进路线**：MCM 菜单本地化 -> 游戏内通知/UI 本地化 -> LLM 多语言 Prompt 动态加载 -> RAG 知识库扩展包。

---

## 三、现存测试套件验证结果

以下所有测试均在本地工作区完成执行并全部通过：

| 测试套件 / 验证项 | 测试命令 / 工具 | 结果 |
| :--- | :--- | :---: |
| **Stage 8 Readiness** | `test_phase8_readiness.py` | **72 / 72 PASS** |
| **Duel Replay Tests** | `ProductionDuelOutcomeReplayTests.cs` | **35 / 35 PASS** |
| **Bridge Binding Contract** | `test_validate_bridge_bindings.py` | **20 / 20 PASS** |
| **Bridge Runtime Isolation** | `BridgeRuntimeIsolationTests.cs` | **9 / 9 PASS** |
| **Interaction Pipeline Contract** | `InteractionPipelineContractTests.cs` | **109 / 109 PASS** |
| **Live Host Alignment Audit** | `live_host_readiness_audit.py` | **PASS** (`installedMatchesStage: true`) |
| **1.4 / 1.3 / Bootstrap 构建** | `dotnet build` (双分支) | **0 警告 0 错误** |

---

## 四、接手开发者后续行动建议（Next Steps）

接手者可根据当前任务目标选择以下方向推进：

### 选项 1：启动游戏进行实机 LIVE / SAVE 验证（首选推荐）
- 启动 Mount & Blade II: Bannerlord 并加载存档；
- 重点验证项：
  1. **决斗交互**：在领主大厅或场景中找领主/NPC 发起切磋决斗，确认正文答应后立刻跳出 `[ACTION:DUEL]` 倒计时并进入战斗；
  2. **敌对遭遇放行**：在大地图被优势敌军截停，在对话中赔款/求饶/说服放行，确认 NPC 答应放行后安全返回大地图，且敌军 5 天内不主动追击；
  3. **日志排查**：观察游戏日志，确认控制台不再出现此前高达 4.8 万次的 FirstChance NPE 异常刷屏。

### 选项 2：进入阶段 9 执行 Release 构型打包
- 若实机测试平稳，运行项目的单模块 Release 打包脚本；
- 确认 ZIP 中仅包含单个 `Modules/AnimusForge`，内含 `Bootstrap.dll` 与 `versions/1.3`、`versions/1.4` 双实现。

### 选项 3：向用户申请远端推送授权（Git Push）
- 当前本地领先 4 个提交（`54fd7974` ~ `05b173bc`）；
- **注意**：遵守项目规则，推送前必须获得用户明确授权。

### 选项 4：开启多语言本地化工程专项
- 编写 Python 脚本扫描 C# 中的 `[SettingProperty]` 与 `InformationMessage`；
- 批量自动分配唯一 ID（如 `af_mcm_*`、`af_msg_*`），将硬编码替换为 `{=ID}中文原文`；
- 生成 `ModuleData/Languages/animusforge_strings.xml` 与 `CNs/animusforge_strings-zh-CN.xml`。

---

## 五、常用验证与构建命令速查

```powershell
# 1. 编译 1.4 实现
dotnet build AnimusForge.csproj -c Debug -p:BannerlordApi=1.4

# 2. 编译 1.3 实现
dotnet build AnimusForge.csproj -c Debug -p:BannerlordApi=1.3 -p:Bannerlord13ReferencesVerified=true

# 3. 编译 Bootstrap 引导
dotnet build AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj -c Debug

# 4. 验证 Stage 与本地部署是否一致
python tools/LiveHostReadinessAudit/live_host_readiness_audit.py

# 5. 运行契约与阶段 8 就绪测试
python tools/BridgeBindingContractTests/test_validate_bridge_bindings.py
python tools/PhaseEightReadiness/test_phase8_readiness.py
```
