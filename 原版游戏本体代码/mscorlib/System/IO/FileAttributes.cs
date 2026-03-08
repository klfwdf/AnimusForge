using System;
using System.Runtime.InteropServices;

namespace System.IO
{
	// Token: 0x02000196 RID: 406
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileAttributes
	{
		// Token: 0x040008A3 RID: 2211
		ReadOnly = 1,
		// Token: 0x040008A4 RID: 2212
		Hidden = 2,
		// Token: 0x040008A5 RID: 2213
		System = 4,
		// Token: 0x040008A6 RID: 2214
		Directory = 16,
		// Token: 0x040008A7 RID: 2215
		Archive = 32,
		// Token: 0x040008A8 RID: 2216
		Device = 64,
		// Token: 0x040008A9 RID: 2217
		Normal = 128,
		// Token: 0x040008AA RID: 2218
		Temporary = 256,
		// Token: 0x040008AB RID: 2219
		SparseFile = 512,
		// Token: 0x040008AC RID: 2220
		ReparsePoint = 1024,
		// Token: 0x040008AD RID: 2221
		Compressed = 2048,
		// Token: 0x040008AE RID: 2222
		Offline = 4096,
		// Token: 0x040008AF RID: 2223
		NotContentIndexed = 8192,
		// Token: 0x040008B0 RID: 2224
		Encrypted = 16384,
		// Token: 0x040008B1 RID: 2225
		[ComVisible(false)]
		IntegrityStream = 32768,
		// Token: 0x040008B2 RID: 2226
		[ComVisible(false)]
		NoScrubData = 131072
	}
}
