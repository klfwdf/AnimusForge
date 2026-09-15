# AF 全范围收尾：并行整合 HANDOFF（2026-09-16）

> 发布更新：本候选代码与本文已于本轮普通快进推送到`origin/codex/af-main-refactor-continuation-20260831`，远端核实提交`6538cc360188b660e697b72bb6ff773b8a8660c9`。本说明是后续文档追加，不改生产源码6e419f6d。fresh fetch发现main新增`0a641aab`，尚未合并/验收；下文“未推送/仅本地”描述保留为发布前记录，当前发布状态以本段为准。整体未结项，未部署游戏。


## 结论先看

**阶段 8 仍 ACTIVE。这次四个责任包已合并并通过离线联合验证，但整个主体重构没有完成。**

不是“代码全部做完，只差游戏验收”：共享 Prompt 线程边界、记忆硬预算、Scene/Courier 公开 SDK、完整内部反向服务及主体大类拆薄仍有实质代码任务。不能把现在标为“旧代码全部删除、功能完美复现、零 BUG”。政策/宴会/GCCZ 玩法不在本任务重构范围，既有 AF 侧桥接不得破坏。

- 唯一工作区：`G:/AFMOD/AF-REFACTOR`。
- 本地分支：`codex/af-framework-skill-delivery-20260911`。
- 本候选生产源码：`6e419f6d17859fc49fef538e8a2ea5acf922deb3`；以下代码位置均绑定此提交。
- 对照 main：`437925b856fae76b4e9ee207e96ba048f35d5a67`，不是浮动 main。
- 最后已核实推送仍为 `10defeb4976f3ffa096a77e847fba254308f6aba`，远端专用分支 `codex/af-main-refactor-continuation-20260831`。本轮未推送，也未重新查询远端新状态。
- 未部署游戏、操作存档、切换默认入口、恢复自动化；原三份用户文档的字节哈希保持。

## 本次实际完成了什么

| 责任 | 实际改变 | 覆盖边界 |
|---|---|---|
| 记忆运行 | `79bf1288`：独立run/lease贯穿计划、capture、重试、解析、Apply/Mark、通知；同代重启的旧finally和旧结果不干扰新run | 不改变摘要文本、排序、RPM、保存身份；不是完整M2算法拆分 |
| 记忆性能 | `b0a20176`：UTF16指纹改为固定缓冲分块编码，原/新13组真实源digest相同 | 微基准13组中位耗时观察下降11.7%–44.4%、分配不变；不是游戏帧保证，也未解决全量clone/最终绑定 |
| Native内外接口 | `67fcb3ff`：内部服务与Api.V1分层；真实owner完成回执、去重/容量/开始前取消、epoch/revision隔离、显式enum投影 | Native已接；Scene/Courier仍NotSupported；不是新的一条缩水LLM管线 |
| Courier准备 | `6e419f6d`：双向最终消息组装回主线程；实际Start级reservation；源变更走原失败流程，避免卡等待或重放旧标签 | 原消息组装尾部/消息builder与main保持；共享routing与全部运输/retry生命周期未完成 |

### 互审确实揪出的缺陷

1. 新Native API原先只捕获档代：A会话排队、结束并切B后仍可能把动作交给B。现在入队前捕获epoch并在主线程claim前重验；原UI先完成另一轮时也通过presentation revision拒绝旧API请求。
2. Courier初版只在输入变化时返回null，却留下Started=true，真实tick无法继续；已通过原Start/Begin/Prepare/Fail/tick链复现并修复。
3. 原Failure会推进信使，若遗留旧reply/tag可能重放；当前run源失效时先清正文/标签并标记已消费，再由原Failure推进。
4. V1直接cast内部enum不稳定；现为显式映射，内部三enum重排后41项调用检查保持通过。

## 稳定接口的准确承诺

```text
AnimusForge.dll
├─ AF主体真实owner：对话/LLM/Prompt/标签/记忆/调度
├─ 内部模块层：既有TeamModulePorts + CoreDialogueServices
└─ 对外层：Api.V1 + 独立显式投影
      ↑ 独立子MOD DLL
```

- 内部不依赖Api源码，CoreOnly编译通过；外部程序集不能直接调用内部服务。
- Native复用当前AF对话，不支持任意指定NPC。后台调用只绑定owner和纯运行stamp；**Hero等目标在主线程准入时解析**，不是调用瞬间的Hero快照。同epoch下准入前目标变化仍按准入时目标决定；要求严格绑定所选NPC的后续接口应由主线程签发不透明context票据。
- `Completed`代表原动作/必要记忆尾部确实给回执，不代表所有标签均获游戏资格批准，也不是TTS播放结束。已开始但无完整回执为UnknownAfterStart，不允许自动换ID重试。
- 同client+同ID+同文本复用；同ID不同文本拒绝。每client最多128有效ID，不自动淘汰造成二次动作；dispose只取消未开始请求，不撤销已执行效果。
- 目录Ready与能力Available不代表当前游戏可执行或实机已验收。详见[公开API指南](../architecture/af-public-api-guide-v1.md)和[内部指南](../architecture/af-internal-module-guide-v1.md)。

