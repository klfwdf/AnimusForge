# Save String Overflow Bad Save Case

本文档记录 2026-06-19 的坏档事件。以后遇到 `LoadContext::Load Strings` 阶段的 `System.OverflowException`，优先按本案例检查单条保存字符串是否超过 TaleWorlds SaveSystem 的 signed short 长度上限。

## 现象

典型日志如下：

```text
LoadContext::Load Strings block is started.
ArchiveDeserializer.LoadFrom failed. BinaryLength=48442947, Exception=System.OverflowException: 数组维度超过了支持的范围。
LoadContext::Load Strings completed
LoadContext.Load returned false.
Error: Could not load the game!
```

玩家最初反馈“存档超过 95MB 就会坏档”，但后续样本证明这不是总文件大小阈值问题。一个压缩后约 18MB 的存档也会在 `Load Strings` 阶段失败。

## 根因

原版 1.4.5 SaveSystem 的字符串表写入路径：

- `原版游戏本体代码1.4.5/TaleWorlds.SaveSystem/TaleWorlds/SaveSystem/Save/SaveContext.cs`
- `SaveStringTo(BinaryWriter stringWriter, int id, string text)`

关键逻辑：

```csharp
int stringSizeInBytes = GetStringSizeInBytes(text);
stringWriter.WriteShort((short)stringSizeInBytes);
stringWriter.WriteString(text);
```

其中 `GetStringSizeInBytes(text)` 是 `4 + Encoding.UTF8.GetByteCount(text)`。也就是说，每个字符串条目的长度会被强制转成 signed `short` 写入。单条字符串超过约 `32763` 字节后，长度会溢出为负数。

原版读取路径：

- `原版游戏本体代码1.4.5/TaleWorlds.SaveSystem/TaleWorlds/SaveSystem/ArchiveDeserializer.cs`
- `ArchiveDeserializer.LoadFrom(byte[] binaryArchive)`

关键逻辑：

```csharp
short length = binaryReader.ReadShort();
byte[] data = binaryReader.ReadBytes(length);
```

当 `length` 是负数时，`ReadBytes(length)` 内部创建负长度数组，抛出：

```text
System.OverflowException: 数组维度超过了支持的范围。
```

这个失败发生在 `LoadContext::Load Strings`，早于模组 `IDataStore.SyncData`。因此已经写坏的存档不能靠更新 DLL 在游戏内自动修复，只能用最后一个可读备份重新保存，或做离线文件修复。

## 已确认样本

### QQQ.sav

路径：

```text
F:\Mount and Blade II Bannerlord\Game Saves\QQQ.sav
```

分析结果：

```text
Compressed file bytes: 18008559
Decompressed GameData bytes: 106079498
Strings block bytes: 48442947
String entries: 330348
Bad StringId: 314786
Bad string bytes: 35848
Written short length: -29684
Preview: {"WorldNotoriety":27.066666666666666,"CultureNotoriety":...
```

修复副本：

```text
F:\Mount and Blade II Bannerlord\Game Saves\QQQ_repaired.sav
```

处理方式：将该条 `PlayerNotoriety` JSON 替换为 `{}`。代价是玩家履历/名声履历数据重置，其它存档数据保留。

校验结果：

```text
NegativeLengthCount: 0
LengthMismatchCount: 0
ParsedPosition == StringsLen
MaxStringBytes: 890
```

### 1121.sav

路径：

```text
F:\Mount and Blade II Bannerlord\Game Saves\1121.sav
```

分析结果：

```text
Compressed file bytes: 19202312
Decompressed GameData bytes: 111948177
Strings block bytes: 53203989
String entries: 362391
Bad StringId: 362335
Bad string bytes: 39638
Written short length: -25894
Preview: {"WorldNotoriety":28.099999999999998,"CultureNotoriety":...
```

修复副本：

```text
F:\Mount and Blade II Bannerlord\Game Saves\1121_repaired2.sav
```

处理方式：将该条 `PlayerNotoriety` JSON 替换为 `{}`。第一次生成的 `1121_repaired.sav` 因 PowerShell 写入 byte 数组时方法绑定错位而校验失败，已删除。有效副本是 `1121_repaired2.sav`。

校验结果：

```text
NegativeLengthCount: 0
LengthMismatchCount: 0
ParsedPosition == StringsLen
MaxStringBytes: 890
```

## 为什么不是 95MB 阈值

坏档触发点不是 `.sav` 文件总大小，也不是解压后的 `GameData` 总大小，而是字符串表里某一条字符串的独立长度。

本次两个样本都只有一个坏字符串项，且内容都以 `{"WorldNotoriety":...` 开头。说明直接根因是 `_af_player_notoriety_state_v1` 对应的 `PlayerNotoriety` JSON 作为单条字符串写入时超过了原版 signed short 上限。

