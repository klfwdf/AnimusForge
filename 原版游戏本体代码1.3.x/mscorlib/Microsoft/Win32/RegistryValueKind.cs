using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	// Token: 0x02000016 RID: 22
	[ComVisible(true)]
	public enum RegistryValueKind
	{
		// Token: 0x040001A2 RID: 418
		String = 1,
		// Token: 0x040001A3 RID: 419
		ExpandString,
		// Token: 0x040001A4 RID: 420
		Binary,
		// Token: 0x040001A5 RID: 421
		DWord,
		// Token: 0x040001A6 RID: 422
		MultiString = 7,
		// Token: 0x040001A7 RID: 423
		QWord = 11,
		// Token: 0x040001A8 RID: 424
		Unknown = 0,
		// Token: 0x040001A9 RID: 425
		[ComVisible(false)]
		None = -1
	}
}
