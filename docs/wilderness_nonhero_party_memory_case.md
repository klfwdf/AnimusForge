# 大地图非 Hero 部队记忆案例

本文档记录 AnimusForge 已验证有效的“大地图非 Hero 部队记忆”实现。这里的非 Hero 部队指野外大地图上的劫匪、土匪、商队等 `MobileParty`，不是 Hero NPC，也不是城镇、酒馆、领主大厅、街道中的普通场景非 Hero NPC。

## 目标

大地图非 Hero 部队记忆必须同时满足三件事：

1. 每个存档独立。
   同一个战役存档的数据只存在于该存档的 Campaign save data 中。不同战役、不同存档不能共享记忆。

2. 每支部队独立。
   同名、同兵种、同阵营的非 Hero 部队也不能共享记忆。例如两支都叫“劫匪”的大地图部队必须有不同记忆。

3. 保存读档后不丢。
   保存、退出、读档后，NPC 主链路能继续读到记忆，右上角对话历史也能显示同一份持久历史。

## 核心结论

不要用显示名称、兵种名、`CharacterObject.StringId` 或 `AgentIndex` 作为持久记忆主键。

正确主键是：

```text
af_nonhero:<base>|party:<party_key>
```

其中 `party_key` 以 `MobileParty.StringId` 为主：

```text
party_string_id:<MobileParty.StringId>
```

实际例子：

```text
af_nonhero:troop:looter:kingdom:looters|party:party_string_id:looters_1161
```

这个 ID 是整套非 Hero 记忆系统的唯一身份。写入、读档恢复、prompt 注入、右上角历史、清理、迁移都必须围绕同一个 ID。

## 适用边界

适用：

- 大地图野外非 Hero 部队。
- 原生大地图 meeting / encounter 对话里的非 Hero 部队。
- 场景喊话中能解析到大地图野外非 Hero `MobileParty` 的目标。
- 商队、劫匪、土匪、海寇、森林强盗等同类移动部队。

不适用：

- Hero NPC。Hero 仍使用 `Hero.StringId` 作为记忆 ID。
- 城镇、酒馆、领主大厅、街道中的普通非 Hero NPC。这类 NPC 不应默认进入长期记忆，除非以后另有明确设计。
- 仅在当前 mission 中临时生成且没有对应大地图 `MobileParty` 的普通 Agent。

## 身份解析

核心入口在 `ShoutBehavior.cs`：

- `TryResolveWildernessNonHeroMemory(...)`
- `BuildWildernessNonHeroPartyMemoryKey(...)`
- `BuildWildernessNonHeroCharacterMemoryKey(...)`
- `BuildNativeConversationHistoryKey(...)`

解析流程：

1. 排除 Hero。
   如果 `targetHero != null`、`targetCharacter.HeroObject != null` 或 `npc.IsHero == true`，不走非 Hero 记忆。

2. 确认野外非 Hero 范围。
   `IsWildernessNonHeroMemoryScope(agentIndex)` 必须成立。非野外范围不进入这套长期记忆。

3. 解析当前 `MobileParty`。
   通过 `TryResolveWildernessNonHeroMobileParty(agentIndex)` 找到当前大地图部队。

4. 构造 base key。
   优先使用 `NpcDataPacket.UnnamedKey`。如果没有，则用 `CharacterObject` 和 `MobileParty` 构造：

   ```text
   troop:<CharacterObject.StringId>
   troop:<CharacterObject.StringId>:kingdom:<MapFaction.StringId>
   troop:<CharacterObject.StringId>:kingdom:<MapFaction.StringId>:lord:<LeaderHero.StringId>
   ```

   `kingdom` 优先取 `party.MapFaction.StringId`，没有时才退到 `character.Culture.StringId`。

5. 构造 party key。
   优先级如下：

   ```text
   party_string_id:<MobileParty.StringId>
   party_guid:<AnimusForge fallback guid>
   party_index:<PartyBase.Index>
   ```

   `party_string_id` 是主路径。它随 Bannerlord 存档保存，能在保存读档后保持同一支 `MobileParty` 的身份。

   `party_guid` 只作为没有 `StringId` 的异常部队兜底，保存在 `_af_wildernessNonHeroPartyMemoryIds_v1`。

   `party_index` 只是极端 fallback，不能作为读档后的持久主键，因为读档后 index 不保证能找回同一份记忆。

