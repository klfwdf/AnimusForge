# Illustrator 全模块修复交付 / HANDOFF

日期：2026-09-17
工作树：`F:\AnimusForge-main`
分支：`codex/af-main-refactor-continuation-20260831`
修复前意图检查点：`501ff5dd`。下面源文件 SHA-256 是最终验证版本；对应提交可通过 `git log -- extensions/AnimusForge.Illustrator` 定位。

## 结论与范围

前一轮审查的 **15 项发现均已落实代码修复**。双 API 编译与离线回归通过，**不等于已完成游戏内视觉验收**。

仅改 Illustrator 子模块的源码、测试和说明。未修改主模组代码、构建/部署入口或 `tools/test_illustrator.ps1`；未推送、未部署游戏、未调用外部付费 API，也未操作真实用户图片缓存。本次提交同时收纳本会话先前已经验证的全装备/纹章规则改动。

玩家与其他角色同规则：百科取当前百科服装；在场会话取实际 Agent 装备；玩家、英雄、普通 NPC 均读取全部 12 槽。完整快照不要求将隐藏装备、所有盾牌或坐骑强行放进最终构图；不得靠堆旗帜替代人物已有纹章载体。

## 逐项交付与证据

路径相对 `extensions/AnimusForge.Illustrator/`；行号定位关键符号，完整文件版本见下方清单。

