using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000327 RID: 807
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum TokenAccessLevels
	{
		// Token: 0x04001014 RID: 4116
		AssignPrimary = 1,
		// Token: 0x04001015 RID: 4117
		Duplicate = 2,
		// Token: 0x04001016 RID: 4118
		Impersonate = 4,
		// Token: 0x04001017 RID: 4119
		Query = 8,
		// Token: 0x04001018 RID: 4120
		QuerySource = 16,
		// Token: 0x04001019 RID: 4121
		AdjustPrivileges = 32,
		// Token: 0x0400101A RID: 4122
		AdjustGroups = 64,
		// Token: 0x0400101B RID: 4123
		AdjustDefault = 128,
		// Token: 0x0400101C RID: 4124
		AdjustSessionId = 256,
		// Token: 0x0400101D RID: 4125
		Read = 131080,
		// Token: 0x0400101E RID: 4126
		Write = 131296,
		// Token: 0x0400101F RID: 4127
		AllAccess = 983551,
		// Token: 0x04001020 RID: 4128
		MaximumAllowed = 33554432
	}
}
