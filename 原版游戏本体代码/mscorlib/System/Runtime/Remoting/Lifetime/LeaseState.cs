using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x02000823 RID: 2083
	[ComVisible(true)]
	[Serializable]
	public enum LeaseState
	{
		// Token: 0x040028B2 RID: 10418
		Null,
		// Token: 0x040028B3 RID: 10419
		Initial,
		// Token: 0x040028B4 RID: 10420
		Active,
		// Token: 0x040028B5 RID: 10421
		Renewing,
		// Token: 0x040028B6 RID: 10422
		Expired
	}
}
