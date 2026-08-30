# InteractionPipeline Contract Tests

独立 net8 runner，链接 `Refactor/Contracts` 中不依赖 Bannerlord 的 contract/runtime seam，不引用生产程序集、网络、存档或游戏目录。

覆盖：

- snapshot 对输入集合的防拷贝污染；
- 资格不满足时不调用 Gateway；
- 成功链路的 Gateway → 可见文本 → ActionPlan；
- 取消结果到 `CancelledAsStale` 的映射；
- `RuntimeConfigSnapshot` reload 隔离。
- 协调器的模块/provider 门控、generation stale 拒绝和外部取消隔离。
- `LegacyInteractionPipelineComposition` 的共享 ports 组合。
- 三阶段主回复/后处理顺序、RAW 保留和后处理失败隔离。
- 主线程提交边界的 action 复核、user/assistant 历史顺序和 AFEF 门控。
- Native Conversation facade 的三阶段旁路和提交前 generation 二次校验。

运行：

```powershell
dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj
```
