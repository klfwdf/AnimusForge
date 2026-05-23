using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000975 RID: 2421
	[Flags]
	public enum RegistrationConnectionType
	{
		// Token: 0x04002BCF RID: 11215
		SingleUse = 0,
		// Token: 0x04002BD0 RID: 11216
		MultipleUse = 1,
		// Token: 0x04002BD1 RID: 11217
		MultiSeparate = 2,
		// Token: 0x04002BD2 RID: 11218
		Suspended = 4,
		// Token: 0x04002BD3 RID: 11219
		Surrogate = 8
	}
}