总存档越大，通常意味着字符串表越大，越容易出现超长 JSON 或超长字典值；但真正会让 `Load Strings` 崩溃的是单条字符串超过约 32KB。

## 预防性代码修复

本次代码侧修复原则：

- 任何可能超过 32KB 的 JSON 不允许直接 `dataStore.SyncData(key, ref json)`。
- 使用 `CampaignSaveChunkHelper.SaveChunkedString` 和 `LoadChunkedString` 保存大 JSON。
- 使用 `CampaignSaveChunkHelper.FlattenStringDictionary` 保存可能包含长 value 的 `Dictionary<string, string>`。
- chunk 大小保持在 signed short 上限以下，本次统一使用 `12000` UTF-8 bytes。
- 保留旧小字符串兼容读取，尽量让旧档能升级到新格式。

关键改动点：

- `CampaignSaveChunkHelper.cs`
  - `StorageChunkMaxBytes = 12000`
  - 新增 `LogRawJsonSaveStats`
  - 新增带 `saveKey` 和 `loggerTag` 的 `FlattenStringDictionary` 统计入口
  - `SaveChunkedString` 写入时输出 `SaveSize` 诊断日志
- `PlayerNotorietyBehavior.cs`
  - `_af_player_notoriety_state_v1` 从直接 JSON SyncData 改为 chunked string
  - 保存时输出 `recentActions`、`majorMaterials`、`pendingMaterials`、`summaryBytes`、`maxMaterialBytes`
- `ProactiveNpcRequestBehavior.cs`
  - 主动请求状态 JSON 改为 chunked string
- `MyBehavior.cs`
  - 对话历史、记忆块、NPC 行动、人格、事件素材、王国稳定度等 string dictionary 保存点接入 `SaveSize` 统计
- `KnowledgeLibraryBehavior.cs`
  - 本地 chunk 大小从 `240` 调整到 `12000`

本次双版本验证：

```text
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3
0 warnings, 0 errors

dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4
0 warnings, 0 errors
```

## 离线修复流程

离线修复只用于已经卡在 `Load Strings` 的坏档。游戏内代码无法接管这种坏档。

基本流程：

1. 读取 `.sav` 前 4 字节，得到 metadata JSON 长度。
2. 保留 metadata 原始字节。
3. 从 metadata 之后的位置用 Deflate 解压 `GameData`。
4. 按原版 1.4.5 `GameData.Read` 顺序读取：
   - Header
   - ObjectData
   - ContainerData
   - Strings
5. 解析 Strings archive：
   - folder count
   - folders
   - entry count
   - entries
6. 对每个字符串 entry 检查：
   - declared short length 是否为负数
   - declared length 是否等于 `4 + utf8ByteLength`
   - utf8ByteLength 是否超过 `32763`
7. 对坏 entry 进行保守替换：
   - JSON 对象替换为 `{}`
   - JSON 数组替换为 `[]`
   - 普通字符串替换为空串
8. 重新写回 Strings archive。
9. 按原版 `GameData.Write` 顺序重新写回：
   - Header
   - ObjectData
   - ContainerData
   - Strings
10. 重新 Deflate 压缩，并把 metadata 原始字节拼回文件头。
11. 生成新文件名，不覆盖原档。
12. 用原版 signed short 读取规则重新校验 Strings archive。

校验必须满足：

```text
NegativeLengthCount == 0
LengthMismatchCount == 0
ParsedPosition == StringsLen
```

## 修复副作用

对本次两个样本，副作用是玩家履历/名声履历数据重置。因为坏项就是 `PlayerNotoriety` 的整段 JSON，替换成 `{}` 后，模组会按空状态重新初始化。

保守修复不改 ObjectData 和 ContainerData，也不改字符串 ID 的引用关系，只缩短坏字符串本身。因此比删除整个字符串表或重排 ID 风险低。

修复副本能进入游戏后，必须立刻使用已更新 DLL 另存一个新档，让后续保存改用 chunked 格式。

## 后续排查

如果后续仍出现坏档，优先看 `Logs/Mod_Logic` 里的 `SaveSize` 行：

```text
save_size key=... kind=json source=... bytes=...
save_size key=... kind=chunked_string source=... chunks=...
save_size key=... kind=dictionary source=... rawEntries=... maxValueBytes=...
```

重点关注：

- `bytes` 超过 `30000` 的 json
- `maxValueBytes` 超过 `30000` 的 dictionary value
- `chunkedItems` 持续增加的存储项
- `PlayerNotoriety` 的 `majorMaterials`、`pendingMaterials`、`maxMaterialBytes`
- `DialogueHistory`、`CompressedMemory`、`NpcAction`、`EventMaterial`

以后新增存档字段时，规则是：只要它是 JSON 字符串、长文本、对话历史、记忆材料、事件材料或 string dictionary value，就默认走 chunk helper，不要直接写入单条字符串。
