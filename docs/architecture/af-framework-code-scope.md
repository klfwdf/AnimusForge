# 当前范围：J12 审查修复后重新开放（2026-09-21）

当前状态为 **J12_REOPENED_PARTIAL**。`a50ab3ad` 已将 Direct 七类动作实算法分到 `DiplomacyBehavior.Actions.cs`，新增 `DirectDiplomacyWarGuard`；`WorldDiplomacyRequestLeaseCoordinator` 拥有请求 identity/冻结参数/释放；`WorldMapDelayedRequestCoordinator` 拥有同伴建队 UI 一次性票据，普通队列回执返回真实接受数。对应生命周期测试 32、完整 Intent 1175、三个修复回放、Debug/Release 双版本与 Bootstrap 六构建、四 DLL API/metadata 1060、Persistence/Identity、Bridge、J09 wiring、Phase8 readiness 与 source inventory 已通过。

仍未归位：WorldDiplomacy 完整 queue/start/completion/commit；WorldMap 协议、受理、队列/事件及 governor 延迟请求。下方 `1c62c2c9` R100 段仅是历史目录归位记录，不再构成 J12 完成证明。J13 在这些责任和 J12d 最终门禁闭合前暂缓。

## 历史目录归位记录（非当前完成结论）

最终产品 `1c62c2c9` 将三个完整真实 owner 原样归位：

- `src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs`：直接外交资格、Prompt、七类动作和通知接缝；
- `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs`：job queue、冻结请求、in-flight/completion、传播、结果提交与持久状态；
- `src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.cs`：协议、受理、队列/事件、延迟建队/远征和嵌套回执。

三文件均 Git `R100`，根旧路径不存在、无转发 facade、无第二份状态。主动源码读取工具、Policy overlay、Bridge manifest、Persistence catalog 和 Phase8 inventory 均接新位置。类型全名、程序集、Campaign 注册、Harmony、public/internal 签名、JSON/SyncData/Saveable identity 未变。

该段旧状态 **J12_OFFLINE_VERIFIED 已撤销**。历史六构建、ABI/存档形状与规则测试仍可作未改范围的参考，但不能替代缺失的 domain owner/lifecycle 回放。LIVE/SAVE/真实玩法和性能仍 `NOT-RUN`。

## J12b1 Diplomacy Rules 已归位

`0ac279fc` 将 `WorldDiplomacy{OfferCooldown,ThreatState,ResultSettlement,PolicyHistory}Rules.cs` 以 100% rename 归入 `src/modules/AF.Module.Diplomacy/Rules/`。`WorldDiplomacyBehavior` 的真实消费者、public/internal 类型、DTO/JSON、slot mutation 和 J08 transport 均未改变。PolicyHistory 94、ResultSettlement 453、Intent rule-focused 1143 与 Debug 双 API/Bootstrap 通过；当时完整 Intent runner 的 PermanentAlliance 注册断言仍读取旧路径；本轮已改读真实 `StartupPatchComposition`，完整 1175 断言通过。

此处记录 b1 当时的阶段状态：四 Rules 已归位、其余 J12b/J12c 尚待施工；后续完成结果以上方 J12 总结为准。该阶段代码地图为 416 锚点绑定 `0ac279fc`，LIVE/SAVE/真实外交未运行。

## J12a Economy 已闭合范围

产品 `3f2c454e` 将既有 public Economy contract/planner/main-thread port 原样归入 `src/AF.Contracts/Compatibility/Economy` 与 `src/modules/AF.Module.Economy/{Planning,Execution}`，并以 `Projection/EconomyPromptProjection.cs` 承担 Debt/Trust detached 文本投影。`RewardSystemBehavior.cs:5108,5119,18847,18878` 仍在主线程读取/规范化/估值后传 string/int-only 输入；真实 Prompt consumers 不变。

后续 `07feb572` 将 Hero/Party/Merchant 三个完整 replay partial 100% 原样迁入 `Execution/{Hero,Party,Merchant}`，并把 11 个 live asset authorization 方法抽入 `Authorization`；删除零调用旧 resolver。`deb421ae` 建立共享 `EconomyReplayBatchCoordinator`，三个 domain step delegate 保留唯一 live mutation；null/reject/applied、prior fact、partial/unknown 和 unknown 后停止不再由三份循环重复。

`54b55aa3` 新增 `Trust/EconomyTrustPolicy.cs` 与 `Debt/RewardSystemBehavior.DebtNormalizationPolicy.cs`：前者承担 clamp、十级映射和 AI 语义文本；后者承担旧账迁移、line clamp、note、aggregate/date 重建和幂等归一。`3d2b636e` 再将 due/reminder/finite+unlimited penalty 算法归 `EconomyDebtSchedulePolicy`，并把 pending quest 状态及 10 个 Quest lifecycle 声明逐声明原样迁入 `DebtPromiseLifecycle`。`616ba892` 将嵌套 DTO/schema、运行账本、提示/导入导出、创建/结清共 35 个声明归 `DebtLedger`，真实 `DailyTickEvent` handler 归 `DailyEconomyLifecycle`，未留根转发壳。`_debtStorage` 与事件注册仍留 Campaign host；嵌套类型全名、JSON/SyncData/save identity 未改名。

`fa26d430` 将 progressive carry、个人/公共/定居点/商人 state apply 及 battle/quest event 75 个声明原样归 `TrustState`，删除零调用 `ClampLong`。`7f2fffba` 将 Hero/merchant 完整授权候选、可见候选和 Prompt capture 8 个声明原样归 `Projection/InventoryPromptCapture`。mixed `ApplyRewardTags` 因仍有三渠道 5 个真实消费者保留；J09 typed Economy 及 delegated-raw 排除继续保证单次执行，待 J12b/J13 各域接走后再删除。

状态 **J12a_OFFLINE_VERIFIED**。Debug/Release 双 API + Bootstrap、四实现 DLL 1060 metadata、Reward 11、Projection/Trust 15+2、Debt 48+2、HeroAsset 67+5、J09 wiring 25、Persistence/Profile、Phase8 73 与 412 锚点地图已过；LIVE/SAVE/真实经济仍 NOT-RUN。详见[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j12a-economy-start-20260921)和[J12计划](../plans/j12-domain-owners-plan.md)。

## 以下为上一阶段 J11 范围

# 当前范围：J11 制作组内部模块接缝离线闭合（2026-09-21）

产品 `cdbd077a` 将 Policy/Gathering/Siege 的 3 个 internal contracts / 13 方法归位 `src/AF.Contracts/Internal/TeamModules`，将 3 个无状态 adapter 分拆到 `src/bridges/{Policy,Gathering,Siege}`；`TeamModuleServices` 仍在 GameAdapter composition。31 个生产调用点、参数/ref/out/异常和原领域 owner 不变，旧两个混合文件已删除。

状态 **J11_OFFLINE_VERIFIED**。Policy 1.3/1.4、Team ports、Campaign/Bridge、三渠道影响面、六构建、API/存档和 394 锚点地图通过。Policy/Gathering/GCCZ 玩法、MCM、Prompt、存档、Harmony、public API 和 `G:/AFMOD/GCCZ` 未改；真实游戏/旧档/玩法结果仍 `NOT-RUN`。详见[接缝矩阵](af-team-module-seam-matrix.md)与[详细 HANDOFF](../handoffs/2026-09-21-j11-team-module-seams-offline-closeout.md)。

## 以下为上一阶段 J10 范围

# 当前范围：J10 Scene / Courier 离线整包闭合（2026-09-21）

J10 产品终点为 `f6c95ac3`，验收/地图补丁为 `7d70f528`。Scene 的 audience/request/pending AFEF/speech queue/group/relay/passive/reaction 与 Courier 的 prompt/generation/transport/session/arrival/letter/retry/domain commit/reply wait 已由 `src/modules/AF.Module.Conversation/Channels/{Scene,Courier}` 的稳定 owner 承担，真实消费者仍是同一 production partial class；没有第二管线、新 facade、公有 API、默认开关或存档键。

Courier 最后责任位于 `CourierDeliveryBehavior.PromptMessages.cs:41,444`、`CourierDeliveryBehavior.DomainCommit.cs:41,167,215,289`、`CourierDeliveryBehavior.ReplyWait.cs:73,102`。动作只在 `DeliveryApplied` 后提交；request/session/target 重验、one-shot Economy reservation、单次历史/AFEF fan-out 和最后 waiter 释放时间锁保持。Scene group/relay 和 reaction 入口位于 `ShoutBehavior.SceneConversationChains.cs:770,1568`；live Agent、TTS/audio、movement 与游戏对象副作用有意留游戏线程 adapter。

状态 **J10_OFFLINE_VERIFIED**。392 锚点地图 recorded/working-tree 通过；六构建、1060 API/metadata、142/168 存档契约、Scene/Courier/Native/Bridge/Phase8 与有效负例详见[详细 HANDOFF](../handoffs/2026-09-21-j10-scene-courier-offline-closeout.md)。Policy/Gathering/GCCZ 玩法未迁入主体。真实 Campaign/Mission、旧 SAVE、provider、live Economy/外交、子 MOD CLR、音频与性能仍 `NOT-RUN`；未 Stage/Deploy/Package。

## 以下历史范围以上方更新为准

# 当前 J09 Actions / 事实提交范围（2026-09-21）

J09 已完成必要离线验收：共享 owner 位于 `src/modules/AF.Module.Actions/{Tags,Plan,Execute,Receipts}`；默认 Native、Scene、Courier 只通过 action-only compatibility boundary 调回各自 live domain core，历史/AFEF 仍由原渠道或 detached `InteractionResultCommitter` 唯一提交。精确符号和一基行号见同目录 `af-framework-code-map.json` 的 `actions.*` 新增锚点；完整结果、回滚和 NOT-RUN 边界见[主台账 J09 回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j09-offline-verified-20260921)。

