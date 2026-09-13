# AF HANDOFF：B1 素材索引职责提取与精确集成（2026-09-14）

## 1. 当前结论

**已恢复手动实施，并完成本轮范围的源码提取、旧行为对照和影响面离线联验。整体仍是阶段 8 / B1，未整批合格，不进入 B2。**

- 本轮生产/测试提交：`73a6977cfea435af8e68fb8b6f608699dbc03681`；意图检查点 `ebdabd63`。
- 工作区：`G:/AFMOD/AF-REFACTOR`；本地分支 `codex/af-framework-skill-delivery-20260911`；收到的远端基线 `3f00fefa`。
- 自动化保持暂停；**没有推送、部署游戏、操作存档或切默认入口**。不要依据下面历史 HANDOFF 的其他机器目录选择本机工作树。
- 原 AF 总体起点仍是 `d4cb1467`；本轮局部原行为参照为 `62abfdb3`，提取前 WIP 为 `c21523f8`，它们包含之前获准修复。局部对照不能代替整个原 AF 的功能验收。
- 双 SKILL 继续适用：通用维护 Skill + 专门主体框架 Skill。同 DLL 的主体/internal 制作组接缝/public 子 MOD 层不变；不重写制作组业务，不把当前实现写死为永久规则。

## 2. 真正改了什么

| 原结构 / 问题 | 本轮实现 | 保持不变 |
|---|---|---|
| 素材索引构建在 MyBehavior，绑定状态在 MyBehavior partial | 新增独立 `EventSourceMaterialIndex<T>` 运行时组件，掌握派生重建/重复键策略与结构绑定 | 原素材模型、源列表、日历/人物处理、权威追加和存档仍归 MyBehavior |
| 原 partial 的四个绑定字段与两个 private 方法 | 删除 `MyBehavior.EventSourceMaterialIndex.cs`；调用者直接使用组件，不留多余转发层 | 仍在用的 Record/Rebuild 入口保留，load/正常写入并未断开 |
| 9 个 WIP 未审项阻断严格 inverse | 纳入精确替换/删除及完整组件锁；保留原未审清单为历史，不再靠过期名字阻断正确候选 | 任意未列主体变更、组件/测试漂移、旧 partial 回流仍拒绝 |
| 重用测试目录可能混入旧 Index.cs | 素材 runner 显式列出本次编译输入，排除陈旧生成源码 | 23 原场景、原/WIP 对照和七类同义故障注入不减少 |
| 不同文档当前状态/机器路径冲突 | 根 HANDOFF、原台账、P0–P6 当前入口统一本机和本轮状态 | 历史结果、旧失败/授权、用户草稿保留 |

**拆薄量要诚实：**MyBehavior 主文件减少 10 行，删除原 partial 35 行，主类文件家族合计减少 45 行；新增独立运行时组件 66 行。不是单纯移动 partial，也不是净代码量减少或“十万行主体已经拆完”。这是首个本轮独立职责落地，后续仍需沿真实调用链继续拆。

## 3. 玩家功能对照

正常路径仍为：事件进入 → 原文本/身份/日期规则 → 索引判定/必要重建 → 更新或追加真实记录 → 成功后重绑。没有第二个素材库、动作执行器或 AFEF 写入者。

- 同日同键更新原记录，保持次序/日期/标志累积；大小写/空白归一不变。
- 命名键重复沿用 last-wins，非负日期空键沿用原 fallback 的 first-wins；负日期历史兼容行为保留。
- 列表或索引换表、同数替换、Clear 后重填、枚举探针耗尽后再修改仍可失效重建。
- 重建第二个键抛错时不发布半张表；真实追加后索引插入失败不假装回滚，后续重建仍能恢复该已追加记录。
- 2000 历史 + 50 新键时全史 fallback 访问保持 0；这是运行时方法计数，**不是实机帧率承诺**。
- 组件每 owner 构造一次，普通有效查询不新增反射、全量扫描、队列或锁。只适用于既有已审串行 writer；不宣称任意深字段写入和后台并发安全。

[架构说明](../architecture/af-event-material-index-runtime.md)解释状态所有权和失败发布顺序。

## 4. 源码坐标（源码 73a6977c，一基行号）

| 位置 | 符号 | 责任 |
|---|---|---|
| `Refactor/Runtime/EventSourceMaterialIndex.cs:11-14` | `internal sealed class EventSourceMaterialIndex<T> where T : class` | 独立派生索引/绑定 owner，无游戏和存档写入 |
| `Refactor/Runtime/EventSourceMaterialIndex.cs:39-42` | `internal Dictionary<string, T> Build(List<T> source)` | 未发布重建、命名last-wins/空键first-wins |
| `MyBehavior.cs:13707-13710` | `private void RecordEventSourceMaterial(` | 原记录/追加/发布仍归主体 owner，使用独立索引 |
| `MyBehavior.cs:20126-20129` | `private void RebuildEventSourceMaterialIndex()` | 重建后复核来源引用，再发布和绑定 |
| `MyBehavior.MemorySealing.cs:198-201` | `private bool ContinueDailyMemorySeal(long startTimestamp, double budgetMs, bool requirePendingProbe)` | 七阶段封存已纳入源差异审查，深原子预算仍未通过 |

完整导航：[代码范围图](../architecture/af-framework-code-scope.md) / [58 点 JSON](../architecture/af-framework-code-map.json)。行号只作入口定位，具体符号/提交优先；地图 PASS 不代表完整玩法验收。

## 5. 验证结果与边界

