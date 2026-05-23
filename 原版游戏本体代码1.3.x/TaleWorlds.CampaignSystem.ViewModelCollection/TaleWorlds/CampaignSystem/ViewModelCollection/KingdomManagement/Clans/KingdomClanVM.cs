using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans
{
	// Token: 0x02000088 RID: 136
	public class KingdomClanVM : KingdomCategoryVM
	{
		// Token: 0x06000B6A RID: 2922 RVA: 0x0002FF54 File Offset: 0x0002E154
		public KingdomClanVM(Action<KingdomDecision> forceDecide)
		{
			this._forceDecide = forceDecide;
			this.SupportHint = new HintViewModel();
			this.ExpelHint = new HintViewModel();
			this._clans = new MBBindingList<KingdomClanItemVM>();
			base.IsAcceptableItemSelected = false;
			this.RefreshClanList();
			base.NotificationCount = 0;
			this.SupportCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfSupportingClan();
			this.ExpelCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfExpellingClan(Clan.PlayerClan);
			TextObject hintText;
			this.CanSupportCurrentClan = this.GetCanSupportCurrentClanWithReason(this.SupportCost, out hintText);
			this.SupportHint.HintText = hintText;
			TextObject hintText2;
			this.CanExpelCurrentClan = this.GetCanExpelCurrentClanWithReason(this._isThereAPendingDecisionToExpelThisClan, this.ExpelCost, out hintText2);
			this.ExpelHint.HintText = hintText2;
			this.ClanSortController = new KingdomClanSortControllerVM(ref this._clans);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			this.RefreshValues();
		}

		// Token: 0x06000B6B RID: 2923 RVA: 0x00030050 File Offset: 0x0002E250
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SupportText = new TextObject("{=N63XYX2r}Support", null).ToString();
			this.NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
			this.InfluenceText = GameTexts.FindText("str_influence", null).ToString();
			this.FiefsText = GameTexts.FindText("str_fiefs", null).ToString();
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.BannerText = GameTexts.FindText("str_banner", null).ToString();
			this.TypeText = GameTexts.FindText("str_sort_by_type_label", null).ToString();
			base.CategoryNameText = new TextObject("{=j4F7tTzy}Clan", null).ToString();
			base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_clan_selected", null).ToString();
			this.SupportActionExplanationText = GameTexts.FindText("str_support_clan_action_explanation", null).ToString();
			this.ExpelActionExplanationText = GameTexts.FindText("str_expel_clan_action_explanation", null).ToString();
		}

		// Token: 0x06000B6C RID: 2924 RVA: 0x0003015C File Offset: 0x0002E35C
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
				this.SupportCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfSupportingClan();
				this._isThereAPendingDecisionToExpelThisClan = Clan.PlayerClan.Kingdom.UnresolvedDecisions.Any(delegate(KingdomDecision x)
				{
					ExpelClanFromKingdomDecision expelClanFromKingdomDecision;
					return (expelClanFromKingdomDecision = x as ExpelClanFromKingdomDecision) != null && expelClanFromKingdomDecision.ClanToExpel == this.CurrentSelectedClan.Clan && !x.ShouldBeCancelled();
				});
				TextObject hintText;
				this.CanExpelCurrentClan = this.GetCanExpelCurrentClanWithReason(this._isThereAPendingDecisionToExpelThisClan, this.ExpelCost, out hintText);
				this.ExpelHint.HintText = hintText;
				if (this._isThereAPendingDecisionToExpelThisClan)
				{
					this.ExpelActionText = GameTexts.FindText("str_resolve", null).ToString();
					this.ExpelActionExplanationText = GameTexts.FindText("str_resolve_explanation", null).ToString();
					this.ExpelCost = 0;
					return;
				}
				this.ExpelActionText = GameTexts.FindText("str_policy_propose", null).ToString();
				this.ExpelCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfExpellingClan(Clan.PlayerClan);
				TextObject hintText2;
				this.CanSupportCurrentClan = this.GetCanSupportCurrentClanWithReason(this.SupportCost, out hintText2);
				this.SupportHint.HintText = hintText2;
				this.ExpelActionExplanationText = GameTexts.FindText("str_expel_clan_action_explanation", null).SetTextVariable("SUPPORT", this.CalculateExpelLikelihood(this.CurrentSelectedClan)).ToString();
				base.IsAcceptableItemSelected = this.CurrentSelectedClan != null;
			}
		}

		// Token: 0x06000B6D RID: 2925 RVA: 0x000302D0 File Offset: 0x0002E4D0
		private bool GetCanSupportCurrentClanWithReason(int supportCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (Hero.MainHero.Clan.Influence < (float)supportCost)
			{
				disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence", null);
				return false;
			}
			if (this.CurrentSelectedClan.Clan == Clan.PlayerClan)
			{
				disabledReason = GameTexts.FindText("str_cannot_support_your_clan", null);
				return false;
			}
			if (Hero.MainHero.Clan.IsUnderMercenaryService)
			{
				disabledReason = GameTexts.FindText("str_mercenaries_cannot_support_clans", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000B6E RID: 2926 RVA: 0x00030358 File Offset: 0x0002E558
		private bool GetCanExpelCurrentClanWithReason(bool isThereAPendingDecision, int expelCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (Hero.MainHero.Clan.IsUnderMercenaryService)
			{
				disabledReason = GameTexts.FindText("str_mercenaries_cannot_expel_clans", null);
				return false;
			}
			if (!isThereAPendingDecision)
			{
				if (Hero.MainHero.Clan.Influence < (float)expelCost)
				{
					disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence", null);
					return false;
				}
				if (this.CurrentSelectedClan.Clan == Clan.PlayerClan)
				{
					disabledReason = GameTexts.FindText("str_cannot_expel_your_clan", null);
					return false;
				}
				Clan clan = this.CurrentSelectedClan.Clan;
				Kingdom kingdom = this.CurrentSelectedClan.Clan.Kingdom;
				if (clan == ((kingdom != null) ? kingdom.RulingClan : null))
				{
					disabledReason = GameTexts.FindText("str_cannot_expel_ruling_clan", null);
					return false;
				}
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000B6F RID: 2927 RVA: 0x0003041C File Offset: 0x0002E61C
		public void RefreshClan()
		{
			this.RefreshClanList();
			foreach (KingdomClanItemVM kingdomClanItemVM in this.Clans)
			{
				kingdomClanItemVM.Refresh();
			}
		}

		// Token: 0x06000B70 RID: 2928 RVA: 0x0003046C File Offset: 0x0002E66C
		public void SelectClan(Clan clan)
		{
			foreach (KingdomClanItemVM kingdomClanItemVM in this.Clans)
			{
				if (kingdomClanItemVM.Clan == clan)
				{
					this.OnClanSelection(kingdomClanItemVM);
					break;
				}
			}
		}

		// Token: 0x06000B71 RID: 2929 RVA: 0x000304C4 File Offset: 0x0002E6C4
		private void OnClanSelection(KingdomClanItemVM clan)
		{
			if (this._currentSelectedClan != clan)
			{
				this.SetCurrentSelectedClan(clan);
			}
		}

		// Token: 0x06000B72 RID: 2930 RVA: 0x000304D8 File Offset: 0x0002E6D8
		private void ExecuteExpelCurrentClan()
		{
			if (Hero.MainHero.Clan.Influence >= (float)this.ExpelCost)
			{
				KingdomDecision kingdomDecision = new ExpelClanFromKingdomDecision(Clan.PlayerClan, this._currentSelectedClan.Clan);
				Clan.PlayerClan.Kingdom.AddDecision(kingdomDecision, false);
				this._forceDecide(kingdomDecision);
			}
		}

		// Token: 0x06000B73 RID: 2931 RVA: 0x00030530 File Offset: 0x0002E730
		private void ExecuteSupport()
		{
			if (Hero.MainHero.Clan.Influence >= (float)this.SupportCost)
			{
				this._currentSelectedClan.Clan.OnSupportedByClan(Hero.MainHero.Clan);
				Clan clan = this._currentSelectedClan.Clan;
				this.RefreshClan();
				this.SelectClan(clan);
			}
		}

		// Token: 0x06000B74 RID: 2932 RVA: 0x00030588 File Offset: 0x0002E788
		private int CalculateExpelLikelihood(KingdomClanItemVM clan)
		{
			return MathF.Round(new KingdomElection(new ExpelClanFromKingdomDecision(Clan.PlayerClan, clan.Clan)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x06000B75 RID: 2933 RVA: 0x000305B4 File Offset: 0x0002E7B4
		private void RefreshClanList()
		{
			this.Clans.Clear();
			if (Clan.PlayerClan.Kingdom != null)
			{
				foreach (Clan clan in Clan.PlayerClan.Kingdom.Clans)
				{
					this.Clans.Add(new KingdomClanItemVM(clan, new Action<KingdomClanItemVM>(this.OnClanSelection)));
				}
			}
			if (this.Clans.Count > 0)
			{
				this.SetCurrentSelectedClan(this.Clans.FirstOrDefault<KingdomClanItemVM>());
			}
			if (this.ClanSortController != null)
			{
				this.ClanSortController.SortByCurrentState();
			}
		}

		// Token: 0x06000B76 RID: 2934 RVA: 0x00030670 File Offset: 0x0002E870
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		}

		// Token: 0x06000B77 RID: 2935 RVA: 0x00030683 File Offset: 0x0002E883
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			if (clan != Clan.PlayerClan && (oldKingdom == Clan.PlayerClan.Kingdom || newKingdom == Clan.PlayerClan.Kingdom))
			{
				this.RefreshClanList();
			}
		}

		// Token: 0x170003A0 RID: 928
		// (get) Token: 0x06000B78 RID: 2936 RVA: 0x000306AD File Offset: 0x0002E8AD
		// (set) Token: 0x06000B79 RID: 2937 RVA: 0x000306B5 File Offset: 0x0002E8B5
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

		// Token: 0x170003A1 RID: 929
		// (get) Token: 0x06000B7A RID: 2938 RVA: 0x000306D3 File Offset: 0x0002E8D3
		// (set) Token: 0x06000B7B RID: 2939 RVA: 0x000306DB File Offset: 0x0002E8DB
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

		// Token: 0x170003A2 RID: 930
		// (get) Token: 0x06000B7C RID: 2940 RVA: 0x000306F9 File Offset: 0x0002E8F9
		// (set) Token: 0x06000B7D RID: 2941 RVA: 0x00030701 File Offset: 0x0002E901
		[DataSourceProperty]
		public string ExpelActionExplanationText
		{
			get
			{
				return this._expelActionExplanationText;
			}
			set
			{
				if (value != this._expelActionExplanationText)
				{
					this._expelActionExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "ExpelActionExplanationText");
				}
			}
		}

		// Token: 0x170003A3 RID: 931
		// (get) Token: 0x06000B7E RID: 2942 RVA: 0x00030724 File Offset: 0x0002E924
		// (set) Token: 0x06000B7F RID: 2943 RVA: 0x0003072C File Offset: 0x0002E92C
		[DataSourceProperty]
		public string SupportActionExplanationText
		{
			get
			{
				return this._supportActionExplanationText;
			}
			set
			{
				if (value != this._supportActionExplanationText)
				{
					this._supportActionExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportActionExplanationText");
				}
			}
		}

		// Token: 0x170003A4 RID: 932
		// (get) Token: 0x06000B80 RID: 2944 RVA: 0x0003074F File Offset: 0x0002E94F
		// (set) Token: 0x06000B81 RID: 2945 RVA: 0x00030757 File Offset: 0x0002E957
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

		// Token: 0x170003A5 RID: 933
		// (get) Token: 0x06000B82 RID: 2946 RVA: 0x0003077A File Offset: 0x0002E97A
		// (set) Token: 0x06000B83 RID: 2947 RVA: 0x00030782 File Offset: 0x0002E982
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

		// Token: 0x170003A6 RID: 934
		// (get) Token: 0x06000B84 RID: 2948 RVA: 0x000307A5 File Offset: 0x0002E9A5
		// (set) Token: 0x06000B85 RID: 2949 RVA: 0x000307AD File Offset: 0x0002E9AD
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

		// Token: 0x170003A7 RID: 935
		// (get) Token: 0x06000B86 RID: 2950 RVA: 0x000307D0 File Offset: 0x0002E9D0
		// (set) Token: 0x06000B87 RID: 2951 RVA: 0x000307D8 File Offset: 0x0002E9D8
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

		// Token: 0x170003A8 RID: 936
		// (get) Token: 0x06000B88 RID: 2952 RVA: 0x000307FB File Offset: 0x0002E9FB
		// (set) Token: 0x06000B89 RID: 2953 RVA: 0x00030803 File Offset: 0x0002EA03
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

		// Token: 0x170003A9 RID: 937
		// (get) Token: 0x06000B8A RID: 2954 RVA: 0x00030826 File Offset: 0x0002EA26
		// (set) Token: 0x06000B8B RID: 2955 RVA: 0x0003082E File Offset: 0x0002EA2E
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

		// Token: 0x170003AA RID: 938
		// (get) Token: 0x06000B8C RID: 2956 RVA: 0x00030851 File Offset: 0x0002EA51
		// (set) Token: 0x06000B8D RID: 2957 RVA: 0x00030859 File Offset: 0x0002EA59
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

		// Token: 0x170003AB RID: 939
		// (get) Token: 0x06000B8E RID: 2958 RVA: 0x00030877 File Offset: 0x0002EA77
		// (set) Token: 0x06000B8F RID: 2959 RVA: 0x0003087F File Offset: 0x0002EA7F
		[DataSourceProperty]
		public bool CanSupportCurrentClan
		{
			get
			{
				return this._canSupportCurrentClan;
			}
			set
			{
				if (value != this._canSupportCurrentClan)
				{
					this._canSupportCurrentClan = value;
					base.OnPropertyChangedWithValue(value, "CanSupportCurrentClan");
				}
			}
		}

		// Token: 0x170003AC RID: 940
		// (get) Token: 0x06000B90 RID: 2960 RVA: 0x0003089D File Offset: 0x0002EA9D
		// (set) Token: 0x06000B91 RID: 2961 RVA: 0x000308A5 File Offset: 0x0002EAA5
		[DataSourceProperty]
		public bool CanExpelCurrentClan
		{
			get
			{
				return this._canExpelCurrentClan;
			}
			set
			{
				if (value != this._canExpelCurrentClan)
				{
					this._canExpelCurrentClan = value;
					base.OnPropertyChangedWithValue(value, "CanExpelCurrentClan");
				}
			}
		}

		// Token: 0x170003AD RID: 941
		// (get) Token: 0x06000B92 RID: 2962 RVA: 0x000308C3 File Offset: 0x0002EAC3
		// (set) Token: 0x06000B93 RID: 2963 RVA: 0x000308CB File Offset: 0x0002EACB
		[DataSourceProperty]
		public string SupportText
		{
			get
			{
				return this._supportText;
			}
			set
			{
				if (value != this._supportText)
				{
					this._supportText = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportText");
				}
			}
		}

		// Token: 0x170003AE RID: 942
		// (get) Token: 0x06000B94 RID: 2964 RVA: 0x000308EE File Offset: 0x0002EAEE
		// (set) Token: 0x06000B95 RID: 2965 RVA: 0x000308F6 File Offset: 0x0002EAF6
		[DataSourceProperty]
		public string ExpelActionText
		{
			get
			{
				return this._expelActionText;
			}
			set
			{
				if (value != this._expelActionText)
				{
					this._expelActionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ExpelActionText");
				}
			}
		}

		// Token: 0x170003AF RID: 943
		// (get) Token: 0x06000B96 RID: 2966 RVA: 0x00030919 File Offset: 0x0002EB19
		// (set) Token: 0x06000B97 RID: 2967 RVA: 0x00030921 File Offset: 0x0002EB21
		[DataSourceProperty]
		public int SupportCost
		{
			get
			{
				return this._supportCost;
			}
			set
			{
				if (value != this._supportCost)
				{
					this._supportCost = value;
					base.OnPropertyChangedWithValue(value, "SupportCost");
				}
			}
		}

		// Token: 0x170003B0 RID: 944
		// (get) Token: 0x06000B98 RID: 2968 RVA: 0x0003093F File Offset: 0x0002EB3F
		// (set) Token: 0x06000B99 RID: 2969 RVA: 0x00030947 File Offset: 0x0002EB47
		[DataSourceProperty]
		public int ExpelCost
		{
			get
			{
				return this._expelCost;
			}
			set
			{
				if (value != this._expelCost)
				{
					this._expelCost = value;
					base.OnPropertyChangedWithValue(value, "ExpelCost");
				}
			}
		}

		// Token: 0x170003B1 RID: 945
		// (get) Token: 0x06000B9A RID: 2970 RVA: 0x00030965 File Offset: 0x0002EB65
		// (set) Token: 0x06000B9B RID: 2971 RVA: 0x0003096D File Offset: 0x0002EB6D
		[DataSourceProperty]
		public HintViewModel ExpelHint
		{
			get
			{
				return this._expelHint;
			}
			set
			{
				if (value != this._expelHint)
				{
					this._expelHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ExpelHint");
				}
			}
		}

		// Token: 0x170003B2 RID: 946
		// (get) Token: 0x06000B9C RID: 2972 RVA: 0x0003098B File Offset: 0x0002EB8B
		// (set) Token: 0x06000B9D RID: 2973 RVA: 0x00030993 File Offset: 0x0002EB93
		[DataSourceProperty]
		public HintViewModel SupportHint
		{
			get
			{
				return this._supportHint;
			}
			set
			{
				if (value != this._supportHint)
				{
					this._supportHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SupportHint");
				}
			}
		}

		// Token: 0x04000513 RID: 1299
		private Action<KingdomDecision> _forceDecide;

		// Token: 0x04000514 RID: 1300
		private bool _isThereAPendingDecisionToExpelThisClan;

		// Token: 0x04000515 RID: 1301
		private MBBindingList<KingdomClanItemVM> _clans;

		// Token: 0x04000516 RID: 1302
		private HintViewModel _expelHint;

		// Token: 0x04000517 RID: 1303
		private HintViewModel _supportHint;

		// Token: 0x04000518 RID: 1304
		private string _bannerText;

		// Token: 0x04000519 RID: 1305
		private string _nameText;

		// Token: 0x0400051A RID: 1306
		private string _influenceText;

		// Token: 0x0400051B RID: 1307
		private string _membersText;

		// Token: 0x0400051C RID: 1308
		private string _fiefsText;

		// Token: 0x0400051D RID: 1309
		private string _typeText;

		// Token: 0x0400051E RID: 1310
		private string _expelActionText;

		// Token: 0x0400051F RID: 1311
		private string _expelActionExplanationText;

		// Token: 0x04000520 RID: 1312
		private string _supportActionExplanationText;

		// Token: 0x04000521 RID: 1313
		private int _expelCost;

		// Token: 0x04000522 RID: 1314
		private string _supportText;

		// Token: 0x04000523 RID: 1315
		private int _supportCost;

		// Token: 0x04000524 RID: 1316
		private bool _canSupportCurrentClan;

		// Token: 0x04000525 RID: 1317
		private bool _canExpelCurrentClan;

		// Token: 0x04000526 RID: 1318
		private KingdomClanItemVM _currentSelectedClan;

		// Token: 0x04000527 RID: 1319
		private KingdomClanSortControllerVM _clanSortController;
	}
}
