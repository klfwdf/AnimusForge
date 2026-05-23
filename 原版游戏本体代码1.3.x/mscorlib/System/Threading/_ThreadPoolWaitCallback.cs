using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000521 RID: 1313
	internal static class _ThreadPoolWaitCallback
	{
		// Token: 0x06003DE3 RID: 15843 RVA: 0x000E7589 File Offset: 0x000E5789
		[SecurityCritical]
		internal static bool PerformWaitCallback()
		{
			return ThreadPoolWorkQueue.Dispatch();
		}
	}
}
