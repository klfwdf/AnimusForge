using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class OrderTroopItemVM : OrderSubjectVM
{
	public FormationClass InitialFormationClass;

	public Formation Formation;

	public Type MachineType;

	public Action<OrderTroopItemVM> SetSelected;

	private OrderTroopItemFormationClassVM _cachedInfantryItem;

	private OrderTroopItemFormationClassVM _cachedRangedItem;

	private OrderTroopItemFormationClassVM _cachedCavalryItem;

	private OrderTroopItemFormationClassVM _cachedHorseArcherItem;

	private BasicCharacterObject _cachedCaptain;

	private bool _isValid;

	private int _formationIndex;

	private int _currentMemberCount;

	private int _morale;

	private float _ammoPercentage;

	private bool _isAmmoAvailable;

	private bool _haveTroops;

	private bool _hasTarget;

	private bool _isTargetRelevant;

	private bool _hasCaptain;

	private string _currentOrderIconId;

	private string _currentTargetFormationType;

	private string _formationName;

	private CharacterImageIdentifierVM _captainImageIdentifier;

	private MBBindingList<OrderTroopItemFormationClassVM> _activeFormationClasses;

	private MBBindingList<OrderTroopItemFilterVM> _activeFilters;

	public bool ContainsDeadTroop { get; private set; }

	[DataSourceProperty]
	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (value != _isValid)
			{
				_isValid = value;
				OnPropertyChangedWithValue(value, "IsValid");
			}
		}
	}

	[DataSourceProperty]
	public int FormationIndex
	{
		get
		{
			return _formationIndex;
		}
		set
		{
			if (value != _formationIndex)
			{
				_formationIndex = value;
				OnPropertyChangedWithValue(value, "FormationIndex");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentMemberCount
	{
		get
		{
			return _currentMemberCount;
		}
		set
		{
			if (value != _currentMemberCount)
			{
				_currentMemberCount = value;
				OnPropertyChangedWithValue(value, "CurrentMemberCount");
				HaveTroops = value > 0;
			}
		}
	}

	[DataSourceProperty]
	public int Morale
	{
		get
		{
			return _morale;
		}
		set
		{
			if (value != _morale)
			{
				_morale = value;
				OnPropertyChangedWithValue(value, "Morale");
			}
		}
	}

	[DataSourceProperty]
	public float AmmoPercentage
	{
		get
		{
			return _ammoPercentage;
		}
		set
		{
			if (value != _ammoPercentage)
			{
				_ammoPercentage = value;
				OnPropertyChangedWithValue(value, "AmmoPercentage");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAmmoAvailable
	{
		get
		{
			return _isAmmoAvailable;
		}
		set
		{
			if (value != _isAmmoAvailable)
			{
				_isAmmoAvailable = value;
				OnPropertyChangedWithValue(value, "IsAmmoAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool HaveTroops
	{
		get
		{
			return _haveTroops;
		}
		set
		{
			if (value != _haveTroops)
			{
				_haveTroops = value;
				OnPropertyChangedWithValue(value, "HaveTroops");
			}
		}
	}

	[DataSourceProperty]
	public bool HasTarget
	{
		get
		{
			return _hasTarget;
		}
		set
		{
			if (value != _hasTarget)
			{
				_hasTarget = value;
				OnPropertyChangedWithValue(value, "HasTarget");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTargetRelevant
	{
		get
		{
			return _isTargetRelevant;
		}
		set
		{
			if (value != _isTargetRelevant)
			{
				_isTargetRelevant = value;
				OnPropertyChangedWithValue(value, "IsTargetRelevant");
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
	public string CurrentOrderIconId
	{
		get
		{
			return _currentOrderIconId;
		}
		set
		{
			if (value != _currentOrderIconId)
			{
				_currentOrderIconId = value;
				OnPropertyChangedWithValue(value, "CurrentOrderIconId");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentTargetFormationType
	{
		get
		{
			return _currentTargetFormationType;
		}
		set
		{
			if (value != _currentTargetFormationType)
			{
				_currentTargetFormationType = value;
				OnPropertyChangedWithValue(value, "CurrentTargetFormationType");
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
	public CharacterImageIdentifierVM CaptainImageIdentifier
	{
		get
		{
			return _captainImageIdentifier;
		}
		set
		{
			if (value != _captainImageIdentifier)
			{
				_captainImageIdentifier = value;
				OnPropertyChangedWithValue(value, "CaptainImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderTroopItemFormationClassVM> ActiveFormationClasses
	{
		get
		{
			return _activeFormationClasses;
		}
		set
		{
			if (value != _activeFormationClasses)
			{
				_activeFormationClasses = value;
				OnPropertyChangedWithValue(value, "ActiveFormationClasses");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderTroopItemFilterVM> ActiveFilters
	{
		get
		{
			return _activeFilters;
		}
		set
		{
			if (value != _activeFilters)
			{
				_activeFilters = value;
				OnPropertyChangedWithValue(value, "ActiveFilters");
			}
		}
	}

	public static event Action<OrderTroopItemVM, bool> OnSelectionChange;

	public OrderTroopItemVM(Formation formation, Action<OrderTroopItemVM> setSelected, Func<Formation, int> getMorale)
	{
		IsValid = true;
		ActiveFormationClasses = new MBBindingList<OrderTroopItemFormationClassVM>();
		ActiveFilters = new MBBindingList<OrderTroopItemFilterVM>();
		InitialFormationClass = formation.FormationIndex;
		SetFormationClassFromFormation(formation);
		Formation = formation;
		FormationIndex = formation.Index;
		FormationName = (FormationIndex + 1).ToString();
		SetSelected = setSelected;
		CurrentMemberCount = (formation.IsPlayerTroopInFormation ? (formation.CountOfUnits - 1) : formation.CountOfUnits);
		Morale = getMorale(formation);
		base.UnderAttackOfType = 0;
		base.BehaviorType = 0;
		UpdateSelectionKeyInfo();
		UpdateVisuals();
		Formation.OnUnitCountChanged += FormationOnOnUnitCountChanged;
		RefreshValues();
	}

	public OrderTroopItemVM()
	{
		IsValid = false;
	}

	public override void OnFinalize()
	{
		if (IsValid)
		{
			Formation.OnUnitCountChanged -= FormationOnOnUnitCountChanged;
		}
		base.ApplySelectionKey?.OnFinalize();
		base.ToggleSelectionKey?.OnFinalize();
	}

	protected override void OnSelectionStateChanged(bool isSelected)
	{
		OrderTroopItemVM.OnSelectionChange?.Invoke(this, isSelected);
	}

	private void FormationOnOnUnitCountChanged(Formation formation)
	{
		CurrentMemberCount = (formation.IsPlayerTroopInFormation ? (formation.CountOfUnits - 1) : formation.CountOfUnits);
		UpdateVisuals();
	}

	public void OnFormationAgentRemoved(Agent agent)
	{
		if (!agent.IsActive())
		{
			ContainsDeadTroop = true;
		}
		UpdateVisuals();
	}

	public virtual void UpdateVisuals()
	{
		if (Formation?.Captain?.Character != null)
		{
			if (CaptainImageIdentifier == null || Formation.Captain.Character != _cachedCaptain)
			{
				CaptainImageIdentifier?.OnFinalize();
				CaptainImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(Formation.Captain.Character));
				HasCaptain = true;
				_cachedCaptain = Formation.Captain.Character;
			}
		}
		else
		{
			CaptainImageIdentifier?.OnFinalize();
			CaptainImageIdentifier = null;
			HasCaptain = false;
		}
	}

	public virtual void Update()
	{
	}

	public void UpdateSelectionKeyInfo()
	{
		if (Formation == null)
		{
			return;
		}
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			GameKey gameKey = HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(91);
			base.ToggleSelectionKey?.OnFinalize();
			base.ToggleSelectionKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: true);
			gameKey = HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(90);
			base.ApplySelectionKey?.OnFinalize();
			base.ApplySelectionKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: true);
			return;
		}
		int num = -1;
		if (Formation.Index == 0)
		{
			num = 79;
		}
		else if (Formation.Index == 1)
		{
			num = 80;
		}
		else if (Formation.Index == 2)
		{
			num = 81;
		}
		else if (Formation.Index == 3)
		{
			num = 82;
		}
		else if (Formation.Index == 4)
		{
			num = 83;
		}
		else if (Formation.Index == 5)
		{
			num = 84;
		}
		else if (Formation.Index == 6)
		{
			num = 85;
		}
		else if (Formation.Index == 7)
		{
			num = 86;
		}
		if (num != -1)
		{
			GameKey gameKey2 = HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(num);
			base.ApplySelectionKey?.OnFinalize();
			base.ApplySelectionKey = InputKeyItemVM.CreateFromGameKey(gameKey2, isConsoleOnly: false);
			base.ToggleSelectionKey?.OnFinalize();
			base.ToggleSelectionKey = null;
		}
	}

	public bool SetFormationClassFromFormation(Formation formation)
	{
		bool flag = formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.Infantry) > 0;
		bool flag2 = formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.Ranged) > 0;
		bool flag3 = formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.Cavalry) > 0;
		bool flag4 = formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.HorseArcher) > 0;
		if (flag && _cachedInfantryItem == null)
		{
			_cachedInfantryItem = new OrderTroopItemFormationClassVM(formation, FormationClass.Infantry);
			ActiveFormationClasses.Add(_cachedInfantryItem);
		}
		else if (!flag)
		{
			ActiveFormationClasses.Remove(_cachedInfantryItem);
			_cachedInfantryItem = null;
		}
		if (flag2 && _cachedRangedItem == null)
		{
			_cachedRangedItem = new OrderTroopItemFormationClassVM(formation, FormationClass.Ranged);
			ActiveFormationClasses.Add(_cachedRangedItem);
		}
		else if (!flag2)
		{
			ActiveFormationClasses.Remove(_cachedRangedItem);
			_cachedRangedItem = null;
		}
		if (flag3 && _cachedCavalryItem == null)
		{
			_cachedCavalryItem = new OrderTroopItemFormationClassVM(formation, FormationClass.Cavalry);
			ActiveFormationClasses.Add(_cachedCavalryItem);
		}
		else if (!flag3)
		{
			ActiveFormationClasses.Remove(_cachedCavalryItem);
			_cachedCavalryItem = null;
		}
		if (flag4 && _cachedHorseArcherItem == null)
		{
			_cachedHorseArcherItem = new OrderTroopItemFormationClassVM(formation, FormationClass.HorseArcher);
			ActiveFormationClasses.Add(_cachedHorseArcherItem);
		}
		else if (!flag4)
		{
			ActiveFormationClasses.Remove(_cachedHorseArcherItem);
			_cachedHorseArcherItem = null;
		}
		foreach (OrderTroopItemFormationClassVM activeFormationClass in ActiveFormationClasses)
		{
			activeFormationClass.UpdateTroopCount();
		}
		UpdateVisuals();
		return false;
	}

	public void UpdateFilterData(List<FormationFilterType> usedFilters)
	{
		ActiveFilters.Clear();
		foreach (FormationFilterType usedFilter in usedFilters)
		{
			ActiveFilters.Add(new OrderTroopItemFilterVM((int)usedFilter));
		}
	}

	public void ExecuteAction()
	{
		SetSelected(this);
	}

	public virtual void RefreshTargetedOrderVisual()
	{
		bool hasTarget = false;
		string currentOrderIconId = null;
		string currentTargetFormationType = null;
		for (int i = 0; i < base.ActiveOrders.Count; i++)
		{
			OrderItemVM orderItemVM = base.ActiveOrders[i];
			if (orderItemVM.Order.IsTargeted())
			{
				Formation targetFormation = Formation.TargetFormation;
				if (targetFormation != null)
				{
					currentTargetFormationType = MissionFormationMarkerTargetVM.GetFormationType(targetFormation.PhysicalClass);
					hasTarget = true;
				}
				currentOrderIconId = orderItemVM.OrderIconId;
			}
		}
		HasTarget = hasTarget;
		CurrentOrderIconId = currentOrderIconId;
		CurrentTargetFormationType = currentTargetFormationType;
	}

	public virtual TextObject GetVisibleNameOfFormationForMessage()
	{
		return GameTexts.FindText("str_formation_class_string", Formation.PhysicalClass.GetName());
	}
}
