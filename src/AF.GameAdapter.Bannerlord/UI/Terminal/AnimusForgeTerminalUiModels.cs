using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AFWarStatsTerminal.UI;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeTerminalNode
{
	public string Id { get; set; } = "";
	public string Title { get; set; } = "";
	public string Hint { get; set; } = "";
	public string SearchTerms { get; set; } = "";
	public string Category { get; set; } = TerminalCategories.All;
	public string Icon { get; set; } = "❖";
	public Action OnExecute { get; set; }
	public Func<ImageIdentifier> ImageFactory { get; set; }
	public bool IsBranch => Children != null && Children.Count > 0;
	public List<AnimusForgeTerminalNode> Children { get; } = new List<AnimusForgeTerminalNode>();
}

public enum TerminalViewMode
{
	MenuList,
	WarStats,
	WeeklyReports,
	Vassalage,
	Details,
	Diagnostics
}

public enum TerminalItemKind
{
	Header,
	Action,
	BoolSetting,
	NumericSetting,
	DropdownSetting,
	TextSetting,
	ButtonSetting,
	HotkeySetting
}

public sealed class AnimusForgeTerminalPopup
{
	private static AnimusForgeTerminalPopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly AnimusForgeTerminalPopupVM _dataSource;
	private readonly Action _onClose;
	private bool _isClosed;
	private bool _consumeEscapeRelease;
	private bool _encyclopediaSuspended;
	private readonly long _saveGeneration = SaveRuntimeGuard.CaptureGeneration();

	public static AnimusForgeTerminalPopup ActivePopup => _activePopup;

	private AnimusForgeTerminalPopup(ScreenBase screen, List<AnimusForgeTerminalNode> roots, Func<string, bool> onExecuteLeaf, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new AnimusForgeTerminalPopupVM(roots, onExecuteLeaf, HandleCloseRequested);
		_layer = new GauntletLayer("AnimusForgeTerminalPopup", 309, false);
	}

