using System;
using System.Runtime.InteropServices;

namespace System.Security
{
	// Token: 0x020001F2 RID: 498
	[ComVisible(true)]
	[Serializable]
	public enum PolicyLevelType
	{
		// Token: 0x04000A8C RID: 2700
		User,
		// Token: 0x04000A8D RID: 2701
		Machine,
		// Token: 0x04000A8E RID: 2702
		Enterprise,
		// Token: 0x04000A8F RID: 2703
		AppDomain
	}
}
