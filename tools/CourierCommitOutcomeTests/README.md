# Courier 最终提交不确定结果

运行 `python -X utf8 -B tools/CourierCommitOutcomeTests/run.py`，`--original` 对照29448d1b实际旧声明；`--mutate` 可取 `false_no_effect`、`retryable_failure`、`lose_inbound_effect`、`unguarded_diagnostic`。旧红/故障变体应编译成功后以行为失败结束，编译错误不算反例。

实际编译 `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs`、InteractionCommitResult DTO 和 PendingOperationRegistry，复用 GameLifetimeTests 原19项队列/退役/回执断言，追加15项内联/物理队列检查：部分副作用后异常、空回执、诊断失败、未开始拒绝、真实成功回执、入站缺回执的效果状态保持与原清理次数。

- 开始前拒绝：RejectedByValidation / NoConfirmedEffect。
- callback已进入但抛异常或未提供回执：NonRetryableFailure / UnknownAfterStart。ErrorCode保持原值，不伪造HistoryWritten/ActionsExecuted为true。
- 已得到合法回执：原样保留；入站清理转译保留EffectState，不把确认成功或未知部分执行都压成无副作用。
- 仅日志失败不改变上述结果。

这不承诺事务回滚，也不允许自动重试整轮。送达、会话和资产业务仍归原owner；本套Session为fixture，不是实机。测试生成副本将30秒deadline缩为120毫秒，生产时限不变。

`source_parity.py` 使用固定29448d1b及逐段精确差异还原整文件；GameLifetime旧证明先消费这个逆变换，原生产hash不刷新。配套同候选双版本Stage/API元数据/实际Host回放单列在HANDOFF。
