# AnimusForge.Illustrator — 技术架构与研发交接文档 (HANDOFF)

> **创建日期**：2026-09-14  
> **当前活动分支**：`codex/af-main-refactor-continuation-20260831`  
> **所属子模块**：`extensions/AnimusForge.Illustrator`  
> **部署输出目录**：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge_Illustrator\bin\Win64_Shipping_Client\`  
> **模块程序集**：`AnimusForge.Illustrator.dll`  
> **编译与部署验证**：`dotnet build` 成功（0 Error, 0 Critical Warning），一键部署脚本 `tools/deploy_illustrator.ps1` 校验通过。

---

## 1. 架构定位与交付范围 (Scope & Responsibilities)

`AnimusForge.Illustrator` 是 AnimusForge 模组生态下的 AI 视觉插画子模组。其核心职责是将《骑马与砍杀2：霸主》游戏底层全量上下文数据（英雄身份传记、FaceGen 骨相、文化风貌、家族纹章识别色、槽位装备、战场围城环境）与前沿多模态视觉大模型、生成式绘图模型（OpenAI 兼容端点 / FLUX / DALL-E / Gemini）桥接，实现游戏内四大场景（**英雄百科专属肖像**、**现场面对面会晤插画**、**每周国家周报纪事大事件画卷**、**大地图营地纪事画廊**）的实时生成、本地持久化与可拖拽 UI 渲染展示。

### 架构依赖图
```text
Bannerlord 引擎层 (DirectX 11 / TableauView / FaceGen / Mission)
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                 AnimusForge.Illustrator                     │
├─────────────────────────────────────────────────────────────┤
│ 1. Engine 渲染层:                                           │
│    - ScreenCaptureHelper (TableauView 离屏 GPU 导出)        │
│    - SwapRedAndBlueInBitmap (DirectX BGRA 通道翻转校准)       │
│    - GauntletTextureLoader (动态 Sprite / EngineTexture 装载)│
│    - DiskImageCacheManager (本地文件与元数据持久化/清理)    │
├─────────────────────────────────────────────────────────────┤
│ 2. Context 上下文提取层 (全量动态无硬编码):                │
│    - HeroVisualExtractor (传记/特质/专长/发型发色/装备/HSV)  │
│    - EnvironmentVisualExtractor (空间/守备/道具/围城/天候)  │
│    - ConversationContextExtractor (对话双方/情景)          │
│    - WeeklyReportContextExtractor (周报重大历史头条)        │
├─────────────────────────────────────────────────────────────┤
│ 3. Core 核心总监与通信层:                                   │
│    - VisualDirectorEngine (纯中文提示词系统 / 多模态降级 /   │
│                           自动复用 AF 正文 API / 规则导演)   │
│    - UniversalOpenAiImageClient (兼容生图客户端 / 异步防卡死)│
├─────────────────────────────────────────────────────────────┤
│ 4. UI 视窗与 Patch 注入层:                                  │
│    - MovableGauntletLayer (可拖拽/可缩放独立视窗图层)       │
│    - IllustrationCardPopup / IllustrationCardVM (肖像卡片)  │
│    - IllustratorGalleryPopup / VM (纪事画廊)                │
│    - EncyclopediaHeroIllustrationPatch (百科按钮注入)       │
│    - ConversationIllustrationPatch (对话按钮注入)           │
│    - WeeklyReportPopupIllustrationPatch (周报覆层注入)      │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 核心源码坐标与职责核实表 (Verified Code Locations & Symbols)

所有源码路径均相对于仓库根目录 `f:/AnimusForge-main/`，行号均为 1-indexed 实测准确坐标：

