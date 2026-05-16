# AnimusForge 自定义窗口方法

本文记录当前项目中已经使用的自定义窗口/界面层方案，以及新增窗口时应遵守的生命周期规则。项目目标为 Bannerlord 1.3.x，优先参考本仓库已有实现和原版 TaleWorlds Gauntlet/ScreenSystem 行为。

## 当前窗口类型

| 类型 | 代表文件 | 适用场景 | 关键风险 |
| --- | --- | --- | --- |
| 自建 Gauntlet 弹窗 | `ShoutTextInputPopup.cs`、`DevHistoryEditPopup.cs`、`DevWeeklyReportPopup.cs`、`TerminalWeeklyReportBrowserPopup.cs` | 需要完整自定义布局、长文本、滚动区、多按钮、列表浏览 | 不要在 `ScreenBase.Update` 正在枚举 layer 时同步 `RemoveLayer` |
| 原版 Inquiry/TextInquiry | `DevTextEditorHelper.cs`、`KnowledgeLibraryBehavior.cs`、`ModOnboardingBehavior.cs` | 简单确认、短文本输入、多步骤配置 | 长文本体验差，复杂布局不可控 |
| 原版 MultiSelectionInquiry | `AnimusForgeTerminalBehavior.cs`、`KnowledgeLibraryBehavior.cs`、`ShoutBehavior.cs` | 菜单、列表选择、分类入口 | 回调链复杂时要维护返回路径 |
| Mission 常驻 Gauntlet layer | `FloatingTextMissionView.cs` | 场景内浮动文本、气泡、HUD 叠层 | 只在 mission screen 生命周期内增删 layer |
| 原版窗口注入控件 | `EncyclopediaHeroPersonaPatch.cs`，详见 `docs/encyclopedia_button_injection_case.md` | 在百科 Hero/NPC 页增加按钮 | 需要拿真实 `IGauntletMovie.RootWidget`，并处理输入拦截 |

## 自建 Gauntlet 弹窗标准结构

自建窗口通常由三部分组成：

1. `*.cs` 窗口控制器  
   负责 `Show()`、`Open()`、`Close()`、`ScreenManager.TrySetFocus()`、`InputRestrictions`、pause request、异常日志。

2. `*VM.cs` 数据源  
   继承 `ViewModel`，公开 `[DataSourceProperty]` 属性，提供 `ExecuteXxx()` 命令给 prefab 绑定。

3. `AnimusForge/GUI/Prefabs/*.xml`  
   使用 `<Window>` 根节点，绑定 `Text="@Property"`、`Command.Click="ExecuteXxx"`、`DataSource="{Items}"` 等。

典型打开流程：

```csharp
ScreenBase topScreen = ScreenManager.TopScreen;
_layer = new GauntletLayer("PrefabName", 4000, false);
_layer.LoadMovie("PrefabName", _dataSource);
_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
topScreen.AddLayer(_layer);
_layer.IsFocusLayer = true;
ScreenManager.TrySetFocus(_layer);
```

典型关闭流程：

```csharp
_layer.IsFocusLayer = false;
ScreenManager.TryLoseFocus(_layer);
_screen.RemoveLayer(_layer);
_dataSource?.OnFinalize();
```

如果窗口会暂停游戏，还要配套：

```csharp
Game.Current?.GameStateManager?.RegisterActiveStateDisableRequest(this);
Game.Current?.GameStateManager?.UnregisterActiveStateDisableRequest(this);
```

## 关闭时机规则

不要从 Gauntlet 命令回调中直接移除当前 layer，尤其是输入框的 Enter、按钮 Click、焦点切换、热键处理。

危险路径示例：

```text
DevMultilineEditableTextWidget.HandleSinglePressKeys()
-> EventFired("TextEntered")
-> VM.ExecuteSubmit()
-> Popup.HandleSubmitRequested()
-> Close()
-> _screen.RemoveLayer(_layer)
```

如果此时 Bannerlord 正在执行：

```text
TaleWorlds.ScreenSystem.ScreenBase.Update(IReadOnlyList<int> lastKeysPressed)
```

`RemoveLayer` 会修改正在枚举的 `List<ScreenLayer>`，导致：

