using System;

namespace Mono.Cecil
{
	// Token: 0x020001E9 RID: 489
	internal enum AssemblyHashAlgorithm : uint
	{
		// Token: 0x04000329 RID: 809
		None,
		// Token: 0x0400032A RID: 810
		MD5 = 32771U,
		// Token: 0x0400032B RID: 811
		SHA1,
		// Token: 0x0400032C RID: 812
		SHA256 = 32780U,
		// Token: 0x0400032D RID: 813
		SHA384,
		// Token: 0x0400032E RID: 814
		SHA512,
		// Token: 0x0400032F RID: 815
		Reserved = 32771U
	}
}
