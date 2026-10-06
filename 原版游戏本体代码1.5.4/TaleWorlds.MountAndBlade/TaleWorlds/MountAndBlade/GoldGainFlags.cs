using System;

namespace TaleWorlds.MountAndBlade;

[Flags]
public enum GoldGainFlags : ushort
{
	FirstRangedKill = 1,
	FirstMeleeKill = 2,
	FirstAssist = 4,
	SecondAssist = 8,
	ThirdAssist = 0x10,
	FifthKill = 0x20,
	TenthKill = 0x40,
	DefaultKill = 0x80,
	DefaultAssist = 0x100,
	ObjectiveCompleted = 0x200,
	ObjectiveDestroyed = 0x400,
	PerkBonus = 0x800
}
