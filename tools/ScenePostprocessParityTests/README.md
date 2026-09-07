# Scene 后处理全方法差分回放

目的：验证把 `TryRunSceneUnifiedActionPostprocess` 拆分为 prepare / network / complete 后，没有改变原有资格裁剪、候选数据传递、请求输入、领域归一化调用顺序、输出拼装和周报触发边界。

## 方法与可信边界

- Oracle 从 Git `d40808b3:ShoutBehavior.cs` 提取完整原方法（不从工作树复制期望实现），固定 SHA-256。
- Candidate 从当前生产文件原样提取 wrapper、work item 及三个阶段。只改变其容器类，不改函数体；不抽掉失败分支。
- 两者分别编译执行，使用同一组确定性的 Hero/Character/资产、领域 helper、Prompt builder 与固定 LLM 结果 stub。逐字符比较结果和**完整 helper 参数/调用顺序**，因此候选列表、规则标签、history、reply、direct/relay 和预算发生变化也会失败。
- 原有规则文本拼装、规则合并、输出去重合并、上下文追加及 Scene user-prompt 转发 helper 同样从 Git 原样提取执行。其他领域 normalizer 使用可观察的标记返回值，不复制生产算法当作自己的期望实现。
- 显式三阶段另验 prepare 不发请求、不归一化、不写周报；network 只收到字符串并只发一次请求；fallback 不写周报。
- work item 完成一次后再调用必须拒绝，第一次 normalizer 抛异常后也不能重新执行。

**这验证的是生产 postprocess 方法的编排与三阶段搬迁等价，不是生产域 normalizer 内部正确性，不是最终真实 HTTP Prompt 模板渲染，不是游戏对象、线程调度器或真实金币/存档验收。** Queue 的世代/主线程/回调保护需由实际外层入口测试覆盖。`firstTurn` 在本方法对应 `replyIsDirectPlayerResponse`；历史/AFEF 的最终实际写入不在此 harness 内。

## 覆盖

71 组差分 fixture（各执行 old、new-wrapper、new-explicit-phases），以及 2 组完成去重检查：

- 各领域单独选中、全部同时选中、20 个路由 gate 分别拒绝。
- 原始 preprocess hits 补入 vassalage / kingdom_agenda / persistent debt / noble gathering；政策目标不合格、relay 禁止直接玩家政策。
- Hero / Party / Merchant 各自物品和金币；过滤与全量资产、现成快照、空 owner、候选异常退化；部队招募等级与领地候选。
- Duel stakes、Scene summon/guide、空目标、战斗 Scene 移动排除且保留处决规则、无可保留规则、多人 relay 与单框说明。
- 非Hero独立氏族和平、入队抑制、native 围城投降、NPC 投降、GCCZ 独占后处理。
- no-aux、既有 mood、空模板、空规则、空后处理结果、网络失败、空人名/历史/回复。
- 后处理只完成一次、归一化抛错后禁止再入。

另外提供 5 个**仅修改生成副本**的反例：丢 reward gate、relay 错当直接回复、丢 vassalage hits、跳过 Duel normalize、移除完成去重。必须在行为断言处失败，编译失败不算检出成功。

## 运行

在 `G:\AFMOD\AF-REFACTOR` 中：

```powershell
python tools\ScenePostprocessParityTests\run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe
python tools\ScenePostprocessParityTests\run_mutations.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe
python -m unittest discover -s tools\ScenePostprocessParityTests -p test_extraction.py
```

Oracle 自检（旧方法与自身执行对比；不是当前生产回归）：

```powershell
python tools\ScenePostprocessParityTests\run.py --source-ref d40808b3 --output-name oracle-self
```

可用 `--source-ref <commit>` 对具体提交执行。需要保留 `d40808b3` Git 对象及现有 `tools/ChannelCutoverBoundaryTests/run.py` 声明提取器。

所有生成源码、隔离 net8 项目、源码哈希和运行日志留在本测试目录 `.generated/<output-name>/`，该目录已忽略。无 NuGet 包引用、无网络调用、无游戏 Stage 写入、无真实配置或存档操作。

## 结果解释

成功行明确显示 `domainHelpers=STUBBED live=NOT_RUN save=NOT_RUN apiNetwork=NOT_RUN`。不能用本测试将阶段八、Scene 完整功能或实机验收标记为 DONE；同时需运行官方双版本构建、生产 DLL 领域 normalizer 回放及外层 Queue/Host 验证。

