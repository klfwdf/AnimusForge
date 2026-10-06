using System;

namespace TaleWorlds.Core;

[Flags]
public enum TroopTraitsMask : ushort
{
	None = 0,
	Melee = 1,
	Ranged = 2,
	Mount = 4,
	Armor = 8,
	Thrown = 0x10,
	Spear = 0x20,
	Shield = 0x40,
	LowTier = 0x80,
	HighTier = 0x100,
	All = 0x1FF
}
