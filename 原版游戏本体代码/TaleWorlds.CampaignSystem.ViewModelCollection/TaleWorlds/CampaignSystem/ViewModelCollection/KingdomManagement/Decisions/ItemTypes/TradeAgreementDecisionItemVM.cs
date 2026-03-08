using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x02000084 RID: 132
	public class TradeAgreementDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x1700037E RID: 894
		// (get) Token: 0x06000B15 RID: 2837 RVA: 0x0002F013 File Offset: 0x0002D213
		private Kingdom _sourceFaction
		{
			get
			{
				return Hero.MainHero.Clan.Kingdom;
			}
		}

		// Token: 0x1700037F RID: 895
		// (get) Token: 0x06000B16 RID: 2838 RVA: 0x0002F024 File Offset: 0x0002D224
		public IFaction TargetFaction
		{
			get
			{
				return (this._decision as TradeAgreementDecision).TargetKingdom;
			}
		}

		// Token: 0x06000B17 RID: 2839 RVA: 0x0002F036 File Offset: 0x0002D236
		public TradeAgreementDecisionItemVM(TradeAgreementDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._tradeAgreementDecision = decision;
			base.DecisionType = 10;
		}

		// Token: 0x06000B18 RID: 2840 RVA: 0x0002F050 File Offset: 0x0002D250
		protected override void InitValues()
		{
			base.InitValues();
			TextObject textObject = GameTexts.FindText("str_kingdom_decision_trade_agreement", null);
			this.NameText = textObject.ToString();
			TextObject textObject2 = GameTexts.FindText("str_kingdom_decision_trade_agreement_desc", null);
			textObject2.SetTextVariable("FACTION", this.TargetFaction.Name);
			this.TradeAgreementDescriptionText = textObject2.ToString();
			this.SourceFactionBanner = new BannerImageIdentifierVM(this._sourceFaction.Banner, true);
			this.TargetFactionBanner = new BannerImageIdentifierVM(this.TargetFaction.Banner, true);
			this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
			this.SourceFactionLeader = new HeroVM(this._sourceFaction.Leader, false);
			this.TargetFactionLeader = new HeroVM(this.TargetFaction.Leader, false);
			this.ComparedStats = new MBBindingList<KingdomWarComparableStatVM>();
			Kingdom kingdom = this.TargetFaction as Kingdom;
			string faction1Color = Color.FromUint(this._sourceFaction.Color).ToString();
			string faction2Color = Color.FromUint(kingdom.Color).ToString();
			KingdomWarComparableStatVM item = new KingdomWarComparableStatVM((int)this._sourceFaction.CurrentTotalStrength, (int)kingdom.CurrentTotalStrength, GameTexts.FindText("str_strength", null), faction1Color, faction2Color, 10000, null, null);
			this.ComparedStats.Add(item);
			KingdomWarComparableStatVM item2 = new KingdomWarComparableStatVM(this._sourceFaction.Armies.Count, kingdom.Armies.Count, GameTexts.FindText("str_armies", null), faction1Color, faction2Color, 5, null, null);
			this.ComparedStats.Add(item2);
			int faction1Stat = this._sourceFaction.Settlements.Count((Settlement settlement) => settlement.IsTown);
			int faction2Stat = kingdom.Settlements.Count((Settlement settlement) => settlement.IsTown);
			KingdomWarComparableStatVM item3 = new KingdomWarComparableStatVM(faction1Stat, faction2Stat, GameTexts.FindText("str_towns", null), faction1Color, faction2Color, 50, null, null);
			this.ComparedStats.Add(item3);
			int faction1Stat2 = this._sourceFaction.Settlements.Count((Settlement settlement) => settlement.IsCastle);
			int faction2Stat2 = this.TargetFaction.Settlements.Count((Settlement settlement) => settlement.IsCastle);
			KingdomWarComparableStatVM item4 = new KingdomWarComparableStatVM(faction1Stat2, faction2Stat2, GameTexts.FindText("str_castles", null), faction1Color, faction2Color, 50, null, null);
			this.ComparedStats.Add(item4);
			this.TargetFactionOtherWars = new MBBindingList<KingdomDiplomacyFactionItemVM>();
			foreach (StanceLink stanceLink in FactionHelper.GetStances(this.TargetFaction))
			{
				if (stanceLink.IsAtWar && stanceLink.Faction1 != this._sourceFaction && stanceLink.Faction2 != this._sourceFaction && (stanceLink.Faction1.IsKingdomFaction || stanceLink.Faction1.Leader == Hero.MainHero) && (stanceLink.Faction2.IsKingdomFaction || stanceLink.Faction2.Leader == Hero.MainHero) && !stanceLink.Faction1.IsRebelClan && !stanceLink.Faction2.IsRebelClan && !stanceLink.Faction1.IsBanditFaction && !stanceLink.Faction2.IsBanditFaction)
				{
					this.TargetFactionOtherWars.Add(new KingdomDiplomacyFactionItemVM((stanceLink.Faction1 == this.TargetFaction) ? stanceLink.Faction2 : stanceLink.Faction1));
				}
			}
			this.IsTargetFactionOtherWarsVisible = this.TargetFactionOtherWars.Count > 0;
		}

		// Token: 0x17000380 RID: 896
		// (get) Token: 0x06000B19 RID: 2841 RVA: 0x0002F43C File Offset: 0x0002D63C
		// (set) Token: 0x06000B1A RID: 2842 RVA: 0x0002F444 File Offset: 0x0002D644
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

		// Token: 0x17000381 RID: 897
		// (get) Token: 0x06000B1B RID: 2843 RVA: 0x0002F467 File Offset: 0x0002D667
		// (set) Token: 0x06000B1C RID: 2844 RVA: 0x0002F46F File Offset: 0x0002D66F
		[DataSourceProperty]
		public string TradeAgreementDescriptionText
		{
			get
			{
				return this._tradeAgreementDescriptionText;
			}
			set
			{
				if (value != this._tradeAgreementDescriptionText)
				{
					this._tradeAgreementDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "TradeAgreementDescriptionText");
				}
			}
		}

		// Token: 0x17000382 RID: 898
		// (get) Token: 0x06000B1D RID: 2845 RVA: 0x0002F492 File Offset: 0x0002D692
		// (set) Token: 0x06000B1E RID: 2846 RVA: 0x0002F49A File Offset: 0x0002D69A
		[DataSourceProperty]
		public BannerImageIdentifierVM SourceFactionBanner
		{
			get
			{
				return this._sourceFactionBanner;
			}
			set
			{
				if (value != this._sourceFactionBanner)
				{
					this._sourceFactionBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "SourceFactionBanner");
				}
			}
		}

		// Token: 0x17000383 RID: 899
		// (get) Token: 0x06000B1F RID: 2847 RVA: 0x0002F4B8 File Offset: 0x0002D6B8
		// (set) Token: 0x06000B20 RID: 2848 RVA: 0x0002F4C0 File Offset: 0x0002D6C0
		[DataSourceProperty]
		public BannerImageIdentifierVM TargetFactionBanner
		{
			get
			{
				return this._targetFactionBanner;
			}
			set
			{
				if (value != this._targetFactionBanner)
				{
					this._targetFactionBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "TargetFactionBanner");
				}
			}
		}

		// Token: 0x17000384 RID: 900
		// (get) Token: 0x06000B21 RID: 2849 RVA: 0x0002F4DE File Offset: 0x0002D6DE
		// (set) Token: 0x06000B22 RID: 2850 RVA: 0x0002F4E6 File Offset: 0x0002D6E6
		[DataSourceProperty]
		public MBBindingList<KingdomWarComparableStatVM> ComparedStats
		{
			get
			{
				return this._comparedStats;
			}
			set
			{
				if (value != this._comparedStats)
				{
					this._comparedStats = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomWarComparableStatVM>>(value, "ComparedStats");
				}
			}
		}

		// Token: 0x17000385 RID: 901
		// (get) Token: 0x06000B23 RID: 2851 RVA: 0x0002F504 File Offset: 0x0002D704
		// (set) Token: 0x06000B24 RID: 2852 RVA: 0x0002F50C File Offset: 0x0002D70C
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

		// Token: 0x17000386 RID: 902
		// (get) Token: 0x06000B25 RID: 2853 RVA: 0x0002F52F File Offset: 0x0002D72F
		// (set) Token: 0x06000B26 RID: 2854 RVA: 0x0002F537 File Offset: 0x0002D737
		[DataSourceProperty]
		public HeroVM SourceFactionLeader
		{
			get
			{
				return this._sourceFactionLeader;
			}
			set
			{
				if (value != this._sourceFactionLeader)
				{
					this._sourceFactionLeader = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "SourceFactionLeader");
				}
			}
		}

		// Token: 0x17000387 RID: 903
		// (get) Token: 0x06000B27 RID: 2855 RVA: 0x0002F555 File Offset: 0x0002D755
		// (set) Token: 0x06000B28 RID: 2856 RVA: 0x0002F55D File Offset: 0x0002D75D
		[DataSourceProperty]
		public HeroVM TargetFactionLeader
		{
			get
			{
				return this._targetFactionLeader;
			}
			set
			{
				if (value != this._targetFactionLeader)
				{
					this._targetFactionLeader = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "TargetFactionLeader");
				}
			}
		}

		// Token: 0x17000388 RID: 904
		// (get) Token: 0x06000B29 RID: 2857 RVA: 0x0002F57B File Offset: 0x0002D77B
		// (set) Token: 0x06000B2A RID: 2858 RVA: 0x0002F583 File Offset: 0x0002D783
		[DataSourceProperty]
		public bool IsTargetFactionOtherWarsVisible
		{
			get
			{
				return this._isTargetFactionOtherWarsVisible;
			}
			set
			{
				if (value != this._isTargetFactionOtherWarsVisible)
				{
					this._isTargetFactionOtherWarsVisible = value;
					base.OnPropertyChangedWithValue(value, "IsTargetFactionOtherWarsVisible");
				}
			}
		}

		// Token: 0x17000389 RID: 905
		// (get) Token: 0x06000B2B RID: 2859 RVA: 0x0002F5A1 File Offset: 0x0002D7A1
		// (set) Token: 0x06000B2C RID: 2860 RVA: 0x0002F5A9 File Offset: 0x0002D7A9
		[DataSourceProperty]
		public MBBindingList<KingdomDiplomacyFactionItemVM> TargetFactionOtherWars
		{
			get
			{
				return this._targetFactionOtherWars;
			}
			set
			{
				if (value != this._targetFactionOtherWars)
				{
					this._targetFactionOtherWars = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomDiplomacyFactionItemVM>>(value, "TargetFactionOtherWars");
				}
			}
		}

		// Token: 0x040004E9 RID: 1257
		private readonly TradeAgreementDecision _tradeAgreementDecision;

		// Token: 0x040004EA RID: 1258
		private string _nameText;

		// Token: 0x040004EB RID: 1259
		private string _tradeAgreementDescriptionText;

		// Token: 0x040004EC RID: 1260
		private BannerImageIdentifierVM _sourceFactionBanner;

		// Token: 0x040004ED RID: 1261
		private BannerImageIdentifierVM _targetFactionBanner;

		// Token: 0x040004EE RID: 1262
		private string _leaderText;

		// Token: 0x040004EF RID: 1263
		private HeroVM _sourceFactionLeader;

		// Token: 0x040004F0 RID: 1264
		private HeroVM _targetFactionLeader;

		// Token: 0x040004F1 RID: 1265
		private MBBindingList<KingdomWarComparableStatVM> _comparedStats;

		// Token: 0x040004F2 RID: 1266
		private bool _isTargetFactionOtherWarsVisible;

		// Token: 0x040004F3 RID: 1267
		private MBBindingList<KingdomDiplomacyFactionItemVM> _targetFactionOtherWars;
	}
}
