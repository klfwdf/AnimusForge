## 2026-09-15：内层同数量变动回归

封存新增 12 个实际入口用例：lines/triggers × unchanged、append、slot、remove-add、swap、finalize-slot。中途让出后对实际来源做变更，再用原40b同步净化体对比完整对象图；不是只测helper或故意改坏生产源码。

```powershell
python run_sealing.py
python run_sealing.py --source-baseline 4d6994bc
python run_sealing.py --mutate ignore-line-structure
python run_sealing.py --mutate ignore-trigger-structure
```

当前88/0；修复前4d同88例80/8；两个结构守卫反例分别84/4，均BUILD_PASS后EXIT=1。旧ignore-line-source现在移除整个line绑定防护（含版本探针），保持其“全部来源校验失效”的原反例含义；当前80/8。历史全量mutation不是本轮全部重跑。

新增List枚举器只验证结构修改，不代表字段revision或并发集合；trigger整列表sanitize、原子字符串工作、完整B1预算仍有未完成项。两份无用的post-Done line发布标志已删，原同步规则/数据DTO未改。具体交接见 `docs/handoffs/2026-09-15-inner-structure-fix-and-modularization-handoff.md`。

# 当前：单draft深line/trigger metadata预算（2026-09-14）

封存末尾单draft的line净化与weekly trigger bind现在消耗共享metadata，不再随一次expensive身份把1024行原子做完。draft身份仍一次expensive；trigger列表sanitize仍一次原子。有限窗口最多128 metadata，实测1×1024行9窗、窗内最多127行。

- sealing：当前76/0；`--source-baseline 40b92e67` 同76例61绿15红；新反例`unbudgeted-line-normalize`（inner-cost红）与`ignore-line-source`（empty-grows/kept-empties/line-change）均BUILD_PASS后EXIT=1。
- 同步`SanitizeDailyMemoryDraftEntry`与cooperative路径共用`BindDailyMemoryDraftWeeklyTrigger`/`SanitizeDailyMemoryDraftLine`；未完成draft的line/trigger列表保持私有，列表引用/count变化失效重封。
- `test_source_parity.py` 12/0，含原Where+Select体与bind体精确还原。代码图77锚点绑定生产`4d6994bc`。
- 本轮完整交接：[深line/trigger HANDOFF](../../docs/handoffs/2026-09-14-b1-deep-line-trigger-handoff.md)。首次capture/copy、全owner绑定、Apply与LIVE/SAVE仍未完成。

```powershell
$env:DOTNET_EXE = 'C:\Program Files\dotnet\dotnet.exe'
C:\Users\klfwdf\AppData\Local\Programs\Python\Python312\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
C:\Users\klfwdf\AppData\Local\Programs\Python\Python312\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --source-baseline 40b92e67
C:\Users\klfwdf\AppData\Local\Programs\Python\Python312\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate unbudgeted-line-normalize
C:\Users\klfwdf\AppData\Local\Programs\Python\Python312\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate ignore-line-source
```

## 以下为前序结果，数字与语义绑定各自当时版本

# 当前：Campaign维护共享预算（2026-09-14）

有限的Campaign维护周期现在共享一个懒创建窗口：主维护与deferred维护使用同一deadline，封存授予累计128个metadata/8个expensive操作。显式同步/无限调用以及EngineTick摘要预算仍单列；这不是全游戏帧硬上限。

