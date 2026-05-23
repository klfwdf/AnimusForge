using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D5 RID: 2005
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum SoapOption
	{
		// Token: 0x040027CC RID: 10188
		None = 0,
		// Token: 0x040027CD RID: 10189
		AlwaysIncludeTypes = 1,
		// Token: 0x040027CE RID: 10190
		XsdString = 2,
		// Token: 0x040027CF RID: 10191
		EmbedAll = 4,
		// Token: 0x040027D0 RID: 10192
		Option1 = 8,
		// Token: 0x040027D1 RID: 10193
		Option2 = 16
	}
}
