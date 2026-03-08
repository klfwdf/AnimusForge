using System;
using System.Runtime.InteropServices;

namespace System.IO
{
	// Token: 0x02000188 RID: 392
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileOptions
	{
		// Token: 0x04000841 RID: 2113
		None = 0,
		// Token: 0x04000842 RID: 2114
		WriteThrough = -2147483648,
		// Token: 0x04000843 RID: 2115
		Asynchronous = 1073741824,
		// Token: 0x04000844 RID: 2116
		RandomAccess = 268435456,
		// Token: 0x04000845 RID: 2117
		DeleteOnClose = 67108864,
		// Token: 0x04000846 RID: 2118
		SequentialScan = 134217728,
		// Token: 0x04000847 RID: 2119
		Encrypted = 16384
	}
}
