# 生图子模块全量审查、画风与离屏场景修复

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。用户要求修改画风、消除可见转镜、修复发型，并审查整个子模块的代码与提示词。

检查点 `cd1b1cfa` 保存任务开始时已有的五个 Illustrator 未提交文件，不应撤销；实现和测试提交 **`d7061300`**，场景事实复查收尾 **`e337754a`**。未部署、未推送、未调用真实模型；游戏目录仍是 2026-09-18 16:36 的版本。

## 审查范围与已处理问题

审查覆盖全部 33 个 C# 源文件（Context 7、Core 7、Engine 9、UI 8、Settings/SubModule 2）、五个 GUI prefab、项目文件与 SubModule.xml。检查实际日志、最近缓存、原版 API 和完整调用路径；不是仅审查本轮 diff。以下列出确认的问题和本轮处理，不表示所有潜在缺陷已经排除。

| 确认问题 | 修改结果 |
| --- | --- |
| 两端重复维护旧的笼统油画词 | 统一画风预设，古典油画导演 314 字具体画法、生图端 91 字简短要求；本地 fallback 也用短版。其他预设保持原语义，自定义已保存文本不覆盖。 |
| 四方向采集直接接管玩家 CustomCamera，引发可见跳转 | 改为独立 Camera、SceneView 和 render target，借用原 Mission Scene；不操作玩家镜头、UI 可见性或输入。 |
| 发色偏移被硬解释为固定发色/微卷，年龄推出白发，胡须固定写成短髭 | 移除推断，须发有无、长短、分缝、束发和耳颈轮廓由原始人物参考图确定；导演与生图端使用同一外观优先规则。 |
| 普通 NPC 被统一描述为壮年战士；未知年龄硬填 28 | 优先读取真实外观年龄，未知明确保留未知；删除身份推断的体格和衣服外露身体细节。会话/周报参考最长边从 512 提至 768，原舞台大小不变。百科此前已经是 768，不能将其问题归咎分辨率。 |
| “野兽人”先命中“兽人”，sorcerer 可误命中 orc | 更具体中文标签优先，英文标识按字母数字边界匹配；未知 Race 不硬写人类。 |
| 精确指定 images/edits 却发送 JSON、没有 multipart 图像 | 精确 Edits 上传实际参考图，保留 URL query；缺少有效参考本地失败，不发请求；精确端点失败不自动换端点。 |
| Chat 成功缓存漏记参考角色、整幅重绘等实际发送文本 | ResolvedPrompt 从真正发送的消息按顺序提取全部文字，不含 base64；画廊查看的是完整实际文本。 |
| 真实超时被错误标成用户取消，周报可重复点击发请求 | 在清理用 Cancel 之前读取原取消状态；周报生成中忽略重复重绘点击。 |
| 周报未知地点读取玩家当前地形；会话未输出已读季节 | 历史事件无地点时保持未知；补回已确认季节。 |
| 定居点被围就将街道硬写成城门上下谈判；Location 缺失被当作平地阵前谈判 | 仅保留确证的围城/场景信息，地点未知不添加地形或城墙高差；酒馆不推避难、牢房不推囚禁，宿主明确的当前上下位置事实仍保留。旧建筑/光线模板确认不进入发送链路。 |
| 四个展示区将横/竖图拉成正方形；画廊选图后主题不可见 | ImageFit 使用 Contain 保留整图比例；主题使用选中详情区的独立绑定。 |
| 四段存在即可通过，装备目录可压过环境描述 | 本地检查环境/光线/空间三段合计不少于人物段，并向导演说明分配；不合格仍走已有本地 fallback，不付费重试。 |

## 发型证据与边界

`rgl_log_14016.txt` 的 16:48–16:51 请求表明人物参考确实发送。拉盖娅百科两张实际缓存（`IllustratorCache/HccAX3011oX4/encyclopedia/`）分别为 `8e537a86b9b0b09b641d29fe1288f151_20260918084915998_c3653a`、`8e537a86b9b0b09b641d29fe1288f151_20260918085008907_13b591`；导演正文分别描述收拢/梳挽与披到肩后的长发，图片也随之变化。确定有导演外观漂移，不能归因于漏发参考。

当次原始离屏人物 PNG 已被现有流程清理，无法证明哪张成图等于当时游戏发型。BodyProperties 传递未发现丢失发型键。本轮未重写其原生造型渲染，也不承诺仅凭提示词保证每张图发型准确。

## 离屏采集与生命周期

- 一次主动生成最多五张：当前相机角度/FOV，加同位置四向 100° 水平视角。场景图仅发导演，人物和纹章参考继续沿原链路发给生图模型。
- SceneView 绑定自有最长边 1024 的纹理，渲染由引擎调度，不新建场景、不手动 tick、不调整共享场景曝光/光源。完整 postfx/shadow 渲染路径准备后才请求原生异步 PNG 导出，严禁 Texture.GetPixelData/SaveToFile 直接读取 GPU render target。
- SceneCaptureLock 与原人物/纹章 `_stageLock` 串行；每方向等待实际应用帧、检查 ready，有 12 秒总预算和单文件有界等待，支持 `.png` / `.png.png`。失败保留已取得视图，正常失败且无图时尝试一张当前截图，不隐藏 UI，不回退可见转镜；取消/退出不会强取截图。
- 退出只 `AddClearTask(clearOnlySceneview: true)`，释放自有相机/目标，绝不清空 Mission Scene。Reset/Shutdown 保持接线；新增 MissionLogic 在 OnEndMission/OnRemoveBehavior 退休额外视图，并拒绝 EndingNextFrame/Over 或场景身份改变后的操作。
- 所有原生对象读取/创建/清理在主线程；落盘等候、图片解码在后台。每个 Mission 只增加无 tick 的生命周期行为；没有持续截图、全场景逐帧扫描或新的付费重试。额外视图的 GPU 开销需实测，不能把“不转玩家镜头”说成“零帧率影响”。

