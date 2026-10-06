using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;

public class RecruitmentVM : ViewModel
{
	private TextObject _recruitAllTextObject;

	private string _playerDoesntHaveEnoughMoneyStr;

	private string _playerIsOverPartyLimitStr;

	private Func<string, TextObject> _getKeyTextFromKeyId;

	private bool _isAvailableTroopsHighlightApplied;

	private string _latestTutorialElementID;

	private bool _enabled;

	private bool _isDoneEnabled;

	private bool _isPartyCapacityWarningEnabled;

	private bool _canRecruitAll;

	private string _titleText;

	private string _doneText;

	private string _recruitAllText;

	private string _resetAllText;

	private string _cancelText;

	private int _totalWealth;

	private int _partyCapacity;

	private int _initialPartySize;

	private int _currentPartySize;

	private MBBindingList<RecruitVolunteerVM> _volunteerList;

	private MBBindingList<RecruitVolunteerTroopVM> _troopsInCart;

	private int _partyWage;

	private string _partyCapacityText = "";

	private string _partyWageText = "";

	private string _partySpeedText = "";

	private string _remainingFoodText = "";

	private string _totalCostText = "";

	private RecruitVolunteerTroopVM _focusedVolunteerTroop;

	private RecruitVolunteerOwnerVM _focusedVolunteerOwner;

	private HintViewModel _partyWageHint;

	private HintViewModel _partyCapacityHint;

	private BasicTooltipViewModel _partySpeedHint;

	private HintViewModel _remainingFoodHint;

	private HintViewModel _totalWealthHint;

	private HintViewModel _totalCostHint;

	private HintViewModel _resetHint;

	private HintViewModel _doneHint;

	private BasicTooltipViewModel _recruitAllHint;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _recruitAllInputKey;

	public bool IsQuitting { get; private set; }

	[DataSourceProperty]
	public HintViewModel ResetHint
	{
		get
		{
			return _resetHint;
		}
		set
		{
			if (value != _resetHint)
			{
				_resetHint = value;
				OnPropertyChangedWithValue(value, "ResetHint");
			}
		}
	}

	[DataSourceProperty]
	public RecruitVolunteerTroopVM FocusedVolunteerTroop
	{
		get
		{
			return _focusedVolunteerTroop;
		}
		set
		{
			if (value != _focusedVolunteerTroop)
			{
				_focusedVolunteerTroop = value;
				OnPropertyChangedWithValue(value, "FocusedVolunteerTroop");
			}
		}
	}

