using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000D0 RID: 208
	[Flags]
	public enum SkinMask
	{
		// Token: 0x04000632 RID: 1586
		NoneVisible = 0,
		// Token: 0x04000633 RID: 1587
		HeadVisible = 1,
		// Token: 0x04000634 RID: 1588
		BodyVisible = 32,
		// Token: 0x04000635 RID: 1589
		UnderwearVisible = 64,
		// Token: 0x04000636 RID: 1590
		HandsVisible = 128,
		// Token: 0x04000637 RID: 1591
		LegsVisible = 256,
		// Token: 0x04000638 RID: 1592
		AllVisible = 481
	}
}
