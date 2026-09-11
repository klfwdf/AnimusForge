# AF 总 HANDOFF — 主体框架已暂停／待转手（2026-09-12）

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
