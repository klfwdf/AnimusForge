# 羊皮纸素材与静态预览

当前阶段：验证。已实际调用本地 `relay-image-gen` 技能一次，配置模型为 `gpt-image-2.5-sunburst福利`。原始输出是 1536×1024 PNG；无额外重试，无游戏运行时生图。

## 来源与可复现处理

- 提示词：`work/parchment-atlas.prompt.txt`。
- 原始输出：`outputs/image_c024cff2f9264fda8757b882d6bce9df.png`，完整保留。
- 离线处理：在子模块根目录运行 `python tools/prepare_assets.py`；依赖 Pillow，不需要密钥或网络。
- 素材清单与 SHA-256：`work/assets-manifest.json`。
- 模型未遵循纯色底要求，因此处理脚本使用针对该原图校对过的轮廓多边形和 4 倍采样抗锯齿蒙版提取透明素材。更换原图时必须重新校对蒙版，不能直接沿用坐标。

## 运行时资源契约

所有资源位于 `GUI/SpriteParts/`，名称不加分类前缀，均为 RGBA PNG；其中输入底板是完整不透明内嵌矩形，其余素材边界含透明像素。资源只有装饰，没有烘焙文字、人物、纹章或点击区域。

| Sprite 名称 | 尺寸 | 使用方式 |
| --- | --- | --- |
| `afdui_scroll_left` | 160×320 | 卷轴左端，和整体高度同比缩放，保持左右端宽度 |
| `afdui_scroll_body` | 1024×320 | 中央羊皮纸，横向扩展；较宽源图避免窄纹理拉成横条 |
| `afdui_scroll_right` | 160×320 | 卷轴右端，与左端对称使用 |
| `afdui_parchment_panel` | 512×512 | 四边 40 px 九切；角花已归一到安全区域 |
| `afdui_input_panel` | 512×192 | 四边 16 px 九切；深棕输入/选项底板 |
| `afdui_button_normal` | 64×64 | 无图标圆形古金底座 |
| `afdui_button_hover` | 64×64 | 提亮的悬浮态 |
| `afdui_button_pressed` | 64×64 | 压暗的按下态 |
| `afdui_wax_seal` | 96×96 | 无虚构印记的蜡封装饰，可选使用 |

运行时加载和缓存 PNG，不应运行此制作脚本；发布包只需 `GUI/SpriteParts/` 中的素材，不应携带 `work/` 和原始 `outputs/`。

## 静态预览与验证界限

`preview-shout-*` 和 `preview-conversation-*` 分别提供 1920×1080、1366×768、2560×1440、2560×1080 四种画布；另有 1920×1080 的记录展开与普通选项预览。`asset-contact-sheet.png` 可检查透明边缘、边框与三态按钮。

背景取自用户提供的参考场景截图并裁去原 UI，缓存为 `reference-scene-crop.png`，只用于设计预览。三栏左侧是明确标注的“原生人物肖像”占位，其他文案也是示例；它们不属于运行时材质。

已执行并通过：9 个 PNG 的 RGBA 格式、尺寸、透明轮廓、文件哈希、8 张分辨率画布尺寸校验；人工查看基本布局、1366×768、超宽画布、记录展开和普通选项。首轮发现的卷轴纸纹拉伸条纹和记录展开提示行超出纸边已修正。

尚未验证：Bannerlord 实际纹理上传/九切效果、运行时控件实际位置、游戏 UI 缩放、中文输入法和焦点、原生肖像渲染。这些静态 PNG **不是游戏实机验收证据**。

## 文件与回滚

本美术切片仅新增 `work/`、`outputs/`、`GUI/SpriteParts/`、`tools/prepare_assets.py`，未修改主模块或提交 Git。由主任务按意图检查点 `d3d4679e` 后的对应美术提交定向回滚，不影响其他人的文件。
