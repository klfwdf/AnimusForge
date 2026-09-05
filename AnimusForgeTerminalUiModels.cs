using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AFWarStatsTerminal.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
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

	public static void OnTick()
	{
		_activePopup?.ViewModel?.OnTick();
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
		if (_dataSource != null && _dataSource.HasUnsavedChanges)
		{
			TerminalSettingsRegistry.SaveSettings();
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
	private string _subtitleText = "选择工具直接执行，或调节全量 252 项机制参数。按 ESC 或关闭退出。";
	private string _breadcrumbText = "终端 / 全部";
	private string _selectedTab = "全部";
	private string _searchFilter = "";
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

	public bool HasUnsavedChanges => _hasUnsavedChanges;

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

		string[] tabs = new[] { "战争", "全部", "外交与国家", "提示词与规则", "AI核心", "决斗与部队", "场景互动", "角色与同伴" };
		foreach (string tab in tabs)
		{
			TabItems.Add(new AnimusForgeTerminalTabItemVM(tab, SelectTab));
		}
		WarStatsVm = new AfWarStatsPopupVM(ExecuteBack);

		// 默认折叠“全部”标签页下的 MCM 二级分组，避免首屏内容过长
		foreach (var d in TerminalSettingsRegistry.AllDefinitions)
		{
			if (!string.IsNullOrEmpty(d.GroupName))
			{
				_collapsedGroups.Add($"全部/{d.GroupName}");
			}
		}

		RefreshItems();
	}

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

	public void MarkModified()
	{
		_hasUnsavedChanges = true;
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
		item.DisplayValue = "请按任意键 (ESC取消)...";
		item.OnPropertyChanged(nameof(item.DisplayValue));
	}

	public void CancelListeningKey()
	{
		if (_activeListeningItem != null)
		{
			_activeListeningItem.IsListeningKey = false;
			DuelSettings s = DuelSettings.GetSettings();
			string orig = s != null ? _activeListeningItem.SettingDef?.Getter(s)?.ToString() ?? "" : "";
			_activeListeningItem.DisplayValue = string.IsNullOrWhiteSpace(orig) ? "未设置" : orig.ToUpperInvariant();
			_activeListeningItem.OnPropertyChanged(nameof(_activeListeningItem.DisplayValue));
			_activeListeningItem = null;
		}
	}

	public void OnTick()
	{
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
						InformationManager.DisplayMessage(new InformationMessage($"快捷键【{_activeListeningItem.Title}】已设置为 [{keyName}]", Colors.Green));
					}
					_activeListeningItem = null;
				}
			}
		}
	}

	private void RefreshItems()
	{
		foreach (AnimusForgeTerminalTabItemVM tab in TabItems)
		{
			tab.IsSelected = string.Equals(tab.TabId, _selectedTab, StringComparison.Ordinal);
		}

		MBBindingList<AnimusForgeTerminalItemVM> list = new MBBindingList<AnimusForgeTerminalItemVM>();

		if (_path.Count > 0)
		{
			foreach (AnimusForgeTerminalNode child in _path.Peek().Children)
			{
				list.Add(new AnimusForgeTerminalItemVM(child, OpenNode));
			}
		}
		else if (string.Equals(_selectedTab, "全部", StringComparison.Ordinal))
		{
			bool hasFilter = !string.IsNullOrWhiteSpace(_searchFilter);
			if (hasFilter)
			{
				list.Add(AnimusForgeTerminalItemVM.CreateSearchAction(
					$"🔍 当前搜索：\"{_searchFilter}\"",
					"点击重新输入搜索词，或清空筛选条件查看全部条目。",
					"清除筛选 ✕",
					ExecuteClearSearch
				));

				string q = _searchFilter.Trim();
				// 匹配过滤工具节点
				foreach (AnimusForgeTerminalNode node in _roots)
				{
					if (NodeMatchesQuery(node, q))
					{
						list.Add(new AnimusForgeTerminalItemVM(node, OpenNode));
					}
				}

				// 匹配过滤配置项
				var matchedSettings = TerminalSettingsRegistry.AllDefinitions.Where(d => SettingMatchesQuery(d, q)).ToList();
				if (matchedSettings.Count > 0)
				{
					var groups = matchedSettings.GroupBy(x => x.GroupName);
					foreach (var g in groups)
					{
						list.Add(AnimusForgeTerminalItemVM.CreateHeader($"▼  {g.Key}", $"{g.Count()} 项匹配"));
						foreach (var def in g)
						{
							list.Add(new AnimusForgeTerminalItemVM(def, MarkModified));
						}
					}
				}
			}
			else
			{
				list.Add(AnimusForgeTerminalItemVM.CreateSearchAction(
					"❖ 搜索全量机制与工具 (252 项)",
					"点击输入关键词，快速过滤全量机制参数、规则与快捷操作工具。",
					"搜索",
					ExecuteOpenSearch
				));

				string[] categories = new[] { "外交与国家", "提示词与规则", "AI核心", "决斗与部队", "场景互动", "角色与同伴" };
				foreach (string cat in categories)
				{
					var catTools = _roots.Where(x => string.Equals(x.Category, cat, StringComparison.Ordinal)).ToList();
					var catSettings = TerminalSettingsRegistry.GetByCategory(cat);
					int totalCount = catTools.Count + catSettings.Count;

					list.Add(AnimusForgeTerminalItemVM.CreateHeader($"❖ {cat}", $"{totalCount} 项"));

					foreach (AnimusForgeTerminalNode node in catTools)
					{
						list.Add(new AnimusForgeTerminalItemVM(node, OpenNode));
					}

					var groups = catSettings.GroupBy(x => x.GroupName);
					foreach (var g in groups)
					{
						string groupKey = $"全部/{g.Key}";
						bool isCollapsed = _collapsedGroups.Contains(groupKey);
						string collapseTitle = isCollapsed ? $"▶  {g.Key}" : $"▼  {g.Key}";
						string collapseBadge = isCollapsed ? $"{g.Count()} 项 [点击展开]" : $"{g.Count()} 项 [点击折叠]";

						list.Add(AnimusForgeTerminalItemVM.CreateCollapsibleHeader(collapseTitle, collapseBadge, groupKey, isCollapsed, ToggleGroupCollapse));

						if (!isCollapsed)
						{
							foreach (var def in g)
							{
								list.Add(new AnimusForgeTerminalItemVM(def, MarkModified));
							}
						}
					}
				}
			}
		}
		else
		{
			// 对应具体分类
			var catTools = _roots.Where(x => string.Equals(x.Category, _selectedTab, StringComparison.Ordinal)).ToList();
			if (catTools.Count > 0)
			{
				list.Add(AnimusForgeTerminalItemVM.CreateHeader("⚡ 专属快捷工具操作", $"{catTools.Count} 项工具"));
				foreach (AnimusForgeTerminalNode node in catTools)
				{
					list.Add(new AnimusForgeTerminalItemVM(node, OpenNode));
				}
			}

			var catSettings = TerminalSettingsRegistry.GetByCategory(_selectedTab);
			if (catSettings.Count > 0)
			{
				var groups = catSettings.GroupBy(x => x.GroupName);
				foreach (var g in groups)
				{
					string groupKey = $"{_selectedTab}/{g.Key}";
					bool isCollapsed = _collapsedGroups.Contains(groupKey);
					string collapseTitle = isCollapsed ? $"▶  {g.Key}" : $"▼  {g.Key}";
					string collapseBadge = isCollapsed ? $"{g.Count()} 项 [点击展开]" : $"{g.Count()} 项 [点击折叠]";

					list.Add(AnimusForgeTerminalItemVM.CreateCollapsibleHeader(collapseTitle, collapseBadge, groupKey, isCollapsed, ToggleGroupCollapse));

					if (!isCollapsed)
					{
						foreach (var def in g)
						{
							list.Add(new AnimusForgeTerminalItemVM(def, MarkModified));
						}
					}
				}
			}
		}

		Items = list;
		UpdateBreadcrumb();
		UpdateCanGoBack();
	}

	private static bool NodeMatchesQuery(AnimusForgeTerminalNode node, string q)
	{
		if (node == null) return false;
		if (node.Title?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
		if (node.Hint?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
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

	public void ExecuteOpenSearch()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(
			"搜索设置项与工具",
			"请输入关键词（参数名、中文说明或工具名称）：",
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"搜索",
			"取消",
			input =>
			{
				_searchFilter = input ?? "";
				RefreshItems();
			},
			null,
			shouldInputBeObfuscated: false,
			null,
			"",
			_searchFilter
		));
	}

	public void ExecuteClearSearch()
	{
		_searchFilter = "";
		RefreshItems();
	}

	public void ExecuteSaveSettings()
	{
		if (TerminalSettingsRegistry.SaveSettings())
		{
			_hasUnsavedChanges = false;
			InformationManager.DisplayMessage(new InformationMessage("AnimusForge 终端设置已成功保存！", Colors.Green));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("保存设置失败，请查看日志。", Colors.Red));
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
	private readonly Action<AnimusForgeTerminalNode> _onOpenNode;
	private readonly Action _onActionTrigger;
	private readonly Action _onModified;

	public AnimusForgeTerminalNode Node { get; }
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

	private SelectorVM<SelectorItemVM> _dropdownSelector;
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
					DisplayValue = string.IsNullOrWhiteSpace(value) ? "点击填写 ✎" : (value.Length > 24 ? value.Substring(0, 24) + "..." : value);
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
	public static AnimusForgeTerminalItemVM CreateHeader(string title, string badge = "")
	{
		return new AnimusForgeTerminalItemVM(TerminalItemKind.Header, title, "", "", "❖", badge, "");
	}

	// 构造：支持点击折叠/展开的分组标题 Header
	public static AnimusForgeTerminalItemVM CreateCollapsibleHeader(string title, string badge, string groupKey, bool isCollapsed, Action<string> onToggle)
	{
		return new AnimusForgeTerminalItemVM(TerminalItemKind.Header, title, "", "", "❖", badge, "", null, true, isCollapsed, groupKey, onToggle);
	}

	// 构造：搜索动作项
	public static AnimusForgeTerminalItemVM CreateSearchAction(string title, string hint, string buttonText, Action onAction)
	{
		return new AnimusForgeTerminalItemVM(TerminalItemKind.Action, title, "", hint, "❖", "检索", buttonText, onAction);
	}

	// 内部全参构造
	private AnimusForgeTerminalItemVM(TerminalItemKind kind, string title, string codeName, string hintText, string iconText, string badgeText, string buttonText, Action onAction = null, bool isCollapsible = false, bool isCollapsed = false, string groupKey = "", Action<string> onToggle = null)
	{
		ItemKind = kind;
		Title = title ?? "";
		CodeName = codeName ?? "";
		HintText = hintText ?? "";
		IconText = string.IsNullOrWhiteSpace(iconText) ? "❖" : iconText;
		BadgeText = badgeText ?? "";
		ButtonText = buttonText ?? "";
		_onActionTrigger = onAction;
		IsCollapsible = isCollapsible;
		IsCollapsed = isCollapsed;
		GroupKey = groupKey ?? "";
		_onToggleCollapse = onToggle;
	}

	// 构造：原有 Action 工具节点
	public AnimusForgeTerminalItemVM(AnimusForgeTerminalNode node, Action<AnimusForgeTerminalNode> onOpen)
	{
		Node = node;
		_onOpenNode = onOpen;
		ItemKind = TerminalItemKind.Action;
		Id = node?.Id ?? "";
		Title = node?.Title ?? "";
		CodeName = "";
		HintText = string.IsNullOrWhiteSpace(node?.Hint) ? "无附加说明。" : node.Hint;
		IconText = string.IsNullOrWhiteSpace(node?.Icon) ? "❖" : node.Icon;
		BadgeText = node?.IsBranch == true ? "子面板" : "动作";
		ButtonText = node?.IsBranch == true ? "进入 ➔" : "执行 ⚡";
	}

	// 构造：MCM 设置项
	public AnimusForgeTerminalItemVM(TerminalSettingDef def, Action onModified)
	{
		SettingDef = def;
		_onModified = onModified;
		Id = def?.Id ?? "";
		Title = def?.DisplayName ?? "";
		CodeName = def?.Id ?? "";
		HintText = string.IsNullOrWhiteSpace(def?.HintText) ? "按需调节该机制项的运行参数或开关。" : def.HintText;
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
				BadgeText = "开关";
				BoolValue = Convert.ToBoolean(def.Getter(settings));
				BoolStatusText = BoolValue ? "✔ 开启" : "✕ 关闭";
				break;

			case TerminalSettingType.Integer:
			case TerminalSettingType.FloatingInteger:
				ItemKind = TerminalItemKind.NumericSetting;
				BadgeText = "参数";
				object numVal = def.Getter(settings);
				DisplayValue = FormatNumeric(numVal, def.FormatString, def.Id);
				break;

			case TerminalSettingType.Dropdown:
				ItemKind = TerminalItemKind.DropdownSetting;
				BadgeText = "下拉";
				DisplayValue = def.Getter(settings)?.ToString() ?? "";
				var dropdown = def.DropdownGetter?.Invoke(settings);
				if (dropdown != null && dropdown.Count > 0)
				{
					var options = new List<string>();
					for (int i = 0; i < dropdown.Count; i++)
					{
						options.Add(dropdown[i]);
					}
					_dropdownSelector = new SelectorVM<SelectorItemVM>(options, dropdown.SelectedIndex, OnDropdownItemChanged);
				}
				break;

			case TerminalSettingType.Hotkey:
				ItemKind = TerminalItemKind.HotkeySetting;
				BadgeText = "按键";
				string keyStr = def.Getter(settings)?.ToString() ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(keyStr) ? "未设置" : keyStr.ToUpperInvariant();
				break;

			case TerminalSettingType.Text:
				ItemKind = TerminalItemKind.TextSetting;
				BadgeText = "文本";
				string raw = def.Getter(settings)?.ToString() ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(raw) ? "点击填写 ✎" : (raw.Length > 24 ? raw.Substring(0, 24) + "..." : raw);
				break;

			case TerminalSettingType.Button:
				ItemKind = TerminalItemKind.ButtonSetting;
				BadgeText = "执行";
				ButtonText = "执行 ⚡";
				break;
		}
	}

	private void OnDropdownItemChanged(SelectorVM<SelectorItemVM> selector)
	{
		if (SettingDef == null || selector == null || selector.SelectedIndex < 0) return;
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null) return;
		SettingDef.Setter(settings, selector.SelectedIndex);
		DisplayValue = SettingDef.Getter(settings)?.ToString() ?? "";
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
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
			unit = " 秒";
		}
		else if (propId.IndexOf("Days", StringComparison.OrdinalIgnoreCase) >= 0 || propId.IndexOf("Day", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			unit = " 天";
		}
		else if (propId.IndexOf("Hours", StringComparison.OrdinalIgnoreCase) >= 0 || propId.IndexOf("Hour", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			unit = " 小时";
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
			return $"{(int)f} 第纳尔";
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
		else if (_onActionTrigger != null)
		{
			_onActionTrigger.Invoke();
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
		BoolStatusText = next ? "✔ 开启" : "✕ 关闭";
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
		InformationManager.ShowTextInquiry(new TextInquiryData(
			$"设置 {Title}",
			$"请输入数值（有效范围: {SettingDef.MinValue} ~ {SettingDef.MaxValue}）：\n{HintText}",
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"确定",
			"取消",
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
		DisplayValue = SettingDef.Getter(settings)?.ToString() ?? "";
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
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
		DisplayValue = SettingDef.Getter(settings)?.ToString() ?? "";
		OnPropertyChanged(nameof(DisplayValue));
		_onModified?.Invoke();
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
			string opt = dropdown[i];
			elements.Add(new InquiryElement(i.ToString(), opt, null, isEnabled: true, ""));
		}

		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			$"选择 {Title}",
			$"请从以下可用选项中选择：\n{HintText}",
			elements,
			isExitShown: true,
			1,
			1,
			"确定",
			"返回",
			selected =>
			{
				if (selected != null && selected.Count > 0 && int.TryParse(selected[0].Identifier as string, out int idx))
				{
					SettingDef.Setter(settings, idx);
					DisplayValue = SettingDef.Getter(settings)?.ToString() ?? "";
					OnPropertyChanged(nameof(DisplayValue));
					_onModified?.Invoke();
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
		InformationManager.ShowTextInquiry(new TextInquiryData(
			$"编辑 {Title}",
			$"{HintText}",
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"确定",
			"取消",
			input =>
			{
				SettingDef.Setter(settings, input ?? "");
				string raw = input ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(raw) ? "点击填写 ✎" : (raw.Length > 24 ? raw.Substring(0, 24) + "..." : raw);
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
			InformationManager.DisplayMessage(new InformationMessage($"已触发：{Title}", Colors.Green));
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", $"[ERROR] Failed to invoke setting action {SettingDef.Id}: {ex}");
			InformationManager.DisplayMessage(new InformationMessage($"触发失败：{ex.Message}", Colors.Red));
		}
	}

	public void UpdateFromSettings(DuelSettings settings)
	{
		if (SettingDef == null || settings == null) return;
		switch (ItemKind)
		{
			case TerminalItemKind.BoolSetting:
				BoolValue = Convert.ToBoolean(SettingDef.Getter(settings));
				BoolStatusText = BoolValue ? "✔ 开启" : "✕ 关闭";
				OnPropertyChanged(nameof(BoolValue));
				OnPropertyChanged(nameof(BoolStatusText));
				OnPropertyChanged(nameof(IsBoolTrue));
				break;
			case TerminalItemKind.NumericSetting:
				DisplayValue = FormatNumeric(SettingDef.Getter(settings), SettingDef.FormatString, SettingDef.Id);
				OnPropertyChanged(nameof(DisplayValue));
				break;
			case TerminalItemKind.DropdownSetting:
				DisplayValue = SettingDef.Getter(settings)?.ToString() ?? "";
				OnPropertyChanged(nameof(DisplayValue));
				if (_dropdownSelector != null)
				{
					var dropdown = SettingDef.DropdownGetter?.Invoke(settings);
					if (dropdown != null && dropdown.SelectedIndex != _dropdownSelector.SelectedIndex && dropdown.SelectedIndex >= 0 && dropdown.SelectedIndex < _dropdownSelector.ItemList.Count)
					{
						_dropdownSelector.SelectedIndex = dropdown.SelectedIndex;
					}
				}
				break;
			case TerminalItemKind.HotkeySetting:
				if (!_isListeningKey)
				{
					string rawKey = SettingDef.Getter(settings)?.ToString() ?? "";
					DisplayValue = string.IsNullOrWhiteSpace(rawKey) ? "未设置" : rawKey.ToUpperInvariant();
					OnPropertyChanged(nameof(DisplayValue));
				}
				break;
			case TerminalItemKind.TextSetting:
				string raw = SettingDef.Getter(settings)?.ToString() ?? "";
				DisplayValue = string.IsNullOrWhiteSpace(raw) ? "点击填写 ✎" : (raw.Length > 24 ? raw.Substring(0, 24) + "..." : raw);
				OnPropertyChanged(nameof(DisplayValue));
				OnPropertyChanged(nameof(TextValue));
				break;
		}
	}
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