- sealing：原30个语义场景保留，新增10个实际维护周期/生命周期/兼容场景，当前40/0。提取前73a6977c在同40场景中35绿/5红；14个mutant全部BUILD_PASS后EXIT=1。
- 历史对照使用旧OnCampaignTick中两段真实调用体派生的最薄命名壳；当前执行真实RunCampaignMemoryMaintenanceCycle，并检查实际OnCampaignTick只调用一次，不保留旁路。
- 原 `same-tick-multiple-callers` 明确更名为 `standalone-multiple-callers-compatibility`：它直接调用私有helper，是原独立调用语义，不是新的实际Campaign周期。新限额证明在 `campaign-shared-*`。
- 预算运行时组件及接缝单独编入测试；sealing/business runner采用显式Compile输入，旧生成文件不参与当次编译。
- source inverse当前为56声明、2删除、2新增精确跨度、4个完整组件锁；9个防误放测试。一次更新hash前必须具备本候选实际证据，不能仅依据方法存在。
- 本轮完整交接：[共享预算HANDOFF](../../docs/handoffs/2026-09-14-b1-campaign-budget-handoff.md)。B1深来源/原子尾步与LIVE/SAVE仍未完成。

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --source-baseline 73a6977c
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate renew-deferred-deadline
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate drop-deferred-start
```

## 以下为前序结果，数字与语义绑定各自当时版本

# 当前 B1 集成与独立素材索引（2026-09-14）

当前 source inverse 已纳入 **54 个真实 MyBehavior 声明、2 个删除跨度、1 个精确新增装配字段跨度、2 个完整运行时源文件锁**。这只关闭所列源差异的集成门禁，不代表 B1 深来源预算、真实游戏或旧档验收通过。

- 素材的重建/重复键策略与结构绑定已提取到 `Refactor/Runtime/EventSourceMaterialIndex.cs`；旧 `MyBehavior.EventSourceMaterialIndex.cs` 删除。MyBehavior 仍是原始记录/存档与发布的唯一 owner。
- `run_materials.py` 当前 23/0；`--source-baseline c21523f8` 23/0；`--source-baseline 62abfdb3` 8 个有效旧版红例；七个故障注入仍可检出。只编译本次 manifest 的显式源文件，复用输出目录不会混入旧 Index.cs。
- `test_source_parity.py` 8 个测试（含 5 个依赖变体），检验完整基线恢复、正文/新增字段漂移、重复字段、未列源改动、恢复已删代码、组件/测试输入篡改和旧 partial 回流。
- 原 `unreviewedWip` 保存在 `previousUnreviewedWip`，是收到的历史清单；不得把历史的“未审”段落当成本候选状态。当前接受以精确声明/文件 hash、有效对照与最终交接为准，不只把 acceptedInverse 改为 true。
- 正常/故障验证层级与回滚见[本轮 HANDOFF](../../docs/handoffs/2026-09-14-b1-index-owner-integration-handoff.md)。原旧版结果保留于 Git，不修改为当时已通过。

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py --source-baseline c21523f8
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/test_source_parity.py
```

## 以下为历史范围/阶段记录，当前状态以上方及最新交接为准

# Memory summary main-thread boundary tests

```powershell
python tools/MemorySummaryMainThreadBoundaryTests/run.py
python tools/MemorySummaryMainThreadBoundaryTests/run.py --original
python tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate ignore-generation
```

The runtime harness compiles the production `MyBehavior.MemorySummaryMainThread.cs`
boundary with a small Campaign fixture. It covers direct and queued execution, bounded
drain, owner/Campaign/generation rejection, reset completion, wrong-thread drains and
exceptions. `run.py --original` only detects the old `e40c92d7` source and exits with
failure; it does **not** execute that business method. Its three helper mutations
must fail at runtime. The source-fragment checks are auxiliary wiring evidence, not
proof that all business operations execute behind the boundary.

This is an offline concurrency/ownership replay. It does not call a provider, load a
save, or prove Bannerlord live acceptance.

