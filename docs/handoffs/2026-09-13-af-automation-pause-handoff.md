# AF 自动化暂停 HANDOFF｜2026-09-13

**用户已要求暂停。自动化 `af-7-8` 已设为 PAUSED，实施/测试代理已停止。未经新的明确指示，不恢复开发或自动化。**

## 一句话结论

这段时间主要推进了**阶段8中的 B1 记忆可靠性**，不是整个重构项目已完成。B1 仍有性能/集成门槛，**B2、B3 尚未进入**。

| 工作 | 这段时间做了什么 | 目前状态 |
|---|---|---|
| 三类记忆链路 | 主线程捕获、来源重验、过期结果拒绝、正确计数与失败通知 | 已离线联验 |
| 调度与写入 | 分片规划/清理；补普通提交、编辑、导入和读档晚回调保护 | 已离线联验 |
| 重复计算优化 | 一次捕获/生成Prompt；重复检查分配约1.57MB→0.24MB；overview资格不再复制整图 | 已离线联验 |
| 最新素材索引 | 修复换表后命中孤立旧记录；2000历史+50新键的fallback访问101225→0 | 23场景+7反例通过；待联合构建 |
| 最新封存/维护 | 7阶段续跑，修同日停住、旧key与列表替换漏任务 | 30场景通过；WIP未完成整套验收 |
| 最终交付 | 大来源硬预算、完整回归、真实游戏/旧档，之后B2/B3 | 尚未完成 |

## 两个版本必须分清

- **最后完成整套离线联验的生产：`62abfdb3`**（交接 `1c36f328`）。109捕获、85真实writer、51提交/导入、54入队资格；相邻回归、Debug/Release×1.3/1.4/Bootstrap六项Stage与接口/存档身份检查通过。不是实机验收。
- **本次上传的暂停现场：`c21523f8`，明确为 WIP。** 保留新索引/封存源码和测试，不回滚丢工作；最后完整构建结果不能套用到它。
- WIP 的旧共享suite适配、严格源码inverse/审查表、封存mutation、最终相邻回归与六项Stage尚未完成。代码地图仍绑定最后已验版`62abfdb3`，不能刷新hash来假装新WIP合格。
- 封存128/8是**每调用**，不是每Tick；实际调度壳可同一逻辑Tick调用9次。完整raw摘要、单owner净化、全owner绑定与队列排序等仍有原子成本，未证明全局硬帧预算。
- 素材索引限于已审计的串行writer前提，不宣称任意后台并发安全；没有改变制作组业务、public写能力或默认渠道。

## 代码接续坐标（WIP源码 `c21523f8`，一基行号）

| 位置 | 符号 / 责任 |
|---|---|
| `MyBehavior.cs:13705-13763` | `private void RecordEventSourceMaterial(` — 正常素材追加/更新，移除完整索引下的全史fallback |
| `MyBehavior.cs:20126-20143` | `private void RebuildEventSourceMaterialIndex(` — 局部完整重建，named last-wins、空键first-wins |
| `MyBehavior.EventSourceMaterialIndex.cs:16-25` | `private bool IsEventSourceMaterialIndexCurrent(` — 绑定source/map引用、count和List结构；同步接口不变 |
| `MyBehavior.cs:4814-4833` | `private bool TrySealPastDailyMemoryDrafts(` — 封存入口；同步完成或有预算地续跑 |
| `MyBehavior.MemorySealing.cs:198-315` | `private bool ContinueDailyMemorySeal(` — 7阶段、128便宜操作/8昂贵操作每调用；仍有原子尾步 |
| `MyBehavior.cs:17741-17770` | `private void TryRunCampaignMemoryMaintenance(` — 同日无job仍续跑，使用完成回执保留原扫描条件 |

## 恢复时怎么继续

1. 先读本交接与总 `HANDOFF.md`，确认要从WIP继续；不要自动融合其他分支或删除原始数据。
2. 完成WIP的上述适配/反例/联合验证，再继续关闭B1剩余预算问题。旧17例对照是12绿/5红，**扩到30例后尚未重新运行旧版或新mutation**。
3. raw hash仍保留；若换revision，先覆盖保存、导出/只读菜单原地净化、Save前嵌套写、直接state/queue写及跨实体迁移，不可只包Save。
4. B1合格后才进B2/B3。真实游戏/旧档等独立验收，不能写阶段8 DONE。

## Git、证据与边界

- 本地：`G:\AFMOD\AF-REFACTOR`；分支 `codex/af-framework-skill-delivery-20260911`。
- 本次按用户新授权推送至 `origin/codex/af-main-refactor-continuation-20260831`。已fetch确认远端`bd2ed35f`为祖先，无分叉；只普通快进推送，不force、不碰main。最终是否成功以本轮Git回执/消息为准。
- 当前WIP输入、日志和测试二进制冻结于本地忽略目录 `.tmp/pause-transfer-20260913/`（180项hash索引）；旧已验候选证据仍在`.tmp/b1-20260913/context-admission-62abfdb3/`。远端含源码/可复现runner，不上传这些大产物。
- 两份2026-09-06用户草稿未改/未暂存；指定旧Native简明HANDOFF不上传。暂停后仅整理一行删除方法留下的尾空白，抽取方法/partial与既有23/30场景证据hash仍一致，未继续功能修改。
- 未覆盖游戏、未操作存档、未部署。若要恢复已验状态，另行指示定向撤销WIP，**不要 hard reset 覆盖用户工作**。
