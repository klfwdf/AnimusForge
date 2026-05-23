using System;

namespace TaleWorlds.Core;

[Flags]
public enum TroopUsageFlags : ushort
{
	None = 0,
	OnFoot = 1,
	Mounted = 2,
	Melee = 4,
	Ranged = 8,
	OneHandedUser = 0x10,
	ShieldUser = 0x20,
	TwoHandedUser = 0x40,
	PolearmUser = 0x80,
	BowUser = 0x100,
	ThrownUser = 0x200,
	CrossbowUser = 0x400,
	Undefined = ushort.MaxValue
}
