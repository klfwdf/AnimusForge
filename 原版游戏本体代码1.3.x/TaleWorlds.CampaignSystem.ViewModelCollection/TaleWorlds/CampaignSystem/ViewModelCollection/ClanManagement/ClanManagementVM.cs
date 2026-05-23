using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.Information;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000128 RID: 296
	public class ClanManagementVM : ViewModel
	{
		// Token: 0x06001AD2 RID: 6866 RVA: 0x000642AC File Offset: 0x000624AC
		public ClanManagementVM(Action onClose, Action<Hero> showHeroOnMap, Action<Hero> openPartyAsManage, Action openBannerEditor)
		{
			this._onClose = onClose;
			this._openPartyAsManage = openPartyAsManage;
			this._openBannerEditor = openBannerEditor;
			this._showHeroOnMap = showHeroOnMap;
			this._clan = Hero.MainHero.Clan;
			this.CardSelectionPopup = new ClanCardSelectionPopupVM();
			this.ClanMembers = new ClanMembersVM(new Action(this.RefreshCategoryValues), this._showHeroOnMap);
			this.ClanFiefs = this.CreateFiefsDataSource(new Action(this.RefreshCategoryValues), new Action<ClanCardSelectionInfo>(this.CardSelectionPopup.Open));
			this.ClanParties = new ClanPartiesVM(new Action(this.OnAnyExpenseChange), this._openPartyAsManage, new Action(this.RefreshCategoryValues), new Action<ClanCardSelectionInfo>(this.CardSelectionPopup.Open));
			this.ClanIncome = new ClanIncomeVM(new Action(this.RefreshCategoryValues), new Action<ClanCardSelectionInfo>(this.CardSelectionPopup.Open));
			this._categoryCount = 4;
			this.SetSelectedCategory(0);
			this.Leader = new HeroVM(this._clan.Leader, false);
			this.CurrentRenown = (int)Clan.PlayerClan.Renown;
			this.CurrentTier = Clan.PlayerClan.Tier;
			TextObject textObject;
			if (Campaign.Current.Models.ClanTierModel.HasUpcomingTier(Clan.PlayerClan, out textObject, false).Item2)
			{
				this.NextTierRenown = Clan.PlayerClan.RenownRequirementForNextTier;
				this.MinRenownForCurrentTier = Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(this.CurrentTier);
				this.NextTier = Clan.PlayerClan.Tier + 1;
				this.IsRenownProgressComplete = false;
			}
			else
			{
				this.NextTierRenown = 1;
				this.MinRenownForCurrentTier = 1;
				this.NextTier = 0;
				this.IsRenownProgressComplete = true;
			}
			this.CurrentRenownOverPreviousTier = this.CurrentRenown - this.MinRenownForCurrentTier;
			this.CurrentTierRenownRange = this.NextTierRenown - this.MinRenownForCurrentTier;
			this.RenownHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetClanRenownTooltip(Clan.PlayerClan));
			this.GoldChangeTooltip = CampaignUIHelper.GetDenarTooltip();
			this.RefreshDailyValues();
			this.CanChooseBanner = true;
			TextObject hintText;
			this.PlayerCanChangeClanName = this.GetPlayerCanChangeClanNameWithReason(out hintText);
			this.ChangeClanNameHint = new HintViewModel(hintText, null);
			this.TutorialNotification = new ElementNotificationVM();
			this.UpdateKingdomRelatedProperties();
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x06001AD3 RID: 6867 RVA: 0x00064525 File Offset: 0x00062725
		protected virtual ClanFiefsVM CreateFiefsDataSource(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
		{
			return new ClanFiefsVM(onRefresh, openCardSelectionPopup);
		}

		// Token: 0x06001AD4 RID: 6868 RVA: 0x00064530 File Offset: 0x00062730
		private bool GetPlayerCanChangeClanNameWithReason(out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (this._clan.Leader != Hero.MainHero)
			{
				disabledReason = new TextObject("{=GCaYjA5W}You need to be the leader of the clan to change it's name.", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06001AD5 RID: 6869 RVA: 0x00064574 File Offset: 0x00062774
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = Hero.MainHero.Clan.Name.ToString();
			this.CurrentGoldText = GameTexts.FindText("str_clan_finance_current_gold", null).ToString();
			this.TotalExpensesText = GameTexts.FindText("str_clan_finance_total_expenses", null).ToString();
			this.TotalIncomeText = GameTexts.FindText("str_clan_finance_total_income", null).ToString();
			this.DailyChangeText = GameTexts.FindText("str_clan_finance_daily_change", null).ToString();
			this.ExpectedGoldText = GameTexts.FindText("str_clan_finance_expected", null).ToString();
			this.ExpenseText = GameTexts.FindText("str_clan_expenses", null).ToString();
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.PartiesText = GameTexts.FindText("str_parties", null).ToString();
			this.IncomeText = GameTexts.FindText("str_other", null).ToString();
			this.FiefsText = GameTexts.FindText("str_fiefs", null).ToString();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.LeaderText = GameTexts.FindText("str_sort_by_leader_name_label", null).ToString();
			this.FinanceText = GameTexts.FindText("str_finance", null).ToString();
			GameTexts.SetVariable("TIER", Clan.PlayerClan.Tier);
			this.CurrentRenownText = GameTexts.FindText("str_clan_tier", null).ToString();
			ElementNotificationVM tutorialNotification = this.TutorialNotification;
			if (tutorialNotification != null)
			{
				tutorialNotification.RefreshValues();
			}
			ClanMembersVM clanMembers = this._clanMembers;
			if (clanMembers != null)
			{
				clanMembers.RefreshValues();
			}
			ClanPartiesVM clanParties = this._clanParties;
			if (clanParties != null)
			{
				clanParties.RefreshValues();
			}
			ClanFiefsVM clanFiefs = this._clanFiefs;
			if (clanFiefs != null)
			{
				clanFiefs.RefreshValues();
			}
			ClanIncomeVM clanIncome = this._clanIncome;
			if (clanIncome != null)
			{
				clanIncome.RefreshValues();
			}
			HeroVM leader = this._leader;
			if (leader == null)
			{
				return;
			}
			leader.RefreshValues();
		}

		// Token: 0x06001AD6 RID: 6870 RVA: 0x0006474E File Offset: 0x0006294E
		public void SelectHero(Hero hero)
		{
			this.SetSelectedCategory(0);
			this.ClanMembers.SelectMember(hero);
		}

		// Token: 0x06001AD7 RID: 6871 RVA: 0x00064763 File Offset: 0x00062963
		public void SelectParty(PartyBase party)
		{
			this.SetSelectedCategory(1);
			this.ClanParties.SelectParty(party);
		}

		// Token: 0x06001AD8 RID: 6872 RVA: 0x00064778 File Offset: 0x00062978
		public void SelectSettlement(Settlement settlement)
		{
			this.SetSelectedCategory(2);
			this.ClanFiefs.SelectFief(settlement);
		}

		// Token: 0x06001AD9 RID: 6873 RVA: 0x0006478D File Offset: 0x0006298D
		public void SelectWorkshop(Workshop workshop)
		{
			this.SetSelectedCategory(3);
			this.ClanIncome.SelectWorkshop(workshop);
		}

		// Token: 0x06001ADA RID: 6874 RVA: 0x000647A2 File Offset: 0x000629A2
		public void SelectAlley(Alley alley)
		{
			this.SetSelectedCategory(3);
			this.ClanIncome.SelectAlley(alley);
		}

		// Token: 0x06001ADB RID: 6875 RVA: 0x000647B8 File Offset: 0x000629B8
		public void SelectPreviousCategory()
		{
			int selectedCategory = ((this._currentCategory == 0) ? (this._categoryCount - 1) : (this._currentCategory - 1));
			this.SetSelectedCategory(selectedCategory);
		}

		// Token: 0x06001ADC RID: 6876 RVA: 0x000647E8 File Offset: 0x000629E8
		public void SelectNextCategory()
		{
			int selectedCategory = (this._currentCategory + 1) % this._categoryCount;
			this.SetSelectedCategory(selectedCategory);
		}

		// Token: 0x06001ADD RID: 6877 RVA: 0x0006480C File Offset: 0x00062A0C
		public void ExecuteOpenBannerEditor()
		{
			this._openBannerEditor();
		}

		// Token: 0x06001ADE RID: 6878 RVA: 0x00064819 File Offset: 0x00062A19
		public void UpdateBannerVisuals()
		{
			this.ClanBanner = new BannerImageIdentifierVM(this._clan.Banner, true);
			this.ClanBannerHint = new HintViewModel(new TextObject("{=t1lSXN9O}Your clan's standard carried into battle", null), null);
			this.RefreshValues();
		}

		// Token: 0x06001ADF RID: 6879 RVA: 0x00064850 File Offset: 0x00062A50
		public void SetSelectedCategory(int index)
		{
			this.ClanMembers.IsSelected = false;
			this.ClanParties.IsSelected = false;
			this.ClanFiefs.IsSelected = false;
			this.ClanIncome.IsSelected = false;
			this._currentCategory = index;
			if (index == 0)
			{
				this.ClanMembers.IsSelected = true;
			}
			else if (index == 1)
			{
				this.ClanParties.IsSelected = true;
			}
			else if (index == 2)
			{
				this.ClanFiefs.IsSelected = true;
			}
			else
			{
				this._currentCategory = 3;
				this.ClanIncome.IsSelected = true;
			}
			this.IsMembersSelected = this.ClanMembers.IsSelected;
			this.IsPartiesSelected = this.ClanParties.IsSelected;
			this.IsFiefsSelected = this.ClanFiefs.IsSelected;
			this.IsIncomeSelected = this.ClanIncome.IsSelected;
		}

		// Token: 0x06001AE0 RID: 6880 RVA: 0x00064920 File Offset: 0x00062B20
		private void UpdateKingdomRelatedProperties()
		{
			this.ClanIsInAKingdom = this._clan.Kingdom != null;
			if (this.ClanIsInAKingdom)
			{
				if (this._clan.Kingdom.RulingClan == this._clan)
				{
					this.IsKingdomActionEnabled = false;
					this.KingdomActionDisabledReasonHint = new BasicTooltipViewModel(() => new TextObject("{=vIPrZCZ1}You can abdicate leadership from the kingdom screen.", null).ToString());
					this.KingdomActionText = GameTexts.FindText("str_abdicate_leadership", null).ToString();
				}
				else
				{
					this.IsKingdomActionEnabled = MobileParty.MainParty.Army == null;
					this.KingdomActionText = GameTexts.FindText("str_leave_kingdom", null).ToString();
					this.KingdomActionDisabledReasonHint = new BasicTooltipViewModel();
				}
			}
			else
			{
				List<TextObject> kingdomCreationDisabledReasons;
				this.IsKingdomActionEnabled = Campaign.Current.Models.KingdomCreationModel.IsPlayerKingdomCreationPossible(out kingdomCreationDisabledReasons);
				this.KingdomActionText = GameTexts.FindText("str_create_kingdom", null).ToString();
				this.KingdomActionDisabledReasonHint = new BasicTooltipViewModel(() => CampaignUIHelper.MergeTextObjectsWithNewline(kingdomCreationDisabledReasons));
			}
			this.UpdateBannerVisuals();
		}

		// Token: 0x06001AE1 RID: 6881 RVA: 0x00064A44 File Offset: 0x00062C44
		public void RefreshDailyValues()
		{
			if (this.ClanIncome != null)
			{
				this.CurrentGold = Hero.MainHero.Gold;
				this.TotalIncome = (int)Campaign.Current.Models.ClanFinanceModel.CalculateClanIncome(this._clan, false, false, false).ResultNumber;
				this.TotalExpenses = (int)Campaign.Current.Models.ClanFinanceModel.CalculateClanExpenses(this._clan, false, false, false).ResultNumber;
				this.DailyChange = MathF.Abs(this.TotalIncome) - MathF.Abs(this.TotalExpenses);
				this.ExpectedGold = this.CurrentGold + this.DailyChange;
				if (this.TotalIncome == 0)
				{
					this.TotalIncomeValueText = GameTexts.FindText("str_clan_finance_value_zero", null).ToString();
				}
				else
				{
					GameTexts.SetVariable("IS_POSITIVE", (this.TotalIncome > 0) ? 1 : 0);
					GameTexts.SetVariable("NUMBER", MathF.Abs(this.TotalIncome));
					this.TotalIncomeValueText = GameTexts.FindText("str_clan_finance_value", null).ToString();
				}
				if (this.TotalExpenses == 0)
				{
					this.TotalExpensesValueText = GameTexts.FindText("str_clan_finance_value_zero", null).ToString();
				}
				else
				{
					GameTexts.SetVariable("IS_POSITIVE", (this.TotalExpenses > 0) ? 1 : 0);
					GameTexts.SetVariable("NUMBER", MathF.Abs(this.TotalExpenses));
					this.TotalExpensesValueText = GameTexts.FindText("str_clan_finance_value", null).ToString();
				}
				if (this.DailyChange == 0)
				{
					this.DailyChangeValueText = GameTexts.FindText("str_clan_finance_value_zero", null).ToString();
					return;
				}
				GameTexts.SetVariable("IS_POSITIVE", (this.DailyChange > 0) ? 1 : 0);
				GameTexts.SetVariable("NUMBER", MathF.Abs(this.DailyChange));
				this.DailyChangeValueText = GameTexts.FindText("str_clan_finance_value", null).ToString();
			}
		}

		// Token: 0x06001AE2 RID: 6882 RVA: 0x00064C19 File Offset: 0x00062E19
		public void RefreshCategoryValues()
		{
			this.ClanFiefs.RefreshAllLists();
			this.ClanMembers.RefreshMembersList();
			this.ClanParties.RefreshPartiesList();
			this.ClanIncome.RefreshList();
		}

		// Token: 0x06001AE3 RID: 6883 RVA: 0x00064C48 File Offset: 0x00062E48
		public void ExecuteChangeClanName()
		{
			GameTexts.SetVariable("MAX_LETTER_COUNT", 50);
			GameTexts.SetVariable("MIN_LETTER_COUNT", 1);
			InformationManager.ShowTextInquiry(new TextInquiryData(GameTexts.FindText("str_change_clan_name", null).ToString(), string.Empty, true, true, GameTexts.FindText("str_done", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action<string>(this.OnChangeClanNameDone), null, false, new Func<string, Tuple<bool, string>>(FactionHelper.IsClanNameApplicable), "", ""), false, false);
		}

		// Token: 0x06001AE4 RID: 6884 RVA: 0x00064CD4 File Offset: 0x00062ED4
		private void OnChangeClanNameDone(string newClanName)
		{
			TextObject textObject = GameTexts.FindText("str_generic_clan_name", null);
			textObject.SetTextVariable("CLAN_NAME", new TextObject(newClanName, null));
			this._clan.ChangeClanName(textObject, textObject);
			this.RefreshCategoryValues();
			this.RefreshValues();
		}

		// Token: 0x06001AE5 RID: 6885 RVA: 0x00064D19 File Offset: 0x00062F19
		private void OnAnyExpenseChange()
		{
			this.RefreshDailyValues();
		}

		// Token: 0x06001AE6 RID: 6886 RVA: 0x00064D21 File Offset: 0x00062F21
		public void ExecuteClose()
		{
			this._onClose();
		}

		// Token: 0x06001AE7 RID: 6887 RVA: 0x00064D30 File Offset: 0x00062F30
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.ClanFiefs.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.PreviousTabInputKey.OnFinalize();
			this.NextTabInputKey.OnFinalize();
			this.CardSelectionPopup.OnFinalize();
			this.ClanMembers.OnFinalize();
			this.ClanParties.OnFinalize();
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x1700090D RID: 2317
		// (get) Token: 0x06001AE8 RID: 6888 RVA: 0x00064DAB File Offset: 0x00062FAB
		// (set) Token: 0x06001AE9 RID: 6889 RVA: 0x00064DB3 File Offset: 0x00062FB3
		[DataSourceProperty]
		public HeroVM Leader
		{
			get
			{
				return this._leader;
			}
			set
			{
				if (value != this._leader)
				{
					this._leader = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Leader");
				}
			}
		}

		// Token: 0x1700090E RID: 2318
		// (get) Token: 0x06001AEA RID: 6890 RVA: 0x00064DD1 File Offset: 0x00062FD1
		// (set) Token: 0x06001AEB RID: 6891 RVA: 0x00064DD9 File Offset: 0x00062FD9
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner
		{
			get
			{
				return this._clanBanner;
			}
			set
			{
				if (value != this._clanBanner)
				{
					this._clanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner");
				}
			}
		}

		// Token: 0x1700090F RID: 2319
		// (get) Token: 0x06001AEC RID: 6892 RVA: 0x00064DF7 File Offset: 0x00062FF7
		// (set) Token: 0x06001AED RID: 6893 RVA: 0x00064DFF File Offset: 0x00062FFF
		[DataSourceProperty]
		public ClanCardSelectionPopupVM CardSelectionPopup
		{
			get
			{
				return this._cardSelectionPopup;
			}
			set
			{
				if (value != this._cardSelectionPopup)
				{
					this._cardSelectionPopup = value;
					base.OnPropertyChangedWithValue<ClanCardSelectionPopupVM>(value, "CardSelectionPopup");
				}
			}
		}

		// Token: 0x17000910 RID: 2320
		// (get) Token: 0x06001AEE RID: 6894 RVA: 0x00064E1D File Offset: 0x0006301D
		// (set) Token: 0x06001AEF RID: 6895 RVA: 0x00064E25 File Offset: 0x00063025
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

		// Token: 0x17000911 RID: 2321
		// (get) Token: 0x06001AF0 RID: 6896 RVA: 0x00064E48 File Offset: 0x00063048
		// (set) Token: 0x06001AF1 RID: 6897 RVA: 0x00064E50 File Offset: 0x00063050
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

		// Token: 0x17000912 RID: 2322
		// (get) Token: 0x06001AF2 RID: 6898 RVA: 0x00064E73 File Offset: 0x00063073
		// (set) Token: 0x06001AF3 RID: 6899 RVA: 0x00064E7B File Offset: 0x0006307B
		[DataSourceProperty]
		public ClanMembersVM ClanMembers
		{
			get
			{
				return this._clanMembers;
			}
			set
			{
				if (value != this._clanMembers)
				{
					this._clanMembers = value;
					base.OnPropertyChangedWithValue<ClanMembersVM>(value, "ClanMembers");
				}
			}
		}

		// Token: 0x17000913 RID: 2323
		// (get) Token: 0x06001AF4 RID: 6900 RVA: 0x00064E99 File Offset: 0x00063099
		// (set) Token: 0x06001AF5 RID: 6901 RVA: 0x00064EA1 File Offset: 0x000630A1
		[DataSourceProperty]
		public ClanPartiesVM ClanParties
		{
			get
			{
				return this._clanParties;
			}
			set
			{
				if (value != this._clanParties)
				{
					this._clanParties = value;
					base.OnPropertyChangedWithValue<ClanPartiesVM>(value, "ClanParties");
				}
			}
		}

		// Token: 0x17000914 RID: 2324
		// (get) Token: 0x06001AF6 RID: 6902 RVA: 0x00064EBF File Offset: 0x000630BF
		// (set) Token: 0x06001AF7 RID: 6903 RVA: 0x00064EC7 File Offset: 0x000630C7
		[DataSourceProperty]
		public ClanFiefsVM ClanFiefs
		{
			get
			{
				return this._clanFiefs;
			}
			set
			{
				if (value != this._clanFiefs)
				{
					this._clanFiefs = value;
					base.OnPropertyChangedWithValue<ClanFiefsVM>(value, "ClanFiefs");
				}
			}
		}

		// Token: 0x17000915 RID: 2325
		// (get) Token: 0x06001AF8 RID: 6904 RVA: 0x00064EE5 File Offset: 0x000630E5
		// (set) Token: 0x06001AF9 RID: 6905 RVA: 0x00064EED File Offset: 0x000630ED
		[DataSourceProperty]
		public ClanIncomeVM ClanIncome
		{
			get
			{
				return this._clanIncome;
			}
			set
			{
				if (value != this._clanIncome)
				{
					this._clanIncome = value;
					base.OnPropertyChangedWithValue<ClanIncomeVM>(value, "ClanIncome");
				}
			}
		}

		// Token: 0x17000916 RID: 2326
		// (get) Token: 0x06001AFA RID: 6906 RVA: 0x00064F0B File Offset: 0x0006310B
		// (set) Token: 0x06001AFB RID: 6907 RVA: 0x00064F13 File Offset: 0x00063113
		[DataSourceProperty]
		public bool IsMembersSelected
		{
			get
			{
				return this._isMembersSelected;
			}
			set
			{
				if (value != this._isMembersSelected)
				{
					this._isMembersSelected = value;
					base.OnPropertyChangedWithValue(value, "IsMembersSelected");
				}
			}
		}

		// Token: 0x17000917 RID: 2327
		// (get) Token: 0x06001AFC RID: 6908 RVA: 0x00064F31 File Offset: 0x00063131
		// (set) Token: 0x06001AFD RID: 6909 RVA: 0x00064F39 File Offset: 0x00063139
		[DataSourceProperty]
		public bool IsPartiesSelected
		{
			get
			{
				return this._isPartiesSelected;
			}
			set
			{
				if (value != this._isPartiesSelected)
				{
					this._isPartiesSelected = value;
					base.OnPropertyChangedWithValue(value, "IsPartiesSelected");
				}
			}
		}

		// Token: 0x17000918 RID: 2328
		// (get) Token: 0x06001AFE RID: 6910 RVA: 0x00064F57 File Offset: 0x00063157
		// (set) Token: 0x06001AFF RID: 6911 RVA: 0x00064F5F File Offset: 0x0006315F
		[DataSourceProperty]
		public bool CanSwitchTabs
		{
			get
			{
				return this._canSwitchTabs;
			}
			set
			{
				if (value != this._canSwitchTabs)
				{
					this._canSwitchTabs = value;
					base.OnPropertyChangedWithValue(value, "CanSwitchTabs");
				}
			}
		}

		// Token: 0x17000919 RID: 2329
		// (get) Token: 0x06001B00 RID: 6912 RVA: 0x00064F7D File Offset: 0x0006317D
		// (set) Token: 0x06001B01 RID: 6913 RVA: 0x00064F85 File Offset: 0x00063185
		[DataSourceProperty]
		public bool IsFiefsSelected
		{
			get
			{
				return this._isFiefsSelected;
			}
			set
			{
				if (value != this._isFiefsSelected)
				{
					this._isFiefsSelected = value;
					base.OnPropertyChangedWithValue(value, "IsFiefsSelected");
				}
			}
		}

		// Token: 0x1700091A RID: 2330
		// (get) Token: 0x06001B02 RID: 6914 RVA: 0x00064FA3 File Offset: 0x000631A3
		// (set) Token: 0x06001B03 RID: 6915 RVA: 0x00064FAB File Offset: 0x000631AB
		[DataSourceProperty]
		public bool IsIncomeSelected
		{
			get
			{
				return this._isIncomeSelected;
			}
			set
			{
				if (value != this._isIncomeSelected)
				{
					this._isIncomeSelected = value;
					base.OnPropertyChangedWithValue(value, "IsIncomeSelected");
				}
			}
		}

		// Token: 0x1700091B RID: 2331
		// (get) Token: 0x06001B04 RID: 6916 RVA: 0x00064FC9 File Offset: 0x000631C9
		// (set) Token: 0x06001B05 RID: 6917 RVA: 0x00064FD1 File Offset: 0x000631D1
		[DataSourceProperty]
		public bool ClanIsInAKingdom
		{
			get
			{
				return this._clanIsInAKingdom;
			}
			set
			{
				if (value != this._clanIsInAKingdom)
				{
					this._clanIsInAKingdom = value;
					base.OnPropertyChangedWithValue(value, "ClanIsInAKingdom");
				}
			}
		}

		// Token: 0x1700091C RID: 2332
		// (get) Token: 0x06001B06 RID: 6918 RVA: 0x00064FEF File Offset: 0x000631EF
		// (set) Token: 0x06001B07 RID: 6919 RVA: 0x00064FF7 File Offset: 0x000631F7
		[DataSourceProperty]
		public bool IsKingdomActionEnabled
		{
			get
			{
				return this._isKingdomActionEnabled;
			}
			set
			{
				if (value != this._isKingdomActionEnabled)
				{
					this._isKingdomActionEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsKingdomActionEnabled");
				}
			}
		}

		// Token: 0x1700091D RID: 2333
		// (get) Token: 0x06001B08 RID: 6920 RVA: 0x00065015 File Offset: 0x00063215
		// (set) Token: 0x06001B09 RID: 6921 RVA: 0x0006501D File Offset: 0x0006321D
		[DataSourceProperty]
		public bool PlayerCanChangeClanName
		{
			get
			{
				return this._playerCanChangeClanName;
			}
			set
			{
				if (value != this._playerCanChangeClanName)
				{
					this._playerCanChangeClanName = value;
					base.OnPropertyChangedWithValue(value, "PlayerCanChangeClanName");
				}
			}
		}

		// Token: 0x1700091E RID: 2334
		// (get) Token: 0x06001B0A RID: 6922 RVA: 0x0006503B File Offset: 0x0006323B
		// (set) Token: 0x06001B0B RID: 6923 RVA: 0x00065043 File Offset: 0x00063243
		[DataSourceProperty]
		public bool CanChooseBanner
		{
			get
			{
				return this._canChooseBanner;
			}
			set
			{
				if (value != this._canChooseBanner)
				{
					this._canChooseBanner = value;
					base.OnPropertyChangedWithValue(value, "CanChooseBanner");
				}
			}
		}

		// Token: 0x1700091F RID: 2335
		// (get) Token: 0x06001B0C RID: 6924 RVA: 0x00065061 File Offset: 0x00063261
		// (set) Token: 0x06001B0D RID: 6925 RVA: 0x00065069 File Offset: 0x00063269
		[DataSourceProperty]
		public bool IsRenownProgressComplete
		{
			get
			{
				return this._isRenownProgressComplete;
			}
			set
			{
				if (value != this._isRenownProgressComplete)
				{
					this._isRenownProgressComplete = value;
					base.OnPropertyChangedWithValue(value, "IsRenownProgressComplete");
				}
			}
		}

		// Token: 0x17000920 RID: 2336
		// (get) Token: 0x06001B0E RID: 6926 RVA: 0x00065087 File Offset: 0x00063287
		// (set) Token: 0x06001B0F RID: 6927 RVA: 0x0006508F File Offset: 0x0006328F
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x17000921 RID: 2337
		// (get) Token: 0x06001B10 RID: 6928 RVA: 0x000650B2 File Offset: 0x000632B2
		// (set) Token: 0x06001B11 RID: 6929 RVA: 0x000650BA File Offset: 0x000632BA
		[DataSourceProperty]
		public string CurrentRenownText
		{
			get
			{
				return this._currentRenownText;
			}
			set
			{
				if (value != this._currentRenownText)
				{
					this._currentRenownText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentRenownText");
				}
			}
		}

		// Token: 0x17000922 RID: 2338
		// (get) Token: 0x06001B12 RID: 6930 RVA: 0x000650DD File Offset: 0x000632DD
		// (set) Token: 0x06001B13 RID: 6931 RVA: 0x000650E5 File Offset: 0x000632E5
		[DataSourceProperty]
		public string KingdomActionText
		{
			get
			{
				return this._kingdomActionText;
			}
			set
			{
				if (value != this._kingdomActionText)
				{
					this._kingdomActionText = value;
					base.OnPropertyChangedWithValue<string>(value, "KingdomActionText");
				}
			}
		}

		// Token: 0x17000923 RID: 2339
		// (get) Token: 0x06001B14 RID: 6932 RVA: 0x00065108 File Offset: 0x00063308
		// (set) Token: 0x06001B15 RID: 6933 RVA: 0x00065110 File Offset: 0x00063310
		[DataSourceProperty]
		public int NextTierRenown
		{
			get
			{
				return this._nextTierRenown;
			}
			set
			{
				if (value != this._nextTierRenown)
				{
					this._nextTierRenown = value;
					base.OnPropertyChangedWithValue(value, "NextTierRenown");
				}
			}
		}

		// Token: 0x17000924 RID: 2340
		// (get) Token: 0x06001B16 RID: 6934 RVA: 0x0006512E File Offset: 0x0006332E
		// (set) Token: 0x06001B17 RID: 6935 RVA: 0x00065136 File Offset: 0x00063336
		[DataSourceProperty]
		public int CurrentTier
		{
			get
			{
				return this._currentTier;
			}
			set
			{
				if (value != this._currentTier)
				{
					this._currentTier = value;
					base.OnPropertyChangedWithValue(value, "CurrentTier");
				}
			}
		}

		// Token: 0x17000925 RID: 2341
		// (get) Token: 0x06001B18 RID: 6936 RVA: 0x00065154 File Offset: 0x00063354
		// (set) Token: 0x06001B19 RID: 6937 RVA: 0x0006515C File Offset: 0x0006335C
		[DataSourceProperty]
		public int MinRenownForCurrentTier
		{
			get
			{
				return this._minRenownForCurrentTier;
			}
			set
			{
				if (value != this._minRenownForCurrentTier)
				{
					this._minRenownForCurrentTier = value;
					base.OnPropertyChangedWithValue(value, "MinRenownForCurrentTier");
				}
			}
		}

		// Token: 0x17000926 RID: 2342
		// (get) Token: 0x06001B1A RID: 6938 RVA: 0x0006517A File Offset: 0x0006337A
		// (set) Token: 0x06001B1B RID: 6939 RVA: 0x00065182 File Offset: 0x00063382
		[DataSourceProperty]
		public int NextTier
		{
			get
			{
				return this._nextTier;
			}
			set
			{
				if (value != this._nextTier)
				{
					this._nextTier = value;
					base.OnPropertyChangedWithValue(value, "NextTier");
				}
			}
		}

		// Token: 0x17000927 RID: 2343
		// (get) Token: 0x06001B1C RID: 6940 RVA: 0x000651A0 File Offset: 0x000633A0
		// (set) Token: 0x06001B1D RID: 6941 RVA: 0x000651A8 File Offset: 0x000633A8
		[DataSourceProperty]
		public int CurrentRenown
		{
			get
			{
				return this._currentRenown;
			}
			set
			{
				if (value != this._currentRenown)
				{
					this._currentRenown = value;
					base.OnPropertyChangedWithValue(value, "CurrentRenown");
				}
			}
		}

		// Token: 0x17000928 RID: 2344
		// (get) Token: 0x06001B1E RID: 6942 RVA: 0x000651C6 File Offset: 0x000633C6
		// (set) Token: 0x06001B1F RID: 6943 RVA: 0x000651CE File Offset: 0x000633CE
		[DataSourceProperty]
		public int CurrentTierRenownRange
		{
			get
			{
				return this._currentTierRenownRange;
			}
			set
			{
				if (value != this._currentTierRenownRange)
				{
					this._currentTierRenownRange = value;
					base.OnPropertyChangedWithValue(value, "CurrentTierRenownRange");
				}
			}
		}

		// Token: 0x17000929 RID: 2345
		// (get) Token: 0x06001B20 RID: 6944 RVA: 0x000651EC File Offset: 0x000633EC
		// (set) Token: 0x06001B21 RID: 6945 RVA: 0x000651F4 File Offset: 0x000633F4
		[DataSourceProperty]
		public int CurrentRenownOverPreviousTier
		{
			get
			{
				return this._currentRenownOverPreviousTier;
			}
			set
			{
				if (value != this._currentRenownOverPreviousTier)
				{
					this._currentRenownOverPreviousTier = value;
					base.OnPropertyChangedWithValue(value, "CurrentRenownOverPreviousTier");
				}
			}
		}

		// Token: 0x1700092A RID: 2346
		// (get) Token: 0x06001B22 RID: 6946 RVA: 0x00065212 File Offset: 0x00063412
		// (set) Token: 0x06001B23 RID: 6947 RVA: 0x0006521A File Offset: 0x0006341A
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

		// Token: 0x1700092B RID: 2347
		// (get) Token: 0x06001B24 RID: 6948 RVA: 0x0006523D File Offset: 0x0006343D
		// (set) Token: 0x06001B25 RID: 6949 RVA: 0x00065245 File Offset: 0x00063445
		[DataSourceProperty]
		public string PartiesText
		{
			get
			{
				return this._partiesText;
			}
			set
			{
				if (value != this._partiesText)
				{
					this._partiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartiesText");
				}
			}
		}

		// Token: 0x1700092C RID: 2348
		// (get) Token: 0x06001B26 RID: 6950 RVA: 0x00065268 File Offset: 0x00063468
		// (set) Token: 0x06001B27 RID: 6951 RVA: 0x00065270 File Offset: 0x00063470
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

		// Token: 0x1700092D RID: 2349
		// (get) Token: 0x06001B28 RID: 6952 RVA: 0x00065293 File Offset: 0x00063493
		// (set) Token: 0x06001B29 RID: 6953 RVA: 0x0006529B File Offset: 0x0006349B
		[DataSourceProperty]
		public string IncomeText
		{
			get
			{
				return this._incomeText;
			}
			set
			{
				if (value != this._incomeText)
				{
					this._incomeText = value;
					base.OnPropertyChanged("OtherText");
				}
			}
		}

		// Token: 0x1700092E RID: 2350
		// (get) Token: 0x06001B2A RID: 6954 RVA: 0x000652BD File Offset: 0x000634BD
		// (set) Token: 0x06001B2B RID: 6955 RVA: 0x000652C5 File Offset: 0x000634C5
		[DataSourceProperty]
		public BasicTooltipViewModel RenownHint
		{
			get
			{
				return this._renownHint;
			}
			set
			{
				if (value != this._renownHint)
				{
					this._renownHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "RenownHint");
				}
			}
		}

		// Token: 0x1700092F RID: 2351
		// (get) Token: 0x06001B2C RID: 6956 RVA: 0x000652E3 File Offset: 0x000634E3
		// (set) Token: 0x06001B2D RID: 6957 RVA: 0x000652EB File Offset: 0x000634EB
		[DataSourceProperty]
		public HintViewModel ClanBannerHint
		{
			get
			{
				return this._clanBannerHint;
			}
			set
			{
				if (value != this._clanBannerHint)
				{
					this._clanBannerHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ClanBannerHint");
				}
			}
		}

		// Token: 0x17000930 RID: 2352
		// (get) Token: 0x06001B2E RID: 6958 RVA: 0x00065309 File Offset: 0x00063509
		// (set) Token: 0x06001B2F RID: 6959 RVA: 0x00065311 File Offset: 0x00063511
		[DataSourceProperty]
		public HintViewModel ChangeClanNameHint
		{
			get
			{
				return this._changeClanNameHint;
			}
			set
			{
				if (value != this._changeClanNameHint)
				{
					this._changeClanNameHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ChangeClanNameHint");
				}
			}
		}

		// Token: 0x17000931 RID: 2353
		// (get) Token: 0x06001B30 RID: 6960 RVA: 0x0006532F File Offset: 0x0006352F
		// (set) Token: 0x06001B31 RID: 6961 RVA: 0x00065337 File Offset: 0x00063537
		[DataSourceProperty]
		public BasicTooltipViewModel KingdomActionDisabledReasonHint
		{
			get
			{
				return this._kingdomActionDisabledReasonHint;
			}
			set
			{
				if (value != this._kingdomActionDisabledReasonHint)
				{
					this._kingdomActionDisabledReasonHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "KingdomActionDisabledReasonHint");
				}
			}
		}

		// Token: 0x17000932 RID: 2354
		// (get) Token: 0x06001B32 RID: 6962 RVA: 0x00065355 File Offset: 0x00063555
		// (set) Token: 0x06001B33 RID: 6963 RVA: 0x0006535D File Offset: 0x0006355D
		[DataSourceProperty]
		public TooltipTriggerVM GoldChangeTooltip
		{
			get
			{
				return this._goldChangeTooltip;
			}
			set
			{
				if (value != this._goldChangeTooltip)
				{
					this._goldChangeTooltip = value;
					base.OnPropertyChangedWithValue<TooltipTriggerVM>(value, "GoldChangeTooltip");
				}
			}
		}

		// Token: 0x17000933 RID: 2355
		// (get) Token: 0x06001B34 RID: 6964 RVA: 0x0006537B File Offset: 0x0006357B
		// (set) Token: 0x06001B35 RID: 6965 RVA: 0x00065383 File Offset: 0x00063583
		[DataSourceProperty]
		public string CurrentGoldText
		{
			get
			{
				return this._currentGoldText;
			}
			set
			{
				if (value != this._currentGoldText)
				{
					this._currentGoldText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentGoldText");
				}
			}
		}

		// Token: 0x17000934 RID: 2356
		// (get) Token: 0x06001B36 RID: 6966 RVA: 0x000653A6 File Offset: 0x000635A6
		// (set) Token: 0x06001B37 RID: 6967 RVA: 0x000653AE File Offset: 0x000635AE
		[DataSourceProperty]
		public int CurrentGold
		{
			get
			{
				return this._currentGold;
			}
			set
			{
				if (value != this._currentGold)
				{
					this._currentGold = value;
					base.OnPropertyChangedWithValue(value, "CurrentGold");
				}
			}
		}

		// Token: 0x17000935 RID: 2357
		// (get) Token: 0x06001B38 RID: 6968 RVA: 0x000653CC File Offset: 0x000635CC
		// (set) Token: 0x06001B39 RID: 6969 RVA: 0x000653D4 File Offset: 0x000635D4
		[DataSourceProperty]
		public string ExpenseText
		{
			get
			{
				return this._expenseText;
			}
			set
			{
				if (value != this._expenseText)
				{
					this._expenseText = value;
					base.OnPropertyChangedWithValue<string>(value, "ExpenseText");
				}
			}
		}

		// Token: 0x17000936 RID: 2358
		// (get) Token: 0x06001B3A RID: 6970 RVA: 0x000653F7 File Offset: 0x000635F7
		// (set) Token: 0x06001B3B RID: 6971 RVA: 0x000653FF File Offset: 0x000635FF
		[DataSourceProperty]
		public string TotalIncomeText
		{
			get
			{
				return this._totalIncomeText;
			}
			set
			{
				if (value != this._totalIncomeText)
				{
					this._totalIncomeText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalIncomeText");
				}
			}
		}

		// Token: 0x17000937 RID: 2359
		// (get) Token: 0x06001B3C RID: 6972 RVA: 0x00065422 File Offset: 0x00063622
		// (set) Token: 0x06001B3D RID: 6973 RVA: 0x0006542A File Offset: 0x0006362A
		[DataSourceProperty]
		public string FinanceText
		{
			get
			{
				return this._financeText;
			}
			set
			{
				if (value != this._financeText)
				{
					this._financeText = value;
					base.OnPropertyChangedWithValue<string>(value, "FinanceText");
				}
			}
		}

		// Token: 0x17000938 RID: 2360
		// (get) Token: 0x06001B3E RID: 6974 RVA: 0x0006544D File Offset: 0x0006364D
		// (set) Token: 0x06001B3F RID: 6975 RVA: 0x00065455 File Offset: 0x00063655
		[DataSourceProperty]
		public int TotalIncome
		{
			get
			{
				return this._totalIncome;
			}
			set
			{
				if (value != this._totalIncome)
				{
					this._totalIncome = value;
					base.OnPropertyChangedWithValue(value, "TotalIncome");
				}
			}
		}

		// Token: 0x17000939 RID: 2361
		// (get) Token: 0x06001B40 RID: 6976 RVA: 0x00065473 File Offset: 0x00063673
		// (set) Token: 0x06001B41 RID: 6977 RVA: 0x0006547B File Offset: 0x0006367B
		[DataSourceProperty]
		public string TotalExpensesText
		{
			get
			{
				return this._totalExpensesText;
			}
			set
			{
				if (value != this._totalExpensesText)
				{
					this._totalExpensesText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalExpensesText");
				}
			}
		}

		// Token: 0x1700093A RID: 2362
		// (get) Token: 0x06001B42 RID: 6978 RVA: 0x0006549E File Offset: 0x0006369E
		// (set) Token: 0x06001B43 RID: 6979 RVA: 0x000654A6 File Offset: 0x000636A6
		[DataSourceProperty]
		public int TotalExpenses
		{
			get
			{
				return this._totalExpenses;
			}
			set
			{
				if (value != this._totalExpenses)
				{
					this._totalExpenses = value;
					base.OnPropertyChangedWithValue(value, "TotalExpenses");
				}
			}
		}

		// Token: 0x1700093B RID: 2363
		// (get) Token: 0x06001B44 RID: 6980 RVA: 0x000654C4 File Offset: 0x000636C4
		// (set) Token: 0x06001B45 RID: 6981 RVA: 0x000654CC File Offset: 0x000636CC
		[DataSourceProperty]
		public string DailyChangeText
		{
			get
			{
				return this._dailyChangeText;
			}
			set
			{
				if (value != this._dailyChangeText)
				{
					this._dailyChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "DailyChangeText");
				}
			}
		}

		// Token: 0x1700093C RID: 2364
		// (get) Token: 0x06001B46 RID: 6982 RVA: 0x000654EF File Offset: 0x000636EF
		// (set) Token: 0x06001B47 RID: 6983 RVA: 0x000654F7 File Offset: 0x000636F7
		[DataSourceProperty]
		public int DailyChange
		{
			get
			{
				return this._dailyChange;
			}
			set
			{
				if (value != this._dailyChange)
				{
					this._dailyChange = value;
					base.OnPropertyChangedWithValue(value, "DailyChange");
				}
			}
		}

		// Token: 0x1700093D RID: 2365
		// (get) Token: 0x06001B48 RID: 6984 RVA: 0x00065515 File Offset: 0x00063715
		// (set) Token: 0x06001B49 RID: 6985 RVA: 0x0006551D File Offset: 0x0006371D
		[DataSourceProperty]
		public string ExpectedGoldText
		{
			get
			{
				return this._expectedGoldText;
			}
			set
			{
				if (value != this._expectedGoldText)
				{
					this._expectedGoldText = value;
					base.OnPropertyChangedWithValue<string>(value, "ExpectedGoldText");
				}
			}
		}

		// Token: 0x1700093E RID: 2366
		// (get) Token: 0x06001B4A RID: 6986 RVA: 0x00065540 File Offset: 0x00063740
		// (set) Token: 0x06001B4B RID: 6987 RVA: 0x00065548 File Offset: 0x00063748
		[DataSourceProperty]
		public int ExpectedGold
		{
			get
			{
				return this._expectedGold;
			}
			set
			{
				if (value != this._expectedGold)
				{
					this._expectedGold = value;
					base.OnPropertyChangedWithValue(value, "ExpectedGold");
				}
			}
		}

		// Token: 0x1700093F RID: 2367
		// (get) Token: 0x06001B4C RID: 6988 RVA: 0x00065566 File Offset: 0x00063766
		// (set) Token: 0x06001B4D RID: 6989 RVA: 0x0006556E File Offset: 0x0006376E
		[DataSourceProperty]
		public string DailyChangeValueText
		{
			get
			{
				return this._dailyChangeValueText;
			}
			set
			{
				if (value != this._dailyChangeValueText)
				{
					this._dailyChangeValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "DailyChangeValueText");
				}
			}
		}

		// Token: 0x17000940 RID: 2368
		// (get) Token: 0x06001B4E RID: 6990 RVA: 0x00065591 File Offset: 0x00063791
		// (set) Token: 0x06001B4F RID: 6991 RVA: 0x00065599 File Offset: 0x00063799
		[DataSourceProperty]
		public string TotalExpensesValueText
		{
			get
			{
				return this._totalExpensesValueText;
			}
			set
			{
				if (value != this._totalExpensesValueText)
				{
					this._totalExpensesValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalExpensesValueText");
				}
			}
		}

		// Token: 0x17000941 RID: 2369
		// (get) Token: 0x06001B50 RID: 6992 RVA: 0x000655BC File Offset: 0x000637BC
		// (set) Token: 0x06001B51 RID: 6993 RVA: 0x000655C4 File Offset: 0x000637C4
		[DataSourceProperty]
		public string TotalIncomeValueText
		{
			get
			{
				return this._totalIncomeValueText;
			}
			set
			{
				if (value != this._totalIncomeValueText)
				{
					this._totalIncomeValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalIncomeValueText");
				}
			}
		}

		// Token: 0x06001B52 RID: 6994 RVA: 0x000655E7 File Offset: 0x000637E7
		public void SetDoneInputKey(HotKey hotkey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
			this.CardSelectionPopup.SetDoneInputKey(hotkey);
		}

		// Token: 0x06001B53 RID: 6995 RVA: 0x00065602 File Offset: 0x00063802
		public void SetCancelInputKey(HotKey hotkey)
		{
			this.CardSelectionPopup.SetCancelInputKey(hotkey);
		}

		// Token: 0x06001B54 RID: 6996 RVA: 0x00065610 File Offset: 0x00063810
		public void SetPreviousTabInputKey(HotKey hotkey)
		{
			this.PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06001B55 RID: 6997 RVA: 0x0006561F File Offset: 0x0006381F
		public void SetNextTabInputKey(HotKey hotkey)
		{
			this.NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x17000942 RID: 2370
		// (get) Token: 0x06001B56 RID: 6998 RVA: 0x0006562E File Offset: 0x0006382E
		// (set) Token: 0x06001B57 RID: 6999 RVA: 0x00065636 File Offset: 0x00063836
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000943 RID: 2371
		// (get) Token: 0x06001B58 RID: 7000 RVA: 0x00065654 File Offset: 0x00063854
		// (set) Token: 0x06001B59 RID: 7001 RVA: 0x0006565C File Offset: 0x0006385C
		public InputKeyItemVM PreviousTabInputKey
		{
			get
			{
				return this._previousTabInputKey;
			}
			set
			{
				if (value != this._previousTabInputKey)
				{
					this._previousTabInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PreviousTabInputKey");
				}
			}
		}

		// Token: 0x17000944 RID: 2372
		// (get) Token: 0x06001B5A RID: 7002 RVA: 0x0006567A File Offset: 0x0006387A
		// (set) Token: 0x06001B5B RID: 7003 RVA: 0x00065682 File Offset: 0x00063882
		public InputKeyItemVM NextTabInputKey
		{
			get
			{
				return this._nextTabInputKey;
			}
			set
			{
				if (value != this._nextTabInputKey)
				{
					this._nextTabInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "NextTabInputKey");
				}
			}
		}

		// Token: 0x17000945 RID: 2373
		// (get) Token: 0x06001B5C RID: 7004 RVA: 0x000656A0 File Offset: 0x000638A0
		// (set) Token: 0x06001B5D RID: 7005 RVA: 0x000656A8 File Offset: 0x000638A8
		[DataSourceProperty]
		public ElementNotificationVM TutorialNotification
		{
			get
			{
				return this._tutorialNotification;
			}
			set
			{
				if (value != this._tutorialNotification)
				{
					this._tutorialNotification = value;
					base.OnPropertyChangedWithValue<ElementNotificationVM>(value, "TutorialNotification");
				}
			}
		}

		// Token: 0x06001B5E RID: 7006 RVA: 0x000656C8 File Offset: 0x000638C8
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = string.Empty;
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = this._latestTutorialElementID;
					if (this._latestTutorialElementID == "RoleAssignmentWidget")
					{
						this.SetSelectedCategory(1);
					}
				}
			}
		}

		// Token: 0x04000C84 RID: 3204
		private readonly Action _onClose;

		// Token: 0x04000C85 RID: 3205
		private readonly Action _openBannerEditor;

		// Token: 0x04000C86 RID: 3206
		private readonly Action<Hero> _openPartyAsManage;

		// Token: 0x04000C87 RID: 3207
		private readonly Action<Hero> _showHeroOnMap;

		// Token: 0x04000C88 RID: 3208
		private readonly Clan _clan;

		// Token: 0x04000C89 RID: 3209
		private readonly int _categoryCount;

		// Token: 0x04000C8A RID: 3210
		private int _currentCategory;

		// Token: 0x04000C8B RID: 3211
		private ClanMembersVM _clanMembers;

		// Token: 0x04000C8C RID: 3212
		private ClanPartiesVM _clanParties;

		// Token: 0x04000C8D RID: 3213
		private ClanFiefsVM _clanFiefs;

		// Token: 0x04000C8E RID: 3214
		private ClanIncomeVM _clanIncome;

		// Token: 0x04000C8F RID: 3215
		private HeroVM _leader;

		// Token: 0x04000C90 RID: 3216
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x04000C91 RID: 3217
		private ClanCardSelectionPopupVM _cardSelectionPopup;

		// Token: 0x04000C92 RID: 3218
		private bool _canSwitchTabs;

		// Token: 0x04000C93 RID: 3219
		private bool _isPartiesSelected;

		// Token: 0x04000C94 RID: 3220
		private bool _isMembersSelected;

		// Token: 0x04000C95 RID: 3221
		private bool _isFiefsSelected;

		// Token: 0x04000C96 RID: 3222
		private bool _isIncomeSelected;

		// Token: 0x04000C97 RID: 3223
		private bool _canChooseBanner;

		// Token: 0x04000C98 RID: 3224
		private bool _isRenownProgressComplete;

		// Token: 0x04000C99 RID: 3225
		private bool _playerCanChangeClanName;

		// Token: 0x04000C9A RID: 3226
		private bool _clanIsInAKingdom;

		// Token: 0x04000C9B RID: 3227
		private string _doneLbl;

		// Token: 0x04000C9C RID: 3228
		private bool _isKingdomActionEnabled;

		// Token: 0x04000C9D RID: 3229
		private string _name;

		// Token: 0x04000C9E RID: 3230
		private string _kingdomActionText;

		// Token: 0x04000C9F RID: 3231
		private string _leaderText;

		// Token: 0x04000CA0 RID: 3232
		private int _minRenownForCurrentTier;

		// Token: 0x04000CA1 RID: 3233
		private int _currentRenown;

		// Token: 0x04000CA2 RID: 3234
		private int _currentTier = -1;

		// Token: 0x04000CA3 RID: 3235
		private int _nextTierRenown;

		// Token: 0x04000CA4 RID: 3236
		private int _nextTier;

		// Token: 0x04000CA5 RID: 3237
		private int _currentTierRenownRange;

		// Token: 0x04000CA6 RID: 3238
		private int _currentRenownOverPreviousTier;

		// Token: 0x04000CA7 RID: 3239
		private string _currentRenownText;

		// Token: 0x04000CA8 RID: 3240
		private string _membersText;

		// Token: 0x04000CA9 RID: 3241
		private string _partiesText;

		// Token: 0x04000CAA RID: 3242
		private string _fiefsText;

		// Token: 0x04000CAB RID: 3243
		private string _incomeText;

		// Token: 0x04000CAC RID: 3244
		private BasicTooltipViewModel _renownHint;

		// Token: 0x04000CAD RID: 3245
		private BasicTooltipViewModel _kingdomActionDisabledReasonHint;

		// Token: 0x04000CAE RID: 3246
		private HintViewModel _clanBannerHint;

		// Token: 0x04000CAF RID: 3247
		private HintViewModel _changeClanNameHint;

		// Token: 0x04000CB0 RID: 3248
		private string _financeText;

		// Token: 0x04000CB1 RID: 3249
		private string _currentGoldText;

		// Token: 0x04000CB2 RID: 3250
		private int _currentGold;

		// Token: 0x04000CB3 RID: 3251
		private string _totalIncomeText;

		// Token: 0x04000CB4 RID: 3252
		private int _totalIncome;

		// Token: 0x04000CB5 RID: 3253
		private string _totalIncomeValueText;

		// Token: 0x04000CB6 RID: 3254
		private string _totalExpensesText;

		// Token: 0x04000CB7 RID: 3255
		private int _totalExpenses;

		// Token: 0x04000CB8 RID: 3256
		private string _totalExpensesValueText;

		// Token: 0x04000CB9 RID: 3257
		private string _dailyChangeText;

		// Token: 0x04000CBA RID: 3258
		private int _dailyChange;

		// Token: 0x04000CBB RID: 3259
		private string _dailyChangeValueText;

		// Token: 0x04000CBC RID: 3260
		private string _expectedGoldText;

		// Token: 0x04000CBD RID: 3261
		private int _expectedGold;

		// Token: 0x04000CBE RID: 3262
		private string _expenseText;

		// Token: 0x04000CBF RID: 3263
		private TooltipTriggerVM _goldChangeTooltip;

		// Token: 0x04000CC0 RID: 3264
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000CC1 RID: 3265
		private InputKeyItemVM _previousTabInputKey;

		// Token: 0x04000CC2 RID: 3266
		private InputKeyItemVM _nextTabInputKey;

		// Token: 0x04000CC3 RID: 3267
		private ElementNotificationVM _tutorialNotification;

		// Token: 0x04000CC4 RID: 3268
		private string _latestTutorialElementID;
	}
}