## 独立 Queue 实际控制流回放

```powershell
python tools\ScenePostprocessParityTests\run_queue.py
python tools\ScenePostprocessParityTests\run_queue_mutations.py
```

这两项不替代、也不修改上面的 71 组差分断言。`QueueHarness.cs.txt` 另行原样提取当前 `QueueDeferredScenePostprocessActions`、WorkItem、Complete 和 Request，Prepare 返回固定工作项（它的完整规则/候选编排已经由前述差分测试覆盖）。

37 个实际执行场景包括：

- 上游 main 请求传入已经过期的 expected generation / session 时，入队不能重新捕获当前值放行。
- generation / scene session / conversation epoch 分别在 prepare 前、network 后、dispatch 前失效，动作和 relay 不落地。
- 目标在对应三个位置失效时返回专用 `-2`，而非普通 `-1`。
- relay-only 路径也检查世代；speech 回调前失效不执行动作、不发布 relay。
- direct / relay 各自 playerText、真实 NPC reply、firstTurn 语义；候选及 rule hits 快照不受调用方修改影响。
- 超时、speech 拒绝、queue/main 阶段丢弃、网络异常、prepare/normalize 异常不重复请求或动作。
- 人为阻塞 network，验证 request deadline 能先完成 Task 为 `-1`；随后放行网络，仍无 normalize / action / relay。
- 真实 `Task.Run` 工作线程，专用物理主线程和 BlockingCollection 派发。game-property getter / runtime eligibility / prepare / normalize / dispatch 必须在该主线程，网络必须不在主线程。后台游戏读取即使被生产 `catch` 吞掉也累计为失败。
- 请求的 AsyncLocal mentions 与 6 个 runtime target 在阶段执行时正确恢复，退出后主线程原有上下文不受污染；异常退出同样检查。

生产 Queue 控制流不改写。仅测试宿主的独立 timeout 常量缩短为 100 ms（speech）和 700 ms（整体请求），不改生产常量/配置，不模拟完整游戏主循环。实际 Speech worker、战斗动作执行器、AFEF/存档写入、UI 忙状态等仍由 stub 代替；本测试不能证明实际游戏派发器或实体动作正确。

7 个 Queue 反例只作用于生成副本：忽略 generation、移除 dispatch 检查、丢 ExecutionContext、speech 不设发布 guard、网络阶段读 Mission、忽略上游 expected generation、忽略上游 expected session。必须在运行期行为断言失败，编译失败不计入成功。

日志在 `.generated/queue-current/` 及 `.generated/queue-mutant-*/`；附每个原样提取声明的 SHA-256。

## Gate 所有权竞态红绿回放

```powershell
python tools\ScenePostprocessParityTests\run_gate_red.py
python tools\ScenePostprocessParityTests\run_gate.py
```

Gate runner 原样提取 Register / Get / ForceClear / Wait 四个真实生产方法和它们使用的字段，不替换 task continuation 或 await 分支。游戏提示是 stub；独立测试常量将等待超时缩至 120 ms，不改生产值。

确定性控制：进程使用一个默认 ThreadPool worker，通过 fault-log barrier 和后续 marker 确认 A 的**真实** continuation 已执行完；两个手动 SynchronizationContext 控制 A/B **真实 await 续体**的释放顺序。不是源码字符串检查，不以睡眠时间猜测旧 continuation 已完成。

`d40808b3` 必须编译成功后，在以下三条运行断言失败：

1. Register A → ForceClear → Register B → A 晚完成，A 错扣 B 计数并提前完成 B gate。
2. Wait A 被清理后启动 Wait B，再放行 A 的 finally，A 错清 B waiting / 恢复 B 正在借出的 processing。
3. A 的 timeout 续体延迟至 B 注册后再执行，A 错 ForceClear B。

当前生产修复验证 6 组：晚 task、晚 waiter（同会话/新会话）、晚 timeout、过期排队提示不发布但当前 B 提示正常、同 gate 多任务正确计数、null/已完成/fault/cancel/自身 timeout 清理。依赖 captured gate 身份、wait owner 和上下文隔离。

红测日志在 `.generated/gate-red-*/`；绿测与源码指纹在 `.generated/gate-current/`。它验证共享 gate 的 Task/等待状态所有权，不代表游戏实体、玩家操作或实际 UI 渲染已经验收。
