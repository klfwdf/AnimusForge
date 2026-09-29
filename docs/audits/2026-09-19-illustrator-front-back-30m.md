# 前后双镜头、玩家附近30米与原生渲染名称修复

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `2d62a075`，生产源码提交 **`0088881295468fd700cf69a44bf1290c06d1e71b`**。本轮已完成代码审查、双版本编译与游戏模块部署；用户明确要求不跑离线测试，实机验收仍待进行。

## 崩溃证据与修复

最新日志为 `C:/ProgramData/Mount and Blade II Bannerlord/logs/rgl_log_24200.txt`，转储为 `C:/Users/29310/AppData/Local/CrashDumps/TaleWorlds.MountAndBlade.Launcher.exe.24200.dmp`。日志加载MVID `6714c5e2-77a3-48e5-8642-5abcf7131255`，即前一部署版；06:49:28第一镜头开始后崩溃。

- 转储异常 `0xc0000409`，fast-fail参数5，故障在 `ucrtbase.dll + 0x11858`；候选返回地址包含CRT `+0x8491` 和Native `+0xd793c`。
- Native `+0xd7936` 调用IAT `0xa2e200` 的 `strcpy_s`，传入目标容量 `0x80`（128字节）。CRT对应ERANGE失败分支进入无效参数处理并终止进程。
- 转储保留56字节Scene名 `af_environment_snapshot_4c43d77f25504d7fac692920549e4097`，并出现把同一Scene名重复拼接的GBuffer标签片段。`gbuffer_normals(2)` 加两次完整名称共130字节，已超过128字节容量，且还需要终止符。
- 原生Scene与Tableau/RT名称现分别为 `afi_s00000001`、`afi_v00000001` 形式，13个ASCII字符；native调用前限制不超过16。对应双名标签缩至44字节。完整GUID只保留在临时目录/诊断身份中。

这是本次名称长度故障的修复，不用它替代此前Native `+0x461a0b` 空指针问题的分析，也不据此宣称所有GPU问题已消除。

只读取证产物位于 `artifacts/illustrator-panorama-crash-20260919/`：`latest-24200-dump.json`、`native-d793c.txt`、`render-generated-names.json`、`render-name-occurrences.json`。`native-buffer-name-flow.txt` 开头从非指令边界开始的几行不作为证据；转储堆栈扫描仍只是候选地址，不冒充带符号的完整调用栈。

## 当前采集原理与覆盖

此前生产链建立独立静态环境并采集六面。用户最新要求为前后双镜头、玩家位置约30米范围，现实现为：

1. 仅在发起生成时读取现场实体。中心优先采用 `Mission.MainAgent.Position`；没有玩家Agent才使用当前相机位置并在参考标签说明。遍历仍有界，范围外父节点也检查子节点，避免遗漏挂在远原点父实体下的近处装饰。
2. 使用实体全局包围盒与30米球形范围相交判断，只将范围内可见静态网格加入复制列表；不按prefab名称、文化或地点模板搭景。跨范围的大墙/屋顶保留整块，不切割几何。无效包围盒省略并计数。
3. 复制前再次核对范围，每个实体的所有网格共用同一个冻结全局帧。继续通过 `MetaMesh.CreateCopy` 与 `Scene.AddItemEntity` 重建，不整体复制源GameEntity，不复制灯光、粒子、物理、脚本、骨骼或布料组件。
4. 同一点建立前、后两个相机，后方绕竖直轴转180度；每个水平视野120度、512方形目标，共用离屏RT顺序导出。远裁面为30米加镜头到采集中心的距离再加2米，聚焦阴影范围随之收窄。
5. 原生PNG只在合成入口执行已取证的R/B适配，生成1024×544参考图（左FRONT、右BACK，顶部32像素方向栏）。两张近乎重复时拒绝发送；不做球面投影，不称为完整360度。原六面CPU工具保留，但生产不调用六面采集。
6. 双视角图及可取得的真实当前画面送导演；生图端启用参考图时也接收这两类图。真实画面校验颜色、采光、人物与地形关系。标签明确左右两半不相邻、方向栏不进入作品、最终生成单幅自然机位。

不接管/旋转玩家相机、不改共享Mission场景参数，也不实际隐藏或移动玩家UI。实际无闪烁仍需实机验收。普通第三方静态实体沿相同实际网格路径处理；原生terrain、水面、动态骨骼和完整实时光照尚未覆盖。中性观察光仅用于识别几何与材质，不代表现场光照；侧向盲区和范围外内容保持未知。

## 性能与耗时边界

