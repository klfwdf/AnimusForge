using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000324 RID: 804
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IIdentity
	{
		// Token: 0x1700053F RID: 1343
		// (get) Token: 0x0600289D RID: 10397
		[__DynamicallyInvokable]
		string Name
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000540 RID: 1344
		// (get) Token: 0x0600289E RID: 10398
		[__DynamicallyInvokable]
		string AuthenticationType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000541 RID: 1345
		// (get) Token: 0x0600289F RID: 10399
		[__DynamicallyInvokable]
		bool IsAuthenticated
		{
			[__DynamicallyInvokable]
			get;
		}
	}
}
