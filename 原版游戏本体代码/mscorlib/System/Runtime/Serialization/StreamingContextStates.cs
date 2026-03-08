using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000745 RID: 1861
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum StreamingContextStates
	{
		// Token: 0x04002465 RID: 9317
		CrossProcess = 1,
		// Token: 0x04002466 RID: 9318
		CrossMachine = 2,
		// Token: 0x04002467 RID: 9319
		File = 4,
		// Token: 0x04002468 RID: 9320
		Persistence = 8,
		// Token: 0x04002469 RID: 9321
		Remoting = 16,
		// Token: 0x0400246A RID: 9322
		Other = 32,
		// Token: 0x0400246B RID: 9323
		Clone = 64,
		// Token: 0x0400246C RID: 9324
		CrossAppDomain = 128,
		// Token: 0x0400246D RID: 9325
		All = 255
	}
}
