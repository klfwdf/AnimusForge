# 空回复取证、百科创作与城镇／野外／周报独立管线

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。空回复显示修正 `2a565c51`（检查点 `fc258ee`）；最终生产源码 **`3a73006a9546d64577b2ed3cd1abc3c2bfeee163`**（检查点 `2b44da6`）。用户要求持续修复、不跑离线测试，并追加不要过度保守、城里与野外分开、周报独立事件管线以及保留人物离屏立绘。

## 真实问题与证据

### 三次空回复

均来自已加载 `77ed68e2-8649-4040-84b2-d4deeecd305e` 的百科请求，导演正常完成；模型 `gemini-3.1-flash-image`，Chat协议、两张人物参考。服务端HTTP200包中 `content=null`，没有images/data图片，`finish_reason=null`、`native_finish_reason=null`、`completion_tokens=0`。完整返回没有发现解析器漏读的图片字段，也没有明确refusal或content_filter。

| 本地开始时间 | 生图等待 | 服务端请求ID |
| --- | --- | --- |
| 07:53:09 | 12.428秒 | `9c6taqe_G56DkdUPgMTAuAE` |
| 07:54:41 | 16.220秒 | `Uc-tavL4LcWsmNMP87jjkQk` |
| 07:55:49 | 21.520秒 | `ks-taqSDDP7VjuMPsLekwQw` |

同对象相邻07:54:01、07:56:35请求成功，均同协议、两图及相近文字量。不能将空包认定为参考图数量、导演失败、120秒超时或提示词触发过滤；也不能仅凭空包判断上游未生成还是中转丢失内容。脱敏响应、参考身份及对照在 `artifacts/illustrator-empty-reply-20260919/evidence.json`。代理未发任何验证/重试请求。

本地确定问题是将400字原始JSON追加到状态栏，盖住画面。现只显示中文分类、未自动重试说明和诊断ID末8位；完整脱敏响应仍在诊断中。**此改动没有解决服务端空包，也没有提高生图120秒期限。** 服务端进一步排查应按上述请求ID核对原始候选内容、结束原因、过滤信息和中转转换过程，不靠客户端盲目重试。

### 背景空与周报肖像化

- 百科《南境之主》精确匹配 `20260918T235833_333df508e0c64f809b157a2ce7dfc302`，缓存 `8e537a86b9b0b09b641d29fe1288f151_20260918235901957_e791e9`。导演正常，29.44秒成功；正文只选拼花地砖、平整墙垛、半圆拱券、漫射虚景，且原样发送。背景简略已在导演设计中，不能说环境提示没注入。实际随机51的通用禁止虚构条款与百科布景授权相冲突。证据 `artifacts/illustrator-empty-reply-20260919/portrait-background-nanjing-235833.json`。
- 周报《荒原围城纪》缓存 `HccAX3011oX4/weekly_report/261f67034a9e546706e643e967731a36_20260919001113450_48204f.json`，主题是“执政官卢孔勒马凝视开撒尔要塞”，正文明确“一人一骑占据画面主导”。肖像倾向也来自导演正文。该旧图DiagnosticId为空：周报Scope此前未设置类别，已修为weekly_report，后续可记录真实链路。
- 野外 `Conv_looter` 最新若干诊断（如 `20260919T000528_fbe4be6515154291916c59f17e2b2376`）约6ms即报无已加载场景，尚未发导演请求。原版MapConversationMission实现ICampaignMission而非Mission，旧入口只接受MissionScreen，错误拒绝了正常地图对话。

## 本轮实现

### 百科：保留事实，放开艺术设计

导演可围绕人物主题自主选择场所、陈设、叙事瞬间、动作、镜头和氛围；静态与动态均可，不默认空墙、拱廊、正面站姿。场景要有用途或自然环境特征、具体构造、材质和使用痕迹、相连空间；不靠固定物件清单或豪华等级衡量丰富度。油画概括笔触仍保留环境结构和材质层次。

随机>0与Chat末尾规范认可明确的艺术布景，允许在导演主题下补充一致的细节；随机0仍不追加随机段。单人最终规范明确背景为艺术设计。身份、装备、明确事件事实、无背盾与百科无武器/盾旗/坐骑保持，未恢复600–900字或半数比例。

### 城镇与野外：按实际渲染来源分流

- **Mission实景**：保留玩家附近30米独立静态网格快照、前后双镜头，以及可用的当前画面校验。包括已进入Mission的其他场景，不用Settlement是否为空误判。
- **大地图野外对话**：根据MapScreen的MapConversationView、ConversationMission和ConversationTableau进入独立管线，只读当前呈现画面，复用被动截图遮罩和标准颜色路径；不读取私有Scene/Camera、不操作原生缓存、不新建共享副视图、不隐藏UI或旋转玩家相机。明确单视角对话布景，不冒充30米几何重建、全景或实测战场。
- 野外截图不可用时返回零张环境参考，附导演说明、UI状态和 `coverage=unavailable` 诊断，以已有环境事实和人物参考继续。灰色遮罩处是未知区域，展示人物特写位置不等于双方真实距离/高低。独立 `MapConversationScene` 类型在导演、Chat、Edits路由保持这些语义，不再追加Mission人物站位锁定说明。
- 捕获原screen/会话/tableau身份，在导演前、生图前、响应后及发布/设默认前于主线程复核。退出或换人后晚结果不能发布。阶段检查不会瞬时中止已经发出的HTTP，不能承诺已发请求一定不再计费。

原版坐标：1.4 `MapConversationView.cs:12–69`公开MapConversationMission及ConversationTableau；`GauntletMapConversationView.cs:60–98`在切换/结束时重建会话；`MapConversationTableau.cs:245–267`使用缓存场景及私有相机。1.3同形公开owner入口已核对；缓存管理器GetCached并非只读查询，本轮未调用它。

