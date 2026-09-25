# J14 三渠道公共 API 收尾实施计划

> 规划日期：2026-09-25。本文为执行计划；当前 `J14a_OFFLINE_VERIFIED / J14_ACTIVE`，实际进度以[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md)为准。
> 规划源码基线：`2d9df159282334c02854b38663499ac4a5e9af71`；J13 最终产品为 `39cf9d4724cc372a33503da270fd0c0e9dc6e6af`。
> 用户最新要求：将本计划保存成文件，新建任务，以 `gpt-6-sol` / `xhigh` 开始执行。不是继续只读规划。
> 本文细化主台账 J14a–c。详细进度仍只记录在[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md)，[HANDOFF](../../HANDOFF.md)保留简短当前入口；本文不建立竞争台账。

## 1. 新任务启动指令

```text
执行当前仓库 docs/plans/j14-public-api-plan.md，使用 gpt-6-sol / xhigh。
先核实实际 Git 根、分支、HEAD、dirty，再读 AGENTS.md、HANDOFF.md 当前段、维护/框架两个仓库 Skill、主台账 J13g 最终收口及 J14 定义。不要按历史盘符切换副本。
按 G0 → J14a Scene → J14b Courier → J14c 三渠道契约与最终离线收口执行。J13 已完成有限离线收口，不重开无关责任包。
先记录开工意图并本地提交，再完成第一个真实责任切片和验证。复用现有外部消费者测试，不先批量迁文件，不提前把能力改成 Available，不做第二套缩水 LLM/动作/记忆链。
每个验证切片本地独立提交，只暂存自己的具名文件。包结束或中断时更新同一主台账、代码地图和简短 HANDOFF，并留下下一条具体动作。
普通实现细节自行处理；设计冲突、范围扩大和新的风险授权才请求用户决策。不要把每个机械步骤交回用户，也不要自行创建其他任务或启动子代理。
不 push、Stage、部署、打包、写游戏/外仓/存档、修改自动化、安装全局工具或开始 J15。保留 .dotnet-cli-home/ 和其他作者改动。
原构建脚本存在递归清理。执行前核实精确绝对目标、内容及重解析点并取得本轮清理范围授权；不继承历史清理授权，不绕过引用或安全门禁。
不能用字符串、任务返回或单一布尔判定效果成功。必要离线门禁未完成不能报 OFFLINE_VERIFIED；实机、旧档、provider、音频和帧性能未运行就记 NOT-RUN。
```

## 2. 基线、范围与完成定义

### 2.1 已核实的规划基线

- 工作区：`E:/AnimusForge-refactor-continuation-20260831`；分支：`codex/af-main-refactor-continuation-20260831`。这是定位记录，不覆盖执行时真实 Git 状态。
- 基线 HEAD 如页首；已跟踪文件没有未提交改动，已有未跟踪 `.dotnet-cli-home/` 必须保持。
- J13 状态以根 HANDOFF 当前段和主台账 `主体 J13g 最终离线收口（2026-09-25）` 为准。主台账前面的历史 `ACTIVE` 段不覆盖最终收口。
- 规划者只读运行代码地图 recorded-revision / working-tree 两模式，722 锚点均通过，sourceRevision 为 `39cf9d47`。
- 规划者读取现存 Debug/Release × 1.3/1.4 四实现 DLL 的 SHA256，与 J13g 台账一致；没有重新构建、跑行为回归、访问 provider 或启动游戏。旧证据不替代 J14 新候选验证。

### 2.2 获准目标

1. `SceneSubmit`：真实场景群组对话的公共提交、结果和开始前取消。
2. `CourierSubmit`：真实信使运输链的公共提交、阶段结果和开始前取消。
3. V1 契约收尾：保持 Native 承诺，完成三渠道隔离、生命周期、兼容和独立消费者验证。

不包含任意动作执行、任意记忆写入、扩展/provider 注册、新玩法、默认入口切换、制作组业务重写或 J15–J17。`ActionExecute / MemoryWrite / ExtensionRegister` 保持 `NotSupported`。不新增第二个 AF 实现 DLL 或公共 SDK 分发 DLL。

