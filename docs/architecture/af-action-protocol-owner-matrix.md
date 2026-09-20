# AF Action protocol owner matrix（J09 G0）

> 基线：`fd01974b` 之后的 J09 工作树；最终坐标以代码地图为准。  
> 本表是责任/兼容盘点，不是新增玩法授权。Prompt 配置、parser 允许和真实执行是三种不同证据。

## 1. 盘点结果

- `RuleBehaviorPrompts.json` 当前检出 **63** 个唯一动作标签示例/模板；`ActionPostprocessPrompts.json` 检出 **8** 个全局标签。
- 运行期兼容目录有 **67** 个有限 family/template；parser 只识别 `ACTION/A/ADP/AD/ASS/GUI/ATT/ATP/RELAY/FOL/STP/END` 12 个协议族。
- `AFEF`、`CONTENT`、`AF_SCENE_SESSION` 是事实/内容/会话标记，不属于 ActionPlan，不加入 parser。
- Native、Scene、Courier 的 detached 路径共同使用 `src/modules/AF.Module.Actions/Tags/LegacyActionTagParser.cs`；无绑定的同步/异步入口必须产生零动作。
- 默认三渠道的领域执行仍由各自游戏线程 adapter 负责；J09 不把 Reward、Duel、Policy、Gathering、GCCZ 等玩法搬进 Actions。

## 2. 协议族与 owner

| family / template | Prompt / 资格来源 | 运行期执行 owner | 渠道/限制 | receipt / 事实边界 |
| --- | --- | --- | --- | --- |
| `ACTION:GIVE*`、`GIVE_ASSET`、`GIVE_GOLD`、`GIVE_ITEM`、`SETTLEMENT_TRANSFER` | reward / loan / transaction rules | Economy typed planner/port；未迁领域由 `RewardSystemBehavior` adapter | 三渠道；资产、数量、目标在主线程复核 | 只接受 owner confirmed facts；partial/unknown 不伪造全成功 |
| `AD`、`ADP`、`TRADE_TRUST` | debt / transaction rules | Reward/Debt owner | 三渠道；`TRADE_TRUST` 当前可能被领域 owner 明确忽略 | debt/economy receipt；不能仅凭标签写事实 |
| `ACTION:DUEL*`、`ACTION:MOOD` | duel/global mood rules | Duel request-bound owner；Mood memory adapter | Native/Scene；Courier Duel 显式拒绝 | Duel queued/started/unknown receipt；Mood 不冒充 Duel 成功 |
| `A:H_J_P_P_C&L` 及 `A:C_J_*`、`A:P_J_*`、`A:P_L_K` | kingdom service / hero join rules | Reward/party/kingdom legacy adapter | 按 Hero/非 Hero/野外 party 资格；模板本身不可执行 | 只有实际身份/队伍变化后写事实 |
| `ACTION:KINGDOM_*`、`JOIN_*`、`VASSALAGE`、`KING_ABDICATE_TO_PLAYER` | kingdom service/vassalage/royal rules | Reward、Vassalage、Kingdom owners | 仅具备对应王国/家族资格的请求 | 领域确认后写事实；stale 拒绝 |
| `ACTION:DIPLOMACY` | diplomacy rule 或独立家族和平 resident rule | `DiplomacyBehavior` | 三渠道按显式资格；目标 faction 在主线程复核 | 外交状态实际变化后确认 |
| `ACTION:AGENDA`、`ACTION:VOTE_DEAL` | kingdom agenda/custom policy rules | VoteDeal + Policy typed seam | 当前具体配置使用 `AGENDA:*`；Policy 只经内部制作组接缝 | 提案/交易 owner receipt；政策玩法不进通用 Actions |
| `ACTION:NOBLE_*` | noble gathering / prisoner rules | Gathering internal port、prisoner owners | 由规则/场景/目标 gate 限制 | 各领域 confirmed facts/notifications |
| `ACTION:SCENE_*`、`ASS`、`GUI`、`FOL`、`STP`、`END` | scene mechanism rules | Scene game adapter | Scene/Native 有 live Agent 时；Courier 默认排除 | 场景动作完成后确认；移动/关闭时序归渠道 |
| `RELAY` | Scene group/relay rule | Scene relay coordinator | 仅 Scene；属于渠道控制，不是任意领域动作 | 不生成虚假游戏事实；会话 epoch/目标复核 |
| `ATT`、`ATP` | party transfer rule | Party transfer owner | 三渠道按实际 roster/数量资格 | 只记录实际转移数量 |
| `ACTION:WORLDMAP_ORDER` | worldmap command rule | `WorldMapPartyCommandBehavior` | 需要大地图/party 资格；Mission 场景拒绝 | 命令实际接受后确认 |
| `ACTION:MARRIAGE_*`、`DIVORCE`、`LOVE_DELTA`、`INTIMACY_INTERNAL` | marriage/global intimacy rules | Romance / SexualConception owners | intimacy 明确排除 Courier；年龄/关系/性别等重验 | 领域状态实际变化后确认 |
| `ACTION:ISSUE_*`、`QUEST_TURN_IN` | vanilla issue rule | `VanillaIssueOfferBridge` / issue owner | 当前目标和任务状态重验 | 接受/交付成功后确认 |
| `ACTION:MEETING_TAUNT_*`、`LET_PLAYER_GO`、`NPC_SURRENDER`、`LORDS_HALL_BRIBE_PRICE`、`OPEN_LORDS_HALL`、`TROOP_INSPECTION_*` | encounter/hall/surrender/inspection rules | LordEncounter、SceneTaunt、TroopInspection 等领域 owner | 相应 Encounter/Mission/目标 gate | owner-started 异常记 unknown，不自动重试 |
| `ACTION:1..12`、`CASTLE_*`、`TOWN_*`、`SETS_*` | 仅 active GCCZ/攻城处置规则 | `TeamModuleServices.Siege` 薄桥 → GCCZ owner | 只在明确 active siege-intervention stage；普通 AF 对话不得获权 | GCCZ outcome/receipt；AF 不复制玩法 |
| `ACTION:PROPOSE`、`ACTION:PEACE`、`ACTION:VOTE_DEAL` 的旧短名 | 当前 Prompt 未检出稳定 producer | legacy compatibility adapter | 保留以避免未审计外部/旧调用 ABI 变化；不得仅凭目录出现而执行 | 没有真实 owner 确认就拒绝、无事实 |