	public static bool Show(List<AnimusForgeTerminalNode> roots, Func<string, bool> onExecuteLeaf, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null || roots == null || roots.Count <= 0 || onExecuteLeaf == null)
		{
			return false;
		}
		AnimusForgeTerminalPopup popup = null;
		try
		{
			_activePopup?.Close(silent: true);
			popup = new AnimusForgeTerminalPopup(topScreen, roots, onExecuteLeaf, onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] Failed to open terminal popup: " + ex);
			popup?.Close(silent: true);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void CloseActive(bool silent = true)
	{
		_activePopup?.Close(silent);
	}

	public AnimusForgeTerminalPopupVM ViewModel => _dataSource;

	private void Open()
	{
		_layer.LoadMovie("AnimusForgeTerminalPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		// Native encyclopedia links must render above this terminal (310).
		_screen.AddLayer(_layer);
		Game.Current?.GameStateManager?.RegisterActiveStateDisableRequest(this);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	public static void TickActive()
	{
		AnimusForgeTerminalPopup popup = _activePopup;
		if (popup == null || popup._isClosed) return;
		if (!SaveRuntimeGuard.IsCurrentGeneration(popup._saveGeneration)) { popup.Close(silent: true); return; }
		popup._dataSource.WeeklyReportVm?.Tick();
		if (popup._encyclopediaSuspended) return;
		if (ScreenManager.TopScreen != popup._screen)
		{
			popup.HandleCloseRequested();
			return;
		}
		if (ScreenManager.FocusedLayer != popup._layer) return;
		bool wasListening = popup._dataSource.IsListeningForKey;
		if (wasListening && Input.IsKeyPressed(InputKey.Escape)) popup._consumeEscapeRelease = true;
		popup._dataSource.OnTick();
		if (Input.IsKeyReleased(InputKey.Escape) && popup._consumeEscapeRelease)
		{
			popup._consumeEscapeRelease = false;
			return;
		}
		if (!wasListening && popup._layer.Input.IsHotKeyReleased("Exit"))
		{
			if (popup._dataSource.WeeklyReportVm?.IsReading == true) popup._dataSource.WeeklyReportVm.ExecuteReturnToList();
			else popup.HandleCloseRequested();
		}
	}

	internal void OpenArchiveEncyclopediaLink(string link)
	{
		if (_isClosed || !SaveRuntimeGuard.IsCurrentGeneration(_saveGeneration)) return;
		EncyclopediaEntityLinkNavigationCoordinator.Request(link, () =>
		{
			if (_isClosed || !SaveRuntimeGuard.IsCurrentGeneration(_saveGeneration)) return;
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false; ScreenManager.TryLoseFocus(_layer);
			ScreenManager.SetSuspendLayer(_layer, isSuspended: true); _encyclopediaSuspended = true;
		}, () =>
		{
			if (_isClosed) return;
			if (!SaveRuntimeGuard.IsCurrentGeneration(_saveGeneration) || !ReferenceEquals(ScreenManager.TopScreen, _screen)) { Close(silent: true); return; }
			ScreenManager.SetSuspendLayer(_layer, isSuspended: false); _encyclopediaSuspended = false;
			_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			_layer.IsFocusLayer = true; ScreenManager.TrySetFocus(_layer);
		}, () => !_isClosed && SaveRuntimeGuard.IsCurrentGeneration(_saveGeneration));
	}

	private void HandleCloseRequested()
	{
		Close(silent: true);
		_onClose?.Invoke();
	}

	public void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		if (_dataSource != null && _dataSource.HasUnsavedChanges)
		{
			if (!TerminalSettingsRegistry.SaveSettings())
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_SaveFailed}Terminal settings could not be saved. Check MCM or the log and retry.").ToString(), Colors.Red));
		}
		try
		{
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				Logger.Log("Terminal", "[WARN] Failed to remove terminal popup layer: " + ex.Message);
			}
		}
		_layer.InputRestrictions.ResetInputRestrictions();
		Game.Current?.GameStateManager?.UnregisterActiveStateDisableRequest(this);
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class AnimusForgeTerminalPopupVM : ViewModel
{
	private readonly Func<string, bool> _onExecuteLeaf;
	private readonly Action _onClose;
	private readonly List<AnimusForgeTerminalNode> _roots;
	private readonly Stack<AnimusForgeTerminalNode> _path = new Stack<AnimusForgeTerminalNode>();

	private string _titleText = new TextObject("{=AF_UI_Terminal_Title}AnimusForge Terminal").ToString();
	private string _subtitleText = new TextObject("{=AF_UI_Terminal_Subtitle}Choose a tool or adjust a mechanism setting. Search, paginate, then press ESC or Close to exit.").ToString();
	private string _breadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString();
	private string _selectedTab = TerminalCategories.All;
	private bool _hasUnsavedChanges;
	private bool _canGoBack;
	private readonly HashSet<string> _collapsedGroups = new HashSet<string>(StringComparer.Ordinal);

	private TerminalViewMode _currentViewMode = TerminalViewMode.MenuList;

	private MBBindingList<AnimusForgeTerminalTabItemVM> _tabItems;
	private MBBindingList<AnimusForgeTerminalItemVM> _items;

	// 内嵌模块：战争统计
	private AfWarStatsPopupVM _warStatsVm;

	// 内嵌模块：国家周报
	private TerminalWeeklyReportBrowserPopupVM _weeklyReportVm;

	// 内嵌模块：臣属国与贡金记录
	private TerminalVassalageTributeHistoryPopupVM _vassalageVm;

	private AnimusForgeTagCatalogSnapshot _tagCatalogSnapshot;
	private AnimusForgeTerminalNode _tagBrowser;

	private const int MenuPageSize = 50;
	private string _searchText = "";
	private int _menuPage;
	private int _filteredCount;
	private string _detailText = "";
	private Action _returnToView;

	// 内嵌模块：AI 错误诊断
	private string _diagnosticsStatusText = new TextObject("{=AF_UI_Terminal_DiagnosticsOnline}Sentinel status: online").ToString();
	private string _diagnosticsDetailText = new TextObject("{=AF_UI_Terminal_DiagnosticsEmpty}No recent exception is waiting for analysis.").ToString();
	private bool _hasLatestErrorToAnalyze;

	[DataSourceProperty]
	public string TitleText { get => _titleText; set { if (value != _titleText) { _titleText = value; OnPropertyChangedWithValue(value, nameof(TitleText)); } } }

	[DataSourceProperty]
	public string SubtitleText { get => _subtitleText; set { if (value != _subtitleText) { _subtitleText = value; OnPropertyChangedWithValue(value, nameof(SubtitleText)); } } }

	[DataSourceProperty]
	public string BreadcrumbText { get => _breadcrumbText; set { if (value != _breadcrumbText) { _breadcrumbText = value; OnPropertyChangedWithValue(value, nameof(BreadcrumbText)); } } }

	[DataSourceProperty]
	public bool CanGoBack { get => _canGoBack; set { if (value != _canGoBack) { _canGoBack = value; OnPropertyChangedWithValue(value, nameof(CanGoBack)); } } }

	[DataSourceProperty]
	public MBBindingList<AnimusForgeTerminalTabItemVM> TabItems { get => _tabItems; set { if (value != _tabItems) { _tabItems = value; OnPropertyChangedWithValue(value, nameof(TabItems)); } } }

	[DataSourceProperty]
	public MBBindingList<AnimusForgeTerminalItemVM> Items { get => _items; set { if (value != _items) { _items = value; OnPropertyChangedWithValue(value, nameof(Items)); } } }

	[DataSourceProperty]
	public bool IsMenuListVisible => _currentViewMode == TerminalViewMode.MenuList;

	[DataSourceProperty]
	public bool IsBreadcrumbVisible => _currentViewMode != TerminalViewMode.WarStats;

	[DataSourceProperty]
	public bool IsWarStatsVisible => _currentViewMode == TerminalViewMode.WarStats;

	[DataSourceProperty]
	public bool IsWeeklyReportsVisible => _currentViewMode == TerminalViewMode.WeeklyReports;

	[DataSourceProperty]
	public bool IsVassalageVisible => _currentViewMode == TerminalViewMode.Vassalage;

	[DataSourceProperty]
	public bool IsTagCatalogBrowser => _currentViewMode == TerminalViewMode.MenuList && _tagBrowser != null
		&& _path.Count > 0 && ReferenceEquals(_path.Peek(), _tagBrowser);
	[DataSourceProperty]
	public bool IsSearchVisible => _currentViewMode == TerminalViewMode.MenuList && (string.Equals(_selectedTab, TerminalCategories.All, StringComparison.Ordinal) || IsTagCatalogBrowser);
	[DataSourceProperty]
	public float MenuContentTop => IsTagCatalogBrowser ? 98f : (IsSearchVisible ? 50f : 6f);
	[DataSourceProperty]
	public string TagCatalogStatusText
	{
		get
		{
			if (_tagCatalogSnapshot == null) return "";
			var text = new TextObject("{=AF_UI_Terminal_IndexStatus}Index: {COUNT} entries · updated {TIME}; parameters and sources are searchable");
			text.SetTextVariable("COUNT", _tagCatalogSnapshot.Entries.Count.ToString());
			text.SetTextVariable("TIME", _tagCatalogSnapshot.BuiltUtc.ToLocalTime().ToString("HH:mm:ss"));
			return text.ToString();
		}
	}

	[DataSourceProperty]
	public bool IsDetailsVisible => _currentViewMode == TerminalViewMode.Details;

	[DataSourceProperty]
	public bool IsDiagnosticsVisible => _currentViewMode == TerminalViewMode.Diagnostics;

	[DataSourceProperty]
	public bool HasUnsavedChanges => _hasUnsavedChanges;
	public bool IsListeningForKey => _activeListeningItem != null;

	// 内嵌子视图属性绑定
	[DataSourceProperty]
	public AfWarStatsPopupVM WarStatsVm { get => _warStatsVm; set { if (value != _warStatsVm) { _warStatsVm = value; OnPropertyChangedWithValue(value, nameof(WarStatsVm)); } } }

	[DataSourceProperty]
	public TerminalWeeklyReportBrowserPopupVM WeeklyReportVm { get => _weeklyReportVm; set { if (value != _weeklyReportVm) { _weeklyReportVm = value; OnPropertyChangedWithValue(value, nameof(WeeklyReportVm)); } } }

	[DataSourceProperty]
	public TerminalVassalageTributeHistoryPopupVM VassalageVm { get => _vassalageVm; set { if (value != _vassalageVm) { _vassalageVm = value; OnPropertyChangedWithValue(value, nameof(VassalageVm)); } } }

	[DataSourceProperty]
	public string SearchText
	{
		get => _searchText;
		set
		{
			if (_searchText == (value ?? "")) return;
			_searchText = value ?? "";
			_menuPage = 0;
			OnPropertyChangedWithValue(_searchText, nameof(SearchText));
			OnPropertyChanged(nameof(IsSearchEmpty));
			RefreshItems();
		}
	}

	[DataSourceProperty]
	public bool IsSearchEmpty => string.IsNullOrWhiteSpace(_searchText);

	[DataSourceProperty]
	public string MenuPageText
	{
		get
		{
			var text = new TextObject("{=AF_UI_Terminal_Page}Page {PAGE} · {COUNT} items");
			text.SetTextVariable("PAGE", $"{_menuPage + 1}/{Math.Max(1, (_filteredCount + MenuPageSize - 1) / MenuPageSize)}");
			text.SetTextVariable("COUNT", _filteredCount.ToString());
			return text.ToString();
		}
	}
	[DataSourceProperty]
	public bool HasPreviousMenuPage => _menuPage > 0;
	[DataSourceProperty]
	public bool HasNextMenuPage => (_menuPage + 1) * MenuPageSize < _filteredCount;
	[DataSourceProperty]
	public string DetailText { get => _detailText; private set { _detailText = value ?? ""; OnPropertyChangedWithValue(_detailText, nameof(DetailText)); } }

	public void ExecutePreviousMenuPage() { if (HasPreviousMenuPage) { _menuPage--; RefreshItems(); } }
	public void ExecuteNextMenuPage() { if (HasNextMenuPage) { _menuPage++; RefreshItems(); } }

	[DataSourceProperty]
	public string DiagnosticsStatusText { get => _diagnosticsStatusText; set { if (value != _diagnosticsStatusText) { _diagnosticsStatusText = value; OnPropertyChangedWithValue(value, nameof(DiagnosticsStatusText)); } } }

	[DataSourceProperty]
	public string DiagnosticsDetailText { get => _diagnosticsDetailText; set { if (value != _diagnosticsDetailText) { _diagnosticsDetailText = value; OnPropertyChangedWithValue(value, nameof(DiagnosticsDetailText)); } } }

	[DataSourceProperty]
	public bool HasLatestErrorToAnalyze { get => _hasLatestErrorToAnalyze; set { if (value != _hasLatestErrorToAnalyze) { _hasLatestErrorToAnalyze = value; OnPropertyChangedWithValue(value, nameof(HasLatestErrorToAnalyze)); } } }

	// Static labels from the terminal prefab are resolved through TextObject.
	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_001Text => new TextObject("{=AF_UI_Terminal_Prefab_001}Search:").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_002Text => new TextObject("{=AF_UI_Terminal_Prefab_002}Enter keywords to search settings or features...").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_003Text => new TextObject("{=AF_UI_Terminal_Prefab_003}Previous page").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_004Text => new TextObject("{=AF_UI_Terminal_Prefab_004}Next page").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_005Text => new TextObject("{=AF_UI_Terminal_Prefab_005}Index information").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_006Text => new TextObject("{=AF_UI_Terminal_Prefab_006}Export as TXT").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_007Text => new TextObject("{=AF_UI_Terminal_Prefab_007}Refresh index").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_008Text => new TextObject("{=AF_UI_Terminal_Prefab_008}All").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_009Text => new TextObject("{=AF_UI_Terminal_Prefab_009}Bulletin").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_010Text => new TextObject("{=AF_UI_Terminal_Prefab_010}Diplomacy").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_011Text => new TextObject("{=AF_UI_Terminal_Prefab_011}Policies").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_012Text => new TextObject("{=AF_UI_Terminal_Prefab_012}Recent events").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_013Text => new TextObject("{=AF_UI_Terminal_Prefab_013}Weekly report").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_014Text => new TextObject("{=AF_UI_Terminal_Prefab_014}Back to list").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_015Text => new TextObject("{=AF_UI_Terminal_Prefab_015}Generate full report").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_016Text => new TextObject("{=AF_UI_Terminal_Prefab_016}Run AI diagnostic analysis").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_017Text => new TextObject("{=AF_UI_Terminal_Prefab_017}Open with hotkey").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_018Text => new TextObject("{=AF_UI_Terminal_Prefab_018}Icon entry").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_019Text => new TextObject("{=AF_UI_Terminal_Prefab_019}Save after editing; press ESC or Close to exit").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_020Text => new TextObject("{=AF_UI_Terminal_Prefab_020}Back to previous level").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_021Text => new TextObject("{=AF_UI_Terminal_Prefab_021}Save settings").ToString();

	[DataSourceProperty]
	public string AF_UI_Terminal_Prefab_022Text => new TextObject("{=AF_UI_Terminal_Prefab_022}Close").ToString();

	public AnimusForgeTerminalPopupVM(List<AnimusForgeTerminalNode> roots, Func<string, bool> onExecuteLeaf, Action onClose)
	{
		_onExecuteLeaf = onExecuteLeaf;
		_onClose = onClose;
		_roots = (roots ?? new List<AnimusForgeTerminalNode>()).Where(x => x != null).ToList();
		TabItems = new MBBindingList<AnimusForgeTerminalTabItemVM>();
		Items = new MBBindingList<AnimusForgeTerminalItemVM>();

		string[] tabs = new[] { TerminalCategories.War, TerminalCategories.All, TerminalCategories.Diplomacy, TerminalCategories.Prompt, TerminalCategories.Ai, TerminalCategories.Duel, TerminalCategories.Scene, TerminalCategories.Characters };
		foreach (string tab in tabs)
		{
			TabItems.Add(new AnimusForgeTerminalTabItemVM(tab, LocalizeTab(tab), SelectTab));
		}

		// 默认折叠“全部”标签页下的 MCM 二级分组，避免首屏内容过长
		foreach (var d in TerminalSettingsRegistry.AllDefinitions)
		{
			if (!string.IsNullOrEmpty(d.GroupName))
			{
				_collapsedGroups.Add($"{TerminalCategories.All}/{d.GroupName}");
			}
		}

		RefreshItems();
	}

	private static string LocalizeTab(string tab) => new TextObject(TerminalCategories.DisplayText(tab)).ToString();

	public void ToggleGroupCollapse(string groupKey)
	{
		if (string.IsNullOrEmpty(groupKey)) return;
		if (_collapsedGroups.Contains(groupKey))
		{
			_collapsedGroups.Remove(groupKey);
		}
		else
		{
			_collapsedGroups.Add(groupKey);
		}
		RefreshItems();
	}

	public void SelectTab(string tab)
	{
		_selectedTab = string.IsNullOrWhiteSpace(tab) ? TerminalCategories.All : tab;
		CancelListeningKey();
		_path.Clear();
		_tagBrowser = null;
		_tagCatalogSnapshot = null;
		_returnToView = null;
		ResetMenuFilter();
		if (string.Equals(_selectedTab, TerminalCategories.War, StringComparison.Ordinal))
		{
			ShowWarStats();
			return;
		}
		SetViewMode(TerminalViewMode.MenuList);
		RefreshItems();
	}

	private void SetViewMode(TerminalViewMode mode)
	{
		_currentViewMode = mode;
		OnPropertyChanged(nameof(IsMenuListVisible));
		OnPropertyChanged(nameof(IsBreadcrumbVisible));
		OnPropertyChanged(nameof(IsWarStatsVisible));
		OnPropertyChanged(nameof(IsWeeklyReportsVisible));
		OnPropertyChanged(nameof(IsVassalageVisible));
		OnPropertyChanged(nameof(IsTagCatalogBrowser));
		OnPropertyChanged(nameof(IsSearchVisible));
		OnPropertyChanged(nameof(MenuContentTop));
		OnPropertyChanged(nameof(IsDetailsVisible));
		OnPropertyChanged(nameof(IsDiagnosticsVisible));
		UpdateCanGoBack();
	}

	private void UpdateCanGoBack()
	{
		CanGoBack = _currentViewMode != TerminalViewMode.MenuList || _path.Count > 0;
	}

	public void MarkModified()
	{
		_hasUnsavedChanges = true;
		OnPropertyChanged(nameof(HasUnsavedChanges));
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings != null && Items != null)
		{
			foreach (var item in Items)
			{
				item.UpdateFromSettings(settings);
			}
		}
	}

	private AnimusForgeTerminalItemVM _activeListeningItem;

	public void StartListeningKey(AnimusForgeTerminalItemVM item)
	{
		if (_activeListeningItem != null && _activeListeningItem != item)
		{
			CancelListeningKey();
		}
		_activeListeningItem = item;
		item.IsListeningKey = true;
		item.DisplayValue = new TextObject("{=AF_UI_Terminal_AnyKey}Press any key (ESC cancels)...").ToString();
		item.OnPropertyChanged(nameof(item.DisplayValue));
	}

	public void CancelListeningKey()
	{
		if (_activeListeningItem != null)
		{
			_activeListeningItem.IsListeningKey = false;
			DuelSettings s = DuelSettings.GetSettings();
			string orig = s != null ? _activeListeningItem.SettingDef?.Getter(s)?.ToString() ?? "" : "";
			_activeListeningItem.DisplayValue = string.IsNullOrWhiteSpace(orig) ? new TextObject("{=AF_UI_Terminal_Unset}Not set").ToString() : orig.ToUpperInvariant();
			_activeListeningItem.OnPropertyChanged(nameof(_activeListeningItem.DisplayValue));
			_activeListeningItem = null;
		}
	}

	public void RefreshCurrentItemsFromSettings()
	{
		DuelSettings currentSettings = DuelSettings.GetSettings();
		if (currentSettings != null && Items != null)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				Items[i]?.UpdateFromSettings(currentSettings);
			}
		}
	}

	public void OnTick()
	{
		if (DuelSettings.ModelDropdownsDirty)
		{
			DuelSettings.ModelDropdownsDirty = false;
			RefreshCurrentItemsFromSettings();
		}

		if (_activeListeningItem != null)
		{
			if (Input.IsKeyPressed(InputKey.Escape))
			{
				CancelListeningKey();
				return;
			}

			int keyNum = Input.GetFirstKeyPressedInRange(0);
			if (keyNum >= 0 && keyNum < 256)
			{
				InputKey key = (InputKey)keyNum;
				if (key != InputKey.LeftMouseButton && key != InputKey.RightMouseButton && key != InputKey.MiddleMouseButton && key != InputKey.MouseScrollUp && key != InputKey.MouseScrollDown && key != InputKey.Invalid)
				{
					string keyName = key.ToString();
					DuelSettings settings = DuelSettings.GetSettings();
					if (settings != null && _activeListeningItem.SettingDef != null)
					{
						_activeListeningItem.SettingDef.Setter(settings, keyName);
						_activeListeningItem.DisplayValue = keyName.ToUpperInvariant();
						_activeListeningItem.OnPropertyChanged(nameof(_activeListeningItem.DisplayValue));
						_activeListeningItem.IsListeningKey = false;
						MarkModified();
						var hotkeyMessage = new TextObject("{=AF_UI_Terminal_HotkeySet}Hotkey {TITLE} set to [{KEY}]");
						hotkeyMessage.SetTextVariable("TITLE", _activeListeningItem.Title ?? "");
						hotkeyMessage.SetTextVariable("KEY", keyName);
						InformationManager.DisplayMessage(new InformationMessage(hotkeyMessage.ToString(), Colors.Green));
					}
					_activeListeningItem = null;
				}
			}
		}
	}

	private void RefreshItems()
	{
		CancelListeningKey();
		foreach (AnimusForgeTerminalTabItemVM tab in TabItems)
			tab.IsSelected = string.Equals(tab.TabId, _selectedTab, StringComparison.Ordinal);

		string query = _searchText.Trim();
		var list = new MBBindingList<AnimusForgeTerminalItemVM>();
		if (_path.Count > 0)
		{
			// Query pages retain every identity without creating off-page portraits or settings.
			var nodes = _path.Peek().Children.Where(node => query.Length == 0 || NodeMatchesQuery(node, query)).ToList();
			_filteredCount = nodes.Count;
			ClampMenuPage();
			foreach (var node in nodes.Skip(_menuPage * MenuPageSize).Take(MenuPageSize))
				list.Add(new AnimusForgeTerminalItemVM(node, OpenNode));
		}
		else
		{
			var rootItems = new List<AnimusForgeTerminalItemVM>();
			var nodes = _selectedTab == TerminalCategories.All ? _roots : _roots.Where(node => string.Equals(node.Category, _selectedTab, StringComparison.Ordinal));
			foreach (var node in nodes.Where(node => query.Length == 0 || NodeMatchesQuery(node, query)))
				rootItems.Add(new AnimusForgeTerminalItemVM(node, OpenNode));

			var definitions = _selectedTab == TerminalCategories.All ? TerminalSettingsRegistry.AllDefinitions : TerminalSettingsRegistry.GetByCategory(_selectedTab);
			foreach (var group in definitions.Where(def => query.Length == 0 || SettingMatchesQuery(def, query)).GroupBy(def => def.GroupName))
			{
				string groupKey = _selectedTab + "/" + group.Key;
				bool collapsed = query.Length == 0 && _collapsedGroups.Contains(groupKey);
				var countText = new TextObject("{=AF_UI_Terminal_ItemsCount}{COUNT} items");
				countText.SetTextVariable("COUNT", group.Count());
				rootItems.Add(AnimusForgeTerminalItemVM.CreateCollapsibleHeader(
					(collapsed ? "▶  " : "▼  ") + group.Key, countText.ToString(), groupKey, collapsed, ToggleGroupCollapse));
				if (!collapsed)
					foreach (var def in group) rootItems.Add(new AnimusForgeTerminalItemVM(def, MarkModified));
			}
			_filteredCount = rootItems.Count;
			ClampMenuPage();
			foreach (var item in rootItems.Skip(_menuPage * MenuPageSize).Take(MenuPageSize)) list.Add(item);
		}
		OnPropertyChanged(nameof(IsTagCatalogBrowser));
		OnPropertyChanged(nameof(IsSearchVisible));
		OnPropertyChanged(nameof(IsSearchEmpty));
		OnPropertyChanged(nameof(MenuContentTop));
		OnPropertyChanged(nameof(MenuPageText));
		OnPropertyChanged(nameof(HasPreviousMenuPage));
		OnPropertyChanged(nameof(HasNextMenuPage));
		Items = list;
		UpdateBreadcrumb();
		UpdateCanGoBack();
	}

	private void ClampMenuPage()
	{
		_menuPage = Math.Min(_menuPage, Math.Max(0, (_filteredCount - 1) / MenuPageSize));
	}

	private static bool NodeMatchesQuery(AnimusForgeTerminalNode node, string q)
	{
		if (node == null) return false;
		if (node.Title?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (node.Hint?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (node.SearchTerms?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (node.Id?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		return false;
	}

	private static bool SettingMatchesQuery(TerminalSettingDef def, string q)
	{
		if (def == null) return false;
		if (def.DisplayName?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (def.Id?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (def.HintText?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (def.GroupName?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		return false;
	}

	public void ExecuteSaveSettings()
	{
		if (TerminalSettingsRegistry.SaveSettings())
		{
			_hasUnsavedChanges = false;
			OnPropertyChanged(nameof(HasUnsavedChanges));
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_Saved}AnimusForge terminal settings saved.").ToString(), Colors.Green));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_SaveError}Settings could not be saved. Check the log.").ToString(), Colors.Red));
		}
	}

	private void UpdateBreadcrumb()
	{
		if (_currentViewMode != TerminalViewMode.MenuList)
		{
			return;
		}
		if (_path.Count > 0)
		{
			BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString()
				+ " / " + LocalizeTab(_selectedTab) + " / " + string.Join(" / ", _path.Reverse().Select(x => x.Title));
		}
		else
		{
			BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString()
				+ " / " + LocalizeTab(_selectedTab);
		}
	}

	public void OpenNode(AnimusForgeTerminalNode node)
	{
		if (node == null)
		{
			return;
		}
		if (node.IsBranch)
		{
			_path.Push(node);
			ResetMenuFilter();
			RefreshItems();
			return;
		}

		try
		{
			if (node.OnExecute != null) node.OnExecute();
			else _onExecuteLeaf?.Invoke(node.Id ?? "");
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] terminal leaf failed id=" + node.Id + ": " + ex);
		}
	}

	// --- 子视图切换与内嵌展示接口 ---

	public void ShowWarStats()
	{
		_selectedTab = TerminalCategories.War;
		if (TabItems != null)
		{
			foreach (AnimusForgeTerminalTabItemVM tab in TabItems)
			{
				tab.IsSelected = string.Equals(tab.TabId, TerminalCategories.War, StringComparison.Ordinal);
			}
		}
		if (WarStatsVm == null)
		{
			WarStatsVm = new AfWarStatsPopupVM(ExecuteBack);
		}
		else
		{
			WarStatsVm.RefreshContent();
		}
		BreadcrumbText = "Terminal / " + new TextObject("{=AF_UI_Terminal_WarBreadcrumb}War").ToString();
		SetViewMode(TerminalViewMode.WarStats);
	}

	public void ShowWeeklyReports(List<MyBehavior.WeeklyReportBrowserCountryData> countries)
	{
		_returnToView = ReturnToMenu;
		WeeklyReportVm?.OnFinalize();
		WeeklyReportVm = new TerminalWeeklyReportBrowserPopupVM(countries ?? new List<MyBehavior.WeeklyReportBrowserCountryData>(), null, ExecuteBack,
			link => AnimusForgeTerminalPopup.ActivePopup?.OpenArchiveEncyclopediaLink(link));
		BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString()
			+ " / " + LocalizeTab(_selectedTab) + " / " + new TextObject("{=AF_UI_Terminal_WeeklyBreadcrumb}Current kingdom reports").ToString();
		SetViewMode(TerminalViewMode.WeeklyReports);
	}

	public void ShowVassalageTributeHistory(TerminalTributaryPaymentHistoryData data)
	{
		_returnToView = ReturnToMenu;
		VassalageVm?.OnFinalize();
		VassalageVm = new TerminalVassalageTributeHistoryPopupVM(data ?? new TerminalTributaryPaymentHistoryData(), ExecuteBack);
		BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString()
			+ " / " + LocalizeTab(_selectedTab) + " / " + new TextObject("{=AF_UI_Terminal_VassalBreadcrumb}Vassal tribute settlement history").ToString();
		SetViewMode(TerminalViewMode.Vassalage);
	}

	internal void ShowTagCatalog(AnimusForgeTagCatalogSnapshot snapshot)
	{
		_tagCatalogSnapshot = snapshot ?? AnimusForgeTagCatalog.BuildSnapshot(forceRefresh: false);
		OnPropertyChanged(nameof(TagCatalogStatusText));
		if (_tagBrowser == null || _path.Count == 0 || !ReferenceEquals(_path.Peek(), _tagBrowser))
		{
			_tagBrowser = new AnimusForgeTerminalNode { Title = new TextObject("{=AF_UI_Terminal_TagList}Action tag list").ToString() };
			_path.Push(_tagBrowser);
			ResetMenuFilter();
		}
		_tagBrowser.Children.Clear();
		foreach (AnimusForgeTagCatalogEntry entry in _tagCatalogSnapshot.Entries)
		{
			_tagBrowser.Children.Add(new AnimusForgeTerminalNode
			{
				Id = entry.Id,
				Title = BuildTagCatalogEntryTitle(entry),
				Hint = BuildTagCatalogEntryHint(entry),
				SearchTerms = TagCatalogUiText.SearchTerms(entry),
				OnExecute = () => ShowDetails(new TextObject("{=AF_UI_Terminal_TagDetails}Tag details").ToString(), BuildTagCatalogEntryDetailText(entry))
			});
		}
		ReturnToMenu();
	}

	public void ExecuteTagCatalogInfo()
	{
		if (IsTagCatalogBrowser) ShowDetails(new TextObject("{=AF_UI_Terminal_TagInfo}Tag index information").ToString(), BuildTagCatalogSummary(_tagCatalogSnapshot));
	}

	private void ResetMenuFilter()
	{
		_searchText = "";
		_menuPage = 0;
		OnPropertyChanged(nameof(SearchText));
		OnPropertyChanged(nameof(IsSearchEmpty));
	}

	public void ShowBrowser(string title, IEnumerable<AnimusForgeTerminalNode> entries)
	{
		AnimusForgeTerminalNode browser = new AnimusForgeTerminalNode { Title = title };
		browser.Children.AddRange(entries);
		_path.Push(browser);
		ResetMenuFilter();
		ReturnToMenu();
	}

	private void ReturnToMenu()
	{
		_returnToView = null;
		SetViewMode(TerminalViewMode.MenuList);
		RefreshItems();
	}

	public void ShowDetails(string title, string text)
	{
		_returnToView = ReturnToMenu;
		DetailText = text;
		BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString()
			+ " / " + LocalizeTab(_selectedTab) + " / " + title;
		SetViewMode(TerminalViewMode.Details);
	}

	public void ShowDiagnostics(string status, string detail, bool canAnalyze)
	{
		_returnToView = ReturnToMenu;
		DiagnosticsStatusText = status ?? new TextObject("{=AF_UI_Terminal_DiagnosticsOnline2}Sentinel status: online").ToString();
		DiagnosticsDetailText = string.IsNullOrWhiteSpace(detail) ? new TextObject("{=AF_UI_Terminal_NoFatal}No fatal error stack has been recorded this session.").ToString() : detail;
		HasLatestErrorToAnalyze = canAnalyze;
		BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString() + " / " + LocalizeTab(_selectedTab) + " / " + new TextObject("{=AF_UI_Terminal_DiagnosticsTitle}System error analysis and diagnostics").ToString();
		SetViewMode(TerminalViewMode.Diagnostics);
	}

	public void ExecuteExportTagCatalog()
	{
		if (!IsTagCatalogBrowser || _tagCatalogSnapshot == null) return;
		if (AnimusForgeTagCatalog.TryExportSnapshotToModuleTxt(_tagCatalogSnapshot, out string path, out string error))
		{
			var exported = new TextObject("{=AF_UI_Terminal_ExportedTo}Exported to:\n{PATH}");
			exported.SetTextVariable("PATH", path);
			ShowDetails(new TextObject("{=AF_UI_Terminal_TagExported}Tag list exported").ToString(), exported.ToString());
		}
		else
		{
			ShowDetails(new TextObject("{=AF_UI_Terminal_TagExportFailed}Tag list export failed").ToString(), string.IsNullOrWhiteSpace(error) ? new TextObject("{=AF_UI_Terminal_UnknownError}Unknown error.").ToString() : error);
		}
	}

	public void ExecuteRefreshTagCatalog()
	{
		if (!IsTagCatalogBrowser) return;
		try
		{
			ShowTagCatalog(AnimusForgeTagCatalog.BuildSnapshot(forceRefresh: true));
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] refresh tag catalog failed: " + ex);
			ShowDetails(new TextObject("{=AF_UI_Terminal_TagRefreshFailed}Tag index refresh failed").ToString(), ex.Message);
		}
	}

	private static string BuildTagCatalogSummary(AnimusForgeTagCatalogSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return "";
		}
		int bodyCount = snapshot.Entries.Count((AnimusForgeTagCatalogEntry x) => (x.Category ?? "").StartsWith("正文", StringComparison.Ordinal));
		int postprocessCount = snapshot.Entries.Count((AnimusForgeTagCatalogEntry x) => (x.Category ?? "").StartsWith("后处理", StringComparison.Ordinal));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_TagSummaryA}Extracted from current module files, the current assembly, and built-in runtime rules.").ToString());
		var summary = new TextObject("{=AF_UI_Terminal_TagSummaryB}Total {TOTAL}; body/history {BODY}, postprocessing {POST}.");
		summary.SetTextVariable("TOTAL", snapshot.Entries.Count);
		summary.SetTextVariable("BODY", bodyCount);
		summary.SetTextVariable("POST", postprocessCount);
		stringBuilder.AppendLine(summary.ToString());
		var scanned = new TextObject("{=AF_UI_Terminal_TagSummaryC}Scanned {FILES} files. Use the search box above to filter tags, feature names, or parameters.");
		scanned.SetTextVariable("FILES", snapshot.ScannedFileCount);
		stringBuilder.AppendLine(scanned.ToString());
		if (snapshot.SourceRoots.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_SourceRoots}Source roots:").ToString());
			foreach (string root in snapshot.SourceRoots)
			{
				stringBuilder.AppendLine(root);
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildTagCatalogEntryTitle(AnimusForgeTagCatalogEntry entry)
	{
		if (entry == null)
		{
			return new TextObject("{=AF_UI_Terminal_Tag}Tag").ToString();
		}
		return "[" + TagCatalogUiText.Category(entry.Category) + "] " + TagCatalogUiText.Tag(entry.Tag);
	}

	private static string BuildTagCatalogEntryHint(AnimusForgeTagCatalogEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string description = CompactOneLine(TagCatalogUiText.Description(entry.Description));
		string source = (entry.Sources != null && entry.Sources.Count > 0) ? TagCatalogUiText.Source(entry.Sources[0]) : "";
		string text = "";
		if (!string.IsNullOrWhiteSpace(description))
		{
			text = description;
		}
		if (!string.IsNullOrWhiteSpace(source))
		{
			string sourceLabel = new TextObject("{=AF_UI_Terminal_Source}Source:").ToString();
			text = string.IsNullOrWhiteSpace(text) ? (sourceLabel + " " + source) : (text + " " + sourceLabel + " " + source);
		}
		return TruncateMenuHint(text, 220);
	}

	private static string BuildTagCatalogEntryDetailText(AnimusForgeTagCatalogEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_TagLabel}Tag:").ToString() + " " + TagCatalogUiText.Tag(entry.Tag));
		stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_CategoryLabel}Category:").ToString() + " " + TagCatalogUiText.Category(entry.Category));
		if (!string.IsNullOrWhiteSpace(entry.Description))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_DescriptionLabel}Description:").ToString());
			stringBuilder.AppendLine(TagCatalogUiText.Description(entry.Description));
		}
		if (entry.Sources != null && entry.Sources.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_SourcesLabel}Sources:").ToString());
			foreach (string source in entry.Sources)
			{
				stringBuilder.AppendLine(TagCatalogUiText.Source(source));
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string CompactOneLine(string text)
	{
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		return text;
	}

	private static string TruncateMenuHint(string text, int maxLength)
	{
		text = text ?? "";
		if (maxLength <= 0 || text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, Math.Max(0, maxLength - 1)).TrimEnd() + "…";
	}

	public void ExecuteRequestAiAnalysis()
	{
		if (!HasLatestErrorToAnalyze) return;
		_onClose?.Invoke();
		AiErrorAnalysisInquiry.AnalyzeLatestFailure();
	}

	public void ExecuteBack()
	{
		if (_currentViewMode == TerminalViewMode.WeeklyReports && WeeklyReportVm?.IsReading == true)
		{
			WeeklyReportVm.ExecuteReturnToList();
			return;
		}
		if (_returnToView != null)
		{
			Action onReturn = _returnToView;
			_returnToView = null;
			onReturn();
			return;
		}
		if (_currentViewMode != TerminalViewMode.MenuList)
		{
			SelectTab(TerminalCategories.All);
			return;
		}

		if (_path.Count > 0)
		{
			_path.Pop();
			ResetMenuFilter();
			RefreshItems();
		}
	}

	[DataSourceProperty]
	public bool IsHotkeyEnabled => AnimusForgeTerminalSettings.IsHotkeyEnabled;

	[DataSourceProperty]
	public bool IsMapIconEnabled => AnimusForgeTerminalSettings.IsMapIconEnabled;

	public void ExecuteToggleHotkey()
	{
		if (AnimusForgeTerminalSettings.IsHotkeyEnabled)
		{
			if (!AnimusForgeTerminalSettings.IsMapIconEnabled)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_KeepEntry}At least one terminal entry (key or icon) must remain.").ToString(), Colors.Red));
				return;
			}
			AnimusForgeTerminalSettings.IsHotkeyEnabled = false;
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_HotkeyOff}Terminal hotkey access disabled (U key).").ToString(), Colors.Yellow));
		}
		else
		{
			AnimusForgeTerminalSettings.IsHotkeyEnabled = true;
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_HotkeyOn}Terminal hotkey access enabled (U key).").ToString(), Colors.Green));
		}
		OnPropertyChanged(nameof(IsHotkeyEnabled));
	}

	public void ExecuteToggleMapIcon()
	{
		if (AnimusForgeTerminalSettings.IsMapIconEnabled)
		{
			if (!AnimusForgeTerminalSettings.IsHotkeyEnabled)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_KeepEntry}At least one terminal entry (key or icon) must remain.").ToString(), Colors.Red));
				return;
			}
			AnimusForgeTerminalSettings.IsMapIconEnabled = false;
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_MapIconOff}World-map terminal icon entry disabled.").ToString(), Colors.Yellow));
		}
		else
		{
			AnimusForgeTerminalSettings.IsMapIconEnabled = true;
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=AF_UI_Terminal_MapIconOn}World-map terminal icon entry enabled.").ToString(), Colors.Green));
		}
		AFWarStatsTerminal.UI.AfWarStatsMapNavigationEntry.SyncVisibility();
		OnPropertyChanged(nameof(IsMapIconEnabled));
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	public override void OnFinalize()
	{
		_warStatsVm?.OnFinalize();
		WeeklyReportVm?.OnFinalize();
		VassalageVm?.OnFinalize();
		base.OnFinalize();
	}
}

