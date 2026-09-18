# 会话人物外观与日夜来源修复

源码/测试：`b36fe53b`；修改前检查点：`5eca84eb`（保留此前 HeroVisualExtractor 未提交改动）。工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。仅本地修复，未推送、未部署。

## 证据与处理

用户尼丰截图对应缓存 `HccAX3011oX4/conversation/8a48be4c1236ce7eee5d1491114a697d_20260918072938818_604569.png`，已实际查看与截图一致。`rgl_log_8372.txt` 15:28:28–15:29:38 记录人物装备回退战斗装备栏、头部 `empire_battle_crown_north`，导演 5 张参考图、生图 Chat 实际 4 张。缓存提示词已将护鼻护颊战盔概括为冠冕，将宽紫色披肩改写为紫色内衬；临时人物参考 PNG 已清理，无法核验当次离屏图像内容。

- 导演须逐人按参考图描述实际可辨认的头盔覆盖范围、护具、披肩轮廓与主色；物品名称和身份不充当形状证据，不添加未辨认的部件。
- 材质提取不再按 `crown` 等名称直接输出冠冕/兜帽造型，使用装备材质元数据。
- 删除头部事实和面貌事实里“隐藏头发+胡须等于全脸遮覆”的推断，也删除“名称含 hood 就遮住口鼻”的普通 NPC 推断。保留游戏装备和原生渲染标记；All 遮发、All 遮须独立保留在提示词，面貌段不补画这些被隐藏的须发。既有明确呆喵描述保留。真正全包覆仍要求按参考图完整遮覆。
- Chat 与 Edits 共用人物外观优先说明：文字概括与参考图可见外观冲突时保留外观，姿态、衣褶和光照仍按导演整幅重绘。
- 会话仅在成功读取有效 Mission 场景时间后保留现场时段。大地图会话或读取失败时清除战役时钟推导的时段/光线描述，标明未知并让导演依据当前现场截图辨认；缺失图像时不擅自断言昼夜。周报事件时间不变。增加每次会话生成一次的 ConvLight 来源日志。

正常链路保持“元指令＋硬事实＋参考图→导演四段描述→生图描述＋参考图”，没有把完整事实重新追加到成功生图路径，也没有添加付费重试或自动图像审查模型。

## 核实位置（相对 extensions/AnimusForge.Illustrator/src，b36fe53b）

| 路径与行号 | 符号 / 责任 |
| --- | --- |
| `Core/VisualFidelityRules.cs:5–16,37–38` | `DirectorAppearanceFidelity`、`CharacterAppearancePriority`、`ReferenceRoleInstruction`；外观转写及共用优先规则 |
| `Core/VisualDirectorEngine.cs:72–73` | `SystemPrompt`；导演实际 system 注入 |
| `Core/UniversalOpenAiImageClient.cs:527` | `AttemptGenerateOnceAsync`；Chat 人物参考说明，Edits 沿用 `ReferenceRoleInstruction` |
| `Context/HeroVisualExtractor.cs:594–629,694–718,783–861` | `ExtractArmorSlot`、`BuildHeadgearDescription`、`ResolveItemMaterial`、`ExtractPhysicalFeatures`；名称、材质及遮蔽分离 |
| `Context/EnvironmentVisualExtractor.cs:24–26,53,64–76,699–706` | `HasSceneTime`、`UseConversationTimeEvidence`、`ProbeLiveScene`；时段来源 |
| `Context/ConversationContextExtractor.cs:249–250` | `ExtractFromCurrentConversation`；仅会话接入时间清理 |
| `UI/Overlays/IllustrationCardPopup.cs:356,388–391` | `ExecuteConversationGenerationCore`；日志和现场图用途 |

## 验证和限制

- 1.3 / 1.4 子模块 Release 构建均 0 warning、0 error；使用原 csproj 版本引用选择，未改一键构建/覆盖脚本。
- 两份实际 DLL 的 `tools/illustrator/run_prompt_routing_audit.ps1` 各 **96 PASS / 0 FAIL**。包含四种遮发/遮须组合、Mission 存在与场景时间读取成功的独立组合、非会话时间保留，以及生产 Chat/multipart 请求外观规则。HTTP 使用内存 handler，无供应商访问。
- 日志：`artifacts/illustrator-appearance-fidelity/audit-1.3.txt`、`audit-1.4.txt`。DLL 位于同目录的 `1.3/`、`1.4/` 子目录。编译仍包含用户原有其他未提交依赖，不能将本提交单独当作完整发布版本。
- 只在生成时使用现有快照进行字符串处理、参考图说明拼接和常数次时间判断，无新增逐帧扫描、反射、图像处理或缓存。
- 未验证：真实导演/生图服从率、原生 GPU 导出、实际遮发遮须显示及新的成图。未增加自动视觉对照校验；离线测试仅证明请求和事实路由，不证明模型一定准确还原。还需用同一尼丰装备以及开面/全包覆头盔实机对照。

回滚仅定向 `git revert b36fe53b`；不得回滚检查点中保留的用户原有工作或重置整个工作树。
