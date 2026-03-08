using System;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x020005A7 RID: 1447
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class HResultExceptionMarshaler
	{
		// Token: 0x06004329 RID: 17193 RVA: 0x000FA2F6 File Offset: 0x000F84F6
		internal static int ConvertToNative(Exception ex)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			if (ex == null)
			{
				return 0;
			}
			return ex._HResult;
		}

		// Token: 0x0600432A RID: 17194 RVA: 0x000FA31C File Offset: 0x000F851C
		[SecuritySafeCritical]
		internal static Exception ConvertToManaged(int hr)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			Exception result = null;
			if (hr < 0)
			{
				result = StubHelpers.InternalGetCOMHRExceptionObject(hr, IntPtr.Zero, null, true);
			}
			return result;
		}
	}
}
