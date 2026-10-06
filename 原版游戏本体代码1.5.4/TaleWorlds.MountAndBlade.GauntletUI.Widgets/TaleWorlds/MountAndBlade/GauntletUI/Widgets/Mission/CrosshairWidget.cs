using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class CrosshairWidget : Widget
{
	private double _crosshairAccuracy;

	private double _crosshairScale;

	private bool _isTargetInvalid;

	private double _topArrowOpacity;

	private double _bottomArrowOpacity;

	private double _rightArrowOpacity;

	private double _leftArrowOpacity;

	private bool _isVictimDead;

	private bool _showHitMarker;

	private bool _isHumanoidHeadshot;

	private BrushWidget _leftArrow;

	private BrushWidget _rightArrow;

	private BrushWidget _topArrow;

	private BrushWidget _bottomArrow;

	private BrushWidget _hitMarker;

	private BrushWidget _headshotMarker;

	[Editor(false)]
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
				OnPropertyChanged(value, "TopArrowOpacity");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "BottomArrowOpacity");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "RightArrowOpacity");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "LeftArrowOpacity");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "IsTargetInvalid");
				ApplyActionToAllChildrenRecursive(delegate(Widget child)
				{
					child.SetState(value ? "Invalid" : "Default");
				});
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "CrosshairAccuracy");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "CrosshairScale");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "IsVictimDead");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "IsHumanoidHeadshot");
				ShowHeadshotMarkerChanged();
			}
		}
	}

	[Editor(false)]
	public bool ShowHitMarker
	{
		get
		{
			return _showHitMarker;
		}
		set
		{
			if (value != _showHitMarker)
			{
				_showHitMarker = value;
				OnPropertyChanged(value, "ShowHitMarker");
				ShowHitMarkerChanged();
			}
		}
	}

	[Editor(false)]
	public BrushWidget LeftArrow
	{
		get
		{
			return _leftArrow;
		}
		set
		{
			if (value != _leftArrow)
			{
				_leftArrow = value;
				OnPropertyChanged(value, "LeftArrow");
			}
		}
	}

	[Editor(false)]
	public BrushWidget RightArrow
	{
		get
		{
			return _rightArrow;
		}
		set
		{
			if (value != _rightArrow)
			{
				_rightArrow = value;
				OnPropertyChanged(value, "RightArrow");
			}
		}
	}

	[Editor(false)]
	public BrushWidget TopArrow
	{
		get
		{
			return _topArrow;
		}
		set
		{
			if (value != _topArrow)
			{
				_topArrow = value;
				OnPropertyChanged(value, "TopArrow");
			}
		}
	}

	[Editor(false)]
	public BrushWidget BottomArrow
	{
		get
		{
			return _bottomArrow;
		}
		set
		{
			if (value != _bottomArrow)
			{
				_bottomArrow = value;
				OnPropertyChanged(value, "BottomArrow");
			}
		}
	}

	[Editor(false)]
	public BrushWidget HitMarker
	{
		get
		{
			return _hitMarker;
		}
		set
		{
			if (value != _hitMarker)
			{
				_hitMarker = value;
				OnPropertyChanged(value, "HitMarker");
				HitMarkerUpdated();
			}
		}
	}

	[Editor(false)]
	public BrushWidget HeadshotMarker
	{
		get
		{
			return _headshotMarker;
		}
		set
		{
			if (value != _headshotMarker)
			{
				_headshotMarker = value;
				OnPropertyChanged(value, "HeadshotMarker");
				HeadshotMarkerUpdated();
			}
		}
	}

	public CrosshairWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.IsVisible)
		{
			base.SuggestedWidth = (int)(74.0 + CrosshairAccuracy * 300.0);
			base.SuggestedHeight = (int)(74.0 + CrosshairAccuracy * 300.0);
		}
		LeftArrow.Brush.AlphaFactor = (float)LeftArrowOpacity;
		RightArrow.Brush.AlphaFactor = (float)RightArrowOpacity;
		TopArrow.Brush.AlphaFactor = (float)TopArrowOpacity;
		BottomArrow.Brush.AlphaFactor = (float)BottomArrowOpacity;
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.AddState("Invalid");
	}

	private void HitMarkerUpdated()
	{
		if (HitMarker != null)
		{
			HitMarker.AddState("Show");
		}
	}

	private void HeadshotMarkerUpdated()
	{
		if (HeadshotMarker != null)
		{
			HitMarker.AddState("Show");
		}
	}

	private void ShowHitMarkerChanged()
	{
		if (HitMarker != null)
		{
			string text = (IsVictimDead ? "ShowDeath" : "Show");
			if (HitMarker.CurrentState != text)
			{
				HitMarker.SetState(text);
			}
			else
			{
				HitMarker.BrushRenderer.RestartAnimation();
			}
		}
	}

	private void ShowHeadshotMarkerChanged()
	{
		if (HeadshotMarker != null)
		{
			string text = (IsHumanoidHeadshot ? "Show" : "Default");
			if (HeadshotMarker.CurrentState != text)
			{
				HeadshotMarker.SetState(text);
			}
			HeadshotMarker.BrushRenderer.RestartAnimation();
		}
	}
}