只有三渠道真实公共链、关键正反行为、必要构建/兼容证据及交接全部满足，才标 `J14_OFFLINE_VERIFIED`。这不是实际子 MOD 游戏加载、LIVE/SAVE 或发布 READY。

## 3. 源码入口与当前缺口

以下是一基定位，均基于页首源码基线；执行时按符号重定位。生产 owner 与实际消费者证据在主台账/代码地图维护，不按整文件宣称完成。

| 范围 | 已读入口 | J14 责任 |
| --- | --- | --- |
| 能力声明 | `src/modules/AF.Module.PublicApi/V1/AfApi.cs:16–28` | Native 已 Available，Scene/Courier 仍 NotSupported；完整链路验收后才开放 |
| 公共票据/结果 | `src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs:8–63` | 保留现有 public 类型、Native 签名、枚举值及取消/结果承诺 |
| 纯契约与投影 | `src/AF.Contracts/PublicApi/V1/AfApiContracts.cs`；`src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs` | 新 DTO 不暴露游戏对象或 internal 类型；继续显式枚举映射 |
| 请求命名空间 | `Refactor/Modules/CoreDialogueClient.cs:10–80` | 当前仅按 Native 文本比较；需加入渠道与上下文身份，仍有界去重 |
| claim/取消/回执 | `Refactor/Modules/CoreDialogueOperation.cs:10–100` | 真实 owner completion 优先，未开始取消与开始后不确定效果分离 |
| 内部服务 | `Refactor/Modules/CoreDialogueServices.cs` | 复用真实渠道 owner，不将制作组 internal ports 公开 |
| Native 范例 | `ShoutBehavior.ModuleNativeSubmission.cs:9–59` | 原主线程队列、owner/generation/epoch/revision、原准入与回执 |
| Scene 输入 | `ShoutBehavior.cs:26128–26405`；`src/modules/AF.Module.Conversation/Channels/Scene/ScenePlayerShoutRequestOwner.cs` | `ProcessCapturedScenePlayerShoutAsync` 只等待输入派发；`ProcessCurrentScenePlayerShout` 内仍 `_ = Task.Run(...)`，不能视为群组完成 |
| Scene 群组 | `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs:357,770` | 真实默认 per-Hero 群组、relay、相关旁听、早退/异常和终态 |
| Scene 后处理/队列 | `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs`、`SceneSpeechQueueOwner.cs`、`ScenePendingAfefFactsOwner.cs` | 复用已有局部等待与唯一提交点；局部完成不自动等于全组终态 |
| Courier 准备/派出 | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionCreation.cs:173–232,549–630` | 原资格、PendingCourierFlow、CreateCourierSession；派队和转移成员已是副作用 |
| Courier 到达 | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs:41–89` | 预生成与 DeliveryApplied 分离；到达才进入原提交 |
| Courier 动作/回执 | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs`、`CourierDeliveryBehavior.DomainCommit.cs:41,259–276` | 保留原单次提交、合法性、失败与事实语义；不能只看 consumed 布尔 |
| Courier 返回/销毁 | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DeliveryLifetime.cs:155–176,526–556` | 回信入库与通知、成员/物资归还、失踪/损失；Stage=Completed 也可能是未送达返回 |

另读已有 [V1 指南](../architecture/af-public-api-guide-v1.md)、[J10 计划](j10-scene-courier-plan.md)、[三渠道对齐](../free_conversation_scene_shout_alignment.md)、[指令标签案例](../directive_tag_output_case.md)与 [1.3/1.4 兼容差异](../bannerlord_1_3_to_1_4_5_compatibility_diff.md)。触及场景移动/伤害等旁接责任时，再读对应案例；不因为引用一个动作 owner 就重写其玩法。

## 4. 已确定的设计边界

### 4.1 输入：当前合法上下文与不透明票据

