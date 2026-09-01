# AnimusForge 全方位重构现状、执行总纲与联合验收任务书

日期：2026-08-31。面向 AF 各领域创作者、集成人员与测试人员。

**目标不是“再写一套聊天 Gateway”，而是覆盖 AF 全部生产责任，完成逻辑拆分、真实领域接线、兼容与存档验证、旧实现收敛和可回滚发布。本文一次写清完整范围与完成条件，不代表这些重构已经一次性做完。**

本文由当前 Git、公共执行台账、owner matrix、领域 Gateway 边界和最近本机 handoff 交叉核对形成。本轮只读核对与编写文档，没有重新执行构建/测试、修改生产代码或部署游戏。下述测试 PASS 是上轮已记录的证据。

> **2026-09-01 本机接续更新：**本文主体保留 2026-08-31 的全量任务基线；实时状态以公共台账和最新 handoff 为准。`LOCAL-7-C` 已修复四 runner 的显式依赖边界；`LOCAL-7-D/E/F/G/H/I` 已依次完成 Memory owner readback、Courier Economy reservation、known partial、structured `UnknownAfterStart`、memory-only durable recovery 与 Courier inbound durable completion 的代码/离线验证。最新源码提交为 `de3220b7`，阶段 7 仍 VERIFY；下一切片审计 weekly/notoriety 等 H 未覆盖的 memory 辅助副作用。真实 Campaign/Mission、live Economy/AFEF、旧档和默认切换仍未验收。

## 一、现状