保留边界：`InteractionContracts.cs` 的 Action DTO/port 为稳定契约；Economy/Duel 已有 typed port，其他领域继续走受审 legacy adapter，后续 J12/J13 才按领域归位。Scene group/relay/passive/reaction 与 Courier transport/retry/session 属 J10；Policy/Gathering/GCCZ 玩法不在 J09 搬迁。真实游戏、旧档、live Economy/外交/provider/音频/帧成本仍 `NOT-RUN`。

## 以下历史范围以上方更新为准

# 当前范围：J08 LLM 离线责任包完成（2026-09-21）

源码包 5dc17947 / 4776b691 / 1e1fdfad / 5a2df9d6，322 锚点图绑定最终产品提交。非流与 SSE attempt/lifetime 已由 AF.Module.Llm 唯一 owner 承担，Primary/Configured/Policy/WorldDiplomacy 使用真实消费者；ModelCatalog/TTS transport 已归模块目录。策略 retry、UI/Prompt、领域 profile 和游戏音频生命周期保持原 owner。

状态 J08_OFFLINE_VERIFIED；真实 provider、游戏/旧档/音频/性能仍未验。详细实现、两项缺陷、证据、保留项和回滚见当前主台账 j08-offline-closeout-20260921。下一阶段 J09，自动化暂停。

## 以下历史范围以上方更新为准

# 当前范围：J08a 共享非流 HTTP owner 已接线（2026-09-21）

源码 `5dc17947`，315 锚点图。Primary 和 Configured 的非流 attempt/auth/body/response lifetime 共用 `LlmNonStreamingTransport`；原上层 retry/UI/错误策略保留，经旧/新请求和输出差分验证。资源泄漏已复现并修复。J07 保持离线闭合；J08 上层编排、stream/model/TTS 未完。详见[当前主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j08-nonstream-transport-20260921)。

## 以下历史范围以上方更新为准

# 当前范围：J07 整回合离线验收完成，下一包 J08（2026-09-21）

生产提交 `e5c14b8a`，310 锚点绑定当前实现。Native 单体回合已替换为真实四阶段协调 owner；宿主按准备、Prompt/历史、展示、后处理/提交适配。后处理复用共享 Prepare/Request/Complete，游戏读写/资格/归一化回所属线程，网络留后台。内部/外部 API 和存档身份未改。

状态 **J07_OFFLINE_VERIFIED**，不是全项目或实机完成；主 Shout 仍有 39,190 行，其余渠道/领域按后续包治理。源码坐标、已跑/复用证据、保留项、回滚和 J08 清单见[当前台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j07-offline-closeout-20260921)。

## 以下历史范围已被当前回执取代

# 当前范围：J07b 正文阶段 / raw 线程边界（2026-09-20）

源码 `d9e9aae1`，298 锚点地图。新增已接线 `NativeConversationMainReplyStage` 与内部 typed 端口；private host 使用原 LLM 与主线程验证/撤销操作，不是第二条 LLM 管线。raw 观察与提前 TTS 保持原先后顺序，移到已有校验回调的游戏线程；helper 本体未改。

主编排 484→454 行，只完成第一个实际阶段，J07 父包及 J08–J10 未验收。游戏/旧档/真实语音/provider 未测；具体代码坐标、旧源码复现、正常/负例与构建分层见[当前台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j07b-mainreply-thread-boundary-20260920)。历史“484 行未动”已由本节更新，不把全大类标为重写完毕。

## 以下为历史范围

# 当前范围：J07b Native 身份/领取 owner 已接线，主编排仍待拆（2026-09-20）

当前源码 `2835d1a5`；294 锚点地图绑定该提交。`NativeConversationAdmissionOwner` 持有唯一票据槽、epoch、revision；`NativeConversationDispatchClaim` 持有 admission/action 排队操作的原子领取与开始前过期。普通/主动开场/Overlay/Native public 提交及 completion/pending 消费同一 owner；外部签名、存档身份、默认路径不变。

宿主仍负责游戏对象捕获/目标检查；`SubmitNativeConversationTextInternalAsync` 484 行未拆，J07b/c/d 与 J08–J10 未完成。精确源码坐标、两切片行为/负例/双版本/API/存档证据与保留项见[当前主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j07b-native-ownership-20260920)。未做实机、真实旧档或 provider 验收。

## 以下为历史范围，当前状态以上方台账为准

# 当前范围：J07a 原样归位已完成，Native 真拆待 J07b（2026-09-20）

生产源码 `fb5dc1ca`：10 个已有生命周期 owner 原字节迁入 `src/modules/AF.Module.Conversation/Internal`（Pipeline 两实现位于其子目录）；命名空间/类型/API/存档身份不变，旧活动路径不再存在。此包不减少 Native 主类职责，不能称 Native 状态机重构完成。291 锚点地图仅调整对应路径并绑定该源码，原 hash/行号不变。

G2 的非 Hero/常驻/可见队伍与最终请求补强、J07a 路径消费者/双版本/接口/存档证据、严格旧逆变换修复及下一步，以[当前主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j07a-relocation-20260920)为准；下方旧“尚未执行”的文本验证说明已被该节的具名场景结果取代，不代表实机通过。

## 以下保留旧范围与历史，具体当前责任以上方台账为准

# 当前范围：J06 检索收口仍 VERIFY / NOT_ACCEPTED（2026-09-19）

测试增量 `49441aa1` 验证旧/新实体生产上下文 Hero 直接/称谓和当前空 capture 回退，`3aaece30` 验证 Courier 旧/新最终消息构建器的完整 request JSON（76 场景/550 检查、三项丢正文变异）。共享 `CompleteSharedPromptBuild` 同输入三类生产检索、Native 完整请求仍未执行；这两份测试不迁移任何产品 owner，也不改变公开 API 或代码地图坐标。J06 保持 `VERIFY / NOT_ACCEPTED`。

实体文本新增两条生产方法回放：旧同步 `BuildPromptContext` 与当前 capture→detached match→`BuildPromptContext` 在假 Hero 直接/称谓命中时，对主文、后处理、计数、显式王国 ID 完整比较；当前空 capture 回退相同。三个文本/回退丢失变异拒收。其他实体类别、真实游戏成本及最终模型请求仍未覆盖，生产源码/地图未变。

额外规则文本新增旧/新生产方法差分：`AIConfigHandler` 语义命中与词法回退选择、sticky 合并及正文格式化在同一假配置下产出相同规则 ID/文本；正文丢失、漏词法回退变异拒收。未覆盖运行时特殊规则补文、实体事实或最终请求全文；生产源码/地图未变。

新增独立 Lore 方法差分：旧 `77a3d234` 与当前 `KnowledgeRuleIndex` / `LoreCandidateRetriever` / `BuildLoreContextInternal` / `AIConfigHandler.GetLoreContext*` 在相同假 Hero、规则、mention 上生成相同非空正文，当前过期候选回退也相同；版本守卫和正文丢失变异拒收。这是 Lore 一条具名样例，不覆盖实体/规则或最终模型请求，生产源码及 291 锚点地图未变。

`da677af3` 为生产 `CaptureCandidates` 增加 fake hard-budget 首个 64 项批次立即停止契约；没有更改产品源码/代码地图。

追加 `1fa1a4e1`：Courier 旧同步/新调度生产请求体已在生产 `PromptExtrasComposer` 的六类知识文本 fixture 下比较完整序列化请求（两方向 76 场景 / 390 检查，Lore 丢失变异拒收）。这不覆盖游戏端知识结果或 Native 最终模型请求全文；产品源码与 291 锚点地图未变。

本次加固把 Courier 请求旧侧提取范围扩至 `77a3d234` 的两套最终请求方法，使旧新方法不再共用当前最终组装体；Lore/实体/规则三个仅改新侧的丢正文变异均在请求全文比较处拒收。检索输出与底层消息仍是 fixture，不能替代 J06 要求的生产知识结果及 Native 全文对照；产品源码和地图坐标未变。

本次测试 `ae2cb4f4` 从生产 `WorldEntityRetrievalService.CaptureCandidates`/`CaptureDetachedMetadata` 提取方法做 2,000 假 Hero 捕获耗时、64 项预算次数和两项负向变异；`db9a899e` 从生产 `PromptAssemblyStage` 验证完整 `Extras` 八组合与三项文本丢失变异。产品源码与 291 点地图仍绑定 `70db6ec2`。原脚本 Debug/Release 双 API + Bootstrap 六项已按四目录授权通过，取代下方旧构建阻塞；**最终模型请求 Prompt 的旧同步/新捕获全文对照未完成**，不得将 `Extras` 测试当成 J06 离线验收。性能样本只含 fake 游戏端口，非实机帧耗时。

