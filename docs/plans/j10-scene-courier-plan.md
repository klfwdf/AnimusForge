# J10 Scene / Courier 渠道会话拆分实施计划

> 状态：`J10_OFFLINE_VERIFIED`（2026-09-21）
> 依赖：J07、J08、J09 已完成必要离线验收；启动基线 `d1709717`。
> 本文只授权 Scene/Courier 渠道会话 owner、生命周期和提交时点的整理；不授权 J11–J14、制作组玩法、公开 API 开放、默认切换、部署或存档迁移。

## 0. 最终施工回执

| 包 | 状态 | 产品提交 / 证据 |
| --- | --- | --- |
| G0 | DONE | `2c6530f8` 冻结有限责任矩阵、退出门与非目标 |
| J10a Scene | OFFLINE_VERIFIED | `03c08ac9`、`76b5a429`、`64e438c6`、`a4006d5c`、`2631f33c`、`c1f5aa6a`、`cbf7f453`：audience/request/pending/speech/group/relay/passive/reaction 真实 owner 与消费者闭合 |
| J10b Courier preparation/transport/session | DONE | `bdf58283` 起的 Courier 系列切片：Prompt run、generation、transport、delivery、registry/creation、runtime/proactive/letter inventory 归位；回滚链见主台账历史回执 |
| J10b Prompt message/fact | DONE | `14283c3f`：22 个声明原样迁移，Prompt 550/76、liveness 59/16、Debug 构建通过 |
| J10b domain commit/reply wait | DONE | `f6c95ac3`：6+4 个声明原样迁移；到达提交、session eligibility、Economy reservation、history fan-out 和 wait lock owner 唯一 |
| J10c 整包 | OFFLINE_VERIFIED | `7d70f528` 及最终文档提交：六构建、1060 API、142/168 persistence、Scene/Courier/Native/shared/Bridge/Phase8、有效负例和 392 锚点地图通过 |

详细结果与 NOT-RUN 边界见 `docs/handoffs/2026-09-21-j10-scene-courier-offline-closeout.md`。真实游戏、旧档、provider、音频和性能不因离线闭合而提升；J11–J14 未在本包偷渡。

## 1. 完成定义

J10 只在以下条件全部满足后标记 `J10_OFFLINE_VERIFIED`：

1. Scene 的 audience scope、group/relay/passive/reaction 会话身份、玩家输入去重、pending AFEF、speech queue 与 relay 完成拥有清晰且唯一的渠道 owner，并由真实生产入口消费。
2. Courier 的 prompt-run/session identity、预生成、运输/到达、来信/回复和 retry 归属清晰；预生成不执行动作，只有到达或回复的 `DeliveryApplied` 阶段可以提交。
3. 两渠道复用 J04–J09 的 Prompt、LLM、Tags/Plan/Execute/Receipts，不复制 parser、执行器、历史或 AFEF 提交链。
4. 正常回归、至少一个有效负向变异、双版本构建、API/存档/代码地图门禁通过；LIVE/SAVE/provider/音频/帧耗时继续独立标记 `NOT-RUN`。

## 2. 责任矩阵

| 责任包 | 当前生产 owner / 入口 | J10 目标 owner | 必须保留的语义 | 退出门 |
| --- | --- | --- | --- | --- |
| G0 | `ShoutBehavior*`、`SceneShoutConversationScope.cs`、`CourierDeliveryBehavior*` | 本文与代码范围图 | 只冻结真实消费者、状态和时序，不重开 J07–J09 | 没有未归类的 Scene/Courier 活跃状态；历史路径不当待办 |
| J10a1 Scene audience scope | `SceneShoutConversationScope.cs`；`ShoutBehavior.TryBuildSceneShoutConversationScope` | `src/modules/AF.Module.Conversation/Channels/Scene/SceneShoutConversationScope.cs` | 稳定合并顺序、Agent 引用/身份、Mission/epoch、距离来源、活跃复核 | 原类原样归位；生产消费者唯一；正常和身份/epoch 负例通过 |
| J10a2 Scene conversation session | `ShoutBehavior` group/relay/passive/reaction、`ScenePostprocess` | `Channels/Scene` 的窄 owner/port | 正文先显示；玩家输入只记一次；旁听/距离不丢；pending AFEF 只消费一次 | 四链共享捕获的 session identity；旧完成不能发布到新会话 |
| J10a3 Scene speech/relay completion | speech queue、relay publish 调用点 | `Channels/Scene` 完成 owner | `mood → GCCZ → direct → follow/speech → relay publish`；queued action/speech 完成后才发布 | 可等待真实终态；取消/迟到/异常不伪成功、不双写 |
| J10b1 Courier prompt run | `CourierDeliveryBehavior.PromptPreparation/PromptSchedule/PreparationAdmission.cs` | `Channels/Courier` prompt-run/session owner | capture 后 worker 只读 detached 输入；跨 await 重验 owner/generation/source | 旧 run 不能完成或清理新 run；源码只有一个 reservation owner |
| J10b2 Courier delivery/retry | transport/pregeneration/arrival/letter/inbound/retry partials | `Channels/Courier` delivery owner/ports | 预生成仅产文本/候选；到达或回复后才 commit；owner-started 异常为 unknown | 旧 retry/迟到 completion 不改变新 session；不提前执行动作 |
| J10c | 三渠道对齐文档及相关 runner | 代码范围图、台账、当前 HANDOFF | Native 只做受影响回归；J09 action-only boundary 不被替换 | 全套离线门禁通过并记录保留项/NOT-RUN |