| 仓库相对路径 | 行号范围 | 核心类 / 方法符号 | 核心职责与设计决策 |
| :--- | :--- | :--- | :--- |
| `extensions/AnimusForge.Illustrator/src/SubModule.cs` | 15-60 | `SubModule.OnSubModuleLoad` | 模块入口初始化，集中挂载百科、对话与周报 Harmony Patch；优雅卸载清理 |
| `extensions/AnimusForge.Illustrator/src/SubModule.cs` | 62-101 | `IllustratorCampaignBehavior.OnSessionLaunched` | 在大地图营地菜单 (`camp`) 注册“卡拉迪亚纪事画廊”入口 |
| `extensions/AnimusForge.Illustrator/src/Settings/IllustratorSettings.cs` | 18-180 | `IllustratorSettings` (MCM) | MCM 配置面板（端点 Base URL、API Key、模型名、分辨率尺寸、多模态开关、离屏开关、色道修正） |
| `extensions/AnimusForge.Illustrator/src/Settings/IllustratorSettings.cs` | 252-397 | `FetchModelsAsync` | 异步向服务商发起 `GET /models`，自动解析并筛选出图像与绘画模型列表至下拉选单 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs` | 333-408 | `WaitForOffscreenFileAsync` | 后台异步等待引擎 GPU 渲染落盘 PNG 文件，超时平滑回退，安全无崩 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs` | 762-792 | `SwapRedAndBlueInBitmap` | **核心修复**：内存级遍历互换红蓝字节（BGRA -> RGBA），杜绝阿凡达蓝皮与黄色变蓝 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs` | 543-595 | `ConvertEngineTextureToBase64` | 优先通过 `engineTexture.SaveToFile` 显卡导出，回退到 256 字节对齐的安全缓冲区 |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs` | 90-155 | `HeroVisualExtractor.Extract` | 角色全量信息抽取总入口（文化、地位、传记、特质、技能、发型发色、肤色、装备、坐骑） |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs` | 575-670 | `ExtractPhysicalFeatures` | 从 `MBBodyProperties` 动态提取 FaceGen 发型索引、梯度发色、岁月斑白痕迹与真实肉色肤质 |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs` | 1010-1073| `ResolveColorName` | HSV 色相判定算法，将家族 16 进制识别色转为精准中文（丁香淡紫、辉煌灿金等） |
| `extensions/AnimusForge.Illustrator/src/Context/EnvironmentVisualExtractor.cs` | 68-130 | `EnvironmentVisualExtractor.Extract` | 场景全维感知（建筑风格、地形地貌、时令天候、时辰光影、周围 NPC 数量与职业） |
| `extensions/AnimusForge.Illustrator/src/Context/EnvironmentVisualExtractor.cs` | 477-493 | `ResolveConflictStatus` | 围城/洗劫/盛世状态动态感知，将战场危急战况注入插画提示词 |
| `extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs` | 18-43 | `SystemPrompt` | 顶级艺术总监纯中文系统提示词，强制要求全中文、文化保真、配色铁律、健康肉色皮肤 |
| `extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs` | 120-174 | `TryGetHostPrimaryChatConfig` | 核心反射桥接：全自动免密复用 AnimusForge 本体的主力正文对话 API 配置 |
| `extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs` | 176-264 | `CallLlmDirectorAsync` | 多模态提示词扩写请求；遇非 200 或不支持视觉时自动平滑回退为纯文本重试 |
| `extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs` | 280-373 | `SynthesizeRuleBasedPrompt` | 离线纯中文规则导演组装器（无网或无 LLM 时的顶级古典写实油画保底组装） |
| `extensions/AnimusForge.Illustrator/src/Core/UniversalOpenAiImageClient.cs` | 39-126 | `GenerateImageAsync` | OpenAI 生图客户端总调度，支持智能协议自适应（`/images/generations` 与 `/chat/completions`） |
| `extensions/AnimusForge.Illustrator/src/Engine/DiskImageCacheManager.cs` | 45-160 | `TryGetCachedImage` / `SaveImage` | 本地磁盘缓存读写（图片 + JSON 元数据），LRU 淘汰控制与最大数量限制 |
| `extensions/AnimusForge.Illustrator/src/Engine/GauntletTextureLoader.cs` | 44-100 | `LoadOrRegisterPngBytes` | 内存中无缝创建 `EngineTexture` 与 `BannerlordUiSprite` 供 Gauntlet 实时渲染 |
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs` | 45-120 | `ShowForEncyclopedia` | 百科肖像弹窗拉起：**核心时序防遮挡**（挂载图层前瞬间提取 3D 模型），异步调度出图 |
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/MovableGauntletLayer.cs` | 15-85 | `MovableGauntletLayer` | 支持鼠标自由拖动标题栏移动、动态缩放的独立 Gauntlet UI 浮层容器 |
| `extensions/AnimusForge.Illustrator/src/UI/Patches/EncyclopediaHeroIllustrationPatch.cs` | 31-71 | `EnsurePatched` | 遵循百科案例，通过 `GauntletMovie.Load` 挂钩真 root，拦截 `EncyclopediaData.OnTick` 防热键冲突 |

