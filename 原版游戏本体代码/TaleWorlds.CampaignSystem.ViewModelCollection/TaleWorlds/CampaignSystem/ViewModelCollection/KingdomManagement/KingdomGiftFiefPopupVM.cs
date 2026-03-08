using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement
{
	// Token: 0x02000063 RID: 99
	public class KingdomGiftFiefPopupVM : ViewModel
	{
		// Token: 0x06000760 RID: 1888 RVA: 0x000231EA File Offset: 0x000213EA
		public KingdomGiftFiefPopupVM(Action onSettlementGranted)
		{
			this._clans = new MBBindingList<KingdomClanItemVM>();
			this._onSettlementGranted = onSettlementGranted;
			this.ClanSortController = new KingdomClanSortControllerVM(ref this._clans);
			this.RefreshValues();
		}

		// Token: 0x06000761 RID: 1889 RVA: 0x0002321C File Offset: 0x0002141C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = new TextObject("{=rOKAvjtT}Gift Settlement", null).ToString();
			this.GiftText = GameTexts.FindText("str_gift", null).ToString();
			this.CancelText = GameTexts.FindText("str_cancel", null).ToString();
			this.NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
			this.InfluenceText = GameTexts.FindText("str_influence", null).ToString();
			this.FiefsText = GameTexts.FindText("str_fiefs", null).ToString();
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.BannerText = GameTexts.FindText("str_banner", null).ToString();
			this.TypeText = GameTexts.FindText("str_sort_by_type_label", null).ToString();
		}

		// Token: 0x06000762 RID: 1890 RVA: 0x000232F9 File Offset: 0x000214F9
		private void SetCurrentSelectedClan(KingdomClanItemVM clan)
		{
			if (clan != this.CurrentSelectedClan)
			{
				if (this.CurrentSelectedClan != null)
				{
					this.CurrentSelectedClan.IsSelected = false;
				}
				this.CurrentSelectedClan = clan;
				this.CurrentSelectedClan.IsSelected = true;
				this.IsAnyClanSelected = true;
			}
		}

		// Token: 0x06000763 RID: 1891 RVA: 0x00023334 File Offset: 0x00021534
		private void RefreshClanList()
		{
			this.Clans.Clear();
			foreach (Clan clan in Clan.PlayerClan.Kingdom.Clans)
			{
				if (FactionHelper.CanClanBeGrantedFief(clan))
				{
					this.Clans.Add(new KingdomClanItemVM(clan, new Action<KingdomClanItemVM>(this.SetCurrentSelectedClan)));
				}
			}
			if (this.Clans.Count > 0)
			{
				this.SetCurrentSelectedClan(this.Clans[0]);
			}
			if (this.ClanSortController != null)
			{
				this.ClanSortController.SortByCurrentState();
			}
		}

		// Token: 0x06000764 RID: 1892 RVA: 0x000233EC File Offset: 0x000215EC
		public void OpenWith(Settlement settlement)
		{
			this._settlementToGive = settlement;
			this.RefreshClanList();
			this.IsOpen = true;
		}

		// Token: 0x06000765 RID: 1893 RVA: 0x00023404 File Offset: 0x00021604
		public void ExecuteGiftSettlement()
		{
			if (this._settlementToGive != null && this.CurrentSelectedClan != null)
			{
				Campaign.Current.KingdomManager.GiftSettlementOwnership(this._settlementToGive, this.CurrentSelectedClan.Clan);
				this.ExecuteClose();
				this._onSettlementGranted();
			}
		}

		// Token: 0x06000766 RID: 1894 RVA: 0x00023452 File Offset: 0x00021652
		public void ExecuteClose()
		{
			this._settlementToGive = null;
			this.IsOpen = false;
		}

		// Token: 0x06000767 RID: 1895 RVA: 0x00023462 File Offset: 0x00021662
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey == null)
			{
				return;
			}
			cancelInputKey.OnFinalize();
		}

		// Token: 0x06000768 RID: 1896 RVA: 0x0002348B File Offset: 0x0002168B
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000769 RID: 1897 RVA: 0x0002349A File Offset: 0x0002169A
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700020C RID: 524
		// (get) Token: 0x0600076A RID: 1898 RVA: 0x000234A9 File Offset: 0x000216A9
		// (set) Token: 0x0600076B RID: 1899 RVA: 0x000234B1 File Offset: 0x000216B1
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x1700020D RID: 525
		// (get) Token: 0x0600076C RID: 1900 RVA: 0x000234CF File Offset: 0x000216CF
		// (set) Token: 0x0600076D RID: 1901 RVA: 0x000234D7 File Offset: 0x000216D7
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

		// Token: 0x1700020E RID: 526
		// (get) Token: 0x0600076E RID: 1902 RVA: 0x000234F5 File Offset: 0x000216F5
		// (set) Token: 0x0600076F RID: 1903 RVA: 0x000234FD File Offset: 0x000216FD
		[DataSourceProperty]
		public bool IsAnyClanSelected
		{
			get
			{
				return this._isAnyClanSelected;
			}
			set
			{
				if (value != this._isAnyClanSelected)
				{
					this._isAnyClanSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyClanSelected");
				}
			}
		}

		// Token: 0x1700020F RID: 527
		// (get) Token: 0x06000770 RID: 1904 RVA: 0x0002351B File Offset: 0x0002171B
		// (set) Token: 0x06000771 RID: 1905 RVA: 0x00023523 File Offset: 0x00021723
		[DataSourceProperty]
		public MBBindingList<KingdomClanItemVM> Clans
		{
			get
			{
				return this._clans;
			}
			set
			{
				if (value != this._clans)
				{
					this._clans = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomClanItemVM>>(value, "Clans");
				}
			}
		}

		// Token: 0x17000210 RID: 528
		// (get) Token: 0x06000772 RID: 1906 RVA: 0x00023541 File Offset: 0x00021741
		// (set) Token: 0x06000773 RID: 1907 RVA: 0x00023549 File Offset: 0x00021749
		[DataSourceProperty]
		public KingdomClanItemVM CurrentSelectedClan
		{
			get
			{
				return this._currentSelectedClan;
			}
			set
			{
				if (value != this._currentSelectedClan)
				{
					this._currentSelectedClan = value;
					base.OnPropertyChangedWithValue<KingdomClanItemVM>(value, "CurrentSelectedClan");
				}
			}
		}

		// Token: 0x17000211 RID: 529
		// (get) Token: 0x06000774 RID: 1908 RVA: 0x00023567 File Offset: 0x00021767
		// (set) Token: 0x06000775 RID: 1909 RVA: 0x0002356F File Offset: 0x0002176F
		[DataSourceProperty]
		public KingdomClanSortControllerVM ClanSortController
		{
			get
			{
				return this._clanSortController;
			}
			set
			{
				if (value != this._clanSortController)
				{
					this._clanSortController = value;
					base.OnPropertyChangedWithValue<KingdomClanSortControllerVM>(value, "ClanSortController");
				}
			}
		}

		// Token: 0x17000212 RID: 530
		// (get) Token: 0x06000776 RID: 1910 RVA: 0x0002358D File Offset: 0x0002178D
		// (set) Token: 0x06000777 RID: 1911 RVA: 0x00023595 File Offset: 0x00021795
		[DataSourceProperty]
		public bool IsOpen
		{
			get
			{
				return this._isOpen;
			}
			set
			{
				if (value != this._isOpen)
				{
					this._isOpen = value;
					base.OnPropertyChangedWithValue(value, "IsOpen");
				}
			}
		}

		// Token: 0x17000213 RID: 531
		// (get) Token: 0x06000778 RID: 1912 RVA: 0x000235B3 File Offset: 0x000217B3
		// (set) Token: 0x06000779 RID: 1913 RVA: 0x000235BB File Offset: 0x000217BB
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x17000214 RID: 532
		// (get) Token: 0x0600077A RID: 1914 RVA: 0x000235DE File Offset: 0x000217DE
		// (set) Token: 0x0600077B RID: 1915 RVA: 0x000235E6 File Offset: 0x000217E6
		[DataSourceProperty]
		public string GiftText
		{
			get
			{
				return this._giftText;
			}
			set
			{
				if (value != this._giftText)
				{
					this._giftText = value;
					base.OnPropertyChangedWithValue<string>(value, "GiftText");
				}
			}
		}

		// Token: 0x17000215 RID: 533
		// (get) Token: 0x0600077C RID: 1916 RVA: 0x00023609 File Offset: 0x00021809
		// (set) Token: 0x0600077D RID: 1917 RVA: 0x00023611 File Offset: 0x00021811
		[DataSourceProperty]
		public string CancelText
		{
			get
			{
				return this._cancelText;
			}
			set
			{
				if (value != this._cancelText)
				{
					this._cancelText = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelText");
				}
			}
		}

		// Token: 0x17000216 RID: 534
		// (get) Token: 0x0600077E RID: 1918 RVA: 0x00023634 File Offset: 0x00021834
		// (set) Token: 0x0600077F RID: 1919 RVA: 0x0002363C File Offset: 0x0002183C
		[DataSourceProperty]
		public string BannerText
		{
			get
			{
				return this._bannerText;
			}
			set
			{
				if (value != this._bannerText)
				{
					this._bannerText = value;
					base.OnPropertyChangedWithValue<string>(value, "BannerText");
				}
			}
		}

		// Token: 0x17000217 RID: 535
		// (get) Token: 0x06000780 RID: 1920 RVA: 0x0002365F File Offset: 0x0002185F
		// (set) Token: 0x06000781 RID: 1921 RVA: 0x00023667 File Offset: 0x00021867
		[DataSourceProperty]
		public string TypeText
		{
			get
			{
				return this._typeText;
			}
			set
			{
				if (value != this._typeText)
				{
					this._typeText = value;
					base.OnPropertyChangedWithValue<string>(value, "TypeText");
				}
			}
		}

		// Token: 0x17000218 RID: 536
		// (get) Token: 0x06000782 RID: 1922 RVA: 0x0002368A File Offset: 0x0002188A
		// (set) Token: 0x06000783 RID: 1923 RVA: 0x00023692 File Offset: 0x00021892
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x17000219 RID: 537
		// (get) Token: 0x06000784 RID: 1924 RVA: 0x000236B5 File Offset: 0x000218B5
		// (set) Token: 0x06000785 RID: 1925 RVA: 0x000236BD File Offset: 0x000218BD
		[DataSourceProperty]
		public string InfluenceText
		{
			get
			{
				return this._influenceText;
			}
			set
			{
				if (value != this._influenceText)
				{
					this._influenceText = value;
					base.OnPropertyChangedWithValue<string>(value, "InfluenceText");
				}
			}
		}

		// Token: 0x1700021A RID: 538
		// (get) Token: 0x06000786 RID: 1926 RVA: 0x000236E0 File Offset: 0x000218E0
		// (set) Token: 0x06000787 RID: 1927 RVA: 0x000236E8 File Offset: 0x000218E8
		[DataSourceProperty]
		public string FiefsText
		{
			get
			{
				return this._fiefsText;
			}
			set
			{
				if (value != this._fiefsText)
				{
					this._fiefsText = value;
					base.OnPropertyChangedWithValue<string>(value, "FiefsText");
				}
			}
		}

		// Token: 0x1700021B RID: 539
		// (get) Token: 0x06000788 RID: 1928 RVA: 0x0002370B File Offset: 0x0002190B
		// (set) Token: 0x06000789 RID: 1929 RVA: 0x00023713 File Offset: 0x00021913
		[DataSourceProperty]
		public string MembersText
		{
			get
			{
				return this._membersText;
			}
			set
			{
				if (value != this._membersText)
				{
					this._membersText = value;
					base.OnPropertyChangedWithValue<string>(value, "MembersText");
				}
			}
		}

		// Token: 0x04000331 RID: 817
		private Settlement _settlementToGive;

		// Token: 0x04000332 RID: 818
		private Action _onSettlementGranted;

		// Token: 0x04000333 RID: 819
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000334 RID: 820
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000335 RID: 821
		private bool _isAnyClanSelected;

		// Token: 0x04000336 RID: 822
		private MBBindingList<KingdomClanItemVM> _clans;

		// Token: 0x04000337 RID: 823
		private KingdomClanItemVM _currentSelectedClan;

		// Token: 0x04000338 RID: 824
		private KingdomClanSortControllerVM _clanSortController;

		// Token: 0x04000339 RID: 825
		private bool _isOpen;

		// Token: 0x0400033A RID: 826
		private string _titleText;

		// Token: 0x0400033B RID: 827
		private string _giftText;

		// Token: 0x0400033C RID: 828
		private string _cancelText;

		// Token: 0x0400033D RID: 829
		private string _bannerText;

		// Token: 0x0400033E RID: 830
		private string _nameText;

		// Token: 0x0400033F RID: 831
		private string _influenceText;

		// Token: 0x04000340 RID: 832
		private string _membersText;

		// Token: 0x04000341 RID: 833
		private string _fiefsText;

		// Token: 0x04000342 RID: 834
		private string _typeText;
	}
}
