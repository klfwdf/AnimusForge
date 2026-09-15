# 纹章落盘后原生崩溃：渲染初始化修正

生产/工具修订 `77caa9f6`，检查点 `e2fda1e`。用户明确要求继续修复崩溃，并指出已经生成的纹章应直接发送参考图。本轮保留 PNG → 通道归一化 → labeled reference 的现有发送链路，调整导出前的渲染初始化。06:21:10 已按此前部署授权用原脚本部署到游戏 1.4.8，未修改游戏 DLL、部署脚本或 AF 主体。

## 新证据

此前两次 Windows 事件指向 `TaleWorlds.Native.dll + 0x283860`，都在纹章导出后、退休日志前崩溃。此次只读分析本机对应 DLL 的 PE runtime-function 表及机器码：

- DLL SHA256 `2A5E0E0B15513EBB7052D747A621B82C50EE834553EBA283B33BBBC33766E12A`；故障 RVA 位于函数 `0x282f60–0x283a37`。
- 函数先保存目标图片，随后引用 `passes/`、`final.dds`、`depth.dds`、`shadow.dds`，额外导出渲染通道。
- 故障点为 `mov rcx, qword ptr [rax + 0x58]`，之前从视图相关对象 `+0xe9e8` 取得指针，随后访问数据用于 `shadow.dds`，这段没有先验证该指针。
- 游戏 `bin/Win64_Shipping_Client/passes` 中 `final.dds` 长度 7779、`depth.dds` 长度 70，修改时间均与第二次崩溃 06:03:59 相符。最终纹章 PNG 先生成，随后原生通道导出仍可发生异常，所以不能用“PNG 已出现”判断函数已安全返回。
- 原版 `BannerTableauContinuousRenderFunction` 每轮设置 `SetRenderWithPostfx(false)`；能够导出的人物 `CharacterTableauContinuousRenderFunction` 使用 `true` 并设置 focused shadowmap。本轮据此补齐纹章专用导出的完整渲染路径。

[只读反汇编证据](assets/illustrator-native-render-init-20260916/native-crash-disassembly.txt)。工具 `tools/illustrator/inspect_native_export_crash.py` 接受 DLL 路径、RVA、可选本地依赖目录；仅读取文件，不附加进程、不调用 native、不 patch 内存。`pefile/capstone` 仅安装在仓库 `artifacts/native-audit-python`，未全局安装。没有转储寄存器或完整调用栈，因此尚不能把具体空指针值和最终修复效果作为已验证事实。

## 已核实源码位置（77caa9f6）

| 仓库相对路径与行号 | 符号、责任 |
|---|---|
| `extensions/AnimusForge.Illustrator/src/Engine/NativeBannerExportWidget.cs:15–39` | 缓存 PaintNeeded 事件元数据，检查初始化与清理所需契约；`ReadyForExport` 至少两个已准备的 paint callback 才为真。 |
| 同文件 `43–64`，`OnUpdate / BindExportPaintHandler` | 仅订阅本次生图纹章纹理的回调，追加在原版回调之后，不改所有旗帜；同组件只注册一次，新纹理重置计数。原版每帧重新关闭 postfx，不能只在发出 save 的 Tick 设置一次。 |
| 同文件 `66–88`，`PrepareExportFrame` | 在原版设置之后，打开完整渲染路径及 focused shadowmap；关闭 DOF/动态模糊/bloom、固定曝光及 postfx 配置，减少对旗面颜色的额外影响。实际颜色仍须与游戏 UI 对照。 |
| 同文件 `91–122`，`OnClearTextureProvider` | 保留前轮延迟场景清理；清理时解除本控件缓存。前轮清理不是本次两次崩溃的已证实原因。 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs:913–918,1186–1196` | 等专用回调准备完成后才允许 save；完整 PNG 读取时增加 bytes 日志，用于判断后续发送链路。 |
| `tools/illustrator/NativeEmblemPipelineAudit.cs` | 新增真实 RenderTargetComponent 事件元数据测试：本地订阅、原版顺序不变、无重复注册、仅订阅不授权导出、无纹理不推进就绪。 |

性能：每次生成且缓存未命中期间才存在专用纹章控件；一次纹理变化一次反射注册，provider owner 和事件元数据缓存，每次原生 paint 做固定次数配置，至多现有导出窗口，无全世界扫描或新网络调用。实际额外 GPU 成本未测；成功纹章继续按完整 BannerCode/尺寸复用 32 项/8MiB 缓存，取消及存档隔离保持。

## 验证与部署

- 双 API 子模块 Release 构建 0 警告/0 错误，通用 180 / 0；两份目标 DLL 的纹章专项各 39 / 0。
- 直接针对最终游戏目录 DLL：通用 178 / 0（跳过两构建），专项 39 / 0。[专项结果及 hash](assets/illustrator-native-render-init-20260916/deployed-results.json)、[通用日志](assets/illustrator-native-render-init-20260916/deployed-regression.txt)。真实旧崩溃 PNG 能解码，但测试不会执行原生 GPU 回调。
- 部署 DLL SHA256 `A910DD3C9F9A8E89EC78E224266E03227D7CE0CDC2962E2A0F0BF62E930AAF90`，与本次 build/deploy 输出一致。脚本备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260916-062110` 保存的是刚恢复的原生替换前版本；原 `20260916-054705` 同样保留。
- 检查部署时未发现游戏进程。下一次启动加载最新 DLL；源码和部署再次对齐。

**尚未实机验收**：完整渲染初始化是否消除原生异常、导出旗面颜色/方向、资源释放、多次生成和换页、`ActualRefImages` 是否包含标准纹章、最终模型是否遵循图案。本轮不把 39 个离线检查描述为无崩溃保证。复测日志应出现 `Export render path initialized` → save → retirement → `Reading rendered PNG for reference` → 含纹章的多图请求；出现图片仍需检查正确性，不能只看请求成功。

回滚本次源码可定向 revert `77caa9f6`，但其父级原生实现已经实机崩溃，不可当稳定部署。需要恢复部署应使用 `20260916-062110` 或 `20260916-054705` 备份，仅恢复生图子模块，不动其他作者、存档或 AF 主体。
