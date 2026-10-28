using System;

namespace TaleWorlds.Library;

[Flags]
public enum InputType
{
	None = 0,
	MouseButton = 1,
	MouseWheel = 2,
	Key = 4
}
