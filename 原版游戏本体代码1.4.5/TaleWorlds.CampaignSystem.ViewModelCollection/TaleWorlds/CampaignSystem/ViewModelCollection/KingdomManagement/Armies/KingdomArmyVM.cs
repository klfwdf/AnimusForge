using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;

public class KingdomArmyVM : KingdomCategoryVM
{
	private readonly Action _onManageArmy;

	private readonly Action _refreshDecision;

	private readonly Action<Army> _showArmyOnMap;

	private readonly IViewDataTracker _viewDataTracker;

	private Kingdom _kingdom;

	private MBBindingList<KingdomArmyItemVM> _armies;

	private KingdomArmyItemVM _currentSelectedArmy;

	private HintViewModel _disbandHint;

	private string _categoryLeaderName;

	private string _categoryLordCount;

	private string _categoryStrength;

	private string _categoryObjective;

	private string _categoryParties;

	private string _createArmyText;

	private string _disbandText;

	private string _manageText;

	private string _changeLeaderText;

	private string _showOnMapText;

	private string _disbandActionExplanationText;

	private string _manageActionExplanationText;

	private bool _canCreateArmy;

	private bool _playerHasArmy;

	private HintViewModel _createArmyHint;

	private HintViewModel _manageArmyHint;

	private bool _canChangeLeaderOfCurrentArmy;

	private bool _canDisbandCurrentArmy;

	private bool _canShowLocationOfCurrentArmy;

	private bool _canManageCurrentArmy;

	private int _disbandCost;

	private int _changeLeaderCost;

	private KingdomArmySortControllerVM _armySortController;

	[DataSourceProperty]
	public KingdomArmySortControllerVM ArmySortController
	{
		get
		{
			return _armySortController;
		}
		set
		{
			if (value != _armySortController)
			{
				_armySortController = value;
				OnPropertyChangedWithValue(value, "ArmySortController");
			}
		}
	}

	[DataSourceProperty]
	public string CreateArmyText
	{
		get
		{
			return _createArmyText;
		}
		set
		{
			if (value != _createArmyText)
			{
				_createArmyText = value;
				OnPropertyChangedWithValue(value, "CreateArmyText");
			}
		}
	}

	[DataSourceProperty]
	public string DisbandActionExplanationText
	{
		get
		{
			return _disbandActionExplanationText;
		}
		set
		{
			if (value != _disbandActionExplanationText)
			{
				_disbandActionExplanationText = value;
				OnPropertyChangedWithValue(value, "DisbandActionExplanationText");
			}
		}
	}

	[DataSourceProperty]
	public string ManageActionExplanationText
	{
		get
		{
			return _manageActionExplanationText;
		}
		set
		{
			if (value != _manageActionExplanationText)
			{
				_manageActionExplanationText = value;
				OnPropertyChangedWithValue(value, "ManageActionExplanationText");
			}
		}
	}

