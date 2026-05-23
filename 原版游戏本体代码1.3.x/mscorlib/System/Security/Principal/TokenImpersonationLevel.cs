using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000328 RID: 808
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum TokenImpersonationLevel
	{
		// Token: 0x04001022 RID: 4130
		[__DynamicallyInvokable]
		None,
		// Token: 0x04001023 RID: 4131
		[__DynamicallyInvokable]
		Anonymous,
		// Token: 0x04001024 RID: 4132
		[__DynamicallyInvokable]
		Identification,
		// Token: 0x04001025 RID: 4133
		[__DynamicallyInvokable]
		Impersonation,
		// Token: 0x04001026 RID: 4134
		[__DynamicallyInvokable]
		Delegation
	}
}
