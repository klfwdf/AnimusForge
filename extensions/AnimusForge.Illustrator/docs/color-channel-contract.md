# Illustrator 颜色与百科肖像：实机反例及维护契约

日期：2026-09-17。修复前版本：`4b3fb931`；本轮意图检查点：`fdcc8d2e`。
本文与模块根 `AGENTS.md` 是后续修改必须阅读的持久记录，不依赖聊天记忆。

## 2026-09-19 原生场景导出反例

`1c92decf` 实机记录 `20260918T203953_a098443ab2634860955703ce0cb42c3e` 中，实际送出的五张原生 SceneView PNG 已整体发蓝，人物 JPEG 正常；用户现场截图是暖棕色。对同一原生 PNG 做 R/B 对照后墙体、帷幕和火光恢复到对应暖色。证据位于 `artifacts/illustrator-scene-regression-20260919/evidence/`，生成脚本 `tools/illustrator/inspect_scene_reference_regression.py` 只写对照，不修改原图。

这是新原生场景导出生产者没有经过通道适配，不能通过修改 UI 或对整张已生成成图交换通道解决。新 `PanoramaProjection.Compose` 只接收六张原生场景导出 PNG，在投影合成前一次 R/B 适配；标准截图、HTTP结果、缓存图、UI准备函数和人物导出链保持原有契约。全景合成后的 PNG 已是标准RGBA，后续禁止再换色。

六张不同镜头还须验证实际导出覆盖；旧版五张实图是同一视角且一张人物破面，不能把数学矩阵测试通过当成原生采集正确。私有预制体快照、引擎调度与完整验收范围见当轮 HANDOFF。

## 0. PNG 颜色类型盲区（2026-09-18 追加实机反例）

用户报告：同一版本 DLL 下，chat 协议（gemini-3.1-flash-image）生成图游戏内颜色正常，images 协议（gpt-image-2.5-exact via /images/edits）生成图游戏内**整体红蓝反置**；缓存文件在资源管理器中颜色正常。

实机取证（同一战役、同一会话、同一 DLL、同一加载器）：

| 缓存文件 | 生成通道 | PNG IHDR |
|---|---|---|
| `…_20260917212151192_6d590e.png` | chat/completions | **ct=6 RGBA8** → 游戏内正常 |
| `…_20260917212819557_b33960.png` | images/edits | **ct=2 RGB8（无 alpha）** → 游戏内蓝 |
| `…_20260917212931611_ffffb0.png` | images/edits | **ct=2 RGB8** → 游戏内蓝 |

- 全缓存 262 张 PNG 无一发蓝；两条协议产物在字节层均过 `ImagePayload.Normalize`，显示层共用 `LoadOrRegisterPngBytes`（无任何换通道代码，符合第 1 节契约）。
- 根因：`Normalize` 对 **所有** PNG 原样透传，但 `CreateFromMemory` 对 `ct=2`（24bit RGB，3 字节/像素）与 `ct=6`（32bit RGBA）走**不同解码分支**——3 字节源转 BGRA 纹理时通道次序被引擎按另一套处理。此前契约只在 RGBA 上取证，`ct=2` 是盲区。
- 修复（`ImagePayload.Normalize`）：仅 `bd==8 && ct==6 && interlace==0` 的 PNG 原样透传；其余 PNG 变体（RGB/灰度/调色板/16bit/隔行）经 GDI+ **无损重编码**为 RGBA8 再交付。**这不是通道交换**——像素值逐一保留（实测 25 采样点 0 差异），只是把输入统一到已验证的解码契约格式。
- 旧缓存自愈：`DiskImageCacheManager` 经 `ImagePayload.ReadFile → Normalize` 读取，存量 ct=2 文件下次显示时自动转码，磁盘原文件不改写。
- 维护约束沿用：禁止在 UI 加载路径恢复任何换通道逻辑；新发现的颜色异常仍按"同一图像身份"逐段取证，不得凭"蓝色"症状直接改通道。

## 1. 蓝皮不是本次缓存原图的颜色

用户提供俄洛斯和拉盖娅的游戏截图：皮肤蓝色，原本金色的纹章也变蓝。

只读检查同一角色、同一次生成的缓存，而非另一张图：

- 俄洛斯：`de7087fb76fcf32fbd64cbcd8b1e4523_20260916205343340_fdd368.png`，原图正常肤色、紫色盾底、金色图案。
- 拉盖娅：`8e537a86b9b0b09b641d29fe1288f151_20260916205831210_a6467c.png`，原图也是正常肤色、紫底金图案；举旗扶桌已经存在于生成图，不是 UI 引起的姿态变化。
- 俄洛斯缓存缩略图与截图同一区域统一缩放后的 RGB 平均绝对差约 13.12/255；只交换缓存缩略图 R/B 后降至约 3.26/255。缩放、JPEG 缩略图与截图取样有误差，此数值是辅助对照，不是精确 GPU 标定。

