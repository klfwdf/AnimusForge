using System;

namespace System
{
	// Token: 0x020000E7 RID: 231
	[Serializable]
	internal enum InternalGCCollectionMode
	{
		// Token: 0x04000582 RID: 1410
		NonBlocking = 1,
		// Token: 0x04000583 RID: 1411
		Blocking,
		// Token: 0x04000584 RID: 1412
		Optimized = 4,
		// Token: 0x04000585 RID: 1413
		Compacting = 8
	}
}