- 当前主阶段：**阶段 7，领域模块接入与验证；阶段 8 的 Bridge、旧结构清理与最终验收仍未完成。**
- 阶段 4/5/6 仍有生产接入和真实运行验收事项；阶段 2/3 的 DONE 主要是设计完成，不能理解为完整 Foundation/模块注册体系已经上线。
- “约 60%”只保留为历史粗估，没有正式加权验收口径，**不是精确完成率，更不是发布完成率**。
- 团队远端分支：`refactor/prepare-af-restructure`。
- 本轮重新 fetch 确认的远端提交：`182da1db4db4199cf65783f911f3cb6d46b18970`。
- `a096c1b1` 是历史比较点，不是最新提交，也不是经真实游戏验收的发布回滚点。
- 本机工作树：`G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 本轮编写总纲前，本机 HEAD 为 `d8c81b5e`，领先远端 4 个本地提交、无未提交改动；其中生产源码修复为 `b24fdf4b`。**这些本地提交尚未推送，其他创作者拉取远端不会自动拿到。** 本轮总纲与台账另作本地文档提交。
- 当前仍不是可发布版本：真实 Campaign/Mission、旧存档、核心经济副作用和默认三渠道切换没有完成联合验收。
- 原 NEW-10 与 GCCZ 保留各自用途；不把整个 AF 主体复制进 GCCZ，不把旧融合树覆盖到新重构树。

## 二、已经完成什么

### 1. 契约与共享交互边界

- 已建立 Native / SceneShout / Courier 共用的 detached snapshot、facade、coordinator、Gateway、Prompt、后处理、ActionPlan 与 Memory commit 边界。
- 后台持有不可变 ID/值快照，结果通过渠道主线程提交端口复核和执行。
- 三渠道已有 opt-in/可控 Host 入口和受控回放；**默认入口还没有全部切换**。
- 部分旧入口底层 transport 已实际接入共享 Gateway，所以也不能说“重构代码完全不影响现有运行路径”。

### 2. Gateway 与领域适配

- Configured Chat、SSE、连接验证、模型目录、Policy/WorldDiplomacy、Knowledge/RAG、TTS 与辅助分类器等已有共享协议或 transport adapter。
- 原领域继续负责 JSON 结构、规则资格、预算、缓存、重试策略、数据写入和失败降级；不是把所有 LLM 业务压成一个通用聊天函数。
- Prompt/后处理标签/ActionPlan/已确认事实有明确边界；LLM 的承诺不直接成为游戏事实。

### 3. Economy / Reward / Debt

- Hero、Party、Merchant owner factory、主线程 replay port 和对应协议接线已有实现。
- 三渠道 detached ActionPlan 已具备 Economy-aware commit 路由，保留非经济动作的原领域执行权。
- owner/state fixture、协议与生产 DLL 回放已有通过记录。
- **这证明接线与可控边界，不证明实际金币、库存、市场、债务和存档副作用已经正确。**

### 4. 本机已经修复的提交安全缺口

- `b24fdf4b` 修复了单次 `ExecuteAsync` 内“动作开始执行后，记忆/回调/dispatcher 失败又回旧链路”的重复副作用风险。
- callback 只能消费一次，重复/迟到 callback 被关闭；排队期间取消不会继续提交；已观察到的执行 receipt 保留。
- 仍保留提交开始前的合法 fallback。
- **不承诺跨新请求、重建 facade、读档后的 exactly-once，也未完成部分经济事务恢复与记忆补写方案。**

### 5. 已有验证与交接资料

- 本机 Debug 1.3、1.4、Bootstrap unified Stage：各 0 warning / 0 error。
- InteractionPipeline 原 40 cases，加新提交边界 48 cases，通过。
- 生产 1.4 三渠道新增 12 个提交后故障场景通过，未重复调用 fallback。
- Economy owner/commit、Detached/Courier Host、主要配置/模型/辅助 Gateway 和多项 Python 契约审计已有 PASS。
- Persistence/Profile/Config：95 个 key、121 条 typed binding、8 类类型；身份审计 sync=99、behavior=35。两处 fixture 导航行号已校正，未改生产 key/type，也未放宽检查。
- owner matrix、公共执行台账和本机 handoff 已记录边界、命令、日志、构建哈希、回滚和未验证项。

## 三、尚未完成什么

### 真实运行与发布硬缺口

- 真正初始化的 Bannerlord Campaign/Mission 中的调用、主线程执行和退出/读档生命周期验证。
- Hero → Party → Merchant 的实际金币、普通/RP 物品、市场库存、债务创建/解除、履约记录及结果事实验证。
- 三渠道真实历史读写、user/assistant 顺序、AFEF 成功事实及失败不伪造事实。
- 代表性旧存档实际加载、执行后存读档、未知数据保留、缺失/损坏数据隔离和幂等恢复。
- Duel、WorldMap、Policy/Diplomacy、Scene、Siege/GCCZ/SETS、Courier、周报/主动 NPC、Issue、社交成长、Knowledge、UI/TTS 等全部领域的迁移确认与组合回归。
- 剩余巨型类的责任拆分和旧路径收敛；仅拆成 partial 文件不算完成解耦。
- 可回滚的默认三渠道切换、Release/打包/安装验证和最终发布签收。

### 已知工具与环境缺口

- 4 个 runner 已尝试执行，但依赖加载失败、断言未完成，不能记为 PASS：
  - `PolicyGatewayReplayTests`、`WorldDiplomacyGatewayReplayTests` 缺 `MCMv5, Version=5.12.3.0`。
  - `TtsGatewayReplayTests`、`ProductionOptInEntryReplayTests` 缺 `TaleWorlds.CampaignSystem`。
  - 四者 `.csproj` 的依赖复制仍硬编码另一台机器的 F 盘路径。
- `.NET 10` PromptLab/PlayerExportsEditor 尚未运行；本机上轮确认只有 SDK `8.0.422`。不得降 TargetFramework 来凑通过。
- XihaiAction 独立 net472 runtime、本机 Release/ZIP/package、真实外部 provider 未在上轮执行。别台机器的 Developer Pack 或部署结果不能移作本机证据。
- 重构层仍有本地化欠账：例如模型目录 Gateway 的中文硬编码诊断，需要稳定错误码与英文/简体中文资源映射。
- 参考资料、第三方 provenance/许可证和分发边界仍需确认；原阶段 1 清理 HOLD 不能被“全面重构”自动解除。

## 四、“全方位重构”的完整覆盖范围

以下是**全量责任清单和目标验收要求**，不是宣称每项都有缺陷或已经迁移完成。保留现有清晰边界，由各 owner 核实真实调用路径后逐个交付。

| 领域 | 必须覆盖的责任 | 最低验收要求 |
| --- | --- | --- |
| 1. Bootstrap / Build | 版本识别、resolver、单实现选择、生命周期转发、统一模块结构 | 1.3/1.4 各自正确启动，只加载一个实现；歧义失败可诊断，资源路径正确 |
| 2. Host / Composition | Behavior、Model、Harmony、Mission、UI、Tick 的组合与注册 | 注册顺序和原行为保持，重复注册被阻止；局部模块失败不拖垮其他模块 |
| 3. Runtime / Diagnostics | 主线程调度、取消、generation、后台任务、队列、缓存、日志 | 过期任务不提交；队列/缓存有界；退出清理；无无限轮询或日志敏感信息泄露 |
| 4. GameAdapter / Compatibility | TaleWorlds API、Harmony、反射、Gauntlet、版本差异 | 双 API 构建和实际关键场景通过；AF 关闭/资格不符时不污染原版行为 |
| 5. Persistence / Config | 全 owner 的 key/type/chunk、配置快照、迁移、导入与数据来源优先级 | 旧档/新档往返，缺字段/坏 chunk/未知数据隔离；跨 key 一致性；reload 不改变已开始请求 |
| 6. Conversation / Encounter | 三渠道 session、明确目标、资格、会面入口和历史结构 | 同类机制同规则；军团所选成员不被军团长覆盖；会话关闭后旧结果无效 |
| 7. Gateway / Prompt / Protocol | 前处理、主回复、后处理、SSE、模型获取、辅助 API、规则资源 | 空回复、超时、取消、重试、截断、流式结束可重复验证；标签不进入玩家可见正文 |
| 8. Action / Commit | 解析、授权、当前目标复核、领域执行、receipt 与失败恢复 | raw/plan 一致；未知/无资格动作拒绝；执行中失败不重放；跨请求去重另有证据 |
| 9. Memory / AFEF | 最近/每日/压缩/总览记忆、重大事件、已确认事实 | user/assistant 语义一致，三渠道可读同一记忆；实际发生才写事实；读档后旧任务失效 |
| 10. Economy / Reward / Debt | Hero/Party/Merchant 资产、奖励、RP、信任、生成物品、债务任务 | 双方数量准确；不足/目标失效不误转移；市场与债务对象准确；履约/期限/备注和存读档正确 |
| 11. Policy / Political | policy record/effect/registry、NPC policy、目标路由、投票交易 | 领域 JSON/repair/失败语义保留；权限/管辖与目标冻结；延迟效果一次执行；跨 key 恢复 |
| 12. World Simulation / WorldMap | 外交、战争和平、割让、附庸贡金、兼并、世界队伍命令 | 稳定 Hero/Clan/Kingdom/Party/Settlement 身份；对象销毁/取消/军团变化与读档边界正确 |
| 13. Settlement / Siege / GCCZ / SETS | 村城堡后果、人口文化、城镇记忆、选兵、建设/领主/囚犯桥 | active-stage 守卫、六类角色、MCM 限额、语义标签、完整结算/殖民幂等；普通场景零污染 |
| 14. Scene / Mission / Combat | 环境对话、带路/传唤/跟随、挑衅、训练、检查、护送/处决、会面锁定 | Agent/LocationCharacter 目标准确；Mission start/end 清理；和平/原版战斗/竞技/训练/攻城正确隔离 |
| 15. Duel | 资格、stakes、配置、Mission、死亡/结果、可选 Fourberie 兼容 | 正常/拒绝/取消/失败/死亡/退出都验证；原版战斗不误触；赌注、结果和事实一致 |
| 16. Courier / Proactive / Issue | 信件与 Party 生命周期、送达/返回动作、主动 NPC/同伴、原版任务桥 | 存读档恢复；送达/返回动作不重复；Opening/Pending 转换正确；只在真实任务完成后写事实 |
| 17. Social / Progression / Reports | 声望关系、恋爱婚姻生育、贵族聚会、唯一物品、周报/世界事件时间线 | 冷却和一次性迁移正确；状态与事实一致；频率/缓存/single-flight、未读状态、失败重试有界 |
| 18. Knowledge / Persona / Profile | RAG/ONNX、知识 codec/index、Persona、同伴技能、王国 profile | 模型资源完整；索引重建/取消/fallback；文件、配置、存档与缓存权威不冲突 |
| 19. UI / TTS / External Integration | 对话 overlay、百科、MCM/onboarding、Terminal/弹窗、语音/口型、XihaiAction | 主线程和焦点/关闭时序；Backspace 不串原版页；取消播放；可选依赖缺失可诊断降级；双语资源 |
| 20. Tools / Content / Package | PromptLab、Editor、契约工具、ModuleData/XML/Prompt、PlayerExports、ONNX、Stage/ZIP | SDK/依赖闭包可重现；工具与客户端边界清楚；资源实际加载；统一模块包、数据保护与回滚演练 |

额外提醒：非聊天 LLM 也在范围内，包括周报、记忆压缩、Persona/技能、政策 repair、叛乱命名、RAG 短句、错误分析、辅助分类器和手动连接验证，不能只核对聊天按钮。

## 五、架构底线与清理规则

1. **先保持单一玩法 DLL 的逻辑模块化。** 仍发布一个 `Modules\AnimusForge`；Bootstrap 只加载一个版本化 `AnimusForge.dll`，不借本次任务强拆多个玩法 DLL或承诺运行时热卸载。
2. Foundation 只负责跨域基础能力与宿主安全；经济、政策、攻城、关系等玩法仍归各自 owner，不把基础层变成新的巨型业务类。
3. GameAdapter 统一管理版本/主线程/反射/Harmony 机制；不要每个模块再造兼容探测和私有字段访问路径。
4. 跨域行为先确定双方公共接口与共同 owner，确属共同玩法才建立 Bridge；没有 owner 不抢先实现。
5. `SubModule`、`MyBehavior`、`ShoutBehavior`、`CourierDeliveryBehavior` 按调用链逐片削薄，不整文件重写、不机械拆 partial 充当解耦、不另造平行 Gateway/状态机。
6. 保持存档程序集、序列化类型与既有 key/type；确需迁移，先设计并验证旧数据恢复和失败保留，不靠改默认 JSON 假装迁移完成。
7. GCCZ 规则保留在独立 `AnimusForge.SiegeAftermathIntervention`；AF 只作显式 active-stage 门控、数据转换和真实副作用桥。可复用规则/桥更新必须在 GCCZ 与当时已确认的融合树两侧表示，不编辑错误历史副本。
8. 不借重构改 GCCZ 正负结算条件、奖励、惩罚、阈值、搜掠、血洗或殖民平衡。标签由 AI 语义选择，代码负责解析/授权/拒绝，不用玩家关键词偷猜意图。
9. 新代码标识符、注释、日志和内部键用英文；玩家文本集中到可加载的中英文资源。诊断使用稳定错误码，不把凭据或完整对话放入日志/存档。
10. 删除被替代的死代码、孤立 helper、重复实现、废旗标和注释残骸；但有调用、反射入口或存档兼容责任的 Legacy facade 必须保留，直到替代路径有证据。
11. 保留用户数据、ONNX、参考源码、AD1259 编年史、ZIP/交接备份和第三方资料；源码/内容/测试/工具/参考/依赖/产物分层不等于授权删除或取消跟踪。
12. 不擅改官方一键编译/覆盖流程，不推 main、不 force push、不 hard reset、不自动覆盖游戏；跨工作区改动、发布与部署分别按授权执行。

## 六、完整接续顺序

### P0：关闭本机可重复性缺口

1. 重新 fetch，确认团队分支、参与者工作树和 owner 认领；不要覆盖他人的本地改动。
2. 明确远端 `182da1db` 与本机 `b24fdf4b` 等未推送差异；需要共享时先审查，再按明确授权同步到约定协作分支。
3. 完成 `LOCAL-7-C`：修复四个测试 runner 的 F 盘依赖复制设计，显式指定本机/固定版本引用并检查完整依赖闭包；不把 TaleWorlds/MCM 打入客户端包。
4. 补 SDK 10 或记录清晰 NOT-RUN；安装环境需独立确认，不能降框架、跳断言、吞失败。
5. 固化核心 contract/replay、双 API/Bootstrap、资源与存档身份检查的可重现命令、工具版本和日志目录。

### P1：收紧交互、动作和存档的共同边界

1. 保留本轮单请求提交保护，继续验证跨新请求、同文不同轮、重复 completion、facade 重建、读档 generation 的请求身份与 receipt 语义。
2. 明确部分经济动作已经执行而 memory/后续动作失败时的结果、恢复与人工处理路径；不能宣称未执行，也不能再次跑整套 fallback。
3. 逐 owner 确认存档数据权威、key/type/chunk、持久与瞬态状态，覆盖 unknown/missing/corrupt、旧档和执行后重载。
4. 收紧 Host 注册/调度/退出清理与 GameAdapter 版本边界；以小型闭环迁移，不先建最终程序集大图。

### P2：按完整领域清单交付生产纵切片

1. 优先跑通一条 Hero 经济动作的完整输入 → 资格 → Prompt → 后处理 → ActionPlan → 主线程变更 → AFEF/历史 → 重载链路。
2. 再扩 Party、Merchant、Debt 和三渠道组合；每轮只改变一类副作用，保留明确重置点。
3. 接续 Policy/Courier/Duel/WorldMap/Scene/Diplomacy/Siege，并覆盖表中其他领域；任务排序允许按依赖调整，不能遗漏非 LLM 功能。
4. 不同 owner 可并行；共享契约、巨型 host 文件和桥入口必须先冻结/认领，避免多个创作者同时整文件重写。

### P3：真实游戏、旧档与 Bridge 联合验收

1. 使用独立测试存档、明确版本/模块组合、重置步骤和前后状态记录，不在唯一正式存档上试验副作用。
2. 覆盖 Campaign、Mission、Encounter、Gauntlet、原版战斗及普通场景隔离，不能用进程存在或主菜单截图代替。
3. Bridge 至少验证 A、B、A+B 无桥、A+B+桥、缺依赖/不兼容、桥失败/禁用；各自模块继续可用，已保存数据保留。
4. 若没有真实 Host，继续可独立验证的领域接线与测试，但保持 LIVE/SAVE 验收未完成，不转为发布状态。

### P4：默认切换、清理与发布

1. 各必选领域和实际支持组合通过后，单独设计/评审可回滚的默认三渠道切换；旧 facade 保留到调用点与兼容证据足够。
2. 清理只服务于旧路径的实现、配置和文档；不能仅因类名带 Legacy 就删除。
3. 完成 Release 1.3/1.4/Bootstrap、Stage/ZIP、资源/依赖 allowlist、实现标记与哈希、真实安装/启动/存读档回归。
4. 明确发布负责人、目标分支/版本、备份与回退步骤；经授权后才推送、部署或发布。

## 七、其他创作者现在应该做什么

### 领域 owner

每个 owner 认领完整清单中的范围，并交付：

1. 当前真实入口、调用方、旧实现与新接口的对应关系。
2. 负责的状态/资源/LLM provider、Prompt/Action、save key/type，以及明确不负责的边界。
3. 线程、生命周期、取消、失败、缓存、任务频率与每次工作预算；热路径对比证据。
4. 小型可审阅源码提交，连同废代码清理；不要只交一个 DLL 或只有功能截图。
5. 契约/回放/构建的命令、结果、日志与明确 NOT-RUN。
6. 真实 Campaign/Mission/旧档适用场景的步骤、期望、前后状态与观察证据。
7. 三渠道与 optional dependency 的适用/排除说明，Bridge 的共同评审记录。
8. 回滚提交、数据影响、剩余阻塞和下一任务；同步公共台账与 owner matrix。

### 集成人员

- 维护一个真实状态台账，审查接口与资源/数据 owner，避免出现数份互相矛盾的“最新总表”。
- 合并前核对作者工作、依赖、测试与回滚点；不靠整文件覆盖或强推消除冲突。
- 把静态通过、局部回放、实机通过和发布通过分别记录。
- 发现领域规则被塞回基础层、未经验证删除旧入口、吞异常或假兼容层时，退回该切片处理。

### 游戏测试人员

- 先准备 Hero、Party、Merchant 与旧档/AFEF 的可重复测试；每个用例只验证一类变化。
- 记录游戏 BuildInfo、AF 源码/产物哈希、模块组合、存档/场景、输入与操作顺序。
- 同时做失败测试：不足、目标失效、取消、超时、退出、部分成功、重复回调与执行后重载。
- 原版场景、不适用的渠道和关闭功能路径同样必须验证，防止“新机制能用，普通玩法被污染”。

## 八、统一验收口径

每个切片分别记录以下状态，不混用一个笼统的“完成”：

| 层次 | 什么才算通过 |
| --- | --- |
| DESIGNED | owner、责任、接口、数据与失败语义明确；不代表已接生产调用 |
| MIGRATED | 实际生产调用已走目标边界，旧路径去向清楚；不代表实机无误 |
| LOCAL-PASS | 适用的契约、回放、构建与资源检查通过，具体范围/命令可重现 |
| GAME-PASS / SAVE-PASS | 真实 Host 与代表性存档有可重复步骤和观察证据 |
| RELEASE-READY | 必选领域/支持组合、默认切换、完整包和回滚门槛均通过 |

- WAIT/VERIFY/BLOCKED/HOLD/NOT-RUN 不算 DONE；不是所有领域都必须有网络测试，但不适用项必须写明理由，不能直接删掉。
- 进度按已验收里程碑报告；若将来要统计百分比，先共同确认范围与权重，不能把领域数量简单平均。
- 构建成功不代表原版兼容；mock/反射回放不代表 Campaign/Mission；fixture 不代表真实旧档；readiness 不代表部署一致。
- 目前 readiness 的 `installedMatchesStage` 只比较 Bootstrap，测试/发布前必须另核对版本实现与资源。

### 全局验收矩阵

- 版本：1.3 / 1.4；记录准确 BuildInfo，区分固定引用与实际运行游戏。
- 渠道：Native / SceneShout / Courier；不适用能力显式排除。
- 主体：Hero / Party / Merchant / 适用的非 Hero；目标变更与销毁。
- 场景：世界地图、会面、和平场景、决斗、训练、竞技场、原版战斗、攻城及独立 GCCZ 阶段。
- 时序：正常、取消、超时、退出、读档、配置变化、重复结果、部分副作用。
- 数据：新档、代表旧档、缺失/损坏单条、未知数据、执行后重载。
- 组合：必选/可选依赖、模块禁用、Bridge 缺失/失败、支持的降级配置。
- 性能：主线程耗时、队列/缓存容量、扫描频率、请求数、日志/存档增长；每项给出实测基线和 owner 预算，不用“应该很快”代替。

## 九、什么时候才能说“完全重构完成”

必须同时满足：

- [ ] 完整领域清单均有 owner 和明确完成/不适用证据，没有遗落的后台任务、非聊天 LLM 或工具入口。
- [ ] 每个目标边界已接生产调用，旧调用仍需保留的理由明确；不存在新旧双跑或无归属业务逻辑。
- [ ] 共享契约、三渠道、核心副作用、取消/部分失败/去重、存档身份和数据恢复全部通过适用验证。
- [ ] 必选领域与支持组合的真实 Campaign/Mission/旧档证据齐全，GCCZ/原版场景隔离通过。
- [ ] 默认切换独立验收且可回滚；没有未解释的行为或存档兼容变化。
- [ ] 安全清理完成；活跃兼容入口不误删；第三方资源与发布内容有明确处置结论。
- [ ] Release 双实现、Bootstrap、资源、Stage/ZIP、安装与回滚验证通过。
- [ ] 文档、owner matrix、真实执行台账、日志、版本与哈希一致；剩余风险已按发布规则处理。

**在这些条件满足前，只能说“某个切片完成了设计/接入/离线验证”，不能说“AF 全方位重构完成”。**

## 十、当前基线与回滚点

```text
历史比较点：a096c1b1
当前团队远端基线：182da1db
本机编码前 checkpoint：8020112e
本机 fixture 校正：7acc0c78
本机提交边界修复：b24fdf4b
本机前轮交接收尾：d8c81b5e
```

这些不是已经过游戏验收的发布版本。需要撤销本机提交边界修复时，评估后用 `git revert b24fdf4b` 创建小型反向提交；不执行 hard reset、不改写团队历史。**源码回滚不会自动撤销已经写进游戏存档的金币、物品、债务或领地副作用**，因此测试存档和明确重置点不可省。

本机构建引用：1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`；上轮读取的实际游戏为 `v1.4.7.117484`。不要把其他机器的 F 盘路径、游戏版本、Release 或部署记录当成本机事实。

