using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

public class MissionSiegeEngineMarkerTargetVM : ViewModel
{
	private Vec2 _screenPosition;

	private float _distance;

	private bool _isEnabled;

	private bool _isBehind;

	private bool _isEnemy;

	private string _engineType;

	private int _hitPoints;

	public SiegeWeapon Engine { get; private set; }

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
	public bool IsEnemy
	{
		get
		{
			return _isEnemy;
		}
		set
		{
			if (_isEnemy != value)
			{
				_isEnemy = value;
				OnPropertyChangedWithValue(value, "IsEnemy");
			}
		}
	}

	[DataSourceProperty]
	public string EngineType
	{
		get
		{
			return _engineType;
		}
		set
		{
			if (_engineType != value)
			{
				_engineType = value;
				OnPropertyChangedWithValue(value, "EngineType");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (_isBehind != value)
			{
				_isBehind = value;
				OnPropertyChangedWithValue(value, "IsBehind");
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
	public int HitPoints
	{
		get
		{
			return _hitPoints;
		}
		set
		{
			if (_hitPoints != value)
			{
				_hitPoints = value;
				OnPropertyChangedWithValue(value, "HitPoints");
			}
		}
	}

	public MissionSiegeEngineMarkerTargetVM(SiegeWeapon engine, bool isEnemy)
	{
		Engine = engine;
		EngineType = Engine.GetSiegeEngineType().StringId;
		IsEnemy = isEnemy;
	}

	public void Refresh()
	{
		HitPoints = MathF.Ceiling(Engine.DestructionComponent.HitPoint / Engine.DestructionComponent.MaxHitPoint * 100f);
	}
}
