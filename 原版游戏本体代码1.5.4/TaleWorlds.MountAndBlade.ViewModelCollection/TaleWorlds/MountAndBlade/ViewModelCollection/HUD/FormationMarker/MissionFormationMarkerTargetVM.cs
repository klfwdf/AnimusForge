using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

public class MissionFormationMarkerTargetVM : ViewModel
{
	public enum TeamTypes
	{
		PlayerTeam,
		PlayerAllyTeam,
		EnemyTeam
	}

	private Vec2 _screenPosition;

	private float _distance;

	private string _distanceText;

	private bool _isEnabled;

	private bool _isCenterOfFocus;

	private bool _isFormationTargetRelevant;

	private bool _isTargetingAFormation;

	private bool _showDistanceTexts;

	private int _teamType;

	private int _size;

	private int _wSign;

	private string _formationType;

	private float _farAlphaTarget = 0.7f;

	private float _farDistanceCutoff = 500f;

	private float _closeDistanceCutoff = 10f;

	private float _closestFadeoutRange = 5f;

	private float _visibilityRatio = 1f;

	private float _alwaysOnDistance = 25f;

	private int _visibilityState = -1;

	public Formation Formation { get; private set; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (_isEnabled != value)
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
	public bool IsFormationTargetRelevant
	{
		get
		{
			return _isFormationTargetRelevant;
		}
		set
		{
			if (_isFormationTargetRelevant != value)
			{
				_isFormationTargetRelevant = value;
				OnPropertyChangedWithValue(value, "IsFormationTargetRelevant");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTargetingAFormation
	{
		get
		{
			return _isTargetingAFormation;
		}
		set
		{
			if (_isTargetingAFormation != value)
			{
				_isTargetingAFormation = value;
				OnPropertyChangedWithValue(value, "IsTargetingAFormation");
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
	public string FormationType
	{
		get
		{
			return _formationType;
		}
		set
		{
			if (_formationType != value)
			{
				_formationType = value;
				OnPropertyChangedWithValue(value, "FormationType");
			}
		}
	}

	[DataSourceProperty]
	public int TeamType
	{
		get
		{
			return _teamType;
		}
		set
		{
			if (_teamType != value)
			{
				_teamType = value;
				OnPropertyChangedWithValue(value, "TeamType");
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
			if (value.x != _screenPosition.x || value.y != _screenPosition.y)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
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
			if (_distance != value && !float.IsNaN(value))
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
			if (_distanceText != value)
			{
				_distanceText = value;
				OnPropertyChangedWithValue(value, "DistanceText");
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
			if (_size != value)
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
			if (_wSign != value)
			{
				_wSign = value;
				OnPropertyChangedWithValue(value, "WSign");
			}
		}
	}

	[DataSourceProperty]
	public float FarAlphaTarget
	{
		get
		{
			return _farAlphaTarget;
		}
		set
		{
			if (_farAlphaTarget != value)
			{
				_farAlphaTarget = value;
				OnPropertyChangedWithValue(value, "FarAlphaTarget");
			}
		}
	}

	[DataSourceProperty]
	public float FarDistanceCutoff
	{
		get
		{
			return _farDistanceCutoff;
		}
		set
		{
			if (_farDistanceCutoff != value)
			{
				_farDistanceCutoff = value;
				OnPropertyChangedWithValue(value, "FarDistanceCutoff");
			}
		}
	}

	[DataSourceProperty]
	public float CloseDistanceCutoff
	{
		get
		{
			return _closeDistanceCutoff;
		}
		set
		{
			if (_closeDistanceCutoff != value)
			{
				_closeDistanceCutoff = value;
				OnPropertyChangedWithValue(value, "CloseDistanceCutoff");
			}
		}
	}

	[DataSourceProperty]
	public float ClosestFadeoutRange
	{
		get
		{
			return _closestFadeoutRange;
		}
		set
		{
			if (_closestFadeoutRange != value)
			{
				_closestFadeoutRange = value;
				OnPropertyChangedWithValue(value, "ClosestFadeoutRange");
			}
		}
	}

	[DataSourceProperty]
	public float VisibilityRatio
	{
		get
		{
			return _visibilityRatio;
		}
		set
		{
			if (!_visibilityRatio.ApproximatelyEqualsTo(value))
			{
				_visibilityRatio = value;
				OnPropertyChangedWithValue(value, "VisibilityRatio");
			}
		}
	}

	[DataSourceProperty]
	public float AlwaysOnDistance
	{
		get
		{
			return _alwaysOnDistance;
		}
		set
		{
			if (!_alwaysOnDistance.ApproximatelyEqualsTo(value))
			{
				_alwaysOnDistance = value;
				OnPropertyChangedWithValue(value, "AlwaysOnDistance");
			}
		}
	}

	[DataSourceProperty]
	public int VisibilityState
	{
		get
		{
			return _visibilityState;
		}
		set
		{
			if (value != _visibilityState)
			{
				_visibilityState = value;
				OnPropertyChangedWithValue(value, "VisibilityState");
			}
		}
	}

	public MissionFormationMarkerTargetVM(Formation formation)
	{
		Formation = formation;
		FormationType = GetFormationType(Formation.RepresentativeClass);
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
	}

	public void Refresh()
	{
		Size = Formation.CountOfUnits;
	}

	public void SetTargetedState(bool isFocused, bool isTargetingAFormation)
	{
		IsCenterOfFocus = isFocused;
		IsTargetingAFormation = isTargetingAFormation;
	}

	public static string GetFormationType(FormationClass formationType)
	{
		switch (formationType)
		{
		case FormationClass.Infantry:
			return "Infantry_Light";
		case FormationClass.Ranged:
			return "Archer_Light";
		case FormationClass.Cavalry:
			return "Cavalry_Light";
		case FormationClass.HorseArcher:
			return "HorseArcher_Light";
		case FormationClass.LightCavalry:
			return "Cavalry_Light";
		case FormationClass.HeavyCavalry:
			return "Cavalry_Heavy";
		case FormationClass.NumberOfDefaultFormations:
		case FormationClass.HeavyInfantry:
		case FormationClass.NumberOfRegularFormations:
		case FormationClass.Bodyguard:
		case FormationClass.NumberOfAllFormations:
			return "Infantry_Heavy";
		default:
			return "None";
		}
	}
}
