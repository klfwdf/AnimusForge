using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements
{
	// Token: 0x02000067 RID: 103
	public class KingdomSettlementItemVM : KingdomItemVM
	{
		// Token: 0x1700023B RID: 571
		// (get) Token: 0x060007F1 RID: 2033 RVA: 0x000248FB File Offset: 0x00022AFB
		// (set) Token: 0x060007F2 RID: 2034 RVA: 0x00024903 File Offset: 0x00022B03
		public int Garrison { get; private set; }

		// Token: 0x1700023C RID: 572
		// (get) Token: 0x060007F3 RID: 2035 RVA: 0x0002490C File Offset: 0x00022B0C
		// (set) Token: 0x060007F4 RID: 2036 RVA: 0x00024914 File Offset: 0x00022B14
		public int Militia { get; private set; }

		// Token: 0x060007F5 RID: 2037 RVA: 0x00024920 File Offset: 0x00022B20
		public KingdomSettlementItemVM(Settlement settlement, Action<KingdomSettlementItemVM> onSelect)
		{
			this.Settlement = settlement;
			this._onSelect = onSelect;
			this.Name = settlement.Name.ToString();
			this.Villages = new MBBindingList<KingdomSettlementVillageItemVM>();
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			this.SettlementImagePath = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
			this.ItemProperties = new MBBindingList<SelectableFiefItemPropertyVM>();
			this.ImageName = ((settlementComponent != null) ? settlementComponent.WaitMeshName : "");
			this.Owner = new HeroVM(settlement.OwnerClan.Leader, false);
			this.OwnerClanBanner = new BannerImageIdentifierVM(this.Settlement.OwnerClan.Banner, false);
			this.OwnerClanBanner_9 = new BannerImageIdentifierVM(this.Settlement.OwnerClan.Banner, true);
			Town town = settlement.Town;
			this.WallLevel = ((town == null) ? (-1) : town.GetWallLevel());
			if (town != null)
			{
				this.Prosperity = MathF.Round(town.Prosperity);
				this.IconPath = town.BackgroundMeshName;
			}
			else if (settlement.IsCastle)
			{
				this.Prosperity = MathF.Round(settlement.Town.Prosperity);
				this.IconPath = "";
			}
			foreach (Village village in this.Settlement.BoundVillages)
			{
				this.Villages.Add(new KingdomSettlementVillageItemVM(village));
			}
			int defenders;
			if (!this.Settlement.IsFortification)
			{
				defenders = (int)this.Settlement.Militia;
			}
			else
			{
				MobileParty garrisonParty = this.Settlement.Town.GarrisonParty;
				defenders = ((garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers : 0);
			}
			this.Defenders = defenders;
			this.RefreshValues();
		}

		// Token: 0x060007F6 RID: 2038 RVA: 0x00024AF8 File Offset: 0x00022CF8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Villages.ApplyActionOnAllItems(delegate(KingdomSettlementVillageItemVM x)
			{
				x.RefreshValues();
			});
			this.UpdateProperties();
		}

		// Token: 0x060007F7 RID: 2039 RVA: 0x00024B30 File Offset: 0x00022D30
		protected virtual void UpdateProperties()
		{
			this.ItemProperties.Clear();
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(this.Settlement.Town));
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_walls", null).ToString(), this.Settlement.Town.GetWallLevel().ToString(), 0, SelectableItemPropertyVM.PropertyType.Wall, hint, false));
				BasicTooltipViewModel hint2 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(this.Settlement.Town));
				int changeAmount = (int)SettlementHelper.GetGarrisonChangeExplainedNumber(this.Settlement.Town).ResultNumber;
				Collection<SelectableFiefItemPropertyVM> itemProperties = this.ItemProperties;
				string name = GameTexts.FindText("str_garrison", null).ToString();
				MobileParty garrisonParty = this.Settlement.Town.GarrisonParty;
				itemProperties.Add(new SelectableFiefItemPropertyVM(name, ((garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers.ToString() : null) ?? "0", changeAmount, SelectableItemPropertyVM.PropertyType.Garrison, hint2, false));
			}
			int num = (int)this.Settlement.Militia;
			List<TooltipProperty> militiaHint = (this.Settlement.IsVillage ? CampaignUIHelper.GetVillageMilitiaTooltip(this.Settlement.Village) : CampaignUIHelper.GetTownMilitiaTooltip(this.Settlement.Town));
			int changeAmount2 = ((this.Settlement.Town != null) ? ((int)this.Settlement.Town.MilitiaChange) : ((int)this.Settlement.Village.MilitiaChange));
			this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_militia", null).ToString(), num.ToString(), changeAmount2, SelectableItemPropertyVM.PropertyType.Militia, new BasicTooltipViewModel(() => militiaHint), false));
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint3 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(this.Settlement.Town));
				int changeAmount3 = (int)this.Settlement.Town.FoodChange;
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_food_stocks", null).ToString(), ((int)this.Settlement.Town.FoodStocks).ToString(), changeAmount3, SelectableItemPropertyVM.PropertyType.Food, hint3, false));
			}
			int changeAmount4 = ((this.Settlement.Town != null) ? ((int)this.Settlement.Town.ProsperityChange) : ((int)this.Settlement.Village.HearthChange));
			if (this.Settlement.IsFortification)
			{
				BasicTooltipViewModel hint4;
				if (this.Settlement.Town != null)
				{
					hint4 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(this.Settlement.Town));
				}
				else
				{
					hint4 = new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageProsperityTooltip(this.Settlement.Village));
				}
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_prosperity", null).ToString(), string.Format("{0:0.#}", this.Settlement.Town.Prosperity), changeAmount4, SelectableItemPropertyVM.PropertyType.Prosperity, hint4, false));
			}
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint5 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(this.Settlement.Town));
				int changeAmount5 = (int)this.Settlement.Town.LoyaltyChange;
				bool isWarning = this.Settlement.IsTown && this.Settlement.Town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_loyalty", null).ToString(), string.Format("{0:0.#}", this.Settlement.Town.Loyalty), changeAmount5, SelectableItemPropertyVM.PropertyType.Loyalty, hint5, isWarning));
				BasicTooltipViewModel hint6 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(this.Settlement.Town));
				int changeAmount6 = (int)this.Settlement.Town.SecurityChange;
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_security", null).ToString(), string.Format("{0:0.#}", this.Settlement.Town.Security), changeAmount6, SelectableItemPropertyVM.PropertyType.Security, hint6, false));
			}
			if (this.Settlement.IsTown)
			{
				BasicTooltipViewModel hint7 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownPatrolTooltip(this.Settlement.Town));
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_patrol", null).ToString(), Campaign.Current.GetCampaignBehavior<IPatrolPartiesCampaignBehavior>().GetSettlementPatrolStatus(this.Settlement).ToString(), 0, SelectableItemPropertyVM.PropertyType.Patrol, hint7, false));
			}
		}

		// Token: 0x060007F8 RID: 2040 RVA: 0x00024F98 File Offset: 0x00023198
		protected override void OnSelect()
		{
			base.OnSelect();
			this._onSelect(this);
		}

		// Token: 0x060007F9 RID: 2041 RVA: 0x00024FAC File Offset: 0x000231AC
		private void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[] { this.Settlement, true });
		}

		// Token: 0x060007FA RID: 2042 RVA: 0x00024FD5 File Offset: 0x000231D5
		private void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x060007FB RID: 2043 RVA: 0x00024FDC File Offset: 0x000231DC
		public void ExecuteLink()
		{
			if (this.Settlement != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Settlement.EncyclopediaLink);
			}
		}

		// Token: 0x1700023D RID: 573
		// (get) Token: 0x060007FC RID: 2044 RVA: 0x00025000 File Offset: 0x00023200
		// (set) Token: 0x060007FD RID: 2045 RVA: 0x00025008 File Offset: 0x00023208
		[DataSourceProperty]
		public MBBindingList<SelectableFiefItemPropertyVM> ItemProperties
		{
			get
			{
				return this._itemProperties;
			}
			set
			{
				if (value != this._itemProperties)
				{
					this._itemProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<SelectableFiefItemPropertyVM>>(value, "ItemProperties");
				}
			}
		}

		// Token: 0x1700023E RID: 574
		// (get) Token: 0x060007FE RID: 2046 RVA: 0x00025026 File Offset: 0x00023226
		// (set) Token: 0x060007FF RID: 2047 RVA: 0x0002502E File Offset: 0x0002322E
		[DataSourceProperty]
		public MBBindingList<KingdomSettlementVillageItemVM> Villages
		{
			get
			{
				return this._villages;
			}
			set
			{
				if (value != this._villages)
				{
					this._villages = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomSettlementVillageItemVM>>(value, "Villages");
				}
			}
		}

		// Token: 0x1700023F RID: 575
		// (get) Token: 0x06000800 RID: 2048 RVA: 0x0002504C File Offset: 0x0002324C
		// (set) Token: 0x06000801 RID: 2049 RVA: 0x00025054 File Offset: 0x00023254
		[DataSourceProperty]
		public string IconPath
		{
			get
			{
				return this._iconPath;
			}
			set
			{
				if (value != this._iconPath)
				{
					this._iconPath = value;
					base.OnPropertyChangedWithValue<string>(value, "IconPath");
				}
			}
		}

		// Token: 0x17000240 RID: 576
		// (get) Token: 0x06000802 RID: 2050 RVA: 0x00025077 File Offset: 0x00023277
		// (set) Token: 0x06000803 RID: 2051 RVA: 0x0002507F File Offset: 0x0002327F
		[DataSourceProperty]
		public int Defenders
		{
			get
			{
				return this._defenders;
			}
			set
			{
				if (value != this._defenders)
				{
					this._defenders = value;
					base.OnPropertyChangedWithValue(value, "Defenders");
				}
			}
		}

		// Token: 0x17000241 RID: 577
		// (get) Token: 0x06000804 RID: 2052 RVA: 0x0002509D File Offset: 0x0002329D
		// (set) Token: 0x06000805 RID: 2053 RVA: 0x000250A5 File Offset: 0x000232A5
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000242 RID: 578
		// (get) Token: 0x06000806 RID: 2054 RVA: 0x000250C8 File Offset: 0x000232C8
		// (set) Token: 0x06000807 RID: 2055 RVA: 0x000250D0 File Offset: 0x000232D0
		[DataSourceProperty]
		public string ImageName
		{
			get
			{
				return this._imageName;
			}
			set
			{
				if (value != this._imageName)
				{
					this._imageName = value;
					base.OnPropertyChangedWithValue<string>(value, "ImageName");
				}
			}
		}

		// Token: 0x17000243 RID: 579
		// (get) Token: 0x06000808 RID: 2056 RVA: 0x000250F3 File Offset: 0x000232F3
		// (set) Token: 0x06000809 RID: 2057 RVA: 0x000250FB File Offset: 0x000232FB
		[DataSourceProperty]
		public string SettlementImagePath
		{
			get
			{
				return this._settlementImagePath;
			}
			set
			{
				if (value != this._settlementImagePath)
				{
					this._settlementImagePath = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementImagePath");
				}
			}
		}

		// Token: 0x17000244 RID: 580
		// (get) Token: 0x0600080A RID: 2058 RVA: 0x0002511E File Offset: 0x0002331E
		// (set) Token: 0x0600080B RID: 2059 RVA: 0x00025126 File Offset: 0x00023326
		[DataSourceProperty]
		public string GovernorName
		{
			get
			{
				return this._governorName;
			}
			set
			{
				if (value != this._governorName)
				{
					this._governorName = value;
					base.OnPropertyChangedWithValue<string>(value, "GovernorName");
				}
			}
		}

		// Token: 0x17000245 RID: 581
		// (get) Token: 0x0600080C RID: 2060 RVA: 0x00025149 File Offset: 0x00023349
		// (set) Token: 0x0600080D RID: 2061 RVA: 0x00025151 File Offset: 0x00023351
		[DataSourceProperty]
		public BannerImageIdentifierVM OwnerClanBanner
		{
			get
			{
				return this._ownerClanBanner;
			}
			set
			{
				if (value != this._ownerClanBanner)
				{
					this._ownerClanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "OwnerClanBanner");
				}
			}
		}

		// Token: 0x17000246 RID: 582
		// (get) Token: 0x0600080E RID: 2062 RVA: 0x0002516F File Offset: 0x0002336F
		// (set) Token: 0x0600080F RID: 2063 RVA: 0x00025177 File Offset: 0x00023377
		[DataSourceProperty]
		public BannerImageIdentifierVM OwnerClanBanner_9
		{
			get
			{
				return this._ownerClanBanner_9;
			}
			set
			{
				if (value != this._ownerClanBanner_9)
				{
					this._ownerClanBanner_9 = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "OwnerClanBanner_9");
				}
			}
		}

		// Token: 0x17000247 RID: 583
		// (get) Token: 0x06000810 RID: 2064 RVA: 0x00025195 File Offset: 0x00023395
		// (set) Token: 0x06000811 RID: 2065 RVA: 0x0002519D File Offset: 0x0002339D
		[DataSourceProperty]
		public HeroVM Owner
		{
			get
			{
				return this._owner;
			}
			set
			{
				if (value != this._owner)
				{
					this._owner = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Owner");
				}
			}
		}

		// Token: 0x17000248 RID: 584
		// (get) Token: 0x06000812 RID: 2066 RVA: 0x000251BB File Offset: 0x000233BB
		// (set) Token: 0x06000813 RID: 2067 RVA: 0x000251C3 File Offset: 0x000233C3
		[DataSourceProperty]
		public int WallLevel
		{
			get
			{
				return this._wallLevel;
			}
			set
			{
				if (value != this._wallLevel)
				{
					this._wallLevel = value;
					base.OnPropertyChangedWithValue(value, "WallLevel");
				}
			}
		}

		// Token: 0x17000249 RID: 585
		// (get) Token: 0x06000814 RID: 2068 RVA: 0x000251E1 File Offset: 0x000233E1
		// (set) Token: 0x06000815 RID: 2069 RVA: 0x000251E9 File Offset: 0x000233E9
		[DataSourceProperty]
		public int Prosperity
		{
			get
			{
				return this._prosperity;
			}
			set
			{
				if (value != this._prosperity)
				{
					this._prosperity = value;
					base.OnPropertyChangedWithValue(value, "Prosperity");
				}
			}
		}

		// Token: 0x04000369 RID: 873
		private readonly Action<KingdomSettlementItemVM> _onSelect;

		// Token: 0x0400036A RID: 874
		public readonly Settlement Settlement;

		// Token: 0x0400036D RID: 877
		private string _iconPath;

		// Token: 0x0400036E RID: 878
		private string _name;

		// Token: 0x0400036F RID: 879
		private string _imageName;

		// Token: 0x04000370 RID: 880
		private string _settlementImagePath;

		// Token: 0x04000371 RID: 881
		private string _governorName;

		// Token: 0x04000372 RID: 882
		private BannerImageIdentifierVM _ownerClanBanner;

		// Token: 0x04000373 RID: 883
		private BannerImageIdentifierVM _ownerClanBanner_9;

		// Token: 0x04000374 RID: 884
		private HeroVM _owner;

		// Token: 0x04000375 RID: 885
		private MBBindingList<SelectableFiefItemPropertyVM> _itemProperties;

		// Token: 0x04000376 RID: 886
		private MBBindingList<KingdomSettlementVillageItemVM> _villages;

		// Token: 0x04000377 RID: 887
		private int _wallLevel;

		// Token: 0x04000378 RID: 888
		private int _prosperity;

		// Token: 0x04000379 RID: 889
		private int _defenders;
	}
}
