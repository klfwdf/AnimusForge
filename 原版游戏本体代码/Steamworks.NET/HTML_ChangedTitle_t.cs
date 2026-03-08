using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000059 RID: 89
	[CallbackIdentity(4508)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_ChangedTitle_t
	{
		// Token: 0x040000BA RID: 186
		public const int k_iCallback = 4508;

		// Token: 0x040000BB RID: 187
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000BC RID: 188
		public string pchTitle;
	}
}
