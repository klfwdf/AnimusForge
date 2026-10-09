# AnimusForge 中文硬编码本地化接力（Half B）

状态：ACTIVE / VERIFY

## 目标

继续把玩家可见 UI 的中文硬编码改为 Bannerlord 原生本地化：代码保留英文 fallback，简体中文放入对应 `zh-CN` XML；其他语言回退英文。只处理玩家可见 UI、通知、弹窗、MCM/编辑器文案和固定结果消息。

Prompt 正文、模型默认内容、AFEF 事实、日志机器字段、协议标签、规则匹配值、存档键和动态业务正文必须保留原语义，不要改写。

## 当前基线

- Git 根：`F:/AnimusForge-main`
- 分支：`main`
- 最近已验证基线：`f0cefff34`；工作区可能同时有其他 Agent 的未提交修改，先读取 `git status`，不要 reset、stash 或覆盖。
- 当前资源契约：21 对中英文资源。
- 盘点工具当前输出：85,357 条中文记录；其中保守分类 `player_visible_ui` 约 2,709 条，包含仍需审查的动态/内部上下文，不能按数量直接视为 UI 缺口。
- 已有验证：真实 DLL 本地化运行时 10,076 项通过；1.3/1.4/Bootstrap 官方构建通过，各实现 348 个既有警告、0 错误；Coup seam 和内存回归通过。
- 全量内容契约目前被既有未审漂移拦截：`content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownPrompt.zh-CN.json`。不要刷新该基线；它属于 Prompt/世界书并行资产。

## 本接力 Agent 的范围（Half B）

### 1. PolicySystem 玩家可见 UI

审查并本地化以下文件中的玩家可见通知、Inquiry、编辑器标题/按钮和结果消息：

- `PolicySystem/Core/CustomPolicyBehavior.Management.cs`
- `PolicySystem/Core/CustomPolicyBehavior.Generation.cs`
- `PolicySystem/Core/CustomPolicyBehavior.Lifecycle.cs`
- `PolicySystem/Core/CustomPolicyBehavior.Effects.cs`
- `PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs`

只改 UI 外壳；不要改政策 Prompt、效果 JSON、数值协议、日志或存档。

### 2. Kingdom CivilWar 残余 UI

审查并本地化：

- `src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.cs`
- `src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Actions.cs`
- `src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Coup.cs`

只处理玩家可见 Inquiry、通知、按钮和结果消息；保留叛乱规则、Prompt、事件事实和存档结构。

### 3. Weekly/World Bulletin 残余 UI

审查并本地化：

- `src/modules/AF.Module.Weekly/Materials/WorldBulletinCampaignMaterialPolicy.cs`
- `src/modules/AF.Module.Weekly/Generation/WeeklyReportRuntimeOwner.cs`
- 相关 Weekly 编辑器或结果通知（仅玩家可见部分）

保留报告正文、REPORT_BLOCK、TAGS、AFEF 和生成协议；只翻译标题、按钮、状态和固定外壳。

### 4. Knowledge 残余 UI

先审查盘点结果，再处理确属玩家可见的残余：

- `src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs`
- `src/AF.GameAdapter.Bannerlord/UI/Editors/MemoryEditorProjection.cs`
- `src/modules/AF.Module.Knowledge/WorldEntityRetrievalService.cs`

Knowledge 条目正文、关键词、RAG 内容、Prompt、规则 ID 和动态事实不是 UI 外壳，不要翻译。复用 `afknowledge_strings.xml` / `afknowledge_strings-zh-CN.xml`。

### 5. DialogueUI 残余 UI

审查并处理：

- `extensions/AnimusForge.DialogueUI/src/DialogueUiSettings.cs`
- `extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml`
- 其他明确玩家可见的 DialogueUI 标题、提示、按钮

不要修改对话历史、Prompt、AFEF 或模型输出正文。

## 不得触碰的并行范围

当前已有 Agent 正在处理，除非先收到明确交接，不要修改：

- `src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs` 与 WorldMap 文件
- `extensions/AnimusForge.Illustrator/**` 与 `NobleGatheringBehavior`
- `TerminalSettingsRegistry.cs`、`DatabaseReloadController.cs`、`KingdomFactionTab.cs`
- 任何当前 `git status` 中由其他 Agent 修改的文件
- `content/modules/AF.Module.Prompt/**CustomPrompts/**`、World Book 和平行 `.en.json` / `.en-US.json` 资产

## 实现约束

- 代码使用 `TextObject("{=AF_*}English fallback")` 或现有模块文本目录。
- ID 使用稳定模块前缀：`AF_POL_*`、`AF_DIP_*`、`AF_KINGDOM_*`、`AF_WEEKLY_*`、`AF_KNOWLEDGE_*`、`AF_DIALOGUE_*`。
- 中英文键集合与占位符必须完全一致；变量名不要使用 Bannerlord 保留词（例如 `MATCHED`、`UNMATCHED`）。
- 不修改语言探测/加载逻辑、Bootstrap、程序集名、存档键、双实现输出结构、部署脚本或打包流程。
- 每个模块单独 focused commit；不要把其他 Agent 的修改一起提交。

## 验证命令

```powershell
python tests/content/J15ContentContractTests/LocalizationResourceContractTests.py
git diff --check

powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build\build_single_module.ps1 `
  -ProjectRoot . `
  -BannerlordRoot "F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" `
  -Bannerlord13ReferenceDir .\.tmp\build_check\1.3 `
  -Bannerlord14ReferenceDir .\.tmp\build_check\1.4 `
  -Configuration Debug

powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\content\J15ContentContractTests\LocalizationRuntimeTests.ps1 `
  -ReferenceDir .\.tmp\build_check\1.4
```

完整 `python tests/content/J15ContentContractTests/run.py` 目前会在既有 GCCZ Prompt 中文漂移处停止；保留失败证据，不刷新其基线。

## 交付格式

回报：

1. 修改文件和新增资源键数量。
2. 明确保留的 Prompt/日志/协议/动态正文白名单。
3. focused commit SHA。
4. 资源契约、`git diff --check`、1.3/1.4 构建和运行时测试结果。
5. 未执行的实机、完整战役、旧存档验证标记为 `NOT-RUN`。

建议回滚点：每个模块的 focused inverse commit；禁止 hard reset 或重写历史。
