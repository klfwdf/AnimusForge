# HANDOFF: AF 0.8.6 + GCCZ 士兵行为决策链路优化

> 目标：给后续 Codex/开发者接手 `AnimusForge v0.8.6 + GCCZ 攻城后处置` 时，明确目录角色、隔离边界、桥接方式，以及士兵在 GCCZ 场景中的行为决策链路优化方向。
>
> 注意：这里的“士兵思维链路”指**可实现的行为决策状态机/上下文链路**，不是要求模型输出隐藏思维过程；NPC 正文仍只自然说话，不输出内部推理或机制标签。

## 固定目录角色

- `G:\AFMOD\YM0.8.6\Mount-Blade-Bannerlord-AnimusForge-mod-main`
  - AF v0.8.6 上游源码。
  - **只读参考/拉取源，不能直接改。**
  - 任何适配都应在融合树中完成，不要污染这份原作者源码。

- `G:\AFMOD\GCCZ`
  - GCCZ / 攻城后处置独立源码区。
  - 士兵行为决策、标签路由、处置规则、提示词片段、结算参数、测试都应优先写在这里。
  - 这里是 GCCZ 可复用逻辑的 source of truth。

- `G:\AFMOD\new-086`
  - AF v0.8.6 + GCCZ 的融合编译/测试树。
  - 从 AF 0.8.6 源码接入 GCCZ 独立代码，只做可构建、可进游戏测试的融合版。
  - AF 侧文件只允许薄桥接：传入上下文、调用 GCCZ 独立区、接收结果并执行 Bannerlord/AF runtime side effects。

## 底层技能 / 架构硬规则

1. **GCCZ 主体代码必须在独立区域**
   - 优先路径：`G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention\*`
   - 融合树镜像路径：`G:\AFMOD\new-086\AnimusForge.SiegeAftermathIntervention\*`
   - 独立区应承载：策略、常量、profile、tag catalog、routing policy、settlement outcome profile、prompt/context builder、测试。

2. **AF 侧只做桥接和代码调用**
   - 可改融合树中的 AF 文件，但只能做：
     - GCCZ active-stage gate；
     - 收集 live Bannerlord/AF 对象；
     - 传入 speaker/target/player-turn/mission facts；
     - 调用 GCCZ 独立策略；
     - 根据策略结果执行原生/AF side effects；
     - 记录日志和最小 UI 提示。
   - 不要把 GCCZ 业务规则、标签语义、士兵行为策略直接堆进 `ShoutBehavior.cs`、`MyBehavior.cs`、`SiegeAiInterventionBehavior.cs`。

3. **只能在 GCCZ 阶段触发**
   - 所有 GCCZ 标签、NPC 状态覆写、士兵行为、平民逃跑/反抗、结算后果必须以 active GCCZ mission/session 为前置条件。
   - 普通 AF 对话、普通城镇场景、野外喊话、世界地图命令不得受 GCCZ 逻辑影响。

