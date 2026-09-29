# 生图五项优化与现场参考联动部署

当前工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `6ccc335f`，生产与测试提交 **`1c92decf`**。用户确认五项优化全部实施，并继续反馈场景跳镜头、不还原及此前修复尚未部署；本轮已实际部署。

## 本轮行为

1. **每次生成有诊断记录。** 三个生成入口的 scope 创建独立 AsyncLocal 记录，保存实际导演/生图请求文字、模型、协议、状态、返回结束标记/用量、耗时，以及真实发送参考图的字节、SHA256和尺寸。成功、失败、取消都记录；响应中的大图 base64 不重复保存。记录加载中程序集 MVID/Version，避免将磁盘新 DLL 当作进程已加载新版本。
2. **人物增加原生头肩细节图。** 同一外观/装备快照顺序生成全身和 768×768 头肩图。使用原版 `CharacterTableauWidget.StanceIndex` 的 `EmphasizeFace` 原生镜头，不是裁剪放大；不摘头盔、不补画装备隐藏的头发。头肩失败保留全身，取消正常传播。`CharacterDetail` 是已有同名人物的补充，Chat 不把它计为新人物，Edits 同样保留用途标签。
3. **百科无载体时提前跳过纹章采集。** 仅 Head/Body/Leg/Gloves/Cape 实际穿戴槽的原生 `IsUsingTableau` 作为百科载体证据，包含披风等模组装备。家族旗帜归属不触发采集。会话/周报原有现场纹章责任保留，没有全局删旗帜。
4. **导演降级可见且可追踪。** 区分 `complete`、`vision_unsupported`、`truncated`、`local_fallback`，保留原因/结束标记/用量。明确截断的输出不采用残文和错误标题，沿原有本地 fallback；不新增付费重试。只在明确不支持图像输入时保留一次文字降级，普通 vision timeout 不再被误判。状态显示于生成中、成功卡片和画廊，旧缓存兼容，状态元数据不进入生图正文。
5. **缓存查询和画廊加载优化。** 按存档/分类/subject及图片ID建立元数据索引，返回副本，保存/删/设默认时失效；每次查询最多检查五个目录时间戳以识别外部增删恢复，没有每帧扫描。首次扫描仍同步，手工原地编辑 JSON 需 forceRefresh。画廊后台有界读盘并通过原颜色准备函数验证一次，主线程只注册已验证纹理；每窗口最多一个解码和一个最新待选任务，快切/关闭/场景切换丢弃晚结果。

### 场景反馈的证据与追加处理

用户反馈时游戏 DLL 仍为 `4939DEBD...`（9 月 18 日 16:36 部署）。`rgl_log_492.txt` 的 04:09:52 记录 `Scene references captured=5, panoramicDirections=4`，证明仍运行旧可见转镜版本。04:09:53 导演收 8 图，04:10:04 生图只收 2 人物图，环境只能经文字转述。

本轮一并部署前轮独立 SceneView 修复：玩家相机和 UI 不被接管，Mission 结束前退休额外视图。新增路由将全部现场方向送导演，同时把第一张实际取得的现场图送生图端，锁定空间布局、材质、固有色和高低关系，要求按导演画风重绘而不复制截图/UI；用户关闭生图参考开关时仍尊重开关。此变化覆盖此前场景图只送导演的规则，已更新 scoped AGENTS。

诊断还记录每方向渲染等待、导出等待、图片字节、总采集时长及采集期间应用帧均值/最大值。这是应用帧采样，不是 GPU profiler；后续据实测决定是否减少持续渲染窗口，未以牺牲环境信息换性能。

## 诊断存储边界

位置：`IllustratorCache/Diagnostics/<UTC时间_GUID>/trace.json` 和内容哈希命名的 PNG/JPEG，独立于画廊存档文件。默认文档目录通常为 `C:/Users/29310/Documents/Mount and Blade II Bannerlord/AnimusForge/IllustratorCache/Diagnostics`。

- 最多 12 份记录，每份最多 15 MiB 参考图、512 KiB JSON、48个阶段事件；超限明确标记省略，不伪称完整保留。正在生成的记录不清除；清理只限诊断根下本模块格式的目录，不动其他目录和画廊缓存。
- 不保存鉴权头；注册密钥只留内存用于脱敏，URL用户信息/query及凭证回显被净化。不会修改真正发送的请求对象或参考字节。
- JSON 原子更新，写入失败不阻断生图；Dispose后晚到图不再落盘，AsyncLocal恢复，故障也不遗留活动记录锁。
- 历史生成的参考图不能追溯恢复；本功能从新版实际运行后开始提供记录。诊断能力不等于修复供应商空回复。

## 已核实代码坐标

以下相对 `extensions/AnimusForge.Illustrator/src/`，对应生产提交 `1c92decf`。

