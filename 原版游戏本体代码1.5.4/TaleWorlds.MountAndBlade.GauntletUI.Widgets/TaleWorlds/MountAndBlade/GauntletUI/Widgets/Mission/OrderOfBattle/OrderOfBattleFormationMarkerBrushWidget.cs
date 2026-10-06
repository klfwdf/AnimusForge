using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleFormationMarkerBrushWidget : BrushWidget
{
	private Vec2 _position;

	private bool _isAvailable;

	private bool _isTracked;

	private int _wSign;

	[Editor(false)]
	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (value != _position)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	[Editor(false)]
	public bool IsAvailable
	{
		get
		{
			return _isAvailable;
		}
		set
		{
			if (value != _isAvailable)
			{
				_isAvailable = value;
				OnPropertyChanged(value, "IsAvailable");
			}
		}
	}

	[Editor(false)]
	public bool IsTracked
	{
		get
		{
			return _isTracked;
		}
		set
		{
			if (value != _isTracked)
			{
				_isTracked = value;
				OnPropertyChanged(value, "IsTracked");
			}
		}
	}

	[Editor(false)]
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
				OnPropertyChanged(value, "WSign");
			}
		}
	}

	public OrderOfBattleFormationMarkerBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.IsVisible = IsAvailable && WSign > 0;
		if (base.IsVisible)
		{
			base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = Position.y - base.Size.Y / 2f;
		}
	}
}
