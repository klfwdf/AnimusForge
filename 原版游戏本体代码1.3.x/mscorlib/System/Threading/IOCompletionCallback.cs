using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000525 RID: 1317
	// (Invoke) Token: 0x06003DF3 RID: 15859
	[SecurityCritical]
	[CLSCompliant(false)]
	[ComVisible(true)]
	public unsafe delegate void IOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP);
}