## 十一、资料与下一精确任务

- 实时执行状态：`G:\AFMOD\AF-REFACTOR\docs\animusforge-refactoring-and-repository-reorganization-plan.md`
- owner 导航：`G:\AFMOD\AF-REFACTOR\docs\animusforge-owner-matrix.md`
- 架构/入口导航：`G:\AFMOD\AF-REFACTOR\docs\animusforge-refactor-map.md`
- 已有领域 transport 边界：`G:\AFMOD\AF-REFACTOR\docs\animusforge-phase7-domain-gateway-boundary.md`
- 本机验证、精确命令与产物哈希：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-08-31-local-refactor-commit-boundary.md`
- 原始接续说明：`G:\AFMOD\NEW-10\docs\handoffs\af-main-refactor-continuation-20260831.md`
- 上轮本机日志：`G:\AFMOD\.build-cache\af-refactor-20260831`

owner/refactor map 的第一版基于较早基线，适合导航，不可照搬其旧状态；实际状态以最新 Git、运行证据和公共台账为准。本文件是总纲，不新建一套平行执行台账。

**下一精确任务：先做 `LOCAL-7-C`，关闭四个 runner 的本机依赖问题；并行认领 20 领域责任清单。随后推进请求身份/receipt/部分提交恢复和 Hero → Party → Merchant 的真实 Host 纵切片，最后才做默认切换与发布。**
