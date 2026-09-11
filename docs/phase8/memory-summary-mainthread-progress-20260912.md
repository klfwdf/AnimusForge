# 压缩记忆主线程提交切片进度（2026-09-12）

## 状态

`OFFLINE_COMPLETE / LIVE_SAVE_PENDING`。来源 `e40c92d7`，意图 checkpoint `909550d4`，生产/测试提交 `9040d184`。只完成“压缩记忆 post-await 结果由当前 Campaign owner 在主线程接受”这一片，不开始下一片。

## 完成内容

- 新增 owner/generation/Campaign-bound 的低频完成队列；主线程直达，后台入队前后双检，reset 退休未开始工作。
- `OnEngineTick` 每次最多消费 2 个；三类 Apply/Mark、队列清理、提示和 `_memorySummaryProcessing` 释放均经过该边界。
- 保留原请求、重试、解析、存档字段、三渠道语义、公开只读 API 和默认入口。
- 更新 Native history 与团队端口严格 source inverse 审核哈希；仅登记本切片 4 个已审方法，不放宽 whole-file 证明。
- 为当前 Windows host 的两个既有离线 runner 移除失效的固定 G 盘 dotnet 路径，仍使用无网络 NuGet 配置；没有改生产构建脚本。

## 验证层级

- 专用 fixture/replay：17/17 PASS；`e40c92d7` 精确旧实现 FAIL；忽略 generation、忽略 owner、无界 drain 三个 runtime mutation 均 FAIL。
- 相关离线回归：HistorySnapshot 852/852、NativeHistorySnapshot 27/27、MemoryFailureUi 85/85、MemoryCommitRecovery PASS、WeeklyMemoryMaterialOutcome PASS、TeamModulePorts 308 项 PASS，3 个 port mutation FAIL。
- 身份：相对 `e40c92d7`，SyncData 146/146、CampaignBehavior 36/36；模块 `Id/Name=AnimusForge`，仅 Bootstrap 加载不变。
- 构建/Stage：Debug 与 Release 的 Bannerlord 1.3、1.4、Bootstrap 全部 0 error；两套项目内单模块 Stage PASS，未改游戏目录。每个构建仅有 2 个 `NU1900`，原因为沙箱无法访问 nuget.org 漏洞元数据。
- 实际 DLL：公开 API/内部可见性 119 项、256 次并发读取、四份实现 DLL 532 个 PE 元数据断言 PASS。
- LIVE、真实 provider、真实 SAVE/旧档：`NOT_RUN`。

详细证据见 `docs/audits/2026-09-12-memory-summary-mainthread-verification.md` / `.json`；边界设计见 `docs/architecture/af-memory-summary-mainthread-boundary.md`。

## 未覆盖与接续

三类 Execute job 的 live 输入准备与逐任务来源 fingerprint 尚未处理；不能写成“后台只剩网络”或“所有 memory writer 已线程安全”。下一切片只捕获三类压缩任务的只读输入与 source revision，并复用本轮接受边界。Courier、公共写 API、制作组业务和默认切换继续不在本切片范围。

回滚按用户指示定向 inverse/revert `9040d184`；不 reset/rebase，不处理其他 worktree。
