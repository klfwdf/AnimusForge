# AF 总 HANDOFF — 本地目标模式续作（2026-09-12）

## 预制体六镜头全景部署（2026-09-19 05:44:52）

生产/测试 `b63c10f8`，检查点 `c6da95b`。用户实机证明 `1c92decf` 共享Mission副视图产出重复角度、破面和蓝色原始参考；前轮离线通过不能算实机通过。现改为实际静态预制体的独立冻结副本，6个512方形相机顺序共用RT，由引擎PaintNeeded调度，后台投影成2048×1024全景；原生场景导出仅在合成入口做一次通道适配。附加真实画面只作颜色/人物关系校验，不以单截图替代全景。人物导出控件禁止屏幕Draw。

性能限额：8次复制/批、4ms软预算、根/节点/副本和25秒总预算；实际native调用不可抢占。普通第三方静态实体使用实际实例，不按名称重搭；terrain、水面、真实骨骼和完整实时光照明确未覆盖。双API0警告/0错误，每版14组759 PASS/0 FAIL；部署副本同样759/0，8文件哈希一致。部署SHA256 `5E381C8910F16A5A47F5A9933CEA1CC771490E9229F08D70A444C5283D38429A`，MVID `0f61f17a-693d-48c3-adc9-c3be45ebfba1`。备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-054452` 为旧问题版，不能标稳定版。新GPU六面、闪烁、帧率、第三方及成图仍待实机验收，未付费调用/推送。证据、源码坐标和回滚见 [预制体全景报告](docs/audits/2026-09-19-illustrator-prefab-panorama.md)。

## 生图五项优化与部署（2026-09-19 04:27:38）

生产/测试 `1c92decf`，检查点 `6ccc335f`：限量脱敏诊断、原生全身+头肩参考、百科无载体提前跳过纹章、导演截断/降级状态、缓存索引及画廊后台准备均已接入。追加将一张真实现场图送生图端，多向图仍供导演识图；独立离屏相机不接管玩家镜头。用户本轮跳镜反馈时核实游戏仍是9月18日旧版，本轮已实际覆盖到独立模块。

双API构建0警告/0错误，每版九组离线审计595 PASS/0 FAIL。实际部署DLL同字节副本也595/0，8文件哈希一致；部署SHA256 `152DBB6A49DCF568FD1E26BE7F8669290A40CB2FA14842F6373A1DD2954D8712`，MVID `70fac8d5-a1cd-4139-a423-89102346f0c2`。回滚备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260919-042738`。未推送、未启动游戏或付费调用；原生头肩/离屏场景、GPU退出与帧率、实际成图及UI布局待实机验收。源码坐标、诊断容量、验证边界与回滚详见 [本轮优化部署报告](docs/audits/2026-09-19-illustrator-optimization-deployment.md)。下方“未部署”为前轮历史状态。

## 生图全量审查、画风与无可见转镜采集（2026-09-19）

当前工作区仍为 `F:/AnimusForge-main` / `codex/af-main-refactor-continuation-20260831`。检查点 `cd1b1cfa` 保存原有生图改动；生产与测试 `d7061300`，场景事实复查收尾 `e337754a`。已统一古典油画导演长版/生图短版，移除须发和普通 NPC 外观伪事实，修正物种误判；场景采集改为独立相机+SceneView借用 Mission Scene，玩家相机与界面不修改，并接入 Mission 结束清理。全量审查 33 C# / 5 GUI 及项目清单，修复精确 Edits、实际 Chat 提示词记录、重复请求/超时、事件地点与围城误判、图片拉伸和画廊主题等问题；无 Location 不再判定平地谈判。

双 API Release 0 警告/0 错误，两份 DLL 各 **299 PASS / 0 FAIL**（PromptRouting 144、ClientEndpoint 108、ModuleReview 33、SceneCapture 14）。**未部署、未推送、未付费调用；游戏仍是下方 9 月 18 日 16:36 版本**。GPU 离屏导出与取消/切场景、帧率、实际 UI 布局及发型/画风成图未实机验收。已核实源码坐标、证据、性能与回滚见 [全量审查报告](docs/audits/2026-09-19-illustrator-full-review.md)。回滚按逆序定向 revert `e337754a`、`d7061300`，保留检查点和其他作者改动。

## 导演行动、命名与四向场景参考（2026-09-18 后续）

本地生产 `987fa696`（精简负面词，导演先行动后姿态，标题/主题单独显示与缓存，最近三张动作参考）和 `192098f2`（完整当前视图＋最多四向环境参考，仅发导演，现有 Mission 相机采集及恢复）。双 API 构建通过，最终两份 DLL 各 131 PASS/0 FAIL；缓存、HTTP 与相机数学离线验证，GPU/真实转镜恢复/成图/界面尚未验收。**已于 2026-09-18 16:36:53 按用户授权部署，未推送**：原脚本识别游戏 v1.4.8，API 1.4 Release 构建 0 警告/0 错误，8 文件哈希一致，部署 DLL 同字节副本 131 PASS/0 FAIL。DLL SHA256 `4939DEBD704A8B775302773C5BD634A59218ED50E948CE5D8383E3C23EC70371`；旧版备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260918-163653`。intent `ac0a33c`，渲染器修改前检查点 `ec38ea99`。核实坐标、适用边界、性能和定向回滚见 [本轮报告](docs/audits/2026-09-18-illustrator-action-title-panorama.md)。

## 生图部署更新（2026-09-18 15:48:59）

用户授权后，已通过原独立生图部署脚本将含 `b36fe53b` 的当前工作区构建部署到游戏 `v1.4.8` 的 `Modules/AnimusForge_Illustrator`。构建 0 warning/error，8 文件哈希一致，实际部署 DLL 96 PASS/0 FAIL；DLL SHA256 `8A841CFED9EC939DDE8C84704DCEB2F69DD37EA65A977A6A21C524CC2B69AC79`。旧版备份 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260918-154859`。未推送、未启动游戏，真实出图仍待验收；详见 [外观保真报告部署节](docs/audits/2026-09-18-illustrator-appearance-fidelity.md)。下方“未部署”为此前修复完成时状态。

