# AnimusForge.Illustrator — 技术架构与维修交接文档 (HANDOFF)

> **更新日期**：2026-09-16
> **活动分支**：`codex/af-main-refactor-continuation-20260831`
> **维修前检查点**：`02b4c5b3 chore: checkpoint Illustrator before lifecycle and fidelity repairs`
> **第四轮审计修正点**：`fd5657e4`
> **第五轮提交**：`1698b431`（重绘变体 + 相似度滑块）
> **第六轮本地修复**：`9dda2fa2`（四项审查修复）；检查点 `cf5ccc0`；113 checks / 0 failures，未部署，详见第 13 节。下文此前的部署与实机记录不代表本次修复已实机验收。
> **子模块源码**：`extensions/AnimusForge.Illustrator`
> **目标模块 ID**：`AnimusForge_Illustrator`
> **目标程序集**：`AnimusForge.Illustrator.dll`
> **本机游戏**：Native `v1.4.8`（changeset `119303`）
> **本轮状态**：第四轮实机反馈修复已完成并部署（见第 11 节）：场景感知纠正（野外/海上/军团/围城阵前/囚禁）、人物处境状态注入、台词只进导演不进生图、纹章图集定为 4×4、周报主题扩至 10 类+场景名词兜底、导演超时 120s、空回复诊断不自动重试。回归基线 90 checks / 0 failures。实机验收持续进行中。

---

## 1. 当前架构边界

`AnimusForge.Illustrator` 是独立 Bannerlord 子模块，不属于统一 `AnimusForge` Bootstrap 版本装载链。它通过 `SubModule.xml` 直接加载 `AnimusForge.Illustrator.dll`，并依赖已安装的 `AnimusForge` 主模块提供 MCM/正文 API 配置桥接。

### 线程与生命周期边界

```text
游戏主线程（唯一允许触碰 Bannerlord / Gauntlet 对象）
  ├─ 冻结 Hero / Campaign / Screen / Settings / UI 上下文
  ├─ 创建、注册、替换、释放 Texture 与 Sprite
  ├─ 更新 ViewModel、挂载/移除 Gauntlet Layer
  ├─ 检查 Campaign、Screen、Scope revision、CancellationToken
  └─ 每帧最多提交 2 个完成回调，待处理队列上限 32

后台线程
  ├─ HTTP / 图片 URL 下载
  ├─ 提示词 LLM 扩写与 JSON 解析
  ├─ PNG 字节读写、磁盘缓存保存与回收
  └─ 不直接创建纹理、不直接写 UIResourceManager、不直接改 ViewModel
```

核心实现：

- `src/Core/IllustratorRuntime.cs:53-183`：`IllustratorRuntime` 捕获主线程 ID，提供 `AssertMainThread`、`CaptureOptions`、`Post`、`Start<T>` 与 worker/completion 限流。
- `src/Core/IllustratorRuntime.cs:186-258`：`IllustrationScope` 记录 Campaign、TopScreen、CampaignKey、类别、request revision 与取消令牌；窗口关闭、切屏或换档后，旧结果会被拒绝。

---

## 2. 已核实源码坐标

所有路径均相对 `F:\AnimusForge-main`，行号为当前工作区 1-indexed 坐标。

