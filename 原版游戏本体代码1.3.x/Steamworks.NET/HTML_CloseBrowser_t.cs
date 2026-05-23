using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000055 RID: 85
	[CallbackIdentity(4504)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_CloseBrowser_t
	{
		// Token: 0x040000AA RID: 170
		public const int k_iCallback = 4504;

		// Token: 0x040000AB RID: 171
		public HHTMLBrowser unBrowserHandle;
	}
}
