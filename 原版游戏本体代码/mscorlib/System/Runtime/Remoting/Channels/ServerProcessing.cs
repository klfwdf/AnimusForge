using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000846 RID: 2118
	[ComVisible(true)]
	[Serializable]
	public enum ServerProcessing
	{
		// Token: 0x040028F9 RID: 10489
		Complete,
		// Token: 0x040028FA RID: 10490
		OneWay,
		// Token: 0x040028FB RID: 10491
		Async
	}
}
