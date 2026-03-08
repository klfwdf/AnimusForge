using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000E2 RID: 226
	public class EncyclopediaListVM : EncyclopediaPageVM
	{
		// Token: 0x0600153D RID: 5437 RVA: 0x00053824 File Offset: 0x00051A24
		public EncyclopediaListVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this.Page = base.Obj as EncyclopediaPage;
			this.Items = new MBBindingList<EncyclopediaListItemVM>();
			this.FilterGroups = new MBBindingList<EncyclopediaFilterGroupVM>();
			this.SortController = new EncyclopediaListSortControllerVM(this.Page, this.Items);
			this.IsInitializationOver = true;
			foreach (EncyclopediaFilterGroup filterGroup in this.Page.GetFilterItems())
			{
				this.FilterGroups.Add(new EncyclopediaFilterGroupVM(filterGroup, new Action<EncyclopediaListFilterVM>(this.UpdateFilters)));
			}
			this.IsInitializationOver = false;
			this.Items.Clear();
			foreach (EncyclopediaListItem listItem in this.Page.GetListItems())
			{
				EncyclopediaListItemVM encyclopediaListItemVM = new EncyclopediaListItemVM(listItem);
				encyclopediaListItemVM.IsFiltered = this.Page.IsFiltered(encyclopediaListItemVM.Object);
				this.Items.Add(encyclopediaListItemVM);
			}
			this.RefreshValues();
			this.IsInitializationOver = true;
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x0600153E RID: 5438 RVA: 0x00053974 File Offset: 0x00051B74
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
		{
			this.IsFilterHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaFiltersContainer";
		}

		// Token: 0x0600153F RID: 5439 RVA: 0x0005398C File Offset: 0x00051B8C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SortController.RefreshValues();
			this.EmptyListText = GameTexts.FindText("str_encyclopedia_empty_list_error", null).ToString();
			this.Items.ApplyActionOnAllItems(delegate(EncyclopediaListItemVM x)
			{
				x.RefreshValues();
			});
			this.FilterGroups.ApplyActionOnAllItems(delegate(EncyclopediaFilterGroupVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06001540 RID: 5440 RVA: 0x00053A14 File Offset: 0x00051C14
		public override void OnFinalize()
		{
			base.OnFinalize();
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			EncyclopediaListSortControllerVM sortController = this.SortController;
			if (sortController != null)
			{
				sortController.OnFinalize();
			}
			this.SortController = null;
			this.FilterGroups.ApplyActionOnAllItems(delegate(EncyclopediaFilterGroupVM x)
			{
				x.OnFinalize();
			});
			this.FilterGroups.Clear();
			this.Items.ApplyActionOnAllItems(delegate(EncyclopediaListItemVM x)
			{
				x.OnFinalize();
			});
			this.Items.Clear();
		}

		// Token: 0x06001541 RID: 5441 RVA: 0x00053AC4 File Offset: 0x00051CC4
		public override string GetName()
		{
			return this.Page.GetName().ToString();
		}

		// Token: 0x06001542 RID: 5442 RVA: 0x00053AD8 File Offset: 0x00051CD8
		public override string GetNavigationBarURL()
		{
			string text = HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ ";
			if (this.Page.HasIdentifierType(typeof(Kingdom)))
			{
				text += GameTexts.FindText("str_encyclopedia_kingdoms", null).ToString();
			}
			else if (this.Page.HasIdentifierType(typeof(Clan)))
			{
				text += GameTexts.FindText("str_encyclopedia_clans", null).ToString();
			}
			else if (this.Page.HasIdentifierType(typeof(Hero)))
			{
				text += GameTexts.FindText("str_encyclopedia_heroes", null).ToString();
			}
			else if (this.Page.HasIdentifierType(typeof(Settlement)))
			{
				text += GameTexts.FindText("str_encyclopedia_settlements", null).ToString();
			}
			else if (this.Page.HasIdentifierType(typeof(CharacterObject)))
			{
				text += GameTexts.FindText("str_encyclopedia_troops", null).ToString();
			}
			else if (this.Page.HasIdentifierType(typeof(Concept)))
			{
				text += GameTexts.FindText("str_encyclopedia_concepts", null).ToString();
			}
			else if (this.Page.HasIdentifierType(typeof(ShipHull)))
			{
				text += GameTexts.FindText("str_encyclopedia_ships", null).ToString();
			}
			return text;
		}

		// Token: 0x06001543 RID: 5443 RVA: 0x00053C68 File Offset: 0x00051E68
		private void ExecuteResetFilters()
		{
			foreach (EncyclopediaFilterGroupVM encyclopediaFilterGroupVM in this.FilterGroups)
			{
				foreach (EncyclopediaListFilterVM encyclopediaListFilterVM in encyclopediaFilterGroupVM.Filters)
				{
					encyclopediaListFilterVM.IsSelected = false;
				}
			}
		}

		// Token: 0x06001544 RID: 5444 RVA: 0x00053CE8 File Offset: 0x00051EE8
		public void CopyFiltersFrom(Dictionary<EncyclopediaFilterItem, bool> filters)
		{
			this.FilterGroups.ApplyActionOnAllItems(delegate(EncyclopediaFilterGroupVM x)
			{
				x.CopyFiltersFrom(filters);
			});
		}

		// Token: 0x06001545 RID: 5445 RVA: 0x00053D1C File Offset: 0x00051F1C
		public override void Refresh()
		{
			base.Refresh();
			foreach (EncyclopediaListItemVM encyclopediaListItemVM in this.Items)
			{
				Hero hero;
				Clan clan;
				Concept concept;
				Kingdom kingdom;
				Settlement settlement;
				CharacterObject unit;
				ShipHull shipHull;
				if ((hero = encyclopediaListItemVM.Object as Hero) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(hero);
				}
				else if ((clan = encyclopediaListItemVM.Object as Clan) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(clan);
				}
				else if ((concept = encyclopediaListItemVM.Object as Concept) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(concept);
				}
				else if ((kingdom = encyclopediaListItemVM.Object as Kingdom) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(kingdom);
				}
				else if ((settlement = encyclopediaListItemVM.Object as Settlement) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(settlement);
				}
				else if ((unit = encyclopediaListItemVM.Object as CharacterObject) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(unit);
				}
				else if ((shipHull = encyclopediaListItemVM.Object as ShipHull) != null)
				{
					encyclopediaListItemVM.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(shipHull);
				}
			}
			this._isInitializationOver = false;
			this.IsInitializationOver = true;
		}

		// Token: 0x06001546 RID: 5446 RVA: 0x00053ED0 File Offset: 0x000520D0
		private void UpdateFilters(EncyclopediaListFilterVM filterVM)
		{
			this.IsInitializationOver = false;
			foreach (EncyclopediaListItemVM encyclopediaListItemVM in this.Items)
			{
				encyclopediaListItemVM.IsFiltered = this.Page.IsFiltered(encyclopediaListItemVM.Object);
			}
			this.IsInitializationOver = true;
		}

		// Token: 0x17000708 RID: 1800
		// (get) Token: 0x06001547 RID: 5447 RVA: 0x00053F3C File Offset: 0x0005213C
		// (set) Token: 0x06001548 RID: 5448 RVA: 0x00053F44 File Offset: 0x00052144
		[DataSourceProperty]
		public string EmptyListText
		{
			get
			{
				return this._emptyListText;
			}
			set
			{
				if (value != this._emptyListText)
				{
					this._emptyListText = value;
					base.OnPropertyChangedWithValue<string>(value, "EmptyListText");
				}
			}
		}

		// Token: 0x17000709 RID: 1801
		// (get) Token: 0x06001549 RID: 5449 RVA: 0x00053F67 File Offset: 0x00052167
		// (set) Token: 0x0600154A RID: 5450 RVA: 0x00053F6F File Offset: 0x0005216F
		[DataSourceProperty]
		public string LastSelectedItemId
		{
			get
			{
				return this._lastSelectedItemId;
			}
			set
			{
				if (value != this._lastSelectedItemId)
				{
					this._lastSelectedItemId = value;
					base.OnPropertyChangedWithValue<string>(value, "LastSelectedItemId");
				}
			}
		}

		// Token: 0x1700070A RID: 1802
		// (get) Token: 0x0600154B RID: 5451 RVA: 0x00053F92 File Offset: 0x00052192
		// (set) Token: 0x0600154C RID: 5452 RVA: 0x00053F9A File Offset: 0x0005219A
		[DataSourceProperty]
		public override MBBindingList<EncyclopediaListItemVM> Items
		{
			get
			{
				return this._items;
			}
			set
			{
				if (value != this._items)
				{
					this._items = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaListItemVM>>(value, "Items");
				}
			}
		}

		// Token: 0x1700070B RID: 1803
		// (get) Token: 0x0600154D RID: 5453 RVA: 0x00053FB8 File Offset: 0x000521B8
		// (set) Token: 0x0600154E RID: 5454 RVA: 0x00053FC0 File Offset: 0x000521C0
		[DataSourceProperty]
		public override EncyclopediaListSortControllerVM SortController
		{
			get
			{
				return this._sortController;
			}
			set
			{
				if (value != this._sortController)
				{
					this._sortController = value;
					base.OnPropertyChangedWithValue<EncyclopediaListSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x1700070C RID: 1804
		// (get) Token: 0x0600154F RID: 5455 RVA: 0x00053FDE File Offset: 0x000521DE
		// (set) Token: 0x06001550 RID: 5456 RVA: 0x00053FE6 File Offset: 0x000521E6
		[DataSourceProperty]
		public bool IsInitializationOver
		{
			get
			{
				return this._isInitializationOver;
			}
			set
			{
				if (value != this._isInitializationOver)
				{
					this._isInitializationOver = value;
					base.OnPropertyChangedWithValue(value, "IsInitializationOver");
				}
			}
		}

		// Token: 0x1700070D RID: 1805
		// (get) Token: 0x06001551 RID: 5457 RVA: 0x00054004 File Offset: 0x00052204
		// (set) Token: 0x06001552 RID: 5458 RVA: 0x0005400C File Offset: 0x0005220C
		[DataSourceProperty]
		public bool IsFilterHighlightEnabled
		{
			get
			{
				return this._isFilterHighlightEnabled;
			}
			set
			{
				if (value != this._isFilterHighlightEnabled)
				{
					this._isFilterHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsFilterHighlightEnabled");
				}
			}
		}

		// Token: 0x1700070E RID: 1806
		// (get) Token: 0x06001553 RID: 5459 RVA: 0x0005402A File Offset: 0x0005222A
		// (set) Token: 0x06001554 RID: 5460 RVA: 0x00054032 File Offset: 0x00052232
		[DataSourceProperty]
		public override MBBindingList<EncyclopediaFilterGroupVM> FilterGroups
		{
			get
			{
				return this._filterGroups;
			}
			set
			{
				if (value != this._filterGroups)
				{
					this._filterGroups = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaFilterGroupVM>>(value, "FilterGroups");
				}
			}
		}

		// Token: 0x040009B0 RID: 2480
		public readonly EncyclopediaPage Page;

		// Token: 0x040009B1 RID: 2481
		private MBBindingList<EncyclopediaFilterGroupVM> _filterGroups;

		// Token: 0x040009B2 RID: 2482
		private MBBindingList<EncyclopediaListItemVM> _items;

		// Token: 0x040009B3 RID: 2483
		private EncyclopediaListSortControllerVM _sortController;

		// Token: 0x040009B4 RID: 2484
		private bool _isInitializationOver;

		// Token: 0x040009B5 RID: 2485
		private bool _isFilterHighlightEnabled;

		// Token: 0x040009B6 RID: 2486
		private string _emptyListText;

		// Token: 0x040009B7 RID: 2487
		private string _lastSelectedItemId;
	}
}
