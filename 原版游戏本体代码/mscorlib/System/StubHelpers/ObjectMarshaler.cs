using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.StubHelpers
{
	// Token: 0x02000597 RID: 1431
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class ObjectMarshaler
	{
		// Token: 0x060042DF RID: 17119
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertToNative(object objSrc, IntPtr pDstVariant);

		// Token: 0x060042E0 RID: 17120
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern object ConvertToManaged(IntPtr pSrcVariant);

		// Token: 0x060042E1 RID: 17121
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ClearNative(IntPtr pVariant);
	}
}
