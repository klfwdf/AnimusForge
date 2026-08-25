# AnimusForge SceneActions / 自然语言动作与阵前演讲 v1.1

SceneActions 的运行时代码和 MCM 已迁移进 AnimusForge 主程序集。游戏内只保留
`AnimusForge设置` 一个 MCM 页面，分组为“自然语言动作与阵前演讲”；旧的
`AnimusForge_XihaiAction` 配置会在首次读取 AF 设置时迁移一次，旧 JSON 文件保留作
回滚材料，不再作为运行时配置源。

## Y 键演讲菜单

战场部署或战斗阶段按 `Y`，在 AF 原有场景喊话菜单中选择两个演讲项：
`演讲` 和 `他人演讲`。二者复用 AF 的文本框外观（旧 AF 没有该弹窗合同时
使用原生输入框），但提交不会调用 AF 的 `T` 场景喊话入口，也不会进入普通
SceneActions 观察链，而是直接进入阵前演讲会话：

```text
弟兄们，身后就是家园，稳住阵线，准备前进！
我演讲：今天我们守住这里！
```

`演讲` 分支的输入默认就是玩家对己方士兵的正文，不要求正文出现“演讲”
关键词，也不调用轻量通道分类器；`他人演讲` 分支不再打开主题输入框，确认后直接由 AF
根据当前战场和当前冻结主目标 NPC 的背景生成一段面向士兵的正文。Y 菜单只在
Deployment/Battle 支持阶段替换为演讲菜单；其他场景仍使用原来的 AF 菜单。
T 键和其它 AF 对话行为保持原样。

NPC 的“他人演讲”请求不会伪装成一次 T 喊话：扩展直接调用 AF 的单 NPC 生成方法，
使用已冻结的主题、当前目标和演讲回复认领；AF 返回后才进入本模块的到位、
正文动作分类、`ShowNpcSpeechOutput`/TTS 一次播放和士兵反应流程。生成失败、
目标离场或会话超时都会丢弃结果，不回退到普通场景喊话。

## v1.1 自然语言动作与阵前演讲 V2

- “来给大家讲俩句、上前跟弟兄们说几句”等宽松请求可在本地识别；不确定请求交给 AF 闭集分类，演员仍冻结为玩家或当前 AF 主目标。
- NPC演讲回复按会话、NPC、对话代次与正文指纹唯一认领；NPC默认前移 10 米到己方阵线前方，身体朝向身后的士兵后才显示并播放 AF 原有 TTS，重复发布会被抑制。
- 步兵与骑乘 NPC 都可演讲；NPC先到己方阵线前方并持续面向有效听众，再沿与士兵阵线平行的横线小幅随机踱步，默认半宽 2 米、每 2.5-4.5 秒确定性换位；不会向敌军纵深移动、强制下马或传送。
- 被认领的 NPC 回复会追加优先级更高的阵前演讲专用提示词：默认忽略 `PlayerCustomPromptRule`，强制把听众锁定为演讲者身后的己方士兵，要求使用上下文真实战场细节、人物口吻和隐性“观察→情绪→号召”推进，禁止向玩家答话、模板口号、星号动作、旁白或自贬拒绝；输入/背景为英文时仍输出简体中文。MCM 可在 6-160 字内设置范围，随模组交付的默认值为 60-160 字。模型仍越界时按完整句校验并回退到合规号召，不截断半句话。
- 完整演讲正文由 AF 一次闭集调用同时判断最多四个 V4 动作，并生成最多 8 条简短士兵回应；模型不能改变演员、听众、权限、最终开战命令或输出任意 `act_*`。
- 听众冻结为全体同侧有效士兵，默认 48 人分批播放 `agree/greet/cheer`，每批 6 人、每 Tick最多提交 6 项；默认 4 名不同士兵依次通过 AF 原有气泡/TTS入口作简短口头回复，随后 22 人分批发出原生 `Victory/Yell`。三类人数均可在 MCM 独立调整。
- 士兵动作、多人短回复与战吼结束后，演讲者播放一次武器状态匹配的 Native 发令动作；骑乘持武器优先使用 `act_horse_command`，步行持武器优先使用 `act_command`，随后逐个向玩家队伍的有效编队下达原生 `Advance`。该结束链可在 MCM 关闭，盟军只听和回应，不受令，也不改变玩家当前编队选择。
- MCM 页面名为 AF 主体的“AnimusForge设置”，分组“自然语言动作与阵前演讲”，保留27项动作、NPC站位、听众、声音、推进、安全半径、通知和诊断设置；原先零散的玩家/NPC自然动作和AF分类选项合并为一个“自然语言回复动作与演讲”开关并附说明。运行时覆盖再次验证，非法组合失败关闭。
- AF 原生对话输入（图示的 AI 输入框）也会把 NPC 完整自然回复送入同一 SceneActions 解析队列：模型不需要输出 `act_*` 或标签，只要确实写出“他慢慢跪下并指向旁边”等已发生动作即可；否定、引用、假设和纯对白不会误触发。该观察只在对话回复完成后提交一次，使用当前 Agent 和 Mission 快照，不增加额外模型请求。
- 远处部队接战不再取消演讲；演讲者受击/攻击、死亡、换队、逃跑、进入交互物、近敌或 Mission结束仍会取消，并只释放本模块拥有的移动与动作。
- 本轮只完成离线结构、编译和自动化验证，不代表NPC寻路、TTS时序、踱步视觉、战吼规模或 `Advance` 已通过游戏内实测。

