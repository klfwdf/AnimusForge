using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x0200059B RID: 1435
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class UriMarshaler
	{
		// Token: 0x060042EB RID: 17131
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetRawUriFromNative(IntPtr pUri);

		// Token: 0x060042EC RID: 17132
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern IntPtr CreateNativeUriInstanceHelper(char* rawUri, int strLen);

		// Token: 0x060042ED RID: 17133 RVA: 0x000F98B4 File Offset: 0x000F7AB4
		[SecurityCritical]
		internal unsafe static IntPtr CreateNativeUriInstance(string rawUri)
		{
			char* ptr = rawUri;
			if (ptr != null)
			{
				ptr += RuntimeHelpers.OffsetToStringData / 2;
			}
			return UriMarshaler.CreateNativeUriInstanceHelper(ptr, rawUri.Length);
		}
	}
}
