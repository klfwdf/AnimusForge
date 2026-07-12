# Courier Letter Inventory Item Persistence Case

本文档记录 AnimusForge 信件/回信库存物品与 `GIVE` 生成物品的成功实现。以后排查“生成物品不进库存、读档消失、跨存档串物品、卖掉丢弃又回来、显示成模板物品名”等问题，先按本文检查。

## 成功目标

最终成功状态必须同时满足：

- `GIVE` 标签生成的物品能进入玩家库存。
- 信使来信能作为玩家库存物品生成。
- NPC 回信也能作为玩家库存物品生成。
- 鼠标悬停或查看物品时，能看到信件完整内容；物品名本身就是内容载体。
- 保存读档后，同一存档内物品仍存在。
- 不同战役、不同存档之间不共享生成物品或信件。
- 玩家在游戏中卖掉或丢弃后，物品能正常从库存消失，保存读档后不被后台恢复。
- 生成物品保持 Bannerlord 正常库存物品行为，不被改成不可交易、不可丢弃的特殊物品。

## 核心教训

### 不要把信件做成自定义特殊物品

失败路径是把生成物品强制改成 `Book`、替换 `ItemComponent`、设置 `NotMerchandise=true`、改 `ItemFlags`。这会导致库存 UI、分类、出售、丢弃、tooltip 和保存行为都变得不可控。

成功做法是：

- 用稳定模板物品创建 `ItemObject`。
- 复制模板物品的原生属性、分类、组件、价值、重量、是否商品等字段。
- 稳定模板必须排除食物和动物，避免物品被自动消费或进入牲畜逻辑。
- 只改 `StringId` 和显示名称。
- 信件内容放进显示名称，不替换原版 `ItemComponent`。

成功代码锚点：`RewardSystemBehavior.ApplyGeneratedRewardItemTemplateState(...)`

```csharp
ItemObject copy = new ItemObject(templateItem);
CopyGeneratedRewardItemProperty(target, copy, "ItemCategory");
CopyGeneratedRewardItemProperty(target, copy, "ItemComponent");
CopyGeneratedRewardItemProperty(target, copy, "ItemFlags");
CopyGeneratedRewardItemProperty(target, copy, "Value");
CopyGeneratedRewardItemProperty(target, copy, "Weight");
CopyGeneratedRewardItemProperty(target, copy, "IsFood");
CopyGeneratedRewardItemProperty(target, copy, "NotMerchandise");
CopyGeneratedRewardItemProperty(target, copy, "ItemType");
target.Type = templateItem.Type;

TrySetRewardItemObjectName(target, displayName);
ApplyGeneratedRewardItemRpState(target, displayName);
TryEnsureGeneratedRewardItemCategory(target, templateItem, "template_state");
```

成功代码锚点：`RewardSystemBehavior.ApplyGeneratedRewardItemRpState(...)`

```csharp
string text = NormalizeGeneratedInventoryDisplayName(displayName);
if (string.IsNullOrWhiteSpace(text))
{
    text = target.Name?.ToString() ?? target.StringId ?? "";
}
TrySetRewardItemObjectName(target, text);
```

`ApplyGeneratedRewardItemRpState(...)` 这个名字现在只是历史命名。它不能再设置 RP 组件、不能改类型、不能改是否商品。

### 食物和动物不能作为生成模板

失败路径是用奶酪、粮食、肉、鱼、酒、牲畜等作为生成物品模板。即使显示名正确，物品仍会继承食物自动消费或动物逻辑，最后可能在游戏推进时从库存里消失。

成功做法是：

- `IsStableGeneratedRewardTemplateItem(...)` 必须排除食物和动物。
- 模板打分时，食物和动物必须返回 0。
- 如果相似度匹配先选中了食物或动物，创建入口必须 fallback 到安全模板。
- 安全模板优先选择非食物 `Goods` 或 `Book`。

成功代码锚点：`RewardSystemBehavior.IsGeneratedRewardAutoConsumedTemplateItem(...)`

```csharp
return item.Type == ItemObject.ItemTypeEnum.Animal
    || item.IsFood
    || ItemCategoryIsAny(item,
        DefaultItemCategories.Meat,
        DefaultItemCategories.Fish,
        DefaultItemCategories.Grain,
        DefaultItemCategories.Beer,
        DefaultItemCategories.Wine,
        DefaultItemCategories.DateFruit);
```

### 全局 manifest 不能作为游戏数据源

失败路径是把 `GeneratedRewardItems.json` 当成运行时数据源读取，并在读档时合并进当前存档记录。这会导致 A 存档生成过的信件或物品，进入 B 存档。

成功做法是：

- `GeneratedRewardItems.json` 不再读写为游戏数据。
- 静态字典只作为当前存档运行期索引。
- 每次读档开头都清空运行期缓存。
- 只从当前存档的 `SyncData` 重建记录。

