# 主线知名度、传唤与浏览器显示切片

阶段：静态验证完成；全英文发行仍 ACTIVE / NOT RELEASE READY。

- `5173ec03e`：玩家百科知名度、11 个评级、历史空日期与文化 fallback，新增 24 对 Social 键。原历史正文、Prompt、存档与知名度计算保持；百科每页缓存旧追加块，刷新时清理旧语言 suffix，无新增 Tick 扫描。
- `3f8227076`：传唤通知与浏览器失败，新增 6 对 UI 键。通知在 request 创建时解析；目标节点、阶段、时序、日志及浏览器参数、URL 校验、注册表/磁盘 fallback 和缓存不改。异常通过 REASON 变量保留。

代码入口：`PlayerNotorietyBehavior.BuildPlayerNotorietyDisplayText`、`BuildPlayerNotorietyHistoryText`、`GetLevelText`；`EncyclopediaHeroPersonaPatch` 的追加块清理；`SceneMovementController.BuildSceneSummonBatchTargetSummary` / `BuildSceneSummonRequest`；`ExternalBrowserLauncher.TryOpen`。

实际验证：`PlayerNotorietyLocalizationContractTests.py`、`SceneAndBrowserNoticeLocalizationContractTests.py`、26 对资源契约与 diff-check PASS。知名度切片既有双 API 本地化 DLL 各 440 次 Social 文本解析；通知切片既有双 API 引用各 12,652 次 UI 文本解析 PASS。这些是资源解析，不能代替当前产品 C# 编译。

最新稳定盘点 HEAD `5173ec03e`，包含通知工作树候选：7 处确认 UI（Policy），1,217 消费者待审，133 候选文件，26 对注册资源，扫描期间无变化。盘点不是全量语义计数，不提供发行完成百分比；世界书身份匹配仍不完整，不据匹配缺项否定用户确认的 B 交付。一处中文原版世界书 JSON 非法控制字符仍未修。

NOT-RUN：当前 C#/fixture 编译执行、双实现/Bootstrap 构建、实机、旧档、正式 CN/EN ZIP。未 Stage、部署或 Push。回滚按上述提交 focused inverse，保护外部 C/E、中文及英文平行资产。

下一切片意图：修 Illustrator 公告配图的两个 raw UI 路径。`RefreshPreparedBulletin` 直接显示 Job.Status；`StartGeneration` 的 bulletin 分支绕过 Direction 显示投影。保留原始状态/诊断/存储元数据，在已有主线程 UI 更新回调投影；预加载 Job 仅保留既有已完成缓存项的引用供显示，不保存新字段，不改队列/等待者/取消/生成机制。固定提示、错误包装和缓存状态进入中英资源，任意未知错误原文保留。旧静态检查按真实显示边界更新，原元数据及生命周期断言保留。
