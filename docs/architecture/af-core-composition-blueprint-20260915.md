# 全范围收尾已启动（2026-09-15）

已发布前一候选10defeb4；新本地生产043b62b4完成共享正常Hero人设请求的主线程捕获/接受、独立预约冷却与重生UI边界，不新增调度队列。125检查/7有效故障和相邻联合回归通过，[代码位置与后续完整范围](../handoffs/2026-09-15-full-closeout-persona-handoff.md)。

仍按既有14职责和main矩阵推进：渠道准备全链→生命周期/记忆预算→内部双向服务+三渠道SDK→全主体对照/清理→最终同候选验收；小包通过不放行整体。制作组业务不重写，未恢复自动化或部署。

## 以下为原计划与历史实现

# 实施更新：Q1 历史子责任（2026-09-15）

`af754ab6` 完成双向历史主线程捕获/后台旧检索/主线程接受，复用现有 owner phase 和 Memory snapshot，无第二队列/存储。两个旧同步公开 Capture 保持；其余 persona/preprocess/lore/消息准备尚未完成，首次快照规模成本仍待验证。

[当前 HANDOFF](../handoffs/2026-09-15-courier-history-capture-handoff.md)。后续先收口剩余 prepare/生命周期与 B1，再落实内部双向服务及 Native/Scene/Courier 三渠道版本化提交；不因局部线程修复放行整体收尾。

## 以下为此前计划与实现记录

# 实施更新：请求资源所有权与最终API范围（2026-09-15）

73774a94将共用请求的CTS所有权提取为内部lease，隔离取消异常和完成/释放竞态；不增加第二套队列/注册器，原coordinator公开签名保持。[当前HANDOFF](../handoffs/2026-09-15-main-closeout-lifetime-handoff.md)。

用户已明确外部Native/Scene/Courier三渠道全部必交，现有只读Api.V1不算完成。完整Campaign/Mission生命周期与主线程捕获仍是独立门槛，不能由此次request lease代替。[按main主体对照的收尾矩阵](../phase8/af-core-main-closeout-matrix-20260915.md)。

## 以下为历史蓝图与进度

# 实施更新：内外快照依赖已分离（2026-09-15）

f07cb2a2完成现有Runtime内部状态/冻结快照与Api.V1投影分离，保持唯一Directory、原锁/状态与公开语义；CoreOnly无API源码编译和旧新DTO/停止/重载对照通过。[当前HANDOFF](../handoffs/2026-09-15-snapshot-boundary-handoff.md)。

此项只完成目录快照作用域，不代表统一Campaign/Mission生命周期已落地。后续仍依据真实owner的新档/读档/结束与晚回包资格建立接缝，不新造Host假完成。下方历史源码坐标按对应历史提交解释。

## 以下为此前蓝图/实现记录

# 实施更新：装配职责已接线（2026-09-15）

用户已授权开始拆分。955a6be3完成现有装配入口下的Campaign行为/模型清单与制作组目录声明提取，见[实现HANDOFF](../handoffs/2026-09-15-composition-extraction-handoff.md)。本表下面的61d57892坐标与“本轮不改C#”是设计时的历史事实，不再是当前状态。

已落地：唯一入口委托、原36/4装配顺序、无静态Campaign缓存、3组目录绑定策略独立。未落地：完整生命周期作用域、可执行状态/停止闭环、公共投影进一步解耦与各领域迁移；原蓝图的这些验收出口继续有效。

## 以下为原始蓝图（目标与不变量仍有效）

# AF 整体框架编排蓝图（2026-09-15）

> 本轮先确定框架如何组织与装配，暂不继续深复制细节或大范围业务搬迁。**这是设计蓝图，不是已经落地的完整Host。**
> 依据本地源码 `61d578926329ace61bf6b6ae43e12bf7d89b4696`；写本文前HEAD为16af548b。本轮不改C#、不改接口签名、不切默认、不部署/推送，自动化仍暂停。

## 1. 先分清两种“编排”

| 编排 | 负责什么 | 不负责什么 |
|---|---|---|
| 框架装配（Composition） | 谁创建模块、谁持有实例、依赖怎样注入、何时激活/停止、哪些能力可用 | Prompt正文、标签业务规则、记忆算法、模块玩法 |
| 一次对话的执行编排（Workflow） | 按明确阶段调用上下文、LLM、后处理、执行、记忆和展示，汇总真实结果 | 自己实现所有模块业务、创建全局服务、把队列成功当动作成功 |

