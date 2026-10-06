using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionAgentLockItemVM : ViewModel
{
	public enum LockStates
	{
		Possible,
		Active
	}

	private Vec2 _position;

	private int _lockState = -1;

	public Agent TrackedAgent { get; private set; }

	[DataSourceProperty]
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
				OnPropertyChangedWithValue(value, "Position");
			}
		}
	}

	[DataSourceProperty]
	public int LockState
	{
		get
		{
			return _lockState;
		}
		set
		{
			if (value != _lockState)
			{
				_lockState = value;
				OnPropertyChangedWithValue(value, "LockState");
			}
		}
	}

	public MissionAgentLockItemVM(Agent agent, LockStates initialLockState)
	{
		TrackedAgent = agent;
		LockState = (int)initialLockState;
	}

	public void SetLockState(LockStates lockState)
	{
		LockState = (int)lockState;
	}

	public void UpdatePosition(Vec2 position)
	{
		Position = position;
	}
}
