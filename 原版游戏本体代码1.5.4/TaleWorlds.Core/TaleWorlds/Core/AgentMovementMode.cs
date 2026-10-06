using System;

namespace TaleWorlds.Core;

[Flags]
public enum AgentMovementMode : byte
{
	None = 0,
	Land = 1,
	WaterSurface = 2,
	WaterDiving = 3,
	PhysicsCheck = 4,
	NoPhysics = 8,
	MovementModeMask = 3
}