| 路径 | 行号 | 符号 | 当前职责 |
| :--- | ---: | :--- | :--- |
| `extensions/AnimusForge.Illustrator/src/SubModule.cs` | 21-74 | `SubModule` | 初始化运行时、挂载周报/百科/对话 Patch、每帧提交完成回调、卸载时清理 |
| `extensions/AnimusForge.Illustrator/src/SubModule.cs` | 77-115 | `IllustratorCampaignBehavior` | `OnSessionLaunched` 中设置 `Campaign.Current.UniqueGameId` 并注册营地画廊入口 |
| `src/Settings/IllustratorSettings.cs` | 39-207 | MCM 开关 | 总开关、多模态、离屏渲染、百科/对话入口与缓存上限 |
| `src/Settings/IllustratorSettings.cs` | 215-222 | `QueueInjectedButtonRefresh` | 设置变更通过主线程队列刷新已注入按钮显隐 |
| `src/Settings/IllustratorSettings.cs` | 328-483 | `RequestModelListFetch` / `FetchModelListAsync` / `ApplyFetchedModels` | 主线程发起模型拉取；后台只做 HTTP 与模型缓存；结果回主线程更新 Dropdown/MCM |
| `src/Engine/ScreenCaptureHelper.cs` | 56-126 | `CaptureActiveWindowBase64` | 只允许截取当前进程前台窗口，非本进程窗口直接返回 `null` |
| `src/Engine/ScreenCaptureHelper.cs` | 295-418 | `TriggerTableauViewSave` / `WaitForOffscreenFileAsync` | 主线程触发 Tableau 落盘；后台等待并读取临时 PNG |
| `src/Engine/ScreenCaptureHelper.cs` | 772-804 | `SwapRedAndBlueInBitmap` | 对离屏导出位图执行 R/B 通道互换 |
| `src/Engine/GauntletTextureLoader.cs` | 22-150 | 动态 Sprite 注册/释放 | 所有访问均断言主线程；同名动态 Sprite 会先释放旧纹理；关闭/换档可全局释放 |
| `src/Engine/GauntletTextureLoader.cs` | 198-224 | `RuntimeIllustrationSprite` | 持有并释放底层 `PlatformTexture` |
| `src/Engine/DiskImageCacheManager.cs` | 64-120 | campaign/category 路径与加载 | 按 `IllustratorCache/<campaign>/<category>` 查找，不回退到其他存档 |
| `src/Engine/DiskImageCacheManager.cs` | 122-155 | `SaveImage` | 保存 PNG + 同名 JSON 元数据，并按当前存档执行数量上限 |
| `src/Engine/DiskImageCacheManager.cs` | 167-214 | `GetAllCachedIllustrations` | 显式刷新当前存档缓存；支持无元数据 PNG 兜底；结果缓存按 CampaignKey 隔离 |
| `src/Engine/DiskImageCacheManager.cs` | 232-249 | `SetDefault` | 同一 `SubjectKey + Category` 内设置默认图，不跨分类覆盖 |
| `src/Engine/DiskImageCacheManager.cs` | 252-291 | `DeleteItem` / `EnforceLimit` | 删除移入 `_trash/<campaign>`；超限先回收非默认旧图，仍超限才回收默认图 |
| `src/Context/HeroVisualExtractor.cs` | 90-157 | `HeroVisualExtractor.Extract` | 冻结角色身份、文化、年龄、家族、传记、特质、技能、装备与坐骑事实 |
| `src/Context/ConversationContextExtractor.cs` | 71-299 | `ExtractFromCurrentConversation` | 冻结对话双方、当前语句、围城/敌对状态、坐骑站位与环境 |
| `src/Context/WeeklyReportContextExtractor.cs` | 74-104 | `ExtractFromWeeklyReport` | 从周报标题/副标题/正文提取事件主题、事件地与主角档案 |
| `src/Core/VisualDirectorEngine.cs` | 51-85 | `ExpandToDetailedPromptAsync` | 优先 LLM/多模态扩写，失败时走规则保底 |
| `src/Core/VisualDirectorEngine.cs` | 286-378 | `SynthesizeRuleBasedPrompt` | 不再输出泛化模板，末尾强制保留完整游戏上下文事实 |
| `src/Core/UniversalOpenAiImageClient.cs` | 39-128 | `GenerateImageAsync` | 生图请求与 `/chat/completions` 自动回退；请求带取消令牌 |
| `src/Core/UniversalOpenAiImageClient.cs` | 446-461 | `DownloadImageBytesAsync` | 图片 URL 下载遵循请求取消令牌 |
| `src/UI/Overlays/IllustrationCardPopup.cs` | 52-155 | 百科/对话弹窗入口 | 主线程截图与上下文冻结；先查当前存档默认缓存，未命中才生成 |
| `src/UI/Overlays/IllustrationCardPopup.cs` | 157-328 | 生图请求 | 背景任务只生成与保存；完成回调在主线程发布纹理和 VM 状态 |
| `src/UI/Overlays/IllustrationCardPopup.cs` | 331-395 | `PublishImage` / `Close` | 替换前释放旧 Sprite；关闭时取消 scope、释放纹理、移除 layer、清空活动实例 |
| `src/UI/Overlays/MovableGauntletLayer.cs` | 61-182 | 拖动/缩放 Tick | 标题栏拖动、右下角缩放、UI scale 感知鼠标位移 |
| `src/UI/Gallery/IllustratorGalleryPopup.cs` | 42-92 | 画廊窗口生命周期 | 注册 scope、延迟加载预览、关闭时释放纹理并移除 layer |
| `src/UI/Gallery/IllustratorGalleryPopupVM.cs` | 178-229 | 列表与预览 | 显式刷新当前存档条目，选择时才读取文件并注册预览纹理 |
| `src/UI/Gallery/IllustratorGalleryPopupVM.cs` | 260-318 | 管理操作 | 复制提示词、设为默认、删除到回收区、打开缓存目录、关闭 |
| `src/UI/Patches/EncyclopediaHeroIllustrationPatch.cs` | 143-259 | 百科按钮 | `GauntletMovie.Load` 捕获真实 root；开关关闭时隐藏按钮；点击后走主线程弹窗 |
| `src/UI/Patches/ConversationIllustrationPatch.cs` | 73-230 | 对话按钮 | 注入 AnimusForge overlay 与原版 Map/MissionConversation fallback，支持热刷新显隐 |
| `src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs` | 210-320 | 周报覆层 | 冻结当前周报事件与 VM；后台请求完成后只在同一 scope/revision 下发布 |
| `src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs` | 323-372 | 周报纹理与关闭 | 替换/关闭时释放动态 Sprite，清空静态 VM/context 引用 |
| `src/AnimusForge.Illustrator.csproj` | 17-40 | API 选择 | `BannerlordApi=1.3` 使用固定 1.3.15 reference assemblies；`1.4` 使用当前游戏安装 |
| `src/AnimusForge.Illustrator.csproj` | 66-164 | 引用路由 | Core/Native/SandBox 引用分别路由，避免 1.3/1.4 DLL 混用 |
| `tools/test_illustrator.ps1` | 1-119 | 离线回归 | 双 API 编译、提示词事实回归、XML、prefab command 绑定与反射依赖解析 |
| `tools/deploy_illustrator.ps1` | 1-153 | 部署/校验 | `-ValidateOnly`、`-BannerlordApi auto|1.3|1.4`、manifest/prefab/依赖边界校验、部署前备份 |

---

## 3. 本地缓存契约

实际缓存根目录：

```text
Documents\Mount and Blade II Bannerlord\AnimusForge\IllustratorCache
```

目录结构：

```text
IllustratorCache\
  <campaign-key>\
    encyclopedia\*.png + *.json
    conversation\*.png + *.json
    weekly_report\*.png + *.json
    general\*.png + *.json
  _trash\
    <campaign-key>\*.png + *.json
```

