# Illustrator 会话插画：背景人群与动作生动度 — 交接文档（2026-09-21）

## 任务

会话插画目前"太聚焦对话双方、背景空、动作拘谨"。实机反例：酒馆现场实际有 8 名 NPC（店主/侍女/乐师/雇佣兵/赌徒/镇民），生成图背景却几乎无人；对话已是邀舞进行中，画面却停在"准备伸手"的前一瞬。对照组（另一模组酒馆图）人物动作幅度大、背景酒客满座。

用户批准实施以下五项（对应分析中的 A/B/C/D/F；E 是用户侧 MCM 设置，无代码改动，不在本任务）：

- A：把已采集但未送达的"附近人群活动/陈设"事实接入导演输入
- B：会话系统提示明确"附近角色是现场证据，应安排为背景活动"
- C：生图端环境参考标签明确"此图省略所有人物"
- D：动作偏好"对话动作进行中"而非预备瞬间，鼓励发起—反应的不对称互动
- F：NearbyPropFacts 的 spawn 标记名降噪/转译

## 真实证据（交接前已核实）

诊断目录：`C:\Users\29310\Documents\Mount and Blade II Bannerlord\AnimusForge\IllustratorCache\Diagnostics\`

- `20260921T155223_*`、`20260921T155401_*`、`20260921T155630_*` 三次会话生成（对应三张酒馆图）。
- `155630`：导演输入 `prompt_tokens=13206`，`finish_reason=stop`；`game_facts` 含 `【附近实际角色】开路骑兵(Mercenary)、酒馆店主(Tavernkeeper)、酒馆侍女(TavernWench)、吟游诗人(Musician)、赎金经纪人(RansomBroker)、赌徒(TavernGameHost)、镇民、女镇民`，但导演【场景空间】只写"就座食客轮廓"，成图背景无人。
- `155223`：导演写"稀疏分布的模糊酒客背影"；`155401`：场景段几乎纯建筑家具。
- 三次导演选景均为 `yaw≈-5; pitch≈-3; hfov≈70` 前向切片。
- `【现场附近物体标识】` 实际内容为 `audience、duo、sp_npc_audience_trio、sp_npc_left/middle/right、musicians、play_music_point、animation_point、chair_sit_position_*、adam_0、point_of_interest、torch_candle_c_fire` 等 spawn/动画标记名——恰好证明该酒馆设计上有人群站位，但对导演不可读。

## 根因（勿凭猜测复核一遍再动手）

1. `EnvironmentVisualExtractor.ResolveSurroundings` 已按 25m 内真实 Agent 职业统计生成生动人群描述（`profile.SurroundingCharacters`）及陈设描述（`SurroundingProps`），但两个属性**全代码库无读取方**，`BuildHardFactsSummary` 从不发射它们——死代码。
2. 生图端唯一环境图是投影自"剔除全部 Agent"的全景副本（`PanoramaSceneSnapshot` 按既定安全规则省略骨骼/布料实体），视觉上是一间空酒馆；其标签只说"人物位置按本次事实"，未明说"此图故意无人"。
3. `ConversationSystemPrompt` 只有"没有现场证据不新增人物"的禁令，没有"【附近实际角色】就是现场证据、应入背景"的授权。
4. 【人物与镜头】只说"选择一个能表现双方关系的时刻"，未偏好"进行中瞬间"；历史行动去重样本又都是同类克制动作，模型收敛到预备态。

## 改动规格

### A. 接通 SurroundingCharacters / SurroundingProps

文件：`extensions/AnimusForge.Illustrator/src/Context/EnvironmentVisualExtractor.cs`

- 现状：`SurroundingCharacters`（属性 line 30）由 `ResolveSurroundings`（约 402-518 行）按真实 Agent 职业计数生成，如"吧台后酒馆老板正在擦拭陶土酒杯，侍女端着木托盘在席间穿梭添送麦芽酒；角落处游吟乐师正在低头拨弄鲁特琴弦…"；`SurroundingProps`（line 31）由同方法后半段（约 520-597 行）按子场景类型填充陈设模板。
- `BuildHardFactsSummary`（约 41-63 行）目前发射：定居点/当前子场景/【附近实际角色】NamedCharacters/【附近实际预制件】RealProps/季节/时段/天气/文化/地貌/室内外/冲突状态/场景补充事实。
- **要求**：在该方法中追加发射，建议标签 `【附近人群活动】` 与 `【附近陈设特征】`（命名可微调）。
- **虚构风险（必须处理）**：约 525-596 行的 fallback 模板在**真实扫描为零时**也会填充"老板擦杯、侍女穿梭、乐师弹琴"。若把 fallback 当硬事实发射，空酒馆也会被声称满座。必须区分来源：给 profile 加来源标记（如 `internal bool SurroundingCharactersFromLiveScan`，真实计数分支置 true）；真实扫描版进 `【附近人群活动】`（硬事实）；fallback 版只能进弱标签（如 `【场所类型常识·非在场证据】`）或不发。SurroundingProps 同理：fallback 属"该类型场所典型陈设"，不能当实测事实。
- **消费方核实**：`BuildHardFactsSummary` 被 `ConversationContextExtractor.BuildHardFacts()`（line 61，会话）和 `WeeklyReportContextExtractor`（line 80，周报，标头为"本期提及地点资料并非已选事件现场"）消费。周报 `Extract(eventAnchored:true)` 会跳过 `ResolveSurroundings`（约 154 行 `if (!eventAnchored)`），字段为空、条件发射天然安全。百科肖像路径只用 `HeroVisualProfile`，不经此 profile——**验证百科路径不出现人群行**（肖像个绘不应有背景人群）。
- 可选：`【附近实际角色】`目前含对话对象本人（女镇民也在列表里），可在发射时标注哪位是会话双方，避免导演把主角误当路人；非必须。

### B. 会话系统提示：人群证据授权

文件：`extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs`，`ConversationSystemPrompt`（约 86-98 行）

- 保留 line 89 "没有现场证据不新增人物"不动。
- 新增一条规则（建议并入【场景空间】或紧邻【人物与现场边界】之后），语义等价于：
  > 【附近实际角色】与【附近人群活动】属于已确认现场证据。【场景空间】应把证据支持的在场人物组织为与所选机位一致的背景活动（写清方位与正在做的事，如吧台擦拭、席间穿行、角落弹奏、邻桌掷骰），数量与身份不超过所列证据；没有人流/人群证据的场景才允许空旷背景。
- 注意措辞层级：授权的是"按证据安排在场者"，不是放开虚构人物。

### C. 生图端环境参考标签补"省略人物"声明

文件：`extensions/AnimusForge.Illustrator/src/Core/IllustrationReferenceRouting.cs`，约 62-67 行 `IllustrationReferenceKind.ScenePerspective` 标签

- 现状标签："同一现场独立静态副本的普通透视环境参考：…保留可见建筑、家具、门窗与材质的空间关系，按正文统一绘制人物和环境；不把参考取景作为必须复制的画面。这是中性观察补光，不代表现场光源；光源方向、时段与氛围按正文，人物位置按本次事实。缺失区域不作为开放天空或新增物体的证据。"
- **要求**：追加语义等价于"此参考图省略了现场所有人物与动态实体，空桌椅/空地不代表现场无人；在场人物与背景人群以正文描述为准。"——该标签发给**生图模型**（不是导演），措辞面向生图端。
- 对照：`PanoramaSceneSnapshot.Notes`（约 81 行）已有"主动省略人物…不能据此推断现场人数"，但那是**导演侧**全景说明；本项改的是生图侧投影图标签，两者不重复。

### D. 动作偏好"进行中"与不对称互动

文件：同 `ConversationSystemPrompt` 的【人物与镜头】段（约 94 行）

- 现状："从最近对话选择一个能表现双方关系的时刻，逐人确定一个主要行动或注意对象，再写景别、机位、身体朝向、可见手部和视线；只转写入镜且可辨的外观装备。"
- **要求**：追加/改写为偏好语义——最近对话已经发生的动作优先选"进行中瞬间"而非预备瞬间（邀舞→已在搭手/转步而非伸手前；举杯→已碰杯；递物→正在交接）；优先一方发起、另一方可见反应的不对称互动；避免默认两人都坐着对视。
- **保留底线**：`VisualFidelityRules.DirectorQualityFloor` 的"不强迫复杂动作/支撑真实"、装备事实约束不变；只是给导演"进行中"是合法且偏好的选择，不是强制每次都画剧烈运动。

### F. NearbyPropFacts 标记降噪

文件：`extensions/AnimusForge.Illustrator/src/Engine/PanoramaSceneSnapshot.cs`，`ObserveNearbyProp`（约 48-72 行）/`NearbyPropFacts`（约 73-75 行）

- 现状：24m 内实体 prefab/名称按距离取 20 种直接进 `【现场附近物体标识】`，真实样本几乎全是 `sp_npc_*`、`*_point`、`chair_sit_position_*` 等不可读标记。
- **要求**（实现者二选一或组合，保持 ≤20 上限与现有预算）：
  - 过滤模式化标记名（`sp_*`、`*_point`、`point_of_interest`、`animation_point`、`chair_sit_position_*` 等无可见语义者），保留可读家具/道具名；
  - 或将人群类标记（`audience`、`musicians`、`play_music_point`、`sp_npc_*`）转译为一条可读提示，如"场景配置有乐师演奏位与观众/酒客站位"——**措辞必须是"站位/设计配置"而非"现场有人"**，是否真有人以 NamedCharacters/人群活动事实为准。
- 保留"仅为物体识别线索，不代表全部陈设"的免责语气。

## 必须遵守的约束

- 先读 `extensions/AnimusForge.Illustrator/AGENTS.md` 全部持久规则：禁背盾、全景采集安全约束、颜色契约、诊断/失败语义等。
- 不改 `ScenePerspectiveProjection.cs`、`PanoramaProjection.cs`、`IsolatedPanoramaRenderer.cs`、`SceneReferenceCapture.cs` 的采集/投影机制；C 项只改标签字符串。
- 不新增模型调用、不新增场景扫描、不加热路径枚举：人群扫描已在 `ResolveSurroundings` 内运行（用户触发生成时一次性），发射是纯字符串拼接。
- 不放宽"无证据不新增人物/盾牌/旗帜/坐骑/纹章载体"边界；所有新增措辞都以"已列证据"为上限。
- 双版本兼容：`BannerlordApi=1.3` 与 `1.4` 均须 Release 0 警告 0 错误。

## 验证清单

```powershell
# 构建（仓库根 F:\AnimusForge-main）
dotnet build extensions\AnimusForge.Illustrator\src\AnimusForge.Illustrator.csproj -c Release -p:BannerlordApi=1.4 -p:OutputPath=bin\<tag>\1.4\ -p:BaseIntermediateOutputPath=obj\<tag>\1.4\
dotnet build extensions\AnimusForge.Illustrator\src\AnimusForge.Illustrator.csproj -c Release -p:BannerlordApi=1.3 -p:OutputPath=bin\<tag>\1.3\ -p:BaseIntermediateOutputPath=obj\<tag>\1.3\

