using System;
using System.Runtime.InteropServices;

namespace System.Threading
{
	// Token: 0x02000527 RID: 1319
	[ComVisible(true)]
	[Serializable]
	public enum ThreadPriority
	{
		// Token: 0x04001A1F RID: 6687
		Lowest,
		// Token: 0x04001A20 RID: 6688
		BelowNormal,
		// Token: 0x04001A21 RID: 6689
		Normal,
		// Token: 0x04001A22 RID: 6690
		AboveNormal,
		// Token: 0x04001A23 RID: 6691
		Highest
	}
}