- `campaign-key` 来自 `Campaign.Current.UniqueGameId`，由 `IllustratorRuntime.SetCampaign` 在 `OnSessionLaunched` 时设置。
- JSON 元数据包含 `Key`、`SubjectKey`、`CampaignKey`、`Category`、`Prompt`、`Title`、`IsDefault`、`CreatedTime`。
- 百科主题键：`Hero_<Hero.StringId>`；对话主题键：`Conv_<Character.StringId>`；周报主题键：`weekly_report:<hash(title:subtitle)>`。
- 缓存读取必须传入当前 `CampaignKey`，不会跨存档命中。
- 打开画廊或刷新条目才扫描当前存档目录；正常 Tick 不做全目录扫描。

---

## 4. 本轮修复覆盖

1. **后台线程不再直接触碰引擎/UI**
   - 生图、LLM、下载、缓存保存均在 `IllustratorRuntime.Start` worker 中执行。
   - 纹理创建、Sprite 注册、VM 更新与 layer 清理通过 `Post` 回到主线程。

2. **请求失效与取消**
   - `IllustrationScope` 校验 Campaign、CampaignKey、TopScreen、revision 与 cancellation。
   - 关闭窗口、切屏、换档、再次重绘都会使旧请求结果失效。
   - worker 上限 4，完成队列上限 32，每帧最多处理 2 个完成项。

3. **纹理/Sprite 生命周期**
   - `GauntletTextureLoader` 只释放本模块登记的 `RuntimeIllustrationSprite`。
   - 同名重绘、切换预览、关闭弹窗、切换存档均释放旧动态纹理。
   - `Reset` 会清空所有 Illustrator 动态 Sprite。

4. **存档隔离与缓存上限**
   - 缓存路径和元数据均带 campaign identity。
   - 删除不直接物理销毁，先移动到 `_trash/<campaign>`。
   - `MaxCacheCount` 已实际执行，默认图优先保留。

5. **功能缺口补齐**
   - 卡片支持重新绘制、提示词展开/复制、打开画廊、关闭。
   - 画廊支持当前存档列表、懒加载预览、按主题选中、复制提示词、设默认、删除、打开目录。
   - 卡片和画廊支持标题栏拖动与右下角缩放。
   - MCM 模型列表按钮不再在后台线程更新 UI。

6. **提示词事实保真**
   - 人物身份、性别、文化、装备、围城/事件地点、周报标题与事件摘要会保留在最终提示词尾部。
   - 离线保底不再把男性 Lord/Lady 误判为女性，也不再把“和”误判为日本文化。

7. **双版本编译**
   - `BannerlordApi=1.3`：固定 `Bannerlord.ReferenceAssemblies 1.3.15.110062`。
   - `BannerlordApi=1.4`：当前游戏 `v1.4.8` 引用。
   - `BANNERLORD_1_4_OR_GREATER` 仅为 1.4 编译常量；当前源码无需额外 `#if` 分支。

---

## 5. 验证命令与结果

### 双版本构建

```powershell
dotnet build extensions/AnimusForge.Illustrator/src/AnimusForge.Illustrator.csproj -c Release `
  -p:BannerlordApi=1.3 `
  -p:OutputPath="F:\AnimusForge-main\extensions\AnimusForge.Illustrator\bin\compat\1.3\" `
  -p:BaseIntermediateOutputPath="F:\AnimusForge-main\extensions\AnimusForge.Illustrator\obj\compat\1.3\"

dotnet build extensions/AnimusForge.Illustrator/src/AnimusForge.Illustrator.csproj -c Release `
  -p:BannerlordApi=1.4 `
  -p:OutputPath="F:\AnimusForge-main\extensions\AnimusForge.Illustrator\bin\compat\1.4\" `
  -p:BaseIntermediateOutputPath="F:\AnimusForge-main\extensions\AnimusForge.Illustrator\obj\compat\1.4\"
```

结果：

- `BannerlordApi=1.3`：**0 warnings / 0 errors**
- `BannerlordApi=1.4`：**0 warnings / 0 errors**

### 离线回归

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\test_illustrator.ps1 -Configuration Release
```

覆盖内容：

- 1.3 与 1.4 独立构建。
- 离线规则导演事实保留：男性身份、真实装备、中文连词不误判日本、周报地点/攻城事件保留。
- `SubModule.xml` 与 4 个 prefab XML 解析。
- 所有 `Command.Click` 均存在对应 VM `Execute*` 方法。

最终结果：**41 checks / 0 failures**。

### 部署边界校验（未部署）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\deploy_illustrator.ps1 -ValidateOnly -BannerlordApi 1.3
powershell -NoProfile -ExecutionPolicy Bypass -File tools\deploy_illustrator.ps1 -ValidateOnly -BannerlordApi 1.4
```

结果：

- 两种 API 构建均通过。
- `SubModule.xml`、DLLName、Assemblies、已安装依赖模块、4 个 prefab 均通过校验。
- 构建输出未发现 TaleWorlds、SandBox、Harmony、MCM、Newtonsoft 或 `AnimusForge.dll` 被复制。
- `-ValidateOnly` 未向 `Modules\AnimusForge_Illustrator` 写入文件。

---

## 6. 明确未验证项

以下内容不能由离线编译证明，仍需真实客户端验收：

1. 游戏内 Gauntlet 实际渲染、颜色通道和画面比例。
2. Harmony Patch 在真实 1.4.8 / 1.3 客户端中的挂载与目标 UI 结构兼容性。
3. 真实 OpenAI 兼容生图 API、模型列表 API、图片 URL 下载与服务商错误响应。
4. 快速切存档、切窗口、连点重绘、关闭周报等真实竞态。
5. 长时间游戏后的 VRAM/内存占用与引擎纹理释放完整性。
6. 1.3 客户端真实运行时加载；当前只验证了固定 1.3.15 引用集编译。
7. 营地菜单入口在目标整合包实际菜单结构中的可达性。
8. 最终图像质量、构图稳定性与特定模型输出效果。
9. 非 ASCII/中文系统环境下部署脚本与 XML/JSON 编码行为。
10. 用户授权的正式部署与游戏目录备份/覆盖流程（本轮只运行 `-ValidateOnly`）。

---

## 7. 部署与回滚

### 显式部署（需用户确认后执行）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\deploy_illustrator.ps1 -BannerlordApi auto
```

