# 纹章原生渲染替换与离线验证（2026-09-16）

源码/测试提交：`cf237202`；修改前检查点：`75d116aa`。工作树 `F:\AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。用户在旧纹章审计后明确授权“做”。本轮仅本地修改、构建和离线验证，未部署、推送或调用生图服务。

## 改动与原因

[旧离线审计](2026-09-16-illustrator-emblem-offline-audit.md) 中两份目标 DLL 各有 30 checks / 7 failures：掩码反相、配色契约矛盾、描边透明、旋转方向错误。本轮删除 CPU 图集切片及 shader 重建，将完整 BannerCode 交给原版 BannerTableau。旧失败证据保留，不能用新的模拟管线测试把旧视觉失败改记为 PASS。

现在的链路为：冻结的 BannerCode → 按完整代码与尺寸查缓存 → 主线程挂 Gauntlet 导出控件 → 原版 provider 渲染完整旗面 → TableauView 请求 PNG 落盘 → 文件大小稳定至少 50ms → 主线程拆层 → 后台通道归一化、必要缩小、无损 PNG → 已有参考图发送链路。

导出控件继承 BannerTableauWidget，`OnRender` 只设置下一轮 provider 更新标志，不调用基类绘制或屏幕 Draw。`IsNineGrid=true` 使用完整旗面。没有直接读取共享 GPU RenderTarget，也没有自行创建/手动 tick 原生场景。图案、描边、旋转、镜像与多色背景都交给原版实现。

## 已核实源码坐标

以下路径均相对仓库，源码修订 `cf237202`。

| 路径与行号 | 符号与责任 |
|---|---|
| `extensions/AnimusForge.Illustrator/src/Engine/BannerEmblemComposer.cs:7–16` | `ComposeToBase64Async / Reset`，薄适配器，移除旧 CPU 合成入口实现。 |
| `extensions/AnimusForge.Illustrator/src/Engine/NativeBannerPipeline.cs:9–102` | `GetAsync / Reset`，串行缓存、取消传递、存档代际校验、失败不缓存。 |
| `extensions/AnimusForge.Illustrator/src/Engine/NativeBannerImage.cs:13–75` | `Encode / NormalizeAndValidate`，完整 PNG 检查、一次 R/B 交换、缩小与无损编码；不重建图案。 |
| `extensions/AnimusForge.Illustrator/src/Engine/NativeBannerExportWidget.cs:8–18` | `OnRender`，保持 provider 更新但不提交屏幕绘制。 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs:824–846,855–1006,1053–1205` | `RunOnGameThreadAsync / OffscreenStagePump / PumpOffscreenStage / FinishStage / RetireStageAsync / ExtractViaStageAsync / RenderNativeBannerPngAsync`，主线程准入、文件稳定等待、原生导出、取消和拆层。共用舞台部分也影响人物离屏导出。 |
| `extensions/AnimusForge.Illustrator/src/Core/IllustratorRuntime.cs:114–122` | `Reset`，取消旧纹章任务、清缓存并在主线程退休活动舞台。 |
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs:200–208,350–359` | 百科与会话参考图传递请求 token。 |
| `extensions/AnimusForge.Illustrator/src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs:332–339` | 周报参考图传递请求 token。 |
| `extensions/AnimusForge.Illustrator/src/Settings/IllustratorSettings.cs:62–64` | `AutoCleanTempFiles`，更新实际文件清理范围说明，开关语义不变。 |

原版链路调查来源为仓库 `原版游戏本体代码1.4.5` 下的 `BannerTableauWidget`、`TextureWidget.UpdateTextureWidget / OnUpdate`、`BannerTableauTextureProvider.Tick` 和 `BannerTableau.OnTick / SetTargetSize`。控件尺寸/可见性仍由原版 Gauntlet 正常处理；这份反编译源码不是原生 GPU 验收证据。

## 生命周期与性能

- 仅生成参考图且缓存未命中时创建舞台，不做持续全场景扫描。主线程队列沿用每 Tick 至多两个动作，纹章与人物共用全局串行舞台锁。
- 缓存按完整 BannerCode + 尺寸标识，至多 32 项、PNG base64 字符串预算 8MiB，FIFO 淘汰；存档重置清空。失败不缓存，同代码并发请求在排队后复用成功结果。
- 纹章请求排队/导出携带 caller 与存档生命周期 token，20 秒本地取消期限；舞台预热 12 次 pump、至多 360 次 pump，开始后等待文件期限 6 秒。期限不强行跳过主线程拆层；活动舞台的安全退休仍依赖后续 Tick 或 Reset。未测实机耗时。
- 主线程工作未开始时取消能立即结束等待；已经开始则等 work 返回，防止创建舞台途中提前释放锁。锁只在 `Retired` 完成后释放；队列满不会伪装拆层成功，Reset 不需要再等队列 Tick 即可退休活动舞台。
- 文件可能在同帧被观察多次，改用至少 50ms 的大小稳定窗口；后台读文件最多四次，间隔 50ms。稳定窗口不是原生写入完成的正式 fence，实际引擎时序仍需验收。
- 自动清理只处理本次唯一前缀/输出文件；关闭时保留完整纹章 PNG。原生迟到落盘可能留下文件；不增加持久后台轮询。旧 `banner_debug` 不再生成，也不删除历史文件。

## 验证证据

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_illustrator.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_illustrator_emblems.ps1 -BannerlordApi 1.3 -OutputDirectory F:/AnimusForge-main/artifacts/tests/emblem-native-1.3
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_illustrator_emblems.ps1 -BannerlordApi 1.4 -OutputDirectory F:/AnimusForge-main/artifacts/tests/emblem-native-1.4
```

