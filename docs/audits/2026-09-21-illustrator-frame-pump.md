# 城镇超时续修：消除每批多帧调度等待

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `0457b53`，生产提交 `7baff04e2049825018e8f5de319f2cf81897b8a9`。用户反馈上一修正版仍超时，本轮沿持续修复和部署授权执行；仅独立 Illustrator 四份生产文件，未改AF主体、用户配置或构建覆盖脚本。

## 真实证据

`20260920T223659_ff8a41b65b094d7598b20d71c287d3f7` 与 `20260920T223734_5ea0cb8f018a481ca6bca03fce5d407d` 均加载上一版MVID `a0a0aa8d-d50b-4dc9-8a66-bf05dcfe8883`，约25.2秒超时、faces=0。所以不是没重启或没部署。

第一条：5637项已处理5328项（约94.5%），783资产组；边界测量347次，模板总加载666次，提前排除4420项。实际补齐290个网格，加现场67个，共357个；没有达到1024复制上限。第二条处理5299项，提前排除4412项、加载650次。分组预筛选已实际减少重复远处加载，但仍未完整完成采集，不能称为上一版已通过城镇验收。

真实 `rgl_log_32028.txt`：第一条现场遍历6604节点分113批耗5773ms；第二条112批耗6185ms，平均每批墙钟约51/55ms。旧调度每批分别向主线程提交CopyBatch和AfterFramesAsync，再等待后台续体提交下一批；AfterFramesAsync(1)实际以Math.Max(2, frames)至少等两帧。4ms是单批工作软预算，不是该批与下一批之间的总间隔。日志未单独记录旧版实际CPU工作时间，因此不能将51/55ms全部称为纯空等。

原诊断三份JSON与rgl摘要保存于 `artifacts/illustrator-frame-pump-20260921/`。

## 修改

- 新 `PanoramaBatchPump` 只保存一个采集批次任务，注册后由模块既有 `OnApplicationTick → IllustratorRuntime.Tick` 每帧调用一次。闲置时仅空引用检查；没有轮询线程、Scene.Tick、可见相机操作或额外全场扫描。
- 现场快照与资源补齐各注册一次，后台等待其完成，取消每批“后台→主线程工作→后台→主线程注册帧等待→后台”的往返。相机PaintNeeded/导出等待不改；通用AfterFramesAsync及其他调用者不改。
- 仍调用原CopyBatch，保留64工作项/8重操作/4ms软预算、30米、32768节点、1024网格与25秒总期限。一次native调用不可抢占。连续帧的工作频率提高，实际帧率和GPU稳定性仍待实机。
- 原生批次返回后才发布成功、异常或取消结果，后台finally才进入诊断和清理。Reset/Shutdown的CancelIsolatedPanorama在释放副本前取消批次并唤醒等待者，即使之后不再Tick也不会悬挂。新任务仍由现有采集串行锁约束；owner、场景与token检查沿用原调用。
- 每阶段结束（包括超时）在后台记录 `scene_snapshot_batches`：phase、scheduler、complete、batches、elapsedMs、workMs、maxBatchMs、betweenBatchesMs。betweenBatchesMs是总历时减执行批次时长，包含正常帧间隔与调度等待，不是纯CPU空转时间。每阶段仅写一次，不逐帧写盘。

## 已核实代码位置

以下均对应生产提交 `7baff04e`，路径前缀 `extensions/AnimusForge.Illustrator/src/`：

| 文件、符号 | 行范围 | 责任 |
| --- | --- | --- |
| `Engine/PanoramaBatchPump.cs`，`Start/Tick` | 28–62 | 单槽注册、每帧一次批次、返回后发布完成。 |
| 同文件，`CancelActive/Finish/Describe` | 66–92 | 停止Tick时取消、释放委托引用、分段统计。 |
| `Core/IllustratorRuntime.cs`，`Tick` | 175–180 | 既有应用帧唯一调度入口。 |
| `Engine/PanoramaSceneSnapshot.cs`，`CreatePanoramaSnapshotAsync` | 190–213、226–227 | 两阶段接线与后台诊断；旧每批帧等待移除。 |
| `Engine/SceneReferenceCapture.cs`，`CancelIsolatedPanorama` | 225–230 | 原生快照清理前解除批次等待。 |

## 验证与部署

已做源码审查，覆盖串行任务槽、开始前取消、执行中token取消、回调异常、阶段衔接、Reset/Shutdown与已有snapshot retirement清理路径。API1.3和1.4 Release均0警告/0错误。按用户要求没有跑离线测试/审计脚本，没有启动游戏、付费模型请求或推送。

原部署脚本于2026-09-21 **06:42:54**覆盖独立 `Modules/AnimusForge_Illustrator`（v1.4.8/API1.4），8文件哈希一致。DLL SHA256 `23AD821D8F0C94BE01924B0A027A7EB08718C4DB45E52784C56D5D5A6978ACB2`，MVID `3b86c026-b6d4-44b7-a3ac-dad20312017c`。06:43:23核查无Bannerlord/MountAndBlade/TaleWorlds进程，用户可直接启动新版；不是当前已加载新版的实机证据。

待实机：同街道完整试采能否在25秒内结束，分段workMs/帧间隔、连续帧4ms工作对帧率的影响、全部镜头与家具墙顶覆盖、切场景/关闭和GPU稳定性。不能因编译通过或理论少等几帧就承诺具体秒数。若仍失败，先核实MVID，再用分段计数定位，不发送不完整全景，不只提高总时限。

回滚源码用定向逆提交 `7baff04e`；部署前备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260921-064254/`，是已知城镇超时版。构建/部署/哈希manifest和原始故障证据在本轮artifacts目录。
