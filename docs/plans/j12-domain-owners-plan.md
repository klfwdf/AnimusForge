# J12 Economy / Diplomacy / WorldMap 实施计划

> 状态：`J12_REOPENED_PARTIAL`（2026-09-21）；审查确认此前把三个完整文件的 R100 目录迁移误写成职责拆分完成。J12a、J12b1 保持已验证；J12b2–b4 与 J12c1–c4 按原退出门继续施工。不是实机、旧档、Stage 或发布完成。
> 规划基线：`60499d44b3b2b97e5f35b32364a8e819ee9b4768`；J11 产品 `cdbd077a`，真实 Campaign 入口验收修复 `60499d44` 已推送。
> 计划现已进入实施；第 0 节表格与主台账记录实际完成结果，其余章节仍是后续 J12b–J12d 的施工约束。本文件不授权自动化、推送、Stage、部署、打包或存档操作。


## 0. 当前实施回执

| 包 | 状态 | 当前结果 |
| --- | --- | --- |
| G0 | DONE | fetch 后目标远端未领先；SDK 8.0.422、1.3.15/1.4.6 引用和原构建 wrapper 核对；`ProductionReward` 改为显式 SDK 参数 |
| J12a1.1 compatibility + projection | DONE | `3f2c454e`（测试输出忽略 `9e8e10e8`）：3 个既有 public boundary 原样归位；Debt/Trust 保持主线程 capture/normalize，detached projection 接真实消费者 |
| J12a1 剩余 Reward/Loan capture | DONE | `7f2fffba`：Hero/merchant 授权快照、完整候选和可见 Prompt capture 8 个声明逐声明原样归 `Projection`；ProductionReward 11 接真实新路径 |
| J12a2 replay/authorization/batch owner | DONE | `07feb572`：三个完整 replay partial 与 11 个 live authorization 方法归位；`deb421ae`：共享 batch owner 统一 null/reject/applied/partial/unknown/fact retention，三个 step delegate 是唯一游戏 mutation host；删除零调用旧 resolver |
| J12a3.1 Trust 规则 + Debt 归一 | DONE | `54b55aa3`：Trust clamp/十级语义/提示指南归 `EconomyTrustPolicy`；旧账迁移、line clamp、aggregate/date 重建和 note 归一归 `EconomyDebtNormalizationPolicy`；原 nested save schema、SyncData 和 wrapper 保持 |
| J12a3.2 Debt schedule / Quest / ledger / daily | DONE | `3d2b636e`：due/reminder/finite+unlimited penalty arithmetic 与 pending Quest lifecycle；`616ba892`：nested schema、35 个账本/兼容声明和真实 `DailyTickEvent` handler 逐声明原样归位；SyncData field/registration 保持 host |
| J12a3.3 Trust progressive state | DONE | `fa26d430`：75 个 progressive carry、个人/公共/settlement/merchant state 与 battle/quest event 声明原样归 Trust owner；删除零调用 `ClampLong` |
| J12a4 | DONE / RETAINED-COMPAT | Economy typed owner/J09 single execution 已通过；`ApplyRewardTags` 仍有 Native/Scene/Courier 5 个真实 mixed-domain 消费者，保留到 J12b/J13 各域接走，不用删活路径冒充清理 |
| J12b1 四规则归位 | DONE | `0ac279fc`：OfferCooldown/ThreatState/ResultSettlement/PolicyHistory 四文件 100% rename 至 `AF.Module.Diplomacy/Rules`；消费者与 smoke Compile 路径同步，public/internal/DTO/JSON 不变 |
| J12b2–b4 | PARTIAL | `a50ab3ad`：七类 Direct 动作实算法迁入 Actions partial，补宣战主权与真实状态回执；新增不可变 WorldDiplomacy request lease/snapshot，worker 不读可变 job。完整 queue/completion/commit owner 和异代生产回放仍待完成 |
| J12c1–c4 | PARTIAL | `a50ab3ad`：同伴建队关窗请求已有独立票据 owner，旧/重复回调不能释放新请求；普通队列回执返回真实接受数。协议、受理、队列/事件及 governor 延迟请求仍未达到原 c1–c4 退出门 |
| J12d | REOPENED | 当前修复候选 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error、生命周期 32、三个旧红例修后通过、Intent full 1175、source inventory 7、四 DLL API/metadata 1060、Persistence 142/168/13/44、Identity 5、Bridge 23/12、J09 wiring 25、Phase8 readiness 73；本轮 Release/API/Persistence 已通过；剩余职责落地后仍需重跑三渠道/Phase8/代码地图并复验完整矩阵 |

