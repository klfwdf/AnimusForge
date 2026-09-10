# Native 动作派发结果回归

在 `G:\AFMOD\AF-REFACTOR` 运行：

```powershell
$env:PYTHONIOENCODING = 'utf-8'
python -B tools/NativeActionDispatchOutcomeTests/run.py --original
python -B tools/NativeActionDispatchOutcomeTests/run.py
python -B tools/NativeActionDispatchOutcomeTests/run_mutations.py
```

SDK 使用 `G:\AFMOD\.dotnet-sdk`；复用项目 `.tmp/dotnet-cli` / `.tmp/nuget-packages`，禁用开发证书生成，无外部包依赖。产物与日志在本工具 `.generated`，不提交。

## 真正执行的部分

- 从源码抽取实际 `ApplyNativeConversationGameActionsOnMainThreadAsync`、result 类型和 Native 调用方的完成判定片段。
- Link `ShoutBehavior.NativeActionDispatch.cs` 的真实执行边界、诊断隔离和类型化异常；使用现有 `ActionExecutionEffectState` 的原始声明。
- 抽取实际普通/主动 UI 的动作失败 catch 分支，验证 suppressReadyNotice 和失败报告，不调用重试入口。
- 队列、游戏对象、实际业务 Core、最后历史存储、UI 通知服务为明确 fixture。历史计数只是证明完成分支是否被越过，不冒充真实 AFEF/存档验证。UI 展示 scope 本身由已有展示套件另测。

## 原反例

`646dd987` 的实际队列执行：

1. fake Core 先产生一个效果再抛错，Task 仍返回原回复，调用方穿过真实正常完成判定并进入模拟历史存储。
2. enqueue 后日志抛错，调用方提前正常完成，但队列仍保留动作且随后执行。

## 当前覆盖

**59 PASS / 0 FAIL**：direct/queued 正常返回、owner discard、执行前校验拒绝/异常、执行后异常与原始 cause、null 结果、诊断异常、enqueue 前/发布后/已 claim 后异常、执行前失效、重复 callback，以及实际 UI 失败分支。

六个行为变异：丢失 start 边界、允许 null 作为普通结果、吞掉 owner 异常、让诊断异常影响控制流、去掉 claim 去重、让失败发布的 callback 继续执行。均须由运行时失败捕获，不接受编译错误冒充。

## 语义边界

- `NoConfirmedEffect` 只描述本次派发未进入 owner；**不保证此前 raw/taunt/自然动作等没有效果**。
- `UnknownAfterStart` 表示进入 owner 后无法确认完整结果，不回滚或抹掉已发生的效果，也不自动重试整轮。
- 正常 Task 返回只代表此次同步调用返回，不代表每个异步玩法结果最终成功；未虚设统一 ConfirmedEffect。
- 整个历史/事实原子回执、排队一直不被消费时的超时/取消、真实游戏验收仍未由此套件解决。
