using System;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
	// Token: 0x0200057D RID: 1405
	[FriendAccessAllowed]
	internal enum AsyncCausalityStatus
	{
		// Token: 0x04001B7F RID: 7039
		Canceled = 2,
		// Token: 0x04001B80 RID: 7040
		Completed = 1,
		// Token: 0x04001B81 RID: 7041
		Error = 3,
		// Token: 0x04001B82 RID: 7042
		Started = 0
	}
}
