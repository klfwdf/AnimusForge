# Native 请求准入：行为回归

## 验证范围

本工具执行 `G:\AFMOD\AF-REFACTOR\ShoutBehavior.NativeAdmission.cs` 的真实源码，并从当前 `ShoutBehavior.cs` 抽取两个实际公开入口、完整 Native 方法的初始捕获段、真实动作调度队列。UI 部分抽取两条实际提交方法 finally 中的流式/busy 所有权片段。游戏实体与后续 LLM/业务执行使用明确标注的 stub，**不冒充整条实际游戏对话测试**。

```powershell
Set-Location -LiteralPath 'G:\AFMOD\AF-REFACTOR'
$env:PYTHONIOENCODING = 'utf-8'
python -B tools/NativeConversationAdmissionTests/run_original.py
python -B tools/NativeConversationAdmissionTests/run.py
python -B tools/NativeConversationAdmissionTests/run_mutations.py
```

使用本机 `G:\AFMOD\.dotnet-sdk\dotnet.exe`、现有 `.tmp/dotnet-cli` 与 `.tmp/nuget-packages`，禁用开发证书生成。无网络包依赖；产物/日志在 `.generated/`，不提交 DLL 或日志。生产主线程准入排队上限 30000ms，隔离测试缩短为 40ms；不改生产超时。

## 原代码反例

`14dec2d7` 的原公开入口和内部捕获前缀在真实 `Task.Run` 下执行：同时两次调用均进入后端；在后台解析目标之前切换对象，两次都捕获替换后的 `B:99` 而非调用时的 `A:0`。控制两个缺陷确实存在。抽取前缀未包含后续 await，因此原反例有一个 fixture-only CS1998 警告；不计入生产构建结果。

## 当前检查

44 项：主线程先捕获再 Task.Run、正常输入/主动开场共用 busy、busy 不能关闭 Overlay/消费开场、稳定对象、换目标/character/agentIndex/Mission/ConversationManager/ActiveToken、读档、真实结束后同 token 再开会话、旧完成不能清新票据、异常释放、空输入与战后拒绝、后台调用转主线程、排队超时跳过、已开始捕获不能丢弃返回值、动作实际执行前再检查、旧/关闭界面 finally 不能结束新展示或清 busy。

原有完整 Native 业务体没有被移入测试或替换。源码接线检查另验证：CanSubmit 函数与旧基线相同；ConversationEnded 处理第一步失效旧票据；完整 Native 体不再重新解析目标/消费开场；六个既有动作前边界加会话检查；两个 Overlay 拒绝分支保留输入/待开场。

七个反例变异必须在运行时失败，不接受编译错误冒充捕获：

- 移除 busy。
- 旧 finally 无条件清新票据。
- 去掉排队开始/超时 CAS。
- 跳过动作队列执行时的作用域检查。
- 跳过 generation。
- 重新装入 `14dec2d7` 的旧 UI finally 清理片段。
- 跳过排队请求的 conversation epoch（关闭重开后旧输入不得进入新会话）。

## 尚未证明

真实 Bannerlord 主线程/ConversationEnded 时序、旧存档、后续人设/Prompt/历史/AFEF 的完整线程和原子提交边界、流式回调的完整 UI 生命周期、物理网络取消及公共请求回执。新公共 V1 提交仍为 NotSupported。后端票据结束不代表 TTS 播放结束，也不撤销已经执行的业务副作用。