- Scene 使用 AF 当前有效框选/主对象上下文；Courier 使用原流程已经完成收件人、信使成员、模式和附件选择的待发送草稿。
- 无合法上下文时明确拒绝，不自动选 NPC、不默认派兵、不创建虚拟信使。无 UI 的任意目标选择/自动编组/任意附件构造不在本次范围；若需要，另行扩输入能力。
- 增加由 AF 主线程签发、绑定 client 的不透明上下文票据，再允许从任意线程提交文本。方法/DTO 名称由执行者按现有代码惯例确定，上述语义不变。
- 公共 DTO 不暴露 Hero、Agent、Mission、TroopRoster、内部执行器或委托；调用者不得传权威 extraFact、AFEF 或 action plan。
- 上下文绑定 owner、generation、场景或草稿身份、修订及目标。玩家改选、草稿改变或 UI 先确认，旧票据失效；新请求显式重新捕获，不静默重定向。
- 捕获不得发送 LLM、写历史、派队或抢占 UI。Scene 现有 Capture 会增长 input sequence，不能直接公开为无副作用查询。查询/签发与真正 claim 分离。
- 票据签发/失效也要有界；不得靠不断创建未提交上下文绕过请求容量或永久保留 Agent。公共结果只保存 detached 数据。

### 4.2 幂等、并发与资源归属

- 同一 client + ID + 渠道 + 上下文身份 + 完全相同输入，返回原内部 operation，不重发网络或执行动作。
- 同一 ID 的渠道、上下文或输入不同，返回 `dialogue.request_id_conflict`；Native 原有相同 ID/文本重试语义保持。
- 三渠道共用现有每 client 128 个有效请求 ID 上限，不静默淘汰终态 ID，不按渠道扩大容量；文本/ID 的已有上限与大小写语义保持。
- 不跨 client、读档或进程承诺去重。能力查询不初始化 AF；Available 只表示契约可用，不表示本次游戏上下文合法。
- UI 与 API 必须竞争同一业务准入/草稿 claim，不能创建两条相互绕开的 busy 状态或派出入口。
- 公共 continuation 不在 owner/registry 锁内运行；沿用异步 continuation。旧请求不能清理新请求槽位。
- Dispose 禁止新提交并取消尚未开始请求；已开始请求仍能结算、已返回 operation 仍可读取。不因为 client 释放就撤销已经发生的游戏效果。

### 4.3 取消与失败

- Queued：真实主线程未 claim，可取消且没有本请求副作用。
- claim 后：TooLate，不承诺网络中止或游戏回滚。Courier claim 必须先于创建队伍、成员/物资转移及其他派出效果。
- 资格拒绝发生在效果前；开始后异常/缺回执不能改成无副作用 Rejected 或 Cancelled，使用 Failed 与保守的 UnknownAfterStart。
- 首个真实确认回执不能被迟到取消、Dispose 或非权威尾部异常覆盖；部分成功也不能冒充整体成功。
- 外部 ReasonCode 使用稳定、有限代码；不泄露原始异常、Prompt、凭据、用户路径或内部协议标签。

### 4.4 Scene 完成定义

- 完成依据是本次请求所属的群组/接力、必要后处理、历史与事实提交的真实 owner 回执。不得以 Task 正常返回、可见文字非空或 relay index=-1 推断成功。
- 结果提供本次按真实顺序产生的可见发言，不能只丢回最后一个 NPC 的一句话；快照不可变，发言身份不暴露活游戏对象。
- 主回复、相关旁听/接力及该请求所依赖的队列收尾必须纳入；无关后续自主对话不计入。沿用原轮次/资格规则，不为了有界而减少玩法。
- 不承诺 TTS 播放结束。已有动作/必要历史阶段缺回执或失效时如实失败；已产生的部分效果不伪装为全回滚。
- 检查所开放调用链每个 Task.Run、await、重试和回调。主线程捕获 detached 输入，worker 不访问 live Hero/Mission/Agent；后续主线程重验 owner/generation/session/source。只将最终回写放主线程不足以通过。

