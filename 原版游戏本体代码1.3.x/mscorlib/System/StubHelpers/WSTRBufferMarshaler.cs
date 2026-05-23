using System;
using System.Runtime.ConstrainedExecution;

namespace System.StubHelpers
{
	// Token: 0x02000593 RID: 1427
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class WSTRBufferMarshaler
	{
		// Token: 0x060042D6 RID: 17110 RVA: 0x000F9768 File Offset: 0x000F7968
		internal static IntPtr ConvertToNative(string strManaged)
		{
			return IntPtr.Zero;
		}

		// Token: 0x060042D7 RID: 17111 RVA: 0x000F976F File Offset: 0x000F796F
		internal static string ConvertToManaged(IntPtr bstr)
		{
			return null;
		}

		// Token: 0x060042D8 RID: 17112 RVA: 0x000F9772 File Offset: 0x000F7972
		internal static void ClearNative(IntPtr pNative)
		{
		}
	}
}
