using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200005A RID: 90
	[CallbackIdentity(4509)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_SearchResults_t
	{
		// Token: 0x040000BD RID: 189
		public const int k_iCallback = 4509;

		// Token: 0x040000BE RID: 190
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000BF RID: 191
		public uint unResults;

		// Token: 0x040000C0 RID: 192
		public uint unCurrentMatch;
	}
}