## 3. 不变量

1. 不新增、改名或删除 `SyncData` key/保存类型，不改程序集名和公开 V1 ABI。
2. 游戏对象读取、资格和状态修改留所属主线程；worker 只接 detached 输入。
3. 每次跨 `await` 重新核对 runtime generation、scene/courier session、epoch、subject/target、request identity。
4. 票据只能由创建者释放；旧请求不得清理新请求的槽位或终态。
5. owner 开始后异常可能已修改状态，记 `unknown` 且不可自动重试。
6. Scene/Courier 各自只有一个历史/AFEF/动作提交点；不得新旧双执行。
7. Scene 先统一后处理再动作派发；Courier 预生成不得提前提交动作。
8. Team Policy、Gathering、GCCZ 只保留现有薄接缝，玩法不迁入主体。
9. `ShoutBehavior.cs` 如需修改必须精确字节写入并保持 CRLF/无 BOM；优先移动或新增 partial/src owner。
10. 物理路径、文件数和主类行数不是完成证据；真实 owner、消费者、失败语义和验证才是。

## 4. 有限施工顺序

### G0：一次性冻结责任

- 记录 Scene/Courier 活跃状态字段、创建/认领/完成/取消者、线程和持久化边界。
- 记录真实消费者、反射/Harmony/源码提取测试和 `.csproj` Compile 输入。
- 标出哪些历史文档只用于取证，不随物理路径重写。

### J10a：Scene

1. 先原样归位已独立的 audience scope，保持 namespace/可见性/算法不变并接真实消费者。
2. 再按一条真实链一次提取一个 owner：会话身份与 completion → group/relay → passive/reaction → pending AFEF/玩家输入去重。
3. 最后收拢 speech queue/relay 终态，使独立调用方可等待真实结果；不借此开放 J14 public submit。

### J10b：Courier

1. 先归位 prompt-run/session reservation，保留 `ConditionalWeakTable` 所有权和 source-change 语义。**已由 `bdf58283` 完成。**
2. 分离预生成候选与到达/回复提交；把 transport、arrival、letter/inbound、retry 的 session identity 接到同一 owner。
3. 删除仅在替代实现已接线且无 ABI/反射/Harmony/存档责任的旧路径；保留项列理由。

### J10c：整包验收与交接

- 对齐 `docs/free_conversation_scene_shout_alignment.md`、范围图、代码地图、主台账和根 HANDOFF。
- 清查重复 session gate、重复历史/AFEF/动作提交、预生成提交和无消费者接口。
- 生成本地制作组简版 `.tmp/af-j10-team-handoff-20260921.md`，不纳入 Git。

## 5. 验证矩阵

### 每个责任包

- 受影响的 Scene/Courier 正常 runner。
- 至少一个编译成功、命中具名场景并因预期断言失败的变异。
- 真实生产消费者/Compile 输入与重复 owner 搜索。
- `git diff --check` 和相关 Debug 构建。

### J10 最终候选

- Scene：Postprocess parity/queue/lifetime/request lifetime。
- Courier：Prompt preparation/liveness、owner phase、inbound completion、postprocess owner、commit outcome。
- 共享：InteractionPipeline、J09 default-channel wiring、Actions/Economy/Duel 与受影响 Native。
- Debug/Release × Bannerlord 1.3/1.4 + Bootstrap；四 DLL API/metadata；Persistence/Profile/Chunk/Identity；Bridge/Phase8；代码地图 recorded/working-tree。

## 6. 防止兜圈

每个包在“真实 owner 接线、正常回归、有效负例、必要构建、清理、提交”完成后立即进入下一包。不得因主类仍大、还能抽 helper、历史文档旧路径、Legacy 命名或可增加相似测试而停留。只有新的可复现失败、相关源码变化或本包退出门未满足才重开。

## 7. 提交与回滚

每个包独立本地提交并可用定向 `git revert` 回滚；不用 reset/rebase/force push。自动化不 push、部署、Stage、Package、写游戏/存档或修改其他工作树。远端分叉或他人修改同一 owner 时停止相关写入并报告。
