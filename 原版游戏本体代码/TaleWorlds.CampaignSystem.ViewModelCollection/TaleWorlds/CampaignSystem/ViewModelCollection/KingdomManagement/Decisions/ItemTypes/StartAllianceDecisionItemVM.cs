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
	// Token: 0x02000083 RID: 131
	public class StartAllianceDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x17000372 RID: 882
		// (get) Token: 0x06000AFD RID: 2813 RVA: 0x0002EA61 File Offset: 0x0002CC61
		private Kingdom _sourceFaction
		{
			get
			{
				return Hero.MainHero.Clan.Kingdom;
			}
		}

		// Token: 0x17000373 RID: 883
		// (get) Token: 0x06000AFE RID: 2814 RVA: 0x0002EA72 File Offset: 0x0002CC72
		public IFaction TargetFaction
		{
			get
			{
				return (this._decision as StartAllianceDecision).KingdomToStartAllianceWith;
			}
		}

		// Token: 0x06000AFF RID: 2815 RVA: 0x0002EA84 File Offset: 0x0002CC84
		public StartAllianceDecisionItemVM(StartAllianceDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._startAllianceDecision = decision;
			base.DecisionType = 7;
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x0002EA9C File Offset: 0x0002CC9C
		protected override void InitValues()
		{
			base.InitValues();
			TextObject textObject = GameTexts.FindText("str_kingdom_decision_start_alliance", null);
			this.NameText = textObject.ToString();
			TextObject textObject2 = GameTexts.FindText("str_kingdom_decision_start_alliance_desc", null);
			textObject2.SetTextVariable("FACTION", this.TargetFaction.Name);
			this.StartAllianceDescriptionText = textObject2.ToString();
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

		// Token: 0x17000374 RID: 884
		// (get) Token: 0x06000B01 RID: 2817 RVA: 0x0002EE88 File Offset: 0x0002D088
		// (set) Token: 0x06000B02 RID: 2818 RVA: 0x0002EE90 File Offset: 0x0002D090
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

		// Token: 0x17000375 RID: 885
		// (get) Token: 0x06000B03 RID: 2819 RVA: 0x0002EEB3 File Offset: 0x0002D0B3
		// (set) Token: 0x06000B04 RID: 2820 RVA: 0x0002EEBB File Offset: 0x0002D0BB
		[DataSourceProperty]
		public string StartAllianceDescriptionText
		{
			get
			{
				return this._startAllianceDescriptionText;
			}
			set
			{
				if (value != this._startAllianceDescriptionText)
				{
					this._startAllianceDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "StartAllianceDescriptionText");
				}
			}
		}

		// Token: 0x17000376 RID: 886
		// (get) Token: 0x06000B05 RID: 2821 RVA: 0x0002EEDE File Offset: 0x0002D0DE
		// (set) Token: 0x06000B06 RID: 2822 RVA: 0x0002EEE6 File Offset: 0x0002D0E6
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

		// Token: 0x17000377 RID: 887
		// (get) Token: 0x06000B07 RID: 2823 RVA: 0x0002EF04 File Offset: 0x0002D104
		// (set) Token: 0x06000B08 RID: 2824 RVA: 0x0002EF0C File Offset: 0x0002D10C
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

		// Token: 0x17000378 RID: 888
		// (get) Token: 0x06000B09 RID: 2825 RVA: 0x0002EF2A File Offset: 0x0002D12A
		// (set) Token: 0x06000B0A RID: 2826 RVA: 0x0002EF32 File Offset: 0x0002D132
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

		// Token: 0x17000379 RID: 889
		// (get) Token: 0x06000B0B RID: 2827 RVA: 0x0002EF50 File Offset: 0x0002D150
		// (set) Token: 0x06000B0C RID: 2828 RVA: 0x0002EF58 File Offset: 0x0002D158
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

		// Token: 0x1700037A RID: 890
		// (get) Token: 0x06000B0D RID: 2829 RVA: 0x0002EF7B File Offset: 0x0002D17B
		// (set) Token: 0x06000B0E RID: 2830 RVA: 0x0002EF83 File Offset: 0x0002D183
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

		// Token: 0x1700037B RID: 891
		// (get) Token: 0x06000B0F RID: 2831 RVA: 0x0002EFA1 File Offset: 0x0002D1A1
		// (set) Token: 0x06000B10 RID: 2832 RVA: 0x0002EFA9 File Offset: 0x0002D1A9
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

		// Token: 0x1700037C RID: 892
		// (get) Token: 0x06000B11 RID: 2833 RVA: 0x0002EFC7 File Offset: 0x0002D1C7
		// (set) Token: 0x06000B12 RID: 2834 RVA: 0x0002EFCF File Offset: 0x0002D1CF
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

		// Token: 0x1700037D RID: 893
		// (get) Token: 0x06000B13 RID: 2835 RVA: 0x0002EFED File Offset: 0x0002D1ED
		// (set) Token: 0x06000B14 RID: 2836 RVA: 0x0002EFF5 File Offset: 0x0002D1F5
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

		// Token: 0x040004DE RID: 1246
		private readonly StartAllianceDecision _startAllianceDecision;

		// Token: 0x040004DF RID: 1247
		private string _nameText;

		// Token: 0x040004E0 RID: 1248
		private string _startAllianceDescriptionText;

		// Token: 0x040004E1 RID: 1249
		private BannerImageIdentifierVM _sourceFactionBanner;

		// Token: 0x040004E2 RID: 1250
		private BannerImageIdentifierVM _targetFactionBanner;

		// Token: 0x040004E3 RID: 1251
		private string _leaderText;

		// Token: 0x040004E4 RID: 1252
		private HeroVM _sourceFactionLeader;

		// Token: 0x040004E5 RID: 1253
		private HeroVM _targetFactionLeader;

		// Token: 0x040004E6 RID: 1254
		private MBBindingList<KingdomWarComparableStatVM> _comparedStats;

		// Token: 0x040004E7 RID: 1255
		private bool _isTargetFactionOtherWarsVisible;

		// Token: 0x040004E8 RID: 1256
		private MBBindingList<KingdomDiplomacyFactionItemVM> _targetFactionOtherWars;
	}
}