## 已核实源码坐标

以下均对照最终源码提交 `e337754a`（主要实现始于 `d7061300`），源码前缀为 `extensions/AnimusForge.Illustrator/src/`。

| 路径 / 行号 | 符号、责任与边界 |
| --- | --- |
| `Core/IllustrationStylePresets.cs:3–78` | `IllustrationStylePreset` / `Resolve`，统一画风两端文本与 API 枚举；不改用户设置。 |
| `Core/VisualDirectorEngine.cs:242–262,569–579` | `HasRequiredSceneDescription` / `BuildDirectorStylePreference` / `BuildImageStylePreference`，环境占比、长短画风接线。 |
| `Core/UniversalOpenAiImageClient.cs:90–180,319–340,560–578,646–661` | 主请求路由、精确 Edits、实际 Chat 文本提取；HTTP 由离线 handler 验证。 |
| `Context/HeroVisualExtractor.cs:233–275,662–681,789–797` | `DescribeSpecies` / `ContainsIdentityToken` / `ResolveAppearanceAge` / `BuildPhysicalFeaturesDescription`，删除伪事实与物种子串误判。 |
| `Context/ConversationContextExtractor.cs:196–216` | 普通 NPC 快照年龄、物种与外观事实使用同一来源。 |
| `Core/VisualFidelityRules.cs:5–17` | `DirectorAppearanceFidelity` / `CharacterAppearancePriority`，须发形状及遮盖约束。 |
| `Engine/ScreenCaptureHelper.cs:1218,1279` | 两个人物离屏入口默认最长边 768；原角色舞台不变。 |
| `Engine/ScenePanoramaCapture.cs:57–159,173–298` | 采集、落盘读取、`PanoramaSession` 所有权与退休；原生 GPU 尚未验收。 |
| `SubModule.cs:53–65` | `SceneCaptureLifetimeBehavior`，Mission 结束前退休离屏采集。 |
| `Core/IllustratorRuntime.cs:274–295` | 完成回调的取消/超时分类，保留 revision 与 scope 隔离。 |
| `Context/EnvironmentVisualExtractor.cs:46–52,124–134,222–277,389–394` | 季节、事件地点未知与围城场景事实边界；无 Location 不判定平地。 |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs:135–140` | 生成期间忽略重复重绘。 |
| `UI/Gallery/IllustratorGalleryPopupVM.cs:167–178,249–255` | 选中主题绑定，不依赖空状态 StatusText。 |

GUI 前缀 `extensions/AnimusForge.Illustrator/GUI/Prefabs/`：`ConversationIllustrationOverlay.xml:24`、`EncyclopediaIllustrationOverlay.xml:24`、`WeeklyReportIllustrationOverlay.xml:23`、`IllustratorGalleryPopup.xml:74,82` 对应比例和主题显示。

## 验证

1. API 1.3 和 1.4 Release 构建均 **0 警告 / 0 错误**。
2. 两份实际 DLL 各执行以下四组离线检查，分别 **299 PASS / 0 FAIL**：PromptRouting 144、ClientEndpoint 108、ModuleReview 33、SceneCapture 14。
3. 真实 HTTP 请求内容在内存中捕获；实际 scope.Run/Close/Tick 测超时和晚回调；缓存使用验证目录；图像比例调用原版纯托管 ImageFit。场景测试检查编译后调用边界和落盘读取，不执行原生渲染。
4. 两份实现的离线加载都使用当前安装游戏的托管依赖；不能把 API 1.3 DLL 测试称为 1.3 游戏/GPU 验收。相关 API 和 ImageFit 已比对双版本源/引用。
5. 证据和产物位于 `artifacts/illustrator-review-20260919/`，构建为 `build-{1.3,1.4}.txt`，审计为 `{prompt_routing,client_endpoint,module_review,scene_capture}-{1.3,1.4}.txt`。

DLL SHA256：

- 1.3：`5555E8724D0731EB023ED0CF3F14BBEFED7B2AC590D22E4FB2AD54BB338D751D`
- 1.4：`4DAF5B84A0B3119F938F65AA5FF1BDEA9AE2DF7FD12D982E2F605593A7D045A4`

未实机验证：独立 SceneView 渲染/导出正确性、颜色、取消/切场景时的 GPU 清理、帧率、画廊主题布局、真实导演的发型和姿态保真、油画效果。尚未进行付费对照生图。历史服务端空 completion 也不能由这些离线检查宣称解决。

## 回滚

按逆序定向 revert `e337754a`、`d7061300` 可撤销本轮代码与测试；不要撤销保存原有工作区状态的 `cd1b1cfa`。本轮未覆盖游戏模块，游戏仍是上一部署版本，若需回退其部署可使用之前记录的 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260918-163653`。其他作者的 ShoutBehavior、TtsEngine、dump 工具及未跟踪素材保留。
