# 导演行动、作品命名与多视角现场参考

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。
行动/命名生产提交 `987fa696`，intent `ac0a33c`；场景生产提交 `192098f2`，修改前检查点 `ec38ea99`（保存此前 ScreenCaptureHelper 用户改动）。已于 2026-09-18 16:36:53 按用户授权部署，未推送；详情见部署节。

## 行动与标题主题

- 导演先明确人物正在做什么，再推导身体姿态、入镜手部动作、视线与镜头。保留自然站姿，去除多处重复强调沿用站姿的规则，不强迫复杂肢体动作。
- 同存档、同人物/事件最近三张画作的行动摘要进入导演参考；旧缓存从人物与镜头段提取首尾，兼顾位于装备段落后面的手势。百科减少重复的展示姿势；会话和周报明确要求保留本次事实，不能为去重改变事件或引入道具。
- 导演输出画作标题、主题、人物行动三个记录字段和四段生图正文；字段解析后与正文分离。标题显示于卡片标题栏和画廊条目；主题显示于卡片/画廊状态区。缓存保存 Theme、ActionSummary，旧缓存仍能加载。缺少命名字段或导演失败时保留原有通用标题，不将失败构思的元数据绑定到回退图。
- 通用负面词缩为七项：extra limbs、malformed hands、bad anatomy、unwanted text、watermark、UI overlay、reference image collage。删除全局禁止露脸、头饰及面具专用词，保留画风所需负面词和用户自定义输入。
- 正常生图路径仍仅使用导演视觉正文和参考图；标题主题不要求绘入画面，画廊提示词不混入导演背景和历史行动。

## 会话环境参考

用户酒馆对照中，生成图未保留红砖拱墙、抹灰主色、楼梯与桌椅关系。原截图只截窗口上半部分，已改为完整客户端区域、最大边 1024。会话生成最多采集五张：当前视角一张，加同一拍摄点原地水平转向的 0/90/180/270 度四张，水平视野 100 度；作为单独图片带方向说明发送导演，不拼成变形的球面图。人物/纹章参考仍发送生图端，场景截图只发导演。

使用现有 MissionScreen/CombatCamera 渲染链路，临时 Camera 只提供参数，不新建 Scene/TableauView、不调用原生图像导出、不移动玩家或相机位置。读取实际渲染相机，确认方向、位置和视野后才接受截图；其他相机接管、原生固定相机/缩放阻止预期视角时停止补图。大地图会话、已有自定义相机、场景未开始渲染时保留完整单张视图。

采集使用原 SceneCaptureLock 串行，隐藏生图卡片/画廊/周报叠层；每次转向等待三个应用帧，不忙轮询。主线程 finally 恢复视角/FOV和界面；Reset/Shutdown 的现有 CancelActiveStage 入口也恢复活动采集。其他系统已接管相机时不覆盖其新相机。原版 HUD、字幕等可能仍在截图中，参考标签要求忽略；未宣称截图彻底无 UI。

导演新增环境约束：同一空间多视图不能变成多个房间/重复人物；保留墙面材质配色、楼梯所在墙面及走向、门窗、层高和桌椅关系，地点名称不作为重新设计建筑的许可。

## 源码坐标

以下以 `extensions/AnimusForge.Illustrator/src/` 为前缀，行号核对最终生产提交 `192098f2`（行动元数据实现始于 `987fa696`）。

