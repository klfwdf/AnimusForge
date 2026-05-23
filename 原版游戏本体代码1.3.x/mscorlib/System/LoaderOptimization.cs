using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x020000A0 RID: 160
	[ComVisible(true)]
	[Serializable]
	public enum LoaderOptimization
	{
		// Token: 0x040003B8 RID: 952
		NotSpecified,
		// Token: 0x040003B9 RID: 953
		SingleDomain,
		// Token: 0x040003BA RID: 954
		MultiDomain,
		// Token: 0x040003BB RID: 955
		MultiDomainHost,
		// Token: 0x040003BC RID: 956
		[Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		DomainMask = 3,
		// Token: 0x040003BD RID: 957
		[Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		DisallowBindings
	}
}
