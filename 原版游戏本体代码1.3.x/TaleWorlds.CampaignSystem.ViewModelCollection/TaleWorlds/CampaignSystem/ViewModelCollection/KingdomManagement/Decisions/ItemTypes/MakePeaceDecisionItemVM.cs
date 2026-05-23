using System;
using Helpers;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x0200007F RID: 127
	public class MakePeaceDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x17000335 RID: 821
		// (get) Token: 0x06000A7B RID: 2683 RVA: 0x0002D365 File Offset: 0x0002B565
		private Kingdom _sourceFaction
		{
			get
			{
				return Hero.MainHero.Clan.Kingdom;
			}
		}

		// Token: 0x17000336 RID: 822
		// (get) Token: 0x06000A7C RID: 2684 RVA: 0x0002D376 File Offset: 0x0002B576
		public IFaction TargetFaction
		{
			get
			{
				return (this._decision as MakePeaceKingdomDecision).FactionToMakePeaceWith;
			}
		}

		// Token: 0x06000A7D RID: 2685 RVA: 0x0002D388 File Offset: 0x0002B588
		public MakePeaceDecisionItemVM(MakePeaceKingdomDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._makePeaceDecision = decision;
			base.DecisionType = 5;
		}

		// Token: 0x06000A7E RID: 2686 RVA: 0x0002D3A0 File Offset: 0x0002B5A0
		protected override void InitValues()
		{
			base.InitValues();
			TextObject textObject = GameTexts.FindText("str_kingdom_decision_make_peace", null);
			this.NameText = textObject.ToString();
			TextObject textObject2 = GameTexts.FindText("str_kingdom_decision_make_peace_desc", null);
			textObject2.SetTextVariable("FACTION", this.TargetFaction.Name);
			this.PeaceDescriptionText = textObject2.ToString();
			this.SourceFactionBanner = new BannerImageIdentifierVM(this._sourceFaction.Banner, true);
			this.TargetFactionBanner = new BannerImageIdentifierVM(this.TargetFaction.Banner, true);
			this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
			this.SourceFactionLeader = new HeroVM(this._sourceFaction.Leader, false);
			this.TargetFactionLeader = new HeroVM(this.TargetFaction.Leader, false);
			this.ComparedStats = new MBBindingList<KingdomWarComparableStatVM>();
			Kingdom targetFaction = this.TargetFaction as Kingdom;
			string faction1Color = Color.FromUint(this._sourceFaction.Color).ToString();
			string faction2Color = Color.FromUint(targetFaction.Color).ToString();
			StanceLink stanceWith = this._sourceFaction.GetStanceWith(this.TargetFaction);
			KingdomWarComparableStatVM item = new KingdomWarComparableStatVM((int)this._sourceFaction.CurrentTotalStrength, (int)targetFaction.CurrentTotalStrength, GameTexts.FindText("str_strength", null), faction1Color, faction2Color, 10000, null, null);
			this.ComparedStats.Add(item);
			KingdomWarComparableStatVM item2 = new KingdomWarComparableStatVM(stanceWith.GetCasualties(targetFaction), stanceWith.GetCasualties(this._sourceFaction), GameTexts.FindText("str_war_casualties_inflicted", null), faction1Color, faction2Color, 10000, null, null);
			this.ComparedStats.Add(item2);
			KingdomWarComparableStatVM item3 = new KingdomWarComparableStatVM(stanceWith.GetSuccessfulSieges(this._sourceFaction), stanceWith.GetSuccessfulSieges(targetFaction), GameTexts.FindText("str_war_successful_sieges", null), faction1Color, faction2Color, 5, null, null);
			this.ComparedStats.Add(item3);
			KingdomWarComparableStatVM item4 = new KingdomWarComparableStatVM(stanceWith.GetSuccessfulRaids(this._sourceFaction), stanceWith.GetSuccessfulRaids(targetFaction), GameTexts.FindText("str_war_successful_raids", null), faction1Color, faction2Color, 10, null, null);
			this.ComparedStats.Add(item4);
			ExplainedNumber warProgressOfFaction1 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(this._sourceFaction, targetFaction, true);
			ExplainedNumber warProgressOfFaction2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(targetFaction, this._sourceFaction, true);
			int num = (int)(warProgressOfFaction1.ResultNumber * 100f / warProgressOfFaction1.LimitMaxValue);
			int num2 = (int)(warProgressOfFaction2.ResultNumber * 100f / warProgressOfFaction2.LimitMaxValue);
			int faction1Stat = MathF.Max(0, num - num2);
			int faction2Stat = MathF.Max(0, num2 - num);
			KingdomWarComparableStatVM item5 = new KingdomWarComparableStatVM(faction1Stat, faction2Stat, new TextObject("{=8qbkS5D2}War Progress", null), faction1Color, faction2Color, 100, new BasicTooltipViewModel(() => CampaignUIHelper.GetNormalizedWarProgressTooltip(warProgressOfFaction1, warProgressOfFaction2, warProgressOfFaction1.LimitMaxValue, this._sourceFaction.Name, targetFaction.Name)), new BasicTooltipViewModel(() => CampaignUIHelper.GetNormalizedWarProgressTooltip(warProgressOfFaction2, warProgressOfFaction1, warProgressOfFaction2.LimitMaxValue, targetFaction.Name, this._sourceFaction.Name)));
			this.ComparedStats.Add(item5);
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

		// Token: 0x17000337 RID: 823
		// (get) Token: 0x06000A7F RID: 2687 RVA: 0x0002D820 File Offset: 0x0002BA20
		// (set) Token: 0x06000A80 RID: 2688 RVA: 0x0002D828 File Offset: 0x0002BA28
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

		// Token: 0x17000338 RID: 824
		// (get) Token: 0x06000A81 RID: 2689 RVA: 0x0002D84B File Offset: 0x0002BA4B
		// (set) Token: 0x06000A82 RID: 2690 RVA: 0x0002D853 File Offset: 0x0002BA53
		[DataSourceProperty]
		public string PeaceDescriptionText
		{
			get
			{
				return this._peaceDescriptionText;
			}
			set
			{
				if (value != this._peaceDescriptionText)
				{
					this._peaceDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "PeaceDescriptionText");
				}
			}
		}

		// Token: 0x17000339 RID: 825
		// (get) Token: 0x06000A83 RID: 2691 RVA: 0x0002D876 File Offset: 0x0002BA76
		// (set) Token: 0x06000A84 RID: 2692 RVA: 0x0002D87E File Offset: 0x0002BA7E
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

		// Token: 0x1700033A RID: 826
		// (get) Token: 0x06000A85 RID: 2693 RVA: 0x0002D89C File Offset: 0x0002BA9C
		// (set) Token: 0x06000A86 RID: 2694 RVA: 0x0002D8A4 File Offset: 0x0002BAA4
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

		// Token: 0x1700033B RID: 827
		// (get) Token: 0x06000A87 RID: 2695 RVA: 0x0002D8C2 File Offset: 0x0002BAC2
		// (set) Token: 0x06000A88 RID: 2696 RVA: 0x0002D8CA File Offset: 0x0002BACA
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

		// Token: 0x1700033C RID: 828
		// (get) Token: 0x06000A89 RID: 2697 RVA: 0x0002D8E8 File Offset: 0x0002BAE8
		// (set) Token: 0x06000A8A RID: 2698 RVA: 0x0002D8F0 File Offset: 0x0002BAF0
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

		// Token: 0x1700033D RID: 829
		// (get) Token: 0x06000A8B RID: 2699 RVA: 0x0002D913 File Offset: 0x0002BB13
		// (set) Token: 0x06000A8C RID: 2700 RVA: 0x0002D91B File Offset: 0x0002BB1B
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

		// Token: 0x1700033E RID: 830
		// (get) Token: 0x06000A8D RID: 2701 RVA: 0x0002D939 File Offset: 0x0002BB39
		// (set) Token: 0x06000A8E RID: 2702 RVA: 0x0002D941 File Offset: 0x0002BB41
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

		// Token: 0x1700033F RID: 831
		// (get) Token: 0x06000A8F RID: 2703 RVA: 0x0002D95F File Offset: 0x0002BB5F
		// (set) Token: 0x06000A90 RID: 2704 RVA: 0x0002D967 File Offset: 0x0002BB67
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

		// Token: 0x17000340 RID: 832
		// (get) Token: 0x06000A91 RID: 2705 RVA: 0x0002D985 File Offset: 0x0002BB85
		// (set) Token: 0x06000A92 RID: 2706 RVA: 0x0002D98D File Offset: 0x0002BB8D
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

		// Token: 0x040004A2 RID: 1186
		private readonly MakePeaceKingdomDecision _makePeaceDecision;

		// Token: 0x040004A3 RID: 1187
		private string _nameText;

		// Token: 0x040004A4 RID: 1188
		private string _peaceDescriptionText;

		// Token: 0x040004A5 RID: 1189
		private BannerImageIdentifierVM _sourceFactionBanner;

		// Token: 0x040004A6 RID: 1190
		private BannerImageIdentifierVM _targetFactionBanner;

		// Token: 0x040004A7 RID: 1191
		private string _leaderText;

		// Token: 0x040004A8 RID: 1192
		private HeroVM _sourceFactionLeader;

		// Token: 0x040004A9 RID: 1193
		private HeroVM _targetFactionLeader;

		// Token: 0x040004AA RID: 1194
		private MBBindingList<KingdomWarComparableStatVM> _comparedStats;

		// Token: 0x040004AB RID: 1195
		private bool _isTargetFactionOtherWarsVisible;

		// Token: 0x040004AC RID: 1196
		private MBBindingList<KingdomDiplomacyFactionItemVM> _targetFactionOtherWars;
	}
}