成功代码锚点：`RewardSystemBehavior.SyncGeneratedRewardItemData(...)`

```csharp
if (dataStore.IsLoading)
{
    ClearGeneratedRewardRuntimeState("sync_data_load_begin");
    _generatedRewardItemRecords.Clear();
    _generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    _generatedRewardPlayerRosterRecords.Clear();
    _generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    Logger.Log("Logic", "[RewardItemResolve] generated_save_scope_cleared reason=sync_data_load_begin");
}
```

成功代码锚点：`RewardSystemBehavior.ClearGeneratedRewardRuntimeState(...)`

```csharp
GeneratedRewardPendingItemsByObjectId.Clear();
GeneratedRewardDetachedItemsByObjectId.Clear();
GeneratedRewardDetachedItemsByStringId.Clear();
GeneratedRewardManifestByObjectId.Clear();
GeneratedRewardManifestByStringId.Clear();
GeneratedRewardManifestLoaded = true;
```

### 读档时 ref 容器必须用空字典

失败路径是把上一个存档残留的字段直接作为 `SyncData` 的 ref 容器传入。如果当前存档没有这个 key，旧字典可能残留。

成功做法是读档时传空字典，读到什么就用什么，读不到就是空。

成功代码锚点：`CourierDeliveryBehavior.SyncCourierLetterInventoryData(...)`

```csharp
Dictionary<string, string> inventoryStorage =
    dataStore.IsSaving
        ? _courierLetterInventoryStorage
        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

dataStore.SyncData(CourierLetterInventoryStorageKey, ref inventoryStorage);
```

读档开头同时清空本实例信件库存记录：

```csharp
if (dataStore.IsLoading)
{
    _courierLetterInventoryRecords.Clear();
    _courierLetterInventoryStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    _courierLetterInventoryRestoreRetryRemaining = 0;
    _nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;
    Log("courier letter inventory save scope cleared reason=sync_load_begin");
}
```

### 信件库存不能从全局生成物品清单反推

失败路径是信件系统调用 `RewardSystemBehavior.ExportGeneratedInventoryItemsForExternal(...)`，再把所有看起来像信件的生成物品导入 `_courierLetterInventoryRecords`。这会重新打开跨存档串物品。

成功做法是保留方法入口但让它成为 no-op，只记录日志。

成功代码锚点：`CourierDeliveryBehavior.DiscoverCourierLetterInventoryRecordsFromGeneratedRewardManifest(...)`

```csharp
EnsureCourierLetterInventoryData();
Log("skip courier letter inventory manifest import reason=" + (reason ?? "")
    + " tracked=" + _courierLetterInventoryRecords.Count
    + " disabled=cross_save_guard");
```

### 后台恢复不能和玩家丢弃对抗

失败路径是加载后不断重试恢复信件库存。玩家刚卖掉或丢弃，后台又根据旧记录补回来，看起来就像物品无法丢弃。

成功做法是关闭恢复重试。读档修复可以执行一次，但不允许长期重试对抗玩家库存操作。

成功代码锚点：`CourierDeliveryBehavior.ScheduleCourierLetterInventoryRestoreRetries(...)`

```csharp
_courierLetterInventoryRestoreRetryRemaining = 0;
_nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;
Log("skip courier letter inventory restore retry reason=" + (reason ?? "")
    + " requested=" + Math.Max(0, attempts)
    + " records=" + _courierLetterInventoryRecords.Count
    + " disabled=discard_guard");
```

保存时以玩家当前库存为准。如果信件已卖掉或丢弃，刷新时数量为 0，记录必须移除。

成功代码锚点：`CourierDeliveryBehavior.RefreshCourierLetterInventoryRecordsFromPlayerRoster(...)`

```csharp
int amount = CountCourierLetterRecordInRoster(roster, record, out ItemObject rosterItem);
if (amount <= 0)
{
    _courierLetterInventoryRecords.Remove(key);
    continue;
}
```

## 信件和回信入库

来信和回信共用同一个入库入口：`CourierDeliveryBehavior.AddCourierLetterToPlayerInventory(...)`。

来信：

```csharp
AddCourierLetterToPlayerInventory(session, sender, senderName, visibleLetter, isReply: false);
```

回信：

```csharp
AddCourierLetterToPlayerInventory(session, recipient, senderName, reply, isReply: true);
```

物品名称由 `BuildCourierLetterInventoryDisplayName(...)` 生成：

```csharp
return (isReply ? name + "的回信：" : "来自" + name + "的信：") + "\n" + text;
```

所以库存中看到的是完整文本承载物：

```text
来自某人的信：
正文内容
```

或：

```text
某人的回信：
正文内容
```

## 生成物品创建流程

成功链路：

