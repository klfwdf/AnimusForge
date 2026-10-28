using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;

public class ArmyManagementVM : ViewModel
{
	public class ManagementItemComparer : IComparer<ArmyManagementItemVM>
	{
		public int Compare(ArmyManagementItemVM x, ArmyManagementItemVM y)
		{
			if (x.IsMainHero)
			{
				return -1;
			}
			return y.IsAlreadyWithPlayer.CompareTo(x.IsAlreadyWithPlayer);
		}
	}

	private readonly Action _onClose;

	private readonly ArmyManagementItemVM _mainPartyItem;

	private readonly ManagementItemComparer _itemComparer;

	private readonly float _initialInfluence;

	private string _latestTutorialElementID;

	private string _playerDoesntHaveEnoughInfluenceStr;

	private const int _cohesionBoostAmount = 10;

	private int _influenceSpentForCohesionBoosting;

	private int _boostedCohesion;

	private string _titleText;

	private string _boostTitleText;

	private string _cancelText;

	private string _doneText;

	private bool _canCreateArmy;

	private bool _canBoostCohesion;

	private List<MobileParty> _currentParties;

	private ArmyManagementItemVM _focusedItem;

	private MBBindingList<ArmyManagementItemVM> _partyList;

	private MBBindingList<ArmyManagementItemVM> _partiesInCart;

	private MBBindingList<ArmyManagementItemVM> _partiesToRemove;

	private ArmyManagementSortControllerVM _sortControllerVM;

	private int _totalStrength;

	private int _totalCost;

	private int _cohesion;

	private int _cohesionBoostCost;

	private string _cohesionText;

	private int _newCohesion;

	private string _totalStrengthText;

	private string _totalCostText;

	private string _totalCostNumbersText;

	private string _totalInfluence;

	private string _totalLords;

	private string _costText;

	private string _strengthText;

	private string _shipCountText;

	private string _lordsText;

	private string _distanceText;

	private string _clanText;

	private string _ownerText;

	private string _nameText;

	private string _disbandArmyText;

	private string _cohesionBoostAmountText;

	private bool _playerHasArmy;

	private bool _canDisbandArmy;

	private bool _canConfirm;

	private bool _canAffordInfluenceCost;

	private string _moraleText;

	private string _foodText;

	private BasicTooltipViewModel _cohesionHint;

	private HintViewModel _moraleHint;

	private HintViewModel _foodHint;

	private HintViewModel _boostCohesionHint;

	private HintViewModel _disbandArmyHint;

	private HintViewModel _doneHint;

	public ElementNotificationVM _tutorialNotification;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _removeInputKey;

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

	[DataSourceProperty]
	public ArmyManagementSortControllerVM SortControllerVM
	{
		get
		{
			return _sortControllerVM;
		}
		set
		{
			if (value != _sortControllerVM)
			{
				_sortControllerVM = value;
				OnPropertyChangedWithValue(value, "SortControllerVM");
			}
		}
	}