## v1.0 兼容桥接层重构

- 模组版本重置为 `v1.0.0`，主程序集版本为 `1.0.0.0`；冻结的 Framework V1/V2/V3/V4 动作合同仍分别保持 8/16/24/27 项，不随产品版本重置。
- 新增 `ISceneActionsAfBridge`、`SceneActionsAfBridgeHost` 与 `AfV130ReflectionSceneBridge`，把 AF 接入生命周期从 `SubModule` 和具体反射实现之间解耦。
- 当前仍使用原 `AfCompatV130` 的结构兼容/Harmony 观察路径，玩家框选快照、NPC 回复排队/显示去重、AF 分类器和失败关闭行为不变。
- 分类器语义与网络传输已拆开：`IAfClassifierTransport` / `AfV130CallApiTransport` 只负责把冻结请求送入当前 AF `CallApiWithMessages`；未来切换 AF 专用 Action Postprocess 或 Typed API 时，不再改闭集语义和权限逻辑。
- Mission 状态字典已集中到 `SceneActionStateStore` partial；排队、同意、程序执行、冷却和通道所有权现在有明确的存储边界，既保持现有执行调用顺序，也为后续输入/权限/调度器拆分保留替换点。
- 输入与权限策略已拆成 `SceneActionInputRouter` / `SceneActionPermissionRouter`：输入来源开关、玩家/NPC分流、冻结目标模式、NPC同意、强制多人同步屏障与错开策略均在不接触 Agent 的纯策略层决定；Mission 层继续保留旧反射兼容入口。
- 稳定队列已封装为每 Mission 一个 `SceneActionScheduleQueue`，内部仍使用既有 `StableScheduler<PlannedTarget>`；入队顺序、容量、到期出队和关闭时取消语义没有改变。
- 权限路由现在使用 `TryResolveTargetMode`；缺失 `ParseDecision` 或 `IntentDefinition` 时返回失败并取消该请求，不再把异常状态默认降级为玩家目标。
- 通道释放失败现在记录 `CHANNEL` 诊断日志，包含调用上下文、通道号、当前动作和异常类型；原有失败关闭行为不变。诊断版入口使用独立的 `TryReleaseOwnedChannelWithContext` 名称，避免破坏旧反射入口的唯一匹配。
- `SceneActionsMissionBehavior` 的输入/权限路由已移动到 `SceneActionsMissionBehavior.Input.cs`，最近玩家语境与隐含情绪推断已移动到 `SceneActionsMissionBehavior.Context.cs`；只移动 private 方法，Mission 生命周期、反射方法名、状态字段和执行顺序不变。主文件由约 4420 行降至约 4119 行。
- NPC 冻结同意的注册、匹配、接受/拒绝/不明确处理与单 NPC 消费已移动到 `SceneActionsMissionBehavior.Consent.cs`；AF 可选动作白名单和 Native provider 就绪检查已移动到 `SceneActionsMissionBehavior.Readiness.cs`。主文件进一步降至约 3732 行，反射兼容方法名保持不变。
- AF 普通分类、同意分类、超时/取消、Late Task 异常观察和两个 Mission-thread 完成队列已移动到 `SceneActionsMissionBehavior.Classification.cs`；后台任务仍只写入完成队列，目标/权限/动作状态仍在 Mission 线程应用。主文件进一步降至约 3351 行。
- MissionBehavior 已完成职责分区：生命周期、程序编排、动作播放、Stateful 状态机、结果/清理、运行时模型分别位于 `SceneActionsMissionBehavior.Lifecycle.cs`、`Program.cs`、`Playback.cs`、`Stateful.cs`、`Results.cs`、`Models.cs`；原主文件只保留字段、构造函数和公共 Mission 属性，当前约 73 行。所有方法体按机械边界原样移动，未改变方法名或调用顺序。
- 程序编排和播放执行已继续细分：Program 分为 `Build/Start/Progress/Barrier/Completion`，Playback 分为 `Ownership/Targets/Execute/Engine/Cooldown`；旧的空 `Program.cs`/`Playback.cs` 壳已删除。当前共有 22 个 `SceneActionsMissionBehavior*.cs` 职责文件，主壳约 69 行、0 个业务方法，最大单文件约 472 行。
- 重构后复跑自然语言回归：1000 条刻意避开已知关键词的 OOV 样本覆盖全部 27 个动作，玩家/NPC/闭集分类共 4000 次检查、0 mismatch；990 条真实 AF 去重语料 changed=0；Downloads 中 20 份 Token_Stats 提取的 1707 条动作描写与重构前结果文件 SHA-256 完全一致。
- 传输仍保持单飞门、取消令牌、32/8 token 上限、温度 0 和 `recordTokenStats=false`，本轮不改变模型请求参数。
- SceneActions 已作为 AF 主工程源码的一部分编译；`SceneActionsMcmSettings` 仅保留窄范围、缓存反射元数据的兼容桥，不再注册第二个 MCM。它不进入 Mission Tick 热路径，未来可在不改动作核心和 Mission 执行器的前提下替换为 Typed Bridge。
- `settings.v1/v2/v3/v4.json` 继续代表配置结构代际，不等同于模组产品版本。

