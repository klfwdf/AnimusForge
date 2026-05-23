using System;
using System.Runtime.InteropServices;

namespace System.Threading
{
	// Token: 0x02000529 RID: 1321
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum ThreadState
	{
		// Token: 0x04001A25 RID: 6693
		Running = 0,
		// Token: 0x04001A26 RID: 6694
		StopRequested = 1,
		// Token: 0x04001A27 RID: 6695
		SuspendRequested = 2,
		// Token: 0x04001A28 RID: 6696
		Background = 4,
		// Token: 0x04001A29 RID: 6697
		Unstarted = 8,
		// Token: 0x04001A2A RID: 6698
		Stopped = 16,
		// Token: 0x04001A2B RID: 6699
		WaitSleepJoin = 32,
		// Token: 0x04001A2C RID: 6700
		Suspended = 64,
		// Token: 0x04001A2D RID: 6701
		AbortRequested = 128,
		// Token: 0x04001A2E RID: 6702
		Aborted = 256
	}
}
