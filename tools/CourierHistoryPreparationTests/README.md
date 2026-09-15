# Courier 双向历史捕获边界回归

验证生产 `CourierDeliveryBehavior.HistoryPreparation.cs`，不是另写一份等价实现。运行器同时抽取真实 `RunCourierOwnerPhaseAsync<T>`，在独立物理主线程/后台线程上执行。引擎、Session、历史 provider 为替身；不代表真实 Campaign 验收。

## 运行

在仓库根使用 Python 3.10+；`run.py` 复用现有 API 测试编译工具，本机 SDK 为 `G:/AFMOD/.dotnet-sdk/dotnet.exe`。该路径只在测试中，不进入生产代码。

```powershell
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/run.py
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/source_parity.py
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/test_source_parity.py
# 每个变体应先成功编译，再因行为断言失败而非零退出：
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/run.py --mutate worker_capture
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/run.py --mutate main_resolve
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/run.py --mutate skip_accept
G:/Python310/python.exe -X utf8 -B tools/CourierHistoryPreparationTests/run.py --mutate wrong_input
```

## 覆盖和限制

- 双向 30 场景及 2 个旧同步兼容用例，共 122 断言：正常/空历史、捕获与解析失败、捕获前后 Session/participant/owner/generation/终态变化、同 owner 超时与晚队列回调失效。
- 出站历史 currentInput 仍为原信正文；入站仍为 null，不把 NPC 意图伪造成玩家发言。交付事实方向保持；显式空历史不重新读取。
- `source_parity.py` 将固定 `73774a94` 整个 Courier 文件精确变换为当前文件：两个异步调用、两个 request builder、两个同步公开调用；任何其他差异拒绝。helper/历史快照/runner/harness 和原 owner phase 有审查哈希，不以刷新旧 owner 哈希绕过整文件对照。
- `test_source_parity.py` 4 项证明额外业务差异、删除失效检查和未审查依赖不能混入。
- 唯一超时改写在生成测试源码：30000 ms 缩为 180 ms；生产未变。`.generated/` 下项目/日志不提交。
- 历史算法与文本另由 `NativeHistorySnapshotTests` 852 项及 Native 27 项验证；生产 DLL Courier replay 只验证宿主合同，不运行真实运输或资产修改。
- 未覆盖：人设、preprocess/lore 和剩余消息构造线程安全；完整 Campaign/Mission 生命周期；真实网络取消；历史规模/帧耗时；三渠道版本化提交 SDK。旧同步公开 Capture API 仍须主线程并可能阻塞，不是新 SDK。