## 阵前演讲 V1

阵前演讲是独立的 Mission 会话层：AF 继续负责接受喊话、生成 NPC 回复和播放语音；本模组只冻结演讲者、听众、战斗阶段和会话生命周期。它不修改 AnimusForge 本体，不暂停全场 AI，也不直接改变士气、伤害或属性。

玩家亲自演讲支持两种写法：

```text
开始阵前演讲
随后发送：士兵们，今天我们守住这条防线！

我阵前演讲：士兵们，今天我们守住这条防线！
```

NPC 演讲只通过 Y 键菜单触发：

```text
Y → 他人演讲 → 确认
```

### 强制演讲通道

带冒号的强制命令会在 AF 普通场景喊话入口之前被本模组认领，不再让 SceneActions 普通动作链重复消费。T 键强制通道只接受玩家演讲，发起者（玩家）直接发表冒号后的正文：

```text
*强制演讲：将士们，守住阵线，随我前进！
强制指令演讲:弟兄们，今天我们为家园而战！
我演讲：弟兄们，今天我们为家园而战！
```

ASCII `:` 和中文 `：` 均支持。旧版 `你/他/她/目标演讲`、`强制你来演讲` 等 T 键 NPC 语法已移除；它们不会创建 NPC 演讲会话，也不会把 NPC 改由玩家代演。NPC 演讲请使用 Y 菜单的“他人演讲”，由菜单冻结演员并绕过 T 键触发分类器。强制命令必须带冒号；缺少冒号时不会进入强制通道，避免误伤普通聊天。旧的 `我阵前演讲：` 玩家接口保持不变。

玩家两步式命令会等待最多 60 秒；NPC 请求会等待该 NPC 在同一 AF 对话代次中的回复。NPC 回复必须先进入 AF 队列并通过会话、Agent、代次和正文指纹认领；随后暂存到 NPC 到达阵前位置，再由原 `ShowNpcSpeechOutput` 入口显示和播放 TTS 一次。旧回复、接力 NPC、错 Agent、错代次、错正文和重复显示都会被拒绝。

### 不写“演讲”的直接正文

玩家也可以直接喊出完整的战前正文，不必先写“演讲”：

```text
弟兄们，我们的身后就是家园，该死的敌人就在前方！
```

框架会先检查是否以“弟兄们/将士们/战士们/全军”等部队称呼开头，并同时包含家园、敌人、阵线、冲锋、胜利等战前修辞；高置信度时本地直接进入 `PLAYER_SPEECH`，整句原文立即播放。较弱但可能是演讲的文本才调用一次轻量闭集分类器；T 键分类器只返回 `PLAYER_SPEECH`、`ORDINARY_SCENE` 或 `NONE`，不生成正文，也不能选择 NPC 演员。`ORDINARY_SCENE` 和超时/非法结果都回到 AF 原普通通道。普通寒暄、引用他人、复述、假设和没有战前修辞的“弟兄们”不会触发。

支持的阶段：

- Deployment：部队已生成且 `TeamSetupOver`，但部署尚未结束。
- Battle：部署结束后的有效战场，且演讲者周围安全半径内没有敌人。

会话开始时冻结当时全部同侧有效士兵，默认包括盟军，不受玩家框选数量限制；后续增援不加入本次快照。演讲期间只对演讲者附近做节流的敌军安全扫描。远处部队开战以及 Deployment 切换到 Battle 不会终止会话。

以下情况会自动取消：演讲者受击、主动蓄力或攻击、敌人进入默认 35 米安全半径、演讲者死亡/离场/换队/逃跑/使用场景物体、战场关闭、等待超时或玩家输入“取消阵前演讲”。远处其他 Agent 的命中不会取消。

### 演讲者动作编排

演讲开始前，框架会读取完整正文中的星号描写、普通正文身体动作和隐含演讲语义。本地明确单动作直接冻结；复杂、隐含或多动作正文交给 AF 闭集分类，一次返回最多 4 个 V4 一次性动作。模型不能改变演讲者、听众、权限、最终开战命令或输出任意 `act_*`。常用演讲动作包括：

- `explain`：说明原因、局势和战术。
- `point`：提到前方、侧翼、城墙、山口或明确方位。
- `command`：守住、列阵、向前、冲锋、跟随等号令。
- `promise`：保证、发誓、承诺、同生共死。
- `rage`：复仇、血债、绝不屈服或强烈战意。

