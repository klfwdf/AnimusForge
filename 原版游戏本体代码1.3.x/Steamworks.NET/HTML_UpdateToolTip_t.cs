using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000066 RID: 102
	[CallbackIdentity(4525)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_UpdateToolTip_t
	{
		// Token: 0x040000F5 RID: 245
		public const int k_iCallback = 4525;

		// Token: 0x040000F6 RID: 246
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000F7 RID: 247
		public string pchMsg;
	}
}
