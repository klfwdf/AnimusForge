using System;

namespace System.Runtime.Versioning
{
	// Token: 0x02000721 RID: 1825
	[Flags]
	public enum ResourceScope
	{
		// Token: 0x04002414 RID: 9236
		None = 0,
		// Token: 0x04002415 RID: 9237
		Machine = 1,
		// Token: 0x04002416 RID: 9238
		Process = 2,
		// Token: 0x04002417 RID: 9239
		AppDomain = 4,
		// Token: 0x04002418 RID: 9240
		Library = 8,
		// Token: 0x04002419 RID: 9241
		Private = 16,
		// Token: 0x0400241A RID: 9242
		Assembly = 32
	}
}