J12a 累计证据：Reward capture 8 声明精确迁移、ProductionReward 11；prompt/trust projection 15 + 2 个可编译变异；Trust state/event 75 声明精确迁移并删 1 个零调用 helper；Debt normalization/schedule 48 + 2 个可编译变异；Quest 10、ledger/DTO 35、daily 1 个声明精确迁移；HeroAssetScope 67 + 5 个有效变异；GiveAsset stress 80562；Economy port/executor、J09 三渠道 single execution 25、Production owner current-DLL replay、Phase8 73 通过。Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；四实现 DLL API/metadata 1060；Persistence/Profile 142 literal / 168 typed / 13 chunked / 44 flattened；代码地图 412。`MemorySummaryMainThreadBoundaryTests/run_terminal.py` 的既有非 J12 `Missing TagSceneSessionHistoryLine` 仍不计 PASS。LIVE、真实旧 SAVE、真实 Economy/AFEF/provider/性能均未运行。

J12b1 证据：四文件 Git `R100`；PolicyHistory 94、ResultSettlement 453、Intent rule-focused 1143 通过；Debug 1.3/1.4/Bootstrap 0 warning/error。原两个 net6 smoke 在本机离线缺 net6 ref pack 并尝试 nuget.org，故使用不改断言的临时 net8 wrapper；当时完整 Intent runner 因读取旧 `SubModule` 注册位置失败；`a50ab3ad` 后测试追随真实 `StartupPatchComposition`，完整 1175 断言通过。代码地图 416 锚点绑定 `0ac279fc`；LIVE/SAVE/真实外交均未运行。

旧 `1c62c2c9` 证据只证明 Direct/World/WorldMap 三文件 R100、编译/ABI/存档形状未回归，不能替代本计划 b2–b4/c1–c4 的真实 owner 与生命周期回放。`a50ab3ad` 已修三项运行缺陷并建立 Direct action、WorldDiplomacy request lease、WorldMap delayed request 的首批真实责任；剩余项以上表 PARTIAL 为准。LIVE/SAVE/真实 provider/帧性能仍 NOT-RUN。

## 1. 目标、范围与完成含义

将 AF 主体的经济、外交、大地图命令责任按真实算法/状态/消费者拆到各自模块，继续编入同一个 `AnimusForge.dll`。不以搬文件、加转发接口或主类行数下降冒充完成。

```text
Conversation / Prompt / Actions（J04–J10 的既有链路）
    └─ 窄 typed 接缝 / 原公开兼容入口
       ├─ AF.Module.Economy：捕获与投影、资产授权、债务/信任、执行协调
       ├─ AF.Module.Diplomacy：规则、对话动作、世界外交作业与提交
       └─ AF.Module.WorldMap：协议、资格、受理、命令队列与延迟请求
          └─ 原 Campaign / TaleWorlds / Harmony / 存档适配边界
```

- **包含**：Reward/Debt/Trust 与 Hero/Party/Merchant 执行；直接对话外交、世界外交任务和结果；WorldMap 命令受理、队列、延迟建队/远征接缝。
- **不包含**：PolicySystem、宴会、GCCZ 的玩法重写；Weekly/Social/Duel/Encounter 等 J13 责任；J14 新 public Scene/Courier 提交；J15 内容目录；J16 构建脚本迁移；J17 全仓清空。
- **允许保留**：有实际 Campaign、Saveable、public ABI、Harmony、UI 或原生游戏调用责任的窄 host。保留理由必须按符号记录，不能将整个未拆大类统称“兼容壳”。
- **完成分层**：J12 三域的获准责任完成迁移、真实消费者接通、必要离线门禁通过，才标 `J12_OFFLINE_VERIFIED`。不是“原类完全消失”、实机/旧档通过或全项目收尾。