	[DataSourceProperty]
	public string BoostTitleText
	{
		get
		{
			return _boostTitleText;
		}
		set
		{
			if (value != _boostTitleText)
			{
				_boostTitleText = value;
				OnPropertyChangedWithValue(value, "BoostTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DisbandArmyText
	{
		get
		{
			return _disbandArmyText;
		}
		set
		{
			if (value != _disbandArmyText)
			{
				_disbandArmyText = value;
				OnPropertyChangedWithValue(value, "DisbandArmyText");
			}
		}
	}

	[DataSourceProperty]
	public string CohesionBoostAmountText
	{
		get
		{
			return _cohesionBoostAmountText;
		}
		set
		{
			if (value != _cohesionBoostAmountText)
			{
				_cohesionBoostAmountText = value;
				OnPropertyChangedWithValue(value, "CohesionBoostAmountText");
			}
		}
	}

	[DataSourceProperty]
	public string DistanceText
	{
		get
		{
			return _distanceText;
		}
		set
		{
			if (value != _distanceText)
			{
				_distanceText = value;
				OnPropertyChangedWithValue(value, "DistanceText");
			}
		}
	}

	[DataSourceProperty]
	public string CostText
	{
		get
		{
			return _costText;
		}
		set
		{
			if (value != _costText)
			{
				_costText = value;
				OnPropertyChangedWithValue(value, "CostText");
			}
		}
	}

	[DataSourceProperty]
	public string OwnerText
	{
		get
		{
			return _ownerText;
		}
		set
		{
			if (value != _ownerText)
			{
				_ownerText = value;
				OnPropertyChangedWithValue(value, "OwnerText");
			}
		}
	}

	[DataSourceProperty]
	public string StrengthText
	{
		get
		{
			return _strengthText;
		}
		set
		{
			if (value != _strengthText)
			{
				_strengthText = value;
				OnPropertyChangedWithValue(value, "StrengthText");
			}
		}
	}

	[DataSourceProperty]
	public string ShipCountText
	{
		get
		{
			return _shipCountText;
		}
		set
		{
			if (value != _shipCountText)
			{
				_shipCountText = value;
				OnPropertyChangedWithValue(value, "ShipCountText");
			}
		}
	}

	[DataSourceProperty]
	public string LordsText
	{
		get
		{
			return _lordsText;
		}
		set
		{
			if (value != _lordsText)
			{
				_lordsText = value;
				OnPropertyChangedWithValue(value, "LordsText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalInfluence
	{
		get
		{
			return _totalInfluence;
		}
		set
		{
			if (value != _totalInfluence)
			{
				_totalInfluence = value;
				OnPropertyChangedWithValue(value, "TotalInfluence");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStrength
	{
		get
		{
			return _totalStrength;
		}
		set
		{
			if (value != _totalStrength)
			{
				_totalStrength = value;
				OnPropertyChangedWithValue(value, "TotalStrength");
			}
		}
	}

	[DataSourceProperty]
	public int TotalCost
	{
		get
		{
			return _totalCost;
		}
		set
		{
			if (value != _totalCost)
			{
				_totalCost = value;
				CanAffordInfluenceCost = TotalCost <= 0 || (float)TotalCost <= Hero.MainHero.Clan.Influence;
				OnPropertyChangedWithValue(value, "TotalCost");
			}
		}
	}

	[DataSourceProperty]
	public string TotalLords
	{
		get
		{
			return _totalLords;
		}
		set
		{
			if (value != _totalLords)
			{
				_totalLords = value;
				OnPropertyChangedWithValue(value, "TotalLords");
			}
		}
	}

	[DataSourceProperty]
	public bool CanCreateArmy
	{
		get
		{
			return _canCreateArmy;
		}
		set
		{
			if (value != _canCreateArmy)
			{
				_canCreateArmy = value;
				OnPropertyChangedWithValue(value, "CanCreateArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBoostCohesion
	{
		get
		{
			return _canBoostCohesion;
		}
		set
		{
			if (value != _canBoostCohesion)
			{
				_canBoostCohesion = value;
				OnPropertyChangedWithValue(value, "CanBoostCohesion");
			}
		}
	}

	[DataSourceProperty]
	public bool CanDisbandArmy
	{
		get
		{
			return _canDisbandArmy;
		}
		set
		{
			if (value != _canDisbandArmy)
			{
				_canDisbandArmy = value;
				OnPropertyChangedWithValue(value, "CanDisbandArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanConfirm
	{
		get
		{
			return _canConfirm;
		}
		set
		{
			if (value != _canConfirm)
			{
				_canConfirm = value;
				OnPropertyChangedWithValue(value, "CanConfirm");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAffordInfluenceCost
	{
		get
		{
			return _canAffordInfluenceCost;
		}
		set
		{
			if (value != _canAffordInfluenceCost)
			{
				_canAffordInfluenceCost = value;
				OnPropertyChangedWithValue(value, "CanAffordInfluenceCost");
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
	public string ClanText
	{
		get
		{
			return _clanText;
		}
		set
		{
			if (value != _clanText)
			{
				_clanText = value;
				OnPropertyChangedWithValue(value, "ClanText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
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
	public ArmyManagementItemVM FocusedItem
	{
		get
		{
			return _focusedItem;
		}
		set
		{
			if (value != _focusedItem)
			{
				_focusedItem = value;
				OnPropertyChangedWithValue(value, "FocusedItem");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ArmyManagementItemVM> PartyList
	{
		get
		{
			return _partyList;
		}
		set
		{
			if (value != _partyList)
			{
				_partyList = value;
				OnPropertyChangedWithValue(value, "PartyList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ArmyManagementItemVM> PartiesInCart
	{
		get
		{
			return _partiesInCart;
		}
		set
		{
			if (value != _partiesInCart)
			{
				_partiesInCart = value;
				OnPropertyChangedWithValue(value, "PartiesInCart");
			}
		}
	}

	[DataSourceProperty]
	public string TotalStrengthText
	{
		get
		{
			return _totalStrengthText;
		}
		set
		{
			if (value != _totalStrengthText)
			{
				_totalStrengthText = value;
				OnPropertyChangedWithValue(value, "TotalStrengthText");
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
	public string TotalCostNumbersText
	{
		get
		{
			return _totalCostNumbersText;
		}
		set
		{
			if (value != _totalCostNumbersText)
			{
				_totalCostNumbersText = value;
				OnPropertyChangedWithValue(value, "TotalCostNumbersText");
			}
		}
	}

	[DataSourceProperty]
	public string CohesionText
	{
		get
		{
			return _cohesionText;
		}
		set
		{
			if (value != _cohesionText)
			{
				_cohesionText = value;
				OnPropertyChangedWithValue(value, "CohesionText");
			}
		}
	}

	[DataSourceProperty]
	public int Cohesion
	{
		get
		{
			return _cohesion;
		}
		set
		{
			if (value != _cohesion)
			{
				_cohesion = value;
				OnPropertyChangedWithValue(value, "Cohesion");
			}
		}
	}

	[DataSourceProperty]
	public int CohesionBoostCost
	{
		get
		{
			return _cohesionBoostCost;
		}
		set
		{
			if (value != _cohesionBoostCost)
			{
				_cohesionBoostCost = value;
				OnPropertyChangedWithValue(value, "CohesionBoostCost");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerHasArmy
	{
		get
		{
			return _playerHasArmy;
		}
		set
		{
			if (value != _playerHasArmy)
			{
				_playerHasArmy = value;
				OnPropertyChangedWithValue(value, "PlayerHasArmy");
			}
		}
	}

	[DataSourceProperty]
	public string MoraleText
	{
		get
		{
			return _moraleText;
		}
		set
		{
			if (value != _moraleText)
			{
				_moraleText = value;
				OnPropertyChangedWithValue(value, "MoraleText");
			}
		}
	}

	[DataSourceProperty]
	public string FoodText
	{
		get
		{
			return _foodText;
		}
		set
		{
			if (value != _foodText)
			{
				_foodText = value;
				OnPropertyChangedWithValue(value, "FoodText");
			}
		}
	}

	[DataSourceProperty]
	public int NewCohesion
	{
		get
		{
			return _newCohesion;
		}
		set
		{
			if (value != _newCohesion)
			{
				_newCohesion = value;
				OnPropertyChangedWithValue(value, "NewCohesion");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CohesionHint
	{
		get
		{
			return _cohesionHint;
		}
		set
		{
			if (value != _cohesionHint)
			{
				_cohesionHint = value;
				OnPropertyChangedWithValue(value, "CohesionHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MoraleHint
	{
		get
		{
			return _moraleHint;
		}
		set
		{
			if (value != _moraleHint)
			{
				_moraleHint = value;
				OnPropertyChangedWithValue(value, "MoraleHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BoostCohesionHint
	{
		get
		{
			return _boostCohesionHint;
		}
		set
		{
			if (value != _boostCohesionHint)
			{
				_boostCohesionHint = value;
				OnPropertyChangedWithValue(value, "BoostCohesionHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisbandArmyHint
	{
		get
		{
			return _disbandArmyHint;
		}
		set
		{
			if (value != _disbandArmyHint)
			{
				_disbandArmyHint = value;
				OnPropertyChangedWithValue(value, "DisbandArmyHint");
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
	public HintViewModel FoodHint
	{
		get
		{
			return _foodHint;
		}
		set
		{
			if (value != _foodHint)
			{
				_foodHint = value;
				OnPropertyChangedWithValue(value, "FoodHint");
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
	public InputKeyItemVM RemoveInputKey
	{
		get
		{
			return _removeInputKey;
		}
		set
		{
			if (value == _removeInputKey)
			{
				return;
			}
			_removeInputKey = value;
			OnPropertyChangedWithValue(value, "RemoveInputKey");
			foreach (ArmyManagementItemVM party in PartyList)
			{
				party.RemoveInputKey = value;
			}
		}
	}

	public ArmyManagementVM(Action onClose)
	{
		_onClose = onClose;
		_itemComparer = new ManagementItemComparer();
		PartyList = new MBBindingList<ArmyManagementItemVM>();
		PartiesInCart = new MBBindingList<ArmyManagementItemVM>();
		_partiesToRemove = new MBBindingList<ArmyManagementItemVM>();
		_currentParties = new List<MobileParty>();
		CohesionHint = new BasicTooltipViewModel();
		FoodHint = new HintViewModel();
		MoraleHint = new HintViewModel();
		BoostCohesionHint = new HintViewModel();
		DisbandArmyHint = new HintViewModel();
		DoneHint = new HintViewModel();
		TutorialNotification = new ElementNotificationVM();
		CanConfirm = false;
		CanAffordInfluenceCost = true;
		PlayerHasArmy = MobileParty.MainParty.Army != null;
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.LeaderHero != null && item.MapFaction == Hero.MainHero.MapFaction && item.LeaderHero != Hero.MainHero && !item.IsCaravan)
			{
				PartyList.Add(new ArmyManagementItemVM(OnAddToCart, OnRemove, OnFocus, item));
			}
		}
		_mainPartyItem = new ArmyManagementItemVM(null, null, null, Hero.MainHero.PartyBelongedTo)
		{
			IsAlreadyWithPlayer = true,
			IsMainHero = true,
			IsInCart = true
		};
		PartiesInCart.Add(_mainPartyItem);
		foreach (ArmyManagementItemVM party in PartyList)
		{
			if (MobileParty.MainParty.Army != null && party.Party.Army == MobileParty.MainParty.Army && party.Party != MobileParty.MainParty)
			{
				party.Cost = 0;
				party.IsAlreadyWithPlayer = true;
				party.IsInCart = true;
				PartiesInCart.Add(party);
			}
		}
		if (MobileParty.MainParty.Army != null)
		{
			CohesionBoostCost = Campaign.Current.Models.ArmyManagementCalculationModel.GetCohesionBoostInfluenceCost(MobileParty.MainParty.Army, 10);
		}
		_initialInfluence = Hero.MainHero.Clan.Influence;
		OnRefresh();
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.ArmyManagement));
		SortControllerVM = new ArmyManagementSortControllerVM(_partyList);
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = GameTexts.FindText("str_army_management").ToString();
		BoostTitleText = GameTexts.FindText("str_boost_cohesion").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		DistanceText = GameTexts.FindText("str_distance").ToString();
		CostText = GameTexts.FindText("str_cost").ToString();
		StrengthText = GameTexts.FindText("str_men").ToString();
		LordsText = GameTexts.FindText("str_leader").ToString();
		ClanText = GameTexts.FindText("str_clan").ToString();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		OwnerText = GameTexts.FindText("str_party").ToString();
		DisbandArmyText = GameTexts.FindText("str_disband_army").ToString();
		ShipCountText = new TextObject("{=7Q8ufo5X}Ships").ToString();
		_playerDoesntHaveEnoughInfluenceStr = GameTexts.FindText("str_warning_you_dont_have_enough_influence").ToString();
		GameTexts.SetVariable("TOTAL_INFLUENCE", TaleWorlds.Library.MathF.Round(Hero.MainHero.Clan.Influence));
		TotalInfluence = GameTexts.FindText("str_total_influence").ToString();
		GameTexts.SetVariable("NUMBER", 10);
		CohesionBoostAmountText = GameTexts.FindText("str_plus_with_number").ToString();
		PartyList.ApplyActionOnAllItems(delegate(ArmyManagementItemVM x)
		{
			x.RefreshValues();
		});
		PartiesInCart.ApplyActionOnAllItems(delegate(ArmyManagementItemVM x)
		{
			x.RefreshValues();
		});
		TutorialNotification.RefreshValues();
	}

	private void CalculateCohesion()
	{
		if (MobileParty.MainParty.Army == null)
		{
			return;
		}
		Cohesion = (int)MobileParty.MainParty.Army.Cohesion;
		NewCohesion = TaleWorlds.Library.MathF.Min(Cohesion + _boostedCohesion, 100);
		ArmyManagementCalculationModel armyManagementCalculationModel = Campaign.Current.Models.ArmyManagementCalculationModel;
		_currentParties.Clear();
		foreach (ArmyManagementItemVM item in PartiesInCart)
		{
			if (!item.Party.IsMainParty)
			{
				_currentParties.Add(item.Party);
				if (!item.IsAlreadyWithPlayer)
				{
					NewCohesion = armyManagementCalculationModel.CalculateNewCohesion(MobileParty.MainParty.Army, item.Party.Party, NewCohesion, 1);
				}
			}
		}
	}

	private void OnFocus(ArmyManagementItemVM focusedItem)
	{
		FocusedItem = focusedItem;
	}

	private void OnAddToCart(ArmyManagementItemVM armyItem)
	{
		if (!PartiesInCart.Contains(armyItem))
		{
			PartiesInCart.Add(armyItem);
			armyItem.IsInCart = true;
			Game.Current.EventManager.TriggerEvent(new PartyAddedToArmyByPlayerEvent(armyItem.Party));
			if (_partiesToRemove.Contains(armyItem))
			{
				_partiesToRemove.Remove(armyItem);
			}
			if (armyItem.IsAlreadyWithPlayer)
			{
				armyItem.CanJoinBackWithoutCost = false;
			}
			TotalCost += armyItem.Cost;
		}
		OnRefresh();
	}

	private void OnRemove(ArmyManagementItemVM armyItem)
	{
		if (PartiesInCart.Contains(armyItem))
		{
			PartiesInCart.Remove(armyItem);
			armyItem.IsInCart = false;
			_partiesToRemove.Add(armyItem);
			if (armyItem.IsAlreadyWithPlayer)
			{
				armyItem.CanJoinBackWithoutCost = true;
			}
			TotalCost -= armyItem.Cost;
		}
		OnRefresh();
	}

	private void UpdateCanConfirm()
	{
		if (!CanAffordInfluenceCost)
		{
			CanConfirm = false;
			DoneHint.HintText = GameTexts.FindText("str_warning_you_dont_have_enough_influence").CopyTextObject();
		}
		else if (PartiesInCart.Count == 1 && PartiesInCart[0].IsMainHero)
		{
			CanConfirm = CanDisbandArmy;
			if (!CanConfirm)
			{
				DoneHint.HintText = new TextObject("{=aUq1M6Wa}You need more than 1 party to create an army");
			}
		}
		else
		{
			CanConfirm = true;
		}
		if (CanConfirm)
		{
			DoneHint.HintText = TextObject.GetEmpty();
		}
	}

	private void ApplyCohesionChange()
	{
		if (MobileParty.MainParty.Army != null)
		{
			int num = NewCohesion - Cohesion;
			MobileParty.MainParty.Army.BoostCohesionWithInfluence(num, _influenceSpentForCohesionBoosting);
		}
	}

	private void OnBoostCohesion()
	{
		if (CanBoostCohesion)
		{
			TotalCost += CohesionBoostCost;
			_boostedCohesion += 10;
			_influenceSpentForCohesionBoosting += CohesionBoostCost;
			OnRefresh();
		}
	}

	private void OnRefresh()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		float num4 = 0f;
		foreach (ArmyManagementItemVM item in PartiesInCart)
		{
			num2++;
			num += (int)item.Party.Party.EstimatedStrength;
			if (item.IsAlreadyWithPlayer)
			{
				num4 += item.Party.Food;
				num3 += (int)item.Party.Morale;
			}
		}
		TotalStrength = num;
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_total_cost").ToString());
		TotalCostText = GameTexts.FindText("str_LEFT_colon").ToString();
		GameTexts.SetVariable("LEFT", TotalCost.ToString());
		GameTexts.SetVariable("RIGHT", ((int)Hero.MainHero.Clan.Influence).ToString());
		TotalCostNumbersText = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		GameTexts.SetVariable("NUM", num2);
		TotalLords = GameTexts.FindText("str_NUM_lords").ToString();
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_strength").ToString());
		TotalStrengthText = GameTexts.FindText("str_LEFT_colon").ToString();
		CanCreateArmy = (float)TotalCost <= Hero.MainHero.Clan.Influence && num2 > 1;
		PlayerHasArmy = MobileParty.MainParty.Army != null && (_partiesToRemove.Count <= 0 || PartiesInCart.Count((ArmyManagementItemVM p) => p.IsAlreadyWithPlayer) >= 1);
		CalculateCohesion();
		CanBoostCohesion = PlayerHasArmy && NewCohesion + 10 <= 100;
		if (CanBoostCohesion)
		{
			TextObject textObject = new TextObject("{=nNZ1ZtTE}Add {BOOSTAMOUNT} cohesion to your army");
			textObject.SetTextVariable("BOOSTAMOUNT", 10);
			BoostCohesionHint.HintText = textObject;
		}
		else if (NewCohesion + 10 > 100)
		{
			TextObject textObject2 = new TextObject("{=rsHPaaYZ}Cohesion needs to be lower than {MINAMOUNT} to boost");
			textObject2.SetTextVariable("MINAMOUNT", 90);
			BoostCohesionHint.HintText = textObject2;
		}
		else
		{
			BoostCohesionHint.HintText = new TextObject("{=Ioiqzz4E}You need to be in an army to boost cohesion");
		}
		if (MobileParty.MainParty.Army != null)
		{
			CohesionText = GameTexts.FindText("str_cohesion").ToString();
			num3 += (int)MobileParty.MainParty.Morale;
			num4 += MobileParty.MainParty.Food;
		}
		MoraleText = num3.ToString();
		FoodText = TaleWorlds.Library.MathF.Round(num4, 1).ToString();
		PartiesInCart.Sort(_itemComparer);
		CanDisbandArmy = GetCanDisbandArmyWithReason(out var disabledReason);
		DisbandArmyHint.HintText = disabledReason;
		UpdateCanConfirm();
		UpdateTooltips();
	}

	private bool GetCanDisbandArmyWithReason(out TextObject disabledReason)
	{
		if (MobileParty.MainParty.Army == null)
		{
			disabledReason = new TextObject("{=iSZTOeYH}No army to disband.");
			return false;
		}
		if (MobileParty.MainParty.MapEvent != null)
		{
			disabledReason = new TextObject("{=uipNpzVw}Cannot disband the army right now.");
			return false;
		}
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_siege");
			return false;
		}
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private void UpdateTooltips()
	{
		if (PlayerHasArmy)
		{
			CohesionHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyCohesionTooltip(PartyBase.MainParty.MobileParty.Army));
			PartyBase.MainParty.MobileParty.Army.RecalculateArmyMorale();
			TaleWorlds.Library.MathF.Round(PartyBase.MainParty.MobileParty.Army.Morale, 1).ToString("0.0");
			MBTextManager.SetTextVariable("BASE_EFFECT", TaleWorlds.Library.MathF.Round(MobileParty.MainParty.Morale, 1).ToString("0.0"));
			MBTextManager.SetTextVariable("STR1", "");
			MBTextManager.SetTextVariable("STR2", "");
			MBTextManager.SetTextVariable("ARMY_MORALE", MobileParty.MainParty.Army.Morale);
			foreach (MobileParty party in MobileParty.MainParty.Army.Parties)
			{
				MBTextManager.SetTextVariable("STR1", GameTexts.FindText("str_STR1_STR2").ToString());
				MBTextManager.SetTextVariable("PARTY_NAME", party.Name);
				MBTextManager.SetTextVariable("PARTY_MORALE", (int)party.Morale);
				MBTextManager.SetTextVariable("STR2", GameTexts.FindText("str_new_morale_item_line"));
			}
			MBTextManager.SetTextVariable("ARMY_MORALE_ITEMS", GameTexts.FindText("str_STR1_STR2").ToString());
			MoraleHint.HintText = GameTexts.FindText("str_army_morale_tooltip");
		}
		else
		{
			GameTexts.SetVariable("reg1", (int)MobileParty.MainParty.Morale);
			MoraleHint.HintText = GameTexts.FindText("str_morale_reg1");
		}
		MBTextManager.SetTextVariable("newline", "\n");
		MBTextManager.SetTextVariable("DAILY_FOOD_CONSUMPTION", MobileParty.MainParty.FoodChange);
		FoodHint.HintText = GameTexts.FindText("str_food_consumption_tooltip");
	}

	public void ExecuteDone()
	{
		if (!CanAffordInfluenceCost)
		{
			return;
		}
		if (PartiesInCart.Count == 1 && PartiesInCart[0].IsMainHero)
		{
			ExecuteDisbandArmy();
			return;
		}
		if (NewCohesion > Cohesion)
		{
			ApplyCohesionChange();
		}
		if (PartiesInCart.Count > 1 && MobileParty.MainParty.MapFaction.IsKingdomFaction)
		{
			if (MobileParty.MainParty.Army == null)
			{
				((Kingdom)MobileParty.MainParty.MapFaction).CreateArmy(Hero.MainHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Defender);
			}
			foreach (ArmyManagementItemVM item in PartiesInCart)
			{
				if (item.Party != MobileParty.MainParty)
				{
					item.Party.Army = MobileParty.MainParty.Army;
				}
			}
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -(TotalCost - _influenceSpentForCohesionBoosting));
		}
		if (_partiesToRemove.Count > 0)
		{
			bool flag = false;
			foreach (ArmyManagementItemVM item2 in _partiesToRemove)
			{
				if (item2.Party == MobileParty.MainParty)
				{
					item2.Party.Army = null;
					flag = true;
				}
			}
			if (!flag)
			{
				foreach (ArmyManagementItemVM item3 in _partiesToRemove)
				{
					Army army = MobileParty.MainParty.Army;
					if (army != null && army.Parties.Contains(item3.Party))
					{
						item3.Party.Army = null;
					}
				}
			}
			_partiesToRemove.Clear();
		}
		_onClose();
		CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
	}

	public void ExecuteCancel()
	{
		ChangeClanInfluenceAction.Apply(Clan.PlayerClan, _initialInfluence - Clan.PlayerClan.Influence);
		_onClose();
	}

	public void ExecuteReset()
	{
		foreach (ArmyManagementItemVM item in PartiesInCart.ToList())
		{
			OnRemove(item);
			item.UpdateEligibility();
		}
		PartiesInCart.Add(_mainPartyItem);
		foreach (ArmyManagementItemVM party in PartyList)
		{
			if (party.IsAlreadyWithPlayer)
			{
				PartiesInCart.Add(party);
				party.IsInCart = true;
				party.CanJoinBackWithoutCost = false;
			}
		}
		NewCohesion = Cohesion;
		ChangeClanInfluenceAction.Apply(Clan.PlayerClan, _initialInfluence - Clan.PlayerClan.Influence);
		TotalCost = 0;
		_boostedCohesion = 0;
		_influenceSpentForCohesionBoosting = 0;
		_partiesToRemove.Clear();
		OnRefresh();
	}

	public void ExecuteDisbandArmy()
	{
		if (CanDisbandArmy)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=ViYdZUbQ}Disband Army").ToString(), new TextObject("{=kqeA8rjL}Are you sure you want to disband your army?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				DisbandArmy();
			}, null));
		}
	}

	public void ExecuteBoostCohesionManual()
	{
		OnBoostCohesion();
		Game.Current.EventManager.TriggerEvent(new ArmyCohesionBoostedByPlayerEvent());
	}

	private void DisbandArmy()
	{
		foreach (ArmyManagementItemVM item in PartiesInCart.ToList())
		{
			OnRemove(item);
		}
		ExecuteDone();
	}

	private void OnCloseBoost()
	{
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.ArmyManagement));
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (obj.NewNotificationElementID != _latestTutorialElementID)
		{
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = string.Empty;
			}
			_latestTutorialElementID = obj.NewNotificationElementID;
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = _latestTutorialElementID;
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		ResetInputKey?.OnFinalize();
		RemoveInputKey?.OnFinalize();
	}

	public void SetResetInputKey(HotKey hotKey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetRemoveInputKey(HotKey hotKey)
	{
		RemoveInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
