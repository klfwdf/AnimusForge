# 城镇采集：按资产分组与局部边界预筛选

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。用户确认实施优化前两项；检查点 `101fe5e`，生产提交 `5add4b7bf67f141e9de3d37255cd6f7858d544a2`。仅修改 Illustrator 两份生产源码。没有运行离线测试/审计脚本、调用模型或推送。

## 实机故障基线

旧版 MVID `bee95733-ba3c-4f6e-8283-54a8b756a73e` 在 `empire_town_s` 街道连续两次约25.2秒超时，faces=0。首次 `20260920T221541_54b222f009794a9c9ae229da67945dee` 的 XML 计划279ms完成，5637项；现场遍历6604节点，复制67个组件。资源补齐约17.93秒，仅处理1062项、加载755次模板、提前排除78项，补齐46个组件，合计113个。第二次 `20260920T221611_5f298b77181941f7a8fd235ed7d817db` 命中XML计划缓存仍超时，加载670次、提前排除62项。证据保存至 `artifacts/illustrator-grouped-bounds-20260921/`。

因此瓶颈不是32768 XML读取限制，也不是1024渲染副本已满。旧缓存要求完全相同旋转/缩放，大量远处实例仍先创建模板才能判断距离。

## 实现与性能边界

- `SceneResourceGeometryPlan` 在既有后台读取中按准确资产名和 prefab/mesh 类别进行稳定分组；组与组内均保留首次出现顺序，SourceId仍是原XML编号。父子变换、当前等级筛选在分组前已完成。O(n)临时托管集合受原32768计划项限制；分组逐项检查取消，并随最后一份文件/长度/mtime/activeMask计划复用，不在游戏每帧重新分组。
- 资源补齐首先应用运行时实例权威规则。每个有待补齐的重复资产组，最多先创建一个Identity根的私有模板，完整遍历允许复制的静态几何，将其原生边界合并成资产局部AABB；测量阶段不往最终副本复制。只有遍历结束且边界完整有效，才允许用于提前排除。无几何、缺失或无效边界仍按实例走原路径；只有一次引用的资产不增加测量开销。
- 将局部AABB八角按每个实例的完整线性变换和平移转换，支持旋转、镜像、非等比缩放；使用双精度中间运算并扩大保守余量。只有确认整个包围盒在30米外才跳过。异常数值不用于拒绝实例；跨范围整块墙/屋顶保留，不以资产名或原点距离猜测。
- 可能在范围内的实例仍用原来准确世界帧重新创建模板，并按原生逐实体边界、网格去重、脚本/动态/cloth排除规则复制。没有复用修改过世界帧的native树，也没有长期保存native句柄。常驻边界只有当前组一份；换组覆盖，取消/完成清理。
- 原生主线程、每批64工作项/最多8重操作/4ms软预算、总25秒、总遍历32768、最终最多1024网格保持。失败的原生资源查找现在也消耗重操作名额。单次native调用仍不可抢占；分帧调度未重构，没有跨采集边界缓存。
- 新诊断 `assetGroups`、`boundsMode=grouped_identity_template`、`boundsProbes`（测量尝试次数）、`instanceLoads`（实际世界帧加载成功次数），保留 `templateLoads` 总成功数、`outsideRadiusBeforeLoad`。`cachedAssetBounds`现表示本轮完成有效边界测量的累计组数，不是常驻字典大小。明细的 `boundsProbe` 区分测量和实际实例。

此优化主要减少远处重复资产加载。重复资产全部在近处时仍可能增加一次测量成本；唯一资产、边界缺失资产仍必须加载。首次读取场景文件和现场枚举仍存在；不能宣称只访问30米文件片段，或承诺所有城镇都能在25秒内完成。

## 已核实生产源码坐标

以下均对应提交 `5add4b7b`，前缀 `extensions/AnimusForge.Illustrator/src/Engine/`。

| 文件、符号 | 行范围 | 责任 |
| --- | --- | --- |
| `SceneResourceGeometryPlan.cs`，`Entry.AssetKey` | 32–55 | 区分prefab/mesh资产身份。 |
| 同文件，构造函数、`HasRepeatedAsset` | 71–114 | 后台稳定分组与单次资产绕过测量。 |
| `PanoramaResourceSupplement.cs`，`CopyBatch` | 40–174 | 完整测量后启用边界、组内预筛选、原实例复制、预算。 |
| 同文件，`CopyMesh/IncludeTemplateBounds` | 177–255 | 测量与实际复制分支，局部边界合并。 |
| 同文件，`TryTransformBounds` | 259–287 | 完整八角变换与保守余量。 |
| 同文件，`Record/Describe/DisposeTemplates` | 305–345 | 新计数、测量标签与私有资源清理。 |

## 验证、部署与未验收范围

完成源码审查及双API Release编译，两套均0警告/0错误；核对原版1.3/1.4矩阵坐标约定，检查单次资产、全组运行时已观察、测量取消/失败、变换无效、场景切换清理、数量预算和完整性失败路径。没有执行离线测试，新几何边界与画面只能由实机确认。

原 `tools/deploy_illustrator.ps1` 于2026-09-21 **06:32:42**部署独立 `Modules/AnimusForge_Illustrator`（游戏v1.4.8/API1.4），8文件SHA256一致。DLL SHA256 `4CA5BB9E3AA0D0E7DFCE15FDE7CAEC82B5B5A902F7384BABA74973AE92288E31`，MVID `a0a0aa8d-d50b-4dc9-8a66-bf05dcfe8883`。DLL当时被占用，脚本使用既有重命名替换流程；06:33核对时仍有Launcher进程9128。必须完整退出并重新启动游戏，不能把磁盘更新当作已运行新版。

用户随后确认已退出。06:35:19再次核查没有Bannerlord/MountAndBlade/TaleWorlds进程，安装DLL与部署输出SHA256仍一致；记录在 `after-exit-check.json`，无需重新构建或覆盖，可直接重新启动。

待实机：同一街道的模板加载/提前排除/总耗时是否改善，屋顶墙面与家具是否保留，镜像/非等比缩放第三方资产的保守边界、GPU稳定性和取消退场。没有承诺秒数，也没有自动运行付费生图。未改主模组、用户配置、参考图通道或采集半径。

构建、部署日志与8文件manifest见 `artifacts/illustrator-grouped-bounds-20260921/`。回滚生产源码使用定向逆提交 `5add4b7b`；部署前完整独立模块备份为 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260921-063242/`。备份版本是已知街道超时版本，不称为城镇稳定版。