原代码在 `GauntletTextureLoader.LoadOrRegisterPngBytes` 中，当默认开启的 `FixColorChannels` 为 true 时，把已编码 PNG 解码、交换红蓝、重新编码，再交给 `Texture.CreateFromMemory`。该操作改变的是图片实际颜色，不是“修正显存格式”。

**本次确证的故障点是 UI 预处理。** 不能再把 `Format32bppArgb` 的 BGRA 内存布局当成 `CreateFromMemory` 需要交换 PNG 红蓝的证据。前版 README/MCM 中“UI 默认红蓝反置、必须开启”的说明错误，已删除。

### 修复契约

- UI 使用 `PrepareEncodedImageForUi` 校验并保持标准图片颜色；正常 PNG 字节原样通过。
- 删除 UI 的 `SwapRedAndBlueInPng` 转换。
- 从 MCM 移除“修正 UI 显示色彩通道”选项及错误说明。
- 用户明确项目未发布、不需要旧配置兼容：彻底删除 `FixColorChannels` 设置属性、`IllustrationOptions` 字段和读取、loader/准备函数中的 bool 参数，以及三个 UI 调用点的参数传递。无设置、无兼容层、无调用方覆盖入口；只隐藏 MCM 项或改默认值不算完成。
- 不改写、删除、重新编码真实用户缓存；不修改用户设置文件。
- 原生舞台导出的适配和编码图片 UI 加载分开。此次没有将修复扩散至未经重新取证的原生导出分支；后续修改这些分支必须单独提交色块/旗帜对照。

## 2. 为什么百科里反复出现旗帜

读取实际缓存的最终提示词，发现以下明确输入：

- 拉盖娅：`【当前装备中的武器与盾牌】…武器: 纪律之旗帜`。
- 温吉德：`【当前装备中的武器与盾牌】…武器: 苏丹老鹰之旗`，其导演输出进一步把旗帜写进背景。

`HeroVisualExtractor.ExtractWeapons` 扫描全部武器槽时没有单独处理 `ItemTypeEnum.Banner`，旗帜落入普通武器分支，又进入“不可改写的游戏事实”。这与“纹章标准图不是要求画旗”的通用文字限制互相拉扯。读取完整 12 槽是正确要求；**装备持有信息不等于本人正在举旗，也不等于该物件必须入镜。**

修复：

1. Banner 类型进入独立 `BannerEquipmentDetails`，不再被标成普通手持武器。
2. 完整 Equipment、渲染快照及旗帜槽保持不变，没有以“少读装备”规避问题；普通武器与盾牌继续保留。
3. 摘要明确区分“旗帜装备栏记录”和“现场可见/使用证据”。
4. 百科肖像明确不展示旗帜、旗杆或巨大悬挂纹章；已有盾面/服饰上的纹章仍保真。
5. 会话/周报不全局禁止旗帜；实际现场或事件的旗帜仍按证据表现。
6. 最终请求只保留一份完整保真契约，去除导演与生图客户端重复堆叠的同一段落。

## 3. 举旗撑桌的动作来自哪里

拉盖娅缓存的导演正文要求侧身站立、长矛靠身侧、盾自然背负，**没有明确要求举旗扶桌**。因此不能把该具体动作说成某条模板逐字强制：具体持旗撑桌动作由最终生图模型补出。

但管线存在可修正的诱因：

- 旗帜被写成必须尊重的普通武器，增加持旗倾向。
- 百科随机模板含行进、下马、穿过空间、广角前景等动作/环境构图。
- 身份参考图标签及总导演提示反复要求禁止复制姿态、必须重设计；重绘又要求与前图不同。

修复：

- 百科默认自然直立/轻微侧身、重心稳定、肩臂放松，以面部和实际衣甲为视觉中心；背景简洁低对比。
- 不再用百科随机模板强加行进/下马/桌案/举旗动作。重绘只适度变化镜头、景别、光线，不要求更换姿势或添加道具。
- 允许保留人物参考图里的自然姿态，不照抄界面和游戏渲染质感。
- 百科构图规则加入硬事实区，贯穿导演、离线合成及最终生图请求；最高随机强度也不能覆盖。
- `ViolatesPortraitComposition` 对百科导演输出中的旗/桌/撑/扭身等词作保守拒绝，使用本地肖像回退，不重试付费请求。该文本检查可能将“不要画旗”这类否定表述也回退，属于有意保守取舍；它不是图像理解或人体解剖检测。
- 对会话/周报模式不启用百科专用拒绝规则，不抹掉真实战场旗手。

## 4. 回归与责任边界

`tests/test-full-audit.ps1` 新增：

