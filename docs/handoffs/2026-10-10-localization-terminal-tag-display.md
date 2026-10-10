# 终端标签目录显示本地化

阶段：源码/静态验证完成；产品提交 `aacd12c78`，修改意图 `63591ee19`；全英文发行仍 ACTIVE。

真实路径：AnimusForgeTagCatalog.BuildSnapshot 缓存 15 秒，提取原 Tag/Category/Description/Sources；ShowTagCatalog 创建列表，BuildTagCatalogEntryTitle/Hint/DetailText 上屏；ExecuteExportTagCatalog 调 TryExportSnapshotToModuleTxt / BuildExportText 导出。

本片涉及上述两个源文件、新 TagCatalogUiText 显示目录与既有 AF_UI 资源对、独立静态消费者检查。固定分类、内置四项说明、普通村庄动作说明包装、程序集来源、导出错误/标题/统计/说明双语化。正文（自然语言）是展示伪标签，可只投影显示；真实方括号协议标签逐字保留。

CategoryOrder、分类/计数、NormalizeTag、提取扫描、原快照、缓存、存档和协议不改。解析仅列表打开/详情/显式导出入口，不加入 Tick 扫描或语言探测。未知描述/来源/分类原文保留，不猜译；中文文件名/模型或用户配置、GCCZ 长规则说明仍需后续英文资产/显示接线。

验证退出门：中英资源集合/占位符/英文汉字、实际 fallback；消费者接线；仅授权显示表达式改动的全文件逆向；原快照与缓存/分类/排序/日志/协议代码保留。可运行现有 DLL 的资源解析；当前 C#、双实现/Bootstrap 编译、实机、旧档与正式 ZIP NOT-RUN。不构建/Stage/部署/Push，不触碰 C/E/世界书工作区改动。回滚 focused inverse。

结果：新增28对键、复用3键，专项消费者/原快照与缓存逆向检查、26对资源契约和定向diff-check PASS。现有1.3/1.4本地化引用DLL各12,764次UI资源解析PASS（英文、中文、其他语言回退、切回英文）；这不是当前产品C#编译。只读复审发现自然语言伪标签英化后丢失中文搜索别名，已修复为保留raw Tag/Category并追加完整英文说明和来源，只在列表创建时生成。

源码坐标（`aacd12c78`）：`TagCatalogUiText.cs:30,84`分类显示/双语搜索；`AnimusForgeTerminalUiModels.cs:837-840,890,953-990`列表、详情和诊断面包屑；`AnimusForgeTagCatalog.cs:616`导出。上述源文件均位于`src/AF.GameAdapter.Bannerlord/UI/Terminal/`，导出仍经原`TryExportSnapshotToModuleTxt:66`入口，原文件名与编码不变。

最新工作树盘点（提交前`63591ee19`）：确认UI7、消费者待审1212、134候选文件、26对资源，扫描期间无变化。新显示helper中的原文匹配常量也会被计为候选，不代表新增玩家中文漏口；协议、GCCZ长规则、未知配置说明、路径及运行时中文外置仍未全部完成。C已出现未提交交付文档，正在复审；E无最终回执。回滚仅focused inverse `aacd12c78`，保留其他作者工作。