public sealed class AnimusForgeTerminalTabItemVM : ViewModel
{
	private readonly Action<string> _onSelect;
	private bool _isSelected;

	[DataSourceProperty]
	public string TabId { get; }

	[DataSourceProperty]
	public string DisplayText { get; }

	[DataSourceProperty]
	public bool IsSelected { get => _isSelected; set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); } } }

	public AnimusForgeTerminalTabItemVM(string tabId, string displayText, Action<string> onSelect)
	{
		TabId = tabId ?? "";
		DisplayText = displayText ?? TabId;
		_onSelect = onSelect;
	}

	public void ExecuteSelect() => _onSelect?.Invoke(TabId);
}

public sealed class AnimusForgeTerminalItemVM : ViewModel
{
	private readonly Action<AnimusForgeTerminalNode> _onOpenNode;
	private readonly Action _onModified;

	public AnimusForgeTerminalNode Node { get; }
	private readonly ImageIdentifier _image;
	[DataSourceProperty]
	public string ImageId => _image?.Id ?? "";
	[DataSourceProperty]
	public string ImageArgs => _image?.AdditionalArgs ?? "";
	[DataSourceProperty]
	public string ImageProvider => _image?.TextureProviderName ?? "";
	[DataSourceProperty]
	public bool HasImage => _image != null;
	[DataSourceProperty]
	public bool ShowIcon => !HasImage;
	public TerminalSettingDef SettingDef { get; }
	public TerminalItemKind ItemKind { get; }

