using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace NavalDLC.ViewModelCollection.OrderOfBattle;

public class NavalOrderOfBattleFormationItemVM : ViewModel
{
	public readonly Formation Formation;

	private readonly Action<NavalOrderOfBattleFormationItemVM> _onSelected;

	private readonly Action<NavalOrderOfBattleFormationItemVM> _onClassChanged;

	private readonly Action<NavalOrderOfBattleFormationItemVM> _onFilterToggled;

	public static Action<NavalOrderOfBattleFormationItemVM> OnAcceptCaptain;

	public static Action<NavalOrderOfBattleFormationItemVM> OnAcceptShip;

	public static Func<DeploymentFormationClass, FormationFilterType, int> GetTotalTroopCountWithFilter;

	private readonly TextObject _captainSlotHintText = new TextObject("{=shipcaptain}Captain");

	private readonly TextObject _shipSlotHintText = new TextObject("{=1nbU1tV5}Ship");

	private readonly TextObject _assignCaptainHintText = new TextObject("{=rHEi6aVz}Assign as Captain");

	private readonly TextObject _assignShipHintText = new TextObject("{=6o2JKNbt}Assign as Ship");

	private readonly TextObject _infantryHintText = new TextObject("{=IxI1HecC}Give preference to infantry troops");

	private readonly TextObject _rangedHintText = new TextObject("{=I9X4VvhG}Give preference to ranged troops");

	private readonly TextObject _infantryAndRangedHintText = new TextObject("{=e9nO59x4}Give equal preference to infantry and ranged troops");

	private readonly TextObject _filteredTroopCountInfoText = new TextObject("{=yRIPADWl}{TROOP_COUNT}/{TOTAL_TROOP_COUNT}");

	private bool _isSelected;

	private bool _isEnabled;

	private bool _isSelectable;

	private bool _hasCaptain;

	private bool _hasShip;

	private bool _isAcceptingCaptain;

	private bool _isAcceptingShip;

	private bool _isInfantrySelected;

	private bool _isRangedSelected;

	private bool _isInfantryAndRangedSelected;

	private string _formationName;

	private string _formationIsEmptyText;

	private int _troopCount;

	private int _formationClassInt;

	private bool _isSkeletalCrewCountWarningActive;

	private string _skeletalCrewCountWarning;

	private bool _isMinimumCrewCountWarningActive;

	private string _minimumCrewCountWarning;

	private HintViewModel _captainSlotHint;

	private HintViewModel _shipSlotHint;

	private HintViewModel _assignCaptainHint;

	private HintViewModel _assignShipHint;

	private NavalOrderOfBattleHeroItemVM _captain;

	private NavalOrderOfBattleShipItemVM _ship;

	private int _wSign;

	private Vec2 _screenPosition;

	private MBBindingList<OrderOfBattleFormationFilterSelectorItemVM> _filterItems;

	private HintViewModel _infantryHint;

	private HintViewModel _rangedHint;

	private HintViewModel _infantryAndRangedHint;

	private HintViewModel _disabledHint;

	private BasicTooltipViewModel _tooltip;

