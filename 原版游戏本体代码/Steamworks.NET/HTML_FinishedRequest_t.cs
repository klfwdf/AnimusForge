using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000057 RID: 87
	[CallbackIdentity(4506)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_FinishedRequest_t
	{
		// Token: 0x040000B3 RID: 179
		public const int k_iCallback = 4506;

		// Token: 0x040000B4 RID: 180
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000B5 RID: 181
		public string pchURL;

		// Token: 0x040000B6 RID: 182
		public string pchPageTitle;
	}
}
