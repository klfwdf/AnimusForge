using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A14 RID: 2580
	[FriendAccessAllowed]
	internal static class WindowsRuntimeBufferHelper
	{
		// Token: 0x060065BF RID: 26047
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("QCall")]
		private unsafe static extern void StoreOverlappedPtrInCCW(ObjectHandleOnStack windowsRuntimeBuffer, NativeOverlapped* overlapped);

		// Token: 0x060065C0 RID: 26048 RVA: 0x00159CD9 File Offset: 0x00157ED9
		[FriendAccessAllowed]
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal unsafe static void StoreOverlappedInCCW(object windowsRuntimeBuffer, NativeOverlapped* overlapped)
		{
			WindowsRuntimeBufferHelper.StoreOverlappedPtrInCCW(JitHelpers.GetObjectHandleOnStack<object>(ref windowsRuntimeBuffer), overlapped);
		}
	}
}
