using System;
using System.Runtime.InteropServices;

namespace System.IO
{
	// Token: 0x02000186 RID: 390
	[ComVisible(true)]
	[Serializable]
	public enum FileMode
	{
		// Token: 0x04000838 RID: 2104
		CreateNew = 1,
		// Token: 0x04000839 RID: 2105
		Create,
		// Token: 0x0400083A RID: 2106
		Open,
		// Token: 0x0400083B RID: 2107
		OpenOrCreate,
		// Token: 0x0400083C RID: 2108
		Truncate,
		// Token: 0x0400083D RID: 2109
		Append
	}
}
