using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200005E RID: 94
	[CallbackIdentity(4513)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_LinkAtPosition_t
	{
		// Token: 0x040000D3 RID: 211
		public const int k_iCallback = 4513;

		// Token: 0x040000D4 RID: 212
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000D5 RID: 213
		public uint x;

		// Token: 0x040000D6 RID: 214
		public uint y;

		// Token: 0x040000D7 RID: 215
		public string pchURL;

		// Token: 0x040000D8 RID: 216
		[MarshalAs(UnmanagedType.I1)]
		public bool bInput;

		// Token: 0x040000D9 RID: 217
		[MarshalAs(UnmanagedType.I1)]
		public bool bLiveLink;
	}
}