	[DataSourceProperty]
	public KingdomArmyItemVM CurrentSelectedArmy
	{
		get
		{
			return _currentSelectedArmy;
		}
		set
		{
			if (value != _currentSelectedArmy)
			{
				_currentSelectedArmy = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedArmy");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CreateArmyHint
	{
		get
		{
			return _createArmyHint;
		}
		set
		{
			if (value != _createArmyHint)
			{
				_createArmyHint = value;
				OnPropertyChangedWithValue(value, "CreateArmyHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ManageArmyHint
	{
		get
		{
			return _manageArmyHint;
		}
		set
		{
			if (value != _manageArmyHint)
			{
				_manageArmyHint = value;
				OnPropertyChangedWithValue(value, "ManageArmyHint");
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
	public string LeaderText
	{
		get
		{
			return _categoryLeaderName;
		}
		set
		{
			if (value != _categoryLeaderName)
			{
				_categoryLeaderName = value;
				OnPropertyChanged("CategoryLeaderName");
			}
		}
	}

	[DataSourceProperty]
	public string ShowOnMapText
	{
		get
		{
			return _showOnMapText;
		}
		set
		{
			if (value != _showOnMapText)
			{
				_showOnMapText = value;
				OnPropertyChangedWithValue(value, "ShowOnMapText");
			}
		}
	}

	[DataSourceProperty]
	public string ArmyNameText
	{
		get
		{
			return _categoryLordCount;
		}
		set
		{
			if (value != _categoryLordCount)
			{
				_categoryLordCount = value;
				OnPropertyChanged("CategoryLordCount");
			}
		}
	}

	[DataSourceProperty]
	public string StrengthText
	{
		get
		{
			return _categoryStrength;
		}
		set
		{
			if (value != _categoryStrength)
			{
				_categoryStrength = value;
				OnPropertyChanged("CategoryStrength");
			}
		}
	}

	[DataSourceProperty]
	public string PartiesText
	{
		get
		{
			return _categoryParties;
		}
		set
		{
			if (value != _categoryParties)
			{
				_categoryParties = value;
				OnPropertyChangedWithValue(value, "PartiesText");
			}
		}
	}

	[DataSourceProperty]
	public string LocationText
	{
		get
		{
			return _categoryObjective;
		}
		set
		{
			if (value != _categoryObjective)
			{
				_categoryObjective = value;
				OnPropertyChanged("CategoryObjective");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomArmyItemVM> Armies
	{
		get
		{
			return _armies;
		}
		set
		{
			if (value != _armies)
			{
				_armies = value;
				OnPropertyChangedWithValue(value, "Armies");
			}
		}
	}

	[DataSourceProperty]
	public bool CanDisbandCurrentArmy
	{
		get
		{
			return _canDisbandCurrentArmy;
		}
		set
		{
			if (value != _canDisbandCurrentArmy)
			{
				_canDisbandCurrentArmy = value;
				OnPropertyChangedWithValue(value, "CanDisbandCurrentArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanManageCurrentArmy
	{
		get
		{
			return _canManageCurrentArmy;
		}
		set
		{
			if (value != _canManageCurrentArmy)
			{
				_canManageCurrentArmy = value;
				OnPropertyChangedWithValue(value, "CanManageCurrentArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanChangeLeaderOfCurrentArmy
	{
		get
		{
			return _canChangeLeaderOfCurrentArmy;
		}
		set
		{
			if (value != _canChangeLeaderOfCurrentArmy)
			{
				_canChangeLeaderOfCurrentArmy = value;
				OnPropertyChangedWithValue(value, "CanChangeLeaderOfCurrentArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanShowLocationOfCurrentArmy
	{
		get
		{
			return _canShowLocationOfCurrentArmy;
		}
		set
		{
			if (value != _canShowLocationOfCurrentArmy)
			{
				_canShowLocationOfCurrentArmy = value;
				OnPropertyChangedWithValue(value, "CanShowLocationOfCurrentArmy");
			}
		}
	}

	[DataSourceProperty]
	public string DisbandText
	{
		get
		{
			return _disbandText;
		}
		set
		{
			if (value != _disbandText)
			{
				_disbandText = value;
				OnPropertyChangedWithValue(value, "DisbandText");
			}
		}
	}

	[DataSourceProperty]
	public string ManageText
	{
		get
		{
			return _manageText;
		}
		set
		{
			if (value != _manageText)
			{
				_manageText = value;
				OnPropertyChangedWithValue(value, "ManageText");
			}
		}
	}

	[DataSourceProperty]
	public int DisbandCost
	{
		get
		{
			return _disbandCost;
		}
		set
		{
			if (value != _disbandCost)
			{
				_disbandCost = value;
				OnPropertyChangedWithValue(value, "DisbandCost");
			}
		}
	}

	[DataSourceProperty]
	public string ChangeLeaderText
	{
		get
		{
			return _changeLeaderText;
		}
		set
		{
			if (value != _changeLeaderText)
			{
				_changeLeaderText = value;
				OnPropertyChangedWithValue(value, "ChangeLeaderText");
			}
		}
	}

	[DataSourceProperty]
	public int ChangeLeaderCost
	{
		get
		{
			return _changeLeaderCost;
		}
		set
		{
			if (value != _changeLeaderCost)
			{
				_changeLeaderCost = value;
				OnPropertyChangedWithValue(value, "ChangeLeaderCost");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisbandHint
	{
		get
		{
			return _disbandHint;
		}
		set
		{
			if (value != _disbandHint)
			{
				_disbandHint = value;
				OnPropertyChangedWithValue(value, "DisbandHint");
			}
		}
	}

	public KingdomArmyVM(Action onManageArmy, Action refreshDecision, Action<Army> showArmyOnMap)
	{
		_onManageArmy = onManageArmy;
		_refreshDecision = refreshDecision;
		_showArmyOnMap = showArmyOnMap;
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		_armies = new MBBindingList<KingdomArmyItemVM>();
		PlayerHasArmy = MobileParty.MainParty.Army != null;
		ChangeLeaderCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfChangingLeaderOfArmy();
		DisbandCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfDisbandingArmy();
		CreateArmyHint = new HintViewModel();
		DisbandHint = new HintViewModel();
		ManageArmyHint = new HintViewModel();
		base.IsAcceptableItemSelected = false;
		RefreshArmyList();
		ArmySortController = new KingdomArmySortControllerVM(ref _armies);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ArmyNameText = GameTexts.FindText("str_sort_by_army_name_label").ToString();
		LeaderText = GameTexts.FindText("str_sort_by_leader_name_label").ToString();
		StrengthText = GameTexts.FindText("str_men").ToString();
		LocationText = GameTexts.FindText("str_tooltip_label_location").ToString();
		base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_army_selected").ToString();
		DisbandActionExplanationText = GameTexts.FindText("str_kingdom_disband_army_explanation").ToString();
		ManageActionExplanationText = GameTexts.FindText("str_kingdom_manage_army_explanation").ToString();
		ManageText = GameTexts.FindText("str_manage").ToString();
		CreateArmyText = (PlayerHasArmy ? new TextObject("{=DAmdTxuC}Army Manage").ToString() : new TextObject("{=lc9s4rLZ}Create Army").ToString());
		base.CategoryNameText = new TextObject("{=j12VrGKz}Army").ToString();
		ChangeLeaderText = new TextObject("{=NcYbdiyT}Change Leader").ToString();
		PartiesText = new TextObject("{=t3tq0eoW}Parties").ToString();
		DisbandText = new TextObject("{=xXSFaGW8}Disband").ToString();
		ShowOnMapText = GameTexts.FindText("str_show_on_map").ToString();
		CreateArmyText = new TextObject("{=lc9s4rLZ}Create Army").ToString();
		Armies.ApplyActionOnAllItems(delegate(KingdomArmyItemVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedArmy?.RefreshValues();
	}

	public void RefreshArmyList()
	{
		base.NotificationCount = _viewDataTracker.NumOfKingdomArmyNotifications;
		_kingdom = Hero.MainHero.MapFaction as Kingdom;
		if (_kingdom != null)
		{
			Armies.Clear();
			foreach (Army army in _kingdom.Armies)
			{
				Armies.Add(new KingdomArmyItemVM(army, OnSelection));
			}
		}
		else
		{
			Debug.FailedAssert("Kingdom screen can't open if you're not in kingdom", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\KingdomManagement\\Armies\\KingdomArmyVM.cs", "RefreshArmyList", 81);
		}
		RefreshCanManageArmy();
		if (Armies.Count == 0 && CurrentSelectedArmy != null)
		{
			OnSelection(null);
		}
		else if (Armies.Count > 0)
		{
			OnSelection(Armies[0]);
			CurrentSelectedArmy.IsSelected = true;
		}
	}

	private void ExecuteManageArmy()
	{
		_onManageArmy();
	}

	private void ExecuteShowOnMap()
	{
		if (CurrentSelectedArmy != null)
		{
			_showArmyOnMap(CurrentSelectedArmy.Army);
		}
	}

	private void RefreshCurrentArmyVisuals(KingdomArmyItemVM item)
	{
		if (item != null)
		{
			if (CurrentSelectedArmy != null)
			{
				CurrentSelectedArmy.IsSelected = false;
			}
			CanManageCurrentArmy = false;
			CurrentSelectedArmy = item;
			base.NotificationCount = _viewDataTracker.NumOfKingdomArmyNotifications;
			DisbandCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfDisbandingArmy();
			ChangeLeaderCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfChangingLeaderOfArmy();
			CanDisbandCurrentArmy = GetCanDisbandCurrentArmyWithReason(item, DisbandCost, out var disabledReason);
			DisbandHint.HintText = disabledReason;
			DisbandActionExplanationText = GameTexts.FindText("str_kingdom_disband_army_explanation").ToString();
			if (CurrentSelectedArmy != null)
			{
				CanShowLocationOfCurrentArmy = CurrentSelectedArmy.Army.AiBehaviorObject is Settlement || CurrentSelectedArmy.Army.AiBehaviorObject is MobileParty;
				CanManageCurrentArmy = GetCanManageCurrentArmyWithReason(out var disabledReason2);
				ManageArmyHint.HintText = disabledReason2;
			}
		}
	}

	private bool GetCanManageCurrentArmyWithReason(out TextObject disabledReason)
	{
		KingdomArmyItemVM currentSelectedArmy = CurrentSelectedArmy;
		if (currentSelectedArmy == null || !currentSelectedArmy.IsMainArmy)
		{
			disabledReason = TextObject.GetEmpty();
			return false;
		}
		return CampaignUIHelper.GetCanManageCurrentArmyWithReason(out disabledReason);
	}

	private bool GetCanDisbandCurrentArmyWithReason(KingdomArmyItemVM armyItem, int disbandCost, out TextObject disabledReason)
	{
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			disabledReason = GameTexts.FindText("str_cannot_disband_army_while_mercenary");
			return false;
		}
		if (Clan.PlayerClan.Influence < (float)disbandCost)
		{
			disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence");
			return false;
		}
		if (armyItem.Army.LeaderParty.MapEvent != null)
		{
			disabledReason = GameTexts.FindText("str_cannot_disband_army_while_in_event");
			return false;
		}
		if (armyItem.Army.Parties.Contains(MobileParty.MainParty))
		{
			disabledReason = GameTexts.FindText("str_cannot_disband_army_while_in_that_army");
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

	public void SelectArmy(Army army)
	{
		foreach (KingdomArmyItemVM army2 in Armies)
		{
			if (army2.Army == army)
			{
				OnSelection(army2);
				break;
			}
		}
	}

	private void OnSelection(KingdomArmyItemVM item)
	{
		if (CurrentSelectedArmy != item)
		{
			RefreshCurrentArmyVisuals(item);
			CurrentSelectedArmy = item;
			base.IsAcceptableItemSelected = item != null;
		}
	}

	private void ExecuteDisbandCurrentArmy()
	{
		if (CurrentSelectedArmy != null && Hero.MainHero.Clan.Influence >= (float)DisbandCost)
		{
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_disband_army").ToString(), new TextObject("{=zrhr4rDA}Are you sure you want to disband this army? This will result in relation loss.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), DisbandCurrentArmy, null));
		}
	}

	private void DisbandCurrentArmy()
	{
		if (CurrentSelectedArmy != null && Hero.MainHero.Clan.Influence >= (float)DisbandCost)
		{
			DisbandArmyAction.ApplyByReleasedByPlayerAfterBattle(CurrentSelectedArmy.Army);
			RefreshArmyList();
		}
	}

	private void RefreshCanManageArmy()
	{
		PlayerHasArmy = MobileParty.MainParty.Army != null;
		CanCreateArmy = Campaign.Current.Models.ArmyManagementCalculationModel.CanPlayerCreateArmy(out var disabledReason);
		CreateArmyHint.HintText = disabledReason;
	}
}
