# 原场景单镜头离屏试采

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `12b9c9c`，源码 **`03cb39ce0abb712283921d63baaa1af7dd7d1c16`**。用户明确批准先试一个原场景隐藏镜头；本轮交付可手动试采入口，尚未替用户操作游戏或生成PNG。

## 为什么独立试采

独立静态快照实图仍大面积黑且部分建筑未出现在复制明细。用户希望直接借原场景渲染。旧 `1c92decf` 曾借Mission.Scene建立副视图，导出重复角度和人物破面；旧SetDirection只改Camera.Frame/FOV却没有再次view.SetCamera，这是确切遗漏，但不能据此断言人物破面也已找到根因。

此次不把共享Scene实验接入普通付费生成。场景卡片新增独立按钮，只采一张和预览；原前后静态快照生成、野外管线、周报与人物离屏流程保持。

## 使用方法

1. 加载部署新版，进入真实Mission场景（例如原大厅）并打开会话插画卡片。
2. 卡片空闲时点击 **“单镜头试采”**。大地图对话没有此按钮；忙时禁用。
3. 图像成功后直接显示在卡片，标题“单镜头试采预览”。查看家具、建筑、地面和人物是否正常，同时观察主画面有无跳动、闪烁或破面。

试采按钮不请求模型、不写图库、不设默认图；预览纹理注册失败保留旧图。普通重绘仍调用原流程；首次打开无缓存卡片的自动生成行为未改。不能把上述“不请求模型”扩大为所有打开卡片行为都不请求模型。

## 实现与边界

- 自有Camera、短ASCII名称 `afi_pXXXXXXXX` 的512方形RT及SceneView，借用实际Mission.Scene。复制当前相机的帧、水平FOV和裁面到自有相机，固定原朝向，aspect=1；不是两镜头或完整屏幕画幅。
- 不创建Scene/SceneLayer、不AddLayer、不切换或写CombatCamera/CustomCamera，不调用源Scene的光照/曝光/postfx修改或Tick，不复制网格。不使用桌面截图作为试采回退。
- 自有view采用普通引擎调度；准备信息在启用渲染前持久化。实验Tick和导出前明确提交自有Camera；准备就绪及若干应用帧只作导出门槛，完整PNG读回后停用视图。原生导出使用既有单一生产者的R/B适配一次，预览经标准图片加载，原图与适配图均记入诊断。
- 保留view普通阴影/postfx通路，不主动设置focused-shadow区域。原版SceneTableau正常渲染明确关闭focused shadow，但它没有PNG导出调用，不能当本实验已验证的证据。也未证明此前空指针因缺focus引起。
- 原SceneCaptureLock与人物舞台锁串行，8秒取消预算。实验Tick核对原screen/Mission/Scene及token，结束/切换/关闭会停止；Reset/Shutdown通过CancelActiveStage同步退休。清理只对自有view用 `AddClearTask(clearOnlySceneview:true)`，普通Texture.Release与Camera.ReleaseCamera，不对源Scene ClearAll或ManualInvalidate，不调用ReleaseImmediately。
- Readiness、应用帧数、AddClearTask提交均不是GPU fence。共享场景内部相机/骨骼/阴影缓存仍可能相互影响；这里只限制暴露时间并记录结果，不承诺玩家无感或稳定。
- 新增每帧工作仅在手动试采活跃期间执行，最多一个自有副视图；没有常驻附加渲染、场景扫描或模型请求。实际GPU耗时、卡顿和导出正确性尚未测量，8秒也不能抢占阻塞的native调用。

诊断仍在 `IllustratorCache/Diagnostics/<请求ID>/trace.json`，subject为 `shared-scene-single-camera-probe`。阶段包括 `shared_scene_probe_start`、`shared_scene_probe_view_ready`、`shared_scene_probe_images`、`shared_scene_probe_end`。images里的native/preview给出实际文件和哈希；包含相机矩阵/FOV/裁面、相机提交次数、应用帧间隔、总耗时及清理异常。只存采集证据，不把这些图自动发送模型。

## 核实源码坐标

对应 `03cb39ce`，前缀 `extensions/AnimusForge.Illustrator/`：

| 文件与行号 | 符号/责任 |
| --- | --- |
| `src/Engine/SharedSceneCameraProbe.cs:20–105` | `CaptureSharedSceneProbeAsync`：显式试采、锁/预算、导出读取、停止/诊断。 |
| `src/Engine/SharedSceneCameraProbe.cs:140–185,187–250` | `Begin`、`StartRendering`、`Tick`、`RequestExport`：自有相机与RT、反复提交Camera、owner校验、共享边界说明。 |
| `src/Engine/SharedSceneCameraProbe.cs:252–288` | `Retire` / `CleanupFiles`：只清自有view、释放自有资源和临时文件。 |
| `src/Engine/PanoramaProjection.cs:184–205` | `ConvertSingleNativeView`：单张原生PNG生产者适配。 |
| `src/Core/GenerationDiagnostics.cs:141–151` | `RecordSceneProbeImages`：保存原生与预览图，无模型发送。 |
| `src/Engine/ScreenCaptureHelper.cs:972` / `src/SubModule.cs:48` | Shutdown取消接线与活跃试采Tick。 |
| `src/UI/Overlays/IllustrationCardPopup.cs:120–137,347–387,519–531` | Mission-only按钮回调、试采scope及预览，注册失败保留旧图。 |
| `src/UI/Overlays/IllustrationCardVM.cs:115–118,168–174` | 按钮可见性、忙时禁用与命令。 |
| `GUI/Prefabs/ConversationIllustrationOverlay.xml:56–60` | 独立按钮，位于状态区与底部按钮条之间。 |

## 验证、部署与回滚

1.3/1.4 API与原版SceneTableau/MissionScreen用法已核对；生命周期只读复核、XML解析和diff格式检查完成。初次编译发现System.IO.Path与TaleWorlds.Engine.Path重名，添加明确别名后最终双API Release均 **0警告/0错误**。没有运行离线测试或付费调用。

按原 `tools/deploy_illustrator.ps1` 于2026-09-20 **06:36:17** 部署独立模块，8文件哈希一致；游戏v1.4.8选择API1.4。SHA256 `F472B300A9E7357AA2587BD9D68E47CE103D6512BE97A3C4A29984009FC9D03B`，MVID `7dbaf06b-e752-4469-8e56-7973435c6a62`。构建、部署及manifest位于 `artifacts/illustrator-shared-scene-probe-20260920/`；核对时无游戏进程。

**尚未验证：** 实际按钮布局/点击、原场景PNG、无跳镜/闪烁/人物破面、GPU稳定性、退出清理及实测耗时。本轮没有实机生成结果，不能宣布共享Scene方案成功或据此扩成双镜头。

源码定向回滚 `03cb39ce`；部署备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260920-063617` 保存前一版 `a32ed40c` 及旧会话prefab。恢复模块需同步DLL/资源并重启。主模组、原版DLL、用户缓存/设置及其他作者改动未动，未推送。
