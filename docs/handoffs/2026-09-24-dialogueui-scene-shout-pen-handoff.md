# DialogueUI 场景喊话轮盘与全屏输入 UI：PEN 实施交接

日期：2026-09-24  
工作区：`F:\AnimusForge-main`  
分支：`codex/af-main-refactor-continuation-20260831`  
记录时 HEAD：`e8cf180f89c1c364602bb60a09cce958fee8c2d9`

## 1. 任务边界与当前状态

这是 **AnimusForge.DialogueUI 子模块的独立视觉/PEN 任务**。用户已明确该子模块不受主项目 AF core/framework skill 的流程约束；本交接只描述 DialogueUI 视觉资产和 Pencil 文档，不要求 PEN agent 先做 AF 双版本构建、部署或游戏接入。

当前仍处于 **预览确认阶段**：

- 用户确定以原方案 01（象牙羊皮纸与古铜）为基础。
- 已生成两套方案 01 精修预览：A 花饰金线、B 冷铜纹章。
- **尚未写入 Pencil 文档**。
- **尚未修改轮盘/场景喊话运行时代码、Prefab、资源注册或热键逻辑**。
- 只有在用户明确选择 A 或 B 并授权后，才把选定方案写入 PEN；未授权前不要调用 Pencil 写入工具。

## 2. 用户已经确认的交互方向

### 轮盘

- 入口只保留 T：按住 T 框选 NPC，松开打开轮盘。
- Y 不再作为物理入口。
- 一级轮盘包含：`对话`、`动作`、`离开`。
- 动作二级内容必须继续放在轮盘体系内，不使用右侧矩形菜单：
  - 给予物品
  - 展示物品
  - 给予部队
  - 给予俘虏
  - 转移固定资产
  - 演讲
  - 开发模式标签测试
- 预览中使用一级轮盘 + 圆形动作二级轮盘，表示动作分支仍在轮盘内。
- 主目标需要被清晰标记；无名路人显示可点击取消和已取消状态。

### 场景喊话输入 UI

用户提供的参考图决定了布局方向：

- 场景主体占据上方大部分画面，底部三分之一放交互层。
- 底部交互层采用宽幅羊皮纸卷轴，而不是 980×460 弹窗整体拉伸。
- 上方有快捷话题/参与者状态条。
- 中央是宽幅历史和深色输入框。
- 左侧保留参与者勾选，主目标锁定，无名路人可取消。
- 右侧使用圆形功能按钮：历史、参与、插画占位；保留发送和离开。
- 通过 `收起 ▲` / `展开 ▼` 按钮在展开卷轴和底部状态条之间切换。
- 发送后继续保持相同参与者快照；本轮插画按钮只做视觉占位。

## 3. 当前精修预览文件

### 精修 A：花饰金线羊皮纸

- `extensions/AnimusForge.DialogueUI/outputs/shout-wheel-option-01-refined-a-preview.png`
- `extensions/AnimusForge.DialogueUI/outputs/shout-session-option-01-refined-a-preview.png`

视觉关键词：暖金线、植物卷草纹、红色蜡封、浮雕角饰、暖象牙纸张。

### 精修 B：冷铜纹章羊皮纸

- `extensions/AnimusForge.DialogueUI/outputs/shout-wheel-option-01-refined-b-preview.png`
- `extensions/AnimusForge.DialogueUI/outputs/shout-session-option-01-refined-b-preview.png`

视觉关键词：青灰冷铜、纹章徽记、蓝色蜡封、几何边线、偏冷象牙纸张。

四张精修图均为 `1920×1080 RGB`。快速对比图（仅供查看，不是 PEN 源文件）：

- `.tmp/scene-shout-option01-refined-contact.png`

基础五套预览仍保留在：

- `extensions/AnimusForge.DialogueUI/outputs/shout-wheel-option-01..05-preview.png`
- `extensions/AnimusForge.DialogueUI/outputs/shout-session-option-01..05-preview.png`

不要覆盖旧的 `shout-option-01..05-preview.png`（它们是旧版 980×460 场景喊话输入候选）。

## 4. 可复现预览工具

基础全屏预览生成器：

- `extensions/AnimusForge.DialogueUI/tools/prepare_scene_shout_previews.py`

方案 01 两套精修生成器：

- `extensions/AnimusForge.DialogueUI/tools/prepare_scene_shout_refined_option01.py`

运行命令：

```powershell
python extensions\AnimusForge.DialogueUI\tools\prepare_scene_shout_refined_option01.py
```

精修生成器只输出四张新 PNG，不触碰 PEN、Prefab 或运行时代码。其装饰层包含：