## 2. 开工前已解决的设计歧义

| 旧提纲容易误读之处 | 本计划采用的真实边界 |
| --- | --- |
| 每域先做“只读投影” | 分为主线程 capture/既有规范化和 detached projection；债务 getter 可改状态，不能直接移到 worker |
| Economy 端口都改为 internal | 现有 Economy 契约/adapter/工厂已有 public 兼容面；保留可见性、namespace、签名和枚举，新实现 owner 用 internal |
| WorldDiplomacy client 重新接 J08 | 已经接到唯一 `LlmNonStreamingTransport`；只迁领域职责，不重开传输管线 |
| 四个外交 Rules 全是不可变纯函数 | 不依赖游戏对象不等于不修改传入 DTO；ResultSettlementSlot 可持久化，类型/字段与 mutation 语义都保持 |
| “幂等，失败不改状态” | 仅未进入副作用 owner 的拒绝保证无效果；开始后分成功、部分成功、unknown；不发明全域事务回滚或任意重试 |
| WorldMap 返回 false 就没有动作 | 可能已先 STOP 再失败；queued 也不等于建队/行军完成。保留细分结果与原 bool/ref 兼容语义 |

接口稳定约束的是能力、身份和失败承诺，不冻结内部算法。若发现需改变玩家行为的真实 Bug，先给具名复现、旧/新语义及影响面，再按获准修复处理；不能在目录迁移里偷偷改变规则。

## 3. 基线与旧 AF 对照

- 唯一施工树：`G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`。
- 本地分支：`codex/af-modularize-j04-20260918`；未来交付目标：`origin/codex/af-main-refactor-continuation-20260831`。本次写计划不自动推送。
- 当前保留 `.dotnet-cli-home/` 和其他本地资料，不纳入 Git。不得改旧 AF-REFACTOR、NEW-10、GCCZ 或其他工作树。
- 功能对照双基线：`60499d44` 保留 J01–J11 已批准修复；当前本地 `origin/main` 为 `0a641aab7bb3f802625e7a06a8667138aaf0c3d2`，仅作原 AF 参考，不宣称本轮已联网核对 main 最新状态。实施首轮 fetch 后记录精确参考；分叉/同 owner 他人改动时停止相关写入，不自动 merge/rebase。
- 不机械恢复 main 的已修缺陷；每域维护“原功能 → 当前入口 → 新 owner → 应保持/已批准变化 → 正常/负向证据”对照，避免历史修复丢失。

## 4. 当前代码坐标与目标责任

下表均为 `60499d44` 的仓库相对路径、一基行号；后续以符号和新提交重定位。目标目录为设计，不代表现已实现。

