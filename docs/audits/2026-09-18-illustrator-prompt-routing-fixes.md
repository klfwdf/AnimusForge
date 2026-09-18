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
