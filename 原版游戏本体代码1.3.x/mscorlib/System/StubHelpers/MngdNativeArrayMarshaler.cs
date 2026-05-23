using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.StubHelpers
{
	// Token: 0x0200059D RID: 1437
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class MngdNativeArrayMarshaler
	{
		// Token: 0x060042F1 RID: 17137
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags);

		// Token: 0x060042F2 RID: 17138
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

		// Token: 0x060042F3 RID: 17139
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

		// Token: 0x060042F4 RID: 17140
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);

		// Token: 0x060042F5 RID: 17141
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

		// Token: 0x060042F6 RID: 17142
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ClearNative(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);

		// Token: 0x060042F7 RID: 17143
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
	}
}
