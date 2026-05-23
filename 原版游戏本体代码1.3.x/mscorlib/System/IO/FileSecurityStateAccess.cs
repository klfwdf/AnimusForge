using System;

namespace System.IO
{
	// Token: 0x02000189 RID: 393
	[Flags]
	internal enum FileSecurityStateAccess
	{
		// Token: 0x04000849 RID: 2121
		NoAccess = 0,
		// Token: 0x0400084A RID: 2122
		Read = 1,
		// Token: 0x0400084B RID: 2123
		Write = 2,
		// Token: 0x0400084C RID: 2124
		Append = 4,
		// Token: 0x0400084D RID: 2125
		PathDiscovery = 8,
		// Token: 0x0400084E RID: 2126
		AllAccess = 15
	}
}
