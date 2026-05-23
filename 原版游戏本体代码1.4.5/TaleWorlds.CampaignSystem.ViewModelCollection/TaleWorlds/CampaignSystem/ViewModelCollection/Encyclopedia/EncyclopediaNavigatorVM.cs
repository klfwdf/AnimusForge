using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class EncyclopediaNavigatorVM : ViewModel
{
	private class SearchResultComparer : IComparer<EncyclopediaSearchResultVM>
	{
		private string _searchText;

		public string SearchText
		{
			get
			{
				return _searchText;
			}
			set
			{
				if (value != _searchText)
				{
					_searchText = value;
				}
			}
		}

		public SearchResultComparer(string searchText)
		{
			SearchText = searchText;
		}

		private int CompareBasedOnCapitalization(EncyclopediaSearchResultVM x, EncyclopediaSearchResultVM y)
		{
			int num = ((x.NameText.Length > 0 && char.IsUpper(x.NameText[0])) ? 1 : (-1));
			int value = ((y.NameText.Length > 0 && char.IsUpper(y.NameText[0])) ? 1 : (-1));
			return num.CompareTo(value);
		}

		public int Compare(EncyclopediaSearchResultVM x, EncyclopediaSearchResultVM y)
		{
			if (x.MatchStartIndex == y.MatchStartIndex)
			{
				int num = CompareBasedOnCapitalization(x, y);
				if (num == 0)
				{
					return y.NameText.Length.CompareTo(x.NameText.Length);
				}
				return num;
			}
			int matchStartIndex = y.MatchStartIndex;
			return matchStartIndex.CompareTo(x.MatchStartIndex);
		}
	}

	private List<Tuple<string, object>> History;

	private int HistoryIndex;

	private readonly Func<string, object, bool, EncyclopediaPageVM> _goToLink;

	private readonly Action _closeEncyclopedia;

	private SearchResultComparer _searchResultComparer;

	private MBBindingList<EncyclopediaSearchResultVM> _searchResults;

	private string _searchText = "";

	private string _pageName;

	private string _doneText;

	private string _leaderText;

	private bool _canSwitchTabs;

	private bool _isBackEnabled;

	private bool _isForwardEnabled;

	private bool _isHighlightEnabled;

	private bool _isSearchResultsShown;

	private string _navBarString;

	private int _minCharAmountToShowResults;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _previousPageInputKey;

	private InputKeyItemVM _nextPageInputKey;

	public Tuple<string, object> LastActivePage
	{
		get
		{
			if (!History.IsEmpty())
			{
				return History[HistoryIndex];
			}
			return null;
		}
	}

	[DataSourceProperty]
	public bool CanSwitchTabs
	{
		get
		{
			return _canSwitchTabs;
		}
		set
		{
			if (value != _canSwitchTabs)
			{
				_canSwitchTabs = value;
				OnPropertyChangedWithValue(value, "CanSwitchTabs");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBackEnabled
	{
		get
		{
			return _isBackEnabled;
		}
		set
		{
			if (value != _isBackEnabled)
			{
				_isBackEnabled = value;
				OnPropertyChangedWithValue(value, "IsBackEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsForwardEnabled
	{
		get
		{
			return _isForwardEnabled;
		}
		set
		{
			if (value != _isForwardEnabled)
			{
				_isForwardEnabled = value;
				OnPropertyChangedWithValue(value, "IsForwardEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlightEnabled
	{
		get
		{
			return _isHighlightEnabled;
		}
		set
		{
			if (value != _isHighlightEnabled)
			{
				_isHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSearchResultsShown
	{
		get
		{
			return _isSearchResultsShown;
		}
		set
		{
			if (value != _isSearchResultsShown)
			{
				_isSearchResultsShown = value;
				OnPropertyChangedWithValue(value, "IsSearchResultsShown");
			}
		}
	}

	[DataSourceProperty]
	public string NavBarString
	{
		get
		{
			return _navBarString;
		}
		set
		{
			if (value != _navBarString)
			{
				_navBarString = value;
				OnPropertyChangedWithValue(value, "NavBarString");
			}
		}
	}

	[DataSourceProperty]
	public string PageName
	{
		get
		{
			return _pageName;
		}
		set
		{
			if (value != _pageName)
			{
				_pageName = value;
				OnPropertyChangedWithValue(value, "PageName");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public string LeaderText
	{
		get
		{
			return _leaderText;
		}
		set
		{
			if (value != _leaderText)
			{
				_leaderText = value;
				OnPropertyChangedWithValue(value, "LeaderText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSearchResultVM> SearchResults
	{
		get
		{
			return _searchResults;
		}
		set
		{
			if (value != _searchResults)
			{
				_searchResults = value;
				OnPropertyChangedWithValue(value, "SearchResults");
			}
		}
	}

	[DataSourceProperty]
	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (value != _searchText)
			{
				bool isAppending = value.ToLower().Contains(_searchText);
				bool isPasted = string.IsNullOrEmpty(_searchText) && !string.IsNullOrEmpty(value);
				_searchText = value.ToLower();
				Debug.Print("isAppending: " + isAppending + " isPasted: " + isPasted);
				RefreshSearch(isAppending, isPasted);
				OnPropertyChangedWithValue(value, "SearchText");
			}
		}
	}

	[DataSourceProperty]
	public int MinCharAmountToShowResults
	{
		get
		{
			return _minCharAmountToShowResults;
		}
		set
		{
			if (value != _minCharAmountToShowResults)
			{
				_minCharAmountToShowResults = value;
				OnPropertyChangedWithValue(value, "MinCharAmountToShowResults");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PreviousPageInputKey
	{
		get
		{
			return _previousPageInputKey;
		}
		set
		{
			if (value != _previousPageInputKey)
			{
				_previousPageInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousPageInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextPageInputKey
	{
		get
		{
			return _nextPageInputKey;
		}
		set
		{
			if (value != _nextPageInputKey)
			{
				_nextPageInputKey = value;
				OnPropertyChangedWithValue(value, "NextPageInputKey");
			}
		}
	}

	public EncyclopediaNavigatorVM(Func<string, object, bool, EncyclopediaPageVM> goToLink, Action closeEncyclopedia)
	{
		_closeEncyclopedia = closeEncyclopedia;
		History = new List<Tuple<string, object>>();
		HistoryIndex = 0;
		MinCharAmountToShowResults = 3;
		SearchResults = new MBBindingList<EncyclopediaSearchResultVM>();
		Campaign.Current.EncyclopediaManager.SetLinkCallback(ExecuteLink);
		_goToLink = goToLink;
		_searchResultComparer = new SearchResultComparer(string.Empty);
		AddHistory("Home", null);
		RefreshValues();
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
	{
		IsHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaSearchButton";
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		PreviousPageInputKey?.OnFinalize();
		NextPageInputKey?.OnFinalize();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DoneText = GameTexts.FindText("str_done").ToString();
		LeaderText = GameTexts.FindText("str_done").ToString();
	}

	public void ExecuteHome()
	{
		Campaign.Current.EncyclopediaManager.GoToLink("Home", "-1");
	}

	public void ExecuteBarLink(string targetID)
	{
		if (targetID.Contains("Home"))
		{
			ExecuteHome();
		}
		else if (targetID.Contains("ListPage"))
		{
			switch (targetID.Split(new char[1] { '-' })[1])
			{
			case "Clans":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Faction");
				break;
			case "Kingdoms":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Kingdom");
				break;
			case "Heroes":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Hero");
				break;
			case "Settlements":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Settlement");
				break;
			case "Units":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "NPCCharacter");
				break;
			case "Concept":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Concept");
				break;
			case "Ships":
				Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "ShipHull");
				break;
			}
		}
	}

	public void ExecuteCloseEncyclopedia()
	{
		_closeEncyclopedia();
	}

	private void ExecuteLink(string pageId, object target)
	{
		if (pageId != "LastPage" && target != LastActivePage.Item2)
		{
			if (pageId != "Home")
			{
				_ = 1;
			}
			else
				_ = pageId != LastActivePage.Item1;
			AddHistory(pageId, target);
		}
		_goToLink(pageId, target, arg3: true);
		PageName = GameTexts.FindText("str_encyclopedia_name").ToString();
		ResetSearch();
	}

	public void ResetHistory()
	{
		HistoryIndex = 0;
		History.Clear();
		AddHistory("Home", null);
	}

	public void ExecuteBack()
	{
		if (HistoryIndex != 0)
		{
			int num = HistoryIndex - 1;
			Tuple<string, object> tuple = History[num];
			if (tuple.Item1 != "LastPage" && (tuple.Item1 != LastActivePage.Item1 || tuple.Item2 != LastActivePage.Item2))
			{
				if (tuple.Item1 != "Home")
				{
					_ = 1;
				}
				else
					_ = tuple.Item1 != LastActivePage.Item1;
			}
			else
				_ = 0;
			UpdateHistoryIndex(num);
			_goToLink(tuple.Item1, tuple.Item2, arg3: true);
			PageName = GameTexts.FindText("str_encyclopedia_name").ToString();
		}
	}

	public void ExecuteForward()
	{
		if (HistoryIndex != History.Count - 1)
		{
			int num = HistoryIndex + 1;
			Tuple<string, object> tuple = History[num];
			if (tuple.Item1 != "LastPage" && (tuple.Item1 != LastActivePage.Item1 || tuple.Item2 != LastActivePage.Item2))
			{
				if (tuple.Item1 != "Home")
				{
					_ = 1;
				}
				else
					_ = tuple.Item1 != LastActivePage.Item1;
			}
			else
				_ = 0;
			UpdateHistoryIndex(num);
			_goToLink(tuple.Item1, tuple.Item2, arg3: true);
			PageName = GameTexts.FindText("str_encyclopedia_name").ToString();
		}
	}

	public Tuple<string, object> GetLastPage()
	{
		return History[HistoryIndex];
	}

	public void AddHistory(string pageId, object obj)
	{
		if (HistoryIndex < History.Count - 1)
		{
			Tuple<string, object> tuple = History[HistoryIndex];
			if (tuple.Item1 == pageId && tuple.Item2 == obj)
			{
				return;
			}
			History.RemoveRange(HistoryIndex + 1, History.Count - HistoryIndex - 1);
		}
		History.Add(new Tuple<string, object>(pageId, obj));
		UpdateHistoryIndex(History.Count - 1);
	}

	private void UpdateHistoryIndex(int newIndex)
	{
		HistoryIndex = newIndex;
		IsBackEnabled = newIndex > 0;
		IsForwardEnabled = newIndex < History.Count - 1;
	}

	public void UpdatePageName(string value)
	{
		PageName = GameTexts.FindText("str_encyclopedia_name").ToString();
	}

	private void RefreshSearch(bool isAppending, bool isPasted)
	{
		int firstAsianCharIndex = GetFirstAsianCharIndex(SearchText);
		MinCharAmountToShowResults = ((firstAsianCharIndex > -1 && firstAsianCharIndex < 3) ? (firstAsianCharIndex + 1) : 3);
		if (SearchText.Length < MinCharAmountToShowResults)
		{
			SearchResults.Clear();
			return;
		}
		string text = StringHelpers.RemoveDiacritics(_searchText);
		if (!isAppending || SearchText.Length == MinCharAmountToShowResults || isPasted)
		{
			SearchResults.Clear();
			foreach (EncyclopediaPage encyclopediaPage in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages())
			{
				foreach (EncyclopediaListItem listItem in encyclopediaPage.GetListItems())
				{
					int num = StringHelpers.RemoveDiacritics(listItem.Name).IndexOf(text, StringComparison.InvariantCultureIgnoreCase);
					if (num >= 0)
					{
						SearchResults.Add(new EncyclopediaSearchResultVM(listItem, text, num));
					}
				}
			}
			_searchResultComparer.SearchText = text;
			SearchResults.Sort(_searchResultComparer);
		}
		else
		{
			if (!isAppending)
			{
				return;
			}
			foreach (EncyclopediaSearchResultVM item in SearchResults.ToList())
			{
				if (StringHelpers.RemoveDiacritics(item.OrgNameText).IndexOf(text, StringComparison.InvariantCultureIgnoreCase) == -1)
				{
					SearchResults.Remove(item);
				}
				else
				{
					item.UpdateSearchedText(text);
				}
			}
		}
	}

	private static int GetFirstAsianCharIndex(string searchText)
	{
		for (int i = 0; i < searchText.Length; i++)
		{
			if (Common.IsCharAsian(searchText[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public void ResetSearch()
	{
		SearchText = string.Empty;
	}

	public void ExecuteOnSearchActivated()
	{
		Game.Current.EventManager.TriggerEvent(new OnEncyclopediaSearchActivatedEvent());
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetPreviousPageInputKey(HotKey hotkey)
	{
		PreviousPageInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetNextPageInputKey(HotKey hotkey)
	{
		NextPageInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
