using System;

namespace HarmonyLib
{
	// Token: 0x02000026 RID: 38
	internal enum InjectionType
	{
		// Token: 0x04000066 RID: 102
		Unknown,
		// Token: 0x04000067 RID: 103
		Instance,
		// Token: 0x04000068 RID: 104
		OriginalMethod,
		// Token: 0x04000069 RID: 105
		ArgsArray,
		// Token: 0x0400006A RID: 106
		Result,
		// Token: 0x0400006B RID: 107
		ResultRef,
		// Token: 0x0400006C RID: 108
		State,
		// Token: 0x0400006D RID: 109
		Exception,
		// Token: 0x0400006E RID: 110
		RunOriginal
	}
}
