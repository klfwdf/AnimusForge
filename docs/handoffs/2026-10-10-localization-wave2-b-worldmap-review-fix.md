# Wave 2 B WorldMap Display Projection Review Fix

状态：INTENT / IN PROGRESS

范围：只修复 Wave 2 B 世界地图玩家可见通知的英文显示投影。保留 `LogFact` 写入的中文 AFEF 事实、命令状态、协议标签、存档字段和内部匹配文本；只在 `DisplayCommandMessage` 的显示边界将已知事实句式投影为本地化文本。

允许修改：

- `src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior*.cs`
- `src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.Admission.cs`
- `content/modules/AF.Module.WorldMap/ModuleData/Languages/afworldmap_strings.xml`
- `content/modules/AF.Module.WorldMap/ModuleData/Languages/CNs/afworldmap_strings-zh-CN.xml`

目标：覆盖审查确认的停留/巡逻/跟随完成、建队结果、队列结束、藏身处结算详情和伤亡摘要；不翻译未知动态事实，未知文本继续原样保留以避免误改模型记忆。

验证边界：运行资源键/占位符契约和本地化 DLL 专项；不构建、不 Stage、不部署、不 Push。
