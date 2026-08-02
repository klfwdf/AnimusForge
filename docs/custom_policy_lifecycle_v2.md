# 自定义政策效果与条件生命周期 v2

## 兼容原则

- 未返回 `lifecycle` 的政策继续使用原有 `durationDays + effects` 固定时长流程。
- 旧存档缺少的新字段按 `0` / 全王国原范围处理；活动效果存档版本由 4 升到 5，但不拒绝读取旧版本。
- 条件生命周期只对全国政策开放；地方政策仍立即生效、固定时长、不进入王国条件状态机。
- AI 必须先判断执行机制：政策利益依赖限期验收、持续条件或失败后果时返回 `conditional`；纯行政常态继续使用普通固定期限。代码不按关键词替 AI 决定。

## 新增指标

| metric | 作用 | 运行时挂点 |
|---|---|---|
| `volunteerProductionPct` | 空缺志愿兵槽位的日补充概率百分比变化 | `VolunteerModel.GetDailyVolunteerProductionProbability` |
| `volunteerUpgradeRatePct` | 已有志愿兵进入原版升级流程的日概率百分比变化 | 同上，按槽位当前是否已有兵种区分 |
| `clanInfluencePerDay` | 氏族每日影响力固定变化 | `ClanPoliticsModel.CalculateInfluenceChange` |

志愿兵概率最终限制在 `0..1`；影响力通过 `ExplainedNumber` 加入，因此原版说明界面仍能显示政策来源。

两个志愿兵指标不绑定战争、围剿、征服或其他固定政策类型。AI 只按具体措施的直接因果决定是否输出：战争政策可以没有志愿兵变化，非战争的人口、征募制度或训练改革也可以产生变化；补充速度与精锐化速度彼此独立判断。

## 主体选择器

全国 effect 可带：

```json
{
  "subject": {
    "kind": "rulerFiefs",
    "minClanTier": 3,
    "maxClanTier": 6
  }
}
```

| kind | 用途 |
|---|---|
| `rulerClan` | 统治氏族影响力 |
| `vassalClans` | 非统治氏族、非雇佣兵氏族影响力 |
| `allMemberClans` | 全部正式成员氏族影响力 |
| `rulerFiefs` | 统治氏族领地的定居点、经济与征募效果 |
| `vassalFiefs` | 封臣领地的定居点、经济与征募效果 |
| `allKingdomFiefs` | 全王国领地；等价于旧政策默认范围 |

`minClanTier` / `maxClanTier` 可选，范围固定为 0..6。氏族影响力指标必须使用氏族主体；定居点、税收、建造、志愿兵指标只能使用领地主体。编译器会拒绝混用，避免“字段写了但运行时没有目标”的静默失效。

AI 提示规则不是机械零和：

- 明确集权、强化统治氏族的政策，应考虑封臣不受益或承担对应代价。
- 全国动员、共同丰收、普遍减税等语义明确时，可以让统治氏族与封臣同时受益。
- 同一王国可拆成多条不同 subject 的 effect，因此统治者与封臣可获得不同方向和数值。

## 条件生命周期

普通政策仍使用：

```json
{
  "durationDays": 60,
  "effects": []
}
```

条件政策改用：

```json
{
  "durationDays": 60,
  "lifecycle": {
    "kind": "conditional",
    "initialPhase": "grace",
    "graceDays": 14,
    "fulfillmentCondition": {
      "type": "warDeclaredAfterEnactment",
      "target": "ANY_FOREIGN"
    },
    "maintenanceCondition": {
      "type": "isAtWarWithRecordedEnemy"
    },
    "failureToleranceDays": 3,
    "recoveryMode": "none",
    "penaltyDurationDays": 21,
    "breachOnAbolition": true,
    "renewalMode": "none"
  },
  "phases": [
    { "id": "grace", "effects": [] },
    { "id": "maintained", "effects": [] },
    { "id": "breached", "effects": [] }
  ]
}
```

运行顺序：

1. `grace`（履约期限）：准备/宽限期，只应用 grace 效果。
2. 达成 `fulfillmentCondition` 后进入 `maintained`；超时则进入 `breached`。
3. `maintained` 可持续检查 `maintenanceCondition`，连续失败超过容忍天数后违约。
4. `breached` 只应用违约效果，并按 `penaltyDurationDays` 计时。
5. 条件政策提前废止会强制进入违约阶段，不能通过撤销政策规避负面效果。
6. `recoveryMode=automatic` 时，非提前废止导致的违约可在维持条件恢复后回到收益阶段；提前废止的惩罚必须执行完毕。

支持的条件：

- `warDeclaredAfterEnactment`（必须由政策所属王国作为宣战发起方；被动遭到宣战不算履约）
- `isAtWarWithAny`
- `isAtWarWithTarget`
- `isAtWarWithRecordedEnemy`
- `activeWarCountAtLeast`
- `rulingClanTierAtLeast`
- `settlementCountAtLeast`
- `kingdomStabilityAtLeast`
- `targetFiefCountAtMost`（目标王国剩余城镇/城堡数不高于 `value`；完全吞并使用 `0`）
- `targetKingdomEliminated`

条件中的王国只能引用本次提示提供的 `K*` 句柄，不能由模型伪造 StringId。

## 状态与运行时

- 生命周期状态独立保存到 `_afPolicyLifecycleStates_v1`。
- 活动效果保存 `PhaseId` 与 `Subject`；模型缓存只收集当前阶段效果。
- `DailyTick` / 引擎维护推进期限和维持条件；`WarDeclared` 捕获“生效后宣战”；`MakePeace` 进入统一事件观察日志，维持条件在每日检查时决定是否违约。
- 阶段切换会清理上一阶段未完成的分批结算、刷新税收/建造/征募/影响力缓存，并更新政策历史。
- 玩家会收到阶段切换和生命周期结束提示；历史记录同时显示当前阶段与各阶段效果。
- 王国政策页和发布结果会明确显示 AI 判定：普通固定期限，或 `grace（履约期限） → maintained（条件维持阶段） → breached（违约负面阶段）`；条件政策的每条数值效果标注所属阶段。
- `PolicySystem.txt` 的 `active-created` 记录包含主体、阶段、志愿兵补充、志愿兵精锐化和氏族影响力，便于游戏内验证。

## NPC 政策

NPC 固定时长政策同样支持三个新增指标、六种主体和家族等级过滤。同一目标王国允许按不同主体拆成多条 effect，因此 NPC 可以生成“统治氏族增强、封臣削弱”或“全国共同动员”等差异化结果。NPC 条件生命周期暂不启用；其原有固定时长生成和续期流程保持不变。