| 责任 | 已核实入口 / 消费者 | 目标与保留边界 |
| --- | --- | --- |
| Economy 契约/计划/线程边界 | `src/AF.Contracts/Compatibility/Economy/EconomyRewardDebtContracts.cs:13–149`；`src/modules/AF.Module.Economy/{Planning,Execution}`（`3f2c454e` 原样迁移） | 原 public/namespace/签名/enum 保持；实际 planner/port 已归 Economy，未造第二份 |
| Hero/Party/Merchant replay | `RewardSystemBehavior.EconomyReplay.cs:23,61,155`；`RewardSystemBehavior.EconomyPartyReplay.cs:22,82,183`；`RewardSystemBehavior.EconomyMerchantReplay.cs:21,74,177` | Economy 的三个实际目标 owner；游戏资产变更走窄 host，public 工厂和主体 target 绑定保持 |
| Reward/Debt/Trust capture | `RewardSystemBehavior.cs:4970–5120,18301,18343,18846,18884,19169`；`MyBehavior.cs:30611,30615`；`src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs:84–87` | 主线程规范化/捕获，detached 格式化；PromptExtras 仍唯一拼装，不重复生成规则块 |
| 资产/债务/信任执行 | `RewardSystemBehavior.cs:19825` 授权；`:8359,21406,21895,22194,22355` mutation；`:8720,8984,19449` debt；`:3267,3385` trust | 领域算法实际移出，不能让新 owner 仅回调整个旧方法；不改 ALL/modifier/market/RP、逾期和信任数值 |
| Economy 消费链 | `ShoutBehavior.cs:16701–16739,16771,16866,17268,17324`；`CourierDeliveryBehavior.cs:364–395`；`src/modules/AF.Module.Actions/Execute/LegacyNativeActionPlanExecutor.cs:270–307` | Hero/Party/Merchant resolver 与已有唯一 action commit；Economy tags 从 legacy delegated raw 排除，不能双执行 |
| Economy 保存/任务身份 | `RewardSystemBehavior.cs:940,1207`；`DebtPromiseQuest.cs:12,15–46,112,142,463–472` | 原 CampaignBehavior、Quest、SaveableField 1–10、definer base 711110/class 1 留兼容身份；不改 key/type |
| 外交规则与 DTO | 根目录 `WorldDiplomacy{OfferCooldown,ThreatState,ResultSettlement,PolicyHistory}Rules.cs:134,22,70,9`；ResultSettlementSlot `:55` | 四文件归 `AF.Module.Diplomacy/Rules`；保持 public 类型、DTO/JSON 形状、输入对象 mutation 与实际消费者 |
| 对话外交 capture/execute | `DiplomacyBehavior.cs:62,85,100,128` capture/Prompt；`DiplomacyBehavior.Actions.cs:24–317` 七类动作；`DirectDiplomacyWarGuard.cs`；`ShoutBehavior.cs:10488,18703,26703`；Courier `CourierDeliveryBehavior.DomainCommit.cs:74` / `CourierDeliveryBehavior.DeliveryLifetime.cs:66` | 七类外交动作的领域 owner 与捕获/规则分别归位；Native/Scene/Courier 和保留兼容调用都接同一实现 |
| 世界外交作业/完成 | `WorldDiplomacyBehavior.cs:2282,2538`；`WorldDiplomacyRequestLeaseCoordinator.cs` | 外交 queue/snapshot/in-flight/completion owner；领域预算、优先级、冻结历史和提交语义保持 |
| 已有外交 Gateway | `WorldDiplomacyBehavior.cs:2419,2429` → `Refactor/Adapters/LegacyWorldDiplomacyLlmGateway.cs:31` → `WorldDiplomacyLlmClient.cs:59,108,198,213` | 继续共用 J08 transport；route/retry/thinking/token/stale 仍属领域 client，不迁进通用 HTTP owner |
| 外交跨域与保存 | `WorldDiplomacyBehavior.cs:213,228,381,5515`；`VassalageBehavior.cs:1234`；`KingdomAnnexationBehavior.cs:32`；`VoteDealBehavior.cs:2910,3010`；`KingdomStrategicProfileBehavior.cs:58,69` | 对话外交、附庸/吞并接缝有明确 owner；PolicySystem 玩法不改；VoteDeal/战略档案仅处理本包明确列出的 AF 主体接缝，其余责任保留、不整体重写，保存和注册不重命名 |
| WorldMap 协议/资格/受理 | `WorldMapPartyCommandBehavior.cs:559,589,722,914,4883,5137,5175,5189` | `AF.Module.WorldMap` 分协议、capture、受理；目标 resolver 仍在所属游戏线程 |
| WorldMap 队列/延迟请求 | `WorldMapPartyCommandBehavior.cs:1216,1300,1439,1503` append/stop；`:1550,1600,2215,2311` tick/推进；`:1837,7340,7431,9266` 待建队/远征 | 迁有限命令调度/请求协调；原生 AI 和游戏操作保 host，不能只挪入口而保留全部调度算法 |
| WorldMap 保存/外部事件 | `WorldMapPartyCommandBehavior.cs:28–31,387,406`；`WorldMapGovernorExpeditionNativeLifecyclePatch.cs:47`、`WorldMapOrderedArmySurvivalPatch.cs:52` | 四 key、JSON 形状和加载后 transient 清理保持；Harmony 类型/反射目标/fail-open 不迁移 |
| WorldMap 回执/兼容 | `WorldMapPartyCommandBehavior.cs:376,1107,1142,1147,1189`；公开 `WorldMapOrderApplyResult` | 保留嵌套 public 类型、ref/out、StopApplied/AddedCommandCount/Queued/NeedsChannelExit，不把排队当事实完成 |
| 引擎装配/调度 | `src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs:45–53`；`ApplicationTickComposition.cs:77–80,118–121` | 保持注册次数/顺序与单一调度入口；Harmony 类留原适配层，只更新必要调用接缝 |