两者不能合成一个新的万能Manager。先定装配边界，再把现有业务逐个接进去。

## 2. 总体结构（目标，不强制拆DLL）

```text
AnimusForge.Bootstrap.dll
└─ 按游戏版本只加载一个 AnimusForge.dll 实现
   ├─ 装配入口（Composition）
   │  ├─ 模块目录/依赖校验与能力说明
   │  ├─ Campaign / Mission / Session 生命周期作用域
   │  └─ 显式创建与注入、失败收口、停止与释放
   ├─ AF 主体模块（Core）
   │  ├─ Conversation：对话执行编排
   │  ├─ Prompt & Rules：上下文、话题、提示词
   │  ├─ LLM Gateway：网络协议、配置、重试与流式
   │  ├─ Actions：标签/计划/资格与真实结果回执
   │  ├─ Memory：历史、AFEF、摘要、恢复
   │  └─ Runtime：所属线程、排队、预算、过期请求隔离
   ├─ Host / Channel 适配
   │  ├─ Native / Scene / Courier
   │  └─ 游戏对象、展示/TTS、设置、存档兼容、开发工具
   ├─ 制作组 internal 接口与薄桥
   │  └─ 政策 / 宴会 / GCCZ 等自有业务模块
   └─ 对外 public 版本化 API
      ↑ 独立子 MOD DLL
```

**对子MOD的调用不强制经过全部制作组模块。** 对外API进入AF的受支持核心能力；只有使用已明确开放的制作组能力时才经过相应桥。两层接口独立，共用底层权威owner。

Composition可以引用具体实现来组装它们；核心模块不能反过来依赖Composition或扫描全局容器。初期用显式构造/工厂，不引入反射发现、通用ServiceLocator或每帧注册表查询。

## 3. 哪些已经有，哪些需要补

| 当前代码 | 已有职责 | 不能冒称已实现 |
|---|---|---|
| `ModuleFrameworkRuntime` | 3组typed adapter装配、目录校验和只读投影 | 完整Campaign/Mission生命周期、实际游戏可接单、任意热卸载 |
| `TeamModuleServices` | 三个无状态内部adapter实例 | 制作组全部业务迁移或所有调用都已经收口 |
| `LegacyInteractionPipelineComposition.Create` | 用现有ports构造真实pipeline/coordinator | 原Prompt/规则/后处理已经完全离开大类；其delegates仍调用原owner |
| `InteractionRequestCoordinator` | channel/session请求协调及档代保护 | 所有默认渠道、动作与记忆/展示生命周期已经全量统一 |
| `MemorySummaryDispatcher` | 真正独立的Memory线程队列、待办/预算/异常owner | 全局游戏调度器、深复制已分段、全部Memory模块化完成 |
| `Api.V1` | 版本/能力/目录只读查询 | 完整提交/取消/写入/注册SDK |

**演进现有装配入口，不并排新增第二套ModuleFrameworkRuntime。** 如果需要抽出Core composition或Campaign scope，让现有入口成为唯一委托方，并在同次接线中删除被替代的装配逻辑；不让两个入口重复创建owner。

## 4. 生命周期和状态归属

| 作用域 | 可以持有 | 创建/激活条件 | 停止/释放要求 |
|---|---|---|---|
| 实现程序集/目录级 | 不可变能力描述、无状态工厂、可共享基础设施 | Bootstrap已选唯一实现，注册与依赖检查完成 | 模组卸载释放自有可逆注册；不能持有某个档的Hero/Agent |
| Campaign级 | Memory/历史/summary状态、档代绑定服务、对话接入与领域owner适配 | 真实Campaign owner已建立，读档/新档状态达到可接受请求的边界 | 先拒收旧请求并退役未开始工作，再释放会话/订阅；不重放已开始副作用 |
| Mission级 | 场景参与者、接力/旁听上下文、展示/声音队列、Mission订阅 | 所属Mission可用且功能资格满足 | Mission结束后旧回包不可写入/播放；保留Campaign级记忆owner，不另建一份 |
| Session/Turn级 | 不可变请求快照、所选能力、关联身份、结果/错误回执 | channel/session/目标/档代有效 | 完成、拒绝、取消或过期均有可观察终态；取消不伪称网络一定已中断 |

