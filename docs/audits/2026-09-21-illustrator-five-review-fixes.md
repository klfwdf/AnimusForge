# Illustrator 五项审查修复

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。检查点 `f8a6e6a`，源码提交 `ceaa5e11e5e4c537bd8ef11712f4ec122e1fc7ea`。仅修用户指定的五项；其他作者的主模组、DialogueUI、工具及未跟踪文件不纳入提交。本轮未部署、未推送。

## 修复与源码位置

以下行号对应上述源码提交，路径以 `extensions/AnimusForge.Illustrator/src/` 为前缀。

| 问题 | 修复与核实位置 |
| --- | --- |
| 阶段耗时被请求累计时间覆盖 | `Core/GenerationDiagnostics.cs:320–326` 的 `AddEvent` 保留调用者的 `elapsedMs`，累计时间独立写 `requestElapsedMs`；元数据裁剪保留二者（343–366）。schema升为2，顶层elapsedMs仍是请求总时长；旧记录不会被修改。 |
| 打开已有缓存卡片仍全场枚举物体 | `Context/EnvironmentVisualExtractor.cs:688–758` 的 `ProbeLiveScene` 移除ActiveMissionObjects及Scene.GetEntities物体扫描，保留场景、天气、附近角色。`Engine/PanoramaSceneSnapshot.cs:45–75,514–533` 在已有InspectOne中复用已读的可见性、名称、全局帧收集24米内最近20种标识；不增加native读取。 |
| 延迟采集的物体事实需要继续到达导演 | `Engine/MapConversationSceneCapture.cs:68–77` 的 `ConversationSceneReferenceCapture` 携带请求内托管字符串；`Engine/SceneReferenceCapture.cs:60,154–156` 从本次snapshot返回；`UI/Overlays/IllustrationCardPopup.cs:475–476` 接入HardFacts。地图会话默认空，不伪造Mission物体事实。 |
| 同族纹章去重丢失玩家归属 | `UI/Overlays/IllustrationCardPopup.cs:324–348` 的 `EmblemSpec/AddEmblemSpec` 按代码去重同时合并双方标记；`503–515` 对双方实际载体资格取OR。仍只导出一次，标签说明双方共享，不能据归属给无载体者增加纹章。 |
| 正常撑桌被当违规姿势 | `Core/VisualDirectorEngine.cs:387–395` 去除手撑桌、双手撑桌、扭转躯干等动作关键词拒绝；保留百科举旗检查。行动、手部、视线和真实支撑仍由已有导演规则约束，不新增付费请求。 |
| 普通日志输出带认证信息的URL | 新 `Core/SensitiveLogText.cs:12–32` 为日志剔除URL用户信息、查询和片段，遮蔽显式密钥及Bearer；`Core/UniversalOpenAiImageClient.cs:153,202–220,408,634–645,875,991–994` 接入并移除普通日志的原始服务端错误/异常正文。`Core/VisualDirectorEngine.cs:449,465,475` 三处配置日志也脱敏；诊断共用URL处理（GenerationDiagnostics:329–341）。真实HTTP地址、认证头和重试判定继续使用原值，不依赖诊断scope存在。 |

## 性能和责任边界

物体名称随正式生成或用户手动独立试采的原分帧遍历收集，无定时任务、无第二次全场枚举、无长期native缓存。每次采集最多20个名称、各160字符，满额比较最多20项，完成后后台排序一次；没有玩家中心则不输出“玩家附近”标识。原30米AABB几何选择、64工作项/8重操作/4ms软预算、25秒总预算不变。物体名称是识别线索，不证明持握、空间关系或全部陈设已覆盖。

不修改颜色、GPU生命周期、场景渲染或全景透视规则。阶段计时修复使后续诊断可信，不意味着缩短模型服务耗时。取消/超时仍丢弃未完成的采集，不把残缺列表单独当成功场景发送。

## 验证

- API1.3及API1.4 Release均通过，0警告、0错误；使用现有项目参数，分别隔离OutputPath及BaseIntermediateOutputPath，未修改编译/覆盖脚本。
- 源码复核：缓存开卡到ProbeLiveScene、分帧收集到导演HardFacts、同族双方载体资格、正常动作拒绝路径、无诊断scope时的URL日志、阶段计时及元数据裁剪。所改模块 `git diff --check` 通过。
- 按用户要求未运行离线测试或审计脚本，未调用模型；编译和源码复核不代表原生渲染或最终图像验收。
- 构建日志及DLL：`artifacts/illustrator-five-fixes-20260921/{build-1.3.txt,build-1.4.txt,1.3/,1.4/}`。
- API1.3 DLL SHA256：`AC051C0ED137656A040944421D791A9CC762DA8C5D3A092DCD7F0D25CA105B93`。
- API1.4 DLL SHA256：`8123669278FCC2863DE9AFB347BB4E14EE17CAD9D974DEA35ADBBEBC7773B36A`。

## 部署后的游戏验证清单（本轮尚未部署）

1. 先打开已有缓存的城镇插画卡，观察开卡响应；独立场景试采检查家具、墙顶、人物不闪及取消/重开。无缓存的卡片可能自动开始付费生成，不把它当免费试采入口。
2. 正式会话生成后查看导演请求：应含本次24米内的物体标识；野外对话不能混入上一Mission的标识。30米几何覆盖和实际帧率仍需观察。
3. 同族双方会话，NPC没有纹章载体而玩家有：参考清单应只有一份该代码的纹章，同时标明双方归属；生图实际请求不能漏掉这份图。再看模型是否正确将纹章画在有依据的载体上。
4. 百科导演给出有实际支撑的撑桌/转身构图时，不应仅因该动作词而本地回退；最终肢体自然程度仍需看生成图。
5. 新trace的schema应为2；scene_snapshot_batches中elapsedMs约等于workMs加betweenBatchesMs，requestElapsedMs记录该事件距请求开始的时间。常规日志不应出现URL认证参数、userinfo或原始服务端错误；不为验证而粘贴真实密钥。

回滚源码用针对 `ceaa5e11` 的定向逆提交；不要hard reset或改写其他作者历史。本轮没有覆盖游戏文件，游戏侧仍是此前部署版。