| 路径与行号 | 符号与责任 |
| --- | --- |
| `Core/GenerationDiagnostics.cs:21–90,97–225,244–318` | Begin、请求/响应/参考记录、Flush/Prune/Dispose；容量、脱敏、同请求隔离。 |
| `Core/IllustratorRuntime.cs:273–288` | Scope.Run 自动建立诊断并区分完成/失败/取消。 |
| `Core/UniversalOpenAiImageClient.cs:66–84,214–221,408–412,621–626` | 请求ID和真实两种协议发送/回复/结果记录。 |
| `Engine/CharacterPortraitReferences.cs:12–92,95–148` | typed 双图、顺序/取消、共享真实快照、原生头肩镜头；旧字符串API继续包装原全身路径。 |
| `Core/IllustrationReferenceRouting.cs:9–33` | 同名人物全身/细节分组、选一张实景交生图。 |
| `Core/IllustrationReferenceImage.cs:3–10`，`Core/VisualFidelityRules.cs:34–46` | CharacterDetail 追加枚举，保留旧值与细节用途。 |
| `Context/HeroVisualExtractor.cs:619–642,680–686` | 按实际穿戴槽的 IsUsingTableau 判断纹章载体。 |
| `Core/IllustrationDirection.cs:15–37` | 状态、原因、用量及短显示文案。 |
| `Core/VisualDirectorEngine.cs:120–212,527–566,636–710` | typed导演流程、结束标记解析、单deadline及明确视觉不支持降级。 |
| `Engine/DiskImageCacheManager.cs:19–43,109–141,204–289` | 诊断/状态缓存字段、元数据副本和查询索引、目录失效。 |
| `Engine/GauntletTextureLoader.cs:44–75,169–206` | PreparedImage只能经固定颜色准备函数产生，后台准备与主线程注册分离。 |
| `Engine/GalleryPreviewLoader.cs:9–101` | 单个后台任务/最新待选、取消与晚回调隔离。 |
| `UI/Overlays/IllustrationCardPopup.cs:234–267,380–437` | 百科/会话双图、提前过滤、环境图、状态缓存集成。 |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs:331–367` | 周报双图、诊断/状态缓存集成。 |
| `Engine/ScenePanoramaCapture.cs:24,110–111,146–149` | 采集性能记录；SubModule原Tick调用只在活动采集时累计常数个计数。 |

性能责任：人物每次最多额外一次原生头肩渲染和一张图片输入；场景生图端增加一张已有参考，没有额外付费模型轮次。诊断读写在后台请求链路；原生游戏对象仍只在主线程。缓存索引避免重复解析整类JSON，但首次打开和GPU注册仍须实机量测。

## 验证与部署

- API 1.3、1.4 Release **0 警告/0 错误**。
- 每份 DLL 九组离线审计 **595 PASS / 0 FAIL**：PromptRouting 144、SceneCapture 14、ClientEndpoint 108、ModuleReview 35、HeadDetail 30、DirectorStatus 79、CacheGallery 72、ReferenceRouting 52、GenerationDiagnostics 61。
- HTTP 全部内存拦截；诊断和缓存测试写仓库 artifacts 的隔离目录；双目标使用本机托管依赖执行，不能冒充1.3真实游戏验收。
- 2026-09-19 **04:27:38** 使用原 `tools/deploy_illustrator.ps1`，自动识别游戏 v1.4.8/API1.4；未改部署流程、主模组或游戏原版DLL。
- 游戏目标 `F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge_Illustrator`。DLL/PDB、清单、五个prefab **8文件哈希一致**。
- 实际部署DLL复制到 `artifacts/illustrator-optimization-20260919/deployed-verification/`，确认同字节后九组审计再次 **595/0**，避免测试缓存写入游戏目录。
- 部署 SHA256：`152DBB6A49DCF568FD1E26BE7F8669290A40CB2FA14842F6373A1DD2954D8712`。MVID：`70fac8d5-a1cd-4139-a423-89102346f0c2`，下次启动日志应带该实现标识。
- 证据在 `artifacts/illustrator-optimization-20260919/`：`build-*.txt`、九组 `*-1.3.txt`/`*-1.4.txt`/`*-deployed.txt`、`deploy.txt`、`deployed-hashes.json`、`deployment-manifest.json`。

**未验证**：真实GPU头肩镜头及特殊种族/头盔取景、离屏SceneView渲染/导出/退出稳定性和帧率、游戏主题/降级文案布局、供应商对发型/环境/画风的服从。没有启动游戏或付费生图；已有静态与托管测试不能替代这些实机验收。

## 回滚

源码定向 revert `1c92decf` 回到本轮检查点所对应的优化前状态（仍含前轮审查修复），不重置工作树。实际游戏回滚备份为 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-042738`，备份是此前旧可见转镜版本；恢复相应8文件后应再次校验哈希。主模组 ShoutBehavior、TtsEngine、dump工具和未跟踪素材的既有更改全部保留；未推送。