当前源码 `70db6ec2`；[291 锚点代码地图](af-framework-code-map.json) recorded/working-tree 均通过。最新状态与离线阻塞以[主台账 J06 检索收口节](../animusforge-refactoring-and-repository-reorganization-plan.md#j06-retrieval-cutover-20260919)为准，下方 J06d/J05/J04 表是历史切片，不应继续读作当前完成度。

| 责任 / 一基坐标 | 本轮实际接线 | 尚未宣称完成 |
| --- | --- | --- |
| `KnowledgeLibraryBehavior.cs:508-635,711,1535`、`MyBehavior.cs:30285-30386` | Lore 每版本一次的规则快照、索引准备及 MCM 数值快照在游戏线程、候选召回在后台；Hero 文本补文留最终阶段 | Index 版本/缓存与每版本规则快照已有可执行契约；冷索引耗时和最终文本生产对照未齐 |
| `WorldEntityRetrievalService.cs:270-445,514-650,668`、`MyBehavior.cs:30325,30656` | 世界候选和可见队伍一次捕获（每 64 项检查原 3 秒预算）；DTO 名称/称谓匹配及唯一全局分配在后台，同步入口保留；生产分配方法新旧排序/分数 4 项对照与两项变异已跑 | 关系/距离同趟捕获可能增加游戏线程耗时；最终事实格式仍在游戏线程，缺完整文本和捕获性能对照 |
| `AIConfigHandler.cs:5414-5432`、`MyBehavior.cs:30335` | 无预选 ID 的额外规则检索后台化，游戏线程按旧顺序补运行时规则正文；实际回退方法 21 项与两项变异已跑 | 最终 Prompt 文本对照需补 |
| `src/modules/AF.Module.Prompt/Composition/PromptRetrievalCapture.cs:34-57`、`ShoutBehavior.NativePromptBuild.cs:82-123`、`CourierDeliveryBehavior.PromptSchedule.cs:97-120` | Native/Courier 新捕获→纯 DTO 检索→最终 owner 发布与重验；Scene 顺序组合；Courier 迟到和 Native 实际调度方法迟到/异常回放已跑 | Scene 完整异步化归 J10；最终 Prompt 文本对照未齐 |

当前 `70db6ec2` 的非删除性 Debug/Release 双 API + Bootstrap 直接构建六项通过；原一键脚本在固定目录递归重置处被自动审核拒绝，**当前源码未完成官方构建验收**。无 push、Stage 或部署。实机、旧档、真实 provider `NOT-RUN`，不当作本次离线阻塞原因。

## 以下为 J06d 历史范围

# J06d 验证记录 / J06 当时未验收（2026-09-19）

生产修正 `61ff0875`、导入归属 `66abbdbd`、捕获异常隔离 `dc9c49fb`（契约 `d93bb1e9`）；[代码地图](af-framework-code-map.json) 276 锚点 recorded/working-tree 通过。状态与验证信号集中在[主台账 J06d 当前节](../animusforge-refactoring-and-repository-reorganization-plan.md#j06d-current-verification-20260919)，下方 J05/J04 为历史完成范围。

| 责任 / 一基坐标 | 已接线 | 仍在旧类或未覆盖 |
| --- | --- | --- |
| `src/modules/AF.Module.Knowledge/Index/KnowledgeRuleIndex.cs:39`；`KnowledgeLibraryBehavior.cs:508` | J06a Index owner 接 host live 规则/engine ports，规则版本失效/召回/512 缓存；Index/Lore/Import 70 契约 | Campaign、ONNX 生命周期、知识存档、Hero 内容仍在 KnowledgeLibrary host |
| `src/modules/AF.Module.Knowledge/Lore/LoreCandidateRetriever.cs:30`；`KnowledgeLibraryBehavior.cs:521` | J06b mention/候选/round-robin 纯检索接真实 host | Lore 文本、人物事实与最终格式化留 host；真实 provider 未跑 |
| `src/modules/AF.Module.Knowledge/Entities/{EntityNameMatcher.cs:21,EntityMentionList.cs:8,EntityInjectionAllocator.cs:44}`；`WorldEntityRetrievalService.cs:421` | J06c 纯匹配/mention/分配接消费者，Entities 31 契约 | 游戏候选、位置/距离、称谓和最终块留 host，实机容量未测 |
| `AIConfigHandler.cs:1734,5602–5760,7336`；`PromptRuleEligibility.cs:12`；`MyBehavior.cs:19672,30058,30344` | J06d 11 资格事实在游戏线程捕获，worker 纯判断；异常各门控隔离，排除事实 fail-closed；目标 setter 变动清旧事实；Native/Courier DTO 接线 | 旧 live 分支仍供 Scene/同步 setter-only 调用；真实游戏异常/提前评估副作用未测，J10 Scene 调度未做 |
| `src/modules/AF.Module.Knowledge/Import/KnowledgeImportSupport.cs:12`；`MyBehavior.cs` 12 个导入调用点 | 8 个纯规则/文件读取方法原样迁入，旧体删除；Import fixture 覆盖关键词/When 去重、文件优先级、坏 JSON、缺目录 | `ValidateKnowledgeKeywordsForSingleRuleImport`/`ValidateKnowledgeKeywordsForImport`/`BuildKnowledgeRuleImportFailureMessage` 需当前 Campaign/KnowledgeLibrary，留游戏线程 host；真实玩家数据/旧档未测 |

## 以下为 J05 完成范围与历史

# 当前范围：J05_OFFLINE_VERIFIED（2026-09-19）

当前源码 `d903df67`，[代码地图](af-framework-code-map.json) 262 锚点（9 个搬迁文件的 14 个旧锚点已按新路径重绑，无悬空路径）。唯一状态见[主台账 J05 回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j05-offline-verified-20260919)。

| 责任 | 已迁与接线 | 保留（归属包） |
| --- | --- | --- |
| `src/modules/AF.Module.Memory/Records/` `NpcActionLedger`、`DialogueHistoryLedger`（+搬入 `EventSourceMaterialIndex`） | 行为账本与对话历史账本规则唯一 owner；`MyBehavior` 11 个 helper 删除，Record/Append/Expire/Recovery 四入口改调 | 五个记忆记录类型与 `Sanitize*` 仍是 `MyBehavior` 私有嵌套存档类型（J16/J17 存档类型评估） |
| `src/modules/AF.Module.Memory/Summary/`（6 文件）、`Recovery/`（2 文件） | 由 `Refactor/Runtime|Contracts` 纯 rename，命名空间不变，24 个 tool 工程路径更新 | 逐 record/字符/耗时预算替换每帧回调未实施（需实机数据，J07/J13 复评） |
| `src/AF.Persistence/OwnerJsonStorageCodec` | SyncData 七处 owner→JSON 循环收敛；键策略/空列表/sanitize/失败隔离参数化；key/chunk/字段/日志不变 | `CompressedMemoryExportBundle` 构造/应用直接读写 5 个 host 字段（随存档类型评估） |
| `src/AF.Persistence/PlayerExportsStore`、`NpcDataFileName` | 三 host 重复副本删除（MyBehavior 16、ModOnboarding 3、KingdomStrategicProfile.DevUi 2）；Hero 名解析唯一留 host | 159 个 Import/Export/OpenDev 方法体未重写；Knowledge 导入校验 8 静态方法（J06） |

## 以下为 J04 范围记录

# 当前范围：J04_OFFLINE_VERIFIED（2026-09-19）

当前源码 `8faf5fbe`，[代码地图](af-framework-code-map.json) 253 锚点两模式通过。唯一状态见[主台账 J04 最终回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j04-offline-verified-20260919)。

| 责任 | 已迁与接线 | 保留（归属包） |
| --- | --- | --- |
| `src/modules/AF.Module.Prompt/Composition/` 13 owner | 规则 ID/sticky/路由/ID 收敛/Extras/目标绑定/规则块文本/路由阶段/请求+排除/上下文决策/装配/检索捕获/规则指令拼装；目录零 TaleWorlds/AIConfigHandler/Logger 引用 | — |
| `MyBehavior.cs` Begin/Routing/Complete 三步 + Capture*/Apply* 阶段 | 步骤2 完整 mention 解析；步骤3 段落捕获 207 行全为游戏读 | lore/实体/extra-rule 检索在步骤3（J06）；四个 Add*Exclusions adapter（J07） |
| `ShoutBehavior.NativePromptBuild.cs` / `CourierDeliveryBehavior.PromptSchedule.cs` | 主线程/后台分段 + 逐跳重验 | Scene 五调用点（J10） |

## 以下为 J04f 范围记录

# 当前范围：J04f 完成，Native/Courier 执行位置已搬（2026-09-19，J04_PARTIAL）

当前源码 `52247a51`，[代码地图](af-framework-code-map.json) 250 锚点两模式通过。唯一状态见[主台账 J04f 回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j04f-receipt-20260919)。

| 责任 / 坐标 | 已迁与接线 | 仍在旧类 / 未覆盖 |
| --- | --- | --- |
| `MyBehavior.cs` `BeginSharedPromptBuild` / `RunSharedPromptRouting` / `CompleteSharedPromptBuild` | 三个可调度步骤；旧 builder 为其顺序组合 | 步骤3 内 `GetLoreContext` / `WorldEntityRetrievalService` 仍在游戏线程（J04g） |
| `ShoutBehavior.NativePromptBuild.cs` | 主线程步骤1 → 后台 slot 步骤2 → 主线程步骤3，两次 admission + generation 重验 | — |
| `CourierDeliveryBehavior.PromptSchedule.cs`；`MyBehavior.cs` `BeginCourierRulePreprocess` / `RunCourierRulePreprocessRetrieval` | owner 阶段 × 3 + `Task.Run` × 2，每阶段重验 run/source | — |
| `Composition/PromptRetrievalCapture.cs` | 步骤2 预取 mentions；`PromptBuildPhases` 载体 | lore 预取未做 |
| Scene 八个调用点 | 继续走顺序组合，行为不变 | 无 owner 调度器，J10 处理 |

## 以下为 J04 第二批范围记录

# 当前范围：J04 第二批切片 J04_PARTIAL（2026-09-19）

当前源码 `d6824d9d`，[代码地图](af-framework-code-map.json) 241 锚点两模式通过。唯一状态与命令见[主台账 J04 第二批回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j04-slice2-20260919)。

| 责任 / 一基坐标 | 已迁与接线 | 仍在旧类 / 未覆盖 |
| --- | --- | --- |
| `Composition/PromptRuleBlockText.cs:11` | 注入规则块格式唯一 owner；MyBehavior 36 处调用 | `AfGcczShoutBridge`、`CourierDeliveryBehavior:9550`、`DiplomacyBehavior:902` 仍各自内联标记字符串（领域侧，J11/J12） |
| `Composition/PromptTopicRoutingStage.cs:58,26` | 路由阶段 + host ports | `AIConfigHandler.IsGuardrailSemanticHit` / `GetGuardrailSemanticRuleHitsForPreprocess` 内部 live 资格读取（J03 遗留） |
| `Composition/PromptBuildRequest.cs:12,49` | detached 请求 DTO + 三层排除集合 | 四个游戏派生 Add* adder 仍在 MyBehavior |
| `Composition/PromptContextDecisions.cs:29` | 上下文标志/澄清门控/lore 来源 | — |
| `Composition/PromptAssemblyStage.cs:35` | 纯装配 | — |
| `MyBehavior.cs:30474` orchestrator；`:30557/:30648/:30862` 三个捕获/追加阶段 | 五阶段显式化，行为不变 | **执行位置未搬**：Native 后台线程 / Courier `Task.Run` 仍跑全部阶段；`BuildTriggeredRuleInstructions`/`BuildExtraRuleInstructions` 未段落化 |

Debug/Release 双 API + Bootstrap 六项 0 警告/0 错误；Composition 契约 142 + BuildPhases 源码契约（各 2 变异拒收）；J03 六契约与三渠道 runner 复跑 PASS；实机、旧档、真实 provider `NOT-RUN`。

## 以下为 J04 首批范围记录

# 当前范围：J04 首批切片 J04_PARTIAL（2026-09-18）

当前源码 `11f90fec`，[代码地图](af-framework-code-map.json) 231 锚点两模式通过。唯一状态与命令见[主台账 J04 回执](../animusforge-refactoring-and-repository-reorganization-plan.md#j04-slice1-20260918)。

| 责任 / 一基坐标 | 已迁与接线 | 仍在旧类 / 未覆盖 |
| --- | --- | --- |
| `src/modules/AF.Module.Prompt/Composition/PromptRuleIdPolicy.cs:12` | 规则 ID 集合/排除/规范化/门控/命中排序纯策略；MyBehavior 与 Courier 前处理消费，旧 helper 删除 | 读游戏对象的 `AddPlayerCompanionOrFamilyRuleExclusionsForTarget` 等四个 Add* 适配仍在 MyBehavior |
| `.../BuiltInRuleStickyCarry.cs:12` | duel/reward/loan 跨回合 carry 唯一 owner；存档加载 Clear | 与 J03 `PromptStickyRuleStore` 分别管理，未统一 |
| `.../PromptBuiltInTopicRouter.cs:24` | 八话题路由 + sticky 兜底；语义评估经委托 | `AIConfigHandler.IsGuardrailSemanticHit` 真实 ONNX/关键词评估未迁 |
| `.../PromptPreprocessRuleIdAssembler.cs:17` | preprocess 规则 ID 收敛 | — |
| `.../PromptExtrasComposer.cs:12,47` | Extras 段序/模板/实体规则集/标记检测；host 捕获段落 | 段落文本仍在调用线程 live 读取（Reward/Duel/Army/Weekly/Policy/Lore/Entity） |
| `.../PromptRuntimeTargetBinding.cs:10`；`AIConfigHandler.cs:5666` | 六值目标身份唯一派生/发布口；8 组 setter 块收敛 | `ShoutBehavior.cs` 四 setter 局部调用保留 |
| `MyBehavior.cs:30526` `BuildShoutPromptContextForExternalInternal` | 现约 470 行：排除集合 → 目标发布 → 路由 → 段落捕获 → Compose → ID 收敛 | 线程边界（主线程捕获 → 后台组合 → 重验接受）未做；`BuildTriggeredRuleInstructions` 未段落化 |

Debug/Release 双 API + Bootstrap 六项 0 警告/0 错误（本机 SDK 8.0.422 / 1.4.6 引用，非制作组安装）；J03 六契约、三渠道全部 runner 复跑 PASS；实机、旧档、真实 provider `NOT-RUN`。

## 以下为 J03 历史范围记录

# 当前范围：J03_OFFLINE_VERIFIED（2026-09-18）

当前源码 `e6c82d8d`，导航[代码地图](af-framework-code-map.json)为 221 锚点，recorded/working-tree 均通过（记录源码 `7d8b8d0b`；之后 `d1d3407a` 只补 Scene 测试/地图）。地图是源码坐标证据，不是实机验收；唯一状态与命令见[主台账 J03](../animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)。

| 责任 / 当前一基源码坐标 | 已验证的归属和调用 | 保留适配与非离线范围 |
| --- | --- | --- |
| `src/modules/AF.Module.Prompt/Configuration/PromptConfigurationSnapshot.cs:7-49`；`AIConfigHandler.cs:113-118,1831,1839,7553` | 六份真实模型发布/兼容读取深层隔离，loader/registry/store 同代；配置 36、真实模型 18、命中入口 7，去 pin 变异拒收 | 内部借用模型保持可变但仅旧 owner 读取；实机配置/旧档未跑 |
| `src/modules/AF.Module.Prompt/Retrieval/PromptRuleRetrievalPipeline.cs:36`；`AIConfigHandler.cs:36,4720` | 唯一语义/辅助规则管线；逐调用内部确定性资格/provider 接缝；生产评估 22 覆盖回退、MCM/目标/资格 key、旧代晚结果 | 旧类留真实 ONNX/辅助网络、游戏资格、日志/缓存发布；真实 provider `NOT-RUN` |
| `PromptListRetrievalService.cs:12,190-286`；`MyBehavior.cs:24041-24094,24407-24521`；`RewardSystemBehavior.cs:18449-18530` | 唯一候选 owner；My 生产固定资产消费者 4、Reward 生产装备/商人 11、Scene 原始 hero/merchant 片段直接执行，全量/展示/私装/原顺序/target 与 settlement 隔离；检索 135 | 游戏库存、人物授权和实际转移仍由原 adapter 读取；不以 fixture 代实机 |
| `ShoutBehavior.cs:5664,5733,5763,7136,7232,20090,20221,23011,23235`；`PolicySystem/History/PolicyHistoryRetrievalService.cs:214` | Scene/Native 生产包装 3 验证 mentions 与 trade 标志交付，五类消费者源码调用契约含三个断线变异；Policy 实际程序集 history 1115 | Native/Scene 大方法的游戏域/网络部分仍有替身，旧档/实机不在离线结论 |
| `src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalContextOwner.cs:25`；`AIConfigHandler.cs:1903,1925,1950,2709`；`RagWarmupCoordinator.cs:11,23` | scope 嵌套、异常、提前返回、真实 yield 与 mentions 交付；session/mission/RAG seed 所属线程及旧 worker 跨 reload 门控；检索 fixture 135、评估/warmup 22 | J04 全渠道线程改造不在此包；真实 provider/游戏帧耗时另验 |

Debug/Release 的 Bannerlord 1.3、1.4 和 Bootstrap 六项按原脚本在获准目录预检后均 0 警告/0 错误；两 API 各 789 Compile/7 EmbeddedResource，未 Stage/Deploy/打包。离线性能样本与全部回归数量见主台账；实机、旧档、真实 provider 均 `NOT-RUN`。

## 以下为 J03 历史部分实施范围记录

本次生产配置切片 `01dd8267`：`src/modules/AF.Module.Prompt/Configuration/PromptConfigurationSnapshot.cs:7-49` 在发布与普通读取边界隔离六份嵌套模型；`AIConfigHandler.cs:103-108` 只在旧 owner 内借用已发布对象，不使热路径每次 JSON 拷贝。实际提取的 `AIConfigHandler.cs:5010` 命中入口与 `BuildRulePromptRegistry` 接同 revision 生产测试，配置 36、检索 132、入口 7（去 pin 反例拒收）。借用模型本身仍可变；五类消费者、双 provider 入口及具名 scope 全链路尚未全部验收。代码地图 `a3d6c3d2` 更新两份变化源码的准确行号和摘要，共 220 锚点 recorded／working-tree 通过，`sourceRevision=2c741536`（其后的 `a3d6c3d2` 只含测试/地图）。Debug／Release 双 API 加 Bootstrap 六项 0 警告/0 错误；各版 789 Compile／7 EmbeddedResource，未 Stage/Deploy。当前状态以[主台账 J03](../animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)为准，仍 `PARTIAL / NOT_ACCEPTED`。

生产源码修订 `9242bcfaf3920d07ef2dca6a25b680c1d3c413eb`；[代码地图](af-framework-code-map.json)共 220 锚点，recorded／working-tree 均通过。地图只用于导航，不是 gameplay 或 J03 整体验收。`0916b60c`→`9242bcfa` 的真实差异还包括意图拆分／输入批次与完整召回→rerank→聚合→评估编排迁出旧类、规则后处理 getter 改为副本，以及命中结果与规则正文的外层同 revision pin；不是只刷新源码 hash。

| 责任 / 一基源码坐标 | 已迁与接线 | 仍在旧类 / 未覆盖 |
| --- | --- | --- |
| `PromptConfigurationLoader.cs:54,63`、`PromptRuleRegistry.cs:8`、`RevisionedPromptConfigurationStore.cs:19`、`PromptRevisionedDerivedCache.cs:6`；`AIConfigHandler.cs:1829,7528` | 六份生产 loader 与同 ID 覆盖归 Configuration，完整 replacement 原子换代；内置 RP 文本只缓存原文、每次 fallback 重新建模，避免旧代修改污染新代；静态 registry 和排除提示列表按 revision 懒建，旧 getter/reload 不变 | 配置模型仍内部可变，深层只读未证明；所有旧红／真实 getter 并发契约未全覆盖 |
| `PromptCandidateSelection.cs:24`、`PromptCandidateSnapshotIndex.cs:8`、`IntentQueryOptimizer.cs:9`、`PromptListRetrievalService.cs:12` | 唯一意图算法、纯候选排序及 80-key／10 分钟 store 接旧候选门面 | 游戏对象别名、授权 payload、全量与展示 scope 留适配；My/Reward/Scene/Native/Policy 全消费者生产契约未齐 |
| `PromptRuleIntentSplitter.cs:7`、`PromptRuleIntentInputBatch.cs:6`、`PromptRuleRetrievalPipeline.cs:36`、`PromptRuleSemanticRecall.cs:25`、`PromptRuleIntentSelection.cs:45`、`PromptRuleTextEvidence.cs:8`、`PromptRuleEvaluationAssembler.cs:18`、`PromptAuxiliaryRuleEvaluation.cs:15`；`AIConfigHandler.cs:4712,5010` | 拆分与 2+2 输入批次、seed/vector、逐意图重排/失败回退、跨意图聚合、最终命中与辅助评分均归 Retrieval；MCM 单次捕获、目标资格与 revision 进入唯一 key，生产旧入口调用唯一 pipeline，外层 pin 保证旧代命中与规则正文同源 | `AIConfigHandler.TryGetGuardrailEvalSnapshot` 仍适配真实 ONNX／辅助网络、游戏资格、日志及缓存发布；真实 provider 路径未验，不能把它标成完全薄适配 |
| `PromptAuxiliaryMentionStore.cs:7`、`PromptStickyRuleStore.cs:20`、`PromptSemanticVectorCache.cs:6`、`PromptSemanticWarmupSeedBatch.cs:8`；`AIConfigHandler.cs:2298,2345,6035` | 64 实体 FIFO、三轮 sticky 衰减、1024/256 向量缓存与 seed 捕获归 Retrieval；sticky 跨 reload 保留、旧代结果拒收 | sticky 目标总量原本无上限；实际辅助网络/游戏资格仍留旧适配，实机未验 |
| `PromptRetrievalContextOwner.cs:25`、`PromptRetrievalOperationScope.cs:7`、`AIConfigHandler.cs:2699`；`ShoutBehavior.cs`／`MyBehavior.cs` 具名入口 | AsyncLocal 嵌套 scope 与配置 pin；退出恢复父值，mentions 有显式交付接缝 | 全具名入口的异常/yield/mentions 生产闭包未完整证明，J04 完整三渠道线程捕获不在此包 |

离线已跑：配置 34（含内置 RP 共享旧红及跨 reload 命中规则同代）、检索 128（直接编译生产候选 facade、warmup coordinator 与完整规则 pipeline，含双意图与配置关键词脱离）、Courier prompt 252/59、Courier postprocess 39、Scene parity 71／queue 37／lifetime 30、Native preparation 589／admission 44／completion 184／pending 111／history 852，以及最终生产源码 Debug／Release 原脚本 1.3／1.4／Bootstrap 均 0 警告／0 错误（无 Stage/Deploy；revision pin 后两配置均复跑）。Native History runner 初次因本机 SDK apphost 包版本不一致未启动；`dfe6b12c` 改为禁用 apphost 构建并直接运行生成 DLL，不改任何断言，原 runner 当前普通 852 项／`--native` 27 项均 PASS。PersistenceProfileConfigContract 最初失败 `extra=['synthetic-only-key']`；`de6bd963` 按 csproj 未编译测试源码的事实收窄扫描，并在 168 条 key/ref/type/source 身份完全相同的前提下刷新 52 个准确行号，严格断言未降级，最终 runner PASS；行号和类型内存反例仍被拒收。J03 维持 `PARTIAL / NOT_ACCEPTED`；实机、旧档、真实 provider 均 `NOT-RUN`。详见[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j03-implementation-status-20260918)。

## 以下为 J02 历史范围记录

# 当前增量：完整 J02 Foundation/宿主源码离线完成（2026-09-18）

源码 `9d14a1eca2c25075975134605c24d48666ee123a`，185锚点在记录/工作树两模式通过。当前状态以[唯一台账J02](../animusforge-refactoring-and-repository-reorganization-plan.md#j02-full-completion)为准；下方仅目录生命周期子包是历史，不再代表完整J02未完成。

| 责任 / 已核实路径和一基坐标（源码 `9d14a1ec`） | 实际迁移与消费者 | 保留 / 未覆盖 |
| --- | --- | --- |
| `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs:7-95` | 目录4状态字段、Initialize/Shutdown/CaptureSnapshot唯一owner；Runtime门面接入；本项沿用102eab84并已集成复验 | 目录不等于通用插件热卸载Host |
| `src/AF.Foundation.Runtime/Diagnostics/DiagnosticTraceContext.cs:6-54`、`MetricWindow.cs:6-57` | Logger的AsyncLocal trace/父scope及180秒指标窗口真实退出旧根；Logger BeginTrace:280、Metric:508调用 | Logger记录文本/路径/MCM、TraceScope输出门面保留；RecordHitRate:522-616为J03/J06领域观测 |
| `src/AF.Foundation.Runtime/Diagnostics/BoundedLogWriteQueue.cs:10-147` | 4096/8192背压、drop计数/摘要节流、唯一worker门控/批量/排空；Logger EnqueueLogWrite:1366调用 | UTF8文件sink/清理原位置；Logger tokenStats队列:769-845属J08 LLM消息dump，隐私与领域队列不冒称已解决 |
| `src/AF.Foundation.Runtime/Diagnostics/PerformanceWindow.cs:8-264`、`FreezeWatchState.cs:9-215` | Perf帧/桶/事件/30秒窗口与Freeze心跳、scope、256事件环/缓存唯一owner；根门面接入 | Perf保250ms MCM缓存；Freeze保唯一实际监控线程、游戏现场读取、OS dump、文件sink；无新逐tick委托/扫描 |
| `src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs:6-65`、`GameLifetimeCoordinator.cs:7-39`；`src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs:9-88` | 已提取owner原字节归位：generation、实际Game身份、退役/准入暂停/Seal/reset-clear不重写；四迁移raw SHA相同 | Registry不是第二业务队列；J05记忆预算/J07渠道调度/请求lease与业务SyncData不在J02 |
| `src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs:11-65` | 实际My/Shout/Courier实例捕获/退役；SubModule GameEnd:98-103、Unload:105-112、InitializeGameStarter:120-140继续接线 | 不接管团队业务存档/整个游戏生命周期平台 |
| `src/AF.GameAdapter.Bannerlord/Composition/{CampaignComposition,CampaignModelComposition,TeamModuleRegistration,TeamModuleServices,ModuleFrameworkRuntime}.cs` | 五原owner归位，namespace/类型/内容不变；Runtime:11-36仍工厂与静态兼容门面，未造第二Host | Git100% rename/归一原文证明；迁前raw SHA未捕获，不冒称双向raw哈希 |
| `src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs:7-548`、`ApplicationTickComposition.cs:7-137` | Startup注册顺序/逐组catch与36相位fast/watched/异常finally/WarStats真实移交；SubModule:114-118、142-145仅引擎壳 | SubModule UIExtender/欢迎/Mission/WarStats适配仍必要引擎或J13领域责任；不重写玩法，不改公开ABI |

验证覆盖：C全SubModule严格inverse；B四raw迁移哈希；A关键旧新可执行oracle而非全三门面inverse；指定编译后反例、双API Debug/Release/Bootstrap/本地Stage、4实际DLL/1060、762 Compile/7资源通过。源坐标/契约/构建不等于LIVE/旧SAVE/provider或全模块平台证明。1.3原混合引用与资产许可/用户数据HOLD未解除，不推送/打包/部署。详见台账证据回执。

## 以下为历史范围记录

# 当前增量：J02 目录生命周期真实 owner 提取（2026-09-17）

- 源码修订 `102eab84134ee8e2ab2edb2e25d9f9aa7f560837`；[代码地图](af-framework-code-map.json) 167 锚点在 recorded/working-tree 两模式通过。仅更新本包 7 个既有定位并新增 4 个 owner/转接锚点，其他 156 项保持；一切以[唯一台账](../animusforge-refactoring-and-repository-reorganization-plan.md#parallel-controller-handover)为当前状态。
- `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs:7-95` 唯一持有 4 个状态字段，`Initialize:14-56`、`Shutdown:58-66`、`CaptureSnapshot:68-94` 原同锁算法已退出宿主；`Refactor/Modules/ModuleFrameworkRuntime.cs:11-36` 只选择制作组工厂并保留原静态转接，`RegisterCampaign:22-25` 不受目录状态门控。运行频率为加载/卸载/显式查询，无 tick 扫描或第二 Host。
- 同目录 `InternalModuleDirectory.cs:8-420`、`ModuleFrameworkSnapshot.cs:8-47` 为 Git 100% rename，namespace/type 不变；没有迁前工作树 raw SHA，严格证据为同卷移动、Git 内容相同及归一 inverse，不声称物理字节哈希已对比。新增 owner 与旧门面严格逆组回 `60072f07` 后继续原历史 inverse，不能靠刷新 hash 放行漂移。
- Directory 44、API snapshot36/public119 与并发128/256、Composition42、Native正常/重排各41通过；5 API+5 Composition+8 Native 指定变异均编译成功后被行为断言拒绝。双 API 各 755 Compile/7资源（原754+新owner、两路径映射），Debug/Release 双实现+Bootstrap+Stage通过；当次4实现 DLL API元数据1060通过，6组 artifact/Stage hash一致、XML仅Bootstrap。源码/契约/产物证据不等于 LIVE/旧 SAVE/provider 验收。
- 仍未覆盖：完整 J02 的游戏生命周期、队列/generation/诊断等其他 owner，以及 J03/J05/J08 依赖的剩余职责；本包不解锁全仓广泛提取。资产/用户数据/许可仍 HOLD，1.3 既有混合引用只证明本次选择一致，不证明纯1.3依赖或实机兼容。没有推送、打包、部署或数据迁移。

## 以下为历史范围记录；当前状态以上方为准

# 当前增量：J01 LLM Protocol 原样提取与路径归位（2026-09-17）

- 源码修订 `02f1747c4e226d9c8e187f2503c6197ed6148156`；当前[代码地图](af-framework-code-map.json)的 163 个锚点在 recorded/working-tree 两模式均通过，其中原 142 项逐字段保持，新增 21 项标注协议 owner 与旧宿主实际发送边界。当前结论和完整命令见[根 HANDOFF](../../HANDOFF.md)与[执行台账 J01 当前状态](../animusforge-refactoring-and-repository-reorganization-plan.md#j01-current-status)；地图仅导航，不等于功能/实机验收。
- `src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs:9-235` 真正持有 8 方法/4 常量、无共享可变状态；`ShoutNetwork.cs:260,381,408,424,450,670,751,793,800,911,1018,1253,1256` 的 13 处调用直接接新 owner。`LlmApiCompat.cs:1-720` 与 `LlmVisibleReplyNormalizer.cs:1-485` 原始字节迁入同目录；`StreamFilter:62-144` 状态仍每实例独占。namespace、同 DLL、公开 API/存档身份不变。
- 旧 `ShoutNetwork.cs:133-159,665-905,906-1365` 仍持有真实 HTTP 发送、普通/SSE 调度、取消/重试、配置/统计/姓名过滤，未拆成传输 owner；Scene/Courier/Native 三渠道共享执行/历史/记忆也非 J01 完成项。13 协议用例与 7 变异、Courier/Legacy 和 Debug/Release 双 API+Bootstrap+Stage、实际 DLL ABI 等为**离线**通过；逐字符 Unicode 流发射旧缺陷保留未修。LIVE、旧 SAVE、真实 provider 网络均 NOT-RUN，J02 NOT_STARTED。

## 以下为历史范围记录；当前 J01 状态以上方为准

# 当前增量：并行整合与Native接口（2026-09-16）

源码6e419f6d，142点地图。[详细交接](../handoffs/2026-09-16-parallel-closeout-handoff.md)。summary run owner独立；原文fingerprint固定缓冲等价优化；Courier原Start级reservation与最终Prompt主线程组装；CoreDialogue内部服务和Api.V1显式投影接实际Native入口。

NativeSubmit现为真实可探测能力，Scene/Courier仍NotSupported且必交；不是把全部内部ports公开。旧Native UI与必要Saveable/ABI保留。共享Prompt/lore、B1硬预算、全部反向服务/SDK及主体大类仍未迁完，不能整文件标DONE。最终六Stage/API1056与fixture回归不代替LIVE/SAVE。

## 以下为历史范围记录

# 当前增量：Courier不确定回执（2026-09-15）

生产51844800，125点地图。[最新HANDOFF](../handoffs/2026-09-15-courier-commit-outcome-handoff.md)。CourierDeliveryBehavior.CommitDispatch的私有结果分类和入站转译不再将已开始异常/空回执伪装成无副作用；actual Host不回退重试。没有对外新增API、改变其他主体/团队业务或放行整个阶段8。

## 以下为已有范围记录

# 当前增量：实际 Game 生命周期与队列退役（2026-09-15）

源码29448d1b，124点地图。[详细HANDOFF](../handoffs/2026-09-15-game-lifetime-closeout-handoff.md)。SubModule真实回调绑定Game身份；AfCampaignRuntimeLifecycle只掌管主体My/Shout/Courier寿命。PendingOperationRegistry是原队列的退役登记，不是第二个调度队列；已claim副作用保留真实结果。My先关闭准入再清理，避免新generation/旧singleton竞态。

Courier transport/generation/session/commit/lifetime 已归位 `Channels/Courier` 稳定 partial；同步 registry/creation 之外，主线程队列/节流调度、主动来信增量候选、信件运行时库存现分别位于 `RuntimeTick`、`ProactiveLetters`、`LetterInventory`。SyncData/DTO/key 未移动，既有自动 restore retry `discard_guard` 未改。其余 Prompt message/fact、旧 domain commit/wait、B1预算、双向服务/外部三渠道SDK仍未完成；不把某个 partial 或目录 Ready 当整项完成。

## 以下为已有范围记录

# 当前增量：三渠道人设消费/信使准入与执行回执（2026-09-15）

生产807bc5b9，112点地图。主线程值捕获、原admission/Mission/session/candidate重验和共享生成等待；Courier已claim回执不被取消/超时伪装为未执行。[详细范围](../handoffs/2026-09-15-channel-persona-and-courier-receipt-handoff.md)。其他preprocess/lore/消息live读、GameEnd/B1和SDK仍未完成。

## 以下为既有范围记录

# 当前增量：共享Hero人设请求owner（2026-09-15）

源码043b62b4，105点地图。`NpcPersonaGenerationOwner`持有预约/冷却；`MyBehavior.PersonaGeneration.cs`持有请求捕获/后台解析/主线程接受编排，真实profile/Saveable仍归MyBehavior。正常Hero外部Ensure及编辑器重生已接线，旧3字段/生成体删除。

[当前HANDOFF](../handoffs/2026-09-15-full-closeout-persona-handoff.md)。消费者外围状态读取、升格同伴与完整生命周期仍未覆盖；共享既有调度不等于B1硬预算完成，不把整个MyBehavior标DONE。

## 以下为已有定位与保留责任

# 当前增量：Courier 双向历史捕获（2026-09-15）

生产 `af754ab6`，100 点地图；当前文件已于 J10 归位 `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs`。该 owner 提供历史捕获/检索/接受和旧同步兼容；真实两向 Prepare 已接线。两个 request builder 的旧 live 历史块删除，其余人设/规则/lore/消息构造仍混合运行，不能整文件标 DONE。

[最新 HANDOFF](../handoffs/2026-09-15-courier-history-capture-handoff.md)记录核实范围、兼容消费者与性能缺口；公开 V1 仍只读，全部三渠道提交仍必交。

## 以下为已有定位与保留责任

# 当前增量：共用请求生命周期owner（2026-09-15）

生产73774a94，96点地图。InteractionRequestCoordinator保留公开接入/取消/Dispose，CTS生命周期由内部InteractionRequestLease持有；执行与活动取消回调均结束后释放，旧直接Dispose实现移除。现有facade是真实消费者，三渠道版本化public SDK仍未开放完成。

[main功能矩阵](../phase8/af-core-main-closeout-matrix-20260915.md) / [当前HANDOFF](../handoffs/2026-09-15-main-closeout-lifetime-handoff.md)。本轮不是整个Campaign/Mission或全部主体拆分完成；不得删有活跃调用/序列化/ABI责任的旧owner。

## 以下为已有定位与保留责任

# 当前增量：内部目录快照 / V1 投影分离（2026-09-15）

源码f07cb2a2，91点地图。ModuleFrameworkRuntime仅持有内部装配状态、原Directory和冻结捕获；Api/Internal/AfV1SnapshotProjection承接公开版本映射。公开GetSnapshot签名与能力不变，原混杂映射已删除，CoreOnly无API源码编译通过。

[最新接续与验证边界](../handoffs/2026-09-15-snapshot-boundary-handoff.md)：完整制作组对照已修复接线恢复通过；未完成Campaign/Mission完整生命周期、B1记忆深复制/预算和其他领域大类，不整文件标DONE。

## 以下为既有定位与保留责任

# 当前增量：Campaign 与制作组目录装配职责（2026-09-15）

源码955a6be3、86点地图：现有ModuleFrameworkRuntime委托CampaignComposition注册36行为、CampaignModelComposition注册4包装模型；TeamModuleRegistration创建原typed目录。SubModule净减148行，Runtime净减25行，不新增注册器/全局Campaign状态或public能力。

[详细HANDOFF/实例创建释放表](../handoffs/2026-09-15-composition-extraction-handoff.md)。本轮仅装配边界，业务/存档/其他UI/Harmony/Tick保留原owner；完整生命周期/公共投影进一步分离/业务拆分未完成，不整文件标DONE。

## 以下为既有定位与保留责任

# 当前增量：独立Memory dispatch owner（2026-09-15）

源码61d57892、81点地图：`Refactor/Runtime/MemorySummaryDispatcher`持有唯一队列/待办/预算/异常完成，internal `IMemorySummaryDispatchHost`隔离游戏读；原Host仅57行薄适配，规划两处耗时读取同步迁移。

[详细接续](../handoffs/2026-09-15-memory-dispatch-owner-handoff.md) / [接口契约](af-memory-dispatch-contract.md)。本包不覆盖首次整图capture/copy分段、完整writer或全部Memory owner；旧根大类仍有混合责任，不能整文件标DONE。

## 以下为既有定位与保留责任

# 当前补充：内层结构守卫修复与真正模块化计划（2026-09-15）

生产/测试 `9617f96a`：仅MemorySealing内层List结构绑定修复与关联测试，源码定位图78锚点；同步规则/存档/Prompt/API未变。相同数量变动已旧红新绿，不代表深字段修改或B1硬预算全部闭合。

后续职责迁移按[收尾前真正模块化计划](../phase8/af-core-responsibility-decomposition-plan-20260915.md)；这是未来工作包，不把原大类/partial整文件标成已重写。详见[本轮HANDOFF](../handoffs/2026-09-15-inner-structure-fix-and-modularization-handoff.md)。

## 以下为既有覆盖与保留责任

# AF 框架代码范围图

当前 R2 B1 仅迁移五个 V1 API 源文件的物理位置，保留字节、namespace、程序集与公开 ABI。实际行号/符号见同目录 `af-framework-code-map.json`；以下旧版本记录仍按其原提交解释，不用新路径改写历史证据。此图不是完整功能完成清单，也不把未列到的代码当成可删垃圾。

| 当前责任 | 物理位置 | 对应代码图锚点 |
|---|---|---|
| 纯公开契约 | `src/AF.Contracts/PublicApi/V1/AfApiContracts.cs` | `public.ids` |
| 同 DLL 公开门面与 Native 客户端 | `src/modules/AF.Module.PublicApi/V1/{AfApi,AfDialogueClient}.cs` | `public.api`、`public.dialogue.client`、`public.dialogue.operation` |
| 内部状态到 V1 的投影 | `src/modules/AF.Module.PublicApi/Internal/{AfV1SnapshotProjection,AfV1DialogueProjection}.cs` | `api.snapshot.projection`、`public.dialogue.projection` |

## 以下为原源码版本的历史定位快照

原图是源码 `86805518` 相对于 GitHub 基线 `3f00fefa` 的记录。它不覆盖当前代码图绑定的提交。

当前已包含独立素材索引、Campaign共享预算、稳定队列排序和完整raw摘要写入组件；封存Daily/Major尾部实际消费排序组件。旧索引partial/嵌套预算类及封存原子排序路径已替换；仍被同步调用的Sanitize保留，不误删。主体家族本次为可靠性增加净化边界与状态，不能用这一步宣称大类整体已拆薄。B1深来源/原子净化与真实验收未完成。

## 新旧责任分区（不搬动运行代码）

| 分区 | 当前含义 |
|---|---|
| `Refactor/Modules/` | 新 internal 契约/目录/薄桥，只有选定接缝已接线，非整个制作组业务迁移 |
| `Api/V1/` | 新公开只读接口；未来按需求和兼容证据扩展 |
| 本次具名 Native / Memory partial 边界 | 已接线的主体局部边界，不代表整个 Native / Memory 完成 |
| `ShoutBehavior.cs` / `MyBehavior.cs` / `CourierDeliveryBehavior.cs` / `SubModule.cs` | 新旧混合 owner，按符号标界，不能整文件打 DONE |
| 原 Scene/Courier 默认历史、Native persona/周报/剩余 TTS、压缩记忆 record/time 预算与其他尚未验证的维护 writer | 保留运行责任，未完成部分仍需接续 |
| 政策 / 宴会 / `AnimusForge.SiegeAftermathIntervention` 业务 | 本轮不重写，只处理 AF 侧接口，不误删旧业务 |
| GitHub 主分支及其他旧版本 | 分支/固定基线隔离，不复制到活动编译目录，不整片覆盖当前重构分支 |

## 已核实代码坐标

以下一基行号均属于源码 `9158132c`，仅为导航，不代表整个方法的改动量。用符号和固定提交重新定位。

| 边界 | 源码位置 | 符号 / 责任 | 状态 |
|---|---|---|---|
| `internal.port.policy` | `Refactor/Modules/TeamModulePorts.cs:7-10` | `internal interface IPolicyModulePort` — 政策 typed 接缝，业务归原 owner | `wired-boundary` |
| `internal.port.gathering` | `Refactor/Modules/TeamModulePorts.cs:17-20` | `internal interface IGatheringModulePort` — 宴会 typed 接缝，不迁移玩法 | `wired-boundary` |
| `internal.port.siege` | `Refactor/Modules/TeamModulePorts.cs:27-30` | `internal interface ISiegeModulePort` — GCCZ 接缝，保留原场景门禁 | `wired-boundary` |
| `internal.adapters` | `Refactor/Modules/TeamModuleAdapters.cs:7-10` | `internal sealed class PolicyModuleAdapter : IPolicyModulePort` — 同文件三组薄桥原样转接参数/返回/ref/out | `wired-boundary` |
| `internal.services` | `Refactor/Modules/TeamModuleServices.cs:5-8` | `internal static class TeamModuleServices` — 无状态 typed 单例装配 | `wired-boundary` |
| `internal.directory` | `Refactor/Modules/InternalModuleDirectory.cs:137-140` | `internal sealed class InternalModuleDirectory` — 注册/冻结/依赖校验，非游戏执行授权 | `wired-boundary` |
| `internal.runtime` | `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` | `internal static class ModuleFrameworkRuntime` — 装配与只读投影，不是第二套执行器 | `wired-boundary` |
| `public.api` | `Api/V1/AfApi.cs:13-16` | `public static class AfApi` — 当前只读；其他提交/写能力未开放 | `readonly-api` |
| `public.ids` | `Api/V1/AfApiContracts.cs:35-38` | `public static class AfCapabilityIds` — 公开 ID 和同文件 DTO，可按兼容版本演进 | `readonly-api` |
| `lifecycle.load` | `SubModule.cs:60-63` | `ModuleFrameworkRuntime.Initialize(out string moduleFrameworkReason);` — 模块装配，不是 Campaign Ready | `mixed-host` |
| `lifecycle.unload` | `SubModule.cs:110-113` | `ModuleFrameworkRuntime.Shutdown();` — 发布停止状态 | `mixed-host` |
| `native.admission` | `ShoutBehavior.NativeAdmission.cs:17-20` | `internal sealed class NativeConversationAdmission` — 准入/忙碌/会话绑定，不是完整公共提交服务 | `wired-boundary` |
| `native.preparation` | `ShoutBehavior.NativePreparation.cs:14-17` | `private sealed class NativeConversationPreparationSnapshot` — 仍含原 Location 引用，不是公共不可变 DTO | `wired-boundary` |
| `native.history.capture` | `MyBehavior.HistoryPromptSnapshot.cs:35-38` | `internal static Func<string> CaptureHistoryContextWorkById` — 召回用途投影，非全局记忆事务 | `wired-boundary` |
| `native.history.bridge` | `ShoutBehavior.cs:16180-16183` | `private static Func<string> CaptureNativeConversationPersistedHistoryWork` — 原身份解析在主线程捕获，非全 Shout 重写 | `mixed-host` |
| `native.history.dispatch` | `ShoutBehavior.cs:20159-20162` | `"persisted_history_capture", nativeTargetLog, nativeTargetAgentIndex,` — 原 admission 验证后捕获 work | `mixed-host` |
| `native.history.accept` | `ShoutBehavior.cs:20201-20204` | `if (!await RunNativeConversationMainThreadFuncAsync("persisted_history_accept"` — 使用结果前再验原 admission | `mixed-host` |
| `native.dispatch.shared` | `ShoutBehavior.cs:19437-19440` | `private Task<T> RunNativeConversationMainThreadFuncAsync<T>` — 排队/开始/退休，不假称网络已取消 | `mixed-host` |
| `memory.acceptance` | `MyBehavior.DialogueHistoryCommit.cs:12-15` | `internal static MemoryCommitResult CommitDialogueHistoryWithScene` — 运行期接受，不是磁盘/跨动作事务 | `wired-boundary` |
| `memory.failure.ui` | `MyBehavior.MemoryFailureNotice.cs:53-56` | `private void ProcessPendingMemoryFailureNotice()` — 原 EngineTick 消费；owner/Campaign/generation/revision | `wired-boundary` |
| `memory.summary.dispatch` | `MyBehavior.MemorySummaryMainThread.cs:58-61` | `private Task<bool> RunMemorySummaryMainThreadAsync(long generation, Func<bool> operation)` — 主线程剩余额度内直达，否则排队；原 owner/generation/Campaign 与退休门禁保留 | `wired-boundary` |
| `memory.summary.drain` | `MyBehavior.MemorySummaryMainThread.cs:128-131` | `private void ProcessMemorySummaryMainThreadActions()` — inline/queued 共用两次操作和实际累计耗时；超预算不启动下一操作，单原子与record预算仍未完整 | `wired-boundary` |
| `memory.summary.accept` | `MyBehavior.cs:4961-4964` | `if (ApplyMemorySummarySuccess(result.Job, result.Block)) appliedDaily++;` — 源重验紧接真实Apply；部分/未知错误通知，不盲重放或假报成功 | `mixed-host` |
| `legacy.history` | `MyBehavior.cs:27835-27838` | `public static string BuildHistoryContextForExternal(` — Scene/Courier 仍调用，共享兼容入口不能盲删 | `retained-live` |
| `legacy.recall` | `MyBehavior.cs:33841-33844` | `private string BuildCompressedMemoryContextById(` — snapshot 和默认旧调用共用原算法 | `mixed-host` |
| `scene.postprocess` | `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs:25-28` | `private sealed class SceneActionPostprocessWorkItem` — 已有完整后处理；J10 将真实队列/完成 owner 归入 Scene 渠道目录，玩法不重写 | `wired-j10-candidate` |
| `courier.prepare.reply` | `CourierDeliveryBehavior.cs:4674-4677` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(recipient,` — 回信早期准备待线程审查，未迁快照 | `retained-live` |
| `courier.prepare.inbound` | `CourierDeliveryBehavior.cs:5102-5105` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(sender,` — 来信早期准备待线程审查，未迁快照 | `retained-live` |
| `memory.summary.capture` | `MyBehavior.MemorySummaryInput.cs:252-255` | `private MemorySummaryInput CaptureMemorySummaryInput(` — 三类唯一初捕获/复制/原Build，raw与effective context分离；首次绑定检查，深记录原子成本仍未收口 | `mixed-host` |
| `memory.summary.source-check` | `MyBehavior.MemorySummaryInput.cs:338-341` | `private bool IsMemorySummaryInputCurrent(` — 不重新Capture/Clone/Build；context后fresh raw摘要与动态资格，retarget先拒绝，不靠Save-only epoch | `mixed-host` |
| `memory.summary.execute` | `MyBehavior.MemorySummaryInput.cs:357-360` | `private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(` — 共用原 provider/Build/Parse；主线程解析、波次/重试退休，完成后释放大 payload | `mixed-host` |
| `memory.source.facades` | `MyBehavior.MemorySourceWrites.cs:22-25` | `private static bool DeferMemorySourceWriteIfNeeded(` — 旧 void façade 主线程同步、后台 owner/generation 排队；不是持久接受回执 | `mixed-host` |
| `memory.summary.rendering` | `PlayerNotorietyBehavior.cs:203-206` | `internal static string CaptureMemorySummaryHistoryRenderingIdentity()` — 无 observer 公称/实际匿名别名纳入 daily 解析来源身份 | `mixed-host` |
| `memory.summary.copy` | `MyBehavior.MemorySummaryInput.cs:46-49` | `private static T CloneMemorySummarySource<T>(T value)` — 10种模型和2种列表的typed复制；各CopyForSummary分离可变图 | `mixed-host` |
| `memory.summary.fingerprint` | `MyBehavior.MemorySummaryInput.cs:323-326` | `private static string ComputeMemorySummaryFingerprint(object identity)` — raw和有效context各自流式SHA256；不再把完整Prompt反复纳入来源digest | `mixed-host` |
| `memory.summary.time` | `MyBehavior.MemorySummaryMainThread.cs:20-23` | `private bool HasMemorySummaryMainThreadAllowance()` — 复用既有维护毫秒配置，实际Stopwatch累计；非同步抢占 | `mixed-host` |
| `memory.summary.partial` | `MyBehavior.MemorySummaryMainThread.cs:88-91` | `private async Task<bool> RunMemorySummaryCompletionAsync(long generation, Func<bool> operation)` — 仅协调器传播operation异常，区分拒绝/取消与部分执行失败 | `mixed-host` |
| `memory.summary.admission` | `MyBehavior.cs:4859-4862` | `private void TryStartMemorySummaryQueue(bool forceOverviewCandidateScan = false)` — raw数量入场；候选ID扫描仍有同步全扫，不代表全部入口已预算 | `mixed-host` |
| `memory.summary.planner` | `MyBehavior.cs:4921-4924` | `private async Task ProcessMemorySummaryQueueAsync(bool forceOverviewCandidateScan = false)` — 分段初筛/extra/cleanup，冻结metadata排序与失败汇总在worker；仅完整接受才计数 | `mixed-host` |
| `memory.summary.maintenance` | `MyBehavior.cs:17759-17762` | `private void TryRunCampaignMemoryMaintenance()` — 不在每Tick重复读完整summary来源；past draft检查仍有全扫 | `mixed-host` |
| `memory.plan.entry` | `MyBehavior.MemorySummaryPlanning.cs:13-16` | `private sealed class MemorySummaryPlanEntry` — 冻结job标记/排序键，Job只作opaque原引用，非新持久owner | `mixed-host` |
| `memory.plan.scan` | `MyBehavior.MemorySummaryPlanning.cs:69-72` | `private async Task<List<MemorySummaryPlanEntry>> ScanMemorySummaryQueueAsync<T>(` — 每片8槽，当前片tombstone；纯引用compaction在结构仍有效时发布，变化则部分defer | `mixed-host` |
| `memory.plan.build` | `MyBehavior.MemorySummaryPlanning.cs:159-162` | `private async Task<MemorySummaryPlan> BuildMemorySummaryPlanAsync(` — 独立重验无效owner；worker仅按冻结metadata去重排序，cleanup不建多余计划 | `mixed-host` |
| `memory.editor.guard` | `MyBehavior.MemorySourceWrites.cs:13-16` | `private bool IsMemorySourceEditorCurrent(long generation)` — 开窗generation、物理主线程、Instance和Campaign owner同时验证 | `mixed-host` |
| `memory.editor.text` | `MyBehavior.cs:50038-50041` | `private void OpenDevDailyMemoryLineTextEditor(` — 文本保存/取消真实红绿证据；同代记录引用/指纹也须保持 | `mixed-host` |
| `memory.import.single` | `MyBehavior.cs:54876-54879` | `private void ImportSingleNpcDialogueHistoryData(` — 单NPC导入窗口生命周期，真实文件路径/ReadJson/选择/Apply受控回放 | `mixed-host` |
| `memory.import.batch` | `MyBehavior.cs:56981-56984` | `private void ImportDialogueHistoryData(` — 记忆批量导入生命周期，保留overwrite/merge业务 | `mixed-host` |
| `memory.import.hero-all` | `MyBehavior.cs:55073-55076` | `private void ImportHeroNpcAllData(` — 仅AF汇总导入窗口门禁；业务owner不重写，本轮结构验证 | `mixed-host` |
| `memory.import.all` | `MyBehavior.cs:57509-57512` | `private void ImportAllData(` — 仅AF全量导入窗口门禁；非全部导入业务已运行验收 | `mixed-host` |
| `memory.summary.raw-view` | `MyBehavior.MemorySummaryInput.cs:126-129` | `private MemorySummarySourceView ReadMemorySummarySource(` — 主线程/owner/队列/目标资格，直接读取 raw 字典状态；不把 live view 留给异步请求 | `mixed-host` |
| `memory.summary.scene-dependencies` | `MyBehavior.MemorySummaryInput.cs:88-91` | `private static void DescribeMemorySummaryDailyContext(` — 首次构建有序 header / 非空正文场景依赖；明确场景和无效设置变动不误退 | `mixed-host` |
| `memory.summary.effective-context` | `MyBehavior.MemorySummaryInput.cs:205-208` | `private string CaptureMemorySummaryContextFingerprint(` — 实际有效目标字数、写作要求、目标 observer、名字 resolver、解析身份 | `mixed-host` |
| `memory.overview.pending-projection` | `MyBehavior.cs:26803-26806` | `private bool HasMemoryOverviewPendingBlocks(` — 一轮资格投影，保留原始ID占位、计数和非幂等标题；不复制/排序无关大图 | `mixed-host` |
| `memory.material.index-owner` | `Refactor/Runtime/EventSourceMaterialIndex.cs:11-14` | `internal sealed class EventSourceMaterialIndex<T> where T : class` — 独立派生索引/绑定 owner，无游戏和存档写入 | `source-linked-offline-verified` |
| `memory.material.index-build` | `Refactor/Runtime/EventSourceMaterialIndex.cs:39-42` | `internal Dictionary<string, T> Build(List<T> source)` — 未发布重建、命名last-wins/空键first-wins | `source-linked-offline-verified` |
| `memory.material.record` | `MyBehavior.cs:13707-13710` | `private void RecordEventSourceMaterial(` — 原记录/追加/发布仍归主体 owner，使用独立索引 | `source-linked-offline-verified` |
| `memory.material.rebuild` | `MyBehavior.cs:20162-20165` | `private void RebuildEventSourceMaterialIndex()` — 重建后复核来源引用，再发布和绑定 | `source-linked-offline-verified` |
| `memory.sealing.continue` | `MyBehavior.MemorySealing.cs:180-183` | `private bool ContinueDailyMemorySeal(long startTimestamp, double budgetMs, bool requirePendingProbe)` — 有限Campaign窗口共享封存授予；独立/同步调用与深原子成本单列 | `source-linked-offline-verified` |
| `memory.budget.runtime` | `Refactor/Runtime/MemoryMaintenanceWorkBudget.cs:10-13` | `internal sealed class MemoryMaintenanceWorkBudget` — 独立协作预算窗口；不抢占单次深操作 | `source-linked-offline-verified` |
| `memory.budget.resolve` | `MyBehavior.MemoryMaintenanceBudget.cs:16-19` | `private void ResolveDailyMaintenanceBudget(` — 有限Campaign周期懒创建共享窗口，空闲不读取预算设置 | `source-linked-offline-verified` |
| `memory.budget.cycle` | `MyBehavior.cs:17733-17736` | `private void RunCampaignMemoryMaintenanceCycle(` — 真实主/deferred维护共享周期与异常/nested恢复 | `source-linked-offline-verified` |
| `memory.budget.caller` | `MyBehavior.cs:17687-17690` | `private void OnCampaignTick(float dt)` — 实际Campaign入口接入共享维护周期，其他顺序保持 | `source-linked-offline-verified` |
| `memory.budget.deferred` | `MyBehavior.cs:5923-5926` | `private void ProcessDeferredDailyMaintenance()` — 复用共享deadline，原子超时后不再开始下一维护域 | `source-linked-offline-verified` |

## 核对或更新

运行仓库 Skill 的 `scripts/verify_code_map.py`，默认按记录的 Git 提交验证符号、一基行号和内容摘要；加 `--working-tree` 核对当前文件。主体变化后，按新源码更新 JSON、此表与 HANDOFF，保留历史提交作对照。不能只刷新行号/hash 就宣称新功能通过验收。

本轮完整raw摘要接入独立4096-byte buffer writer，私有DTO122字段映射留owner边界；不迁移存档类型，不修改通用JSON摘要/Prompt/权威写入。对应字段与code-unit反例和原版成本对照已验证，但仍完整原子O(N)，不是深来源预算或主体总拆薄完成。

owner封存尾部已逐draft计费并复用稳定排序；单draft内line净化与weekly trigger bind现按共享metadata计费，同步Sanitize与续跑共用原line/bind规则，未完成draft的列表保持私有。trigger列表sanitize、首次capture/copy、全owner绑定和Apply仍有原子工作；当前不按“大类行数减少”或“全部拆完”交付。