### 周报：事件主导的独立导演与参考策略

- 独立 `WeeklyReportSystemPrompt`，不接收百科或实时现场的导演总则。一次导演请求从完整事件证据选一个核心事件/瞬间，保持参与方、地点关联、计划/否定/结果对应，不跨报道拼接。
- 事件行动、参与方互动与相关环境构成主体，不能以领主展示或骑马肖像替代事件；不强制广角、群像或固定人数。允许符合事件与时代的非具名艺术再现细节，不借玩家当前地点/时间冒充历史现场，不编造特定参与者、伤亡或胜负。
- 已识别英雄和地点是候选资料，附原文证据句；不因其最早出现就强制成为所选事件主角/发生地。`EventCharacter` / `EventEmblem`独立用途进入Chat与Edits，避免公共标签再称其“核心人物”。未选中参考人物时模型按事件选择是否入画；本轮未实现根据结构化人物选择自动过滤参考图片。
- 周报独立重绘、旧画历史说明、最终事件契约及本地回退；修正旧历史中的现场“不得添加道具”与事件艺术环境冲突。Scope改为weekly_report，补齐后续诊断。

### 人物离屏立绘保留

会话（城镇和野外共用人物提取）依然为玩家与对话对象各导出全身+头肩，普通NPC走CharacterObject路径；周报开启离屏时仍导出一位已识别候选英雄的全身+头肩。头肩失败按既有逻辑保留全身。导演识图、生图参考开关分别生效，未把立绘直接作为最终姿势模板。

## 核实源码坐标

均对应 `3a73006a`，路径前缀 `extensions/AnimusForge.Illustrator/src/`：

| 文件与行号 | 符号与责任 |
| --- | --- |
| `Core/UniversalOpenAiImageClient.cs:699–731` | `DescribeMissingImageResponse` / `FormatMissingImageError`：短错误+诊断后缀，完整响应仍先记录。 |
| `Core/VisualDirectorEngine.cs:66–105,652` | 百科创作规则、独立 `WeeklyReportSystemPrompt` 及 `BuildDirectorPayload` 按模式选择。 |
| `Core/VisualDirectorEngine.cs:231–244,334–345,778` | 正常/降级最终契约路由与周报独立本地正文。 |
| `Core/VisualFidelityRules.cs:18–24,49–74` | 自然或动态体态、地图/事件参考用途、独立周报最终规范。 |
| `Core/IllustrationStylePresets.cs:27–38` | 油画长短版均保留环境结构与叙事细节。 |
| `Core/IllustrationDirection.cs:70–90` | 周报旧画历史独立尾句，不继承现场道具禁令。 |
| `Core/IllustrationReferenceRouting.cs:9–46` | 人物全身/头肩、候选事件身份类型、地图环境保留覆盖语义。 |
| `Core/UniversalOpenAiImageClient.cs:317,389,480–519,539–545,570` | 随机艺术布景、Edits用途、Chat独立地图/事件标签和末尾呈现规范。 |
| `Engine/MapConversationSceneCapture.cs:19–65,78–138` | 会话owner捕获/复核、独立单视角采集和缺图诊断。 |
| `Engine/SceneReferenceCapture.cs:45–63,80–91,187–191` | 实际owner分流，Mission原双镜头链及路由诊断。 |
| `UI/Overlays/IllustrationCardPopup.cs:377–449` | 会话管线接线、双方离屏参考、发送及发布前身份检查。 |
| `Context/WeeklyReportContextExtractor.cs:60–124,169–185,252–259,406–416` | 事件证据、人物/地点来源句、独立开放艺术指导。 |
| `UI/Patches/WeeklyReportPopupIllustrationPatch.cs:257,306–359,402–406` | 周报诊断类别、人物/纹章参考、独立重绘。 |

## 验证、部署、边界与回滚

- 真实日志/缓存/响应取证及分工只读复核，`git diff --check`通过。最终API1.3/1.4 Release均0警告/0错误，日志及产物在 `artifacts/illustrator-separated-pipelines-20260919/`。遵照用户要求没有运行离线测试或审计脚本，旧测试数字不沿用。
- 原 `tools/deploy_illustrator.ps1` 选择已安装v1.4.8的API1.4实现，于08:30:49部署独立模块。DLL/PDB、清单及五个prefab共8文件哈希一致；核对时未发现游戏进程，下一次启动加载新版。
- 部署SHA256 `C5653D341A1290C423DE6845820719245F9E513E23F6B5A4CE52ED202CA8C908`，MVID `da2ba76c-3ac2-4e28-9630-70e868c4be5b`；同目录 `deploy.txt` / `deploy-manifest.json`。
- 新增逻辑在打开周报/点击生成/请求阶段运行：周报既有一次人物地点识别，新增两次有界证据句匹配；野外只做一次屏幕复制/编码，无常驻渲染、全场景扫描或新增原生复制；未增加模型调用次数。实际帧率/截图耗时尚未实测。
- 未验证新野外截图可用率、会话退出/换人、周报事件成图、百科多样性与最终外观保真；源码规则不能代替模型成图验收。空回复上游根因仍未解决。周报既有缓存键仍由报头/副题构成，未在本轮改缓存身份迁移；相同报头副题、不同正文的历史复用风险仍待另行处理。
- 主模组、原版DLL、用户设置/缓存、其他作者改动保持；未推送、未操作真实存档、未由代理付费生成。

回滚源码使用定向revert `3a73006a`，如连短错误显示一起回滚再revert `2a565c51`；不hard reset。部署备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-083049` 保存前一版 `d652694a` 产物，恢复模块后须重新启动游戏。旧图片不会被提示词修改自动重画。
