# Native 收尾线程 / 上下文边界回归

在 `G:\AFMOD\AF-REFACTOR` 执行：

```powershell
$env:PYTHONIOENCODING = 'utf-8'
python -B tools/NativeCompletionBoundaryTests/run.py --original
python -B tools/NativeCompletionBoundaryTests/run.py --memory-baseline
python -B tools/NativeCompletionBoundaryTests/run.py
python -B tools/NativeCompletionBoundaryTests/run_mutations.py
```

本地 SDK：`G:\AFMOD\.dotnet-sdk`；复用项目 `.tmp/dotnet-cli` 和 `.tmp/nuget-packages`，不安装 SDK、不生成开发证书。生成文件与日志在本工具 `.generated`，不提交。

## 执行真实源码的部分

- 从完整 Native 请求体提取实际动作调用到返回的全部 tail，不复制一份“等价成功逻辑”。
- 提取真实 dispatch / result / admission 声明及三层 context 检查；链接实际 NativeActionDispatch 与 NativeCompletion partial。
- 复用 MyBehavior 真实 void/strict 外壳、ID 规范化与 Hero 资格检查，链接实际 scene-aware strict owner；只有更底层 Append 接受/部分写入/异常使用 fixture。
- 主线程由实际 ManagedThreadId 标识，动作用真实 ConcurrentQueue 消费。校验每次游戏/历史操作的物理线程。
- 旧基线 `d9288faa` 的真实 tail 已复现后台历史操作、校验后改存档/scene session 仍写入新 session 的缺陷。

游戏对象、Game availability provider、业务 Action core、底层日记/最近历史 Append、TTS 与实际场景记录器是明确 fixture。验证了接受/拒绝结果如何传播，但真实日记与 AFEF 格式/规则、磁盘/SyncData、真 Agent、实际游戏事件仍不由此证明。Applied 不推导磁盘持久化成功。

## 覆盖

184 检查（保留原 102 项并强化 owner 接线）：Hero/非 Hero、普通/主动开场的精确 payload，场景/loose 路由、非持久 NPC、无发言占位、提前 TTS 去重；owner 合法结束/换场景/换目标后的原身份历史；读档隔离、重复 callback、snapshot/历史异常，动作 discard 的原上下文主线程清理，以及后端释放后仍合法但新 revision/目标/token/manager/Mission/generation 后失效的延迟关窗。

15 个行为变异：丢失 generation、重新读取当前 scene、动作后重解析非 Hero、丢失展示上下文、删关窗消费 guard、错误依赖后台 slot、跳过真实收尾、snapshot 错误标记为 owner 已开始、丢失 discard 清理上下文，以及忽略记忆接受结果、owner 丢失 scene 参数、false 被当 Applied、缺少严格主线程检查、错误公开内部 owner、记忆失败吞掉必要关窗。必须运行时拒绝；编译失败不算通过。

## 与旧套件的接线

`NoCompletionStubs.cs.txt` 仅供旧准入/展示/动作派发套件使用：那些 fixture 明确不传 completion payload，若实际进入新收尾就直接失败。它不是生产路径、备用实现或历史提交替身。完整 payload 与实际 tail 由本套件执行。

动作派发套件继续执行真实 discard gate；其普通 Content 与历史计数为局部 fixture，不再声称覆盖完整最终文本返回。Null fallback 变异仍须触发预期“应有类型化错误”的运行时失败，而不是依赖 fixture 意外 NRE。

## 记忆接受续作

`--memory-baseline` 从 `29ca75c9` 提取真实 Native 全 tail 和真实 void owner 外壳：owner 缺失、返回 false、抛异常都曾继续正常收尾。新版执行同一底层接受结果门禁，失败不重放/不删除部分记录，并保留必要的原上下文关窗。

新场景严格入口反向去除方法名/scene 参数差异后，完整声明必须等于原六参 strict owner；原 public 六参 facade 仍走 -1，scene-aware owner 仍 internal。原资格、规范化、错误码与 payload 顺序不改。非持久 NPC/真正空 payload 不伪造写入请求。实际 DLL ABI/可见性由 ModuleFrameworkApiTests 的 PE 元数据检查补充。