### 4.5 Courier 完成定义

分别投影排队、已发出、运输/预生成、已送达、动作与必要历史收尾、回信交付/运输结束；不要把全部压成一个成功布尔。

- 预生成成功不等于送达或动作执行；DeliveryApplied 与 PostprocessConsumed 单独都不是完整成功证据。
- 到达后的动作/历史回执与回程状态分开记录。完整公共 operation 在回信交付和必要收尾后才成功完成，不提前向子 MOD 泄露尚未送到玩家的回信正文。
- 无法送达返回、目标死亡、队伍丢失/被毁、回信生成/提交失败都必须结算；Stage=Completed 或队伍移除不能直接映射成功。
- 到达后已有确认效果但回程失败，保留已确认的阶段信息，整体不报成功，也不宣称全部回滚。
- 出站 API 不得认领 NPC 主动来信的入站 session；如更改共享代码，定向回归入站、回复和原模式例外。
- 不新增保存键：API 关联是瞬态，原运输 session 照旧持久化。换档/owner 退休时结算旧 operation，释放关联；恢复的运输按原玩法继续，不把旧 API 自动重新绑定或重发。

### 4.6 兼容与性能

- 现有 V1 类型、方法签名、枚举数值、默认参数、Native 结果承诺不变。优先新增只读 DTO/方法，内部枚举经显式投影；若必须破坏兼容，暂停该设计决策，不偷偷改 V1。
- 继续使用同一套真实 Prompt/后处理/Actions/History/AFEF，不增加第二份 parser、效果执行器或成功事实写入。
- 绑定和结果更新发生在显式请求/阶段事件，关联按 session/request ID 查询；不新增每帧扫描全部 client/operation、不加反射轮询。
- 优先复用现有主线程队列、退休通知和背压。核对实际工作项而非仅回调数；API 活动关联/待办有上界，完成后释放，不占用已终态对象保存 live 引用。
- 构建仍为一套源码、1.3/1.4 两实现、一个模块及 Bootstrap 唯一加载。不改默认交互入口、开关或一键构建流程。

## 5. 执行包与有限退出门

### G0：开工基线与契约冻结

1. 核实 Git 根/分支/HEAD/dirty、暂存差异及计划文件；保护所有已有改动，不 reset/stash 覆盖，不 git add .。
2. 读第 3 节入口和实际消费者。冻结当前选择的输入范围、状态转换、成功/部分失败条件、线程及持久化责任。
3. 核对 SDK、双版本引用、Harmony/MCM/runtime 依赖；只读必要路径，不输出密钥。先读 runner 的写入/清理行为，再运行本包最小基线。
4. 复用 Native 公共消费者 runner；追加 Scene 提前完成、Courier 预生成假完成的具名回归场景。编译失败/缺 fixture 不是有效行为红例。
5. 将目标/非目标/owner/消费者/退出门登记主台账，作本地开工意图提交后再实质产品修改。不在 G0 重跑全仓或重新审计 J13。

### J14a：Scene

**a1 上下文和共享票据**：实现无副作用的上下文捕获；接入 client 隔离、渠道化 fingerprint、source revision；在原主线程入口校验与 claim；Native 和原 UI 行为不变。

退出门：重复并发、跨渠道冲突、错 client、旧票据、取消/UI 竞争有行为证据，Native 原回归通过。只建公共壳不算通过。

**a2 群组真实终态**：沿 ProcessCapturedScenePlayerShoutAsync → ProcessCurrentScenePlayerShout → HandleGroupResponse / HandleGroupResponsePerHeroIndependent → 后处理/队列/历史/事实串联本次工作；提供真实 typed 结果，覆盖早退、吞异常、队列退休与晚结果；修复实际开放路径所暴露的线程/生命周期缺口，不扩成全仓重构。

退出门：首轮结束而接力未完时不完成；旧请求不能发布到新场景；动作/历史各一次；部分效果后失败不假成功；每个结束/失效路径结算任务。已有局部可等待结果必须复用，不再造一套 Scene 链。