- 6 个色块（红、蓝、金色、肤色、紫色、半透明）× 首次加载/缓存重开两条测试路径：精确 RGBA 保持；PNG 字节保持。
- 实际 UI 加载路径接线；反射确认设置属性和 options 字段不存在，loader/准备函数没有修色参数；全模块 C# 扫描确认没有设置名或 PNG 通道交换函数残留。
- 真实原生 Equipment 的 Banner 单独分类、普通武器保留、原旗帜槽不变。
- 实际导演输出决策：百科非法道具回退、自然姿态通过、现场旗帜不受百科规则误伤。
- 百科约束经过最大随机强度和最终请求组装仍存在，保真契约只出现一次。

离线通过只能证明取值、分类、请求约束和接线；最终模型仍可能不遵循，不能承诺任何图都不会出现姿态问题。旧缓存中的举旗扶桌已属于图片内容，不能靠 UI 修复抹掉。

## 5. 使用与验收

- 颜色：部署新 DLL 后完整退出并重启游戏，再打开旧卡片/画廊；正常缓存不需要重绘或额外付费。
- 动作和旗帜：旧图不会自动改变，需在用户愿意发起新生成时用新规则重绘；本轮修复不自动调用生图 API。
- 核对俄洛斯与拉盖娅旧图肤色/纹章恢复；再分别验收新百科肖像的自然姿态、低对比背景，以及真实战场旗帜未被全局禁掉。
- 仅修改 Illustrator 子模块。不得借此次修复改主模组、删除真实图片、覆盖原版 DLL 或改部署入口。

## 6. 最终离线验证 / HANDOFF

- 工作树：`F:\AnimusForge-main`，分支：`codex/af-main-refactor-continuation-20260831`。
- 1.3 与 1.4 Release 构建：各 0 警告、0 错误。
- 每个 API 构建：审查/颜色/构图回归 85/85，全装备 53/53，原有回归 178/178，共 632 次检查。原有 `tools/test_illustrator.ps1` 未修改。
- 最终矩阵命令：`cmd_4c7c9891eafdd152323821d5180476fd636a71d2cfa7b8e7`，退出 0。
- 日志：`obj/fidelity/color-{build,full-audit,fidelity,regression}-{1.3,1.4}.log`；隔离 DLL：`bin/color-fix/{1.3,1.4}/`。
- 双 API 编译不等于两套游戏实机启动；离线原生依赖来自本机已安装游戏。未用外部付费 API 验收新图。
- 新增逻辑只在请求准备/结果解析时运行，无每帧扫描；UI 去掉一次无意义的位图交换和重新编码；分类仍为既有固定装备槽遍历。

### 已验证源码范围与版本

下面 SHA-256 对应测试时的工作树文件；提交记录按此文档路径查询。

| 文件（相对模块根） | 行范围 | SHA-256 |
|---|---:|---|
| `src/Engine/GauntletTextureLoader.cs` | 1–184 | `sha256:768d2dc0d48c49d3c43d163a90fe8edd22ac09bc039ce33ebe62d81dec329b6a` |
| `src/Settings/IllustratorSettings.cs` | 1–618 | `sha256:d9876b6fbf29a002615716d142756f6f86f9ed4cbd7b7b49c4c68baab59d13de` |
| `src/Core/IllustratorRuntime.cs` | 1–316 | `sha256:a67a79a27aa355e8cf9531ebabe36336f6e6b8052a05c17ffd4fdabb2087d656` |
| `src/Context/HeroVisualExtractor.cs` | 1–892 | `sha256:cfef3b107170084f22196f89d6a62a14a658d399d2492b3a4a8c2df18f2ae900` |
| `src/Core/VisualFidelityRules.cs` | 1–20 | `sha256:b801b94e6cb698a670e9baa7454970e26b36ae33694192cbf75ebaeb6f823082` |
| `src/Core/VisualDirectorEngine.cs` | 1–482 | `sha256:d4604d02da2072e93cae508e111026e133969b9f617738ab4d59d0239ac4a9e2` |
| `src/Core/UniversalOpenAiImageClient.cs` | 1–887 | `sha256:bd7d61804276a72b311a0e8bd2acbe890853043fd4f0a3a9957d8e7b7089ecca` |
| `src/UI/Overlays/IllustrationCardPopup.cs` | 1–481 | `sha256:93bdd5cfeac0fb3fd5da6ec486c14e247191f399f219f7f5d0a6fbca7b845956` |
| `src/UI/Gallery/IllustratorGalleryPopupVM.cs` | 1–341 | `sha256:efb648266bec730da67d0867d9b0e858701583b1af292e166260d680c4973494` |
| `src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs` | 1–470 | `sha256:eb17b8ca69bf8cde7320de579cbf97a84bfcaa4c9a8257d9bf6991040bdad1fa` |
| `tests/test-full-audit.ps1` | 1–227 | `sha256:92ddb051c57eabf04ec5efe759c669a9e31188f323dce6624a08ac7ab8fbd6b0` |