| 验证 | 结果 | 不可扩大解释 |
|---|---|---|
| 素材真实方法 | 当前23/0，提取前 c21523f8 23/0；62abfdb3 的8个缺陷/成本反例仍红；7个 mutation 均编译成功后失败 | 人物/日历/文本渲染边界为受控替身，非实机周报 |
| 封存真实方法 | 当前30/0；8个 mutation 均编译成功后失败；原 62abfdb3 的19绿/11红已在收到版本独立复现 | 封存仍有单源/尾步原子成本；128/8 是每调用，不是全 Tick |
| 完成/捕获/规划/写入 | helper32、business36、captured109、planning24、writers238、terminal85、commit51、admission54通过 | 各 runner 的 game/provider/UI 替身仍有效；部分异常通知不是事务回滚 |
| 相邻消费者 | history852、Native history27、失败UI85、Native准备589、渠道边界132通过 | 不是 Campaign/Mission、真正气泡/音频或所有三渠道完整游戏回归 |
| 精确源码门禁 | 整个 MyBehavior 逆变换与 `90201155` 完全相等；54声明、2删除、1精确装配字段跨度、2完整组件锁 | 不以这种静态比对证明所有运行时语义；另有实际正反例 |
| 防误放验证 | 8个只读门禁测试通过，其中依赖测试含5个子变体 | 未修改生产文件来运行故障注入；历史未审清单不当当前白名单 |
| 最终构建 | Debug/Release × 1.3/1.4/Bootstrap 六项通过；两套项目内 Stage，六份 DLL 与 Stage 内容一致 | 没执行 -Deploy；游戏引用为现有受支持版本覆盖，不是实际游戏运行 |
| API / 持久化身份 | API119、并发读取256、外部 internal 访问应拒绝；4 DLL 元数据532；SyncData146 / behaviors36不变 | 类型/键/ABI不变不等于旧档加载与真实子 MOD 已验 |
| 定位图 | 58点 recorded/working-tree均通过，绑定73a6977c | 导航证据，不是全项目完成率 |

第一次沙箱内产品构建因禁止读取现有 Windows SDK 目录报 `MSB4184`。经权限审查使用**原官方脚本**读取既有 SDK 后构建成功；未安装 SDK、修改脚本或把错误记成通过。清理删除方法残留空白后又执行最终六项，并核对构建期间源码 hash 未变。

[机器验收索引](../audits/2026-09-14-b1-index-owner-integration.json)绑定源码、计数、实际 DLL/hash、精确命令和未验证项。88 项输入/日志/产品 DLL 与 marker 冻结在本地忽略目录：
`.tmp/b1-index-owner-20260914/final-evidence/`。这是审计证据集合，不是可独立部署的发行包；完整源码仍由 Git 提交提供。

## 6. 复现方式

在仓库根目录使用既有 SDK/合法依赖，不覆盖游戏：

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py --source-baseline c21523f8
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py --source-baseline 62abfdb3
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py --mutate publish-partial
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate ignore-empty-probe
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/test_source_parity.py
G:\Python310\python.exe -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree
```

正常项退出0；旧版缺陷对照/mutation退出1且必须有 BUILD_PASS + 具体断言失败，退出2/提取或编译错误不能算有效反例。六项 Stage 沿用原 `build_single_module.ps1 -Stage`，精确参数在验收 JSON；本机依赖可用，不沿用另一台机器“缺 MCM”作为本机结论。

## 7. 尚未完成与下一步

1. **继续同一 B1 的深来源/原子成本。**本轮没有关闭完整 raw hash/首次捕获、单 owner 净化、全 owner 绑定及队列排序等原子工作。先测真实每 Tick/每 record 成本，再做职责提取和预算，不能只限制外层回调数。
2. 若把 raw hash 改成 revision，须覆盖所有 writer，包括保存/导出/菜单原地净化、Save 前嵌套写、直接 state/queue 写和跨实体迁移；不能只包 Save。
3. B1 合格后再做 B2；**Courier 后台准备仍读取 live 对象，本轮未修**。三渠道完整 Prompt→动作→历史/AFEF→展示仍按原功能表对照。
4. B3/public 能力继续按确认范围；制作组玩法、默认迁移、广泛删旧及最终发布不自动执行。
5. LIVE/provider/旧档/真实金币物品债务/AFEF/TTS/子 MOD 加载未验；不能说功能完美复现、无 Bug 或阶段8完成。

不重新从9个旧未审符号盘点开始；本轮源差异门禁已经关闭，下一轮应解决剩余实际生产问题。仍需保留每次修改的有效原行为/故障对照，不因“已审”放宽新差异。

## 8. 回滚与协作

- 回滚参照 `ebdabd63` / 收到版本 `3f00fefa`；若另行要求撤销本轮，定向 revert `73a6977c` 并同步定位图/交接，不 hard reset。
- 仅当前工作树写入。NEW-10 仅作构建引用读取；GCCZ、其他工作树、全局 Skill、游戏目录/ONNX/存档未改。
- 两份用户草稿 hash 保持且不暂存；指定旧 Native 简明 HANDOFF 不上传。新简明说明放本地 `.tmp/af-b1-index-owner-team-handoff-20260914.md`。
- GitHub 未推送、自动化未恢复；本轮源代码已本地提交，详细交接另提交。后续推送须按当时授权和远端祖先关系执行。

## 9. 给制作组的简明说明

> 本轮把事件素材索引的真实职责从 AF 主类提取为独立运行时组件，保留原记录和存档 owner，并删除旧 partial。原有23场景及7个故障反例通过；现有9个未审项已接回精确源码门禁，相关回归和六项构建通过。整体仍是阶段8/B1，深来源性能预算和实机/旧档等未完成，信使线程问题尚未处理。本轮没有部署或切默认，也没有重写政策、宴会、GCCZ 的玩法，不能把它当全功能重构完成版。
