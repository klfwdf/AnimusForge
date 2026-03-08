using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000062 RID: 98
	[CallbackIdentity(4521)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_NewWindow_t
	{
		// Token: 0x040000E4 RID: 228
		public const int k_iCallback = 4521;

		// Token: 0x040000E5 RID: 229
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000E6 RID: 230
		public string pchURL;

		// Token: 0x040000E7 RID: 231
		public uint unX;

		// Token: 0x040000E8 RID: 232
		public uint unY;

		// Token: 0x040000E9 RID: 233
		public uint unWide;

		// Token: 0x040000EA RID: 234
		public uint unTall;

		// Token: 0x040000EB RID: 235
		public HHTMLBrowser unNewWindow_BrowserHandle_IGNORE;
	}
}
