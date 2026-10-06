using System;

namespace TaleWorlds.GauntletUI.GamepadNavigation;

[Flags]
public enum GamepadNavigationTypes
{
	None = 0,
	Up = 1,
	Down = 2,
	Vertical = 3,
	Left = 4,
	Right = 8,
	Horizontal = 0xC
}
