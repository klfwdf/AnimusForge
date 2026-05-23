using System;

namespace Steamworks
{
	// Token: 0x02000118 RID: 280
	[Flags]
	public enum EChatMemberStateChange
	{
		// Token: 0x04000639 RID: 1593
		k_EChatMemberStateChangeEntered = 1,
		// Token: 0x0400063A RID: 1594
		k_EChatMemberStateChangeLeft = 2,
		// Token: 0x0400063B RID: 1595
		k_EChatMemberStateChangeDisconnected = 4,
		// Token: 0x0400063C RID: 1596
		k_EChatMemberStateChangeKicked = 8,
		// Token: 0x0400063D RID: 1597
		k_EChatMemberStateChangeBanned = 16
	}
}