	[DataSourceProperty]
	public RecruitVolunteerOwnerVM FocusedVolunteerOwner
	{
		get
		{
			return _focusedVolunteerOwner;
		}
		set
		{
			if (value != _focusedVolunteerOwner)
			{
				_focusedVolunteerOwner = value;
				OnPropertyChangedWithValue(value, "FocusedVolunteerOwner");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PartyWageHint
	{
		get
		{
			return _partyWageHint;
		}
		set
		{
			if (value != _partyWageHint)
			{
				_partyWageHint = value;
				OnPropertyChangedWithValue(value, "PartyWageHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PartyCapacityHint
	{
		get
		{
			return _partyCapacityHint;
		}
		set
		{
			if (value != _partyCapacityHint)
			{
				_partyCapacityHint = value;
				OnPropertyChangedWithValue(value, "PartyCapacityHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel PartySpeedHint
	{
		get
		{
			return _partySpeedHint;
		}
		set
		{
			if (value != _partySpeedHint)
			{
				_partySpeedHint = value;
				OnPropertyChangedWithValue(value, "PartySpeedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RemainingFoodHint
	{
		get
		{
			return _remainingFoodHint;
		}
		set
		{
			if (value != _remainingFoodHint)
			{
				_remainingFoodHint = value;
				OnPropertyChangedWithValue(value, "RemainingFoodHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TotalWealthHint
	{
		get
		{
			return _totalWealthHint;
		}
		set
		{
			if (value != _totalWealthHint)
			{
				_totalWealthHint = value;
				OnPropertyChangedWithValue(value, "TotalWealthHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TotalCostHint
	{
		get
		{
			return _totalCostHint;
		}
		set
		{
			if (value != _totalCostHint)
			{
				_totalCostHint = value;
				OnPropertyChangedWithValue(value, "TotalCostHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DoneHint
	{
		get
		{
			return _doneHint;
		}
		set
		{
			if (value != _doneHint)
			{
				_doneHint = value;
				OnPropertyChangedWithValue(value, "DoneHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel RecruitAllHint
	{
		get
		{
			return _recruitAllHint;
		}
		set
		{
			if (value != _recruitAllHint)
			{
				_recruitAllHint = value;
				OnPropertyChangedWithValue(value, "RecruitAllHint");
			}
		}
	}

	[DataSourceProperty]
	public int PartyWage
	{
		get
		{
			return _partyWage;
		}
		set
		{
			if (value != _partyWage)
			{
				_partyWage = value;
				OnPropertyChangedWithValue(value, "PartyWage");
			}
		}
	}

	[DataSourceProperty]
	public string PartyCapacityText
	{
		get
		{
			return _partyCapacityText;
		}
		set
		{
			if (value != _partyCapacityText)
			{
				_partyCapacityText = value;
				OnPropertyChangedWithValue(value, "PartyCapacityText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyWageText
	{
		get
		{
			return _partyWageText;
		}
		set
		{
			if (value != _partyWageText)
			{
				_partyWageText = value;
				OnPropertyChangedWithValue(value, "PartyWageText");
			}
		}
	}

	[DataSourceProperty]
	public string RecruitAllText
	{
		get
		{
			return _recruitAllText;
		}
		set
		{
			if (value != _recruitAllText)
			{
				_recruitAllText = value;
				OnPropertyChangedWithValue(value, "RecruitAllText");
			}
		}
	}

	[DataSourceProperty]
	public string PartySpeedText
	{
		get
		{
			return _partySpeedText;
		}
		set
		{
			if (value != _partySpeedText)
			{
				_partySpeedText = value;
				OnPropertyChangedWithValue(value, "PartySpeedText");
			}
		}
	}

	[DataSourceProperty]
	public string ResetAllText
	{
		get
		{
			return _resetAllText;
		}
		set
		{
			if (value != _resetAllText)
			{
				_resetAllText = value;
				OnPropertyChangedWithValue(value, "ResetAllText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string RemainingFoodText
	{
		get
		{
			return _remainingFoodText;
		}
		set
		{
			if (value != _remainingFoodText)
			{
				_remainingFoodText = value;
				OnPropertyChangedWithValue(value, "RemainingFoodText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalCostText
	{
		get
		{
			return _totalCostText;
		}
		set
		{
			if (value != _totalCostText)
			{
				_totalCostText = value;
				OnPropertyChangedWithValue(value, "TotalCostText");
			}
		}
	}

	[DataSourceProperty]
	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			if (value != _enabled)
			{
				_enabled = value;
				OnPropertyChangedWithValue(value, "Enabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDoneEnabled
	{
		get
		{
			return _isDoneEnabled;
		}
		set
		{
			if (value != _isDoneEnabled)
			{
				_isDoneEnabled = value;
				OnPropertyChangedWithValue(value, "IsDoneEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPartyCapacityWarningEnabled
	{
		get
		{
			return _isPartyCapacityWarningEnabled;
		}
		set
		{
			if (value != _isPartyCapacityWarningEnabled)
			{
				_isPartyCapacityWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsPartyCapacityWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanRecruitAll
	{
		get
		{
			return _canRecruitAll;
		}
		set
		{
			if (value != _canRecruitAll)
			{
				_canRecruitAll = value;
				OnPropertyChangedWithValue(value, "CanRecruitAll");
			}
		}
	}

	[DataSourceProperty]
	public int TotalWealth
	{
		get
		{
			return _totalWealth;
		}
		set
		{
			if (value != _totalWealth)
			{
				_totalWealth = value;
				OnPropertyChangedWithValue(value, "TotalWealth");
			}
		}
	}

	[DataSourceProperty]
	public int PartyCapacity
	{
		get
		{
			return _partyCapacity;
		}
		set
		{
			if (value != _partyCapacity)
			{
				_partyCapacity = value;
				OnPropertyChangedWithValue(value, "PartyCapacity");
			}
		}
	}

	[DataSourceProperty]
	public int InitialPartySize
	{
		get
		{
			return _initialPartySize;
		}
		set
		{
			if (value != _initialPartySize)
			{
				_initialPartySize = value;
				OnPropertyChangedWithValue(value, "InitialPartySize");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentPartySize
	{
		get
		{
			return _currentPartySize;
		}
		set
		{
			if (value != _currentPartySize)
			{
				_currentPartySize = value;
				OnPropertyChangedWithValue(value, "CurrentPartySize");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<RecruitVolunteerVM> VolunteerList
	{
		get
		{
			return _volunteerList;
		}
		set
		{
			if (value != _volunteerList)
			{
				_volunteerList = value;
				OnPropertyChangedWithValue(value, "VolunteerList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<RecruitVolunteerTroopVM> TroopsInCart
	{
		get
		{
			return _troopsInCart;
		}
		set
		{
			if (value != _troopsInCart)
			{
				_troopsInCart = value;
				OnPropertyChangedWithValue(value, "TroopsInCart");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
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

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM RecruitAllInputKey
	{
		get
		{
			return _recruitAllInputKey;
		}
		set
		{
			if (value != _recruitAllInputKey)
			{
				_recruitAllInputKey = value;
				OnPropertyChangedWithValue(value, "RecruitAllInputKey");
			}
		}
	}

	public RecruitmentVM()
	{
		VolunteerList = new MBBindingList<RecruitVolunteerVM>();
		TroopsInCart = new MBBindingList<RecruitVolunteerTroopVM>();
		RefreshValues();
		if (Settlement.CurrentSettlement != null)
		{
			RefreshScreen();
		}
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		RecruitVolunteerTroopVM.OnFocused = (Action<RecruitVolunteerTroopVM>)Delegate.Combine(RecruitVolunteerTroopVM.OnFocused, new Action<RecruitVolunteerTroopVM>(OnVolunteerTroopFocusChanged));
		RecruitVolunteerOwnerVM.OnFocused = (Action<RecruitVolunteerOwnerVM>)Delegate.Combine(RecruitVolunteerOwnerVM.OnFocused, new Action<RecruitVolunteerOwnerVM>(OnVolunteerOwnerFocusChanged));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PartyWageHint = new HintViewModel(GameTexts.FindText("str_weekly_wage"));
		TotalWealthHint = new HintViewModel(GameTexts.FindText("str_wealth"));
		TotalCostHint = new HintViewModel(GameTexts.FindText("str_total_cost"));
		PartyCapacityHint = new HintViewModel();
		PartySpeedHint = new BasicTooltipViewModel();
		RemainingFoodHint = new HintViewModel();
		DoneHint = new HintViewModel();
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset"));
		DoneText = GameTexts.FindText("str_done").ToString();
		TitleText = GameTexts.FindText("str_recruitment").ToString();
		_recruitAllTextObject = GameTexts.FindText("str_recruit_all");
		ResetAllText = GameTexts.FindText("str_reset_all").ToString();
		CancelText = GameTexts.FindText("str_party_cancel").ToString();
		_playerDoesntHaveEnoughMoneyStr = GameTexts.FindText("str_warning_you_dont_have_enough_money").ToString();
		_playerIsOverPartyLimitStr = GameTexts.FindText("str_party_size_limit_exceeded").ToString();
		VolunteerList.ApplyActionOnAllItems(delegate(RecruitVolunteerVM x)
		{
			x.RefreshValues();
		});
		TroopsInCart.ApplyActionOnAllItems(delegate(RecruitVolunteerTroopVM x)
		{
			x.RefreshValues();
		});
		SetRecruitAllHint();
		UpdateRecruitAllProperties();
		if (Settlement.CurrentSettlement != null)
		{
			RefreshScreen();
		}
	}

	public void RefreshScreen()
	{
		VolunteerList.Clear();
		TroopsInCart.Clear();
		int num = 0;
		InitialPartySize = PartyBase.MainParty.NumberOfAllMembers;
		RefreshPartyProperties();
		foreach (Hero notable in Settlement.CurrentSettlement.Notables)
		{
			if (notable.CanHaveRecruits)
			{
				MBTextManager.SetTextVariable("INDIVIDUAL_NAME", notable.Name);
				List<CharacterObject> volunteerTroopsOfHeroForRecruitment = HeroHelper.GetVolunteerTroopsOfHeroForRecruitment(notable);
				RecruitVolunteerVM item = new RecruitVolunteerVM(notable, volunteerTroopsOfHeroForRecruitment, OnRecruit, OnRemoveFromCart);
				VolunteerList.Add(item);
				num++;
			}
		}
		TotalWealth = Hero.MainHero.Gold;
		UpdateRecruitAllProperties();
	}

	private void OnRecruit(RecruitVolunteerVM recruitNotable, RecruitVolunteerTroopVM recruitTroop)
	{
		if (recruitTroop.CanBeRecruited)
		{
			recruitNotable.OnRecruitMoveToCart(recruitTroop);
			recruitTroop.CanBeRecruited = false;
			TroopsInCart.Add(recruitTroop);
			recruitTroop.IsInCart = true;
			CampaignEventDispatcher.Instance.OnPlayerStartRecruitment(recruitTroop.Character);
			RefreshPartyProperties();
		}
	}

	private void RefreshPartyProperties()
	{
		int num = TroopsInCart.Sum((RecruitVolunteerTroopVM t) => t.Wage);
		PartyWage = MobileParty.MainParty.TotalWage;
		if (num > 0)
		{
			PartyWageText = CampaignUIHelper.GetValueChangeText(PartyWage, num);
		}
		else
		{
			PartyWageText = PartyWage.ToString();
		}
		double num2 = 0.0;
		if (TroopsInCart.Count > 0)
		{
			int num3 = 0;
			int num4 = 0;
			foreach (RecruitVolunteerTroopVM item in TroopsInCart)
			{
				if (item.Character.IsMounted)
				{
					num4++;
				}
				else
				{
					num3++;
				}
			}
			ExplainedNumber finalSpeed = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(MobileParty.MainParty, includeDescriptions: false, num3, num4);
			ExplainedNumber explainedNumber = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(MobileParty.MainParty, finalSpeed);
			ExplainedNumber finalSpeed2 = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(MobileParty.MainParty);
			ExplainedNumber explainedNumber2 = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(MobileParty.MainParty, finalSpeed2);
			num2 = TaleWorlds.Library.MathF.Round(explainedNumber.ResultNumber, 1) - TaleWorlds.Library.MathF.Round(explainedNumber2.ResultNumber, 1);
		}
		PartySpeedText = MobileParty.MainParty.Speed.ToString("0.0");
		PartySpeedHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartySpeedTooltip(considerArmySpeed: false));
		if (num2 != 0.0)
		{
			PartySpeedText = CampaignUIHelper.GetValueChangeText(MobileParty.MainParty.Speed, (float)num2, "0.0");
		}
		int partySizeLimit = PartyBase.MainParty.PartySizeLimit;
		CurrentPartySize = PartyBase.MainParty.NumberOfAllMembers + TroopsInCart.Count;
		PartyCapacity = partySizeLimit;
		IsPartyCapacityWarningEnabled = CurrentPartySize > PartyCapacity;
		GameTexts.SetVariable("LEFT", CurrentPartySize.ToString());
		GameTexts.SetVariable("RIGHT", partySizeLimit.ToString());
		PartyCapacityText = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		PartyCapacityHint.HintText = new TextObject("{=!}" + PartyBase.MainParty.PartySizeLimitExplainer);
		float food = MobileParty.MainParty.Food;
		RemainingFoodText = TaleWorlds.Library.MathF.Round(food, 1).ToString();
		float foodChange = MobileParty.MainParty.FoodChange;
		int totalFoodAtInventory = MobileParty.MainParty.TotalFoodAtInventory;
		int numDaysForFoodToLast = MobileParty.MainParty.GetNumDaysForFoodToLast();
		MBTextManager.SetTextVariable("DAY_NUM", numDaysForFoodToLast);
		RemainingFoodHint.HintText = GameTexts.FindText("str_food_consumption_tooltip");
		RemainingFoodHint.HintText.SetTextVariable("DAILY_FOOD_CONSUMPTION", foodChange);
		RemainingFoodHint.HintText.SetTextVariable("REMAINING_DAYS", GameTexts.FindText("str_party_food_left"));
		RemainingFoodHint.HintText.SetTextVariable("TOTAL_FOOD_AMOUNT", ((double)totalFoodAtInventory + 0.01 * (double)PartyBase.MainParty.RemainingFoodPercentage).ToString("0.00"));
		RemainingFoodHint.HintText.SetTextVariable("TOTAL_FOOD", totalFoodAtInventory);
		int num5 = TroopsInCart.Sum((RecruitVolunteerTroopVM t) => t.Cost);
		TotalCostText = num5.ToString();
		bool doesPlayerHasEnoughMoney = (IsDoneEnabled = num5 <= Hero.MainHero.Gold);
		DoneHint.HintText = new TextObject("{=!}" + GetDoneHint(doesPlayerHasEnoughMoney));
		UpdateRecruitAllProperties();
	}

	public void ExecuteDone()
	{
		if (CurrentPartySize <= PartyCapacity)
		{
			OnDone();
			return;
		}
		GameTexts.SetVariable("newline", "\n");
		string text = GameTexts.FindText("str_party_over_limit_troops").ToString();
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=uJro3Bua}Over Limit").ToString(), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			OnDone();
		}, null));
	}

	private void OnDone()
	{
		RefreshPartyProperties();
		int num = TroopsInCart.Sum((RecruitVolunteerTroopVM t) => t.Cost);
		if (num > Hero.MainHero.Gold)
		{
			Debug.FailedAssert("Execution shouldn't come here. The checks should happen before", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Recruitment\\RecruitmentVM.cs", "OnDone", 229);
			return;
		}
		foreach (RecruitVolunteerTroopVM item in TroopsInCart)
		{
			item.Owner.OwnerHero.VolunteerTypes[item.Index] = null;
			MobileParty.MainParty.MemberRoster.AddToCounts(item.Character, 1);
			CampaignEventDispatcher.Instance.OnUnitRecruited(item.Character, 1);
		}
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num, disableNotification: true);
		if (num > 0)
		{
			MBTextManager.SetTextVariable("GOLD_AMOUNT", TaleWorlds.Library.MathF.Abs(num));
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_gold_removed_with_icon").ToString(), "event:/ui/notification/coins_negative"));
		}
		Deactivate();
	}

	public void ExecuteForceQuit()
	{
		if (IsQuitting)
		{
			return;
		}
		IsQuitting = true;
		if (TroopsInCart.Count > 0)
		{
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_quit").ToString(), GameTexts.FindText("str_quit_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				ExecuteReset();
				ExecuteDone();
				IsQuitting = false;
			}, delegate
			{
				IsQuitting = false;
			}), pauseGameActiveState: true);
		}
		else
		{
			Deactivate();
		}
	}

	public void ExecuteReset()
	{
		for (int num = TroopsInCart.Count - 1; num >= 0; num--)
		{
			TroopsInCart[num].ExecuteRemoveFromCart();
		}
	}

	public void ExecuteRecruitAll()
	{
		foreach (RecruitVolunteerVM item in VolunteerList.ToList())
		{
			foreach (RecruitVolunteerTroopVM item2 in item.Troops.ToList())
			{
				item2.ExecuteRecruit();
			}
		}
	}

	public void Deactivate()
	{
		ExecuteReset();
		Enabled = false;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		RecruitVolunteerTroopVM.OnFocused = (Action<RecruitVolunteerTroopVM>)Delegate.Remove(RecruitVolunteerTroopVM.OnFocused, new Action<RecruitVolunteerTroopVM>(OnVolunteerTroopFocusChanged));
		RecruitVolunteerOwnerVM.OnFocused = (Action<RecruitVolunteerOwnerVM>)Delegate.Remove(RecruitVolunteerOwnerVM.OnFocused, new Action<RecruitVolunteerOwnerVM>(OnVolunteerOwnerFocusChanged));
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		CancelInputKey.OnFinalize();
		DoneInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
		RecruitAllInputKey.OnFinalize();
	}

	private void OnRemoveFromCart(RecruitVolunteerVM recruitNotable, RecruitVolunteerTroopVM recruitTroop)
	{
		if (TroopsInCart.Any((RecruitVolunteerTroopVM r) => r == recruitTroop))
		{
			recruitNotable.OnRecruitRemovedFromCart(recruitTroop);
			recruitTroop.CanBeRecruited = true;
			recruitTroop.IsInCart = false;
			recruitTroop.IsHiglightEnabled = false;
			TroopsInCart.Remove(recruitTroop);
			RefreshPartyProperties();
		}
	}

	private static bool IsBitSet(int num, int bit)
	{
		return 1 == ((num >> bit) & 1);
	}

	private string GetDoneHint(bool doesPlayerHasEnoughMoney)
	{
		if (!doesPlayerHasEnoughMoney)
		{
			return _playerDoesntHaveEnoughMoneyStr;
		}
		return null;
	}

	private void SetRecruitAllHint()
	{
		RecruitAllHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("HOTKEY", GetRecruitAllKey());
			GameTexts.SetVariable("TEXT", GameTexts.FindText("str_recruit_all"));
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
	}

	private void UpdateRecruitAllProperties()
	{
		int numberOfAvailableRecruits = GetNumberOfAvailableRecruits();
		GameTexts.SetVariable("STR", numberOfAvailableRecruits);
		GameTexts.SetVariable("STR1", _recruitAllTextObject);
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_STR_in_parentheses"));
		RecruitAllText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		CanRecruitAll = numberOfAvailableRecruits > 0;
	}

	private int GetNumberOfAvailableRecruits()
	{
		int num = 0;
		foreach (RecruitVolunteerVM volunteer in VolunteerList)
		{
			foreach (RecruitVolunteerTroopVM troop in volunteer.Troops)
			{
				if (!troop.IsInCart && troop.CanBeRecruited)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void OnVolunteerTroopFocusChanged(RecruitVolunteerTroopVM volunteer)
	{
		FocusedVolunteerTroop = volunteer;
	}

	private void OnVolunteerOwnerFocusChanged(RecruitVolunteerOwnerVM owner)
	{
		FocusedVolunteerOwner = owner;
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (obj.NewNotificationElementID != _latestTutorialElementID)
		{
			if (_latestTutorialElementID != null && _isAvailableTroopsHighlightApplied)
			{
				SetAvailableTroopsHighlightState(state: false);
				_isAvailableTroopsHighlightApplied = false;
			}
			_latestTutorialElementID = obj.NewNotificationElementID;
			if (_latestTutorialElementID != null && !_isAvailableTroopsHighlightApplied && _latestTutorialElementID == "AvailableTroops")
			{
				SetAvailableTroopsHighlightState(state: true);
				_isAvailableTroopsHighlightApplied = true;
			}
		}
	}

	private void SetAvailableTroopsHighlightState(bool state)
	{
		foreach (RecruitVolunteerVM volunteer in VolunteerList)
		{
			foreach (RecruitVolunteerTroopVM troop in volunteer.Troops)
			{
				if (troop.Wage < Hero.MainHero.Gold && troop.PlayerHasEnoughRelation && !troop.IsTroopEmpty)
				{
					troop.IsHiglightEnabled = state;
				}
			}
		}
	}

	public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
	{
		_getKeyTextFromKeyId = getKeyTextFromKeyId;
	}

	private string GetRecruitAllKey()
	{
		if (RecruitAllInputKey == null || _getKeyTextFromKeyId == null)
		{
			return string.Empty;
		}
		return _getKeyTextFromKeyId(RecruitAllInputKey.KeyID).ToString();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetRecruitAllInputKey(HotKey hotKey)
	{
		RecruitAllInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		SetRecruitAllHint();
	}

	public void SetResetInputKey(HotKey hotKey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