	public DeploymentFormationClass SelectedClass { get; private set; }

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				IsSelectable = HasShip && IsEnabled;
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChangedWithValue(value, "IsSelectable");
				FilterItems.ApplyActionOnAllItems(delegate(OrderOfBattleFormationFilterSelectorItemVM x)
				{
					x.IsEnabled = IsSelectable;
				});
			}
		}
	}

	[DataSourceProperty]
	public bool HasCaptain
	{
		get
		{
			return _hasCaptain;
		}
		set
		{
			if (value != _hasCaptain)
			{
				_hasCaptain = value;
				OnPropertyChangedWithValue(value, "HasCaptain");
			}
		}
	}

	[DataSourceProperty]
	public bool HasShip
	{
		get
		{
			return _hasShip;
		}
		set
		{
			if (value != _hasShip)
			{
				_hasShip = value;
				OnPropertyChangedWithValue(value, "HasShip");
				IsSelectable = HasShip && IsEnabled;
				UpdateIsWarned();
			}
		}
	}

	[DataSourceProperty]
	public bool IsAcceptingCaptain
	{
		get
		{
			return _isAcceptingCaptain;
		}
		set
		{
			if (value != _isAcceptingCaptain)
			{
				_isAcceptingCaptain = value;
				OnPropertyChangedWithValue(value, "IsAcceptingCaptain");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAcceptingShip
	{
		get
		{
			return _isAcceptingShip;
		}
		set
		{
			if (value != _isAcceptingShip)
			{
				_isAcceptingShip = value;
				OnPropertyChangedWithValue(value, "IsAcceptingShip");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInfantrySelected
	{
		get
		{
			return _isInfantrySelected;
		}
		set
		{
			if (value != _isInfantrySelected)
			{
				_isInfantrySelected = value;
				OnPropertyChangedWithValue(value, "IsInfantrySelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRangedSelected
	{
		get
		{
			return _isRangedSelected;
		}
		set
		{
			if (value != _isRangedSelected)
			{
				_isRangedSelected = value;
				OnPropertyChangedWithValue(value, "IsRangedSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInfantryAndRangedSelected
	{
		get
		{
			return _isInfantryAndRangedSelected;
		}
		set
		{
			if (value != _isInfantryAndRangedSelected)
			{
				_isInfantryAndRangedSelected = value;
				OnPropertyChangedWithValue(value, "IsInfantryAndRangedSelected");
			}
		}
	}

	[DataSourceProperty]
	public string FormationName
	{
		get
		{
			return _formationName;
		}
		set
		{
			if (value != _formationName)
			{
				_formationName = value;
				OnPropertyChangedWithValue(value, "FormationName");
			}
		}
	}

	[DataSourceProperty]
	public string FormationIsEmptyText
	{
		get
		{
			return _formationIsEmptyText;
		}
		set
		{
			if (value != _formationIsEmptyText)
			{
				_formationIsEmptyText = value;
				OnPropertyChangedWithValue(value, "FormationIsEmptyText");
			}
		}
	}

	[DataSourceProperty]
	public int TroopCount
	{
		get
		{
			return _troopCount;
		}
		set
		{
			if (value != _troopCount)
			{
				_troopCount = value;
				OnPropertyChangedWithValue(value, "TroopCount");
				UpdateIsWarned();
			}
		}
	}

	[DataSourceProperty]
	public int FormationClassInt
	{
		get
		{
			return _formationClassInt;
		}
		set
		{
			if (value != _formationClassInt)
			{
				_formationClassInt = value;
				OnPropertyChangedWithValue(value, "FormationClassInt");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSkeletalCrewCountWarningActive
	{
		get
		{
			return _isSkeletalCrewCountWarningActive;
		}
		set
		{
			if (value != _isSkeletalCrewCountWarningActive)
			{
				_isSkeletalCrewCountWarningActive = value;
				OnPropertyChangedWithValue(value, "IsSkeletalCrewCountWarningActive");
			}
		}
	}

	[DataSourceProperty]
	public string SkeletalCrewCountWarning
	{
		get
		{
			return _skeletalCrewCountWarning;
		}
		set
		{
			if (value != _skeletalCrewCountWarning)
			{
				_skeletalCrewCountWarning = value;
				OnPropertyChangedWithValue(value, "SkeletalCrewCountWarning");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMinimumCrewCountWarningActive
	{
		get
		{
			return _isMinimumCrewCountWarningActive;
		}
		set
		{
			if (value != _isMinimumCrewCountWarningActive)
			{
				_isMinimumCrewCountWarningActive = value;
				OnPropertyChangedWithValue(value, "IsMinimumCrewCountWarningActive");
			}
		}
	}

	[DataSourceProperty]
	public string MinimumCrewCountWarning
	{
		get
		{
			return _minimumCrewCountWarning;
		}
		set
		{
			if (value != _minimumCrewCountWarning)
			{
				_minimumCrewCountWarning = value;
				OnPropertyChangedWithValue(value, "MinimumCrewCountWarning");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CaptainSlotHint
	{
		get
		{
			return _captainSlotHint;
		}
		set
		{
			if (value != _captainSlotHint)
			{
				_captainSlotHint = value;
				OnPropertyChangedWithValue(value, "CaptainSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ShipSlotHint
	{
		get
		{
			return _shipSlotHint;
		}
		set
		{
			if (value != _shipSlotHint)
			{
				_shipSlotHint = value;
				OnPropertyChangedWithValue(value, "ShipSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AssignCaptainHint
	{
		get
		{
			return _assignCaptainHint;
		}
		set
		{
			if (value != _assignCaptainHint)
			{
				_assignCaptainHint = value;
				OnPropertyChangedWithValue(value, "AssignCaptainHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AssignShipHint
	{
		get
		{
			return _assignShipHint;
		}
		set
		{
			if (value != _assignShipHint)
			{
				_assignShipHint = value;
				OnPropertyChangedWithValue(value, "AssignShipHint");
			}
		}
	}

	[DataSourceProperty]
	public NavalOrderOfBattleHeroItemVM Captain
	{
		get
		{
			return _captain;
		}
		set
		{
			if (value != _captain)
			{
				_captain = value;
				OnPropertyChangedWithValue(value, "Captain");
				HasCaptain = Captain != null;
			}
		}
	}

	[DataSourceProperty]
	public NavalOrderOfBattleShipItemVM Ship
	{
		get
		{
			return _ship;
		}
		set
		{
			if (value == _ship)
			{
				return;
			}
			_ship = value;
			OnPropertyChangedWithValue(value, "Ship");
			HasShip = Ship != null;
			if (!HasShip)
			{
				foreach (OrderOfBattleFormationFilterSelectorItemVM filterItem in FilterItems)
				{
					filterItem.IsActive = false;
				}
			}
			UpdateIsWarned();
		}
	}

	[DataSourceProperty]
	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (value != _wSign)
			{
				_wSign = value;
				OnPropertyChangedWithValue(value, "WSign");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 ScreenPosition
	{
		get
		{
			return _screenPosition;
		}
		set
		{
			if (value != _screenPosition)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderOfBattleFormationFilterSelectorItemVM> FilterItems
	{
		get
		{
			return _filterItems;
		}
		set
		{
			if (value != _filterItems)
			{
				_filterItems = value;
				OnPropertyChangedWithValue(value, "FilterItems");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InfantryHint
	{
		get
		{
			return _infantryHint;
		}
		set
		{
			if (value != _infantryHint)
			{
				_infantryHint = value;
				OnPropertyChangedWithValue(value, "InfantryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RangedHint
	{
		get
		{
			return _rangedHint;
		}
		set
		{
			if (value != _rangedHint)
			{
				_rangedHint = value;
				OnPropertyChangedWithValue(value, "RangedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InfantryAndRangedHint
	{
		get
		{
			return _infantryAndRangedHint;
		}
		set
		{
			if (value != _infantryAndRangedHint)
			{
				_infantryAndRangedHint = value;
				OnPropertyChangedWithValue(value, "InfantryAndRangedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	public NavalOrderOfBattleFormationItemVM(Formation formation, Action<NavalOrderOfBattleFormationItemVM> onSelected, Action<NavalOrderOfBattleFormationItemVM> onClassChanged, Action<NavalOrderOfBattleFormationItemVM> onFilterToggled)
	{
		Formation = formation;
		_onSelected = onSelected;
		_onClassChanged = onClassChanged;
		_onFilterToggled = onFilterToggled;
		FilterItems = new MBBindingList<OrderOfBattleFormationFilterSelectorItemVM>();
		for (FormationFilterType formationFilterType = FormationFilterType.Shield; formationFilterType < FormationFilterType.NumberOfFilterTypes; formationFilterType++)
		{
			if (formationFilterType != FormationFilterType.Spear)
			{
				FilterItems.Add(new OrderOfBattleFormationFilterSelectorItemVM(formationFilterType, OnFilterToggled));
			}
		}
		FilterItems.ApplyActionOnAllItems(delegate(OrderOfBattleFormationFilterSelectorItemVM x)
		{
			x.IsEnabled = IsSelectable;
		});
		Tooltip = new BasicTooltipViewModel(() => GetTooltip());
		ExecuteSelectInfantryAndRanged();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		FormationName = (Formation.Index + 1).ToString();
		FormationIsEmptyText = new TextObject("{=P3IWytsr}Formation is currently empty").ToString();
		CaptainSlotHint = new HintViewModel(_captainSlotHintText);
		ShipSlotHint = new HintViewModel(_shipSlotHintText);
		AssignCaptainHint = new HintViewModel(_assignCaptainHintText);
		AssignShipHint = new HintViewModel(_assignShipHintText);
		InfantryHint = new HintViewModel(_infantryHintText);
		RangedHint = new HintViewModel(_rangedHintText);
		InfantryAndRangedHint = new HintViewModel(_infantryAndRangedHintText);
		TroopCount = Formation.CountOfUnits;
		SkeletalCrewCountWarning = new TextObject("{=JEwakKND}Ship is undercrewed!").ToString();
		MinimumCrewCountWarning = new TextObject("{=DY1uYOVI}Ship cannot be operated!").ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		foreach (OrderOfBattleFormationFilterSelectorItemVM filterItem in FilterItems)
		{
			filterItem.OnFinalize();
		}
		FilterItems.Clear();
	}

	public void ExecuteSelect()
	{
		_onSelected?.Invoke(this);
	}

	public void ExecuteAcceptShip()
	{
		if (GetCanAcceptShip())
		{
			OnAcceptShip?.Invoke(this);
		}
	}

	public void ExecuteAcceptCaptain()
	{
		if (GetCanAcceptCaptain())
		{
			OnAcceptCaptain?.Invoke(this);
		}
	}

	private void OnFilterToggled(OrderOfBattleFormationFilterSelectorItemVM filterItem)
	{
		if (IsSelectable)
		{
			_onFilterToggled?.Invoke(this);
		}
	}

	private bool HasAnyActiveFilter()
	{
		return FilterItems.Any((OrderOfBattleFormationFilterSelectorItemVM f) => f.IsActive);
	}

	public bool HasFilter(FormationFilterType filter)
	{
		return FilterItems.Any((OrderOfBattleFormationFilterSelectorItemVM f) => f.IsActive && f.FilterType == filter);
	}

	public void ExecuteSelectInfantry()
	{
		SelectedClass = DeploymentFormationClass.Infantry;
		OnClassSelectionUpdated();
	}

	public void ExecuteSelectRanged()
	{
		SelectedClass = DeploymentFormationClass.Ranged;
		OnClassSelectionUpdated();
	}

	public void ExecuteSelectInfantryAndRanged()
	{
		SelectedClass = DeploymentFormationClass.InfantryAndRanged;
		OnClassSelectionUpdated();
	}

	private void OnClassSelectionUpdated()
	{
		IsInfantrySelected = SelectedClass == DeploymentFormationClass.Infantry;
		IsRangedSelected = SelectedClass == DeploymentFormationClass.Ranged;
		IsInfantryAndRangedSelected = SelectedClass == DeploymentFormationClass.InfantryAndRanged;
		FormationClassInt = (int)SelectedClass;
		if (IsSelectable)
		{
			_onClassChanged?.Invoke(this);
		}
	}

	public bool GetCanAcceptShip()
	{
		if (!IsEnabled)
		{
			return Captain?.IsMainHero ?? false;
		}
		return true;
	}

	public bool GetCanAcceptCaptain()
	{
		if (IsEnabled && HasShip)
		{
			NavalOrderOfBattleHeroItemVM captain = Captain;
			if (captain == null)
			{
				return true;
			}
			return !captain.IsMainHero;
		}
		return false;
	}

	private List<TooltipProperty> GetTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty(new TextObject("{=cZNA5Z6l}Formation {NUMBER}").SetTextVariable("NUMBER", FormationName).ToString(), string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		if (!HasShip)
		{
			return list;
		}
		List<Agent> list2 = new List<Agent>();
		int[] array = new int[4];
		foreach (IFormationUnit allUnit in Formation.Arrangement.GetAllUnits())
		{
			if (allUnit is Agent agent)
			{
				if (agent.IsHero)
				{
					list2.Add(agent);
				}
				FormationClass actualTroopType = GetActualTroopType(agent);
				if (actualTroopType >= FormationClass.Infantry && actualTroopType < FormationClass.NumberOfDefaultFormations)
				{
					array[(int)actualTroopType]++;
				}
			}
		}
		foreach (Agent detachedUnit in Formation.DetachedUnits)
		{
			if (detachedUnit.IsHero)
			{
				list2.Add(detachedUnit);
			}
			FormationClass actualTroopType2 = GetActualTroopType(detachedUnit);
			if (actualTroopType2 >= FormationClass.Infantry && actualTroopType2 < FormationClass.NumberOfDefaultFormations)
			{
				array[(int)actualTroopType2]++;
			}
		}
		bool flag = false;
		for (FormationClass formationClass = FormationClass.Infantry; formationClass < FormationClass.NumberOfDefaultFormations; formationClass++)
		{
			int num = array[(int)formationClass];
			List<Agent> list3 = new List<Agent>();
			for (int i = 0; i < list2.Count; i++)
			{
				Agent agent2 = list2[i];
				if (formationClass == GetActualTroopType(agent2))
				{
					list3.Add(agent2);
				}
			}
			if (num > 0)
			{
				if (flag)
				{
					list.Add(new TooltipProperty(string.Empty, string.Empty, -1));
				}
				else
				{
					flag = true;
				}
				int num2 = (int)formationClass;
				list.Add(new TooltipProperty(GameTexts.FindText("str_troop_group_name", num2.ToString()).ToString(), num.ToString(), 0));
				if (list3.Count > 0)
				{
					list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
				}
				for (int j = 0; j < list3.Count; j++)
				{
					list.Add(new TooltipProperty(list3[j].Name, " ", 0));
				}
			}
		}
		if (HasAnyActiveFilter())
		{
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		}
		if (HasFilter(FormationFilterType.Shield))
		{
			GameTexts.SetVariable("TROOP_COUNT", Formation.GetCountOfUnitsWithCondition((Agent agent3) => agent3.HasShieldCached));
			GameTexts.SetVariable("TOTAL_TROOP_COUNT", GetTotalTroopCountWithFilter(SelectedClass, FormationFilterType.Shield));
			list.Add(new TooltipProperty(FormationFilterType.Shield.GetFilterName().ToString(), _filteredTroopCountInfoText.ToString(), 0));
		}
		if (HasFilter(FormationFilterType.Thrown))
		{
			GameTexts.SetVariable("TROOP_COUNT", Formation.GetCountOfUnitsWithCondition((Agent agent3) => agent3.HasThrownCached));
			GameTexts.SetVariable("TOTAL_TROOP_COUNT", GetTotalTroopCountWithFilter(SelectedClass, FormationFilterType.Thrown));
			list.Add(new TooltipProperty(FormationFilterType.Thrown.GetFilterName().ToString(), _filteredTroopCountInfoText.ToString(), 0));
		}
		if (HasFilter(FormationFilterType.Heavy))
		{
			GameTexts.SetVariable("TROOP_COUNT", Formation.GetCountOfUnitsWithCondition((Agent agent3) => MissionGameModels.Current.AgentStatCalculateModel.HasHeavyArmor(agent3)));
			GameTexts.SetVariable("TOTAL_TROOP_COUNT", GetTotalTroopCountWithFilter(SelectedClass, FormationFilterType.Heavy));
			list.Add(new TooltipProperty(FormationFilterType.Heavy.GetFilterName().ToString(), _filteredTroopCountInfoText.ToString(), 0));
		}
		if (HasFilter(FormationFilterType.HighTier))
		{
			GameTexts.SetVariable("TROOP_COUNT", Formation.GetCountOfUnitsWithCondition((Agent agent3) => agent3.Character.GetBattleTier() >= 4));
			GameTexts.SetVariable("TOTAL_TROOP_COUNT", GetTotalTroopCountWithFilter(SelectedClass, FormationFilterType.HighTier));
			list.Add(new TooltipProperty(FormationFilterType.HighTier.GetFilterName().ToString(), _filteredTroopCountInfoText.ToString(), 0));
		}
		if (HasFilter(FormationFilterType.LowTier))
		{
			GameTexts.SetVariable("TROOP_COUNT", Formation.GetCountOfUnitsWithCondition((Agent agent3) => agent3.Character.GetBattleTier() <= 3));
			GameTexts.SetVariable("TOTAL_TROOP_COUNT", GetTotalTroopCountWithFilter(SelectedClass, FormationFilterType.LowTier));
			list.Add(new TooltipProperty(FormationFilterType.LowTier.GetFilterName().ToString(), _filteredTroopCountInfoText.ToString(), 0));
		}
		if (Ship?.MissionShip != null)
		{
			int reservedTroopsCountOfShip = Mission.Current.GetMissionBehavior<NavalAgentsLogic>().GetReservedTroopsCountOfShip(Ship.MissionShip);
			if (reservedTroopsCountOfShip > 0)
			{
				list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
				list.Add(new TooltipProperty(new TextObject("{=25fleLuY}Troops In Reserve").ToString(), reservedTroopsCountOfShip.ToString(), 0));
			}
		}
		return list;
	}

	private FormationClass GetActualTroopType(Agent agent)
	{
		if (QueryLibrary.IsInfantry(agent))
		{
			return FormationClass.Infantry;
		}
		if (QueryLibrary.IsRanged(agent))
		{
			return FormationClass.Ranged;
		}
		if (QueryLibrary.IsCavalry(agent))
		{
			return FormationClass.Cavalry;
		}
		if (QueryLibrary.IsRangedCavalry(agent))
		{
			return FormationClass.HorseArcher;
		}
		return FormationClass.NumberOfAllFormations;
	}

	private void UpdateIsWarned()
	{
		IsMinimumCrewCountWarningActive = HasShip && TroopCount < 4;
		IsSkeletalCrewCountWarningActive = !IsMinimumCrewCountWarningActive && HasShip && TroopCount < Ship.ShipOrigin.SkeletalCrewCapacity;
	}
}