	[DataSourceProperty]
	public bool IsHeader => ItemKind == TerminalItemKind.Header;

	[DataSourceProperty]
	public bool IsAction => ItemKind == TerminalItemKind.Action;

	[DataSourceProperty]
	public bool IsBool => ItemKind == TerminalItemKind.BoolSetting;

	[DataSourceProperty]
	public bool IsNumeric => ItemKind == TerminalItemKind.NumericSetting;

	[DataSourceProperty]
	public bool IsDropdown => ItemKind == TerminalItemKind.DropdownSetting;

	[DataSourceProperty]
	public bool IsText => ItemKind == TerminalItemKind.TextSetting;

	[DataSourceProperty]
	public bool IsButton => ItemKind == TerminalItemKind.ButtonSetting;

	[DataSourceProperty]
	public bool IsHotkey => ItemKind == TerminalItemKind.HotkeySetting;

	private bool _isListeningKey;
	[DataSourceProperty]
	public bool IsListeningKey
	{
		get => _isListeningKey;
		set
		{
			if (value != _isListeningKey)
			{
				_isListeningKey = value;
				OnPropertyChangedWithValue(value, nameof(IsListeningKey));
			}
		}
	}

	private bool _isUpdatingFromSettings;
	private SelectorVM<SelectorItemVM> _dropdownSelector;
	private readonly Dictionary<string, string> _dropdownDisplayLabels = new Dictionary<string, string>(StringComparer.Ordinal);

