# 返回独立场景与真实资源几何补齐

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `0983bf1`，源码 **`c286160681bd09b8c019fa214d69a38b926576f4`**。用户明确选择独立场景，仍不跑离线测试，不调用付费模型。

## 本轮实现

1. 删除共享Mission.Scene的试采类、Tick/取消及单图适配函数。按钮改“独立场景试采”，复用正式的独立前后采集，只显示SceneViews拼图、不写图库、不请求模型。野外/周报/人物参考管线保持。
2. 原运行时网格复制继续。已观察实例（包括隐藏/零网格）的prefab或实体名与全局帧具有优先权；隐藏容器子层继续按预算登记但不复制，资源候选与已观察实例匹配即省略。该规则避免常见作坊替换、身份布景隐藏被资源默认状态覆盖。
3. 主线程用 `Utilities.TryGetFullFilePathOfScene(Mission.SceneName)` 取得引擎实际资源文件及等级mask；后台解析真实资产引用、累计变换/scale/visible/levels。使用原版ApplyEulerAngles，不自行猜欧拉顺序或模块覆盖优先级。源Scene指针/等级在解析后及复制批次复核。
4. 不整场Scene.Read。准确prefab引用通过 `Instantiate(...callScriptCallbacks:false, createPhysics:false)` 进入私有未渲染模板Scene；inline网格用MetaMesh.GetCopy加载到无物理空载体。模板不Tick，脚本、非Stationary、骨骼、cloth、helper和不可见分支省略；只有纯MetaMesh进入最终渲染副本，模板灯光/物理/脚本不进入渲染。
5. 按模板实际全局AABB筛30米，跨界网格保留整块。按网格名与有效世界帧去重（位置约2cm、旋转轴向量约0.005容差），runtime+resource合计最多1024组件。模板逐项退休，阶段结束/取消纳入快照清理；两Scene清理用嵌套finally确保均尝试执行。
6. 新增 `scene-resource-supplement.json`：文件来源、当前mask、计划和省略原因、运行时实例抑制、重复/范围过滤、复制结果和源/目标Frame/AABB。旧inventory增加目标边界及runtime/resource拆分计数。Copied只说明挂载，不证明最终像素可见。
7. 离屏view的clear color改为中灰 `#404040`，标签明确它表示几何未覆盖，不作为墙/地。既有观察补光和颜色生产者适配保留。

## 实际资源与限制

当前大厅 `empire_castle_keep_a_l2_interior/scene.xscene:3271–3274` 明确引用整体大厅prefab，坐标 `(223.804,220.493,0)`；Native `Prefabs/castle_empire.xml:265–270` 对应普通大厅MetaMesh。该节点只有transform/edit_mode_data，解析器允许它进入计划，不仅处理inline小装饰。仍须实机确认native加载、可见性和实际像素。

双版公开接口支持场景文件解析、升级mask、Instantiate(false,false)、MetaMesh及矩阵操作。原版SettlementVisual使用包含全部active mask的匹配，不能用简单非零相交混入civilian/siege变体。Scene.Read没有30米或禁止脚本创建的参数且无法分批取消，本轮不使用。

XML预算为8MiB、32768元素、64层、4096候选；Dtd禁止、XmlResolver=null。仅缓存最近文件的托管计划，键含路径/大小/mtime/mask。资源native阶段每批最多8重操作/64工作项/4ms软预算，整个采集保留25秒限额。单次native调用或复杂prefab创建不可抢占，实际开销尚未测。

两个自有Scene中仅最终副本被渲染；模板无视图。每个inventory文件各2MiB、1024记录，截断有计数，随既有12请求目录轮转；诊断失败不阻断清理或生成。新增操作仅用户采集时执行，无常驻扫描。

脚本/动态对象、未知属性、材质或子节点覆盖、缺失资产明确省略。完全删除的对象与未被runtime枚举的静态对象无法仅凭源文件区分，**不保证完整实时破坏状态**；复杂第三方改景、native terrain/水面也不保证覆盖。模板默认mask语义只有native实现，已记录mask/cumulativeMask/visible供实机定位。不能把XML接受大厅资产当作建筑已成功渲染。

此前498节点/89网格的原始inventory已被诊断保留策略淘汰，未冒称重验其具体矩阵。此前14椅子/宝座copied的记录也不能说明其最终可见；新目标边界和中灰底用于区分错位、缺几何与照明问题。

## 源码坐标（c2861606）

前缀 `extensions/AnimusForge.Illustrator/src/`：

| 文件与行号 | 责任 |
| --- | --- |
| `Engine/SceneResourceGeometryPlan.cs:79–128,227–363` | Load/Visit/TryLevelMask/TryFrame：有界计划、等级、静态筛选与变换。 |
| `Engine/PanoramaResourceSupplement.cs:36–139` | CopyBatch：私有模板、预算、实例抑制、静态过滤。 |
| `Engine/PanoramaResourceSupplement.cs:142–244` | CopyMesh/ContainsGeometry/Describe/DisposeTemplates：30米/去重、挂载、诊断与退休。 |
| `Engine/PanoramaSceneSnapshot.cs:134–232` | 两阶段复制、后台解析、源身份复核与清理。 |
| `Engine/PanoramaSceneSnapshot.cs:446–568` | InspectOne/CopyStaticMesh：已观察实例、隐藏子层、目标边界。 |
| `Engine/IsolatedPanoramaRenderer.cs:155–157` | 自有view中灰clear color。 |
| `Core/GenerationDiagnostics.cs:141–189` | 独立预览与资源补齐诊断。 |
| `UI/Overlays/IllustrationCardPopup.cs:348–416` | 只采集、不调用模型的独立预览。 |

## 验证、部署及回滚

源码、原版API、真实XML结构与退场复核完成。已修正补齐前判空、动态/cloth过滤、运行时/隐藏实例抑制和双Scene异常清理边界。初编译一处无效异常类型筛选警告已修；最终API1.3/1.4 Release均 **0警告/0错误**，XML和diff检查通过。没有离线测试或新路径实机验收。

原部署脚本于2026-09-20 **07:46:33**覆盖独立Illustrator模块，v1.4.8选择API1.4，8文件哈希一致。SHA256 `2E38A32B3C3E7AE9C0767F3A70CACEA80A7F55353C3EA8A4507AE296751212F9`，MVID `40acdac8-efb3-493d-a07b-c66b13a7f2c3`。构建、部署、manifest在 `artifacts/illustrator-isolated-resource-20260920/`。部署前无游戏进程。

下一步启动新版，在原大厅点击“独立场景试采”，对照前后PNG及resource-supplement中大厅prefab结果、源/目标AABB，确认墙地/家具可读性、帧率与稳定性。该按钮不发模型；首次打开无缓存卡片的原自动生成行为未改。

定向回滚 `c2861606`；备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260920-074633` 保存前一排序实验版，恢复DLL及会话prefab后重启。共享试采是已知失败方案，不建议无原因恢复。主模组、原版DLL、其他作者改动、用户缓存/设置未改；限定路径提交，未推送。
