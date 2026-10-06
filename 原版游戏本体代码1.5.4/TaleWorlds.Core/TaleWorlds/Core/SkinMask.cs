using System;

namespace TaleWorlds.Core;

[Flags]
public enum SkinMask
{
	NoneVisible = 0,
	HeadVisible = 1,
	BodyVisible = 0x20,
	UnderwearVisible = 0x40,
	HandsVisible = 0x80,
	LegsVisible = 0x100,
	AllVisible = 0x1E1
}