脚本会先构建并校验，再把现有 `Modules\AnimusForge_Illustrator` 文件备份到：

```text
F:\AnimusForge-main\artifacts\deploy-backups\AnimusForge_Illustrator\<api>\<timestamp>\
```

然后覆盖：

```text
Modules\AnimusForge_Illustrator\SubModule.xml
Modules\AnimusForge_Illustrator\bin\Win64_Shipping_Client\AnimusForge.Illustrator.dll
Modules\AnimusForge_Illustrator\GUI\Prefabs\*.xml
```

### 源码回滚

- Illustrator 维修前本地检查点：`02b4c5b3`。
- 只应对 Illustrator、专用脚本与相关文档做定向 revert；不得 `git reset --hard`，不得回滚无关 TTS/喊话工作区改动。
- 当前工作区另有未提交的用户改动：`ShoutBehavior.cs`、`TtsEngine.cs`、`ShoutBehavior.TtsGameContext.cs`、`AnimusForge/GUI/SpriteParts/af_courier/*`、`preview_courier_scroll.html`；这些不属于 Illustrator 回滚范围。

---

## 8. 后续验收建议

1. 先执行 `-ValidateOnly`，再经用户确认后执行真实部署。
2. 游戏内依次验收：百科按钮、对话按钮、周报覆层、营地画廊入口。
3. 检查关闭/切换周报、重复重绘、跨存档打开同一 NPC 时是否命中正确缓存。
4. 检查长会话后 `UIResourceManager.SpriteData` 中 Illustrator 动态 Sprite 是否随关闭/换档释放。
5. 对真实 API 做一次小尺寸测试，确认端点解析、模型回退和错误提示符合预期。

---

## 9. 实机反馈保真修复（第二轮）

针对实机验收暴露的四类保真缺陷完成修复：

### 文化头饰
- `src/Context/HeroVisualExtractor.cs`：`ResolveRegalHeadwear` 按文化解析君主头饰——阿塞莱=金丝刺绣缠头巾（严禁西式尖顶王冠）、库赛特=貂皮尖顶汗冠、斯特吉亚=环形战冠、巴旦尼亚=凯尔特青铜环冠、帝国=拜占庭月桂冠冕、瓦兰迪亚=西式金冠。
- `src/Core/VisualDirectorEngine.cs`：系统提示词新增【王权头饰铁律】与【纹章铁律】；`SynthesizeRuleBasedPrompt` 君主模板同样按文化选择头饰。

### 真实纹章与多参考图管线
- `src/Engine/ScreenCaptureHelper.cs`：新增 `ExtractBannerOffscreenAsync`（`BannerThumbnailCreationData` 九宫格大旗，setAction 在 GPU 渲染完成后才回调）、`ExtractHeroPortraitOffscreenAsync`（`CharacterThumbnailCreationData` 渲染真实五官/装备立绘）、`CaptureConversationSceneBase64`（裁剪掉底部对话 UI 的 3D 场景带）。全部引擎访问经 `RunOnGameThreadAsync` 调度到主线程，后台仅等待结果。
- 新增 `src/Core/IllustrationReferenceImage.cs`：带中文标签的参考图容器；`VisualDirectorEngine` 与 `UniversalOpenAiImageClient` 均支持 `IReadOnlyList<IllustrationReferenceImage>` 多图混传（保留单图重载）。

### 周报保真
- `src/Context/WeeklyReportContextExtractor.cs`：`ResolveProtagonistHero` 从正文文本匹配真实英雄（最早出现者优先，君主/族长加权），不再写死 `Hero.MainHero`；`EnvironmentVisualExtractor.Extract(settlement, eventAnchored: true)` 跳过当前 Mission/菜单位置探测，`ApplyEventSceneAnchoring` 按事件主题锁定场景字段——同一份周报多次生成场景不再漂移。
- `src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs`：主线程发起肖像+纹章离屏任务，后台 await 后随提示词与生图请求一并发送。

### 会面保真
- `src/Context/ConversationContextExtractor.cs`：补全骑乘组合（玩家骑马对方步行/双方骑马/均步行）与对方随行护卫计数；新增 `InterlocutorCivilian`。
- `src/UI/Overlays/IllustrationCardPopup.cs`：会面参考图 = 场景实景裁剪 + 对方肖像 + 对方纹章；百科参考图 = Tableau 人物 + 家族纹章。

### 新设置项
- `向生图模型附带参考图 (垫图/图生图)`（默认开）：控制参考图是否进入生图请求。
- `负面提示词 (Negative Prompt)`：对话生图通道注入禁止元素指令。
- 注：提示词中的“克雷格·穆林斯（Craig Mullins）”是画师风格引用，非宗教表述。

### 验证
- `tools/test_illustrator.ps1`：63 项检查 0 失败（含 1.3/1.4 双编译、阿塞莱缠头巾断言、多图重载反射、源码管线检查）。
- `deploy_illustrator.ps1 -ValidateOnly` 双 API 通过；`git diff --check` 通过。
- 未实机验证：纹章/肖像缩略图在真实游戏中的渲染完成率与超时回退表现。

