using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class CrosshairVM : ViewModel
{
	private bool _isVisible;

	private bool _isReloadPhasesVisible;

	private bool _isHitMarkerVisible;

	private bool _isVictimDead;

	private bool _isHumanoidHeadshot;

	private bool _isTargetInvalid;

	private MBBindingList<ReloadPhaseItemVM> _reloadPhases;

	private double _crosshairAccuracy;

	private double _crosshairScale;

	private double _topArrowOpacity;

	private double _bottomArrowOpacity;

	private double _rightArrowOpacity;

	private double _leftArrowOpacity;

	private int _crosshairType;

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsReloadPhasesVisible
	{
		get
		{
			return _isReloadPhasesVisible;
		}
		set
		{
			if (value != _isReloadPhasesVisible)
			{
				_isReloadPhasesVisible = value;
				OnPropertyChangedWithValue(value, "IsReloadPhasesVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHitMarkerVisible
	{
		get
		{
			return _isHitMarkerVisible;
		}
		set
		{
			if (value != _isHitMarkerVisible)
			{
				_isHitMarkerVisible = value;
				OnPropertyChangedWithValue(value, "IsHitMarkerVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVictimDead
	{
		get
		{
			return _isVictimDead;
		}
		set
		{
			if (value != _isVictimDead)
			{
				_isVictimDead = value;
				OnPropertyChangedWithValue(value, "IsVictimDead");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHumanoidHeadshot
	{
		get
		{
			return _isHumanoidHeadshot;
		}
		set
		{
			if (value != _isHumanoidHeadshot)
			{
				_isHumanoidHeadshot = value;
				OnPropertyChangedWithValue(value, "IsHumanoidHeadshot");
			}
		}
	}

	[DataSourceProperty]
	public double TopArrowOpacity
	{
		get
		{
			return _topArrowOpacity;
		}
		set
		{
			if (value != _topArrowOpacity)
			{
				_topArrowOpacity = value;
				OnPropertyChangedWithValue(value, "TopArrowOpacity");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ReloadPhaseItemVM> ReloadPhases
	{
		get
		{
			return _reloadPhases;
		}
		set
		{
			if (value != _reloadPhases)
			{
				_reloadPhases = value;
				OnPropertyChangedWithValue(value, "ReloadPhases");
			}
		}
	}

	[DataSourceProperty]
	public double BottomArrowOpacity
	{
		get
		{
			return _bottomArrowOpacity;
		}
		set
		{
			if (value != _bottomArrowOpacity)
			{
				_bottomArrowOpacity = value;
				OnPropertyChangedWithValue(value, "BottomArrowOpacity");
			}
		}
	}

	[DataSourceProperty]
	public double RightArrowOpacity
	{
		get
		{
			return _rightArrowOpacity;
		}
		set
		{
			if (value != _rightArrowOpacity)
			{
				_rightArrowOpacity = value;
				OnPropertyChangedWithValue(value, "RightArrowOpacity");
			}
		}
	}

	[DataSourceProperty]
	public double LeftArrowOpacity
	{
		get
		{
			return _leftArrowOpacity;
		}
		set
		{
			if (value != _leftArrowOpacity)
			{
				_leftArrowOpacity = value;
				OnPropertyChangedWithValue(value, "LeftArrowOpacity");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTargetInvalid
	{
		get
		{
			return _isTargetInvalid;
		}
		set
		{
			if (value != _isTargetInvalid)
			{
				_isTargetInvalid = value;
				OnPropertyChangedWithValue(value, "IsTargetInvalid");
			}
		}
	}

	[DataSourceProperty]
	public double CrosshairAccuracy
	{
		get
		{
			return _crosshairAccuracy;
		}
		set
		{
			if (value != _crosshairAccuracy)
			{
				_crosshairAccuracy = value;
				OnPropertyChangedWithValue(value, "CrosshairAccuracy");
			}
		}
	}

	[DataSourceProperty]
	public double CrosshairScale
	{
		get
		{
			return _crosshairScale;
		}
		set
		{
			if (value != _crosshairScale)
			{
				_crosshairScale = value;
				OnPropertyChangedWithValue(value, "CrosshairScale");
			}
		}
	}

	[DataSourceProperty]
	public int CrosshairType
	{
		get
		{
			return _crosshairType;
		}
		set
		{
			if (value != _crosshairType)
			{
				_crosshairType = value;
				OnPropertyChangedWithValue(value, "CrosshairType");
			}
		}
	}

	public CrosshairVM()
	{
		ReloadPhases = new MBBindingList<ReloadPhaseItemVM>();
	}

	public void SetProperties(double accuracy, double scale)
	{
		CrosshairAccuracy = accuracy;
		CrosshairScale = scale;
	}

	public void SetArrowProperties(double topArrowOpacity, double rightArrowOpacity, double bottomArrowOpacity, double leftArrowOpacity)
	{
		TopArrowOpacity = topArrowOpacity;
		BottomArrowOpacity = bottomArrowOpacity;
		RightArrowOpacity = rightArrowOpacity;
		LeftArrowOpacity = leftArrowOpacity;
	}

	public void SetReloadProperties(in StackArray.StackArray10FloatFloatTuple reloadPhases, int reloadPhaseCount)
	{
		if (reloadPhaseCount == 0)
		{
			IsReloadPhasesVisible = false;
		}
		else
		{
			for (int i = 0; i < reloadPhaseCount; i++)
			{
				if (reloadPhases[i].Item1 < 1f)
				{
					IsReloadPhasesVisible = true;
					break;
				}
			}
		}
		PopulateReloadPhases(in reloadPhases, reloadPhaseCount);
	}

	private void PopulateReloadPhases(in StackArray.StackArray10FloatFloatTuple reloadPhases, int reloadPhaseCount)
	{
		if (reloadPhaseCount != ReloadPhases.Count)
		{
			ReloadPhases.Clear();
			for (int i = 0; i < reloadPhaseCount; i++)
			{
				ReloadPhases.Add(new ReloadPhaseItemVM(reloadPhases[i].Item1, reloadPhases[i].Item2));
			}
		}
		else
		{
			for (int j = 0; j < reloadPhaseCount; j++)
			{
				ReloadPhases[j].Update(reloadPhases[j].Item1, reloadPhases[j].Item2);
			}
		}
	}

	public void ShowHitMarker(bool isVictimDead, bool isHumanoidHeadShot)
	{
		IsVictimDead = isVictimDead;
		IsHitMarkerVisible = false;
		IsHitMarkerVisible = true;
		IsHumanoidHeadshot = false;
		IsHumanoidHeadshot = isHumanoidHeadShot;
	}
}