| ID | 问题 | 修复 | 关键入口 | 验证边界 |
|---|---|---|---|---|
| F01 | 缓存越界读写/删除 | 规范路径、分类白名单、重解析点检查；元数据只认相邻的自有 PNG；删除失败回滚。 | `src/Engine/DiskImageCacheManager.cs:232` · `IsSafePath` | 运行：外部路径拒绝、旧恶意元数据拒绝、外部文件逐字节保留。 |
| F02 | CosmeticItem 丢失 | 先将 12 槽投影为实际可见物品再生成渲染码，保留 cosmetic-only 槽；不修改源装备。 | `src/Context/CharacterAppearanceSnapshot.cs:20` · `VisibleEquipment` | 运行：真实 Equipment 的外观头盔与全部其他槽；原 53 项全装备检查。 |
| F03 | 实际衣甲染色丢失 | 冻结衣甲双染色、身体、性别、种族、纹章；现场读取 Agent，百科读取当前 tableau；异步渲染不再重建覆盖。 | `src/Context/CharacterAppearanceSnapshot.cs:13` · `Color1` | 运行：冻结值不可变；源码核对 ApplyAppearance；实际色彩/mesh 待实机。 |
| F04 | 俘虏模板凭空换装 | 删除身份推导的没收武器、换简朴衣装、镣铐与未观察到的绷带；身份不覆盖现场。 | `src/Context/HeroVisualExtractor.cs:144` · `ExtractCurrentState` | 源码断言 + 规则复核；实际俘虏场景待验收。 |
| F05 | 周报否定/计划被变成既成事实 | 不从关键词主题创建冲突结果与地点硬事实，保留否定/计划；不将人物当下状态注入历史事件。 | `src/Context/WeeklyReportContextExtractor.cs:150` · `ApplyEventSceneAnchoring` | 运行：攻城未成功、拒绝处决、计划海战未出航的实际分类→锚定→硬事实链路。 |
| F06 | 海岸误判海上 | 仅明确海上航行、海船甲板、海上接舷才认作海上场景。 | `src/Context/ConversationContextExtractor.cs:226` · `IsExplicitSeaScene` | 运行：海岸保持陆地、明确甲板识别。 |
| F07 | 凭年龄/职业/索引编造面容 | 移除不可靠表型推测；未知明确保持未知，优先真实 BodyProperties/原生立绘。 | `src/Context/HeroVisualExtractor.cs:155` · `ExtractPhysicalFeatures` | 运行：未知面容不出现伪造的银发等特征；源码复核。 |
| F08 | edits 上传格式/归属/画质缺失 | 上传归一为真实 PNG；按序号标注参考图归属；携带 quality/style。 | `src/Core/UniversalOpenAiImageClient.cs:156` · `AttemptImagesEditsAsync` | 运行：本机 TCP 模拟服务实际收到 multipart，核对双归属、PNG 字节和 quality。 |
| F09 | 非法/超大图像与响应 | 只接受可解码 PNG/JPEG，预检尺寸；24 MiB 图像、36 MiB 响应、8192 单边/16,777,216 像素限制；流式有界读取及完整请求期限；缓存/GPU 输入校验。 | `src/Engine/ImagePayload.cs:15` · `Normalize` | 运行：任意字节、截断 PNG、巨大尺寸、过大响应拒绝；JPEG→PNG；坏候选不遮挡后续有效图。 |
| F10 | 准备异常导致忙状态/异常外泄 | 百科、会话生成全准备阶段捕获并恢复可见失败状态；重绘 UI 命令和周报准备同样兜底。 | `src/UI/Overlays/IllustrationCardPopup.cs:74` · `ExecuteEncyclopediaGeneration` | 运行：实际百科空对象异常、重绘准备异常均显示错误并恢复空闲。 |
| F11 | 画廊/UI 部分构造泄漏 | 部分构造、挂载失败回滚 scope/图层；画廊 I/O 失败清空无效选择并显示错误；卡片与周报也补齐回滚。 | `src/UI/Gallery/IllustratorGalleryPopup.cs:23` · `Close` | 源码所有权/回滚接线检查；Gauntlet 构造/原生句柄释放待实机。 |
| F12 | 并发参考图遗留兄弟任务 | 改为惰性串行逐项 await 人物和纹章引用；失败取消请求令牌再释放，避免未观察到的并发兄弟任务。 | `src/UI/Overlays/IllustrationCardPopup.cs:381` · `Func<Task<string>>` | 源码顺序 await 接线检查；原生舞台取消待实机。 |
| F13 | 满队列丢弃任务完成 | 正常队列容量 32；另保留完成/清理通道；最多 4 个已接纳 worker；每 Tick 最多 2 项，完成 finally 才释放名额。 | `src/Core/IllustratorRuntime.cs:69` · `PostCritical` | 运行：塞满正常队列，任务仍完成恰好一次并释放名额。 |
| F14 | 默认图并发与取消覆盖 | 缓存事务加锁，元数据与默认指针原子写；worker 仅写历史，不隐式默认；有效 scope 的完成回调才提升默认。 | `src/Engine/DiskImageCacheManager.cs:155` · `SetDefault` | 运行：30 次并发默认切换只留一个默认；后到的未提升历史不覆盖当前默认；坏图不能成为默认。 |
| F15 | 重绘截入旧插画 | 初次和重绘均在请求 scope 内串行隐藏 Illustrator 根图层→等待 2 个 Tick→截图→保留队列恢复；恢复逐层容错。 | `src/Engine/ScreenCaptureHelper.cs:1369` · `CaptureConversationSceneWithoutUiAsync` | 运行：两帧围栏与取消；源码隐藏/恢复/串行接线检查。截图像素与第三方覆盖待实机。 |

## 最终验证

| 检查 | BannerlordApi=1.3 | BannerlordApi=1.4 |
|---|---:|---:|
| Release 构建 | 0 警告、0 错误 | 0 警告、0 错误 |
| 新增审查回归 `tests/test-full-audit.ps1` | 55/55 | 55/55 |
| 全装备回归 `tests/test-fidelity.ps1` | 53/53 | 53/53 |
| 原有 `tools/test_illustrator.ps1 -SkipBuild` | 178/178 | 178/178 |

合计 **572 次检查通过**（每个 API 构建各 286 项；不是 572 个不同场景）。新增测试混合实际方法调用、原生 Equipment 数据对象、磁盘/并发故障夹具、本机 HTTP 模拟和源码接线断言，不能统称全部为端到端测试。

双 API 编译目标不同；离线执行的原生依赖来自本机安装的游戏程序集，**不是分别启动两个版本游戏进行测试**。

