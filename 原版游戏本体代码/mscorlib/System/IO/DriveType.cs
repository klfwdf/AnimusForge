using System;
using System.Runtime.InteropServices;

namespace System.IO
{
	// Token: 0x0200017E RID: 382
	[ComVisible(true)]
	[Serializable]
	public enum DriveType
	{
		// Token: 0x04000824 RID: 2084
		Unknown,
		// Token: 0x04000825 RID: 2085
		NoRootDirectory,
		// Token: 0x04000826 RID: 2086
		Removable,
		// Token: 0x04000827 RID: 2087
		Fixed,
		// Token: 0x04000828 RID: 2088
		Network,
		// Token: 0x04000829 RID: 2089
		CDRom,
		// Token: 0x0400082A RID: 2090
		Ram
	}
}
