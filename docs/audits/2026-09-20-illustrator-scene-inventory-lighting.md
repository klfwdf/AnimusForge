# 场景实体复制清单与独立观察补光

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。用户明确只做前轮方案前两项，不跑离线测试。检查点 `294c141`，源码 **`a32ed40c274a0d51780870bf908cd53a3c70b391`**。

## 问题证据及范围

旧版08:39:09前后拼图在剔除方向栏后，按每4像素采样，约97.7%为纯黑；快照日志记录498节点、89个MetaMesh组件、403个无几何节点。无法仅凭这两个统计断言椅子/墙体未复制；部分轮廓在图中仍可见。原版Scene资源包含不同升级等级及civilian/siege变体，资源节点数量与运行时数量差也不能单独证明遗漏。

当前私有Scene用 `character_menu_a` 气氛，其环境光/天空亮度为0；`SetDefaultLighting`仅设太阳方向光，之后原导出初始化启用阴影。副本不含原室内灯和间接光，存在已复制表面黑掉的风险。本轮加入观察补光并记录具体复制明细，供下一次实机区别“未入队”与“入队后不可见”。

只做这两项：没有重新按场景资源或prefab名称搭景，没有改变原网格复制来源、筛选、半径或排序；没有黑图拒绝门禁，没有改野外/周报/人物参考、生图协议、超时或付费重试。

## 实体清单

- 仅生成Mission双镜头参考时，随原分帧遍历读取元数据：节点/父节点序号、实体名、prefab标识、全局位置、MetaMesh/Composite组件数、可见性及flags、已读取包围盒、选择/跳过原因。
- 已选实体继续记录冻结全局帧、MetaMesh名称/子网格数量/局部帧、组件复制结果。`CopiedComponents`只表示挂载到私有Scene，不证明最终像素可见。
- 记录 `agent_branch`、`animated_or_cloth_branch`、`not_visible_branch`、`helper_or_ignored`、`no_meta_mesh`、`invalid_bounds`、`outside_radius` 或 `selected`；复制阶段区分pending、in_progress、copied、源失效/隐藏、源组件移除、范围变化、空组件与异常。
- 主线程原生读取，后台只消费托管字段。记录写入一次，位于快照结束后、原生渲染开始前；已建立builder的取消/中断会以 `complete=false` 保存，不等待GPU或模型成功。
- 文件位于每次生成的 `IllustratorCache/Diagnostics/<请求ID>/scene-inventory.json`，`trace.json` 的 `scene_inventory` 事件给出路径、数量、容量及截断。最多前1024个已枚举节点、每节点前4个组件明细、名称160字符、文件2MiB；额外实体和明细明确计数。原生分支跳过意味着子树未遍历，不能把记录缺失解释为场景没有该对象。
- 写盘沿用脱敏、异常隔离和原子替换。清单属于既有最多12个诊断目录的保留策略，额外磁盘上限约24MiB，不复制原场景文件。诊断失败不会阻断生成。

## 观察补光

- 只在自有Scene创建3个空灯光载体，无物理/脚本，固定中性白点光。以采集相机原点上方0.75米为基准，一盏居中，另两盏沿水平前后各4米、左右各2米；前后两镜头共用一套，不逐帧调整。
- 灯强度8/6/6，半径为30米加灯到采集中心距离再加5米。关闭灯的阴影、体积光和闪烁，保留导出原有postfx/focused-shadow基础设施和固定曝光。亮度是待实机校准的初值，不承诺已经达到可辨效果。
- 灯名称 `afi_l0` 至 `afi_l2` 为短ASCII。仅新建Light，不复制源场景灯和阴影缓存，不修改现场相机、光照或材质，不手动Tick Scene。
- 已挂载灯由独立Scene退休处理；释放托管引用前抑制Light自身终结器，避免以后对已随Scene退休的组件调用Release。未挂载失败走Dispose/ManualInvalidate，部分创建对象仍由原renderer失败路径清理所属Scene。
- `panorama_observation_lighting` 事件记录灯位置、半径、强度、颜色、无阴影与非现场光照用途；参考标签明确三盏观察补光不代表真实采光。

## 核实源码坐标

以 `extensions/AnimusForge.Illustrator/src/` 为前缀，均对应 `a32ed40c`：

| 文件与行号 | 符号及责任 |
| --- | --- |
| `Engine/PanoramaSnapshotInventory.cs:12–117` | 有界托管Entry/MeshEntry、`Inspect` / `BeginMesh` / `ToDocument`。 |
| `Engine/PanoramaSceneSnapshot.cs:128–179` | `CreatePanoramaSnapshotAsync`：主线程读取批次结束后，后台记录成功/未完成清单。 |
| `Engine/PanoramaSceneSnapshot.cs:315–501` | `CopyBatch` / `InspectOne` / `CopyStaticMesh`：保持原筛选及复制行为，附加选择/复制过程记录。 |
| `Core/GenerationDiagnostics.cs:104–141` | `RecordSceneInventory`：2MiB限额、脱敏、原子文件及trace索引。 |
| `Engine/PanoramaObservationLighting.cs:15–145` | `AddToSnapshot` / `AddPointLight`：独立观察灯布局、参数、挂载与失败清理。 |
| `Engine/IsolatedPanoramaRenderer.cs:85,118` | 初始化时加入补光，提供纯托管诊断。 |
| `Engine/SceneReferenceCapture.cs:104` | 后台写入灯组诊断事件，保留原采集管线。 |

已对照1.3/1.4反编译的Light、GameEntity.CreateEmpty/AddLight、NativeObject.ManualInvalidate和MetaMesh.GetName接口；都存在。原版资源有同量纲灯强度，但反编译没有本次CreatePointLight→AddLight组合的现成调用例，不能冒称原版运行验收。

## 验证、部署与未覆盖

- 主审及实体清单独立只读复核完成；`git diff --check`通过。1.3/1.4 Release最终均0警告/0错误。没有运行离线测试、没有创建模拟GPU或付费生图。
- 构建与部署记录：`artifacts/illustrator-scene-inventory-lighting-20260920/`。按原 `tools/deploy_illustrator.ps1`，v1.4.8选择API1.4，于06:06:11部署独立 `Modules/AnimusForge_Illustrator`；部署构建同样0警告/0错误。
- 8文件哈希一致，SHA256 `3DDDFBC80A8D62B9B41632111CBCCE1851570721646B39ED8181BBE89C88B1E6`，MVID `70893834-3341-4131-b83c-60008e5b401e`，详细清单 `deploy-manifest.json`。核对时没有游戏进程。
- 原8次复制/批、4ms软预算、30米筛选及25秒采集预算保持；元数据读取占用原批次预算，不新增常驻扫描。清单后台一次编码/落盘，灯仅采集时创建3个、使用同一512RT，不加镜头。单次native调用仍不可抢占；实际帧率和GPU开销未测。
- **未验证：** 新版本的真实清单、家具是否漏采、补光亮度与空间可读性、GPU稳定性和退出清理。需启动新版在原大厅触发一次场景生成，先检查 `scene-inventory.json` 中家具/建筑及 `panorama_observation_lighting`，再对比实际发送的前后拼图，不能只看计数。
- 其他作者未提交文件、缓存、设置、主模组和原版DLL未改；未推送。

源码回滚定向 `git revert a32ed40c`；部署备份为 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260920-060611`，保存的是前一版 `3a73006a` 产物。恢复模块后需重新启动游戏，不使用hard reset或改写历史。