---

## 10. 提示词放宽 + 对话联动 + 离屏舞台（第三轮）

### 提示词三层结构

- `src/Core/VisualDirectorEngine.cs`：`IllustrationPromptPlan(Mode, HardFacts, ArtDirection)` 把上下文拆成只读事实区 `<game_facts>` 与开放构图区 `<open_art_direction>`；导演输出经 `ComposeFinalPrompt` 由程序把硬事实重新拼回——LLM 漏写事实也不会丢。
- 系统提示词全面放宽：删掉"绝对禁止/严禁"式命令与文化刻板模板，只保留事实不可改写、无证据不虚构两条底线；画风服从用户设置，不再锁定"伦勃朗+穆林斯"单一画家混合。
- 离线规则保底 `SynthesizeRuleBasedPrompt(plan, options)` 同样走事实/建议分层并尊重自定义画风。

### 更多场景与最近三轮对话

- `src/UI/Overlays/IllustrationCardPopup.cs`：`GenerateDiversePoseDirective` 改为 16 条开放构图方向（远景环境肖像、过肩、高低机位、前景遮挡、行进瞬间等）+ 按身份给倾向而非模板；`GenerateConversationSceneVariation` 提供 16 条会面镜头变化。
- `src/Context/ConversationContextExtractor.cs`：`RecentDialogueHistory` 经 `ReadNativeConversationHistory` **反射**调用主模组 `ShoutBehavior.GetNativeConversationSessionHistoryEntriesForExternal`（主模组由他人重构，反射使旧版宿主/字段改名只降级为无历史，不抛 MissingMethod）；`BuildRecentDialogueHistory` 按"玩家开题为一轮"聚合，取最近 3 轮并按说话人/顺序格式化进硬事实区。
- `SceneDirective` 要求导演围绕最近三轮对话的关系/情绪转折选瞬间，空间关系（骑乘、随行、围城攻防）作为已确认事实给出。

### 离屏舞台与并发修复

- `GUI/Prefabs/IllustratorOffscreenStage.xml`：`BannerTableauWidget`/`CharacterTableauWidget` 位于 `0,0` 但 `AlphaFactor=0`——处于可渲染区域所以游戏 UI 管线正常创建/tick provider，`OnRender` 每帧执行；玩家看到的只有全透明像素。
- `src/Engine/ScreenCaptureHelper.cs`：`ExtractViaStageAsync` 临时挂 Gauntlet 层 → 预热 → `SetSaveFinalResultToDisk` 原生落盘 → 后台读 PNG → `FinishStage` 幂等拆层。每请求独立 `af_offscreen_{Guid}` 前缀，并发任务不再互删；全链路携带 `CancellationToken`，取消/切屏/超时立即终止泵并释放舞台。
- 会面/周报/百科的离屏提取全部移入 `IllustrationScope.Run` 内执行——关闭弹窗或重绘时旧舞台随 scope 取消。

### 周报结构化快照

- `src/Context/WeeklyReportContextExtractor.cs`：新增子模块侧 `WeeklyReportIllustrationSnapshot`（标题/副题/要闻/主题/当事人/事件定居点/发布日期一次解析成型）。`ResolveEventSettlement` 只匹配文本真实提及的定居点，**删除了"主角当前定居点→家乡→玩家位置"的虚构回退**；事件现场标未知而不是冒充。`GenerateSceneDirective`/`ApplyEventSceneAnchoring` 改为"可选取景元素"建议，进艺术指导区而非硬命令。
- `src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs`：改用 `IllustrationPromptPlan`，离屏任务带 token，缓存保存 `result.ResolvedPrompt`（实际发送的有效提示词）。

### 生图协议参考图修复

- `src/Core/UniversalOpenAiImageClient.cs`：新增 `BuildEffectivePrompt`（实际发送值 = 画风/画幅/负面注入后的提示词，存入缓存与"查看提示词"）；Images 协议有参考图时先走 `/images/edits` multipart（`image[]` 文件流真正上传）；edits 不可用时回退纯文本 generations 并明确记录"参考图仅供导演识图"；日志区分 `refImages`（请求）与 `ActualRefImages`（实际发送）。

### 验证

- `BannerlordApi=1.3` / `1.4` 各 0 警告 0 错误；`tools/test_illustrator.ps1` **86 checks / 0 failures**；`git diff --check` 干净。
- 已部署 `Modules\AnimusForge_Illustrator`（含新 prefab）。
- 未实机验证：舞台纹章/立绘真实出图率（AlphaFactor=0 方案）、/images/edits 在用户网关的兼容性、最近三轮对话实际进图效果、取消时舞台释放表现。

---

## 11. 实机反馈迭代（第四轮，2026-09-15）

第四轮全部针对真实游戏截图与日志暴露的问题，约 35 个提交（`3de2e554`..`fd5657e4`）。按主题归类：

### 11.1 场景感知纠正