## 5. 有限实施顺序及退出门

顺序：**G0 → J12a → J12b → J12c → J12d 整包验收**。每包实现、接线、自审、针对性验证后本地提交；不为每个 helper 重跑六构建。表内子包是审查/回滚边界，不要求每次只完成一个小改动。

### G0：一次性冻结责任与验收输入

1. 核对 Git/作者/三 SKILL，仅按相关范围加载；三渠道当前接线、旧 public/Saveable/JSON/反射消费者逐符号登记。
2. 确认 SDK `G:/AFMOD/.dotnet-sdk/dotnet.exe`（当前 8.0.422）、对应 Newtonsoft、1.3/1.4 引用及现有 wrapper；不装 SDK、不改生产引用凑通过。
3. 只跑会被本包改动失效的基线；对已有有效 J07–J11 证据记录复用理由。存在环境/测试工具红项先分类，不反复全仓审计。
4. 首包环境注意：`ProductionReward/run.py` 固定的 `local/dotnet/8.0.425/dotnet.exe` 当前不存在；部分外交 smoke 是 net6.0，不能假定 SDK8 就能启动。仅在测试工具层增加显式环境参数或使用已核实的 wrapper，保留断言；运行时/依赖不足如实记 NOT-RUN，相关退出门不算通过。
5. 建立本阶段保留清单：符号、剩余责任、真实消费者、保留原因、退出条件。所有旧代码删除以调用/ABI/存档证据为前提。

**退出门**：第一责任包可构建、关键基线可执行，源/产物/依赖有明确版本绑定；没有同 owner 并发写入。此后不按心跳重复 G0。

### J12a：Economy

| 子包 | 实际工作 | 退出门 |
| --- | --- | --- |
| a1 契约归位与 capture/projection | 原契约/adapter 有证据地归位；提取 Reward/Loan/Debt/Trust 捕获和输出算法。将 `NormalizeDebtRecord` 等既有规范化留主线程，worker 只收 detached DTO | Hero、Party、Merchant 输入→Prompt/授权快照与当前生产等价；债务查询副作用次数/顺序保持；旧 public ABI 不变 |
| a2 资产授权和三个 replay owner | 实际迁移授权解析、数量/owner 路由、三个 replay 循环及领域 action 分派；窄 host 负责真实 inventory/gold/settlement 操作，复用 J09 planner/port/回执 | ALL/modifier/market/RP/finite forceComplete 不回归；目标失效拒绝；partial/unknown 保此前确认事实并停止后续；三渠道只执行一次 |
| a3 债务与信任责任 | 拆归一、创建/约定结清、到期/提醒/罚则及 Quest 同步协调；分离信任计算和真实状态应用，保原数值/累进余量/保存形状 | 代表性旧 debt 数据、重复还款、到期与 Quest 对齐、trust carry 边界对照通过；不得将 getter 当无状态纯函数 |
| a4 兼容链清理 | 清理已替代的重复实现；`ApplyRewardTags`/Party/Merchant 中仍活跃的外交/入队等分支逐项归属；给 J12b 接缝而非整段删除 | public 工厂、保存 owner、PlayerRpCrafting/装备/商人刷新等保留符号有明确理由；无同一 economy 标签新旧双执行 |

