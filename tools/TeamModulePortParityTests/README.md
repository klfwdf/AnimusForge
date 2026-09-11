# 制作组 typed port：接线等价验收

## 一句话

**新增的是调用接缝，不是第二套政策、宴会或 GCCZ 业务。** 这套测试检查“转交给谁、参数有没有变、事实结果有没有被复制或吞掉”。

## 运行

```powershell
python tools/TeamModulePortParityTests/run.py
```

默认 SDK 为 `G:/AFMOD/.dotnet-sdk/dotnet.exe`；可用 `--dotnet` 指定。运行器复用 `ModuleFrameworkApiTests/run.py` 的离线项目生成与隔离环境。生成物/日志在 `.generated/current/`，不提交。

默认运行三项行为 mutation；`--skip-mutations` 仅用于快速重跑，不能替代完整交付验收。`--baseline` 默认为初版前的 `df6ab928`；未来合法 owner 业务变更必须先重新审核基线，不应为使测试变绿而随便改基线。

## 两层证据

### 1. 原调用点没有改业务

- 对 `MyBehavior.cs`、`ShoutBehavior.cs`、`ShoutBehavior.ScenePostprocess.cs`、`CourierDeliveryBehavior.cs` 四个业务 owner，将批准的 `TeamModuleServices.<port>.<method>` 接收者逆变换为原类名，并移除唯一新增 using。
- 逆变换后的**整份文件**必须与 `df6ab928` 完全相等；这会抓到意外变动的参数、条件、调用顺序、异常处理、历史、事实与通知逻辑。
- 首版共 **13 个方法、31 处实际调用点**，不是仅登记目录。
- `SubModule.cs` 单独检查 load/unload 注入的精确差异及初始化顺序，不将它冒充业务 owner。
- 从实际政策、宴会、GCCZ 原 owner 抽取 13 个声明，与记录器 stub 的类型、参数名称、`ref/out`、可选默认值逐项核对；结果写入 `owner-signatures.json`。

### 2. 实际 adapter 的委托行为没有变

- 将真实 `TeamModulePorts.cs`、`TeamModuleAdapters.cs`、`TeamModuleServices.cs` 链接进 net8 测试程序。
- 游戏类型和原 owner 用只记录参数的 stub 替代，测试不模拟政策判定或攻城处置结果。
- 验证每次调用只到原 owner 一次；Hero/Character/规则集合引用、各段文本、布尔/索引不串位；`ref` 正文、`out` 原因/事实/通知/handled 原样传回。
- `handled` 与返回值可以不同；空目标、null 返回、false、默认参数与异常同实例均不得被“兼容兜底”吞掉。
- 三个 adapter 保持无实例状态，服务返回固定实例；不持有存档或游戏对象。
- 三个**仅在生成副本中**执行的 mutation（反转政策资格、反转 Siege selected、互换玩家与 NPC 文本）必须被运行时断言捕获，而不是靠编译错误假报捕获。

2026-09-11：**308 个运行时断言、13 份原 owner 签名、四 owner 全文逆变换、独立生命周期逆变换及 3 个 mutation 全部通过。**

## 证据边界

这些是接线/委托契约，不是模块业务单测或真实游戏验收。生产源码双版本构建、完整后处理回放与旧存档/live Economy/AFEF 仍需独立证据；不能据此删除仍被使用的原 owner。


## 后续 Native 准入变更的严格例外（2026-09-11）

原 31 个制作组调用点仍全部保持。随后新增 Native 准入修改了 5 个**不含任何 TeamModuleServices 调用**的声明：两个入口、Native 完整方法、动作队列、ConversationEnded 处理。`reviewed-native-admission-deltas.json` 逐个冻结这些已审阅声明的 SHA；本工具只有精确 hash 一致才将它们还原到历史基线后继续做全文逆变换。任何后续改动仍失败，不能笼统跳过整个 Native 文件或随意刷新基线。这些方法的新行为由 `tools/NativeConversationAdmissionTests` 的真实准入源码/入口及动作队列执行、反例和 mutation 负责验证。原 13 个 adapter 方法的参数/回执断言和 3 个 mutation 不变。

展示 scope 接入后，原 public 可用性查询的三行调用方注释更新为兼容说明。该注释以明确的 current/original 对纳入逆变换，只允许 `//` 行注释；不忽略其它注释或代码，也不放宽原行为断言。
动作队列失败修复只更新已登记 dispatch 声明的精确 SHA，其它声明不变；dispatcher 及调用方完成门禁/动作失败 UI 分支由 NativeActionDispatchOutcomeTests 的真实源码片段和反例验证。31 个制作组接缝与原断言保留。

后续动作队列等待同样只刷新既有派发声明 SHA；新 88 检查 / 9 变异负责证明等待期限、未执行取消和已开始归属，13 方法 / 31 调用 / 308 断言不变。

Native 收尾续作只刷新既有 dispatch、完整 Native request 声明 SHA，并将 private NativeConversationGameActionResult（新增 FinalVisible）作为一个精确冻结声明纳入反向恢复。均无 TeamModuleServices receiver；未删除原 13 方法 / 31 调用 / 308 断言或 3 变异，完整文件对照不放宽。

记忆接受续作将旧 public 六参 CommitExternalDialogueHistory 的薄 facade 纳入精确 hash 反向恢复；原业务入口/receiver 不变。新 MyBehavior.DialogueHistoryCommit.cs 的 strict owner 由 NativeCompletionBoundaryTests 对原方法作逐字逆变换对照，并验证真实 Native 调用与 scene 参数，不跳过原 308 断言。
