using System;

namespace System
{
	// Token: 0x020000B8 RID: 184
	[Serializable]
	internal enum ConfigEvents
	{
		// Token: 0x04000404 RID: 1028
		StartDocument,
		// Token: 0x04000405 RID: 1029
		StartDTD,
		// Token: 0x04000406 RID: 1030
		EndDTD,
		// Token: 0x04000407 RID: 1031
		StartDTDSubset,
		// Token: 0x04000408 RID: 1032
		EndDTDSubset,
		// Token: 0x04000409 RID: 1033
		EndProlog,
		// Token: 0x0400040A RID: 1034
		StartEntity,
		// Token: 0x0400040B RID: 1035
		EndEntity,
		// Token: 0x0400040C RID: 1036
		EndDocument,
		// Token: 0x0400040D RID: 1037
		DataAvailable,
		// Token: 0x0400040E RID: 1038
		LastEvent = 9
	}
}