	private string DropdownDisplayLabel(string raw)
	{
		if (string.IsNullOrEmpty(raw)) return raw ?? "";
		if (!raw.StartsWith("{=", StringComparison.Ordinal) && raw != "*手动填写*") return raw;
		if (_dropdownDisplayLabels.TryGetValue(raw, out var label)) return label;
		// Resolve once per option in this UI instance, never in the model/configuration.
		label = raw == "*手动填写*"
			? new TextObject("{=AF_MCM_Option_ModelManual}*manual entry*").ToString()
			: raw.StartsWith("{=", StringComparison.Ordinal) ? new TextObject(raw).ToString() : raw;
		_dropdownDisplayLabels[raw] = label;
		return label;
	}
	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> DropdownSelector
	{
		get => _dropdownSelector;
		set
		{
			if (value != _dropdownSelector)
			{
				_dropdownSelector = value;
				OnPropertyChangedWithValue(value, nameof(DropdownSelector));
			}
		}
	}

	[DataSourceProperty]
	public string Id { get; } = "";

	[DataSourceProperty]
	public string Title { get; } = "";

	[DataSourceProperty]
	public string CodeName { get; } = "";

	[DataSourceProperty]
	public string HintText { get; } = "";

	[DataSourceProperty]
	public string IconText { get; } = "❖";

