using System;

namespace System.Security.AccessControl
{
	// Token: 0x020001F8 RID: 504
	[Flags]
	public enum PropagationFlags
	{
		// Token: 0x04000AA2 RID: 2722
		None = 0,
		// Token: 0x04000AA3 RID: 2723
		NoPropagateInherit = 1,
		// Token: 0x04000AA4 RID: 2724
		InheritOnly = 2
	}
}
