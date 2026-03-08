using System;

namespace Mono.Cecil
{
	// Token: 0x02000285 RID: 645
	[Flags]
	internal enum ParameterAttributes : ushort
	{
		// Token: 0x04000536 RID: 1334
		None = 0,
		// Token: 0x04000537 RID: 1335
		In = 1,
		// Token: 0x04000538 RID: 1336
		Out = 2,
		// Token: 0x04000539 RID: 1337
		Lcid = 4,
		// Token: 0x0400053A RID: 1338
		Retval = 8,
		// Token: 0x0400053B RID: 1339
		Optional = 16,
		// Token: 0x0400053C RID: 1340
		HasDefault = 4096,
		// Token: 0x0400053D RID: 1341
		HasFieldMarshal = 8192,
		// Token: 0x0400053E RID: 1342
		Unused = 53216
	}
}
