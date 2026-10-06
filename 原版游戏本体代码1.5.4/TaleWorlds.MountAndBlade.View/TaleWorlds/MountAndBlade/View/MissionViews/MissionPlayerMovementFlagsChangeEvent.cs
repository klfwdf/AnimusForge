using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionPlayerMovementFlagsChangeEvent : EventBase
{
	public Agent.MovementControlFlag MovementFlag { get; private set; }

	public MissionPlayerMovementFlagsChangeEvent(Agent.MovementControlFlag movementFlag)
	{
		MovementFlag = movementFlag;
	}
}
