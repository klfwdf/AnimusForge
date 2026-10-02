# 当前交付：远端世界书已融合部署，追加导演自定义输入（2026-10-03）

- merge `e6e0fe4d` 合入main `016baa7a` 的四套内置世界书/双来源导入，保留最新生图、内战和对话代码；导演输入产品 `1ed96cb5` 也已按原脚本更新游戏。
- MCM画卷第3组新增“自定义导演提示词”长编辑器；只给导演、下一次请求生效，空白默认、在途快照/事实/输出格式保留。指南第7节。
- 最终双API+Bootstrap0错误、两DLL16审查入口PASS（导演各73）；新档API完成→知识库导入→四套选择实际DLL链路每API32项。未实机MCM/新档/GPU/provider，历史总内容oracle仍FAIL，独立完整源/Stage/安装核验通过。
- 已部署 `Modules/AnimusForge`，3378受管文件hash匹配，1944非受管保护文件大小/mtime未变；原Stage和两次Recovery可回滚。未push/新ZIP/启动游戏，旧发布包不含此候选。原共享文档dirty保留。
- [唯一主台账与源码/回滚坐标](docs/animusforge-refactoring-and-repository-reorganization-plan.md#remote-fusion-worldbook-director-20261003)、[使用说明](docs/illustrator_pipeline_diagnostics.md#7-自定义导演提示词)。最终本地收据 `artifacts/illustrator-custom-director-20261003/receipt.json`；本条替代下方各次历史“未部署”对本轮组合候选的状态。

# 当前交接：正式封臣 / 对话正文 / 拦截原版返回修复（2026-10-03，离线）

- `50a5a4fa` 修复玩家初始小势力标签误拒正式封臣，投票“强推”不满只计实际执行的逆多数决策；概率/冷却未改，不称反馈玩家高频已实机归因。
- `0e4634ed` 分离AI/普通正文恢复，修正请求revision与会话身份界限；同一次未释放、无真实战斗的被拦截原版对话退出返回自定义面板，保留释放/攻击/投降/原版特殊流程。
- 两API+Bootstrap通过；内战生命周期330、新矩阵36包含其中；规则smoke通过；对话93行为/source/XML + 两候选各9元数据（每API102）。真实LordEncounter菜单事件顺序/玩家档/实机未验；未push/部署/Stage/打包。并行生图与原两文档dirty保留。
- [唯一主台账、源码坐标与未验](docs/animusforge-refactoring-and-repository-reorganization-plan.md#civilwar-eligibility-dialogue-return-20261003)；本地 `artifacts/civilwar-eligibility-grievance-20261003/receipt.json`。仅本轮具名修复，不延续历史发布授权。

# 当前交付：后台画卷、单人物单立绘与拖拽修复（2026-10-03）

- 产品 `a596ea6c`：关闭面板继续生成；素材采集完成后离开会面不丢HTTP结果；保存成功左下角提示、画廊延后刷新、重开同目标接回任务，不强弹面板。读档/换战役仍取消旧任务。
- 每人物只采集/上传一张完整立绘，保留旧形状兼容。弹窗拖拽按实际CustomScale、当前输入和同帧布局修复，不改一键构建流程。
- 双API+Bootstrap0错误、两DLL共22审查入口通过；拖拽fixture28项、快报preloader56项。状态OFFLINE_VERIFIED，实机手感/HUD/GPU/真provider/旧档未验；未push/覆盖游戏/打包。保留其他会话dirty及提交。
- [唯一主台账与源码坐标](docs/animusforge-refactoring-and-repository-reorganization-plan.md#illustrator-background-single-portrait-drag)、[玩家可见行为与排查指南](docs/illustrator_pipeline_diagnostics.md#6-关闭面板继续生成完成通知与拖拽)；本地证据 `artifacts/illustrator-background-generation-20261003/receipt.json`。下方单图/双图与部署报告属相应历史候选，不能替代本轮实机验证。

# 生图收尾补验：完整当前树也已通过

- 另一会话UI修复提交完成后，`5a1eb668`完整树双API+Bootstrap构建0error；两实际DLL的九套审查各PASS，共18入口，18553源输入hash零变化。早期并行半成品失败/隔离结果保留，未把实机升级为PASS。
- 详细证据仍见[同一主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#illustrator-chain-review-20261002)和本地receipt的fullWorkspace字段。生图产品只本地提交，未部署/推送；需新日志匹配玩家的同次请求。

# 当前交付：生图全链路审查与详细诊断

- 立绘等待/高FPS帧截止、百科离屏开关、edits/query路径及缓存类别优先级已修；保持必需人物失败停止、原生串行与安全退休。有图默认实际发images/edits，单数非精确路径本地报错。
- 新日志按同ID串起人物/provider/落盘、提示词来源、导演HTTP、图片HTTP/下载/解码、缓存、主线程dispatchWaitMs与UI publishMs，晚期失败不丢；密钥/URL敏感部分脱敏。正文自定义规则不直接注入视觉导演，实际对白可间接影响构图。
- 隔离候选双API+Bootstrap/18实际DLL审查通过；完整当前树另有并行UI缺类型失败，未改他人代码/未声称组合实机通过。[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#illustrator-chain-review-20261002)、[日志指南](docs/illustrator_pipeline_diagnostics.md)与本地收据集中证据。
- 未push/部署/Stage/重打包；玩家具体失败与一分钟/三分钟反馈需部署后同次请求的trace/Mod_Logic时间戳，未凭本机成功记录推断其根因。之前1.5ZIP仍旧候选。

# 当前交接：对话输入 / 新档向导 / 普通选项修复（离线已验证）

- 检查点 `445c877d`、产品/测试 `4149c2e2`：F 开场输入隔离、MCM 自动 AI 开关、同层 URL 编辑与 ESC 关闭、使用现有 API、新档世界书/人物背景续接、每轮超时提示、ONNX 新档触发/弹窗调度、长/多普通选项限高滚动与专用模板。
- Debug 双 API + Bootstrap 通过；定向每 API 73项（64行为/源码/XML契约 + 9 DLL元数据）、既有给予桥55断言、223资源清单通过。直接 DLL运行回放因游戏依赖未通过，不计 PASS；实机/旧档/真HTTP/ONNX推理/打包部署均未验。
- [唯一主台账与源码坐标](docs/animusforge-refactoring-and-repository-reorganization-plan.md#dialogue-onboarding-fixes-20261002)与本地 `artifacts/dialogue-onboarding-fixes-20261002/` 保存失败、验证与回滚证据。原 Illustrator 和两文档 dirty 保留；本轮未 push/Stage/覆盖游戏，不把下方历史部署授权延续至本轮。

# 当前交付：远端 main 外交/政策已融合（2026-10-02）

- 合入远端 `720d8449`（10月2日21:27:56，北京时间），保留本地1.5版本/公告与各会话最新功能；四扩展与外交模块注册并存，共八目录项。
- Debug双API+Bootstrap、外交端口/架构、两实际DLL政策定向回归通过。原历史源码oracle仍FAIL；独立当前API/Campaign行为与负控通过，不称完整测试全绿/实机通过。
- [唯一主台账证据](docs/animusforge-refactoring-and-repository-reorganization-plan.md#remote-main-fusion-20261002)与本地`artifacts/remote-main-fusion-20261002/`记录冲突处理、失败/通过层、备份及恢复。原两文档未提交改动恢复保留，不上传私有历史。
- 本轮未push/部署/Stage/重新打包。下方1.5 ZIP是融合前候选，不含本次外交/政策代码；以新源码重建包需另行授权。

# 当前交付：1.5 发布包与玩家更新说明（2026-10-02）

- 已由原一键打包入口完成 Debug 双 API + Bootstrap、Stage、ZIP，最终 exit0；238 包文件与 Stage hash 全一致，源/Stage/ZIP 为 `v1.5.0`，保持单模块/Bootstrap-only。
- [玩家公告](docs/releases/AnimusForge_1.5_玩家更新说明.md)按 2026-09-06 零点之前的 `bfe07190` 树对比累计成果，完整介绍公开处决、政变、内战、UI、生图与消息链路；最新各会话代码未遗漏。
- [主台账详细证据](docs/animusforge-refactoring-and-repository-reorganization-plan.md#release-15-20261002)绑定最终 ZIP、hash、原入口日志与本地收据。本轮只打包/说明/本地提交，未另推送或部署 1.5；实机、旧档与真实 provider 尚未验。
- 下方此前各会话最新产品已推送/部署的历史继续有效，但不能据此宣称本轮新版本号已发布到远端或安装进游戏。


# 当前交接：地方政策修复与最新 main 合并（2026-10-02）

用户明确授权推送 main；合并保留 `d6e859c3` 全部远端更新及地方政策修复，仅交接文档冲突并保留双方记录。合并工作树双 API + Bootstrap 和政策三套回归通过，原 SessionTransport dirty 未提交。普通推送结果以远端核对为准，不强推、不再次覆盖游戏；实机未验。详见[本次唯一主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#local-policy-jurisdiction-20261002)。

# 最新操作：地方政策修复已打包并覆盖游戏（2026-10-02）

用户已授权覆盖及 Recovery 备份、指定生成目录重建。当前工作树 Debug 双 API + Bootstrap 构建成功；原脚本部署更新 156 文件，安装的 238 个清单文件 hash 与 Stage 一致，单模块 v1.3.7.2 ZIP 已生成。此前“未覆盖”仅为当时状态；实机仍 NOT_RUN、无 push。完整路径、收据及旧 Stage 拒绝/重建经过见[主台账本次条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#local-policy-jurisdiction-20261002)。

# 当前交接：地方政策无王国修复（2026-10-02，离线已验证）

- 本次最新授权仅地方政策管辖最小重构：检查点 `0d7e9e43`、产品及测试 `208ce554`。统一按所选封地/发布家族及合法关联授权，王国政策展示、费用、公开 API 和存档格式未改。
- Debug 两 API + Bootstrap 构建通过；两 API 各管辖 62、模块 1457（18 modules）、TargetPlan 765 断言通过，非唯一合计。实机、真实旧档与完整延长交互仍 NOT_RUN；未 push/部署/覆盖游戏，原 SessionTransport dirty 与其他作者文件保留。
- 唯一详细证据、源码坐标及失败/未验见[主台账本次条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#local-policy-jurisdiction-20261002)；[既有框架代码图](docs/architecture/af-framework-code-scope.md)仅作导航。下方保留既有交付历史，不构成本次发布授权。
# 当前完整交付：各会话最新代码已统一推送并部署（2026-10-02）

- 产品 `416e00aa` 覆盖内战最新玩家控制/P2、政变大厅/增援/驻防/胜利、周报/生图、处决发言、主API流式/YJ等74正式文件，含5新增源码/测试；不再仅发布四扩展接入。
- Debug双API+Bootstrap/Stage和各功能定向回归通过；实际部署更新26文件，238 Stage hash一致、5364清单外文件与三原版DLL保持。Recovery complete/备份hash通过，游戏未启动/真实旧档与实机仍未验。
- [主台账统一交付](docs/animusforge-refactoring-and-repository-reorganization-plan.md#latest-session-integration-20261002)与本地`artifacts/latest-session-integration-20261002/receipt.json`集中命令、源码/结果/恢复。此条替代下方本批功能“仍未推送/部署”和此前仅四扩展范围，不上传私有历史。

# 当前接入：四扩展宿主目录（2026-10-02）

- 生图、对话UI、政变、公开处决的 `.host` 声明和真实启动/安装/注册/停止状态已接到原目录；不自动 Ready、不新增公开执行接口。政变按要求等待另一会话结束后最后接入。
- 本包离线：97生命周期、API171/快照36、Campaign46及5+6变异 PASS；工作区 Debug双API+Bootstrap与两DLL760元数据 PASS。发布候选验证以本地收据为准；Release/实机/旧档仍未验。
- [主台账唯一条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#four-hosted-extension-catalog-20261002)与[本包源码导航](docs/architecture/af-hosted-extension-code-map.json)说明范围、候选/证据/回滚。已获提交推送授权，仅发布本包到main，不带其他本地历史；未Stage/部署。实际远端结果见收据。

# 当前交接：主 API 正文流式 MCM 选项（2026-10-02）

- 新增主 API“开启流式传输”，默认关闭、无需重启，终端/MCM 共用 `MainApiStreamingEnabled`；共享正文入口按开关走 JSON/SSE，有回调则预览，无回调则完整汇总后返回。native 原有 callback-selected stream 也统一受控，非正文 API 配置/发送入口不改。
- 保留温度/Token/思考参数和有限重试；缺正常结束标记、坏 chunk、断流、取消、stale 均不把半句提交为完整业务回复。后处理仍在正文完成后处理，不按分片执行动作。
- 最终隔离 Debug 双API+Bootstrap PASS；真实 DLL 15项新回放及既有 Primary 回放 PASS，transport17/gateway40/non-stream240 checks PASS。未部署/push、未真实 API 或游戏验收；保留并行 dirty。详细范围/源码坐标/修订与失败记录见[主台账条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#primary-api-streaming-option-20261002)。

# 当前交接：YJ URL 与 API 设置/请求头检查（2026-10-02）

- 新旧引导 YJ Base URL 改为 `https://www.shenlanqaq.com/v1`；Gemini 兼容保留旧域名；修复共享 query-chat 拼接和代理前缀 models 丢 `/v1`。终端/MCM 共用设置对象，终端手动/关闭 dirty 保存；API 面板在保存完成时写回。
- 新增真实协议源回归与既有 Protocol 13项 PASS，最终隔离 Debug 双API+Bootstrap PASS/0错误。不是实机/真实 API 验收；未部署/push，保留其他 dirty。
- 握手不覆盖实际生成 tokens/Anthropic thinking，新旧 YJ 思考默认值不一致和 API 面板保存失败反馈不足均明确保留。详细证据与代码坐标见[主台账本次条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#yj-api-settings-audit-20261002)。本条仅交付本次配置/协议小修，不替代其他并行任务状态。

# 当前交接：AF 2.0 收尾交付（2026-10-02，离线接入已就绪）

[执行计划](docs/plans/af2-module-integration-delivery-closeout-20261002.md) R0–R3 已执行到 **INTEGRATION_HANDOFF_READY**：B0/B1 本轮直验 PASS，B2/B3 同源证据 REUSED_PASS；只修改接入/交接文档，无具名产品阻塞和产品改动。制作组可按[内部指南](docs/architecture/af-internal-module-guide-v1.md)及[公开 V1 指南](docs/architecture/af-public-api-guide-v1.md)接入。旧宿主拆分后置。

冻结源码 `dd6f45bf` 的 1337 raw 输入与 Debug/Release 六 DLL hash 已复核；SessionTransport raw hash `a393a98b…` 在冻结源中，受保护 dirty 未暂存。远端 main 只读核对仍为 `261a3be3`。v1.3.5/v1.4.5 的 U1–U9 实机、代表性旧档和独立子 MOD 加载仍 **NOT_RUN**，故不是 USE_ACCEPTED 或发布；原 355 runner 非全 PASS。详细状态/下一步见[主台账本次条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-module-integration-delivery-plan-20261002)，源码责任见[代码范围图](docs/architecture/af-framework-code-scope.md)。未 push、Stage、打包、部署或触碰真实玩家数据。

# 历史交接：P1–P8 host terminal closeout（2026-10-02）

本轮授权替代“仅计划/不启动”；唯一主台账末尾“最后三个原闭包与同候选验证进度”和当前代码范围图收集证据。最后三个 original core 已实际迁 owner（D9eb43b6d/B355acce0/C581faa44），保持公共入口/保存/程序集身份。

**P1–P8源码/离线出口：OFFLINE_VERIFIED_WITH_LIMITS。** 唯一详细主台账见[P1–P8最终源码/离线出口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-host-terminal-final-offline-20261002)，完整收据 `artifacts/af2-host-terminal-closeout/final-receipt.json`；838-ID代码图与真正partial/owner规模保持。

保存兼容产品候选 `dd6f45bf`，current/Debug/Release同1337 raw输入；两配置双API+Bootstrap六构建exit0（debug-b67c7b0cc63f/frozen-release-f0bd0f0c6496）。ReadingXp原8literal恢复，真实legacy键44断言与坏键负控PASS；新Release1.4 SHA `DEAA0BE7FDE9E4C88AA2169A86CD7A7E5368B4697B5367F2EC9F305DA9D6D521`。原mandatory d57身份审计178键/类型、42行为与模块/Bootstrap PASS，不等于真实旧档已验。

最终原runner `final-gates-55eb` 完整355入口，**343 PASS/0 FAIL/3 PREEXISTING_FAIL/5 NEEDS_INPUT/3 SUPERSEDED_BY_RUNNER/1 ENV_STATE，exit0**。411actualbindings/163boundedpaths与4漂移负控PASS。当前owner、历史protocol/UIoracle、sourceinverse、真实DLLreplay分层，不称“355测试全通过/全当前产品验收”。原354、69、40和前完整355的失败日志保留逐项对照，不拼局部成绩；旧取消run仍CANCELLED_PARTIAL。整体CourierSchedule/ModuleNativeSubmission/NativeWait未直测仍列NOT_RUN，关键late-wait→Audio原子25实测另外成立。

LIVE/真实旧档/网络/TTS/帧性能NOT_RUN；证书全局影响归因UNKNOWN；512terminal原淘汰边界保持。保护原SessionTransport raw dirty及tools/NuGet，不push/部署/清理。下方旧交付均历史，不授权本轮推送。

# 当前交接：AF2 两大宿主 P1–P8 终态实施（2026-10-02）

- 用户已直接授权 [完整终态计划](docs/plans/af2-host-terminal-closeout-20261002.md) P1–P8 实施并持续到出口；计划中“仅规划/不启动”是被本轮 supersession 的历史状态，原验收标准不改。意图 `16a6ce67`，当前 ACTIVE，尚未宣布源码职责闭合。
- 四执行线按独占簇迁真实 owner，唯一集成人维护共享边界/两个主文件/保存组合/索引和集中门禁；过渡 partial 不算完成。原 SessionTransport raw dirty 与旧 tools/NuGet 未提交、不清理。
- [唯一主台账当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-host-terminal-closeout-implementation-20261002)保存授权、退出门、切片和失败/未验；既有 [代码图](docs/architecture/af-framework-code-scope.md#af2-final-responsibility-closeout-20261001)是旧已验证导航，不能当新候选验收。
- 新精确合成 TEMP 目录另获授权，仅当前新子树；无 push/Stage/部署/游戏覆盖/真实玩家音频清理。实机、旧档、真实网络/音频/帧成本仍 NOT_RUN。SDK 首次初始化证书 stdout 信号已披露，只有既有证书只读元信息，实际写入归因 UNKNOWN；不擅自删除或改系统配置。

# 当前交接：7 个残留测试/工具 FAIL 离线闭环（2026-10-02）

- 新授权仅替代下方“7残留”停点：意图 `9488c9be`、已验证切片 `9cff3ee8`；产品仍冻结 `2287069b`，1212同源hash/原SessionTransport保护一致，原dirty/tools/NuGet及他人计划不纳入提交、不清理。
- 原7完整入口 **7 PASS/0 FAIL**，加2新回归入口实际 **9 PASS/0 FAIL**；Prompt21/10、inventory12、candidate4、overlay3检查与4运行时变异负控通过。Primary明确Debug候选，Release负控仍拒绝，无skip/真实网络fallback；完整307成员隔离ZIP合同已验、未发布。41真实新候选登记保持REPRESENTATIVE，不冒充游戏完成。
- 首轮303/旧失败/分类保留，不是第二次303/当前305全量或实机验收；六构建复用同源证据。命令、源码坐标、收据和盲区只见[唯一主台账本次条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-seven-residual-fail-closeout-20261002)，产品范围沿用[已验证责任图](docs/architecture/af-framework-code-scope.md#af2-final-responsibility-closeout-20261001)。F7/历史非PASS/许可/凭据轮换未闭，无push/Stage/部署/一键语义变化。

# 当前交接：AF 2.0 最终职责收官实施（2026-10-01）

- 本轮授权实施F0–F6/S1–S6；五组完整职责差集冻结并进入实施，不继承历史push/Stage/部署/仓外写入许可。用户已明确批准F5d Persona Prompt/fallback/解析规则收口，五组+F5d最终范围冻结；实际技能效果/保存不改，完整门禁未完成不能宣称全闭。
- 起始 `320c1aad`，空意图 `3d59d616`；两API求值各1172唯一Compile/8资源，51宿主partial与旧A成员证据复用并按变更/保留/迁移具名分类。原SessionTransport raw dirty与旧tools/NuGet保持不提交、不清理。
- 产品冻结 `2287069b`，F3/F4测试 `1018a8b0`、F5测试 `d0363c1a`，最后必要fixture `fa555f7c`；最终1199 Compile/8资源，Debug/Release双API+Bootstrap同manifest六构建PASS，838地图双模式PASS，S6五项维护影响具名落点见[当前范围图](docs/architecture/af-framework-code-scope.md#af2-final-responsibility-closeout-20261001)。完整303首轮及受影响完整入口复验已分类：首轮254PASS/37FAIL，受影响最终30PASS/7残留；最新逐入口284PASS/7FAIL及其余12分类，不是首轮全PASS或第二次303执行；本轮有限职责退出门已交付，不宣称全仓历史/实机全闭；唯一失败/证据/未验见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-final-responsibility-closeout-20261001)。F7 LIVE/SAVE/真实网络/帧性能NOT-RUN。
- 全量runner在精确新根 `E:/AF2-SyntheticTemp-20261001-7dd0991fd822` 获准子树串行执行合成夹具，不批量清根、不触其他外部数据。Conversation早期越界写入一个精确TEMP源码引用文件，覆盖旧文件与否UNKNOWN，已停止且不清理；详情如实记主台账。旧凭据轮换未确认，不读取/复述秘密；本轮无push/Stage/部署/一键语义变化。

- 最后收尾2026-10-02：同源Debug Primary回放PASS，Release缺DEBUG seam仍记失败；6个旧fixture缺口有exact320实际复现。原Memory历史15同baseline28failure记录，未刷审批期望；两个Sealing盲区保留NOT_PROVEN。准确分类、artifact及S6保留责任只见上述唯一主台账，不把本地提交当推送/游戏部署。

# 历史交接：合并 main 参战邀请修复并交付 main（2026-10-01）

- 用户已明确授权先合并最新 `main`、保留修复并验证，再普通推送到 `origin/main`；这只改变本轮发布目标，不授权强推、部署或清理。
- 意图 `ff0ce7e4`，双父合并 `7dc16f68` 保留远端 `0a641aab` 的参战邀请去重修复及现有源码归位/功能；唯一测试冲突仅适配两个 canonical 路径，新增断言完整保留。
- 原测试 Program 在本地 net8 宿主通过 5 项冒烟检查；封存候选 Release 两 API + Bootstrap 构建零错误，真实 DLL/marker 核验通过；两 API 各 1172 Compile / 8 资源集合与此前相同，813 代码图两模式通过。不是实机或全仓测试验收。
- [本轮唯一台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#main-call-to-war-integration-20261001)记录源码坐标、失败诊断、保护范围和发布核对；实际推送以最终远端 ref 与本地 receipt 为准。原 SessionTransport raw dirty / 1599 未跟踪文件保持，不纳入提交；无 Stage、游戏部署、下载、清理或一键脚本修改。

# 历史交接：远端功能合并、9归位与普通推送（2026-10-01）

- 普通merge `9ee686ee`保留远端`982a5861`功能与本地目录；9新增按精确批准canonical归位，root tracked C#仍0，总定位336，future新外交未覆盖。
- 两API1172 Compile/8资源、purecore186实际增量核通过；六隔离构建/marker、实际DLL各45、最终严格非外交C43 43PASS/0FAIL/0BLOCKED_ENV、813地图两模式通过。受影响Execution/Weekly/CivilWar/Coup/GCCZ/Policy/Illustrator/WorldMap专项有限通过，不称LIVE或全仓PASS。
- 原历史hash/负控保留，approved feature exact投影与当前功能分层验；首次C43 42PASS/1FAIL因拒绝错误前缀漂移，具体边界包装修`e3d65455`后复验；完整Memory历史层等继续延期。保存inventory178/205/46与内容5named deltas有实证，不盲刷旧hash。
- 意图4d1d4ad7、merge9ee686ee、测试5691a9c7、负控包装e3d65455；正式材料提交以Git日志为准。用户授权普通push，执行前fresh fetch/祖先核，执行后ls-remote核SHA；实际结果以远端ref与本地receipt为准，文档不代替成功证据。
- [唯一主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#remote-feature-merge-20261001)集中证据、失败/未验与push范围；[目录说明](docs/architecture/af-source-directory-guide.md)/[范围图](docs/architecture/af-framework-code-scope.md#remote-feature-merge-20261001)为当前导航。原SessionTransport raw dirty及1528旧tools/NuGet保留，不纳入提交。无Stage/部署/清理/外仓或G镜像/下载安装/一键变化；实机旧档网络音频性能、完整历史业务、TPAC及凭据轮换未验。下方旧不push是当时历史，不撤销本轮新授权。

# 当前交接：第三轮最后38源码归位有限交付（2026-10-01）

- 批准38项原样归位（外交18、GCCZ实质游戏宿主19、Gathering1）；根级tracked C#38→0，三轮累计327归位，不表示root资源/产物清空。业务/namespace/类型/保存身份/API/程序集和默认入口不改。
- 两API各1150 Compile/7资源及纯core184保持；六隔离构建通过；严格非外交C43 43PASS/0FAIL/0BLOCKED_ENV；两API实际DLL各45检查；813map两模式PASS。新增外交消费者只验必要路径/有限源码接线，不扩完整业务。
- 本地c283963b意图、71b04bfa小片、6b6603c2余37、2401bbfd消费者；正式文档提交以Git日志为准。完整证据、实际失败、保护/未验仅见[唯一主台账第三轮](docs/animusforge-refactoring-and-repository-reorganization-plan.md#source-relocation-round3-20261001)。[目录说明](docs/architecture/af-source-directory-guide.md)/[范围图](docs/architecture/af-framework-code-scope.md#source-relocation-round3-20261001)为现态；下方前两轮root保留只是历史，已明确替代。
- 未来新外交canonical为src/modules/AF.Module.Diplomacy，后续另行实施，不root副本/双编译。本轮未覆盖新实现。原SessionTransport dirty/旧tools/NuGet/产物保留，无push/Stage/部署/清理/安装下载/外仓或G镜像/一键变更。完整历史服务/外交、实机旧档/网络音频性能、远端SourceLink、TPAC许可及凭据轮换未验。

# 当前交接：第二轮源码物理归位有限交付（2026-10-01）

- 第二轮批准111项已全部raw-byte原样归位；根级tracked C# **149→38**，两轮累计327→149→38、289归位。MyBehavior22及其他混合AF宿主整家族/整文件归宿主；30具证纯AF-side桥单独归src/bridges。根级38明确为18外交产品+20制作组实质规则/状态机/业务存档，不擅自迁排除业务。
- 两API实际1150 Compile一一映射无漏重/7资源LogicalName保持；Debug/Release双API+Bootstrap六构建exit0；严格非外交C43 **43PASS/0FAIL/0BLOCKED_ENV**；两API真实DLL各45message/history检查；813地图recorded/working-tree PASS。原runner hash/路径失败均保留，精确inverse后复验；完整TeamModuleServices缺CivilWar接口仍未验，不用stub绕过。
- 本地05e35849意图、2f6a4ce4小片、2f833b28其余110、32097613消费者、23ac43dc额外Native path inverse；正式收尾commit以Git为准。[唯一台账第二轮](docs/animusforge-refactoring-and-repository-reorganization-plan.md#source-relocation-round2-20261001)集中实证/未验；[目录说明](docs/architecture/af-source-directory-guide.md)/[现态范围图](docs/architecture/af-framework-code-scope.md#source-relocation-round2-20261001)导航。本条替代下方第一轮混合宿主保留/测试hold状态，旧记录只作历史。
- 原SessionTransport dirty/旧tools/NuGet/产物保留，无push/Stage/部署/清理/下载安装/G镜像/一键行为变化。实机/旧档/真实网络/音频/性能、完整外交业务、TPAC HOLD、旧凭据轮换未验。外交测试仅必要current path，不变外交产品/fixture/metadata语义。

# 当前交接：AF 源码物理归位有限离线交付（2026-10-01）

- 批准179候选实际**178原样归位 + 1外交消费者hold**，根级tracked C# **327→149**；RewardSystemBehavior.cs原样留根，外交J12测试不改。不是职责抽取或整个领域重写；MyBehavior22/DuelSettings/SubModule/IntegratedModuleHost等混合宿主保留。
- 双API实际Compile各1150无漏重、7资源逻辑名保持；Debug/Release两API+Bootstrap六构建exit0；最终严格具名非外交C43 **43PASS/0FAIL/0BLOCKED_ENV**；真实1.3/1.4 message/history各45PASS；代码图813两模式PASS。首轮C43两路径失配/1.3缺SaveSystem失败日志保留，已明确诊断与复验，不称全仓PASS。
- 本地`a4ebda43`意图、`dbae81d3`小片、`63b1a8e0`其余迁移、`2524ddcf`路径消费者；正式收尾提交以Git日志为准。完整证据/保留/失败/未验读[唯一主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md#source-relocation-20261001)，现态入口读[目录说明](docs/architecture/af-source-directory-guide.md)和[范围图](docs/architecture/af-framework-code-scope.md#source-relocation-20261001)。
- 原SessionTransport dirty/22旧tools/NuGet/产物保留，无push/Stage/部署/清理/下载安装/一键行为变更/仓外扩写。实机/旧档/真实网络/性能、TPAC许可HOLD、旧凭据轮换未验。下方旧当前记录保留历史，不恢复施工/上传授权。

# 当前交接：本地功能与远端融合（2026-10-01）

- **融合版本已部署并完成复核（2026-10-01）**：实际安装源码`6c32576c`，原脚本更新28文件，238个Stage/安装哈希一致，原文件Recovery备份与complete标记通过；包含城镇记忆7日间隔/384 tokens。实机/真实旧档/provider未验；用户已授权本轮提交推送。[部署验收、证据及安装回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#merged-deployment-verified-20261001)。此项替代下方历史“未部署”状态。

- **远端8f3903e2已融合，未推送/部署**：保留本地城镇记忆7日间隔、快报/政变/处决等功能与远端测试收尾、场景死代码清理；两份文档冲突保留双方证据。修正渠道fixture对本地公开处决规则的依赖，群聊31、渠道138、记忆60、处决98、调度19、源码15检查与Release双API+Bootstrap通过；全仓C/实机/真实旧档未验。[本次源码责任、失败修复、证据和回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#remote-integration-20261001-town-memory)。下方各任务记录保留各自验收范围。

# 当前交接：J 历史非外交测试闭环（2026-10-01）

- **本轮有限PASS**：空intent `ec74d44d`；六包精确本地测试/metadata提交，无产品源码/Host异常政策改动。新Debug/Release双API+Bootstrap六构建通过；最终严格具名同候选C43 **43 PASS/0 FAIL/0 BLOCKED_ENV**，不是全仓PASS；当前1.3真实message DLL45检查通过。38旧J16入口一次映射，最近33既有失败中28已复验PASS；外交1排除、shared-Service及captured/sealing外交耦合延期，旧B1 current15断言已有限闭合但原完整inverse保留延期；Budget新generate-pair实际双workload/原比较已验，Budget/finite两新入口在最终C43复验；含外交注册名的CampaignComposition共享入口保守延期（前序空壳注册记录保留）。详见[唯一主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j-history-nondiplomacy-closeout-20261001)与[测试证明范围](docs/architecture/af-framework-code-scope.md#j-history-test-proof-scope-20261001)。
- A/J17迁移/R05-i已闭状态不重开；原SessionTransport dirty/22旧tools/NuGet保留。D实机/旧档/真实network/性能、TPAC HOLD、凭据轮换仍未验；无push/Stage/部署/安装下载/外仓写/清理。下方旧“当前”记录为历史，不能恢复其上传/施工授权。

# 当前交接：J17 收尾与远端 CivilWar 合并（2026-10-01）

- **政变后旧王朝复位已离线实现（2026-10-01，未部署）**：`f1760f76`；扣押分支排除旧王族，释放分支将原王族加入实际叛军（可无地跟随）；只有明确叛军夺位胜利恢复原王朝及发动前国名，和平/妥协不复位，旧王死亡由原王族现任族长继承。建国后登记可保存并手动重试，普通玩家派系开关不拦政变战争结算。205内战契约、128政变契约、228真实DLL夹具与规则smoke、Release双API+Bootstrap通过；游戏/真实旧档/原生战争未验，无部署推送。[源码、边界、验证及回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-restoration-victory-20261001)。

- **快报事件配图与宽版UI已离线完成（2026-10-01）**：`a3183aaa`，收集期到期且≥2组真实其他消息后，选定事件即并行生图/写稿；冻结事实和多人物参考，标题润色不重画，作废取消。报纸加宽、16:9沿用预设映射、去黑色遮罩，并增强冲突动作提示。29任务+46政策+10人物检查、Release双API/Bootstrap、两DLL存档/生命周期/等比fit通过；未部署/推送，实机/GPU/真实API未验。[源码、证据、回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#bulletin-event-art-20261001)。

- **城镇记忆间隔改为7游戏日（2026-10-01，未部署）**：按用户要求替代下方3日默认值，其他成本限制和手工保护不变；60项检查及Release双API+Bootstrap通过，实机/玩家旧档未验。[本次验证与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#town-memory-seven-days-20261001)。

- **城镇记忆确认事件与限频更新已离线完成（2026-10-01，未部署）**：`eb39f14b`，接入易主/继任、战后实际结果、文化和地方政策生命周期；按需累计、3游戏日/1分钟限频、2并发、384输出tokens，手工正文保护、失败保留、主线程版本校验；原键v1/v2读入与v3分块保存。59定向检查、token/HTTP回放、Release双API+Bootstrap通过；实机/真实API/玩家旧档未验，GCCZ外仓路径不存在未同步，无推送。[源码坐标、性能、存档及回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#town-memory-refresh-20261001)。

- **政变旧王支持者反抗与处置菜单已离线修复（2026-10-01，未部署）**：`86991dc3`，按用户选择，原王族/更支持旧王的合格有地家族可反抗，不再要求与新王关系≤−5；旧请求/普通周叛乱保持。夺位结算后通过原入口打开胜利处置菜单，跳过重复转城、失败可手动重试、成功收据防重开；修正政变场景冒用SETS的TAB提示。128契约、223实际DLL/夹具、Release双API+Bootstrap通过；真实建国/命名/GCCZ菜单及旧档未验，无部署/推送；旧已完成政变不追溯重算。[故障日志、源码、验证与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-loyalists-and-aftermath-20261001)。

- **本会话快报/场景插画/画廊RP功能已部署（2026-10-01）**：用户“部署”授权，干净源码`454a63b3`重新Release双API+Bootstrap通过；官方单模块脚本更新9文件，安装后238个Stage文件哈希一致，Bootstrap唯一入口。实机/真实旧档/生图耗时未验，未推送。[部署与回滚证据](docs/animusforge-refactoring-and-repository-reorganization-plan.md#illustrator-deploy-20261001)。

- **快报配图提前生成（2026-10-01）**：`5f096d08`，正文发布后即开始生成本期新画；打开/重开复用本期精确缓存或加入进行中任务，关闭面板不取消后台任务，失败不自动重试。此项替代旧“每次打开都重生”。22状态检查、两个实际DLL关闭/取消检查及Release双API+Bootstrap通过；真实导出/API/游戏未验，未部署/推送。[源码坐标、证据和回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#bulletin-prefetch-20261001)。

- **篡位发动门槛已离线实现（2026-10-01，未部署）**：`d7dff15a`，默认4级家族/300影响力/实际60名突击队另留1人，MCM可调；不扣影响力、无筹备费用或等待。菜单/选兵前/确认后重验，门槛随确认窗快照，登记后不追检，大厅仍最少1人，旧会话沿用原门槛。113契约、206真实DLL/夹具检查、Release双API+Bootstrap通过；原生UI、MCM改值持久化及真实旧档未验，无部署/推送。[源码、验证与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-admission-sixty-20261001)。

- **画廊转RP物品/插画入口去重（2026-10-01）**：`fed01fb6`，画廊选图→编辑介绍→加入背包，沿用原展示物品链路向NPC提供画作介绍；保留DialogueUI现有场景插画按钮，移除额外右上角注入。19转换检查、双API+Bootstrap、真实DLL回调与RP介绍JSON/共享事实读取通过。未部署/推送，游戏及真实旧档未验。[源码、证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#gallery-rp-items-20261001)。

- **派系面板与事件政治已一键编译部署（2026-10-01 13:56）**：`3b8337d7` + `79874aaa`，玩家加入/退出/创建/起兵，国王压制/谈判/妥协/强制解散，跨国七天冷却、保存的三日回应、v4兼容、日级分批处理；补齐周报素材、快报类别、相关领袖事迹及AFEF。180项离线回归、规则smoke、Release双API+Bootstrap及实际DLL面板XML/关闭补丁探针通过。用户授权后通过原版一键编译并覆盖BAT完成Debug双API+Bootstrap，最终更新9程序文件、238安装哈希一致，旧版备份已验；真实游戏/旧档/命名服务/发布与帧耗时未验；其他作者改动保留，无推送。[源码责任、候选、性能边界及聚焦回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#civilwar-political-panel-20261001)。

- **篡位MCM七项战斗参数已离线实现（2026-10-01，未部署）**：`0aff8322`，街道/大厅突击队、门卫/大厅护卫、守军每波/间隔/存活波数均可调；默认保持现状，发动时快照并保存，下次政变生效。接通SETS真实人数/波次，旧接口及普通场景保持默认。修复Newtonsoft复用对象掩盖部分损坏快照；91契约、170真实DLL/夹具检查、Release双API+Bootstrap通过。并行WIP排除在隔离构建外；MCM实显、大规模导航性能和真实旧档未验，无部署/推送。[源码、完整证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-mcm-battle-options-20261001)。

- **处决遗言/喊话行刑已离线接通，轮刑选择已隐藏（2026-10-01）**：`68dd6cd5`，三阶段台词结合相关记忆，实际气泡显示后才记原文；不等遗言播完，未显示后半段不补记。死亡成功后才供公共消息转述，新增实录存档保留最近100场。必须单选本场刽子手，通过统一后处理启动；否定/条件/错目标/迟到/重复不执行，Native 仍等对话关闭。普通及自定义方法列表隐藏轮刑，旧 ID/预设保留。98项定向断言、原parser、Release双API+Bootstrap及独立复仇编译通过；真实模型/游戏/旧档未验，本轮未部署推送。[唯一台账、源码坐标、候选哈希与逆向回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#execution-last-words-20261001)。

- **政变完整结果快报已接入并部署（2026-10-01 13:06）**：`a4723aef`，成功/失败最终事实包含城镇/王国/双方、实际街道/大厅过程、处置与伤亡；进入现有快报Prompt/模板及NPC知识，原记忆/周报保持。63政变契约、38快报规则、真实DLL117检查及双API+Bootstrap通过。排除并行内战WIP，用已提交基线+本任务overlay快照构建部署；27文件更新、238哈希一致，备份已验。沿用收集窗口/冷却，关闭快报不阻塞结算，旧完成事件不补发；真实文案/NPC转述/旧档未验。[精确源码、构建失败与隔离证据、回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-bulletin-outcome-20261001)。

- **政策投票五项性格已离线接通（2026-10-01）**：`d54ac0eb`，玩家/NPC 首次评议保存政策手段的仁慈/荣誉/慷慨/勇气/审慎画像，原投票计算加入独立 P，保留实际损失扣分、拉票承诺与提案者保护；MCM 性格倍率默认1。旧政策无画像保持原评分，重新评议可生成。Release 双API+Bootstrap通过，两候选定向回归各1475断言通过；1.3离线runner补用了本机通用依赖，不代表纯1.3运行时验收。未部署/推送，真实模型、旧档和游戏投票未验。[源码证据、产物与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#policy-personality-vote-20261001)。

- **驻守城镇遇敌出城已离线修复（2026-10-01）**：`a9539dc0`，实际已进目标城镇/城堡的驻守部队不再自动转成攻击围城军，并屏蔽原版 initiative 绕锁出击；城外援军、巡逻、明确攻击及到期语义保留。24项真实方法/桩Campaign回归与3故障变异通过，Release双API+Bootstrap零错误。未部署/实机，旧逻辑已转攻击的命令需取消后重新下达驻守。[代码责任图、证据、产物及逆向回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#settlement-stay-sortie-20261001)。

- **政变大厅 F 门转场抢占已修复并部署（2026-10-01 12:34）**：`5623ac25` 接管所属Coup任务的原版PassageUsePoint.OnUse，取消裸F轮询，守卫清除后先HallSelection选兵再登记大厅援军，避免原版直跳导致撤退/普通大厅。双API+Bootstrap、14新增+91既有离线回归、安装DLL门补丁注册PASS；11文件覆盖、238文件哈希一致，旧文件已备份。真实F/大厅交战/旧档未验，失败旧局需读发动前存档重测。[实机故障序列、精确坐标、部署证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-passage-transition-20261001)。

- **快报首字绕排与封存按钮已离线修复（2026-10-01）**：`5f5bb1bc`，首字去蓝金底图，实际测量仅前三行缩进、后文全宽并保留链接；封存改浅纸色细描边，去ESC字样。Release双API+Bootstrap及两DLL各152排版/字符检查PASS，游戏字体/控件加载/滚动点击未验，未部署/推送。[精确坐标、证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#bulletin-dropcap-flow-20261001)。

- **王室阵营会因事件撤回支持（2026-10-01）**：`f77c4f51`，负面事件命中的王室阵营NPC累计不满达到现有MCM门槛（默认35）立即退为中立，玩家/王族不自动退出；记录历史和周报素材，不直接换国或起兵。复用现有事件，无新增轮询；102项生命周期检查和Release双API+Bootstrap通过。未部署、未实机；[源码坐标、边界、证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#civilwar-crown-withdrawal-20261001)。此前“王室阵营无自动退出”的说明由此替代，AI自动加入王室阵营仍未实现。

- **场景插画预设映射已替代固定尺寸（2026-10-01）**：`82269de8`，选2048方形→2048×1152，1344/1536档→1536×864，其余现有档→1280×720；只转换本次场景请求，设置原值保留。双API+Bootstrap和两个DLL各六预设检查PASS，未部署/推送/真实API或游戏验收。[源码坐标、验证与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#scene-image-presets-20261001)。下方固定1280×720交付为历史。

- **全部派系不满改为每日平滑衰减（2026-10-01）**：`05870e6f`，按用户明确选择保持原7天累计降幅，覆盖全部9来源。日事件处理已有记录、缓存系数和复用缓冲，存档日期防重复与旧档追扣；成派/通牒仍按周。76项生命周期回归、原smoke、Release双API+Bootstrap通过；合成100家族×9来源测得日事件均值0.1859ms，实机耗时/旧档未验。本片未部署；[具体证据、源码坐标与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#civilwar-daily-decay-20261001)。未新增自动保皇派。

- **场景插画16:9已离线修复（2026-10-01）**：`c4ac6035`，场景/地图会话生成固定请求1280×720，images的size与Chat的aspect_ratio均有真实参数接线；快报3:2及百科不变。Release双API+Bootstrap通过，真实服务端与游戏未验，未部署/推送。[源码坐标、证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#scene-image-16x9-20261001)。

- **政变专用进场与受击崩溃修复已部署（2026-10-01 12:10）**：`f5225e5d` 补街道/大厅专用菜单，修复 internal Origin 构造器漏绑定和 MbEvent 逆序导致 Coup 场景漏挂载。最终双 API+Bootstrap、真实注册四标志、26 新回归+65 既有回归 PASS；用户随后授权部署，原脚本更新11文件，Stage全部238文件哈希一致，旧文件已备份。未把当前内战施工源码重新编入。原生进场/受击/旧档未验，不将离线 fixture 当实机完成；[证据与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-scene-repair-20261001)。

- **快报生图与封存控件已离线修复（2026-10-01）**：`5d0969b8`，快报每次打开重新生成1536×1024横图，不加载旧图；图片按实际比例缩放，封存改金边深红按钮。Release双API+Bootstrap、两个实际DLL尺寸验证、XML/资源映射通过。未部署/推送，真实生图及游戏点击未验；并发Coup改动保留。[范围、精确源码坐标与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#bulletin-image-controls-20261001)。

- **内战派系五项审查修复已部署（2026-10-01）**：产品/回归 `76503341`，修复内战标签双重过滤、玩家国王缺答复规则、失败结算丢记录、废除政策误增不满、拒绝参战仍扣忠诚度。56 项真实 owner/效果＋桩游戏回归、原 smoke/交互契约、最终 Release 双 API+Bootstrap PASS；按用户后续“部署”授权覆盖单模块，更新9文件，部署后238个Stage文件哈希一致。实机/真实旧档未验；异常副作用不明确时保留状态等待核对。源码坐标、构建版本/哈希、历史地图失败、旧 Stage 和安装备份见[本任务台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#civilwar-review-repair-20261001)，不替代其他任务或J17总体验收。

- **宣权篡位灰色入口已离线修复（2026-10-01）**：`f1c27b5b` 将 Coup 的旧私有字段反射改为当前主体状态查询，保持冲突阻挡与失败关闭。Release 双 API+Bootstrap、真实 1.4 注册四标志、36 状态/拘押生命周期与 29 选兵回归通过；拘押加载期 False 已证为实例生命周期条件，未改拘押产品代码。未部署/推送/游戏旧档验收，其他内战/周报并行改动保留。独立产物、精确坐标、全局地图历史失败及回滚见[本任务台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#coup-entry-repair-20261001)，不提升 J17 总体状态。

- **即时快报审查修复（2026-10-01）**：修复读档后最新快报选错、坏存档原文被自动初始化覆盖、过期摘要混入近期消息；38规则检查 + 16真实方法/桩存储回放 PASS，Release双API+Bootstrap通过，实机/真实旧档未验。新增恢复字段随原存档键保存，查询缓存避免每次对话全量扫描。仅本地提交，无部署/推送；详细源码坐标、构建哈希、其他任务并发边界与回滚见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#world-bulletin-review-fixes-20261001)。

- **最新 R05-i 精确清理（2026-10-01）**：`ccd6c2fd` 仅删除恒真分支后的旧群聊死尾段；活 per-hero/旁听、开关分支与反射/兼容入口保留。31编译行为+15源码检查、两个编译成功具名负控、Debug/Release双API+Bootstrap通过。新候选完整C唯一失败是尚未写入的文档锚点，已补齐并定向收尾；原失败不改写，D仍NOT-RUN。详见[唯一台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-r05-i-cleanup-20261001)及[813代码地图](docs/architecture/af-framework-code-map.json)。无推送/部署。


- **本轮最新：J17 B6有限离线出口与完整C已验（2026-10-01）**：六包必要定向/编译后行为变异及当前1.4 DLL回读完成；Debug/Release × 双API+Bootstrap全部exit0，最终同候选C **287项：243 PASS、0 FAIL/0 BLOCKED_ENV；33既有失败、6需输入、4替代、1环境状态保留，exit0**，不是全部测试通过。产品相对远端WIP无新增语义修改，测试冻结`af6006c4`，813地图已绑定/双模式验。f/g retained、i OPEN；D实机/真实旧档/真实网络/性能NOT-RUN，1.3实际message runtime缺SaveSystem未验，旧凭据轮换未确认。未push/Stage/部署/清理，原SessionTransport dirty和旧tools/NuGet保留。[唯一详细当前结果](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-b6-c-resume-20261001)；下方ACTIVE/暂停与历史上传授权均不再发当前指令。

- **本轮恢复意图（历史，结果见上条；2026-10-01）**：用户已明确恢复施工，不延续推送/Stage/部署授权。WIP 基线 `3c00ae2e`，空意图检查点 `38efd7eb`；六包各自实现/定向验证，P6 唯一共享接线/Git/串行构建和最终同候选 C。A/Memory/Weekly/B7 不重做，保留 SessionTransport 初始 dirty 及旧 tools/NuGet/产物；当前 ACTIVE，尚未验收。最新范围和证据集中于[主台账当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-b6-c-resume-20261001)，下方暂停和历史授权不是当前指令。D NOT-RUN，旧凭据轮换未确认。

- **前次远端 WIP 接续交付（历史；2026-10-01）**：用户要求本轮全部上传并说明进度；P1/P2 全部必要产品、新 owner、测试及共享接线纳入本次 WIP 提交（以实际 Git HEAD 为准），不是验收 PASS。A 已完成，P3/P4/P5 已提交；最新源码最终双配置双 API+Bootstrap/C 未跑，P2 最近 fixture/三变异与 fallback retry 证据待补，地图未更新，D NOT-RUN。下方“未提交/明天本地继续”是历史停点，不是当前状态。远端可用精确清单、离线构建/C helper、已核实依赖与接手顺序见[唯一现态与接续说明](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-b6-c-resume-20261001)；本地 artifacts 未上传，不能当远端证据。旧 tools/NuGet/原日志与二进制不上传、不清理，无部署/Stage。

- **2026-10-01 远端合并收尾**：按用户授权，将本地 `7154f658` 与远端 `d7970096` 无冲突合并为 `4e4aad06`，保留双方历史与 CivilWar 修复。合并候选 Debug/Release × 1.3/1.4 + Bootstrap、CivilWar smoke 通过；验证隔离旧产物引用，未改一键流程或部署。B6 未闭、C 原未通过和 D NOT-RUN 不变；未恢复扩展施工。[合并证据](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-remote-merge-20261001)。

- **最新现态，取代下方 2026-09-30 停止/串行指令**：用户已恢复 J17 B/C；P1–P4 Memory/Weekly 切片 `8c7b0fb0`、P5 Conversation/B7 产品候选 `aa3539ca` 已本地提交。该候选隔离 Debug/Release 双 API+Bootstrap 均 exit 0，代码地图 795 锚点通过；全量 C runner 273 项中 PASS 148、FAIL 78，另有预期/环境状态，**C 未通过**，B6 仍有具名未闭子项。D 实机/旧档 NOT-RUN。原 22 个未跟踪 `tools/` 与本轮 `NuGet/` 目录保留，不推送/部署/产品 Stage。[主台账最新实证与残余](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-multiagent-execution-20260930)及[B7 精确许可](docs/plans/j17-responsibility-closeout-plan.md#j17-b7-exact-move-permission-proposal-20260930)。

- **最新停点：新会话恢复后再次立即停止（2026-09-30）**：唯一执行者 `j17_delivery` 已被中断；HEAD 仍为 `c5edeca4`，B1a 未提交、未验收。执行者最后报告 sealing 回放 **84 PASS / 4 FAIL**（`actual day=10, expected day=21`），正在区分旧 oracle/fixture 漂移与产品回归，原因未定。现有产品/测试改动及 22 个 tools 目录全部保留。本次仅补交接，不恢复执行。[详细停点与恢复入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-resume-stop-20260930)。

- **用户已要求停止（2026-09-30）**：运行中子代理已中断，不再推进。最后提交 `c5edeca4`；B1a产品与测试留在工作树，双API/Bootstrap和部分定向回归通过，但sealing/terminal等收尾与包级验收未闭，未提交为完成；代码地图仍是旧产品基线。22个原未跟踪目录保留，无推送/部署。[停点、已验与未验](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-multiagent-execution-20260930)。等用户要求再继续。

- **J17-B/C 接续已启动**：现场 `84c4c0ff`，三个内置 `gpt-6-sol / medium` 执行者分别负责 B1a 产品、迁前兼容基线/迁后回归、B6 渠道前置测试；先冻结基线再改产品，总控独占提交与双构建。A20/20复用，C/D尚未执行，22个既有未跟踪目录保留。[唯一详细状态与所有权](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-multiagent-execution-20260930)。无推送/部署/Stage授权。

- **B6 前置测试切片已验**：真实源码 helper/回执 27、Lifetime 34、queue 40、gate 6组、提取8项通过；只改四个测试文件，整条游戏群聊/旁听、权威持久与网络取消未验，不代表B6迁移完成。B1a产品已在工作树，双API/Bootstrap初编通过，兼容回归未闭，暂不提交为完成。[详细证据](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-multiagent-execution-20260930)。

- **J17-A已完成（20/20）；验证入口隔离已获准并落实**：产品仍`8ae0f831`，责任证据E120–E133与795地图不变。六runner改为新输出、三业务工具默认不启动、编辑器使用合成根九步分支；六 C# 合计369检查、总入口13回归、编辑器9步通过，默认失效SDK已用单次本机SDK10覆盖复验。首轮mock日志误输出凭据已告知轮换，后续使用最小环境；不复述敏感值。B1产品迁移/C最终矩阵仍未执行，D NOT-RUN；原22个未跟踪tools目录保留，未推送/部署。[唯一详细状态](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-multiagent-execution-20260930)。

- **J17 本轮已启动（2026-09-30）**：现场基线 `8ae0f831`；用户明确选择五个互斥责任包并行补 A，总控独占提交。双 API 当前 Compile **1,128/1,128**，语法声明 **49,854/49,853**，不是语义完成。原文档改动纳入本地检查点，22 个 tools 未跟踪目录保持。[本轮唯一状态入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17-multiagent-execution-20260930)。先 A 后 B/C，未推送/部署。

- **最新文档交付（2026-09-30）**：新增[独立、可选的智能体执行计划](docs/plans/multi-agent-execution-plan.md)，总控分包、执行者连续完成、一次集成验收，不设常驻复审；J17 不强制多智能体。J17 仅修正证据重复审查、历史起点及每片全量测试成本，必要出口保留。[详细变更](docs/animusforge-refactoring-and-repository-reorganization-plan.md#optional-agent-plan-20260930)。未启动代理/产品实施，J17-A/B 状态不变。

- **合并后未测清单（2026-09-30）**：用户已推送 `8ae0f831`；MCM 合并未做，周报第二种模式（`TitleShortTagsOnly` 短报，无 MCM 开关、按距离取最近 3 国出全文）、内战叛乱派系（`_af_kingdom_civil_war_v2`，MCM“12b. 内战派系”）、宣权篡位政变均**未测**。定位与逐项验证要点见 [交接](docs/handoffs/2026-09-30-j17-remote-sync-handoff.md)。

- **当前交付：远端合并与移除未完成 WorldBulletin（2026-09-30）**：按用户要求保留本地提交与远端 `391ceb74` 的其他变更，只撤销快报开关/接线并恢复旧周报行为。双 API、Bootstrap 构建通过，CivilWarRules / WeeklyReportSchedulePolicy smoke PASS；原 22 个未跟踪目录不动，未部署，游戏/旧档未验。普通推送同名分支获本轮授权，提交与远端状态以 Git 为准；[主台账新结果](docs/animusforge-refactoring-and-repository-reorganization-plan.md#remote-merge-no-bulletin-20260930)取代上一轮编译阻塞，J17-A/B 总状态不提升。

- **J17-A 最新接续（2026-09-30）**：[范围图 E119](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)将旧 null 导航全部**非根级 26 文件/两 API 各 872 同坐标成员**去重：`Refactor/` 23/767 = Contracts 340 + Adapters 149 + Modules 107 + Runtime 171，`src/` 3/105 = E109 Actions 75 + Scene 后处理 30。Scene 的状态/一次完成门、同步请求、deferred queue、Action commit、规则准备各自有界，实际效果/记忆不转移 owner。当前 J09 **25 checks**、Scene 提取 **8 tests**、Notoriety **14/14**、Bridge 元数据 validator **16/12/4** 通过；非双产品/网络/旧档/游戏。R05 迟到完成、R08 每回复线程/Agent/规则成本、R09 完整 RAW/FINAL/归一化回复日志隐私仍 OPEN；旧导航根级 254/11,803 须与已有 E 证据去重，动态消费者和另 13 桶待签，**仍 7/20、current unknown 未证为 0，J17-A 未闭、B 未启动**。产品/测试/构建脚本未改，22 个既有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E118](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)核旧 null 导航 280 文件/两 API 各 12,675 成员的路径分母（根级 254/11,803、`Refactor/` 23/767、`src/` 3/105），并将其中 `InteractionContracts` **184** + `LegacyInteractionSnapshotAdapters` **48** = **232 同坐标成员**按身份/Prompt/Action/Memory 合同及配置/三渠道主线程捕获/Memory-Weekly 委派互斥分区。Coup 反射内部 Memory 构造需兼容，契约 DTO 不持游戏效果或保存权威。直接编合同文件的 `InteractionPipelineContractTests` 七组 **40/4/69/39/10/18/13 PASS**，但未编 SnapshotAdapters；请求期 Agent/历史/Hero fallback 成本、退化捕获/晚结果和 RAW/反射消费者仍 OPEN。旧导航未更新且其余 278 文件已有零散 E 证据待去重，**不能称 current unknown=0；仍 7/20、另 13 桶待签，J17-A 未闭、B 未启动**。产品/测试/构建脚本未改，22 个原未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E117](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)签收获准范围内 `policy-political` A 责任桶，累计 **7/20**：当前双 API 78 文件/各 5,547 成员中，`PolicySystem/` 实为 **77 文件/5,542 成员**，按用户更正整体排除而非已审政策业务；AF 唯一候选是无状态 `PolicyModuleAdapter` 5 成员/四原样委派，七个当前 Native/Scene/Courier/对话端口调用已定位，其他直接政策接缝仍归各宿主。旧端口 runner 在历史两处 Courier Policy 调用与当前一处比对时报失败，随后独立编译因测试源缺新 CivilWar port 报 `CS0246`；隔离生成副本仅去掉无关 CivilWar 属性，真实 port/adapter **308 桩断言 PASS**，不冒充当前完整 Host/游戏。Courier 一次且不漏、请求成本及反射消费者仍 OPEN；**另 13 桶与全局未知成员待审，J17-A 未闭、B 未启动**。产品/测试/构建脚本未改，原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E116](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)将 `knowledge-persona-profile` 旧导航双 API **13 文件/各 908 同坐标成员**首轮分作 Knowledge 宿主 531、王国战略档案 146、Persona 37、Knowledge helper 194；战略档案实际归 WorldDiplomacy/Policy 画像而非 Knowledge/Persona。E100 世界实体 267、ONNX 资产 15、Persona 快照 10 另计。Entity **32 checks**、Persona **269/0**、独立隔离 Index/Lore/Import **70 checks** 通过；开局结果 flag 竞态仅源码推断，手动导入全目录扫描、导出非原子 fallback、输入日志与三渠道晚结果/动态消费者仍 OPEN；未跑会清理既存产物的原 runner、真实用户文件/双产品/旧档/实机。**总桶未签，仍 6/20，另 14 桶及全局未知成员待审，J17-A 未闭、B 未启动**。产品/测试/构建脚本未改，22 个原未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E115](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)把 `memory-afef` 旧导航剩余十文件 **136 成员**与 E111 的 278 合为双 API 各 **414 同坐标成员**：Records 40、进程级 receipt cache 8、Summary 内部调度 88、H 恢复 278。EventSourceMaterialIndex 归 Weekly 材料派生索引；`MemoryCommitReceiptCache.TryAccept` 的返回被生产 Commit 忽略，静态 512 缓存不能充当一次提交门或跨 Campaign 保证。记录规则 **34 checks**、dispatch **37 checks**、run lease **47 checks**、fingerprint **9 vectors/5 guards** 通过；材料索引 runner 因旧方法名提取漂移在 build 前报 `ValueError`，未验索引行为。R02–R04 根级 MyBehavior 状态/动态消费者、R07/R08/R09 具名余项及旧档/双产品/实机仍 OPEN；**总桶未签，仍 6/20，另 14 桶及全局未知成员待审，J17-A 未闭、B 未启动**。产品/测试/构建脚本未改，22 个原未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E114](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)复核 `persistence-config` 旧导航双 API 八文件各 **543 同坐标成员**：通用 I/O 99、Knowledge ONNX 资产 15、`AIConfigHandler` 跨 Prompt/Conversation、GameAdapter、Gateway、Knowledge 的 429；终端独立设置 17、UI registry 36、E96 家族内 TerminalSave 2 和 Prompt DTO/请求配置另归原 owner。只读 validator **5 cases PASS**、内存 chunk replay **8 项 PASS**；词法消费者不是 MCM/JSON/反射及三渠道线程闭包，`PlayerExportsStore.ClearJsonFiles` 顶层 JSON 删除和手动导出发布保持具名数据安全风险，未在真实用户目录运行写盘 runner。未读写真实配置/旧档、未跑双产品/实机；**此桶未签，仍 6/20，另 14 桶和全局未知成员待审，J17-A 未闭、B 未启动**。产品/脚本未改，22 个原未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E113](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)签收 `duel` **A 责任归属**，累计 **6/20**：双 API 七导航文件各 1,567 成员唯一分作 Duel 请求/终局/回执 658、Fourberie GameAdapter 守卫 39、跨域 MCM 870；GCCZ 城堡决斗、Meeting Finalizer、旧聊天候选另归原 owner。源码链接 Outcome **20/20**、Dispatch **16/16** PASS；现存双版产物回放 **33 PASS/2 FAIL**，两失败仅为 `AnimusForge.csproj` 比 Sep27 marker 新的 freshness，不当成当前 DLL 验收。赌注身份/野外结果时序、Fourberie 局部 patch 失败与外 MOD 回调误抑制、跨 Campaign/热路径继续 OPEN；未跑真实 Harmony、战斗/经济/旧档。**另 14 桶与全局未知成员待审，J17-A 未闭、B 未启动**；产品/脚本未改，原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E112](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)把旧 null 导航的根级 Prompt 配置 DTO **14 文件/双 API 各 136 同坐标成员**归到六 JSON schema/话题规则输入，不误归通用 Persistence 或 Duel/Reward/Loan 玩法；E49 的 Conversation 请求配置 10、E96 两个 DuelSettings partial 121 均不双计。源码链接 loader 真源码/模型 stub **36 checks**、真 DTO+snapshot **18 checks** 通过；未读真实用户配置或跑三渠道/双产品/实机。`PostprocessRuleEntry` 的运行时允许参数 `[JsonIgnore]` 不可丢，DTO 深克隆/请求期复制成本及其余动态消费者仍 OPEN。**`persistence-config`/`gateway-prompt-protocol` 未签，总进度 5/20、J17-A 未闭、B 未启动**；产品/脚本未改，22 个原未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E111](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)对 `memory-afef` 的 H 恢复账本 219 + MyBehavior 接线 59 = 双 API 同坐标 **278 成员**首轮分区；Daily/Recent 投影见证和真实集合/SaveSystem 分责，Notoriety 只在初次完成后委派 Social owner，Action/Weekly 不从恢复账本重播。源码链接恢复合同 **PASS**，但仅编账本，非游戏或旧档。R07 裁剪后 marker 与可见文本区别、R08 Tick/载入扫描和 JSON 克隆、R09 `AFMR1:` 待恢复文本隐私均 OPEN；导航余十文件 136 成员及根级历史仍待审，**未签 memory 桶，总进度 5/20，J17-A 未闭、B 未启动**。未改产品/脚本，原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E110](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)补查 `game-adapter-compatibility`：旧导航三文件各 47 成员、七个混合接缝各 89 成员在双 API 同坐标；对各 1,123 实际 Compile 路径的手工 Harmony **词法**扫描命中 75 文件/211 处文本，其中旧 null 候选 42 文件/96 处。它们不等于实际安装/成功目标，也不能与 E93 的 77 属性点相加；**R09-Harmony-ManualClosure** 须按成员、注册链、目标与失败重试逐项归领域 owner，再做双版真实安装核对。故兼容桶仍**未签**，总进度 **5/20、另 15 桶和全局未知成员待审，J17-A 未闭、B 未启动**。未改产品/脚本，未运行 Harmony、双产品构建、旧档/实机；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E109](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)按双 API Actions 九文件各 **167 成员**签收 `action-commit` A 责任桶，累计 **5/20**；旧导航七文件 92 漏 Native Executor 43/Result Committer 32，错把 Conversation Detached Host 18 归 Actions。真实 Native/Scene/Courier 入口已追到，Economy/Duel 游戏效果、Memory/AFEF/Weekly 状态及 Conversation 生命周期保留各自 owner。仓内 .NET 8 源码链接七组合同行为 **4/40/69/39/10/18/13 PASS**，`game=NOT_RUN`；首次系统 .NET 10 `NETSDK1127` 已诊断为目标包/工具链不匹配，未当产品失败。R07 legacy 双 Append 单边写、静态 512 receipt 跨 Campaign/满槽和混合动作部分效果、R08 多次 RAW 扫描/指纹分配继续 OPEN，非旧档/实机验收。另 **15 桶与全局未知成员待审，J17-A 未闭、B 未启动**；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E108](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)按双 API 各 387 成员、13 个实际导航候选文件签收 `runtime-diagnostics` **A 责任归属**，累计 **4/20**；Foundation 145 + Diagnostics 144 + FeatureLog 28 + CampaignTick 70 对账，另六根级混合接缝 308 成员不双计。checkpoint→世界 AI 18 次跳过、Sentinel 异常压制、错误 UI/网关和领域日志内容仍由相应 owner 唯一负责。R07 正常退出残留 ENTER 误判/安装不重试及其它晚结果/清理竞争、R08 真负载、R09 明文/覆盖/跨会话隐私均 OPEN；签桶只证明静态责任，不是已修或实机/旧档验收。离线目录 44、生命周期 36、J02 6、Campaign 恢复桩 PASS，后者 `live=NOT_RUN`；未碰真实用户日志/checkpoint、网络/dump/游戏。**另 16 桶与全局未知成员仍待审，J17-A 未闭、B 未启动**。原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E107](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 `CampaignTickDiagnosticsPatch` 双 API 同坐标 **70 成员**九段首轮分区，追到默认 HostRuntime bridge、每帧 1 秒写额度、Logger checkpoint 覆盖和世界 AI Safety Prefix 的 18 次同 ID 原版 hourly AI 跳过；[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)新增 **R07-Checkpoint-FalseCrash-AiSkip**：ENTER 可独占写额度、EXIT 未落盘，正常退出前残留 ENTER 也可能在下次启动被当崩溃；须受控文件/两版 Harmony 时序验证，不称实机已发生。安装前置 `_patched` 的部分失败不重试、static ID 跨 Campaign、每秒活 Party 事实/同步文件与隐私亦保留。源码提取回放 **PASS**（18 次跳过，第 19 次恢复，`live=NOT_RUN`），未测试磁盘误判；没有读取/改写真实 checkpoint、运行 Harmony/实机。`runtime-diagnostics`、世界 AI 安全桶仍未签，累计 **3/20、J17-A 未闭、B 未启动**；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E106](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 Foundation.Runtime.Diagnostics 的 PerformanceWindow 29、TraceContext 16、MetricWindow 13、BoundedLogWriteQueue 30、FreezeWatchState 56，共双 API 同坐标 **144 成员**逐段首轮分区；[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)保留动态名称字典基数/逐帧锁与 30 秒排序、普通日志异步 writer 出队后丢批或与清理覆盖竞争、Freeze 静态环的跨会话上下文隐私反例。当前 J02 源码链接六条 **6 PASS/0 FAIL**、保护区检查 **PASS**，非真实日志/竞态/实机。没有触发清理、dump 或用户文件；Campaign checkpoint 与世界 AI 安全跳过及动态消费者尚待审，`runtime-diagnostics` 未签，仍 **3/20 桶、J17-A 未闭、B 未启动**；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E105](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把错误分析 Inquiry 40、非阻塞错误报告 16、通用 Feature 日志 28、PerfProbe 25，共双 API 同坐标 **109 成员**按互斥范围首轮分区；[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)具名保留 60 秒网络分析跨 Campaign 迟到且无接受门、后台读取 MCM、四键替换非通用脱敏、重复失败确认回调无界积压、桌面导出/轮转/清空覆盖日志及逐帧/事件级性能成本。仅静态源码和直接消费者；**未调用网络、未读取/导出/清空用户日志、未触发文件轮转**。E102–E104 仍有效，Campaign checkpoint 与 Foundation 五诊断文件及动态消费者尚待逐成员复核；`runtime-diagnostics` 未签，累计 **3/20，J17-A 未达出口、B 未启动**。未跑双产品构建、旧档/实机；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E104](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 `FreezeWatchdog` 67 + `BannerlordExceptionSentinel` 61 = 双 API **128 成员**按段首轮分区，诊断写盘/缓存与 GameAdapter Harmony 异常传播权威分开；[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)纠正 E36 的过宽“保存关键异常均排除”概述：仅自动生成 callback 路径显式检查，生命周期/Dispatcher Finalizer 的同类失败待具名回放，不称已证实旧档损坏。Sentinel patch 安装前占键、失败可能不重试；Watchdog 的进程级 1 秒轮询、保存状态缓存与 dump/快照隐私均留 R07/R08/R09。**未运行 Harmony、dump、日志或实机**。Logger E103 与 Foundation E102 仍有效；`runtime-diagnostics`/兼容桶未签，累计 **3/20，J17-A 未达出口、B 未启动**；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E103](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)将根级 `Logger.cs` 双 API **99 成员**五段首轮分区；[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)具名保留默认开启的 Token/Event 明文请求/回复日志、请求期逐消息反射与无界 Token 队列、默认每三天覆盖七日志的现有数据清理策略。J02 只读源码保护区检查 PASS，不等于全 Logger 运行回放；**没有读取/清除用户日志或改设置**。E102 的 Foundation.Runtime 145 成员分区仍有效，但 Watchdog/Sentinel/Inquiry、checkpoint 世界 AI 安全跳过及动态消费者未全审，诊断桶未签；仍 **3/20 桶，J17-A 未达出口、B 未启动**。未跑双产品构建、实机/旧档，22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E102](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 Foundation.Runtime 生命周期/同 DLL 目录/调度六文件双 API **145 成员**首轮分区，连到实际 Game 退役、FeatureBridge 能力目录、PublicApi 快照和 Shout/Courier 释放；不是“全为日志”或第二 Host/队列。当前源码链接目录测试 **44 PASS/0 FAIL**、生命周期 **36 checks/0 failures**（退役业务体为 fixture）；首次 `--no-restore` 因缺 `project.assets.json` 失败，按项目空源 `NuGet.Config` 离线 restore 后通过。仅静态/桩，未跑双产品构建、真实 Campaign/旧档/实机；Logger/Watchdog/Sentinel/Inquiry 与诊断→世界 AI 安全跳过仍未全成员复核，`runtime-diagnostics` 未签。累计 **3/20 桶，J17-A 未达出口、B 未启动**；22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E101](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)与[主台账 §3.3](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)将非 Compile `tools-content-package` 签为第 **3/20** 桶：195 条内容源/目标唯一、22 个原资源 owner、两版 7 个内嵌资源、Stage/ZIP 精确交付边界与 tests/tools/content/scripts/docs 跟踪路径已核；五个 J15 只读契约和实际 layout 195 条 PASS。未运行会写/清理的完整 J15 runner、Stage、ZIP、部署、产品双构建或游戏；模型/TPAC/私有依赖来源与再分发、旧档/发布仍 HOLD/NOT-RUN，资源业务归原 loader，不因本桶签收转归 Tools。**其余 17 桶、全局 Compile 成员和动态消费者仍未全签，未知不为 0，J17-A 未达出口、B 未启动。**此前 E100/E99 条的“2/20”是当时状态，以本条/主台账为准。22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 最新接续（2026-09-30）**：[范围图 E100](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 `WorldEntityRetrievalService.cs` 双 API **267 成员**按九段首轮归责，接续 E33 的关键链；[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)具名保留捕获 live 引用跨 worker 到最终拼装的身份重验，以及无 mention 也枚举可见队伍、四类全集别名/每 mention 评分与分段预算非单请求硬 3 秒。E99 的 Knowledge 宿主 531 成员分区仍有效，但两者不构成 Knowledge 桶闭包；仅静态源码/成员/消费者，未跑双产品构建、三渠道时序、旧档/实机。仍 **2/20 桶，J17-A 未达出口、B 未启动**；22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E99](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)将 `KnowledgeLibraryBehavior.cs` 双 API **531 成员**按十一段首轮归责，区分保存/索引候选、最终活游戏 Lore 拼装、资料包导入、玩家介绍/UI 与 RAG 短句 Gateway；[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)具名保留预检输入对象别名、同步 UI 网络等待、请求期全规则/映射扫描及诊断原文。最终活 Hero/Settlement 拼装不可因 worker 候选路径而误称 worker-safe；此片仅静态源码/成员/消费者，不是双产品构建、真实 HTTP/UI、旧档或实机。Knowledge 桶与其余 18 桶未签，仍 **2/20 桶，J17-A 未达出口、B 未启动**；22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E98](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)将 `DuelBehavior` 三文件宿主 partial **437 成员**及主文件另 13 个独立 Harmony patch **33 成员**合计 470，按当前双 API 逐段归责；[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)具名保留 pending 债务/赌注先移再比 DuelOutcomeId、野外结算先标完成而 typed result 失败后的状态缺口，以及 Economy/Memory 多步副作用反例。仅静态源码/成员/直接消费者，不等于 Mission、Harmony、死亡、旧档或双产品构建验收；Duel 桶与其余 **18 桶未签**，J17-A 总出口未达、B 未启动。22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E97](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 `AIConfigHandler.cs` 双 API **429 成员**分十二段首轮归责，分清 Prompt 配置/reload、规则和后处理语义、活游戏事实捕获、Gateway provider/交互重试与 Knowledge 桥；[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)具名保留后处理地点事实主线程/同轮身份、`CancellationToken.None` 同步等待及重试寿命、城外每次全排序 `Settlement.All` 和原文日志暴露风险。E96 的 `DuelSettings` 1,002 成员分区有效；两文件不等于配置/Prompt 总桶签收，仍仅 **2/20 桶**，J17-A 总出口未达、B 未启动。仅静态证据，双产品构建、三渠道端到端、旧档/实机未验；22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E96](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)将 `DuelSettings` 六文件 partial 家族的双 API **1,002 成员**按配置身份/Prompt 文件、模型目录/缓存、UI/领域 MCM 和 Gateway 接缝完整首轮分区；[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)登记模型刷新 `Task.Run` 续体改 MCM/UI、默认迁移 marker 部分提交、高频 `GetSettings` 的文件 metadata/锁成本及 API Key/原始响应日志风险。此家族分区**不签 `persistence-config`**；`AIConfigHandler` 429 成员、其它配置与动态消费者未闭，仍仅 **2/20 桶**，J17-A 总出口未达、B 未启动。本片仅源码/清单/消费者静态核对，未做双产品构建、实机/旧档；原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E95](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把旧 host 候选 239 成员和四个根级宿主/桥 53 成员双 API 对账为 **77 host + 16 UI/Config + 199 已指名其它 owner**，据此在[主台账 20 桶](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)正式签 `host-composition`，累计 **2/20 桶**。Courier ModuleInitializer/Harmony 旁路注册与宿主 Tick 异常传播/帧成本留具名余项；签收仅指 A 语义归属，不是产品构建或实机。E94 的 `LordEncounterBehavior` 384 成员分区有效，另 18 桶及全 Compile/动态消费者仍需复核，**J17-A 总出口未达、B 未启动**。原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E94](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)将 `LordEncounterBehavior.cs` 双 API **384 成员**按七段完整首轮归责，更新[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。具名保留军团附属成员待结算身份可能被主队 leader 清除、外交效果先标一次性、投降 loot roster 先移后转俘、静态状态跨 Campaign 退役，以及会面 `[ACTION:...]` 注入主链而非仅后处理的既定合同冲突；均未在 A 片改生产语义。仅源码/成员/直接消费者证据，未跑双构建、Harmony/Mission、旧档或真实三渠道。Bootstrap A 桶仍为 1/20，其余桶、全 Compile 与动态消费者未闭，**J17-A 未达出口、B 未启动**。原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[主台账 20 桶](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)将 `bootstrap-build` 基于[范围图 E37](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)及当前只读 MSBuild 输入正式签为 **A 责任桶 1/20**：独立 Bootstrap 编译 3 文件，两套实现各 1,123 文件且不含其源码，共用 AssemblyInfo 为 0 成员；XML/相关源码与脚本相对 `944712f8` 未变。只签语义归属，不等于构建/实机/发布；另外 19 桶、全 Compile 成员与动态消费者仍须对账，**J17-A 总出口未达、B 未启动**。原 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E93](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)将实际 Compile 路径的 77 个 Harmony 属性点对账为 69 声明类型 + 8 方法目标属性：2 父标记不单装、3 类仅见定义无仓内注册、余 64 类有启动可达安装路径（1.4 明确跳过其中一个，故最多 63）；见[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)。这是静态入口，不等于成功安装；无属性动态 patch、目标签名/失败、外部补丁与实机仍未验。原 22 个未跟踪 `tools/` 目录未动，未推送/部署；**兼容桶及 J17-A 未签，B 未启动**。

- **J17-A 前次接续（2026-09-30）**：[范围图 E92](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把七个版本适配/Mission 防护/启动 audit 文件 **89 成员**按双 API 对账，并对当前实际 Compile 的 **49 处 `#if`/25 文件**分域；见[主台账 A2](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)。新具名余项是 1.4 场景主线程队列每 Tick 全量排空（1.3 有数量/毫秒预算）、Mission Finalizer 误吞异常反例、一次性兼容 audit 只有日志信号；贸易/raid/SpawnTroop 请求期重复反射沿 E39。完整 Harmony 安装（当前 77 属性点）/动态消费者、产品双构建和真实 Mission/Encounter 未验，**兼容桶及 J17-A 仍未签，B 未启动**。原有 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：[范围图 E91](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)完成 `KingdomAnnexationBehavior.cs` **37 个成员**六段双 API 首轮归责，核对三渠道规则/Actions 标签入口、Vassalage 断约及 Clan/和平/灭国原版动作；见[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。具名余项是逐 Clan 部分转移后先断约/议和再判残余、初始领地快照异常可让检查缺项，以及本地只验国王身份而同意仍依赖后处理标签。须用失败/重复标签/旧档与真实战态反例，当前并非实机故障结论。E90 NPC 桥 75 成员和 E85–E89 Vassalage 文件 581 成员仍有效；世界桶/其余 Compile/动态消费者/20 桶语义未签，**J17-A 未闭，B 未启动**。未推送/部署，22 个原有未跟踪 `tools/` 目录未动。

- **J17-A 前次接续（2026-09-30）**：[范围图 E90](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)把 `NpcTributeVassalageBehavior.cs` 75 个双 API 成员分六段首轮归责，并核对 Harmony 议和/宣战事件、共用条约桥与日志消费者；见[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。新增具名余项：新约先存再同步战争/通知的部分提交、ThreadStatic 单槽在嵌套议和时的快照覆盖；和平 Prefix 双方 Clan 国力刷新与同步 JSONL 写盘需按事件规模量测。仅静态证据，不是旧档/Harmony/游戏验收；`VassalageBehavior` 文件 E85–E89 的 581 成员首轮分区仍有效，世界桶/全部 Compile 成员/动态消费者/20 桶语义未签，**J17-A 未闭，B 未启动**。22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：产品源码 `944712f8` 的 `VassalageBehavior.cs:5724–8531` 尾段 **147 个成员**双 API 六段首轮归责完成；[范围图 E85–E89](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)现覆盖同文件 peer 214 + 宿主 367 = **581 个**，仅此文件首轮分区完成，非世界桶签收。见[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。新增具名余项为庇护通知先消费后领域效果/宣战状态、即时原版外交失败未必排队重试；王国灭亡结算日错键因正常断约/载入清理而收窄为本会话/旧档反例。待外交队列一秒一次仍全量处理，须量测单次积压与王国扫描。无产品双构建、旧档、实机、真实 UI/Harmony；其余 Compile 成员、动态消费者及 20 桶语义未签，**J17-A 未闭，B 未启动**。原有 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：在源码 `944712f8` 上续审 `VassalageBehavior.cs:3675–5723` 条约/通知/战和同步/Policy 桥 **42 个宿主方法**，双 API 同坐标；连同[范围图 E85–E88](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)的 peer 214 与宿主前 220，本文件 581 个中已有 434 个首轮分区，**宿主余 147**，不等于文件/桶签收。见[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。具名保留条约跨 Policy/原版战和生命周期、AF Policy 提交按状态观测推断、地图通知先标 session 再 AddNotice、王国灭亡按裸 ID 清结算日但实际键为 AgreementId 的反例；E87 贡赋部分提交及 E86 每帧筛查/终端全表成本仍开放。未运行产品双构建、旧档或实机；其余 Compile 成员/动态消费者与 20 桶语义未签，J17-A 未闭、B 未启动。22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：在产品源码 `944712f8` 上完成 `VassalageBehavior.cs` **214 个非宿主 peer 成员**的双 API 首轮责任分区，七段无漏/重；同文件总计 581 = peer 214 + 宿主 367，**E85 本条未审宿主**。见[范围图 E85](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)及[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。条约/旧枚举/JSON、Policy 外部提交接缝、通知记录与终端/UI/Harmony 适配分别归责，五张图确由 `content-map.json` 投射统一模块。具名保留日志通知 patch 未见注册及 UI 刷新 patch 标记先置而失败不重试，须以运行时 Harmony/UI 验证；非已证实游戏故障。未运行产品双 API 构建、真实 UI、Harmony 列表、旧档或实机。E86 后宿主仍余 212、其它 Compile 成员/动态消费者与 20 桶语义未签；J17-A 未闭、J17-B 未启动。原有 22 个未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：当前产品源码 `944712f8` 上完成 `PlayerNotorietyBehavior` 三个 partial 的 **363 个成员**首轮责任分区：主文件 323、`ConversationOutcomes` 27、嵌套 ObservationOwner 13，双 API 成员/坐标一致，见[范围图 E84](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)和[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。具名保留摘要异步迟到接受、旧 JSON 归一化碰撞及请求期重复全表归一化/活 Hero 查找的反例与 R08 量测。纯 `AFNR1` receipt 合同 14/14 通过，**不执行宿主**；未运行当前候选产品 DLL、双 API 构建、旧档或实机。连同 E82–E83 军演切片，仍有其余具名宿主、所有 Compile 成员和 20 桶语义/动态消费者需对账到未知 0；J17-A 未闭、J17-B 未启动。22 个原有未跟踪 `tools/` 目录未动，未推送/部署。

- **J17-A 前次接续（2026-09-30）**：当前 `944712f8` 产品源码上续审 `MyBehavior` 具名片段、Romance 162、SceneTaunt Campaign 宿主 132、TroopInspection 宿主 188、MilitaryExercise 宿主 238 及其同文件 peer 72 个成员；16 个具名宿主双 API 分母各 13,380（10,268 direct + 2,998 嵌套 + 114 根 partial 类型），军演同文件总计 310，`DuelSettings` peer 的七成员已在 13,380 中，不能重复相加。证据见[范围图 E72–E83](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)，逐宿主状态及具名 R07/R08 余项见[主台账 A2-5](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)。分母不是已审量；E78/E80–E82 为四个宿主首轮分区，E83 为军演 Mission/Harmony peer 首轮分区，Taunt/Inspection 的 peer 及动态/实机均未闭。军演具名余项含 roster 返还、Encounter 身份、跨 Campaign 状态、败方结算断链与 patch 安装失败；Tick 需量测每帧两次 Agent 扫描和每 3 秒按 ID 重扫 party，均未证实为实机故障。Taunt 纯 owner 回放 22/22、16/16、14/14 通过；Inspection/Exercise 的候选 DLL 回放本轮未跑。**下一步仍是其余宿主、全 Compile 成员与 20 桶语义/动态消费者对账**；R06-M0 保持 `HOLD_USER_DECISION`，A 未闭、B 未启动。未运行双 API 产品构建、游戏或旧档验收；已有未跟踪 `tools/` 目录原样保留。

- **J17 当前接续**：[J17 执行计划](docs/plans/j17-responsibility-closeout-plan.md)；本工作区 A0 双 API Compile/语法清单各 1,123 文件、49,509/49,508 声明；A1 已复核 R04/R02/R03/R01 关键路径并更正旧“纯净化/纯状态变换/仅 DeathLine 读游戏”结论，R04 又逐字段核对 11 类/127 个公有字段及别名/时间语义。A2 已定向复核 `Refactor/`、四功能 Tick、GCCZ/Policy 范围、六个跨桶接缝及 R05 三渠道生产主链（Native 默认入口与 Scene/Courier 开关网关不可混同）；旧候选索引 843 文件有导航桶、280 文件/12,675 声明无候选，其中 28 个根级文件已有后续定向复核，**全量语义审查仍未签收**，详见[主台账当前段](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j17a-intent-20260928)与[范围图 E14–E29](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)。`OwnerJsonStorageCodec` 8 项、Memory RunOwner 47 项、修复夹具编译依赖后的 captured 116 项、terminal 85 场景通过；别名语义两个变异按具名断言失败。旧 source-parity 经基线路径重映射复核：Planning/Input 精确，但 MyBehavior 后续源码约 8,706 行差异，**未通过且不可只刷 hash**。**按用户更正，先完成 A 再做 B；B 未启动**，没有产品构建、游戏或旧档验收。

- **J17-A 本次 R05 回放**：[范围图 E29–E30](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)；Native completion 191/0、Scene postprocess 差分 71 fixture、Courier detached postprocess owner 44/0、Courier domain commit 静态 32 检查。均非真实游戏/完整三渠道端到端，R05 与 A 仍未闭；下一步审群聊/入站/失败、规则与 AFEF 回读，再补 20 桶，先 A 后 B。
- **J17-A 入站新余项**：[E31](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)默认入站 History/AFEF 经无回执旧门面，交付置位后无法静态确认写入失败的处理；`AFCI1:` 是未见默认调用者的 detached opt-in 契约。须先复现并确定失败语义，非已证实丢失。回执 tick 每 0.75 秒全量过滤排序 session 需 R08 规模测量；Courier Prompt 552/76、入站回执契约、Scene group receipt 18 离线检查通过。仍先 A 后 B，不改产品。
- **J17-A 检索续查**：[E32–E33](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)根级 Prompt 列表服务 58 声明完成文件级语义审查；世界实体服务 267 声明只审捕获/匹配/投影关键链，未逐项签收。R08 具名项为请求期全候选别名/最多两轮 `MobileParty.All` 扫描及 live 对象静态快照的跨 Campaign 失效核查。Prompt 定向 150 检查、Knowledge 提取/桩性能和分配回放通过；未运行实机、双 API 产品构建。旧候选 JSON 的 280 未分桶仍是导航快照；A 未闭，B 未启动。
- **J17-A Native 续查**：[E34](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)15 个根级 `ShoutBehavior.*` partial/274 声明完成文件级责任审查；其中 Campaign 退役与其余 Native/Scene 适配分界清楚，但主文件 1,716 声明、全部动态消费者仍未闭。新具名生命周期余项：静态行刑回调未见 Campaign 退役清理，需复现。NativeTurn 112、Admission 44、Module API 55、Knowledge schedule 8、ChannelPersona 169 定向检查通过；非实机/完整链。A 未闭，B 未启动。
- **J17-A 四功能接缝**：[E35](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)核对双 API 内置四功能 164 文件/6,210 声明及宿主装配；Coup bridge 持有存档收据并反射 `MyBehavior`，DialogueUI/Illustrator 也有 Native 字符串反射合同。宿主相位回放通过，但未逐审四功能全部声明、实机/旧档和模块互斥；A 未闭，先 A 后 B。
- **J17-A 诊断续查**：[E36](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)四个根级混合诊断接缝 267 声明与 Foundation 五文件 144 声明分界已核对；J02 基础 owner 回放及六个变异通过。可选 Token Stats 队列无容量门、可落盘原文；错误 AI 分析的四键替换不是通用脱敏。根级全成员与真实负载/隐私路径仍待验；A 未闭，B 未启动。
- **J17-A Bootstrap 桶**：[E37](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)当前 Compile/XML/装载源码闭包确认三文件独立 Bootstrap 与唯一入口，`bootstrap-build` 的 A 责任归属已审；每帧反射转发留 R08 量测。未运行会重置产物目录的一键脚本或实机。
- **J17-A 宿主装配桶**：[E38](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)引擎/生命周期、八个 Composition、typed 目录和内置薄桥的 A 归属已审；SubModule 混有 API 指引/WarStats UI，旧候选误把请求期 pipeline 工厂及六个领域/API 合同归 host，已在范围图纠正。Campaign 旧测试 oracle 漏 CivilWar，现全 runner 46 断言/6 变异、Host source inverse 通过。余下 18 桶、R05/R06/R08、游戏/旧档尚未闭，先 A 后 B；未推送或部署。
- **J17-A 兼容层续查**：[E39](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)三文件旧候选混合版本签名、Encounter 战后资格和 Mission view 异常防护；Coup 私有遭遇 helper 缺字段时 fail-closed。全量 `#if`/Harmony/启动 audit 未审、请求期反射频率未测，故兼容桶与 A 未闭，B 未启动。
- **J17-A 入站信失败回放**：[E40](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)当前生产入站送达与旧 void Memory 门面经源码提取在合成桩中跑通：拒收/异常/无 owner 仍推进 `DeliveryApplied`、信件及结束；删去 History 调用变异按具名断言失败。E31 的“待合成复现”已被取代，实机事实丢失未证。B6 须先定失败/部分写入及一次性交付恢复合同，不盲目重试；Courier route 与 Campaign composition 两个旧 PREEXISTING_FAIL runner 已修复/重跑 PASS。A 仍未闭、B 未启动，未推送/部署。
- **J17-A 诊断/世界 AI 边界**：[E41](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)prior-crash checkpoint 可按队伍 ID 设置 18 次原版 hourly AI 跳过；当前方法提取/桩回放和忽略跳过变异通过。此为诊断状态与世界 AI 安全策略混合责任，静态 ID 跨 Campaign 失效及实机影响未验；`runtime-diagnostics` 未签，A 仍未闭，B 不启动。
- **J17-A R04 记录闭包续查**：[E42](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)核对双 API 实际 Compile 的 9 个生产消费者文件、11 类/127 字段、9 个直接记录存档键，以及 `PlayerExportsStore` 的单人/整包 `CompressedMemoryExportBundle` 读写。`NpcActionEntry` 兼供行动写入、Weekly 和 major 摘要，Weekly trigger 含 outcome 收据，导出 bundle 属 R03；不可把 11 类整体搬进 Memory/Records。现有反射回放依赖旧嵌套名，净化有原地/clone 与时间副作用。旧档/真实玩家导出未验，R04 和 A 未闭，B 未启动。
- **J17-A Actions 小桶续查**：[E43](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)核对实际 Compile 的 Actions 模块 9 文件/167 声明及生产调用；旧候选漏 `LegacyNativeActionPlanExecutor`/`InteractionResultCommitter`，误把 Conversation `DetachedInteractionHost` 归 Actions。生产文件直接链接的 InteractionPipeline runner 本轮七组 PASS（40/69/39/10/18/13/4）；静态 512 回执缓存跨 Campaign 清理未见，R08 待复现。`action-commit` 共享层 A 归属已审；渠道/领域效果分派留 R05/R07，**全 A 未闭、B 不启动**。
- **J17-A R02 摘要闭包**：[E44](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)按当前源码确认 daily/major/overview 捕获→规则/解析→重试→接受及 Native/Notoriety/Weekly 副作用，A1 归属已审；`run_business.py` 36/0、`run_terminal.py` 85/0、`run_captured.py` 116/0 本轮通过。开发者编辑器不只是 UI：草稿/块/总览改权威状态和历史；总览与块编辑回调未自行重验代际，迟到后实际影响仍待复现，留 B4。1,000 记录捕获仍为未分片单 job，R08 硬帧预算未证。R02 的 B2/B3 未迁，A 总出口未闭，B 未启动。
- **J17-A R03 导入导出闭包**：[E45](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)核对压缩记忆六个 Apply 消费点、五域覆盖/合并与 dirty，以及四个导入入口代际门；`All` 仍是旧 `dialogue_history`，不含 `compressed_memory`，须保留范围差异。当前源码提取合成回放 51/0，绕过导入门的变异具名失败 8 项。坏 JSON 可出现“导入完成”是源码控制流推论，B4 先复现并明确提示策略；真实文件/旧包/游戏未验。R03 的 A1 归属已审，B4 未迁，A 总出口未闭，B 未启动。
- **J17-A R01 Weekly 双路径**：[E46](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)自动与预览的聚合/全文/短报消费者已核对；Weekly owner 仍反向调用宿主六个聚合规则及全文掠夺、短报转换，多个 helper 读取活 Hero/Clan/Kingdom/Settlement。B5 需主线程事实捕获与两模式回归，单 group 工作量须按材料和不同 ID 量测。仅源码审查，未跑 Weekly 回放/产品构建；R01 B5、A2 Weekly 桶及 A 总出口未闭，B 未启动。
- **J17-A 共享持久化分区**：[E47](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)七候选文件/114 声明已分：99 为通用 chunk/路径/JSON/PlayerExports，15 为 Knowledge ONNX 资产定位；`AIConfigHandler` 429 声明是配置/Prompt/规则/游戏资格/辅助 LLM 请求策略混合大文件，不能整包签 Persistence。只读 persistence/profile/config validator 5 cases PASS；递归清理/仓外 TEMP runner 未跑。A 总桶与 20 桶仍未闭，B 未启动。
- **J17-A `CoreDialogue` 内部接缝**：[E48](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)复核 `Refactor/Modules/` 四文件/107 声明归 Conversation 内部请求/状态/回执，V1 DTO 只是投影，Native/Scene/Courier 仍由原渠道 owner 准入和确认效果。当前 source-linked ModuleFrameworkApiTests 的 snapshot 36、public API 158 断言及 `CS0122` 通过，但未测试真实请求提交或双 DLL；R06 其余 26 文件/1,008 声明和 20 桶仍未签，先 A 后 B。
- **J17-A 跨域契约分区**：[E49](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)复核 `Refactor/Contracts/` 五文件/156 声明：FeatureBridge、LLM、Persona、Conversation 请求配置、TTS 合同各归其主责；LlmContracts 尾部三接口并非传输，TTS 音频结果 getter 仍可变。旧测试链接/源码路径需要 B7 改接；本片仅静态源码与消费者证据，R06 尚余 21 文件/852 声明，全 A 未闭。当前目标先做到 J17-A 收尾，不启动 B。
- **J17-A Contracts 目录分区续查**：[E50](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)补审 `InteractionContracts` 184 与 `LegacyShoutNetworkGateway` 10 声明，七个 Contracts 文件/350 声明现有 A 归属；后者是兼容网络实现而非 DTO。当前 gateway source-linked 40/0 与旧兼容 3 项通过，但真实 HTTP/三渠道/双 DLL 未验。R06 尚余 Adapters 14/250、Runtime 5/408 及动态消费者，A 总出口未达，B 未启动。
- **J17-A detached Prompt 适配簇**：[E51](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)五文件/58 声明归 Conversation/Prompt 请求期适配；Native parity 默认关闭，规则 selector 实际委派 AIConfigHandler，不拥有第二套规则/网络。当前只读 source extraction 14 项通过，不是三渠道行为验收。R06 尚余 Adapters 9/192、Runtime 5/408 及动态消费者；A 未闭，B 未启动。
- **J17-A 四个 LLM 网关适配**：[E52](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 `Configured/Knowledge/Policy/WorldDiplomacy` 四文件/68 声明；Configured 保留显式 400 thinking-control fallback、可选设置读取与含敏感正文的诊断，后三者按领域 bridge/retry 边界分离。只读 BridgeBinding 16/12/4 PASS，不是 HTTP 验收。R06 现余 Adapters 5/124、Runtime 5/408，20 桶与全 A 未签，B 未启动。
- **J17-A Conversation 适配与混合快照**：[E53](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)补审 Adapters 剩余五文件/124 声明；Native 公开 opt-in runner 不等于生产默认链，快照前段读游戏事实、后段 Memory facade 只转发 MyBehavior 权威提交。`Commit` 有主线程 gate，`Read/Append` 靠渠道调用边界；Agent/历史/Hero 查找成本留 R08。Adapters 14/250 已有逻辑分区，但 R06 仍余 Runtime 5/408 和动态消费者；20 桶与全 A 未签，B 未启动。本片只作源码/消费者复核，无当前 DLL/双 API/游戏验收。
- **J17-A Courier 收据与 Bridge Runtime**：[E54](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 Runtime 三文件/81 声明：Courier `AFCI1:` Pending 先于 Memory owner 写入，回执只承载冻结来信/恢复身份；FeatureBridge 是固定 ID 的一次配置装载与请求 gate。当前 Courier source-linked 契约 PASS、只读 Bridge 绑定 16/12/4 PASS；隔离 runner 会仓外递归删除，未执行，旧档/实机/双 API 产品 DLL 未验。R06 余 Runtime 2/327 与动态消费者，20 桶及 A 未签，B 未启动。
- **J17-A Duel 回执与 exact dispatch**：[E55](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 Runtime `DuelOutcomeReceipt` 一文件/186 声明；进程内一次请求/结果/效果 readback 不拥有 Mission、Economy、Memory 的真实副作用或保存恢复。源码链接 Outcome 20/20、Dispatch 16/16 通过；初次缺 assets `NETSDK1004`，仓内本地 restore 后通过。静态 owner 跨 Campaign 残留待核，R06 余 Notoriety 一文件/141 声明与动态消费者，A 未签，B 未启动。
- **J17-A Notoriety 持久回执**：[E56](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 Runtime 最后一文件/141 声明，`AFNR1:` 会话/行/绝对目标由 Notoriety ledger 持有，Memory H 只供已写行见证；source-linked 合同 14/14 通过。`Refactor/` 30 文件/1,115 声明现有文件内逻辑分区，但动态消费者、全部成员/20 桶仍未签，A 未闭、B 未启动；旧档/实机/双 API DLL 未验。
- **J17-A `MyBehavior` 前置状态图**：[E57](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)读取双 API 各 1,033 个 `MyBehavior.cs:58–2278` 类型/字段等定义并按多域初分，核对交易 DTO、Native Prompt、Weekly、Diplomacy/UI、Coup 字面反射和宿主保存键。其余 2,013 声明（含 1,985 个方法）、动态入口及其他编译文件仍未语义签收；本片仅源码审查，不改候选 `PENDING`、不启动 B。
- **J17-A `MyBehavior` 入口与 Memory 摘要方法**：[E58](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)再读双 API 各 48 个 `MyBehavior.cs:2279–2573,4518–5299` 方法体，与 E57 合计 1,081/3,046 声明；注册/载入代际/Native memory key 与 Memory 队列、Notoriety 终结的边界已具名。余 1,965 声明（含 1,937 方法）及 handler/动态消费者未闭；本片只读，无双 API DLL/旧档/实机验证，A 未签、B 未启动。
- **J17-A 遭遇/会面补丁簇**：[E59](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审双 API 均编译的 10 文件/40 声明和启动安装点：三会话入口、原版菜单/遭遇 gate、getter 安全前缀与外交阻断分别具名。GameMenu 按首领重设目标与已选军团成员路线是否交错仍待回放；其余 270 个 null 导航文件、动态 Harmony/实机未验，A 未签、B 未启动。
- **J17-A 防崩溃补丁簇**：[E60](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)再审双 API 14 文件/67 声明，分别归 Conversation、Mission/UI、场景、政治决策与地图 fallback；会面决斗 NRE 抑制日志的无 MapEvent 前提在代码 gate 中未核实，须回放。E59–E60 已具名 24 个 null 导航文件/107 声明，余 256 个 null 文件及 20 桶仍未签；未改产品、未跑双 API/实机，B 未启动。
- **J17-A 配置与协议小簇**：[E61](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 12 个 null 导航文件/双 API 各 72 声明：Prompt JSON/Guardrail/Postprocess、Economy ALL 数量与 YJ 请求兼容分属不同 owner；动态标签的运行时 allowed IDs 不进 JSON，不能在迁移时丢失。E59–E61 共 36 文件/179 声明初分，余 244 个 null 文件及 20 桶未闭；本片只读，A 未签、B 未启动。
- **J17-A UI 文本/资源接缝**：[E62](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 14 个 null 导航文件/双 API 各 195 声明，区分运行时 sprite/brush、动态 XML 控件、Native 展示代数、输入与 Courier 可见信清洗、消息及单模块路径。AutoHeight 每帧重分配委托、FillBar 每帧布局写入留 R08；sprite Harmony 安装失败不重试留回放。E59–E62 共 50 文件/374 声明初分，仍余 230 个 null 文件，候选 JSON 保持原 280/PENDING；20 桶与 A 未闭、B 未启动。只读源码，未跑产品 DLL/实机。
- **J17-A 历史展示与消息模型**：[E63](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 7 个 null 导航文件/双 API 各 203 声明：持久行 MyBehavior、瞬时 Native/Scene ShoutBehavior 与弹窗展示缓存/删除 callback 不可混为同一 owner；DialogueUI 有 XML 与私有反射消费者。50 行分页/每 tick 至多 3 行预格式化不限制单行 RichText 耗时，持久历史打开仍排序全部天数。E59–E63 合计 57 文件/577 声明初分，余 223 个 null 文件、20 桶未闭，导航 JSON 仍 PENDING；A 未签、B 未启动，未跑产品 DLL/实机。
- **J17-A `AIConfigHandler` 混合职责续审**：[E64](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)逐段读两 API 各 429 声明所在的 7,900 行，分清配置、Prompt 检索/历史/后处理、辅助 LLM、实时游戏资格与 Knowledge 门面，不把原 `persistence-config` 导航桶当权威。具名余项：野外后处理全 Settlement 排序、静态 sticky/评估缓存跨 Campaign 隔离、legacy null 资格 worker 回退与诊断原文隐私。仅静态源码/部分消费者审查；另外 223 个 null 导航文件、20 桶与全 A 仍未闭，B 不启动，未跑产品双构建/实机。
- **J17-A 主回复网络续审**：[E65](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 `ShoutNetwork` 双 API 各 50 声明与 Native/Scene/Vengeance/typed gateway 消费者；流式 raw sample 以 `-1` 关闭长度上限、取消部分回复回调、逐块活 Hero 名称转换和 raw/token 诊断为 R05/R08/R09 具名余项，不把源码风险冒充实机故障。E59–E63 加本片为 58 个原 null 导航文件/627 声明初分，余 222 文件、20 桶和 A 总出口仍未闭，B 不启动；未重跑旧网络回放、双 API 产品构建或实机。
- **J17-A GCCZ 本地化/城镇记忆接缝**：[E66](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)审 12 个原 null 导航文件/72 声明及五份资源的 `.csproj` LogicalName/发布映射；区分 Lazy 内容 loader、MCM/inquiry、城镇 ruler/speaker 活事实、可写手工叙事开发者菜单和诊断端口，GCCZ 复用规则/记录仍归 core。E59–E63 加 E65–E66 共 70 文件/699 声明初分，余 210 文件，20 桶与 A 总出口未闭、B 不启动。未写仓外 GCCZ、未运行日志导出/清理/产品双构建/实机；Agent 扫描和迟到编辑回调留具名验收。
- **J17-A 战斗事件小段已收尾**：[E67](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)只读核对双 API 各 3 个 `MyBehavior.cs:2617–2721` 方法及事件注册、History/AFEF/NPC 行动下游：两个获胜事件共用去重键；键先加入、旧 `void` History 门面后写的拒收风险待合成回放。其余 `MyBehavior` 方法、20 桶和 A 出口仍未闭；按用户要求本轮到此停止，不进入 B。未改产品/构建、未部署/推送。
- **交付**：J16 15 个提交与 J17-A 盘点文档通过普通 merge 整合；目标分支 `codex/af-main-refactor-continuation-20260831` 已成功快进推送至 `3f7b9018`（GitHub 回执：`ada9894a..3f7b9018`，未强推）。
- **J15**：`J15_CLOSED_BY_USER / LIVE_SAVE_NOT_RUN / RELEASE_HOLD_CARRIED`；实机/旧档、F4-A 来源与视觉遗留继续保留，发布 HOLD 不变。
- **J16**：`J16_OFFLINE_CLOSED`（a/b/c/d/e）。全量 runner 记录 257 项：`PASS=209 / PREEXISTING_FAIL=38 / NEEDS_INPUT=6 / SUPERSEDED_BY_RUNNER=3 / ENV_STATE=1 / FAIL=0`；inventory `unknown=0`、链接检查 0 断链、代码地图两模式 795 PASS。LIVE、旧 SAVE、三渠道修复回放未运行；旧档键兼容、`IntegratedModuleHost.Tick` 保护、AuxiliaryTests/net472 等遗留见主台账。
- **J17-A**：旧 E09–E13 与 940/3,332 分母仅为 `99ca85ae` 历史导航；当前实际编译基线及 A1/A2 开放项以上述 E14–E28 和主台账为准。R06 有 `Refactor/` 30 文件责任**导航**（1,115 声明）、测试/工具 688 文件的精确旧路径/字面 CLR 全名索引（155/149 行）；R07 另审根级 15 个 Prompt 配置/结果 DTO（143 声明）及 13 个 `MyBehavior.*` partial（284 声明），指出 Memory Seal 原子 O(N) 尾部。这不是所有成员/动态调用闭合。R05/R06/R07/R08/R09 与 20 桶语义仍未闭，不满足 A 出口；依用户顺序 B 不启动。
- **验证边界**：J16 的全量测试数据来自其交接记录，本轮未重跑全套；本轮完成文档整合与普通推送。未运行游戏/旧档/provider，未部署或打包；保留 `.dotnet-cli-home/` 和忽略的 J17 盘点材料。
## J16/J17 整合前的 J16 交接（保留历史，以上方状态为准）
# 当前交接：J15 按用户决定结项，转入 J17（2026-09-29）

- **J15**：`J15_CLOSED_BY_USER / LIVE_SAVE_NOT_RUN / RELEASE_HOLD_CARRIED`——离线成果保持；F5 实机/旧档、F4-A 出处文字与实机视觉转为已知遗留，发布 HOLD 继续有效。见[主台账 J15 结项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-closed-by-user-20260929)。
- **下一步**：J17 在 `G:\AFMOD\AF-J17` 续作，先合入本分支 J16 成果。

# 上一段交接：J16 离线收口完成（2026-09-29，分支 `codex/af-j16-continue-20260929`，未推送）

- **状态**：`J16_OFFLINE_CLOSED`——J16a/b/c/d/e 全部完成；`LIVE / SAVE / 三渠道回放 NOT-RUN`。详见[主台账 J16 续作与收口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j16-continue-20260929)。基于远端 `ada9894a`，本分支 15 个提交。
- **全量验证**：`py -3 tests/run_all.py` 257 项 `PASS=209 / PREEXISTING_FAIL=38 / NEEDS_INPUT=6 / SUPERSEDED_BY_RUNNER=3 / ENV_STATE=1 / FAIL=0`；对照 99ca85ae 与 ada9894a 两份基线 PASS→非 PASS 为 0，远端引入的 34 项失败全部恢复。inventory `unknown=0`、链接检查 0 断链、代码地图两模式 795 PASS、`git diff --check` 通过。
- **结构**：远端新增测试按 owner 归位；阶段 2–7 文档入 `docs/history/phase2-7/` 并新增 `docs/README.md`；A1 去跟踪 411 路径；Bootstrap → `src/AF.Bootstrap`，一键 PowerShell → `scripts/build/`（`.bat` 入口不变，实跑一键编译与打包，DLL 元数据与移动前一致）；新增 `tests/run_all.py` + `tests/runners.json` 总入口。
- **产品修复（用户授权）**：SceneActions 指令改在 stale/目标/丢弃判定通过后提交（Scene、Native）；仪式处决改为接受回复后挂上单一处理器。配套新增 3 条断言与 3 个变异用例。
- **存档期望**：按当前源码更新（177 键）；`PersistenceIdentityAudit` 基线移至 `ada9894a`。
- **未验 / 遗留**：实机与旧档未跑，**被移除的 `_af_xihai_legacy_equipment_cleanup_v1` 对旧档的兼容未验**；三渠道回放未针对修复运行；`IntegratedModuleHost.Tick` 每帧无异常保护（未修）；DialogueUI `AuxiliaryTests` 缺 net472 引用（NEEDS_INPUT）；PromptLab 其余 cases 未处理；`.codex_tmp` Edge 配置与 Logs zip 仍在 Git 历史和远端，历史清洗未做。
- **下一步**：推送需用户确认（按规则推到新分支）；之后回到 J17（`G:\AFMOD\AF-J17`，J17-A R05/R06/R07 残余审查，5 组中断待续）。

## 以下为上一段交接

# 当前交接：J16b A1 去跟踪与 J16c 链接检查完成（2026-09-29，分支 `codex/af-j16-continue-20260929`）

- **范围**：AF-J15 快进到远端 `ada9894a` 后新建分支，先续 J16b/J16c。详见[主台账 J16 续作](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j16-continue-20260929)。
- **已完成**：inventory 恢复 `PASS / unknown=0`；A1 去跟踪 411 个无消费者路径；新增 `tests/docs/LinkCheck`；代码地图重定位。该段待办已在上方收口中完成。

## 以下为更早交接

# 当前交接：未推送提交与四功能整合工作树说明已整理（2026-09-29）

- **范围**：本地 `codex/af-main-refactor-continuation-20260831` 领先远端同名分支 **266 个提交**（2026-09-12→09-29，主体为 Illustrator 迭代，另有 DialogueUI、Coup 与两次远端 J15/J16 合并）；其上还有一层**未提交**的四功能整合与审查修复工作树，本次提交涉及内容见[提交说明](docs/handoffs/2026-09-29-four-feature-integration-commit.md)。
- **本轮新增修复**：复仇双模块互斥（独立版反射探测 AF 宿主 + 三入口拦截）、共享源码去 AF 硬依赖（`EscortedHeroesProvider` 回调 + ID 延迟重解析）、连续行刑换受刑者重建请求、处决 7 项资源入 `content-map.json`（195 项齐）、生图缓存取消自动搬迁（根回文档目录）、政变坏存档不覆盖与补丁生命周期复位、西海衣服自动删除机制整体移除。
- **验证**：官方双版本 Debug 构建 1.3/1.4 各 336 警告 0 错误、Bootstrap 0/0；独立 RichExecutions 15 警告 0 错误；内容清单 195 项/0 缺失/0 重复；`git diff --check` 与 code-map PASS。**未验**：实机、旧存档、双模块运行时互斥、Stage/部署/打包。
- **未推送/未提交**：全部成果只在本地；`Vengeance/`、`AnimusForge/GUI`、`AssetPackages`、`ModuleData`、DialogueUI 素材等仍为未跟踪文件，干净检出不可复现。复仇处决由用户稍后自行提交推送；历史 Git 含私密资料的既有警示仍然有效，推送需另行明确决定。

## 以下为此前交接历史（相冲突状态以上方最新为准）

# 当前交接：J16a 测试归位已完成，J16b 进行中；J15 离线收口（2026-09-28）

- **当前计划不变**：[AF 2.0 责任结项计划](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)仍为唯一执行口径；当前是普通 merge 核验，不是重复 J15 产品施工。范围、父提交和结果见[合并回执](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-remote-reconcile-20260928)。
- **证据区分**：下方远端作者记录了 TPAC“用户声明自制/有授权”，作为有来源的补充保留，具体出处待补；不能继续仅因未读取远端记录就称“完全没有来源声明”，也不把该声明等同本机核验或所有模型/素材的再分发许可。远端六构建/Stage/编辑器运行只按其候选引用，不当成本机本轮测试。发布和真实游戏/旧档状态仍单列。
- **本轮实测**：远端 J15 合并回执记录编辑器 Release 构建 0 warning/error、合成根九步 smoke/六安全反例及 12 次备份通过；这不是本机本轮构建。本机合并后复跑 J15 内容 runner `108 mappings / 0 holds`、J15 FullStage synthetic Fault/Abrupt 两模式、DataPaths `66`、RepositorySourceInventory `7`、代码地图 recorded/working-tree `795`，均 PASS；`git diff --cached --check` 通过。O6 写入仅在两个全新忽略的 `artifacts/j15-content/post-merge-o6-*` 合成根，真实游戏/玩家数据未改。
- **合并约束**：保留双方有效测试和本地最新计划，不强推、不改历史、不写真实游戏/玩家包、不改默认覆盖方式；未跟踪 `.dotnet-cli-home/` 保持。完整测试只使用本轮获准的全新 `E:/AF-J15-merge-smoke-20260928-a`，不清理。
- **J16 分支整理**：保留本地 `codex/af-j15-closeout-20260928` 上的 18 个 J16 相关提交，并与远端 J15/AF2 重规划的 10 个并行提交普通合并；J16a 归位的 99 个测试目录和 3 个散脚本已逐批对照 G0 基线，无回归。J16b 已完成 overlay 输出迁移与 pycache 去跟踪；其它 A1 跟踪产物未处理。详见主台账 J16 进度节与 [J16 计划](docs/plans/j16-tests-tools-docs-plan.md)。
- **计划顺序待衔接**：AF2 责任结项计划要求 J17-A 保存/Memory 输入先于后续 J16 收口；现有 J16a 进度保留，不视为全 J16 完成。下一轮应先对齐该前置关系，再按授权续做 J16b/J16c/J16e；J16d 仍需单独授权。

## 以下为本地已交付计划与历史证据

# 当前交接：AF 2.0 结项计划已重整，产品职责尚未全部迁完（2026-09-28）

- **本轮仅文档**：`PLAN_READY / RESPONSIBILITY_AUDIT_PENDING / PRODUCT_REFACTOR_NOT_COMPLETE`。当前解释、历史 supersession、R01–R09 初始登记与 20 桶全量盘点要求见[唯一主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#af2-closeout-plan-20260928)，源码核实见[范围图 E01–E08](docs/architecture/af-framework-code-scope.md#af2-responsibility-evidence-20260928)。这不是产品重构或全量审查已完成。
- **保留成果，不沿误导续作**：J01–J15 的有限离线证据按原范围保持；J15 仍为 `J15_OFFLINE_VERIFIED / LIVE_SAVE_NOT_RUN / RELEASE_HOLD`。旧总计划“J12/J13 尚未施工”“J14 尚未开放”和旧 owner matrix 的时点状态不能当当前指令；存档身份与业务算法分开判定，不能把大类整体叫兼容壳。
- **下一步**：后续获准继续时先按 J17-A 做 R04 保存/JSON 边界与 R02 Memory、R01 Weekly 的成员/消费者闭包，再补全全部责任桶；这是 J16 开工输入。之后 J16 归位、J17-B 残余业务、J17-C 同候选离线、J17-D 实机/旧档分层关闭。不清空目录冒充职责完成，不重做已独立 HTTP owner。
- **授权与数据**：本轮不改产品、测试、配置、Skill 或一键流程，不构建/部署/打包/推送、不写游戏/玩家数据/外仓；`.dotnet-cli-home/` 和既有归档保持。沿用户 AFMOD 默认提示词包＋玩家另装模块 ONNX 的发行方式，不恢复已撤回的默认提示词覆盖保护。历史私人 Git/素材及发布 HOLD 不因文档重整解除。

## 以下为历史交接；阶段证据仍有效，当前计划与下一步以上方入口为准

# 当前交接：J15 有限离线验收已完成，实机/旧档/发布未签收（2026-09-28）

- **状态**：`J15_OFFLINE_VERIFIED / LIVE_SAVE_NOT_RUN / RELEASE_HOLD`。F4 资源/个人数据/模块 ONNX 的功能离线范围已闭，F5 O1–O7 同一最终候选矩阵已复核；108 内容映射、0 旧根 HOLD、双 Stage 与本地测试 ZIP 各 124 项、正式 StaticVerifier 各 13/0。六构建不在本切片递归重做：`4304397c` 后产品 C#/项目/构建脚本未变，当前六 DLL marker SHA、Stage/ZIP 字节与原六构建日志同候选；最新内容、DataPaths 66、PlayerExports 25 加两变异、持久化/Bridge、库存/代码地图已重跑。完整证据和适用边界只见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)及[范围图](docs/architecture/af-framework-code-scope.md)；[J15 计划](docs/plans/j15-content-profile-plan.md)保留完整上线规格。
- **停点/下一步**：TPAC 历史 11.2 MB 身份与当前 1.67 MB 不符，上游素材/模型再分发权、两版本动作/UI 视觉仍 HOLD；1.3/1.4 实机 M9、新战役/旧档/升级、真实 Deploy、真实 provider 均 NOT-RUN。本地测试 ZIP 不可发布。没有本轮游戏/玩家库/外仓写入、推送或发布；历史私人 Git 字节仍在已推送的旧历史中，任何后续推送/历史治理另行决定。忽略的 SHA 归档与编辑器外部合成 smoke 材料保持原位，不作清理；`.dotnet-cli-home/` 原有未跟踪状态保持。

## 以下为本轮离线收口前的交接历史（相冲突状态以上段为准）

# J15 O2、F4-U 与当前 124 项 O6 故障合成已验，最终离线门禁未闭（2026-09-28）

- **当前进展**：编辑器 O2 隔离合成完整模式已在批准的 `E:\AF-J15-editor-smoke-20260927-a\` PASS，材料保留；测试/交接本地提交 `a62ab001`。其后六项剩余旧 GUI 逐项 SHA 复制并二次复核到忽略的 `local/j15-gui-archive-20260927/legacy-remaining/` 后才退出活动源码；完整 J15 内容 runner **108 映射/0 旧根 HOLD PASS**，inventory **19477 tracked/0 unknown**、7 单测 PASS。归档不随 Git 提交；不等于实机视觉或素材许可签收。详证见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)。
- **当前 O6 补证与下一门槛**：新全 Stage 契约以当前 Release 124 项分别做第 61 项同步故障回滚、前两项后突然退出/写前拒绝/合成手工恢复，重试与 no-op、124/124 SHA 和三哨兵均 PASS；只在全新仓内虚构根，非产品自动恢复或实际部署。随后须按最终同候选复验 O1–O7 并修正旧范围图/清单状态。TPAC 历史身份、第三方许可及双实际版本游戏/旧档仍 HOLD/NOT-RUN。已推送 `8af57b39` 取代下方历史“未推送”表述，但私人历史风险仍在；不得据此再次推送、部署或发布。

## 以下为本轮之前的交接历史（相冲突状态以上段为准）

# 当前交接：J15 F4-U 旧 GUI 退役、F4-A TPAC 投影及双 Stage 13/0 已验，F4/F5 仍未闭（2026-09-27）

- **用户要求停下，待恢复**：截至本交接，最新已验证切片为 `b2b946d3`（F4-A 重复 TPAC 归档退役、六设计源 100% rename）；F4-U 退役为 `f26e4c2b`。两份忽略归档分别在 `local/j15-tpac-archive-20260927/legacy-module/`、`local/j15-gui-archive-20260927/legacy-splitshadows/`，**不随 Git 提交**，保留当前工作区才能直接恢复；Git 历史仍含旧私人资料/图，绝不据此推送。J15 内容契约最近 **108 映射/6 HOLD PASS**、双正式 Stage 各 124 项白名单、StaticVerifier 各 **13/0**、两个本地测试 ZIP 各 124 项；这些不是游戏/旧档或发布验收。
- **未完成的编辑器 smoke 入口改动，不可报 PASS**：用户已批准仅写全新 `E:\AF-J15-editor-smoke-20260927-a\`，但该外部目录**尚未创建**、完整 smoke **NOT-RUN**。工作树目前只有我们未提交的 `tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/Program.cs` 测试改动：新增 `--isolated-full`，要求数据根与 TEMP 在同一指定合成根，保存删除 fixture；并把旧删除 fixture 改为产品要求的 `UserData/PlayerExports` 正规路径。**尚未编译、未运行、未验证**，恢复时先检查 `git status` 和改动，再以 .NET 10 定向构建/测试；只在再次确认该 E: 根仍不存在、无 reparse 且 TEMP/TMP/`ANIMUSFORGE_DATA_ROOT` 全指向其内部后执行。不要无参数运行旧 full smoke（它会递归清理临时 fixture 并可能解析真实用户根）。`.dotnet-cli-home/` 是先前未跟踪用户现状，原样保留。
- **下一步与边界**：先验证或修正该测试入口，跑获准外部合成编辑/保存/备份恢复 smoke 并保留目录/日志，再按结果更新主台账、提交已验证切片；若失败先诊断，不把测试入口当产品能力。F4-A 历史 11.2 MB manifest 与当前 1.67 MB TPAC 身份、上游来源/再分发权和实机动作视觉，以及 F5 两版本真实游戏/旧档仍未闭。未部署、未启动游戏、未改真实玩家库、未推送；J15 不标完成。

- **F4-A 去重/归位续验**：经再次批准，重复的 `AnimusForge/AssetPackages/pack0.tpac` 已逐项 SHA 备份到忽略的 `local/j15-tpac-archive-20260927/legacy-module/` 后退出活动源码，Xihai 仍是正式投影唯一权威源；六项金币设计源原样迁到 `content/modules/AF.Module.Economy/AssetSources/`，每项 hash 相同，分类为 `design`、不进 Stage/ZIP。J15 完整内容 runner **108/6 PASS**、库存 7 单测、双正式 Stage 124 项白名单通过。来源/再分发权、11.2 MB 历史 manifest 与实机视觉仍未闭；未部署/推送。详证见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)。

- **最新 F4-U**：用户确认旧账号/订阅/充值 UI 已废弃并批准归档；两个索引 XML + 25 张分图共 27 项/6,211,268 字节逐项 SHA 复制到忽略的 `local/j15-gui-archive-20260927/legacy-splitshadows/` 后，从活动源码退役，移后归档 27/27 再验。内容完整 runner **108 映射/6 HOLD PASS**；正式双 Stage 各 124 项仍符合原白名单/当前产物。另六项旧 GUI 保留，三张诊断 atlas 不进包；历史 Git 仍含旧图，不推送。详证见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)。

- **续验**：当前 Release 正式 124 项 Stage 在全新仓内虚构安装根逐项 SHA 部署一致、三类未受管数据不变、重复执行 no-op；不是游戏部署或 124 项故障回滚验收。三张 GUI atlas 已由 `origin/main` 的 25 张源图按 XML 坐标在忽略目录重建为诊断 PNG，逐图保存后裁切像素一致；但画面涉及旧订阅/充值宣传，生产源码/当前 DLL 未发现直接消费者，未纳入正式 Stage/ZIP。详细尺寸、SHA、范围与 HOLD 见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)。

- **最新 F4-A 本地切片**：用户批准仅将 `origin/main` 同字节的 1,671,304 字节 TPAC 映射进本仓双 Stage/本地测试 ZIP；新增一条 Xihai content 映射及 hash 契约，J15 runner **108 映射/33 HOLD PASS**。原 Stage 入口重建 Debug/Release 各 124 项，正式 StaticVerifier **各 13/0**；原打包入口 `-NoBump` 的两个本地测试 ZIP 各 124 项、123 非 XML 字节与 Stage 一致，禁入路径 0。程序 DLL 未变、未重编；两份 TPAC 原件保留。此前 `11/2` 和 123 项 Stage/ZIP 均为较早候选，不再代表当前 Stage。详情见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)。历史 11.2 MB manifest、两原件去重、来源/再分发权、三张 atlas、实机/旧档未闭；测试 ZIP 不可发布。无部署、推送或游戏/玩家数据写入。

- **状态**：`F4-C/D_OFFLINE_VERIFIED / F4-M_COLD_RACE_FIXED_OFFLINE / F5_PARTIAL / J15_NOT_COMPLETE`。产品/测试 `63138958`、`688ad6b8`，八项私人旧源退役 `6b2c4373`。`OnnxEmbeddingEngine.cs:306,457–529` 完成初始化后才发布 volatile 状态，模块 ONNX 强制门禁不放松；`deploy_module.ps1:505–537` 在受管写入前拒绝未知手改/坏 `FeatureBridges.json`。详证见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)及[范围图](docs/architecture/af-framework-code-scope.md)，[795 锚点地图](docs/architecture/af-framework-code-map.json)仅导航。
- **已验**：获准四构建目录、双 Stage 与 ABI/API 生成根预检后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建各 0 warning/error；四 DLL 各 7 默认资源，Release 双 API ONNX 当前 DLL 各 33+5；J15 内容 107 映射/**33 HOLD**、F4-C 合成部署、DataPaths 66、个人迁移、PlayerExports 25+两业务变异、PersistenceIdentity 142/36、PersistenceProfile typed 168、编辑器两个路径模式通过。编辑器秒级备份撞名由 `3d5a37f8` 修复，12 次连续保存各留独立旧字节、坏 JSON 保留活动文件；完整数据 smoke 仍未跑。八项/39,960 字节私人旧源 SHA 复核归档到忽略的 `local/j15-private-archive-20260927/legacy-module/` 后仅从活动源码退役，inventory unknown=0。两 Stage 各 123 项、Xihai Core 88、Native 55/旧 ABI 41+41、公开 API 158/四 DLL metadata 1620、两本地测试 ZIP 各 123 项与 Stage 122 非 XML 字节相等、禁入路径 0；前候选完整 Release Stage 在全新虚构安装根安装 123 项、已知旧配置升级/私有备份、未知手改前置拒绝、ONNX 等三哨兵和 no-op 均通过，**不等于真实部署或发布完整资源**。
- **新候选 `4304397c`**：`AfCompatV130.IsExpectedRecordedPlayerMessageMethod` 精确对齐当前 `RecordPlayerMessage` 的 bool/六参数；StaticVerifier 旧 Stage 10/3 红例中的桥签名失败消失，新 Debug/Release Stage 各 11/2，剩余两项同属 TPAC 资源。原脚本新候选六构建均 0 warning/error、两 Stage 各 123 项；Native 55、旧 ABI 41+41、公开 API 158、四 DLL 元数据 1620、Release 双 API ONNX 各 33+5 再验通过；两新本地测试 ZIP 各 123 项/122 非 XML 字节匹配；当前完整 Release Stage 在新虚构根安装 123/123 SHA 相同、三哨兵保留、no-op 与坏配置前置拒绝通过，原合成部署契约的回滚/中断/旧默认升级也通过，均非真实部署或发布。详细红绿及候选 SHA 见主台账；编辑器仓内合成根被产品拒写门禁正确拦截，未放宽门禁、未碰真实玩家库。
- **O6 完整 Stage 补证**：全新仓内虚构目标对当前 Release Stage 123 项在第 61 项前注入同步故障，旧受管文件由私有备份恢复、其余 Stage 路径撤销，三未受管哨兵未变；之后成功安装 123/123 SHA 相同，no-op 无新 Recovery。另在独立合成根于第三项前让测试子进程突然退出，实见两项部分安装/`activating` 记录；再次部署写前拒绝，按 manifest/SHA 手工恢复旧状态后重试 123/123 通过。仅合成手工恢复，不是产品自动续传或真实游戏 L4，详见主台账。
- **O4 当前只读复核**：Bridge 绑定 16/12 wired/4 declared-only、fixture 10/6、存档身份 142/36 与配置 typed 168 均 PASS；Xihai Core/测试源码相对前候选未变，Runtime 桥由当前 StaticVerifier 另验。只读/fixture 结果不是运行时或实机签收，详见主台账。
- **HOLD/下一步**：F4-U 33 GUI/缺三 atlas、F4-A TPAC 历史 manifest 身份与来源/许可未闭；25 项图集源图片均在且 XML 尺寸/坐标不冲突，其中 4 个 `.png` 实为 JPEG。远端 `origin/main` 的 1.67 MB TPAC 与本仓字节相同，**实际包含 `nacisword1`**；将它仅加入全新合成 Release Stage 后 StaticVerifier **13/0**，纠正此前把正式 Stage `11/2` 归因于小包内容不足的推断。正式双 Stage/ZIP 仍未纳入 TPAC，远端没有 11.2 MB manifest 版本或三张 GUI sheet；不能以合成通过替代来源/分发权、游戏视觉或实际 Stage 签收。manifest 的 F: 原始来源及约定 G: 外仓当前均不存在。编辑器完整 smoke 待获准外部合成根，M9 两个实际版本和 L/S 新装/旧档未验。O7 当前只读清单 `tracked=19511 / unknown=0`、7 单测及代码地图 795 锚点双模式 PASS，但不代表资产来源签收。无真实 Deploy、游戏/玩家库/外仓写入、推送、发布或游戏启动；历史 Git 含私密资料，切勿推送。

## 以下为本轮之前的交接历史（相冲突状态以上段为准）

# 当前交接：J15 F4-M 模块 ONNX 契约代码与关联离线验证已完成（2026-09-27）

- **当前状态**：`F4-M_CODE_AND_OFFLINE_VERIFIED / F5_PARTIAL / J15_NOT_COMPLETE`。产品/测试 `df49d81a`、`7693bedc`、`43e8bcf2` 将门禁、embedding 与 reranker 统一到当前 `Modules/AnimusForge/ONNX`，退役模型外迁 CLI 的执行入口与 ModelsLock 运行资源；用户 Models/Recovery/仓内忽略副本保留但不再是运行来源。代码导航见[范围图](docs/architecture/af-framework-code-scope.md)与 792 锚点[地图](docs/architecture/af-framework-code-map.json)，一基坐标、证据和替代关系见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-onnx-f4m-implementation-20260927)，规格仍为[J15 第 9 节](docs/plans/j15-content-profile-plan.md#j15-onnx-install-contract)。
- **已验/边界**：Debug/Release 的 1.3、1.4、Bootstrap 六构建均 0 警告/错误；四实现 DLL 各 7 默认资源；DataPaths 66 checks、旧模型迁移拒绝、J15 内容/ManagedDeploy 合成契约；当前 Release 双版本 DLL 对安装模块现有 ONNX 作只读离线真实推理，各 28 断言。Debug/Release Stage 各 123 项、本地测试 ZIP 与 Stage 集合及非 XML 字节对齐，均无 ONNX。没有部署、游戏启动、外仓或用户库/Recovery 写入、推送；测试 ZIP 不可发布。
- **下一步**：F4-C/U/A/D 的手改配置、GUI/atlas、TPAC/设计源、旧资料与来源/许可分别闭合；F5 其余同候选矩阵后，另获具名游戏写入/实测授权才做 M9 双实际版本新战役、旧档、暂停/保存退出、补模重启和检索。LIVE/SAVE 均 `NOT-RUN`，不可把离线 DLL 推理当实机验收。保留玩家现有模型及历史副本，不自动搬回或清理。

## 以下为本次实施取代的仅文档交接（历史，不再作为当前状态）

- **唯一当前入口**：[新版计划第 9 节](docs/plans/j15-content-profile-plan.md#j15-af2-final-state)，先做 [F4-M](docs/plans/j15-content-profile-plan.md#j15-onnx-install-contract)；实际代码坐标、替代关系与验证在[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-onnx-contract-correction-20260927)。[代码范围图](docs/architecture/af-framework-code-scope.md)未变，产品仍为 `f4280eed`。
- **用户硬要求**：ONNX 放当前 `Modules/AnimusForge/ONNX`，门禁和实际加载使用这里，缺必需文件就暂停/退出。主 ZIP 不带模型，玩家另装；不迁 AppData、不依赖模型迁移凭证、不装 Python/SDK、不跑命令。
- **明确纠偏而非补迁移入口**：F2 的用户模型根/Recovery 前置条件与保留的模块门禁冲突；`fbd71c38`、`95159efd` 的模型迁移/初始化方案撤销。计划已按此重写分类、F2 状态、F4-M 和 F5，并同步单模块说明；其他个人数据保护、内容归位和 F3 产物链不整体回滚。
- **先做什么**：用户授权实施后，优先一次接通模块根/原候选、门禁、两个引擎、模型外迁入口退役及 M1–M9 验收；再完成其余 F4/F5。既有用户 Models、Recovery、忽略副本不删不搬回，游戏恢复另按具名范围确认。
- **本轮只有四份文档**：未改代码/测试/构建脚本，未搬模型、未写游戏/用户库、未安装软件、未构建/迁移/运行游戏/推送。F3 原离线证据不等于 ONNX 正常；F2 模型回归仍 OPEN，F4/F5、实机/旧档未完成。后文均为历史，不能用“无需重做 F1–F3”跳过本次纠偏。

## 以下为被替代的计划交接（非执行指令）

### J15 F4/F5 计划重构历史（模型方案已撤销）

- **本轮仅文档**：用户要求先完善可执行计划，未授权恢复产品实施。新的唯一入口为[计划第 9.7–9.11 节](docs/plans/j15-content-profile-plan.md#j15-scope-decisions)，实际定位和验证记录见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-executable-plan-20260927)；[代码范围图](docs/architecture/af-framework-code-scope.md)未改，产品基线仍为 `f4280eed`。
- **纠正正式版要求**：撤销 `fbd71c38` 中将开发 Python 迁移脚本作为玩家初始化入口的错误方案。玩家不装 Python/SDK、不取源码、不敲迁移命令、不额外捆绑解释器；初始化/升级/恢复由 AF 产品侧承担，开发脚本只供开发维护。F5-L1 必须在无开发环境下验收；这仍是待实现规格，未改产品或安装任何软件。
- **已定边界**：未来 AF 模块设置 UI 不挤进 J15 假迁移；当前 MCM 存储保留；FeatureBridges 按内部随版配置管理，不新增个人覆盖层，但旧手改冲突不能静默覆盖。F4 明确五组输入/动作/退出门，补模型无旧安装的产品侧初始化与恢复；F5 明确当前 8 资源、同候选离线矩阵、两版本实机/旧档矩阵和确认点。
- **下一步**：用户明确开始实施后先执行 F4 的逐项冻结与已确认仓内切片；批量移动/真实资料/游戏覆盖按具名清单确认。没有改产品/测试/脚本、没有迁移/启动游戏/推送；F4/F5 与完整 AF2.0 仍未完成。

## 以下为 F3 停点与历史切片（非新的执行授权）

- **F3 产品/测试 `a8f69b57`、`82d8ce53`**：同一干净 Stage 装配服务 Stage-only 与部署；部署只更新 Stage 列出的受管文件，目标旧字节先入私有 Recovery 并做 hash，同卷候选替换，失败回滚，未完成记录拒绝重试。旧 `/MIR` 和安装资料合并函数已移除；旧双模块只读警告的虚构重跑未改旧模块且无新 Recovery。虚构游戏根 123 项 SHA 对齐 Stage，PlayerExports 未变；故障注入回滚、恢复、重复、中断、7 个未受管/旧模块哨兵与完整 J15 内容 runner PASS。Debug/Release Stage 各 123 项无输入重组稳定，两 ZIP 各 123 项集合及 122 个非 XML SHA 对齐；代码地图 **789** 锚点两模式通过。既有 C# 六构建产物未变，本切片未重编；**未写 D: 游戏目录、未实机部署/启动、未改原始 `.sav`、未推送**。详证见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[范围图](docs/architecture/af-framework-code-scope.md)。
- **停点**：用户明确“F3做完就停下”。F3 为 **OFFLINE_VERIFIED**，不是 AF2.0 完成或可发布；33 项未映射 GUI/TPAC 及其权利、剩余旧资料属 F4，F5 同候选总验收与 1.3/1.4 实机/旧档均 **NOT-RUN**。当前 ZIP 仅装配/隐私工程候选。下方为早前修订历史，出现“部署失败关闭”不代表当前实现。

- **F3 新候选防陈旧打包 `8b3f3b51`**：打包对 Stage 九项程序产物逐字节比对当前 `single_module_artifacts`，旧 Stage 在创建 ZIP 目录前被拒；Stage 重建仍只要求旧目录满足隐私/内容白名单，不因程序版本旧而误清未知文件。合成文件漂移反例 PASS；Debug/Release 以当前模型代码重组 Stage 各 123 项并重复一次，文件集合/hash 稳定；两 ZIP 与各自 Stage 的 123 项集合及非 XML 字节独立一致，PlayerExports/ONNX/日志/未获准资产均为 0。代码地图 **785** 锚点两模式通过。此为装配与隐私工程证据，GUI/TPAC 等必需资源仍缺，**不可发布**。
- **产品 `eb03ce28`、锁换行固定 `2f589e6b`、旧模型退役 `0188367a`、完成记录门禁 `44e91672`**：两 ONNX 消费者只从 typed `%LOCALAPPDATA%/AnimusForge/Models/{embedding,reranker}` 加载；内嵌 `content/models.lock.json`、私有完成记录和整组文件元数据一次性验证，旧模块路径不再日常回退。显式迁移对 D: 安装源 10 项与仓内源 5 项分别做完整组 SHA-256、私有备份、同卷候选和目标优先激活；安装 10 项激活，仓内 0 项激活且一组字节冲突保留，两源重复执行均无再次激活。独立复验活动组 10 项全组 SHA；仓内五个已跟踪模型字节在验证两套私有备份后移至工作区忽略的 `local/` 并停止跟踪，D: 原件及用户活动库不动。合成迁移涵盖冲突、损坏、并发目标、中断、磁盘满与重复；DataPaths **58** 项、四实现各 **8** 内嵌资源、当前 Debug/Release × 1.3/1.4 + Bootstrap **六构建零警告错误**、代码地图 **784** 锚点两模式通过。模型活动库约 1.14 GiB、私有 Recovery 约 5.09 GiB；锁格式修正产生额外已验证备份集，未清理。详情见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[范围图](docs/architecture/af-framework-code-scope.md)。
- **仓内 PlayerExports 退役 `7f30aaa6`**：迁移完成记录、仓内原 3139 项、私有备份与用户活动副本逐项大小/SHA-256 一致；旧仓内目录同卷移入忽略的 `local/`，移后再逐项复验，当前跟踪数 0。编辑器用户根/拒写旧路径复测通过；D: 安装原件、用户活动副本及私有 Recovery 均保留。**Git 历史仍含私人数据，未改历史且严禁据此推送。**
- **仍未闭合**：模型运行时未在游戏中启动；reranker 本地文件的上游身份与转换仓再分发权未证实，故仍不进客户端 ZIP，也不能宣称发布许可通过。F3 程序文件部署仍失败关闭；GUI/TPAC/设计源及 `UnnamedNpcProfiles`/`VoiceMapping.json` 等旧资料退役、F5 同候选 Stage/ZIP 与总验收继续。无游戏覆盖/启动、原始 `.sav` 修改、推送或 G: 写入；实机旧档 **NOT-RUN**，**F1–F5 未完成**。

## 以下为 F3 Stage/ZIP 交接（历史）

# AF2.0 F3 干净 Stage/ZIP 已验，部署和资源门禁未闭（2026-09-27）

- **产品/测试 `d7c16c06`**：原一键打包入口现只接收项目内经清单验证的 Stage；Stage 仅从 107 项 `content` 映射、唯一 XML、双实现/Bootstrap 和六项 hash 锁定私有 DLL 组装，不复制 PlayerExports、ONNX、日志、安装覆盖或源码其他文件。ZIP 不再“扫安装目录再排除”，逐文件匹配 Stage，版本只在 ZIP 中生成，不回写源码/Stage。旧 Stage 中与来源不同的 9 个文件先私有备份并复验；Debug/Release 新 Stage 各 **123** 文件，两次无输入重组的文件集合/hash 稳定；两种 ZIP 与 Stage 集合/hash 独立核对通过，未知文件及旧打包参数失败关闭。代码地图 **780** 锚点两模式通过。详细见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **未闭门禁**：部署入口目前失败关闭，未实现/验收“仅程序文件”的事务部署；33 项未映射 GUI 和 TPAC 等必需资产还须 loader、来源与权利核对或功能等价替代，当前 Stage/ZIP 不可宣称功能完整或发布就绪。ONNX 用户模型定位/全组锁、其余旧资料退役及 F5 同候选总验收也未完成。未覆盖游戏、启动游戏、修改原始 `.sav`、推送或写 G:；实机旧档 **NOT-RUN**。**F1–F5 保持未完成。**

## 以下为 TerminalSettings 接线交接（历史）

# AF2.0 TerminalSettings 用户根与迁移契约已验，继续 F1–F5（2026-09-26）

- **本轮产品/测试 `7c39b200`**：TerminalSettings 活读写改为 typed `UserData/Settings`，损坏旧文件不被保存覆盖，候选校验后原子替换；写失败时 UI 内存值回退。显式迁移器对旧安装/仓内单文件按私有备份、hash、冲突保留与中断续作处理。两真实来源只读盘点均为 **0**，故无真实设置激活，也未读取旧模块作为日常回退。合成迁移及 DataPaths **50** 项通过，当前六构建均 0 warning/error；代码地图 775 锚点两模式通过。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **下一步**：ONNX 定位及依赖锁、F3 干净 Stage/ZIP 白名单、F4 其余资料与资产许可/退役，之后 F5 同候选离线总验收。无游戏覆盖/启动、原始 `.sav` 修改、Stage/ZIP、推送或 G: 外仓写入；实机旧档与发布验收 **NOT-RUN**。**F1–F5 不标完成。**

## 以下为 Prompt 迁移交接（历史）

# AF2.0 Prompt 分层与真实迁移已验，继续 F1–F5（2026-09-26）

- **本轮产品/测试 `528031c5`**：普通/Policy Prompt 读取改为用户覆盖→随包默认→owner 安全回退；编辑写入用户根且候选原子替换，损坏覆盖保留而不回写模块或源码。日志、模型下拉缓存改用 typed 用户根；30 份随包 Prompt 默认已加入 SHA 锁。D: 安装来源 30 份 Prompt 已分别私有备份并逐文件 hash 验证，14 份非基线文件无覆盖激活，重复执行激活 0；安装原件未改。合成迁移、Prompt/Policy 契约 9055 断言、DataPaths 44 项、内容映射静态核验及当前 Debug/Release × 1.3/1.4 + Bootstrap 六构建通过。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **仍未完成**：TerminalSettings、ONNX 定位及其迁移，F3 干净 Stage/ZIP 白名单，F4 其余资料与资产权利/退役，F5 同候选离线总验收。完整 J15 runner 会清理未授权目录，因此本轮仅运行其静态映射检查；游戏启动、部署、原始 `.sav` 修改、实机旧档与发布验收均 **NOT-RUN**。没有推送或 G: 外仓写入。**F1–F5 不标完成。**

## 以下为 PlayerExports 迁移交接（历史）

# AF2.0 PlayerExports 双来源真实迁移可恢复完成，继续 F1–F5（2026-09-26）

- **状态**：产品/测试 **`38c5effd`** 将游戏与编辑器 PlayerExports 接同一 typed 用户根、未迁移默认根失败关闭；**`d8acf666`** 修复首次真实迁移发现的 Windows 临时路径过长，并有合成长路径回归。安装与仓内两来源各 3139 项已分别备份到私有 Recovery，安装激活 3139、仓内因相同字节激活 0；独立逐文件 hash/大小与原件一致，两来源重复运行均无再次激活。首次失败停在备份第 51 项之前、无活动数据/完成记录，修复后在同一恢复材料上续作；D: 与仓内原件、原始 `.sav` 未删除/修改。此前 DataPaths 44 项、编辑器路径正反例、编辑器 App 和六构建、762 锚点地图仍有效，但它们不证明剩余 F1–F5。详细见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **下一步**：接普通/Policy Prompt、可编辑配置、TerminalSettings、日志/缓存、ONNX 的真实消费者及合成迁移/回退；之后 Stage/ZIP 白名单、其他旧资料迁移与仓内旧树退役。BAAI 原模型 MIT 标签不能直接证明 ONNX 转换仓文件可再分发，F4 许可仍阻断发布。无 Stage、部署、打包、游戏启动、原始 `.sav` 修改、推送或 G: 外仓写入；编辑器完整编辑 smoke、实机旧档与发布许可 **NOT-RUN**。**F1–F5 仍未完成。**

## 以下为 AF2.0 设计交接（历史）

# AF2.0 完整资源/数据终态方案，待实际实施（2026-09-26）

- **最新请求**：用户拒绝“保护项原位排除、缩范围收尾”，要求完整最终方案；不再重复之前的二选一问题。
- **方案**：[J15 计划第 9 节](docs/plans/j15-content-profile-plan.md#j15-af2-final-state)定义独立用户数据、唯一默认源、可恢复迁移、取消源码回写、纯 Stage 与发布白名单，分 F1–F5 有限批次。意图 `093e4d3a`；实际 consumer 坐标与范围变化集中于[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)，[范围图](docs/architecture/af-framework-code-scope.md)仍反映已实现的旧候选。
- **当前事实**：这轮只交付设计，生产代码/脚本/真实数据未动；此前 107 项资源映射和离线结果保持，但不能用于证明新终态已实现。不是整个 AF2.0 或 J15 完成，真实旧档、发布许可和迁移仍须对应验收。
- **下一工程**：F1 的真实读写矩阵、路径/默认覆盖契约及反例，再做 F2 实际接线。F1–F3 可用合成数据推进，不让尚未闭合的素材许可阻塞不相关工程；真实批量迁移/外写/删除另用具名清单。无部署、推送、全局安装或 J16/J17。

## 以下为已被完整终态方案取代的过渡交接

# J15c 默认源已归位并离线验收，剩余数据/素材边界待闭合（2026-09-26）

目标状态：**blocked**。`0815fb6b` 后复核未发现新增变更或解除保护项门禁的决定；同一缺口连续三个目标轮次仍在，独立工作已完成。待下述处置决定/来源凭据到位后恢复，不重跑既有门禁制造进度。

- **状态**：`J15c_DEFAULTS_OFFLINE_VERIFIED / J15_PARTIAL_HOLD`。产品 `9db8fa8b`，测试消费者修复 `a3fa77af`；30 份默认 Prompt 原样 R100 归位，映射 107，安装覆盖/运行路径不变。完整目标未缩小；详见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)、[J15 计划](docs/plans/j15-content-profile-plan.md)。
- **已验**：完整 107 项合成投影、四种安装状态及重复组装、Bridge 24/隔离 12、Policy 216/1406/UI 387、六构建零警告错误、四实现七资源、旧 Native ABI、1620 DLL 元数据和当前 DLL Phase8 均通过；755 锚点两模式通过。额外重复 runner 清理被自动审查拒绝，未绕过，新增静态段单独通过，证据层级见台账。
- **最新补证 `2b61f1c5`**：PlayerExports 独立合成测试 45 断言及四个行为变异拒绝通过，没有读取真实导出或清理旧 fixture。ONNX 五文件已匹配固定上游提交，不再是来源未知；分发许可与真实数据迁移仍未闭合。hash、源码坐标、失败诊断只记主台账；新增子测试不冒充整体 runner 重跑。
- **下一步**：数据合成保护、模型来源和剩余 GUI/素材的可用 Git/loader 证据已查完，细节见主台账。已请求保护项处置决定：用户明确批准原位排除本次迁移，或维持全范围并补数据备份/迁移要求及素材来源凭据；未答复，不自动缩范围。之后再作 J15d 退出审计，不把 HOLD 算完成。不重做已迁 Prompt、已验六构建或被拒清理。无 Stage/Deploy/Package/push/外写；实机、旧档、provider、Gauntlet、音频、帧性能 NOT-RUN，`.dotnet-cli-home/` 保留。

## 以下为 FeatureBridges 最小片历史交接

# 当前交接：J15c FeatureBridges 已归位，完整验证待授权（2026-09-26）

- **状态**：`J15c_FEATUREBRIDGES_VERIFY / J15_PARTIAL_HOLD`，产品/测试 `7bc2ffbf`。FeatureBridges 默认源按 Foundation.Runtime 原样 R100 归位；映射 77、旧根 HOLD 40，运行路径/正文/七嵌入资源/C# 行为不变。完整 J15 目标未缩小，J15a/b 不重做；详见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)、[J15 计划](docs/plans/j15-content-profile-plan.md)。
- **已验**：五项 J15 无写入静态函数、15 项无临时文件 Bridge 测试、16/12 Bridge validator、inventory 7 和 unknown=0、755 锚点两模式通过。修复当前 Bridge 清单两处遗留 RuleBehavior 源路径，未改历史记录或 runtime。
- **下一步**：待确认具名构建/测试生成目录清理，再跑完整投影、Bridge 隔离、六构建和候选 DLL 门禁；这些当前 NOT-RUN，不能继承 J15b 结果。CustomPrompts/用户数据/模型/素材等继续按台账 HOLD 与授权条件推进，未到 J15d，更不是 J15 完成。无 Stage/Deploy/Package/push/外写，`.dotnet-cli-home/` 保留；实机/旧档/provider/Gauntlet/音频/帧性能均 NOT-RUN。

## 以下为 J15b 历史交接

# J15b 已确认静态内容有限离线完成（2026-09-26）

- **状态**：`J15a/b_OFFLINE_VERIFIED / J15_PARTIAL_HOLD`；J15b 产品/测试切片 `04056ce7`。在 J15a 7 项基础上，69 个已确认静态 JSON/GUI/XML/语言文件按 15 个 module/foundation owner 逐文件 `R100` 迁入 `content/`，映射现为 76 项；运行时 `ModuleData`/`GUI` 目标、7 个 EmbeddedResource、loader/LoadMovie/sprite/XML/Bootstrap/public API/保存身份均不变。实际责任、坐标与精确 HOLD 见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[J15 计划](docs/plans/j15-content-profile-plan.md)与[范围图](docs/architecture/af-framework-code-scope.md)。
- **验证**：J15 契约 `76 mappings / 69 J15b / 41 holds / 76 real projection / 8 invalid / 4 GCCZ fallback / 11 overlay aliases`；Prompt 36、两个 PromptLab、Xihai Core 88、inventory 7/11 与 unknown=0、当前 Debug 1.4 完整 Phase8 均 PASS。原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；四实现 DLL 各仍恰好 7 个资源且字节一致，卫星资源 0。代码地图仍绑定未改 J14 C# 产品 `e58f3558`，755 锚点两模式 PASS。
- **HOLD/停点**：旧 ModuleData/GUI 精确保留 41 项（异常/动态 profile、FeatureBridges、sprite 索引、临时/旧版/来源未确认图）；J15c 的 CustomPrompts、PlayerExports、VoiceMapping、ONNX、AssetPackages 等也未动。完整 Xihai StaticVerifier 为 10/13，未通过的是外部 TPAC `nacisword1`、其连带初始化和 AF bridge 签名漂移，不冒充通过。没有 Stage、Deploy、Package、push、游戏/外仓/玩家数据写入；两版本实机、旧档、安装覆盖、Gauntlet、provider、音频、帧性能均 NOT-RUN。`.dotnet-cli-home/` 保留。按用户要求停在 J15b；后续若明确继续，下一项是 J15c。

## 以下为 J15 计划就绪历史交接

# J15 content / profile 计划就绪，尚未施工（2026-09-26）

- **历史状态**：`J15_PLAN_READY / IMPLEMENTATION_NOT_STARTED`；已由上方 J15a 结果取代。本段其余规划边界仍作为后续 J15b/c/d 的历史输入。
- **历史下一动作**：原计划从 G0 开始逐文件分类和授权；J15a 已按实际授权完成，未由此授权后续批次。
- **历史验证边界**：当时只完成规划和 755 锚点定位，没有施工；现以主台账最新条目为准。

## 以下为前序 J14 产品交接

# J14 三渠道公共 API 离线完成（2026-09-26）

- **状态**：`J14_OFFLINE_VERIFIED`。Courier 公共票据/一次派出/权威阶段与失败回执已开放；三渠道共用限额、幂等和开始前取消。最终产品 `e58f3558`，测试修复止于 `a90e1b45`；责任、候选 SHA、失败修复与全部证据见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)及[V1 指南](docs/architecture/af-public-api-guide-v1.md)。
- **验收**：六构建、四 DLL 元数据/旧 Native ABI、三渠道及真实记忆写入/回读、保存/Bridge、显式当前 DLL Courier/Phase8 回放、755 锚点地图两模式和差异检查通过。仅离线完成；两版本实机、旧档、真实 provider、音频、独立子 MOD 游戏加载与帧性能分别 NOT-RUN。
- **停点**：J14 收口，不进入 J15，不推送、Stage、部署、打包、写游戏/外仓/玩家存档、安装工具或修改默认入口。`.dotnet-cli-home/` 保留；后续实机/发布或新阶段需新的明确请求。下方旧 ACTIVE 和继续指令均被本段取代，仅作历史证据。

## 以下为 J14b2 及更早交接（历史切片）

# 当前交接：J14b2 内部运输回执已接线，继续 b3/c（2026-09-26）

- **状态**：`J14b_ACTIVE / J14_ACTIVE`，产品/测试 `b2572623`。原运输各阶段、实际历史/信件接受、终态与精确 session/run 防陈旧回写已接线；179 项准入/生命周期、44 项后处理、552 项 Prompt、59 项 liveness、Native/Scene 55 与 Debug 双版本+Bootstrap 通过。详细身份与未覆盖范围见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[范围图](docs/architecture/af-framework-code-scope.md)。
- **继续执行**：b3 增加 V1 Courier 票据/提交与只读阶段投影，外部消费者/兼容验证后再开放能力；然后 c 完成当前候选 SHA 回放、真实记忆写入/回读、旧 ABI、六构建和代码地图两模式。Courier 仍 `NotSupported`，尚不能标 J14_OFFLINE_VERIFIED。仅本地；保留 `.dotnet-cli-home/`，不 push/Stage/部署/打包/外写/J15；实机、旧档、provider、音频、子 MOD 游戏加载、帧性能均 NOT-RUN。

## 以下为 J14b1 交接（历史切片）

# 当前交接：J14b1 准入完成，接续运输回执（2026-09-26）

- **状态**：`J14b1_ADMISSION_VERIFIED / J14b_ACTIVE / J14_ACTIVE`；本地产品切片 `4bc855cd`、`8fb16385`、`b6daf65f`，105 项准入检查、Native/Scene 55、Debug 双版本及 Bootstrap 通过。真实责任及证据见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[代码范围图](docs/architecture/af-framework-code-scope.md)；JSON 地图仍是历史 J14a 基线，不声称本轮最终矩阵已过。
- **下一动作**：实施 b2 实际 session 阶段/动作与历史/回信交付/运输终止及失败回执；当前内部成功派出只保持 Running，Courier V1 仍未开放。再做 b3 和 c 最终离线矩阵及地图重绑，按[J14 计划](docs/plans/j14-public-api-plan.md)完成整体离线验收。保留 `.dotnet-cli-home/`；仅本地实施/测试/提交及本轮限定四个构建目录，无 push、Stage、部署、打包、游戏/外仓/存档写入或 J15。实机/旧档/provider/音频/帧性能仍 NOT-RUN。

## 以下为 J14a 交接（历史基线）

# J14a Scene 有限离线收口（2026-09-26）

- **状态**：`J14_G0_BASELINE_VERIFIED / J14a_OFFLINE_VERIFIED / J14_ACTIVE`。产品/测试 `1740b338`, `79fa7c48` 已开放真实 Scene 公共票据→原群组/接力→speech/后处理/记忆/AFEF 回执→终态，Native 原签名与 UI 入口不变；Courier/J14c 尚未完成。具体 owner、消费者和未覆盖责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)及[代码地图](docs/architecture/af-framework-code-map.json)。
- **离线证据与下一步**：Scene 群组 18、请求生命周期 34、后处理 37、独立外部消费者正常/枚举重排各 55、ChannelCutover 133、NativeCompletion 186、V1 142、四实际 DLL metadata 1252；原脚本在用户授权四个精确仓内产物目录、不带 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；730 锚点地图 recorded/working-tree 通过。旧 MemorySummary terminal runner 的既存抽取锚失效，此次未通过，详细限制与 SHA 见主台账。下一包按[计划](docs/plans/j14-public-api-plan.md)做 J14b Courier，随后 J14c 最终候选；不能称整体 J14、实机或发布完成。真实游戏、旧档、provider、音频、帧性能 **NOT-RUN**；不 push、Stage、部署、打包、写游戏/外仓/存档或启动 J15。原未跟踪 `.dotnet-cli-home/` 保留。

## 以下为 J13 交接（历史）

# 当前交接：主体 J13 最终离线收口（2026-09-25）

- **状态**：`J13_OFFLINE_VERIFIED / J13g_OFFLINE_VERIFIED`，仅按[原 J13 计划](docs/plans/j13-domain-owners-plan.md)完成 a–f owner 与 g 最终离线门禁，不是实机或发布验收；J14 未启动。最终产品 `39cf9d47` 修复 Onboarding Base URL 取消后、主线程消费前的迟到结果竞态；证据目录 `894acdfc`，详细责任、候选身份和风险见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[范围图](docs/architecture/af-framework-code-scope.md)。
- **最终证据**：原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；当前 Debug 1.4 SHA256 `03D0FACFC1199B7BFA68B8BDEEABCFF8B69E5B9A97AE6BB279050C33509F0E55` 完整 Phase8 当前 DLL 回放通过。V1 API 119、四 DLL metadata 1148、Persistence Profile 142 key/168 binding、Identity 142 SyncData/36 CampaignBehavior、Bridge/readiness、Native/Scene/Courier/J12 定向回归与 722 锚点代码地图均通过；证据层级和命令见主台账。真实游戏、旧档、provider、UI 焦点、音频、帧性能仍 **NOT-RUN**，Phase8 目录仍 `REPRESENTATIVE`，不能称发布 READY。
- **交接边界**：本次只收口 J13；不 push、Stage、部署、打包、写游戏/外仓、改自动化或清理 `.dotnet-cli-home/`。J14 如需开工须另行授权。

## 以下为 f 交接（历史）

# 当前交接：主体 J13f UI/Overlay/Onboarding 有限离线收口（2026-09-25）

- **状态**：`J13f_OFFLINE_VERIFIED / J13_ACTIVE`，仅本包离线完成，下一步按[原计划](docs/plans/j13-domain-owners-plan.md)执行 **J13g** 最终候选门禁；J14 不开始。真实 owner 与保留 Campaign/Gauntlet/Native/百科边界见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)。产品/回放 `1e2bd80a`、`a2e9b8eb`、`26bc72fa`、`48bcc48a`，Native 测试入口修复 `61f8720a`。
- **离线证据**：Debug 1.3/1.4 + Bootstrap 0 warning/error；当前 Debug 1.4 `AnimusForge.dll` SHA256 `E07886118A440F3B462EE7C6A0182213B5C00BD42391AF76435E52EDD51156A5` 的完整 Phase8 包含五个 J13f owner 回放及 UI 宿主源码契约；Native admission 44/44、presentation 46/46，source inventory 7；[代码地图](docs/architecture/af-framework-code-map.json) 722 锚点 recorded/working-tree 通过（仅定位）。真实 Gauntlet/Campaign/provider/旧档/帧性能 **NOT-RUN**；Release 和最终 API/保存/Bridge 门禁留给 g，不冒充已验。
- **下一条具体动作**：以当前提交为候选，先核实 Release 两个仓内原脚本清理目标的绝对路径和 reparse，再运行原脚本 Debug/Release 六构建；随后四实现 DLL API/metadata、Persistence Profile/Chunk/Identity、Bridge/readiness/source inventory、当前 DLL Phase8、Native/Scene/Courier 与 J12 相关回归及地图两模式，最后主台账/交接收口。`.dotnet-cli-home/` 保留；无 push、Stage、部署、打包、游戏/外仓写入或自动化改动。

## 历史交接（以下各节按原记录保留）

# 当前交接：主体 J13e1 Duel 有限离线收口（2026-09-25）

- **状态**：`J13e1_OFFLINE_VERIFIED / J13_ACTIVE`。Duel 模块持有 exact 受理/运行转换与三种终局 typed 效果投影，原 Mission/Harmony/保存/Courier 薄适配保留。意图 `8cc8ef63`，生产/回放 `054781b1`、`88643619`、`ca49023f`、`d0d15a57`、`d89a13b5`；一基坐标、Patch 清单、覆盖和未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4+Bootstrap 六构建成功；Debug 1.4 SHA256 `64CBF30A1F436F1D05B8A0A8817DBA9A4A186C568F9484080E67B4427BC6A700` 的 Phase8 含当前 DLL exact owner 行为回放与 e1 聚合契约通过；DuelOutcome 20/20、DuelDispatch 16/16、ProductionDuel 35/35 × Debug/Release；V1 119/四 DLL metadata 1064、PersistenceIdentity 142/36、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 617 锚点两模式通过。实机 Mission/Harmony、旧档、MCM、经济副作用与帧性能均 **NOT-RUN**。
- **下一条具体动作**：按[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e2 Taunt**：先核对和平冲突 allowlist/原场景伤害上下文、`SceneTauntBehavior`/五个原 patch/生产消费者与关闭恢复路径，再逐责任切片迁真正 owner、做正反/失效回放和聚合契约；随后 e3–e5，不提前 J14。`.dotnet-cli-home/` 原未跟踪目录保留；无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 d4 交接（历史）

# 当前交接：主体 J13d4 WarStats 有限离线收口（2026-09-25）

- **状态**：`J13d4_OFFLINE_VERIFIED / J13_ACTIVE`。WarStats 活动/历史/旧账及近期序号、宣战/计数/死亡/归档/保存恢复归唯一 ledger；原 CampaignBehavior、v1–v5 保存键、事件装配和终端消费者保留。产品/行为 `6d5657e9`、`c8cc0efd`、`ab05a736`、`db1830bc`、`f722dd41`，聚合接线 `f0cc3ede`/`6b0c07f7`；一基坐标、覆盖/未覆盖责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **当前证据**：原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `656D4E9BF9E5894FEA899B6E3CA05BC264B1F725A16D32853E4874B6F051EC07` 的 Phase8 含 d4 状态反例及聚合契约通过；V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、迁移 fixture 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 600 锚点两模式通过。合成数据不是实机 Campaign/旧档/终端点击/帧性能验收，均 `NOT-RUN`。
- **下一条动作**：按[原计划](docs/plans/j13-domain-owners-plan.md)与场景伤害/军团会面案例先审 e1 Duel 的 host、typed outcome、三个现有 Duel 套件和生产消费者，开工意图后逐验证切片迁 owner；依序 e2–e5，不提前 J14。`.dotnet-cli-home/` 原未跟踪目录保留；未 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 d3 交接（历史）

# 当前交接：主体 J13d3 WorldEvents 有限离线收口（2026-09-25）

- **状态**：按[原 J13 计划](docs/plans/j13-domain-owners-plan.md)，`J13d3_OFFLINE_VERIFIED / J13_ACTIVE`；WorldEvents 收件箱 records/unread/stable-key/version 归唯一 owner，原 CampaignBehavior、v1 保存键和政策/UI/档案入口保留。产品/行为 `62ec9065`，聚合接线/政策 UI 契约 `b07898cb`；具体一基代码坐标、保留职责和风险见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **证据边界**：原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `C70D15E9B5251356F98FBDBA633E61205A45EB1B48D18FB0CE39C68E5BDE17B0` 的 Phase8 含 d3 行为/聚合通过，Policy UI 387、V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 586 锚点两模式通过。真实 Campaign、旧档、Gauntlet、provider/音频及帧性能均 `NOT-RUN`；离线数据不冒充实机。
- **下一步**：依计划先读 d4 WarStats 的事件→计数→归档→v5 保存/终端消费链，逐验证切片迁真正 owner、保持原 `AFWarStatsTerminal` 类型及事件注册；随后 e1–e5。`.dotnet-cli-home/` 未触碰；未 push、Stage、部署、打包、写游戏/外仓、改自动化或启动 J14。

## 以下为 d2 交接（历史）

# 当前交接：主体 J13d2 Proactive/Issue 有限离线收口（2026-09-25）

- **状态**：按[原计划](docs/plans/j13-domain-owners-plan.md)的 d2 范围，`J13d2_OFFLINE_VERIFIED / J13_ACTIVE`；Social 的资格/状态/pending opening 与 Issue 的 offer/in-progress/turn-in/完成回执已迁入各自 owner，Campaign/TW/反射/窗口保留原 host 适配。产品提交 `a59a0fc0`、`fe74d4fa`、`3f330c58`、`f739dbaf`、`4e4ad8bf`、`96d058ca`，聚合证据 `36037592`、`7fc6f680`，重复领取行为回放 `d34fd858`。
- **当前证据**：原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；Debug 1.4 SHA256 `0071A62D00D40E4113222F7A3E1FCDA641CABFA26EA4FC9371301025EF745544` 的 Phase8 包含 d2 聚合与行为反例通过，其中当前生产方法对合成的原版 Issue/Hero 验证未受理可 offer、已受理及错 owner 不可 offer；V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 575 锚点 recorded/working-tree 通过。真实 Quest/PartyScreen/旧档/provider/UI/音频/帧耗时未验，不以合成 fixture 冒充实机。
- **下一步**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[代码范围图](docs/architecture/af-framework-code-scope.md)进入 J13d3 WorldEvents，随后 d4 WarStats；J13e/f/g 未由 d2 替代。未 push、Stage、部署、打包、写游戏/外仓或 J14；`.dotnet-cli-home/` 不动。

## 以下为 d2 资格切片交接（历史）

# 当前交接：主体 J13d2 主动资格归 Social，继续 Issue（2026-09-25）

- **状态**：产品/回放 `a59a0fc0`，Proactive 的资格、状态、pending opening/消费均有有限离线证据；**J13d2/J13 仍 `ACTIVE`**，Issue 的 offer/in-progress/turn-in/完成回执未闭合。
- **本片证据**：完整主动候选/各需要资格算法迁入 `src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs`，原 host 留 Campaign/存档/会面适配；前驱源码字节级重组相等。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；Debug 1.4 SHA256 `DD81C07FD3F4146DC696A498A88467F703B886A8CBA69548BACABF1DCC1A85B5` Phase8 全通过，API 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 560 锚点两模式通过。真实游戏/Quest/旧档/provider/UI/音频/帧耗时未验。
- **下一步**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)核对三渠道、Quest 身份、offer/受理/交付与完成事实，迁入 Issue owner 并做 d2 聚合退出门。未 push、Stage、部署、打包、写游戏或外仓；`.dotnet-cli-home/` 不动。

## 以下为 d2 主会话切片交接（历史）

# 当前交接：主体 J13d2 主会话 owner 有限切片完成，继续资格/Issue（2026-09-25）

- **状态**：分支 `codex/af-main-refactor-continuation-20260831`，产品/回放 `f1ecb315`；J13a–c/d1 和 d2 opening、冷却、扫描、主会话/Issue 派遣 pending 均有有限离线证据，**J13d2/J13 仍 `ACTIVE`**，不提前 d3/J14。
- **本片证据**：Social 唯一 session owner 接管原保存 DTO 的读档规范化、重复启动、追逐探测/阶段/过期/取消与一次疲劳；原 Campaign/TW/AFEF 适配仍保留。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `D42E34E7105D35D9289D4FAD8DCEBC0D879E47E4159EE9B87D063EB17A835DEB` 的 Phase8 全通过，V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 554 锚点两模式通过。真实游戏/旧档/provider/UI/音频/帧耗时未验。
- **下一条具体动作**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)先关闭主动候选资格的 Social 归属与聚合反例，再迁 Issue offer/in-progress/turn-in/完成回执并做 d2 聚合退出验收。未 push、Stage、部署、打包、游戏/外仓写入或改自动化；`.dotnet-cli-home/` 不动。

## 以下为 d2 增量扫描切片交接（历史）

# 当前交接：主体 J13d2 增量扫描 owner 有限切片完成，继续主会话/Issue（2026-09-25）

- **状态**：分支 `codex/af-main-refactor-continuation-20260831`，产品/回放 `6eb3d170`；J13a–c/d1 及 d2 opening、冷却、扫描/Issue 派遣 pending 各有限片有离线证据，**J13d2/J13 仍 `ACTIVE`**，不提前进入 d3/J14。
- **本片证据**：唯一 Social owner 负责扫描实例、批大小、统计/排名、精确完成；原每帧 16 队伍/1.5ms 上限保留。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `507BE34AEDA844518AC537DC43E05C773D96DDA1FEACB36EBF43DB919FF0580C` 的 Phase8 全通过，V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 546 锚点两模式通过。实机帧耗时、Quest/旧档/provider/UI/音频仍 `NOT-RUN`。
- **下一条具体动作**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)读主动主 session 启动/失效/取消/完成的全部消费者并归属 Social，再关闭 Issue offer/in-progress/turn-in/完成回执，按[原计划](docs/plans/j13-domain-owners-plan.md)的 d2 聚合退出门验收。未 push、Stage、部署、打包、游戏/外仓写入或改自动化；`.dotnet-cli-home/` 不动。

## 以下为 d2 冷却切片交接（历史）

# 当前交接：主体 J13d2 冷却/扫描节流有限切片完成，继续 d2（2026-09-25）

- **状态**：实际分支 `codex/af-main-refactor-continuation-20260831`，产品/回放 `43566f0f`；J13a–c/d1 保持有限 `OFFLINE_VERIFIED`，d2 opening/派遣 pending 首片及本冷却片有限 `OFFLINE_VERIFIED`，但 **J13d2/J13 仍 `ACTIVE`**。用户恢复推进的请求不授权 push、Stage、部署、打包、外仓写入、自动化或 J14。
- **本片/证据**：唯一 Social owner 接管 Hero/类型/外交话题冷却与 global/lastScan、存档导入和小时裁剪；host 保留 Campaign/MCM/DTO 适配。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建均零警告错误；当前 Debug 1.4 SHA256 `D8C3AC41C447D13CE6DB3FA2D6F270C4CF811D3BB3EE6E61E736A8394E9A51D2` 的 Phase8 全通过；V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 539 锚点两模式通过。真实 Campaign/Quest/旧档/provider/UI/音频/帧性能仍 `NOT-RUN`，不可当实机验收。
- **下一条具体动作**：按[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)先读 `ProactiveNpcRequestBehavior.cs:410,528,4288` 的资格/增量扫描/主 session 转换并做下一个可回放闭包，随后 Issue offer/in-progress/turn-in/完成回执；再依[原计划](docs/plans/j13-domain-owners-plan.md)推进 d3/d4、e、f、g。不要以本片替代完整 J13 离线目标。`.dotnet-cli-home/` 未跟踪且原样保留；需撤销以定向 inverse/revert，不 reset/改历史。

## 以下为 d2 前一切片交接（历史）

# 当前交接：主体 J13d2 生命周期首片离线验证，d2 整包继续（2026-09-25）

- **工作区与状态**：实际分支 `codex/af-main-refactor-continuation-20260831`，最新产品/回放 `580a1466`。J13a Weekly、J13b Kingdom、J13c Persona、J13d1 Notoriety/Romance/Recruitment 维持有限 `OFFLINE_VERIFIED`；d2 的主动 opening 与原版 Issue 同伴窗口迟到回调首片有限 `OFFLINE_VERIFIED`，但 **J13d2/J13 仍 `ACTIVE`**。此前暂停/推送交接只作历史，不授权本轮再次推送。
- **本片证据**：原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `DDFBE3B5BE81CCED67D110524EE01F9AADDAC273D5974184E1367A3D49401300` Phase8 含新增生产回放通过，V1 119/四 DLL metadata 1060、PersistenceIdentity 142 key/type 对/36 behaviors、source inventory 7、代码地图 531 锚点两模式通过。真实 Campaign/Quest/party-screen、旧档、provider、UI/音频、帧性能仍 `NOT-RUN`；旧 Persona/Channel source-parity 与 terminal harness 问题未由本片解决。
- **下一步**：按[主台账当前段](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[原计划](docs/plans/j13-domain-owners-plan.md)继续 d2 主动资格、冷却、session 生命周期，以及 Issue offer/in-progress/turn-in/完成回执；再按既定顺序推进 d3/d4、e、f、g，不缩小 J13 离线目标或提前 J14。[代码范围](docs/architecture/af-framework-code-scope.md)和[代码地图](docs/architecture/af-framework-code-map.json)标出已迁与保留 host。`.dotnet-cli-home/` 未跟踪且原样保留；未 Stage、部署、打包、写游戏/外仓、修改自动化或推送。需撤销时对 `580a1466` 作定向 inverse/revert，不 reset/改写历史。

## 以下为历史交接，不作为当前执行指令

# 历史交接：J13 暂停于 Weekly a2（2026-09-25）

- **停点与半途改动**：当前分支 `codex/af-main-refactor-continuation-20260831`；截至本交接前的最新提交 `66eda319`。本次中断只发生在阅读/设计下一步多波协调归属期间，尚未编辑生产或测试代码；无未提交的已跟踪改动。唯一未跟踪的 `.dotnet-cli-home/` 保留原状，不提交、不清理。
- **实际完成**：J13a 的 a1 调度/材料已有限 `OFFLINE_VERIFIED`；a2 请求/完成生命周期仍 `ACTIVE`。最近产品提交 `78b87434`，回放提交 `c0221d02`：部分提交与异常恢复、双排队波次源失效均有受控回放。产品切片经原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8、V1 119/四 DLL metadata 1060、入口 11/source 7、492 锚点地图已通过。交接本身未重跑构建；这些证据不等于实机或完整多波验收。
- **继续时的第一步**：先按[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)复核 Git、代码地图和候选新鲜度；完成 a2 真实 minute orchestration/Campaign tick、部分提交＋UI 显式重采＋一次发布组合、提交工作量/积压和余下动态源门禁，随后 a3，再依计划 J13b–g。不要把已排队两波 fixture 当作真实 60 秒多波。实机、旧档、provider、音频、帧性能仍 `NOT-RUN`；本交接不授权 Stage、部署、打包、写游戏/外仓或 J14。

## 以下为双排队波次切片交接（历史）

# 当前交接：J13a a2 双排队波次源失效回放（2026-09-25）

- **最新切片**：`c0221d02` 为真实 `EnqueueWeeklyWaveLaunchAsync`/`ProcessPendingWeeklyWaveLaunches` 补受控双排队波次回放：每次 pump 只启动队首，同周源变化使第二波网络前取消。当前 Debug 1.4 SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8 全回放通过；产品未改，沿用前片原脚本六构建通过。**没有**实测 60 秒延迟或真实 Campaign 多波；a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：补真实 minute orchestration/Campaign tick 与部分提交、UI 显式重采、一次发布组合，并量化提交工作量/积压。见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为部分提交与异常恢复切片交接（历史）

# 当前交接：J13a a2 部分提交与异常恢复切片离线闭合（2026-09-25）

- **最新切片**：`0a1d6c8d` 补真实 pending commit 的“已有胜出者 + 另一目标 RPM 失败”组合回放；`78b87434` 使提交异常只对未完成目标生成需显式重采的恢复上下文、结算等待者，并尝试幂等补发已写记录通知。原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8 显式候选、V1 119/四 DLL metadata 1060、入口 11/source 7、492 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；live 弹窗、实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：补多 wave/Campaign tick、真实 UI 显式重采/一次发布组合与提交工作量上界；核对余下动态源投影，再判定 a2 有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为批量失败恢复元数据切片交接（历史）

# 当前交接：J13a a2 批量失败恢复元数据切片离线闭合（2026-09-25）

- **最新切片**：`683987dd` 修复批量失败到暂停恢复 UI 的分类丢失：按失败目标对应批次传递真实 RPM/配额/Retry-After/尝试数，HTTP 后续成功清除旧 429 标记。四目录复核后原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `C22AC98C51B314330927FB848E80D9B2C9D80F75E01AE85C81BB9EE50187C7C4` 的 Phase8 分类/目标隔离回放、V1 119/四 DLL metadata 1060、入口 11/source 7、490 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；live 弹窗、实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：补多 wave/Campaign tick、部分成功/失败 UI/显式重采和一次发布的组合回放；核对剩余动态投影源状态，再判定 a2 有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为王国拓扑事件失效切片交接（历史）

# 当前交接：J13a a2 王国拓扑事件源失效切片离线闭合（2026-09-25）

- **最新切片**：`dc2dd917` 将六个已注册的王国/家族变更事件接到 Weekly 修订 owner，补足无同周材料写入时当前统治者、归属等 live 投影的失效信号。四目录复核后原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `2D1E29B437B67FF955BF2511A3749B3FB8CFC54B46E0723F65FBAA746294C942` 的 Phase8 真实空王国事件负例、V1 119/四 DLL metadata 1060、入口 11/source 7、487 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：覆盖多 wave/Campaign tick、部分失败 UI/显式重采和一次发布的组合回放，核对余下动态投影的源重验；再决定 a2 是否满足有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为源素材修订切片交接（历史）

# 当前交接：J13a a2 源素材修订门禁切片离线闭合（2026-09-25）

- **最新切片**：`d4443bcb` 将同周事件素材、NPC 行动和开局概要的请求期修订快照接入自动/手动批量周报、每波发起及写前重验。获准四目录复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `1560FCA28974CEE714465BCF8B042BC8777B5A012B30625D2F4DAC5F683799E5` 的 Phase8 真正 block 写前拒绝回放、V1 119/四 DLL metadata 1060、入口 11/source 7、484 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：核对 live Kingdom/统治者等未入修订 owner 的动态素材投影；补多 wave/Campaign tick、部分结果 UI 与一次发布的组合证据，之后才评估 a2 有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为跨批次部分结果结算切片交接（历史）

# 当前交接：J13a a2 部分结果跨批次结算切片离线闭合（2026-09-24）

- **最新切片**：`d6fea0e6` 将跨批次已结算 ID 和暂缺目标归 Weekly owner；重复缺失只登记一次，后续批次解析成功会移除早先暂缺，最终只计仍未恢复目标。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `3587CB727BBE95A2B5F3B3CD4289165B41C0204288A6280937B2CC9AB3BAFCF4` 的 Phase8 owner/真实 finalizer 回放、入口 11/source 7、V1 119/四 DLL metadata 1060、477 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：设计并验证与周界/目标相关的 live 素材源提交重验，不能靠目标事件记录状态代替；随后补多 wave/Campaign tick、部分结果 UI 与一次发布组合回放，判定 a2 退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为自动重试配置主线程边界切片交接（历史）

# 当前交接：J13a a2 自动重试配置主线程边界切片离线闭合（2026-09-24）

- **最新切片**：`4721460f` 保留首轮同波并发，第二/三次批量自动重试经 Campaign tick 主线程读取当前 MCM 配置并启动原 gateway；读档结算等待者，旧代/退役 owner 在配置读取前拒绝。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `47452DC940B1E6D4199E6449928088A8303D06BDDDF83D8B934F49F2F0145369` 的 Phase8 强制后台/清理负例、入口 11/source 7、V1 119/四 DLL metadata 1060、472 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：核对独立于已保存目标记录的 live 素材源变化；补多 wave、部分失败、积压与一次发布的组合回放，再判定 a2 退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为 minute wave 主线程发布切片交接（历史）

# 当前交接：J13a a2 minute wave 主线程发布切片离线闭合（2026-09-24）

- **最新切片**：`5ca5e5c3` 把每分钟每波的 UI 通知和初次请求启动排入 Campaign tick 主线程，旧代/读档结算未发波次等待者，原 60 秒间隔与批次汇总不变。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `114EFBCB2B1053BEA585E06AE99A8BE99F79B0CFE04DFAB7D4466D0747960288` 的 Phase8 来源/新鲜度及原队列回放、入口 11/source 7、V1 119/四 DLL metadata 1060、469 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；真实多 wave、实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：审查自动重试延迟后的可变 API 配置读取，把配置快照/执行边界闭合；再处理独立 live 素材源变化及部分失败、积压、一次发布组合回放。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为旧代自动重试拦截切片交接（历史）

# 当前交接：J13a a2 旧代自动重试拦截切片离线闭合、多 wave 待核（2026-09-24）

- **最新切片**：`39606755` 把原请求 generation 传入批次自动重试，发 API 前与回包后拒绝旧代，避免读档后延迟完成又外发旧请求。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `9A6376610A6CE7223AEEDC8FE509B69606A1FFF69ED54FDDF334BF7627C2D303` 的 Phase8/网络前旧代负例、入口 11/source 7、V1 119/四 DLL metadata 1060、466 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：审查后续 minute wave 的主线程通知/配置快照，独立 live 素材源变化，以及部分失败、积压和一次发布的组合回放；a2 全门禁未过。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为批次 Prompt 主线程准备切片交接（历史）

# 当前交接：J13a a2 批次 Prompt 主线程准备切片离线闭合、请求全链路待核（2026-09-24）

- **最新切片**：`f812ec1b` 避免手动/重试后续 minute wave 在后台构建含 live Kingdom/统治者的 Prompt；Campaign tick 按预算逐批准备，旧代/读档结算等待者，worker 未准备就在网络前拒绝。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `E68FF3F12D19C426E3D6611CC1BD24D2BB977838946A94E3689DBA3C13C30962` 的 Phase8/worker 负例、入口 11/source 7、V1 119/四 DLL metadata 1060、465 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：检查多 wave 调度的后台 UI 通知、API 配置快照及 live 源变更边界，补实际队列积压/部分失败/发布组合回放。此切片不代表 a2 收口。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为显式重采切片交接（历史）

# 当前交接：J13a a2 目标变更显式重采切片离线闭合、源状态与批量生命周期待核（2026-09-24）

- **最新切片**：`240c10aa` 将目标编辑冲突与 API/RPM 错误分开；用户明确点击后才重新采集当前周界内原失败分组，任一失踪则不发请求，原已完成分组不重跑。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `E7738A4EE42B65C625D3C65D9748A7ADB582BF1CFD0077664B91C0A71834112C` 的 Phase8/选择反例、入口 11/source 7、V1 119/四 DLL metadata 1060、462 锚点地图通过。UI 仅源码接线，live 弹窗/API NOT-RUN。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：核对并补足独立于目标记录的 live 材料源变更防护，以及 minute burst/部分失败/发布全链路；a2 必要门禁未过不标完成。见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为批量胜出者切片交接（历史）

# 当前交接：J13a a2 批量完整/短报胜出者切片离线闭合、恢复 UI 待施工（2026-09-24）

- **最新切片**：`32a8379c` 使迟到批量回包遇到同周同目标完整报/短报已有完成内容时计为已满足，不覆盖、不重复通知或发布；未完成编辑仍失败。四授权目录复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `9650264CA89CEFC48FD5D7FA9C52A66FEAF3AFB8395FCCF402DF76EAC064D6BE` 的 Phase8、全文/短报/错误周界反例、入口 11/source 7、V1 119/四 DLL metadata 1060、459 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：不完整编辑的失败 popup 仍无正确“重新采集素材”入口；先闭合这条恢复语义，再核对独立 live 源变更、minute burst/部分失败与一次发布。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为手动重试防覆盖切片交接（历史）

# 当前交接：J13a a2 手动重试旧素材防覆盖切片离线闭合、恢复 UI 待施工（2026-09-24）

- **最新切片**：`8b1f703d` 将批量失败的原目标状态传入显式手动重试；当前目标变化时发请求前拒绝旧素材，未变组可重试。四授权目录复核后，Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `231869AC9A7D7854B5066C08D0D350E14EE8B5DD0F6A10ACE02E8DB0CD448F05` 的 Phase8/重试准入反例、入口 11/source 7、V1 119/四 DLL metadata 1060、458 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**，其余 J13 不抢跑；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：目标变更后的失败 popup 仍沿 API 修复/重试 UI，无正确的重新采集材料恢复动作；先闭合该恢复语义，再核对独立 live 源状态、minute burst/部分失败和一次发布。详细证据见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 保持不动。

## 以下为目标记录提交防覆盖切片交接（历史）

# 当前交接：J13a a2 批量目标记录防覆盖切片离线闭合、源/重试门禁待施工（2026-09-24）

- **最新切片**：`d59848d1` 在 `MyBehavior.cs:42429–42464,45326–45397,45558–45742` 捕获目标事件记录原状态，pending commit 期间重验当前 owner、generation 和目标记录；编辑/已完成胜出者/保存素材变化不被迟到回包覆盖。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `FCAB4573DD0A2DE6FC9524BF8A786C93D7AC1C9A35738C46ABFA47FC30FAA066` 的 Phase8、目标状态反例，入口 11/source 7、四实现 DLL metadata 1060、456 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：继续核对批量请求捕获时点之外的 live 材料源和手动/自动重试：目标被用户编辑后显式重试不能重新捕获并覆盖；再处理 minute burst/部分失败/popup 恢复，之后才评估 a2 有限退出门。见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，保持不动。

## 以下为分块材料游标切片交接（历史）

# 当前交接：J13a a2 分块材料游标切片离线闭合、源重验待施工（2026-09-24）

- **最新切片**：`bd972386` 将批量周报单 block 材料克隆下标/结果归 Weekly owner，主线程仍按预算逐项调用并保留原 Upsert。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `01AB3C5A0069C223B3A06104BF45F8947E7B6CE09CA6DFAEC076239EB6867F99` 的 Phase8/游标回放、入口 11/source 7、453 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：先复现活动批量 `MyBehavior.cs:45517–45667,44217–44261` 现有记录被迟到结果覆盖、源材料变化未重验的反例；审阅自动/手动重试的捕获时点和目标身份，再补主线程 owner/目标/源门禁。该缺口未过之前不报 a2 完成。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 commit 队列切片交接（历史）

# 当前交接：J13a a2 批量 commit 队列切片离线闭合、a2 继续（2026-09-24）

- **最新切片**：`e4a78829` 将批量周报 worker→主线程 commit 队列的 FIFO、无工作快路径、队首隔离、读档清理等待者归 Weekly owner；原私有嵌套 DTO、generation 与主线程写入保留。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `CF2279401002FABF2F140289AF834C7806836631F6E198783EFAF5129BDEAD86` 的 Phase8/新队列回放、入口 11/source 7、451 锚点地图通过。**a1 已有限 OFFLINE_VERIFIED，a2/J13a/J13 仍 ACTIVE**；请求发送/重试、分块提交/源重验、失败恢复、a3 和 J13b–g 未完。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：核对 `MyBehavior.cs:42876–42972,45302–45373,45513–45650,45668–45752` 请求与分块提交状态，先将有界分块提交的状态转换移交 Weekly owner，验证大积压、旧代、部分失败；之后继续 a2 其余模式。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 a1 收口交接（历史）

# 当前交接：J13a a1 离线闭合、a2 请求生命周期待施工（2026-09-24）

- **最新门禁**：`577f7cac` 用当前候选生产 owner 做同输入同步/分阶段材料顺序、模式、周界组合回放；结合调度/材料责任切片、schedule smoke、Weekly outcome contract、Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error、Phase8、四 DLL metadata 1060、449 锚点地图，**J13a 的 a1 有限 `OFFLINE_VERIFIED`**。它不代表 a2/a3 或 J13 全包。当前 Debug 1.4 候选 SHA256 `9FE600156D304C3E97B5682AFF1F720A8BFFA8A7DD7713E0A5B039354168349A`；实机/旧档/provider/音频/帧性能 NOT-RUN；初始 O(N) 快照及单组原子成本无帧耗时保证。未 Stage/部署/打包/推送。
- **下一条具体动作**：按[J13 计划](docs/plans/j13-domain-owners-plan.md)的 a2，先核对 `MyBehavior.cs:42400–42800,43000–43700,45500–46900` 自动/手动、多模式、minute burst、批次重试、按需全文、pending commit/发布/失败的真实生命周期和 SaveRuntimeGuard/源重验，再做第一个请求责任切片。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为材料游标切片交接（历史）

# 当前交接：J13a Weekly 材料三阶段游标切片离线闭合、a1 待整体校验（2026-09-24）

- **最新切片**：`cd37d2f2` 将聚合、PromptMaterials、Batch Prompt 三阶段游标与完成状态归 Weekly owner；宿主保留主线程操作和预算检查。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `9FE600156D304C3E97B5682AFF1F720A8BFFA8A7DD7713E0A5B039354168349A` 的 Phase8/游标回放、schedule smoke、Weekly outcome contract、四 DLL metadata 1060、入口 11/source 7、449 锚点地图通过。**a1 仍 VERIFY，J13a/J13 ACTIVE**：同输入同步/延迟整体材料 parity 尚未断言，a2/a3 和 J13b–g 未完成。初始化 O(N) 快照及单组原子工作未证明帧耗时；实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：补同步/延迟同一日期/材料下 eligibility、分组、顺序、全文短报及周界集成回放，并审阅 `MyBehavior.cs:6033–6065` 快照一致性；a1 退出门过后进 a2 请求完成。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 PromptMaterials 组装切片交接（历史）

# 当前交接：J13a Weekly PromptMaterials 组装切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`c2d8565b` 将全文/短报材料组装和劫掠归并归 Weekly owner，自动、同步和独立劫掠构造入口接通；原 host 保留 live 解析与专用素材文字 helper。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `C319221CE7B0D9C523EA305DBFA315B71FE6EABC46D612A86540812B1C348DE1` 的 Phase8/新增材料回放、Weekly outcome contract、入口 11/source 7、447 锚点地图通过。**a1/J13a/J13 仍 ACTIVE**：自动三阶段游标与初始快照预算、a2/a3、J13b–g 未完成。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：处理 `MyBehavior.cs:6244–6315` 的聚合/Prompt/Batch 三阶段推进状态与预算重入，定向验证空组/预算边界；再有限关闭 a1，转入 a2。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为全文/短报选择切片交接（历史）

# 当前交接：J13a Weekly 全文/短报材料选择切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`12f9e3d2` 将邻近前三王国全文、其余短报及空邻近回退规则统一归 Weekly owner；同步预览和延迟自动准备共用，后者缓存选择集合。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `AD1E082F0DC73ADE5E17AD6D0B87BC1A85916309CAA620FF2A8954B196397E92` 的 Phase8/选择规则回放、入口 11/source 7、444 锚点地图通过。**a1/J13a/J13 仍 ACTIVE**：PromptMaterials 具体构造与阶段游标、a2/a3、J13b–g 未完。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：检查 `MyBehavior.cs:37171–37240,6266–6321` 的 Full/Short PromptMaterials 构造、分批阶段和预算游标，转移一段完整材料责任并验证负例，然后推进 a2 请求完成。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 action 游标切片交接（历史）

# 当前交接：J13a Weekly action 材料游标切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`8d934447` 将 recent/major action 游标和当前 Hero 缓存归 Weekly owner，单次至多跨一个 owner 边界或消费一条 action；连续 1000 个失效 owner 的预算反例回放通过。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `E169B058AF9653A908B4814126CE09CB83CBFE6C35B8EE17903A67E845593537` 的 Phase8 回放、入口 11/source 7、442 锚点地图通过。**J13a/J13 仍 ACTIVE**；初始化 O(N) 快照及单组聚合仍未严格预算化，a2/a3 和 J13b–g 未完成。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：核对 `MyBehavior.cs:6039–6151,6244–6321` 一次性 source/owner snapshot 与单组聚合/提示准备预算，确定不改变源状态语义的增量化边界，然后继续 a2 请求完成、a3 回执发布。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为材料聚合切片交接（历史）

# 当前交接：J13a Weekly 材料聚合切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`f54fbcf6` 将预览材料克隆、分类/事件键分桶和重排归 Weekly owner；自动/同步预览共用，live 渲染仍在 host。获准四目录核对后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `5FF0628720BA2D06A27F12C99096AC8D82A087A3134526CACFAF80F5727FF1C9` 的 Phase8、新增聚合回放、入口 11/source 7、440 锚点地图通过。**J13a/J13 仍 ACTIVE**；单组聚合 O(M log M) 未严格预算化，a2/a3、J13b–g 均未完成。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：读取 `MyBehavior.cs:6051–6389` 的一次性材料/action snapshot 与每组聚合/提示准备预算入口，构造单次工作量反例并迁移预算状态转换；之后才推进 a2 请求完成和 a3 回执发布。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为材料分组切片交接（历史）

# 当前交接：J13a Weekly 材料分组切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`008a3f84` 将 Weekly 分组优先级及世界/全文/短报批次划分真实归新 owner；原 host 保留王国资格和邻近状态捕获。当前 Debug 1.4 候选 SHA256 `746E51D79EC3C7F4CBF5DF2114320BA9CCF1BD2281A22007D36078BFCA126655`，Debug/Release 双版本+Bootstrap 六构建 0 warning/error，Phase8 与新增批次身份/顺序回放、四 DLL API metadata 1060、source inventory、438 坐标地图通过。**仍为 J13a_ACTIVE**：预览聚合/游标、请求/完成、回执发布和 J13b–g 未完成；详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。未 Stage/部署/打包/推送；实机、旧档、provider、音频、帧性能仍 NOT-RUN。
- **下一条具体动作**：从 `MyBehavior.cs:1494–1547,6144–6389,38550–38594` 核对预览/聚合游标与每日预算的单次工作量，再设计并接通 Weekly 材料 owner；保留主线程 Hero/Kingdom 捕获、同一 context 游标及同步/延迟生成一致性。不能直接把整个 private 嵌套状态改名或仅拆 partial。其后才进 a2/a3。当前 Git 仅 `.dotnet-cli-home/` 未跟踪，未触碰。

## 以下为调度切片交接（历史）

# 当前交接：J13a Weekly 调度切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`d1697338` 已将自动调度与待处理周次真实归 Weekly owner，Schedule/Text helper 归位；同步/延迟/叛乱恢复真实消费者接通。定向 smoke、Phase8 inventory 11、source inventory 7、435 代码坐标、Debug/Release 双版本+Bootstrap 0 warning/error、当前 Debug 1.4 候选 SHA256 `D45D3320F3A7520FDF72F92220808D43A1BB641D3EC6897F40F1AE05E0F7F218` 的 Phase8/Weekly replay 均通过。**J13a 整包仍 ACTIVE**，材料/批量请求/回执未完，下一步按计划继续；详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。

- **状态**：`J13a1_OFFLINE_VERIFIED / J13a_ACTIVE`，不是 J13a/J13 全包完成。`dd303847` 产品切片，`b240778f` 当前候选 replay 宿主。
- **门禁**：经用户授权且复核四目录后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `FEC1F04BB7F5B6BB4E4D025886A735FA44BE4A87E06331891FA377392F6E02F6`，Phase8 从该候选回放通过，错误 hash 负例拒绝；schedule smoke/outcome contract/source inventory/432 坐标地图通过。构建引用 1.3 `v1.3.15`、1.4 `v1.4.6`，不代表其他补丁版或实机。
- **下一动作**：按 [J13 计划](docs/plans/j13-domain-owners-plan.md)继续 Weekly 调度/材料、生成生命周期、回执发布，再按 Kingdom 等顺序推进。`.dotnet-cli-home/` 未跟踪且未触碰；未 Stage/部署/打包/推送。实机、旧档、provider、音频、帧性能仍 NOT-RUN。详细记录见[主台账当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。

## 以下为首切片验证前状态（历史）

# 当前交接：J13a Weekly 首切片待完整门禁（2026-09-24）

- **状态**：`J13_ACTIVE / J13a_VERIFY`，J07–J12 仍 `OFFLINE_VERIFIED`。开工 `f3b79d4c`，产品切片 `83cc314b`，测试修正 `d12e8d65`；未标 J13a/J13 离线完成。
- **已改**：按需全文完成队列的锁、状态、owner/generation 受理、每 tick 两次提交、异常和清理等待者归 `WeeklyFullReportCompletionOwner`；`MyBehavior` 保留真实 UI/引擎薄入口、源材料重验及 Campaign/存档身份。未动自动/批量周报、玩法、Stage/部署。
- **已验**：相同 Program/生产源码的 net8 Weekly smoke（本机无 net6 targeting pack）、Weekly outcome contract、双 API Compile 集合、source inventory 与 432 锚点地图通过；均不能替代双版本构建/实机。
- **门禁与下一步**：原构建脚本会清理并重建工作区内 `bin/Debug/single_module_artifacts`、`obj/single_module/Debug`、`bin/Release/single_module_artifacts`、`obj/single_module/Release`，已核实位置/链接/内容并请求**仅这四目录**的清理确认；确认前不运行。之后跑 Debug/Release × 1.3/1.4 + Bootstrap、当前候选 replay，再继续 Weekly 自动调度/材料及 a2/a3。详细证据见[主台账 J13a1](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)与[代码范围图](docs/architecture/af-framework-code-scope.md)。`.dotnet-cli-home/` 未跟踪且未触碰；实机、旧档、provider、音频、性能均 NOT-RUN。

## 以下为规划交接（历史）

# 当前交接：J13 计划已就绪，尚未施工（2026-09-24）

- **状态**：J07–J12 保持 `OFFLINE_VERIFIED`；J13 为 `PLANNED`。本轮只有文档，没有新增产品验收结论。
- **新对话入口**：[J13 可执行计划](docs/plans/j13-domain-owners-plan.md)，第 1 节可直接复制作为启动指令；先 G0，再 Weekly → Kingdom → Persona → Social/Issue/WorldEvents/WarStats → 场景领域 → UI/Onboarding → 离线收口。
- **详细证据**：[主台账当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)、[代码范围图](docs/architecture/af-framework-code-scope.md)。J12 产品终点 `5c3e7b0e`，保留责任和验证见 [J12 最终交接](docs/handoffs/2026-09-22-j12-final-closeout.md)。
- **实际定位**：规划基线 `0624d502`；工作树 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`。新对话重新核实 Git；不使用历史 G: 工作树指令，保留未跟踪 `.dotnet-cli-home/`。
- **首包重点**：Weekly 的真实调度/材料/生成完成/回执发布 owner，保留唯一 Actions 提交、主线程捕获/回写、generation 和存档身份。不能只搬文件或拆 partial 就报完成。
- **已知前置**：核实本机 SDK/引用；处理测试旧路径和 Stage DLL 依赖。原构建脚本有产物目录清理，运行前需要精确范围确认；计划没有更改原构建流程。
- **未验证/未授权**：本轮未跑产品测试或构建；真实 Campaign/Mission、旧 SAVE、provider、音频和帧性能仍 NOT-RUN。未 Stage/Deploy/Package/push，未修改自动化、游戏、存档或外部工作树；不提前执行 J14。
# 当前交接：主体 J13e5 Exercise 有限离线收口（2026-09-25）

- **状态**：`J13e5_OFFLINE_VERIFIED / J13_ACTIVE`；仅此包完成离线门禁，J13f/g 与 J14 尚未开始。精确 MapEvent 排除真实战斗、两轮选择/延迟/迟到回调、活动及孤立事件一次 XP 回执分别归 Exercise owners，TaleWorlds PartyScreen/Mission/roster/角色/Hero/MapEvent/Encounter 实际副作用与原 Harmony/Terminal 适配保留。产品/回放 `ba645b29`、`def2b082`、`3d00d416`、`80e21be1`、`8654427a`；详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；最终 Debug 1.4 SHA256 `2F9D0E976A18CA4C28FDA20B4C6C728886EE770AF9769EBC825B5D8BEBFD02E8` 的完整 Phase8 含当前 DLL 身份/会话/结算回放，聚合接线另核对四 DLL 奖励参数。V1 119、四 DLL metadata 1128、PersistenceIdentity 142/36、迁移 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 708 锚点两模式通过。真实两版本 Mission/Harmony、旧档、MCM 实机、伤害/奖励/经济与帧性能 **NOT-RUN**；部分 XP 失败不重试以免双发，异常 TW 清理仍须实机观察。
- **下一步**：按[原计划](docs/plans/j13-domain-owners-plan.md)仅继续 **J13f UI/Overlay/Onboarding**，再 J13g；不提前 J14。`.dotnet-cli-home/` 保留，无 push、Stage、部署、打包、游戏/外仓写入或自动化改动。

## 以下为 e4 交接（历史）

# 主体 J13e4 Settlement/Inspection 有限离线收口（2026-09-25）

- **状态**：`J13e4_OFFLINE_VERIFIED / J13_ACTIVE`。入场 ticket、随行 Mission/Agent 身份和 Inspection 临时 session/清理分别归真实 owner，原 Campaign/Harmony/Terminal、存档与 GCCZ 薄接缝保留；产品/回放 `a946301a`、`cf162dd5`、`35daa7a6`，聚合 `ef6a83e1`。一基源码、保留/未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `877CE07F95D20F7DD82FEFAFA89D873CB3C831E0CC9240A5A5B5B39EAEA2BE39` 完整 Phase8 含三个当前 DLL owner 回放和 e4 聚合接线契约；V1 119/四 DLL metadata 1112、PersistenceIdentity 142/36、迁移 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 680 锚点两模式通过。实机入场/死亡/中止、旧档、GCCZ 副作用及帧性能 **NOT-RUN**。
- **下一步**：依[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e5 Exercise**；不提前 J14。`.dotnet-cli-home/` 保留，无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 e3 交接（历史）

# 当前交接：主体 J13e3 Encounter 有限离线收口（2026-09-25）

- **状态**：`J13e3_OFFLINE_VERIFIED / J13_ACTIVE`。目标/会话优先级、释放授权及 pending 返回四个真实 owner 与原 Campaign/tick/三个会话 patch 连接；产品/回放 `0b0b32c0`、`dc2bbe03`、`86c11889`，聚合 `73bef755`。一基位置、保留 host 和未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：源码链接提取 53/53；Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `8545757A9A8880F1E5D40E74E7F0727D967DD86C6675442E571232293B803ACD` 完整 Phase8 含四个当前 DLL owner 回放和 e3 聚合源码契约；V1 119/四 DLL metadata 1100、PersistenceIdentity 142/36、迁移 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 663 锚点两模式通过。原版事件顺序、实机会面/Harmony、旧档、native safe-passage 与帧性能 **NOT-RUN**。
- **下一步**：按[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e4 Settlement/Inspection**，随后 e5；不提前 J14。`.dotnet-cli-home/` 原未跟踪目录保留，无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 e2 交接（历史）

# 当前交接：主体 J13e2 Taunt 有限离线收口（2026-09-25）

- **状态**：`J13e2_OFFLINE_VERIFIED / J13_ACTIVE`。和平场景/物理 MCM、延迟犯罪与信任小数、冲突升格/结束状态分别由 Taunt 三个真实 owner 承接；原 Campaign/Mission、五个 Harmony patch、保存键与原生处罚/队伍恢复适配保留。产品/行为 `e62e2a82`、`9ec814eb`、`5873d594`，聚合 `bee366a8`；一基坐标、Patch target/条件、覆盖和未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `4C8FDC5AF6BD57AFDBB380B639FB4340792FA08237A0F2515C40EB2434DAE0A9` 的完整 Phase8 含三个当前 DLL owner 回放和 e2 聚合契约；context 22/22、penalty 16/16、lifecycle 14/14、V1 119/四 DLL metadata 1080、PersistenceIdentity 142/36、迁移 fixture 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) recorded/working-tree。实际游戏 Mission/Harmony/旧档/MCM 点击/原生副作用和帧性能均 **NOT-RUN**。
- **下一条具体动作**：按[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e3 Encounter**，先按军团会面目标案例审选中军团成员优先、合法 `_targetHero` 保留、释放授权/超时、pending 回调的换 party/Mission/save 失效和原 patch/注册，再逐切片迁 owner 与行为/聚合契约；随后 e4、e5，不提前 J14。`.dotnet-cli-home/` 保留；无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 e1 交接（历史）

## 远端并行收尾的独有记录（来源 e7936b04；不覆盖当前状态或授权）

# 远端时点交接：J15 离线收口（F4-U 0 HOLD、F4-A 许可由用户声明、F5 离线重验）；F5 实机待部署 1.4.7（2026-09-28）

- **F4-U 完成**：经用户批准，最后 6 项旧 GUI（`af_courier_scroll_version_a`、notoriety `background.png.tmp` / `culture_panel_patch`、`af_scroll_quill`、旧 `af_world_diplomacy_notice`、`af_vassalage_notifications_1` SpriteSheet，共 5,152,346 字节）逐项 SHA 备份到忽略的 `local/j15-gui-archive-20260928/legacy-unused/`（含 manifest）后退出活动树，`e3140bb1`。无消费者依据：运行期 sprite 加载器按精确文件名读 `content/modules` 下的正式版；`af_terminal` 类无引用；vassalage 旧图集在 `e4b1079c` 删掉 Config.xml 类别后不再加载，原版 `SpriteData.LoadFromDepot` 只为 `*SpriteData.xml` 声明的类别加载 `SpriteSheets`（模块现无此类文件）。policy overlay 不再打包旧通知图。runner **HOLD 6 → 0**。
- **F4-A 许可**：用户 2026-09-28 声明 `pack0.tpac`（1,671,304 字节，含 `nacisword1`）为“我们自制/有授权”；以此作为来源依据记录，具体出处文字待补。11.2 MB 历史 manifest 与当前包身份不符的问题保留为历史说明，不影响当前唯一权威源。实机动作视觉归 F5。
- **可移植性修正** `3b4a905c`：六项 Economy 设计源锁的是作者机器的 CRLF 工作区字节（README 连作者当前字节都不符），新 checkout 必失败；改锁已提交 LF blob 并做 CRLF→LF 归一。runner 的 pwsh/dotnet8 路径可用 `AF_J15_PWSH`/`AF_J15_DOTNET8` 覆盖。
- **F5 离线重验（候选 `e3140bb1`）**：六构建 Debug/Release × 1.3（v1.3.15.110062）/1.4（v1.4.7.117484，从 E: 游戏只读复制到忽略的 `.tmp/refs-1.4.7`）+ Bootstrap 全部 0 警告 0 错误；J15 内容 runner 全新根 `artifacts/j15-content/f4u-unused-retirement-20260928-a` **108 mappings / 0 holds PASS**（含内容布局、Prompt 投影、ManagedDeploy 回滚/中断/未知哨兵、PlayerExports 部署、GCCZ loader）；inventory `unknown=0` PASS；Release Stage 124 项；StaticVerifier 对该 Stage + E: 游戏根 **13/0**；编辑器隔离 smoke 见下。工具：pwsh 7.4.6 装到仓外 `G:\AFMOD\.pwsh7\`，dotnet8 用 `G:\AFMOD\.dotnet-sdk\`。
- **F5 实机**：本机只有 1.4.7；1.3 线 LIVE/SAVE 在此机 NOT-RUN。1.4.7 部署待用户确认备份与写入清单。J15 完整完成仍取决于 F5 实机与旧档。未推送。

- **O2 编辑器完整 smoke PASS（离线合成数据）**：远端作者未推送的 `--isolated-full` 改动不可得，已在 `tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/Program.cs` 重新实现：要求合成根预先存在、无 reparse，且 `ANIMUSFORGE_DATA_ROOT`/`TEMP`/`TMP` 全在其内，否则写入前拒绝；覆盖建包、知识规则新建/编辑+备份、人设新建/编辑+备份、原始 JSON 编辑、坏 JSON 保留、从备份恢复、包列表新旧排序、单文件/数据类型/整包软删除、4 类越界写入拒绝、最终加载校验与条件目录，并比对真实 `%LOCALAPPDATA%\AnimusForge\UserData\PlayerExports` 前后不变；不清理，保留证据。旧删除 fixture 改为 `<root>/UserData/PlayerExports` 规范路径（旧写法在当前拒写校验下必然失败）。
- 证据：用户批准的全新根 `E:\AF-J15-editor-smoke-20260927-a\`（运行前确认不存在），`smoke.log` 为 `steps=9 real_root_unchanged=1`，退出 0；所有写入仅在其 `data/` 下。SDK：系统 dotnet 无 SDK，新装官方 10.0.400 到仓外 `G:\AFMOD\.dotnet-sdk10\`；Release 构建 0 警告 0 错误；`--path-contract`、`--path-contract-invalid` 回归 PASS；缺根、TEMP 越界两个反例在写入前拒绝。
- 仍然：只证明独立编辑器冷路径，不是游戏内导入导出或真实玩家库验收；F4-A 来源/再分发权与实机视觉、F4-U 六项 GUI + 三张 atlas HOLD、F5 两版本实机/旧档仍 NOT-RUN；J15 不标完成。未部署、未推送。


# 当前局部修复：复仇刑场气泡接线（2026-10-02）

- 修复非流式主 API 完成全文被刑场丢弃、空流误标已发言导致本地兜底被拒绝，以及刑场等待旧 overlay/AF UI 未就绪虚报成功。默认流式开关、刑罚和死亡结算不变。
- 107 项处决契约、speech parser、AF Debug 1.3/1.4/Bootstrap PASS；独立工程共享源码本地隔离编译 PASS。实机气泡/模型/旧档/性能未验，未部署/push。
- [主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#execution-bubble-repair-20261002)记录源码职责与证据；局部回滚及日志 `artifacts/execution-bubble-repair-20261002/`。保留现有其他作者改动。


# 当前局部修复：复仇气泡审查收尾（2026-10-02）

- 修复长全文被2000字符pending缓冲裁切：分片/全文统一按512字符喂入，保留开场、遗言、阶段及原24条上限。上一轮主索引反向暂存已精确同步，本次通过主索引正常提交。
- 新9项回归先失败后通过，总116契约PASS；审查2290字符复现两模式均15条、遗言/阶段正确；原Debug双API+Bootstrap success。未实机/部署/push，保留其他作者与并行任务改动。
- [主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#execution-bubble-review-fix-20261002)及 `artifacts/execution-bubble-review-fix-20261002/` 保存索引恢复、回归、构建与局部回滚证据。
