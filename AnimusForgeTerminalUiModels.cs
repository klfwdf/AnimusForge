using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AFWarStatsTerminal.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
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
	public string Category { get; set; } = "全部";
	public string Icon { get; set; } = "❖";
	public bool IsBranch => Children != null && Children.Count > 0;
	public List<AnimusForgeTerminalNode> Children { get; } = new List<AnimusForgeTerminalNode>();
}

public enum TerminalViewMode
{
	MenuList,
	WarStats,
	WeeklyReports,
	Vassalage,
	TagCatalog,
	TrustQuery,
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
		_layer = new GauntletLayer("AnimusForgeTerminalPopup", 3950, false);
	}

	public static bool Show(List<AnimusForgeTerminalNode> roots, Func<string, bool> onExecuteLeaf, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null || roots == null || roots.Count <= 0 || onExecuteLeaf == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			AnimusForgeTerminalPopup popup = new AnimusForgeTerminalPopup(topScreen, roots, onExecuteLeaf, onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] Failed to open terminal popup: " + ex);
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
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
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

	// 内嵌模块：标签字典
	private string _tagCatalogSummaryText = "";
	private MBBindingList<TerminalTagCatalogItemVM> _tagCatalogItems;

	// 内嵌模块：信任度查询
	private string _trustQuerySummaryText = "";
	private MBBindingList<TerminalTrustItemVM> _trustItems;

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
	public bool IsTagCatalogVisible => _currentViewMode == TerminalViewMode.TagCatalog;

	[DataSourceProperty]
	public bool IsTrustQueryVisible => _currentViewMode == TerminalViewMode.TrustQuery;

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
	public string TagCatalogSummaryText { get => _tagCatalogSummaryText; set { if (value != _tagCatalogSummaryText) { _tagCatalogSummaryText = value; OnPropertyChangedWithValue(value, nameof(TagCatalogSummaryText)); } } }

	[DataSourceProperty]
	public MBBindingList<TerminalTagCatalogItemVM> TagCatalogItems { get => _tagCatalogItems; set { if (value != _tagCatalogItems) { _tagCatalogItems = value; OnPropertyChangedWithValue(value, nameof(TagCatalogItems)); } } }

	[DataSourceProperty]
	public string TrustQuerySummaryText { get => _trustQuerySummaryText; set { if (value != _trustQuerySummaryText) { _trustQuerySummaryText = value; OnPropertyChangedWithValue(value, nameof(TrustQuerySummaryText)); } } }

	[DataSourceProperty]
	public MBBindingList<TerminalTrustItemVM> TrustItems { get => _trustItems; set { if (value != _trustItems) { _trustItems = value; OnPropertyChangedWithValue(value, nameof(TrustItems)); } } }

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
		TagCatalogItems = new MBBindingList<TerminalTagCatalogItemVM>();
		TrustItems = new MBBindingList<TerminalTrustItemVM>();

		string[] tabs = new[] { "战争", "全部", "外交", "部队", "玩家", "查询与记录", "系统" };
		foreach (string tab in tabs)
		{
			TabItems.Add(new AnimusForgeTerminalTabItemVM(tab, SelectTab));
		}
		WarStatsVm = new AfWarStatsPopupVM(ExecuteBack);
		RefreshItems();
	}

	public void SelectTab(string tab)
	{
		_selectedTab = string.IsNullOrWhiteSpace(tab) ? "全部" : tab;
		_path.Clear();
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
		OnPropertyChanged(nameof(IsTagCatalogVisible));
		OnPropertyChanged(nameof(IsTrustQueryVisible));
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

		MBBindingList<AnimusForgeTerminalItemVM> list = new MBBindingList<AnimusForgeTerminalItemVM>();
		foreach (AnimusForgeTerminalNode node in source)
		{
			list.Add(new AnimusForgeTerminalItemVM(node, OpenNode));
		}
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
			RefreshItems();
			return;
		}

		try
		{
			_onExecuteLeaf?.Invoke(node.Id ?? "");
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
		WeeklyReportVm = new TerminalWeeklyReportBrowserPopupVM(countries ?? new List<MyBehavior.WeeklyReportBrowserCountryData>(), null, ExecuteBack);
		BreadcrumbText = "终端 / " + _selectedTab + " / 查看周报";
		SetViewMode(TerminalViewMode.WeeklyReports);
	}

	public void ShowVassalageTributeHistory(TerminalTributaryPaymentHistoryData data)
	{
		VassalageVm = new TerminalVassalageTributeHistoryPopupVM(data ?? new TerminalTributaryPaymentHistoryData(), ExecuteBack);
		BreadcrumbText = "终端 / " + _selectedTab + " / 臣属贡金结算记录";
		SetViewMode(TerminalViewMode.Vassalage);
	}

	internal void ShowTagCatalog(AnimusForgeTagCatalogSnapshot snapshot)
	{
		snapshot ??= AnimusForgeTagCatalog.BuildSnapshot(forceRefresh: false);
		TagCatalogSummaryText = $"已从当前代码库与配置文件提取到 {snapshot.Entries.Count} 个指令动作标签。";
		MBBindingList<TerminalTagCatalogItemVM> items = new MBBindingList<TerminalTagCatalogItemVM>();
		foreach (AnimusForgeTagCatalogEntry entry in snapshot.Entries)
		{
			items.Add(new TerminalTagCatalogItemVM(entry));
		}
		TagCatalogItems = items;
		BreadcrumbText = "终端 / " + _selectedTab + " / 指令标签列表";
		SetViewMode(TerminalViewMode.TagCatalog);
	}

	public void ShowTrustQuery(List<Settlement> settlements, List<Hero> heroes)
	{
		RewardSystemBehavior reward = RewardSystemBehavior.Instance;
		MBBindingList<TerminalTrustItemVM> list = new MBBindingList<TerminalTrustItemVM>();
		if (settlements != null)
		{
			foreach (Settlement s in settlements.Take(30))
			{
				int trust = reward != null ? (reward.GetSettlementLocalPublicTrust(s) + reward.GetSettlementSharedPublicTrust(s)) : 0;
				list.Add(new TerminalTrustItemVM(s.Name?.ToString() ?? s.StringId, "定居点", trust, s.OwnerClan?.Name?.ToString() ?? ""));
			}
		}
		if (heroes != null)
		{
			foreach (Hero h in heroes.Take(30))
			{
				int trust = reward?.GetEffectiveTrust(h) ?? 0;
				list.Add(new TerminalTrustItemVM(h.Name?.ToString() ?? h.StringId, "NPC领主", trust, h.Clan?.Name?.ToString() ?? ""));
			}
		}
		TrustItems = list;
		TrustQuerySummaryText = $"已统计当前卡拉迪亚主要封地与领主信任度 (显示前 {list.Count} 项)";
		BreadcrumbText = "终端 / " + _selectedTab + " / 信任度查询";
		SetViewMode(TerminalViewMode.TrustQuery);
	}

	public void ShowDiagnostics(string status, string detail, bool canAnalyze)
	{
		DiagnosticsStatusText = status ?? "捕获哨兵状态: 在线";
		DiagnosticsDetailText = string.IsNullOrWhiteSpace(detail) ? "本局当前未记录到任何致命报错堆栈。" : detail;
		HasLatestErrorToAnalyze = canAnalyze;
		BreadcrumbText = "终端 / " + _selectedTab + " / 系统错误分析与诊断";
		SetViewMode(TerminalViewMode.Diagnostics);
	}

	public void ExecuteExportTagCatalog()
	{
		if (AnimusForgeTagCatalog.TryExportSnapshotToModuleTxt(null, out string path, out string err))
		{
			InformationManager.DisplayMessage(new InformationMessage("已导出标签字典至: " + Path.GetFileName(path), Colors.Green));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败: " + err, Colors.Red));
		}
	}

	public void ExecuteRefreshTagCatalog()
	{
		AnimusForgeTagCatalogSnapshot snapshot = AnimusForgeTagCatalog.BuildSnapshot(forceRefresh: true);
		ShowTagCatalog(snapshot);
		InformationManager.DisplayMessage(new InformationMessage("标签字典已强制刷新。", Colors.Yellow));
	}

	public void ExecuteRequestAiAnalysis()
	{
		AiErrorAnalysisInquiry.AnalyzeLatestFailure();
	}

	public void ExecuteBack()
	{
		if (_currentViewMode != TerminalViewMode.MenuList)
		{
			SelectTab("全部");
			return;
		}

		if (_path.Count > 0)
		{
			_path.Pop();
			RefreshItems();
		}
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
	public string ButtonText => IsBranch ? "进入 ➔" : "执行 ⚡";

	public AnimusForgeTerminalItemVM(AnimusForgeTerminalNode node, Action<AnimusForgeTerminalNode> onOpen)
	{
		Node = node;
		_onOpen = onOpen;
	}

	public void ExecuteOpen() => _onOpen?.Invoke(Node);
}

public sealed class TerminalTagCatalogItemVM : ViewModel
{
	[DataSourceProperty]
	public string Tag { get; }

	[DataSourceProperty]
	public string Category { get; }

	[DataSourceProperty]
	public string Description { get; }

	internal TerminalTagCatalogItemVM(AnimusForgeTagCatalogEntry entry)
	{
		Tag = entry?.Tag ?? "";
		Category = entry?.Category ?? "";
		Description = entry?.Description ?? "";
	}
}

public sealed class TerminalTrustItemVM : ViewModel
{
	[DataSourceProperty]
	public string Name { get; }

	[DataSourceProperty]
	public string TypeText { get; }

	[DataSourceProperty]
	public string TrustValueText { get; }

	[DataSourceProperty]
	public string ExtraInfo { get; }

	public TerminalTrustItemVM(string name, string typeText, int trustValue, string extraInfo)
	{
		Name = name ?? "";
		TypeText = typeText ?? "";
		TrustValueText = (trustValue >= 0 ? "+" : "") + trustValue.ToString();
		ExtraInfo = extraInfo ?? "";
	}
}
