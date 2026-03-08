using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000325 RID: 805
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IPrincipal
	{
		// Token: 0x17000542 RID: 1346
		// (get) Token: 0x060028A0 RID: 10400
		[__DynamicallyInvokable]
		IIdentity Identity
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060028A1 RID: 10401
		[__DynamicallyInvokable]
		bool IsInRole(string role);
	}
}
