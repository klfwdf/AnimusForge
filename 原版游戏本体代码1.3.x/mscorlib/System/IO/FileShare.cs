using System;
using System.Runtime.InteropServices;

namespace System.IO
{
	// Token: 0x0200018A RID: 394
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileShare
	{
		// Token: 0x04000850 RID: 2128
		None = 0,
		// Token: 0x04000851 RID: 2129
		Read = 1,
		// Token: 0x04000852 RID: 2130
		Write = 2,
		// Token: 0x04000853 RID: 2131
		ReadWrite = 3,
		// Token: 0x04000854 RID: 2132
		Delete = 4,
		// Token: 0x04000855 RID: 2133
		Inheritable = 16
	}
}
