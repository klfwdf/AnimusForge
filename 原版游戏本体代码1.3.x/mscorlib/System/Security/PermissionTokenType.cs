using System;

namespace System.Security
{
	// Token: 0x020001DF RID: 479
	[Flags]
	internal enum PermissionTokenType
	{
		// Token: 0x04000A28 RID: 2600
		Normal = 1,
		// Token: 0x04000A29 RID: 2601
		IUnrestricted = 2,
		// Token: 0x04000A2A RID: 2602
		DontKnow = 4,
		// Token: 0x04000A2B RID: 2603
		BuiltIn = 8
	}
}
