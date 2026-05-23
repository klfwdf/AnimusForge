using System;

namespace TaleWorlds.GauntletUI.GamepadNavigation
{
	// Token: 0x0200004F RID: 79
	[Flags]
	public enum GamepadNavigationTypes
	{
		// Token: 0x0400026E RID: 622
		None = 0,
		// Token: 0x0400026F RID: 623
		Up = 1,
		// Token: 0x04000270 RID: 624
		Down = 2,
		// Token: 0x04000271 RID: 625
		Vertical = 3,
		// Token: 0x04000272 RID: 626
		Left = 4,
		// Token: 0x04000273 RID: 627
		Right = 8,
		// Token: 0x04000274 RID: 628
		Horizontal = 12
	}
}
