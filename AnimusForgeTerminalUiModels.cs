using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AFWarStatsTerminal.UI;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeTerminalNode
{
	public string Id { get; set; } = "";
	public string Title { get; set; } = "";
	public string Hint { get; set; } = "";
	public string SearchTerms { get; set; } = "";
	public string Category { get; set; } = "全部";
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

public sealed class AnimusForgeTerminalPopup
{
	private static AnimusForgeTerminalPopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly AnimusForgeTerminalPopupVM _dataSource;
	private readonly Action _onClose;
	private bool _isClosed;

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

	public static bool ShowWarStats()
	{
		// The map entry remains usable when the terminal behavior is unavailable.
		// It uses the same view model/movie instead of a second, divergent war popup.
		try
		{
			var roots = new List<AnimusForgeTerminalNode>
			{
				new AnimusForgeTerminalNode { Id = "war_stats", Title = "战争统计", Category = "战争" }
			};
			if (!Show(roots, id =>
			{
				if (id != "war_stats") return false;
				_activePopup?.ViewModel.ShowWarStats();
				return true;
			})) return false;
			_activePopup.ViewModel.ShowWarStats();
			return true;
		}
		catch (Exception ex)
		{
			CloseActive();
			Logger.Log("Terminal", "[ERROR] failed to open war-only terminal: " + ex);
			return false;
		}
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
		popup._dataSource.WeeklyReportVm?.Tick();
		if (ScreenManager.TopScreen != popup._screen
			|| (ScreenManager.FocusedLayer == popup._layer && popup._layer.Input.IsHotKeyReleased("Exit")))
		{
			popup.HandleCloseRequested();
		}
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

	private string _titleText = "AnimusForge 终端";
	private string _subtitleText = "选择工具直接执行，或进入二级功能面板。按 ESC 或关闭退出。";
	private string _breadcrumbText = "终端 / 全部";
	private string _selectedTab = "全部";
	private bool _canGoBack;

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
	private string _diagnosticsStatusText = "捕获哨兵状态: 正常在线";
	private string _diagnosticsDetailText = "暂无待分析的最近异常。";
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
	public float MenuContentTop => IsTagCatalogBrowser ? 98f : 50f;
	[DataSourceProperty]
	public string TagCatalogStatusText => _tagCatalogSnapshot == null ? "" : $"索引：{_tagCatalogSnapshot.Entries.Count} 项 · {_tagCatalogSnapshot.BuiltUtc.ToLocalTime():HH:mm:ss} 更新；可搜索参数/来源";

	[DataSourceProperty]
	public bool IsDetailsVisible => _currentViewMode == TerminalViewMode.Details;

	[DataSourceProperty]
	public bool IsDiagnosticsVisible => _currentViewMode == TerminalViewMode.Diagnostics;

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
			RefreshItems();
		}
	}

	[DataSourceProperty]
	public string MenuPageText => $"{_menuPage + 1}/{Math.Max(1, (_filteredCount + MenuPageSize - 1) / MenuPageSize)} 页 · {_filteredCount} 项";
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

	public AnimusForgeTerminalPopupVM(List<AnimusForgeTerminalNode> roots, Func<string, bool> onExecuteLeaf, Action onClose)
	{
		_onExecuteLeaf = onExecuteLeaf;
		_onClose = onClose;
		_roots = (roots ?? new List<AnimusForgeTerminalNode>()).Where(x => x != null).ToList();
		TabItems = new MBBindingList<AnimusForgeTerminalTabItemVM>();
		Items = new MBBindingList<AnimusForgeTerminalItemVM>();

		string[] tabs = new[] { "战争", "全部", "外交", "部队", "玩家", "查询与记录", "系统" };
		foreach (string tab in tabs)
		{
			TabItems.Add(new AnimusForgeTerminalTabItemVM(tab, SelectTab));
		}
		RefreshItems();
	}

	public void SelectTab(string tab)
	{
		_selectedTab = string.IsNullOrWhiteSpace(tab) ? "全部" : tab;
		_path.Clear();
		_tagBrowser = null;
		_tagCatalogSnapshot = null;
		_returnToView = null;
		ResetMenuFilter();
		if (string.Equals(_selectedTab, "战争", StringComparison.Ordinal))
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
		OnPropertyChanged(nameof(MenuContentTop));
		OnPropertyChanged(nameof(IsDetailsVisible));
		OnPropertyChanged(nameof(IsDiagnosticsVisible));
		UpdateCanGoBack();
	}

	private void UpdateCanGoBack()
	{
		CanGoBack = _currentViewMode != TerminalViewMode.MenuList || _path.Count > 0;
	}

	private void RefreshItems()
	{
		foreach (AnimusForgeTerminalTabItemVM tab in TabItems)
		{
			tab.IsSelected = string.Equals(tab.TabId, _selectedTab, StringComparison.Ordinal);
		}
		IEnumerable<AnimusForgeTerminalNode> source = _path.Count > 0
			? _path.Peek().Children
			: (_selectedTab == "全部"
				? _roots
				: _roots.Where(x => string.Equals(x.Category, _selectedTab, StringComparison.Ordinal)));

		string query = _searchText.Trim();
		List<AnimusForgeTerminalNode> filtered = source.Where(node => query.Length == 0
			|| (node.Title ?? "").IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0
			|| (node.Hint ?? "").IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0
			|| (node.SearchTerms ?? "").IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
		_filteredCount = filtered.Count;
		_menuPage = Math.Min(_menuPage, Math.Max(0, (_filteredCount - 1) / MenuPageSize));
		MBBindingList<AnimusForgeTerminalItemVM> list = new MBBindingList<AnimusForgeTerminalItemVM>();
		foreach (AnimusForgeTerminalNode node in filtered.Skip(_menuPage * MenuPageSize).Take(MenuPageSize))
		{
			list.Add(new AnimusForgeTerminalItemVM(node, OpenNode));
		}
		OnPropertyChanged(nameof(IsTagCatalogBrowser));
		OnPropertyChanged(nameof(MenuContentTop));
		OnPropertyChanged(nameof(MenuPageText));
		OnPropertyChanged(nameof(HasPreviousMenuPage));
		OnPropertyChanged(nameof(HasNextMenuPage));
		Items = list;
		UpdateBreadcrumb();
		UpdateCanGoBack();
	}

	private void UpdateBreadcrumb()
	{
		if (_currentViewMode != TerminalViewMode.MenuList)
		{
			return;
		}
		if (_path.Count > 0)
		{
			BreadcrumbText = "终端 / " + _selectedTab + " / " + string.Join(" / ", _path.Reverse().Select(x => x.Title));
		}
		else
		{
			BreadcrumbText = "终端 / " + _selectedTab;
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
		_selectedTab = "战争";
		if (TabItems != null)
		{
			foreach (AnimusForgeTerminalTabItemVM tab in TabItems)
			{
				tab.IsSelected = string.Equals(tab.TabId, "战争", StringComparison.Ordinal);
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
		BreadcrumbText = "终端 / 战争";
		SetViewMode(TerminalViewMode.WarStats);
	}

	public void ShowWeeklyReports(List<MyBehavior.WeeklyReportBrowserCountryData> countries)
	{
		_returnToView = ReturnToMenu;
		WeeklyReportVm?.OnFinalize();
		WeeklyReportVm = new TerminalWeeklyReportBrowserPopupVM(countries ?? new List<MyBehavior.WeeklyReportBrowserCountryData>(), null, ExecuteBack);
		BreadcrumbText = "终端 / " + _selectedTab + " / 查看周报";
		SetViewMode(TerminalViewMode.WeeklyReports);
	}

	public void ShowVassalageTributeHistory(TerminalTributaryPaymentHistoryData data)
	{
		_returnToView = ReturnToMenu;
		VassalageVm?.OnFinalize();
		VassalageVm = new TerminalVassalageTributeHistoryPopupVM(data ?? new TerminalTributaryPaymentHistoryData(), ExecuteBack);
		BreadcrumbText = "终端 / " + _selectedTab + " / 臣属贡金结算记录";
		SetViewMode(TerminalViewMode.Vassalage);
	}

	internal void ShowTagCatalog(AnimusForgeTagCatalogSnapshot snapshot)
	{
		_tagCatalogSnapshot = snapshot ?? AnimusForgeTagCatalog.BuildSnapshot(forceRefresh: false);
		OnPropertyChanged(nameof(TagCatalogStatusText));
		if (_tagBrowser == null || _path.Count == 0 || !ReferenceEquals(_path.Peek(), _tagBrowser))
		{
			_tagBrowser = new AnimusForgeTerminalNode { Title = "指令标签列表" };
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
				SearchTerms = (entry.Description ?? "") + " " + string.Join(" ", entry.Sources),
				OnExecute = () => ShowDetails("标签详情", BuildTagCatalogEntryDetailText(entry))
			});
		}
		ReturnToMenu();
	}

	public void ExecuteTagCatalogInfo()
	{
		if (IsTagCatalogBrowser) ShowDetails("标签索引说明", BuildTagCatalogSummary(_tagCatalogSnapshot));
	}

	private void ResetMenuFilter()
	{
		_searchText = "";
		_menuPage = 0;
		OnPropertyChanged(nameof(SearchText));
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
		BreadcrumbText = "终端 / " + _selectedTab + " / " + title;
		SetViewMode(TerminalViewMode.Details);
	}

	public void ShowDiagnostics(string status, string detail, bool canAnalyze)
	{
		_returnToView = ReturnToMenu;
		DiagnosticsStatusText = status ?? "捕获哨兵状态: 在线";
		DiagnosticsDetailText = string.IsNullOrWhiteSpace(detail) ? "本局当前未记录到任何致命报错堆栈。" : detail;
		HasLatestErrorToAnalyze = canAnalyze;
		BreadcrumbText = "终端 / " + _selectedTab + " / 系统错误分析与诊断";
		SetViewMode(TerminalViewMode.Diagnostics);
	}

	public void ExecuteExportTagCatalog()
	{
		if (!IsTagCatalogBrowser || _tagCatalogSnapshot == null) return;
		if (AnimusForgeTagCatalog.TryExportSnapshotToModuleTxt(_tagCatalogSnapshot, out string path, out string error))
		{
			ShowDetails("标签列表已导出", "已导出到：\n" + path);
		}
		else
		{
			ShowDetails("标签列表导出失败", string.IsNullOrWhiteSpace(error) ? "未知错误。" : error);
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
			ShowDetails("刷新标签索引失败", ex.Message);
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
		stringBuilder.AppendLine("从当前 AnimusForge 模块文件、当前程序集和内置运行时规则提取。");
		stringBuilder.AppendLine("共 " + snapshot.Entries.Count + " 项；正文/历史 " + bodyCount + " 项，后处理 " + postprocessCount + " 项。");
		stringBuilder.AppendLine("已扫描文件：" + snapshot.ScannedFileCount + " 个。可用上方搜索框按标签、功能名或参数名筛选。");
		if (snapshot.SourceRoots.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("来源根目录：");
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
			return "标签";
		}
		return "[" + (entry.Category ?? "标签") + "] " + (entry.Tag ?? "");
	}

	private static string BuildTagCatalogEntryHint(AnimusForgeTagCatalogEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string description = CompactOneLine(entry.Description);
		string source = (entry.Sources != null && entry.Sources.Count > 0) ? entry.Sources[0] : "";
		string text = "";
		if (!string.IsNullOrWhiteSpace(description))
		{
			text = description;
		}
		if (!string.IsNullOrWhiteSpace(source))
		{
			text = string.IsNullOrWhiteSpace(text) ? ("来源：" + source) : (text + " 来源：" + source);
		}
		return TruncateMenuHint(text, 220);
	}

	private static string BuildTagCatalogEntryDetailText(AnimusForgeTagCatalogEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("标签：" + (entry.Tag ?? ""));
		stringBuilder.AppendLine("分类：" + (entry.Category ?? "标签"));
		if (!string.IsNullOrWhiteSpace(entry.Description))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("说明：");
			stringBuilder.AppendLine(entry.Description.Trim());
		}
		if (entry.Sources != null && entry.Sources.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("来源：");
			foreach (string source in entry.Sources)
			{
				stringBuilder.AppendLine(source);
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
		if (_returnToView != null)
		{
			Action onReturn = _returnToView;
			_returnToView = null;
			onReturn();
			return;
		}
		if (_currentViewMode != TerminalViewMode.MenuList)
		{
			SelectTab("全部");
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
				InformationManager.DisplayMessage(new InformationMessage("至少需要保留一个终端入口（按键或图标）。", Colors.Red));
				return;
			}
			AnimusForgeTerminalSettings.IsHotkeyEnabled = false;
			InformationManager.DisplayMessage(new InformationMessage("已关闭终端按键呼出（U键）。", Colors.Yellow));
		}
		else
		{
			AnimusForgeTerminalSettings.IsHotkeyEnabled = true;
			InformationManager.DisplayMessage(new InformationMessage("已开启终端按键呼出（U键）。", Colors.Green));
		}
		OnPropertyChanged(nameof(IsHotkeyEnabled));
	}

	public void ExecuteToggleMapIcon()
	{
		if (AnimusForgeTerminalSettings.IsMapIconEnabled)
		{
			if (!AnimusForgeTerminalSettings.IsHotkeyEnabled)
			{
				InformationManager.DisplayMessage(new InformationMessage("至少需要保留一个终端入口（按键或图标）。", Colors.Red));
				return;
			}
			AnimusForgeTerminalSettings.IsMapIconEnabled = false;
			InformationManager.DisplayMessage(new InformationMessage("已关闭大地图终端图标入口。", Colors.Yellow));
		}
		else
		{
			AnimusForgeTerminalSettings.IsMapIconEnabled = true;
			InformationManager.DisplayMessage(new InformationMessage("已开启大地图终端图标入口。", Colors.Green));
		}
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
	public bool IsSelected { get => _isSelected; set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); } } }

	public AnimusForgeTerminalTabItemVM(string tabId, Action<string> onSelect)
	{
		TabId = tabId ?? "";
		_onSelect = onSelect;
	}

	public void ExecuteSelect() => _onSelect?.Invoke(TabId);
}

public sealed class AnimusForgeTerminalItemVM : ViewModel
{
	private readonly Action<AnimusForgeTerminalNode> _onOpen;

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

	[DataSourceProperty]
	public string Id => Node?.Id ?? "";

	[DataSourceProperty]
	public string Title => Node?.Title ?? "";

	[DataSourceProperty]
	public string HintText => string.IsNullOrWhiteSpace(Node?.Hint) ? "无附加说明。" : Node.Hint;

	[DataSourceProperty]
	public string IconText => string.IsNullOrWhiteSpace(Node?.Icon) ? "❖" : Node.Icon;

	[DataSourceProperty]
	public bool IsBranch => Node?.IsBranch == true;

	[DataSourceProperty]
	public string BadgeText => IsBranch ? "子面板" : "动作";

	[DataSourceProperty]
	public string ButtonText => IsBranch ? "进入 ➔" : Node?.OnExecute != null ? "查看" : "执行 ⚡";

	public AnimusForgeTerminalItemVM(AnimusForgeTerminalNode node, Action<AnimusForgeTerminalNode> onOpen)
	{
		Node = node;
		_image = node?.ImageFactory?.Invoke();
		_onOpen = onOpen;
	}

	public void ExecuteOpen() => _onOpen?.Invoke(Node);
}