| 问题 | 根因 | 修复 | 提交 |
| :--- | :--- | :--- | :--- |
| 野外遭遇生成在城镇 | 贴着城镇的野外会话 `Settlement.CurrentSettlement` 仍非空，`settlement.IsTown` 分支误判"市集街道" | `ResolveSpecificLocation` 中 `Mission.Current != null && CampaignMission.Location == null` 排在城镇兜底之前；宿主场景快照含野外词时反向纠正 | `1a0dea8f` |
| 海上会话无场景 | 此前无海上判定 | `EnvironmentVisualExtractor.ResolveSeaAndArmyContext`：1.4 用 `MobileParty.IsCurrentlyAtSea`（`#if BANNERLORD_1_4_OR_GREATER` 隔离，1.3 走宿主快照兜底）→ "海船甲板"；`MainParty.Army` → 军团联营事实 | `b0282d48` |
| 围城阵前谈判场景缺失 | 围城会话无专门场景 | `BesiegedSettlement`/`PlayerEncounter` 围城态 → 阵前谈判场景 | `662ed903` |
| 宿主场景不联动 | 主模组 `ShoutUtils.GetCurrentSceneDescription()` 的结果未被使用 | 反射调用宿主快照进事实区，并与本地判定交叉纠错；`ConvScene host= loc= scene=` 三值诊断日志 | `662ed903`/`69dc488f` |

宿主快照可产出的场景词：平原/森林/山地/海岸、海上、街道/酒馆/地牢/港口/竞技场/大厅。**宿主含野外词而我们判成城镇时以宿主为准**。

### 11.2 人物处境状态注入

`HeroVisualExtractor.ExtractCurrentState`（`b65ce107`）：`Hero.IsPrisoner`（区分定居点地牢关押/队伍押送，附带"武器收缴、锁链镣铐、勿照搬参考图战甲"视觉约束）、`IsWounded`、`StayingInSettlement`、`PartyBelongedTo`、`IsDead`（纪念肖像语境）。经 `BuildSummary` 的 `【当前处境状态】` 进百科/会话/周报全链路。会话层另加囚禁场景修正：任一方为战俘时 `SpecificLocation` 改地牢/囚笼，`SceneDirective` 注入看押语境。

非英雄会话方（劫匪等模板 NPC）：`ConversationContextExtractor` 读取在场 `ConversationAgents[0]` 的 `Agent.BodyPropertiesValue`，经 `ExtractCharacterPortraitOffscreenAsync(..., bodyProperties)` 还原**正在对话的那张脸**，不再从 `CharacterObject` 模板重新随机（`a2bfe177`）。

### 11.3 台词路由与禁文字约束（`d52591ca`）

- `IllustrationPromptPlan` 新增 **`DirectorOnlyFacts`**：并入 `<game_facts>` 给导演，但 `ComposeFinalPrompt` 不拼入生图提示词。
- `ConversationVisualContext.BuildDialogueBlock()`（当前台词+近三轮历史）从 `BuildHardFacts()` 移出，仅作导演专属——生图模型看不到台词原文，画面不再被画进字幕。
- `ComposeFinalPrompt` 尾部硬约束：严禁任何文字/字幕/台词/标牌/UI；人物肤色发色五官严格以立绘参考图为准。

### 11.4 纹章管线（真实旗帜合成）

- **`BannerEmblemComposer`**（`8a61eb25` 起）：纯托管 GDI+ 合成——`Banner.BannerDataList` 配方 + `banner_icons` 图集像素读回（`GetPixelData`），主线程只做配方/句柄解析（`RunOnGameThreadAsync`），像素合成在调用线程。彻底摆脱纹章舞台渲染。
- **图集网格定案 4×4**（`4a8786cd`）：原版 `BannerVisual.ConvertToMultiMesh` 实锤 `u=(texIdx%4)*0.25`、`v=1-(texIdx/4)*0.25`，`texture_index` 实测范围 0-15。先前按 8×8/16×16 裁出的是真格的角部残片；文件名中的 163/510 是 meshId 非 texIdx。
- **格位/字形翻转**（`58360c85`）：图集不翻转（索引对应原始行序），单格裁出后 `RotateFlip` 回正字形——`GetPixelData` 自下而上位图行序所致。
- **语义降级为约束**（`69dc488f`）：参考图标签改为"当画面出现纹章载体时必须一致，不要仅为展示纹章强塞盾/旗"——解决盾牌出现频率过高。
- **双家族纹章**（`938e12a9`）：会话参考图同时发玩家家族与对方家族纹章（BannerCode 去重），标签注明归属方。
- 空纹章（合成失败/全透明）不发参考图（`deec7e5d`）；调试落盘 `banner_debug/`（`38dded32`）。

### 11.5 周报事件场景

- **卡"构思"修复**：`IllustrationScope.Run` 在 `!IsCurrent` 时静默返回致卡片停初始文案（`6f8acf3b` 回报失败+日志）；`Show` 中上下文先于 `AttachOverlay` 内部 `CloseOverlay()` 提取被清空（`04429495` 提取挪到 CloseOverlay 之后）。
- **主题扩至 10 类**（`06cf47e1`）：劫掠/野战/围城/庆典比武/外交/**海战/囚禁处决/定居点易主/亡故继位**/通用——每类有专属场景锚定与取景指令。
- **General 兜底三级递进**（`56cfdf2d`/`fd5657e4`）：预设主题未命中 → 场景名词扫描（酒馆/地牢/渡口/军营等 14 组**复合词**，裸单字"海/山/河"会误中人名地名已收紧）→ 定居点泛指+授权导演从要闻文本自行推断。
- 事件地点只认正文真实提及的定居点，绝不拿玩家当前位置冒充（`eventAnchored` 模式跳过实时 Mission 探测，也不注入玩家海上/军团实时状态）。

### 11.6 可靠性与舞台可见性

