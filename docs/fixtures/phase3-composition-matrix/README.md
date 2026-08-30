# Phase 3 Composition Matrix Fixtures

本目录保存 Foundation/Module/Bridge 的设计期组合 fixture，不属于生产源码，不接入 `AnimusForge.csproj`。

覆盖 18 个案例：

- Foundation + NoOp；
- Foundation + GameAdapter + NoOp；
- 独立 Conversation/Policy；
- A+B 无 Bridge；
- A+B+Bridge；
- required dependency/provider 缺失；
- required failure cascade；
- optional provider 缺失；
- contract version 不兼容；
- SafeMode；
- stale completion；
- partial start failure；
- Bridge runtime failure/disabled；
- runtime-toggle 与 Harmony 冲突；
- health output 有界/溢出。

所有案例只包含字符串、数字、布尔值和稳定 ID，不包含 live Bannerlord 对象、delegate、存档实例、API key、Prompt 或原始日志。