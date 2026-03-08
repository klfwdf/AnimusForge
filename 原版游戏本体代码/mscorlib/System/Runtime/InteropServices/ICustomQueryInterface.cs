using System;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000967 RID: 2407
	[ComVisible(false)]
	[__DynamicallyInvokable]
	public interface ICustomQueryInterface
	{
		// Token: 0x06006228 RID: 25128
		[SecurityCritical]
		CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out IntPtr ppv);
	}
}
