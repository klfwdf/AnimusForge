using System;
using System.Runtime.InteropServices;

namespace System.IO
{
	// Token: 0x02000183 RID: 387
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileAccess
	{
		// Token: 0x04000831 RID: 2097
		Read = 1,
		// Token: 0x04000832 RID: 2098
		Write = 2,
		// Token: 0x04000833 RID: 2099
		ReadWrite = 3
	}
}