6. 生成最终 ID。

   ```text
   MyBehavior.BuildNonHeroMemoryIdForExternal(baseKey + "|party:" + partyKey)
   ```

   `MyBehavior` 会统一加 `af_nonhero:` 前缀，并通过 `NormalizeMemoryHeroId(...)` 归一化。

## 严禁使用的主键

这些 key 都会重新制造共享记忆：

```text
劫匪
商队
looter
troop:looter
native_nonhero:name:劫匪
native_nonhero:character:looter
```

原因：

- 显示名称会重复。
- 兵种会重复。
- 同一阵营的同类部队会重复。
- `AgentIndex` 只在当前 mission 有意义，读档后不可作为持久身份。
- `PartyBase.Index` 只能做兼容或清理当前会话旧记录，不能做主键。

## 写入链路

非 Hero 写入由 `ShoutBehavior.AppendWildernessNonHeroMemory(...)` 发起。

流程：

```text
AppendWildernessNonHeroMemory
-> TryResolveWildernessNonHeroMemory
-> MyBehavior.AppendExternalNonHeroDialogueHistory
   或 MyBehavior.AppendExternalNonHeroSceneDialogueHistory
-> AppendDialogueHistoryById
```

`AppendDialogueHistoryById(...)` 同时写两类数据：

1. 最近对话历史 `_dialogueHistory`
   用 `DialogueDay` 保存，每天一组 `Lines`，最多保留最近 260 行。

2. 每日未压缩记忆草稿 `_dailyMemoryDrafts`
   用 `DailyMemoryDraft` 和 `DailyMemoryLine` 保存，用于后续压缩、总览和未压缩记忆注入。

每日草稿行包含这些关键字段：

```text
GameDayIndex
GameDate
GameHour
Scene
Speaker
Text
SceneSessionId
DialogueSessionId
MemorySessionKey
IsAfef
IsLlmDialogue
```

玩家普通发言写为 LLM 对话行。NPC 回复写为 `npcName: <reply>`。系统事实必须使用 AFEF 前缀：

```text
[AFEF玩家行为补充] ...
[AFEF NPC行为补充] ...
```

普通玩家口头陈述不是事实。只有 AFEF 行代表已经发生的游戏事实。

## 保存读档

保存和读取都在 `MyBehavior.SyncData(...)` 中执行。

非 Hero 相关数据不是全局文件，不是跨存档缓存，而是随 Campaign save data 保存。每个存档都有自己的数据。

保存时会写入：

```text
_af_wildernessNonHeroPartyMemoryIds_v1
_dialogueHistory_v2
_af_dailyMemoryDrafts_v1
_af_compressedMemoryBlocks_v1
_af_memorySummaryQueue_v1
_af_memoryOverviewStates_v1
_af_memoryOverviewQueue_v1
_af_npcMajorActionSummaries_v1
_af_npcMajorActionSummaryQueue_v1
_npcMajorActions_v1
_npcRecentActions_v1
```

其中：

- `_dialogueHistory_v2` 保存最近对话历史。
- `_af_dailyMemoryDrafts_v1` 保存当天未压缩记忆草稿。
- `_af_compressedMemoryBlocks_v1` 保存日结后的压缩记忆块。
- `_af_memoryOverviewStates_v1` 保存记忆总览。
- `_af_wildernessNonHeroPartyMemoryIds_v1` 只保存 fallback GUID 映射，不是主路径。

读档时会先 `ResetRuntimeForLoadedSave("sync_load")`，然后重新恢复上述结构。关键日志应能看到：

```text
[NonHeroMemoryTrace] stage=sync_load_dialogue_restored owners=... lines=...
[NonHeroMemoryTrace] stage=sync_load_daily_restored owners=... lines=...
```

如果这两条有行数，说明保存读档没有丢。后续如果 NPC 表现为“不记得”，要继续查 prompt 注入或 UI 展示，而不是先假定存档丢失。