AF 不可用、超时或输出非法时，模型推断的动作失败关闭；本地已经明确识别出的动作仍可播放。最终发令与全军推进由 MCM 冻结，不依赖 AF 分类结果。动作间隔至少 1.4 秒，短演讲会自动减少动作数量。每个动作都通过独立可信 Mission 队列进入现有 SceneActions Catalog、Native Provider、Agent 校验、冷却、通道所有权和 `SetActionChannel(ignorePriority:false)`，不接受任意 `act_*`，也不绕过引擎优先级。

### 听众分批反应

- 从冻结听众快照中确定性抽取，默认最多 48 人；同一演讲会得到相同的参与者和反应计划。
- 较长演讲中段，少量士兵以 `agree` 点头或以 `greet` 举手回应。
- 演讲正常结束后，以 `cheer / greet / agree` 组成最终响应；默认每批 6 人，每 Tick 最多提交 6 项，不会让数百人同帧播放。
- 默认确定性选择 4 名不同士兵，依次说出与正文相关的短回应；AF在同一次演讲语义调用中生成这些短句，运行时复用原 `ShowNpcSpeechOutput` 气泡/TTS/口型入口，不为每名士兵单独请求模型。AF输出无效或不可用时使用有界本地短句兜底。MCM可设置 0-8 人和 0.5-3 秒间隔。
- 默认从冻结听众中独立确定性选择 22 人，分批播放原生 `Victory/Yell`，不依赖动作回应者或文字回应是否成功播放。
- 全部文字回应和战吼完成后，演讲者先播放 `command` 下令手势，等待 MCM 设置的动作展示时间，再向玩家直属队伍的各有效编队下达原生 `Advance`；盟军只听和回应，不受令。发令动作队列连续 3 秒不可用时取消本次 Advance，避免状态机永久等待。
- 演讲被取消时不播放结尾欢呼；已进入队列但尚未执行的步骤由同一演讲 OwnerToken 阻断。
- `cheer` 按持续风险动作管理；表演尾声结束时只释放仍属于该演讲 OwnerToken 的通道，不会停止玩家后来下达的其他 SceneActions 动作。
- 即使某帧卡顿积压了多个到期动作，每 Tick 也只按 MCM 预算向可信队列提交；成功迟到提交后会重新保留 3.5 秒表演尾声，避免刚排入的欢呼被同一时刻的清理吃掉。
- 士兵移动、受击、骑乘、逃跑、撤退、使用场景物体或 Agent 失效时会跳过反应，不修改编队、AI 控制器或战斗命令。

独立严格配置位于 `ModuleData/SceneActions/battle-speech-performance.v1.json`；文件缺失时使用审计默认值，文件存在但含未知/缺失字段、重复键、注释、错误类型或越界数值时，表演层失败关闭，但基础阵前演讲会话仍可继续。

当前演讲结束点仍按正文长度估算，尚未桥接 AF 的真实 TTS 播放完成事件，因此不能宣称 NPC 动作尾声与音频结束精确同步。士气效果、摄像机过场和最终视觉表现仍属于后续扩展。本轮没有启动游戏；不同兵种、装备和密集阵型下的动作可见性与穿插仍需实机验证。

## v5.3.1 播放前安全清理上一动作

- 普通一次性动作、拔剑和收剑准备执行时，会先释放本模组拥有的旧循环/残留动作通道。
- 跪姿 state 不再只是从所有权字典移除；替换为普通动作前会真正释放其 channel 0。
- 旧动作通道释放失败时，新动作失败关闭并记录 `PreviousActionNotReleased`，避免出现“新动作在播放、旧循环仍继续”的叠加状态。
- 如果通道已经被游戏或其他 Mod 接管，SceneActions 只丢弃过期所有权记录，不会清除外部动作。
- 明确的跪姿双通道程序仍按原规则保留跪姿和上层手势，不会被普通播放前清理误伤。
- `放下手臂`、`把手放下`、`收回手臂`、`结束行礼`、`停止行礼` 现在明确映射到本地 `stop_action`；可用于结束西海礼或其他本模组仍占用的动作通道。
- `laugh` 补充“发出一阵笑声、爆发出笑声、骤然笑了出来、朗声笑了起来”等已发生动作线索；AF 回复中的“骤然发出一阵笑声”不再因只命中普通“站直身体”而漏播。
- 否定句如“没有放下手臂”“没有发出一阵笑声”仍保持 `NoAction`，不会反向触发停止或大笑。

## v5.3.0 循环/残留动作停止与真实持械控制

