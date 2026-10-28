using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Agent_movement_locked_state", true, "amls", false)]
public enum AgentMovementLockedState
{
	None,
	PositionLocked,
	FrameLocked
}