1. `GenerateNamedInventoryItemToRosterForExternal(...)`
2. `TryCreateGeneratedRewardItemResolution(...)`
3. `TryGetOrCreateGeneratedRewardItem(...)`
4. `ApplyGeneratedRewardItemTemplateState(...)`
5. `AddEquipmentElementToRosterAndCountDelta(...)`
6. `RememberGeneratedRewardPlayerRosterItemIfNeeded(...)`

关键原则：

- `StringId` 使用 `af_generated_reward_...` 稳定 ID。
- `ObjectId` 用稳定生成逻辑，不复用模板物品 ID。
- `ItemObject` 复制模板状态，而不是改成特殊类型。
- 入库存后用实际 roster delta 验证，不盲信 `AddToCounts(...)`。
- 玩家库存记录只记录当前存档当前库存中的生成物品。

## 保存和读档

生成物品定义保存字段：

- `_rewardGeneratedItems_v1`

玩家库存中生成物品保存字段：

- `_rewardGeneratedPlayerRosterItems_v1`

信件库存保存字段：

- `_af_courier_letter_inventory_v1`

这些字段都必须只通过 `IDataStore.SyncData(...)` 保存到当前存档。不要用全局 JSON、日志目录文件、静态字典或模块目录文件作为游戏数据源。

## 日志检查

成功日志应能看到：

- `[RewardItemResolve] generated_save_scope_cleared reason=sync_data_load_begin`
- `[RewardItemResolve] generated_runtime_state_cleared ... global_manifest_io=disabled`
- `courier letter inventory save scope cleared reason=sync_load_begin`
- `skip courier letter inventory manifest import ... disabled=cross_save_guard`
- `skip courier letter inventory restore retry ... disabled=discard_guard`
- `courier letter inventory item added ... reply=False`
- `courier letter inventory item added ... reply=True`
- `[RewardItemResolve] generated_player_roster_capture ... records=... amount=...`

如果出现跨存档串物品，优先查：

- 是否又读取了 `GeneratedRewardItems.json`。
- 是否又把 manifest merge 进 `_generatedRewardItemRecords`。
- 信件是否又从 `ExportGeneratedInventoryItemsForExternal(...)` 导入。
- `SyncData` 读档时是否传了旧实例字典而不是空字典。

如果出现卖掉/丢弃后回弹，优先查：

- `_courierLetterInventoryRestoreRetryRemaining` 是否被重新设为大于 0。
- `ScheduleCourierLetterInventoryRestoreRetries(...)` 是否又恢复了重试。
- 保存时 `RefreshCourierLetterInventoryRecordsFromPlayerRoster(...)` 是否因为匹配失败没有删除记录。
- 是否存在新的后台 `Restore...` 调用绕过了 discard guard。

如果显示成模板名，例如奶酪，优先查：

- `TrySetRewardItemObjectName(...)` 是否成功。
- `ApplyGeneratedRewardItemRpState(...)` 是否又被改回设置组件/类型。
- tooltip 是否从当前存档记录或物品 `Name` 取文本。
- 日志中的 `generated_object_visibility ... name=... type=... component=...`。

## 禁止回归清单

以后改这块功能时，不要做这些事：

- 不要把生成信件强制设为 `ItemObject.ItemTypeEnum.Book`。
- 不要把信件内容塞进自定义 `ItemComponent` 后替换模板组件。
- 不要设置 `NotMerchandise=true` 来表达 RP 物品。
- 不要用食物或动物作为生成物品模板。
- 不要把 `GeneratedRewardItems.json` 或任何日志目录文件作为正式存档数据。
- 不要在读档时把全局 manifest 合并到当前存档记录。
- 不要通过后台重试长期恢复信件库存。
- 不要用模板名作为玩家可见名称。
- 不要把信件做成一个通用物品，然后另存内容；物品名称本身就是内容。

## 验证清单

功能完成后至少测试：

1. 通过 `GIVE` 标签生成一个不存在于原版物品表的名称，确认库存出现该名称。
2. 保存并读档，确认该生成物品仍在同一存档库存中。
3. 切换到另一个战役或另一个存档，确认不会出现上一存档的生成物品。
4. 收到 NPC 主动来信，确认库存出现 `来自某人的信：...`。
5. 收到 NPC 回信，确认库存出现 `某人的回信：...`。
6. 鼠标悬停或查看物品，确认能看到完整信件内容。
7. 卖掉或丢弃信件/生成物品，确认库存中消失。
8. 卖掉或丢弃后保存并读档，确认不会恢复。
9. 查看 `mod_logic`，确认没有跨存档 manifest 导入和恢复重试。
10. 分别构建 `BannerlordApi=1.3` 和 `BannerlordApi=1.4`。

## 双版本注意

本案例只使用共享 C# 逻辑、`IDataStore.SyncData(...)`、`ItemRoster`、`ItemObject`、`MBObjectManager` 等现有兼容路径。不要为了这块逻辑新增版本分支，除非未来 Bannerlord API 签名确实变化。

验证命令：

```bat
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4
```