**a3 公共接线与开放**：增加 Scene 公共提交、不可变结果和显式投影；独立外部程序集调用真实生产入口；完成必要双版本回归后再将 SceneSubmit 设为 Available。更新指南/主台账/代码地图/HANDOFF。

停点：`J14a_OFFLINE_VERIFIED`，随后进入 b；不因还能抽 helper 无限停留。

### J14b：Courier

**b1 草稿资格和一次派出**：给已完成准备的草稿签发票据；抽出 UI/API 共用的最小准入与发送入口；派出前重验收件人、活动信使、成员/物资可用量、模式资格及草稿身份；claim 早于第一处派出副作用；旧弹窗回调不得清理/再次发送新草稿。

退出门：UI/API 竞争仅派出一次；未开始取消不派队/扣除；草稿变更拒绝；同一请求重试不创建第二队伍；派出部分失败如实 Unknown，不自动重派。

**b2 运输与权威回执**：绑定 operation 与真实 session 实例/ID/owner/generation；在原预生成、到达提交、回信交付、损失、移除及退休路径发布阶段结果。保留原普通、给予、展示及其他模式的资格和算法；不将 consumed/DeliveryApplied/Completed 布尔当成功回执。到达业务回执与回程结局分开保存于瞬态 API 关联。

退出门：预生成没有新增效果提交；重复到达只提交一次；未交付正文不泄露；丢失/死亡/换档/session 替换/退休均结算且无悬挂任务；不认领入站 session；结束后无残留活对象/订阅。

**b3 公共接线与开放**：增加 Courier 提交、阶段快照/终态 DTO、稳定失败码和显式投影；独立外部消费者覆盖整条运输链，必要双版本门禁通过后开放 CourierSubmit；文档写清取消、读档和可能跨多个 Campaign tick 的等待语义。

停点：`J14b_OFFLINE_VERIFIED`。

### J14c：三渠道契约与最终候选

1. 同一最终候选运行三渠道外部消费者；复用 Native 设施，优先增加渠道用例和少量 fixture，不为每个 helper 建重复 harness。
2. 保留旧 Native 消费者，验证旧 public ABI；准备基线编译的消费者/成员形状证据，不能只有新消费者重新编译成功。明确源码链接、PE metadata、实际 DLL/CLR、游戏加载各证明哪层。
3. 公共正向调用、internal 访问预期 CS0122、DTO 无活对象、内部枚举重排、能力查询冷启动/停止行为全部通过。
4. 完成第 6 节最终离线矩阵，绑定候选提交、DLL SHA、实际依赖和命令；不得为通过仅刷新 hash 或删断言。
5. 主台账统一记录，更新 V1 指南、代码范围图/地图和简短 HANDOFF。标明保留 host、实际覆盖及 NOT-RUN。完成后停止，不开始 J15。

## 6. 验证矩阵与工具入口

| 风险 | 必要用例 |
| --- | --- |
| 输入/隔离 | 空/超长/非法 ID，错渠道，缺上下文，错 client，失效票据，不同 client 同 ID |
| 幂等/容量 | 并发重复只执行一次，终态重试不重执行，同 ID 不同 payload/context/channel 冲突，128 容量不淘汰 |
| 取消 | 排队取消、claim 竞态、开始后 TooLate、Dispose、终态后取消、不抢占真实回执 |
| 生命周期 | owner 替换/退休、换档、Mission/epoch 改变、同代 source 变化、旧回调不清理新请求 |
| Scene | 单人、多轮接力、相关旁听、无人回应、目标离场/索引复用、后处理等待、部分效果后失败 |
| Courier | 预生成早于送达、重复到达、延迟回信、无法送达返回、队伍失踪/被毁、目标死亡、恢复运输不恢复旧票据 |
| 默认链路 | Native 原 UI/busy/历史/动作保持；Scene/Courier UI 与 API 不双派发；入站不被出站 API 认领 |
| 事实/可见输出 | 角色 user/assistant 正确，标签不泄漏，AFEF 只来自实际 owner，未交付回信不提前返回 |
| 外部 ABI | 独立 public 消费者、旧 Native 签名、internal 拒绝访问、只读 DTO、显式枚举映射、四实现 metadata |
| 性能 | 事件驱动与有界关联，重复/积压/退休释放，不扫描全部 API 请求，不在锁内执行外部续作 |

