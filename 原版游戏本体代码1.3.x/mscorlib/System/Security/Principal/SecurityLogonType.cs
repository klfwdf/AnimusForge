using System;

namespace System.Security.Principal
{
	// Token: 0x0200032E RID: 814
	[Serializable]
	internal enum SecurityLogonType
	{
		// Token: 0x0400104F RID: 4175
		Interactive = 2,
		// Token: 0x04001050 RID: 4176
		Network,
		// Token: 0x04001051 RID: 4177
		Batch,
		// Token: 0x04001052 RID: 4178
		Service,
		// Token: 0x04001053 RID: 4179
		Proxy,
		// Token: 0x04001054 RID: 4180
		Unlock
	}
}
