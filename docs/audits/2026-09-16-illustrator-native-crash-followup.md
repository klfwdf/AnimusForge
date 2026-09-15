# 原生纹章导出崩溃与缺失参考图跟进

> **最新状态：第二次实机仍崩溃，已撤回原生版本部署。** 当前游戏使用 05:47 首次部署前备份，源码 `c55334ed` 的原生路径仍未解决。以下修复尝试保留作历史；以末节为准。

当前生产修订 `c55334ed`，包含清理修正 `0127816d` 和日志修正 `8a52d1b`；检查点 `1299d0b`。用户已授权部署，本轮通过既有 `tools/deploy_illustrator.ps1 -BannerlordApi auto` 覆盖独立生图模块，不修改部署脚本、不覆盖 AF 主体或 TaleWorlds DLL、不推送。

## 真实证据与结论边界

- 2026-09-16 05:48，部署 `cf237202` 后在百科生成时崩溃。`rgl_log_24952.txt` 最后记录为 05:48:34.656 请求纹章舞台落盘；[日志节选](assets/illustrator-native-crash-20260916/rgl-tail.txt) 已保存。
- Windows Application 1000/1001 记录 `TaleWorlds.Native.dll`、`0xc0000005`、偏移 `0x283860`。watchdog 显示用户取消了 dump/report 生成，WER 临时 minidump 不存在；没有原生调用栈，不能断言异常恰好发生在某个托管清理调用。
- `af_offscreen_22e4a40739824759beced1c5d4006787.png.png` 已生成 341×341 PNG，能解码，包含鹰与城堡。原始文件保存在 `tools/illustrator/fixtures/native_banner_before_crash.png`；[通道归一化后的图](assets/illustrator-native-crash-20260916/actual-native-normalized.png) 来自这个真实输出，不是模拟图。能够落盘不等于原生资源已可立即释放。
- 原版 `BannerTableau.OnFinalize` 会直接 `_scene.ClearAll()`；人物与 SceneTableau 使用 `View.AddClearTask()`。因此本轮针对立即清空场景的风险修正资源归属，不把 50ms 文件稳定时间当成渲染 fence。崩溃因果仍需实机复测确认。
- 首次修正版 `0127816d` 在用户 05:56 启动的进程 34808 中没有恢复纹章：05:57:15.562 日志为 `Native banner deferred scene cleanup is unavailable on this runtime`，生图只有 `ActualRefImages=1`。按程序集名称查询 provider 在游戏模块加载环境中失败，保护性检查省略了参考图。用户 05:57 截图背景为错误的四分图案旗帜，不能算纹章读取成功，也不能算新清理路径实机通过。

## 当前修改与坐标

以下行号相对源码 `c55334ed`，路径前缀为 `extensions/AnimusForge.Illustrator/src/`。

| 位置 | 责任 |
|---|---|
| `Engine/NativeBannerExportWidget.cs:15–33`，`SupportsDeferredSceneClear` | 读取 Gauntlet 实际注册表中的 `BannerTableauTextureProvider`，验证 `_bannerTableau` 与 `_scene` 元数据，缓存成功解析的 FieldInfo；不按程序集名猜测类型，不构造额外原生实例。 |
| 同文件 `35–63`，`OnClearTextureProvider` | 停止导出/视图，调用 `AddClearTask` 将场景交给引擎清理队列，然后解除 BannerTableau 的场景引用并失效包装；最后执行原版 provider finalizer，避免再次直接清空同一场景。无活跃视图时沿用原版预初始化清理。 |
| `Engine/ScreenCaptureHelper.cs:976–993,1087–1091`，`FinishStage / ExtractViaStageAsync` | 创建纹章舞台前检查清理契约；记录退休开始、ReleaseMovie/RemoveLayer 失败及退休结束。落盘说明去掉“杜绝 DirectX 冲突”的未经验证承诺。 |
| `tools/illustrator/NativeEmblemPipelineAudit.cs` | 增加真实 PNG 解码、注册表缺失拒收、注册实际 provider 类型的离线元数据 fixture、无 provider 的重复清理检查。保持已有像素/缓存/取消检查。 |

