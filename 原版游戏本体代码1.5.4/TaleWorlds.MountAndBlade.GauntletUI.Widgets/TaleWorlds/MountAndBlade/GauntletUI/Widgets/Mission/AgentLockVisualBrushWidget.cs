using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class AgentLockVisualBrushWidget : BrushWidget
{
	private Vec2 _position;

	private int _lockState = -1;

	[Editor(false)]
	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	[Editor(false)]
	public int LockState
	{
		get
		{
			return _lockState;
		}
		set
		{
			if (_lockState != value)
			{
				_lockState = value;
				OnPropertyChanged(value, "LockState");
				UpdateVisualState(value);
			}
		}
	}

	public AgentLockVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.ScaledPositionXOffset = Position.X - base.Size.X / 2f;
		base.ScaledPositionYOffset = Position.Y - base.Size.Y / 2f;
	}

	private void UpdateVisualState(int lockState)
	{
		switch (lockState)
		{
		case 0:
			SetState("Possible");
			break;
		case 1:
			SetState("Active");
			break;
		}
	}
}