## 验证证据与限度

| 验证层 | 结果 | 不代表什么 |
|---|---|---|
| 当前六Stage | Debug/Release各1.3、1.4、Bootstrap通过；使用原脚本，仅项目内Stage | 不代表游戏部署/加载 |
| 实际DLL接口 | 4个实现DLL、1056项元数据检查；公开兼容与内部可见性保持 | 不代表独立子MOD在游戏中实际加载 |
| Native实际入口链 | 41项、8有效故障；enum重排41项；原44/46/184/111保持 | 底层网络、游戏动作/存储为声明的fixture |
| Courier实际准备 | 252项/58场景；liveness59项/16场景；5+4有效故障 | 未完整执行真实运输/游戏导航 |
| 记忆 | run owner47；实际业务39（旧36保留、新3旧红）；完整terminal95；capture116、writers238、planning24、dispatcher37、sealing88 | 非LIVE/SAVE，不是硬帧预算 |
| 字符与原文 | 原/新各65808检查；13真实源SHA一致；3故障、6inverse守卫 | 合成微基准，不代表游戏卡顿已消除 |
| 邻接回归 | 历史852/Native27；三渠道132；人设169；Courier历史122/后处理39/回执34；内部ports308+3故障；生命周期36+12故障 | 不等于main全部主体功能的实机复现 |
| main保存身份 | 146 SyncData键、36行为保持；原ModuleId/Bootstrap单模块加载形态保持 | 不代替真实旧存档加载 |
| 1.4生产DLL | 真实Courier Host回放及不确定回执不重试通过 | fixture端口，不是Campaign/Mission验收 |

最终有效日志在 `.tmp/parallel-closeout-20260916` 的`*-final.log`及审计明确列出的邻接日志；首轮无final的六Stage/API是互审修复前证据，不用于当前候选放行。旧红/故障只计编译成功后的行为失败；早期fixture/工具错误单独保留，不计验收数字。

[同候选审计JSON](../audits/2026-09-16-parallel-closeout-verification.json)绑定源码、产物和证据哈希。[142点地图](../architecture/af-framework-code-map.json)校验记录提交与工作树两种模式；地图只负责导航，不证明整文件完成。

## 核实后的代码位置

所有行号以源码 `6e419f6d17859fc49fef538e8a2ea5acf922deb3` 为准；更改后按符号重定位，不能把整文件当成已重写。

| 文件 / 一基行号 | 符号 | 已覆盖与未覆盖责任 |
|---|---|---|
| `MyBehavior.cs:4909–5039` | `private async Task ProcessMemorySummaryQueueAsync` | 同一个run贯穿计划、网络批次和每次接受；摘要算法尚未独立owner化 |
| `Refactor/Runtime/MemorySummaryRunOwner.cs:8–29` | `internal sealed class MemorySummaryRunOwner` | 唯一运行授权，旧finally不清新run |
| `MyBehavior.MemorySummaryRun.cs:13–17` | `private Task<bool> RunMemorySummaryRunPhaseAsync` | 仅摘要scope，保留原dispatcher与异常传播 |
| `MyBehavior.MemorySummaryInput.cs:358–435` | `private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync` | capture/retry/parse显式run；原freshness校验保持 |
| `MyBehavior.MemorySummaryPlanning.cs:160–220` | `private async Task<MemorySummaryPlan> BuildMemorySummaryPlanAsync` | 各计划切片持原run，不在旧scope重启 |
| `Refactor/Runtime/MemorySourceFingerprintWriter.cs:36–63` | `internal void Write` | 固定缓冲UTF16编码，仍O(chars) |
| `CourierDeliveryBehavior.PromptPreparation.cs:26–34` | `private CourierPromptRun BeginCourierPromptRun` | 真实Start级reservation，弱表不修改存档 |
| `CourierDeliveryBehavior.PromptPreparation.cs:46–67` | `private void CompleteCourierPromptSourceChanged` | 当前源失效原Fail推进，清理旧reply/tag避免重放 |
| `CourierDeliveryBehavior.PromptPreparation.cs:156–191` | `private async Task<T> PrepareCourierPromptRequestAsync<T>` | 主线程capture→原routing后台→主线程最终组装 |
| `CourierDeliveryBehavior.cs:4284–4300` | `private void StartCourierReplyGeneration` | 出信回信原Start接reservation |
| `CourierDeliveryBehavior.cs:4302–4314` | `private void StartInboundLetterGeneration` | 入站原Start接reservation |
| `ShoutBehavior.ModuleNativeSubmission.cs:10–22` | `internal static void SubmitModuleNativeDialogue` | worker只捕获owner/gen/epoch/revision，目标在原main准入解析 |
| `ShoutBehavior.NativeCompletion.cs:75–122` | `private string CompleteNativeConversationReplyOnMainThread` | 唯一真实完成回执，不用错误字符串推测成功 |
| `Refactor/Modules/CoreDialogueClient.cs:10–78` | `internal sealed class CoreDialogueClient` | 内部消费者隔离与有界去重，不淘汰旧ID重放 |
| `Api/V1/AfDialogueClient.cs:53–64` | `public sealed class AfDialogueClient` | 外部Native submit/result/cancel投影，未开放Scene/Courier |
| `Api/Internal/AfV1DialogueProjection.cs:7–39` | `internal static class AfV1DialogueProjection` | 公开协议显式映射，不绑定内部枚举数值 |

