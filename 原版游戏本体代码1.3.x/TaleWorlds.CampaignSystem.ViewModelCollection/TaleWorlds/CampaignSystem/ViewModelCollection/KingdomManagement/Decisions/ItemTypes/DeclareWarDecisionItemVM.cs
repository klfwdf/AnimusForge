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
	// Token: 0x0200007B RID: 123
	public class DeclareWarDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x1700030F RID: 783
		// (get) Token: 0x06000A2A RID: 2602 RVA: 0x0002C255 File Offset: 0x0002A455
		private Kingdom _sourceFaction
		{
			get
			{
				return Hero.MainHero.Clan.Kingdom;
			}
		}

		// Token: 0x17000310 RID: 784
		// (get) Token: 0x06000A2B RID: 2603 RVA: 0x0002C266 File Offset: 0x0002A466
		public IFaction TargetFaction
		{
			get
			{
				return (this._decision as DeclareWarDecision).FactionToDeclareWarOn;
			}
		}

		// Token: 0x06000A2C RID: 2604 RVA: 0x0002C278 File Offset: 0x0002A478
		public DeclareWarDecisionItemVM(DeclareWarDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._declareWarDecision = decision;
			base.DecisionType = 4;
		}

		// Token: 0x06000A2D RID: 2605 RVA: 0x0002C290 File Offset: 0x0002A490
		protected override void InitValues()
		{
			base.InitValues();
			TextObject textObject = GameTexts.FindText("str_kingdom_decision_declare_war", null);
			this.NameText = textObject.ToString();
			TextObject textObject2 = GameTexts.FindText("str_kingdom_decision_declare_war_desc", null);
			textObject2.SetTextVariable("FACTION", this.TargetFaction.Name);
			this.WarDescriptionText = textObject2.ToString();
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

		// Token: 0x17000311 RID: 785
		// (get) Token: 0x06000A2E RID: 2606 RVA: 0x0002C67C File Offset: 0x0002A87C
		// (set) Token: 0x06000A2F RID: 2607 RVA: 0x0002C684 File Offset: 0x0002A884
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

		// Token: 0x17000312 RID: 786
		// (get) Token: 0x06000A30 RID: 2608 RVA: 0x0002C6A7 File Offset: 0x0002A8A7
		// (set) Token: 0x06000A31 RID: 2609 RVA: 0x0002C6AF File Offset: 0x0002A8AF
		[DataSourceProperty]
		public string WarDescriptionText
		{
			get
			{
				return this._warDescriptionText;
			}
			set
			{
				if (value != this._warDescriptionText)
				{
					this._warDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "WarDescriptionText");
				}
			}
		}

		// Token: 0x17000313 RID: 787
		// (get) Token: 0x06000A32 RID: 2610 RVA: 0x0002C6D2 File Offset: 0x0002A8D2
		// (set) Token: 0x06000A33 RID: 2611 RVA: 0x0002C6DA File Offset: 0x0002A8DA
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

		// Token: 0x17000314 RID: 788
		// (get) Token: 0x06000A34 RID: 2612 RVA: 0x0002C6F8 File Offset: 0x0002A8F8
		// (set) Token: 0x06000A35 RID: 2613 RVA: 0x0002C700 File Offset: 0x0002A900
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

		// Token: 0x17000315 RID: 789
		// (get) Token: 0x06000A36 RID: 2614 RVA: 0x0002C71E File Offset: 0x0002A91E
		// (set) Token: 0x06000A37 RID: 2615 RVA: 0x0002C726 File Offset: 0x0002A926
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

		// Token: 0x17000316 RID: 790
		// (get) Token: 0x06000A38 RID: 2616 RVA: 0x0002C744 File Offset: 0x0002A944
		// (set) Token: 0x06000A39 RID: 2617 RVA: 0x0002C74C File Offset: 0x0002A94C
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

		// Token: 0x17000317 RID: 791
		// (get) Token: 0x06000A3A RID: 2618 RVA: 0x0002C76F File Offset: 0x0002A96F
		// (set) Token: 0x06000A3B RID: 2619 RVA: 0x0002C777 File Offset: 0x0002A977
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

		// Token: 0x17000318 RID: 792
		// (get) Token: 0x06000A3C RID: 2620 RVA: 0x0002C795 File Offset: 0x0002A995
		// (set) Token: 0x06000A3D RID: 2621 RVA: 0x0002C79D File Offset: 0x0002A99D
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

		// Token: 0x17000319 RID: 793
		// (get) Token: 0x06000A3E RID: 2622 RVA: 0x0002C7BB File Offset: 0x0002A9BB
		// (set) Token: 0x06000A3F RID: 2623 RVA: 0x0002C7C3 File Offset: 0x0002A9C3
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

		// Token: 0x1700031A RID: 794
		// (get) Token: 0x06000A40 RID: 2624 RVA: 0x0002C7E1 File Offset: 0x0002A9E1
		// (set) Token: 0x06000A41 RID: 2625 RVA: 0x0002C7E9 File Offset: 0x0002A9E9
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

		// Token: 0x0400047E RID: 1150
		private readonly DeclareWarDecision _declareWarDecision;

		// Token: 0x0400047F RID: 1151
		private string _nameText;

		// Token: 0x04000480 RID: 1152
		private string _warDescriptionText;

		// Token: 0x04000481 RID: 1153
		private BannerImageIdentifierVM _sourceFactionBanner;

		// Token: 0x04000482 RID: 1154
		private BannerImageIdentifierVM _targetFactionBanner;

		// Token: 0x04000483 RID: 1155
		private string _leaderText;

		// Token: 0x04000484 RID: 1156
		private HeroVM _sourceFactionLeader;

		// Token: 0x04000485 RID: 1157
		private HeroVM _targetFactionLeader;

		// Token: 0x04000486 RID: 1158
		private MBBindingList<KingdomWarComparableStatVM> _comparedStats;

		// Token: 0x04000487 RID: 1159
		private bool _isTargetFactionOtherWarsVisible;

		// Token: 0x04000488 RID: 1160
		private MBBindingList<KingdomDiplomacyFactionItemVM> _targetFactionOtherWars;
	}
}