- 子模块 Release、`BannerlordApi=1.3` 与 `1.4` 均 0 警告/0 错误。通用回归 **180 checks / 0 failures**；数量从 185 变化是删去已移除的 CPU shader/图集与调试目录实现断言，加入原生入口断言。文件归属、场景事实、随机设置和响应解析测试继续保留。
- 两份目标 DLL 的专项测试各 **32 checks / 0 failures**，直接调用生产程序集中的纯托管方法：模拟完整渲染 PNG 的像素保持、损坏/空图拒绝、真实旧空图 fixture、缓存身份与容量、取消/晚结果、无屏幕绘制 override、队列停止与满队列退休。
- Harness 使用 C# 依赖解析回调并防止重入；旧 PowerShell 回调在异步测试进入 UI 类型加载时导致 StackOverflow。替换后相同步骤通过。
- `git diff --check` 通过。抽看模拟 PNG 与逐像素断言一致。

可复查文件：[通用日志](assets/illustrator-native-emblem-20260916/general-regression.log)、[1.3 结果及 DLL SHA256](assets/illustrator-native-emblem-20260916/results-1.3.json)、[1.4 结果及 DLL SHA256](assets/illustrator-native-emblem-20260916/results-1.4.json)、[模拟旗面](assets/illustrator-native-emblem-20260916/native-normalized-0.png)、[模拟旋转](assets/illustrator-native-emblem-20260916/native-normalized-1.png)、[模拟镜像旋转](assets/illustrator-native-emblem-20260916/native-normalized-2.png)。

测试运行时复用本机可执行的 TaleWorlds DLL，实际路径与程序集版本已写入各 JSON 的 `RuntimeDependencies`。这是两份不同编译目标 DLL 的离线托管检查，不等于分别启动了 1.3 与 1.4 客户端。1.3 编译引用为项目锁定的 `Bannerlord.ReferenceAssemblies 1.3.15.110062`。

## 未验收与回滚

未进行真实 GPU 导出或调用生图模型。模拟图片预先按既有人像导出约定交换 R/B，只证明后处理没有损坏已给定图像，不能证明游戏纹章 PNG 本身的方向/通道。真实国王/贵族、多层纹章、自定义 MOD、多色背景、重复取消/换存档、零闪屏、导出期间原生资源安全、人物共用舞台回归和模型遵循率仍待实机验收。

空图检测目前保守，刻意纯色或图案覆盖极低/颜色差异极小的有效旗帜也可能省略。只要原生渲染有内容，并不代表每层 MOD 徽记都存在；无法离线保证缺失 mesh 必然被发现。未做生成后纹章贴图，因此不承诺最终模型逐像素复制参考图。

本轮生产/测试回滚使用定向 `git revert cf237202`，修改前检查点 `75d116aa`。不要 hard reset 或回滚其他作者的喊话、TTS、信使图片等改动。
