# Illustrator 公告状态与默认输入显示

阶段：公告状态与默认输入源码/静态完成；全英文发行仍 ACTIVE / NOT RELEASE READY。

`ecd8cca60` 新增 11 对资源键，公告提前生成、作废、取消、无缓存、重绘与保存失败进入中英显示。预加载 Job 仅补内存中的显示状态码、详情和已完成缓存引用，原 Status 字符串逐字不变。完成/取消/通知/等待者/字节释放/生成行为逆向后与 `577f29b7a` 相同。显示在快报既有更新回调解析，未知 code 回退真实 Status，错误变量不解析成模板。公告导演进度走已有 Direction 投影；保存依然写原 StatusText。

PASS：公告/图库消费者/卡片状态与卡片整体静态合同，26 对资源契约，现有 1.3/1.4 本地化引用 DLL 各 1,280 次 Illustrator 文本解析（英文、中文、其他语言回退、切回英文）。无当前产品 C# 编译/实机证据。

默认输入切片 `b510df4bb`：`GalleryRpItemConverter.BuildIntroduction` 只有图库转 RP 输入框及 fixture 消费。默认可编辑介绍的固定包装改为英文 fallback + 中文资源，标题、Theme、ActionSummary 保留用户/模型原内容。输入框副标题走已有 DisplayItemName，而转换后原 ItemName/Identity、模板、背包、描述保存与去重算法不变。新增 5 个默认介绍键、1 个试采说明键。

必须如实保留的缺口：TryCreateGeneratedInventoryItemForExternal 会将 name 重新应用到既有物品，不能只替换 ItemName 就宣称兼容旧物品。转 RP 后的原始物品名仍为中文模板，后续需与 E/经济注册显示边界成对收口；本片不算整条转 RP 全英文完成。原中文/旧存档和用户内容不猜译。

同时将 `IllustrationCardPopup` 试采完成回调中的 probeNote 本地化；该说明只进预览 VM，不发模型、不保存图库。原采集、截图、诊断与空图检查保持。

NOT-RUN：C#/fixture 编译执行、双实现/Bootstrap、实机、旧档、正式 ZIP。回滚 focused inverse；不构建/Stage/部署/Push，不接入语言探测，不改外部 C/E 文件。

## 当前代码证据（b510df4bb）

| 文件与位置 | 已覆盖的显示边界 |
| --- | --- |
| `extensions/AnimusForge.Illustrator/src/Core/BulletinIllustrationPreloader.cs:24-28,80,142-167,183-215,227` | DisplayCode/Detail/Item 仅为内存显示状态，原 Status、生成/取消/等待者不变。|
| `extensions/AnimusForge.Illustrator/src/UI/IllustrationStatusUiText.cs:13-32` | Bulletin 按 code 投影；错误原文作为变量；未知状态回退真实原值。|
| `extensions/AnimusForge.Illustrator/src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs:417-428,687-695` | 两个公告 UI 路径接入投影，原生成代际和 Post 边界、directorStatusText 保存值保持。|
| `extensions/AnimusForge.Illustrator/src/UI/Gallery/GalleryRpItemConverter.cs:27-37,101-105` | 介绍包装与名称显示；Theme/ActionSummary 原值经同一 Clean；不读取原 Prompt。|
| `extensions/AnimusForge.Illustrator/src/UI/Gallery/IllustratorGalleryPopupVM.cs:453-464` | 已有输入框消费投影标题与介绍，转换 callback 及旧注册路径不变。|
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs:763-775` | 试采成功回调解析说明，PublishImage 的 null cache 参数不变。|

两片实际 PASS：`verify_bulletin_static.py`、`verify_gallery_defaults_static.py`、既有 `verify_consumers_static.py` / `verify_remaining_static.py` / `verify_status_static.py` / `verify_static.py`、26 对注册资源契约、diff-check。旧 raw-bulletin 断言已由“元数据/raw producer/lifecycle 保留、UI 用投影”替代；旧完整逆向仍通过。最新既有 1.3/1.4 引用各 1,304 次 Illustrator 文本解析 PASS，不是当前产品编译。

最新稳定工作树盘点 `b510df4bb`：确认 UI 7（全部 Policy）、消费者待审 1,210、133 候选文件、26 对资源，扫描期间无变化。原中文世界书 JSON 控制字符问题仍登记，B 英文身份匹配缺项不能作为未交付判据。候选减少不代表所有内部字符串已外置。

下一主线：终端 `AnimusForgeTagCatalog.BuildExportText` 的固定导出标题、统计、说明与错误；`AnimusForgeTerminalUiModels.BuildTagCatalogEntryTitle/Hint/DetailText` 的分类、说明、来源显示。原 Category 参与排序/计数，Tag 含协议，原分类和标签必须保留，仅在显示/导出边界增加投影。GCCZ 动态规则说明和用户配置未知内容逐条追踪，不将全部 raw Description 当可直接翻译资产。配图本地错误、默认持久化标题、转 RP 真物品名仍未闭环。