- 羊皮纸纹理颗粒和纤维线；
- 四角卷草/纹章线稿；
- 滚轴边缘铜钉；
- 左侧喊话丝带标签；
- 右上蜡封；
- 输入框内嵌雕刻边线和羽笔符号；
- 历史/参与/插画圆形按钮的金属内圈；
- 收起状态说明条。

## 5. PEN 实施目标

Pencil 文档：

`C:\Users\29310\.pencil\documents\88c60482-c2e6-49c1-b12d-63e6866ac83d\pencil-new.pen`

现有旧场景喊话 Frame：

- `GrsfR`
- `izPAQ`
- `tEgI3`
- `tuwz1`
- `b712q`

这些旧 Frame 是 1920×1080 画布里的 980×460 弹窗候选。PEN agent 应保留它们作为历史候选，不要删除或覆盖。

### 授权后的写入步骤

1. 先确认用户最终选择精修 A 或精修 B；如果用户明确说“两套都写”，才同时写入两套。
2. 使用 Pencil MCP 打开上述 `pencil-new.pen`。
3. 新增全屏轮盘 Frame 和全屏场景喊话 Frame，画板尺寸统一 1920×1080。
4. 轮盘画板必须包含一级轮盘、圆形动作二级轮盘、主目标、候选 NPC、无名路人取消态、悬停态和确认态。
5. 场景喊话画板必须把卷轴交互层放在底部三分之一，保留场景可见区域，并展示展开态与收起态入口。
6. 继续保留现有对话 UI 与旧历史候选，不把 980×460 贴图作为新整体背景拉伸。
7. 将羊皮纸、边框、铜钉、卷草、蜡封、按钮、中心底纹等拆成可复用层；后续 Gauntlet 适配时按布局层重排。
8. PEN 阶段只改 Pencil 文档和设计画板，不注册游戏资源、不改 Prefab、不构建、不部署。

## 6. PEN 视觉验收清单

- [ ] 用户选定的精修风格与预览一致。
- [ ] 画布为 1920×1080；安全边距在不同宽高比下不裁切。
- [ ] 一级轮盘与动作二级轮盘均为圆形层级，不退回右侧矩形菜单。
- [ ] 二级动作文字完整可读，长文本“转移固定资产”“开发模式标签测试”不溢出。
- [ ] 主目标锁定状态明显，不能误显示为可取消。
- [ ] 无名路人有已选、悬停、取消三种视觉状态。
- [ ] 场景喊话卷轴位于底部约三分之一，场景主体仍可见。
- [ ] 参与者勾选、历史、输入框、发送、离开、插画占位和收起/展开按钮均存在。
- [ ] 展开态和收起态的边缘、按钮禁用态、文字对比度通过检查。
- [ ] 不删除既有对话 UI、旧 Frame 或旧候选预览。

## 7. 当前验证与未验证项

已验证：

- 四张精修预览均存在，尺寸为 1920×1080、模式 RGB、内容边界完整。
- `prepare_scene_shout_refined_option01.py` 已通过 Python 编译检查。
- A/B 两套轮盘和场景喊话预览已做视觉抽查。

未验证：

- PEN 文档是否已写入（当前明确未写入）。
- Pencil 画板在实际编辑器中的安全边距、节点层级和文本可编辑性。
- Gauntlet Prefab、DialogueUI runtime、T/Y 热键、AgentIndex 参与者校验。
- Bannerlord 1.3/1.4 构建、部署和游戏内验收。

## 8. 回滚点

本交接新增的可回滚文件只有：

- `docs/handoffs/2026-09-24-dialogueui-scene-shout-pen-handoff.md`
- `extensions/AnimusForge.DialogueUI/tools/prepare_scene_shout_refined_option01.py`
- 四张 `shout-*-option-01-refined-{a,b}-preview.png`

删除上述新增文件即可撤销本轮交接/精修预览；既有 `shout-option-01..05-preview.png`、基础全屏预览、旧 Pencil Frame 和其他 DialogueUI dirty 文件不得一起回滚。

## 9. 给下一位 PEN agent 的短指令

> 这是独立 DialogueUI 视觉任务。先查看四张 `option-01-refined-{a,b}` 预览，等待用户明确选择 A 或 B 并授权。授权后只在 `pencil-new.pen` 新增所选全屏轮盘与场景喊话画板：动作二级菜单放进圆形轮盘，场景喊话采用底部三分之一羊皮纸卷轴，补齐卷草、铜钉、蜡封、历史/参与/插画按钮、发送/离开、参与者勾选和收起/展开状态。保留旧 Frame，不改 runtime、Prefab、资源注册、构建或部署。
