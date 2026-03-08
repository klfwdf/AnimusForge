using System;
using Helpers;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x02000079 RID: 121
	public class AcceptingCallToWarAgreementDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x170002F2 RID: 754
		// (get) Token: 0x060009DC RID: 2524 RVA: 0x0002AEEB File Offset: 0x000290EB
		private Kingdom _callingKingdom
		{
			get
			{
				return (this._decision as AcceptCallToWarAgreementDecision).CallingKingdom;
			}
		}

		// Token: 0x170002F3 RID: 755
		// (get) Token: 0x060009DD RID: 2525 RVA: 0x0002AEFD File Offset: 0x000290FD
		public IFaction TargetFaction
		{
			get
			{
				return (this._decision as AcceptCallToWarAgreementDecision).KingdomToCallToWarAgainst;
			}
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x0002AF0F File Offset: 0x0002910F
		public AcceptingCallToWarAgreementDecisionItemVM(AcceptCallToWarAgreementDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._callToWarAgreementDecision = decision;
			base.DecisionType = 8;
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x0002AF28 File Offset: 0x00029128
		protected override void InitValues()
		{
			base.InitValues();
			TextObject textObject = GameTexts.FindText("str_kingdom_decision_accept_call_to_war_agreement", null);
			this.NameText = textObject.ToString();
			TextObject textObject2 = GameTexts.FindText("str_kingdom_decision_accept_call_to_war_agreement_desc", null);
			textObject2.SetTextVariable("CALLING_KINGDOM", this._callingKingdom.Name);
			textObject2.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", this.TargetFaction.Name);
			this.AcceptCallToWarAgreementDescriptionText = textObject2.ToString();
			this.SourceFactionBanner = new BannerImageIdentifierVM(this._callingKingdom.Banner, true);
			this.TargetFactionBanner = new BannerImageIdentifierVM(this.TargetFaction.Banner, true);
			this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
			this.SourceFactionLeader = new HeroVM(this._callingKingdom.Leader, false);
			this.TargetFactionLeader = new HeroVM(this.TargetFaction.Leader, false);
			this.ComparedStats = new MBBindingList<KingdomWarComparableStatVM>();
			Kingdom kingdom = this.TargetFaction as Kingdom;
			string faction1Color = Color.FromUint(this._callingKingdom.Color).ToString();
			string faction2Color = Color.FromUint(kingdom.Color).ToString();
			KingdomWarComparableStatVM item = new KingdomWarComparableStatVM((int)this._callingKingdom.CurrentTotalStrength, (int)kingdom.CurrentTotalStrength, GameTexts.FindText("str_strength", null), faction1Color, faction2Color, 10000, null, null);
			this.ComparedStats.Add(item);
			this.TargetFactionOtherWars = new MBBindingList<KingdomDiplomacyFactionItemVM>();
			foreach (StanceLink stanceLink in FactionHelper.GetStances(this.TargetFaction))
			{
				if (stanceLink.IsAtWar && stanceLink.Faction1 != this._callingKingdom && stanceLink.Faction2 != this._callingKingdom && (stanceLink.Faction1.IsKingdomFaction || stanceLink.Faction1.Leader == Hero.MainHero) && (stanceLink.Faction2.IsKingdomFaction || stanceLink.Faction2.Leader == Hero.MainHero) && !stanceLink.Faction1.IsRebelClan && !stanceLink.Faction2.IsRebelClan && !stanceLink.Faction1.IsBanditFaction && !stanceLink.Faction2.IsBanditFaction)
				{
					this.TargetFactionOtherWars.Add(new KingdomDiplomacyFactionItemVM((stanceLink.Faction1 == this.TargetFaction) ? stanceLink.Faction2 : stanceLink.Faction1));
				}
			}
			this.IsTargetFactionOtherWarsVisible = this.TargetFactionOtherWars.Count > 0;
		}

		// Token: 0x170002F4 RID: 756
		// (get) Token: 0x060009E0 RID: 2528 RVA: 0x0002B1E0 File Offset: 0x000293E0
		// (set) Token: 0x060009E1 RID: 2529 RVA: 0x0002B1E8 File Offset: 0x000293E8
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

		// Token: 0x170002F5 RID: 757
		// (get) Token: 0x060009E2 RID: 2530 RVA: 0x0002B20B File Offset: 0x0002940B
		// (set) Token: 0x060009E3 RID: 2531 RVA: 0x0002B213 File Offset: 0x00029413
		[DataSourceProperty]
		public string AcceptCallToWarAgreementDescriptionText
		{
			get
			{
				return this._callToWarAgreementDescriptionText;
			}
			set
			{
				if (value != this._callToWarAgreementDescriptionText)
				{
					this._callToWarAgreementDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "AcceptCallToWarAgreementDescriptionText");
				}
			}
		}

		// Token: 0x170002F6 RID: 758
		// (get) Token: 0x060009E4 RID: 2532 RVA: 0x0002B236 File Offset: 0x00029436
		// (set) Token: 0x060009E5 RID: 2533 RVA: 0x0002B23E File Offset: 0x0002943E
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

		// Token: 0x170002F7 RID: 759
		// (get) Token: 0x060009E6 RID: 2534 RVA: 0x0002B25C File Offset: 0x0002945C
		// (set) Token: 0x060009E7 RID: 2535 RVA: 0x0002B264 File Offset: 0x00029464
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

		// Token: 0x170002F8 RID: 760
		// (get) Token: 0x060009E8 RID: 2536 RVA: 0x0002B282 File Offset: 0x00029482
		// (set) Token: 0x060009E9 RID: 2537 RVA: 0x0002B28A File Offset: 0x0002948A
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

		// Token: 0x170002F9 RID: 761
		// (get) Token: 0x060009EA RID: 2538 RVA: 0x0002B2A8 File Offset: 0x000294A8
		// (set) Token: 0x060009EB RID: 2539 RVA: 0x0002B2B0 File Offset: 0x000294B0
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

		// Token: 0x170002FA RID: 762
		// (get) Token: 0x060009EC RID: 2540 RVA: 0x0002B2D3 File Offset: 0x000294D3
		// (set) Token: 0x060009ED RID: 2541 RVA: 0x0002B2DB File Offset: 0x000294DB
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

		// Token: 0x170002FB RID: 763
		// (get) Token: 0x060009EE RID: 2542 RVA: 0x0002B2F9 File Offset: 0x000294F9
		// (set) Token: 0x060009EF RID: 2543 RVA: 0x0002B301 File Offset: 0x00029501
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

		// Token: 0x170002FC RID: 764
		// (get) Token: 0x060009F0 RID: 2544 RVA: 0x0002B31F File Offset: 0x0002951F
		// (set) Token: 0x060009F1 RID: 2545 RVA: 0x0002B327 File Offset: 0x00029527
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

		// Token: 0x170002FD RID: 765
		// (get) Token: 0x060009F2 RID: 2546 RVA: 0x0002B345 File Offset: 0x00029545
		// (set) Token: 0x060009F3 RID: 2547 RVA: 0x0002B34D File Offset: 0x0002954D
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

		// Token: 0x0400045C RID: 1116
		private readonly AcceptCallToWarAgreementDecision _callToWarAgreementDecision;

		// Token: 0x0400045D RID: 1117
		private string _nameText;

		// Token: 0x0400045E RID: 1118
		private string _callToWarAgreementDescriptionText;

		// Token: 0x0400045F RID: 1119
		private BannerImageIdentifierVM _sourceFactionBanner;

		// Token: 0x04000460 RID: 1120
		private BannerImageIdentifierVM _targetFactionBanner;

		// Token: 0x04000461 RID: 1121
		private string _leaderText;

		// Token: 0x04000462 RID: 1122
		private HeroVM _sourceFactionLeader;

		// Token: 0x04000463 RID: 1123
		private HeroVM _targetFactionLeader;

		// Token: 0x04000464 RID: 1124
		private MBBindingList<KingdomWarComparableStatVM> _comparedStats;

		// Token: 0x04000465 RID: 1125
		private bool _isTargetFactionOtherWarsVisible;

		// Token: 0x04000466 RID: 1126
		private MBBindingList<KingdomDiplomacyFactionItemVM> _targetFactionOtherWars;
	}
}
