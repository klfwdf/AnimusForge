using System;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
	// Token: 0x02000534 RID: 1332
	[__DynamicallyInvokable]
	public static class WaitHandleExtensions
	{
		// Token: 0x06003EA6 RID: 16038 RVA: 0x000E9300 File Offset: 0x000E7500
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static SafeWaitHandle GetSafeWaitHandle(this WaitHandle waitHandle)
		{
			if (waitHandle == null)
			{
				throw new ArgumentNullException("waitHandle");
			}
			return waitHandle.SafeWaitHandle;
		}

		// Token: 0x06003EA7 RID: 16039 RVA: 0x000E9316 File Offset: 0x000E7516
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static void SetSafeWaitHandle(this WaitHandle waitHandle, SafeWaitHandle value)
		{
			if (waitHandle == null)
			{
				throw new ArgumentNullException("waitHandle");
			}
			waitHandle.SafeWaitHandle = value;
		}
	}
}
