# Vengeance 源码维护约束

`Vengeance/Source` 是复仇处决的唯一权威源码。

工作区中的 `C:\Users\29310\Documents\骑砍2mod\projects\mods\RichExecutions\Source` 是指向本目录的目录联接。它们是同一份文件，不是两份需要同步的副本。

## 哪些改动会同时影响两个版本

修改 `Vengeance/Source` 中的任何文件，会同时进入：

- 独立模块 `RichExecutions.dll`。
- AnimusForge 1.3 与 1.4 的内置实现。

因此，这里只能放两个版本都必须拥有的行刑逻辑。AnimusForge 特有的启动、城镇袭击豁免、模块资源登记和原有俘虏处决隔离，不能写进本目录。

## AnimusForge 特有文件

这些文件只属于 AnimusForge，修改它们不会改变独立模块：

- `VengeanceRuntimeBridge.cs`：内置版启动、战役注册和卸载。
- `SubModule.cs`：调用复仇桥的 AnimusForge 生命周期。
- `SceneTauntBehavior.cs`：仅跳过当前任务中精确登记的复仇受刑者。
- `AnimusForge/SubModule.xml`：复仇物品登记。
- `AnimusForge/ModuleData/vengeance_items.xml`：内置版物品。
- `AnimusForge/ModuleData/Languages/vengeance_strings-en.xml`：内置版英文。
- `AnimusForge/ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml`：内置版简体中文。
- `AnimusForge/ModuleData/action_sets.xml` 与 `action_types.xml`：已有动作追加，不能整文件替换。
- `AnimusForge/GUI/Prefabs/RichExecutionJudgement.xml`、`RichExecutionSiteBuilder.xml`、`rex_speech_bubble.xml`：内置版界面。
- `AnimusForge/AssetPackages/vengeance_pack0.tpac`：内置版资源包，不能覆盖 AnimusForge 的 `pack0.tpac`。

## 不能破坏的身份

- 保持 `RichExecutions.Campaign.RichExecutionCampaignBehavior` 的命名空间和类型名。
- 保持 `_rex_*` 存档键、`Configs/RichExecutions` 预设目录和现有预设 GUID。
- 保持逻辑模块 ID `RichExecutions` 与玩家可见文本 ID。
- `Vengeance/Source/SubModule.cs` 只供独立模块使用。AnimusForge 工程必须继续排除它，避免内置 DLL 出现第二个 Bannerlord 模块入口。

## 双版本运行规则

- 只启用 AnimusForge 时，由内置版注册并运行。
- 只启用独立模块时，由 `RichExecutions.SubModule` 注册并运行。
- 两者同时启用时，AnimusForge 内置版优先。独立模块检测到 `VengeanceIntegration.IsEmbeddedHostActive` 后，不得注册菜单、战役行为、任务行为或界面。
- 同一场处决只能有一个结算所有者。AnimusForge 原有贵族俘虏处决不能自动转入复仇结算。

## 版本兼容

`Vengeance/Source` 必须同时通过 AnimusForge 的 1.3 与 1.4 构建。1.3 与 1.4 的引擎差异放在 `BannerlordApiCompatibility`，用运行时可用的方法解析，不能把 1.4 专用调用直接散落到业务代码里。

每次修改本目录后，至少验证：

- AnimusForge `BannerlordApi=1.3`。
- AnimusForge `BannerlordApi=1.4`。
- 独立 `RichExecutions.csproj` 的目标版本。

编译通过不等于游戏内的行刑、自定义刑场、旧存档和城镇场景已经验证。
