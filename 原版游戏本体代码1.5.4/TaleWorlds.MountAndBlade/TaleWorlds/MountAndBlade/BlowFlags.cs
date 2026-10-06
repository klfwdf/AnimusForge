using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[Flags]
[EngineStruct("Blow_flags", true, "bf", false)]
public enum BlowFlags
{
	None = 0,
	KnockBack = 0x10,
	KnockDown = 0x20,
	NoSound = 0x40,
	CrushThrough = 0x80,
	ShrugOff = 0x100,
	MakesRear = 0x200,
	NonTipThrust = 0x400,
	CanDismount = 0x800
}
