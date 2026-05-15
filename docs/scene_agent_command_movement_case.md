# Scene Agent Command Movement Case

本文档记录“场景内 NPC 命令移动机制”的项目案例。这里的“场景内”特指村庄、城市、城堡等 mission 场景中的 NPC 行为，不是大地图移动，也不包括原版会面/meeting 专用链路。

## 核心原则

场景内 NPC 命令移动必须以 `Agent` 为目标节点，不要以坐标点作为主目标节点。

原因很直接：在村庄、城市、城堡这类复杂 scene 中，直接把坐标当目标节点时，NPC 往往根本不会实际移动。表面上移动命令已经触发，但 Agent 没有可靠的场景目标对象可追踪，结果就是站在原地，后续也无法稳定判断“是否到达”“该由谁继续会话”“谁要返回原岗位”。

正确做法是围绕 `Agent` / `AgentIndex` / `LocationCharacter` 建模，把坐标只当作临时路径点、站位或距离判断，不当作机制身份。如果确实需要“走到某个坐标”的效果，应创建一个隐藏/隐形的代理 `Agent` 放在目标位置，让 NPC 以这个代理 `Agent` 为目标节点；也就是说，用隐形 Agent 承载坐标，而不是让移动机制直接追裸坐标。

## 名词解释

- `Agent`：当前 mission 场景里真实存在、能被 AI 控制的实体。玩家、NPC、士兵、临时代理都可以是 Agent。场景内移动命令应尽量追踪 Agent。
- `AgentIndex`：当前 mission 中某个 Agent 的运行时编号。它不是永久 ID，只在当前场景/当前 mission 内有效。用它做状态机 key，可以在后续 tick、TTS 结束、抵达、取消、返回时找回同一个场景实体。
- `LocationCharacter`：城镇/村庄/城堡 location 系统里的“这个地点中的角色槽位/角色记录”。它比 Agent 更偏向场景刷人和地点安排，可以用来重新解析当前场景里的 Agent，或处理跨 location 的传唤、门口代理和返回岗位。

简单理解：`LocationCharacter` 更像“这个场景地点里应该存在的某个人”，`Agent` 是“他当前在场景里生成出来的实体”，`AgentIndex` 是“这个实体当前这局 mission 的编号”。

## 参考话题

`RuleBehaviorPrompts.json` 中的：

- Id：`scene_mechanism_actions`
- TopicLabel：`Lead the way, summon, and follow`

对应标签：

- `[ACTION:SCENE_FOLLOW_PLAYER]`
- `[ACTION:SCENE_STOP_FOLLOW]`
- `[END]`
- `[ACTION:SCENE_SUMMON:人物名称列表]`
- `[ACTION:SCENE_GUIDE:人物名称]`

## 关键文件

- `AnimusForge/ModuleData/RuleBehaviorPrompts.json`
- `ShoutBehavior.cs`
- `AIConfigHandler.cs`

重点看 `ShoutBehavior.cs` 中这些区域：

- `BuildSceneGuidePromptTargets(...)`
- `BuildRuntimeSceneMechanismPostprocessRulesForScene(...)`
- `TryTriggerSceneSummonAction(...)`
- `TryTriggerSceneGuideAction(...)`
- `StartSceneSummonBatchAction(...)`
- `StartSceneGuideAction(...)`
- `ResolveAgentForLocationCharacter(...)`
- `TryGetSceneSummonConversationSessionForAgentIndex(...)`
- `CancelSceneGuideActionForAgent(...)`
- `CancelSceneSummonActionForAgent(...)`

## 适用边界

适用：

- 城市街道
- 城堡场景
- 村庄场景
- 酒馆、领主大厅门口等场景内引导
- 场景内喊话链路里的带路、传唤、跟随、停止跟随

不适用：

- 大地图移动
- 大地图队伍 AI
- 原版 meeting 会面专用逻辑
- 纯坐标寻路功能

如果任务说“场景里让 NPC 带路/传唤/跟随”，默认走本案例。如果任务说“大地图上让部队移动到某地”，不要套本案例。

## 设计要求

1. 目标身份用 Agent。
   每个动作都应能落到当前场景里的 `Agent` 或可解析到 `Agent` 的 `LocationCharacter`。记录时优先保留 `AgentIndex`。

2. 提示词目标用清单。
   主链路和后处理只能允许 LLM 从【带路与传唤NPC清单】中选目标，不允许编造地点或人物。

3. 后处理标签仍只输出人物/清单名。
   标签里不要输出坐标。`[ACTION:SCENE_GUIDE:人物名称]` 应由 C# 解析到清单目标，再解析到 `Agent`、`LocationCharacter` 或门口代理。