	[DataSourceProperty]
	public bool IsBranch => Node?.IsBranch == true;

	[DataSourceProperty]
	public string BadgeText { get; set; } = "";

	[DataSourceProperty]
	public string ButtonText { get; set; } = "";

	[DataSourceProperty]
	public string DisplayValue { get; set; } = "";

	[DataSourceProperty]
	public string TextValue
	{
		get
		{
			if (SettingDef == null) return "";
			DuelSettings settings = DuelSettings.GetSettings();
			return settings != null ? (SettingDef.Getter(settings)?.ToString() ?? "") : "";
		}
		set
		{
			if (SettingDef != null && value != null)
			{
				DuelSettings s = DuelSettings.GetSettings();
				if (s != null)
				{
					SettingDef.Setter(s, value);
					DisplayValue = string.IsNullOrWhiteSpace(value) ? new TextObject("{=AF_UI_Terminal_EditValue}Click to enter ✎").ToString() : (value.Length > 24 ? value.Substring(0, 24) + "..." : value);
					OnPropertyChangedWithValue(value, nameof(TextValue));
					OnPropertyChanged(nameof(DisplayValue));
					_onModified?.Invoke();
				}
			}
		}
	}

	[DataSourceProperty]
	public bool BoolValue { get; set; }

	[DataSourceProperty]
	public string BoolStatusText { get; set; } = "";

	[DataSourceProperty]
	public bool IsBoolTrue => BoolValue;

	private readonly Action<string> _onToggleCollapse;

	[DataSourceProperty]
	public bool IsCollapsible { get; }

	[DataSourceProperty]
	public bool IsCollapsed { get; }

	public string GroupKey { get; } = "";

	public void ExecuteToggleCollapse()
	{
		if (IsCollapsible && !string.IsNullOrEmpty(GroupKey))
		{
			_onToggleCollapse?.Invoke(GroupKey);
		}
	}

	// 构造：分组标题 Header
	public static AnimusForgeTerminalItemVM CreateCollapsibleHeader(string title, string badge, string groupKey, bool isCollapsed, Action<string> onToggle)
	{
		return new AnimusForgeTerminalItemVM(title, badge, groupKey, isCollapsed, onToggle);
	}

	private AnimusForgeTerminalItemVM(string title, string badge, string groupKey, bool isCollapsed, Action<string> onToggle)
	{
		ItemKind = TerminalItemKind.Header;
		Title = title ?? "";
		BadgeText = badge ?? "";
		IsCollapsible = true;
		IsCollapsed = isCollapsed;
		GroupKey = groupKey ?? "";
		_onToggleCollapse = onToggle;
	}

	// 构造：工具与查询条目
	public AnimusForgeTerminalItemVM(AnimusForgeTerminalNode node, Action<AnimusForgeTerminalNode> onOpen)
	{
		Node = node;
		_onOpenNode = onOpen;
		ItemKind = TerminalItemKind.Action;
		Id = node?.Id ?? "";
		Title = node?.Title ?? "";
		CodeName = "";
		HintText = string.IsNullOrWhiteSpace(node?.Hint) ? new TextObject("{=AF_UI_Terminal_NoHint}No additional description.").ToString() : node.Hint;
		IconText = string.IsNullOrWhiteSpace(node?.Icon) ? "❖" : node.Icon;
		BadgeText = node?.IsBranch == true ? new TextObject("{=AF_UI_Terminal_Branch}Panel").ToString() : new TextObject("{=AF_UI_Terminal_Action}Action").ToString();
		ButtonText = node?.IsBranch == true ? new TextObject("{=AF_UI_Terminal_Enter}Enter ➔").ToString() : node?.OnExecute != null ? new TextObject("{=AF_UI_Terminal_View}View").ToString() : new TextObject("{=AF_UI_Terminal_Execute}Execute ⚡").ToString();
		_image = node?.ImageFactory?.Invoke();
	}

	// 构造：MCM 设置项
	public AnimusForgeTerminalItemVM(TerminalSettingDef def, Action onModified)
	{
		SettingDef = def;
		_onModified = onModified;
		Id = def?.Id ?? "";
		Title = def?.DisplayName ?? "";
		CodeName = def?.Id ?? "";
		HintText = string.IsNullOrWhiteSpace(def?.HintText) ? new TextObject("{=AF_UI_Terminal_SettingHint}Adjust this mechanism setting or switch as needed.").ToString() : def.HintText;
		IconText = "⚙";

		DuelSettings settings = DuelSettings.GetSettings();
		if (def == null || settings == null)
		{
			ItemKind = TerminalItemKind.Header;
			return;
		}

		switch (def.SettingType)
		{
			case TerminalSettingType.Bool:
				ItemKind = TerminalItemKind.BoolSetting;
				BadgeText = new TextObject("{=AF_UI_Terminal_Toggle}Toggle").ToString();
				BoolValue = Convert.ToBoolean(def.Getter(settings));
				BoolStatusText = BoolValue ? new TextObject("{=AF_UI_Terminal_Enabled}✔ Enabled").ToString() : new TextObject("{=AF_UI_Terminal_Disabled}✕ Disabled").ToString();
				break;

			case TerminalSettingType.Integer:
			case TerminalSettingType.FloatingInteger:
				ItemKind = TerminalItemKind.NumericSetting;
				BadgeText = new TextObject("{=AF_UI_Terminal_Parameter}Parameter").ToString();
				object numVal = def.Getter(settings);
				DisplayValue = FormatNumeric(numVal, def.FormatString, def.Id);
				break;

			case TerminalSettingType.Dropdown:
				ItemKind = TerminalItemKind.DropdownSetting;
				BadgeText = new TextObject("{=AF_UI_Terminal_Dropdown}Dropdown").ToString();
				DisplayValue = DropdownDisplayLabel(def.Getter(settings)?.ToString());
				var dropdown = def.DropdownGetter?.Invoke(settings);
				if (dropdown != null && dropdown.Count > 0)
				{
					var options = new List<string>();
					for (int i = 0; i < dropdown.Count; i++)
					{
						options.Add(DropdownDisplayLabel(dropdown[i]));
					}
					_dropdownSelector = new SelectorVM<SelectorItemVM>(options, dropdown.SelectedIndex, OnDropdownItemChanged);
				}
				break;

			case TerminalSettingType.Hotkey:
				ItemKind = TerminalItemKind.HotkeySetting;
				BadgeText = new TextObject("{=AF_UI_Terminal_Hotkey}Key").ToString();
				string keyStr = def.Getter(settings)?.ToString() ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(keyStr) ? new TextObject("{=AF_UI_Terminal_Unset}Not set").ToString() : keyStr.ToUpperInvariant();
				break;

			case TerminalSettingType.Text:
				ItemKind = TerminalItemKind.TextSetting;
				BadgeText = new TextObject("{=AF_UI_Terminal_Text}Text").ToString();
				string raw = def.Getter(settings)?.ToString() ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(raw) ? new TextObject("{=AF_UI_Terminal_EditValue}Click to enter ✎").ToString() : (raw.Length > 24 ? raw.Substring(0, 24) + "..." : raw);
				break;

			case TerminalSettingType.Button:
				ItemKind = TerminalItemKind.ButtonSetting;
				BadgeText = new TextObject("{=AF_UI_Terminal_ExecuteBadge}Execute").ToString();
				ButtonText = new TextObject("{=AF_UI_Terminal_Execute}Execute ⚡").ToString();
				break;
		}
	}

