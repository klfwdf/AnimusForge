# AF Memory 调度职责拆分 HANDOFF（2026-09-15）

## 当前结果

**按职责计划完成了M1/M2的“捕获/接受主线程调度”子包。** 独立组件已经服务真实捕获、规划、写入、总结完成，不是只加接口或另拆一个partial。整体仍阶段8/B1，首次深复制和全部记忆owner尚未完成。

- 生产/测试：`61d578926329ace61bf6b6ae43e12bf7d89b4696`；前生产`9617f96a`，起点文档`b7c90201`，检查点`94e05a2f`。
- 工作区G:/AFMOD/AF-REFACTOR；分支codex/af-framework-skill-delivery-20260911。fresh fetch远端af618912，保留本地修改；本轮未融合/推送。
- 自动化仍PAUSED；未部署、操作存档、切默认或修改制作组玩法/公开V1。

## 1. 真正迁走了什么

| 责任 | 现在归属 |
|---|---|
| 待办队列、单次claim/retire、异步完成 | 独立 `MemorySummaryDispatcher` 持有唯一状态 |
| inline/queued共用的操作额度与实际耗时 | 同一dispatcher；规划读取该owner的ElapsedTicks，不另存副本 |
| 普通失败false、部分完成异常原样传播 | dispatcher两个明确入口，共用一条队列，不重放副作用 |
| 所属线程、owner/档代、Campaign身份、动态设置/诊断 | `IMemorySummaryDispatchHost`，由MyBehavior的薄adapter实现 |
| 每owner实例发布、现有调用兼容 | 原Host仅惰性初始化和转接；并发首次提交用同一个CAS发布实例 |

原MyBehavior.MemorySummaryMainThread **156→57行，家族减少99行**。新增独立runtime128行、contract22行；总代码略增，但mutable scheduling状态实际离开了MyBehavior。不是靠增加partial隐藏大类，也不把这次迁移说成全部主体拆完。

已删除旧Host的并发队列、WorkItem实现、动作/耗时计数器、HasAllowance/执行/claim/retire等重复算法；保留的是现有真实调用点需要的四个薄入口，没有第二条业务实现。

## 2. 稳定接口与线程边界

[契约说明](../architecture/af-memory-dispatch-contract.md)明确了五个Host成员及Submit/SubmitCompletion/Tick/Reset语义：

- Host是**同DLL internal代码接口**，不是子MOD SDK。新的public能力没有开放，API.V1只读及制作组ports保持原样。
- worker提交只能用线程判断、owner/档代检查；Campaign验证只在主线程执行operation之前进行。
- 当前仍2个operation/原动态设置耗时，不伪装成每tick最多2条记录；超长capture/Apply仍可能是一个原子操作。
- Reset只退役未开始工作；已执行部分不回滚，网络不自动取消，完成异常不盲重放。
- 空闲Tick/Reset不创建dispatcher；并发首次提交只使用发布胜出的实例，避免任务进入遗失的候选队列。
- 纯runtime只依赖System与内部contract，不引用TaleWorlds或MyBehavior，也不拥有记忆/资产/Prompt数据。

内部接口未来如需改变，必须迁移全部真实consumers和测试；不能静默改变成功/失败、线程或生命周期承诺。公开接口破坏性变更另行版本化。

## 3. 代码坐标（61d57892，一基行号）

| 路径:行号 | 符号 / 责任 | 未覆盖 |
|---|---|---|
| `Refactor/Contracts/IMemorySummaryDispatchHost.cs:9–22` | typed Host线程/生命周期/设置/诊断契约 | 非公开SDK、不包含业务存档 |
| `Refactor/Runtime/MemorySummaryDispatcher.cs:16–128` | 独立队列、待办状态和预算owner | 不读取游戏对象、不负责来源复制/指纹 |
| 同文件 `62–76` | `Submit`，inline/queued和提交-读档竞态结算 | 不表示业务持久化完成 |
| 同文件 `80–90` | `SubmitCompletion`，原异常身份保留 | 不承诺整事务回滚 |
| 同文件 `110–127` | `Tick` / `Reset`，主线程排空与未claim退役 | 不取消已开始操作 |
| `MyBehavior.MemorySummaryMainThread.cs:18–44` | 惰性发布及 `MemorySummaryDispatchHost` 实际游戏适配 | 不保留第二套队列或计数器 |
| 同文件 `46–57` | 原Run/Completion/Tick/Reset薄转接 | 为真实callers保留，不是待删除死代码 |
| `MyBehavior.MemorySummaryPlanning.cs` | 两处原 `_memorySummaryMainThreadElapsedTicks` 读取改为新owner只读值 | 其余规划算法原样，尚未整个迁出 |
| `tools/MemorySummaryMainThreadBoundaryTests` | helper和7类相邻runner编入真实新组件；fixtures读新owner诊断 | 游戏/provider有明示替身，不冒充实机 |