## P1-01 首轮（历史基线 `2c90ef8a`）：真实完成业务链回放

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py --original
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py --mutate worker-primary
```

- `run_business.py` 提取完整 `ProcessMemorySummaryQueueAsync`、六个实际 Apply/Mark、队列 sanitize/pending、状态归一化及对应数据模型；不在 fixture 中复写这些业务。`--original` 提取并编译执行 `e40c92d7` 的实际方法，这才是运行时旧问题红例。
- 14 个场景覆盖三类型成功/部分失败、真正 dictionary/list/draft/重试状态、顺序、空队列重入、过期/畸形结果、额外 overview 成功/失败、换 owner/读档/退休、额外等待期读档、provider 异常、来源 owner 不匹配、目标失效和实际 Apply 数量观测。
- 14 个 mutation：`worker-primary`、`worker-extra`、`worker-cleanup`、`worker-release`、`omit-release`、`omit-cleanup`、`omit-mark-daily`、`omit-mark-major`、`omit-mark-overview`、`duplicate-apply`、`accept-obsolete`、`ignore-owner`、`ignore-generation`、`ignore-draft-owner`。前 11 个修改实际调用方法，末三项修改所属守卫；必须编译成功后执行断言失败，提取/编译错误不能计为成功红例。
- 正常返回 0；运行断言失败返回 1；编译/非正常进程没有业务结果返回 2，提取异常也不能作为红例。每次在 `.generated/business/<case>/` 留生成源码、源码行号/符号/hash manifest、build.log、run.log；不修改生产或打包这些 fixture。

### 哪些是替身，哪些确实执行

`BusinessHarness.cs.txt` 的 TaleWorlds/Campaign/Hero、provider executor、底层内存存取/序列化、block/action 深层 sanitizer、目标清理、overview 入队资格、周报/声望/Native 历史/UI 发布末端均为明确替身；真实 Apply/Mark 的控制流和 list/dictionary/草稿改写执行。provider 的 incomplete TCS 强制真实 Process 跨 await，不访问网络；不能证明真实三类 Execute 的输入捕获、重试或 RPM。

只对生成的业务代码做三类测试变换：60 秒延时替换为受控异步时钟门；六个 Apply/Mark 入口记线程事件；真实 `_memorySummaryProcessing = false` 前记事件。其余实际业务条件和写入不替换。真实主线程队列 helper 与 `SaveRuntimeGuard.cs` 一起编译；fixture 直接驱动 drain，不是完整 `OnEngineTick`/Campaign。异常 popup 的 publish 可以后台调用，真正 UI 消费由原 `MemoryFailureUiBoundaryTests` 覆盖。

### 尚未解决的生产问题

当前基线中，12 个 daily 结果可同一 Tick 被一次回调全部应用（只观测处理数，非真实帧时）；过期 successful payload 虽未 Apply，仍被计入完成提示，并可能再次规划 overview。新工具输出 `BUDGET_OBSERVATION` / `STALE_UI_OBSERVATION` 如实记录，没有断言这些缺陷必须永远保留。修复须增加对应目标断言，不能只改说明或把观察值当预算已通过。

本项是完成层离线证据；同 generation 的精确 source fingerprint、实际 job/record/耗时预算、真实 provider、游戏线程/旧存档验收仍由 P1-02/03/04 与后续验收承接。

## B1 当前联合候选（继续同一批，未整批合格）

沿用上面的 `DOTNET_EXE` 和 Python 环境，一次检查这五层，而不是仅跑 helper：

| Runner | 结果 / 实际执行责任 | 明确替身与边界 |
|---|---|---|
| `run.py` | 32 个断言；FIFO、owner/load/reset、inline/queued 共享数量和实际 Stopwatch 计时，超时后下一操作留给下个 Tick | 直接驱动 drain；1 ms 时间场景使用真实耗时，其他场景隔离 JIT/机器负载；不是硬抢占或完整 EngineTick |
| `run_business.py` | 36 场景；真实入场/maintenance/分段规划、三型 Process/Apply/Mark、终态清理、部分执行异常提示和释放 | provider executor、源有效性和底层游戏端为明确 seam；不是完整来源证明 |
| `run_captured.py` | 70 场景；真实 Capture/Execute/Build/Parse/JSON/tag/retry/source；10 模型+2列表反射覆盖351个标量检查、49个可变节点分离，251次逐字段来源变动拒绝 | HTTP/game/settings/rendering为替身；不模拟真实 provider/游戏 |
| `run_writers.py` | 238 项；8个真实旧 façade、封送、参数/嵌套DTO复制 | 存储/weekly末端为替身，由 terminal 补具体真实链路 |
| `run_terminal.py` | 47 场景；真实Daily append/Save/readback、Recovery Daily→Recent及ledger、Weekly outcome/回读/ledger、Major记录和压缩块Save→在途总结失效；两处真实Apply部分写入异常 | 场景/人物事实、provider、UI显示、若干下游发布是隔离边界；不等于普通提交/导入/编辑所有调用方已验证 |
| `run_planning.py` | 24场景；真实分片分类/空洞整理、结构变化defer、metadata worker排序；4096槽分类/整理各访问4096次，每片≤8槽、每Tick≤16槽 | 单个HasPending内部仍可扫大来源；不能据槽预算说深记录已硬切分 |
| `run_commit_writers.py` | 49场景；普通Commit→Daily/Recent→回读、文本编辑真实保存、单NPC/批量记忆导入真实ReadJson/选择/Apply；过期保存与源变动拒绝 | 文件系统、选择窗口为替身；8编辑/4导入入口结构检查不是12个窗口均跑过真实UI |


```powershell
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py --mutate swallow-completion-failure
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_business.py --mutate drop-forced-rescan
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate omit-time-charge
G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_captured.py --mutate drop-nested-copy
```

正常返回0；反例必须编译成功后执行断言失败，提取/编译/工具错误不能计成红例。所有 runner 的 `--help` 列出对应故障变体。保存于忽略的 `.generated/<suite>/<variant>/`，保留输入清单、精确提取声明、生成 hash、build/run 日志；历史结果不可冒充当前源码。

`source_parity.py` / `source-review-b1.json` 只对50个已审声明逆变换，并精确恢复1个已审删除的私有预扫方法，并约束对应 runner/harness hash；随后原 whole-owner/default 校验完整执行。WIP `c21523f8` 记在 `unreviewedWip`，`HasPastDailyMemoryDrafts` 为未审删除；inverse 必须失败并列出全部未审项。不得仅刷新已审 sha256 / `productionFileSha256` 消除未解释变化。

未审 8 个符号和删除的 `HasPastDailyMemoryDrafts` 只挂 sealing/materials 反例指针（mutate 名、变体日志、30/23 里变红的用例）。`acceptedInverse` 仍为 false。未审 evidence 会锁 runner/harness hash，但这不是验收，也不能让 inverse 变绿。

```powershell
$env:DOTNET_EXE = 'C:\Program Files\dotnet\dotnet.exe'
python -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
python -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --original
python -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate ignore-empty-probe
python -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py
python -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py --source-baseline 62abfdb3
python -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_materials.py --mutate restore-fallback
```

封存 current 30/0；`--original` `62abfdb3` 19/11；8 个 sealing mutate 必须 BUILD_PASS 且 EXIT=1。素材 current 23/0；baseline 8 红；7 个 materials mutate EXIT=1。日志 `.generated/sealing/<variant>/run.log`、`.generated/materials/<variant>/run.log`；汇总 `.tmp/b1-sealing-mutate-20260914/summary.json` 与 `.tmp/b1-adjacent-20260914/summary.json`。丢掉的绿 mutate 不当红例。


### 实际变化与仍未通过的门槛

- typed copy 分离完整可变图；SHA256 流式接收完整来源/Prompt/解析依赖，不再构造嵌套转义大JSON。1000行源仍全部保留，测试分配由旧9,257,688降到1,579,504 bytes；耗时随机器负载变化，不作游戏帧率承诺。
- 入场只读raw数量，真实资格筛选集中到guarded planner；无效队列仍清掉，强制扫描/节流及terminal重复项顺序保留。候选ID排队不冒充创建了总结job。
- 初筛/extra/cleanup现按槽分片，并扣除同Tick已耗时；无效slot当片置null，之后只复制nonnull原引用，结构探针未变才发布整理结果，不以旧过滤表覆盖新入队。worker仅按冻结metadata排序/去重；计划指纹与Capture同回调校验。结构变化时只返回已采集部分计划，其余留live下一轮，不无限重启或宣称全积压已扫描。
- **仍未过深层门槛：** 单大源capture/check、单次HasPending、Apply/Mark内部、候选ID生产/维护前探测/无效owner取消仍有原子工作。槽和协作耗时预算已证，不等于深记录硬切分或同步抢占。失败全文拼接已移worker，原文/顺序及聚合期间退休均验证。
- 部分Apply异常不再静默消失：异常沿专用Completion包装进入原通知，保留已发生副作用、停止本轮、不盲重放；这是partial/unknown通知，**不是事务回滚、尾项恢复或全局exactly-once已经完成**。
- Recent-only不会虚构为Daily来源变化。普通Commit与代表编辑/导入现有真实链证据：两类旧编辑late保存和8类旧导入late保存已复现并转绿。窗口绑定开窗generation/current owner/main；编辑同时绑定记录原引用/指纹，拒绝索引移位与已改变内容。原overwrite/merge业务不改，聚合导入只加AF窗口门禁，不重写各领域owner。
- 完整恢复load/retention、其余窗口实机UI、广泛导入组合、LIVE/SAVE仍未验；继续B1，不提前进入B2。

新套复现：`python tools/MemorySummaryMainThreadBoundaryTests/run_planning.py`、`python tools/MemorySummaryMainThreadBoundaryTests/run_commit_writers.py`。每套`--help`列出反例，测试失败与工具/编译失败必须区分。

## B1 大来源 / 上下文 / overview 资格联合验证（2026-09-13，本候选）

本节覆盖上方历史数字；不把 B1 标为整批合格。实际生产入口是 `MyBehavior.MemorySummaryInput.cs` 的 Capture → Execute → IsCurrent 与 `MyBehavior.cs` 的 `HasMemoryOverviewPendingBlocks`。旧六个 Build/三个 Parse、Apply/Mark、Prompt 文字、存档身份不变。

- `run_captured.py`：109 场景；保留原 70，增加有效上下文/等效设置与场景、初始资格、getter 同步改源、retarget、state dictionary absent/null。设置 getter、重大履历名字 resolver 使用实际提取源码；游戏 registry/HTTP/时钟为替身。
- `run_captured.py --observe-rebuilds`：7 场景运行真实 Capture/Execute/retry 与实际最终 IsCurrent，记录调用和分配；普通成功与第三次成功都只 Capture 一次、各 Build 一次；重复检查无 Clone/Build。该观察不执行整个 Process/Apply，不能冒充游戏帧时。
- `run_terminal.py`：85 场景；`run_commit_writers.py`：51 场景，新增实际 Import 覆盖在途成功/失败结果。`run_terminal.py --source-baseline e77602f9` 只替换旧 Input，85 场景中 30 个 raw state 漏检变红，其余 55 绿；保留精确历史输入和生成清单。
- `run_terminal.py --admission-only`：54 场景；真实旧 `e77602f9` predicate 作为 oracle，包含 raw/已清理两种输入、首个无效记录占用 ID、未 Trim 的重复 ID 计数、非匹配块 owner、非幂等双层日期标题、真实 enqueue 和 2000 块成本。
- admission 反例用 `--admission-only --admission-mutate <name>`，准确参数以 `--help` 为准；正常返回 0，必须 BUILD_PASS 后断言失败才算有效反例。不要把恢复旧 clone 的成本红例说成旧资格业务错误。

### 新边界不冻结主体功能

后续改变 Build/Parse 的游戏依赖时，同时更新 `CaptureMemorySummaryContextFingerprint` 和对应正反对照；不是永久锁定现有 Prompt。Daily 保存实际 targetChars、header 顺序/场景 fallback 描述符，Major 保存实际被解析名字的去重 ID，Overview 保留目标与动态门槛。来源 raw 全字段摘要独立于净化后正文；原地编辑、导入和格式等价改动也能拒绝旧结果。

清理掉的是重验时重新捕获/复制/渲染，以及资格查询中丢弃的大图复制和排序。权威写入 sanitizer、仍被 planner 使用的三个 HasPending 入口继续保留。完整 raw hash、首次复制、Apply/public/weekly 与外围维护仍有原子扫描；此次没有证明深记录硬预算、实机或旧存档通过。

## B1 封存尾部协作排序（2026-09-14）

- `run_sealing.py` 当前60场景；`--source-baseline 9158132c` 使用全部对应版本生产输入，在同60例中46绿14红（BUILD_PASS，不把编译失败当反例）。保留原40例，增加Daily/Major排序、跨tick源与culture变化、别名净化及实际Campaign累计预算。
- 20个 `--mutate` 全部 BUILD_PASS 后业务断言失败；新增6个为 `unbudgeted-sort`、`unstable-sort`、`ordinal-sort`、`ignore-sort-source`、`ignore-sort-culture`、`ignore-sort-final-binding`。原14个保留。
- 513项Daily/Major：旧LINQ一个调用实测9224/8250次标量key比较；新组件每窗口最多128个排序工作单元，每单元最多一次entry比较（day及必要的name比较）+一次move，或一次结果copy。计量层不同，不能相减当CPU加速比；新总5643单元，两类都保留原引用和稳定最终顺序。
- `CooperativeMemoryQueueSort<T>`是真实消费者组件，捕获不可变key，不跨tick读取live DTO键；封存尾部持有未发布结果，最终核对实际owner列表/版本、字段和culture。无worker/新线程。显式同步调用仍排空。
- 两个原Sanitize入口保留同步排序责任；新的Normalize仅提取完全相同的原地净化/去重体。`test_source_parity.py`现10项，增加精确反拼原净化体断言；58声明/4新增span/2删除/5组件锁仅覆盖具名变化。
- 初次测试曾因planning extractor未包含新Normalize而编译失败：已补两个真实声明，不改24例断言、不用stub。business/terminal一并抽取相同函数；旧business e40c92d7跳过不存在的新helper，旧行为对照仍4/32。
- 限制：队列净化、两次标量绑定、数组分配/key捕获、单个字符串比较、pending内深来源等仍原子。同步Sanitize、整体游戏帧/内存上限、完整B1及LIVE/SAVE没有借此放行。冻结日志及最终版本见根HANDOFF指向的排序交接与验收JSON。


## B1 完整raw来源编码（2026-09-14）

- `run_captured.py`现在116/0；`--source-baseline 8bcde78b`在相同116例中112/4，BUILD_PASS后3个分配门槛和1个异常UTF16区分变红。35个`--mutate`都是真实编译后的断言红例（原28保留，新增7个字段/帧/整数/列表/码元/完整buffer反例）。不把编辑器、计划或context的通用JSON摘要改为新格式。
- 新`MyBehavior.MemorySourceFingerprint.cs`只映射原private DTOs的122字段及状态presence；真实主线程Capture和最终IsCurrent调用独立`Refactor/Runtime/MemorySourceFingerprintWriter.cs`，完整raw内容逐字段进入SHA256，有界4096-byte缓冲。不是只比较count/版本号，不省略未渲染内容，不改变Clone/Build/Parse/Apply/Mark。
- 反射驱动的三类every-source-field总294次修改继续拒绝旧来源；包括同generation、render等价字段、nested list、状态null/absent。将来新增DTO字段时必须补编码与测试；测试会因未覆盖字段而失败，不能只刷新hash。
- `run_fingerprint.py`用独立BinaryWriter/MemoryStream/SHA256 oracle验证9个跨buffer向量，开启checked arithmetic；5个finish/dispose/结构变动拒绝守卫。原始UTF16码元逐个编码，因此不同未配对代理项不再在UTF8 replacement fallback中合并。这个修正仅覆盖新的raw来源摘要，非所有通用JSON指纹。
- 1000记录×12次：新三类raw摘要分配约0.06–0.10MB，旧约1.9–5.1MB；报告实测数字、未锁定耗时或游戏FPS。仍有完整O(N)原子来源遍历、初捕获/净化/Apply，B1不因此通过整批门槛。
- captured/terminal/sealing编入实际新编码组件；captured的8bc历史输入和terminal的e776历史Input不编当前新组件，显式Compile清单排除残留生成cs。captured旧版读取和hash都绑定同一指定版本，不用现工作树hash伪装历史来源。
- 严格MyBehavior inverse仍58声明/4新增span/2删除；另把Input的4个具名声明精确反换到8bc，并证明其余完整Input（含原通用JSON摘要、async/parse/release）未改。整文件组件锁增为8，守卫11项。
- 构建/相邻/最终版本与回滚见根HANDOFF指向的本轮详细交接、`docs/audits/2026-09-14-b1-raw-digest-verification.json`。不把离线fixture/实际DLL元数据当作LIVE/SAVE验收。


## B1 owner净化落实到draft预算（2026-09-14）

- `run_sealing.py` 当前75/0，`--source-baseline 40b92e67` 同75例62绿13红；26个mutation均BUILD_PASS/EXIT=1。旧版257/65条草稿一次全净化，新版每有限窗口最多8条，累计沿用Campaign授予；同步调用仍排空。
- 保留原60例，新增15例覆盖原sanitizer oracle、empty-first占key、同对象/共享line与trigger别名副作用、原引用、源追加/替换/同slot/key变更、空winner长新lines、清空既存lines、重置、异常和发布时替换。过期key必须重走owner封存/队列索引，不能只重新排序。
- `SanitizeDailyMemoryDraftEntry`精确提取原内层体：只将continue与list.Add变为返回值。根同步入口与cooperative尾部共用，主线程原地/后台clone、标签/AFEF/marker规则不变。`test_source_parity.py`增加精确40b内层体还原，现12项；主文件58声明/5新增span/2删除和Input4声明精确inverse、8组件锁。
- 元数据按每draft原子操作可提前可见；删/去重/排序后的list保持私有，直到当前owner列表结构、全部key/日期与empty/include条件通过。并非整个owner事务，也没有原子回滚承诺。
- 新owner排序复用`CooperativeMemoryQueueSort`的day-only+constant-name路径。测试分别记`owner-sort-unit`和`queue-sort-unit`，防止旧队列排序用例误停在新owner排序。原queue fault已限定到QueueTail类；曾因同名guard数量增加而拒绝提取，修正selector后才计入有效红例，不扩大到两个类掩盖失败。
- 一条含1024行的draft仍在一次record操作里净化；metadata key/empty绑定也仍原子。75/0不代表深line、初捕获、raw、Apply预算或真实游戏完成。
- 当前captured116、旧8bc112/4及主线程/后台clone两个相关故障再验；另外raw摘要控制保留40b固定版本证据，Input/编码器源码未变，不冒充本轮重新运行全部35个。其余相邻/最终构建结果见当前HANDOFF。
