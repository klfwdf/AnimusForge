using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

namespace NavalDLC.ViewModelCollection.HUD.ShipMarker;

public class NavalShipMarkerItemVM : ViewModel
{
	public enum TeamTypes
	{
		PlayerTeam,
		PlayerAllyTeam,
		EnemyTeam
	}

	public readonly Formation Formation;

	public readonly MissionShip Ship;

	private readonly string _formationType;

	private readonly string _shipType;

	private int _teamType;

	private bool _isEnabled;

	private bool _isCenterOfFocus;

	private bool _isShipTargetRelevant;

	private bool _isTargetingAShip;

	private bool _showDistanceTexts;

	private int _size;

	private int _wSign;

	private float _distance;

	private string _distanceText;

	private string _markerType;

	private Vec2 _screenPosition;

	private int _crewCount;

	private float _hitPoints;

	private float _maxHitPoints;

	private bool _hasAnyTroops;

	[DataSourceProperty]
	public int TeamType
	{
		get
		{
			return _teamType;
		}
		set
		{
			if (value != _teamType)
			{
				_teamType = value;
				OnPropertyChangedWithValue(value, "TeamType");
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
			}
		}
	}

	[DataSourceProperty]
	public bool IsCenterOfFocus
	{
		get
		{
			return _isCenterOfFocus;
		}
		set
		{
			if (_isCenterOfFocus != value)
			{
				_isCenterOfFocus = value;
				OnPropertyChangedWithValue(value, "IsCenterOfFocus");
			}
		}
	}

	[DataSourceProperty]
	public bool IsShipTargetRelevant
	{
		get
		{
			return _isShipTargetRelevant;
		}
		set
		{
			if (_isShipTargetRelevant != value)
			{
				_isShipTargetRelevant = value;
				OnPropertyChangedWithValue(value, "IsShipTargetRelevant");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTargetingAShip
	{
		get
		{
			return _isTargetingAShip;
		}
		set
		{
			if (_isTargetingAShip != value)
			{
				_isTargetingAShip = value;
				OnPropertyChangedWithValue(value, "IsTargetingAShip");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowDistanceTexts
	{
		get
		{
			return _showDistanceTexts;
		}
		set
		{
			if (_showDistanceTexts != value)
			{
				_showDistanceTexts = value;
				OnPropertyChangedWithValue(value, "ShowDistanceTexts");
			}
		}
	}

	[DataSourceProperty]
	public int Size
	{
		get
		{
			return _size;
		}
		set
		{
			if (value != _size)
			{
				_size = value;
				OnPropertyChangedWithValue(value, "Size");
			}
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
	public float Distance
	{
		get
		{
			return _distance;
		}
		set
		{
			if (value != _distance)
			{
				_distance = value;
				OnPropertyChangedWithValue(value, "Distance");
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
	public int CrewCount
	{
		get
		{
			return _crewCount;
		}
		set
		{
			if (value != _crewCount)
			{
				_crewCount = value;
				OnPropertyChangedWithValue(value, "CrewCount");
			}
		}
	}

	[DataSourceProperty]
	public string MarkerType
	{
		get
		{
			return _markerType;
		}
		set
		{
			if (value != _markerType)
			{
				_markerType = value;
				OnPropertyChangedWithValue(value, "MarkerType");
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
	public float HitPoints
	{
		get
		{
			return _hitPoints;
		}
		set
		{
			if (value != _hitPoints)
			{
				_hitPoints = value;
				OnPropertyChangedWithValue(value, "HitPoints");
			}
		}
	}

	[DataSourceProperty]
	public float MaxHitPoints
	{
		get
		{
			return _maxHitPoints;
		}
		set
		{
			if (value != _maxHitPoints)
			{
				_maxHitPoints = value;
				OnPropertyChangedWithValue(value, "MaxHitPoints");
			}
		}
	}

	[DataSourceProperty]
	public bool HasAnyTroops
	{
		get
		{
			return _hasAnyTroops;
		}
		set
		{
			if (value != _hasAnyTroops)
			{
				_hasAnyTroops = value;
				OnPropertyChangedWithValue(value, "HasAnyTroops");
			}
		}
	}

	public NavalShipMarkerItemVM(Formation formation, MissionShip ship)
	{
		Formation = formation;
		Ship = ship;
		_formationType = MissionFormationMarkerTargetVM.GetFormationType(Formation.RepresentativeClass);
		_shipType = "Ship_" + Ship?.ShipOrigin.Hull.Type.ToString();
		if (Formation.Team.IsPlayerTeam)
		{
			TeamType = 0;
		}
		else if (Formation.Team.IsPlayerAlly)
		{
			TeamType = 1;
		}
		else
		{
			TeamType = 2;
		}
		Refresh();
	}

	public void Refresh()
	{
		Size = Formation.CountOfUnits;
		HasAnyTroops = Size > 0;
		MarkerType = (IsShipActive() ? _shipType : _formationType);
		HitPoints = (IsShipActive() ? Ship.HitPoints : 0f);
		MaxHitPoints = Ship?.MaxHealth ?? 1f;
	}

	public void SetTargetedState(bool isFocused, bool isTargetingAShip)
	{
		IsCenterOfFocus = isFocused;
		IsTargetingAShip = isTargetingAShip;
	}

	public bool IsShipActive()
	{
		if (Ship != null && !Ship.IsDisabled && !Ship.IsSinking && !Ship.IsRemoved)
		{
			return Ship.HitPoints > 0f;
		}
		return false;
	}
}
