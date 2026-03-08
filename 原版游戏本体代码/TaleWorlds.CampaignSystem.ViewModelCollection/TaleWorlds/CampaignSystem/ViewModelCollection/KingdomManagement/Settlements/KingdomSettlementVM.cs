using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements
{
	// Token: 0x02000069 RID: 105
	public class KingdomSettlementVM : KingdomCategoryVM
	{
		// Token: 0x06000831 RID: 2097 RVA: 0x00025638 File Offset: 0x00023838
		public KingdomSettlementVM(Action<KingdomDecision> forceDecision, Action<Settlement> onGrantFief)
		{
			this._forceDecision = forceDecision;
			this._onGrantFief = onGrantFief;
			this._kingdom = Hero.MainHero.MapFaction as Kingdom;
			this.AnnexCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(Clan.PlayerClan);
			this.AnnexHint = new HintViewModel();
			base.IsAcceptableItemSelected = false;
			this.Settlements = new MBBindingList<KingdomSettlementItemVM>();
			this.RefreshSettlementList();
			base.NotificationCount = 0;
			this.SettlementSortController = new KingdomSettlementSortControllerVM(this.Settlements);
			this.RefreshValues();
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x000256CE File Offset: 0x000238CE
		protected virtual KingdomSettlementItemVM CreateSettlementItemVM(Settlement settlement, Action<KingdomSettlementItemVM> onSelect)
		{
			return new KingdomSettlementItemVM(settlement, onSelect);
		}

		// Token: 0x06000833 RID: 2099 RVA: 0x000256D8 File Offset: 0x000238D8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.OwnerText = GameTexts.FindText("str_owner", null).ToString();
			this.NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
			this.TypeText = GameTexts.FindText("str_sort_by_type_label", null).ToString();
			this.ProsperityText = GameTexts.FindText("str_prosperity_abbr", null).ToString();
			this.FoodText = GameTexts.FindText("str_inventory_category_tooltip", "6").ToString();
			this.GarrisonText = GameTexts.FindText("str_map_tooltip_garrison", null).ToString();
			this.MilitiaText = GameTexts.FindText("str_militia", null).ToString();
			this.ClanText = GameTexts.FindText("str_clans", null).ToString();
			this.VillagesText = GameTexts.FindText("str_villages", null).ToString();
			base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_settlement_selected", null).ToString();
			this.ProposeText = GameTexts.FindText("str_policy_propose", null).ToString();
			this.DefendersText = GameTexts.FindText("str_sort_by_defenders_label", null).ToString();
			base.CategoryNameText = new TextObject("{=qKUjgS6r}Settlement", null).ToString();
			this.Settlements.ApplyActionOnAllItems(delegate(KingdomSettlementItemVM x)
			{
				x.RefreshValues();
			});
			KingdomSettlementItemVM currentSelectedSettlement = this.CurrentSelectedSettlement;
			if (currentSelectedSettlement == null)
			{
				return;
			}
			currentSelectedSettlement.RefreshValues();
		}

		// Token: 0x06000834 RID: 2100 RVA: 0x0002584C File Offset: 0x00023A4C
		public void RefreshSettlementList()
		{
			this.Settlements.Clear();
			if (this._kingdom != null)
			{
				foreach (Settlement settlement in from S in this._kingdom.Settlements
					where S.IsCastle || S.IsTown
					select S)
				{
					KingdomSettlementItemVM item = this.CreateSettlementItemVM(settlement, new Action<KingdomSettlementItemVM>(this.OnSettlementSelection));
					this.Settlements.Add(item);
				}
			}
			if (this.Settlements.Count > 0)
			{
				this.SetCurrentSelectedSettlement(this.Settlements.FirstOrDefault<KingdomSettlementItemVM>());
			}
		}

		// Token: 0x06000835 RID: 2101 RVA: 0x00025910 File Offset: 0x00023B10
		private void SetCurrentSelectedSettlement(KingdomSettlementItemVM settlementItem)
		{
			if (this.CurrentSelectedSettlement != settlementItem)
			{
				if (this.CurrentSelectedSettlement != null)
				{
					this.CurrentSelectedSettlement.IsSelected = false;
				}
				this.CurrentSelectedSettlement = settlementItem;
				this.CurrentSelectedSettlement.IsSelected = true;
				if (settlementItem != null)
				{
					this._currenItemsUnresolvedDecision = this.GetSettlementsAnyWaitingDecision(settlementItem.Settlement);
					if (this._currenItemsUnresolvedDecision != null)
					{
						base.IsAcceptableItemSelected = true;
						this.AnnexCost = 0;
						this.AnnexText = GameTexts.FindText("str_resolve", null).ToString();
						this.AnnexActionExplanationText = GameTexts.FindText("str_resolve_explanation", null).ToString();
						this.AnnexHint.HintText = TextObject.GetEmpty();
					}
					else if (settlementItem.Owner.Hero == Hero.MainHero)
					{
						if (Hero.MainHero.IsKingdomLeader)
						{
							this.AnnexActionExplanationText = new TextObject("{=G2h0V10w}Gift this settlement to a clan in your kingdom.", null).ToString();
							this.AnnexText = new TextObject("{=sffGeQ1g}Gift", null).ToString();
						}
						else
						{
							this.AnnexActionExplanationText = new TextObject("{=1UbocG5B}Denounce your rights and responsibilities from this fief by giving it back to the realm.", null).ToString();
							this.AnnexText = new TextObject("{=U3ksQXD3}Give Away", null).ToString();
						}
						if (Hero.MainHero.IsPrisoner)
						{
							this.CanAnnexCurrentSettlement = false;
							this.HasCost = true;
							this.AnnexHint.HintText = GameTexts.FindText("str_action_disabled_reason_prisoner", null);
						}
						else if (!Campaign.Current.Models.DiplomacyModel.CanSettlementBeGifted(this._currentSelectedSettlement.Settlement))
						{
							this.CanAnnexCurrentSettlement = false;
							this.HasCost = true;
							this.AnnexHint.HintText = GameTexts.FindText("str_cannot_annex_waiting_for_ruler_decision", null);
						}
						else if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == null)
						{
							this.CanAnnexCurrentSettlement = false;
							this.HasCost = true;
							this.AnnexHint.HintText = GameTexts.FindText("str_action_disabled_reason_encounter", null);
						}
						else if (PlayerSiege.PlayerSiegeEvent != null)
						{
							this.CanAnnexCurrentSettlement = false;
							this.HasCost = true;
							this.AnnexHint.HintText = GameTexts.FindText("str_action_disabled_reason_siege", null);
						}
						else
						{
							this.CanAnnexCurrentSettlement = true;
							this.HasCost = false;
							this.AnnexHint.HintText = TextObject.GetEmpty();
						}
					}
					else
					{
						this.AnnexText = GameTexts.FindText("str_policy_propose", null).ToString();
						this.AnnexCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(Clan.PlayerClan);
						this.AnnexActionExplanationText = GameTexts.FindText("str_annex_fief_action_explanation", null).SetTextVariable("SUPPORT", KingdomSettlementVM.CalculateLikelihood(settlementItem.Settlement)).ToString();
						TextObject hintText;
						this.CanAnnexCurrentSettlement = this.GetCanAnnexSettlementWithReason(this.AnnexCost, out hintText);
						this.AnnexHint.HintText = hintText;
						this.HasCost = true;
					}
				}
				base.IsAcceptableItemSelected = this.CurrentSelectedSettlement != null;
			}
		}

		// Token: 0x06000836 RID: 2102 RVA: 0x00025BDC File Offset: 0x00023DDC
		private bool GetCanAnnexSettlementWithReason(int annexCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (Hero.MainHero.Clan.Influence < (float)annexCost)
			{
				disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence", null);
				return false;
			}
			if (this.CurrentSelectedSettlement.Settlement.OwnerClan == this._kingdom.RulingClan)
			{
				disabledReason = GameTexts.FindText("str_cannot_annex_ruling_clan_settlement", null);
				return false;
			}
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				disabledReason = GameTexts.FindText("str_cannot_annex_while_mercenary", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x00025C68 File Offset: 0x00023E68
		public void SelectSettlement(Settlement settlement)
		{
			foreach (KingdomSettlementItemVM kingdomSettlementItemVM in this.Settlements)
			{
				if (kingdomSettlementItemVM.Settlement == settlement)
				{
					this.OnSettlementSelection(kingdomSettlementItemVM);
					break;
				}
			}
		}

		// Token: 0x06000838 RID: 2104 RVA: 0x00025CC0 File Offset: 0x00023EC0
		private void OnSettlementSelection(KingdomSettlementItemVM settlement)
		{
			if (this._currentSelectedSettlement != settlement)
			{
				this.SetCurrentSelectedSettlement(settlement);
			}
		}

		// Token: 0x06000839 RID: 2105 RVA: 0x00025CD4 File Offset: 0x00023ED4
		private void ExecuteAnnex()
		{
			if (this._currentSelectedSettlement != null)
			{
				if (this._currenItemsUnresolvedDecision != null)
				{
					this._forceDecision(this._currenItemsUnresolvedDecision);
					return;
				}
				Settlement settlement = this._currentSelectedSettlement.Settlement;
				if (settlement.OwnerClan.Leader == Hero.MainHero)
				{
					this._onGrantFief(settlement);
					return;
				}
				if (Hero.MainHero.Clan.Influence >= (float)this.AnnexCost)
				{
					SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision = new SettlementClaimantPreliminaryDecision(Clan.PlayerClan, settlement);
					Clan.PlayerClan.Kingdom.AddDecision(settlementClaimantPreliminaryDecision, false);
					this._forceDecision(settlementClaimantPreliminaryDecision);
				}
			}
		}

		// Token: 0x0600083A RID: 2106 RVA: 0x00025D74 File Offset: 0x00023F74
		private KingdomDecision GetSettlementsAnyWaitingDecision(Settlement settlement)
		{
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
			{
				SettlementClaimantDecision settlementClaimantDecision;
				return (settlementClaimantDecision = d as SettlementClaimantDecision) != null && settlementClaimantDecision.Settlement == settlement && !d.ShouldBeCancelled();
			});
			KingdomDecision kingdomDecision2 = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
			{
				SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision;
				return (settlementClaimantPreliminaryDecision = d as SettlementClaimantPreliminaryDecision) != null && settlementClaimantPreliminaryDecision.Settlement == settlement && !d.ShouldBeCancelled();
			});
			return kingdomDecision ?? kingdomDecision2;
		}

		// Token: 0x17000254 RID: 596
		// (get) Token: 0x0600083B RID: 2107 RVA: 0x00025DD4 File Offset: 0x00023FD4
		// (set) Token: 0x0600083C RID: 2108 RVA: 0x00025DDC File Offset: 0x00023FDC
		[DataSourceProperty]
		public KingdomSettlementItemVM CurrentSelectedSettlement
		{
			get
			{
				return this._currentSelectedSettlement;
			}
			set
			{
				if (value != this._currentSelectedSettlement)
				{
					this._currentSelectedSettlement = value;
					base.OnPropertyChangedWithValue<KingdomSettlementItemVM>(value, "CurrentSelectedSettlement");
				}
			}
		}

		// Token: 0x17000255 RID: 597
		// (get) Token: 0x0600083D RID: 2109 RVA: 0x00025DFA File Offset: 0x00023FFA
		// (set) Token: 0x0600083E RID: 2110 RVA: 0x00025E02 File Offset: 0x00024002
		[DataSourceProperty]
		public KingdomSettlementSortControllerVM SettlementSortController
		{
			get
			{
				return this._settlementSortController;
			}
			set
			{
				if (value != this._settlementSortController)
				{
					this._settlementSortController = value;
					base.OnPropertyChangedWithValue<KingdomSettlementSortControllerVM>(value, "SettlementSortController");
				}
			}
		}

		// Token: 0x17000256 RID: 598
		// (get) Token: 0x0600083F RID: 2111 RVA: 0x00025E20 File Offset: 0x00024020
		// (set) Token: 0x06000840 RID: 2112 RVA: 0x00025E28 File Offset: 0x00024028
		[DataSourceProperty]
		public HintViewModel AnnexHint
		{
			get
			{
				return this._annexHint;
			}
			set
			{
				if (value != this._annexHint)
				{
					this._annexHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AnnexHint");
				}
			}
		}

		// Token: 0x17000257 RID: 599
		// (get) Token: 0x06000841 RID: 2113 RVA: 0x00025E46 File Offset: 0x00024046
		// (set) Token: 0x06000842 RID: 2114 RVA: 0x00025E4E File Offset: 0x0002404E
		[DataSourceProperty]
		public string ProposeText
		{
			get
			{
				return this._proposeText;
			}
			set
			{
				if (value != this._proposeText)
				{
					this._proposeText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProposeText");
				}
			}
		}

		// Token: 0x17000258 RID: 600
		// (get) Token: 0x06000843 RID: 2115 RVA: 0x00025E71 File Offset: 0x00024071
		// (set) Token: 0x06000844 RID: 2116 RVA: 0x00025E79 File Offset: 0x00024079
		[DataSourceProperty]
		public string AnnexActionExplanationText
		{
			get
			{
				return this._annexActionExplanationText;
			}
			set
			{
				if (value != this._annexActionExplanationText)
				{
					this._annexActionExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "AnnexActionExplanationText");
				}
			}
		}

		// Token: 0x17000259 RID: 601
		// (get) Token: 0x06000845 RID: 2117 RVA: 0x00025E9C File Offset: 0x0002409C
		// (set) Token: 0x06000846 RID: 2118 RVA: 0x00025EA4 File Offset: 0x000240A4
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

		// Token: 0x1700025A RID: 602
		// (get) Token: 0x06000847 RID: 2119 RVA: 0x00025EC7 File Offset: 0x000240C7
		// (set) Token: 0x06000848 RID: 2120 RVA: 0x00025ECF File Offset: 0x000240CF
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

		// Token: 0x1700025B RID: 603
		// (get) Token: 0x06000849 RID: 2121 RVA: 0x00025EF2 File Offset: 0x000240F2
		// (set) Token: 0x0600084A RID: 2122 RVA: 0x00025EFA File Offset: 0x000240FA
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

		// Token: 0x1700025C RID: 604
		// (get) Token: 0x0600084B RID: 2123 RVA: 0x00025F1D File Offset: 0x0002411D
		// (set) Token: 0x0600084C RID: 2124 RVA: 0x00025F25 File Offset: 0x00024125
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

		// Token: 0x1700025D RID: 605
		// (get) Token: 0x0600084D RID: 2125 RVA: 0x00025F48 File Offset: 0x00024148
		// (set) Token: 0x0600084E RID: 2126 RVA: 0x00025F50 File Offset: 0x00024150
		[DataSourceProperty]
		public string ClanText
		{
			get
			{
				return this._clanText;
			}
			set
			{
				if (value != this._clanText)
				{
					this._clanText = value;
					base.OnPropertyChangedWithValue<string>(value, "ClanText");
				}
			}
		}

		// Token: 0x1700025E RID: 606
		// (get) Token: 0x0600084F RID: 2127 RVA: 0x00025F73 File Offset: 0x00024173
		// (set) Token: 0x06000850 RID: 2128 RVA: 0x00025F7B File Offset: 0x0002417B
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

		// Token: 0x1700025F RID: 607
		// (get) Token: 0x06000851 RID: 2129 RVA: 0x00025F9E File Offset: 0x0002419E
		// (set) Token: 0x06000852 RID: 2130 RVA: 0x00025FA6 File Offset: 0x000241A6
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

		// Token: 0x17000260 RID: 608
		// (get) Token: 0x06000853 RID: 2131 RVA: 0x00025FC9 File Offset: 0x000241C9
		// (set) Token: 0x06000854 RID: 2132 RVA: 0x00025FD1 File Offset: 0x000241D1
		[DataSourceProperty]
		public string MilitiaText
		{
			get
			{
				return this._militiaText;
			}
			set
			{
				if (value != this._militiaText)
				{
					this._militiaText = value;
					base.OnPropertyChangedWithValue<string>(value, "MilitiaText");
				}
			}
		}

		// Token: 0x17000261 RID: 609
		// (get) Token: 0x06000855 RID: 2133 RVA: 0x00025FF4 File Offset: 0x000241F4
		// (set) Token: 0x06000856 RID: 2134 RVA: 0x00025FFC File Offset: 0x000241FC
		[DataSourceProperty]
		public string AnnexText
		{
			get
			{
				return this._annexText;
			}
			set
			{
				if (value != this._annexText)
				{
					this._annexText = value;
					base.OnPropertyChangedWithValue<string>(value, "AnnexText");
				}
			}
		}

		// Token: 0x17000262 RID: 610
		// (get) Token: 0x06000857 RID: 2135 RVA: 0x0002601F File Offset: 0x0002421F
		// (set) Token: 0x06000858 RID: 2136 RVA: 0x00026027 File Offset: 0x00024227
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

		// Token: 0x17000263 RID: 611
		// (get) Token: 0x06000859 RID: 2137 RVA: 0x0002604A File Offset: 0x0002424A
		// (set) Token: 0x0600085A RID: 2138 RVA: 0x00026052 File Offset: 0x00024252
		[DataSourceProperty]
		public int AnnexCost
		{
			get
			{
				return this._annexCost;
			}
			set
			{
				if (value != this._annexCost)
				{
					this._annexCost = value;
					base.OnPropertyChangedWithValue(value, "AnnexCost");
				}
			}
		}

		// Token: 0x17000264 RID: 612
		// (get) Token: 0x0600085B RID: 2139 RVA: 0x00026070 File Offset: 0x00024270
		// (set) Token: 0x0600085C RID: 2140 RVA: 0x00026078 File Offset: 0x00024278
		[DataSourceProperty]
		public string DefendersText
		{
			get
			{
				return this._defendersText;
			}
			set
			{
				if (value != this._defendersText)
				{
					this._defendersText = value;
					base.OnPropertyChangedWithValue<string>(value, "DefendersText");
				}
			}
		}

		// Token: 0x17000265 RID: 613
		// (get) Token: 0x0600085D RID: 2141 RVA: 0x0002609B File Offset: 0x0002429B
		// (set) Token: 0x0600085E RID: 2142 RVA: 0x000260A3 File Offset: 0x000242A3
		[DataSourceProperty]
		public MBBindingList<KingdomSettlementItemVM> Settlements
		{
			get
			{
				return this._settlements;
			}
			set
			{
				if (value != this._settlements)
				{
					this._settlements = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomSettlementItemVM>>(value, "Settlements");
				}
			}
		}

		// Token: 0x17000266 RID: 614
		// (get) Token: 0x0600085F RID: 2143 RVA: 0x000260C1 File Offset: 0x000242C1
		// (set) Token: 0x06000860 RID: 2144 RVA: 0x000260C9 File Offset: 0x000242C9
		[DataSourceProperty]
		public bool CanAnnexCurrentSettlement
		{
			get
			{
				return this._canAnnexCurrentSettlement;
			}
			set
			{
				if (value != this._canAnnexCurrentSettlement)
				{
					this._canAnnexCurrentSettlement = value;
					base.OnPropertyChangedWithValue(value, "CanAnnexCurrentSettlement");
				}
			}
		}

		// Token: 0x17000267 RID: 615
		// (get) Token: 0x06000861 RID: 2145 RVA: 0x000260E7 File Offset: 0x000242E7
		// (set) Token: 0x06000862 RID: 2146 RVA: 0x000260EF File Offset: 0x000242EF
		[DataSourceProperty]
		public bool HasCost
		{
			get
			{
				return this._hasCost;
			}
			set
			{
				if (value != this._hasCost)
				{
					this._hasCost = value;
					base.OnPropertyChangedWithValue(value, "HasCost");
				}
			}
		}

		// Token: 0x06000863 RID: 2147 RVA: 0x0002610D File Offset: 0x0002430D
		private static int CalculateLikelihood(Settlement settlement)
		{
			return MathF.Round(new KingdomElection(new SettlementClaimantPreliminaryDecision(Clan.PlayerClan, settlement)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x0400038A RID: 906
		private readonly Action<KingdomDecision> _forceDecision;

		// Token: 0x0400038B RID: 907
		private readonly Action<Settlement> _onGrantFief;

		// Token: 0x0400038C RID: 908
		private readonly Kingdom _kingdom;

		// Token: 0x0400038D RID: 909
		private KingdomDecision _currenItemsUnresolvedDecision;

		// Token: 0x0400038E RID: 910
		private MBBindingList<KingdomSettlementItemVM> _settlements;

		// Token: 0x0400038F RID: 911
		private KingdomSettlementItemVM _currentSelectedSettlement;

		// Token: 0x04000390 RID: 912
		private HintViewModel _annexHint;

		// Token: 0x04000391 RID: 913
		private string _ownerText;

		// Token: 0x04000392 RID: 914
		private string _nameText;

		// Token: 0x04000393 RID: 915
		private string _typeText;

		// Token: 0x04000394 RID: 916
		private string _prosperityText;

		// Token: 0x04000395 RID: 917
		private string _foodText;

		// Token: 0x04000396 RID: 918
		private string _garrisonText;

		// Token: 0x04000397 RID: 919
		private string _militiaText;

		// Token: 0x04000398 RID: 920
		private string _annexText;

		// Token: 0x04000399 RID: 921
		private string _clanText;

		// Token: 0x0400039A RID: 922
		private string _villagesText;

		// Token: 0x0400039B RID: 923
		private string _annexActionExplanationText;

		// Token: 0x0400039C RID: 924
		private string _proposeText;

		// Token: 0x0400039D RID: 925
		private string _defendersText;

		// Token: 0x0400039E RID: 926
		private int _annexCost;

		// Token: 0x0400039F RID: 927
		private bool _canAnnexCurrentSettlement;

		// Token: 0x040003A0 RID: 928
		private bool _hasCost;

		// Token: 0x040003A1 RID: 929
		private KingdomSettlementSortControllerVM _settlementSortController;
	}
}
