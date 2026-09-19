# 单镜头试采人物短暂消失：渲染顺序试验

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `9bca4d7`，源码 **`8dc8053e7a56ceb05931a74c1361879cc71ad4ff`**。

用户实机反馈：原场景单镜头已经能导出墙地、家具和人物，但主画面人物在试采期间消失，采集结束后恢复。原版加载MVID `7dbaf06b-e752-4469-8e56-7973435c6a62`。

三条真实诊断均completed、未调用模型、无retirementError：

| 开始时间 | 诊断后缀 | 总耗时 | 应用帧 / 相机提交 |
| --- | --- | --- | --- |
| 06:48:32 | `019a9a55359e48d280a330aec37003a7` | 475ms | 7 / 10 |
| 06:48:37 | `adb01ce0612640c3bea0dba816fd7e00` | 466ms | 13 / 16 |
| 06:48:38 | `533bad8c85c94922b6a65a07f2915cf1` | 556ms | 16 / 19 |

记录在 `artifacts/illustrator-probe-render-order-20260920/previous-probe-evidence.json`。症状更符合共享场景渲染过程的副作用，现有托管日志不能确定native骨骼、剔除或其他渲染缓存的具体故障。

## 仅做的改动

`extensions/AnimusForge.Illustrator/src/Engine/SharedSceneCameraProbe.cs:117–122,173,253`：新增常量 `ProbeRenderOrder=-2001`，自有SceneView初始化时显式调用SetRenderOrder；Describe记录顺序与策略。其余采集、相机提交、导出、停止及清理流程保持。仍是手动单镜头，不扩双镜头，不改普通生成管线。

原版1.4.5 `TaleWorlds.ScreenSystem/ScreenManager.cs:154–181` 的RefreshGlobalOrder从-2000给活动屏幕视图分配顺序，SceneLayer.RefreshGlobalOrder再传给View。原版1.3 `TaleWorlds.Engine/Screens/SceneLayer.cs:188–191` 使用同一传递方式；本机 `_deps_auto/TaleWorlds.ScreenSystem.dll` IL也显示active=-2000、inactive=10000，但不凭该DLL的1.0.0版本字符串证明整个1.3运行环境。

此前试采view未显式设置顺序；不能臆测native默认值。此次只明确让副视图早于活动屏幕视图绘制，作为可单独验证的候选修正。**顺序改变不等于native缓存已隔离，也未验证人物消失已经修复。**

公开SceneView/View接口未找到只针对一个视图的RenderAgents/static-only开关；Utilities.SetRenderAgents是全局开关，Agent剔除/可见性及场景renderer controller也影响真实游戏对象，本轮均不调用。

## 验证与部署

- 真实诊断、原版排序代码和公开API已只读核对；diff检查通过。API1.3/1.4 Release均0警告/0错误，日志在同名artifacts目录。未运行离线测试，未操作游戏、模型或存档。
- 原部署脚本于2026-09-20 06:57:37覆盖独立生图模块（游戏v1.4.8 / API1.4），8文件哈希一致；部署前检查没有游戏进程。
- SHA256 `A37F0EA0A388A02DAFD5C3C7FF66D5B2DA8081CDF5733E97B59749DBC6EEC99E`，MVID `0341c278-a944-4269-99fc-7b3176ccdbbe`，`deploy.txt` / `deploy-manifest.json`留证。
- 新增成本为每次试采一次native顺序设置和少量诊断字段，不加镜头/循环/扫描。实际画面、主画面人物可见性及GPU稳定性待同场景复测；之前成功PNG不能替代新版验收。

下一次启动新版点击“单镜头试采”，核对 `shared_scene_probe_view_ready.renderOrder=-2001`，并观察采集期间人物是否仍消失。如仍复现，排序候选即未解决，不能声称成功或继续扩大为双镜头。

回滚源码定向revert `8dc8053e`；部署备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260920-065737` 保存上一单镜头实验版。其他作者改动、普通生成与原版DLL未动；未推送。
