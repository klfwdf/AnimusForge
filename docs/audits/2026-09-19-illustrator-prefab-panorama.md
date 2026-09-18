# 预制体六镜头全景：实机反例、替换实现与性能边界

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `c6da95b`；生产与测试 **`b63c10f8`**。用户明确要求玩家无感知的多个离屏镜头，导出并合成全景，重点还原当前预制体、装饰及第三方内容，并顾虑性能。本轮已于 **2026-09-19 05:44:52** 部署。

## 已证实的旧版问题

- `rgl_log_25528.txt:991` 确认实机加载的是 `1c92decf` 对应 MVID `70fac8d5-a1cd-4139-a423-89102346f0c2`，不是此前旧 DLL。
- 诊断 `20260918T203953_a098443ab2634860955703ce0cb42c3e` 五张声称不同方向的 PNG 实际都是同一正面大厅，仅动画略变；第三张 `579BEC...png` 的对面人物出现纵向破面。第二条诊断同样重复视角。旧代码只改 Camera.Frame，没有按原版用法重新提交 SceneView.SetCamera；数学方向测试没有覆盖原生相机提交契约。
- 同批原始场景 PNG 已发蓝，人物 JPEG 正常。对同一原生 PNG 作 R/B 对照后，墙体、帷幕和火光恢复到与用户现场对应的暖色。蓝色进入模型之前已经存在，不能通过修改画廊 UI 或交换整张成图通道修复。
- 副 SceneView 绑定了正在运行的 Mission.Scene。该 Scene 有共享的 LastFinalRenderCameraFrame/Position，私有 Camera 不等于独立渲染状态；不能据“没写 CombatCamera”宣称主画面不受影响。人物闪烁具体发生在 native 的哪一缓存/阴影/骨骼阶段，现有托管证据不能确定。

只读取证脚本 `tools/illustrator/inspect_scene_reference_regression.py`，证据产物 `artifacts/illustrator-scene-regression-20260919/evidence/`，包括五图对照、原生颜色对照和原图 SHA。脚本不修改玩家原图。此前595项离线通过不等于这条原生采集链已经实机通过。

## 最终实现

**当前实例预制体 → 独立冻结环境快照 → 六个90°相机顺序导出 → 360×180投影全景 → 导演和生图端。** 单张屏幕截图仅作为颜色、光照和人物关系校验，不能替代全景。

1. 从实际已加载 Scene 枚举实体，使用 `GameEntity.CopyFrom(target, source, false, false)` 保留原网格、材质、摆放和可见状态；不按 prefab 名称重新实例化、不按文化模板搭景。第三方普通静态实体沿同一原生路径处理。
2. 所有相机只渲染新建且自有的 Scene，源/目标场景和实体指针须不同。目标禁止物理、脚本回调、布料模拟及手动 Scene.Tick。真实 Agent/坐骑骨骼不复制；混合层级拆分保留最大安全静态子树及父节点自身静态网格，不因一个骨骼丢弃整座建筑。
3. 采用原版 `TableauView.AddTableau` / `PaintNeeded` 调度。每面一个固定相机，每次真实 paint 重新提交相机；至少三次 paint 后才允许导出。导出未完成不切面；完成后立即停用持续渲染。postfx、shadow和focused shadowmap仅初始化在自有 View；清理走引擎延迟清理队列。
4. 六张方形原生 PNG 在 `PanoramaProjection.Compose` 入口做一次该生产者专用 R/B 适配，再作双线性 cube→equirectangular 投影，默认2048×1024。不是六图拼贴；缺面、全部近似重复或超预算均明确失败，不发送假全景。标准截图、人物JPEG、HTTP结果和UI加载不增加换色。
5. 将全景和至多一张当前实景校验图发给导演、生图端，标注全景展开不能照搬为最终构图，不把它当多个房间或额外人物。用户关闭生图参考开关时仍尊重设置。
6. 当前画面截图只读客户端像素，在副本中遮除 AF 面板，防止旧成图污染环境参考；未知面板或遮挡过半时不提供校验图，不实际隐藏/移动 UI。人物离屏导出使用不提交屏幕 Draw 的控件，保留原生 provider 生命周期，不再靠 Alpha 或2像素裁剪承诺不可见。

## 覆盖范围与性能

