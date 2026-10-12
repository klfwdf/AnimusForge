# AnimusForge 中文硬编码本地化接力（Half C）

状态：ACTIVE / VERIFY

## 接力目标

继续完成玩家可见 UI 的中英本地化。代码保留英文 fallback，简体中文放入对应 `zh-CN` 资源，其他语言回退英文。只处理 UI 外壳、通知、Inquiry、按钮、编辑器标题/说明和固定结果消息。

Prompt 正文、模型输出、AFEF 事实、日志字段、协议标签、规则匹配值、存档键和动态业务正文必须保留原语义。

## 当前基线和协作规则

- 工作区：`F:/AnimusForge-main`
- 不要 reset、stash、批量覆盖或清理其他 Agent 的改动；先运行 `git status --short`。
- 当前主线已有大量已提交本地化切片；最近稳定验证包含 1.3/1.4/Bootstrap 构建、21 对资源契约和 10,280 项运行时文本解析。
- 全量内容契约目前只在既有 `content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownPrompt.zh-CN.json` 未审漂移处停止；不要刷新该 Prompt 基线。
- 外部 Half B 正在处理 PolicySystem 资源和后续接入；不要修改 `PolicySystem/**` 或其 `afpol_ui_strings`。
- 当前主线 Agent 正在收尾 WorldMap、Illustrator/NobleGathering、Terminal/Kingdom/DatabaseReload；不要修改这些路径。

## Half C 负责范围

### 1. Kingdom CivilWar UI

审查并本地化：

- `src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.cs`
- `src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Actions.cs`
- `src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Coup.cs`

只改玩家可见菜单、弹窗、通知和结果标题；保留叛乱资格、政治事实、Prompt、AFEF 和存档。

### 2. Weekly / World Bulletin UI

审查并本地化：

- `src/modules/AF.Module.Weekly/Materials/WorldBulletinCampaignMaterialPolicy.cs`
- `src/modules/AF.Module.Weekly/Generation/WeeklyReportRuntimeOwner.cs`
- Weekly 编辑器和公报结果通知中明确固定的 UI 外壳

报告正文、REPORT_BLOCK、TAGS、AFEF、生成协议和模型输出不翻译。

### 3. DialogueUI UI

审查并本地化：

- `extensions/AnimusForge.DialogueUI/src/DialogueUiSettings.cs`
- `extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml`
- 其他明确玩家可见的 DialogueUI 标题、按钮、输入提示和错误外壳

不要修改对话历史格式、Prompt、AFEF、模型正文或协议标签。

## 不得修改

- `PolicySystem/**` 及 `content/modules/PolicySystem/ModuleData/Languages/afpol_ui_strings*.xml`
- `content/modules/AF.Module.Prompt/**`、World Book、平行 `.en.json` / `.en-US.json` 资产
- 当前其他 Agent 的未提交文件
- 语言探测/加载、Bootstrap、程序集、存档键、构建/部署/打包脚本

## 实现要求

- 使用原生 `TextObject("{=AF_*}English fallback")` 或现有模块文本目录。
- 新 ID 使用 `AF_KINGDOM_*`、`AF_WEEKLY_*`、`AF_DIALOGUE_*` 前缀。
- 中英文键集合与占位符完全一致；保留变量名、数字格式、换行和按钮动作。
- 每个模块独立 focused commit；不要把其他 Agent 的文件带入提交。

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

完整内容契约的 GCCZ Prompt 漂移属于既有 VERIFY 项，不要刷新其 hash。

## 交付

回报修改文件、新增键数量、保留白名单、focused commit、资源/双版本验证结果和 `NOT-RUN` 实机/旧档项。使用 focused inverse commit 回滚，禁止 hard reset。
