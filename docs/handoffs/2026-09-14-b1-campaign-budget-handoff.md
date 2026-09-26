# AF HANDOFF：Campaign记忆维护共享预算（2026-09-14）

## 当前状态

**生产/测试 `9158132c06659b8bcd660bb85c388ff8ce5db3bc`；检查点 `01e24b9e`，前生产 `73a6977c`。本轮影响面离线验证完成，整体阶段8/B1仍未整批合格。自动化af-7-8继续ACTIVE。**

工作区 `G:/AFMOD/AF-REFACTOR`，分支 `codex/af-framework-skill-delivery-20260911`；启动fetch远端仍3f00fefa、无新协作覆盖。不推送、部署、读写真实存档或切默认入口。双Skill/同DLL的主体、internal制作组接缝与public子MOD层不变；未改制作组玩法。

## 1. 本轮完整链路改动

| 旧问题 / 应保持行为 | 新实现 | 证据 |
|---|---|---|
| 同一次Campaign维护先主维护，再deferred；多次封存各自拿128/8额度 | 一个真实维护周期共享一个有限预算窗口；相同窗口内累计封存metadata/expensive授予 | 当前大积压周期≤128/8；原73a6977c新测试失败 |
| 前一阶段超时，deferred又创建新deadline继续干 | 两个实际消费者通过同一Resolve读取起点/时限；耗尽后不进入下一维护域 | shared-deadline反例与renew-deferred-deadline故障注入 |
| 封存消耗最后原始任务，预算恰好耗尽；同日不再触发总结启动 | 留下一个带save generation的延迟启动意图；下一周期直接启动，不重复封存 | deferred-start与retired-start；旧版本丢失，drop-deferred-start/ignore-pending-generation均失败 |
| 异常/嵌套调用不能把旧窗口带到下一周期 | scope在finally恢复前一个窗口/active状态 | scope-exception与omit-window-restore反例 |
| 空闲Tick不能多分配/查询设置 | 只有实际维护请求到达Resolve才创建窗口；空闲周期不读预算设置 | idle-no-budget |
| 原明确同步调用应继续同步完成 | 0/NaN/Infinity/Max等原无限约定保留；显式独立参数不强制继承耗尽窗口 | 原4个sentinel场景及campaign-explicit-sync |

原30个封存语义场景保留；新增10个真实周期/生命周期/兼容场景。未改变队列/素材/原始数据的权威owner、Prompt/provider、存档字段/类型或动作事实机制。

## 2. 真实职责和范围

```text
OnCampaignTick
  → 原weekly提交等前序工作
  → RunCampaignMemoryMaintenanceCycle
      → TryRunCampaignMemoryMaintenance
      → 未被原weekly-commit条件跳过时，ProcessDeferredDailyMaintenance
      → 共用懒创建MemoryMaintenanceWorkBudget
  → 原Clan等后续工作
```

- `Refactor/Runtime/MemoryMaintenanceWorkBudget.cs` 独立管理时间、metadata/expensive授予与同步例外，不读取游戏对象。
- `MyBehavior.MemoryMaintenanceBudget.cs` 仅保留owner接缝/瞬态状态；原设置仍经GetDailyMaintenanceFrameBudgetMs读取，不新增MCM或硬编码人物。
- 删除旧嵌套 `DailyMemorySealBudget`；不留下旧预算类或旁路调用。新的主类方法是实际调用入口与scope owner，不是空接口。
- 本轮为修复正确性增加协调/延迟状态，主类行数不是下降指标；只声明预算责任已提取，不声明整个大类已拆薄。
- 延迟意图不保存旧任务/游戏对象，只表示一次总结接受请求。generation变化会退休；不是实际动作重放、事务回滚或新的自动重试系统。

**准确边界：**128/8只限制本次正常有限Campaign维护周期中的封存授予。EngineTick摘要回调、其他模块/回调及显式同步/无限调用单列；单次净化、raw指纹、Apply或排序仍可能超时，不宣称全游戏每帧硬上限。

## 3. 源码坐标（9158132c，一基行号）