最终执行命令记录：`cmd_bf974c5652da186027a22fa85f91e344761585db46e3665a`，退出 0；`git diff --check` 通过。测试过程中出现过脚本双 BOM、缺失测试程序集加载、日志引用命名歧义，均修正后重新执行以上最终矩阵；早期日志不作为最终依据。

原有回归脚本 SHA-256（与修复前一致）：
`8db123c6e0f7d0eb8f4eec5c82ca870dc02266b8325b134544c7fee2aac83ca6`

### 产物与日志

- 构建：`bin/full-audit/{1.3,1.4}/AnimusForge.Illustrator.dll`（隔离输出，非游戏部署目录）。
- 构建日志：`obj/fidelity/full-fix-build-{1.3,1.4}.log`。
- 审查回归：`obj/fidelity/final-full-audit-{1.3,1.4}.log`。
- 全装备回归：`obj/fidelity/final-fidelity-{1.3,1.4}.log`。
- 原有回归：`obj/fidelity/final-regression-{1.3,1.4}.log`。
- 故障夹具：`obj/fidelity/full-audit-test-*`，缓存根只在测试进程内重定向，外部文件也是夹具，不是真实用户图片。日志、DLL 和夹具属于忽略的生成产物，不进入源码提交。

### 复跑（只编译测试，不部署）

```powershell
foreach ($api in @('1.3','1.4')) {
  dotnet build extensions/AnimusForge.Illustrator/src/AnimusForge.Illustrator.csproj -c Release -p:BannerlordApi=$api -p:OutputPath=../bin/full-audit/$api/
  $dll = "extensions/AnimusForge.Illustrator/bin/full-audit/$api/AnimusForge.Illustrator.dll"
  powershell -NoProfile -ExecutionPolicy Bypass -File extensions/AnimusForge.Illustrator/tests/test-full-audit.ps1 -AssemblyPath $dll
  powershell -NoProfile -ExecutionPolicy Bypass -File extensions/AnimusForge.Illustrator/tests/test-fidelity.ps1 -AssemblyPath $dll
  powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_illustrator.ps1 -SkipBuild -AssemblyPath $dll
}
```

## 性能与保留语义

- 人物外观只在请求准备阶段快照；不新增每帧全人物/全装备扫描。参考图串行，避免同时保留一组未被等待的任务。
- 图像解码与网络流在 worker 中处理；入库的首次有效性校验在缓存锁外。缓存写入/指针更新加锁确保一致性，但画廊操作仍可能等待磁盘事务，未测量实机帧耗时。
- 主线程正常任务容量 32、接纳 worker 上限 4、每 Tick 消费最多 2 项；完成与清理使用保留队列，避免饱和时永久忙状态。
- HTTP 完整响应期限 120 秒；图像与响应大小有界。该限额不是对整台进程/GDI 的总体内存预算。
- 活动画廊按最早图片清理；回收区不自动永久删除，不计入保留张数上限。
- 取消发生在保存之后时，允许保留未提升为默认的历史图片，不承诺删除所有取消结果。

## 尚未覆盖的验收责任

1. 两个游戏版本内：玩家/对方英雄/普通 NPC，百科、城镇平民服、野外战装分别检查 12 槽来源。
2. 实际 MOD 外观头盔和附加 mesh、盾牌与武器挂载、家族与 Agent 衣甲染色；对照原生立绘和最终生成图，不仅看提示词。
3. 初次与连续重绘，画廊/卡片同时开启及取消/切屏期间，确认 Illustrator 层不混入现场图且恢复正常。
4. Gauntlet 原生层构造失败、舞台取消、退出/重进存档、应用关闭时的 native 资源释放；当前恢复/完成消费依赖主线程 Tick，不能以离线帧围栏测试声称已证明停 Tick 后的卸载行为。
5. 第三方/平台覆盖层未被本模块主动隐藏；服务商的参数兼容、颜色遵循、旗帜数量和最终图像质量仍需实际请求验收。

以上为需要显式授权部署或实机执行的验收项，不是已通过的结果。

## 最终源码版本清单

下表为本轮改变的源码/测试文件及读取版本（SHA-256）；范围覆盖文件全文，关键符号定位见上表。