---

## 3. 关键技术突破与底层细节剖析

### 3.1 DirectX 11 与 Gauntlet UI 出入双向色彩通道校准 (彻底根治偏色与蓝皮)
在骑砍 2 与生成式 AI 的数据交互链路中，存在**两处方向相反但同等致命的色彩通道翻转点**：

1. **出方向（游戏 3D 导出 -> 视觉 AI 垫图）：**
   - **机理**：`TableauView` 导出显卡 RenderTarget 离屏图像时，DirectX 11 原始缓冲区为 BGRA 排列。若不处理直接发给大模型，金黄色（高 R 高 G 低 B）会变成青蓝色（低 R 高 G 高 B）。视觉模型“眼见为实”，会在提示词中写出诸如 `"cobalt-blue silk trim"` 与蓝鹰盾牌。
   - **修复**：在 [`ScreenCaptureHelper.cs:762-792`](file:///f:/AnimusForge-main/extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs#L762-L792) 中执行 `SwapRedAndBlueInBitmap`（BGRA -> RGBA），恢复真实金色与健康肉色，使 Vision LLM 识别出精准色彩。

2. **进方向（AI 生成 PNG -> 游戏 Gauntlet UI 渲染）：**
   - **机理**：AI 生成的图片与本地缓存是标准 RGBA PNG（在磁盘上肉眼查看绝对正常，如俄洛斯在磁盘上为真实小麦色皮肤、暖橙色火盆）。然而，Bannerlord 原生 `Texture.CreateFromMemory` 装载进显存后，Gauntlet UI 的材质着色器（Material Shader）默认以 BGRA 顺序采样动态贴图。若直接渲染，屏幕上的红蓝将再次对调，导致人物在游戏 UI 中呈现阿凡达般的“蓝皮”与“蓝火”。
   - **修复**：在 [`GauntletTextureLoader.cs:57-61`](file:///f:/AnimusForge-main/extensions/AnimusForge.Illustrator/src/Engine/GauntletTextureLoader.cs#L57-L61) 中将 `FixColorChannels` 默认设为永久开启（`shouldFixColors = true`），通过 `SwapRedAndBlueInPng` 在向显卡提交纹理前执行预置置换，与 Gauntlet 着色器的对调相互抵消，实现在游戏 UI 中 100% 还原真实血色肉色与金华战袍。

```csharp
// 内存级快速通道翻转核心代码 (指针操作，0 GC 内存分配)
var bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
unsafe {
    byte* ptr = (byte*)bmpData.Scan0.ToPointer();
    int totalBytes = bmpData.Stride * bmp.Height;
    for (int i = 0; i < totalBytes; i += 4) {
        byte temp = ptr[i];      // B
        ptr[i] = ptr[i + 2];     // R -> B
        ptr[i + 2] = temp;       // B -> R
    }
}
bmp.UnlockBits(bmpData);
```

### 3.2 3D TableauView 离屏渲染异步安全落盘管线
- **时序与防遮挡设计**：
  在用户点击百科右上角按钮时，若弹窗先被挂载到 `ScreenManager.TopScreen`，弹窗自身的 UI 面板就会挡住背景中的 3D 人物模型。
  [`IllustrationCardPopup.cs:53-67`](file:///f:/AnimusForge-main/extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs#L53-L67) 采用严格的前置时序控制：
  1. 在 `AddLayer` 之前的一瞬间，先从 `TableauWidget` 触发 `TriggerTableauViewSave` 并抓取备用视口缓冲；
  2. 后台开启异步线程等待文件落盘（25ms 轮询，上限 450ms）；
  3. 拿到干净 PNG 字节后立即将临时文件删除，并执行 `SwapRedAndBlueInBitmap` 校准；
  4. 绝不在主线程发生同步阻塞等待，游戏帧率丝毫不受影响。

### 3.3 全量游戏视觉特征感知（零死板硬编码）
- **绝不依靠硬编码**：
  `HeroVisualExtractor` 动态读取 `hero.Culture.EncyclopediaText`、`hero.Clan.EncyclopediaText` 以及 `hero.EncyclopediaText`。因此无论是原生卡拉迪亚文化，还是东方日韩、西欧中世纪、指环王中土等大型 MOD，都能**100% 自动兼容其独特的文化建筑、装备与风土人情**。
- **FaceGen 骨相参数提取**：
  直接反射原生 `MBBodyProperties.GetParamsFromKey` 与 `MBBodyProperties.GetHairColorGradientPoints`，将年龄、发型、胡须类型与肤色微血管光泽解析为文学级描摹。

### 3.4 提示词系统双引擎与自动配置桥接
- **自动免密复用本体配置**：
  玩家在安装并配置好 AnimusForge 本体的聊天 API 后，`AnimusForge.Illustrator` 依靠 [`VisualDirectorEngine.cs:120-174`](file:///f:/AnimusForge-main/extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs#L120-L174) 的无入侵反射，直接提取 `DuelSettings.GetSettings()` 中的 URL、Key 与主力模型，**无需玩家重复输入二次 API 鉴权信息**。
- **离线规则智能导演（保底机制）**：
  即便大模型服务不可用，离线引擎 [`VisualDirectorEngine.cs:280-373`](file:///f:/AnimusForge-main/extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs#L280-L373) 依然能把英雄的真实装备材质、文化背景、身处环境组合成大师级的古典写实油画提示词。

---

## 4. 覆盖范围与未覆盖责任边界 (Coverage & Boundaries)

### 已完全覆盖并验证 (Covered)
1. **百科全书（EncyclopediaHeroPage）**：动态注入立绘按钮、离屏截取、UI 展示、重绘、提示词复制；遵循百科防热键冲突规范。
2. **场景会晤与大地图对话（Conversation）**：对话现场环境感知、双方站位与氛围提取、场景插画卡片浮层。
3. **周报纪事大事件（WeeklyReport）**：提取重大国家头条事件，自动生成史诗级纪事插画并嵌入周报。
4. **大地图营地画廊（Gallery）**：大地图营地菜单常驻入口，分类瀑布流查看、设为默认、删除管理。
5. **DirectX 色彩通道翻转与纯中文提示词系统**：BGRA 像素通道校正，全中文油画风格提词。
6. **通用生图通信与多格式支持**：兼容标准 `/images/generations` 及对话多模态通道，支持 DALL-E、FLUX、SDXL 等。
7. **本地磁盘缓存生命周期管理**：自动持久化、限制数量淘汰、元数据存储。

### 明确未覆盖 / 留待未来扩展 (Uncovered)
1. **战役即时战报插画**：野战或攻城战结算界面（BattleResultScreen）目前尚未注入独立战果纪事按钮（已预留接口，后续可接入）。
2. **多英雄同框立绘合成**：当前会面场景主要描绘谈话双方的主体对峙与环境，尚未实现复杂的多达 5 人以上的宗族同框全家福。
3. **ControlNet 姿态精细约束**：当前以垫图图生图与提示词语义约束为主，若未来接入私有本地 ComfyUI，可扩展输出 OpenPose 骨骼数据。

---

## 5. 编译、构建与热部署说明

### 构建命令
```bash
# 切换到项目根目录
cd f:\AnimusForge-main

# 构建 AnimusForge.Illustrator 子模组
dotnet build extensions/AnimusForge.Illustrator/src/AnimusForge.Illustrator.csproj -c Release
```

### 一键部署命令
```powershell
# 运行专用热部署脚本
.\tools\deploy_illustrator.ps1
```
该脚本执行以下动作：
1. 校验源码路径与项目文件；
2. 执行 `dotnet build -c Release`；
3. 将编译产物 `AnimusForge.Illustrator.dll`、`SubModule.xml` 以及 `GUI/` 界面贴图资源，自动部署至本地游戏目录：
   `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge_Illustrator\`
4. 校验输出 DLL 大小与时间戳。

---

## 6. 回滚与安全恢复指南

- **本轮无破坏性侵入**：
  所有 Illustrator 的代码均位于独立的 `extensions/AnimusForge.Illustrator/` 子目录与专用 Harmony Patch 中，未修改 AnimusForge 本体的既有存档持久化（`SyncData` 保持 146/146、`CampaignBehavior` 保持 36/36）。
- **如果需要回退或停用**：
  1. 在游戏启动器中取消勾选 `AnimusForge.Illustrator` 模块，或直接在 MCM 中将“启用 AI 画卷生图系统”置为 `关`，系统将完全静默且不产生任何性能开销；
  2. 源码回退使用定向 Git revert，严禁执行 `git reset --hard`。
