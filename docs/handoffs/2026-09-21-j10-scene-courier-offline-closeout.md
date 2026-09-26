# J10 Scene / Courier 离线收口 HANDOFF（2026-09-21）

## 结论

**J10 已达到 `OFFLINE_VERIFIED`。** Scene 的 audience/request/pending AFEF/speech queue/group/relay/passive/reaction，及 Courier 的 prompt、generation、transport、session、arrival/delivery、letter/inbound/retry、domain commit、reply wait 均已归入各自渠道 owner，并由原生产 partial class 的真实消费者继续调用。

这不是实机或发布验收：真实 Campaign/Mission、旧 SAVE、付费 provider、live Economy/外交、TTS/audio 和真实帧/网络性能仍是 `NOT-RUN`；本轮未 Stage、Deploy、Package，也未改游戏目录或存档。

## 最后两个 Courier 责任包

| 责任 | 当前生产位置 | 保留语义 |
| --- | --- | --- |
| Prompt message / fact | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptMessages.cs:41,444` | 回信/来信 message、memory role、地点、运输事实、货物估值和 payload 摘要原样迁移；没有第二套 Prompt 或新语法 |
| 到达后领域提交 | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs:41,167,215,289` | `DeliveryApplied` 后才执行；session/target/request identity 重验；Economy one-shot reservation；历史/AFEF/notoriety 只写一次 |
| 回信等待与时间锁 | `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.ReplyWait.cs:41,73,102,143` | 第一个 waiter 捕获旧时间模式/锁；仍有 active waiter 时不释放；最后一个完成才恢复 |

`14283c3f` 原样迁移 22 个 Prompt/fact 方法；`f6c95ac3` 原样迁移 6 个 domain commit/history 方法和 4 个 reply-wait 方法。根 `CourierDeliveryBehavior.cs` 不再含这些声明，生产合并源码中每个声明恰好一份。

## J10 后的物理结构（只作导航）

- `src/modules/AF.Module.Conversation/Channels/Scene/`：6 个源码文件。
- `src/modules/AF.Module.Conversation/Channels/Courier/`：20 个源码文件。
- 根 `CourierDeliveryBehavior.cs`：J09 的 10,514 行降至 3,477 行；保留 DTO/SyncData/Harmony/地图与跨域宿主适配。
- 根 `ShoutBehavior.cs`：J09 的 39,190 行降至 37,062 行；Scene live Agent、TTS/audio、movement、游戏线程副作用和其他渠道/领域宿主仍有明确保留理由。

行数不是完成门槛；完成依据是唯一 owner、真实消费者、时序/失败语义和可执行证据。Team Policy、Gathering、GCCZ 玩法没有迁入主体，只保留已有 typed port 调用。

## 最终离线证据

### 生产构建与 API / 存档

- Debug / Release × Bannerlord 1.3、1.4、Bootstrap：六构建全部成功，均 0 warning / 0 error；引用为 1.3.15.110062、1.4.6.115628，SDK 8.0.422。
- 四个实际实现 DLL：1060 项 API/metadata 断言通过；public V1、legacy memory ABI 不变，内部 ports 仍不可由外部访问。
- Persistence/Profile：142 literal keys、168 typed bindings、13 chunked keys、44 flattened dictionary keys 通过。
- Chunk replay 8 项、PersistenceIdentity 审计工具契约 5 项通过。唯一 Scene SyncData key 仍为 `_sceneHeroRevisitDays_v1`；只更新拆分后两处源码坐标 10813/10816，没有新增、删除或改型。

### Scene / Courier / Native / shared