| 路径 / 行号 | 符号与责任 |
| --- | --- |
| `Core/IllustrationDirection.cs:16–66` | `SplitMetadata`、`ExtractActionSummary`、`BuildActionHistory`、`ReadEventActionHistory`；字段分离、截断净化和历史上下文 |
| `Core/VisualDirectorEngine.cs:74–88,114–163,181–237` | system 指令、`CreateDirectionAsync`、`ResolveDirection`；旧 string 返回接口保留，拒绝输出时丢弃元数据 |
| `Core/UniversalOpenAiImageClient.cs:320–335` | `BuiltinNegativePrompt`、`BuildEffectivePrompt`；简短通用负面词 |
| `Engine/DiskImageCacheManager.cs:19–24,131–158` | 缓存新增字段与 `SaveImage` |
| `UI/Overlays/IllustrationCardPopup.cs:200–223,264–283,356,391–392,441–460,487` | 历史、元数据、场景采集接线与卡片显示 |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs:315,357–376,415` | 周报同样接入命名、主题及历史行动 |
| `UI/Gallery/IllustratorGalleryPopupVM.cs:236–239` | 选中作品的标题/主题显示，Prompt 保持原发送正文 |
| `Engine/ScenePanoramaCapture.cs:22–111,126–210` | `BuildPanoramaFrame`、`IsUsablePanoramaFrame`、`CaptureConversationSceneReferencesAsync`、`PanoramaSession`；采集、主线程所有权、恢复 |
| `Engine/ScreenCaptureHelper.cs:969–975,1390–1411` | Reset/Shutdown 恢复接线；完整客户端截图默认值 |

## 验证与性能

- 行动/命名切片：1.3 / 1.4 子模块 Release 构建均 0 warning/error，各 114 PASS/0 FAIL。
- 最终包含场景切片：双 API Release 构建均 0 warning/error，实际 DLL 各 **131 PASS/0 FAIL**。日志在 `artifacts/illustrator-panorama/audit-1.3.txt`、`audit-1.4.txt`；DLL 在对应 `1.3/`、`1.4/` 子目录。
- 离线覆盖：元数据与正文分离、不合格导演输出回退丢弃错误标题、实际缓存落盘重载/旧元数据兼容、最近三张非删除记录和旧提示词回退、Chat/Edits 请求、负面词冲突消除；四向数学变换原地/水平、垂直初始视角兜底、渲染相机错位/缩放拒收、截图全高参数。
- 缓存测试将 CacheBaseDir 重定向至 DLL 产物旁的唯一 cache-audit 子目录，不读写玩家图片。HTTP 使用内存 handler，没有供应商调用。
- 原版 1.3 / 1.4.5 源码确认 Camera 的创建/视野/释放及 MissionScreen 的 CustomCamera 参数复制流程，双引用构建通过。没有手工 tick 第二场景。
- 工作只在主动生成时执行：历史使用已有缓存列表，最多三条摘要；每次最多五次屏幕截图，额外四张图片会增加导演视觉输入成本。没有逐帧全场景扫描、重复反射或自动付费重试。

## 尚未验收与回滚

未进行游戏/GPU验收：转镜可能短暂可见，截图采集期间人物可移动；正常完成/取消/切场景/相机接管时的实机恢复、真实环境准确率、标题显示布局和导演姿态多样性需实测。数学/缓存/请求测试不代表原生生命周期已验证，也不保证模型服从。多图视觉不支持时沿用现有文本降级。镜头被其他系统占用时有意只保留单张，不伪称完整环视。

两次历史空回复未保存完整失败请求，现有证据只确认服务端 completion_tokens=0；删除冲突负面词不等于证明已修复服务端空回复。新版本未对供应商稳定性作结论。

源码回滚只定向、按逆序撤销 `192098f2` 和 `987fa696`；不要撤销保存用户改动的 `ec38ea99` 或重置工作树。游戏端回滚使用下节备份。

## 部署（2026-09-18 16:36:53）

- 用户明确授权后运行原 `tools/deploy_illustrator.ps1 -Configuration Release -BannerlordApi auto`，识别游戏 v1.4.8 / API 1.4，构建 0 警告/0 错误。只覆盖独立模块 `F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge_Illustrator`。
- DLL/PDB、SubModule.xml 和五个 prefab 共 8 文件与部署来源 SHA256 一致。DLL SHA256：`4939DEBD704A8B775302773C5BD634A59218ED50E948CE5D8383E3C23EC70371`。
- 将实际部署 DLL/PDB 复制到 `artifacts/illustrator-panorama/deployed-verification/`，确认 DLL 哈希相同后对副本执行审计，结果 **131 PASS / 0 FAIL**。缓存测试只写本地验证目录，没有向游戏目录写测试缓存，也没有真实模型调用。
- 证据：`artifacts/illustrator-panorama/deploy.txt`、`deployed-hashes.json`、`audit-deployed.txt`。
- 脚本覆盖前备份：`artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260918-163653`，包含上一版模块文件。回滚时从该目录恢复对应 DLL/PDB、清单和五个 prefab，再核对哈希。
- 部署构建包含当前工作区已有依赖改动；单个生产提交不是完整发布快照。无关未提交改动保留，未推送、未启动游戏。GPU、多向转镜与取消/切场景恢复、标题主题布局、真实模型姿态和环境还原效果仍未实机验收。