## 主链路读取

非 Hero 主链路读取分两类。

### 压缩/总览上下文

`ShoutBehavior.BuildWildernessNonHeroHistoryContextForPrompt(...)` 调用：

```text
MyBehavior.BuildNonHeroHistoryContextForExternal(...)
```

最后进入 `BuildHistoryContextById(...)`，按同一个 `af_nonhero` ID 读取：

- 最近历史窗口
- 每日草稿
- 压缩记忆块
- 记忆总览
- 召回上下文

### 未压缩长期记忆注入

读档后，原生自由对话的短期 session 历史会清空，所以必须把同一个 `af_nonhero` 持久记忆注入 prompt。

关键修复在 `ShoutBehavior.BuildUncompressedMemoryRoleMessagesForPrompt(...)`。

旧问题是：原生非 Hero 路径里经常出现：

```text
Hero == null
agentIndex == -1
```

旧函数只拿 hero 和 agentIndex，拿不到 `CharacterObject` / `NpcDataPacket`，所以无法重建 `af_nonhero` ID。日志表现为：

```text
stage=uncompressed_inject agent=-1 memoryId= reason=resolve_failed messages=0
```

修复后的函数必须传入：

```text
Hero hero
CharacterObject targetCharacter
NpcDataPacket npc
int targetAgentIndex
```

非 Hero 时用这些对象重新调用 `TryResolveWildernessNonHeroMemory(...)`，再调用：

```text
MyBehavior.BuildNonHeroUncompressedMemoryRoleMessagesForExternal(memoryId, memoryName, targetAgentIndex, false)
```

成功日志应类似：

```text
stage=uncompressed_inject agent=-1 memoryId=af_nonhero:... messages=...
```

`messages` 应大于 0，除非这支部队确实没有当天草稿或未压缩历史。

## 原生自由对话 session 历史

`ShoutBehavior` 里还有 `_nativeConversationSessionHistory`。它只代表当前运行时 session，不是持久记忆。

正确规则：

- 当前未读档的连续对话可以使用 session 历史。
- 保存读档后 session 必然清空。
- session 历史不能作为读档后的唯一历史来源。
- 对于野外非 Hero，`BuildNativeConversationHistoryKey(...)` 能解析到 `af_nonhero` 时必须返回 `af_nonhero` ID，不能退回 `native_nonhero:name` 或 `native_nonhero:character`。
- `native_nonhero:*` 只能作为非野外、非持久、临时 NPC 的 fallback。

## 右上角历史 UI

自由对话右上角历史弹窗在 `AnimusForgeConversationHistoryLogPopup.cs`。

读档后右上角曾经为空，是因为 UI 只 fallback 到 `_nativeConversationSessionHistory`。读档后 session 清空，所以面板为空，而不是记忆真的丢失。

正确流程：

```text
ShowForNativeConversation
-> ShoutBehavior.TryGetNativeConversationPersistentHistoryTargetForExternal(...)
-> Hero: MyBehavior.GetDialogueHistoryEntriesForExternal(hero, 260)
-> 非 Hero: MyBehavior.GetDialogueHistoryEntriesByIdForExternal(memoryId, 260)
-> 仍为空时才 fallback ShoutBehavior.GetNativeConversationSessionHistoryEntriesForExternal(260)
```

非 Hero 成功时日志应看到：

```text
NativeConversationHistory persistent_target kind=nonhero memoryId=af_nonhero:...
NativeConversationHistory open source=persistent_nonhero memoryId=af_nonhero:... entries=...
```

这条路径只改变 UI 数据源，不改变 LLM 主链路语义。

## 迁移规则

迁移只允许把同一支部队的旧 party-scoped key 合并到 canonical key。

允许迁移：

```text
af_nonhero:...|party:<StringId>
-> af_nonhero:...|party:party_string_id:<StringId>

af_nonhero:...|party:party_guid:<guid>
-> af_nonhero:...|party:party_string_id:<StringId>
```

实现入口：

