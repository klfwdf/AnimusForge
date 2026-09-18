# 最新酒馆全景崩溃：静态网格重建修正

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `ece3f36`，修正提交 **`500ab25e`**。用户明确要求“修复并审查，不用跑离线测试”；本轮没有运行任何离线审计脚本，只分析真实日志/转储、审查代码并编译双版本。

## 最新证据

- 用户最新酒馆截图对应 `rgl_log_30808.txt`，06:07:28开始场景生成，地点 `empire_house_c_tavern_a`。诊断 `20260918T220728_5bcfd1eff3654a308f899ba517334efc` 停在 running、events为空，没有发出导演请求。
- 同一进程06:06的百科请求已成功，因此不能把此次问题归因于人物参考或API空回复。
- 最新转储 `C:/Users/29310/AppData/Local/CrashDumps/TaleWorlds.MountAndBlade.Launcher.exe.30808.dmp` 与此前大厅 `...1740.dmp` 都记录 `TaleWorlds.Native.dll + 0x461a0b`、`0xc0000005`，读取地址 `0x8`，故障寄存器 `RAX=0`。
- 反汇编该点为 `mov rdx, qword ptr [rax + 8]`。最新堆栈中的 `+0x40f772`、`+0x40fa26`、`+0x40ff0e` 三个候选地址均核实为对应调用后的返回指令；其中一层循环六次调用绘制函数。没有原生符号，不能把候选堆栈扫描冒充完整展开栈，也不能据此断言某个官方函数名。
- 临时 panorama 目录已创建，但没有首张PNG，说明尚未获得可用全景。旧日志缺少细分阶段，不能仅凭空目录确定最后一次原生调用。
- 05:59启动时另一崩溃 `+0x74e20a` 出现在着色器缓存读取阶段，未混为本次同址采集崩溃，也没有擅自删除第三方缓存。

分析产物：`artifacts/illustrator-panorama-crash-20260919/latest-tavern-dump.json`、`earlier-hall-dump.json`、`native-461a0b.txt`、`caller-0x40f772.txt`等。读取工具 `tools/illustrator/inspect_panorama_crash_dump.py` 只输出寄存器、模块偏移和候选栈地址，不输出任意进程内存文本。依赖仅安装在仓库 artifacts 中。

## 修正与代码审查

原实现整体 `GameEntity.CopyFrom`，把当前实体层级连同灯光等原生组件带到新Scene；公开契约不能保证源场景的灯光/阴影/其他运行缓存可跨Scene复用。这是确定的隔离边界缺口，与最新原生渲染崩溃相符，但未宣称已完整确定native内部根因。

本轮改为：

1. **不再整体复制 GameEntity。** 遍历实际可见的静态 MetaMesh，复制网格与材质，再按原版 BannerTableau 的 `Scene.AddItemEntity` 路径挂载到自有场景；不带入源灯光、阴影缓存、粒子、脚本和物理组件。
2. 人物、坐骑、骨骼与布料模拟分支排除。每个源实体的多个网格共用一个冻结全局帧，避免跨批复制时运动造成组件错位。网格和实体指针都检查不与源对象相同；挂载后更新边界/可见性，并按原版方式移交所有权。
3. 按原版 Tableau 初始化独立内部场景基础设施，加载 `character_menu_a` 观察气氛，关闭静态阴影和天空绘制；仅用自有postfx/focused shadow完成导出需要的初始化。不是重新按模板搭建环境，现场几何仍来自当前实际网格。局部真实光照不复制，颜色/采光继续参考附加实际画面。
4. 删除不再需要的混合树整棵复制/覆盖传播阶段；保留每批8次网格复制、4ms软预算、数量上限和六镜头合成。这里的4ms仍不能抢占单次native调用。
5. 修复清理等待竞态：Shutdown可能先同步退休snapshot、随后停止Tick。后台现在等待“队列清理完成”或“snapshot已退休”任一信号，避免永久等无人执行的回调。
6. 增加快照完成、渲染器建立、镜头开始和导出请求的真实阶段日志，并在首次渲染前持久化诊断阶段；若仍失败，可以缩小具体原生边界。

审查未发现本轮改动向源场景写入参数的路径。原生生命周期、材质持有、实际光照和GPU稳定性仍不能用源码/编译代替实机验收。

## 核实源码坐标

以 `extensions/AnimusForge.Illustrator/src/` 为前缀，对应 `500ab25e`：

| 文件与行号 | 符号/责任 |
| --- | --- |
| `Engine/PanoramaSceneSnapshot.cs:34–40,132–142` | Notes说明网格边界；两种退休信号解除等待。 |
| `Engine/PanoramaSceneSnapshot.cs:209–238` | 原版Tableau基础设施与观察光初始化，不复制源运行组件。 |
| `Engine/PanoramaSceneSnapshot.cs:266–312,318–339` | 分帧逐网格复制，冻结实体全局帧，排除骨骼/布料分支。 |
| `Engine/PanoramaSceneSnapshot.cs:342–377` | `CopyStaticMesh`，MetaMesh独立指针、AddItemEntity、边界和所有权。 |
| `Engine/IsolatedPanoramaRenderer.cs:113–144,161,189,218` | 关闭天空绘制，加入首次渲染前/镜头/导出边界日志。 |
| `Engine/SceneReferenceCapture.cs:75–94` | 快照/渲染器完成及每面开始的持久化诊断。 |

旧 `PanoramaSnapshotAudit` 中要求整体CopyFrom的断言已改为网格重建契约，**没有运行它**；不能继续引用前版759项通过数字作为当前修正版的验证结果。

## 编译、部署与未验证范围

- API1.3和1.4 Release编译均0警告、0错误；做了修改差异和原版调用对照审查。
- 2026-09-19 **06:44:06** 用原 `tools/deploy_illustrator.ps1` 部署至独立生图模块，DLL/PDB、清单及五个prefab共8文件哈希一致；未改主模组或游戏原版DLL。
- 部署SHA256：`43F3A19B6353368603DB0F81D725FAA29CC7520610DF182614B5A197F09F8C30`；MVID：`6714c5e2-77a3-48e5-8642-5abcf7131255`。
- 构建、部署和哈希记录在 `artifacts/illustrator-panorama-crash-20260919/`。
- **未运行离线测试、未启动游戏、未付费调用模型。** 原生崩溃是否消除、六面内容和实际耗时仍待重启后的实机采集。未取得成功的新全景耗时，不能用崩溃前的几秒当作采集耗时。

源码可定向 revert `500ab25e`。游戏备份为 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-064406`，其中保存的是已经崩溃的上一版，不能称为稳定版。其他作者改动、用户缓存及旧成图保留；未推送。
