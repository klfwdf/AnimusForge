# Game lifetime / queued operation retirement checks

本工具只验证 AF 主体生命周期和现有主线程队列，不部署游戏，不代表全范围收尾或实机验收。

## 实际生产接线

- `SubModule.InitializeGameStarter / OnGameEnd / OnSubModuleUnloaded` → `AfCampaignRuntimeLifecycle` → 实际注册的 MyBehavior、ShoutBehavior、CourierDeliveryBehavior 实例。
- `GameLifetimeCoordinator` 按 Game 引用识别开始/替换/结束；旧结束回调无权结束新 Game。先推进 generation，再独立退役各 owner。
- `PendingOperationRegistry` 只登记现有队列待办的退役回调，不是新队列。reset 后允许下一会话，seal 后拒绝旧 owner 的新工作；清队列窗口暂停准入。
- Native 准备函数、Native 最终动作、Courier owner phase 和 Courier 最终 commit 共享各自 owner 的退役登记；未 claim 的等待立即失效，已经 claim 的结果不被定时器或退役覆盖。
- Courier 原最终 commit 及原 inbound 无回执清理主体仍是唯一执行者；新文件是调度/退役边界，不是第二个动作执行器。

## 重跑

在仓库根用可用 Python/.NET SDK 运行：

```powershell
python -X utf8 -B tools/GameLifetimeTests/source_parity.py
python -X utf8 -B tools/GameLifetimeTests/run.py
python -X utf8 -B tools/GameLifetimeTests/run_bindings.py
python -X utf8 -B tools/GameLifetimeTests/run_commit.py
python -X utf8 -B tools/GameLifetimeTests/run_memory.py
python -X utf8 -B tools/NativeActionDispatchOutcomeTests/run.py
python -X utf8 -B tools/NativeCompletionBoundaryTests/run.py
```

`run.py` 默认还运行 12 个生产行为故障变体，必须是编译成功后断言失败，不能把编译错误计作反例。
`run_commit.py --mutate drop_claim|expire_claimed|skip_retirement` 以及 Native 原测试的故障选项返回非零是预期。

旧红对照：`run.py --original-callbacks`、`run_commit.py --original`、Native `--retirement-baseline` 使用固定 807bc5b9 的实际声明。分别暴露旧 Game 回调未退役、重复 commit 回调执行以及 Native 清理后仍在等待；不是对全部旧功能的否定。

## 证据边界

- registry/coordinator/engine adapter、三个生命周期回调、Native/Courier 派发方法和新 retirement partial 为实际源码。
- MyBehavior 在清理开始即关闭新提交准入，覆盖 dispatcher 尚未创建及已创建两种同代竞态；`run_memory.py` 执行真实宿主/dispatcher/退役方法。
- Game/TWParallel/注册失败、低层 reset 和游戏业务为明确 fixture。各场景有物理 worker/main 队列；实际 DLL 另外做双版本 Stage、元数据及 Courier Host 回放。
- Courier commit 测试只把 deadline 在生成副本从 30 秒缩为 120 毫秒；Native 既有测试短时钟保持。生产时限不变。
- `source_parity.py` 先对新改动做固定基线整文件逆变换，再允许既有三渠道/历史/内部 ports 的旧证明继续运行。不得仅刷新旧生产 hash 使测试变绿。
- 此处没有验证真实 Campaign/Mission 结束顺序、旧存档、live Economy/AFEF、全部 TTS/第三方订阅，也没有交付外部三渠道 SDK。
- 异常 commit 的既有失败回执语义未在这里改写为“全部回滚”；仍不可根据失败就自动重放整轮。