[81点代码图](../architecture/af-framework-code-map.json)绑定实际source revision，记录版/工作树均通过。规划文件还通过精确检查：与9617f96a相比仅两处耗时getter替换。

## 4. 本轮实际验证

[验证JSON](../audits/2026-09-15-memory-dispatch-owner-verification.json)保存源码、命令、日志/214份冻结材料hash及6份产物/marker/Stage比对。

| 检查 | 结果 |
|---|---|
| 当前helper | **37/0**：共同32项 + 新5项惰性/并发首次提交/typed异常契约 |
| 原9617f96a owner | **共同32/0**；不声称新5项也在旧实现运行 |
| 七个故障控制 | 档代/owner、无限drain/inline、忽略耗时/漏计时/不重置，全为BUILD_PASS后的有效断言失败 |
| 捕获 / 业务 / 规划 | 116/0、36/0、24/0 |
| writer / 封存 / terminal / commit-writers | 238/0、88/0、85/0、51/0；执行真实新dispatcher |
| 精确守卫 | **15/0**，含独立层依赖、薄Host形态及规划两处getter对照 |
| 双版本构建 | Debug/Release×1.3/1.4/Bootstrap **六项Stage通过**；6份输出/marker/项目Stage一致 |
| API与实际DLL | 119断言、256并发查询、预期CS0122、4份DLL元数据532断言 |
| 存档身份 | 对9617f96a，146 SyncData keys / 36 behaviors保持，模块仍只加载Bootstrap |

测试接线初期曾漏新组件编译输入，以及一处fixture限定对象后的取反写法不正确；修正测试适配后重跑通过。这些编译失败没有当作业务红例或算入故障控制。当前测试没有保留假的旧队列/计数器来凑PASS。

未重跑所有无关历史suite；未进行真实Campaign/Mission、旧档、真实资产/AFEF、TTS/provider、外部DLL运行验收。成员之前的实机反馈不自动套给本候选。

### 复跑

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run.py
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run.py --source-baseline 9617f96a
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate ignore-generation
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run.py --mutate unbounded-inline
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_planning.py
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/test_source_parity.py
```

完整命令/依赖在JSON；Stage使用原脚本及已有G盘SDK，不改一键流程、不加-Deploy。日志与生成输入在`.tmp/dispatch-owner-20260915/final-evidence/`，不参与产品编译或上传。

## 5. 玩家视角核对

已在离线真实代码验证：后台捕获/写入只能通过所属主线程；关档/换owner后的晚任务被拒绝；排队保持顺序、不会丢掉并发首次提交；普通失败能结束，部分写入后的异常仍由协调者感知，不重复应用。相邻实际Daily/Recent提交、编辑器/导入回调和封存也通过。

这不是实机录像。实际验收仍需长历史总结中正常交互/换档、任务失败恢复与真实副作用账本；必须使用获准测试环境/副本，不自行覆盖游戏与存档。

## 6. 下一包与收尾边界

本次完成的是职责计划M1/M2的**线程接受基础子包**，不是M1/M2全部完成。按[职责计划](../phase8/af-core-responsibility-decomposition-plan-20260915.md)继续：

1. **首次capture/copy**：在本次独立调度与真实时间记账上建立实际record预算；三类来源复制全过程检测结构/字段变化，最终重验当前来源，不能只看ref/count。
2. **完整writer/接受**：明确所有源写入者，保持原完整raw验证直至有充分替代证据；处理复制、净化、最终绑定、Apply/public/weekly的原子成本，不能把两个callback当预算完成。
3. 继续真正提取Memory数据/历史/事实/summary owner；然后B2完整三渠道（先Courier线程）、B3内部生命周期/大类剩余职责，再已选public与P5/P6。

“接口稳定、拆分细致”的门槛仍是实际责任/状态迁移、消费者接线、兼容与正反回归、旧算法删除；不是为了减行数转移到一个新的万能类。

## 7. 回滚与交付

- 本轮定向revert `61d57892` 及其文档/地图即可回到前一实现；先核对后续依赖，不hard reset/强推。
- 两份用户草稿及指定本地Native简明版hash不变、不暂存；没有写其他AF/GCCZ工作区。
- 代码与交接仅本地提交，自动化仍暂停。本地主体简明版：`.tmp/AF调度模块拆分简明交接-20260915.md`。
