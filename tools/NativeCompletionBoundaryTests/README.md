# Native 收尾线程 / 上下文边界回归

在 `G:\AFMOD\AF-REFACTOR` 执行：

```powershell
$env:PYTHONIOENCODING = 'utf-8'
python -B tools/NativeCompletionBoundaryTests/run.py --original
python -B tools/NativeCompletionBoundaryTests/run.py
python -B tools/NativeCompletionBoundaryTests/run_mutations.py
```

本地 SDK：`G:\AFMOD\.dotnet-sdk`；复用项目 `.tmp/dotnet-cli` 和 `.tmp/nuget-packages`，不安装 SDK、不生成开发证书。生成文件与日志在本工具 `.generated`，不提交。

## 执行真实源码的部分

- 从完整 Native 请求体提取实际动作调用到返回的全部 tail，不复制一份“等价成功逻辑”。
- 提取真实 dispatch / result / admission 声明及三层 context 检查；链接实际 NativeActionDispatch 与 NativeCompletion partial。
- 主线程由实际 ManagedThreadId 标识，动作用真实 ConcurrentQueue 消费。校验每次游戏/历史操作的物理线程。
- 旧基线 `d9288faa` 的真实 tail 已复现后台历史操作、校验后改存档/scene session 仍写入新 session 的缺陷。

游戏对象、Game availability provider、业务 Action core、MyBehavior 历史 owner、TTS 与实际场景记录器是明确 fixture。日记是否接受、AFEF 格式/规则、磁盘/SyncData、真 Agent、实际游戏事件不由此测试证明。对源码方法返回正常，不推导持久化成功。

## 覆盖

102 检查：Hero/非 Hero、普通/主动开场的精确 payload，场景/loose 路由、非持久 NPC、无发言占位、提前 TTS 去重；owner 合法结束/换场景/换目标后的原身份历史；读档隔离、重复 callback、snapshot/历史异常，动作 discard 的原上下文主线程清理，以及后端释放后仍合法但新 revision/目标/token/manager/Mission/generation 后失效的延迟关窗。

9 个行为变异：丢失 generation、重新读取当前 scene、动作后重解析非 Hero、丢失展示上下文、删关窗消费 guard、错误依赖后台 slot、跳过真实收尾、snapshot 错误标记为 owner 已开始、丢失 discard 清理上下文。必须运行时拒绝；编译失败不算通过。

## 与旧套件的接线

`NoCompletionStubs.cs.txt` 仅供旧准入/展示/动作派发套件使用：那些 fixture 明确不传 completion payload，若实际进入新收尾就直接失败。它不是生产路径、备用实现或历史提交替身。完整 payload 与实际 tail 由本套件执行。

动作派发套件继续执行真实 discard gate；其普通 Content 与历史计数为局部 fixture，不再声称覆盖完整最终文本返回。Null fallback 变异仍须触发预期“应有类型化错误”的运行时失败，而不是依赖 fixture 意外 NRE。