- **导演超时 30s→120s**（`db441a9a`），区分"用户取消"与"上游超时"两种失败文案；带多参考图的多模态调用不再频繁误报超时。
- **空回复不自动重试**（`f670c82d`，用户明确要求）：`created:0+content:null+completion_tokens:0` 属上游网关瞬时故障；失败原因+原始响应预览进日志，手动重绘即可。
- **离屏舞台零可见**（`662ed903`/`265ca711`/`782cf160`）：裁剪 2×2、排在正常 UI 层后、串行化渲染防原生崩溃。
- **MCM 新增**：`生成完成后自动清理临时文件`（默认关）——清理离屏立绘 PNG/纹章图集导出/banner_debug；画风预设扩展为 7 项（古典油画默认/vivid/natural/暗黑史诗/电影级/自定义提示词/莫桑珐琅彩饰），自定义画风与负面词各有独立编辑器；分辨率快捷预设下拉。
- **画廊**：预览唯一 sprite 名修首点空白；新生成图自动设为该主题默认（`8a40a255`）。

### 11.7 验证与遗留

- 双版本（1.3.15 引用集 / 1.4.8 实机引用）编译 0 警告 0 错误；`tools/test_illustrator.ps1` **90 checks / 0 failures**；已部署（最新备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260915-164726`）。
- **已实机确认**：野外场景识别（`ConvScene` 三值一致）、纹章合成出真图标、囚禁状态字段编译通过。
- **待实机确认**：劫匪真实脸还原度、台词彻底不入画、纹章 4×4 格位在各类家族的准确性、俘虏/海上/军团场景实际生成效果、周报新主题命中率。
- **已知上游问题**：生图网关偶发空 completion（`content:null`），非本模块 bug，无自动重试（用户要求）；空回复时卡片显示原因、日志含响应预览。
- 回滚基线：本轮起点 `3de2e554`（检查点提交）；单点修复均可 `git revert <sha>` 定向回退。

---

## 12. 重绘变体与相似度滑块（第五轮，2026-09-16，提交 `1698b431`）

### 重绘雷同修复

- **根因**：周报 `BuildArtDirection()` 输出完全确定——同一周报重绘提示词逐字相同；会话/百科虽有随机构图种子但仅为软建议。
- `VisualDirectorEngine.BuildRedrawVariationDirective(redrawIndex)`：注入"必须换景别/机位/瞬间/景深层次"的重绘指令；三条链路各挂计数器（卡片 `_generationCount`、周报 `_redrawCount` 在新周报 attach 时归零），首次生成不注入、点重绘才生效。
- 周报新增 `GenerateWeeklyVariation()`：12 条纪事构图变体**每次生成**都注入（此前一条没有）。

### MCM 相似度滑块

- `IllustratorSettings.Similarity`：0-100 默认 80（"2. 生图 API 配置"组 Order=12）。
- `IllustrationOptions.Similarity` clamp 0-100 → `BuildEffectivePrompt(..., similarity)` 追加 `[参考还原度约束]`：人物五官/肤色/发型/纹章**恒严格一致**；其余部分（场景布置/构图/装备细节/氛围）按 N% 还原参考图与事实。100=完全还原，0=仅留事实骨架自由创作。
- 对 /images 与 /chat 两个协议通道均生效。

### 事实路由审计结论（评估记录，未动代码）

用户问"是否还有事实该只发导演"——逐块评估如下，供下一棒决策：

| 内容 | 当前位置 | 建议 | 理由 |
| :--- | :--- | :--- | :--- |
| 当前台词+近三轮对话 | DirectorOnlyFacts | ✅ 保持 | 已实现，防字幕入画 |
| 周报【报头】【核心局势】【事件要闻】**原文** | HardFacts | ⚠️ 最强候选：提炼版留 HardFacts、原文挪 DirectorOnlyFacts | 叙述性标题文本与台词同性质，有被渲染成画面字的风险；但事件主体（谁/在哪/结果）必须以提炼干形式留在 HardFacts |
| 性格特质/顶尖专长/官方传记 | HardFacts | ⚠️ 可挪导演专属 | 抽象非视觉（"算度""战神"），导演翻译成神态气质即可；对生图模型是噪声且有轻微误导风险 |
| 【纪元时间】、宿主场景原句 | HardFacts | 可挪（低风险低价值） | 与 SpecificLocation 重复/非视觉 |
| 外貌/装备/坐骑/纹章色/处境状态/场景类型/骑乘站位/护卫数 | HardFacts | ✅ 必须留 | 纯视觉事实，且"导演漏写不丢"是本架构的核心保证 |

**关键约束**：离线保底 `SynthesizeRuleBasedPrompt` **只消费 HardFacts**——挪进 DirectorOnlyFacts 的内容在无导演/断网时彻底消失。因此任何迁移必须是"提炼版留 HardFacts + 原文进 DirectorOnlyFacts"的双层写法，不能整段搬走，否则离线模式丢事件主体。

### 验证

- 双版本 0 错误；回归 90 checks / 0 failures；部署备份 `20260916-034050`。
- 待实机：重绘构图差异化效果、相似度滑块低/高值出图差异。

---

## 13. 四项代码审查修复（第六轮，2026-09-16）

源码及测试提交：`9dda2fa2`。修改前本地检查点：`cf5ccc0`。活动目录仍为 `F:\AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。本轮未推送、未覆盖游戏目录。

### 已修复行为与核实坐标

以下路径均相对 `extensions/AnimusForge.Illustrator/src`，行号对应 `9dda2fa2`：