| 路径 | 符号 | 责任 |
|---|---|---|
| `MyBehavior.MemorySealing.cs:180-183` | `private bool ContinueDailyMemorySeal(long startTimestamp, double budgetMs, bool requirePendingProbe)` | 有限Campaign窗口共享封存授予；独立/同步调用与深原子成本单列 |
| `Refactor/Runtime/MemoryMaintenanceWorkBudget.cs:10-13` | `internal sealed class MemoryMaintenanceWorkBudget` | 独立协作预算窗口；不抢占单次深操作 |
| `MyBehavior.MemoryMaintenanceBudget.cs:16-19` | `private void ResolveDailyMaintenanceBudget(` | 有限Campaign周期懒创建共享窗口，空闲不读取预算设置 |
| `MyBehavior.cs:17733-17736` | `private void RunCampaignMemoryMaintenanceCycle(` | 真实主/deferred维护共享周期与异常/nested恢复 |
| `MyBehavior.cs:17687-17690` | `private void OnCampaignTick(float dt)` | 实际Campaign入口接入共享维护周期，其他顺序保持 |
| `MyBehavior.cs:5923-5926` | `private void ProcessDeferredDailyMaintenance()` | 复用共享deadline，原子超时后不再开始下一维护域 |

完整[63点地图](../architecture/af-framework-code-map.json)在recorded/current均PASS；[范围图](../architecture/af-framework-code-scope.md)只作导航，不是完成功能清单。

## 4. 验证

| 层级 | 本轮结果 | 保留限制 |
|---|---|---|
| 当前真实封存/维护 | 40/0；4096 owner布局跨225个真实维护窗口收敛 | 游戏身份、总结启动及其他维护域仍有明确替身 |
| 提取前生产 | 73a6977c在同40场景中35绿/5红，BUILD_PASS后实际断言失败 | 不是编译/工具错误充作缺陷证据 |
| 故障注入 | 原8个保留，新增6个，共14个均BUILD_PASS后EXIT=1 | 真正改变共享scope/deadline/授予/启动意图/恢复/代际判断 |
| 相邻业务 | business36；原业务4绿/32红；maintenance反例34绿/2红 | 原业务完成层范围，非完整游戏 |
| 记忆相关 | helper32 / captured109 / planning24 / writers238 / terminal85 / commit51 / admission54 / materials23通过 | 深来源硬预算仍未证明 |
| 渠道/界面相关 | history852 / Native history27 / failure UI85 / Native preparation589 / channel132通过 | UI/provider/完整游戏仍未运行 |
| 精确门禁 | 56声明、2删除、2新增跨度、4完整组件锁，恢复整个MyBehavior到90201155；9个防误放测试通过 | 只接纳解释并验证的差异，不绕过whole-source相等 |
| 构建/身份 | Debug/Release×1.3/1.4/Bootstrap六项Stage；API119、并发256、实际DLL元数据532；SyncData146/behaviors36不变 | 未实际加载游戏/旧存档/外部DLL |
| 产物绑定 | 六份产品DLL与项目内Stage相同，构建期间源码hash不变 | 不等于可发布实机合格包 |

旧 `same-tick-multiple-callers` 现明确叫 `standalone-multiple-callers-compatibility`：它直接调用私有helper，仍用于保护原独立调用语义，不能把其1152次观测当作新实际Campaign周期。新的限额证明由 `campaign-shared-*` 覆盖。

数据在[验收JSON](../audits/2026-09-14-b1-campaign-budget-verification.json)。83项输入、日志与实际DLL/marker已冻结到 `.tmp/b1-campaign-budget-20260914/final-evidence/`；不是部署包。使用Git源码与现有合法SDK/依赖重现，历史证据不改写。

## 5. 重现

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --source-baseline 73a6977c
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate renew-seal-budget
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate drop-deferred-start
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/test_source_parity.py
```

历史对照使用旧OnCampaignTick两段真实调用的最薄命名壳，当前执行实际新cycle；还检查当前OnCampaignTick只调用cycle一次且无旧旁路。sealing/business项目都明确列出编译输入，避免旧生成文件混入。

## 6. 下一步与回滚

- **仍继续B1，不重做本次共享窗口。** 下一轮处理剩余完整raw来源/首次捕获、单owner净化、全owner最终绑定及队列整理/排序/Apply的原子成本。先测真实规模/耗时，再选择能保留来源一致性的完整改动。
- 若替换raw hash为revision，仍须证明全部writer路径；不能只拦Save，不能因外围分段就称深记录有界。
- Courier后台live读取、完整三渠道、public选定能力、LIVE/旧档/真实资产AFEF/音频/外部DLL加载仍未完成。本轮未进入B2/B3，不标READY或阶段8DONE。
- 回滚另按指示定向revert9158132c并同步地图/交接，不hard reset；保护原草稿和其他工作树。
- 自动化继续每小时在当前任务推进；本次有明确独立可做的B1工作，不触发自动暂停条件。不推送、不部署、不恢复任何其他自动化。

简明交接同步到本地 `.tmp/af-core-precloseout-team-handoff.md`；当前总HANDOFF与计划续点已更新。