```text
Collection was modified; enumeration operation may not execute.
at System.Collections.Generic.List`1.Enumerator.MoveNextRare()
at TaleWorlds.ScreenSystem.ScreenBase.Update(...)
```

推荐做法是把提交/取消拆成两段：

1. Gauntlet 命令里只记录待处理状态。
2. 在 `SubModule.OnApplicationTick()` 或其他安全 tick 中统一关闭 layer 并执行回调。

当前 `ShoutTextInputPopup` 已采用该模式：

- `HandleSubmitRequested()` 只调用 `RequestDeferredClose(PendingCloseAction.Submit, inputText)`。
- `HandleCancelRequested()` 只调用 `RequestDeferredClose(PendingCloseAction.Cancel, null)`。
- `SubModule.OnApplicationTick()` 调用 `ShoutTextInputPopup.ProcessDeferredCloseIfNeeded()`。
- `ProcessPendingCloseAction()` 中才执行 `Close()` 和 `_onSubmit/_onCancel`。

新增带输入框或按钮的自建弹窗，优先照这个模式写。只读展示窗口如果关闭按钮也可能在同一帧触发 layer 枚举，同样建议延迟关闭。

## 现有自建窗口清单

### `ShoutTextInputPopup`

用途：T 键场景喊话输入框，支持多行输入，Enter 发送，Shift+Enter 换行。

文件：

- `ShoutTextInputPopup.cs`
- `ShoutTextInputPopupVM.cs`
- `AnimusForge/GUI/Prefabs/ShoutTextInputPopup.xml`
- `DevMultilineEditableTextWidget.cs`

特点：

- 使用 `GauntletLayer("ShoutTextInputPopup", 1000, false)`。
- 打开时暂停 mission：`Mission.Current.Scene.TimeSpeed = 0f`。
- 注册 active state disable request，避免游戏继续推进。
- `HotkeyInputGuard.IsTextInputFocused()` 会把该窗口视为文本输入状态，阻止 T 键重复触发。
- Escape、焦点丢失、系统菜单等路径会请求取消。
- 提交/取消使用延迟关闭，避免 `ScreenBase.Update` 枚举期间 `RemoveLayer`。

注意：

- 不要把 `HandleSubmitRequested()` 改回同步 `Close()`。
- `DevMultilineEditableTextWidget` 的 `SubmitOnEnter="true"` 会让 Enter 触发 `Command.TextEntered="ExecuteSubmit"`。
- 如果要增加发送按钮，也应走同一个延迟关闭路径。

### `DevHistoryEditPopup`

用途：长文本编辑窗口，当前由 `DevTextEditorHelper.ShowLongTextEditor()` 优先调用；失败后 fallback 到原版 `InformationManager.ShowTextInquiry()`。

文件：

- `DevHistoryEditPopup.cs`
- `DevHistoryEditPopupVM.cs`
- `DevTextEditorHelper.cs`
- `AnimusForge/GUI/Prefabs/DevHistoryEditPopup.xml`

特点：

- 使用 `GauntletLayer("DevHistoryEditPopup", 4000, false)`。
- `DevMultilineEditableTextWidget` 负责长文本编辑。
- `ExecuteSave()` / `ExecuteCancel()` 由按钮触发。
- 当前关闭是同步 `Close()`；如果未来出现按钮点击崩溃，应按 `ShoutTextInputPopup` 改成延迟关闭。

### `DevWeeklyReportPopup`

用途：周报预览/长文本只读窗口。

文件：

- `DevWeeklyReportPopup.cs`
- `DevWeeklyReportPopupVM.cs`
- `AnimusForge/GUI/Prefabs/DevWeeklyReportPopup.xml`

特点：

- 使用 `GauntletLayer("DevWeeklyReportPopup", 4000, false)`。
- 支持滚动阅读正文，正文字号来自 `DuelSettings.WeeklyReportPopupBodyFontSize`。
- 注册 active state disable request。
- 当前关闭按钮同步 `Close()`；如果高频复现 `Collection was modified`，按延迟关闭模式处理。

### `TerminalWeeklyReportBrowserPopup`

用途：终端里的历史周报浏览器，左侧国家列表，右侧周报列表/详情入口。

文件：

- `TerminalWeeklyReportBrowserPopup.cs`
- `TerminalWeeklyReportBrowserPopupVM.cs`
- `AnimusForge/GUI/Prefabs/TerminalWeeklyReportBrowserPopup.xml`

特点：

- 使用 `GauntletLayer("TerminalWeeklyReportBrowserPopup", 4000, false)`。
- `CountryItems`、`ReportItems` 使用 `MBBindingList<T>` 绑定列表。
- item 模板中通过 `Command.Click="ExecuteSelect"`、`Command.Click="ExecuteViewFullReport"` 调用 VM 命令。
- 复杂列表窗口推荐继续使用这种 VM + `MBBindingList` 模式。
- 当前关闭按钮同步 `Close()`；新增更多按钮时建议统一改延迟关闭。

### `FloatingTextMissionView`

用途：场景内 NPC 头顶/附近浮动文本气泡。

文件：

- `FloatingTextMissionView.cs`
- `FloatingTextVM.cs`
- `FloatingTextItemVM.cs`
- `FloatingTextManager.cs`
- `AnimusForge/GUI/Prefabs/FloatingTextLayer.xml`

特点：

- 不是 popup，而是 mission 生命周期里的常驻 Gauntlet layer。
- 在 `OnMissionScreenInitialize()` 中尝试创建 layer。
- 若 `MissionScreen` 为空，会在 `OnMissionTick()` 中重试，最多 300 次。
- 在 `OnMissionScreenFinalize()` 中 `RemoveLayer()`，并清空 VM 和状态。
- 位置由 `SceneView.WorldPointToScreenPoint()` 转换，写入 item VM 的 `X/Y`。

注意：

- 不要从任意后台线程直接操作 `_dataSource.Items`。
- 不要在遍历 `_agentStates` 时直接删除当前项；现有实现先收集到 list，再统一 `RemoveItem()`，应保持。
- 该 layer 设置 `DoNotAcceptEvents="true"`，不应抢输入焦点。

## 原版 Inquiry 窗口

简单场景优先用原版窗口，不必自建 Gauntlet：

```csharp
InformationManager.ShowInquiry(new InquiryData(
	title,
	text,
	isAffirmativeOptionShown: true,
	isNegativeOptionShown: true,
	"确定",
	"取消",
	affirmativeAction,
	negativeAction));