注意：CampaignBehavior注册不等于存档加载完成。下一轮编码必须依据本项目实际注册/SyncData/加载和结束路径选取Hook，不能为了好看虚构引擎回调或擅自移动Saveable类型。

逻辑状态必须区分：**已登记、已装配、运行域可用、功能资格满足、停止/失败**。当前V1中Ready已有“adapter装配”的含义，不能在不版本化的情况下偷偷改解释。本轮不新增公开状态字段。

## 5. 启动与停止顺序

### 启动（依赖就绪后才激活）

1. Bootstrap选择唯一1.3/1.4实现，保持模块/程序集身份。
2. 现有装配入口建立静态目录、校验ID/依赖/契约；此时不读取未就绪Campaign对象、不发布“游戏可提交”。
3. 按所属作用域绑定Host线程/设置/诊断/存档适配；建立唯一核心owner实例。
4. 注入Memory查询/接受、LLM Gateway、Prompt/规则与Action执行端口，建立Conversation工作流；通过端口避免相互引用整套大类。
5. 创建Native/Scene/Courier适配器，绑定各自真实owner/session；Mission服务等到Mission作用域存在再激活。
6. 接入已启用的制作组贡献/薄桥；对外只发布真正支持的能力。未支持或尚未可用如实报告。

这是依赖阶段，不是要求立即把所有静态对象改成一张巨大的注册表。

### 停止（先关入口，后按依赖逆序释放）

拒绝新请求 → 标记当前域/请求过期 → 退役未开始工作 → 停止Mission/session展示和订阅 → 释放自身可逆资源 → 清理目录可用状态。

- 正在执行的游戏动作不能假装撤销；已发生结果按原owner规则收口，失败/部分成功显式回执。
- 模块失败只阻断真实依赖它的能力；无关AF流程继续。必需依赖缺失不能吞错装正常。
- 初始化中途失败仅回收自己已经获得的可逆资源；不可逆引擎注册、存档或副作用单列，不承诺任意热重启。

## 6. 一次对话的执行编排

```text
Native / Scene / Courier / 已开放API
  → 请求接入：档代、目标、session、busy与资格
  → 主线程捕获上下文/历史/模块贡献
  → 话题/规则选择与Prompt编排
  → LLM Gateway：正文/流式及原前后处理模型请求
  → 权威后处理：标签 → ActionPlan → 执行前资格重验
  → 原领域owner执行 → 真实成功/失败/部分结果回执
  → 唯一历史/AFEF接受者按事实写入
  → 渠道展示与通知收口
```

上图是责任顺序，不抹平实际渠道时序：

- **Native**：保留普通、流式、主动开场、窗口/目标变更语义；不能用opt-in演示链替换完整默认链。
- **Scene**：正文可先显示，再做完整后处理；接力、旁听、玩家输入去重、动作/事实唯一提交和发言队列不可省略。
- **Courier**：预生成不代表到达；只在原到达/执行条件满足时提交动作和事实，来回两个方向分别捕获/接受。
- **展示/TTS**：生成完成、动作/记忆完成、真实播放结束分开记录；不得用Task完成冒充播放完成。
- **已有写入者**：接入编排前先确定谁已经写事实；协调器不能收到结果后再写第二遍AFEF。

## 7. 模块之间如何连接

| 接缝 | 建议使用的契约形态 | 必须说明 |
|---|---|---|
| Composition → 实现 | 显式构造/窄工厂，owner作用域确定 | 创建数量、依赖、谁释放；不要到处new全局服务 |
| Channel → Conversation | typed请求/回执、目标和session身份 | busy/重复/过期、取消、实际状态，不暴露私有可变对象图 |
| Conversation → Prompt/Memory查询 | 只读上下文/快照及明确查询端口 | 所属线程、来源时点、限额、读档隔离 |
| Actions → 领域owner | typed执行命令/真实结果，不透传未授权raw标签 | 资格重验、部分成功、未知状态、不盲重试 |
| 结果 → Memory接受 | 关联身份、已发生事实、来源/档代 | 唯一写入者、重复/过期拒绝；不是所有写入都可事务回滚 |
| 制作组 → 主体 | internal背景/规则/能力贡献与薄桥 | 启用范围、优先级/冲突、owner、错误隔离；不复制制作组业务 |
| 子MOD → AF | public版本化DTO/能力API | 兼容、未知能力/版本、订阅释放、回调线程、缺依赖 |

