using System;

namespace System.Security.AccessControl
{
	// Token: 0x020001FA RID: 506
	[Flags]
	public enum SecurityInfos
	{
		// Token: 0x04000AAA RID: 2730
		Owner = 1,
		// Token: 0x04000AAB RID: 2731
		Group = 2,
		// Token: 0x04000AAC RID: 2732
		DiscretionaryAcl = 4,
		// Token: 0x04000AAD RID: 2733
		SystemAcl = 8
	}
}
