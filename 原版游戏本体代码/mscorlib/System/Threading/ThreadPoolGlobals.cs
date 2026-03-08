using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200051A RID: 1306
	internal static class ThreadPoolGlobals
	{
		// Token: 0x04001A01 RID: 6657
		public static uint tpQuantum = 30U;

		// Token: 0x04001A02 RID: 6658
		public static int processorCount = Environment.ProcessorCount;

		// Token: 0x04001A03 RID: 6659
		public static bool tpHosted = ThreadPool.IsThreadPoolHosted();

		// Token: 0x04001A04 RID: 6660
		public static volatile bool vmTpInitialized;

		// Token: 0x04001A05 RID: 6661
		public static bool enableWorkerTracking;

		// Token: 0x04001A06 RID: 6662
		[SecurityCritical]
		public static ThreadPoolWorkQueue workQueue = new ThreadPoolWorkQueue();
	}
}
