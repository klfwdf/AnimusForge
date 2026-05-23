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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000CB RID: 203
	public class EncyclopediaNavigatorVM : ViewModel
	{
		// Token: 0x17000642 RID: 1602
		// (get) Token: 0x0600131E RID: 4894 RVA: 0x0004CDE7 File Offset: 0x0004AFE7
		public Tuple<string, object> LastActivePage
		{
			get
			{
				if (!this.History.IsEmpty<Tuple<string, object>>())
				{
					return this.History[this.HistoryIndex];
				}
				return null;
			}
		}

		// Token: 0x0600131F RID: 4895 RVA: 0x0004CE0C File Offset: 0x0004B00C
		public EncyclopediaNavigatorVM(Func<string, object, bool, EncyclopediaPageVM> goToLink, Action closeEncyclopedia)
		{
			this._closeEncyclopedia = closeEncyclopedia;
			this.History = new List<Tuple<string, object>>();
			this.HistoryIndex = 0;
			this.MinCharAmountToShowResults = 3;
			this.SearchResults = new MBBindingList<EncyclopediaSearchResultVM>();
			Campaign.Current.EncyclopediaManager.SetLinkCallback(new Action<string, object>(this.ExecuteLink));
			this._goToLink = goToLink;
			this._searchResultComparer = new EncyclopediaNavigatorVM.SearchResultComparer(string.Empty);
			this.AddHistory("Home", null);
			this.RefreshValues();
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x06001320 RID: 4896 RVA: 0x0004CEB4 File Offset: 0x0004B0B4
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
		{
			this.IsHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaSearchButton";
		}

		// Token: 0x06001321 RID: 4897 RVA: 0x0004CECC File Offset: 0x0004B0CC
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM previousPageInputKey = this.PreviousPageInputKey;
			if (previousPageInputKey != null)
			{
				previousPageInputKey.OnFinalize();
			}
			InputKeyItemVM nextPageInputKey = this.NextPageInputKey;
			if (nextPageInputKey == null)
			{
				return;
			}
			nextPageInputKey.OnFinalize();
		}

		// Token: 0x06001322 RID: 4898 RVA: 0x0004CEF5 File Offset: 0x0004B0F5
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.LeaderText = GameTexts.FindText("str_done", null).ToString();
		}

		// Token: 0x06001323 RID: 4899 RVA: 0x0004CF29 File Offset: 0x0004B129
		public void ExecuteHome()
		{
			Campaign.Current.EncyclopediaManager.GoToLink("Home", "-1");
		}

		// Token: 0x06001324 RID: 4900 RVA: 0x0004CF44 File Offset: 0x0004B144
		public void ExecuteBarLink(string targetID)
		{
			if (targetID.Contains("Home"))
			{
				this.ExecuteHome();
				return;
			}
			if (targetID.Contains("ListPage"))
			{
				string a = targetID.Split(new char[] { '-' })[1];
				if (a == "Clans")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Faction");
					return;
				}
				if (a == "Kingdoms")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Kingdom");
					return;
				}
				if (a == "Heroes")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Hero");
					return;
				}
				if (a == "Settlements")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Settlement");
					return;
				}
				if (a == "Units")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "NPCCharacter");
					return;
				}
				if (a == "Concept")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "Concept");
					return;
				}
				if (a == "Ships")
				{
					Campaign.Current.EncyclopediaManager.GoToLink("ListPage", "ShipHull");
				}
			}
		}

		// Token: 0x06001325 RID: 4901 RVA: 0x0004D099 File Offset: 0x0004B299
		public void ExecuteCloseEncyclopedia()
		{
			this._closeEncyclopedia();
		}

		// Token: 0x06001326 RID: 4902 RVA: 0x0004D0A8 File Offset: 0x0004B2A8
		private void ExecuteLink(string pageId, object target)
		{
			if (pageId != "LastPage" && target != this.LastActivePage.Item2)
			{
				if (!(pageId != "Home"))
				{
					pageId != this.LastActivePage.Item1;
				}
				this.AddHistory(pageId, target);
			}
			this._goToLink(pageId, target, true);
			this.PageName = GameTexts.FindText("str_encyclopedia_name", null).ToString();
			this.ResetSearch();
		}

		// Token: 0x06001327 RID: 4903 RVA: 0x0004D12D File Offset: 0x0004B32D
		public void ResetHistory()
		{
			this.HistoryIndex = 0;
			this.History.Clear();
			this.AddHistory("Home", null);
		}

		// Token: 0x06001328 RID: 4904 RVA: 0x0004D150 File Offset: 0x0004B350
		public void ExecuteBack()
		{
			if (this.HistoryIndex == 0)
			{
				return;
			}
			int num = this.HistoryIndex - 1;
			Tuple<string, object> tuple = this.History[num];
			if (tuple.Item1 != "LastPage" && (tuple.Item1 != this.LastActivePage.Item1 || tuple.Item2 != this.LastActivePage.Item2))
			{
				if (!(tuple.Item1 != "Home"))
				{
					tuple.Item1 != this.LastActivePage.Item1;
				}
			}
			this.UpdateHistoryIndex(num);
			this._goToLink(tuple.Item1, tuple.Item2, true);
			this.PageName = GameTexts.FindText("str_encyclopedia_name", null).ToString();
		}

		// Token: 0x06001329 RID: 4905 RVA: 0x0004D220 File Offset: 0x0004B420
		public void ExecuteForward()
		{
			if (this.HistoryIndex == this.History.Count - 1)
			{
				return;
			}
			int num = this.HistoryIndex + 1;
			Tuple<string, object> tuple = this.History[num];
			if (tuple.Item1 != "LastPage" && (tuple.Item1 != this.LastActivePage.Item1 || tuple.Item2 != this.LastActivePage.Item2))
			{
				if (!(tuple.Item1 != "Home"))
				{
					tuple.Item1 != this.LastActivePage.Item1;
				}
			}
			this.UpdateHistoryIndex(num);
			this._goToLink(tuple.Item1, tuple.Item2, true);
			this.PageName = GameTexts.FindText("str_encyclopedia_name", null).ToString();
		}

		// Token: 0x0600132A RID: 4906 RVA: 0x0004D2FB File Offset: 0x0004B4FB
		public Tuple<string, object> GetLastPage()
		{
			return this.History[this.HistoryIndex];
		}

		// Token: 0x0600132B RID: 4907 RVA: 0x0004D310 File Offset: 0x0004B510
		public void AddHistory(string pageId, object obj)
		{
			if (this.HistoryIndex < this.History.Count - 1)
			{
				Tuple<string, object> tuple = this.History[this.HistoryIndex];
				if (tuple.Item1 == pageId && tuple.Item2 == obj)
				{
					return;
				}
				this.History.RemoveRange(this.HistoryIndex + 1, this.History.Count - this.HistoryIndex - 1);
			}
			this.History.Add(new Tuple<string, object>(pageId, obj));
			this.UpdateHistoryIndex(this.History.Count - 1);
		}

		// Token: 0x0600132C RID: 4908 RVA: 0x0004D3A8 File Offset: 0x0004B5A8
		private void UpdateHistoryIndex(int newIndex)
		{
			this.HistoryIndex = newIndex;
			this.IsBackEnabled = newIndex > 0;
			this.IsForwardEnabled = newIndex < this.History.Count - 1;
		}

		// Token: 0x0600132D RID: 4909 RVA: 0x0004D3D1 File Offset: 0x0004B5D1
		public void UpdatePageName(string value)
		{
			this.PageName = GameTexts.FindText("str_encyclopedia_name", null).ToString();
		}

		// Token: 0x0600132E RID: 4910 RVA: 0x0004D3EC File Offset: 0x0004B5EC
		private void RefreshSearch(bool isAppending, bool isPasted)
		{
			int firstAsianCharIndex = EncyclopediaNavigatorVM.GetFirstAsianCharIndex(this.SearchText);
			this.MinCharAmountToShowResults = ((firstAsianCharIndex > -1 && firstAsianCharIndex < 3) ? (firstAsianCharIndex + 1) : 3);
			if (this.SearchText.Length < this.MinCharAmountToShowResults)
			{
				this.SearchResults.Clear();
				return;
			}
			string text = StringHelpers.RemoveDiacritics(this._searchText);
			if (!isAppending || this.SearchText.Length == this.MinCharAmountToShowResults || isPasted)
			{
				this.SearchResults.Clear();
				foreach (EncyclopediaPage encyclopediaPage in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages())
				{
					foreach (EncyclopediaListItem encyclopediaListItem in encyclopediaPage.GetListItems())
					{
						int num = StringHelpers.RemoveDiacritics(encyclopediaListItem.Name).IndexOf(text, StringComparison.InvariantCultureIgnoreCase);
						if (num >= 0)
						{
							this.SearchResults.Add(new EncyclopediaSearchResultVM(encyclopediaListItem, text, num));
						}
					}
				}
				this._searchResultComparer.SearchText = text;
				this.SearchResults.Sort(this._searchResultComparer);
				return;
			}
			if (isAppending)
			{
				foreach (EncyclopediaSearchResultVM encyclopediaSearchResultVM in this.SearchResults.ToList<EncyclopediaSearchResultVM>())
				{
					if (StringHelpers.RemoveDiacritics(encyclopediaSearchResultVM.OrgNameText).IndexOf(text, StringComparison.InvariantCultureIgnoreCase) == -1)
					{
						this.SearchResults.Remove(encyclopediaSearchResultVM);
					}
					else
					{
						encyclopediaSearchResultVM.UpdateSearchedText(text);
					}
				}
			}
		}

		// Token: 0x0600132F RID: 4911 RVA: 0x0004D5A8 File Offset: 0x0004B7A8
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

		// Token: 0x06001330 RID: 4912 RVA: 0x0004D5D7 File Offset: 0x0004B7D7
		public void ResetSearch()
		{
			this.SearchText = string.Empty;
		}

		// Token: 0x06001331 RID: 4913 RVA: 0x0004D5E4 File Offset: 0x0004B7E4
		public void ExecuteOnSearchActivated()
		{
			Game.Current.EventManager.TriggerEvent<OnEncyclopediaSearchActivatedEvent>(new OnEncyclopediaSearchActivatedEvent());
		}

		// Token: 0x17000643 RID: 1603
		// (get) Token: 0x06001332 RID: 4914 RVA: 0x0004D5FA File Offset: 0x0004B7FA
		// (set) Token: 0x06001333 RID: 4915 RVA: 0x0004D602 File Offset: 0x0004B802
		[DataSourceProperty]
		public bool CanSwitchTabs
		{
			get
			{
				return this._canSwitchTabs;
			}
			set
			{
				if (value != this._canSwitchTabs)
				{
					this._canSwitchTabs = value;
					base.OnPropertyChangedWithValue(value, "CanSwitchTabs");
				}
			}
		}

		// Token: 0x17000644 RID: 1604
		// (get) Token: 0x06001334 RID: 4916 RVA: 0x0004D620 File Offset: 0x0004B820
		// (set) Token: 0x06001335 RID: 4917 RVA: 0x0004D628 File Offset: 0x0004B828
		[DataSourceProperty]
		public bool IsBackEnabled
		{
			get
			{
				return this._isBackEnabled;
			}
			set
			{
				if (value != this._isBackEnabled)
				{
					this._isBackEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsBackEnabled");
				}
			}
		}

		// Token: 0x17000645 RID: 1605
		// (get) Token: 0x06001336 RID: 4918 RVA: 0x0004D646 File Offset: 0x0004B846
		// (set) Token: 0x06001337 RID: 4919 RVA: 0x0004D64E File Offset: 0x0004B84E
		[DataSourceProperty]
		public bool IsForwardEnabled
		{
			get
			{
				return this._isForwardEnabled;
			}
			set
			{
				if (value != this._isForwardEnabled)
				{
					this._isForwardEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsForwardEnabled");
				}
			}
		}

		// Token: 0x17000646 RID: 1606
		// (get) Token: 0x06001338 RID: 4920 RVA: 0x0004D66C File Offset: 0x0004B86C
		// (set) Token: 0x06001339 RID: 4921 RVA: 0x0004D674 File Offset: 0x0004B874
		[DataSourceProperty]
		public bool IsHighlightEnabled
		{
			get
			{
				return this._isHighlightEnabled;
			}
			set
			{
				if (value != this._isHighlightEnabled)
				{
					this._isHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsHighlightEnabled");
				}
			}
		}

		// Token: 0x17000647 RID: 1607
		// (get) Token: 0x0600133A RID: 4922 RVA: 0x0004D692 File Offset: 0x0004B892
		// (set) Token: 0x0600133B RID: 4923 RVA: 0x0004D69A File Offset: 0x0004B89A
		[DataSourceProperty]
		public bool IsSearchResultsShown
		{
			get
			{
				return this._isSearchResultsShown;
			}
			set
			{
				if (value != this._isSearchResultsShown)
				{
					this._isSearchResultsShown = value;
					base.OnPropertyChangedWithValue(value, "IsSearchResultsShown");
				}
			}
		}

		// Token: 0x17000648 RID: 1608
		// (get) Token: 0x0600133C RID: 4924 RVA: 0x0004D6B8 File Offset: 0x0004B8B8
		// (set) Token: 0x0600133D RID: 4925 RVA: 0x0004D6C0 File Offset: 0x0004B8C0
		[DataSourceProperty]
		public string NavBarString
		{
			get
			{
				return this._navBarString;
			}
			set
			{
				if (value != this._navBarString)
				{
					this._navBarString = value;
					base.OnPropertyChangedWithValue<string>(value, "NavBarString");
				}
			}
		}

		// Token: 0x17000649 RID: 1609
		// (get) Token: 0x0600133E RID: 4926 RVA: 0x0004D6E3 File Offset: 0x0004B8E3
		// (set) Token: 0x0600133F RID: 4927 RVA: 0x0004D6EB File Offset: 0x0004B8EB
		[DataSourceProperty]
		public string PageName
		{
			get
			{
				return this._pageName;
			}
			set
			{
				if (value != this._pageName)
				{
					this._pageName = value;
					base.OnPropertyChangedWithValue<string>(value, "PageName");
				}
			}
		}

		// Token: 0x1700064A RID: 1610
		// (get) Token: 0x06001340 RID: 4928 RVA: 0x0004D70E File Offset: 0x0004B90E
		// (set) Token: 0x06001341 RID: 4929 RVA: 0x0004D716 File Offset: 0x0004B916
		[DataSourceProperty]
		public string DoneText
		{
			get
			{
				return this._doneText;
			}
			set
			{
				if (value != this._doneText)
				{
					this._doneText = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneText");
				}
			}
		}

		// Token: 0x1700064B RID: 1611
		// (get) Token: 0x06001342 RID: 4930 RVA: 0x0004D739 File Offset: 0x0004B939
		// (set) Token: 0x06001343 RID: 4931 RVA: 0x0004D741 File Offset: 0x0004B941
		[DataSourceProperty]
		public string LeaderText
		{
			get
			{
				return this._leaderText;
			}
			set
			{
				if (value != this._leaderText)
				{
					this._leaderText = value;
					base.OnPropertyChangedWithValue<string>(value, "LeaderText");
				}
			}
		}

		// Token: 0x1700064C RID: 1612
		// (get) Token: 0x06001344 RID: 4932 RVA: 0x0004D764 File Offset: 0x0004B964
		// (set) Token: 0x06001345 RID: 4933 RVA: 0x0004D76C File Offset: 0x0004B96C
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSearchResultVM> SearchResults
		{
			get
			{
				return this._searchResults;
			}
			set
			{
				if (value != this._searchResults)
				{
					this._searchResults = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSearchResultVM>>(value, "SearchResults");
				}
			}
		}

		// Token: 0x1700064D RID: 1613
		// (get) Token: 0x06001346 RID: 4934 RVA: 0x0004D78A File Offset: 0x0004B98A
		// (set) Token: 0x06001347 RID: 4935 RVA: 0x0004D794 File Offset: 0x0004B994
		[DataSourceProperty]
		public string SearchText
		{
			get
			{
				return this._searchText;
			}
			set
			{
				if (value != this._searchText)
				{
					bool isAppending = value.ToLower().Contains(this._searchText);
					bool isPasted = string.IsNullOrEmpty(this._searchText) && !string.IsNullOrEmpty(value);
					this._searchText = value.ToLower();
					Debug.Print("isAppending: " + isAppending.ToString() + " isPasted: " + isPasted.ToString(), 0, Debug.DebugColor.White, 17592186044416UL);
					this.RefreshSearch(isAppending, isPasted);
					base.OnPropertyChangedWithValue<string>(value, "SearchText");
				}
			}
		}

		// Token: 0x1700064E RID: 1614
		// (get) Token: 0x06001348 RID: 4936 RVA: 0x0004D829 File Offset: 0x0004BA29
		// (set) Token: 0x06001349 RID: 4937 RVA: 0x0004D831 File Offset: 0x0004BA31
		[DataSourceProperty]
		public int MinCharAmountToShowResults
		{
			get
			{
				return this._minCharAmountToShowResults;
			}
			set
			{
				if (value != this._minCharAmountToShowResults)
				{
					this._minCharAmountToShowResults = value;
					base.OnPropertyChangedWithValue(value, "MinCharAmountToShowResults");
				}
			}
		}

		// Token: 0x1700064F RID: 1615
		// (get) Token: 0x0600134A RID: 4938 RVA: 0x0004D84F File Offset: 0x0004BA4F
		// (set) Token: 0x0600134B RID: 4939 RVA: 0x0004D857 File Offset: 0x0004BA57
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x17000650 RID: 1616
		// (get) Token: 0x0600134C RID: 4940 RVA: 0x0004D875 File Offset: 0x0004BA75
		// (set) Token: 0x0600134D RID: 4941 RVA: 0x0004D87D File Offset: 0x0004BA7D
		[DataSourceProperty]
		public InputKeyItemVM PreviousPageInputKey
		{
			get
			{
				return this._previousPageInputKey;
			}
			set
			{
				if (value != this._previousPageInputKey)
				{
					this._previousPageInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PreviousPageInputKey");
				}
			}
		}

		// Token: 0x17000651 RID: 1617
		// (get) Token: 0x0600134E RID: 4942 RVA: 0x0004D89B File Offset: 0x0004BA9B
		// (set) Token: 0x0600134F RID: 4943 RVA: 0x0004D8A3 File Offset: 0x0004BAA3
		[DataSourceProperty]
		public InputKeyItemVM NextPageInputKey
		{
			get
			{
				return this._nextPageInputKey;
			}
			set
			{
				if (value != this._nextPageInputKey)
				{
					this._nextPageInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "NextPageInputKey");
				}
			}
		}

		// Token: 0x06001350 RID: 4944 RVA: 0x0004D8C1 File Offset: 0x0004BAC1
		public void SetCancelInputKey(HotKey hotkey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06001351 RID: 4945 RVA: 0x0004D8D0 File Offset: 0x0004BAD0
		public void SetPreviousPageInputKey(HotKey hotkey)
		{
			this.PreviousPageInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06001352 RID: 4946 RVA: 0x0004D8DF File Offset: 0x0004BADF
		public void SetNextPageInputKey(HotKey hotkey)
		{
			this.NextPageInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x040008C4 RID: 2244
		private List<Tuple<string, object>> History;

		// Token: 0x040008C5 RID: 2245
		private int HistoryIndex;

		// Token: 0x040008C6 RID: 2246
		private readonly Func<string, object, bool, EncyclopediaPageVM> _goToLink;

		// Token: 0x040008C7 RID: 2247
		private readonly Action _closeEncyclopedia;

		// Token: 0x040008C8 RID: 2248
		private EncyclopediaNavigatorVM.SearchResultComparer _searchResultComparer;

		// Token: 0x040008C9 RID: 2249
		private MBBindingList<EncyclopediaSearchResultVM> _searchResults;

		// Token: 0x040008CA RID: 2250
		private string _searchText = "";

		// Token: 0x040008CB RID: 2251
		private string _pageName;

		// Token: 0x040008CC RID: 2252
		private string _doneText;

		// Token: 0x040008CD RID: 2253
		private string _leaderText;

		// Token: 0x040008CE RID: 2254
		private bool _canSwitchTabs;

		// Token: 0x040008CF RID: 2255
		private bool _isBackEnabled;

		// Token: 0x040008D0 RID: 2256
		private bool _isForwardEnabled;

		// Token: 0x040008D1 RID: 2257
		private bool _isHighlightEnabled;

		// Token: 0x040008D2 RID: 2258
		private bool _isSearchResultsShown;

		// Token: 0x040008D3 RID: 2259
		private string _navBarString;

		// Token: 0x040008D4 RID: 2260
		private int _minCharAmountToShowResults;

		// Token: 0x040008D5 RID: 2261
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x040008D6 RID: 2262
		private InputKeyItemVM _previousPageInputKey;

		// Token: 0x040008D7 RID: 2263
		private InputKeyItemVM _nextPageInputKey;

		// Token: 0x02000239 RID: 569
		private class SearchResultComparer : IComparer<EncyclopediaSearchResultVM>
		{
			// Token: 0x17000B9D RID: 2973
			// (get) Token: 0x060024B8 RID: 9400 RVA: 0x0007FF8B File Offset: 0x0007E18B
			// (set) Token: 0x060024B9 RID: 9401 RVA: 0x0007FF93 File Offset: 0x0007E193
			public string SearchText
			{
				get
				{
					return this._searchText;
				}
				set
				{
					if (value != this._searchText)
					{
						this._searchText = value;
					}
				}
			}

			// Token: 0x060024BA RID: 9402 RVA: 0x0007FFAA File Offset: 0x0007E1AA
			public SearchResultComparer(string searchText)
			{
				this.SearchText = searchText;
			}

			// Token: 0x060024BB RID: 9403 RVA: 0x0007FFBC File Offset: 0x0007E1BC
			private int CompareBasedOnCapitalization(EncyclopediaSearchResultVM x, EncyclopediaSearchResultVM y)
			{
				int num = ((x.NameText.Length > 0 && char.IsUpper(x.NameText[0])) ? 1 : (-1));
				int value = ((y.NameText.Length > 0 && char.IsUpper(y.NameText[0])) ? 1 : (-1));
				return num.CompareTo(value);
			}

			// Token: 0x060024BC RID: 9404 RVA: 0x00080020 File Offset: 0x0007E220
			public int Compare(EncyclopediaSearchResultVM x, EncyclopediaSearchResultVM y)
			{
				if (x.MatchStartIndex != y.MatchStartIndex)
				{
					return y.MatchStartIndex.CompareTo(x.MatchStartIndex);
				}
				int num = this.CompareBasedOnCapitalization(x, y);
				if (num == 0)
				{
					return y.NameText.Length.CompareTo(x.NameText.Length);
				}
				return num;
			}

			// Token: 0x0400121C RID: 4636
			private string _searchText;
		}
	}
}