- Scene：postprocess parity 71；queue 37；request lifetime 30；scope 5；pending AFEF 5；speech queue 6。
- Courier：Prompt 550/76；liveness 59/16；History 122/30；owner phase 16；postprocess 39；inbound contract；delivery/proactive/letter/route/session；domain commit 32。
- Shared：ChannelCutover 132；J09 三渠道 wiring 25；InteractionPipeline 全套；GiveAsset 80,562。
- Native 受影响回归：Action 91、Admission 44、Completion 184。
- 内部制作组 ports：13 个方法、31 个真实调用点的 receiver/参数顺序对照；308 项 source-linked 行为断言和 3 个有效变异通过。
- Bridge：16 bindings（12 wired / 4 declared-only）、23 单测、fixture 10、runtime isolation 12；Phase8 readiness 共 73 单测通过。
- 392 锚点代码地图 recorded / working-tree 两模式通过，绑定产品提交 `f6c95ac37b7bdc21392c14627f7e529437465a57`。

### 有效负例

- Courier domain：删除 `DeliveryApplied` 门、提前释放 reply-wait 均命中具名断言。
- Courier commit：4 个编译成功的 effect/终态/诊断变异全部运行期失败。
- J09 Courier pre-delivery commit 变异被时序断言拒绝。
- TeamModule ports：资格反转、Siege selected 反转、参数交换 3 个变异被运行期断言拒绝。

## 验收工具修正

1. `tools/NativeConversationAdmissionTests/turn_extraction.py` 不再把整个 37k 行 `ShoutBehavior.cs` 当 Native 夹具。它仍严格验证 Native 入口和四阶段算法/参数/顺序，但 Scene 的合法物理搬迁由 Scene 自己的精确迁移和行为套件负责。
2. `tools/TeamModulePortParityTests/run.py` 不再被不相关的 Memory/GameLifetime fixture hash 前置阻断；现在直接比较基线与当前 13 个 typed-port call 的完整 receiver、参数顺序和调用数，并单独验证 ModuleFramework 生命周期。没有刷新旧 hash 或删除行为断言来“做绿”。
3. `GiveAssetTagCodec.StressTests`、Duel replay 源码检查和 Phase8 entry inventory 已跟随真实 Scene/Courier partial 路径，未恢复旧实现。

## 清理与保留

- 相关目录无冲突标记、TODO/HACK/TEMP、重复 owner 或新旧双执行路径。
- Prompt parser、ActionPlan、Action executor、memory/AFEF committer 继续复用 J04–J09 owner，未在 Scene/Courier 复制。
- `CourierDeliveryBehavior.cs` 中仍保留的 DTO、SyncData key/type、Harmony patch、Map/UI/游戏对象 adapter 有活动消费者或 ABI/存档责任，不按文件行数盲删。
- `ShoutBehavior.cs` 中 live Agent、TTS、movement、History/Memory/动作副作用仍是所属线程 host adapter；J10 不把 J11–J14 或公开 Scene/Courier submit 偷渡进来。

## Git、回滚和下一步

- 最后产品包：`14283c3f`（Prompt/fact）、`f6c95ac3`（domain commit/reply wait）。
- 验收工具/地图包：`7d70f528`。
- 最新 J10 文档提交以本文件所在提交为准。
- 回滚使用定向 `git revert`，优先顺序为文档/验收包 → `f6c95ac3` → `14283c3f`；更早 Scene/Courier 包见 `docs/plans/j10-scene-courier-plan.md`，不得 reset/rebase/强推。
- 下一阶段只先制定 J11 计划：盘点制作组内部 Policy/Gathering/GCCZ 等 typed port 与薄桥，**不迁玩法、不改默认路径、不开放 J14 public submit**。

## 未运行 / 不可冒充

- `ProductionDuelOutcomeReplayTests` 与 `PhaseEightParityReplayTests` 的实际 DLL 回放依赖 `single_module_stage`；本轮明确禁止 Stage，因此未运行，不用旧 Stage 冒充当前产品证据。
- 未运行真实 Bannerlord Campaign/Mission、旧档 round-trip、真实 provider、live Economy/外交、实际子 MOD CLR 加载、音频播放和帧/网络性能。