| 路径 | 行号 | 符号 / 责任 |
| --- | --- | --- |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs` | 249–254、418–421 | `AttachOverlay` / `CloseOverlayForScope`：关闭回调捕获自身 scope，旧 scope 延迟清理不再关闭新弹窗。 |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs` | 305–343 | `TriggerRegenerate`：中间状态回调也核对 scope 与取消令牌；提取任务接收冻结的清理选项。 |
| `Engine/ScreenCaptureHelper.cs` | 314–343、392–473 | `CleanupTempArtifacts` / `WaitForOffscreenFileAsync`：只能清理调用方 GUID 前缀，不接受空前缀或全局通配符。 |
| `Engine/ScreenCaptureHelper.cs` | 979–1008、1022–1115 | `ReadOffscreenPngBase64` / `ExtractViaStageAsync`：消费结束后删除自身图片，取消/失败走自身清理；成功交付文件不能在读取前删除。 |
| `Engine/BannerEmblemComposer.cs` | 191–300、619–630 | `ComposeBitmapsAsync` / `CleanupDebugDump` / `DebugDumpDir`：每次合成独占 `banner_debug/<guid>`，按设置在合成结束后只清理自身目录。图集导出的唯一临时文件继续由原有 `finally` 清理。 |
| `Settings/IllustratorSettings.cs` | 62–64、260–262 | `AutoCleanTempFiles` / `Similarity`：更新清理范围说明；使用 `0'%'` 显示整数百分比，80 不再显示为 8000%。 |
| `Core/UniversalOpenAiImageClient.cs` | 297–323 | `BuildEffectivePrompt`：各相似度均保留身份、装备及全部硬事实；区分身份图、纹章图与场景参考，100 也允许重绘换镜头。生图结束处不再调用全局临时文件清理。 |
| `UI/Overlays/IllustrationCardPopup.cs` | 192–204、357–373 | 百科与会话参考图调用点传递冻结的 `AutoCleanTempFiles` 选项。 |

### 性能与保留边界

- scope 身份检查为回调时的常数时间比较；不增加 Tick 扫描或轮询。
- 清理发生于提取任务结束的后台流程，只枚举其唯一前缀或独占调试目录；没有新增全局锁。
- 清理开关关闭时保留调试产物；成功读取的立绘和图集临时文件沿用原有释放行为。历史调试文件不再由其他请求的结束动作删除。
- 原生层若在超时清理之后才迟到落盘，仍可能留下本次临时文件；未增加持续轮询或跨请求清扫。现有过期离屏文件维护继续保留。
- 相似度是文本指导，不是图像模型的数值采样参数；程序保证发送的约束一致，实际图像遵循程度仍需实机验收。

### 验证与回滚

执行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_illustrator.ps1 -Configuration Release`：1.3 / 1.4 构建均 0 警告、0 错误；总计 **113 checks / 0 failures**。

新增测试位于仓库 `tools/test_illustrator.ps1:195–279`，23 项检查覆盖：实际 MCM 属性格式的 0/80/100 输出；两种协议下三个相似度值的事实、重绘与参考用途约束；旧 scope 清理不影响新 scope、当前 scope 正常关闭并取消请求；真实文件夹中清理 A 保留 B、拒绝宽泛前缀、纹章目录隔离。测试直接调用编译后的方法；scope 使用无游戏对象的离线实例，不能代替真实 Gauntlet 验收。

`git diff --check` 通过。未验证真实客户端连续切换周报、原生落盘时序、真实 API 出图及新提示词的视觉效果；本轮没有部署授权，也未部署。

源码回滚仅定向 `git revert 9dda2fa2`；不要 hard reset，不要回滚其他作者的 TTS、喊话、信使图片或工具改动。
### 第七轮：事实路由与叙述回退（2026-09-16，`e86efa18`）

截图中第五轮的事实路由审计已落实。新增 `Context/NarrativeFactRouter.cs:10-119`，对冻结文本做一次线性句段摘录：周报保留行动、地点、否定、计划与结果所在完整证据句；直接引语不升级为已发生事实；空/未知事件明确保持未知。`WeeklyReportContextExtractor.cs:52-115,180-210` 不再截断正文，不再无命中时把玩家主角冒充事件当事人，报头/局势/要闻原文进入导演专属区。

`HeroVisualExtractor.cs:39-91` 将文化/势力/传记中的视觉证据保留在 HardFacts，抽象特质、技能和非视觉传记进入导演专属区。`EnvironmentVisualExtractor.cs:37-70` 将季节、时段、天气、护卫和已确认场景保留；精确日期、引擎资源名与宿主场景原句进入导演专属区。`ConversationContextExtractor.cs:39-82`、`IllustrationCardPopup.cs:180-185,324`、`WeeklyReportPopupIllustrationPatch.cs:305` 已接入三条生图入口。

`VisualDirectorEngine.cs:39-61,112-132,165-176` 为导演原文建立 `<director_only_narrative>` 独立区，并禁止逐字复述、把引语/计划/否定改为结果；`ResolveDirectorOutput` 发现叙述复述时直接走离线保底，不重试、不增加网络请求。离线保底只使用视觉 HardFacts 与开放构图方向。

`tools/test_illustrator.ps1:195-380` 新增 20 项事实路由与导演回退检查。最终 1.3 / 1.4 构建 0 警告、0 错误，**133 checks / 0 failures**；`git diff --check` 通过。当前未验证真实导演模型的改写质量、超长非标点正文、真实存档周报与实机画面；本轮未部署。回滚点：`git revert e86efa18`，前一修复提交 `9dda2fa2`。
补充审计（第七轮）：`NarrativeFactRouter` 对周报正文采用完整句证据摘录，不再 400 字截断；引语内容不升级为已确认结果。人物视觉证据与导演专属原文分离，陌生 MOD 种族名称可通过真实文化/百科/面貌字段进入提示词，模型不会被硬编码的人类文化模板覆盖。没有“兽人/精灵”等关键词时也会保留角色真实种族名、文化名、面貌字段和立绘参考图；模型实际识别效果仍取决于子 MOD 是否提供这些数据以及使用的视觉模型。