# Phase 8 parity replay

加载 project-local Debug 1.4 生产 DLL，而不是复制生产逻辑或模拟整个游戏。复用 `tools/ReplayDependencies` 的六个显式依赖参数；命令形式：

```powershell
dotnet run --project tools/PhaseEightParityReplayTests/PhaseEightParityReplayTests.csproj @properties
```

验证终端全量分页/搜索、精确选择、详情和贡赋返回、空列表与关闭；调用真实 WarStats owner 的归档、重复归档及 v5 平行存档列表读写。战争 fixture 使用空王国标识避免访问真实 Campaign。此项不验证原生和平事件、Gauntlet 渲染/焦点、IDataStore 序列化或实际旧档，不得标记 LIVE/SAVE PASS。依赖参数与来源规则见 `tools/ReplayDependencies/README.md`。输出包含实际 DLL SHA256。

标签回归使用 73 条 fixture，验证分页、卡片截断后参数搜索、15 个来源的完整详情、4 个根目录说明、空索引刷新与返回。导出仅验证生产文本格式器和空快照拒绝路径，不调用非空快照的文件写入或真实模块扫描。

周报回归使用世界/王国/空记录国家、无序日期和超过 12,000 字的正文，验证国家选择、周次/日期排序、三种稳定度标签、全文按钮、空状态与保留查询的返回；受控 Task 验证主线程 Tick 完成处理、失败可重试和关闭失效。直接解析正在提交的终端 XML 验证正文/日期/全文动作绑定，不将 XML 可解析等同于 Gauntlet 已渲染。`WeeklyReportOwnerReplay` 验证生产 owner 主线程完成队列、世代保护和去重；不访问 API、游戏进程或实际存档。
