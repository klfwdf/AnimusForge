# Army Member Custom Meeting Target Case

本文档记录“军团选择成员后，AnimusForge 自定义会面仍导向军团长”的已验证修复案例。这里的“军团成员”指大地图遭遇军团时，原版先弹出的成员选择界面里选中的某个附属队伍首领。

## 问题现象

在大地图接触军团时，原版流程会先进入军团成员选择界面。玩家选择某个非军团长成员后，后续进入 AnimusForge 自定义会面菜单，但菜单目标、会面对象或后续交互仍然变成军团长。

典型表现：

- 选择军团长时正常。
- 选择军团其他成员时，自定义会面菜单仍显示或使用军团长。
- 连续选择不同成员时，目标可能看起来被正确拦截过，但菜单初始化、tick 或点击后又回到军团长。

## 根因

军团遭遇中的 `PlayerEncounter.EncounteredParty` 通常代表当前遭遇方的主队伍，`EncounteredParty.LeaderHero` 很容易是军团长，而不是玩家在原版成员选择界面里选中的附属队伍首领。

AnimusForge 自定义会面链路不止一个入口会决定目标：

1. 原版 map conversation / conversation start 阶段会传入玩家选中的成员。
2. AnimusForge patch 在这些入口中解析目标并写入 `_targetHero`。
3. 自定义菜单打开、初始化、条件判断、tick、点击时还会反复调用 `EnsureEncounterTargetHero(...)`。

本次 bug 的关键在第 3 层：即使前面的 patch 已经把 `_targetHero` 设置成选中的军团成员，`EnsureEncounterTargetHero(...)` 仍可能直接通过 `PlayerEncounter.EncounteredParty.LeaderHero` 刷新目标，导致 `_targetHero` 被覆盖回军团长。

因此，单纯修“入口解析选中成员”不够；还必须修“后续菜单刷新不能覆盖一个仍然合法的已选目标”。

## 正确规则

1. 入口解析要优先从原版传参里取选中对象。
   `ConversationCharacterData`、`MobileParty`、`PartyBase`、`MapConversationAgent.Character` 等参数都可能携带玩家选中的军团成员。只有这些都解析不到时，才允许回退到遭遇队伍首领。

2. 合法性判断不能只接受 `EncounteredParty.LeaderHero`。
   对军团遭遇，选中的目标可以是当前军团中附属队伍的 `LeaderHero`。如果只认遭遇主队伍队长，就会把合法成员误判为无效目标。

3. `_targetHero` 已经合法时，`EnsureEncounterTargetHero(...)` 必须保留它。
   这条是本案例最重要的经验：自定义会面菜单打开、初始化、tick 和点击阶段会反复确认目标，但确认目标不等于重置目标。只要当前 `_targetHero` 仍然属于当前遭遇上下文，就不能用军团长覆盖它。

4. 目标失效时才安全回退。
   如果军团解散、成员离队、队伍失效或 `_targetHero` 不再属于当前遭遇上下文，才允许回退到遭遇队伍首领；回退仍失败时应清空过期目标，避免串到上一轮会面。

## 关键文件

- `EncounterConversationTargetResolver.cs`
- `Patch_ConversationManager_OpenMapConversation.cs`
- `Patch_ConversationManager_SetupAndStartMapConversation.cs`
- `Patch_Conversation_Start_Intercept.cs`
- `LordEncounterBehavior.cs`

重点检查：

- `EncounterConversationTargetResolver.TryResolveLordFromArgumentsThenEncounterLeader(...)`
- `LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(...)`
- `LordEncounterBehavior.IsEncounterArmyMemberTarget(...)`
- `LordEncounterBehavior.TryResolveEncounterLeaderHero(...)`
- `LordEncounterBehavior.EnsureEncounterTargetHero(...)`

## 已验证修复形态

入口 patch 使用统一 resolver：

```text
TryResolveLordFromArgumentsThenEncounterLeader(instance, args)
```

它先从原版 conversation 参数里解析玩家选中的 Hero，再把遭遇首领作为最后兜底。

目标合法性允许“当前遭遇军团里的附属队伍首领”：

```text
IsEligibleCustomLordEncounterTarget(hero, encounterParty)
-> IsEncounterArmyMemberTarget(hero, encounterParty)
```

菜单刷新阶段必须先检查已有 `_targetHero`：

```text
encounteredParty = GetCurrentEncounterPartySafe()

if (_targetHero != null
    && IsEligibleCustomLordEncounterTarget(_targetHero, encounteredParty))
{
    return _targetHero
}

hero = TryResolveEncounterLeaderHero(encounteredParty)
```

这样菜单初始化、tick、条件判断和点击阶段会保留玩家选中的军团成员；只有当前目标不再合法时，才回退到军团长。

## 回归测试清单

修这个链路后，至少测试：

- 找军团长，进入自定义会面，目标仍是军团长。
- 找军团其他成员，进入自定义会面，目标是选中的成员。
- 连续选择两个不同军团成员，不残留上一次目标。
- 选成员后退出再重新遭遇，不串到旧目标。
- 普通非军团领主会面不受影响。
- 1.3 和 1.4 双版本都能编译通过。

本案例对应修复已按上述清单完成游戏内测试，并完成：

```text
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4
```

## 常见反例

不要在 `EnsureEncounterTargetHero(...)` 里无条件执行：

```text
_targetHero = PlayerEncounter.EncounteredParty.LeaderHero
```

这会把玩家在军团成员选择界面里选中的成员覆盖成军团长。

也不要只在 `OpenMapConversation` 或 `Conversation.Start` patch 里修目标解析后就认为问题结束。自定义菜单后续生命周期同样会重新取目标，如果没有“保留合法 `_targetHero`”这道保护，bug 会在菜单初始化或点击时复现。

## 给 Codex 的调用方式

以后可以直接说：

```text
调用军团成员自定义会面目标案例，排查为什么选了军团成员但会面对象变成军团长。
```

或：

```text
参考 army member custom meeting target case，检查 LordEncounterBehavior 的 _targetHero 是否被 EnsureEncounterTargetHero 覆盖。
```

Codex 应先检查本文件，再检查 conversation 入口 patch、`EncounterConversationTargetResolver.cs` 和 `LordEncounterBehavior.cs` 的目标合法性与目标保留逻辑。