不把整个 `RewardSystemBehavior` 剩余内容打成“已拆净”。PlayerRpCrafting 的游戏生成、UI、装备恢复等本轮不改玩法；跨到这些入口只做必要接线，后续拆分责任在保留清单明列，不以 J12 完成宣称它们已重写。

### J12b：Diplomacy

| 子包 | 实际工作 | 退出门 |
| --- | --- | --- |
| b1 四规则归位 | 四 Rules 内容等价迁移，同步 Compile/源码提取工具/地图；保 public 类型和 result slot 保存形状 | 规则执行、slot 合并/消费、冷却、威胁和历史压缩边界保持；不能只改源码字符串检查让其通过 |
| b2 直接外交 capture→execute | 提取资格/Prompt/规则与七类动作执行；跨 Economy 的附庸/吞并交给外交权威 owner，原 bool/ref/通知接缝保留 | 合法/错误目标、非国王、独立氏族和谈、已战/已和平、先解约再宣战等具名场景；执行事实才通知世界外交档案 |
| b3 世界外交作业 | 提取排队、请求冻结、in-flight 所有权、完成路由及领域提交协调；worker 不闭包读取可变 job/Game 对象；保持 generate/analyze/compress/round_plan/round_compress | 强制迟到完成、同 jobId 不同 generation、取消/失败/截断下不污染新 job、不发布伪事实；领域预算/cache affinity/优先级保持 |
| b4 剩余接缝与清理 | 明列 Vassalage/Annexation 的主体执行责任、VoteDeal/StrategicProfile 的调用边界；清理替代旧体，不把政策玩法搬进通用 Actions/LLM | 所有真实消费者接唯一领域 owner；存档、Campaign 注册、Harmony 不变；不能只迁四 Rules 就标外交整包完成 |

世界外交 client 的 route、retry/backoff、400 thinking fallback、token/cache/truncation 和 stale 检查继续由同一领域 client 负责。J08 的网络 owner 不重建，PolicySystem 的检索/存档/执行规则不改。

### J12c：WorldMap

| 子包 | 实际工作 | 退出门 |
| --- | --- | --- |
| c1 协议、资格与投影 | 将 token/顺序解析和纯格式化迁实际 owner；目标解析、授权与活动队伍捕获在游戏线程；不复制 J09 通用 parser/allowlist | 有效/未知标签、无效 ID、STOP 首位与中途语义、别名/天数/mode、Hero/非 Hero 目标，以及 worker 不读游戏对象的证据 |
| c2 受理执行 | 从 `TryApplyWorldMapOrderTagsForExternal` 迁真实分支编排：STOP、普通追加、非 Hero、同伴建队、总督远征；host 只提供窄游戏操作 | STOP 后续失败、部分效果、排队接受、拒绝均保真；public 结果类型和旧 bool/ref 行为不变，不造“失败自动回滚” |
| c3 队列/事件生命周期 | 将命令队列的推进、暂停/结束判定及失效目标处理归明确 owner；原事件入口转接一次；游戏 AI 调用仍在 host | 单次 tick/event 不重复推进；目标死亡/俘虏/队伍销毁/攻城变化不执行旧命令；存取仍复用原容器/序列化身份，不双持状态 |
| c4 延迟请求/渠道收口 | 分离待关窗后的同伴建队/总督远征请求协调、claim/completion 与回执；保原生 UI/建队/兵员操作，核对 J10 渠道和 Gathering 消费者 | 旧请求/重复回调不操作新请求；待离开 Mission/Conversation/PartyScreen 后重新校验，Gathering 的 SourceId 不匹配不得停止别人的命令；`NeedsChannelExit` 顺序、取消/部分结果、单次历史/AFEF 保持；排队不宣称最终建队/移动完成 |

不重写世界地图 AI、战斗/围攻或总督玩法。队列/建队协作需要访问原私有/嵌套保存类型时，优先保类型身份、抽无保存身份的算法/协调，不能为换目录新增持久化状态或反射访问私有字段。无法安全抽取的具体符号留保留表并说明原因；核心队列/受理业务若仍整体留旧类，J12c 只能 PARTIAL，不能靠薄 wrapper 结项。

### J12d：整包收口

