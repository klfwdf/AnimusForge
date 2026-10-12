# Illustrator Gallery / Weekly status consumers

阶段：静态验证完成。基线 `488957016f7765b7fcf3f4da7f69b4f3d546c21f`，工作区 `F:/AnimusForge-main`，分支 `main`。
未提交、未编译、未 Stage、未部署、未打包、未 Push。

## 变化

复用已交付的 `UI/IllustrationStatusUiText.cs`，新增资源键 **0**。

- `UI/Gallery/IllustratorGalleryPopupVM.cs:398,411`：选择画卷和预览加载完成时，主题/导演状态改用 `Cached` 显示投影。
- `UI/Patches/WeeklyReportPopupIllustrationPatch.cs:442,468,578,609`：公报缓存读取、周报缓存读取、后台完成通知与生成完成状态改用 `Cached`；样式兼容提示仍只在原样式检查路径启用。
- 同文件 `:687`：既有主线程 `IllustratorRuntime.Post` 中，普通周报导演状态改用 `Direction`。共用公报生成管线的 `bulletin` 分支仍发送原始 `direction.StatusText`，不改变 Core `job.Status` 中间数据。

两个目标文件修改前 Git 无 dirty；本切片生产源码仅 **7 行显示表达式替换**。
模型 Prompt、动态标题/主题正文、保存字段、原始错误、状态机、取消/过期判断与元数据保持原样。

## 线程与频率

Gallery 选择入口有 `IllustratorRuntime.AssertMainThread`；`GalleryPreviewLoader` 的 `_post` 注入 `PostCritical`，后台读取完成后再调用 UI 回调。
Weekly 缓存完成通过 `IllustratorRuntime.Start` 的 `PostCritical` 进入主线程，生成完成通过现有 scope 投递回调，导演进度显式位于既有 `Post`。
只在选择/加载/生成事件边界解析文本，无新增 Tick、反射、队列、磁盘扫描或 worker。

## 验证

```powershell
python -B extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/verify_consumers_static.py
python -B extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/verify_static.py
git diff --check -- extensions/AnimusForge.Illustrator/src/UI/Gallery/IllustratorGalleryPopupVM.cs extensions/AnimusForge.Illustrator/src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs
```

均 PASS / exit 0。`diff --check` 仅提示本机 LF/CRLF 转换策略。
新专项按精确逆向替换恢复基线字节文本，确认仅 7 处接线，Core 生产者未变，保存 `directorStatusText` 保留原始值，公报分支原始进度保留。
已有卡片与状态专项仍通过；未知状态/模型正文/动态 provider 错误保留原值。

NOT-RUN：1.3 / 1.4 / Bootstrap 编译、CLR/游戏 UI 运行、实机英文/中文/其他语言切换、完整战役、旧存档、模型请求路径。
静态逆向验证不代表运行验收。

## 已确认残余，未计入本切片完成

- `WeeklyReportPopupIllustrationPatch.cs:423` 直接显示 `BulletinIllustrationPreloader.Job.Status`。
  Core `BulletinIllustrationPreloader.cs:24,75,141,151,161,163,175,179,181,182,191,193,217` 生产固定预加载/取消/失败外壳与缓存状态；原字段连接 Ready/Pending 与通知释放流程，需单独显示投影，不能修改状态本体。
- `Engine/GalleryPreviewLoader.cs:97` 的固定无法解码文本，经 Gallery `:415` 的 `GalleryLoadError(error)` 上屏。
- `UI/Gallery/IllustratorGalleryPopup.cs:101,109,114` 的加载、成功和失败 UI 外壳仍中文。
- `UI/Gallery/GalleryRpItemConverter.cs:66,72,96` 等原始转换结果，经 VM `:465` 的 `StatusText = result` 上屏；需核实资产/记录消费者后做显示投影。
- `UI/Overlays/MissionPhotoOverlay.cs:63,82` 固定取景/中止 UI 尚有中文。

这些是确认的残余路径，不是完整遗漏总数；未知动态错误仍需其生产者专项判断。

## 回滚

无本切片提交。以基线及专项 `edits` 表为 focused inverse，逆向恢复上述 7 处显示表达式；不执行整文件 checkout/reset，不覆盖其他作者后续变化。
本文件与 `verify_consumers_static.py` 属该切片新增证据。
