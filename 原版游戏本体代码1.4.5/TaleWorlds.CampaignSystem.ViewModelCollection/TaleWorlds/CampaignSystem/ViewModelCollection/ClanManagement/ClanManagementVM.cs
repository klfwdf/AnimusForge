using System;
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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanManagementVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action _openBannerEditor;

	private readonly Action<Hero> _openPartyAsManage;

	private readonly Action<Hero> _showHeroOnMap;

	private readonly Clan _clan;

	private readonly int _categoryCount;

	private int _currentCategory;

	private ClanMembersVM _clanMembers;

	private ClanPartiesVM _clanParties;

	private ClanFiefsVM _clanFiefs;

	private ClanIncomeVM _clanIncome;

	private HeroVM _leader;

	private BannerImageIdentifierVM _clanBanner;

	private ClanCardSelectionPopupVM _cardSelectionPopup;

	private bool _canSwitchTabs;

	private bool _isPartiesSelected;

	private bool _isMembersSelected;

	private bool _isFiefsSelected;

	private bool _isIncomeSelected;

	private bool _canChooseBanner;

	private bool _isRenownProgressComplete;

	private bool _playerCanChangeClanName;

	private bool _clanIsInAKingdom;

	private string _doneLbl;

	private bool _isKingdomActionEnabled;

	private string _name;

	private string _kingdomActionText;

	private string _leaderText;

	private int _minRenownForCurrentTier;

	private int _currentRenown;

	private int _currentTier = -1;

	private int _nextTierRenown;

	private int _nextTier;

	private int _currentTierRenownRange;

	private int _currentRenownOverPreviousTier;

	private string _currentRenownText;

	private string _membersText;

	private string _partiesText;

	private string _fiefsText;

	private string _incomeText;

	private BasicTooltipViewModel _renownHint;

	private BasicTooltipViewModel _kingdomActionDisabledReasonHint;

	private HintViewModel _clanBannerHint;

	private HintViewModel _changeClanNameHint;

	private string _financeText;

	private string _currentGoldText;

	private int _currentGold;

	private string _totalIncomeText;

	private int _totalIncome;

	private string _totalIncomeValueText;

	private string _totalExpensesText;

	private int _totalExpenses;

	private string _totalExpensesValueText;

	private string _dailyChangeText;

	private int _dailyChange;

	private string _dailyChangeValueText;

	private string _expectedGoldText;

	private int _expectedGold;

	private string _expenseText;

	private TooltipTriggerVM _goldChangeTooltip;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _previousTabInputKey;

	private InputKeyItemVM _nextTabInputKey;

	private ElementNotificationVM _tutorialNotification;

	private string _latestTutorialElementID;

	[DataSourceProperty]
	public HeroVM Leader
	{
		get
		{
			return _leader;
		}
		set
		{
			if (value != _leader)
			{
				_leader = value;
				OnPropertyChangedWithValue(value, "Leader");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public ClanCardSelectionPopupVM CardSelectionPopup
	{
		get
		{
			return _cardSelectionPopup;
		}
		set
		{
			if (value != _cardSelectionPopup)
			{
				_cardSelectionPopup = value;
				OnPropertyChangedWithValue(value, "CardSelectionPopup");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string LeaderText
	{
		get
		{
			return _leaderText;
		}
		set
		{
			if (value != _leaderText)
			{
				_leaderText = value;
				OnPropertyChangedWithValue(value, "LeaderText");
			}
		}
	}

	[DataSourceProperty]
	public ClanMembersVM ClanMembers
	{
		get
		{
			return _clanMembers;
		}
		set
		{
			if (value != _clanMembers)
			{
				_clanMembers = value;
				OnPropertyChangedWithValue(value, "ClanMembers");
			}
		}
	}

	[DataSourceProperty]
	public ClanPartiesVM ClanParties
	{
		get
		{
			return _clanParties;
		}
		set
		{
			if (value != _clanParties)
			{
				_clanParties = value;
				OnPropertyChangedWithValue(value, "ClanParties");
			}
		}
	}

	[DataSourceProperty]
	public ClanFiefsVM ClanFiefs
	{
		get
		{
			return _clanFiefs;
		}
		set
		{
			if (value != _clanFiefs)
			{
				_clanFiefs = value;
				OnPropertyChangedWithValue(value, "ClanFiefs");
			}
		}
	}

	[DataSourceProperty]
	public ClanIncomeVM ClanIncome
	{
		get
		{
			return _clanIncome;
		}
		set
		{
			if (value != _clanIncome)
			{
				_clanIncome = value;
				OnPropertyChangedWithValue(value, "ClanIncome");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMembersSelected
	{
		get
		{
			return _isMembersSelected;
		}
		set
		{
			if (value != _isMembersSelected)
			{
				_isMembersSelected = value;
				OnPropertyChangedWithValue(value, "IsMembersSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPartiesSelected
	{
		get
		{
			return _isPartiesSelected;
		}
		set
		{
			if (value != _isPartiesSelected)
			{
				_isPartiesSelected = value;
				OnPropertyChangedWithValue(value, "IsPartiesSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSwitchTabs
	{
		get
		{
			return _canSwitchTabs;
		}
		set
		{
			if (value != _canSwitchTabs)
			{
				_canSwitchTabs = value;
				OnPropertyChangedWithValue(value, "CanSwitchTabs");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFiefsSelected
	{
		get
		{
			return _isFiefsSelected;
		}
		set
		{
			if (value != _isFiefsSelected)
			{
				_isFiefsSelected = value;
				OnPropertyChangedWithValue(value, "IsFiefsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsIncomeSelected
	{
		get
		{
			return _isIncomeSelected;
		}
		set
		{
			if (value != _isIncomeSelected)
			{
				_isIncomeSelected = value;
				OnPropertyChangedWithValue(value, "IsIncomeSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool ClanIsInAKingdom
	{
		get
		{
			return _clanIsInAKingdom;
		}
		set
		{
			if (value != _clanIsInAKingdom)
			{
				_clanIsInAKingdom = value;
				OnPropertyChangedWithValue(value, "ClanIsInAKingdom");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKingdomActionEnabled
	{
		get
		{
			return _isKingdomActionEnabled;
		}
		set
		{
			if (value != _isKingdomActionEnabled)
			{
				_isKingdomActionEnabled = value;
				OnPropertyChangedWithValue(value, "IsKingdomActionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerCanChangeClanName
	{
		get
		{
			return _playerCanChangeClanName;
		}
		set
		{
			if (value != _playerCanChangeClanName)
			{
				_playerCanChangeClanName = value;
				OnPropertyChangedWithValue(value, "PlayerCanChangeClanName");
			}
		}
	}

	[DataSourceProperty]
	public bool CanChooseBanner
	{
		get
		{
			return _canChooseBanner;
		}
		set
		{
			if (value != _canChooseBanner)
			{
				_canChooseBanner = value;
				OnPropertyChangedWithValue(value, "CanChooseBanner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRenownProgressComplete
	{
		get
		{
			return _isRenownProgressComplete;
		}
		set
		{
			if (value != _isRenownProgressComplete)
			{
				_isRenownProgressComplete = value;
				OnPropertyChangedWithValue(value, "IsRenownProgressComplete");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentRenownText
	{
		get
		{
			return _currentRenownText;
		}
		set
		{
			if (value != _currentRenownText)
			{
				_currentRenownText = value;
				OnPropertyChangedWithValue(value, "CurrentRenownText");
			}
		}
	}

	[DataSourceProperty]
	public string KingdomActionText
	{
		get
		{
			return _kingdomActionText;
		}
		set
		{
			if (value != _kingdomActionText)
			{
				_kingdomActionText = value;
				OnPropertyChangedWithValue(value, "KingdomActionText");
			}
		}
	}

	[DataSourceProperty]
	public int NextTierRenown
	{
		get
		{
			return _nextTierRenown;
		}
		set
		{
			if (value != _nextTierRenown)
			{
				_nextTierRenown = value;
				OnPropertyChangedWithValue(value, "NextTierRenown");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentTier
	{
		get
		{
			return _currentTier;
		}
		set
		{
			if (value != _currentTier)
			{
				_currentTier = value;
				OnPropertyChangedWithValue(value, "CurrentTier");
			}
		}
	}

	[DataSourceProperty]
	public int MinRenownForCurrentTier
	{
		get
		{
			return _minRenownForCurrentTier;
		}
		set
		{
			if (value != _minRenownForCurrentTier)
			{
				_minRenownForCurrentTier = value;
				OnPropertyChangedWithValue(value, "MinRenownForCurrentTier");
			}
		}
	}

	[DataSourceProperty]
	public int NextTier
	{
		get
		{
			return _nextTier;
		}
		set
		{
			if (value != _nextTier)
			{
				_nextTier = value;
				OnPropertyChangedWithValue(value, "NextTier");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentRenown
	{
		get
		{
			return _currentRenown;
		}
		set
		{
			if (value != _currentRenown)
			{
				_currentRenown = value;
				OnPropertyChangedWithValue(value, "CurrentRenown");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentTierRenownRange
	{
		get
		{
			return _currentTierRenownRange;
		}
		set
		{
			if (value != _currentTierRenownRange)
			{
				_currentTierRenownRange = value;
				OnPropertyChangedWithValue(value, "CurrentTierRenownRange");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentRenownOverPreviousTier
	{
		get
		{
			return _currentRenownOverPreviousTier;
		}
		set
		{
			if (value != _currentRenownOverPreviousTier)
			{
				_currentRenownOverPreviousTier = value;
				OnPropertyChangedWithValue(value, "CurrentRenownOverPreviousTier");
			}
		}
	}

	[DataSourceProperty]
	public string MembersText
	{
		get
		{
			return _membersText;
		}
		set
		{
			if (value != _membersText)
			{
				_membersText = value;
				OnPropertyChangedWithValue(value, "MembersText");
			}
		}
	}

	[DataSourceProperty]
	public string PartiesText
	{
		get
		{
			return _partiesText;
		}
		set
		{
			if (value != _partiesText)
			{
				_partiesText = value;
				OnPropertyChangedWithValue(value, "PartiesText");
			}
		}
	}

	[DataSourceProperty]
	public string FiefsText
	{
		get
		{
			return _fiefsText;
		}
		set
		{
			if (value != _fiefsText)
			{
				_fiefsText = value;
				OnPropertyChangedWithValue(value, "FiefsText");
			}
		}
	}

	[DataSourceProperty]
	public string IncomeText
	{
		get
		{
			return _incomeText;
		}
		set
		{
			if (value != _incomeText)
			{
				_incomeText = value;
				OnPropertyChanged("OtherText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel RenownHint
	{
		get
		{
			return _renownHint;
		}
		set
		{
			if (value != _renownHint)
			{
				_renownHint = value;
				OnPropertyChangedWithValue(value, "RenownHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ClanBannerHint
	{
		get
		{
			return _clanBannerHint;
		}
		set
		{
			if (value != _clanBannerHint)
			{
				_clanBannerHint = value;
				OnPropertyChangedWithValue(value, "ClanBannerHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ChangeClanNameHint
	{
		get
		{
			return _changeClanNameHint;
		}
		set
		{
			if (value != _changeClanNameHint)
			{
				_changeClanNameHint = value;
				OnPropertyChangedWithValue(value, "ChangeClanNameHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel KingdomActionDisabledReasonHint
	{
		get
		{
			return _kingdomActionDisabledReasonHint;
		}
		set
		{
			if (value != _kingdomActionDisabledReasonHint)
			{
				_kingdomActionDisabledReasonHint = value;
				OnPropertyChangedWithValue(value, "KingdomActionDisabledReasonHint");
			}
		}
	}

	[DataSourceProperty]
	public TooltipTriggerVM GoldChangeTooltip
	{
		get
		{
			return _goldChangeTooltip;
		}
		set
		{
			if (value != _goldChangeTooltip)
			{
				_goldChangeTooltip = value;
				OnPropertyChangedWithValue(value, "GoldChangeTooltip");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentGoldText
	{
		get
		{
			return _currentGoldText;
		}
		set
		{
			if (value != _currentGoldText)
			{
				_currentGoldText = value;
				OnPropertyChangedWithValue(value, "CurrentGoldText");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentGold
	{
		get
		{
			return _currentGold;
		}
		set
		{
			if (value != _currentGold)
			{
				_currentGold = value;
				OnPropertyChangedWithValue(value, "CurrentGold");
			}
		}
	}

	[DataSourceProperty]
	public string ExpenseText
	{
		get
		{
			return _expenseText;
		}
		set
		{
			if (value != _expenseText)
			{
				_expenseText = value;
				OnPropertyChangedWithValue(value, "ExpenseText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalIncomeText
	{
		get
		{
			return _totalIncomeText;
		}
		set
		{
			if (value != _totalIncomeText)
			{
				_totalIncomeText = value;
				OnPropertyChangedWithValue(value, "TotalIncomeText");
			}
		}
	}

	[DataSourceProperty]
	public string FinanceText
	{
		get
		{
			return _financeText;
		}
		set
		{
			if (value != _financeText)
			{
				_financeText = value;
				OnPropertyChangedWithValue(value, "FinanceText");
			}
		}
	}

	[DataSourceProperty]
	public int TotalIncome
	{
		get
		{
			return _totalIncome;
		}
		set
		{
			if (value != _totalIncome)
			{
				_totalIncome = value;
				OnPropertyChangedWithValue(value, "TotalIncome");
			}
		}
	}

	[DataSourceProperty]
	public string TotalExpensesText
	{
		get
		{
			return _totalExpensesText;
		}
		set
		{
			if (value != _totalExpensesText)
			{
				_totalExpensesText = value;
				OnPropertyChangedWithValue(value, "TotalExpensesText");
			}
		}
	}

	[DataSourceProperty]
	public int TotalExpenses
	{
		get
		{
			return _totalExpenses;
		}
		set
		{
			if (value != _totalExpenses)
			{
				_totalExpenses = value;
				OnPropertyChangedWithValue(value, "TotalExpenses");
			}
		}
	}

	[DataSourceProperty]
	public string DailyChangeText
	{
		get
		{
			return _dailyChangeText;
		}
		set
		{
			if (value != _dailyChangeText)
			{
				_dailyChangeText = value;
				OnPropertyChangedWithValue(value, "DailyChangeText");
			}
		}
	}

	[DataSourceProperty]
	public int DailyChange
	{
		get
		{
			return _dailyChange;
		}
		set
		{
			if (value != _dailyChange)
			{
				_dailyChange = value;
				OnPropertyChangedWithValue(value, "DailyChange");
			}
		}
	}

	[DataSourceProperty]
	public string ExpectedGoldText
	{
		get
		{
			return _expectedGoldText;
		}
		set
		{
			if (value != _expectedGoldText)
			{
				_expectedGoldText = value;
				OnPropertyChangedWithValue(value, "ExpectedGoldText");
			}
		}
	}

	[DataSourceProperty]
	public int ExpectedGold
	{
		get
		{
			return _expectedGold;
		}
		set
		{
			if (value != _expectedGold)
			{
				_expectedGold = value;
				OnPropertyChangedWithValue(value, "ExpectedGold");
			}
		}
	}

	[DataSourceProperty]
	public string DailyChangeValueText
	{
		get
		{
			return _dailyChangeValueText;
		}
		set
		{
			if (value != _dailyChangeValueText)
			{
				_dailyChangeValueText = value;
				OnPropertyChangedWithValue(value, "DailyChangeValueText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalExpensesValueText
	{
		get
		{
			return _totalExpensesValueText;
		}
		set
		{
			if (value != _totalExpensesValueText)
			{
				_totalExpensesValueText = value;
				OnPropertyChangedWithValue(value, "TotalExpensesValueText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalIncomeValueText
	{
		get
		{
			return _totalIncomeValueText;
		}
		set
		{
			if (value != _totalIncomeValueText)
			{
				_totalIncomeValueText = value;
				OnPropertyChangedWithValue(value, "TotalIncomeValueText");
			}
		}
	}

	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public InputKeyItemVM PreviousTabInputKey
	{
		get
		{
			return _previousTabInputKey;
		}
		set
		{
			if (value != _previousTabInputKey)
			{
				_previousTabInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousTabInputKey");
			}
		}
	}

	public InputKeyItemVM NextTabInputKey
	{
		get
		{
			return _nextTabInputKey;
		}
		set
		{
			if (value != _nextTabInputKey)
			{
				_nextTabInputKey = value;
				OnPropertyChangedWithValue(value, "NextTabInputKey");
			}
		}
	}

	[DataSourceProperty]
	public ElementNotificationVM TutorialNotification
	{
		get
		{
			return _tutorialNotification;
		}
		set
		{
			if (value != _tutorialNotification)
			{
				_tutorialNotification = value;
				OnPropertyChangedWithValue(value, "TutorialNotification");
			}
		}
	}

	public ClanManagementVM(Action onClose, Action<Hero> showHeroOnMap, Action<Hero> openPartyAsManage, Action openBannerEditor)
	{
		_onClose = onClose;
		_openPartyAsManage = openPartyAsManage;
		_openBannerEditor = openBannerEditor;
		_showHeroOnMap = showHeroOnMap;
		_clan = Hero.MainHero.Clan;
		CardSelectionPopup = new ClanCardSelectionPopupVM();
		ClanMembers = new ClanMembersVM(RefreshCategoryValues, _showHeroOnMap);
		ClanFiefs = CreateFiefsDataSource(RefreshCategoryValues, CardSelectionPopup.Open);
		ClanParties = new ClanPartiesVM(OnAnyExpenseChange, _openPartyAsManage, RefreshCategoryValues, CardSelectionPopup.Open);
		ClanIncome = new ClanIncomeVM(RefreshCategoryValues, CardSelectionPopup.Open);
		_categoryCount = 4;
		SetSelectedCategory(0);
		Leader = new HeroVM(_clan.Leader);
		CurrentRenown = (int)Clan.PlayerClan.Renown;
		CurrentTier = Clan.PlayerClan.Tier;
		if (Campaign.Current.Models.ClanTierModel.HasUpcomingTier(Clan.PlayerClan, out var _).Item2)
		{
			NextTierRenown = Clan.PlayerClan.RenownRequirementForNextTier;
			MinRenownForCurrentTier = Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(CurrentTier);
			NextTier = Clan.PlayerClan.Tier + 1;
			IsRenownProgressComplete = false;
		}
		else
		{
			NextTierRenown = 1;
			MinRenownForCurrentTier = 1;
			NextTier = 0;
			IsRenownProgressComplete = true;
		}
		CurrentRenownOverPreviousTier = CurrentRenown - MinRenownForCurrentTier;
		CurrentTierRenownRange = NextTierRenown - MinRenownForCurrentTier;
		RenownHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetClanRenownTooltip(Clan.PlayerClan));
		GoldChangeTooltip = CampaignUIHelper.GetDenarTooltip();
		RefreshDailyValues();
		CanChooseBanner = true;
		PlayerCanChangeClanName = GetPlayerCanChangeClanNameWithReason(out var disabledReason);
		ChangeClanNameHint = new HintViewModel(disabledReason);
		TutorialNotification = new ElementNotificationVM();
		UpdateKingdomRelatedProperties();
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	protected virtual ClanFiefsVM CreateFiefsDataSource(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
	{
		return new ClanFiefsVM(onRefresh, openCardSelectionPopup);
	}

	private bool GetPlayerCanChangeClanNameWithReason(out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (_clan.Leader != Hero.MainHero)
		{
			disabledReason = new TextObject("{=GCaYjA5W}You need to be the leader of the clan to change it's name.");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Hero.MainHero.Clan.Name.ToString();
		CurrentGoldText = GameTexts.FindText("str_clan_finance_current_gold").ToString();
		TotalExpensesText = GameTexts.FindText("str_clan_finance_total_expenses").ToString();
		TotalIncomeText = GameTexts.FindText("str_clan_finance_total_income").ToString();
		DailyChangeText = GameTexts.FindText("str_clan_finance_daily_change").ToString();
		ExpectedGoldText = GameTexts.FindText("str_clan_finance_expected").ToString();
		ExpenseText = GameTexts.FindText("str_clan_expenses").ToString();
		MembersText = GameTexts.FindText("str_members").ToString();
		PartiesText = GameTexts.FindText("str_parties").ToString();
		IncomeText = GameTexts.FindText("str_other").ToString();
		FiefsText = GameTexts.FindText("str_fiefs").ToString();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		LeaderText = GameTexts.FindText("str_sort_by_leader_name_label").ToString();
		FinanceText = GameTexts.FindText("str_finance").ToString();
		GameTexts.SetVariable("TIER", Clan.PlayerClan.Tier);
		CurrentRenownText = GameTexts.FindText("str_clan_tier").ToString();
		TutorialNotification?.RefreshValues();
		_clanMembers?.RefreshValues();
		_clanParties?.RefreshValues();
		_clanFiefs?.RefreshValues();
		_clanIncome?.RefreshValues();
		_leader?.RefreshValues();
	}

	public void SelectHero(Hero hero)
	{
		SetSelectedCategory(0);
		ClanMembers.SelectMember(hero);
	}

	public void SelectParty(PartyBase party)
	{
		SetSelectedCategory(1);
		ClanParties.SelectParty(party);
	}

	public void SelectSettlement(Settlement settlement)
	{
		SetSelectedCategory(2);
		ClanFiefs.SelectFief(settlement);
	}

	public void SelectWorkshop(Workshop workshop)
	{
		SetSelectedCategory(3);
		ClanIncome.SelectWorkshop(workshop);
	}

	public void SelectAlley(Alley alley)
	{
		SetSelectedCategory(3);
		ClanIncome.SelectAlley(alley);
	}

	public void SelectPreviousCategory()
	{
		int selectedCategory = ((_currentCategory == 0) ? (_categoryCount - 1) : (_currentCategory - 1));
		SetSelectedCategory(selectedCategory);
	}

	public void SelectNextCategory()
	{
		int selectedCategory = (_currentCategory + 1) % _categoryCount;
		SetSelectedCategory(selectedCategory);
	}

	public void ExecuteOpenBannerEditor()
	{
		_openBannerEditor();
	}

	public void UpdateBannerVisuals()
	{
		ClanBanner = new BannerImageIdentifierVM(_clan.Banner, nineGrid: true);
		ClanBannerHint = new HintViewModel(new TextObject("{=Nkue5MX8}Click to edit your clan's banner"));
		RefreshValues();
	}

	public void SetSelectedCategory(int index)
	{
		ClanMembers.IsSelected = false;
		ClanParties.IsSelected = false;
		ClanFiefs.IsSelected = false;
		ClanIncome.IsSelected = false;
		_currentCategory = index;
		switch (index)
		{
		case 0:
			ClanMembers.IsSelected = true;
			break;
		case 1:
			ClanParties.IsSelected = true;
			break;
		case 2:
			ClanFiefs.IsSelected = true;
			break;
		default:
			_currentCategory = 3;
			ClanIncome.IsSelected = true;
			break;
		}
		IsMembersSelected = ClanMembers.IsSelected;
		IsPartiesSelected = ClanParties.IsSelected;
		IsFiefsSelected = ClanFiefs.IsSelected;
		IsIncomeSelected = ClanIncome.IsSelected;
	}

	private void UpdateKingdomRelatedProperties()
	{
		ClanIsInAKingdom = _clan.Kingdom != null;
		if (ClanIsInAKingdom)
		{
			if (_clan.Kingdom.RulingClan == _clan)
			{
				IsKingdomActionEnabled = false;
				KingdomActionDisabledReasonHint = new BasicTooltipViewModel(() => new TextObject("{=vIPrZCZ1}You can abdicate leadership from the kingdom screen.").ToString());
				KingdomActionText = GameTexts.FindText("str_abdicate_leadership").ToString();
			}
			else
			{
				IsKingdomActionEnabled = MobileParty.MainParty.Army == null;
				KingdomActionText = GameTexts.FindText("str_leave_kingdom").ToString();
				KingdomActionDisabledReasonHint = new BasicTooltipViewModel();
			}
		}
		else
		{
			IsKingdomActionEnabled = Campaign.Current.Models.KingdomCreationModel.IsPlayerKingdomCreationPossible(out var kingdomCreationDisabledReasons);
			KingdomActionText = GameTexts.FindText("str_create_kingdom").ToString();
			KingdomActionDisabledReasonHint = new BasicTooltipViewModel(() => CampaignUIHelper.MergeTextObjectsWithNewline(kingdomCreationDisabledReasons));
		}
		UpdateBannerVisuals();
	}

	public void RefreshDailyValues()
	{
		if (ClanIncome != null)
		{
			CurrentGold = Hero.MainHero.Gold;
			TotalIncome = (int)Campaign.Current.Models.ClanFinanceModel.CalculateClanIncome(_clan).ResultNumber;
			TotalExpenses = (int)Campaign.Current.Models.ClanFinanceModel.CalculateClanExpenses(_clan).ResultNumber;
			DailyChange = TaleWorlds.Library.MathF.Abs(TotalIncome) - TaleWorlds.Library.MathF.Abs(TotalExpenses);
			ExpectedGold = CurrentGold + DailyChange;
			if (TotalIncome == 0)
			{
				TotalIncomeValueText = GameTexts.FindText("str_clan_finance_value_zero").ToString();
			}
			else
			{
				GameTexts.SetVariable("IS_POSITIVE", (TotalIncome > 0) ? 1 : 0);
				GameTexts.SetVariable("NUMBER", TaleWorlds.Library.MathF.Abs(TotalIncome));
				TotalIncomeValueText = GameTexts.FindText("str_clan_finance_value").ToString();
			}
			if (TotalExpenses == 0)
			{
				TotalExpensesValueText = GameTexts.FindText("str_clan_finance_value_zero").ToString();
			}
			else
			{
				GameTexts.SetVariable("IS_POSITIVE", (TotalExpenses > 0) ? 1 : 0);
				GameTexts.SetVariable("NUMBER", TaleWorlds.Library.MathF.Abs(TotalExpenses));
				TotalExpensesValueText = GameTexts.FindText("str_clan_finance_value").ToString();
			}
			if (DailyChange == 0)
			{
				DailyChangeValueText = GameTexts.FindText("str_clan_finance_value_zero").ToString();
				return;
			}
			GameTexts.SetVariable("IS_POSITIVE", (DailyChange > 0) ? 1 : 0);
			GameTexts.SetVariable("NUMBER", TaleWorlds.Library.MathF.Abs(DailyChange));
			DailyChangeValueText = GameTexts.FindText("str_clan_finance_value").ToString();
		}
	}

	public void RefreshCategoryValues()
	{
		ClanFiefs.RefreshAllLists();
		ClanMembers.RefreshMembersList();
		ClanParties.RefreshPartiesList();
		ClanIncome.RefreshList();
		RefreshDailyValues();
	}

	public void ExecuteChangeClanName()
	{
		GameTexts.SetVariable("MAX_LETTER_COUNT", 50);
		GameTexts.SetVariable("MIN_LETTER_COUNT", 1);
		InformationManager.ShowTextInquiry(new TextInquiryData(GameTexts.FindText("str_change_clan_name").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), OnChangeClanNameDone, null, shouldInputBeObfuscated: false, FactionHelper.IsClanNameApplicable));
	}

	private void OnChangeClanNameDone(string newClanName)
	{
		TextObject textObject = GameTexts.FindText("str_generic_clan_name");
		textObject.SetTextVariable("CLAN_NAME", new TextObject(newClanName));
		_clan.ChangeClanName(textObject, textObject);
		RefreshCategoryValues();
		RefreshValues();
	}

	private void OnAnyExpenseChange()
	{
		RefreshDailyValues();
	}

	public void ExecuteClose()
	{
		_onClose();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ClanFiefs.OnFinalize();
		DoneInputKey.OnFinalize();
		PreviousTabInputKey.OnFinalize();
		NextTabInputKey.OnFinalize();
		CardSelectionPopup.OnFinalize();
		ClanMembers.OnFinalize();
		ClanParties.OnFinalize();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
		CardSelectionPopup.SetDoneInputKey(hotkey);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CardSelectionPopup.SetCancelInputKey(hotkey);
	}

	public void SetPreviousTabInputKey(HotKey hotkey)
	{
		PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetNextTabInputKey(HotKey hotkey)
	{
		NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (!(obj.NewNotificationElementID != _latestTutorialElementID))
		{
			return;
		}
		if (_latestTutorialElementID != null)
		{
			TutorialNotification.ElementID = string.Empty;
		}
		_latestTutorialElementID = obj.NewNotificationElementID;
		if (_latestTutorialElementID != null)
		{
			TutorialNotification.ElementID = _latestTutorialElementID;
			if (_latestTutorialElementID == "RoleAssignmentWidget")
			{
				SetSelectedCategory(1);
			}
		}
	}
}
