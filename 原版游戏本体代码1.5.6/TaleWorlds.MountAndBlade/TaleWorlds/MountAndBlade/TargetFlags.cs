using System;

namespace TaleWorlds.MountAndBlade;

[Flags]
public enum TargetFlags
{
	None = 0,
	IsMoving = 1,
	IsFlammable = 2,
	IsStructure = 4,
	IsSiegeEngine = 8,
	IsAttacker = 0x10,
	IsSmall = 0x20,
	NotAThreat = 0x40,
	DebugThreat = 0x80,
	IsSiegeTower = 0x100,
	IsShip = 0x200
}
