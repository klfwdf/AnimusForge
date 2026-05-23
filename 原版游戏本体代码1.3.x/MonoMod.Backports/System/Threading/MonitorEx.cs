using System;
using System.Runtime.CompilerServices;

namespace System.Threading
{
	// Token: 0x02000021 RID: 33
	public static class MonitorEx
	{
		// Token: 0x0600015F RID: 351 RVA: 0x000095F5 File Offset: 0x000077F5
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Enter(object obj, ref bool lockTaken)
		{
			Monitor.Enter(obj, ref lockTaken);
		}
	}
}
