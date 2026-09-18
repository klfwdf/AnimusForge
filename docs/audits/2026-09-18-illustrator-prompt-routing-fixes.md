# 生图提示词链路审查与四项修复

源码修复提交：`ec0d13fb`。工作区：`F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。
这是对当前未提交版本的修复，不是 G 盘框架续作或远程交付。编译输入包含用户此前保留的设置、外观提取等未提交更改，不能把此提交单独视为完整发布版本。

## 期望链路与审查结论

正常成功路径：元指令、硬事实、叙事原文和参考图进入导演；导演推导四段视觉描述；视觉描述与人物参考图进入生图端。叙事原文、原始事实块和导演元指令不会在成功路径整体再追加给生图端。生图端仍追加画风、负面词、格式及身份保真要求，但不再另外规定左右站位、伸展体态或四层纵深。

本轮修复：

1. 参考图新增显式 Character / Emblem / Scene 类型；周报人物标签提及纹章不再使人物图变成纹章样图。保留原双参数构造，未声明类型时不从标签猜身份。
2. 双人会话约束仅用于会话模式，周报和通用插画按事件数量及关系生成。
3. 百科本地回退移除战盔、胸甲、长戟和军械厅模板，使用非具名艺术布景，保留四段结构及身份事实。
4. 围城室内按具体 location 分类，酒馆、地牢不再提前命中内堡。未知室内不假定用途。
5. 提示词审查追加修复：会话 SceneFacts 与 SceneDirective 分离；骑乘未知不写成步行站在地面；取消围城默认“双方驻马”；动作名称推断只作为导演启发，不当成硬事实。人物参考图锁定身份和穿戴，机位姿态由导演推导。

## 核实代码位置

以下路径以 `extensions/AnimusForge.Illustrator/src/` 为前缀，行号对应 `ec0d13fb`。

| 路径与行号 | 符号 / 责任 |
| --- | --- |
| `Core/IllustrationReferenceImage.cs:3–30` | `IllustrationReferenceKind`、类型化构造；不解析自然语言标签 |
| `Core/UniversalOpenAiImageClient.cs:489–638` | `AttemptGenerateOnceAsync`；真实 Chat 多模态请求分类及最终呈现规则 |
| `Core/VisualDirectorEngine.cs:156–198` | `ComposeFinalPrompt`、`ResolveDirectorOutput`；成功与回退分流、模式传递 |
| `Core/VisualDirectorEngine.cs:225–239` | `BuildLocalSceneDirection`；中性百科回退 |
| `Core/VisualFidelityRules.cs:5–22` | `GetEssentialContract`；按模式限定人物关系规则 |
| `Context/ConversationContextExtractor.cs:42–85,368–405,513–516` | `BuildHardFacts`、`BuildArtDirection`、`DescribeMountState`；事实与指导分离 |
| `Context/EnvironmentVisualExtractor.cs:205–256,339–343` | `ResolveBesiegedLocation` 与生产接线；围城场景分类 |
| `UI/Overlays/IllustrationCardPopup.cs:231–264,384–432` | 百科、会话参考图显式标记；移除固定左右机位要求 |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs:331–359` | 周报人物图、纹章图显式标记 |

## 验证与性能

- 子模块 Release：`BannerlordApi=1.3`、`BannerlordApi=1.4` 均 0 warning / 0 error。
- `tools/illustrator/run_prompt_routing_audit.ps1` 对两份实际 DLL 各执行 **60 PASS / 0 FAIL**。生产 HTTP 客户端在独立测试进程中替换为内存 handler，验证真实请求正文；不访问供应商、不产生付费请求。
- 覆盖成功输出、短输出、空输出；四种模式；人物标签包含纹章/现场词；围城室内与室外；原始叙事隔离；动作元指令与事实分离；缺失 Agent 的未知状态。
- 测试依赖当前安装的托管游戏程序集，只执行纯托管逻辑；不构成 1.3/1.4 原生运行时或 GPU 验收。
- 构建及测试日志：`artifacts/illustrator-four-fixes/build-1.3.txt`、`build-1.4.txt`、`audit-1.3.txt`、`audit-1.4.txt`。
- 类型比较每次生成随参考图线性遍历；地点分类只使用已有快照。没有新增逐帧反射、全场景扫描或缓存失效策略；删除了回退每次创建多组模板数组的分配。

