using System;
using System.Runtime.CompilerServices;

namespace System.Threading
{
	// Token: 0x02000483 RID: 1155
	internal static class MonitorEx
	{
		// Token: 0x060019B0 RID: 6576 RVA: 0x0005469D File Offset: 0x0005289D
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Enter(object obj, ref bool lockTaken)
		{
			Monitor.Enter(obj, ref lockTaken);
		}
	}
}