- 新增本地运行时控制 `stop_action`，识别“停止欢呼、别再欢呼、停止跳舞、结束此项、恢复正常站姿”等命令或 NPC 星号舞台描写。
- 非状态动作播放成功后登记演员、逻辑动作、原生动作 ID 与通道；停止只会在当前通道仍属于 SceneActions 时释放，不会清除游戏或其他 Mod 后来接管的动作。
- 新增 `draw_weapon` 与 `sheathe_weapon`：拔剑会扫描演员 0–3 号装备槽，选择真实的非消耗型近战武器并调用 Bannerlord 1.4.8 `TryToWieldWeaponInSlot`；收剑调用 `TryToSheathWeaponInHand`。
- `*拔剑` 与 `*停止欢呼` 仍需要框选 NPC 同意，`*强制拔剑` 与 `*强制停止欢呼` 直接作用于冻结的框选目标，`*我拔剑` 只作用于玩家。
- 三个控制意图不属于冻结的 V1/V2/V3/V4 公共动作合同，也不允许 AF 分类器输出；只由本地确定性语法和已发生的 NPC 舞台描写触发。
- 本轮未启动游戏；动作通道释放、会话场景中 AI 是否保持持械，以及拔剑/入鞘视觉完成仍需实机验证。

## v5.2.0 AF 闭集模型软分流

- NPC 星号动作描写不再把所有“提到已知动作但本地无法确认”的情况统一标记为 `Invalid`。
- 本地现在分成三层：明确否定、假设、引用或未发生动作直接 `NONE`；原生 `act_*` 注入继续硬拒绝；复杂可见动作、命令他人、库外动作混合及措辞不确定的情况交给 AF 闭集模型。
- AF 接入仍反射调用其公开 `ShoutNetwork.CallApiWithMessages`，沿用 AF 当前配置的主模型通道；分类请求固定温度 `0`、最多 `32 tokens`、不记录 Token 统计，并且演员、目标、强制状态和允许动作键均由本模组冻结。
- 模型可以忽略库外动作并选择语义独立、确实由当前回复 NPC 做出的白名单动作，但不能输出库外动作、原生动作 ID、目标或权限。
- 同一批 990 条真实 AF NPC 动作语料复跑后为：171 条本地动作、753 条 AF 分类、66 条本地 `NONE`、0 条通用安全阻断。原 v5.1.1 的 136 条阻断被拆成 80 条 AF 分类与 56 条 `NONE`，其余 854 条结果没有退化。
- 当前模型另对这 80 条做了离线保守标注：34 条预期播放动作或程序，46 条预期 `NONE`；80/80 输出均通过闭集协议解析。该离线标注不代表用户实际配置的 AF 模型一定给出完全相同结果。

## v5.1.0 上下文隐含情绪推断

- NPC 回复的隐含情绪不再要求出现 `fear/rage/disappointed/laugh/unsure` 或对应中文标签。
- 模组为每个框选 NPC 保存最近一轮玩家对白，最长 60 秒；回复时同时使用玩家上一句、星号动作段和完整 NPC 回复，但演员、目标、权限与强制状态仍由代码冻结。
- 五个可推断的表达动画为 `fear`、`rage`、`disappointed`、`laugh`、`unsure`；跪下、指向、跳舞、投降、割喉手势等实体动作仍必须明确发生，不能根据内心或对白虚构。
- 本地推断采用处境、身体反应、语气、对白和掩饰行为的组合计分；“脸色发白后强作镇定”保留已经发生的恐惧证据，明确的“面不改色、毫无惧色”等相反证据继续阻止误触发。
- 本地仍无法确定时，AF 闭集分类器会收到受限的一轮上下文和隐含情绪白名单；协议仍只接受 `NONE`、`PLAY_ACTION`、`PLAY_PROGRAM`。
- 离线压力测试覆盖 1000 条刻意避开动作 ID 和显式情绪标签的复杂表达：800 条间接情绪正例、200 条相反或中性反例。

## v5.0.4 自然语言分类可靠性修复

- 为 `fear` 增加“惶恐、局促不安、畏缩、战战兢兢”等已发生动作描写的本地语义线索。
- NPC 舞台描写命中这些线索时直接进入 `fear`，不再依赖 AF 远程分类器才能播放。
- 未命中的复杂自然语言仍可请求 AF 分类；分类超时从 6 秒调整为 15 秒，请求寿命同步为 20 秒。
- 纯对白、否定、引用和“解释自己惶恐但没有动作”仍失败关闭。
## v5.0.3 跪姿 loop 动作

- 新增模组动作 ID `act_af_kneel_loop`，只绑定 Native 动画 `anim_main_story_conspirator_kneel_down_1_loop`。
- `kneel` 的 Enter/Hold 两阶段都冻结为该 loop；`stand_up` 仍只释放本模块拥有的状态，不直接暴露原生起身动作。
- 这一轮只做静态资源和离线执行链验证，loop 的最终视觉姿态仍需用户实机确认。

本模块观察 AnimusForge 已接受的场景喊话，在不改变 AF 对话、历史、回复或 TTS 的前提下，
把严格闭集中的逻辑意图转换为 Bannerlord 表现动作。

## v5.0.2 / Bannerlord 1.4.8 兼容调整