```

文本输入：

```csharp
InformationManager.ShowTextInquiry(new TextInquiryData(
	title,
	description,
	isAffirmativeOptionShown: true,
	isNegativeOptionShown: true,
	"确定",
	"取消",
	onDone,
	onCancel,
	shouldInputBeObfuscated: false,
	inputValidator: null,
	soundEventPath: "",
	defaultInputText: initialText));
```

多选/菜单：

```csharp
MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
```

适合使用原版 Inquiry 的情况：

- 一次确认。
- 短文本输入。
- 简单列表选择。
- 多步骤配置向导。

不适合的情况：

- 多行长文本编辑。
- 需要自定义滚动布局、左右分栏、复杂 item 模板。
- 需要自定义输入控件行为，如 Enter 发送、Shift+Enter 换行。

## 百科窗口注入

百科页不是新开 popup，而是在原版百科 UI 中注入按钮或控件。涉及 Hero/NPC 百科页时，先读：

```text
docs/encyclopedia_button_injection_case.md
```

关键规则：

- 用 `GauntletMovie.Load` 获取真实 `IGauntletMovie.RootWidget`。
- 自动生成 prefab patch 只能作为 fallback。
- `ButtonWidget` 要接收事件，文字子控件不要抢事件。
- 自定义文本弹窗打开时，要阻止百科页面继续响应 Backspace、切页、前进/后退等快捷键。

## 新增窗口推荐模板

新增自建窗口时建议直接复制以下结构：

```csharp
public sealed class XxxPopup
{
	private enum PendingCloseAction
	{
		None,
		Confirm,
		Cancel
	}

	private static XxxPopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly XxxPopupVM _dataSource;
	private bool _isClosed;
	private PendingCloseAction _pendingCloseAction;

	public static bool Show(...)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		_activePopup?.Close(silent: true);
		var popup = new XxxPopup(topScreen, ...);
		popup.Open();
		_activePopup = popup;
		return true;
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		_activePopup?.ProcessPendingCloseAction();
	}

	private void Open()
	{
		_layer.LoadMovie("XxxPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void RequestDeferredClose(PendingCloseAction action)
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = action;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || _pendingCloseAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction action = _pendingCloseAction;
		_pendingCloseAction = PendingCloseAction.None;
		Close(silent: true);
		// invoke action callbacks here
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		_layer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_layer);
		_screen.RemoveLayer(_layer);
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}
```

同时在 `SubModule.OnApplicationTick()` 增加：

```csharp
XxxPopup.ProcessDeferredCloseIfNeeded();
```

## 检查清单

新增或修改窗口后至少检查：

- prefab 名称和 `LoadMovie("Name", vm)` 一致。
- prefab 已包含在 `AnimusForge/GUI/Prefabs/`。
- 文本输入窗口已被 `HotkeyInputGuard.IsTextInputFocused()` 覆盖。
- `InputRestrictions.SetInputRestrictions(true, InputUsageMask.All)` 是否符合预期。
- `TrySetFocus()` 和 `TryLoseFocus()` 成对。
- 有暂停需求时，active state disable request 成对注册/注销。
- `RemoveLayer()` 不在 Gauntlet 命令回调中同步执行。
- `ViewModel.OnFinalize()` 被调用。
- 列表 VM 使用 `MBBindingList<T>`，不要从后台线程直接改 UI 绑定集合。
- fallback 使用原版 Inquiry 时，不要丢失保存/取消回调。

## 本次 Enter 闪退经验

用户在 T 键喊话窗口按 Enter 发送时，曾出现：

```text
Collection was modified; enumeration operation may not execute.
at System.Collections.Generic.List`1.Enumerator.MoveNextRare()
at TaleWorlds.ScreenSystem.ScreenBase.Update(...)
```

判断依据：

- 崩溃发生在 `ScreenBase.Update`，不是 LLM 请求或对话后处理。
- Enter 触发 `DevMultilineEditableTextWidget` 的 `TextEntered` 命令。
- 旧实现会在命令回调中同步 `Close()` 并 `_screen.RemoveLayer(_layer)`。
- 这会修改 `ScreenBase.Update` 正在枚举的 layer 列表。

修复方式：

- `ShoutTextInputPopup` 改为延迟关闭。
- `SubModule.OnApplicationTick()` 下一轮安全处理提交/取消。

以后所有“按钮/Enter/热键导致关闭窗口”的自建 Gauntlet 窗口，都应默认采用延迟关闭。
