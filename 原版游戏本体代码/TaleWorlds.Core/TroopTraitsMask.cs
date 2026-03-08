using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000D7 RID: 215
	[Flags]
	public enum TroopTraitsMask : ushort
	{
		// Token: 0x04000660 RID: 1632
		None = 0,
		// Token: 0x04000661 RID: 1633
		Melee = 1,
		// Token: 0x04000662 RID: 1634
		Ranged = 2,
		// Token: 0x04000663 RID: 1635
		Mount = 4,
		// Token: 0x04000664 RID: 1636
		Armor = 8,
		// Token: 0x04000665 RID: 1637
		Thrown = 16,
		// Token: 0x04000666 RID: 1638
		Spear = 32,
		// Token: 0x04000667 RID: 1639
		Shield = 64,
		// Token: 0x04000668 RID: 1640
		LowTier = 128,
		// Token: 0x04000669 RID: 1641
		HighTier = 256,
		// Token: 0x0400066A RID: 1642
		All = 511
	}
}
