# 终端标签目录显示本地化

阶段：修改意图；基线 main `19102ea9a`。

真实路径：AnimusForgeTagCatalog.BuildSnapshot 缓存 15 秒，提取原 Tag/Category/Description/Sources；ShowTagCatalog 创建列表，BuildTagCatalogEntryTitle/Hint/DetailText 上屏；ExecuteExportTagCatalog 调 TryExportSnapshotToModuleTxt / BuildExportText 导出。

本片涉及上述两个源文件、新 TagCatalogUiText 显示目录与既有 AF_UI 资源对、独立静态消费者检查。固定分类、内置四项说明、普通村庄动作说明包装、程序集来源、导出错误/标题/统计/说明双语化。正文（自然语言）是展示伪标签，可只投影显示；真实方括号协议标签逐字保留。

CategoryOrder、分类/计数、NormalizeTag、提取扫描、原快照、缓存、存档和协议不改。解析仅列表打开/详情/显式导出入口，不加入 Tick 扫描或语言探测。未知描述/来源/分类原文保留，不猜译；中文文件名/模型或用户配置、GCCZ 长规则说明仍需后续英文资产/显示接线。

验证退出门：中英资源集合/占位符/英文汉字、实际 fallback；消费者接线；仅授权显示表达式改动的全文件逆向；原快照与缓存/分类/排序/日志/协议代码保留。可运行现有 DLL 的资源解析；当前 C#、双实现/Bootstrap 编译、实机、旧档与正式 ZIP NOT-RUN。不构建/Stage/部署/Push，不触碰 C/E/世界书工作区改动。回滚 focused inverse。
