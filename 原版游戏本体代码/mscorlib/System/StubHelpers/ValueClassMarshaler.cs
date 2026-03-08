using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x02000598 RID: 1432
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class ValueClassMarshaler
	{
		// Token: 0x060042E2 RID: 17122
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertToNative(IntPtr dst, IntPtr src, IntPtr pMT, ref CleanupWorkList pCleanupWorkList);

		// Token: 0x060042E3 RID: 17123
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertToManaged(IntPtr dst, IntPtr src, IntPtr pMT);

		// Token: 0x060042E4 RID: 17124
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ClearNative(IntPtr dst, IntPtr pMT);
	}
}