- 按用户要求删除 AnimusForge 与 `TaleWorlds.Library.dll` 的精确 SHA-256 运行时门禁。
- DLL 文件内容发生变化时，不再仅因为哈希不同就关闭全部动作输入。
- 继续保留结构契约检查：AF 必须只加载一个主程序集，且喊话入口、框选上下文、NPC 数据、
  NPC 回复发布方法、模型调用方法及其参数/返回类型必须仍然匹配。
- 结构契约不匹配、Harmony 安装失败或必要类型缺失时仍会停止 AF 接入，避免在未知接口上调用。
- 当前构建与静态验证基线更新为 Bannerlord `v1.4.8.119303`、AnimusForge `v1.3.2.1`。
- `animusforge.main.v130`、`AfCompatV130` 和 Harmony ID 作为既有兼容身份保留，不再表示精确版本锁。

## v5.0.1 Mission 会话修复

- SceneActions 行为现在通过 `OnBeforeMissionBehaviorInitialize` 注册，确保 Bannerlord 在同一轮
  引擎行为初始化中调用其 `OnBehaviorInitialize` 并建立活动会话。
- `OnMissionBehaviorInitialize` 仅执行后置完整性检查；若行为缺失或未激活，该 Mission 失败关闭并写入明确日志。
- V1/V2/V3/V4 动作合同、解析语法、目标权限、配置结构及 Native 动作映射均未改变。

## V1、V2、V3 与 V4 合同

- `SceneActionFrameworkV1`、`SceneActionContractEntryV1`、`SceneActionsApiV1` 保持原八项：
  `kneel`、`stand_up`、`xihai`、`cheer`、`applaud`、`respect`、`threat`、`surrender`。
- `SceneActionFrameworkV2`、`SceneActionContractEntryV2`、`SceneActionsApiV2` 对外返回 16 项，
  在 V1 上新增以下八项：

| 逻辑键 | Bannerlord 动作 |
|---|---|
| `laugh` | `act_taunt_15` |
| `point` | `act_taunt_17` |
| `rage` | `act_taunt_18` |
| `fear` | 确定性选择 `act_taunt_01/21` |
| `disappointed` | 确定性选择 `act_taunt_04–07` |
| `challenge` | 确定性选择 `act_taunt_10/11/14` |
| `search` | 确定性选择 `act_taunt_23/24` |
| `dance` | `act_dance_norse` |

`dance` 在 `as_human_warrior` 中映射到 `anim_tavern_dance_norse`。生产入口始终拒绝玩家输入的
任意 `act_*`；用户配置只能添加逻辑别名，不能扩展原生动作 ID 白名单。

- `SceneActionFrameworkV3`、`SceneActionContractEntryV3`、`SceneActionsApiV3` 对外固定返回 24 项，
  保持 V1/V2 的键、顺序、数量与程序白名单不变，并新增以下八项：

| 逻辑键 | Bannerlord 动作 | 模式 | 可与跪姿叠加 |
|---|---|---|---|
| `greet` | `act_greeting_front_1–6` | 确定性选择一次性 | 是 |
| `agree` | `act_conversation_normal_positive/normal_very_positive` | 确定性选择一次性 | 是 |
| `disagree` | `act_conversation_normal_negative/normal_very_negative` | 确定性选择一次性 | 是 |
| `unsure` | `act_conversation_normal_unsure/talk_dunno` | 确定性选择一次性 | 是 |
| `explain` | `act_conversation_talk_explain/talk_commenting` | 确定性选择一次性 | 是 |
| `promise` | `act_conversation_talk_promise` | 一次性 | 是 |
| `cross_arms` | `act_conversation_talk_crossedarms` | 一次性，不保持 | 是 |
| `deep_bow` | `act_taunt_02` | 一次性 | 否 |

- `SceneActionFrameworkV4`、`SceneActionContractEntryV4`、`SceneActionsApiV4` 对外固定返回 27 项，
  保持 V1/V2/V3 的键、顺序、数量与程序白名单不变，并新增以下三项：

| 逻辑键 | Bannerlord 动作 | 模式 | 可与跪姿叠加 |
|---|---|---|---|
| `command` | `act_command_unarmed` | 一次性 | 是 |
| `follow_me` | `act_command_follow_unarmed` | 一次性 | 是 |
| `cut_throat` | `act_conversation_threat_cuttrhoat` | 一次性 | 是 |

V4 还只在 `BuiltInContent.CreateV4` 中扩充五组确定性变体；旧工厂映射不变：

| 逻辑键 | V4 Native 变体池 |
|---|---|
| `cheer` | `act_cheer_1–4`、`act_taunt_cheer_1–4` |
| `threat` | `act_taunt_29/30`、`act_conversation_threat_arm/body/point` |
| `surrender` | `act_taunt_26/28` |
| `point` | `act_taunt_17`、`act_conversation_point_somewhere` |
| `rage` | `act_taunt_18`、`act_conversation_rage` |

运行时使用 V4；V1/V2/V3 API 继续给旧调用者提供冻结的 8/16/24 项外观。V4 新增或扩充的
14 个 Native 动作 ID 已离线核对为 `Native/ModuleData/action_types.xml` 中各声明一次，并在
`as_human_warrior` 中各映射一次；模组自身不重复声明它们。历史 V1 `applaud` 仍保留原 Native
映射契约；`dance` 继续由本模组为 `as_human_warrior` 显式补充映射。

