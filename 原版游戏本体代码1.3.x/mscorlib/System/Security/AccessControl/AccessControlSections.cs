using System;

namespace System.Security.AccessControl
{
	// Token: 0x020001FC RID: 508
	[Flags]
	public enum AccessControlSections
	{
		// Token: 0x04000ABD RID: 2749
		None = 0,
		// Token: 0x04000ABE RID: 2750
		Audit = 1,
		// Token: 0x04000ABF RID: 2751
		Access = 2,
		// Token: 0x04000AC0 RID: 2752
		Owner = 4,
		// Token: 0x04000AC1 RID: 2753
		Group = 8,
		// Token: 0x04000AC2 RID: 2754
		All = 15
	}
}