4. **同步规则**
   - 改了 GCCZ 规则/策略/提示词/结算参数：先改 `G:\AFMOD\GCCZ`，再同步到 `G:\AFMOD\new-086`。
   - 若某个改动只能存在于融合树 AF 文件中，必须在 `G:\AFMOD\GCCZ\docs\bridge\` 或本 handoff 记录桥接点，防止下次迁移丢失。

## 士兵行为决策链路优化目标

### 1. 身份覆写链路

士兵在 GCCZ 场景中必须稳定识别自己为：

1. 玩家带入城的攻城胜利方士兵；
2. 玩家当前处置命令的执行者；
3. 不是本城旧守军、不是俘虏、不是被缴械者、不是城镇治安官；
4. 不应把玩家当作普通路人、犯罪者或本地领主在和平城镇里闹事；
5. 身份覆写不抹掉原兵种、文化、口癖和 AF 知识库命中条件；库赛特士兵仍应能调用库赛特相关知识，只是本场景军务身份变成玩家入城胜利方士兵。

实现建议：

- 独立区维护 `SiegeRuntimePromptProfile` / soldier identity profile。
- AF 桥接只传：是否 allied soldier、是否 guard/soldier、是否 civilian、当前 settlement、当前 outcome、历史记忆。
- 规则文案必须反复压制“我是俘虏/败兵/旧守卫”的错误自我认知。
- 规则文案也要保留“我是某文化/某兵种出身”的口吻来源，避免把身份覆写写成文化失忆。

### 2. 玩家权威链路

士兵的行为判断应先确认：

1. 当前是攻城后入城处置；
2. 玩家是胜利方现场统帅；
3. 玩家对平民/财物/处置路线拥有现场命令权；
4. 士兵可以不满、请示、提醒军心，但不能抗命或自行升级处置；
5. 同文化不是阻止玩家进城处置、搜掠、血洗或屠民迁殖的理由；玩家可以清算自己的同胞。

实现建议：

- 在 GCCZ prompt/context 中把“玩家现场权威”作为 runtime fact。
- 不要在 AF 全局 prompt 中永久写死这条，只在 GCCZ active scene 注入。

### 3. 破坏性标签触发链路

由于 AF 0.8.x 后处理会让 NPC 自然聊天也更容易触发标签，GCCZ 必须严格限制破坏性结算标签来源：

- `[ACTION:搜掠]`
- `[ACTION:血洗]`
- `[ACTION:殖民]`

必须同时满足：

1. 当前处于 GCCZ active scene；
2. 当前说话者是玩家己方入城士兵；
3. 当前 NPC 回复是**直接回应玩家本轮明确命令**；
4. 回复语义是服从/确认/执行玩家命令，而不是 NPC 之间闲聊、转述、担忧、请示或猜测；
5. 不得因为定居点与玩家/士兵同文化就阻断破坏性标签；同文化只影响士兵语气，例如压抑、不适、沉默或更低沉。

禁止触发：

- 士兵和士兵聊天提到“要不要血洗”；
- 士兵听到平民害怕“他们会屠城吗”；
- 平民向玩家求饶时提到“别血洗”；
- 士兵主动请示“是否搜掠/血洗/殖民”。

这些情况最多只能生成士兵请示/上下文记忆，不能直接结算。

### 4. 平民坏苗头 → 士兵请示链路

玩家和平民/商人/头人/要人对话时，如果出现抢钱、威胁、交物资换命、屠城暗示等坏苗头：

1. 平民侧只允许局部标签，例如 `[ACTION:抢钱]`；
2. 不允许平民输出 `[ACTION:搜掠]`、`[ACTION:血洗]`、`[ACTION:殖民]`；
3. GCCZ runtime 可以选择附近己方士兵触发即时反应；
4. 士兵反应内容应是请示玩家，例如：
   - “大人，要不要只收这人的钱，还是扩大为全城搜掠？”
   - “若您真要下令血洗，请明示，我们才会执行。”
5. 士兵请示本身不触发结算；只有玩家下一轮明确命令士兵，且士兵直接回应服从，才触发破坏性标签。

### 5. 血洗后平民移动/反抗链路

触发血洗后：

- 多数普通平民：向场景预设藏身点、室内点、城门/逃离点移动；
- 少数平民：会反抗；
- 头人/要人、携械者、守卫/士兵类目标：更倾向反抗或至少不按普通平民逃跑处理；
- 儿童/受保护角色仍不得作为普通屠杀目标。
- “血洗不可逆”只表示不能把最终结算降回搜掠、宽恕或救济；血洗后仍可继续升级为屠民迁殖。
- 屠民迁殖也可以在一开始由玩家直接命令触发，不必先血洗。

实现建议：

- 独立区保留 profile，例如：
  - `SiegeMassacreInteractionProfile`
  - `ShouldCivilianResist(...)`
  - rout/escape distance constants
- 融合树 AF bridge 负责从 mission scene 读取：
  - tagged points；
  - current agent position；
  - team/formation；
  - path/ground projection；
  - actual `SetTargetPosition` / `SetScriptedPosition` 等 runtime 调用。

## 建议桥接点（融合树）

> 具体方法名以 AF v0.8.6 实际源码为准，不要按旧行号硬套。

- `ShoutBehavior.cs`
  - 只负责识别本轮回复是否“直接回应玩家”。
  - 把 `replyIsDirectPlayerResponse` 传给 GCCZ 后处理桥。
  - 不在这里写标签业务规则。

- `SiegeAiInterventionBehavior.cs`
  - 只做 active GCCZ stage gate、live mission agent 查询、调用 GCCZ policy、执行 side effects。
  - `TryProcessAiActionTags(...)` 之类入口应委托 GCCZ routing policy 判断标签是否可执行。
  - 对无效破坏性标签：剥离标签，必要时触发士兵请示，不结算。

- `RuleBehaviorPrompts.json`
  - 只合并 `siege_intervention_aftermath` 被动规则。
  - `TriggerKeywords` 保持空或被动；由 runtime 在 GCCZ scene 注入。
  - 不替换 AF 全量规则文件。

## 推荐独立区文件职责

- `SiegeActionTagCatalog.cs`
  - 标签别名、canonical order、regex。

- `SiegeActionRoutingPolicy.cs`
  - 判断标签批次是否包含破坏性动作。
  - 判断士兵执行型破坏性动作是否允许。
  - 判断是否需要士兵请示。

- `SiegePostprocessContextBuilder.cs`
  - 构造后处理运行时事实文本。
  - 明确“是否直接回应玩家本轮发言”。

- `SiegeRuntimePromptProfile.cs`
  - 士兵/平民在 GCCZ 场景中的身份覆写。
  - 阻断“士兵自认俘虏/旧守军”的错觉。
  - 保留士兵原文化/兵种知识来源，不让身份覆写清空 AF 知识库命中。

- `SiegeSoldierThinkingProfile.cs`
  - 士兵可见行为链路：现场事实 → 玩家命令权 → 当前处置状态 → 原文化/兵种口吻 → 情绪/人格分化 → 自然回复。
  - 明确同文化只影响情绪与语气，不阻断处置。
  - 明确血洗后仍可升级殖民，殖民也可开局直接触发。

- `SiegeCivilianThinkingProfile.cs`
  - 平民可见求生链路：战败事实 → 自身阶层 → 可用筹码 → 当前处置状态 → 求生反应 → 自然回复。
  - 普通平民/镇民/村民：保命、护家人、交少量钱粮、逃散或少数绝望反抗。
  - 商人/富户/市场摊主：装穷、讨价还价、交第纳尔/货物/账本/仓库钥匙买命。
  - 工匠/行会人/酒馆人员：用修甲、造箭、作坊、工具、学徒、消息和人脉证明自己还有用。
  - 要人/头人/本地名流：用名望、名册、粮仓、劝众集合、公开宽恕或盟誓换取保命和新秩序位置。
  - 帮派/灰色人物如出现，可投机、装忠、出卖别人藏财或试探新秩序，但不能替玩家宣布全城处置。
  - 平民、商人、工匠、要人和头人只能把玩家索财理解为局部 `抢钱`；不得把自己的求饶、转述或恐惧说成 `搜掠`、`血洗` 或 `殖民` 已执行。

- `SiegeDestructiveInquiryProfile.cs`
  - 士兵请示的事实文本、冷却、原因码。

- `SiegeMassacreInteractionProfile.cs`
  - 平民逃跑/反抗比例、距离、source code。

- `tests/AnimusForge.SiegeAftermathIntervention.Tests`
  - 不依赖 Bannerlord 的纯策略测试。

## 最小验证清单

1. GCCZ 独立测试：
   - `G:\AFMOD\.dotnet-sdk\dotnet.exe run --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\AnimusForge.SiegeAftermathIntervention.Tests.csproj`

2. 融合树构建：
   - 在 `G:\AFMOD\new-086` 中构建 AF v0.8.6 + GCCZ。
   - 先 1.3.x，再按需 1.4.5。

3. 游戏内测试点：
   - 玩家和平民谈抢钱/威胁：只能局部抢钱或引发士兵请示，不能直接搜掠/血洗/殖民。
   - 士兵和士兵闲聊提到血洗/搜掠：不触发结算。
   - 士兵主动请示是否血洗：不触发结算。
   - 玩家直接命令己方士兵搜掠/血洗/殖民，士兵直接回应服从：对应标签触发。
   - 血洗后多数平民跑向路点，少数/头人/携械者反抗。
   - 血洗后玩家继续命令屠民迁殖：允许触发殖民。
   - 同文化定居点：仍允许进入 GCCZ，并允许搜掠/血洗/殖民，只是士兵语气可更压抑。
   - 士兵不再自称俘虏、败兵、旧守军。

## 禁止事项

- 不要改 `G:\AFMOD\YM0.8.6\Mount-Blade-Bannerlord-AnimusForge-mod-main`。
- 不要把旧 GCCZ 大文件整块塞回 AF 文件。
- 不要让普通 AF 场景触发 GCCZ 标签。
- 不要让 NPC-to-NPC 自然聊天直接结算破坏性处置。
- 不要 hard-reset、force-push、删远端分支或直接推 main，除非用户明确要求。

## 接手顺序建议

1. 从 AF 0.8.6 源码 seed `G:\AFMOD\new-086`。
2. 从 `G:\AFMOD\GCCZ` 同步 `AnimusForge.SiegeAftermathIntervention` 独立源码到 `new-086`。
3. 在 `new-086` 中做最小 AF bridge：active gate、postprocess context、direct-player-response flag、side effect executor。
4. 合并 `siege_intervention_aftermath` 被动规则到 `new-086\AnimusForge\ModuleData\RuleBehaviorPrompts.json`。
5. 跑独立测试 + 构建。
6. 覆盖游戏模块前确认无 Bannerlord 进程，保留 DLL/PDB/ModuleData 备份。
7. 游戏内按最小验证清单测试。