## 会话外观与现场时段保真（2026-09-18）

`b36fe53b` 修复按装备名称推断冠冕造型、遮发遮须误推全脸遮覆、会话战役时间冒充现场日夜；导演须转写可见装备特征，Chat/Edits 共用参考外观优先规则。游戏隐藏须发标记和原生装备渲染保持。双 API 构建各 0 warning/error，实际 DLL 各 96 PASS/0 FAIL；未付费生图、未部署、未推送，尚无新成图/原生渲染验收。检查点 `5eca84eb`，回滚只定向 revert `b36fe53b`。核实源码坐标、日志证据、性能与边界见 [外观保真报告](docs/audits/2026-09-18-illustrator-appearance-fidelity.md)。

## 生图自然体态与 Edits 整幅重绘（2026-09-18 后续）

用户反馈 Edits 5 张中 3 张保留立绘站姿、2 张仅稍作变化，并要求提高导演下限。`6d5cb827` 移除强制反站姿/每次换姿势，给导演增加简单体态、稳定支撑与统一透视光照的质量要求；Chat/Edits 均附加整幅重绘说明，Edits 参考图明确分角色且不再自动附加全透明 mask。Edits 缓存提示词与实际 multipart 文本一致。双 API 子模块构建 0 warning / 0 error，两份 DLL 各 71 PASS / 0 FAIL。未付费生图、未部署、未推送，不能宣称已改善实际成图比例；同模型同参考图对照仍待实测。检查点 `b7fc2409`，只定向 revert `6d5cb827` 回滚本轮修复。代码坐标及限制见[本轮记录末节](docs/audits/2026-09-18-illustrator-prompt-routing-fixes.md#自然体态与-edits-重绘后续修正)。

## 生图审查修复与导演提示词分工（2026-09-18）

`ec0d13fb` 修复参考图误分类、周报被强制双人会面、百科回退补造盔甲、围城室内分支误判，并将会话硬事实与创作指导分离、移除生图端二次指定构图。正常链路维持“元指令＋硬事实＋参考图→导演→生图描述＋参考图”；失败时仍保留中性本地回退。双 API 子模块构建通过，两份 DLL 各 60 项离线回归通过。未部署/推送，实际出图与游戏未验收。当前构建还包含用户原有未提交更改，不能将单个修复提交视为完整发布版本。坐标、边界、日志与定向回滚见[本轮报告](docs/audits/2026-09-18-illustrator-prompt-routing-fixes.md)。

## 当前生图部署：原生导出初始化修正（2026-09-16 06:21）

用户要求继续修复后，生产 `77caa9f6` 已部署。只读反汇编同一原生故障 RVA `0x283860`：最终图像写出后函数还导出 final/depth/shadow，故障位于阴影数据访问；对应 passes 文件与崩溃时间相符。生图专用 Banner 控件现于原版 paint 后初始化完整渲染/shadow 路径，至少两次准备 callback 后才允许 save；完整 PNG 继续直接进入参考图链路。没有修改游戏 DLL。

双构建通过，通用 180 / 0，两目标专项各 39 / 0；最终部署 DLL 178 / 0、39 / 0，hash 一致。**最终修正尚未实机验收，不宣称已消除崩溃**。源码和部署再次对齐，回滚备份 `20260916-062110`（原生替换前版）。已核实符号/行号/源码提交、反汇编证据、测试与限制见 [导出初始化报告](docs/audits/2026-09-16-illustrator-native-export-initialization.md)。下方撤回状态为历史。

## 当前部署已撤回：原生纹章仍崩溃（2026-09-16 06:04 之后）

用户重启后再次崩溃，`rgl_log_32720.txt` 停于纹章 save 请求，没有新增退休日志；Windows 再次记录 `TaleWorlds.Native.dll / 0xc0000005 / +0x283860`。`c55334ed` 未修复崩溃，之前清理路径假设没有得到实机支持。没有转储，不能定位原生内部调用。

已将游戏 `Modules/AnimusForge_Illustrator` 的 DLL/PDB、清单和五个 prefab 共 8 文件恢复到 `artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260916-054705`，逐文件 hash 一致；恢复 DLL SHA256 为 `2DAC0572BD0A79EB6E55DA530D75A37EF3009450012F6B856A64945D7A939896`。源码仍保留失败的原生方案及证据，**当前源码不是当前游戏部署；不能把 c55334ed 当作可交付修复再次覆盖**。旧纹章准确性问题未解决，回退后尚未实机验收。完整状态见 [崩溃跟进报告末节](docs/audits/2026-09-16-illustrator-native-crash-followup.md)。

## 当前生图崩溃跟进（2026-09-16 05:59）

生产最新 `c55334ed`（含 `0127816d` 场景延迟清理、`8a52d1b` 诊断日志），已按用户授权部署到游戏 1.4.8。`cf237202` 实机原生访问冲突，首次修正版又因程序集名称解析失败跳过纹章；用户截图背景旗帜仍错，不能当作成功验收。现改用 Gauntlet 实际注册类型、引擎延迟场景清理，双构建 0 警告/0 错误，最终部署 DLL 通用 178 / 0、专项 36 / 0。最终版本实机仍待重启复测，原生调用栈缺失，未宣称崩溃根因完全确认。责任坐标、真实输出及回滚边界见 [崩溃跟进报告](docs/audits/2026-09-16-illustrator-native-crash-followup.md)。

## 最新纹章核心替换：离线管线通过，实机待验收（2026-09-16）

用户确认“做”后，生产/测试提交 `cf237202`，检查点 `75d116aa`。移除手工图集着色/描边/旋转，改用原版 BannerTableau 渲染完整旗面；导出控件不提交屏幕 Draw，缓存受存档代际/取消与容量约束。双 API 子模块 Release 构建均 0 警告/0 错误，通用 180 checks / 0 failures，两份目标 DLL 专项各 32 / 0。专项使用模拟 GPU 输出，不能代替真实纹章颜色、方向、多色背景、取消清理、人物共用舞台和最终生图验收。纯色/低覆盖图案仍可能被保守空图检测省略。未部署/推送；其他作者工作保留。

已核实源码行号、符号、源码提交、性能、证据及未覆盖责任见 [原生替换报告](docs/audits/2026-09-16-illustrator-native-emblem-replacement.md) 与 [生图交接第十三轮](docs/handoffs/2026-09-14-animusforge-illustrator-handoff.md)。生产回滚只定向 revert `cf237202`。

## 历史纹章 CPU 离线验收：未通过（2026-09-16）

工具 `5de6e6c9`，生产源码仍为 `90a61c9f`。1.3 / 1.4 目标 DLL 各 30 checks / 7 failures：大面积掩码反相、配色契约矛盾、描边无效、90°/270°方向相反。已保留实测结果与对照图，建议重做纹章渲染核心。未改生产渲染、未部署/推送。源码定位与边界见 [离线验收报告](docs/audits/2026-09-16-illustrator-emblem-offline-audit.md)，此前通用回归通过不等于纹章保真通过。

## 生图未闭环问题审计（2026-09-16）

源码/测试 `90a61c9f`，检查点 `aedb8f0`：真实日志确认两次空 completion；旧纹章参考图实际近乎空白。已修复空图误判、原生 PNG 文件读取、现场模板污染、残留盾牌提示和响应解析/错误降级。双 API 构建与 185 检查通过，真实旧空图复验被拒绝；新原生导出及实际出图未验收，双色背景仍省略，未部署/推送。定位、证据、性能与未覆盖范围见 [生图交接第十一轮](docs/handoffs/2026-09-14-animusforge-illustrator-handoff.md)。可定向 revert `90a61c9f`。

## 生图子模块最新设置修正（2026-09-16）

生产/测试提交 `2f978705`：设置改为“随机”0–100，默认 0，不追加随机提示词，正值增强艺术表现变化。双 API 构建通过，160 checks / 0 failures；未部署、未推送、未实机验收。源码位置与边界见 [生图交接第十轮](docs/handoffs/2026-09-14-animusforge-illustrator-handoff.md)。检查点 `bfc1aea`，可定向 revert 生产提交。

## 当前续作入口

用户在全项目分析后明确要求“开目标模式做吧”，已恢复当前 `F:\AnimusForge-main`、`codex/af-main-refactor-continuation-20260831` 上的本地实现和验证。起点 `bd2ed35f`；执行顺序、边界及状态见 [本轮台账](docs/phase8/local-goal-continuation-20260912.md)。先重新建立当前源码的本机构建基线，再按独立切片推进压缩输入快照、Native、TTS 和 Courier。原有 75 个未跟踪图片/预览文件保留。没有本轮 push、游戏覆盖、真实存档操作或恢复定时自动化授权。


## 当前已验证小片：Native 未压缩历史（生产/测试 `54e07882`）

`ShoutBehavior.cs:20270-20288` 的 `SubmitNativeConversationTextInternalAsync` 未压缩历史入口改为原主线程队列中校验 admission 后调用；失效/超时走原暂存输入回滚。原 Hero/普通人物 ID 解析、AFEF、当前会话排除与主动开场去重不变。编辑前 intent 为 `75a692ac`，可定向 revert `54e07882` 并复验。

验证：69 PASS，真实旧源码失败 13，三个变异失败 4/4/3；pending-history 111、history 27、preparation 589、scheduler 132、admission 44、ports 308/3 mutation；六构建/两 Stage、四 DLL 532、存档身份 146/146 和 36/36 均 PASS。详见 [验证记录](docs/audits/2026-09-12-native-uncompressed-history-verification.md)。代码图已更新到 `54e07882`。

只覆盖这一个入口。Native 的角色上下文、persona、共享规则/lore、后处理 prepare/complete 仍需后续；TTS worker 查询、Courier 也未完成。LIVE/provider/旧存档/实机耗时均未验证。继续本地目标，不推送或覆盖游戏。

## 当前已验证切片：压缩输入与来源拒收（生产/测试 `53ddb7d4`）

`OFFLINE_COMPLETE / LIVE_SAVE_PENDING`。当前分支继续本地推进；检查点 `15c58d59`，生产/测试/架构证据提交 `53ddb7d4`。本轮原有 75 个未跟踪文件保持原样。

- `MyBehavior.MemorySummaryInputs.cs:121-231`：`CaptureDailyMemorySummaryInput` / `CaptureMajorActionSummaryInput` / `CaptureMemoryOverviewInput` 在主线程冻结三个任务的私有来源副本、prompt 和解析材料；`76-119` 的 `MemorySummarySourceStamp` / `IsMemorySummaryInputCurrent` 校验任务、来源和已有摘要的对象身份及完整内容。
- `PlayerNotorietyBehavior.MemorySummarySnapshot.cs:10-33`：`CaptureMemorySummaryHistoryRenderer` 冻结实际姓名、公开称呼和文化年龄别名；后台只替换字符串。
- `MyBehavior.cs:4960-5203`：`ProcessMemorySummaryQueueAsync` / `RunDailySummaryQueueItemsAsync` / `ExecuteDailySummaryQueueItemAsync` 传递原 save generation；首段准备与所有成功/失败接受走原主线程边界，来源改变的结果不能消费或标失败。
- `MyBehavior.cs:5239-5392`：三个 Execute 仅消费独立输入进行网络/解析，重试前校验来源；`5475-5525`、`5644-5679`、`5838-5898` 的三个 parser 使用冻结的名字/cursor/metadata。
- 原 prompt builders、正式 Apply/Mark、玩法/三渠道语义、存档键、公开 ABI 和默认入口未改；原 60 秒波次、三次重试及 EngineTick 两个动作上限保留。成本按当前任务来源规模增长，未测实机峰值及单动作耗时。

验证：net8/net472 各 137；六个故障变异拒绝；history 852、Native 27、failure UI 85、main-thread 17、ports 308/3 mutation、recovery/weekly 合同 PASS；持久身份 146/146 与 36/36；六构建/两 Stage 和实际四 DLL 532 元数据断言 PASS。游戏对象/网络/写入副作用仍为 fixture，LIVE/真实 provider/旧存档未运行。

详细说明：[输入边界](docs/architecture/af-memory-summary-input-boundary.md)；[验证记录](docs/audits/2026-09-12-memory-summary-input-verification.md)；[本轮台账](docs/phase8/local-goal-continuation-20260912.md)。当前代码图已按 `53ddb7d4` 更新。回滚使用定向 revert `53ddb7d4` 并复验，禁止 reset/改写历史。

接下来继续 Native 剩余实时读取，再做 TTS 与 Courier。上述边界不代表整个 memory owner 或阶段八已完成。

以下暂停／转手文字是前一机器的历史记录，不再代表本轮开发暂停；其中一次性 push 授权也不沿用。

## 历史暂停／转手记录

> **当前指令：停止继续重构，不得启动下一切片。** 专题转手文档见 [`docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md`](docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md)。交接写作前 `HEAD=f948f419`、detached、工作树干净；最后生产源码为 `9040d184`，相对目标远端 `e40c92d7` 为 3 ahead / 0 behind。自动化 `af` 已只读确认 `PAUSED`。用户只额外授权 docs-only 交接提交后、fetch/祖先核验通过时，精确 fast-forward push 当前历史到 `origin/codex/af-main-refactor-continuation-20260831`；不授权 force、其他 refspec、部署或继续开发。之后只有新的明确用户授权才可按专题文档的安全恢复步骤继续；不得据下方历史“下一切片/自动化继续”文字自行恢复。

## 最新本地切片：压缩记忆 post-await 主线程提交（生产/测试 `9040d184`）

状态：`OFFLINE_COMPLETE / LIVE_SAVE_PENDING`。活动 worktree 为当前仓库；checkout 保持 detached，来源/预期远端目标仍是 `origin/codex/af-main-refactor-continuation-20260831` 的 `e40c92d7`。本轮在来源上创建 intent/checkpoint `909550d4` 和聚焦代码/测试提交 `9040d184`；没有 switch、pull、merge、rebase、reset、cherry-pick、stash、push、部署或覆盖游戏。

- `MyBehavior.MemorySummaryMainThread.cs:44-73` 的 `RunMemorySummaryMainThreadAsync`：主线程直达；后台发布前后双检 `MyBehavior.Instance` 与 save generation，避免 reset 后把等待者挂在不再 tick 的旧 owner。
- `MyBehavior.MemorySummaryMainThread.cs:75-110` 的 `TryApplyMemorySummaryMainThreadAction` / `ProcessMemorySummaryMainThreadActions`：只在物理主线程接受，并复核当前 Campaign behavior；EngineTick 每次最多处理 2 个。
- `MyBehavior.cs:4957-5129` 的 `ProcessMemorySummaryQueueAsync`：三类 post-await Apply/Mark、队列清理、玩家提示和 `_memorySummaryProcessing` 释放经新边界；`MyBehavior.cs:20311-20333` 的 `OnEngineTick` 是消费者。
- `MyBehavior.cs:2415-2419` / `48278-48282`：loaded-save 瞬态重置和当前存档清理退休未开始工作。没有新增持久字段。
- 原 prompt、三渠道 role/AFEF/动作、重试/RPM/成功失败文字、默认入口和公开 ABI 不变；`Api.V1` 仍只读。政策/宴会/GCCZ 业务未改。
- 性能：低频日结 worker 每批等待接受，EngineTick 有 2 个动作硬上限；使用 ConcurrentQueue/CAS，不加热路径全量扫描、反射、锁等待或轮询。

验证：专用 17/17；精确旧源码 `e40c92d7` 和 generation/owner/unbounded-drain 三个 mutation 均按预期失败；HistorySnapshot 852、Native 27、MemoryFailureUi 85、memory recovery、weekly material、团队端口 308/3 mutation 均通过。相对来源的持久化身份为 SyncData 146/146、CampaignBehavior 36/36，单一 AnimusForge/Bootstrap 不变。最终 Debug/Release × 1.3/1.4/Bootstrap 六项构建和两套项目内 Stage 通过，四份实现 DLL 532 个元数据断言通过；仅有无法联网读取漏洞元数据的 `NU1900`，0 error。

详细边界：[架构说明](docs/architecture/af-memory-summary-mainthread-boundary.md)；[验证 MD](docs/audits/2026-09-12-memory-summary-mainthread-verification.md) / [JSON](docs/audits/2026-09-12-memory-summary-mainthread-verification.json)；[本轮进度](docs/phase8/memory-summary-mainthread-progress-20260912.md)；[范围图](docs/architecture/af-framework-code-scope.md) / [机器定位图](docs/architecture/af-framework-code-map.json)。

明确未覆盖：首次 await 前的调度快照和三个 Execute job 的 live prompt/目标准备仍可能在异步 continuation 读 owner/game；逐任务 source revision/fingerprint 未实现。真实游戏、provider、旧存档和读档晚返回均未验收，不能宣称完整 memory 线程安全或阶段八完成。

下一独立切片：只为 `ExecuteMemorySummaryJobAsync` / `ExecuteMajorActionSummaryJobAsync` / `ExecuteMemoryOverviewJobAsync` 在 Campaign 主线程捕获只读输入和精确 source fingerprint，后台只做 provider/解析，并在本轮接受边界拒绝来源已改变的结果。先建精确旧源码红例，不扩到 Courier、公共写 API 或默认切换。回滚只按用户指示定向 inverse/revert `9040d184`。

## 前序交付：框架 Skill / 代码位置 / 新旧范围

**GitHub 交付已确认：`38c003ab` 已推到原重构分支（此前为 `a58c2191`）。本记录随后单独提交；确切最新末端以远端 ref 为准。指定制作组简明 HANDOFF 未进入上传文件树或新增提交历史。**

用户本轮授权把框架和维护要求写成仓库 Skill 并推送专门重构分支，**指定 Native history 制作组简明版只留本地**。本轮不改游戏运行代码，不恢复自动化；下面历史段落中的“未推送/自动化继续”等仅代表当时状态。

- Skill：`.agents/skills/af-core-framework/SKILL.md`，根 `AGENTS.md` 已接入读取规则。允许批准的主体功能演进，不把当前算法、模块名单、只读 API 状态写成永久上限；稳定分层、公开兼容与唯一权威提交责任。
- 新的可上传交接：[框架 Skill GitHub HANDOFF](docs/handoffs/2026-09-11-framework-skill-github-handoff.md)，包含逐项代码路径、行号、符号和责任注释。
- 当前源码坐标：[范围图](docs/architecture/af-framework-code-scope.md) / [JSON](docs/architecture/af-framework-code-map.json)，核对源码 `8f1cd479`，25 个定位点；新接缝/混合 owner/仍运行旧入口/不处理业务分开标注，不搬动仍在用的旧源码。
- 本地干净交付分支：`codex/af-framework-skill-delivery-20260911` → 获准远端 `origin/codex/af-main-refactor-continuation-20260831`；旧远端基线 `a58c2191`。原本地同名来源分支只保留历史，不能直接推送其中含本地专用文档的提交。
- 发布校验/状态见 `docs/phase8/framework-skill-publish-progress-20260911.md`。运行代码仍为 `8f1cd479`；本轮为 Skill/文档/定位校验工具，未产生新游戏构建或实机证据。自动化仍 PAUSED，阶段 8 未 DONE。

### 主体关键位置（源码 8f1cd479）

- `Refactor/Modules/TeamModulePorts.cs:7-10` — `internal interface IPolicyModulePort`：政策 typed 接缝，业务归原 owner。
- `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` — `internal static class ModuleFrameworkRuntime`：装配与只读投影，不是第二套执行器。
- `Api/V1/AfApi.cs:13-16` — `public static class AfApi`：当前只读；其他提交/写能力未开放。
- `MyBehavior.HistoryPromptSnapshot.cs:35-38` — `internal static Func<string> CaptureHistoryContextWorkById`：召回用途投影，非全局记忆事务。
- `ShoutBehavior.cs:16180-16183` — `private static Func<string> CaptureNativeConversationPersistedHistoryWork`：原身份解析在主线程捕获，非全 Shout 重写。
- `MyBehavior.cs:28087-28090` — `public static string BuildHistoryContextForExternal(`：Scene/Courier 仍调用，共享兼容入口不能盲删。

## 上一轮状态：记忆快照完成，自动化暂停

**上一轮要求为“工作完成后暂停自动化，写两份 handoff”。Native 持久历史快照已验证并本地提交 `8f1cd479`，`af-7-8` 已设为 PAUSED 并回读确认；此刻只交接，不自动开始下一项。下面历史记录中的“自动化继续/下一轮”仅是当时状态，不构成恢复授权。整个阶段 8 未 DONE。**

上一轮的制作组简明版按新要求仅留本地，不作为 GitHub 交付文档或链接依赖；本轮可上传版本见顶部专题 HANDOFF。公共 Api.V1 仍只读。

## 最新续作：Native 持久历史输入快照（生产/测试 8f1cd479）

- 在原主线程队列核对 Native admission，捕获原 Hero/普通人物身份、owner、generation、总览、场景/日期/设置、召回查询和块/AFEF 投影；后台复用原召回/筛选/格式，结果使用前再验原 admission。
- 保留原最新块、两种筛选模式、候选/填充/顺序、Summary 与 AFEF、主动开场输入语义和 history-only 失败空串 fallback。少量历史跳过不需要的草稿查询构造，ONNX/API 不整段搬到主线程。
- 删除被替代的 `BuildNativeConversationPersistedHistoryContextForPrompt` 和原后台 identity/owner 接线。原 public 历史签名与 Scene/Courier 的共享默认入口仍有调用责任，未删除；存档身份/默认渠道/制作组业务均未改。
- 新 memory 852 / 120 组合、Native 27 检查；旧实现分别 305 / 12 个断言失败，候选通过；10 个新变异被 runtime 拒绝。既有 UI/Native/ports 及 26 个旧变异复验通过。最终源码的六项 Stage、16 组相关回归、实际四 DLL 532 元数据检查均通过；无证据的发布门禁仍拒绝。
- 技术边界：[记忆快照说明](docs/architecture/af-native-history-snapshot-boundary.md)；证据：[审计 MD](docs/audits/2026-09-11-native-history-snapshot-verification.md) / [JSON](docs/audits/2026-09-11-native-history-snapshot-verification.json)；台账：[本轮进度](docs/phase8/native-history-snapshot-progress-20260911.md)。原始日志在 `.tmp/native-history-snapshot-20260911/`，只留本地。
- 代码文件：`MyBehavior.HistoryPromptSnapshot.cs`、`MyBehavior.cs`、`ShoutBehavior.cs`；真实旧代码对照 `659bb998`，本轮检查点 `e1a09954`。回滚需经用户指示定向反转 `8f1cd479` 并复跑测试；不 reset、不覆盖用户草稿。

### 恢复后才做的工作

1. 先核对实际 HEAD、工作树和新用户指示；两份 2026-09-06 草稿仍有用户改动，不纳入自己的提交。不能仅因定时提示或历史 handoff 就恢复自动化。
2. 继续检查其他后台维护/压缩 writer；当前证明的是捕获后不再共享可变列表，**未证明与所有 writer 并发捕获的全局原子性**。此投影缺少本路径不读的字段，不能作为完整存档块写回。
3. 再沿 Native persona/规则/独立周报绑定/剩余游戏对象读取与 TTS 直接回调检查；随后推进 Courier 双向早期 prepare。Scene/Courier 本轮尚未接入该快照入口。
4. 实机核对英雄/普通人物、空/多历史、主动开场、换会话/读档晚返回、失败提示、AFEF 内容及主线程耗时，再考虑新公共提交/生命周期能力。已开始网络不可伪称取消，空历史 fallback 不可伪称严格读取成功。
5. 整个阶段 8 的真实 Host、旧存档、新外部 DLL 加载/升级与最终清理仍需独立验收；不因本轮 PASS 自动切默认路径、删所有旧 facade 或推送/部署。

## 前序续作：记忆失败提示（生产/测试 6f0bac67）

- 深层记忆审查发现 9 处失败出口可从后台直接弹 UI；已改为有界待提示，由原 EngineTick 消费，核对 owner / 实际 Campaign / 操作 generation / 展示 revision。旧确认、显示失败重入、日志失败不再干扰新提示。
- 原错误文字、按钮/暂停、召回/筛选/总结算法与重试策略保持。读档和现有数据清理的瞬态重置点只同步清理新提示；不执行或改变数据清理业务。
- 85 检查 / 7 变异；原 Native 589/132/44/46/88/184/111、ports 308/3、最终六项 Stage、16 组相关回归、四 DLL 532 元数据通过。存档绑定仅刷新两处 -14 行号，168 个身份不变。
- 简明版：`docs/handoffs/2026-09-11-memory-failure-ui-team-handoff.md`；技术说明：`docs/architecture/af-memory-failure-presentation-boundary.md`；审计：`docs/audits/2026-09-11-memory-failure-ui-verification.md`；台账：`docs/phase8/memory-failure-ui-progress-20260911.md`。
- 检查点 `88777e45`；未推送/部署/真实存档访问。Api.V1 仍只读，真实游戏/旧存档未验收，阶段 8 未完成。
- 下一项仍是记忆数据快照：原身份/owner、可变 blocks/drafts、总览、场景/日期和召回输入；保留原检索/格式，不整段主线程化。已确认召回不写块内 embedding，引擎缓存按原锁保留。本轮不是完整记忆线程安全；自动化继续。

## 前序续作：Native 初始场景准备（生产/测试 0306beba）

- NPC、文化/已有历史标记、挑衅规则、传唤/带路候选与规则排除表改在原 request_target_validation 消费中一次准备；先验证原 admission，删除被替代的后台读取片段。原 helper、参数/结果/顺序与默认业务链保留。
- private 准备包仍有既有 LocationCharacter/Location 引用，不是公共 immutable DTO；没有把持久历史召回/前处理 Task 整段搬主线程。
- 589 检查 / 5 变异；共用调度 132/7、准入 44/7、展示 46、动作 88、收尾 184、前置历史 111、ports 308/3；六项最终 Stage、16 组相关回归、四 DLL 532 元数据通过。原 7 个前置守卫仍为 6 + 1，未弱化门禁。
- 简明版：`docs/handoffs/2026-09-11-native-preparation-team-handoff.md`；技术说明：`docs/architecture/af-native-initial-preparation-boundary.md`；审计：`docs/audits/2026-09-11-native-preparation-verification.md`；台账：`docs/phase8/native-preparation-progress-20260911.md`。
- 检查点 `62468e7c`；未推送/部署/真实存档访问，Api.V1 仍只读。真实游戏/主线程耗时尚未验收，整个阶段 8 未完成。
- 下一项：沿实际持久历史链拆游戏/owner 数据读取、可变记忆集合与召回/选择，不删记忆、不整段主线程化；之后继续 persona/周报绑定、TTS、Courier prepare。自动化继续。

## 前序续作：共用主线程函数（生产/测试 5bf830f3）

- Native/Scene 共用调度改为 queued/claimed/retired CAS：未开始才可过期，已开始等真实结果；失败发布不遗留晚到工作，日志/错误消息格式化不改变结果。direct/queued 前处理格式异常一致，普通 fallback 兼容责任保留。
- 仅两个私有调度声明改变，25 个业务调用点保持原样；移除 bool timeout、wait 吞错和重复执行处理。未改制作组业务、存档键、默认路径或公共写 API。
- 132 检查 / 7 变异；Native 44/46/88/184/111、ports 308/3、六项最终 Stage、16 组相关回归、实际四 DLL 532 元数据通过；不是实机。
- 简明版：`docs/handoffs/2026-09-11-mainthread-function-team-handoff.md`；技术说明：`docs/architecture/af-mainthread-function-boundary.md`；审计：`docs/audits/2026-09-11-mainthread-function-verification.md`；台账：`docs/phase8/mainthread-function-progress-20260911.md`。
- 检查点 `84d7097b`。未推送/部署/真实存档访问；Api.V1 仍只读。普通 fallback 仍不能证明没有部分副作用，已开始的同步 owner 不能强行取消。
- 下一项：Native 更早 prepare（尤其后台 persisted history 与人设/规则构造的游戏读取）、TTS 直接回调，再处理 Courier prepare。现有 whole-host inverse 是本轮严格对照，后续其他 host 变更要补独立审查证据，不弱化断言。自动化继续。

## 前序续作：Native 前置历史（生产/测试 128e9842）

- 玩家显示名、tentative 输入、pending AFEF 与 Native 历史消息改在同一次主线程消费里准备；原 history key 只解析一次，私有 helper 默认行为不变。
- 五个拒绝分支与 action discard 共用固定 key + 原 owner/generation/会话/revision 的清理，改为只删 player/user，不误删事实或新存档重用序号。未开始队列超时明确失败，晚到不补做；原 Action core 未改。
- 111 检查 / 12 变异，原 184/15、88/9、44/7、46/6、ports 308/3，六项 Stage、四 DLL 532 元数据和 16 组相关回归通过；不是实机。
- 简明版：`docs/handoffs/2026-09-11-native-pending-history-team-handoff.md`；技术说明：`docs/architecture/af-native-pending-history-boundary.md`；审计：`docs/audits/2026-09-11-native-pending-history-verification.md`；台账：`docs/phase8/native-pending-history-progress-20260911.md`。
- 检查点 `1547460a`。未推送、未部署、未操作真实存档；公共 Api.V1 仍只读。
- 下一项：更早 Native 人设/规则/持久记忆 prepare，以及通用 main-thread func 的 bool timeout 问题；保持网络在后台，随后继续 TTS 直接回调、Courier prepare。不要把本段完成当成全 Native 或最终阶段 8 DONE。

## 前序续作：Native 记忆接受结果（生产/测试 18f48678）

- Native 已从 void 历史外壳接到一个支持 sceneSessionId 的 internal strict owner，检查运行期接受结果；原 public 六参接口保留 -1 loose 与 ABI。原 Action core、底层 Append/AFEF/数值未改。
- owner 缺失/false/失败不再按正常完成处理，提示记忆未确认，不重放动作或删除部分记录。必要关窗在记忆失败后也保留且仍绑定原会话；非持久 NPC/空 payload 不伪造写入请求。
- 184 检查 / 15 变异，原 88/9、44/7、46/6、ports 308/3，六项 Stage 和实际四 DLL 532 元数据通过。存档契约仅修正两处 -34 的源码行号，168 绑定身份不变，复验 PASS。
- 简明版：`docs/handoffs/2026-09-11-native-memory-acceptance-team-handoff.md`；技术说明沿用更新后的 `docs/architecture/af-native-completion-boundary.md`；审计：`docs/audits/2026-09-11-native-memory-acceptance-verification.md`；台账：`docs/phase8/native-memory-acceptance-progress-20260911.md`。
- 检查点 `c5de2186`。Applied 仅为运行期接受，不是磁盘/SyncData/跨动作事务或恢复 receipt；新 Api.V1 仍只读。未推送、未部署、未实机验收。
- 下一项：更早 Native prepare/失败 pending 清理，再做 TTS 直接回调、Courier prepare 和完整生命周期/恢复证据；不改制作组业务或直接开放新公共提交。

## 前序续作：Native 主线程收尾（生产/测试 d7ab9610）

- 动作后的历史派发、短期记录/显示标记和最终 TTS 改到同一次主线程消费；不再先检查目标再返回后台写游戏状态。原 Action core 未改。
- 动作前捕获 scene session 与非 Hero party memory identity；动作合法结束会话时保留原目标历史派发，临时状态/延迟关窗仍绑定原 context/revision。动作 discard 清理也限定原上下文主线程。
- 新 102 检查 / 9 变异，原动作 88 / 9、准入 44 / 7、展示 46 / 6、ports 308 / 3，最终六项 Stage 和 16 组回归通过；不是实机/旧存档验收。
- 最新简明版：`docs/handoffs/2026-09-11-native-completion-team-handoff.md`；技术说明：`docs/architecture/af-native-completion-boundary.md`；审计：`docs/audits/2026-09-11-native-completion-verification.md`；台账：`docs/phase8/native-completion-progress-20260911.md`。
- 本轮检查点 `e49aabbd`。未推送、未部署，公共 Api.V1 仍只读；旧 ForExternal 兼容入口不等于新公开 SDK。
- 下一项：复用已有 MemoryCommitResult 严格接受边界并保留 Native scene session；随后处理更早 prepare/失败 pending 清理、TTS 直接回调、Courier prepare。旧 void 历史 owner 仍可能吞错/无 owner，不能宣称已实现可靠持久化或完整原子 AFEF receipt。

## 前序续作：Native 动作派发边界（生产/测试 9a5335be）

- 前半批 `8da4fbd7` 修复动作异常被当成功、日志异常让回复提前结束；本次 `9a5335be` 继续补齐未消费动作队列的等待期限。原业务 Core 未改。
- 仅尚未 claim 的动作可在 30 秒后过期，晚到不补做；已开始动作等待真实结果，不按超时伪装取消或自动重试。两个 Overlay 失败分支仍在原展示 scope 内，目前共 16 个受保护异步 UI 消费点。
- 88 检查 / 9 变异、原准入 44 / 7、展示 46 / 6、ports 308 / 3、六项 Stage 构建及 16 组相关回归通过；不是实机验收。
- 简明交接：`docs/handoffs/2026-09-11-native-action-outcome-handoff.md`；技术边界：`docs/architecture/af-native-action-dispatch.md`；最终审计：`docs/audits/2026-09-11-native-action-timeout-verification.md`；台账：`docs/phase8/native-action-outcome-progress-20260911.md`。
- 前半批审计保留在 `docs/audits/2026-09-11-native-action-outcome-verification.md`；检查点分别为 `861dd7a7`、`841e8751`。
- 未推送、未部署、公共 API 仍只读。下一项：Native 成功路径的主线程事实/记忆收尾（须区分旧会话晚返回和 owner 合法结束会话），然后更早 prepare/TTS、Courier prepare。不要把本次派发取消当整个回合回滚。

## 前序续作：Native 展示观察（生产/测试 32230a64）

- 两个 Overlay 提交入口复用同一内部观察桥和完整旧 Native 流程；14 个 UI 异步消费位置在出队时核对捕获会话，而不是只看当前 NPC 可用。
- 后端已释放时，合法最终结果仍能显示；换会话/读档/新 revision 后旧结果失效，并只释放本地旧 busy，不操作新显示。
- 新 46 检查 / 6 变异、原准入 44 / 7、六项构建和相关回归通过。公共 V1 仍只读；没有推送或部署，实机未验收。
- 最新短版：`docs/handoffs/2026-09-11-native-presentation-handoff.md`；技术边界：`docs/architecture/af-native-presentation-lifetime.md`；验证：`docs/audits/2026-09-11-native-presentation-verification.md`。
- 前序准入生产 `77d4a940`，记录保留在 `docs/phase8/native-admission-progress-20260911.md`；本轮台账为 `docs/phase8/native-presentation-progress-20260911.md`。
- 下一轮：继续 Native prepare/动作后事实回执及剩余 TTS 直接回调边界，然后处理 Courier 双向 prepare；不能把 Overlay 观察票据当成完整公共请求服务。

## 1. 当前结论

**已进入确认架构的初版实施；本轮新接口不是整个阶段 8 或完整 SDK 的最终完成。**

```text
AnimusForge.dll
├─ AF 主体：对话、LLM、Prompt、标签、记忆、调度
├─ internal 模块接口与薄桥 → 政策 / 宴会 / GCCZ
└─ public Api.V1（首版只读） ← 独立子 MOD DLL
```

当前工作树：`G:\AFMOD\AF-REFACTOR`。
分支：`codex/af-main-refactor-continuation-20260831`。
初版实施前：`df6ab928`；本轮意图/回滚检查点：`6e0de826`。
框架初版生产与测试提交：`a616958c`；最新生产见上方续作段。
精确最终提交请运行 `git log -3 --oneline`；本文与本轮源码一起提交，不编造包含自身的未来 commit hash。

给制作组直接看的最新短版见上方；框架初版说明保留在 `docs/handoffs/2026-09-11-framework-v1-team-handoff.md`。

## 2. 框架初版真实变更（a616958c）

- `Refactor/Modules/InternalModuleDirectory.cs`：内部定义、依赖/版本校验、冻结与只读目录。未初始化不报告可用，冲突不覆盖 provider。
- `TeamModulePorts.cs / TeamModuleAdapters.cs / TeamModuleServices.cs`：3 组 internal 接口、13 个原样转接方法、单例薄桥。没有改额外模块业务实现。
- `ShoutBehavior.cs / ShoutBehavior.ScenePostprocess.cs / MyBehavior.cs / CourierDeliveryBehavior.cs`：共 31 处 receiver 接入；既有默认流程、参数和权威提交顺序保留。
- `ModuleFrameworkRuntime.cs / SubModule.cs`：加载时显式装配，卸载时发布停止状态；不是 Campaign/game ready 事件，也没有新增 Tick。
- `Api/V1/AfApi.cs / AfApiContracts.cs`：稳定英文 ID、V1 能力查询、框架只读快照。内部能力 `IsExternallyCallable=false`。
- 新增契约/外部编译/薄桥回归，更新受真实receiver迁移影响的旧测试接线。
- 存档对照表只刷新因新增 using 引起的源码行号；168 个 key/ref/type/source 身份保持不变。

### 不要夸大

- 当前只登记 `af.team.policy/gathering/siege` 的选定 `dialogue` 接缝，不是所有模块功能完成迁移。
- 目录的可用状态不是执行授权，也不拦截全部历史 ForExternal 调用；每个真实请求仍由原 owner 检查。
- 新公共 API 只有 `CatalogRead`；Native/Scene/Courier 提交、动作、记忆和扩展注册明确 `NotSupported`。
- 旧 `NpcRulerPolicyBehavior.BuildActivePolicyDialogueContextForExternal` 目前本就返回空上下文；新桥没有恢复/更改业务。

## 3. 文档导航

- 总体图及责任：`docs/architecture/af-framework-v1-overview.md`
- 制作组接入步骤：`docs/architecture/af-internal-module-guide-v1.md`
- 子 MOD 使用示例/兼容边界：`docs/architecture/af-public-api-guide-v1.md`
- 本轮实施与验证台账：`docs/phase8/framework-v1-execution-20260911.md`
- 原逐项清单：`docs/phase8/af-core-review-checklist-20260910.md`（历史审批快照；用户随后已授权本轮初版）
- 上一生产修复：`docs/handoffs/2026-09-09-recovery-fixes-handoff.md`
- Courier 深层线程缺口：`docs/audits/2026-09-09-courier-thread-boundary-plan.md`

## 4. 框架初版验证（最新 Native 验证见上方）

已完成：Debug/Release × 1.3/1.4/Bootstrap 六项构建全部通过；新目录 44、公共 API 119、薄桥 308 个断言通过，四份实际实现 DLL 的 472 个元数据断言通过；原 Scene/Courier/管线与所选生产回放通过。原始命令/日志在 `.tmp/framework-v1-20260911/`，可提交的摘要在 `docs/audits/2026-09-11-framework-v1-verification.md`。

**离线回归/构建不能替代实机验收。** 前次制作组对旧候选的测试反馈，不会自动成为本轮新接口的 LIVE/SAVE 证据。

## 5. 后续工作（待用户恢复后，按顺序，不重写额外模块业务）

1. Native：准入、排队 epoch、共享后端 busy、Overlay 队列观察与动作派发失败/未开始超时和主线程收尾边界已落地；单次运行期记忆接受结果也已接入；前置历史与五个拒绝清理也已收敛；共用主线程函数的等待/诊断边界也已修复；初始场景准备与本轮 Native 持久历史输入也已捕获；继续更深 prepare、完整请求/恢复证据及 TTS 引擎直接回调边界，再评估有限公共普通文本提交。
2. Courier 双向更早的 prepare：拆开游戏读取、网络/人设/记忆准备和主线程完成，避免把整段含网络的 builder 搬主线程。
3. 保持 Scene 主体的接力、旁听、后处理、记忆/AFEF 和 TTS 回归；新接缝必须有原功能对照。
4. 在稳定请求与事实回执上再扩充公共结果/生命周期通知、内部贡献协议、经过批准的制作组能力转接或子 MOD 扩展。
5. 真实 1.3/1.4 Host、旧存档、新外部 DLL 加载/升级验收完成后，再单独确认默认迁移及有证据的旧路径删除。

不要把旧 MOOD fallback 差异、同步 Action 网络不能真正取消、Courier 前置线程缺口写成“本轮已修”。

## 6. 构建 / 回滚 / 协作边界

使用既有 `一键编译覆盖推送/build_single_module.ps1 -Stage`，一套源码构建 1.3、1.4、Bootstrap；不改脚本、不拆 Contracts DLL、不改程序集/存档身份。准确本机构建参数保存在验证日志及实施台账中。

当前自动化 `af-7-8` 已按用户最新要求暂停（PAUSED），需用户明确指示后才恢复。暂停前仅本地推进、验证和提交；没有推送、部署、安装 SDK 或操作真实存档。原两份 2026-09-06 用户草稿改动保留，未 stage 进本轮提交。

回滚采用本轮实现提交的定向 `git revert <commit>` 并保留用户改动，不 hard reset，不 force-push。`6e0de826` 是框架初版检查点；最新两批检查点见顶部，需回滚时定向反转对应实现提交并保留用户改动。

不要推原共享 `refactor/prepare-af-restructure` 或恢复其已改写历史；远端交付要使用经用户确认的专门重构分支。最新 fetch 时同名远端为 `a58c2191`，本地已有源码/测试/文档领先；新修改尚未推送。
