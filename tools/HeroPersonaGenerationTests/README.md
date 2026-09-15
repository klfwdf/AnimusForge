# Hero 人设生成生命周期回归

验证正常 Hero 自动补全、原公开外部入口和编辑器重生请求的捕获/生成/提交。升格同伴生成及 Native/Courier 状态轮询不在本包冒充完成。

## 运行

```powershell
G:/Python310/python.exe -X utf8 -B tools/HeroPersonaGenerationTests/run.py
G:/Python310/python.exe -X utf8 -B tools/HeroPersonaGenerationTests/run.py --original
G:/Python310/python.exe -X utf8 -B tools/HeroPersonaGenerationTests/test_source_parity.py
# 六个变体必须编译成功，再因行为断言失败而非零退出：
# worker_capture, worker_commit, ignore_edit, stale_lease, release_new, drop_voice
G:/Python310/python.exe -X utf8 -B tools/HeroPersonaGenerationTests/run.py --mutate worker_commit
```

- 当前：118 checks / 0 failures，24 个生成/外部/UI 场景及 12 项独立预约/冷却检查。
- `--original`：106 checks / 34 failures。执行10defeb4的四个真实旧生成/外部/UI声明；已逐声明核对它们与固定main437925b8完全相同。新增预约owner的12项仅用于当前代码，不虚报旧代码也执行了它们。
- 旧失败包括后台读写/配置捕获、owner/目标替换仍写入、清理后旧回包写入、重生覆盖中途编辑、UI错误成功提示。34是失败断言数，不是34个不同Bug。
- 当前编译整个生产 `MyBehavior.PersonaGeneration.cs`、整个 `NpcPersonaGenerationOwner`、原 `MemorySummaryDispatcher` 和真实重生UI方法。独立物理主线程消费队列，强制异步响应；引擎、人物注册表、存储、parser和网络用替身，不能证明实机。
- 原 Prompt 文本完整保留，正常补缺失字段、重生保留最新VoiceId、5分钟失败冷却、显式重试绕过冷却但不绕过busy均验证；中途编辑保护与清理失效是有意修复，不复制main缺陷。
- 精确逆变换恢复整个MyBehavior到10defeb4，6个守卫拒绝额外源变更/漏reset/状态变更/依赖漂移，并核对原Prompt块。B1入口仅先撤销这些精确人设变更，仍保留其完整旧文件对照与15项守卫，不刷新B1生产哈希。
- `.generated/` 为忽略产物。测试沿用本机SDK G:/AFMOD/.dotnet-sdk/dotnet.exe，不把机器路径放入生产。

## 边界

生产复用MyBehavior已有的budgeted主线程dispatcher（保留历史MemorySummary名称），没有第二条队列。事实/设置在主线程捕获并启动已有异步辅助Gateway，不同步等待网络；返回后再次调度主线程写档案/UI。独立owner只持有请求预约与冷却，不存Hero/档案。档案Saveable类型和原读写/导入接口不改变。

本包未证明：大存档事实捕获/FindHeroById主线程成本上界、完整GameEnd/所有队列释放、真实HTTP取消、消费者外围所有live读取、升格同伴技能/人设流程、实际游戏/旧档、三渠道版本化提交SDK。既有状态接口仍是同步game-facing调用，不可因异步生成入口已安全就推断其worker读取也已安全。