4. 坐标只能辅助。
   可以用坐标做：
   - 距离判断
   - 站位
   - 门口等待点
   - stuck 兜底
   - 返回原位

   不可以用坐标替代目标身份。

   如果需求本质上是“移动到某个固定点”，也要把这个固定点包装成隐藏/隐形 `Agent` 或门口 proxy agent，再让行动 NPC 面向这个 Agent 执行移动。坐标是 proxy 的出生/摆放依据，不是命令目标。

5. 状态机绑定 AgentIndex。
   guide、summon、follow、return、arrival speech、cancel 都要以 `AgentIndex` 为 key。不要用坐标作为 session key。

6. 跨场景要用代理和 LocationCharacter。
   目标在酒馆、领主大厅或其他 location 时，优先维护 `LocationCharacter` 路径和门口 proxy agent，不要让 LLM 或机制直接写一个目标坐标。

7. 会话恢复也按 Agent。
   传唤后的会面、结束会面 `[END]`、返回岗位，都要能通过 `TryGetSceneSummonConversationSessionForAgentIndex(...)` 找回当前会话对象。

8. 场景出口、门、Passage 不要直接当最终目标节点。
   门和场景出口更像交互/切换/通道对象，不是稳定的 NPC 目标实体。直接把门当目标，可能出现 NPC 站住不动、卡在交互点、触发错误的场景切换意图、抵达判断不稳定，或后续没有可恢复的会话对象。

   如果目标是“带玩家到门口”或“去某个出口附近”，做法应是：
   - 找到门/Passage 附近一个可用等待点。
   - 在等待点创建隐藏/隐形 proxy Agent，或复用已有门口 proxy agent。
   - 让行动 NPC 以 proxy Agent 为目标节点移动。
   - 抵达、台词、返回和清理都按行动 NPC 的 `AgentIndex` 与 proxy Agent 状态机处理。

   门/Passage 可以用来计算 proxy 的位置和跨 location 路径，但不要直接作为 movement target 的身份。

## 新增场景命令清单

新增或修改场景内 NPC 命令移动时，至少检查：

- 是否属于场景内 mission 行为，而不是大地图行为。
- 是否排除了 meeting 专用链路。
- 话题是否使用 `scene_mechanism_actions` 或同类 scene group。
- 主链路是否注入【场景行为规则】和【带路与传唤NPC清单】。
- 后处理是否只在 `sceneMechanismRuleInjected` 时注入对应 `PostprocessRules`。
- 标签格式是否被 `SceneSummonActionTagRegex`、`SceneGuideActionTagRegex`、`SceneFollowStartTagRegex`、`SceneFollowStopTagRegex` 或新增正则识别。
- 触发函数是否拿到当前说话 NPC 的 `Agent` 和 `AgentIndex`。
- 目标是否从清单解析到 `Agent` / `LocationCharacter`，而不是解析成裸坐标。
- 如果目标是门、出口或 Passage，是否创建了门口 proxy Agent，而不是直接把门/坐标当目标。
- 取消、返回、抵达、TTS 后续动作是否仍能按 `AgentIndex` 找回任务。
- 日志是否包含 agent、target agent、location character、阶段名，而不是只有坐标。

## 反例

不要这样设计：

```text
LLM 输出 [ACTION:SCENE_MOVE_TO:123.4,56.7,8.9]
C# 直接让当前 NPC 移动到这个坐标。
```

这在当前场景机制里会表现为移动命令看似触发，但 NPC 实际不移动；同时机制也会失去目标身份，后续无法可靠判断“他是在带玩家找谁”“传唤的是谁”“该由谁说抵达台词”“谁要返回原岗位”。

应当这样设计：

```text
LLM 输出 [ACTION:SCENE_GUIDE:铁匠]
C# 从【带路与传唤NPC清单】解析“铁匠” -> LocationCharacter/Agent -> 当前场景目标或门口代理 -> 执行带路状态机。
```

如果目标只是一个固定位置，应当这样设计：

```text
C# 在目标坐标创建隐藏/隐形 proxy Agent -> 当前 NPC 以 proxy Agent 为目标节点移动 -> 到达后按 proxy Agent / 发起 NPC 的 AgentIndex 继续状态机 -> 清理 proxy Agent。
```

如果目标是门口或场景出口，应当这样设计：

```text
C# 从门/Passage 找到附近等待点 -> 创建隐藏/隐形门口 proxy Agent -> 当前 NPC 以 proxy Agent 为目标节点移动 -> 到达后播放/触发门口逻辑 -> 按 AgentIndex 返回或清理 proxy。
```

## 给 Codex 的调用方式

以后可以直接说：

```text
按场景 Agent 命令移动案例，做一个场景内 NPC 带路/传唤/跟随机制。
```

或：

```text
参考 Lead the way, summon, and follow，排查为什么场景内 NPC 移动命令触发了但没有实际移动。
```

Codex 应先检查本文件，再检查 `RuleBehaviorPrompts.json` 的 `scene_mechanism_actions` 和 `ShoutBehavior.cs` 的 Agent/LocationCharacter 状态机。
