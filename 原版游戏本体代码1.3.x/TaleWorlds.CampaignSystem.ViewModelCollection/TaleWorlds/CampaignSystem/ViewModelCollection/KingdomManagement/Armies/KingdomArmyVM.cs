using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies
{
	// Token: 0x0200008C RID: 140
	public class KingdomArmyVM : KingdomCategoryVM
	{
		// Token: 0x06000BE6 RID: 3046 RVA: 0x00031488 File Offset: 0x0002F688
		public KingdomArmyVM(Action onManageArmy, Action refreshDecision, Action<Army> showArmyOnMap)
		{
			this._onManageArmy = onManageArmy;
			this._refreshDecision = refreshDecision;
			this._showArmyOnMap = showArmyOnMap;
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this._armies = new MBBindingList<KingdomArmyItemVM>();
			this.PlayerHasArmy = MobileParty.MainParty.Army != null;
			this.ChangeLeaderCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfChangingLeaderOfArmy();
			this.DisbandCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfDisbandingArmy();
			this.CreateArmyHint = new HintViewModel();
			this.DisbandHint = new HintViewModel();
			this.ManageArmyHint = new HintViewModel();
			base.IsAcceptableItemSelected = false;
			this.RefreshArmyList();
			this.ArmySortController = new KingdomArmySortControllerVM(ref this._armies);
			this.RefreshValues();
		}

		// Token: 0x06000BE7 RID: 3047 RVA: 0x00031558 File Offset: 0x0002F758
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ArmyNameText = GameTexts.FindText("str_sort_by_army_name_label", null).ToString();
			this.LeaderText = GameTexts.FindText("str_sort_by_leader_name_label", null).ToString();
			this.StrengthText = GameTexts.FindText("str_men", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
			base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_army_selected", null).ToString();
			this.DisbandActionExplanationText = GameTexts.FindText("str_kingdom_disband_army_explanation", null).ToString();
			this.ManageActionExplanationText = GameTexts.FindText("str_kingdom_manage_army_explanation", null).ToString();
			this.ManageText = GameTexts.FindText("str_manage", null).ToString();
			this.CreateArmyText = (this.PlayerHasArmy ? new TextObject("{=DAmdTxuC}Army Manage", null).ToString() : new TextObject("{=lc9s4rLZ}Create Army", null).ToString());
			base.CategoryNameText = new TextObject("{=j12VrGKz}Army", null).ToString();
			this.ChangeLeaderText = new TextObject("{=NcYbdiyT}Change Leader", null).ToString();
			this.PartiesText = new TextObject("{=t3tq0eoW}Parties", null).ToString();
			this.DisbandText = new TextObject("{=xXSFaGW8}Disband", null).ToString();
			this.ShowOnMapText = GameTexts.FindText("str_show_on_map", null).ToString();
			this.CreateArmyText = new TextObject("{=lc9s4rLZ}Create Army", null).ToString();
			this.Armies.ApplyActionOnAllItems(delegate(KingdomArmyItemVM x)
			{
				x.RefreshValues();
			});
			KingdomArmyItemVM currentSelectedArmy = this.CurrentSelectedArmy;
			if (currentSelectedArmy == null)
			{
				return;
			}
			currentSelectedArmy.RefreshValues();
		}

		// Token: 0x06000BE8 RID: 3048 RVA: 0x0003170C File Offset: 0x0002F90C
		public void RefreshArmyList()
		{
			base.NotificationCount = this._viewDataTracker.NumOfKingdomArmyNotifications;
			this._kingdom = Hero.MainHero.MapFaction as Kingdom;
			if (this._kingdom != null)
			{
				this.Armies.Clear();
				using (List<Army>.Enumerator enumerator = this._kingdom.Armies.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Army army = enumerator.Current;
						this.Armies.Add(new KingdomArmyItemVM(army, new Action<KingdomArmyItemVM>(this.OnSelection)));
					}
					goto IL_A0;
				}
			}
			Debug.FailedAssert("Kingdom screen can't open if you're not in kingdom", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\KingdomManagement\\Armies\\KingdomArmyVM.cs", "RefreshArmyList", 81);
			IL_A0:
			this.RefreshCanManageArmy();
			if (this.Armies.Count == 0 && this.CurrentSelectedArmy != null)
			{
				this.OnSelection(null);
				return;
			}
			if (this.Armies.Count > 0)
			{
				this.OnSelection(this.Armies[0]);
				this.CurrentSelectedArmy.IsSelected = true;
			}
		}

		// Token: 0x06000BE9 RID: 3049 RVA: 0x00031818 File Offset: 0x0002FA18
		private void ExecuteManageArmy()
		{
			this._onManageArmy();
		}

		// Token: 0x06000BEA RID: 3050 RVA: 0x00031825 File Offset: 0x0002FA25
		private void ExecuteShowOnMap()
		{
			if (this.CurrentSelectedArmy != null)
			{
				this._showArmyOnMap(this.CurrentSelectedArmy.Army);
			}
		}

		// Token: 0x06000BEB RID: 3051 RVA: 0x00031848 File Offset: 0x0002FA48
		private void RefreshCurrentArmyVisuals(KingdomArmyItemVM item)
		{
			if (item != null)
			{
				if (this.CurrentSelectedArmy != null)
				{
					this.CurrentSelectedArmy.IsSelected = false;
				}
				this.CanManageCurrentArmy = false;
				this.CurrentSelectedArmy = item;
				base.NotificationCount = this._viewDataTracker.NumOfKingdomArmyNotifications;
				this.DisbandCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfDisbandingArmy();
				this.ChangeLeaderCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfChangingLeaderOfArmy();
				TextObject hintText;
				this.CanDisbandCurrentArmy = this.GetCanDisbandCurrentArmyWithReason(item, this.DisbandCost, out hintText);
				this.DisbandHint.HintText = hintText;
				this.DisbandActionExplanationText = GameTexts.FindText("str_kingdom_disband_army_explanation", null).ToString();
				if (this.CurrentSelectedArmy != null)
				{
					this.CanShowLocationOfCurrentArmy = this.CurrentSelectedArmy.Army.AiBehaviorObject is Settlement || this.CurrentSelectedArmy.Army.AiBehaviorObject is MobileParty;
					TextObject hintText2;
					this.CanManageCurrentArmy = this.GetCanManageCurrentArmyWithReason(out hintText2);
					this.ManageArmyHint.HintText = hintText2;
				}
			}
		}

		// Token: 0x06000BEC RID: 3052 RVA: 0x00031953 File Offset: 0x0002FB53
		private bool GetCanManageCurrentArmyWithReason(out TextObject disabledReason)
		{
			KingdomArmyItemVM currentSelectedArmy = this.CurrentSelectedArmy;
			if (currentSelectedArmy == null || !currentSelectedArmy.IsMainArmy)
			{
				disabledReason = TextObject.GetEmpty();
				return false;
			}
			return CampaignUIHelper.GetCanManageCurrentArmyWithReason(out disabledReason);
		}

		// Token: 0x06000BED RID: 3053 RVA: 0x0003197C File Offset: 0x0002FB7C
		private bool GetCanDisbandCurrentArmyWithReason(KingdomArmyItemVM armyItem, int disbandCost, out TextObject disabledReason)
		{
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				disabledReason = GameTexts.FindText("str_cannot_disband_army_while_mercenary", null);
				return false;
			}
			if (Clan.PlayerClan.Influence < (float)disbandCost)
			{
				disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence", null);
				return false;
			}
			if (armyItem.Army.LeaderParty.MapEvent != null)
			{
				disabledReason = GameTexts.FindText("str_cannot_disband_army_while_in_event", null);
				return false;
			}
			if (armyItem.Army.Parties.Contains(MobileParty.MainParty))
			{
				disabledReason = GameTexts.FindText("str_cannot_disband_army_while_in_that_army", null);
				return false;
			}
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000BEE RID: 3054 RVA: 0x00031A20 File Offset: 0x0002FC20
		public void SelectArmy(Army army)
		{
			foreach (KingdomArmyItemVM kingdomArmyItemVM in this.Armies)
			{
				if (kingdomArmyItemVM.Army == army)
				{
					this.OnSelection(kingdomArmyItemVM);
					break;
				}
			}
		}

		// Token: 0x06000BEF RID: 3055 RVA: 0x00031A78 File Offset: 0x0002FC78
		private void OnSelection(KingdomArmyItemVM item)
		{
			if (this.CurrentSelectedArmy != item)
			{
				this.RefreshCurrentArmyVisuals(item);
				this.CurrentSelectedArmy = item;
				base.IsAcceptableItemSelected = item != null;
			}
		}

		// Token: 0x06000BF0 RID: 3056 RVA: 0x00031A9C File Offset: 0x0002FC9C
		private void ExecuteDisbandCurrentArmy()
		{
			if (this.CurrentSelectedArmy != null && Hero.MainHero.Clan.Influence >= (float)this.DisbandCost)
			{
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_disband_army", null).ToString(), new TextObject("{=zrhr4rDA}Are you sure you want to disband this army? This will result in relation loss.", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.DisbandCurrentArmy), null, "", 0f, null, null, null), false, false);
			}
		}

		// Token: 0x06000BF1 RID: 3057 RVA: 0x00031B34 File Offset: 0x0002FD34
		private void DisbandCurrentArmy()
		{
			if (this.CurrentSelectedArmy != null && Hero.MainHero.Clan.Influence >= (float)this.DisbandCost)
			{
				DisbandArmyAction.ApplyByReleasedByPlayerAfterBattle(this.CurrentSelectedArmy.Army);
				this.RefreshArmyList();
			}
		}

		// Token: 0x06000BF2 RID: 3058 RVA: 0x00031B6C File Offset: 0x0002FD6C
		private void RefreshCanManageArmy()
		{
			this.PlayerHasArmy = MobileParty.MainParty.Army != null;
			TextObject hintText;
			this.CanCreateArmy = Campaign.Current.Models.ArmyManagementCalculationModel.CanPlayerCreateArmy(out hintText);
			this.CreateArmyHint.HintText = hintText;
		}

		// Token: 0x170003CD RID: 973
		// (get) Token: 0x06000BF3 RID: 3059 RVA: 0x00031BB4 File Offset: 0x0002FDB4
		// (set) Token: 0x06000BF4 RID: 3060 RVA: 0x00031BBC File Offset: 0x0002FDBC
		[DataSourceProperty]
		public KingdomArmySortControllerVM ArmySortController
		{
			get
			{
				return this._armySortController;
			}
			set
			{
				if (value != this._armySortController)
				{
					this._armySortController = value;
					base.OnPropertyChangedWithValue<KingdomArmySortControllerVM>(value, "ArmySortController");
				}
			}
		}

		// Token: 0x170003CE RID: 974
		// (get) Token: 0x06000BF5 RID: 3061 RVA: 0x00031BDA File Offset: 0x0002FDDA
		// (set) Token: 0x06000BF6 RID: 3062 RVA: 0x00031BE2 File Offset: 0x0002FDE2
		[DataSourceProperty]
		public string CreateArmyText
		{
			get
			{
				return this._createArmyText;
			}
			set
			{
				if (value != this._createArmyText)
				{
					this._createArmyText = value;
					base.OnPropertyChangedWithValue<string>(value, "CreateArmyText");
				}
			}
		}

		// Token: 0x170003CF RID: 975
		// (get) Token: 0x06000BF7 RID: 3063 RVA: 0x00031C05 File Offset: 0x0002FE05
		// (set) Token: 0x06000BF8 RID: 3064 RVA: 0x00031C0D File Offset: 0x0002FE0D
		[DataSourceProperty]
		public string DisbandActionExplanationText
		{
			get
			{
				return this._disbandActionExplanationText;
			}
			set
			{
				if (value != this._disbandActionExplanationText)
				{
					this._disbandActionExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "DisbandActionExplanationText");
				}
			}
		}

		// Token: 0x170003D0 RID: 976
		// (get) Token: 0x06000BF9 RID: 3065 RVA: 0x00031C30 File Offset: 0x0002FE30
		// (set) Token: 0x06000BFA RID: 3066 RVA: 0x00031C38 File Offset: 0x0002FE38
		[DataSourceProperty]
		public string ManageActionExplanationText
		{
			get
			{
				return this._manageActionExplanationText;
			}
			set
			{
				if (value != this._manageActionExplanationText)
				{
					this._manageActionExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "ManageActionExplanationText");
				}
			}
		}

		// Token: 0x170003D1 RID: 977
		// (get) Token: 0x06000BFB RID: 3067 RVA: 0x00031C5B File Offset: 0x0002FE5B
		// (set) Token: 0x06000BFC RID: 3068 RVA: 0x00031C63 File Offset: 0x0002FE63
		[DataSourceProperty]
		public KingdomArmyItemVM CurrentSelectedArmy
		{
			get
			{
				return this._currentSelectedArmy;
			}
			set
			{
				if (value != this._currentSelectedArmy)
				{
					this._currentSelectedArmy = value;
					base.OnPropertyChangedWithValue<KingdomArmyItemVM>(value, "CurrentSelectedArmy");
				}
			}
		}

		// Token: 0x170003D2 RID: 978
		// (get) Token: 0x06000BFD RID: 3069 RVA: 0x00031C81 File Offset: 0x0002FE81
		// (set) Token: 0x06000BFE RID: 3070 RVA: 0x00031C89 File Offset: 0x0002FE89
		[DataSourceProperty]
		public HintViewModel CreateArmyHint
		{
			get
			{
				return this._createArmyHint;
			}
			set
			{
				if (value != this._createArmyHint)
				{
					this._createArmyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CreateArmyHint");
				}
			}
		}

		// Token: 0x170003D3 RID: 979
		// (get) Token: 0x06000BFF RID: 3071 RVA: 0x00031CA7 File Offset: 0x0002FEA7
		// (set) Token: 0x06000C00 RID: 3072 RVA: 0x00031CAF File Offset: 0x0002FEAF
		[DataSourceProperty]
		public HintViewModel ManageArmyHint
		{
			get
			{
				return this._manageArmyHint;
			}
			set
			{
				if (value != this._manageArmyHint)
				{
					this._manageArmyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ManageArmyHint");
				}
			}
		}

		// Token: 0x170003D4 RID: 980
		// (get) Token: 0x06000C01 RID: 3073 RVA: 0x00031CCD File Offset: 0x0002FECD
		// (set) Token: 0x06000C02 RID: 3074 RVA: 0x00031CD5 File Offset: 0x0002FED5
		[DataSourceProperty]
		public bool PlayerHasArmy
		{
			get
			{
				return this._playerHasArmy;
			}
			set
			{
				if (value != this._playerHasArmy)
				{
					this._playerHasArmy = value;
					base.OnPropertyChangedWithValue(value, "PlayerHasArmy");
				}
			}
		}

		// Token: 0x170003D5 RID: 981
		// (get) Token: 0x06000C03 RID: 3075 RVA: 0x00031CF3 File Offset: 0x0002FEF3
		// (set) Token: 0x06000C04 RID: 3076 RVA: 0x00031CFB File Offset: 0x0002FEFB
		[DataSourceProperty]
		public bool CanCreateArmy
		{
			get
			{
				return this._canCreateArmy;
			}
			set
			{
				if (value != this._canCreateArmy)
				{
					this._canCreateArmy = value;
					base.OnPropertyChangedWithValue(value, "CanCreateArmy");
				}
			}
		}

		// Token: 0x170003D6 RID: 982
		// (get) Token: 0x06000C05 RID: 3077 RVA: 0x00031D19 File Offset: 0x0002FF19
		// (set) Token: 0x06000C06 RID: 3078 RVA: 0x00031D21 File Offset: 0x0002FF21
		[DataSourceProperty]
		public string LeaderText
		{
			get
			{
				return this._categoryLeaderName;
			}
			set
			{
				if (value != this._categoryLeaderName)
				{
					this._categoryLeaderName = value;
					base.OnPropertyChanged("CategoryLeaderName");
				}
			}
		}

		// Token: 0x170003D7 RID: 983
		// (get) Token: 0x06000C07 RID: 3079 RVA: 0x00031D43 File Offset: 0x0002FF43
		// (set) Token: 0x06000C08 RID: 3080 RVA: 0x00031D4B File Offset: 0x0002FF4B
		[DataSourceProperty]
		public string ShowOnMapText
		{
			get
			{
				return this._showOnMapText;
			}
			set
			{
				if (value != this._showOnMapText)
				{
					this._showOnMapText = value;
					base.OnPropertyChangedWithValue<string>(value, "ShowOnMapText");
				}
			}
		}

		// Token: 0x170003D8 RID: 984
		// (get) Token: 0x06000C09 RID: 3081 RVA: 0x00031D6E File Offset: 0x0002FF6E
		// (set) Token: 0x06000C0A RID: 3082 RVA: 0x00031D76 File Offset: 0x0002FF76
		[DataSourceProperty]
		public string ArmyNameText
		{
			get
			{
				return this._categoryLordCount;
			}
			set
			{
				if (value != this._categoryLordCount)
				{
					this._categoryLordCount = value;
					base.OnPropertyChanged("CategoryLordCount");
				}
			}
		}

		// Token: 0x170003D9 RID: 985
		// (get) Token: 0x06000C0B RID: 3083 RVA: 0x00031D98 File Offset: 0x0002FF98
		// (set) Token: 0x06000C0C RID: 3084 RVA: 0x00031DA0 File Offset: 0x0002FFA0
		[DataSourceProperty]
		public string StrengthText
		{
			get
			{
				return this._categoryStrength;
			}
			set
			{
				if (value != this._categoryStrength)
				{
					this._categoryStrength = value;
					base.OnPropertyChanged("CategoryStrength");
				}
			}
		}

		// Token: 0x170003DA RID: 986
		// (get) Token: 0x06000C0D RID: 3085 RVA: 0x00031DC2 File Offset: 0x0002FFC2
		// (set) Token: 0x06000C0E RID: 3086 RVA: 0x00031DCA File Offset: 0x0002FFCA
		[DataSourceProperty]
		public string PartiesText
		{
			get
			{
				return this._categoryParties;
			}
			set
			{
				if (value != this._categoryParties)
				{
					this._categoryParties = value;
					base.OnPropertyChangedWithValue<string>(value, "PartiesText");
				}
			}
		}

		// Token: 0x170003DB RID: 987
		// (get) Token: 0x06000C0F RID: 3087 RVA: 0x00031DED File Offset: 0x0002FFED
		// (set) Token: 0x06000C10 RID: 3088 RVA: 0x00031DF5 File Offset: 0x0002FFF5
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._categoryObjective;
			}
			set
			{
				if (value != this._categoryObjective)
				{
					this._categoryObjective = value;
					base.OnPropertyChanged("CategoryObjective");
				}
			}
		}

		// Token: 0x170003DC RID: 988
		// (get) Token: 0x06000C11 RID: 3089 RVA: 0x00031E17 File Offset: 0x00030017
		// (set) Token: 0x06000C12 RID: 3090 RVA: 0x00031E1F File Offset: 0x0003001F
		[DataSourceProperty]
		public MBBindingList<KingdomArmyItemVM> Armies
		{
			get
			{
				return this._armies;
			}
			set
			{
				if (value != this._armies)
				{
					this._armies = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomArmyItemVM>>(value, "Armies");
				}
			}
		}

		// Token: 0x170003DD RID: 989
		// (get) Token: 0x06000C13 RID: 3091 RVA: 0x00031E3D File Offset: 0x0003003D
		// (set) Token: 0x06000C14 RID: 3092 RVA: 0x00031E45 File Offset: 0x00030045
		[DataSourceProperty]
		public bool CanDisbandCurrentArmy
		{
			get
			{
				return this._canDisbandCurrentArmy;
			}
			set
			{
				if (value != this._canDisbandCurrentArmy)
				{
					this._canDisbandCurrentArmy = value;
					base.OnPropertyChangedWithValue(value, "CanDisbandCurrentArmy");
				}
			}
		}

		// Token: 0x170003DE RID: 990
		// (get) Token: 0x06000C15 RID: 3093 RVA: 0x00031E63 File Offset: 0x00030063
		// (set) Token: 0x06000C16 RID: 3094 RVA: 0x00031E6B File Offset: 0x0003006B
		[DataSourceProperty]
		public bool CanManageCurrentArmy
		{
			get
			{
				return this._canManageCurrentArmy;
			}
			set
			{
				if (value != this._canManageCurrentArmy)
				{
					this._canManageCurrentArmy = value;
					base.OnPropertyChangedWithValue(value, "CanManageCurrentArmy");
				}
			}
		}

		// Token: 0x170003DF RID: 991
		// (get) Token: 0x06000C17 RID: 3095 RVA: 0x00031E89 File Offset: 0x00030089
		// (set) Token: 0x06000C18 RID: 3096 RVA: 0x00031E91 File Offset: 0x00030091
		[DataSourceProperty]
		public bool CanChangeLeaderOfCurrentArmy
		{
			get
			{
				return this._canChangeLeaderOfCurrentArmy;
			}
			set
			{
				if (value != this._canChangeLeaderOfCurrentArmy)
				{
					this._canChangeLeaderOfCurrentArmy = value;
					base.OnPropertyChangedWithValue(value, "CanChangeLeaderOfCurrentArmy");
				}
			}
		}

		// Token: 0x170003E0 RID: 992
		// (get) Token: 0x06000C19 RID: 3097 RVA: 0x00031EAF File Offset: 0x000300AF
		// (set) Token: 0x06000C1A RID: 3098 RVA: 0x00031EB7 File Offset: 0x000300B7
		[DataSourceProperty]
		public bool CanShowLocationOfCurrentArmy
		{
			get
			{
				return this._canShowLocationOfCurrentArmy;
			}
			set
			{
				if (value != this._canShowLocationOfCurrentArmy)
				{
					this._canShowLocationOfCurrentArmy = value;
					base.OnPropertyChangedWithValue(value, "CanShowLocationOfCurrentArmy");
				}
			}
		}

		// Token: 0x170003E1 RID: 993
		// (get) Token: 0x06000C1B RID: 3099 RVA: 0x00031ED5 File Offset: 0x000300D5
		// (set) Token: 0x06000C1C RID: 3100 RVA: 0x00031EDD File Offset: 0x000300DD
		[DataSourceProperty]
		public string DisbandText
		{
			get
			{
				return this._disbandText;
			}
			set
			{
				if (value != this._disbandText)
				{
					this._disbandText = value;
					base.OnPropertyChangedWithValue<string>(value, "DisbandText");
				}
			}
		}

		// Token: 0x170003E2 RID: 994
		// (get) Token: 0x06000C1D RID: 3101 RVA: 0x00031F00 File Offset: 0x00030100
		// (set) Token: 0x06000C1E RID: 3102 RVA: 0x00031F08 File Offset: 0x00030108
		[DataSourceProperty]
		public string ManageText
		{
			get
			{
				return this._manageText;
			}
			set
			{
				if (value != this._manageText)
				{
					this._manageText = value;
					base.OnPropertyChangedWithValue<string>(value, "ManageText");
				}
			}
		}

		// Token: 0x170003E3 RID: 995
		// (get) Token: 0x06000C1F RID: 3103 RVA: 0x00031F2B File Offset: 0x0003012B
		// (set) Token: 0x06000C20 RID: 3104 RVA: 0x00031F33 File Offset: 0x00030133
		[DataSourceProperty]
		public int DisbandCost
		{
			get
			{
				return this._disbandCost;
			}
			set
			{
				if (value != this._disbandCost)
				{
					this._disbandCost = value;
					base.OnPropertyChangedWithValue(value, "DisbandCost");
				}
			}
		}

		// Token: 0x170003E4 RID: 996
		// (get) Token: 0x06000C21 RID: 3105 RVA: 0x00031F51 File Offset: 0x00030151
		// (set) Token: 0x06000C22 RID: 3106 RVA: 0x00031F59 File Offset: 0x00030159
		[DataSourceProperty]
		public string ChangeLeaderText
		{
			get
			{
				return this._changeLeaderText;
			}
			set
			{
				if (value != this._changeLeaderText)
				{
					this._changeLeaderText = value;
					base.OnPropertyChangedWithValue<string>(value, "ChangeLeaderText");
				}
			}
		}

		// Token: 0x170003E5 RID: 997
		// (get) Token: 0x06000C23 RID: 3107 RVA: 0x00031F7C File Offset: 0x0003017C
		// (set) Token: 0x06000C24 RID: 3108 RVA: 0x00031F84 File Offset: 0x00030184
		[DataSourceProperty]
		public int ChangeLeaderCost
		{
			get
			{
				return this._changeLeaderCost;
			}
			set
			{
				if (value != this._changeLeaderCost)
				{
					this._changeLeaderCost = value;
					base.OnPropertyChangedWithValue(value, "ChangeLeaderCost");
				}
			}
		}

		// Token: 0x170003E6 RID: 998
		// (get) Token: 0x06000C25 RID: 3109 RVA: 0x00031FA2 File Offset: 0x000301A2
		// (set) Token: 0x06000C26 RID: 3110 RVA: 0x00031FAA File Offset: 0x000301AA
		[DataSourceProperty]
		public HintViewModel DisbandHint
		{
			get
			{
				return this._disbandHint;
			}
			set
			{
				if (value != this._disbandHint)
				{
					this._disbandHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisbandHint");
				}
			}
		}

		// Token: 0x0400054C RID: 1356
		private readonly Action _onManageArmy;

		// Token: 0x0400054D RID: 1357
		private readonly Action _refreshDecision;

		// Token: 0x0400054E RID: 1358
		private readonly Action<Army> _showArmyOnMap;

		// Token: 0x0400054F RID: 1359
		private readonly IViewDataTracker _viewDataTracker;

		// Token: 0x04000550 RID: 1360
		private Kingdom _kingdom;

		// Token: 0x04000551 RID: 1361
		private MBBindingList<KingdomArmyItemVM> _armies;

		// Token: 0x04000552 RID: 1362
		private KingdomArmyItemVM _currentSelectedArmy;

		// Token: 0x04000553 RID: 1363
		private HintViewModel _disbandHint;

		// Token: 0x04000554 RID: 1364
		private string _categoryLeaderName;

		// Token: 0x04000555 RID: 1365
		private string _categoryLordCount;

		// Token: 0x04000556 RID: 1366
		private string _categoryStrength;

		// Token: 0x04000557 RID: 1367
		private string _categoryObjective;

		// Token: 0x04000558 RID: 1368
		private string _categoryParties;

		// Token: 0x04000559 RID: 1369
		private string _createArmyText;

		// Token: 0x0400055A RID: 1370
		private string _disbandText;

		// Token: 0x0400055B RID: 1371
		private string _manageText;

		// Token: 0x0400055C RID: 1372
		private string _changeLeaderText;

		// Token: 0x0400055D RID: 1373
		private string _showOnMapText;

		// Token: 0x0400055E RID: 1374
		private string _disbandActionExplanationText;

		// Token: 0x0400055F RID: 1375
		private string _manageActionExplanationText;

		// Token: 0x04000560 RID: 1376
		private bool _canCreateArmy;

		// Token: 0x04000561 RID: 1377
		private bool _playerHasArmy;

		// Token: 0x04000562 RID: 1378
		private HintViewModel _createArmyHint;

		// Token: 0x04000563 RID: 1379
		private HintViewModel _manageArmyHint;

		// Token: 0x04000564 RID: 1380
		private bool _canChangeLeaderOfCurrentArmy;

		// Token: 0x04000565 RID: 1381
		private bool _canDisbandCurrentArmy;

		// Token: 0x04000566 RID: 1382
		private bool _canShowLocationOfCurrentArmy;

		// Token: 0x04000567 RID: 1383
		private bool _canManageCurrentArmy;

		// Token: 0x04000568 RID: 1384
		private int _disbandCost;

		// Token: 0x04000569 RID: 1385
		private int _changeLeaderCost;

		// Token: 0x0400056A RID: 1386
		private KingdomArmySortControllerVM _armySortController;
	}
}
