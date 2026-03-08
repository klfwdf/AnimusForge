using System;
using System.Runtime.InteropServices;

namespace System.Threading
{
	// Token: 0x02000503 RID: 1283
	[ComVisible(true)]
	public struct NativeOverlapped
	{
		// Token: 0x040019A5 RID: 6565
		public IntPtr InternalLow;

		// Token: 0x040019A6 RID: 6566
		public IntPtr InternalHigh;

		// Token: 0x040019A7 RID: 6567
		public int OffsetLow;

		// Token: 0x040019A8 RID: 6568
		public int OffsetHigh;

		// Token: 0x040019A9 RID: 6569
		public IntPtr EventHandle;
	}
}