## 保留的边界与未验证范围

- **正常链路不是唯一分支**：导演关闭、失败或输出不合格时仍保留项目要求的本地回退；回退会携带硬事实，未宣称其也是导演生成。未更改自动重试策略。
- 两个已有开关 `EnableMultimodalVision`、`EnableReferenceImageForGeneration` 均默认 true；手动关闭、导演不支持视觉而降级时不会保证两端都有图。没有擅自更改用户设置。
- 本轮没有重做全部事实提取器。发色、头部遮蔽、场景语义等既有启发式仍需实际 MOD/人物/场景样本验收；“硬事实”标签本身不能证明这些推断正确。
- 未验证真实导演遵循程度、付费生图效果、游戏显示/原生导出、历史存档或 TTS。未部署、未推送，未更改构建覆盖脚本。

## 回滚

定向 `git revert ec0d13fb` 仅撤销本轮生产修复与测试。`d3149346` 保存最初七个目标文件的修改前检查点，`6370d4b2` 保存会话提示词追加修复前检查点；不要重置整棵工作树或回滚其他用户改动。

## 自然体态与 Edits 重绘后续修正

源码/测试 `6d5cb827`，修改前检查点 `b7fc2409`。用户反馈 Edits 5 张中 3 张维持立绘站姿、2 张稍作变化；这是用户观察，不是本轮对照试验结果。

- `src/Core/VisualFidelityRules.cs` 的 `DirectorQualityFloor`、`ReferenceRepaint`、`ReferenceRoleInstruction`：导演限制复杂肢体动作、明确机位与人物朝向、重心和接触支撑；生图端要求根据导演描述重新构建人物体积、衣褶、透视、光照及环境反光。保留身份与实际装备，不保留原立绘像素。站立和坐姿均允许。
- `src/Core/VisualDirectorEngine.cs:71,271–277`：质量要求进入实际导演 system prompt，重绘优先通过镜头光线变化，不强制换动作。
- `src/UI/Overlays/IllustrationCardPopup.cs:218–241,288–297`：清除“直立是未经思考”“严禁重复姿势”等旧压力，替换为自然体态和统一重绘；依然传入历史场景母题。
- `src/Core/UniversalOpenAiImageClient.cs:165–173,367–451,474–477`：Edits 真正上传人物/纹章/现场图，添加类型说明和与 Chat 共用的整幅重绘要求；删除自动 mask 及其 Bitmap/PNG 分配。`ResolvedPrompt` 返回实际 multipart prompt，调用方据此保存画廊记录。
- `extensions/AnimusForge.Illustrator/AGENTS.md` 已依据本轮用户要求覆盖旧的反站姿/强制动作变化要求。

以上 `src/` 路径以 `extensions/AnimusForge.Illustrator/` 为前缀。共用文本每次生成拼接一次、参考图处理仍是有界线性遍历；不新增逐帧工作、反射或缓存。

验证：1.3 / 1.4 Release 子模块均 0 warning / 0 error；实际 DLL 各 **71 PASS / 0 FAIL**，日志在 `artifacts/illustrator-natural-pose/audit-1.3.txt`、`audit-1.4.txt`。新增测试通过内存 HTTP handler 检查真实 multipart：三张图的文件顺序、明确用途、完整导演描述、整幅重绘要求、无 mask，以及返回的提示词与发送文本一致。保留前轮 60 项覆盖。

边界：移除全透明 mask 是取消无证据的身份分离假设，不等于证明 mask 导致坏图；本轮没有实际供应商 A/B、视觉结果评估、GPU/游戏验收，没有部署或改动用户 API/随机设置。原有协议降级仍存在，网关不支持 Edits 时依旧按原策略处理。提示词约束不能保证模型执行，须在同一模型、同一参考图下复验人物重绘、支撑自然度及身份保真。回滚只需定向 `git revert 6d5cb827`，不撤销其他未提交改动。