规划时已确认固定 SDK `local/dotnet/8.0.425/dotnet.exe`、`_deps_auto` 和 `local/bannerlord-refs/1.4.7.117484` 存在。具体引用版本由原脚本实际验证，不凭目录名猜测。

以下为执行者在仓库根目录使用的既有命令，规划者本轮没有运行它们；运行前先检查各工具对 .generated/产物目录的写入或清理范围：

```powershell
$dotnet = (Resolve-Path .\local\dotnet\8.0.425\dotnet.exe).Path
python -X utf8 -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet
python -X utf8 -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet --reorder-core-enums
python -X utf8 -B tools/NativeModuleSubmissionTests/source_boundary.py
python -X utf8 -B tools/ModuleFrameworkApiTests/run.py --dotnet $dotnet --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts
python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py
python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree
```

Native source_boundary 是精确批准差异校验：新增接线若使其失配，须读取实际差异、更新有限 reviewed 变换并保留原承诺的行为测试；不能只重写 expected hash。runner 若缺新依赖，应明确补真实源和最底层 fixture，不 stub 掉待验生产 owner。

受影响渠道优先复用现有 SceneRequestLifetimeRegressionTests、ScenePostprocessParityTests、SceneSpeechQueueOwnerTests、SceneConversationScopeTests、CourierSessionCreationTests、CourierDeliveryLifetimeTests、CourierOwnerPhaseTests、CourierCommitOutcomeTests、CourierDomainCommitTests、CourierPromptPreparationTests、CourierInboundCompletionContractTests 及 ProductionCourierHostReplayTests。开始每片时读具体 runner，再选择必要用例，不按历史数量充当通过。

最终候选：原脚本 Debug/Release × BannerlordApi 1.3/1.4 + Bootstrap 六构建；四实现 public API/metadata；受影响 Persistence Profile/Chunk/Identity（现有身份审计基线 `053ad485`，避免更老基线误报）；Bridge/readiness/source inventory、当前 DLL Phase8 与相关 Native/Scene/Courier 回归；地图两模式与 git diff --check。Phase8 用显式当前候选及 SHA/freshness，不为测试偷偷 Stage，不拿旧产物当新候选。

## 7. 安全、记录和交接

- 原 `一键编译覆盖推送/build_single_module.ps1` 会递归清理 `bin/<Configuration>/single_module_artifacts` 与 `obj/single_module/<Configuration>`。执行前核实最终绝对路径仍在本工作区、根及嵌套无 reparse、内容仅为获准产物；取得本轮精确范围确认，不继承 J13 历史授权，不绕过脚本校验。不得传 -Stage/-Deploy。
- 不写游戏、外仓、玩家存档或全局配置；不安装工具，不推送。保留原一键流程与 Bootstrap。外部路径仅作已核实的只读依赖。
- 公共边界不输出原始异常/Prompt/凭据。真实 provider smoke、实机、旧档、音频及帧性能未运行就记 NOT-RUN；同进程 MOD 不是抵御恶意反射的安全沙箱。
- 各片仅提交本任务具名文件。撤销采用定向 inverse/revert，不 reset、不改历史；不覆盖他人或其他任务变更。
- 主台账记录 owner/真实消费者、一基源码坐标、提交/产物绑定、命令与失败信号、覆盖与保留责任；HANDOFF 只摘要并链接。计划写成文件不代表产品实现已完成。
- 最终退出门全部满足立即交接 J14；只有新的具体回归或相关修改才追加门槛。无关历史 HOLD、目录未清空、主类仍大不阻塞本包有限收口。
