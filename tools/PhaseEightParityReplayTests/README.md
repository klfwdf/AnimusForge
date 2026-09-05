# Phase 8 parity replay

加载 project-local Debug 1.4 生产 DLL，而不是复制生产逻辑或模拟整个游戏。复用 `tools/ReplayDependencies` 的六个显式依赖参数；命令形式：

```powershell
dotnet run --project tools/PhaseEightParityReplayTests/PhaseEightParityReplayTests.csproj @properties
```

验证终端全量分页/搜索、精确选择、详情和贡赋返回、空列表与关闭；调用真实 WarStats owner 的归档、重复归档及 v5 平行存档列表读写。战争 fixture 使用空王国标识避免访问真实 Campaign。此项不验证原生和平事件、Gauntlet 渲染/焦点、IDataStore 序列化或实际旧档，不得标记 LIVE/SAVE PASS。依赖参数与来源规则见 `tools/ReplayDependencies/README.md`。输出包含实际 DLL SHA256。