- 保留生成时单次根枚举、分帧线性遍历、每批最多8次网格复制/4ms软预算，以及4096根、32768节点、1024网格副本、25秒采集预算。
- 相机数与原始导出像素量降至原来的1/3；合成直接并排复制像素，不再逐像素做球面投影。范围过滤减少实际复制量，但仍需遍历有界层级，不等于完全免除远处节点检查。
- 单个native调用不可抢占，4ms不是单帧硬保证，25秒也不是native阻塞的强制终止保证。
- 旧版24200日志实测：检查498节点、复制91网格、22批，复制阶段843ms。诊断中快照就绪1049ms、渲染器就绪1062ms、第一镜头开始1066ms，随后崩溃。**这不是完整采集耗时，也不是新30米版本的实测结果。**
- 新版成功后的 `panorama_composed` / `scene_capture_end` 记录2个方向、范围省略数、复制批次/耗时、总耗时及应用帧间隔，留待真实采集评估。

## 核实源码坐标

以下均对应源码 `00888812`，前缀 `extensions/AnimusForge.Illustrator/src/`：

| 文件与行号 | 符号、责任与接线 |
| --- | --- |
| `Engine/PanoramaSceneSnapshot.cs:40–50,99–116` | `Notes`、`HasUsablePanoramaBounds`、`IntersectsPanoramaRadius`：30米边界与参考标签。 |
| `Engine/PanoramaSceneSnapshot.cs:235–256` | `PanoramaSnapshotBuilder.Begin` 内短Scene名、玩家/相机中心及有效性检查。 |
| `Engine/PanoramaSceneSnapshot.cs:305–389` | `CopyBatch` / `InspectOne`：筛选候选、复制前复核、冻结帧、分帧预算与遍历上限。 |
| `Engine/IsolatedPanoramaRenderer.cs:105–140,202–205` | `Create` / `Paint`：可选FOV、两个相机的实际创建、裁面、短RT名与自有阴影范围。 |
| `Engine/PanoramaProjection.cs:176–224` | `BuildFrontBackCameraFrames` / `ComposeFrontBack`：前后方向、一次通道适配、重复图拒绝与拼板编码。 |
| `Engine/SceneReferenceCapture.cs:47–137,169–174` | `CaptureConversationSceneReferencesAsync`：生产两镜头循环、120度FOV、参考类型/标签及诊断。 |
| `Core/IllustrationReferenceImage.cs:3–12` | `IllustrationReferenceKind` 追加 `SceneViews`，旧枚举值保持。 |
| `Core/IllustrationReferenceRouting.cs:33–39` | `AddSceneReferences`：双视角与当前真实视图送生图链。 |
| `Core/VisualFidelityRules.cs:34–51` | `ReferenceRoleInstruction`：双视角用途说明，区分完整全景。 |
| `Core/UniversalOpenAiImageClient.cs:389,480,529–534` | Edits参考标签与Chat场景分类/专属用途；两种通道均识别新类型。 |
| `UI/Overlays/IllustrationCardPopup.cs:378–386` | 会话生成入口消费场景参考并向导演/生图列表接线。 |
| `SubModule.cs:32–34` | 启动日志 `sceneCapture=isolated-front-back-30m` 与实际MVID。 |

## 验证、部署与回滚

- 真实日志/转储分析，主审与独立只读审查未发现本轮确定的新代码问题。核对 `GetGlobalBoundingBox`、`Camera.Far`、`SetFovHorizontal` 在1.3/1.4原版源码均存在。
- API1.3/1.4 Release均0警告、0错误；最终生产代码编译后未再修改。构建日志为 `artifacts/illustrator-native-name-fix-20260919/build-1.3.txt`、`build-1.4.txt`。
- 部署前确认没有游戏进程，原 `tools/deploy_illustrator.ps1 -Configuration Release -BannerlordApi auto` 为已安装v1.4.8选择API1.4，再次编译0警告/0错误，2026-09-19 **07:27:12** 覆盖独立 `Modules/AnimusForge_Illustrator`。
- DLL/PDB、SubModule.xml和五个prefab共8文件哈希全部匹配。实际部署DLL SHA256：`E704559B8D2F5518C3FF631F84364CA7BA4BE1B9F03CD46F009D9C5ED483580D`；MVID：`21cc0212-07ea-4d6e-ae7e-d278aed36474`。部署日志及清单为同目录 `deploy.txt`、`deploy-manifest.json`。
- 未运行离线测试/审计脚本，部分旧审计仍针对历史六面契约，不能声称本轮通过。没有启动游戏、操作存档或调用付费模型。新GPU稳定性、无感采集、实际颜色/双视图、第三方场景、帧率、完整耗时及最终生图还原仍待重启实测。
- 原部署流程不变，只覆盖生图子模块；主模组、TaleWorlds原版DLL、其他作者未提交文件、用户图片缓存保持原状。没有推送。

源码回滚可定向 `git revert 00888812`，不得hard reset。部署备份为 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-072712`；它保存的是已发生名称溢出的前一版，不是已验证稳定版。回滚若被要求，须同步恢复对应模块文件或按原脚本重建部署，不只移动Git指针。
