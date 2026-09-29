# 城镇场景XML读取预算修复

工作区`F:/AnimusForge-main`，分支`codex/af-main-refactor-continuation-20260831`。检查点`137300e`；生产提交`78c13e6cf1b1550bf9ab8b565d854cd2cf6d58e6`。仅Illustrator三份生产源码，未运行离线测试或审计脚本，未调用模型，未推送。

## 真实故障与资源规模

`20260920T215714_51bae0ea63fa45cf8fdeb24952ced6cd`在北京时间2026-09-21 05:57:14失败；加载MVID`79de0ab6-c83d-40e0-989e-089757f3c0e5`。总耗时284ms，faces=0，没有director_request/image_request，错误为“场景XML节点超过32768个读取预算”。原诊断已保存到`artifacts/illustrator-town-streaming-20260921/`。

同期`C:/ProgramData/Mount and Blade II Bannerlord/logs/rgl_log_22700.txt`中05:57:14.644的ConvScene记录为达努斯提卡街道、`empire_town_s`。安装目录找到的同名文件为`Modules/SandBoxCore/SceneObj/empire_town_s/scene.xscene`：6850324字节、111806个XML元素、17037个game_entity、9030个顶层game_entity，最大根子树817个元素。这是对真实文件的只读统计，不是执行新解析器的离线测试；原失败trace未记录引擎解析后的绝对路径，新诊断补上该字段。

旧流程先给整份XML建DOM，再层级筛选，再在复制阶段判断30m。32768是XML元素而不是复制网格数量；改10m不会影响此错误。文件的9030个根实体也表明旧4096枚举预算不适合这类城镇，但不能将资源根数当作实机RootEntityCount。

## 实现

- `SceneResourceGeometryPlan.Reader`使用同一只读锁定文件的两次顺序扫描：首遍仅保存全局levels、检查文档结构/预算；第二遍每次只构建一个根实体子树，沿既有Visit规则处理后释放树和实体ID映射。两遍使第三方文件将levels置于entities之后仍可解析，不要求节段顺序。SourceId继续按第二遍XML元素序号稳定生成。
- 区分工作量和驻留树预算：文件仍8MiB、最大深度64；每遍最多524288元素；每棵子树最多32768元素；精简不可变几何计划最多32768项。最后一个文件/写入时间/长度/activeMask的缓存保留，缓存不保存整份DOM。首遍/第二遍均逐Read检查取消，DTD和外部解析继续禁用，不能以流式为名无限读取。
- 根枚举上限4096调整为32768，实际总遍历节点仍32768；复制范围仍30m，渲染副本仍最多1024网格，4ms/64工作项/8次重操作的分帧策略与25秒总期限不变。根枚举仍是一次原生调用，不能承诺该调用零卡顿。
- 资源补齐增加本次采集内的有界托管AABB缓存，最多1024个“资产+完全相同旋转/缩放”键。只有完成模板遍历并取得有效边界才保存，按位置平移并加0.25m保守边界余量；可确认远离30m范围的重复实例不再加载模板。身份观测优先，未知/无效边界、不同旋转缩放、新资产仍走原分帧加载；不用实体原点代替大块建筑边界，不缓存原生句柄，不跨采集沿用可见状态。诊断缓存计数在清理后保留，实际字典清空。
- 新增`scene_resource_plan_start/ready`、XML元素/实体/峰值子树/计划项/读取耗时，和模板加载数/预加载前范围排除数/边界缓存数。今后可区分解析、全场枚举、模板资源加载、30m复制与镜头采集成本。

完整文件索引首次仍要顺序扫描；运行时仍按一次枚举、分帧遍历找附近实际几何。这不是只读取文件中的30m片段，也不声称完全消除了远处资源首次加载。native terrain、水面、动态人物/布料与实时破坏状态的原覆盖边界不变。

## 已核实源码坐标（78c13e6c）

前缀`extensions/AnimusForge.Illustrator/src/Engine/`：

| 文件与符号 | 行范围 | 责任 |
| --- | --- | --- |
| `SceneResourceGeometryPlan.cs`，预算常量 | 18–23 | 文件工作、子树与精简计划分离。 |
| `SceneResourceGeometryPlan.cs`，`Reader.Read/ReadNext/ReadSubtree` | 155–273 | 两遍流式读取、逐根释放、取消与预算检查。 |
| `PanoramaSceneSnapshot.cs`，`PanoramaSnapshotMaxRoots` | 116–124 | 根遍历上限与原复制预算分离。 |
| `PanoramaSceneSnapshot.cs`，`CreatePanoramaSnapshotAsync` | 171–181 | 解析阶段来源及耗时诊断。 |
| `PanoramaResourceSupplement.cs`，`AssetBoundsKey` | 31–54 | 有界同资产同线性变换缓存。 |
| `PanoramaResourceSupplement.cs`，`CopyBatch` | 119–150 | 完整遍历后缓存，重复远处模板提前排除。 |
| `PanoramaResourceSupplement.cs`，`IncludeTemplateBounds/Describe/DisposeTemplates` | 251–316 | 已读网格边界合并、统计与清理。 |

## 验证和部署

只做真实日志/文件分析、源码审查、双版本编译和部署哈希校验。审查覆盖全局levels顺序、空根/嵌套实体、稳定ID、父子变换/层级继承、取消、重复节点拒绝、缓存仅完整边界写入与大块网格保留。第一次编译发现新增JObject诊断缺using，已补齐；最终API1.3/1.4 Release均0警告/0错误。未执行新解析器的离线测试，未调用模型或启动游戏。

原`tools/deploy_illustrator.ps1`于2026-09-21 **06:10:16**部署独立`Modules/AnimusForge_Illustrator`（游戏v1.4.8/API1.4），8文件哈希一致。DLL SHA256为`D5C24B6F4C27E2987F7F70878DADF36A1F7FAAA5935E53A3C276747BE38C5D7F`，MVID为`bee95733-ba3c-4f6e-8283-54a8b756a73e`。构建、部署及manifest在本轮artifacts目录。

待实机：该城镇的新解析路径、实际RootEntityCount/实例节点数、25秒预算、1024副本上限、模板缓存命中率、画面覆盖及第三方大子树。文件尺寸符合新预算和编译通过不等于实机成功；不沿用旧大厅耗时当新城镇耗时。重启后先在同街道做独立场景试采，可在不调用模型的情况下验证环境采集。

回滚：定向revert`78c13e6c`；模块备份`artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260921-061016`。未改主模组、构建覆盖脚本、用户配置或旧图片。
