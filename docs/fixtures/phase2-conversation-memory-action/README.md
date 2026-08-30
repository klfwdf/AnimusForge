# Phase 2 Conversation / Memory / Action fixtures

本目录是阶段 2 的纯数据 fixture 目录，不属于生产源码，也不被当前 `AnimusForge.csproj` 编译。

## 文件

- `valid-courier-context.yaml`：信使渠道的有效 `ConversationContextSnapshot`。
- `valid-action-result.yaml`：主线程确认后的 `ActionExecutionResult`。
- `invalid-contract-cases.yaml`：live object、角色、AFEF、generation、目标身份、postprocess closure 等无效样例。
- `expected-results.yaml`：每个 fixture 的有界预期状态和 issue code。

## 规则

- 不包含 `Hero`、`Agent`、`Mission`、`Game`、`IDataStore`、delegate、`MethodInfo`、API key 或原始网络响应。
- 不读取存档、不调用网络、不依赖 Bannerlord 程序集。
- 只验证纯 contract 语义；当前没有 runner，所有结果均为预期输出记录。
- `ApplicationTick` 和 `EngineTick` 的分离、三渠道 role/AFEF 语义、stale generation 和主线程 apply 是必测边界。