# 压缩输入与来源接受回归

`run.py` 运行真实 production partial、三个 Execute worker、三个 parser、队列准备/接受和既有主线程/generation 边界。游戏、网络、sanitizer 及最终写入副作用使用可控 fixture；提示词构造、解析和来源身份/内容校验使用提取的实际源码。真实游戏、真实 provider 和存档未运行。

```powershell
python tools/MemorySummaryInputBoundaryTests/run.py
python tools/MemorySummaryInputBoundaryTests/runtime.py --framework net472
python tools/MemorySummaryInputBoundaryTests/run.py --original
python tools/MemorySummaryInputBoundaryTests/runtime.py --mutate ignore-content
```

- `DOTNET_EXE` 可指定 SDK 启动器；本机测试使用项目内 `.tmp/goal-20260912/dotnet8.cmd` 选择已安装 SDK 8.0.421，不改变全局 SDK。
- net8 使用可由 `NEWTONSOFT_TEST_DLL` 覆盖的现有 SDK Json.NET；net472 使用 `_deps_auto/Newtonsoft.Json.dll`，引用程序集只读定位自项目已还原的 `obj/project.assets.json`。
- `--original` 精确检查 `9040d184` 的实时输入调用与无来源接受校验，预期 exit 1；这是静态旧源码反例。
- 六个行为变异：`ignore-content`、`ignore-identity`、`reuse-draft`、`skip-retry-guard`、`skip-accept-guard`、`live-name`，预期 exit 1。
- 137 个检查包括三类真实 worker 的成功/三次失败/来源变化、相同 ID/cursor 的内容变化、替换 queue/source/state、AFEF/周报回执、名字/信任/时间快照、后台首次启动、owner/Campaign/generation 晚结果。
- `source-review.json` 记录 12 处已审查声明；`source_parity.py` 定向还原后必须与旧完整 `MyBehavior.cs` 相等，供旧失败提示/模块端口证明继续使用。没有刷新旧边界 hash 掩盖业务变化。

原始日志与生成项目只留本地 `.generated/` 和 `.tmp/goal-20260912/`。新增代码仍需独立实机帧耗时、provider 和旧存档验收。
