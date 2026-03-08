using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000064 RID: 100
	[CallbackIdentity(4523)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_StatusText_t
	{
		// Token: 0x040000EF RID: 239
		public const int k_iCallback = 4523;

		// Token: 0x040000F0 RID: 240
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000F1 RID: 241
		public string pchMsg;
	}
}