## 输入语法与权限

- `*我指向`：目标冻结为玩家，立即执行。
- `*指向`：目标冻结为提交时的框选 NPC，每名 NPC 分别决定是否接受。
- `*强制指向`：目标冻结为提交时的全部框选 NPC，跳过同意，直接执行。
- NPC 回复中的 `*他指向旁边*`：只作用于说出该回复的 NPC。
- 新增动作的不带星号精确口令（如 `大笑`、`跳舞`、`发号施令`）默认由玩家执行。
- V3 精确口令包括 `问候`、`点头同意`、`摇头否定`、`摊手`、`比划解释`、`举手起誓`、
  `抱臂`、`深鞠躬`，并保留对应英文逻辑键及常见英文动作短语。
- V4 精确口令包括 `发号施令`、`下令手势`、`招手示意跟上`、`跟上手势`、`割喉手势`、
  `抹脖子手势`，并保留 `command`、`follow_me`、`cut_throat` 和常见英文动作短语。
- 普通聊天不做开放式动作触发；成对星号玩家文本也不会被当作单星号命令。

演员、目标、框选快照、强制标志和 NPC 同意要求均在模型调用前冻结。动作模型只能判断动作、
先后顺序和同时关系，不能输出或改变目标与权限。

## 自然语言与动作程序

精确别名和单个明确的自然动作由本地解析；多个动作或本地未知措辞才交给 AF 主模型。协议只接受：

```text
NONE
PLAY_ACTION <key>
PLAY_PROGRAM <key>[+<key>][><key>...]
```

`>` 表示先后，`+` 表示同时，总逻辑动作最多 4 个。旧的 `PLAY_ACTION` 自动包装为单步
`ActionProgramV4`。模型输出中的每个键都必须属于运行时冻结且当前可用的 V4 白名单；多行、解释、目标、
强制标志、原生 ID、第五个动作或任何未知键都会使整条请求失败，不会执行部分结果。
旧 `ActionProgramV3` 可无损包装为 V4；V4 程序只有在全部键均属于冻结的 V3 合同时才提供 V3 兼容视图，
含 `command`、`follow_me` 或 `cut_throat` 的程序不会伪造旧程序字段。

例如 `*我大笑着跪下并指向旁边` 会请求模型判断关系；若模型返回
`PLAY_PROGRAM laugh+kneel+point`，受控规范化结果为：

```text
kneel+laugh > kneel+point
```

否定、拒绝、假设、意图、尝试、差点发生、引用、解释词义、未发生动作，以及白名单动作与已知
库外动作混合的描述继续失败关闭。模型不可用、超时或输出非法时整条不执行。任意普通聊天不会
因为包含动作词而调用动作模型。

V4 的 27 项动作共用一份编译冻结语义表，本地自然语言解析、精确别名与 AF 定义块从同一表读取。匹配采用
区间和长线索优先：`深深鞠躬` 优先于普通 `鞠躬`，`摇头叹息` 是 `disappointed`，普通摇头表示
反对才是 `disagree`，`颔首/点头致意` 是 `respect`，点头表示赞成才是 `agree`。不同位置实际
出现两个动作时仍交给 AF 判断先后或同时关系。

`你好`、`我同意`、`我不同意`、`我不知道`、`我保证` 等纯对白不触发身体动作；必须出现挥手、
点头、摇头、摊手/耸肩、比划、举手起誓等已发生动作线索。普通 `鞠躬/行礼` 仍是 `respect`；
只有 `深深鞠躬`、`弯腰深鞠一躬`、`躬身到底` 等明确深弯腰描写才是 `deep_bow`。

`前进`、`跟我来`、`我要割断你的喉咙` 等纯对白也不触发 V4 手势。`command` 必须有向群体
挥臂/抬手下令的实际动作，单纯指向仍是 `point`；`follow_me` 必须有向友方招手示意跟随的动作，
向敌人勾手叫阵仍是 `challenge`；`cut_throat` 必须明确是手指划过喉前的威胁手势。真实持刀割喉、
砍喉或攻击属于库外暴力行为，整条程序失败关闭。

## NPC 同意

普通 NPC 请求冻结的是完整 `ActionProgramV4`，不是单个动作键。NPC 一次明确接受后执行整段；
拒绝则删除自己的请求；不明确答复保留到后续回复或 30 秒超时。框选多人时，每名 NPC 的请求、
回复、模型同意判断和执行状态互相隔离，一个人的答复不会替别人同意。

- `好，我答应。`、`遵命。`：接受完整冻结程序。
- `绝不！`、`我拒绝。`：拒绝。
- `让我考虑。`：不明确，不执行。
- NPC 回复里直接出现实际动作描写：该回复 NPC 执行描写得到的动作程序，并消费其旧请求。