只有真实consumer需要时才新增具体接口；不一次生成几十个空IService。已有能承担责任的契约优先沿用；仍公开的legacy面不能靠改internal静默破坏外部消费者。

## 8. 编排先行的实施顺序

本轮只完成蓝图。接下来编码先围绕装配而非继续局部深复制：

1. **装配现状表与作用域所有权**：列清实际创建点、静态实例、Tick/注册/释放、外部可见面；确认哪些共享、哪些必须按Campaign/Mission隔离。
2. **唯一装配入口接线**：在现有ModuleFrameworkRuntime演进，先接入已经完成且有真实owner的组件；不是把MemorySummaryDispatcher从每owner错误提升成全局singleton。
3. **最小可用生命周期**：明确目录就绪与运行域可用，绑定真实生命周期；补中途失败/重复初始化/停止后晚结果的组合测试。
4. **核心工作流端口装配**：沿现有完整pipeline及真实渠道接线，保持原规则/执行/记忆所有权；不切默认、不新增第二条缩水链来展示“框架能跑”。
5. **制作组/对外两层接口投影**：目录、能力和实际状态一致，公开范围D-A/D-B仍需明确，不擅自开放写入。
6. **编排骨架通过后继续职责迁移**：恢复原14类职责包，包含B1深复制/来源接受、B2渠道/Prompt/Actions、B3剩余大类和生命周期；所有原门槛仍保留。

这只是实施优先级变化，**没有把阶段8/B1改成DONE，也不解除真实游戏/旧档验收要求**。编排设计可先行；会影响生产默认链或释放时序的代码必须有对应回归与明确兼容范围。

## 9. 编排合格的出口

- 全部实例有唯一创建/释放者；无双重注册、双队列、双写、跨档全局可变引用。
- 构造依赖无环；核心不反向查询装配器或依赖渠道大类，跨模块只走受支持端口。
- 重复初始化、部分失败、缺依赖、Mission结束、读档/结束后晚结果均有明确行为；不影响无关模块。
- 基础能力缺失不能假Ready；公开既有字段含义/ABI不被悄悄重定义。
- 至少一条真实完整调用链通过；其余未接入明确标记，不能凭目录测试称全框架功能完成。
- 拆出的owner真接线后，同次删除被替代的创建/调度逻辑；未迁移业务不假删。

## 10. 当前源码定位与验证边界

下列坐标核对源码61d57892，均为入口/相关片段而非整类已完成标记：

| 路径:行号 | 当前事实 |
|---|---|
| `SubModule.cs:60–63` / `107–113` | 模组加载/卸载调用ModuleFrameworkRuntime |
| `SubModule.cs:656–668` | InitializeGameStarter登记MyBehavior/Shout/Courier等CampaignBehavior |
| `SubModule.cs:755–782` | 当前ApplicationTick入口；不能凭新增目录替代全部Tick生命周期 |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:22–80` | 显式三组adapter目录装配与Stopped状态，不读取Campaign |
| `Refactor/Modules/TeamModuleServices.cs:5–10` | 三个静态无状态typed桥 |
| `Refactor/Adapters/LegacyInteractionPipelineComposition.cs:75–114` | 用原ports组合pipeline/coordinator，原owner仍运行 |
| `src/modules/AF.Module.Conversation/Internal/InteractionRequestCoordinator.cs:15–31` | 请求协调器持有in-flight/代际依赖，不是全局模块Host |
| `MyBehavior.MemorySummaryMainThread.cs:18–44` | 已提取dispatcher的每owner惰性发布与游戏host |
| `Api/V1/AfApi.cs:13–27` | 当前CatalogRead可用，提交/动作/写入/注册未开放 |

本轮仅核查源码定位、文档链接、生产源码未变及保护文件hash；不重跑无关构建、不伪造运行验收。已执行的生产验证见[最近模块交接](../handoffs/2026-09-15-memory-dispatch-owner-handoff.md)。后续14类职责与删旧标准见[职责计划](../phase8/af-core-responsibility-decomposition-plan-20260915.md)。