```text
MyBehavior.MigrateNonHeroPartyScopedMemoryForExternal(...)
MyBehavior.MigrateNonHeroPartyScopedMemory(...)
MergeMemoryEntityDataById(...)
```

`MergeMemoryEntityDataById(...)` 会合并并重定向：

- `_dialogueHistory`
- `_dailyMemoryDrafts`
- `_compressedMemoryBlocks`
- `_memorySummaryQueue`
- `_pendingWeeklyMemoryMaterialTriggers`
- `_memoryOverviewStates`
- `_memoryOverviewQueue`
- `_npcMajorActionSummaries`
- `_npcMajorActionSummaryQueue`
- `_npcMajorActions`
- `_npcRecentActions`
- `_dirtyMemoryOverviewIds`
- `_pendingMemoryOverviewCandidateScanIds`

然后删除旧 source ID。

严禁迁移：

```text
troop:looter
name:劫匪
native_nonhero:name:劫匪
只含兵种或显示名的共享 key
```

`MigrateNonHeroPartyIndexMemory(...)` 当前故意是 no-op。`party_index` 不应合并到 canonical key，否则可能把不同部队合并。

## 部队消灭后的清理

非 Hero 大地图部队经常被消灭，所以必须清理对应记忆。

入口：

```text
OnMobilePartyDestroyedForNonHeroMemoryCleanup(...)
OnPartyRemovedForNonHeroMemoryCleanup(...)
RemoveWildernessNonHeroPartyMemory(...)
CleanupNonHeroMemoryForRemovedParty(...)
```

清理只按这支部队的精确 party needle：

```text
|party:party_string_id:<MobileParty.StringId>
|party:<MobileParty.StringId>              # 兼容旧未标注 StringId
|party:party_guid:<fallback guid>
|party:party_index:<PartyBase.Index>       # 只清旧 fallback
```

严禁按名字、兵种、阵营模糊删除。否则一支“劫匪”被消灭，会误删另一支同名“劫匪”的记忆。

`RemoveMemoryEntityDataById(...)` 会删除该 ID 的全部记忆实体：

- 对话历史及存储
- 每日草稿及存储
- 压缩块及存储
- 记忆摘要队列
- 每周素材触发器
- 记忆总览及队列
- NPC 重大行动摘要及队列
- NPC 重大行动
- NPC 最近行动
- 脏扫描 ID 和候选扫描队列

成功日志：

```text
[NonHeroMemoryTrace] stage=cleanup_removed_party reason=... party=... removed=...
```

## 日志规范

所有非 Hero 记忆排查日志使用：

```text
Logger.Log("Logic", "[NonHeroMemoryTrace] ...")
```

关键阶段：

```text
party_key
resolve
migrate_party_scoped
append_request
daily_append
load_dialogue_hit
load_dialogue_miss
save_dialogue_in_memory
append_commit
history_context_request
history_context_done
uncompressed_build_done
uncompressed_inject
entries_read
sync_save_begin
sync_save_dialogue_storage
sync_save_daily_storage
sync_load_party_map
sync_load_dialogue_restored
sync_load_daily_restored
cleanup_removed_party
```

右上角 UI 使用：

```text
Logger.Log("NativeConversationHistory", ...)
```

关键阶段：

```text
persistent_target kind=nonhero
open source=persistent_nonhero
open source=session
```

## 已验证过的故障模式

### 同名队伍共享记忆

原因通常是用了显示名称、兵种、`UnnamedKey` 或 `native_nonhero:name` 作为持久 key。

修复方式：

```text
必须在 base key 后追加 |party:party_string_id:<MobileParty.StringId>
```

### 保存读档后 NPC 不记得

日志曾证明保存读档数据已经恢复：

```text
sync_save_begin ... dialogueLines=... dailyDraftLines=...
sync_save_dialogue_storage owners=...
sync_save_daily_storage owners=...
sync_load_dialogue_restored owners=... lines=...
sync_load_daily_restored owners=... lines=...
```

真正问题是读档后的 prompt 注入失败：

```text
stage=uncompressed_inject agent=-1 memoryId= reason=resolve_failed messages=0
```

