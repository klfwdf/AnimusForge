using System;

namespace System.Threading
{
	// Token: 0x02000517 RID: 1303
	[Serializable]
	internal enum StackCrawlMark
	{
		// Token: 0x040019FD RID: 6653
		LookForMe,
		// Token: 0x040019FE RID: 6654
		LookForMyCaller,
		// Token: 0x040019FF RID: 6655
		LookForMyCallersCaller,
		// Token: 0x04001A00 RID: 6656
		LookForThread
	}
}