| 路径 | 行范围 | SHA-256 |
|---|---:|---|
| `src/Context/CharacterAppearanceSnapshot.cs` | 1–52 | `3b67932d0d3a0d5739b3df02ee8bd261562962ce9a0f4b0bdeed065f52a7e63c` |
| `src/Context/ConversationContextExtractor.cs` | 1–579 | `98316e2248f4ee58d95ee83e0002a91902f9042a26b3750e0de40ce48d733d9a` |
| `src/Context/ConversationEquipmentSnapshot.cs` | 1–84 | `c29ced9c7a8418e5d99ecaccd027f391e00b99edb610c34e28476ba3042898ec` |
| `src/Context/EnvironmentVisualExtractor.cs` | 1–773 | `52a3098517f84ecb590522e47c88fe1a4e97f996eead148883683523099fa353` |
| `src/Context/HeroVisualExtractor.cs` | 1–881 | `811979e23197d62a13d7008027fb6403215e8e9433f8899d3a44fe87edc8c4a3` |
| `src/Context/WeeklyReportContextExtractor.cs` | 1–477 | `3d3c7c0b0549e4f5f7c0f854664476a9d488dce705a8308ad12573e1c82decb9` |
| `src/Core/IllustratorRuntime.cs` | 1–318 | `678cec32ee14fc7b0ae1594b61452913d554faf2d688d432ce2916d6fc47e846` |
| `src/Core/UniversalOpenAiImageClient.cs` | 1–883 | `8e77f3c66da43ded7cbe6a81f8230dcae1456e75aa17bb5a167a28ecd4f09b26` |
| `src/Core/VisualDirectorEngine.cs` | 1–465 | `3207f319756f8417f1717ccf57f5ba69b26e82c14ee4e3eb2c1723eb74fcf9e9` |
| `src/Engine/DiskImageCacheManager.cs` | 1–386 | `3b53dd3110d93a4edb269835d699743b50657189bb69c7eae240b835cf153a04` |
| `src/Engine/GauntletTextureLoader.cs` | 1–227 | `c735673a491b49d7ade1a89c63ef3a9bbd0aeee63ca5813f93345adc975ba842` |
| `src/Engine/ImagePayload.cs` | 1–111 | `77fda8d8231670421f5f23d49774fb42fb824b0743304ebdbdd670cf7d6cb308` |
| `src/Engine/ScreenCaptureHelper.cs` | 1–1503 | `6e8a9c834bbf4b06a39f9b1f4ebce1786b0ec609056166c1827334f13561b5f9` |
| `src/Settings/IllustratorSettings.cs` | 1–621 | `641be59c07eb1bbe4a7b2192a1887897da97cbb6efeefb51c9b785cfb2383c3d` |
| `src/UI/Gallery/IllustratorGalleryPopup.cs` | 1–102 | `a2471ba8b259c6cc95b5a2fc79b80a71540681fc448b4ad93b239b514c3a06e8` |
| `src/UI/Gallery/IllustratorGalleryPopupVM.cs` | 1–341 | `ab9728d1c991aa0407fe0b4c73c250e8e421fcbb2911af3cf3552743c81e1181` |
| `src/UI/Overlays/IllustrationCardPopup.cs` | 1–510 | `75b6bd35c20912ee9bc7079b28eea45e24c202fc6f6d925817ca1d2a38dc99f0` |
| `src/UI/Overlays/IllustrationCardVM.cs` | 1–188 | `e92896c7e3f9a0d854b4c577ba4dee15516ba110ba606d010319191c56bfdcf5` |
| `src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs` | 1–470 | `17e492193d1846e040732e048951640447b4b05e4392ee6f8fd419ba45b709c4` |
| `tests/test-fidelity.ps1` | 1–106 | `2e99bb86272cd62f21a2eb6aff0ba996777375d6445863f4425666254ab47259` |
| `tests/test-full-audit.ps1` | 1–174 | `7b0ea974f2b51b006ee172d3404f4b45ffc3bc9b97ed1496594782f04733aafc` |
