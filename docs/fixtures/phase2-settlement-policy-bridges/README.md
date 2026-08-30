# Phase 2 Settlement/Siege + Policy/Diplomacy Bridge fixtures

本目录是候选 Bridge 的纯 JSON 输入/预期结果，不属于生产源码，也不被当前 `.csproj` 编译。

每个领域都覆盖：

- A：提供方单独运行；
- B：使用方/另一领域单独运行；
- A+B：双方存在但没有 Bridge；
- A+B+Bridge：Bridge 正常；
- Bridge failure：Bridge 失败、缺依赖或 generation 过期。

fixture 不包含 Bannerlord live 对象、delegate、MethodInfo、API key、存档实例或原始 Prompt。`TargetIdentity` 只使用稳定 ID/AgentIndex 等元数据；真正的 Agent/Mission/Settlement/Kingdom 操作仍属于各自主线程 runtime。