## 对照main：主体大类还没有拆净

| 文件 | 固定main行数 | 当前行数 |
|---|---:|---:|
| MyBehavior.cs | 59022 | 58666 |
| ShoutBehavior.cs | 40109 | 39692 |
| CourierDeliveryBehavior.cs | 10675 | 10514 |
| SubModule.cs | 1077 | 952 |

行数不是功能进度百分比，也不能把partial搬文件当责任已分离。当前仍保留大量真实业务、Live适配、Saveable/ABI和尚未迁移调用。只能删除已有真实替代owner与回归证据的旧实现；本轮移除了共享摘要busy布尔值和Courier主文件的两个旧混合builder体，同步旧入口仍有实际消费者必须保留。

## 后续到真正收尾的顺序

1. **共享Prompt/线程闭环**：盘点770行共享Build与所有Native/Scene/Courier实际消费者；主线程冻结资格/角色/资产，后台规则/lore计算，主线程重新验证并提交必要粘连状态。不得整体搬网络到主线程，也不得只包Task.Run。保留Scene多人接力、旁听、输入去重和唯一后处理。Promoted companion生成仍单列。
2. **完整请求寿命**：迁移Courier旧retry按钮按sessionId操作旧request的责任；Start reservation不等于整个运输/最终commit授权。追踪旧按钮跨reset/替换与现有后台session字段读取。不要用取消伪造已执行动作回滚。
3. **Scene/Courier SDK**：Scene从真实group/reply链取得全部参与者/接力/旁听终态，不把启动Task当完成；Courier明确运输/预生成/送达commit/入出站的事件和结果，不提前交付动作。沿内部服务复用同一owner，分别形成版本化DTO与独立消费者回归，再开放能力。
4. **记忆真正有界**：先收敛所有原地字段/嵌套List写者、编辑/normalize/merge/恢复/删除/加载，形成唯一source authority/immutable publication；之后才能用O(1)版本绑定代替最终全量digest并分片处理。不能只给Save加revision或只检查List版本。当前单条200万字符仍观察到约13ms raw digest，未解决。
5. **主体大类按责任迁出**：依现有main矩阵/14职责计划处理M1/M2/M3和其余核心owner；清单区分真正业务owner、引擎适配、存档ABI保留、外域保留。完整内部反向服务逐实际消费者接线，禁止装饰接口或反射热扫描。
6. **最终放行**：同候选六Stage/旧编译客户端/回放，再在明确授权与备份下验证Campaign/Mission、旧档、live Economy/AFEF和独立子MOD加载。现有其他成员对旧候选的反馈不替代此提交证据。只有这些与代码迁移均完成，才讨论删最后兼容路径、默认切换和最终打包。

## 回滚、协作和本地交接

- 回滚按反向顺序使用focused revert：Courier `6e419f6d` → 指纹 `b0a20176` → Native接口 `67fcb3ff` → 记忆run `79bf1288`。不要hard-reset、覆盖其他作者或强推；先评估当前调用依赖和保留测试。
- 本候选上一共同起点为`155f1b7a`（生产背景`51844800`）；意图/互审台账提交保留审计。
- 本地简明版：`.tmp/parallel-closeout-20260916/team-handoff.md`，不作为仓库依赖，也未发QQ。
- 受保护：`docs/handoffs/2026-09-06-integrated-phase8-handoff.md`、`docs/handoffs/2026-09-06-team-brief.md`原有未提交改动未变；`docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md`本地专用继续不上传。
- 新线程先读根HANDOFF、唯一执行台账、本文和双Skill，再核Git/产物/当前代码；不用旧历史“已完成”描述覆盖本次明确未完成事项。