- 三域真实消费者及关键失败路径闭合后，再集中跑最终 Debug/Release × 1.3/1.4 + Bootstrap；保持原一键构建流程，不传 Stage/Deploy。
- API/metadata、保存 Profile/Chunk/Identity、三渠道影响面、Bridge/Composition/Phase8、代码地图 recorded/working-tree 验收。
- 清理被替代路径、未使用 helper、重复声明、重复 Compile/资源和临时开关；保留兼容项逐符号交接。
- 更新主台账、范围图/代码地图和根 HANDOFF，写本地制作组简明版到 `.tmp/`（不入 Git）。推送/自动化按届时用户指示，不继承历史授权。

## 6. 不可破坏的行为与稳定接口

1. 同 DLL 的 internal 实现与 public V1 子 MOD 层分开；旧 public Economy/Rules/嵌套 result 属兼容面，不自动算 V1 新能力，也不得降级 internal。
2. 不新增/改名 SyncData key、Saveable type/field ID、程序集、模块 ID、JSON 字段或默认开关；原嵌套类型不要随 owner 改名。Quest、CampaignBehavior、Harmony 的类型身份保留。
3. 先规则资格→`tag_rules`→解析/计划→唯一领域执行→确认事实/历史/AFEF。实际共享入口不增第二 parser、第二 ActionPlan executor 或第二事实提交者。
4. Capture 不等于纯查询：保留原领域规范化/状态同步，只在所属线程做；后台只处理 detached 值。跨 await 重验 generation、目标、owner、request/session 身份。
5. pre-owner 拒绝不得碰资产/外交/队列；开始后的异常不能返回假“零效果”、不能自动重试、不能转 legacy 再执行。保留 partial/unknown 和既有确认事实。
6. 一次请求重复 completion 不重复动作；不把不同新请求的合法重复命令一概去重，不新增持久化幂等 key。
7. Courier 动作仍在 `DeliveryApplied` 后提交；Scene relay/旁听/去重与 Native completion/关窗不改。WorldMap queued 请求按原渠道退场时点启动。
8. WorldMap `StopApplied`、Queued、AddedCommandCount、Handled 等分别测试；不能从 bool、正文、通知或 LLM 接受语气倒推实际效果。
9. 外交 Threat/Offer/ResultSlot 的身份、冷却、优先级、历史冻结、失败/截断与发布顺序保留；政策/宴会/GCCZ 保留原领域 gate，包括 J11 已记录的兼容路径。
10. 高频行为不新增全世界扫描、逐 tag 重复捕获、反射、无限缓存/队列、游戏线程同步等网络；记录实际工作项数量/复杂度/等待行为，帧耗时未测就 NOT-RUN。
11. `ShoutBehavior.cs` 必须用精确字节修改，保 CRLF/原 BOM 状态；其余文件也避免整文件格式噪声。不要移动完整混合文件再声明所有职责已完成。
12. owner 接线前不删旧路径；删除前查真实调用、反射/Harmony、ABI、SyncData/资源 loader。编译 Link、测试路径及地图同步；禁止复制一份旧核心参与编译。

## 7. 验证矩阵：复用有效证据，补本包风险

