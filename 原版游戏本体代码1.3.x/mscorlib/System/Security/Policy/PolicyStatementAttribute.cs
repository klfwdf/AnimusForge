using System;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x02000368 RID: 872
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum PolicyStatementAttribute
	{
		// Token: 0x04001192 RID: 4498
		Nothing = 0,
		// Token: 0x04001193 RID: 4499
		Exclusive = 1,
		// Token: 0x04001194 RID: 4500
		LevelFinal = 2,
		// Token: 0x04001195 RID: 4501
		All = 3
	}
}
