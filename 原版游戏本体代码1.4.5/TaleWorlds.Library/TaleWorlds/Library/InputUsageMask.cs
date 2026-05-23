using System;

namespace TaleWorlds.Library;

[Flags]
public enum InputUsageMask
{
	Invalid = 0,
	MouseButtons = 1,
	MouseWheels = 2,
	Keyboardkeys = 4,
	BlockEverythingWithoutHitTest = 8,
	Mouse = 3,
	All = 7
}
