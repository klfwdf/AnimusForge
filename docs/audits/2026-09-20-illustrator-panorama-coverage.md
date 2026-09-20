# 独立场景全景覆盖与base层家具漏采纠正

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `f0b9139`，源码 **`869166feb2112a62ae6530c1415e22b1f3d25cbf`**。用户先反馈参考图奇怪，再要求完整全景；已解释普通透视相机不能直接设置180度，采用六方向90度投影实现目标。没有运行离线测试或模型请求。

## 实图取证

对应 `20260919T235246_73f0497c95db47af87f19d2f56bd382a`，前版MVID `40acdac8-efb3-493d-a07b-c66b13a7f2c3`。原trace、inventory、resource-supplement和实际拼图已保存在 `artifacts/illustrator-panorama-coverage-20260920/`，避免再次被12条诊断轮转删除。

- 123网格＝运行时89＋资源34。大厅主体 `empire_castle_keep_a_l2_interior` 仅资源 `xml:2458`一份；两路网格名称集合无交集，没有主体同名同位重复证据。
- 123份源/目标实体Frame一致；资源34份中33份AABB也一致。唯一不同为burning_campfire中的burned_wood_a，省略了非网格组件的大边界，不能当坐标错位。
- 运行时已有14把椅子与1宝座，但第二个红座椅 `throne_empire_unsittable` 位于 `(214.021,221.168,2.531)`、多张table_empire_h/e、empire_endtable_a等均未补入。真实XML常规平民物体levels声明mask30，当前activeMask21含base1，旧全位包含检查误拒绝它们。
- 实图120度方形视角本身有明显超广角拉伸，中性补光与现场金色灯光不同；亮条成因仍未确证。没有因为看到亮条就改alpha、LOD或声称z-fighting已修复。

## 本轮变更

1. 生产镜头恢复为前、右、后、左、上、下六个正交方向，每个HFOV90度、aspect1、512方形RT。只有自有Scene参与渲染，顺序共用一个RT，不触碰玩家相机或再次共享Mission.Scene。
2. 复用既有六面球面映射，后台合成2048×1024等距柱状全景（水平360、垂直180）。原生导出只做一次既有生产者R/B适配；原三参数Compose入口保留，新增ComposeWithCancellation用于逐面解码/逐行映射检查取消。
3. 正式参考、试采、Chat/Edits用途均使用ScenePanorama，说明中央前方、左右边缘在后方相接、顶部上方/底部下方，不把展开畸变当实际建筑或最终构图。旧前后CPU拼板工具仅保留历史用途，生产不调用。
4. base纠正：读取XML定义的base位，`requiredVariants=activeMask & ~baseMask`；本例21去掉base1后要求20。平民家具30通过，siege46缺civilian16仍不通过；不是改成任意位相交。base-only容器按公共层处理，子层仍受自己的variant约束。诊断保存implicitBaseMask/requiredVariantMask。
5. 原30米范围、一次资源与runtime副本重建、分帧/数量/25秒预算保持。相机方向从2增至6，渲染量增加；不能沿用此前约3秒作为新版耗时。六面原生证据预算由4MiB增为8MiB，仍在每请求15MiB总参考预算内。

所有六方向成功且通过既有重复面检查后才发送全景，不用两张180度透视图冒充完整覆盖。完整角度覆盖不等于所有家具/地形/动态状态都已重建；资源补齐的已知边界继续保持。

## 核实源码坐标（869166fe）

前缀 `extensions/AnimusForge.Illustrator/src/`：

| 文件与行号 | 责任 |
| --- | --- |
| `Engine/SceneReferenceCapture.cs:19,52–59,91–158,192–196` | 六方向帧、90度FOV、循环/合成/标签和诊断路由。 |
| `Engine/PanoramaProjection.cs:112–185` | 原Compose兼容入口与可取消的球面合成。 |
| `Engine/SceneResourceGeometryPlan.cs:54,70,223,266–273,335–338` | 读取implicit base、实际变体筛选、公共base-only容器。 |
| `Engine/PanoramaResourceSupplement.cs:223` | 诊断标注base和required variant。 |
| `Core/GenerationDiagnostics.cs:95–101,141–150` | 六面证据预算与全景预览记录。 |
| `Core/VisualFidelityRules.cs:46–47` | 模型全景用途与展开方向说明。 |
| `UI/Overlays/IllustrationCardPopup.cs:354–403` | 试采选择ScenePanorama、全景状态与预览。 |

## 验证、部署、回滚

源码与数据对照、独立只读审查、diff检查完成；六方向矩阵和映射按代码复核一致。最终API1.3/1.4 Release均 **0警告/0错误**。未运行离线测试，未实际GPU采集新版，不能以数学审查替代六方向实机验收。

原脚本于2026-09-20 **08:08:05**部署独立Illustrator模块（v1.4.8/API1.4），8文件哈希一致。SHA256 `1C241BB36519AB485CCC14C1079B6CE8585EDEA823299B9E07054B938AD9AE65`，MVID `5bfac8d7-55ee-41ce-be61-25611834d0a0`。部署/构建/manifest在同名artifacts目录。校验期间其他任务提交了无关代码，manifest分别记录本模块源码 `869166fe` 与workspaceRevisionAtVerification；已确认本模块src相对869166fe无差异。

待实机：六面不同方向与接缝、上下覆盖、新家具像素、亮条原因、耗时和GPU稳定性。启动新版点击“独立场景试采”，应看到一张全景展开图；该按钮不调用模型。未推送、未改其他作者文件/游戏原版DLL/用户设置或缓存。

源码回滚定向revert `869166fe`；部署备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260920-080805` 保存前一独立双视角版，恢复模块后重启。
