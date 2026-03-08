using System;
using System.Runtime.InteropServices;

namespace System.Threading
{
	// Token: 0x0200052C RID: 1324
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public static class Timeout
	{
		// Token: 0x04001A2F RID: 6703
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);

		// Token: 0x04001A30 RID: 6704
		[__DynamicallyInvokable]
		public const int Infinite = -1;

		// Token: 0x04001A31 RID: 6705
		internal const uint UnsignedInfinite = 4294967295U;
	}
}