	private void OnDropdownItemChanged(SelectorVM<SelectorItemVM> selector)
	{
		if (_isUpdatingFromSettings || SettingDef == null || selector == null || selector.SelectedIndex < 0) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;
		var current = SettingDef.DropdownGetter?.Invoke(settings);
		if (current == null || current.SelectedIndex == selector.SelectedIndex) return;
		SettingDef.Setter(settings, selector.SelectedIndex);
		DisplayValue = DropdownDisplayLabel(SettingDef.Getter(settings)?.ToString());
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
		AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.RefreshCurrentItemsFromSettings();
	}

	public void ExecuteRebindHotkey()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.HotkeySetting) return;
		AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.StartListeningKey(this);
	}

	private static string FormatNumeric(object val, string fmt, string propId)
	{
		if (val == null) return "0";
		float f = Convert.ToSingle(val);
		string unit = "";
		if (propId.IndexOf("Percent", StringComparison.OrdinalIgnoreCase) >= 0 || (fmt != null && fmt.Contains("%")))
		{
			if (f <= 1.01f && f >= -1.01f && ((fmt != null && fmt.Contains("%")) || (f != 0f && f < 1f)))
			{
				return $"{(int)Math.Round(f * 100)} %";
			}
			return $"{(int)Math.Round(f)} %";
		}
		if (propId.IndexOf("Seconds", StringComparison.OrdinalIgnoreCase) >= 0 || propId.IndexOf("Second", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			unit = new TextObject("{=AF_UI_Terminal_UnitSeconds} sec").ToString();
		}
		else if (propId.IndexOf("Days", StringComparison.OrdinalIgnoreCase) >= 0 || propId.IndexOf("Day", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			unit = new TextObject("{=AF_UI_Terminal_UnitDays} days").ToString();
		}
		else if (propId.IndexOf("Hours", StringComparison.OrdinalIgnoreCase) >= 0 || propId.IndexOf("Hour", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			unit = new TextObject("{=AF_UI_Terminal_UnitHours} hours").ToString();
		}
		else if (propId.IndexOf("Multiplier", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return $"{f:F2}x";
		}
		else if (propId.IndexOf("Tokens", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return $"{(int)f} Tokens";
		}
		else if (propId.IndexOf("Megabytes", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return $"{(int)f} MB";
		}
		else if (propId.IndexOf("Cost", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return $"{(int)f}{new TextObject("{=AF_UI_Terminal_UnitDenars} denars").ToString()}";
		}

		if (fmt == "0.00" || fmt == "0.0")
		{
			return f.ToString(fmt) + unit;
		}
		if (Math.Abs(f - Math.Round(f)) < 0.001)
		{
			return ((int)Math.Round(f)).ToString() + unit;
		}
		return f.ToString("0.##") + unit;
	}

	public void ExecuteOpen()
	{
		if (Node != null)
		{
			_onOpenNode?.Invoke(Node);
		}
		else if (ItemKind == TerminalItemKind.ButtonSetting)
		{
			ExecuteButtonClick();
		}
		else if (ItemKind == TerminalItemKind.BoolSetting)
		{
			ExecuteToggleBool();
		}
		else if (ItemKind == TerminalItemKind.NumericSetting)
		{
			ExecuteEditNumeric();
		}
		else if (ItemKind == TerminalItemKind.DropdownSetting)
		{
			ExecuteCycleDropdown();
		}
		else if (ItemKind == TerminalItemKind.TextSetting)
		{
			ExecuteEditText();
		}
		else if (ItemKind == TerminalItemKind.HotkeySetting)
		{
			ExecuteRebindHotkey();
		}
	}

	public void ExecuteToggleBool()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.BoolSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;
		bool next = !BoolValue;
		SettingDef.Setter(settings, next);
		BoolValue = next;
		BoolStatusText = next ? new TextObject("{=AF_UI_Terminal_Enabled}✔ Enabled").ToString() : new TextObject("{=AF_UI_Terminal_Disabled}✕ Disabled").ToString();
		OnPropertyChanged(nameof(BoolValue));
		OnPropertyChanged(nameof(BoolStatusText));
		OnPropertyChanged(nameof(IsBoolTrue));
		_onModified?.Invoke();
	}

	public void ExecuteDecrease()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.NumericSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		if (SettingDef.SettingType == TerminalSettingType.Integer)
		{
			int cur = Convert.ToInt32(SettingDef.Getter(settings));
			int step = (int)Math.Max(1f, SettingDef.StepValue);
			int next = Math.Max((int)SettingDef.MinValue, cur - step);
			SettingDef.Setter(settings, next);
			DisplayValue = FormatNumeric(next, SettingDef.FormatString, SettingDef.Id);
		}
		else
		{
			float cur = Convert.ToSingle(SettingDef.Getter(settings));
			float step = SettingDef.StepValue > 0 ? SettingDef.StepValue : 0.05f;
			float next = (float)Math.Round(Math.Max(SettingDef.MinValue, cur - step), 2);
			SettingDef.Setter(settings, next);
			DisplayValue = FormatNumeric(next, SettingDef.FormatString, SettingDef.Id);
		}
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
	}

	public void ExecuteIncrease()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.NumericSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		if (SettingDef.SettingType == TerminalSettingType.Integer)
		{
			int cur = Convert.ToInt32(SettingDef.Getter(settings));
			int step = (int)Math.Max(1f, SettingDef.StepValue);
			int next = Math.Min((int)SettingDef.MaxValue, cur + step);
			SettingDef.Setter(settings, next);
			DisplayValue = FormatNumeric(next, SettingDef.FormatString, SettingDef.Id);
		}
		else
		{
			float cur = Convert.ToSingle(SettingDef.Getter(settings));
			float step = SettingDef.StepValue > 0 ? SettingDef.StepValue : 0.05f;
			float next = (float)Math.Round(Math.Min(SettingDef.MaxValue, cur + step), 2);
			SettingDef.Setter(settings, next);
			DisplayValue = FormatNumeric(next, SettingDef.FormatString, SettingDef.Id);
		}
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
	}

	public void ExecuteEditNumeric()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.NumericSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		object cur = SettingDef.Getter(settings);
		var titleText = new TextObject("{=AF_UI_Terminal_NumericTitle}Set {TITLE}");
		titleText.SetTextVariable("TITLE", Title ?? "");
		var promptText = new TextObject("{=AF_UI_Terminal_NumericPrompt}Enter a value (valid range: {MIN} ~ {MAX}):\n{HINT}");
		promptText.SetTextVariable("MIN", SettingDef.MinValue);
		promptText.SetTextVariable("MAX", SettingDef.MaxValue);
		promptText.SetTextVariable("HINT", HintText ?? "");
		InformationManager.ShowTextInquiry(new TextInquiryData(
			titleText.ToString(),
			promptText.ToString(),
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			new TextObject("{=AF_UI_Terminal_Confirm}Confirm").ToString(),
			new TextObject("{=AF_UI_Terminal_Cancel}Cancel").ToString(),
			input =>
			{
				if (float.TryParse(input, out float parsed))
				{
					if (SettingDef.SettingType == TerminalSettingType.Integer)
					{
						int clamped = (int)Math.Max(SettingDef.MinValue, Math.Min(SettingDef.MaxValue, Math.Round(parsed)));
						SettingDef.Setter(settings, clamped);
						DisplayValue = FormatNumeric(clamped, SettingDef.FormatString, SettingDef.Id);
					}
					else
					{
						float clamped = (float)Math.Round(Math.Max(SettingDef.MinValue, Math.Min(SettingDef.MaxValue, parsed)), 2);
						SettingDef.Setter(settings, clamped);
						DisplayValue = FormatNumeric(clamped, SettingDef.FormatString, SettingDef.Id);
					}
					OnPropertyChanged(nameof(DisplayValue));
					_onModified?.Invoke();
				}
			},
			null,
			shouldInputBeObfuscated: false,
			null,
			"",
			cur?.ToString() ?? ""
		));
	}

	public void ExecuteCycleDropdownPrevious()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.DropdownSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;
		var dropdown = SettingDef.DropdownGetter?.Invoke(settings);
		if (dropdown == null || dropdown.Count <= 0) return;
		int nextIdx = dropdown.SelectedIndex - 1;
		if (nextIdx < 0) nextIdx = dropdown.Count - 1;
		SettingDef.Setter(settings, nextIdx);
		DisplayValue = DropdownDisplayLabel(SettingDef.Getter(settings)?.ToString());
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
		AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.RefreshCurrentItemsFromSettings();
	}

	public void ExecuteCycleDropdownNext()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.DropdownSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;
		var dropdown = SettingDef.DropdownGetter?.Invoke(settings);
		if (dropdown == null || dropdown.Count <= 0) return;
		int nextIdx = (dropdown.SelectedIndex + 1) % dropdown.Count;
		SettingDef.Setter(settings, nextIdx);
		DisplayValue = DropdownDisplayLabel(SettingDef.Getter(settings)?.ToString());
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
		AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.RefreshCurrentItemsFromSettings();
	}

	public void ExecuteCycleDropdown()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.DropdownSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		var dropdown = SettingDef.DropdownGetter?.Invoke(settings);
		if (dropdown == null || dropdown.Count <= 0) return;

		List<InquiryElement> elements = new List<InquiryElement>();
		for (int i = 0; i < dropdown.Count; i++)
		{
			string opt = DropdownDisplayLabel(dropdown[i]);
			elements.Add(new InquiryElement(i.ToString(), opt, null, isEnabled: true, ""));
		}

		var titleText = new TextObject("{=AF_UI_Terminal_DropdownTitle}Select {TITLE}");
		titleText.SetTextVariable("TITLE", Title ?? "");
		var promptText = new TextObject("{=AF_UI_Terminal_DropdownPrompt}Choose from the available options:\n{HINT}");
		promptText.SetTextVariable("HINT", HintText ?? "");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			titleText.ToString(),
			promptText.ToString(),
			elements,
			isExitShown: true,
			1,
			1,
			new TextObject("{=AF_UI_Terminal_Confirm}Confirm").ToString(),
			new TextObject("{=AF_UI_Terminal_Back}Back").ToString(),
			selected =>
			{
				if (selected != null && selected.Count > 0 && int.TryParse(selected[0].Identifier as string, out int idx))
				{
					SettingDef.Setter(settings, idx);
					DisplayValue = DropdownDisplayLabel(SettingDef.Getter(settings)?.ToString());
					OnPropertyChanged(nameof(DisplayValue));
					_onModified?.Invoke();
					AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.RefreshCurrentItemsFromSettings();
				}
			},
			null,
			"",
			isSeachAvailable: true
		);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	public void ExecuteEditText()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.TextSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		string cur = SettingDef.Getter(settings)?.ToString() ?? "";
		var titleText = new TextObject("{=AF_UI_Terminal_TextTitle}Edit {TITLE}");
		titleText.SetTextVariable("TITLE", Title ?? "");
		InformationManager.ShowTextInquiry(new TextInquiryData(
			titleText.ToString(),
			$"{HintText}",
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			new TextObject("{=AF_UI_Terminal_Confirm}Confirm").ToString(),
			new TextObject("{=AF_UI_Terminal_Cancel}Cancel").ToString(),
			input =>
			{
				SettingDef.Setter(settings, input ?? "");
				string raw = input ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(raw) ? new TextObject("{=AF_UI_Terminal_EditValue}Click to enter ✎").ToString() : (raw.Length > 24 ? raw.Substring(0, 24) + "..." : raw);
				OnPropertyChanged(nameof(DisplayValue));
				OnPropertyChanged(nameof(TextValue));
				_onModified?.Invoke();
			},
			null,
			shouldInputBeObfuscated: false,
			null,
			"",
			cur
		));
	}

	public void ExecuteButtonClick()
	{
		if (SettingDef == null || ItemKind != TerminalItemKind.ButtonSetting) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;

		try
		{
			SettingDef.Invoker?.Invoke(settings);
			var triggeredMessage = new TextObject("{=AF_UI_Terminal_SettingTriggered}Triggered: {TITLE}");
			triggeredMessage.SetTextVariable("TITLE", Title ?? "");
			InformationManager.DisplayMessage(new InformationMessage(triggeredMessage.ToString(), Colors.Green));
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", $"[ERROR] Failed to invoke setting action {SettingDef.Id}: {ex}");
			var triggerFailedMessage = new TextObject("{=AF_UI_Terminal_SettingTriggerFailed}Trigger failed: {ERROR}");
			triggerFailedMessage.SetTextVariable("ERROR", ex.Message ?? "Unknown error.");
			InformationManager.DisplayMessage(new InformationMessage(triggerFailedMessage.ToString(), Colors.Red));
		}
	}

	public void UpdateFromSettings(DuelSettings settings)
	{
		if (SettingDef == null || settings == null || _isUpdatingFromSettings) return;
		try
		{
			_isUpdatingFromSettings = true;
			switch (ItemKind)
			{
				case TerminalItemKind.BoolSetting:
					BoolValue = Convert.ToBoolean(SettingDef.Getter(settings));
					BoolStatusText = BoolValue ? new TextObject("{=AF_UI_Terminal_Enabled}✔ Enabled").ToString() : new TextObject("{=AF_UI_Terminal_Disabled}✕ Disabled").ToString();
					OnPropertyChanged(nameof(BoolValue));
					OnPropertyChanged(nameof(BoolStatusText));
					OnPropertyChanged(nameof(IsBoolTrue));
					break;
				case TerminalItemKind.NumericSetting:
					DisplayValue = FormatNumeric(SettingDef.Getter(settings), SettingDef.FormatString, SettingDef.Id);
					OnPropertyChanged(nameof(DisplayValue));
					break;
				case TerminalItemKind.DropdownSetting:
					DisplayValue = DropdownDisplayLabel(SettingDef.Getter(settings)?.ToString());
					OnPropertyChanged(nameof(DisplayValue));
					var dropdown = SettingDef.DropdownGetter?.Invoke(settings);
					if (dropdown != null && dropdown.Count > 0)
					{
						bool needRebuild = (_dropdownSelector == null || _dropdownSelector.ItemList == null || _dropdownSelector.ItemList.Count != dropdown.Count);
						if (!needRebuild)
						{
							for (int i = 0; i < dropdown.Count; i++)
							{
								if (_dropdownSelector.ItemList[i]?.StringItem != DropdownDisplayLabel(dropdown[i]))
								{
									needRebuild = true;
									break;
								}
							}
						}

						if (needRebuild)
						{
							var options = new List<string>(dropdown.Count);
							for (int i = 0; i < dropdown.Count; i++)
							{
								options.Add(DropdownDisplayLabel(dropdown[i]));
							}
							int selectedIdx = dropdown.SelectedIndex;
							if (selectedIdx < 0 || selectedIdx >= options.Count)
							{
								selectedIdx = 0;
							}
							DropdownSelector = new SelectorVM<SelectorItemVM>(options, selectedIdx, OnDropdownItemChanged);
						}
						else if (dropdown.SelectedIndex != _dropdownSelector.SelectedIndex && dropdown.SelectedIndex >= 0 && dropdown.SelectedIndex < _dropdownSelector.ItemList.Count)
						{
							_dropdownSelector.SelectedIndex = dropdown.SelectedIndex;
						}
					}
					break;
				case TerminalItemKind.HotkeySetting:
					if (!_isListeningKey)
					{
						string rawKey = SettingDef.Getter(settings)?.ToString() ?? "";
						DisplayValue = string.IsNullOrWhiteSpace(rawKey) ? new TextObject("{=AF_UI_Terminal_Unset}Not set").ToString() : rawKey.ToUpperInvariant();
						OnPropertyChanged(nameof(DisplayValue));
					}
					break;
				case TerminalItemKind.TextSetting:
					string raw = SettingDef.Getter(settings)?.ToString() ?? "";
					DisplayValue = string.IsNullOrWhiteSpace(raw) ? new TextObject("{=AF_UI_Terminal_EditValue}Click to enter ✎").ToString() : (raw.Length > 24 ? raw.Substring(0, 24) + "..." : raw);
					OnPropertyChanged(nameof(DisplayValue));
					OnPropertyChanged(nameof(TextValue));
					break;
			}
		}
		finally
		{
			_isUpdatingFromSettings = false;
		}
	}
}