- 全景主要覆盖普通静态预制体、装饰、可复制局部灯光和其真实摆放；**不含原生 terrain 高度场/混合材质、水面模拟、真实人物骨骼及完整实时光照**。混合骨骼父节点自身的粒子/布料/特殊灯光可能省略，均写入 Notes 和诊断。
- 副本使用中性材质观察光和固定曝光，只作网格、材质图案与布局依据；现场颜色、时段和光照优先依据附加真实画面。没有声称公开API能完整复制现场 atmosphere。第三方自定义脚本/特殊着色器也未实机验收，不能保证所有模组内容等价。
- 只在用户生成时执行。根句柄仅枚举一次，选择过程线性分帧；每批最多64个检查步骤、8次原生复制、4ms软预算。单次native调用不可抢占，4ms不是硬帧时保证。
- 上限4096源根、32768检查节点、1024实际副本；总采集预算25秒。超限停止并明确报错，不偷偷删掉环境再称完整。没有常驻全场景扫描，没有用旧环境缓存冒充当前事实。
- 六个512×512镜头顺序共用一个 RT，阴影分辨率倍率0.5；CPU拼接放后台。独立离线2048×1024合成约0.16–0.19秒，不包括游戏GPU采集，也不是实机FPS结论。
- 记录每面耗时、复制批次/最大批耗时、实体计数、应用帧间隔、缺失范围；原生六面证据另限4MiB，仍受总诊断15MiB参考限额约束。原生原图明确标“转换前证据”，不混称模型实际收到的参考。
- Reset/Shutdown 同步退休正在创建的副本和渲染器；`Retirement/Retired` 表示清理或所有权移交已提交，**不是 GPU fence**。

## 核实源码坐标

以下以 `extensions/AnimusForge.Illustrator/src/` 为前缀，均对应 `b63c10f8`。

| 文件 / 行号 | 符号与责任 |
| --- | --- |
| `Engine/SceneReferenceCapture.cs:47–169,172–201` | 主流程、六面读取、后台合成、诊断和取消清理；不直接渲染 Mission.Scene。 |
| `Engine/PanoramaSceneSnapshot.cs:34–77,83–139,187–329,335–386` | 覆盖说明、所有权、分批实际实体复制、混合树拆分与预算。 |
| `Engine/IsolatedPanoramaRenderer.cs:103–198,207–294` | Create/SelectFace/Paint、导出锁面、StopExport、独立清理失败收敛。 |
| `Engine/PanoramaProjection.cs:28–68,73–181,191–242` | 六相机帧、方向映射、合成、一次原生通道适配及重复检测。 |
| `Engine/PassiveSceneScreenshot.cs:19–103`，`Engine/SceneScreenshotMask.cs:1–92` | 只读真实像素与截图副本遮罩，非全景替代。 |
| `Engine/NativeCharacterExportWidget.cs:7–19` | 原生人物 provider 更新保留，屏幕 Draw 禁止。 |
| `Core/IllustrationReferenceRouting.cs:25–39` | 全景和当前实景两种参考路由；角色参考不被当环境。 |
| `Core/IllustrationReferenceImage.cs:3–11`，`Core/VisualFidelityRules.cs:34–48` | 追加 ScenePanorama 角色及用途，不改旧枚举值。 |

`ScreenCaptureHelper.ExtractViaStageAsync` 动态创建新人物控件，`CancelActiveStage` 接入 `CancelIsolatedPanorama`；SubModule启动日志标记 `sceneCapture=isolated-prefab-cubemap`，原Tick仅对活动采集累计常数个帧时计数。`GenerationDiagnostics.RecordPanoramaFace` 保存限量原生证据。

## 验证、部署与回滚

- API1.3/1.4 Release均 **0 警告/0 错误**；每版14组审计 **759 PASS / 0 FAIL**。实际部署DLL同字节副本再次759/0。分组：Prompt137、Scene27、Client108、Module35、Head30、Director79、Cache72、Reference59、Diagnostics61、Projection65、Snapshot23、Renderer29、Passive22、NoDraw12。
- 覆盖实际HTTP内存请求、完整六面解析球/颜色/接缝/极点、预算、选择与取消、编译IL所有权、截图RGBA和无屏幕Draw。未调用付费模型，未执行真实GPU；1.3产物离线仍使用本机托管依赖，不能视为1.3实机验收。
- 用原 `tools/deploy_illustrator.ps1` 覆盖独立 `Modules/AnimusForge_Illustrator`，没有改主模组、游戏原版DLL或编译覆盖流程。DLL/PDB、清单和五个prefab **8文件哈希一致**。
- 部署SHA256：`5E381C8910F16A5A47F5A9933CEA1CC771490E9229F08D70A444C5283D38429A`；MVID：`0f61f17a-693d-48c3-adc9-c3be45ebfba1`。
- 产物/日志：`artifacts/illustrator-prefab-panorama-20260919/` 的 `build-*`、14组 `*-1.3/1.4/deployed.txt`、`deployed-hashes.json`、`deployment-manifest.json`。
- **尚未实机验证**：六面GPU实际取景与导出、复制资源持有和退出稳定性、人物闪烁是否消失、实际帧率、第三方素材和最终模型环境还原。旧蓝色成图属于已生成内容，不改旧缓存；需新版重新生成验证。
- 源码只定向 revert `b63c10f8`。游戏备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-054452` 保存的是已出现蓝色/闪烁的旧版，用于回滚与复现，**不标为稳定验收版**。其他作者修改保留，未推送。