# 审计（对构建产物 DLL，基线：42+142+61+35=280）
.\tools\illustrator\run_director_status_audit.ps1 -AssemblyPath <dll>
.\tools\illustrator\run_prompt_routing_audit.ps1 -AssemblyPath <dll>
.\tools\illustrator\run_reference_routing_audit.ps1 -AssemblyPath <dll>
.\tools\illustrator\run_module_review_audit.ps1 -AssemblyPath <dll>
# 触及 PanoramaSceneSnapshot/SceneReferenceCapture 的 F 项另跑：
.\tools\illustrator\run_panorama_snapshot_audit.ps1 -AssemblyPath <dll>
.\tools\illustrator\run_scene_capture_audit.ps1 -AssemblyPath <dll>
```

- `PromptRoutingAudit` 断言系统提示内容，提示词刻意改动后需同步更新断言（保留"无证据不新增人物"等安全断言）。
- 部署：`tools\deploy_illustrator.ps1 -BannerlordApi auto`，需关闭游戏；脚本自动备份旧模块并校验 SubModule/Prefab；部署后核对安装 DLL SHA256/MVID。
- 台账：完成后更新 `HANDOFF.md` 与 `docs\animusforge-refactoring-and-repository-reorganization-plan.md`（改动文件、行号、指纹、已验证/未验证、回滚点）。

## 验收标准（实机）

- 酒馆会话生成：导演【场景空间】出现与【附近实际角色】一致的背景人群活动（店主/侍女/乐师/雇佣兵等按证据数量），成图背景可见酒客；
- 对话为邀舞/举杯等进行中动作时，画面呈现进行中瞬间而非伸手前；
- 空旷场景（无附近角色证据）不得被强行塞人；
- 四项核心审计不回归。

## 非目标 / 用户侧事项

- E：MCM `生图分辨率尺寸` 由 `1024x1024` 改为 `1344x768 (7:4)` 或 `1280x720 (16:9)` 可零代码改善横构图人群空间——用户自行设置，本任务不改默认值。
- 不改透视/投影/采集管线，不改装备保真规则，不为生动度牺牲现场事实。

## 相关位置速查

| 事项 | 文件 | 位置 |
|---|---|---|
| 人群扫描与死属性 | `src/Context/EnvironmentVisualExtractor.cs` | 属性 30-31；扫描 402-518；fallback 520-597；发射 41-63 |
| 会话系统提示 | `src/Core/VisualDirectorEngine.cs` | `ConversationSystemPrompt` 86-98 |
| 生图环境标签 | `src/Core/IllustrationReferenceRouting.cs` | 62-67 |
| 附近物体标识 | `src/Engine/PanoramaSceneSnapshot.cs` | `ObserveNearbyProp` 48-72；`NearbyPropFacts` 73-75 |
| 会话 facts 组装 | `src/Context/ConversationContextExtractor.cs` | `BuildHardFacts` ~58-66 |
| 部署 | `tools/deploy_illustrator.ps1` | `-BannerlordApi auto` |

当前部署态：游戏安装为输入精简版 MVID `2082849d-7de6-4489-8dbe-ee4091d4fbce`（SHA256 `E2A7897F…C626`）；回滚备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260921-234406`。