同意模型只允许返回 `ACCEPT`、`REFUSE` 或 `UNCLEAR`。其负载只有冻结程序和不可信 NPC 回复，
没有目标字段，也不能生成新的动作程序。

## 程序执行

- 一次性动作只有在当前动作/进度被观察到完成后才推进；每步安全超时默认 6 秒。
- 中间跪姿进入 Hold 后保持 1 秒再推进；末尾跪姿继续保持，`stand_up` 只退出本模块拥有的跪姿。
- 中间舞蹈保持 4 秒并安全释放；末尾舞蹈保持到移动、受击或下一动作打断。
- 随机动作变体在请求、步骤和目标维度预先确定，程序执行中不会重新抽取。
- `SetActionChannel` 始终使用 `ignorePriority=false`。

### 实验双通道

配置默认开启受控实验双通道，只允许：

```text
kneel + laugh/point/rage/fear/disappointed/challenge/search
kneel + greet/agree/disagree/unsure/explain/promise/cross_arms
kneel + command/follow_me/cut_throat
```

跪姿占通道 0，并附加 `anf_enforce_lowerbody`；上层动作占通道 1。舞蹈使用通道 0、
`anf_enforce_all`，不参与叠加。三动作同时组会拆成连续的双层阶段。

任一通道拒绝或被打断时，运行时只在确认通道仍由本模块拥有时释放它们，然后把该目标剩余程序
降级为顺序播放；若无法安全释放，就只取消该目标，不触碰外部动作通道。

### `*强制` 多人同步

- 1–2 人：无延迟。
- 3 人及以上：当前有效目标中的首人 0 秒，其余每人获得独立、确定性的 `0.01–0.02s` 延迟。
- 延迟不是按人数累计；连续程序每一步都有同步屏障，再重新计算同样的独立错开。
- 失效或失败目标会退出屏障，不阻塞其余目标。

普通非强制批次继续保留原有稳定调度与容量上限。强制只绕过 NPC 同意，不绕过非人类、骑马、
受击、失效 Agent、资源缺失、引擎优先级或安全释放检查。

## 配置与迁移

运行时优先严格读取 `ModuleData/SceneActions/settings.v4.json`。如果 v4 文件存在但结构非法，
配置失败关闭；不会暗中读取 v3/v2/v1。只有 v4 文件不存在时，才依次兼容读取 v3、v2、v1，并补齐：

- 最大 4 动作；
- 单步 6 秒；
- 中间跪姿 1 秒、舞蹈 4 秒；
- 双通道实验开关；
- 强制多人阈值 3 与 `0.01–0.02s` 独立错开；
- V2 执行参数与动作审计项；
- V3 新八项和 V4 新三项动作覆盖；从旧 schema 迁移时，所有较新动作默认 `enabled=false`。

随模组交付的 v4 文件显式启用全部 26 个可播放动作。JSON 禁止注释、重复属性、未知属性、
schema 合同外动作/意图和任意 `act_*` 配置键。

## 发布验证状态

本次按用户要求没有启动游戏，也没有调用真实 AF 模型。已执行 Core 测试、Release 编译、
离线反射/静态验证和发布文件哈希审计。V4 新动作视觉、扩充变体、扩展双通道组合和普通 warrior 的舞蹈表现属于
“用户授权的实验能力、尚未实机验证”，不能视为视觉验证通过。

当前构建与静态验证基线：

- Bannerlord：`v1.4.8.119303`
- AnimusForge：`v1.3.2.1`
- 运行时不再比较 AnimusForge 或 TaleWorlds DLL 的 SHA-256。
- 发布清单仍记录构建时文件哈希，用于部署审计和回滚，不参与启用/禁用判定。
- AF 私有接口仍通过类型、字段、方法、参数和返回类型进行结构验证。

常用验证命令：

```powershell
$game = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
$module = Join-Path $game 'Modules\AnimusForge_XihaiAction'
$stage = Join-Path $env:TEMP ('AnimusForge_XihaiAction-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))

dotnet run --project "$module\tests\AnimusForge.SceneActions.Core.Tests.csproj" -c Release
dotnet build "$module\src\AnimusForge.XihaiAction.csproj" -c Release --no-incremental `
  -p:BannerlordGamePath="$game" -p:OutputPath="$stage\main\"
dotnet build "$module\tools\StaticVerifier\AnimusForge.SceneActions.StaticVerifier.csproj" `
  -c Release --no-incremental -p:OutputPath="$stage\verifier\"
& "$stage\verifier\AnimusForge.SceneActions.StaticVerifier.exe" <staging-module-root> $game
```

主工程默认 `OutputPath` 指向活动模组的 `bin\Win64_Shipping_Client`；因此验证构建必须像上面一样显式指定
staging 输出目录，不能直接运行未覆盖输出路径的 `dotnet build`。

日志：`bin/Win64_Shipping_Client/AnimusForge.XihaiAction.log`。
模组目录、显示名和 Module ID 均不带 `_1_3`；保留的 `v130` 名称只是兼容 API 身份。
