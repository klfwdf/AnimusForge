# Hero Encyclopedia Button Injection Case

本文档记录 `EncyclopediaHeroPersonaPatch` 的可复用做法，后续给百科页增加其他按钮时优先按这个案例改，不要只依赖聊天记录。

## 适用场景

- 在原版百科页上增加 Mod 自定义按钮。
- 按钮需要根据 MCM/运行时状态显示或隐藏。
- 点击按钮后打开自定义 Gauntlet 弹窗、菜单或编辑器。
- 弹窗输入期间不能让原版百科继续响应返回、前进、后退、切页等快捷键。

## 当前案例文件

- `EncyclopediaHeroPersonaPatch.cs`
- `MyBehavior.cs`
- `DevHistoryEditPopup.cs`
- `AnimusForge/GUI/Prefabs/DevHistoryEditPopup.xml`

## 关键结论

1. Hero 百科按钮不要只 patch 自动生成 prefab 的 root。
   当前有效入口是 `GauntletMovie.Load`，当 `movieName == "EncyclopediaHeroPage"` 且 datasource 是 `EncyclopediaHeroPageVM` 时，从 `IGauntletMovie.RootWidget` 注入按钮。

2. 自动生成类 patch 可以作为 fallback。
   `GeneratedUIPrefabCreator.CreateEncyclopediaHeroPage...`、`GeneratedHeroPageRoot.CreateWidgets`、`GeneratedHeroPageRoot.SetDataSource` 可以保留，但不要把它们当唯一入口。

3. 按钮要接收事件，文字子控件不要抢事件。
   `ButtonWidget` 需要 `DoNotAcceptEvents = false`，文本 `TextWidget` 需要 `DoNotAcceptEvents = true`，并设置 `DoNotPassEventsToChildren = true`，否则按钮可能看得见但点不动。

4. 数据源需要缓存。
   真实 root 是普通 `Widget` 时，不能总是从生成类字段 `_datasource_Root` 读到 VM。当前用 `ConditionalWeakTable<object, EncyclopediaHeroPageVM>` 绑定 root 和 datasource。

5. 弹窗输入要拦住百科自己的 tick。
   原版 `SandBox.GauntletUI.Encyclopedia.EncyclopediaData.OnTick()` 会在 `!_activeGauntletLayer.IsFocusedOnInput()` 时处理 `BackSpace`、`SwitchToPreviousTab`、`SwitchToNextTab`。我们的编辑框在另一个 Gauntlet layer，所以必须在 `DevHistoryEditPopup.IsOpen` 时跳过百科 `OnTick()`。

## 最小复用模板

```csharp
MethodInfo loadMovie = AccessTools.Method(typeof(GauntletMovie), nameof(GauntletMovie.Load));
activeHarmony.Patch(loadMovie, postfix: new HarmonyMethod(typeof(MyPatch), nameof(LoadMoviePostfix)));

MethodInfo encyclopediaTick = AccessTools.Method(typeof(EncyclopediaData), "OnTick");
activeHarmony.Patch(encyclopediaTick, prefix: new HarmonyMethod(typeof(MyPatch), nameof(EncyclopediaDataOnTickPrefix)));

public static void LoadMoviePostfix(string movieName, IViewModel datasource, IGauntletMovie __result)
{
	if (!string.Equals(movieName, "EncyclopediaHeroPage", StringComparison.Ordinal) ||
		datasource is not EncyclopediaHeroPageVM vm)
	{
		return;
	}

	Widget root = __result?.RootWidget;
	TrackRootDataSource(root, vm);
	EnsureButton(root);
	UpdateButtonState(root);
}

public static bool EncyclopediaDataOnTickPrefix()
{
	return !DevHistoryEditPopup.IsOpen;
}
```

## 新增按钮清单

- 明确按钮属于哪个百科 movie，例如 `EncyclopediaHeroPage`。
- 用 `GauntletMovie.Load` 拿真实 `RootWidget`。
- 用稳定 ID，例如 `AnimusForgeSomeFeatureButton`，避免重复添加。
- 先定位可用父控件；Hero 页当前可参考 `ClanDivider.ParentWidget` 或 `RightSideList`。
- `ButtonWidget.ClickEventHandlers.Add(...)` 内重新解析当前 datasource，不要捕获过期 Hero。
- 用 MCM 或业务状态统一控制 `button.IsVisible`。
- 打开文本输入弹窗时，确认底层页面的 `OnTick` 或热键层被暂停。
- 加一条首次状态日志，记录 datasource、hero、MCM 开关、visible，方便玩家回报截图时查日志。

## 不建议的做法

- 不要只改 XML，原版自动生成 UI 和真实运行 root 可能不是同一个入口。
- 不要把按钮做成纯背景图片或只加 `TextWidget`，那样容易没有点击事件。
- 不要让文字子控件接收事件，否则会挡住父按钮。
- 不要只依赖 `InputRestrictions.SetInputRestrictions(true, InputUsageMask.All)`，它不能阻止另一个已存在 layer 的 `OnTick()` 主动处理快捷键。
- 不要修改项目构建/覆盖流程来验证 UI 注入问题；本项目已有自己的覆盖流程。

## 后续抽象建议

当第二个百科按钮也要加入时，再提取一个小 helper，例如：

- `EncyclopediaWidgetInjection.TryInjectButton(...)`
- `EncyclopediaWidgetInjection.TrackDataSource(...)`
- `EncyclopediaInputBlocker.IsBlockingEncyclopediaInput`

现在先保留当前案例，避免为了一个按钮过早抽象。