| 包 | 复用入口 | 本包必须补/确认的行为 |
| --- | --- | --- |
| Economy capture | `tests/modules/AF.Module.Prompt/ProductionReward/run.py`；PromptExtras/相关三渠道捕获测试 | Hero/Party/Merchant/空值/规范化后输出差分；真实 capture→投影，不是仅 DTO 样例 |
| Economy execute | `tools/HeroAssetScopeRegressionTests/run.py`（既有 67 场景与 4 变异）；`EconomyRewardDebtPortContractTests`、`EconomyAwareActionPlanExecutorContractTests` | 原 owner 授权与 mutator observation、ALL/modifier/market/RP、已成功后 unknown、失败无伪事实、唯一消费 |
| Economy DLL replay | `tools/ProductionEconomyOwnerReplayTests`、`ProductionEconomyAwareCommitReplayTests` | 明确 fake port、无 live owner、源码检查的盲区；不能把工厂 fail-closed 当真实转账成功 |
| Diplomacy rules | `tools/WorldDiplomacy{IntentBoundary,ResultSettlement,PolicyHistory,Compression}.SmokeTests` | 真实 Rules 算法和迁移路径保持；源码片段检查不能替代领域执行与异步完成回放 |
| Diplomacy runtime | 现有 `WorldDiplomacyGatewayReplayTests` + J08 transport 证据 | 真实 owner 的 queue/start/completion/commit；同 jobId 异代、失败/取消/截断、正常五类作业路由；不访问付费 provider |
| WorldMap | 本包直接生产入口/owner 的 source-linked 或真实 DLL 回放；受影响三渠道用例 | c1–c4 各条实际分支和 lifecycle；STOP 后失败、失效目标、重复/迟到完成、延期建队与事实一次提交 |
| 共同接线 | `J09DefaultChannelActionWiringTests`、`ActionProtocol`、Native/Scene/Courier 相关回归；`CampaignCompositionTests`、`TeamModulePortParityTests` | 模块接入仍执行真实入口，不回退手写 Current；J11 42+6/308+3 按受影响范围复用或复跑 |
| 最终兼容 | 现有双版本/Bootstrap wrapper、ModuleFrameworkApi、Persistence/Profile/Chunk/Identity、代码地图 | 同一候选与实际 DLL/hash 绑定；不因 doc-only 改动重跑六构建 |

- 每个完整责任包正常用例先过，至少一个针对本包关键风险的有效反例；复用仍命中风险的既有变异即可，不按测试数刷进度。变异必须编译成功并因预期具名断言失败，路径/编译失败不是红例。
- `WorldDiplomacyGatewayReplayTests` 及部分 production replay 固定 Stage DLL 路径。优先复用已核实的无 Stage/显式候选 DLL 运行方式；不能拿旧 Stage 冒充新产品，也不为跑测试擅自 Stage。
- 源码迁移同时检查完整 Compile/嵌入资源集合，不只比较文件数。公开和保存声明只保形状还不够，受影响方法需行为证据。
- 不强求每个 helper 一个新 fixture；沿完整消费链建立证据，保旧断言，不刷新 hash 掩盖差异。
- LIVE、旧 SAVE、真实库存/金币/商人/债务、外交状态、地图队伍/建队/UI、provider、音频、帧成本分别标 NOT-RUN；离线结果不替代玩家实测。

## 8. 状态、收工与回滚

| 阶段 | 本次状态 | 后续完成判定 |
| --- | --- | --- |
| J11 | OFFLINE_VERIFIED | 已有产品和修复证据，不重开 |
| J12 计划 | IN-PROGRESS | 本文、主台账与 HANDOFF 已纠正过早 DONE；继续按原退出门施工 |
| G0 / J12a / J12b1 | OFFLINE_VERIFIED | 已有产品与离线证据；相关源码变化才定向重跑 |
| J12b2–b4 / J12c1–c4 / J12d | PARTIAL / REOPENED | `a50ab3ad` 完成首批修复与 owner；剩余真实职责及整包验收不得用目录迁移代替 |
| J13–J17 | NOT-STARTED（本轮） | 不在本计划提前宣告完成 |

- 不设“几小时必须全部完美”的硬承诺。已满足责任包退出门就进入下一包，不因还能抽 helper、旧类仍长、无关历史 HOLD 而停留；只因新复现/本包改变导致风险才追加必要门禁。
- 产品施工前本地检查点；每个完整验证包独立提交。发现同 owner 他人修改、远端分叉、需要改政策玩法/公开 ABI/存档协议/其他树时暂停相关写入，保留其他安全工作。
- 回滚用对应文档/接线/owner 包的逆序定向 `git revert`，不 reset、rebase、stash 覆盖或强推；代码回滚不假装回滚已发生的游戏状态。本轮未接触游戏/存档。
- 唯一详细进度继续记录在[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j12-planned-20260921)，根 [HANDOFF](../../HANDOFF.md) 只保当前摘要；本计划不是新竞争台账。