修复方式是让未压缩记忆注入函数接收 `targetCharacter` 和 `NpcDataPacket`，不要只依赖 hero/agent。

### 读档后右上角没有当天记忆

原因是 UI 只读 session 历史。读档后 session 历史不存在。

修复方式是 UI 先解析持久 `af_nonhero` ID，再读：

```text
MyBehavior.GetDialogueHistoryEntriesByIdForExternal(memoryId, 260)
```

## 回归测试清单

1. 同名不共享。
   找两支同名劫匪或商队，分别对话。日志里的 `party_string_id` 必须不同，`memoryId` 必须不同。

2. 保存读档不丢。
   对某支非 Hero 部队发问，等 `append_commit` 和 `daily_append` 出现后保存。读档后应看到 `sync_load_dialogue_restored` 和 `sync_load_daily_restored` 有行数。

3. 读档后 prompt 能注入。
   读档后再次向同一支部队发问，应看到：

   ```text
   stage=uncompressed_inject ... memoryId=af_nonhero:... messages>0
   ```

4. 读档后右上角能显示。
   点自由对话右上角历史，应看到：

   ```text
   open source=persistent_nonhero ... entries>0
   ```

5. 消灭后清理。
   消灭一支已记录记忆的非 Hero 部队，应看到 `cleanup_removed_party`。另一支同名部队的 `memoryId` 不应被删除。

6. 跨存档隔离。
   同名或同类部队在另一个战役存档中不应读到当前存档的记忆，因为所有数据来自该存档自己的 `SyncData`。

## 修改禁区

以后改这套逻辑时不要做以下改动：

- 不要把非 Hero 持久 key 改回显示名称、兵种或 `native_nonhero:name`。
- 不要把 `party_index` 当成读档后的持久主键。
- 不要让右上角 UI 只读 `_nativeConversationSessionHistory`。
- 不要在读档后只靠 `Hero` 或 `AgentIndex` 注入非 Hero 未压缩记忆。
- 不要按名字清理被消灭部队的记忆。
- 不要把 troop-only、name-only 旧共享 key 自动合并进 canonical key。
- 不要把城镇、酒馆、领主大厅普通非 Hero NPC 默认接入这套长期记忆。

## 关键文件

- `ShoutBehavior.cs`
  - `TryResolveWildernessNonHeroMemory(...)`
  - `BuildWildernessNonHeroPartyMemoryKey(...)`
  - `BuildNativeConversationHistoryKey(...)`
  - `BuildUncompressedMemoryRoleMessagesForPrompt(...)`
  - `BuildWildernessNonHeroHistoryContextForPrompt(...)`
  - `AppendWildernessNonHeroMemory(...)`
  - `TryGetNativeConversationPersistentHistoryTargetForExternal(...)`

- `MyBehavior.cs`
  - `BuildNonHeroMemoryIdForExternal(...)`
  - `AppendExternalNonHeroDialogueHistory(...)`
  - `AppendExternalNonHeroSceneDialogueHistory(...)`
  - `AppendDialogueHistoryById(...)`
  - `BuildNonHeroHistoryContextForExternal(...)`
  - `BuildNonHeroUncompressedMemoryRoleMessagesForExternal(...)`
  - `GetDialogueHistoryEntriesByIdForExternal(...)`
  - `MigrateNonHeroPartyScopedMemoryForExternal(...)`
  - `CleanupNonHeroMemoryForRemovedParty(...)`
  - `RemoveMemoryEntityDataById(...)`
  - `SyncData(...)`

- `AnimusForgeConversationHistoryLogPopup.cs`
  - `ShowForNativeConversation(...)`

## 给 Codex 的调用方式

以后可以直接说：

```text
按大地图非 Hero 部队记忆案例检查。
```

或：

```text
参考大地图非 Hero 队伍记忆案例，排查同名劫匪共享记忆、保存读档后记忆丢失、右上角历史为空的问题。
```

Codex 应先检查本文件，再检查 `ShoutBehavior.cs`、`MyBehavior.cs` 和 `AnimusForgeConversationHistoryLogPopup.cs` 中列出的关键入口。
