using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000DE RID: 222
	public class EncyclopediaListSortControllerVM : ViewModel
	{
		// Token: 0x0600151C RID: 5404 RVA: 0x00053320 File Offset: 0x00051520
		public EncyclopediaListSortControllerVM(EncyclopediaPage page, MBBindingList<EncyclopediaListItemVM> items)
		{
			this._page = page;
			this._items = items;
			this.UpdateSortItemsFromPage(page);
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x0600151D RID: 5405 RVA: 0x0005336E File Offset: 0x0005156E
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
		{
			this.IsHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaSortButton";
		}

		// Token: 0x0600151E RID: 5406 RVA: 0x00053388 File Offset: 0x00051588
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameLabel = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.SortByLabel = GameTexts.FindText("str_sort_by_label", null).ToString();
			this.SortedValueLabelText = this._sortedValueLabel.ToString();
		}

		// Token: 0x0600151F RID: 5407 RVA: 0x000533D8 File Offset: 0x000515D8
		public override void OnFinalize()
		{
			base.OnFinalize();
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x06001520 RID: 5408 RVA: 0x000533FB File Offset: 0x000515FB
		public void SetSortSelection(int index)
		{
			this.SortSelection.SelectedIndex = index;
			this.OnSortSelectionChanged(this.SortSelection);
		}

		// Token: 0x06001521 RID: 5409 RVA: 0x00053418 File Offset: 0x00051618
		private void UpdateSortItemsFromPage(EncyclopediaPage page)
		{
			this.SortSelection = new EncyclopediaListSelectorVM(0, new Action<SelectorVM<EncyclopediaListSelectorItemVM>>(this.OnSortSelectionChanged), new Action(this.OnSortSelectionActivated));
			foreach (EncyclopediaSortController sortController in page.GetSortControllers())
			{
				EncyclopediaListItemComparer comparer = new EncyclopediaListItemComparer(sortController);
				this.SortSelection.AddItem(new EncyclopediaListSelectorItemVM(comparer));
			}
		}

		// Token: 0x06001522 RID: 5410 RVA: 0x00053498 File Offset: 0x00051698
		private void UpdateAlternativeSortState(EncyclopediaListItemComparerBase comparer)
		{
			CampaignUIHelper.SortState alternativeSortState = (comparer.IsAscending ? CampaignUIHelper.SortState.Ascending : CampaignUIHelper.SortState.Descending);
			this.AlternativeSortState = (int)alternativeSortState;
		}

		// Token: 0x06001523 RID: 5411 RVA: 0x000534BC File Offset: 0x000516BC
		private void OnSortSelectionChanged(SelectorVM<EncyclopediaListSelectorItemVM> s)
		{
			EncyclopediaListItemComparer comparer = s.SelectedItem.Comparer;
			comparer.SortController.Comparer.SetDefaultSortOrder();
			this._items.Sort(comparer);
			this._items.ApplyActionOnAllItems(delegate(EncyclopediaListItemVM x)
			{
				x.SetComparedValue(comparer.SortController.Comparer);
			});
			this._sortedValueLabel = comparer.SortController.Name;
			this.SortedValueLabelText = this._sortedValueLabel.ToString();
			this.IsAlternativeSortVisible = this.SortSelection.SelectedIndex != 0;
			this.UpdateAlternativeSortState(comparer.SortController.Comparer);
		}

		// Token: 0x06001524 RID: 5412 RVA: 0x00053570 File Offset: 0x00051770
		public void ExecuteSwitchSortOrder()
		{
			EncyclopediaListItemComparer comparer = this.SortSelection.SelectedItem.Comparer;
			comparer.SortController.Comparer.SwitchSortOrder();
			this._items.Sort(comparer);
			this.UpdateAlternativeSortState(comparer.SortController.Comparer);
		}

		// Token: 0x06001525 RID: 5413 RVA: 0x000535BC File Offset: 0x000517BC
		public void SetSortOrder(bool isAscending)
		{
			EncyclopediaListItemComparer comparer = this.SortSelection.SelectedItem.Comparer;
			if (comparer.SortController.Comparer.IsAscending != isAscending)
			{
				comparer.SortController.Comparer.SetSortOrder(isAscending);
				this._items.Sort(comparer);
				this.UpdateAlternativeSortState(comparer.SortController.Comparer);
			}
		}

		// Token: 0x06001526 RID: 5414 RVA: 0x0005361B File Offset: 0x0005181B
		public bool GetSortOrder()
		{
			return this.SortSelection.SelectedItem.Comparer.SortController.Comparer.IsAscending;
		}

		// Token: 0x06001527 RID: 5415 RVA: 0x0005363C File Offset: 0x0005183C
		private void OnSortSelectionActivated()
		{
			Game.Current.EventManager.TriggerEvent<OnEncyclopediaListSortedEvent>(new OnEncyclopediaListSortedEvent());
		}

		// Token: 0x17000700 RID: 1792
		// (get) Token: 0x06001528 RID: 5416 RVA: 0x00053652 File Offset: 0x00051852
		// (set) Token: 0x06001529 RID: 5417 RVA: 0x0005365A File Offset: 0x0005185A
		[DataSourceProperty]
		public EncyclopediaListSelectorVM SortSelection
		{
			get
			{
				return this._sortSelection;
			}
			set
			{
				if (value != this._sortSelection)
				{
					this._sortSelection = value;
					base.OnPropertyChangedWithValue<EncyclopediaListSelectorVM>(value, "SortSelection");
				}
			}
		}

		// Token: 0x17000701 RID: 1793
		// (get) Token: 0x0600152A RID: 5418 RVA: 0x00053678 File Offset: 0x00051878
		// (set) Token: 0x0600152B RID: 5419 RVA: 0x00053680 File Offset: 0x00051880
		[DataSourceProperty]
		public string NameLabel
		{
			get
			{
				return this._nameLabel;
			}
			set
			{
				if (value != this._nameLabel)
				{
					this._nameLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "NameLabel");
				}
			}
		}

		// Token: 0x17000702 RID: 1794
		// (get) Token: 0x0600152C RID: 5420 RVA: 0x000536A3 File Offset: 0x000518A3
		// (set) Token: 0x0600152D RID: 5421 RVA: 0x000536AB File Offset: 0x000518AB
		[DataSourceProperty]
		public string SortedValueLabelText
		{
			get
			{
				return this._sortedValueLabelText;
			}
			set
			{
				if (value != this._sortedValueLabelText)
				{
					this._sortedValueLabelText = value;
					base.OnPropertyChangedWithValue<string>(value, "SortedValueLabelText");
				}
			}
		}

		// Token: 0x17000703 RID: 1795
		// (get) Token: 0x0600152E RID: 5422 RVA: 0x000536CE File Offset: 0x000518CE
		// (set) Token: 0x0600152F RID: 5423 RVA: 0x000536D6 File Offset: 0x000518D6
		[DataSourceProperty]
		public string SortByLabel
		{
			get
			{
				return this._sortByLabel;
			}
			set
			{
				if (value != this._sortByLabel)
				{
					this._sortByLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "SortByLabel");
				}
			}
		}

		// Token: 0x17000704 RID: 1796
		// (get) Token: 0x06001530 RID: 5424 RVA: 0x000536F9 File Offset: 0x000518F9
		// (set) Token: 0x06001531 RID: 5425 RVA: 0x00053701 File Offset: 0x00051901
		[DataSourceProperty]
		public int AlternativeSortState
		{
			get
			{
				return this._alternativeSortState;
			}
			set
			{
				if (value != this._alternativeSortState)
				{
					this._alternativeSortState = value;
					base.OnPropertyChangedWithValue(value, "AlternativeSortState");
				}
			}
		}

		// Token: 0x17000705 RID: 1797
		// (get) Token: 0x06001532 RID: 5426 RVA: 0x0005371F File Offset: 0x0005191F
		// (set) Token: 0x06001533 RID: 5427 RVA: 0x00053727 File Offset: 0x00051927
		[DataSourceProperty]
		public bool IsAlternativeSortVisible
		{
			get
			{
				return this._isAlternativeSortVisible;
			}
			set
			{
				if (value != this._isAlternativeSortVisible)
				{
					this._isAlternativeSortVisible = value;
					base.OnPropertyChangedWithValue(value, "IsAlternativeSortVisible");
				}
			}
		}

		// Token: 0x17000706 RID: 1798
		// (get) Token: 0x06001534 RID: 5428 RVA: 0x00053745 File Offset: 0x00051945
		// (set) Token: 0x06001535 RID: 5429 RVA: 0x0005374D File Offset: 0x0005194D
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

		// Token: 0x040009A3 RID: 2467
		private TextObject _sortedValueLabel = TextObject.GetEmpty();

		// Token: 0x040009A4 RID: 2468
		private MBBindingList<EncyclopediaListItemVM> _items;

		// Token: 0x040009A5 RID: 2469
		private EncyclopediaPage _page;

		// Token: 0x040009A6 RID: 2470
		private EncyclopediaListSelectorVM _sortSelection;

		// Token: 0x040009A7 RID: 2471
		private string _nameLabel;

		// Token: 0x040009A8 RID: 2472
		private string _sortedValueLabelText;

		// Token: 0x040009A9 RID: 2473
		private string _sortByLabel;

		// Token: 0x040009AA RID: 2474
		private int _alternativeSortState;

		// Token: 0x040009AB RID: 2475
		private bool _isAlternativeSortVisible;

		// Token: 0x040009AC RID: 2476
		private bool _isHighlightEnabled;
	}
}
