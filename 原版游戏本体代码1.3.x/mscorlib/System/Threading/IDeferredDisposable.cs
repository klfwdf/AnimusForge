using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200050A RID: 1290
	internal interface IDeferredDisposable
	{
		// Token: 0x06003CC3 RID: 15555
		[SecurityCritical]
		void OnFinalRelease(bool disposed);
	}
}
