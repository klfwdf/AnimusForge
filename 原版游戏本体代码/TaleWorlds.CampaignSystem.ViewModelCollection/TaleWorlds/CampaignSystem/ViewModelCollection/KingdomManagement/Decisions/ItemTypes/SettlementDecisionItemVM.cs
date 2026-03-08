using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x02000082 RID: 130
	public class SettlementDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x17000352 RID: 850
		// (get) Token: 0x06000AB5 RID: 2741 RVA: 0x0002DF90 File Offset: 0x0002C190
		public Settlement Settlement
		{
			get
			{
				if (this._settlementDecision == null && this._settlementPreliminaryDecision == null)
				{
					SettlementClaimantDecision settlementDecision;
					SettlementClaimantPreliminaryDecision settlementPreliminaryDecision;
					if ((settlementDecision = this._decision as SettlementClaimantDecision) != null)
					{
						this._settlementDecision = settlementDecision;
					}
					else if ((settlementPreliminaryDecision = this._decision as SettlementClaimantPreliminaryDecision) != null)
					{
						this._settlementPreliminaryDecision = settlementPreliminaryDecision;
					}
				}
				if (this._settlementDecision == null)
				{
					return this._settlementPreliminaryDecision.Settlement;
				}
				return this._settlementDecision.Settlement;
			}
		}

		// Token: 0x06000AB6 RID: 2742 RVA: 0x0002DFFA File Offset: 0x0002C1FA
		public SettlementDecisionItemVM(Settlement settlement, KingdomDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._settlement = settlement;
			base.DecisionType = 1;
		}

		// Token: 0x06000AB7 RID: 2743 RVA: 0x0002E014 File Offset: 0x0002C214
		protected override void InitValues()
		{
			base.InitValues();
			base.DecisionType = 1;
			this.SettlementImageID = ((this.Settlement.SettlementComponent != null) ? this.Settlement.SettlementComponent.WaitMeshName : "");
			this.BoundVillages = new MBBindingList<EncyclopediaSettlementVM>();
			this.NotableCharacters = new MBBindingList<HeroVM>();
			this.SettlementName = this.Settlement.Name.ToString();
			Town town = this.Settlement.Town;
			this.Governor = new HeroVM((town != null) ? town.Governor : null, false);
			foreach (Village village in this.Settlement.BoundVillages)
			{
				this.BoundVillages.Add(new EncyclopediaSettlementVM(village.Settlement));
			}
			Town town2 = this.Settlement.Town;
			this.WallsText = town2.GetWallLevel().ToString();
			this.WallsHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(this.Settlement.Town));
			this.HasNotables = this.Settlement.Notables.Count > 0;
			if (!this.Settlement.IsCastle)
			{
				Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
				foreach (Hero hero in this.Settlement.Notables)
				{
					this.NotableCharacters.Add(new HeroVM(hero, false));
				}
			}
			this.DescriptorText = this.Settlement.Culture.Name.ToString();
			this.DetailsText = GameTexts.FindText("str_people_encyclopedia_details", null).ToString();
			this.OwnerText = GameTexts.FindText("str_owner", null).ToString();
			this.Owner = new HeroVM(this.Settlement.OwnerClan.Leader, false);
			SettlementComponent settlementComponent = this.Settlement.SettlementComponent;
			this.SettlementPath = settlementComponent.BackgroundMeshName;
			this.SettlementCropPosition = (double)settlementComponent.BackgroundCropPosition;
			this.NotableCharactersText = GameTexts.FindText("str_notable_characters", null).ToString();
			this.BoundSettlementText = GameTexts.FindText("str_villages", null).ToString();
			if (this.HasBoundSettlement)
			{
				GameTexts.SetVariable("SETTLEMENT_LINK", this.Settlement.Village.Bound.EncyclopediaLinkWithName);
				this.BoundSettlementText = GameTexts.FindText("str_bound_settlement_encyclopedia", null).ToString();
			}
			this.MilitasText = ((int)this.Settlement.Militia).ToString();
			this.MilitasHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownMilitiaTooltip(this.Settlement.Town));
			this.ProsperityText = ((this.Settlement.Town != null) ? ((int)this.Settlement.Town.Prosperity).ToString() : (this.Settlement.IsVillage ? ((int)this.Settlement.Village.Hearth).ToString() : string.Empty));
			if (this.Settlement.IsTown)
			{
				this.ProsperityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(this.Settlement.Town));
			}
			else
			{
				this.ProsperityHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_prosperity", null).ToString());
			}
			if (this.Settlement.Town != null)
			{
				this.LoyaltyHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(this.Settlement.Town));
				this.LoyaltyText = string.Format("{0:0.#}", this.Settlement.Town.Loyalty);
				this.SecurityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(this.Settlement.Town));
				this.SecurityText = string.Format("{0:0.#}", this.Settlement.Town.Security);
			}
			else
			{
				this.LoyaltyText = "-";
				this.SecurityText = "-";
			}
			Town town3 = this.Settlement.Town;
			this.FoodText = ((town3 != null) ? town3.FoodStocks.ToString("0.0") : null);
			if (this.Settlement.IsFortification)
			{
				this.FoodHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(this.Settlement.Town));
				MobileParty garrisonParty = this.Settlement.Town.GarrisonParty;
				this.GarrisonText = ((garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers.ToString() : null) ?? "0";
				this.GarrisonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(this.Settlement.Town));
				return;
			}
			this.FoodHint = new BasicTooltipViewModel();
			this.GarrisonText = "-";
		}

		// Token: 0x17000353 RID: 851
		// (get) Token: 0x06000AB8 RID: 2744 RVA: 0x0002E4F4 File Offset: 0x0002C6F4
		// (set) Token: 0x06000AB9 RID: 2745 RVA: 0x0002E4FC File Offset: 0x0002C6FC
		[DataSourceProperty]
		public bool HasBoundSettlement
		{
			get
			{
				return this._hasBoundSettlement;
			}
			set
			{
				if (value != this._hasBoundSettlement)
				{
					this._hasBoundSettlement = value;
					base.OnPropertyChangedWithValue(value, "HasBoundSettlement");
				}
			}
		}

		// Token: 0x17000354 RID: 852
		// (get) Token: 0x06000ABA RID: 2746 RVA: 0x0002E51A File Offset: 0x0002C71A
		// (set) Token: 0x06000ABB RID: 2747 RVA: 0x0002E522 File Offset: 0x0002C722
		[DataSourceProperty]
		public double SettlementCropPosition
		{
			get
			{
				return this._settlementCropPosition;
			}
			set
			{
				if (value != this._settlementCropPosition)
				{
					this._settlementCropPosition = value;
					base.OnPropertyChangedWithValue(value, "SettlementCropPosition");
				}
			}
		}

		// Token: 0x17000355 RID: 853
		// (get) Token: 0x06000ABC RID: 2748 RVA: 0x0002E540 File Offset: 0x0002C740
		// (set) Token: 0x06000ABD RID: 2749 RVA: 0x0002E548 File Offset: 0x0002C748
		[DataSourceProperty]
		public string BoundSettlementText
		{
			get
			{
				return this._boundSettlementText;
			}
			set
			{
				if (value != this._boundSettlementText)
				{
					this._boundSettlementText = value;
					base.OnPropertyChangedWithValue<string>(value, "BoundSettlementText");
				}
			}
		}

		// Token: 0x17000356 RID: 854
		// (get) Token: 0x06000ABE RID: 2750 RVA: 0x0002E56B File Offset: 0x0002C76B
		// (set) Token: 0x06000ABF RID: 2751 RVA: 0x0002E573 File Offset: 0x0002C773
		[DataSourceProperty]
		public string DetailsText
		{
			get
			{
				return this._detailsText;
			}
			set
			{
				if (value != this._detailsText)
				{
					this._detailsText = value;
					base.OnPropertyChangedWithValue<string>(value, "DetailsText");
				}
			}
		}

		// Token: 0x17000357 RID: 855
		// (get) Token: 0x06000AC0 RID: 2752 RVA: 0x0002E596 File Offset: 0x0002C796
		// (set) Token: 0x06000AC1 RID: 2753 RVA: 0x0002E59E File Offset: 0x0002C79E
		[DataSourceProperty]
		public string SettlementPath
		{
			get
			{
				return this._settlementPath;
			}
			set
			{
				if (value != this._settlementPath)
				{
					this._settlementPath = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementPath");
				}
			}
		}

		// Token: 0x17000358 RID: 856
		// (get) Token: 0x06000AC2 RID: 2754 RVA: 0x0002E5C1 File Offset: 0x0002C7C1
		// (set) Token: 0x06000AC3 RID: 2755 RVA: 0x0002E5C9 File Offset: 0x0002C7C9
		[DataSourceProperty]
		public string SettlementName
		{
			get
			{
				return this._settlementName;
			}
			set
			{
				if (value != this._settlementName)
				{
					this._settlementName = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementName");
				}
			}
		}

		// Token: 0x17000359 RID: 857
		// (get) Token: 0x06000AC4 RID: 2756 RVA: 0x0002E5EC File Offset: 0x0002C7EC
		// (set) Token: 0x06000AC5 RID: 2757 RVA: 0x0002E5F4 File Offset: 0x0002C7F4
		[DataSourceProperty]
		public string InformationText
		{
			get
			{
				return this._informationText;
			}
			set
			{
				if (value != this._informationText)
				{
					this._informationText = value;
					base.OnPropertyChangedWithValue<string>(value, "InformationText");
				}
			}
		}

		// Token: 0x1700035A RID: 858
		// (get) Token: 0x06000AC6 RID: 2758 RVA: 0x0002E617 File Offset: 0x0002C817
		// (set) Token: 0x06000AC7 RID: 2759 RVA: 0x0002E61F File Offset: 0x0002C81F
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

		// Token: 0x1700035B RID: 859
		// (get) Token: 0x06000AC8 RID: 2760 RVA: 0x0002E63D File Offset: 0x0002C83D
		// (set) Token: 0x06000AC9 RID: 2761 RVA: 0x0002E645 File Offset: 0x0002C845
		[DataSourceProperty]
		public string VillagesText
		{
			get
			{
				return this._villagesText;
			}
			set
			{
				if (value != this._villagesText)
				{
					this._villagesText = value;
					base.OnPropertyChangedWithValue<string>(value, "VillagesText");
				}
			}
		}

		// Token: 0x1700035C RID: 860
		// (get) Token: 0x06000ACA RID: 2762 RVA: 0x0002E668 File Offset: 0x0002C868
		// (set) Token: 0x06000ACB RID: 2763 RVA: 0x0002E670 File Offset: 0x0002C870
		[DataSourceProperty]
		public string SettlementImageID
		{
			get
			{
				return this._settlementImageID;
			}
			set
			{
				if (value != this._settlementImageID)
				{
					this._settlementImageID = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementImageID");
				}
			}
		}

		// Token: 0x1700035D RID: 861
		// (get) Token: 0x06000ACC RID: 2764 RVA: 0x0002E693 File Offset: 0x0002C893
		// (set) Token: 0x06000ACD RID: 2765 RVA: 0x0002E69B File Offset: 0x0002C89B
		[DataSourceProperty]
		public string NotableCharactersText
		{
			get
			{
				return this._notableCharactersText;
			}
			set
			{
				if (value != this._notableCharactersText)
				{
					this._notableCharactersText = value;
					base.OnPropertyChangedWithValue<string>(value, "NotableCharactersText");
				}
			}
		}

		// Token: 0x1700035E RID: 862
		// (get) Token: 0x06000ACE RID: 2766 RVA: 0x0002E6BE File Offset: 0x0002C8BE
		// (set) Token: 0x06000ACF RID: 2767 RVA: 0x0002E6C6 File Offset: 0x0002C8C6
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSettlementVM> BoundVillages
		{
			get
			{
				return this._boundVillages;
			}
			set
			{
				if (value != this._boundVillages)
				{
					this._boundVillages = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementVM>>(value, "BoundVillages");
				}
			}
		}

		// Token: 0x1700035F RID: 863
		// (get) Token: 0x06000AD0 RID: 2768 RVA: 0x0002E6E4 File Offset: 0x0002C8E4
		// (set) Token: 0x06000AD1 RID: 2769 RVA: 0x0002E6EC File Offset: 0x0002C8EC
		[DataSourceProperty]
		public MBBindingList<HeroVM> NotableCharacters
		{
			get
			{
				return this._notableCharacters;
			}
			set
			{
				if (value != this._notableCharacters)
				{
					this._notableCharacters = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "NotableCharacters");
				}
			}
		}

		// Token: 0x17000360 RID: 864
		// (get) Token: 0x06000AD2 RID: 2770 RVA: 0x0002E70A File Offset: 0x0002C90A
		// (set) Token: 0x06000AD3 RID: 2771 RVA: 0x0002E712 File Offset: 0x0002C912
		[DataSourceProperty]
		public BasicTooltipViewModel MilitasHint
		{
			get
			{
				return this._militasHint;
			}
			set
			{
				if (value != this._militasHint)
				{
					this._militasHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "MilitasHint");
				}
			}
		}

		// Token: 0x17000361 RID: 865
		// (get) Token: 0x06000AD4 RID: 2772 RVA: 0x0002E730 File Offset: 0x0002C930
		// (set) Token: 0x06000AD5 RID: 2773 RVA: 0x0002E738 File Offset: 0x0002C938
		[DataSourceProperty]
		public BasicTooltipViewModel FoodHint
		{
			get
			{
				return this._foodHint;
			}
			set
			{
				if (value != this._foodHint)
				{
					this._foodHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "FoodHint");
				}
			}
		}

		// Token: 0x17000362 RID: 866
		// (get) Token: 0x06000AD6 RID: 2774 RVA: 0x0002E756 File Offset: 0x0002C956
		// (set) Token: 0x06000AD7 RID: 2775 RVA: 0x0002E75E File Offset: 0x0002C95E
		[DataSourceProperty]
		public BasicTooltipViewModel GarrisonHint
		{
			get
			{
				return this._garrisonHint;
			}
			set
			{
				if (value != this._garrisonHint)
				{
					this._garrisonHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "GarrisonHint");
				}
			}
		}

		// Token: 0x17000363 RID: 867
		// (get) Token: 0x06000AD8 RID: 2776 RVA: 0x0002E77C File Offset: 0x0002C97C
		// (set) Token: 0x06000AD9 RID: 2777 RVA: 0x0002E784 File Offset: 0x0002C984
		[DataSourceProperty]
		public BasicTooltipViewModel ProsperityHint
		{
			get
			{
				return this._prosperityHint;
			}
			set
			{
				if (value != this._prosperityHint)
				{
					this._prosperityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ProsperityHint");
				}
			}
		}

		// Token: 0x17000364 RID: 868
		// (get) Token: 0x06000ADA RID: 2778 RVA: 0x0002E7A2 File Offset: 0x0002C9A2
		// (set) Token: 0x06000ADB RID: 2779 RVA: 0x0002E7AA File Offset: 0x0002C9AA
		[DataSourceProperty]
		public BasicTooltipViewModel LoyaltyHint
		{
			get
			{
				return this._loyaltyHint;
			}
			set
			{
				if (value != this._loyaltyHint)
				{
					this._loyaltyHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "LoyaltyHint");
				}
			}
		}

		// Token: 0x17000365 RID: 869
		// (get) Token: 0x06000ADC RID: 2780 RVA: 0x0002E7C8 File Offset: 0x0002C9C8
		// (set) Token: 0x06000ADD RID: 2781 RVA: 0x0002E7D0 File Offset: 0x0002C9D0
		[DataSourceProperty]
		public BasicTooltipViewModel SecurityHint
		{
			get
			{
				return this._securityHint;
			}
			set
			{
				if (value != this._securityHint)
				{
					this._securityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SecurityHint");
				}
			}
		}

		// Token: 0x17000366 RID: 870
		// (get) Token: 0x06000ADE RID: 2782 RVA: 0x0002E7EE File Offset: 0x0002C9EE
		// (set) Token: 0x06000ADF RID: 2783 RVA: 0x0002E7F6 File Offset: 0x0002C9F6
		[DataSourceProperty]
		public BasicTooltipViewModel WallsHint
		{
			get
			{
				return this._wallsHint;
			}
			set
			{
				if (value != this._wallsHint)
				{
					this._wallsHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "WallsHint");
				}
			}
		}

		// Token: 0x17000367 RID: 871
		// (get) Token: 0x06000AE0 RID: 2784 RVA: 0x0002E814 File Offset: 0x0002CA14
		// (set) Token: 0x06000AE1 RID: 2785 RVA: 0x0002E81C File Offset: 0x0002CA1C
		[DataSourceProperty]
		public string MilitasText
		{
			get
			{
				return this._militasText;
			}
			set
			{
				if (value != this._militasText)
				{
					this._militasText = value;
					base.OnPropertyChangedWithValue<string>(value, "MilitasText");
				}
			}
		}

		// Token: 0x17000368 RID: 872
		// (get) Token: 0x06000AE2 RID: 2786 RVA: 0x0002E83F File Offset: 0x0002CA3F
		// (set) Token: 0x06000AE3 RID: 2787 RVA: 0x0002E847 File Offset: 0x0002CA47
		[DataSourceProperty]
		public string ProsperityText
		{
			get
			{
				return this._prosperityText;
			}
			set
			{
				if (value != this._prosperityText)
				{
					this._prosperityText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProsperityText");
				}
			}
		}

		// Token: 0x17000369 RID: 873
		// (get) Token: 0x06000AE4 RID: 2788 RVA: 0x0002E86A File Offset: 0x0002CA6A
		// (set) Token: 0x06000AE5 RID: 2789 RVA: 0x0002E872 File Offset: 0x0002CA72
		[DataSourceProperty]
		public string LoyaltyText
		{
			get
			{
				return this._loyaltyText;
			}
			set
			{
				if (value != this._loyaltyText)
				{
					this._loyaltyText = value;
					base.OnPropertyChangedWithValue<string>(value, "LoyaltyText");
				}
			}
		}

		// Token: 0x1700036A RID: 874
		// (get) Token: 0x06000AE6 RID: 2790 RVA: 0x0002E895 File Offset: 0x0002CA95
		// (set) Token: 0x06000AE7 RID: 2791 RVA: 0x0002E89D File Offset: 0x0002CA9D
		[DataSourceProperty]
		public string SecurityText
		{
			get
			{
				return this._securityText;
			}
			set
			{
				if (value != this._securityText)
				{
					this._securityText = value;
					base.OnPropertyChangedWithValue<string>(value, "SecurityText");
				}
			}
		}

		// Token: 0x1700036B RID: 875
		// (get) Token: 0x06000AE8 RID: 2792 RVA: 0x0002E8C0 File Offset: 0x0002CAC0
		// (set) Token: 0x06000AE9 RID: 2793 RVA: 0x0002E8C8 File Offset: 0x0002CAC8
		[DataSourceProperty]
		public string WallsText
		{
			get
			{
				return this._wallsText;
			}
			set
			{
				if (value != this._wallsText)
				{
					this._wallsText = value;
					base.OnPropertyChangedWithValue<string>(value, "WallsText");
				}
			}
		}

		// Token: 0x1700036C RID: 876
		// (get) Token: 0x06000AEA RID: 2794 RVA: 0x0002E8EB File Offset: 0x0002CAEB
		// (set) Token: 0x06000AEB RID: 2795 RVA: 0x0002E8F3 File Offset: 0x0002CAF3
		[DataSourceProperty]
		public string FoodText
		{
			get
			{
				return this._foodText;
			}
			set
			{
				if (value != this._foodText)
				{
					this._foodText = value;
					base.OnPropertyChangedWithValue<string>(value, "FoodText");
				}
			}
		}

		// Token: 0x1700036D RID: 877
		// (get) Token: 0x06000AEC RID: 2796 RVA: 0x0002E916 File Offset: 0x0002CB16
		// (set) Token: 0x06000AED RID: 2797 RVA: 0x0002E91E File Offset: 0x0002CB1E
		[DataSourceProperty]
		public string GarrisonText
		{
			get
			{
				return this._garrisonText;
			}
			set
			{
				if (value != this._garrisonText)
				{
					this._garrisonText = value;
					base.OnPropertyChangedWithValue<string>(value, "GarrisonText");
				}
			}
		}

		// Token: 0x1700036E RID: 878
		// (get) Token: 0x06000AEE RID: 2798 RVA: 0x0002E941 File Offset: 0x0002CB41
		// (set) Token: 0x06000AEF RID: 2799 RVA: 0x0002E949 File Offset: 0x0002CB49
		[DataSourceProperty]
		public string DescriptorText
		{
			get
			{
				return this._descriptorText;
			}
			set
			{
				if (value != this._descriptorText)
				{
					this._descriptorText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptorText");
				}
			}
		}

		// Token: 0x1700036F RID: 879
		// (get) Token: 0x06000AF0 RID: 2800 RVA: 0x0002E96C File Offset: 0x0002CB6C
		// (set) Token: 0x06000AF1 RID: 2801 RVA: 0x0002E974 File Offset: 0x0002CB74
		[DataSourceProperty]
		public string OwnerText
		{
			get
			{
				return this._ownerText;
			}
			set
			{
				if (value != this._ownerText)
				{
					this._ownerText = value;
					base.OnPropertyChangedWithValue<string>(value, "OwnerText");
				}
			}
		}

		// Token: 0x17000370 RID: 880
		// (get) Token: 0x06000AF2 RID: 2802 RVA: 0x0002E997 File Offset: 0x0002CB97
		// (set) Token: 0x06000AF3 RID: 2803 RVA: 0x0002E99F File Offset: 0x0002CB9F
		[DataSourceProperty]
		public HeroVM Governor
		{
			get
			{
				return this._governor;
			}
			set
			{
				if (value != this._governor)
				{
					this._governor = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Governor");
				}
			}
		}

		// Token: 0x17000371 RID: 881
		// (get) Token: 0x06000AF4 RID: 2804 RVA: 0x0002E9BD File Offset: 0x0002CBBD
		// (set) Token: 0x06000AF5 RID: 2805 RVA: 0x0002E9C5 File Offset: 0x0002CBC5
		[DataSourceProperty]
		public bool HasNotables
		{
			get
			{
				return this._hasNotables;
			}
			set
			{
				if (value != this._hasNotables)
				{
					this._hasNotables = value;
					base.OnPropertyChangedWithValue(value, "HasNotables");
				}
			}
		}

		// Token: 0x040004BC RID: 1212
		private SettlementClaimantDecision _settlementDecision;

		// Token: 0x040004BD RID: 1213
		private SettlementClaimantPreliminaryDecision _settlementPreliminaryDecision;

		// Token: 0x040004BE RID: 1214
		private Settlement _settlement;

		// Token: 0x040004BF RID: 1215
		private string _settlementName;

		// Token: 0x040004C0 RID: 1216
		private HeroVM _governor;

		// Token: 0x040004C1 RID: 1217
		private MBBindingList<EncyclopediaSettlementVM> _boundVillages;

		// Token: 0x040004C2 RID: 1218
		private MBBindingList<HeroVM> _notableCharacters;

		// Token: 0x040004C3 RID: 1219
		private BasicTooltipViewModel _militasHint;

		// Token: 0x040004C4 RID: 1220
		private BasicTooltipViewModel _prosperityHint;

		// Token: 0x040004C5 RID: 1221
		private BasicTooltipViewModel _loyaltyHint;

		// Token: 0x040004C6 RID: 1222
		private BasicTooltipViewModel _securityHint;

		// Token: 0x040004C7 RID: 1223
		private BasicTooltipViewModel _wallsHint;

		// Token: 0x040004C8 RID: 1224
		private BasicTooltipViewModel _garrisonHint;

		// Token: 0x040004C9 RID: 1225
		private BasicTooltipViewModel _foodHint;

		// Token: 0x040004CA RID: 1226
		private HeroVM _owner;

		// Token: 0x040004CB RID: 1227
		private string _ownerText;

		// Token: 0x040004CC RID: 1228
		private string _militasText;

		// Token: 0x040004CD RID: 1229
		private string _garrisonText;

		// Token: 0x040004CE RID: 1230
		private string _prosperityText;

		// Token: 0x040004CF RID: 1231
		private string _loyaltyText;

		// Token: 0x040004D0 RID: 1232
		private string _securityText;

		// Token: 0x040004D1 RID: 1233
		private string _wallsText;

		// Token: 0x040004D2 RID: 1234
		private string _foodText;

		// Token: 0x040004D3 RID: 1235
		private string _descriptorText;

		// Token: 0x040004D4 RID: 1236
		private string _villagesText;

		// Token: 0x040004D5 RID: 1237
		private string _notableCharactersText;

		// Token: 0x040004D6 RID: 1238
		private string _settlementPath;

		// Token: 0x040004D7 RID: 1239
		private string _informationText;

		// Token: 0x040004D8 RID: 1240
		private string _settlementImageID;

		// Token: 0x040004D9 RID: 1241
		private string _boundSettlementText;

		// Token: 0x040004DA RID: 1242
		private string _detailsText;

		// Token: 0x040004DB RID: 1243
		private double _settlementCropPosition;

		// Token: 0x040004DC RID: 1244
		private bool _hasBoundSettlement;

		// Token: 0x040004DD RID: 1245
		private bool _hasNotables;
	}
}
