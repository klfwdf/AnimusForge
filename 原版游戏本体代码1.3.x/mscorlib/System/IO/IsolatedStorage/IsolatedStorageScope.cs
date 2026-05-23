using System;
using System.Runtime.InteropServices;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001AE RID: 430
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum IsolatedStorageScope
	{
		// Token: 0x04000945 RID: 2373
		None = 0,
		// Token: 0x04000946 RID: 2374
		User = 1,
		// Token: 0x04000947 RID: 2375
		Domain = 2,
		// Token: 0x04000948 RID: 2376
		Assembly = 4,
		// Token: 0x04000949 RID: 2377
		Roaming = 8,
		// Token: 0x0400094A RID: 2378
		Machine = 16,
		// Token: 0x0400094B RID: 2379
		Application = 32
	}
}
