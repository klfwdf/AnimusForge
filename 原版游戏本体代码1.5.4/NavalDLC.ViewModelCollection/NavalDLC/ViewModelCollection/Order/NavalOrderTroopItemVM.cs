using System;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace NavalDLC.ViewModelCollection.Order;

public class NavalOrderTroopItemVM : OrderTroopItemVM
{
	private readonly NavalShipsLogic _navalShipsLogic;

	private readonly TextObject _troopCountTextObj;

	private readonly TextObject _healthTextObj;

	private MissionShip _cachedShip;

	private string _troopCountText;

	private string _healthText;

	private int _formationClassInt = 5;

	private string _prefabId;

	private bool _hasShip;

	private bool _isShipActive;

	[DataSourceProperty]
	public string TroopCountText
	{
		get
		{
			return _troopCountText;
		}
		set
		{
			if (value != _troopCountText)
			{
				_troopCountText = value;
				OnPropertyChangedWithValue(value, "TroopCountText");
			}
		}
	}

	[DataSourceProperty]
	public string HealthText
	{
		get
		{
			return _healthText;
		}
		set
		{
			if (value != _healthText)
			{
				_healthText = value;
				OnPropertyChangedWithValue(value, "HealthText");
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
	public string PrefabId
	{
		get
		{
			return _prefabId;
		}
		set
		{
			if (value != _prefabId)
			{
				_prefabId = value;
				OnPropertyChangedWithValue(value, "PrefabId");
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
			}
		}
	}

	[DataSourceProperty]
	public bool IsShipActive
	{
		get
		{
			return _isShipActive;
		}
		set
		{
			if (value != _isShipActive)
			{
				_isShipActive = value;
				OnPropertyChangedWithValue(value, "IsShipActive");
			}
		}
	}

	public NavalOrderTroopItemVM(Formation formation, Action<OrderTroopItemVM> setSelected, Func<Formation, int> getMorale)
		: base(formation, setSelected, getMorale)
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_troopCountTextObj = GameTexts.FindText("str_LEFT_over_RIGHT_no_space");
		_healthTextObj = GameTexts.FindText("str_NUMBER_percent");
		UpdateVisuals();
	}

	public override void UpdateVisuals()
	{
		base.UpdateVisuals();
		if (Formation != null && _navalShipsLogic != null && _navalShipsLogic.GetShip(TeamSideEnum.PlayerTeam, Formation.FormationIndex, out var ship))
		{
			if (string.IsNullOrEmpty(PrefabId) || ship != _cachedShip)
			{
				_cachedShip = ship;
				HasShip = _cachedShip != null;
				MissionShip cachedShip = _cachedShip;
				IsShipActive = cachedShip != null && cachedShip.HitPoints > 0f;
				PrefabId = ((_cachedShip != null) ? NavalUIHelper.GetPrefabIdOfShipHull(_cachedShip.ShipOrigin.Hull) : null);
			}
		}
		else
		{
			PrefabId = null;
			HasShip = false;
			_cachedShip = null;
			IsShipActive = false;
		}
	}

	public override void Update()
	{
		base.Update();
		MissionShip cachedShip = _cachedShip;
		IsShipActive = cachedShip != null && cachedShip.HitPoints > 0f;
		if (IsShipActive)
		{
			TroopCountText = _troopCountTextObj.SetTextVariable("LEFT", Formation.CountOfUnits.ToString()).SetTextVariable("RIGHT", _cachedShip.CrewSizeOnMainDeck.ToString()).ToString();
			HealthText = _healthTextObj.SetTextVariable("NUMBER", ((int)(_cachedShip.HitPoints / _cachedShip.MaxHealth * 100f)).ToString()).ToString();
		}
		else
		{
			TroopCountText = Formation.CountOfUnits.ToString();
			HealthText = _healthTextObj.SetTextVariable("NUMBER", 0).ToString();
		}
	}

	public override void RefreshTargetedOrderVisual()
	{
		if (!IsShipActive)
		{
			base.RefreshTargetedOrderVisual();
			return;
		}
		bool flag = false;
		string currentOrderIconId = null;
		string currentTargetFormationType = null;
		if (_cachedShip.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Engage && _cachedShip.ShipOrder.TargetShip != null)
		{
			flag = true;
			currentTargetFormationType = "Ship_" + _cachedShip.ShipOrder.TargetShip.ShipOrigin.Hull.Type;
			currentOrderIconId = "order_movement_advance";
		}
		if (!flag)
		{
			for (int i = 0; i < base.ActiveOrders.Count; i++)
			{
				OrderItemVM orderItemVM = base.ActiveOrders[i];
				if (orderItemVM.Order.IsTargeted())
				{
					Formation targetFormation = Formation.TargetFormation;
					if (targetFormation != null)
					{
						_navalShipsLogic.GetShip(targetFormation, out var ship);
						currentTargetFormationType = ((ship == null) ? MissionFormationMarkerTargetVM.GetFormationType(targetFormation.PhysicalClass) : ("Ship_" + ship.ShipOrigin.Hull.Type));
						flag = true;
					}
					currentOrderIconId = orderItemVM.OrderIconId;
				}
			}
		}
		base.HasTarget = flag;
		base.CurrentOrderIconId = currentOrderIconId;
		base.CurrentTargetFormationType = currentTargetFormationType;
	}

	public void UpdateClassData(DeploymentFormationClass formationClass)
	{
		FormationClassInt = (int)formationClass;
	}

	public override TextObject GetVisibleNameOfFormationForMessage()
	{
		if (IsShipActive)
		{
			return _cachedShip.ShipOrigin.Name;
		}
		return base.GetVisibleNameOfFormationForMessage();
	}
}
