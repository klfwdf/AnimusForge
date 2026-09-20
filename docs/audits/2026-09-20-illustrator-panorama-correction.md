# 全景展开照搬、导演失败与独立副本太阳光纠正

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `12d734f`，源码 `e24cea6e56da9a44e05c93166b30cc2b867719d4`。仅修改独立Illustrator；未运行离线测试/审计脚本、未调用模型或推送。

## 实机证据

- 正式请求 `20260920T001727_02f04393024c4a26830eb6cc76c701a6` 加载MVID `5bfac8d7-55ee-41ce-be61-25611834d0a0`。六面采集约6.20秒，复制266组件（运行时89+资源177）。导演约8.56秒后返回HTTP400：`User location is not supported for the API use.`，没有导演正文；程序回退并继续请求生图。不能把该次归因提示词长度或契约误拒。
- 生图实际收到6图：全景 `A73CEBF42B500C3AB8685D60E43A4B2C193AC656933C747001A1EB82206179E7`、当前截图、双方各全身/头肩。全景未丢失，其颜色正常；原生六面诊断刻意保存转换前的蓝色，既有生产者一次R/B适配生效，未改颜色通道。
- 原生向上面已有屋顶亮缝，地面条纹在合成全景中存在；生成图沿用弯曲展开透视并强化成屋顶开口。副本采用`SetDefaultLighting`，原版实现设置非零太阳色(1.15,1.2,1.25)，与实际大厅atmosphere的室内/太阳强度0不一致。不能据此声称所有细缝已确证由太阳导致。
- 大厅主体`xml:2458`仅一份、源目标Frame/AABB一致，高度范围约-0.457..23.425。正式请求最远AABB角35.484m，小于旧far36.986m，不能说本次成图屋顶被30m截断。试采`001912`最远角38.252m大于旧far35.608m，证明整块保留的几何边界可能超出旧远裁面，但AABB角并不证明该处有三角形。
- 第二张《殿堂承命》对应`20260920T002244_3f40b36c82d340a783f7fa4ed70e4b29`：导演正常结束，四段正文完整，未本地回退；仍将参考解释为上方天窗，不能用一次好图宣称光照来源已正确。
- 上述正式记录、试采记录、第二张成功记录已保存至`artifacts/illustrator-panorama-correction-20260920/`，避免诊断轮转。

## 最终行为

1. 实际请求导演后，HTTP错误、网络异常、超时、空包、拒绝或截断均明确停止，不自动继续生图或增加付费重试。地区拒绝等显示安全短原因；HTTP实码写诊断。用户关闭/未配置导演，以及有正文但不满足契约，仍可本地构图。回退显示原因，本地正文移除旧行动历史块，避免直接沿用上次动作。
2. Mission导演继续收到完整360×180全景和当前截图，参考开关仍生效。导演额外交付`【环境取景】yaw=...;pitch=...;hfov=...`元数据，单独解析剥离；范围分别[-180,180]/[-60,60]/[45,100]。未提供合法三值或本地构图时使用0/0/75，仅作为默认前向环境资料，不假称导演已选机位。
3. 生图前在后台从已转换为正常颜色的全景投影一张768×768普通透视图；不再向生图发送全景展开图。保持接缝环绕、每行取消、解码尺寸/内存限额，无第二次通道交换、无新增GPU采集或模型调用。投影失败停止，不静默恢复全景输入。
4. Mission当前画面截图仅在本地构图时发给生图端；正常时只供导演校验采光/人物关系。地图会话没有全景，继续使用其唯一真实单视角参考，百科/周报不接此投影。Chat/Edits共用新ScenePerspective语义；关闭生图参考图时不执行投影。
5. 独立副本太阳能量清零，保留三盏中性无阴影观察灯及原native导出所需shadow初始化，不改Mission。30m复制筛选保持；复用复制过程已读目标AABB，O(1)累计完整几何半径，far与筛选范围分离、上限200m。超上限明确写notes/诊断，不新增场景全量扫描。用户询问10m，未直接缩小，因大厅结构高度和完整边界会受影响。

## 核实源码坐标（e24cea6e）

前缀`extensions/AnimusForge.Illustrator/src/`：

| 文件、行号、符号 | 责任 |
| --- | --- |
| `Core/VisualDirectorEngine.cs:158–210`，`CreateDirectionWithClientAsync` | 实际导演失败终止，主动本地模式保留。 |
| `Core/VisualDirectorEngine.cs:613–626`，`BuildDirectorPayload` | 有全景时附加取景元数据要求。 |
| `Core/VisualDirectorEngine.cs:746–760`，`DescribeDirectorHttpFailure` | 上游错误安全分类。 |
| `Core/IllustrationDirection.cs:57–95`，`SplitMetadata/ReadSceneFraming` | 取景解析与剥离。 |
| `Core/IllustrationDirection.cs:129–136`，`RemoveActionHistory` | 本地提示词排除旧行动块。 |
| `Core/IllustrationReferenceRouting.cs:43–84`，`AddSceneReferences` | 透视参考、截图仅本地回退、地图独立路由。 |
| `Engine/ScenePerspectiveProjection.cs:14–139`，`Project/Sample` | 有界普通透视采样与颜色保持。 |
| `UI/Overlays/IllustrationCardPopup.cs:510–517`，会话生成scope | 导演完成后准备生图环境参考。 |
| `Engine/PanoramaSceneSnapshot.cs:67–75,350–358,569–593` | 完整边界累计、零太阳及复制时复用边界。 |
| `Engine/PanoramaResourceSupplement.cs:178–198` | 资源目标边界累计及失败记录。 |
| `Engine/IsolatedPanoramaRenderer.cs:122–147` | 远裁面与有界覆盖诊断。 |
| `Engine/SceneReferenceCapture.cs:107` | 保存镜头覆盖诊断。 |

## 验证、部署与回滚

- 源码审查、真实日志/同图对照、原版双版本API对照、独立只读复核及diff检查完成。API1.3/1.4 Release均0警告/0错误。没有跑离线测试，不能以数学/编译正确替代GPU及模型验收。
- 原`tools/deploy_illustrator.ps1`于2026-09-20 **08:35:45**部署游戏v1.4.8的独立`Modules/AnimusForge_Illustrator`，8文件哈希一致。
- DLL SHA256：`9F04F4CF697AD28BBA9299D7C65DDF3C788A587438D3D0CB0DAFD877E05DFC3B`；MVID：`79de0ab6-c83d-40e0-989e-089757f3c0e5`。构建/部署/manifest保存在本轮artifacts目录。
- 待实机：零太阳后亮缝与材质亮条、照明可读性、完整高处结构、导演取景选择及缺字段默认效果、正常透视成图、当前截图新路由和新投影耗时。约6秒仅是修改前这轮六面采集实测，不冒充新修复耗时。上游地区拒绝是否稳定恢复不受本地代码控制。
- 定向revert源码`e24cea6e`可回到检查点`12d734f`对应模块状态；部署备份`artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260920-083545`保存前版六面实现。恢复文件后重启。未动其他作者文件、用户配置、旧图片缓存或游戏原版DLL。
