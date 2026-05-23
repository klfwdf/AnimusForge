using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar
{
	// Token: 0x0200005E RID: 94
	public class MapNavigationVM : ViewModel
	{
		// Token: 0x060006C1 RID: 1729 RVA: 0x00021AD4 File Offset: 0x0001FCD4
		public MapNavigationVM(INavigationHandler navigationHandler, Func<MapBarShortcuts> getMapBarShortcuts)
		{
			this._navigationHandler = navigationHandler;
			this._getMapBarShortcuts = getMapBarShortcuts;
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this.NavigationItems = new MBBindingList<MapNavigationItemVM>();
			INavigationElement[] elements = navigationHandler.GetElements();
			for (int i = 0; i < elements.Length; i++)
			{
				this.NavigationItems.Add(new MapNavigationItemVM(elements[i]));
			}
			this.RefreshValues();
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x00021B40 File Offset: 0x0001FD40
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._shortcuts = this._getMapBarShortcuts();
			this.EncyclopediaHint = new HintViewModel(GameTexts.FindText("str_encyclopedia", null), null);
			this.CampHint = new HintViewModel(GameTexts.FindText("str_camp", null), null);
			this.FinanceHint = new HintViewModel(GameTexts.FindText("str_finance", null), null);
			this.CenterCameraHint = new HintViewModel(GameTexts.FindText("str_return_to_hero", null), null);
			this.Refresh();
			this.NavigationItems.ApplyActionOnAllItems(delegate(MapNavigationItemVM n)
			{
				n.RefreshValues();
			});
		}

		// Token: 0x060006C3 RID: 1731 RVA: 0x00021BF0 File Offset: 0x0001FDF0
		public override void OnFinalize()
		{
			base.OnFinalize();
			this._navigationHandler = null;
			this._getMapBarShortcuts = null;
			this.NavigationItems.ApplyActionOnAllItems(delegate(MapNavigationItemVM n)
			{
				n.OnFinalize();
			});
		}

		// Token: 0x060006C4 RID: 1732 RVA: 0x00021C30 File Offset: 0x0001FE30
		public void Refresh()
		{
			this.RefreshStates();
			this._viewDataTracker.UpdatePartyNotification();
		}

		// Token: 0x060006C5 RID: 1733 RVA: 0x00021C43 File Offset: 0x0001FE43
		public void Tick()
		{
			this.RefreshStates();
		}

		// Token: 0x060006C6 RID: 1734 RVA: 0x00021C4B File Offset: 0x0001FE4B
		protected virtual void RefreshStates()
		{
			this.NavigationItems.ApplyActionOnAllItems(delegate(MapNavigationItemVM n)
			{
				n.RefreshStates(false);
			});
		}

		// Token: 0x060006C7 RID: 1735 RVA: 0x00021C77 File Offset: 0x0001FE77
		public void ExecuteOpenQuests()
		{
			this._navigationHandler.OpenQuests();
		}

		// Token: 0x060006C8 RID: 1736 RVA: 0x00021C84 File Offset: 0x0001FE84
		public void ExecuteOpenInventory()
		{
			this._navigationHandler.OpenInventory();
		}

		// Token: 0x060006C9 RID: 1737 RVA: 0x00021C91 File Offset: 0x0001FE91
		public void ExecuteOpenParty()
		{
			this._navigationHandler.OpenParty();
		}

		// Token: 0x060006CA RID: 1738 RVA: 0x00021C9E File Offset: 0x0001FE9E
		public void ExecuteOpenCharacterDeveloper()
		{
			this._navigationHandler.OpenCharacterDeveloper();
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x00021CAB File Offset: 0x0001FEAB
		public void ExecuteOpenKingdom()
		{
			this._navigationHandler.OpenKingdom();
		}

		// Token: 0x060006CC RID: 1740 RVA: 0x00021CB8 File Offset: 0x0001FEB8
		public void ExecuteOpenClan()
		{
			this._navigationHandler.OpenClan();
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x00021CC5 File Offset: 0x0001FEC5
		public void ExecuteOpenEscapeMenu()
		{
			this._navigationHandler.OpenEscapeMenu();
		}

		// Token: 0x060006CE RID: 1742 RVA: 0x00021CD2 File Offset: 0x0001FED2
		public void ExecuteOpenMainHeroKingdomEncyclopedia()
		{
			if (Hero.MainHero.MapFaction != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.MapFaction.EncyclopediaLink);
			}
		}

		// Token: 0x170001D2 RID: 466
		// (get) Token: 0x060006CF RID: 1743 RVA: 0x00021CFE File Offset: 0x0001FEFE
		// (set) Token: 0x060006D0 RID: 1744 RVA: 0x00021D06 File Offset: 0x0001FF06
		[DataSourceProperty]
		public MBBindingList<MapNavigationItemVM> NavigationItems
		{
			get
			{
				return this._navigationItems;
			}
			set
			{
				if (value != this._navigationItems)
				{
					this._navigationItems = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapNavigationItemVM>>(value, "NavigationItems");
				}
			}
		}

		// Token: 0x170001D3 RID: 467
		// (get) Token: 0x060006D1 RID: 1745 RVA: 0x00021D24 File Offset: 0x0001FF24
		// (set) Token: 0x060006D2 RID: 1746 RVA: 0x00021D2C File Offset: 0x0001FF2C
		[DataSourceProperty]
		public HintViewModel FinanceHint
		{
			get
			{
				return this._financeHint;
			}
			set
			{
				if (value != this._financeHint)
				{
					this._financeHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FinanceHint");
				}
			}
		}

		// Token: 0x170001D4 RID: 468
		// (get) Token: 0x060006D3 RID: 1747 RVA: 0x00021D4A File Offset: 0x0001FF4A
		// (set) Token: 0x060006D4 RID: 1748 RVA: 0x00021D52 File Offset: 0x0001FF52
		[DataSourceProperty]
		public HintViewModel EncyclopediaHint
		{
			get
			{
				return this._encyclopediaHint;
			}
			set
			{
				if (value != this._encyclopediaHint)
				{
					this._encyclopediaHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EncyclopediaHint");
				}
			}
		}

		// Token: 0x170001D5 RID: 469
		// (get) Token: 0x060006D5 RID: 1749 RVA: 0x00021D70 File Offset: 0x0001FF70
		// (set) Token: 0x060006D6 RID: 1750 RVA: 0x00021D78 File Offset: 0x0001FF78
		[DataSourceProperty]
		public HintViewModel CenterCameraHint
		{
			get
			{
				return this._centerCameraHint;
			}
			set
			{
				if (value != this._centerCameraHint)
				{
					this._centerCameraHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CenterCameraHint");
				}
			}
		}

		// Token: 0x170001D6 RID: 470
		// (get) Token: 0x060006D7 RID: 1751 RVA: 0x00021D96 File Offset: 0x0001FF96
		// (set) Token: 0x060006D8 RID: 1752 RVA: 0x00021D9E File Offset: 0x0001FF9E
		[DataSourceProperty]
		public HintViewModel CampHint
		{
			get
			{
				return this._campHint;
			}
			set
			{
				if (value != this._campHint)
				{
					this._campHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CampHint");
				}
			}
		}

		// Token: 0x040002EC RID: 748
		protected INavigationHandler _navigationHandler;

		// Token: 0x040002ED RID: 749
		protected Func<MapBarShortcuts> _getMapBarShortcuts;

		// Token: 0x040002EE RID: 750
		protected MapBarShortcuts _shortcuts;

		// Token: 0x040002EF RID: 751
		protected readonly IViewDataTracker _viewDataTracker;

		// Token: 0x040002F0 RID: 752
		private MBBindingList<MapNavigationItemVM> _navigationItems;

		// Token: 0x040002F1 RID: 753
		private HintViewModel _encyclopediaHint;

		// Token: 0x040002F2 RID: 754
		private HintViewModel _financeHint;

		// Token: 0x040002F3 RID: 755
		private HintViewModel _centerCameraHint;

		// Token: 0x040002F4 RID: 756
		private HintViewModel _campHint;
	}
}
