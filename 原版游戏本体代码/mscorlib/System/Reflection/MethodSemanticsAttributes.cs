using System;

namespace System.Reflection
{
	// Token: 0x020005F9 RID: 1529
	[Flags]
	[Serializable]
	internal enum MethodSemanticsAttributes
	{
		// Token: 0x04001D2D RID: 7469
		Setter = 1,
		// Token: 0x04001D2E RID: 7470
		Getter = 2,
		// Token: 0x04001D2F RID: 7471
		Other = 4,
		// Token: 0x04001D30 RID: 7472
		AddOn = 8,
		// Token: 0x04001D31 RID: 7473
		RemoveOn = 16,
		// Token: 0x04001D32 RID: 7474
		Fire = 32
	}
}