## 3. 唯一共享 owner 与保留边界

```text
src/modules/AF.Module.Actions/
├─ Tags/
│  ├─ LegacyActionTagCatalog.cs       # 有限兼容目录
│  └─ LegacyActionTagParser.cs        # balanced scanner + detached ActionPlan
├─ Plan/
│  └─ ActionPlanIntegrityPolicy.cs    # raw/plan 有序逐字段一致性
├─ Execute/
│  └─ LegacyNativeActionPlanExecutor.cs # Economy/Duel typed ports + legacy adapter
└─ Receipts/
   ├─ InteractionResultCommitter.cs
   └─ InteractionCommitReceiptCache.cs
```

- 命名空间/可见性保持兼容；物理归位不等于已删除 legacy adapter。
- `LegacyNativeActionPlanExecutor` 的 legacy callback 仍承载尚未进入 J12/J13 typed port 的领域；这是活跃兼容职责，不是可删转发壳。
- 带存档/领域生命周期的 Duel、Weekly、Notoriety、Courier inbound receipt 暂留其领域 owner；Actions 只读取已声明接口。
- 默认 Native/Scene 的完整渠道提交切换仍需 J09d 与 J10 生命周期共同验证，不能因 opt-in facade 已通过就宣称默认链全部切换。

## 4. 清理候选与禁止项

- `Refactor/Adapters` 下旧 Tags/Executor 物理路径已由真实 Compile/测试消费者迁走；不保留副本。
- 不新增 `ACTION:*` 全通配，不把 UI 标签目录当执行授权。
- 不删除旧短名兼容项，直到外部/反射/Prompt/执行消费者清零证据齐全；当前仅将其标为 compatibility-only。
- 不把 `AFEF`、`CONTENT`、session marker 解析成动作。

