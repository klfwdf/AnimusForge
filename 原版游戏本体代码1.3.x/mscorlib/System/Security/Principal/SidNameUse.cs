using System;

namespace System.Security.Principal
{
	// Token: 0x02000339 RID: 825
	internal enum SidNameUse
	{
		// Token: 0x040010A3 RID: 4259
		User = 1,
		// Token: 0x040010A4 RID: 4260
		Group,
		// Token: 0x040010A5 RID: 4261
		Domain,
		// Token: 0x040010A6 RID: 4262
		Alias,
		// Token: 0x040010A7 RID: 4263
		WellKnownGroup,
		// Token: 0x040010A8 RID: 4264
		DeletedAccount,
		// Token: 0x040010A9 RID: 4265
		Invalid,
		// Token: 0x040010AA RID: 4266
		Unknown,
		// Token: 0x040010AB RID: 4267
		Computer
	}
}
