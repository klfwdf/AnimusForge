# Illustrator 公告状态与默认输入显示

阶段：公告状态静态完成；默认输入切片实施中；全英文发行仍 ACTIVE。

`ecd8cca60` 新增 11 对资源键，公告提前生成、作废、取消、无缓存、重绘与保存失败进入中英显示。预加载 Job 仅补内存中的显示状态码、详情和已完成缓存引用，原 Status 字符串逐字不变。完成/取消/通知/等待者/字节释放/生成行为逆向后与 `577f29b7a` 相同。显示在快报既有更新回调解析，未知 code 回退真实 Status，错误变量不解析成模板。公告导演进度走已有 Direction 投影；保存依然写原 StatusText。

PASS：公告/图库消费者/卡片状态与卡片整体静态合同，26 对资源契约，现有 1.3/1.4 本地化引用 DLL 各 1,280 次 Illustrator 文本解析（英文、中文、其他语言回退、切回英文）。无当前产品 C# 编译/实机证据。

下一切片意图：`GalleryRpItemConverter.BuildIntroduction` 只有图库转 RP 输入框及 fixture 消费。将默认可编辑介绍的固定包装译为英文 fallback + 中文资源，标题、Theme、ActionSummary 仍保留用户/模型原内容。输入框副标题改走已有 DisplayItemName，而转换后原 ItemName/Identity、模板、背包、描述保存与去重算法不变。

必须如实保留的缺口：TryCreateGeneratedInventoryItemForExternal 会将 name 重新应用到既有物品，不能只替换 ItemName 就宣称兼容旧物品。转 RP 后的原始物品名仍为中文模板，后续需与 E/经济注册显示边界成对收口；本片不算整条转 RP 全英文完成。原中文/旧存档和用户内容不猜译。

同时将 `IllustrationCardPopup` 试采完成回调中的 probeNote 本地化；该说明只进预览 VM，不发模型、不保存图库。原采集、截图、诊断与空图检查保持。

NOT-RUN：C#/fixture 编译执行、双实现/Bootstrap、实机、旧档、正式 ZIP。回滚 focused inverse；不构建/Stage/部署/Push，不接入语言探测，不改外部 C/E 文件。