原版行为对照来自仓库 `原版游戏本体代码1.4.5` 的 `BannerTableau.OnFinalize`、`CharacterTableau.OnFinalize`、`SceneTableau.OnFinalize` 和 `TextureProviderFactory`。兼容性字段检查不等于原生时序测试；字段缺失时省略纹章导出，其他参考图仍能继续。

性能：只在纹章导出准入/退休时访问元数据；成功后缓存 FieldInfo，没有新增 Tick 全类型扫描、额外网络请求、Sleep 或长期保存原生场景。引擎延迟队列的实际释放时序仍需实机观察。

## 验证、部署与回滚

- 双 API Release 编译 0 警告/0 错误，通用脚本 180 / 0；两份目标 DLL 专项各 36 / 0。测试提供器注册表是离线 fixture，不代表真实 Gauntlet 初始化或真实延迟清理完成。
- 最终部署于 **05:59:48**，游戏 v1.4.8，目录 `F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge_Illustrator`。
- 直接对最终游戏目录 DLL 再验：通用 **178 / 0**（不含两项构建检查）、专项 **36 / 0**。证据：[部署 DLL 结果](assets/illustrator-native-crash-20260916/deployed-results.json)、[通用结果](assets/illustrator-native-crash-20260916/deployed-regression.txt)。部署 DLL 与构建输出 SHA256 均为 `72600405AB899D4B4BE5C8171D23C1D0BEC750A645FC11B0158C401C2249A347`。
- 用户当时进程 34808 在 05:56 启动，仍加载首次修正版；磁盘更新不会替换已加载程序集。必须完全退出游戏再启动测试最终修订。尚未验收最终修订的纹章送入模型、延迟清理无崩溃与背景图案正确性。
- 回滚源码按逆序定向 revert `c55334ed`、`8a52d1b`、`0127816d`；这会回到已报告崩溃的原生版本，不应作为稳定版部署。**恢复此次原生替换之前的部署**使用 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260916-054705`。后续 `055535` 备份是崩溃版本，`055711/055948` 为中间修正版，不能混称稳定回滚点。保留其他作者修改。

## 第二次崩溃与撤回（06:04 之后，最新状态）

用户重启后再次报告崩溃。进程 32720 的日志在 06:03:59.632 记录纹章 `Stage save requested at tick=13` 后结束，没有 `Retiring stage`、`Queueing deferred scene clear` 或 `Provider release completed`。Windows 06:04:01 事件再次指向同一个 `TaleWorlds.Native.dll` 偏移 `0x283860`、异常 `0xc0000005`。兼容性检查已允许创建纹章舞台，但清理修正没有消除崩溃，不能继续把清理认定为已证实根因。

watchdog 仍记录未生成转储。现有证据能定位到原生纹章导出阶段，不能提供原生栈或证明内部失败函数；离线图片/生命周期 fixture 无法模拟该异常。后续深入定位需要有效的原生转储及可调试的复现场景，不再以延长等待、追加反射或通过离线检查作为修复成功证据。

已执行受控部署回退，不修改源码/提交历史：先保存当前失败模块至 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260916-0604-failed-native`，再从 `20260916-054705` 备份恢复 DLL、PDB、SubModule.xml 与五个 prefab。8 项 SHA256 全部与备份一致，恢复后的 DLL SHA256 为 `2DAC0572BD0A79EB6E55DA530D75A37EF3009450012F6B856A64945D7A939896`。没有覆盖游戏本体、AF 主体、其他模块或存档，也未删除用户图片。

证据：[第二次日志](assets/illustrator-native-crash-20260916/second-crash-tail.txt)、[回退记录](assets/illustrator-native-crash-20260916/rollback.json)。这次只验证部署回退的文件一致性；旧版纹章准确性问题仍在，回退后的实际游戏运行也未由代理验收。源码与游戏部署已分离，`tools/deploy_illustrator.ps1` 会编译当前源码，**直接再次运行会重新装回未解决的原生实现**，不能误称修复版。
