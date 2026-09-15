# Courier 已执行动作回执边界

直接抽取实际 `RunCourierOwnerPhaseAsync<T>`，在物理主线程执行动作，另一个线程触发取消或超时。测试不加载游戏；只在测试副本将30秒deadline改80ms。

```powershell
G:/Python310/python.exe -X utf8 -B tools/CourierOwnerPhaseTests/run.py
G:/Python310/python.exe -X utf8 -B tools/CourierOwnerPhaseTests/run.py --original
G:/Python310/python.exe -X utf8 -B tools/CourierOwnerPhaseTests/source_parity.py
# 有效反例：cancel_claimed / timeout_claimed / drop_claim
G:/Python310/python.exe -X utf8 -B tools/CourierOwnerPhaseTests/run.py --mutate cancel_claimed
```

16项：正常、未claim取消/超时、已claim取消/超时、执行后档代失效、真实异常保留。旧4140bd04同16项4失败，新16/0；三个反例编译后被行为断言拒绝。执行前主线程暂停有握手，避免把队列竞态当成确定前置条件。

**有意契约修复**：未开始可以取消；已开始不能把超时/取消伪装为“没执行”，应等待真实回执。不能回滚正在执行的游戏动作，也不能强制中断卡死的动作。真实结果完成后档代失效仍拒绝向新存档交付。

`CourierPostprocessOwnerRegressionTests`原39项中，CancelAfterStart原来要求已发生副作用但仍返回取消；本轮明确改为验证返回真实结果且副作用仍恰好一次，其余断言保留。独立新用例复现原缺陷，不是只改旧测试让其变绿。

`source_parity.py`记录三段精确替换，保持其余整个DetachedPostprocess文件不变，并为历史测试提供严格旧声明还原。原历史owner-phase哈希没有被无证据刷新。
